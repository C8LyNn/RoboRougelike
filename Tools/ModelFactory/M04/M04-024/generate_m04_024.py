#!/usr/bin/env python3
"""M04-024 — Quad vector-thruster array, Blender 4.x/5.x.
Underside mounting plane Z=0, +Z up, exhaust -Z, 1 unit = 1 metre.
Four independently animated +/-12 degree nozzle gimbals.
Usage: blender -b --factory-startup --python-exit-code 1 --python generate_m04_024.py -- --out build/M04-024
Visual-only asset; does not implement propulsion/gameplay.
"""
import bpy
import bmesh
import json
import math
import sys
from pathlib import Path
from mathutils import Vector

MODEL = "M04-024"
FRAMES = 121
ANGLE = math.radians(12.0)
CENTRES = [(-0.47,-0.47),(0.47,-0.47),(-0.47,0.47),(0.47,0.47)]

def material(name, rgb, metallic, roughness, emission=0):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = (*rgb, 1)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (*rgb, 1)
    bsdf.inputs["Metallic"].default_value = metallic
    bsdf.inputs["Roughness"].default_value = roughness
    if emission:
        for key in ("Emission Color", "Emission"):
            if key in bsdf.inputs:
                bsdf.inputs[key].default_value = (*rgb, 1)
                break
        if "Emission Strength" in bsdf.inputs:
            bsdf.inputs["Emission Strength"].default_value = emission
    return mat

def attach(obj, parent, mat=None, role="detail"):
    if mat:
        obj.data.materials.append(mat)
    obj["model_id"] = MODEL
    obj["role"] = role
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
    obj.empty_display_size = .075
    return attach(obj, parent)

def box(name, location, size, mat, parent, role="detail"):
    bpy.ops.mesh.primitive_cube_add(size=1, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    bevel = obj.modifiers.new("Soft machined edges", "BEVEL")
    bevel.width = min(.008, min(size) * .15)
    bevel.segments = 2
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.modifier_apply(modifier=bevel.name)
    return attach(obj, parent, mat, role)

def cylinder(name, location, radius, depth, mat, parent, vertices=40, role="detail"):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=location)
    obj = bpy.context.object
    obj.name = name
    for face in obj.data.polygons:
        face.use_smooth = len(face.vertices) == 4
    return attach(obj, parent, mat, role)

def cone(name, location, lower, upper, depth, mat, parent, role="detail"):
    bpy.ops.mesh.primitive_cone_add(vertices=48, radius1=lower, radius2=upper, depth=depth, location=location)
    obj = bpy.context.object
    obj.name = name
    for face in obj.data.polygons:
        face.use_smooth = len(face.vertices) == 4
    return attach(obj, parent, mat, role)

def ring(name, location, radius, tube, mat, parent, role="detail"):
    bpy.ops.mesh.primitive_torus_add(major_segments=48, minor_segments=10,
        major_radius=radius, minor_radius=tube, location=location)
    obj = bpy.context.object
    obj.name = name
    for face in obj.data.polygons:
        face.use_smooth = True
    return attach(obj, parent, mat, role)

def make_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for blockset in (bpy.data.materials, bpy.data.meshes):
        for block in list(blockset):
            if block.users == 0:
                blockset.remove(block)
    graphite = material("01_Graphite_housing", (.045,.065,.08), .58,.35)
    titanium = material("02_Brushed_titanium", (.53,.60,.64), .80,.27)
    copper = material("03_Copper_inductor", (.64,.29,.11), .76,.30)
    ceramic = material("04_Pale_ceramic_lips", (.78,.83,.79), .20,.25)
    dark = material("05_Recessed_black_nozzle", (.015,.024,.035), .25,.55)
    rubber = material("06_Elastomer_isolator", (.085,.095,.11), .05,.78)
    cyan = material("07_Cyan_vector_field", (.02,.72,1.0), .20,.20,2.5)
    amber = material("08_Amber_service_lights", (1.0,.42,.045), .20,.25,1.7)
    root = empty("ROOT_UNDERSIDE_MOUNT_Z0", (0,0,0))
    root["mount"] = "underside chassis Z=0; exhaust -Z"
    root["unit_scale"] = "1 unit = 1 metre"
    box("Octagonal_mount_base", (0,0,-.065), (1.50,1.50,.12), graphite, root, "mount")
    box("Centre_titanium_X_truss", (0,0,-.145), (1.35,.095,.075), titanium, root, "mount")
    box("Centre_titanium_Y_truss", (0,0,-.145), (.095,1.35,.075), titanium, root, "mount")
    cylinder("Central_service_core", (0,0,-.165), .19,.13, copper, root)
    ring("Central_cyan_status_ring", (0,0,-.240), .13,.014, cyan, root)
    pivots = []
    for i, (x,y) in enumerate(CENTRES):
        n = i + 1
        prefix = "N%02d_" % n
        # Fixed socket and spherical bearing above each moving nozzle.
        box(prefix+"Socket_foot", (x,y,-.165), (.34,.34,.10), titanium, root, "mount")
        cylinder(prefix+"Rubber_isolator", (x,y,-.235), .205,.055, rubber, root, role="fixed_socket")
        cylinder(prefix+"Swivel_collar", (x,y,-.295), .155,.075, graphite, root, role="fixed_socket")
        ring(prefix+"Copper_bearing_rim", (x,y,-.326), .148,.017, copper, root, role="fixed_socket")
        cylinder(prefix+"Gimbal_spindle", (x,y,-.355), .085,.075, titanium, root, role="bearing")
        pivot = empty(prefix+"GIMBAL_PIVOT", (x,y,-.365), root)
        axis = "X" if i in (0,3) else "Y"
        pivot["axis"] = axis
        pivot["max_angle_deg"] = 12.0
        pivot["mount_position_m"] = (x,y,-.365)
        pivots.append((pivot, axis, 1 if i % 2 == 0 else -1))
        # Every moving component is rigidly parented to the same pivot.
        cylinder(prefix+"Inner_motor", (x,y,-.505), .145,.20, graphite, pivot, role="animated_nozzle")
        ring(prefix+"Upper_ceramic_collar", (x,y,-.490), .154,.021, ceramic, pivot)
        cone(prefix+"Flared_vector_bell", (x,y,-.705), .215,.137,.29, titanium, pivot, "animated_nozzle")
        ring(prefix+"Copper_induction_ring", (x,y,-.675), .185,.012, copper, pivot)
        ring(prefix+"Ceramic_exit_lip", (x,y,-.835), .205,.021, ceramic, pivot)
        ring(prefix+"Cyan_exit_trace", (x,y,-.852), .178,.011, cyan, pivot)
        cylinder(prefix+"Dark_exhaust_recess", (x,y,-.856), .151,.016, dark, pivot)
        cylinder(prefix+"Blue_ion_core", (x,y,-.868), .096,.012, cyan, pivot)
        for j in range(4):
            a = j * math.pi / 2
            fx = x + .178 * math.cos(a)
            fy = y + .178 * math.sin(a)
            fin = box(prefix+"Copper_cooling_fin_%02d" % j,
                (fx,fy,-.658), (.028,.038,.17), copper, pivot)
            fin.rotation_euler[2] = a
            cylinder(prefix+"Ceramic_fastener_%02d" % j,
                (x+.17*math.cos(a), y+.17*math.sin(a),-.550),
                .017,.022,ceramic,pivot,vertices=20)
        for j in range(4):
            a = (j+.5) * math.pi/2
            cylinder(prefix+"Amber_telemetry_%02d" % j,
                (x+.135*math.cos(a),y+.135*math.sin(a),-.455),
                .014,.012,amber,pivot,vertices=16)
        # Each nozzle has its own action and gimbal axis.
        component = 0 if axis == "X" else 1
        sign = 1 if i % 2 == 0 else -1
        for frame, amount in ((1,0),(31,sign*ANGLE),(61,0),(91,-sign*ANGLE),(121,0)):
            pivot.rotation_euler = (0,0,0)
            pivot.rotation_euler[component] = amount
            pivot.keyframe_insert(data_path="rotation_euler",frame=frame,group="Vector nozzle gimbal")
        pivot.rotation_euler = (0,0,0)
    scene = bpy.context.scene
    scene.frame_start = 1
    scene.frame_end = FRAMES
    scene.render.fps = 30
    scene.frame_set(1)
    return pivots

def bounds(objects):
    pts = [obj.matrix_world @ Vector(corner) for obj in objects for corner in obj.bound_box]
    return tuple(min(v[k] for v in pts) for k in range(3)), tuple(max(v[k] for v in pts) for k in range(3))

def validate(pivots):
    meshes = [o for o in bpy.data.objects if o.type == "MESH"]
    mats = {m.name for o in meshes for m in o.data.materials if m}
    assert len(meshes) >= 75, "Too few mesh components"
    assert len(mats) == 8, "Expected 8 materials"
    edges = 0
    for obj in meshes:
        assert not obj.data.validate(clean_customdata=False), "Invalid mesh "+obj.name
        bm = bmesh.new()
        bm.from_mesh(obj.data)
        assert all(e.is_manifold for e in bm.edges), "Non-manifold "+obj.name
        assert all(face.calc_area() > 1e-10 for face in bm.faces), "Degenerate face "+obj.name
        edges += len(bm.edges)
        bm.free()
    assert len(pivots) == 4
    min_xy_gap = 999
    min_mount_gap = 999
    plate = bpy.data.objects["Octagonal_mount_base"]
    for frame in range(1,FRAMES+1,2):
        bpy.context.scene.frame_set(frame)
        groups = []
        for pivot,axis,sign in pivots:
            group = [o for o in meshes if o.parent == pivot]
            assert len(group) >= 15, "Incomplete nozzle "+pivot.name
            groups.append(bounds(group))
        plate_min_z = bounds([plate])[0][2]
        for lo,hi in groups:
            min_mount_gap = min(min_mount_gap,plate_min_z-hi[2])
        for i in range(4):
            for j in range(i+1,4):
                a,b=groups[i],groups[j]
                dx=max(0,a[0][0]-b[1][0],b[0][0]-a[1][0])
                dy=max(0,a[0][1]-b[1][1],b[0][1]-a[1][1])
                min_xy_gap=min(min_xy_gap,math.hypot(dx,dy))
    assert min_xy_gap > .25, "Nozzle envelopes collide"
    assert min_mount_gap > .20, "Moving nozzle reaches mounting plate"
    for frame,polarity in ((31,1),(91,-1)):
        bpy.context.scene.frame_set(frame)
        for pivot,axis,sign in pivots:
            component = 0 if axis == "X" else 1
            assert abs(pivot.rotation_euler[component]-polarity*sign*ANGLE)<1e-4
    bpy.context.scene.frame_set(1)
    return dict(model=MODEL,meshes=len(meshes),materials=len(mats),manifold_edges_checked=edges,
        nozzle_pivots=4,animation_frames=FRAMES,animation_fps=30,angle_degrees=12,
        sampled_frames=61,min_inter_nozzle_xy_aabb_gap_m=round(min_xy_gap,4),
        min_mount_plate_vertical_aabb_gap_m=round(min_mount_gap,4),
        limitations="AABB sampled every 2 frames; intentional bearing contact excluded. No continuous triangle collision or Unity test.")

def main():
    args = sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else []
    out = Path(args[args.index("--out")+1]) if "--out" in args else Path("build/M04-024")
    out.mkdir(parents=True,exist_ok=True)
    pivots = make_scene()
    report = validate(pivots)
    bpy.ops.wm.save_as_mainfile(filepath=str((out/(MODEL+".blend")).resolve()))
    bpy.ops.export_scene.gltf(filepath=str((out/(MODEL+".glb")).resolve()),
        export_format="GLB",export_animations=True)
    (out/"manifest.json").write_text(json.dumps(report,indent=2)+"\n",encoding="utf-8")
    print("M04_024_BLENDER_QA_PASS "+json.dumps(report,sort_keys=True))

if __name__=="__main__":
    main()
