#!/usr/bin/env python3
"""M01-010 — Energetyczny widełkowy projektor (visual only).

Run: blender -b --factory-startup --python-exit-code 1 --python generate_m01_010.py -- --out /tmp/m01-010
Unity scale: 1 Blender unit = 1 metre; +Z forward; +Y up; +X right.
Root MOUNT_FRONT_SOCKET is at the front vehicle connection plane (0,0,0).
Fork hinges rotate around local Y, left outward negative, right positive.
No gameplay/WPN implementation. Blender 4.x compatible.
"""
import argparse
from collections import Counter
import json
import math
from pathlib import Path
import sys

import bpy
from mathutils import Vector

ID = "M01-010"
COLLECTION = "M01-010_ENERGY_FORK_PROJECTOR"
FORK_PIVOT_X = .185
FORK_PIVOT_Z = .330
FORK_YAW_DEG = 21.0
FRAMES = (1, 32, 64)
MATERIALS = {
    "Graphite_Armor": ((.055,.074,.091,1),.78,.35,0),
    "Titanium_Frame": ((.36,.43,.48,1),.76,.27,0),
    "Ceramic_Ivory": ((.79,.84,.80,1),.12,.29,0),
    "Copper_Coil": ((.52,.235,.095,1),.72,.29,0),
    "Black_Gasket": ((.018,.026,.035,1),.10,.77,0),
    "Cyan_Emitter": ((.018,.73,.95,1),.17,.22,4.0),
    "Amber_Status": ((1.0,.32,.035,1),.10,.28,2.0),
}
objects = []
fixed = []
moving = {"L":[], "R":[]}


def material(name):
    rgba,metal,rough,glow=MATERIALS[name]
    m=bpy.data.materials.get(ID+"_"+name) or bpy.data.materials.new(ID+"_"+name)
    m.diffuse_color=rgba
    m.use_nodes=True
    shader=m.node_tree.nodes.get("Principled BSDF")
    shader.inputs["Base Color"].default_value=rgba
    shader.inputs["Metallic"].default_value=metal
    shader.inputs["Roughness"].default_value=rough
    if "Emission Color" in shader.inputs:
        shader.inputs["Emission Color"].default_value=rgba
        shader.inputs["Emission Strength"].default_value=glow
    return m


def reset_model_collection():
    old=bpy.data.collections.get(COLLECTION)
    if old is not None:
        for ob in list(old.objects):
            bpy.data.objects.remove(ob,do_unlink=True)
        bpy.data.collections.remove(old)
    col=bpy.data.collections.new(COLLECTION)
    bpy.context.scene.collection.children.link(col)
    return col


COL = reset_model_collection()


def register(ob, mat=None, group=None):
    for col in tuple(ob.users_collection):
        col.objects.unlink(ob)
    COL.objects.link(ob)
    if mat:
        ob.data.materials.clear()
        ob.data.materials.append(material(mat))
    if ob.type=="MESH":
        objects.append(ob)
        (moving[group] if group else fixed).append(ob)
    return ob


def empty(name, location, parent=None):
    ob=bpy.data.objects.new(name,None)
    COL.objects.link(ob)
    ob.empty_display_type="ARROWS"
    ob.empty_display_size=.08
    ob.parent=parent
    ob.location=location
    return ob


ROOT=empty("MOUNT_FRONT_SOCKET",(0,0,0))


def finish_mesh(ob,name,mat,parent,group=None,bevel=0):
    ob.name=name
    ob.parent=parent
    if bevel:
        mod=ob.modifiers.new("machined_edge_rounding","BEVEL")
        mod.width=bevel
        mod.segments=2
        mod.limit_method="ANGLE"
        bpy.context.view_layer.objects.active=ob
        bpy.ops.object.modifier_apply(modifier=mod.name)
        for face in ob.data.polygons:
            face.use_smooth=True
        mod=ob.modifiers.new("face_weighted_normals","WEIGHTED_NORMAL")
        mod.keep_sharp=True
        bpy.ops.object.modifier_apply(modifier=mod.name)
    return register(ob,mat,group)


def box(name,xyz,dimensions,mat,parent=ROOT,group=None,bevel=.006):
    bpy.ops.mesh.primitive_cube_add(size=1)
    ob=bpy.context.object
    ob.dimensions=dimensions
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    ob.location=xyz
    return finish_mesh(ob,name,mat,parent,group,bevel)


def cylinder(name,xyz,radius,depth,mat,parent=ROOT,group=None,vertices=32):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices,radius=radius,depth=depth)
    ob=bpy.context.object
    ob.location=xyz
    return finish_mesh(ob,name,mat,parent,group,.0025)


def ring(name,xyz,radius,tube,mat,parent=ROOT,group=None):
    bpy.ops.mesh.primitive_torus_add(major_segments=48,minor_segments=12,
                                    location=(0,0,0),major_radius=radius,
                                    minor_radius=tube)
    ob=bpy.context.object
    ob.location=xyz
    return finish_mesh(ob,name,mat,parent,group,0)


def build():
    # Mount: +Z front socket plane at Z=0, with real visual screw pattern.
    cylinder("Front_Mount_Flange",(0,0,.022),.147,.044,"Titanium_Frame")
    cylinder("Front_Mount_Recess",(0,0,.052),.104,.022,"Black_Gasket")
    ring("Front_Mount_Copper_Lip",(0,0,.063),.108,.008,"Copper_Coil")
    for x in (-.108,.108):
        for y in (-.093,.093):
            cylinder("Mount_Bolt_%s_%s"%(int(x*1000),int(y*1000)),
                     (x,y,.048),.012,.012,"Ceramic_Ivory",vertices=12)

    # Wedge-like forward housing with separated top and bottom ceramic skins.
    box("Graphite_Main_Case",(0,0,.190),(.328,.202,.245),"Graphite_Armor",bevel=.023)
    box("Upper_Ceramic_Crown",(0,.111,.185),(.285,.028,.192),"Ceramic_Ivory",bevel=.012)
    box("Lower_Titanium_Skid",(0,-.118,.190),(.298,.026,.220),"Titanium_Frame",bevel=.010)
    for side,label in ((-1,"L"),(1,"R")):
        box("Lateral_Copper_Spine_"+label,(side*.152,-.009,.183),
            (.020,.095,.178),"Copper_Coil",bevel=.007)
        for n in range(3):
            box("Service_Gill_"+label+"_"+str(n),
                (side*.145,.080,.125+n*.055),
                (.027,.014,.020),"Black_Gasket",bevel=.003)
    box("Top_Amber_Indicator",(0,.135,.193),(.061,.008,.065),"Amber_Status",bevel=.003)
    box("Forward_Central_Emitter_Case",(0,0,.321),(.178,.130,.072),
        "Titanium_Frame",bevel=.018)
    cylinder("Forward_Emitter_Lens",(0,0,.375),.071,.028,"Cyan_Emitter")
    ring("Forward_Emitter_Bezel",(0,0,.394),.083,.009,"Copper_Coil")

    # Visible open energy gate; isolated from fork swept volumes.
    ring("Energy_Gate_Outer",(0,0,.515),.091,.009,"Cyan_Emitter")
    ring("Energy_Gate_Inner",(0,0,.519),.057,.004,"Titanium_Frame")
    cylinder("Energy_Gate_Centre",(0,0,.510),.021,.012,"Cyan_Emitter",vertices=24)
    for n in range(8):
        theta=2*math.pi*n/8
        x=.083*math.cos(theta)
        y=.083*math.sin(theta)
        cylinder("Gate_Copper_Stud_"+str(n),(x,y,.519),
                 .008,.012,"Copper_Coil",vertices=12)

    # The fork roots are outside the central energy gate.
    # Fixed hinge covers terminate at Z=.365; moving arms start >=.380.
    for side,label in ((-1,"L"),(1,"R")):
        cylinder("Hinge_Collar_"+label,(side*FORK_PIVOT_X,.009,.332),
                 .062,.061,"Titanium_Frame")
        cylinder("Hinge_Ruby_Pin_"+label,(side*FORK_PIVOT_X,.009,.369),
                 .025,.012,"Amber_Status",vertices=16)

    for side,label in ((-1,"L"),(1,"R")):
        hinge=empty("FORK_HINGE_"+label,
                    (side*FORK_PIVOT_X,.009,FORK_PIVOT_Z),ROOT)
        # Each child is local to its hinge: closed forward +Z.
        box("Fork_Graphite_Beam_"+label,(0,0,.352),(.102,.100,.535),
            "Graphite_Armor",hinge,label,bevel=.017)
        box("Fork_Titanium_Spine_"+label,(0,.058,.352),(.061,.018,.494),
            "Titanium_Frame",hinge,label,bevel=.007)
        box("Fork_Inner_Copper_Conductor_"+label,(-side*.053,.006,.365),
            (.013,.037,.418),"Copper_Coil",hinge,label,bevel=.004)
        box("Fork_Inner_Cyan_Light_"+label,(-side*.063,.012,.371),
            (.008,.018,.335),"Cyan_Emitter",hinge,label,bevel=.002)
        box("Fork_Heel_Slider_"+label,(0,-.007,.145),(.085,.080,.100),
            "Titanium_Frame",hinge,label,bevel=.010)
        for n in range(3):
            box("Fork_Ceramic_Armor_"+label+"_"+str(n),
                (side*.010,.063,.208+n*.140),(.076,.021,.095),
                "Ceramic_Ivory",hinge,label,bevel=.008)
            box("Fork_Outer_Copper_Fin_"+label+"_"+str(n),
                (side*.054,-.027,.211+n*.140),(.018,.044,.043),
                "Copper_Coil",hinge,label,bevel=.005)
        cylinder("Fork_Tip_Ceramic_Nozzle_"+label,(0,0,.641),
                 .049,.062,"Ceramic_Ivory",hinge,label)
        ring("Fork_Tip_Cyan_Ring_"+label,(0,0,.678),.040,.006,
             "Cyan_Emitter",hinge,label)
        cylinder("Fork_Tip_Lens_"+label,(0,0,.688),.022,.012,
                 "Cyan_Emitter",hinge,label,vertices=24)
        for frame,amount in ((1,0.0),(32,side*math.radians(FORK_YAW_DEG)),
                             (64,0.0)):
            hinge.rotation_euler[1]=amount
            hinge.keyframe_insert(data_path="rotation_euler",frame=frame,
                                  group="Fork Deployment")
        hinge.rotation_euler[1]=0

    # Energy gate breathes in place, not into the arms.
    gate=bpy.data.objects["Energy_Gate_Outer"]
    for frame,factor in ((1,1.0),(16,1.055),(32,1.0),
                         (48,1.055),(64,1.0)):
        gate.scale=(factor,)*3
        gate.keyframe_insert(data_path="scale",frame=frame,group="Gate Pulse")
    gate.scale=(1,)*3
    scene=bpy.context.scene
    scene.frame_start,scene.frame_end,scene.render.fps=1,64,60
    scene.frame_set(1)
    return scene


def mesh_health():
    faces=0
    vertices=0
    for ob in objects:
        mesh=ob.data
        vertices+=len(mesh.vertices)
        mesh.calc_loop_triangles()
        faces+=len(mesh.loop_triangles)
        assert len(mesh.polygons)>0,(ob.name,"empty mesh")
        edges=Counter()
        signed=0.0
        for tri in mesh.loop_triangles:
            a,b,c=[mesh.vertices[i].co for i in tri.vertices]
            assert (b-a).cross(c-a).length>1e-10,(ob.name,"degenerate")
            signed+=a.dot(b.cross(c))/6
            v=tri.vertices
            for i,j in ((v[0],v[1]),(v[1],v[2]),(v[2],v[0])):
                edges[tuple(sorted((i,j)))]+=1
        assert all(n==2 for n in edges.values()),(ob.name,"open/nonmanifold",Counter(edges.values()))
        assert signed>1e-9,(ob.name,"inward/zero volume",signed)
        assert len(mesh.materials)==1,(ob.name,"missing material")
    assert len({m.name for ob in objects for m in ob.data.materials})>=3
    return vertices,faces


def world_bounds(ob):
    mat=ob.matrix_world
    coords=[mat@v.co for v in ob.data.vertices]
    return tuple((min(p[i] for p in coords),max(p[i] for p in coords))
                 for i in range(3))


def aabb_gap(a,b):
    gaps=[max(b[i][0]-a[i][1],a[i][0]-b[i][1],0.0) for i in range(3)]
    return math.sqrt(sum(x*x for x in gaps))


def collision_qa(scene):
    # Exclude intentional contacts WITHIN each assembly, but never between
    # a moving fork and the fixed body, or between opposite moving forks.
    checks=0
    minimum=float("inf")
    for frame in range(1,65):
        scene.frame_set(frame)
        bpy.context.view_layer.update()
        static=[world_bounds(o) for o in fixed]
        left=[world_bounds(o) for o in moving["L"]]
        right=[world_bounds(o) for o in moving["R"]]
        for group in (left,right):
            for a in group:
                for b in static:
                    d=aabb_gap(a,b)
                    assert d>1e-6,(frame,"fork intersects fixed",d)
                    checks+=1
                    minimum=min(minimum,d)
        for a in left:
            for b in right:
                d=aabb_gap(a,b)
                assert d>1e-6,(frame,"fork collision",d)
                checks+=1
                minimum=min(minimum,d)
    scene.frame_set(1)
    return checks,minimum


def main():
    args=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else []
    parser=argparse.ArgumentParser()
    parser.add_argument("--out",default="/tmp/m01-010")
    opts=parser.parse_args(args)
    out=Path(opts.out).resolve()
    out.mkdir(parents=True,exist_ok=True)
    scene=build()
    verts,tris=mesh_health()
    checks,clearance=collision_qa(scene)
    assert len(moving["L"])==len(moving["R"])>=10
    assert all(name in bpy.data.objects for name in
               ("MOUNT_FRONT_SOCKET","FORK_HINGE_L","FORK_HINGE_R"))
    assert len(objects)>=40 and verts>1000
    bpy.ops.wm.save_as_mainfile(filepath=str(out/"M01-010_energy_fork_projector.blend"))
    bpy.ops.export_scene.gltf(filepath=str(out/"M01-010_energy_fork_projector_animated.glb"),
                              export_format="GLB",export_animations=True)
    bpy.ops.export_scene.fbx(filepath=str(out/"M01-010_energy_fork_projector_animated.fbx"),
                             axis_forward="-Z",axis_up="Y",add_leaf_bones=False,
                             bake_anim=True)
    # OBJ/MTL are static previews; GLB/FBX carry the rig animation.
    if hasattr(bpy.ops.wm,"obj_export"):
        bpy.ops.wm.obj_export(filepath=str(out/"M01-010_energy_fork_projector.obj"),
                              export_materials=True)
    else:
        bpy.ops.export_scene.obj(filepath=str(out/"M01-010_energy_fork_projector.obj"),
                                 use_materials=True)
    result={
        "id":ID,"model":"Energetyczny widełkowy projektor",
        "status":"BLENDER_EXPORTED","units":"metres","front_axis":"+Z",
        "up_axis":"+Y","mount":"MOUNT_FRONT_SOCKET at origin",
        "fork_hinges":["FORK_HINGE_L","FORK_HINGE_R"],
        "fork_yaw_degrees":FORK_YAW_DEG,
        "frame_range":[1,64],"fps":60,
        "materials":len({m.name for ob in objects for m in ob.data.materials}),
        "mesh_objects":len(objects),"vertices":verts,"triangles":tris,
        "aabb_checks":checks,"sampled_frames":64,
        "minimum_aabb_separation_m":round(clearance,6),
        "mesh_closed_positive_volume":"PASS",
        "fork_vs_fixed_and_opposite_fork_aabb":"PASS",
        "blender_version":bpy.app.version_string,
        "unity_import":"NOT RUN","unity_playmode":"NOT RUN",
        "note":"AABB is a conservative broad-phase; physical runtime collision not tested."
    }
    (out/"M01-010_qa.json").write_text(json.dumps(result,indent=2,ensure_ascii=False)+"\n")
    print("M01-010_RESULT "+json.dumps(result,ensure_ascii=False))


if __name__=="__main__":
    main()
