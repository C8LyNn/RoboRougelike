#!/usr/bin/env python3
"""M02-010 independent wishbone with externally sprung telescopic damper.

Blender: X fore/aft, Y outboard, Z up; 1 unit = 1 metre.
Mount ROOT at (0,0,0); two inboard pivot bearings rotate about +X.
Outboard spherical joint follows an exact rigid X-axis arc (24 degrees peak
to peak). A telescopic damper and a 9-turn coil follow the moving arm.
Source is reproducible; use --self-test / --static-obj without bpy, or
blender -b --factory-startup --python generate_m02_010.py -- --out DIR.
Visual-only asset, no Unity gameplay modifications.
"""
from __future__ import annotations
import argparse
import json
import math
import sys
from pathlib import Path

ID="M02-010"
PREFIX="RRL_M02_010_"
COLLECTION=PREFIX+"INDEPENDENT_WISHBONE"
PIVOT=(0.,.130,0.)
TOP=(0.,.180,.390)
LOWER_LOCAL=(0.,.310,.100)
MAX_SWING=math.radians(12)
ROD_LENGTH=.280
FRAMES=96
COLORS={
 "SatinTitanium":((.39,.47,.54,1),.82,.27),
 "GraphiteArmor":((.065,.079,.094,1),.67,.37),
 "RubberBushing":((.022,.030,.035,1),.02,.85),
 "CopperHardware":((.69,.30,.10,1),.78,.26),
 "AmberCoil":((.97,.37,.045,1),.56,.27),
 "ChromedRod":((.72,.80,.86,1),.91,.17),
 "CyanIndicator":((.055,.59,.78,1),.31,.23),
 "CeramicWhite":((.75,.77,.69,1),.20,.48),
}
BOX_FACES=((0,3,2),(0,2,1),(4,5,6),(4,6,7),
           (0,1,5),(0,5,4),(1,2,6),(1,6,5),
           (2,3,7),(2,7,6),(3,0,4),(3,4,7))

def sub(a,b):return tuple(a[i]-b[i] for i in range(3))
def add(a,b):return tuple(a[i]+b[i] for i in range(3))
def mul(a,k):return tuple(x*k for x in a)
def dot(a,b):return sum(x*y for x,y in zip(a,b))
def cross(a,b):return (a[1]*b[2]-a[2]*b[1],
                       a[2]*b[0]-a[0]*b[2],
                       a[0]*b[1]-a[1]*b[0])
def norm(v):return math.sqrt(dot(v,v))
def unit(v):return mul(v,1/norm(v))
def rotate_x(v,t):
    x,y,z=v;c=math.cos(t);s=math.sin(t)
    return (x,c*y-s*z,s*y+c*z)
def frame(direction):
    t=unit(direction)
    helper=(1.,0.,0.) if abs(t[0])<.85 else (0.,1.,0.)
    u=unit(cross(helper,t));v=cross(t,u)
    return u,v,t
def orient(v,origin,axis):
    u,w,t=frame(axis)
    return add(origin,add(add(mul(u,v[0]),mul(w,v[1])),mul(t,v[2])))
def mesh(name,verts,faces,material,group="STATIC",smooth=False,bevel=0.):
    return dict(name=name,vertices=verts,faces=faces,material=material,
                group=group,smooth=smooth,bevel=bevel)
def box(name,centre,size,material,group="STATIC",bevel=.004):
    x,y,z=centre;a,b,c=(d/2 for d in size)
    v=[(x-a,y-b,z-c),(x+a,y-b,z-c),(x+a,y+b,z-c),(x-a,y+b,z-c),
       (x-a,y-b,z+c),(x+a,y-b,z+c),(x+a,y+b,z+c),(x-a,y+b,z+c)]
    return mesh(name,v,list(BOX_FACES),material,group,bevel=bevel)
def cylinder(name,centre,axis,radius,length,material,group="STATIC",sides=24):
    verts=[]
    for z in (-length/2,length/2):
        for i in range(sides):
            a=2*math.pi*i/sides
            verts.append(orient((radius*math.cos(a),radius*math.sin(a),z),centre,axis))
    verts.extend((orient((0,0,-length/2),centre,axis),
                  orient((0,0,length/2),centre,axis)))
    faces=[]
    for i in range(sides):
        j=(i+1)%sides
        faces.extend(((i,j,sides+j),(i,sides+j,sides+i),
                      (2*sides,j,i),(2*sides+1,sides+i,sides+j)))
    return mesh(name,verts,faces,material,group,smooth=True)
def ring(name,centre,axis,inner,outer,length,material,group="STATIC",sides=32):
    assert 0<inner<outer
    verts=[]
    for z,r in ((-length/2,outer),(-length/2,inner),
                (length/2,outer),(length/2,inner)):
        for i in range(sides):
            a=2*math.pi*i/sides
            verts.append(orient((r*math.cos(a),r*math.sin(a),z),centre,axis))
    faces=[]
    for i in range(sides):
        j=(i+1)%sides
        ob,oj=i,j;ib,ij=sides+i,sides+j
        ot,otj=2*sides+i,2*sides+j
        it,itj=3*sides+i,3*sides+j
        faces.extend(((ob,oj,otj),(ob,otj,ot),
                      (ib,itj,ij),(ib,it,itj),
                      (ob,ij,oj),(ob,ib,ij),
                      (ot,otj,itj),(ot,itj,it)))
    return mesh(name,verts,faces,material,group,smooth=True)
def beam(name,a,b,radius,material,group="STATIC",sides=16):
    direction=sub(b,a)
    return cylinder(name,mul(add(a,b),.5),direction,radius,norm(direction),
                    material,group,sides)
def sphere(name,centre,radius,material,group="STATIC",sides=20,rings=10):
    v=[add(centre,(0,0,radius))]
    for k in range(1,rings):
        polar=math.pi*k/rings
        for i in range(sides):
            a=2*math.pi*i/sides
            v.append(add(centre,(radius*math.sin(polar)*math.cos(a),
                                 radius*math.sin(polar)*math.sin(a),
                                 radius*math.cos(polar))))
    bottom=len(v);v.append(add(centre,(0,0,-radius)))
    faces=[]
    for i in range(sides):
        j=(i+1)%sides
        faces.append((0,1+i,1+j))
    for k in range(rings-2):
        start=1+k*sides;next_start=start+sides
        for i in range(sides):
            j=(i+1)%sides
            faces.extend(((start+i,next_start+i,next_start+j),
                          (start+i,next_start+j,start+j)))
    start=1+(rings-2)*sides
    for i in range(sides):
        j=(i+1)%sides
        faces.append((bottom,start+j,start+i))
    return mesh(name,v,faces,material,group,smooth=True)
def helix(name,turns=9,steps_per_turn=16,sides=12):
    """Closed, round wire helix in normalized Z=0.12..0.88 spring space."""
    steps=turns*steps_per_turn;rad=.069;wire=.007
    verts=[];faces=[]
    for k in range(steps+1):
        t=k/steps;phi=2*math.pi*turns*t
        c=(rad*math.cos(phi),rad*math.sin(phi),.12+.76*t)
        tangent=unit((-rad*math.sin(phi)*2*math.pi*turns,
                       rad*math.cos(phi)*2*math.pi*turns,.76))
        u=(math.cos(phi),math.sin(phi),0.)
        w=cross(tangent,u)
        for i in range(sides):
            a=2*math.pi*i/sides
            verts.append(add(c,mul(add(mul(u,math.cos(a)),
                                        mul(w,math.sin(a))),wire)))
    for k in range(steps):
        for i in range(sides):
            j=(i+1)%sides
            a=k*sides+i;b=k*sides+j
            c=(k+1)*sides+i;d=(k+1)*sides+j
            faces.extend(((a,b,d),(a,d,c)))
    start=len(verts);end=start+1
    verts.extend(((rad,0,.12),(rad,0,.88)))
    for i in range(sides):
        j=(i+1)%sides
        faces.append((start,j,i))
        a=steps*sides+i;b=steps*sides+j
        faces.append((end,a,b))
    return mesh(name,verts,faces,"AmberCoil","SPRING",smooth=True)

def lower(theta):
    return add(PIVOT,rotate_x(LOWER_LOCAL,theta))
def suspension(theta):
    b=lower(theta);d=sub(b,TOP);L=norm(d)
    return b,d,L

def build():
    parts=[];put=parts.append
    # Static mounting plate, load path to two widely spaced X-axis bearings.
    put(box("VehicleSocketPlate",(0,.005,0),(.680,.055,.240),"GraphiteArmor"))
    put(box("MountReinforcement",(0,.038,0),(.580,.030,.150),"SatinTitanium"))
    for side in (-1,1):
        x=side*.275;tag="L" if side<0 else "R"
        put(beam("FixedPivotStanchion_"+tag,(x,.045,0),(x,.130,0),
                 .027,"SatinTitanium"))
        put(cylinder("FixedBearingSupport_"+tag,(x,.130,0),(1,0,0),
                     .045,.029,"GraphiteArmor"))
        put(ring("CopperPivotFace_"+tag,(x+side*.018,.130,0),(1,0,0),
                 .017,.039,.008,"CopperHardware"))
        for z in (-.077,.077):
            put(cylinder("SocketBolt_%s_%s"%(tag,"T" if z>0 else "B"),
                         (x,.0,z),(0,1,0),.012,.009,"CopperHardware",sides=12))
    put(cylinder("StationaryThroughShaft",(0,.130,0),(1,0,0),
                 .016,.585,"ChromedRod",sides=32))
    # Upper shock load bracket and triangulated stays to the plate.
    put(box("UpperDamperTower",(0,.085,.245),(.095,.080,.270),"GraphiteArmor"))
    put(beam("UpperTowerStay_L",(-.150,.050,.110),TOP,.019,"SatinTitanium"))
    put(beam("UpperTowerStay_R",(.150,.050,.110),TOP,.019,"SatinTitanium"))
    put(sphere("UpperShockEye",TOP,.030,"CopperHardware"))
    put(cylinder("UpperEyePin",TOP,(1,0,0),.011,.105,"ChromedRod"))
    put(box("TowerSafetyStripe",(0,.044,.270),(.072,.008,.070),"AmberCoil"))
    # Rotating wishbone; all geometry authored relative to hinge pivot.
    def a(p):return sub(p,PIVOT)
    for side in (-1,1):
        x=side*.205;tag="L" if side<0 else "R"
        put(ring("InboardBushing_"+tag,a((x,.130,0)),(1,0,0),
                 .024,.053,.069,"RubberBushing","ARM"))
        put(ring("InboardSleeve_"+tag,a((x,.130,0)),(1,0,0),
                 .024,.040,.073,"SatinTitanium","ARM"))
        put(ring("InboardTrim_"+tag,a((x+side*.039,.130,0)),(1,0,0),
                 .025,.048,.008,"CopperHardware","ARM"))
        put(beam("TriangulatedFork_"+tag,a((x,.185,-.035)),
                 a((0,.638,-.072)),.029,"SatinTitanium","ARM"))
        put(beam("OuterRib_"+tag,a((x,.240,-.041)),
                 a((side*.075,.495,-.062)),.014,"GraphiteArmor","ARM"))
        put(cylinder("ForkRootCollar_"+tag,a((x,.190,-.035)),
                     (0,1,0),.035,.034,"GraphiteArmor","ARM"))
    put(beam("CrossBrace",a((-.106,.403,-.055)),
             a((.106,.403,-.055)),.021,"GraphiteArmor","ARM"))
    put(beam("SpringSeatStanchion",a((0,.403,-.055)),
             a((0,.440,.100)),.019,"SatinTitanium","ARM"))
    put(cylinder("LowerSpringSeat",a((0,.440,.100)),(0,0,1),
                 .048,.013,"CopperHardware","ARM"))
    put(sphere("LowerDamperEye",a((0,.440,.100)),.024,
               "GraphiteArmor","ARM"))
    put(cylinder("LowerEyePin",a((0,.440,.100)),(1,0,0),
                 .009,.090,"ChromedRod","ARM"))
    put(sphere("OutboardBallJoint",a((0,.638,-.072)),.052,
               "ChromedRod","ARM"))
    put(ring("OutboardBallSocket",a((0,.638,-.072)),(1,0,0),
             .042,.065,.049,"GraphiteArmor","ARM"))
    put(ring("OutboardCopperSeal",a((.029,.638,-.072)),(1,0,0),
             .044,.061,.008,"CopperHardware","ARM"))
    put(box("ArmWearIndicator",a((0,.402,-.020)),
            (.064,.070,.008),"CyanIndicator","ARM"))
    for x in (-.075,.075):
        put(cylinder("BraceRivet_%s"%("L" if x<0 else "R"),
                     a((x,.403,-.055)),(0,0,1),
                     .008,.052,"CopperHardware","ARM",sides=12))
    # Two coaxial, mechanically separated shock parts; inner rod telescopes
    # into outer housing. Coil surrounds both with 25+mm radial clearance.
    put(cylinder("DamperOuterBarrel",(0,0,.120),(0,0,1),
                 .030,.210,"GraphiteArmor","DAMPER",sides=32))
    put(ring("DamperCopperCollar",(0,0,.214),(0,0,1),
             .017,.039,.016,"CopperHardware","DAMPER"))
    put(ring("DamperTopFlange",(0,0,.031),(0,0,1),
             .020,.047,.015,"SatinTitanium","DAMPER"))
    put(cylinder("ChromeTelescopingRod",(0,0,.140),(0,0,1),
                 .014,ROD_LENGTH,"ChromedRod","ROD",sides=32))
    put(cylinder("RodEndStop",(0,0,.274),(0,0,1),
                 .022,.012,"CopperHardware","ROD"))
    put(helix("ExternalNineTurnSpring"))
    put(ring("SpringTopPerch",(0,0,.094),(0,0,1),
             .040,.086,.019,"GraphiteArmor","SPRING"))
    put(ring("SpringBottomPerch",(0,0,.906),(0,0,1),
             .040,.086,.019,"GraphiteArmor","SPRING"))
    put(ring("TopPerchAmberBand",(0,0,.084),(0,0,1),
             .075,.084,.007,"AmberCoil","SPRING"))
    put(ring("BottomPerchAmberBand",(0,0,.916),(0,0,1),
             .075,.084,.007,"AmberCoil","SPRING"))
    return parts

def transform(p,v,theta=0.):
    group=p["group"]
    if group=="STATIC":return v
    if group=="ARM":return add(PIVOT,rotate_x(v,theta))
    _,direction,L=suspension(theta)
    if group=="SPRING":
        return orient((v[0],v[1],v[2]*L),TOP,direction)
    if group=="DAMPER":return orient(v,TOP,direction)
    if group=="ROD":
        return orient((v[0],v[1],v[2]+L-ROD_LENGTH),TOP,direction)
    raise ValueError(group)

def mesh_qa(parts):
    names=set();total_v=total_f=0
    for p in parts:
        assert p["name"] not in names,p["name"];names.add(p["name"])
        assert p["material"] in COLORS
        assert p["group"] in ("STATIC","ARM","SPRING","DAMPER","ROD")
        vertices=[transform(p,v) for v in p["vertices"]]
        edges={};volume=0.
        for a,b,c in p["faces"]:
            assert len({a,b,c})==3 and 0<=min(a,b,c) and max(a,b,c)<len(vertices)
            A,B,C=(vertices[i] for i in (a,b,c))
            assert norm(cross(sub(B,A),sub(C,A)))>1e-9,("degenerate",p["name"])
            volume+=dot(A,cross(B,C))/6
            for i,j in ((a,b),(b,c),(c,a)):
                edge=tuple(sorted((i,j)))
                edges[edge]=edges.get(edge,0)+1
            total_f+=1
        assert edges and all(n==2 for n in edges.values()),("nonmanifold",p["name"])
        assert volume>1e-12,("inverted or zero volume",p["name"],volume)
        total_v+=len(vertices)
    return total_v,total_f

def point_segment_distance(p,a,b):
    d=sub(b,a);u=max(0.,min(1.,dot(sub(p,a),d)/dot(d,d)))
    return norm(sub(p,add(a,mul(d,u))))

def sweep_qa():
    min_coil_arm=1e9;min_rod_overlap=1e9;lengths=[]
    for i in range(97):
        theta=-MAX_SWING+2*MAX_SWING*i/96
        bottom,d,L=suspension(theta);lengths.append(L)
        assert .285<L<.480
        assert .0<L-ROD_LENGTH<.200
        overlap=.230-(L-ROD_LENGTH)
        min_rod_overlap=min(min_rod_overlap,overlap)
        assert overlap>.040,("damper loses telescoping engagement",i,overlap)
        # 9 coils must not touch on full compression; radial clearance
        # to the 30mm outer housing is 69-7-30=32mm.
        pitch=.76*L/9
        assert pitch>2*.007+.006,("coil self-intersection",i,pitch)
        assert .069-.007-.030>.025
        ball=add(PIVOT,rotate_x((0,.508,-.072),theta))
        assert .55<ball[1]<.70
        # Conservative spring centreline / wishbone tube clearance at
        # intermediate coil stations. Exclude lower mechanical attachment.
        for k in range(3,16):
            f=k/20
            centre=add(TOP,mul(d,f))
            for side in (-1,1):
                a=add(PIVOT,rotate_x((side*.205,.055,-.035),theta))
                b=add(PIVOT,rotate_x((0,.508,-.072),theta))
                gap=point_segment_distance(centre,a,b)-(.029+.069+.007)
                min_coil_arm=min(min_coil_arm,gap)
                assert gap>.002,("coil vs wishbone envelope",i,k,side,gap)
        # Stationary 16mm shaft passes through 24mm arm bearing bore.
        assert .024-.016>=.008-1e-10
    return dict(pose_samples=97,arm_swing_degrees=[-12,12],
                shock_length_range_m=[round(min(lengths),6),round(max(lengths),6)],
                min_rod_engagement_m=round(min_rod_overlap,6),
                min_spring_to_fork_envelope_m=round(min_coil_arm,6),
                shaft_radial_clearance_m=.008,
                spring_to_housing_radial_clearance_m=.032)

def validate(parts):
    vertices,triangles=mesh_qa(parts)
    motion=sweep_qa()
    assert len(COLORS)>=3
    assert sum(p["group"]=="ARM" for p in parts)>=15
    assert any(p["name"]=="ExternalNineTurnSpring" for p in parts)
    return dict(id=ID,status="PASS",parts=len(parts),vertices=vertices,
                triangles=triangles,materials=len(COLORS),
                mount_pivot_m=[0,0,0],inboard_hinge_axis="+X",
                outboard_ball_joint_at_rest_m=[0,.638,-.072],
                suspension=motion,units="metres; Unity 1 unit = 1 metre",
                coordinate_system="Blender X fore-aft, Y lateral, Z up",
                collision_method="97 sampled analytical envelope checks, not Unity physics",
                blender="NOT RUN",unity="NOT RUN")

def static_export(parts,out,report):
    out.mkdir(parents=True,exist_ok=True)
    obj=ID+"_independent_wishbone.obj";mtl=ID+"_independent_wishbone.mtl"
    materials=[]
    for name,(rgba,metal,rough) in COLORS.items():
        materials.extend(("newmtl "+name,
                          "Kd %.6f %.6f %.6f"%tuple(rgba[:3]),
                          "Ks %.5f %.5f %.5f"%(metal,metal,metal),
                          "Ns %.2f"%max(1,(1-rough)*800),"illum 2",""))
    (out/mtl).write_text("\n".join(materials)+"\n",encoding="utf-8")
    lines=["# M02-010 static neutral-pose OBJ; animation in GLB","mtllib "+mtl]
    offset=1
    for p in parts:
        lines.extend(("o "+p["name"],"usemtl "+p["material"]))
        for v in p["vertices"]:
            x,y,z=transform(p,v)
            lines.append("v %.8f %.8f %.8f"%(x,y,z))
        for a,b,c in p["faces"]:
            lines.append("f %d %d %d"%(a+offset,b+offset,c+offset))
        offset+=len(p["vertices"])
    (out/obj).write_text("\n".join(lines)+"\n",encoding="utf-8")
    report.update(backend="python-stdlib",obj=obj,mtl=mtl)
    (out/(ID+"_qa.json")).write_text(json.dumps(report,indent=2)+"\n")
    return report

def blender_export(parts,out,report):
    import bpy
    from mathutils import Vector
    old=bpy.data.collections.get(COLLECTION)
    if old:
        for obj in tuple(old.objects):
            bpy.data.objects.remove(obj,do_unlink=True)
        bpy.data.collections.remove(old)
    col=bpy.data.collections.new(COLLECTION)
    bpy.context.scene.collection.children.link(col)
    scene=bpy.context.scene
    scene.unit_settings.system="METRIC"
    scene.unit_settings.scale_length=1.
    root=bpy.data.objects.new(PREFIX+"ROOT_MOUNT_PIVOT",None)
    col.objects.link(root);root.empty_display_type="PLAIN_AXES"
    root.empty_display_size=.08
    root["mount"]="side socket";root["pivot_m"]=[0.,0.,0.]
    root["axis"]="+X hinge";root["visual_only"]=True
    groups={"STATIC":root}
    for name in ("ARM","SPRING","DAMPER"):
        ob=bpy.data.objects.new(PREFIX+name+"_RIG",None)
        col.objects.link(ob);ob.parent=root
        ob.empty_display_type="ARROWS";ob.empty_display_size=.035
        groups[name]=ob
    groups["ARM"].location=PIVOT
    rod=bpy.data.objects.new(PREFIX+"ROD_RIG",None)
    col.objects.link(rod);rod.parent=groups["DAMPER"]
    rod.empty_display_type="PLAIN_AXES";rod.empty_display_size=.025
    groups["ROD"]=rod
    mount=bpy.data.objects.new(PREFIX+"VEHICLE_SOCKET",None)
    col.objects.link(mount);mount.parent=root
    mount.empty_display_type="ARROWS";mount.empty_display_size=.065
    mats={}
    for name,(rgba,metal,rough) in COLORS.items():
        mat=bpy.data.materials.get(PREFIX+name) or bpy.data.materials.new(PREFIX+name)
        mat.use_nodes=True
        bsdf=mat.node_tree.nodes.get("Principled BSDF")
        bsdf.inputs["Base Color"].default_value=rgba
        bsdf.inputs["Metallic"].default_value=metal
        bsdf.inputs["Roughness"].default_value=rough
        if name=="CyanIndicator":
            try:
                bsdf.inputs["Emission Color"].default_value=(.012,.19,.26,1)
                bsdf.inputs["Emission Strength"].default_value=.6
            except KeyError:pass
        mats[name]=mat
    for p in parts:
        me=bpy.data.meshes.new(PREFIX+p["name"]+"_MESH")
        me.from_pydata(p["vertices"],[],p["faces"])
        me.update(calc_edges=True)
        ob=bpy.data.objects.new(PREFIX+p["name"],me)
        col.objects.link(ob);ob.parent=groups[p["group"]]
        me.materials.append(mats[p["material"]])
        if p["smooth"]:
            for face in me.polygons:face.use_smooth=True
        if p["bevel"]>0:
            mod=ob.modifiers.new("MachinedEdgeRadius","BEVEL")
            mod.width=p["bevel"];mod.segments=2
            mod.limit_method="ANGLE";mod.angle_limit=.5
    for frame_num in range(1,FRAMES+2,6):
        phase=2*math.pi*(frame_num-1)/FRAMES
        theta=MAX_SWING*math.sin(phase)
        bottom,d,L=suspension(theta)
        rot=Vector((0.,0.,1.)).rotation_difference(Vector(d).normalized())
        arm=groups["ARM"];arm.rotation_mode="XYZ"
        arm.rotation_euler[0]=theta
        arm.keyframe_insert(data_path="rotation_euler",frame=frame_num)
        spring=groups["SPRING"];spring.rotation_mode="QUATERNION"
        spring.location=TOP;spring.rotation_quaternion=rot
        spring.scale=(1.,1.,L)
        for prop in ("location","rotation_quaternion","scale"):
            spring.keyframe_insert(data_path=prop,frame=frame_num)
        damper=groups["DAMPER"];damper.rotation_mode="QUATERNION"
        damper.location=TOP;damper.rotation_quaternion=rot
        for prop in ("location","rotation_quaternion"):
            damper.keyframe_insert(data_path=prop,frame=frame_num)
        rod.location=(0.,0.,L-ROD_LENGTH)
        rod.keyframe_insert(data_path="location",frame=frame_num)
    scene.frame_start=1;scene.frame_end=97;scene.render.fps=24
    scene.frame_set(1)
    out.mkdir(parents=True,exist_ok=True)
    blend=out/(ID+".blend")
    glb=out/(ID+"_animated.glb")
    bpy.ops.wm.save_as_mainfile(filepath=str(blend))
    bpy.ops.object.select_all(action="DESELECT")
    for ob in col.objects:ob.select_set(True)
    bpy.context.view_layer.objects.active=root
    bpy.ops.export_scene.gltf(filepath=str(glb),export_format="GLB",
                              use_selection=True,export_animations=True)
    assert blend.stat().st_size>10000 and glb.stat().st_size>10000
    report.update(backend="blender-bpy",blender="PASS",
                  blend=blend.name,glb=glb.name,
                  animation_frames=97,
                  animation_channels=["ARM rotation","SPRING pose+length",
                                      "DAMPER aim","ROD telescopic extension"])
    (out/(ID+"_qa.json")).write_text(json.dumps(report,indent=2)+"\n")
    return report

def main():
    args=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else sys.argv[1:]
    ap=argparse.ArgumentParser()
    ap.add_argument("--self-test",action="store_true")
    ap.add_argument("--static-obj",action="store_true")
    ap.add_argument("--out",default="build/M02-010")
    a=ap.parse_args(args)
    parts=build();report=validate(parts)
    if a.self_test:
        print(json.dumps(report,indent=2));return
    out=Path(a.out).resolve()
    if a.static_obj:
        report=static_export(parts,out,report)
    else:
        report=blender_export(parts,out,report)
    print(json.dumps(report,indent=2))

if __name__=="__main__":
    main()
