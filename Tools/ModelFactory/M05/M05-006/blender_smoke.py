#!/usr/bin/env python3
"""Blender headless integration test for M05-006, run via blender -b --python."""
from __future__ import annotations
import json
import math
import struct
import sys
from pathlib import Path

import bpy

HERE=Path(__file__).resolve().parent
sys.path.insert(0,str(HERE))
import generate_m05_006 as model

argv=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else []
output=Path(argv[0] if argv else "/tmp/m05-006-blender")
output.mkdir(parents=True,exist_ok=True)
spec=model.spec_load()
report=model.validate(spec)

bpy.ops.mesh.primitive_cube_add(size=.22,location=(3.0,4.0,5.0))
sentinel=bpy.context.object
sentinel.name="FOREIGN_SENTINEL_DO_NOT_DELETE"
scene=bpy.context.scene
scene.frame_start=11
scene.frame_end=111
scene.frame_set(13)
before=(scene.frame_start,scene.frame_end,scene.frame_current)
sentinel.select_set(True)
bpy.context.view_layer.objects.active=sentinel

def owned_state():
    coll=bpy.data.collections.get("M05-006_ASSET")
    assert coll is not None
    names=[o.name for o in coll.objects]
    assert len(names)==len(spec["parts"])+3,(len(names),len(spec["parts"]))
    assert all(n.startswith("M05-006_") for n in names)
    assert len(names)==len(set(names))
    assert sentinel.name in bpy.data.objects
    assert bpy.data.objects[sentinel.name] is sentinel
    assert (scene.frame_start,scene.frame_end,scene.frame_current)==before
    assert bpy.context.view_layer.objects.active is sentinel
    assert sentinel.select_get()
    return coll

result1=model.make_blender(spec,output)
coll=owned_state()
first_meshes=len([m for m in bpy.data.meshes if m.name.startswith("M05-006_")])
first_materials=len([m for m in bpy.data.materials if m.name.startswith("M05-006_")])
assert first_meshes==len(spec["parts"]),(first_meshes,len(spec["parts"]))
assert first_materials==len(spec["materials"])

result2=model.make_blender(spec,output)
coll=owned_state()
second_meshes=len([m for m in bpy.data.meshes if m.name.startswith("M05-006_")])
second_materials=len([m for m in bpy.data.materials if m.name.startswith("M05-006_")])
assert second_meshes==first_meshes,(first_meshes,second_meshes)
assert second_materials==first_materials,(first_materials,second_materials)

left=bpy.data.objects["M05-006_HINGE_L"]
right=bpy.data.objects["M05-006_HINGE_R"]
for frame,expected in ((1,0),(16,15),(31,30),(46,15),(61,0)):
    scene.frame_set(frame)
    bpy.context.view_layer.update()
    assert abs(math.degrees(left.rotation_euler.z)+expected)<.01,(frame,left.rotation_euler.z)
    assert abs(math.degrees(right.rotation_euler.z)-expected)<.01,(frame,right.rotation_euler.z)
scene.frame_set(before[2])
assert bpy.data.objects[sentinel.name] is sentinel

blend=output/"M05-006.blend"
glb=output/"M05-006_animated.glb"
assert blend.is_file() and blend.stat().st_size>10000
assert glb.is_file() and glb.stat().st_size>10000
with bpy.data.libraries.load(str(blend)) as (source,target):
    assert "M05-006_ASSET" in source.collections,source.collections

with glb.open("rb") as f:
    header=f.read(12)
    magic,version,length=struct.unpack("<4sII",header)
    assert magic==b"glTF" and version==2
    assert length==glb.stat().st_size
    size,chunk_type=struct.unpack("<I4s",f.read(8))
    assert chunk_type==b"JSON"
    gltf=json.loads(f.read(size).decode("utf-8"))
assert gltf.get("animations"),"Animated GLB has no animation"
assert len(gltf.get("meshes",[]))==len(spec["parts"]),(len(gltf.get("meshes",[])),len(spec["parts"]))
assert all("FOREIGN_SENTINEL" not in n.get("name","") for n in gltf["nodes"])
assert any("HINGE_L" in n.get("name","") for n in gltf["nodes"])
assert any("HINGE_R" in n.get("name","") for n in gltf["nodes"])

print(json.dumps({
    "model":"M05-006","status":"PASS",
    "blender_version":bpy.app.version_string,
    "owned_objects":len(coll.objects),"meshes":second_meshes,
    "materials":second_materials,
    "animation_frames_tested":[1,16,31,46,61],
    "glb_animations":len(gltf["animations"]),
    "glb_meshes":len(gltf["meshes"]),
    "glb_bytes":glb.stat().st_size,
    "blend_bytes":blend.stat().st_size,
    "foreign_scene_preserved":True,
    "rerun_orphans":0,
    "sweep_qa":report["swept_collision_qa"]
},indent=2))
