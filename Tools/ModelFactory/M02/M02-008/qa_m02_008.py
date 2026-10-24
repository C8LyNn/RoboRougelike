#!/usr/bin/env python3
"""Independent OBJ/GLB evidence checks for M02-008; stdlib only."""
import json
import math
import struct
import subprocess
import sys
import tempfile
from pathlib import Path

HERE=Path(__file__).resolve().parent
GEN=HERE/"generate_m02_008.py"


def check_obj(path,manifest):
    obj=path.read_text(encoding="utf-8")
    verts=[]
    faces=0
    names=[]
    used=set()
    material_file=None
    for line in obj.splitlines():
        if line.startswith("mtllib "):
            material_file=line[7:]
        elif line.startswith("v "):
            xyz=tuple(map(float,line.split()[1:]))
            assert len(xyz)==3 and all(math.isfinite(v) for v in xyz)
            verts.append(xyz)
        elif line.startswith("f "):
            idx=[int(x) for x in line.split()[1:]]
            assert len(idx)==3 and len(set(idx))==3
            assert all(1<=v<=len(verts) for v in idx)
            faces+=1
        elif line.startswith("o "):
            names.append(line[2:])
        elif line.startswith("usemtl "):
            used.add(line[7:])
    assert len(names)==len(set(names))==manifest["parts"]
    assert faces==manifest["triangles"]
    assert len([x for x in names if x.startswith("LinkPlate_")])==32
    assert len([x for x in names if x.startswith("WedgeCleat_")])==32
    assert len([x for x in names if x.startswith("JointPin_")])==64
    assert material_file==manifest["mtl"]
    mtl=(path.parent/material_file).read_text()
    declared={line[7:] for line in mtl.splitlines() if line.startswith("newmtl ")}
    assert used==declared and len(declared)>=3
    assert len(verts)>3000
    assert all(-1.0<v[0]<1.0 and -.5<v[1]<.5 and -.8<v[2]<.5 for v in verts)
    return {"vertices":len(verts),"triangles":faces,"objects":len(names),
            "materials":len(declared)}


def check_glb(path):
    raw=path.read_bytes()
    assert len(raw)>10000
    magic,version,length=struct.unpack_from("<4sII",raw,0)
    assert magic==b"glTF" and version==2 and length==len(raw)
    offset=12
    chunks=[]
    while offset<len(raw):
        size,kind=struct.unpack_from("<I4s",raw,offset)
        offset+=8
        chunks.append((kind,raw[offset:offset+size]))
        offset+=size
    assert offset==len(raw)
    assert chunks[0][0]==b"JSON"
    data=json.loads(chunks[0][1].decode("utf-8").rstrip(" \x00"))
    assert len(data["meshes"])>=100
    assert len(data["materials"])>=3
    assert len(data["animations"])>=1
    nodes=[n.get("name","") for n in data["nodes"]]
    assert any("LINK_00" in n for n in nodes)
    assert any("LINK_31" in n for n in nodes)
    assert any("MOUNT_SOCKET" in n for n in nodes)
    paths={ch["target"]["path"] for a in data["animations"] for ch in a["channels"]}
    assert "rotation" in paths and "translation" in paths
    return {"meshes":len(data["meshes"]),"materials":len(data["materials"]),
            "nodes":len(nodes),"animations":len(data["animations"]),
            "animated_paths":sorted(paths)}


def run():
    subprocess.run([sys.executable,"-m","py_compile",str(GEN)],check=True)
    s=subprocess.run([sys.executable,str(GEN),"--self-test"],
                     check=True,capture_output=True,text=True)
    report=json.loads(s.stdout)
    assert report["result"]=="PASS"
    assert report["links"]==32
    assert report["materials"]>=3
    assert report["link_pair_sat_tests"]==97*32*31//2
    assert report["min_link_clearance_m"]>.0004
    assert report["min_wheel_clearance_m"]>.001
    with tempfile.TemporaryDirectory() as d:
        out=Path(d)
        subprocess.run([sys.executable,str(GEN),"--static-obj","--out",str(out)],
                       check=True,capture_output=True,text=True)
        m=json.loads((out/"M02-008_qa.json").read_text())
        assert m["backend"]=="python-stdlib" and m["unity"]=="NOT RUN"
        obj=check_obj(out/m["obj"],m)
    print(json.dumps({"static_qa":"PASS",**obj,
                      "min_link_clearance_m":report["min_link_clearance_m"],
                      "min_wheel_clearance_m":report["min_wheel_clearance_m"]},indent=2))
    if len(sys.argv)>1:
        glb=check_glb(Path(sys.argv[1]))
        print(json.dumps({"glb_qa":"PASS",**glb},indent=2))


if __name__=="__main__":
    run()
