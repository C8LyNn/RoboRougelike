#!/usr/bin/env python3
"""Independent stdlib validation of M01-007 static OBJ/MTL and optional Blender GLB."""
import importlib.util
import json
import math
from pathlib import Path
import struct
import sys
from collections import Counter

ROOT=Path(__file__).resolve().parent
NAME="M01-007_impulse_ring_cannon"
DEFAULT=ROOT/"generated"

def check_static(out):
    obj=out/(NAME+".obj")
    mtl=out/(NAME+".mtl")
    assert obj.is_file() and mtl.is_file(),"Missing real OBJ or MTL"
    groups={};names=set();materials=set();vertices=[];current=None;count=0
    for line in obj.read_text(encoding="utf-8").splitlines():
        if line.startswith("o "):
            current=line[2:]
            assert current not in groups
            groups[current]=[];count=0
        elif line.startswith("v "):
            v=tuple(map(float,line.split()[1:]))
            assert len(v)==3 and all(math.isfinite(x) for x in v)
            vertices.append(v)
        elif line.startswith("usemtl "):
            materials.add(line.split()[1])
        elif line.startswith("f "):
            assert current is not None
            f=tuple(int(t)-1 for t in line.split()[1:])
            assert len(f)==3 and len(set(f))==3
            assert all(0<=i<len(vertices) for i in f)
            groups[current].append(f)
    for name,faces in groups.items():
        edges=Counter()
        for f in faces:
            a,b,c=(vertices[k] for k in f)
            u=tuple(b[i]-a[i] for i in range(3))
            v=tuple(c[i]-a[i] for i in range(3))
            cross=(u[1]*v[2]-u[2]*v[1],u[2]*v[0]-u[0]*v[2],u[0]*v[1]-u[1]*v[0])
            assert sum(x*x for x in cross)>1e-15,name
            for a,b in zip(f,f[1:]+f[:1]):
                edges[tuple(sorted((a,b)))]+=1
        assert edges and all(v==2 for v in edges.values()),name
    mtl_names={l.split()[1] for l in mtl.read_text().splitlines() if l.startswith("newmtl ")}
    assert mtl_names==materials and len(materials)>=3
    spec=importlib.util.spec_from_file_location("m01_007",ROOT/"generate_m01_007.py")
    mod=importlib.util.module_from_spec(spec);spec.loader.exec_module(mod)
    source=mod.build()
    qa=mod.validate(source)
    assert len(source)==len(groups)
    assert qa["moving_static_collisions"]==0
    assert qa["ring_count"]==3 and qa["animation_pose_samples"]==49
    assert len(vertices)==qa["vertices"]
    assert sum(map(len,groups.values()))==qa["triangles"]
    assert all(p["name"] in groups for p in source)
    return {"static":"PASS","parts":len(groups),"vertices":len(vertices),
            "triangles":sum(map(len,groups.values())),"materials":len(materials),
            "pose_samples":49,"ring_count":3,"collision_count":0}

def check_glb(out):
    glb=out/(NAME+"_animated.glb")
    fbx=out/(NAME+"_animated.fbx")
    blend=out/(NAME+".blend")
    assert all(p.is_file() and p.stat().st_size>10000 for p in (glb,fbx,blend))
    data=glb.read_bytes()
    magic,version,length=struct.unpack_from("<4sII",data)
    assert magic==b"glTF" and version==2 and length==len(data)
    chunk_size,chunk_type=struct.unpack_from("<II",data,12)
    assert chunk_type==0x4E4F534A
    gltf=json.loads(data[20:20+chunk_size].decode("utf-8").rstrip("\x00 "))
    assert len(gltf.get("materials",[]))>=7
    nodes=gltf["nodes"];targets=set()
    animations=gltf.get("animations",[])
    assert animations,"GLB has no animations"
    for anim in animations:
        for channel in anim.get("channels",[]):
            target=channel["target"]
            if target["path"]=="translation":
                name=nodes[target["node"]]["name"]
                if "RING_" in name and "SLIDE_Y" in name:targets.add(name)
    assert len(targets)>=3,("Missing independent animated ring nodes",sorted(targets))
    return {"glb":"PASS","fbx":"EXPORTED","blend":"EXPORTED",
            "animated_ring_nodes":sorted(targets),"glb_materials":len(gltf["materials"]),
            "animations":len(animations)}

def main():
    out=Path(sys.argv[1]) if len(sys.argv)>1 else DEFAULT
    report=check_static(out)
    if (out/(NAME+"_animated.glb")).exists():
        report.update(check_glb(out))
    else:
        report.update({"glb":"NOT_RUN","fbx":"NOT_RUN","blend":"NOT_RUN"})
    print(json.dumps(report,indent=2))

if __name__=="__main__":main()
