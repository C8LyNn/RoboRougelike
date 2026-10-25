#!/usr/bin/env python3
"""Independent stdlib validation of M02-016 source and actual Blender exports."""
from __future__ import annotations
import argparse, importlib.util, json, struct
from pathlib import Path

ROOT=Path(__file__).resolve().parent
ID="M02-016"

def source_check():
    spec=importlib.util.spec_from_file_location("m02_016_factory",ROOT/"generate_m02_016.py")
    assert spec and spec.loader
    mod=importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    result=mod.source_self_test()
    assert result["id"]==ID
    assert result["sampled_gear_phases"]==121
    assert result["teeth_input"]==16 and result["teeth_output"]==24
    assert abs(result["ratio"]+2/3)<1e-12
    return result

def inspect_glb(path):
    data=path.read_bytes()
    assert len(data)>20000
    magic,version,length=struct.unpack_from("<4sII",data,0)
    assert (magic,version,length)==(b"glTF",2,len(data))
    chunk_len,chunk_type=struct.unpack_from("<I4s",data,12)
    assert chunk_type==b"JSON"
    gltf=json.loads(data[20:20+chunk_len].decode("utf-8"))
    assert gltf["asset"]["version"]=="2.0"
    meshes=gltf.get("meshes",[])
    materials=gltf.get("materials",[])
    nodes=gltf.get("nodes",[])
    animations=gltf.get("animations",[])
    assert len(meshes)>=60,("missing meshes",len(meshes))
    assert len(materials)>=7,("missing materials",len(materials))
    names={n.get("name",""):i for i,n in enumerate(nodes)}
    for name in ("INPUT_BEVEL_ROT_X","OUTPUT_BEVEL_ROT_Y","MOUNT_DATUM_ORIGIN",
                 "INPUT_SHAFT_SOCKET_X","OUTPUT_SHAFT_SOCKET_Y"):
        assert name in names,("missing pivot/socket",name)
    rotated=set()
    for anim in animations:
        for channel in anim.get("channels",[]):
            target=channel.get("target",{})
            if target.get("path")=="rotation":
                rotated.add(target.get("node"))
    assert names["INPUT_BEVEL_ROT_X"] in rotated
    assert names["OUTPUT_BEVEL_ROT_Y"] in rotated
    return {"glb_bytes":len(data),"meshes":len(meshes),"materials":len(materials),
            "nodes":len(nodes),"animations":len(animations),"rotating_nodes":2}

def verify(out):
    source_check()
    blend=out/(ID+".blend")
    glb=out/(ID+"_animated.glb")
    report=out/(ID+"_qa.json")
    assert blend.is_file() and blend.stat().st_size>20000
    assert glb.is_file() and report.is_file()
    qa=json.loads(report.read_text(encoding="utf-8"))
    assert qa["id"]==ID
    assert qa["mesh_objects"]>=60
    assert qa["materials"]>=7
    assert qa["sampled_gear_phases"]==121
    assert qa["cross_gear_bvh_hits"]==0
    assert len(qa["sampled_bvh_frames"])>=10
    assert qa["glb_status"]=="EXPORTED_NOT_UNITY_IMPORTED"
    result={"blend_bytes":blend.stat().st_size,**inspect_glb(glb)}
    fbx=out/(ID+"_animated.fbx")
    if fbx.is_file():
        assert fbx.stat().st_size>20000
        assert fbx.open("rb").read(20).startswith(b"Kaydara FBX Binary")
        result["fbx_bytes"]=fbx.stat().st_size
        result["fbx_status"]="EXPORTED_NOT_UNITY_IMPORTED"
    else:
        result["fbx_status"]=qa.get("fbx_status","NOT RUN")
    return result

def main():
    parser=argparse.ArgumentParser()
    parser.add_argument("--verify",action="store_true")
    parser.add_argument("--out",type=Path)
    args=parser.parse_args()
    result=verify(args.out) if args.verify else source_check()
    print(json.dumps(result,indent=2))

if __name__=="__main__":
    main()
