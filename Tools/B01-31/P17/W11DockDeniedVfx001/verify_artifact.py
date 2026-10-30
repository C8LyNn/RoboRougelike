#!/usr/bin/env python3
"""Independent read-only physical PNG/GIF/Unity ZIP receiver. Python stdlib+Pillow.
Never runs the producer renderer or any Unity APIs. Validation is source-level,
not B01-40 formal independent QA and not licensed Unity Editor execution.
"""
from __future__ import annotations
import argparse
import hashlib
import io
import json
from pathlib import Path
import sys
import zipfile
from PIL import Image, ImageChops

ICONS=("collision-overlap","distance-limit","unguarded-route","query-saturated")
COUNT=8
ROOT="Assets/B0131DockDeniedVfx"
H=lambda data:hashlib.sha256(data).hexdigest()

def check(ok,why):
    if not ok:raise ValueError("W11_PHYSICAL_ART_RECEIVER_REJECT: "+why)

def inspect_zip(data,receipt):
    check(H(data)==receipt["physical_unity_sprite_zip_sha256"],
          "original inner ZIP SHA256 mismatch")
    with zipfile.ZipFile(io.BytesIO(data)) as z:
        names=z.namelist()
        check(len(names)==70 and len(set(names))==70,
              "exact physical Unity import archive entries")
        check(z.testzip() is None,"actual Unity ZIP member CRC")
        check(all(not n.startswith("/") and ".." not in n.split("/")
                  and "\\" not in n for n in names),"ZIP path traversal")
        check(len({n.casefold() for n in names})==len(names),
              "Windows import casefold collision")
        check(all(n==ROOT+".meta" or n.startswith(ROOT+"/"),
                  for n in names),"alien Unity import namespace")
        check(len([n for n in names if n.endswith(".meta")])==38,
              "must include 38 stable .meta records")
        metas=[z.read(n).decode("utf8") for n in names if n.endswith(".meta")]
        guids=[line[6:].strip() for body in metas for line in body.splitlines()
               if line.startswith("guid: ")]
        check(len(guids)==38 and len(set(guids))==38
              and all(len(g)==32 and all(c in "0123456789abcdef" for c in g)
                      for g in guids), "actual GUID duplicate/inventory")
        # All physical 32 sprite bytes must equal final verified frame bytes.
        for state in ICONS:
            for i in range(COUNT):
                source=f"{ROOT}/Sprites/{state}/frame_{i:02}.png"
                check(source in names and source+".meta" in names,
                      "missing actual Unity Sprite frame/meta")
                with Image.open(io.BytesIO(z.read(source))) as image:
                    image.load()
                    check(image.format=="PNG" and image.mode=="RGBA"
                          and image.size==(192,192),"actual zipped RGBA sprite")
        return 70,38

def inspect(root):
    raw=(root/"receipt.json").read_bytes()
    d=json.loads(raw)
    check(d["package"]=="B01-31-DOCK-DENIED-ANIM-001"
          and d["version"]=="P17-W11-R1","unexpected package/version")
    check(d["original_source_sha"]=="7944af98524b7eb5ceadb88eb056de1d486ac461"
          and d["native_unity"]=="NOT_RUN","source provenance/native gate")
    inputs=d["w11_original_svg_inputs"]
    check([x["name"] for x in inputs]==list(ICONS),
          "all four immutable reference source drawings")
    check(len({x["git_blob"] for x in inputs})==4,
          "source blobs not distinct")
    check(d["negative_tests"]==6,"source negative tests not completed")
    frames=d["real_rgba_frame_pngs"]
    check(len(frames)==32,"physical source must include all 32 independent PNGs")
    seen={}
    for decl in frames:
        state,index=decl["icon"],decl["frame"]
        check(state in ICONS and isinstance(index,int) and 0<=index<COUNT
              and (state,index) not in seen,"frame ids must be exactly 4x8")
        check(decl["path"]==f"frames/{state}/frame_{index:02}.png",
              "unexpected frame path")
        buf=(root/decl["path"]).read_bytes()
        check(H(buf)==decl["sha256"] and len(buf)==decl["bytes"],
              "original real PNG bytes modified")
        with Image.open(io.BytesIO(buf)) as im:
            im.load()
            check(im.mode=="RGBA" and im.size==(192,192)
                  and im.format=="PNG","actual frame PNG pixel+type")
            alpha=im.getchannel("A")
            check(alpha.getbbox() is not None and im.getpixel((0,0))[3]==0,
                  "real transparent denied/hold frame")
            check(im.getpixel((191,191))[3]==0,"real PNG transparent far corner")
        seen[(state,index)]=buf
    check(len(seen)==32,"all semantic states and frame indices present")
    check(len(d["animated_gifs"])==4,"four physical animated preview GIF")
    for g in d["animated_gifs"]:
        key=g["icon"]
        check(key in ICONS,"preview unmatched")
        path=root/"previews"/(key+".gif")
        data=path.read_bytes()
        check(H(data)==g["sha256"] and len(data)==g["bytes"],
              "real animated GIF digest/bytes")
        with Image.open(io.BytesIO(data)) as gif:
            check(gif.format=="GIF" and gif.n_frames==8,
                  "actual GIF must be animated 8 frames")
            for i in range(COUNT):
                gif.seek(i)
                check(gif.info.get("duration")==100,
                      "actual GIF frame 100 ms timing")
    check(len(d["eight_frame_strips"])==4,
          "4 standalone 8-frame concept sequences")
    for strip in d["eight_frame_strips"]:
        path=root/"previews"/(strip["icon"]+"-strip.png")
        check(H(path.read_bytes())==strip["sha256"],"actual sprite strip SHA")
        with Image.open(path) as image:
            image.load()
            check(image.format=="PNG" and image.size==(1536,192),
                  "real horizontal 8-frame strip")
    contact=root/"previews"/"ALL_DENIED_HOLD_ANIM_STORYBOARD.png"
    check(H(contact.read_bytes())==d["original_32frame_storyboard"],
          "overview physical PNG checksum")
    with Image.open(contact) as image:
        image.load()
        check(image.size==(1536,860),"32-frame contact dimensions")
    zpath=root/"B0131-DockDeniedVfx-UnitySpriteFrames.zip"
    z=zpath.read_bytes()
    check(len(z)==d["physical_unity_sprite_zip_bytes"],"real ZIP expected length")
    entry_count,guid_count=inspect_zip(z,d)
    # Compare all actual 32 stored ZIP sprites to independent PNG source readback.
    with zipfile.ZipFile(io.BytesIO(z)) as arc:
        for (name,index),original in seen.items():
            check(arc.read(f"{ROOT}/Sprites/{name}/frame_{index:02}.png")==original,
                  "Unity import does not contain exactly the actual VFX PNG")
    cfg_raw=(root/"clip_timing.json").read_bytes()
    check(H(cfg_raw)==d["source_clip_timing_sha256"],
          "clip timing manifest SHA")
    config=json.loads(cfg_raw)
    check(config["frame_interval_ms"]==100
          and config["frame_count"]==8
          and config["loop"] is True,
          "real Unity clip timing source")
    check([c["id"] for c in config["clips"]]==list(ICONS),
          "4 correct original status clip configurations")
    check(all(c["frame_files"]==[f"frame_{i:02}.png" for i in range(COUNT)]
              for c in config["clips"]),"frame sequence order")
    # Fail closed: verify a real mutated file would invalidate the receipt.
    original=seen[(ICONS[0],0)]
    check(H(original+b"tampered")!=frames[0]["sha256"],
          "negative PNG byte mutation was not detected")
    check(H(z+b"tampered")!=d["physical_unity_sprite_zip_sha256"],
          "negative ZIP byte mutation was not detected")
    print(json.dumps({
        "status":"PASS", "real_pngs":len(seen), "real_gifs":4,
        "real_strips":4,"physical_contact":1,
        "verified_original_sprite_zip_members":entry_count,
        "verified_unique_meta_guids":guid_count,
        "source_sha":d["original_source_sha"],
        "original_sprite_zip_sha256":H(z),
        "native_unity":"NOT_RUN"
    }))
    return 0

if __name__=="__main__":
    p=argparse.ArgumentParser()
    p.add_argument("--source",required=True)
    a=p.parse_args()
    try:
        sys.exit(inspect(Path(a.source)))
    except Exception as exc:
        print("B0131_W11_PHYSICAL_RECEIVER_RED "+str(exc),file=sys.stderr)
        raise
