"""Executed inside Blender, not a substitute for Unity/URP review."""
import bpy
import bmesh
import importlib.util
import math
from pathlib import Path

src = Path(__file__).with_name("generate_m05_020.py")
spec = importlib.util.spec_from_file_location("m05_020", str(src))
m = importlib.util.module_from_spec(spec)
spec.loader.exec_module(m)

bpy.ops.mesh.primitive_cube_add(size=0.1, location=(9, 9, 9))
sentinel = bpy.context.object
sentinel.name = "FOREIGN_SENTINEL_M05_020"
counts = []
for attempt in range(2):
    result = m.build()
    col = bpy.data.collections[m.COLLECTION]
    meshes = [o for o in col.objects if o.type == "MESH"]
    assert sentinel.name in bpy.data.objects, "Generator deleted unrelated object"
    assert len(meshes) >= 45, "Unexpected missing geometry"
    assert result["materials"] >= 3
    for obj in meshes:
        assert len(obj.data.polygons) > 0
        bm = bmesh.new()
        bm.from_mesh(obj.data)
        assert all(e.is_manifold for e in bm.edges), obj.name
        assert all(f.normal.length > 0.9 for f in bm.faces), obj.name
        bm.free()
    counts.append((len(col.objects), len(meshes),
                   len([mat for mat in bpy.data.materials if mat.name.startswith(m.TAG + "_")]),
                   len([mesh for mesh in bpy.data.meshes if mesh.name.startswith(m.TAG + "_")])))
assert counts[0] == counts[1], "Non-idempotent resource count: " + repr(counts)
assert m.YAW.animation_data and m.YAW.animation_data.action
for hinge in m.WINGS:
    assert hinge.animation_data and hinge.animation_data.action
# Verify physical hinge positions, fold angles and swept wing clearance from
# central spine. Geometry is checked in the yaw-pivot local frame.
minimum = 999
for frame in range(1, 101):
    bpy.context.scene.frame_set(frame)
    bpy.context.view_layer.update()
    yaw_inv = m.YAW.matrix_world.inverted()
    for sign, hinge in ((-1, m.WINGS[0]), (1, m.WINGS[1])):
        assert abs(hinge.rotation_euler.z) <= math.radians(65.1)
        for obj in hinge.children:
            if obj.type != "MESH":
                continue
            mat = yaw_inv @ obj.matrix_world
            for vert in obj.data.vertices:
                p = mat @ vert.co
                # Central array ends at +/-0.175m; 0.02m guard.
                margin = sign * p.x - 0.175
                minimum = min(minimum, margin)
                assert margin > 0.015, (frame, obj.name, margin)
assert minimum > 0.015
print("M05-020_BLENDER_QA_PASS", counts, "min_lateral_clearance_m", round(minimum, 4))
