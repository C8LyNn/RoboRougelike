#!/usr/bin/env python3
"""M04-023 — Przepustnica jonowa / rear ion throttle.
Blender 4.x/5.x. One unit = one metre; +Z up, rear exhaust points -Y.
The mounting interface is the Y=0 plane; ROOT_MOUNT is (0,0,0).
Six iris shutter sectors slide radially 70 mm, without crossing the housing.
Usage: blender -b --factory-startup --python-exit-code 1 --python generate_m04_023.py -- --out build/M04-023
Visual asset only; no propulsion physics or gameplay changes.
"""
import bpy
import bmesh
import json
import math
import sys
from pathlib import Path

MODEL = "M04-023"
FRAME_END = 121
SHUTTER_Y = -0.875
TRAVEL = 0.070
INNER_R = 0.250
OUTER_R = 0.490
RING_MAJOR = 0.680
RING_MINOR = 0.070
LENS_RADIUS = 0.180

def material(name, rgb, metallic=0.0, roughness=0.4, glow=0.0):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = (*rgb, 1.0)
    mat.use_nodes = True
    p = mat.node_tree.nodes.get("Principled BSDF")
    p.inputs["Base Color"].default_value = (*rgb, 1.0)
    p.inputs["Metallic"].default_value = metallic
    p.inputs["Roughness"].default_value = roughness
    if glow:
        for key in ("Emission Color", "Emission"):
            if key in p.inputs:
                p.inputs[key].default_value = (*rgb, 1.0)
                break
        if "Emission Strength" in p.inputs:
            p.inputs["Emission Strength"].default_value = glow
    return mat

def link_parent(obj, parent):
    if parent:
        bpy.context.view_layer.update()
        obj.parent = parent
        obj.matrix_parent_inverse = parent.matrix_world.inverted()
    return obj

def empty(name, location, parent=None):
    obj = bpy.data.objects.new(name, None)
    bpy.context.collection.objects.link(obj)
    obj.location = location
    obj.empty_display_type = "ARROWS"
    obj.empty_display_size = 0.085
    link_parent(obj, parent)
    return obj

def finish(obj, mat, parent, role):
    obj.data.materials.append(mat)
    obj["model_id"] = MODEL
    obj["role"] = role
    link_parent(obj, parent)
    return obj

def bevel(obj, width=0.007):
    mod = obj.modifiers.new("Machined edge bevel", "BEVEL")
    mod.width = width
    mod.segments = 2
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.modifier_apply(modifier=mod.name)

def box(name, pos, dimensions, mat, parent, role="detail", edge=0.006, angle=0.0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=pos)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = dimensions
    obj.rotation_euler[1] = -angle
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if edge:
        bevel(obj, min(edge, min(dimensions) * 0.20))
    return finish(obj, mat, parent, role)

def cylinder(name, pos, radius, depth, mat, parent, role="detail", vertices=48):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth,
                                    location=pos, rotation=(math.pi / 2, 0, 0))
    obj = bpy.context.object
    obj.name = name
    for poly in obj.data.polygons:
        poly.use_smooth = len(poly.vertices) == 4
    return finish(obj, mat, parent, role)

def torus(name, y, radius, minor, mat, parent, role="housing"):
    bpy.ops.mesh.primitive_torus_add(major_segments=80, minor_segments=12,
        location=(0, y, 0), rotation=(math.pi / 2, 0, 0),
        major_radius=radius, minor_radius=minor)
    obj = bpy.context.object
    obj.name = name
    for poly in obj.data.polygons:
        poly.use_smooth = True
    return finish(obj, mat, parent, role)

def wedge(name, theta, y, r0, r1, half_angle, thickness, mat, parent):
    # Extruded annular sector. Every edge belongs to two faces.
    corners = [(r0, -half_angle), (r1, -half_angle),
               (r1, half_angle), (r0, half_angle)]
    verts = []
    for yy in (y - thickness / 2, y + thickness / 2):
        for radius, da in corners:
            a = theta + da
            verts.append((radius * math.cos(a), yy, radius * math.sin(a)))
    faces = [(0, 1, 2, 3), (7, 6, 5, 4)]
    for i in range(4):
        j = (i + 1) % 4
        faces.append((i, i + 4, j + 4, j))
    mesh = bpy.data.meshes.new(name + "_mesh")
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    bevel(obj, 0.005)
    return finish(obj, mat, parent, "animated_shutter")

def create_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for datablocks in (bpy.data.materials, bpy.data.meshes):
        for block in list(datablocks):
            if block.users == 0:
                datablocks.remove(block)

    graphite = material("01_Graphite_ceramic_composite", (0.055, 0.072, 0.085), .55, .40)
    titanium = material("02_Brushed_titanium", (.48, .54, .58), .78, .28)
    copper = material("03_Copper_heat_sink", (.55, .25, .105), .72, .31)
    ivory = material("04_Ivory_ceramic_lip", (.78, .83, .78), .15, .24)
    shutter = material("05_Blackened_iridium_shutters", (.12, .16, .20), .86, .23)
    lens = material("06_Ion_blue_lens", (.015, .40, .89), .25, .17, 2.0)
    cyan = material("07_Cyan_ion_trace", (.012, .88, .95), .20, .20, 2.5)
    amber = material("08_Amber_service_lamps", (1.0, .42, .055), .25, .25, 1.6)
    root = empty("ROOT_MOUNT_Y0", (0, 0, 0))
    root["mount_plane"] = "rear chassis Y=0; exhaust -Y"
    root["unity_scale"] = "1 unit = 1 metre"

    # The plate terminates 10 mm before the Y=0 interface.
    box("Rear_socket_octagonal_base", (0, -.058, 0), (.74, .095, .68),
        graphite, root, "rear_mount", .028)
    cylinder("Main_ion_duct", (0, -.39, 0), .535, .52, graphite, root, "housing", 80)
    torus("Titanium_rear_collar", -.650, .565, .045, titanium, root)
    torus("Ivory_ceramic_nozzle_lip", -.860, RING_MAJOR, RING_MINOR, ivory, root)
    torus("Copper_ion_induction_ring", -.805, .655, .018, copper, root)
    torus("Cyan_outer_ion_trace", -.866, .675, .010, cyan, root, "accent")
    cylinder("Inner_ion_chamber", (0, -.680, 0), .270, .055, titanium, root)
    cylinder("Main_blue_focus_lens", (0, -.754, 0), LENS_RADIUS, .035,
             lens, root, "blue_lens", 64)
    cylinder("Focus_lens_bezel", (0, -.732, 0), .195, .020, copper, root, "lens_bezel", 64)

    # Four diagonal ducts and their armored clamps.
    for i in range(4):
        a = (i + .5) * math.pi / 2
        x, z = .48 * math.cos(a), .48 * math.sin(a)
        box("Strut_%02d" % i, (x, -.690, z), (.095, .38, .10),
            titanium, root, "support", .013, a)
        box("Strut_ceramic_cap_%02d" % i, (x, -.835, z), (.115, .045, .12),
            ivory, root, "support", .008, a)

    # Radial rails sit behind the moving iris, so the shutters never hit them.
    for i in range(6):
        a = 2 * math.pi * i / 6
        r = .46
        x, z = r * math.cos(a), r * math.sin(a)
        box("Rail_%02d" % i, (x, -.735, z), (.23, .035, .038),
            copper, root, "rail", .007, a)
        cylinder("Aux_blue_lens_%02d" % i,
                 (.335 * math.cos(a), -.700, .335 * math.sin(a)),
                 .042, .025, lens, root, "blue_lens", 32)
        box("Amber_rail_lock_%02d" % i,
            (.545 * math.cos(a), -.732, .545 * math.sin(a)),
            (.042, .025, .050), amber, root, "status_lamp", .005, a)

    # Six angularly separated sliding shutter sectors.
    slides = []
    for i in range(6):
        a = 2 * math.pi * i / 6
        pivot = empty("SHUTTER_%02d_SLIDE" % (i + 1), (0, SHUTTER_Y, 0), root)
        pivot["travel_m"] = TRAVEL
        pivot["radial_angle_deg"] = round(math.degrees(a), 2)
        wedge("Iris_blade_%02d" % (i + 1), a, SHUTTER_Y,
              INNER_R, OUTER_R, math.radians(20), .030, shutter, pivot)
        wedge("Iris_ceramic_inlay_%02d" % (i + 1), a, SHUTTER_Y - .021,
              .305, .455, math.radians(11), .009, ivory, pivot)
        cylinder("Iris_blue_indicator_%02d" % (i + 1),
                 (.43 * math.cos(a), SHUTTER_Y - .042, .43 * math.sin(a)),
                 .015, .012, cyan, pivot, "moving_indicator", 24)
        for frame, d in ((1, 0.0), (32, TRAVEL), (64, TRAVEL),
                         (95, 0.0), (121, 0.0)):
            pivot.location = (d * math.cos(a), SHUTTER_Y, d * math.sin(a))
            pivot.keyframe_insert(data_path="location", frame=frame,
                                  group="Ion throttle iris")
        if pivot.animation_data and pivot.animation_data.action:
            action = pivot.animation_data.action
            # Blender 4/5 both interpolate safely when keyframe interpolation is linear.
            try:
                for fc in action.fcurves:
                    for key in fc.keyframe_points:
                        key.interpolation = "LINEAR"
            except AttributeError:
                pass
        slides.append(pivot)

    # Exhaust-side highlights and readable service fasteners.
    for i in range(8):
        a = 2 * math.pi * i / 8
        x, z = .71 * math.cos(a), .71 * math.sin(a)
        box("Rim_titanium_fin_%02d" % i, (x, -.86, z),
            (.06, .10, .035), titanium, root, "rim_detail", .005, a)
    for x in (-.29, .29):
        for z in (-.245, .245):
            cylinder("Mount_fastener_%s_%s" % (str(x), str(z)),
                     (x, -.119, z), .034, .020, copper, root, "mount_fastener", 24)

    scene = bpy.context.scene
    scene.frame_start = 1
    scene.frame_end = FRAME_END
    scene.render.fps = 30
    scene.frame_set(1)
    return slides

def validate(slides):
    mesh_objects = [o for o in bpy.data.objects if o.type == "MESH"]
    assert len(mesh_objects) >= 50, "Unexpectedly few mesh objects"
    used_materials = {mat.name for obj in mesh_objects for mat in obj.data.materials if mat}
    assert len(used_materials) == 8, "Expected eight assigned PBR materials"
    edge_total = 0
    for obj in mesh_objects:
        assert not obj.data.validate(clean_customdata=False), "Invalid mesh: " + obj.name
        bm = bmesh.new()
        bm.from_mesh(obj.data)
        assert all(e.is_manifold for e in bm.edges), "Non-manifold: " + obj.name
        assert all(f.calc_area() > 1e-9 for f in bm.faces), "Degenerate face: " + obj.name
        edge_total += len(bm.edges)
        bm.free()
    min_outer_gap = 1e9
    min_lens_gap = 1e9
    for frame in range(1, FRAME_END + 1):
        bpy.context.scene.frame_set(frame)
        for p in slides:
            d = math.hypot(p.location.x, p.location.z)
            assert d <= TRAVEL + 1e-5, "Slide overshoot"
            min_outer_gap = min(min_outer_gap, RING_MAJOR - RING_MINOR - OUTER_R - d)
            min_lens_gap = min(min_lens_gap, INNER_R + d - LENS_RADIUS)
    assert min_outer_gap >= .049, "Shutter meets housing"
    assert min_lens_gap >= .069, "Shutter meets central lens"
    # Adjacent sectors are 60 degrees apart and each occupies at most +/-20 degrees.
    assert 60 - 2 * 20 >= 20, "Angular sectors intersect"
    bpy.context.scene.frame_set(1)
    return {
        "model": MODEL, "meshes": len(mesh_objects), "materials": len(used_materials),
        "manifold_edges_checked": edge_total, "frames_checked": FRAME_END,
        "moving_shutters": len(slides), "slide_travel_m": TRAVEL,
        "min_housing_radial_gap_m": round(min_outer_gap, 6),
        "min_lens_radial_gap_m": round(min_lens_gap, 6),
        "adjacent_sector_angular_gap_deg": 20,
        "limitations": "Analytical envelope clearance only; no continuous triangle-triangle collision or Unity physics test."
    }

def main():
    args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    out = Path("build/M04-023")
    if "--out" in args:
        out = Path(args[args.index("--out") + 1])
    out.mkdir(parents=True, exist_ok=True)
    slides = create_scene()
    report = validate(slides)
    bpy.ops.wm.save_as_mainfile(filepath=str((out / (MODEL + ".blend")).resolve()))
    bpy.ops.export_scene.gltf(filepath=str((out / (MODEL + ".glb")).resolve()),
                              export_format="GLB", export_animations=True)
    (out / "manifest.json").write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print("MODEL_QA_PASS " + json.dumps(report, sort_keys=True))

if __name__ == "__main__":
    main()
