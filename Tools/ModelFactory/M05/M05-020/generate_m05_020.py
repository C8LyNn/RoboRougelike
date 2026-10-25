"""M05-020 — Navigation lattice antenna. Blender 4+ / Unity metres.
Visual-only asset. No gameplay navigation or sensor implementation.
Usage: blender -b --python generate_m05_020.py -- /tmp/m05-020
"""
import bpy
import math
import sys
from pathlib import Path
from mathutils import Vector

TAG = "M05_020"
COLLECTION = TAG + "_NavigationLattice"
FRAME_END = 100
MATS = {}
ROOT = None
YAW = None
WINGS = []

def material(label, rgb, metallic=0.0, roughness=0.4, emission=0.0):
    name = TAG + "_" + label
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = (*rgb, 1)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (*rgb, 1)
    bsdf.inputs["Metallic"].default_value = metallic
    bsdf.inputs["Roughness"].default_value = roughness
    if emission:
        color = bsdf.inputs.get("Emission Color") or bsdf.inputs.get("Emission")
        if color:
            color.default_value = (*rgb, 1)
        strength = bsdf.inputs.get("Emission Strength")
        if strength:
            strength.default_value = emission
    MATS[label] = mat
    return mat

def move_to_collection(obj):
    collection = bpy.data.collections[COLLECTION]
    for old in tuple(obj.users_collection):
        old.objects.unlink(obj)
    collection.objects.link(obj)
    if obj.type == "MESH":
        obj.data.name = obj.name + "_Mesh"
    return obj

def attach(obj, parent, position):
    move_to_collection(obj)
    obj.parent = parent
    obj.location = position
    return obj

def empty(name, parent, pos):
    obj = bpy.data.objects.new(TAG + "_" + name, None)
    bpy.data.collections[COLLECTION].objects.link(obj)
    obj.empty_display_type = "SPHERE"
    obj.empty_display_size = 0.055
    obj.parent = parent
    obj.location = pos
    return obj

def cube(name, pos, size, mat, parent, bevel=0.0):
    bpy.ops.mesh.primitive_cube_add(size=1)
    obj = bpy.context.object
    obj.name = TAG + "_" + name
    obj.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    attach(obj, parent, pos)
    obj.data.materials.append(MATS[mat])
    if bevel:
        mod = obj.modifiers.new("Machined edge radius", "BEVEL")
        mod.width = bevel
        mod.segments = 2
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=mod.name)
        mod = obj.modifiers.new("Weighted normals", "WEIGHTED_NORMAL")
        bpy.ops.object.modifier_apply(modifier=mod.name)
    return obj

def cylinder(name, pos, radius, depth, mat, parent, vertices=24):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth)
    obj = bpy.context.object
    obj.name = TAG + "_" + name
    attach(obj, parent, pos)
    obj.data.materials.append(MATS[mat])
    for face in obj.data.polygons:
        face.use_smooth = len(face.vertices) == 4
    return obj

def bar(name, start, end, radius, mat, parent, vertices=12):
    a, b = Vector(start), Vector(end)
    axis = b - a
    if axis.length < 0.0001:
        raise ValueError("Zero-length lattice bar")
    obj = cylinder(name, (a + b) / 2, radius, axis.length, mat, parent, vertices)
    obj.rotation_mode = "QUATERNION"
    obj.rotation_quaternion = Vector((0, 0, 1)).rotation_difference(axis)
    return obj

def torus(name, pos, major, minor, mat, parent):
    bpy.ops.mesh.primitive_torus_add(major_segments=32, minor_segments=8,
        location=(0, 0, 0), major_radius=major, minor_radius=minor)
    obj = bpy.context.object
    obj.name = TAG + "_" + name
    attach(obj, parent, pos)
    obj.data.materials.append(MATS[mat])
    return obj

def clean():
    if COLLECTION in bpy.data.collections:
        old = bpy.data.collections[COLLECTION]
        for obj in tuple(old.objects):
            bpy.data.objects.remove(obj, do_unlink=True)
        bpy.data.collections.remove(old)
    for mesh in tuple(bpy.data.meshes):
        if mesh.name.startswith(TAG + "_") and mesh.users == 0:
            bpy.data.meshes.remove(mesh)
    for mat in tuple(bpy.data.materials):
        if mat.name.startswith(TAG + "_") and mat.users == 0:
            bpy.data.materials.remove(mat)
    bpy.data.collections.new(COLLECTION)
    bpy.context.scene.collection.children.link(bpy.data.collections[COLLECTION])

def build():
    global ROOT, YAW, WINGS
    clean()
    MATS.clear()
    material("Graphite", (0.045, 0.065, 0.087), 0.68, 0.35)
    material("Titanium", (0.39, 0.49, 0.55), 0.85, 0.23)
    material("Ceramic", (0.72, 0.78, 0.76), 0.13, 0.29)
    material("Copper", (0.54, 0.22, 0.075), 0.83, 0.26)
    material("Rubber", (0.022, 0.027, 0.034), 0.04, 0.79)
    material("OpticGlass", (0.022, 0.14, 0.20), 0.3, 0.16)
    material("SignalCyan", (0.03, 0.80, 0.97), 0.13, 0.2, 4.0)
    material("WarningAmber", (1.0, 0.40, 0.035), 0.2, 0.27, 2.5)
    ROOT = empty("MOUNT_TOP_ORIGIN", None, (0, 0, 0))
    # Fixed mount: Z-up; +Y is the visual forward direction.
    cube("mount_plate", (0, 0, 0.055), (0.46, 0.42, 0.11), "Graphite", ROOT, 0.025)
    cube("mount_gasket", (0, 0, 0.121), (0.37, 0.34, 0.023), "Rubber", ROOT, 0.008)
    for x in (-0.17, 0.17):
        for y in (-0.14, 0.14):
            cylinder("mount_bolt", (x, y, 0.114), 0.024, 0.018, "Titanium", ROOT, 12)
    cylinder("pedestal", (0, 0, 0.20), 0.116, 0.16, "Ceramic", ROOT)
    torus("lower_bearing", (0, 0, 0.265), 0.11, 0.022, "Copper", ROOT)
    YAW = empty("YAW_PIVOT_Z", ROOT, (0, 0, 0.28))
    cylinder("yaw_bearing", (0, 0, 0.016), 0.10, 0.068, "Titanium", YAW)
    cylinder("mast_shaft", (0, 0, 0.37), 0.065, 0.70, "Graphite", YAW)
    for z in (0.18, 0.48, 0.71):
        torus("mast_band", (0, 0, z), 0.07, 0.014, "Copper", YAW)
    cube("array_spine", (0, 0, 0.83), (0.35, 0.12, 0.51), "Ceramic", YAW, 0.018)
    cube("optical_panel", (0, -0.069, 0.83), (0.24, 0.015, 0.31), "OpticGlass", YAW, 0.004)
    for i in range(4):
        cube("center_cyan_trace", (-0.085 + i * 0.057, -0.080, 0.83),
             (0.014, 0.008, 0.20), "SignalCyan", YAW, 0.002)
    for x in (-0.13, 0.13):
        cylinder("antenna_status", (x, -0.085, 1.02), 0.016, 0.013, "WarningAmber", YAW, 12)
    # Hinges are separated from the central spine. Each lattice wing is
    # constructed in hinge-local coordinates, so its pivot is exact.
    WINGS = []
    for sign, side in ((-1, "L"), (1, "R")):
        hinge = empty("HINGE_" + side + "_Z", YAW, (sign * 0.26, 0, 0.83))
        WINGS.append(hinge)
        cylinder(side + "_hinge", (0, 0, 0), 0.040, 0.48, "Titanium", hinge)
        for z in (-0.20, 0.20):
            torus(side + "_hinge_collar", (0, 0, z), 0.043, 0.011, "Copper", hinge)
        # Rigid triangular lattice, all members share the hinge transform.
        left, right = sign * 0.045, sign * 0.61
        for z in (-0.19, 0.19):
            bar(side + "_horizontal", (left, 0, z), (right, 0, z),
                0.014, "Titanium", hinge)
        for x in (left, sign * 0.33, right):
            bar(side + "_upright", (x, 0, -0.19), (x, 0, 0.19),
                0.013, "Graphite", hinge)
        bar(side + "_brace_a", (left, 0, -0.19), (sign * 0.33, 0, 0.19),
            0.010, "Copper", hinge)
        bar(side + "_brace_b", (sign * 0.33, 0, 0.19), (right, 0, -0.19),
            0.010, "Copper", hinge)
        for k in range(3):
            x = sign * (0.14 + 0.19 * k)
            cube(side + "_ceramic_louver", (x, -0.026, 0.0),
                 (0.09, 0.023, 0.055), "Ceramic", hinge, 0.008)
            cube(side + "_emitter", (x, -0.041, 0.0),
                 (0.065, 0.009, 0.013), "SignalCyan", hinge, 0.002)
        cube(side + "_tip_flag", (sign * 0.61, -0.022, 0.0),
             (0.048, 0.028, 0.11), "WarningAmber", hinge, 0.006)
        # 65 degree folding toward the rear (-Y); wing x stays outside
        # |x|>=0.26 throughout, avoiding the central body.
        for frame, angle in ((1, 65), (20, 35), (42, 0),
                             (64, 0), (82, 35), (100, 65)):
            hinge.rotation_euler.z = math.radians(sign * -angle)
            hinge.keyframe_insert(data_path="rotation_euler", frame=frame)
    for frame, yaw_deg in ((1, 0), (100, 360)):
        YAW.rotation_euler.z = math.radians(yaw_deg)
        YAW.keyframe_insert(data_path="rotation_euler", frame=frame)
    for obj in (YAW, *WINGS):
        if obj.animation_data and obj.animation_data.action:
            for fc in obj.animation_data.action.fcurves:
                for key in fc.keyframe_points:
                    key.interpolation = "LINEAR"
    empty("NAV_FORWARD_POS_Y", ROOT, (0, 0.36, 0.12))
    empty("NAV_UP_POS_Z", ROOT, (0, 0, 1.52))
    empty("SERVICE_PORT_REAR", ROOT, (0, 0.20, 0.17))
    bpy.context.scene.frame_start = 1
    bpy.context.scene.frame_end = FRAME_END
    bpy.context.scene.render.fps = 30
    bpy.context.scene.frame_set(1)
    return {"meshes": len([o for o in bpy.data.collections[COLLECTION].objects
                           if o.type == "MESH"]),
            "materials": len(MATS), "frames": FRAME_END}

def export_assets(out_dir):
    out = Path(out_dir)
    out.mkdir(parents=True, exist_ok=True)
    bpy.ops.object.select_all(action="DESELECT")
    for obj in bpy.data.collections[COLLECTION].objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = ROOT
    bpy.ops.wm.save_as_mainfile(filepath=str(out / "M05-020.blend"))
    bpy.ops.export_scene.gltf(filepath=str(out / "M05-020.glb"),
        export_format="GLB", use_selection=True, export_animations=True)
    try:
        bpy.ops.export_scene.fbx(filepath=str(out / "M05-020.fbx"),
            use_selection=True, add_leaf_bones=False)
        print("FBX_EXPORT=PASS")
    except Exception as exc:
        print("FBX_EXPORT=NOT_RUN " + repr(exc))
    print("ASSET_EXPORT=PASS " + str(out))

if __name__ == "__main__":
    stats = build()
    print("M05-020_BUILD " + repr(stats))
    args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    if args:
        export_assets(args[0])
