#!/usr/bin/env python3
"""Independent Blender mesh, actual binary GLB and four PNG QA for ENV04-001 v002.
SPDX-License-Identifier: CC0-1.0.
"""
import bpy
import bmesh
from mathutils import Vector
import hashlib
import json
import math
import struct
import sys
from pathlib import Path

BASE = Path(__file__).resolve().parent
OUT = BASE / "generated" / "v002"
if "--" in sys.argv:
    argv = sys.argv[sys.argv.index("--") + 1:]
    if "--out" in argv:
        OUT = Path(argv[argv.index("--out") + 1]).resolve()
STEM = "ENV04-001_industrial_vent_grate"
blend = OUT / (STEM + ".blend")
glb = OUT / (STEM + ".glb")
assert blend.is_file() and blend.stat().st_size > 3000, "BLEND binary missing"
assert glb.is_file() and glb.stat().st_size > 3000, "GLB binary missing"

bpy.ops.wm.open_mainfile(filepath=str(blend))
scene = bpy.context.scene
root = bpy.data.objects.get("ENV04_001_ROOT")
assert root is not None and root.type == "EMPTY"
assert all(abs(v) < 1e-7 for v in root.location)
assert scene.unit_settings.system == "METRIC"
assert abs(scene.unit_settings.scale_length - 1.0) < 1e-7
assert not bpy.data.actions, "Unexpected animation actions"

objects = [o for o in bpy.data.objects if o.type == "MESH"]
assert len(objects) == 40, "Unexpected object count %d" % len(objects)
names = {o.name for o in objects}
assert len([n for n in names if n.startswith("Static_louver_")]) == 6
assert len([n for n in names if "_mount_" in n]) == 12
materials = {slot.material.name for obj in objects for slot in obj.material_slots}
expected = {"01_GRAPHITE_POWDERCOAT", "02_BRUSHED_STEEL", "03_SAFETY_AMBER"}
assert materials == expected, "Unexpected materials %r" % materials

scene.view_layers[0].update()
triangles = 0
nonmanifold = 0
degenerate = 0
bad_volumes = []
bad_normals = []
bad_uv = []
geometry_records = []
bounds_min = [float("inf")] * 3
bounds_max = [float("-inf")] * 3
for obj in objects:
    assert obj.parent == root, "Unparented mesh " + obj.name
    assert not obj.modifiers, "Unapplied modifier " + obj.name
    assert obj.animation_data is None, "Unexpected mesh animation " + obj.name
    assert all(abs(s - 1.0) < 1e-6 for s in obj.scale), "Unapplied scale"
    assert len(obj.data.uv_layers) == 1, "Missing metric UV layer " + obj.name
    assert obj.data.uv_layers.active.name == "UV_METRIC_1M"
    for loop in obj.data.uv_layers.active.data:
        if not all(math.isfinite(float(x)) for x in loop.uv):
            bad_uv.append(obj.name)
    for face in obj.data.polygons:
        if abs(face.normal.length - 1.0) > 1e-5:
            bad_normals.append(obj.name)
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    nonmanifold += sum(len(e.link_faces) != 2 for e in bm.edges)
    degenerate += sum(face.calc_area() < 1e-10 for face in bm.faces)
    vol = bm.calc_volume(signed=True)
    if vol <= 1e-9:
        bad_volumes.append([obj.name, vol])
    triangles += sum(len(face.verts) - 2 for face in bm.faces)
    bm.free()
    for v in obj.data.vertices:
        p = obj.matrix_world @ v.co
        for axis in range(3):
            bounds_min[axis] = min(bounds_min[axis], p[axis])
            bounds_max[axis] = max(bounds_max[axis], p[axis])
for obj in sorted(objects, key=lambda item: item.name):
    geometry_records.append({
        "name": obj.name,
        "material": [slot.material.name for slot in obj.material_slots],
        "vertices": [[round(float(c), 6) for c in (obj.matrix_world @ v.co)]
                     for v in obj.data.vertices],
        "polygons": [list(face.vertices) for face in obj.data.polygons],
        "uv": [[round(float(c), 6) for c in loop.uv]
               for loop in obj.data.uv_layers.active.data],
    })
geometry_sha256 = hashlib.sha256(json.dumps(
    geometry_records, sort_keys=True, separators=(",", ":")).encode("utf-8")).hexdigest()
build = json.loads((OUT / "build_report.json").read_text())
assert build["revision"] == "v002"
assert geometry_sha256 == build["geometry_sha256"], "BLEND differs from generated source"
assert build["source_sha256"] == hashlib.sha256(
    (BASE / "generate_env04_001_v002.py").read_bytes()).hexdigest(), "Source drift"
assert nonmanifold == 0, "Nonmanifold edges %d" % nonmanifold
assert degenerate == 0, "Degenerate polygons %d" % degenerate
assert not bad_normals, "Bad normals %r" % bad_normals
assert not bad_uv, "Invalid UVs %r" % bad_uv
assert not bad_volumes, "Nonpositive signed volume %r" % bad_volumes
assert triangles == 1344, "Expected 1344 triangles, got %d" % triangles
assert 1.5 <= bounds_max[0] - bounds_min[0] <= 1.8
assert 0.95 <= bounds_max[1] - bounds_min[1] <= 1.2
assert -0.005 <= bounds_min[2] <= 0.015
assert 0.45 <= bounds_max[2] <= 0.62

# Conservative AABB nonpenetration test for potentially conflicting visible parts.
# Zero-volume contact (butt joints) is permitted; this does not prove swept triangle CCD.
def aabb(obj):
    pts = [obj.matrix_world @ Vector(corner) for corner in obj.bound_box]
    return ([min(p[i] for p in pts) for i in range(3)],
            [max(p[i] for p in pts) for i in range(3)])

def overlap_volume(a, b):
    alo, ahi = aabb(a)
    blo, bhi = aabb(b)
    ext = [max(0.0, min(ahi[i], bhi[i]) - max(alo[i], blo[i]))
           for i in range(3)]
    return ext[0] * ext[1] * ext[2]

critical_pairs = []
for i in range(1, 7):
    slat = bpy.data.objects["Static_louver_%02d" % i]
    for j in range(i + 1, 7):
        critical_pairs.append((slat, bpy.data.objects["Static_louver_%02d" % j]))
    for side in ("L", "R"):
        bracket = bpy.data.objects["Louver_%02d_mount_%s" % (i, side)]
        critical_pairs.append((slat, bracket))
        critical_pairs.append((bracket, bpy.data.objects["Raised_grate_side_rail_" + side]))
        critical_pairs.append((slat, bpy.data.objects["Raised_grate_side_rail_" + side]))
for side in ("L", "R"):
    for end in ("rear", "front"):
        critical_pairs.append((bpy.data.objects["Raised_grate_side_rail_" + side],
                               bpy.data.objects["Raised_grate_end_rail_" + end]))
critical_overlaps = [(a.name, b.name, overlap_volume(a, b))
                     for a, b in critical_pairs if overlap_volume(a, b) > 1e-9]
assert not critical_overlaps, "Positive AABB penetration %r" % critical_overlaps


raw = glb.read_bytes()
magic, version, size = struct.unpack_from("<4sII", raw, 0)
assert magic == b"glTF" and version == 2 and size == len(raw), "Invalid GLB header"
json_size, chunk_type = struct.unpack_from("<II", raw, 12)
assert chunk_type == 0x4E4F534A, "No GLB JSON chunk"
gltf = json.loads(raw[20:20 + json_size].decode("utf-8").rstrip(" \t\r\n\x00"))
assert gltf.get("asset", {}).get("version") == "2.0"
assert len(gltf.get("materials", [])) == 3
assert len(gltf.get("meshes", [])) >= 20
assert not gltf.get("animations"), "Static asset contains GLB animation"
assert any(n.get("name") == "ENV04_001_ROOT" for n in gltf.get("nodes", []))
assert gltf.get("buffers"), "No GLB binary geometry buffer"

views = {}
for name in ("preview_front.png", "preview_rear.png", "preview_top.png", "preview_side.png"):
    path = OUT / name
    assert path.is_file() and path.stat().st_size > 1500, "Missing preview " + name
    png = path.read_bytes()
    assert png[:8] == b"\x89PNG\r\n\x1a\n"
    w, h = struct.unpack(">II", png[16:24])
    assert w == 960 and h == 640
    views[name] = [w, h]
assert hashlib.sha256((OUT / "preview_front.png").read_bytes()).digest() != \
       hashlib.sha256((OUT / "preview_rear.png").read_bytes()).digest(), "Duplicate preview views"

paths = [BASE / "generate_env04_001_v002.py", BASE / "qa_env04_001_v002.py",
         blend, glb, OUT / "preview_front.png", OUT / "preview_rear.png",
         OUT / "preview_top.png", OUT / "preview_side.png",
         OUT / "build_report.json"]
# A second isolated QA pass may use /tmp; keep canonical manifest keys.
hashes = {(str(p.relative_to(BASE)) if p.is_relative_to(BASE)
           else "generated/v002/" + p.name): hashlib.sha256(p.read_bytes()).hexdigest()
          for p in paths}
result = {
    "id": "ENV04-001", "revision": "v002", "status": "BLENDER_QA_PASS",
    "blender_version": bpy.app.version_string,
    "mesh_objects": len(objects), "triangles": triangles,
    "materials": sorted(materials),
    "nonmanifold_edges": nonmanifold, "degenerate_faces": degenerate,
    "negative_volume_meshes": bad_volumes,
    "invalid_normals": bad_normals, "invalid_uv": bad_uv,
    "critical_aabb_pairs_checked": len(critical_pairs),
    "critical_positive_overlap_pairs": critical_overlaps,
    "geometry_sha256": geometry_sha256,
    "blender_bounds_min_m": [round(v, 5) for v in bounds_min],
    "blender_bounds_max_m": [round(v, 5) for v in bounds_max],
    "glb_valid": True, "preview_dimensions": views,
    "sha256": hashes, "license": "CC0-1.0",
    "animation": "NOT_APPLICABLE",
    "unity_editmode": "NOT_RUN", "unity_playmode": "NOT_RUN",
    "windows": "NOT_RUN", "manual_art_review": "PENDING_HQ"
}
(OUT / "qa_report.json").write_text(json.dumps(result, indent=2) + "\n")
print("ENV04_001_V002_QA_PASS", json.dumps(result, sort_keys=True))
