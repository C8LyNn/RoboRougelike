#!/usr/bin/env python3
"""Independent checks of M04-009 real static OBJ and optional animated GLB.

No Blender/Unity dependency. Tests every OBJ triangle and actual coordinates.
"""
import argparse
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
GEN=HERE/"generate_m04_009.py"

def cross(a,b):
    return (a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0])
def sub(a,b):return tuple(x-y for x,y in zip(a,b))
def dot(a,b):return sum(x*y for x,y in zip(a,b))

def check_obj(folder):
    manifest=json.loads((folder/"M04-009_manifest.json").read_text())
    assert manifest["asset"]=="M04-009" and manifest["status"]=="PASS"
    assert manifest["pivot_m"]==[0,0,0]
    assert manifest["gyro_centre_m"]==[0,0,.38]
    assert manifest["animation_frames"]==121 and manifest["fps"]==30
    assert manifest["moving_groups"]=={"OUTER":10,"MIDDLE":8,"INNER":13}
    assert manifest["sampled_cage_clearance_checks"]>500000
    assert manifest["min_cage_clearance_m"]>.008
    obj=(folder/manifest["obj"]).read_text()
    mtl=(folder/manifest["mtl"]).read_text()
    mats={line[7:] for line in mtl.splitlines() if line.startswith("newmtl ")}
    assert len(mats)==8
    objects={}
    vertices=[]
    name=None
    for line in obj.splitlines():
        if line.startswith("o "):
            name=line[2:]
            assert name not in objects
            objects[name]={"faces":[],"indices":[],"material":None}
        elif line.startswith("usemtl "):
            objects[name]["material"]=line[7:]
        elif line.startswith("v "):
            v=tuple(float(c) for c in line.split()[1:])
            assert len(v)==3 and all(math.isfinite(x) for x in v)
            vertices.append(v)
            objects[name]["indices"].append(len(vertices)-1)
        elif line.startswith("f "):
            f=tuple(int(c)-1 for c in line.split()[1:])
            assert len(f)==3
            objects[name]["faces"].append(f)
    assert len(objects)==manifest["parts"]
    assert len(vertices)==manifest["vertices"]
    assert sum(len(p["faces"]) for p in objects.values())==manifest["triangles"]
    assert {p["material"] for p in objects.values()}==mats
    for name,p in objects.items():
        owned=set(p["indices"])
        edges=Counter();oriented=Counter();volume=0.
        for f in p["faces"]:
            assert len(set(f))==3 and all(i in owned for i in f),(name,f)
            a,b,c=(vertices[i] for i in f)
            n=cross(sub(b,a),sub(c,a))
            assert dot(n,n)>1e-17,(name,"degenerate")
            volume+=dot(a,cross(b,c))/6
            for i,j in ((f[0],f[1]),(f[1],f[2]),(f[2],f[0])):
                edges[tuple(sorted((i,j)))]+=1
                oriented[(i,j)]+=1
        assert all(n==2 for n in edges.values()),(name,"open mesh")
        assert all(oriented[(j,i)]==n for (i,j),n in oriented.items()),(name,"winding")
        assert volume>1e-11,(name,"negative volume",volume)
    assert abs(min(v[2] for v in vertices))<1e-8
    assert max(v[2] for v in vertices)<.75
    assert min(v[0] for v in vertices)>-.31
    assert max(v[0] for v in vertices)<.31
    # Actual OBJ radial shells at the default rest pose. This catches stale
    # previews even if source's own build() validation passed.
    centre=(0,0,.38)
    shell={}
    for group,prefix in (("OUTER","OUTER_"),("MIDDLE","MIDDLE_"),
                         ("INNER","INNER_")):
        names=[n for n in objects if n.startswith(prefix) or
               (group=="INNER" and n.startswith("CORE_"))]
        rr=[math.dist(vertices[i],centre)
            for name in names for i in objects[name]["indices"]]
        shell[group]=(min(rr),max(rr))
    assert shell["OUTER"][0]-shell["MIDDLE"][1]>.004
    assert shell["MIDDLE"][0]-shell["INNER"][1]>.004
    assert "MOUNT_FLOOR" in objects
    assert "OUTER_GIMBAL_Z" in objects
    assert "MIDDLE_GIMBAL_Y" in objects
    assert "INNER_GIMBAL_X" in objects
    print(json.dumps({"obj":"PASS","parts":len(objects),
                      "vertices":len(vertices),
                      "triangles":manifest["triangles"],
                      "materials":len(mats),
                      "all_parts_closed_and_positive_volume":True,
                      "radial_shells":shell,
                      "mount_at_z_zero":True,
                      "cage_clearance_m":manifest["min_cage_clearance_m"]},
                     indent=2))
    return manifest

def check_glb(path):
    raw=path.read_bytes()
    assert raw[:4]==b"glTF"
    version,length=struct.unpack_from("<II",raw,4)
    assert version==2 and length==len(raw)
    chunk,typ=struct.unpack_from("<II",raw,12)
    assert typ==0x4e4f534a
    j=json.loads(raw[20:20+chunk])
    names=[x.get("name","") for x in j.get("nodes",[])]
    expected=["OUTER_Z_PIVOT","MIDDLE_Y_PIVOT","INNER_X_PIVOT"]
    for name in expected:
        assert any(name in n for n in names),(name,names)
    assert len(j.get("materials",[]))>=8
    assert len(j.get("meshes",[]))>=55
    animations=j.get("animations",[])
    assert animations,"GLB animation missing"
    channels=[c for a in animations for c in a.get("channels",[])]
    rot={names[c["target"]["node"]] for c in channels
         if c["target"]["path"]=="rotation"}
    for name in expected:
        assert any(name in n for n in rot),(name,rot)
    assert len(rot)>=3
    print(json.dumps({"glb":"PASS","meshes":len(j["meshes"]),
                      "materials":len(j["materials"]),
                      "animations":len(animations),
                      "rotation_channels":len(rot),
                      "animated_pivots":sorted(rot)},indent=2))

def main():
    ap=argparse.ArgumentParser()
    ap.add_argument("--check-glb",type=Path)
    args=ap.parse_args()
    subprocess.run([sys.executable,"-m","py_compile",str(GEN)],check=True)
    subprocess.run([sys.executable,str(GEN),"--self-test"],check=True,
                   capture_output=True,text=True)
    with tempfile.TemporaryDirectory(prefix="m04-009-qa-") as td:
        d=Path(td)
        subprocess.run([sys.executable,str(GEN),"--static-obj","--out",str(d)],
                       check=True,capture_output=True,text=True)
        check_obj(d)
        hashes={p.name:hashlib.sha256(p.read_bytes()).hexdigest()
                for p in d.iterdir()}
        subprocess.run([sys.executable,str(GEN),"--static-obj","--out",str(d)],
                       check=True,capture_output=True,text=True)
        assert hashes=={p.name:hashlib.sha256(p.read_bytes()).hexdigest()
                        for p in d.iterdir()},"nondeterministic generation"
        print("Deterministic regeneration: PASS")
    if args.check_glb:check_glb(args.check_glb)

if __name__=="__main__":main()
