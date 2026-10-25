#!/usr/bin/env python3
"""M05-018 Portable Worklight — deterministic visual-only Blender asset.

Usage:
  blender -b --python Tools/ModelFactory/M05/M05-018/generate.py -- --output /tmp/m05-018
Coordinates: metres, Z up, -Y = optical forward, mount at world origin.
Only objects in the dedicated collection are replaced on re-run.
"""
import argparse
import json
import math
import os
import sys

import bpy
import bmesh

ID = "M05-018"
PREFIX = "M05_018"
COLLECTION = PREFIX + "_PORTABLE_WORKLIGHT"
PIVOT_HEIGHT = 0.61
HEAD_HALF_WIDTH = 0.25
YOKE_INNER_X = 0.305
BASE_TOP_Z = 0.255
TILT_DEGREES = (-25.0, 35.0)
MATS = {}
PARTS = []
MARKERS = []


def collection_reset():
    old = bpy.data.collections.get(COLLECTION)
    if old:
        meshes = [obj.data for obj in old.all_objects if obj.type == "MESH"]
        actions = [obj.animation_data.action for obj in old.all_objects
                   if obj.animation_data and obj.animation_data.action]
        for obj in list(old.all_objects):
            bpy.data.objects.remove(obj, do_unlink=True)
        bpy.data.collections.remove(old)
        for mesh in meshes:
            if mesh.users == 0:
                bpy.data.meshes.remove(mesh)
        for action in actions:
            if action.users == 0:
                bpy.data.actions.remove(action)
    col = bpy.data.collections.new(COLLECTION)
    bpy.context.scene.collection.children.link(col)
    return col


def material(label, color, metallic=0.0, roughness=0.5, emission=None):
    name = PREFIX + "_" + label
    mat = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    mat.diffuse_color = (*color, 1.0)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (*color, 1.0)
    bsdf.inputs["Metallic"].default_value = metallic
    bsdf.inputs["Roughness"].default_value = roughness
    if emission:
        if "Emission Color" in bsdf.inputs:
            bsdf.inputs["Emission Color"].default_value = (*emission[0], 1.0)
            bsdf.inputs["Emission Strength"].default_value = emission[1]
        elif "Emission" in bsdf.inputs:
            bsdf.inputs["Emission"].default_value = (*emission[0], 1.0)
    MATS[label] = mat
    return mat


def adopt(obj, col, name, mat, parent, pos, rot=(0.0, 0.0, 0.0)):
    obj.name = PREFIX + "_" + name
    for source_collection in list(obj.users_collection):
        source_collection.objects.unlink(obj)
    col.objects.link(obj)
    obj.parent = parent
    obj.location = pos
    obj.rotation_euler = rot
    if mat:
        obj.data.materials.clear()
        obj.data.materials.append(MATS[mat])
    PARTS.append(obj)
    return obj


def bevel(obj, width):
    if width <= 0:
        return
    mod = obj.modifiers.new("Machined edge radius", "BEVEL")
    mod.width = width
    mod.segments = 2
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.modifier_apply(modifier=mod.name)
    for polygon in obj.data.polygons:
        polygon.use_smooth = True


def box(col, name, pos, dims, mat, parent, radius=0.0, rot=(0.0, 0.0, 0.0)):
    bpy.ops.mesh.primitive_cube_add(size=1.0)
    obj = bpy.context.object
    obj.dimensions = dims
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    adopt(obj, col, name, mat, parent, pos, rot)
    bevel(obj, radius)
    return obj


def cylinder(col, name, pos, radius, depth, mat, parent, vertices=24, axis="Z"):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth)
    obj = bpy.context.object
    rot = {"Z": (0.0, 0.0, 0.0), "X": (0.0, math.pi / 2, 0.0),
           "Y": (math.pi / 2, 0.0, 0.0)}[axis]
    return adopt(obj, col, name, mat, parent, pos, rot)


def empty(col, name, parent, pos):
    obj = bpy.data.objects.new(PREFIX + "_" + name, None)
    col.objects.link(obj)
    obj.empty_display_type = "PLAIN_AXES"
    obj.empty_display_size = 0.045
    obj.parent = parent
    obj.location = pos
    MARKERS.append(obj)
    return obj


def make_materials():
    material("GRAPHITE", (0.042, 0.055, 0.067), 0.7, 0.38)
    material("TITANIUM", (0.29, 0.36, 0.39), 0.83, 0.28)
    material("CERAMIC", (0.76, 0.81, 0.77), 0.12, 0.42)
    material("COPPER", (0.48, 0.22, 0.082), 0.83, 0.31)
    material("RUBBER", (0.023, 0.029, 0.035), 0.02, 0.78)
    material("AMBER", (0.78, 0.32, 0.025), 0.18, 0.42)
    material("LENS", (0.052, 0.11, 0.16), 0.24, 0.16)
    material("LIGHT", (0.85, 0.88, 0.62), 0.04, 0.3,
             emission=((1.0, 0.74, 0.31), 3.0))
    material("STATUS", (0.06, 0.36, 0.28), 0.2, 0.3,
             emission=((0.03, 0.96, 0.55), 2.2))


def create():
    PARTS.clear()
    MARKERS.clear()
    MATS.clear()
    col = collection_reset()
    make_materials()
    root = empty(col, "MOUNT_TOP", None, (0.0, 0.0, 0.0))
    root["mount_type"] = "top_socket"
    root["scale_m"] = 1.0
    root["forward_axis"] = "-Y"
    root["asset_id"] = ID

    # Bolted octagonal-looking lower pedestal, vibration isolation and socket.
    box(col, "mount_plate", (0, 0, 0.045), (0.56, 0.44, 0.09),
        "GRAPHITE", root, 0.028)
    box(col, "mount_ceramic_isolator", (0, 0, 0.104),
        (0.40, 0.34, 0.032), "CERAMIC", root, 0.012)
    cylinder(col, "mast_shaft", (0, 0, 0.237), 0.073, 0.23,
             "TITANIUM", root, 32)
    cylinder(col, "mast_collar", (0, 0, 0.34), 0.12, 0.045,
             "COPPER", root, 32)
    for sx in (-1, 1):
        for sy in (-1, 1):
            x, y = sx * 0.224, sy * 0.164
            cylinder(col, "base_bolt_%s_%s" % (sx, sy),
                     (x, y, 0.102), 0.022, 0.025, "TITANIUM", root, 12)
            cylinder(col, "base_washer_%s_%s" % (sx, sy),
                     (x, y, 0.093), 0.029, 0.009, "RUBBER", root, 16)
    box(col, "pedestal_status_strip", (0, -0.228, 0.062),
        (0.23, 0.009, 0.023), "STATUS", root, 0.005)

    # Static U-yoke: gap between inner faces and the moving lamp housing.
    for side in (-1, 1):
        x = side * 0.341
        box(col, "yoke_upright_%s" % side, (x, 0.055, 0.435),
            (0.072, 0.115, 0.36), "TITANIUM", root, 0.018)
        box(col, "yoke_ceramic_spine_%s" % side, (side * 0.384, 0.055, 0.435),
            (0.012, 0.085, 0.235), "CERAMIC", root, 0.004)
        cylinder(col, "fixed_bearing_%s" % side,
                 (side * 0.35, 0.0, PIVOT_HEIGHT),
                 0.088, 0.045, "GRAPHITE", root, 32, "X")
        cylinder(col, "fixed_bearing_ring_%s" % side,
                 (side * 0.385, 0.0, PIVOT_HEIGHT),
                 0.067, 0.011, "COPPER", root, 32, "X")
        cylinder(col, "hinge_cap_%s" % side,
                 (side * 0.395, 0.0, PIVOT_HEIGHT),
                 0.038, 0.014, "AMBER", root, 24, "X")

    # Only this parent rotates: physical hinge axis X at z=0.61 m.
    head = empty(col, "HEAD_TILT_PIVOT_X", root, (0, 0, PIVOT_HEIGHT))
    head["hinge_axis_local"] = "+X"
    head["tilt_min_deg"] = TILT_DEGREES[0]
    head["tilt_max_deg"] = TILT_DEGREES[1]

    box(col, "head_body", (0, 0.018, 0.035), (0.49, 0.25, 0.335),
        "GRAPHITE", head, 0.039)
    box(col, "front_lens_frame", (0, -0.128, 0.035),
        (0.465, 0.046, 0.308), "TITANIUM", head, 0.022)
    box(col, "optical_lens", (0, -0.156, 0.035),
        (0.414, 0.012, 0.252), "LENS", head, 0.01)
    # 3x3 warm emitters are visible in front of the dark optical carrier.
    for row in range(3):
        for column in range(3):
            x = (column - 1) * 0.124
            z = 0.035 + (row - 1) * 0.075
            box(col, "led_%d_%d" % (row, column), (x, -0.166, z),
                (0.069, 0.012, 0.046), "LIGHT", head, 0.007)
    # Shroud overhang and a separate amber hazard lip.
    box(col, "top_sun_visor", (0, -0.195, 0.214),
        (0.51, 0.19, 0.034), "CERAMIC", head, 0.013)
    box(col, "visor_warning_edge", (0, -0.277, 0.199),
        (0.46, 0.015, 0.011), "AMBER", head, 0.003)
    for side in (-1, 1):
        box(col, "head_side_guard_%s" % side,
            (side * 0.248, -0.095, 0.04),
            (0.018, 0.18, 0.31), "CERAMIC", head, 0.006)
        cylinder(col, "moving_trunnion_%s" % side,
                 (side * 0.278, 0, 0),
                 0.05, 0.045, "TITANIUM", head, 24, "X")
        box(col, "side_hazard_mark_%s" % side,
            (side * 0.259, 0.035, 0.145),
            (0.008, 0.10, 0.017), "AMBER", head, 0.002)
    # Rear cooling fins are attached to the tilting head, not the yoke.
    box(col, "rear_heat_spreader", (0, 0.161, 0.032),
        (0.43, 0.025, 0.285), "COPPER", head, 0.01)
    for index in range(9):
        x = (index - 4) * 0.047
        box(col, "rear_cooling_fin_%02d" % index,
            (x, 0.214, 0.025), (0.017, 0.093, 0.246),
            "TITANIUM", head, 0.004)
    # Sturdy rear carrying handle; no loose cable to clip through the hinge.
    box(col, "handle_crossbar", (0, 0.274, 0.208),
        (0.27, 0.047, 0.044), "RUBBER", head, 0.015)
    for side in (-1, 1):
        box(col, "handle_stanchion_%s" % side,
            (side * 0.125, 0.243, 0.171),
            (0.035, 0.046, 0.091), "TITANIUM", head, 0.009)
    box(col, "rear_service_port", (0, 0.275, -0.108),
        (0.105, 0.028, 0.065), "COPPER", head, 0.008)
    box(col, "rear_power_socket", (0, 0.296, -0.108),
        (0.065, 0.015, 0.041), "RUBBER", head, 0.006)

    empty(col, "OPTICAL_AIM_FORWARD", head, (0, -0.286, 0.035))
    empty(col, "POWER_INPUT_REAR", head, (0, 0.31, -0.108))
    empty(col, "SERVICE_CLEARANCE_TOP", root, (0, 0, 0.97))

    for frame, degrees in ((1, -25.0), (40, 35.0), (80, -25.0)):
        head.rotation_euler[0] = math.radians(degrees)
        head.keyframe_insert(data_path="rotation_euler", frame=frame,
                             group="HeadTilt")
    if head.animation_data and head.animation_data.action:
        action = head.animation_data.action
        # Blender 4.x and 5.x use different Action representations.
        for fc in getattr(action, "fcurves", []):
            for key in fc.keyframe_points:
                key.interpolation = "LINEAR"
    bpy.context.scene.frame_start = 1
    bpy.context.scene.frame_end = 80
    bpy.context.scene.render.fps = 30
    bpy.context.scene.frame_set(1)
    return col, root, head


def validate_geometry():
    if len(MATS) < 3:
        raise RuntimeError("Less than three materials")
    if not PARTS:
        raise RuntimeError("No generated mesh parts")
    bad = []
    triangles = 0
    for obj in PARTS:
        bm = bmesh.new()
        bm.from_mesh(obj.data)
        if any(not edge.is_manifold for edge in bm.edges):
            bad.append(obj.name)
        triangles += sum(max(1, len(face.verts) - 2) for face in bm.faces)
        bm.free()
    if bad:
        raise RuntimeError("Non-manifold meshes: " + ", ".join(bad))
    # Conservative moving head envelope (not a full mesh/mesh collision test).
    # Head body/visor/fins within x=[-.26,.26], y=[-.29,.31], z=[-.18,.24].
    min_clearance = 100.0
    for i in range(121):
        angle = math.radians(TILT_DEGREES[0] +
                             (TILT_DEGREES[1] - TILT_DEGREES[0]) * i / 120)
        for y in (-0.29, 0.31):
            for z in (-0.18, 0.24):
                world_z = PIVOT_HEIGHT + y * math.sin(angle) + z * math.cos(angle)
                min_clearance = min(min_clearance, world_z - BASE_TOP_Z)
    side_gap = YOKE_INNER_X - HEAD_HALF_WIDTH
    if min_clearance <= 0.015 or side_gap <= 0.025:
        raise RuntimeError("Head/base or head/yoke conservative clearance failed")
    return {
        "geometry": "PASS", "closed_manifold_meshes": len(PARTS),
        "triangles": triangles, "materials": len(MATS),
        "tilt_samples": 121,
        "min_head_to_base_clearance_m": round(min_clearance, 5),
        "min_head_to_yoke_side_gap_m": round(side_gap, 5),
        "collision_method": "conservative envelope only; full mesh collision NOT RUN"
    }


def export(col, output):
    os.makedirs(output, exist_ok=True)
    bpy.ops.object.select_all(action="DESELECT")
    for obj in col.all_objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = next(o for o in col.objects if o.type == "EMPTY")
    glb_path = os.path.join(output, ID + ".glb")
    bpy.ops.export_scene.gltf(filepath=glb_path, export_format="GLB",
                              use_selection=True, export_animations=True)
    blend_path = os.path.join(output, ID + ".blend")
    bpy.ops.wm.save_as_mainfile(filepath=blend_path)
    return {"glb": glb_path, "blend": blend_path,
            "fbx": "NOT RUN (not required; exporter compatibility unverified)"}


def main():
    args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", default=os.path.join(os.path.dirname(__file__), "generated"))
    opts = parser.parse_args(args)
    col, root, head = create()
    qa = validate_geometry()
    qa.update({"id": ID, "blender_version": bpy.app.version_string,
               "mount": [0, 0, 0], "forward": "-Y", "tilt_axis": "+X",
               "tilt_range_deg": list(TILT_DEGREES),
               "mesh_parts": len(PARTS), "markers": len(MARKERS),
               "material_names": sorted(MATS),
               "export": export(col, os.path.abspath(opts.output))})
    report_path = os.path.join(os.path.abspath(opts.output), ID + "-qa.json")
    with open(report_path, "w", encoding="utf-8") as handle:
        json.dump(qa, handle, indent=2, ensure_ascii=False)
    print("M05-018 QA:", json.dumps(qa, ensure_ascii=False))


if __name__ == "__main__":
    main()
