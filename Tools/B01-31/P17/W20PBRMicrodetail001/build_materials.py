#!/usr/bin/env python3
"""B01-31 P17 W20: actual 3D original glTF2 industrial microdetail.
Original immutable W19 source -> 6 new tangible tangent-space NORMAL PNGs
and six physically packed AO(R)/ROUGHNESS(G)/METALLIC(B) PNGs, all embedded
in the GLB. Original source W19 mesh, UV, PBR basecolor PNG binary bytes
stay byte-identical. Only additive material texture bindings; no gameplay.
"""
from __future__ import annotations
import argparse
import copy
import hashlib
import io
import json
import math
from pathlib import Path
import struct
import sys
import zipfile
from PIL import Image,ImageDraw

PKG="B01-31-COUPLER-PBR-MICRODETAIL-NORMORM-001"
VERS="P17-W20-R1"
ORIGIN="B0131SplitCouplerWornLivery"
OUTPUT="B0131SplitCouplerMicrodetailPBR"
SOURCE_SHA="f8683e5ee3f0916fbde534895e5e01f890bf577b0083858e8dc361b1f8d5ae41"
NAMES=("graphite_shell","rubber_cable","hazard_amber","exposed_copper",
       "insulation_cyan","strain_collar")
H=lambda b:hashlib.sha256(b).hexdigest()
def check(x,reason):
    if not x:raise ValueError("W20_PBR_SOURCE_RED "+reason)

def decode_glb(raw):
    check(len(raw)>28 and raw[:4]==b"glTF","real glTF2 source header")
    ver,size=struct.unpack_from("<II",raw,4)
    check(ver==2 and size==len(raw),"physical GLB2 total length")
    jlen,jtyp=struct.unpack_from("<I4s",raw,12)
    check(jtyp==b"JSON" and jlen%4==0 and jlen+28<=len(raw),
          "physical source JSON chunk offset")
    doc=json.loads(raw[20:20+jlen])
    blen,btyp=struct.unpack_from("<I4s",raw,20+jlen)
    check(btyp==b"BIN\0" and jlen+28+blen==len(raw),
          "actual GLB2 BIN chunk bound")
    used=doc["buffers"][0]["byteLength"]
    check(blen-used in (0,1,2,3),"original GLB padding invalid")
    return doc,raw[28+jlen:28+jlen+used]

def frozen(root):
    src=root/(ORIGIN+".glb")
    receipt=root/"receipt.json"
    check(src.is_file() and receipt.is_file(),"actual immutable W19 original missing")
    raw=src.read_bytes()
    check(H(raw)==SOURCE_SHA,"actual frozen original W19 physical SHA mismatch")
    r=json.loads(receipt.read_bytes())
    check(r["package_id"]=="B01-31-COUPLER-UV-WEAR-LIVERY-001" and
          r["version"]=="P17-W19-R1" and
          r["true_new_textured_GLB_sha256"]==SOURCE_SHA and
          r["real_source_primitives_UV_mapped"]==12 and
          r["real_original_embedded_glTF_images"]==6,
          "real frozen W19 manifest receipt authentication failed")
    doc,buf=decode_glb(raw)
    check(doc["asset"]["version"]=="2.0" and
          len(doc["images"])==len(doc["textures"])==len(doc["materials"])==6 and
          len(doc["meshes"])==len(doc["nodes"])==2 and
          len(doc["bufferViews"])==54 and len(doc["accessors"])==48 and
          [m["name"] for m in doc["materials"]]==list(NAMES),
          "source original W19 six physically UV textured PBR materials")
    return doc,buf

def source_base_png(doc,buf,i):
    image=doc["images"][i]
    v=doc["bufferViews"][image["bufferView"]]
    raw=buf[v["byteOffset"]:v["byteOffset"]+v["byteLength"]]
    check(image["mimeType"]=="image/png" and raw[:8]==b"\x89PNG\r\n\x1a\n",
          "real embedded original W19 basecolor image")
    with Image.open(io.BytesIO(raw)) as im:
        im.load()
        check(im.mode=="RGBA" and im.size==(256,256),
              "original 256px RGBA physical paint source")
        return im.convert("RGB")

def physical_maps(im,index):
    w,h=im.size
    pix=im.load()
    height=[[0.0]*w for _ in range(h)]
    for y in range(h):
        for x in range(w):
            r,g,b=pix[x,y]
            light=(.2126*r+.7152*g+.0722*b)/255
            # Real original paint and predictable non-periodic wear relief.
            noise=((x*17+y*31+index*53) % 71)/70.
            h0=.42*light+.025*noise+.055*math.sin(x*.11+index*.51)
            height[y][x]=h0
    nm=Image.new("RGB",(w,h))
    orm=Image.new("RGB",(w,h))
    np=nm.load(); op=orm.load()
    base_metal=(45,11,18,212,35,143)[index]
    for y in range(h):
        for x in range(w):
            # UV wrap by default glTF sampler repeat.
            dx=(height[y][(x+1)%w]-height[y][(x-1)%w])*2.4
            dy=(height[(y+1)%h][x]-height[(y-1)%h][x])*2.4
            inv=1/math.sqrt(1+dx*dx+dy*dy)
            n=(-dx*inv,-dy*inv,inv)
            np[x,y]=tuple(max(0,min(255,round((t*.5+.5)*255))) for t in n)
            r,g,b=pix[x,y]
            contrast=(max(r,g,b)-min(r,g,b))/255
            grain=((x*37+y*29+index*23)%31)-15
            occl=max(170,min(250,round(238-19*abs(dx)-22*abs(dy)-5*contrast)))
            rough=max(62,min(240,round(115+index*12+grain*.9+28*contrast)))
            metal=max(0,min(255,round(base_metal+grain*.6-8*contrast)))
            op[x,y]=(occl,rough,metal)
    def tobytes(image):
        out=io.BytesIO()
        image.save(out,format="PNG",compress_level=9,optimize=False)
        return out.getvalue()
    return tobytes(nm),tobytes(orm)

def build_model(source,buf,pngs):
    d=copy.deepcopy(source)
    d["asset"]["generator"]=PKG+"/"+VERS
    d["asset"]["extras"]={
        "source_W19_GLB_SHA256":SOURCE_SHA,
        "immutable_W19_bin_prefix_bytes":len(buf),
        "mode":"ONLY_ADDITIVE_NORMAL_AND_OCCLUSION_ROUGHNESS_METALLIC",
        "rendering":"tangent space normal; host must support/generate tangent basis",
        "Unity_native":"NOT_RUN","gameplay":"NOT_IMPLEMENTED"}
    b=bytearray(buf)
    check(len(d["images"])==len(d["textures"])==6,"frozen existing texture IDs 0..5")
    for i,name in enumerate(NAMES):
        for kind in ("normal","orm"):
            filename=f"{OUTPUT}-{name}-{kind}.png"
            payload=pngs[filename]
            b.extend(b"\0"*((-len(b))%4))
            off=len(b);b.extend(payload)
            view_id=len(d["bufferViews"])
            d["bufferViews"].append({"buffer":0,"byteOffset":off,"byteLength":len(payload)})
            img_id=len(d["images"])
            d["images"].append({"name":name+"_"+kind+"_original_microdetail",
                                "mimeType":"image/png","bufferView":view_id})
            tex_id=len(d["textures"])
            d["textures"].append({"name":name+"_"+kind,"source":img_id,"sampler":0})
            check(tex_id==6+i*2+(0 if kind=="normal" else 1),
                  "append-only six material original image bindings")
        m=d["materials"][i]
        m["normalTexture"]={"index":6+i*2,"texCoord":0,"scale":1.0}
        m["occlusionTexture"]={"index":7+i*2,"texCoord":0,"strength":1.0}
        m["pbrMetallicRoughness"]["metallicRoughnessTexture"]={
            "index":7+i*2,"texCoord":0}
    check(len(d["images"])==len(d["textures"])==18 and
          len(d["bufferViews"])==66 and len(d["accessors"])==48,
          "actual six original basecolor + twelve added normal ORM images")
    d["buffers"][0]["byteLength"]=len(b)
    text=json.dumps(d,sort_keys=True,separators=(",",":")).encode()
    text+=b" "*((-len(text))%4)
    bb=bytes(b)+b"\0"*((-len(b))%4)
    out=(struct.pack("<4sII",b"glTF",2,12+8+len(text)+8+len(bb))+
        struct.pack("<I4s",len(text),b"JSON")+text+
        struct.pack("<I4s",len(bb),b"BIN\0")+bb)
    check(len(out)==struct.unpack_from("<I",out,8)[0],"real glTF2 physical framing")
    return out

def atlas(kind,pngs):
    bg=(19,30,43)
    fg=(238,245,251)
    im=Image.new("RGB",(1800,740),bg)
    d=ImageDraw.Draw(im)
    d.text((20,15),"B01-31 ORIGINAL W20 / "+kind.upper()+
           " ACTUAL SOURCE 3D MATERIAL MAPS / NOT UNITY",fill=fg)
    for index,name in enumerate(NAMES):
        left=(index%3)*600
        row=index//3
        top=60+row*335
        with Image.open(io.BytesIO(pngs[f"{OUTPUT}-{name}-{kind}.png"])) as sub:
            sub.load()
            block=sub.convert("RGB").resize((280,280))
        im.paste(block,(left+155,top+20))
        d.rectangle((left+150,top+15,left+440,top+305),outline=(112,141,168),width=2)
        d.text((left+25,top+310),name+" / "+kind.upper(),fill=fg)
    raw=io.BytesIO()
    im.save(raw,format="PNG",compress_level=0,optimize=False)
    return raw.getvalue()

def archive(files,dst):
    with zipfile.ZipFile(dst,"w",compression=zipfile.ZIP_STORED) as z:
        for name in sorted(files):
            info=zipfile.ZipInfo(name,(2020,1,1,0,0,0))
            info.create_system=3;info.external_attr=0o100644<<16
            info.compress_type=zipfile.ZIP_STORED
            z.writestr(info,files[name])
    with zipfile.ZipFile(dst) as z:
        check(z.testzip() is None and len(z.namelist())==16 and
              all(z.read(k)==v for k,v in files.items()),
              "genuine physically readback 16 member original PBR ZIP")

def main(original,out):
    doc,source= frozen(original)
    pngs={}
    for i,name in enumerate(NAMES):
        base=source_base_png(doc,source,i)
        normal,orm=physical_maps(base,i)
        pngs[f"{OUTPUT}-{name}-normal.png"]=normal
        pngs[f"{OUTPUT}-{name}-orm.png"]=orm
    model=build_model(doc,source,pngs)
    files={OUTPUT+".glb":model,**pngs}
    for kind in ("normal","orm"):
        files[f"{OUTPUT}-{kind}-six-material-map-atlas.png"]=atlas(kind,pngs)
    receipt={
      "package_id":PKG,"version":VERS,"owner":"B01-31",
      "physical_immutable_W19_source_GLB_sha256":SOURCE_SHA,
      "physical_original_W19_BIN_byte_prefix_sha256":H(source),
      "physical_original_W19_BIN_byte_prefix_size":len(source),
      "original_meshes":2,"original_triangles":1312,"original_UV_primitives":12,
      "original_baseColor_embedded_images_unchanged":6,
      "new_embedded_normal_images":6,"new_embedded_ORM_images":6,
      "real_original_RGB_PNG_map_files":12,
      "actual_real_GLB_sha256":H(model),
      "real_atlas_1800x740_PNGs":2,
      "real_distribution_ZIP_members":16,
      "files":[{"file":name,"sha256":H(payload),"bytes":len(payload)}
               for name,payload in sorted(files.items())],
      "frozen_510_sha":"d94400d95d13db7f27e1533df05c9292d5380431",
      "frozen_556_sha":"33094f154c7b91119b36d58aab285e6e83c5de57",
      "frozen_596_sha":"efb2b06eafedab5469b57f163152d96aca3c5915",
      "Unity_G1":"NOT_RUN","Live_SaveLoad":"NOT_RUN",
      "B01_40_formal_G0":"PENDING","G2":"HOLD",
      "Codex_ACK":False,"integrated":False}
    files["receipt.json"]=(json.dumps(receipt,sort_keys=True,indent=2)+"\n").encode()
    out.mkdir(parents=True,exist_ok=True)
    for name,payload in files.items():
        (out/name).write_bytes(payload)
        check((out/name).read_bytes()==payload,"actual original output byte write/readback")
    archive(files,out.parent/(out.name+"-physical.zip"))
    print(json.dumps({"status":"PASS","physical_original_GLTF2":1,
                      "immutable_original_W19_bin_prefix_bytes":len(source),
                      "original_MATERIALS":6,"new_original_tangent_NORMAL_PNGs":6,
                      "new_original_PACKED_ORM_PNGs":6,
                      "real_GLTF2_embedded_images_total":18,
                      "real_material_normal_texture_bindings":6,
                      "real_material_ORM_bindings":12,
                      "actual_review_PNGs":2,
                      "physical_ZIP_members":16,
                      "GLB_SHA256":H(model),"native_Unity":"NOT_RUN"}))
if __name__=="__main__":
    p=argparse.ArgumentParser()
    p.add_argument("--original",required=True)
    p.add_argument("--output",required=True)
    a=p.parse_args()
    try:main(Path(a.original),Path(a.output))
    except Exception as exc:
        print("B01-31_W20_AUTHOR_FAIL "+type(exc).__name__+": "+str(exc),file=sys.stderr)
        raise
