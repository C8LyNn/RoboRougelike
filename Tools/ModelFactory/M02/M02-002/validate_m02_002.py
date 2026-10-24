#!/usr/bin/env python3
"""M02-002: stdlib structural validation of committed OBJ/MTL/animated glTF."""
import base64
from collections import Counter
import hashlib
import importlib.util
import json
import math
from pathlib import Path
import struct

ROOT=Path(__file__).resolve().parent
OUT=ROOT/"generated"
NAME="M02-002_narrow_speed_wheel"

def validate():
    obj=OUT/(NAME+".obj");mtl=OUT/(NAME+".mtl")
    gltf=OUT/(NAME+"_animated.gltf")
    assert all(x.is_file() for x in (obj,mtl,gltf))
    vs=[];faces=[];groups={};group_vertices={};current=None;matnames=set()
    for line in obj.read_text(encoding="utf-8").splitlines():
        if line.startswith("o "):
            current=line[2:];assert current not in groups
            groups[current]=[]
            group_vertices[current]=[]
        elif line.startswith("v "):
            v=tuple(map(float,line.split()[1:]));assert len(v)==3
            assert all(math.isfinite(x) for x in v);vs.append(v)
            group_vertices[current].append(v)
        elif line.startswith("usemtl "):matnames.add(line.split()[1])
        elif line.startswith("f "):
            face=tuple(int(x)-1 for x in line.split()[1:])
            assert len(face)==3 and len(set(face))==3
            assert all(0<=i<len(vs) for i in face)
            faces.append(face);groups[current].append(face)
    assert len(vs)==6016 and len(faces)==11952,(len(vs),len(faces))
    assert len(groups)==30 and len(matnames)==7
    assert len([g for g in groups if g.startswith("Spoke_Alloy")])==10
    assert len([g for g in groups if g.endswith("_STATIC")])==3
    for group,fs in groups.items():
        edges=Counter()
        for f in fs:
            for a,b in zip(f,f[1:]+f[:1]):edges[tuple(sorted((a,b)))]+=1
        assert all(n==2 for n in edges.values()),group
    radius=max(math.hypot(y,z) for x,y,z in vs)
    assert abs(radius-.523)<.0001,radius
    assert abs(min(x for x,y,z in vs)-(-.040))<.0001
    assert abs(max(x for x,y,z in vs)-.275)<.0001
    names=[line.split()[1] for line in mtl.read_text().splitlines() if line.startswith("newmtl ")]
    assert set(names)==matnames
    # Import the actual source generator without bpy; verify that this OBJ
    # corresponds exactly to the latest procedural source specifications.
    spec=importlib.util.spec_from_file_location("M02_002_gen",ROOT/"generate_m02_002.py")
    module=importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    parts=module.build()
    source_qa=module.validate(parts)
    assert source_qa["structural_revision"]=="M02-002_V2_CONNECTED_ASSEMBLY"
    assert source_qa["fixed_contact_pairs_verified"]==5
    assert source_qa["spoke_ceramic_contacts_verified"]==10
    assert set(p["name"] for p in parts)==set(group_vertices)
    for p in parts:
        baked=group_vertices[p["name"]]
        assert len(baked)==len(p["vertices"]),p["name"]
        assert max(abs(a-b) for source,bakedv in zip(p["vertices"],baked)
                   for a,b in zip(source,bakedv))<=1e-7, (
                       "Generated OBJ does not match source",p["name"])
    g=json.loads(gltf.read_text(encoding="utf-8"))
    assert g["asset"]["version"]=="2.0"
    assert len(g["materials"])==7 and len(g["meshes"])==2
    assert len(g["meshes"][0]["primitives"])==3
    assert len(g["meshes"][1]["primitives"])==27
    assert g["nodes"][0]["name"]=="StationaryMount_X0"
    assert g["nodes"][1]["name"]=="Wheel_ROTATE_X"
    assert len(g["animations"])==1
    anim=g["animations"][0]
    assert anim["channels"][0]["target"]=={"node":1,"path":"rotation"}
    raw=base64.b64decode(g["buffers"][0]["uri"].split(",",1)[1])
    assert len(raw)==g["buffers"][0]["byteLength"]
    for v in g["bufferViews"]:
        assert v["byteOffset"]+v["byteLength"]<=len(raw)
    def accessor(index):
        a=g["accessors"][index];v=g["bufferViews"][a["bufferView"]]
        n={"SCALAR":1,"VEC3":3,"VEC4":4}[a["type"]]
        assert a["componentType"]==5126
        return struct.unpack_from("<"+str(a["count"]*n)+"f",raw,v["byteOffset"])
    # Full parity check against the actual embedded animated glTF buffer.
    # Its glTF Y-up basis is (x, z, -y) from the source/OBJ Z-up basis.
    # Accessor vertex ordering is 3 stationary components, then 27 rotating.
    order=[n for n in groups if n.endswith("_STATIC")]
    order += [n for n in groups if not n.endswith("_STATIC")]
    assert len(order)==30
    checked=0
    for i,name in enumerate(order):
        primitive=g["meshes"][0 if i<3 else 1]["primitives"][i if i<3 else i-3]
        pindex=primitive["attributes"]["POSITION"]
        nindex=primitive["attributes"]["NORMAL"]
        a=g["accessors"][pindex]
        n=g["accessors"][nindex]
        pcoords=accessor(pindex)
        normals=accessor(nindex)
        assert a["count"]==n["count"]==len(group_vertices[name]),name
        for j,coord in enumerate(group_vertices[name]):
            source_x,source_y,source_z=coord
            target=(source_x,source_z,-source_y)
            actual=pcoords[j*3:j*3+3]
            assert max(abs(x-y) for x,y in zip(actual,target))<=2e-6, (
                "Stale glTF geometry",name,j,actual,target)
            normal=normals[j*3:j*3+3]
            assert abs(sum(x*x for x in normal)-1.0)<.002, (
                "GLTF vertex normal not normalized",name,j)
            checked+=1
    assert checked==6016
    sampler=anim["samplers"][0]
    times=accessor(sampler["input"]);quat=accessor(sampler["output"])
    assert len(times)==9 and len(quat)==36
    assert abs(times[0])<1e-6 and abs(times[-1]-2)<1e-6
    assert all(abs(sum(x*x for x in quat[i:i+4])-1)<1e-5 for i in range(0,36,4))
    assert all(abs(quat[i+1])<1e-6 and abs(quat[i+2])<1e-6 for i in range(0,36,4))
    return {"status":"PASS","parts":len(groups),"vertices":len(vs),"triangles":len(faces),
      "materials":len(names),"gltf_animation_keyframes":len(times),
      "shaft_radial_clearance_m":.010,
      "connected_wheel_contacts":15,
      "gltf_obj_parity_vertices":checked,
      "geometry_revision":"M02-002_V2_CONNECTED_ASSEMBLY",
      "obj_sha256":hashlib.sha256(obj.read_bytes()).hexdigest(),
      "gltf_sha256":hashlib.sha256(gltf.read_bytes()).hexdigest(),
      "blender":"NOT RUN","unity":"NOT RUN"}

if __name__=="__main__":print(json.dumps(validate(),indent=2))
