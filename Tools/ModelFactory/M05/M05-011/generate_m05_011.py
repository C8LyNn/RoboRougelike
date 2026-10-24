#!/usr/bin/env python3
"""MODEL-05 / M05-011 — Caged miniature reactor, VISUAL ASSET ONLY.

Blender 4.x Python (bpy), 1 unit = 1 metre. Blender Z is up.
The assembly origin (0,0,0) is the centre of the bottom mounting plane.
The only moving assembly is a 360-degree reactor containment ring, rotating
around LOCAL +Z through (0,0,0.46). It does not touch the luminous fixed core
or the protective cage. The file creates no gameplay, scripts or prefabs.

Usage:
  blender -b -t 2 --python generate_m05_011.py -- --out build/M05-011
"""
import argparse
import json
import math
from pathlib import Path
import sys

import bpy
import bmesh
from mathutils import Quaternion

ID="M05-011"
PREFIX="M05_011_"
COLLECTION="M05-011_CAGED_MINI_REACTOR"
PIVOT=(0.0,0.0,0.46)
CORE_RADIUS=0.125
ROTOR_MIN_RADIUS=0.230
ROTOR_MAX_RADIUS=0.335
CAGE_INNER_RADIUS=0.350
FRAMES=(1,16,31,46,61)
MATS={}
COL=None
ROTOR=None
MOVING=[]
FIXED=[]
MOUNT_NAMES=[]


def clean_own():
    """Remove only the tagged collection and its orphan datablocks."""
    global COL,ROTOR,MATS,MOVING,FIXED,MOUNT_NAMES
    MOVING=[]
    FIXED=[]
    MOUNT_NAMES=[]
    ROTOR=None
    old=bpy.data.collections.get(COLLECTION)
    if old is not None:
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


def mat(name,color,metallic=0.0,roughness=.4,emission=None,power=0.,alpha=1.):
    m=bpy.data.materials.new(PREFIX+name)
    m.use_nodes=True
    m.diffuse_color=(*color,alpha)
    bsdf=m.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value=(*color,alpha)
    bsdf.inputs["Metallic"].default_value=metallic
    bsdf.inputs["Roughness"].default_value=roughness
    if alpha<1:
        bsdf.inputs["Alpha"].default_value=alpha
        if hasattr(m,"blend_method"):
            m.blend_method="BLEND"
        m.use_nodes=True
    if emission is not None:
        socket=bsdf.inputs.get("Emission Color") or bsdf.inputs.get("Emission")
        if socket:
            socket.default_value=(*emission,1)
        strength=bsdf.inputs.get("Emission Strength")
        if strength:
            strength.default_value=power
    MATS[name]=m
    return m


def palette():
    mat("Graphite",(.040,.055,.067),.68,.38)
    mat("Titanium",(.40,.45,.49),.87,.28)
    mat("Copper",(.58,.255,.11),.78,.29)
    mat("Ceramic",(.73,.78,.75),.13,.47)
    mat("Gasket",(.020,.025,.030),.03,.79)
    mat("EnergyCyan",(.018,.34,.49),.23,.19,(.02,.76,1.0),3.2)
    mat("EnergyViolet",(.22,.055,.39),.16,.23,(.42,.11,1.0),2.6)
    mat("StatusAmber",(.76,.30,.025),.15,.34,(1.0,.28,.015),2.1)
    mat("ShieldGlass",(.15,.28,.31),.06,.12,None,0.,.25)


def own(ob,name,material=None,moving=False):
    ob.name=PREFIX+name
    for col in list(ob.users_collection):
        col.objects.unlink(ob)
    COL.objects.link(ob)
    if material is not None:
        ob.data.materials.append(MATS[material])
    if moving:
        # Parent inverse preserves original world coordinates. Blender
        # evaluates the moving mesh about the empty at PIVOT, not (0,0,0).
        ob.parent=ROTOR
        ob.matrix_parent_inverse=ROTOR.matrix_world.inverted()
        MOVING.append(ob)
    else:
        FIXED.append(ob)
    return ob


def bevel(ob,width=.006):
    if width<=0:
        return ob
    bpy.ops.object.select_all(action="DESELECT")
    ob.select_set(True)
    bpy.context.view_layer.objects.active=ob
    modifier=ob.modifiers.new("Machined edge radii","BEVEL")
    modifier.width=width
    modifier.segments=2
    bpy.ops.object.modifier_apply(modifier=modifier.name)
    for polygon in ob.data.polygons:
        polygon.use_smooth=False
    return ob


def cube(name,loc,size,material,edge=.004,moving=False,angle=0.):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc)
    ob=own(bpy.context.object,name,material,moving)
    ob.dimensions=size
    ob.rotation_euler[2]=angle
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    return bevel(ob,edge)


def cylinder(name,loc,radius,depth,material,vertices=32,axis="Z",moving=False):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices,radius=radius,
                                       depth=depth,location=loc)
    ob=own(bpy.context.object,name,material,moving)
    if axis=="X":
        ob.rotation_euler[1]=math.pi/2
    elif axis=="Y":
        ob.rotation_euler[0]=math.pi/2
    for face in ob.data.polygons:
        face.use_smooth=(len(face.vertices)==4)
    return ob


def torus(name,loc,major,minor,material,moving=False):
    bpy.ops.mesh.primitive_torus_add(major_segments=64,minor_segments=10,
        location=loc,major_radius=major,minor_radius=minor)
    ob=own(bpy.context.object,name,material,moving)
    for poly in ob.data.polygons:
        poly.use_smooth=True
    return ob


def sphere(name,loc,radius,material):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=32,ring_count=16,
                                        radius=radius,location=loc)
    ob=own(bpy.context.object,name,material)
    for poly in ob.data.polygons:
        poly.use_smooth=True
    return ob


def marker(name,loc,parent=None):
    ob=bpy.data.objects.new(PREFIX+name,None)
    ob.empty_display_type="SPHERE"
    ob.empty_display_size=.025
    COL.objects.link(ob)
    ob.location=loc
    if parent:
        ob.parent=parent
    MOUNT_NAMES.append(ob.name)
    return ob


def build():
    """Deterministic, rerunnable geometry + keyed containment ring animation."""
    global ROTOR
    clean_own()
    palette()
    scene=bpy.context.scene

    # Bottom mount: the XY plane at Z=0 is the physical connection surface.
    cube("Bottom_Mount_Plate",(0,0,.030),(.58,.58,.060),"Graphite",.025)
    cylinder("Base_Octagonal_Collar",(0,0,.090),.378,.045,
             "Titanium",vertices=8)
    torus("Lower_Cage_Hoop",(0,0,.135),.375,.021,"Copper")
    torus("Upper_Cage_Hoop",(0,0,.795),.375,.023,"Titanium")
    cylinder("Top_Heat_Sink",(0,0,.843),.250,.055,"Graphite",vertices=12)
    torus("Top_Emissive_Indicator",(0,0,.877),.215,.008,"StatusAmber")
    cylinder("Top_Ceramic_Cap",(0,0,.883),.170,.018,"Ceramic",vertices=12)

    # Eight protective posts are outside the complete ring swept volume.
    for k in range(8):
        a=2*math.pi*k/8
        x=.390*math.cos(a)
        y=.390*math.sin(a)
        cube("Cage_Post_%02d"%k,(x,y,.467),
             (.036,.036,.650),"Titanium",.006,angle=a)
        cylinder("Cage_Post_Bolt_L_%02d"%k,(x,y,.160),
                 .024,.016,"Copper",vertices=12)
        cylinder("Cage_Post_Bolt_U_%02d"%k,(x,y,.774),
                 .024,.016,"Copper",vertices=12)

    # Four translucent polycarbonate view panels; metallic posts are visible.
    for k in range(4):
        a=math.pi/2*k
        x=.393*math.cos(a)
        y=.393*math.sin(a)
        cube("Shield_Window_%02d"%k,(x,y,.470),
             (.009,.420,.390),"ShieldGlass",.002,angle=a)

    # Four bolted underside mounting feet and status lamps.
    for sx in (-1,1):
        for sy in (-1,1):
            label=("P" if sx>0 else "N")+("P" if sy>0 else "N")
            cube("Mount_Foot_"+label,(sx*.235,sy*.235,.011),
                 (.087,.087,.022),"Gasket",.006)
            cylinder("Mount_Bolt_"+label,(sx*.235,sy*.235,.064),
                     .021,.014,"Copper",vertices=12)
            marker("MOUNT_FRAME_"+label,(sx*.235,sy*.235,0))
    marker("MOUNT_FRAME_ORIGIN",(0,0,0))

    # Stationary reactor core and four independent lower ceramic isolators.
    cylinder("Core_Support_Stem",(0,0,.294),.071,.225,"Graphite")
    cylinder("Core_Lower_Collar",(0,0,.355),.125,.032,"Copper")
    sphere("Reactor_Luminous_Core",PIVOT,CORE_RADIUS,"EnergyCyan")
    torus("Core_Halo_Lower",(0,0,.320),.105,.013,"EnergyViolet")
    torus("Core_Halo_Upper",(0,0,.602),.100,.011,"EnergyViolet")
    for k in range(4):
        a=2*math.pi*k/4+math.pi/4
        x=.158*math.cos(a)
        y=.158*math.sin(a)
        cylinder("Core_Insulator_%02d"%k,(x,y,.270),.023,.120,
                 "Ceramic",vertices=16)

    # Rotor empty is the sole animated parent; all ring details follow it.
    ROTOR=bpy.data.objects.new(PREFIX+"ROTOR_Z_PIVOT",None)
    ROTOR.empty_display_type="ARROWS"
    ROTOR.empty_display_size=.090
    COL.objects.link(ROTOR)
    ROTOR.location=PIVOT
    bpy.context.view_layer.update()

    torus("Rotor_Copper_Ring",PIVOT,.260,.018,"Copper",moving=True)
    torus("Rotor_Luminous_Track",(0,0,.487),.257,.009,
          "EnergyViolet",moving=True)
    for k in range(12):
        a=2*math.pi*k/12
        x=.297*math.cos(a)
        y=.297*math.sin(a)
        cube("Rotor_Radial_Vane_%02d"%k,(x,y,.460),
             (.053,.037,.050),"Titanium",.005,moving=True,angle=a)
        cube("Rotor_Glow_Index_%02d"%k,(x,y,.494),
             (.029,.021,.009),"EnergyCyan",.002,moving=True,angle=a)
    for k in range(4):
        a=2*math.pi*k/4+math.pi/4
        x=.276*math.cos(a)
        y=.276*math.sin(a)
        cylinder("Rotor_Copper_Rivet_%02d"%k,(x,y,.492),
                 .012,.012,"StatusAmber",vertices=12,moving=True)

    ROTOR.rotation_mode="QUATERNION"
    for frame,angle in zip(FRAMES,(0,math.pi/2,math.pi,3*math.pi/2,2*math.pi)):
        ROTOR.rotation_quaternion=Quaternion((0,0,1),angle)
        ROTOR.keyframe_insert(data_path="rotation_quaternion",frame=frame,
                              group="Containment_Ring_360")
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
    meshes=[ob for ob in COL.objects if ob.type=="MESH"]
    assert len(MATS)>=3 and len(meshes)>50
    assert len(MOVING)==30,(len(MOVING),"rotating components")
    assert len(MOUNT_NAMES)==5
    assert all(ob.parent is ROTOR for ob in MOVING)
    assert tuple(round(v,4) for v in ROTOR.location)==PIVOT
    manifold_checked=0
    for ob in meshes:
        assert ob.data.materials and len(ob.data.vertices)>0
        assert all(math.isfinite(c) for v in ob.data.vertices for c in v.co)
        ob.data.calc_loop_triangles()
        bm=bmesh.new()
        try:
            bm.from_mesh(ob.data)
            assert bm.faces and bm.edges
            assert all(edge.is_manifold for edge in bm.edges), (
                ob.name,"non-manifold geometry")
            volume=bm.calc_volume(signed=True)
            assert volume>1e-9,(ob.name,"inward normals or degenerate volume",volume)
            manifold_checked+=1
        finally:
            bm.free()
    assert manifold_checked==len(meshes)
    assert ROTOR.animation_data and ROTOR.animation_data.action
    # Explicit engineering envelope: full 360-degree sweep of every
    # moving vertex, not only ring centre positions. The core is static.
    samples=0
    min_radius=float("inf")
    max_radius=0.
    min_z=float("inf")
    max_z=-float("inf")
    for frame in range(1,62):
        bpy.context.scene.frame_set(frame)
        bpy.context.view_layer.update()
        for ob in MOVING:
            matrix=ob.matrix_world
            for v in ob.data.vertices:
                world=matrix@v.co
                radius=math.hypot(world.x,world.y)
                min_radius=min(min_radius,radius)
                max_radius=max(max_radius,radius)
                min_z=min(min_z,world.z)
                max_z=max(max_z,world.z)
                samples+=1
    assert min_radius>CORE_RADIUS+.06,("core collision",min_radius)
    assert max_radius<CAGE_INNER_RADIUS-.01,("cage collision",max_radius)
    assert min_z>.39 and max_z<.54,(min_z,max_z)
    assert min_z>.135+.15 and max_z<.795-.15
    bpy.context.scene.frame_set(1)
    bpy.context.view_layer.update()
    vertices=sum(len(ob.data.vertices) for ob in meshes)
    triangles=sum(len(ob.data.loop_triangles) for ob in meshes)
    return dict(id=ID,geometry_revision="M05-011_V1_CAGED_MINI_REACTOR",
        scale="1 Blender unit = 1 metre",mount_origin=[0,0,0],
        ring_pivot=list(PIVOT),axis="local +Z",
        ring_animation_frames=list(FRAMES),ring_rotation_degrees=360,
        mesh_objects=len(meshes),moving_meshes=len(MOVING),
        manifold_meshes=manifold_checked,mesh_topology="PASS",
        vertices=vertices,triangles=triangles,materials=len(MATS),
        mounting_markers=len(MOUNT_NAMES),
        moving_vertex_samples=samples,
        min_rotor_radius_m=round(min_radius,6),
        max_rotor_radius_m=round(max_radius,6),
        core_clearance_m=round(min_radius-CORE_RADIUS,6),
        cage_clearance_m=round(CAGE_INNER_RADIUS-max_radius,6),
        rotation_clearance="PASS (actual Blender evaluated vertices, 61 frames)",
        blender="PASS",unity="NOT RUN",unity_editmode="NOT RUN",
        unity_playmode="NOT RUN",urp_visual_review="NOT RUN")


def export(outdir,report):
    outdir.mkdir(parents=True,exist_ok=True)
    scene=bpy.context.scene
    previous=(scene.frame_current,scene.frame_start,scene.frame_end,
              scene.render.fps)
    selected=tuple(bpy.context.selected_objects)
    active=bpy.context.view_layer.objects.active
    glb=outdir/"M05-011_caged_mini_reactor.glb"
    fbx=outdir/"M05-011_caged_mini_reactor.fbx"
    blend=outdir/"M05-011_caged_mini_reactor.blend"
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
        for file in (glb,fbx,blend):
            assert file.exists() and file.stat().st_size>1024,file
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
        scene.frame_start,scene.frame_end,scene.render.fps=previous[1:]
        scene.frame_set(previous[0])
    (outdir/"M05-011_qa.json").write_text(json.dumps(report,indent=2)+"\n",
                                           encoding="utf-8")
    return report


def main():
    argv=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else []
    parser=argparse.ArgumentParser()
    parser.add_argument("--out",default="build/M05-011")
    args=parser.parse_args(argv)
    report=build()
    report=export(Path(args.out),report)
    print("M05-011 EXPORT PASS",json.dumps(report,sort_keys=True))


if __name__=="__main__":
    main()
