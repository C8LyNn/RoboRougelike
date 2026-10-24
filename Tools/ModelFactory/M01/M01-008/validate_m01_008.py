#!/usr/bin/env python3
"""Independent M01-008 real OBJ/MTL/GLB/FBX/BLEND QA (stdlib only)."""
from collections import Counter
import hashlib
import importlib.util
import json
import math
from pathlib import Path
import struct
import sys
import tempfile

ROOT=Path(__file__).resolve().parent
NAME="M01-008_liquid_cooled_rifle"

def load_source():
    spec=importlib.util.spec_from_file_location("m01_008",ROOT/"generate_m01_008.py")
    module=importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module

def inspect_obj(out):
    obj=out/(NAME+".obj")
    mtl=out/(NAME+".mtl")
    assert obj.is_file() and mtl.is_file(),"Missing actual OBJ/MTL"
    vertices=[];groups={};current=None;used=set()
    for line in obj.read_text(encoding="utf-8").splitlines():
        if line.startswith("o "):
            current=line[2:]
            assert current not in groups
            groups[current]=[]
        elif line.startswith("v "):
            v=tuple(map(float,line.split()[1:]))
            assert len(v)==3 and all(math.isfinite(x) for x in v)
            vertices.append(v)
        elif line.startswith("usemtl "):
            used.add(line[7:])
        elif line.startswith("f "):
            assert current is not None
            face=tuple(int(k)-1 for k in line.split()[1:])
            assert len(face)==3 and len(set(face))==3
            assert all(0<=k<len(vertices) for k in face)
            groups[current].append(face)
    for name,faces in groups.items():
        edges=Counter()
        for face in faces:
            a,b,c=(vertices[k] for k in face)
            u=tuple(b[i]-a[i] for i in range(3))
            v=tuple(c[i]-a[i] for i in range(3))
            cross=(u[1]*v[2]-u[2]*v[1],
                   u[2]*v[0]-u[0]*v[2],
                   u[0]*v[1]-u[1]*v[0])
            assert sum(x*x for x in cross)>1e-15,name
            for a,b in zip(face,face[1:]+face[:1]):
                edges[tuple(sorted((a,b)))]+=1
        assert edges and all(n==2 for n in edges.values()),name
    declared={line[7:] for line in mtl.read_text().splitlines()
              if line.startswith("newmtl ")}
    assert used==declared and len(declared)>=3
    mod=load_source()
    source=mod.build()
    report=mod.validate(source)
    assert len(groups)==report["parts"]
    assert len(vertices)==report["vertices"]
    assert sum(len(v) for v in groups.values())==report["triangles"]
    assert report["moving_static_intersections"]==0
    assert report["animation_pose_samples"]==61
    assert report["pump_groups"]==["PUMP_L","PUMP_R"]
    assert report["mount_pivot_m"]==[0,0,0]
    assert all(p["name"] in groups for p in source)
    assert abs(min(v[2] for v in vertices))<1e-7
    assert max(v[1] for v in vertices)>1.7
    manifest=json.loads((out/"M01-008_qa.json").read_text())
    assert manifest["result"]=="PASS_STATIC"
    assert manifest["obj_sha256"]==hashlib.sha256(obj.read_bytes()).hexdigest()
    assert manifest["mtl_sha256"]==hashlib.sha256(mtl.read_bytes()).hexdigest()
    return {"static":"PASS","parts":len(groups),"vertices":len(vertices),
            "triangles":sum(len(f) for f in groups.values()),
            "materials":len(declared),"pose_samples":61,
            "moving_static_collisions":0}

def inspect_glb(out):
    glb=out/(NAME+"_animated.glb")
    fbx=out/(NAME+"_animated.fbx")
    blend=out/(NAME+".blend")
    assert all(p.is_file() and p.stat().st_size>10000
               for p in (glb,fbx,blend)), "Missing actual Blender exports"
    raw=glb.read_bytes()
    magic,version,size=struct.unpack_from("<4sII",raw)
    assert magic==b"glTF" and version==2 and size==len(raw)
    json_length,chunk_type=struct.unpack_from("<II",raw,12)
    assert chunk_type==0x4E4F534A
    doc=json.loads(raw[20:20+json_length].decode("utf-8").rstrip("\0 "))
    assert len(doc.get("materials",[]))>=8
    assert len(doc.get("meshes",[]))>=70
    nodes=doc["nodes"]
    animated=set()
    for animation in doc.get("animations",[]):
        for channel in animation["channels"]:
            target=channel["target"]
            if target["path"]=="translation":
                name=nodes[target["node"]].get("name","")
                if "PUMP_" in name and "SLIDE_Y" in name:
                    animated.add(name)
    assert any("PUMP_L" in x for x in animated),animated
    assert any("PUMP_R" in x for x in animated),animated
    return {"glb":"PASS","fbx":"EXPORTED","blend":"EXPORTED",
            "glb_materials":len(doc["materials"]),
            "glb_meshes":len(doc["meshes"]),
            "animated_piston_nodes":sorted(animated),
            "animation_clips":len(doc["animations"])}

def main():
    out=Path(sys.argv[1]) if len(sys.argv)>1 else ROOT/"generated"
    report=inspect_obj(out)
    if (out/(NAME+"_animated.glb")).exists():
        report.update(inspect_glb(out))
    else:
        report.update({"glb":"NOT_RUN","fbx":"NOT_RUN","blend":"NOT_RUN"})
    print(json.dumps(report,indent=2))

if __name__=="__main__":main()
