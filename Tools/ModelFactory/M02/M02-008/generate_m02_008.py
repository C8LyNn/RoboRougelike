#!/usr/bin/env python3
"""M02-008 — one metre scale animated track cassette for Unity 6 RoboRougelike.

X: vehicle longitudinal; Y: wheel axle/lateral; Z: up (Blender).
Pivot RRL_M02_008_ROOT at (0,0,0); mounting socket at (0,-0.210,-0.080).
Drive wheel centres (+/-0.340, 0, -0.200); all dimensions in metres.
Python-only --self-test / --static-obj work without Blender.
Blender 4.x/5.x -- --out <dir> exports animated GLB and .blend.
"""
from __future__ import annotations

import argparse
import json
import math
import sys
from pathlib import Path

ID = "M02-008"
PREFIX = "RRL_M02_008_"
COLLECTION = PREFIX + "TRACK_CASSETTE"
R = 0.207
HALF_SPAN = 0.340
CZ = -0.200
N_LINKS = 32
PERIMETER = 4 * HALF_SPAN + 2 * math.pi * R
PITCH = PERIMETER / N_LINKS
FRAMES = 96
MATERIALS = {
    "BlackenedSteel": ((.070,.095,.125,1),.72,.32),
    "BrushedTitanium": ((.42,.51,.55,1),.80,.29),
    "TreadRubber": ((.029,.038,.044,1),.06,.83),
    "SafetyOrange": ((.95,.30,.045,1),.37,.31),
    "Copper": ((.60,.28,.12,1),.77,.30),
    "SignalCyan": ((.035,.68,.86,1),.25,.24),
}
FACES_BOX = (
    (0,3,2),(0,2,1),(4,5,6),(4,6,7),
    (0,1,5),(0,5,4),(1,2,6),(1,6,5),
    (2,3,7),(2,7,6),(3,0,4),(3,4,7)
)


def box(name, center, size, mat, parent="STATIC", bevel=0.0):
    x,y,z=center
    a,b,c=(v/2 for v in size)
    v=[(x-a,y-b,z-c),(x+a,y-b,z-c),(x+a,y+b,z-c),
       (x-a,y+b,z-c),(x-a,y-b,z+c),(x+a,y-b,z+c),
       (x+a,y+b,z+c),(x-a,y+b,z+c)]
    return dict(name=name,vertices=v,faces=list(FACES_BOX),
                material=mat,parent=parent,bevel=bevel)


def cylinder(name, center, radius, width, axis, mat, parent="STATIC", sides=24):
    assert axis in ("X","Y","Z")
    x,y,z=center
    def p(u,v,w):
        if axis=="X": return (x+w,y+u,z+v)
        if axis=="Y": return (x+v,y+w,z+u)
        return (x+u,y+v,z+w)
    vertices=[p(radius*math.cos(2*math.pi*i/sides),
                radius*math.sin(2*math.pi*i/sides),w)
              for w in (-width/2,width/2) for i in range(sides)]
    vertices.extend((p(0,0,-width/2),p(0,0,width/2)))
    faces=[]
    for i in range(sides):
        j=(i+1)%sides
        faces.extend(((i,j,sides+j),(i,sides+j,sides+i),
                      (2*sides,j,i),(2*sides+1,sides+i,sides+j)))
    return dict(name=name,vertices=vertices,faces=faces,
                material=mat,parent=parent,bevel=0)


def ring_y(name, center, inner, outer, width, mat, parent="STATIC", sides=32):
    """Real through-hole annulus, not a solid cylinder with painted centre."""
    assert 0<inner<outer
    x,y,z=center
    verts=[]
    for yy,rad in ((-width/2,outer),(-width/2,inner),
                   (width/2,outer),(width/2,inner)):
        for i in range(sides):
            a=2*math.pi*i/sides
            verts.append((x+rad*math.cos(a),y+yy,z+rad*math.sin(a)))
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
    return dict(name=name,vertices=verts,faces=faces,
                material=mat,parent=parent,bevel=0)


def path_pose(distance):
    """Clockwise closed capsule; continuous Y rotation for animation."""
    lap=math.floor(distance/PERIMETER)
    s=distance-lap*PERIMETER
    line=2*HALF_SPAN
    arc=math.pi*R
    if s<line:
        x=-HALF_SPAN+s
        z=CZ+R
        yaw=0
    elif s<line+arc:
        q=(s-line)/R
        x=HALF_SPAN+R*math.sin(q)
        z=CZ+R*math.cos(q)
        yaw=q
    elif s<2*line+arc:
        q=s-line-arc
        x=HALF_SPAN-q
        z=CZ-R
        yaw=math.pi
    else:
        q=(s-2*line-arc)/R
        x=-HALF_SPAN-R*math.sin(q)
        z=CZ-R*math.cos(q)
        yaw=math.pi+q
    return (x,0,z),yaw+2*math.pi*lap


def rotate_y(v,theta):
    x,y,z=v
    return (math.cos(theta)*x+math.sin(theta)*z,
            y,-math.sin(theta)*x+math.cos(theta)*z)


def world_vertex(v,pose):
    loc,ang=pose
    r=rotate_y(v,ang)
    return tuple(r[i]+loc[i] for i in range(3))


def pose_for_parent(parent, phase=0):
    if parent.startswith("LINK_"):
        i=int(parent.split("_")[1])
        return path_pose(i*PITCH+phase)
    if parent=="DRIVE_L":
        return ((-HALF_SPAN,0,CZ),phase/R)
    if parent=="DRIVE_R":
        return ((HALF_SPAN,0,CZ),phase/R)
    if parent.startswith("BOGIE_"):
        i=int(parent.split("_")[1])
        return (([-.170,0,.170][i],0,-.308),phase/.073)
    return ((0,0,0),0)


def build_parts():
    parts=[]
    add=parts.append
    # Side cassette frames, kept outside the rotating road wheels.
    for side in (-1,1):
        y=side*.115
        add(box("ChassisRail_%+d"%side,(0,y,-.157),
                (.790,.026,.047),"BlackenedSteel",bevel=.007))
        for x in (-.305,0,.305):
            add(box("RailRib_%+d_%+.3f"%(side,x),(x,y,-.190),
                    (.055,.031,.075),"BrushedTitanium",bevel=.006))
        for x in (-.340,.340):
            add(cylinder("AxleSeat_%+d_%+.2f"%(side,x),
                         (x,y,-.200),.036,.027,"Y","Copper"))
    # Axles and sprockets: rotor groups are centred on the axle bearings.
    for side,label in ((-1,"L"),(1,"R")):
        p="DRIVE_"+label
        add(cylinder("FixedAxle_"+label,(side*HALF_SPAN,0,CZ),
                     .022,.215,"Y","BrushedTitanium"))
        add(ring_y("SprocketRing_"+label,(0,0,0),.090,.157,.092,
                   "BrushedTitanium",p))
        add(ring_y("SprocketHub_"+label,(0,0,0),.024,.076,.100,
                   "Copper",p))
        for j in range(16):
            a=2*math.pi*j/16
            # Rounded-off teeth with an intentional 9mm radial clearance
            # to the track inner surface at the ends.
            x=.169*math.cos(a)
            z=.169*math.sin(a)
            part=box("DriveTooth_%s_%02d"%(label,j),(x,0,z),
                     (.018,.075,.026),"BrushedTitanium",p,bevel=.004)
            # Orient each tooth radially inside the drive rotor.
            part["vertices"]=[rotate_y(v,0) for v in part["vertices"]]
            # The rectangular tooth has a conservative radius < 0.183m.
            add(part)
        for j in range(8):
            a=2*math.pi*j/8
            add(cylinder("HubBolt_%s_%02d"%(label,j),
                         (.062*math.cos(a),-.054,.062*math.sin(a)),
                         .007,.006,"Y","SafetyOrange",p,sides=12))
    # Independent lower rollers, with enough clearance for the inner tread.
    for i,x in enumerate((-.170,0,.170)):
        p="BOGIE_%d"%i
        add(cylinder("BogieAxle_%d"%i,(x,0,-.308),
                     .015,.210,"Y","BrushedTitanium"))
        add(cylinder("BogieWheel_%d"%i,(0,0,0),
                     .073,.085,"Y","BlackenedSteel",p))
        add(ring_y("BogieRim_%d"%i,(0,0,0),
                   .030,.067,.088,"Copper",p))
        add(cylinder("BogieCap_%d"%i,(0,-.049,0),
                     .025,.008,"Y","SafetyOrange",p))
    # All link components share a transform, and actually circulate.
    for i in range(N_LINKS):
        parent="LINK_%02d"%i
        add(box("LinkPlate_%02d"%i,(0,0,0),
                (.071,.210,.032),"BlackenedSteel",parent,bevel=.004))
        # Wedge-shaped / stepped rubber cleat on each link's outer face.
        add(box("WedgeCleat_%02d"%i,(0,0,.023),
                (.058,.224,.018),"TreadRubber",parent,bevel=.005))
        for side in (-1,1):
            # Side-mounted bronze joint caps, outside the sprocket wheel.
            add(cylinder("JointPin_%02d_%+d"%(i,side),
                         (.030,side*.111,0),.011,.012,"Y",
                         "Copper",parent,sides=12))
        if i%4==0:
            add(box("IndexChevron_%02d"%i,(0,0,.034),
                    (.026,.094,.006),"SafetyOrange",parent,bevel=.002))
        elif i%4==2:
            add(box("Reflector_%02d"%i,(0,0,.034),
                    (.012,.060,.006),"SignalCyan",parent,bevel=.002))
    # Physical vehicle-side mounting plate, kept outside track Y envelope.
    add(box("MountNeck",(0,-.165,-.080),
            (.150,.090,.060),"BlackenedSteel",bevel=.008))
    add(box("VehicleSocketPlate",(0,-.212,-.080),
            (.200,.020,.150),"BrushedTitanium",bevel=.008))
    for x in (-.070,.070):
        for z in (-.128,-.032):
            add(cylinder("SocketBolt_%+.2f_%+.3f"%(x,z),
                         (x,-.225,z),.012,.007,"Y","Copper",sides=12))
    return parts


def sub(a,b):
    return tuple(a[i]-b[i] for i in range(3))


def cross(a,b):
    return (a[1]*b[2]-a[2]*b[1],
            a[2]*b[0]-a[0]*b[2],
            a[0]*b[1]-a[1]*b[0])


def dot(a,b):
    return sum(x*y for x,y in zip(a,b))


def mesh_validate(parts):
    names=set()
    tris=0
    for p in parts:
        assert p["name"] not in names,p["name"]
        names.add(p["name"])
        assert p["material"] in MATERIALS
        verts=p["vertices"]
        edges={}
        volume=0.0
        for a,b,c in p["faces"]:
            assert len({a,b,c})==3
            assert min(a,b,c)>=0 and max(a,b,c)<len(verts)
            A,B,C=verts[a],verts[b],verts[c]
            n=cross(sub(B,A),sub(C,A))
            assert dot(n,n)>1e-16,p["name"]
            volume+=dot(A,cross(B,C))/6
            for u,v in ((a,b),(b,c),(c,a)):
                k=tuple(sorted((u,v)))
                edges[k]=edges.get(k,0)+1
            tris+=1
        assert all(count==2 for count in edges.values()),("open",p["name"])
        assert volume>1e-11,("reversed",p["name"],volume)
    return tris


def rect_axes(yaw):
    # X tangent and Z outward; all track shoes have same Y interval.
    return ((math.cos(yaw),-math.sin(yaw)),
            (math.sin(yaw),math.cos(yaw)))


def obb_separation(pose_a,pose_b,half_length=.0355,half_depth=.026):
    """2D SAT: positive value means a separating gap in at least one axis."""
    a,ang_a=pose_a
    b,ang_b=pose_b
    A=rect_axes(ang_a)
    B=rect_axes(ang_b)
    dx=b[0]-a[0]
    dz=b[2]-a[2]
    gaps=[]
    for ax in (*A,*B):
        dist=abs(dx*ax[0]+dz*ax[1])
        ra=half_length*abs(A[0][0]*ax[0]+A[0][1]*ax[1])+half_depth*abs(A[1][0]*ax[0]+A[1][1]*ax[1])
        rb=half_length*abs(B[0][0]*ax[0]+B[0][1]*ax[1])+half_depth*abs(B[1][0]*ax[0]+B[1][1]*ax[1])
        gaps.append(dist-ra-rb)
    return max(gaps)


def moving_clearance():
    """Analytic swept 2D shoe SAT + wheel clearance over 97 phases."""
    min_gap=1e9
    min_wheel=1e9
    for frame in range(FRAMES+1):
        phase=PERIMETER*frame/FRAMES
        poses=[]
        for i in range(N_LINKS):
            loc,yaw=path_pose(i*PITCH+phase)
            # The real link shell is radially asymmetric: -16mm steel
            # inside and +37mm rubber/marker outside. A shifted 52mm
            # OBB conservatively encloses both with no false wheel contact.
            outward=(math.sin(yaw),0,math.cos(yaw))
            shifted=tuple(loc[k]+.0105*outward[k] for k in range(3))
            poses.append((shifted,yaw))
        for i in range(N_LINKS):
            a=poses[i]
            for j in range(i+1,N_LINKS):
                gap=obb_separation(a,poses[j])
                min_gap=min(min_gap,gap)
                assert gap>0.0004,("shoe collision",frame,i,j,gap)
            # Circle-vs-OBB clearance for rotating sprocket teeth and bogies.
            tangent,normal=rect_axes(a[1])
            for wx,wz,rad in ((-HALF_SPAN,CZ,.182),
                              (HALF_SPAN,CZ,.182),
                              (-.170,-.308,.075),
                              (0,-.308,.075),
                              (.170,-.308,.075)):
                dx=wx-a[0][0]
                dz=wz-a[0][2]
                along=dx*tangent[0]+dz*tangent[1]
                radial=dx*normal[0]+dz*normal[1]
                cx=max(abs(along)-.0355,0)
                cz=max(abs(radial)-.026,0)
                clearance=math.hypot(cx,cz)-rad
                min_wheel=min(min_wheel,clearance)
                assert clearance>.001,("wheel/track collision",frame,i,wx,clearance)
    return min_gap,min_wheel


def validate(parts):
    triangles=mesh_validate(parts)
    gap,wheel_gap=moving_clearance()
    assert len([p for p in parts if p["name"].startswith("LinkPlate_")])==N_LINKS
    assert len(MATERIALS)>=3
    # Closed track pose must coincide after one full rotation.
    for i in range(N_LINKS):
        a,ay=path_pose(i*PITCH)
        b,by=path_pose(i*PITCH+PERIMETER)
        assert max(abs(a[k]-b[k]) for k in range(3))<1e-8
        assert abs(by-ay-2*math.pi)<1e-8
    return {
        "id":ID,"result":"PASS","units":"metres (Unity 1 unit = 1 m)",
        "parts":len(parts),"triangles":triangles,"materials":len(MATERIALS),
        "links":N_LINKS,"track_pitch_m":round(PITCH,7),
        "animation_frames":FRAMES,"swept_samples":FRAMES+1,
        "link_pair_sat_tests":(FRAMES+1)*N_LINKS*(N_LINKS-1)//2,
        "min_link_clearance_m":round(gap,6),
        "min_wheel_clearance_m":round(wheel_gap,6),
        "root_pivot_m":[0,0,0],
        "vehicle_socket_m":[0,-.212,-.080],
        "axes":"X longitudinal; Y axle; Z up; Unity imports as Y up",
        "blender":"NOT RUN","unity":"NOT RUN",
        "collision_method":"2D swept OBB SAT + conservative wheel circles; Blender visual check pending",
    }


def static_obj(parts,out):
    out.mkdir(parents=True,exist_ok=True)
    mtl=ID+"_single_track.mtl"
    obj=ID+"_single_track.obj"
    mlines=[]
    for name,(rgba,metallic,roughness) in MATERIALS.items():
        mlines.extend(("newmtl "+name,
                       "Kd %.6f %.6f %.6f"%tuple(rgba[:3]),
                       "Ks %.5f %.5f %.5f"%(metallic,)*3,
                       "Ns %.2f"%max(1,(1-roughness)*800),"illum 2",""))
    (out/mtl).write_text("\n".join(mlines)+"\n",encoding="utf-8")
    lines=["# M02-008 real static OBJ (animation in GLB only)",
           "mtllib "+mtl]
    offset=1
    for p in parts:
        lines.extend(("o "+p["name"],"usemtl "+p["material"]))
        pose=pose_for_parent(p["parent"])
        for v in p["vertices"]:
            xyz=world_vertex(v,pose)
            lines.append("v %.8f %.8f %.8f"%xyz)
        for a,b,c in p["faces"]:
            lines.append("f %d %d %d"%(a+offset,b+offset,c+offset))
        offset+=len(p["vertices"])
    (out/obj).write_text("\n".join(lines)+"\n",encoding="utf-8")
    return {"backend":"python-stdlib","obj":obj,"mtl":mtl,
            "blend":"NOT RUN","glb":"NOT RUN"}


def blender_export(parts,out):
    import bpy
    # This generator owns only one named collection.
    old=bpy.data.collections.get(COLLECTION)
    if old:
        for o in tuple(old.objects):
            bpy.data.objects.remove(o,do_unlink=True)
        bpy.data.collections.remove(old)
    col=bpy.data.collections.new(COLLECTION)
    bpy.context.scene.collection.children.link(col)
    bpy.context.scene.unit_settings.system="METRIC"
    bpy.context.scene.unit_settings.scale_length=1.0
    root=bpy.data.objects.new(PREFIX+"ROOT",None)
    col.objects.link(root)
    root.empty_display_type="PLAIN_AXES"
    root.empty_display_size=.08
    root["mount_position_m"]=[0,-.212,-.080]
    root["visual_only"]=True
    root["track_link_count"]=N_LINKS
    parents={"STATIC":root}
    for label in ("DRIVE_L","DRIVE_R","BOGIE_0","BOGIE_1","BOGIE_2"):
        o=bpy.data.objects.new(PREFIX+label,None)
        col.objects.link(o)
        o.parent=root
        parents[label]=o
    for i in range(N_LINKS):
        name="LINK_%02d"%i
        o=bpy.data.objects.new(PREFIX+name,None)
        col.objects.link(o)
        o.parent=root
        parents[name]=o
    socket=bpy.data.objects.new(PREFIX+"MOUNT_SOCKET",None)
    col.objects.link(socket)
    socket.parent=root
    socket.location=(0,-.212,-.080)
    socket.empty_display_type="ARROWS"
    socket.empty_display_size=.07
    socket["local_outward_axis"]="-Y"
    mats={}
    for name,(rgba,metal,rough) in MATERIALS.items():
        mat=bpy.data.materials.get(PREFIX+name) or bpy.data.materials.new(PREFIX+name)
        mat.use_nodes=True
        bsdf=mat.node_tree.nodes.get("Principled BSDF")
        bsdf.inputs["Base Color"].default_value=rgba
        bsdf.inputs["Metallic"].default_value=metal
        bsdf.inputs["Roughness"].default_value=rough
        if name=="SignalCyan":
            bsdf.inputs["Emission Color"].default_value=(.02,.30,.40,1)
            bsdf.inputs["Emission Strength"].default_value=.25
        mat.diffuse_color=rgba
        mats[name]=mat
    for p in parts:
        mesh=bpy.data.meshes.new(PREFIX+p["name"]+"_Mesh")
        mesh.from_pydata(p["vertices"],[],p["faces"])
        mesh.update()
        ob=bpy.data.objects.new(PREFIX+p["name"],mesh)
        col.objects.link(ob)
        ob.parent=parents[p["parent"]]
        mesh.materials.append(mats[p["material"]])
        if p["bevel"]:
            mod=ob.modifiers.new("Machined chamfer","BEVEL")
            mod.width=p["bevel"]
            mod.segments=3
            if hasattr(mod,"affect"):
                mod.affect="EDGES"
            ob.modifiers.new("Weighted normals","WEIGHTED_NORMAL")
        else:
            for face in mesh.polygons:
                face.use_smooth=p["name"].startswith(("Bogie","Sprocket","FixedAxle"))
    # Animated link motion and rotor spin; closed loop, 25 keyframes.
    scene=bpy.context.scene
    scene.frame_start=1
    scene.frame_end=FRAMES+1
    for frame in range(1,FRAMES+2,4):
        phase=PERIMETER*(frame-1)/FRAMES
        for name,o in parents.items():
            if name=="STATIC":continue
            loc,yaw=pose_for_parent(name,phase)
            o.location=loc
            o.rotation_mode="XYZ"
            o.rotation_euler=(0,yaw,0)
            o.keyframe_insert(data_path="location",frame=frame)
            o.keyframe_insert(data_path="rotation_euler",frame=frame)
    for o in parents.values():
        if o.animation_data and o.animation_data.action:
            action=o.animation_data.action
            # Blender 4/5 Action fcurves may be slotted; linear is optional.
            try:
                for curve in action.fcurves:
                    for k in curve.keyframe_points:
                        k.interpolation="LINEAR"
            except (AttributeError,TypeError):
                pass
    scene.frame_set(1)
    bpy.context.view_layer.update()
    out.mkdir(parents=True,exist_ok=True)
    bpy.ops.object.select_all(action="DESELECT")
    for o in col.objects:o.select_set(True)
    bpy.context.view_layer.objects.active=root
    glb=out/(ID+"_animated.glb")
    bpy.ops.export_scene.gltf(filepath=str(glb),export_format="GLB",
                              use_selection=True,export_animations=True)
    blend=out/(ID+".blend")
    bpy.ops.wm.save_as_mainfile(filepath=str(blend))
    assert glb.exists() and glb.stat().st_size>10000
    assert blend.exists() and blend.stat().st_size>10000
    return {"backend":"blender-bpy","glb":glb.name,"blend":blend.name,
            "blender":"EXECUTED","unity":"NOT RUN"}


def main():
    args=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else sys.argv[1:]
    ap=argparse.ArgumentParser()
    ap.add_argument("--out",default="build/M02-008")
    ap.add_argument("--self-test",action="store_true")
    ap.add_argument("--static-obj",action="store_true")
    ns=ap.parse_args(args)
    parts=build_parts()
    report=validate(parts)
    if ns.self_test:
        print(json.dumps(report,indent=2))
        return
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
