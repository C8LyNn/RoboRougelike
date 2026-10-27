#!/usr/bin/env python3
"""Rasterize W17 concept research SVGs and verify basic source/visible-image contracts.

This is a research-only visual artifact check. It neither loads Unity nor validates
physical attachment, camera occlusion, collisions, suspension, movement or playability.
"""
from __future__ import annotations
from pathlib import Path
import hashlib
import json
import sys
import xml.etree.ElementTree as ET
import cairosvg
from PIL import Image

HERE = Path(__file__).resolve().parent
OUT = HERE / "rendered"
SOURCE = ["comparison.svg", "A-wheel.svg", "B-track.svg", "C-omni.svg"]
EXPECTED = {"comparison.svg": (2100,974), "A-wheel.svg": (700,866),
            "B-track.svg": (700,866), "C-omni.svg": (700,866)}
SVGNS = "{http://www.w3.org/2000/svg}"
checks = []
files = []

def check(name: str, ok: bool, detail: str):
    checks.append({"name":name,"status":"PASS" if ok else "FAIL","detail":detail})
    print(f"{'PASS' if ok else 'FAIL'}: {name}: {detail}")

def record(path: Path):
    data = path.read_bytes()
    files.append({"path": str(path.relative_to(HERE)), "bytes":len(data),
                  "sha256":hashlib.sha256(data).hexdigest()})

def main():
    OUT.mkdir(parents=True, exist_ok=True)
    for file in SOURCE:
        raw = (HERE / file).read_bytes()
        try:
            tree = ET.fromstring(raw)
            valid = tree.tag == SVGNS+"svg"
            dims = (int(tree.attrib["width"]),int(tree.attrib["height"]))
            text = "".join(tree.itertext())
            shapes = sum(1 for elem in tree.iter() if elem.tag in
                         (SVGNS+"polygon",SVGNS+"path",SVGNS+"circle",SVGNS+"ellipse"))
            check(file+"-xml", valid, f"parsed root SVG; {shapes} vector primitives")
            check(file+"-reference-and-unknown",
                  ("0.55 m" in text and "SOCKET UNKNOWN" in text and
                   "HITBOX UNKNOWN" in text and "MASS UNKNOWN" in text and
                   "CLEARANCE UNKNOWN" in text), "0.55 m and all four UNKNOWN disclosures")
            check(file+"-dimensions",dims==EXPECTED[file],str(dims))
            check(file+"-original-shape-density",shapes>=25,f"{shapes} vector primitives")
        except Exception as err:
            check(file+"-xml",False,str(err))
            continue
        try:
            dest = OUT / file.replace(".svg",".png")
            cairosvg.svg2png(bytestring=raw,write_to=str(dest))
            with Image.open(dest) as im:
                im.load()
                px = im.convert("RGB")
                sample = [px.getpixel((x,y)) for x,y in
                          ((im.width//2,im.height//2),
                           (im.width//4,im.height//2),
                           (3*im.width//4,im.height//2),
                           (im.width//2,im.height//4))]
                check(file+"-raster-encoded",
                      im.format=="PNG" and im.size==EXPECTED[file],
                      f"real CairoSVG->PNG {im.size}; sampled pixel colors {sample}")
                # Distinct dimensions and output are real raster results, not readability pass.
                for sz in (128,64):
                    thumb=OUT / file.replace(".svg",f"-{sz}px.png")
                    newh=max(1,round(im.height*sz/im.width))
                    px.resize((sz,newh), Image.Resampling.LANCZOS).save(thumb)
                    record(thumb)
            record(dest)
        except Exception as err:
            check(file+"-raster-encoded",False,str(err))
    for name in SOURCE:
        record(HERE/name)
    summary={"purpose":"source SVG parse and real raster PNG existence; no visual gameplay proof",
             "status":"PASS" if all(x["status"]=="PASS" for x in checks) else "FAIL",
             "pass_count":sum(x["status"]=="PASS" for x in checks),
             "fail_count":sum(x["status"]=="FAIL" for x in checks),
             "checks":checks,"outputs":files,
             "not_run":["Unity EditMode","Unity PlayMode","PhysX collision","camera read tests",
                        "manual 64/128 px readability evaluation","PartSize socket/measured clearance"]}
    (OUT/"test-receipt.json").write_text(json.dumps(summary,indent=2)+"\n",encoding="utf-8")
    print(json.dumps({"status":summary["status"],"pass":summary["pass_count"],
                      "fail":summary["fail_count"],"outputs":len(files)}))
    return 0 if summary["fail_count"]==0 else 1

if __name__=="__main__":
    sys.exit(main())
