#!/usr/bin/env python3
"""Independent Blender-native geometry and exported GLB QA for ENV03-001."""
import argparse
import bmesh
import hashlib
import json
import math
import os
import struct
import sys
import bpy

def options():
    args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    parser = argparse.ArgumentParser()
    parser.add_argument("--output-dir", required=True)
    return parser.parse_args(args)

def sha256(path):
    h = hashlib.sha256()
    with open(path, "rb") as f:
        for chunk in iter(lambda: f.read(1024 * 1024), b""):
            h.update(chunk)
    return h.hexdigest()

def assert_close(actual, expected, label, tolerance=0.00001):
    assert abs(actual - expected) < tolerance, "%s: %s != %s" % (label, actual, expected)

def bounds(meshes):
    points = [obj.matrix_world @ v.co for obj in meshes for v in obj.data.vertices]
    return {
        "min": [min(v[i] for v in points) for i in range(3)],
        "max": [max(v[i] for v in points) for i in range(3)],
    }

def check_blend(path):
    assert os.path.isfile(path) and os.path.getsize(path) > 1000, "BLEND missing"
    bpy.ops.wm.open_mainfile(filepath=path)
    root = bpy.data.objects.get("ENV03-001_ROOT")
    assert root is not None and root.type == "EMPTY", "Missing root pivot"
    assert root.location.length < 0.000001, "Root pivot must be at ground center"
    meshes = [obj for obj in bpy.data.objects if obj.type == "MESH"]
    assert len(meshes) == 18, "Expected 18 separate closed mesh objects, got %d" % len(meshes)
    assert all(obj.parent == root for obj in meshes), "Unparented geometry"
    assert not any(obj.type == "ARMATURE" for obj in bpy.data.objects), "Unexpected rig"
    assert not any(obj.animation_data and obj.animation_data.action for obj in bpy.data.objects), "Unexpected animation"
    names = {m.name for m in bpy.data.materials}
    expected = {"ENV03_Concrete", "ENV03_DarkSteel", "ENV03_SafetyOchre"}
    assert names == expected, "Material set differs: %s" % names
    assert all(len(obj.data.materials) == 1 for obj in meshes), "Each mesh must use one material"
    assert_close(bpy.context.scene.unit_settings.scale_length, 1.0, "Metric scale")
    assert bpy.context.scene.unit_settings.system == "METRIC", "Units not metric"

    triangle_count = 0
    per_mesh = {}
    for obj in meshes:
        bm = bmesh.new()
        bm.from_mesh(obj.data)
        assert len(bm.verts) > 0 and len(bm.faces) > 0, obj.name + " empty"
        nonmanifold = [edge for edge in bm.edges if len(edge.link_faces) != 2]
        assert not nonmanifold, "%s: %d nonmanifold edges" % (obj.name, len(nonmanifold))
        assert all(face.calc_area() > 1e-10 for face in bm.faces), obj.name + " degenerate face"
        volume = bm.calc_volume(signed=True)
        assert volume > 1e-9, "%s: inverted normals or zero volume %f" % (obj.name, volume)
        tris = sum(len(face.verts) - 2 for face in bm.faces)
        triangle_count += tris
        per_mesh[obj.name] = {"tris": tris, "signed_volume_m3": round(volume, 9)}
        bm.free()
    assert 200 <= triangle_count <= 1500, "Triangle budget violated: %d" % triangle_count

    b = bounds(meshes)
    for i, (low, high) in enumerate(((-1.0, 1.0), (-0.24, 0.24), (0.0, 0.88))):
        assert_close(b["min"][i], low, "bounds.min[%d]" % i)
        assert_close(b["max"][i], high, "bounds.max[%d]" % i)

    # Only the two continuous extrusions may touch X=+/-1.0.
    end_objects = {"Concrete_Core", "Continuous_Steel_Coping"}
    for obj in meshes:
        if obj.name not in end_objects:
            assert all(abs(v.co.x) < 0.9 for v in obj.data.vertices), obj.name + " intrudes into mating end"
    for name in sorted(end_objects):
        obj = bpy.data.objects[name]
        left = sorted((round(v.co.y, 6), round(v.co.z, 6)) for v in obj.data.vertices if abs(v.co.x + 1.0) < 1e-6)
        right = sorted((round(v.co.y, 6), round(v.co.z, 6)) for v in obj.data.vertices if abs(v.co.x - 1.0) < 1e-6)
        assert left and left == right, name + " has nonmatching end profiles"

    assert len([m for m in meshes if m.name.startswith("TiePlate_")]) == 4
    assert len([m for m in meshes if m.name.startswith("Reflector_")]) == 4
    assert len([m for m in meshes if m.name.startswith("Bolt_")]) == 8

    # Verify analytic surface contact without penetration at front/back:
    # the body is flat at +/-0.20 between Z=0.19 and 0.70.
    for obj in meshes:
        if obj.name.startswith("TiePlate_"):
            side = -1 if "_Arena_" in obj.name else 1
            assert_close(obj.location.y, side * 0.207, obj.name + " Y")
            assert_close(obj.dimensions.y, 0.014, obj.name + " depth")
        if obj.name.startswith("Reflector_"):
            side = -1 if "_Arena_" in obj.name else 1
            assert_close(obj.location.y, side * 0.2035, obj.name + " Y")
            assert_close(obj.dimensions.y, 0.007, obj.name + " depth")

    return {
        "mesh_count": len(meshes),
        "triangles": triangle_count,
        "materials": sorted(names),
        "bounds_m": b,
        "pivot_m": [0, 0, 0],
        "mating_ends": "MATCH",
        "watertight_positive_volume": "PASS",
        "collision_proposal": {"type": "BOX", "center_m": [0, 0, 0.44], "size_m": [2.0, 0.48, 0.88]},
        "mesh_details": per_mesh,
    }

def check_glb(path, expected_triangles):
    assert os.path.isfile(path) and os.path.getsize(path) > 1024, "GLB missing"
    with open(path, "rb") as f:
        magic, version, declared_length = struct.unpack("<4sII", f.read(12))
    assert magic == b"glTF" and version == 2, "Invalid GLB header"
    assert declared_length == os.path.getsize(path), "Truncated GLB"
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=path)
    meshes = [obj for obj in bpy.data.objects if obj.type == "MESH"]
    assert len(meshes) == 18, "GLB mesh count differs: %d" % len(meshes)
    tris = sum(sum(len(poly.vertices) - 2 for poly in obj.data.polygons) for obj in meshes)
    assert tris == expected_triangles, "GLB triangle count differs: %d" % tris
    mats = {mat.name for obj in meshes for mat in obj.data.materials}
    assert len(mats) == 3, "GLB material count differs: %s" % mats
    return {"mesh_count": len(meshes), "triangles": tris, "materials": sorted(mats), "sha256": sha256(path)}

def main():
    out = os.path.abspath(options().output_dir)
    blend = os.path.join(out, "ENV03-001_ArenaWall.blend")
    glb = os.path.join(out, "ENV03-001_ArenaWall.glb")
    blend_result = check_blend(blend)
    glb_result = check_glb(glb, blend_result["triangles"])
    previews = {}
    for label in ("arena_front", "rear_end"):
        p = os.path.join(out, "ENV03-001_preview_%s.png" % label)
        if os.path.isfile(p) and os.path.getsize(p) > 1024:
            with open(p, "rb") as f:
                assert f.read(8) == b"\\x89PNG\\r\\n\\x1a\\n", "PNG signature invalid"
            previews[label] = {"status": "PASS", "bytes": os.path.getsize(p), "sha256": sha256(p)}
        else:
            previews[label] = {"status": "NOT_RUN"}
    result = {
        "id": "ENV03-001", "status": "BLENDER_QA_PASS",
        "blender": bpy.app.version_string, "blend": blend_result, "glb": glb_result,
        "blend_sha256": sha256(blend), "previews": previews,
        "unity": "NOT_RUN", "windows": "NOT_RUN",
        "manual_visual_review": "PENDING",
    }
    with open(os.path.join(out, "qa_report.json"), "w", encoding="utf-8") as f:
        json.dump(result, f, indent=2, ensure_ascii=False)
    print("ENV03_QA_PASS " + json.dumps(result, ensure_ascii=False))

if __name__ == "__main__":
    main()
