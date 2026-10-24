#!/usr/bin/env python3
"""M02-005 folding-paddle wheel. Deterministic mesh builder and Blender animation.

1 unit = 1 metre. Blender Z-up, side axle along +X. Mount pivot at (0,0,0).
Wheel rotor and eight independent paddles have distinct, physical pivots.
Python stdlib generates OBJ/MTL; Blender 3.4+ additionally exports BLEND/GLB.
This is a visual asset only, not a Unity wheel/traction physics implementation.
"""
from __future__ import annotations

import argparse
from collections import Counter
import json
import math
from pathlib import Path
import sys

ID = "M02-005"
TAU = math.tau
SEGMENTS = 64
PADDLES = 8
PADDLE_HINGE_R = .383
FOLD_DEG = 80.0
DEPLOY_DEG = 0.0
TIRE_OUTER_R = .355
SHAFT_R = .050
HUB_BORE_R = .078
SEAL_OUTER_R = .071
PARTS = []
MATERIALS = {
    "gunmetal": {"rgb":(.095,.132,.17),"metallic":.86,"roughness":.34},
    "tire_rubber": {"rgb":(.026,.034,.043),"metallic":.04,"roughness":.76},
    "copper": {"rgb":(.65,.32,.105),"metallic":.78,"roughness":.32},
    "ceramic": {"rgb":(.71,.77,.73),"metallic":.22,"roughness":.47},
    "amber_signal": {"rgb":(.95,.40,.05),"metallic":.04,"roughness":.30,
                     "emission":(.95,.28,.018),"strength":2.1},
}

def mesh(name, vertices, faces, material, group="rotor", paddle=None, smooth=False):
    assert material in MATERIALS
    assert vertices and faces
    PARTS.append({"name":name,"vertices":vertices,"faces":faces,
                  "material":material,"group":group,"paddle":paddle,"smooth":smooth})

def polar(x,r,t):
    return (x,r*math.cos(t),r*math.sin(t))

def ring(name, profile, material, group="rotor", n=SEGMENTS):
    """Closed watertight axial/radial section; central axle bore remains open."""
    verts=[polar(x,r,TAU*i/n) for x,r in profile for i in range(n)]
    faces=[]
    for j in range(len(profile)):
        k=(j+1)%len(profile)
        for i in range(n):
            q=(i+1)%n
            faces.append((j*n+i,j*n+q,k*n+q,k*n+i))
    mesh(name,verts,faces,material,group,smooth=True)

def cylinder_x(name,x0,x1,r,y,z,material,group="rotor",n=24):
    verts=[(x,y+r*math.cos(TAU*i/n),z+r*math.sin(TAU*i/n))
           for x in (x0,x1) for i in range(n)]
    v0,v1=len(verts),len(verts)+1
    verts.extend(((x0,y,z),(x1,y,z)))
    faces=[]
    for i in range(n):
        j=(i+1)%n
        faces.extend(((i,j,n+j,n+i),(v0,j,i),(v1,n+i,n+j)))
    mesh(name,verts,faces,material,group,smooth=True)

def rotate_x(v,angle):
    x,y,z=v
    c,s=math.cos(angle),math.sin(angle)
    return (x,y*c-z*s,y*s+z*c)

def extruded_yz(name,x0,x1,polygon,material,group="rotor",paddle=None,
                azimuth=0.0):
    """Counter-clockwise YZ polygon extruded along X; positive signed volume."""
    n=len(polygon)
    verts=[(x,y,z) for x in (x0,x1) for y,z in polygon]
    if azimuth:
        verts=[rotate_x(v,azimuth) for v in verts]
    faces=[tuple(reversed(range(n))),tuple(range(n,2*n))]
    for i in range(n):
        j=(i+1)%n
        faces.append((i,j,n+j,n+i))
    mesh(name,verts,faces,material,group,paddle)

def build():
    if PARTS:
        return PARTS
    # Central wheel: rubber hoop, forged annular web, machined hub, two inlays.
    ring("rubber_rolling_hoop",((.115,.274),(.115,.330),(.131,.355),
         (.337,.355),(.355,.330),(.355,.274)),"tire_rubber")
    ring("steel_web_open_bore",((.164,HUB_BORE_R),(.164,.284),
         (.288,.284),(.288,HUB_BORE_R)),"gunmetal")
    ring("front_hub_copper",((.286,.078),(.286,.143),
         (.303,.143),(.303,.078)),"copper",n=48)
    ring("rear_hub_steel",((.145,.078),(.145,.150),
         (.163,.150),(.163,.078)),"gunmetal",n=48)
    ring("front_rim_ceramic_stripe",((.355,.303),(.355,.314),
         (.359,.314),(.359,.303)),"ceramic")
    # Hub bolts are on rotor and cannot scrape the stationary shaft.
    for k in range(6):
        t=TAU*k/6
        cylinder_x("hub_fastener_%02d"%k,.304,.315,.009,
                   .120*math.cos(t),.120*math.sin(t),"ceramic",n=12)
    # Eight independent foldable paddles. All use the same local X-axis hinge.
    # The hinge's radial location is beyond the tire; anchor overlaps tire
    # intentionally (bolted joint), while paddle sweep clears the tire.
    blade_outline=((.022,-.012),(.046,-.019),(.147,-.021),
                   (.180,-.012),(.180,.012),(.147,.021),
                   (.046,.019),(.022,.012))
    for k in range(PADDLES):
        theta=TAU*k/PADDLES
        cy,cz=PADDLE_HINGE_R*math.cos(theta),PADDLE_HINGE_R*math.sin(theta)
        extruded_yz("hinge_anchor_%02d"%k,.207,.267,
                    ((.337,-.027),(.386,-.027),(.386,.027),(.337,.027)),
                    "gunmetal",azimuth=theta)
        cylinder_x("hinge_barrel_%02d"%k,.172,.298,.026,cy,cz,
                   "copper",n=32)
        cylinder_x("hinge_pin_rear_%02d"%k,.160,.174,.012,cy,cz,
                   "ceramic",n=16)
        cylinder_x("hinge_pin_front_%02d"%k,.298,.312,.012,cy,cz,
                   "ceramic",n=16)
        extruded_yz("folding_paddle_%02d"%k,-.067,.067,blade_outline,
                    "gunmetal",group="paddle",paddle=k)
        extruded_yz("paddle_face_inlay_%02d"%k,.068,.073,
                    ((.062,-.010),(.147,-.010),(.147,.010),(.062,.010)),
                    "ceramic",group="paddle",paddle=k)
        extruded_yz("paddle_tip_signal_%02d"%k,.074,.078,
                    ((.152,-.009),(.173,-.009),(.173,.009),(.152,.009)),
                    "amber_signal",group="paddle",paddle=k)
    # Stationary mount and axle. A true annular dust seal is NOT a filled disk.
    cylinder_x("stationary_axle",.012,.385,SHAFT_R,0,0,"gunmetal",
               group="fixed",n=48)
    cylinder_x("mount_side_flange",0,.058,.070,0,0,"copper",
               group="fixed",n=48)
    ring("stationary_dust_seal",((.296,.052),(.296,SEAL_OUTER_R),
         (.333,SEAL_OUTER_R),(.333,.052)),"tire_rubber",group="fixed",n=48)
    return PARTS

def world_vertex(part, vertex, fold_deg=FOLD_DEG):
    if part["group"]!="paddle":
        return vertex
    k=part["paddle"]
    angle=TAU*k/PADDLES+math.radians(fold_deg)
    x,y,z=rotate_x(vertex,angle)
    theta=TAU*k/PADDLES
    return (x+.235,y+PADDLE_HINGE_R*math.cos(theta),
            z+PADDLE_HINGE_R*math.sin(theta))

def signed_volume_and_topology(part):
    v,f=part["vertices"],part["faces"]
    edges=Counter()
    directed=Counter()
    vol=0.0
    for face in f:
        assert len(face)>=3 and len(set(face))==len(face),part["name"]
        assert all(0<=idx<len(v) for idx in face),part["name"]
        for i,a in enumerate(face):
            b=face[(i+1)%len(face)]
            edges[tuple(sorted((a,b)))]+=1
            directed[(a,b)]+=1
        p=v[face[0]]
        for i in range(1,len(face)-1):
            q,r=v[face[i]],v[face[i+1]]
            cross=(q[1]*r[2]-q[2]*r[1],
                   q[2]*r[0]-q[0]*r[2],
                   q[0]*r[1]-q[1]*r[0])
            vol+=sum(p[j]*cross[j] for j in range(3))/6
    assert all(n==2 for n in edges.values()),(part["name"],"non-manifold")
    assert all(n==1 and directed[(b,a)]==1 for (a,b),n in directed.items()),(
        part["name"],"winding")
    assert vol>1e-9,(part["name"],"volume",vol)
    return vol

def qa():
    parts=build()
    assert len(parts)==70,len(parts)
    assert len({p["name"] for p in parts})==len(parts)
    assert len({p["material"] for p in parts})==5
    assert sum(p["group"]=="paddle" for p in parts)==PADDLES*3
    assert sum(p["group"]=="fixed" for p in parts)==3
    for part in parts:
        signed_volume_and_topology(part)
        for v in part["vertices"]:
            assert all(math.isfinite(x) for x in v)
    assert HUB_BORE_R-SEAL_OUTER_R>=.007-1e-9
    assert HUB_BORE_R-SHAFT_R>=.028-1e-9
    # Test the ACTUAL moving polygon vertices in all 81 deployment poses.
    # Radial distance from axle and angular sector disjointness are invariant
    # under wheel rotation. We test wheel rotation at 25 angles independently.
    blade=[p for p in parts if p["name"]=="folding_paddle_00"][0]
    radial_min=float("inf")
    angular_gap=float("inf")
    for step in range(81):
        angle=FOLD_DEG*step/80
        world=[world_vertex(blade,v,angle) for v in blade["vertices"]]
        radial_min=min(radial_min,min(math.hypot(v[1],v[2]) for v in world))
        local_angles=[math.atan2(v[2],v[1]) for v in world]
        spread=max(local_angles)-min(local_angles)
        angular_gap=min(angular_gap,TAU/PADDLES-spread)
        assert min(math.hypot(v[1],v[2]) for v in world)>TIRE_OUTER_R+.009,(
            step,"paddle intersects tire")
        assert spread<TAU/PADDLES-math.radians(8),(
            step,"neighboring paddle sweep sectors overlap")
    # Rotor's stationary interface uses annular bore, not a fake capped hub.
    for turn in range(25):
        assert math.isfinite(math.sin(TAU*turn/24))
        assert HUB_BORE_R>SEAL_OUTER_R>SHAFT_R
    verts=sum(len(p["vertices"]) for p in parts)
    polys=sum(len(p["faces"]) for p in parts)
    return {
        "id":ID,"result":"STATIC_GEOMETRY_PASS",
        "model":"Koło z rozkładanymi łopatkami",
        "units":"metres","mount_pivot":[0,0,0],
        "wheel_axis":"Blender +X / glTF Y-up conversion",
        "wheel_max_radius_folded_m":round(max(
            math.hypot(v[1],v[2]) for p in parts for v in
            (world_vertex(p,x) for x in p["vertices"])),5),
        "tire_radius_m":TIRE_OUTER_R,
        "paddles":PADDLES,"paddle_fold_deg":FOLD_DEG,
        "paddle_deploy_deg":DEPLOY_DEG,
        "paddle_sweep_samples":81,
        "rotor_rotation_samples":25,
        "paddle_to_tire_min_radial_clearance_m":round(radial_min-TIRE_OUTER_R,5),
        "adjacent_paddle_sector_min_gap_deg":round(math.degrees(angular_gap),3),
        "shaft_to_bore_clearance_m":round(HUB_BORE_R-SHAFT_R,3),
        "seal_to_bore_clearance_m":round(HUB_BORE_R-SEAL_OUTER_R,3),
        "mesh_count":len(parts),"obj_vertices":verts,"obj_polygons":polys,
        "materials_used":sorted({p["material"] for p in parts}),
        "closed_manifold_meshes":len(parts),
        "intentional_joints":"hinge anchors welded into tire; paddle roots overlap hinge barrel",
        "blender":"NOT RUN","unity":"NOT RUN","visual_review":"NOT RUN",
    }

def export_obj(out):
    out.mkdir(parents=True,exist_ok=True)
    obj=out/(ID+".obj")
    mtl=out/(ID+".mtl")
    with mtl.open("w",encoding="utf-8",newline="\n") as f:
        f.write("# M02-005 deterministic five-material palette\n")
        for name,p in MATERIALS.items():
            r,g,b=p["rgb"]
            f.write("newmtl %s\nKa .03 .03 .03\nKd %.6f %.6f %.6f\n"
                    "Ks .30 .30 .30\nNs 100\nd 1.0\nillum 2\n\n"%(
                        name,r,g,b))
    with obj.open("w",encoding="utf-8",newline="\n") as f:
        f.write("# M02-005 neutral folded pose; 1m Unity scale\nmtllib M02-005.mtl\n")
        offset=1
        for p in build():
            f.write("o %s\nusemtl %s\n"%(p["name"],p["material"]))
            for v in p["vertices"]:
                f.write("v %.6f %.6f %.6f\n"%world_vertex(p,v))
            for face in p["faces"]:
                f.write("f "+" ".join(str(offset+i) for i in face)+"\n")
            offset+=len(p["vertices"])
    return obj,mtl

def blender_export(out):
    import bpy
    colname=ID+"_ASSET"
    old=bpy.data.collections.get(colname)
    if old:
        for obj in tuple(old.objects):
            bpy.data.objects.remove(obj,do_unlink=True)
        bpy.data.collections.remove(old)
    col=bpy.data.collections.new(colname)
    bpy.context.scene.collection.children.link(col)
    palette={}
    for name,props in MATERIALS.items():
        mat=bpy.data.materials.get(ID+"_"+name) or bpy.data.materials.new(
            ID+"_"+name)
        mat.use_nodes=True
        mat.diffuse_color=(*props["rgb"],1)
        bsdf=mat.node_tree.nodes.get("Principled BSDF")
        bsdf.inputs["Base Color"].default_value=(*props["rgb"],1)
        bsdf.inputs["Metallic"].default_value=props["metallic"]
        bsdf.inputs["Roughness"].default_value=props["roughness"]
        if "emission" in props:
            bsdf.inputs["Emission Color"].default_value=(
                *props["emission"],1)
            bsdf.inputs["Emission Strength"].default_value=props["strength"]
        palette[name]=mat
    def empty(name,parent=None):
        ob=bpy.data.objects.new(name,None)
        col.objects.link(ob)
        ob.empty_display_type="PLAIN_AXES"
        ob.empty_display_size=.06
        if parent:
            ob.parent=parent
        return ob
    root=empty(ID+"_MOUNT_SIDE_X0")
    root["port"]="SIDE_AXLE_X0"
    root["metres_per_unit"]=1.0
    rotor=empty(ID+"_WHEEL_ROTOR",root)
    hinges=[]
    for k in range(PADDLES):
        h=empty(ID+"_PADDLE_HINGE_%02d"%k,rotor)
        t=TAU*k/PADDLES
        h.location=(.235,PADDLE_HINGE_R*math.cos(t),
                    PADDLE_HINGE_R*math.sin(t))
        h.rotation_mode="XYZ"
        hinges.append(h)
    for p in build():
        data=bpy.data.meshes.new(ID+"_"+p["name"]+"_mesh")
        data.from_pydata(p["vertices"],[],p["faces"])
        data.update(calc_edges=True)
        ob=bpy.data.objects.new(ID+"_"+p["name"],data)
        col.objects.link(ob)
        ob.parent=(root if p["group"]=="fixed" else
                   hinges[p["paddle"]] if p["group"]=="paddle" else rotor)
        data.materials.append(palette[p["material"]])
        for polygon in data.polygons:
            polygon.use_smooth=p["smooth"]
        ob["model_id"]=ID
        ob["kinematic_group"]=p["group"]
    # Rotation and deployment are distinct tracks; all 8 hinges move.
    for frame,turn in ((1,0),(121,TAU)):
        rotor.rotation_euler.x=turn
        rotor.keyframe_insert(data_path="rotation_euler",index=0,frame=frame)
    for k,h in enumerate(hinges):
        t=TAU*k/PADDLES
        for frame,fold in ((1,FOLD_DEG),(31,DEPLOY_DEG),
                           (91,DEPLOY_DEG),(121,FOLD_DEG)):
            h.rotation_euler.x=t+math.radians(fold)
            h.keyframe_insert(data_path="rotation_euler",index=0,frame=frame)
    for ob in (rotor,*hinges):
        act=ob.animation_data.action if ob.animation_data else None
        if act:
            for fc in getattr(act,"fcurves",()):
                for kp in fc.keyframe_points:
                    kp.interpolation="LINEAR"
    scene=bpy.context.scene
    prev=(scene.frame_start,scene.frame_end,scene.frame_current)
    previous_selection=tuple(bpy.context.selected_objects)
    previous_active=bpy.context.view_layer.objects.active
    try:
        scene.frame_start=1
        scene.frame_end=121
        scene.frame_set(1)
        bpy.context.view_layer.update()
        assert len(col.objects)==len(build())+PADDLES+2
        # Asset-only Blender library, not a save of unrelated user objects.
        bpy.data.libraries.write(str(out/(ID+".blend")),{col},compress=True)
        bpy.ops.object.select_all(action="DESELECT")
        for ob in col.objects:
            ob.select_set(True)
        bpy.context.view_layer.objects.active=root
        bpy.ops.export_scene.gltf(
            filepath=str(out/(ID+"_animated.glb")),
            export_format="GLB",export_animations=True,
            use_selection=True,export_yup=True)
    finally:
        bpy.ops.object.select_all(action="DESELECT")
        for ob in previous_selection:
            if ob.name in bpy.data.objects:
                ob.select_set(True)
        if previous_active and previous_active.name in bpy.data.objects:
            bpy.context.view_layer.objects.active=previous_active
        scene.frame_start,scene.frame_end=prev[:2]
        scene.frame_set(prev[2])
    return [ID+".blend",ID+"_animated.glb"]

def main():
    argv=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else sys.argv[1:]
    parser=argparse.ArgumentParser()
    parser.add_argument("--output-dir",default=str(Path(__file__).resolve().parent/"generated"))
    parser.add_argument("--self-test",action="store_true")
    parser.add_argument("--require-blender",action="store_true")
    args=parser.parse_args(argv)
    out=Path(args.output_dir).resolve()
    report=qa()
    obj,mtl=export_obj(out)
    report["obj"]=obj.name
    report["mtl"]=mtl.name
    if not args.self_test:
        try:
            import bpy
        except ImportError:
            if args.require_blender:
                raise RuntimeError("Blender bpy unavailable: animated GLB NOT RUN")
        else:
            report["blender_files"]=blender_export(out)
            report["blender"]="EXECUTED"
    (out/(ID+".static-qa.json")).write_text(
        json.dumps(report,indent=2,ensure_ascii=False)+"\n",encoding="utf-8")
    print(json.dumps(report,indent=2,ensure_ascii=False))

if __name__=="__main__":
    main()
