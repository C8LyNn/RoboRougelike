#!/usr/bin/env python3
"""Executed inside Blender: repeat-build, foreign-object safety and asset export."""
import importlib.util
import json
import os
import pathlib
import struct

import bpy

here = pathlib.Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location("m05_018_generator", here / "generate.py")
gen = importlib.util.module_from_spec(spec)
spec.loader.exec_module(gen)

# This is deliberately outside the generator's collection.
bpy.ops.mesh.primitive_cube_add(size=0.05, location=(3, 3, 3))
sentinel = bpy.context.object
sentinel.name = "FOREIGN_QA_SENTINEL_M05_018"

def snapshot():
    return {
        "objects": len(bpy.data.objects),
        "meshes": len(bpy.data.meshes),
        "actions": len(bpy.data.actions),
        "materials": len(bpy.data.materials),
    }

gen.create()
first = snapshot()
qa_first = gen.validate_geometry()
gen.create()
second = snapshot()
qa_second = gen.validate_geometry()
assert first == second, "Repeat build leaked datablocks: %r vs %r" % (first, second)
assert bpy.data.objects.get(sentinel.name) is sentinel, "Foreign object removed"
assert qa_first["closed_manifold_meshes"] == qa_second["closed_manifold_meshes"]
assert qa_second["min_head_to_base_clearance_m"] > 0.015
assert qa_second["min_head_to_yoke_side_gap_m"] > 0.025

# The QA-only sentinel must not contaminate production .blend.
mesh = sentinel.data
bpy.data.objects.remove(sentinel, do_unlink=True)
if mesh.users == 0:
    bpy.data.meshes.remove(mesh)

# main() builds again and writes GLB, BLEND and JSON QA.
gen.main()
args = __import__("sys").argv
cli = args[args.index("--") + 1:] if "--" in args else []
out = pathlib.Path(cli[cli.index("--output") + 1]) if "--output" in cli else here / "generated"
report_file = out / "M05-018-qa.json"
report = json.loads(report_file.read_text(encoding="utf-8"))
report["repeat_build"] = "PASS"
report["foreign_object_preserved"] = "PASS"
report["repeat_datablocks"] = second

raw = (out / "M05-018.glb").read_bytes()
assert raw[:4] == b"glTF", "Missing GLB magic"
assert struct.unpack_from("<I", raw, 4)[0] == 2, "GLB version != 2"
assert struct.unpack_from("<I", raw, 8)[0] == len(raw), "GLB length mismatch"
size, kind = struct.unpack_from("<II", raw, 12)
assert kind == 0x4E4F534A, "First GLB chunk not JSON"
gltf = json.loads(raw[20:20 + size].decode("utf-8"))
assert len(gltf.get("meshes", [])) >= 30
assert len(gltf.get("materials", [])) >= 3
assert len(gltf.get("animations", [])) >= 1
assert any("HEAD_TILT_PIVOT_X" in node.get("name", "")
           for node in gltf.get("nodes", []))
assert any("MOUNT_TOP" in node.get("name", "")
           for node in gltf.get("nodes", []))
report["glb_structure"] = "PASS"
report["glb_meshes"] = len(gltf["meshes"])
report["glb_materials"] = len(gltf["materials"])
report["glb_animations"] = len(gltf["animations"])
report_file.write_text(json.dumps(report, indent=2, ensure_ascii=False),
                       encoding="utf-8")
print("M05-018 Blender QA PASS", report["repeat_datablocks"])
