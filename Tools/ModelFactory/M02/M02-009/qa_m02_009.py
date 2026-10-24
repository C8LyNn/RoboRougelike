#!/usr/bin/env python3
"""Independent read-only QA for actual M02-009 OBJ/MTL and animated GLB."""
from __future__ import annotations
from collections import Counter
import hashlib
import json
import math
from pathlib import Path
import struct
import subprocess
import sys
import tempfile

HERE=Path(__file__).resolve().parent
GEN=HERE/"generate_m02_009.py"
ID="M02-009"

def inspect_obj(path,report):
    vertices=[]
    objects={}
    material=None
    current=None
    mtllib=None
    for line in path.read_text(encoding="utf-8").splitlines():
        if line.startswith("mtllib "):
            mtllib=line[7:]
        elif line.startswith("o "):
            current=line[2:]
            assert current not in objects,current
            objects[current]={"faces":[],"vertices":set(),"material":None}
        elif line.startswith("usemtl "):
            assert current is not None
            material=line[7:]
            objects[current]["material"]=material
        elif line.startswith("v "):
            v=tuple(map(float,line.split()[1:]))
            assert len(v)==3 and all(math.isfinite(x) for x in v)
            vertices.append(v)
        elif line.startswith("f "):
            face=tuple(int(x)-1 for x in line.split()[1:])
            assert len(face)==3 and len(set(face))==3
            assert all(0<=i<len(vertices) for i in face)
            objects[current]["faces"].append(face)
            objects[current]["vertices"].update(face)
    assert mtllib==report["mtl"]
    materials={x[7:] for x in (path.parent/mtllib).read_text().splitlines()
               if x.startswith("newmtl ")}
    assert len(materials)==report["materials"]>=3
    assert len(vertices)==report["vertices"]
    assert len(objects)==report["parts"]
    assert sum(len(x["faces"]) for x in objects.values())==report["triangles"]
    assert len([n for n in objects if n.startswith("SteelShoe_")])==40
    assert len([n for n in objects if n.startswith("ChevronCleat_")])==40
    assert len([n for n in objects if n.startswith("RubberTire_")])==3
    assert len([n for n in objects if n.startswith("Truss_")])==6
    assert all(x["material"] in materials for x in objects.values())
    assert all(-1.0<v[0]<1.0 and -.1<v[1]<.6 and -.7<v[2]<.6
               for v in vertices)
    for name,obj in objects.items():
        edges=Counter()
        volume=0.
        owned=obj["vertices"]
        for a,b,c in obj["faces"]:
            assert a in owned and b in owned and c in owned
            A,B,C=(vertices[i] for i in (a,b,c))
            u=[B[i]-A[i] for i in range(3)]
            v=[C[i]-A[i] for i in range(3)]
            n=(u[1]*v[2]-u[2]*v[1],u[2]*v[0]-u[0]*v[2],
               u[0]*v[1]-u[1]*v[0])
            assert sum(x*x for x in n)>1e-15,(name,"degenerate")
            volume+=(A[0]*(B[1]*C[2]-B[2]*C[1])+
                     A[1]*(B[2]*C[0]-B[0]*C[2])+
                     A[2]*(B[0]*C[1]-B[1]*C[0]))/6.
            for x,y in ((a,b),(b,c),(c,a)):
                edges[(min(x,y),max(x,y))]+=1
        assert all(n==2 for n in edges.values()),("nonmanifold",name)
        assert volume>1e-11,("inverted",name,volume)
    return {"objects":len(objects),"vertices":len(vertices),
            "triangles":report["triangles"],"materials":len(materials),
            "closed_manifold":"PASS"}

def inspect_glb(path):
    raw=path.read_bytes()
    assert len(raw)>10000
    magic,version,length=struct.unpack_from("<4sII",raw,0)
    assert (magic,version,length)==(b"glTF",2,len(raw))
    offset=12
    chunks=[]
    while offset<len(raw):
        size,kind=struct.unpack_from("<I4s",raw,offset)
        offset+=8
        chunks.append((kind,raw[offset:offset+size]))
        offset+=size
    assert offset==len(raw) and chunks[0][0]==b"JSON"
    j=json.loads(chunks[0][1].decode("utf-8").rstrip(" \x00"))
    assert j["asset"]["version"]=="2.0"
    names=[n.get("name","") for n in j["nodes"]]
    assert len(j["materials"])>=3
    assert len(j["meshes"])>=150
    assert any("MOUNT_SOCKET" in n for n in names)
    assert any("ROOT_MOUNT_PIVOT" in n for n in names)
    for i in range(3):
        assert any(("WHEEL_%d"%i) in n for n in names)
    for i in range(40):
        assert any(("LINK_%02d"%i) in n for n in names)
    animations=j.get("animations",[])
    assert animations
    channels=[c for a in animations for c in a["channels"]]
    paths={c["target"]["path"] for c in channels}
    assert "rotation" in paths and "translation" in paths
    return {"meshes":len(j["meshes"]),"nodes":len(names),
            "materials":len(j["materials"]),"animations":len(animations),
            "channels":len(channels),"paths":sorted(paths)}

def main():
    subprocess.run([sys.executable,"-m","py_compile",str(GEN)],check=True)
    s=subprocess.run([sys.executable,str(GEN),"--self-test"],
                     capture_output=True,text=True,check=True)
    r=json.loads(s.stdout)
    assert r["result"]=="PASS"
    assert r["wheels"]==3 and r["links"]==40
    assert r["pose_samples"]==97
    assert r["link_pair_sat_tests"]==97*40*39//2
    assert r["min_link_clearance_m"]>.0005
    assert r["min_wheel_clearance_m"]>.0005
    with tempfile.TemporaryDirectory(prefix="m02-009-qa-") as tmp:
        a=Path(tmp)/"a";b=Path(tmp)/"b"
        for out in (a,b):
            subprocess.run([sys.executable,str(GEN),"--static-obj","--out",str(out)],
                           capture_output=True,text=True,check=True)
        report=json.loads((a/(ID+"_qa.json")).read_text())
        assert report["backend"]=="python-stdlib"
        assert report["unity"]=="NOT RUN" and report["blender"]=="NOT RUN"
        result=inspect_obj(a/report["obj"],report)
        for name in (report["obj"],report["mtl"],ID+"_qa.json"):
            ha=hashlib.sha256((a/name).read_bytes()).digest()
            hb=hashlib.sha256((b/name).read_bytes()).digest()
            assert ha==hb,("nondeterministic",name)
    print(json.dumps({"static_qa":"PASS","source_manifold":"PASS",
                      "deterministic_exports":"BYTE_IDENTICAL",
                      "swept_sat_tests":r["link_pair_sat_tests"],
                      "min_link_clearance_m":r["min_link_clearance_m"],
                      "min_wheel_clearance_m":r["min_wheel_clearance_m"],
                      **result},indent=2))
    if len(sys.argv)>1:
        result=inspect_glb(Path(sys.argv[1]))
        print(json.dumps({"glb_qa":"PASS",**result},indent=2))

if __name__=="__main__":
    main()
