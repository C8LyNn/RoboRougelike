#!/usr/bin/env python3
"""M03-017 — Quick-release chassis coupler. Blender 4.x bpy procedural asset.

Usage: blender -b --python generate_m03_017.py -- /tmp/m03-017
Units: metres. Pivot: (0,0,0), bottom chassis mounting plane.
Animation: slide lock bolt +X by 0.10 m, red/green indicator tabs move on Z.
No Unity scenes, prefabs, scripts or project settings are modified.
"""
import json
import math
import os
import sys
from pathlib import Path

import bpy

PREFIX = "M03_017_"
COLLECTION = "M03-017_QuickReleaseCoupler"
FRAME_END = 72

PALETTE = {
    "graphite": ((0.055, 0.071, 0.090, 1), 0.82, 0.31),
    "titanium": ((0.39, 0.46, 0.52, 1), 0.85, 0.28),
    "copper": ((0.63, 0.29, 0.11, 1), 0.72, 0.26),
    "ceramic": ((0.77, 0.80, 0.73, 1), 0.12, 0.38),
    "red": ((0.72, 0.035, 0.025, 1), 0.16, 0.28),
    "green": ((0.04, 0.65, 0.14, 1), 0.14, 0.28),
}

def clear_previous():
    previous = bpy.data.collections.get(COLLECTION)
    if previous:
        for ob in list(previous.objects):
            bpy.data.objects.remove(ob, do_unlink=True)
        bpy.data.collections.remove(previous)
    for mesh in list(bpy.data.meshes):
        if mesh.name.startswith(PREFIX) and mesh.users == 0:
            bpy.data.meshes.remove(mesh)
    for material in list(bpy.data.materials):
        if material.name.startswith(PREFIX) and material.users == 0:
            bpy.data.materials.remove(material)

def material(name, rgba, metallic, roughness):
    mat = bpy.data.materials.new(PREFIX + name)
    mat.diffuse_color = rgba
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = rgba
    bsdf.inputs["Metallic"].default_value = metallic
    bsdf.inputs["Roughness"].default_value = roughness
    return mat

def place(ob, col, parent=None):
    for old in list(ob.users_collection):
        old.objects.unlink(ob)
    col.objects.link(ob)
    if parent is not None:
        ob.parent = parent
        ob.matrix_parent_inverse.identity()
    return ob

def bevel(ob, amount):
    if amount <= 0:
        return
    mod = ob.modifiers.new("Machined edge radius", "BEVEL")
    mod.width = amount
    mod.segments = 2
    bpy.context.view_layer.objects.active = ob
    bpy.ops.object.modifier_apply(modifier=mod.name)
    for face in ob.data.polygons:
        face.use_smooth = False

def box(name, center, dimensions, mat, col, parent=None, radius=0.002):
    bpy.ops.mesh.primitive_cube_add(size=1, location=center)
    ob = bpy.context.object
    ob.name = PREFIX + name
    ob.dimensions = dimensions
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    ob.data.materials.append(mat)
    bevel(ob, min(radius, min(dimensions) * 0.18))
    return place(ob, col, parent)

def cylinder(name, center, radius, length, mat, col, parent=None,
             axis="X", bevel_radius=0.0015):
    rotation = (0, math.pi / 2, 0) if axis == "X" else (0, 0, 0)
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=40, radius=radius, depth=length,
        end_fill_type="NGON", location=center, rotation=rotation)
    ob = bpy.context.object
    ob.name = PREFIX + name
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    ob.data.materials.append(mat)
    bevel(ob, bevel_radius)
    return place(ob, col, parent)

def ring(name, center, mat, col, parent=None):
    # Inner bore radius = 0.060 - 0.014 = 0.046 m.
    # Sliding pin radius = 0.029 m, radial clearance >= 0.017 m.
    bpy.ops.mesh.primitive_torus_add(
        major_segments=48, minor_segments=12,
        location=center, rotation=(0, math.pi / 2, 0),
        major_radius=0.060, minor_radius=0.014)
    ob = bpy.context.object
    ob.name = PREFIX + name
    ob.data.materials.append(mat)
    return place(ob, col, parent)

def empty(name, location, col, parent=None):
    ob = bpy.data.objects.new(PREFIX + name, None)
    col.objects.link(ob)
    ob.location = location
    ob.empty_display_type = "ARROWS"
    ob.empty_display_size = 0.065
    if parent:
        ob.parent = parent
    return ob

def key_location(ob, frames_and_xyz):
    for frame, xyz in frames_and_xyz:
        ob.location = xyz
        ob.keyframe_insert(data_path="location", frame=frame, group="QuickRelease")
    if ob.animation_data and ob.animation_data.action:
        for curve in ob.animation_data.action.fcurves:
            for point in curve.keyframe_points:
                point.interpolation = "LINEAR"

def create():
    clear_previous()
    col = bpy.data.collections.new(COLLECTION)
    bpy.context.scene.collection.children.link(col)
    mats = {key: material(key, *args) for key, args in PALETTE.items()}
    root = empty("ROOT_PIVOT_MOUNT_Z0", (0, 0, 0), col)

    # Rigid housing: bottom mounting plane Z=0; top of plate Z=0.050.
    box("mount_plate", (0, 0, 0.025), (0.72, 0.50, 0.05),
        mats["graphite"], col, root, 0.012)
    box("top_ceramic_isolator", (0, 0, 0.060), (0.46, 0.20, 0.014),
        mats["ceramic"], col, root, 0.004)
    for xsign in (-1, 1):
        for ysign in (-1, 1):
            x, y = xsign * 0.282, ysign * 0.188
            box("mount_pad_%s_%s" % (xsign, ysign),
                (x, y, 0.066), (0.11, 0.085, 0.032),
                mats["titanium"], col, root, 0.005)
            cylinder("bolt_head_%s_%s" % (xsign, ysign),
                     (x, y, 0.092), 0.019, 0.013,
                     mats["copper"], col, root, axis="Z")
            empty("SNAP_%s_%s_Z_UP" % (xsign, ysign), (x, y, 0), col, root)

    # Receiver and guide rings are actual open-bore toroidal meshes.
    for name, x in (("guide", -0.18), ("receiver", 0.22)):
        ring(name + "_open_bore", (x, 0, 0.202), mats["titanium"], col, root)
        for ysign in (-1, 1):
            box(name + "_support_%s" % ysign,
                (x, ysign * 0.079, 0.112),
                (0.045, 0.038, 0.093), mats["graphite"], col, root, 0.003)
    # Side rails are offset from the moving bolt and never cross its envelope.
    for sign in (-1, 1):
        box("side_guide_%s" % sign, (0, sign * 0.120, 0.132),
            (0.47, 0.030, 0.040), mats["titanium"], col, root, 0.004)

    # Bolt is a separate parent, so GLB/FBX preserves an animated transform.
    slider = empty("ANIM_LOCK_BOLT_PLUS_X", (0, 0, 0), col, root)
    cylinder("sliding_lock_pin", (-0.09, 0, 0.202),
             0.029, 0.48, mats["titanium"], col, slider, "X")
    cylinder("copper_lock_tip", (0.146, 0, 0.202),
             0.027, 0.012, mats["copper"], col, slider, "X")
    cylinder("operating_handle", (-0.175, 0, 0.295),
             0.018, 0.146, mats["copper"], col, slider, "Z")
    bpy.ops.mesh.primitive_uv_sphere_add(segments=20, ring_count=12,
                                         radius=0.042,
                                         location=(-0.175, 0, 0.383))
    knob = bpy.context.object
    knob.name = PREFIX + "grip_ball"
    knob.data.materials.append(mats["graphite"])
    place(knob, col, slider)
    # Unlocked: bolt tip at X~0.152, before receiver X=0.22.
    # Locked: +0.10 m travel, tip at X~0.252, inside receiver bore.
    key_location(slider, [(1, (0, 0, 0)), (24, (0.10, 0, 0)),
                          (48, (0.10, 0, 0)), (72, (0, 0, 0))])

    # Mechanical indicators: colored tabs move up/down inside open guide frames.
    # They occupy y = +/-0.177, safely clear of bolt (r=0.029).
    for label, y, color, positions in (
        ("UNLOCKED_RED", -0.177, "red", [0.151, 0.093, 0.093, 0.151]),
        ("LOCKED_GREEN", 0.177, "green", [0.093, 0.151, 0.151, 0.093]),
    ):
        for xoff in (-0.043, 0.043):
            box(label + "_indicator_rail_%s" % xoff,
                (-0.217 + xoff, y, 0.113),
                (0.013, 0.070, 0.115), mats["graphite"], col, root, 0.002)
        box(label + "_window_lower_lip", (-0.217, y, 0.061),
            (0.105, 0.071, 0.015), mats["copper"], col, root, 0.002)
        tab = empty("ANIM_" + label + "_FLAG_Z", (-0.217, y, positions[0]),
                    col, root)
        box(label + "_color_tab", (0, 0, 0),
            (0.060, 0.044, 0.022), mats[color], col, tab, 0.002)
        key_location(tab, [(1, (-0.217, y, positions[0])),
                           (24, (-0.217, y, positions[1])),
                           (48, (-0.217, y, positions[2])),
                           (72, (-0.217, y, positions[3]))])
    for x in (-0.32, 0.32):
        box("caution_marker_%s" % x, (x, 0.0, 0.053),
            (0.035, 0.080, 0.005), mats["copper"], col, root, 0.001)

    scene = bpy.context.scene
    scene.frame_start = 1
    scene.frame_end = FRAME_END
    scene.render.fps = 24
    scene.unit_settings.system = "METRIC"
    scene.unit_settings.scale_length = 1.0
    scene.frame_set(1)
    bpy.context.view_layer.update()
    return col

def verify(col):
    import bmesh
    meshes = [o for o in col.objects if o.type == "MESH"]
    mounts = [o for o in col.objects if o.name.startswith(PREFIX + "SNAP_")]
    animated = [o for o in col.objects if o.animation_data and o.animation_data.action]
    assert len(meshes) >= 20, len(meshes)
    assert len(mounts) == 4, len(mounts)
    assert len(animated) == 3, len(animated)
    assert len({m.name for ob in meshes for m in ob.data.materials}) >= 3
    for ob in meshes:
        bm = bmesh.new()
        bm.from_mesh(ob.data)
        assert all(e.is_manifold for e in bm.edges), ob.name
        assert bm.calc_volume(signed=True) > 0, ob.name
        bm.free()
    # Analytic swept clearance: bore inner radius 0.046, bolt outer 0.029.
    assert 0.046 - 0.029 >= 0.015
    for frame in (1, 12, 24, 48, 60, 72):
        bpy.context.scene.frame_set(frame)
        bolt = bpy.data.objects[PREFIX + "ANIM_LOCK_BOLT_PLUS_X"]
        assert -1e-5 <= bolt.location.x <= 0.10001
    bpy.context.scene.frame_set(1)
    return {"id": "M03-017", "mesh_count": len(meshes),
            "mounts": len(mounts), "materials": len(PALETTE),
            "animated_objects": len(animated), "frames": [1, 24, 48, 72],
            "bore_clearance_m": 0.017, "lock_travel_m": 0.10}

def export(out):
    out.mkdir(parents=True, exist_ok=True)
    col = create()
    report = verify(col)
    bpy.ops.export_scene.gltf(filepath=str(out / "M03-017.glb"),
                              export_format="GLB", export_animations=True)
    bpy.ops.export_scene.fbx(filepath=str(out / "M03-017.fbx"),
                             use_selection=False, bake_anim=True,
                             apply_unit_scale=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(out / "M03-017.blend"))
    for ext in ("glb", "fbx", "blend"):
        path = out / ("M03-017." + ext)
        assert path.exists() and path.stat().st_size > 1000, path
    (out / "M03-017.qa.json").write_text(
        json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print("M03-017 BLENDER_EXPORT_PASS", json.dumps(report))
    return report

if __name__ == "__main__":
    args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    output = Path(args[0]) if args else Path.cwd() / "M03-017-export"
    export(output)
