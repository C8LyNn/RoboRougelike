#!/usr/bin/env python3
"""Independent M01-006 committed OBJ/MTL and optional animated GLB audit."""
from __future__ import annotations
from collections import Counter
import hashlib
import json
import math
from pathlib import Path
import struct
import sys

HERE=Path(__file__).resolve().parent
OUT=HERE/"generated"
NAME="M01-006_ceramic_needle_launcher"

def parse_obj(path):
    vs=[];groups={};group=None;materials=set();face_count=0
    for line in path.read_text(encoding="utf-8").splitlines():
        if line.startswith("v "):
            p=tuple(map(float,line.split()[1:]))
            assert len(p)==3 and all(math.isfinite(x) for x in p)
            vs.append(p)
        elif line.startswith("o "):
            group=line[2:];assert group not in groups;groups[group]=[]
        elif line.startswith("usemtl "):materials.add(line[7:])
        elif line.startswith("f "):
            face=tuple(int(x)-1 for x in line.split()[1:])
            assert group is not None
            assert len(face)==3 and len(set(face))==3 and all(0<=i<len(vs) for i in face)
            groups[group].append(face);face_count+=1
    for name,faces in groups.items():
        edges=Counter()
        for face in faces:
            for a,b in zip(face,face[1:]+face[:1]):edges[tuple(sorted((a,b)))]+=1
        assert edges and all(n==2 for n in edges.values()),name
    return vs,groups,materials,face_count

def audit_glb(path):
    data=path.read_bytes()
    assert len(data)>=20 and data[:4]==b"glTF"
    magic,version,length=struct.unpack_from("<4sII",data)
    assert version==2 and length==len(data)
    chunk_len,chunk_type=struct.unpack_from("<I4s",data,12)
    assert chunk_type==b"JSON"
    g=json.loads(data[20:20+chunk_len].decode("utf-8"))
    nodes={n.get("name"):i for i,n in enumerate(g.get("nodes",[]))}
    rail=[(name,i) for name,i in nodes.items() if name and "RAIL_EXTEND_Y" in name]
    mount=[(name,i) for name,i in nodes.items() if name and "MOUNT_TOP_Z0" in name]
    assert rail and mount,list(nodes)
    assert g.get("animations"),"GLB has no animations"
    targets=[(ch["target"]["node"],ch["target"]["path"]) for a in g["animations"] for ch in a["channels"]]
    assert any(i==rail[0][1] and p=="translation" for i,p in targets),targets
    assert all(i!=mount[0][1] for i,p in targets),"mount pivot is animated"
    return {"glb_nodes":len(g["nodes"]),"glb_animations":len(g["animations"]),
            "rail_translation_animated":True,"mount_static":True}

def validate(out=OUT):
    obj=out/(NAME+".obj");mtl=out/(NAME+".mtl")
    assert obj.is_file() and mtl.is_file()
    verts,groups,materials,triangles=parse_obj(obj)
    assert len(groups)==63 and len(verts)==994 and triangles==1736
    assert len(materials)==7
    assert len([n for n in groups if n.startswith("Needle_Tapered_Point_")])==5
    assert set(line.split()[1] for line in mtl.read_text().splitlines() if line.startswith("newmtl "))==materials
    assert abs(min(p[2] for p in verts))<1e-7
    assert abs(max(p[2] for p in verts)-.633)<1e-6
    assert abs(min(p[0] for p in verts)+.427)<1e-6
    assert abs(max(p[0] for p in verts)-.427)<1e-6
    assert abs(max(p[1] for p in verts)-.7915)<1e-6
    report={"status":"PASS","parts":len(groups),"vertices":len(verts),
            "triangles":triangles,"materials":len(materials),"needles":5,
            "obj_sha256":hashlib.sha256(obj.read_bytes()).hexdigest(),
            "mtl_sha256":hashlib.sha256(mtl.read_bytes()).hexdigest()}
    glb=out/(NAME+"_animated.glb")
    if glb.is_file():report.update(audit_glb(glb))
    else:report["blender_glb"]="NOT_RUN"
    return report

if __name__=="__main__":
    result=validate(Path(sys.argv[1]) if len(sys.argv)>1 else OUT)
    print(json.dumps(result,indent=2))
