#!/usr/bin/env python3
"""M01-008 Liquid-Cooled Rifle — visual-only procedural 3D model.

Unity scale: 1 Blender unit = 1 metre. Mount pivot (0,0,0), top socket
contact plane Z=0, +Y barrel forward, +Z up. Two external coolant-pump
pistons reciprocate along Y, phase shifted by 180 degrees. No gameplay.
Can be run in Python 3 without bpy for deterministic OBJ/MTL/geometry QA,
or in Blender 4.x/5.x for animated GLB/FBX and a BLEND library.
"""
from __future__ import annotations
import argparse
from collections import Counter
import hashlib
import json
import math
from pathlib import Path
import sys

ID = "M01-008"
NAME = "M01-008_liquid_cooled_rifle"
PREFIX = "M01_008_"
COLLECTION = "M01-008_ASSET"
PISTON_STROKE = .070
POSES = 61
MATERIALS = {
    "Carbon_Graphite": ((.066,.088,.117,1),.55,.40,0.0),
    "Titanium_Satin": ((.43,.53,.60,1),.83,.29,0.0),
    "Ceramic_Ivory": ((.79,.83,.80,1),.16,.38,0.0),
    "Copper_Pipe": ((.75,.35,.13,1),.78,.30,0.0),
    "Coolant_Cyan": ((.035,.66,.86,1),.30,.20,2.0),
    "Amber_Service": ((1.0,.49,.09,1),.22,.30,1.0),
    "Bore_Black": ((.018,.026,.038,1),.08,.85,0.0),
    "Rubber": ((.034,.043,.050,1),.02,.83,0.0),
}

def sub(a,b): return tuple(x-y for x,y in zip(a,b))
def add(a,b): return tuple(x+y for x,y in zip(a,b))
def scale(a,s): return tuple(x*s for x in a)
def dot(a,b): return sum(x*y for x,y in zip(a,b))
def cross(a,b):
    return (a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0])
def unit(v):
    n=math.sqrt(dot(v,v))
    assert n>1e-9
    return scale(v,1/n)
def volume(vs,faces):
    return sum(dot(vs[a],cross(vs[b],vs[c]))/6 for a,b,c in faces)

def make(name,verts,faces,material,group="STATIC",smooth=False,bevel=0.0):
    assert material in MATERIALS and verts and faces
    if volume(verts,faces)<0:
        faces=[(a,c,b) for a,b,c in faces]
    return dict(name=name,vertices=verts,faces=faces,material=material,
                group=group,smooth=smooth,bevel=bevel)

def box(name,center,size,material,group="STATIC",bevel=.004):
    assert min(size)>0
    x,y,z=center;hx,hy,hz=(t/2 for t in size)
    v=[(x+u,y+w,z+t) for u,w,t in (
       (-hx,-hy,-hz),(hx,-hy,-hz),(hx,hy,-hz),(-hx,hy,-hz),
       (-hx,-hy,hz),(hx,-hy,hz),(hx,hy,hz),(-hx,hy,hz))]
    polys=((0,3,2,1),(4,5,6,7),(0,1,5,4),
           (1,2,6,5),(2,3,7,6),(3,0,4,7))
    f=[]
    for p in polys:
        f.extend(((p[0],p[1],p[2]),(p[0],p[2],p[3])))
    return make(name,v,f,material,group,bevel=bevel)

def cylinder(name,a,b,radius,material,group="STATIC",sides=20):
    """Closed round bar from a to b, outward winding; no flat fake geometry."""
    assert radius>0 and sides>=8
    direction=unit(sub(b,a))
    seed=(1.,0.,0.) if abs(direction[0])<.85 else (0.,0.,1.)
    right=unit(cross(direction,seed))
    up=cross(direction,right)
    v=[]
    for c in (a,b):
        for j in range(sides):
            ang=2*math.pi*j/sides
            v.append(add(c,add(scale(right,radius*math.cos(ang)),
                               scale(up,radius*math.sin(ang)))))
    v.extend((a,b))
    f=[]
    for j in range(sides):
        k=(j+1)%sides
        f.extend(((j,k,sides+k),(j,sides+k,sides+j),
                  (2*sides,k,j),(2*sides+1,sides+j,sides+k)))
    return make(name,v,f,material,group,smooth=True)

def annulus_y(name,y0,y1,r_in,r_out,z,material,group="STATIC",sides=36):
    """Real hollow barrel/ring: two concentric walls and annular end caps."""
    assert 0<r_in<r_out and y0<y1
    profile=((y0,r_in),(y0,r_out),(y1,r_out),(y1,r_in))
    v=[(r*math.cos(2*math.pi*j/sides),y,
        z+r*math.sin(2*math.pi*j/sides))
       for y,r in profile for j in range(sides)]
    f=[]
    for i in range(4):
        ni=(i+1)%4
        for j in range(sides):
            k=(j+1)%sides
            a=i*sides+j;b=i*sides+k;c=ni*sides+k;d=ni*sides+j
            f.extend(((a,b,c),(a,c,d)))
    return make(name,v,f,material,group,smooth=True)

def coolant_tube(name,points,radius,material,sides=14):
    """One continuous manifold curved tube, rather than intersecting rods."""
    assert len(points)>=3
    v=[]
    for i,p in enumerate(points):
        tangent=unit(sub(points[min(i+1,len(points)-1)],
                         points[max(i-1,0)]))
        radial=(1.,0.,0.)
        secondary=unit(cross(tangent,radial))
        for j in range(sides):
            ang=2*math.pi*j/sides
            v.append(add(p,add(scale(radial,radius*math.cos(ang)),
                               scale(secondary,radius*math.sin(ang)))))
    v.extend((points[0],points[-1]))
    f=[]
    for i in range(len(points)-1):
        for j in range(sides):
            k=(j+1)%sides
            a=i*sides+j;b=i*sides+k;c=(i+1)*sides+k;d=(i+1)*sides+j
            f.extend(((a,b,c),(a,c,d)))
    start=len(points)*sides
    for j in range(sides):
        k=(j+1)%sides
        f.extend(((start,k,j),
                  (start+1,(len(points)-1)*sides+j,
                   (len(points)-1)*sides+k)))
    return make(name,v,f,material,smooth=True)

def build():
    parts=[];push=parts.append
    # Mount plane at Z=0, four visible bolts, physically keyed top socket.
    push(cylinder("Top_Mount", (0,0,0),(0,0,.048),.166,"Titanium_Satin",sides=32))
    push(box("Mount_Key",(0,0,.067),(.230,.250,.042),"Carbon_Graphite"))
    for sx in (-1,1):
        for sy in (-1,1):
            push(cylinder("Mount_Bolt_%+d_%+d"%(sx,sy),
                 (sx*.108,sy*.107,.048),(sx*.108,sy*.107,.060),
                 .013,"Copper_Pipe",sides=12))
    push(box("Lower_Pedestal",(0,-.040,.170),(.230,.320,.190),"Titanium_Satin"))
    # A compact asymmetric receiver, separate ceramic side shells.
    push(box("Receiver",(0,.140,.360),(.390,.690,.270),"Carbon_Graphite"))
    push(box("Rear_Breech", (0,-.275,.385),(.300,.145,.200),"Titanium_Satin"))
    push(box("Rear_Grip_Cap",(0,-.353,.387),(.315,.025,.210),"Rubber"))
    push(box("Receiver_Spine",(0,.130,.520),(.265,.590,.048),"Titanium_Satin"))
    for side in (-1,1):
        side_name="L" if side<0 else "R"
        push(box("Ceramic_Cheek_"+side_name,
                 (side*.244,.125,.371),(.090,.485,.193),
                 "Ceramic_Ivory",bevel=.012))
        push(box("Copper_Cheek_Rail_"+side_name,
                 (side*.297,.120,.410),(.014,.450,.021),"Copper_Pipe"))
        push(box("Amber_Side_Signal_"+side_name,
                 (side*.305,-.030,.425),(.010,.112,.018),"Amber_Service"))
        for k,y in enumerate((-.10,.045,.19,.335)):
            push(box("Cheek_Slit_%s_%02d"%(side_name,k),
                     (side*.300,y,.326),(.008,.055,.014),"Bore_Black"))
    # Hollow barrel and vented ceramic/titanium shroud, +Y forward.
    push(annulus_y("Hollow_Rifle_Barrel",.375,1.655,.038,.067,.440,"Bore_Black"))
    push(annulus_y("Breech_Barrel_Collar",.430,.515,.065,.108,.440,"Copper_Pipe"))
    push(annulus_y("Barrel_Heat_Shroud",.525,.970,.079,.112,.440,"Titanium_Satin"))
    push(annulus_y("Muzzle_Brake",1.630,1.745,.038,.124,.440,"Titanium_Satin"))
    push(annulus_y("Muzzle_Copper_Lip",1.741,1.762,.091,.129,.440,"Copper_Pipe"))
    push(annulus_y("Muzzle_Cyan_Inner",1.752,1.763,.039,.055,.440,"Coolant_Cyan"))
    for k,y in enumerate((.590,.675,.760,.845,.930)):
        push(box("Shroud_Rib_%02d"%k,(0,y,.565),
                 (.195,.020,.035),"Ceramic_Ivory"))
    # Visible, tall radiator comb with individual fins and two manifolds.
    push(box("Radiator_Lower_Manifold",(0,.555,.621),
             (.380,.825,.087),"Carbon_Graphite"))
    push(box("Radiator_Upper_Rail",(0,.575,.820),
             (.355,.840,.036),"Titanium_Satin"))
    for k in range(11):
        y=.210+k*.071
        push(box("Radiator_Fin_%02d"%k,(0,y,.745),
                 (.560,.013,.146),"Titanium_Satin",bevel=.002))
        push(box("Radiator_Fin_Edge_%02d"%k,(0,y,.819),
                 (.450,.016,.012),"Copper_Pipe",bevel=.001))
    # Curved external cyan coolant pipes: side silhouette + visible returns.
    for side in (-1,1):
        sn="L" if side<0 else "R"
        x=side*.316
        path=[(x,-.135,.525),(x,-.090,.573),(x,-.020,.635),
              (x,.080,.688),(x,.185,.706),(x,.340,.706),
              (x,.500,.706),(x,.660,.706),(x,.820,.706),
              (x,.965,.690),(x,1.060,.630),(x,1.102,.562)]
        push(coolant_tube("Coolant_Tube_"+sn,path,.018,"Coolant_Cyan"))
        for k,y in enumerate((.19,.50,.82)):
            push(cylinder("Pipe_Copper_Collar_%s_%02d"%(sn,k),
                 (x,y-.016,.706),(x,y+.016,.706),
                 .024,"Copper_Pipe",sides=16))
        push(box("Coolant_Connector_"+sn,(x,-.145,.525),
                 (.070,.066,.050),"Copper_Pipe"))
        push(box("Coolant_Return_"+sn,(x,1.112,.560),
                 (.070,.075,.055),"Copper_Pipe"))
    # Twin open-sided pump cages, *clear* of the reciprocating assemblies.
    # Side rail X inner face at +/-0.4775; piston max X=+/-0.449.
    # Upper/lower guide surfaces Z=0.5075/0.3025; piston Z=.359..441.
    for side in (-1,1):
        sn="L" if side<0 else "R"
        x=side*.405
        push(box("Pump_Guide_Upper_"+sn,(x,.465,.520),
                 (.145,.840,.025),"Titanium_Satin"))
        push(box("Pump_Guide_Lower_"+sn,(x,.465,.290),
                 (.145,.840,.025),"Titanium_Satin"))
        push(box("Pump_Guide_Outer_"+sn,(side*.492,.465,.405),
                 (.025,.840,.185),"Carbon_Graphite"))
        push(box("Pump_Rear_Anchor_"+sn,(x,.020,.405),
                 (.170,.055,.260),"Copper_Pipe"))
        push(box("Pump_Front_Anchor_"+sn,(x,.925,.405),
                 (.170,.055,.260),"Copper_Pipe"))
        group="PUMP_"+sn
        push(box("Pump_Piston_Head_"+sn,(x,.415,.400),
                 (.088,.105,.080),"Ceramic_Ivory",group,bevel=.009))
        push(cylinder("Pump_Piston_Rod_"+sn,(x,.457,.400),
                 (x,.745,.400),.018,"Titanium_Satin",group,sides=20))
        push(box("Pump_Head_Copper_Band_"+sn,(x,.418,.444),
                 (.065,.052,.010),"Copper_Pipe",group,bevel=.002))
        push(box("Pump_Amber_Flow_Indicator_"+sn,(x,.413,.451),
                 (.035,.037,.008),"Amber_Service",group,bevel=.001))
    # Side-mounted sight and asymmetry, clear of piston motion.
    push(box("Offset_Sight_Base",(-.100,-.045,.570),
             (.130,.215,.040),"Carbon_Graphite"))
    push(box("Offset_Sight",(-.105,.005,.611),
             (.095,.140,.050),"Ceramic_Ivory"))
    push(box("Sight_Cyan_Strip",(-.105,.020,.641),
             (.057,.080,.010),"Coolant_Cyan"))
    return parts

def pose(group,step):
    """Reciprocating pistons, exactly 180-degree phase shift."""
    phase=0 if group=="PUMP_L" else math.pi
    return PISTON_STROKE*math.sin(2*math.pi*step/(POSES-1)+phase)

def bounds(p,dy=0.):
    return [(min(v[i] for v in p["vertices"])+(dy if i==1 else 0),
             max(v[i] for v in p["vertices"])+(dy if i==1 else 0))
            for i in range(3)]

def validate(parts):
    names=set();nverts=0;ntris=0;mesh_count=0
    for p in parts:
        assert p["name"] not in names,p["name"]
        names.add(p["name"])
        assert p["material"] in MATERIALS
        vs=p["vertices"];faces=p["faces"]
        assert all(all(math.isfinite(c) for c in v) for v in vs)
        edges=Counter()
        for f in faces:
            assert len(f)==3 and len(set(f))==3
            assert all(0<=k<len(vs) for k in f)
            a,b,c=(vs[k] for k in f)
            n=cross(sub(b,a),sub(c,a))
            assert dot(n,n)>1e-15,("degenerate",p["name"])
            for u,v in zip(f,f[1:]+f[:1]):
                edges[tuple(sorted((u,v)))]+=1
        assert edges and all(n==2 for n in edges.values()),("non-manifold",p["name"])
        assert volume(vs,faces)>1e-9,("reversed normals",p["name"])
        mesh_count+=1;nverts+=len(vs);ntris+=len(faces)
    static=[p for p in parts if p["group"]=="STATIC"]
    moving=[p for p in parts if p["group"]!="STATIC"]
    assert set(p["group"] for p in moving)=={"PUMP_L","PUMP_R"}
    assert len(moving)==8
    assert len(MATERIALS)>=3
    # Distinct piston groups never contact each other (opposite sides).
    checks=0;min_clearance=100.
    for step in range(POSES):
        for m in moving:
            mb=bounds(m,pose(m["group"],step))
            for s in static:
                sb=bounds(s)
                overlap=[min(mb[i][1],sb[i][1]) -
                         max(mb[i][0],sb[i][0]) for i in range(3)]
                checks+=1
                if all(x>1e-6 for x in overlap):
                    raise AssertionError(("PUMP_INTERSECTION",m["name"],s["name"],step,overlap))
                min_clearance=min(min_clearance,max(-v for v in overlap))
    # Key moving extremes and static reference constraints.
    assert abs(pose("PUMP_L",15)-PISTON_STROKE)<1e-8
    assert abs(pose("PUMP_R",15)+PISTON_STROKE)<1e-8
    assert abs(pose("PUMP_L",45)+PISTON_STROKE)<1e-8
    assert abs(pose("PUMP_R",45)-PISTON_STROKE)<1e-8
    allv=[v for p in parts for v in p["vertices"]]
    xyz=[(min(v[i] for v in allv),max(v[i] for v in allv)) for i in range(3)]
    assert abs(xyz[2][0])<1e-8,"Mount must touch Z=0 exactly"
    assert xyz[1][1]>1.7 and xyz[2][1]>.80
    return dict(id=ID,result="PASS_STATIC",parts=len(parts),
        vertices=nverts,triangles=ntris,materials=len(MATERIALS),
        closed_positive_volume_components=mesh_count,
        mount_pivot_m=[0,0,0],mount_plane="Z=0",mount_type="top_socket",
        forward_axis="+Y",up_axis="+Z",animation_axis="+Y",
        pump_groups=["PUMP_L","PUMP_R"],pump_stroke_amplitude_m=PISTON_STROKE,
        animation_pose_samples=POSES,moving_parts=len(moving),
        static_parts=len(static),moving_static_aabb_checks=checks,
        moving_static_intersections=0,extent_m=xyz,
        blender_export="NOT_RUN",unity_import="NOT_RUN",
        visual_review="NOT_RUN",gameplay="UNCHANGED")

def static_export(parts,out):
    out.mkdir(parents=True,exist_ok=True)
    obj=out/(NAME+".obj");mtl=out/(NAME+".mtl")
    lines=["# M01-008, Z-up, meters, +Y forward, top mount at origin",
           "mtllib "+mtl.name]
    offset=1
    for p in parts:
        lines.extend(("o "+p["name"],"usemtl "+p["material"]))
        lines.extend("v %.7f %.7f %.7f"%v for v in p["vertices"])
        lines.extend("f "+" ".join(str(offset+k) for k in f) for f in p["faces"])
        offset+=len(p["vertices"])
    obj.write_text("\n".join(lines)+"\n",encoding="utf-8")
    lines=[]
    for name,(rgba,metal,rough,emission) in MATERIALS.items():
        lines.extend(["newmtl "+name,"Kd %.5f %.5f %.5f"%rgba[:3],
                      "Ks %.5f %.5f %.5f"%((metal*.5,)*3),
                      "Ns %.2f"%((1-rough)*400),
                      "Ke %.5f %.5f %.5f"%tuple(emission*v for v in rgba[:3]),
                      "d 1.0","illum 2",""])
    mtl.write_text("\n".join(lines)+"\n",encoding="utf-8")
    return dict(obj=obj.name,mtl=mtl.name,
        obj_sha256=hashlib.sha256(obj.read_bytes()).hexdigest(),
        mtl_sha256=hashlib.sha256(mtl.read_bytes()).hexdigest())

def blender_export(parts,out):
    import bpy
    owned=bpy.data.collections.get(COLLECTION)
    if owned:
        for obj in tuple(owned.objects):
            bpy.data.objects.remove(obj,do_unlink=True)
        bpy.data.collections.remove(owned)
    for mesh in tuple(bpy.data.meshes):
        if mesh.name.startswith(PREFIX) and mesh.users==0:
            bpy.data.meshes.remove(mesh)
    owned=bpy.data.collections.new(COLLECTION)
    bpy.context.scene.collection.children.link(owned)
    bpy.context.scene.unit_settings.system="METRIC"
    bpy.context.scene.unit_settings.scale_length=1.0
    def empty(name,parent=None):
        ob=bpy.data.objects.new(PREFIX+name,None)
        owned.objects.link(ob);ob.parent=parent
        ob.empty_display_type="ARROWS";ob.empty_display_size=.10
        return ob
    mount=empty("MOUNT_TOP_Z0")
    mount["mount_type"]="top_socket"
    mount["forward_axis"]="+Y"
    mount["unit"]="meter"
    sliders={g:empty(g+"_SLIDE_Y",mount) for g in ("PUMP_L","PUMP_R")}
    mats={}
    for name,(rgba,metal,rough,emit) in MATERIALS.items():
        mat=bpy.data.materials.get(PREFIX+name)
        if mat is None:mat=bpy.data.materials.new(PREFIX+name)
        mat.use_nodes=True
        bsdf=mat.node_tree.nodes.get("Principled BSDF")
        if bsdf:
            for key,value in (("Base Color",rgba),("Metallic",metal),
                              ("Roughness",rough)):
                if key in bsdf.inputs:bsdf.inputs[key].default_value=value
            if emit:
                for key,value in (("Emission Color",rgba),
                                  ("Emission Strength",emit)):
                    if key in bsdf.inputs:bsdf.inputs[key].default_value=value
        mat.diffuse_color=rgba;mats[name]=mat
    for p in parts:
        mesh=bpy.data.meshes.new(PREFIX+p["name"]+"_Mesh")
        mesh.from_pydata(p["vertices"],[],p["faces"])
        mesh.update(calc_edges=True)
        ob=bpy.data.objects.new(PREFIX+p["name"],mesh)
        owned.objects.link(ob)
        ob.parent=mount if p["group"]=="STATIC" else sliders[p["group"]]
        mesh.materials.append(mats[p["material"]])
        for poly in mesh.polygons:poly.use_smooth=p["smooth"]
        if p["bevel"] and not p["smooth"]:
            bevel=ob.modifiers.new("Machined_Edge","BEVEL")
            bevel.width=min(p["bevel"],.006)
            bevel.segments=2
            bevel.limit_method="ANGLE"
            bevel.angle_limit=.4
            ob.modifiers.new("Weighted_Normals","WEIGHTED_NORMAL")
    for group,ob in sliders.items():
        for frame in (1,16,31,46,61):
            ob.location.y=pose(group,frame-1)
            ob.keyframe_insert(data_path="location",index=1,frame=frame)
        if ob.animation_data and ob.animation_data.action:
            try:
                for fc in ob.animation_data.action.fcurves:
                    for kp in fc.keyframe_points:kp.interpolation="LINEAR"
            except AttributeError:
                pass
    scene=bpy.context.scene
    old=(scene.frame_start,scene.frame_end,scene.frame_current,scene.render.fps)
    selected=list(bpy.context.selected_objects)
    active=bpy.context.view_layer.objects.active
    try:
        scene.frame_start=1;scene.frame_end=61;scene.render.fps=60
        scene.frame_set(1);bpy.context.view_layer.update()
        assert len(owned.objects)==len(parts)+3
        bpy.data.libraries.write(str(out/(NAME+".blend")),{owned},compress=True)
        bpy.ops.object.select_all(action="DESELECT")
        for ob in owned.objects:ob.select_set(True)
        bpy.context.view_layer.objects.active=mount
        bpy.ops.export_scene.gltf(
            filepath=str(out/(NAME+"_animated.glb")),export_format="GLB",
            use_selection=True,export_animations=True)
        bpy.ops.export_scene.fbx(
            filepath=str(out/(NAME+"_animated.fbx")),use_selection=True,
            axis_forward="-Z",axis_up="Y",add_leaf_bones=False,
            bake_anim=True,bake_anim_use_all_actions=False)
    finally:
        bpy.ops.object.select_all(action="DESELECT")
        for ob in selected:
            if ob.name in bpy.data.objects:ob.select_set(True)
        if active and active.name in bpy.data.objects:
            bpy.context.view_layer.objects.active=active
        scene.frame_start,scene.frame_end,scene.render.fps=old[0],old[1],old[3]
        scene.frame_set(old[2])
    paths={k:out/(NAME+suffix) for k,suffix in (
        ("glb","_animated.glb"),("fbx","_animated.fbx"),("blend",".blend"))}
    assert all(p.is_file() and p.stat().st_size>10000 for p in paths.values())
    return dict(blender_export="PASS",**{k:p.name for k,p in paths.items()})

def main():
    argv=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else sys.argv[1:]
    ap=argparse.ArgumentParser()
    ap.add_argument("--out",default="build/M01-008")
    ap.add_argument("--self-test",action="store_true")
    args=ap.parse_args(argv)
    parts=build();report=validate(parts)
    if args.self_test:
        print(json.dumps(report,indent=2));return
    out=Path(args.out).resolve();out.mkdir(parents=True,exist_ok=True)
    report.update(static_export(parts,out))
    try:import bpy
    except ImportError:pass
    else:report.update(blender_export(parts,out))
    (out/"M01-008_qa.json").write_text(
        json.dumps(report,indent=2)+"\n",encoding="utf-8")
    print(json.dumps(report,indent=2))

if __name__=="__main__":main()
