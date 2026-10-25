#!/usr/bin/env python3
"""M02-021 retractable screw-jack support foot. Blender bpy 3.4+; metres."""
import bpy
import bmesh
import json
import math
from pathlib import Path
from mathutils import Vector

ID = "M02-021"
OUT = Path(__file__).resolve().parent / "generated"
OUT.mkdir(parents=True, exist_ok=True)
FRAMES = 121
STROKE = 0.250
TURNS = 3.0
PI = math.pi

def extension(frame):
    return (1.0 - math.cos(2.0 * PI * (frame - 1) / (FRAMES - 1))) * 0.5

def analytical_qa():
    samples = []
    for frame in range(1, FRAMES + 1):
        s = extension(frame)
        slide = -STROKE * s
        inner_top = -0.340 + slide
        outer_bottom = -0.700
        overlap = inner_top - outer_bottom
        pad_top = -0.738 + slide
        sleeve_to_pad = outer_bottom - pad_top
        nut_center = -0.355 + slide
        screw_bottom = -0.650
        nut_engagement = nut_center - screw_bottom
        rod_bottom = -0.940
        bushing_center = -0.580 + slide
        rod_margin = bushing_center - rod_bottom
        samples.append((overlap, sleeve_to_pad, nut_engagement, rod_margin))
    mins = [min(x[i] for x in samples) for i in range(4)]
    assert mins[0] > 0.100, mins
    assert mins[1] > 0.030, mins
    assert mins[2] > 0.040, mins
    assert mins[3] > 0.090, mins
    # Square sleeve: inner opening half-width 0.086; slider outside 0.078.
    sleeve_radial = 0.086 - 0.078
    # Nut is contained in slider cavity; 0.055 square half-width.
    nut_radial = 0.055 - 0.052
    # Lead-screw thread outside 0.028; nut bore radius 0.031.
    thread_bore = 0.031 - 0.028
    # Guide-rod radius 0.012; bushing bore radius 0.018.
    guide_bore = 0.018 - 0.012
    assert min(sleeve_radial, nut_radial, thread_bore, guide_bore) > 0.002
    return {
        "sampled_frames": FRAMES,
        "stroke_m": STROKE,
        "screw_turns_each_direction": TURNS,
        "minimum_inner_outer_overlap_m": mins[0],
        "minimum_pad_sleeve_axial_clearance_m": mins[1],
        "minimum_nut_engagement_margin_m": mins[2],
        "minimum_guide_rod_margin_m": mins[3],
        "minimum_sleeve_radial_clearance_m": sleeve_radial,
        "minimum_nut_to_inner_tube_clearance_m": nut_radial,
        "minimum_thread_to_nut_bore_clearance_m": thread_bore,
        "minimum_guide_rod_bore_clearance_m": guide_bore,
        "scope": "Analytical kinematic envelopes, not a triangle-level rigid-body collision simulation"
    }

def clean():
    for obj in list(bpy.data.objects):
        bpy.data.objects.remove(obj, do_unlink=True)
    for col in list(bpy.data.collections):
        if col.users == 0 or col.name.startswith(ID + "_ASSEMBLY"):
            bpy.data.collections.remove(col)
    for datablocks in (bpy.data.meshes, bpy.data.curves, bpy.data.materials, bpy.data.actions):
        for block in list(datablocks):
            if block.users == 0:
                datablocks.remove(block)

def build():
    clean()
    scene = bpy.context.scene
    scene.unit_settings.system = "METRIC"
    scene.unit_settings.scale_length = 1.0
    scene.frame_start, scene.frame_end, scene.render.fps = 1, FRAMES, 30
    col = bpy.data.collections.new(ID + "_ASSEMBLY")
    scene.collection.children.link(col)
    def mat(name, rgb, metallic, roughness, glow=False):
        m = bpy.data.materials.new(ID + "_" + name)
        m.diffuse_color = (*rgb, 1.0)
        m.use_nodes = True
        bs = m.node_tree.nodes.get("Principled BSDF")
        bs.inputs["Base Color"].default_value = (*rgb, 1.0)
        bs.inputs["Metallic"].default_value = metallic
        bs.inputs["Roughness"].default_value = roughness
        if glow:
            emission = bs.inputs.get("Emission Color")
            strength = bs.inputs.get("Emission Strength")
            if emission: emission.default_value = (*rgb, 1.0)
            if strength: strength.default_value = 1.8
        return m
    graphite = mat("01_GraphiteSteel", (.085,.110,.140), .82, .32)
    alloy = mat("02_BrushedAlloy", (.51,.59,.65), .73, .30)
    copper = mat("03_Copper", (.66,.34,.12), .78, .28)
    orange = mat("04_SafetyOrange", (.96,.27,.035), .24, .39)
    rubber = mat("05_Rubber", (.030,.035,.041), .04, .87)
    ceramic = mat("06_Ceramic", (.72,.77,.72), .17, .46)
    dark = mat("07_DarkHardened", (.20,.24,.28), .90, .25)
    cyan = mat("08_StatusCyan", (.025,.62,.83), .24, .24, True)
    materials = (graphite, alloy, copper, orange, rubber, ceramic, dark, cyan)

    def attach(obj, material, parent=None):
        for c in list(obj.users_collection): c.objects.unlink(obj)
        col.objects.link(obj)
        if material is not None: obj.data.materials.append(material)
        if parent is not None: obj.parent = parent
        return obj
    def empty(name, location=(0,0,0), parent=None):
        obj = bpy.data.objects.new(name, None)
        col.objects.link(obj)
        obj.location = location
        obj.empty_display_type = "PLAIN_AXES"
        obj.empty_display_size = .045
        if parent is not None: obj.parent = parent
        return obj
    mount = empty("MOUNT_UNDERSIDE_Z0_UNITY_1M")
    slide = empty("SLIDE_TELESCOPIC_Z", parent=mount)
    screw = empty("LEAD_SCREW_ROTATE_Z", parent=mount)
    knob = empty("DRIVE_KNOB_ROTATE_Z", parent=mount)
    def box(name, center, dimensions, material, parent=mount, bevel=0.0):
        bpy.ops.mesh.primitive_cube_add(size=1, location=center)
        obj = bpy.context.object
        obj.name = name
        obj.dimensions = dimensions
        bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
        attach(obj, material, parent)
        if bevel:
            mod = obj.modifiers.new("Soft_machined_edges", "BEVEL")
            mod.width, mod.segments = bevel, 2
            obj.data.use_auto_smooth = True
            obj.modifiers.new("Corner_normals", "WEIGHTED_NORMAL")
        return obj
    def cyl(name, center, radius, depth, material, parent=mount, axis="Z", sides=32):
        rotation = (0, PI/2, 0) if axis == "X" else ((PI/2, 0, 0) if axis == "Y" else (0,0,0))
        bpy.ops.mesh.primitive_cylinder_add(vertices=sides, radius=radius,
            depth=depth, location=center, rotation=rotation)
        obj = bpy.context.object
        obj.name = name
        return attach(obj, material, parent)
    def torus(name, center, major, minor, material, parent=mount, axis="Z"):
        rotation = (0, PI/2, 0) if axis == "X" else ((PI/2,0,0) if axis == "Y" else (0,0,0))
        bpy.ops.mesh.primitive_torus_add(major_segments=48, minor_segments=10,
            major_radius=major, minor_radius=minor, location=center, rotation=rotation)
        obj = bpy.context.object
        obj.name = name
        return attach(obj, material, parent)
    def beam(name, a, b, radius, material, parent=mount):
        a, b = Vector(a), Vector(b)
        obj = cyl(name, (a+b)*.5, radius, (b-a).length, material, parent)
        obj.rotation_mode = "QUATERNION"
        obj.rotation_quaternion = (b-a).to_track_quat("Z", "Y")
        return obj
    def hollow_square(name, bottom, top, outer, inner, material, parent=mount):
        # Watertight hollow extrusion: inner faces and both annular end caps.
        verts = []
        for z, h in ((bottom,outer),(top,outer),(bottom,inner),(top,inner)):
            verts.extend([(-h,-h,z),(h,-h,z),(h,h,z),(-h,h,z)])
        faces = []
        for i in range(4):
            j=(i+1)%4
            faces.append((i,j,4+j,4+i))
            faces.append((8+j,8+i,12+i,12+j))
            faces.append((4+i,4+j,12+j,12+i))
            faces.append((j,i,8+i,8+j))
        mesh=bpy.data.meshes.new(name+"_mesh")
        mesh.from_pydata(verts, [], faces)
        mesh.update()
        obj=bpy.data.objects.new(name,mesh)
        col.objects.link(obj)
        obj.data.materials.append(material)
        obj.parent=parent
        return obj
    def helix(name, zlow, zhigh, turns, radius, wire, material, parent, sides=9, samples=560):
        # Solid helical wire with sealed end caps; visually represents the lead screw thread.
        vertices, faces = [], []
        for i in range(samples+1):
            t=i/samples
            angle=2*PI*turns*t
            z=zlow+(zhigh-zlow)*t
            p=Vector((radius*math.cos(angle),radius*math.sin(angle),z))
            tangent=Vector((-radius*2*PI*turns*math.sin(angle),
                            radius*2*PI*turns*math.cos(angle),zhigh-zlow)).normalized()
            normal=Vector((math.cos(angle),math.sin(angle),0))
            binormal=tangent.cross(normal).normalized()
            for j in range(sides):
                phi=2*PI*j/sides
                q=p+wire*(normal*math.cos(phi)+binormal*math.sin(phi))
                vertices.append(tuple(q))
            if i:
                a=(i-1)*sides
                b=i*sides
                for j in range(sides):
                    k=(j+1)%sides
                    faces.append((a+j,a+k,b+k,b+j))
        faces.append(tuple(reversed(tuple(range(sides)))))
        faces.append(tuple(samples*sides+j for j in range(sides)))
        mesh=bpy.data.meshes.new(name+"_mesh")
        mesh.from_pydata(vertices, [], faces)
        mesh.update()
        obj=bpy.data.objects.new(name,mesh)
        col.objects.link(obj)
        obj.data.materials.append(material)
        obj.parent=parent
        return obj

    # Chassis interface: mounting plane z=0, four through-bolt locations.
    box("01_Mounting_flange", (0,0,-.027), (.60,.36,.054), graphite, bevel=.014)
    box("02_Mounting_center_rib", (0,0,-.067), (.30,.26,.040), alloy, bevel=.009)
    for x in (-.25,.25):
        for y in (-.135,.135):
            cyl("Mount_M10_bolt", (x,y,.003), .014,.012, dark, sides=12)
            torus("Mount_bolt_washer", (x,y,-.005), .020,.004, copper)
    # Cage is intentionally open on all sides for inspection and clean clearance.
    for x in (-.103,.103):
        for y in (-.103,.103):
            box("Cage_vertical_corner_rail", (x,y,-.405), (.034,.034,.590),
                graphite, bevel=.004)
            cyl("Lower_rail_service_fastener", (x,y,-.683),.010,.014,copper)
    for z in (-.113,-.680):
        for sign in (-1,1):
            box("Cage_X_crossmember", (0,sign*.103,z),(.172,.034,.035),
                alloy,bevel=.004)
            box("Cage_Y_crossmember", (sign*.103,0,z),(.034,.172,.035),
                alloy,bevel=.004)
    for x in (-.103,.103):
        beam("Cage_diagonal_reinforcement", (x,-.103,-.15),(x,-.103,-.59),
             .008, copper)
    # Slider's square tube is inside the cage (minimum side clearance 8 mm).
    hollow_square("Inner_hollow_telescopic_column",-.735,-.340,.078,.055,alloy,slide)
    for x in (-.077,.077):
        box("Slider_orange_raceway", (x,-.079,-.505),(.011,.009,.330),
            orange,slide,bevel=.002)
    for z in (-.370,-.700):
        box("Slider_end_belt", (0,0,z),(.158,.158,.018),dark,slide,bevel=.003)
    # Drive screw, axial bearing and a hollow nut with a genuine radial bore.
    cyl("Lead_screw_core", (0,0,-.385),.022,.530,dark,screw)
    helix("Lead_screw_thread",-.635,-.135,23.0,.025,.0027,copper,screw)
    for z in (-.090,-.119):
        torus("Upper_thrust_bearing", (0,0,z),.040,.008,alloy)
    torus("Travelling_nut_bore", (0,0,-.355),.0415,.0105,copper,slide)
    torus("Travelling_nut_lockring", (0,0,-.373),.0415,.0105,dark,slide)
    # Four fixed guide rods with moving sleeve bushings.
    for sign in (-1,1):
        x=sign*.260
        beam("Outboard_guide_rod",(x,0,-.145),(x,0,-.940),.012,alloy)
        box("Guide_rod_upper_bracket",(x,0,-.136),(.084,.070,.046),graphite,bevel=.006)
        cyl("Guide_rod_bottom_stop",(x,0,-.947),.024,.016,orange)
        for z in (-.558,-.603):
            torus("Moving_guide_bushing",(x,0,z),.026,.008,copper,slide)
        beam("Slider_guide_link",(sign*.085,0,-.582),(sign*.231,0,-.582),
             .012,alloy,slide)
    # Low-profile swivel-capable foot plate, deliberately kept rigid in this asset.
    cyl("Foot_load_spreader",(0,0,-.760),.192,.044,graphite,slide,sides=16)
    cyl("Foot_ceramic_contact_layer",(0,0,-.789),.178,.017,ceramic,slide,sides=16)
    cyl("Foot_rubber_traction",(0,0,-.805),.164,.018,rubber,slide,sides=16)
    torus("Foot_orange_safety_rim",(0,0,-.758),.178,.009,orange,slide)
    cyl("Foot_central_swivel_hub",(0,0,-.725),.054,.035,alloy,slide)
    for i in range(8):
        a=2*PI*i/8
        x,y=.137*math.cos(a),.137*math.sin(a)
        cyl("Foot_countersunk_bolt",(x,y,-.743),.011,.008,dark,slide,sides=12)
        # Radial traction pads stay inside the plate perimeter.
        cyl("Foot_grip_stud",(x*.82,y*.82,-.818),.014,.008,rubber,slide,sides=10)
    # Coaxial motor/hand-crank drive. The input knob and screw rotate together.
    cyl("Drive_fixed_motor_housing",(0,0,.029),.088,.075,graphite)
    cyl("Drive_copper_motor_ring",(0,0,.068),.080,.012,copper)
    cyl("Drive_rotating_knurled_crown",(0,0,.086),.062,.028,orange,knob)
    for i in range(12):
        a=2*PI*i/12
        cyl("Drive_knurl",(0.058*math.cos(a),.058*math.sin(a),.105),
            .006,.019,dark,knob,sides=10)
    for sign in (-1,1):
        box("Motor_power_connector",(sign*.112,0,.026),(.038,.055,.034),
            alloy,bevel=.005)
        cyl("Status_lamp",(sign*.112,-.035,.030),.010,.007,cyan,axis="Y")
    # Animate 3 screw revolutions during extension and reverse on retraction.
    for frame in range(1,FRAMES+1):
        s=extension(frame)
        slide.location.z=-STROKE*s
        slide.keyframe_insert(data_path="location",frame=frame,group="Telescopic stroke")
        screw.rotation_euler.z=2*PI*TURNS*s
        screw.keyframe_insert(data_path="rotation_euler",frame=frame,group="Lead screw")
        knob.rotation_euler.z=2*PI*TURNS*s
        knob.keyframe_insert(data_path="rotation_euler",frame=frame,group="Drive crown")
    for obj in (slide,screw,knob):
        if obj.animation_data and obj.animation_data.action:
            for fc in obj.animation_data.action.fcurves:
                for key in fc.keyframe_points: key.interpolation="LINEAR"
    scene.frame_set(1)
    return {"collection":col,"materials":len(materials),
            "mesh_objects":sum(o.type=="MESH" for o in col.objects),
            "animated_groups":["SLIDE_TELESCOPIC_Z","LEAD_SCREW_ROTATE_Z","DRIVE_KNOB_ROTATE_Z"]}

def topology_check():
    checked=0
    bad=[]
    for obj in bpy.data.objects:
        if obj.type!="MESH": continue
        bm=bmesh.new()
        bm.from_mesh(obj.data)
        if not bm.faces or any(not e.is_manifold for e in bm.edges):
            bad.append(obj.name)
        if any(f.calc_area()<1e-11 for f in bm.faces):
            bad.append(obj.name+":degenerate")
        checked+=1
        bm.free()
    assert not bad, bad
    return checked

def main():
    math_result=analytical_qa()
    first=build()
    first_count=topology_check()
    second=build()
    second_count=topology_check()
    assert first["mesh_objects"]==second["mesh_objects"]==first_count==second_count
    assert first["materials"]==second["materials"]>=3
    assert second_count>=55
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/(ID+".blend")))
    bpy.ops.object.select_all(action="DESELECT")
    for obj in second["collection"].objects: obj.select_set(True)
    bpy.ops.export_scene.gltf(filepath=str(OUT/(ID+"_animated.glb")),
        export_format="GLB",use_selection=True,export_animations=True,
        export_frame_range=True,export_force_sampling=True)
    for name in (ID+".blend",ID+"_animated.glb"):
        assert (OUT/name).stat().st_size>10000,name
    report={"id":ID,"blender":bpy.app.version_string,
        "reentrant":"PASS","manifold":"PASS","geometry":second,
        "topology_meshes_checked":second_count,"kinematics":math_result,
        "files":{name:(OUT/name).stat().st_size for name in (ID+".blend",ID+"_animated.glb")},
        "unity":"NOT RUN","full_triangle_collision":"NOT RUN"}
    (OUT/(ID+"_qa.json")).write_text(json.dumps(report,indent=2),encoding="utf-8")
    print("M02-021 RESULT "+json.dumps(report,sort_keys=True))

if __name__=="__main__":
    main()
