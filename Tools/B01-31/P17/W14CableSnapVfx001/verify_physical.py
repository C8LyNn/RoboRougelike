#!/usr/bin/env python3
"""Independent physical asset receiver, no producer import or Unity Editor.
Decodes actual sprite pixels/GIF and verifies 77 real Unity ZIP members.
"""
from __future__ import annotations
import argparse
import hashlib
import io
import json
from pathlib import Path
import re
import sys
import zipfile
from PIL import Image, ImageChops

STATES=("fracture","discharge","cooldown")
ROOT="Assets/B0131CableSnapVfx"
H=lambda b:hashlib.sha256(b).hexdigest()

def check(ok,reason):
    if not ok: raise ValueError("W14_CABLE_SNAP_PHYSICAL_RECEIVER_RED "+reason)

def verify(root:Path):
    rec=json.loads((root/"receipt.json").read_bytes())
    check(rec["package_id"]=="B01-31-CABLE-SNAP-VFX-001" and
          rec["version"]=="P17-W14-R1","wrong original package/version")
    check(rec["phases"]==list(STATES) and rec["source_art"].startswith("ORIGINAL"),
          "three distinct original phase/author claims")
    check(rec["Unity6000.3.14f1_import"]=="NOT_RUN" and
          rec["true_PhysX"]=="NOT_RUN" and
          rec["real_save_load"]=="NOT_RUN","do not claim native runtime")
    check(rec["selected_reattach_pr"]==510 and
          rec["selected_reattach_sha"]=="d94400d95d13db7f27e1533df05c9292d5380431",
          "frozen selected reattach owner identification")
    frames=rec["sprites"]
    check(len(frames)==36 and rec["all_frames_unique_per_phase"],
          "36 required source RGBAs")
    originals={}
    for item in frames:
        state,idx=item["state"],item["frame"]
        check(state in STATES and isinstance(idx,int) and 0<=idx<12
              and (state,idx) not in originals,
              "nonunique/unknown actual sprite path/phase")
        check(item["path"]==f"sprites/{state}/frame_{idx:02d}.png",
              "potential path escape or sprite mismatch")
        raw=(root/item["path"]).read_bytes()
        check(H(raw)==item["sha256"] and len(raw)==item["bytes"],
              "real PNG source SHA / exact byte count")
        with Image.open(io.BytesIO(raw)) as im:
            im.load()
            check(im.format=="PNG" and im.mode=="RGBA" and im.size==(192,192),
                  "actual source Sprite Single original RGBA 192")
            check(im.getpixel((0,0))[3]==0 and
                  im.getpixel((191,191))[3]==0 and
                  im.getchannel("A").getbbox() is not None,
                  "real transparent nonempty sprite")
            # Pixel decoder checks physical halves survive across all frames.
            # Intentional sparks can cross the gap, but NO physical cable bridges.
            alpha=im.getchannel("A")
            left=max(alpha.getpixel((x,99)) for x in range(55,75))
            right=max(alpha.getpixel((x,99)) for x in range(143,161))
            check(left>245 and right>245,
                  "severed cable visual halves absent in real raster")
        originals[(state,idx)]=raw
    check(len(originals)==36, "all 36 physical PNG asset ids must appear")
    for state in STATES:
        check(len({H(originals[(state,idx)]) for idx in range(12)})==12,
              "actual real 12 timeframes per original VFX phase")
    check(len(rec["gifs"])==3,"three original looping preview videos")
    for item in rec["gifs"]:
        name=item["state"]
        check(name in STATES and item["frames"]==12, "GIF phase mismatch")
        data=(root/"previews"/(name+".gif")).read_bytes()
        check(H(data)==item["sha256"] and len(data)==item["bytes"],
              "real animated GIF binary bytes do not match receipt")
        with Image.open(io.BytesIO(data)) as gif:
            check(gif.format=="GIF" and gif.n_frames==12,
                  "real GIF 12-frame animation frame count")
            for idx in range(12):
                gif.seek(idx)
                check(gif.info.get("duration")==80,
                      "real animated GIF 12x80ms runtime timing")
    check(len(rec["actual_2_state_storyboards"])==2,
          "both real light/dark original contact art")
    for item in rec["actual_2_state_storyboards"]:
        name=item["mode"]
        check(name in ("dark","light")
              and item["path"]==f"previews/ALL_PHASES_{name.upper()}_12_FRAME_STORYBOARD.png",
              "board naming and independent theme")
        data=(root/item["path"]).read_bytes()
        check(H(data)==item["sha256"] and len(data)==item["bytes"],
              "real storyboard original byte receipt")
        with Image.open(io.BytesIO(data)) as im:
            im.load()
            check(im.format=="PNG" and im.mode=="RGB"
                  and im.size==(2304,716), "real 3×12 storyboard pixel readback")
    clip=(root/"clip_timing.json").read_bytes()
    check(H(clip)==rec["clip_timing_sha256"],"source phase timing hashes")
    conf=json.loads(clip)
    check(conf["frames_per_phase"]==12 and conf["frame_interval_ms"]==80
          and conf["loop"] is False, "real source event timing 960ms, non-loop")
    check([p["phase"] for p in conf["phases"]]==list(STATES),"phase order")
    for p in conf["phases"]:
        check(p["png_frames"]==
              [f"{ROOT}/Sprites/{p['phase']}/frame_{i:02d}.png" for i in range(12)],
              "ordered Unity path naming and no foreign asset")
    raw_zip=(root/"B0131-CableSnap-UnitySpriteImport.zip").read_bytes()
    check(H(raw_zip)==rec["unity_sprite_zip_sha256"] and
          len(raw_zip)==rec["unity_sprite_zip_bytes"],"real zip digest/bytes")
    with zipfile.ZipFile(io.BytesIO(raw_zip)) as z:
        names=z.namelist()
        check(z.testzip() is None and len(names)==77
              and len(set(names))==77 and len({p.casefold() for p in names})==77,
              "real 77 entries, no CRC/case collision")
        check(all(p==ROOT+".meta" or p.startswith(ROOT+"/") for p in names),
              "no other source owner canonical Assets paths")
        check(all(not p.startswith("/") and ".." not in p.split("/")
                  and "\\" not in p for p in names),
              "ZIP traversal")
        metanames=[p for p in names if p.endswith(".meta")]
        check(len(metanames)==41 and rec["unique_unity_guids"]==41,
              "36 sprite meta and five folder meta files")
        guids=[]
        for p in metanames:
            meta=z.read(p).decode("utf-8")
            m=re.search(r"(?m)^guid: ([a-f0-9]{32})\s*$",meta)
            check(m is not None,"original Unity .meta GUID")
            guids.append(m.group(1))
            if p.endswith(".png.meta"):
                check("textureType: 8" in meta and "spriteMode: 1" in meta and
                      "alphaIsTransparency: 1" in meta,
                      "actual RGBA Unity Single Sprite importer metadata")
        check(len(set(guids))==41,"source GUID duplicates")
        for (state,i),raw in originals.items():
            p=f"{ROOT}/Sprites/{state}/frame_{i:02d}.png"
            check(p in names and p+".meta" in names and z.read(p)==raw,
                  "real Unity ZIP Sprite raw PNG bytes not identical")
    check(rec["negative_controls_rejected"]==6,
          "producer six negative invalid phase/frame checks")
    check(H(raw_zip+b"\0")!=rec["unity_sprite_zip_sha256"],
          "ZIP corrupted bytes must be rejected")
    check(H(originals[("fracture",0)]+b"\0")!=frames[0]["sha256"],
          "PNG tampered bytes must be rejected")
    print(json.dumps({"status":"PASS","actual_source_pngs":len(originals),
          "actual_12x80ms_animated_GIFs":3,"actual_dark_light_storyboards":2,
          "zip_members":77,"original_unique_unity_guids":41,
          "zip_sha256":H(raw_zip),"real_Unity":"NOT_RUN"}))
    return 0

if __name__=="__main__":
    p=argparse.ArgumentParser()
    p.add_argument("--source",required=True)
    a=p.parse_args()
    try:sys.exit(verify(Path(a.source)))
    except Exception as err:
        print("W14_CABLE_SNAP_INDEPENDENT_SOURCE_RECEIVER_FAIL "+
              type(err).__name__+": "+str(err),file=sys.stderr)
        raise
