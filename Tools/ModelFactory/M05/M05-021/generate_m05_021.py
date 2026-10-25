#!/usr/bin/env python3
"""M05-021 — Signal Folding Mast (visual-only), Blender 4.x/5.x, Unity metres.
Run: blender -b --python generate_m05_021.py -- --out /tmp/M05-021
Owns only collection M05-021_SignalMast; does not reset or clear other scenes.
"""
import argparse
import json
import math
import os
import sys

import bpy
import bmesh
from mathutils import Vector

ID = "M05-021"
COLLECTION = ID + "_SignalMast"
FRAME_END = 95
MID_TRAVEL = 0.50
INNER_TRAVEL = 0.50
TUBES = {
    "outer": (0.115, 0.095, 0.22, 1.16),
    "middle": (0.085, 0.068, 0.42, 1.50),
    "inner": (0.060, 0.047, 0.75, 1.85),
}
C = None
created = []

def clear_owned():
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
    for mat in list(bpy.data.materials):
        if mat.name.startswith(ID + "_") and mat.users == 0:
            bpy.data.materials.remove(mat)

def register(obj, parent=None):
    for coll in list(obj.users_collection):
        coll.objects.unlink(obj)
    C.objects.link(obj)
    obj["model_factory_id"] = ID
    if parent is not None:
        obj.parent = parent
    created.append(obj)
    return obj

def empty(name, xyz=(0, 0, 0), parent=None, display="PLAIN_AXES"):
    ob = bpy.data.objects.new(ID + "_" + name, None)
    C.objects.link(ob)
    ob.empty_display_type = display
    ob.empty_display_size = 0.08
    ob["model_factory_id"] = ID
    if parent is not None:
        ob.parent = parent
    ob.location = xyz
    created.append(ob)
    return ob

def material(name, color, metallic=0.0, roughness=0.4, emission=None):
    m = bpy.data.materials.new(ID + "_" + name)
    m.diffuse_color = (*color, 1.0)
    m.use_nodes = True
    p = m.node_tree.nodes.get("Principled BSDF")
    p.inputs["Base Color"].default_value = (*color, 1.0)
    p.inputs["Metallic"].default_value = metallic
    p.inputs["Roughness"].default_value = roughness
    if emission is not None:
        em = p.inputs.get("Emission Color") or p.inputs.get("Emission")
        if em is not None:
            em.default_value = (*color, 1.0)
        strength = p.inputs.get("Emission Strength")
        if strength is not None:
            strength.default_value = emission
    return m

def shade(ob, mat):
    ob.data.materials.append(mat)
    if ob.type == "MESH":
        for face in ob.data.polygons:
            face.use_smooth = (len(ob.data.polygons) > 20 and abs(face.normal.z) < 0.5)
    return ob

def cube(name, location, scale, mat, parent=None, bevel=0.015):
    bpy.ops.mesh.primitive_cube_add(size=1, location=(0, 0, 0))
    ob = register(bpy.context.object, parent)
    ob.name = ID + "_" + name
    ob.location = location
    ob.dimensions = scale
    bpy.ops.object.select_all(action="DESELECT")
    ob.select_set(True)
    bpy.context.view_layer.objects.active = ob
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    shade(ob, mat)
    if bevel:
        mod = ob.modifiers.new("machined_edge", "BEVEL")
        mod.width = bevel
        mod.segments = 2
        ob.modifiers.new("weighted_normals", "WEIGHTED_NORMAL")
    return ob

def cylinder(name, location, radius, depth, mat, parent=None, vertices=32):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth)
    ob = register(bpy.context.object, parent)
    ob.name = ID + "_" + name
    ob.location = location
    shade(ob, mat)
    return ob

def tube(name, inner, outer, z0, z1, mat, parent=None, segments=32):
    # Hollow closed manifold: two concentric surfaces plus annular end caps.
    verts = []
    for r, z in ((outer, z0), (outer, z1), (inner, z0), (inner, z1)):
        for k in range(segments):
            a = 2 * math.pi * k / segments
            verts.append((r * math.cos(a), r * math.sin(a), z))
    faces = []
    for i in range(segments):
        j = (i + 1) % segments
        faces.extend([
            (i, j, segments+j, segments+i),
            (2*segments+j, 2*segments+i, 3*segments+i, 3*segments+j),
            (segments+i, segments+j, 3*segments+j, 3*segments+i),
            (j, i, 2*segments+i, 2*segments+j),
        ])
    mesh = bpy.data.meshes.new(ID + "_" + name + "_Mesh")
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    ob = bpy.data.objects.new(ID + "_" + name, mesh)
    C.objects.link(ob)
    ob["model_factory_id"] = ID
    if parent:
        ob.parent = parent
    created.append(ob)
    shade(ob, mat)
    return ob

def rod(name, start, end, radius, mat, parent=None):
    a, b = Vector(start), Vector(end)
    d = b - a
    ob = cylinder(name, tuple((a+b)/2), radius, d.length, mat, parent, 12)
    ob.rotation_euler = d.to_track_quat("Z", "Y").to_euler()
    return ob

def assert_manifold():
    for ob in created:
        if ob.type != "MESH":
            continue
        bm = bmesh.new()
        bm.from_mesh(ob.data)
        bad = [e for e in bm.edges if not e.is_manifold]
        bm.free()
        if bad:
            raise AssertionError(ob.name + " has " + str(len(bad)) + " nonmanifold edges")

def key_z(ob, frames):
    for frame, z in frames:
        ob.location.z = z
        ob.keyframe_insert(data_path="location", frame=frame, group="Telescoping")
    action = ob.animation_data.action
    if action and hasattr(action, "fcurves"):
        for curve in action.fcurves:
            for point in curve.keyframe_points:
                point.interpolation = "LINEAR"

def verify(middle, inner):
    # Radial clearance and axial overlap are checked at all animated frames.
    outer_r, outer_in, _, outer_top = TUBES["outer"]
    mid_r, mid_in, mid_bottom, mid_top = TUBES["middle"]
    inner_r, _, inner_bottom, _ = TUBES["inner"]
    assert outer_in - mid_r >= 0.009
    assert mid_in - inner_r >= 0.007
    for frame in range(1, FRAME_END + 1):
        bpy.context.scene.frame_set(frame)
        m = middle.location.z
        i = inner.location.z
        assert -1e-6 <= m <= MID_TRAVEL + 1e-6
        assert -1e-6 <= i <= INNER_TRAVEL + 1e-6
        assert outer_top - (mid_bottom + m) >= 0.20
        assert mid_top - (inner_bottom + i) >= 0.20
    bpy.context.scene.frame_set(FRAME_END)
    assert abs(middle.location.z - MID_TRAVEL) < 1e-5
    assert abs(inner.location.z - INNER_TRAVEL) < 1e-5
    return {"frames_checked": FRAME_END, "outer_middle_radial_clearance_m": round(outer_in-mid_r, 4),
            "middle_inner_radial_clearance_m": round(mid_in-inner_r, 4),
            "minimum_axial_engagement_m": 0.24,
            "maximum_beacon_height_m": 3.14}

def build():
    global C
    clear_owned()
    created.clear()
    C = bpy.data.collections.new(COLLECTION)
    bpy.context.scene.collection.children.link(C)
    graphite = material("graphite", (0.075, 0.09, 0.12), 0.62)
    steel = material("brushed_steel", (0.40, 0.47, 0.50), 0.82, 0.26)
    titanium = material("titanium", (0.62, 0.67, 0.69), 0.88, 0.22)
    copper = material("copper", (0.60, 0.28, 0.09), 0.75)
    ceramic = material("ceramic", (0.80, 0.83, 0.76), 0.14)
    rubber = material("rubber", (0.035, 0.038, 0.045), 0.0, 0.9)
    cyan = material("cyan_led", (0.02, 0.67, 0.95), 0.0, 0.20, 4.0)
    amber = material("amber_led", (1.0, 0.40, 0.035), 0.0, 0.20, 3.0)
    root = empty("MOUNT_ROOT")
    root["mount_type"] = "top_socket"
    root["units"] = "metres"
    root["forward_axis"] = "-Y"
    cube("mount_plate", (0,0,0.10), (0.68,0.60,0.17), graphite, root)
    cube("ceramic_deck", (0,0,0.195), (0.50,0.43,0.025), ceramic, root, 0.008)
    cylinder("base_swivel", (0,0,0.24), 0.19, 0.10, steel, root)
    cylinder("socket_gasket", (0,0,0.30), 0.155, 0.025, rubber, root)
    for x in (-0.26, 0.26):
        for y in (-0.21, 0.21):
            cylinder("mount_bolt_%s_%s" % (x,y), (x,y,0.205), 0.022, 0.02, titanium, root, 12)
    for side in (-1, 1):
        rod("angled_support_%s" % side, (side*0.28,0,0.23),
            (side*0.19,0,0.83), 0.023, steel, root)
        cube("cable_bracket_%s" % side, (side*0.245,-0.18,0.35),
             (0.08,0.10,0.10), copper, root)
    tube("outer_sleeve", *TUBES["outer"][:2][::-1], 0.22, 1.16, graphite, root)
    tube("outer_top_collar", 0.116, 0.139, 1.105, 1.155, titanium, root)
    tube("outer_lower_collar", 0.116, 0.135, 0.32, 0.37, steel, root)
    for z in (0.48, 0.69, 0.90):
        tube("outer_copper_stripe_%s" % z, 0.116, 0.118, z, z+0.012, copper, root)
    mid = empty("PIVOT_MIDDLE_STAGE", parent=root)
    tube("middle_sleeve", 0.068, 0.085, 0.42, 1.50, steel, mid)
    tube("middle_top_collar", 0.086, 0.095, 1.45, 1.49, copper, mid)
    inner = empty("PIVOT_INNER_STAGE", parent=mid)
    tube("inner_sleeve", 0.047, 0.060, 0.75, 1.85, titanium, inner)
    tube("inner_top_collar", 0.061, 0.074, 1.81, 1.85, graphite, inner)
    for idx, z in enumerate((1.63, 1.72, 1.80)):
        tube("LED_ring_%02d" % idx, 0.061, 0.079, z, z+0.021, cyan if idx != 1 else amber, inner)
        tube("LED_separator_%02d" % idx, 0.080, 0.083, z+0.005, z+0.010, steel, inner)
    cylinder("beacon_stem", (0,0,1.91), 0.038, 0.16, copper, inner)
    cylinder("beacon_lens", (0,0,2.04), 0.087, 0.14, amber, inner, 12)
    cylinder("beacon_cap", (0,0,2.125), 0.096, 0.027, ceramic, inner, 12)
    for a in range(0, 360, 90):
        angle = math.radians(a)
        rod("beacon_cage_%d" % a,
            (0.096*math.cos(angle),0.096*math.sin(angle),1.975),
            (0.096*math.cos(angle),0.096*math.sin(angle),2.115),
            0.009, graphite, inner)
    empty("MOUNT_CABLE_IN", (0,-0.29,0.25), root)
    empty("MOUNT_SERVICE_LEFT", (-0.34,0,0.17), root)
    empty("MOUNT_SERVICE_RIGHT", (0.34,0,0.17), root)
    empty("MOUNT_BEACON_TOP", (0,0,2.14), inner)
    key_z(mid, ((1,0),(20,0),(55,MID_TRAVEL),(95,MID_TRAVEL)))
    key_z(inner, ((1,0),(55,0),(95,INNER_TRAVEL)))
    bpy.context.scene.frame_start = 1
    bpy.context.scene.frame_end = FRAME_END
    assert_manifold()
    qa = verify(mid, inner)
    qa.update({"id":ID, "status":"BLENDER_QA_PASS", "materials":8,
               "mesh_count":sum(o.type == "MESH" for o in created),
               "mount_origin_m":[0,0,0], "middle_pivot_m":[0,0,0],
               "inner_pivot_m":[0,0,0], "visual_only":True,
               "test_scope":"manifold meshes; nested-tube radial and axial clearance, 95 animation frames"})
    return qa

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--out", default=os.path.join(os.path.dirname(__file__), "output"))
    args = parser.parse_args(sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else [])
    os.makedirs(args.out, exist_ok=True)
    qa = build()
    base = os.path.join(args.out, ID)
    bpy.context.scene.frame_set(1)
    bpy.ops.wm.save_as_mainfile(filepath=base + ".blend")
    bpy.ops.object.select_all(action="DESELECT")
    for ob in created:
        ob.select_set(True)
    bpy.context.view_layer.objects.active = created[0]
    try:
        bpy.ops.export_scene.gltf(filepath=base + ".glb", export_format="GLB",
                                  use_selection=True, export_animations=True)
        qa["glb_export"] = "PASS"
    except Exception as exc:
        qa["glb_export"] = "FAILED: " + str(exc)
    try:
        bpy.ops.export_scene.fbx(filepath=base + ".fbx", use_selection=True,
                                 apply_unit_scale=True, add_leaf_bones=False)
        qa["fbx_export"] = "PASS"
    except Exception as exc:
        qa["fbx_export"] = "NOT_RUN_OR_FAILED: " + str(exc)
    qa["blend_export"] = "PASS"
    with open(base + "_qa.json", "w", encoding="utf-8") as f:
        json.dump(qa, f, indent=2)
    print(json.dumps(qa, indent=2))
    if qa["glb_export"] != "PASS":
        raise SystemExit(2)

if __name__ == "__main__":
    main()
