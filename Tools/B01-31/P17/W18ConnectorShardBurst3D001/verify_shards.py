#!/usr/bin/env python3
"""Independent actual GIF and glTF2 inspection of original W18 3D debris art.
Does not import producer, does not launch Unity, does not inspect fake logs.
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

PKG="B01-31-CONNECTOR-SHARD-BURST3D-001"
BASE="B0131-ConnectorShardBurst3D"
T=(0.,.08,.20,.36,.60,.88,1.20)
SHA=lambda b:hashlib.sha256(b).hexdigest()

def ok(condition,reason):
    if not condition:raise ValueError("W18_SHARD_INDEPENDENT_BINARY_RED "+reason)

def parse_glb(raw):
    ok(len(raw)>28 and raw[:4]==b"glTF","real glTF2 magic")
    version,size=struct.unpack_from("<II",raw,4)
    ok(version==2 and size==len(raw),"actual GLB2 byte size")
    jlen,jtype=struct.unpack_from("<I4s",raw,12)
    ok(jtype==b"JSON" and jlen%4==0 and jlen+28<=len(raw),
       "actual original JSON chunk bounds")
    doc=json.loads(raw[20:20+jlen])
    offset=20+jlen
    blen,btype=struct.unpack_from("<I4s",raw,offset)
    ok(btype==b"BIN\0" and blen%4==0 and offset+8+blen==len(raw),
       "actual raw binary chunk")
    blob=raw[offset+8:]
    ok(len(blob)-doc["buffers"][0]["byteLength"] in (0,1,2,3),
       "GLB BIN official bytePadding within 0..3")
    return doc,blob

def unpack_accessor(doc,blob,idx):
    a=doc["accessors"][idx]
    v=doc["bufferViews"][a["bufferView"]]
    elems={"SCALAR":1,"VEC3":3,"VEC4":4}
    ok(a["type"] in elems and a["componentType"] in (5126,5123),
       "actual FLOAT or UNSIGNED SHORT accessors only")
    n=a["count"]*elems[a["type"]]
    fmt,size=("f",4) if a["componentType"]==5126 else ("H",2)
    ok(v["byteOffset"]%4==0 and v["buffer"]==0 and
       v["byteOffset"]+v["byteLength"]<=len(blob) and
       v["byteLength"]==n*size,
       "real actual binary accessor alignment/bounds/count")
    values=struct.unpack_from("<"+fmt*n,blob,v["byteOffset"])
    ok(all(math.isfinite(x) for x in values),"actual NaN/Inf authored vertex or animation")
    return a,values

def analyse(doc,blob):
    ok(doc["asset"]["version"]=="2.0" and
       PKG in doc["asset"]["generator"],
       "producer owner/actual GLB2 source")
    ok(len(doc["nodes"])==len(doc["meshes"])==16 and
       len(doc["materials"])==4 and
       doc["scenes"][0]["nodes"]==list(range(16)),
       "real 16 distinct 3D shard root nodes + four materials")
    ok(len(doc["animations"])==1,"one glTF2 original debris animation")
    total_faces=0
    unique_shapes=set()
    for i in range(16):
        node=doc["nodes"][i]
        ok(node["name"]==f"Shard_{i:02d}_IndependentPivot"
           and node["mesh"]==i and
           len(node["translation"])==3 and len(node["rotation"])==4,
           "source each original 3D separate pivot and start pose")
        mesh=doc["meshes"][i]
        ok(mesh["name"]==f"Shard_{i:02d}_PrismMesh"
           and len(mesh["primitives"])==1,
           "each shard one distinct prism primitive")
        prim=mesh["primitives"][0]
        ok(prim["material"]==i%4 and prim["mode"]==4,
           "source four distinct PBR material accents and triangles")
        _,coords=unpack_accessor(doc,blob,prim["attributes"]["POSITION"])
        _,normals=unpack_accessor(doc,blob,prim["attributes"]["NORMAL"])
        _,indices=unpack_accessor(doc,blob,prim["indices"])
        ok(len(coords)==len(normals)==72 and
           list(indices)==list(range(24)),
           "physical actual closed prism 8 triangles with 24 original indexed vertices")
        shape=tuple(round(x,6) for x in coords)
        unique_shapes.add(shape)
        points=[coords[k:k+3] for k in range(0,72,3)]
        for j in range(0,24,3):
            a,b,c=points[j:j+3]
            ab=[b[k]-a[k] for k in range(3)]
            ac=[c[k]-a[k] for k in range(3)]
            n=(ab[1]*ac[2]-ab[2]*ac[1],
               ab[2]*ac[0]-ab[0]*ac[2],
               ab[0]*ac[1]-ab[1]*ac[0])
            length=math.sqrt(sum(x*x for x in n))
            ok(length>1e-9,"physically nondegenerate actual 3D shard triangle")
            encoded=normals[j*3:j*3+3]
            ok(abs(math.sqrt(sum(x*x for x in encoded))-1)<1e-5
               and sum(n[k]/length*encoded[k] for k in range(3))>.999,
               "encoded original winding-normal physical conformance")
            total_faces+=1
    ok(total_faces==128 and len(unique_shapes)>=10,
       "128 real source 3D faces and >9 distinct authored shard shapes")
    animation=doc["animations"][0]
    ok(animation["name"]=="B0131_Shards_OutwardBurst_1p20s"
       and len(animation["samplers"])==32 and
       len(animation["channels"])==32,
       "all 16 unique physical 3D shard tracks translation+rotation")
    all_tracks={}
    reference_time=None
    for k,channel in enumerate(animation["channels"]):
        sampler=animation["samplers"][channel["sampler"]]
        ok(channel["sampler"]==k and sampler["interpolation"]=="LINEAR",
           "real 32 exclusive float linear timeline samplers")
        at,times=unpack_accessor(doc,blob,sampler["input"])
        va,values=unpack_accessor(doc,blob,sampler["output"])
        ok(at["type"]=="SCALAR" and at["count"]==7 and
           va["count"]==7 and at["componentType"]==va["componentType"]==5126,
           "real seven time/key samples and FLOAT32")
        ok(all(abs(times[j]-T[j])<2e-7 for j in range(7)),
           "actual 0..1.20s true 7-key source chronology")
        node=channel["target"]["node"]
        attr=channel["target"]["path"]
        ok(node in range(16) and attr in ("translation","rotation")
           and (node,attr) not in all_tracks,
           "16 actual unique 3D fragment source node trajectories")
        count=3 if attr=="translation" else 4
        ok(va["type"]==("VEC3" if count==3 else "VEC4")
           and len(values)==7*count,
           "real glTF animated translation/quaternion output vectors")
        poses=[values[j*count:(j+1)*count] for j in range(7)]
        if attr=="rotation":
            for q in poses:
                ok(abs(math.sqrt(sum(x*x for x in q))-1)<1e-5,
                   "real original unit 3D rotation quaternion")
        else:
            initial=doc["nodes"][node]["translation"]
            ok(all(abs(poses[0][j]-initial[j])<1e-7 for j in range(3)),
               "actual animated first pose = original initial node")
            distances=[math.dist(p,poses[0]) for p in poses]
            ok(all(distances[j]<=distances[j+1]+1e-7 for j in range(6))
               and distances[-1]>.22,
               "physical source 3D shards truly burst outward without backward rebound")
        all_tracks[(node,attr)]=poses
    ok(len(all_tracks)==32,"all 16 shards independently rotate and fly")
    return total_faces,len(unique_shapes),all_tracks

def verify(root):
    rec=json.loads((root/"receipt.json").read_bytes())
    ok(rec["package_id"]==PKG and rec["version"]=="P17-W18-R1"
       and rec["real_nodes"]==16 and rec["real_meshes"]==16
       and rec["real_animation_channels"]==32
       and rec["animation_keyframes_total"]==224
       and rec["actual_zip_entries"]==8,
       "producer receipt source schema and independent expected counts")
    ok(rec["actual_unity"]=="NOT_RUN" and rec["PhysX"]=="NOT_RUN"
       and rec["SAVELOAD"]=="NOT_RUN" and rec["integrated"] is False,
       "no false native Unity/physx/Save/Load integration assertions")
    data=(root/(BASE+".glb")).read_bytes()
    ok(SHA(data)==rec["true_glb_sha256"],"actual original 3D GLB sha256 source")
    doc,blob=parse_glb(data)
    faces,shapes,tracks=analyse(doc,blob)
    receipt_files={x["path"]:x for x in rec["actual_files"]}
    actual_names={BASE+".glb","timeline.json",
                  BASE+"-dark-7poses.png",BASE+"-light-7poses.png"}
    actual_names|={BASE+"-"+v+"-16frames-80ms.gif"
                   for v in ("iso","side","top")}
    ok(set(receipt_files)==actual_names and len(receipt_files)==7,
       "seven actual source physical deliverables plus receipt")
    for filename,item in receipt_files.items():
        raw=(root/filename).read_bytes()
        ok(SHA(raw)==item["sha256"] and len(raw)==item["bytes"],
           "real original GLB/GIF/storyboard byte hash")
        if filename.endswith(".png"):
            with Image.open(io.BytesIO(raw)) as im:
                im.load()
                ok(im.format=="PNG" and im.mode=="RGB"
                   and im.size==(2100,520),
                   "actual original two 7x3D-pose storyboards in source")
                checks=[SHA(im.crop((i*300+10,60,i*300+280,480)).tobytes())
                        for i in range(7)]
                ok(len(set(checks))==7,
                   "each of 7 original physical shard world poses visibly distinct")
        if filename.endswith(".gif"):
            with Image.open(io.BytesIO(raw)) as im:
                ok(im.format=="GIF" and im.n_frames==16,
                   "real 16-frame 80ms GIF actual animation")
                digests=[]
                for i in range(16):
                    im.seek(i)
                    ok(im.info.get("duration")==80,"actual 80ms GIF duration")
                    digests.append(SHA(im.convert("RGB").tobytes()))
                ok(len(set(digests))==16,
                   "no collapsed frames; genuinely 16 moving 3D camera frames")
    tl=json.loads((root/"timeline.json").read_bytes())
    ok(tl["package"]==PKG and tl["original_3d_prism_shards"]==16
       and tl["real_animation_tracks"]==32
       and tl["real_anim_keyframes"]==224
       and tl["gif_duration_ms"]==1280,
       "source timeline machine-backed exact key/GIF counts")
    ok(tl["selected_save_adapter_pr"]==596
       and tl["frozen_save_adapter_sha"]=="efb2b06eafedab5469b57f163152d96aca3c5915"
       and tl["frozen_reattach_sha"]=="d94400d95d13db7f27e1533df05c9292d5380431"
       and tl["frozen_save_contract_sha"]=="33094f154c7b91119b36d58aab285e6e83c5de57",
       "source-only art cannot alter frozen selected contracts")
    expected=actual_names|{"receipt.json"}
    archive=root.parent/(root.name+"-physical.zip")
    with zipfile.ZipFile(archive) as z:
        ok(z.testzip() is None and set(z.namelist())==expected,
           "original 8-entry physical ZIP actual members/CRC")
        ok(all(z.read(p)==(root/p).read_bytes() for p in expected),
           "actual 8 physical ZIP entries byte equal to loose output")
    negative=0
    broken=[
      b"NOPE"+data[4:],
      data[:8]+struct.pack("<I",len(data)+400)+data[12:]
    ]
    for b in broken:
        try:parse_glb(b)
        except (ValueError,struct.error,json.JSONDecodeError):negative+=1
        else:raise AssertionError("false GREEN invalid glTF2 physical bytes")
    mutants=[]
    d=copy.deepcopy(doc);d["animations"][0]["channels"].pop();mutants.append(d)
    d=copy.deepcopy(doc);d["nodes"][1]["mesh"]=0;mutants.append(d)
    d=copy.deepcopy(doc);d["animations"][0]["channels"][0]["target"]["node"]=1;mutants.append(d)
    d=copy.deepcopy(doc);d["materials"].pop();mutants.append(d)
    d=copy.deepcopy(doc);d["animations"][0]["samplers"][2]["interpolation"]="STEP";mutants.append(d)
    for m in mutants:
        try:analyse(m,blob)
        except (ValueError,KeyError,IndexError):negative+=1
        else:raise AssertionError("false GREEN original animation semantically modified")
    ok(negative==7,"independent seven real source/structure mutants RED")
    print(json.dumps({"status":"PASS","original_physical_3D_shards":16,
      "verified_distinct_3D_shapes":shapes,"real_triangle_faces":faces,
      "actual_glTF_track_channels":32,"physical_KEYFRAMES":224,
      "actual_16x80ms_GIFs":3,"actual_7pose_3D_image_boards":2,
      "actual_8member_ZIP":True,"adversarial_mutants_REJECTED":negative,
      "GLB_SHA256":SHA(data),"actual_Unity":"NOT_RUN"}))
    return 0

if __name__=="__main__":
    p=argparse.ArgumentParser();p.add_argument("--source",required=True)
    a=p.parse_args()
    try:sys.exit(verify(Path(a.source)))
    except Exception as exc:
        print("W18_SHARD_PHYSICAL_RECEIVER_FAIL "+type(exc).__name__+": "+str(exc),file=sys.stderr)
        raise
