#!/usr/bin/env python3
"""M01-015: Dwukierunkowa wieżyczka strzelecka — procedural visual asset.

Unity 6: 1 unit = 1 metre; Y up, +Z forward. MOUNT_TOP_SOCKET
at (0,0,0), top attachment plane. Pure geometry builds OBJ/MTL
without Blender; Blender 4+ additionally exports animated FBX/GLB/BLEND.
Visual-only: no projectile, damage, targeting or gameplay logic.
"""
from __future__ import annotations
import argparse
import json
import math
import sys
from pathlib import Path

ASSET_ID = "M01-015"
STEM = "M01-015_bidirectional_turret"
FRAMES = 73
FPS = 30
MATS = {
    "Armor_Graphite": ((.066,.084,.105,1),.72,.36,(0,0,0),0),
    "Titanium_Frame": ((.35,.42,.48,1),.83,.29,(0,0,0),0),
    "Ceramic_Ivory": ((.78,.82,.78,1),.20,.34,(0,0,0),0),
    "Copper_Guides": ((.54,.24,.105,1),.84,.26,(0,0,0),0),
    "Gasket_Dark": ((.025,.032,.043,1),.07,.82,(0,0,0),0),
    "Signal_Cyan": ((.09,.62,.88,1),.16,.28,(.04,.53,.85),2.5),
    "Warning_Amber": ((.96,.38,.05,1),.27,.35,(.95,.23,.03),.35),
}

def sub(a,b): return tuple(a[i]-b[i] for i in range(3))
def cross(a,b): return (a[1]*b[2]-a[2]*b[1], a[2]*b[0]-a[0]*b[2], a[0]*b[1]-a[1]*b[0])
def dot(a,b): return sum(a[i]*b[i] for i in range(3))
def rotate_xy(v,theta,origin=(0.,0.)):
    x,y,z=v; a=x-origin[0]; b=y-origin[1]
    c=math.cos(theta); s=math.sin(theta)
    return (origin[0]+c*a-s*b,origin[1]+s*a+c*b,z)

def part(name,material,v,f,moving=False):
    return {"name":name,"material":material,"vertices":v,"faces":f,"moving":moving}

def box(name,material,x,y,z,sx,sy,sz,cut=.008,moving=False,angle=0):
    assert min(sx,sy,sz)>0 and 0<cut<min(sx,sy)/2
    a,b=sx/2,sy/2
    xy=[(-a+cut,-b),(a-cut,-b),(a,-b+cut),(a,b-cut),
        (a-cut,b),(-a+cut,b),(-a,b-cut),(-a,-b+cut)]
    v=[(x+u,y+w,z+q) for q in (-sz/2,sz/2) for u,w in xy]
    v.extend(((x,y,z-sz/2),(x,y,z+sz/2)))
    if angle: v=[rotate_xy(p,angle,(x,y)) for p in v]
    f=[]
    for i in range(8):
        j=(i+1)%8
        f.extend(((i,j,8+j),(i,8+j,8+i)))
        f.append((16,j,i));f.append((17,8+i,8+j))
    return part(name,material,v,f,moving)

def tube(name,material,x,y,z0,z1,inner,outer,segments=32,moving=False):
    """A truly hollow, closed annular sleeve, not overlapping solid discs."""
    assert z1>z0 and 0<inner<outer and segments>=6
    profile=((z0,inner),(z0,outer),(z1,outer),(z1,inner))
    v=[(x+r*math.cos(2*math.pi*j/segments),
        y+r*math.sin(2*math.pi*j/segments),z)
       for z,r in profile for j in range(segments)]
    f=[]
    for i in range(4):
        nxt=(i+1)%4
        for j in range(segments):
            k=(j+1)%segments
            a=i*segments+j;b=i*segments+k
            c=nxt*segments+k;d=nxt*segments+j
            f.extend(((a,b,c),(a,c,d)))
    return part(name,material,v,f,moving)

def cylinder(name,material,x,y,z0,z1,r,segments=16,moving=False):
    assert z1>z0 and r>0 and segments>=6
    v=[(x+r*math.cos(2*math.pi*j/segments),
        y+r*math.sin(2*math.pi*j/segments),z)
       for z in (z0,z1) for j in range(segments)]
    v.extend(((x,y,z0),(x,y,z1)))
    f=[]
    for j in range(segments):
        k=(j+1)%segments
        f.extend(((j,k,segments+k),(j,segments+k,segments+j)))
        f.extend(((2*segments,k,j),(2*segments+1,segments+j,segments+k)))
    return part(name,material,v,f,moving)



ANGLE_REST_DEG = 16.0
ANGLE_WIDE_DEG = 32.0
ANGLE_NARROW_DEG = 8.0
PIVOT_X = 0.25
PIVOT_Z = 0.26
BEARING_INNER_R = 0.069
SHAFT_R = 0.052

def rotate_y(v, angle):
    x,y,z=v
    c=math.cos(angle);s=math.sin(angle)
    return (c*x+s*z,y,-s*x+c*z)

def world_vertex(v,side,angle_deg):
    if not side:return v
    x,y,z=rotate_y(v,math.radians(side*angle_deg))
    return (x+side*PIVOT_X,y,z+PIVOT_Z)

def static_pose(parts):
    return [dict(p,vertices=[world_vertex(v,p.get("side",0),ANGLE_REST_DEG)
                              for v in p["vertices"]]) for p in parts]

def box_xz(name,mat,x,y,z,sx,sy,sz,angle,cut=.008):
    p=box(name,mat,x,y,z,sx,sy,sz,cut)
    p["vertices"]=[(x+rotate_y((a-x,b-y,c-z),angle)[0],
                    b,
                    z+rotate_y((a-x,b-y,c-z),angle)[2])
                   for a,b,c in p["vertices"]]
    return p

def cylinder_y(name,mat,x,z,y0,y1,r,segments=20,moving=False,side=0):
    p=cylinder(name,mat,0,0,y0,y1,r,segments,moving)
    p["vertices"]=[(x+a,h,z-b) for a,b,h in p["vertices"]]
    p["side"]=side
    return p

def tube_y(name,mat,x,z,y0,y1,inner,outer,segments=32,moving=False,side=0):
    p=tube(name,mat,0,0,y0,y1,inner,outer,segments,moving)
    p["vertices"]=[(x+a,h,z-b) for a,b,h in p["vertices"]]
    p["side"]=side
    return p

def build():
    """Low-profile V-frame with two independently yawing, outward-pointing arms.

    Pivot origin: MOUNT_TOP_SOCKET=(0,0,0) at bottom mounting plane.
    Right/left arm origins: (+/-0.25,0,0.26) metres.
    Both arm meshes use pivot-local vertices; neutral yaw = +/-16 deg.
    Only visual geometry/animation, no weapon behaviour.
    """
    p=[];add=p.append
    # Fixed V-shaped load-bearing chassis and two isolated yaw bearings.
    add(box("Mount_Top_Socket","Armor_Graphite",0,.07,0,.80,.14,.70,.024))
    add(box("Mount_Grip_Titanium","Titanium_Frame",0,.165,.04,.62,.09,.55,.018))
    add(box("V_Central_Keel","Armor_Graphite",0,.275,.12,.23,.16,.45,.014))
    add(box("V_Armor_Nose","Ceramic_Ivory",0,.345,.31,.22,.035,.30,.008))
    add(box("V_Copper_Spine","Copper_Guides",0,.370,.11,.065,.020,.31,.006))
    for side,label in ((-1,"L"),(1,"R")):
        sx=side*PIVOT_X
        add(box("Pedestal_"+label,"Titanium_Frame",sx,.278,PIVOT_Z,
                .25,.165,.27,.018))
        add(box_xz("V_Rail_"+label,"Titanium_Frame",
                   side*.16,.376,.245,.078,.055,.49,side*.37,.010))
        add(box_xz("V_Ceramic_Rib_"+label,"Ceramic_Ivory",
                   side*.16,.408,.25,.047,.015,.34,side*.37,.004))
        add(box("Side_Rear_Armor_"+label,"Armor_Graphite",
                side*.34,.238,.01,.10,.10,.30,.012))
        add(box("Side_Warning_"+label,"Warning_Amber",
                side*.35,.302,.01,.055,.014,.095,.004))
        add(tube_y("Bearing_Ring_"+label,"Titanium_Frame",
                   sx,PIVOT_Z,.385,.490,BEARING_INNER_R,.092,32))
        add(tube_y("Bearing_Copper_Lip_"+label,"Copper_Guides",
                   sx,PIVOT_Z,.485,.497,BEARING_INNER_R,.088,32))
        # Annular bearings intentionally surround a smaller shaft with 17mm
        # radial clearance. Their boxes overlap, their actual solids do not.
        for j,z in enumerate((-.20,.04,.23),1):
            add(box("Rivet_"+label+str(j),"Copper_Guides",
                    side*.345,.153,z,.022,.014,.024,.004))
    # Moving arm assemblies: no inward swing; distinct +/- yaw about Y.
    for side,label in ((-1,"L"),(1,"R")):
        def m(q):
            q["moving"]=True;q["side"]=side;add(q)
        m(cylinder_y("Pivot_Shaft_"+label,"Titanium_Frame",0,0,
                     .425,.527,SHAFT_R,32,True,side))
        m(cylinder_y("Pivot_Cap_"+label,"Copper_Guides",0,0,
                     .530,.552,.078,32,True,side))
        m(box("Arm_Root_"+label,"Armor_Graphite",0,.608,.115,
              .205,.120,.35,.017,True))
        m(box("Arm_Long_Carrier_"+label,"Titanium_Frame",0,.651,.535,
              .175,.15,.84,.017,True))
        m(box("Arm_Front_Shell_"+label,"Armor_Graphite",0,.648,1.06,
              .215,.184,.31,.018,True))
        m(box("Arm_Rear_Ceramic_"+label,"Ceramic_Ivory",0,.692,.18,
              .18,.035,.20,.007,True))
        m(box("Arm_Dorsal_Ceramic_"+label,"Ceramic_Ivory",0,.743,.66,
              .136,.024,.67,.007,True))
        m(box("Arm_Dorsal_Copper_"+label,"Copper_Guides",0,.766,.70,
              .038,.015,.56,.004,True))
        for edge,tag in ((-1,"A"),(1,"B")):
            m(box("Arm_Flank_"+label+tag,"Ceramic_Ivory",edge*.112,.657,.74,
                  .031,.155,.72,.007,True))
            m(box("Arm_Flank_Trace_"+label+tag,"Signal_Cyan",edge*.134,.663,.72,
                  .011,.036,.56,.003,True))
        # Hollow weapon tube and luminous muzzle — visually readable
        # without inserting new shooting or projectile behaviour.
        m(tube("Arm_Barrel_"+label,"Titanium_Frame",0,.649,
               .835,1.485,.033,.055,32,True))
        m(tube("Muzzle_Ceramic_"+label,"Ceramic_Ivory",0,.649,
               1.465,1.525,.034,.079,32,True))
        m(tube("Muzzle_Emissive_"+label,"Signal_Cyan",0,.649,
               1.525,1.543,.035,.070,32,True))
        m(tube("Muzzle_Copper_Rim_"+label,"Copper_Guides",0,.649,
               1.538,1.554,.039,.082,32,True))
        for j,z in enumerate((.39,.53,.67,.81),1):
            m(box("Copper_Cooling_Fin_"+label+str(j),"Copper_Guides",
                  0,.559,z,.145,.018,.032,.005,True))
        m(box("Rear_Emitter_Block_"+label,"Gasket_Dark",0,.643,.05,
              .145,.105,.12,.012,True))
        m(box("Rear_Emitter_Signal_"+label,"Warning_Amber",0,.718,.04,
              .105,.012,.07,.003,True))
    return p

def bounds(v):
    return tuple((min(a[i] for a in v),max(a[i] for a in v)) for i in range(3))

def signed_volume(p):
    v=p["vertices"]
    return sum(dot(v[a],cross(v[b],v[c]))/6 for a,b,c in p["faces"])

def check_topology(parts):
    names=set(); total_faces=0;total_vertices=0
    for p in parts:
        assert p["name"] not in names,p["name"]
        names.add(p["name"])
        assert p["material"] in MATS
        v=p["vertices"];faces=p["faces"]
        assert v and faces
        assert all(all(math.isfinite(x) for x in q) for q in v)
        edges={}; orientation={}
        for a,b,c in faces:
            assert len({a,b,c})==3 and min(a,b,c)>=0 and max(a,b,c)<len(v)
            assert dot(cross(sub(v[b],v[a]),sub(v[c],v[a])),
                       cross(sub(v[b],v[a]),sub(v[c],v[a])))>1e-15,(p["name"],"degenerate")
            for u,w in ((a,b),(b,c),(c,a)):
                e=tuple(sorted((u,w)))
                edges[e]=edges.get(e,0)+1
                orientation[e]=orientation.get(e,0)+(1 if u<w else -1)
        assert all(n==2 for n in edges.values()),(p["name"],"nonmanifold")
        assert all(o==0 for o in orientation.values()),(p["name"],"inconsistent winding")
        assert signed_volume(p)>1e-10,(p["name"],"inward winding",signed_volume(p))
        total_faces+=len(faces);total_vertices+=len(v)
    return {"parts":len(parts),"vertices":total_vertices,"triangles":total_faces,
            "materials":len(MATS),"watertight_oriented_positive_volume":"PASS"}


def check_clearance(parts):
    """73-pose conservative AABB sweep, both yaw arms and fixed housing.

    Bearing/shaft pairs are treated with exact concentric radial clearance;
    all other moving/fixed pairs must have separated AABBs. This is a
    discrete geometric QA, NOT a continuous mesh collision solver.
    """
    fixed=[q for q in parts if not q["moving"]]
    moving=[q for q in parts if q["moving"]]
    assert len([q for q in moving if q["name"].startswith("Pivot_Shaft_")])==2
    radial_gap=BEARING_INNER_R-SHAFT_R
    assert radial_gap>=.012,radial_gap
    checks=0; cross_checks=0; gaps_all=[]
    for i in range(FRAMES):
        t=i/(FRAMES-1)
        # Four key phases, mirrored yaw; never turns inward.
        if t<=.25:deg=ANGLE_REST_DEG+(ANGLE_WIDE_DEG-ANGLE_REST_DEG)*t/.25
        elif t<=.50:deg=ANGLE_WIDE_DEG-(ANGLE_WIDE_DEG-ANGLE_REST_DEG)*(t-.25)/.25
        elif t<=.75:deg=ANGLE_REST_DEG-(ANGLE_REST_DEG-ANGLE_NARROW_DEG)*(t-.50)/.25
        else:deg=ANGLE_NARROW_DEG+(ANGLE_REST_DEG-ANGLE_NARROW_DEG)*(t-.75)/.25
        world={q["name"]:bounds([world_vertex(v,q.get("side",0),deg)
                                for v in q["vertices"]]) for q in moving}
        for q in moving:
            bb=world[q["name"]]
            for f in fixed:
                if (q["name"].startswith("Pivot_Shaft_") and
                    f["name"].startswith(("Bearing_Ring_","Bearing_Copper_Lip_")) and
                    q["name"].split("_")[-1]==f["name"].split("_")[-1]):
                    continue # concentric shaft and annular bearing: 17mm gap
                ff=bounds(f["vertices"])
                sep=max(max(bb[k][0]-ff[k][1],ff[k][0]-bb[k][1]) for k in range(3))
                checks+=1
                assert sep>1e-7,("moving/fixed overlap",i,q["name"],f["name"],sep)
                gaps_all.append(sep)
        left=[q for q in moving if q["side"]==-1]
        right=[q for q in moving if q["side"]==1]
        for l in left:
            a=world[l["name"]]
            for r in right:
                b=world[r["name"]]
                sep=max(max(a[k][0]-b[k][1],b[k][0]-a[k][1]) for k in range(3))
                cross_checks+=1
                assert sep>1e-7,("left/right arms overlap",i,l["name"],r["name"],sep)
                gaps_all.append(sep)
    assert min(gaps_all)>.004,min(gaps_all)
    return {"sampled_animation_frames":FRAMES,
            "aabb_pair_checks":checks,"cross_arm_aabb_checks":cross_checks,
            "moving_parts":len(moving),"moving_vs_fixed_collisions":0,
            "cross_arm_collisions":0,
            "minimum_separation_aabb_m":round(min(gaps_all),6),
            "minimum_radial_bearing_clearance_m":round(radial_gap,6),
            "arm_yaw_range_degrees":[ANGLE_NARROW_DEG,ANGLE_WIDE_DEG],
            "clearance":"PASS (sampled AABB + analytic concentric bearing clearance)"}

def export_obj(parts,out):
    obj=out/(STEM+".obj");mtl=out/(STEM+".mtl")
    lines=["# M01-015: Unity Y up, +Z forward; top mount (0,0,0)","mtllib "+mtl.name]
    offset=0
    for p in parts:
        lines.extend(("o "+p["name"],"usemtl "+p["material"]))
        for x,y,z in p["vertices"]: lines.append(f"v {x:.8f} {y:.8f} {z:.8f}")
        for a,b,c in p["faces"]:lines.append(f"f {a+offset+1} {b+offset+1} {c+offset+1}")
        offset+=len(p["vertices"])
    obj.write_text("\n".join(lines)+"\n",encoding="utf-8")
    m=["# static MTL; animated PBR emission via Blender FBX/GLB"]
    for name,(rgba,metal,rough,emission,strength) in MATS.items():
        m.extend(("newmtl "+name,
                  "Kd %.5f %.5f %.5f"%rgba[:3],
                  "Ks %.5f %.5f %.5f"%(metal,metal,metal),
                  "Ns %.3f"%((1-rough)*1000),
                  "Ke %.5f %.5f %.5f"%tuple(min(1.,v*strength) for v in emission),
                  "d %.4f"%rgba[3],"illum 2",""))
    mtl.write_text("\n".join(m)+"\n",encoding="utf-8")
    return [obj.name,mtl.name]

def blender_export(parts,out):
    import bpy
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for mesh in list(bpy.data.meshes):
        if mesh.users==0:bpy.data.meshes.remove(mesh)
    for col in list(bpy.data.collections):
        if col.name.startswith("M01_015_BIDIRECTIONAL_TURRET"):
            bpy.data.collections.remove(col)
    for mat in list(bpy.data.materials):
        if mat.name.startswith("M01_015_") and mat.users==0:
            bpy.data.materials.remove(mat)
    scene=bpy.context.scene
    collection=bpy.data.collections.new("M01_015_BIDIRECTIONAL_TURRET")
    scene.collection.children.link(collection)
    def empty(name,loc=(0.,0.,0.),parent=None):
        ob=bpy.data.objects.new(name,None)
        collection.objects.link(ob)
        if parent:ob.parent=parent
        ob.location=loc
        ob.empty_display_size=.065
        return ob
    mount=empty("MOUNT_TOP_SOCKET")
    left=empty("ARM_L_YAW",(-PIVOT_X,0.,PIVOT_Z),mount)
    right=empty("ARM_R_YAW",(PIVOT_X,0.,PIVOT_Z),mount)
    parents={-1:left,1:right,0:mount}
    materials={}
    for name,(rgba,metal,rough,emission,strength) in MATS.items():
        mat=bpy.data.materials.new("M01_015_"+name)
        mat.use_nodes=True
        bsdf=mat.node_tree.nodes.get("Principled BSDF")
        if bsdf:
            bsdf.inputs["Base Color"].default_value=rgba
            bsdf.inputs["Metallic"].default_value=metal
            bsdf.inputs["Roughness"].default_value=rough
            if "Emission Color" in bsdf.inputs:
                bsdf.inputs["Emission Color"].default_value=(*emission,1.)
            if "Emission Strength" in bsdf.inputs:
                bsdf.inputs["Emission Strength"].default_value=strength
        mat.diffuse_color=rgba
        materials[name]=mat
    for p in parts:
        mesh=bpy.data.meshes.new("M01_015_"+p["name"])
        mesh.from_pydata(p["vertices"],[],p["faces"])
        mesh.update(calc_edges=True)
        ob=bpy.data.objects.new("M01_015_"+p["name"],mesh)
        collection.objects.link(ob)
        ob.parent=parents[p.get("side",0)]
        mesh.materials.append(materials[p["material"]])
        for face in mesh.polygons:face.use_smooth=False
    scene.frame_start=1;scene.frame_end=FRAMES;scene.render.fps=FPS
    keys=((1,ANGLE_REST_DEG),(19,ANGLE_WIDE_DEG),(37,ANGLE_REST_DEG),
          (55,ANGLE_NARROW_DEG),(73,ANGLE_REST_DEG))
    for sign,ob in ((-1,left),(1,right)):
        for frame,deg in keys:
            ob.rotation_euler.y=math.radians(sign*deg)
            ob.keyframe_insert(data_path="rotation_euler",index=1,frame=frame)
        action=ob.animation_data.action
        if action:
            try:
                for curve in action.fcurves:
                    for key in curve.keyframe_points:key.interpolation="LINEAR"
            except AttributeError:pass
    scene.frame_set(1)
    bpy.ops.object.select_all(action="DESELECT")
    for ob in collection.objects:ob.select_set(True)
    bpy.context.view_layer.objects.active=mount
    fbx=out/(STEM+".fbx");glb=out/(STEM+".glb")
    blend=out/(STEM+".blend")
    bpy.ops.export_scene.fbx(filepath=str(fbx),use_selection=True,
        axis_forward="-Z",axis_up="Y",add_leaf_bones=False,
        bake_anim=True,bake_anim_use_all_actions=False)
    bpy.ops.export_scene.gltf(filepath=str(glb),export_format="GLB",
        use_selection=True,export_animations=True)
    bpy.context.preferences.filepaths.save_version=0
    bpy.ops.wm.save_as_mainfile(filepath=str(blend))
    for backup in out.glob(STEM+".blend[0-9]*"):backup.unlink()
    return {"blender_export":"PASS (files written; visual inspection pending)",
            "blender_version":".".join(str(x) for x in bpy.app.version),
            "animated_files":[fbx.name,glb.name,blend.name],
            "animated_nodes":["ARM_L_YAW","ARM_R_YAW"],"keyframes_per_arm":len(keys)}

def main():
    argv=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else sys.argv[1:]
    parser=argparse.ArgumentParser()
    parser.add_argument("--out",default="build/M01-015")
    parser.add_argument("--self-test",action="store_true")
    parser.add_argument("--repeat-blender",action="store_true")
    args=parser.parse_args(argv)
    parts=build()
    report={"asset_id":ASSET_ID,"model":"Dwukierunkowa wieżyczka strzelecka",
            "mount":"MOUNT_TOP_SOCKET (0,0,0), Y-up, +Z-forward, metres",
            "motion":"ARM_L_YAW / ARM_R_YAW local Y yaw, mirrored +/-8..32 degrees",
            **check_topology(parts),**check_clearance(parts),
            "blender_export":"NOT RUN","unity_import":"NOT RUN",
            "unity_editmode":"NOT RUN","unity_playmode":"NOT RUN",
            "visual_inspection":"NOT RUN"}
    if args.self_test:
        print(json.dumps(report,indent=2,ensure_ascii=False))
        return
    out=Path(args.out).resolve();out.mkdir(parents=True,exist_ok=True)
    report["static_files"]=export_obj(static_pose(parts),out)
    try:import bpy
    except ImportError:report["backend"]="plain-python-static-obj"
    else:
        report["backend"]="blender-bpy"
        report.update(blender_export(parts,out))
        if args.repeat_blender:
            first=(len(bpy.data.objects),len(bpy.data.materials),
                   len([c for c in bpy.data.collections
                        if c.name.startswith("M01_015_BIDIRECTIONAL_TURRET")]))
            report.update(blender_export(parts,out))
            second=(len(bpy.data.objects),len(bpy.data.materials),
                    len([c for c in bpy.data.collections
                         if c.name.startswith("M01_015_BIDIRECTIONAL_TURRET")]))
            assert first==second,("Blender scene leaked objects/materials",first,second)
            report["blender_repeatability"]="PASS (two builds in one process)"
            report["blender_scene_counts"]=list(second)
    (out/(STEM+"_qa.json")).write_text(json.dumps(report,indent=2,ensure_ascii=False)+"\n",encoding="utf-8")
    print(json.dumps(report,indent=2,ensure_ascii=False))

if __name__=="__main__":main()
