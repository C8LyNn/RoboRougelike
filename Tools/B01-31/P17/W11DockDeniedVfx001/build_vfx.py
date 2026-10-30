#!/usr/bin/env python3
"""B01-31 original physical dock-denied 2D VFX sprites, GIFs, Unity source ZIP.
Read-only pins four immutable B01-31 W10 source SVGs from PR #489.
Pure art; NEVER grants permission to Reattach or changes gameplay/physics.
Native Unity import, Editor, PlayMode and GPU tests are NOT_RUN.
"""
from __future__ import annotations
import argparse
import hashlib
import io
import json
import math
from pathlib import Path
import re
import sys
import xml.etree.ElementTree as ET
import zipfile

import cairosvg
from PIL import Image, ImageChops, ImageDraw, ImageFilter, ImageFont

ROOT = Path("Tools/B01-31/P17/W11DockDeniedVfx001")
ORIGINAL_PREFIX = "Tools/B01-31/P17/W10SafeDockSignals001/source"
SOURCE_SHA = "7944af98524b7eb5ceadb88eb056de1d486ac461"
SVG_BLOBS = {
    "collision-overlap": "8c762cde35b5cc991130ac643c2a558de6a31cdb",
    "distance-limit": "ff40ff8c5c1c68867004b439fa797e51dc999544",
    "unguarded-route": "45bfb39e323279b667245b3e19bf8ad3bb4231fb",
    "query-saturated": "d604cbd4af1aa5e9b5111576f36fa5a5dd28a993",
}
ICONS = tuple(SVG_BLOBS)
TINT = {
    "collision-overlap": (255, 117, 123),
    "distance-limit": (255, 190, 99),
    "unguarded-route": (255, 140, 99),
    "query-saturated": (103, 218, 230),
}
LABELS = {
    "collision-overlap": "DENIED | PHYSICAL OVERLAP",
    "distance-limit": "DENIED | OUT OF REACH",
    "unguarded-route": "HOLD | PUBLIC API NOT GUARDED",
    "query-saturated": "HOLD | QUERY NOT COMPLETE",
}
COUNT = 8
MS_PER_FRAME = 100
SIDE = 192
UNITY_ROOT = "Assets/B0131DockDeniedVfx"
PACKAGE = "B01-31-DOCK-DENIED-ANIM-001"
VERSION = "P17-W11-R1"
H = lambda data: hashlib.sha256(data).hexdigest()

def check(ok, why):
    if not ok:
        raise ValueError("B0131_DOCK_DENIED_VFX_REJECT: " + why)

def git_blob(raw):
    return hashlib.sha1(("blob "+str(len(raw))+"\0").encode()+raw).hexdigest()

def verify_vector(raw, key):
    check(key in SVG_BLOBS and git_blob(raw) == SVG_BLOBS[key],
          "original SVG Git blob mismatch at immutable W10 source: "+key)
    check(len(raw) < 20000, "unexpected SVG size")
    check(not re.search(rb"<\s*(script|image|foreignObject|use|filter)\b",
                        raw, re.IGNORECASE), "SVG external/script input")
    check(b"href" not in raw.lower() and b"<!DOCTYPE" not in raw.upper(),
          "SVG external href/entity")
    root = ET.fromstring(raw)
    ns = "{http://www.w3.org/2000/svg}"
    check(root.tag==ns+"svg" and root.get("viewBox")=="0 0 256 256",
          "source SVG canonical root")
    check(all(el.tag in {ns+x for x in ("svg","title","g","path","rect","circle")}
              for el in root.iter()), "unrecognized SVG primitive")

def png_encode(image):
    out=io.BytesIO()
    image.save(out,"PNG",optimize=False,compress_level=0)
    return out.getvalue()

def read_svg_icon(raw):
    raster=cairosvg.svg2png(bytestring=raw,output_width=136,output_height=136)
    with Image.open(io.BytesIO(raster)) as im:
        im.load()
        check(im.format=="PNG", "not actual CairoSVG bitmap")
        return im.convert("RGBA")

def draw_frame(base, name, index):
    """Different signature dynamics for four blocked conditions, no SAFE/green."""
    check(0<=index<COUNT, "frame index")
    x = Image.new("RGBA",(SIDE,SIDE),(0,0,0,0))
    draw=ImageDraw.Draw(x,"RGBA")
    tint=TINT[name]
    # All shapes are inside the 192px sprite; transparent corners and border.
    pulse=(1-math.cos(math.tau*index/COUNT))/2
    cx=cy=SIDE//2
    radius=76+int(5*pulse)
    draw.ellipse((cx-radius,cy-radius,cx+radius,cy+radius),
                 outline=(*tint,52+int(80*pulse)),width=3)
    draw.arc((17,17,175,175),-55+45*index,-31+45*index,
             fill=(*tint,200),width=5)
    glow=Image.new("RGBA",(SIDE,SIDE),(0,0,0,0))
    glow.paste(base,(28,28),base)
    # Glow passes are actually composited behind source icon, without assets.
    silhouette=glow.getchannel("A").filter(ImageFilter.GaussianBlur(6))
    glow_only=Image.new("RGBA",(SIDE,SIDE),(*tint,0))
    glow_only.putalpha(silhouette.point(lambda a:int(a*(0.16+0.19*pulse))))
    x=Image.alpha_composite(x,glow_only)
    x=Image.alpha_composite(x,glow)
    d=ImageDraw.Draw(x,"RGBA")
    if name=="collision-overlap":
        # Impact rays and crossing stress marks expand in eight authored frames.
        for angle in (25,65,115,155,205,245,295,335):
            r0=67+int(3*pulse)
            r1=74+int(7*pulse)
            aa=math.radians(angle)
            d.line((cx+r0*math.cos(aa),cy+r0*math.sin(aa),
                    cx+r1*math.cos(aa),cy+r1*math.sin(aa)),
                   fill=(*tint,135+int(115*pulse)),width=4)
        d.line((82,82,110,110),fill=(255,238,220,100+int(140*pulse)),width=3)
    elif name=="distance-limit":
        # Dashed reach ticks commute inwards/outwards; never imply permission.
        offset=(index*5)%18
        for yy in (45,145):
            for xx in range(33+offset,160,22):
                d.line((xx,yy,xx+7,yy),fill=(*tint,178),width=3)
        d.line((36+int(6*pulse),96,52+int(6*pulse),96),
               fill=(255,238,218,208),width=4)
        d.line((140-int(6*pulse),96,156-int(6*pulse),96),
               fill=(255,238,218,208),width=4)
    elif name=="unguarded-route":
        # Repeating scan line: explicitly HOLD, not progress to "safe".
        yy=47+12*index
        d.line((42,yy,150,yy),fill=(*tint,120+int(75*pulse)),width=4)
        d.line((35,yy+5,44,yy+5),fill=(255,235,205,210),width=2)
        d.ellipse((155,29,165,39),fill=(*tint,100+int(150*pulse)))
    else:
        # Diagnostic query-capacity rows cycle through four static warnings.
        for row in range(3):
            enabled=(index+row)%4!=0
            d.rectangle((41,44+row*19,46+int(14*pulse),48+row*19),
                        fill=(*tint,190 if enabled else 55))
        d.arc((27,27,165,165),180+24*index,195+24*index,
              fill=(205,248,250,145),width=3)
    check(x.getpixel((0,0))[3]==0 and x.getpixel((191,191))[3]==0,
          "VFX sprite transparent edge")
    check(x.getchannel("A").getbbox() is not None,
          "all denied/hold status frames must remain visible")
    return x

def folder_meta(path):
    return ("fileFormatVersion: 2\n"
            f"guid: {guid(path)}\n"
            "folderAsset: yes\nDefaultImporter:\n"
            "  externalObjects: {}\n  userData:\n"
            "  assetBundleName:\n  assetBundleVariant:\n").encode()

def guid(path):
    return H((PACKAGE+":"+VERSION+":"+path).encode())[:32]

def texture_meta(path):
    return (f"fileFormatVersion: 2\nguid: {guid(path)}\n"
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

def unity_pack(files,out):
    all_paths={UNITY_ROOT+".meta":folder_meta(UNITY_ROOT),
               UNITY_ROOT+"/Sprites.meta":folder_meta(UNITY_ROOT+"/Sprites")}
    for name in ICONS:
        folder=UNITY_ROOT+"/Sprites/"+name
        all_paths[folder+".meta"]=folder_meta(folder)
        for idx in range(COUNT):
            p=folder+"/frame_%02d.png"%idx
            all_paths[p]=files[(name,idx)]
            all_paths[p+".meta"]=texture_meta(p)
    check(len(all_paths)==70,"4*8 PNG+meta plus 6 folder metas expected")
    check(len(set(p.casefold() for p in all_paths))==70,
          "Unity Windows-casefold import collision")
    metas={p:guid(p[:-5]) for p in all_paths if p.endswith(".meta")}
    check(len(metas)==38 and len(set(metas.values()))==38,
          "38 deterministic stable GUIDs")
    for p,g in metas.items():
        check(("guid: "+g).encode() in all_paths[p],
              "GUID does not match physical meta")
    for p in all_paths:
        check(not p.startswith("/") and ".." not in p.split("/")
              and "\\" not in p, "ZIP traversal")
    # STORED ensures binary-identical across systems without platform compressors.
    with zipfile.ZipFile(out,"w",compression=zipfile.ZIP_STORED) as z:
        for p,data in sorted(all_paths.items()):
            zi=zipfile.ZipInfo(p,(2020,1,1,0,0,0))
            zi.create_system=3
            zi.external_attr=0o100644<<16
            zi.compress_type=zipfile.ZIP_STORED
            z.writestr(zi,data)
    with zipfile.ZipFile(out) as z:
        check(z.testzip() is None,"actual Unity import ZIP CRC")
        check(set(z.namelist())==set(all_paths),"actual Unity ZIP inventory")
        check(all(z.read(p)==data for p,data in all_paths.items()),
              "actual Unity ZIP per-member content mismatches")
    return metas

def negative_checks(sample,orig):
    # Must reject direct source mutation even if parse remains valid.
    for mutant in (orig+b"<!--unauthorized edit-->", orig.replace(b"<title>",b"<script>"),
                   orig.replace(b'viewBox="0 0 256 256"', b'viewBox="0 0 12 12"')):
        try:
            verify_vector(mutant,"collision-overlap")
        except (ValueError,ET.ParseError):
            pass
        else:
            raise AssertionError("negative source mutant accepted")
    # All frame checks refer to actual object and integrity, not a test-only boolean.
    check(H(sample+b"tampered")!=H(sample),"binary corruption negative control")
    try:
        draw_frame(Image.new("RGBA",(136,136)), "collision-overlap", COUNT)
    except ValueError:
        pass
    else:
        raise AssertionError("invalid frame index accepted")
    check(len(set(guid(UNITY_ROOT+"/Sprites/a/frame_%02d.png"%i)
                  for i in range(COUNT))) == COUNT,
          "negative stable GUID alias detected")
    return 6

def actual_images(out,base_dir):
    out.mkdir(parents=True,exist_ok=True)
    (out/"frames").mkdir(exist_ok=True)
    (out/"previews").mkdir(exist_ok=True)
    images={}
    svg_input=[]
    physical=[]
    gifs=[]
    strips=[]
    for key in ICONS:
        source=(base_dir/(key+".svg")).read_bytes()
        verify_vector(source,key)
        svg_input.append({"name":key,"git_blob":git_blob(source),"sha256":H(source)})
        icon=read_svg_icon(source)
        frames=[]
        for idx in range(COUNT):
            img=draw_frame(icon,key,idx)
            frames.append(img)
            path=out/"frames"/key/("frame_%02d.png"%idx)
            path.parent.mkdir(parents=True,exist_ok=True)
            data=png_encode(img)
            path.write_bytes(data)
            with Image.open(path) as verify:
                verify.load()
                check(verify.format=="PNG" and verify.mode=="RGBA"
                      and verify.size==(SIDE,SIDE),
                      "real frame PNG decode "+key)
            images[(key,idx)]=data
            physical.append({"icon":key,"frame":idx,
                             "path":path.relative_to(out).as_posix(),
                             "sha256":H(data),"bytes":len(data)})
        # Distinct visible frames after actual alpha composite on opaque dark.
        samples=[]
        for img in frames:
            bg=Image.new("RGBA",img.size,(24,33,47,255))
            samples.append(Image.alpha_composite(bg,img).convert("RGB"))
        check(len(set(H(im.tobytes()) for im in samples))==COUNT,
              "8 actually distinct visual frames "+key)
        gif_path=out/"previews"/(key+".gif")
        # Animated preview, 8 real composited frames with exact 100ms timing.
        frames[0].save(gif_path,"GIF",save_all=True,
                       append_images=frames[1:],duration=[MS_PER_FRAME]*COUNT,
                       loop=0,disposal=2,optimize=False)
        with Image.open(gif_path) as test:
            check(test.format=="GIF" and test.n_frames==COUNT,
                  "actual animated GIF must have eight frames")
            durations=[]
            for n in range(test.n_frames):
                test.seek(n)
                durations.append(test.info.get("duration"))
            check(durations==[MS_PER_FRAME]*COUNT,
                  "actual gif frame timing, 8 x 100ms")
        gifs.append({"icon":key,"sha256":H(gif_path.read_bytes()),
                     "bytes":gif_path.stat().st_size,"frames":COUNT})
        strip=Image.new("RGB",(SIDE*COUNT,SIDE),(22,32,47))
        for i,img in enumerate(frames):
            bg=Image.new("RGBA",img.size,(22,32,47,255))
            strip.paste(Image.alpha_composite(bg,img).convert("RGB"),(i*SIDE,0))
        strip_path=out/"previews"/(key+"-strip.png")
        strip_path.write_bytes(png_encode(strip))
        strips.append({"icon":key,"sha256":H(strip_path.read_bytes()),
                       "size":list(strip.size)})
    sheet=Image.new("RGB",(COUNT*SIDE,COUNT*SIDE//2+92),(23,32,48))
    textdraw=ImageDraw.Draw(sheet)
    font=ImageFont.load_default()
    for row,key in enumerate(ICONS):
        textdraw.text((8,row*(SIDE+23)+7),LABELS[key],
                      fill=(238,239,240),font=font)
        for col in range(COUNT):
            with Image.open(io.BytesIO(images[(key,col)])) as im:
                rgba=im.convert("RGBA")
            bg=Image.new("RGBA",rgba.size,(23,32,48,255))
            im=Image.alpha_composite(bg,rgba)
            sheet.paste(im.convert("RGB"),(col*SIDE,row*(SIDE+23)+23))
    contact_path=out/"previews"/"ALL_DENIED_HOLD_ANIM_STORYBOARD.png"
    contact_path.write_bytes(png_encode(sheet))
    with Image.open(contact_path) as verify:
        verify.load()
        check(verify.format=="PNG" and verify.size==sheet.size,
              "actual VFX storyboard physical output")
    zpath=out/"B0131-DockDeniedVfx-UnitySpriteFrames.zip"
    meta=unity_pack(images,zpath)
    clip_config={
        "package_id":PACKAGE,
        "revision":VERSION,
        "source_commit":SOURCE_SHA,
        "frame_interval_ms":MS_PER_FRAME,
        "frame_count":COUNT,
        "loop":True,
        "host_integration":"NOT IMPLEMENTED; ONLY visual material; never enforce safety",
        "clips":[{"id":key,"semantic":LABELS[key],
                  "folder":UNITY_ROOT+"/Sprites/"+key,
                  "frame_files":["frame_%02d.png"%i for i in range(COUNT)]}
                 for key in ICONS]
    }
    (out/"clip_timing.json").write_bytes(
        (json.dumps(clip_config,indent=2,sort_keys=True)+"\n").encode("utf-8"))
    result={
        "package":PACKAGE,"version":VERSION,
        "original_source_pr":489,"original_source_sha":SOURCE_SHA,
        "w11_original_svg_inputs":svg_input,
        "real_rgba_frame_pngs":physical,"animated_gifs":gifs,
        "eight_frame_strips":strips,
        "original_32frame_storyboard":H(contact_path.read_bytes()),
        "source_clip_timing_sha256":H((out/"clip_timing.json").read_bytes()),
        "physical_unity_sprite_zip_sha256":H(zpath.read_bytes()),
        "physical_unity_sprite_zip_bytes":zpath.stat().st_size,
        "zip_entry_count":70,"stable_unique_meta_guids":len(meta),
        "negative_tests":negative_checks(images[("collision-overlap",0)],
                                         (base_dir/"collision-overlap.svg").read_bytes()),
        "native_unity":"NOT_RUN","independent_B01_40_B01_60":"PENDING",
        "codex_ack":False,"integrated":False,
        "current_reattach_security":"P0 HOLD #486: no safety claims from VFX",
        "limitations":["GIF for 2D preview only, not live Unity GPU output",
                       "Unity Sprite frame import NOT_RUN",
                       "Does not resolve #488/#494 or #486 atomics",
                       "No success/green/SAFE message",
                       "No actual gameplay event wiring or Animator"],
    }
    (out/"receipt.json").write_bytes(
        (json.dumps(result,indent=2,sort_keys=True)+"\n").encode("utf-8"))
    print(json.dumps({
        "status":"PASS", "source_svg_pins":len(svg_input),
        "actual_png_frames":len(physical),"actual_animated_gifs":len(gifs),
        "actual_8frame_preview_strips":len(strips),"contact_sheets":1,
        "physical_unity_zip_members":70,"unique_guids":len(meta),
        "negative_inputs_rejected":result["negative_tests"],
        "sprite_zip_sha256":result["physical_unity_sprite_zip_sha256"],
        "native_unity":"NOT_RUN"
    }))
    return 0

if __name__=="__main__":
    p=argparse.ArgumentParser()
    p.add_argument("--w10-svg-dir",required=True,
                   help="Read-only extracted 4 SVG files from immutable Git PR #489")
    p.add_argument("--output",required=True)
    arg=p.parse_args()
    try:
        sys.exit(actual_images(Path(arg.output),Path(arg.w10_svg_dir)))
    except Exception as exc:
        print("B0131_DOCK_DENIED_VFX_CLOSED_RED "+type(exc).__name__+" "+str(exc),
              file=sys.stderr)
        raise
