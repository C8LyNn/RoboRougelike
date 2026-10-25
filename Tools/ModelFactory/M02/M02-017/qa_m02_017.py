#!/usr/bin/env python3
"""M02-017: stdlib-only source and exported GLB/BLEND checks."""
import argparse
import importlib.util
import json
import struct
from pathlib import Path

ROOT=Path(__file__).resolve().parent
ID="M02-017"

def source_check():
    spec=importlib.util.spec_from_file_location("m02_017_generator",ROOT/"generate_m02_017.py")
    assert spec and spec.loader
    mod=importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    report=mod.math_qa()
    assert report["poses"]==121
    assert report["kinematics"]=="PASS"
    assert report["radial_guard_clearance_m"]>=.055
    assert report["side_to_spider_axial_bound_m"]>=.020
    assert len(mod.MATERIALS)>=3
    return report

def inspect_glb(path):
    data=path.read_bytes()
    assert len(data)>20000,("GLB too small",len(data))
    magic,version,total=struct.unpack_from("<4sII",data,0)
    assert (magic,version,total)==(b"glTF",2,len(data))
    chunk_size,chunk_type=struct.unpack_from("<I4s",data,12)
    assert chunk_type==b"JSON"
    gltf=json.loads(data[20:20+chunk_size].decode("utf-8"))
    assert gltf["asset"]["version"]=="2.0"
    nodes=gltf.get("nodes",[])
    names={n.get("name",""):i for i,n in enumerate(nodes)}
    required=["CARRIER_RIG","LEFT_OUTPUT_RIG","RIGHT_OUTPUT_RIG",
              "SPIDER_RIG","CHASSIS_MOUNT_SOCKET",
              "LEFT_OUTPUT_SOCKET","RIGHT_OUTPUT_SOCKET"]
    for name in required:
        assert ID+"_"+name in names,("missing node",name)
    rotated=set()
    for animation in gltf.get("animations",[]):
        for channel in animation.get("channels",[]):
            target=channel.get("target",{})
            if target.get("path")=="rotation":
                rotated.add(target.get("node"))
    for name in required[:4]:
        assert names[ID+"_"+name] in rotated,("not animated",name)
    assert len(gltf.get("materials",[]))>=7
    assert len(gltf.get("meshes",[]))>=90
    return {"glb_bytes":len(data),"meshes":len(gltf["meshes"]),
            "materials":len(gltf["materials"]),
            "nodes":len(nodes),"animations":len(gltf.get("animations",[])),
            "animated_rigs":4}

def verify(out):
    source_check()
    blend=out/(ID+".blend")
    glb=out/(ID+"_animated.glb")
    qa_path=out/(ID+"_qa.json")
    assert blend.is_file() and blend.stat().st_size>20000
    assert glb.is_file() and qa_path.is_file()
    qa=json.loads(qa_path.read_text(encoding="utf-8"))
    assert qa["poses"]==121
    assert qa["materials"]>=7
    assert qa["frames"]==121
    assert qa["bvh_pair_checks"]>=25
    assert len(qa["bvh_sampled_frames"])==5
    return {"blend_bytes":blend.stat().st_size,**inspect_glb(glb),
            "bvh_checks":qa["bvh_pair_checks"]}

def main():
    ap=argparse.ArgumentParser()
    ap.add_argument("--verify",action="store_true")
    ap.add_argument("--out",type=Path)
    args=ap.parse_args()
    result=verify(args.out) if args.verify else source_check()
    print(json.dumps(result,indent=2))

if __name__=="__main__":
    main()
