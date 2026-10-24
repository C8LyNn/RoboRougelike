#!/usr/bin/env python3
"""Independent export inspection for M01-010. Python stdlib only."""
import json
from pathlib import Path
import struct
import sys

BASE="M01-010_energy_fork_projector"


def verify(directory):
    d=Path(directory)
    manifest=json.loads((d/"M01-010_qa.json").read_text(encoding="utf-8"))
    assert manifest["id"]=="M01-010"
    assert manifest["status"]=="BLENDER_EXPORTED"
    assert manifest["units"]=="metres"
    assert manifest["front_axis"]=="+Z" and manifest["up_axis"]=="+Y"
    assert manifest["mount"]=="MOUNT_FRONT_SOCKET at origin"
    assert manifest["fork_hinges"]==["FORK_HINGE_L","FORK_HINGE_R"]
    assert manifest["fork_yaw_degrees"]==21.0
    assert manifest["frame_range"]==[1,64]
    assert manifest["sampled_frames"]==64
    assert manifest["mesh_objects"]>=40
    assert manifest["vertices"]>1000
    assert manifest["triangles"]>2000
    assert manifest["materials"]>=3
    assert manifest["aabb_checks"]>10000
    assert manifest["minimum_aabb_separation_m"]>0.000001
    assert manifest["mesh_closed_positive_volume"]=="PASS"
    assert manifest["fork_vs_fixed_and_opposite_fork_aabb"]=="PASS"
    assert manifest["unity_import"]=="NOT RUN"
    assert manifest["unity_playmode"]=="NOT RUN"

    blend=d/(BASE+".blend")
    fbx=d/(BASE+"_animated.fbx")
    glb=d/(BASE+"_animated.glb")
    obj=d/(BASE+".obj")
    mtl=d/(BASE+".mtl")
    for path in (blend,fbx,glb,obj,mtl):
        assert path.exists() and path.stat().st_size>500,(path,"missing or empty")
    assert blend.open("rb").read(7)==b"BLENDER"
    assert fbx.open("rb").read(18).startswith(b"Kaydara FBX Binary")
    data=glb.read_bytes()
    magic,version,length=struct.unpack_from("<III",data)
    assert magic==0x46546C67 and version==2 and length==len(data)
    chunk_length,chunk_type=struct.unpack_from("<II",data,12)
    assert chunk_type==0x4E4F534A
    doc=json.loads(data[20:20+chunk_length].decode("utf-8"))
    nodes=doc["nodes"]
    names=[n.get("name","") for n in nodes]
    assert "MOUNT_FRONT_SOCKET" in names
    for name in ("FORK_HINGE_L","FORK_HINGE_R"):
        assert name in names,(name,names)
    assert len(doc.get("materials",[]))>=3
    assert len(doc.get("meshes",[]))>=40
    channels=[(names[a["target"]["node"]],a["target"]["path"])
              for animation in doc.get("animations",[])
              for a in animation.get("channels",[])]
    assert ("FORK_HINGE_L","rotation") in channels,channels
    assert ("FORK_HINGE_R","rotation") in channels,channels
    assert ("Energy_Gate_Outer","scale") in channels,channels

    obj_text=obj.read_text(encoding="utf-8",errors="replace")
    mtl_text=mtl.read_text(encoding="utf-8",errors="replace")
    assert sum(line.startswith("v ") for line in obj_text.splitlines())>1000
    assert sum(line.startswith("f ") for line in obj_text.splitlines())>2000
    assert sum(line.startswith("newmtl ") for line in mtl_text.splitlines())>=3
    result={"status":"PASS","id":"M01-010",
            "blender":manifest["blender_version"],
            "mesh_objects":manifest["mesh_objects"],
            "materials":manifest["materials"],
            "triangles":manifest["triangles"],
            "sampled_frames":manifest["sampled_frames"],
            "aabb_checks":manifest["aabb_checks"],
            "min_clearance_m":manifest["minimum_aabb_separation_m"],
            "glb_animated_channels":channels,
            "real_exports":["BLEND","FBX","GLB","OBJ","MTL"],
            "unity":"NOT RUN"}
    print(json.dumps(result,ensure_ascii=False,indent=2))
    return result


if __name__=="__main__":
    verify(sys.argv[1] if len(sys.argv)>1 else "/tmp/m01-010")
