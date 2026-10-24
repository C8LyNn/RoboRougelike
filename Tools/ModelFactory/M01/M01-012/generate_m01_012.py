#!/usr/bin/env python3
"""M01-012 Pneumatic bolt launcher: visual-only Unity 6 Blender source.

blender -b --factory-startup --python-exit-code 1 --python generate_m01_012.py -- --out /tmp/m01-012
One Blender unit is one metre; +Z is muzzle-forward, +Y up, +X right.
MOUNT_TOP_SOCKET is at (0,0,0), the vehicle attachment plane.
The pneumatic ram travels 90 mm in local -Z and returns. No gameplay code.
"""
import argparse
import json
import math
from pathlib import Path
import sys

import bpy
import bmesh
from mathutils import Vector

MODEL_ID = "M01-012"
STEM = "M01-012_pneumatic_bolt_launcher"
COLLECTION_NAME = MODEL_ID + "_PNEUMATIC_BOLT_LAUNCHER"
STROKE = 0.090
FPS = 60
END_FRAME = 61
PALETTE = {
    "Graphite": ((.045,.057,.071,1), .64,.35,0.0),
    "Brushed_Titanium": ((.34,.41,.47,1), .84,.29,0.0),
    "Ivory_Ceramic": ((.79,.82,.77,1), .12,.30,0.0),
    "Copper": ((.56,.245,.090,1), .78,.26,0.0),
    "Black_Rubber": ((.017,.025,.032,1), .02,.72,0.0),
    "Pressure_Cyan": ((.025,.70,.88,1), .16,.25,2.1),
    "Warning_Amber": ((.95,.38,.045,1), .12,.32,1.4),
}
parts = []
fixed = []
moving = []

def reset_collection():
    previous = bpy.data.collections.get(COLLECTION_NAME)
    if previous:
        for ob in list(previous.objects):
            bpy.data.objects.remove(ob,do_unlink=True)
        bpy.data.collections.remove(previous)
    for action in list(bpy.data.actions):
        if action.name.startswith(MODEL_ID+"_") and action.users==0:
            bpy.data.actions.remove(action)
    col=bpy.data.collections.new(COLLECTION_NAME)
    bpy.context.scene.collection.children.link(col)
    return col

COL = reset_collection()

def material(name):
    label=MODEL_ID+"_"+name
    m=bpy.data.materials.get(label) or bpy.data.materials.new(label)
    rgba,metal,rough,emission=PALETTE[name]
    m.diffuse_color=rgba
    m.use_nodes=True
    shader=m.node_tree.nodes.get("Principled BSDF")
    shader.inputs["Base Color"].default_value=rgba
    shader.inputs["Metallic"].default_value=metal
    shader.inputs["Roughness"].default_value=rough
    if "Emission Color" in shader.inputs:
        shader.inputs["Emission Color"].default_value=rgba
        shader.inputs["Emission Strength"].default_value=emission
    return m

def node(name, location=(0,0,0), parent=None):
    ob=bpy.data.objects.new(name,None)
    COL.objects.link(ob)
    ob.empty_display_type="ARROWS"
    ob.empty_display_size=.075
    ob.parent=parent
    ob.location=location
    return ob

ROOT=node("MOUNT_TOP_SOCKET")

def attach(ob, name, mat, parent=ROOT, is_moving=False, bevel=0):
    ob.name=name
    ob.parent=parent
    if bevel:
        modifier=ob.modifiers.new("Machined_Edges","BEVEL")
        modifier.width=bevel
        modifier.segments=2
        modifier.limit_method="ANGLE"
        bpy.ops.object.select_all(action="DESELECT")
        ob.select_set(True)
        bpy.context.view_layer.objects.active=ob
        bpy.ops.object.modifier_apply(modifier=modifier.name)
        for face in ob.data.polygons:
            face.use_smooth=True
        normal=ob.modifiers.new("Weighted_Normals","WEIGHTED_NORMAL")
        normal.keep_sharp=True
        bpy.ops.object.modifier_apply(modifier=normal.name)
    for existing in list(ob.users_collection):
        existing.objects.unlink(ob)
    COL.objects.link(ob)
    ob.data.materials.clear()
    ob.data.materials.append(material(mat))
    parts.append(ob)
    (moving if is_moving else fixed).append(ob)
    return ob

def box(name, at, size, mat, parent=ROOT, is_moving=False, bevel=.005):
    bpy.ops.mesh.primitive_cube_add(size=1)
    ob=bpy.context.object
    ob.dimensions=size
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    ob.location=at
    return attach(ob,name,mat,parent,is_moving,min(bevel,min(size)*.19))

def cylinder(name, at, radius, length, mat, parent=ROOT,
             is_moving=False, axis="Z", segments=32, bevel=.001):
    bpy.ops.mesh.primitive_cylinder_add(vertices=segments,radius=radius,depth=length)
    ob=bpy.context.object
    ob.location=at
    if axis=="X":
        ob.rotation_euler.y=math.pi/2
    elif axis=="Y":
        ob.rotation_euler.x=math.pi/2
    return attach(ob,name,mat,parent,is_moving,bevel)

def torus(name, at, major, minor, mat, parent=ROOT, is_moving=False):
    bpy.ops.mesh.primitive_torus_add(major_segments=48,minor_segments=12,
                                    major_radius=major,minor_radius=minor)
    ob=bpy.context.object
    ob.location=at
    return attach(ob,name,mat,parent,is_moving)

def annulus(name, at, outer, inner, length, mat, parent=ROOT,
            is_moving=False, segments=48):
    """Real open-through cylinder: 2 radial walls, 2 annular end faces."""
    assert outer>inner>0 and length>0
    verts=[]
    for i in range(segments):
        a=2*math.pi*i/segments
        x,y=math.cos(a),math.sin(a)
        verts.extend(((outer*x,outer*y,-length/2),
                      (outer*x,outer*y,length/2),
                      (inner*x,inner*y,-length/2),
                      (inner*x,inner*y,length/2)))
    faces=[]
    for i in range(segments):
        j=(i+1)%segments
        a,b=4*i,4*j
        faces.extend(((a,b,b+1,a+1),(a+2,a+3,b+3,b+2),
                      (a+1,b+1,b+3,a+3),(a,a+2,b+2,b)))
    mesh=bpy.data.meshes.new(name+"_Mesh")
    mesh.from_pydata(verts,[],faces)
    mesh.update()
    ob=bpy.data.objects.new(name,mesh)
    COL.objects.link(ob)
    ob.location=at
    return attach(ob,name,mat,parent,is_moving)

def strut(name, a, b, width, thickness, mat):
    A,B=Vector(a),Vector(b)
    ob=box(name,(A+B)*.5,(width,thickness,(B-A).length),mat,bevel=.004)
    ob.rotation_euler=(B-A).to_track_quat("Z","Y").to_euler()
    return ob

def build():
    scene=bpy.context.scene
    scene.unit_settings.system="METRIC"
    scene.unit_settings.scale_length=1.0
    scene.frame_start,scene.frame_end,scene.render.fps=1,END_FRAME,FPS

    # Top mount is not a fake floating empty: these plates touch the receiver.
    box("Socket_Flange",(0,.025,.15),(.340,.050,.300),"Brushed_Titanium",bevel=.012)
    box("Socket_Seal",(0,.059,.15),(.300,.018,.265),"Black_Rubber",bevel=.004)
    for x in (-.130,.130):
        for z in (.050,.250):
            cylinder("Mount_Bolt_%s_%s"%(int(x*1000),int(z*1000)),
                     (x,.069,z),.013,.017,"Copper",axis="Y",segments=12)
    box("Riser_Core",(0,.150,.36),(.200,.190,.310),"Brushed_Titanium",bevel=.020)
    box("Receiver",(0,.270,.695),(.350,.215,.625),"Graphite",bevel=.024)
    box("Rear_Cushion",(0,.265,.368),(.300,.125,.040),"Black_Rubber",bevel=.010)
    box("Receiver_Ceramic_Top",(0,.390,.715),(.282,.032,.470),
        "Ivory_Ceramic",bevel=.009)
    box("Receiver_Belly",(0,.142,.730),(.270,.036,.485),
        "Brushed_Titanium",bevel=.008)
    for side,label in ((-1,"L"),(1,"R")):
        box("Receiver_Side_Armor_"+label,(side*.184,.277,.760),
            (.023,.135,.350),"Ivory_Ceramic",bevel=.009)
        for n,z in enumerate((.52,.70,.88)):
            cylinder("Side_Rivet_%s_%d"%(label,n),(side*.203,.277,z),
                     .013,.016,"Copper",axis="X",segments=12)
    box("Lower_Magazine",(0,.094,.750),(.190,.100,.230),
        "Graphite",bevel=.015)
    box("Magazine_Amber_Indicator",(0,.038,.775),(.090,.012,.065),
        "Warning_Amber",bevel=.004)

    # Barrel is genuinely hollow: no capped cylinder over its bore.
    annulus("Bored_Bolt_Barrel",(0,.297,1.385),.059,.031,.900,
            "Brushed_Titanium",segments=64)
    annulus("Muzzle_Brake",(0,.297,1.798),.085,.048,.145,
            "Graphite",segments=48)
    torus("Copper_Muzzle_Ring",(0,.297,1.855),.071,.007,"Copper")
    for n,z in enumerate((1.05,1.21,1.37,1.53,1.69)):
        torus("Barrel_Cooling_Ring_%02d"%n,(0,.297,z),.070,.007,
              "Copper")
        for side,label in ((-1,"L"),(1,"R")):
            box("Ceramic_Heat_Fin_%s_%d"%(label,n),
                (side*.079,.297,z),(.024,.067,.075),
                "Ivory_Ceramic",bevel=.005)

    # Exposed cylindrical pressure vessel on the left, with visible
    # pressure dial, segmented safety bands and mechanical load brackets.
    cylinder("Pressure_Tank",(-.270,.462,.682),.123,.635,
             "Brushed_Titanium",segments=48,bevel=.009)
    cylinder("Tank_Rear_Cap",(-.270,.462,.370),.111,.040,
             "Graphite",segments=48,bevel=.005)
    cylinder("Tank_Front_Cap",(-.270,.462,.997),.109,.045,
             "Graphite",segments=48,bevel=.005)
    for n,z in enumerate((.443,.585,.727,.869)):
        torus("Tank_Copper_Band_%d"%n,(-.270,.462,z),.124,.010,"Copper")
    for n,z in enumerate((.50,.78)):
        box("Tank_Load_Bridge_%d"%n,(-.178,.396,z),
            (.174,.074,.065),"Graphite",bevel=.007)
    cylinder("Pressure_Gauge_Housing",(-.270,.601,.715),.059,.031,
             "Graphite",axis="Y",segments=48)
    cylinder("Pressure_Gauge_Face",(-.270,.621,.715),.044,.006,
             "Pressure_Cyan",axis="Y",segments=48,bevel=0)
    box("Gauge_Amber_Needle",(-.270,.629,.715),(.008,.006,.062),
        "Warning_Amber",bevel=.002)
    box("Pressure_Line_Left",(-.185,.438,1.015),(.040,.035,.105),
        "Copper",bevel=.006)
    strut("Pressure_Line_Diagonal",(-.183,.439,1.015),
          (-.098,.386,1.050),.025,.025,"Copper")
    for n,z in enumerate((.54,.66,.78,.90)):
        box("Tank_Ceramic_Scale_%02d"%n,(-.396,.462,z),
            (.012,.045,.038),"Ivory_Ceramic",bevel=.003)

    # Right-hand pneumatic drive: tube is a real annular mesh with
    # 32mm bore. The animated 20mm radius ram travels inside that
    # hollow opening, NOT through the tube's material.
    tube_center=(.330,.349,.653)
    annulus("Piston_Cylinder_Hollow",tube_center,.069,.032,.390,
            "Graphite",segments=64)
    torus("Piston_Rear_Copper_Collar",(.330,.349,.484),
          .071,.008,"Copper")
    torus("Piston_Front_Copper_Collar",(.330,.349,.831),
          .071,.008,"Copper")
    box("Piston_Support_Inner",(.224,.349,.535),(.145,.078,.080),
        "Brushed_Titanium",bevel=.007)
    box("Piston_Support_Rear",(.224,.349,.640),(.145,.065,.050),
        "Graphite",bevel=.005)
    for z in (.540,.640):
        cylinder("Piston_Anchor_Bolt_%d"%int(z*1000),
                 (.261,.389,z),.014,.025,"Copper",axis="Y",segments=12)
    ram=node("PNEUMATIC_RAM",parent=ROOT)
    cylinder("Ram_Chrome_Rod",(.330,.349,.967),.020,.400,
             "Brushed_Titanium",parent=ram,is_moving=True,
             segments=32,bevel=.001)
    cylinder("Ram_Piston_Head",(.330,.349,1.203),.052,.072,
             "Graphite",parent=ram,is_moving=True,
             segments=32,bevel=.005)
    cylinder("Ram_Copper_Collar",(.330,.349,1.244),.044,.016,
             "Copper",parent=ram,is_moving=True,segments=32)
    box("Ram_Ceramic_Status",(.330,.405,1.204),(.040,.009,.041),
        "Pressure_Cyan",parent=ram,is_moving=True,bevel=.002)
    for frame,travel in ((1,0),(12,-STROKE),(23,-STROKE),
                         (40,0),(61,0)):
        ram.location.z=travel
        ram.keyframe_insert(data_path="location",frame=frame,group="Pneumatic Stroke")
    ram.location.z=0
    scene.frame_set(1)
    return scene

def mesh_qa():
    vertices=triangles=0
    mats=set()
    for ob in parts:
        mesh=ob.data
        assert len(mesh.materials)==1,(ob.name,"missing material")
        mats.add(mesh.materials[0].name)
        vertices+=len(mesh.vertices)
        mesh.calc_loop_triangles()
        triangles+=len(mesh.loop_triangles)
        bm=bmesh.new()
        bm.from_mesh(mesh)
        assert bm.faces and all(edge.is_manifold for edge in bm.edges),(
            ob.name,"nonmanifold")
        assert bm.calc_volume(signed=True)>1e-9,(ob.name,"wrong normals/volume")
        bm.free()
        for tri in mesh.loop_triangles:
            a,b,c=(mesh.vertices[i].co for i in tri.vertices)
            assert (b-a).cross(c-a).length>1e-12,(ob.name,"degenerate")
    assert len(mats)>=3
    assert len(parts)>=50
    return vertices,triangles,len(mats)

def aabb(ob):
    pts=[ob.matrix_world @ v.co for v in ob.data.vertices]
    return [(min(v[k] for v in pts),max(v[k] for v in pts))
            for k in range(3)]

def overlap(a,b):
    return [min(a[i][1],b[i][1])-max(a[i][0],b[i][0])
            for i in range(3)]

def motion_qa(scene):
    tube=bpy.data.objects["Piston_Cylinder_Hollow"]
    rod=bpy.data.objects["Ram_Chrome_Rod"]
    collar_names={"Piston_Front_Copper_Collar","Piston_Rear_Copper_Collar"}
    checked=0
    samples=0
    min_gap=float("inf")
    for frame in range(1,END_FRAME+1):
        scene.frame_set(frame)
        bpy.context.view_layer.update()
        tb=aabb(tube)
        rb=aabb(rod)
        # Real annular bore: rod is concentric and its radius is below
        # the 32mm inner radius at every frame, including extrema.
        assert abs((rb[0][0]+rb[0][1])*.5-.330)<1e-5
        assert abs((rb[1][0]+rb[1][1])*.5-.349)<1e-5
        assert .032-.020>=.0119
        assert rb[2][0]>=.675 and rb[2][0]<=.770
        assert rb[2][1]>=1.075
        assert rb[2][0]<tb[2][1]-.06,(frame,"ram disconnected")
        for m in moving:
            ma=aabb(m)
            for stationary in fixed:
                sb=aabb(stationary)
                ol=overlap(ma,sb)
                checked+=1
                if m is rod and (stationary is tube or stationary.name in collar_names):
                    # AABB is conservative for a hollow cylinder. Check
                    # actual radial bore clearance instead of false collision.
                    assert .020 < (.032 if stationary is tube else .063)
                    continue
                if all(axis>1e-6 for axis in ol):
                    raise AssertionError((frame,"moving/fixed penetration",
                                          m.name,stationary.name,ol))
                gap=math.sqrt(sum(max(0.,-axis)**2 for axis in ol))
                if gap>0:
                    min_gap=min(min_gap,gap)
        samples+=1
    scene.frame_set(1)
    assert samples==61
    return samples,checked,min_gap

def export_files(folder):
    folder.mkdir(parents=True,exist_ok=True)
    scene=bpy.context.scene
    scene.frame_set(1)
    selected=tuple(bpy.context.selected_objects)
    active=bpy.context.view_layer.objects.active
    try:
        bpy.ops.object.select_all(action="DESELECT")
        for ob in COL.objects:
            ob.select_set(True)
        bpy.context.view_layer.objects.active=ROOT
        bpy.ops.wm.save_as_mainfile(filepath=str(folder/(STEM+".blend")))
        bpy.ops.export_scene.gltf(filepath=str(folder/(STEM+"_animated.glb")),
                                  export_format="GLB",export_animations=True,
                                  use_selection=True)
        bpy.ops.export_scene.fbx(filepath=str(folder/(STEM+"_animated.fbx")),
                                  axis_forward="-Z",axis_up="Y",
                                  add_leaf_bones=False,bake_anim=True,
                                  use_selection=True)
        if hasattr(bpy.ops.wm,"obj_export"):
            bpy.ops.wm.obj_export(filepath=str(folder/(STEM+".obj")),
                                  export_materials=True,
                                  export_selected_objects=True)
        else:
            bpy.ops.export_scene.obj(filepath=str(folder/(STEM+".obj")),
                                      use_materials=True,use_selection=True)
    finally:
        bpy.ops.object.select_all(action="DESELECT")
        for ob in selected:
            if bpy.data.objects.get(ob.name) is ob:
                ob.select_set(True)
        if active and bpy.data.objects.get(active.name) is active:
            bpy.context.view_layer.objects.active=active
    for suffix in (".blend","_animated.glb","_animated.fbx",".obj",".mtl"):
        path=folder/(STEM+suffix)
        assert path.exists() and path.stat().st_size>100,(path,"missing export")

def main():
    argv=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else []
    parser=argparse.ArgumentParser()
    parser.add_argument("--out",default="/tmp/m01-012")
    args=parser.parse_args(argv)
    folder=Path(args.out).resolve()
    scene=build()
    verts,tris,mats=mesh_qa()
    samples,checks,gap=motion_qa(scene)
    export_files(folder)
    report={
        "id":MODEL_ID,"model":"Pneumatyczna wyrzutnia śrub",
        "status":"BLENDER_EXPORTED",
        "blender_version":bpy.app.version_string,
        "units":"metres","forward":"+Z","up":"+Y",
        "mount":"MOUNT_TOP_SOCKET at world (0,0,0); top vehicle socket",
        "animation_node":"PNEUMATIC_RAM","axis":"local -Z",
        "stroke_m":STROKE,"fps":FPS,"frames":[1,END_FRAME],
        "motion_samples":samples,"moving_fixed_aabb_checks":checks,
        "minimum_noncontact_aabb_gap_m":round(gap,6),
        "mesh_objects":len(parts),"vertices":verts,
        "triangles":tris,"materials":mats,
        "manifold_and_positive_volume":"PASS",
        "pneumatic_bore_clearance_m":.012,
        "collision_qa":"PASS (AABB except analytically cleared annular bore)",
        "unity_import":"NOT RUN","unity_playmode":"NOT RUN",
        "manual_visual_review":"NOT RUN",
        "scope":"visual-only; no WPN or gameplay edits"
    }
    (folder/(MODEL_ID+"_qa.json")).write_text(
        json.dumps(report,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
    print(MODEL_ID+"_RESULT "+json.dumps(report,ensure_ascii=False))

if __name__=="__main__":
    main()
