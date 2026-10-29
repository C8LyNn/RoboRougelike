#!/usr/bin/env python3
"""Independent Blender + GLB + preview QA for static ENV01-001.
Run with Blender, NOT plain Python. SPDX-License-Identifier: CC0-1.0.
"""
import bpy
import bmesh
import hashlib
import json
import math
import struct
import sys
from pathlib import Path
from mathutils import Vector

BASE = Path(__file__).resolve().parent
OUT = BASE / "generated" / "v002"
if "--" in sys.argv:
    args = sys.argv[sys.argv.index("--") + 1:]
    if "--out" in args:
        OUT = Path(args[args.index("--out") + 1]).resolve()
stem = "ENV01-001_protective_barrier"
blend = OUT / (stem + ".blend")
glb = OUT / (stem + ".glb")
assert blend.is_file() and blend.stat().st_size > 2000, "Missing actual .blend"
assert glb.is_file() and glb.stat().st_size > 2000, "Missing actual .glb"

bpy.ops.wm.open_mainfile(filepath=str(blend))
root = bpy.data.objects.get("ENV01_001_ROOT")
assert root is not None and root.type == "EMPTY", "Root pivot missing"
assert tuple(round(v, 7) for v in root.location) == (0, 0, 0)
assert bpy.context.scene.unit_settings.scale_length == 1.0
assert not bpy.data.actions, "Static pilot must not contain animations"
meshes = [o for o in bpy.data.objects if o.type == "MESH"]
assert 10 <= len(meshes) <= 45, "Unexpected mesh-object count"
materials = {slot.material.name for obj in meshes for slot in obj.material_slots}
assert materials == {"01_CONCRETE_COOL_GRAY", "02_STEEL_GRAPHITE", "03_SAFETY_AMBER"}, materials

triangles = 0
nonmanifold_edges = 0
degenerate_faces = 0
negative_volumes = []
world_min = [float("inf")] * 3
world_max = [float("-inf")] * 3
for obj in meshes:
    assert obj.parent == root, "Unparented mesh: " + obj.name
    assert not obj.modifiers, "Unapplied modifier: " + obj.name
    assert all(abs(v - 1.0) < 1e-6 for v in obj.scale), "Unapplied scale: " + obj.name
    assert obj.animation_data is None, "Unexpected animation: " + obj.name
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bad_edges = sum(len(e.link_faces) != 2 for e in bm.edges)
    bad_faces = sum(f.calc_area() < 1e-10 for f in bm.faces)
    volume = bm.calc_volume(signed=True)
    nonmanifold_edges += bad_edges
    degenerate_faces += bad_faces
    if volume <= 1e-9:
        negative_volumes.append((obj.name, volume))
    triangles += sum(max(0, len(f.verts) - 2) for f in bm.faces)
    bm.free()
    for vertex in obj.data.vertices:
        world = obj.matrix_world @ vertex.co
        for i in range(3):
            world_min[i] = min(world_min[i], world[i])
            world_max[i] = max(world_max[i], world[i])
assert nonmanifold_edges == 0, "Open or nonmanifold edges: %d" % nonmanifold_edges
assert degenerate_faces == 0, "Degenerate faces: %d" % degenerate_faces
assert not negative_volumes, "Wrong winding/volume: %r" % negative_volumes
assert 200 <= triangles <= 1500, "Triangle budget exceeded: %d" % triangles
assert 2.2 <= world_max[0] - world_min[0] <= 2.55
assert abs(world_min[2]) <= 0.002, "Barrier must touch ground Z=0"
assert world_max[2] <= 1.0
assert world_min[1] >= -0.60 and world_max[1] <= 0.42

# Check rear structural steel against the concrete body. AABB disjointness
# is deliberately conservative; touching faces are permitted but positive
# volumetric overlap is forbidden for these deliberately offset frame parts.
def bounds(obj):
    points = [obj.matrix_world @ Vector(corner) for corner in obj.bound_box]
    return [(min(p[i] for p in points), max(p[i] for p in points)) for i in range(3)]
core = bpy.data.objects["Concrete_low_flared_core"]
core_box = bounds(core)
frame_prefixes = ("Foot_plate_", "Rear_post_", "Rear_brace_",
                  "Rear_top_steel_tie", "Concrete_end_steel_strap_")
frame_checks = 0
for obj in meshes:
    if obj.name.startswith(frame_prefixes):
        box = bounds(obj)
        penetration = [min(a[1], b[1]) - max(a[0], b[0])
                       for a, b in zip(core_box, box)]
        assert min(penetration) <= 0.0005, (
            "Concrete / structural steel AABB intersection: %s %s" %
            (obj.name, penetration))
        frame_checks += 1
assert frame_checks >= 9, "Rear steel overlap checks incomplete"

# Front rails must sit outside the concrete profile with at least 5mm gap.
# Evaluate the maximum forward concrete Y at each rail's lowest Z.
def concrete_front_y(z):
    if z <= 0.16:
        return 0.30
    if z <= 0.29:
        return 0.30 + (z - 0.16) * (0.255 - 0.30) / (0.29 - 0.16)
    return 0.255 + (z - 0.29) * (0.16 - 0.255) / (0.68 - 0.29)
front_rail_clearances = {}
for name in ("Front_impact_rail", "Front_toe_rail"):
    box = bounds(bpy.data.objects[name])
    gap = box[1][0] - concrete_front_y(box[2][0])
    assert gap >= 0.005, "Front rail embeds into concrete: %s %.6f" % (name, gap)
    front_rail_clearances[name] = round(gap, 6)

# The proposed single collider must fully enclose rear feet and all meshes.
collider_center = (0.0, -0.09, 0.42)
collider_size = (2.62, 1.00, 0.90)
collider_min = [c - s / 2 for c, s in zip(collider_center, collider_size)]
collider_max = [c + s / 2 for c, s in zip(collider_center, collider_size)]
for axis in range(3):
    assert collider_min[axis] <= world_min[axis] - 0.015, "Collider min undercovers"
    assert collider_max[axis] >= world_max[axis] + 0.015, "Collider max undercovers"

# Independently parse actual GLB v2 binary, not just its extension.
raw = glb.read_bytes()
magic, version, length = struct.unpack_from("<4sII", raw, 0)
assert magic == b"glTF" and version == 2 and length == len(raw)
chunk_len, chunk_type = struct.unpack_from("<II", raw, 12)
assert chunk_type == 0x4E4F534A, "Missing GLB JSON chunk"
doc = json.loads(raw[20:20 + chunk_len].decode("utf-8").rstrip(" \t\r\n\x00"))
assert doc.get("asset", {}).get("version") == "2.0"
assert len(doc.get("materials", [])) == 3, "GLB materials not 3"
assert len(doc.get("meshes", [])) >= 10, "GLB meshes missing"
assert not doc.get("animations"), "Static GLB has animation"
assert any(node.get("name") == "ENV01_001_ROOT" for node in doc.get("nodes", []))
assert len(doc.get("buffers", [])) >= 1, "GLB buffer missing"

preview_dims = {}
for name in ("preview_front.png", "preview_rear.png"):
    p = OUT / name
    assert p.is_file() and p.stat().st_size > 1000, "Missing actual render: " + name
    png = p.read_bytes()
    assert png[:8] == b"\x89PNG\r\n\x1a\n", "Invalid PNG: " + name
    width, height = struct.unpack(">II", png[16:24])
    assert width >= 640 and height >= 400
    preview_dims[name] = [width, height]

paths = [
    BASE / "generate_env01_001.py",
    BASE / "qa_env01_001.py",
    blend, glb,
    OUT / "preview_front.png",
    OUT / "preview_rear.png",
    OUT / "build_report.json",
]
sha256 = {str(p.relative_to(BASE)): hashlib.sha256(p.read_bytes()).hexdigest() for p in paths}
report = {
    "id": "ENV01-001", "revision": "v002",
    "v001_head": "265835419a7144799cc92fb4a7dac637edbb7cc3",
    "status": "BLENDER_QA_PASS",
    "blender_version": bpy.app.version_string,
    "triangles": triangles,
    "mesh_objects": len(meshes),
    "materials": sorted(materials),
    "nonmanifold_edges": nonmanifold_edges,
    "degenerate_faces": degenerate_faces,
    "negative_volume_meshes": negative_volumes,
    "rear_frame_concrete_nonintersection_aabb_checks": frame_checks,
    "front_rail_clearances_m": front_rail_clearances,
    "ground_contact_z_m": round(world_min[2], 6),
    "collider_center_m": list(collider_center),
    "collider_size_m": list(collider_size),
    "blender_aabb_min_m": [round(v, 5) for v in world_min],
    "blender_aabb_max_m": [round(v, 5) for v in world_max],
    "preview_dimensions": preview_dims,
    "glb_valid": True,
    "license": "CC0-1.0; original geometry",
    "unity_editmode": "NOT_RUN",
    "unity_playmode": "NOT_RUN",
    "windows": "NOT_RUN",
    "manual_art_review": "PENDING_HQ",
    "sha256": sha256,
}
(OUT / "qa_report.json").write_text(json.dumps(report, indent=2) + "\n")
print("ENV01_001_QA_PASS", json.dumps(report, sort_keys=True))
