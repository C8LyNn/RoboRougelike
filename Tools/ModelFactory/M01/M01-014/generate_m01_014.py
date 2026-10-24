#!/usr/bin/env python3
"""M01-014: Elektromagnetyczny impulsator — procedural visual asset.

Unity 6: 1 unit = 1 metre; Y up, +Z forward. MOUNT_FRONT_SOCKET
at (0,0,0), front attachment face. Pure geometry builds OBJ/MTL
without Blender; Blender 4+ additionally exports animated FBX/GLB/BLEND.
Visual-only: no projectile, damage, targeting or gameplay logic.
"""
from __future__ import annotations
import argparse
import json
import math
import sys
from pathlib import Path

ASSET_ID = "M01-014"
STEM = "M01-014_electromagnetic_pulser"
CORE_CENTER = (0.0, 0.36, 0.0)
CORE_TRAVEL_M = 0.18
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
    """Front-mounted electromagnetic pulse emitter with a sliding ferrite core.

    The visible barrel is genuinely hollow: ferrite annuli surround, but do
    not intersect, the axially moving central core. All dimensions are metres.
    """
    p=[];add=p.append
    # Load path and mechanical front-mount plate; mounting face lies at Z=0.
    add(box("Mount_Front_Socket","Armor_Graphite",0,.36,.06,.66,.48,.12,.024))
    add(box("Mount_Ceramic_Key_L","Ceramic_Ivory",-.225,.36,.155,.09,.18,.19,.012))
    add(box("Mount_Ceramic_Key_R","Ceramic_Ivory", .225,.36,.155,.09,.18,.19,.012))
    add(box("Mount_Titanium_Bridge","Titanium_Frame",0,.36,.255,.45,.30,.22,.024))
    add(box("Rear_Electronics_Housing","Armor_Graphite",0,.36,.43,.55,.35,.29,.026))
    add(box("Rear_Service_Ceramic","Ceramic_Ivory",0,.565,.43,.34,.035,.22,.008))
    add(box("Rear_Copper_Heat_Sink","Copper_Guides",0,.155,.43,.35,.028,.23,.009))
    # Four massive ferrite yokes, exposed copper induction windings and
    # luminous ceramic separation rings form a distinctive open silhouette.
    for i,z in enumerate((.72,.88,1.04,1.20),1):
        label=f"{i:02d}"
        add(tube("Ferrite_Yoke_"+label,"Armor_Graphite",0,.36,z,z+.095,
                 .152,.282,48))
        add(tube("Copper_Induction_"+label,"Copper_Guides",0,.36,z+.026,z+.066,
                 .284,.316,48))
        add(tube("Ceramic_Yoke_Face_"+label,"Ceramic_Ivory",0,.36,z+.087,z+.102,
                 .154,.257,48))
        add(tube("Cyan_Field_Trace_"+label,"Signal_Cyan",0,.36,z+.067,z+.080,
                 .286,.306,48))
    add(tube("Muzzle_Ring_Titanium","Titanium_Frame",0,.36,1.355,1.425,
             .152,.273,48))
    add(tube("Muzzle_Ring_Ceramic","Ceramic_Ivory",0,.36,1.419,1.448,
             .154,.257,48))
    add(tube("Muzzle_Cyan_Annulus","Signal_Cyan",0,.36,1.448,1.460,
             .159,.231,48))
    # Exterior side rails and their ceramic ribs leave the moving core clear.
    for sign,label in ((-1,"L"),(1,"R")):
        add(box("Load_Rail_"+label,"Titanium_Frame",sign*.375,.36,.980,
                .075,.14,1.010,.014))
        add(box("Side_Armor_"+label,"Ceramic_Ivory",sign*.422,.36,.985,
                .025,.17,.78,.007))
        add(box("Side_Copper_Bus_"+label,"Copper_Guides",sign*.447,.36,1.02,
                .018,.046,.70,.004))
        add(box("Rear_Rail_Brace_"+label,"Titanium_Frame",sign*.315,.36,.52,
                .145,.19,.18,.015))
        add(box("Front_Rail_Brace_"+label,"Titanium_Frame",sign*.338,.36,1.39,
                .12,.13,.12,.012))
        for j,z in enumerate((.72,.91,1.10,1.29),1):
            add(cylinder("Rail_Rivet_"+label+str(j),"Warning_Amber",
                         sign*.441,.36,z-.016,z+.016,.022,12))
    add(box("Dorsal_Spine","Titanium_Frame",0,.704,.975,.20,.050,.90,.012))
    add(box("Dorsal_Ceramic_Insulator","Ceramic_Ivory",0,.746,.96,
            .125,.024,.66,.006))
    add(box("Ventral_Spine","Titanium_Frame",0,.012,.975,.20,.050,.90,.012))
    add(box("Ventral_Gasket","Gasket_Dark",0,-.027,.96,.13,.018,.61,.005))
    # Moving ferrite projectile is a retained, non-firing visual core.
    # Radius <= .10m vs a minimum 0.152m yoke bore; 52mm radial clearance.
    add(cylinder("Ferrite_Core_Shaft","Gasket_Dark",0,.36,.755,1.415,
                 .082,32,True))
    add(cylinder("Core_Titanium_Collar","Titanium_Frame",0,.36,.758,.826,
                 .096,32,True))
    add(cylinder("Core_Front_Ceramic","Ceramic_Ivory",0,.36,1.410,1.485,
                 .095,32,True))
    add(cylinder("Core_Copper_Cap","Copper_Guides",0,.36,1.481,1.510,
                 .090,32,True))
    add(cylinder("Core_Emitter","Signal_Cyan",0,.36,1.505,1.538,
                 .074,32,True))
    add(tube("Core_Emitter_Lip","Warning_Amber",0,.36,1.524,1.542,
             .045,.092,32,True))
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
    """Conservative 61-frame AABB test plus bore radial clearance.

    Intentional coaxial overlap between a hollow fixed annulus and the
    sliding shaft is checked analytically, not by false-positive AABB.
    """
    by={p["name"]:p for p in parts}
    fixed=[p for p in parts if not p["moving"]]
    moving=[p for p in parts if p["moving"]]
    assert len(moving)==6,len(moving)
    coaxial_prefix=("Ferrite_Yoke_","Copper_Induction_",
        "Ceramic_Yoke_Face_","Cyan_Field_Trace_","Muzzle_")
    bore=[p for p in fixed if p["name"].startswith(coaxial_prefix)]
    assert len(bore)==19,len(bore)
    min_inner=.152
    # Exact radial maximum from the moving mesh vertices. For the convex
    # cylindrical sections, radial distance is maximised at a vertex.
    max_moving_radius=max(math.hypot(v[0],v[1]-.36)
        for p in moving for v in p["vertices"])
    radial_gap=min_inner-max_moving_radius
    assert radial_gap>.04,(radial_gap,"insufficient ferrite bore clearance")
    comparisons=0;separations=[]
    for frame in range(FRAMES):
        t=frame/(FRAMES-1)
        # Start seated, extend 180mm at mid-cycle, retract to start.
        displacement=CORE_TRAVEL_M*math.sin(math.pi*t)**2
        for m in moving:
            bb=bounds([(x,y,z+displacement) for x,y,z in m["vertices"]])
            for f in fixed:
                if f in bore:
                    # All annuli share the same axial centre and the
                    # minimum measured bore radius exceeds the core radius.
                    continue
                ff=bounds(f["vertices"])
                gaps=[max(bb[k][0]-ff[k][1],ff[k][0]-bb[k][1])
                      for k in range(3)]
                comparisons+=1
                sep=max(gaps)
                assert sep>1e-7,(frame,m["name"],f["name"],gaps)
                separations.append(sep)
    # Physical load-path connections are intentional, unlike core collisions.
    def overlap(a,b):
        aa=bounds(by[a]["vertices"]);bb=bounds(by[b]["vertices"])
        return [min(aa[k][1],bb[k][1])-max(aa[k][0],bb[k][0]) for k in range(3)]
    for a,b in (("Mount_Front_Socket","Mount_Titanium_Bridge"),
                ("Mount_Titanium_Bridge","Rear_Electronics_Housing"),
                ("Rear_Rail_Brace_L","Rear_Electronics_Housing"),
                ("Rear_Rail_Brace_R","Rear_Electronics_Housing")):
        assert min(overlap(a,b))>.004,(a,b,overlap(a,b))
    return {"sampled_animation_frames":FRAMES,
            "aabb_pair_checks":comparisons,
            "moving_parts":len(moving),
            "moving_vs_fixed_collisions":0,
            "minimum_separation_aabb_m":round(min(separations),6),
            "minimum_radial_bore_clearance_m":round(radial_gap,6),
            "core_travel_m":CORE_TRAVEL_M,
            "clearance":"PASS (sampled AABB + analytic coaxial bore clearance)"}


def export_obj(parts,out):
    obj=out/(STEM+".obj");mtl=out/(STEM+".mtl")
    lines=["# M01-014: Unity Y up, +Z forward; front mount (0,0,0)","mtllib "+mtl.name]
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
        if collection_old.name.startswith("M01_014_EM_PULSER"):
            bpy.data.collections.remove(collection_old)
    for mat in list(bpy.data.materials):
        if mat.name.startswith("M01_014_") and mat.users==0:
            bpy.data.materials.remove(mat)
    scene=bpy.context.scene
    collection=bpy.data.collections.new("M01_014_EM_PULSER")
    scene.collection.children.link(collection)
    def empty(name,loc=(0.,0.,0.),parent=None):
        o=bpy.data.objects.new(name,None)
        collection.objects.link(o)
        o.location=loc
        if parent:o.parent=parent
        o.empty_display_size=.07
        return o
    mount=empty("MOUNT_FRONT_SOCKET")
    rotor=empty("FERRITE_CORE_SLIDE_Z",(0.,.36,0.),mount)
    materials={}
    for name,(rgba,metal,rough,emission,strength) in MATS.items():
        mat=bpy.data.materials.new("M01_014_"+name)
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
        # The ferrite core is authored in global coordinates for OBJ; convert
        # vertices to sliding-parent local space for Blender transforms.
        vs=[(x,y-.36,z) if p["moving"] else (x,y,z)
            for x,y,z in p["vertices"]]
        mesh=bpy.data.meshes.new("M01_014_"+p["name"])
        mesh.from_pydata(vs,[],p["faces"])
        mesh.update(calc_edges=True)
        obj=bpy.data.objects.new("M01_014_"+p["name"],mesh)
        collection.objects.link(obj)
        obj.parent=rotor if p["moving"] else mount
        mesh.materials.append(materials[p["material"]])
        for face in mesh.polygons:face.use_smooth=False
    scene.frame_start=1;scene.frame_end=FRAMES;scene.render.fps=FPS
    # Axial extension / retraction; no object is moved into its bore walls.
    for frame,shift in ((1,0.0),(16,0.09),(31,0.18),(46,0.09),(61,0.0)):
        rotor.location.z=shift
        rotor.keyframe_insert(data_path="location",index=2,frame=frame)
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
            "rotor_node":"FERRITE_CORE_SLIDE_Z","keyframes":5}

def main():
    argv=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else sys.argv[1:]
    parser=argparse.ArgumentParser()
    parser.add_argument("--out",default="build/M01-014")
    parser.add_argument("--self-test",action="store_true")
    parser.add_argument("--repeat-blender",action="store_true",
                        help="Export twice in one Blender session to check idempotence")
    args=parser.parse_args(argv)
    parts=build()
    report={"asset_id":ASSET_ID,"model":"Elektromagnetyczny impulsator",
            "mount":"MOUNT_FRONT_SOCKET (0,0,0), Y-up, +Z-forward",
            "motion":"FERRITE_CORE_SLIDE_Z translates 0..0.18m along local +Z at (0,0.36,0)",
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
                        if c.name.startswith("M01_014_EM_PULSER")]))
            report.update(blender_export(parts,out))
            second=(len(bpy.data.objects),len(bpy.data.materials),
                    len([c for c in bpy.data.collections
                         if c.name.startswith("M01_014_EM_PULSER")]))
            assert first==second,("Blender rerun leaked objects/materials",first,second)
            report["blender_repeatability"]="PASS (two builds in one process)"
            report["blender_scene_counts"]=list(second)
    (out/(STEM+"_qa.json")).write_text(json.dumps(report,indent=2,ensure_ascii=False)+"\n",encoding="utf-8")
    print(json.dumps(report,indent=2,ensure_ascii=False))

if __name__=="__main__":main()
