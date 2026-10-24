#!/usr/bin/env python3
"""Independent standard-library QA for actual M03-008 OBJ/MTL and manifest."""
from __future__ import annotations

import json
import math
import subprocess
import sys
import tempfile
from collections import Counter
from pathlib import Path

HERE = Path(__file__).resolve().parent
GEN = HERE/"generate_m03_008.py"
STEM = "M03-008_wedge_armor"


def sub(a,b):
    return tuple(x-y for x,y in zip(a,b))


def cross(a,b):
    return (a[1]*b[2]-a[2]*b[1],
            a[2]*b[0]-a[0]*b[2],
            a[0]*b[1]-a[1]*b[0])


def dot(a,b):
    return sum(x*y for x,y in zip(a,b))


def check_obj(obj_path, mtl_path, manifest):
    verts = []
    objects = {}
    current = None
    used = set()
    for line in obj_path.read_text().splitlines():
        if line.startswith("mtllib "):
            assert line[7:] == mtl_path.name
        elif line.startswith("o "):
            current = line[2:]
            assert current not in objects
            objects[current] = {"faces": [], "vertices": [], "material": None}
        elif line.startswith("usemtl "):
            assert current is not None
            objects[current]["material"] = line[7:]
            used.add(line[7:])
        elif line.startswith("v "):
            assert current is not None
            p = tuple(float(v) for v in line.split()[1:])
            assert len(p) == 3 and all(math.isfinite(x) for x in p)
            verts.append(p)
            objects[current]["vertices"].append(len(verts))
        elif line.startswith("f "):
            assert current is not None
            ids = tuple(int(v) for v in line.split()[1:])
            assert len(ids) == 3 and all(0<i<=len(verts) for i in ids)
            assert all(i in objects[current]["vertices"] for i in ids)
            objects[current]["faces"].append(ids)
    declared = {line[7:] for line in mtl_path.read_text().splitlines()
                if line.startswith("newmtl ")}
    assert used == declared and len(used) == manifest["materials"] == 5
    assert len(objects) == manifest["parts"] == 19
    assert len(verts) == manifest["vertices"]
    assert sum(len(x["faces"]) for x in objects.values()) == manifest["triangles"]

    # Re-evaluate manifoldness and winding from exported OBJ, not Python
    # generator's in-memory mesh. Every directed edge has its inverse.
    for name, item in objects.items():
        edges = Counter()
        volume = 0.
        for face in item["faces"]:
            a,b,c = (verts[i-1] for i in face)
            n = cross(sub(b,a),sub(c,a))
            assert dot(n,n)>1e-17, ("degenerate",name)
            volume += dot(a,cross(b,c))/6
            for i,j in ((face[0],face[1]),(face[1],face[2]),
                        (face[2],face[0])):
                edges[(i,j)] += 1
        assert all(count==1 and edges.get((j,i))==1
                   for (i,j),count in edges.items()), ("open",name)
        assert volume>1e-12, ("inward",name,volume)

    # Test that 4 holes are actual empty through-bores of 52mm diameter,
    # with concentric open collars, rather than a material-only illusion.
    for sx in (-1,1):
        for sz in (-1,1):
            cx,cz=sx*.410,sz*.290
            tile = next(o for n,o in objects.items()
                        if n.startswith("BaseBoredTile_") and
                        any(abs(verts[i-1][0]-cx)<.027 and
                            abs(verts[i-1][2]-cz)<.027
                            for i in o["vertices"]))
            collar=objects[f"MountCollar_{sx:+d}_{sz:+d}"]
            for obj in (tile,collar):
                radial = [math.hypot(verts[i-1][0]-cx,verts[i-1][2]-cz)
                          for i in obj["vertices"]]
                assert sum(abs(r-.026)<1e-7 for r in radial)==48
                assert all(r>=.026-1e-7 for r in radial)
            assert any(abs(math.hypot(verts[i-1][0]-cx,
                                      verts[i-1][2]-cz)-.040)<1e-7
                       for i in collar["vertices"])
    for name in ("ThreeLayerWedgeOuter","ShockAbsorberMiddle",
                 "CrestWarning_-1","CrestWarning_+1",
                 "AngledRib_-1","AngledRib_+1"):
        assert name in objects, name
    assert sum(1 for n in objects if n.startswith("BaseBoredTile_"))==4
    assert sum(1 for n in objects if n.startswith("BasePlateTile_"))==5
    lo=[min(v[a] for v in verts) for a in range(3)]
    hi=[max(v[a] for v in verts) for a in range(3)]
    assert all(abs(x-y)<1e-7 for x,y in zip(lo,(-.48,-.184,-.36)))
    assert all(abs(x-y)<1e-7 for x,y in zip(hi,(.48,0,.36)))
    assert manifest["mount_normal"]==[0,1,0]
    assert manifest["pivot_m"]==[0,0,0]
    assert manifest["animation"]=="NOT_REQUIRED"
    assert manifest["unity"]=="NOT RUN"
    return {"result":"PASS","parts":len(objects),"vertices":len(verts),
            "triangles":sum(len(x["faces"]) for x in objects.values()),
            "materials":len(used),"real_through_bores":4,
            "bounds_m":[[round(v,6) for v in lo],[round(v,6) for v in hi]],
            "blender":"NOT RUN","unity":"NOT RUN"}


def run():
    subprocess.run([sys.executable,"-m","py_compile",str(GEN)],
                   check=True,capture_output=True,text=True)
    selftest=subprocess.run([sys.executable,str(GEN),"--self-test"],
                            check=True,capture_output=True,text=True)
    source_report=json.loads(selftest.stdout)
    assert source_report["result"]=="PASS"
    with tempfile.TemporaryDirectory() as td:
        root=Path(td)
        a,b=root/"a",root/"b"
        for output in (a,b):
            subprocess.run([sys.executable,str(GEN),"--static-obj",
                            "--out",str(output)],
                           check=True,capture_output=True,text=True)
        for extension in (".obj",".mtl","_manifest.json"):
            assert (a/(STEM+extension)).read_bytes()==(
                    b/(STEM+extension)).read_bytes(), extension
        manifest=json.loads((a/(STEM+"_manifest.json")).read_text())
        assert manifest["backend"]=="python-stdlib"
        assert manifest["blender"]=="NOT RUN"
        qa=check_obj(a/(STEM+".obj"),a/(STEM+".mtl"),manifest)
        print(json.dumps(qa,indent=2))


if __name__=="__main__":
    run()
