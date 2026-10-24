#!/usr/bin/env python3
"""M04-012 independent static OBJ topology, deterministic rebuild and GLB rig QA."""
from __future__ import annotations
import argparse
from collections import Counter
import hashlib
import importlib.util
import json
import math
from pathlib import Path
import struct
import subprocess
import sys
import tempfile

HERE=Path(__file__).resolve().parent
SRC=HERE/"generate_m04_012.py"
STEM="M04-012_rear_correction_aileron"

def parse_obj(path):
    verts=[];objects={};materials=set();current=None
    for line in path.read_text(encoding="utf8").splitlines():
        if line.startswith("o "):
            current=line[2:];assert current not in objects
            objects[current]={"v":[],"f":[],"material":None}
        elif line.startswith("v "):
            assert current is not None
            coords=tuple(map(float,line.split()[1:]))
            assert len(coords)==3 and all(math.isfinite(x) for x in coords)
            verts.append(coords);objects[current]["v"].append(len(verts)-1)
        elif line.startswith("f "):
            f=tuple(int(x)-1 for x in line.split()[1:])
            assert len(f)==3
            objects[current]["f"].append(f)
        elif line.startswith("usemtl "):
            materials.add(line[7:]);objects[current]["material"]=line[7:]
    return verts,objects,materials

def check_topology(v,objects):
    faces=0
    for name,obj in objects.items():
        owned=set(obj["v"]);edges=Counter();directions=Counter();volume=0.
        for a,b,c in obj["f"]:
            assert len({a,b,c})==3 and {a,b,c}<=owned,name
            A,B,C=v[a],v[b],v[c]
            u=tuple(B[i]-A[i] for i in range(3))
            w=tuple(C[i]-A[i] for i in range(3))
            n=(u[1]*w[2]-u[2]*w[1],u[2]*w[0]-u[0]*w[2],u[0]*w[1]-u[1]*w[0])
            assert sum(t*t for t in n)>1e-18,name
            volume+=(A[0]*(B[1]*C[2]-B[2]*C[1])+
                     A[1]*(B[2]*C[0]-B[0]*C[2])+
                     A[2]*(B[0]*C[1]-B[1]*C[0]))/6
            for i,j in ((a,b),(b,c),(c,a)):
                edges[tuple(sorted((i,j)))]+=1;directions[(i,j)]+=1
        assert edges and all(x==2 for x in edges.values()),("nonmanifold",name)
        assert all(directions[(j,i)]==n for (i,j),n in directions.items()),("winding",name)
        assert volume>1e-11,("volume",name,volume)
        faces+=len(obj["f"])
    return faces

def check_static():
    spec=importlib.util.spec_from_file_location("m04_012",SRC)
    model=importlib.util.module_from_spec(spec);spec.loader.exec_module(model)
    report=model.validate(model.build())
    with tempfile.TemporaryDirectory(prefix="m04-012-static-") as tmp:
        out=Path(tmp)
        subprocess.run([sys.executable,str(SRC),"--static-obj","--out",str(out)],
                       check=True,capture_output=True,text=True)
        verts,objects,mats=parse_obj(out/(STEM+".obj"))
        assert len(objects)==report["parts"]
        assert len(verts)==report["vertices"]
        assert check_topology(verts,objects)==report["triangles"]
        assert len(mats)==report["materials"]==7
        assert max(y for x,y,z in verts)==0.0
        assert min(y for x,y,z in verts)<-.85
        # Validate the exported hollow bearing against actual rotating axle.
        shaft=objects["ROTATING_AXLE"]
        shaft_radius=max(math.hypot(verts[i][1]-model.HINGE[1],
                                    verts[i][2]-model.HINGE[2])
                         for i in shaft["v"])
        for side in ("L","R"):
            ring=objects[side+"_HOLLOW_BEARING"]
            bore=min(math.hypot(verts[i][1]-model.HINGE[1],
                                verts[i][2]-model.HINGE[2])
                     for i in ring["v"])
            assert bore-shaft_radius>=.00599
        with tempfile.TemporaryDirectory(prefix="m04-012-rebuild-") as other:
            subprocess.run([sys.executable,str(SRC),"--static-obj","--out",other],
                           check=True,capture_output=True,text=True)
            for name in (STEM+".obj",STEM+".mtl","M04-012_static_qa.json"):
                a=(out/name).read_bytes();b=(Path(other)/name).read_bytes()
                assert hashlib.sha256(a).digest()==hashlib.sha256(b).digest(),name
        saved=json.loads((out/"M04-012_static_qa.json").read_text())
        assert saved==json.loads(json.dumps(report))  # JSON converts tuple bounds to lists
    print(json.dumps({"id":"M04-012","static":"PASS",
        "parts":report["parts"],"vertices":report["vertices"],
        "triangles":report["triangles"],"materials":len(mats),
        "sampled_angles":report["pose_samples"],
        "pair_checks":report["fixed_pair_checks"],
        "min_clearance_m":report["min_conservative_aabb_clearance_m"],
        "axle_bore_clearance_m":.006,"obj_mtl_deterministic":"PASS",
        "blender":"NOT RUN","unity":"NOT RUN"},indent=2))

def check_glb(path):
    with path.open("rb") as f:
        magic,version,length=struct.unpack("<4sII",f.read(12))
        assert magic==b"glTF" and version==2 and length==path.stat().st_size
        size,kind=struct.unpack("<I4s",f.read(8))
        assert kind==b"JSON"
        gltf=json.loads(f.read(size).decode("utf8"))
    nodes=gltf["nodes"];animations=gltf.get("animations",[])
    paths=[(nodes[ch["target"]["node"]]["name"],ch["target"]["path"])
           for animation in animations for ch in animation["channels"]]
    assert len(gltf["meshes"])>=30
    assert len(gltf["materials"])>=7
    assert any("AILERON_HINGE_X" in name and kind=="rotation" for name,kind in paths),paths
    print(json.dumps({"id":"M04-012","glb":"PASS",
        "meshes":len(gltf["meshes"]),"materials":len(gltf["materials"]),
        "animations":len(animations),"channels":len(paths),
        "aileron_hinge_rotation":"PASS","unity":"NOT RUN"},indent=2))

def main():
    parser=argparse.ArgumentParser()
    parser.add_argument("--check-glb",type=Path)
    args=parser.parse_args()
    if args.check_glb:check_glb(args.check_glb)
    else:check_static()
if __name__=="__main__":main()
