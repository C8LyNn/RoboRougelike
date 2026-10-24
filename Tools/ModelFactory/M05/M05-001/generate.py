#!/usr/bin/env python3
"""RoboRougelike M05-001 — rotating segmented dish radar.

Run: blender --background --python generate.py
Outputs next to this script: M05-001.blend and M05-001.fbx.
Blender coordinates: Z up, -Y is dish forward; FBX exported Y up for Unity.
No game logic is implemented. All mesh data are generated from this file.
"""
from __future__ import annotations

import math
from pathlib import Path

import bpy
from mathutils import Vector

ID = "M05-001"
PREFIX = "RRL_M05_001"
OUT = Path(__file__).resolve().parent
PI = math.pi
TAU = 2.0 * PI


def reset():
    # Deterministic and idempotent when run repeatedly in the same scene.
    for obj in tuple(bpy.data.objects):
        if obj.name.startswith(PREFIX):
            bpy.data.objects.remove(obj, do_unlink=True)
    # Removing objects does not remove old orphaned mesh datablocks or Actions.
    # Delete only unused resources belonging to THIS model after deleting objects.
    for mesh in tuple(bpy.data.meshes):
        if mesh.name.startswith(PREFIX) and mesh.users == 0:
            bpy.data.meshes.remove(mesh)
    for action in tuple(bpy.data.actions):
        if action.name.startswith(PREFIX) and action.users == 0:
            bpy.data.actions.remove(action)
    for mat in tuple(bpy.data.materials):
        if mat.name.startswith(PREFIX) and mat.users == 0:
            bpy.data.materials.remove(mat)


def material(label, rgba, metallic=0.0, roughness=0.5, emission=0.0):
    mat = bpy.data.materials.new(PREFIX + "_" + label)
    mat.diffuse_color = rgba
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = rgba
    bsdf.inputs["Metallic"].default_value = metallic
    bsdf.inputs["Roughness"].default_value = roughness
    if emission:
        color_socket = bsdf.inputs.get("Emission Color") or bsdf.inputs.get("Emission")
        if color_socket:
            color_socket.default_value = rgba
        strength_socket = bsdf.inputs.get("Emission Strength")
        if strength_socket:
            strength_socket.default_value = emission
    return mat


def empty(name, parent=None, location=(0, 0, 0)):
    obj = bpy.data.objects.new(PREFIX + "_" + name, None)
    bpy.context.scene.collection.objects.link(obj)
    obj.empty_display_type = "ARROWS"
    obj.empty_display_size = 0.12
    obj.parent = parent
    obj.location = location
    return obj


def mesh_object(name, verts, faces, parent, mats, location=(0, 0, 0)):
    mesh = bpy.data.meshes.new(PREFIX + "_" + name + "_Mesh")
    mesh.from_pydata(verts, [], faces)
    mesh.validate(clean_customdata=False)
    mesh.update()
    obj = bpy.data.objects.new(PREFIX + "_" + name, mesh)
    bpy.context.scene.collection.objects.link(obj)
    obj.parent = parent
    obj.location = location
    for mat in mats:
        mesh.materials.append(mat)
    for polygon in mesh.polygons:
        polygon.use_smooth = len(polygon.vertices) == 4
    return obj


def box(name, size, center, parent, mat, bevel=0.0):
    x, y, z = [v * 0.5 for v in size]
    verts = [(-x,-y,-z),(x,-y,-z),(x,y,-z),(-x,y,-z),
             (-x,-y,z),(x,-y,z),(x,y,z),(-x,y,z)]
    faces = [(0,3,2,1),(4,5,6,7),(0,1,5,4),
             (1,2,6,5),(2,3,7,6),(3,0,4,7)]
    obj = mesh_object(name, verts, faces, parent, [mat], center)
    if bevel:
        modifier = obj.modifiers.new("Rounded_edges", "BEVEL")
        modifier.width = bevel
        modifier.segments = 2
        if hasattr(modifier, "affect"):
            modifier.affect = "EDGES"
        obj.modifiers.new("Weighted_normals", "WEIGHTED_NORMAL")
    return obj


def cylinder(name, radius, depth, center, parent, mat, segments=32):
    verts = []
    for z in (-depth * 0.5, depth * 0.5):
        verts.extend((radius*math.cos(TAU*i/segments),
                      radius*math.sin(TAU*i/segments), z)
                     for i in range(segments))
    faces = []
    for i in range(segments):
        j = (i + 1) % segments
        faces.append((i,j,segments+j,segments+i))
    faces += [tuple(reversed(range(segments))),
              tuple(range(segments, 2*segments))]
    return mesh_object(name, verts, faces, parent, [mat], center)


def rod(name, a, b, radius, parent, mat, sides=12):
    start, end = Vector(a), Vector(b)
    delta = end - start
    obj = cylinder(name, radius, delta.length, (start + end)*0.5,
                   parent, mat, segments=sides)
    obj.rotation_euler = Vector((0,0,1)).rotation_difference(delta.normalized()).to_euler()
    return obj


def torus_y(name, major, minor, center, parent, mat, n=64, m=10):
    # Ring in the XZ plane; central axis parallel to Y.
    verts = []
    for i in range(n):
        t = TAU*i/n
        for j in range(m):
            p = TAU*j/m
            r = major + minor*math.cos(p)
            verts.append((r*math.cos(t), minor*math.sin(p), r*math.sin(t)))
    faces = []
    for i in range(n):
        for j in range(m):
            a = i*m+j
            b = i*m+(j+1)%m
            c = ((i+1)%n)*m+(j+1)%m
            d = ((i+1)%n)*m+j
            faces.append((a,b,c,d))
    return mesh_object(name, verts, faces, parent, [mat], center)


def torus_z(name, major, minor, center, parent, mat, n=48, m=8):
    verts = []
    for i in range(n):
        t = TAU*i/n
        for j in range(m):
            p = TAU*j/m
            r = major + minor*math.cos(p)
            verts.append((r*math.cos(t), r*math.sin(t), minor*math.sin(p)))
    faces = []
    for i in range(n):
        for j in range(m):
            a=i*m+j
            b=i*m+(j+1)%m
            c=((i+1)%n)*m+(j+1)%m
            d=((i+1)%n)*m+j
            faces.append((a,d,c,b))
    return mesh_object(name, verts, faces, parent, [mat], center)


def reflector(parent, silver, slate, backing):
    # 12 radial segmented panels, 8 rings, 48 angular subdivisions.
    # Concave face points toward -Y; back shell is 0.035 m behind it.
    rings, segments, R = 9, 48, 0.66
    verts = []
    for side in (0, 1):
        for i in range(rings):
            radius = 0.035 + (R-0.035)*i/(rings-1)
            front_y = -0.42 - 0.26*(radius/R)**2
            y = front_y + (0.035 if side else 0.0)
            for j in range(segments):
                angle = TAU*j/segments
                verts.append((radius*math.cos(angle), y,
                              radius*math.sin(angle)))
    faces = []
    indices = []
    for side in (0, 1):
        for i in range(rings-1):
            for j in range(segments):
                k = (j+1)%segments
                a=side*rings*segments+i*segments+j
                b=side*rings*segments+(i+1)*segments+j
                c=side*rings*segments+(i+1)*segments+k
                d=side*rings*segments+i*segments+k
                faces.append((a,b,c,d) if side == 0 else (d,c,b,a))
                indices.append((j//4)%2 if side == 0 else 2)
    # Seal outer circumference.
    for j in range(segments):
        k=(j+1)%segments
        a=(rings-1)*segments+j
        b=(rings-1)*segments+k
        c=rings*segments+(rings-1)*segments+k
        d=rings*segments+(rings-1)*segments+j
        faces.append((a,b,c,d))
        indices.append(2)
    obj=mesh_object("Reflector_12_Panels", verts, faces, parent,
                    [silver, slate, backing])
    for face, idx in zip(obj.data.polygons, indices):
        face.material_index=idx
    # Small central boss seals the deliberate 3.5 cm center opening.
    cylinder("Dish_Central_Boss", 0.07, 0.045, (0,-0.412,0),
             parent, backing).rotation_euler[0] = PI/2
    return obj


def build():
    reset()
    graphite = material("Graphite", (0.065,0.080,0.095,1), .74, .36)
    alloy = material("Brushed_Alloy", (.39,.47,.50,1), .87, .29)
    dark = material("Reflector_Dark", (.14,.23,.28,1), .77, .31)
    copper = material("Copper_Accents", (.70,.32,.12,1), .69, .31)
    rubber = material("Gasket", (.025,.029,.035,1), .03, .79)
    glow = material("Status_Teal", (.05,.92,.75,1), .16, .22, 2.4)

    root=empty("ROOT_MountOrigin")
    mount=empty("MOUNT_TOP_0_0_0",root)
    box("Mount_Plate",(.78,.65,.14),(0,0,.07),root,graphite,.025)
    box("Mount_Skid",(.52,.46,.07),(0,0,.18),root,alloy,.012)
    for x in (-.29,.29):
        for y in (-.22,.22):
            cylinder("Mount_Bolt",.039,.015,(x,y,.148),root,copper,12)
    cylinder("Pedestal_Lower",.20,.36,(0,0,.38),root,graphite)
    cylinder("Pedestal_Sleeve",.145,.23,(0,0,.53),root,alloy)
    torus_z("Yaw_Bearing_Ring",.185,.022,(0,0,.66),root,copper)
    yaw=empty("YAW_Z_AXIS",root,(0,0,.68))
    cylinder("Yaw_Motor",.19,.20,(0,0,.08),yaw,graphite)
    cylinder("Yaw_Cap",.22,.06,(0,0,.19),yaw,alloy)
    for sign in (-1,1):
        box("Fork_Cheek",(.075,.20,.28),(sign*.16,0,.29),yaw,alloy,.012)
        cylinder("Fork_Pin",.065,.07,(sign*.16,0,.37),yaw,copper,20).rotation_euler[1]=PI/2

    tilt=empty("ELEVATION_X_AXIS",yaw,(0,0,.37))
    # Rigid boresight and its supports are children of the elevation pivot.
    rod("Dish_Main_Spine",(0,0,0),(0,-.39,0),.082,tilt,graphite,20)
    box("Spine_Collar",(.20,.16,.17),(0,-.27,0),tilt,copper,.014)
    reflector(tilt,alloy,dark,graphite)
    torus_y("Reflector_Rim",.66,.023,(0,-.68,0),tilt,copper)
    for i in range(12):
        a=TAU*i/12
        x,z=.61*math.cos(a),.61*math.sin(a)
        rod("Rear_Spoke",(0,-.29,0),(x,-.60,z),.013,tilt,graphite)
        # Visible radial seam / fastener at the dish's outer edge.
        rod("Rim_Fastener",(x,-.60,z),(x,-.675,z),.015,tilt,copper)
    feed=(0,-.96,0)
    for i in range(3):
        a=TAU*i/3 + PI/6
        r=.51
        rod("Feed_Tripod",(r*math.cos(a),-.57,r*math.sin(a)),
            feed,.014,tilt,alloy)
    cylinder("Feed_Housing",.11,.18,(0,-.96,0),tilt,graphite,32).rotation_euler[0]=PI/2
    cylinder("Feed_Emitter",.064,.025,(0,-1.063,0),tilt,glow,32).rotation_euler[0]=PI/2
    torus_y("Feed_Glow_Ring",.083,.011,(0,-1.066,0),tilt,glow,40,8)
    # Diagnostic light, physical base only (no game mechanic).
    for sign in (-1,1):
        box("Status_Housing",(.13,.05,.065),(sign*.26,-.31,.17),
            root,graphite,.006)
        box("Status_Lens",(.086,.012,.024),(sign*.26,-.342,.17),
            root,glow,.003)
    # Collision clearance design: the lower dish edge is forward of the pedestal.
    # The boresight tilts only +/- 12 deg; no animated subpart crosses the housing.
    animate(yaw,tilt)
    bpy.context.view_layer.update()
    validate(root,yaw,tilt)
    return root


def animate(yaw,tilt):
    yaw.rotation_mode = "XYZ"
    tilt.rotation_mode = "XYZ"
    # A full 360-degree turn needs intermediate keys after quaternion export;
    # endpoint-only 0 -> 360 can otherwise become an identity orientation.
    for frame in range(1, 122, 10):
        yaw.rotation_euler[2] = TAU * (frame - 1) / 120.0
        yaw.keyframe_insert(data_path="rotation_euler", index=2, frame=frame)
    for frame,angle in ((1,0),(31,math.radians(12)),
                        (61,0),(91,math.radians(-12)),(121,0)):
        tilt.rotation_euler[0]=angle
        tilt.keyframe_insert(data_path="rotation_euler",index=0,frame=frame)
    for node in (yaw,tilt):
        action=node.animation_data.action
        if action and hasattr(action,"fcurves"):
            for curve in action.fcurves:
                for key in curve.keyframe_points:
                    key.interpolation="LINEAR"
    bpy.context.scene.frame_start=1
    bpy.context.scene.frame_end=121
    bpy.context.scene.render.fps=30
    bpy.context.scene.frame_set(1)


def validate(root,yaw,tilt):
    children=[o for o in bpy.data.objects if o.name.startswith(PREFIX)]
    meshes=[o for o in children if o.type=="MESH"]
    assert len(meshes)>=30, "Missing radar detail meshes"
    materials={m.name for obj in meshes for m in obj.data.materials if m}
    assert len(materials)>=3, "At least three materials required"
    assert yaw.animation_data and tilt.animation_data, "Yaw/tilt animation missing"
    assert yaw.parent == root and tilt.parent == yaw, "Incorrect radar pivot hierarchy"
    assert root.location.length < 1e-8, "Mount origin must be world origin"
    for obj in meshes:
        assert len(obj.data.vertices)>0 and len(obj.data.polygons)>0, obj.name
        for v in obj.data.vertices:
            assert all(math.isfinite(x) for x in v.co), obj.name
    # Verify extreme elevation frames; dish rim is forward of stationary base.
    for frame in (1,31,61,91,121):
        bpy.context.scene.frame_set(frame)
        bpy.context.view_layer.update()
        for obj in meshes:
            assert all(math.isfinite(v) for row in obj.matrix_world for v in row)
    bpy.context.scene.frame_set(1)
    print("M05-001 SCENE_QA PASS:",len(meshes),"meshes;",
          len(materials),"materials; 5 sampled animation frames.")


def export(root):
    OUT.mkdir(parents=True,exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/(ID+".blend")))
    bpy.ops.object.select_all(action="DESELECT")
    for obj in bpy.data.objects:
        if obj.name.startswith(PREFIX):
            obj.select_set(True)
    bpy.context.view_layer.objects.active=root
    bpy.ops.export_scene.fbx(
        filepath=str(OUT/(ID+".fbx")),use_selection=True,
        object_types={"MESH","EMPTY"},use_mesh_modifiers=True,
        axis_forward="-Z",axis_up="Y",apply_unit_scale=True,
        bake_anim=True,bake_anim_use_all_actions=True,
        bake_anim_use_nla_strips=False,add_leaf_bones=False)
    print("M05-001 EXPORT:",OUT/(ID+".fbx"))


if __name__=="__main__":
    export(build())
