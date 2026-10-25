#!/usr/bin/env python3
"""Run in Blender: blender -b --python qa_blender_m03_021.py -- --output /tmp/M03-021"""
import bpy
import bmesh
import importlib.util
import json
import math
import struct
import sys
from pathlib import Path
from mathutils import Vector

HERE=Path(__file__).resolve().parent
spec=importlib.util.spec_from_file_location("m03_021_generator",HERE/"generate_m03_021.py")
mod=importlib.util.module_from_spec(spec)
spec.loader.exec_module(mod)
out=Path(sys.argv[sys.argv.index("--")+2]
         if "--" in sys.argv and "--output" in sys.argv else "/tmp/M03-021")

# An unrelated object must survive generator cleanup.
bpy.ops.mesh.primitive_cube_add(size=0.1,location=(4,4,4))
foreign=bpy.context.object
foreign.name="FOREIGN_SENTINEL_DO_NOT_DELETE"

def verify():
    coll=bpy.data.collections[mod.COLLECTION]
    mesh_objs=[ob for ob in coll.objects if ob.type=="MESH"]
    assert len(mesh_objs)>=25, len(mesh_objs)
    mats={m.name for ob in mesh_objs for m in ob.data.materials}
    assert len(mats)==6, mats
    snaps=[ob for ob in coll.objects if "_SNAP_" in ob.name]
    assert len(snaps)==4, len(snaps)
    for ob in snaps:
        assert abs(ob.location.z)<1e-7
        assert ob.get("mount_normal")=="+Z"
    for ob in mesh_objs:
        bm=bmesh.new()
        bm.from_mesh(ob.data)
        assert all(e.is_manifold for e in bm.edges),ob.name
        assert all(f.calc_area()>1e-10 for f in bm.faces),ob.name
        vol=bm.calc_volume(signed=True)
        assert vol>1e-9,(ob.name,vol)
        bm.free()
    hoops=[ob for ob in mesh_objs if "_Hoop_" in ob.name]
    assert len(hoops)==2
    for ob in hoops:
        assert len(ob.data.vertices)>1200
    assert bpy.data.objects.get("FOREIGN_SENTINEL_DO_NOT_DELETE") is foreign
    assert bpy.data.objects[mod.NAME+"_ROOT_PIVOT"].location.length<1e-8
    # Hoops are separated along Y by 0.38 m, leaving >= 0.292 m
    # outer-surface gap except for explicitly welded crossmembers.
    assert 2*mod.Y_OFF-2*mod.TUBE_R>=0.29
    return (len(coll.objects),len(mesh_objs),len(mats),len(snaps))

first=verify()
# Repeat build in the same Blender session, checking no duplicated assets.
mod.build()
second=verify()
assert first==second,(first,second)
assert len([c for c in bpy.data.collections if c.name==mod.COLLECTION])==1
assert not [m for m in bpy.data.meshes if m.name.startswith(mod.NAME+"_")
            and m.users==0]
# Inspect the actual GLB bytes generated in the preceding CI step.
path=out/(mod.NAME+".glb")
assert path.is_file() and path.stat().st_size>1000
data=path.read_bytes()
magic,version,length=struct.unpack_from("<4sII",data,0)
assert magic==b"glTF" and version==2 and length==len(data)
chunk_size,chunk_type=struct.unpack_from("<I4s",data,12)
assert chunk_type==b"JSON"
doc=json.loads(data[20:20+chunk_size])
names=[node.get("name","") for node in doc.get("nodes",[])]
assert sum("_SNAP_" in name for name in names)==4,names
assert len(doc.get("materials",[]))>=6
assert len(doc.get("meshes",[]))>=25
assert not doc.get("animations"),"Catalog says static model"
print("M03-021 QA PASS",{"repeat":first,"glb_nodes":len(names),
                        "glb_meshes":len(doc["meshes"]),
                        "glb_materials":len(doc["materials"])})
