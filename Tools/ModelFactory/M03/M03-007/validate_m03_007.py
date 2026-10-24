#!/usr/bin/env python3
"""Independent checks of M03-007 generated OBJ, MTL and source parameters."""
import importlib.util
from collections import Counter
import json
import math
from pathlib import Path
import sys

ROOT=Path(__file__).resolve().parent
sys.path.insert(0,str(ROOT))
import generate_m03_007 as m

def validate(out=None):
    if out is None:out=ROOT/"generated"
    out=Path(out)
    obj=out/(m.NAME+".obj")
    mtl=out/(m.NAME+".mtl")
    assert obj.is_file() and mtl.is_file(), "Missing generated 3D geometry"
    vertices=[];groups={};materials=set();current=None
    for line in obj.read_text(encoding="utf-8").splitlines():
        if line.startswith("o "):
            current=line[2:]
            assert current not in groups
            groups[current]=[]
        elif line.startswith("usemtl "):
            materials.add(line[7:])
        elif line.startswith("v "):
            v=tuple(float(x) for x in line[2:].split())
            assert len(v)==3 and all(math.isfinite(x) for x in v)
            vertices.append(v)
        elif line.startswith("f "):
            face=tuple(int(x)-1 for x in line[2:].split())
            assert current is not None and len(face)==3
            assert len(set(face))==3 and 0<=min(face) and max(face)<len(vertices)
            groups[current].append(face)
    assert len(groups)==27 and len(materials)==5
    assert len(vertices)==1100
    assert sum(len(fs) for fs in groups.values())==2108
    for name,faces in groups.items():
        edges=Counter()
        for f in faces:
            for a,b in zip(f,f[1:]+f[:1]):edges[tuple(sorted((a,b)))]+=1
        assert edges and all(n==2 for n in edges.values()),name
    mtl_names={line.split()[1] for line in mtl.read_text().splitlines()
               if line.startswith("newmtl ")}
    assert materials==mtl_names==set(m.MATERIALS)
    assert len([n for n in groups if n.startswith("Foot_Boss_Bored_")])==2
    assert len([n for n in groups if n.startswith("Top_Boss_Bored_")])==2
    assert len([n for n in groups if "_Bolt_Head_" in n])==4
    # A real empty annular centre is a mesh topology property, not a decal.
    assert abs(m.BORE_R-m.SHAFT_R-.009)<1e-9
    source=m.build();qa=m.validate(source)
    assert qa["parts"]==len(groups) and qa["vertices"]==len(vertices)
    assert qa["triangles"]==2108 and qa["animation"]=="NOT_REQUIRED"
    assert qa["pivot"]==[0,0,0] and qa["mount_normal"]==[0,0,1]
    assert max(math.hypot(v[0]-x,v[1]) for x in m.BOLT_X for v in
               next(p for p in source if p["name"]=="Foot_Boss_Bored_0")["vertices"])>.08
    return dict(status="PASS",parts=len(groups),vertices=len(vertices),
                triangles=sum(map(len,groups.values())),materials=len(materials),
                genuine_bores=4,bolt_heads=4,clearance_m=.009,
                blender="NOT_RUN_BY_THIS_TEST",unity="NOT_RUN")

if __name__=="__main__":
    path=Path(sys.argv[1]) if len(sys.argv)>1 else None
    print(json.dumps(validate(path),indent=2))
