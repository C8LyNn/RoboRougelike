#!/usr/bin/env python3
"""M04-001: side-mounted ducted rotor for RoboRougelike (Blender 4.x).

Usage:
    blender --background --python generate_m04_001.py -- --out ./build/M04-001

Creates a GLB with a looping-compatible 48-frame rotor animation and manifest.
Mount plane: X=0; outward axis: +X. Blender Z-up, glTF/Unity Y-up.
This is a VISUAL asset, not a propulsion gameplay implementation.
"""
from __future__ import annotations

import argparse
import json
import math
from pathlib import Path
import sys

import bpy
import bmesh
from mathutils import Vector

MODEL_ID = "M04-001"
COLLECTION_NAME = "RRL_M04_001_SideDuctedRotor"
ROTOR_X = 0.300
BLADE_COUNT = 5
BLADE_TIP_RADIUS = 0.307
DUCT_MAJOR_RADIUS = 0.390
DUCT_TUBE_RADIUS = 0.045
DUCT_INNER_RADIUS = DUCT_MAJOR_RADIUS - DUCT_TUBE_RADIUS
FRAME_START = 1
FRAME_END = 49


def arguments():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    parser = argparse.ArgumentParser()
    parser.add_argument("--out", default="./build/M04-001")
    parser.add_argument("--no-export", action="store_true")
    return parser.parse_args(argv)


def collection_for_model():
    previous = bpy.data.collections.get(COLLECTION_NAME)
    if previous:
        for obj in list(previous.objects):
            bpy.data.objects.remove(obj, do_unlink=True)
        bpy.data.collections.remove(previous)
    result = bpy.data.collections.new(COLLECTION_NAME)
    bpy.context.scene.collection.children.link(result)
    return result


def to_collection(obj, collection):
    for source in list(obj.users_collection):
        source.objects.unlink(obj)
    collection.objects.link(obj)
    return obj


def material(name, rgba, metallic, roughness, emission=None, strength=0.0):
    mat = bpy.data.materials.get("RRL_M04_001_" + name)
    if mat is None:
        mat = bpy.data.materials.new("RRL_M04_001_" + name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = rgba
    bsdf.inputs["Metallic"].default_value = metallic
    bsdf.inputs["Roughness"].default_value = roughness
    if emission is not None:
        socket = bsdf.inputs.get("Emission Color") or bsdf.inputs.get("Emission")
        if socket:
            socket.default_value = emission
        socket = bsdf.inputs.get("Emission Strength")
        if socket:
            socket.default_value = strength
    mat.diffuse_color = rgba
    return mat


def apply_material(obj, mat):
    obj.data.materials.append(mat)
    return obj


def smooth_curves(obj):
    for polygon in obj.data.polygons:
        polygon.use_smooth = len(polygon.vertices) == 4
    return obj


def bevel(obj, width, segments=3):
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    modifier = obj.modifiers.new("Soft machined edges", "BEVEL")
    modifier.width = width
    modifier.segments = segments
    bpy.ops.object.modifier_apply(modifier=modifier.name)
    return obj


def box(collection, name, center, dimensions, mat, radius=0.0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=center)
    obj = to_collection(bpy.context.object, collection)
    obj.name = name
    obj.dimensions = dimensions
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if radius:
        bevel(obj, radius)
    return apply_material(obj, mat)


def cylinder(collection, name, center, radius, depth, mat, vertices=48):
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=vertices, radius=radius, depth=depth, location=center,
        rotation=(0.0, math.pi / 2.0, 0.0)
    )
    obj = to_collection(bpy.context.object, collection)
    obj.name = name
    return apply_material(smooth_curves(obj), mat)


def torus(collection, name, center, major, minor, mat):
    bpy.ops.mesh.primitive_torus_add(
        major_segments=96, minor_segments=16,
        major_radius=major, minor_radius=minor,
        location=center, rotation=(0.0, math.pi / 2.0, 0.0)
    )
    obj = to_collection(bpy.context.object, collection)
    obj.name = name
    for polygon in obj.data.polygons:
        polygon.use_smooth = True
    return apply_material(obj, mat)


def strut(collection, name, start, end, radius, mat):
    start, end = Vector(start), Vector(end)
    axis = end - start
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=24, radius=radius, depth=axis.length,
        location=(start + end) * 0.5
    )
    obj = to_collection(bpy.context.object, collection)
    obj.name = name
    obj.rotation_euler = Vector((0.0, 0.0, 1.0)).rotation_difference(axis).to_euler()
    return apply_material(smooth_curves(obj), mat)


def empty(collection, name, location, parent=None):
    obj = bpy.data.objects.new(name, None)
    collection.objects.link(obj)
    obj.empty_display_type = "PLAIN_AXES"
    obj.empty_display_size = 0.07
    obj.location = location
    if parent:
        obj.parent = parent
    return obj


def parent_preserve_world(obj, parent):
    world = obj.matrix_world.copy()
    obj.parent = parent
    obj.matrix_world = world
    return obj


def blade(collection, index, angle, mat):
    # Four radial stations, each with two tangential edges and two axial faces.
    # Tip radius <= 0.307 m, duct inner radius = 0.345 m.
    stations = (
        (0.105, 0.084, 0.000),
        (0.155, 0.110, 0.004),
        (0.235, 0.090, 0.013),
        (0.305, 0.047, 0.021),
    )
    vertices = []
    tangent = Vector((0.0, -math.sin(angle), math.cos(angle)))
    radial = Vector((0.0, math.cos(angle), math.sin(angle)))
    for r, chord, camber in stations:
        for axial, side in ((-1, -1), (-1, 1), (1, -1), (1, 1)):
            point = (
                Vector((ROTOR_X + camber + axial * 0.009, 0.0, 0.0))
                + radial * r + tangent * (side * chord * 0.5)
            )
            vertices.append(tuple(point))
    faces = [(0, 1, 3, 2), (12, 14, 15, 13)]
    for i in range(len(stations) - 1):
        a, b = i * 4, (i + 1) * 4
        faces.extend((
            (a, b, b + 1, a + 1),
            (a + 2, a + 3, b + 3, b + 2),
            (a, a + 2, b + 2, b),
            (a + 1, b + 1, b + 3, a + 3),
        ))
    mesh = bpy.data.meshes.new("M04_001_blade_mesh_%02d" % index)
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    bm = bmesh.new()
    bm.from_mesh(mesh)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new("Rotor_Blade_%02d" % (index + 1), mesh)
    collection.objects.link(obj)
    apply_material(obj, mat)
    bevel(obj, 0.0035, 2)
    return obj


def build():
    if not BLADE_TIP_RADIUS < DUCT_INNER_RADIUS - 0.03:
        raise ValueError("Rotor blade/duct radial clearance too small")
    scene = bpy.context.scene
    scene.unit_settings.system = "METRIC"
    scene.unit_settings.scale_length = 1.0
    scene.render.fps = 24
    scene.frame_start = FRAME_START
    scene.frame_end = FRAME_END
    collection = collection_for_model()

    metal = material("graphite_metal", (0.055, 0.070, 0.084, 1), 0.80, 0.29)
    ceramic = material("ivory_ceramic", (0.61, 0.64, 0.62, 1), 0.10, 0.38)
    copper = material("copper_alloy", (0.62, 0.24, 0.075, 1), 0.78, 0.26)
    rubber = material("rubber_black", (0.018, 0.021, 0.025, 1), 0.03, 0.84)
    light = material(
        "teal_emissive", (0.04, 0.40, 0.43, 1), 0.10, 0.24,
        emission=(0.02, 0.85, 1.0, 1.0), strength=2.0
    )

    root = empty(collection, "M04_001_ROOT_SIDE_MOUNT", (0.0, 0.0, 0.0))
    root["model_id"] = MODEL_ID
    root["mount_axis_blender"] = "+X"
    root["mount_axis_unity"] = "+X"
    root["units"] = "metres"
    root["visual_only"] = True
    socket = empty(collection, "SOCKET_SIDE_X_POS", (0.0, 0.0, 0.0), root)
    socket["socket_type"] = "side_vehicle_port"
    socket["mount_normal"] = (1.0, 0.0, 0.0)

    static = []
    static.append(box(collection, "Mount_Plate", (0.030, 0, 0),
                      (0.060, 0.220, 0.270), metal, 0.012))
    static.append(box(collection, "Mount_Rubber_Gasket", (0.068, 0, 0),
                      (0.016, 0.190, 0.225), rubber, 0.006))
    for yi in (-0.075, 0.075):
        for zi in (-0.102, 0.102):
            static.append(cylinder(
                collection, "Mount_Bolt_%s_%s" % (yi, zi),
                (0.068, yi, zi), 0.016, 0.014, copper, 16
            ))

    static.append(cylinder(collection, "Bearing_Collar",
                           (0.145, 0, 0), 0.110, 0.090, ceramic))
    static.append(cylinder(collection, "Bearing_Rim",
                           (0.195, 0, 0), 0.123, 0.016, copper))
    for side in (-1, 1):
        static.append(strut(
            collection, "Frame_Arm_%s" % side,
            (0.065, side * 0.070, side * 0.050),
            (0.242, side * 0.382, side * 0.050),
            0.026, metal
        ))
        static.append(strut(
            collection, "Frame_Brace_%s" % side,
            (0.075, side * 0.070, -side * 0.055),
            (0.225, side * 0.300, -side * 0.250),
            0.015, copper
        ))

    static.append(torus(collection, "Duct_Main_Casing",
                        (ROTOR_X, 0, 0), DUCT_MAJOR_RADIUS,
                        DUCT_TUBE_RADIUS, metal))
    for x, name in ((0.240, "Rear"), (0.360, "Front")):
        static.append(torus(collection, "Duct_%s_Lip" % name,
                            (x, 0, 0), 0.392, 0.025, ceramic))
    static.append(torus(collection, "Duct_Accent_Outer",
                        (ROTOR_X, 0, 0), 0.429, 0.009, copper))
    for index in range(8):
        a = index * math.tau / 8
        static.append(cylinder(
            collection, "Duct_Light_%02d" % index,
            (0.310, 0.438 * math.cos(a), 0.438 * math.sin(a)),
            0.009, 0.009, light, 12
        ))

    for obj in static:
        parent_preserve_world(obj, root)

    rotor = empty(collection, "ROTOR_PIVOT_X", (ROTOR_X, 0.0, 0.0), root)
    rotor.rotation_mode = "XYZ"
    rotor["axis"] = "X"
    rotor["blade_count"] = BLADE_COUNT
    moving = [
        cylinder(collection, "Rotor_Shaft", (0.232, 0, 0),
                 0.038, 0.158, copper),
        cylinder(collection, "Rotor_Hub", (ROTOR_X, 0, 0),
                 0.118, 0.066, ceramic),
        cylinder(collection, "Rotor_Hub_Cap", (0.346, 0, 0),
                 0.075, 0.028, metal),
    ]
    for index in range(BLADE_COUNT):
        moving.append(blade(collection, index, index * math.tau / BLADE_COUNT, copper))
    for obj in moving:
        parent_preserve_world(obj, rotor)

    # Key every 3 frames (45 degrees) to prevent quaternion shortest-path
    # collapsing multi-turn animation in GLB/Unity.
    for frame in range(FRAME_START, FRAME_END + 1, 3):
        rotor.rotation_euler[0] = math.tau * (frame - FRAME_START) / 24.0
        rotor.keyframe_insert(data_path="rotation_euler", index=0, frame=frame)
    if rotor.animation_data and rotor.animation_data.action:
        action = rotor.animation_data.action
        if hasattr(action, "fcurves"):
            for curve in action.fcurves:
                for key in curve.keyframe_points:
                    key.interpolation = "LINEAR"
    scene.frame_set(FRAME_START)
    bpy.context.view_layer.update()
    return collection


def export_asset(collection, outdir):
    outdir.mkdir(parents=True, exist_ok=True)
    bpy.ops.object.select_all(action="DESELECT")
    for obj in collection.objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = collection.objects.get("M04_001_ROOT_SIDE_MOUNT")
    path = outdir / "M04-001_side_ducted_rotor.glb"
    bpy.ops.export_scene.gltf(
        filepath=str(path),
        export_format="GLB",
        use_selection=True,
        export_animations=True,
        export_frame_range=True,
        export_frame_step=1,
    )
    manifest = {
        "id": MODEL_ID,
        "title": "Wirnik boczny w osłonie",
        "glb": path.name,
        "mount": "side X+",
        "dimensions_estimate_m": [0.46, 0.88, 0.88],
        "axis_blender": "+X",
        "axis_unity": "+X",
        "animation": "ROTOR_PIVOT_X, 2 turns / 48 frames @ 24 fps",
        "materials": 5,
        "clearance_radial_m_min": round(DUCT_INNER_RADIUS - BLADE_TIP_RADIUS, 3),
        "visual_only": True,
        "unity_import": "Enable animation import and loop the rotor clip."
    }
    (outdir / "M04-001_manifest.json").write_text(
        json.dumps(manifest, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8"
    )
    print("EXPORTED:", path, "bytes=", path.stat().st_size)
    return path


def main():
    args = arguments()
    collection = build()
    if args.no_export:
        print("BUILT_IN_BLENDER: M04-001 (export intentionally disabled)")
    else:
        export_asset(collection, Path(args.out).resolve())


if __name__ == "__main__":
    main()
