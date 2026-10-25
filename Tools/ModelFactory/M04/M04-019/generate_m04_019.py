#!/usr/bin/env python3
"""M04-019 — deployable six-segment side turbine, Blender 4+/Unity 6.

Visual asset only. Metres; Blender Z-up; Unity conversion via glTF importer.
The chassis SIDE mounting plane is X=0; +X points away from the chassis.
The seven-blade rotor spins around local +X; six protective shell petals
hinge OUTWARD around their respective local tangent (+Z) axes, 0..30 deg.
No gameplay, scenes, shared prefabs or ProjectSettings are touched.

blender -b --factory-startup --python-exit-code 1 --python \
  Tools/ModelFactory/M04/M04-019/generate_m04_019.py -- --out build/M04-019
"""
import argparse
import json
import math
import sys
from pathlib import Path

import bpy
import bmesh
from mathutils import Vector

ID = "M04-019"
STEM = "M04-019_segment_turbine"
PREFIX = "M04_019_"
PETALS = 6
BLADES = 7
MATERIALS = {}
SHELL_MESHES = []
ROTOR_MESHES = []
PIVOTS = []
MESHES = []


def material(label, color, metallic=0.0, roughness=0.42, emission=None):
    m = bpy.data.materials.new(PREFIX + label)
    m.diffuse_color = (*color, 1)
    m.use_nodes = True
    p = m.node_tree.nodes.get("Principled BSDF")
    p.inputs["Base Color"].default_value = (*color, 1)
    p.inputs["Metallic"].default_value = metallic
    p.inputs["Roughness"].default_value = roughness
    if emission is not None:
        key = "Emission Color" if "Emission Color" in p.inputs else "Emission"
        p.inputs[key].default_value = (*emission, 1)
        if "Emission Strength" in p.inputs:
            p.inputs["Emission Strength"].default_value = 2.1
    MATERIALS[label] = m


def empty(label, parent=None, location=(0, 0, 0)):
    obj = bpy.data.objects.new(PREFIX + label, None)
    bpy.context.collection.objects.link(obj)
    obj.empty_display_type = "PLAIN_AXES"
    obj.empty_display_size = .07
    obj.parent = parent
    obj.location = location
    return obj


def mesh(label, vertices, faces, mat, parent=None, smooth=True):
    me = bpy.data.meshes.new(PREFIX + label + "_geometry")
    me.from_pydata(vertices, [], faces)
    me.update()
    bm = bmesh.new()
    bm.from_mesh(me)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(me)
    bm.free()
    assert not me.validate(clean_customdata=False), label + " invalid geometry"
    assert len(me.polygons) >= 6, label
    ob = bpy.data.objects.new(PREFIX + label, me)
    bpy.context.collection.objects.link(ob)
    ob.parent = parent
    me.materials.append(MATERIALS[mat])
    for face in me.polygons:
        face.use_smooth = smooth and len(face.vertices) == 4
    MESHES.append(ob)
    return ob


def box(label, loc, dims, mat, parent=None):
    x, y, z = (d / 2 for d in dims)
    v = [(-x,-y,-z),(-x,-y,z),(-x,y,-z),(-x,y,z),
         (x,-y,-z),(x,-y,z),(x,y,-z),(x,y,z)]
    f = [(0,2,3,1),(4,5,7,6),(0,1,5,4),
         (2,6,7,3),(0,4,6,2),(1,3,7,5)]
    ob = mesh(label, v, f, mat, parent, smooth=False)
    ob.location = loc
    return ob


def ring(label, x0, x1, inner, outer, mat, parent=None, steps=64):
    """Closed annular tube with true hollow bore and positive wall thickness."""
    v = []
    for x in (x0, x1):
        for r in (inner, outer):
            for j in range(steps):
                t = 2 * math.pi * j / steps
                v.append((x, r * math.cos(t), r * math.sin(t)))
    def ix(a, b, j):
        return (a * 2 + b) * steps + (j % steps)
    f = []
    for j in range(steps):
        k = (j + 1) % steps
        f.extend([
            (ix(0,1,j),ix(0,1,k),ix(1,1,k),ix(1,1,j)),
            (ix(0,0,k),ix(0,0,j),ix(1,0,j),ix(1,0,k)),
            (ix(0,0,j),ix(0,0,k),ix(0,1,k),ix(0,1,j)),
            (ix(1,0,k),ix(1,0,j),ix(1,1,j),ix(1,1,k)),
        ])
    return mesh(label, v, f, mat, parent)


def arc_shell(label, parent, x0, x1, r0, r1, half_angle, mat, n=12):
    """Watertight arc-shaped armour panel; local Y is radial, Z tangential."""
    v = []
    for x in (x0, x1):
        for r in (r0, r1):
            for j in range(n+1):
                t = -half_angle + 2 * half_angle * j / n
                v.append((x, r*math.cos(t)-.390, r*math.sin(t)))
    def ix(a,b,j):
        return (a*2+b)*(n+1)+j
    f=[]
    for j in range(n):
        k=j+1
        f.extend([
            (ix(0,1,j),ix(0,1,k),ix(1,1,k),ix(1,1,j)),
            (ix(0,0,k),ix(0,0,j),ix(1,0,j),ix(1,0,k)),
            (ix(0,0,j),ix(0,0,k),ix(0,1,k),ix(0,1,j)),
            (ix(1,0,k),ix(1,0,j),ix(1,1,j),ix(1,1,k)),
        ])
    f.extend([
        (ix(0,0,0),ix(0,1,0),ix(1,1,0),ix(1,0,0)),
        (ix(0,1,n),ix(0,0,n),ix(1,0,n),ix(1,1,n)),
    ])
    return mesh(label, v, f, mat, parent)


def blade(label, angle, parent):
    """Twisted, swept, solid 4-station airfoil with two surfaces and edge walls."""
    v=[]
    for radius, twist, width, axial in [
        (.075, -.13, .22, -.010),
        (.120, -.06, .23,  .002),
        (.185,  .06, .19,  .016),
        (.240,  .12, .11,  .020),
    ]:
        for side in (-1, 1):
            t=angle+twist+side*width
            for face in (-1, 1):
                v.append((axial+face*.010,
                          radius*math.cos(t),radius*math.sin(t)))
    f=[]
    for k in range(3):
        a,b=k*4,(k+1)*4
        f.extend([(a,a+1,b+1,b),
                  (a+2,b+2,b+3,a+3),
                  (a,b,b+2,a+2),
                  (a+1,a+3,b+3,b+1)])
    f.extend([(0,2,3,1),(12,13,15,14)])
    return mesh(label,v,f,"ceramic",parent)


def key(obj, frame, channel, value):
    if channel == "x":
        obj.rotation_euler[0] = value
        obj.keyframe_insert(data_path="rotation_euler", frame=frame)
    else:
        obj.rotation_euler[2] = value
        obj.keyframe_insert(data_path="rotation_euler", frame=frame)


def build():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for m in list(bpy.data.materials):
        if m.name.startswith(PREFIX):
            bpy.data.materials.remove(m)

    for args in [
        ("graphite",(.050,.069,.085),.75,.37,None),
        ("titanium",(.37,.46,.52),.82,.27,None),
        ("ceramic",(.77,.82,.78),.18,.32,None),
        ("copper",(.58,.25,.105),.77,.30,None),
        ("rubber",(.025,.034,.040),.05,.85,None),
        ("darksteel",(.12,.16,.19),.72,.44,None),
        ("cyan",(.015,.36,.48),.18,.26,(.00,.65,1.0)),
        ("amber",(.50,.19,.025),.16,.29,(1.0,.30,.015)),
    ]:
        material(*args)

    mount=empty("SIDE_MOUNT_X0_ORIGIN")
    # Side plate is a ring rather than a solid disk, allowing shaft access.
    ring("mount_socket", .000, .060, .090, .165, "titanium", mount)
    ring("mount_gasket", .062, .076, .098, .148, "rubber", mount)
    ring("bearing_neck", .077, .255, .085, .112, "darksteel", mount)
    ring("rear_bearing", .250, .315, .077, .126, "copper", mount)
    for j in range(4):
        t=j*math.pi/2
        y,z=.228*math.cos(t),.228*math.sin(t)
        box("support_beam_%02d"%j,(.240,y,z),(.310,.037,.037),"titanium",mount)
        box("support_rivet_%02d"%j,(.110,y,z),(.032,.060,.060),"copper",mount)

    # Duct remains completely inside radius .33; rotor tip radius <= .25.
    ring("inlet_stator", .398, .475, .274, .321, "graphite", mount)
    ring("outlet_stator", .746, .812, .274, .321, "titanium", mount)
    ring("ceramic_inlet_lip", .388, .408, .276, .330, "ceramic", mount)
    ring("copper_outlet_trace", .800, .822, .286, .319, "copper", mount)
    for j in range(8):
        t=2*math.pi*j/8
        y,z=.318*math.cos(t),.318*math.sin(t)
        box("duct_heat_sink_%02d"%j,(.645,y,z),(.180,.018,.020),"darksteel",mount)
    # Rotor is free of all stationary surfaces: 0.240m blade tip vs 0.274m bore.
    rotor=empty("ROTOR_PIVOT_AXIS_X",mount,(.610,0,0))
    ring("rotor_hub",-.055,.065,.020,.069,"copper",rotor,steps=40)
    ring("rotor_front_collar",-.068,-.053,.030,.082,"titanium",rotor,steps=40)
    ring("rotor_rear_collar",.064,.078,.030,.080,"titanium",rotor,steps=40)
    for j in range(BLADES):
        ROTOR=blade("swept_blade_%02d"%j,2*math.pi*j/BLADES,rotor)
        ROTOR_MESHES.append(ROTOR)

    # Petals pivot on tangent axes attached to radial hinge frames, not on top.
    for j in range(PETALS):
        theta=2*math.pi*j/PETALS
        hinge=empty("HINGE_FRAME_%02d"%j,mount,
                    (.405,.390*math.cos(theta),.390*math.sin(theta)))
        hinge.rotation_euler[0]=theta
        pivot=empty("PETAL_TANGENT_Z_PIVOT_%02d"%j,hinge)
        PIVOTS.append(pivot)
        shell=arc_shell("scale_armour_%02d"%j,pivot,
                        .024,.410,.385,.415,.472,"graphite")
        SHELL_MESHES.append(shell)
        arc_shell("edge_inlay_%02d"%j,pivot,
                  .160,.220,.417,.425,.315,"copper")
        arc_shell("status_strip_%02d"%j,pivot,
                  .280,.310,.426,.432,.245,"cyan")
        # Outer hinge block sits outside the swept armour sector.
        box("hinge_lock_%02d"%j,(-.018,.072,0),
            (.045,.028,.060),"titanium",hinge)
        box("hinge_warning_%02d"%j,(-.040,.094,0),
            (.016,.011,.037),"amber",hinge)

    for frame,deg in [(1,0),(18,0),(48,30),(82,30),(111,0),(121,0)]:
        for p in PIVOTS:
            key(p,frame,"z",math.radians(deg))
    for frame,turn in [(1,0),(31,math.pi/2),(61,math.pi),
                       (91,3*math.pi/2),(121,2*math.pi)]:
        key(rotor,frame,"x",turn)
    for ob in [rotor]+PIVOTS:
        if ob.animation_data and ob.animation_data.action:
            for curve in ob.animation_data.action.fcurves:
                for point in curve.keyframe_points:
                    point.interpolation="LINEAR"

    scene=bpy.context.scene
    scene.frame_start=1
    scene.frame_end=121
    scene.render.fps=30
    scene.frame_set(1)
    bpy.context.view_layer.update()

    # Topology and animation envelope checks on actual evaluated vertex transforms.
    checked=0
    min_shell_rad=100
    max_rotor_rad=0
    min_angular_gap=100
    for frame in range(1,122,3):
        scene.frame_set(frame)
        bpy.context.view_layer.update()
        max_half_angle=0
        for j,o in enumerate(SHELL_MESHES):
            center=2*math.pi*j/PETALS
            for v in o.data.vertices:
                p=o.matrix_world@v.co
                rad=math.hypot(p.y,p.z)
                min_shell_rad=min(min_shell_rad,rad)
                diff=math.atan2(math.sin(math.atan2(p.z,p.y)-center),
                                math.cos(math.atan2(p.z,p.y)-center))
                max_half_angle=max(max_half_angle,abs(diff))
                checked+=1
        min_angular_gap=min(min_angular_gap,
                            2*math.pi/PETALS-2*max_half_angle)
        for o in ROTOR_MESHES:
            for v in o.data.vertices:
                p=o.matrix_world@v.co
                max_rotor_rad=max(max_rotor_rad,math.hypot(p.y,p.z))
                checked+=1
    assert min_shell_rad > .347, min_shell_rad
    assert max_rotor_rad < .252, max_rotor_rad
    assert min_angular_gap > .045, min_angular_gap
    assert len(MATERIALS)>=3
    assert len(MESHES)>=35
    scene.frame_set(1)
    bpy.context.view_layer.update()
    return dict(id=ID,meshes=len(MESHES),materials=len(MATERIALS),
                petals=PETALS,blades=BLADES,animation_frames=121,fps=30,
                sampled_frames=len(range(1,122,3)),
                transformed_vertex_checks=checked,
                min_shell_radial_distance_m=round(min_shell_rad,6),
                min_shell_stator_clearance_m=round(min_shell_rad-.330,6),
                min_rotor_bore_clearance_m=round(.274-max_rotor_rad,6),
                min_adjacent_petal_angular_gap_deg=round(
                    math.degrees(min_angular_gap),4),
                mount="SIDE X=0; axis +X; 1 unit = 1 metre",
                shell_deploy_deg=30,
                qa_geometry="PASS_SAMPLED_VERTEX_AND_ANGULAR_ENVELOPES",
                qa_limits="Not exact swept triangle-triangle or Unity physics QA")


def main():
    parser=argparse.ArgumentParser()
    parser.add_argument("--out",default="build/M04-019")
    args=parser.parse_args(sys.argv[sys.argv.index("--")+1:]
                           if "--" in sys.argv else [])
    out=Path(args.out).resolve()
    out.mkdir(parents=True,exist_ok=True)
    manifest=build()
    blend=out/(STEM+".blend")
    glb=out/(STEM+".glb")
    bpy.ops.wm.save_as_mainfile(filepath=str(blend))
    bpy.ops.export_scene.gltf(filepath=str(glb),export_format="GLB",
                              export_animations=True,export_apply=False)
    assert blend.stat().st_size>10000 and glb.stat().st_size>10000
    manifest["blend_bytes"]=blend.stat().st_size
    manifest["glb_bytes"]=glb.stat().st_size
    (out/(STEM+"_manifest.json")).write_text(
        json.dumps(manifest,indent=2,ensure_ascii=False)+"\n",
        encoding="utf-8")
    print("M04-019 QA",json.dumps(manifest,sort_keys=True))


if __name__=="__main__":
    main()
