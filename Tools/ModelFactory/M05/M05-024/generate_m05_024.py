#!/usr/bin/env python3
"""M05-024: visual-only cryogenic cooling system for Unity 6 (1 Blender unit = 1 m).

Run: blender -b -t 2 --python generate_m05_024.py -- --out /tmp/m05-024 --self-test
This generator touches only its own named collection and M05_024_ materials/actions.
"""
import argparse
import json
import math
import os
import sys
from pathlib import Path

import bpy
import bmesh
from mathutils import Vector

PREFIX = "M05_024_"
COLLECTION = PREFIX + "CRYOGENIC_COOLER"
FRAME_COUNT = 61
PISTON_X = (-0.35, 0.35)
BORE_RADIUS = 0.108
ROD_RADIUS = 0.076
SHELL_TOP = 1.10
SHELL_BOTTOM = 0.39
MATERIALS = {
    "graphite": ((0.055, 0.075, 0.093, 1), 0.78, 0.34, None),
    "titanium": ((0.43, 0.52, 0.57, 1), 0.83, 0.24, None),
    "ceramic": ((0.77, 0.85, 0.88, 1), 0.12, 0.35, None),
    "copper": ((0.49, 0.24, 0.10, 1), 0.83, 0.28, None),
    "rubber": ((0.018, 0.025, 0.030, 1), 0.04, 0.83, None),
    "ice_blue": ((0.045, 0.45, 0.73, 1), 0.28, 0.22, ((0.03, 0.22, 0.52, 1), 0.42)),
    "cyan_led": ((0.035, 0.82, 1.0, 1), 0.03, 0.2, ((0.02, 0.62, 1, 1), 3.0)),
    "amber_led": ((1.0, 0.40, 0.04, 1), 0.04, 0.3, ((1.0, 0.22, 0.01, 1), 2.1)),
}

def clear_own():
    col = bpy.data.collections.get(COLLECTION)
    if col:
        for obj in list(col.all_objects):
            bpy.data.objects.remove(obj, do_unlink=True)
        bpy.data.collections.remove(col)
    for mat in list(bpy.data.materials):
        if mat.name.startswith(PREFIX) and mat.users == 0:
            bpy.data.materials.remove(mat)
    for act in list(bpy.data.actions):
        if act.name.startswith(PREFIX) and act.users == 0:
            bpy.data.actions.remove(act)

def make_collection():
    col = bpy.data.collections.new(COLLECTION)
    bpy.context.scene.collection.children.link(col)
    return col

def own(obj, col, name, mat=None, parent=None):
    obj.name = PREFIX + name
    for c in list(obj.users_collection):
        c.objects.unlink(obj)
    col.objects.link(obj)
    if mat:
        obj.data.materials.clear()
        obj.data.materials.append(mat)
    if parent:
        world = obj.matrix_world.copy()
        obj.parent = parent
        obj.matrix_world = world
    return obj

def material(name, spec):
    rgba, metal, rough, glow = spec
    m = bpy.data.materials.new(PREFIX + name)
    m.diffuse_color = rgba
    m.use_nodes = True
    p = m.node_tree.nodes.get("Principled BSDF")
    p.inputs["Base Color"].default_value = rgba
    p.inputs["Metallic"].default_value = metal
    p.inputs["Roughness"].default_value = rough
    if glow:
        p.inputs["Emission Color"].default_value = glow[0]
        p.inputs["Emission Strength"].default_value = glow[1]
    return m

def empty(col, name, xyz, parent=None):
    obj = bpy.data.objects.new(PREFIX + name, None)
    col.objects.link(obj)
    obj.location = xyz
    if parent:
        world = obj.matrix_world.copy()
        obj.parent = parent
        obj.matrix_world = world
    obj.empty_display_type = 'PLAIN_AXES'
    obj.empty_display_size = 0.085
    return obj

def bevel_cube(col, mats, name, xyz, dims, key, bevel=0.015, parent=None):
    bpy.ops.mesh.primitive_cube_add(size=1, location=xyz)
    o = own(bpy.context.object, col, name, mats[key], parent)
    o.dimensions = dims
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel:
        mod = o.modifiers.new("edge_softening", "BEVEL")
        mod.width = bevel
        mod.segments = 2
        bpy.context.view_layer.objects.active = o
        bpy.ops.object.modifier_apply(modifier=mod.name)
    for face in o.data.polygons:
        face.use_smooth = False
    return o

def cylinder(col, mats, name, xyz, radius, depth, key, vertices=32, parent=None):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=xyz)
    o = own(bpy.context.object, col, name, mats[key], parent)
    for p in o.data.polygons:
        p.use_smooth = len(p.vertices) == 4
    return o

def torus(col, mats, name, xyz, major, minor, key, parent=None):
    bpy.ops.mesh.primitive_torus_add(major_segments=40, minor_segments=10,
        location=xyz, major_radius=major, minor_radius=minor)
    return own(bpy.context.object, col, name, mats[key], parent)

def annulus(col, mats, name, x, y, z0, z1, outer, inner, key, n=40):
    verts = []
    for r,z in ((outer,z0),(outer,z1),(inner,z0),(inner,z1)):
        verts.extend([(x+r*math.cos(2*math.pi*i/n),
                       y+r*math.sin(2*math.pi*i/n),z) for i in range(n)])
    faces = []
    for i in range(n):
        j=(i+1)%n
        faces.extend(((i,j,n+j,n+i),
                      (2*n+j,2*n+i,3*n+i,3*n+j),
                      (n+i,n+j,3*n+j,3*n+i),
                      (j,i,2*n+i,2*n+j)))
    mesh=bpy.data.meshes.new(PREFIX+name+"_mesh")
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    o=bpy.data.objects.new(PREFIX+name,mesh)
    col.objects.link(o)
    mesh.materials.append(mats[key])
    for f in mesh.polygons:
        f.use_smooth=True
    return o

def pipe(col, mats, name, points, radius, key):
    curve=bpy.data.curves.new(PREFIX+name+"_curve",'CURVE')
    curve.dimensions='3D'
    curve.resolution_u=16
    curve.bevel_depth=radius
    curve.bevel_resolution=4
    curve.use_fill_caps=True
    spl=curve.splines.new('BEZIER')
    spl.bezier_points.add(len(points)-1)
    for bp, xyz in zip(spl.bezier_points,points):
        bp.co=xyz
        bp.handle_left_type='AUTO'
        bp.handle_right_type='AUTO'
    o=bpy.data.objects.new(PREFIX+name,curve)
    col.objects.link(o)
    bpy.context.view_layer.objects.active=o
    o.select_set(True)
    bpy.ops.object.convert(target='MESH')
    o=bpy.context.view_layer.objects.active
    o.select_set(False)
    o.data.materials.append(mats[key])
    return o

def validate_manifold(col):
    failures=[]
    for o in col.all_objects:
        if o.type != 'MESH':
            continue
        bm=bmesh.new()
        bm.from_mesh(o.data)
        bm.normal_update()
        if not bm.faces or any(not e.is_manifold for e in bm.edges):
            failures.append(o.name)
        bm.free()
    if failures:
        raise AssertionError("Non-manifold meshes: "+", ".join(failures))
    return sum(o.type=='MESH' for o in col.all_objects)

def build():
    clear_own()
    col=make_collection()
    mats={k:material(k,v) for k,v in MATERIALS.items()}
    mount=empty(col,"MOUNT_ROOT",(0,0,0))
    mount["mount_direction"]="internal_frame"
    mount["units"]="metres"
    for x in (-0.65,0.65):
        for y in (-0.40,0.40):
            empty(col,"MOUNT_BOLT_%s_%s"%("L" if x<0 else "R","F" if y<0 else "B"),(x,y,0.0))
            cylinder(col,mats,"isolator_%s_%s"%(x,y),(x,y,0.043),0.075,0.075,"rubber",20)
            cylinder(col,mats,"bolt_%s_%s"%(x,y),(x,y,0.17),0.034,0.045,"titanium",16)
    bevel_cube(col,mats,"base_plate",(0,0,0.105),(1.57,1.03,0.15),"graphite",0.045)
    bevel_cube(col,mats,"front_service_rail",(0,-0.445,0.24),(1.40,0.085,0.13),"ceramic",0.014)
    bevel_cube(col,mats,"rear_service_rail",(0,0.445,0.24),(1.40,0.085,0.13),"titanium",0.014)
    for i,x in enumerate(PISTON_X):
        side="L" if i==0 else "R"
        # Real hollow annular barrel: moving rod passes through the bore without penetrating shell.
        annulus(col,mats,side+"_hollow_barrel",x,0,SHELL_BOTTOM,SHELL_TOP,0.200,BORE_RADIUS,"titanium")
        for z in (0.42,0.68,0.95,1.085):
            torus(col,mats,side+"_barrel_band_%s"%z,(x,0,z),0.205,0.014,"copper")
        cylinder(col,mats,side+"_barrel_foot",(x,0,0.32),0.21,0.10,"graphite")
        # Rod has 32 mm radial clearance to the inside wall, and stays above the solid foot.
        piston=empty(col,side+"_PISTON_PIVOT",(0,0,0))
        piston["axis"]="local +Z"
        piston["stroke_m"]=0.10 if i==0 else -0.10
        cylinder(col,mats,side+"_piston_rod",(x,0,1.25),ROD_RADIUS,0.47,"ceramic",32,piston)
        cylinder(col,mats,side+"_piston_cap",(x,0,1.52),0.16,0.085,"graphite",32,piston)
        torus(col,mats,side+"_piston_ring",(x,0,1.565),0.11,0.017,"ice_blue",piston)
        cylinder(col,mats,side+"_top_fastener",(x,0,1.585),0.050,0.028,"copper",16,piston)
        empty(col,side+"_PISTON_AXIS",(x,0,1.1),piston)
        # Distinct piston strokes, opposing phase, keyed at both extrema and the rest pose.
        values=(0.0,0.10,0.0) if i==0 else (0.0,-0.10,0.0)
        for frame,z in zip((1,31,61),values):
            piston.location.z=z
            piston.keyframe_insert(data_path="location",frame=frame,group=PREFIX+"PISTONS")
        if piston.animation_data and piston.animation_data.action:
            piston.animation_data.action.name=PREFIX+side+"_stroke"
        piston.location.z=0
        empty(col,side+"_SERVICE_PORT",(x,0.32,0.75))
        # Lateral blue coolant lines deliberately routed beyond barrel radius.
        sx=-1 if i==0 else 1
        pipe(col,mats,side+"_cryo_supply",[
            (sx*0.63,-0.25,0.31),(sx*0.66,-0.28,0.56),
            (sx*0.64,-0.27,0.93),(sx*0.57,-0.23,1.06)],0.025,"ice_blue")
        pipe(col,mats,side+"_cryo_return",[
            (sx*0.61,0.27,0.31),(sx*0.65,0.31,0.56),
            (sx*0.65,0.31,0.92),(sx*0.56,0.25,1.02)],0.023,"ice_blue")
        for z in (0.44,0.86):
            bevel_cube(col,mats,side+"_service_tab_%s"%z,
                (sx*0.61,0,z),(0.09,0.10,0.06),"ceramic",0.008)
    # Heat exchanger fins are stationary, not part of the moving pistons.
    bevel_cube(col,mats,"exchanger_spine",(0,-0.29,0.77),(0.16,0.23,0.84),"graphite",0.02)
    for n in range(15):
        z=0.43+n*0.045
        bevel_cube(col,mats,"exchanger_fin_%02d"%n,(0,-0.29,z),
                   (0.31,0.30,0.018),"titanium",0.004)
    bevel_cube(col,mats,"valve_block",(0,0.30,0.48),(0.23,0.20,0.23),"copper",0.02)
    cylinder(col,mats,"pressure_knob",(0,0.30,0.67),0.09,0.05,"ceramic",24)
    empty(col,"FLOW_IN",(-0.68,-0.29,0.32))
    empty(col,"FLOW_OUT",(0.68,0.29,0.32))
    for side,x in (("L",-0.56),("R",0.56)):
        bevel_cube(col,mats,side+"_warning_light",(x,-0.445,0.315),
                   (0.085,0.016,0.045),"amber_led",0.006)
        bevel_cube(col,mats,side+"_coolant_status",(x,0.445,0.315),
                   (0.11,0.016,0.045),"cyan_led",0.006)
    scene=bpy.context.scene
    scene.frame_start=1
    scene.frame_end=FRAME_COUNT
    scene.render.fps=30
    scene.frame_set(1)
    bpy.context.view_layer.update()
    count=validate_manifold(col)
    return col, count

def assert_kinematics(col):
    # Analytic tests for all frames: rod/barrel bore, rod/foot, cap/shell and twin-cylinder separation.
    checks=0
    min_gap=float('inf')
    for frame in range(1,FRAME_COUNT+1):
        bpy.context.scene.frame_set(frame)
        for i,x in enumerate(PISTON_X):
            p=col.objects[PREFIX+("L" if i==0 else "R")+"_PISTON_PIVOT"]
            shift=p.location.z
            # rod spans 1.015..1.485 + shift; foot ends at 0.37.
            bottom=1.25-0.235+shift
            cap_bottom=1.52-0.0425+shift
            radial=BORE_RADIUS-ROD_RADIUS
            axial=cap_bottom-SHELL_TOP
            foot=bottom-0.37
            twin=abs(PISTON_X[1]-PISTON_X[0])-2*0.21
            min_gap=min(min_gap,radial,axial,foot,twin)
            assert radial>=0.025 and axial>=0.25 and foot>=0.5 and twin>=0.25
            checks+=1
    bpy.context.scene.frame_set(1)
    return {"samples":checks,"minimum_analytic_gap_m":round(min_gap,6),
            "scope":"rod/bore, cap/barrel, rod/foot, cylinder/cylinder; NOT full mesh-pair collision"}

def counts(col):
    return {"objects":len(col.all_objects),
            "meshes":sum(o.type=='MESH' for o in col.all_objects),
            "materials":len([m for m in bpy.data.materials if m.name.startswith(PREFIX)])}

def export(col, out):
    out.mkdir(parents=True,exist_ok=True)
    for o in bpy.context.selected_objects:
        o.select_set(False)
    for o in col.all_objects:
        o.select_set(True)
    bpy.context.view_layer.objects.active=next(o for o in col.all_objects if o.type=='MESH')
    bpy.ops.wm.save_as_mainfile(filepath=str(out/"M05-024.blend"))
    bpy.ops.export_scene.gltf(filepath=str(out/"M05-024.glb"),export_format='GLB',
        use_selection=True,export_animations=True,export_extras=True)
    fbx="NOT RUN"
    try:
        bpy.ops.export_scene.fbx(filepath=str(out/"M05-024.fbx"),use_selection=True,
            add_leaf_bones=False,bake_anim=True)
        fbx="PASS"
    except Exception as e:
        fbx="NOT RUN: "+str(e)
    return fbx

def main():
    argv=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else []
    parser=argparse.ArgumentParser()
    parser.add_argument("--out",default=str(Path(__file__).parent/"out"))
    parser.add_argument("--self-test",action="store_true")
    args=parser.parse_args(argv)
    foreign=None
    if args.self_test:
        foreign=bpy.data.objects.new("FOREIGN_DO_NOT_TOUCH_M05_024",None)
        bpy.context.scene.collection.objects.link(foreign)
    col,mesh_count=build()
    first=counts(col)
    if args.self_test:
        col,mesh_count=build()
        assert first==counts(col), "Repeated generation changed resource counts"
        assert foreign.name in bpy.data.objects, "Generator removed foreign object"
    kinematics=assert_kinematics(col)
    if foreign:
        bpy.data.objects.remove(foreign,do_unlink=True)
        foreign=None
    fbx=export(col,Path(args.out).resolve())
    result={"id":"M05-024","status":"BLENDER_PASS","counts":counts(col),
        "materials":list(MATERIALS),"animation_frames":FRAME_COUNT,
        "kinematics":kinematics,"fbx":fbx,
        "unity":"NOT RUN","full_triangle_collision":"NOT RUN"}
    path=Path(args.out).resolve()/"qa_m05_024.json"
    path.write_text(json.dumps(result,indent=2)+"\n",encoding="utf-8")
    print("M05_024_QA "+json.dumps(result,sort_keys=True))

if __name__=="__main__":
    main()
