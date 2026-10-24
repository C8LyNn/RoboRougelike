#!/usr/bin/env python3
"""M02-014: hybrid electric drive unit. Run: blender -b -t 2 --python generate_m02_014.py -- --out /tmp/m02-014
Blender 4.x; units metres; mount pivot (0,0,0); shaft axes +X. No game logic.
"""
import bpy
import math
import json
import sys
from pathlib import Path
from mathutils import Vector

ID = "M02-014"
CY, CZ = 0.14, 0.30
TAU = 2.0 * math.pi
N = 48

def material(name, rgb, metal=0.0, rough=0.45, emission=None):
    m = bpy.data.materials.new(name)
    m.diffuse_color = (*rgb, 1.0)
    m.use_nodes = True
    p = m.node_tree.nodes.get("Principled BSDF")
    p.inputs["Base Color"].default_value = (*rgb, 1.0)
    p.inputs["Metallic"].default_value = metal
    p.inputs["Roughness"].default_value = rough
    if emission:
        p.inputs["Emission Color"].default_value = (*rgb, 1.0)
        p.inputs["Emission Strength"].default_value = emission
    return m

def attach(obj, parent):
    if parent is not None:
        obj.parent = parent
        obj.matrix_parent_inverse = parent.matrix_world.inverted()
    return obj

def finish(obj, mat, parent=None, smooth=False):
    obj.data.materials.append(mat)
    attach(obj, parent)
    if smooth:
        for p in obj.data.polygons:
            p.use_smooth = True
    return obj

def empty(name, location, parent=None):
    obj = bpy.data.objects.new(name, None)
    bpy.context.collection.objects.link(obj)
    obj.location = location
    attach(obj, parent)
    return obj

def box(name, location, dims, mat, parent=None, bevel=0.008):
    bpy.ops.mesh.primitive_cube_add(size=1, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = dims
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel:
        mod = obj.modifiers.new("Machined edge radius", "BEVEL")
        mod.width = bevel
        mod.segments = 2
        obj.modifiers.new("Corner normals", "WEIGHTED_NORMAL")
    return finish(obj, mat, parent)

def annulus(name, x0, x1, cy, cz, inner, outer, mat, parent=None, segments=N):
    assert x1 > x0 and 0 < inner < outer
    vertices = []
    for x, radius in ((x0,outer),(x1,outer),(x0,inner),(x1,inner)):
        for i in range(segments):
            a = TAU*i/segments
            vertices.append((x, cy+radius*math.cos(a), cz+radius*math.sin(a)))
    faces = []
    for i in range(segments):
        j=(i+1)%segments
        faces += [
            (i,j,segments+j,segments+i),
            (2*segments+i,3*segments+i,3*segments+j,2*segments+j),
            (segments+i,segments+j,3*segments+j,3*segments+i),
            (i,2*segments+i,2*segments+j,j)
        ]
    mesh=bpy.data.meshes.new(name+"-mesh")
    mesh.from_pydata(vertices,[],faces)
    mesh.update()
    obj=bpy.data.objects.new(name,mesh)
    bpy.context.collection.objects.link(obj)
    return finish(obj,mat,parent,smooth=True)

def rod(name, x0, x1, cy, cz, radius, mat, parent=None):
    bpy.ops.mesh.primitive_cylinder_add(vertices=48, radius=radius, depth=x1-x0,
                                      location=((x0+x1)/2,cy,cz), rotation=(0,math.pi/2,0))
    obj=bpy.context.object
    obj.name=name
    return finish(obj,mat,parent,smooth=True)

def gear(name,x0,x1,cy,cz,teeth,root,tip,mat,parent=None,bore=0.0):
    # Stylised straight-cut teeth, narrow backlash; not an involute gear solver.
    steps=teeth*4
    radii=[root if i%4 in (0,3) else tip for i in range(steps)]
    vertices=[]
    for x in (x0,x1):
        for i in range(steps):
            a=TAU*i/steps
            r=radii[i]
            vertices.append((x,cy+r*math.cos(a),cz+r*math.sin(a)))
    if bore:
        for x in (x0,x1):
            for i in range(steps):
                a=TAU*i/steps
                vertices.append((x,cy+bore*math.cos(a),cz+bore*math.sin(a)))
    else:
        vertices += [(x0,cy,cz),(x1,cy,cz)]
    faces=[]
    for i in range(steps):
        j=(i+1)%steps
        faces.append((i,j,steps+j,steps+i))
        if bore:
            faces += [
                (2*steps+i,3*steps+i,3*steps+j,2*steps+j),
                (steps+i,steps+j,3*steps+j,3*steps+i),
                (i,2*steps+i,2*steps+j,j)]
        else:
            faces += [(2*steps,j,i),(2*steps+1,steps+i,steps+j)]
    mesh=bpy.data.meshes.new(name+"-mesh")
    mesh.from_pydata(vertices,[],faces)
    mesh.update()
    obj=bpy.data.objects.new(name,mesh)
    bpy.context.collection.objects.link(obj)
    return finish(obj,mat,parent)

def rebuild():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for mat in list(bpy.data.materials):
        if mat.users == 0:
            bpy.data.materials.remove(mat)
    scene=bpy.context.scene
    scene.render.fps=30
    scene.frame_start=1
    scene.frame_end=121
    scene.unit_settings.system="METRIC"
    scene.unit_settings.scale_length=1.0
    graphite=material("M02_014_graphite_powdercoat",(0.052,0.067,0.081),0.65)
    titanium=material("M02_014_satin_titanium",(0.41,0.47,0.51),0.85,0.28)
    copper=material("M02_014_brushed_copper",(0.63,0.27,0.095),0.75,0.3)
    rubber=material("M02_014_rubber_insulation",(0.022,0.026,0.030),0.0,0.9)
    amber=material("M02_014_amber_status",(0.96,0.38,0.035),0.15,0.3,1.4)
    blue=material("M02_014_cyan_coolant",(0.045,0.43,0.68),0.32,0.2)
    ceramic=material("M02_014_ceramic",(0.72,0.75,0.69),0.15,0.42)
    root=empty("M02-014_MOUNT_FRAME__ORIGIN", (0,0,0))
    rotor=empty("ROTOR_SHROUD_SPIN_X", (0,CY,CZ),root)
    carrier=empty("PLANETARY_CARRIER_OUTPUT_X_4_TO_1",(0,CY,CZ),root)

    # Chassis and four unobstructed bolted mounting feet; bottom mounting plane z=0.
    box("chassis_bridge",(0.42,0,0.072),(0.87,0.69,0.048),graphite,root)
    for x in (0.07,0.76):
        for y in (-0.28,0.28):
            box("mount_foot", (x,y,0.025),(0.12,0.085,0.05),titanium,root,0.004)
            rod("mount_bolt_head",x-0.016,x+0.016,y,0.06,0.012,copper,root)

    # Battery on -Y side, with independent insulated modules and cell separators.
    box("battery_vibration_cradle",(0.295,-0.284,0.15),(0.48,0.245,0.05),rubber,root)
    box("battery_enclosure",(0.295,-0.284,0.27),(0.46,0.23,0.23),graphite,root)
    for i in range(4):
        box("battery_cell_service_rib",(0.11+i*0.12,-0.410,0.275),
            (0.016,0.012,0.19),titanium,root,0.003)
    box("battery_terminal_positive",(0.18,-0.409,0.375),(0.052,0.012,0.018),copper,root,0.002)
    box("battery_terminal_negative",(0.40,-0.409,0.375),(0.052,0.012,0.018),ceramic,root,0.002)
    for i in range(3):
        box("battery_charge_LED",(0.235+i*0.043,-0.411,0.338),
            (0.022,0.009,0.009),amber,root,0.002)

    # Motor stator cage: fixed annular endplates and 8 longitudinal rails.
    annulus("stator_rear_plate",0.135,0.153,CY,CZ,0.126,0.173,titanium,root)
    annulus("stator_front_plate",0.445,0.463,CY,CZ,0.126,0.173,titanium,root)
    for i in range(8):
        a=TAU*i/8
        yy=CY+0.157*math.cos(a)
        zz=CZ+0.157*math.sin(a)
        rod("stator_guard_rail",0.153,0.445,yy,zz,0.009,graphite,root)
    for i in range(12):
        a=TAU*i/12
        yy=CY+0.130*math.cos(a)
        zz=CZ+0.130*math.sin(a)
        rod("copper_winding_end",0.172,0.197,yy,zz,0.008,copper,root)

    # Rotor bell is inside the fixed cage, never rotates the external safety bars.
    annulus("ventilated_rotor_shroud",0.215,0.421,CY,CZ,0.099,0.115,blue,rotor)
    annulus("rotor_spider_rear",0.214,0.226,CY,CZ,0.025,0.115,titanium,rotor)
    annulus("rotor_spider_front",0.409,0.421,CY,CZ,0.025,0.115,titanium,rotor)
    rod("input_shaft",0.140,0.645,CY,CZ,0.023,copper,rotor)
    annulus("input_sun_keyed_coupler",0.598,0.642,CY,CZ,0.022,0.024,copper,rotor)
    for i in range(12):
        a=TAU*i/12
        yy=CY+0.118*math.cos(a)
        zz=CZ+0.118*math.sin(a)
        rod("rotor_bell_heat_fin",0.246,0.388,yy,zz,0.0035,titanium,rotor)

    # 4:1 planetary reduction: sun=16 teeth, ring=48, planets=16 (three).
    # Teeth are stylised and separated by >=0.5mm in radial envelopes.
    annulus("stationary_gearbox_ring",0.552,0.755,CY,CZ,0.147,0.178,graphite,root)
    annulus("gearbox_rear_face",0.526,0.549,CY,CZ,0.044,0.181,titanium,root)
    annulus("gearbox_front_face",0.761,0.781,CY,CZ,0.047,0.181,titanium,root)
    gear("sun_gear_16T",0.600,0.646,CY,CZ,16,0.047,0.053,copper,rotor,bore=0.024)
    gear("planet_carrier_plate",0.685,0.704,CY,CZ,16,0.127,0.133,titanium,carrier,bore=0.025)
    annulus("carrier_hub",0.704,0.751,CY,CZ,0.025,0.040,copper,carrier)
    rod("output_drive_shaft",0.715,0.937,CY,CZ,0.024,titanium,carrier)
    annulus("output_carrier_keyed_coupler",0.715,0.749,CY,CZ,0.023,0.025,copper,carrier)
    annulus("output_spline_flange",0.896,0.922,CY,CZ,0.024,0.060,copper,carrier)
    for k in range(3):
        a=TAU*k/3
        py=CY+0.100*math.cos(a)
        pz=CZ+0.100*math.sin(a)
        spin=empty("PLANET_%02d_LOCAL_SPIN"%k,(0,py,pz),carrier)
        gear("planet_%02d_16T"%k,0.600,0.646,py,pz,16,0.040,0.0465,ceramic,spin,bore=0.011)
        rod("planet_%02d_carrier_pin"%k,0.646,0.685,py,pz,0.010,titanium,carrier)
        spin.rotation_mode="XYZ"
        spin["planet_index"]=k

    # Coolant and high-voltage service conduits, routed outside rotating envelope.
    box("inverter_module",(0.49,-0.24,0.437),(0.20,0.14,0.045),blue,root)
    for i in range(4):
        box("inverter_cooling_fin",(0.43+i*0.038,-0.24,0.471),
            (0.012,0.13,0.028),titanium,root,0.002)
    box("HV_cable_outer_insulation",(0.52,-0.115,0.426),(0.20,0.035,0.029),rubber,root)
    box("HV_cable_safety_stripe",(0.52,-0.137,0.426),(0.17,0.006,0.009),amber,root,0.001)

    # Deliberately parented visual motion, with physical gear ratio.
    for frame,turn in ((1,0.0),(121,TAU)):
        scene.frame_set(frame)
        rotor.rotation_euler=(turn,0,0)
        rotor.keyframe_insert(data_path="rotation_euler",frame=frame)
        carrier.rotation_euler=(turn/4,0,0)
        carrier.keyframe_insert(data_path="rotation_euler",frame=frame)
        for obj in bpy.data.objects:
            if "planet_index" in obj:
                obj.rotation_euler=(-0.75*turn,0,0)
                obj.keyframe_insert(data_path="rotation_euler",frame=frame)
    if scene.animation_data:
        pass
    for obj in bpy.data.objects:
        if obj.animation_data and obj.animation_data.action:
            for fc in obj.animation_data.action.fcurves:
                for key in fc.keyframe_points:
                    key.interpolation="LINEAR"
    scene.frame_set(1)
    return {"id":ID,"objects":len(bpy.data.objects),
            "materials":7,"frame_count":121,
            "axis":"+X","pivot":[0,0,0],
            "sun_planet_radial_gap_m":0.100-0.053-0.0465,
            "planet_ring_radial_gap_m":0.147-0.100-0.0465,
            "rotor_stator_rail_gap_m":0.157-0.009-0.118-0.0035,
            "reduction_ratio":4}

def main():
    args=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else []
    out=Path(args[args.index("--out")+1] if "--out" in args else "build/M02-014")
    out.mkdir(parents=True,exist_ok=True)
    report=rebuild()
    assert report["sun_planet_radial_gap_m"]>0
    assert report["planet_ring_radial_gap_m"]>0
    assert report["rotor_stator_rail_gap_m"]>0
    # Idempotence in the same Blender process.
    n1=report["objects"]
    report=rebuild()
    assert report["objects"]==n1
    bpy.ops.wm.save_as_mainfile(filepath=str((out/"M02-014.blend").resolve()))
    bpy.ops.export_scene.gltf(filepath=str((out/"M02-014_animated.glb").resolve()),
        export_format="GLB",export_animations=True,export_apply=False)
    if not (out/"M02-014_animated.glb").is_file():
        raise RuntimeError("GLB export missing: do not report Blender PASS")
    (out/"M02-014_qa.json").write_text(json.dumps(report,indent=2)+"\n",encoding="utf-8")
    print("M02-014 EXPORT PASS",json.dumps(report))

if __name__=="__main__":
    main()
