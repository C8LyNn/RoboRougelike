#!/usr/bin/env python3
"""M01-017 Rapid Ball Thrower — deterministic Blender 3.6+/4.x model factory.

1 Blender unit = 1 metre. Blender: Z up, -Y muzzle forward.
GLB/FBX export: Y up, +Z forward. MOUNT_TOP_SOCKET is (0,0,0).
Visual-only asset; no gameplay/weapon logic.
Run: blender -b --python generate_m01_017.py -- --out DIR --repeat
"""
import argparse
import json
import math
import sys
from pathlib import Path

import bpy
import bmesh
from mathutils import Vector

MODEL_ID = "M01-017"
STEM = "M01-017_rapid_ball_thrower"
FRAMES = 73
PALETTE = {
    "Graphite": ((0.045, 0.060, 0.078, 1.0), 0.75, 0.38),
    "Titanium": ((0.38, 0.45, 0.49, 1.0), 0.83, 0.26),
    "CeramicWhite": ((0.84, 0.86, 0.78, 1.0), 0.20, 0.36),
    "Copper": ((0.64, 0.28, 0.10, 1.0), 0.84, 0.25),
    "SignalOrange": ((0.98, 0.31, 0.025, 1.0), 0.25, 0.32),
    "EmissiveCyan": ((0.018, 0.57, 0.76, 1.0), 0.12, 0.20),
    "Rubber": ((0.022, 0.028, 0.030, 1.0), 0.0, 0.85),
    "AmmoSteel": ((0.22, 0.27, 0.31, 1.0), 0.86, 0.20),
}
materials = {}
parts = []
mount = None
rotor = None


def reset_scene():
    global materials, parts, mount, rotor
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for mesh in list(bpy.data.meshes):
        bpy.data.meshes.remove(mesh)
    for material in list(bpy.data.materials):
        bpy.data.materials.remove(material)
    for action in list(bpy.data.actions):
        bpy.data.actions.remove(action)
    materials, parts = {}, []
    mount = rotor = None
    scene = bpy.context.scene
    scene.render.fps = 60
    scene.frame_start = 1
    scene.frame_end = FRAMES
    scene.unit_settings.system = "METRIC"
    scene.unit_settings.scale_length = 1.0
    scene.frame_set(1)


def build_materials():
    for name, (rgba, metallic, roughness) in PALETTE.items():
        mat = bpy.data.materials.new(MODEL_ID + "_" + name)
        mat.diffuse_color = rgba
        mat.use_nodes = True
        shader = mat.node_tree.nodes.get("Principled BSDF")
        shader.inputs["Base Color"].default_value = rgba
        shader.inputs["Metallic"].default_value = metallic
        shader.inputs["Roughness"].default_value = roughness
        if name == "EmissiveCyan" and "Emission Color" in shader.inputs:
            shader.inputs["Emission Color"].default_value = rgba
            shader.inputs["Emission Strength"].default_value = 2.0
        materials[name] = mat


def finish(obj, name, material, group="STATIC", parent=None, bevel=0.0):
    obj.name = MODEL_ID + "_" + name
    if bevel > 0.0:
        bpy.context.view_layer.objects.active = obj
        modifier = obj.modifiers.new("Manufactured_edges", "BEVEL")
        modifier.width = bevel
        modifier.segments = 2
        bpy.ops.object.modifier_apply(modifier=modifier.name)
    obj.data.materials.append(materials[material])
    obj["qa_group"] = group
    if parent is not None:
        obj.parent = parent
        obj.matrix_parent_inverse.identity()
    for poly in obj.data.polygons:
        poly.use_smooth = (len(poly.vertices) > 4 and material != "CeramicWhite")
    parts.append(obj)
    return obj


def cube(name, loc, dims, mat, group="STATIC", parent=None, bevel=0.0):
    bpy.ops.mesh.primitive_cube_add()
    obj = bpy.context.object
    obj.dimensions = dims
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    finish(obj, name, mat, group, parent, bevel)
    obj.location = loc
    return obj


def cylinder(name, loc, radius, depth, mat, group="STATIC", parent=None,
             rotation=(0.0, 0.0, 0.0), vertices=24):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth)
    obj = bpy.context.object
    finish(obj, name, mat, group, parent)
    obj.location = loc
    obj.rotation_euler = rotation
    return obj


def sphere(name, loc, radius, mat, group="STATIC", parent=None, segments=24, rings=12):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=rings, radius=radius)
    obj = bpy.context.object
    finish(obj, name, mat, group, parent)
    obj.location = loc
    for face in obj.data.polygons:
        face.use_smooth = True
    return obj


def torus(name, loc, major, minor, mat, group="STATIC", parent=None,
          rotation=(0.0, 0.0, 0.0)):
    bpy.ops.mesh.primitive_torus_add(major_segments=32, minor_segments=8,
                                    location=(0, 0, 0), rotation=(0, 0, 0),
                                    major_radius=major, minor_radius=minor)
    obj = bpy.context.object
    finish(obj, name, mat, group, parent)
    obj.location = loc
    obj.rotation_euler = rotation
    return obj


def rod(name, start, end, radius, mat, group="STATIC", parent=None):
    a, b = Vector(start), Vector(end)
    mid = (a + b) / 2.0
    direction = b - a
    obj = cylinder(name, mid, radius, direction.length, mat, group, parent, vertices=12)
    obj.rotation_euler = direction.to_track_quat("Z", "Y").to_euler()
    return obj


def hollow_barrel(name, loc, length, outer, inner, mat, parent):
    """One closed, manifold annular mesh with a genuinely open axial bore."""
    verts = []
    count = 32
    for radius, y in ((outer, -length/2), (outer, length/2),
                      (inner, -length/2), (inner, length/2)):
        for i in range(count):
            a = math.tau * i / count
            verts.append((radius * math.cos(a), y, radius * math.sin(a)))
    faces = []
    for i in range(count):
        j = (i+1) % count
        faces += [(i, count+i, count+j, j),
                  (2*count+i, 2*count+j, 3*count+j, 3*count+i),
                  (i, j, 2*count+j, 2*count+i),
                  (count+i, 3*count+i, 3*count+j, count+j)]
    mesh = bpy.data.meshes.new(name + "_mesh")
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    bm = bmesh.new()
    bm.from_mesh(mesh)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    finish(obj, name, mat, "STATIC", parent)
    obj.location = loc
    return obj


def build_scene():
    global mount, rotor
    reset_scene()
    build_materials()
    mount = bpy.data.objects.new("MOUNT_TOP_SOCKET", None)
    bpy.context.collection.objects.link(mount)
    mount.location = (0, 0, 0)
    mount.empty_display_type = "CUBE"
    mount.empty_display_size = 0.18

    # Broad mounting foot and stepped receiver: unmistakably a vehicle module.
    cube("Socket_foot", (0, 0.12, 0.125), (0.62, 0.76, 0.22),
         "Graphite", parent=mount, bevel=0.045)
    cylinder("Mount_lock", (0, 0.12, 0.02), 0.23, 0.045, "Titanium", parent=mount)
    cube("Receiver", (0, -0.30, 0.43), (0.61, 0.78, 0.29),
         "Titanium", parent=mount, bevel=0.05)
    cube("Receiver_top", (0, -0.30, 0.605), (0.43, 0.64, 0.09),
         "Graphite", parent=mount, bevel=0.02)
    for side in (-1, 1):
        cube("Side_ceramic_%s" % side, (side*0.355, -0.32, 0.48),
             (0.072, 0.53, 0.21), "CeramicWhite", parent=mount, bevel=0.018)
        cube("Side_orange_%s" % side, (side*0.396, -0.32, 0.53),
             (0.012, 0.23, 0.04), "SignalOrange", parent=mount, bevel=0.005)
        for j in range(3):
            cube("Heat_fin_%s_%s" % (side,j),
                 (side*0.29, -0.58+j*0.15, 0.665),
                 (0.04, 0.035, 0.12), "Copper", parent=mount, bevel=0.009)

    # Long hollow throwing bore and distinct muzzle geometry.
    hollow_barrel("Actual_hollow_bore", (0, -1.06, 0.58), 1.30,
                  0.145, 0.092, "Titanium", mount)
    torus("Muzzle_copper_ring", (0, -1.724, 0.58), 0.154, 0.017,
          "Copper", parent=mount, rotation=(math.pi/2,0,0))
    torus("Muzzle_warning_ring", (0, -1.68, 0.58), 0.157, 0.011,
          "SignalOrange", parent=mount, rotation=(math.pi/2,0,0))
    for j in range(4):
        torus("Barrel_collar_%02d" % j, (0, -1.42+j*0.27, 0.58),
              0.152, 0.018, "Graphite" if j%2 else "Copper",
              parent=mount, rotation=(math.pi/2,0,0))
    cube("Muzzle_sight", (0, -1.60, 0.77), (0.075,0.18,0.13),
         "CeramicWhite", parent=mount, bevel=0.012)

    # Globular feed hopper at the rear, visually dominant from a distance.
    cube("Hopper_pedestal", (0, 0.68, 0.42), (0.34, 0.39, 0.34),
         "Titanium", parent=mount, bevel=0.025)
    sphere("Spherical_hopper", (0, 0.72, 1.04), 0.425,
           "Graphite", parent=mount, segments=32, rings=16)
    torus("Hopper_meridian_1", (0, 0.72, 1.04), 0.45, 0.016,
          "Copper", parent=mount, rotation=(math.pi/2,0,0))
    torus("Hopper_meridian_2", (0, 0.72, 1.04), 0.45, 0.016,
          "Titanium", parent=mount, rotation=(0,math.pi/2,0))
    torus("Hopper_equator", (0, 0.72, 1.04), 0.45, 0.019,
          "CeramicWhite", parent=mount)
    cylinder("Hopper_fill_cap", (0,0.72,1.49),0.145,0.085,
             "CeramicWhite", parent=mount)
    torus("Fill_cap_signal", (0,0.72,1.54),0.105,0.013,
          "EmissiveCyan", parent=mount)
    for i in range(8):
        a = math.tau*i/8
        sphere("Hopper_rivet_%02d" % i,
               (0.27*math.cos(a), 0.72+0.27*math.sin(a), 1.33),
               0.033, "Copper", parent=mount, segments=12, rings=8)
    cube("Feed_neck", (0, 0.26, 0.76), (0.23,0.28,0.17),
         "Copper", parent=mount, bevel=0.02)
    cube("Inspection_plate", (-0.27,0.68,0.67), (0.065,0.29,0.14),
         "CeramicWhite", parent=mount, bevel=0.012)

    # Exposed right-hand spherical-ammunition carousel. Its entire swept
    # envelope is outside the receiver, hopper, mount and bearing support.
    rotor = bpy.data.objects.new("FEED_ROTOR_X_AXIS", None)
    bpy.context.collection.objects.link(rotor)
    rotor.parent = mount
    rotor.matrix_parent_inverse.identity()
    rotor.location = (0.67,-0.25,0.84)
    cylinder("Rotor_disc", (0,0,0), 0.24, 0.06, "Titanium",
             "ROTOR", rotor, rotation=(0,math.pi/2,0))
    cylinder("Rotor_hub", (0,0,0), 0.12, 0.11, "Copper",
             "ROTOR", rotor, rotation=(0,math.pi/2,0))
    torus("Rotor_outer_guard", (0,0,0), 0.425, 0.016,
          "Copper", "ROTOR", rotor, rotation=(0,math.pi/2,0))
    for i in range(8):
        a = math.tau*i/8
        y,z = 0.295*math.cos(a),0.295*math.sin(a)
        rod("Carousel_spoke_%02d" % i, (0,0,0), (0,y,z),
            0.025, "Titanium", "ROTOR", rotor)
        sphere("Steel_ball_%02d" % i, (0,y,z),0.092,
               "AmmoSteel","ROTOR",rotor,segments=20,rings=12)
        torus("Ball_locator_%02d" % i, (0,y,z),0.101,0.008,
              "EmissiveCyan","ROTOR",rotor,rotation=(0,math.pi/2,0))
    # Bearing is static and separated axially from all rotating geometry.
    cube("Bearing_pedestal", (0.91,-0.25,0.24),(0.13,0.42,0.24),
         "Graphite",parent=mount,bevel=0.017)
    rod("Bearing_upright", (0.91,-0.25,0.27),(0.91,-0.25,0.80),
        0.042,"Titanium",parent=mount)
    cylinder("Bearing_outer", (0.91,-0.25,0.84),0.16,0.085,
             "CeramicWhite",parent=mount,rotation=(0,math.pi/2,0))
    cylinder("Bearing_cap", (0.97,-0.25,0.84),0.092,0.026,
             "Copper",parent=mount,rotation=(0,math.pi/2,0))
    for j in range(3):
        cube("Bearing_fins_%d" % j,(0.91,-0.35+j*0.1,0.36),
             (0.11,0.022,0.11),"Copper",parent=mount,bevel=0.008)

    # Feed rotor spins six full revolutions, 60 FPS, continuously sampled.
    for frame in range(1,FRAMES+1,3):
        rotor.rotation_euler = (math.tau*(frame-1)/12.0,0.0,0.0)
        rotor.keyframe_insert(data_path="rotation_euler",frame=frame,group="Feeder")
    for fcurve in rotor.animation_data.action.fcurves:
        for point in fcurve.keyframe_points:
            point.interpolation = "LINEAR"
    bpy.context.scene.frame_set(1)
    bpy.context.view_layer.update()
    return len(parts), len(materials)


def bounds(obj):
    corners = [obj.matrix_world @ Vector(corner) for corner in obj.bound_box]
    return ([min(v[i] for v in corners) for i in range(3)],
            [max(v[i] for v in corners) for i in range(3)])


def separation(a,b):
    low_a, high_a = a
    low_b, high_b = b
    delta = [max(low_a[i]-high_b[i],low_b[i]-high_a[i],0.0) for i in range(3)]
    return math.sqrt(sum(d*d for d in delta))


def validate_geometry():
    for obj in parts:
        mesh = obj.data
        bm = bmesh.new()
        bm.from_mesh(mesh)
        bm.normal_update()
        if not bm.faces or not all(edge.is_manifold for edge in bm.edges):
            raise AssertionError("Non-manifold mesh: "+obj.name)
        if not all(face.calc_area() > 1e-10 for face in bm.faces):
            raise AssertionError("Degenerate face: "+obj.name)
        if bm.calc_volume(signed=False) <= 1e-11:
            raise AssertionError("Zero-volume mesh: "+obj.name)
        bm.free()


def validate_motion():
    static = [obj for obj in parts if obj["qa_group"] == "STATIC"]
    moving = [obj for obj in parts if obj["qa_group"] == "ROTOR"]
    if len(moving) < 20 or len(static) < 20:
        raise AssertionError("Incomplete static/moving geometry")
    scene = bpy.context.scene
    scene.frame_set(1)
    bpy.context.view_layer.update()
    fixed = [(obj.name,bounds(obj)) for obj in static]
    checks = 0
    closest = 1e6
    for frame in range(1,FRAMES+1):
        scene.frame_set(frame)
        bpy.context.view_layer.update()
        for obj in moving:
            bb = bounds(obj)
            for name, other in fixed:
                gap = separation(bb,other)
                checks += 1
                closest = min(closest,gap)
                if gap <= 0.00001:
                    raise AssertionError("AABB collision frame %d: %s vs %s" %
                                         (frame,obj.name,name))
    scene.frame_set(1)
    return checks, closest


def export_scene(folder):
    folder.mkdir(parents=True,exist_ok=True)
    stem = folder/STEM
    bpy.ops.wm.save_as_mainfile(filepath=str(stem)+".blend")
    bpy.ops.export_scene.gltf(filepath=str(stem)+".glb",export_format="GLB",
                              export_yup=True,export_animations=True)
    bpy.ops.export_scene.fbx(filepath=str(stem)+".fbx",axis_forward="-Z",
                             axis_up="Y",bake_anim=True,add_leaf_bones=False)
    if hasattr(bpy.ops.wm,"obj_export"):
        bpy.ops.wm.obj_export(filepath=str(stem)+".obj",export_materials=True,
                              forward_axis="NEGATIVE_Z",up_axis="Y")
    else:
        bpy.ops.export_scene.obj(filepath=str(stem)+".obj",use_materials=True,
                                 axis_forward="-Z",axis_up="Y")
    return stem


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--out",default="generated")
    parser.add_argument("--repeat",action="store_true")
    args = parser.parse_args(sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else [])
    first = build_scene()
    if args.repeat:
        second = build_scene()
        assert first == second, "Generator not repeatable"
    validate_geometry()
    checks,closest = validate_motion()
    assert len(materials) >= 3
    assert closest > 0.005, "Insufficient moving/static clearance"
    stem = export_scene(Path(args.out).resolve())
    vertices = sum(len(o.data.vertices) for o in parts)
    triangles = sum(len(p.vertices)-2 for o in parts for p in o.data.polygons)
    report = {
        "id":MODEL_ID,"model":"Szybkostrzelny miotacz kul",
        "status":"BLENDER_QA_PASS","meshes":len(parts),"materials":len(materials),
        "vertices":vertices,"triangles":triangles,"frames_tested":FRAMES,
        "aabb_checks":checks,"aabb_intersections":0,
        "min_aabb_clearance_m":round(closest,6),
        "rotor_axis":"Blender local X; 6 turns / 73 frames / 60 FPS",
        "mount":"MOUNT_TOP_SOCKET at origin (0,0,0)",
        "limitations":"AABB sampled at frames, not continuous mesh collision or Unity QA"
    }
    Path(str(stem)+"_qa.json").write_text(json.dumps(report,indent=2)+"\n",encoding="utf-8")
    print("MODEL_FACTORY_REPORT="+json.dumps(report,sort_keys=True))


if __name__ == "__main__":
    main()
