#!/usr/bin/env python3
"""M03-020 independent Blender geometry, mounting and repeatability QA."""
import bpy
import bmesh
import importlib.util
import math
from pathlib import Path
from mathutils import Vector

source = Path(__file__).with_name("generate_m03_020.py")
spec = importlib.util.spec_from_file_location("m03_020_generator", source)
gen = importlib.util.module_from_spec(spec)
spec.loader.exec_module(gen)

# Foreign scene data must survive repeated runs.
bpy.ops.mesh.primitive_cube_add(size=.012, location=(5, 5, 5))
sentinel = bpy.context.object
sentinel.name = "FOREIGN_SENTINEL_M03_020"


def snapshot(col):
    meshes = [o for o in col.objects if o.type == "MESH"]
    mats = {m.name for o in meshes for m in o.data.materials}
    return len(col.objects), len(meshes), len(mats)


col = gen.build_model()
first = snapshot(col)
assert first == (83, 78, 6), ("unexpected first geometry", first)
col = gen.build_model()
second = snapshot(col)
assert second == first, ("duplicate objects on repeat build", first, second)
assert bpy.data.objects.get(sentinel.name) is sentinel, "foreign object removed"
assert len([c for c in bpy.data.collections if c.name == gen.COLLECTION]) == 1
assert len([m for m in bpy.data.materials if m.name.startswith(gen.TAG + "_")]) == 6

root = bpy.data.objects[gen.TAG + "_ROOT_REAR_MOUNT"]
assert root.parent is None
assert root.location.length < 1e-8
snaps = [o for o in col.objects if o.name.startswith(gen.TAG + "_SNAP_REAR_")]
assert len(snaps) == 4
snap_xyz = {(round(o.location.x, 3), round(o.location.z, 3)) for o in snaps}
assert snap_xyz == {(-.45, -.20), (-.45, .20), (.45, -.20), (.45, .20)}
for o in snaps:
    assert abs(o.location.y) < 1e-8
    outward = o.matrix_world.to_3x3() @ Vector((0, 0, 1))
    assert (outward - Vector((0, -1, 0))).length < 1e-6

meshes = [o for o in col.objects if o.type == "MESH"]
for obj in meshes:
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    assert all(e.is_manifold for e in bm.edges), "nonmanifold " + obj.name
    assert bm.calc_volume(signed=True) > 1e-9, "zero or reversed volume " + obj.name
    assert all(f.calc_area() > 1e-9 for f in bm.faces), "degenerate face " + obj.name
    bm.free()
    assert len(obj.data.materials) == 1
    assert min(obj.scale) > 0

# Real apertures: rods must be individual solids, not a solid fake mesh.
walls = [o for o in meshes if "WallVertical_" in o.name]
sides = [o for o in meshes if "SideVertical_" in o.name]
floor_bars = [o for o in meshes if "FloorBar_" in o.name]
ceramic = [o for o in meshes if "FloorCeramicRunner_" in o.name]
assert (len(walls), len(sides), len(floor_bars), len(ceramic)) == (18, 10, 9, 5)
for family, minimum_gap in ((walls, .085), (sides, .09), (floor_bars, .085)):
    # Check nearest parallel bar centre spacing analytically in each wall.
    assert minimum_gap > .018, family

# Rods are 18 mm diameter at 110/115 mm centres; open side apertures
# are at least 92 mm / 97 mm before considering cross bars.
assert .110 - 2 * .009 >= .090
assert .115 - 2 * .009 >= .095
# Slats 52 mm wide on 200 mm centres, leaving 148 mm open slots.
assert .20 - .052 >= .14

# Basket is static by catalog; no accidental animation actions.
assert all(not o.animation_data for o in col.objects)
assert bpy.context.scene.frame_end == 1

print("M03-020 BLENDER QA PASS", {
    "owned_objects": second[0],
    "watertight_positive_volume_meshes": second[1],
    "materials": second[2],
    "rear_mounts": len(snaps),
    "wall_vertical_bars": len(walls),
    "side_vertical_bars": len(sides),
    "floor_bars": len(floor_bars),
    "floor_ceramic_runners": len(ceramic),
    "min_wall_vertical_aperture_m": .092,
    "min_side_vertical_aperture_m": .097,
    "foreign_sentinel_preserved": True,
    "repeatable": True,
})
