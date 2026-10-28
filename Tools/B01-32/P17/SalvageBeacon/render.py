#!/usr/bin/env python3
"""Rasterize B01-32 original salvage-beacon VECTOR source to real RGBA textures.
No Unity-native or gameplay claims. Reject broken frames, alpha bleeding,
duplicate phases, mismatched 4x2 sprite atlas and unsupported SVG externals.
"""
import hashlib
import io
import json
import sys
from pathlib import Path
from xml.etree import ElementTree as ET

import cairosvg
from PIL import Image, ImageChops, ImageStat, ImageDraw

ROOT=Path(__file__).resolve().parents[4]
SOURCE=ROOT/"Research/B01-32/P17/SalvageBeacon"
OUT=Path(sys.argv[1]).resolve() if len(sys.argv)>1 else ROOT/"build/b01-32-p17-salvage"
(OUT/"frames").mkdir(parents=True,exist_ok=True)
manifest=json.loads((SOURCE/"manifest.json").read_text(encoding="utf8"))
assert manifest["package_id"]=="B01-32-SALVAGE-BEACON-001"
assert manifest["frame_count"]==8 and manifest["src_px"]==[512,512]
assert manifest["atlas"]["size_px"]==[2048,1024]
assert len(manifest["frames"])==8
digest=lambda data:hashlib.sha256(data).hexdigest()
images=[]
stats=[]
atlas=Image.new("RGBA",(2048,1024),(0,0,0,0))
for i,entry in enumerate(manifest["frames"]):
    assert entry["id"]==i and entry["source"]==f"frames/beacon-{i}.svg"
    assert entry["column"]==i%4 and entry["row"]==i//4
    svg_path=SOURCE/entry["source"]
    original=svg_path.read_bytes()
    doc=ET.fromstring(original)
    ns="{http://www.w3.org/2000/svg}"
    assert doc.tag==ns+"svg" and doc.attrib.get("viewBox")=="0 0 512 512"
    assert doc.attrib.get("width")=="512" and doc.attrib.get("height")=="512"
    assert doc.find(ns+"title") is not None and doc.find(ns+"desc") is not None
    ids=[]
    for node in doc.iter():
        tag=node.tag.rsplit("}",1)[-1]
        assert tag not in ("script","image","foreignObject"),"unapproved executable/external svg node"
        for k,v in node.attrib.items():
            key=k.rsplit("}",1)[-1].lower()
            assert not key.startswith("on"), "SVG callback denied"
            if key in ("href","src"): assert not v.startswith(("http:","https:","file:")), "external reference denied"
            if key=="id":ids.append(v)
    assert len(ids)==len(set(ids)),"Duplicate internal SVG element IDs"
    png=cairosvg.svg2png(bytestring=original,output_width=512,output_height=512)
    img=Image.open(io.BytesIO(png)).convert("RGBA")
    assert img.size==(512,512)
    a=img.getchannel("A")
    assert a.getbbox() is not None, "missing visual frame"
    assert not a.crop((0,0,24,512)).getbbox(),"left alpha overflow"
    assert not a.crop((488,0,512,512)).getbbox(),"right alpha overflow"
    assert not a.crop((0,0,512,24)).getbbox(),"top alpha overflow"
    assert not a.crop((0,488,512,512)).getbbox(),"bottom alpha overflow"
    occupied=sum(c for alpha,c in enumerate(a.histogram()) if alpha>0)
    assert 15000 <= occupied <= 180000, "unexpected alpha coverage"
    path=OUT/"frames"/f"beacon-{i}.png"
    img.save(path,optimize=True)
    images.append(img)
    atlas.paste(img,((i%4)*512,(i//4)*512))
    stats.append({"phase":i,"source_svg_sha256":digest(original),
                  "rendered_png_sha256":digest(path.read_bytes()),
                  "alpha_pixels":occupied,"source_tile_top_left":[(i%4)*512,(i//4)*512,512,512]})
    print("ORIGINAL_SVG_RENDER_PASS",i,"alpha_pixels",occupied)

for i in range(8):
    j=(i+1)%8
    diff=ImageChops.difference(images[i],images[j])
    energy=sum(ImageStat.Stat(diff).sum)
    assert energy>120000, f"animation frame {i}->{j} not visibly different"
    tile=atlas.crop(((i%4)*512,(i//4)*512,((i%4)+1)*512,((i//4)+1)*512))
    assert ImageChops.difference(tile,images[i]).getbbox() is None,"Atlas tile has changed source RGBA"
print("ANIMATION_DISTINCT_PASS 8/8 adjacent frames independently rendered")
print("ATLAS_PARITY_PASS 8/8 RGBA tiles at exact 2048x1024 size")
atlas_path=OUT/"salvage-beacon-atlas.png"
atlas.save(atlas_path,optimize=True)
images[2].resize((128,128),Image.Resampling.LANCZOS).save(OUT/"salvage-beacon-icon-128.png",optimize=True)

# Inspectable 4x2 frame reference, with checkerboard matte and labels.
sheet=Image.new("RGBA",(2048,1024),(17,32,43,255))
painter=ImageDraw.Draw(sheet)
for y in range(0,1024,32):
    for x in range(0,2048,32):
        if (x//32+y//32)%2==0:painter.rectangle((x,y,x+31,y+31),fill=(30,48,60,255))
for i,img in enumerate(images):
    sheet.alpha_composite(img,((i%4)*512,(i//4)*512))
    painter.text(((i%4)*512+24,(i//4)*512+32),f"Frame {i}",fill=(245,240,221,255))
sheet.convert("RGB").save(OUT/"contact-sheet-4x2.png",optimize=True)
receipt={"package_id":manifest["package_id"],
 "original_source_sha":None,
 "test_kind":"physical_original_svg_raster_no_unity",
 "atlas":{"name":"salvage-beacon-atlas.png","dimensions":[2048,1024],
          "sha256":digest(atlas_path.read_bytes()),"frames":8,
          "unity_row_formula":"unityRow = 1-sourceRow"},
 "frames":stats,"native_unity":"NOT_RUN","integrated":False}
(OUT/"receipt.json").write_text(json.dumps(receipt,indent=2)+"\n",encoding="utf8")
print("SALVAGE_GAME_ART_READY 8 PNG frames + 2048x1024 atlas + reference + icon; native Unity NOT_RUN")
