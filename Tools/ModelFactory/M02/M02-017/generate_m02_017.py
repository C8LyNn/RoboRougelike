#!/usr/bin/env python3
"""M02-017 — open off-road differential, Blender 4/5 procedural visual asset.
Usage: python generate_m02_017.py --self-test
       blender -b --python generate_m02_017.py -- --export-dir /tmp/m02-017
All dimensions are metres. No gameplay changes; animation is demonstrative.
"""
import argparse
import json
import math
import sys
from pathlib import Path

ID = "M02-017"
AXLE_Z = 0.35
CARRIER_R = 0.255
GUARD_R = 0.345
SIDE_GEAR_X = 0.130
SPIDER_Y = 0.130
SIDE_R = 0.102
SPIDER_R = 0.080
FRAMES = 121
FPS = 30
MATERIALS = {
    "graphite_frame": ((0.085, 0.11, 0.14, 1), 0.72, 0.38),
    "machined_steel": ((0.40, 0.48, 0.54, 1), 0.88, 0.23),
    "bronze_gears": ((0.56, 0.30, 0.12, 1), 0.75, 0.31),
    "ceramic_seals": ((0.08, 0.12, 0.16, 1), 0.05, 0.48),
    "rubber_isolators": ((0.025, 0.035, 0.04, 1), 0.0, 0.78),
    "safety_orange": ((0.96, 0.31, 0.04, 1), 0.22, 0.36),
    "inspection_cyan": ((0.06, 0.67, 0.75, 1), 0.24, 0.22),
}
# Carrier rotation +1 turn; left output +1.5; right output +0.5.
# Spider relative spin -5/6 turns for nominal 20:12 side/spider teeth.
def pose(t):
    return {
        "carrier": 2 * math.pi * t,
        "left": 3 * math.pi * t,
        "right": math.pi * t,
        "spider": -(5 * math.pi / 3) * t,
    }

def math_qa():
    assert GUARD_R - (CARRIER_R + 0.030) >= 0.055
    assert SIDE_GEAR_X - 0.028 - SPIDER_R >= 0.020
    assert AXLE_Z - (CARRIER_R + 0.030) >= 0.065 - 1e-9
    for i in range(121):
        p = pose(i / 120)
        assert abs((p["left"] + p["right"]) / 2 - p["carrier"]) < 1e-12
        assert abs(p["spider"] + (p["left"] - p["right"]) * (20 / 24)) < 1e-12
    return {"poses": 121, "kinematics": "PASS",
            "radial_guard_clearance_m": round(GUARD_R-CARRIER_R-0.030, 4),
            "side_to_spider_axial_bound_m": round(SIDE_GEAR_X-0.028-SPIDER_R, 4),
            "minimum_underbody_clearance_m": round(AXLE_Z-CARRIER_R-0.030, 4)}

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--self-test", action="store_true")
    ap.add_argument("--export-dir", default="generated")
    args = ap.parse_args(sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else sys.argv[1:])
    qa = math_qa()
    if args.self_test:
        print(json.dumps(qa, indent=2))
        return
    import bpy
    from mathutils import Vector

    collection_name = ID + "_OFFROAD_DIFFERENTIAL"
    old = bpy.data.collections.get(collection_name)
    if old:
        for obj in list(old.objects):
            bpy.data.objects.remove(obj, do_unlink=True)
        bpy.data.collections.remove(old)
    coll = bpy.data.collections.new(collection_name)
    bpy.context.scene.collection.children.link(coll)

    def move_to_collection(obj):
        for c in list(obj.users_collection):
            c.objects.unlink(obj)
        coll.objects.link(obj)
        return obj

    mats = {}
    for name, (rgba, metallic, roughness) in MATERIALS.items():
        mat = bpy.data.materials.get(ID+"_"+name) or bpy.data.materials.new(ID+"_"+name)
        mat.diffuse_color = rgba
        mat.use_nodes = True
        bsdf = mat.node_tree.nodes.get("Principled BSDF")
        if bsdf:
            bsdf.inputs["Base Color"].default_value = rgba
            bsdf.inputs["Metallic"].default_value = metallic
            bsdf.inputs["Roughness"].default_value = roughness
        mats[name] = mat

    def material(obj, name):
        obj.data.materials.clear()
        obj.data.materials.append(mats[name])
        return obj

    def bevel(obj, width=0.004, segments=2):
        mod = obj.modifiers.new("Machined edge radii", "BEVEL")
        mod.width = width
        mod.segments = segments
        if hasattr(mod, 'affect'): mod.affect = 'EDGES'
        obj.modifiers.new("Weighted corner normals", "WEIGHTED_NORMAL")
        return obj

    def cube(name, loc, dims, mat, parent=None, angle_x=0, bev=0.004):
        bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
        obj = move_to_collection(bpy.context.object)
        obj.name = ID+"_"+name
        obj.dimensions = dims
        bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
        obj.rotation_euler.x = angle_x
        if parent: obj.parent = parent
        material(obj, mat)
        if bev: bevel(obj, min(bev, min(dims)*0.25))
        return obj

    def cyl(name, loc, radius, depth, mat, axis="Z", parent=None, vertices=48):
        bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=loc)
        obj = move_to_collection(bpy.context.object)
        obj.name = ID+"_"+name
        if axis == "X": obj.rotation_euler.y = math.pi/2
        if axis == "Y": obj.rotation_euler.x = math.pi/2
        if parent: obj.parent = parent
        material(obj, mat)
        bevel(obj, min(0.003, radius*0.12))
        return obj

    def torus(name, loc, major, minor, mat, axis="X", parent=None):
        bpy.ops.mesh.primitive_torus_add(major_segments=64, minor_segments=12,
            location=loc, major_radius=major, minor_radius=minor)
        obj = move_to_collection(bpy.context.object)
        obj.name = ID+"_"+name
        if axis == "X": obj.rotation_euler.y = math.pi/2
        if axis == "Y": obj.rotation_euler.x = math.pi/2
        if parent: obj.parent = parent
        material(obj, mat)
        return obj

    def root(name, loc):
        obj = bpy.data.objects.new(ID+"_"+name, None)
        coll.objects.link(obj)
        obj.empty_display_type = 'CIRCLE'
        obj.empty_display_size = .08
        obj.location = loc
        return obj

    def rod(name, start, end, radius, mat, parent=None):
        a, b = Vector(start), Vector(end)
        center = (a+b)*0.5
        obj = cyl(name, center, radius, (b-a).length, mat, axis="Z", parent=parent, vertices=16)
        obj.rotation_euler = (b-a).to_track_quat('Z','Y').to_euler()
        return obj

    mount_socket = root('CHASSIS_MOUNT_SOCKET', (0,0,0))
    # Stationary chassis bracket: the origin is the top face of the mounting datum.
    cube("MOUNTING_DATUM", (0,0,-.018), (.58,.25,.036), "graphite_frame")
    for x in (-.235,.235):
        for y in (-.085,.085):
            cyl("MOUNT_BOLT", (x,y,.007), .012,.014,"safety_orange",vertices=16)
    for sign in (-1,1):
        x=sign*.405
        cube("BEARING_PEDESTAL", (x,0,.135), (.070,.090,.27), "graphite_frame")
        torus("STATIONARY_BEARING", (x,0,AXLE_Z), .055,.012,"machined_steel")
        torus("BEARING_SEAL", (x-sign*.016,0,AXLE_Z), .041,.008,"ceramic_seals")
        for y in (-.06,.06):
            rod("PEDESTAL_BRACE", (x,y,.02),(x,0,.20),.012,"graphite_frame")
    # Static guard rods are outside the swept moving envelope.
    for a in range(8):
        ang = a*2*math.pi/8
        y = GUARD_R*math.sin(ang)
        z = AXLE_Z + GUARD_R*math.cos(ang)
        rod("OUTER_SAFETY_RAIL", (-.33,y,z),(.33,y,z),.010,"graphite_frame")
        if a in (0,2,4,6):
            cyl("GUARD_BOLT",(-.335,y,z),.013,.015,"safety_orange",axis="X",vertices=12)

    carrier = root("CARRIER_RIG", (0,0,AXLE_Z))
    left = root("LEFT_OUTPUT_RIG", (0,0,AXLE_Z))
    right = root("RIGHT_OUTPUT_RIG", (0,0,AXLE_Z))
    spider = root("SPIDER_RIG", (0,0,0))
    spider.parent = carrier
    for label, rig, x in (("LEFT",left,-.51),("RIGHT",right,.51)):
        sock = root(label+"_OUTPUT_SOCKET", (x,0,0))
        sock.parent = rig

    # Rotating carrier cage and visible crown ring.
    for x in (-.23,.23):
        torus("CARRIER_END_RING",(x,0,0),.238,.012,"machined_steel",parent=carrier)
    torus("CROWN_GEAR_CORE",(.205,0,0),CARRIER_R,.016,"bronze_gears",parent=carrier)
    for i in range(32):
        a = 2*math.pi*i/32
        y,z=CARRIER_R*math.sin(a),CARRIER_R*math.cos(a)
        cube("CROWN_TOOTH_%02d"%i,(.205,y,z),(.033,.032,.034),
             "bronze_gears",parent=carrier,angle_x=-a,bev=.002)
    for i in range(8):
        a=2*math.pi*i/8
        y,z=.236*math.sin(a),.236*math.cos(a)
        rod("CARRIER_SPACER_%02d"%i,(-.23,y,z),(.23,y,z),.009,"machined_steel",parent=carrier)

    # Side output gears and shafts; shaft clearance through stationary bearing.
    for sign, rig, label in ((-1,left,"LEFT"),(1,right,"RIGHT")):
        x=sign*SIDE_GEAR_X
        cyl(label+"_SHAFT",(sign*.315,0,0),.024,.370,"machined_steel",axis="X",parent=rig)
        cyl(label+"_SIDE_GEAR",(x,0,0),.090,.055,"bronze_gears",axis="X",parent=rig)
        torus(label+"_GEAR_SHOULDER",(x,0,0),.083,.010,"machined_steel",parent=rig)
        for i in range(20):
            a=2*math.pi*i/20
            y,z=.098*math.sin(a),.098*math.cos(a)
            cube(label+"_TOOTH_%02d"%i,(x,y,z),(.052,.018,.020),
                 "bronze_gears",parent=rig,angle_x=-a,bev=.0015)
        cyl(label+"_SPLINE_END",(sign*.49,0,0),.030,.025,
            "inspection_cyan",axis="X",parent=rig,vertices=12)

    # Two small satellite gears on Y-axis, inside the rotating carrier.
    for sign in (-1,1):
        y=sign*SPIDER_Y
        cyl("SPIDER_PINION",(0,y,0),.068,.040,"machined_steel",axis="Y",parent=spider)
        for i in range(12):
            a=2*math.pi*i/12
            x,z=.078*math.sin(a),.078*math.cos(a)
            cube("SPIDER_TOOTH_%d_%02d"%(sign,i),(x,y,z),(.015,.037,.017),
                 "bronze_gears",parent=spider,angle_x=0,bev=.001)
    cyl("SPIDER_CROSS_SHAFT",(0,0,0),.016,.330,"machined_steel",axis="Y",parent=spider)
    # Reference markings attached to carrier, outside side-gear sweep.
    for i in range(4):
        a=i*math.pi/2
        y,z=.238*math.sin(a),.238*math.cos(a)
        cube("CARRIER_ORANGE_MARK_%d"%i,(-.23,y,z),(.012,.015,.025),
             "safety_orange",parent=carrier,angle_x=-a,bev=.001)

    scene = bpy.context.scene
    scene.render.fps = FPS
    scene.frame_start = 1
    scene.frame_end = FRAMES
    for frame in range(1,FRAMES+1,3):
        t=(frame-1)/(FRAMES-1)
        p=pose(t)
        for obj,angle,axis in ((carrier,p["carrier"],"X"),
                               (left,p["left"],"X"),
                               (right,p["right"],"X"),
                               (spider,p["spider"],"Y")):
            obj.rotation_mode='XYZ'
            if axis=="X": obj.rotation_euler.x=angle
            else: obj.rotation_euler.y=angle
            obj.keyframe_insert(data_path="rotation_euler",frame=frame)
    for obj in (carrier,left,right,spider):
        if obj.animation_data and obj.animation_data.action:
            action=obj.animation_data.action
            if hasattr(action,"fcurves"):
                for curve in action.fcurves:
                    for point in curve.keyframe_points:
                        point.interpolation='LINEAR'
    # Independent evaluated-triangle BVH checks of high-risk inter-group pairs.
    # These pairs are designed to have clearance, not intentional gear contact.
    from mathutils.bvhtree import BVHTree
    collision_pairs = [
        ("LEFT_SIDE_GEAR","SPIDER_PINION"),
        ("RIGHT_SIDE_GEAR","SPIDER_PINION"),
        ("LEFT_SIDE_GEAR","CROWN_GEAR_CORE"),
        ("RIGHT_SIDE_GEAR","CROWN_GEAR_CORE"),
        ("CROWN_GEAR_CORE","OUTER_SAFETY_RAIL"),
    ]
    collision_checks = 0
    for frame in (1,31,61,91,121):
        scene.frame_set(frame)
        depsgraph = bpy.context.evaluated_depsgraph_get()
        cache = {}
        def world_bvh(obj):
            if obj.name in cache: return cache[obj.name]
            evaluated = obj.evaluated_get(depsgraph)
            mesh = evaluated.to_mesh()
            try:
                verts = [evaluated.matrix_world @ v.co for v in mesh.vertices]
                faces = [tuple(p.vertices) for p in mesh.polygons]
                bvh = BVHTree.FromPolygons(verts, faces, all_triangles=False) if faces else None
            finally:
                evaluated.to_mesh_clear()
            cache[obj.name] = bvh
            return bvh
        for name_a,name_b in collision_pairs:
            aa = [o for o in coll.objects if o.type=='MESH' and name_a in o.name]
            bb = [o for o in coll.objects if o.type=='MESH' and name_b in o.name]
            for a in aa:
                for b in bb:
                    collision_checks += 1
                    ba,bbvh=world_bvh(a),world_bvh(b)
                    if ba and bbvh and ba.overlap(bbvh):
                        raise RuntimeError("Unexpected moving-part BVH collision: %s / %s at frame %d" % (a.name,b.name,frame))
    qa["bvh_pair_checks"] = collision_checks
    qa["bvh_sampled_frames"] = [1,31,61,91,121]
    scene.frame_set(1)
    # Never silently leave geometry in a non-unity scale.
    scene.unit_settings.system = 'METRIC'
    scene.unit_settings.scale_length = 1.0
    out=Path(args.export_dir).resolve()
    out.mkdir(parents=True,exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(out/(ID+".blend")))
    bpy.ops.export_scene.gltf(filepath=str(out/(ID+"_animated.glb")),
        export_format="GLB", export_animations=True, use_selection=False)
    qa.update({"blender_version":bpy.app.version_string,
        "objects":len(coll.objects),
        "meshes":sum(o.type=='MESH' for o in coll.objects),
        "materials":len(MATERIALS),"frames":FRAMES,
        "output_blend":str(out/(ID+".blend")),
        "output_glb":str(out/(ID+"_animated.glb")),
        "limitations":"Analytic envelope clearance; no triangle BVH or Unity physics."})
    (out/(ID+"_qa.json")).write_text(json.dumps(qa,indent=2)+"\n",encoding="utf-8")
    print(json.dumps(qa,indent=2))

if __name__=="__main__":
    main()
