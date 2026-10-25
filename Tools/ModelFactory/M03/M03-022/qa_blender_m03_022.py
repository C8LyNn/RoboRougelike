#!/usr/bin/env python3
"""Independent M03-022 Blender and binary GLB QA. Run in Blender after export."""
import bmesh
import bpy
import importlib.util
import json
import math
import struct
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location("m03_022_generator",
                                              HERE/"generate_m03_022.py")
mod = importlib.util.module_from_spec(spec)
spec.loader.exec_module(mod)
argv = sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else []
out = Path(argv[argv.index("--output")+1] if "--output" in argv
           else "/tmp/M03-022")

# Foreign content must never be destroyed by a repeat generation.
bpy.ops.mesh.primitive_cube_add(size=0.10,location=(7,7,7))
sentinel = bpy.context.object
sentinel.name = "FOREIGN_SENTINEL_DO_NOT_DELETE"


def verify_scene():
    coll=bpy.data.collections[mod.COLLECTION]
    meshes=[ob for ob in coll.objects if ob.type=="MESH"]
    snaps=[ob for ob in coll.objects if "_SNAP_" in ob.name]
    materials={mat.name for ob in meshes for mat in ob.data.materials}
    assert len(meshes)==41,("mesh count",len(meshes))
    assert len(materials)==6,("material count",materials)
    assert len(snaps)==4,("mount count",len(snaps))
    assert len([ob for ob in meshes if "TriangleDiagonal_" in ob.name])==8
    assert len([ob for ob in meshes if "NodeCollar_" in ob.name])==9
    assert len([ob for ob in meshes if "Rail_" in ob.name])==2
    assert len([ob for ob in meshes if "MountStandoff_" in ob.name])==4
    for i,snap in enumerate(sorted(snaps,key=lambda ob:ob.name)):
        expected=mod.SNAPS[i]
        assert all(abs(snap.location[j]-expected[j])<1e-7
                   for j in range(3)),(snap.name,tuple(snap.location))
        assert snap.get("mount_normal")=="+Y"
    assert bpy.data.objects[mod.ID+"_ROOT_PIVOT"].location.length<1e-8
    assert bpy.data.objects.get(sentinel.name) is sentinel
    for ob in meshes:
        bm=bmesh.new()
        bm.from_mesh(ob.data)
        assert all(e.is_manifold for e in bm.edges),ob.name
        assert all(f.calc_area()>1e-10 for f in bm.faces),ob.name
        volume=bm.calc_volume(signed=True)
        assert volume>1e-9,(ob.name,volume)
        bm.free()
    # The zigzag uses alternating top/bottom nodes; adjacent bays
    # share endpoints but do not cross away from those welded nodes.
    assert len(mod.X_NODES)==9 and len(mod.Z_NODES)==9
    assert all(mod.X_NODES[i]<mod.X_NODES[i+1] for i in range(8))
    assert all(mod.Z_NODES[i]*mod.Z_NODES[i+1]<0 for i in range(8))
    # Consecutive diagonals touch only at one design node. Non-neighbours
    # have >=0.20 m longitudinal separation at their closest X extents,
    # excluding the rails and intentional weld sleeves.
    assert min(mod.X_NODES[i+2]-mod.X_NODES[i+1]
               for i in range(7))>=0.199
    assert all(abs(ob.location.y)<1e-7 for ob in snaps)
    return (len(coll.objects),len(meshes),len(materials),len(snaps))


mod.build()
first=verify_scene()
mod.build()
second=verify_scene()
assert first==second,(first,second)
assert len([c for c in bpy.data.collections if c.name==mod.COLLECTION])==1
assert not [m for m in bpy.data.meshes
            if m.name.startswith(mod.ID+"_") and m.users==0]
assert not [m for m in bpy.data.materials
            if m.name.startswith(mod.ID+"_") and m.users==0]

# Validate actual GLB from separate exporter execution, not just bpy memory.
glb=out/(mod.ID+".glb")
assert glb.is_file() and glb.stat().st_size>1000,glb
data=glb.read_bytes()
magic,version,total=struct.unpack_from("<4sII",data,0)
assert magic==b"glTF" and version==2 and total==len(data)
json_len,kind=struct.unpack_from("<I4s",data,12)
assert kind==b"JSON"
doc=json.loads(data[20:20+json_len])
names=[n.get("name","") for n in doc.get("nodes",[])]
assert sum("_SNAP_" in name for name in names)==4,names
assert len(doc.get("materials",[]))>=6,len(doc.get("materials",[]))
assert len(doc.get("meshes",[]))>=41,len(doc.get("meshes",[]))
assert not doc.get("animations"),"Static model should not animate"
for ext in (".blend",".fbx"):
    p=out/(mod.ID+ext)
    assert p.is_file() and p.stat().st_size>1000,p
print("M03-022 BLENDER + GLB QA PASS",
      {"repeat":first,"glb_nodes":len(names),
       "glb_meshes":len(doc["meshes"]),
       "glb_materials":len(doc["materials"])})
