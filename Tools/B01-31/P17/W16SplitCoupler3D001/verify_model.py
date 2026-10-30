#!/usr/bin/env python3
"""Independent actual GLB2 geometry and OBJ interchange validation.
No dependency on producer code, no Blender/Unity/PhysX. Validates physically
saved triangles, normals, two distinct node pivots and >=0.26m copper gap.
"""
from __future__ import annotations
import argparse
import hashlib
import io
import json
import math
from pathlib import Path
import re
import struct
import sys
import zipfile
from PIL import Image

NAME="B0131SplitCouplerConcept"
PKG="B01-31-SPLIT-COUPLER-CONCEPT3D-001"
MATERIALS=("graphite_shell","rubber_cable","hazard_amber",
           "exposed_copper","insulation_cyan","strain_collar")
H=lambda b:hashlib.sha256(b).hexdigest()
def check(value,why):
    if not value: raise ValueError("W16_SPLIT_COUPLER_INDEPENDENT_RECEIVER_REJECT "+why)

def glb_read(raw):
    check(len(raw)>=28,"GLB header truncated")
    magic,version,full=struct.unpack_from("<4sII",raw,0)
    check(magic==b"glTF" and version==2 and full==len(raw),
          "GLB 2.0 magic/version/full byte length")
    jlen,jtype=struct.unpack_from("<I4s",raw,12)
    check(jtype==b"JSON" and jlen>50 and jlen%4==0,
          "GLB JSON chunk header")
    check(20+jlen+8<=len(raw),"GLB JSON overrun")
    gltf=json.loads(raw[20:20+jlen])
    pos=20+jlen
    blen,btype=struct.unpack_from("<I4s",raw,pos)
    check(btype==b"BIN\0" and blen%4==0 and
          pos+8+blen==len(raw),"GLB BIN chunk offset/size")
    buffer=raw[pos+8:]
    check(len(buffer)>=gltf["buffers"][0]["byteLength"]
          and len(buffer)-gltf["buffers"][0]["byteLength"]<=3,
          "GLB declared BIN byte length vs padding")
    return gltf,buffer

def check_geometry(gltf,binary):
    check(gltf["asset"]["version"]=="2.0" and
          "B01-31-SPLIT-COUPLER-CONCEPT3D-001" in gltf["asset"]["generator"],
          "original B01-31 glTF source provenance")
    check(gltf["scene"]==0 and gltf["scenes"][0]["nodes"]==[0,1]
          and len(gltf["nodes"])==2 and len(gltf["meshes"])==2,
          "EXACT two independent connector root nodes")
    check([x["translation"] for x in gltf["nodes"]]==
          [[-0.2,0,0],[0.2,0,0]],
          "original independent authoring pivots and 0.4m origin separation")
    check([x["mesh"] for x in gltf["nodes"]]==[0,1] and
          [x["name"] for x in gltf["nodes"]]==
          ["LeftConnectorPivot","RightConnectorPivot"],
          "actual GLB root owner IDs/mesh references")
    check(len(gltf["materials"])==6 and
          [m["name"] for m in gltf["materials"]]==list(MATERIALS),
          "six original PBR material source names")
    check(all(m.get("doubleSided") is True and
              len(m["pbrMetallicRoughness"]["baseColorFactor"])==4
              for m in gltf["materials"]),
          "real valid PBR materials/raster viewing")
    check(len(gltf["accessors"])==36 and
          len(gltf["bufferViews"])==36 and
          len(gltf["buffers"])==1,
          "36 source bufferViews/accessors: 12 primitives x position,normal,indices")
    # Accessors and actual buffer ranges/lengths must be physically valid.
    def acc_data(id):
        a=gltf["accessors"][id]
        view=gltf["bufferViews"][a["bufferView"]]
        check(view["buffer"]==0 and view["byteOffset"]%4==0
              and view["byteLength"]>0
              and view["byteOffset"]+view["byteLength"]<=len(binary),
              "actual bufferView offset/alignment/byte containment")
        c=a["componentType"]
        typ=a["type"]
        comps=3 if typ=="VEC3" else 1 if typ=="SCALAR" else 0
        check(comps in (1,3) and c in (5126,5123),"actual compatible vertex/index componentType")
        fmt="f" if c==5126 else "H"
        size=4 if c==5126 else 2
        expected=a["count"]*comps*size
        check(expected==view["byteLength"],"actual physical accessor count/stride mismatch")
        start=view["byteOffset"]
        value=struct.unpack_from("<"+fmt*(a["count"]*comps),binary,start)
        return a,value
    sides=[]
    tri_sum=0
    all_world=[]
    for side,mesh in enumerate(gltf["meshes"]):
        check(mesh["name"]==gltf["nodes"][side]["name"]+"Mesh"
              and len(mesh["primitives"])==6,
              "exact one distinct geometry mesh per pivot, 6 materials each")
        vertex_world=[]
        copper_world=[]
        face_count=0
        seen_material=[]
        for prim in mesh["primitives"]:
            mat=prim["material"]
            seen_material.append(mat)
            check(prim["mode"]==4 and
                  set(prim["attributes"])=={"POSITION","NORMAL"},
                  "actual indexed triangles and normal shading")
            pa,pos=acc_data(prim["attributes"]["POSITION"])
            na,nor=acc_data(prim["attributes"]["NORMAL"])
            ia,idx=acc_data(prim["indices"])
            check(pa["type"]=="VEC3" and pa["componentType"]==5126
                  and na["type"]=="VEC3" and na["componentType"]==5126
                  and ia["type"]=="SCALAR" and ia["componentType"]==5123,
                  "position normals signed float and ushort triangle indices")
            count=pa["count"]
            check(count>30 and count==na["count"]
                  and ia["count"]%3==0 and ia["count"]==count
                  and list(idx)==list(range(count)),
                  "source actual triangle soup contiguous vertex indices and normals")
            check(all(math.isfinite(x) and abs(x)<2 for x in pos+nor),
                  "nonfinite/overflow actual geometry coordinates or normal")
            points=[pos[i:i+3] for i in range(0,len(pos),3)]
            normals=[nor[i:i+3] for i in range(0,len(nor),3)]
            for n in normals:
                check(abs(math.sqrt(sum(x*x for x in n))-1)<1e-4,
                      "physically encoded vertex normal not unit length")
            for i in range(0,len(points),3):
                p,q,r=points[i:i+3]
                ab=tuple(q[j]-p[j] for j in range(3))
                ac=tuple(r[j]-p[j] for j in range(3))
                cross=(ab[1]*ac[2]-ab[2]*ac[1],
                       ab[2]*ac[0]-ab[0]*ac[2],
                       ab[0]*ac[1]-ab[1]*ac[0])
                area=math.sqrt(sum(x*x for x in cross))*0.5
                check(area>1e-8,"actual 3D degenerate triangle or zero area")
                normvec=tuple(x/(2*area) for x in cross)
                check(sum(normvec[j]*normals[i][j] for j in range(3))>0.999,
                      "physically encoded triangle winding/normal divergence")
            actual_min=[min(pos[d::3]) for d in range(3)]
            actual_max=[max(pos[d::3]) for d in range(3)]
            check(all(abs(actual_min[j]-pa["min"][j])<1e-6 and
                      abs(actual_max[j]-pa["max"][j])<1e-6 for j in range(3)),
                  "actual accessor AABB lies about real source vertex floats")
            world=[(x+gltf["nodes"][side]["translation"][0],y,z)
                   for x,y,z in points]
            vertex_world.extend(world)
            all_world.extend(world)
            if mat==3:
                copper_world.extend(world)
            face_count+=len(points)//3
        check(sorted(seen_material)==list(range(6)),
              "each pivot must contain all six PBR material primitives")
        check(len(vertex_world)>0 and len(copper_world)>0,
              "no physical pin mesh or visible connector")
        minx=min(p[0] for p in vertex_world)
        maxx=max(p[0] for p in vertex_world)
        sides.append({"name":mesh["name"],
                      "world_x_min":minx,"world_x_max":maxx,
                      "copper_x_min":min(p[0] for p in copper_world),
                      "copper_x_max":max(p[0] for p in copper_world),
                      "triangles":face_count,
                      "vertices":len(vertex_world)})
        tri_sum+=face_count
    check(sides[0]["world_x_max"]<=-0.1299
          and sides[1]["world_x_min"]>=0.1299,
          "original geometry bridged physical centre gap unexpectedly")
    check(sides[1]["world_x_min"]-sides[0]["world_x_max"]>=0.2599,
          "actual min whole-mesh gap <0.26m")
    check(sides[1]["copper_x_min"]-sides[0]["copper_x_max"]>=0.2599,
          "actual original conductor pin-tip gap <0.26m")
    check(sides[0]["triangles"]==sides[1]["triangles"]
          and tri_sum>500 and tri_sum<20000,
          "two symmetric genuinely original lowpoly mesh complexity")
    return sides,tri_sum

def check_obj(obj,mtl,sides,triangles):
    s=obj.decode("utf8")
    lines=s.splitlines()
    positions=[x for x in lines if x.startswith("v ")]
    normals=[x for x in lines if x.startswith("vn ")]
    faces=[x for x in lines if x.startswith("f ")]
    check(len(positions)==triangles*3
          and len(normals)==triangles*3 and
          len(faces)==triangles,"ACTUAL OBJ positions/normals/face parity with GLB")
    check("o LeftConnectorPivot" in lines
          and "o RightConnectorPivot" in lines and
          "mtllib "+NAME+".mtl" in lines,
          "real two original separately named OBJ part groups")
    check(sorted(set(x.split(" ",1)[1] for x in lines if x.startswith("usemtl ")))==
          sorted(MATERIALS),
          "real OBJ MTL material references match GLB materials")
    check(len([x for x in mtl.decode().splitlines() if x.startswith("newmtl ")])==6,
          "actual original six OBJ diffuse PBR fallback materials")
    for k,face in enumerate(faces):
        ints=[x for x in face.split()[1:]]
        expect=(k*3+1,k*3+2,k*3+3)
        check(ints==[f"{q}//{q}" for q in expect],
              "OBJ face-local normal/position offsets corrupt")
    # OBJ rounds to 7 decimal places; exact GLB/OBJ world bounds stay close.
    world_x=[float(ln.split()[1]) for ln in positions]
    check(abs(min(world_x)-min(s["world_x_min"] for s in sides))<2e-6
          and abs(max(world_x)-max(s["world_x_max"] for s in sides))<2e-6,
          "actual OBJ world spatial bounds diverge from GLB")
    return len(faces)

def check_previews(path,receipt):
    expected={NAME+"-dark-three-views.png",NAME+"-light-three-views.png"}
    check(set(x["name"] for x in receipt["actual_preview_images"])==expected,
          "expected two original real 3-view geometry projections")
    for item in receipt["actual_preview_images"]:
        data=(path/item["name"]).read_bytes()
        check(H(data)==item["sha256"] and len(data)==item["bytes"],
              "actual original dark/light PNG geometry preview receipt")
        with Image.open(io.BytesIO(data)) as im:
            im.load()
            check(im.format=="PNG" and im.size==(1800,700) and im.mode=="RGB",
                  "actual physical three-view raster dimensions/color")
            colors=im.getcolors(maxcolors=1000000)
            check(colors is not None and len(colors)>30,
                  "actual multi-material geometry image, not blank placeholder")

def negative_checks(raw):
    bad=[b"GL0T"+raw[4:],raw[:8]+struct.pack("<I",len(raw)+100)+raw[12:],
         raw[:12]+struct.pack("<I4s",500000,b"JSON")+raw[20:],
         raw[:16]+b"FAIL"+raw[20:]]
    n=0
    for blob in bad:
        try:glb_read(blob)
        except (ValueError,struct.error,json.JSONDecodeError):n+=1
        else:raise AssertionError("FALSE GREEN tampered actual GLB2 header")
    check(n==4,"four source header negative controls")
    return n

def verify(root):
    rec=json.loads((root/"receipt.json").read_bytes())
    check(rec["package_id"]==PKG and rec["version"]=="P17-W16-R1"
          and rec["owner"]=="B01-31","not authorized own original source package")
    checks={x["file"]:x for x in rec["actual_binary_assets"]}
    expect={NAME+".glb",NAME+".obj",NAME+".mtl"}
    check(set(checks)==expect,"three actual original files missing")
    blobs={}
    for filename,entry in checks.items():
        raw=(root/filename).read_bytes()
        check(H(raw)==entry["sha256"] and len(raw)==entry["bytes"],
              "actual raw original model asset SHA changed")
        blobs[filename]=raw
    glb=blobs[NAME+".glb"]
    gltf,bin=glb_read(glb)
    sides,triangles=check_geometry(gltf,bin)
    nfaces=check_obj(blobs[NAME+".obj"],blobs[NAME+".mtl"],sides,triangles)
    check_previews(root,rec)
    check(rec["total_glb_triangles"]==triangles
          and len(rec["actual_pivot_nodes"])==2
          and all(rec["actual_pivot_nodes"][i]["triangles"]==sides[i]["triangles"]
                  for i in (0,1)),
          "receipt computed complexity not actual independent triangle count")
    check(abs(rec["minimum_design_pin_tip_gap_m"]-0.26)<1e-6
          and rec["runtime"]["unity_G1"]=="NOT_RUN"
          and rec["runtime"]["physx"]=="NOT_RUN"
          and rec["runtime"]["gameplay_reattach"]=="NOT_IMPLEMENTED"
          and rec["runtime"]["game_save_load"]=="NOT_IMPLEMENTED"
          and rec["freeze"]["gameplay_sha"]=="d94400d95d13db7f27e1533df05c9292d5380431"
          and rec["freeze"]["save_sha"]=="33094f154c7b91119b36d58aab285e6e83c5de57",
          "receipt must not fake actual Unity Save/Load/PhysX success")
    archive=root.parent/(root.name+"-physical.zip")
    with zipfile.ZipFile(archive) as z:
        check(z.testzip() is None and len(z.namelist())==6,
              "actual physical six-member immutable distribution ZIP")
        names=set(z.namelist())
        check(names==expect|{x["name"] for x in rec["actual_preview_images"]}|{"receipt.json"},
              "actual physical archive missing foreign or source files")
        check(all(z.read(filename)==(root/filename).read_bytes() for filename in names),
              "real zipped original GLB/OBJ/MTL/PNG/receipt bytes differ")
    neg=negative_checks(glb)
    check(H(glb+b"x")!=checks[NAME+".glb"]["sha256"],
          "actual binary digest corruption must change SHA")
    print(json.dumps({"status":"PASS","independent_real_GLB2_geometry":True,
                      "distinct_pivot_roots":2,"PBR_materials":6,
                      "actual_3d_triangles":triangles,"OBJ_real_faces":nfaces,
                      "lowest_separation_m":round(sides[1]["world_x_min"]-sides[0]["world_x_max"],4),
                      "actual_preview_PNG":2,"source_header_mutants_rejected":neg,
                      "physical_distribution_archive_entries":6,
                      "glb_sha256":H(glb),"real_Unity":"NOT_RUN"}))
    return 0

if __name__=="__main__":
    p=argparse.ArgumentParser()
    p.add_argument("--source",required=True)
    args=p.parse_args()
    try:sys.exit(verify(Path(args.source)))
    except Exception as e:
        print("B0131_SPLIT_COUPLER_INDEPENDENT_FAIL "+
              type(e).__name__+": "+str(e),file=sys.stderr)
        raise
