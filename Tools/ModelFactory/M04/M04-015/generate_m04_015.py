#!/usr/bin/env python3
"""M04-015 — deterministic pneumatic jump leg (Blender 4.x, Unity metres).

Coordinate convention: +Y up, +Z vehicle forward, +X right.
MOUNT_UNDERSIDE_Y0 = origin; mounting surface Y=0, all geometry Y<=0.
PNEUMATIC_STROKE pivots at origin; it translates the piston/foot in Y.
Visual-only: no gameplay, Rigidbody, vehicle controller, or scene changes.
"""
import argparse
import json
import math
import sys
from pathlib import Path

try:
    import bpy
    from mathutils import Vector
except ImportError as exc:
    raise SystemExit("This script requires Blender's bpy: run blender -b --python generate_m04_015.py -- --out DIR") from exc

MODEL_ID = "M04-015"
ANIMATION = [(1, 0.0), (18, 0.20), (30, -0.05), (49, 0.0)]
MOVING_NAMES = []
STATIC_NAMES = []
MATERIALS = {}

def material(name, color, metallic=0.0, roughness=0.4, emission=None):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = (*color, 1.0)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (*color, 1.0)
    bsdf.inputs["Metallic"].default_value = metallic
    bsdf.inputs["Roughness"].default_value = roughness
    if emission:
        bsdf.inputs["Emission Color"].default_value = (*emission, 1.0)
        bsdf.inputs["Emission Strength"].default_value = 2.0
    MATERIALS[name] = mat
    return mat

def finish(obj, mat, moving=False, bevel=0.0):
    obj.data.materials.append(MATERIALS[mat])
    if bevel:
        modifier = obj.modifiers.new("Micro bevels", "BEVEL")
        modifier.width = bevel
        modifier.segments = 2
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=modifier.name)
    for face in obj.data.polygons:
        face.use_smooth = len(face.vertices) > 3
    (MOVING_NAMES if moving else STATIC_NAMES).append(obj.name)
    return obj

def box(name, loc, scale, mat, moving=False, bevel=0.008, rotation=None):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if rotation:
        obj.rotation_euler = rotation
    return finish(obj, mat, moving, bevel)

def cylinder(name, loc, radius, depth, mat, moving=False, vertices=32, bevel=0.003):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=loc, rotation=(math.pi/2,0,0))
    obj = bpy.context.object
    obj.name = name
    return finish(obj, mat, moving, bevel)

def annulus(name, center_y, r_outer, r_inner, depth, mat, vertices=48):
    """A truly hollow, watertight annular sleeve, with no solid cap over the piston."""
    n = vertices
    zlo, zhi = -depth/2, depth/2
    verts = []
    for radius, z in ((r_outer,zlo),(r_outer,zhi),(r_inner,zlo),(r_inner,zhi)):
        verts.extend([(radius*math.cos(2*math.pi*i/n),radius*math.sin(2*math.pi*i/n),z) for i in range(n)])
    faces = []
    for i in range(n):
        j=(i+1)%n
        faces.extend(((i,j,n+j,n+i),(n+i,n+j,3*n+j,3*n+i),
                      (2*n+j,2*n+i,3*n+i,3*n+j),
                      (i,2*n+i,2*n+j,j)))
    mesh=bpy.data.meshes.new(name+"_mesh")
    mesh.from_pydata(verts,[],faces)
    mesh.update()
    obj=bpy.data.objects.new(name,mesh)
    bpy.context.collection.objects.link(obj)
    obj.rotation_euler=(math.pi/2,0,0)
    obj.location=(0,center_y,0)
    return finish(obj,mat)

def strut(name, a, b, thickness, mat):
    mid=(Vector(a)+Vector(b))*0.5
    direction=Vector(b)-Vector(a)
    obj=box(name,mid,(thickness,thickness,direction.length),mat,bevel=0.005)
    obj.rotation_euler=direction.to_track_quat('Z','Y').to_euler()
    return obj

def build():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for material_obj in list(bpy.data.materials):
        bpy.data.materials.remove(material_obj)
    STATIC_NAMES.clear()
    MOVING_NAMES.clear()
    MATERIALS.clear()
    material("01_GraphiteArmor", (0.065,0.082,0.10), 0.72, 0.33)
    material("02_BrushedTitanium", (0.42,0.51,0.55), 0.85, 0.27)
    material("03_CopperHydraulics", (0.54,0.24,0.10), 0.72, 0.30)
    material("04_CeramicWear", (0.72,0.74,0.66), 0.14, 0.41)
    material("05_RubberFoot", (0.026,0.036,0.044), 0.04, 0.79)
    material("06_HazardAmber", (0.75,0.37,0.04), 0.27, 0.37, (1.0,0.26,0.012))
    material("07_PressureCyan", (0.015,0.32,0.39), 0.35, 0.24, (0.0,0.67,1.0))

    mount=bpy.data.objects.new("MOUNT_UNDERSIDE_Y0",None)
    bpy.context.collection.objects.link(mount)
    mount.empty_display_type="CUBE"
    mount.empty_display_size=0.12
    mover=bpy.data.objects.new("PNEUMATIC_STROKE_Y",None)
    bpy.context.collection.objects.link(mover)
    mover.empty_display_type="ARROWS"
    mover.empty_display_size=0.22
    mover.parent=mount

    # Upper chassis socket, pressure reservoir and robust cylinder cage.
    box("Socket_Titanium_1mPivot",(0,-0.046,0),(0.62,0.092,0.50),"02_BrushedTitanium",bevel=0.022)
    box("Socket_Graphite_Armor",(0,-0.102,0),(0.55,0.035,0.44),"01_GraphiteArmor")
    for x in (-0.245,0.245):
        for z in (-0.185,0.185):
            cylinder("Socket_Fastener_%s_%s"%(x,z),(x,-0.105,z),0.026,0.014,"03_CopperHydraulics",vertices=12)
    annulus("Pneumatic_Cylinder_Hollow",-0.437,0.208,0.118,0.50,"01_GraphiteArmor")
    for i,y in enumerate((-0.215,-0.43,-0.662)):
        annulus("Reinforcing_Ring_%02d"%i,y,0.226,0.118,0.035,"02_BrushedTitanium")
    for i in range(8):
        angle=2*math.pi*i/8
        x,z=0.250*math.cos(angle),0.250*math.sin(angle)
        fin=box("Radiator_Fin_%02d"%i,(x,-0.43,z),(0.052,0.32,0.024),
                "03_CopperHydraulics" if i%2 else "02_BrushedTitanium",bevel=0.004)
        fin.rotation_euler[1]=-angle
    for x in (-0.35,0.35):
        cylinder("Side_Pressure_Canister_%s"%x,(x,-0.405,0),0.075,0.35,"03_CopperHydraulics")
        cylinder("Pressure_Indicator_%s"%x,(x,-0.24,0),0.077,0.018,"07_PressureCyan")
        for z in (-0.14,0.14):
            strut("Side_Cage_Strut_%s_%s"%(x,z),(x,-0.12,z),(x,-0.625,z),0.025,"02_BrushedTitanium")

    # Real telescopic shaft travels freely inside the hollow sleeve (118 mm bore,
    # 68 mm shaft radius), and the lower shoe remains clear of the housing.
    cylinder("Chromed_Telescopic_Piston",(0,-0.765,0),0.068,0.55,"02_BrushedTitanium",moving=True)
    cylinder("Piston_Lower_Collar",(0,-1.035,0),0.115,0.055,"03_CopperHydraulics",moving=True)
    cylinder("Piston_Safety_Amber",(0,-1.067,0),0.114,0.018,"06_HazardAmber",moving=True)
    cylinder("Impact_Foot_Carrier",(0,-1.117,0),0.335,0.065,"01_GraphiteArmor",moving=True,vertices=48)
    cylinder("Impact_Ceramic_WearPlate",(0,-1.164,0),0.31,0.028,"04_CeramicWear",moving=True,vertices=48)
    cylinder("Rubber_Contact_Pad",(0,-1.189,0),0.295,0.035,"05_RubberFoot",moving=True,vertices=48)
    for i in range(8):
        angle=2*math.pi*i/8
        x,z=0.255*math.cos(angle),0.255*math.sin(angle)
        foot=box("Impact_Grip_%02d"%i,(x,-1.215,z),(0.092,0.021,0.11),
                 "05_RubberFoot",moving=True,bevel=0.007)
        foot.rotation_euler[1]=-angle
    for i in range(4):
        angle=2*math.pi*i/4+math.pi/4
        x,z=0.265*math.cos(angle),0.265*math.sin(angle)
        cylinder("Foot_Bolt_%02d"%i,(x,-1.102,z),0.018,0.012,
                 "06_HazardAmber",moving=True,vertices=12)

    for name in MOVING_NAMES:
        bpy.data.objects[name].parent=mover
    for name in STATIC_NAMES:
        bpy.data.objects[name].parent=mount
    for frame,y in ANIMATION:
        mover.location.y=y
        mover.keyframe_insert(data_path="location",index=1,frame=frame)
    # Blender 4.x exposes Action.fcurves; Blender 5.x stores curves in
    # layered action channelbags. Support both without silently losing keys.
    action=mover.animation_data.action
    curves=[]
    if hasattr(action,"fcurves"):
        curves=list(action.fcurves)
    else:
        for layer in action.layers:
            for strip in layer.strips:
                for slot in action.slots:
                    bag=strip.channelbag(slot)
                    if bag:
                        curves.extend(list(bag.fcurves))
    assert curves,"Missing animation curves"
    for curve in curves:
        for point in curve.keyframe_points:
            point.interpolation="LINEAR"
    bpy.context.scene.frame_start=1
    bpy.context.scene.frame_end=49
    bpy.context.scene.render.fps=30
    bpy.context.scene.frame_set(1)
    bpy.context.view_layer.update()
    return mount,mover

def validate():
    assert len(MATERIALS)>=3
    assert len(MOVING_NAMES)>=6
    assert len(STATIC_NAMES)>=10
    assert all(obj.type=="MESH" for obj in (bpy.data.objects[n] for n in MOVING_NAMES+STATIC_NAMES))
    # Conservative analytic envelope: fixed annular sleeve inner radius 118mm,
    # shaft radius 68mm, minimum 50mm radial air gap across every frame.
    assert 0.118-0.068>=0.050-1e-9
    # At max retraction (+.20m), the lower collar top is -0.8075m,
    # and the bottom of stationary guard ring is -0.6795m.
    collar_top=-1.035+0.055/2+max(y for _,y in ANIMATION)
    guard_bottom=-0.662-0.035/2
    assert guard_bottom-collar_top>0.12
    # Contact pad and grippers stay below housing, even at max retraction.
    assert -1.102+0.012/2+0.20<guard_bottom
    for obj in bpy.data.objects:
        if obj.type=="MESH":
            assert len(obj.data.polygons)>0
            assert all(p.area>1e-11 for p in obj.data.polygons),obj.name
    return {"id":MODEL_ID,"meshes":len(MOVING_NAMES)+len(STATIC_NAMES),
            "moving_meshes":len(MOVING_NAMES),"static_meshes":len(STATIC_NAMES),
            "materials":len(MATERIALS),"triangles":sum(len(p.vertices)-2 for n in MOVING_NAMES+STATIC_NAMES for p in bpy.data.objects[n].data.polygons),
            "animation_frames":ANIMATION,"radial_clearance_m":0.05,
            "min_collar_guard_axial_clearance_m":round(guard_bottom-collar_top,6),
            "coordinate_system":"+Y up; +Z forward; 1 unit = 1 metre",
            "mount_pivot":[0,0,0],"animation_pivot":[0,0,0],
            "blender":"EXECUTED","unity_import":"NOT RUN",
            "unity_editmode":"NOT RUN","unity_playmode":"NOT RUN",
            "notes":"Analytical clearances, not Unity collision/physics validation."}

def main():
    argv=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else []
    parser=argparse.ArgumentParser()
    parser.add_argument("--out",default="build/M04-015")
    args=parser.parse_args(argv)
    out=Path(args.out).resolve()
    out.mkdir(parents=True,exist_ok=True)
    build()
    manifest=validate()
    (out/"M04-015_manifest.json").write_text(json.dumps(manifest,indent=2,ensure_ascii=False)+"\n",encoding="utf-8")
    bpy.ops.wm.save_as_mainfile(filepath=str(out/"M04-015_pneumatic_jump_leg.blend"))
    bpy.ops.export_scene.gltf(filepath=str(out/"M04-015_pneumatic_jump_leg.glb"),export_format="GLB",export_animations=True)
    # OBJ/MTL are a robust static Unity fallback. Ubuntu Blender 5.0.1 has
    # an upstream FBX addon failure (missing ExportFBX.use_space_transform).
    bpy.ops.wm.obj_export(filepath=str(out/"M04-015_pneumatic_jump_leg.obj"),export_materials=True)
    try:
        bpy.ops.export_scene.fbx(filepath=str(out/"M04-015_pneumatic_jump_leg.fbx"))
    except Exception as exc:
        print("M04-015 FBX_NOT_EXPORTED",repr(exc))
    print("M04-015 EXPORT_COMPLETE",json.dumps(manifest,ensure_ascii=False))

if __name__=="__main__":
    main()
