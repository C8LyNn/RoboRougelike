#!/usr/bin/env python3
"""B01-31 P17 W02: real original SVG -> PNG perspective-icon source QA.

Validates exact pinned original vector blobs and the actual three different
viewpoint drawings. This is image-source QA ONLY: not Unity, physical mounting,
human-recognition, material approval, or gameplay verification.
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
from PIL import Image, ImageDraw, ImageFont, ImageStat

HERE = Path(__file__).resolve().parent
OUT = HERE / "rendered"
NS = "{http://www.w3.org/2000/svg}"
VIEWS = ("face", "side", "top")
SIZES = (48, 96, 192, 384)
PARTS = {
    "part-mount": "mount-proposal",
    "part-arm": "support-arm",
    "part-damper": "damper-proposal",
    "part-wheel": "wheel",
}
GEOMETRY_KEYS = {
    "d", "cx", "cy", "rx", "ry", "r", "x", "y", "x1", "x2",
    "y1", "y2", "transform", "points", "width", "height",
}
DRAWABLES = {NS + n for n in (
    "circle", "ellipse", "rect", "path", "line", "polyline", "polygon"
)}


def blob_sha(raw: bytes) -> str:
    return hashlib.sha1(
        b"blob " + str(len(raw)).encode("ascii") + b"\0" + raw
    ).hexdigest()


def by_id(root: ET.Element, target: str) -> ET.Element:
    hits = [n for n in root.iter() if n.attrib.get("id") == target]
    if len(hits) != 1:
        raise ValueError(f"SVG id {target!r} multiplicity = {len(hits)}")
    return hits[0]


def geometry(root: ET.Element) -> tuple:
    entries = []
    for part_id in PARTS:
        part = by_id(root, part_id)
        for n in part.iter():
            if n.tag in DRAWABLES:
                attrs = tuple(sorted(
                    (k, v) for k, v in n.attrib.items() if k in GEOMETRY_KEYS
                ))
                entries.append((part_id, n.tag, attrs))
    return tuple(entries)


def image_from_svg(raw: bytes, size: int) -> Image.Image:
    png = cairosvg.svg2png(
        bytestring=raw, output_width=size, output_height=size
    )
    img = Image.open(io.BytesIO(png)).convert("RGBA")
    img.load()
    return img


def main() -> int:
    manifest = json.loads((HERE / "manifest.json").read_text("utf-8"))
    OUT.mkdir(parents=True, exist_ok=True)
    checks: list[dict] = []
    files: list[dict] = []
    raws: dict[str, bytes] = {}
    geometry_by_view: dict[str, tuple] = {}
    raster_sha_96: dict[str, str] = {}
    contact = Image.new("RGB", (3 * 256 + 48, 322), "#112333")
    draw = ImageDraw.Draw(contact)

    def check(name: str, condition: bool) -> None:
        checks.append({
            "name": name,
            "result": "PASS" if condition else "FAIL",
        })

    def write_png(path: Path, image: Image.Image):
        image.save(path, "PNG")
        raw_png = path.read_bytes()
        reopened = Image.open(io.BytesIO(raw_png))
        check(f"PNG header {path.name}", raw_png.startswith(b"\x89PNG\r\n\x1a\n"))
        check(f"real nonempty PNG {path.name}", len(raw_png) > 350)
        check(f"PNG decode {path.name}", reopened.size == image.size)
        files.append({
            "path": str(path.relative_to(HERE)),
            "size_bytes": len(raw_png),
            "sha256": hashlib.sha256(raw_png).hexdigest(),
            "crc32": f"{zlib.crc32(raw_png):08x}",
            "pixels": list(image.size),
        })

    check("approved subject remains one wheel plus support arm",
          manifest["authority"]["authoring_unit"] ==
          "SINGLE_ATTACHABLE_PART"
          and manifest["authority"]["subject"] ==
          "ONE_WHEEL_PLUS_SUPPORT_ARM"
          and manifest["authority"]["model_job"] == "NOT_AUTHORIZED")
    check("previous package is distinct color treatment, not views",
          manifest["previous_package"]["pr"] == 358
          and "COLOR THEMES" in
          manifest["previous_package"]["difference"])
    check("source provenance pinned to approved original",
          manifest["approved_source"]["a1_git_blob"] ==
          "96cc46a7c14f94a45c387e102d5940cb787d2d5c")
    check("declared views and outputs",
          tuple(v["view"] for v in manifest["assets"]) == VIEWS
          and tuple(manifest["png_sizes"]) == SIZES)

    for i, entry in enumerate(manifest["assets"]):
        view = entry["view"]
        path = HERE / entry["path"]
        raw = path.read_bytes()
        raws[view] = raw
        svg = ET.fromstring(raw)
        check(f"{view}: original file exact Git blob",
              blob_sha(raw) == entry["git_blob_sha1"])
        ids = [el.get("id") for el in svg.iter() if el.get("id")]
        check(f"{view}: unique SVG IDs", len(ids) == len(set(ids)))
        check(f"{view}: 256 square independent vector source",
              (svg.get("width"), svg.get("height"),
               svg.get("viewBox")) == ("256", "256", "0 0 256 256"))
        check(f"{view}: exactly ONE assembly root",
              len([n for n in svg.iter()
                   if n.get("data-authoring-unit") ==
                   "SINGLE_ATTACHABLE_PART"]) == 1)
        actual_parts = {
            n.get("id"): n.get("data-part")
            for n in svg.iter() if n.get("data-part")
        }
        check(f"{view}: four identifiable research parts",
              actual_parts == PARTS)
        check(f"{view}: all physical values UNMEASURED",
              all(by_id(svg, part).get("data-physical-status") ==
                  "UNMEASURED" for part in PARTS))
        geom = geometry(svg)
        geometry_by_view[view] = geom
        check(f"{view}: actual original drawing not empty",
              len(geom) >= 10)
        check(f"{view}: no embedded sheet background",
              all(not (n.tag == NS + "rect"
                       and n.get("width") == "256"
                       and n.get("height") == "256")
                  for n in svg.iter()))

        for size in SIZES:
            img = image_from_svg(raw, size)
            a = img.getchannel("A")
            bbox = a.getbbox()
            check(f"{view}/{size}: real raster dimensions",
                  img.size == (size, size))
            check(f"{view}/{size}: transparent corners",
                  all(a.getpixel(xy) == 0 for xy in
                      ((0, 0), (size-1, 0), (0, size-1),
                       (size-1, size-1))))
            check(f"{view}/{size}: true nonflat alpha",
                  ImageStat.Stat(a).stddev[0] >= 27)
            check(f"{view}/{size}: graphic not clipped",
                  bbox is not None and
                  bbox[0] >= max(1, size//64) and
                  bbox[1] >= max(1, size//64) and
                  bbox[2] <= size-max(1, size//64) and
                  bbox[3] <= size-max(1, size//64))
            check(f"{view}/{size}: sufficient visible coverage",
                  sum(px > 48 for px in a.getdata()) >
                  size * size * .075)
            name = f"A1-view-{view}-{size}.png"
            write_png(OUT / name, img)

            if size == 96:
                raster_sha_96[view] = hashlib.sha256(
                    img.tobytes()
                ).hexdigest()
            if size == 192:
                card = Image.new("RGBA", (256, 256), "#172e41")
                card.alpha_composite(img, (32, 26))
                contact.paste(card.convert("RGB"), (16+i*256, 36))
                draw.text(
                    (26+i*256, 13), view.upper(), fill="#f6c17e",
                    font=ImageFont.load_default()
                )

    check("three distinct real source geometries",
          len(set(geometry_by_view.values())) == 3)
    check("three distinct 96px original raster images",
          len(set(raster_sha_96.values())) == 3)

    # Positive geometry fingerprint must reject a real path change.
    mutant = copy.deepcopy(ET.fromstring(raws["face"]))
    real_wheel_circle = next(
        n for n in by_id(mutant, "part-wheel").iter()
        if n.tag == NS + "circle" and n.get("r") == "80"
    )
    real_wheel_circle.set("r", "81")
    check("negative: changed wheel circumference rejected",
          geometry(mutant) != geometry_by_view["face"])

    # Negative structural change must reject a lost identifiable subpart.
    lost_arm = copy.deepcopy(ET.fromstring(raws["side"]))
    by_id(lost_arm, "part-arm").set("data-part", "unknown")
    lost_parts = {
        n.get("id"): n.get("data-part")
        for n in lost_arm.iter() if n.get("data-part")
    }
    check("negative: missing semantic arm detected",
          lost_parts != PARTS)

    # Negative background pixel mutation must fail transparent-corner gate.
    opaque = copy.deepcopy(ET.fromstring(raws["top"]))
    bg = ET.Element(NS + "rect", {
        "x": "0", "y": "0", "width": "256", "height": "256",
        "fill": "#0c1929",
    })
    opaque.insert(0, bg)
    image = image_from_svg(ET.tostring(opaque), 48)
    check("negative: opaque canvas identified",
          image.getchannel("A").getpixel((0, 0)) != 0)

    write_png(OUT / "A1-orientation-contact.png", contact)
    check("actual output count includes 12 icons and 1 contact",
          len(files) == 13)

    receipt = {
        "package_id": manifest["package_id"],
        "version": manifest["version"],
        "source_qa": "ORIGINAL SVG PNG G0, not Unity native",
        "status": "PASS" if all(x["result"] == "PASS" for x in checks)
                  else "FAIL",
        "passed": sum(x["result"] == "PASS" for x in checks),
        "failed": sum(x["result"] == "FAIL" for x in checks),
        "checks": checks,
        "outputs": files,
        "not_run": [
            "Unity Editor", "Unity PlayMode", "Unity Player",
            "PhysX", "human legibility", "garage UI implementation",
            "Blender", "MODEL_JOB",
        ],
    }
    (OUT / "receipt.json").write_text(
        json.dumps(receipt, indent=2) + "\n", encoding="utf-8"
    )
    for t in checks:
        if t["result"] == "FAIL":
            print("FAILED_CHECK: " + t["name"])
    print(json.dumps({
        "status": receipt["status"], "pass": receipt["passed"],
        "fail": receipt["failed"], "png_files": len(files),
    }))
    return 1 if receipt["failed"] else 0


if __name__ == "__main__":
    raise SystemExit(main())
