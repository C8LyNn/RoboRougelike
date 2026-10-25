#!/usr/bin/env python3
"""Independent stdlib validation of generated M02-019 GLB and Blender report."""
import json, struct, pathlib, sys, math
folder=pathlib.Path(__file__).resolve().parent/"generated"
base=folder/"M02-019"
blend=base.with_suffix(".blend")
glb=folder/"M02-019_animated.glb"
qa=folder/"M02-019_qa.json"
for f in (blend,glb,qa):
    assert f.is_file() and f.stat().st_size>100, f"Missing/empty {f}"
data=glb.read_bytes()
magic,version,total=struct.unpack_from("<4sII",data)
assert magic==b"glTF" and version==2 and total==len(data)
json_len,json_type=struct.unpack_from("<I4s",data,12)
assert json_type==b"JSON"
gltf=json.loads(data[20:20+json_len].decode("utf-8"))
assert gltf["asset"]["version"]=="2.0"
names={x.get("name","") for x in gltf.get("nodes",[])}
for required in (
    "DRUM_ROTOR__ROTATE_Y","LEFT_SHOE__PIVOT_LOWER",
    "RIGHT_SHOE__PIVOT_LOWER","WHEEL_CYLINDER_PISTON_L__SLIDE_X",
    "WHEEL_CYLINDER_PISTON_R__SLIDE_X",
    "MOUNT_AXLE_Y__PIVOT_0_0_0",
    "DRUM_CYLINDRICAL_FRICTION_SHELL",
    "HYDRAULIC_WHEEL_CYLINDER",
):
    assert required in names, f"Missing node: {required}"
assert len(gltf.get("materials",[]))>=3
assert len(gltf.get("meshes",[]))>=30
assert gltf.get("animations"), "Missing animations"
channels=sum(len(a.get("channels",[])) for a in gltf["animations"])
assert channels>=6, f"Too few animation channels: {channels}"
# Validate actual sampled glTF animation data, not merely channel metadata.
bin_header_offset=20+json_len
bin_length,bin_type=struct.unpack_from("<I4s",data,bin_header_offset)
assert bin_type==b"BIN\\x00" or bin_type==b"BIN\x00"
bin_start=bin_header_offset+8
def samples(accessor_index):
    a=gltf["accessors"][accessor_index]
    assert a["componentType"]==5126, "Expected FLOAT animation"
    comps={"SCALAR":1,"VEC3":3,"VEC4":4}[a["type"]]
    view=gltf["bufferViews"][a["bufferView"]]
    stride=view.get("byteStride",comps*4)
    start=bin_start+view.get("byteOffset",0)+a.get("byteOffset",0)
    assert start+(a["count"]-1)*stride+comps*4<=len(data)
    return [struct.unpack_from("<"+"f"*comps,data,start+i*stride) for i in range(a["count"])]
animated={}
for animation in gltf["animations"]:
    for channel in animation["channels"]:
        target=channel["target"]
        name=gltf["nodes"][target["node"]].get("name","")
        path=target["path"]
        sampler=animation["samplers"][channel["sampler"]]
        values=samples(sampler["output"])
        unique={tuple(round(v,4) for v in row) for row in values}
        animated[(name,path)]=len(unique)
for node,path,min_unique in (
    ("DRUM_ROTOR__ROTATE_Y","rotation",12),
    ("LEFT_SHOE__PIVOT_LOWER","rotation",2),
    ("RIGHT_SHOE__PIVOT_LOWER","rotation",2),
    ("WHEEL_CYLINDER_PISTON_L__SLIDE_X","translation",2),
    ("WHEEL_CYLINDER_PISTON_R__SLIDE_X","translation",2),
    ("RETURN_SPRING_TOP__STRETCH_X","scale",2),
):
    assert animated.get((node,path),0)>=min_unique, (
        f"Animation {node}/{path} is missing or static: {animated.get((node,path),0)}"
    )
report=json.loads(qa.read_text(encoding="utf-8"))
assert report["id"]=="M02-019"
assert report["material_count"]==len(gltf["materials"])==8, "Reentrant material duplication"
assert report["motion_samples"]==121
assert report["manifold_meshes_checked"]==62
assert report["degenerate_faces"]==0
assert report["minimum_radial_shoe_to_drum_clearance_m"]>.004
assert report["mount_pivot"]==[0,0,0]
print(json.dumps({
    "result":"PASS","blend_bytes":blend.stat().st_size,
    "glb_bytes":glb.stat().st_size,
    "meshes":len(gltf["meshes"]),"materials":len(gltf["materials"]),
    "animations":len(gltf["animations"]),"channels":channels,
    "animated_unique_samples":{name+" "+path:count for (name,path),count in animated.items()},
    "minimum_analytic_clearance_m":report["minimum_radial_shoe_to_drum_clearance_m"]
},indent=2))
