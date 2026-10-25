#!/usr/bin/env python3
"""M05-015 — Mechanical cargo gripper, visual-only Blender 4.x generator.
Units: metres. Local +Y is the gripping direction, +Z is up.
Origin (0,0,0) is the rear vehicle mounting plane.
Run: blender -b --python generate_m05_015.py -- --out /tmp/m05-015
"""
import bpy
import bmesh
import math
import json
import sys
from pathlib import Path
from mathutils import Vector

PREFIX = "M05_015"
COLLECTION = PREFIX + "_CARGO_GRIPPER"
OUT = None
M = {}
PARTS = []
FINGERS = []
ANIM = {"open_deg": -12.0, "closed_deg": 16.0, "frames": [1, 20, 40, 60]}

def reset_ours():
    PARTS.clear()
    FINGERS.clear()
    M.clear()
    col = bpy.data.collections.get(COLLECTION)
    if col:
        for obj in list(col.objects):
            data = obj.data
            bpy.data.objects.remove(obj, do_unlink=True)
            if data and hasattr(data, "users") and data.users == 0:
                if isinstance(data, bpy.types.Mesh):
                    bpy.data.meshes.remove(data)
        bpy.data.collections.remove(col)
    for mat in list(bpy.data.materials):
        if mat.name.startswith(PREFIX + "_") and mat.users == 0:
            bpy.data.materials.remove(mat)
    col = bpy.data.collections.new(COLLECTION)
    bpy.context.scene.collection.children.link(col)
    return col

def material(name, rgba, metallic=0, roughness=.35, glow=0):
    mat = bpy.data.materials.new(PREFIX + "_" + name)
    mat.diffuse_color = (*rgba, 1)
    mat.use_nodes = True
    bs = mat.node_tree.nodes.get("Principled BSDF")
    bs.inputs["Base Color"].default_value = (*rgba, 1)
    bs.inputs["Metallic"].default_value = metallic
    bs.inputs["Roughness"].default_value = roughness
    if glow:
        bs.inputs["Emission Color"].default_value = (*rgba, 1)
        bs.inputs["Emission Strength"].default_value = glow
    M[name] = mat
    return mat

def move_into_collection(obj):
    for old in list(obj.users_collection):
        old.objects.unlink(obj)
    bpy.data.collections[COLLECTION].objects.link(obj)
    PARTS.append(obj)
    return obj

def assign(obj, mat):
    obj.data.materials.append(M[mat])
    return obj

def box(name, xyz, size, mat, bevel=.008, parent=None):
    bpy.ops.mesh.primitive_cube_add(size=1, location=xyz)
    obj = bpy.context.object
    obj.name = PREFIX + "_" + name
    move_into_collection(obj)
    obj.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel:
        mod = obj.modifiers.new("machined_edges", "BEVEL")
        mod.width = bevel
        mod.segments = 2
        obj.modifiers.new("weighted_normals", "WEIGHTED_NORMAL")
    assign(obj, mat)
    if parent:
        obj.parent = parent
        obj.location = xyz
    return obj

def cyl(name, xyz, radius, depth, mat, vertices=24, axis="Z", parent=None):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=xyz)
    obj = bpy.context.object
    obj.name = PREFIX + "_" + name
    move_into_collection(obj)
    if axis == "Y":
        obj.rotation_euler[0] = math.pi / 2
    elif axis == "X":
        obj.rotation_euler[1] = math.pi / 2
    assign(obj, mat)
    if parent:
        obj.parent = parent
        obj.location = xyz
    return obj

def torus(name, xyz, major, minor, mat, axis="Y", parent=None):
    bpy.ops.mesh.primitive_torus_add(major_segments=32, minor_segments=8,
        location=xyz, major_radius=major, minor_radius=minor)
    obj = bpy.context.object
    obj.name = PREFIX + "_" + name
    move_into_collection(obj)
    if axis == "Y":
        obj.rotation_euler[0] = math.pi / 2
    assign(obj, mat)
    if parent:
        obj.parent = parent
        obj.location = xyz
    return obj

def empty(name, xyz, parent=None):
    obj = bpy.data.objects.new(PREFIX + "_" + name, None)
    bpy.data.collections[COLLECTION].objects.link(obj)
    PARTS.append(obj)
    obj.empty_display_type = "ARROWS"
    obj.empty_display_size = .075
    if parent:
        obj.parent = parent
    obj.location = xyz
    return obj

def beam(name, a, b, width, depth, mat, parent=None):
    va, vb = Vector(a), Vector(b)
    mid = (va + vb) / 2
    length = (vb - va).length
    obj = box(name, mid, (width, length, depth), mat, .007, parent)
    obj.rotation_euler = (vb - va).to_track_quat("Y", "Z").to_euler()
    return obj

def build():
    reset_ours()
    material("graphite", (.045,.065,.078), .8, .3)
    material("titanium", (.37,.43,.47), .82, .24)
    material("ceramic", (.78,.79,.73), .12, .31)
    material("copper", (.60,.27,.09), .72, .28)
    material("rubber", (.045,.055,.048), .0, .8)
    material("warning", (.95,.44,.035), .22, .35)
    material("signal", (.05,.79,.94), .12, .2, 3.5)
    material("shadow", (.012,.021,.03), .45, .32)

    root = empty("MOUNT_ORIGIN", (0,0,0))
    root["mount_type"] = "rear_frame"
    root["axis_forward"] = "+Y"
    root["units"] = "metres"
    box("rear_mount_plate", (0,.055,0), (.48,.11,.48), "graphite", .025, root)
    box("rear_ceramic_face", (0,.116,0), (.37,.022,.37), "ceramic", .013, root)
    cyl("mount_lock_ring", (0,.135,0), .15,.03,"titanium",32,"Y",root)
    torus("mount_copper_trace", (0,.157,0), .128,.009,"copper","Y",root)
    for x in (-.165,.165):
        for z in (-.165,.165):
            cyl("rear_bolt_%s_%s" % (x,z), (x,.124,z), .025,.024,"titanium",12,"Y",root)
            cyl("rear_bolt_socket_%s_%s" % (x,z), (x,.139,z), .011,.007,"shadow",12,"Y",root)
    cyl("central_spine", (0,.325,0), .105,.43,"graphite",32,"Y",root)
    cyl("front_spine_collar", (0,.548,0), .151,.08,"titanium",32,"Y",root)
    torus("front_status_ring", (0,.603,0), .113,.012,"signal","Y",root)
    cyl("front_ceramic_cap", (0,.608,0), .091,.026,"ceramic",32,"Y",root)
    for k in range(3):
        theta = 2*math.pi*k/3
        rx,rz = math.cos(theta),math.sin(theta)
        # Each radial strut ends before the rotating hinge; no finger intersects the spine.
        beam("radial_strut_%d" % k, (.09*rx,.42,.09*rz),
            (.27*rx,.58,.27*rz), .058,.058,"titanium",root)
        cyl("hinge_bearing_%d" % k, (.27*rx,.63,.27*rz),
            .075,.102,"graphite",24,"Y",root)
        # Static radial carrier rotates the finger's local +X onto the radial axis.
        carrier = empty("FINGER_CARRIER_%d" % k, (.27*rx,.63,.27*rz), root)
        carrier.rotation_euler[1] = -theta
        hinge = empty("FINGER_PIVOT_%d" % k, (0,0,0), carrier)
        hinge["hinge_axis_local"] = "+Z"
        hinge["finger_index"] = k
        FINGERS.append(hinge)
        cyl("hinge_shaft_%d" % k, (0,0,0), .043,.112,"copper",24,"Z",hinge)
        cyl("hinge_endcap_%d" % k, (0,0,.066), .048,.014,"titanium",24,"Z",hinge)
        # Main tapered gripper: the outer segment is a narrower articulated tip.
        beam("finger_beam_%d" % k, (0,.035,0), (0,.325,0), .072,.081,"titanium",hinge)
        box("finger_armor_%d" % k, (0,.175,.049), (.080,.23,.017),
            "ceramic", .009,hinge)
        box("finger_side_stripe_%d" % k, (.047,.17,0), (.012,.17,.028),
            "warning", .004,hinge)
        beam("finger_nose_%d" % k, (0,.31,0), (-.019,.455,0),
            .056,.071,"graphite",hinge)
        box("finger_grip_pad_%d" % k, (-.044,.434,0), (.035,.112,.092),
            "rubber", .009,hinge)
        box("finger_pad_texture_%d" % k, (-.065,.434,0), (.008,.075,.064),
            "ceramic", .003,hinge)
        cyl("finger_tip_lamp_%d" % k, (-.01,.394,.044), .015,.009,
            "signal",16,"Z",hinge)
        # Small decorative hydraulic detail stays on the moving finger.
        cyl("piston_shell_%d" % k, (.039,.222,-.028), .014,.15,
            "copper",16,"Y",hinge)
        cyl("piston_rod_%d" % k, (.039,.308,-.028), .008,.085,
            "titanium",16,"Y",hinge)
        for frame,deg in ((1,-12),(20,3),(40,16),(60,-12)):
            hinge.rotation_euler[2] = math.radians(deg)
            hinge.keyframe_insert(data_path="rotation_euler",frame=frame)
        # Blender 5 uses layered/slotted Actions without Action.fcurves.
        # The default BEZIER interpolation works in Blender 4 and 5.
    # Real mounting and optional cargo target markers are empties, not gameplay.
    for i,(x,z) in enumerate(((-.165,-.165),(-.165,.165),(.165,-.165),(.165,.165))):
        p = empty("MOUNT_BOLT_%d" % i,(x,0,z),root)
        p["role"] = "vehicle_mount"
    target = empty("CARGO_GRIP_CENTRE",(0,1.025,0),root)
    target["role"] = "visual_alignment_only"
    scene = bpy.context.scene
    scene.frame_start,scene.frame_end,scene.render.fps = 1,60,30
    scene.frame_set(1)
    return root

def qa():
    meshes = [o for o in PARTS if o.type == "MESH"]
    assert len(M) >= 3 and len(FINGERS) == 3
    assert all(len(o.data.vertices) >= 8 for o in meshes)
    for obj in meshes:
        bm = bmesh.new()
        bm.from_mesh(obj.data)
        bm.normal_update()
        assert all(e.is_manifold for e in bm.edges), ("non-manifold mesh",obj.name)
        assert bm.calc_volume(signed=True) > 0, ("inverted normals",obj.name)
        bm.free()
    # Fingertip clearance (three radial fingers). Evaluate all keyframes and
    # intermediate poses. The pad has .035 m radial depth, .092 m tangential width.
    # Conservative radial tip bound uses the full arm's 0.49 m reach.
    min_gap = 1e9
    for frame in range(1,61):
        bpy.context.scene.frame_set(frame)
        bpy.context.view_layer.update()
        tips = []
        for hinge in FINGERS:
            p = hinge.matrix_world @ Vector((-.044,.434,0))
            tips.append(p)
        for i in range(3):
            for j in range(i+1,3):
                gap = (tips[i]-tips[j]).length - .12
                min_gap = min(min_gap,gap)
                assert gap > .015, ("fingers too close",frame,i,j,gap)
        # Fingers start beyond the central spine in Y and never enter the
        # front collar (which ends at Y=.621).
        assert all((h.matrix_world @ Vector((0,.035,0))).y > .62 for h in FINGERS)
    bpy.context.scene.frame_set(1)
    assert min_gap > .015
    return {"model":"M05-015","status":"PASS","meshes":len(meshes),
            "vertices":sum(len(o.data.vertices) for o in meshes),
            "materials":len(M),"finger_count":3,"frames_checked":60,
            "minimum_approximate_pad_clearance_m":round(min_gap,5),
            "mount_origin_m":[0,0,0],"forward_axis":"+Y",
            "hinge_axis":"+local Z","finger_open_deg":-12,
            "finger_closed_deg":16,
            "note":"Pad-centre clearance is a conservative proxy, not full mesh collision detection."}

def export_all(out):
    out.mkdir(parents=True,exist_ok=True)
    for obj in bpy.context.selected_objects:
        obj.select_set(False)
    for obj in PARTS:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = PARTS[0]
    bpy.ops.export_scene.gltf(filepath=str(out/"M05-015.glb"),
        export_format="GLB",use_selection=True,export_animations=True)
    bpy.ops.export_scene.fbx(filepath=str(out/"M05-015.fbx"),
        use_selection=True,add_leaf_bones=False,apply_unit_scale=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(out/"M05-015.blend"))

def main():
    global OUT
    args = sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else []
    OUT = Path(args[args.index("--out")+1]) if "--out" in args else None
    build()
    result = qa()
    # A second build in the same Blender process verifies repeatability.
    before = (result["meshes"],result["vertices"],result["materials"])
    build()
    again = qa()
    after = (again["meshes"],again["vertices"],again["materials"])
    assert before == after, ("non-repeatable build",before,after)
    result["repeat_build"] = "PASS"
    if OUT:
        export_all(OUT)
        (OUT/"qa.json").write_text(json.dumps(result,indent=2)+"\n",encoding="utf-8")
    print("M05_015_QA " + json.dumps(result,sort_keys=True))

if __name__ == "__main__":
    main()
