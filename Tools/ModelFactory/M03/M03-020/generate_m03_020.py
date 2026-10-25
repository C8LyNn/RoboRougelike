#!/usr/bin/env python3
"""MODEL-03 / M03-020 — modular open cargo basket, Blender 4.x, metres, Z up.

Visual-only mesh generator. Rear-facing attachment plane is Y=0; cargo extends
toward +Y. Root pivot (0,0,0). No gameplay, colliders or Unity scene edits.
Run: blender -b --factory-startup --python-exit-code 1 --python generate_m03_020.py
"""
import bpy
import math
import os
from pathlib import Path

TAG = "M03_020"
COLLECTION = "M03_020_ModularCargoBasket"
OUT = Path(os.environ.get("M03_020_OUT", "/tmp/m03-020-output"))


def cleanup():
    old = bpy.data.collections.get(COLLECTION)
    if old:
        for obj in list(old.objects):
            bpy.data.objects.remove(obj, do_unlink=True)
        bpy.data.collections.remove(old)
    # Remove only unreferenced data belonging to this generator.
    for mesh in list(bpy.data.meshes):
        if mesh.name.startswith(TAG + "_") and mesh.users == 0:
            bpy.data.meshes.remove(mesh)
    for mat in list(bpy.data.materials):
        if mat.name.startswith(TAG + "_") and mat.users == 0:
            bpy.data.materials.remove(mat)


def material(label, rgb, metal, rough, emission=None):
    mat = bpy.data.materials.new(TAG + "_" + label)
    mat.diffuse_color = (*rgb, 1.0)
    mat.use_nodes = True
    shader = mat.node_tree.nodes.get("Principled BSDF")
    shader.inputs["Base Color"].default_value = (*rgb, 1.0)
    shader.inputs["Metallic"].default_value = metal
    shader.inputs["Roughness"].default_value = rough
    if emission is not None:
        shader.inputs["Emission Color"].default_value = (*emission, 1.0)
        shader.inputs["Emission Strength"].default_value = 0.4
    return mat


def attach(obj, name, col, root, xyz, mat=None):
    obj.name = TAG + "_" + name
    for old in tuple(obj.users_collection):
        old.objects.unlink(obj)
    col.objects.link(obj)
    obj.parent = root
    obj.location = xyz
    if obj.type == "MESH":
        obj.data.name = TAG + "_" + name + "_mesh"
        obj.data.materials.append(mat)
    return obj


def box(name, col, root, xyz, size, mat, bevel=0.003):
    bpy.ops.mesh.primitive_cube_add(size=1, location=(0, 0, 0))
    obj = bpy.context.object
    obj.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel:
        mod = obj.modifiers.new("SoftMachinedEdges", "BEVEL")
        mod.width = min(bevel, min(size) * 0.18)
        mod.segments = 2
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=mod.name)
        weighted = obj.modifiers.new("WeightedNormals", "WEIGHTED_NORMAL")
        bpy.ops.object.modifier_apply(modifier=weighted.name)
    return attach(obj, name, col, root, xyz, mat)


def rod(name, col, root, xyz, radius, length, mat, axis="Z"):
    bpy.ops.mesh.primitive_cylinder_add(vertices=16, radius=radius,
                                        depth=length, location=(0, 0, 0))
    obj = bpy.context.object
    if axis == "X":
        obj.rotation_euler[1] = math.pi / 2
    elif axis == "Y":
        obj.rotation_euler[0] = math.pi / 2
    for poly in obj.data.polygons:
        poly.use_smooth = len(poly.vertices) == 4
    return attach(obj, name, col, root, xyz, mat)


def marker(name, col, parent, xyz, size=0.07, rear_normal=False):
    obj = bpy.data.objects.new(TAG + "_" + name, None)
    col.objects.link(obj)
    obj.parent = parent
    obj.location = xyz
    obj.empty_display_type = "ARROWS"
    obj.empty_display_size = size
    if rear_normal:
        # Local +Z points toward the vehicle, i.e. -Y in Blender.
        obj.rotation_euler[0] = math.pi / 2
    return obj


def build_model():
    cleanup()
    col = bpy.data.collections.new(COLLECTION)
    bpy.context.scene.collection.children.link(col)

    graphite = material("GraphiteSteel", (.075, .095, .12), .78, .35)
    titanium = material("TitaniumRods", (.40, .47, .52), .82, .31)
    ceramic = material("CeramicWearSlats", (.82, .80, .70), .08, .45)
    copper = material("CopperMounts", (.64, .29, .12), .80, .30)
    rubber = material("RubberIsolators", (.025, .035, .045), .02, .86)
    amber = material("AmberLoadMarkings", (.90, .36, .035), .12, .33,
                     (.26, .075, .006))

    root = marker("ROOT_REAR_MOUNT", col, None, (0, 0, 0), .14)
    # Basket frame: 1.12 m across X, 0.70 m between rear/front faces,
    # floor at -0.25 m and open top at +0.30 m. Four unobstructed SNAP
    # anchors on the rear Y=0 attachment plane, oriented outward -Y.
    for sx in (-1, 1):
        for sz in (-1, 1):
            x, z = sx * .45, sz * .20
            marker("SNAP_REAR_%s_%s" % (sx, sz), col, root,
                   (x, 0, z), .060, rear_normal=True)
            box("Isolator_%s_%s" % (sx, sz), col, root,
                (x, .018, z), (.095, .025, .090), rubber, .003)
            box("MountPad_%s_%s" % (sx, sz), col, root,
                (x, .043, z), (.108, .018, .105), copper, .003)
            rod("Fastener_%s_%s" % (sx, sz), col, root,
                (x, .055, z), .013, .012, titanium, "Y")

    # Structural corner uprights and two continuous rectangular rims.
    # Member ends terminate against adjacent uprights; touching/welded
    # connections are intentional, not arbitrary solid intersections.
    for sx in (-1, 1):
        for sy in (-1, 1):
            x = sx * .535
            y = .15 if sy < 0 else .85
            box("CornerPost_%s_%s" % (sx, sy), col, root,
                (x, y, .025), (.050, .050, .55), graphite, .007)

    for label, z in (("LOW", -.235), ("TOP", .285)):
        for sy, y in (("REAR", .15), ("FRONT", .85)):
            box("RimX_%s_%s" % (label, sy), col, root,
                (0, y, z), (1.020, .045, .045), graphite, .005)
        for sx, x in (("L", -.535), ("R", .535)):
            box("RimY_%s_%s" % (label, sx), col, root,
                (x, .50, z), (.045, .650, .045), graphite, .005)

    # Welded rod cage, open apertures rather than opaque side plates.
    # Bar centres are at least 0.110 m apart; bar diameter is 0.018 m.
    # They terminate at inner faces of the upper and lower rims.
    for face, y in (("REAR", .15), ("FRONT", .85)):
        for i in range(9):
            x = -.44 + i * .11
            rod("WallVertical_%s_%02d" % (face, i), col, root,
                (x, y, .025), .009, .472, titanium)
        for z, level in ((-.065, "LOW"), (.115, "HIGH")):
            rod("WallHorizontal_%s_%s" % (face, level), col, root,
                (0, y, z), .008, .990, titanium, "X")

    for side, x in (("L", -.535), ("R", .535)):
        for i in range(5):
            y = .27 + i * .115
            rod("SideVertical_%s_%02d" % (side, i), col, root,
                (x, y, .025), .009, .472, titanium)
        for z, level in ((-.065, "LOW"), (.115, "HIGH")):
            rod("SideHorizontal_%s_%s" % (side, level), col, root,
                (x, .50, z), .008, .630, titanium, "Y")

    # Floor is a visibly slatted open grid: 9 structural rods + 5
    # removable ceramic anti-abrasion runners. Deliberate 25 mm+ slots.
    for i in range(9):
        x = -.44 + i * .11
        rod("FloorBar_%02d" % i, col, root,
            (x, .50, -.225), .011, .650, titanium, "Y")
    for i in range(5):
        x = -.40 + i * .20
        box("FloorCeramicRunner_%02d" % i, col, root,
            (x, .50, -.205), (.052, .600, .018), ceramic, .003)

    # Two external diagonal stiffeners sit outside the rod-cage volume.
    # Their oblique appearance is expressed with rotated, separate braces.
    for sx in (-1, 1):
        obj = box("OuterBrace_%s" % sx, col, root,
                  (sx * .577, .50, .025), (.020, .74, .025),
                  copper, .003)
        obj.rotation_euler[0] = math.radians(34 * sx)

    # Non-emissive colour accents remain legible without bloom.
    for sx in (-1, 1):
        box("RearAmberMarker_%s" % sx, col, root,
            (sx * .345, .120, .285), (.080, .008, .028), amber, .002)

    bpy.context.scene.frame_start = 1
    bpy.context.scene.frame_end = 1  # Catalog explicitly requires no motion.
    bpy.context.scene.frame_set(1)
    bpy.context.view_layer.update()
    return col


def export_model(col, destination=OUT):
    destination.mkdir(parents=True, exist_ok=True)
    bpy.ops.object.select_all(action="DESELECT")
    for obj in col.objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = next(
        obj for obj in col.objects if obj.type == "MESH")
    bpy.ops.wm.save_as_mainfile(filepath=str(destination / "M03-020.blend"))
    bpy.ops.export_scene.gltf(filepath=str(destination / "M03-020.glb"),
                              export_format="GLB", use_selection=True,
                              export_animations=False)
    bpy.ops.export_scene.fbx(filepath=str(destination / "M03-020.fbx"),
                             use_selection=True, bake_anim=False,
                             axis_forward="-Z", axis_up="Y")
    for ext in ("blend", "glb", "fbx"):
        file = destination / ("M03-020." + ext)
        if not file.is_file() or file.stat().st_size < 1000:
            raise RuntimeError("Missing or empty real export: " + str(file))
    print("M03-020 REAL BLENDER EXPORT PASS", destination)


if __name__ == "__main__":
    export_model(build_model())
