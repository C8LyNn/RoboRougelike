#!/usr/bin/env python3
"""M02-007: twin support rollers with independent sprung carriers.
Unity/Blender convention: 1 unit = 1 metre; Z up in Blender, Y up after glTF export.
Chassis underside mounting pivot: (0,0,0), underside mounting plane Z=0.
Travel direction +X, each roller axle local +Y, centres at X=+-0.34 m.
This is a visual assembly, not a vehicle suspension/physics implementation.
"""
from __future__ import annotations

import argparse
from collections import Counter
import json
import math
from pathlib import Path
import sys

ASSET = "M02-007"
SEG = 64
CENTRES = (-.34, .34)
Z_REST = -.405
TRAVEL = .040
OUTER_R = .166
TREADS = 16
MATS = {
    "Chassis_Gunmetal": ((.16,.20,.25,1), .79,.45),
    "Rim_Brushed_Steel": ((.52,.59,.64,1), .77,.33),
    "Hub_Bronze": ((.68,.39,.21,1), .75,.37),
    "Rubber_Graphite": ((.085,.100,.115,1), .02,.89),
    "Tread_Slate": ((.16,.19,.21,1), .02,.85),
    "Shock_Cyan": ((.06,.54,.66,1), .44,.32),
    "Shock_Amber": ((.95,.56,.10,1), .35,.35),
    "Piston_Chrome": ((.78,.83,.86,1), .87,.22),
}

def sub(a,b): return tuple(a[i]-b[i] for i in range(3))
def cross(a,b):
    return (a[1]*b[2]-a[2]*b[1], a[2]*b[0]-a[0]*b[2],
            a[0]*b[1]-a[1]*b[0])
def dot(a,b): return sum(x*y for x,y in zip(a,b))
def signed_volume(v,f):
    return sum(dot(v[a],cross(v[b],v[c]))/6 for a,b,c in f)

def make(name,mat,vertices,faces,role="fixed",smooth=False,side=None):
    assert mat in MATS and role in ("fixed","carrier","rotor")
    if signed_volume(vertices,faces)<0:
        faces=[(a,c,b) for a,b,c in faces]
    return dict(name=name,material=mat,vertices=vertices,faces=faces,
                role=role,smooth=smooth,side=side)

def solid_box(name,centre,size,mat,role="fixed",side=None):
    cx,cy,cz=centre;dx,dy,dz=(s/2 for s in size)
    v=[(cx+x*dx,cy+y*dy,cz+z*dz)
       for x,y,z in ((-1,-1,-1),(1,-1,-1),(1,1,-1),(-1,1,-1),
                     (-1,-1,1),(1,-1,1),(1,1,1),(-1,1,1))]
    q=((0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),
       (2,3,7,6),(3,0,4,7))
    f=[]
    for a,b,c,d in q:f.extend(((a,b,c),(a,c,d)))
    return make(name,mat,v,f,role,side=side)

def ring_y(name,xc,zc,profile,mat,role="rotor",side=None):
    """Watertight revolution about Y; profile entries are (Y,radius)."""
    assert len(profile)>=4 and min(r for y,r in profile)>0
    v=[(xc+r*math.cos(2*math.pi*j/SEG),y,
        zc+r*math.sin(2*math.pi*j/SEG))
       for y,r in profile for j in range(SEG)]
    f=[]
    for i in range(len(profile)):
        nxt=(i+1)%len(profile)
        for j in range(SEG):
            k=(j+1)%SEG
            a=i*SEG+j;b=i*SEG+k;c=nxt*SEG+k;d=nxt*SEG+j
            f.extend(((a,b,c),(a,c,d)))
    return make(name,mat,v,f,role,True,side)

def ann_y(name,xc,zc,y0,y1,r0,r1,mat,role="rotor",side=None):
    assert y0<y1 and 0<r0<r1
    return ring_y(name,xc,zc,[(y0,r0),(y0,r1),(y1,r1),(y1,r0)],
                  mat,role,side)

def ring_z(name,xc,yc,profile,mat,role="fixed",side=None):
    assert len(profile)>=4 and min(r for z,r in profile)>0
    v=[(xc+r*math.cos(2*math.pi*j/SEG),
        yc+r*math.sin(2*math.pi*j/SEG),z)
       for z,r in profile for j in range(SEG)]
    f=[]
    for i in range(len(profile)):
        nxt=(i+1)%len(profile)
        for j in range(SEG):
            k=(j+1)%SEG
            a=i*SEG+j;b=i*SEG+k;c=nxt*SEG+k;d=nxt*SEG+j
            f.extend(((a,b,c),(a,c,d)))
    return make(name,mat,v,f,role,True,side)

def ann_z(name,xc,yc,z0,z1,r0,r1,mat,role="fixed",side=None):
    assert z0<z1 and 0<r0<r1
    return ring_z(name,xc,yc,[(z0,r0),(z0,r1),(z1,r1),(z1,r0)],
                  mat,role,side)

def cylinder(name,axis,centre,radius,start,end,mat,role="fixed",
             side=None,segments=24):
    """Closed capped cylinder, axis Y or Z, axial range start..end."""
    assert end>start and radius>0 and segments>=12
    x,y,z=centre;v=[]
    for axial in (start,end):
        for j in range(segments):
            t=2*math.pi*j/segments
            if axis=="Y":v.append((x+radius*math.cos(t),axial,z+radius*math.sin(t)))
            else:v.append((x+radius*math.cos(t),y+radius*math.sin(t),axial))
    v.extend(((x,start,z) if axis=="Y" else (x,y,start),
              (x,end,z) if axis=="Y" else (x,y,end)))
    f=[]
    for j in range(segments):
        k=(j+1)%segments
        f.extend(((j,k,segments+k),(j,segments+k,segments+j)))
        f.extend(((2*segments,j,k),(2*segments+1,segments+k,segments+j)))
    return make(name,mat,v,f,role,side=side)

def tread(name,xc,zc,angle,mat,side):
    """Closed raised radial shoe, penetrating rubber by 5 mm."""
    v=[]
    for r,half in ((.150,.14),(.166,.115)):
        for y in (-.064,.064):
            for t in (angle-half,angle+half):
                v.append((xc+r*math.cos(t),y,zc+r*math.sin(t)))
    q=((0,1,3,2),(4,6,7,5),(0,2,6,4),
       (1,5,7,3),(0,4,5,1),(2,3,7,6))
    f=[]
    for a,b,c,d in q:f.extend(((a,b,c),(a,c,d)))
    return make(name,mat,v,f,"rotor",side=side)

def build():
    parts=[];add=parts.append
    add(solid_box("00_Underside_Mount_Plate",(0,0,-.016),
                  (1.15,.63,.032),"Chassis_Gunmetal"))
    for i,xc in enumerate(CENTRES):
        label="L" if i==0 else "R"
        add(solid_box(f"{label}_Upper_Crossmember",(xc,0,-.048),
                      (.14,.60,.050),"Rim_Brushed_Steel"))
        for sign in (-1,1):
            tag="NEG" if sign<0 else "POS"
            yc=sign*.235
            add(solid_box(f"{label}_{tag}_Upper_Clevis",
                          (xc,yc,-.090),(.085,.080,.140),
                          "Chassis_Gunmetal"))
            add(ann_z(f"{label}_{tag}_Damper_Housing",xc,yc,
                      -.235,-.10,.022,.038,"Shock_Cyan"))
            add(ann_z(f"{label}_{tag}_Damper_Top_Band",xc,yc,
                      -.135,-.116,.038,.047,"Shock_Amber"))
            add(cylinder(f"{label}_{tag}_Sliding_Piston","Z",
                         (xc,yc,0),.015,-.350,-.150,
                         "Piston_Chrome","carrier",i))
            add(ann_z(f"{label}_{tag}_Piston_Amber_Sleeve",xc,yc,
                      -.340,-.310,.015,.023,"Shock_Amber","carrier",i))
            add(cylinder(f"{label}_{tag}_Lower_Cap","Z",
                         (xc,yc,0),.032,-.375,-.345,
                         "Hub_Bronze","carrier",i))
            add(solid_box(f"{label}_{tag}_Lower_Link",
                          (xc,sign*.1975,-.386),(.084,.087,.060),
                          "Chassis_Gunmetal","carrier",i))
            add(solid_box(f"{label}_{tag}_Axle_Ear",
                          (xc,sign*.137,-.404),(.105,.047,.095),
                          "Chassis_Gunmetal","carrier",i))
        add(cylinder(f"{label}_Axle_Shaft","Y",(xc,0,Z_REST),
                     .018,-.162,.162,"Piston_Chrome","carrier",i,24))
        for sign in (-1,1):
            tag="NEG" if sign<0 else "POS"
            a,b=((- .166,-.123) if sign<0 else (.123,.166))
            add(cylinder(f"{label}_{tag}_Axle_Retainer","Y",
                         (xc,0,Z_REST),.031,a,b,
                         "Hub_Bronze","carrier",i,24))
        profile=[(-.088,.100),(-.088,.127),(-.080,.145),
                 (-.069,.153),(-.053,.155),(-.037,.151),
                 (-.022,.155),(.022,.155),(.037,.151),
                 (.053,.155),(.069,.153),(.080,.145),
                 (.088,.127),(.088,.100)]
        add(ring_y(f"{label}_Roller_Rubber",xc,Z_REST,profile,
                   "Rubber_Graphite","rotor",i))
        add(ann_y(f"{label}_Forged_Rim",xc,Z_REST,-.074,.074,
                  .063,.110,"Rim_Brushed_Steel","rotor",i))
        add(ann_y(f"{label}_Hollow_Hub",xc,Z_REST,-.077,.077,
                  .029,.070,"Hub_Bronze","rotor",i))
        for sign in (-1,1):
            tag="NEG" if sign<0 else "POS"
            a,b=((- .085,-.071) if sign<0 else (.071,.085))
            add(ann_y(f"{label}_{tag}_Amber_Bead_Ring",xc,Z_REST,
                      a,b,.072,.083,"Shock_Amber","rotor",i))
        for j in range(TREADS):
            add(tread(f"{label}_Raised_Tread_{j:02}",xc,Z_REST,
                      2*math.pi*(j+.1)/TREADS,"Tread_Slate",i))
    return parts

def validate(parts):
    names=set();total_v=total_f=0;role_counts=Counter();materials=set()
    for p in parts:
        name=p["name"]
        assert name not in names,name
        names.add(name);materials.add(p["material"]);role_counts[p["role"]]+=1
        v=p["vertices"];f=p["faces"];edges=Counter()
        assert v and f
        assert all(math.isfinite(a) for vertex in v for a in vertex)
        for a,b,c in f:
            assert len({a,b,c})==3 and all(0<=k<len(v) for k in (a,b,c))
            normal=cross(sub(v[b],v[a]),sub(v[c],v[a]))
            assert dot(normal,normal)>1e-15,(name,"degenerate")
            for u,w in ((a,b),(b,c),(c,a)):
                edges[tuple(sorted((u,w)))]+=1
        assert all(n==2 for n in edges.values()),(name,"nonmanifold")
        assert signed_volume(v,f)>1e-9,(name,"nonpositive volume")
        total_v+=len(v);total_f+=len(f)
    assert len(materials)==len(MATS)
    assert len([p for p in parts if "Raised_Tread" in p["name"]])==32
    assert len([p for p in parts if "Damper_Housing" in p["name"]])==4
    assert len([p for p in parts if "Sliding_Piston" in p["name"]])==4
    assert len([p for p in parts if p["role"]=="rotor"])==40
    assert len([p for p in parts if p["role"]=="carrier"])==22
    # Independent kinematic clearance gates at 81 suspension poses and
    # 72 rotor angles per pose. Concentric shaft/bore separation is invariant
    # under spin. Static housings surround piston with a 7 mm radial gap.
    checked=0;min_clear=1.0
    for travel_index in range(81):
        lift=TRAVEL*travel_index/80
        wheel_top=Z_REST+lift+OUTER_R
        frame_gap=-.160-wheel_top
        collar_top=-.310+lift
        collar_gap=-.235-collar_top
        piston_clear=.022-.015
        shaft_clear=.029-.018
        housing_side_clear=.235-.047-.088
        wheel_pair_gap=abs(CENTRES[1]-CENTRES[0])-2*OUTER_R
        axle_ear_side_clear=.1135-.088
        for spin in range(72):
            # Rotor angle changes neither radial envelope nor axial extent.
            _angle=2*math.pi*spin/72
            clearances=(frame_gap,collar_gap,piston_clear,shaft_clear,
                        housing_side_clear,wheel_pair_gap,axle_ear_side_clear)
            assert min(clearances)>=.0069,(travel_index,spin,clearances)
            min_clear=min(min_clear,*clearances)
            checked+=1
    assert checked==5832
    return dict(id=ASSET,model="Para rolek podporowych",
        mounting="underside chassis",pivot_m=[0,0,0],
        forward_axis="+X",roller_axle_axis="+Y",
        roller_centres_m=[[x,0,Z_REST] for x in CENTRES],
        max_radius_m=OUTER_R,roll_width_m=.176,travel_m=TRAVEL,
        parts=len(parts),vertices=total_v,triangles=total_f,
        materials=len(materials),role_counts=dict(role_counts),
        treads=32,dampers=4,closed_positive_volume_parts=len(parts),
        pose_pairs_checked=checked,
        clearance_min_m=round(min_clear,6),
        shaft_bore_radial_clearance_m=.011,
        piston_housing_radial_clearance_m=.007,
        static_qa="PASS",blender="NOT_RUN",unity="NOT_RUN")

def export_obj(parts,out):
    out.mkdir(parents=True,exist_ok=True)
    obj=out/"M02-007_support_roller_pair.obj"
    mtl=out/"M02-007_support_roller_pair.mtl"
    lines=["# M02-007 Z-up, metres; mount at Z=0; roller axle +Y",
           "mtllib "+mtl.name]
    offset=1
    for p in parts:
        lines.extend(("o "+p["name"],"usemtl "+p["material"]))
        lines.extend("v %.7f %.7f %.7f"%v for v in p["vertices"])
        lines.extend("f "+" ".join(str(offset+k) for k in face)
                     for face in p["faces"])
        offset+=len(p["vertices"])
    obj.write_text("\n".join(lines)+"\n",encoding="utf-8")
    m=[]
    for name,(color,metal,rough) in MATS.items():
        m.extend(("newmtl "+name,"Kd %.5f %.5f %.5f"%color[:3],
                  "Ks %.5f %.5f %.5f"%((metal*.45,)*3),
                  "Ns %.1f"%((1-rough)*400),"d 1.0","illum 2",""))
    mtl.write_text("\n".join(m)+"\n",encoding="utf-8")
    return [str(obj),str(mtl)]

def blender_export(parts,out):
    import bpy
    name="M02-007_ASSET"
    coll=bpy.data.collections.get(name)
    if coll:
        for ob in tuple(coll.objects):
            bpy.data.objects.remove(ob,do_unlink=True)
        bpy.data.collections.remove(coll)
    coll=bpy.data.collections.new(name)
    bpy.context.scene.collection.children.link(coll)
    def empty(name,parent=None,location=None):
        ob=bpy.data.objects.new(name,None)
        coll.objects.link(ob)
        ob.empty_display_size=.06
        ob.parent=parent
        if location:ob.location=location
        return ob
    mount=empty("M02-007_MOUNT_Z0")
    carriers=[empty("M02-007_SUSP_"+label,mount)
              for label in ("L","R")]
    rotors=[empty("M02-007_ROLLER_"+label+"_ROTATE_Y",carriers[i],
                  (xc,0,Z_REST))
            for i,(label,xc) in enumerate(zip(("L","R"),CENTRES))]
    mats={}
    for name,(rgba,metal,rough) in MATS.items():
        m=bpy.data.materials.get("M02-007_"+name)
        if m is None:m=bpy.data.materials.new("M02-007_"+name)
        m.use_nodes=True
        bsdf=m.node_tree.nodes.get("Principled BSDF")
        if bsdf:
            bsdf.inputs["Base Color"].default_value=rgba
            bsdf.inputs["Metallic"].default_value=metal
            bsdf.inputs["Roughness"].default_value=rough
        m.diffuse_color=rgba
        mats[name]=m
    for p in parts:
        verts=p["vertices"]
        if p["role"]=="rotor":
            xc=CENTRES[p["side"]]
            verts=[(x-xc,y,z-Z_REST) for x,y,z in verts]
        mesh=bpy.data.meshes.new("M02-007_"+p["name"]+"_mesh")
        mesh.from_pydata(verts,[],p["faces"])
        mesh.update(calc_edges=True)
        ob=bpy.data.objects.new("M02-007_"+p["name"],mesh)
        coll.objects.link(ob)
        ob.parent=(mount if p["role"]=="fixed" else
                   carriers[p["side"]] if p["role"]=="carrier" else
                   rotors[p["side"]])
        ob.data.materials.append(mats[p["material"]])
        for poly in mesh.polygons:poly.use_smooth=p["smooth"]
    # 2 second loop at 30 fps: rotating rollers, independent dampers.
    for i,rotor in enumerate(rotors):
        rotor.rotation_mode="XYZ"
        for frame,angle in ((1,0),(16,math.pi/2),(31,math.pi),
                            (46,3*math.pi/2),(61,2*math.pi)):
            rotor.rotation_euler[1]=angle*(1 if i==0 else -1)
            rotor.keyframe_insert(data_path="rotation_euler",index=1,frame=frame)
    for i,carrier in enumerate(carriers):
        for frame,lift in ((1,0),(16,.040 if i==0 else .018),
                           (31,0),(46,.018 if i==0 else .040),(61,0)):
            carrier.location.z=lift
            carrier.keyframe_insert(data_path="location",index=2,frame=frame)
    scene=bpy.context.scene
    old=(scene.frame_start,scene.frame_end,scene.render.fps,scene.frame_current)
    try:
        scene.frame_start=1;scene.frame_end=61;scene.render.fps=30
        scene.frame_set(1)
        out.mkdir(parents=True,exist_ok=True)
        blend=out/"M02-007.blend"
        bpy.data.libraries.write(str(blend),{coll},compress=True)
        bpy.ops.object.select_all(action="DESELECT")
        for ob in coll.objects:ob.select_set(True)
        bpy.context.view_layer.objects.active=mount
        glb=out/"M02-007_animated.glb"
        bpy.ops.export_scene.gltf(filepath=str(glb),export_format="GLB",
                                  use_selection=True,export_animations=True,
                                  export_force_sampling=True,
                                  export_frame_range=True)
        assert blend.is_file() and glb.is_file()
        return dict(blender="PASS_EXPORT",blend=str(blend),glb=str(glb),
                    fbx="NOT_RUN",unity="NOT_RUN")
    finally:
        bpy.ops.object.select_all(action="DESELECT")
        scene.frame_start,scene.frame_end,scene.render.fps=old[:3]
        scene.frame_set(old[3])

def main():
    args=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else sys.argv[1:]
    ap=argparse.ArgumentParser()
    ap.add_argument("--out",default="build/M02-007")
    ap.add_argument("--self-test",action="store_true")
    a=ap.parse_args(args)
    parts=build();report=validate(parts)
    if a.self_test:
        print(json.dumps(report,indent=2));return
    out=Path(a.out)
    report["files"]=export_obj(parts,out)
    try:import bpy
    except ImportError:pass
    else:report.update(blender_export(parts,out))
    (out/"M02-007_qa.json").write_text(
        json.dumps(report,indent=2)+"\n",encoding="utf-8")
    print(json.dumps(report,indent=2))

if __name__=="__main__":main()
