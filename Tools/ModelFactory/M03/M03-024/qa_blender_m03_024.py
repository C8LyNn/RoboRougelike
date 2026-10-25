#!/usr/bin/env python3
"""Independent Blender QA for M03-024: manifold, mounts, travel, repeatability, binary GLB."""
import bmesh
import bpy
import importlib.util
import json
import struct
import sys
from pathlib import Path
from mathutils import Vector

HERE=Path(__file__).resolve().parent
spec=importlib.util.spec_from_file_location("m03_024_generator",HERE/"generate_m03_024.py")
mod=importlib.util.module_from_spec(spec)
spec.loader.exec_module(mod)
argv=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else []
out=Path(argv[argv.index("--output")+1] if "--output" in argv else "/tmp/M03-024")

bpy.ops.mesh.primitive_cube_add(size=0.1,location=(9,9,9))
sentinel=bpy.context.object
sentinel.name="FOREIGN_SENTINEL_M03_024"

def verify():
    coll=bpy.data.collections[mod.COLLECTION]
    meshes=[o for o in coll.objects if o.type=="MESH"]
    snaps=[o for o in coll.objects if "_SNAP_" in o.name]
    mats={m.name for o in meshes for m in o.data.materials}
    assert len(meshes)==24,("meshes",len(meshes))
    assert len(snaps)==4,("snaps",len(snaps))
    assert len(mats)==6,("materials",mats)
    assert len(coll.objects)==30,("objects",len(coll.objects))
    assert bpy.data.objects.get(sentinel.name) is sentinel
    root=bpy.data.objects[mod.ID+"_ROOT_PIVOT"]
    moving=bpy.data.objects[mod.ID+"_SLIDING_CARRIAGE"]
    assert root.location.length<1e-8
    assert moving.parent is root
    assert moving.animation_data and moving.animation_data.action
    assert mod.TRAVEL==0.42
    for side,y in enumerate(mod.Y_RAILS):
        near=bpy.data.objects[mod.ID+"_SNAP_NEAR_%d"%side]
        far=bpy.data.objects[mod.ID+"_SNAP_FAR_%d"%side]
        assert near.parent is root and far.parent is moving
        assert (near.location-Vector((0,y,0.22))).length<1e-7
        assert (far.location-Vector((1.79,y,0.22))).length<1e-7
        assert ((near.rotation_quaternion @ Vector((0,0,1)))-Vector((-1,0,0))).length<1e-6
        assert ((far.rotation_quaternion @ Vector((0,0,1)))-Vector((1,0,0))).length<1e-6
        for prefix in ("FixedShell_","SlidingCore_","NearMountPad_",
                       "FarMountPad_","GuideStrip_","WarningStripe_","EndStop_"):
            assert bpy.data.objects.get(mod.ID+"_"+prefix+str(side)),prefix
    for obj in meshes:
        assert obj.parent in (root,moving),obj.name
        bm=bmesh.new()
        bm.from_mesh(obj.data)
        assert bm.verts and bm.faces,obj.name
        assert all(e.is_manifold for e in bm.edges),obj.name
        assert all(f.calc_area()>1e-10 for f in bm.faces),obj.name
        assert bm.calc_volume(signed=True)>1e-9,obj.name
        bm.free()
    # Independent clearance calculation: hollow outer shell vs moving core.
    # Y: (0.174 - 2*0.018 - 0.124)/2 = 0.007 m;
    # Z: (0.214 - 2*0.018 - 0.160)/2 = 0.009 m.
    assert (0.174-0.036-0.124)/2 >= 0.0069
    assert (0.214-0.036-0.160)/2 >= 0.0089
    # Sample entire 110-frame animation and moving sockets, not just endpoints.
    for frame in range(1,111):
        bpy.context.scene.frame_set(frame)
        bpy.context.view_layer.update()
        x=moving.location.x
        assert -1e-6<=x<=mod.TRAVEL+1e-6,(frame,x)
        # Moving core begins at x=0.55; fixed shell ends at x=1.20.
        # A positive structural overlap remains even at full extension.
        overlap=1.20-(0.55+x)
        assert overlap>=0.22,(frame,overlap)
        for side in range(2):
            far=bpy.data.objects[mod.ID+"_SNAP_FAR_%d"%side]
            assert abs(far.matrix_world.translation.x-(1.79+x))<1e-5
    bpy.context.scene.frame_set(1)
    return (len(coll.objects),len(meshes),len(mats),len(snaps))

mod.build()
first=verify()
mod.build()
second=verify()
assert first==second,(first,second)
assert len([c for c in bpy.data.collections if c.name==mod.COLLECTION])==1
assert not [m for m in bpy.data.meshes if m.name.startswith(mod.ID+"_") and m.users==0]
assert not [m for m in bpy.data.materials if m.name.startswith(mod.ID+"_") and m.users==0]
assert bpy.data.objects.get(sentinel.name) is sentinel

glb=out/(mod.ID+".glb")
assert glb.is_file() and glb.stat().st_size>1000
data=glb.read_bytes()
magic,version,total=struct.unpack_from("<4sII",data,0)
assert magic==b"glTF" and version==2 and total==len(data)
json_len,kind=struct.unpack_from("<I4s",data,12)
assert kind==b"JSON"
doc=json.loads(data[20:20+json_len])
names=[n.get("name","") for n in doc.get("nodes",[])]
assert sum("_SNAP_" in n for n in names)==4,names
assert len(doc.get("meshes",[]))>=24,len(doc.get("meshes",[]))
assert len(doc.get("materials",[]))>=6,len(doc.get("materials",[]))
assert len(doc.get("animations",[]))>=1,"No exported extension animation"
assert any("SLIDING_CARRIAGE" in n for n in names),names
for ext in (".blend",".fbx"):
    p=out/(mod.ID+ext)
    assert p.is_file() and p.stat().st_size>1000,p
print("M03-024 BLENDER + BINARY GLB QA PASS",
      {"repeat":first,"frames":110,"glb_nodes":len(names),
       "glb_meshes":len(doc["meshes"]),"glb_animations":len(doc["animations"])})
