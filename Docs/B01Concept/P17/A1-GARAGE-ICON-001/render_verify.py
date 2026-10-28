#!/usr/bin/env python3
"""Original B01-31 A1 garage icon artwork: real SVG pixels, theme parity, QA.

This tests only actual source vectors and image exports, never a Unity UI,
human readability result, approved material, physical mount or gameplay.
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
SIZES = (48, 96, 192, 384)
PARTS = {
    "part-mount-proposal": "mount-proposal",
    "part-support-arm": "support-arm",
    "part-damper-visual": "damper-proposal",
    "part-wheel": "wheel",
}
SPATIAL_KEYS = {
    "d", "cx", "cy", "r", "rx", "ry", "x", "y", "x1", "x2", "y1", "y2",
    "width", "height", "transform", "points",
}
DRAWABLES = {NS + x for x in ("circle", "ellipse", "rect", "path",
                              "line", "polygon", "polyline")}


def blob_sha(raw: bytes) -> str:
    return hashlib.sha1(b"blob " + str(len(raw)).encode() + b"\0" + raw).hexdigest()


def node(root: ET.Element, name: str) -> ET.Element:
    matches = [x for x in root.iter() if x.get("id") == name]
    if len(matches) != 1:
        raise AssertionError(f"{name} multiplicity: {len(matches)}")
    return matches[0]


def geometry_fingerprint(root: ET.Element) -> tuple:
    result = []
    for part_id in PARTS:
        group = node(root, part_id)
        for el in group.iter():
            if el.tag in DRAWABLES:
                values = tuple(sorted((k, v) for k, v in el.attrib.items()
                                      if k in SPATIAL_KEYS))
                result.append((part_id, el.tag, values))
    return tuple(result)


def render(svg_bytes: bytes, size: int) -> Image.Image:
    png = cairosvg.svg2png(bytestring=svg_bytes,
                           output_width=size, output_height=size)
    return Image.open(io.BytesIO(png)).convert("RGBA")


def contrast_share(image: Image.Image, bg_rgb: tuple[int, int, int]) -> float:
    background = Image.new("RGBA", image.size, bg_rgb + (255,))
    composed = Image.alpha_composite(background, image).convert("RGB")
    a = image.getchannel("A")
    opaque = 0
    contrasted = 0
    for pixel, alpha in zip(composed.getdata(), a.getdata()):
        if alpha < 32:
            continue
        opaque += 1
        if max(abs(channel - b) for channel, b in zip(pixel, bg_rgb)) >= 45:
            contrasted += 1
    return contrasted / max(opaque, 1)


def rgb(hex_value: str) -> tuple[int, int, int]:
    value = hex_value.lstrip("#")
    return tuple(int(value[i:i + 2], 16) for i in (0, 2, 4))


def main() -> int:
    OUT.mkdir(parents=True, exist_ok=True)
    manifest = json.loads((HERE / "manifest.json").read_text(encoding="utf-8"))
    checks: list[dict] = []
    files: list[dict] = []
    original_geom = None
    source_bytes = {}
    grid = Image.new("RGB", (3 * 288 + 40, 342), "#102234")
    draw = ImageDraw.Draw(grid)

    def check(name: str, good: bool) -> None:
        checks.append({"name": name, "status": "PASS" if good else "FAIL"})

    def save(name: str, image: Image.Image) -> None:
        path = OUT / name
        image.save(path, "PNG")
        raw = path.read_bytes()
        decoded = Image.open(path)
        check(f"{name}: actual PNG header", raw.startswith(b"\x89PNG\r\n\x1a\n"))
        check(f"{name}: decoded PNG dims", decoded.size == image.size)
        check(f"{name}: nontrivial real file", len(raw) > 450)
        files.append({
            "file": str(path.relative_to(HERE)), "bytes": len(raw),
            "sha256": hashlib.sha256(raw).hexdigest(),
            "crc32": f"{zlib.crc32(raw):08x}", "dimensions": list(decoded.size),
        })

    check("one approved detachable A1 part only",
          manifest["authority"]["unit"] == "SINGLE_ATTACHABLE_PART"
          and manifest["authority"]["MODEL_JOB"] == "NOT_AUTHORIZED")
    check("manifest declares exactly 3 variants and 4 image sizes",
          {x["variant"] for x in manifest["vector_assets"]} ==
          {"dark", "light", "mono"} and manifest["sizes_px"] == list(SIZES))
    check("separate old art branch immutable reference",
          manifest["source_reference"]["original_svg_git_blob_sha1"] ==
          "96cc46a7c14f94a45c387e102d5940cb787d2d5c")
    check("source intentionally not a CAD copy",
          manifest["source_reference"]["fidelity"].startswith("Simplified independent"))

    theme_hashes = []
    for index, item in enumerate(manifest["vector_assets"]):
        raw = (HERE / item["svg"]).read_bytes()
        source_bytes[item["variant"]] = raw
        root = ET.fromstring(raw)
        ids = [e.attrib["id"] for e in root.iter() if "id" in e.attrib]
        check(item["variant"] + ": Git blob exact",
              blob_sha(raw) == item["sha1"])
        check(item["variant"] + ": unique XML ids", len(ids) == len(set(ids)))
        check(item["variant"] + ": transparent 1:1 SVG dimensions",
              (root.get("width"), root.get("height"), root.get("viewBox")) ==
              ("256", "256", "0 0 256 256"))
        check(item["variant"] + ": exactly four semantic parts",
              {e.get("id"): e.get("data-part") for e in root.iter()
               if e.get("data-part")} == PARTS)
        check(item["variant"] + ": no claims of actual physical state",
              all(node(root, id_value).get("data-physical-status") ==
                  "UNMEASURED" for id_value in PARTS))
        geom = geometry_fingerprint(root)
        check(item["variant"] + ": actual geometry exists", len(geom) >= 22)
        if original_geom is None:
            original_geom = geom
        else:
            check(item["variant"] + ": identical shape geometry to dark source",
                  geom == original_geom)

        for size in SIZES:
            image = render(raw, size)
            check(f"{item['variant']} {size}: original raster size",
                  image.size == (size, size))
            alpha = image.getchannel("A")
            box = alpha.getbbox()
            check(f"{item['variant']} {size}: visible alpha",
                  box is not None and ImageStat.Stat(alpha).stddev[0] > 22)
            check(f"{item['variant']} {size}: transparent corners",
                  all(alpha.getpixel((x, y)) == 0
                      for x, y in ((0, 0), (size - 1, 0),
                                   (0, size - 1), (size - 1, size - 1))))
            check(f"{item['variant']} {size}: image not clipped",
                  box is not None and box[0] >= 2 and box[1] >= 2
                  and box[2] <= size - 2 and box[3] <= size - 2)
            frac = contrast_share(image, rgb(item["background_for_preview"]))
            check(f"{item['variant']} {size}: pixel contrast coverage > 18%",
                  frac > .18)
            save(f"A1-icon-{item['variant']}-{size}.png", image)
            if size == 192:
                theme_hashes.append(hashlib.sha256(image.tobytes()).hexdigest())
                preview = Image.new("RGBA", (288, 288),
                                    rgb(item["background_for_preview"]) + (255,))
                preview.alpha_composite(image, (48, 38))
                x = 16 + index * 288
                grid.paste(preview.convert("RGB"), (x, 28))
                draw.text((x + 8, 12), item["variant"].upper(),
                          fill="#f6ce83", font=ImageFont.load_default())
    check("three real visual color treatments differ",
          len(set(theme_hashes)) == 3)

    # Regression negative control 1: altering actual wheel geometry must
    # invalidate the all-colorways geometry parity, not get silently ignored.
    dark_root = ET.fromstring(source_bytes["dark"])
    geometry_mutant = copy.deepcopy(dark_root)
    wheel_path = next(e for e in node(geometry_mutant, "part-wheel").iter()
                      if e.tag == NS + "circle" and e.get("r") == "69")
    wheel_path.set("r", "71")
    check("negative mutant wheel geometry is rejected",
          geometry_fingerprint(geometry_mutant) != original_geom)

    # Regression negative control 2: an invisible same-background icon
    # must NOT qualify as a high-contrast garage icon.
    low_contrast_mutant = copy.deepcopy(dark_root)
    bg_hex = manifest["vector_assets"][0]["background_for_preview"]
    for el in low_contrast_mutant.iter():
        for attr in ("fill", "stroke"):
            if attr in el.attrib:
                el.set(attr, bg_hex)
    check("negative same-background palette is rejected",
          contrast_share(render(ET.tostring(low_contrast_mutant), 48),
                         rgb(bg_hex)) < .18)

    # Regression negative control 3: a baked opaque sheet must be rejected.
    opaque_mutant = copy.deepcopy(dark_root)
    bg = ET.Element(NS + "rect", {
        "x": "0", "y": "0", "width": "256", "height": "256",
        "fill": "#0c1b2d"
    })
    opaque_mutant.insert(0, bg)
    fake = render(ET.tostring(opaque_mutant), 48)
    check("negative baked background rejected",
          fake.getchannel("A").getpixel((0, 0)) != 0)

    save("A1-UI-contact.png", grid)
    report = {
        "package_id": manifest["package_id"],
        "version": manifest["version"],
        "status": "PASS" if all(t["status"] == "PASS" for t in checks)
                  else "FAIL",
        "pass": sum(t["status"] == "PASS" for t in checks),
        "fail": sum(t["status"] == "FAIL" for t in checks),
        "checks": checks, "real_png_files": files,
        "actual_outputs": len(files),
        "execution": "original editable SVG sources -> CairoSVG/Pillow",
        "not_run": ["Unity Editor/PlayMode", "actual garage import",
                    "human recognition", "actual in-game contrast",
                    "Blender", "PhysX", "MODEL_JOB"],
    }
    (OUT / "receipt.json").write_text(
        json.dumps(report, indent=2) + "\n", encoding="utf-8")
    for t in checks:
        if t["status"] == "FAIL":
            print("FAILED_CHECK: " + t["name"])
    print(json.dumps({"status": report["status"], "pass": report["pass"],
                      "fail": report["fail"],
                      "png_files": report["actual_outputs"]}))
    return 1 if report["fail"] else 0


if __name__ == "__main__":
    raise SystemExit(main())
