#!/usr/bin/env python3
"""M01-007 Impulse Ring Cannon: visual-only Blender/Unity 6 asset.

1 unit = 1 metre; top mount pivot (0,0,0); +Y forward, +Z up.
Three independent glowing rings slide +/- 0.040 m on the barrel's Y axis.
No gameplay, project settings, scenes, or existing WPN code are touched.
"""
from __future__ import annotations
import argparse
from collections import Counter
import json
import math
import sys
from pathlib import Path

ASSET_ID = "M01-007"
NAME = "M01-007_impulse_ring_cannon"
PREFIX = "M01_007_"
RING_Y = (.68, .98, 1.28)
RING_STROKE = .040
BARREL_Z = .440
SEGMENTS = 48
MATERIALS = {
    "Graphite_Armor": ((.095,.113,.139,1), .47,.48,0),
    "Titanium": ((.42,.49,.55,1), .84,.31,0),
    "Copper": ((.72,.37,.19,1), .78,.30,0),
    "Ceramic_Ivory": ((.82,.84,.79,1), .10,.35,0),
    "Cyan_Pulse": ((.06,.81,1.0,1), .22,.22,3.5),
    "Amber_Status": ((1.0,.52,.12,1), .28,.34,1.2),
    "Black_Bore": ((.025,.035,.050,1), .11,.80,0),
}

def sub(a,b): return tuple(x-y for x,y in zip(a,b))
def dot(a,b): return sum(x*y for x,y in zip(a,b))
def cross(a,b):
    return (a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0])
def signed_volume(vs,faces):
    return sum(dot(vs[a],cross(vs[b],vs[c]))/6 for a,b,c in faces)

def convex(name,vertices,polygons,material,group="STATIC",smooth=False):
    """Triangulate closed convex solids and correct outward winding."""
    centroid=tuple(sum(v[i] for v in vertices)/len(vertices) for i in range(3))
    faces=[]
    for poly in polygons:
        a,b,c=(vertices[k] for k in poly[:3])
        n=cross(sub(b,a),sub(c,a))
        centre=tuple(sum(vertices[k][i] for k in poly)/len(poly) for i in range(3))
        if dot(n,sub(centre,centroid))<0: poly=tuple(reversed(poly))
        for j in range(1,len(poly)-1):
            faces.append((poly[0],poly[j],poly[j+1]))
    return dict(name=name,vertices=vertices,faces=faces,material=material,group=group,smooth=smooth,coaxial=False)

def box(name,center,size,material,group="STATIC"):
    x,y,z=center;hx,hy,hz=(s/2 for s in size)
    v=[(x+a,y+b,z+c) for a,b,c in
       [(-hx,-hy,-hz),(hx,-hy,-hz),(hx,hy,-hz),(-hx,hy,-hz),
        (-hx,-hy,hz),(hx,-hy,hz),(hx,hy,hz),(-hx,hy,hz)]]
    return convex(name,v,[(0,3,2,1),(4,5,6,7),(0,1,5,4),
                         (1,2,6,5),(2,3,7,6),(3,0,4,7)],material,group)

def cylinder_z(name,center,radius,height,material,segments=24):
    x,y,z=center
    v=[(x+radius*math.cos(2*math.pi*j/segments),
        y+radius*math.sin(2*math.pi*j/segments),
        z+height*sign/2) for sign in (-1,1) for j in range(segments)]
    v.extend(((x,y,z-height/2),(x,y,z+height/2)))
    q=[]
    for j in range(segments):
        k=(j+1)%segments
        q.extend(((j,k,segments+k,segments+j),(2*segments,k,j),
                  (2*segments+1,segments+j,segments+k)))
    return convex(name,v,q,material,smooth=True)

def prism_y(name,outline,y0,y1,material):
    """Faceted 2D armor plate extruded along the forward axis."""
    n=len(outline)
    v=[(x,y,z) for y in (y0,y1) for x,z in outline]
    polys=[tuple(range(n-1,-1,-1)),tuple(range(n,2*n))]
    polys.extend((i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n))
    return convex(name,v,polys,material)

def revolve_y(name,profile,material,group="STATIC",segments=SEGMENTS):
    """Closed annular extrusion/lathe, with a real empty central bore.
    Profile entries are (y, radial_distance) around the shared Y axis.
    """
    assert len(profile)>=4 and min(r for y,r in profile)>0
    v=[(r*math.cos(2*math.pi*j/segments),
        y,BARREL_Z+r*math.sin(2*math.pi*j/segments))
       for y,r in profile for j in range(segments)]
    faces=[]
    for i in range(len(profile)):
        ni=(i+1)%len(profile)
        for j in range(segments):
            nj=(j+1)%segments
            a=i*segments+j;b=i*segments+nj
            c=ni*segments+nj;d=ni*segments+j
            faces.extend(((a,b,c),(a,c,d)))
    if signed_volume(v,faces)<0:
        faces=[(a,c,b) for a,b,c in faces]
    return dict(name=name,vertices=v,faces=faces,material=material,
                group=group,smooth=True,coaxial=True)

def annulus(name,y,inner,outer,width,material,group="STATIC"):
    assert 0<inner<outer and width>0
    return revolve_y(name,[(y-width/2,inner),(y-width/2,outer),
                           (y+width/2,outer),(y+width/2,inner)],
                     material,group)

def build():
    p=[];a=p.append
    # Vehicle mount: the plane z=0 is exactly the installation plane.
    a(cylinder_z("Top_Mount_Plate",(0,0,.026),.170,.052,"Titanium",32))
    a(box("Top_Mount_Key",(0,0,.066),(.240,.250,.040),"Graphite_Armor"))
    for x in (-.113,.113):
        for y in (-.105,.105):
            a(cylinder_z("Mount_Bolt_%s_%s"%("L" if x<0 else "R","F" if y>0 else "B"),
                         (x,y,.058),.016,.012,"Copper",12))
    a(box("Central_Pedestal",(0,-.055,.157),(.240,.350,.154),"Titanium"))
    a(box("Breech_Core",(0,-.075,.348),(.485,.860,.285),"Graphite_Armor"))
    a(box("Rear_Actuator",(0,-.495,.378),(.320,.105,.180),"Copper"))
    a(box("Rear_Heat_Shield",(0,-.554,.403),(.380,.035,.205),"Ceramic_Ivory"))
    a(box("Upper_Spine",(0,-.095,.510),(.285,.690,.045),"Titanium"))
    for side in (-1,1):
        letter="L" if side<0 else "R"
        profile=[(side*.238,.233),(side*.324,.255),(side*.360,.353),
                 (side*.328,.495),(side*.248,.521),(side*.223,.468)]
        a(prism_y("Faceted_Ceramic_Shell_"+letter,profile,-.423,.300,"Ceramic_Ivory"))
        a(box("Copper_Edge_"+letter,(side*.369,-.035,.363),(.014,.595,.032),"Copper"))
        a(box("Amber_Side_Signal_"+letter,(side*.380,-.070,.398),(.013,.270,.013),"Amber_Status"))
        for k,y in enumerate((-.30,-.12,.06,.24)):
            a(box("Vent_%s_%02d"%(letter,k),(side*.372,y,.290),
                  (.014,.065,.018),"Black_Bore"))
        a(box("Front_Arm_"+letter,(side*.193,.310,.443),(.070,.310,.105),"Titanium"))
        a(box("Front_Arm_Inlay_"+letter,(side*.194,.315,.505),(.028,.270,.010),"Copper"))
        for k,y in enumerate((-.27,.08)):
            a(cylinder_z("Side_Rivet_%s_%02d"%(letter,k),
                         (side*.292,y,.528),.015,.018,"Copper",12))
    # Real hollow barrel and recessed muzzle, not a solid cylinder.
    a(annulus("Hollow_Cannon_Barrel",.970,.063,.106,1.140,"Black_Bore"))
    a(annulus("Breech_Barrel_Collar",.417,.104,.153,.085,"Titanium"))
    a(annulus("Muzzle_Brake",1.563,.063,.147,.090,"Titanium"))
    a(annulus("Muzzle_Copper_Lip",1.608,.105,.155,.024,"Copper"))
    a(annulus("Muzzle_Glow_Inner",1.613,.064,.079,.010,"Cyan_Pulse"))
    # The three independent rings float around the barrel with a >= 69 mm
    # radial clearance. Their internal colored layers deliberately interlock.
    for idx,y in enumerate(RING_Y,1):
        group="RING_%02d"%idx
        a(annulus("Ring_%02d_Titanium"%idx,y,.178,.235,.108,"Titanium",group))
        a(annulus("Ring_%02d_Cyan"%idx,y,.232,.248,.070,"Cyan_Pulse",group))
        a(annulus("Ring_%02d_Copper_Edge"%idx,y,.245,.261,.095,"Copper",group))
        # 12 decorative armor teeth per ring; mounted on the outer metal rim.
        for j in range(12):
            t=2*math.pi*j/12
            x=.263*math.cos(t);z=BARREL_Z+.263*math.sin(t)
            # Small, non-interfering faceted studs are part of same moving assembly.
            a(box("Ring_%02d_Rivet_%02d"%(idx,j),(x,y,z),
                  (.022,.032,.022),"Graphite_Armor",group))
    return p

def pose_offset(index,step,samples=49):
    phase=(index-1)*2*math.pi/3
    return RING_STROKE*math.sin(2*math.pi*step/(samples-1)+phase)

def xyz_bounds(p,dy=0):
    v=p["vertices"]
    return [(min(t[i] for t in v)+(dy if i==1 else 0),
             max(t[i] for t in v)+(dy if i==1 else 0)) for i in range(3)]

def radial_bounds(p):
    r=[math.hypot(v[0],v[2]-BARREL_Z) for v in p["vertices"]]
    return min(r),max(r)

def overlaps(a,b):
    return [min(a[i][1],b[i][1])-max(a[i][0],b[i][0]) for i in range(3)]

def validate(parts):
    names=set();vertices=0;triangles=0;volumes=[]
    for p in parts:
        assert p["name"] not in names,p["name"]
        names.add(p["name"])
        assert p["material"] in MATERIALS
        vs=p["vertices"];edges=Counter()
        for v in vs:assert all(math.isfinite(x) for x in v)
        for face in p["faces"]:
            assert len(face)==3 and len(set(face))==3
            assert all(0<=i<len(vs) for i in face)
            a,b,c=(vs[k] for k in face)
            assert dot(cross(sub(b,a),sub(c,a)),cross(sub(b,a),sub(c,a)))>1e-16,p["name"]
            for i,j in zip(face,face[1:]+face[:1]):
                edges[tuple(sorted((i,j)))]+=1
        assert edges and all(v==2 for v in edges.values()),p["name"]
        volume=signed_volume(vs,p["faces"])
        assert volume>1e-9,(p["name"],volume)
        volumes.append(volume)
        vertices+=len(vs);triangles+=len(p["faces"])
    static=[p for p in parts if p["group"]=="STATIC"]
    moving=[p for p in parts if p["group"]!="STATIC"]
    assert len(MATERIALS)>=3 and len(set(p["group"] for p in moving))==3
    assert all(len([p for p in moving if p["group"]==g])==15 for g in ("RING_01","RING_02","RING_03"))
    checked=0;min_axial=100
    for step in range(49):
        offsets={g:pose_offset(i,step) for i,g in enumerate(("RING_01","RING_02","RING_03"),1)}
        for m in moving:
            mb=xyz_bounds(m,offsets[m["group"]])
            for s in static:
                sb=xyz_bounds(s)
                ov=overlaps(mb,sb)
                checked+=1
                if ov[1]<=1e-6:continue
                if s["coaxial"]:
                    a,b=radial_bounds(m),radial_bounds(s)
                    if min(a[1],b[1])-max(a[0],b[0])<=1e-6:continue
                assert not all(x>1e-6 for x in ov),("collision",m["name"],s["name"],step,ov)
        for i in range(3):
            for j in range(i+1,3):
                yi=RING_Y[i]+offsets["RING_%02d"%(i+1)]
                yj=RING_Y[j]+offsets["RING_%02d"%(j+1)]
                axial_gap=abs(yj-yi)-.108
                assert axial_gap>.07,("rings collide",i,j,step,axial_gap)
                min_axial=min(min_axial,axial_gap)
    return dict(id=ASSET_ID,status="PASS_STATIC",parts=len(parts),
        static_parts=len(static),moving_parts=len(moving),vertices=vertices,
        triangles=triangles,materials=len(MATERIALS),unit="meter",
        mount_pivot_m=[0,0,0],forward_axis="+Y",up_axis="+Z",
        animation_axis="+Y",ring_centers_y_m=list(RING_Y),
        ring_translation_amplitude_m=RING_STROKE,
        ring_count=3,animation_pose_samples=49,aabb_or_radial_pair_checks=checked,
        moving_static_collisions=0,min_ring_axial_clearance_m=round(min_axial,6),
        closed_positive_volume_components=len(volumes),
        blender_export="NOT_RUN",unity_import="NOT_RUN",visual_review="NOT_RUN")

def static_export(parts,out):
    out.mkdir(parents=True,exist_ok=True)
    obj=out/(NAME+".obj");mtl=out/(NAME+".mtl")
    lines=["# M01-007, Unity meters, pivot origin, +Y forward, +Z up",
           "mtllib "+mtl.name]
    start=1
    for p in parts:
        lines.extend(("o "+p["name"],"usemtl "+p["material"]))
        lines.extend("v %.7f %.7f %.7f"%v for v in p["vertices"])
        lines.extend("f "+" ".join(str(i+start) for i in f) for f in p["faces"])
        start+=len(p["vertices"])
    obj.write_text("\n".join(lines)+"\n",encoding="utf-8")
    materials=[]
    for name,(rgba,metal,rough,emit) in MATERIALS.items():
        materials.extend(["newmtl "+name,
            "Kd %.5f %.5f %.5f"%rgba[:3],
            "Ks %.5f %.5f %.5f"%((metal*.5,)*3),
            "Ns %.2f"%((1-rough)*400),
            "Ke %.5f %.5f %.5f"%tuple(emit*v for v in rgba[:3]),
            "d 1.0","illum 2",""])
    mtl.write_text("\n".join(materials)+"\n",encoding="utf-8")
    return dict(obj=str(obj),mtl=str(mtl))

def blender_export(parts,out):
    import bpy
    collection_name="M01-007_ASSET"
    owned=bpy.data.collections.get(collection_name)
    if owned:
        for obj in tuple(owned.objects):
            bpy.data.objects.remove(obj,do_unlink=True)
        bpy.data.collections.remove(owned)
    owned=bpy.data.collections.new(collection_name)
    bpy.context.scene.collection.children.link(owned)
    def empty(name,parent=None):
        ob=bpy.data.objects.new(PREFIX+name,None)
        owned.objects.link(ob);ob.parent=parent;ob.empty_display_size=.08
        return ob
    mount=empty("MOUNT_TOP_Z0")
    rotors={g:empty(g+"_SLIDE_Y",mount) for g in ("RING_01","RING_02","RING_03")}
    mats={}
    for name,(rgba,metal,rough,emit) in MATERIALS.items():
        mat=bpy.data.materials.get(PREFIX+name)
        if mat is None:mat=bpy.data.materials.new(PREFIX+name)
        mat.use_nodes=True
        bsdf=mat.node_tree.nodes.get("Principled BSDF")
        if bsdf:
            bsdf.inputs["Base Color"].default_value=rgba
            bsdf.inputs["Metallic"].default_value=metal
            bsdf.inputs["Roughness"].default_value=rough
            if emit:
                bsdf.inputs["Emission Color"].default_value=rgba
                bsdf.inputs["Emission Strength"].default_value=emit
        mat.diffuse_color=rgba;mats[name]=mat
    for p in parts:
        mesh=bpy.data.meshes.new(PREFIX+p["name"]+"_mesh")
        mesh.from_pydata(p["vertices"],[],p["faces"])
        mesh.update(calc_edges=True)
        ob=bpy.data.objects.new(PREFIX+p["name"],mesh)
        owned.objects.link(ob)
        ob.parent=mount if p["group"]=="STATIC" else rotors[p["group"]]
        mesh.materials.append(mats[p["material"]])
        for polygon in mesh.polygons:polygon.use_smooth=p["smooth"]
    for i,g in enumerate(("RING_01","RING_02","RING_03"),1):
        ob=rotors[g]
        for frame in (1,13,25,37,49,61):
            ob.location.y=pose_offset(i,(frame-1)*48/60)
            ob.keyframe_insert(data_path="location",index=1,frame=frame)
        if ob.animation_data and ob.animation_data.action:
            try:
                for fc in ob.animation_data.action.fcurves:
                    for key in fc.keyframe_points:key.interpolation="LINEAR"
            except AttributeError:
                pass # Blender 4.4+ layered action API
    scene=bpy.context.scene
    old=(scene.frame_start,scene.frame_end,scene.frame_current,scene.render.fps)
    selection=list(bpy.context.selected_objects)
    active=bpy.context.view_layer.objects.active
    try:
        scene.frame_start=1;scene.frame_end=61;scene.render.fps=60;scene.frame_set(1)
        bpy.context.view_layer.update()
        assert len(owned.objects)==len(parts)+4
        bpy.data.libraries.write(str(out/(NAME+".blend")),{owned},compress=True)
        bpy.ops.object.select_all(action="DESELECT")
        for ob in owned.objects:ob.select_set(True)
        bpy.context.view_layer.objects.active=mount
        bpy.ops.export_scene.gltf(filepath=str(out/(NAME+"_animated.glb")),
                                  export_format="GLB",use_selection=True,export_animations=True)
        bpy.ops.export_scene.fbx(filepath=str(out/(NAME+"_animated.fbx")),
                                 use_selection=True,axis_forward="-Y",axis_up="Z",
                                 add_leaf_bones=False,bake_anim=True,
                                 bake_anim_use_all_actions=False)
    finally:
        bpy.ops.object.select_all(action="DESELECT")
        for ob in selection:
            if ob.name in bpy.data.objects:ob.select_set(True)
        if active and active.name in bpy.data.objects:bpy.context.view_layer.objects.active=active
        scene.frame_start,scene.frame_end,scene.render.fps=old[0],old[1],old[3]
        scene.frame_set(old[2])
    return dict(blender_export="PASS",
        glb=str(out/(NAME+"_animated.glb")),
        fbx=str(out/(NAME+"_animated.fbx")),
        blend=str(out/(NAME+".blend")))

def main():
    argv=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else sys.argv[1:]
    ap=argparse.ArgumentParser()
    ap.add_argument("--out",default="build/M01-007")
    ap.add_argument("--self-test",action="store_true")
    args=ap.parse_args(argv)
    parts=build()
    report=validate(parts)
    print(json.dumps(report,indent=2))
    if args.self_test:return
    out=Path(args.out).resolve()
    out.mkdir(parents=True,exist_ok=True)
    report.update(static_export(parts,out))
    try:import bpy
    except ImportError:pass
    else:report.update(blender_export(parts,out))
    (out/"M01-007_qa.json").write_text(json.dumps(report,indent=2)+"\n",encoding="utf-8")
    print(json.dumps(report,indent=2))

if __name__=="__main__":main()
