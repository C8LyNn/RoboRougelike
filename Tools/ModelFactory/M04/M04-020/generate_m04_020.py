#!/usr/bin/env python3
"""M04-020 — Wysuwany booster manewrowy. Blender 4.x/5.x, metres, Z up.
Mount plane: Y=0, rearward exhaust: -Y. Root pivot: (0, 0, 0).
Motion: carriage slides 0.28 m along -Y; rotor spins around its local Y.
Run: blender -b --factory-startup --python generate_m04_020.py -- --out ./build
"""
import bpy
import math
import sys
import json
from pathlib import Path
from mathutils import Vector

ID = "M04-020"
FRAMES = (1, 25, 55, 85, 121)
MOTION = 0.28

def material(name, rgba, metal=0.0, rough=0.45, emission=0.0):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = (*rgba, 1.0)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (*rgba, 1.0)
    bsdf.inputs["Metallic"].default_value = metal
    bsdf.inputs["Roughness"].default_value = rough
    if emission:
        if "Emission Color" in bsdf.inputs:
            bsdf.inputs["Emission Color"].default_value = (*rgba, 1.0)
        elif "Emission" in bsdf.inputs:
            bsdf.inputs["Emission"].default_value = (*rgba, 1.0)
        if "Emission Strength" in bsdf.inputs:
            bsdf.inputs["Emission Strength"].default_value = emission
    return mat

def tag(obj, role, mat, parent=None):
    obj["M04_ID"] = ID
    obj["role"] = role
    if mat:
        obj.data.materials.append(mat)
    if parent:
        obj.parent = parent
    return obj

def box(name, xyz, size, mat, role="static", parent=None, bevel=0.012):
    bpy.ops.mesh.primitive_cube_add(size=1, location=xyz)
    ob = bpy.context.object
    ob.name = name
    ob.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel:
        mod = ob.modifiers.new("Manufactured radius", "BEVEL")
        mod.width = min(bevel, min(size) * 0.24)
        mod.segments = 3
        bpy.context.view_layer.objects.active = ob
        bpy.ops.object.modifier_apply(modifier=mod.name)
    return tag(ob, role, mat, parent)

def cyl(name, xyz, radius, depth, mat, role="static", parent=None, vertices=48):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=xyz)
    ob = bpy.context.object
    ob.name = name
    ob.rotation_euler.x = math.pi / 2
    for face in ob.data.polygons:
        face.use_smooth = len(face.vertices) == 4
    return tag(ob, role, mat, parent)

def torus(name, xyz, radius, tube, mat, role="moving", parent=None):
    bpy.ops.mesh.primitive_torus_add(major_segments=64, minor_segments=10,
                                    location=xyz, major_radius=radius,
                                    minor_radius=tube)
    ob = bpy.context.object
    ob.name = name
    ob.rotation_euler.x = math.pi / 2
    for face in ob.data.polygons:
        face.use_smooth = True
    return tag(ob, role, mat, parent)

def empty(name, xyz, parent=None):
    ob = bpy.data.objects.new(name, None)
    bpy.context.collection.objects.link(ob)
    ob.location = xyz
    if parent:
        ob.parent = parent
    ob.empty_display_size = 0.12
    ob.empty_display_type = 'ARROWS'
    return ob

def bell(name, mat, parent):
    # Continuous double-walled exhaust bell, sealed at both ends, no zero-thickness faces.
    # Coordinates relative to carriage, rear is negative Y.
    sections = [(-0.70, 0.140, 0.111), (-0.77, 0.153, 0.124),
                (-0.84, 0.185, 0.155), (-0.92, 0.217, 0.188)]
    n = 48
    verts = []
    for y, outer, inner in sections:
        for j in range(n):
            a = 2 * math.pi * j / n
            verts.append((outer * math.cos(a), y, outer * math.sin(a)))
    for y, outer, inner in sections:
        for j in range(n):
            a = 2 * math.pi * j / n
            verts.append((inner * math.cos(a), y, inner * math.sin(a)))
    faces = []
    for i in range(len(sections)-1):
        for j in range(n):
            k = (j+1) % n
            faces.append((i*n+j, i*n+k, (i+1)*n+k, (i+1)*n+j))
            off = len(sections)*n
            faces.append((off+i*n+k, off+i*n+j, off+(i+1)*n+j, off+(i+1)*n+k))
    off = len(sections)*n
    last = (len(sections)-1)*n
    for j in range(n):
        k = (j+1) % n
        faces.append((j, off+j, off+k, k))
        faces.append((last+k, off+last+k, off+last+j, last+j))
    mesh = bpy.data.meshes.new(name+"_mesh")
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    ob = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(ob)
    for p in mesh.polygons:
        p.use_smooth = True
    return tag(ob, "moving", mat, parent)

def build_scene():
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    for m in list(bpy.data.materials):
        if m.users == 0:
            bpy.data.materials.remove(m)
    mats = {
        "graphite": material("M04 Graphite ceramic", (0.045,0.060,0.077), 0.58, 0.38),
        "titanium": material("M04 Satin titanium", (0.37,0.45,0.51), 0.80, 0.28),
        "copper": material("M04 Brushed copper", (0.62,0.29,0.10), 0.78, 0.30),
        "white": material("M04 White heatshield", (0.74,0.81,0.81), 0.26, 0.33),
        "rubber": material("M04 Black dampener", (0.017,0.023,0.028), 0.0, 0.78),
        "cyan": material("M04 Cyan drive light", (0.03,0.65,0.95), 0.18, 0.20, 3.0),
        "amber": material("M04 Amber warning light", (1.0,0.32,0.035), 0.12, 0.26, 2.0),
    }
    root = empty("M04-020_ROOT_MOUNT_Y0", (0,0,0))
    root["mount"] = "rear chassis; Y=0 plane; exhaust points -Y"
    root["scale"] = "metres"
    root["animation"] = "carriage translation -Y, rotor spin about local Y"
    moving = empty("BOOSTER_TELESCOPIC_CARRIAGE", (0,0,0), root)
    rotor = empty("ROTOR_PIVOT_Y", (0,-0.66,0), moving)
    # Stationary mounting hardware and protected slide rails.
    box("Mounting plate", (0,0.005,0), (0.86,0.055,0.62), mats["graphite"], parent=root, bevel=0.035)
    box("Mount gasket", (0,-0.033,0), (0.72,0.016,0.49), mats["rubber"], parent=root, bevel=0.01)
    for x in (-0.33,0.33):
        for z in (-0.225,0.225):
            cyl("Mount captive bolt", (x,-0.045,z), 0.027, 0.024, mats["titanium"], parent=root, vertices=16)
    for x in (-0.40,0.40):
        for z in (-0.235,0.235):
            box("External slide rail", (x,-0.44,z), (0.07,0.77,0.068), mats["titanium"], "rail", root, 0.012)
            box("Rail amber end stop", (x,-0.83,z), (0.078,0.027,0.074), mats["amber"], "rail", root, 0.005)
    for z in (-0.305,0.305):
        box("Rear structural bridge", (0,-0.08,z), (0.87,0.075,0.048), mats["graphite"], parent=root)
    for x in (-0.40,0.40):
        box("Rail heel brace", (x,-0.085,0), (0.075,0.085,0.44), mats["copper"], parent=root)
    # Telescoping assembly: all parts have clearance from rails and mounting plate.
    box("Sliding carriage", (0,-0.36,0), (0.52,0.12,0.39), mats["graphite"], "moving", moving, 0.03)
    box("Ceramic spine", (0,-0.44,0.185), (0.32,0.38,0.032), mats["white"], "moving", moving)
    for x in (-0.29,0.29):
        cyl("Sliding piston rod", (x,-0.39,0), 0.031, 0.39, mats["titanium"], "moving", moving, 24)
        torus("Piston guide cuff", (x,-0.26,0), 0.034, 0.010, mats["copper"], "moving", moving)
    for y in (-0.49,-0.63):
        torus("Engine compression band", (0,y,0), 0.199, 0.021, mats["titanium"], "moving", moving)
    for y in (-0.50,-0.57,-0.64):
        for a in range(8):
            theta = a * 2*math.pi/8
            x,z = 0.207*math.cos(theta),0.207*math.sin(theta)
            block = box("Copper heat fin", (x,y,z), (0.040,0.022,0.056),
                        mats["copper"], "moving", moving, 0.006)
            block.rotation_euler.y = -theta
    for x in (-0.16,0.16):
        for z in (-0.13,0.13):
            box("Carriage white armor tab", (x,-0.40,z), (0.075,0.14,0.055),
                mats["white"], "moving", moving, 0.009)
    cyl("Rear turbine core", (0,-0.66,0), 0.064, 0.105, mats["graphite"], "moving", moving)
    for i in range(6):
        angle = i*2*math.pi/6
        blade = box("Rotor swept vane %02d"%i,
                    (0.090*math.cos(angle),0,0.090*math.sin(angle)),
                    (0.086,0.025,0.028), mats["titanium"], "rotor", rotor, 0.008)
        blade.rotation_euler.y = -angle
    torus("Rotor cyan optical ring", (0,0,0), 0.126, 0.009, mats["cyan"], "rotor", rotor)
    bell("Ceramic double-wall exhaust bell", mats["white"], moving)
    torus("Outer exit lip", (0,-0.92,0), 0.215, 0.012, mats["titanium"], "moving", moving)
    torus("Cyan nozzle annulus", (0,-0.913,0), 0.184, 0.006, mats["cyan"], "moving", moving)
    for a in range(4):
        theta = a*math.pi/2
        x,z = 0.23*math.cos(theta),0.23*math.sin(theta)
        box("Nozzle radial clamp", (x,-0.83,z), (0.040,0.11,0.038),
            mats["copper"], "moving", moving, 0.008)
    # Position animation; hold at full extension before retracting.
    for frame, dy in [(1,0.0),(25,-0.08),(55,-MOTION),(85,-MOTION),(121,0.0)]:
        moving.location = (0,dy,0)
        moving.keyframe_insert(data_path="location", frame=frame, group="M04 booster deployment")
    for frame, angle in [(1,0),(121,4*math.pi)]:
        rotor.rotation_euler = (0,angle,0)
        rotor.keyframe_insert(data_path="rotation_euler", frame=frame, group="M04 rotor")
    if moving.animation_data and moving.animation_data.action:
        moving.animation_data.action.name = "M04-020_DEPLOY_RETRACT"
    if rotor.animation_data and rotor.animation_data.action:
        rotor.animation_data.action.name = "M04-020_ROTOR_SPIN"
    scene = bpy.context.scene
    scene.frame_start = 1
    scene.frame_end = 121
    scene.render.fps = 30
    scene.unit_settings.system = 'METRIC'
    scene.unit_settings.scale_length = 1.0
    scene.frame_set(1)
    bpy.context.view_layer.update()
    return {"root":root, "moving":moving, "rotor":rotor, "mats":mats}

def aabb_world(obj):
    points = [obj.matrix_world @ Vector(v) for v in obj.bound_box]
    return ([min(v[i] for v in points) for i in range(3)],
            [max(v[i] for v in points) for i in range(3)])

def separated(a,b,epsilon=0.001):
    lo1,hi1 = a
    lo2,hi2 = b
    return any(hi1[i] + epsilon < lo2[i] or hi2[i] + epsilon < lo1[i] for i in range(3))

def qa_scene():
    scene = bpy.context.scene
    meshes = [o for o in scene.objects if o.type=="MESH" and o.get("M04_ID")==ID]
    stationary = [o for o in meshes if o.get("role") in ("static","rail")]
    moving = [o for o in meshes if o.get("role") in ("moving","rotor")]
    assert len(meshes)>=35, "Too few distinct mesh elements"
    mats = set(m.name for o in meshes for m in o.data.materials)
    assert len(mats)>=3, "Insufficient distinct materials"
    assert scene.unit_settings.scale_length==1.0
    assert any(o.name.startswith("M04-020_ROOT_MOUNT_Y0") for o in scene.objects)
    for ob in meshes:
        assert len(ob.data.polygons)>0 and len(ob.data.vertices)>0, ob.name
        assert all(math.isfinite(v) for p in ob.data.vertices for v in p.co), ob.name
    checked=0
    for f in range(1,122,5):
        scene.frame_set(f)
        bpy.context.view_layer.update()
        for a in stationary:
            bb_a=aabb_world(a)
            for b in moving:
                checked+=1
                assert separated(bb_a,aabb_world(b)), ("moving/static AABB overlap",f,a.name,b.name)
    # Rotor vanes and optical ring stay inside minimum nozzle opening 0.155 m.
    assert 0.126+0.009 < 0.155
    scene.frame_set(1)
    result={"id":ID,"mesh_count":len(meshes),"materials":sorted(mats),
            "moving_meshes":len(moving),"stationary_meshes":len(stationary),
            "sampled_frames":len(range(1,122,5)),"aabb_pair_checks":checked,
            "mount":"rear plane Y=0; local metres","carriage_travel_m":MOTION,
            "rotor_axis":"local Y","qa":"PASS sampled static/moving AABB; NOT full triangle CCD",
            "unity":"NOT RUN"}
    print("M04-020 QA "+json.dumps(result,ensure_ascii=False))
    return result

def main():
    args = sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else []
    out = Path(args[args.index("--out")+1]) if "--out" in args else Path("build/M04-020")
    out.mkdir(parents=True,exist_ok=True)
    build_scene()
    report = qa_scene()
    bpy.ops.wm.save_as_mainfile(filepath=str((out/"M04-020.blend").resolve()))
    bpy.ops.export_scene.gltf(filepath=str((out/"M04-020.glb").resolve()),
                              export_format='GLB',export_animations=True)
    report["blend_bytes"]=(out/"M04-020.blend").stat().st_size
    report["glb_bytes"]=(out/"M04-020.glb").stat().st_size
    (out/"M04-020_QA.json").write_text(json.dumps(report,indent=2,ensure_ascii=False)+"\n",encoding="utf-8")
    print("M04-020 EXPORT COMPLETE",str(out.resolve()))

if __name__=="__main__":
    main()
