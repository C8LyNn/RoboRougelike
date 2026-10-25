#!/usr/bin/env python3
"""Run with: blender -b --factory-startup --python qa_blender_m03_016.py -- --output /tmp/m03-016"""
import importlib.util
import json
import struct
import sys
from pathlib import Path
import bpy

HERE = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location("m03_016", HERE/"generate_m03_016.py")
model = importlib.util.module_from_spec(spec)
spec.loader.exec_module(model)
args = sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else []
out = Path(args[args.index("--output")+1] if "--output" in args else "/tmp/m03-016").resolve()
meshes = model.build()
model.write_obj(out,meshes)

def state():
    coll = bpy.data.collections[model.COLLECTION]
    return (len(coll.objects),len(bpy.data.meshes),
            len([o for o in coll.objects if o.type=="MESH"]),
            len([o for o in coll.objects if o.name.startswith("SNAP_")]))

model.blender_export(out,meshes)
first = state()
model.blender_export(out,meshes)
second = state()
assert first == second, ("Non-idempotent Blender build",first,second)
assert second[0] == 29 and second[2] == 24 and second[3] == 4, second
glb = (out/"M03-016.glb").read_bytes()
assert glb[:4] == b"glTF"
length, = struct.unpack_from("<I",glb,8)
assert length == len(glb)
json_length,json_type = struct.unpack_from("<II",glb,12)
assert json_type == 0x4E4F534A
doc = json.loads(glb[20:20+json_length].decode("utf-8"))
assert len(doc["meshes"]) == 24, len(doc["meshes"])
assert len(doc["materials"]) == 5, len(doc["materials"])
nodes = [n.get("name","") for n in doc["nodes"]]
assert len([n for n in nodes if n.startswith("SNAP_")]) == 4, nodes
assert any(n=="M03_016_ROOT_PIVOT" for n in nodes)
assert not doc.get("animations"), "Static catalog entry must not animate"
assert (out/"M03-016.fbx").stat().st_size > 1000
assert (out/"M03-016.blend").stat().st_size > 1000
print(json.dumps({"status":"PASS","blender":bpy.app.version_string,
                  "meshes":24,"materials":5,"snaps":4,
                  "objects_first":first[0],"objects_second":second[0],
                  "orphan_mesh_growth":second[1]-first[1],
                  "glb_bytes":len(glb)},sort_keys=True))
