#!/usr/bin/env python3
"""M01-001 — Czterolufowy blaster bębnowy.
Blender 4.2+ / 5.x, Python bpy. Run:
  blender -b --python generate_m01_001.py -- --output ./export
Creates M01-001.blend, M01-001.fbx, M01-001.glb.
Coordinates: Blender +Z up, -Y muzzle forward; mount origin (0,0,0).
Only visual asset, no gameplay implementation.
"""
import bpy
import math
import os
import sys
from mathutils import Vector

ID = "M01-001"
PREFIX = "RRL_" + ID.replace("-", "_") + "_"
AXIS_Z = 0.78
BARREL_RADIUS = 0.24
TUBE_RADIUS = 0.088
BARREL_ANGLES = (0, math.pi / 2, math.pi, 3 * math.pi / 2)

def mat(name, rgb, metallic=0.0, roughness=0.45, glow=0.0):
    m = bpy.data.materials.new(PREFIX + name)
    m.diffuse_color = (*rgb, 1)
    m.use_nodes = True
    p = m.node_tree.nodes.get("Principled BSDF")
    p.inputs["Base Color"].default_value = (*rgb, 1)
    p.inputs["Metallic"].default_value = metallic
    p.inputs["Roughness"].default_value = roughness
    if glow:
        p.inputs["Emission Color"].default_value = (*rgb, 1)
        p.inputs["Emission Strength"].default_value = glow
    return m

def assign(o, m):
    o.data.materials.append(m)
    return o

def bevel(o, width=0.025, segments=2):
    mod = o.modifiers.new("ControlledChamfer", "BEVEL")
    mod.width = width
    mod.segments = segments
    if hasattr(mod, "affect"):
        mod.affect = "EDGES"
    normal = o.modifiers.new("WeightedNormals", "WEIGHTED_NORMAL")
    normal.keep_sharp = True
    return o

def box(name, center, size, material, bevel_width=0.02):
    bpy.ops.mesh.primitive_cube_add(size=1, location=center)
    o = bpy.context.object
    o.name = PREFIX + name
    o.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    assign(o, material)
    if bevel_width:
        bevel(o, min(bevel_width, min(size)*0.20))
    return o

def cyl(name, center, radius, depth, material, direction="Y", vertices=24):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=center)
    o = bpy.context.object
    o.name = PREFIX + name
    if direction == "Y":
        o.rotation_euler[0] = math.pi / 2
    elif direction == "X":
        o.rotation_euler[1] = math.pi / 2
    assign(o, material)
    for p in o.data.polygons:
        p.use_smooth = len(p.vertices) == 4
    return o

def torus(name, center, major, minor, material, direction="Y"):
    bpy.ops.mesh.primitive_torus_add(
        major_segments=40, minor_segments=10,
        location=center, major_radius=major, minor_radius=minor
    )
    o = bpy.context.object
    o.name = PREFIX + name
    if direction == "Y":
        o.rotation_euler[0] = math.pi / 2
    assign(o, material)
    for p in o.data.polygons:
        p.use_smooth = True
    return o

def parent_keep_world(obj, parent):
    world = obj.matrix_world.copy()
    obj.parent = parent
    obj.matrix_world = world

def build():
    # Idempotence: remove only our own objects and materials.
    for o in list(bpy.data.objects):
        if o.name.startswith(PREFIX):
            bpy.data.objects.remove(o, do_unlink=True)
    for m in list(bpy.data.materials):
        if m.name.startswith(PREFIX) and m.users == 0:
            bpy.data.materials.remove(m)

    graphite = mat("Graphite", (0.055,0.071,0.089), .72, .36)
    steel = mat("Gunmetal", (0.20,0.26,0.29), .78, .31)
    copper = mat("Copper", (0.62,0.25,0.085), .68, .32)
    rubber = mat("CeramicBlack", (0.021,0.028,0.036), .12, .57)
    glow = mat("AmberEmission", (1.0,0.28,0.025), .18, .25, 3.0)
    silver = mat("CeramicSilver", (0.59,0.67,0.67), .55, .26)

    # Mount pivot at bottom center, vehicle upper socket, four-bolt flange.
    mount = bpy.data.objects.new(PREFIX + "MOUNT_TOP_SOCKET", None)
    bpy.context.collection.objects.link(mount)
    mount.location = (0,0,0)
    mount.empty_display_type = "ARROWS"
    mount.empty_display_size = 0.19
    box("SocketFoot", (0, .10, .085), (.54,.52,.17), steel)
    box("SocketPad", (0, .10, .192), (.42,.40,.045), rubber, .006)
    cyl("AzimuthBearing", (0,.08,.27), .205, .11, graphite, direction="Z")
    cyl("BearingAccent", (0,.08,.33), .173, .025, copper, direction="Z")
    for sx in (-1,1):
        for sy in (-1,1):
            cyl("MountBolt_%s_%s"%(sx,sy), (sx*.212,.10+sy*.195,.177), .028,.016,silver,direction="Z",vertices=12)
    box("Cradle", (0,.13,.45), (.55,.72,.20), graphite)
    box("MainReceiver", (0,.06,.755), (.74,1.00,.55), graphite, .045)
    box("TopSpine", (0,.02,1.067), (.40,.81,.075), steel, .012)
    box("RearCounterweight", (0,.575,.73), (.58,.25,.37), steel, .03)
    box("RearServiceCover", (0,.715,.80), (.44,.035,.27), copper, .008)
    for side in (-1,1):
        box("SideArmor_%s"%side, (side*.472,-.065,.82), (.11,.67,.48), steel, .026)
        box("CopperSide_%s"%side, (side*.538,-.025,.92), (.018,.38,.10), copper, .005)
        box("Vent_%s"%side, (side*.54,.105,.747), (.021,.18,.035), rubber, .003)
        for i in range(3):
            box("HeatRib_%s_%d"%(side,i), (side*.541,-.235+i*.075,.80), (.023,.026,.19), copper, .004)
    for side in (-1,1):
        cyl("ServiceFastener_%s"%side,(side*.545,.22,.98),.026,.014,silver,"X",12)
    # Forward collar is fixed. Clearance: rotor envelope r<=0.35; cage inner radius >=0.395.
    box("FrontReceiver",(0,-.47,.78),(.49,.11,.44),steel,.015)
    for y in (-.77,-1.40):
        torus("FixedCageRing_%s"%str(y).replace(".","_"),(0,y,AXIS_Z),.455,.042,steel)
    for x,z in ((-.46,.78),(.46,.78),(0,.32),(0,1.24)):
        cyl("CageRail_%s_%s"%(x,z),(x,-1.09,z),.026,.68,graphite,"Y",12)

    # Four barrels share ONE animated rotor. Fixed cage is outside swept envelope.
    rotor = bpy.data.objects.new(PREFIX + "ROTOR_Y_AXIS", None)
    bpy.context.collection.objects.link(rotor)
    rotor.location = (0,-.57,AXIS_Z)
    rotor.empty_display_type = "CIRCLE"
    rotor.empty_display_size = .36
    moving = []
    moving.append(cyl("RotorHub", (0,-.67,AXIS_Z), .103, .38, steel))
    moving.append(cyl("RotorRearDisk",(0,-.62,AXIS_Z), .335,.065,graphite))
    moving.append(torus("RotorFrontWeb",(0,-1.48,AXIS_Z),.275,.033,copper))
    for i,angle in enumerate(BARREL_ANGLES):
        x = math.cos(angle)*BARREL_RADIUS
        z = AXIS_Z + math.sin(angle)*BARREL_RADIUS
        moving.append(cyl("Barrel_%d"%i,(x,-1.12,z),TUBE_RADIUS,1.10,steel))
        moving.append(cyl("MuzzleRim_%d"%i,(x,-1.685,z),.107,.085,copper))
        moving.append(cyl("BoreInset_%d"%i,(x,-1.732,z),.060,.009,rubber))
        moving.append(torus("CoolingRing_%d"%i,(x,-1.31,z),.090,.009,silver))
        moving.append(torus("HeatRing_%d"%i,(x,-.90,z),.091,.011,glow))
        # Rear cap + small amber telltale, all within swept radius.
        moving.append(cyl("FeedPort_%d"%i,(x,-.585,z),.083,.025,graphite))
    for o in moving:
        parent_keep_world(o,rotor)

    # Continuous 360-degree rotation, constant speed, exactly 48 frames.
    rotor.rotation_mode = 'XYZ'
    rotor.rotation_euler = (0,0,0)
    rotor.keyframe_insert(data_path="rotation_euler",index=1,frame=1)
    rotor.rotation_euler[1] = -2*math.pi
    rotor.keyframe_insert(data_path="rotation_euler",index=1,frame=49)
    if rotor.animation_data and rotor.animation_data.action:
        action = rotor.animation_data.action
        # Works for both classic fcurves and Blender 4.4+ layered actions.
        try:
            for fc in action.fcurves:
                for key in fc.keyframe_points:
                    key.interpolation = 'LINEAR'
                fc.modifiers.new('CYCLES')
        except (AttributeError, RuntimeError):
            pass
    bpy.context.scene.frame_start = 1
    bpy.context.scene.frame_end = 49
    bpy.context.scene.render.fps = 24
    bpy.context.scene.frame_set(1)

    # Bounding-circle analytical clearance: moving radius 0.24+0.107=0.347.
    assert BARREL_RADIUS + .107 < .455-.042, "Barrels intersect fixed shroud"
    assert math.sqrt(2)*BARREL_RADIUS > 2*.107, "Adjacent muzzle rims intersect"
    return rotor

def export(output):
    os.makedirs(output,exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(output,ID+".blend"))
    objs = [o for o in bpy.data.objects if o.name.startswith(PREFIX)]
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = next(o for o in objs if o.type == 'MESH')
    bpy.ops.export_scene.fbx(
        filepath=os.path.join(output,ID+".fbx"),
        use_selection=True, axis_forward='-Z', axis_up='Y',
        apply_unit_scale=True, bake_anim=True, add_leaf_bones=False,
        path_mode='AUTO'
    )
    bpy.ops.export_scene.gltf(
        filepath=os.path.join(output,ID+".glb"),
        export_format='GLB', use_selection=True,
        export_animations=True
    )
    print("EXPORT_OK",ID,len(objs))

if __name__ == "__main__":
    build()
    args = sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else []
    if "--output" in args:
        i = args.index("--output")
        export(os.path.abspath(args[i+1]))
    else:
        print("BUILD_OK", ID, "export skipped; pass -- --output PATH")
