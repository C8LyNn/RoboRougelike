#!/usr/bin/env python3
"""Independent stdlib QA for M02-015 source and real Blender exports."""
from __future__ import annotations
import argparse
import importlib.util
import json
from pathlib import Path
import struct

HERE=Path(__file__).resolve().parent
SOURCE=HERE/"generate_m02_015.py"


def source_check():
    spec=importlib.util.spec_from_file_location("m02_015_generator",SOURCE)
    assert spec is not None and spec.loader is not None
    module=importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    result=module.self_test()
    assert result["sampled_gear_phases"]==121
    assert result["silhouette_intersections"]==0
    assert result["materials_planned"]>=3
    return result


def inspect_glb(path):
    data=path.read_bytes()
    assert len(data)>10000, "GLB suspiciously small"
    magic,version,length=struct.unpack_from("<4sII",data,0)
    assert magic==b"glTF" and version==2 and length==len(data)
    json_len,json_type=struct.unpack_from("<I4s",data,12)
    assert json_type==b"JSON"
    gltf=json.loads(data[20:20+json_len].decode("utf-8"))
    assert gltf["asset"]["version"]=="2.0"
    meshes=gltf.get("meshes",[])
    nodes=gltf.get("nodes",[])
    materials=gltf.get("materials",[])
    animations=gltf.get("animations",[])
    assert len(meshes)>=80, ("Missing model parts",len(meshes))
    assert len(materials)>=7, ("Missing PBR materials",len(materials))
    names={n.get("name",""):i for i,n in enumerate(nodes)}
    assert "INPUT_GEAR_ROT_Y" in names and "OUTPUT_GEAR_ROT_Y" in names
    assert animations, "No exported animation"
    rotation_nodes=set()
    for anim in animations:
        for channel in anim.get("channels",[]):
            if channel.get("target",{}).get("path")=="rotation":
                rotation_nodes.add(channel["target"]["node"])
    assert names["INPUT_GEAR_ROT_Y"] in rotation_nodes
    assert names["OUTPUT_GEAR_ROT_Y"] in rotation_nodes
    return {"glb_bytes":len(data),"gltf_meshes":len(meshes),
            "gltf_nodes":len(nodes),"gltf_materials":len(materials),
            "gltf_animations":len(animations),
            "animated_rotor_nodes":2}


def export_check(out):
    blend=out/"M02-015.blend"
    glb=out/"M02-015_animated.glb"
    qa=out/"M02-015_qa.json"
    assert blend.is_file() and blend.stat().st_size>10000
    assert glb.is_file() and qa.is_file()
    metadata=json.loads(qa.read_text(encoding="utf-8"))
    assert metadata["mesh_objects"]>=80
    assert metadata["materials"]>=7
    assert metadata["sampled_gear_phases"]==121
    result={"blend_bytes":blend.stat().st_size,**inspect_glb(glb)}
    fbx=out/"M02-015_animated.fbx"
    if fbx.exists():
        content=fbx.read_bytes()
        assert content.startswith(b"Kaydara FBX Binary")
        assert len(content)>10000
        result["fbx_bytes"]=len(content)
        result["fbx_status"]="EXPORTED_NOT_IMPORTED"
    else:
        result["fbx_status"]=metadata.get("fbx_export","NOT RUN")
    result["unity"]="NOT RUN"
    return result


def main():
    parser=argparse.ArgumentParser()
    parser.add_argument("--verify",action="store_true")
    parser.add_argument("--out",default="build/M02-015")
    args=parser.parse_args()
    report={"source":source_check()}
    if args.verify:
        report["exports"]=export_check(Path(args.out))
    print(json.dumps(report,indent=2,ensure_ascii=False))


if __name__=="__main__":
    main()
