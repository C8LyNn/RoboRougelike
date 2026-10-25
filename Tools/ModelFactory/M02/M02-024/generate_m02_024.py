#!/usr/bin/env python3
"""M02-024: Mecanum wheel with 12 angled independently spinning rollers.

Run: blender -b -noaudio --python generate_m02_024.py
Units: 1 Blender unit = 1 metre; +Y side axle; no gameplay changes.
"""
import bpy
import bmesh
import json
import math
import sys
from pathlib import Path
from mathutils import Vector

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
from kinematics_m02_024 import (
    ID, ROLLER_COUNT, ROLLER_CENTRE_R, ROLLER_RADIUS,
    ROLLER_LENGTH, RIM_MAJOR_R, RIM_MINOR_R, FRAMES,
    roller_pose, wheel_angle, roller_angle, analytical_qa,
)

OUT = HERE / "generated"
OUT.mkdir(parents=True, exist_ok=True)

def reset():
    # Dedicated scene for this CLI build; avoid accumulating objects/actions.
    for obj in list(bpy.data.objects):
        bpy.data.objects.remove(obj, do_unlink=True)
    for blocks in (bpy.data.meshes, bpy.data.materials, bpy.data.actions):
        for block in list(blocks):
            if block.users == 0:
                blocks.remove(block)
    for collection in list(bpy.data.collections):
        if collection.name.startswith(ID + "_"):
            bpy.data.collections.remove(collection)

def build():
    reset()
    scene = bpy.context.scene
    scene.unit_settings.system = "METRIC"
    scene.unit_settings.scale_length = 1.0
    scene.frame_start = 1
    scene.frame_end = FRAMES
    scene.render.fps = 30
    col = bpy.data.collections.new(ID + "_MECANUM_WHEEL")
    scene.collection.children.link(col)

    def mat(name, rgb, metallic, roughness, emission=0.0):
        m = bpy.data.materials.new(ID + "_" + name)
        m.diffuse_color = (*rgb, 1.0)
        m.use_nodes = True
        bsdf = m.node_tree.nodes.get("Principled BSDF")
        bsdf.inputs["Base Color"].default_value = (*rgb, 1.0)
        bsdf.inputs["Metallic"].default_value = metallic
        bsdf.inputs["Roughness"].default_value = roughness
        if emission:
            inp = bsdf.inputs.get("Emission Color")
            power = bsdf.inputs.get("Emission Strength")
            if inp: inp.default_value = (*rgb, 1.0)
            if power: power.default_value = emission
        return m

    graphite = mat("01_forged_graphite", (.085, .103, .125), .85, .34)
    alloy = mat("02_brushed_aluminium", (.58, .64, .70), .72, .27)
    rubber = mat("03_rubber_rollers", (.048, .061, .071), .06, .78)
    bronze = mat("04_bronze_bearings", (.67, .39, .17), .70, .30)
    orange = mat("05_safety_orange", (.97, .30, .04), .28, .39)
    ceramic = mat("06_ceramic_hub_ring", (.77, .80, .73), .16, .41)
    steel = mat("07_hardened_steel", (.23, .27, .32), .94, .25)
    cyan = mat("08_service_led", (.02, .48, .81), .18, .22, 1.1)
    materials = (graphite, alloy, rubber, bronze, orange, ceramic, steel, cyan)

    def link(obj, material=None, parent=None):
        for old in list(obj.users_collection):
            old.objects.unlink(obj)
        col.objects.link(obj)
        if material is not None:
            obj.data.materials.append(material)
        if parent is not None:
            obj.parent = parent
        return obj

    def empty(name, loc=(0, 0, 0), parent=None):
        obj = bpy.data.objects.new(name, None)
        col.objects.link(obj)
        obj.empty_display_type = "PLAIN_AXES"
        obj.empty_display_size = .045
        if parent is not None:
            obj.parent = parent
        obj.location = loc
        return obj

    def cylinder(name, loc, radius, depth, material, parent,
                 vertices=32, orient=None):
        bpy.ops.mesh.primitive_cylinder_add(
            vertices=vertices, radius=radius, depth=depth, location=(0, 0, 0))
        obj = link(bpy.context.object, material, parent)
        obj.name = name
        obj.location = loc
        if orient is not None:
            obj.rotation_mode = "QUATERNION"
            obj.rotation_quaternion = Vector(orient).to_track_quat("Z", "Y")
        for polygon in obj.data.polygons:
            if len(polygon.vertices) == 4:
                polygon.use_smooth = True
        return obj

    def torus(name, loc, major, minor, material, parent, axis_y=False):
        bpy.ops.mesh.primitive_torus_add(
            major_segments=48, minor_segments=12,
            location=(0, 0, 0), major_radius=major, minor_radius=minor)
        obj = link(bpy.context.object, material, parent)
        obj.name = name
        obj.location = loc
        if axis_y:
            obj.rotation_euler.x = math.pi / 2.0
        for face in obj.data.polygons:
            face.use_smooth = True
        return obj

    def cube(name, loc, dimensions, material, parent, bevel=0.0):
        bpy.ops.mesh.primitive_cube_add(size=1, location=(0, 0, 0))
        obj = link(bpy.context.object, material, parent)
        obj.name = name
        obj.location = loc
        obj.dimensions = dimensions
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
        if bevel:
            mod = obj.modifiers.new("rounded_machined_edges", "BEVEL")
            mod.width = bevel
            mod.segments = 2
            obj.modifiers.new("weighted_corner_normals", "WEIGHTED_NORMAL")
        return obj

    def beam(name, start, end, radius, material, parent):
        a, b = Vector(start), Vector(end)
        vec = b - a
        assert vec.length > .001
        return cylinder(name, tuple((a + b) / 2), radius,
                        vec.length, material, parent, 16, tuple(vec))

    mount = empty("MOUNT_SIDE_AXLE_ORIGIN_1M")
    # Fixed hollow-style axle assembly. Rotor hub annulus leaves radial clearance.
    cylinder("FIXED_AXLE_SHAFT_Y", (0, -.092, 0),
             .034, .310, steel, mount, 40, (0, 1, 0))
    cylinder("CHASSIS_MOUNT_PLATE", (0, -.245, 0),
             .115, .021, graphite, mount, 40, (0, 1, 0))
    for j in range(6):
        a = 2 * math.pi * j / 6
        cylinder("MOUNT_BOLT_%02d" % j,
                 (.083 * math.cos(a), -.260, .083 * math.sin(a)),
                 .008, .009, alloy, mount, 12, (0, 1, 0))
    torus("STATIONARY_AXLE_SEAL", (0, -.148, 0),
          .042, .008, bronze, mount, True)

    rotor = empty("WHEEL_SPIN_Y", (0, 0, 0), mount)
    torus("HOLLOW_ROTOR_HUB", (0, 0, 0), .091, .024, alloy, rotor, True)
    torus("CERAMIC_HUB_ACCENT", (0, -.041, 0), .108, .008, ceramic, rotor, True)
    torus("LEFT_RIM_RING", (0, -.083, 0),
          RIM_MAJOR_R, RIM_MINOR_R, steel, rotor, True)
    torus("RIGHT_RIM_RING", (0, .083, 0),
          RIM_MAJOR_R, RIM_MINOR_R, steel, rotor, True)
    for j in range(8):
        a = 2 * math.pi * j / 8
        direction = (math.cos(a), 0.0, math.sin(a))
        for y in (-.082, .082):
            beam("ROTOR_SPOKE_%02d_%s" % (j, "L" if y < 0 else "R"),
                 (.115 * direction[0], y, .115 * direction[2]),
                 (.279 * direction[0], y, .279 * direction[2]),
                 .013, graphite, rotor)
    for j in range(8):
        a = 2 * math.pi * j / 8
        cylinder("HUB_FASTENER_%02d" % j,
                 (.112 * math.cos(a), -.045, .112 * math.sin(a)),
                 .006, .007, orange, rotor, 12, (0, 1, 0))

    rollers = []
    for i in range(ROLLER_COUNT):
        center, axis = roller_pose(i)
        carrier = empty("ROLLER_CARRIER_%02d_45DEG" % i, center, rotor)
        carrier.rotation_mode = "QUATERNION"
        carrier.rotation_quaternion = Vector(axis).to_track_quat("Z", "Y")
        spin = empty("ROLLER_SPIN_%02d_Z" % i, (0, 0, 0), carrier)
        rollers.append(spin)
        # Each rubber roller has its own bearing spindle and protective end caps.
        cylinder("ROLLER_%02d_RUBBER_BARREL" % i, (0, 0, 0),
                 ROLLER_RADIUS, ROLLER_LENGTH, rubber, spin, 32)
        for side in (-1, 1):
            label = "A" if side < 0 else "B"
            end = side * (ROLLER_LENGTH / 2 + .013)
            cylinder("ROLLER_%02d_BEARING_%s" % (i, label),
                     (0, 0, end), .023, .014, bronze, carrier, 24)
            cylinder("ROLLER_%02d_STEEL_ENDCAP_%s" % (i, label),
                     (0, 0, end + side * .010), .017, .007,
                     orange if i % 3 == 0 else alloy, carrier, 20)
            # Spoke-mounted brackets stop before the roller's running surface.
            endpoint = Vector(center) + Vector(axis) * end
            start = Vector((center[0] * .79, endpoint.y, center[2] * .79))
            beam("ROLLER_%02d_CARRIER_ARM_%s" % (i, label),
                 tuple(start), tuple(endpoint), .010, graphite, rotor)

    torus("ROTOR_HUB_INSPECTION_STRIPE", (0, .052, 0),
          .108, .006, cyan, rotor, True)
    for frame in range(1, FRAMES + 1):
        rotor.rotation_euler.y = -wheel_angle(frame)
        rotor.keyframe_insert(data_path="rotation_euler", frame=frame)
        for moving in rollers:
            moving.rotation_euler.z = roller_angle(frame)
            moving.keyframe_insert(data_path="rotation_euler", frame=frame)
    # Identical NLA track names combine independent actions in glTF 2.0.
    for moving in [rotor, *rollers]:
        action = moving.animation_data.action
        track = moving.animation_data.nla_tracks.new()
        track.name = "M02_024_MECANUM_CYCLE"
        track.strips.new("M02_024_MECANUM_CYCLE", 1, action)
        moving.animation_data.action = None
    scene.frame_set(1)
    return {
        "collection": col,
        "materials": len(materials),
        "mesh_objects": sum(o.type == "MESH" for o in col.objects),
        "moving_groups": [rotor.name] + [r.name for r in rollers],
    }

def check_topology():
    count = 0
    problems = []
    for obj in bpy.data.objects:
        if obj.type != "MESH":
            continue
        bm = bmesh.new()
        bm.from_mesh(obj.data)
        if not bm.faces or any(not e.is_manifold for e in bm.edges):
            problems.append(obj.name + ":nonmanifold")
        if any(f.calc_area() < 1e-11 for f in bm.faces):
            problems.append(obj.name + ":degenerate")
        bm.free()
        count += 1
    assert not problems, problems[:20]
    return count

def main():
    analytical = analytical_qa()
    first = build()
    first_count = check_topology()
    second = build()
    second_count = check_topology()
    assert first_count == second_count == first["mesh_objects"] == second["mesh_objects"]
    assert second["materials"] == 8
    assert second_count >= 100, second_count
    blend = OUT / (ID + ".blend")
    glb = OUT / (ID + "_animated.glb")
    bpy.ops.wm.save_as_mainfile(filepath=str(blend))
    bpy.ops.object.select_all(action="DESELECT")
    for obj in second["collection"].objects:
        obj.select_set(True)
    export = dict(
        filepath=str(glb), export_format="GLB", use_selection=True,
        export_animations=True, export_frame_range=True,
        export_force_sampling=True,
    )
    props = bpy.ops.export_scene.gltf.get_rna_type().properties.keys()
    if "export_animation_mode" in props:
        export["export_animation_mode"] = "NLA_TRACKS"
    elif "export_nla_strips" in props:
        export["export_nla_strips"] = True
    bpy.ops.export_scene.gltf(**export)
    assert blend.stat().st_size > 10000
    assert glb.stat().st_size > 10000
    report = {
        "id": ID, "blender": bpy.app.version_string,
        "scale_m_per_unit": 1.0,
        "pivot": "MOUNT_SIDE_AXLE_ORIGIN_1M at (0,0,0)",
        "wheel_axis": "Blender +Y", "roller_axis_tilt_deg": 45,
        "mesh_objects": second_count, "materials": second["materials"],
        "moving_groups": second["moving_groups"],
        "analytical_qa": analytical,
        "reentrant": "PASS", "manifold": "PASS",
        "blend_bytes": blend.stat().st_size,
        "glb_bytes": glb.stat().st_size,
        "unity_import": "NOT RUN",
        "triangle_collision": "NOT RUN",
        "roller_bearing_contact_simulation": "NOT RUN",
    }
    (OUT / (ID + "_qa.json")).write_text(
        json.dumps(report, indent=2), encoding="utf-8")
    print("M02-024 RESULT " + json.dumps(report, sort_keys=True))

if __name__ == "__main__":
    main()
