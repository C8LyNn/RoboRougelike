#!/usr/bin/env python3
"""Read and validate the ACTUAL committed M03-005 OBJ/MTL, independent of bpy."""
import json
import math
from pathlib import Path
from generate_m03_005 import read_spec, build_parts, validate, verify_manifold

ROOT=Path(__file__).resolve().parent
spec=read_spec()
source=build_parts(spec)
reference={p["name"]:p for p in source}
obj=(ROOT/"M03-005.obj").read_text(encoding="utf-8").splitlines()
mtl=(ROOT/"M03-005.mtl").read_text(encoding="utf-8").splitlines()
parts=[]
vertices=[]
current=None
for line in obj:
    if line.startswith("mtllib "):
        assert line=="mtllib M03-005.mtl"
    elif line.startswith("o "):
        if current:
            parts.append(current)
        name=line[2:]
        assert name in reference and name not in {p["name"] for p in parts}
        current=dict(name=name,material=None,vertices=[],faces=[])
    elif line.startswith("usemtl "):
        assert current and current["material"] is None
        current["material"]=line[7:]
    elif line.startswith("v "):
        assert current
        v=tuple(float(x) for x in line[2:].split())
        assert len(v)==3 and all(math.isfinite(x) for x in v)
        current["vertices"].append(v)
        vertices.append(v)
    elif line.startswith("f "):
        assert current
        face=[int(x) for x in line[2:].split()]
        offset=len(vertices)-len(current["vertices"])
        assert all(offset < i <= len(vertices) for i in face)
        current["faces"].append(tuple(i-1-offset for i in face))
if current:
    parts.append(current)
assert len(parts)==32 and {p["name"] for p in parts}==set(reference)
assert {line[7:] for line in mtl if line.startswith("newmtl ")}==set(spec["palette"])
for p in parts:
    ref=reference[p["name"]]
    assert p["material"]==ref["material"]
    assert p["faces"]==ref["faces"]
    assert len(p["vertices"])==len(ref["vertices"])
    for a,b in zip(p["vertices"],ref["vertices"]):
        assert all(abs(x-y)<0.000001 for x,y in zip(a,b)),p["name"]
    assert verify_manifold(p)>0
qa=validate(spec,source)
manifest=json.loads((ROOT/"M03-005.static-qa.json").read_text(encoding="utf-8"))
assert qa==manifest,"Stale static QA manifest"
assert len(vertices)==3498
assert sum(len(p["faces"]) for p in parts)==3600
assert qa["triangles"]==6944
assert qa["base_rotor_pair_checks"]==12775
assert qa["poses_checked"]==73
print(json.dumps({"result":"PASS","parts":32,"vertices":3498,"faces":3600,
                  "triangles":6944,"materials":5,"open_socket_bores":8,
                  "sweep_poses":73,"base_rotor_checks":12775,
                  "blender":"NOT RUN by this validator","unity":"NOT RUN"}))
