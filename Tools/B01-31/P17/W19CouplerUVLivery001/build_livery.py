#!/usr/bin/env python3
"""B01-31 W19: actual original UV-textured 3D coupler livery.
Original W16 immutable source GLB -> add twelve physical VEC2 UV attributes,
six original abrasion/hazard painted 256px PNGs embedded in glTF2, previews.
NO original vertex/normal/index byte changes. No Unity/PhysX/SaveLoad calls.
"""
from __future__ import annotations
import argparse
import hashlib
import io
import json
import math
from pathlib import Path
import struct
import sys
import zipfile
from PIL import Image, ImageDraw

PKG="B01-31-COUPLER-UV-WEAR-LIVERY-001"
VER="P17-W19-R1"
ORIGINAL="B0131SplitCouplerConcept"
OUT="B0131SplitCouplerWornLivery"
SOURCE_SHA="eeae6a553ca6136b5eaa97a65d5dd995f5f28fcb8bc54eb2180dfe28bc3653d7"
SOURCE_PR=599
NAMES=("graphite_shell","rubber_cable","hazard_amber",
       "exposed_copper","insulation_cyan","strain_collar")
H=lambda b:hashlib.sha256(b).hexdigest()
def need(c,m):
    if not c:raise ValueError("B0131_W19_LIVERY_FAIL_CLOSED "+m)

def glb_read(raw):
    need(len(raw)>28 and raw[:4]==b"glTF","source exact original GLB binary header")
    ver,n=struct.unpack_from("<II",raw,4)
    need(ver==2 and n==len(raw),"source glTF v2 exact length")
    jlen,jtype=struct.unpack_from("<I4s",raw,12)
    need(jtype==b"JSON" and jlen%4==0 and 28+jlen<=len(raw),
         "source GLB JSON alignment")
    doc=json.loads(raw[20:20+jlen])
    blen,btype=struct.unpack_from("<I4s",raw,20+jlen)
    need(btype==b"BIN\0" and 28+jlen+blen==len(raw),
         "source GLB BIN bytes")
    declared=doc["buffers"][0]["byteLength"]
    need(blen-declared in (0,1,2,3),"actual original BIN padding")
    return doc,raw[28+jlen:28+jlen+declared]

def source(path):
    model=path/(ORIGINAL+".glb")
    rec=path/"receipt.json"
    need(model.is_file() and rec.is_file(),"missing physical immutable original W16 files")
    blob=model.read_bytes()
    need(H(blob)==SOURCE_SHA,"frozen W16 original source GLB sha256")
    r=json.loads(rec.read_bytes())
    need(r["package_id"]=="B01-31-SPLIT-COUPLER-CONCEPT3D-001"
         and r["version"]=="P17-W16-R1"
         and r["total_glb_triangles"]==1312
         and any(x["file"]==model.name and x["sha256"]==SOURCE_SHA
                 for x in r["actual_binary_assets"]),
         "real original W16 receipt source provenance/triangle count")
    doc,buf=glb_read(blob)
    need(len(doc["meshes"])==2 and len(doc["nodes"])==2
         and len(doc["materials"])==6
         and [m["name"] for m in doc["materials"]]==list(NAMES)
         and len(doc["bufferViews"])==36 and len(doc["accessors"])==36
         and all(len(m["primitives"])==6 for m in doc["meshes"])
         and "images" not in doc
         and "textures" not in doc,
         "actual 12 untextured source primitives / no conflicting textures")
    return doc,buf

def textures():
    output={}
    for k,name in enumerate(NAMES):
        im=Image.new("RGBA",(256,256),(247,247,247,255))
        d=ImageDraw.Draw(im)
        # Actual UV-space industrial surface markings, not tiny uniform
        # swatches: diagnostic bands, abrasion score, rivet pattern.
        base=(225-k*5,225-k*5,225-k*4,255)
        d.rectangle((0,0,255,255),fill=base)
        for x in range(0,256,32):
            d.line((x,0,x,255),fill=(182,185,188,255),width=1)
        for y in range(0,256,32):
            d.line((0,y,255,y),fill=(197,199,201,255),width=1)
        if k==2:
            # Amber material uses black diagonal industrial hazard enamel.
            for i in range(-256,512,48):
                d.polygon([(i,0),(i+17,0),(i+273,255),(i+256,255)],
                          fill=(32,36,42,255))
        elif k==4:
            for y in (30,92,154,218):
                d.rectangle((0,y,255,y+12),fill=(78,123,166,255))
                d.rectangle((0,y+13,255,y+16),fill=(238,247,255,255))
        elif k==3:
            for x in range(10,255,48):
                d.line((x,0,x+18,255),fill=(114,120,105,255),width=7)
        else:
            for i in range(10):
                x=(i*43+19*k)%244
                y=(i*61+11*k)%236
                d.line((x,y,x+15+(i%4),y+7+(i%3)*4),
                       fill=(111,117,125,255),width=2)
        for cy in (22,86,150,214):
            for cx in (22,86,150,214):
                d.ellipse((cx-3,cy-3,cx+3,cy+3),
                          fill=(96,105,116,255),
                          outline=(234,238,240,255),width=1)
        for s in range(29):
            # Deterministic high-frequency scuff wear as physical raster.
            x=(s*53+k*71+13)%247
            y=(s*83+k*23+31)%247
            d.line((x,y,x+4+(s%5),y+2),fill=(163,172,180,255),width=1)
        raw=io.BytesIO()
        im.save(raw,format="PNG",compress_level=9,optimize=False)
        output[f"{OUT}-{name}-basecolor.png"]=raw.getvalue()
    return output

def modify(original,basebuf,pngs):
    doc=json.loads(json.dumps(original))
    doc["asset"]["generator"]=PKG+"/"+VER
    doc["asset"]["extras"]={"original_source_GLB_SHA256":SOURCE_SHA,
                           "original_geometry_binary_prefix_bytes":len(basebuf),
                           "not_gameplay":"no live Unity/PhysX/SaveLoad",
                           "purpose":"worn industrial UV skin, original clean geometry unchanged"}
    doc["samplers"]=[{"magFilter":9729,"minFilter":9729,
                     "wrapS":10497,"wrapT":10497}]
    doc["images"]=[];doc["textures"]=[]
    b=bytearray(basebuf)
    def view(raw,target=None):
        b.extend(b"\0"*((-len(b))%4))
        offset=len(b);b.extend(raw)
        q={"buffer":0,"byteOffset":offset,"byteLength":len(raw)}
        if target is not None:q["target"]=target
        doc["bufferViews"].append(q)
        return len(doc["bufferViews"])-1
    for k,name in enumerate(NAMES):
        raw=pngs[f"{OUT}-{name}-basecolor.png"]
        vi=view(raw)
        doc["images"].append({"name":name+"_original_wear_png",
                              "bufferView":vi,"mimeType":"image/png"})
        doc["textures"].append({"name":name+"_basecolor","source":k,"sampler":0})
        doc["materials"][k]["pbrMetallicRoughness"]["baseColorTexture"]={
            "index":k,"texCoord":0}
    for mesh in doc["meshes"]:
        for primitive in mesh["primitives"]:
            posacc=original["accessors"][primitive["attributes"]["POSITION"]]
            pv=original["bufferViews"][posacc["bufferView"]]
            need(posacc["componentType"]==5126 and posacc["type"]=="VEC3",
                 "immutable FLOAT32 original 3D geometry")
            vals=struct.unpack_from("<"+"f"*(posacc["count"]*3),
                       basebuf,pv["byteOffset"])
            uv=[]
            for i in range(0,len(vals),3):
                x,y,z=vals[i:i+3]
                u=max(0.,min(1.,(x+0.85)/1.70))
                v=(math.atan2(z,y)+math.pi)/(2*math.pi)
                uv.extend((u,v))
            raw=struct.pack("<"+"f"*len(uv),*uv)
            vid=view(raw,34962)
            aid=len(doc["accessors"])
            doc["accessors"].append({"bufferView":vid,
                                    "componentType":5126,"count":posacc["count"],
                                    "type":"VEC2",
                                    "min":[min(uv[::2]),min(uv[1::2])],
                                    "max":[max(uv[::2]),max(uv[1::2])]})
            primitive["attributes"]["TEXCOORD_0"]=aid
    doc["buffers"][0]["byteLength"]=len(b)
    jb=json.dumps(doc,sort_keys=True,separators=(",",":")).encode()
    jb+=b" "*((-len(jb))%4)
    bb=bytes(b)+b"\0"*((-len(b))%4)
    raw=(struct.pack("<4sII",b"glTF",2,12+8+len(jb)+8+len(bb))+
         struct.pack("<I4s",len(jb),b"JSON")+jb+
         struct.pack("<I4s",len(bb),b"BIN\0")+bb)
    return raw,doc

def render_board(doc,rawbase,pngs,theme):
    colors=[]
    for name in NAMES:
        im=Image.open(io.BytesIO(pngs[f"{OUT}-{name}-basecolor.png"]))
        im.load();colors.append(im)
    bg=(20,30,43) if theme=="dark" else (225,232,239)
    fg=(235,245,250) if theme=="dark" else (34,48,62)
    canvas=Image.new("RGB",(1800,720),bg)
    d=ImageDraw.Draw(canvas)
    cameras=[(.53,.32,"ISOMETRIC"),(0.,.09,"SIDE PROFILE"),(-.62,.60,"REVERSE THREE QUARTER")]
    for panel,(yaw,pitch,title) in enumerate(cameras):
        left=panel*600
        d.rectangle((left+8,55,left+590,680),outline=fg,width=2)
        d.text((left+18,18),title+" - SOURCE REAL 3D UV / "+theme.upper(),fill=fg)
        triangles=[]
        for s,mesh in enumerate(doc["meshes"]):
            trans=doc["nodes"][s]["translation"]
            for prim in mesh["primitives"]:
                mid=prim["material"]
                a=doc["accessors"][prim["attributes"]["POSITION"]]
                bv=doc["bufferViews"][a["bufferView"]]
                vals=struct.unpack_from("<"+"f"*(a["count"]*3),
                    rawbase,bv["byteOffset"])
                for i in range(0,len(vals),9):
                    tri=[vals[i+j:i+j+3] for j in (0,3,6)]
                    poly=[]
                    for x,y,z in tri:
                        x+=trans[0];y+=trans[1];z+=trans[2]
                        xx=math.cos(yaw)*x+math.sin(yaw)*z
                        zz=-math.sin(yaw)*x+math.cos(yaw)*z
                        yy=math.cos(pitch)*y-math.sin(pitch)*zz
                        dd=math.sin(pitch)*y+math.cos(pitch)*zz
                        poly.append((left+300+xx*200,365-yy*255,dd))
                    # Sample physically authored texels by real original position UV.
                    center=[sum(p[j] for p in tri)/3 for j in range(3)]
                    u=max(0,min(1,(center[0]+.85)/1.70))
                    v=(math.atan2(center[2],center[1])+math.pi)/(2*math.pi)
                    texel=colors[mid].getpixel((min(255,int(u*255)),min(255,int(v*255))))
                    base=doc["materials"][mid]["pbrMetallicRoughness"]["baseColorFactor"]
                    c=tuple(max(0,min(255,int(texel[k]*base[k]*(.84 if theme=="dark" else .96)))) for k in range(3))
                    triangles.append((sum(p[2] for p in poly)/3,
                        [(round(p[0]),round(p[1])) for p in poly],c))
        for _,poly,color in sorted(triangles,key=lambda x:x[0]):
            d.polygon(poly,fill=color)
        d.text((left+20,695),"6 PHYSICAL PNG + 12 UV MESH PRIMITIVES / NOT UNITY",fill=fg)
    bio=io.BytesIO()
    canvas.save(bio,format="PNG",compress_level=0,optimize=False)
    for im in colors:im.close()
    return bio.getvalue()

def output_zip(out,assets):
    dst=out.parent/(out.name+"-physical.zip")
    with zipfile.ZipFile(dst,"w",compression=zipfile.ZIP_STORED) as z:
        for name in sorted(assets):
            i=zipfile.ZipInfo(name,(2020,1,1,0,0,0))
            i.create_system=3
            i.external_attr=0o100644<<16
            i.compress_type=zipfile.ZIP_STORED
            z.writestr(i,assets[name])
    with zipfile.ZipFile(dst) as z:
        need(z.testzip() is None and len(z.namelist())==10
             and all(z.read(k)==v for k,v in assets.items()),
             "physical 10-member archive CRC and raw byte readback")

def build(sourcepath,out):
    original,sourcebytes=source(sourcepath)
    pngs=textures()
    glb,doc=modify(original,sourcebytes,pngs)
    out.mkdir(parents=True,exist_ok=True)
    assets={OUT+".glb":glb,**pngs}
    for theme in ("dark","light"):
        assets[f"{OUT}-{theme}-three-views.png"]=render_board(doc,sourcebytes,pngs,theme)
    record={
        "package_id":PKG,"version":VER,"owner":"B01-31",
        "original_static_source_pr":SOURCE_PR,"original_source_glb_sha256":SOURCE_SHA,
        "unchanged_original_geometry_BIN_prefix_sha256":H(sourcebytes),
        "unchanged_original_geometry_prefix_bytes":len(sourcebytes),
        "original_triangles":1312,"original_meshes":2,"original_materials":6,
        "real_source_primitives_UV_mapped":12,
        "real_original_256x256_PNG_material_textures":6,
        "real_original_embedded_glTF_images":6,
        "actual_3view_color_contrast_boards":2,
        "true_new_textured_GLB_sha256":H(glb),
        "actual_zip_members":10,
        "files":[{"name":n,"sha256":H(v),"bytes":len(v)}
                 for n,v in sorted(assets.items())],
        "selected_R3":"OWNER_DECISION_PENDING #642/#644",
        "immutable_gameplay_sha":"d94400d95d13db7f27e1533df05c9292d5380431",
        "immutable_save_schema_sha":"33094f154c7b91119b36d58aab285e6e83c5de57",
        "immutable_selected_R2_sha":"efb2b06eafedab5469b57f163152d96aca3c5915",
        "native_Unity":"NOT_RUN","live_game_SaveLoad":"NOT_RUN",
        "formal_G0":"PENDING","G2":"HOLD","codex_ACK":False,
        "integrated":False,"released":False}
    assets["receipt.json"]=(json.dumps(record,sort_keys=True,indent=2)+"\n").encode()
    for n,v in assets.items():
        (out/n).write_bytes(v)
        need((out/n).read_bytes()==v,"physical writer readback failed")
    output_zip(out,assets)
    print(json.dumps({"status":"PASS","original_model_GLTF2":1,
          "six_embedded_original_PNGs":6,"UV_mapped_original_mesh_primitives":12,
          "static_W16_source_geom_bin_prefix_bytes":len(sourcebytes),
          "actual_preview_3view_boards":2,"actual_physical_ZIP_members":10,
          "textured_model_GLb_SHA256":H(glb),"Unity_native":"NOT_RUN"}))

if __name__=="__main__":
    p=argparse.ArgumentParser()
    p.add_argument("--original",required=True)
    p.add_argument("--output",required=True)
    a=p.parse_args()
    try:build(Path(a.original),Path(a.output))
    except Exception as e:
        print("W19_LIVERY_SOURCE_RED "+str(e),file=sys.stderr)
        raise
