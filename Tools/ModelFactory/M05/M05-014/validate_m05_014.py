#!/usr/bin/env python3
"""Independent static-source and exported-file checks for M05-014."""
import argparse
import ast
import json
import struct
from pathlib import Path

HERE = Path(__file__).resolve().parent
STEM = "M05-014_dual_arm_lift"

def static():
    path = HERE / "generate_m05_014.py"
    source = path.read_text(encoding="utf-8")
    tree = ast.parse(source, filename=str(path))
    compile(tree, str(path), "exec")
    defs = {node.name for node in tree.body if isinstance(node, ast.FunctionDef)}
    required = {"clear_owned", "make_materials", "box", "cyl", "torus",
                "build", "inspect", "run"}
    assert required <= defs, required - defs
    assert 'M05-014' in source and 'MOUNT_REAR_SOCKET' in source
    assert 'bpy.ops.export_scene.gltf' in source
    assert 'bpy.ops.export_scene.fbx' in source
    print("PASS: source parses; generator, geometry, export and QA entrypoints present")
    print("NOT RUN: Blender geometry/topology and Unity import (static mode only)")

def exports(root):
    qa = json.loads((root / (STEM + "_qa.json")).read_text(encoding="utf-8"))
    assert qa["id"] == "M05-014"
    assert qa["blender"] == "PASS" and qa["blender_export"] == "PASS"
    assert qa["blender_repeat_build"] == "PASS"
    assert qa["blender_foreign_object_preserved"] == "PASS"
    assert qa["topology"].startswith("PASS")
    assert qa["mesh_count"] >= 40, qa["mesh_count"]
    assert len(qa["materials"]) >= 8
    assert qa["minimum_conservative_inner_gap_m"] > .30
    assert qa["swept_envelope_samples"] == 121
    assert qa["unity_import"] == "NOT RUN"
    for ext in ("fbx", "blend"):
        p = root / (STEM + "." + ext)
        assert p.exists() and p.stat().st_size > 1000, p
    glb = (root / (STEM + ".glb")).read_bytes()
    assert len(glb) > 1000
    magic, version, total = struct.unpack_from("<III", glb, 0)
    assert (magic, version, total) == (0x46546c67, 2, len(glb))
    length, chunk_type = struct.unpack_from("<II", glb, 12)
    assert chunk_type == 0x4e4f534a
    scene = json.loads(glb[20:20 + length])
    assert len(scene.get("materials", [])) >= 8
    assert len(scene.get("meshes", [])) >= 40
    animations = scene.get("animations", [])
    assert len(animations) >= 1, "missing yaw animation"
    assert sum(len(a.get("channels", [])) for a in animations) >= 2
    names = {n.get("name") for n in scene.get("nodes", [])}
    assert {"MOUNT_REAR_SOCKET", "M05_014_ARM_L_PIVOT",
            "M05_014_ARM_R_PIVOT"}.issubset(names), names
    print("PASS: actual Blender exports: GLB/FBX/BLEND, 8 materials, 2 yaw pivots")
    print("PASS: manifold meshes, deterministic repeat build and swept-envelope report")
    print("NOT RUN: Unity import/EditMode/PlayMode and visual review")

if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--exports", type=Path)
    args = parser.parse_args()
    if args.exports:
        exports(args.exports)
    else:
        static()
