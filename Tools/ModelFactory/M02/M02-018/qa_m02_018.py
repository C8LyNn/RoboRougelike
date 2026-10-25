#!/usr/bin/env python3
"""M02-018 independent stdlib source and exported glTF checks."""
from __future__ import annotations
import argparse, importlib.util, json, struct
from pathlib import Path

ID="M02-018"
ROOT=Path(__file__).resolve().parent

def source_check():
    spec=importlib.util.spec_from_file_location("m02_018_factory",ROOT/"generate_m02_018.py")
    assert spec and spec.loader
    mod=importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    result=mod.source_self_test()
    assert result["id"]==ID
    assert result["sampled_kinematic_frames"]==121
    assert result["minimum_pad_rotor_axial_gap_m"]>=.000999
    assert result["materials_expected"]>=3
    return result

def inspect_glb(path):
    data=path.read_bytes()
    assert len(data)>20000
    magic,version,length=struct.unpack_from("<4sII",data,0)
    assert (magic,version,length)==(b"glTF",2,len(data))
    json_size,json_type=struct.unpack_from("<I4s",data,12)
    assert json_type==b"JSON"
    doc=json.loads(data[20:20+json_size].decode("utf-8"))
    assert doc["asset"]["version"]=="2.0"
    nodes=doc.get("nodes",[])
    names={x.get("name",""):i for i,x in enumerate(nodes)}
    required=("MOUNT_DATUM_ORIGIN","ROTOR_SPIN_Y","FIXED_CALIPER_CARRIER",
              "PAD_SLIDE_NEG_Y","PAD_SLIDE_POS_Y","CABLE_CAM_LEVER_PIVOT_Y",
              "PERFORATED_STEEL_ROTOR","WHEEL_AXLE_SOCKET_Y")
    assert all(name in names for name in required),[n for n in required if n not in names]
    assert len(doc.get("meshes",[]))>=45
    assert len(doc.get("materials",[]))>=8
    animated={}
    for animation in doc.get("animations",[]):
        for channel in animation.get("channels",[]):
            target=channel.get("target",{})
            animated.setdefault(target.get("node"),set()).add(target.get("path"))
    assert "rotation" in animated.get(names["ROTOR_SPIN_Y"],set())
    assert "location" not in animated.get(names["ROTOR_SPIN_Y"],set())
    assert "translation" in animated.get(names["PAD_SLIDE_NEG_Y"],set())
    assert "translation" in animated.get(names["PAD_SLIDE_POS_Y"],set())
    assert "rotation" in animated.get(names["CABLE_CAM_LEVER_PIVOT_Y"],set())
    return {"glb_bytes":len(data),"meshes":len(doc["meshes"]),
            "materials":len(doc["materials"]),"animations":len(doc.get("animations",[])),
            "nodes":len(nodes),"animated_groups":4}

def verify(out):
    source_check()
    blend=out/(ID+".blend")
    glb=out/(ID+"_animated.glb")
    report=out/(ID+"_qa.json")
    assert blend.is_file() and blend.stat().st_size>20000
    assert glb.is_file() and report.is_file()
    qa=json.loads(report.read_text(encoding="utf-8"))
    assert qa["id"]==ID
    assert qa["mesh_objects"]>=45
    assert qa["material_count"]>=8
    assert qa["disc_ventilation_holes"]==18
    assert qa["tested_blender_frames"]==121
    assert qa["min_analytic_gap_m"]>=.000999
    assert not qa["non_manifold_meshes"]
    assert qa["glb_status"]=="EXPORTED_NOT_UNITY_IMPORTED"
    result={"blend_bytes":blend.stat().st_size,**inspect_glb(glb)}
    assert qa["glb_export_scope"]=="MODEL_COLLECTION_ONLY"
    assert result["meshes"]==qa["mesh_objects"], "foreign or missing mesh in GLB"
    assert result["materials"]==qa["material_count"], "foreign or missing material in GLB"
    fbx=out/(ID+"_animated.fbx")
    if fbx.is_file():
        assert fbx.stat().st_size>20000
        assert fbx.open("rb").read(20).startswith(b"Kaydara FBX Binary")
        result["fbx_bytes"]=fbx.stat().st_size
    else:
        result["fbx_status"]=qa.get("fbx_status","NOT RUN")
    return result

if __name__=="__main__":
    parser=argparse.ArgumentParser()
    parser.add_argument("--verify",action="store_true")
    parser.add_argument("--out",type=Path)
    args=parser.parse_args()
    result=verify(args.out) if args.verify else source_check()
    print(json.dumps(result,indent=2))
