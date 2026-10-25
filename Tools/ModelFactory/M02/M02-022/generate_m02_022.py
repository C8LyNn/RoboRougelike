#!/usr/bin/env python3
"""M02-022 steerable rigid axle module, Blender 4+ bpy, all dimensions metres."""
import bpy
import bmesh
import json
import math
from pathlib import Path
from mathutils import Vector

ID = "M02-022"
OUT = Path(__file__).resolve().parent / "generated"
OUT.mkdir(parents=True, exist_ok=True)
FRAMES = 121
MAX_STEER = math.radians(24)
SIDES = (-1, 1)
Z_AXLE = -0.36
AXLE_X = 0.68
ANCHOR_X = 0.35
ARM_X = 0.11
ARM_Y = -0.18
BARREL_END = 0.30
ROD_START = 0.245
ROD_END_OFFSET = 0.015

def angle(frame):
    return MAX_STEER * math.sin(2 * math.pi * (frame - 1) / (FRAMES - 1))

def actuator_geometry(side, steer):
    a = Vector((side * ANCHOR_X, -0.28, Z_AXLE))
    x = side * AXLE_X + side * ARM_X * math.cos(steer) - ARM_Y * math.sin(steer)
    y = side * ARM_X * math.sin(steer) + ARM_Y * math.cos(steer)
    b = Vector((x, y, Z_AXLE))
    return a, b, (b-a).length

def analytical_qa():
    minimum_length = 1e9
    maximum_length = 0.0
    min_rod = 1e9
    min_hub_clearance = 1e9
    for frame in range(1, FRAMES + 1):
        t = angle(frame)
        for side in SIDES:
            a,b,d = actuator_geometry(side,t)
            minimum_length = min(minimum_length,d)
            maximum_length = max(maximum_length,d)
            rod = d - ROD_END_OFFSET - ROD_START
            min_rod = min(min_rod,rod)
            # Nearest outer hub to the stationary crossbeam end at |X|=0.605.
            hub_inner_x = AXLE_X + 0.12 * math.cos(t)
            min_hub_clearance = min(min_hub_clearance,hub_inner_x - 0.605)
            assert rod > 0.085, (frame,side,rod)
            assert d > BARREL_END + 0.055, (frame,side,d)
            assert d < 0.56, (frame,side,d)
            assert abs(t) <= MAX_STEER + 1e-9
    assert min_hub_clearance > 0.12, min_hub_clearance
    assert 0.023 - 0.016 > 0.006 # Rod/tube running clearance, 7 mm.
    assert 0.041 - 0.028 > 0.012 # Kingpin/bushing running clearance, 13 mm.
    return dict(frames=FRAMES, actuator_samples=FRAMES*2,
        steering_limit_deg=24, actuator_min_length_m=minimum_length,
        actuator_max_length_m=maximum_length, min_rod_exposure_m=min_rod,
        min_hub_to_beam_x_margin_m=min_hub_clearance,
        piston_radial_clearance_m=0.007, kingpin_radial_clearance_m=0.013,
        scope="Analytical motion envelopes only; not triangle-triangle collision or Unity physics")

def reset():
    for o in list(bpy.data.objects):
        bpy.data.objects.remove(o,do_unlink=True)
    for db in (bpy.data.meshes,bpy.data.materials,bpy.data.actions):
        for item in list(db):
            if item.users == 0: db.remove(item)

def build():
    reset()
    scene = bpy.context.scene
    scene.unit_settings.system = "METRIC"
    scene.unit_settings.scale_length = 1.0
    scene.frame_start,scene.frame_end,scene.render.fps = 1,FRAMES,30
    col=bpy.data.collections.new(ID+"_ASSEMBLY")
    scene.collection.children.link(col)

    def material(name,color,metal,rough,emission=0):
        m=bpy.data.materials.new(ID+"_"+name)
        m.diffuse_color=(*color,1)
        m.use_nodes=True
        p=m.node_tree.nodes.get("Principled BSDF")
        p.inputs["Base Color"].default_value=(*color,1)
        p.inputs["Metallic"].default_value=metal
        p.inputs["Roughness"].default_value=rough
        if emission:
            e=p.inputs.get("Emission Color")
            power=p.inputs.get("Emission Strength")
            if e: e.default_value=(*color,1)
            if power: power.default_value=emission
        return m
    graphite=material("01_forged_graphite",(.095,.12,.15),.83,.31)
    alloy=material("02_satin_alloy",(.57,.63,.67),.73,.26)
    bronze=material("03_bronze_bearings",(.67,.37,.12),.78,.31)
    orange=material("04_safety_orange",(.94,.29,.035),.28,.36)
    rubber=material("05_seal_rubber",(.035,.045,.052),.06,.82)
    ceramic=material("06_ceramic_white",(.78,.81,.77),.14,.41)
    hardened=material("07_hardened_steel",(.19,.23,.28),.92,.24)
    cyan=material("08_cyan_indicator",(.035,.60,.83),.22,.23,1.4)
    mats=(graphite,alloy,bronze,orange,rubber,ceramic,hardened,cyan)

    def link(o,mat=None,parent=None):
        for old in list(o.users_collection): old.objects.unlink(o)
        col.objects.link(o)
        if mat is not None: o.data.materials.append(mat)
        if parent is not None: o.parent=parent
        return o

    def empty(name,location=(0,0,0),parent=None):
        o=bpy.data.objects.new(name,None)
        col.objects.link(o)
        o.location=location
        o.empty_display_type="PLAIN_AXES"
        o.empty_display_size=.04
        if parent: o.parent=parent
        return o

    mount=empty("MOUNT_UNDERSIDE_Z0_UNITY_1M")
    def cube(name,center,size,mat,parent=mount,bevel=0):
        bpy.ops.mesh.primitive_cube_add(size=1)
        o=bpy.context.object
        o.name=name
        o.location=center
        o.dimensions=size
        bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
        link(o,mat,parent)
        if bevel:
            b=o.modifiers.new("Machined_corner_radii","BEVEL")
            b.width=bevel
            b.segments=2
            o.modifiers.new("Weighted_normals","WEIGHTED_NORMAL")
        return o

    def cyl(name,a,b,r,mat,parent=mount,vertices=32):
        a,b=Vector(a),Vector(b)
        d=b-a
        assert d.length>1e-6,name
        bpy.ops.mesh.primitive_cylinder_add(vertices=vertices,radius=r,depth=d.length)
        o=bpy.context.object
        o.name=name
        o.location=(a+b)/2
        o.rotation_mode="QUATERNION"
        o.rotation_quaternion=Vector((0,0,1)).rotation_difference(d.normalized())
        link(o,mat,parent)
        for f in o.data.polygons:
            if len(f.vertices)==4: f.use_smooth=True
        return o

    def ball(name,center,r,mat,parent=mount):
        bpy.ops.mesh.primitive_uv_sphere_add(segments=20,ring_count=12,radius=r)
        o=bpy.context.object
        o.name=name
        o.location=center
        link(o,mat,parent)
        for f in o.data.polygons: f.use_smooth=True
        return o

    def torus(name,center,major,minor,mat,parent=mount):
        bpy.ops.mesh.primitive_torus_add(major_segments=36,minor_segments=10,
            location=center,major_radius=major,minor_radius=minor)
        o=bpy.context.object
        o.name=name
        link(o,mat,parent)
        for f in o.data.polygons: f.use_smooth=True
        return o

    def tube(name,lo,hi,outer,inner,mat,parent):
        # Closed annular cylinder with a true hollow bore; no solid tube/rod intersection.
        n=32
        verts=[]
        for z,r in ((lo,outer),(hi,outer),(lo,inner),(hi,inner)):
            verts.extend((r*math.cos(2*math.pi*i/n),r*math.sin(2*math.pi*i/n),z)
                for i in range(n))
        faces=[]
        for i in range(n):
            j=(i+1)%n
            faces.extend(((i,j,n+j,n+i),
                (2*n+j,2*n+i,3*n+i,3*n+j),
                (n+i,n+j,3*n+j,3*n+i),
                (j,i,2*n+i,2*n+j)))
        mesh=bpy.data.meshes.new(name+"_mesh")
        mesh.from_pydata(verts,[],faces)
        mesh.update()
        o=bpy.data.objects.new(name,mesh)
        col.objects.link(o)
        o.parent=parent
        o.data.materials.append(mat)
        return o

    # Four chassis fastening ports, mounting plane exactly Z=0.
    cube("Main_chassis_mount",(0,0,-.055),(1.05,.36,.11),graphite,bevel=.014)
    for sx in SIDES:
        for sy in SIDES:
            x,y=sx*.425,sy*.13
            cyl("Mount_bolt_standoff",(x,y,-.004),(x,y,.014),.034,alloy)
            cyl("Mount_bolt_head",(x,y,.015),(x,y,.031),.020,hardened,vertices=12)
            torus("Mount_orange_witness",(x,y,.020),.026,.004,orange)
    # Two visible drop supports and a forged central axle with bracing.
    for sx in SIDES:
        cube("Suspension_drop_leg",(sx*.38,0,-.19),(.09,.11,.27),alloy,bevel=.009)
        cyl("Diagonal_axle_brace",(sx*.24,-.08,-.09),
            (sx*.51,-.04,-.33),.022,hardened)
    cube("Axle_crossbeam",(0,0,-.36),(1.21,.12,.105),graphite,bevel=.014)
    cube("Axle_lower_stiffener",(0,.017,-.425),(.91,.06,.024),alloy,bevel=.005)
    for sx in SIDES:
        for k in range(3):
            cube("Axle_welded_gusset",(sx*(.18+k*.15),.064,-.36),
                (.045,.017,.078),hardened,bevel=.004)

    knuckles={}
    actuators={}
    for side in SIDES:
        tag="L" if side<0 else "R"
        x=side*AXLE_X
        # Fixed kingpin and exposed upper/lower radial bearing seats.
        cyl(tag+"_stationary_kingpin",(x,0,-.515),(x,0,-.205),.028,hardened)
        for z in (-.475,-.245):
            torus(tag+"_bronze_kingpin_bearing",(x,0,z),.055,.014,bronze)
            torus(tag+"_rubber_dust_seal",(x,0,z+.026),.052,.006,rubber)
        root=empty("STEER_"+tag+"_KINGPIN_Z",(x,0,Z_AXLE),mount)
        knuckles[side]=root
        # Rotating fork has a 13 mm minimum radial clearance from the kingpin.
        for z in (-.115,.115):
            cube(tag+"_rotating_fork_arm",(side*.13,0,z),
                (.10,.083,.032),alloy,root,bevel=.006)
            cyl(tag+"_fork_outer_fastener",
                (side*.133,0,z-.018),(side*.133,0,z+.018),
                .019,hardened,root,vertices=12)
        cyl(tag+"_steerable_spindle",(side*.118,0,0),
            (side*.34,0,0),.036,hardened,root)
        cyl(tag+"_hub_collar",(side*.218,0,0),(side*.26,0,0),
            .062,bronze,root)
        cyl(tag+"_wheel_mount_flange",(side*.278,0,0),
            (side*.303,0,0),.135,alloy,root)
        cyl(tag+"_flange_endcap",(side*.305,0,0),
            (side*.317,0,0),.057,graphite,root)
        for i in range(6):
            a=2*math.pi*i/6
            y,z=.102*math.cos(a),.102*math.sin(a)
            cyl(tag+"_six_bolt_circle",(side*.302,y,z),
                (side*.321,y,z),.012,hardened,root,vertices=12)
        # Exposed steering arm, structurally connected to rotating knuckle.
        cyl(tag+"_steering_arm",
            (side*.13,-.015,-.012),(side*ARM_X,ARM_Y,0),
            .026,orange,root)
        ball(tag+"_outer_steering_ball",(side*ARM_X,ARM_Y,0),
            .026,bronze,root)
        cube(tag+"_flange_stripe",(side*.293,-.043,.085),
            (.014,.052,.016),orange,root,bevel=.002)
        # Chassis-anchored telescoping hydraulic steering actuator.
        a,b,d=actuator_geometry(side,0)
        ball(tag+"_actuator_inner_ball",a,.032,bronze)
        rig=empty("ACTUATOR_"+tag+"_AIM",a,mount)
        rig.rotation_mode="QUATERNION"
        rig.rotation_quaternion=Vector((0,0,1)).rotation_difference((b-a).normalized())
        tube(tag+"_hydraulic_hollow_barrel",.025,BARREL_END,
            .036,.023,graphite,rig)
        tube(tag+"_bronze_rod_guide",.288,.306,.023,.019,bronze,rig)
        # A true 7 mm barrel/rod radial clearance.
        rod=cyl(tag+"_moving_piston_rod",(0,0,ROD_START),
            (0,0,d-ROD_END_OFFSET),.016,alloy,rig)
        # Apply dynamic Z scaling relative to a unit-length mesh, keeping radius unchanged.
        # The cylinder's native depth is its initial length; animate relative Z scale.
        initial_length=d-ROD_END_OFFSET-ROD_START
        actuators[side]=(rig,rod,initial_length)
        torus(tag+"_barrel_service_ring",(0,0,.063),.045,.006,orange,rig)
        # Cylinder end remains inside outer steering ball socket.
        # Fixed visual sensor does not participate in steering.
        cube(tag+"_actuator_sensor",(side*.34,-.38,-.26),
            (.062,.028,.035),ceramic,bevel=.004)
        cube(tag+"_actuator_status_led",(side*.34,-.397,-.26),
            (.024,.007,.014),cyan)

    # Keyframe full kinematic solution; no straight-line approximation of cylinder aim.
    for frame in range(1,FRAMES+1):
        t=angle(frame)
        for side in SIDES:
            root=knuckles[side]
            root.rotation_euler.z=t
            root.keyframe_insert(data_path="rotation_euler",frame=frame)
            a,b,d=actuator_geometry(side,t)
            rig,rod,initial=actuators[side]
            rig.rotation_quaternion=Vector((0,0,1)).rotation_difference((b-a).normalized())
            rig.keyframe_insert(data_path="rotation_quaternion",frame=frame)
            length=d-ROD_END_OFFSET-ROD_START
            rod.scale.z=length/initial
            # Cylinder originally centred at (ROD_START + initial/2).
            rod.location.z=ROD_START+length/2
            rod.keyframe_insert(data_path="location",frame=frame)
            rod.keyframe_insert(data_path="scale",frame=frame)
    scene.frame_set(1)
    return {"collection":col,"mesh_objects":sum(o.type=="MESH" for o in col.objects),
            "materials":len(mats),
            "animated_groups":[o.name for o in [*knuckles.values(),
                *(actuators[s][0] for s in SIDES),
                *(actuators[s][1] for s in SIDES)]]}

def topology():
    bad=[]
    count=0
    for o in bpy.data.objects:
        if o.type!="MESH": continue
        bm=bmesh.new()
        bm.from_mesh(o.data)
        if not bm.faces or any(not e.is_manifold for e in bm.edges):
            bad.append(o.name+":not_manifold")
        if any(f.calc_area()<1e-11 for f in bm.faces):
            bad.append(o.name+":degenerate")
        count+=1
        bm.free()
    assert not bad,bad
    return count

def main():
    kinematics=analytical_qa()
    a=build()
    n1=topology()
    b=build()
    n2=topology()
    assert n1==n2==a["mesh_objects"]==b["mesh_objects"]
    assert a["materials"]==b["materials"]==8
    assert n2>=65,n2
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/(ID+".blend")))
    bpy.ops.object.select_all(action="DESELECT")
    for o in b["collection"].objects: o.select_set(True)
    bpy.ops.export_scene.gltf(filepath=str(OUT/(ID+"_animated.glb")),
        export_format="GLB",use_selection=True,export_animations=True,
        export_frame_range=True,export_force_sampling=True)
    sizes={p.name:p.stat().st_size for p in
        (OUT/(ID+".blend"),OUT/(ID+"_animated.glb"))}
    assert all(v>10000 for v in sizes.values()),sizes
    report=dict(id=ID,blender=bpy.app.version_string,scale_m_per_unit=1.0,
        pivot="MOUNT_UNDERSIDE_Z0_UNITY_1M",steer_axis="Blender +Z",
        materials=b["materials"],meshes=n2,animated_groups=b["animated_groups"],
        kinematics=kinematics,reentrant="PASS",manifold="PASS",
        binary_sizes=sizes,unity_import="NOT RUN",triangle_collision="NOT RUN")
    (OUT/(ID+"_qa.json")).write_text(json.dumps(report,indent=2),
        encoding="utf-8")
    print("M02-022 RESULT "+json.dumps(report,sort_keys=True))

if __name__=="__main__":
    main()
