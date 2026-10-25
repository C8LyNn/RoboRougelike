#!/usr/bin/env python3
"""M02-018 mechanical disc brake visual asset for RoboRougelike / Unity 6.
Blender 4+; Z-up; metres; mount datum (0,0,0), wheel axis +Y at Z=.36 m.
Rotor is perforated, caliper is fixed, opposed pads close without touching rotor.
Run: blender -b --factory-startup --python-exit-code 1 --python generate_m02_018.py -- --out /tmp/M02-018
"""
from __future__ import annotations
import argparse, json, math, sys
from pathlib import Path

ID = "M02-018"
FRAMES = 121
ROTOR_R = .35
ROTOR_HALF_THICKNESS = .009
PAD_HALF_THICKNESS = .005
PAD_INITIAL_GAP = .004
PAD_TRAVEL = .003
PAD_FINAL_GAP = PAD_INITIAL_GAP - PAD_TRAVEL
ROTOR_CENTER_Z = .36
MATERIALS = ("rotor_steel", "graphite_chassis", "titanium_caliper",
             "copper_hardware", "ceramic_friction", "rubber_seals", "safety_amber",
             "cyan_indicator")

def source_self_test():
    assert len(MATERIALS) >= 3
    assert abs(PAD_FINAL_GAP - .001) < 1e-10
    assert ROTOR_R < .36
    samples = []
    for frame in range(1, FRAMES + 1):
        # Closing occurs during the central part of the animation, then releases.
        t = (frame - 1) / (FRAMES - 1)
        clamp = max(0., min(1., (t - .18) / .20, (.84 - t) / .20))
        gap = PAD_INITIAL_GAP - PAD_TRAVEL * clamp
        assert gap >= .000999999
        samples.append(gap)
    return {"id": ID, "sampled_kinematic_frames": len(samples),
            "minimum_pad_rotor_axial_gap_m": min(samples),
            "initial_pad_rotor_axial_gap_m": PAD_INITIAL_GAP,
            "rotor_outer_diameter_m": 2 * ROTOR_R,
            "mount_pivot_m": [0,0,0], "wheel_axis": "+Y",
            "rotor_axis_center_m": [0,0,ROTOR_CENTER_Z],
            "materials_expected": len(MATERIALS),
            "collision_scope": "analytic pad/rotor axial clearance and bridge envelope; no full PhysX"}

def build(out: Path):
    import bpy, bmesh
    from mathutils import Vector
    out.mkdir(parents=True, exist_ok=True)
    previous = bpy.data.collections.get(ID)
    if previous:
        for ob in list(previous.objects):
            bpy.data.objects.remove(ob, do_unlink=True)
        bpy.data.collections.remove(previous)
    coll = bpy.data.collections.new(ID)
    bpy.context.scene.collection.children.link(coll)
    scene = bpy.context.scene
    scene.unit_settings.system = "METRIC"
    scene.unit_settings.scale_length = 1.0
    scene.frame_start, scene.frame_end = 1, FRAMES
    scene.render.fps = 30

    def material(name, rgb, metal, rough, emission=0.0):
        m = bpy.data.materials.new(name)
        m.diffuse_color = (*rgb, 1.)
        m.use_nodes = True
        p = m.node_tree.nodes.get("Principled BSDF")
        p.inputs["Base Color"].default_value = (*rgb, 1.)
        p.inputs["Metallic"].default_value = metal
        p.inputs["Roughness"].default_value = rough
        if emission and p.inputs.get("Emission Color"):
            p.inputs["Emission Color"].default_value = (*rgb, 1.)
            p.inputs["Emission Strength"].default_value = emission
        return m

    mats = {
        "rotor_steel": material("rotor_steel", (.24,.31,.35), .92, .29),
        "graphite_chassis": material("graphite_chassis", (.055,.07,.09), .64, .45),
        "titanium_caliper": material("titanium_caliper", (.42,.49,.55), .86, .28),
        "copper_hardware": material("copper_hardware", (.69,.30,.10), .78, .28),
        "ceramic_friction": material("ceramic_friction", (.13,.12,.11), .12, .77),
        "rubber_seals": material("rubber_seals", (.027,.033,.041), .0, .9),
        "safety_amber": material("safety_amber", (.93,.36,.025), .35, .38),
        "cyan_indicator": material("cyan_indicator", (.04,.64,.78), .20, .29, 1.2)
    }

    def own(ob, name, mat=None, parent=None):
        ob.name = name
        for c in list(ob.users_collection):
            c.objects.unlink(ob)
        coll.objects.link(ob)
        if mat and ob.type == "MESH":
            ob.data.materials.append(mats[mat])
        if parent is not None:
            ob.parent = parent
        return ob

    def empty(name, loc, parent=None):
        ob = bpy.data.objects.new(name, None)
        coll.objects.link(ob)
        if parent is not None: ob.parent = parent
        ob.location = loc
        ob.empty_display_type = "PLAIN_AXES"
        ob.empty_display_size = .045
        return ob

    def cylinder(name, radius, depth, loc, mat, parent=None, vertices=48, axis="Y"):
        rot = (math.pi/2,0,0) if axis == "Y" else (0,0,0)
        bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius,
                                            depth=depth, location=(0,0,0), rotation=rot)
        ob = own(bpy.context.object, name, mat, parent)
        ob.location = loc
        return ob

    def cube(name, dimensions, loc, mat, parent=None, bevel=.005):
        bpy.ops.mesh.primitive_cube_add(size=1, location=(0,0,0))
        ob = own(bpy.context.object, name, mat, parent)
        ob.location = loc
        ob.dimensions = dimensions
        bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
        if bevel:
            mod = ob.modifiers.new("Machined radius", "BEVEL")
            mod.width = bevel
            mod.segments = 2
            if hasattr(mod, "affect"): mod.affect = "EDGES"
            bpy.context.view_layer.objects.active = ob
            bpy.ops.object.modifier_apply(modifier=mod.name)
            normal = ob.modifiers.new("Weighted corner normals", "WEIGHTED_NORMAL")
        return ob

    mount = empty("MOUNT_DATUM_ORIGIN", (0,0,0))
    empty("CHASSIS_MOUNT_PLANE", (0,0,0), mount)
    rotor = empty("ROTOR_SPIN_Y", (0,0,ROTOR_CENTER_Z), mount)
    empty("WHEEL_AXLE_SOCKET_Y", (0,.13,0), rotor)
    # Disc rotor: 18 genuine through-holes, boolean cut from the disc body.
    disc = cylinder("PERFORATED_STEEL_ROTOR", ROTOR_R, 2*ROTOR_HALF_THICKNESS,
                    (0,0,0), "rotor_steel", rotor, 128)
    cutters = []
    for i in range(18):
        a = 2*math.pi*i/18
        bpy.ops.mesh.primitive_cylinder_add(vertices=16, radius=.014, depth=.09,
                                            location=(.265*math.cos(a),0,.265*math.sin(a)),
                                            rotation=(math.pi/2,0,0))
        cutters.append(bpy.context.object)
    bpy.ops.object.select_all(action="DESELECT")
    for cutter in cutters: cutter.select_set(True)
    bpy.context.view_layer.objects.active = cutters[0]
    bpy.ops.object.join()
    cutter = bpy.context.object
    cutter.name = "TEMP_DISC_DRILL_JIG"
    boolean = disc.modifiers.new("18 actual ventilation holes", "BOOLEAN")
    boolean.operation = "DIFFERENCE"
    boolean.object = cutter
    if hasattr(boolean, "solver"): boolean.solver = "EXACT"
    bpy.context.view_layer.objects.active = disc
    bpy.ops.object.modifier_apply(modifier=boolean.name)
    bpy.data.objects.remove(cutter, do_unlink=True)

    cylinder("ROTOR_HAT", .139, .038, (0,0,0), "graphite_chassis", rotor, 64)
    cylinder("AXLE_SPLINE", .069, .16, (0,0,0), "titanium_caliper", rotor, 48)
    cylinder("CENTER_HUB_BOSS", .093, .058, (0,0,0), "copper_hardware", rotor, 48)
    for i in range(8):
        a = 2*math.pi*i/8
        cylinder("HUB_BOLT_%02d"%i, .009, .052,
                 (.115*math.cos(a),0,.115*math.sin(a)),
                 "copper_hardware", rotor, 16)
    for i in range(12):
        a = 2*math.pi*i/12
        cylinder("ROTOR_WARNING_RIVET_%02d"%i, .006, .022,
                 (.332*math.cos(a),0,.332*math.sin(a)),
                 "safety_amber", rotor, 12)

    # Fixed C-shaped caliper above the disc, no solid housing crossing the disc.
    caliper = empty("FIXED_CALIPER_CARRIER", (0,0,ROTOR_CENTER_Z), mount)
    cube("CALIPER_TOP_BRIDGE", (.29,.12,.037), (0,0,.412),
         "titanium_caliper", caliper, .009)
    for sign, side in ((-1,"LEFT"),(1,"RIGHT")):
        cube("CALIPER_OUTER_CHEEK_"+side, (.17,.026,.125),
             (0,sign*.053,.335), "titanium_caliper", caliper, .009)
        cube("CALIPER_GUIDE_RAIL_"+side, (.22,.021,.027),
             (0,sign*.057,.224), "graphite_chassis", caliper, .004)
        for x in (-.094,.094):
            cylinder("CALIPER_GUIDE_BOLT_%s_%s"%(side,str(x)), .012, .03,
                     (x,sign*.072,.275), "copper_hardware", caliper, 16)
    for x in (-.128,.128):
        cube("FIXED_MOUNT_TAB_%s"%str(x), (.055,.095,.043),
             (x,0,.445), "graphite_chassis", caliper, .004)
        cylinder("MOUNTING_STUD_%s"%str(x), .014, .12,
                 (x,0,.445), "copper_hardware", caliper, 20)
    # Opposed ceramic pads; their inner faces are always >=1mm from rotor faces.
    pad_roots = []
    for sign, side in ((-1,"NEG_Y"),(1,"POS_Y")):
        pad_root = empty("PAD_SLIDE_"+side, (0,0,0), caliper)
        pad_roots.append((sign,pad_root))
        pad_y = sign*(ROTOR_HALF_THICKNESS+PAD_INITIAL_GAP+PAD_HALF_THICKNESS)
        cube("FRICTION_PAD_"+side, (.151,2*PAD_HALF_THICKNESS,.112),
             (0,pad_y,.273), "ceramic_friction", pad_root, .004)
        cube("PAD_BACKING_PLATE_"+side, (.165,.009,.122),
             (0,sign*.026,.273), "copper_hardware", pad_root, .004)
        for x in (-.055,.055):
            cylinder("PAD_RETENTION_PIN_%s_%s"%(side,str(x)), .009, .019,
                     (x,sign*.034,.273), "graphite_chassis", pad_root, 16)
    # Manual cable-actuated cam/lever outside the rotor swept volume.
    lever_root = empty("CABLE_CAM_LEVER_PIVOT_Y", (.215,.10,.74), mount)
    cylinder("CABLE_CAM_BOSS", .035, .022, (0,0,0),
             "copper_hardware", lever_root, 24)
    cube("CABLE_ACTUATION_ARM", (.025,.025,.14), (.048,0,.067),
         "safety_amber", lever_root, .004)
    cylinder("LEVER_TIP", .021, .042, (.048,0,.134),
             "rubber_seals", lever_root, 20)
    cube("CABLE_OUTER_ANCHOR", (.05,.032,.038), (.19,.10,.81),
         "graphite_chassis", mount, .005)
    for i in range(4):
        cube("STATUS_LIGHT_%02d"%i, (.022,.008,.012),
             (-.081+i*.054,.069,.76), "cyan_indicator", mount, .003)

    # A single brake application and release, while the rotor spins twice.
    keys = ((1,0.),(22,0.),(46,1.),(76,1.),(102,0.),(121,0.))
    for frame, factor in keys:
        for sign, pad in pad_roots:
            pad.location.y = -sign*PAD_TRAVEL*factor
            pad.keyframe_insert(data_path="location", frame=frame)
        lever_root.rotation_euler.y = -.15*factor
        lever_root.keyframe_insert(data_path="rotation_euler", frame=frame)
    for frame, angle in ((1,0.),(121,4*math.pi)):
        rotor.rotation_euler.y = angle
        rotor.keyframe_insert(data_path="rotation_euler", frame=frame)
    for ob in (rotor,lever_root,*[x[1] for x in pad_roots]):
        if ob.animation_data and ob.animation_data.action:
            action = ob.animation_data.action
            for curve in getattr(action, "fcurves", []):
                for point in curve.keyframe_points:
                    point.interpolation = "LINEAR"

    # Blender checks: actual mesh topology and world-space pad/rotor separation.
    scene.frame_set(1)
    bpy.context.view_layer.update()
    meshes = [ob for ob in coll.objects if ob.type=="MESH"]
    assert len(meshes)>=45, len(meshes)
    invalid = []
    for ob in meshes:
        bm = bmesh.new()
        bm.from_mesh(ob.data)
        if not bm.faces or any(not e.is_manifold for e in bm.edges):
            invalid.append(ob.name)
        bm.free()
    assert not invalid, ("non-manifold mesh objects",invalid)
    assert len(mats)>=3
    sampled = list(range(1,FRAMES+1))
    minimum_gap = 1.0
    for frame in sampled:
        scene.frame_set(frame)
        bpy.context.view_layer.update()
        for sign,pad in pad_roots:
            inner = ROTOR_HALF_THICKNESS + PAD_INITIAL_GAP - PAD_TRAVEL*(
                (-sign*pad.location.y)/PAD_TRAVEL)
            gap = inner - ROTOR_HALF_THICKNESS
            minimum_gap = min(minimum_gap,gap)
            assert gap >= .000999, (frame,sign,gap)
    assert .412-.037/2 > ROTOR_R, "top bridge intersects swept rotor"
    scene.frame_set(1)
    qa = {**source_self_test(), "blender_version":bpy.app.version_string,
          "mesh_objects":len(meshes),"material_count":len(mats),
          "disc_ventilation_holes":18,
          "tested_blender_frames":len(sampled),
          "min_analytic_gap_m":minimum_gap,
          "non_manifold_meshes":invalid,
          "glb_status":"NOT RUN","fbx_status":"NOT RUN",
          "unity_import":"NOT RUN"}
    bpy.ops.wm.save_as_mainfile(filepath=str(out/(ID+".blend")))
    bpy.ops.export_scene.gltf(filepath=str(out/(ID+"_animated.glb")),
                              export_format="GLB", export_animations=True)
    qa["glb_status"]="EXPORTED_NOT_UNITY_IMPORTED"
    try:
        bpy.ops.export_scene.fbx(filepath=str(out/(ID+"_animated.fbx")),
                                 use_selection=False, bake_anim=True)
        qa["fbx_status"]="EXPORTED_NOT_UNITY_IMPORTED"
    except Exception as exc:
        qa["fbx_status"]="BLOCKED_FBX_OPERATOR: "+str(exc)
    (out/(ID+"_qa.json")).write_text(json.dumps(qa,indent=2)+"\n",encoding="utf-8")
    print("M02-018 QA",json.dumps(qa))

if __name__=="__main__":
    if "--" not in sys.argv:
        print(json.dumps(source_self_test(),indent=2))
    else:
        parser=argparse.ArgumentParser()
        parser.add_argument("--out",required=True,type=Path)
        build(parser.parse_args(sys.argv[sys.argv.index("--")+1:]).out)
