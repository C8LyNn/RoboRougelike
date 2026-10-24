#!/usr/bin/env python3
"""Independent actual-file QA for M02-011: OBJ/MTL topology and animated GLB."""
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
GEN=HERE/"generate_m02_011.py"
ID="M02-011"

def obj_qa(path,report):
    vertices=[];parts={};current=None;mtllib=None
    for line in path.read_text(encoding="utf-8").splitlines():
        if line.startswith("mtllib "):
            mtllib=line[7:]
        elif line.startswith("o "):
            current=line[2:]
            assert current not in parts
            parts[current]={"vertices":set(),"faces":[],"material":None}
        elif line.startswith("usemtl "):
            parts[current]["material"]=line[7:]
        elif line.startswith("v "):
            v=tuple(map(float,line.split()[1:]))
            assert len(v)==3 and all(math.isfinite(a) for a in v)
            vertices.append(v)
        elif line.startswith("f "):
            f=tuple(int(v)-1 for v in line.split()[1:])
            assert len(f)==3 and len(set(f))==3
            assert all(0<=v<len(vertices) for v in f)
            parts[current]["faces"].append(f)
            parts[current]["vertices"].update(f)
    assert mtllib==report["mtl"]
    mats={line[7:] for line in (path.parent/mtllib).read_text().splitlines()
          if line.startswith("newmtl ")}
    assert len(mats)==report["materials"]>=3
    assert len(parts)==report["parts"]>=65
    assert len(vertices)==report["vertices"]
    assert sum(len(p["faces"]) for p in parts.values())==report["triangles"]
    assert "ExternalCoilSpring" in parts
    assert "LOWER_OutboardBall" in parts and "UPPER_OutboardBall" in parts
    assert "VerticalSteeringKnuckle" in parts and "WheelHubFace" in parts
    assert "LOWER_InnerRubber_F" in parts and "UPPER_InnerRubber_R" in parts
    assert "TelescopicChromeRod" in parts
    for name,p in parts.items():
        assert p["material"] in mats
        edges=Counter();volume=0.
        for a,b,c in p["faces"]:
            A,B,C=(vertices[i] for i in (a,b,c))
            u=[B[i]-A[i] for i in range(3)]
            v=[C[i]-A[i] for i in range(3)]
            n=(u[1]*v[2]-u[2]*v[1],u[2]*v[0]-u[0]*v[2],
               u[0]*v[1]-u[1]*v[0])
            assert sum(q*q for q in n)>1e-17,("degenerate",name)
            volume+=(A[0]*(B[1]*C[2]-B[2]*C[1])+
                     A[1]*(B[2]*C[0]-B[0]*C[2])+
                     A[2]*(B[0]*C[1]-B[1]*C[0]))/6
            for x,y in ((a,b),(b,c),(c,a)):
                edges[(min(x,y),max(x,y))]+=1
        assert edges and all(n==2 for n in edges.values()),("open mesh",name)
        assert volume>1e-11,("inverted mesh",name,volume)
    assert all(-.5<v[0]<.5 and -.15<v[1]<.9 and -.35<v[2]<.6
               for v in vertices)
    return dict(parts=len(parts),vertices=len(vertices),
                triangles=report["triangles"],materials=len(mats),
                all_meshes_closed="PASS",positive_winding="PASS")

def glb_qa(path):
    raw=path.read_bytes()
    assert len(raw)>10000
    magic,version,total=struct.unpack_from("<4sII",raw)
    assert (magic,version,total)==(b"glTF",2,len(raw))
    offset=12;chunks=[]
    while offset<len(raw):
        length,kind=struct.unpack_from("<I4s",raw,offset)
        offset+=8
        chunks.append((kind,raw[offset:offset+length]))
        offset+=length
    assert offset==len(raw) and chunks[0][0]==b"JSON"
    data=json.loads(chunks[0][1].decode("utf-8").rstrip(" \x00"))
    assert data["asset"]["version"]=="2.0"
    names=[x.get("name","") for x in data["nodes"]]
    for part in ("ROOT_MOUNT_PIVOT","VEHICLE_SOCKET","LOWER_ARM_RIG",
                 "UPPER_ARM_RIG","KNUCKLE_RIG","SPRING_RIG",
                 "DAMPER_RIG","ROD_RIG","ExternalCoilSpring",
                 "LOWER_OutboardBall","UPPER_OutboardBall"):
        assert any(part in name for name in names),part
    assert len(data["materials"])>=3
    assert len(data["meshes"])>=65
    animations=data.get("animations",[])
    assert animations
    channels=[c for a in animations for c in a["channels"]]
    assert len(channels)>=6
    paths={c["target"]["path"] for c in channels}
    assert {"rotation","translation","scale"}<=paths,paths
    targets={names[c["target"]["node"]] for c in channels}
    for key in ("LOWER_ARM_RIG","UPPER_ARM_RIG","KNUCKLE_RIG",
                "SPRING_RIG","DAMPER_RIG","ROD_RIG"):
        assert any(key in n for n in targets),(key,targets)
    return dict(meshes=len(data["meshes"]),nodes=len(names),
                animations=len(animations),channels=len(channels),
                animated_paths=sorted(paths),glb_structure="PASS")

def main():
    subprocess.run([sys.executable,"-m","py_compile",str(GEN)],check=True)
    p=subprocess.run([sys.executable,str(GEN),"--self-test"],
                     capture_output=True,text=True,check=True)
    report=json.loads(p.stdout)
    assert report["status"]=="PASS"
    assert report["suspension"]["pose_samples"]==97
    assert report["mount_pivot_m"]==[0,0,0]
    assert report["hinge_axes"]=="two separate +X hinges"
    assert report["suspension"]["min_rod_engagement_m"]>.035
    assert report["suspension"]["min_coil_to_arm_envelope_m"]>.001
    assert report["suspension"]["knuckle_ball_spacing_m"]==.300
    assert report["suspension"]["coil_arm_pair_checks"]==97*12*4
    with tempfile.TemporaryDirectory(prefix="m02-010-qa-") as temp:
        a=Path(temp)/"a";b=Path(temp)/"b"
        for folder in (a,b):
            subprocess.run([sys.executable,str(GEN),"--static-obj",
                            "--out",str(folder)],
                           check=True,capture_output=True,text=True)
        actual=json.loads((a/(ID+"_qa.json")).read_text())
        assert actual["blender"]=="NOT RUN" and actual["unity"]=="NOT RUN"
        result=obj_qa(a/actual["obj"],actual)
        for filename in (actual["obj"],actual["mtl"],ID+"_qa.json"):
            assert hashlib.sha256((a/filename).read_bytes()).digest()==hashlib.sha256(
                (b/filename).read_bytes()).digest(),filename
    print(json.dumps({"static_qa":"PASS","rebuild":"BYTE_IDENTICAL",
                      "suspension":report["suspension"],**result},indent=2))
    if len(sys.argv)>1:
        print(json.dumps({"animated_qa":"PASS",
                          **glb_qa(Path(sys.argv[1]))},indent=2))

if __name__=="__main__":
    main()
