#!/usr/bin/env python3
"""M05-022 — visual-only kinetic energy recovery flywheel for RoboRougelike.
Blender 4.x/5.x. 1 Blender unit = 1 metre; Z up, +Y shaft axis.
Usage: blender -b -t 2 --python generate_m05_022.py -- --out /tmp/m05-022
Owns ONLY M05-022_KineticRecovery collection and M05-022_ materials.
"""
import argparse
import json
import math
import os
import sys

import bpy
import bmesh
from mathutils import Vector

ID = "M05-022"
COLLECTION = ID + "_KineticRecovery"
FRAME_END = 61
ROTOR_CENTRE_Z = 0.62
GUARD_INNER_RADIUS = 0.405
created = []
owned = None


def clean_owned():
    old = bpy.data.collections.get(COLLECTION)
    if old:
        for obj in list(old.objects):
            if obj.get("model_factory_id") == ID:
                mesh = obj.data if obj.type == "MESH" else None
                bpy.data.objects.remove(obj, do_unlink=True)
                if mesh is not None and mesh.users == 0:
                    bpy.data.meshes.remove(mesh)
            else:
                old.objects.unlink(obj)
                bpy.context.scene.collection.objects.link(obj)
        bpy.data.collections.remove(old)
    for material in list(bpy.data.materials):
        if material.name.startswith(ID + "_") and material.users == 0:
            bpy.data.materials.remove(material)


def mark(obj, parent=None):
    for col in list(obj.users_collection):
        col.objects.unlink(obj)
    owned.objects.link(obj)
    obj["model_factory_id"] = ID
    if parent:
        obj.parent = parent
    created.append(obj)
    return obj


def empty(name, position=(0, 0, 0), parent=None):
    obj = bpy.data.objects.new(ID + "_" + name, None)
    owned.objects.link(obj)
    obj["model_factory_id"] = ID
    obj.empty_display_type = "PLAIN_AXES"
    obj.empty_display_size = 0.07
    if parent:
        obj.parent = parent
    obj.location = position
    created.append(obj)
    return obj


def mat(name, color, metallic=0, roughness=0.4, emission=0):
    m = bpy.data.materials.new(ID + "_" + name)
    m.diffuse_color = (*color, 1)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (*color, 1)
    bsdf.inputs["Metallic"].default_value = metallic
    bsdf.inputs["Roughness"].default_value = roughness
    if emission:
        key = bsdf.inputs.get("Emission Color") or bsdf.inputs.get("Emission")
        if key:
            key.default_value = (*color, 1)
        strength = bsdf.inputs.get("Emission Strength")
        if strength:
            strength.default_value = emission
    return m


def finish(obj, material):
    obj.data.materials.append(material)
    if obj.type == "MESH" and len(obj.data.polygons) > 20:
        for face in obj.data.polygons:
            face.use_smooth = len(face.vertices) == 4
    return obj


def cube(name, pos, dims, material, parent=None, bevel=0.008):
    bpy.ops.mesh.primitive_cube_add(size=1, location=(0, 0, 0))
    obj = mark(bpy.context.object, parent)
    obj.name = ID + "_" + name
    obj.location = pos
    obj.dimensions = dims
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    finish(obj, material)
    if bevel:
        b = obj.modifiers.new("Machined_edges", "BEVEL")
        b.width = bevel
        b.segments = 2
        obj.modifiers.new("Weighted_normals", "WEIGHTED_NORMAL")
    return obj


def cylinder(name, pos, radius, depth, material, parent=None, sides=32, axis="Z"):
    bpy.ops.mesh.primitive_cylinder_add(vertices=sides, radius=radius, depth=depth)
    obj = mark(bpy.context.object, parent)
    obj.name = ID + "_" + name
    obj.location = pos
    if axis == "Y":
        obj.rotation_euler.x = math.pi / 2
    finish(obj, material)
    return obj


def ring(name, inner, outer, depth, pos, material, parent=None, sides=48, axis="Y"):
    """Closed, consistently oriented annular solid, with genuine hollow centre."""
    verts = []
    for radius, z in ((outer, -depth/2), (outer, depth/2),
                      (inner, -depth/2), (inner, depth/2)):
        for i in range(sides):
            a = 2*math.pi*i/sides
            verts.append((radius*math.cos(a), radius*math.sin(a), z))
    faces = []
    for i in range(sides):
        j = (i+1) % sides
        faces.extend(((i, j, sides+j, sides+i),
                      (2*sides+j, 2*sides+i, 3*sides+i, 3*sides+j),
                      (sides+i, sides+j, 3*sides+j, 3*sides+i),
                      (j, i, 2*sides+i, 2*sides+j)))
    mesh = bpy.data.meshes.new(ID + "_" + name + "_mesh")
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(ID + "_" + name, mesh)
    owned.objects.link(obj)
    obj["model_factory_id"] = ID
    if parent:
        obj.parent = parent
    obj.location = pos
    if axis == "Y":
        obj.rotation_euler.x = math.pi/2
    created.append(obj)
    bm = bmesh.new()
    bm.from_mesh(mesh)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(mesh)
    bm.free()
    finish(obj, material)
    return obj


def rod(name, start, end, radius, material, parent=None, sides=16):
    a, b = Vector(start), Vector(end)
    direction = b-a
    obj = cylinder(name, tuple((a+b)*0.5), radius, direction.length,
                   material, parent, sides=sides)
    obj.rotation_euler = direction.to_track_quat("Z", "Y").to_euler()
    return obj


def verify_topology():
    for obj in created:
        if obj.type != "MESH":
            continue
        bm = bmesh.new()
        bm.from_mesh(obj.data)
        bad = [edge for edge in bm.edges if not edge.is_manifold]
        assert not bad, "%s has %d nonmanifold edges" % (obj.name, len(bad))
        assert bm.verts and bm.faces, "empty mesh: " + obj.name
        bm.free()


def verify_motion(rotor):
    scene = bpy.context.scene
    moving = [obj for obj in created if obj.type == "MESH" and obj.parent == rotor]
    assert moving, "no moving flywheel meshes"
    minimum_clearance = 100
    max_radius = 0
    for frame in range(1, FRAME_END+1):
        scene.frame_set(frame)
        bpy.context.view_layer.update()
        for obj in moving:
            matrix = obj.matrix_world
            for vertex in obj.data.vertices:
                p = matrix @ vertex.co
                radius = math.hypot(p.x, p.z-ROTOR_CENTRE_Z)
                max_radius = max(max_radius, radius)
                minimum_clearance = min(minimum_clearance, GUARD_INNER_RADIUS-radius)
                assert radius < GUARD_INNER_RADIUS-0.015, (
                    "rotor intersects stationary cage: %s frame %d" % (obj.name, frame))
                assert p.z > 0.19, "rotor intersects floor mount"
    assert abs(rotor.rotation_euler.y-2*math.pi) < 1e-5
    scene.frame_set(1)
    return {"frames_checked": FRAME_END, "moving_meshes": len(moving),
            "max_rotor_radius_m": round(max_radius, 5),
            "min_rotor_to_guard_radial_clearance_m": round(minimum_clearance, 5)}


def build():
    global owned
    clean_owned()
    created.clear()
    owned = bpy.data.collections.new(COLLECTION)
    bpy.context.scene.collection.children.link(owned)

    graphite = mat("graphite", (0.065, 0.080, 0.105), 0.65, 0.42)
    titanium = mat("titanium", (0.52, 0.60, 0.64), 0.83, 0.26)
    steel = mat("brushed_steel", (0.30, 0.36, 0.40), 0.78, 0.32)
    copper = mat("copper", (0.68, 0.31, 0.08), 0.76, 0.29)
    ceramic = mat("ceramic_ivory", (0.77, 0.81, 0.76), 0.16, 0.39)
    rubber = mat("rubber", (0.032, 0.040, 0.046), 0, 0.84)
    cyan = mat("cyan_status", (0.035, 0.61, 0.90), 0, 0.23, 3.5)
    amber = mat("amber_warning", (1.0, 0.37, 0.025), 0, 0.27, 2.8)
    dark_glass = mat("smoked_glass", (0.09, 0.16, 0.19), 0.35, 0.16)

    root = empty("MOUNT_ROOT")
    root["mount_type"] = "internal_frame"
    root["units"] = "metres"
    root["up_axis"] = "+Z"
    root["shaft_axis"] = "+Y"
    cube("base_foot", (0, 0, 0.095), (1.22, 0.83, 0.15), graphite, root, 0.025)
    cube("base_armor", (0, 0, 0.181), (0.94, 0.67, 0.025), ceramic, root)
    for x in (-0.50, 0.50):
        for y in (-0.32, 0.32):
            cylinder("mount_bolt_%s_%s" % (x,y), (x,y,0.184),
                     0.026, 0.020, titanium, root, sides=12)
            empty("MOUNT_BOLT_%s_%s" % (x,y), (x,y,0), root)

    # Four rubber vibration isolators remain clear of the moving rotor.
    for x in (-0.26, 0.26):
        for y in (-0.22, 0.22):
            cube("vibration_isolator_%s_%s" % (x,y),
                 (x,y,0.208), (0.085,0.072,0.028), rubber, root, 0.006)

    for side in (-1, 1):
        cube("side_pylon_%s" % side, (side*0.55,0,0.62),
             (0.072,0.42,0.78), steel, root, 0.015)
        cube("side_armor_%s" % side, (side*0.60,0,0.65),
             (0.020,0.35,0.42), ceramic, root)
        cube("status_backing_%s" % side, (side*0.615,-0.14,0.62),
             (0.015,0.08,0.20), dark_glass, root, 0.004)
        cube("status_led_%s" % side, (side*0.625,-0.14,0.67),
             (0.009,0.040,0.07), cyan if side==1 else amber, root, 0.002)

    cube("upper_bridge", (0,0,1.15), (1.12,0.25,0.065), titanium, root)
    cube("lower_service_rail", (0,-0.37,0.23), (0.95,0.045,0.09), copper, root)
    # Cage is a true hollow annulus; radial clearance from rotating flywheel >= 40 mm.
    ring("fixed_guard_outer", GUARD_INNER_RADIUS, 0.455, 0.23,
         (0,0,ROTOR_CENTRE_Z), steel, root)
    ring("fixed_guard_front_edge", 0.406, 0.469, 0.022,
         (0,-0.133,ROTOR_CENTRE_Z), titanium, root)
    ring("fixed_guard_rear_edge", 0.406, 0.469, 0.022,
         (0,0.133,ROTOR_CENTRE_Z), titanium, root)
    for side in (-1,1):
        y = side*0.225
        ring("bearing_shell_%s" % side, 0.077, 0.120, 0.07,
             (0,y,ROTOR_CENTRE_Z), graphite, root)
        ring("bearing_race_%s" % side, 0.069, 0.077, 0.074,
             (0,y,ROTOR_CENTRE_Z), copper, root)
        empty("MOUNT_SHAFT_%s" % side, (0,side*0.29,ROTOR_CENTRE_Z), root)
    # Four thin cage struts stand in front/behind the rotor, never through it.
    for y in (-0.185,0.185):
        for x in (-0.38,0.38):
            rod("cage_strut_%s_%s" % (y,x),
                (x,y,0.43),(x,y,0.81),0.011,titanium,root)

    rotor = empty("PIVOT_FLYWHEEL_Y", (0,0,ROTOR_CENTRE_Z), root)
    rotor["animation_axis"] = "local +Y"
    rotor["speed_note"] = "visual 360 degrees per 60 frames; gameplay controls speed"
    ring("flywheel_heavy_rim", 0.274, 0.355, 0.160,
         (0,0,0), steel, rotor)
    ring("flywheel_copper_front", 0.358, 0.365, 0.014,
         (0,-0.057,0), copper, rotor)
    ring("flywheel_copper_back", 0.358, 0.365, 0.014,
         (0,0.057,0), copper, rotor)
    cylinder("flywheel_hub", (0,0,0), 0.125, 0.175,
             titanium, rotor, axis="Y")
    cylinder("flywheel_shaft", (0,0,0), 0.055, 0.565,
             steel, rotor, axis="Y")
    for i in range(8):
        angle = 2*math.pi*i/8
        x,z = math.cos(angle),math.sin(angle)
        rod("radial_spoke_%02d" % i,
            (0.105*x,0,0.105*z), (0.296*x,0,0.296*z),
            0.026, titanium, rotor)
        weight = cube("weighted_segment_%02d" % i,
                      (0.314*x,0,0.314*z), (0.072,0.164,0.058),
                      graphite if i%2 else ceramic, rotor, 0.007)
        weight.rotation_euler.y = -angle
        cylinder("weight_rivet_%02d" % i,
                 (0.319*x,-0.091,0.319*z),0.014,0.015,
                 copper,rotor,sides=12,axis="Y")
    for side in (-1,1):
        ring("hub_face_%s" % side, 0.058,0.108,0.018,
             (0,side*0.102,0),copper,rotor)
    for f, degrees in ((1,0),(31,180),(61,360)):
        rotor.rotation_euler.y = math.radians(degrees)
        rotor.keyframe_insert(data_path="rotation_euler",frame=f,group="Flywheel")
    action = rotor.animation_data.action
    if action and hasattr(action,"fcurves"):
        for curve in action.fcurves:
            for key in curve.keyframe_points:
                key.interpolation = "LINEAR"
    bpy.context.scene.frame_start = 1
    bpy.context.scene.frame_end = FRAME_END
    verify_topology()
    qa = verify_motion(rotor)
    qa.update({"id":ID, "status":"BLENDER_QA_PASS", "materials":9,
               "mesh_count":sum(o.type=="MESH" for o in created),
               "object_count":len(created), "mount_origin_m":[0,0,0],
               "rotor_pivot_m":[0,0,ROTOR_CENTRE_Z],
               "rotor_axis":"local +Y", "visual_only":True,
               "test_scope":"closed manifold meshes and swept rotor-to-guard radial clearance"})
    return qa


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--out",default=os.path.join(os.path.dirname(__file__),"output"))
    args = parser.parse_args(sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else [])
    os.makedirs(args.out,exist_ok=True)
    qa = build()
    base = os.path.join(args.out,ID)
    bpy.context.scene.frame_set(1)
    bpy.ops.wm.save_as_mainfile(filepath=base+".blend")
    qa["blend_export"] = "PASS"
    bpy.ops.object.select_all(action="DESELECT")
    for obj in created:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = created[0]
    try:
        bpy.ops.export_scene.gltf(filepath=base+".glb",export_format="GLB",
                                  use_selection=True,export_animations=True)
        qa["glb_export"] = "PASS"
    except Exception as exc:
        qa["glb_export"] = "FAILED: "+str(exc)
    try:
        bpy.ops.export_scene.fbx(filepath=base+".fbx",use_selection=True,
                                 apply_unit_scale=True,add_leaf_bones=False)
        qa["fbx_export"] = "PASS"
    except Exception as exc:
        qa["fbx_export"] = "NOT_RUN_OR_FAILED: "+str(exc)
    with open(base+"_qa.json","w",encoding="utf-8") as fp:
        json.dump(qa,fp,indent=2)
    print(json.dumps(qa,indent=2))
    if qa["glb_export"]!="PASS":
        raise SystemExit(2)


if __name__=="__main__":
    main()
