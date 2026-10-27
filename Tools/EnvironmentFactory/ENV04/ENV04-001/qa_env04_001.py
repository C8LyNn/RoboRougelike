#!/usr/bin/env python3
"""Independent Blender mesh, actual binary GLB and two PNG QA for ENV04-001.
SPDX-License-Identifier: CC0-1.0.
"""
import bpy
import bmesh
import hashlib
import json
import struct
import sys
from pathlib import Path

BASE = Path(__file__).resolve().parent
OUT = BASE / "generated"
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
assert 20 <= len(objects) <= 45, "Unexpected object count %d" % len(objects)
names = {o.name for o in objects}
assert len([n for n in names if n.startswith("Static_louver_")]) == 6
materials = {slot.material.name for obj in objects for slot in obj.material_slots}
expected = {"01_GRAPHITE_POWDERCOAT", "02_BRUSHED_STEEL", "03_SAFETY_AMBER"}
assert materials == expected, "Unexpected materials %r" % materials

scene.view_layers[0].update()
triangles = 0
nonmanifold = 0
degenerate = 0
bad_volumes = []
bounds_min = [float("inf")] * 3
bounds_max = [float("-inf")] * 3
for obj in objects:
    assert obj.parent == root, "Unparented mesh " + obj.name
    assert not obj.modifiers, "Unapplied modifier " + obj.name
    assert obj.animation_data is None, "Unexpected mesh animation " + obj.name
    assert all(abs(s - 1.0) < 1e-6 for s in obj.scale), "Unapplied scale"
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
assert nonmanifold == 0, "Nonmanifold edges %d" % nonmanifold
assert degenerate == 0, "Degenerate polygons %d" % degenerate
assert not bad_volumes, "Nonpositive signed volume %r" % bad_volumes
assert 200 <= triangles <= 1500, "Triangle budget %d" % triangles
assert 1.5 <= bounds_max[0] - bounds_min[0] <= 1.8
assert 0.95 <= bounds_max[1] - bounds_min[1] <= 1.2
assert -0.005 <= bounds_min[2] <= 0.015
assert 0.45 <= bounds_max[2] <= 0.62

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
for name in ("preview_front.png", "preview_rear.png"):
    path = OUT / name
    assert path.is_file() and path.stat().st_size > 1500, "Missing preview " + name
    png = path.read_bytes()
    assert png[:8] == b"\x89PNG\r\n\x1a\n"
    w, h = struct.unpack(">II", png[16:24])
    assert w == 960 and h == 640
    views[name] = [w, h]
assert hashlib.sha256((OUT / "preview_front.png").read_bytes()).digest() != \
       hashlib.sha256((OUT / "preview_rear.png").read_bytes()).digest(), "Duplicate preview views"

paths = [BASE / "generate_env04_001.py", BASE / "qa_env04_001.py",
         blend, glb, OUT / "preview_front.png", OUT / "preview_rear.png",
         OUT / "build_report.json"]
hashes = {str(p.relative_to(BASE)): hashlib.sha256(p.read_bytes()).hexdigest()
          for p in paths}
result = {
    "id": "ENV04-001", "status": "BLENDER_QA_PASS",
    "blender_version": bpy.app.version_string,
    "mesh_objects": len(objects), "triangles": triangles,
    "materials": sorted(materials),
    "nonmanifold_edges": nonmanifold, "degenerate_faces": degenerate,
    "negative_volume_meshes": bad_volumes,
    "blender_bounds_min_m": [round(v, 5) for v in bounds_min],
    "blender_bounds_max_m": [round(v, 5) for v in bounds_max],
    "glb_valid": True, "preview_dimensions": views,
    "sha256": hashes, "license": "CC0-1.0",
    "animation": "NOT_APPLICABLE",
    "unity_editmode": "NOT_RUN", "unity_playmode": "NOT_RUN",
    "windows": "NOT_RUN", "manual_art_review": "PENDING_HQ"
}
(OUT / "qa_report.json").write_text(json.dumps(result, indent=2) + "\n")
print("ENV04_001_QA_PASS", json.dumps(result, sort_keys=True))
