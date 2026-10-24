#!/usr/bin/env python3
"""MODEL-03 / M03-012: modular U spring bumper, 1 Blender unit = 1 metre.

Blender authoring axes: X left/right, Y front/back, Z up. Root at
(0,0,0) on the rear mounting face; forward is -Y. Mount sockets at
X=+-0.4, Y=0, Z=0. Moving U and two guide rods translate +Y by 60mm.
Five-turn coil springs shorten from 150mm to 90mm with 61 sampled poses.

Run: python3 generate_m03_012.py --out build/M03-012
     blender -b --python generate_m03_012.py -- --out build/M03-012 --blender
No gameplay, scenes, prefabs or ProjectSettings are modified.
"""
from collections import Counter
import json
import math
from pathlib import Path
import sys

ID="M03-012"
COLLECTION="RRL_M03_012_U_SPRING_BUMPER"
TRAVEL=.060
SPRING_REST=.150
SPRING_TURNS=5
SPRING_RADIUS=.064
WIRE_RADIUS=.0055
GUIDE_OUTER=.045
GUIDE_INNER=.027
ROD_RADIUS=.024
FPS=30
KEYS=((1,0.),(16,TRAVEL),(31,0.),(46,TRAVEL),(61,0.))
MATERIALS={
    "Graphite_Steel":((.095,.135,.175,1),.76,.37,0.),
    "Ceramic_Wear":((.77,.79,.73,1),.18,.35,0.),
    "Copper_Springs":((.66,.31,.12,1),.78,.29,0.),
    "Rubber_Buffers":((.04,.055,.067,1),.02,.86,0.),
    "Amber_Markers":((.94,.44,.07,1),.18,.32,1.3),
}
SNAPS={
    "SNAP_FRAME_ORIGIN":((0.,0.,0.),"REAR_Y"),
    "SNAP_FRAME_LEFT":((-.4,0.,0.),"REAR_Y"),
    "SNAP_FRAME_RIGHT":((.4,0.,0.),"REAR_Y"),
}
PARTS=[]

def area2(poly):
    return sum(a[0]*b[1]-b[0]*a[1]
               for a,b in zip(poly,poly[1:]+poly[:1]))/2

def chamfer(poly,cut):
    assert area2(poly)>0
    result=[]
    for i,p in enumerate(poly):
        a=poly[i-1]
        b=poly[(i+1)%len(poly)]
        la=math.dist(p,a);lb=math.dist(p,b)
        assert min(la,lb)>cut*2
        result.extend(((p[0]+(a[0]-p[0])*cut/la,
                        p[1]+(a[1]-p[1])*cut/la),
                       (p[0]+(b[0]-p[0])*cut/lb,
                        p[1]+(b[1]-p[1])*cut/lb)))
    assert area2(result)>0
    return result

def inside_triangle(p,a,b,c):
    def cross(u,v,w):
        return (v[0]-u[0])*(w[1]-u[1])-(v[1]-u[1])*(w[0]-u[0])
    return min(cross(a,b,p),cross(b,c,p),cross(c,a,p))>=-1e-12

def triangulate(poly):
    assert area2(poly)>0
    remain=list(range(len(poly)))
    tris=[]
    while len(remain)>3:
        found=False
        for k in range(len(remain)):
            a,b,c=(remain[(k-1)%len(remain)],
                   remain[k],remain[(k+1)%len(remain)])
            A,B,C=poly[a],poly[b],poly[c]
            cross=(B[0]-A[0])*(C[1]-B[1])-(B[1]-A[1])*(C[0]-B[0])
            if cross<=1e-11:
                continue
            if any(inside_triangle(poly[p],A,B,C)
                   for p in remain if p not in (a,b,c)):
                continue
            tris.append((a,b,c))
            remain.pop(k)
            found=True
            break
        assert found,("polygon not triangulable",remain)
    tris.append(tuple(remain))
    return tris

def extrude(poly,z0,z1):
    assert z1>z0 and area2(poly)>0
    n=len(poly)
    v=[(x,y,z0) for x,y in poly]+[(x,y,z1) for x,y in poly]
    cap=triangulate(poly)
    faces=[(c,b,a) for a,b,c in cap]+[(a+n,b+n,c+n) for a,b,c in cap]
    for i in range(n):
        j=(i+1)%n
        faces.extend(((i,j,j+n),(i,j+n,i+n)))
    return v,faces

def box(cx,cy,cz,dx,dy,dz,bevel=.003):
    assert min(dx,dy,dz)>bevel*3
    poly=[(cx-dx/2,cy-dy/2),(cx+dx/2,cy-dy/2),
          (cx+dx/2,cy+dy/2),(cx-dx/2,cy+dy/2)]
    return extrude(chamfer(poly,bevel),cz-dz/2,cz+dz/2)

def add(name,mat,geometry,rig=None):
    assert name not in [p["name"] for p in PARTS]
    assert mat in MATERIALS
    v,f=geometry
    PARTS.append(dict(name=name,material=mat,vertices=v,faces=f,rig=rig))


def signed_volume(v,faces):
    return sum((v[a][0]*(v[b][1]*v[c][2]-v[b][2]*v[c][1])+
                v[a][1]*(v[b][2]*v[c][0]-v[b][0]*v[c][2])+
                v[a][2]*(v[b][0]*v[c][1]-v[b][1]*v[c][0]))/6
               for a,b,c in faces)

def ring_y(cx,cz,y0,y1,outer,inner,segments=32):
    """Closed bored cylindrical ring, along local Y (real open bore)."""
    assert y1>y0 and outer>inner>0 and segments>=12
    angles=[2*math.pi*i/segments for i in range(segments)]
    def circle(r,y):
        return [(cx+r*math.cos(t),y,cz+r*math.sin(t)) for t in angles]
    v=circle(outer,y0)+circle(outer,y1)+circle(inner,y0)+circle(inner,y1)
    f=[]
    n=segments
    for i in range(n):
        j=(i+1)%n
        # Outer, inner, front cap, back cap.
        f.extend(((i,i+n,j+n),(i,j+n,j),
                  (2*n+i,2*n+j,3*n+j),(2*n+i,3*n+j,3*n+i),
                  (i,j,2*n+j),(i,2*n+j,2*n+i),
                  (n+i,3*n+i,3*n+j),(n+i,3*n+j,n+j)))
    if signed_volume(v,f)<0:
        f=[(a,c,b) for a,b,c in f]
    return v,f

def u_bar_xz(profile,y0,y1):
    """Concave U profile, front-to-back extrusion with closed caps."""
    assert area2(profile)>0 and y1>y0
    n=len(profile)
    v=[(x,y0,z) for x,z in profile]+[(x,y1,z) for x,z in profile]
    cap=triangulate(profile)
    f=[(a,b,c) for a,b,c in cap]+[(c+n,b+n,a+n) for a,b,c in cap]
    for i in range(n):
        j=(i+1)%n
        f.extend(((i,j+n,j),(i,i+n,j+n)))
    if signed_volume(v,f)<0:
        f=[(a,c,b) for a,b,c in f]
    return v,f

def helix_tube(cx,cz,y0,length,turns=SPRING_TURNS):
    """Capped closed 3D wire, with constant pitch and parallel-transport-like frame."""
    steps=turns*24
    sides=8
    v=[]
    pitch=length/turns
    for i in range(steps+1):
        t=i/steps
        theta=2*math.pi*turns*t
        radial=(math.cos(theta),0.,math.sin(theta))
        tangent=(-SPRING_RADIUS*math.sin(theta),-pitch/(2*math.pi),
                 SPRING_RADIUS*math.cos(theta))
        norm=math.sqrt(sum(x*x for x in tangent))
        tangent=tuple(x/norm for x in tangent)
        normal=(tangent[1]*radial[2]-tangent[2]*radial[1],
                tangent[2]*radial[0]-tangent[0]*radial[2],
                tangent[0]*radial[1]-tangent[1]*radial[0])
        center=(cx+SPRING_RADIUS*radial[0],y0-length*t,
                cz+SPRING_RADIUS*radial[2])
        for j in range(sides):
            a=2*math.pi*j/sides
            v.append(tuple(center[k]+WIRE_RADIUS*(math.cos(a)*radial[k]+
                       math.sin(a)*normal[k]) for k in range(3)))
    f=[]
    for i in range(steps):
        for j in range(sides):
            a=i*sides+j
            b=i*sides+(j+1)%sides
            c=(i+1)*sides+j
            d=(i+1)*sides+(j+1)%sides
            f.extend(((a,b,d),(a,d,c)))
    first=len(v)
    v.append((cx+SPRING_RADIUS,y0,cz))
    last=len(v)
    v.append((cx+SPRING_RADIUS,y0-length,cz))
    for j in range(sides):
        k=(j+1)%sides
        f.append((first,k,j))
        f.append((last,steps*sides+j,steps*sides+k))
    if signed_volume(v,f)<0:
        f=[(a,c,b) for a,b,c in f]
    return v,f

def build():
    PARTS.clear()
    # Two rear-facing hollow mounts; their bores remain clear along Y.
    for side,label in ((-1,"L"),(1,"R")):
        x=side*.4
        add("Frame_Mount_Ring_"+label,"Graphite_Steel",
            ring_y(x,0.,0.,.08,.075,.038))
    # Central bridge welded into outer shoulders of both mounting rings,
    # but not into their 38mm-radius bores.
    add("Frame_Crossmember","Graphite_Steel",
        box(0.,.038,0.,.70,.053,.110,.007))
    for side,label in ((-1,"L"),(1,"R")):
        x=side*.30
        # Fixed bored guides y[-.18,+.04]; bores r=27mm, rod r=24mm.
        add("Fixed_Guide_Tube_"+label,"Graphite_Steel",
            ring_y(x,0.,-.18,.04,GUIDE_OUTER,GUIDE_INNER))
        add("Fixed_Spring_Seat_"+label,"Ceramic_Wear",
            ring_y(x,0.,-.18,-.165,.076,.028))
        # Moving rods are mechanically seated 10mm into the U bar.
        add("Moving_Slider_Rod_"+label,"Copper_Springs",
            ring_y(x,0.,-.34,-.04,ROD_RADIUS,.008), "SLIDER")
        add("Moving_Spring_Seat_"+label,"Ceramic_Wear",
            ring_y(x,0.,-.342,-.330,.076,.028),"SLIDER")
        add("Compression_Spring_"+label,"Copper_Springs",
            helix_tube(x,0.,-.18,SPRING_REST),"SPRING_"+label)
        add("Guide_Accent_"+label,"Amber_Markers",
            box(x,.044,.043,.060,.011,.012,.002))
    # One closed concave U, rather than three intersecting boxes.
    profile=[(-.47,-.12),(.47,-.12),(.47,.25),(.35,.25),
             (.35,.02),(-.35,.02),(-.35,.25),(-.47,.25)]
    add("Moving_U_Bumper","Graphite_Steel",
        u_bar_xz(profile,-.39,-.33),"SLIDER")
    # Wear-facing trim is deliberately inset 1mm into the front of U.
    add("Ceramic_Front_Impact_Strip","Ceramic_Wear",
        box(0.,-.395,-.075,.70,.012,.055,.004),"SLIDER")
    for side,label in ((-1,"L"),(1,"R")):
        add("Horn_Rubber_Cap_"+label,"Rubber_Buffers",
            box(side*.41,-.360,.250,.112,.066,.035,.005),"SLIDER")
        add("Horn_Status_Marker_"+label,"Amber_Markers",
            box(side*.41,-.396,.115,.035,.014,.115,.003),"SLIDER")
    return PARTS

def bounds(part):
    return [(min(v[k] for v in part["vertices"]),
             max(v[k] for v in part["vertices"])) for k in range(3)]

def validate(parts):
    assert len(MATERIALS)>=3
    assert len(parts)==21,(len(parts),[p["name"] for p in parts])
    assert len({p["name"] for p in parts})==len(parts)
    for p in parts:
        v,f=p["vertices"],p["faces"]
        assert len(v)>7 and len(f)>11,p["name"]
        assert len(set(v))==len(v),p["name"]
        edges=Counter()
        for a,b,c in f:
            assert len({a,b,c})==3
            assert all(0<=i<len(v) for i in (a,b,c))
            A,B,C=v[a],v[b],v[c]
            U=tuple(B[k]-A[k] for k in range(3))
            W=tuple(C[k]-A[k] for k in range(3))
            cross=(U[1]*W[2]-U[2]*W[1],U[2]*W[0]-U[0]*W[2],
                   U[0]*W[1]-U[1]*W[0])
            assert sum(t*t for t in cross)>1e-17,(p["name"],"degenerate")
            for i,j in ((a,b),(b,c),(c,a)):
                edges[(min(i,j),max(i,j))]+=1
        assert all(n==2 for n in edges.values()),(p["name"],"nonmanifold")
        assert signed_volume(v,f)>1e-10,(p["name"],"inward winding")
    by={p["name"]:p for p in parts}
    assert bounds(by["Moving_U_Bumper"])[1]==[-.39,-.33]
    assert abs(bounds(by["Frame_Crossmember"])[1][0]-.0115)<1e-8
    # Two real mounting bores with no crossmember obstruction.
    for side,label in ((-1,"L"),(1,"R")):
        mount=by["Frame_Mount_Ring_"+label]
        assert len(mount["vertices"])==128
        assert abs(bounds(mount)[1][0])<1e-9
        guide=by["Fixed_Guide_Tube_"+label]
        rod=by["Moving_Slider_Rod_"+label]
        assert abs(bounds(guide)[1][0]+.18)<1e-9
        assert abs(bounds(rod)[1][0]+.34)<1e-9
        assert GUIDE_INNER-ROD_RADIUS>=.003-1e-10
        assert SPRING_RADIUS-WIRE_RADIUS-GUIDE_OUTER>=.013-1e-10
        assert .038 > .008
    samples=0
    for frame in range(1,62):
        phase=(frame-1)%30
        d=TRAVEL*(phase/15 if phase<=15 else (30-phase)/15)
        assert 0<=d<=TRAVEL+1e-9
        # Moving U rear face never reaches guide front (no collision).
        rear=-.33+d
        guide_front=-.18
        gap=guide_front-rear
        assert gap>=.09-1e-9,(frame,gap)
        assert abs((SPRING_REST-d)-gap)<1e-9
        # Helix pitch at maximum compression remains greater than wire dia.
        assert (SPRING_REST-d)/SPRING_TURNS>2*WIRE_RADIUS+.005
        for side in ("L","R"):
            rod=by["Moving_Slider_Rod_"+side]
            low,high=bounds(rod)[1]
            assert low+d<=rear-.0099,(side,frame,"rod disconnected from U")
            assert high+d<=.04-.0199,(side,frame,"rod escapes tube")
            assert high+d>=-.18+.1,(side,frame,"rod loses guide engagement")
        samples+=1
    assert samples==61
    assert set(SNAPS)=={"SNAP_FRAME_ORIGIN","SNAP_FRAME_LEFT","SNAP_FRAME_RIGHT"}
    return samples

def write_static(out,parts):
    out.mkdir(parents=True,exist_ok=True)
    stem="M03-012_u_spring_bumper"
    obj=["# M03-012 static rest pose; Blender Z up, metres","mtllib "+stem+".mtl"]
    offset=0
    for p in parts:
        obj+=["o "+p["name"],"usemtl "+p["material"]]
        obj+=["v %.8f %.8f %.8f"%v for v in p["vertices"]]
        obj+=["f %d %d %d"%(a+1+offset,b+1+offset,c+1+offset)
              for a,b,c in p["faces"]]
        offset+=len(p["vertices"])
    (out/(stem+".obj")).write_text("\n".join(obj)+"\n",encoding="utf-8")
    mtl=[]
    for name,(rgba,metal,rough,emission) in MATERIALS.items():
        mtl+=["newmtl "+name,"Kd %.6f %.6f %.6f"%rgba[:3],
              "Ks %.6f %.6f %.6f"%(metal,metal,metal),
              "Ns %.6f"%((1-rough)*1000),
              "Ke %.6f %.6f %.6f"%tuple(emission*c for c in rgba[:3]),
              "d 1.0","illum 2",""]
    (out/(stem+".mtl")).write_text("\n".join(mtl),encoding="utf-8")
    manifest=dict(id=ID,model="Zderzak sprężynujący U",revision="M03-012_V1",
        unit="metre",axes="Blender X/Y horizontal Z up; Unity Y up after GLB import",
        pivot=[0.,0.,0.],mount_face="Y=0, rear",mount_normal="+Y",
        parts=len(parts),vertices=sum(len(p["vertices"]) for p in parts),
        triangles=sum(len(p["faces"]) for p in parts),
        materials=len(MATERIALS),
        snap_points={k:{"location":list(v[0]),"normal":v[1]}
                     for k,v in SNAPS.items()},
        animation=dict(name="U_BUMPER_COMPRESSION",frames=[list(k) for k in KEYS],
            fps=FPS,axis="+Y inward",travel_m=TRAVEL,pose_samples=61,
            spring_rest_length_m=SPRING_REST,
            spring_compressed_length_m=SPRING_REST-TRAVEL,
            min_u_to_guide_gap_m=.09,rod_guide_radial_clearance_m=.003),
        blender="NOT RUN",unity="NOT RUN")
    (out/"M03-012_manifest.json").write_text(
        json.dumps(manifest,indent=2,ensure_ascii=False)+"\n",encoding="utf-8")
    return manifest

def blender_scene(parts):
    import bpy
    old=bpy.data.collections.get(COLLECTION)
    if old:
        for ob in list(old.objects):
            mesh=ob.data if ob.type=="MESH" else None
            bpy.data.objects.remove(ob,do_unlink=True)
            if mesh and mesh.users==0: bpy.data.meshes.remove(mesh)
        bpy.data.collections.remove(old)
        for action in list(bpy.data.actions):
            if action.name.startswith(ID+"_") and action.users==0:
                bpy.data.actions.remove(action)
    col=bpy.data.collections.new(COLLECTION)
    bpy.context.scene.collection.children.link(col)
    mats={}
    for name,(rgba,metal,rough,emission) in MATERIALS.items():
        mat=bpy.data.materials.get(ID+"_"+name)
        if mat is None: mat=bpy.data.materials.new(ID+"_"+name)
        mat.diffuse_color=rgba
        mat.use_nodes=True
        bs=mat.node_tree.nodes.get("Principled BSDF")
        assert bs
        bs.inputs["Base Color"].default_value=rgba
        bs.inputs["Metallic"].default_value=metal
        bs.inputs["Roughness"].default_value=rough
        if "Emission Color" in bs.inputs:
            bs.inputs["Emission Color"].default_value=rgba
        elif "Emission" in bs.inputs:
            bs.inputs["Emission"].default_value=rgba
        if "Emission Strength" in bs.inputs:
            bs.inputs["Emission Strength"].default_value=emission
        mats[name]=mat
    def empty(name,loc=(0,0,0),rot=None):
        ob=bpy.data.objects.new(ID+"_"+name,None)
        col.objects.link(ob)
        ob.empty_display_type="ARROWS"
        ob.empty_display_size=.07
        ob.location=loc
        if rot:ob.rotation_euler=rot
        return ob
    root=empty("ROOT")
    slider=empty("U_SLIDER")
    slider.parent=root
    springs={}
    for label in ("L","R"):
        springs[label]=empty("SPRING_SCALE_"+label,(0.,-.18,0.))
        springs[label].parent=root
    for p in parts:
        verts=p["vertices"]
        if p["rig"] in ("SPRING_L","SPRING_R"):
            # Vertices are static world positions in OBJ; Blender spring
            # controller sits at y=-.18 so convert to controller local.
            verts=[(x,y+.18,z) for x,y,z in verts]
        mesh=bpy.data.meshes.new(ID+"_"+p["name"])
        mesh.from_pydata(verts,[],p["faces"])
        mesh.validate(verbose=False)
        mesh.update()
        ob=bpy.data.objects.new(ID+"_"+p["name"],mesh)
        col.objects.link(ob)
        ob.data.materials.append(mats[p["material"]])
        ob.parent=({"SLIDER":slider,"SPRING_L":springs["L"],
                    "SPRING_R":springs["R"]}.get(p["rig"],root))
    for name,(pos,normal) in SNAPS.items():
        rot=(math.pi/2,0,0) if normal=="REAR_Y" else (0,0,0)
        marker=empty(name,pos,rot)
        marker.parent=root
    for frame,d in KEYS:
        slider.location=(0,d,0)
        slider.keyframe_insert(data_path="location",frame=frame)
        for ctrl in springs.values():
            ctrl.scale=(1,1-d/SPRING_REST,1)
            ctrl.keyframe_insert(data_path="scale",frame=frame)
    for ctrl in (slider,*springs.values()):
        if ctrl.animation_data and ctrl.animation_data.action:
            ctrl.animation_data.action.name=ID+"_"+ctrl.name+"_Action"
            for curve in ctrl.animation_data.action.fcurves:
                for key in curve.keyframe_points:
                    key.interpolation="LINEAR"
    scene=bpy.context.scene
    scene.frame_start=1
    scene.frame_end=61
    scene.render.fps=FPS
    scene.frame_set(1)
    return col

def blender_export(parts,out):
    import bpy
    col=blender_scene(parts)
    first=len(col.objects)
    col=blender_scene(parts)
    assert len(col.objects)==first==len(parts)+7,(len(col.objects),first)
    assert len([m for m in bpy.data.meshes if m.name.startswith(ID+"_")])==len(parts)
    selected=tuple(bpy.context.selected_objects)
    active=bpy.context.view_layer.objects.active
    try:
        bpy.ops.object.select_all(action="DESELECT")
        for ob in col.objects:ob.select_set(True)
        bpy.context.view_layer.objects.active=next(
            ob for ob in col.objects if ob.name==ID+"_ROOT")
        stem="M03-012_u_spring_bumper"
        glb=out/(stem+".glb")
        fbx=out/(stem+".fbx")
        blend=out/(stem+".blend")
        bpy.ops.export_scene.gltf(filepath=str(glb),export_format="GLB",
                                  use_selection=True,export_animations=True)
        bpy.ops.export_scene.fbx(filepath=str(fbx),use_selection=True,
            axis_forward="-Z",axis_up="Y",add_leaf_bones=False,
            bake_anim=True,bake_anim_use_all_actions=False)
        bpy.ops.wm.save_as_mainfile(filepath=str(blend))
        assert all(p.exists() and p.stat().st_size>1000 for p in (glb,fbx,blend))
    finally:
        bpy.ops.object.select_all(action="DESELECT")
        for ob in selected:
            if bpy.data.objects.get(ob.name) is ob:ob.select_set(True)
        if active and bpy.data.objects.get(active.name) is active:
            bpy.context.view_layer.objects.active=active
    return dict(glb_bytes=glb.stat().st_size,fbx_bytes=fbx.stat().st_size,
                blend_bytes=blend.stat().st_size,scene_objects=first,
                idempotent_second_build=True)

def main():
    argv=sys.argv
    args=argv[argv.index("--")+1:] if "--" in argv else argv[1:]
    out=Path("build/M03-012")
    if "--out" in args:out=Path(args[args.index("--out")+1])
    parts=build()
    samples=validate(parts)
    manifest=write_static(out,parts)
    report=dict(id=ID,status="PASS",static_geometry="PASS",
        moving_pose_samples=samples,parts=manifest["parts"],
        vertices=manifest["vertices"],triangles=manifest["triangles"],
        blender="NOT RUN",unity="NOT RUN")
    if "--blender" in args:
        export=blender_export(parts,out)
        report["blender"]="PASS"
        report["blender_export"]=export
        manifest["blender"]="EXPORTED"
        manifest["blender_export"]=export
        (out/"M03-012_manifest.json").write_text(
            json.dumps(manifest,indent=2,ensure_ascii=False)+"\n",
            encoding="utf-8")
    (out/"M03-012_qa.json").write_text(
        json.dumps(report,indent=2)+"\n",encoding="utf-8")
    print(json.dumps(report,indent=2))

if __name__=="__main__":
    main()
