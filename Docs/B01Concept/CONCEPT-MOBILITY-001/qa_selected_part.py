#!/usr/bin/env python3
"""Render an approved single-part research SVG from real vector source bytes.

Image/authoring QA only: not a physical mount, Unity, gameplay, or human test.
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
from PIL import Image, ImageOps

HERE = Path(__file__).resolve().parent
OUT = HERE / "rendered"
SVG = HERE / "A1-single-wheel-arm.svg"
NS = "{http://www.w3.org/2000/svg}"
LEGACY = {
    "A-wheel.svg": "b85bbb014a10eee7aab5e165d8772f68672a84bf",
    "B-track.svg": "bc618f99884aa55e7eff3fda9c232db2112c596f",
    "C-omni.svg": "0ec85328b1eb740f9fae8769e8cc4754b9f50c87",
}
EXPECTED = {
    "part-wheel": "wheel",
    "part-support-arm": "support-arm",
    "part-mount-proposal": "mount-proposal",
    "part-damper-visual": "damper-proposal",
}


def git_blob_sha(raw: bytes) -> str:
    return hashlib.sha1(f"blob {len(raw)}\0".encode() + raw).hexdigest()


def main() -> int:
    checks = []
    outputs = []

    def test(name: str, value: bool) -> None:
        checks.append({"name": name, "status": "PASS" if value else "FAIL"})

    source = SVG.read_bytes()
    tree = ET.fromstring(source)
    id_nodes = [n for n in tree.iter() if "id" in n.attrib]
    ids = [n.attrib["id"] for n in id_nodes]
    test("xml-and-unique-identifiers", len(ids) == len(set(ids)))
    test("standalone-art-canvas", tree.attrib.get("width") == "1600" and tree.attrib.get("height") == "1000")
    test("one-subject-root", ids.count("subject-root") == 1)
    parts = {n.attrib["id"]: n.attrib["data-part"] for n in id_nodes if n.attrib.get("data-part")}
    test("exact-four-semantic-part-groups", parts == EXPECTED)
    test("mount-interface-remains-proposal", any(
        n.attrib.get("id") == "part-mount-proposal"
        and n.attrib.get("data-status") == "DESIGN_PROPOSAL_UNKNOWN"
        for n in id_nodes))
    test("research-disclaimer", b"GAMEPLAY = UNKNOWN" in source and b"G-DESIGN PENDING" in source)
    for name, expected_blob in LEGACY.items():
        test(f"legacy-abc-unchanged:{name}", git_blob_sha((HERE / name).read_bytes()) == expected_blob)

    def render_and_check(name: str, raw: bytes, width: int, height: int, grayscale: bool = False):
        png = cairosvg.svg2png(bytestring=raw, output_width=width, output_height=height)
        img = Image.open(io.BytesIO(png)).convert("RGB")
        if grayscale:
            img = ImageOps.grayscale(img).convert("RGB")
        path = OUT / name
        path.parent.mkdir(parents=True, exist_ok=True)
        img.save(path, "PNG", optimize=False)
        verify = Image.open(path)
        test(f"actual-file:{name}", path.is_file() and path.stat().st_size > 200)
        test(f"exact-dims:{name}", verify.size == (width, height))
        pixels = ImageOps.grayscale(verify)
        extrema = pixels.getextrema()
        test(f"image-is-nonflat:{name}", extrema[1] - extrema[0] > (75 if width > 256 else 35))
        test(f"image-has-real-ink:{name}",
             sum(1 for x in pixels.getdata() if x > 65) >= (100 if width <= 128 else 300))
        data = path.read_bytes()
        outputs.append({"path": str(path.relative_to(HERE)), "size": len(data),
                        "sha256": hashlib.sha256(data).hexdigest(),
                        "crc32": f"{zlib.crc32(data):08x}", "dimensions": [width, height]})

    render_and_check("A1-selected-full.png", source, 1600, 1000)

    # Thumbnail uses EXACTLY the original subject group, never sheet labels.
    subject = next(n for n in tree if n.attrib.get("id") == "subject-root")
    defs = next(n for n in tree if n.tag == NS + "defs")
    crop = ET.Element(NS + "svg", {"width": "1150", "height": "540",
                                 "viewBox": "220 260 1150 540"})
    crop.append(copy.deepcopy(defs))
    ET.SubElement(crop, NS + "rect", {"x": "220", "y": "260",
                                     "width": "1150", "height": "540", "fill": "#0c1828"})
    crop.append(copy.deepcopy(subject))
    raw_subject = ET.tostring(crop, encoding="utf-8", xml_declaration=True)
    test("thumbnail-has-no-sheet-text",
         b"sheet-annotations" not in raw_subject and b"sheet-title" not in raw_subject)
    test("thumbnail-still-has-semantic-parts",
         all(x.encode() in raw_subject for x in EXPECTED))
    render_and_check("A1-selected-subject-128.png", raw_subject, 128, 60)
    render_and_check("A1-selected-subject-64.png", raw_subject, 64, 30)
    render_and_check("A1-selected-grayscale-128.png", raw_subject, 128, 60, grayscale=True)
    render_and_check("A1-selected-grayscale-64.png", raw_subject, 64, 30, grayscale=True)

    receipt = {"task_id": "B01-31-P15-CM001-A1-SINGLE-PART", "art_source": SVG.name,
               "source_git_blob_sha1": git_blob_sha(source),
               "source_sha256": hashlib.sha256(source).hexdigest(),
               "status": "PASS" if all(x["status"] == "PASS" for x in checks) else "FAIL",
               "pass": sum(x["status"] == "PASS" for x in checks),
               "fail": sum(x["status"] == "FAIL" for x in checks),
               "checks": checks, "outputs": outputs,
               "not_run": ["Unity", "Blender", "PhysX", "socket measurement",
                           "gameplay", "human recognition"]}
    OUT.mkdir(parents=True, exist_ok=True)
    (OUT / "A1-selected-test-receipt.json").write_text(
        json.dumps(receipt, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"status": receipt["status"], "pass": receipt["pass"],
                      "fail": receipt["fail"], "outputs": len(outputs),
                      "subject_only": True}))
    return int(receipt["fail"] > 0)


if __name__ == "__main__":
    raise SystemExit(main())
