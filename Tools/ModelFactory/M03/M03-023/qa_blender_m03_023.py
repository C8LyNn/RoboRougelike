#!/usr/bin/env python3
"""Independent M03-023 Blender and binary GLB QA. Run in Blender after export."""
import bmesh
import bpy
import importlib.util
import json
import math
from mathutils import Vector
import struct
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location("m03_023_generator",
                                              HERE/"generate_m03_023.py")
mod = importlib.util.module_from_spec(spec)
spec.loader.exec_module(mod)
argv = sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else []
out = Path(argv[argv.index("--output")+1] if "--output" in argv
           else "/tmp/M03-023")

# Foreign content must never be destroyed by a repeat generation.
bpy.ops.mesh.primitive_cube_add(size=0.10,location=(7,7,7))
sentinel = bpy.context.object
sentinel.name = "FOREIGN_SENTINEL_DO_NOT_DELETE"


def verify_scene():
    coll=bpy.data.collections[mod.COLLECTION]
    meshes=[o for o in coll.objects if o.type=="MESH"]
    snaps=[o for o in coll.objects if "_SNAP_" in o.name]
    materials={m.name for o in meshes for m in o.data.materials}
    # 12 rails + 8 hubs + 6 * (3 annular shells + 4 braces + 4 markers).
    assert len(meshes)==86,("mesh count",len(meshes))
    assert len(materials)==6,("material count",materials)
    assert len(snaps)==6,("socket count",len(snaps))
    expected={"EdgeBeam_":12,"CornerHub_":8,"SocketCollar_":6,
              "CeramicLiner_":6,"RubberSeal_":6,"FaceBrace_":24,
              "Indicator_":24}
    for prefix,count in expected.items():
        assert len([o for o in meshes if prefix in o.name])==count,prefix
    root=bpy.data.objects[mod.ID+"_ROOT_PIVOT"]
    assert root.location.length<1e-8
    assert bpy.data.objects.get(sentinel.name) is sentinel
    for tag,center,normal in mod.FACE_SOCKETS:
        snap=bpy.data.objects[mod.ID+"_SNAP_"+tag]
        assert (snap.location-Vector(center)).length<1e-7,tag
        axis=snap.rotation_quaternion @ Vector((0,0,1))
        assert (axis-Vector(normal)).length<1e-6,(tag,axis)
        assert snap.parent==root
    for o in meshes:
        assert o.parent==root
        bm=bmesh.new()
        bm.from_mesh(o.data)
        assert bm.verts and bm.faces and all(e.is_manifold for e in bm.edges),o.name
        assert all(f.calc_area()>1e-10 for f in bm.faces),o.name
        volume=bm.calc_volume(signed=True)
        assert volume>1e-9,(o.name,volume)
        bm.free()
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
assert sum("_SNAP_" in name for name in names)==6,names
assert len(doc.get("materials",[]))>=6,len(doc.get("materials",[]))
assert len(doc.get("meshes",[]))>=86,len(doc.get("meshes",[]))
assert not doc.get("animations"),"Static model should not animate"
for ext in (".blend",".fbx"):
    p=out/(mod.ID+ext)
    assert p.is_file() and p.stat().st_size>1000,p
print("M03-023 BLENDER + GLB QA PASS",
      {"repeat":first,"glb_nodes":len(names),
       "glb_meshes":len(doc["meshes"]),
       "glb_materials":len(doc["materials"])})
