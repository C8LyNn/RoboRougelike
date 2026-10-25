#!/usr/bin/env python3
"""M01-019 — Wyrzutnia dymna sześciotubowa (visual-only Blender model).

Blender 3.6+/4.x. 1 Blender unit = 1 metre. Native: Z-up, muzzle -Y. Side mounting plane X=0.
GLB/FBX: Y-up, +Z-forward. Mount origin MOUNT_SIDE_SOCKET=(0,0,0).
Run: blender -b --python-exit-code 1 --python generate_m01_018.py -- --out generated --repeat
"""
import argparse
import json
import math
import sys
from pathlib import Path

import bpy
import bmesh
from mathutils import Vector

ID = "M01-019"
STEM = "M01-019_six_tube_smoke_launcher"
FRAMES = 73
PALETTE = {
    "Graphite": ((0.038, 0.052, 0.070, 1), 0.74, 0.37),
    "Titanium": ((0.43, 0.49, 0.54, 1), 0.84, 0.27),
    "CeramicIvory": ((0.83, 0.82, 0.72, 1), 0.17, 0.34),
    "Copper": ((0.65, 0.30, 0.105, 1), 0.83, 0.29),
    "Tungsten": ((0.13, 0.16, 0.19, 1), 0.90, 0.26),
    "SafetyOrange": ((0.98, 0.32, 0.035, 1), 0.26, 0.34),
    "EmissiveCyan": ((0.02, 0.64, 0.80, 1), 0.18, 0.24),
}
mats = {}
parts = []
mount = None
slider = None


def reset():
    global mats, parts, mount, slider
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for collection in (bpy.data.meshes, bpy.data.materials, bpy.data.actions):
        for block in list(collection):
            collection.remove(block)
    mats, parts = {}, []
    mount = slider = None
    sc = bpy.context.scene
    sc.unit_settings.system = "METRIC"
    sc.unit_settings.scale_length = 1.0
    sc.render.fps = 60
    sc.frame_start, sc.frame_end = 1, FRAMES
    sc.frame_set(1)


def materials():
    for key, (color, metal, rough) in PALETTE.items():
        m = bpy.data.materials.new(ID + "_" + key)
        m.diffuse_color = color
        m.use_nodes = True
        p = m.node_tree.nodes.get("Principled BSDF")
        p.inputs["Base Color"].default_value = color
        p.inputs["Metallic"].default_value = metal
        p.inputs["Roughness"].default_value = rough
        if key == "EmissiveCyan":
            for prop in ("Emission Color", "Emission"):
                if prop in p.inputs:
                    p.inputs[prop].default_value = color
            if "Emission Strength" in p.inputs:
                p.inputs["Emission Strength"].default_value = 1.8
        mats[key] = m


def finish(obj, name, mat, group="STATIC", parent=None, bevel=0.0, smooth=False):
    obj.name = ID + "_" + name
    if bevel:
        bpy.context.view_layer.objects.active = obj
        mod = obj.modifiers.new("machined_edge", "BEVEL")
        mod.width = bevel
        mod.segments = 2
        bpy.ops.object.modifier_apply(modifier=mod.name)
    obj.data.materials.append(mats[mat])
    obj["qa_group"] = group
    if parent:
        obj.parent = parent
        obj.matrix_parent_inverse.identity()
    if smooth:
        for face in obj.data.polygons:
            face.use_smooth = (len(face.vertices) <= 4)
    parts.append(obj)
    return obj


def box(name, loc, dims, mat, group="STATIC", parent=None, bevel=0.0):
    bpy.ops.mesh.primitive_cube_add()
    obj = bpy.context.object
    obj.dimensions = dims
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    finish(obj, name, mat, group, parent, bevel)
    obj.location = loc
    return obj


def cylinder(name, loc, radius, depth, mat, group="STATIC", parent=None,
             rotation=(0,0,0), vertices=24):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth)
    obj = bpy.context.object
    finish(obj, name, mat, group, parent)
    obj.location = loc
    obj.rotation_euler = rotation
    for face in obj.data.polygons:
        face.use_smooth = len(face.vertices) == 4
    return obj


def torus(name, loc, major, minor, mat, parent=None, rotation=(0,0,0)):
    bpy.ops.mesh.primitive_torus_add(major_segments=32, minor_segments=8,
                                    major_radius=major, minor_radius=minor)
    obj = bpy.context.object
    finish(obj, name, mat, "STATIC", parent, smooth=True)
    obj.location = loc
    obj.rotation_euler = rotation
    return obj


def rod(name, a, b, radius, mat, parent=None, group="STATIC"):
    va, vb = Vector(a), Vector(b)
    vec = vb-va
    obj = cylinder(name, (va+vb)/2, radius, vec.length, mat, group, parent, vertices=16)
    obj.rotation_euler = vec.to_track_quat("Z","Y").to_euler()
    return obj


def annular_barrel(name, loc, length, outer, inner, mat, parent):
    """Watertight annular cylinder; bore is actual empty geometry, not a decal."""
    n = 32
    verts = []
    for r, y in ((outer,-length/2),(outer,length/2),
                 (inner,-length/2),(inner,length/2)):
        verts.extend((r*math.cos(math.tau*i/n),y,r*math.sin(math.tau*i/n))
                     for i in range(n))
    faces = []
    for i in range(n):
        j = (i+1)%n
        faces.extend(((i,n+i,n+j,j),
                      (2*n+i,2*n+j,3*n+j,3*n+i),
                      (i,j,2*n+j,2*n+i),
                      (n+i,3*n+i,3*n+j,n+j)))
    mesh = bpy.data.meshes.new(ID+"_"+name+"_mesh")
    mesh.from_pydata(verts,[],faces)
    mesh.update()
    bm = bmesh.new()
    bm.from_mesh(mesh)
    bmesh.ops.recalc_face_normals(bm,faces=bm.faces)
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new(ID+"_"+name,mesh)
    bpy.context.collection.objects.link(obj)
    finish(obj,name,mat,parent=parent)
    obj.location = loc
    return obj


def empty(name, parent=None, loc=(0,0,0)):
    obj = bpy.data.objects.new(name,None)
    bpy.context.collection.objects.link(obj)
    if parent:
        obj.parent = parent
        obj.matrix_parent_inverse.identity()
    obj.location = loc
    obj.empty_display_size = 0.12
    obj.empty_display_type = "CUBE"
    return obj


def build():
    global mount, slider
    reset()
    materials()
    mount = empty("MOUNT_SIDE_SOCKET")
    # Side mounting face X=0, external assembly extends along +X.
    box("Side_mount_foot",(.075,0,0),(.15,.26,.28),"Graphite",parent=mount,bevel=.018)
    cylinder("Mount_lock",(.16,0,0),.09,.035,"Copper",parent=mount,rotation=(0,math.pi/2,0))
    box("Receiver_spine",(.32,.30,.26),(.28,.22,.54),"Titanium",parent=mount,bevel=.025)
    box("Ceramic_receiver_shield",(.34,.33,.53),(.35,.19,.21),"CeramicIvory",parent=mount,bevel=.022)
    box("Receiver_upper",(.34,.36,.68),(.28,.16,.10),"Graphite",parent=mount,bevel=.016)
    for side in (-1,1):
        rod("Diagonal_mount_strut_%s"%side,(.16,.22,side*.095),
            (.59,.24,.48+side*.17),.032,"Titanium",parent=mount)
        box("Rear_heat_fin_%s"%side,(.54,.33,.48+side*.18),
            (.13,.075,.085),"Copper",parent=mount,bevel=.012)
        box("Side_warning_%s"%side,(.38,.225,.50+side*.16),
            (.11,.018,.035),"SafetyOrange",parent=mount,bevel=.004)
    # Rear bearing and fixed safety hoop sit behind the rotating cassette.
    cylinder("Bearing_rear_plate",(.66,.205,.48),.185,.09,"Graphite",
             parent=mount,rotation=(math.pi/2,0,0),vertices=32)
    cylinder("Bearing_shaft",(.66,.105,.48),.072,.11,"Titanium",
             parent=mount,rotation=(math.pi/2,0,0),vertices=32)
    torus("Fixed_guard_hoop",(.66,.205,.48),.365,.020,"Titanium",parent=mount,rotation=(math.pi/2,0,0))
    for side in (-1,1):
        rod("Guard_spoke_%s"%side,(.66+side*.37,.205,.48),
            (.66+side*.14,.205,.48),.020,"Graphite",parent=mount)
    for j in range(3):
        box("Receiver_rivet_%s"%j,(.25+j*.085,.225,.59),
            (.033,.018,.033),"SafetyOrange",parent=mount,bevel=.004)
    # Six asymmetric hollow tubes; indexing rotates the entire cassette.
    slider = empty("CASSETTE_INDEX_Y",mount,(.66,-.13,.48))
    cylinder("Drum_hub",(0,-.025,0),.105,.14,"Graphite","MOVING",
             slider,rotation=(math.pi/2,0,0),vertices=32)
    cylinder("Drum_front_star",(0,-.37,0),.09,.055,"Copper","MOVING",
             slider,rotation=(math.pi/2,0,0),vertices=32)
    for tag,y,mat in (("Rear",-.005,"Titanium"),("Front",-.29,"Graphite")):
        ring=torus("Drum_%s_retainer"%tag,(0,y,0),.317,.019,mat,parent=slider,rotation=(math.pi/2,0,0))
        ring["qa_group"]="MOVING"
    lengths=(.46,.54,.49,.57,.51,.44)
    radii=(.067,.075,.066,.072,.070,.064)
    for i in range(6):
        a=math.tau*i/6
        x,z=.225*math.cos(a),.225*math.sin(a)
        length,radius=lengths[i],radii[i]
        tube_y=-.235-(length-.46)*.16
        barrel=annular_barrel("Open_bore_%02d"%i,(x,tube_y,z),
                             length,radius,radius*.62,
                             "Titanium" if i%2 else "Graphite",slider)
        barrel["qa_group"]="MOVING"
        for suffix,y,major,minor,mat in (
            ("Muzzle",tube_y-length/2+.018,radius+.002,.012,
             "Copper" if i%2 else "CeramicIvory"),
            ("Mid",-.235,radius+.012,.013,"CeramicIvory"),
            ("Rear",-.055,radius+.010,.011,
             "SafetyOrange" if i in (0,3) else "Graphite")):
            ring=torus("%s_clamp_%02d"%(suffix,i),(x,y,z),major,minor,
                       mat,parent=slider,rotation=(math.pi/2,0,0))
            ring["qa_group"]="MOVING"
        rod("Radial_spoke_%02d"%i,(x*.40,-.075,z*.40),
            (x*.82,-.075,z*.82),.025,"Copper",parent=slider,group="MOVING")
        cylinder("Tube_status_%02d"%i,(x,-.015,z),.019,.012,
                 "EmissiveCyan","MOVING",slider,
                 rotation=(math.pi/2,0,0),vertices=16)
    # Six 60-degree index moves, each followed by a dwell; 73 frames at 30 fps.
    for f,angle in ((1,0),(7,0),(13,math.pi/3),(19,math.pi/3),
                    (25,2*math.pi/3),(31,2*math.pi/3),
                    (37,math.pi),(43,math.pi),
                    (49,4*math.pi/3),(55,4*math.pi/3),
                    (61,5*math.pi/3),(67,5*math.pi/3),(73,math.tau)):
        slider.rotation_euler=(0,angle,0)
        slider.keyframe_insert(data_path="rotation_euler",frame=f)
    if slider.animation_data and slider.animation_data.action:
        action=slider.animation_data.action
        if hasattr(action,"fcurves"):
            for fc in action.fcurves:
                for key in fc.keyframe_points:
                    key.interpolation="LINEAR"
    bpy.context.scene.frame_set(1)
    bpy.context.view_layer.update()


def world_bounds(obj):
    m=obj.matrix_world
    pts=[m @ Vector(c) for c in obj.bound_box]
    return ([min(p[i] for p in pts) for i in range(3)],
            [max(p[i] for p in pts) for i in range(3)])


def validate():
    meshes=[o for o in parts if o.type=="MESH"]
    assert len(meshes)>=55, "Too few distinct designed components"
    assert len({m.name for m in mats.values()})>=3
    assert mount.location.length<1e-8
    assert len([o for o in meshes if o["qa_group"]=="MOVING"])>=35
    for obj in meshes:
        bm=bmesh.new()
        bm.from_mesh(obj.data)
        assert len(bm.verts)>=8 and len(bm.faces)>=6, obj.name
        assert all(e.is_manifold for e in bm.edges), "Nonmanifold: "+obj.name
        assert all(f.calc_area()>1e-9 for f in bm.faces), "Degenerate: "+obj.name
        assert bm.calc_volume(signed=False)>1e-8, "Zero volume: "+obj.name
        bm.free()
    moving=[o for o in meshes if o["qa_group"]=="MOVING"]
    fixed=[o for o in meshes if o["qa_group"]=="STATIC"]
    checks=0
    hits=[]
    min_gap=1e6
    sample_positions=[]
    for frame in range(1,FRAMES+1):
        bpy.context.scene.frame_set(frame)
        bpy.context.view_layer.update()
        sample_positions.append(round(slider.rotation_euler.y,6))
        bounds_fixed=[(o,world_bounds(o)) for o in fixed]
        for obj in moving:
            a0,a1=world_bounds(obj)
            for other,(b0,b1) in bounds_fixed:
                checks+=1
                gaps=[max(b0[i]-a1[i],a0[i]-b1[i],0.0) for i in range(3)]
                sep=max(gaps)
                if sep<1e-7:
                    hits.append((frame,obj.name,other.name))
                min_gap=min(min_gap,sep)
    assert not hits, "Unintended moving/static AABB intersections: "+str(hits[:8])
    assert min_gap>.012, "Too little moving clearance: "+str(min_gap)
    assert abs(min(sample_positions))<1e-5
    assert abs(max(sample_positions)-math.tau)<1e-5
    bpy.context.scene.frame_set(1)
    triangles=sum(max(0,len(p.vertices)-2) for o in meshes for p in o.data.polygons)
    return {"id":ID,"status":"BLENDER_QA_PASS","meshes":len(meshes),
            "materials":len(mats),"vertices":sum(len(o.data.vertices) for o in meshes),
            "triangles":triangles,"frames_tested":FRAMES,
            "aabb_checks":checks,"aabb_intersections":len(hits),
            "min_aabb_clearance_m":round(min_gap,6),
            "cassette_rotation_radians":round(math.tau,6),
            "mount":"MOUNT_SIDE_SOCKET","moving_pivot":"CASSETTE_INDEX_Y"}


def export(folder,report):
    folder.mkdir(parents=True,exist_ok=True)
    stem=str(folder/STEM)
    bpy.ops.wm.save_as_mainfile(filepath=stem+".blend")
    bpy.ops.export_scene.gltf(filepath=stem+".glb",export_format="GLB",
                              export_animations=True)
    bpy.ops.export_scene.fbx(filepath=stem+".fbx",use_selection=False,
                             axis_forward="Z",axis_up="Y",bake_anim=True)
    if hasattr(bpy.ops.wm,"obj_export"):
        bpy.ops.wm.obj_export(filepath=stem+".obj",export_materials=True,
                              export_selected_objects=False)
    else:
        bpy.ops.export_scene.obj(filepath=stem+".obj",use_selection=False,
                                 use_materials=True)
    (folder/(STEM+"_qa.json")).write_text(
        json.dumps(report,indent=2,ensure_ascii=False)+"\n",encoding="utf-8")


def main():
    p=argparse.ArgumentParser()
    p.add_argument("--out",default="generated")
    p.add_argument("--repeat",action="store_true")
    args=p.parse_args(sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else [])
    build()
    result=validate()
    if args.repeat:
        baseline=(result["meshes"],result["materials"],result["vertices"])
        build()
        again=validate()
        assert baseline==(again["meshes"],again["materials"],again["vertices"])
        result=again
        result["repeat_determinism"]="PASS"
    export(Path(args.out),result)
    print(json.dumps(result,indent=2))


if __name__=="__main__":
    main()
