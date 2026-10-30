#!/usr/bin/env python3
"""Independent READ-ONLY physical 3D glTF2 UV/PBR/PNG receiver for B01-31 W19.
Never imports build_livery.py, no recompilation of source W16. Decode immutable
real original GLB geometry and independently validate all new embedded PNGs.
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
from PIL import Image

PKG="B01-31-COUPLER-UV-WEAR-LIVERY-001"
OUT="B0131SplitCouplerWornLivery"
ORIGINAL="B0131SplitCouplerConcept"
SOURCE_SHA="eeae6a553ca6136b5eaa97a65d5dd995f5f28fcb8bc54eb2180dfe28bc3653d7"
NAMES=("graphite_shell","rubber_cable","hazard_amber",
       "exposed_copper","insulation_cyan","strain_collar")
H=lambda b:hashlib.sha256(b).hexdigest()
def need(c,reason):
    if not c:raise ValueError("B01-31-W19-INDEPENDENT-PHYSICAL-RED "+reason)

def read_glb(raw):
    need(len(raw)>28 and raw[:4]==b"glTF","GLB2 magic")
    version,size=struct.unpack_from("<II",raw,4)
    need(version==2 and size==len(raw),"physical glTF2 length")
    jlen,jtype=struct.unpack_from("<I4s",raw,12)
    need(jtype==b"JSON" and jlen%4==0 and 28+jlen<=len(raw),
         "physical GLB2 JSON chunk bounds and alignment")
    d=json.loads(raw[20:20+jlen])
    blen,btype=struct.unpack_from("<I4s",raw,20+jlen)
    need(btype==b"BIN\0" and 28+jlen+blen==len(raw),
         "physical GLB2 BIN offset/padding")
    declared=d["buffers"][0]["byteLength"]
    need(blen-declared in (0,1,2,3),"declared BIN size and padded bytes")
    return d,raw[28+jlen:28+jlen+declared]

def accessor(d,binary,index):
    a=d["accessors"][index]
    v=d["bufferViews"][a["bufferView"]]
    comp={"VEC2":2,"VEC3":3}.get(a["type"],0)
    need(a["componentType"]==5126 and comp>0
         and v["buffer"]==0 and v["byteOffset"]%4==0
         and v["byteLength"]==a["count"]*comp*4
         and v["byteOffset"]+v["byteLength"]<=len(binary),
         "physical UV/position float32 count offsets or bounds")
    raw=struct.unpack_from("<"+"f"*(a["count"]*comp),binary,v["byteOffset"])
    need(all(math.isfinite(x) for x in raw),
         "real UV coordinate contains NaN or infinity")
    return a,raw

def check_source_geometry(original,d,buf,sourcebytes):
    need(buf[:len(sourcebytes)]==sourcebytes,
         "byte-for-byte original 102336-byte GLB BIN source immutable")
    for field in ("scenes","nodes","scene"):
        need(d[field]==original[field],"immutable source "+field+" altered")
    need(len(d["meshes"])==2 and len(d["materials"])==6
         and len(d["images"])==6 and len(d["textures"])==6
         and len(d["samplers"])==1,
         "2 real geometries x 6 PBR textures, six distinct embedded images")
    need(d["asset"]["extras"]["original_source_GLB_SHA256"]==SOURCE_SHA,
         "real original SHA provenance ungrounded")
    need(len(d["bufferViews"])==54 and len(d["accessors"])==48,
         "actual 6 PNG bufferViews + 12 UV views/float32 accessors")
    need(d["bufferViews"][:36]==original["bufferViews"]
         and d["accessors"][:36]==original["accessors"],
         "even one immutable original geometry accessor/offset changed")
    flat_old=[p for m in original["meshes"] for p in m["primitives"]]
    flat_new=[p for m in d["meshes"] for p in m["primitives"]]
    need(len(flat_old)==len(flat_new)==12,
         "real six material primitives per original static coupler half")
    seen=[]
    for old,p in zip(flat_old,flat_new):
        base_attrs=copy.deepcopy(p["attributes"])
        vid=base_attrs.pop("TEXCOORD_0",None)
        need(vid is not None and base_attrs==old["attributes"]
             and all(p[k]==old[k] for k in ("indices","material","mode")),
             "original geometry/primitives can change ONLY by additive TEXCOORD_0")
        need(36<=vid<48 and vid not in seen,"12 unique original material UV buffers")
        seen.append(vid)
        pos,positions=accessor(d,buf,p["attributes"]["POSITION"])
        uv,coords=accessor(d,buf,vid)
        need(uv["type"]=="VEC2" and uv["count"]==pos["count"],
             "physically valid VEC2 UV count for each original vertex")
        need(uv["bufferView"]>=42 and d["bufferViews"][uv["bufferView"]].get("target")==34962,
             "UV is an actually packed new glTF vertex attribute")
        need(all(-0.00001<=v<=1.00001 for v in coords),
             "all real original UV within intended texture coordinates")
        need(max(coords[::2])-min(coords[::2])>.003
             and max(coords[1::2])-min(coords[1::2])>.02,
             "each UV primitive has genuine 2D span (not constant 0,0)")
        for i in range(0,len(positions),3):
            x,y,z=positions[i:i+3]
            u=min(1.,max(0.,(x+.85)/1.70))
            v=(math.atan2(z,y)+math.pi)/(2*math.pi)
            j=(i//3)*2
            need(abs(coords[j]-u)<1e-6 and abs(coords[j+1]-v)<1e-6,
                 "binary UV source geometry mapping differs from original true x/angle")
    need(len(seen)==12,"all twelve source material primitives separately UV textured")
    for i,name in enumerate(NAMES):
        before=copy.deepcopy(original["materials"][i])
        after=copy.deepcopy(d["materials"][i])
        new_texture=after["pbrMetallicRoughness"].pop("baseColorTexture",None)
        need(new_texture=={"index":i,"texCoord":0} and after==before,
             "original six PBR materials changed beyond additive baseColorTexture")
        tex=d["textures"][i]
        need(tex["source"]==i and tex["sampler"]==0,
             "material texture points to wrong original image")
        image=d["images"][i]
        need(image["mimeType"]=="image/png"
             and image["name"]==name+"_original_wear_png",
             "genuine six original embedded PNG images with explicit MIME")
        view=d["bufferViews"][image["bufferView"]]
        need(36<=image["bufferView"]<=41 and
             view.get("target") is None and view["byteOffset"]%4==0 and
             view["byteOffset"]+view["byteLength"]<=len(buf),
             "embedded image physical bufferView changed")
    return len(seen)

def check_images(root,d,buf):
    for i,name in enumerate(NAMES):
        path=OUT+"-"+name+"-basecolor.png"
        raw=(root/path).read_bytes()
        view=d["bufferViews"][d["images"][i]["bufferView"]]
        actual=buf[view["byteOffset"]:view["byteOffset"]+view["byteLength"]]
        need(raw==actual and raw.startswith(b"\x89PNG\r\n\x1a\n"),
             "physically embedded original PNG bytewise equal standalone PNG")
        with Image.open(io.BytesIO(raw)) as image:
            image.load()
            need(image.format=="PNG" and image.mode=="RGBA"
                 and image.size==(256,256),"actual original UV PNG 256px RGBA")
            colors=image.getcolors(65536)
            need(colors is not None and len(colors)>=5
                 and image.getextrema()[3]==(255,255),
                 "original texture is a genuinely painted opaque industrial surface")
    # Specific hazard stripes not just bland 6 color squares
    with Image.open(root/(OUT+"-hazard_amber-basecolor.png")) as hazard:
        hazard.load()
        need(hazard.getpixel((6,6))!=hazard.getpixel((29,6)),
             "real diagonal industrial hazard striping, not stock flat swatch")

def check_boards(root):
    names=[OUT+"-dark-three-views.png",OUT+"-light-three-views.png"]
    for name in names:
        with Image.open(root/name) as im:
            im.load()
            need(im.format=="PNG" and im.mode=="RGB"
                 and im.size==(1800,720),"physical original 3 view UV concept PNG")
            for panel in range(3):
                crop=im.crop((panel*600+40,120,panel*600+565,640))
                colors=crop.getcolors(1000000)
                need(colors is not None and len(colors)>=8,
                     "original source textured 3D geometry board missing/bare or flat")
    need((root/names[0]).read_bytes()!=(root/names[1]).read_bytes(),
         "dark/light physically distinct contrast review boards")

def negative_tests(original,d,buf,originalbuf):
    errors=0
    scenarios=[]
    t=copy.deepcopy(d);t["meshes"][0]["primitives"][0]["attributes"].pop("TEXCOORD_0");scenarios.append(t)
    t=copy.deepcopy(d);t["nodes"][0]["mesh"]=1;scenarios.append(t)
    t=copy.deepcopy(d);t["materials"][0]["pbrMetallicRoughness"]["baseColorFactor"]=[0,0,0,1];scenarios.append(t)
    t=copy.deepcopy(d);t["materials"][0]["pbrMetallicRoughness"]["baseColorTexture"]["index"]=4;scenarios.append(t)
    t=copy.deepcopy(d);t["accessors"][0]["count"]+=1;scenarios.append(t)
    t=copy.deepcopy(d);t["images"][0]["bufferView"]=53;scenarios.append(t)
    for model in scenarios:
        try:check_source_geometry(original,model,buf,originalbuf)
        except (ValueError,KeyError,IndexError):errors+=1
        else:raise AssertionError("FALSE GREEN original GLB wrong PBR/UV/source")
    need(errors==6,"six semantic binary source negative controls")
    return errors

def main(source,output):
    physical=(source/(ORIGINAL+".glb")).read_bytes()
    need(H(physical)==SOURCE_SHA,"actual original immutable W16 exact GLB source SHA")
    srcreceipt=json.loads((source/"receipt.json").read_bytes())
    need(srcreceipt["package_id"]=="B01-31-SPLIT-COUPLER-CONCEPT3D-001"
         and srcreceipt["total_glb_triangles"]==1312
         and any(x["file"]==ORIGINAL+".glb" and x["sha256"]==SOURCE_SHA
                 for x in srcreceipt["actual_binary_assets"]),
         "original actual W16 physical GLB receipt mismatch")
    old,oldbuf=read_glb(physical)
    rec=json.loads((output/"receipt.json").read_bytes())
    need(rec["package_id"]==PKG and rec["version"]=="P17-W19-R1"
         and rec["original_source_glb_sha256"]==SOURCE_SHA
         and rec["original_triangles"]==1312
         and rec["real_source_primitives_UV_mapped"]==12
         and rec["real_original_embedded_glTF_images"]==6
         and rec["actual_zip_members"]==10,
         "W19 original artifact receipt lineage and physical expected counts")
    need(rec["native_Unity"]=="NOT_RUN" and rec["live_game_SaveLoad"]=="NOT_RUN"
         and rec["G2"]=="HOLD" and rec["integrated"] is False
         and rec["selected_R3"]=="OWNER_DECISION_PENDING #642/#644"
         and rec["immutable_selected_R2_sha"]=="efb2b06eafedab5469b57f163152d96aca3c5915",
         "source art cannot impersonate native/Save/Load/R3 authorization")
    raw=(output/(OUT+".glb")).read_bytes()
    need(H(raw)==rec["true_new_textured_GLB_sha256"],
         "actual original textured GLB receipt SHA mismatch")
    d,buf=read_glb(raw)
    need(rec["unchanged_original_geometry_prefix_bytes"]==len(oldbuf)
         and H(oldbuf)==rec["unchanged_original_geometry_BIN_prefix_sha256"],
         "actual source original binary SHA/prefix")
    mapped=check_source_geometry(old,d,buf,oldbuf)
    check_images(output,d,buf)
    check_boards(output)
    expected={OUT+".glb","receipt.json",
             OUT+"-dark-three-views.png",OUT+"-light-three-views.png"}
    expected|={OUT+"-"+name+"-basecolor.png" for name in NAMES}
    listed={x["name"]:x for x in rec["files"]}
    need(set(listed)==expected-{"receipt.json"} and len(listed)==9,
         "nine actual source art files plus one machine receipt")
    for path,receipt in listed.items():
        rawfile=(output/path).read_bytes()
        need(H(rawfile)==receipt["sha256"] and
             len(rawfile)==receipt["bytes"],
             "actual physical standalone PNG/GLB/board file hash/size")
    zipfile_path=output.parent/(output.name+"-physical.zip")
    with zipfile.ZipFile(zipfile_path) as z:
        need(z.testzip() is None and len(z.namelist())==10
             and set(z.namelist())==expected,
             "genuine 10-member physical art ZIP membership and CRC")
        need(all(z.read(name)==(output/name).read_bytes() for name in expected),
             "real 10-member original ZIP bytes exactly match loose files")
    invalid=negative_tests(old,d,buf,oldbuf)
    for corruption in (b"NOPE"+raw[4:],raw[:8]+struct.pack("<I",len(raw)+4)+raw[12:]):
        try:read_glb(corruption)
        except (ValueError,struct.error,json.JSONDecodeError):invalid+=1
        else:raise AssertionError("FALSE GREEN corrupt real glTF2 header")
    need(invalid==8,"eight adversarial mutations rejected")
    print(json.dumps({"status":"PASS","original_geometry_untouched_bytes":len(oldbuf),
        "actual_UV_mapped_glTF_2_primitives":mapped,
        "actual_original_PBR_textured_materials":6,
        "actual_binary_embedded_PNGs":6,
        "actual_original_dark_light_3view_boards":2,
        "actual_10_member_physical_ZIP":True,
        "negative_mutants_rejected":invalid,
        "original_texture_GLb_sha256":H(raw),
        "native_unity":"NOT_RUN","game_save":"NOT_RUN"}))
if __name__=="__main__":
    p=argparse.ArgumentParser()
    p.add_argument("--original",required=True)
    p.add_argument("--output",required=True)
    a=p.parse_args()
    try:main(Path(a.original),Path(a.output))
    except Exception as e:
        print("W19_INDEPENDENT_PHYSICAL_RECEIVER_RED "+type(e).__name__+": "+str(e),file=sys.stderr)
        raise
