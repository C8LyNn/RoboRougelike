#!/usr/bin/env python3
"""M02-015: exposed spur-gear gearbox, deterministic Blender 4.x asset factory.

1 Blender unit = 1 m. +Z up; +Y is the common gear/shaft axis.
The fixed chassis mounting origin is (0,0,0), on the underside foot plane.
Only this generated scene is cleared; never run inside a shared Unity scene.
"""
from __future__ import annotations
import argparse
import json
import math
from pathlib import Path

MODULE = 0.012
INPUT_TEETH = 18
OUTPUT_TEETH = 36
RATIO = -INPUT_TEETH / OUTPUT_TEETH
PITCH_DISTANCE = MODULE * (INPUT_TEETH + OUTPUT_TEETH) / 2
INPUT_X = -0.174
OUTPUT_X = INPUT_X + PITCH_DISTANCE
GEAR_Z = 0.345
FRAME_MIN_X, FRAME_MAX_X = -0.350, 0.455
FRAME_MIN_Z, FRAME_MAX_Z = 0.055, 0.625
GEAR_HALF_WIDTH = 0.034
FRAMES = 121
MATERIALS = ("frame_graphite", "machined_titanium", "brushed_steel",
             "copper_fasteners", "rubber_isolators", "ceramic_white",
             "safety_amber", "indicator_cyan")


def gear_outline(teeth, phase=0.0, rotation=0.0):
    """Radially monotone, chamfer-like tooth profile (not involute contact geometry)."""
    pitch = 2.0 * math.pi / teeth
    root = MODULE * (teeth / 2.0 - 0.82)
    tip = MODULE * (teeth / 2.0 + 0.50)
    points = []
    for tooth in range(teeth):
        angle = phase + rotation + tooth * pitch
        for fraction, radius in ((-0.50, root), (-0.32, root),
                                 (-0.17, tip), (0.00, tip),
                                 (0.17, tip), (0.32, root)):
            theta = angle + fraction * pitch
            points.append((radius * math.cos(theta), radius * math.sin(theta)))
    return points


def _orient(a, b, c):
    return (b[0]-a[0])*(c[1]-a[1])-(b[1]-a[1])*(c[0]-a[0])


def _segments_cross(a, b, c, d):
    return (_orient(a,b,c)*_orient(a,b,d) < -1e-15
            and _orient(c,d,a)*_orient(c,d,b) < -1e-15)


def _inside(point, polygon):
    x,y = point
    inside = False
    for a,b in zip(polygon, polygon[1:]+polygon[:1]):
        if (a[1] > y) != (b[1] > y):
            if x < (b[0]-a[0])*(y-a[1])/(b[1]-a[1])+a[0]:
                inside = not inside
    return inside


def outlines_intersect(p, q):
    """Exact segment/containment test of 2D polygon outlines, not PhysX."""
    for a,b in zip(p, p[1:]+p[:1]):
        for c,d in zip(q, q[1:]+q[:1]):
            if (max(a[0],b[0]) < min(c[0],d[0])
                or max(c[0],d[0]) < min(a[0],b[0])
                or max(a[1],b[1]) < min(c[1],d[1])
                or max(c[1],d[1]) < min(a[1],b[1])):
                continue
            if _segments_cross(a,b,c,d):
                return True
    return _inside(p[0],q) or _inside(q[0],p)


def self_test():
    assert INPUT_TEETH == 18 and OUTPUT_TEETH == 36
    assert abs(PITCH_DISTANCE - (OUTPUT_X-INPUT_X)) < 1e-10
    assert len(MATERIALS) >= 3
    small_r = MODULE*(INPUT_TEETH/2 + .50)
    large_r = MODULE*(OUTPUT_TEETH/2 + .50)
    assert INPUT_X-small_r > FRAME_MIN_X + 0.03
    assert OUTPUT_X+large_r < FRAME_MAX_X - 0.03
    assert GEAR_Z-large_r > FRAME_MIN_Z + 0.03
    assert GEAR_Z+large_r < FRAME_MAX_Z - 0.03
    # Full input two-revolution cycle; output one opposite revolution.
    # All gear outlines are checked in the common XZ plane.
    for i in range(FRAMES):
        angle = 4.0*math.pi*i/(FRAMES-1)
        small = [(x+INPUT_X,z+GEAR_Z)
                 for x,z in gear_outline(INPUT_TEETH, rotation=angle)]
        large = [(x+OUTPUT_X,z+GEAR_Z)
                 for x,z in gear_outline(OUTPUT_TEETH, phase=math.pi/OUTPUT_TEETH,
                                        rotation=RATIO*angle)]
        assert not outlines_intersect(small, large), (
            "Gear-tooth silhouette collision at sample {}".format(i))
    # Axial separation from stationary front/back support frames:
    assert 0.125-GEAR_HALF_WIDTH > 0.080
    return {"id":"M02-015","sampled_gear_phases":FRAMES,
            "silhouette_intersections":0,"teeth":[INPUT_TEETH,OUTPUT_TEETH],
            "pitch_center_distance_m":PITCH_DISTANCE,
            "gear_ratio":RATIO,"frame_clearance_verified":True,
            "materials_planned":len(MATERIALS),
            "scope":"2D polygon intersections and conservative frame envelopes only"}


def make_material(bpy, name, rgba, metal, rough, emission=0.0):
    material = bpy.data.materials.new(name)
    material.diffuse_color = rgba
    material.use_nodes = True
    bsdf = material.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = rgba
    bsdf.inputs["Metallic"].default_value = metal
    bsdf.inputs["Roughness"].default_value = rough
    if emission:
        if "Emission Color" in bsdf.inputs:
            bsdf.inputs["Emission Color"].default_value = rgba
            bsdf.inputs["Emission Strength"].default_value = emission
        elif "Emission" in bsdf.inputs:
            bsdf.inputs["Emission"].default_value = rgba
    return material


def make_mesh(bpy, name, verts, faces, material, parent=None):
    mesh = bpy.data.meshes.new(name + "_MESH")
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(material)
    if parent is not None:
        obj.parent = parent
    return obj


def extruded_annulus(bpy, name, outer, inner_radius, width, material,
                     parent=None, ycenter=0.0):
    """Closed outward-wound quad shell with a real through-bore; +Y is axle."""
    n = len(outer)
    assert n >= 12 and inner_radius > 0
    verts = []
    for y, radius_mode in ((-width/2, "outer"), (width/2, "outer"),
                           (-width/2, "inner"), (width/2, "inner")):
        for x,z in outer:
            if radius_mode == "inner":
                a = math.atan2(z,x)
                x,z = inner_radius*math.cos(a),inner_radius*math.sin(a)
            verts.append((x,y+ycenter,z))
    faces = []
    for i in range(n):
        j = (i+1)%n
        ob,of,ib,inf = i,n+i,2*n+i,3*n+i
        ob2,of2,ib2,inf2 = j,n+j,2*n+j,3*n+j
        faces.extend(((ob,ob2,ib2,ib),
                      (of2,of,inf,inf2),
                      (ob,of,of2,ob2),
                      (ib2,inf2,inf,ib)))
    return make_mesh(bpy,name,verts,faces,material,parent)


def tube(bpy, name, r_outer, r_inner, width, material, parent=None,
         center=(0,0,0), segments=48):
    circle = [(r_outer*math.cos(2*math.pi*i/segments),
               r_outer*math.sin(2*math.pi*i/segments))
              for i in range(segments)]
    obj = extruded_annulus(bpy,name,circle,r_inner,width,material,parent)
    obj.location = center
    return obj


def cylinder(bpy, name, radius, length, material, parent=None, center=(0,0,0),
             vertices=32):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices,radius=radius,
                                        depth=length,location=(0,0,0))
    obj=bpy.context.object
    obj.name=name
    obj.rotation_euler[0]=math.pi/2
    obj.data.materials.append(material)
    if parent is not None:
        obj.parent=parent
    obj.location=center
    return obj


def block(bpy,name,center,dimensions,material,parent=None,bevel=0.0):
    bpy.ops.mesh.primitive_cube_add(size=1,location=(0,0,0))
    obj=bpy.context.object
    obj.name=name
    obj.dimensions=dimensions
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    obj.data.materials.append(material)
    if parent is not None:
        obj.parent=parent
    obj.location=center
    if bevel:
        modifier=obj.modifiers.new("machined edge radius","BEVEL")
        modifier.width=bevel
        modifier.segments=2
        obj.modifiers.new("weighted corner normals","WEIGHTED_NORMAL")
    return obj


def strut(bpy,name,p0,p1,radius,material,parent=None,vertices=12):
    from mathutils import Vector
    v0,v1=Vector(p0),Vector(p1)
    direction=v1-v0
    assert direction.length>1e-5
    obj=cylinder(bpy,name,radius,direction.length,material,parent)
    obj.location=(v0+v1)/2
    # Cylinder's local Z points along the strut; override default +Y orientation.
    obj.rotation_euler=Vector((0,0,1)).rotation_difference(direction).to_euler()
    return obj


def rotor_root(bpy,name,center):
    obj=bpy.data.objects.new(name,None)
    bpy.context.collection.objects.link(obj)
    obj.empty_display_type="ARROWS"
    obj.empty_display_size=0.075
    obj.location=center
    return obj


def build_scene(bpy):
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    bpy.context.scene.unit_settings.system="METRIC"
    bpy.context.scene.unit_settings.scale_length=1.0
    bpy.context.scene.render.fps=30
    bpy.context.scene.frame_start=1
    bpy.context.scene.frame_end=FRAMES
    palette={
      "frame_graphite":((.055,.071,.090,1),.70,.37,0),
      "machined_titanium":((.44,.51,.57,1),.84,.23,0),
      "brushed_steel":((.19,.23,.27,1),.82,.31,0),
      "copper_fasteners":((.67,.31,.11,1),.72,.29,0),
      "rubber_isolators":((.035,.040,.047,1),.03,.84,0),
      "ceramic_white":((.74,.76,.72,1),.14,.35,0),
      "safety_amber":((.97,.43,.045,1),.22,.35,.25),
      "indicator_cyan":((.025,.63,.75,1),.15,.28,.8)}
    mats={k:make_material(bpy,k,*v) for k,v in palette.items()}
    frame=mats["frame_graphite"]
    steel=mats["brushed_steel"]
    titanium=mats["machined_titanium"]
    copper=mats["copper_fasteners"]
    rubber=mats["rubber_isolators"]
    amber=mats["safety_amber"]
    white=mats["ceramic_white"]
    cyan=mats["indicator_cyan"]

    mount=rotor_root(bpy,"M02-015_MOUNT_ORIGIN__0_0_0",(0,0,0))
    # Two separated open steel trusses: the entire moving gear pack is
    # inside +/-0.034m, while the frames sit at +/-0.140m.
    for side,y in (("BACK",-0.14),("FRONT",0.14)):
        for x,tag in ((FRAME_MIN_X,"L"),(FRAME_MAX_X,"R")):
            strut(bpy,side+"_upright_"+tag,(x,y,FRAME_MIN_Z),
                  (x,y,FRAME_MAX_Z),.016,frame)
        for z,tag in ((FRAME_MIN_Z,"BOTTOM"),(FRAME_MAX_Z,"TOP")):
            strut(bpy,side+"_rail_"+tag,(FRAME_MIN_X,y,z),
                  (FRAME_MAX_X,y,z),.017,frame)
        for x,z,tag in ((FRAME_MIN_X,FRAME_MIN_Z,"LB"),
                        (FRAME_MAX_X,FRAME_MIN_Z,"RB"),
                        (FRAME_MIN_X,FRAME_MAX_Z,"LT"),
                        (FRAME_MAX_X,FRAME_MAX_Z,"RT")):
            block(bpy,side+"_gusset_"+tag,(x,y,z),(.070,.026,.070),steel,
                  bevel=.005)
        # Non-occluding rear diagonal bracing near the perimeter.
        if side=="BACK":
            strut(bpy,"BACK_crossbrace_1",
                  (FRAME_MIN_X,y,FRAME_MIN_Z+.08),
                  (FRAME_MIN_X+.15,y,FRAME_MAX_Z),.010,steel)
            strut(bpy,"BACK_crossbrace_2",
                  (FRAME_MAX_X,y,FRAME_MIN_Z+.08),
                  (FRAME_MAX_X-.15,y,FRAME_MAX_Z),.010,steel)
    for x,tagx in ((FRAME_MIN_X,"L"),(FRAME_MAX_X,"R")):
        for z,tagz in ((FRAME_MIN_Z,"B"),(FRAME_MAX_Z,"T")):
            strut(bpy,"cross_member_"+tagx+tagz,(x,-.14,z),
                  (x,.14,z),.013,titanium)
    for x,tagx in ((-.28,"L"),(.36,"R")):
        for y,tagy in ((-.14,"B"),(.14,"F")):
            block(bpy,"chassis_foot_"+tagx+tagy,(x,y,.018),
                  (.10,.080,.036),frame,bevel=.004)
            cylinder(bpy,"foot_bolt_"+tagx+tagy,.010,.008,copper,
                     center=(x,y,.042))
            block(bpy,"rubber_foot_"+tagx+tagy,(x,y,.004),
                  (.075,.060,.008),rubber)

    for x,tag in ((INPUT_X,"INPUT"),(OUTPUT_X,"OUTPUT")):
        for y,side in ((-.14,"REAR"),(.14,"FRONT")):
            bearing=tube(bpy,tag+"_"+side+"_bearing",.046,.025,.025,
                         steel,center=(x,y,GEAR_Z))
            tube(bpy,tag+"_"+side+"_bearing_seal",.031,.024,.009,
                 rubber,center=(x,y+(.017 if y>0 else -.017),GEAR_Z))
            for a in range(4):
                angle=math.pi*a/2
                cylinder(bpy,tag+"_"+side+"_bearing_bolt_"+str(a),
                         .006,.012,copper,center=(x+.038*math.cos(angle),
                         y+(.019 if y>0 else -.019),
                         GEAR_Z+.038*math.sin(angle)),vertices=12)

    for name,x,teeth,phase,hole,hub,spokes,bolt_r in (
        ("INPUT",INPUT_X,INPUT_TEETH,0.0,.068,.051,6,.082),
        ("OUTPUT",OUTPUT_X,OUTPUT_TEETH,math.pi/OUTPUT_TEETH,.151,.064,8,.180)):
        root=rotor_root(bpy,name+"_GEAR_ROT_Y",(x,0,GEAR_Z))
        extruded_annulus(bpy,name+"_EXPOSED_SPUR_TEETH",
                         gear_outline(teeth,phase),hole,
                         GEAR_HALF_WIDTH*2,titanium,root)
        tube(bpy,name+"_KEYED_HUB",hub,.019,.090,steel,root)
        cylinder(bpy,name+"_SHAFT",.020,.49,steel,root,vertices=32)
        for i in range(spokes):
            angle=2*math.pi*i/spokes
            r0=hub-.005
            r1=hole+.007
            strut(bpy,name+"_OPEN_SPOKE_"+str(i),
                  (r0*math.cos(angle),0,r0*math.sin(angle)),
                  (r1*math.cos(angle),0,r1*math.sin(angle)),
                  .011 if teeth==OUTPUT_TEETH else .008,steel,root)
        for i in range(8 if teeth==OUTPUT_TEETH else 6):
            angle=2*math.pi*i/(8 if teeth==OUTPUT_TEETH else 6)
            cylinder(bpy,name+"_COPPER_RIVET_"+str(i),.007,.010,copper,
                     root,center=(bolt_r*math.cos(angle),.039,
                                  bolt_r*math.sin(angle)),vertices=12)
        tube(bpy,name+"_SHAFT_COLLAR",.030,.020,.020,copper,root,
             center=(0,.095,0))
        tube(bpy,name+"_REAR_COLLAR",.030,.020,.020,copper,root,
             center=(0,-.095,0))
        # Fine ceramic identifier ring on the moving hub, not a full guard.
        tube(bpy,name+"_HUB_MARK",hub+.004,hub-.002,.008,white,root,
             center=(0,.051,0))
        for i in range(0,FRAMES,3):
            frame_num=i+1
            angle=4*math.pi*i/(FRAMES-1)
            root.rotation_euler=(0,angle if name=="INPUT" else RATIO*angle,0)
            root.keyframe_insert(data_path="rotation_euler",frame=frame_num)
        if (FRAMES-1)%3:
            root.rotation_euler=(0,4*math.pi if name=="INPUT" else RATIO*4*math.pi,0)
            root.keyframe_insert(data_path="rotation_euler",frame=FRAMES)
        # Blender 5 uses layered Action channels instead of Action.fcurves.
        # Keyframe samples every 3 frames preserve continuous revolutions.
        action = root.animation_data.action if root.animation_data else None
        if action and hasattr(action, "fcurves"):
            for fcurve in action.fcurves:
                for key in fcurve.keyframe_points:
                    key.interpolation = "LINEAR"
        elif action and hasattr(action, "layers"):
            for layer in action.layers:
                for strip in layer.strips:
                    for slot in action.slots:
                        bag = strip.channelbag(slot)
                        if bag is not None:
                            for fcurve in bag.fcurves:
                                for key in fcurve.keyframe_points:
                                    key.interpolation = "LINEAR"

    # Unobstructed stationary service and safety details.
    block(bpy,"service_tag_plate",(FRAME_MAX_X,.14,.355),
          (.010,.040,.11),white,bevel=.002)
    block(bpy,"service_tag_stripe",(FRAME_MAX_X+.006,.14,.355),
          (.006,.042,.055),amber)
    block(bpy,"speed_sensor_bracket",(FRAME_MIN_X,-.14,.41),
          (.024,.045,.055),steel,bevel=.003)
    block(bpy,"speed_sensor_lens",(FRAME_MIN_X-.014,-.14,.41),
          (.008,.027,.022),cyan)
    # Scene/world and metadata are stable between runs.
    bpy.context.scene["asset_id"]="M02-015"
    bpy.context.scene["mount_origin_m"]="(0,0,0) chassis foot plane"
    bpy.context.scene["axis"]="Y (input +Y, output counter-rotating)"
    bpy.context.scene["ratio"]="-1/2, 18:36 teeth"
    bpy.context.scene["QA"]="121 sampled polygon phases; Unity physics NOT RUN"
    bpy.context.scene.frame_set(1)
    return {"mesh_objects":sum(o.type=="MESH" for o in bpy.data.objects),
            "materials":len(mats),
            "rotor_nodes":["INPUT_GEAR_ROT_Y","OUTPUT_GEAR_ROT_Y"]}


def export_blender(out_dir):
    import bpy
    out_dir.mkdir(parents=True,exist_ok=True)
    qa=self_test()
    scene=build_scene(bpy)
    # Regenerate in the SAME Blender process to verify no accumulation.
    first=scene["mesh_objects"]
    scene=build_scene(bpy)
    assert scene["mesh_objects"]==first
    qa.update(scene)
    bpy.ops.wm.save_as_mainfile(filepath=str(out_dir/"M02-015.blend"))
    bpy.ops.export_scene.gltf(filepath=str(out_dir/"M02-015_animated.glb"),
                              export_format="GLB",export_animations=True)
    try:
        # Blender 5 exporter removed several Blender 4 FBX bake_anim options.
        # Use the current operator defaults; verify the actual file separately.
        bpy.ops.export_scene.fbx(filepath=str(out_dir/"M02-015_animated.fbx"),
                                 axis_forward="-Z",axis_up="Y")
        qa["fbx_export"]="EXPORTED_NOT_IMPORTED"
    except Exception as exc:
        qa["fbx_export"]="BLOCKED_FBX_EXPORT: "+str(exc)
    qa["blender_version"]=bpy.app.version_string
    qa["unity_import"]="NOT RUN"
    (out_dir/"M02-015_qa.json").write_text(
        json.dumps(qa,indent=2,ensure_ascii=False)+"\n",encoding="utf-8")
    print("M02-015 actual Blender export:",json.dumps(qa))


def main():
    parser=argparse.ArgumentParser()
    parser.add_argument("--self-test",action="store_true")
    parser.add_argument("--out",default="build/M02-015")
    args=parser.parse_args()
    if args.self_test:
        print(json.dumps(self_test(),indent=2))
    else:
        export_blender(Path(args.out).resolve())


if __name__=="__main__":
    # Blender passes arguments following the '--' separator.
    import sys
    if "--" in sys.argv:
        sys.argv=[sys.argv[0]]+sys.argv[sys.argv.index("--")+1:]
    main()
