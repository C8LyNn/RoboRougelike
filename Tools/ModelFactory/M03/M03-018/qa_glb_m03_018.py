#!/usr/bin/env python3
"""Independent M03-018 binary GLB inspection; Python stdlib only."""
import json
import os
import struct
import sys

def validate(path):
    with open(path,"rb") as f:
        header=f.read(12)
        if len(header)!=12:
            raise AssertionError("truncated GLB")
        magic,version,total=struct.unpack("<4sII",header)
        assert magic==b"glTF" and version==2, (magic,version)
        assert total==os.path.getsize(path), (total,os.path.getsize(path))
        length,kind=struct.unpack("<I4s",f.read(8))
        assert kind==b"JSON"
        data=json.loads(f.read(length).decode("utf-8"))
    meshes=data.get("meshes",[])
    materials=data.get("materials",[])
    nodes=data.get("nodes",[])
    animations=data.get("animations",[])
    names=[n.get("name","") for n in nodes]
    assert len(meshes)>=20, "too few meshes: "+str(len(meshes))
    assert len(materials)>=6, "too few materials: "+str(len(materials))
    for marker in ("ROOT_MOUNT_Z0","YAW_AXIS_Z","PITCH_AXIS_X",
                   "SNAP_PAYLOAD_TOP","SNAP_BASE_-1_-1",
                   "SNAP_BASE_1_1"):
        assert any(marker in n for n in names), "missing node "+marker
    assert animations, "missing exported animation"
    animated=set()
    for anim in animations:
        for channel in anim.get("channels",[]):
            target=channel.get("target",{})
            if target.get("path")=="rotation":
                animated.add(names[target["node"]])
    assert any("YAW_AXIS_Z" in name for name in animated), animated
    assert any("PITCH_AXIS_X" in name for name in animated), animated
    print("M03-018 GLB PASS meshes=%d materials=%d nodes=%d animations=%d rotation_nodes=%s" %
          (len(meshes),len(materials),len(nodes),len(animations),sorted(animated)))

if __name__=="__main__":
    validate(sys.argv[1] if len(sys.argv)>1 else "/tmp/m03-018/M03-018.glb")
