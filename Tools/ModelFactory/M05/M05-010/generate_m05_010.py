#!/usr/bin/env python3
"""MODEL-05 / M05-010 — High-performance vertical radiator (visual only).

Blender 4.x bpy; 1 Blender unit = 1 metre; Unity Y-up / +Z rear.
Origin is the centre of the underside top-mount plate at (0,0,0).
Fixed heat exchanger: 30 independent thin vertical lamellae, two coolant
risers, dual headers and a front caged turbine. Rotor pivots around local Z.
No gameplay systems, scripts, scenes or ProjectSettings are changed.

Usage:
  blender -b -t 2 --python generate_m05_010.py -- --out /tmp/M05-010
"""
import argparse
import json
import math
from pathlib import Path
import sys

import bpy
from mathutils import Matrix

ID="M05-010"
PREFIX="M05_010_"
COLLECTION="M05-010_VERTICAL_RADIATOR"
ROTOR_CENTER=(0.0,0.78,-0.263)
FIN_COUNT=30
BLADE_COUNT=7
RPM_ANIMATION_FRAMES=(1,31,61)
RING_INNER_RADIUS=.184
BLADE_MAX_RADIUS=math.hypot(.109+.064,.019)
assert BLADE_MAX_RADIUS<RING_INNER_RADIUS

COL=None
MATS={}
MOVING=[]


def clear_own_collection():
    global COL,MATS,MOVING
    MOVING=[]
    MATS={}
    old=bpy.data.collections.get(COLLECTION)
    if old:
        for ob in list(old.objects):
            bpy.data.objects.remove(ob,do_unlink=True)
        bpy.data.collections.remove(old)
    # Materials from a previous invocation are removed only if orphaned.
    for mat in list(bpy.data.materials):
        if mat.name.startswith(PREFIX) and mat.users==0:
            bpy.data.materials.remove(mat)
    COL=bpy.data.collections.new(COLLECTION)
    bpy.context.scene.collection.children.link(COL)


def own(obj,name,material=None):
    obj.name=PREFIX+name
    for collection in list(obj.users_collection):
        collection.objects.unlink(obj)
    COL.objects.link(obj)
    if material:
        obj.data.materials.append(MATS[material])
    return obj


def material(name,color,metallic=0.,roughness=.42,emission=None,strength=0.):
    m=bpy.data.materials.new(PREFIX+name)
    m.diffuse_color=(*color,1)
    m.use_nodes=True
    bsdf=m.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value=(*color,1)
    bsdf.inputs["Metallic"].default_value=metallic
    bsdf.inputs["Roughness"].default_value=roughness
    if emission:
        socket=bsdf.inputs.get("Emission Color") or bsdf.inputs.get("Emission")
        if socket:
            socket.default_value=(*emission,1)
        power=bsdf.inputs.get("Emission Strength")
        if power:
            power.default_value=strength
    MATS[name]=m


def palette():
    material("Graphite",(.055,.068,.083),.72,.34)
    material("Titanium",(.34,.40,.44),.88,.28)
    material("Copper",(.52,.22,.095),.79,.28)
    material("Ceramic",(.72,.76,.72),.12,.48)
    material("Rubber",(.026,.030,.034),.03,.76)
    material("Coolant",(.025,.34,.39),.32,.19,(.01,.72,.86),2.8)
    material("StatusAmber",(.67,.28,.03),.25,.32,(1.,.24,.012),2.0)
    material("BladeAlloy",(.17,.25,.31),.82,.28)


def cube(name,loc,scale,mat,bevel=0.):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc)
    ob=own(bpy.context.object,name,mat)
    ob.dimensions=scale
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    if bevel:
        mod=ob.modifiers.new("Rounded machined edges","BEVEL")
        mod.width=bevel
        mod.segments=2
        bpy.context.view_layer.objects.active=ob
        bpy.ops.object.modifier_apply(modifier=mod.name)
    return ob


def cylinder(name,loc,radius,depth,mat,vertices=32,axis="Z"):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices,radius=radius,depth=depth,location=loc)
    ob=own(bpy.context.object,name,mat)
    if axis=="Y":
        ob.rotation_euler[0]=math.pi/2
    elif axis=="X":
        ob.rotation_euler[1]=math.pi/2
    for polygon in ob.data.polygons:
        polygon.use_smooth=(len(polygon.vertices)>4)
    return ob


def torus(name,loc,major,minor,mat):
    bpy.ops.mesh.primitive_torus_add(major_segments=64,minor_segments=12,
                                     location=loc,major_radius=major,minor_radius=minor)
    ob=own(bpy.context.object,name,mat)
    for face in ob.data.polygons:
        face.use_smooth=True
    return ob


def empty(name,loc):
    ob=bpy.data.objects.new(PREFIX+name,None)
    COL.objects.link(ob)
    ob.location=loc
    ob.empty_display_type="ARROWS"
    ob.empty_display_size=.065
    return ob


def parent_preserve_world(obj,parent):
    world=obj.matrix_world.copy()
    obj.parent=parent
    obj.matrix_world=world
    MOVING.append(obj)


def build():
    clear_own_collection()
    palette()
    # The actual underside of the mounting shoe lies on y=0.
    cube("TopMount_Plate",(0,.042,0),(.72,.084,.35),"Graphite",.012)
    for x in (-.28,.28):
        for z in (-.125,.125):
            cylinder("Mount_Bolt_%s_%s"%(int(x*1000),int(z*1000)),
                     (x,.091,z),.022,.017,"Titanium",16,axis="Y")
    empty("MOUNT_TOP_SOCKET",(0,0,0))
    empty("COOLANT_IN",(-.31,.175,.19))
    empty("COOLANT_OUT",(.31,.175,.19))
    empty("FAN_AXIS",ROTOR_CENTER)

    # Header and two side beams; 30 lamellae do not touch one another.
    cube("Bottom_Header",(0,.175,.085),(.78,.09,.23),"Copper",.012)
    cube("Upper_Header",(0,1.118,.085),(.78,.095,.23),"Copper",.012)
    cube("Top_Ceramic_Cap",(0,1.180,.085),(.76,.034,.24),"Ceramic",.009)
    for side,x in (("L",-.383),("R",.383)):
        cube("Side_Stanchion_"+side,(x,.646,.083),(.034,.89,.17),"Graphite",.006)
        cylinder("Coolant_Riser_"+side,(x*.81,.65,.194),.016,.96,"Coolant",20,axis="Y")
        cylinder("Riser_Valve_"+side,(x*.81,.205,.194),.028,.035,"Titanium",24,axis="Y")
    for i in range(FIN_COUNT):
        x=-.338+i*(.676/(FIN_COUNT-1))
        cube("Lamella_%02d"%(i+1),(x,.651,.065),(.008,.835,.114),
             "Titanium" if i%5 else "Ceramic",.0015)
    # Cross-braced fan mounts reach the stationary heat exchanger.
    for side,x in (("L",-.289),("R",.289)):
        cube("Turbine_Arm_"+side,(x,.78,-.125),(.193,.036,.26),"Graphite",.007)
        cube("Arm_Copper_Inlay_"+side,(x,.78,-.16),(.117,.015,.08),"Copper",.004)

    # Two caged toroidal rings and 12 cross rods surround the rotor. Rings
    # inner radius .184m; swept blade outer radius < .175m.
    torus("Turbine_Ring_Front",(0,.78,-.300),.208,.024,"Graphite")
    torus("Turbine_Ring_Back",(0,.78,-.205),.208,.024,"Graphite")
    for i in range(12):
        angle=2*math.pi*i/12
        x=.208*math.cos(angle)
        y=.78+.208*math.sin(angle)
        cylinder("Cage_Spacer_%02d"%i,(x,y,-.2525),.008,.095,"Titanium",12)
    # Static spindle ends in the moving hub: concentric bearing is an
    # intentional fitted joint, not a clearance requirement.
    cylinder("Fixed_Spindle",(0,.78,-.214),.020,.072,"Titanium",24)
    rotor=empty("ROTOR_PIVOT",ROTOR_CENTER)
    cylinder("Rotor_Hub",ROTOR_CENTER,.052,.038,"Copper",32)
    parent_preserve_world(bpy.context.object,rotor)
    for i in range(BLADE_COUNT):
        a=2*math.pi*i/BLADE_COUNT
        r=.109
        x=r*math.cos(a)
        y=.78+r*math.sin(a)
        blade=cube("Rotor_Blade_%02d"%i,(x,y,-.263),
                   (.128,.038,.015),"BladeAlloy",.005)
        blade.rotation_euler[2]=a
        parent_preserve_world(blade,rotor)
    for i in range(4):
        a=i*math.pi/2
        dot=cylinder("Rotor_Status_%d"%i,
                     (.034*math.cos(a),.78+.034*math.sin(a),-.286),
                     .007,.004,"Coolant",12)
        parent_preserve_world(dot,rotor)

    # Auxiliary illuminated temperature bars, safely outside the rotor.
    for side,x in (("L",-.347),("R",.347)):
        cube("Temp_Window_"+side,(x,1.013,-.018),
             (.026,.085,.012),"Rubber",.003)
        cube("Temp_Emitter_"+side,(x,1.013,-.026),
             (.014,.066,.004),"StatusAmber",.001)

    rotor.rotation_mode="XYZ"
    for frame,angle in ((1,0.),(31,math.pi),(61,2*math.pi)):
        rotor.rotation_euler[2]=angle
        rotor.keyframe_insert(data_path="rotation_euler",index=2,frame=frame,
                              group="M05_010_TURBINE")
    for curve in rotor.animation_data.action.fcurves:
        for key in curve.keyframe_points:
            key.interpolation="LINEAR"
    bpy.context.scene.frame_start=1
    bpy.context.scene.frame_end=61
    bpy.context.scene.render.fps=60
    bpy.context.scene.frame_set(1)
    return rotor


def audit():
    objects=list(COL.objects)
    meshes=[o for o in objects if o.type=="MESH"]
    names=[o.name for o in meshes]
    assert len(names)==len(set(names))
    assert len([n for n in names if "Lamella_" in n])==FIN_COUNT
    assert len([n for n in names if "Rotor_Blade_" in n])==BLADE_COUNT
    assert len(MATS)>=3
    assert all(len(o.data.materials)>=1 for o in meshes)
    assert all(len(o.data.vertices)>0 for o in meshes)
    for ob in meshes:
        for v in ob.data.vertices:
            assert all(math.isfinite(t) for t in v.co)
    rotor=bpy.data.objects[PREFIX+"ROTOR_PIVOT"]
    assert len([o for o in MOVING if o.parent==rotor])==12
    assert abs(rotor.location[2]-ROTOR_CENTER[2])<1e-6  # Blender float32 transform
    assert BLADE_MAX_RADIUS< RING_INNER_RADIUS-.009
    for f in (1,16,31,46,61):
        bpy.context.scene.frame_set(f)
        for ob in MOVING:
            assert ob.parent==rotor
        assert abs(rotor.location[0])<1e-9
    bpy.context.scene.frame_set(1)
    vertices=sum(len(o.data.vertices) for o in meshes)
    triangles=sum(sum(max(0,len(p.vertices)-2) for p in o.data.polygons) for o in meshes)
    return {
        "id":ID,"geometry_revision":"M05-010_V1_VERTICAL_RADIATOR",
        "mesh_objects":len(meshes),"vertices":vertices,"triangles":triangles,
        "materials":len(MATS),"lamellae":FIN_COUNT,"fan_blades":BLADE_COUNT,
        "animated_meshes":len(MOVING),"fan_pivot_local_m":ROTOR_CENTER,
        "fan_axis":"local +Z","animation_frames":[1,31,61],
        "fan_revolution_degrees":360,
        "fan_inner_ring_radius_m":RING_INNER_RADIUS,
        "blade_max_swept_radius_m":round(BLADE_MAX_RADIUS,6),
        "fan_radial_clearance_m":round(RING_INNER_RADIUS-BLADE_MAX_RADIUS,6),
        "mount":"MOUNT_TOP_SOCKET (0,0,0), local +Y up",
        "blender":"PASS (generator and analytic QA)",
        "unity":"NOT RUN","visual_inspection":"NOT RUN"}


def export(outdir):
    outdir.mkdir(parents=True,exist_ok=True)
    report=audit()
    saved_selection=list(bpy.context.selected_objects)
    saved_active=bpy.context.view_layer.objects.active
    try:
        bpy.ops.object.select_all(action="DESELECT")
        for obj in COL.objects:
            obj.select_set(True)
        bpy.context.view_layer.objects.active=bpy.data.objects[PREFIX+"ROTOR_PIVOT"]
        bpy.ops.export_scene.gltf(filepath=str(outdir/(ID+"_vertical_radiator.glb")),
            export_format="GLB",use_selection=True,export_animations=True)
        bpy.ops.export_scene.fbx(filepath=str(outdir/(ID+"_vertical_radiator.fbx")),
            use_selection=True,add_leaf_bones=False,axis_forward="-Z",
            axis_up="Y",bake_anim=True,bake_anim_use_all_actions=False)
        bpy.ops.wm.save_as_mainfile(filepath=str(outdir/(ID+"_vertical_radiator.blend")))
    finally:
        bpy.ops.object.select_all(action="DESELECT")
        for ob in saved_selection:
            if bpy.data.objects.get(ob.name) is ob:
                ob.select_set(True)
        if saved_active and bpy.data.objects.get(saved_active.name) is saved_active:
            bpy.context.view_layer.objects.active=saved_active
    report["export_glb"]="PASS"
    report["export_fbx"]="PASS"
    report["save_blend"]="PASS"
    (outdir/(ID+"_qa.json")).write_text(json.dumps(report,indent=2)+"\n",encoding="utf-8")
    return report


def main():
    args=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else []
    parser=argparse.ArgumentParser()
    parser.add_argument("--out",default="build/M05-010")
    parser.add_argument("--no-export",action="store_true")
    opt=parser.parse_args(args)
    build()
    if opt.no_export:
        print("M05-010 QA",json.dumps(audit(),sort_keys=True))
    else:
        print("M05-010 EXPORT",json.dumps(export(Path(opt.out)),sort_keys=True))


if __name__=="__main__":
    main()
