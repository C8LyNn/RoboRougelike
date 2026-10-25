#!/usr/bin/env python3
"""Independent artifact-level QA for M04-015; never claims Unity testing."""
import argparse
import json
import struct
from pathlib import Path

def check(out):
    manifest_path=out/"M04-015_manifest.json"
    manifest=json.loads(manifest_path.read_text(encoding="utf-8"))
    assert manifest["id"]=="M04-015"
    assert manifest["materials"]>=3
    assert manifest["moving_meshes"]>=6
    assert manifest["static_meshes"]>=10
    assert manifest["triangles"]>1000
    assert manifest["radial_clearance_m"]>=0.05-1e-6
    assert manifest["min_collar_guard_axial_clearance_m"]>0.12
    assert manifest["mount_pivot"]==[0,0,0]
    assert len(manifest["animation_frames"])==4
    assert manifest["animation_frames"][0][1]==0
    assert manifest["animation_frames"][1][1]>0
    assert manifest["animation_frames"][2][1]<0
    blend=out/"M04-015_pneumatic_jump_leg.blend"
    glb=out/"M04-015_pneumatic_jump_leg.glb"
    fbx=out/"M04-015_pneumatic_jump_leg.fbx"
    assert blend.stat().st_size>10000
    assert blend.read_bytes()[:7]==b"BLENDER-"
    assert fbx.stat().st_size>10000
    data=glb.read_bytes()
    assert len(data)>10000 and data[:4]==b"glTF"
    version,total=struct.unpack_from("<II",data,4)
    assert version==2 and total==len(data)
    chunk_length,chunk_type=struct.unpack_from("<I4s",data,12)
    assert chunk_type==b"JSON"
    doc=json.loads(data[20:20+chunk_length].decode("utf-8"))
    assert len(doc.get("materials",[]))>=3
    assert len(doc.get("meshes",[]))>=manifest["meshes"]
    animations=doc.get("animations",[])
    assert animations,"GLB missing animation"
    channels=[channel for anim in animations for channel in anim.get("channels",[])]
    assert any(c["target"]["path"]=="translation" for c in channels),channels
    names={node.get("name") for node in doc.get("nodes",[])}
    assert "PNEUMATIC_STROKE_Y" in names
    assert "MOUNT_UNDERSIDE_Y0" in names
    print(json.dumps({"status":"PASS","meshes":manifest["meshes"],"triangles":manifest["triangles"],
                      "materials":len(doc["materials"]),"animations":len(animations),
                      "translation_channels":sum(c["target"]["path"]=="translation" for c in channels),
                      "unity":"NOT RUN"},indent=2))

if __name__=="__main__":
    p=argparse.ArgumentParser()
    p.add_argument("--out",default="build/M04-015")
    args=p.parse_args()
    check(Path(args.out))
