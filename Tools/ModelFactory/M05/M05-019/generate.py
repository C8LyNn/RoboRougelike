#!/usr/bin/env python3
"""M05-019 visual holographic projector for RoboRougelike (Blender 4+/Unity 6).
Run: blender -b -noaudio --python generate.py -- --output /tmp/m05-019
Units: meters. Root origin (0,0,0) is the top-socket mounting plane.
This is a VISUAL ASSET only; it does not implement scanning or gameplay.
"""
import argparse
import json
import math
import os
import sys

import bpy
import bmesh
from mathutils import Vector

PREFIX = "M05_019_"
COLLECTION = PREFIX + "Projector"
MOUNT = (0.0, 0.0, 0.0)
PANEL_CLOSED = 0.27
PANEL_OPEN = 0.40
PANEL_Z = 0.43
PANEL_RADIAL_LENGTH = 0.14
PANEL_TANGENTIAL_WIDTH = 0.17
CONE_BOTTOM = 0.55
PANEL_TOP = 0.47
FPS = 30

def collection():
    col = bpy.data.collections.new(COLLECTION)
    bpy.context.scene.collection.children.link(col)
    return col

def cleanup():
    col = bpy.data.collections.get(COLLECTION)
    if col is not None:
        for obj in list(col.objects):
            bpy.data.objects.remove(obj, do_unlink=True)
        bpy.data.collections.remove(col, do_unlink=True)
    for mat in list(bpy.data.materials):
        if mat.name.startswith(PREFIX) and mat.users == 0:
            bpy.data.materials.remove(mat)
    for mesh in list(bpy.data.meshes):
        if mesh.name.startswith(PREFIX) and mesh.users == 0:
            bpy.data.meshes.remove(mesh)

def move_to_collection(obj, col):
    for old in list(obj.users_collection):
        old.objects.unlink(obj)
    col.objects.link(obj)
    return obj

def material(label, rgb, metallic=0.0, roughness=0.5, emission=0.0, alpha=1.0):
    mat = bpy.data.materials.new(PREFIX + label)
    mat.diffuse_color = (*rgb, alpha)
    mat.use_nodes = True
    shader = mat.node_tree.nodes.get("Principled BSDF")
    shader.inputs["Base Color"].default_value = (*rgb, 1.0)
    shader.inputs["Metallic"].default_value = metallic
    shader.inputs["Roughness"].default_value = roughness
    shader.inputs["Alpha"].default_value = alpha
    if emission:
        socket = shader.inputs.get("Emission Color") or shader.inputs.get("Emission")
        if socket:
            socket.default_value = (*rgb, 1.0)
        strength = shader.inputs.get("Emission Strength")
        if strength:
            strength.default_value = emission
    if alpha < 1.0:
        if hasattr(mat, "surface_render_method"):
            try:
                mat.surface_render_method = "DITHERED"
            except (ValueError, TypeError):
                pass
        elif hasattr(mat, "blend_method"):
            mat.blend_method = "BLEND"
        mat.use_nodes = True
    return mat

def assign(obj, mat):
    obj.data.materials.append(mat)
    return obj

def finalize(obj, name, col, mat, parent=None, pos=None, angle=0.0, bevel=0.0):
    obj.name = PREFIX + name
    obj.data.name = PREFIX + name + "_Mesh"
    move_to_collection(obj, col)
    if parent is not None:
        obj.parent = parent
    if pos is not None:
        obj.location = pos
    obj.rotation_euler[2] = angle
    assign(obj, mat)
    if bevel:
        mod = obj.modifiers.new("Machined edges", "BEVEL")
        mod.width = bevel
        mod.segments = 2
        if hasattr(mod, "affect"):
            mod.affect = "EDGES"
        if hasattr(obj.data, "use_auto_smooth"):
            obj.data.use_auto_smooth = True
        obj.modifiers.new("Weighted normals", "WEIGHTED_NORMAL")
    return obj

def box(name, dims, pos, mat, col, angle=0.0, parent=None, bevel=0.0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=(0, 0, 0))
    obj = bpy.context.object
    obj.dimensions = dims
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return finalize(obj, name, col, mat, parent, pos, angle, bevel)

def cylinder(name, radius, depth, pos, mat, col, vertices=32, parent=None, bevel=0.0):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=(0, 0, 0))
    return finalize(bpy.context.object, name, col, mat, parent, pos, bevel=bevel)

def ring(name, major, minor, pos, mat, col, parent=None):
    bpy.ops.mesh.primitive_torus_add(major_segments=48, minor_segments=8,
                                    location=(0, 0, 0), major_radius=major,
                                    minor_radius=minor)
    return finalize(bpy.context.object, name, col, mat, parent, pos)

def marker(name, pos, col, parent=None):
    obj = bpy.data.objects.new(PREFIX + name, None)
    col.objects.link(obj)
    obj.empty_display_type = "SPHERE"
    obj.empty_display_size = 0.035
    if parent is not None:
        obj.parent = parent
    obj.location = pos
    return obj

def create():
    cleanup()
    col = collection()
    mats = {
        "graphite": material("Graphite", (0.035, 0.051, 0.070), 0.65, 0.34),
        "titanium": material("Titanium", (0.37, 0.46, 0.51), 0.83, 0.28),
        "ceramic": material("IvoryCeramic", (0.76, 0.81, 0.78), 0.12, 0.26),
        "copper": material("Copper", (0.48, 0.21, 0.083), 0.80, 0.26),
        "rubber": material("Rubber", (0.015, 0.020, 0.026), 0.05, 0.79),
        "cyan": material("CyanEmitter", (0.02, 0.70, 0.96), 0.15, 0.24, 5.0),
        "amber": material("AmberStatus", (1.0, 0.43, 0.045), 0.1, 0.3, 2.8),
        "field": material("VolumetricHologram", (0.03, 0.45, 0.88), 0.0, 0.2, 1.5, 0.11),
    }
    root = marker("MOUNT_TOP", MOUNT, col)
    cylinder("Socket", 0.18, 0.085, (0, 0, 0.045), mats["titanium"], col, bevel=0.008)
    cylinder("Insulator", 0.202, 0.040, (0, 0, 0.106), mats["rubber"], col)
    cylinder("MainHousing", 0.247, 0.185, (0, 0, 0.216), mats["graphite"], col, vertices=12, bevel=0.012)
    cylinder("UpperCollar", 0.224, 0.035, (0, 0, 0.325), mats["titanium"], col, bevel=0.006)
    cylinder("OpticTower", 0.078, 0.20, (0, 0, 0.433), mats["ceramic"], col, bevel=0.005)
    cylinder("ProjectorLens", 0.071, 0.020, (0, 0, 0.545), mats["cyan"], col)
    ring("OpticHalo", 0.086, 0.010, (0, 0, 0.552), mats["copper"], col)
    for k in range(8):
        a = math.tau * k / 8
        x, y = 0.22 * math.cos(a), 0.22 * math.sin(a)
        cylinder("Bolt_%02d" % k, 0.012, 0.012, (x, y, 0.335), mats["copper"], col, vertices=12)
    for k in range(4):
        a = math.tau * k / 4
        cs, sn = math.cos(a), math.sin(a)
        box("GuideRail_%d" % k, (0.295, 0.052, 0.030),
            (0.270 * cs, 0.270 * sn, 0.355), mats["titanium"], col, a, bevel=0.005)
        box("RailGroove_%d" % k, (0.29, 0.012, 0.005),
            (0.270 * cs, 0.270 * sn, 0.373), mats["copper"], col, a)
        pivot = marker("PANEL_PIVOT_%d" % k, (PANEL_CLOSED * cs, PANEL_CLOSED * sn, PANEL_Z), col)
        pivot.rotation_euler[2] = a
        box("Panel_%d" % k, (PANEL_RADIAL_LENGTH, PANEL_TANGENTIAL_WIDTH, 0.080),
            (0, 0, 0), mats["ceramic"], col, parent=pivot, bevel=0.009)
        box("PanelTop_%d" % k, (0.115, 0.125, 0.012),
            (0, 0, 0.048), mats["graphite"], col, parent=pivot, bevel=0.004)
        box("Slider_%d" % k, (0.095, 0.045, 0.045),
            (0, 0, -0.075), mats["copper"], col, parent=pivot, bevel=0.004)
        for side in (-1, 1):
            box("Emitter_%d_%s" % (k, "L" if side < 0 else "R"),
                (0.055, 0.022, 0.008), (0.005, side * 0.047, 0.056),
                mats["cyan"], col, parent=pivot, bevel=0.002)
        for frame, radius in ((1, PANEL_CLOSED), (25, PANEL_OPEN), (49, PANEL_CLOSED)):
            pivot.location = (radius * cs, radius * sn, PANEL_Z)
            pivot.keyframe_insert(data_path="location", frame=frame, group="M05-019 panel deployment")
        if pivot.animation_data and pivot.animation_data.action:
            action = pivot.animation_data.action
            if hasattr(action, "fcurves"):
                for fc in action.fcurves:
                    for key in fc.keyframe_points:
                        key.interpolation = "LINEAR"
    bpy.ops.mesh.primitive_cone_add(vertices=48, radius1=0.075, radius2=0.315,
                                    depth=0.64, location=(0, 0, 0))
    finalize(bpy.context.object, "HologramField", col, mats["field"], pos=(0, 0, 0.875))
    for i, (z, radius) in enumerate(((0.61, 0.096), (0.80, 0.17), (1.00, 0.25), (1.17, 0.30))):
        ring("ProjectionScanRing_%d" % i, radius, 0.0035, (0, 0, z), mats["cyan"], col)
    for k in range(4):
        a = math.tau * k / 4
        box("HousingVent_%d" % k, (0.060, 0.020, 0.058),
            (0.220 * math.cos(a), 0.220 * math.sin(a), 0.23),
            mats["copper"], col, a)
    marker("POWER_INPUT", (0, -0.14, 0.02), col)
    marker("DATA_INPUT", (0.12, 0.08, 0.02), col)
    marker("PROJECTION_ORIGIN", (0, 0, 0.55), col)
    scene = bpy.context.scene
    scene.frame_start, scene.frame_end = 1, 49
    scene.render.fps = FPS
    scene.unit_settings.system = "METRIC"
    scene.unit_settings.scale_length = 1.0
    scene.frame_set(1)
    bpy.context.view_layer.update()
    return col

def validate(col):
    meshes = [o for o in col.objects if o.type == "MESH"]
    materials = {m.name for obj in meshes for m in obj.data.materials if m}
    assert len(materials) >= 3, "At least three distinct materials required"
    assert len(meshes) >= 35, "Unexpectedly incomplete projector"
    assert bpy.data.objects.get(PREFIX + "MOUNT_TOP") is not None
    assert bpy.data.objects.get(PREFIX + "PROJECTION_ORIGIN") is not None
    for obj in meshes:
        bm = bmesh.new()
        bm.from_mesh(obj.data)
        invalid = [e for e in bm.edges if not e.is_manifold]
        bm.free()
        assert not invalid, "%s has %d nonmanifold edges" % (obj.name, len(invalid))
    # Conservative analytic separation, independent of panel animation.
    # Panels are at z=0.39..0.47; field begins at z=0.555.
    assert CONE_BOTTOM - PANEL_TOP >= 0.08
    assert PANEL_CLOSED - PANEL_RADIAL_LENGTH / 2 - 0.078 > 0.11
    assert 2 * PANEL_CLOSED * math.sin(math.pi / 4) > PANEL_TANGENTIAL_WIDTH + 0.15
    samples = 121
    minimum_core_gap = 1e9
    minimum_neighbor_gap = 1e9
    for step in range(samples):
        t = step / (samples - 1)
        radius = PANEL_CLOSED + (PANEL_OPEN - PANEL_CLOSED) * t
        minimum_core_gap = min(minimum_core_gap, radius - PANEL_RADIAL_LENGTH / 2 - 0.078)
        minimum_neighbor_gap = min(minimum_neighbor_gap,
                                   math.sqrt(2) * radius - PANEL_TANGENTIAL_WIDTH)
    assert minimum_core_gap > 0.1
    assert minimum_neighbor_gap > 0.15
    for k in range(4):
        obj = bpy.data.objects.get(PREFIX + "PANEL_PIVOT_%d" % k)
        assert obj is not None and obj.animation_data is not None
        assert obj.animation_data.action is not None
    return {
        "id": "M05-019",
        "status": "PASS",
        "mesh_count": len(meshes),
        "material_count": len(materials),
        "object_count": len(col.objects),
        "frames": [1, 25, 49],
        "analytic_motion_samples": samples,
        "minimum_core_gap_m": round(minimum_core_gap, 5),
        "minimum_adjacent_center_clearance_proxy_m": round(minimum_neighbor_gap, 5),
        "panel_to_hologram_vertical_gap_m": round(CONE_BOTTOM - PANEL_TOP, 5),
        "full_triangle_collision_test": "NOT RUN",
        "unity_import": "NOT RUN",
        "unity_editmode": "NOT RUN",
        "unity_playmode": "NOT RUN",
    }

def run(output):
    # An unrelated sentinel must survive repeated generation.
    sentinel = bpy.data.objects.new("M05_019_FOREIGN_SENTINEL_QA", None)
    bpy.context.scene.collection.objects.link(sentinel)
    first = create()
    a = validate(first)
    second = create()
    b = validate(second)
    assert a["mesh_count"] == b["mesh_count"]
    assert a["material_count"] == b["material_count"]
    assert a["object_count"] == b["object_count"]
    assert bpy.data.objects.get(sentinel.name) is sentinel
    bpy.data.objects.remove(sentinel, do_unlink=True)
    b["idempotence"] = "PASS (2 builds, foreign object preserved)"
    b["blender_version"] = bpy.app.version_string
    os.makedirs(output, exist_ok=True)
    for obj in bpy.context.selected_objects:
        obj.select_set(False)
    for obj in second.objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = next(o for o in second.objects if o.type == "MESH")
    glb = os.path.join(output, "M05-019-holographic-projector.glb")
    bpy.ops.export_scene.gltf(filepath=glb, export_format="GLB", use_selection=True,
                              export_animations=True)
    b["glb"] = glb
    fbx = os.path.join(output, "M05-019-holographic-projector.fbx")
    try:
        bpy.ops.export_scene.fbx(filepath=fbx, use_selection=True,
                                 apply_unit_scale=True, bake_anim=True)
        b["fbx"] = fbx
    except (AttributeError, RuntimeError, TypeError) as exc:
        b["fbx"] = "NOT RUN / BLOCKED: " + str(exc)
    blend = os.path.join(output, "M05-019-holographic-projector.blend")
    bpy.ops.wm.save_as_mainfile(filepath=blend)
    b["blend"] = blend
    with open(os.path.join(output, "M05-019-qa.json"), "w", encoding="utf-8") as handle:
        json.dump(b, handle, ensure_ascii=False, indent=2)
    print("M05-019_QA_REPORT=" + json.dumps(b, ensure_ascii=False))

if __name__ == "__main__":
    args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", default="/tmp/m05-019")
    cfg = parser.parse_args(args)
    run(os.path.abspath(cfg.output))
