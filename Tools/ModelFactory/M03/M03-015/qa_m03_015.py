#!/usr/bin/env python3
"""Dependency-free geometric and motion-clearance checks for M03-015."""
import hashlib
import importlib.util
import math
import tempfile
from collections import Counter
from pathlib import Path

HERE=Path(__file__).resolve().parent
spec=importlib.util.spec_from_file_location("generator",HERE/"generate_m03_015.py")
m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
parts=m.build()
assert len(parts)>=40
assert len(m.MATS)>=3
assert len({p[0] for p in parts})==len(parts)
assert len(m.manifest("static")["snap"])==4
for name,vs,fs,mat,parent in parts:
    assert m.volume(vs,fs)>1e-10,name
    edges=Counter()
    for face in fs:
        assert len(set(face))==len(face),name
        for a,b in zip(face,face[1:]+face[:1]):
            assert a!=b,name
            edges[tuple(sorted((a,b)))]+=1
    assert all(n==2 for n in edges.values()),(name,[x for x in edges.items() if x[1]!=2][:5])
    assert all(math.isfinite(c) for v in vs for c in v),name
# Door local Y=0 with positive outward rotation around Z.
# Its sweep must not enter interior Y<.425 (front frame inner face) for any angle.
door=[p for p in parts if p[4]=="DOOR"]
for deg in range(0,101):
    t=math.radians(deg)
    for name,vs,*_ in door:
        ys=[m.HINGE[1]+x*math.sin(t)+y*math.cos(t) for x,y,z in vs]
        assert min(ys)>.425,(deg,name,min(ys))
# Static mounting and cage dimensions.
assert all(abs(v)<1.2 for _,vs,*_ in parts for vertex in vs for v in vertex)
with tempfile.TemporaryDirectory() as td:
    out=Path(td)
    m.write_obj(out)
    before={p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in out.iterdir()}
    m.write_obj(out)
    after={p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in out.iterdir()}
    assert before==after
    assert (out/(m.STEM+".obj")).stat().st_size>10000
print("PASS M03-015:",len(parts),"closed oriented meshes,",len(m.MATS),
      "materials, 4 SNAP, 101 swept-door poses, deterministic OBJ/MTL")
