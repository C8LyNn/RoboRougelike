#!/usr/bin/env python3
"""M04-022 — Mikrohover ślizgowy / glide microhover.
Blender 4.x/5.x. One Blender unit = one metre, +Z up.
Underside mount: chassis plane Z=0, root pivot at world (0,0,0).
Single disk tilts around LOCAL X through (0,0,-0.55), +/- 10 degrees.
Usage: blender -b --factory-startup --python-exit-code 1 --python generate_m04_022.py -- --out build/M04-022
Visual asset only: no physics or gameplay code.
"""
import bpy
import json
import math
import sys
from pathlib import Path

ID = "M04-022"
PIVOT_Z = -0.55
TILT_DEG = 10.0
FRAME_END = 121

def make_material(name, color, metal=0.0, rough=0.38, emission=0.0):
    m = bpy.data.materials.new(name)
    m.diffuse_color = (*color, 1)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (*color, 1)
    bsdf.inputs["Metallic"].default_value = metal
    bsdf.inputs["Roughness"].default_value = rough
    if emission:
        for key in ("Emission Color", "Emission"):
            if key in bsdf.inputs:
                bsdf.inputs[key].default_value = (*color, 1)
                break
        if "Emission Strength" in bsdf.inputs:
            bsdf.inputs["Emission Strength"].default_value = emission
    return m

def empty(name, location, parent=None):
    obj = bpy.data.objects.new(name, None)
    bpy.context.collection.objects.link(obj)
    obj.location = location
    obj.empty_display_type = "ARROWS"
    obj.empty_display_size = .10
    if parent:
        bpy.context.view_layer.update()
        obj.parent = parent
        obj.matrix_parent_inverse = parent.matrix_world.inverted()
    return obj

def finish(obj, mat, parent, role):
    obj.data.materials.append(mat)
    obj["model_id"] = ID
    obj["role"] = role
    if parent:
        bpy.context.view_layer.update()
        obj.parent = parent
        obj.matrix_parent_inverse = parent.matrix_world.inverted()
    return obj

def bevel(obj, amount=.01):
    mod = obj.modifiers.new("Rounded machined edges", "BEVEL")
    mod.width = amount
    mod.segments = 3
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.modifier_apply(modifier=mod.name)

def box(name, xyz, dims, mat, parent, role="detail", edge=.008, angle=0.0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=xyz)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = dims
    obj.rotation_euler[2] = angle
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if edge:
        bevel(obj, min(edge, min(dims)*.22))
    return finish(obj, mat, parent, role)

def cylinder(name, xyz, r, depth, mat, parent, role="detail", sides=48, axis="Z"):
    bpy.ops.mesh.primitive_cylinder_add(vertices=sides, radius=r, depth=depth, location=xyz)
    obj = bpy.context.object
    obj.name = name
    if axis == "X":
        obj.rotation_euler[1] = math.pi/2
    elif axis == "Y":
        obj.rotation_euler[0] = math.pi/2
    for face in obj.data.polygons:
        face.use_smooth = len(face.vertices) == 4
    return finish(obj, mat, parent, role)

def torus(name, xyz, r, tube, mat, parent, role="detail"):
    bpy.ops.mesh.primitive_torus_add(major_segments=64, minor_segments=12,
                                    major_radius=r, minor_radius=tube, location=xyz)
    obj = bpy.context.object
    obj.name = name
    for face in obj.data.polygons:
        face.use_smooth = True
    return finish(obj, mat, parent, role)

def beam(name, start, end, width, mat, parent, role="frame"):
    a = math.dist(start, end)
    mid = tuple((s+e)/2 for s,e in zip(start,end))
    obj = box(name, mid, (width,width,a), mat, parent, role, edge=.007)
    from mathutils import Vector
    obj.rotation_euler = (Vector(end)-Vector(start)).to_track_quat("Z","Y").to_euler()
    return obj

def build(out):
    # Deterministic, re-entrant generation: clear scene, not just visible objects.
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for datablocks in (bpy.data.meshes, bpy.data.materials, bpy.data.actions):
        for datablock in list(datablocks):
            if datablock.users == 0:
                datablocks.remove(datablock)
    scene = bpy.context.scene
    scene.render.fps = 30
    scene.frame_start = 1
    scene.frame_end = FRAME_END
    scene.unit_settings.system = "METRIC"
    scene.unit_settings.scale_length = 1.0

    graphite = make_material("01 Graphite powder-coated shell", (.065,.079,.102), .72,.31)
    titanium = make_material("02 Satin titanium skeleton", (.46,.51,.57), .84,.27)
    ceramic = make_material("03 Ivory ceramic edge guards", (.79,.84,.80), .17,.26)
    copper = make_material("04 Brushed copper hardware", (.51,.25,.105), .79,.34)
    rubber = make_material("05 Textured dark elastomer skids", (.035,.044,.049), .06,.73)
    cyan = make_material("06 Cyan ion-field emitter", (.025,.73,.91), .23,.22,2.8)
    amber = make_material("07 Amber diagnostic lights", (1.0,.31,.035), .18,.29,1.7)
    deep = make_material("08 Midnight blue inset panels", (.028,.10,.19), .63,.32)

    root = empty("M04-022 | UNDERSIDE MOUNT Z=0", (0,0,0))
    pivot = empty("M04-022 | TILT_X @ Z=-0.55", (0,0,PIVOT_Z), root)
    pivot["rotation_axis"] = "local X"
    pivot["tilt_limit_deg"] = TILT_DEG

    # Upper mounting cassette; contact with chassis only at Z=0.
    box("Mount | chamfered socket plate", (0,0,-.045), (.54,.58,.09), graphite, root, "mount", .024)
    box("Mount | titanium spine", (0,0,-.118), (.31,.45,.045), titanium, root, "mount", .009)
    for x in (-.205,.205):
        for y in (-.225,.225):
            cylinder("Mount | countersunk bolt", (x,y,-.003), .026,.012, copper, root, "fastener",24)
    for x in (-.245,.245):
        box("Support | dropped fork rail", (x,0,-.255), (.058,.37,.25), titanium, root, "frame", .012)
        box("Support | ceramic rail protector", (x,0,-.245), (.066,.29,.025), ceramic, root, "guard", .007)
    box("Support | central power bus", (0,0,-.30), (.22,.20,.09), graphite, root, "frame", .018)
    box("Support | copper power conductor", (0,0,-.365), (.12,.15,.028), copper, root, "detail", .008)

    # External protective skid geometry is static and laterally outside tilt envelope.
    for side in (-1,1):
        x = side*.555
        box("Skid | long composite runner", (x,0,-.83), (.115,1.04,.095), rubber, root, "skid", .036)
        box("Skid | ceramic wear sole", (x,0,-.884), (.106,.92,.024), ceramic, root, "skid", .008)
        box("Skid | titanium nose", (x,-.52,-.823), (.11,.12,.085), titanium, root, "skid", .028)
        box("Skid | titanium heel", (x,.52,-.823), (.11,.12,.085), titanium, root, "skid", .028)
        for y in (-.33,.33):
            beam("Skid | canted strut", (side*.43,y,-.35), (x,y,-.77), .045, titanium, root)
            cylinder("Skid | shock bushing", (x,y,-.755), .045,.085, copper, root, "joint",24,axis="X")
        box("Skid | warning bar", (x,0,-.762), (.118,.16,.014), amber, root, "indicator", .004)
    # Two side gimbal bearings; bearings intentionally seat on outer gimbal journals.
    for side in (-1,1):
        x=side*.468
        cylinder("Gimbal | stationary side bearing", (x,0,PIVOT_Z), .072,.066,
                 graphite, root, "bearing",32,axis="X")
        cylinder("Gimbal | copper race", (x+side*.038,0,PIVOT_Z), .050,.016,
                 copper, root, "bearing",32,axis="X")

    # Moving hover assembly: closed disc + protective ring, one articulated pivot.
    cylinder("Hover | composite lift pancake", (0,0,PIVOT_Z), .305,.055,
             deep,pivot,"tilting_disk",64)
    cylinder("Hover | lower induction plate", (0,0,PIVOT_Z-.042), .276,.020,
             graphite,pivot,"tilting_disk",64)
    cylinder("Hover | ceramic central cap", (0,0,PIVOT_Z+.048), .109,.042,
             ceramic,pivot,"tilting_disk",48)
    cylinder("Hover | copper flux concentrator", (0,0,PIVOT_Z+.022), .145,.018,
             copper,pivot,"tilting_disk",48)
    torus("Hover | segmented titanium outer halo", (0,0,PIVOT_Z), .362,.028,
          titanium,pivot,"tilting_disk")
    torus("Hover | continuous cyan ion emitter", (0,0,PIVOT_Z-.041), .322,.013,
          cyan,pivot,"emitter")
    torus("Hover | rubber anti-impact bead", (0,0,PIVOT_Z+.034), .354,.010,
          rubber,pivot,"guard")
    for i in range(12):
        theta=math.tau*i/12
        r=.239
        box("Hover | radial ion-field vane %02d"%i,
            (r*math.cos(theta),r*math.sin(theta),PIVOT_Z+.035),
            (.104,.024,.012),cyan,pivot,"emitter",.004,theta)
    for i in range(8):
        theta=math.tau*(i+.5)/8
        r=.365
        box("Hover | armor tile %02d"%i,
            (r*math.cos(theta),r*math.sin(theta),PIVOT_Z+.025),
            (.106,.067,.025),graphite,pivot,"guard",.009,theta)
        box("Hover | copper fastening %02d"%i,
            (r*math.cos(theta),r*math.sin(theta),PIVOT_Z+.045),
            (.026,.016,.008),copper,pivot,"fastener",.003,theta)
    for side in (-1,1):
        cylinder("Gimbal | moving journal", (side*.416,0,PIVOT_Z), .042,.060,
                 titanium,pivot,"tilting_disk",32,axis="X")
    # Analytically safe envelope: moving radius <= 0.44m; fixed skid inner edge 0.4975m.
    # Vertical moving bounds for +/-10deg are inside [-0.69,-0.41]m.
    keys=((1,0),(31,10),(61,0),(91,-10),(121,0))
    for frame,degrees in keys:
        pivot.rotation_euler=(math.radians(degrees),0,0)
        pivot.keyframe_insert(data_path="rotation_euler",frame=frame,group="Microhover Tilt")
    if pivot.animation_data and pivot.animation_data.action:
        action=pivot.animation_data.action
        if hasattr(action,"fcurves"):
            for curve in action.fcurves:
                for key in curve.keyframe_points:
                    key.interpolation="BEZIER"
                    key.handle_left_type="AUTO_CLAMPED"
                    key.handle_right_type="AUTO_CLAMPED"
    scene.frame_set(1)
    out.mkdir(parents=True,exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(out/(ID+".blend")))
    bpy.ops.export_scene.gltf(filepath=str(out/(ID+".glb")),export_format="GLB",
                              export_animations=True,export_apply=False)
    meshes=[o for o in bpy.data.objects if o.type=="MESH"]
    report={
        "id":ID,"mesh_count":len(meshes),"materials":len(bpy.data.materials),
        "triangles":sum(len(p.vertices)-2 for o in meshes for p in o.data.polygons),
        "mount_pivot_m":[0,0,0],"mount_plane":"Z=0, underside",
        "tilt_pivot_m":[0,0,PIVOT_Z],"tilt_axis":"local X",
        "tilt_range_deg":[-TILT_DEG,TILT_DEG],"frames":[k[0] for k in keys],
        "clearance_method":"conservative radial envelope, not continuous mesh collision",
        "static_skid_inner_x_m":.4975,"moving_max_x_m":.44,
        "unity_tests":"NOT RUN","manual_visual_review":"NOT RUN"
    }
    (out/"manifest.json").write_text(json.dumps(report,indent=2)+"\n",encoding="utf-8")
    print("MODEL_FACTORY_RESULT",json.dumps(report))

if __name__=="__main__":
    argv=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else []
    out=Path("build/M04-022")
    if "--out" in argv:
        out=Path(argv[argv.index("--out")+1])
    build(out.resolve())
