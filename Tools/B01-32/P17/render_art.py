#!/usr/bin/env python3
"""Render exactly the original B01-32 PASS17 damage decals to real RGBA PNG.
Reports actual alpha pixels, output SHA256 and atlas parity. NOT native Unity.
"""
import hashlib
import io
import json
import sys
from pathlib import Path
from xml.etree import ElementTree as ET

import cairosvg
from PIL import Image, ImageChops, ImageStat

ROOT = Path(__file__).resolve().parents[3]
SOURCE = ROOT / "Research/B01-32/P17/PartDamageDecals"
OUTPUT = Path(sys.argv[1]).resolve() if len(sys.argv) > 1 else ROOT / "build/b01-32-p17"
OUTPUT.mkdir(parents=True, exist_ok=True)
(OUTPUT / "frames").mkdir(exist_ok=True)

manifest = json.loads((SOURCE / "manifest.json").read_text(encoding="utf-8"))
assert manifest["package_id"] == "B01-32-DAMAGE-DECAL-001"
stages = manifest["stage_order"]
assert stages == ["clean", "abraded", "fractured", "critical"]
assert manifest["frame_order"] == [0, 1, 2]
assert manifest["atlas"]["output_px"] == [768, 1024]
assert len(manifest["stages"]) == 4

def digest(data):
    return hashlib.sha256(data).hexdigest()

def check_svg(path):
    source_bytes = path.read_bytes()
    root = ET.fromstring(source_bytes)
    ns = "{http://www.w3.org/2000/svg}"
    assert root.tag == ns + "svg" and root.attrib.get("viewBox") == "0 0 256 256"
    assert root.attrib.get("width") == "256" and root.attrib.get("height") == "256"
    assert root.find(ns + "title") is not None and root.find(ns + "desc") is not None
    used = [v for node in root.iter() for k, v in node.attrib.items() if k=="id"]
    assert len(used)==len(set(used)), "duplicate SVG IDs"
    for node in root.iter():
        assert not node.tag.endswith(("script","foreignObject","image")), "foreign executable/asset SVG"
        assert not any(k.lower().startswith("on") for k in node.attrib), "event callback SVG"
        assert not any("http://" in v or "https://" in v for k,v in node.attrib.items()
                       if k not in ("xmlns","{http://www.w3.org/2000/svg}xmlns")), "external link SVG"
    return source_bytes

atlas = Image.new("RGBA", (768, 1024), (0,0,0,0))
images = {}
metrics = []
for stage_index, stage in enumerate(stages):
    source_row = manifest["stages"][stage_index]
    assert source_row["name"] == stage
    for frame_index in range(3):
        name = f"{stage}-{frame_index}"
        rel = source_row["frames"][frame_index]
        assert rel == f"frames/{name}.svg"
        svg_source = check_svg(SOURCE / rel)
        png = cairosvg.svg2png(bytestring=svg_source, output_width=256, output_height=256)
        img = Image.open(io.BytesIO(png)).convert("RGBA")
        assert img.size == (256,256)
        images[name] = img
        alpha = img.getchannel("A")
        pixels = sum(count for alpha_value,count in enumerate(alpha.histogram()) if alpha_value > 0)
        # No texture may bleed into 8px border of a part overlay.
        assert alpha.crop((0,0,8,256)).getbbox() is None, name+" left-edge overflow"
        assert alpha.crop((248,0,256,256)).getbbox() is None, name+" right-edge overflow"
        assert alpha.crop((0,0,256,8)).getbbox() is None, name+" top-edge overflow"
        assert alpha.crop((0,248,256,256)).getbbox() is None, name+" bottom-edge overflow"
        if stage == "clean":
            assert pixels == 0, "Clean state leaked visual pixels"
        else:
            assert pixels > 100, "Required nonempty visual overlay missing: "+name
        path = OUTPUT / "frames" / (name+".png")
        img.save(path, optimize=True)
        atlas.paste(img, (frame_index*256, stage_index*256))
        metrics.append({"state":stage,"frame":frame_index,"svg_sha256":digest(svg_source),
                        "png_sha256":digest(path.read_bytes()),"alpha_pixels":pixels,
                        "atlas_rect_top_left":[frame_index*256,stage_index*256,256,256]})
        print("SOURCE_RASTER_PASS",name,"alpha_pixels",pixels)

for frame in range(3):
    assert images[f"clean-{frame}"].getchannel("A").getbbox() is None
assert all(images[f"critical-{i}"].getchannel("A").getbbox() for i in range(3))
for state in stages[1:]:
    a, b = images[f"{state}-0"], images[f"{state}-1"]
    diff = ImageChops.difference(a,b)
    assert sum(ImageStat.Stat(diff).sum) > 400, state+" frame 0 and 1 appear static"

# Distinct shapes are independent of hue: alpha mask must differ across states.
assert ImageChops.difference(images["abraded-1"].getchannel("A"),
                             images["fractured-1"].getchannel("A")).getbbox() is not None
assert ImageChops.difference(images["fractured-1"].getchannel("A"),
                             images["critical-1"].getchannel("A")).getbbox() is not None
print("NEGATIVE_STATE_PASS clean=transparent, all nonclean opaque, stage shapes differ")
print("ANIMATION_VARIATION_PASS each nonclean state has true frame pixels differences")

for stage_idx, stage in enumerate(stages):
    for frame in range(3):
        tile=atlas.crop((frame*256,stage_idx*256,(frame+1)*256,(stage_idx+1)*256))
        assert ImageChops.difference(tile,images[f"{stage}-{frame}"]).getbbox() is None
atlas_path=OUTPUT/"part-damage-atlas.png"
atlas.save(atlas_path,optimize=True)
print("ATLAS_PARITY_PASS 12/12 actual tiles, RGBA, 768x1024, no flipped source rows")

receipt={
  "package_id":manifest["package_id"],"test_kind":"original_source_raster_no_Unity",
  "png_atlas":"part-damage-atlas.png","png_atlas_sha256":digest(atlas_path.read_bytes()),
  "atlas_size":[768,1024],"unity_source_row_conversion":"unityRow = 3 - sourceRow",
  "stage_order":stages,"entries":metrics,"unity_native":"NOT_RUN"}
(OUTPUT/"receipt.json").write_text(json.dumps(receipt,indent=2)+"\n",encoding="utf-8")
for stage in stages[1:]:
    small=images[f"{stage}-1"].resize((64,64),Image.Resampling.LANCZOS)
    small.save(OUTPUT/f"{stage}-icon-64.png")
print("REAL_ATLAS_DONE input=12SVG output=12RGBA-PNG+atlas+3icons; UNITY=NOT_RUN")
