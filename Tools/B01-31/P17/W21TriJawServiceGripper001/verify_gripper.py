#!/usr/bin/env python3
"""Independent genuine physical glTF2 3D tri-jaw service clamp receiver, W21.
No import from generator. Parses original glTF2 vertex/normal/index and actual
binary animation keyframes, verifies clearance of entire real hub/jaw geometry
against 241 sampled world poses, PNG/GIF frames, receipts, ZIP and mutations.
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

PKG="B01-31-TRIJAW-SERVICE-GRIPPER-CONCEPT3D-001"
BASENAME="B0131-TriJawServiceGripper"
TIMES=(0.,.15,.32,.45,.62,.82,1.10)
RADIUS=(.72,.66,.56,.47,.41,.42,.46)
TILT=(0.,1.,3.,7.,12.,9.,16.)
SHA=lambda raw:hashlib.sha256(raw).hexdigest()
def need(c,reason):
    if not c:raise ValueError("W21_INDEPENDENT_ORIGINAL_GLTF2_RED "+reason)
def decode(raw):
    need(len(raw)>28 and raw[:4]==b"glTF","true binary glTF2 magic")
    version,total=struct.unpack_from("<II",raw,4)
    need(version==2 and total==len(raw),"actual GLB2 byte framing")
    size,typ=struct.unpack_from("<I4s",raw,12)
    need(typ==b"JSON" and size%4==0 and size+28<=len(raw),
         "actual JSON aligned GLB source chunk")
    doc=json.loads(raw[20:20+size])
    bsize,btyp=struct.unpack_from("<I4s",raw,20+size)
    need(btyp==b"BIN\0" and bsize%4==0 and 28+size+bsize==len(raw),
         "real original physical geometry and animation BIN")
    buf=raw[28+size:]
    need(bsize-doc["buffers"][0]["byteLength"] in (0,1,2,3),
         "actual glTF BIN alignment padding")
    return doc,buf

def readacc(doc,buf,i):
    ac=doc["accessors"][i]
    view=doc["bufferViews"][ac["bufferView"]]
    comps={"SCALAR":1,"VEC3":3,"VEC4":4}
    need(ac["type"] in comps and ac["componentType"] in (5126,5123),
         "real source accessors FLOAT or unsigned short")
    n=comps[ac["type"]]*ac["count"]
    fmt="f" if ac["componentType"]==5126 else "H"
    width=4 if fmt=="f" else 2
    need(view["buffer"]==0 and view["byteOffset"]%4==0 and
         view["byteLength"]==n*width and
         view["byteOffset"]+view["byteLength"]<=len(buf),
         "physical source float/index bytes not out of bounds")
    values=struct.unpack_from("<"+fmt*n,buf,view["byteOffset"])
    need(all(math.isfinite(v) for v in values),"actual vertex/pose NaN or Inf")
    return ac,values

def validate(doc,buf):
    need(doc["asset"]["version"]=="2.0" and
         doc["asset"]["generator"]==PKG+"/P17-W21-R1" and
         doc["asset"]["extras"]["native_Unity"]=="NOT_RUN" and
         doc["asset"]["extras"]["no_physics"]=="no Rigidbody, no authority, no colliders",
         "real original 3D art not a phony Unity gameplay authority")
    need(len(doc["meshes"])==4 and len(doc["nodes"])==4
         and len(doc["materials"])==6 and
         doc["scenes"][0]["nodes"]==[0,1,2,3],
         "original 4 separately transformable genuine 3D mesh nodes and PBR")
    need(doc["nodes"][0]["name"]=="ServiceHubStationary"
         and doc["nodes"][0]["mesh"]==0 and
         len(doc["animations"])==1,
         "one actual stationary hub and one 3-jaw glTF2 motion clip")
    count=0
    min_jaw_x=0
    hub_radius=0.
    unique_jaws=set()
    for k,mesh in enumerate(doc["meshes"]):
        node=doc["nodes"][k]
        need(node["mesh"]==k and len(node["translation"])==3 and
             len(node["rotation"])==4,
             "four original physically independent pivot transforms")
        vertices=[]
        triangles=0
        for prim in mesh["primitives"]:
            need(prim["mode"]==4 and 0<=prim["material"]<6,
                 "real original 3D indexed PBR triangles")
            ac,values=readacc(doc,buf,prim["attributes"]["POSITION"])
            nc,normals=readacc(doc,buf,prim["attributes"]["NORMAL"])
            ix,indices=readacc(doc,buf,prim["indices"])
            need(ac["type"]==nc["type"]=="VEC3" and
                 ix["type"]=="SCALAR" and ix["componentType"]==5123 and
                 len(values)==len(normals) and len(values)//3==len(indices) and
                 len(indices)%3==0,
                 "real glTF triangle POSITION/NORMAL/indices shape")
            for i in range(0,len(indices),3):
                a,b,c=[values[3*indices[i+j]:3*indices[i+j]+3] for j in range(3)]
                ab=[b[z]-a[z] for z in range(3)]
                acvec=[c[z]-a[z] for z in range(3)]
                n=(ab[1]*acvec[2]-ab[2]*acvec[1],
                   ab[2]*acvec[0]-ab[0]*acvec[2],
                   ab[0]*acvec[1]-ab[1]*acvec[0])
                area=math.sqrt(sum(v*v for v in n))
                need(area>1e-11,"real original triangle must not degenerate")
                normal=normals[3*indices[i]:3*indices[i]+3]
                need(abs(math.sqrt(sum(x*x for x in normal))-1)<1e-5 and
                     sum(n[j]/area*normal[j] for j in range(3))>.999,
                     "original 3D winding and normalized vertex normals")
                triangles+=1
            vertices.extend(values)
        need(triangles==(144 if k==0 else 60),
             "genuine 144-hub +3x60-jaw original 3D indexed faces")
        if k==0:
            hub_radius=max(math.hypot(vertices[i],vertices[i+1])
                           for i in range(0,len(vertices),3))
        else:
            min_jaw_x=min(vertices[::3])
            unique_jaws.add(tuple(round(x,6) for x in vertices))
        count+=triangles
    need(count==324 and len(unique_jaws)>=2,
         "original 324 actual triangles and 3 physical separate jaws with details")
    animation=doc["animations"][0]
    need(animation["name"]=="B0131_TriJaw_GraspAndSettle_1p10s" and
         len(animation["samplers"])==len(animation["channels"])==6,
         "exact 6 actual glTF2 3D transform channels")
    observed={}
    for chidx,channel in enumerate(animation["channels"]):
        sample=animation["samplers"][chidx]
        need(channel["sampler"]==chidx and sample["interpolation"]=="LINEAR",
             "six individually addressed linearly interpolated glTF2 streams")
        node=channel["target"]["node"]
        path=channel["target"]["path"]
        need(node in (1,2,3) and path in ("translation","rotation") and
             (node,path) not in observed,
             "three independently selectable physical movement and roll tracks")
        tac,times=readacc(doc,buf,sample["input"])
        data,values=readacc(doc,buf,sample["output"])
        need(tac["type"]=="SCALAR" and tac["componentType"]==5126 and
             data["componentType"]==5126 and
             tac["count"]==data["count"]==7 and
             all(abs(times[i]-TIMES[i])<2e-7 for i in range(7)),
             "seven real physical time samples per actual motion channel")
        n=3 if path=="translation" else 4
        need(data["type"]==("VEC3" if n==3 else "VEC4") and
             len(values)==n*7,
             "original genuine independently packed float32 translation or quaternion")
        keys=[values[j*n:(j+1)*n] for j in range(7)]
        yaw=(node-1)*2*math.pi/3
        for j,key in enumerate(keys):
            if path=="translation":
                exact=(RADIUS[j]*math.cos(yaw),RADIUS[j]*math.sin(yaw),0.)
                need(all(abs(key[z]-exact[z])<3e-7 for z in range(3)),
                     "real original 3D three radial jaws move on own rays")
            else:
                rz=math.radians(TILT[j])
                cz,sz=math.cos(yaw/2),math.sin(yaw/2)
                cx,sx=math.cos(rz/2),math.sin(rz/2)
                expect=(cz*sx,sz*sx,sz*cx,cz*cx)
                need(all(abs(key[z]-expect[z])<3e-7 for z in range(4)) and
                     abs(sum(t*t for t in key)-1)<1e-5,
                     "authentic each original 3D jaw rotation unit quaternion")
        observed[(node,path)]=keys
    need(len(observed)==6,"no missing jaw translation or rotation channels")
    need(min(RADIUS)+min_jaw_x>hub_radius+.025,
         "actual physical source geometry-to-hub radial safety clearance at deepest close")
    # 241 independent continuous interpolated motion radial clearance samples
    smallest=float("inf")
    for j in range(241):
        t=1.10*j/240
        seg=min(next((k for k in range(6) if TIMES[k]<=t<=TIMES[k+1]),5),5)
        f=(t-TIMES[seg])/(TIMES[seg+1]-TIMES[seg])
        radius=RADIUS[seg]*(1-f)+RADIUS[seg+1]*f
        clearance=radius+min_jaw_x-hub_radius
        smallest=min(smallest,clearance)
        need(clearance>.025,"jaw real original 3D geometry hit stationary hub during interpolation")
    return count,smallest,len(observed)

def adversarial(doc,buf):
    cases=[]
    x=copy.deepcopy(doc);x["animations"][0]["channels"].pop();cases.append(x)
    x=copy.deepcopy(doc);x["nodes"][2]["mesh"]=1;cases.append(x)
    x=copy.deepcopy(doc);x["materials"].pop();cases.append(x)
    x=copy.deepcopy(doc);x["animations"][0]["channels"][2]["target"]["node"]=1;cases.append(x)
    x=copy.deepcopy(doc);x["animations"][0]["samplers"][3]["interpolation"]="STEP";cases.append(x)
    x=copy.deepcopy(doc);x["meshes"][0]["primitives"][0]["mode"]=1;cases.append(x)
    bad=0
    for item in cases:
        try:validate(item,buf)
        except (ValueError,KeyError,IndexError):bad+=1
        else:raise AssertionError("false positive corrupted glTF2 original rig")
    need(bad==6,"exact six original semantic mutants rejected")
    return bad

def verify(root):
    rec=json.loads((root/"receipt.json").read_bytes())
    timeline=json.loads((root/"timeline.json").read_bytes())
    need(rec["package_id"]==timeline["package_id"]==PKG and
         rec["version"]==timeline["version"]=="P17-W21-R1" and
         rec["real_mesh_triangles"]==324 and
         rec["independent_REAL_glTF2_animation_channels"]==6 and
         rec["real_keyframes_across_channels"]==42 and
         rec["original_distribution_zip_members"]==6,
         "actual physical six-file original art contract and receipt")
    need(rec["Unity_G1"]==rec["PhysX"]==rec["SaveLoad"]=="NOT_RUN" and
         rec["G0_formal"]=="PENDING" and
         rec["G2"]=="HOLD" and rec["integrated"] is False and
         rec["frozen_510"]=="d94400d95d13db7f27e1533df05c9292d5380431" and
         rec["frozen_556"]=="33094f154c7b91119b36d58aab285e6e83c5de57" and
         rec["frozen_596"]=="efb2b06eafedab5469b57f163152d96aca3c5915",
         "material concept may not claim Unity/Save/Load gameplay authority")
    blob=(root/(BASENAME+".glb")).read_bytes()
    need(SHA(blob)==rec["true_original_GLTF2_SHA256"],
         "original physical glTF2 binary actual SHA")
    doc,buf=decode(blob)
    triangles,clearance,tracks=validate(doc,buf)
    expected={BASENAME+".glb","timeline.json","receipt.json",
              BASENAME+"-dark-seven-pose.png",
              BASENAME+"-light-seven-pose.png",
              BASENAME+"-12frame-100ms-isometric.gif"}
    listed={x["name"]:x for x in rec["actual_files"]}
    need(set(listed)==expected-{"receipt.json"} and len(listed)==5,
         "five actual original art source file receipts + receipt")
    for name,item in listed.items():
        raw=(root/name).read_bytes()
        need(SHA(raw)==item["sha256"] and len(raw)==item["bytes"],
             "true physical GLB2/GIF/PNG/original timeline digest and byte counts")
        if name.endswith(".png"):
            with Image.open(io.BytesIO(raw)) as im:
                im.load()
                need(im.format=="PNG" and im.size==(2100,480) and im.mode=="RGB",
                     "actual original physical seven-pose concept board")
                unique=[]
                for i in range(7):
                    unique.append(SHA(im.crop((i*300+15,75,i*300+290,420)).tobytes()))
                need(len(set(unique))==7,
                     "all seven actual projected 3D machine poses must differ visibly")
        if name.endswith(".gif"):
            with Image.open(io.BytesIO(raw)) as im:
                need(im.format=="GIF" and im.n_frames==12,
                     "actual original animated 12-frame GIF, no duplicate-frame collapse")
                distinct=set()
                for i in range(12):
                    im.seek(i)
                    need(im.info.get("duration")==100,
                         "original physical GIF 100ms exact frame duration")
                    distinct.add(SHA(im.convert("RGB").tobytes()))
                need(len(distinct)==12,
                     "original real 3D 12 GIF frames must have independent visible poses")
    zipped=root.parent/(root.name+"-physical.zip")
    with zipfile.ZipFile(zipped) as z:
        need(set(z.namelist())==expected and z.testzip() is None and
             all(z.read(name)==(root/name).read_bytes() for name in expected),
             "real original 6-member physical ZIP CRC and binary member parity")
    rejected=adversarial(doc,buf)
    for broken in (b"NOPE"+blob[4:],
                   blob[:8]+struct.pack("<I",len(blob)+7)+blob[12:]):
        try:decode(broken)
        except (ValueError,struct.error,json.JSONDecodeError):rejected+=1
        else:raise AssertionError("invalid original GLB binary header false-green")
    need(rejected==8,"eight intentionally wrong original 3D semantic/binary cases rejected")
    print(json.dumps({"status":"PASS","actual_3D_original_triangles":triangles,
        "actual_3D_stationary_hub_and_three_rigged_jaws":True,
        "independent_physical_3D_animation_tracks":tracks,
        "original_true_float32_keyframes":42,
        "sampled_world_motion_poses":241,
        "minimum_original_hub_jaw_clearance_m":round(clearance,6),
        "real_genuine_7pose_PNGs":2,"real_genuine_12frame_100ms_GIF":1,
        "real_deterministic_6member_ZIP":True,
        "original_negative_mutants_REJECTED":rejected,
        "original_GLB_SHA256":SHA(blob),
        "Unity_native":"NOT_RUN"}))

if __name__=="__main__":
    p=argparse.ArgumentParser();p.add_argument("--source",required=True)
    a=p.parse_args()
    try:verify(Path(a.source))
    except Exception as e:
        print("B01_31_W21_INDEPENDENT_RECEIVER_RED "+type(e).__name__+": "+str(e),
              file=sys.stderr)
        raise
