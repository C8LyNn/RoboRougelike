#!/usr/bin/env python3
"""M01-013: Wyrzutnia siatki zakłócającej — procedural visual asset.

Unity 6: 1 unit = 1 metre; Y up, +Z forward. MOUNT_TOP_SOCKET
at (0,0,0), bottom of mounting plate. Pure geometry builds OBJ/MTL
without Blender; Blender 4+ additionally exports animated FBX/GLB/BLEND.
Visual-only: no projectile, damage, targeting or gameplay logic.
"""
from __future__ import annotations
import argparse
import json
import math
import sys
from pathlib import Path

ASSET_ID = "M01-013"
STEM = "M01-013_disruption_net_launcher"
ROTOR_CENTRE = (0.0, 0.54, 0.0)  # local Z rotation axis through the cassette
ROTOR_RADIUS = .195
ROLLER_RADIUS = .052
ROLLER_COUNT = 6
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

def build():
    p=[]; add=p.append
    # Load path: top socket -> riser -> chassis -> bearing spine -> axle.
    add(box("Mount_Top_Socket","Armor_Graphite",0,.045,0,.55,.09,.38,.022))
    add(box("Mount_Key_L","Ceramic_Ivory",-.18,.095,-.065,.05,.02,.17,.008))
    add(box("Mount_Key_R","Ceramic_Ivory", .18,.095,-.065,.05,.02,.17,.008))
    add(box("Mount_Riser","Copper_Guides",0,.167,.07,.22,.22,.18,.016))
    add(box("Main_Chassis","Armor_Graphite",0,.295,.28,.64,.15,.50,.034))
    add(box("Dorsal_Armor","Ceramic_Ivory",0,.382,.31,.30,.028,.36,.010))
    add(box("Rear_Heat_Sink","Titanium_Frame",0,.263,.08,.43,.035,.18,.010))
    for side,label in ((-1,"L"),(1,"R")):
        add(box("Support_Bracket_"+label,"Titanium_Frame",
                side*.33,.43,.49,.06,.18,.24,.010))
        add(box("Outer_Guard_"+label,"Armor_Graphite",
                side*.34,.54,.77,.05,.65,.42,.012))
        add(box("Guard_Ceramic_"+label,"Ceramic_Ivory",
                side*.369,.54,.77,.012,.27,.28,.004))
        add(box("Guard_Signal_"+label,"Warning_Amber",
                side*.378,.58,.84,.008,.075,.12,.002))
        add(box("Rear_Fin_"+label,"Copper_Guides",
                side*.26,.316,.30,.048,.032,.35,.009))
        add(box("Cable_Anchor_"+label,"Gasket_Dark",
                side*.225,.315,.145,.07,.055,.11,.010))
    add(box("Upper_Guard","Armor_Graphite",0,.835,.77,.69,.06,.42,.012))
    add(box("Lower_Guard","Armor_Graphite",0,.245,.77,.69,.06,.42,.012))
    add(box("Upper_Guard_Trim","Ceramic_Ivory",0,.869,.76,.40,.012,.28,.004))
    add(box("Lower_Guard_Trim","Titanium_Frame",0,.211,.76,.40,.012,.28,.004))
    add(box("Bearing_Spine","Titanium_Frame",0,.438,.48,.14,.20,.18,.014))
    add(cylinder("Fixed_Axle","Titanium_Frame",0,.54,.54,.925,.040,32))
    add(tube("Axle_Gasket","Gasket_Dark",0,.54,.552,.590,.041,.058,32))
    # Rotor: all parts share a parent at (0,0.54,0), rotating about local Z.
    add(tube("Cassette_Rear_Annulus","Armor_Graphite",0,.54,.620,.643,
             .064,.247,48,True))
    add(tube("Cassette_Hub","Copper_Guides",0,.54,.640,.916,
             .057,.071,32,True))
    add(tube("Cassette_Front_Collar","Ceramic_Ivory",0,.54,.916,.943,
             .056,.090,32,True))
    add(tube("Cassette_Cyan_Lip","Signal_Cyan",0,.54,.943,.954,
             .061,.080,32,True))
    for i in range(ROLLER_COUNT):
        angle=i*math.tau/ROLLER_COUNT
        x=ROTOR_RADIUS*math.cos(angle)
        y=.54+ROTOR_RADIUS*math.sin(angle)
        name=f"{i+1:02d}"
        # Radial load-bearing spokes join the central hub to each hex roller.
        add(box("Cassette_Spoke_"+name,"Titanium_Frame",
                .130*math.cos(angle),.54+.130*math.sin(angle),.665,
                .148,.036,.075,.010,True,angle))
        add(cylinder("Hex_Guide_Roller_"+name,"Copper_Guides",
                     x,y,.663,.915,ROLLER_RADIUS,6,True))
        add(cylinder("Hex_Roller_Ceramic_Face_"+name,"Ceramic_Ivory",
                     x,y,.911,.936,.046,6,True))
        add(cylinder("Hex_Roller_Cyan_Core_"+name,"Signal_Cyan",
                     x,y,.933,.945,.026,12,True))
        add(tube("Hex_Roller_Rear_Bearing_"+name,"Gasket_Dark",
                 x,y,.638,.669,.023,.045,12,True))
        add(box("Roller_Indicator_"+name,"Warning_Amber",
                x,y,.943,.027,.014,.008,.003,True,angle))
    # Visible stowed net: luminous flexible-looking filaments run between
    # the six guide rollers in two circumferential layers and a staggered
    # diagonal layer. These are visual strands, not gameplay projectiles.
    def strand(label,i,j,z):
        a=math.tau*i/ROLLER_COUNT;b=math.tau*j/ROLLER_COUNT
        x1=ROTOR_RADIUS*math.cos(a);y1=.54+ROTOR_RADIUS*math.sin(a)
        x2=ROTOR_RADIUS*math.cos(b);y2=.54+ROTOR_RADIUS*math.sin(b)
        dx=x2-x1;dy=y2-y1
        add(box("Net_Filament_"+label,"Signal_Cyan",
                (x1+x2)/2,(y1+y2)/2,z,math.hypot(dx,dy),.010,.013,
                .003,True,math.atan2(dy,dx)))
    for i in range(ROLLER_COUNT):
        strand(f"Outer_A_{i+1:02d}",i,(i+1)%ROLLER_COUNT,.815)
        strand(f"Outer_B_{i+1:02d}",i,(i+1)%ROLLER_COUNT,.865)
        strand(f"Diagonal_{i+1:02d}",i,(i+2)%ROLLER_COUNT,.842)
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
    by={p["name"]:p for p in parts}
    assert bounds(by["Mount_Top_Socket"]["vertices"])[1][0]>=0
    moving=[p for p in parts if p["moving"]]
    fixed=[p for p in parts if not p["moving"]]
    assert len(moving)==58,(len(moving),"moving group count")
    # AABB covers hollow bores and diagonal spokes, so the fixed axle is
    # checked with an exact projected triangle-to-axis distance instead.
    # Rotation about the same Z axis preserves this radial distance.
    def segment_distance_sq(a,b):
        dx=b[0]-a[0];dy=b[1]-a[1]
        denom=dx*dx+dy*dy
        t=max(0.,min(1.,-(a[0]*dx+a[1]*dy)/denom)) if denom>1e-16 else 0.
        x=a[0]+t*dx;y=a[1]+t*dy
        return x*x+y*y
    def tri_min_radius_sq(a,b,c):
        # Origin inside a nondegenerate 2D triangle -> distance zero.
        ab=(b[0]-a[0],b[1]-a[1]);ac=(c[0]-a[0],c[1]-a[1])
        det=ab[0]*ac[1]-ab[1]*ac[0]
        if abs(det)>1e-12:
            q=(-a[0],-a[1]);u=(q[0]*ac[1]-q[1]*ac[0])/det
            v=(ab[0]*q[1]-ab[1]*q[0])/det
            if u>=-1e-10 and v>=-1e-10 and u+v<=1+1e-10:
                return 0.
        return min(segment_distance_sq(a,b),segment_distance_sq(b,c),
                   segment_distance_sq(c,a))
    min_radius=float("inf")
    for m in moving:
        xy=[(v[0],v[1]-.54) for v in m["vertices"]]
        for a,b,c in m["faces"]:
            min_radius=min(min_radius,math.sqrt(tri_min_radius_sq(
                xy[a],xy[b],xy[c])))
    assert min_radius-.040>.012,("axle-to-rotor clearance",min_radius)
    # Chassis/guards are guaranteed outside cassette's full circular swept
    # envelope. Fixed axle is a deliberate concentric bearing interface.
    assert .34-.025-(ROTOR_RADIUS+ROLLER_RADIUS)>=.068
    assert .835-.030-(.54+ROTOR_RADIUS+ROLLER_RADIUS)>=.0179
    assert (.54-ROTOR_RADIUS-ROLLER_RADIUS)-(.245+.030)>=.0179
    # All moving vertices are actually transformed for every sampled frame.
    comparisons=0;positive_gaps=[];checked_frames=73
    for frame in range(checked_frames):
        theta=math.tau*frame/(checked_frames-1)
        for m in moving:
            transformed=[rotate_xy(v,theta,(0,.54)) for v in m["vertices"]]
            mb=bounds(transformed)
            for f in fixed:
                if f["name"]=="Fixed_Axle":
                    continue  # radial mesh-to-axis check above, not AABB
                fb=bounds(f["vertices"])
                gaps=[max(mb[k][0]-fb[k][1],fb[k][0]-mb[k][1]) for k in range(3)]
                comparisons+=1
                assert max(gaps)>1e-7,("collision",frame,m["name"],f["name"],gaps)
                positive_gaps.append(max(gaps))
    # Independent load path checks: mounts touch chassis, chassis meets
    # brackets/spine, fixed shaft reaches the hub through its real bore.
    def overlap(a,b):
        aa=bounds(by[a]["vertices"]);bb=bounds(by[b]["vertices"])
        return [min(aa[k][1],bb[k][1])-max(aa[k][0],bb[k][0]) for k in range(3)]
    for a,b in (("Mount_Riser","Mount_Top_Socket"),
                ("Main_Chassis","Mount_Riser"),
                ("Bearing_Spine","Main_Chassis"),
                ("Fixed_Axle","Bearing_Spine"),
                ("Outer_Guard_L","Support_Bracket_L"),
                ("Outer_Guard_R","Support_Bracket_R")):
        assert min(overlap(a,b))>.005,(a,b,overlap(a,b))
    return {"sampled_animation_frames":checked_frames,
            "aabb_pair_checks":comparisons,"moving_parts":len(moving),
            "moving_vs_fixed_collisions":0,
            "minimum_separation_aabb_m":round(min(positive_gaps),6),
            "fixed_axle_radius_m":.040,
            "minimum_axle_to_moving_mesh_radial_clearance_m":round(min_radius-.040,6),
            "rotation_degrees":360,
            "clearance":"PASS (sampled AABB plus triangle-to-axle radial distance)"}

def export_obj(parts,out):
    obj=out/(STEM+".obj");mtl=out/(STEM+".mtl")
    lines=["# M01-013: Unity Y up, +Z forward; mount (0,0,0)","mtllib "+mtl.name]
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
    from mathutils import Vector
    # A deterministic, isolated scene: rerunning does not accumulate meshes.
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for mesh in list(bpy.data.meshes):
        if mesh.users==0:bpy.data.meshes.remove(mesh)
    for collection_old in list(bpy.data.collections):
        if collection_old.name.startswith("M01_013_NET_LAUNCHER"):
            bpy.data.collections.remove(collection_old)
    for mat in list(bpy.data.materials):
        if mat.name.startswith("M01_013_") and mat.users==0:
            bpy.data.materials.remove(mat)
    scene=bpy.context.scene
    collection=bpy.data.collections.new("M01_013_NET_LAUNCHER")
    scene.collection.children.link(collection)
    def empty(name,loc=(0.,0.,0.),parent=None):
        o=bpy.data.objects.new(name,None)
        collection.objects.link(o)
        o.location=loc
        if parent:o.parent=parent
        o.empty_display_size=.07
        return o
    mount=empty("MOUNT_TOP_SOCKET")
    rotor=empty("CASSETTE_ROT_Z",(0.,.54,0.),mount)
    materials={}
    for name,(rgba,metal,rough,emission,strength) in MATS.items():
        mat=bpy.data.materials.new("M01_013_"+name)
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
        # The cassette is authored in global coordinates for OBJ; convert
        # vertices to rotor-local space for Blender parent transforms.
        vs=[(x,y-.54,z) if p["moving"] else (x,y,z)
            for x,y,z in p["vertices"]]
        mesh=bpy.data.meshes.new("M01_013_"+p["name"])
        mesh.from_pydata(vs,[],p["faces"])
        mesh.update(calc_edges=True)
        obj=bpy.data.objects.new("M01_013_"+p["name"],mesh)
        collection.objects.link(obj)
        obj.parent=rotor if p["moving"] else mount
        mesh.materials.append(materials[p["material"]])
        for face in mesh.polygons:face.use_smooth=False
    scene.frame_start=1;scene.frame_end=FRAMES;scene.render.fps=FPS
    # 30-degree keys prevent quaternion interpolation from unwinding the
    # intended full 360-degree turn when exported as glTF/FBX.
    for step in range(13):
        frame=1+6*step
        rotor.rotation_euler.z=math.tau*step/12
        rotor.keyframe_insert(data_path="rotation_euler",index=2,frame=frame)
    action=rotor.animation_data.action
    if action:
        try:
            for curve in action.fcurves:
                for key in curve.keyframe_points:key.interpolation="LINEAR"
        except AttributeError:
            pass # Blender 4.4+ layered Actions; sampling remains 30deg/6fr.
    scene.frame_set(1)
    bpy.ops.object.select_all(action="DESELECT")
    for obj in collection.objects:obj.select_set(True)
    bpy.context.view_layer.objects.active=mount
    fbx=out/(STEM+".fbx");glb=out/(STEM+".glb")
    blend=out/(STEM+".blend")
    bpy.ops.export_scene.fbx(filepath=str(fbx),use_selection=True,
        axis_forward="-Z",axis_up="Y",add_leaf_bones=False,
        bake_anim=True,bake_anim_use_all_actions=False)
    bpy.ops.export_scene.gltf(filepath=str(glb),export_format="GLB",
        use_selection=True,export_animations=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(blend))
    return {"blender_export":"PASS (files written, visual inspection pending)",
            "blender_version":".".join(str(x) for x in bpy.app.version),
            "animated_files":[fbx.name,glb.name,blend.name],
            "rotor_node":"CASSETTE_ROT_Z","keyframes":13}

def main():
    argv=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else sys.argv[1:]
    parser=argparse.ArgumentParser()
    parser.add_argument("--out",default="build/M01-013")
    parser.add_argument("--self-test",action="store_true")
    parser.add_argument("--repeat-blender",action="store_true",
                        help="Export twice in one Blender session to check idempotence")
    args=parser.parse_args(argv)
    parts=build()
    report={"asset_id":ASSET_ID,"model":"Wyrzutnia siatki zakłócającej",
            "mount":"MOUNT_TOP_SOCKET (0,0,0), Y-up, +Z-forward",
            "motion":"CASSETTE_ROT_Z rotates 360 degrees about local Z at (0,0.54,0)",
            **check_topology(parts),**check_clearance(parts),
            "blender_export":"NOT RUN","unity_import":"NOT RUN",
            "unity_editmode":"NOT RUN","unity_playmode":"NOT RUN",
            "visual_inspection":"NOT RUN"}
    if args.self_test:
        print(json.dumps(report,indent=2,ensure_ascii=False))
        return
    out=Path(args.out).resolve();out.mkdir(parents=True,exist_ok=True)
    report["static_files"]=export_obj(parts,out)
    try:import bpy
    except ImportError:report["backend"]="plain-python-static-obj"
    else:
        report["backend"]="blender-bpy"
        report.update(blender_export(parts,out))
        if args.repeat_blender:
            first=(len(bpy.data.objects),len(bpy.data.materials),
                   len([c for c in bpy.data.collections
                        if c.name.startswith("M01_013_NET_LAUNCHER")]))
            report.update(blender_export(parts,out))
            second=(len(bpy.data.objects),len(bpy.data.materials),
                    len([c for c in bpy.data.collections
                         if c.name.startswith("M01_013_NET_LAUNCHER")]))
            assert first==second,("Blender rerun leaked objects/materials",first,second)
            report["blender_repeatability"]="PASS (two builds in one process)"
            report["blender_scene_counts"]=list(second)
    (out/(STEM+"_qa.json")).write_text(json.dumps(report,indent=2,ensure_ascii=False)+"\n",encoding="utf-8")
    print(json.dumps(report,indent=2,ensure_ascii=False))

if __name__=="__main__":main()
