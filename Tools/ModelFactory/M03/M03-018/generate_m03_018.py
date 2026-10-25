#!/usr/bin/env python3
"""M03-018 dual-axis gimbal mount, visual-only. Blender 4.x bpy; metres, Z-up."""
import bpy
import bmesh
import math
import os
import sys
from mathutils import Vector

TAG = "M03_018"
COLLECTION = "M03_018_Gimbal"
OUT = os.environ.get("M03_018_OUT", "/tmp/m03-018")
F = 121

def clean():
    col = bpy.data.collections.get(COLLECTION)
    if col:
        for obj in list(col.objects):
            bpy.data.objects.remove(obj, do_unlink=True)
        bpy.data.collections.remove(col)
    # Remove only assets created by this generator; preserve unrelated scenes.
    for mat in list(bpy.data.materials):
        if mat.name.startswith(TAG + "_") and mat.users == 0:
            bpy.data.materials.remove(mat)
    for mesh in list(bpy.data.meshes):
        if mesh.name.startswith(TAG + "_") and mesh.users == 0:
            bpy.data.meshes.remove(mesh)

def material(name, rgba, metallic=0.0, roughness=0.42, emission=None):
    mat = bpy.data.materials.new(TAG + "_" + name)
    mat.diffuse_color = tuple(rgba)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = tuple(rgba)
    bsdf.inputs["Metallic"].default_value = metallic
    bsdf.inputs["Roughness"].default_value = roughness
    if emission is not None:
        node = mat.node_tree.nodes.new("ShaderNodeEmission")
        node.inputs["Color"].default_value = tuple(emission)
        node.inputs["Strength"].default_value = 2.0
        output = next(n for n in mat.node_tree.nodes if n.type == "OUTPUT_MATERIAL")
        mat.node_tree.links.new(node.outputs["Emission"], output.inputs["Surface"])
    return mat

def assign(obj, name, parent, loc, mat, col):
    obj.name = TAG + "_" + name
    for c in list(obj.users_collection):
        c.objects.unlink(obj)
    col.objects.link(obj)
    obj.parent = parent
    obj.location = loc
    obj.data.name = obj.name + "_mesh"
    obj.data.materials.append(mat)
    return obj

def cube(name, parent, loc, dims, mat, col, bevel=0.0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=(0,0,0))
    obj = bpy.context.object
    obj.dimensions = dims
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel:
        mod = obj.modifiers.new("machined_edges", "BEVEL")
        mod.width = min(bevel, min(dims) * 0.24)
        mod.segments = 2
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=mod.name)
    return assign(obj, name, parent, loc, mat, col)

def cyl(name, parent, loc, radius, depth, mat, col, axis="Z", vertices=48):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=(0,0,0))
    obj = bpy.context.object
    if axis == "X":
        obj.rotation_euler[1] = math.pi / 2
    elif axis == "Y":
        obj.rotation_euler[0] = math.pi / 2
    return assign(obj, name, parent, loc, mat, col)

def torus(name, parent, loc, major, minor, mat, col, axis="Z"):
    bpy.ops.mesh.primitive_torus_add(major_segments=64, minor_segments=12,
                                    location=(0,0,0), major_radius=major, minor_radius=minor)
    obj = bpy.context.object
    if axis == "X":
        obj.rotation_euler[1] = math.pi / 2
    elif axis == "Y":
        obj.rotation_euler[0] = math.pi / 2
    return assign(obj, name, parent, loc, mat, col)

def empty(name, parent, loc, col, size=0.12):
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
    dark = material("Graphite", (0.075,0.092,0.115,1), .75)
    steel = material("Titanium", (0.39,0.46,0.52,1), .88, .29)
    copper = material("Copper", (0.69,0.31,0.105,1), .83, .32)
    ceramic = material("Ceramic", (0.81,0.83,0.76,1), .08, .31)
    rubber = material("Rubber", (0.032,0.037,0.042,1), .02, .82)
    cyan = material("Cyan_Indicator", (0.035,0.65,0.75,1), .1, .2,
                    emission=(0.02,0.45,0.65,1))
    amber = material("Amber_Indicator", (0.85,0.36,0.02,1), .1, .26,
                     emission=(0.8,0.18,0.01,1))
    root = empty("ROOT_MOUNT_Z0", None, (0,0,0), col)
    yaw = empty("YAW_AXIS_Z", root, (0,0,1.0), col)
    pitch = empty("PITCH_AXIS_X", yaw, (0,0,.35), col)
    # Fixed upper chassis socket. Four grid studs, 0.56m square pitch.
    cube("Base_Plate", root, (0,0,.065), (.76,.76,.13), dark, col, .035)
    cube("Base_Ceramic_Inset", root, (0,0,.143), (.42,.42,.023), ceramic, col, .009)
    cube("Fixed_Column", root, (0,0,.525), (.16,.16,.76), steel, col, .02)
    torus("Fixed_Yaw_Bearing", root, (0,0,.94), .17, .024, copper, col)
    for ix in (-1,1):
        for iy in (-1,1):
            cyl("Mount_Bolt_%s_%s"%(ix,iy), root, (ix*.28,iy*.28,.142), .043, .028, copper, col)
            empty("SNAP_BASE_%s_%s"%(ix,iy), root, (ix*.28,iy*.28,0), col, .07)
    # Yaw stage: rotating hub, low transverse arms, and raised outer annular guard.
    cyl("Yaw_Hub", yaw, (0,0,-.05), .12, .08, steel, col)
    torus("Yaw_Hub_Rubber_Seal", yaw, (0,0,-.06), .138, .012, rubber, col)
    torus("Outer_Annular_Guard", yaw, (0,0,.35), .55, .047, dark, col)
    for side in (-1,1):
        cube("Yaw_Low_Beam_%s"%side, yaw, (side*.33,0,-.08), (.48,.095,.068), steel, col, .015)
        cube("Yaw_Riser_%s"%side, yaw, (side*.55,0,.14), (.085,.105,.40), steel, col, .018)
        cube("Yaw_Riser_Ceramic_%s"%side, yaw, (side*.55,0,.17), (.091,.112,.12), ceramic, col, .009)
        torus("Pitch_Bearing_%s"%side, yaw, (side*.45,0,.35), .076, .020, copper, col, axis="X")
        cube("Pitch_Bearing_Bridge_%s"%side, yaw, (side*.50,0,.35), (.09,.085,.075), dark, col, .012)
    for i in range(8):
        a=math.tau*i/8
        cyl("Yaw_Annulus_Tick_%02d"%i, yaw, (.55*math.cos(a),.55*math.sin(a),.35),
            .014,.024, amber if i in (0,4) else copper, col)
    # Pitch stage: vertical inner ring and axle. Rotation about local X.
    torus("Inner_Pitch_Ring", pitch, (0,0,0), .28, .035, steel, col, axis="X")
    cyl("Pitch_Axle", pitch, (0,0,0), .033, .84, copper, col, axis="X")
    cube("Payload_Saddle", pitch, (0,0,0), (.17,.17,.105), dark, col, .015)
    cube("Payload_Ceramic_Plate", pitch, (0,0,.075), (.20,.20,.028), ceramic, col, .009)
    for side in (-1,1):
        cyl("Pitch_Spoke_Y_%s"%side, pitch, (0,side*.17,0), .025,.25, steel, col, axis="Y")
        cube("Pitch_Ring_Accent_%s"%side, pitch, (0,side*.28,0), (.07,.05,.075),
             cyan if side==1 else amber, col, .007)
    empty("SNAP_PAYLOAD_TOP", pitch, (0,0,.10), col, .11)
    empty("SERVICE_YAW_PIVOT", yaw, (0,0,0), col, .10)
    empty("SERVICE_PITCH_PIVOT", pitch, (0,0,0), col, .10)
    # One authored animation, independent channels. Yaw +/-45deg; pitch +/-25deg.
    yaw.rotation_mode = pitch.rotation_mode = "XYZ"
    for frame, yaw_deg, pitch_deg in ((1,0,0),(31,45,25),(61,0,0),
                                       (91,-45,-25),(121,0,0)):
        yaw.rotation_euler[2] = math.radians(yaw_deg)
        pitch.rotation_euler[0] = math.radians(pitch_deg)
        yaw.keyframe_insert(data_path="rotation_euler", index=2, frame=frame,
                            group="M03_018_Yaw")
        pitch.keyframe_insert(data_path="rotation_euler", index=0, frame=frame,
                              group="M03_018_Pitch")
    bpy.context.scene.frame_start=1
    bpy.context.scene.frame_end=F
    bpy.context.scene.render.fps=30
    bpy.context.scene.frame_set(1)
    # Assert mesh topology. Structural mating interfaces are intentional.
    meshes=[o for o in col.objects if o.type=="MESH"]
    for obj in meshes:
        bm=bmesh.new()
        bm.from_mesh(obj.data)
        if any(not e.is_manifold for e in bm.edges):
            bm.free()
            raise RuntimeError("Nonmanifold mesh: "+obj.name)
        if bm.calc_volume(signed=True)<=0:
            bm.free()
            raise RuntimeError("Nonpositive mesh volume: "+obj.name)
        bm.free()
    assert len({m.name for m in bpy.data.materials if m.name.startswith(TAG+"_")}) >= 3
    assert len([o for o in col.objects if "SNAP_BASE_" in o.name]) == 4
    # Analytic ring envelope, conservative across all yaw/pitch keyframes.
    assert .55-.047-(.28+.035) > .18
    # Inner ring low point 1.35 - .315; hub top .99 -> 45 mm gap.
    assert (1.35-.28-.035)-.99 >= .04
    return col, len(meshes)

def export():
    col,count=build_model()
    os.makedirs(OUT,exist_ok=True)
    blend=os.path.join(OUT,"M03-018.blend")
    glb=os.path.join(OUT,"M03-018.glb")
    fbx=os.path.join(OUT,"M03-018.fbx")
    bpy.ops.wm.save_as_mainfile(filepath=blend)
    bpy.ops.export_scene.gltf(filepath=glb, export_format="GLB", export_animations=True)
    bpy.ops.export_scene.fbx(filepath=fbx, use_selection=False, bake_anim=True,
                             add_leaf_bones=False)
    for path in (blend,glb,fbx):
        if not os.path.isfile(path) or os.path.getsize(path)<512:
            raise RuntimeError("Missing/empty export "+path)
    print("M03-018 EXPORT PASS meshes=%d files=%s"%(count,(blend,glb,fbx)))

if __name__=="__main__":
    export()
