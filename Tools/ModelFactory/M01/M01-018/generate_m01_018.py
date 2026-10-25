#!/usr/bin/env python3
"""M01-018 — Armata kinetyczna z przeciwwagą (visual-only Blender model).

Blender 3.6+/4.x. 1 Blender unit = 1 metre. Native: Z-up, muzzle -Y.
GLB/FBX: Y-up, +Z-forward. Mount origin MOUNT_TOP_SOCKET=(0,0,0).
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

ID = "M01-018"
STEM = "M01-018_kinetic_counterweight_cannon"
FRAMES = 61
TRAVEL = 0.22
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
    mount = empty("MOUNT_TOP_SOCKET")
    # Wide base plate, locking stud and tall armored breech.
    box("Socket_foot",(0,0.05,0.11),(.76,.76,.20),"Graphite",parent=mount,bevel=.035)
    cylinder("Socket_lock",(0,0.05,0.012),.25,.035,"Titanium",parent=mount)
    box("Receiver",(0,-.28,.49),(.65,.85,.45),"Titanium",parent=mount,bevel=.055)
    box("Receiver_upper",(0,-.33,.76),(.52,.62,.12),"Graphite",parent=mount,bevel=.025)
    box("Breech_hatch",(0,.12,.55),(.43,.07,.25),"Tungsten",parent=mount,bevel=.018)
    for side in (-1,1):
        box("Ceramic_shield_%s"%side,(side*.36,-.34,.54),
            (.075,.58,.29),"CeramicIvory",parent=mount,bevel=.021)
        box("Safety_chevron_%s"%side,(side*.407,-.44,.62),
            (.012,.26,.045),"SafetyOrange",parent=mount,bevel=.005)
        for i in range(4):
            box("Copper_receiver_fin_%s_%s"%(side,i),
                (side*.27,-.57+i*.15,.81),(.04,.035,.11),
                "Copper",parent=mount,bevel=.007)
    # Massive forward kinetic barrel with a true annular bore.
    annular_barrel("True_hollow_barrel",(0,-1.23,.57),1.32,.184,.102,"Tungsten",mount)
    torus("Muzzle_copper_ring",(0,-1.905,.57),.187,.022,"Copper",
          parent=mount,rotation=(math.pi/2,0,0))
    torus("Muzzle_safety_ring",(0,-1.86,.57),.19,.012,"SafetyOrange",
          parent=mount,rotation=(math.pi/2,0,0))
    for i in range(4):
        torus("Barrel_collar_%02d"%i,(0,-1.57+i*.26,.57),
              .19,.020,"Titanium" if i%2 else "Graphite",
              parent=mount,rotation=(math.pi/2,0,0))
    box("Top_muzzle_sight",(0,-1.69,.80),(.10,.21,.13),
        "CeramicIvory",parent=mount,bevel=.012)
    # Rear mechanism is deliberately OPEN: two side rails and cross braces,
    # separated from the moving central counterweight's swept AABB.
    for side in (-1,1):
        x=side*.48
        rod("Exposed_guide_rail_%s"%side,(x,.38,.66),(x,1.61,.66),
            .042,"Titanium",parent=mount)
        cylinder("Rail_front_socket_%s"%side,(x,.38,.66),.075,.10,
                 "Graphite",parent=mount,rotation=(math.pi/2,0,0))
        cylinder("Rail_rear_socket_%s"%side,(x,1.61,.66),.075,.10,
                 "Copper",parent=mount,rotation=(math.pi/2,0,0))
        for j in range(3):
            box("Rail_side_mark_%s_%s"%(side,j),
                (side*.57,.64+j*.33,.65),(.032,.08,.055),
                "SafetyOrange",parent=mount,bevel=.007)
        rod("Rear_support_leg_%s"%side,
            (side*.37,.13,.34),(x,.38,.66),.045,
            "Graphite",parent=mount)
    rod("Open_rear_crossbar",(-.48,1.66,.66),(.48,1.66,.66),
        .045,"Titanium",parent=mount)
    # Sliding counterweight: centreline inside rail spacing, rearward stroke.
    slider = empty("COUNTERWEIGHT_SLIDE_Y_AXIS",mount,(0,.79,.66))
    box("Tungsten_counterweight",(0,0,0),(.48,.36,.32),
        "Tungsten","MOVING",slider,bevel=.034)
    box("Counterweight_top_cap",(0,0,.19),(.40,.29,.075),
        "CeramicIvory","MOVING",slider,bevel=.018)
    box("Counterweight_lower_saddle",(0,0,-.20),(.36,.29,.055),
        "Titanium","MOVING",slider,bevel=.012)
    for side in (-1,1):
        box("Counterweight_guide_fin_%s"%side,(side*.27,0,0),
            (.065,.28,.15),"Copper","MOVING",slider,bevel=.011)
        box("Counterweight_warning_%s"%side,(side*.31,-.04,.10),
            (.014,.18,.045),"SafetyOrange","MOVING",slider,bevel=.003)
        cylinder("Counterweight_status_%s"%side,
                 (side*.21,.04,.24),.032,.014,"EmissiveCyan",
                 "MOVING",slider)
    for j in range(3):
        box("Mass_segment_%02d"%j,(-.13+j*.13,.14,.0),
            (.10,.075,.20),"Graphite","MOVING",slider,bevel=.009)
    # Rear travel stop remains well beyond the swept counterweight.
    box("Rear_stop",(0,1.70,.66),(.32,.09,.13),
        "Graphite",parent=mount,bevel=.018)
    for f, delta in ((1,0),(9,TRAVEL),(21,0),
                     (29,TRAVEL),(41,0),(49,TRAVEL),(61,0)):
        slider.location=(0,.79+delta,.66)
        slider.keyframe_insert(data_path="location",frame=f)
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
    assert len(meshes)>=45, "Too few distinct designed components"
    assert len({m.name for m in mats.values()})>=3
    assert mount.location.length<1e-8
    assert len([o for o in meshes if o["qa_group"]=="MOVING"])>=8
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
        sample_positions.append(round(slider.location.y,6))
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
    assert min_gap>.005, "Too little moving clearance: "+str(min_gap)
    assert abs(min(sample_positions)-.79)<1e-5
    assert abs(max(sample_positions)-(.79+TRAVEL))<1e-5
    bpy.context.scene.frame_set(1)
    triangles=sum(max(0,len(p.vertices)-2) for o in meshes for p in o.data.polygons)
    return {"id":ID,"status":"BLENDER_QA_PASS","meshes":len(meshes),
            "materials":len(mats),"vertices":sum(len(o.data.vertices) for o in meshes),
            "triangles":triangles,"frames_tested":FRAMES,
            "aabb_checks":checks,"aabb_intersections":len(hits),
            "min_aabb_clearance_m":round(min_gap,6),
            "counterweight_travel_m":TRAVEL,
            "mount":"MOUNT_TOP_SOCKET","moving_pivot":"COUNTERWEIGHT_SLIDE_Y_AXIS"}


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
