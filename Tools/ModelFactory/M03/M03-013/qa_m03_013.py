#!/usr/bin/env python3
"""M03-013 independent stdlib QA of actual generated OBJ/MTL and source."""
from __future__ import annotations

import hashlib
import json
import math
import subprocess
import sys
import tempfile
from pathlib import Path

HERE=Path(__file__).resolve().parent
GEN=HERE/"generate_m03_013.py"
STEM="M03-013_lower_sill_guard"

def run(out):
    subprocess.run([sys.executable,str(GEN),"--out",str(out)],
                   check=True,stdout=subprocess.DEVNULL)
    return [(out/(STEM+ext)).read_bytes() for ext in (".obj",".mtl")]

def cross(a,b):
    return (a[1]*b[2]-a[2]*b[1],
            a[2]*b[0]-a[0]*b[2],
            a[0]*b[1]-a[1]*b[0])

def dot(a,b):
    return sum(x*y for x,y in zip(a,b))

def parse_obj(raw):
    vertices=[]
    groups={}
    materials={}
    current=None
    for line in raw.decode("utf-8").splitlines():
        if line.startswith("o "):
            current=line[2:]
            assert current not in groups
            groups[current]={"faces":[],"indices":set()}
        elif line.startswith("usemtl "):
            materials[current]=line.split(maxsplit=1)[1]
        elif line.startswith("v "):
            vertices.append(tuple(map(float,line.split()[1:])))
        elif line.startswith("f "):
            assert current is not None
            face=tuple(int(x) for x in line.split()[1:])
            assert len(face)==3
            assert all(1<=i<=len(vertices) for i in face)
            groups[current]["faces"].append(face)
            groups[current]["indices"].update(face)
    return vertices,groups,materials

def bounds(vertices,group):
    v=[vertices[i-1] for i in group["indices"]]
    return [(min(x[k] for x in v),max(x[k] for x in v))
            for k in range(3)]

def check_topology(vertices,groups):
    for name,group in groups.items():
        edges={}
        volume=0.
        for face in group["faces"]:
            A,B,C=[vertices[i-1] for i in face]
            n=cross(tuple(B[k]-A[k] for k in range(3)),
                    tuple(C[k]-A[k] for k in range(3)))
            assert dot(n,n)>1e-16,(name,"degenerate triangle")
            volume+=dot(A,cross(B,C))/6.
            for a,b in zip(face,face[1:]+face[:1]):
                key=tuple(sorted((a,b)))
                edges.setdefault(key,[]).append((a,b))
        assert edges and all(len(pair)==2 and pair[0]==pair[1][::-1]
                             for pair in edges.values()),name
        assert volume>1e-10,(name,"inverted mesh",volume)

def main():
    subprocess.run([sys.executable,"-m","py_compile",str(GEN)],
                   check=True,stdout=subprocess.DEVNULL)
    subprocess.run([sys.executable,str(GEN),"--self-test"],
                   check=True,stdout=subprocess.DEVNULL)
    with tempfile.TemporaryDirectory() as tmp:
        out=Path(tmp)
        first=run(out)
        second=run(out)
        assert first==second,"non-deterministic OBJ/MTL"
        manifest=json.loads((out/"M03-013_manifest.json").read_text())
        assert manifest["backend"]=="plain-python-OBJ-MTL"
        assert manifest["blender_export"]=="NOT RUN"
        assert manifest["unity_import"]=="NOT RUN"
        assert manifest["animation"]=="NOT REQUIRED (static)"
        vertices,groups,used=parse_obj(first[0])
        assert len(vertices)==manifest["vertices"]
        assert len(groups)==manifest["parts"]==38
        assert sum(len(x["faces"]) for x in groups.values())==manifest["triangles"]
        assert len(used)==38
        mtl={line.split(maxsplit=1)[1] for line in first[1].decode().splitlines()
             if line.startswith("newmtl ")}
        assert len(mtl)==manifest["materials"]==5
        assert set(used.values())==mtl
        check_topology(vertices,groups)
        assert len(manifest["snap_ports"])==4
        assert manifest["actual_bored_mounts"]==4
        assert manifest["heavy_rivet_heads"]==8
        assert manifest["ceramic_wear_runners"]==3
        for label,x in (("L",-.195),("R",.195)):
            for word,z in (("Aft",-.5),("Fore",.5)):
                tile="Bored_Mount_Tile_"+label+"_"+word
                collar="Raised_Collar_"+label+"_"+word
                for name in (tile,collar):
                    v=[vertices[i-1] for i in groups[name]["indices"]]
                    assert min(math.hypot(a-x,c-z) for a,b,c in v)>=.032-1e-8
                assert abs(bounds(vertices,groups[collar])[1][1])<1e-8
                assert abs(bounds(vertices,groups[tile])[1][1]-
                           bounds(vertices,groups[collar])[1][0])<1e-8
            rail=bounds(vertices,groups["Side_Impact_Edge_"+label])[0]
            assert abs((rail[0] if x>0 else rail[1])-
                       (.270 if x>0 else -.270))<1e-8
            for word,sign in (("Aft",-1),("Fore",1)):
                for zabs in (.15,.78):
                    suffix=label+"_"+word+"_"+str(int(zabs*1000))
                    stem=bounds(vertices,groups["Rivet_Shank_"+suffix])
                    head=bounds(vertices,groups["Heavy_Rivet_Head_"+suffix])
                    assert abs(stem[1][1]+.135)<1e-8
                    assert abs(stem[1][0]-head[1][1])<1e-8
                    assert abs(sign*zabs-sign*.5)>.17
        assert all(-.18300001<=y<=.00000001 for x,y,z in vertices)
        assert len([x for x in groups if x.startswith("Heavy_Rivet_Head_")])==8
        assert len([x for x in groups if x.startswith("Bored_Mount_Tile_")])==4
        print(json.dumps({
            "status":"PASS","id":"M03-013",
            "parts":len(groups),"vertices":len(vertices),
            "triangles":sum(len(x["faces"]) for x in groups.values()),
            "materials":len(mtl),"real_open_mount_bores":4,
            "heavy_rivet_heads":8,"oriented_watertight_meshes":38,
            "deterministic_obj_sha256":hashlib.sha256(first[0]).hexdigest(),
            "blender":"NOT RUN (plain-Python QA)",
            "unity":"NOT RUN"},indent=2))

if __name__=="__main__":
    main()
