#!/usr/bin/env python3
"""Independent W22 read-only glTF2 ORIGINAL 1x2x2 damage-geometry state QA.
Does NOT import generator. Checks actual binary mesh vertex/normal/index faces,
full material identities, physical front-area geometry differences, genuine
three-view RGB images, 9 member ZIP & eight deliberately corrupted inputs.
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

PKG="B01-31-MODULAR-ARMOR-3GEOMETRY-STATES-001"
BASE="B0131ArmorModule1x2x2"
STATES=("OK","DAMAGED","DESTROYED")
COUNTS={"OK":204,"DAMAGED":264,"DESTROYED":288}
MATERIALS=("FrameGraphite","ArmorTitanium","SafetyAmber",
           "CopperInternals","SealRubber","StatusCyan")
SHA=lambda x:hashlib.sha256(x).hexdigest()
def check(x,why):
    if not x:raise ValueError("B0131_W22_INDEPENDENT_REAL_MESH_RED "+why)

def glb(raw):
    check(len(raw)>=28 and raw[:4]==b"glTF","actual glTF2 physical binary magic")
    ver,n=struct.unpack_from("<II",raw,4)
    check(ver==2 and n==len(raw),"actual GLB2 physical length")
    jn,jtyp=struct.unpack_from("<I4s",raw,12)
    check(jtyp==b"JSON" and jn%4==0 and jn+28<=len(raw),
          "actual GLB JSON boundary")
    d=json.loads(raw[20:20+jn])
    bn,btyp=struct.unpack_from("<I4s",raw,20+jn)
    check(btyp==b"BIN\0" and bn%4==0 and 28+jn+bn==len(raw),
          "genuine source geometry BIN framing")
    declared=d["buffers"][0]["byteLength"]
    check(bn-declared in (0,1,2,3),"source packed BIN padding")
    return d,raw[28+jn:28+jn+declared]

def acc(doc,data,i):
    a=doc["accessors"][i]
    v=doc["bufferViews"][a["bufferView"]]
    comps={"SCALAR":1,"VEC3":3}.get(a["type"])
    check(comps is not None and a["componentType"] in (5126,5123),
          "real indexed triangle FLOAT32 or uint16 accessor")
    n=a["count"]*comps
    fmt="f" if a["componentType"]==5126 else "H"
    check(v["byteOffset"]%4==0 and v["byteLength"]==n*(4 if fmt=="f" else 2)
          and v["byteOffset"]+v["byteLength"]<=len(data),
          "real packed mesh buffer count alignment/size")
    vals=struct.unpack_from("<"+fmt*n,data,v["byteOffset"])
    check(all(math.isfinite(z) for z in vals),"actual source mesh contains NaN/Inf")
    return a,vals

def analyse(doc,data,state):
    check(doc["asset"]["version"]=="2.0" and
          doc["asset"]["generator"]==PKG+"/P17-W22-R1" and
          doc["asset"]["extras"]["damage_visual_state"]==state and
          doc["asset"]["extras"]["part_size_grid_xyz"]==[1,2,2] and
          doc["asset"]["extras"]["no_collision"]=="no native Unity/PhysX gameplay authority",
          "actual original geometry state and nonauthority scope")
    check(len(doc["meshes"])==len(doc["nodes"])==1 and
          doc["scene"]==0 and doc["scenes"][0]["nodes"]==[0] and
          doc["nodes"][0]["name"]=="ArmorModule1x2x2_Pivot_"+state and
          doc["nodes"][0]["translation"]==[0,0,0] and
          doc["nodes"][0]["rotation"]==[0,0,0,1],
          "three original models must have identical actually grounded origin and mount pivot")
    check([x["name"] for x in doc["materials"]]==list(MATERIALS)
          and len(doc["materials"])==6,
          "same six physically real glTF2 materials across each source state")
    mesh=doc["meshes"][0]
    check(mesh["name"]=="B0131_Armor_"+state+"_ActualGeometry" and
          len(mesh["primitives"])>=4,
          "real distinct 3D mesh with multiple physically assigned source materials")
    tris=0;verts=[]
    shape=[]
    forward_area=0.
    for p in mesh["primitives"]:
        check(p["mode"]==4 and 0<=p["material"]<6,
              "genuine PBR material TRIANGLES original indexed source")
        a,pos=acc(doc,data,p["attributes"]["POSITION"])
        n,norm=acc(doc,data,p["attributes"]["NORMAL"])
        i,indices=acc(doc,data,p["indices"])
        check(a["type"]==n["type"]=="VEC3" and
              i["type"]=="SCALAR" and i["componentType"]==5123 and
              len(pos)==len(norm) and len(indices)==len(pos)//3 and
              len(indices)%3==0 and
              list(indices)==list(range(len(indices))),
              "actual mesh vertices/indices/normals must be discrete and consistent")
        verts.extend([pos[j:j+3] for j in range(0,len(pos),3)])
        shape.extend(round(z,6) for z in pos)
        for j in range(0,len(indices),3):
            xyz=[pos[3*indices[j+k]:3*indices[j+k]+3] for k in range(3)]
            p0,p1,p2=xyz
            u=[p1[k]-p0[k] for k in range(3)]
            v=[p2[k]-p0[k] for k in range(3)]
            cross=(u[1]*v[2]-u[2]*v[1],
                   u[2]*v[0]-u[0]*v[2],
                   u[0]*v[1]-u[1]*v[0])
            mag=math.sqrt(sum(x*x for x in cross))
            check(mag>1.e-10,"actual original damaged/whole mesh has degenerate triangles")
            stored=norm[3*indices[j]:3*indices[j]+3]
            check(abs(math.sqrt(sum(x*x for x in stored))-1)<2e-5 and
                  sum(stored[k]*cross[k]/mag for k in range(3))>.999,
                  "real triangle original vertex NORMAL+winding")
            # Actual source front plate coverage from normal-facing -X caps,
            # excludes common outer bezel, bolts and shared core.
            if (cross[0]<-1.e-7 and all(pt[0]<-.46 for pt in xyz)
                and all(abs(pt[1])<=.745 and abs(pt[2])<=.745 for pt in xyz)):
                forward_area+=abs(cross[0])/2
            tris+=1
    check(tris==COUNTS[state],
          "physically different original triangulated source geometry count "+state)
    for axis,span in enumerate((1.,2.,2.)):
        coords=[v[axis] for v in verts]
        check(max(coords)-min(coords)<=span+1e-5,
              "original all actual 3D geometry not within 1x2x2 grid envelope")
        check(min(coords)<0 and max(coords)>0,
              "original 3D model must straddle common origin pivot")
    check(.05<forward_area<2.5,
          "physical source must contain actual original state-dependent front armor area")
    return tris,forward_area,SHA(str(shape).encode())

def independent_negative(doc,data,state):
    invalid=[]
    d=copy.deepcopy(doc);d["nodes"][0]["translation"]=[1,0,0];invalid.append(d)
    d=copy.deepcopy(doc);d["asset"]["extras"]["damage_visual_state"]="FAKE";invalid.append(d)
    d=copy.deepcopy(doc);d["meshes"][0]["primitives"][0]["mode"]=1;invalid.append(d)
    d=copy.deepcopy(doc);d["accessors"][0]["count"]+=1;invalid.append(d)
    d=copy.deepcopy(doc);d["materials"].pop();invalid.append(d)
    d=copy.deepcopy(doc);d["meshes"][0]["name"]="Copied_Old_State";invalid.append(d)
    rejects=0
    for bad in invalid:
        try:analyse(bad,data,state)
        except (ValueError,IndexError,KeyError,struct.error):rejects+=1
        else:raise AssertionError("FALSE GREEN corrupt original geometry")
    check(rejects==6,"six actual mutated 3D source conditions MUST fail")
    return rejects

def verify(root):
    receipt=json.loads((root/"receipt.json").read_bytes())
    states=json.loads((root/"states.json").read_bytes())
    check(receipt["package_id"]==states["package_id"]==PKG
          and receipt["version"]==states["version"]=="P17-W22-R1"
          and receipt["actual_standalone_GLBs"]==3 and
          receipt["actual_original_PNG_boards"]==4 and
          receipt["real_ZIP_members"]==9 and
          states["grid_module_envelope_xyz"]==[1,2,2] and
          states["mount_pivot_xyz"]==[0,0,0],
          "original physical receipt and source module reference grid")
    check(receipt["unity_G1"]==receipt["PhysX"]==receipt["gameplay"]=="NOT_RUN"
          and receipt["formal_G0"]=="PENDING"
          and receipt["G2"]=="HOLD" and receipt["integrated"] is False
          and receipt["codex_ACK"] is False
          and receipt["frozen_510_sha"]=="d94400d95d13db7f27e1533df05c9292d5380431"
          and receipt["frozen_556_sha"]=="33094f154c7b91119b36d58aab285e6e83c5de57"
          and receipt["frozen_596_sha"]=="efb2b06eafedab5469b57f163152d96aca3c5915",
          "source art cannot claim real game Save/Load/PhysX/Unity authority")
    counts={};coverage={};shapes={};keys={}
    for s in STATES:
        raw=(root/(BASE+"-"+s+".glb")).read_bytes()
        model,buf=glb(raw)
        ct,area,geo=analyse(model,buf,s)
        counts[s]=ct;coverage[s]=area;shapes[s]=geo
        keys[s]=SHA(raw)
        check(receipt["state_counts"][s]==ct and
              states["three_visual_states"][s]["triangle_count"]==ct and
              states["three_visual_states"][s]["geometry_sha256"]==keys[s],
              "authored actual original physically distinct states and per-GLB SHA receipt")
    check(len(set(shapes.values()))==3 and len(set(keys.values()))==3,
          "three art states MUST actually contain distinct 3D physical mesh bytes not recolored same geometry")
    check(coverage["OK"]>coverage["DAMAGED"]>coverage["DESTROYED"]+.15,
          "source front armor actual physical surface must progressively tear away")
    names={BASE+"-"+s+".glb" for s in STATES}
    names|={BASE+"-"+s+"-three-views.png" for s in STATES}
    names|={BASE+"-three-states-comparison.png","states.json","receipt.json"}
    listed={item["name"]:item for item in receipt["actual_files"]}
    check(len(names)==9 and set(listed)==names-{"receipt.json"},
          "physical 9 actual member originals exactly listed (except receipt itself)")
    views=[]
    for name,rec in listed.items():
        payload=(root/name).read_bytes()
        check(SHA(payload)==rec["sha256"] and len(payload)==rec["bytes"],
              "true original image/glTF byte receipt mismatch")
        if name.endswith(".png"):
            with Image.open(io.BytesIO(payload)) as im:
                im.load()
                check(im.format=="PNG" and im.mode=="RGB" and
                      im.size==(1800,650 if name.endswith("three-states-comparison.png") else 640),
                      "actual original 3D projected PNG image physically decodable and right sizes")
                chunks=[]
                for i in range(3):
                    crops=im.crop((i*600+90,110,i*600+510,540))
                    chunks.append(SHA(crops.tobytes()))
                check(len(set(chunks))==3,
                      "three source cameras/states must show nonduplicate actual original geometry pixels")
                views.append(SHA(im.tobytes()))
    check(len(set(views))==4,"four source physical original geometry art boards all distinct")
    physical=root.parent/(root.name+"-physical.zip")
    with zipfile.ZipFile(physical) as z:
        check(z.testzip() is None and set(z.namelist())==names
              and all(z.read(n)==(root/n).read_bytes() for n in names),
              "real original 9-file physical ZIP CRC and source content byte parity")
    model,data=glb((root/(BASE+"-DAMAGED.glb")).read_bytes())
    rejected=independent_negative(model,data,"DAMAGED")
    for raw in (b"NOPE"+(root/(BASE+"-OK.glb")).read_bytes()[4:],
                (lambda x:x[:8]+struct.pack("<I",len(x)+11)+x[12:])(
                   (root/(BASE+"-DESTROYED.glb")).read_bytes())):
        try:glb(raw)
        except (ValueError,IndexError,KeyError,struct.error,json.JSONDecodeError):rejected+=1
        else:raise AssertionError("FALSE GREEN corrupt glTF header")
    check(rejected==8,"8 real geometry+GLB malformed samples denied")
    print(json.dumps({"status":"PASS",
        "original_three_REAL_distinct_geometries":True,
        "actual_indexed_triangles":counts,
        "actual_front_armor_cover_area_grid_units2":{s:round(coverage[s],4) for s in STATES},
        "original_three_actual_GLTF2_SHA256":keys,
        "actual_four_original_3D_render_boards":4,
        "actual_nine_member_distribution_ZIP_CRC":True,
        "independent_corrupt_inputs_REJECTED":rejected,
        "native_Unity":"NOT_RUN"}))
if __name__=="__main__":
    parser=argparse.ArgumentParser()
    parser.add_argument("--source",required=True)
    args=parser.parse_args()
    try:verify(Path(args.source))
    except Exception as exc:
        print("B01_31_W22_INDEPENDENT_BINARY_RECEIVER_RED "+type(exc).__name__+": "+str(exc),file=sys.stderr)
        raise
