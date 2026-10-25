#!/usr/bin/env python3
"""Static contract checks only. Real mesh and export QA run inside Blender."""
import ast
import json
from pathlib import Path

HERE = Path(__file__).resolve().parent
generator = HERE / "generate_m03_017.py"
blender_qa = HERE / "qa_blender_m03_017.py"
for file in (generator, blender_qa):
    ast.parse(file.read_text(encoding="utf-8"), filename=str(file))
source = generator.read_text(encoding="utf-8")
required = (
    "bpy.ops.export_scene.gltf", "bpy.ops.export_scene.fbx",
    "bpy.ops.wm.save_as_mainfile", "bmesh.new()",
    "SNAP_", "ANIM_LOCK_BOLT_PLUS_X", "ANIM_",
    "keyframe_insert", "ROOT_PIVOT_MOUNT_Z0",
    "clear_previous()", "bore_clearance_m", "lock_travel_m",
)
missing = [token for token in required if token not in source]
assert not missing, "Missing source contracts: " + ", ".join(missing)
assert source.count('"graphite":') == 1
assert source.count('"titanium":') == 1
assert source.count('"copper":') == 1
assert source.count('"ceramic":') == 1
assert source.count('"red":') == 1
assert source.count('"green":') == 1
print("M03-017 STATIC_SOURCE_QA_PASS " + json.dumps(
    {"id": "M03-017", "syntax": "PASS",
     "source_contracts": len(required), "materials_declared": 6}))
