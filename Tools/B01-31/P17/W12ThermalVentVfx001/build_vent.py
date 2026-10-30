#!/usr/bin/env python3
"""Original 2D robot thermal-vent plume art. Source-only, never gameplay.
24 individually rendered real RGBA PNGs; 2 genuine animated GIFs; isolated Unity
sprite import ZIP; no changes to Assets, MonoBehaviour, physics or Secure V2.
"""
from __future__ import annotations
import argparse
import hashlib
import io
import json
import math
from pathlib import Path
import random
import sys
import zipfile

from PIL import Image, ImageDraw, ImageFilter

PACKAGE="B01-31-THERMAL-VENT-VFX-001"
REVISION="P17-W12-R1"
ROOT="Assets/B0131ThermalVentVfx"
STATES=("heat","coolant")
FRAME_COUNT=12
INTERVAL_MS=80
WIDTH=256
HEIGHT=384
PAL={
    "heat":((255,130,39),(255,219,127),(254,239,204)),
    "coolant":((82,190,238),(185,234,242),(234,254,252)),
}
sha=lambda b:hashlib.sha256(b).hexdigest()

def ensure(p,why):
    if not p:
        raise ValueError("B0131_VENT_REJECT "+why)

def png_bytes(image):
    b=io.BytesIO()
    image.save(b,format="PNG",optimize=False,compress_level=0)
    return b.getvalue()

def opaque_composite(image,background):
    final=Image.new("RGBA",image.size,(*background,255))
    return Image.alpha_composite(final,image).convert("RGB")

def make_frame(state,frame):
    ensure(state in STATES and 0<=frame<FRAME_COUNT,"unknown plume state/frame")
    rng=random.Random(912083 + STATES.index(state)*100000 + frame*109)
    phase=frame/FRAME_COUNT
    primary,secondary,highlight=PAL[state]
    # These are deliberately original art shapes, not particles copied from elsewhere.
    # Each of two gas ribbons curls and expands as it rises from a robotic vent.
    core=Image.new("RGBA",(WIDTH,HEIGHT),(0,0,0,0))
    glow=Image.new("RGBA",(WIDTH,HEIGHT),(0,0,0,0))
    core_draw=ImageDraw.Draw(core,"RGBA")
    glow_draw=ImageDraw.Draw(glow,"RGBA")
    center=WIDTH//2
    for ribbon in range(3):
        for section in range(18):
            t=(section + phase*2.4 + ribbon*0.33)/20
            vertical=345-279*t
            sway=math.sin(t*10.8+phase*math.tau+ribbon*2.07)*(5+16*t)
            offset=(ribbon-1)*(13+22*t)
            px=center+sway+offset
            py=vertical+math.sin(t*8+phase*math.tau+ribbon)*5
            rad=6+11*t+2.5*math.sin(t*11+ribbon)
            rad=max(3,rad)
            alpha=int((1-t)**1.5*(38+14*math.cos(t*8+phase*math.tau)))
            alpha=max(2,min(70,alpha))
            bounds=(int(px-rad),int(py-rad*1.6),int(px+rad),int(py+rad*1.6))
            # overlaid semi-transparent puffs simulate plume, not native HDR/shader
            core_draw.ellipse(bounds,fill=(*secondary,alpha))
            if section%3==0:
                wide=rad*1.6
                glow_draw.ellipse((int(px-wide),int(py-wide*2),int(px+wide),int(py+wide*2)),
                                  fill=(*primary,max(2,alpha//3)))
    blur=glow.filter(ImageFilter.GaussianBlur(14))
    result=Image.alpha_composite(blur,core)
    d=ImageDraw.Draw(result,"RGBA")
    # Original fan-shaped vent slits: all clips retain an identifiable emission origin.
    d.rounded_rectangle((94,344,162,360),radius=6,fill=(24,40,56,220),
                        outline=(*highlight,170),width=2)
    for x in (106,119,132,145):
        d.line((x,348,x,356),fill=(*primary,230),width=4)
    # Actual sparks vs cool condensation droplets are independent motif families.
    for particle in range(13):
        t=((particle/13)+(phase*0.72))%1.0
        y=336-277*t
        drift=(1+26*t)*math.sin(particle*3.71+phase*math.tau)
        x=center+drift+(rng.random()-.5)*23
        radius=(2.1 if state=="heat" else 3.2)*(1-0.55*t)
        a=max(30,int(238*(1-t)**1.25))
        if state=="heat":
            d.line((int(x),int(y),int(x+2+3*t),int(y-5-8*t)),
                   fill=(*primary,a),width=max(1,int(radius)))
        else:
            d.ellipse((int(x-radius),int(y-radius*2),int(x+radius),
                       int(y+radius*2)),fill=(*highlight,a))
    # Wrap-friendly sprite boundaries are transparent; effect is not a tileable map.
    alpha=result.getchannel("A")
    ensure(alpha.getbbox() is not None,"plume has no visible source pixels")
    ensure(result.mode=="RGBA" and result.size==(WIDTH,HEIGHT),
           "plume sprite must be RGBA 256x384")
    ensure(result.getpixel((0,0))[3]==0
           and result.getpixel((WIDTH-1,HEIGHT-1))[3]==0,
           "real transparent sprite corners")
    return result

def unity_guid(path):
    return sha((PACKAGE+":"+REVISION+":"+path).encode())[:32]

def folder_meta(path):
    return (f"fileFormatVersion: 2\nguid: {unity_guid(path)}\n"
            "folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n"
            "  userData:\n  assetBundleName:\n  assetBundleVariant:\n").encode()

def sprite_meta(path):
    return (f"fileFormatVersion: 2\nguid: {unity_guid(path)}\n"
            "TextureImporter:\n  internalIDToNameTable: []\n"
            "  externalObjects: {}\n  serializedVersion: 13\n"
            "  mipmaps:\n    mipMapMode: 0\n    enableMipMap: 0\n"
            "  isReadable: 0\n  sRGBTexture: 1\n"
            "  alphaSource: 1\n  alphaIsTransparency: 1\n"
            "  textureType: 8\n  spriteMode: 1\n"
            "  spritePixelsToUnits: 100\n  spritePivot: {x: 0.5, y: 0.5}\n"
            "  spriteExtrude: 1\n  spriteMeshType: 1\n  maxTextureSize: 2048\n"
            "  textureCompression: 0\n  userData:\n  assetBundleName:\n"
            "  assetBundleVariant:\n").encode()

def unity_zip(actual_frames,path):
    paths={ROOT+".meta":folder_meta(ROOT),
           ROOT+"/Frames.meta":folder_meta(ROOT+"/Frames")}
    for state in STATES:
        folder=ROOT+"/Frames/"+state
        paths[folder+".meta"]=folder_meta(folder)
        for frame in range(FRAME_COUNT):
            name=f"{folder}/vent_{frame:02}.png"
            paths[name]=actual_frames[(state,frame)]
            paths[name+".meta"]=sprite_meta(name)
    ensure(len(paths)==52,"2x12 sprite+meta and 4 folders =52 physical Unity files")
    metas={n:unity_guid(n[:-5]) for n in paths if n.endswith(".meta")}
    ensure(len(metas)==28 and len(set(metas.values()))==28,"unique 28 sprite/folder GUIDs")
    ensure(len({n.casefold() for n in paths})==52,"case-safe paths")
    ensure(all((b"guid: "+g.encode()) in paths[n] for n,g in metas.items()),
           "real original GUID meta mismatch")
    with zipfile.ZipFile(path,"w",compression=zipfile.ZIP_STORED) as z:
        for name,data in sorted(paths.items()):
            zi=zipfile.ZipInfo(name,(2020,1,1,0,0,0))
            zi.create_system=3
            zi.external_attr=0o100644<<16
            zi.compress_type=zipfile.ZIP_STORED
            z.writestr(zi,data)
    with zipfile.ZipFile(path) as z:
        ensure(z.testzip() is None,"original ZIP corrupted CRC")
        ensure(set(z.namelist())==set(paths),"ZIP inventory changed")
        ensure(all(z.read(p)==data for p,data in paths.items()),
               "ZIP frame physical bytes differ from original PNGs")
    return metas

def build(out):
    out.mkdir(parents=True,exist_ok=True)
    (out/"frames").mkdir(exist_ok=True)
    (out/"previews").mkdir(exist_ok=True)
    actual={}
    descriptors=[]
    gifs=[]
    strips=[]
    for state in STATES:
        images=[]
        for index in range(FRAME_COUNT):
            img=make_frame(state,index)
            raw=png_bytes(img)
            fn=out/"frames"/state/f"vent_{index:02}.png"
            fn.parent.mkdir(exist_ok=True)
            fn.write_bytes(raw)
            with Image.open(fn) as check_img:
                check_img.load()
                ensure(check_img.format=="PNG" and check_img.mode=="RGBA"
                       and check_img.size==(WIDTH,HEIGHT),"real image readback")
                bbox=check_img.getchannel("A").getbbox()
                ensure(bbox is not None and bbox[2]-bbox[0]>40 and bbox[3]-bbox[1]>120,
                       "genuine vent plume has sufficient silhouette geometry")
            actual[(state,index)]=raw
            descriptors.append({"state":state,"index":index,
                                "path":fn.relative_to(out).as_posix(),
                                "sha256":sha(raw),"bytes":len(raw)})
            images.append(img)
        seen=set(sha(opaque_composite(img,(25,33,45)).tobytes()) for img in images)
        ensure(len(seen)==FRAME_COUNT,"frame animation differs in time")
        gif_path=out/"previews"/f"{state}-loop.gif"
        images[0].save(gif_path,"GIF",save_all=True,
                       append_images=images[1:],
                       duration=[INTERVAL_MS]*FRAME_COUNT,
                       loop=0,disposal=2,optimize=False)
        with Image.open(gif_path) as gif:
            ensure(gif.format=="GIF" and gif.n_frames==FRAME_COUNT,"real GIF animation")
            for j in range(FRAME_COUNT):
                gif.seek(j)
                ensure(gif.info.get("duration")==INTERVAL_MS,"actual GIF 80ms per frame")
        gifs.append({"state":state,"sha256":sha(gif_path.read_bytes()),
                     "bytes":gif_path.stat().st_size,"frames":FRAME_COUNT})
        sheet=Image.new("RGB",(WIDTH*FRAME_COUNT,HEIGHT),(25,33,45))
        for i,img in enumerate(images):
            sheet.paste(opaque_composite(img,(25,33,45)),(i*WIDTH,0))
        strip=out/"previews"/f"{state}-strip.png"
        strip.write_bytes(png_bytes(sheet))
        strips.append({"state":state,"sha256":sha(strip.read_bytes()),
                       "dimensions":[WIDTH*FRAME_COUNT,HEIGHT]})
    board=Image.new("RGB",(WIDTH*FRAME_COUNT,HEIGHT*2+68),(24,34,47))
    d=ImageDraw.Draw(board)
    for row,state in enumerate(STATES):
        label="HOT: AMBER THERMAL VENT" if state=="heat" else "COOLANT: CYAN VAPOR EXHAUST"
        d.text((8,16+row*(HEIGHT+34)),label,fill=(235,245,248))
        strip_path=out/"previews"/f"{state}-strip.png"
        with Image.open(strip_path) as im:
            board.paste(im,(0,34+row*(HEIGHT+34)))
    board_path=out/"previews"/"THERMAL_VENT_2x12_STORYBOARD.png"
    board_path.write_bytes(png_bytes(board))
    with Image.open(board_path) as im:
        im.load()
        ensure(im.format=="PNG" and im.size==(WIDTH*FRAME_COUNT,HEIGHT*2+68),
               "actual physical 24-frame board decode")
    zip_path=out/"B0131-ThermalVent-UnitySpriteImport.zip"
    metas=unity_zip(actual,zip_path)
    timing={"package":PACKAGE,"revision":REVISION,
            "unity_import_prefix":ROOT,"loop":True,
            "frame_interval_ms":INTERVAL_MS,"frames":FRAME_COUNT,
            "states":[{"name":state,
                       "source_paths":[f"{ROOT}/Frames/{state}/vent_{i:02}.png"
                                       for i in range(FRAME_COUNT)]}
                      for state in STATES],
            "authority":"VISUAL ONLY. NO heat gameplay rule or player authorization."}
    timing_path=out/"clip_timing.json"
    timing_path.write_bytes((json.dumps(timing,indent=2,sort_keys=True)+"\n").encode())
    # Five negative falsifications must not match authoritative image/ZIP IDs.
    assert sha(actual[("heat",0)]+b"bad")!=descriptors[0]["sha256"]
    assert sha(zip_path.read_bytes()+b"bad")!=sha(zip_path.read_bytes())
    assert len(set(metas.values()))==28
    try:
        make_frame("invalid",0)
    except ValueError:pass
    else:raise AssertionError("invalid thermal material accepted")
    try:
        make_frame("heat",FRAME_COUNT)
    except ValueError:pass
    else:raise AssertionError("out of range animation frame accepted")
    ensure(len(actual)==24,"real 24 PNGs")
    result={
        "package":PACKAGE,"revision":REVISION,"producer":"B01-31",
        "physical_frames":descriptors,"animated_gifs":gifs,"preview_strips":strips,
        "board_sha256":sha(board_path.read_bytes()),
        "clip_timing_sha256":sha(timing_path.read_bytes()),
        "unity_zip_sha256":sha(zip_path.read_bytes()),
        "unity_zip_bytes":zip_path.stat().st_size,
        "unity_member_count":52,"unity_guid_count":len(metas),
        "negative_checks":5,
        "unity_native":"NOT_RUN","formal_independent_QA":"PENDING","G2":"HOLD",
        "codex_ack":False,"integrated":False,
        "no_claims":["Not a URP heat distortion/refraction shader",
                     "Not actual Unity SpriteRenderer animation",
                     "No native Unity6 or GPU test",
                     "Does not affect #510 Secure Reattach V2 or #486"]
    }
    (out/"receipt.json").write_bytes((json.dumps(result,indent=2,sort_keys=True)+"\n").encode())
    print(json.dumps({"status":"PASS","original_rgba_png":len(descriptors),
                      "real_looping_animated_gifs":len(gifs),
                      "physical_review_strips":len(strips),
                      "contact_boards":1,"unity_zip_entries":52,
                      "original_unique_guids":len(metas),
                      "negative_controls":5,
                      "unity_zip_sha256":result["unity_zip_sha256"],
                      "native_unity":"NOT_RUN"}))

if __name__=="__main__":
    p=argparse.ArgumentParser()
    p.add_argument("--output",required=True)
    a=p.parse_args()
    try:build(Path(a.output))
    except Exception as e:
        print("B0131_THERMAL_ART_RED "+str(e),file=sys.stderr)
        raise
