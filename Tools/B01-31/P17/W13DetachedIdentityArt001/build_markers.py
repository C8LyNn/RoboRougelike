#!/usr/bin/env python3
"""B01-31 original persistent-identity VISUALS for detached-parts QA.
Renders actual ID-dependent transparent PNG symbol sprites, review boards
and an isolated Unity Sprite import ZIP. Input is a SYNTHETIC fixture only.
No real Save/Load, W06 ID ledger, #510 transaction, or Unity source is called.
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
import zipfile

from PIL import Image, ImageChops, ImageDraw, ImageFont

ROOT = Path("Tools/B01-31/P17/W13DetachedIdentityArt001")
PKG = "B01-31-DETACHED-IDENTITY-MARKERS-001"
VERSION = "P17-W13-R1"
NAMESPACE = "Assets/B0131DetachedIdentityMarkers"
PALETTE = (
    (106, 226, 245), (255, 188, 113), (210, 167, 255),
    (139, 231, 187), (255, 143, 161), (239, 221, 119),
)
BG = {"dark": (22, 32, 49), "light": (205, 216, 225)}
H = lambda b: hashlib.sha256(b).hexdigest()


def require(ok, reason):
    if not ok:
        raise ValueError("B0131_ID_ART_REJECT: " + reason)


def source_fixture_validate(f):
    require(isinstance(f, dict) and f.get("schema") == "b01-31-visual-fixture-1",
            "not B01-31 synthetic reference fixture")
    require(isinstance(f.get("warning"),str) and
            "NOT a Save/Load format" in f["warning"],
            "source lacks the non-authoritative Save/Load notice")
    before=f.get("pre_save")
    after=f.get("post_load_simulated")
    require(isinstance(before,list) and isinstance(after,list), "phase arrays required")
    require(len(before)==16 and len(after)==16, "exactly 16 visual IDs each phase")
    for phase, ids in (("before",before),("synthetic-after",after)):
        require(all(isinstance(v,str) and 3<=len(v)<=128
                    and bool(re.fullmatch(r"[A-Za-z0-9][A-Za-z0-9:_./-]*",v))
                    for v in ids), "invalid opaque visual fixture ID "+phase)
        require(len(set(ids))==len(ids), "duplicate opaque ID "+phase)
    require(set(before)==set(after), "missing/foreign/renamed IDs in simulated phase")
    require(before!=after, "fixtures must exercise reorder-invariance")
    return before,after


def marker_hash(part_id):
    return hashlib.sha256(("B01-31/VISUAL-IDENTITY/v1\0"+part_id).encode()).digest()


def marker_signature(part_id):
    digest=marker_hash(part_id)
    # 15-bit mirrored 5x5 matrix; geometry carries identity independent of hue.
    bits=int.from_bytes(digest[:2],"big") & 0x7fff
    return tuple(bool(bits & (1<<i)) for i in range(15))


def icon(part_id):
    digest=marker_hash(part_id)
    color=PALETTE[digest[2] % len(PALETTE)]
    image=Image.new("RGBA",(256,256),(0,0,0,0))
    d=ImageDraw.Draw(image,"RGBA")
    cx,cy=128,124
    # Original authored hexagonal salvage location halo.
    vertices=[(round(cx+106*math.cos(math.pi/6+j*math.pi/3)),
               round(cy+106*math.sin(math.pi/6+j*math.pi/3))) for j in range(6)]
    d.polygon(vertices,fill=(26,39,58,223),outline=(*color,250),width=7)
    d.ellipse((42,38,214,210),outline=(*color,95),width=5)
    d.ellipse((60,56,196,192),outline=(243,250,255,190),width=2)
    # 5x5 symmetric matrix supports 15 unique spatial bits. Empty squares retained.
    sig=marker_signature(part_id)
    for row in range(5):
        for col in range(5):
            idx=row*3+(col if col<3 else 4-col)
            x,y=87+col*17,82+row*17
            d.rounded_rectangle((x,y,x+14,y+14),radius=3,
                fill=(*color,245) if sig[idx] else (56,79,101,220),
                outline=(245,250,253,110) if sig[idx] else (142,161,181,110),
                width=1)
    # Secondary hash-dependent top-side notches for distinct glance recognition.
    for i in range(5):
        if digest[4] & (1<<i):
            x=101+i*14
            d.line((x,51,x,59),fill=(*color,255),width=4)
    d.line((92,192,164,192),fill=(*color,180),width=5)
    d.ellipse((122,188,134,200),fill=(246,251,255,235))
    require(image.getpixel((0,0))[3]==0 and
            image.getpixel((255,255))[3]==0 and
            image.getchannel("A").getbbox() is not None,
            "real transparent RGBA marker with nonempty distinct shape")
    return image


def png(image):
    buff=io.BytesIO()
    # Platform-invariant stored DEFLATE PNG stream (no OS zlib differences).
    image.save(buff,format="PNG",compress_level=0,optimize=False)
    return buff.getvalue()


def meta_guid(asset_path):
    return H((PKG+":"+VERSION+":"+asset_path).encode("utf8"))[:32]


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


def draw_atlas(order, sprites, phase, background, title):
    # All identical opaque IDs remain visible even when input order changes.
    cw,ch=338,344
    board=Image.new("RGB",(cw*4+48,ch*4+104),background)
    dr=ImageDraw.Draw(board)
    font=ImageFont.load_default()
    text=(237,245,249) if sum(background)<250 else (30,46,64)
    edge=(91,113,135) if sum(background)<250 else (142,164,182)
    dr.text((24,20),title,fill=text,font=font)
    dr.text((24,48),
            "SYNTHETIC ORDER: "+phase+" // NOT A REAL UNITY SAVE/LOAD TEST",
            fill=text,font=font)
    for ix,part_id in enumerate(order):
        x=24+(ix%4)*cw
        y=82+(ix//4)*ch
        dr.rounded_rectangle((x,y,x+cw-8,y+ch-8),
                             radius=15,outline=edge,width=2)
        rgba=sprites[part_id]
        base=Image.new("RGBA",rgba.size,(*background,255))
        base=Image.alpha_composite(base,rgba)
        board.paste(base.convert("RGB"),(x+39,y+17))
        dr.text((x+21,y+284),part_id,fill=text,font=font)
        dr.text((x+21,y+307),"VISUAL KEY ONLY / NOT LEDGER DATA",
                fill=text,font=font)
    return board


def positive_background_contrast(sprite,background):
    comp=Image.new("RGB",sprite.size,background)
    comp.paste(sprite,(0,0),sprite)
    d=ImageChops.difference(comp,Image.new("RGB",sprite.size,background))
    pixels=sum(max(rgb)>=18 for rgb in d.getdata())
    require(pixels>2000,"visual signal too weak on background")
    return pixels


def verify_import_zip(files):
    expected={NAMESPACE+".meta",NAMESPACE+"/Sprites.meta"}
    for i in range(16):
        path=f"{NAMESPACE}/Sprites/marker_{i:02d}.png"
        expected.add(path)
        expected.add(path+".meta")
    require(set(files)==expected and len(files)==34,
            "Unity import exact 16 sprites plus 18 metadata files")
    require(len(set(p.lower() for p in files))==34,
            "case-insensitive Unity path collision")
    require(all(p==NAMESPACE+".meta" or p.startswith(NAMESPACE+"/") for p in files),
            "non-isolated Unity package")
    metadata={p:meta_guid(p[:-5]) for p in files if p.endswith(".meta")}
    require(len(metadata)==18 and len(set(metadata.values()))==18,
            "18 stable unique deterministic .meta GUIDs")
    for p,g in metadata.items():
        require(("guid: "+g).encode() in files[p],
                "actual metadata GUID is not deterministic")
        if p.endswith(".png.meta"):
            require(b"textureType: 8" in files[p] and
                    b"alphaIsTransparency: 1" in files[p],
                    "actual Unity SpriteImporter alpha mode")
    return metadata


def mutations_rejected(original):
    start=original["pre_save"][:]
    after=original["post_load_simulated"][:]
    def changed(**kw):
        v=dict(original);v.update(kw);return v
    controls=[
        changed(pre_save=start[:-1]+[start[0]]),
        changed(post_load_simulated=after[:-1]),
        changed(post_load_simulated=after[:-1]+["foreign-node-x"]),
        changed(post_load_simulated=after[:-1]+[after[-1].upper()]),
        changed(pre_save=[start[0]+"\n"]+start[1:]),
        changed(pre_save=[17]+start[1:]),
        changed(post_load_simulated=start),
    ]
    count=0
    for bad in controls:
        try:source_fixture_validate(bad)
        except ValueError:count+=1
        else:raise AssertionError("FALSE GREEN synthetic identity fixture mutation")
    require(count==7,"7 strong malformed/foreign/dup controls")
    return count


def run(fixture_path,output):
    raw=fixture_path.read_bytes()
    fixture=json.loads(raw)
    before,after=source_fixture_validate(fixture)
    output.mkdir(parents=True,exist_ok=True)
    assets=output/"sprites"
    assets.mkdir(exist_ok=True)
    unique_signatures={marker_signature(x) for x in before}
    require(len(unique_signatures)==16,
            "mirrored 5x5 glyph collision among 16 original fixture IDs")
    images={}
    sprites={}
    metrics=[]
    id_file={}
    for i,part_id in enumerate(before):
        im=icon(part_id)
        images[part_id]=im
        data=png(im)
        marker=f"marker_{i:02d}.png"
        target=assets/marker
        target.write_bytes(data)
        with Image.open(target) as test:
            test.load()
            require(test.size==(256,256) and
                    test.format=="PNG" and test.mode=="RGBA",
                    "actual RGBA PNG physical readback")
        id_file[part_id]=marker
        metrics.append({
            "id":part_id, "file":marker,
            "png_sha256":H(data),"bytes":len(data),
            "pattern_signature":"".join("1" if x else "0" for x in marker_signature(part_id)),
            "contrast_dark_ge18":positive_background_contrast(im,BG["dark"]),
            "contrast_light_ge18":positive_background_contrast(im,BG["light"])
        })
        sprites[marker]=data
    require(len({x["png_sha256"] for x in metrics})==16,
            "no two stable IDs may look byte-identical")
    # Actual source and order-invariance: rendering same ID on reordered AFTER
    # is deterministic, including all original raster bytes.
    for part_id in after:
        require(png(icon(part_id))==sprites[id_file[part_id]],
                "a synthetic post-load ordering changed an ID's original sprite")
    preview=[]
    for title,order,bgname in [
        ("BEFORE_SAVE_SYNTHETIC",before,"dark"),
        ("AFTER_LOAD_SIMULATED",after,"dark"),
    ]:
        board=draw_atlas(order,images,title,BG[bgname],title)
        raw_png=png(board)
        path=output/(title+".png")
        path.write_bytes(raw_png)
        with Image.open(path) as test:
            test.load()
            require(test.size==board.size and test.mode=="RGB" and
                    test.format=="PNG","actual test visual board decode")
        preview.append({"name":path.name,"sha256":H(raw_png),"bytes":len(raw_png),
                        "dimensions":list(board.size)})
    # Third physical color-independent lighter theme is a review aid.
    light=draw_atlas(after,images,"AFTER_LOAD_SIMULATED","light" and BG["light"],
                     "AFTER_LOAD_SIMULATED_LIGHT")
    light_bytes=png(light)
    light_file=output/"AFTER_LOAD_SIMULATED_LIGHT.png"
    light_file.write_bytes(light_bytes)
    preview.append({"name":light_file.name,"sha256":H(light_bytes),
                    "bytes":len(light_bytes),"dimensions":list(light.size)})
    files={NAMESPACE+".meta":folder_meta(NAMESPACE),
           NAMESPACE+"/Sprites.meta":folder_meta(NAMESPACE+"/Sprites")}
    for i,part_id in enumerate(before):
        p=f"{NAMESPACE}/Sprites/marker_{i:02d}.png"
        files[p]=sprites[id_file[part_id]]
        files[p+".meta"]=sprite_meta(p)
    guids=verify_import_zip(files)
    archive=output/"B0131-DetachedIdentityMarkers-UnitySprites.zip"
    with zipfile.ZipFile(archive,"w",compression=zipfile.ZIP_STORED) as z:
        for path,data in sorted(files.items()):
            info=zipfile.ZipInfo(path,(2020,1,1,0,0,0))
            info.create_system=3
            info.compress_type=zipfile.ZIP_STORED
            info.external_attr=0o100644<<16
            z.writestr(info,data)
    with zipfile.ZipFile(archive) as z:
        require(z.testzip() is None and set(z.namelist())==set(files),
                "real 34-member Unity ZIP readback/CRC")
        require(all(z.read(p)==data for p,data in files.items()),
                "real Unity ZIP member byte mismatch")
    source_hash=H(archive.read_bytes())
    require(source_hash!=H(archive.read_bytes()+b"tampered"),
            "negative binary corruption must invalidate ZIP hash")
    negative=mutations_rejected(fixture)
    # Release-quality machine receipt: factual producer outputs, not Unity G1.
    receipt={
      "package_id":PKG,"version":VERSION,"owner":"B01-31",
      "fixture_schema":"b01-31-visual-fixture-1",
      "warning":"SYNTHETIC visual order permutation, not a real Save/Load round trip",
      "fixture_sha256":H(raw),"fixture_opaque_id_count":16,
      "original_visual_marker_assets":metrics,"review_png_atlases":preview,
      "pre_save_order":before,"post_load_simulated_order":after,
      "stable_visual_id_bytes_pass":16,
      "pattern_signatures_unique":len(unique_signatures),
      "dark_light_actual_contrast_cases":32,
      "negative_fixture_mutants_rejected":negative,
      "negative_zip_byte_fingerprint":True,
      "actual_unity_sprite_zip":archive.name,
      "actual_unity_sprite_zip_sha256":source_hash,
      "actual_unity_sprite_zip_bytes":archive.stat().st_size,
      "actual_unity_sprite_zip_members":len(files),
      "unique_unity_guids":len(guids),
      "unity_editor_runtime":"NOT_RUN",
      "real_save_load_runtime":"NOT_RUN",
      "formal_B01_40_60_61_62_qa":"NOT_CLAIMED",
      "selected_reattach_pr":510,
      "selected_reattach_sha":"d94400d95d13db7f27e1533df05c9292d5380431",
      "save_contract_owner":"B01-23/B01-62 (not B01-31)",
      "codex_ack":False,"integrated":False,"G2":"HOLD",
    }
    (output/"receipt.json").write_bytes((json.dumps(receipt,sort_keys=True,indent=2)+"\n").encode())
    print(json.dumps({
      "status":"PASS","original_id_sprite_png":len(sprites),
      "actual_review_png":len(preview),"synthetic_pre_post_visual_id_parity":16,
      "unique_shape_signatures":len(unique_signatures),"dark_light_contrast_samples":32,
      "negative_fixture_mutants":negative,"unity_zip_members":len(files),
      "unique_unity_meta_guids":len(guids),"inner_zip_sha256":source_hash,
      "real_unity_save_load":"NOT_RUN"
    }))
    return 0


if __name__=="__main__":
    p=argparse.ArgumentParser()
    p.add_argument("--fixture",required=True)
    p.add_argument("--output",required=True)
    args=p.parse_args()
    try:sys.exit(run(Path(args.fixture),Path(args.output)))
    except Exception as e:
        print("W13_DETACHED_ID_VISUAL_SOURCE_FAIL "+str(e),file=sys.stderr)
        raise
