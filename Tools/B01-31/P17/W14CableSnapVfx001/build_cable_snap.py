#!/usr/bin/env python3
"""Original B01-31 three-phase severed robot cable visual VFX package.
Pure procedural RGBA art. No Rigidbody, transaction, Save/Load or API mutation.
Outputs real PNG sprites, real GIF previews and standalone Unity Sprite ZIP.
"""
from __future__ import annotations

import argparse
import hashlib
import io
import json
import math
from pathlib import Path
import sys
import zipfile
from PIL import Image, ImageDraw, ImageFont, ImageChops

PACKAGE = "B01-31-CABLE-SNAP-VFX-001"
VERSION = "P17-W14-R1"
SOURCE_OWNER = "B01-31"
STATES = ("fracture", "discharge", "cooldown")
FRAME_COUNT = 12
FRAME_DURATION_MS = 80
SIZE = 192
ROOT = "Assets/B0131CableSnapVfx"
DARK = (20, 30, 44)
LIGHT = (208, 220, 230)
COLORS = {
    "fracture": (255, 161, 98),
    "discharge": (114, 213, 255),
    "cooldown": (172, 193, 219),
}
H = lambda b: hashlib.sha256(b).hexdigest()

def require(value, reason):
    if not value:
        raise ValueError("B0131_CABLE_SNAP_FAIL_CLOSED: " + reason)

def bit(seed: bytes, index: int) -> float:
    return hashlib.sha256(seed + index.to_bytes(4, "big")).digest()[0] / 255.0

def check_state(state: str, frame: int):
    require(state in STATES, "unknown animation phase")
    require(isinstance(frame, int) and 0 <= frame < FRAME_COUNT, "invalid frame index")

def img_to_png(image: Image.Image) -> bytes:
    buf = io.BytesIO()
    image.save(buf, format="PNG", optimize=False, compress_level=0)
    return buf.getvalue()

def draw_wire(d, frame, state):
    # Cable halves physically separated: left x<=83, right x>=119.
    dark_jacket = (42, 56, 77, 254)
    outline = (8, 19, 33, 255)
    copper = (237, 168, 88, 255)
    for pts in [
        [(12, 99), (37, 98), (65, 99), (81, 101)],
        [(180, 99), (155, 99), (132, 101), (121, 101)],
    ]:
        d.line(pts, fill=outline, width=22, joint="curve")
        d.line(pts, fill=dark_jacket, width=16, joint="curve")
        d.line(pts, fill=(103, 130, 150, 240), width=3, joint="curve")
    # Torn insulation edge with separated copper tips, no joined cable.
    d.line([(75, 92), (79, 94), (81, 101)], fill=copper, width=5)
    d.line([(121, 101), (126, 96), (129, 93)], fill=copper, width=5)
    d.ellipse((78, 97, 85, 104), fill=(255, 218, 145, 255))
    d.ellipse((116, 97, 123, 104), fill=(255, 218, 145, 255))
    for x in (50, 149):
        d.line((x, 87, x, 111), fill=(12, 24, 37, 230), width=5)
        d.line((x + 3, 87, x + 3, 111), fill=(128, 147, 165, 220), width=2)

def draw_frame(state: str, frame: int) -> Image.Image:
    check_state(state, frame)
    im = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    d = ImageDraw.Draw(im, "RGBA")
    seed = (PACKAGE + ":" + state).encode()
    progress = frame / (FRAME_COUNT - 1)
    col = COLORS[state]
    cx, cy = 101, 100

    if state == "fracture":
        # Shattered insulation expansion: diminishing rays and sharp splinters.
        radius = 18 + frame * 4
        opacity = max(38, 230 - frame * 15)
        d.arc((cx-radius, cy-radius, cx+radius, cy+radius),
              12, 161, fill=(*col, opacity), width=4)
        d.arc((cx-radius, cy-radius, cx+radius, cy+radius),
              194, 341, fill=(*col, opacity), width=4)
        for j in range(8):
            angle = 2 * math.pi * (j/8 + 0.045)
            start = 19 + frame * 1.4 + j % 3
            stop = start + 9 + j % 4
            a = (cx+math.cos(angle)*start, cy+math.sin(angle)*start)
            b = (cx+math.cos(angle)*stop, cy+math.sin(angle)*stop)
            d.line((a[0], a[1], b[0], b[1]), fill=(*col, max(45, 240-frame*15)), width=3)
        for j in range(7):
            ang = 2*math.pi*(j/7 + bit(seed,j)*0.15)
            dis = 16 + frame*3.2 + bit(seed,j+20)*12
            x, y = cx+math.cos(ang)*dis, cy+math.sin(ang)*dis
            half = 2 + int(bit(seed,j+45)*3)
            d.polygon([(x,y-half), (x+half+2,y), (x,y+half+2), (x-half,y)],
                      fill=(255, 214, 155, max(52,240-frame*13)))
    elif state == "discharge":
        # Nonphysical electric flash across broken gap; NEVER a reconnect action.
        # Four animated blue-white forks; central high-energy branches persist.
        power = 0.4 + 0.6 * (1 - abs(math.sin((frame+1)*0.65)))
        for k in range(3):
            yoff = -12 + k*11
            points=[(81,100+yoff)]
            for point in range(1,5):
                xx=81+point*9
                dy=(bit(seed,(frame+1)*100+k*10+point)-0.5)*23
                points.append((xx,100+yoff+dy))
            points.append((119,100+yoff+k-1))
            d.line(points, fill=(38,134,235,int(170*power)), width=8, joint="curve")
            d.line(points, fill=(198,246,255,int(250*power)), width=3, joint="curve")
        for j in range(8):
            a=bit(seed,frame*100+j)*math.tau
            dis=20+bit(seed,frame*100+j+18)*43
            x,y=cx+math.cos(a)*dis,cy+math.sin(a)*dis
            radius=1+(j%3)
            d.ellipse((x-radius,y-radius,x+radius,y+radius),
                      fill=(191,237,255,175+int(80*power)))
        d.arc((58,55,144,145),int(frame*24),int(frame*24+48),
              fill=(99,198,248,130+int(90*power)),width=3)
    else:
        # Post-sever cooldown: distinct waning smoke wisps and embers.
        for j in range(4):
            x=86+j*11+3*math.sin((frame+j)*0.55)
            y=90-(frame*2.8+j*8)
            opacity=max(20,160-frame*9-j*7)
            for off in range(3):
                a=(x-3+off*4, y+off*7)
                b=(x+1+off*3, y-7+off*6)
                d.line((a[0],a[1],b[0],b[1]),fill=(*col,opacity),width=4)
        for j in range(6):
            a=(j/6)*math.tau
            distance=15+frame*1.6+(j%3)*4
            x,y=cx+math.cos(a)*distance,cy+math.sin(a)*distance
            opacity=max(22,175-frame*11)
            d.ellipse((x-2,y-2,x+2,y+2),fill=(255,174,105,opacity))
        rad=32+frame*2
        d.arc((cx-rad,cy-rad,cx+rad,cy+rad),60,125,
              fill=(*col,max(20,120-frame*8)),width=3)

    draw_wire(d,frame,state)
    # A non-green cut marker distinguishes severance from successful reattach.
    d.line((97,79,105,88), fill=(255,156,120,170), width=3)
    d.line((105,79,97,88), fill=(255,156,120,170), width=3)
    require(im.getpixel((0,0))[3]==0 and im.getpixel((191,191))[3]==0,
            "transparent corners required")
    require(im.getchannel("A").getbbox() is not None, "empty PNG state")
    return im

def meta_guid(asset_path: str) -> str:
    return H((PACKAGE+":"+VERSION+":"+asset_path).encode())[:32]

def folder_meta(path):
    return (f"fileFormatVersion: 2\nguid: {meta_guid(path)}\n"
            "folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n"
            "  userData:\n  assetBundleName:\n  assetBundleVariant:\n").encode()

def sprite_meta(path):
    return (f"fileFormatVersion: 2\nguid: {meta_guid(path)}\n"
            "TextureImporter:\n  internalIDToNameTable: []\n"
            "  externalObjects: {}\n  serializedVersion: 13\n"
            "  mipmaps:\n    mipMapMode: 0\n    enableMipMap: 0\n"
            "  isReadable: 0\n  sRGBTexture: 1\n"
            "  alphaSource: 1\n  alphaIsTransparency: 1\n"
            "  textureType: 8\n  spriteMode: 1\n"
            "  spritePixelsToUnits: 100\n"
            "  spritePivot: {x: 0.5, y: 0.5}\n"
            "  spriteExtrude: 1\n  spriteMeshType: 1\n"
            "  maxTextureSize: 2048\n  textureCompression: 0\n"
            "  userData:\n  assetBundleName:\n  assetBundleVariant:\n").encode()

def assert_zip(files):
    require(len(files)==77, "36 sprites+36 sprite meta+5 folder meta")
    require(len(set(p.casefold() for p in files))==77, "duplicate/casefold path")
    require(all(p==ROOT+".meta" or p.startswith(ROOT+"/") for p in files),
            "foreign Unity import path")
    require(all(not p.startswith("/") and "\\" not in p and ".." not in p.split("/")
                for p in files), "ZIP path traversal")
    guid_list=[]
    for p,data in files.items():
        if p.endswith(".meta"):
            g=meta_guid(p[:-5])
            require(("guid: "+g).encode() in data, "wrong Unity .meta GUID")
            guid_list.append(g)
            if p.endswith(".png.meta"):
                require(b"textureType: 8" in data and b"alphaIsTransparency: 1" in data,
                        "not Unity Sprite Single alpha")
    require(len(guid_list)==41 and len(set(guid_list))==41,
            "41 unique deterministic metadata GUIDs")
    return len(guid_list)

def negative_controls():
    fails=0
    for state,frame in [
        ("fracture",-1),("cooldown",12),("invalid",0),
        ("fracture",2.0),("",0),("discharge",99)
    ]:
        try: draw_frame(state,frame)
        except ValueError: fails+=1
        else: raise AssertionError("invalid animation state/frame passed")
    require(fails==6,"six strict phase/index negatives")
    return fails

def make_preview(frames,background):
    cell=SIZE
    gap=30
    img=Image.new("RGB",(12*cell,3*(cell+gap)+50),background)
    d=ImageDraw.Draw(img)
    f=ImageFont.load_default()
    title=(242,244,248) if background==DARK else (25,40,55)
    for r,state in enumerate(STATES):
        d.text((8,r*(cell+gap)+7),state.upper()+" — SEVERED CABLE / 80 MS EACH",
               fill=title,font=f)
        for i,fr in enumerate(frames[state]):
            back=Image.new("RGBA",(SIZE,SIZE),(*background,255))
            back=Image.alpha_composite(back,fr)
            img.paste(back.convert("RGB"),(i*SIZE,r*(SIZE+gap)+gap))
    return img

def produce(output: Path):
    output.mkdir(parents=True,exist_ok=True)
    (output/"sprites").mkdir(exist_ok=True)
    (output/"previews").mkdir(exist_ok=True)
    meta_files={ROOT+".meta":folder_meta(ROOT),
                ROOT+"/Sprites.meta":folder_meta(ROOT+"/Sprites")}
    frames={}
    sprites=[]
    gifs=[]
    for state in STATES:
        meta_files[f"{ROOT}/Sprites/{state}.meta"] = folder_meta(f"{ROOT}/Sprites/{state}")
        series=[]
        composites=[]
        for i in range(FRAME_COUNT):
            img=draw_frame(state,i)
            raw=img_to_png(img)
            with Image.open(io.BytesIO(raw)) as imp:
                imp.load()
                require(imp.format=="PNG" and imp.mode=="RGBA" and
                        imp.size==(SIZE,SIZE), "physical sprite raster output")
            p=output/"sprites"/state/(f"frame_{i:02d}.png")
            p.parent.mkdir(parents=True,exist_ok=True)
            p.write_bytes(raw)
            archive_name=f"{ROOT}/Sprites/{state}/frame_{i:02d}.png"
            meta_files[archive_name]=raw
            meta_files[archive_name+".meta"]=sprite_meta(archive_name)
            sprites.append({"state":state,"frame":i,"path":p.relative_to(output).as_posix(),
                            "sha256":H(raw),"bytes":len(raw)})
            series.append(img)
            bg=Image.new("RGBA",img.size,(*DARK,255))
            composites.append(Image.alpha_composite(bg,img).convert("RGB"))
        require(len({H(x.tobytes()) for x in composites})==FRAME_COUNT,
                "all 12 frames in each phase actually visually differ")
        path=output/"previews"/(state+".gif")
        # GIF previews are reference views, never to be confused with Unity rendering.
        series[0].save(path,format="GIF",save_all=True,append_images=series[1:],
                       duration=[FRAME_DURATION_MS]*FRAME_COUNT,loop=0,
                       optimize=False,disposal=2)
        with Image.open(path) as test:
            require(test.format=="GIF" and test.n_frames==FRAME_COUNT,
                    "animated GIF contains all 12 physical source frames")
            durations=[]
            for i in range(FRAME_COUNT):
                test.seek(i)
                durations.append(test.info.get("duration"))
            require(durations==[FRAME_DURATION_MS]*FRAME_COUNT,
                    "actual preview GIF timing 12*80ms")
        gifs.append({"state":state,"sha256":H(path.read_bytes()),
                     "bytes":path.stat().st_size,"frames":FRAME_COUNT})
        frames[state]=series
    boards=[]
    for mode,bg in (("dark",DARK),("light",LIGHT)):
        art=make_preview(frames,bg)
        target=output/"previews"/(f"ALL_PHASES_{mode.upper()}_12_FRAME_STORYBOARD.png")
        target.write_bytes(img_to_png(art))
        with Image.open(target) as verify:
            verify.load()
            require(verify.format=="PNG" and verify.mode=="RGB" and
                    verify.size==(2304,716),
                    "actual full three-phase contact sheet "+mode)
        boards.append({"mode":mode,"path":target.relative_to(output).as_posix(),
                       "sha256":H(target.read_bytes()),"bytes":target.stat().st_size})
    guid_count=assert_zip(meta_files)
    zip_out=output/"B0131-CableSnap-UnitySpriteImport.zip"
    with zipfile.ZipFile(zip_out,"w",compression=zipfile.ZIP_STORED) as zipf:
        for path,data in sorted(meta_files.items()):
            info=zipfile.ZipInfo(path,(2020,1,1,0,0,0))
            info.create_system=3
            info.external_attr=0o100644<<16
            info.compress_type=zipfile.ZIP_STORED
            zipf.writestr(info,data)
    with zipfile.ZipFile(zip_out) as zipf:
        require(zipf.testzip() is None and set(zipf.namelist())==set(meta_files),
                "physical ZIP CRC and exact 77-member paths")
        require(all(zipf.read(p)==b for p,b in meta_files.items()),
                "physical sprite/metadata ZIP-to-input byte equality")
    negatives=negative_controls()
    timing={
        "package":PACKAGE,"version":VERSION,"frames_per_phase":FRAME_COUNT,
        "frame_interval_ms":FRAME_DURATION_MS,"loop":False,
        "phases":[{"phase":state,"semantic":"non-authoritative visual cue only",
                   "png_frames":[f"{ROOT}/Sprites/{state}/frame_{i:02d}.png"
                                 for i in range(FRAME_COUNT)]} for state in STATES],
        "sequence_hint":"fracture -> discharge -> cooldown, each 960ms, one-shot; host may change",
        "actual_unity_controller":"NOT_IMPLEMENTED",
        "gameplay_trigger":"NOT_IMPLEMENTED",
        "native_unity":"NOT_RUN"
    }
    (output/"clip_timing.json").write_bytes((json.dumps(timing,indent=2,sort_keys=True)+"\n").encode())
    receipt={
        "package_id":PACKAGE,"version":VERSION,"source_owner":SOURCE_OWNER,
        "source_art":"ORIGINAL programmatic authored cable severance",
        "phases":list(STATES),"sprites":sprites,"gifs":gifs,
        "actual_2_state_storyboards":boards,
        "clip_timing_sha256":H((output/"clip_timing.json").read_bytes()),
        "unity_sprite_zip_sha256":H(zip_out.read_bytes()),
        "unity_sprite_zip_bytes":zip_out.stat().st_size,
        "unity_sprite_zip_members":len(meta_files),
        "unique_unity_guids":guid_count,
        "all_frames_unique_per_phase":True,
        "negative_controls_rejected":negatives,
        "status":"PRODUCER_ART_FORMAT_ONLY",
        "allowed_UI_states":"VISUAL FX ONLY; not proof of detach, reattach, save, rollback",
        "Unity6000.3.14f1_import":"NOT_RUN",
        "true_PhysX":"NOT_RUN",
        "real_save_load":"NOT_RUN",
        "selected_reattach_pr":510,
        "selected_reattach_sha":"d94400d95d13db7f27e1533df05c9292d5380431",
        "codex_ACK":False,"integrated":False,"G2":"HOLD"
    }
    (output/"receipt.json").write_bytes((json.dumps(receipt,indent=2,sort_keys=True)+"\n").encode())
    print(json.dumps({
        "status":"PASS","original_192px_rgba_sprites":len(sprites),
        "actual_12x80ms_animated_gifs":len(gifs),
        "actual_3phase_storyboards":len(boards),
        "real_unity_zip_members":len(meta_files),
        "unique_guid":guid_count,"rejected_inputs":negatives,
        "inner_zip_sha256":receipt["unity_sprite_zip_sha256"],
        "actual_unity":"NOT_RUN","gameplay_triggers":"NOT_IMPLEMENTED"
    }))
    return 0

if __name__=="__main__":
    ap=argparse.ArgumentParser()
    ap.add_argument("--output",required=True)
    args=ap.parse_args()
    try:
        sys.exit(produce(Path(args.output)))
    except Exception as e:
        print("W14_CABLE_SNAP_ART_RED "+type(e).__name__+": "+str(e),file=sys.stderr)
        raise
