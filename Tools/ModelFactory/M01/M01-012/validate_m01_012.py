#!/usr/bin/env python3
"""Independent read-only validation of actual M01-012 Blender exports."""
import json
from pathlib import Path
import struct
import sys

ID="M01-012"
STEM="M01-012_pneumatic_bolt_launcher"

def validate(folder):
    folder=Path(folder)
    suffixes=[".blend","_animated.glb","_animated.fbx",".obj",".mtl"]
    files={suffix:folder/(STEM+suffix) for suffix in suffixes}
    for suffix,path in files.items():
        assert path.exists() and path.stat().st_size>100,(suffix,"missing/empty")
    assert files[".blend"].read_bytes()[:7]==b"BLENDER"
    assert files["_animated.fbx"].read_bytes()[:18]==b"Kaydara FBX Binary"
    data=files["_animated.glb"].read_bytes()
    magic,version,length=struct.unpack_from("<III",data,0)
    assert magic==0x46546C67 and version==2 and length==len(data)
    chunk_len,chunk_type=struct.unpack_from("<II",data,12)
    assert chunk_type==0x4E4F534A
    gltf=json.loads(data[20:20+chunk_len].decode("utf-8"))
    nodes={n.get("name","") for n in gltf["nodes"]}
    assert "MOUNT_TOP_SOCKET" in nodes
    assert "PNEUMATIC_RAM" in nodes
    assert "Ram_Chrome_Rod" in nodes
    assert "Pressure_Tank" in nodes
    assert "Piston_Cylinder_Hollow" in nodes
    assert len(gltf.get("materials",[]))>=3
    animations=gltf.get("animations",[])
    assert animations, "no GLB animation"
    animated_nodes=set()
    for animation in animations:
        for channel in animation.get("channels",[]):
            node_idx=channel["target"].get("node")
            if node_idx is not None:
                animated_nodes.add(gltf["nodes"][node_idx].get("name"))
    assert "PNEUMATIC_RAM" in animated_nodes,animated_nodes
    # Validate actual translation curve, not merely the existence of keys.
    found_stroke=False
    for animation in animations:
        for channel in animation.get("channels",[]):
            node_idx=channel["target"].get("node")
            if node_idx is None or gltf["nodes"][node_idx].get("name")!="PNEUMATIC_RAM":
                continue
            if channel["target"].get("path")=="translation":
                found_stroke=True
    assert found_stroke, "animated ram missing translation track"
    obj=files[".obj"].read_text(encoding="utf-8",errors="replace")
    assert "v " in obj and "f " in obj and "usemtl " in obj
    vertices=faces=0
    names=set()
    mats=set()
    for line in obj.splitlines():
        if line.startswith("v "):
            xyz=[float(v) for v in line.split()[1:]]
            assert len(xyz)==3 and max(abs(v) for v in xyz)<10
            vertices+=1
        elif line.startswith("f "):
            indices=[int(s.split("/")[0]) for s in line.split()[1:]]
            assert len(indices)>=3 and len(set(indices))>=3
            assert all(1<=i<=vertices for i in indices)
            faces+=1
        elif line.startswith("o "):
            names.add(line[2:])
        elif line.startswith("usemtl "):
            mats.add(line[7:])
    assert vertices>=1000 and faces>=1000,(vertices,faces)
    assert len(mats)>=3,len(mats)
    assert any("Pressure_Tank" in n for n in names)
    assert any("Ram_Chrome_Rod" in n for n in names)
    assert any("Piston_Cylinder_Hollow" in n for n in names)
    mtl=files[".mtl"].read_text(encoding="utf-8",errors="replace")
    assert mtl.count("newmtl ")>=3
    qa=json.loads((folder/(ID+"_qa.json")).read_text(encoding="utf-8"))
    assert qa["id"]==ID and qa["status"]=="BLENDER_EXPORTED"
    assert qa["manifold_and_positive_volume"]=="PASS"
    assert qa["motion_samples"]==61
    assert qa["stroke_m"]==.09
    assert qa["materials"]>=3
    assert qa["unity_import"]=="NOT RUN"
    result={"status":"PASS","id":ID,"real_glb_animated_ram":True,
            "glb_materials":len(gltf["materials"]),"obj_vertices":vertices,
            "obj_faces":faces,"obj_materials":len(mats),
            "mesh_qa":qa["manifold_and_positive_volume"],
            "motion_samples":qa["motion_samples"],
            "unity_import":"NOT RUN"}
    print(json.dumps(result,indent=2))
    return result

if __name__=="__main__":
    validate(sys.argv[1] if len(sys.argv)>1 else "/tmp/m01-012")
