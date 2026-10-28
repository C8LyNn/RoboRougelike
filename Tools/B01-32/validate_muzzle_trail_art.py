#!/usr/bin/env python3
"""Independent SVG source/render QA for B01-32 P15 research art. NOT UNITY."""
import io
import json
import sys
from pathlib import Path
from xml.etree import ElementTree as ET
from PIL import Image, ImageChops, ImageStat
import cairosvg

ROOT = Path(__file__).resolve().parents[2] / "Research/B01-32/P15/MuzzleTrail"
OUTPUT = Path(sys.argv[1]) if len(sys.argv) > 1 else ROOT / "_rendered"
OUTPUT.mkdir(parents=True, exist_ok=True)
(OUTPUT / "frames").mkdir(parents=True, exist_ok=True)
manifest = json.loads((ROOT / "sequence.json").read_text(encoding="utf-8"))
assert manifest["id"] == "B01-32-P15-MUZZLETRAIL-001"
frames = manifest["frames"]
assert len(frames) == 8, "Sequence must contain exactly eight distinct keyframes"
assert len(set(frame["file"] for frame in frames)) == 8
assert [float(f["intensity"]) for f in frames][0] == 0
assert [float(f["intensity"]) for f in frames][-1] == 0
assert frames[3]["intensity"] == 1
print("SOURCE_MANIFEST_PASS 8 original frames, idle/peak/idle stages")

preview = (ROOT / "viewer.html").read_text(encoding="utf-8")
for needle in ['id="play"', 'id="off"', 'id="seek"', 'id="next"', 'id="prev"', 'show(0)', 'timer=setTimeout', 'e.code==="Space"']:
    assert needle in preview, f"Missing actual viewer control {needle}"
print("SOURCE_VIEWER_CONTROL_PRESENCE_PASS 8/8 controls (browser behavior NOT inferred)")

images = []
metrics = []
svg_namespace = "{http://www.w3.org/2000/svg}"
for index, record in enumerate(frames):
    input_file = ROOT / record["file"]
    assert input_file.is_file(), record["file"]
    source = input_file.read_text(encoding="utf-8")
    element = ET.fromstring(source)
    assert element.tag == svg_namespace + "svg", record["file"]
    assert element.attrib.get("viewBox") == "0 0 1200 675"
    assert element.attrib.get("width") == "1200" and element.attrib.get("height") == "675"
    assert element.attrib.get("role") == "img"
    assert element.find(svg_namespace + "title") is not None
    assert element.find(svg_namespace + "desc") is not None
    ids = [e.attrib["id"] for e in element.iter() if "id" in e.attrib]
    assert len(ids) == len(set(ids)), "Duplicate SVG element IDs"
    assert not any(e.tag.endswith("script") or e.tag.endswith("foreignObject") for e in element.iter())
    assert not any(k.lower().startswith("on") for e in element.iter() for k in e.attrib)
    assert not any("http://" in value or "https://" in value for e in element.iter() for key, value in e.attrib.items()
                   if not key.endswith("xmlns")),"External asset/resource found"
    raster = cairosvg.svg2png(bytestring=source.encode("utf-8"), output_width=1200, output_height=675)
    image = Image.open(io.BytesIO(raster)).convert("RGB")
    assert image.size == (1200, 675)
    dest = OUTPUT / "frames" / (input_file.stem + ".png")
    image.save(dest)
    images.append(image)
    metrics.append({"index": index, "frame": input_file.name, "bytes_svg": len(source.encode("utf-8")),
                    "bytes_png": len(raster), "state": record["name"], "intensity":record["intensity"]})
    print(f"RENDER_PASS {index:02d} {input_file.stem} 1200x675 PNG={len(raster)}")

# Exclude title/status/labels, inspect physical muzzle plume area, not metadata changes.
crop = (680, 145, 1140, 485)
base = images[0].crop(crop)
def changed(a, b):
    image = ImageChops.difference(a.crop(crop), b.crop(crop)).convert("L")
    histogram = image.histogram()
    return sum(histogram[4:])
delta = [changed(images[i], images[0]) for i in range(8)]
assert delta[0] == 0
assert delta[7] == 0, f"Final idle still has an active plume: changed pixels={delta[7]}"
assert delta[3] > 1200, f"Peak has no visible discharge: {delta[3]}"
assert delta[3] > delta[1] * 2, f"Peak intensity not discernibly greater than charge: {delta}"
assert delta[6] < delta[3], f"Residue not visually quieter than peak: {delta}"
for m, pixels in zip(metrics, delta):
    m["plume_pixels_changed_vs_idle"] = pixels
print(f"RENDER_NEGATIVE_PASS idle_frame07_zero_residual; peak_vs_idle={delta[3]} altered pixels")
print(f"RENDER_GRADIENT_PASS charge={delta[1]} peak={delta[3]} residue={delta[6]}")
canvas = Image.new("RGB", (2400, 1350), (8, 17, 27))
for i, original in enumerate(images):
    thumb=original.resize((600, 338), Image.Resampling.LANCZOS)
    canvas.paste(thumb,(i%4*600,i//4*675))
    # Keep four-by-two large contact views with space for readability
# second row is at y=675; contact sheet uses 4 columns and 2 rows with vertical room
canvas.save(OUTPUT / "contact-sheet.png")
for size in [64, 128]:
    out = images[3].resize((size, round(size * 675 / 1200)), Image.Resampling.LANCZOS)
    out.save(OUTPUT / f"peak-thumbnail-{size}.png")
record={"kind":"original_research_SVG_render", "unity":"NOT_RUN", "graphics_driver":"NOT_TESTED",
        "source_frame_count":8, "plume_delta_pixels":delta, "frames":metrics}
(OUTPUT / "render_metrics.json").write_text(json.dumps(record,indent=2)+"\n",encoding="utf-8")
print("PASS: 8/8 source files parsed, 8/8 rendered, 2 idle-clear and intensity contrast checks; NO Unity claim")
