#!/usr/bin/env python3
"""M03-019 battery anti-fragment enclosure; Blender 4.x; metres, Z up.

A purely visual, isolated asset. Two roof leaves rotate around parallel Y axes.
Do not use this as a gameplay collider or battery-system implementation.
"""
import bpy
import math
import os
from pathlib import Path

TAG = "M03_019"
COLLECTION = "M03_019_BatteryShrapnelGuard"
OUT = Path(os.environ.get("M03_019_OUT", "/tmp/m03-019-output"))


def clean():
    col = bpy.data.collections.get(COLLECTION)
    if col:
        for obj in list(col.objects):
            bpy.data.objects.remove(obj, do_unlink=True)
        bpy.data.collections.remove(col)
    # Orphan meshes first, then orphan materials. Never touch foreign collections.
    for mesh in list(bpy.data.meshes):
        if mesh.name.startswith(TAG + "_") and mesh.users == 0:
            bpy.data.meshes.remove(mesh)
    for mat in list(bpy.data.materials):
        if mat.name.startswith(TAG + "_") and mat.users == 0:
            bpy.data.materials.remove(mat)


def material(name, color, metallic, roughness, emission=None):
    mat = bpy.data.materials.new(TAG + "_" + name)
    mat.diffuse_color = (*color, 1.0)
    mat.use_nodes = True
    p = mat.node_tree.nodes.get("Principled BSDF")
    p.inputs["Base Color"].default_value = (*color, 1.0)
    p.inputs["Metallic"].default_value = metallic
    p.inputs["Roughness"].default_value = roughness
    if emission:
        # Emission is an accent, never the sole visible material.
        p.inputs["Emission Color"].default_value = (*emission, 1.0)
        p.inputs["Emission Strength"].default_value = 0.8
    return mat


def move_to_collection(obj, col, name, parent, loc, mat=None):
    obj.name = TAG + "_" + name
    for existing in tuple(obj.users_collection):
        existing.objects.unlink(obj)
    col.objects.link(obj)
    obj.parent = parent
    obj.location = loc
    if obj.type == "MESH":
        obj.data.name = TAG + "_" + name + "_mesh"
        obj.data.materials.append(mat)
    return obj


def cube(name, col, parent, loc, size, mat, bevel=0.004):
    bpy.ops.mesh.primitive_cube_add(size=1, location=(0, 0, 0))
    obj = bpy.context.object
    obj.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel:
        mod = obj.modifiers.new("EdgeRadius", "BEVEL")
        mod.width = min(bevel, min(size) * 0.22)
        mod.segments = 2
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=mod.name)
    return move_to_collection(obj, col, name, parent, loc, mat)


def cylinder(name, col, parent, loc, radius, length, mat, axis="Y"):
    bpy.ops.mesh.primitive_cylinder_add(vertices=32, radius=radius, depth=length,
                                        location=(0, 0, 0))
    obj = bpy.context.object
    if axis == "Y":
        obj.rotation_euler[0] = math.pi / 2
    elif axis == "X":
        obj.rotation_euler[1] = math.pi / 2
    return move_to_collection(obj, col, name, parent, loc, mat)


def empty(name, col, parent, loc, size=0.09):
    obj = bpy.data.objects.new(TAG + "_" + name, None)
    col.objects.link(obj)
    obj.empty_display_type = "ARROWS"
    obj.empty_display_size = size
    obj.parent = parent
    obj.location = loc
    return obj


def build_model():
    clean()
    col = bpy.data.collections.new(COLLECTION)
    bpy.context.scene.collection.children.link(col)

    graphite = material("Graphite", (.075, .09, .11), .72, .38)
    titanium = material("Titanium", (.40, .47, .52), .86, .27)
    ceramic = material("Ceramic", (.81, .82, .75), .05, .39)
    copper = material("Copper", (.67, .31, .12), .81, .29)
    rubber = material("Elastomer", (.033, .043, .05), .02, .81)
    amber = material("Amber", (.88, .37, .025), .16, .31, (.42, .11, .01))
    green = material("LockedGreen", (.055, .64, .18), .15, .3, (.015, .22, .02))

    root = empty("ROOT_MOUNT_Z0", col, None, (0, 0, 0), .13)
    # Four underside attachment markers, +Z normal, symmetric 0.88 x 0.52 m grid.
    for sx in (-1, 1):
        for sy in (-1, 1):
            empty("SNAP_BOTTOM_%s_%s" % (sx, sy), col, root,
                  (sx * .44, sy * .26, 0), .065)
            cube("RubberFoot_%s_%s" % (sx, sy), col, root,
                 (sx * .44, sy * .26, .038), (.12, .12, .038), rubber)
            cube("MountPlate_%s_%s" % (sx, sy), col, root,
                 (sx * .44, sy * .26, .064), (.125, .125, .012), copper, .002)

    # Four posts and eight horizontal cross-members. Members terminate before
    # posts (9-18 mm gaps) so there are no accidental overlapping closed solids.
    for sx in (-1, 1):
        for sy in (-1, 1):
            cube("Post_%s_%s" % (sx, sy), col, root,
                 (sx * .55, sy * .36, .37), (.065, .065, .68), graphite, .011)
    for z, label in ((.12, "LOW"), (.655, "HIGH")):
        for sy in (-1, 1):
            cube("Cross_X_%s_%s" % (label, sy), col, root,
                 (0, sy * .36, z), (1.00, .065, .07), titanium, .008)
        for sx in (-1, 1):
            cube("Cross_Y_%s_%s" % (label, sx), col, root,
                 (sx * .55, 0, z), (.065, .60, .07), titanium, .008)

    # Four separate shrapnel shields: not a solid opaque box.
    # They are deliberately recessed from the structural frame.
    for sx in (-1, 1):
        cube("SideBlastPlate_%s" % sx, col, root,
             (sx * .486, 0, .386), (.034, .54, .335), graphite, .012)
        cube("SideCeramic_%s" % sx, col, root,
             (sx * .512, 0, .386), (.010, .40, .20), ceramic, .004)
        for sy in (-1, 1):
            cube("SideCopperRivet_%s_%s" % (sx, sy), col, root,
                 (sx * .523, sy * .215, .386), (.012, .028, .028), copper, .003)
    for sy in (-1, 1):
        cube("EndBlastPlate_%s" % sy, col, root,
             (0, sy * .296, .386), (.90, .034, .335), graphite, .012)
        cube("EndCeramic_%s" % sy, col, root,
             (0, sy * .322, .386), (.64, .010, .20), ceramic, .004)
        for sx in (-1, 1):
            cube("EndAmberMarker_%s_%s" % (sy, sx), col, root,
                 (sx * .355, sy * .333, .39), (.038, .008, .09), amber, .002)

    # A free internal battery envelope locator; NOT a solid battery mesh.
    empty("BATTERY_CLEARANCE_VOLUME_CENTER", col, root, (0, 0, .37), .14)

    # Top anti-fragment doors, two independent Y-axis pivots. Each leaf spans
    # 0.49 m toward the centre; the seam has a real 18 mm gap when closed.
    # Opening angle 105 degrees lifts the leaf away from the battery volume.
    for side, label in ((-1, "L"), (1, "R")):
        hinge = empty("DOOR_%s_HINGE_Y" % label, col, root,
                      (side * .50, 0, .755), .095)
        # Local X points from the hinge toward the centre.
        dx = -side * .246
        cube("DOOR_%s_BlastSteel" % label, col, hinge,
             (dx, 0, 0), (.49, .64, .036), graphite, .011)
        cube("DOOR_%s_CeramicFace" % label, col, hinge,
             (dx, 0, .027), (.425, .56, .010), ceramic, .004)
        cube("DOOR_%s_CopperRib" % label, col, hinge,
             (dx, -.205, .040), (.39, .028, .008), copper, .002)
        cube("DOOR_%s_AmberChevron" % label, col, hinge,
             (dx, .215, .040), (.25, .035, .008), amber, .002)
        # Fixed hinge knuckles are not included in moving door collision checks.
        for sy in (-1, 1):
            cylinder("FixedHinge_%s_%s" % (label, sy), col, root,
                     (side * .513, sy * .235, .755), .017, .09, copper)
        latch = empty("LATCH_%s_AXIS_Z" % label, col, hinge,
                      (dx + (-side * .15), -.24, .062), .045)
        cube("LATCH_%s_Handle" % label, col, latch,
             (0, .04, 0), (.028, .11, .016), titanium, .004)
        cube("LATCH_%s_LockedMark" % label, col, hinge,
             (dx + (-side * .15), -.30, .041), (.033, .012, .009), green, .002)
        cube("LATCH_%s_OpenMark" % label, col, hinge,
             (dx + (-side * .15), -.15, .041), (.033, .012, .009), amber, .002)

        # 1..20 unlock; 26..65 open; 90..125 close; 140 relock.
        for frame, degrees in ((1, 0), (26, 0), (65, side * 105),
                               (90, side * 105), (125, 0), (145, 0)):
            hinge.rotation_euler[1] = math.radians(degrees)
            hinge.keyframe_insert(data_path="rotation_euler", frame=frame)
        for frame, degrees in ((1, 0), (20, -side * 75), (125, -side * 75),
                               (145, 0)):
            latch.rotation_euler[2] = math.radians(degrees)
            latch.keyframe_insert(data_path="rotation_euler", frame=frame)

    scene = bpy.context.scene
    scene.frame_start, scene.frame_end = 1, 145
    scene.frame_set(1)
    bpy.context.view_layer.update()
    return col


def export_model(col, destination=OUT):
    destination.mkdir(parents=True, exist_ok=True)
    bpy.ops.object.select_all(action="DESELECT")
    for obj in col.objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = next(
        obj for obj in col.objects if obj.type == "MESH")
    bpy.ops.wm.save_as_mainfile(filepath=str(destination / "M03-019.blend"))
    bpy.ops.export_scene.gltf(filepath=str(destination / "M03-019.glb"),
                              export_format="GLB", use_selection=True,
                              export_animations=True)
    bpy.ops.export_scene.fbx(filepath=str(destination / "M03-019.fbx"),
                             use_selection=True, bake_anim=True,
                             axis_forward="-Z", axis_up="Y")
    for extension in ("blend", "glb", "fbx"):
        output = destination / ("M03-019." + extension)
        if not output.is_file() or output.stat().st_size < 1000:
            raise RuntimeError("Missing or empty real Blender export: " + str(output))
    print("M03-019 EXPORT PASS", destination)


if __name__ == "__main__":
    export_model(build_model())
