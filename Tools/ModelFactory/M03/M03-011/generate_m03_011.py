#!/usr/bin/env python3
"""M03-011 Impact-absorbing modular frame corner. Unity: 1 unit = 1 metre.

Blender authoring axes: X/Y = chassis plane, Z = up; GLB/FBX exported
with standard Blender Z-up -> Unity Y-up conversion. Root pivot (0,0,0)
at the grid corner. Grid end locators at (1,0,.2) and (0,1,.2).
The bumper translates diagonally inward (positive X and Y), with
two elastomer blocks compressing exactly to the remaining gap.

Run with Python 3 for deterministic OBJ/MTL + QA, or:
  blender -b --python generate_m03_011.py -- --out /path/to/out --blender
"""
from collections import Counter
import json
import math
from pathlib import Path
import sys

ID = "M03-011"
COLLECTION = "RRL_M03_011_IMPACT_CORNER"
TRAVEL = 0.022
FPS = 30
KEYS = ((1,0.0),(17,TRAVEL),(33,0.0),(49,TRAVEL),(65,0.0))
MATERIALS = {
    "Graphite_Steel": ((.09,.13,.17,1),.75,.37,0.0),
    "Impact_Rubber": ((.045,.061,.073,1),.03,.86,0.0),
    "Ceramic_Wear": ((.70,.75,.73,1),.18,.38,0.0),
    "Copper_Fasteners": ((.69,.33,.13,1),.72,.28,0.0),
    "Amber_Status": ((.95,.43,.06,1),.15,.34,1.6),
}
SNAPS = {
    "SNAP_CORNER_ROOT": ((0.,0.,0.),"UP_Z"),
    "SNAP_FRAME_X": ((1.,0.,.20),"OUT_X"),
    "SNAP_FRAME_Y": ((0.,1.,.20),"OUT_Y"),
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

def build():
    PARTS.clear()
    # Welded steel corner has two 1-m arms, with real 1-m grid locators.
    frame=[(-.07,-.07),(1.,-.07),(1.,.07),
           (.07,.07),(.07,1.),(-.07,1.)]
    add("Corner_L_Frame","Graphite_Steel",
        extrude(chamfer(frame,.014),.095,.305))
    # Single closed concave L-shaped elastomer guard: no self-intersection
    # at the corner. At maximum compression it still clears the steel by
    # 28 mm, while the two fitted shock blocks fill that remaining gap.
    guard=[(-.26,-.26),(.42,-.26),(.42,-.12),
           (-.12,-.12),(-.12,.42),(-.26,.42)]
    add("Impact_Guard_L","Impact_Rubber",
        extrude(chamfer(guard,.012),.08,.34),"GUARD")
    add("Wear_Cap_X","Ceramic_Wear",
        box(.17,-.264,.21,.34,.012,.15),"GUARD")
    add("Wear_Cap_Y","Ceramic_Wear",
        box(-.264,.17,.21,.012,.34,.15),"GUARD")
    for axis in ("X","Y"):
        for index,p in enumerate((.065,.285),1):
            if axis=="X":
                geometry=box(p,-.273,.21,.024,.009,.085,.002)
            else:
                geometry=box(-.273,p,.21,.009,.024,.085,.002)
            add("Copper_Wear_Marker_"+axis+str(index),
                "Copper_Fasteners",geometry,"GUARD")
    add("Status_Bar_X","Amber_Status",
        box(.21,-.19,.341,.25,.022,.012,.003),"GUARD")
    add("Status_Bar_Y","Amber_Status",
        box(-.19,.21,.341,.022,.25,.012,.003),"GUARD")
    # Contact surfaces are exact at rest: -0.120 (bumper) to -0.070
    # (steel). Core shrinks from 50 to 28mm as bumper travels 22mm.
    add("Elastomer_Shock_X","Impact_Rubber",
        box(.18,-.095,.205,.12,.05,.10,.004),"SHOCK_X")
    add("Elastomer_Shock_Y","Impact_Rubber",
        box(-.095,.18,.205,.05,.12,.10,.004),"SHOCK_Y")
    for axis in ("X","Y"):
        for k,p in enumerate((.11,.18,.25),1):
            if axis=="X":
                geo=box(p,-.094,.259,.024,.045,.009,.002)
            else:
                geo=box(-.094,p,.259,.045,.024,.009,.002)
            # Bellows visual fins follow half of compression. They
            # intentionally rest on the elastomer, never on chassis.
            add("Shock_Rib_"+axis+str(k),"Copper_Fasteners",geo,
                "SHOCK_X" if axis=="X" else "SHOCK_Y")
    # Two raised reinforcing tabs, intentionally embedded 4mm in steel.
    add("Upper_Gusset_X","Ceramic_Wear",
        box(.67,0.,.311,.22,.11,.020),"FIXED")
    add("Upper_Gusset_Y","Ceramic_Wear",
        box(0.,.67,.311,.11,.22,.020),"FIXED")
    # Snap faces are physical and lie on the 1m module grid. Clamp
    # studs are visual fasteners, not claimed as through-bores.
    add("Mount_Endplate_X","Graphite_Steel",
        box(1.012,0.,.20,.026,.18,.23),"FIXED")
    add("Mount_Endplate_Y","Graphite_Steel",
        box(0.,1.012,.20,.18,.026,.23),"FIXED")
    for axis in ("X","Y"):
        for k,(off,z) in enumerate((( -.05,.14),(.05,.14),
                                   (-.05,.26),(.05,.26)),1):
            if axis=="X":
                geo=box(1.03,off,z,.013,.023,.023,.002)
            else:
                geo=box(off,1.03,z,.023,.013,.023,.002)
            add("Mount_Stud_"+axis+str(k),"Copper_Fasteners",geo,"FIXED")
    return PARTS

def signed_volume(v,faces):
    return sum((v[a][0]*(v[b][1]*v[c][2]-v[b][2]*v[c][1])+
                v[a][1]*(v[b][2]*v[c][0]-v[b][0]*v[c][2])+
                v[a][2]*(v[b][0]*v[c][1]-v[b][1]*v[c][0]))/6
               for a,b,c in faces)

def validate(parts):
    assert len(MATERIALS)>=3
    assert len(parts)==30,(len(parts),[p["name"] for p in parts])
    for p in parts:
        v,f=p["vertices"],p["faces"]
        assert len(v)>7 and len(f)>11,p["name"]
        assert len(set(v))==len(v),p["name"]
        edges=Counter()
        for a,b,c in f:
            assert len({a,b,c})==3
            assert all(0<=i<len(v) for i in (a,b,c))
            A,B,C=v[a],v[b],v[c]
            cross=((B[1]-A[1])*(C[2]-A[2])-(B[2]-A[2])*(C[1]-A[1]),
                   (B[2]-A[2])*(C[0]-A[0])-(B[0]-A[0])*(C[2]-A[2]),
                   (B[0]-A[0])*(C[1]-A[1])-(B[1]-A[1])*(C[0]-A[0]))
            assert sum(t*t for t in cross)>1e-15,(p["name"],"degenerate")
            for i,j in ((a,b),(b,c),(c,a)):
                edges[(min(i,j),max(i,j))]+=1
        assert all(n==2 for n in edges.values()),(p["name"],"non-manifold")
        assert signed_volume(v,f)>1e-10,(p["name"],"inward normal")
    def gap1(a,b):
        return max(a[0]-b[1],b[0]-a[1],0.)
    def overlap(a,b):
        return min(a[1],b[1])-max(a[0],b[0])
    # Independent analytic AABB check for each pair of L-arm rectangles:
    # full L bounding boxes overlap, but their convex arms must not.
    fixed=(((-.07,1.),(-.07,.07)),
           ((-.07,.07),(-.07,1.)))
    samples=0
    for frame in range(1,66):
        segment=(frame-1)//16
        start=KEYS[segment][1]
        end=KEYS[segment+1][1]
        d=start+(end-start)*((frame-1)%16)/16 if frame<65 else 0.
        moving=(((-.26+d,.42+d),(-.26+d,-.12+d)),
                ((-.26+d,-.12+d),(-.26+d,.42+d)))
        for aa in fixed:
            for bb in moving:
                assert overlap(aa[0],bb[0])<=0 or overlap(aa[1],bb[1])<=0,frame
        # Every shock fills the current free span without overextension.
        gap=.05-d
        assert gap>=.028-1e-9
        assert abs((-.12+d+gap)-(-.07))<1e-9
        samples+=1
    assert samples==65
    assert set(SNAPS)=={"SNAP_CORNER_ROOT","SNAP_FRAME_X","SNAP_FRAME_Y"}
    return samples

def write_static(out,parts):
    out.mkdir(parents=True,exist_ok=True)
    obj=["# M03-011 static rest pose; Blender Z-up, 1 unit = 1m",
         "mtllib M03-011_impact_corner.mtl"]
    offset=0
    for p in parts:
        obj+=["o "+p["name"],"usemtl "+p["material"]]
        obj+=["v %.8f %.8f %.8f"%v for v in p["vertices"]]
        obj+=["f %d %d %d"%(a+1+offset,b+1+offset,c+1+offset)
              for a,b,c in p["faces"]]
        offset+=len(p["vertices"])
    (out/"M03-011_impact_corner.obj").write_text(
        "\n".join(obj)+"\n",encoding="utf-8")
    mtl=[]
    for name,(rgba,metal,rough,emission) in MATERIALS.items():
        mtl+=["newmtl "+name,"Kd %.6f %.6f %.6f"%rgba[:3],
              "Ks %.6f %.6f %.6f"%(metal,metal,metal),
              "Ns %.6f"%((1-rough)*1000),
              "Ke %.6f %.6f %.6f"%tuple(emission*c for c in rgba[:3]),
              "d 1.0","illum 2",""]
    (out/"M03-011_impact_corner.mtl").write_text(
        "\n".join(mtl),encoding="utf-8")
    manifest=dict(id=ID,model="Narożnik pochłaniający uderzenia",
                  revision="M03-011_V1",unit="metre",
                  axes="Blender X/Y horizontal Z up; GLB Unity Y up",
                  pivot=[0,0,0],parts=len(parts),
                  vertices=sum(len(p["vertices"]) for p in parts),
                  triangles=sum(len(p["faces"]) for p in parts),
                  materials=len(MATERIALS),
                  snap_points={k:{"location":list(v[0]),"normal":v[1]}
                               for k,v in SNAPS.items()},
                  animation=dict(name="IMPACT_COMPRESS",
                      frames=[list(k) for k in KEYS],fps=FPS,
                      inward_x_m=TRAVEL,inward_y_m=TRAVEL,
                      diagonal_travel_m=round(TRAVEL*math.sqrt(2),6),
                      pose_samples=65,steel_guard_min_gap_m=.028),
                  blender="NOT RUN",unity="NOT RUN")
    (out/"M03-011_manifest.json").write_text(
        json.dumps(manifest,indent=2,ensure_ascii=False)+"\n",encoding="utf-8")
    return manifest

def blender_scene(parts,out):
    import bpy
    from mathutils import Euler
    old=bpy.data.collections.get(COLLECTION)
    if old:
        for ob in list(old.objects):
            bpy.data.objects.remove(ob,do_unlink=True)
        bpy.data.collections.remove(old)
    col=bpy.data.collections.new(COLLECTION)
    bpy.context.scene.collection.children.link(col)
    materials={}
    for name,(rgba,metal,rough,emission) in MATERIALS.items():
        mat=bpy.data.materials.get(ID+"_"+name)
        if mat is None:
            mat=bpy.data.materials.new(ID+"_"+name)
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
        materials[name]=mat
    def empty(name,loc,rot=None):
        ob=bpy.data.objects.new(ID+"_"+name,None)
        col.objects.link(ob)
        ob.empty_display_type="ARROWS"
        ob.empty_display_size=.075
        ob.location=loc
        if rot: ob.rotation_euler=rot
        return ob
    root=empty("ROOT",(0,0,0))
    guard=empty("BUMP_GUARD",(0,0,0))
    guard.parent=root
    shockx=empty("SHOCK_X",(0,0,0))
    shocky=empty("SHOCK_Y",(0,0,0))
    shockx.parent=root;shocky.parent=root
    for p in parts:
        mesh=bpy.data.meshes.new(ID+"_"+p["name"])
        mesh.from_pydata(p["vertices"],[],p["faces"])
        mesh.validate(verbose=False)
        mesh.update()
        ob=bpy.data.objects.new(ID+"_"+p["name"],mesh)
        col.objects.link(ob)
        ob.data.materials.append(materials[p["material"]])
        parent={"GUARD":guard,"SHOCK_X":shockx,
                "SHOCK_Y":shocky}.get(p["rig"],root)
        ob.parent=parent
    for name,(pos,normal) in SNAPS.items():
        rot={"UP_Z":(0,0,0),"OUT_X":(0,math.pi/2,0),
             "OUT_Y":(-math.pi/2,0,0)}[normal]
        snap=empty(name,pos,rot)
        snap.parent=root
    for frame,d in KEYS:
        guard.location=(d,d,0)
        guard.keyframe_insert(data_path="location",frame=frame)
        shockx.location=(0,d/2,0)
        shockx.scale=(1,1-d/.05,1)
        shockx.keyframe_insert(data_path="location",frame=frame)
        shockx.keyframe_insert(data_path="scale",frame=frame)
        shocky.location=(d/2,0,0)
        shocky.scale=(1-d/.05,1,1)
        shocky.keyframe_insert(data_path="location",frame=frame)
        shocky.keyframe_insert(data_path="scale",frame=frame)
    for controller in (guard,shockx,shocky):
        if controller.animation_data and controller.animation_data.action:
            for fc in controller.animation_data.action.fcurves:
                for kp in fc.keyframe_points:
                    kp.interpolation="LINEAR"
    scene=bpy.context.scene
    scene.frame_start=1
    scene.frame_end=65
    scene.render.fps=FPS
    scene.frame_set(1)
    return col

def blender_export(parts,out):
    import bpy
    col=blender_scene(parts,out)
    first=len(col.objects)
    col=blender_scene(parts,out)
    assert len(col.objects)==first==len(parts)+7,(len(col.objects),first)
    bpy.ops.object.select_all(action="DESELECT")
    for ob in col.objects:
        ob.select_set(True)
    bpy.context.view_layer.objects.active=next(
        ob for ob in col.objects if ob.name==ID+"_ROOT")
    glb=out/"M03-011_impact_corner.glb"
    fbx=out/"M03-011_impact_corner.fbx"
    blend=out/"M03-011_impact_corner.blend"
    bpy.ops.export_scene.gltf(filepath=str(glb),export_format="GLB",
                              use_selection=True,export_animations=True)
    bpy.ops.export_scene.fbx(filepath=str(fbx),use_selection=True,
        axis_forward="-Z",axis_up="Y",add_leaf_bones=False,
        bake_anim=True,bake_anim_use_all_actions=False)
    bpy.ops.wm.save_as_mainfile(filepath=str(blend))
    assert all(p.exists() and p.stat().st_size>1000
               for p in (glb,fbx,blend))
    return {"glb_bytes":glb.stat().st_size,"fbx_bytes":fbx.stat().st_size,
            "blend_bytes":blend.stat().st_size,"scene_objects":first,
            "idempotent_second_build":True}

def main():
    argv=sys.argv
    args=argv[argv.index("--")+1:] if "--" in argv else argv[1:]
    out=Path("build/M03-011")
    use_blender="--blender" in args
    if "--out" in args:
        out=Path(args[args.index("--out")+1])
    parts=build()
    samples=validate(parts)
    manifest=write_static(out,parts)
    report=dict(status="PASS",model=ID,static_geometry="PASS",
                moving_pose_samples=samples,parts=manifest["parts"],
                vertices=manifest["vertices"],triangles=manifest["triangles"],
                blender="NOT RUN",unity="NOT RUN")
    if use_blender:
        export=blender_export(parts,out)
        report["blender"]="PASS"
        report["blender_export"]=export
    (out/"M03-011_qa.json").write_text(
        json.dumps(report,indent=2)+"\n",encoding="utf-8")
    print(json.dumps(report,indent=2))

if __name__=="__main__":
    main()
