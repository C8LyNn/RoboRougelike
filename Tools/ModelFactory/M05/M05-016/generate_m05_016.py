"""M05-016 Tow Cable System — Blender 4.x/5.x, visual-only.
Unity scale: 1 Blender unit = 1 m. Rear mounting plane: Y=0.
Tow direction: -Y. Reel rotates about local/world X.
Run: blender -b --python generate_m05_016.py -- --out /tmp/M05-016
"""
import bpy
import math
import json
import os
import sys
from mathutils import Vector

ID = "M05-016"
COLLECTION = "M05-016_Tow_Cable_System"
PREFIX = "M05_016_"
CENTER = Vector((0.0, -0.43, 0.32))
PARTS = []
MOUNTS = []
MAT = {}
COL = None
ROOT = None
ROTOR = None


def clean():
    old = bpy.data.collections.get(COLLECTION)
    if old:
        meshes = [o.data for o in old.objects if o.type == "MESH"]
        for obj in list(old.objects):
            bpy.data.objects.remove(obj, do_unlink=True)
        bpy.data.collections.remove(old)
        for mesh in meshes:
            if mesh.users == 0:
                bpy.data.meshes.remove(mesh)


def material(name, rgba, metallic=0.0, roughness=0.5, emission=0.0):
    key = PREFIX + "MAT_" + name
    m = bpy.data.materials.get(key) or bpy.data.materials.new(key)
    m.diffuse_color = (*rgba, 1.0)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (*rgba, 1.0)
    bsdf.inputs["Metallic"].default_value = metallic
    bsdf.inputs["Roughness"].default_value = roughness
    if "Emission Color" in bsdf.inputs:
        bsdf.inputs["Emission Color"].default_value = (*rgba, 1.0)
        bsdf.inputs["Emission Strength"].default_value = emission
    else:
        bsdf.inputs["Emission"].default_value = (*rgba, 1.0, 1.0) if emission else (0,0,0,1)
    MAT[name] = m
    return m


def relink(obj):
    for collection in list(obj.users_collection):
        collection.objects.unlink(obj)
    COL.objects.link(obj)
    return obj


def attach(obj, parent):
    world = obj.matrix_world.copy()
    obj.parent = parent
    obj.matrix_world = world


def finish(obj, name, mat, parent=None, bevel=0.0):
    obj.name = PREFIX + name
    relink(obj)
    if mat:
        obj.data.materials.append(MAT[mat])
    if obj.type == "MESH":
        PARTS.append(obj)
        for poly in obj.data.polygons:
            poly.use_smooth = obj.type == "MESH" and len(poly.vertices) > 4
    if bevel:
        mod = obj.modifiers.new("Edge_softening", "BEVEL")
        mod.width = bevel
        mod.segments = 2
        if hasattr(mod, "affect"):\n            mod.affect = "EDGES"
        obj.modifiers.new("Weighted_normals", "WEIGHTED_NORMAL")
    attach(obj, parent or ROOT)
    return obj


def box(name, center, dims, mat, parent=None, bevel=0.0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=center)
    o = bpy.context.object
    o.dimensions = dims
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return finish(o, name, mat, parent, bevel)


def cyl(name, center, radius, depth, mat, axis="Z", parent=None, vertices=32):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=center)
    o = bpy.context.object
    if axis == "X":
        o.rotation_euler[1] = math.pi / 2
    elif axis == "Y":
        o.rotation_euler[0] = math.pi / 2
    return finish(o, name, mat, parent)


def tube(name, points, radius, mat, parent=None, sides=10):
    """Closed manifold tube, explicit mesh; no curve-to-mesh export ambiguity."""
    points = [Vector(p) for p in points]
    assert len(points) >= 2
    verts = []
    for i, p in enumerate(points):
        tangent = (points[min(i+1,len(points)-1)] -
                   points[max(i-1,0)]).normalized()
        reference = Vector((0,0,1)) if abs(tangent.z) < 0.92 else Vector((0,1,0))
        u = tangent.cross(reference).normalized()
        v = tangent.cross(u).normalized()
        for j in range(sides):
            a = 2*math.pi*j/sides
            q = p + radius*(math.cos(a)*u + math.sin(a)*v)
            verts.append(tuple(q))
    faces = [tuple(reversed(range(sides)))]
    for i in range(len(points)-1):
        for j in range(sides):
            k = (j+1) % sides
            faces.append((i*sides+j, (i+1)*sides+j,
                          (i+1)*sides+k, i*sides+k))
    faces.append(tuple((len(points)-1)*sides+j for j in range(sides)))
    mesh = bpy.data.meshes.new(PREFIX + name + "_Mesh")
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    o = bpy.data.objects.new(PREFIX + name, mesh)
    COL.objects.link(o)
    return finish(o, name, mat, parent)


def empty(name, pos, parent=None):
    o = bpy.data.objects.new(PREFIX + name, None)
    COL.objects.link(o)
    o.location = pos
    o.empty_display_type = "ARROWS"
    o.empty_display_size = 0.065
    if parent:
        attach(o, parent)
    MOUNTS.append(o)
    return o


def build():
    global COL, ROOT, ROTOR
    clean()
    PARTS.clear()
    MOUNTS.clear()
    MAT.clear()
    COL = bpy.data.collections.new(COLLECTION)
    bpy.context.scene.collection.children.link(COL)
    material("graphite", (.075,.09,.115), .72, .34)
    material("titanium", (.44,.49,.54), .82, .28)
    material("ceramic", (.79,.81,.75), .12, .42)
    material("copper", (.57,.25,.09), .75, .31)
    material("cable_black", (.025,.034,.042), .08, .75)
    material("hazard_yellow", (.95,.56,.045), .1, .43)
    material("status_cyan", (.015,.73,.84), .1, .28, 2.2)
    material("rubber", (.055,.055,.065), .0, .92)
    ROOT = empty("ROOT_MOUNT_REAR_Y0", (0,0,0))
    ROTOR = empty("PIVOT_REEL_AXIS_X", CENTER, ROOT)
    # Four bolt pads and a rigid rear-facing mounting plate.
    box("Rear_mounting_plate", (0,-.052,.32), (.87,.10,.64), "graphite", bevel=.014)
    for x in (-.31,.31):
        for z in (.10,.54):
            cyl("Mount_bolt_%s_%s" % (x,z), (x,-.111,z), .032, .02,
                "titanium", "Y", vertices=16)
            empty("MOUNT_BOLT_%s_%s" % (x,z), (x,0,z), ROOT)
    # Stationary rails stay outside the rotating drum's X envelope.
    for s in (-1,1):
        x = s*.405
        box("Side_rail_%s" % s, (x,-.435,.32), (.075,.68,.15),
            "titanium", bevel=.015)
        box("Side_ceramic_guard_%s" % s, (s*.452,-.44,.32),
            (.024,.48,.105), "ceramic", bevel=.007)
        cyl("Bearing_housing_%s" % s, (s*.400,-.43,.32), .102,
            .086, "graphite", "X")
        cyl("Bearing_trim_%s" % s, (s*.455,-.43,.32), .071,
            .016, "copper", "X")
    box("Upper_crossbar", (0,-.43,.675), (.84,.085,.055),
        "graphite", bevel=.008)
    box("Lower_crossbar", (0,-.43,-.040), (.84,.085,.055),
        "graphite", bevel=.008)
    # Rotor: drum core, rims, spokes, copper ratchet and wound line.
    cyl("Drum_core", CENTER, .145, .565, "graphite", "X", ROTOR, 48)
    cyl("Axle", CENTER, .050, .90, "titanium", "X", ROTOR, 24)
    for s in (-1,1):
        x = s*.305
        cyl("Reel_flange_%s" % s, (x,-.43,.32), .240,
            .027, "titanium", "X", ROTOR, 48)
        cyl("Flange_hub_%s" % s, (s*.325,-.43,.32), .088,
            .025, "copper", "X", ROTOR, 32)
        for i in range(8):
            a = 2*math.pi*i/8
            y = -.43 + .19*math.cos(a)
            z = .32 + .19*math.sin(a)
            cyl("Flange_rivet_%s_%s" % (s,i), (s*.322,y,z),
                .014,.012,"hazard_yellow","X",ROTOR,12)
    turns = 7
    helix = []
    for i in range(421):
        t = i/420
        a = 2*math.pi*turns*t
        helix.append((-.265 + .53*t,
                      -.43 + .170*math.cos(a),
                      .32 + .170*math.sin(a)))
    tube("Wound_steel_cable", helix, .011, "cable_black", ROTOR, 8)
    # Visible rotating ratchet is outboard of the right rail.
    cyl("Ratchet_disc", (.505,-.43,.32), .105, .026,
        "copper", "X", ROTOR, 32)
    for i in range(12):
        a = 2*math.pi*i/12
        cyl("Ratchet_pin_%02d" % i,
            (.524,-.43+.083*math.cos(a),.32+.083*math.sin(a)),
            .011,.015,"hazard_yellow","X",ROTOR,10)
    # Fixed fairlead: two rollers above/below rope, clearance to rope.
    for s in (-1,1):
        z = .32+s*.115
        cyl("Fairlead_horizontal_%s" % s, (0,-.805,z),
            .041,.40,"titanium","X",vertices=32)
    for s in (-1,1):
        x = s*.165
        cyl("Fairlead_vertical_%s" % s, (x,-.805,.32),
            .039,.29,"titanium","Z",vertices=24)
    for s in (-1,1):
        box("Fairlead_bracket_%s" % s, (s*.236,-.805,.32),
            (.035,.10,.33),"graphite",bevel=.006)
    # Static free cable passes the fairlead. Reel animation is a showcase
    # rotation only: rope payout physics and synchronization are not modeled.
    tail = [(0,-.68,.32),(0,-.72,.32),(0,-.77,.32),
            (0,-.85,.32),(0,-.95,.32),(0,-1.055,.32)]
    tube("Free_cable_through_fairlead", tail, .013, "cable_black")
    cyl("Hook_cable_crimp", (0,-1.060,.32), .030,.065,
        "copper","Y",vertices=20)
    # Eye ring lies in YZ plane, with hook beneath.
    eye = []
    for i in range(49):
        a = 2*math.pi*i/48
        eye.append((0,-1.125+.052*math.cos(a),.32+.052*math.sin(a)))
    tube("Hook_eye", eye, .016, "titanium", sides=10)
    hook = []
    for i in range(31):
        t=i/30
        a=math.pi*.05 + math.pi*1.53*t
        hook.append((0,-1.17+.11*math.cos(a),.185+.11*math.sin(a)))
    tube("Forged_open_J_hook", hook, .022, "titanium", sides=12)
    cyl("Hook_latch_pivot", (0,-1.12,.245), .025,.048,
        "copper","X",vertices=16)
    box("Hazard_plate", (0,-.125,.60), (.30,.024,.045),
        "hazard_yellow",bevel=.004)
    for s in (-1,1):
        box("Status_LED_%s" % s, (s*.225,-.122,.60),
            (.060,.014,.028),"status_cyan",bevel=.003)
    empty("MOUNT_MAIN", (0,0,.32), ROOT)
    empty("CABLE_EXIT_NEGATIVE_Y", (0,-.81,.32), ROOT)
    empty("HOOK_REST", (0,-1.17,.19), ROOT)
    bpy.context.scene.frame_start = 1
    bpy.context.scene.frame_end = 61
    for frame, angle in ((1,0),(31,math.pi),(61,2*math.pi)):
        ROTOR.rotation_euler.x = angle
        ROTOR.keyframe_insert(data_path="rotation_euler", frame=frame, index=0)
    for fcurve in ROTOR.animation_data.action.fcurves:
        for kp in fcurve.keyframe_points:
            kp.interpolation = "LINEAR"
    bpy.context.scene.frame_set(1)
    return len(PARTS), len(MAT)


def validate():
    # Conservative envelope QA, not a full mesh-mesh intersection solver.
    assert len(MAT) >= 3
    assert len(PARTS) >= 40
    assert all(abs(v) < 0.000001 for v in ROOT.location)
    assert (ROTOR.location - CENTER).length < 1e-6
    radial_clearance_top = .675-.055/2 - (.32+.240)
    radial_clearance_bottom = (.32-.240) - (-.040+.055/2)
    lateral_clearance = (.405-.075/2) - (.305+.027/2)
    rear_clearance = (-.43-.240) - (-.805+.041)
    assert min(radial_clearance_top, radial_clearance_bottom,
               lateral_clearance) > .02
    assert abs(rear_clearance) > .05
    # Mesh edge manifoldness for primitives and generated cables.
    import bmesh
    failed = []
    for o in PARTS:
        bm=bmesh.new()
        bm.from_mesh(o.data)
        if not bm.edges or any(not e.is_manifold for e in bm.edges):
            failed.append(o.name)
        bm.free()
    assert not failed, "Non-manifold: " + repr(failed)
    for frame in range(1,62):
        bpy.context.scene.frame_set(frame)
        assert math.isfinite(ROTOR.matrix_world.determinant())
    bpy.context.scene.frame_set(1)
    return {"id":ID,"mesh_count":len(PARTS),"materials":len(MAT),
            "mounts":len(MOUNTS),"sampled_frames":61,
            "envelope_min_clearance_m":round(min(radial_clearance_top,
                radial_clearance_bottom,lateral_clearance),5),
            "mesh_manifold":"PASS",
            "collision_note":"Envelope clearance only; full mesh collision NOT RUN"}


def main():
    args=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else []
    out="/tmp/M05-016"
    if "--out" in args:
        out=args[args.index("--out")+1]
    os.makedirs(out,exist_ok=True)
    a=build()
    qa=validate()
    b=build()
    qa2=validate()
    assert a==b and qa["mesh_count"]==qa2["mesh_count"]
    qa["repeat_build"]="PASS"
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(out,"M05-016.blend"))
    try:
        bpy.ops.export_scene.gltf(filepath=os.path.join(out,"M05-016.glb"),
                                  export_format="GLB",export_animations=True,
                                  export_extras=True)
        qa["glb_export"]="PASS"
    except Exception as exc:
        qa["glb_export"]="FAILED: "+repr(exc)
        raise
    try:
        bpy.ops.export_scene.fbx(filepath=os.path.join(out,"M05-016.fbx"),
                                 use_selection=False,add_leaf_bones=False)
        qa["fbx_export"]="PASS"
    except Exception as exc:
        qa["fbx_export"]="NOT RUN/UNAVAILABLE: "+repr(exc)
    qa["blender_version"]=bpy.app.version_string
    with open(os.path.join(out,"qa.json"),"w",encoding="utf8") as f:
        json.dump(qa,f,ensure_ascii=False,indent=2)
    print("M05-016 QA:",json.dumps(qa))


if __name__=="__main__":
    main()
