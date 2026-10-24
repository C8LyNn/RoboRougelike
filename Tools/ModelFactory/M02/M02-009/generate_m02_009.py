#!/usr/bin/env python3
"""M02-009 — triangular three-wheel tracked bogie, MODEL-02 / RoboRougelike.

Blender coordinates: X forward, Y axle/lateral, Z up; metres (Unity 1 unit=1m).
The actual vehicle-side mounting pivot is ROOT=(0,0,0), outward normal -Y.
All rotating wheels and 40 tread links are independent animation transforms.
Python stdlib: --self-test or --static-obj; Blender: -- --out DIR.
Visual module only; no gameplay, Unity physics or prefabs.
"""
from __future__ import annotations
import argparse
import json
import math
import sys
from pathlib import Path

ID="M02-009"
PREFIX="RRL_M02_009_"
COLLECTION=PREFIX+"TRI_WHEEL_BOGIE"
Y_CENTRE=.230
WHEEL_CENTRES=((-0.310,-0.270),(0.310,-0.270),(0.0,0.170))
R=.147
WHEEL_OUTER=.130
N_LINKS=40
FRAMES=96
COLORS={
 "CeramicBlack":((.060,.075,.085,1),.64,.32),
 "SatinTitanium":((.39,.47,.53,1),.84,.27),
 "GraphiteRubber":((.023,.029,.033,1),.03,.83),
 "Copper":((.66,.29,.10,1),.76,.28),
 "SafetyAmber":((.99,.37,.035,1),.36,.32),
 "CoolCyan":((.045,.57,.78,1),.20,.28),
 "DarkGasket":((.11,.13,.15,1),.06,.79),
}
BOX_FACES=((0,3,2),(0,2,1),(4,5,6),(4,6,7),
           (0,1,5),(0,5,4),(1,2,6),(1,6,5),
           (2,3,7),(2,7,6),(3,0,4),(3,4,7))

def box(name,centre,size,material,parent="STATIC",bevel=0):
    x,y,z=centre; a,b,c=(v/2 for v in size)
    v=[(x-a,y-b,z-c),(x+a,y-b,z-c),(x+a,y+b,z-c),(x-a,y+b,z-c),
       (x-a,y-b,z+c),(x+a,y-b,z+c),(x+a,y+b,z+c),(x-a,y+b,z+c)]
    return dict(name=name,vertices=v,faces=list(BOX_FACES),
                material=material,parent=parent,bevel=bevel)

def rotate_y(v,theta):
    x,y,z=v
    return (math.cos(theta)*x+math.sin(theta)*z,
            y,-math.sin(theta)*x+math.cos(theta)*z)

def beam(name,a,b,y,width,height,material,bevel=.004):
    (x1,z1),(x2,z2)=a,b
    dx=x2-x1;dz=z2-z1
    length=math.hypot(dx,dz)
    theta=math.atan2(-dz,dx)
    p=box(name,(0,0,0),(length+.025,width,height),material,bevel=bevel)
    p["vertices"]=[(v[0]+(x1+x2)/2,v[1]+y,v[2]+(z1+z2)/2)
                   for v in (rotate_y(q,theta) for q in p["vertices"])]
    return p

def cylinder_y(name,centre,radius,width,material,parent="STATIC",sides=24):
    x,y,z=centre
    vertices=[(x+radius*math.cos(2*math.pi*i/sides),
               y+side*width/2,
               z+radius*math.sin(2*math.pi*i/sides))
              for side in (-1,1) for i in range(sides)]
    vertices.extend(((x,y-width/2,z),(x,y+width/2,z)))
    faces=[]
    for i in range(sides):
        j=(i+1)%sides
        faces.extend(((i,j,sides+j),(i,sides+j,sides+i),
                      (2*sides,j,i),(2*sides+1,sides+i,sides+j)))
    # Parametric XZ circle with +Y width has reversed winding under
    # this coordinate convention; flip all faces to point outward.
    faces=[(a,c,b) for a,b,c in faces]
    return dict(name=name,vertices=vertices,faces=faces,
                material=material,parent=parent,bevel=0)

def ring_y(name,centre,inner,outer,width,material,parent="STATIC",sides=32):
    assert 0<inner<outer
    x,y,z=centre
    vertices=[]
    for yy,rad in ((-width/2,outer),(-width/2,inner),
                   (width/2,outer),(width/2,inner)):
        for i in range(sides):
            a=2*math.pi*i/sides
            vertices.append((x+rad*math.cos(a),y+yy,z+rad*math.sin(a)))
    faces=[]
    for i in range(sides):
        j=(i+1)%sides
        ob,oj=i,j
        ib,ij=sides+i,sides+j
        of,ofj=2*sides+i,2*sides+j
        inf,inj=3*sides+i,3*sides+j
        faces.extend(((ob,of,ofj),(ob,ofj,oj),
                      (ib,ij,inj),(ib,inj,inf),
                      (ob,oj,ij),(ob,ij,ib),
                      (of,inj,ofj),(of,inf,inj)))
    return dict(name=name,vertices=vertices,faces=faces,
                material=material,parent=parent,bevel=0)

def wedge(name,centre,length,top_length,width,height,material,parent):
    assert 0<top_length<length
    x,y,z=centre;a=length/2;b=top_length/2;w=width/2;h=height/2
    vertices=[(x-a,y-w,z-h),(x+a,y-w,z-h),(x+a,y+w,z-h),(x-a,y+w,z-h),
              (x-b,y-w,z+h),(x+b,y-w,z+h),(x+b,y+w,z+h),(x-b,y+w,z+h)]
    return dict(name=name,vertices=vertices,faces=list(BOX_FACES),
                material=material,parent=parent,bevel=0)

# The belt runs counter-clockwise around the offset of a convex triangle.
# Edge normals and corner arcs are exact, so every link remains tangent.
A,B,C=WHEEL_CENTRES
EDGES=((A,B),(B,C),(C,A))
def _geometry():
    result=[]
    for start,end in EDGES:
        dx=end[0]-start[0];dz=end[1]-start[1]
        length=math.hypot(dx,dz)
        tangent=(dx/length,dz/length)
        outward=(tangent[1],-tangent[0])
        phi=math.atan2(outward[1],outward[0])
        result.append((length,tangent,outward,phi))
    return result
G=_geometry()
def _positive_arc(phi1,phi2):
    return (phi2-phi1)%(2*math.pi)
ARC_B=_positive_arc(G[0][3],G[1][3])
ARC_C=_positive_arc(G[1][3],G[2][3])
ARC_A=_positive_arc(G[2][3],G[0][3])
PERIMETER=sum(edge[0] for edge in G)+R*(ARC_A+ARC_B+ARC_C)
PITCH=PERIMETER/N_LINKS
assert abs(ARC_A+ARC_B+ARC_C-2*math.pi)<1e-9

def path_pose(distance):
    """Return world location and unwrapped Blender Y Euler angle."""
    lap=math.floor(distance/PERIMETER)
    s=distance-lap*PERIMETER
    # Each edge is followed by the round corner at its endpoint.
    accumulated_angle=0.0
    for edge_idx,arc in enumerate((ARC_B,ARC_C,ARC_A)):
        start,end=EDGES[edge_idx]
        length,tangent,outward,phi=G[edge_idx]
        if s<length:
            p=(start[0]+R*outward[0]+s*tangent[0],
               start[1]+R*outward[1]+s*tangent[1])
            return ((p[0],Y_CENTRE,p[1]),
                    math.atan2(-tangent[1],tangent[0])-2*math.pi*lap
                    if edge_idx==0 else
                    -accumulated_angle-2*math.pi*lap)
        s-=length
        if s<R*arc:
            q=s/R
            angle=phi+q
            p=(end[0]+R*math.cos(angle),end[1]+R*math.sin(angle))
            return ((p[0],Y_CENTRE,p[1]),
                    -(accumulated_angle+q)-2*math.pi*lap)
        s-=R*arc
        accumulated_angle+=arc
    raise AssertionError((distance,s))

def group_pose(group,phase=0.):
    if group.startswith("LINK_"):
        i=int(group[5:])
        return path_pose(i*PITCH+phase)
    if group.startswith("WHEEL_"):
        i=int(group[6:])
        x,z=WHEEL_CENTRES[i]
        return ((x,Y_CENTRE,z),-phase/R)
    return ((0.,0.,0.),0.)

def transform(v,pose):
    loc,ang=pose
    rot=rotate_y(v,ang)
    return tuple(rot[i]+loc[i] for i in range(3))

def build():
    parts=[]
    add=parts.append
    # Strong, visually legible triangular side beams sit OUTSIDE belt width.
    for side in (-1,1):
        y=Y_CENTRE+side*.175
        for i,(start,end) in enumerate(EDGES):
            add(beam("Truss_%s_%d"%("L" if side<0 else "R",i),
                     start,end,y,.028,.042,"SatinTitanium",.005))
        for i,(x,z) in enumerate(WHEEL_CENTRES):
            add(cylinder_y("BearingBlock_%+d_%d"%(side,i),
                           (x,y,z),.038,.035,"CeramicBlack"))
            add(ring_y("BearingLip_%+d_%d"%(side,i),
                       (x,y+side*.021,z),.018,.034,.007,"Copper",sides=24))
        # Triangular inner reinforcement with clearly visible contrasting bars.
        add(beam("CrossBrace_%+d"%side,(-.195,-.185),(.195,-.185),
                 y+side*.019,.018,.020,"SafetyAmber",.002))
    # Three genuine through-bore wheels, each spinning independently.
    for i,(x,z) in enumerate(WHEEL_CENTRES):
        parent="WHEEL_%d"%i
        add(cylinder_y("StationaryAxle_%d"%i,
                       (x,Y_CENTRE,z),.016,.366,"SatinTitanium"))
        add(ring_y("SteelRim_%d"%i,(0,0,0),.057,.119,.094,
                   "SatinTitanium",parent))
        add(ring_y("RubberTire_%d"%i,(0,0,0),.116,WHEEL_OUTER,.113,
                   "GraphiteRubber",parent))
        add(ring_y("CopperHub_%d"%i,(0,0,0),.024,.066,.100,
                   "Copper",parent))
        add(ring_y("Seal_%d"%i,(0,-.053,0),.027,.049,.007,
                   "DarkGasket",parent))
        for j in range(8):
            ang=2*math.pi*j/8
            p=box("WheelSpoke_%d_%02d"%(i,j),(0,0,.087),
                  (.019,.068,.054),"CeramicBlack",parent,.003)
            p["vertices"]=[rotate_y(v,-ang) for v in p["vertices"]]
            add(p)
        for j in range(8):
            ang=2*math.pi*j/8
            add(cylinder_y("WheelRivet_%d_%02d"%(i,j),
                           (.050*math.cos(ang),-.057,
                            .050*math.sin(ang)),
                           .006,.006,"SafetyAmber",parent,12))
        for j in range(12):
            ang=2*math.pi*j/12
            p=box("GripLug_%d_%02d"%(i,j),(0,0,.126),
                  (.027,.107,.006),"DarkGasket",parent,.002)
            p["vertices"]=[rotate_y(v,-ang) for v in p["vertices"]]
            add(p)
    # Continuous track visualized with independently moving rigid shoes.
    for i in range(N_LINKS):
        parent="LINK_%02d"%i
        add(box("SteelShoe_%02d"%i,(0,0,0),(.056,.198,.026),
                "CeramicBlack",parent,.003))
        add(wedge("ChevronCleat_%02d"%i,(0,0,.020),
                  .048,.035,.208,.014,"GraphiteRubber",parent))
        for side in (-1,1):
            add(cylinder_y("HingePin_%02d_%+d"%(i,side),
                           (.023,side*.103,0),.009,.010,
                           "Copper",parent,12))
        if i%5==0:
            add(box("AmberIndex_%02d"%i,(0,0,.029),
                    (.019,.082,.005),"SafetyAmber",parent,.001))
        elif i%5==2:
            add(box("CyanIndex_%02d"%i,(0,0,.029),
                    (.010,.050,.005),"CoolCyan",parent,.001))
    # Socket is ROOT pivot at zero; external -Y direction is vehicle-side.
    add(box("VehicleMountPlate",(0,0,0),
            (.215,.034,.170),"SatinTitanium",bevel=.006))
    add(box("LoadBearingNeck",(0,.040,-.020),
            (.130,.100,.095),"CeramicBlack",bevel=.006))
    add(box("TriangularBackbone",(0,.086,-.100),
            (.170,.100,.075),"SatinTitanium",bevel=.006))
    for x in (-.077,.077):
        for z in (-.055,.055):
            add(cylinder_y("MountBolt_%+.3f_%+.3f"%(x,z),
                           (x,-.020,z),.012,.008,"Copper",sides=12))
    return parts

def sub(a,b):return tuple(a[i]-b[i] for i in range(3))
def cross(a,b):return (a[1]*b[2]-a[2]*b[1],
                        a[2]*b[0]-a[0]*b[2],
                        a[0]*b[1]-a[1]*b[0])
def dot(a,b):return sum(x*y for x,y in zip(a,b))

def mesh_qa(parts):
    seen=set();tris=0;verts=0
    for p in parts:
        assert p["name"] not in seen,p["name"]
        seen.add(p["name"])
        assert p["material"] in COLORS
        vertices=p["vertices"]
        edges={}
        volume=0.
        for a,b,c in p["faces"]:
            assert len({a,b,c})==3 and min(a,b,c)>=0 and max(a,b,c)<len(vertices)
            A,B,C=(vertices[k] for k in (a,b,c))
            normal=cross(sub(B,A),sub(C,A))
            assert dot(normal,normal)>1e-15,(p["name"],"degenerate")
            volume+=dot(A,cross(B,C))/6.
            for x,y in ((a,b),(b,c),(c,a)):
                key=tuple(sorted((x,y)))
                edges[key]=edges.get(key,0)+1
            tris+=1
        assert edges and all(v==2 for v in edges.values()),("nonmanifold",p["name"])
        assert volume>1e-11,("inward or zero volume",p["name"],volume)
        verts+=len(vertices)
    return tris,verts

def obb_separation(pose_a,pose_b,half_length=.028,half_depth=.021):
    # Conservative 2D separating-axis test of complete steel+rubber link.
    (a,theta_a),(b,theta_b)=pose_a,pose_b
    A=((math.cos(theta_a),-math.sin(theta_a)),
       (math.sin(theta_a),math.cos(theta_a)))
    B=((math.cos(theta_b),-math.sin(theta_b)),
       (math.sin(theta_b),math.cos(theta_b)))
    dx=b[0]-a[0];dz=b[2]-a[2]
    gaps=[]
    for axis in (*A,*B):
        dist=abs(dx*axis[0]+dz*axis[1])
        ra=half_length*abs(A[0][0]*axis[0]+A[0][1]*axis[1])+half_depth*abs(A[1][0]*axis[0]+A[1][1]*axis[1])
        rb=half_length*abs(B[0][0]*axis[0]+B[0][1]*axis[1])+half_depth*abs(B[1][0]*axis[0]+B[1][1]*axis[1])
        gaps.append(dist-ra-rb)
    return max(gaps)

def swept_qa():
    min_link=1e9;min_wheel=1e9;count=0
    for frame in range(FRAMES+1):
        phase=PERIMETER*frame/FRAMES
        poses=[]
        for i in range(N_LINKS):
            pos,yaw=path_pose(i*PITCH+phase)
            # Real shell -13..+27mm; centre offset +7mm, half-depth 20mm.
            outward=(math.sin(yaw),math.cos(yaw))
            loc=(pos[0]+.007*outward[0],pos[1],pos[2]+.007*outward[1])
            poses.append((loc,yaw))
        for i in range(N_LINKS):
            for j in range(i+1,N_LINKS):
                gap=obb_separation(poses[i],poses[j])
                min_link=min(min_link,gap)
                assert gap>.0005,("track collision",frame,i,j,gap)
                count+=1
            pos,yaw=poses[i]
            tangent=(math.cos(yaw),-math.sin(yaw))
            normal=(math.sin(yaw),math.cos(yaw))
            for wx,wz in WHEEL_CENTRES:
                dx=wx-pos[0];dz=wz-pos[2]
                tx=dx*tangent[0]+dz*tangent[1]
                tz=dx*normal[0]+dz*normal[1]
                separation=math.hypot(max(abs(tx)-.028,0),
                                      max(abs(tz)-.021,0))-WHEEL_OUTER
                min_wheel=min(min_wheel,separation)
                assert separation>.0005,("wheel/tread collision",frame,i,wx,separation)
    return min_link,min_wheel,count

def validate(parts):
    triangles,vertices=mesh_qa(parts)
    gap,wheel_gap,checks=swept_qa()
    assert len(COLORS)>=3
    assert len([p for p in parts if p["name"].startswith("SteelShoe_")])==N_LINKS
    assert len([p for p in parts if p["name"].startswith("RubberTire_")])==3
    for i in range(N_LINKS):
        a,ra=path_pose(i*PITCH)
        b,rb=path_pose(i*PITCH+PERIMETER)
        assert max(abs(a[k]-b[k]) for k in range(3))<1e-8
        assert abs(rb-ra+2*math.pi)<1e-8
    # Mechanical Y separation between the stationary side truss and track.
    track_y=(Y_CENTRE-.109,Y_CENTRE+.109)
    for side in (-1,1):
        rail_y=Y_CENTRE+side*.175
        assert (rail_y-.014-track_y[1] if side>0 else
                track_y[0]-(rail_y+.014))>.04
    return dict(id=ID,result="PASS",parts=len(parts),vertices=vertices,
                triangles=triangles,materials=len(COLORS),wheels=3,
                links=N_LINKS,track_pitch_m=round(PITCH,7),
                animation_frames=FRAMES,pose_samples=FRAMES+1,
                link_pair_sat_tests=checks,
                min_link_clearance_m=round(gap,6),
                min_wheel_clearance_m=round(wheel_gap,6),
                root_mount_pivot_m=[0,0,0],wheel_axis="+Y",
                axes="X forward, Y axle, Z up (Blender); glTF maps to Unity Y-up",
                units="metres; Unity 1 unit=1m",
                collision_method="2D swept OBB SAT + conservative wheel circles; not physics engine",
                blender="NOT RUN",unity="NOT RUN")

def static_obj(parts,out):
    out.mkdir(parents=True,exist_ok=True)
    obj=ID+"_triwheel_bogie.obj";mtl=ID+"_triwheel_bogie.mtl"
    lines=["# M02-009 real static OBJ, animated version in GLB","mtllib "+mtl]
    materials=[]
    for name,(rgba,metal,rough) in COLORS.items():
        materials.extend(("newmtl "+name,
                          "Kd %.6f %.6f %.6f"%tuple(rgba[:3]),
                          "Ks %.5f %.5f %.5f"%(metal,metal,metal),
                          "Ns %.2f"%max(1,(1-rough)*800),"illum 2",""))
    (out/mtl).write_text("\n".join(materials)+"\n",encoding="utf-8")
    offset=1
    for p in parts:
        lines.extend(("o "+p["name"],"usemtl "+p["material"]))
        pose=group_pose(p["parent"])
        for v in p["vertices"]:
            xyz=transform(v,pose)
            lines.append("v %.8f %.8f %.8f"%xyz)
        for a,b,c in p["faces"]:
            lines.append("f %d %d %d"%(a+offset,b+offset,c+offset))
        offset+=len(p["vertices"])
    (out/obj).write_text("\n".join(lines)+"\n",encoding="utf-8")
    return dict(backend="python-stdlib",obj=obj,mtl=mtl,blend="NOT RUN",glb="NOT RUN")

def blender_export(parts,out):
    import bpy
    old=bpy.data.collections.get(COLLECTION)
    if old:
        for ob in tuple(old.objects):
            bpy.data.objects.remove(ob,do_unlink=True)
        bpy.data.collections.remove(old)
    col=bpy.data.collections.new(COLLECTION)
    bpy.context.scene.collection.children.link(col)
    bpy.context.scene.unit_settings.system="METRIC"
    bpy.context.scene.unit_settings.scale_length=1.
    root=bpy.data.objects.new(PREFIX+"ROOT_MOUNT_PIVOT",None)
    col.objects.link(root)
    root.empty_display_type="PLAIN_AXES";root.empty_display_size=.08
    root["socket_outward_axis"]="-Y";root["unity_metre_scale"]=1.
    root["visual_only"]=True
    socket=bpy.data.objects.new(PREFIX+"MOUNT_SOCKET",None)
    col.objects.link(socket);socket.parent=root
    socket.empty_display_type="ARROWS";socket.empty_display_size=.06
    parents={"STATIC":root}
    for i in range(3):
        key="WHEEL_%d"%i
        ob=bpy.data.objects.new(PREFIX+key,None)
        col.objects.link(ob);ob.parent=root;parents[key]=ob
    for i in range(N_LINKS):
        key="LINK_%02d"%i
        ob=bpy.data.objects.new(PREFIX+key,None)
        col.objects.link(ob);ob.parent=root;parents[key]=ob
    mats={}
    for name,(rgba,metal,rough) in COLORS.items():
        mat=bpy.data.materials.get(PREFIX+name) or bpy.data.materials.new(PREFIX+name)
        mat.use_nodes=True
        bsdf=mat.node_tree.nodes.get("Principled BSDF")
        bsdf.inputs["Base Color"].default_value=rgba
        bsdf.inputs["Metallic"].default_value=metal
        bsdf.inputs["Roughness"].default_value=rough
        if name=="CoolCyan":
            try:
                bsdf.inputs["Emission Color"].default_value=(.025,.16,.24,1)
                bsdf.inputs["Emission Strength"].default_value=.23
            except KeyError:
                pass
        mat.diffuse_color=rgba;mats[name]=mat
    for p in parts:
        mesh=bpy.data.meshes.new(PREFIX+p["name"]+"_MESH")
        mesh.from_pydata(p["vertices"],[],p["faces"])
        mesh.update()
        ob=bpy.data.objects.new(PREFIX+p["name"],mesh)
        col.objects.link(ob);ob.parent=parents[p["parent"]]
        mesh.materials.append(mats[p["material"]])
        if p["bevel"]:
            mod=ob.modifiers.new("Machined soft edges","BEVEL")
            mod.width=p["bevel"];mod.segments=3
            if hasattr(mod,"affect"):mod.affect="EDGES"
            ob.modifiers.new("Weighted vertex normals","WEIGHTED_NORMAL")
        else:
            for face in mesh.polygons:
                face.use_smooth=p["name"].startswith(("SteelRim","RubberTire","CopperHub","StationaryAxle"))
    scene=bpy.context.scene
    scene.frame_start=1;scene.frame_end=FRAMES+1;scene.render.fps=30
    for frame in range(1,FRAMES+2,4):
        phase=PERIMETER*(frame-1)/FRAMES
        for name,ob in parents.items():
            if name=="STATIC":continue
            loc,yaw=group_pose(name,phase)
            ob.location=loc;ob.rotation_mode="XYZ";ob.rotation_euler=(0,yaw,0)
            ob.keyframe_insert(data_path="location",frame=frame)
            ob.keyframe_insert(data_path="rotation_euler",frame=frame)
    for ob in parents.values():
        if ob.animation_data and ob.animation_data.action:
            try:
                for curve in ob.animation_data.action.fcurves:
                    for key in curve.keyframe_points:key.interpolation="LINEAR"
            except (AttributeError,TypeError):
                pass
    scene.frame_set(1)
    bpy.context.view_layer.update()
    out.mkdir(parents=True,exist_ok=True)
    bpy.ops.object.select_all(action="DESELECT")
    for ob in col.objects:ob.select_set(True)
    bpy.context.view_layer.objects.active=root
    glb=out/(ID+"_animated.glb")
    bpy.ops.export_scene.gltf(filepath=str(glb),export_format="GLB",
                              use_selection=True,export_animations=True)
    blend=out/(ID+".blend")
    bpy.ops.wm.save_as_mainfile(filepath=str(blend))
    assert glb.stat().st_size>10000 and blend.stat().st_size>10000
    return dict(backend="blender-bpy",glb=glb.name,blend=blend.name,
                blender="EXECUTED",unity="NOT RUN")

def main():
    args=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else sys.argv[1:]
    parser=argparse.ArgumentParser()
    parser.add_argument("--out",default="build/M02-009")
    parser.add_argument("--self-test",action="store_true")
    parser.add_argument("--static-obj",action="store_true")
    ns=parser.parse_args(args)
    parts=build()
    report=validate(parts)
    if ns.self_test:
        print(json.dumps(report,indent=2));return
    out=Path(ns.out).resolve()
    if ns.static_obj:
        result=static_obj(parts,out)
    else:
        try:import bpy
        except ImportError:result=static_obj(parts,out)
        else:result=blender_export(parts,out)
    report.update(result)
    out.mkdir(parents=True,exist_ok=True)
    (out/(ID+"_qa.json")).write_text(json.dumps(report,indent=2)+"\n",encoding="utf-8")
    print(json.dumps(report,indent=2))

if __name__=="__main__":
    main()
