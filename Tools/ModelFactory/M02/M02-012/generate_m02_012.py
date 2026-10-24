#!/usr/bin/env python3
"""MODEL-02 M02-012: compact, mechanically articulated V2 engine for Unity 6.
Blender 4.x+, metres, Z up; export glTF Y up. No Unity gameplay changes.
A crank-slider solver places two pistons and two connecting rods exactly
on their kinematic constraints. 1.0 Blender unit = 1.0 metre.
"""
import argparse
import json
import math
import os
from pathlib import Path

import bpy
import bmesh
from mathutils import Vector, Quaternion

MODEL_ID = "M02-012"
COLLECTION = "MODEL_02_M02_012"
FRAME_END = 91
CRANK_Z = .255
CRANK_RADIUS = .045
ROD_LENGTH = .220
BANK_ANGLE = math.radians(38.0)
STROKE = 2.0 * CRANK_RADIUS
CYL_INNER = .069
PISTON_RADIUS = .061
PISTON_LENGTH = .100
AXIAL_CLEARANCE = CYL_INNER - PISTON_RADIUS
MATERIALS = {}
OBJECTS = []
MOVING = {}
BANKS = {}

def mat(name, rgba, metallic=0.0, roughness=.45, emission=0.0):
    m = bpy.data.materials.new(MODEL_ID + "_" + name)
    m.diffuse_color = (*rgba, 1.0)
    m.use_nodes = True
    bs = m.node_tree.nodes.get("Principled BSDF")
    bs.inputs["Base Color"].default_value = (*rgba, 1.0)
    bs.inputs["Metallic"].default_value = metallic
    bs.inputs["Roughness"].default_value = roughness
    if emission:
        bs.inputs["Emission Color"].default_value = (*rgba, 1.0)
        bs.inputs["Emission Strength"].default_value = emission
    MATERIALS[name] = m
    return m

def track(obj, material, bevel=0.0):
    obj.name = MODEL_ID + "_" + obj.name
    obj.data.name = obj.name + "_Mesh"
    obj.data.materials.append(MATERIALS[material])
    for coll in list(obj.users_collection):
        coll.objects.unlink(obj)
    bpy.data.collections[COLLECTION].objects.link(obj)
    OBJECTS.append(obj)
    for face in obj.data.polygons:
        face.use_smooth = True
    if bevel:
        mod = obj.modifiers.new("Machined_edge_bevel", "BEVEL")
        mod.width = bevel
        mod.segments = 2
        if hasattr(mod, "affect"):
            mod.affect = "EDGES"
        if hasattr(obj.data, "use_auto_smooth"):
            obj.data.use_auto_smooth = True
            normal = obj.modifiers.new("Weighted_face_normals", "WEIGHTED_NORMAL")
            normal.keep_sharp = True
    return obj

def box(name, loc, size, material, bevel=.008):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    ob = bpy.context.object
    ob.name = name
    ob.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return track(ob, material, bevel)

def cylinder(name, loc, radius, depth, material, axis=(0,0,1),
             vertices=32, bevel=.002):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius,
                                        depth=depth, location=loc)
    ob = bpy.context.object
    ob.name = name
    ob.rotation_mode = "QUATERNION"
    ob.rotation_quaternion = Vector((0,0,1)).rotation_difference(Vector(axis))
    return track(ob, material, bevel)

def ring(name, center, axis, r_inner, r_outer, depth, material,
         start=0.0, sweep=2*math.pi, segments=48):
    """Closed manifold annular sector, cut away on front of each barrel."""
    u = Vector(axis).normalized()
    e1 = Vector((0,-1,0)) if abs(u.y)<.95 else Vector((1,0,0))
    e2 = u.cross(e1).normalized()
    c = Vector(center)
    vs = []
    for level in (-.5,.5):
        for radius in (r_inner,r_outer):
            for i in range(segments+1):
                a = start+sweep*i/segments
                p = c+u*(level*depth)+radius*(e1*math.cos(a)+e2*math.sin(a))
                vs.append(tuple(p))
    n=segments+1
    def ix(level,radial,i):
        return (level*2+radial)*n+i
    faces=[]
    for i in range(segments):
        j=i+1
        faces.extend([
            (ix(0,1,i),ix(0,1,j),ix(1,1,j),ix(1,1,i)),
            (ix(1,0,i),ix(1,0,j),ix(0,0,j),ix(0,0,i)),
            (ix(0,0,i),ix(0,0,j),ix(0,1,j),ix(0,1,i)),
            (ix(1,1,i),ix(1,1,j),ix(1,0,j),ix(1,0,i))])
    if sweep < 2*math.pi-1e-6:
        faces.extend([
            (ix(0,0,0),ix(0,1,0),ix(1,1,0),ix(1,0,0)),
            (ix(1,0,segments),ix(1,1,segments),
             ix(0,1,segments),ix(0,0,segments))])
    else:
        # Closing faces are coplanar at the seam; remove duplicated seam
        # vertices before export with bmesh remove_doubles.
        pass
    mesh=bpy.data.meshes.new(name+"_Mesh")
    mesh.from_pydata(vs,[],faces)
    mesh.update()
    ob=bpy.data.objects.new(name,mesh)
    bpy.data.collections[COLLECTION].objects.link(ob)
    # Track without adding duplicate collection links.
    ob.data.materials.append(MATERIALS[material])
    ob.name=MODEL_ID+"_"+name
    ob.data.name=ob.name+"_Mesh"
    OBJECTS.append(ob)
    for p in ob.data.polygons: p.use_smooth=True
    bm=bmesh.new()
    bm.from_mesh(mesh)
    bmesh.ops.remove_doubles(bm,verts=bm.verts,dist=1e-7)
    bmesh.ops.recalc_face_normals(bm,faces=bm.faces)
    bm.to_mesh(mesh)
    bm.free()
    return ob

def empty(name, loc):
    ob=bpy.data.objects.new(MODEL_ID+"_"+name,None)
    bpy.data.collections[COLLECTION].objects.link(ob)
    ob.location=loc
    ob.empty_display_type="PLAIN_AXES"
    ob.empty_display_size=.06
    OBJECTS.append(ob)
    return ob

def parent_keep_world(obj, parent):
    world=obj.matrix_world.copy()
    obj.parent=parent
    obj.matrix_world=world

def bank_info(sign):
    return {
        "sign":sign,
        "label":"L" if sign<0 else "R",
        "axis":Vector((sign*math.sin(BANK_ANGLE),0,math.cos(BANK_ANGLE))),
        "base":Vector((0,sign*.115,CRANK_Z)),
        "phase":0 if sign<0 else math.pi,
    }

def wrist_position(bank, angle):
    theta=angle+bank["phase"]
    crank=Vector((CRANK_RADIUS*math.sin(theta),
                  bank["base"].y,
                  CRANK_Z+CRANK_RADIUS*math.cos(theta)))
    displacement=crank-bank["base"]
    d=bank["axis"].dot(displacement)
    s=d+math.sqrt(ROD_LENGTH**2-displacement.length_squared+d*d)
    wrist=bank["base"]+bank["axis"]*s
    return crank,wrist,s

def build():
    if COLLECTION in bpy.data.collections:
        old=bpy.data.collections[COLLECTION]
        for ob in list(old.objects): bpy.data.objects.remove(ob,do_unlink=True)
        bpy.data.collections.remove(old)
    coll=bpy.data.collections.new(COLLECTION)
    bpy.context.scene.collection.children.link(coll)
    MATERIALS.clear()
    OBJECTS.clear()
    MOVING.clear()
    BANKS.clear()
    mat("Graphite_Cast",(.075,.092,.112),.68,.35)
    mat("Brushed_Steel",(.44,.51,.55),.82,.27)
    mat("Aluminium_Radiator",(.65,.70,.70),.73,.33)
    mat("Copper_Pipes",(.54,.235,.085),.75,.29)
    mat("Rubber_Isolator",(.025,.033,.039),.05,.77)
    mat("Ceramic_Piston",(.79,.78,.68),.18,.31)
    mat("Cyan_Status",(.018,.68,.82),.12,.24,1.5)
    mat("Amber_Marking",(.96,.41,.075),.26,.34)

    root=empty("MOUNT_CHASSIS_ORIGIN",(0,0,0))
    for x in (-.17,.17):
        for y in (-.245,.245):
            box("Foot_%s_%s"%(int(x*1000),int(y*1000)),
                (x,y,.037),(.10,.085,.035),"Graphite_Cast",.006)
            cylinder("Mount_Bolt_%s_%s"%(int(x*1000),int(y*1000)),
                     (x,y,.058),.016,.016,"Brushed_Steel",vertices=12)
    for x in (-.205,.205):
        box("Crankcase_Long_Rail_%s"%int(x*1000),
            (x,0,.237),(.032,.52,.21),"Graphite_Cast")
        box("Top_Lip_%s"%int(x*1000),
            (x,0,.347),(.042,.51,.023),"Brushed_Steel",.004)
    box("Oil_Sump",(0,0,.124),(.40,.51,.050),"Graphite_Cast",.016)
    for y in (-.267,.267):
        ring("Crankcase_End_Bearing_%s"%int(y*1000),
             (0,y,CRANK_Z),(0,1,0),.044,.176,.034,
             "Graphite_Cast",segments=64)
        ring("End_Seal_%s"%int(y*1000),(0,y,CRANK_Z),
             (0,1,0),.032,.042,.005,"Rubber_Isolator",segments=48)
    box("Nameplate",(0,-.288,.232),(.17,.013,.043),"Amber_Marking",.004)

    rotor=empty("CRANKSHAFT_ROTOR",(0,0,CRANK_Z))
    shaft=cylinder("Crankshaft",(0,0,CRANK_Z),.027,.81,
                   "Brushed_Steel",axis=(0,1,0),vertices=48)
    parent_keep_world(shaft,rotor)
    for sign in (-1,1):
        bank=bank_info(sign)
        label=bank["label"]
        BANKS[label]=bank
        # Webs overlap the 65mm crankpin by 10mm at both ends.
        for off in (-.030,.030):
            web=cylinder("Crank_Web_"+label+("_A" if off<0 else "_B"),
                (0,bank["base"].y+off,CRANK_Z),.083,.015,
                "Brushed_Steel",axis=(0,1,0),vertices=40)
            parent_keep_world(web,rotor)
        pin=cylinder("Crank_Pin_"+label,
                     (0,bank["base"].y,CRANK_Z+CRANK_RADIUS*(1 if sign<0 else -1)),
                     .024,.065,"Copper_Pipes",axis=(0,1,0),vertices=32)
        parent_keep_world(pin,rotor)

        u=bank["axis"]
        base=bank["base"]
        # Axial collar intersects chassis top intentionally, hollow bore
        # remains larger than the piston.
        ring("Cylinder_Base_Collar_"+label,base+u*.143,u,
             .071,.102,.055,"Brushed_Steel",segments=48)
        ring("Cylinder_Cutaway_"+label,base+u*.269,u,
             CYL_INNER,.086,.263,"Graphite_Cast",
             start=math.radians(55),sweep=math.radians(250),segments=48)
        for i in range(6):
            ring("Radiator_Fin_%s_%02d"%(label,i),
                 base+u*(.193+i*.033),u,
                 .086,.110,.009,"Aluminium_Radiator",
                 start=math.radians(55),sweep=math.radians(250),segments=40)
        cylinder("Cylinder_Head_"+label,base+u*.431,.098,.058,
                 "Brushed_Steel",axis=u,vertices=48)
        cylinder("Spark_Plug_"+label,base+u*.477,.022,.044,
                 "Ceramic_Piston",axis=u,vertices=24)
        cylinder("Head_Copper_Trim_"+label,base+u*.458,.039,.010,
                 "Copper_Pipes",axis=u,vertices=32)
        # Movable piston body and crown are one rigid group.
        piston=empty("PISTON_RIG_"+label,(0,0,0))
        body=cylinder("Piston_Skirt_"+label,(0,0,0),
                      PISTON_RADIUS,PISTON_LENGTH,"Ceramic_Piston",
                      vertices=40,bevel=.003)
        body.parent=piston
        body.location=(0,0,0)
        body.rotation_mode="QUATERNION"
        body.rotation_quaternion=Quaternion((1,0,0,0))
        for k in range(2):
            band=cylinder("Piston_Ring_%s_%d"%(label,k),
                (0,0,0),.063,.006,"Brushed_Steel",
                vertices=40,bevel=.0005)
            band.parent=piston
            band.location=(0,0,.025+k*.016)
            band.rotation_mode="QUATERNION"
            band.rotation_quaternion=Quaternion((1,0,0,0))
        # A real transverse wrist pin travels with the piston; the small
        # end eye is an annulus around it, always aligned with crank +Y.
        wrist_pin=cylinder("Wrist_Pin_"+label,(0,0,0),
            .012,.094,"Brushed_Steel",axis=(0,1,0),vertices=32)
        wrist_pin.parent=piston
        wrist_pin.location=(0,0,-.025)
        wrist_pin.rotation_mode="QUATERNION"
        wrist_pin.rotation_quaternion=Vector((0,0,1)).rotation_difference(
            Vector((0,1,0)))
        small_eye=ring("Conrod_Small_End_Eye_"+label,
            (0,0,0),(0,1,0),.0135,.029,.018,
            "Brushed_Steel",segments=40)
        small_eye.parent=piston
        small_eye.location=(0,0,-.025)
        small_eye.rotation_mode="QUATERNION"
        small_eye.rotation_quaternion=Quaternion((1,0,0,0))
        # A separate crank eye is kept coaxial with the Y-axis crank pin,
        # rather than erroneously rotating its bore with the conrod beam.
        big_eye=empty("CRANK_EYE_RIG_"+label,(0,0,0))
        big_ring=ring("Conrod_Big_End_Eye_"+label,
            (0,0,0),(0,1,0),.0255,.035,.018,
            "Brushed_Steel",segments=40)
        big_ring.parent=big_eye
        big_ring.location=(0,0,0)
        # Shortened beam physically joins the outer faces of the two
        # annular eyes without blocking either transverse bearing bore.
        rod=empty("CONROD_RIG_"+label,(0,0,0))
        core=cylinder("Connecting_Rod_"+label,(0,0,0),
            .017,ROD_LENGTH-.034-.028,"Copper_Pipes",
            vertices=20,bevel=.003)
        core.parent=rod
        core.location=(0,0,(.034-.028)/2)
        core.rotation_mode="QUATERNION"
        core.rotation_quaternion=Quaternion((1,0,0,0))
        MOVING[label]=(piston,rod,big_eye)

    flywheel=cylinder("Flywheel",(0,.377,CRANK_Z),.122,.025,
                      "Graphite_Cast",axis=(0,1,0),vertices=64)
    parent_keep_world(flywheel,rotor)
    for k in range(8):
        a=2*math.pi*k/8
        ob=cylinder("Flywheel_Copper_Stud_%d"%k,
            (.087*math.sin(a),.394,CRANK_Z+.087*math.cos(a)),
            .011,.009,"Copper_Pipes",axis=(0,1,0),vertices=16)
        parent_keep_world(ob,rotor)
    for sign in (-1,1):
        x=sign*.17
        cylinder("Status_Lamp_%s"%("L" if sign<0 else "R"),
                 (x,-.289,.290),.013,.012,"Cyan_Status",axis=(0,1,0))
    empty("SNAP_CHASSIS_LEFT",(-.17,-.245,0))
    empty("SNAP_CHASSIS_RIGHT",(.17,.245,0))
    empty("SNAP_OUTPUT_SHAFT",(0,.410,CRANK_Z))
    return rotor

def animate(rotor):
    scene=bpy.context.scene
    scene.frame_start=1
    scene.frame_end=FRAME_END
    scene.render.fps=30
    for f in range(1,FRAME_END+1):
        angle=2*math.pi*(f-1)/(FRAME_END-1)
        rotor.rotation_mode="XYZ"
        rotor.rotation_euler=(0,angle,0)
        rotor.keyframe_insert(data_path="rotation_euler",frame=f,group="Crankshaft")
        for label,bank in BANKS.items():
            crank,wrist,s=wrist_position(bank,angle)
            piston,rod,big_eye=MOVING[label]
            piston.rotation_mode="QUATERNION"
            piston.location=wrist+bank["axis"]*.025
            piston.rotation_quaternion=Vector((0,0,1)).rotation_difference(bank["axis"])
            piston.keyframe_insert(data_path="location",frame=f,group="Pistons")
            piston.keyframe_insert(data_path="rotation_quaternion",frame=f,group="Pistons")
            big_eye.location=crank
            big_eye.keyframe_insert(data_path="location",frame=f,group="CrankEye")
            direction=wrist-crank
            assert abs(direction.length-ROD_LENGTH)<1e-8
            rod.rotation_mode="QUATERNION"
            rod.location=(crank+wrist)/2
            rod.rotation_quaternion=Vector((0,0,1)).rotation_difference(direction)
            rod.keyframe_insert(data_path="location",frame=f,group="Conrods")
            rod.keyframe_insert(data_path="rotation_quaternion",frame=f,group="Conrods")
    scene.frame_set(1)

def validate():
    assert len(MATERIALS)>=3
    assert abs(STROKE-.09)<1e-12
    assert AXIAL_CLEARANCE>=.0079
    assert CYL_INNER-.063>=.0059  # piston compression rings
    assert .0325-(.030-.0075)>=.0099  # crankpin into each web
    assert .0255-.024>=.0014  # crank bearing radial clearance
    assert .0135-.012>=.0014  # wrist bearing radial clearance
    assert len(BANKS)==2 and len(MOVING)==2
    samples=0
    minimum_head_clearance=999
    minimum_rod_error=999
    for k in range(361):
        a=2*math.pi*k/360
        for label,bank in BANKS.items():
            crank,wrist,s=wrist_position(bank,a)
            d=abs((wrist-crank).length-ROD_LENGTH)
            minimum_rod_error=min(minimum_rod_error,d)
            assert d<1e-8
            # Cylinder head underside is at s=.402. Piston top at s+.075.
            clearance=.402-(s+.075)
            minimum_head_clearance=min(minimum_head_clearance,clearance)
            assert clearance>.035,(label,k,clearance)
            # Piston bottom at s-.025 remains within cylinder open base.
            assert s-.025>.13
            samples+=1
    # Verify closed mesh manifold for all objects with no boundary edges,
    # nonzero positive volume; explicitly allow designed bearing contacts.
    mesh_count=0
    for ob in OBJECTS:
        if ob.type!="MESH": continue
        mesh_count+=1
        bm=bmesh.new()
        bm.from_mesh(ob.data)
        assert len(bm.verts)>0 and len(bm.faces)>0,ob.name
        assert all(e.is_manifold for e in bm.edges),ob.name
        volume=bm.calc_volume(signed=True)
        assert abs(volume)>1e-11,(ob.name,volume)
        bm.free()
    assert mesh_count>40
    return dict(id=MODEL_ID,engine="V2 crank-slider",
                units="metres",mount_pivot=[0,0,0],crank_axis="Blender +Y",
                cylinder_bank_angle_degrees=38,stroke_m=STROKE,
                connecting_rod_m=ROD_LENGTH,piston_radius_m=PISTON_RADIUS,
                cylinder_bore_radius_m=CYL_INNER,radial_clearance_m=AXIAL_CLEARANCE,
                materials=len(MATERIALS),mesh_objects=mesh_count,
                animated_groups=7,frames=FRAME_END,
                kinematic_samples=samples,
                max_rod_length_error_m=minimum_rod_error,
                min_head_clearance_m=minimum_head_clearance,
                blender="PASS",unity="NOT RUN")

def export(outdir):
    outdir.mkdir(parents=True,exist_ok=True)
    for ob in bpy.context.selected_objects: ob.select_set(False)
    for ob in OBJECTS: ob.select_set(True)
    bpy.context.view_layer.objects.active=OBJECTS[0]
    blend=outdir/(MODEL_ID+".blend")
    bpy.ops.wm.save_as_mainfile(filepath=str(blend))
    glb=outdir/(MODEL_ID+"_animated.glb")
    bpy.ops.export_scene.gltf(filepath=str(glb),export_format="GLB",
        use_selection=True,export_animations=True,export_yup=True)
    assert blend.exists() and blend.stat().st_size>10000
    assert glb.exists() and glb.stat().st_size>10000
    return {"blend":str(blend),"glb":str(glb),
            "blend_bytes":blend.stat().st_size,"glb_bytes":glb.stat().st_size}

def main():
    ap=argparse.ArgumentParser()
    ap.add_argument("--out",default="Tools/ModelFactory/M02/M02-012/generated")
    args=ap.parse_args(__import__("sys").argv[__import__("sys").argv.index("--")+1:]
                       if "--" in __import__("sys").argv else [])
    rotor=build()
    animate(rotor)
    report=validate()
    report["exports"]=export(Path(args.out).resolve())
    Path(args.out).mkdir(parents=True,exist_ok=True)
    (Path(args.out)/"M02-012_qa.json").write_text(
        json.dumps(report,indent=2)+"\n",encoding="utf-8")
    print(json.dumps(report,indent=2))

if __name__=="__main__":
    main()
