#!/usr/bin/env python3
"""Read-only verification of produced, physical thermal VFX files.
Does not run original producer's renderer; Unity/native remains NOT_RUN.
"""
import argparse
import hashlib
import io
import json
from pathlib import Path
import sys
import zipfile
from PIL import Image

H=lambda b:hashlib.sha256(b).hexdigest()
STATES=("heat","coolant")
ROOT="Assets/B0131ThermalVentVfx"

def check(ok,why):
    if not ok:raise ValueError("B0131_VENT_INDEPENDENT_RECEIVER_RED "+why)

def run(folder):
    d=json.loads((folder/"receipt.json").read_bytes())
    check(d["package"]=="B01-31-THERMAL-VENT-VFX-001"
          and d["revision"]=="P17-W12-R1","unexpected physical package")
    check(d["unity_native"]=="NOT_RUN" and d["G2"]=="HOLD",
          "forged Unity G1 or G2")
    frames=d["physical_frames"]
    check(len(frames)==24,"24 actual PNG receipt files")
    actual={}
    for entry in frames:
        s=entry["state"];i=entry["index"]
        check(s in STATES and isinstance(i,int) and 0<=i<12,
              "invalid physical GIF mode/frame")
        check((s,i) not in actual and entry["path"]==f"frames/{s}/vent_{i:02}.png",
              "duplicated frame index or unexpected file location")
        b=(folder/entry["path"]).read_bytes()
        check(H(b)==entry["sha256"] and len(b)==entry["bytes"],
              "actual PNG source SHA/size mismatch")
        with Image.open(io.BytesIO(b)) as im:
            im.load()
            check(im.format=="PNG" and im.mode=="RGBA"
                  and im.size==(256,384),"physical source PNG shape/mode")
            alpha=im.getchannel("A")
            bb=alpha.getbbox()
            check(bb is not None and bb[2]-bb[0]>40 and bb[3]-bb[1]>120
                  and im.getpixel((0,0))[3]==0
                  and im.getpixel((255,383))[3]==0,
                  "real plume silhouette/transparent corners")
        actual[(s,i)]=b
    check(len(actual)==24,"must have 12 source frames per two modes")
    gifs=d["animated_gifs"]
    check(len(gifs)==2 and len(d["preview_strips"])==2,
          "two actual animated loops and contact strips")
    for g in gifs:
        s=g["state"]
        check(s in STATES and g["frames"]==12,"unexpected animated GIF source")
        b=(folder/"previews"/f"{s}-loop.gif").read_bytes()
        check(H(b)==g["sha256"] and len(b)==g["bytes"],"actual GIF bytes")
        with Image.open(io.BytesIO(b)) as im:
            check(im.format=="GIF" and im.n_frames==12,"12 actual physical GIF frames")
            for i in range(12):
                im.seek(i)
                check(im.info.get("duration")==80,"GIF has genuine 80ms frames")
    for x in d["preview_strips"]:
        s=x["state"];b=(folder/"previews"/f"{s}-strip.png").read_bytes()
        check(s in STATES and H(b)==x["sha256"],"physical PNG strip source")
        with Image.open(io.BytesIO(b)) as im:
            im.load()
            check(im.mode=="RGB" and im.size==(3072,384),"actual 12frame PNG strip")
    board=(folder/"previews"/"THERMAL_VENT_2x12_STORYBOARD.png").read_bytes()
    check(H(board)==d["board_sha256"],"actual 24-frame storyboard digest")
    with Image.open(io.BytesIO(board)) as im:
        check(im.size==(3072,836) and im.mode=="RGB","actual review board")
    cfg=(folder/"clip_timing.json").read_bytes()
    check(H(cfg)==d["clip_timing_sha256"],"actual timing source SHA")
    timing=json.loads(cfg)
    check(timing["frame_interval_ms"]==80 and timing["frames"]==12
          and [x["name"] for x in timing["states"]]==list(STATES),
          "real clip sequencing")
    zdata=(folder/"B0131-ThermalVent-UnitySpriteImport.zip").read_bytes()
    check(H(zdata)==d["unity_zip_sha256"]
          and len(zdata)==d["unity_zip_bytes"],"actual Unity Sprite ZIP")
    with zipfile.ZipFile(io.BytesIO(zdata)) as z:
        names=z.namelist()
        check(len(names)==52 and len(set(names))==52 and z.testzip() is None,
              "original physical ZIP count/CRC")
        check(all(p==ROOT+".meta" or p.startswith(ROOT+"/") for p in names),
              "archive modifies foreign Unity Assets")
        check(all(".." not in p.split("/") and "\\" not in p and not p.startswith("/")
                  for p in names),"ZIP traversal")
        check(len(set(p.casefold() for p in names))==52,"casefold import collision")
        metas=[p for p in names if p.endswith(".meta")]
        check(len(metas)==28,"28 physical importer metas")
        guids=[]
        for p in metas:
            txt=z.read(p).decode("utf-8")
            actual_guid=[v[6:].strip() for v in txt.splitlines() if v.startswith("guid: ")]
            check(len(actual_guid)==1 and len(actual_guid[0])==32,
                  "invalid material importer GUID")
            guids.extend(actual_guid)
        check(len(set(guids))==28 and all(set(g)<={*'0123456789abcdef'} for g in guids),
              "duplicated/invalid physical Unity GUID")
        for (s,i),raw in actual.items():
            p=f"{ROOT}/Frames/{s}/vent_{i:02}.png"
            check(p in names and p+".meta" in names and z.read(p)==raw,
                  "Unity import source frame not identical to physically decoded PNG")
    # Mutation controls on actual serialized bytes, not a mocked receiver result.
    check(H(actual[("heat",0)]+b"mutation")!=frames[0]["sha256"],
          "PNG tamper should be RED")
    check(H(zdata+b"mutation")!=d["unity_zip_sha256"],
          "Unity ZIP tamper should be RED")
    print(json.dumps({"status":"PASS","real_rgba_png":len(actual),
                      "actual_gif_loops":len(gifs),"real_review_strips":2,
                      "real_contact_board":1,"physical_zip_members":52,
                      "distinct_unity_guid":len(guids),
                      "physical_unity_zip_sha256":H(zdata),
                      "unity_native":"NOT_RUN"}))

if __name__=="__main__":
    p=argparse.ArgumentParser()
    p.add_argument("--source",required=True)
    a=p.parse_args()
    try:run(Path(a.source))
    except Exception as e:
        print("B0131_THERMAL_ART_RECEIVER_RED "+str(e),file=sys.stderr)
        raise
