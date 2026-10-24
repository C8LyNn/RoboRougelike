#!/usr/bin/env python3
"""MODEL-05 / M05-012 — Emergency generator, VISUAL ASSET ONLY.

Blender 4.x bpy. 1 unit = 1 m, +Z up. Bottom mounting plane z=0.
A copper-wound alternator rotates about local X at (0,0,0.43).
Yellow service decals distinguish it from the caged miniature reactor.
No gameplay logic, Unity scenes or ProjectSettings are modified.

Usage:
  blender -b -t 2 --python generate_m05_012.py -- --out build/M05-012
"""
import argparse
import json
import math
from pathlib import Path
import sys

import bpy
import bmesh
from mathutils import Quaternion

ID="M05-012"
PREFIX="M05_012_"
COLLECTION="M05-012_EMERGENCY_GENERATOR"
PIVOT=(0.0,0.0,0.43)
FRAMES=(1,16,31,46,61)
ROTOR_MAX_R=0.220
STATOR_MIN_R=0.235
MATS={}
COL=None
ROTOR=None
MOVING=[]
FIXED=[]
MOUNTS=[]


def clean_own():
    """Idempotent and scoped: never delete user objects outside our collection."""
    global COL,ROTOR,MATS,MOVING,FIXED,MOUNTS
    MOVING=[]
    FIXED=[]
    MOUNTS=[]
    ROTOR=None
    old=bpy.data.collections.get(COLLECTION)
    if old:
        old_meshes=[ob.data for ob in old.objects if ob.type=="MESH"]
        for ob in list(old.objects):
            bpy.data.objects.remove(ob,do_unlink=True)
        for mesh in old_meshes:
            if mesh.users==0:
                bpy.data.meshes.remove(mesh)
        bpy.data.collections.remove(old)
    for mat in list(bpy.data.materials):
        if mat.name.startswith(PREFIX) and mat.users==0:
            bpy.data.materials.remove(mat)
    COL=bpy.data.collections.new(COLLECTION)
    bpy.context.scene.collection.children.link(COL)
    MATS={}


def material(name,color,metal=0.,rough=.4,emission=None,power=0.):
    m=bpy.data.materials.new(PREFIX+name)
    m.use_nodes=True
    m.diffuse_color=(*color,1.)
    p=m.node_tree.nodes.get("Principled BSDF")
    p.inputs["Base Color"].default_value=(*color,1.)
    p.inputs["Metallic"].default_value=metal
    p.inputs["Roughness"].default_value=rough
    if emission:
        socket=p.inputs.get("Emission Color") or p.inputs.get("Emission")
        if socket:
            socket.default_value=(*emission,1.)
        strength=p.inputs.get("Emission Strength")
        if strength:
            strength.default_value=power
    MATS[name]=m


def palette():
    material("Graphite",(.035,.045,.056),.63,.37)
    material("Titanium",(.43,.48,.52),.83,.27)
    material("Copper",(.63,.27,.105),.82,.28)
    material("Ceramic",(.77,.79,.72),.09,.51)
    material("Gasket",(.025,.027,.030),.02,.84)
    material("ServiceYellow",(.94,.56,.018),.18,.39)
    material("StatusGreen",(.04,.48,.13),.14,.24,(.07,.95,.19),2.5)
    material("StatusRed",(.53,.045,.025),.10,.32,(1.,.06,.02),2.0)
    material("CoolantBlue",(.045,.24,.39),.50,.28)


def own(ob,name,mat=None,moving=False):
    ob.name=PREFIX+name
    for c in list(ob.users_collection):
        c.objects.unlink(ob)
    COL.objects.link(ob)
    if mat:
        ob.data.materials.append(MATS[mat])
    if moving:
        ob.parent=ROTOR
        ob.matrix_parent_inverse=ROTOR.matrix_world.inverted()
        MOVING.append(ob)
    else:
        FIXED.append(ob)
    return ob


def bevel(ob,width=.004):
    if width<=0:
        return ob
    bpy.ops.object.select_all(action="DESELECT")
    ob.select_set(True)
    bpy.context.view_layer.objects.active=ob
    mod=ob.modifiers.new("Rounded machined edges","BEVEL")
    mod.width=width
    mod.segments=2
    bpy.ops.object.modifier_apply(modifier=mod.name)
    for face in ob.data.polygons:
        face.use_smooth=False
    return ob


def box(name,loc,size,mat,edge=.004,moving=False,rot_x=0.,rot_y=0.,rot_z=0.):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc)
    ob=own(bpy.context.object,name,mat,moving)
    ob.dimensions=size
    ob.rotation_euler=(rot_x,rot_y,rot_z)
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    return bevel(ob,edge)


def cyl(name,loc,radius,depth,mat,vertices=32,axis="Z",moving=False):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices,radius=radius,
                                       depth=depth,location=loc)
    ob=own(bpy.context.object,name,mat,moving)
    if axis=="X":
        ob.rotation_euler[1]=math.pi/2
    elif axis=="Y":
        ob.rotation_euler[0]=math.pi/2
    for face in ob.data.polygons:
        face.use_smooth=(len(face.vertices)==4)
    return ob


def torus(name,loc,major,minor,mat,axis="X",moving=False):
    bpy.ops.mesh.primitive_torus_add(major_segments=48,minor_segments=8,
        location=loc,major_radius=major,minor_radius=minor)
    ob=own(bpy.context.object,name,mat,moving)
    if axis=="X":
        ob.rotation_euler[1]=math.pi/2
    for face in ob.data.polygons:
        face.use_smooth=True
    return ob


def mount(name,loc):
    ob=bpy.data.objects.new(PREFIX+name,None)
    ob.empty_display_type="SPHERE"
    ob.empty_display_size=.027
    COL.objects.link(ob)
    ob.location=loc
    MOUNTS.append(ob)
    return ob


def build():
    global ROTOR
    clean_own()
    palette()
    scene=bpy.context.scene
    # Four shock-isolated feet support a full width base with the real
    # mounting origin at z=0, not the visual centroid.
    box("Main_Base",(0,0,.061),(1.02,.78,.122),"Graphite",.025)
    box("Base_Ceramic_Deck",(0,0,.125),(.86,.64,.018),"Ceramic",.009)
    for sx in (-1,1):
        for sy in (-1,1):
            code=("P" if sx>0 else "N")+("P" if sy>0 else "N")
            box("Mount_Foot_"+code,(sx*.422,sy*.302,.014),
                (.120,.104,.028),"Gasket",.010)
            cyl("Mount_Bolt_"+code,(sx*.422,sy*.302,.125),
                .024,.013,"Copper",vertices=12)
            mount("MOUNT_FRAME_"+code,(sx*.422,sy*.302,0))
    mount("MOUNT_FRAME_ORIGIN",(0,0,0))

    # Protective welded cage. Posts are outside the rotor X envelope.
    for sx in (-1,1):
        for sy in (-1,1):
            code=("P" if sx>0 else "N")+("P" if sy>0 else "N")
            box("Cage_Post_"+code,(sx*.453,sy*.335,.448),
                (.034,.034,.637),"Titanium",.006)
            box("Cage_Corner_Brace_"+code,(sx*.453,sy*.335,.766),
                (.084,.080,.026),"Graphite",.007)
    for sy in (-1,1):
        box("Top_Rail_"+("P" if sy>0 else "N"),
            (0,sy*.335,.772),(.90,.036,.033),"Titanium",.006)
    for sx in (-1,1):
        box("Top_Crossbar_"+("P" if sx>0 else "N"),
            (sx*.453,0,.772),(.036,.67,.033),"Titanium",.006)
    box("Lift_Handle_Bridge",(0,0,.869),(.42,.056,.040),"Titanium",.013)
    for sx in (-1,1):
        box("Lift_Handle_Leg_"+("P" if sx>0 else "N"),
            (sx*.190,0,.821),(.040,.055,.092),"Titanium",.009)

    # Stator around horizontal X-axis, copper induction collars, and
    # eight protective long bars. Inner stator radius is .235 m.
    for sign in (-1,1):
        code="L" if sign<0 else "R"
        torus("Stator_Induction_Ring_"+code,
              (sign*.197,0,PIVOT[2]),.256,.021,"Copper")
        torus("Outer_Cage_Ring_"+code,
              (sign*.290,0,PIVOT[2]),.271,.016,"Graphite")
        torus("Shaft_Bearing_"+code,
              (sign*.267,0,PIVOT[2]),.068,.023,"Titanium")
        cyl("Bearing_Housing_Bolt_"+code,
            (sign*.267,.310,PIVOT[2]),.019,.029,
            "ServiceYellow",vertices=12,axis="X")
    for k in range(8):
        a=2*math.pi*k/8
        y=.286*math.cos(a)
        z=PIVOT[2]+.286*math.sin(a)
        cyl("Longitudinal_Guard_Bar_%02d"%k,
            (0,y,z),.012,.57,"Titanium",vertices=12,axis="X")

    # Fixed front grille is 9mm past the moving shaft end; it can cross
    # the centreline without intersecting the rotor.
    for sign in (-1,1):
        code="L" if sign<0 else "R"
        for k in range(4):
            a=k*math.pi/4
            box("Safety_Grille_"+code+"_%02d"%k,
                (sign*.325,0,PIVOT[2]),(.012,.490,.018),
                "Ceramic",.004,rot_x=a)
        cyl("Grille_Hub_"+code,(sign*.330,0,PIVOT[2]),
            .048,.014,"Graphite",vertices=24,axis="X")

    # Alternator rotor: copper windings, eight steel cooling paddles,
    # amber/yellow witness markings. No moving part intersects the stator.
    ROTOR=bpy.data.objects.new(PREFIX+"ALTERNATOR_ROTOR_X_PIVOT",None)
    ROTOR.empty_display_type="ARROWS"
    ROTOR.empty_display_size=.08
    COL.objects.link(ROTOR)
    ROTOR.location=PIVOT
    bpy.context.view_layer.update()
    cyl("Rotor_Shaft",PIVOT,.038,.610,"Titanium",axis="X",moving=True)
    cyl("Rotor_Laminated_Core",(0,0,PIVOT[2]),
        .121,.260,"Graphite",vertices=32,axis="X",moving=True)
    for sign in (-1,1):
        code="L" if sign<0 else "R"
        cyl("Rotor_End_Cap_"+code,(sign*.148,0,PIVOT[2]),
            .146,.030,"Copper",vertices=32,axis="X",moving=True)
        torus("Rotor_Winding_Band_"+code,(sign*.109,0,PIVOT[2]),
              .157,.014,"Copper",moving=True)
    for k in range(8):
        a=2*math.pi*k/8
        y=.172*math.cos(a)
        z=PIVOT[2]+.172*math.sin(a)
        box("Rotor_Cooling_Paddle_%02d"%k,
            (.010,y,z),(.084,.042,.030),"Titanium",
            .004,moving=True,rot_x=a)
        box("Rotor_Yellow_Index_%02d"%k,
            (.063,y,z),(.009,.026,.022),"ServiceYellow",
            .002,moving=True,rot_x=a)

    # Distinct industrial emergency-power visual language: yellow
    # service plate, diagonal bars, red fault LED, green ready LED.
    box("Control_Box",(.335,-.336,.442),
        (.200,.052,.205),"Graphite",.014)
    box("Control_Face",(.335,-.367,.445),
        (.167,.010,.167),"Ceramic",.005)
    for k in range(4):
        box("Yellow_Service_Stripe_%02d"%k,
            (.285+k*.031,-.375,.449),
            (.013,.006,.127),"ServiceYellow",.001,rot_y=-.32)
    cyl("Green_Ready_LED",(.294,-.377,.508),.015,.012,
        "StatusGreen",vertices=16,axis="Y")
    cyl("Red_Fault_LED",(.364,-.377,.508),.015,.012,
        "StatusRed",vertices=16,axis="Y")
    box("Reset_Switch",(.335,-.383,.383),
        (.063,.022,.030),"Gasket",.004)
    for sign in (-1,1):
        code="L" if sign<0 else "R"
        box("Coolant_Pipe_"+code,(sign*.390,.300,.315),
            (.040,.033,.255),"CoolantBlue",.009)
        cyl("Pipe_End_Collar_"+code,(sign*.390,.300,.443),
            .032,.022,"Copper",vertices=16)
    box("Warning_Cap",(0,.332,.576),
        (.140,.023,.067),"ServiceYellow",.010)
    box("Service_Latch",(0,.346,.579),
        (.060,.013,.022),"Gasket",.003)

    ROTOR.rotation_mode="QUATERNION"
    for frame,angle in zip(FRAMES,(0,math.pi/2,math.pi,3*math.pi/2,2*math.pi)):
        ROTOR.rotation_quaternion=Quaternion((1,0,0),angle)
        ROTOR.keyframe_insert(data_path="rotation_quaternion",
                              frame=frame,group="Alternator_Shaft_360")
    if ROTOR.animation_data and ROTOR.animation_data.action:
        for fc in ROTOR.animation_data.action.fcurves:
            for key in fc.keyframe_points:
                key.interpolation="LINEAR"
    scene.frame_start=1
    scene.frame_end=61
    scene.render.fps=60
    scene.frame_set(1)
    bpy.context.view_layer.update()
    return validate()


def validate():
    meshes=[o for o in COL.objects if o.type=="MESH"]
    assert len(MATS)==9
    assert len(MOUNTS)==5
    assert len(MOVING)==37,(len(MOVING),"moving meshes")
    assert all(o.parent is ROTOR for o in MOVING)
    assert tuple(round(v,4) for v in ROTOR.location)==PIVOT
    manifold=0
    for o in meshes:
        assert o.data.materials and o.data.vertices
        assert all(math.isfinite(float(c)) for v in o.data.vertices for c in v.co)
        o.data.calc_loop_triangles()
        bm=bmesh.new()
        try:
            bm.from_mesh(o.data)
            assert bm.faces and all(e.is_manifold for e in bm.edges),o.name
            assert bm.calc_volume(signed=True)>1e-10,("normals",o.name)
            manifold+=1
        finally:
            bm.free()
    # Evaluate actual Blender world vertices across all 61 animated frames.
    # Stator inner radius .235 m; rotor is required to stay within .220.
    # Bearings are annular (inner radius .045) around a .038 shaft.
    samples=0
    max_r=0.
    min_x=float("inf")
    max_x=-float("inf")
    for frame in range(1,62):
        bpy.context.scene.frame_set(frame)
        bpy.context.view_layer.update()
        for ob in MOVING:
            mat=ob.matrix_world
            for v in ob.data.vertices:
                p=mat@v.co
                r=math.hypot(p.y-PIVOT[1],p.z-PIVOT[2])
                max_r=max(max_r,r)
                min_x=min(min_x,p.x)
                max_x=max(max_x,p.x)
                samples+=1
    assert max_r<ROTOR_MAX_R,(max_r,"rotor hits stator")
    assert min_x>=-.306 and max_x<=.306,(min_x,max_x)
    assert STATOR_MIN_R-max_r>.015
    assert len(meshes)>70
    bpy.context.scene.frame_set(1)
    bpy.context.view_layer.update()
    return dict(id=ID,geometry_revision="M05-012_V1_EMERGENCY_GENERATOR",
        scale="1 Blender unit = 1 metre",mount_origin=[0,0,0],
        rotor_pivot=list(PIVOT),axis="local +X",
        rotor_animation_frames=list(FRAMES),rotor_rotation_degrees=360,
        mesh_objects=len(meshes),moving_meshes=len(MOVING),
        manifold_meshes=manifold,vertices=sum(len(o.data.vertices) for o in meshes),
        triangles=sum(len(o.data.loop_triangles) for o in meshes),
        materials=len(MATS),mounting_markers=len(MOUNTS),
        moving_vertex_samples=samples,
        max_rotor_radius_m=round(max_r,6),
        stator_radial_clearance_m=round(STATOR_MIN_R-max_r,6),
        rotor_x_min_m=round(min_x,6),rotor_x_max_m=round(max_x,6),
        radial_sweep="PASS (actual Blender evaluated vertices, 61 frames)",
        blender="PASS",unity="NOT RUN",unity_editmode="NOT RUN",
        unity_playmode="NOT RUN",urp_visual_review="NOT RUN")


def export(outdir,report):
    outdir.mkdir(parents=True,exist_ok=True)
    scene=bpy.context.scene
    prev=(scene.frame_current,scene.frame_start,scene.frame_end,scene.render.fps)
    selected=tuple(bpy.context.selected_objects)
    active=bpy.context.view_layer.objects.active
    stem="M05-012_emergency_generator"
    glb=outdir/(stem+".glb")
    fbx=outdir/(stem+".fbx")
    blend=outdir/(stem+".blend")
    try:
        bpy.ops.object.select_all(action="DESELECT")
        for ob in COL.objects:
            ob.select_set(True)
        bpy.context.view_layer.objects.active=ROTOR
        bpy.ops.export_scene.gltf(filepath=str(glb),export_format="GLB",
                                  use_selection=True,export_animations=True)
        bpy.ops.export_scene.fbx(filepath=str(fbx),use_selection=True,
            axis_forward="-Z",axis_up="Y",add_leaf_bones=False,
            bake_anim=True,bake_anim_use_all_actions=False)
        bpy.ops.wm.save_as_mainfile(filepath=str(blend))
        for f in (glb,fbx,blend):
            assert f.exists() and f.stat().st_size>1024,f
        report.update(export_glb="PASS",export_fbx="PASS",save_blend="PASS",
                      glb_bytes=glb.stat().st_size,
                      fbx_bytes=fbx.stat().st_size,
                      blend_bytes=blend.stat().st_size)
    finally:
        bpy.ops.object.select_all(action="DESELECT")
        for ob in selected:
            if bpy.data.objects.get(ob.name) is ob:
                ob.select_set(True)
        if active and bpy.data.objects.get(active.name) is active:
            bpy.context.view_layer.objects.active=active
        scene.frame_start,scene.frame_end,scene.render.fps=prev[1:]
        scene.frame_set(prev[0])
    (outdir/"M05-012_qa.json").write_text(
        json.dumps(report,indent=2)+"\n",encoding="utf-8")
    return report


def main():
    argv=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else []
    parser=argparse.ArgumentParser()
    parser.add_argument("--out",default="build/M05-012")
    args=parser.parse_args(argv)
    report=build()
    export(Path(args.out),report)
    print("M05-012 EXPORT PASS",json.dumps(report,sort_keys=True))


if __name__=="__main__":
    main()
