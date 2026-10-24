#!/usr/bin/env python3
"""Independent read-back QA for actual Blender-generated GLB + BLEND assets."""
import argparse
import json
import math
from pathlib import Path
import struct

def load_glb(path):
    raw=path.read_bytes()
    assert len(raw)>10000
    magic,version,total=struct.unpack_from("<III",raw,0)
    assert magic==0x46546C67 and version==2 and total==len(raw)
    length,kind=struct.unpack_from("<II",raw,12)
    assert kind==0x4E4F534A
    gltf=json.loads(raw[20:20+length])
    assert gltf["asset"]["version"]=="2.0"
    return gltf

def main():
    ap=argparse.ArgumentParser()
    ap.add_argument("--dir",required=True)
    args=ap.parse_args()
    d=Path(args.dir)
    report=json.loads((d/"M02-012_qa.json").read_text())
    assert report["id"]=="M02-012"
    assert report["blender"]=="PASS" and report["unity"]=="NOT RUN"
    assert report["kinematic_samples"]==722
    assert report["max_rod_length_error_m"]<2e-6
    assert report["min_head_clearance_m"]>.035
    assert report["radial_clearance_m"]>=.0079
    assert report["materials"]>=8
    assert report["mesh_objects"]>40
    assert report["animated_groups"]==7
    assert report["repeat_build"]=="PASS_TWO_BUILDS"
    assert (d/"M02-012.blend").stat().st_size>10000
    gltf=load_glb(d/"M02-012_animated.glb")
    mats=gltf.get("materials",[])
    meshes=gltf.get("meshes",[])
    nodes=gltf.get("nodes",[])
    assert len(mats)>=8 and len(meshes)>40 and len(nodes)>50
    names={n.get("name",""):i for i,n in enumerate(nodes)}
    targets=("CRANKSHAFT_ROTOR","PISTON_RIG_L","PISTON_RIG_R",
             "CONROD_RIG_L","CONROD_RIG_R",
             "CRANK_EYE_RIG_L","CRANK_EYE_RIG_R")
    for name in targets:
        assert any(k.endswith(name) for k in names),name
    animated=set()
    animation_count=len(gltf.get("animations",[]))
    assert animation_count>=1
    channel_count=0
    for a in gltf["animations"]:
        samplers=a["samplers"]
        for channel in a["channels"]:
            channel_count+=1
            target=channel["target"]
            assert 0<=target["node"]<len(nodes)
            assert target["path"] in ("rotation","translation","scale","weights")
            s=samplers[channel["sampler"]]
            assert 0<=s["input"]<len(gltf["accessors"])
            assert 0<=s["output"]<len(gltf["accessors"])
            assert gltf["accessors"][s["input"]]["count"]>=2
            animated.add(nodes[target["node"]].get("name",""))
    for target in targets:
        assert any(n.endswith(target) for n in animated),(target,animated)
    assert channel_count>=7
    # Verify exported material identity, not only source JSON's count.
    required=("Graphite_Cast","Brushed_Steel","Aluminium_Radiator",
              "Copper_Pipes","Ceramic_Piston","Cyan_Status")
    for material in required:
        assert any(m.get("name","").endswith(material) for m in mats),material
    print(json.dumps({"status":"PASS","id":"M02-012",
        "blender":"PASS","unity":"NOT RUN",
        "meshes":len(meshes),"materials":len(mats),"nodes":len(nodes),
        "animations":animation_count,"channels":channel_count,
        "animated_rigs":sorted(animated),
        "kinematic_samples":report["kinematic_samples"],
        "min_head_clearance_m":report["min_head_clearance_m"],
        "radial_clearance_m":report["radial_clearance_m"]},indent=2))

if __name__=="__main__":
    main()
