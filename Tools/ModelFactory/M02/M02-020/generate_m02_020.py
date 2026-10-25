#!/usr/bin/env python3
"""M02-020 rack-and-pinion steering; bpy 3.6+/4.x/5.x; 1 Blender unit = 1m."""
import bpy, bmesh, math, json
from pathlib import Path
from mathutils import Vector

ID="M02-020"
OUT=Path(__file__).resolve().parent/"generated"
OUT.mkdir(exist_ok=True)
COL=ID+"_MODEL"
PI=math.pi
TRAVEL=.065
PITCH_R=.060
FRAME_END=121

def linkage(side, stroke):
    px,py=side*.600,.100
    ax,ay=-side*.080,.080
    qx,qy=side*.355+stroke,.045
    L=math.hypot(px+ax-side*.355,py+ay-.045)
    arm=math.hypot(ax,ay)
    dx,dy=qx-px,qy-py
    d=math.hypot(dx,dy)
    c=(arm*arm+d*d-L*L)/(2*arm*d)
    if abs(c)>1+1e-9: raise ValueError("Infeasible tie rod geometry")
    phi=math.atan2(dy,dx)
    neutral=math.atan2(ay,ax)
    wrap=lambda a:(a+PI)%(2*PI)-PI
    angle=min((wrap(phi+math.acos(max(-1,min(1,c)))-neutral),
               wrap(phi-math.acos(max(-1,min(1,c)))-neutral)),key=abs)
    end=(px+ax*math.cos(angle)-ay*math.sin(angle),
         py+ax*math.sin(angle)+ay*math.cos(angle),.180)
    anchor=(qx,qy,.180)
    error=abs(math.dist(end,anchor)-L)
    return angle,end,anchor,L,error

def math_check():
    maximum=0
    min_pitch=1
    max_angle=0
    for i in range(FRAME_END):
        s=TRAVEL*math.sin(2*PI*i/(FRAME_END-1))
        for side in (-1,1):
            angle,end,anchor,L,e=linkage(side,s)
            maximum=max(maximum,e)
            max_angle=max(max_angle,abs(angle))
            min_pitch=min(min_pitch,abs(side*.345+s-side*.185)/8)
    # Pinion outer radius .066 at Y=.139; rack tooth tip at Y=.071.
    clearance=.139-.066-.071
    assert maximum<1e-9 and max_angle<math.radians(40)
    assert min_pitch>.008 and clearance>.0019
    return dict(poses=FRAME_END,rod_samples=2*FRAME_END,
                max_rod_length_error_m=maximum,
                max_steer_deg=math.degrees(max_angle),
                min_bellows_spacing_m=min_pitch,
                rack_pinion_tip_gap_m=clearance)

def build():
    old=bpy.data.collections.get(COL)
    if old:
        for obj in list(old.objects): bpy.data.objects.remove(obj,do_unlink=True)
        bpy.data.collections.remove(old)
    for blocks in (bpy.data.meshes,bpy.data.curves,bpy.data.actions):
        for block in list(blocks):
            if block.users==0: blocks.remove(block)
    for m in list(bpy.data.materials):
        if m.name.startswith(ID+"_") and m.users==0: bpy.data.materials.remove(m)
    col=bpy.data.collections.new(COL)
    bpy.context.scene.collection.children.link(col)
    scene=bpy.context.scene
    scene.unit_settings.system="METRIC"
    scene.unit_settings.scale_length=1.
    scene.frame_start,scene.frame_end,scene.render.fps=1,FRAME_END,30

    def material(name,rgb,metal,rough):
        m=bpy.data.materials.new(ID+"_"+name)
        m.diffuse_color=(*rgb,1)
        m.use_nodes=True
        bsdf=m.node_tree.nodes.get("Principled BSDF")
        bsdf.inputs["Base Color"].default_value=(*rgb,1)
        bsdf.inputs["Metallic"].default_value=metal
        bsdf.inputs["Roughness"].default_value=rough
        return m
    steel=material("01_GraphiteSteel",(.11,.15,.19),.83,.34)
    alloy=material("02_Aluminium",(.51,.58,.63),.77,.25)
    bronze=material("03_Bronze",(.58,.34,.12),.72,.31)
    rubber=material("04_Rubber",(.035,.04,.046),.05,.82)
    orange=material("05_Orange",(.92,.27,.04),.27,.38)
    blue=material("06_BlueHousing",(.045,.20,.41),.48,.30)
    chrome=material("07_Chrome",(.73,.79,.81),.91,.18)
    hardened=material("08_HardenedTeeth",(.27,.31,.33),.86,.26)

    def attach(o,mat,parent):
        for c in list(o.users_collection): c.objects.unlink(o)
        col.objects.link(o)
        if mat is not None:o.data.materials.append(mat)
        if parent:o.parent=parent
        return o
    def empty(name,loc=(0,0,0),parent=None):
        o=bpy.data.objects.new(name,None)
        col.objects.link(o)
        o.location=loc
        o.empty_display_size=.03
        if parent:o.parent=parent
        return o
    root=empty("MOUNT_CHASSIS__Z0__UNITY_1M")
    rack=empty("RACK_SLIDE_X",parent=root)
    pinion=empty("PINION_ROTATE_Z",(0,.139,.180),root)
    knuckles={s:empty(("LEFT" if s<0 else "RIGHT")+"_KNUCKLE_ROTATE_Z",
                      (s*.600,.100,.180),root) for s in (-1,1)}
    rods={s:empty(("LEFT" if s<0 else "RIGHT")+"_TIE_ROD_RIG",parent=root)
          for s in (-1,1)}
    def cube(name,pos,size,mat,parent=root,bevel=0):
        bpy.ops.mesh.primitive_cube_add(size=1,location=pos)
        o=bpy.context.object
        o.name=name
        o.dimensions=size
        bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
        attach(o,mat,parent)
        if bevel:
            m=o.modifiers.new("Machined_edges","BEVEL")
            m.width=bevel
            m.segments=2
            o.modifiers.new("Weighted_normals","WEIGHTED_NORMAL")
        return o
    def cylinder(name,pos,radius,depth,mat,parent=root,axis="Z",verts=32):
        rot=(0,PI/2,0) if axis=="X" else ((PI/2,0,0) if axis=="Y" else (0,0,0))
        bpy.ops.mesh.primitive_cylinder_add(vertices=verts,radius=radius,
                                             depth=depth,location=pos,rotation=rot)
        o=bpy.context.object
        o.name=name
        return attach(o,mat,parent)
    def torus(name,pos,major,minor,mat,parent=root,axis="X"):
        rot=(0,PI/2,0) if axis=="X" else ((PI/2,0,0) if axis=="Y" else (0,0,0))
        bpy.ops.mesh.primitive_torus_add(major_segments=36,minor_segments=8,
                                         major_radius=major,minor_radius=minor,
                                         location=pos,rotation=rot)
        o=bpy.context.object
        o.name=name
        return attach(o,mat,parent)
    def beam(name,a,b,r,mat,parent=root):
        a,b=Vector(a),Vector(b)
        o=cylinder(name,(a+b)*.5,r,(b-a).length,mat,parent)
        o.rotation_mode="QUATERNION"
        o.rotation_quaternion=(b-a).to_track_quat("Z","Y")
        return o
    def prism(name,points,z0,z1,mat,parent):
        n=len(points)
        verts=[(x,y,z) for z in (z0,z1) for x,y in points]
        faces=[tuple(reversed(range(n))),tuple(range(n,2*n))]
        faces += [(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
        mesh=bpy.data.meshes.new(name+"_MESH")
        mesh.from_pydata(verts,[],faces)
        mesh.update()
        o=bpy.data.objects.new(name,mesh)
        col.objects.link(o)
        o.data.materials.append(mat)
        o.parent=parent
        bm=bmesh.new()
        bm.from_mesh(mesh)
        bmesh.ops.recalc_face_normals(bm,faces=bm.faces)
        bm.to_mesh(mesh)
        bm.free()
        return o

    # Actual chassis mounting datum: bottom of 32mm steel foot at Z=0.
    cube("MOUNT_FOOT_PLATE",(0,-.005,.016),(.43,.17,.032),steel,bevel=.007)
    for side in (-1,1):
        x=side*.155
        cube("MOUNT_UPRIGHT_"+str(side),(x,-.015,.090),
             (.046,.055,.128),steel,bevel=.005)
        for y in (-.063,.053):
            cylinder("MOUNT_BOLT_%s_%s"%(side,y),(x,y,.036),
                     .012,.013,bronze,verts=20)
    cube("HOUSING_REAR_RAIL",(0,-.012,.180),(.43,.030,.072),blue,bevel=.006)
    cube("HOUSING_UPPER_GUIDE",(0,.012,.221),(.43,.036,.013),alloy,bevel=.002)
    cube("HOUSING_LOWER_GUIDE",(0,.012,.139),(.43,.036,.013),alloy,bevel=.002)
    for side in (-1,1):
        torus("GUIDE_BUSHING_"+str(side),(side*.213,.045,.180),
              .026,.0035,bronze)
        cube("HOUSING_END_BRACKET_"+str(side),(side*.218,-.018,.180),
             (.019,.033,.084),steel,bevel=.003)

    # Sliding rack: 19 solid trapezoidal teeth in the Y+ face.
    cube("RACK_CORE",(0,.045,.180),(.740,.025,.033),chrome,rack,bevel=.002)
    for i in range(-9,10):
        x=i*PI*.006
        prism("RACK_TOOTH_%+03d"%i,
              [(x-.006,.0575),(x+.006,.0575),
               (x+.0037,.071),(x-.0037,.071)],
              .164,.196,hardened,rack)
    for side in (-1,1):
        cylinder("RACK_BALL_"+str(side),(side*.355,.045,.180),
                 .018,.045,bronze,rack)
        torus("RACK_END_SEAL_"+str(side),(side*.366,.045,.180),
              .021,.003,rubber,rack)

    # Single manifold pinion, 20 teeth, Z axis, decorative 2mm tip gap.
    polygon=[]
    for i in range(20):
        a=2*PI*i/20
        for f,r in ((-.50,.048),(-.31,.048),(-.20,.066),
                    (.20,.066),(.31,.048)):
            t=a+f*2*PI/20
            polygon.append((r*math.cos(t),r*math.sin(t)))
    prism("PINION_20_TOOTH_SOLID",polygon,-.016,.016,hardened,pinion)
    cylinder("PINION_SHAFT",(0,0,.039),.020,.078,chrome,pinion)
    cylinder("PINION_COUPLER",(0,0,.085),.035,.020,bronze,pinion)
    for i in range(6):
        a=i*2*PI/6
        cylinder("PINION_FACE_FASTENER_%02d"%i,
                 (.033*math.cos(a),.033*math.sin(a),.020),
                 .006,.009,orange,pinion,verts=12)
    # Split support plate: genuine shaft aperture, not solid clipping mesh.
    for sx in (-1,1):
        cube("PINION_SUPPORT_SIDE_"+str(sx),(sx*.057,.139,.235),
             (.020,.135,.013),steel,bevel=.003)
        cylinder("PINION_SUPPORT_BOLT_"+str(sx),(sx*.059,.139,.246),
                 .009,.018,orange,verts=16)
    for sy in (-1,1):
        cube("PINION_SUPPORT_CROSS_"+str(sy),(0,.139+sy*.057,.235),
             (.095,.020,.013),steel,bevel=.003)
    torus("PINION_SHAFT_BEARING",(0,.139,.242),.029,.007,bronze,axis="Z")

    # Knuckles with true two-circle tie rod kinematics.
    for side in (-1,1):
        label="LEFT" if side<0 else "RIGHT"
        group=knuckles[side]
        cylinder(label+"_KINGPIN",(0,0,0),.035,.093,alloy,group)
        for z in (-.050,.050):
            cylinder(label+"_KINGPIN_BUSH_"+str(z),(0,0,z),
                     .042,.014,bronze,group)
        beam(label+"_STEERING_ARM",(0,0,0),
             (-side*.080,.080,0),.023,steel,group)
        cylinder(label+"_BALL_JOINT",(-side*.080,.080,0),
                 .022,.037,bronze,group)
        cube(label+"_HUB_INTERFACE",(side*.042,-.024,0),
             (.085,.060,.095),blue,group,bevel=.006)
        for z in (-.029,.029):
            cylinder(label+"_HUB_BOLT_"+str(z),
                     (side*.075,-.032,z),.008,.013,
                     orange,group,axis="X",verts=16)
        L=linkage(side,0)[3]
        cylinder(label+"_TIE_ROD",(0,0,0),.010,L-.026,chrome,rods[side])
        cylinder(label+"_ADJUSTER",(0,0,0),.016,.057,alloy,rods[side])
        for z in (-.033,.033):
            cylinder(label+"_LOCKNUT_"+str(z),(0,0,z),
                     .018,.010,bronze,rods[side],verts=12)

    # Nine rubber convolutions per side, each individually animated.
    bellows={}
    for side in (-1,1):
        label="LEFT" if side<0 else "RIGHT"
        for i in range(9):
            bellows[(side,i)]=torus(label+"_BELLOWS_%02d"%i,
                                   (0,.045,.180),.026,.0035,rubber)
        torus(label+"_HOUSING_CLAMP",(side*.171,.045,.180),
              .028,.004,orange)

    for frame in range(1,FRAME_END+1):
        s=TRAVEL*math.sin(2*PI*(frame-1)/(FRAME_END-1))
        rack.location.x=s
        rack.keyframe_insert(data_path="location",frame=frame)
        pinion.rotation_euler.z=-s/PITCH_R
        pinion.keyframe_insert(data_path="rotation_euler",frame=frame)
        for side in (-1,1):
            angle,end,anchor,L,error=linkage(side,s)
            knuckles[side].rotation_euler.z=angle
            knuckles[side].keyframe_insert(data_path="rotation_euler",frame=frame)
            rod=rods[side]
            a,b=Vector(anchor),Vector(end)
            rod.location=(a+b)*.5
            rod.rotation_mode="QUATERNION"
            rod.rotation_quaternion=(b-a).to_track_quat("Z","Y")
            rod.keyframe_insert(data_path="location",frame=frame)
            rod.keyframe_insert(data_path="rotation_quaternion",frame=frame)
            for i in range(9):
                t=i/8
                ring=bellows[(side,i)]
                ring.location.x=(1-t)*side*.185+t*(side*.345+s)
                ring.keyframe_insert(data_path="location",frame=frame)
    scene.frame_set(1)
    for obj in col.objects:
        if obj.animation_data and obj.animation_data.action:
            try:
                for curve in obj.animation_data.action.fcurves:
                    for key in curve.keyframe_points:key.interpolation="LINEAR"
            except (AttributeError,TypeError):pass
    meshes=[o for o in col.objects if o.type=="MESH"]
    assert len(meshes)>=65
    for o in meshes:
        bm=bmesh.new()
        bm.from_mesh(o.data)
        assert all(e.is_manifold for e in bm.edges),o.name
        assert all(f.calc_area()>1e-11 for f in bm.faces),o.name
        bm.free()
    return col,dict(meshes=len(meshes),
                    materials=len([m for m in bpy.data.materials
                                   if m.name.startswith(ID+"_")]),
                    objects=len(col.objects))

def main():
    math_report=math_check()
    _,first=build()
    col,second=build()
    assert first==second,(first,second)
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/(ID+".blend")))
    bpy.ops.object.select_all(action="DESELECT")
    for obj in col.objects:obj.select_set(True)
    bpy.context.view_layer.objects.active=bpy.data.objects["MOUNT_CHASSIS__Z0__UNITY_1M"]
    bpy.ops.export_scene.gltf(filepath=str(OUT/(ID+"_animated.glb")),
                              export_format="GLB",use_selection=True,
                              export_animations=True)
    report=dict(id=ID,agent="M02",blender=bpy.app.version_string,
                geometry=second,reentrant="PASS",manifold="PASS",
                math=math_report,unity="NOT RUN",
                physics="NOT RUN",units="1m",
                root_pivot=[0,0,0],
                caveat="visual noncontact 2mm gear gap, not involute CAD or PhysX")
    (OUT/(ID+"_qa.json")).write_text(json.dumps(report,indent=2),encoding="utf-8")
    print("M02-020 GENERATED BLEND GLB",json.dumps(second))

if __name__=="__main__":main()
