#!/usr/bin/env python3
"""Real-source receiver for B01-31 A1 exploded assembly research artwork.

Loads an exact approved source blob; validates original group identity against
the new exploded SVG; renders actual sheet and alpha-only part sprites via
CairoSVG/Pillow. No Unity/Blender/physics or real socket acceptance.
"""
from __future__ import annotations

import copy
import hashlib
import io
import json
from pathlib import Path
import xml.etree.ElementTree as ET
import zlib

import cairosvg
from PIL import Image, ImageStat

HERE = Path(__file__).resolve().parent
OUT = HERE / "rendered"
SVG = HERE / "A1-exploded-assembly.svg"
REFERENCE = HERE / "A1-approved-reference.svg"
MANIFEST = HERE / "manifest.json"
NS = "{http://www.w3.org/2000/svg}"
SHAPES = {
    "mount": ("part-mount-proposal", (95, 385, 455, 815)),
    "arm": ("part-support-arm", (530, 345, 1240, 815)),
    "damper": ("part-damper-visual", (577, 160, 1010, 493)),
    "wheel": ("part-wheel", (1300, 302, 1845, 845)),
}
PINNED_SOURCE_GIT_BLOB = "96cc46a7c14f94a45c387e102d5940cb787d2d5c"


def git_blob(data: bytes) -> str:
    return hashlib.sha1(b"blob " + str(len(data)).encode() + b"\0" + data).hexdigest()


def by_id(root: ET.Element, id_value: str) -> ET.Element:
    matches = [el for el in root.iter() if el.get("id") == id_value]
    if len(matches) != 1:
        raise ValueError(f"Expected one id={id_value}, got {len(matches)}")
    return matches[0]


def main() -> int:
    checks = []
    outputs = []

    def check(description: str, condition: bool):
        checks.append({"check": description, "status": "PASS" if condition else "FAIL"})

    def record(path: Path, im: Image.Image):
        path.parent.mkdir(parents=True, exist_ok=True)
        im.save(path)
        data = path.read_bytes()
        actual = Image.open(io.BytesIO(data))
        check(f"PNG dimensions {path.name}", actual.size == im.size)
        check(f"PNG nonempty {path.name}",
              data.startswith(b"\x89PNG\r\n\x1a\n") and len(data) > 500)
        outputs.append({"file": str(path.relative_to(HERE)),
                        "bytes": len(data),
                        "sha256": hashlib.sha256(data).hexdigest(),
                        "crc32": f"{zlib.crc32(data):08x}",
                        "dimensions": list(actual.size)})

    source_bytes = REFERENCE.read_bytes()
    art_bytes = SVG.read_bytes()
    m = json.loads(MANIFEST.read_text("utf-8"))
    source_root = ET.fromstring(source_bytes)
    art_root = ET.fromstring(art_bytes)
    check("approved original-source Git blob identity",
          git_blob(source_bytes) == PINNED_SOURCE_GIT_BLOB
          == m["source_provenance"]["reference_blob_sha1"])
    check("approved direction one part and no model job",
          m["authorization"]["unit"] == "SINGLE_ATTACHABLE_PART"
          and m["authorization"]["model_job"] == "NOT_AUTHORIZED")
    check("correct new package version",
          m["package_id"] == "B01-31-A1-EXPLODED-001"
          and m["version"] == "P17-R1")
    check("actual SVG sheet 1920x1080",
          (art_root.get("width"), art_root.get("height")) == ("1920", "1080"))
    ids = [el.get("id") for el in art_root.iter() if el.get("id")]
    check("all SVG IDs unique", len(ids) == len(set(ids)))
    src_parts = by_id(source_root, "subject-root")
    wrapper = by_id(art_root, "exploded-subject")
    check("four exploded wrappers", len(list(wrapper)) == len(SHAPES))
    check("art not claiming mechanical dimensions",
          b"NOT A MECHANICAL ASSEMBLY INSTRUCTION" in art_bytes
          and b"MODEL_JOB" in art_bytes)

    # This is the important non-synthetic test: compare EACH imported
    # original SVG group structurally against the real pinned original source.
    for k, (part_id, _) in SHAPES.items():
        old = by_id(src_parts, part_id)
        new = by_id(wrapper, part_id)
        check(f"{k} exactly original vector group",
              ET.tostring(old) == ET.tostring(new))
        layer = by_id(wrapper, "exploded-" + k)
        check(f"{k} separate transform and source id",
              layer.get("data-source-id") == part_id
              and layer.get("data-physical-status") == "UNMEASURED"
              and "transform" in layer.attrib)
        check(f"{k} manifest receiver identity",
              "exploded-" + k in m["new_art"]["group_ids"])

    sheet_png = cairosvg.svg2png(bytestring=art_bytes)
    sheet = Image.open(io.BytesIO(sheet_png)).convert("RGBA")
    check("real sheet CairoSVG raster size", sheet.size == (1920, 1080))
    check("real full raster not blank",
          ImageStat.Stat(sheet.convert("RGB")).stddev[0] > 20)
    record(OUT / "A1-exploded-sheet.png", sheet)

    # Real sprite exports: render only each original imported vector group
    # with its own transform, with NO sheet background, annotation or label.
    def elements_by_tag(parent, tag):
        return [e for e in parent if e.tag == NS + tag]

    defs = elements_by_tag(art_root, "defs")
    for key, (_, (x0, y0, x1, y1)) in SHAPES.items():
        sub = ET.Element(NS + "svg", {
            "width": "128", "height": "128",
            "viewBox": f"{x0} {y0} {x1-x0} {y1-y0}",
        })
        for d in defs:
            sub.append(copy.deepcopy(d))
        sub.append(copy.deepcopy(by_id(wrapper, "exploded-" + key)))
        sub_png = cairosvg.svg2png(bytestring=ET.tostring(sub, encoding="utf-8"))
        sprite = Image.open(io.BytesIO(sub_png)).convert("RGBA")
        alpha = sprite.getchannel("A")
        check(f"{key} actual opaque geometry", alpha.getextrema()[1] >= 210)
        check(f"{key} background transparency", alpha.getpixel((0, 0)) == 0)
        check(f"{key} actual non-flat alpha", ImageStat.Stat(alpha).stddev[0] > 25)
        record(OUT / f"A1-{key}-128.png", sprite)
        record(OUT / f"A1-{key}-64.png", sprite.resize((64, 64), Image.Resampling.LANCZOS))

    manifest = {
        "status": "PASS" if all(x["status"] == "PASS" for x in checks) else "FAIL",
        "package_id": m["package_id"], "version": m["version"],
        "approved_source_git_blob": PINNED_SOURCE_GIT_BLOB,
        "actual_exploded_svg_sha256": hashlib.sha256(art_bytes).hexdigest(),
        "checks_pass": sum(x["status"] == "PASS" for x in checks),
        "checks_fail": sum(x["status"] == "FAIL" for x in checks),
        "checks": checks, "real_raster_outputs": outputs,
        "execution": "original SVG bytes -> CairoSVG 2.8.2 -> Pillow 11.3.0",
        "not_run": ["Unity Editor", "PhysX", "3D socket", "Blender", "Windows Player", "human evaluation"]
    }
    OUT.mkdir(parents=True, exist_ok=True)
    (OUT / "A1-P17-receipt.json").write_text(
        json.dumps(manifest, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    print(json.dumps({"status": manifest["status"],
                      "pass": manifest["checks_pass"],
                      "fail": manifest["checks_fail"],
                      "pngs": len(outputs)}))
    return 1 if manifest["checks_fail"] else 0


if __name__ == "__main__":
    raise SystemExit(main())
