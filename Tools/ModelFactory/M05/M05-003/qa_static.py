#!/usr/bin/env python3
"""Independent OBJ/MTL topology, reproducibility and material QA for M05-003."""
from __future__ import annotations

import argparse
import hashlib
import json
import math
import subprocess
import sys
import tempfile
from pathlib import Path

HERE=Path(__file__).resolve().parent
BASENAME="M05-003_dual_optical_scanner"

def digest(p):
    return hashlib.sha256(p.read_bytes()).hexdigest()

def area2(a,b,c):
    u=[b[i]-a[i] for i in range(3)]
    v=[c[i]-a[i] for i in range(3)]
    w=(u[1]*v[2]-u[2]*v[1],
       u[2]*v[0]-u[0]*v[2],
       u[0]*v[1]-u[1]*v[0])
    return sum(x*x for x in w)

def verify(directory):
    obj=directory/(BASENAME+".obj")
    mtl=directory/(BASENAME+".mtl")
    manifest=directory/"M05-003_manifest.json"
    assert obj.is_file() and mtl.is_file() and manifest.is_file()
    data=json.loads(manifest.read_text(encoding="utf-8"))
    assert data["id"]=="M05-003"
    assert data["qa_geometry"]=="PASS"
    assert data["rotor_clearance_qa"]=="PASS_5_ANGLES_EACH"
    assert data["blender_export"] in ("NOT RUN","RUN")
    assert data["mount_pivot"]==[0,0,0]
    assert data["optical_axis"]=="+Z" and data["rotation_axis"]=="+Z"
    mats=set()
    glowing=set()
    current=None
    for line in mtl.read_text(encoding="utf-8").splitlines():
        tokens=line.split()
        if not tokens:continue
        if tokens[0]=="newmtl":
            current=tokens[1]
            assert current not in mats
            mats.add(current)
        if tokens[0]=="Ke":
            assert current is not None
            if any(float(x)>0 for x in tokens[1:]):
                glowing.add(current)
    assert len(mats)>=3 and "Ruby_Lens" in glowing
    assert "Crimson_Emitter" in glowing and "Amber_Status" in glowing
    verts=[]
    objects={}
    current=None
    material=None
    referenced=set()
    mtllib=False
    for line in obj.read_text(encoding="utf-8").splitlines():
        tokens=line.split()
        if not tokens or tokens[0].startswith("#"):continue
        if tokens[0]=="mtllib":
            assert tokens[1]==mtl.name
            mtllib=True
        elif tokens[0]=="o":
            current=tokens[1]
            assert current not in objects
            objects[current]={"faces":[],"material":None}
        elif tokens[0]=="usemtl":
            material=tokens[1]
            assert material in mats and current is not None
            objects[current]["material"]=material
            referenced.add(material)
        elif tokens[0]=="v":
            assert len(tokens)==4
            v=tuple(float(x) for x in tokens[1:])
            assert all(math.isfinite(x) for x in v)
            verts.append(v)
        elif tokens[0]=="f":
            assert current is not None and material is not None
            assert len(tokens)==4
            indices=tuple(int(t.split("/")[0])-1 for t in tokens[1:])
            assert len(set(indices))==3 and all(0<=i<len(verts) for i in indices)
            assert area2(*(verts[i] for i in indices))>1e-14
            objects[current]["faces"].append(indices)
    assert mtllib and referenced==mats
    assert len(objects)==data["parts"] and len(verts)==data["vertices"]
    assert sum(len(v["faces"]) for v in objects.values())==data["triangles"]
    assert sum(name.startswith("Iris_Petal_") for name in objects)==10
    assert sum(name.startswith("Ruby_Optical_Lens_") for name in objects)==2
    for name,item in objects.items():
        assert item["faces"],name
        edges={}
        for a,b,c in item["faces"]:
            for u,v in ((a,b),(b,c),(c,a)):
                key=(min(u,v),max(u,v))
                edges[key]=edges.get(key,0)+1
        assert all(v==2 for v in edges.values()),("non-manifold",name)
    xs=[v[0] for v in verts]
    ys=[v[1] for v in verts]
    zs=[v[2] for v in verts]
    assert max(abs(min(xs)),abs(max(xs)))<.42
    assert min(zs)<-.019 and .668<max(zs)<.672
    assert max(ys)<.21 and min(ys)>-.21
    with tempfile.TemporaryDirectory(prefix="m05-003-qa-") as d:
        out=Path(d)
        subprocess.run([sys.executable,str(HERE/"generate_m05_003.py"),
                        "--out",str(out)],check=True,capture_output=True,text=True)
        for suffix in (".obj",".mtl"):
            name=BASENAME+suffix
            assert digest(directory/name)==digest(out/name),("non-deterministic",name)
    return {"asset":"M05-003","result":"PASS",
            "objects":len(objects),"vertices":len(verts),
            "triangles":sum(len(v["faces"]) for v in objects.values()),
            "materials":len(mats),"emissive_materials":sorted(glowing),
            "all_parts_closed_manifold":True,
            "obj_mtl_reproducibility":"PASS",
            "blender":"NOT RUN by this script",
            "unity":"NOT RUN by this script",
            "obj_sha256":digest(obj),"mtl_sha256":digest(mtl)}

if __name__=="__main__":
    ap=argparse.ArgumentParser()
    ap.add_argument("--asset-dir",type=Path,default=HERE/"generated")
    args=ap.parse_args()
    print(json.dumps(verify(args.asset_dir),indent=2))
