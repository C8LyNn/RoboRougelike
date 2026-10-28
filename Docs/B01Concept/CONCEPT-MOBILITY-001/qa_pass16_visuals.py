#!/usr/bin/env python3
"""PASS16: original selected A1 research-only art -> real PNG raster source gate.

Reads/validates immutable Git blob bytes, including new front/side/top artwork
and two color treatments. NOT a CAD/physics/Unity or human readability test.
"""
from __future__ import annotations

import hashlib
import io
import json
from pathlib import Path
import xml.etree.ElementTree as ET
import zlib

import cairosvg
from PIL import Image, ImageOps, ImageStat

HERE = Path(__file__).resolve().parent
OUT = HERE / "rendered" / "P16"
NS = "{http://www.w3.org/2000/svg}"
SOURCES = (("A1-three-direction-study.svg", (1800, 1060)),
           ("A1-color-treatment-study.svg", (1800, 960)))
MANIFEST = HERE / "A1-P16-research-manifest.json"
SHA_ART = "96cc46a7c14f94a45c387e102d5940cb787d2d5c"


def git_blob(raw: bytes) -> str:
    return hashlib.sha1(b"blob " + str(len(raw)).encode() + b"\0" + raw).hexdigest()


def get_by_id(root: ET.Element, target: str):
    return [e for e in root.iter() if e.attrib.get("id") == target]


def main() -> int:
    checks: list[dict] = []
    outputs: list[dict] = []
    OUT.mkdir(parents=True, exist_ok=True)

    def verify(name: str, good: bool):
        checks.append({"check": name, "status": "PASS" if good else "FAIL"})

    def save_png(name: str, img: Image.Image):
        path = OUT / name
        img.save(path)
        raw = path.read_bytes()
        verify("PNG header and actual image: " + name,
               raw.startswith(b"\x89PNG\r\n\x1a\n") and len(raw) > 500)
        verify("preview dimensions: " + name, Image.open(path).size == img.size)
        outputs.append({"path": str(path.relative_to(HERE)), "bytes": len(raw),
                        "sha256": hashlib.sha256(raw).hexdigest(),
                        "crc32": f"{zlib.crc32(raw):08x}",
                        "dimensions": list(img.size)})

    manifest = json.loads(MANIFEST.read_text(encoding="utf-8"))
    verify("scope approved single A1 only",
           manifest["authorization"]["authoring_unit"] == "SINGLE_ATTACHABLE_PART"
           and manifest["authorization"]["model_job"] == "NOT_AUTHORIZED")
    original = (HERE / "A1-single-wheel-arm.svg").read_bytes()
    verify("original A1 still immutable exact Git blob",
           git_blob(original) == SHA_ART
           == manifest["original_approved_source"]["git_blob_sha1"])
    original_root = ET.fromstring(original)
    original_subject = get_by_id(original_root, "subject-root")
    verify("original single subject-root", len(original_subject) == 1)
    record = {v["path"]: v["git_blob_sha1"] for v in
              manifest["new_visual_art"] + manifest["unchanged_semantic_layers"]}
    verify("manifest exactly covers two new original study sheets",
           set(p for p, _ in SOURCES) ==
           {v["path"] for v in manifest["new_visual_art"]})
    for path, sha in record.items():
        verify("actual Git blob pinned: " + path,
               git_blob((HERE / path).read_bytes()) == sha)

    original_part_ids = {"part-wheel", "part-support-arm", "part-damper-visual",
                         "part-mount-proposal"}
    verify("original semantic parts still four",
           original_part_ids.issubset(
               {e.get("id") for e in original_subject[0].iter()}))

    for file_name, dims in SOURCES:
        raw = (HERE / file_name).read_bytes()
        root = ET.fromstring(raw)
        ids = [e.attrib["id"] for e in root.iter() if "id" in e.attrib]
        verify(file_name + ": unique SVG IDs", len(ids) == len(set(ids)))
        verify(file_name + ": original source dimensions",
               (root.attrib.get("width"), root.attrib.get("height")) ==
               tuple(map(str, dims)))
        verify(file_name + ": not a gameplay/collider claim",
               b"MODEL_JOB NOT AUTHORIZED" in raw
               and (b"UNKNOWN" in raw or b"unverified" in raw))
        png = cairosvg.svg2png(bytestring=raw,
                               output_width=dims[0], output_height=dims[1])
        rendered = Image.open(io.BytesIO(png)).convert("RGB")
        verify(file_name + ": original CairoSVG raster size",
               rendered.size == dims)
        variance = ImageStat.Stat(ImageOps.grayscale(rendered)).stddev[0]
        verify(file_name + ": drawn art not blank", variance > 28)
        save_png(file_name.replace(".svg", ".png"), rendered)
        if file_name.startswith("A1-three"):
            views = {e.get("data-view") for e in root.iter() if e.get("data-view")}
            verify("three genuine distinct visual view groups",
                   views == {"SIDE_VISUAL_ONLY", "WHEEL_FACE_VISUAL_ONLY",
                             "TOP_VISUAL_ONLY"})
            verify("three independent wheel drawings",
                   all(len(get_by_id(root, x)) == 1 for x in
                       ("side-wheel", "front-wheel", "top-wheel")))
            for name, box in (("side", (61, 278, 600, 750)),
                              ("face", (630, 278, 1170, 750)),
                              ("top", (1200, 278, 1740, 750))):
                part = rendered.crop(box)
                verify("view contains actual visible geometry: " + name,
                       ImageStat.Stat(ImageOps.grayscale(part)).stddev[0] > 15)
                for sz in (64, 128):
                    thumb = part.resize((sz, sz), Image.Resampling.LANCZOS)
                    save_png(f"A1-P16-{name}-{sz}.png", thumb)
        else:
            uses = [el for el in root.iter() if el.tag == NS + "use"]
            hrefs = [el.attrib.get("href") for el in uses]
            verify("two presentations of same approved geometry",
                   len(uses) == 2 and hrefs == ["#source-visual-geometry"] * 2)
            verify("one filtered color treatment", sum("filter" in el.attrib
                   for el in uses) == 1)
            embedded = get_by_id(root, "source-subject-approved")
            verify("color study embeds exact source group",
                   len(embedded) == 1 and
                   ET.tostring(embedded[0]).replace(b"source-subject-approved",
                       b"subject-root") == ET.tostring(original_subject[0]))
            for label, box in (("cool", (58, 265, 865, 746)),
                               ("warm", (932, 265, 1740, 746))):
                crop = rendered.crop(box)
                verify("treatment visible: " + label,
                       ImageStat.Stat(ImageOps.grayscale(crop)).stddev[0] > 12)
                save_png("A1-P16-color-" + label + "-128.png",
                         crop.resize((128, 76), Image.Resampling.LANCZOS))

    report = {"package_id": manifest["package_id"],
              "status": "PASS" if all(x["status"] == "PASS" for x in checks) else "FAIL",
              "pass": sum(x["status"] == "PASS" for x in checks),
              "fail": sum(x["status"] == "FAIL" for x in checks),
              "checks": checks, "outputs": outputs,
              "execution": "actual original SVG bytes through CairoSVG/Pillow",
              "not_run": ["Unity", "PhysX", "Blender", "human reading",
                          "physical dimensions", "model export"]}
    (OUT / "A1-P16-raster-qa.json").write_text(
        json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"status": report["status"], "pass": report["pass"],
                      "fail": report["fail"], "pngs": len(outputs)}))
    return 1 if report["fail"] else 0


if __name__ == "__main__":
    raise SystemExit(main())
