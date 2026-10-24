#!/usr/bin/env python3
"""RoboRougelike M01-002 — short piston howitzer VISUAL ONLY.
Blender 4.2+/5.x: blender -b --python generate_m01_002.py -- --output /path/to/export
Blender coordinates: +Z up, muzzle faces -Y, 1 unit = 1 metre.
Mount pivot = (0, 0, 0). Recoil group moves +Y by 0.12 m.
No ballistics, firing logic, physics, or gameplay scripts are included.
"""
import bpy
import json
import math
import os
import sys

ID = "M01-002"
PREFIX = "RRL_M01_002_"
MATERIALS = json.loads(r'''[["GraphiteArmor",[0.065,0.081,0.105],0.75,0.38,0],["Gunmetal",[0.24,0.28,0.31],0.8,0.32,0],["BurnishedCopper",[0.62,0.27,0.1],0.72,0.36,0],["CeramicIvory",[0.74,0.77,0.69],0.35,0.36,0],["HydraulicBlack",[0.035,0.043,0.052],0.35,0.51,0],["PistonChrome",[0.66,0.74,0.76],0.88,0.22,0],["AmberSignal",[1,0.37,0.035],0.12,0.24,3]]''')
PARTS = json.loads(r'''[{"name":"Socket_Foot","kind":"box","c":[0,0,0.095],"dim":[0.68,0.62,0.19],"mat":"Gunmetal","move":false,"n":24},{"name":"Socket_Gasket","kind":"box","c":[0,0,0.205],"dim":[0.55,0.52,0.035],"mat":"HydraulicBlack","move":false,"n":24},{"name":"Azimuth_Bearing","kind":"cylZ","c":[0,0,0.285],"dim":[0.215,0.13],"mat":"GraphiteArmor","move":false,"n":24},{"name":"Bearing_Copper_Collar","kind":"cylZ","c":[0,0,0.358],"dim":[0.195,0.028],"mat":"BurnishedCopper","move":false,"n":24},{"name":"Lower_Cradle","kind":"box","c":[0,0.05,0.455],"dim":[0.71,0.89,0.18],"mat":"GraphiteArmor","move":false,"n":24},{"name":"Cradle_Front_Accent","kind":"box","c":[0,-0.4,0.47],"dim":[0.46,0.035,0.085],"mat":"BurnishedCopper","move":false,"n":24},{"name":"Cradle_Cheek_-1","kind":"box","c":[-0.485,-0.02,0.735],"dim":[0.12,0.88,0.42],"mat":"Gunmetal","move":false,"n":24},{"name":"Cheek_Inlay_-1","kind":"box","c":[-0.553,-0.03,0.83],"dim":[0.012,0.52,0.105],"mat":"CeramicIvory","move":false,"n":24},{"name":"Cheek_Accent_-1","kind":"box","c":[-0.562,0.25,0.68],"dim":[0.011,0.18,0.055],"mat":"BurnishedCopper","move":false,"n":24},{"name":"Piston_Housing_-1","kind":"tubeY","c":[-0.335,0.455,0.73],"dim":[0.073,0.34,0.041,0.073],"mat":"HydraulicBlack","move":false,"n":32},{"name":"Piston_Housing_Collar_-1","kind":"tubeY","c":[-0.335,0.277,0.73],"dim":[0.082,0.045,0.041,0.082],"mat":"BurnishedCopper","move":false,"n":32},{"name":"Piston_Rod_-1","kind":"cylY","c":[-0.335,0.095,0.73],"dim":[0.034,0.43],"mat":"PistonChrome","move":true,"n":24},{"name":"Piston_Rod_Nut_-1","kind":"cylY","c":[-0.335,-0.135,0.73],"dim":[0.054,0.05],"mat":"Gunmetal","move":true,"n":16},{"name":"Slide_Guide_-1","kind":"box","c":[-0.304,-0.06,0.535],"dim":[0.06,0.81,0.055],"mat":"PistonChrome","move":false,"n":24},{"name":"Mount_Side_Bolt_-1","kind":"cylX","c":[-0.565,0.285,0.8],"dim":[0.031,0.026],"mat":"HydraulicBlack","move":false,"n":12},{"name":"Cradle_Cheek_1","kind":"box","c":[0.485,-0.02,0.735],"dim":[0.12,0.88,0.42],"mat":"Gunmetal","move":false,"n":24},{"name":"Cheek_Inlay_1","kind":"box","c":[0.553,-0.03,0.83],"dim":[0.012,0.52,0.105],"mat":"CeramicIvory","move":false,"n":24},{"name":"Cheek_Accent_1","kind":"box","c":[0.562,0.25,0.68],"dim":[0.011,0.18,0.055],"mat":"BurnishedCopper","move":false,"n":24},{"name":"Piston_Housing_1","kind":"tubeY","c":[0.335,0.455,0.73],"dim":[0.073,0.34,0.041,0.073],"mat":"HydraulicBlack","move":false,"n":32},{"name":"Piston_Housing_Collar_1","kind":"tubeY","c":[0.335,0.277,0.73],"dim":[0.082,0.045,0.041,0.082],"mat":"BurnishedCopper","move":false,"n":32},{"name":"Piston_Rod_1","kind":"cylY","c":[0.335,0.095,0.73],"dim":[0.034,0.43],"mat":"PistonChrome","move":true,"n":24},{"name":"Piston_Rod_Nut_1","kind":"cylY","c":[0.335,-0.135,0.73],"dim":[0.054,0.05],"mat":"Gunmetal","move":true,"n":16},{"name":"Slide_Guide_1","kind":"box","c":[0.304,-0.06,0.535],"dim":[0.06,0.81,0.055],"mat":"PistonChrome","move":false,"n":24},{"name":"Mount_Side_Bolt_1","kind":"cylX","c":[0.565,0.285,0.8],"dim":[0.031,0.026],"mat":"HydraulicBlack","move":false,"n":12},{"name":"Rear_Bulkhead","kind":"box","c":[0,0.605,0.78],"dim":[0.69,0.17,0.39],"mat":"GraphiteArmor","move":false,"n":24},{"name":"Rear_Service_Plate","kind":"box","c":[0,0.699,0.82],"dim":[0.44,0.022,0.22],"mat":"BurnishedCopper","move":false,"n":24},{"name":"Rear_Heat_Vent","kind":"box","c":[0,0.714,0.82],"dim":[0.28,0.012,0.07],"mat":"HydraulicBlack","move":false,"n":24},{"name":"Socket_Bolt_-1_-1","kind":"cylZ","c":[-0.265,-0.23,0.206],"dim":[0.027,0.024],"mat":"PistonChrome","move":false,"n":12},{"name":"Socket_Bolt_-1_1","kind":"cylZ","c":[-0.265,0.23,0.206],"dim":[0.027,0.024],"mat":"PistonChrome","move":false,"n":12},{"name":"Socket_Bolt_1_-1","kind":"cylZ","c":[0.265,-0.23,0.206],"dim":[0.027,0.024],"mat":"PistonChrome","move":false,"n":12},{"name":"Socket_Bolt_1_1","kind":"cylZ","c":[0.265,0.23,0.206],"dim":[0.027,0.024],"mat":"PistonChrome","move":false,"n":12},{"name":"Moving_Receiver","kind":"box","c":[0,-0.05,0.79],"dim":[0.52,0.74,0.36],"mat":"GraphiteArmor","move":true,"n":24},{"name":"Moving_Receiver_Top","kind":"box","c":[0,-0.065,1.01],"dim":[0.36,0.63,0.07],"mat":"Gunmetal","move":true,"n":24},{"name":"Moving_Rear_Counterweight","kind":"box","c":[0,0.27,0.78],"dim":[0.45,0.1,0.27],"mat":"Gunmetal","move":true,"n":24},{"name":"Receiver_Ceramic_Cheek_-1","kind":"box","c":[-0.271,-0.14,0.85],"dim":[0.019,0.43,0.2],"mat":"CeramicIvory","move":true,"n":24},{"name":"Receiver_Copper_Stripe_-1","kind":"box","c":[-0.284,-0.15,0.89],"dim":[0.009,0.31,0.035],"mat":"BurnishedCopper","move":true,"n":24},{"name":"Receiver_Status_-1","kind":"box","c":[-0.289,0.055,0.89],"dim":[0.009,0.105,0.029],"mat":"AmberSignal","move":true,"n":24},{"name":"Top_Rail_Bolt_-1","kind":"cylZ","c":[-0.14,-0.15,1.058],"dim":[0.021,0.02],"mat":"PistonChrome","move":true,"n":12},{"name":"Receiver_Ceramic_Cheek_1","kind":"box","c":[0.271,-0.14,0.85],"dim":[0.019,0.43,0.2],"mat":"CeramicIvory","move":true,"n":24},{"name":"Receiver_Copper_Stripe_1","kind":"box","c":[0.284,-0.15,0.89],"dim":[0.009,0.31,0.035],"mat":"BurnishedCopper","move":true,"n":24},{"name":"Receiver_Status_1","kind":"box","c":[0.289,0.055,0.89],"dim":[0.009,0.105,0.029],"mat":"AmberSignal","move":true,"n":24},{"name":"Top_Rail_Bolt_1","kind":"cylZ","c":[0.14,-0.15,1.058],"dim":[0.021,0.02],"mat":"PistonChrome","move":true,"n":12},{"name":"Barrel_Jacket","kind":"tubeY","c":[0,-0.765,0.83],"dim":[0.164,0.72,0.102,0.164],"mat":"Gunmetal","move":true,"n":40},{"name":"Barrel_Outer_Ring","kind":"tubeY","c":[0,-1.085,0.83],"dim":[0.18,0.095,0.102,0.18],"mat":"BurnishedCopper","move":true,"n":40},{"name":"Barrel_Connector","kind":"tubeY","c":[0,-1.155,0.83],"dim":[0.145,0.16,0.102,0.145],"mat":"HydraulicBlack","move":true,"n":40},{"name":"Muzzle_Brake_Cone","kind":"tubeY","c":[0,-1.295,0.83],"dim":[0.215,0.2,0.105,0.255],"mat":"Gunmetal","move":true,"n":48},{"name":"Muzzle_Brake_Lip","kind":"tubeY","c":[0,-1.4,0.83],"dim":[0.232,0.035,0.105,0.232],"mat":"BurnishedCopper","move":true,"n":48},{"name":"Brake_Heat_Shield_-1","kind":"box","c":[-0.217,-1.265,0.83],"dim":[0.021,0.095,0.065],"mat":"CeramicIvory","move":true,"n":24},{"name":"Brake_Bolt_-1","kind":"cylX","c":[-0.245,-1.315,0.83],"dim":[0.024,0.018],"mat":"HydraulicBlack","move":true,"n":12},{"name":"Brake_Heat_Shield_1","kind":"box","c":[0.217,-1.265,0.83],"dim":[0.021,0.095,0.065],"mat":"CeramicIvory","move":true,"n":24},{"name":"Brake_Bolt_1","kind":"cylX","c":[0.245,-1.315,0.83],"dim":[0.024,0.018],"mat":"HydraulicBlack","move":true,"n":12},{"name":"Barrel_Cooling_Rib_0","kind":"tubeY","c":[0,-0.59,0.83],"dim":[0.181,0.024,0.155,0.181],"mat":"BurnishedCopper","move":true,"n":32},{"name":"Barrel_Cooling_Rib_1","kind":"tubeY","c":[0,-0.475,0.83],"dim":[0.181,0.024,0.155,0.181],"mat":"BurnishedCopper","move":true,"n":32},{"name":"Barrel_Cooling_Rib_2","kind":"tubeY","c":[0,-0.36,0.83],"dim":[0.181,0.024,0.155,0.181],"mat":"BurnishedCopper","move":true,"n":32},{"name":"Barrel_Cooling_Rib_3","kind":"tubeY","c":[0,-0.24499999999999994,0.83],"dim":[0.181,0.024,0.155,0.181],"mat":"BurnishedCopper","move":true,"n":32},{"name":"Piston_Receiver_Anchor_-1","kind":"box","c":[-0.285,-0.135,0.73],"dim":[0.19,0.09,0.11],"mat":"Gunmetal","move":true,"n":24},{"name":"Piston_Receiver_Anchor_1","kind":"box","c":[0.285,-0.135,0.73],"dim":[0.19,0.09,0.11],"mat":"Gunmetal","move":true,"n":24}]''')
TRAVEL = 0.12
PISTON_ROD_RADIUS_M = 0.034
PISTON_BORE_RADIUS_M = 0.041
PISTON_MIN_ENGAGEMENT_M = 0.020
PISTON_RADIAL_CLEARANCE_M = PISTON_BORE_RADIUS_M - PISTON_ROD_RADIUS_M

def validate_recoil_geometry():
    """Analytic kinematic safety gate for piston bodies over the full stroke.

    A minimum inserted length, real hollow housing/collar and connection
    between moving receiver and each piston-rod nut are required at rest
    AND for 21 recoil poses, rather than trusting nominal mesh bounds.
    """
    by = {p["name"]: p for p in PARTS}
    assert len(by)==len(PARTS)==57
    assert PISTON_RADIAL_CLEARANCE_M >= .006
    for side in (-1, 1):
        housing = by[f"Piston_Housing_{side}"]
        collar = by[f"Piston_Housing_Collar_{side}"]
        rod = by[f"Piston_Rod_{side}"]
        anchor = by[f"Piston_Receiver_Anchor_{side}"]
        assert rod["move"] and anchor["move"] and not housing["move"] and not collar["move"]
        assert all(item["kind"]=="tubeY" for item in (housing,collar))
        for item in (housing,collar):
            assert abs(item["dim"][2]-PISTON_BORE_RADIUS_M)<1e-9
            assert item["dim"][0] > item["dim"][2]
            assert abs(item["c"][0]-rod["c"][0])<1e-9
            assert abs(item["c"][2]-rod["c"][2])<1e-9
        radius, length=rod["dim"]
        assert abs(radius-PISTON_ROD_RADIUS_M)<1e-9
        mouth_y = housing["c"][1]-housing["dim"][1]/2
        rear_y = housing["c"][1]+housing["dim"][1]/2
        for k in range(21):
            travel=TRAVEL*k/20
            rod_front=rod["c"][1]+travel+length/2
            insertion=rod_front-mouth_y
            assert insertion>=PISTON_MIN_ENGAGEMENT_M-1e-9, (side,k,insertion)
            assert rod_front<=rear_y-.025, (side,k,"rod pierces cylinder end")
            collar_low=collar["c"][1]-collar["dim"][1]/2
            collar_high=collar["c"][1]+collar["dim"][1]/2
            assert (rod["c"][1]+travel-length/2)<collar_low and rod_front>collar_high
        # A physically continuous connection to the moving receiver and nut.
        receiver = by["Moving_Receiver"]
        nut = by[f"Piston_Rod_Nut_{side}"]
        def box_bounds(p):
            return [(p["c"][i]-p["dim"][i]/2,p["c"][i]+p["dim"][i]/2)
                    for i in range(3)]
        a=box_bounds(anchor)
        b=box_bounds(receiver)
        overlap=lambda q,r:all(min(q[i][1],r[i][1])-max(q[i][0],r[i][0])>.001
                               for i in range(3))
        assert overlap(a,b), "Piston anchor does not reach moving receiver"
        assert abs(anchor["c"][0]-nut["c"][0]) <= anchor["dim"][0]/2+nut["dim"][0]
        assert abs(anchor["c"][1]-nut["c"][1]) <= anchor["dim"][1]/2+nut["dim"][1]/2
        assert abs(anchor["c"][2]-nut["c"][2]) <= anchor["dim"][2]/2+nut["dim"][0]
    return {
        "recoil_samples":21,"pistons":2,"hollow_housing_collar_shells":4,
        "moving_receiver_anchors":2,
        "rod_bore_radial_clearance_m":round(PISTON_RADIAL_CLEARANCE_M,3),
        "minimum_engagement_at_rest_m":.025,
        "piston_rod_length_m":.43,
        "recoil_travel_m":TRAVEL
    }


def make_material(entry):
    name, rgb, metallic, roughness, emission = entry
    mat = bpy.data.materials.new(PREFIX + name)
    mat.diffuse_color = (*rgb, 1.0)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (*rgb, 1.0)
    bsdf.inputs["Metallic"].default_value = metallic
    bsdf.inputs["Roughness"].default_value = roughness
    if emission:
        bsdf.inputs["Emission Color"].default_value = (*rgb, 1.0)
        bsdf.inputs["Emission Strength"].default_value = emission
    return mat

def make_tube(name, spec):
    # A hollow tapered sleeve: both ends open; no fake filled muzzle.
    outer0, depth, bore, outer1 = spec["dim"]
    n = spec["n"]
    verts = []
    for z, radius in ((-depth/2,outer0),(depth/2,outer1),
                      (-depth/2,bore),(depth/2,bore)):
        for i in range(n):
            a = 2*math.pi*i/n
            verts.append((radius*math.cos(a), z, -radius*math.sin(a)))
    faces = []
    for i in range(n):
        j=(i+1)%n
        faces.extend(((i,j,n+j,n+i),
                      (2*n+i,3*n+i,3*n+j,2*n+j),
                      (i,2*n+i,2*n+j,j),
                      (n+i,n+j,3*n+j,3*n+i)))
    mesh = bpy.data.meshes.new(PREFIX + name + "_Mesh")
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(PREFIX + name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.location = spec["c"]
    return obj

def build():
    validate_recoil_geometry()
    # Idempotent: delete only objects/materials from this model, never scene contents.
    for obj in list(bpy.data.objects):
        if obj.name.startswith(PREFIX):
            bpy.data.objects.remove(obj, do_unlink=True)
    for mat in list(bpy.data.materials):
        if mat.name.startswith(PREFIX) and mat.users == 0:
            bpy.data.materials.remove(mat)
    materials = {x[0]:make_material(x) for x in MATERIALS}
    root = bpy.data.objects.new(PREFIX+"MOUNT_TOP_SOCKET", None)
    bpy.context.collection.objects.link(root)
    root.empty_display_type = "ARROWS"
    root.empty_display_size = 0.20
    mover = bpy.data.objects.new(PREFIX+"RECOIL_Y_POSITIVE", None)
    bpy.context.collection.objects.link(mover)
    mover.empty_display_type = "CUBE"
    mover.empty_display_size = 0.25
    mover.parent = root
    for p in PARTS:
        name, kind, c, d = p["name"], p["kind"], p["c"], p["dim"]
        if kind == "box":
            bpy.ops.mesh.primitive_cube_add(size=1, location=c)
            obj=bpy.context.object
            obj.name=PREFIX+name
            obj.dimensions=d
            bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
            mod=obj.modifiers.new("SmallEdgeChamfer","BEVEL")
            mod.width=min(.009, min(d)*.12)
            mod.segments=2
            norm=obj.modifiers.new("WeightedNormals","WEIGHTED_NORMAL")
            norm.keep_sharp=True
        elif kind.startswith("cyl"):
            bpy.ops.mesh.primitive_cylinder_add(
                vertices=p["n"], radius=d[0], depth=d[1], location=c)
            obj=bpy.context.object
            obj.name=PREFIX+name
            if kind=="cylY":
                obj.rotation_euler[0]=math.pi/2
            elif kind=="cylX":
                obj.rotation_euler[1]=math.pi/2
            for poly in obj.data.polygons:
                poly.use_smooth=len(poly.vertices)==4
        elif kind=="tubeY":
            obj=make_tube(name,p)
            for poly in obj.data.polygons:
                poly.use_smooth=True
        else:
            raise ValueError("Unknown shape "+kind)
        obj.data.materials.append(materials[p["mat"]])
        parent=mover if p["move"] else root
        world=obj.matrix_world.copy()
        obj.parent=parent
        obj.matrix_world=world
        obj["model_id"]=ID
        obj["mount_socket"]="top"
    # Deterministic four-key recoil; +Y points backwards, away from muzzle.
    for frame, offset in ((1,0.0),(4,TRAVEL),(8,.055),(14,0.0),(20,0.0)):
        mover.location=(0.0,offset,0.0)
        mover.keyframe_insert(data_path="location",frame=frame,group="Recoil")
    if mover.animation_data and mover.animation_data.action:
        act=mover.animation_data.action
        # Blender 4.x and 5.x both support a conventional fcurve path when present.
        for fc in getattr(act,"fcurves",[]):
            for kp in fc.keyframe_points:
                kp.interpolation="LINEAR"
    scene=bpy.context.scene
    scene.frame_start=1
    scene.frame_end=20
    scene.render.fps=24
    mover.location=(0,0,0)
    scene.frame_set(1)
    return root,mover

def export(root, out):
    os.makedirs(out,exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(out,ID+".blend"))
    bpy.ops.object.select_all(action="DESELECT")
    for obj in bpy.data.objects:
        if obj.name.startswith(PREFIX):
            obj.select_set(True)
    bpy.context.view_layer.objects.active=root
    # glTF preserves material names, hierarchy and recoil animation.
    bpy.ops.export_scene.gltf(filepath=os.path.join(out,ID+".glb"),
        export_format="GLB", use_selection=True, export_animations=True)
    bpy.ops.export_scene.fbx(filepath=os.path.join(out,ID+".fbx"),
        use_selection=True, add_leaf_bones=False,
        axis_forward="-Z",axis_up="Y",bake_anim=True)

if __name__=="__main__":
    print("M01-002 analytic kinematics:",json.dumps(validate_recoil_geometry()))
    root,_=build()
    args=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else []
    out=os.path.join(os.path.dirname(os.path.abspath(__file__)),"export")
    if "--output" in args:
        out=args[args.index("--output")+1]
    export(root,out)
    print("M01-002: generated Blender, GLB and FBX in",out)
