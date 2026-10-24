#!/usr/bin/env python3
"""M05-009 side liquid radiator. Blender 3.6+/4.x, Unity scale 1 BU = 1 m.

Mount at local origin; +X points away from vehicle side wall; Y width;
Z up. Fan rotor pivots about local +X. Visual-only: no gameplay components.

Usage: blender -b --python generate_m05_009.py -- --out /tmp/m05-009
"""
import argparse
import json
import math
import sys
from pathlib import Path

import bpy
import bmesh

ID = "M05-009"
COLLECTION = "RRL_M05_009"
FAN_CENTER = (0.450, 0.0, 0.025)
FAN_INNER_R = 0.205
FAN_BLADE_R = 0.178
FAN_HUB_R = 0.059
FAN_BLADES = 7
FAN_FRAME_START = 1
FAN_FRAME_END = 61
FAN_ROTATIONS = 1
MOUNT_ORIGIN = (0.0, 0.0, 0.0)
MATS = {
    "graphite": ((0.045, 0.064, 0.081, 1), 0.70, 0.39, None),
    "titanium": ((0.38, 0.45, 0.49, 1), 0.81, 0.27, None),
    "copper": ((0.49, 0.18, 0.070, 1), 0.74, 0.32, None),
    "ceramic": ((0.77, 0.79, 0.72, 1), 0.11, 0.31, None),
    "rubber": ((0.028, 0.033, 0.040, 1), 0.03, 0.82, None),
    "coolant": ((0.015, 0.39, 0.48, 1), 0.15, 0.17, ((0.01, 0.60, 0.81, 1), 3.0)),
    "amber": ((0.82, 0.36, 0.035, 1), 0.12, 0.32, ((1.0, 0.29, 0.02, 1), 0.75)),
    "glass": ((0.075, 0.22, 0.29, 1), 0.08, 0.16, None),
}
OWNED = None
OBJECTS = []
MATERIALS = {}


def reset_owned():
    """Delete only our previous generation, including orphaned datablocks."""
    old = bpy.data.collections.get(COLLECTION)
    if old:
        owned_objects = list(old.objects)
        old_meshes = [o.data for o in owned_objects if o.type == "MESH"]
        old_curves = [o.data for o in owned_objects if o.type == "CURVE"]
        old_actions = [o.animation_data.action for o in owned_objects
                       if o.animation_data and o.animation_data.action]
        for obj in owned_objects:
            bpy.data.objects.remove(obj, do_unlink=True)
        bpy.data.collections.remove(old)
        for mesh in old_meshes:
            if mesh.users == 0:
                bpy.data.meshes.remove(mesh)
        for curve in old_curves:
            if curve.users == 0:
                bpy.data.curves.remove(curve)
        for action in old_actions:
            if action.users == 0:
                bpy.data.actions.remove(action)
    global OWNED, OBJECTS
    OWNED = bpy.data.collections.new(COLLECTION)
    bpy.context.scene.collection.children.link(OWNED)
    OBJECTS = []


def own(obj):
    for c in list(obj.users_collection):
        c.objects.unlink(obj)
    OWNED.objects.link(obj)
    obj["model_id"] = ID
    OBJECTS.append(obj)
    return obj


def material(key):
    name = ID.replace("-", "_") + "_" + key
    mat = bpy.data.materials.get(name)
    if mat is None:
        mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    mat.diffuse_color = MATS[key][0]
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    assert bsdf is not None
    rgba, metal, rough, emission = MATS[key]
    bsdf.inputs["Base Color"].default_value = rgba
    bsdf.inputs["Metallic"].default_value = metal
    bsdf.inputs["Roughness"].default_value = rough
    if emission:
        col, strength = emission
        socket = bsdf.inputs.get("Emission Color") or bsdf.inputs.get("Emission")
        if socket:
            socket.default_value = col
        power = bsdf.inputs.get("Emission Strength")
        if power:
            power.default_value = strength
    MATERIALS[key] = mat
    return mat


def finish_mesh(obj, mat):
    obj.data.materials.append(MATERIALS[mat])
    for p in obj.data.polygons:
        p.use_smooth = True
    return own(obj)


def box(name, pos, size, mat, bevel=0.008, parent=None):
    bpy.ops.mesh.primitive_cube_add(size=1, location=(0, 0, 0) if parent else pos)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if parent:
        obj.parent = parent
        obj.location = pos
    if bevel:
        mod = obj.modifiers.new("manufactured_radius", "BEVEL")
        mod.width = bevel
        mod.segments = 2
        if hasattr(mod, "affect"):
            mod.affect = "EDGES"
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=mod.name)
        weighted = obj.modifiers.new("weighted_normals", "WEIGHTED_NORMAL")
        weighted.keep_sharp = True
    return finish_mesh(obj, mat)


def cylinder(name, pos, radius, depth, mat, vertices=32, parent=None):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth,
                                        location=(0, 0, 0) if parent else pos)
    obj = bpy.context.object
    obj.name = name
    obj.rotation_euler[1] = math.pi / 2
    if parent:
        obj.parent = parent
        obj.location = pos
    return finish_mesh(obj, mat)


def mesh_object(name, vertices, faces, mat, parent=None):
    mesh = bpy.data.meshes.new(name + "_mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.validate(verbose=False)
    mesh.update()
    bm = bmesh.new()
    bm.from_mesh(mesh)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new(name, mesh)
    OWNED.objects.link(obj)
    if parent:
        obj.parent = parent
    obj.data.materials.append(MATERIALS[mat])
    for p in obj.data.polygons:
        p.use_smooth = True
    obj["model_id"] = ID
    OBJECTS.append(obj)
    return obj


def annulus(name, center, inner, outer, depth, mat, n=64):
    # Closed manifold ring aligned to local X, with four quad bands.
    verts = []
    for x in (center[0] - depth / 2, center[0] + depth / 2):
        for radius in (outer, inner):
            for i in range(n):
                a = 2 * math.pi * i / n
                verts.append((x, center[1] + radius * math.cos(a),
                              center[2] + radius * math.sin(a)))
    def idx(side, radial, i):
        return (side * 2 + radial) * n + i % n
    faces = []
    for i in range(n):
        j = (i + 1) % n
        faces.extend((
            (idx(0, 0, i), idx(0, 0, j), idx(1, 0, j), idx(1, 0, i)),
            (idx(1, 1, i), idx(1, 1, j), idx(0, 1, j), idx(0, 1, i)),
            (idx(0, 1, i), idx(0, 1, j), idx(0, 0, j), idx(0, 0, i)),
            (idx(1, 0, i), idx(1, 0, j), idx(1, 1, j), idx(1, 1, i)),
        ))
    return mesh_object(name, verts, faces, mat)


def tube(name, coords, radius, mat):
    curve = bpy.data.curves.new(name + "_curve", "CURVE")
    curve.dimensions = "3D"
    curve.resolution_u = 16
    curve.bevel_depth = radius
    curve.bevel_resolution = 4
    curve.use_fill_caps = True
    spline = curve.splines.new("POLY")
    spline.points.add(len(coords) - 1)
    for point, xyz in zip(spline.points, coords):
        point.co = (*xyz, 1.0)
    obj = bpy.data.objects.new(name, curve)
    OWNED.objects.link(obj)
    obj.data.materials.append(MATERIALS[mat])
    obj["model_id"] = ID
    OBJECTS.append(obj)
    return obj


def rotor_blade(name, phase, parent):
    # Swept, closed eight-vertex blade; no reliance on open zero-thickness faces.
    inner, outer = 0.068, FAN_BLADE_R
    profile = ((inner, -0.14), (outer, 0.13),
               (outer, 0.38), (inner, 0.10))
    verts = []
    for x in (-0.011, 0.011):
        for r, offset in profile:
            a = phase + offset
            verts.append((x, r * math.cos(a), r * math.sin(a)))
    faces = [(0, 3, 2, 1), (4, 5, 6, 7)]
    faces += [(i, (i + 1) % 4, (i + 1) % 4 + 4, i + 4) for i in range(4)]
    return mesh_object(name, verts, faces, "titanium", parent)


def empty(name, loc, parent=None):
    obj = bpy.data.objects.new(name, None)
    OWNED.objects.link(obj)
    obj.empty_display_type = "SPHERE"
    obj.empty_display_size = 0.035
    obj.location = loc
    obj.parent = parent
    obj["model_id"] = ID
    OBJECTS.append(obj)
    return obj


def build():
    reset_owned()
    MATERIALS.clear()
    for key in MATS:
        material(key)
    mount = empty("MOUNT_SIDE_SOCKET", MOUNT_ORIGIN)
    mount["axis_out"] = "+X"
    mount["orientation"] = "Y horizontal, Z vertical; 1 BU = 1 m"
    # Isolated mount plate and four bolt interfaces.
    box("Mount_Backplate", (.045, 0, 0), (.090, .760, .570), "graphite", .025)
    box("Mount_Ceramic_Gasket", (.092, 0, 0), (.018, .680, .500), "rubber", .008)
    for sy in (-1, 1):
        for sz in (-1, 1):
            y, z = sy * .317, sz * .236
            cylinder("Mount_Bolt_%s_%s" % (sy, sz), (.099, y, z),
                     .022, .026, "titanium", 24)
            empty("MOUNT_BOLT_%s_%s" % (sy, sz), (0, y, z))
    # Two manifolds and central finned radiator cassette.
    box("Core_Heat_Exchanger", (.192, 0, 0), (.166, .738, .538), "graphite", .013)
    box("Upper_Manifold", (.280, 0, .293), (.142, .865, .065), "copper", .013)
    box("Lower_Manifold", (.280, 0, -.293), (.142, .865, .065), "copper", .013)
    for sy in (-1, 1):
        box("Side_Rail_%s" % sy, (.275, sy * .426, 0),
            (.135, .040, .640), "ceramic", .009)
        box("Coolant_Glass_%s" % sy, (.348, sy * .430, 0),
            (.016, .028, .470), "glass", .004)
        box("Coolant_Light_%s" % sy, (.359, sy * .430, 0),
            (.008, .012, .416), "coolant", .002)
        # A closed visual coolant circuit with elbows outside fan sweep.
        tube("Coolant_Return_%s" % sy, [
            (.285, sy * .379, -.285), (.333, sy * .391, -.242),
            (.356, sy * .391, 0), (.333, sy * .391, .245),
            (.285, sy * .379, .285)], .012, "copper")
    for i in range(21):
        z = -.250 + .025 * i
        box("Radiator_Fin_%02d" % i, (.310, 0, z),
            (.036, .774, .011), "titanium", .003)
        if i % 5 == 0:
            for sy in (-1, 1):
                box("Fin_Thermal_Tag_%02d_%s" % (i, sy),
                    (.334, sy * .340, z), (.004, .041, .009),
                    "amber", .001)
    # Fan duct is anchored to the cassette by four arms outside rotor sweep.
    annulus("Fan_Duct_Outer", FAN_CENTER, .205, .236, .144, "graphite")
    annulus("Fan_Duct_Copper_Lip", (.525, 0, .025), .202, .239,
            .012, "copper")
    for k in range(4):
        a = (k + .5) * math.pi / 2
        y = .246 * math.cos(a)
        z = .025 + .246 * math.sin(a)
        box("Duct_Standoff_%d" % k, (.365, y, z),
            (.103, .033, .033), "ceramic", .007)
    rotor = empty("ANIM_FAN_ROTOR_X", FAN_CENTER)
    rotor["pivot_axis"] = "+X"
    rotor["speed_note"] = "one revolution per 60 frames at 60 fps"
    cylinder("Fan_Hub", (0, 0, 0), FAN_HUB_R, .054,
             "copper", 48, rotor)
    cylinder("Fan_Hub_Cap", (.033, 0, 0), .036, .010,
             "ceramic", 32, rotor)
    for i in range(FAN_BLADES):
        rotor_blade("Fan_Swept_Blade_%02d" % i,
                    2 * math.pi * i / FAN_BLADES, rotor)
    # Front guard is stationary, well forward of the blade's axial envelope.
    annulus("Fan_Front_Guard", (.550, 0, .025), .193, .237,
            .014, "ceramic")
    for k in range(8):
        a = k * math.pi / 4
        cy, cz = .122 * math.cos(a), .025 + .122 * math.sin(a)
        guard = box("Front_Guard_Spoke_%d" % k, (.551, cy, cz),
                    (.010, .208, .010), "graphite", .003)
        guard.rotation_euler[0] = a
    cylinder("Front_Guard_Centre", (.554, 0, .025), .033, .013,
             "ceramic", 24)
    # Wear plates, visible gasket and side status indicators.
    for sy in (-1, 1):
        box("Corner_Armor_Top_%s" % sy,
            (.340, sy * .397, .316), (.115, .112, .038),
            "graphite", .009)
        box("Corner_Armor_Bottom_%s" % sy,
            (.340, sy * .397, -.316), (.115, .112, .038),
            "graphite", .009)
        box("Status_Indicator_%s" % sy, (.379, sy * .410, .265),
            (.007, .060, .016), "coolant", .003)
    rotor.rotation_mode = "XYZ"
    rotor.rotation_euler = (0, 0, 0)
    rotor.keyframe_insert(data_path="rotation_euler", frame=FAN_FRAME_START)
    rotor.rotation_euler[0] = 2 * math.pi * FAN_ROTATIONS
    rotor.keyframe_insert(data_path="rotation_euler", frame=FAN_FRAME_END)
    if rotor.animation_data and rotor.animation_data.action:
        action = rotor.animation_data.action
        if hasattr(action, "fcurves"):
            for fc in action.fcurves:
                for key in fc.keyframe_points:
                    key.interpolation = "LINEAR"
                for modifier in list(fc.modifiers):
                    fc.modifiers.remove(modifier)
                fc.modifiers.new(type="CYCLES")
    bpy.context.scene.frame_start = FAN_FRAME_START
    bpy.context.scene.frame_end = FAN_FRAME_END
    bpy.context.scene.render.fps = 60
    bpy.context.scene.frame_set(FAN_FRAME_START)
    return rotor


def qa(rotor):
    meshes = [o for o in OBJECTS if o.type == "MESH"]
    assert len(meshes) >= 70, len(meshes)
    assert len(MATERIALS) >= 3
    assert len([o for o in meshes if o.name.startswith("Radiator_Fin_")]) == 21
    assert len([o for o in meshes if o.name.startswith("Fan_Swept_Blade_")]) == FAN_BLADES
    assert rotor.parent is None
    assert max(abs(rotor.location[i] - FAN_CENTER[i]) for i in range(3)) < 1e-6
    assert bpy.data.objects.get("MOUNT_SIDE_SOCKET") is not None
    # Conservative analytic collision checks over the continuous fan sweep:
    # rotor outer envelope < inner duct radius, axial gap to rear fins and
    # to front stationary grille. These bounds hold at every rotation angle.
    radial_gap = FAN_INNER_R - FAN_BLADE_R
    rear_axial_gap = (FAN_CENTER[0] - .027) - (.310 + .018)
    front_axial_gap = (.550 - .007) - (FAN_CENTER[0] + .033)
    assert radial_gap >= .025
    assert rear_axial_gap >= .09
    assert front_axial_gap >= .055
    assert FAN_HUB_R < FAN_BLADE_R
    # Check mesh topology on each piece (no loose, boundary or non-manifold
    # edges); Blender bevelled cubes and closed annuli must remain manifold.
    invalid = []
    for obj in meshes:
        bm = bmesh.new()
        bm.from_mesh(obj.data)
        bad = sum(1 for edge in bm.edges if not edge.is_manifold)
        zero = sum(1 for face in bm.faces if face.calc_area() < 1e-10)
        if bad or zero:
            invalid.append((obj.name, bad, zero))
        bm.free()
    assert not invalid, invalid[:10]
    # No foreign objects removed: ownership is restricted to this collection.
    names = [o.name for o in OBJECTS]
    assert len(names) == len(set(names))
    return {
        "model": ID, "status": "PASS",
        "mount_origin_m": list(MOUNT_ORIGIN), "outward_axis": "+X",
        "fan_pivot_m": list(FAN_CENTER), "fan_rotation_axis": "+X",
        "fan_blades": FAN_BLADES, "fan_full_rotation_deg": 360,
        "fan_rotation_frames": [FAN_FRAME_START, FAN_FRAME_END],
        "radial_clearance_m": round(radial_gap, 5),
        "rear_axial_clearance_m": round(rear_axial_gap, 5),
        "front_axial_clearance_m": round(front_axial_gap, 5),
        "meshes": len(meshes),
        "materials": len(MATERIALS),
        "vertices": sum(len(o.data.vertices) for o in meshes),
        "triangles": sum(sum(len(p.vertices) - 2 for p in o.data.polygons)
                         for o in meshes),
        "manifold": "PASS", "collisions": "PASS (analytic continuous fan envelope)",
        "unity_import": "NOT RUN", "unity_playmode": "NOT RUN",
    }


def main():
    args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    parser = argparse.ArgumentParser()
    parser.add_argument("--out", default="//M05-009-export")
    parser.add_argument("--repeat-check", action="store_true")
    opt = parser.parse_args(args)
    out = Path(bpy.path.abspath(opt.out)).resolve()
    out.mkdir(parents=True, exist_ok=True)
    # Repeatability test is in-process, not two fresh Blender processes.
    sentinel = None
    if opt.repeat_check:
        sentinel = bpy.data.objects.new("M05_009_FOREIGN_SENTINEL", None)
        bpy.context.scene.collection.objects.link(sentinel)
    rotor = build()
    report = qa(rotor)
    if opt.repeat_check:
        before = (len(bpy.data.meshes), len(bpy.data.actions),
                  len(bpy.data.curves), len(bpy.data.materials))
        rotor = build()
        report = qa(rotor)
        after = (len(bpy.data.meshes), len(bpy.data.actions),
                 len(bpy.data.curves), len(bpy.data.materials))
        assert before == after, ("Blender data leak", before, after)
        assert bpy.data.objects.get(sentinel.name) is sentinel
        report["repeatability"] = "PASS (two builds, foreign object preserved)"
        bpy.data.objects.remove(sentinel, do_unlink=True)
    (out / "M05-009_QA.json").write_text(json.dumps(report, indent=2) + "\n")
    # Only own objects are exported. Blender is run in a clean background
    # scene by CI, but generator itself preserves foreign scene objects.
    previous_selection = list(bpy.context.selected_objects)
    previous_active = bpy.context.view_layer.objects.active
    try:
        bpy.ops.object.select_all(action="DESELECT")
        for obj in OBJECTS:
            obj.select_set(True)
        bpy.context.view_layer.objects.active = rotor
        bpy.ops.export_scene.gltf(filepath=str(out / "M05-009_side_liquid_radiator.glb"),
                                  export_format="GLB", use_selection=True,
                                  export_animations=True)
        bpy.ops.export_scene.fbx(filepath=str(out / "M05-009_side_liquid_radiator.fbx"),
                                 use_selection=True, add_leaf_bones=False,
                                 axis_forward="-Z", axis_up="Y",
                                 bake_anim=True)
    finally:
        bpy.ops.object.select_all(action="DESELECT")
        for obj in previous_selection:
            if bpy.data.objects.get(obj.name) is obj:
                obj.select_set(True)
        if previous_active and bpy.data.objects.get(previous_active.name) is previous_active:
            bpy.context.view_layer.objects.active = previous_active
    bpy.ops.wm.save_as_mainfile(filepath=str(out / "M05-009_side_liquid_radiator.blend"))
    print("M05-009 QA PASS", json.dumps(report, sort_keys=True))


if __name__ == "__main__":
    main()
