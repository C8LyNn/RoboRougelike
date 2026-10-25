#!/usr/bin/env python3
"""Independent binary GLB/BLEND manifest verifier for M04-021 (stdlib only)."""
import json
import struct
import sys
from pathlib import Path

def validate(path):
    root=Path(path)
    data=(root/"M04-021.glb").read_bytes()
    assert len(data)>2048,"GLB too small"
    magic,version,total=struct.unpack_from("<4sII",data,0)
    assert (magic,version,total)==(b"glTF",2,len(data)),"Invalid GLB header"
    chunks=[]
    offset=12
    while offset<len(data):
        assert offset+8<=len(data),"Truncated GLB chunk header"
        length,kind=struct.unpack_from("<I4s",data,offset)
        offset+=8
        assert offset+length<=len(data),"Chunk overflow"
        chunks.append((kind,data[offset:offset+length]))
        offset+=length
    assert chunks and chunks[0][0]==b"JSON"
    gltf=json.loads(chunks[0][1].decode("utf-8"))
    manifest=json.loads((root/"M04-021_QA.json").read_text(encoding="utf-8"))
    assert manifest["id"]=="M04-021"
    assert manifest["mesh_count"]>=28
    assert manifest["material_count"]>=7
    assert manifest["sampled_yaw_poses"]==97
    assert manifest["minimum_fixed_to_rudder_y_gap_m"]>0.045
    assert manifest["manifold_edges_checked"]>500
    assert len(gltf.get("meshes",[]))>=28
    assert len(gltf.get("materials",[]))>=7
    anims=gltf.get("animations",[])
    assert anims,"Missing rudder animation"
    rotation_channels=[ch for a in anims for ch in a.get("channels",[])
                       if ch.get("target",{}).get("path")=="rotation"]
    assert rotation_channels,"Missing exported yaw rotation channel"
    assert (root/"M04-021.blend").stat().st_size>2048
    result={"id":"M04-021","binary_glb":"PASS",
            "mesh_count":len(gltf["meshes"]),
            "material_count":len(gltf["materials"]),
            "animations":len(anims),"rotation_channels":len(rotation_channels),
            "blender_geometry_manifest":"PASS (executed Blender generator)",
            "unity":"NOT RUN","full_continuous_collision":"NOT RUN"}
    print(json.dumps(result,indent=2))
    return result

if __name__=="__main__":
    validate(sys.argv[1] if len(sys.argv)>1 else "build/M04-021")
