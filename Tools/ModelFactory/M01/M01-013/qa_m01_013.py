#!/usr/bin/env python3
"""Independent M01-013 verification of generated OBJ/MTL and Blender files.

Requires only Python stdlib; --require-blender additionally verifies real
BLEND, FBX, and GLB files and a rotor rotation animation channel.
"""
from __future__ import annotations
import argparse
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
GEN=HERE/"generate_m01_013.py"
STEM="M01-013_disruption_net_launcher"

def parse_obj(path):
    objects={};current=None;vs=[];faces=[];used=set()
    for line in path.read_text(encoding="utf-8").splitlines():
        if line.startswith("o "):
            current=line[2:]
            assert current not in objects,("duplicate OBJ name",current)
            objects[current]={"vertices":[],"faces":[]}
        elif line.startswith("v "):
            assert current is not None
            vertex=tuple(map(float,line.split()[1:]))
            assert len(vertex)==3 and all(math.isfinite(x) for x in vertex)
            objects[current]["vertices"].append(vertex)
            vs.append(vertex)
        elif line.startswith("f "):
            assert current is not None
            face=tuple(map(int,line.split()[1:]))
            assert len(face)==3 and all(1<=x<=len(vs) for x in face)
            objects[current]["faces"].append(face)
            faces.append(face)
        elif line.startswith("usemtl "):used.add(line.split()[1])
    return objects,vs,faces,used

def check_glb(path):
    blob=path.read_bytes()
    assert len(blob)>100 and blob[:4]==b"glTF"
    magic,version,total=struct.unpack_from("<4sII",blob)
    assert magic==b"glTF" and version==2 and total==len(blob)
    length,typ=struct.unpack_from("<II",blob,12)
    assert typ==0x4E4F534A
    data=json.loads(blob[20:20+length].decode("utf-8"))
    nodes=data.get("nodes",[])
    rotor=[i for i,n in enumerate(nodes) if "CASSETTE_ROT_Z" in n.get("name","")]
    assert len(rotor)==1,("missing/duplicate rotor node",rotor)
    animations=data.get("animations",[])
    targets=[ch.get("target",{}) for anim in animations
             for ch in anim.get("channels",[])]
    rotation=[t for t in targets if t.get("node")==rotor[0]
              and t.get("path")=="rotation"]
    assert rotation,("missing cassette rotation animation",targets)
    assert len(data.get("materials",[]))>=7
    assert len(data.get("meshes",[]))>=84
    assert len(data.get("accessors",[]))>0
    return {"glb_bytes":len(blob),"glb_meshes":len(data["meshes"]),
            "glb_materials":len(data["materials"]),
            "glb_animations":len(animations),
            "rotor_rotation_channels":len(rotation)}

def verify(out,require_blender=False):
    manifest=json.loads((out/(STEM+"_qa.json")).read_text(encoding="utf-8"))
    assert manifest["asset_id"]=="M01-013"
    assert manifest["materials"]==7
    assert manifest["watertight_oriented_positive_volume"]=="PASS"
    assert manifest["sampled_animation_frames"]==73
    assert manifest["aabb_pair_checks"]==105850
    assert manifest["moving_vs_fixed_collisions"]==0
    assert manifest["minimum_separation_aabb_m"]>=.0179
    assert manifest["minimum_axle_to_moving_mesh_radial_clearance_m"]>=.015
    assert manifest["mount"].startswith("MOUNT_TOP_SOCKET (0,0,0)")
    obj=out/(STEM+".obj");mtl=out/(STEM+".mtl")
    objects,vs,faces,used=parse_obj(obj)
    materials={l.split()[1] for l in mtl.read_text(encoding="utf-8").splitlines()
               if l.startswith("newmtl ")}
    assert used==materials and len(materials)==7
    assert len(objects)==manifest["parts"]==84
    assert len(vs)==manifest["vertices"]==2354
    assert len(faces)==manifest["triangles"]==4416
    assert "CASSETTE_ROT_Z" in manifest["motion"]
    assert "360 degrees" in manifest["motion"]
    assert {"Hex_Guide_Roller_01","Hex_Guide_Roller_06",
            "Cassette_Hub","Fixed_Axle","Mount_Top_Socket"}.issubset(objects)
    assert len([x for x in objects if x.startswith("Hex_Guide_Roller_")])==6
    # Independently compare OBJ vertices and faces with actual procedural
    # source, not only the self-declared manifest.
    spec=importlib.util.spec_from_file_location("m01_013_gen",GEN)
    module=importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    parts=module.build()
    assert len(parts)==len(objects)
    cursor=0
    for p in parts:
        actual=objects[p["name"]]
        assert len(actual["vertices"])==len(p["vertices"])
        assert len(actual["faces"])==len(p["faces"])
        for a,b in zip(actual["vertices"],p["vertices"]):
            assert max(abs(a[i]-b[i]) for i in range(3))<5.1e-9
        for face,expected in zip(actual["faces"],p["faces"]):
            assert face==tuple(i+cursor+1 for i in expected)
        cursor+=len(p["vertices"])
    assert cursor==len(vs)
    result={"result":"PASS","parts":len(objects),"vertices":len(vs),
            "triangles":len(faces),"materials":len(materials),
            "sampled_frames":73,"aabb_pair_checks":105850,
            "axle_radial_clearance_m":manifest["minimum_axle_to_moving_mesh_radial_clearance_m"],
            "actual_obj_mtl":True,"blender":"NOT RUN","unity":"NOT RUN"}
    if require_blender:
        assert manifest["backend"]=="blender-bpy"
        assert manifest["blender_repeatability"].startswith("PASS")
        assert manifest["blender_export"].startswith("PASS")
        assert (out/(STEM+".blend")).read_bytes()[:7]==b"BLENDER"
        assert (out/(STEM+".fbx")).read_bytes()[:18]==b"Kaydara FBX Binary"
        result.update(check_glb(out/(STEM+".glb")))
        result["blender"]="PASS"
    else:
        assert manifest["unity_import"]=="NOT RUN"
    return result

def main():
    p=argparse.ArgumentParser()
    p.add_argument("--out",type=Path)
    p.add_argument("--require-blender",action="store_true")
    args=p.parse_args()
    subprocess.run([sys.executable,"-m","py_compile",str(GEN)],check=True)
    if args.out:
        result=verify(args.out,args.require_blender)
    else:
        with tempfile.TemporaryDirectory() as tmp:
            out=Path(tmp)
            subprocess.run([sys.executable,str(GEN),"--out",str(out)],check=True,
                           stdout=subprocess.DEVNULL)
            result=verify(out)
            sha1=hashlib.sha256((out/(STEM+".obj")).read_bytes()).hexdigest()
            subprocess.run([sys.executable,str(GEN),"--out",str(out)],check=True,
                           stdout=subprocess.DEVNULL)
            sha2=hashlib.sha256((out/(STEM+".obj")).read_bytes()).hexdigest()
            assert sha1==sha2,("non-deterministic OBJ",sha1,sha2)
            result["deterministic_obj_sha256"]=sha1
    print(json.dumps(result,indent=2,ensure_ascii=False))

if __name__=="__main__":main()
