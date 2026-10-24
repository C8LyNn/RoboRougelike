#!/usr/bin/env python3
"""M02-002: stdlib structural validation of committed OBJ/MTL/animated glTF."""
import base64
from collections import Counter
import hashlib
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
    vs=[];faces=[];groups={};current=None;matnames=set()
    for line in obj.read_text(encoding="utf-8").splitlines():
        if line.startswith("o "):
            current=line[2:];assert current not in groups
            groups[current]=[]
        elif line.startswith("v "):
            v=tuple(map(float,line.split()[1:]));assert len(v)==3
            assert all(math.isfinite(x) for x in v);vs.append(v)
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
    sampler=anim["samplers"][0]
    times=accessor(sampler["input"]);quat=accessor(sampler["output"])
    assert len(times)==9 and len(quat)==36
    assert abs(times[0])<1e-6 and abs(times[-1]-2)<1e-6
    assert all(abs(sum(x*x for x in quat[i:i+4])-1)<1e-5 for i in range(0,36,4))
    assert all(abs(quat[i+1])<1e-6 and abs(quat[i+2])<1e-6 for i in range(0,36,4))
    return {"status":"PASS","parts":len(groups),"vertices":len(vs),"triangles":len(faces),
      "materials":len(names),"gltf_animation_keyframes":len(times),
      "shaft_radial_clearance_m":.010,
      "obj_sha256":hashlib.sha256(obj.read_bytes()).hexdigest(),
      "gltf_sha256":hashlib.sha256(gltf.read_bytes()).hexdigest(),
      "blender":"NOT RUN","unity":"NOT RUN"}

if __name__=="__main__":print(json.dumps(validate(),indent=2))
