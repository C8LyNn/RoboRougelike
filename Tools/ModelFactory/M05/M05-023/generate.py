#!/usr/bin/env python3
"""M05-023 — Spare Parts Container, visual-only Blender generator.

Run: blender --background --factory-startup --python generate.py
Coordinate convention: 1 Blender unit = 1 m, +Z up, +Y points rearward.
The main mounting pivot is (0, 0, 0), at the chassis-facing rear bracket.
No gameplay logic or Unity project settings are modified.
"""
import json
import math
import os
from pathlib import Path

import bpy
import bmesh
from mathutils import Vector

PREFIX = "M05_023_"
COLLECTION_NAME = "M05-023 | Spare Parts Container"
OUT = Path(os.environ.get("M05_OUTPUT_DIR", str(Path(__file__).resolve().parent / "out")))
ROOT = None
COL = None
M = {}

def purge_own_assets():
    global ROOT, COL, M
    old = bpy.data.collections.get(COLLECTION_NAME)
    if old:
        for obj in list(old.objects):
            bpy.data.objects.remove(obj, do_unlink=True)
        bpy.data.collections.remove(old)
    for mesh in list(bpy.data.meshes):
        if mesh.name.startswith(PREFIX) and mesh.users == 0:
            bpy.data.meshes.remove(mesh)
    for mat in list(bpy.data.materials):
        if mat.name.startswith(PREFIX) and mat.users == 0:
            bpy.data.materials.remove(mat)
    ROOT, COL, M = None, None, {}

def attach(obj):
    for c in list(obj.users_collection):
        c.objects.unlink(obj)
    COL.objects.link(obj)
    return obj

def parent_keep_world(obj, parent):
    world = obj.matrix_world.copy()
    obj.parent = parent
    obj.matrix_world = world

def empty(name, pos, parent=None, size=0.10):
    obj = bpy.data.objects.new(PREFIX + name, None)
    COL.objects.link(obj)
    obj.location = pos
    obj.empty_display_type = 'ARROWS'
    obj.empty_display_size = size
    if parent is not None:
        parent_keep_world(obj, parent)
    return obj

def material(key, rgba, metal=0.0, rough=0.45, emission=None, strength=0.0):
    mat = bpy.data.materials.new(PREFIX + key)
    mat.diffuse_color = rgba
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = rgba
    bsdf.inputs["Metallic"].default_value = metal
    bsdf.inputs["Roughness"].default_value = rough
    if emission is not None:
        e = bsdf.inputs.get("Emission Color") or bsdf.inputs.get("Emission")
        if e is not None:
            e.default_value = emission
        s = bsdf.inputs.get("Emission Strength")
        if s is not None:
            s.default_value = strength
    M[key] = mat

def finish(obj, name, mat, bevel=0.0):
    obj.name = PREFIX + name
    obj.data.name = PREFIX + name + "_mesh"
    attach(obj)
    obj.data.materials.append(M[mat])
    if ROOT is not None:
        parent_keep_world(obj, ROOT)
    if bevel > 0:
        mod = obj.modifiers.new("Machined edge radii", 'BEVEL')
        mod.width = bevel
        mod.segments = 3
        if hasattr(mod, 'affect'):
            mod.affect = 'EDGES'
        mod.use_clamp_overlap = True
        bpy.context.view_layer.objects.active = obj
        obj.select_set(True)
        bpy.ops.object.modifier_apply(modifier=mod.name)
        obj.select_set(False)
    return obj

def box(name, pos, dims, mat, bevel=0.008):
    bpy.ops.mesh.primitive_cube_add(size=1, location=pos)
    obj = bpy.context.object
    obj.dimensions = dims
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return finish(obj, name, mat, min(bevel, min(dims) * 0.24))

def cyl(name, pos, radius, depth, mat, verts=24, rot=(0, 0, 0), bevel=0.004):
    bpy.ops.mesh.primitive_cylinder_add(vertices=verts, radius=radius, depth=depth,
                                        location=pos, rotation=rot)
    obj = bpy.context.object
    result = finish(obj, name, mat, min(bevel, depth * 0.20, radius * 0.15))
    for face in result.data.polygons:
        if len(face.vertices) == 4:
            face.use_smooth = True
    return result

def rod(name, a, b, radius, mat, verts=12):
    va, vb = Vector(a), Vector(b)
    delta = vb - va
    return cyl(name, (va + vb) / 2, radius, delta.length, mat,
               verts=verts, rot=delta.to_track_quat('Z', 'Y').to_euler(), bevel=0.002)

def build():
    global ROOT, COL
    purge_own_assets()
    COL = bpy.data.collections.new(COLLECTION_NAME)
    bpy.context.scene.collection.children.link(COL)
    ROOT = empty("MOUNT_ROOT", (0, 0, 0), size=0.16)

    material("graphite", (0.065, 0.085, 0.11, 1), metal=0.65, rough=0.32)
    material("titanium", (0.40, 0.48, 0.52, 1), metal=0.82, rough=0.24)
    material("ceramic", (0.76, 0.79, 0.72, 1), metal=0.08, rough=0.43)
    material("safety_orange", (0.95, 0.30, 0.045, 1), metal=0.32, rough=0.36)
    material("rubber", (0.025, 0.033, 0.039, 1), metal=0.0, rough=0.84)
    material("copper", (0.56, 0.23, 0.10, 1), metal=0.86, rough=0.27)
    material("led_green", (0.04, 0.8, 0.30, 1), rough=0.20,
             emission=(0.01, 0.95, 0.30, 1), strength=3.5)
    material("led_red", (0.85, 0.04, 0.025, 1), rough=0.20,
             emission=(1.0, 0.02, 0.01, 1), strength=2.0)

    # Chassis-facing adapter: all dimensions are metres.
    box("mount_backplate", (0, 0.17, 0.60), (1.48, 0.095, 0.85), "graphite", 0.035)
    for side, sx in (("L", -1), ("R", 1)):
        for level, z in (("LOW", 0.31), ("HIGH", 0.85)):
            x = sx * 0.56
            box("rear_spacer_" + side + "_" + level,
                (x, 0.075, z), (0.18, 0.14, 0.12), "titanium")
            cyl("rear_boss_" + side + "_" + level,
                (x, 0.008, z), 0.060, 0.024, "copper",
                rot=(math.pi / 2, 0, 0))
            empty("MOUNT_" + side + "_" + level, (x, 0.0, z), ROOT, 0.07)

    # Structural shell, open toward +Y, with visible interior.
    box("floor", (0, 0.59, 0.21), (1.48, 0.75, 0.075), "graphite", 0.017)
    box("roof", (0, 0.59, 0.99), (1.48, 0.75, 0.075), "graphite", 0.017)
    for side, x in (("L", -0.765), ("R", 0.765)):
        box("side_wall_" + side, (x, 0.59, 0.60),
            (0.065, 0.76, 0.77), "ceramic", 0.012)
        box("side_reinforcement_" + side, (x * 1.045, 0.57, 0.61),
            (0.035, 0.63, 0.10), "titanium")
        for j, z in enumerate((0.34, 0.52, 0.70, 0.88)):
            box("side_vent_" + side + "_" + str(j),
                (x * 1.05, 0.66, z), (0.020, 0.26, 0.027), "graphite", 0.004)
        rod("carry_handle_upper_" + side,
            (x * 1.10, 0.44, 0.89), (x * 1.10, 0.73, 0.89),
            0.022, "copper")
    for z in (0.28, 0.91):
        box("front_rail_" + str(z), (0, 0.923, z),
            (1.47, 0.055, 0.075), "titanium")
    for x in (-0.71, 0.71):
        box("front_stile_" + str(x), (x, 0.922, 0.60),
            (0.045, 0.055, 0.65), "titanium")
    for z in (0.375, 0.65):
        box("shelf_" + str(z), (0, 0.60, z), (1.39, 0.65, 0.030),
            "titanium", 0.005)
        box("shelf_lip_" + str(z), (0, 0.917, z + 0.018),
            (1.36, 0.022, 0.045), "safety_orange", 0.004)

    # Distinct internal spare-parts trays, clips and small replacement cartridges.
    for row, z in enumerate((0.453, 0.728)):
        for col, x in enumerate((-0.43, 0, 0.43)):
            color = "ceramic" if (row + col) % 2 == 0 else "graphite"
            box("parts_tray_%d_%d" % (row, col), (x, 0.59, z),
                (0.34, 0.40, 0.115), color, 0.012)
            box("parts_label_%d_%d" % (row, col), (x, 0.805, z + 0.009),
                (0.23, 0.012, 0.042), "safety_orange", 0.003)
            for k in (-1, 1):
                cyl("tray_clip_%d_%d_%d" % (row, col, k),
                    (x + k * 0.125, 0.819, z + 0.027),
                    0.012, 0.018, "copper", 12, (math.pi / 2, 0, 0), 0.001)

    # Independent Z-axis door pivots. Left leaf rotates +105 deg,
    # right leaf -105 deg: both open away from the chassis.
    doors = []
    for side, sx in (("L", -1), ("R", 1)):
        pivot = empty("HINGE_PIVOT_" + side, (sx * 0.75, 1.015, 0.60), ROOT, 0.12)
        doors.append((pivot, sx))
        xcenter = sx * 0.38
        elements = [
            box("door_shell_" + side, (xcenter, 1.015, 0.60),
                (0.68, 0.055, 0.735), "graphite", 0.017),
            box("door_ceramic_" + side, (xcenter, 1.053, 0.61),
                (0.59, 0.020, 0.63), "ceramic", 0.013),
            box("door_rib_top_" + side, (xcenter, 1.071, 0.88),
                (0.55, 0.015, 0.040), "titanium", 0.005),
            box("door_rib_bottom_" + side, (xcenter, 1.071, 0.34),
                (0.55, 0.015, 0.040), "titanium", 0.005),
            box("door_latch_" + side, (sx * 0.105, 1.087, 0.59),
                (0.060, 0.052, 0.22), "safety_orange", 0.008),
            box("door_led_" + side, (sx * 0.20, 1.082, 0.79),
                (0.065, 0.013, 0.027),
                "led_green" if sx == -1 else "led_red", 0.003),
            box("door_grip_" + side, (sx * 0.21, 1.122, 0.59),
                (0.052, 0.048, 0.19), "rubber", 0.009),
        ]
        for j in range(4):
            elements.append(box("door_warning_bar_%s_%d" % (side, j),
                (xcenter + (j - 1.5) * 0.13, 1.083, 0.43),
                (0.082, 0.010, 0.025),
                "safety_orange" if j % 2 == 0 else "graphite", 0.002))
        for z in (0.35, 0.84):
            elements.append(cyl("hinge_barrel_%s_%.2f" % (side, z),
                (sx * 0.75, 1.015, z), 0.030, 0.085, "copper", 16))
        for obj in elements:
            parent_keep_world(obj, pivot)

    # Locking tabs, anti-slip feet and mounting guidance marker.
    for x in (-0.58, 0.58):
        box("lower_foot_" + str(x), (x, 0.45, 0.145),
            (0.25, 0.32, 0.06), "rubber", 0.014)
    empty("CARGO_ACCESS_FORWARD", (0, 1.18, 0.59), ROOT, 0.11)
    for pivot, sx in doors:
        for frame, angle in ((1, 0), (30, -sx * math.radians(105)),
                             (60, -sx * math.radians(105)), (90, 0)):
            pivot.rotation_euler[2] = angle
            pivot.keyframe_insert(data_path="rotation_euler", index=2, frame=frame)
        if pivot.animation_data and pivot.animation_data.action:
            pivot.animation_data.action.name = PREFIX + "Door_" + ("L" if sx < 0 else "R")
            for fc in pivot.animation_data.action.fcurves:
                for key in fc.keyframe_points:
                    key.interpolation = 'LINEAR'
        pivot.rotation_euler[2] = 0

    scene = bpy.context.scene
    scene.frame_start, scene.frame_end = 1, 90
    scene.render.fps = 30
    scene.frame_set(1)
    return doors

def validate(doors):
    meshes = [obj for obj in COL.objects if obj.type == 'MESH']
    assert len(meshes) >= 55, "Incomplete container geometry"
    assert len(M) == 8, "Missing materials"
    nonmanifold = []
    for obj in meshes:
        bm = bmesh.new()
        bm.from_mesh(obj.data)
        if any(not e.is_manifold for e in bm.edges):
            nonmanifold.append(obj.name)
        bm.free()
    assert not nonmanifold, "Non-manifold geometry: " + ", ".join(nonmanifold)
    # Analytic bounding check of the actual door leaf thickness and swing.
    # Decorative hinge-barrel overlap with its leaf is intentional.
    min_frame_clearance = float('inf')
    min_leaf_gap = float('inf')
    for i in range(121):
        angle = math.radians(105 * i / 120)
        # The inner/outer face corners of each 0.68 m leaf.
        left = [(-0.75 + dx * math.cos(angle) - dy * math.sin(angle),
                 1.015 + dx * math.sin(angle) + dy * math.cos(angle))
                for dx in (0.03, 0.71) for dy in (-0.0275, 0.0275)]
        right = [(0.75 - dx * math.cos(angle) + dy * math.sin(angle),
                  1.015 + dx * math.sin(angle) + dy * math.cos(angle))
                 for dx in (0.03, 0.71) for dy in (-0.0275, 0.0275)]
        min_frame_clearance = min(min_frame_clearance,
                                  min(y for _, y in left + right) - 0.9505)
        min_leaf_gap = min(min_leaf_gap,
                           min(x for x, _ in right) - max(x for x, _ in left))
    assert min_frame_clearance >= 0.02, "Door collides with front rail"
    assert min_leaf_gap >= 0.04, "Door leaves collide"
    assert ROOT.location.length < 1e-7, "Mount pivot must be at origin"
    assert all(abs(p.location.z - 0.60) < 1e-6 for p, _ in doors)
    return {
        "id": "M05-023",
        "status": "BLENDER_QA_PASS",
        "mesh_count": len(meshes),
        "material_count": len(M),
        "object_count": len(COL.objects),
        "nonmanifold_meshes": nonmanifold,
        "door_swing_degrees": 105,
        "door_animation_frames": [1, 30, 60, 90],
        "swing_samples": 121,
        "min_front_frame_clearance_m": round(min_frame_clearance, 5),
        "min_leaf_separation_m": round(min_leaf_gap, 5),
        "mount_root_m": [0, 0, 0],
        "mount_markers": 4,
        "units": "1 Blender unit = 1 metre",
        "scope": "visual-only; no cargo inventory gameplay",
        "limitations": "Analytic door clearance; not an all-pairs mesh collision test."
    }

def main():
    OUT.mkdir(parents=True, exist_ok=True)
    sentinel = bpy.data.objects.new("QA_FOREIGN_SENTINEL", None)
    bpy.context.scene.collection.objects.link(sentinel)
    build()
    first = (len(COL.objects), len(bpy.data.materials))
    doors = build()
    second = (len(COL.objects), len(bpy.data.materials))
    assert first == second, "Repeated build multiplied assets: %r vs %r" % (first, second)
    assert bpy.data.objects.get("QA_FOREIGN_SENTINEL") is sentinel
    bpy.data.objects.remove(sentinel, do_unlink=True)
    qa = validate(doors)
    qa["repeatability"] = "PASS"
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT / "M05-023_spare_parts_container.blend"))
    bpy.ops.object.select_all(action='DESELECT')
    for obj in COL.objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = ROOT
    bpy.ops.export_scene.gltf(
        filepath=str(OUT / "M05-023_spare_parts_container.glb"),
        export_format='GLB', use_selection=True, export_animations=True,
        export_extras=True)
    qa["glb_bytes"] = (OUT / "M05-023_spare_parts_container.glb").stat().st_size
    qa["blend_bytes"] = (OUT / "M05-023_spare_parts_container.blend").stat().st_size
    assert qa["glb_bytes"] > 5000 and qa["blend_bytes"] > 5000
    qa["fbx"] = "NOT RUN"
    try:
        if hasattr(bpy.ops.export_scene, "fbx"):
            bpy.ops.export_scene.fbx(
                filepath=str(OUT / "M05-023_spare_parts_container.fbx"),
                use_selection=True, apply_unit_scale=True,
                add_leaf_bones=False)
            qa["fbx"] = "EXPORTED"
    except Exception as exc:
        qa["fbx"] = "NOT RUN: " + str(exc)
    (OUT / "qa_report.json").write_text(json.dumps(qa, indent=2), encoding="utf-8")
    print("M05-023 QA:", json.dumps(qa, sort_keys=True))

if __name__ == "__main__":
    main()
