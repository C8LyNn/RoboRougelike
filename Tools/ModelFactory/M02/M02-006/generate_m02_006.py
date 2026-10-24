#!/usr/bin/env python3
"""M02-006 — replaceable-spike rubble wheel, Unity 6 scale (1 unit = 1 metre).

Pivot/mount: (0,0,0), vehicle side mounting plane X=0.
Axle and wheel rotation: +X. Wheel centre at X≈0.18 m.
Dependency-free static OBJ/MTL + structural QA; Blender 4.2+/5.x exports
animated GLB, Unity-native animated FBX and an appendable .blend collection library. No gameplay edits.
"""
from __future__ import annotations
import argparse
from collections import Counter
import json
import math
from pathlib import Path
import sys

ID = "M02-006"
N = 96
SPIKES = 16
SPOKES = 10
MATERIALS = {
    "Tire_Dark_Rubber": ((.075, .085, .093, 1), 0.0, .87),
    "Rim_Brushed_Titanium": ((.43, .49, .53, 1), .78, .36),
    "Spikes_Hardened_Steel": ((.32, .36, .40, 1), .89, .32),
    "Hub_Copper": ((.58, .31, .17, 1), .78, .39),
    "Safety_Amber": ((.92, .47, .09, 1), .22, .38),
    "Mount_Gunmetal": ((.18, .21, .24, 1), .78, .47),
    "Brake_Ceramic": ((.18, .23, .25, 1), .30, .65),
}

def point(x, r, angle):
    return (x, r * math.cos(angle), r * math.sin(angle))

def sub(a,b):
    return tuple(a[i]-b[i] for i in range(3))

def cross(a,b):
    return (a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0])

def dot(a,b):
    return sum(x*y for x,y in zip(a,b))

def part(name, material, vertices, faces, stationary=False, smooth=False):
    return dict(name=name, material=material, vertices=vertices, faces=faces,
                stationary=stationary, smooth=smooth)

def ring(name, profile, material, stationary=False):
    """Watertight annular revolution, explicitly hollow where r_min>0."""
    assert len(profile)>=4 and min(r for x,r in profile)>0
    verts=[point(x,r,2*math.pi*j/N) for x,r in profile for j in range(N)]
    faces=[]
    for i in range(len(profile)):
        for j in range(N):
            nxt=(i+1)%len(profile);k=(j+1)%N
            a=i*N+j;b=i*N+k;c=nxt*N+k;d=nxt*N+j
            faces.extend(((a,b,c),(a,c,d)))
    return part(name,material,verts,faces,stationary,True)

def ann(name, x0,x1,r0,r1,mat,stationary=False):
    return ring(name,[(x0,r0),(x0,r1),(x1,r1),(x1,r0)],mat,stationary)

def wedge(name,x0,x1,r0,r1,angle,half0,half1,mat):
    """Closed convex radial trapezoid, bevel-free and outward wound."""
    assert x0<x1 and 0<r0<r1 and 0<half1<=half0<.25
    v=[point(x,r,a) for r,h in ((r0,half0),(r1,half1))
       for x in (x0,x1) for a in (angle-h,angle+h)]
    # indices: inner [0,1,2,3], outer [4,5,6,7]
    quads=[(0,1,3,2),(4,6,7,5),(0,2,6,4),
           (1,5,7,3),(0,4,5,1),(2,3,7,6)]
    centre=tuple(sum(v[k][i] for k in range(8))/8 for i in range(3))
    faces=[]
    for q in quads:
        a,b,c,d=q
        normal=cross(sub(v[b],v[a]),sub(v[c],v[a]))
        midpoint=tuple(sum(v[k][i] for k in q)/4 for i in range(3))
        if dot(normal,sub(midpoint,centre))<0:
            q=(d,c,b,a)
        faces.extend(((q[0],q[1],q[2]),(q[0],q[2],q[3])))
    return part(name,mat,v,faces)

def cylinder_x(name,rad,x0,x1,y,z,mat,segments=18,stationary=False):
    assert x1>x0 and rad>0 and segments>=8
    v=[(x,y+rad*math.cos(2*math.pi*j/segments),
        z+rad*math.sin(2*math.pi*j/segments))
       for x in (x0,x1) for j in range(segments)]
    v.extend(((x0,y,z),(x1,y,z)))
    f=[]
    for j in range(segments):
        k=(j+1)%segments
        f.extend(((j,k,segments+k),(j,segments+k,segments+j)))
        f.extend(((2*segments,k,j),(2*segments+1,segments+j,segments+k)))
    return part(name,mat,v,f,stationary)

def build():
    pieces=[];add=pieces.append
    # Crown of the tire has two shallow moulded grooves, not sharp low-poly cuts.
    tire=[(.115,.308),(.113,.345),(.114,.387),(.121,.414),
          (.134,.429),(.147,.432),(.158,.426),(.169,.432),
          (.187,.432),(.198,.426),(.209,.432),(.230,.428),
          (.242,.414),(.245,.382),(.245,.345),(.242,.308)]
    add(ring("01_Rubble_Tire",tire,"Tire_Dark_Rubber"))
    add(ann("02_Reinforced_Rim",.125,.235,.295,.316,"Rim_Brushed_Titanium"))
    add(ann("03_Rear_Bead_Lock",.120,.133,.302,.335,"Mount_Gunmetal"))
    add(ann("04_Front_Bead_Lock",.230,.240,.302,.335,"Spikes_Hardened_Steel"))
    add(ann("05_Hollow_Hub",.140,.232,.055,.128,"Hub_Copper"))
    add(ann("06_Hub_Front_Collar",.227,.247,.055,.104,"Rim_Brushed_Titanium"))
    add(ann("07_Brake_Disc",.133,.151,.105,.272,"Brake_Ceramic"))
    for i in range(SPOKES):
        angle=2*math.pi*(i+.08)/SPOKES
        add(wedge(f"Spoke_{i:02}",.151,.221,.116,.304,angle,.100,.065,
                  "Rim_Brushed_Titanium"))
        add(wedge(f"Spoke_Amber_Insert_{i:02}",.217,.230,.173,.274,
                  angle,.041,.030,"Safety_Amber"))
    for i in range(SPIKES):
        angle=2*math.pi*i/SPIKES
        # The replaceable shoe penetrates the rubber crown by 16 mm;
        # steel spike is seated into the shoe by 8 mm.
        add(wedge(f"Spike_Shoe_{i:02}",.132,.228,.416,.448,
                  angle,.078,.060,"Mount_Gunmetal"))
        add(wedge(f"Replaceable_Spike_{i:02}",.149,.215,.440,.535,
                  angle,.051,.013,"Spikes_Hardened_Steel"))
        # Exposed fastener head at the outer shoe face.
        add(cylinder_x(f"Spike_Bolt_{i:02}",.011,.226,.244,
                       .432*math.cos(angle),.432*math.sin(angle),
                       "Hub_Copper"))
    # Static parts remain children of MOUNT; shaft fits hollow hub with
    # 23 mm nominal radial clearance (0.055-0.032 m).
    add(ann("Mount_Flange_STATIC",-.060,.003,.014,.078,
            "Mount_Gunmetal",True))
    add(cylinder_x("Axle_Shaft_STATIC",.032,0,.272,0,0,
                   "Rim_Brushed_Titanium",24,True))
    add(cylinder_x("Axle_Retainer_STATIC",.042,.267,.290,0,0,
                   "Hub_Copper",24,True))
    return pieces

def bounds(p):
    v=p["vertices"]
    return ((min(x for x,y,z in v),max(x for x,y,z in v)),
            (min(math.hypot(y,z) for x,y,z in v),
             max(math.hypot(y,z) for x,y,z in v)))

def intersect(a,b):
    return min(a[1],b[1])-max(a[0],b[0])

def validate(parts):
    names=set();total_v=0;total_f=0;positive=0
    for p in parts:
        assert p["name"] not in names,p["name"]
        names.add(p["name"])
        assert p["material"] in MATERIALS
        verts=p["vertices"];edges=Counter();volume=0.0
        for vertex in verts:
            assert all(math.isfinite(x) for x in vertex)
        for face in p["faces"]:
            assert len(face)==3 and len(set(face))==3
            assert all(0<=i<len(verts) for i in face)
            a,b,c=(verts[i] for i in face)
            normal=cross(sub(b,a),sub(c,a))
            assert dot(normal,normal)>1e-15,(p["name"],"degenerate")
            volume+=dot(a,cross(b,c))/6
            for i,j in zip(face,face[1:]+face[:1]):
                edges[tuple(sorted((i,j)))]+=1
        assert all(v==2 for v in edges.values()),(p["name"],"nonmanifold")
        assert volume>1e-10,(p["name"],"inward winding",volume)
        positive+=1
        total_v+=len(verts);total_f+=len(p["faces"])
    assert len([p for p in parts if p["name"].startswith("Replaceable_Spike_")])==SPIKES
    assert len([p for p in parts if p["name"].startswith("Spoke_") and "Insert" not in p["name"]])==SPOKES
    assert len([p for p in parts if p["stationary"]])==3
    lookup={p["name"]:p for p in parts}
    # Physical contact between adjacent structural parts (not air gaps).
    contact=[
        ("01_Rubble_Tire","02_Reinforced_Rim",.005,.007),
        ("02_Reinforced_Rim","03_Rear_Bead_Lock",.005,.012),
        ("02_Reinforced_Rim","04_Front_Bead_Lock",.005,.012),
        ("05_Hollow_Hub","06_Hub_Front_Collar",.004,.040),
        ("05_Hollow_Hub","07_Brake_Disc",.007,.020),
    ]
    for a,b,axial,radial in contact:
        x1,r1=bounds(lookup[a]);x2,r2=bounds(lookup[b])
        assert intersect(x1,x2)>=axial-1e-8,(a,b,"axial")
        assert intersect(r1,r2)>=radial-1e-8,(a,b,"radial")
    for i in range(SPIKES):
        shoe=lookup[f"Spike_Shoe_{i:02}"]
        spike=lookup[f"Replaceable_Spike_{i:02}"]
        bolt=lookup[f"Spike_Bolt_{i:02}"]
        for a,b in ((shoe,spike),(shoe,bolt),(shoe,lookup["01_Rubble_Tire"])):
            xa,ra=bounds(a);xb,rb=bounds(b)
            assert intersect(xa,xb)>0 and intersect(ra,rb)>0,(a["name"],b["name"])
    for i in range(SPOKES):
        a=lookup[f"Spoke_{i:02}"];b=lookup[f"Spoke_Amber_Insert_{i:02}"]
        xa,ra=bounds(a);xb,rb=bounds(b)
        assert intersect(xa,xb)>.003 and intersect(ra,rb)>.07
    # Exact radial clearance is invariant under a rotation about the X axis.
    # 72 sampled poses are checked in addition to the analytical invariance.
    moving=[p for p in parts if not p["stationary"]]
    fixed=[p for p in parts if p["stationary"]]
    min_clearance=float("inf");comparisons=0
    for degrees in range(0,360,5):
        angle=math.radians(degrees);c=math.cos(angle);s=math.sin(angle)
        for a in moving:
            for b in fixed:
                comparisons+=1
                xa,ra=bounds(a);xb,rb=bounds(b)
                if intersect(xa,xb)<=0:
                    continue
                # All stationary objects sharing axial range are within
                # the bore, not wrapped around the rotating wheel.
                assert b["name"]=="Axle_Shaft_STATIC",(a["name"],b["name"])
                rotated_r=[math.hypot(y*c-z*s,y*s+z*c) for x,y,z in a["vertices"]]
                clearance=min(rotated_r)-rb[1]
                min_clearance=min(min_clearance,clearance)
                assert clearance>=.020,(a["name"],b["name"],degrees,clearance)
    assert comparisons==72*len(moving)*len(fixed)
    assert abs(bounds(lookup["01_Rubble_Tire"])[1][1]-.432)<1e-8
    assert abs(bounds(lookup["Replaceable_Spike_00"])[1][1]-.535)<1e-8
    return dict(id=ID,parts=len(parts),vertices=total_v,triangles=total_f,
                materials=len(MATERIALS),spikes=SPIKES,spokes=SPOKES,
                rotating_parts=len(moving),stationary_parts=len(fixed),
                maximum_radius_m=.535, tire_width_m=.132,
                mount_pivot_m=[0,0,0],axis="+X", wheel_center_x_m=.18,
                nominal_shaft_bore_clearance_m=.023,
                collision_sample_count=72,collision_pairs=comparisons,
                min_measured_rotor_shaft_clearance_m=round(min_clearance,5),
                closed_positive_volume_meshes=positive,
                contact_checks=len(contact)+SPIKES*3+SPOKES,
                static_qa="PASS",blender="NOT_RUN",unity="NOT_RUN")

def export_obj(parts,out):
    out.mkdir(parents=True,exist_ok=True)
    name="M02-006_spiked_rubble_wheel"
    obj=out/(name+".obj");mtl=out/(name+".mtl")
    lines=["# M02-006 Z-up, metres, mount pivot X=0, rotation +X",
           "mtllib "+mtl.name]
    offset=1
    for p in parts:
        lines.extend(("o "+p["name"],"usemtl "+p["material"]))
        lines.extend("v %.7f %.7f %.7f"%v for v in p["vertices"])
        lines.extend("f "+" ".join(str(offset+i) for i in face) for face in p["faces"])
        offset+=len(p["vertices"])
    obj.write_text("\n".join(lines)+"\n",encoding="utf-8")
    mats=[]
    for key,(color,metal,rough) in MATERIALS.items():
        mats.extend(("newmtl "+key,"Kd %.5f %.5f %.5f"%color[:3],
                     "Ks %.5f %.5f %.5f"%((metal*.45,)*3),
                     "Ns %.2f"%((1-rough)*400),"d 1.0","illum 2",""))
    mtl.write_text("\n".join(mats)+"\n",encoding="utf-8")
    return [str(obj),str(mtl)]

def export_blender(parts,out):
    import bpy
    coll=bpy.data.collections.get("M02-006_ASSET")
    if coll:
        for ob in tuple(coll.objects):
            bpy.data.objects.remove(ob,do_unlink=True)
        bpy.data.collections.remove(coll)
    coll=bpy.data.collections.new("M02-006_ASSET")
    bpy.context.scene.collection.children.link(coll)
    def empty(name,parent=None):
        ob=bpy.data.objects.new(name,None);coll.objects.link(ob)
        ob.parent=parent;ob.empty_display_size=.06
        return ob
    mount=empty("M02-006_MOUNT_X0")
    rotor=empty("M02-006_WHEEL_ROTATE_X",mount)
    mats={}
    for name,(color,metal,rough) in MATERIALS.items():
        key="M02-006_"+name
        m=bpy.data.materials.get(key) or bpy.data.materials.new(key)
        m.use_nodes=True
        shader=m.node_tree.nodes.get("Principled BSDF")
        if shader:
            shader.inputs["Base Color"].default_value=color
            shader.inputs["Metallic"].default_value=metal
            shader.inputs["Roughness"].default_value=rough
        m.diffuse_color=color;mats[name]=m
    for p in parts:
        mesh=bpy.data.meshes.new("M02-006_"+p["name"]+"_mesh")
        mesh.from_pydata(p["vertices"],[],p["faces"]);mesh.update(calc_edges=True)
        ob=bpy.data.objects.new("M02-006_"+p["name"],mesh)
        coll.objects.link(ob)
        ob.parent=mount if p["stationary"] else rotor
        ob.data.materials.append(mats[p["material"]])
        for polygon in mesh.polygons:
            polygon.use_smooth=p["smooth"]
    rotor.rotation_mode="XYZ"
    for frame,angle in ((1,0),(16,math.pi/2),(31,math.pi),
                        (46,3*math.pi/2),(61,2*math.pi)):
        rotor.rotation_euler[0]=angle
        rotor.keyframe_insert(data_path="rotation_euler",index=0,frame=frame)
    action=rotor.animation_data.action
    if action:
        try:
            curves=action.fcurves
        except AttributeError:
            curves=[]
            for layer in action.layers:
                for strip in layer.strips:
                    for bag in strip.channelbags:
                        curves.extend(bag.fcurves)
        for curve in curves:
            for key in curve.keyframe_points:
                key.interpolation="LINEAR"
    scene=bpy.context.scene
    previous=(scene.frame_start,scene.frame_end,scene.render.fps,
              scene.frame_current,tuple(bpy.context.selected_objects),
              bpy.context.view_layer.objects.active)
    try:
        scene.frame_start=1;scene.frame_end=61;scene.render.fps=30
        scene.frame_set(1)
        assert len(coll.objects)==len(parts)+2
        out.mkdir(parents=True,exist_ok=True)
        blend=out/"M02-006.blend"
        bpy.data.libraries.write(str(blend),{coll},compress=True)
        bpy.ops.object.select_all(action="DESELECT")
        for ob in coll.objects:ob.select_set(True)
        bpy.context.view_layer.objects.active=rotor
        glb=out/"M02-006_animated.glb"
        bpy.ops.export_scene.gltf(filepath=str(glb),export_format="GLB",
                                  use_selection=True,export_animations=True,
                                  export_force_sampling=True,
                                  export_frame_range=True)
        # FBX is optional: Ubuntu Blender 5.0.1 registers a broken/minimal
        # operator (only filepath/axis) that cannot safely isolate objects.
        # Do not fake an FBX export or fail the verified GLB/BLEND pipeline.
        fbx=out/"M02-006_animated.fbx"
        fbx_status="BLOCKED_FBX_OPERATOR"
        props={p.identifier for p in bpy.ops.export_scene.fbx.get_rna_type().properties}
        if "use_selection" in props or "collection" in props:
            kw={"filepath":str(fbx)}
            if "use_selection" in props:kw["use_selection"]=True
            else:kw["collection"]=coll.name
            for key,value in {"axis_forward":"-Z","axis_up":"Y",
                              "bake_anim":True,"bake_anim_use_all_actions":False,
                              "add_leaf_bones":False}.items():
                if key in props:kw[key]=value
            try:
                bpy.ops.export_scene.fbx(**kw)
                if fbx.is_file() and fbx.stat().st_size>10000:
                    fbx_status="EXPORTED_NOT_IMPORTED"
                else:
                    fbx_status="BLOCKED_FBX_EMPTY"
            except Exception as exc:
                print("M02-006 FBX unavailable:",str(exc))
                fbx_status="BLOCKED_FBX_EXPORT_ERROR"
        else:
            print("M02-006 FBX BLOCKED: operator lacks selection isolation; GLB remains valid")
        assert blend.is_file() and glb.is_file()
        return {"blender":"PASS_EXPORT","blend":str(blend),"glb":str(glb),
                "fbx":str(fbx) if fbx_status=="EXPORTED_NOT_IMPORTED" else None,
                "unity_native_fbx":fbx_status}
    finally:
        bpy.ops.object.select_all(action="DESELECT")
        for ob in previous[4]:
            if ob.name in bpy.data.objects:ob.select_set(True)
        if previous[5] is not None and previous[5].name in bpy.data.objects:
            bpy.context.view_layer.objects.active=previous[5]
        scene.frame_start,scene.frame_end,scene.render.fps=previous[:3]
        scene.frame_set(previous[3])

def main():
    argv=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else sys.argv[1:]
    ap=argparse.ArgumentParser()
    ap.add_argument("--out",default="build/M02-006")
    ap.add_argument("--self-test",action="store_true")
    args=ap.parse_args(argv)
    parts=build();report=validate(parts)
    if args.self_test:
        print(json.dumps(report,indent=2));return
    out=Path(args.out)
    report["files"]=export_obj(parts,out)
    try:import bpy
    except ImportError:pass
    else:report.update(export_blender(parts,out))
    (out/"M02-006_qa.json").write_text(json.dumps(report,indent=2)+"\n",encoding="utf-8")
    print(json.dumps(report,indent=2))
if __name__=="__main__":
    main()
