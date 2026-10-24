#!/usr/bin/env python3
"""Independent stdlib QA for real M02-013 OBJ/MTL, manifest and GLB."""
import argparse
import hashlib
import importlib.util
import json
import math
import struct
import subprocess
import sys
import tempfile
from pathlib import Path

HERE=Path(__file__).resolve().parent
GEN=HERE/"generate_m02_013.py"
STEM="M02-013_axial_rotor_engine"
def load_generator():
    spec=importlib.util.spec_from_file_location("m02_013",GEN)
    mod=importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    return mod
def sha(p):
    return hashlib.sha256(p.read_bytes()).hexdigest()
def static_check(out):
    report=json.loads((out/"M02-013_qa.json").read_text())
    obj=out/(STEM+".obj")
    mtl=out/(STEM+".mtl")
    assert obj.stat().st_size>20000
    assert mtl.stat().st_size>200
    lines=obj.read_text().splitlines()
    objects=[x[2:] for x in lines if x.startswith("o ")]
    vertices=[tuple(map(float,x.split()[1:])) for x in lines if x.startswith("v ")]
    faces=[tuple(map(int,x.split()[1:])) for x in lines if x.startswith("f ")]
    materials={x.split()[1] for x in mtl.read_text().splitlines()
               if x.startswith("newmtl ")}
    used={x.split()[1] for x in lines if x.startswith("usemtl ")}
    assert len(objects)==len(set(objects))==report["part_count"]
    assert len(vertices)==report["vertices"]
    assert len(faces)==report["triangles"]
    assert materials==used and len(materials)==report["materials"]>=3
    assert report["manifold_closed_outward"]==len(objects)
    assert report["axial_airgap_m"]>=.0289
    assert report["radial_rotor_cage_gap_m"]>.05
    assert report["bearing_outer_race_clearance_m"]>=.0009
    assert report["axis"]=="+Z"
    assert "rear mounting plane z=0" in report["pivot"]
    assert all(all(math.isfinite(v) for v in p) for p in vertices)
    assert all(len(f)==3 and all(1<=i<=len(vertices) for i in f)
               for f in faces)
    # Check OBJ is the geometry from actual source, not a stale preview.
    gen=load_generator()
    parts=gen.build()
    gen_report=gen.validate(parts)
    for key in ("part_count","vertices","triangles","materials",
                "rotor_meshes","static_meshes","axial_airgap_m",
                "radial_rotor_cage_gap_m"):
        assert gen_report[key]==report[key],key
    current=None
    exported={}
    for line in lines:
        if line.startswith("o "):
            current=line[2:]
            assert current not in exported
            exported[current]=[]
        elif line.startswith("v "):
            assert current is not None
            exported[current].append(tuple(map(float,line.split()[1:])))
    assert set(exported)=={p["name"] for p in parts}
    for part in parts:
        v=exported[part["name"]]
        assert len(v)==len(part["vertices"])
        for actual,expected in zip(v,part["vertices"]):
            assert max(abs(actual[i]-expected[i]) for i in range(3))<5.1e-9
    assert report["unity_import"]=="NOT RUN"
    return {"static":"PASS","parts":len(objects),"vertices":len(vertices),
            "triangles":len(faces),"materials":len(materials),
            "obj_sha256":sha(obj),"mtl_sha256":sha(mtl)}

def glb_check(out):
    path=out/"M02-013_animated.glb"
    data=path.read_bytes()
    assert len(data)>10000
    magic,version,length=struct.unpack_from("<III",data,0)
    assert magic==0x46546c67 and version==2 and length==len(data)
    size,typ=struct.unpack_from("<II",data,12)
    assert typ==0x4e4f534a
    gltf=json.loads(data[20:20+size].decode("utf8"))
    assert gltf["asset"]["version"]=="2.0"
    assert len(gltf["meshes"])>=100
    assert len(gltf["materials"])>=3
    nodes=gltf["nodes"]
    rotor=[i for i,n in enumerate(nodes) if n.get("name")=="ROTOR_SPIN_Z"]
    mount=[i for i,n in enumerate(nodes) if n.get("name")=="MOUNT_FRAME"]
    assert len(rotor)==len(mount)==1
    assert len(nodes[rotor[0]].get("children",[]))>=30
    assert rotor[0] in nodes[mount[0]].get("children",[])
    animations=gltf.get("animations",[])
    assert animations, "No real animation in exported GLB"
    rotating=[(a,ch) for a in animations for ch in a["channels"]
              if ch["target"]["node"]==rotor[0] and
                 ch["target"]["path"]=="rotation"]
    assert rotating, "No rotation channel targeting actual rotor"
    assert len(gltf.get("accessors",[]))>0
    report=json.loads((out/"M02-013_qa.json").read_text())
    assert report["blender"].startswith("PASS")
    assert report["glb"].startswith("PASS")
    assert report["unity_import"]=="NOT RUN"
    return {"animated_glb":"PASS","glb_sha256":sha(path),
            "glb_bytes":len(data),"meshes":len(gltf["meshes"]),
            "nodes":len(nodes),"materials":len(gltf["materials"]),
            "animations":len(animations),"rotor_rotation_channels":len(rotating)}

def main():
    parser=argparse.ArgumentParser()
    parser.add_argument("--check-blender",action="store_true")
    parser.add_argument("--out",default=None)
    args=parser.parse_args()
    subprocess.run([sys.executable,"-m","py_compile",str(GEN)],check=True)
    if args.check_blender:
        assert args.out, "Provide --out with Blender outputs"
        out=Path(args.out).resolve()
        r=static_check(out)
        r.update(glb_check(out))
        assert (out/"M02-013.blend").stat().st_size>10000
        r["blend"]="PASS"
    else:
        with tempfile.TemporaryDirectory() as tmp:
            out=Path(tmp)
            subprocess.run([sys.executable,str(GEN),"--self-test"],
                           check=True,stdout=subprocess.DEVNULL)
            subprocess.run([sys.executable,str(GEN),"--out",str(out)],
                           check=True,stdout=subprocess.DEVNULL)
            r=static_check(out)
            assert json.loads((out/"M02-013_qa.json").read_text())[
                "blender"]=="NOT RUN"
            obj_hash=r["obj_sha256"]
            subprocess.run([sys.executable,str(GEN),"--out",str(out)],
                           check=True,stdout=subprocess.DEVNULL)
            assert sha(out/(STEM+".obj"))==obj_hash
            r["repeat_determinism"]="PASS"
    print(json.dumps(r,indent=2))
if __name__=="__main__":
    main()
