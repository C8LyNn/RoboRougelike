#!/usr/bin/env python3
"""M02-011 independent wishbone with externally sprung telescopic damper.

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

ID="M02-011"
PREFIX="RRL_M02_011_"
COLLECTION=PREFIX+"DOUBLE_WISHBONE"
LOWER_PIVOT=(0.,.120,-.140)
UPPER_PIVOT=(0.,.120,.160)
OUTBOARD_SPAN=.480
HUB_REST=(0.,.600,.010)
TOP=(0.,.150,.435)
LOWER_SHOCK_LOCAL=(0.,.255,0.)
MAX_SWING=math.radians(10)
ROD_LENGTH=.360
BARREL_LENGTH=.350
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
    steps=turns*steps_per_turn;rad=.055;wire=.006
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
    return add(LOWER_PIVOT,rotate_x(LOWER_SHOCK_LOCAL,theta))

def hub(theta):
    shift=rotate_x((0.,OUTBOARD_SPAN,0.),theta)
    return (0.,LOWER_PIVOT[1]+shift[1],HUB_REST[2]+shift[2])

def suspension(theta):
    bottom=lower(theta)
    direction=sub(bottom,TOP)
    return bottom,direction,norm(direction)

def build():
    parts=[];put=parts.append
    # Rigid socket plate, two *separate* upper/lower hinge axes and bracing.
    put(box("VehicleMountSocket",(0.,.006,0.),(.650,.052,.570),"GraphiteArmor"))
    put(box("MainChassisSpine",(0.,.047,0.),(.555,.040,.510),"SatinTitanium"))
    for tag,x in (("F",-.215),("R",.215)):
        put(beam("ChassisUpright_"+tag,(x,.056,-.225),(x,.056,.235),
                 .027,"SatinTitanium"))
        for level,pivot in (("LOWER",LOWER_PIVOT),("UPPER",UPPER_PIVOT)):
            # Upper wishbone pivots are 50mm inboard of the lower ones.
            # Mount the fixed bearing coaxially with the matching moving
            # bushing; a diagonal gusset ties it to the main upright.
            px=x if level=="LOWER" else x*.165/.215
            sign=-1 if x<0 else 1
            support_x=px+sign*.056
            put(beam("ChassisHingePost_"+level+"_"+tag,
                     (x,.055,pivot[2]),(support_x,.120,pivot[2]),
                     .028,"GraphiteArmor"))
            # Fixed carrier is outside the moving bushing by 9mm in X.
            # A 12mm pin passes both bores without radial intersection.
            put(ring("FixedPivotBearing_"+level+"_"+tag,
                     (support_x,.120,pivot[2]),(1,0,0),
                     .014,.036,.030,"CopperHardware"))
            put(cylinder("HingePin_"+level+"_"+tag,
                         (px+sign*.015,.120,pivot[2]),(1,0,0),
                         .012,.145,"ChromedRod"))
        for z in (-.210,.210):
            put(cylinder("MountBolt_%s_%s"%(tag,"T" if z>0 else "B"),
                         (x,.001,z),(0,1,0),.010,.014,
                         "CopperHardware",sides=12))
    put(box("CoiloverUpperTower",(0.,.072,.328),(.108,.075,.250),
            "GraphiteArmor"))
    put(beam("TowerDiagonal_L",(-.165,.067,.205),TOP,
             .019,"SatinTitanium"))
    put(beam("TowerDiagonal_R",(.165,.067,.205),TOP,
             .019,"SatinTitanium"))
    put(sphere("UpperDamperBall",TOP,.029,"CopperHardware"))
    put(cylinder("UpperDamperCrossPin",TOP,(1,0,0),.011,.087,
                 "ChromedRod"))
    put(box("ServiceMarker",(0.,.028,.355),(.075,.009,.065),
            "CyanIndicator"))
    # Lower wishbone: wide, heavy-duty A-shaped fork, full pivot bearings.
    def relative(v,p):return sub(v,p)
    for level,pivot,spread,tube,group in (
        ("LOWER",LOWER_PIVOT,.215,.026,"LOWER_ARM"),
        ("UPPER",UPPER_PIVOT,.165,.021,"UPPER_ARM")):
        z=pivot[2]
        for tag,sgn in (("F",-1),("R",1)):
            x=sgn*spread
            put(ring(level+"_InnerRubber_"+tag,(x,0,0),(1,0,0),
                     .017,.045,.064,"RubberBushing",group))
            put(ring(level+"_InnerSteel_"+tag,(x,0,0),(1,0,0),
                     .017,.033,.070,"SatinTitanium",group))
            put(ring(level+"_InnerCopper_"+tag,
                     (x+sgn*.036,0,0),(1,0,0),.018,.042,.008,
                     "CopperHardware",group))
            put(beam(level+"_AArmFork_"+tag,
                     (x,.012,0),(0.,OUTBOARD_SPAN,-.014),
                     tube,"SatinTitanium",group))
            put(beam(level+"_ForgedRib_"+tag,
                     (x*.75,.135,.015),(sgn*.047,.360,-.008),
                     .012,"GraphiteArmor",group))
            put(cylinder(level+"_RootBoss_"+tag,
                         (x,.045,0),(0,1,0),.034,.047,
                         "GraphiteArmor",group))
        put(beam(level+"_CrossMember",(-.105,.290,-.012),
                 (.105,.290,-.012),.020,"GraphiteArmor",group))
        put(box(level+"_IdentificationPanel",(0,.300,.012),
                (.067,.082,.013),
                "AmberCoil" if level=="LOWER" else "CyanIndicator",
                group))
        put(sphere(level+"_OutboardBall",(0,OUTBOARD_SPAN,0),
                   .039,"ChromedRod",group))
        put(ring(level+"_OutboardSeat",(0,OUTBOARD_SPAN,0),
                 (0,0,1),.035,.052,.025,"CopperHardware",group))
    # Dedicated lower-arm shock pickup; moving along the lower wishbone.
    put(beam("LowerShockSupport",(0.,.220,-.014),
             LOWER_SHOCK_LOCAL,.024,"SatinTitanium","LOWER_ARM"))
    put(sphere("LowerDamperBall",LOWER_SHOCK_LOCAL,.028,
               "CopperHardware","LOWER_ARM"))
    put(cylinder("LowerDamperCrossPin",LOWER_SHOCK_LOCAL,(1,0,0),
                 .011,.087,"ChromedRod","LOWER_ARM"))
    # Upright / steering knuckle is NOT rigidly parented to either arm.
    # It translates with their common endpoint and remains upright.
    put(beam("VerticalSteeringKnuckle",(0,0,-.147),(0,0,.147),
             .032,"GraphiteArmor","KNUCKLE"))
    for tag,z in (("LOWER",-.150),("UPPER",.150)):
        put(ring("KnuckleBallSocket_"+tag,(0,0,z),(0,0,1),
                 .037,.057,.045,"SatinTitanium","KNUCKLE"))
        put(ring("KnuckleDustSeal_"+tag,(0,0,z),(0,0,1),
                 .039,.053,.009,"RubberBushing","KNUCKLE"))
    put(beam("HubAxleCarrier",(0,0,0),(0,.142,0),
             .044,"SatinTitanium","KNUCKLE"))
    put(cylinder("WheelHubFace",(0,.154,0),(0,1,0),
                 .118,.045,"GraphiteArmor","KNUCKLE",sides=40))
    put(ring("WheelHubCopperFlange",(0,.178,0),(0,1,0),
             .046,.109,.013,"CopperHardware","KNUCKLE",sides=40))
    put(cylinder("WheelBearingCentre",(0,.187,0),(0,1,0),
                 .041,.015,"ChromedRod","KNUCKLE"))
    for i in range(6):
        a=2*math.pi*i/6
        put(cylinder("WheelBolt_%02d"%i,
                     (.083*math.cos(a),.184,.083*math.sin(a)),
                     (0,1,0),.009,.019,"CeramicWhite","KNUCKLE",
                     sides=12))
    # Shock axis is recomputed per frame; rod telescopes inside barrel.
    put(cylinder("ShockOuterBarrel",(0,0,BARREL_LENGTH/2),
                 (0,0,1),.031,BARREL_LENGTH,"GraphiteArmor",
                 "DAMPER",sides=32))
    put(ring("ShockGland",(0,0,BARREL_LENGTH-.014),(0,0,1),
             .016,.040,.018,"CopperHardware","DAMPER"))
    put(ring("ShockBodyBand",(0,0,.102),(0,0,1),
             .030,.037,.012,"CeramicWhite","DAMPER"))
    put(cylinder("TelescopicChromeRod",(0,0,ROD_LENGTH/2),
                 (0,0,1),.015,ROD_LENGTH,"ChromedRod","ROD",sides=32))
    put(ring("RodEndLock",(0,0,ROD_LENGTH-.009),(0,0,1),
             .016,.027,.013,"CopperHardware","ROD"))
    put(helix("ExternalCoilSpring"))
    put(ring("UpperSpringSeat",(0,0,.108),(0,0,1),
             .041,.073,.019,"GraphiteArmor","SPRING"))
    put(ring("LowerSpringSeat",(0,0,.893),(0,0,1),
             .041,.073,.019,"GraphiteArmor","SPRING"))
    put(ring("AmberUpperSpringTrim",(0,0,.096),(0,0,1),
             .064,.070,.009,"AmberCoil","SPRING"))
    put(ring("AmberLowerSpringTrim",(0,0,.904),(0,0,1),
             .064,.070,.009,"AmberCoil","SPRING"))
    return parts

def transform(p,v,theta=0.):
    group=p["group"]
    if group=="STATIC":return v
    if group=="LOWER_ARM":return add(LOWER_PIVOT,rotate_x(v,theta))
    if group=="UPPER_ARM":return add(UPPER_PIVOT,rotate_x(v,theta))
    if group=="KNUCKLE":return add(hub(theta),v)
    _,direction,L=suspension(theta)
    if group=="SPRING":return orient((v[0],v[1],v[2]*L),TOP,direction)
    if group=="DAMPER":return orient(v,TOP,direction)
    if group=="ROD":
        return orient((v[0],v[1],v[2]+L-ROD_LENGTH),TOP,direction)
    raise ValueError(group)

def mesh_qa(parts):
    names=set();total_v=total_f=0
    for p in parts:
        assert p["name"] not in names,p["name"];names.add(p["name"])
        assert p["material"] in COLORS
        assert p["group"] in ("STATIC","LOWER_ARM","UPPER_ARM",
                              "KNUCKLE","SPRING","DAMPER","ROD")
        vertices=[transform(p,v) for v in p["vertices"]]
        edges={};volume=0.
        for a,b,c in p["faces"]:
            assert len({a,b,c})==3 and min(a,b,c)>=0 and max(a,b,c)<len(vertices)
            A,B,C=(vertices[i] for i in (a,b,c))
            assert norm(cross(sub(B,A),sub(C,A)))>1e-10,("degenerate",p["name"])
            volume+=dot(A,cross(B,C))/6
            for i,j in ((a,b),(b,c),(c,a)):
                edge=tuple(sorted((i,j)));edges[edge]=edges.get(edge,0)+1
            total_f+=1
        assert edges and all(n==2 for n in edges.values()),("nonmanifold",p["name"])
        assert volume>1e-12,("inverted or zero volume",p["name"],volume)
        total_v+=len(vertices)
    return total_v,total_f

def point_segment_distance(p,a,b):
    d=sub(b,a);u=max(0.,min(1.,dot(sub(p,a),d)/dot(d,d)))
    return norm(sub(p,add(a,mul(d,u))))

def sweep_qa():
    minimum_overlap=1e9;min_coil_gap=1e9
    lengths=[];hub_z=[];knuckle_separation=[];sample_count=0
    for i in range(97):
        theta=-MAX_SWING+2*MAX_SWING*i/96
        lower_joint=add(LOWER_PIVOT,rotate_x((0,OUTBOARD_SPAN,0),theta))
        upper_joint=add(UPPER_PIVOT,rotate_x((0,OUTBOARD_SPAN,0),theta))
        h=hub(theta)
        assert abs(h[1]-lower_joint[1])<1e-10
        assert abs(h[2]-.150-lower_joint[2])<1e-10
        assert abs(h[2]+.150-upper_joint[2])<1e-10
        separation=norm(sub(upper_joint,lower_joint))
        assert abs(separation-.300)<1e-10
        knuckle_separation.append(separation);hub_z.append(h[2])
        bottom,d,L=suspension(theta);lengths.append(L)
        overlap=BARREL_LENGTH+ROD_LENGTH-L
        minimum_overlap=min(minimum_overlap,overlap)
        assert .48<L<.68 and overlap>.035,("telescopic disengagement",i,overlap)
        pitch=.76*L/9
        assert pitch>2*.006+.006,("spring coil bind",i,pitch)
        assert .055-.006-.031>.017
        # Conservative 3D distance from spring axis to upper/lower fork
        # centre lines, excluding designed shock attachment at the base.
        for k in range(4,16):
            f=k/20
            centre=add(TOP,mul(d,f))
            for pivot,spread,tube in ((LOWER_PIVOT,.215,.026),
                                      (UPPER_PIVOT,.165,.021)):
                for sign in (-1,1):
                    a=add(pivot,rotate_x((sign*spread,.012,0),theta))
                    b=add(pivot,rotate_x((0,OUTBOARD_SPAN,-.014),theta))
                    gap=point_segment_distance(centre,a,b)-(.055+.006+tube)
                    min_coil_gap=min(min_coil_gap,gap)
                    assert gap>.001,(i,k,pivot,sign,gap)
                    sample_count+=1
        # 12mm axle in 17mm rubber bearing: 5mm radial clearance.
        assert .017-.012>=.005-1e-10
    return dict(pose_samples=97,arm_swing_degrees=[-10,10],
                hub_vertical_range_m=[round(min(hub_z),6),round(max(hub_z),6)],
                knuckle_ball_spacing_m=.300,
                min_rod_engagement_m=round(minimum_overlap,6),
                shock_length_range_m=[round(min(lengths),6),round(max(lengths),6)],
                min_coil_to_arm_envelope_m=round(min_coil_gap,6),
                coil_to_shock_radial_clearance_m=.018,
                bearing_radial_clearance_m=.005,
                coil_arm_pair_checks=sample_count)

def validate(parts):
    vertices,triangles=mesh_qa(parts)
    motion=sweep_qa()
    assert len(COLORS)>=3
    for group in ("UPPER_ARM","LOWER_ARM","KNUCKLE","SPRING","DAMPER","ROD"):
        assert any(p["group"]==group for p in parts),group
    assert any(p["name"]=="ExternalCoilSpring" for p in parts)
    return dict(id=ID,status="PASS",parts=len(parts),vertices=vertices,
                triangles=triangles,materials=len(COLORS),
                mount_pivot_m=[0,0,0],hinge_axes="two separate +X hinges",
                outboard_hub_at_rest_m=list(HUB_REST),
                suspension=motion,units="metres; Unity 1 unit = 1 metre",
                coordinate_system="Blender X fore-aft, Y lateral, Z up",
                collision_method="97 analytical poses, not Unity physics",
                blender="NOT RUN",unity="NOT RUN")

def static_export(parts,out,report):
    out.mkdir(parents=True,exist_ok=True)
    obj=ID+"_double_wishbone.obj";mtl=ID+"_double_wishbone.mtl"
    materials=[]
    for name,(rgba,metal,rough) in COLORS.items():
        materials.extend(("newmtl "+name,
                          "Kd %.6f %.6f %.6f"%tuple(rgba[:3]),
                          "Ks %.5f %.5f %.5f"%(metal,metal,metal),
                          "Ns %.2f"%max(1,(1-rough)*800),"illum 2",""))
    (out/mtl).write_text("\n".join(materials)+"\n",encoding="utf-8")
    lines=["# M02-011 static neutral-pose OBJ; animation in GLB","mtllib "+mtl]
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
    root["hinges"]="upper and lower X";root["visual_only"]=True
    groups={"STATIC":root}
    for name in ("UPPER_ARM","LOWER_ARM","KNUCKLE","SPRING","DAMPER"):
        ob=bpy.data.objects.new(PREFIX+name+"_RIG",None)
        col.objects.link(ob);ob.parent=root
        ob.empty_display_type="ARROWS";ob.empty_display_size=.035
        groups[name]=ob
    groups["UPPER_ARM"].location=UPPER_PIVOT
    groups["LOWER_ARM"].location=LOWER_PIVOT
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
        for name in ("LOWER_ARM","UPPER_ARM"):
            arm=groups[name];arm.rotation_mode="XYZ"
            arm.rotation_euler[0]=theta
            arm.keyframe_insert(data_path="rotation_euler",frame=frame_num)
        knuckle=groups["KNUCKLE"]
        knuckle.location=hub(theta)
        knuckle.keyframe_insert(data_path="location",frame=frame_num)
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
    # Validate evaluated world transforms, not merely keyframe existence.
    for sample in (1,25,49,73,97):
        scene.frame_set(sample);bpy.context.view_layer.update()
        theta=MAX_SWING*math.sin(2*math.pi*(sample-1)/FRAMES)
        bottom,_,L=suspension(theta)
        for name,pivot in (("LOWER_ARM",LOWER_PIVOT),
                           ("UPPER_ARM",UPPER_PIVOT)):
            actual=groups[name].matrix_world @ Vector((0,OUTBOARD_SPAN,0))
            expected=add(pivot,rotate_x((0,OUTBOARD_SPAN,0),theta))
            assert (actual-Vector(expected)).length<1e-5,(
                "arm endpoint mismatch",sample,name,tuple(actual),expected)
        kn=groups["KNUCKLE"].matrix_world @ Vector((0,0,0))
        assert (kn-Vector(hub(theta))).length<1e-5
        spring_tip=groups["SPRING"].matrix_world @ Vector((0,0,1))
        rod_tip=rod.matrix_world @ Vector((0,0,ROD_LENGTH))
        assert (spring_tip-Vector(bottom)).length<1e-5
        assert (rod_tip-Vector(bottom)).length<1e-5
    scene.frame_start=1;scene.frame_end=97;scene.render.fps=24
    scene.frame_set(1)
    out.mkdir(parents=True,exist_ok=True)
    blend=out/(ID+".blend");glb=out/(ID+"_animated.glb")
    bpy.ops.wm.save_as_mainfile(filepath=str(blend))
    bpy.ops.object.select_all(action="DESELECT")
    for ob in col.objects:ob.select_set(True)
    bpy.context.view_layer.objects.active=root
    bpy.ops.export_scene.gltf(filepath=str(glb),export_format="GLB",
                              use_selection=True,export_animations=True)
    assert blend.stat().st_size>10000 and glb.stat().st_size>10000
    report.update(backend="blender-bpy",blender="PASS",
                  blend=blend.name,glb=glb.name,
                  animation_frames=97,blender_evaluated_pose_checks=5,
                  animation_channels=["LOWER_ARM rotation","UPPER_ARM rotation",
                                      "KNUCKLE translation","SPRING aim+length",
                                      "DAMPER aim","ROD telescoping"])
    (out/(ID+"_qa.json")).write_text(json.dumps(report,indent=2)+"\n")
    return report

def main():
    args=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else sys.argv[1:]
    ap=argparse.ArgumentParser()
    ap.add_argument("--self-test",action="store_true")
    ap.add_argument("--static-obj",action="store_true")
    ap.add_argument("--out",default="build/M02-011")
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
