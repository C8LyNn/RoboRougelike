#!/usr/bin/env python3
"""B01-31 W20 INDEPENDENT real GLB2 image/texture/PBR/geometry verifier.
Never imports or calls build_materials.py. Verifies physical original W19
source exact SHA, real embedded 6 normal+6 packed ORM maps, original BIN
geometry/UV/material basecolor unchanged, physical file digests, 8 mutants.
No claims of Unity/PhysX/host activation.
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

PKG="B01-31-COUPLER-PBR-MICRODETAIL-NORMORM-001"
BASE="B0131SplitCouplerMicrodetailPBR"
SRC="B0131SplitCouplerWornLivery"
SRC_SHA="f8683e5ee3f0916fbde534895e5e01f890bf577b0083858e8dc361b1f8d5ae41"
NAMES=("graphite_shell","rubber_cable","hazard_amber","exposed_copper",
       "insulation_cyan","strain_collar")
H=lambda b:hashlib.sha256(b).hexdigest()
def need(condition,what):
    if not condition:raise ValueError("W20_INDEPENDENT_PHYSICAL_PBR_REJECT "+what)

def glb(blob):
    need(len(blob)>=28 and blob[:4]==b"glTF","physical glTF2 header")
    version,total=struct.unpack_from("<II",blob,4)
    need(version==2 and total==len(blob),"actual GLB2 entire binary size")
    jlen,jtype=struct.unpack_from("<I4s",blob,12)
    need(jtype==b"JSON" and jlen%4==0 and jlen+28<=len(blob),
         "JSON binary framing")
    doc=json.loads(blob[20:20+jlen])
    blen,btype=struct.unpack_from("<I4s",blob,20+jlen)
    need(btype==b"BIN\0" and jlen+28+blen==len(blob),
         "actual BIN chunk precise byte count")
    declared=doc["buffers"][0]["byteLength"]
    need(blen-declared in (0,1,2,3),"BIN padding")
    return doc,blob[28+jlen:28+jlen+declared]

def verify_document(original,doc,origbytes,newbytes):
    need(doc["asset"]["version"]=="2.0" and
         doc["asset"]["extras"]["source_W19_GLB_SHA256"]==SRC_SHA and
         doc["asset"]["generator"]==PKG+"/P17-W20-R1",
         "original model SHA lineage source owner and glTF metadata")
    need(newbytes[:len(origbytes)]==origbytes,
         "actual W19 all original mesh/normal/index/UV/image bytes changed")
    for key in ("scenes","nodes","meshes","accessors","scene","samplers"):
        need(doc[key]==original[key],
             "W19 untouched actual source "+key+" changed")
    need(doc["bufferViews"][:len(original["bufferViews"])]==
         original["bufferViews"] and len(original["bufferViews"])==54,
         "all original W19 UV/PNG/vertex bufferViews must remain intact")
    need(doc["images"][:6]==original["images"] and
         doc["textures"][:6]==original["textures"],
         "W19 six originally embedded basecolor image references must not change")
    need(len(doc["images"])==len(doc["textures"])==18 and
         len(doc["bufferViews"])==66 and len(doc["accessors"])==48 and
         len(doc["materials"])==6,
         "six original baseColors plus twelve new physically packed images")
    need(len(doc["meshes"])==2 and len(doc["nodes"])==2 and
         all(len(x["primitives"])==6 for x in doc["meshes"]),
         "both original W19 immutable pivot geometries and twelve primitives")
    need(len(doc["samplers"])==1 and
         doc["samplers"][0]==original["samplers"][0],
         "original source sampler modes unchanged")
    for i,name in enumerate(NAMES):
        source_mat=copy.deepcopy(original["materials"][i])
        current=copy.deepcopy(doc["materials"][i])
        normal=current.pop("normalTexture",None)
        occ=current.pop("occlusionTexture",None)
        metallic=current["pbrMetallicRoughness"].pop("metallicRoughnessTexture",None)
        need(current==source_mat and source_mat["name"]==name,
             "immutable original W19 PBR factor/basecolor/reference changed")
        need(normal=={"index":6+2*i,"texCoord":0,"scale":1.0},
             "real six correct standard glTF NORMAL texture bindings")
        need(occ=={"index":7+2*i,"texCoord":0,"strength":1.0} and
             metallic=={"index":7+2*i,"texCoord":0},
             "real same R AO / G roughness / B metallic standard glTF texture")
        for j,kind in enumerate(("normal","orm")):
            imageidx=6+2*i+j
            tex=doc["textures"][imageidx]
            im=doc["images"][imageidx]
            need(tex["source"]==imageidx and tex["sampler"]==0 and
                 im["name"]==name+"_"+kind+"_original_microdetail" and
                 im["mimeType"]=="image/png",
                 "physical newly embedded image/tex reference mapping")
            v=doc["bufferViews"][im["bufferView"]]
            need(im["bufferView"]==54+2*i+j and
                 v["buffer"]==0 and v["byteOffset"]%4==0 and
                 v.get("target") is None and
                 v["byteOffset"]>=len(origbytes) and
                 v["byteOffset"]+v["byteLength"]<=len(newbytes),
                 "actual new image resides in appended original GLB BIN bytes")
    return 12

def verify_images(root,doc,buf):
    total=0
    extrema={}
    for i,name in enumerate(NAMES):
        for j,kind in enumerate(("normal","orm")):
            fn=f"{BASE}-{name}-{kind}.png"
            raw=(root/fn).read_bytes()
            im=doc["images"][6+2*i+j]
            view=doc["bufferViews"][im["bufferView"]]
            embedded=buf[view["byteOffset"]:view["byteOffset"]+view["byteLength"]]
            need(raw==embedded and raw.startswith(b"\x89PNG\r\n\x1a\n"),
                 "real original RGB image physically embedded and standalone byte-equal")
            with Image.open(io.BytesIO(raw)) as m:
                m.load()
                need(m.format=="PNG" and m.mode=="RGB" and m.size==(256,256),
                     "each real source original normal/ORM is genuine 256x256 RGB PNG")
                colors=m.getcolors(maxcolors=65536)
                need(colors is not None and len(colors)>25,
                     "not a flat or placeholder PNG texture")
                pixel=m.load()
                channel_min=[255]*3;channel_max=[0]*3
                unit_checks=0
                for y in range(0,256,8):
                    for x in range(0,256,8):
                        rgb=pixel[x,y]
                        for c in range(3):
                            channel_min[c]=min(channel_min[c],rgb[c])
                            channel_max[c]=max(channel_max[c],rgb[c])
                        if kind=="normal":
                            n=[v/255*2-1 for v in rgb]
                            length=math.sqrt(sum(v*v for v in n))
                            need(abs(length-1)<.023 and rgb[2]>=160,
                                 "actual tangent-space normal RGB does not decode to a unit forward vector")
                            unit_checks+=1
                        else:
                            need(170<=rgb[0]<=250 and
                                 62<=rgb[1]<=240 and
                                 0<=rgb[2]<=255,
                                 "actual R ambient-occlusion/G roughness/B metallic channel ranges")
                if kind=="normal":
                    need(channel_max[0]-channel_min[0]>=3 and
                         channel_max[1]-channel_min[1]>=3,
                         "actual newly painted normal map is not flat tangent normal")
                    need(unit_checks==1024,"real sampled normal RGB unit count")
                else:
                    need(channel_max[1]-channel_min[1]>8 and
                         channel_max[2]-channel_min[2]>6,
                         "ORM roughness or metallic microdetail lacks original variation")
                extrema[fn]={"low":channel_min,"high":channel_max}
                total+=1
    return total,extrema

def verify_boards(out):
    names=[f"{BASE}-{kind}-six-material-map-atlas.png" for kind in ("normal","orm")]
    for fn in names:
        with Image.open(out/fn) as image:
            image.load()
            need(image.format=="PNG" and image.mode=="RGB" and
                 image.size==(1800,740),"genuine original 6-material PBR review board")
            tiles=[]
            for i in range(6):
                ox=(i%3)*600+175
                oy=60+(i//3)*335+45
                cropped=image.crop((ox,oy,ox+200,oy+200))
                colors=cropped.getcolors(maxcolors=1000000)
                need(colors is not None and len(colors)>20,
                     "six original map atlas has missing/unpainted 256px tile")
                tiles.append(H(cropped.tobytes()))
            need(len(set(tiles))==6,"each of six original shader material maps must have distinct pixels")
    need((out/names[0]).read_bytes()!=(out/names[1]).read_bytes(),
         "tangent normal and packed ORM visual boards are not the same")

def negative_inputs(original,doc,origbin,newbin):
    cases=[]
    d=copy.deepcopy(doc);d["materials"][0].pop("normalTexture");cases.append(d)
    d=copy.deepcopy(doc);d["materials"][0]["occlusionTexture"]["index"]=6;cases.append(d)
    d=copy.deepcopy(doc);d["materials"][0]["pbrMetallicRoughness"]["metallicRoughnessTexture"]["index"]=8;cases.append(d)
    d=copy.deepcopy(doc);d["images"][6]["bufferView"]=0;cases.append(d)
    d=copy.deepcopy(doc);d["accessors"][0]["count"]+=1;cases.append(d)
    d=copy.deepcopy(doc);d["materials"][2]["pbrMetallicRoughness"]["baseColorTexture"]["index"]=5;cases.append(d)
    fail=0
    for mutated in cases:
        try:verify_document(original,mutated,origbin,newbin)
        except (ValueError,KeyError,IndexError):fail+=1
        else:raise AssertionError("false GREEN wrongly textured/overwritten original glTF")
    need(fail==6,"6 corrupted original material/source mutation controls")
    return fail

def run(source,out):
    original_bytes=(source/(SRC+".glb")).read_bytes()
    need(H(original_bytes)==SRC_SHA,"source original W19 physical file SHA256 mismatch")
    receipt=json.loads((source/"receipt.json").read_bytes())
    need(receipt["package_id"]=="B01-31-COUPLER-UV-WEAR-LIVERY-001" and
         receipt["version"]=="P17-W19-R1" and
         receipt["true_new_textured_GLB_sha256"]==SRC_SHA,
         "W19 original full source receipt must match exact original binary SHA")
    original,origbin=glb(original_bytes)
    machine=json.loads((out/"receipt.json").read_bytes())
    need(machine["package_id"]==PKG and machine["version"]=="P17-W20-R1" and
         machine["physical_immutable_W19_source_GLB_sha256"]==SRC_SHA and
         machine["physical_original_W19_BIN_byte_prefix_sha256"]==H(origbin) and
         machine["physical_original_W19_BIN_byte_prefix_size"]==len(origbin),
         "W20 physical machine manifest or frozen original proof changed")
    need(machine["Unity_G1"]=="NOT_RUN" and
         machine["Live_SaveLoad"]=="NOT_RUN" and
         machine["G2"]=="HOLD" and
         machine["integrated"] is False and
         machine["B01_40_formal_G0"]=="PENDING" and
         machine["frozen_510_sha"]=="d94400d95d13db7f27e1533df05c9292d5380431" and
         machine["frozen_556_sha"]=="33094f154c7b91119b36d58aab285e6e83c5de57" and
         machine["frozen_596_sha"]=="efb2b06eafedab5469b57f163152d96aca3c5915",
         "material art cannot claim host Save/Load/native/merge gate")
    blob=(out/(BASE+".glb")).read_bytes()
    need(H(blob)==machine["actual_real_GLB_sha256"],
         "actually authored new GLB binary SHA digest mismatch")
    doc,data=glb(blob)
    added=verify_document(original,doc,origbin,data)
    count,extrema=verify_images(out,doc,data)
    verify_boards(out)
    names={BASE+".glb","receipt.json"}
    names|={f"{BASE}-{kind}-six-material-map-atlas.png" for kind in ("normal","orm")}
    names|={f"{BASE}-{name}-{kind}.png" for name in NAMES for kind in ("normal","orm")}
    need(len(names)==16 and machine["real_distribution_ZIP_members"]==16
         and machine["new_embedded_normal_images"]==6
         and machine["new_embedded_ORM_images"]==6
         and machine["original_baseColor_embedded_images_unchanged"]==6,
         "real 16 output member schema/source original material count")
    listed={r["file"]:r for r in machine["files"]}
    need(set(listed)==names-{"receipt.json"} and len(listed)==15,
         "all actual new GLB PNG atlas file receipts complete")
    for fn,r in listed.items():
        raw=(out/fn).read_bytes()
        need(H(raw)==r["sha256"] and len(raw)==r["bytes"],
             "exact physical original RGB/GLB source file receipt changed")
    archive=out.parent/(out.name+"-physical.zip")
    with zipfile.ZipFile(archive) as z:
        need(z.testzip() is None and set(z.namelist())==names and
             all(z.read(n)==(out/n).read_bytes() for n in names),
             "actual 16-member immutable distribution ZIP CRC and bytes")
    rejected=negative_inputs(original,doc,origbin,data)
    for corrupt in (b"NOPE"+blob[4:],
                    blob[:8]+struct.pack("<I",len(blob)+4)+blob[12:]):
        try:glb(corrupt)
        except (ValueError,struct.error,json.JSONDecodeError):rejected+=1
        else:raise AssertionError("false GREEN corrupt binary GLB2 header")
    need(rejected==8,"eight independent malformed material/GLB inputs rejected")
    print(json.dumps({"status":"PASS","original_W19_geometry_UV_baseColor_BYTES_preserved":len(origbin),
       "genuine_new_embedded_normal_rgb_PNGs":6,
       "genuine_new_embedded_packed_AO_Rough_Metallic_RGB_PNGs":6,
       "physically_bound_material_maps":added,
       "all_original_AND_new_glTF_images":len(doc["images"]),
       "six_original_industrial_texture_normal_detail_verified":True,
       "actual_review_texture_boards":2,
       "actual_16member_ZIP":True,"negative_corrupt_GLTF2_and_materials_RED":rejected,
       "physical_original_GLB_SHA256":H(blob),"unity_native":"NOT_RUN"}))
if __name__=="__main__":
    p=argparse.ArgumentParser()
    p.add_argument("--original",required=True)
    p.add_argument("--output",required=True)
    a=p.parse_args()
    try:run(Path(a.original),Path(a.output))
    except Exception as e:
        print("B01-31_W20_INDEPENDENT_RECEIVER_FAIL "+type(e).__name__+": "+str(e),file=sys.stderr)
        raise
