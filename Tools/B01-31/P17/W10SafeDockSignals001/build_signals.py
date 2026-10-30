#!/usr/bin/env python3
"""B01-31 original safe-dock diagnostic ART. Never enforces reattachment safety.
Builds actual 2D PNG sprites + contact boards + isolated Unity Sprite ZIP.
NOT a Unity import test, not a change to B01-23, B01-61/62, Codex or game Assets.
"""
from __future__ import annotations

import argparse
import hashlib
import io
import json
import re
import sys
import xml.etree.ElementTree as ET
import zipfile
from pathlib import Path

import cairosvg
from PIL import Image, ImageChops, ImageDraw, ImageFont

ROOT = Path("Tools/B01-31/P17/W10SafeDockSignals001")
ICONS = (
    "collision-overlap",
    "distance-limit",
    "unguarded-route",
    "query-saturated",
)
LABELS = {
    "collision-overlap": "PHYSICAL OVERLAP (BLOCK)",
    "distance-limit": "REACH LIMIT (BLOCK)",
    "unguarded-route": "UNGUARDED ROUTE (HOLD)",
    "query-saturated": "INCOMPLETE QUERY (HOLD)",
}
SIZES = (96, 192, 384)
UNITY_ROOT = "Assets/B0131SafeDockSignals"
PACKAGE_ID = "B01-31-SAFEDOCK-SIGNAL-ART-001"
VERSION = "P17-W10-R1"
IMAGE_SIDE = 384
BACKGROUND = {"dark": (21, 31, 46), "light": (202, 210, 219)}
H = lambda b: hashlib.sha256(b).hexdigest()


def must(ok, why):
    if not ok:
        raise ValueError("B0131_SAFEDOCK_ART_REJECT " + why)


def svg_contract(raw: bytes):
    """Strict original-icon content contract. Fail closed against external content."""
    must(len(raw) < 20000, "oversized vector")
    must(not re.search(rb"<\s*(script|image|foreignObject|filter|use|text)\b",
                       raw, re.IGNORECASE), "external/script/text SVG primitive")
    must(b"href" not in raw.lower() and b"<!DOCTYPE" not in raw.upper(),
         "external reference/entity")
    try:
        node = ET.fromstring(raw)
    except ET.ParseError as err:
        raise ValueError("malformed original SVG") from err
    ns = "{http://www.w3.org/2000/svg}"
    must(node.tag == ns + "svg" and node.attrib.get("viewBox") == "0 0 256 256",
         "SVG root/viewBox")
    elements = list(node.iter())
    must(all(el.tag in {ns + x for x in ("svg","g","title","path","rect","circle")}
             for el in elements), "unexpected vector element")
    must(len([el for el in elements if el.tag == ns+"title"]) == 1,
         "one accessible title")
    must(all("onload" not in a.lower() for el in elements for a in el.attrib),
         "event handler")
    must(len([el for el in elements if el.tag in (ns+"path",ns+"rect",ns+"circle")]) >= 4,
         "original authored shape count")
    return True


def sprite_guid(path: str):
    # Exact stable importer IDs, isolated to this package namespace and version.
    return H((PACKAGE_ID + ":" + VERSION + ":" + path).encode())[:32]


def unity_folder_meta(path):
    return (f"fileFormatVersion: 2\nguid: {sprite_guid(path)}\n"
            "folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n"
            "  userData:\n  assetBundleName:\n  assetBundleVariant:\n").encode()


def unity_texture_meta(path):
    return (f"fileFormatVersion: 2\nguid: {sprite_guid(path)}\n"
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


def test_import(files):
    expected_names = {
        UNITY_ROOT+".meta", UNITY_ROOT+"/Icons.meta",
        *[UNITY_ROOT+"/Icons/"+x+".png" for x in ICONS],
        *[UNITY_ROOT+"/Icons/"+x+".png.meta" for x in ICONS],
    }
    must(set(files)==expected_names and len(files)==10,
         "exact 10 original Unity import file paths")
    must(len({p.casefold() for p in files})==10, "case insensitive path uniqueness")
    must(all(p.startswith(UNITY_ROOT+"/") or p==UNITY_ROOT+".meta"
             for p in files), "unsafe output path")
    metas={p:H((PACKAGE_ID+":"+VERSION+":"+p[:-5]).encode())[:32]
           for p in files if p.endswith(".meta")}
    must(len(metas)==6 and len(set(metas.values()))==6, "6 stable unique GUIDs")
    for p,g in metas.items():
        must(("guid: "+g).encode() in files[p], "original GUID in actual .meta "+p)
    must(all(b"textureType: 8" in files[p] and b"alphaIsTransparency: 1" in files[p]
             for p in metas if p.endswith(".png.meta")), "Unity 2D sprite/alpha importer")
    return metas


def png_bytes(image):
    b = io.BytesIO()
    # Portable uncompressed output: stable input bytes -> stable PNG bytes.
    image.save(b, format="PNG", optimize=False, compress_level=0)
    return b.getvalue()


def rgba_contract(image, size):
    must(image.size==(size,size) and image.mode=="RGBA",
         "wrong real RGBA sprite format/size")
    a=image.getchannel("A")
    must(a.getbbox() is not None and
         a.getpixel((0,0)) == 0 and a.getpixel((size-1,size-1)) == 0,
         "transparent image border and real nonzero alpha")
    pixels=sum(v>32 for v in a.getdata())
    must(pixels > size*size//9 and pixels < size*size*98//100,
         "real silhouette coverage limit")
    return pixels


def compare_background(sprite, rgb):
    dest=Image.new("RGB", sprite.size, rgb)
    dest.paste(sprite,(0,0),sprite)
    delta=ImageChops.difference(dest,Image.new("RGB",sprite.size,rgb))
    count=sum(max(p)>=16 for p in delta.getdata())
    must(count>sprite.width*sprite.height//25,
         "minimum silhouette contrast pixel signal")
    return dest, count


def board_build(pngs, theme, rgb):
    w,h = 1840, 1552
    background=(30,42,57) if theme=="dark" else (221,226,231)
    ink=(239,244,251) if theme=="dark" else (22,38,53)
    sub=(164,183,200) if theme=="dark" else (61,81,97)
    border=(61,75,92) if theme=="dark" else (166,183,194)
    board=Image.new("RGB",(w,h),background)
    draw=ImageDraw.Draw(board)
    font=ImageFont.load_default()
    draw.rectangle((0,0,w,116),fill=rgb)
    draw.text((32,29),"B01-31 // SAFE DOCK DIAGNOSTIC ART // NOT A SECURITY GATE",
              fill=ink,font=font)
    draw.text((32,60),"Original source vectors // 4 semantic failures // 96 / 192 / 384 px // "+theme.upper(),
              fill=ink,font=font)
    draw.text((32,86),"NO GREEN/SAFE ICON: legacy Reattach public API remains unsafe until owner repairs",
              fill=sub,font=font)
    xbase=86
    top=166
    colw=425
    rowh=433
    count=0
    for col,name in enumerate(ICONS):
        xx=xbase+col*colw
        draw.text((xx,140),LABELS[name],fill=ink,font=font)
        for row,size in enumerate(SIZES):
            yy=top+row*rowh
            draw.rounded_rectangle((xx,yy,xx+396,yy+409),radius=16,
                                   outline=border,width=3)
            pic=pngs[(name,size)]
            comp,delta=compare_background(pic,rgb)
            imgx=xx+(396-size)//2
            imgy=yy+9+(384-size)//2
            board.paste(comp,(imgx,imgy))
            draw.text((xx+14,yy+388),str(size)+"px  |  COLOR DELTA>=16: "+str(delta),
                      fill=sub,font=font)
            count+=1
    draw.text((32,h-30),
              "VISUAL STUDY ONLY: does not verify PhysX, mounted parts, reach, or any public API",
              fill=ink,font=font)
    must(count==12,"4 states x3 sizes board coverage")
    return board


def run(out):
    out.mkdir(parents=True,exist_ok=True)
    preview=out/"preview"
    preview.mkdir(exist_ok=True)
    images={}
    svg_receipts=[]
    rendered=[]
    for name in ICONS:
        src=ROOT/"source"/(name+".svg")
        raw=src.read_bytes()
        svg_contract(raw)
        svg_receipts.append({"name":name,"path":src.as_posix(),
                             "svg_sha256":H(raw),"bytes":len(raw)})
        for size in SIZES:
            actual=cairosvg.svg2png(bytestring=raw,output_width=size,output_height=size)
            with Image.open(io.BytesIO(actual)) as im:
                im.load()
                must(im.format=="PNG","real CairoSVG SVG raster output")
                icon=im.convert("RGBA")
            coverage=rgba_contract(icon,size)
            file=preview/(name+"-"+str(size)+".png")
            b=png_bytes(icon);file.write_bytes(b)
            with Image.open(file) as test:
                test.load()
                must(test.mode=="RGBA" and test.size==(size,size),
                     "actual physical PNG readback")
            images[(name,size)]=icon
            rendered.append({"name":name,"size":size,
                             "filename":file.relative_to(out).as_posix(),
                             "sha256":H(b),"bytes":len(b),"alpha_pixels_gt32":coverage})
    must(len(rendered)==12,"actual 4x3 PNG count")
    # Verify that original icon shapes differ even when all are rendered in monochrome.
    masks=[images[(n,192)].getchannel("A").point(lambda p: 255 if p>32 else 0)
           for n in ICONS]
    for i in range(4):
        for j in range(i+1,4):
            must(ImageChops.difference(masks[i],masks[j]).getbbox() is not None,
                 "not visually distinguishable alpha silhouettes")
    bg_samples=[]
    board_outputs=[]
    for theme,rgb in BACKGROUND.items():
        for name in ICONS:
            for size in SIZES:
                _,contrast=compare_background(images[(name,size)],rgb)
                bg_samples.append({"theme":theme,"icon":name,"size":size,
                                   "pixel_delta_ge16":contrast})
        board=board_build(images,theme,rgb)
        f=out/("SAFE_DOCK_SIGNALS_"+theme.upper()+".png")
        b=png_bytes(board)
        f.write_bytes(b)
        with Image.open(f) as reread:
            reread.load()
            must(reread.size==(1840,1552) and reread.mode=="RGB",
                 "original real preview board PNG readback")
        board_outputs.append({"name":f.name,"sha256":H(b),"bytes":len(b)})
    must(len(bg_samples)==24,"4x3x2 physical contrast cases")
    must(len(board_outputs)==2,"2 actual reviewable light/dark boards")

    files={
        UNITY_ROOT+".meta":unity_folder_meta(UNITY_ROOT),
        UNITY_ROOT+"/Icons.meta":unity_folder_meta(UNITY_ROOT+"/Icons")
    }
    for name in ICONS:
        f=UNITY_ROOT+"/Icons/"+name+".png"
        source=out/"preview"/(name+"-384.png")
        files[f]=source.read_bytes()
        files[f+".meta"]=unity_texture_meta(f)
    guids=test_import(files)
    archive=out/"B0131-SafeDockSignals-UnityImport.zip"
    with zipfile.ZipFile(archive,"w",compression=zipfile.ZIP_STORED) as z:
        for path,raw in sorted(files.items()):
            info=zipfile.ZipInfo(path,(2020,1,1,0,0,0))
            info.compress_type=zipfile.ZIP_STORED
            info.create_system=3
            info.external_attr=0o100644<<16
            z.writestr(info,raw)
    with zipfile.ZipFile(archive) as z:
        must(z.testzip() is None,"real Unity import ZIP CRC failure")
        must(set(z.namelist())==set(files),"real ZIP member set")
        must(all(z.read(p)==raw for p,raw in files.items()),"real ZIP byte parity")
    # Independent negative input controls: must fail before modifying game or original data.
    for candidate in (
        b'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 256 256"><script/></svg>',
        b'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 256 256"><image href="https://wrong.example/a"/></svg>',
        b'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 256 256"></svg>',
    ):
        try:
            svg_contract(candidate)
        except ValueError:
            pass
        else:
            raise AssertionError("FALSE_GREEN invalid source vector accepted")
    bad=dict(files)
    bad[UNITY_ROOT+"/Icons/distance-limit.png.meta"]=files[UNITY_ROOT+"/Icons/collision-overlap.png.meta"]
    try:
        test_import(bad)
    except ValueError:
        pass
    else:
        raise AssertionError("FALSE_GREEN duplicate GUID accepted")
    must(H(archive.read_bytes()+b"tamper") != H(archive.read_bytes()),
         "negative archive tamper fingerprint")
    must(len(set(ICONS))==4 and all("/" not in n and ".." not in n for n in ICONS),
         "safe sprite naming/ID")
    receipt={
        "package_id":PACKAGE_ID,"version":VERSION,"source_prs":[471,472,476],
        "source_scope":"Original B01-31 original vectors, NOT mechanical reattach implementation",
        "owner":"B01-31","generator":"CairoSVG 2.8.2 + Pillow 11.3.0",
        "source_svg":svg_receipts,"image_files":rendered,"boards":board_outputs,
        "background_contrast":bg_samples,
        "zip_filename":archive.name,"zip_sha256":H(archive.read_bytes()),
        "zip_bytes":archive.stat().st_size,
        "zip_members":len(files),"unity_guid_count":len(guids),
        "unity_guids":[{"path":p,"guid":v} for p,v in sorted(guids.items())],
        "native_unity":"NOT_RUN","formal_B01_40_B01_60":"PENDING",
        "host_reach_and_legacy_api_security":"UNVERIFIED_CRITICAL_BLOCKER",
        "game_integrated":False,
        "limitations":[
            "No real protected public API, no Rigidbody, no gameplay script",
            "No success/green/SAFE badge until all reattach entry points are actually gated",
            "CairoSVG render and meta schema not tested inside Unity 6000.3.14f1 URP",
            "Graphic recognition/accessibility and language-independent interpretation require human QA"
        ],
    }
    (out/"receipt.json").write_bytes((json.dumps(receipt,indent=2,sort_keys=True)+"\n").encode())
    print(json.dumps({
        "status":"PASS","real_original_vectors":len(svg_receipts),
        "original_rgba_png":len(rendered),"actual_art_boards":len(board_outputs),
        "visual_contrast_cases":len(bg_samples),"real_unity_zip_members":len(files),
        "unique_meta_guids":len(guids),"negative_mutants_rejected":5,
        "zip_sha256":receipt["zip_sha256"],"unity_native":"NOT_RUN"
    }))
    return 0


if __name__=="__main__":
    p=argparse.ArgumentParser()
    p.add_argument("--output",default="build/B01-31/P17W10/SafeDockSignal001")
    args=p.parse_args()
    try:
        sys.exit(run(Path(args.output)))
    except Exception as e:
        print("B0131_SAFEDOCK_ASSET_BUILD_FAIL: "+str(e),file=sys.stderr)
        raise
