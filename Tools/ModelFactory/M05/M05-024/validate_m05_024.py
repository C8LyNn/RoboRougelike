#!/usr/bin/env python3
"""Independent GLB and QA-json verification for M05-024. Stdlib only."""
import json
import struct
import sys
from pathlib import Path

def glb_json(path):
    data=Path(path).read_bytes()
    assert len(data)>=20 and data[:4]==b"glTF", "Not a GLB file"
    version,length=struct.unpack_from("<II",data,4)
    assert version==2 and length==len(data), "Bad GLB header/length"
    offset=12
    while offset<len(data):
        n,kind=struct.unpack_from("<I4s",data,offset)
        offset+=8
        chunk=data[offset:offset+n]
        offset+=n
        if kind==b"JSON":
            return json.loads(chunk)
    raise AssertionError("Missing GLB JSON chunk")

def main(path):
    folder=Path(path)
    data=glb_json(folder/"M05-024.glb")
    qa=json.loads((folder/"qa_m05_024.json").read_text())
    mats={m.get("name","") for m in data.get("materials",[])}
    nodes={n.get("name","") for n in data.get("nodes",[])}
    meshes=data.get("meshes",[])
    animations=data.get("animations",[])
    assert len(meshes)>=35, f"Too few meshes: {len(meshes)}"
    assert len(mats)>=8, f"Too few materials: {mats}"
    assert all("M05_024_"+m in mats for m in ("graphite","titanium","ceramic","copper","ice_blue","cyan_led","amber_led")),mats
    for marker in ("MOUNT_ROOT","FLOW_IN","FLOW_OUT","L_PISTON_PIVOT","R_PISTON_PIVOT"):
        assert any(n.endswith(marker) for n in nodes),f"Missing GLB marker: {marker}"
    assert animations, "No exported piston animation"
    channels=sum(len(a.get("channels",[])) for a in animations)
    assert channels>=2,f"Expected two animated pistons, found {channels} channels"
    assert qa["status"]=="BLENDER_PASS"
    assert qa["counts"]["meshes"]>=35
    assert qa["kinematics"]["samples"]==122
    assert qa["kinematics"]["minimum_analytic_gap_m"]>=0.025
    assert (folder/"M05-024.blend").stat().st_size>10000
    print(json.dumps({"result":"PASS","glb_meshes":len(meshes),
        "glb_materials":len(mats),"glb_animations":len(animations),
        "glb_animation_channels":channels,"blender_meshes":qa["counts"]["meshes"]}))
if __name__=="__main__":
    main(sys.argv[1] if len(sys.argv)>1 else "out")
