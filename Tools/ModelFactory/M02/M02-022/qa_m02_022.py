#!/usr/bin/env python3
"""Independent M02-022 kinematic and binary GLB verification (stdlib only)."""
import argparse
import ast
import json
import math
import struct
from pathlib import Path

HERE=Path(__file__).resolve().parent
ID="M02-022"
FRAMES=121

def math_checks():
    min_d=100
    max_d=0
    min_rod=100
    min_beam=100
    for frame in range(1,FRAMES+1):
        t=math.radians(24)*math.sin(2*math.pi*(frame-1)/120)
        for side in (-1,1):
            ax,ay=side*.35,-.28
            bx=side*.68+side*.11*math.cos(t)+.18*math.sin(t)
            by=side*.11*math.sin(t)-.18*math.cos(t)
            d=math.hypot(bx-ax,by-ay)
            min_d=min(min_d,d)
            max_d=max(max_d,d)
            min_rod=min(min_rod,d-.015-.245)
            min_beam=min(min_beam,.68+.12*math.cos(t)-.605)
            assert .355<d<.56,(frame,side,d)
            assert d-.015-.245>.085
    assert min_beam>.12
    assert .023-.016>.006
    assert .041-.028>.012
    assert (.13-.10/2)-(.055+.014)>.010 # Moving fork clears fixed bearing.
    return {"samples":FRAMES*2,"min_actuator_m":min_d,
        "max_actuator_m":max_d,"min_rod_exposure_m":min_rod,
        "min_hub_beam_margin_m":min_beam}

def check_script():
    source=(HERE/"generate_m02_022.py").read_text(encoding="utf-8")
    tree=ast.parse(source)
    compile(tree,str(HERE/"generate_m02_022.py"),"exec")
    names={n.name for n in tree.body if isinstance(n,ast.FunctionDef)}
    assert {"analytical_qa","build","topology","main"}.issubset(names)
    assert "bpy.ops.export_scene.gltf" in source
    assert "bpy.ops.wm.save_as_mainfile" in source
    assert "MOUNT_UNDERSIDE_Z0_UNITY_1M" in source

def check_glb():
    out=HERE/"generated"
    blend=out/(ID+".blend")
    glb=out/(ID+"_animated.glb")
    qa=out/(ID+"_qa.json")
    assert blend.stat().st_size>10000
    assert glb.stat().st_size>10000
    data=glb.read_bytes()
    magic,version,length=struct.unpack_from("<4sII",data,0)
    assert magic==b"glTF" and version==2 and length==len(data)
    json_length,json_type=struct.unpack_from("<I4s",data,12)
    assert json_type==b"JSON"
    root=json.loads(data[20:20+json_length].decode("utf-8"))
    names={n.get("name","") for n in root.get("nodes",[])}
    assert len(root.get("meshes",[]))>=65
    assert len(root.get("materials",[]))>=8
    for target in ("STEER_L_KINGPIN_Z","STEER_R_KINGPIN_Z",
        "ACTUATOR_L_AIM","ACTUATOR_R_AIM",
        "L_moving_piston_rod","R_moving_piston_rod"):
        assert target in names,("missing",target)
    animations=root.get("animations",[])
    assert animations,"no animated glTF tracks"
    channel_nodes=set()
    channel_paths=set()
    for anim in animations:
        for channel in anim.get("channels",[]):
            target=channel.get("target",{})
            channel_paths.add(target.get("path"))
            idx=target.get("node")
            if isinstance(idx,int):
                channel_nodes.add(root["nodes"][idx].get("name",""))
    assert len(channel_nodes)>=6,channel_nodes
    assert {"rotation","translation","scale"}.issubset(channel_paths),channel_paths
    for target in ("STEER_L_KINGPIN_Z","STEER_R_KINGPIN_Z",
        "ACTUATOR_L_AIM","ACTUATOR_R_AIM",
        "L_moving_piston_rod","R_moving_piston_rod"):
        assert target in channel_nodes,("unanimated",target)
    report=json.loads(qa.read_text(encoding="utf-8"))
    assert report["id"]==ID
    assert report["meshes"]>=65
    assert report["materials"]==8
    assert report["reentrant"]==report["manifold"]=="PASS"
    return {"blend_bytes":blend.stat().st_size,"glb_bytes":len(data),
        "meshes":len(root["meshes"]),"materials":len(root["materials"]),
        "animations":len(animations),"animated_nodes":len(channel_nodes),
        "channel_paths":sorted(channel_paths)}

def main():
    p=argparse.ArgumentParser()
    p.add_argument("--math-only",action="store_true")
    args=p.parse_args()
    check_script()
    result={"id":ID,"math":math_checks()}
    if not args.math_only: result["glb"]=check_glb()
    print(json.dumps(result,sort_keys=True,indent=2))

if __name__=="__main__":
    main()
