#!/usr/bin/env python3
"""Independent Blender-side geometry, repeated-build and 145-frame QA for M03-019."""
import bpy
import bmesh
import importlib.util
import math
from pathlib import Path
from mathutils import Vector

SOURCE = Path(__file__).with_name("generate_m03_019.py")
spec = importlib.util.spec_from_file_location("m03_019_generator", SOURCE)
gen = importlib.util.module_from_spec(spec)
spec.loader.exec_module(gen)

# A foreign sentinel proves that rebuilding cannot erase unrelated scene content.
bpy.ops.mesh.primitive_cube_add(size=.01, location=(4, 4, 4))
sentinel = bpy.context.object
sentinel.name = "FOREIGN_SENTINEL_M03_019"

def snapshot(col):
    meshes = [o for o in col.objects if o.type == "MESH"]
    materials = {m.name for o in meshes for m in o.data.materials}
    return len(col.objects), len(meshes), len(materials)

col = gen.build_model()
first = snapshot(col)
assert first[1] >= 35, first
assert first[2] >= 6, first
col = gen.build_model()
second = snapshot(col)
assert first == second, ("non-idempotent", first, second)
assert bpy.data.objects.get(sentinel.name) is sentinel
assert len([c for c in bpy.data.collections if c.name == gen.COLLECTION]) == 1
assert len([m for m in bpy.data.materials if m.name.startswith(gen.TAG + "_")]) == first[2]

meshes = [o for o in col.objects if o.type == "MESH"]
for obj in meshes:
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    assert all(e.is_manifold for e in bm.edges), "nonmanifold " + obj.name
    assert bm.calc_volume(signed=True) > 1e-9, "inverted or zero volume " + obj.name
    bm.free()
    assert all(s > 0 for s in obj.scale), obj.name

snap = [o for o in col.objects if "SNAP_BOTTOM_" in o.name]
assert len(snap) == 4
assert all(abs(o.location.z) < 1e-8 for o in snap)
hinges = [o for o in col.objects if "DOOR_" in o.name and "_HINGE_Y" in o.name]
latches = [o for o in col.objects if "LATCH_" in o.name and "_AXIS_Z" in o.name]
assert len(hinges) == 2 and len(latches) == 2
assert all(o.animation_data and o.animation_data.action for o in hinges + latches)

def world_bounds(obj):
    return [obj.matrix_world @ Vector(p) for p in obj.bound_box]

def door_points(side):
    return [p for obj in meshes if "DOOR_" + side + "_" in obj.name
            for p in world_bounds(obj)]

min_roof_gap = 1e9
min_leaf_gap = 1e9
for frame in range(1, 146):
    bpy.context.scene.frame_set(frame)
    bpy.context.view_layer.update()
    left = door_points("L")
    right = door_points("R")
    assert left and right
    roof_gap = min(p.z for p in left + right) - .710
    leaf_gap = min(p.x for p in right) - max(p.x for p in left)
    min_roof_gap = min(min_roof_gap, roof_gap)
    min_leaf_gap = min(min_leaf_gap, leaf_gap)
    assert roof_gap > .012, ("door touches frame", frame, roof_gap)
    assert leaf_gap > .008, ("door leaves intersect", frame, leaf_gap)

bpy.context.scene.frame_set(1)
bpy.context.view_layer.update()
print("M03-019 BLENDER QA PASS", {
    "objects": second[0], "watertight_meshes": second[1],
    "materials": second[2], "SNAP": len(snap),
    "animated_doors": len(hinges), "animated_latches": len(latches),
    "sampled_frames": 145,
    "minimum_roof_gap_m": round(min_roof_gap, 5),
    "minimum_leaf_separation_m": round(min_leaf_gap, 5),
    "foreign_sentinel_preserved": True})
