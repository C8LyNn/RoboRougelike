#!/usr/bin/env python3
"""Pixel-registration receiver for four real semantically detached A1 SVG files.

Original SVG and independent layer sources are rendered by CairoSVG.
Pass requires the composite of actual PNG exports to match the original
subject-only artwork, not merely the authors' SVG names or a synthetic mirror.
This is concept-art source QA, not native mounting, 3D, PhysX or game QA.
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
from PIL import Image, ImageChops, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
OUT = HERE / "rendered" / "selected-layers"
SOURCE = HERE / "A1-single-wheel-arm.svg"
PARTS = (
    ("mount", "part-mount-proposal", "mount-proposal"),
    ("arm", "part-support-arm", "support-arm"),
    ("damper", "part-damper-visual", "damper-proposal"),
    ("wheel", "part-wheel", "wheel"),
)
SIZE = (1600, 1000)
NS = "{http://www.w3.org/2000/svg}"


def by_id(root: ET.Element, value: str) -> ET.Element:
    matches = [e for e in root.iter() if e.get("id") == value]
    if len(matches) != 1:
        raise AssertionError(f"Expected exactly one id={value}: {len(matches)}")
    return matches[0]


def sha256(b: bytes) -> str:
    return hashlib.sha256(b).hexdigest()


def png_of(raw_svg: bytes) -> Image.Image:
    png = cairosvg.svg2png(bytestring=raw_svg, output_width=SIZE[0], output_height=SIZE[1])
    img = Image.open(io.BytesIO(png)).convert("RGBA")
    img.load()
    return img


def main() -> int:
    checks: list[dict[str, object]] = []
    outputs: list[dict[str, object]] = []

    def check(name: str, condition: bool) -> None:
        checks.append({"name": name, "status": "PASS" if condition else "FAIL"})

    def save(name: str, image: Image.Image) -> None:
        path = OUT / name
        path.parent.mkdir(parents=True, exist_ok=True)
        image.save(path, "PNG")
        raw = path.read_bytes()
        outputs.append({"file": str(path.relative_to(HERE)),
                        "size": len(raw), "sha256": sha256(raw),
                        "crc32": f"{zlib.crc32(raw):08x}",
                        "dimensions": list(image.size)})

    original_bytes = SOURCE.read_bytes()
    original = ET.fromstring(original_bytes)
    subject = by_id(original, "subject-root")
    defs = next(n for n in original if n.tag == NS + "defs")
    subject_children = [n.get("id") for n in subject if n.get("id", "").startswith("part-")]
    check("original exactly four detachable concept groups",
          subject_children == [part[1] for part in PARTS])
    check("original width/height", (original.get("width"), original.get("height")) == ("1600", "1000"))

    # Render original subject WITHOUT title, annotations, shadow, or sheet background.
    baseline = ET.Element(NS + "svg", {"width": "1600", "height": "1000",
                                      "viewBox": "0 0 1600 1000"})
    baseline.append(copy.deepcopy(defs))
    baseline.append(copy.deepcopy(subject))
    expected = png_of(ET.tostring(baseline, encoding="utf-8"))
    save("A1-original-subject-transparent.png", expected)

    assembled = Image.new("RGBA", SIZE, (0, 0, 0, 0))
    contact = Image.new("RGB", (1280, 850), "#0c1828")
    draw = ImageDraw.Draw(contact)
    for index, (short, group_id, data_part) in enumerate(PARTS):
        layer_path = HERE / f"A1-layer-{short}.svg"
        layer_bytes = layer_path.read_bytes()
        layer_root = ET.fromstring(layer_bytes)
        target = by_id(layer_root, group_id)
        original_target = by_id(subject, group_id)
        extracted_parts = [e for e in layer_root.iter() if e.get("data-part")]
        check(f"{short} single detachable group", len(extracted_parts) == 1
              and extracted_parts[0].get("id") == group_id
              and target.get("data-part") == data_part)
        check(f"{short} original vector exact", ET.tostring(target).strip()
              == ET.tostring(original_target).strip())
        check(f"{short} transparent registration", (layer_root.get("width"), layer_root.get("height"))
              == ("1600", "1000")
              and layer_root.get("viewBox") == "0 0 1600 1000")
        if short == "mount":
            check("mount clearly proposal only",
                  target.get("data-status") == "DESIGN_PROPOSAL_UNKNOWN")

        layer_img = png_of(layer_bytes)
        alpha = layer_img.getchannel("A")
        check(f"{short} actually rendered visible pixels", alpha.getbbox() is not None)
        # Isolated layers MUST have real transparency instead of baked-in sheet.
        check(f"{short} no background baked into export", alpha.getpixel((0, 0)) == 0)
        save(f"A1-layer-{short}.png", layer_img)
        assembled = Image.alpha_composite(assembled, layer_img)

        tile_x = 24 + (index % 2) * 632
        tile_y = 18 + (index // 2) * 420
        tile = Image.new("RGBA", (616, 384), "#152536")
        small = layer_img.resize((616, 384), Image.Resampling.LANCZOS)
        tile = Image.alpha_composite(tile, small)
        contact.paste(tile.convert("RGB"), (tile_x, tile_y))
        draw.rectangle((tile_x, tile_y, tile_x + 615, tile_y + 383),
                       outline="#597e91", width=2)
        draw.text((tile_x + 18, tile_y + 16), f"{index+1:02d}  {short.upper()}",
                  fill="#d6eefa", font=ImageFont.load_default())

    save("A1-reassembled-layers.png", assembled)
    save("A1-4-layer-contact.png", contact)

    # A real source-vs-source raster parity check, including alpha channel.
    diff = ImageChops.difference(expected, assembled)
    extrema = diff.getextrema()
    max_error = max(high for low, high in extrema)
    histograms = [channel.histogram() for channel in diff.split()]
    total_abs = sum(sum(value * count for value, count in enumerate(hist))
                    for hist in histograms)
    mean_error = total_abs / (SIZE[0] * SIZE[1] * 4)
    check("pixelwise compositing mean error <= 0.2", mean_error <= 0.2)
    check("pixelwise compositing max error <= 8", max_error <= 8)

    result = {
        "status": "PASS" if all(c["status"] == "PASS" for c in checks) else "FAIL",
        "source": SOURCE.name,
        "source_sha256": sha256(original_bytes),
        "purpose": "Actual 4 independently exportable research-only SVG layers",
        "pixelwise_mean_abs_error_RGBA": round(mean_error, 6),
        "pixelwise_max_error_RGBA": max_error,
        "pass": sum(c["status"] == "PASS" for c in checks),
        "fail": sum(c["status"] == "FAIL" for c in checks),
        "checks": checks,
        "outputs": outputs,
        "not_run": ["Unity", "Blender", "PhysX", "3D", "mount measurement",
                    "gameplay", "human evaluation"],
    }
    OUT.mkdir(parents=True, exist_ok=True)
    (OUT / "A1-layer-export-receipt.json").write_text(
        json.dumps(result, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"status": result["status"], "pass": result["pass"],
                      "fail": result["fail"], "rgba_mean_error": round(mean_error, 6),
                      "rgba_max_error": max_error, "real_export_pngs": len(outputs)}))
    return 1 if result["fail"] else 0


if __name__ == "__main__":
    raise SystemExit(main())
