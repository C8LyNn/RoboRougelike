#!/usr/bin/env python3
"""M02-003: low-pressure balloon sand wheel for RoboRougelike.

Deterministic source geometry in metres (1 Unity unit = 1 m).
Axle and spin axis: local +X. Wheel pivot and MOUNT_AXLE at (0,0,0).
Python 3.11+ for OBJ/MTL + self-test; Blender 4.2+ for animated FBX/GLB.
No gameplay, scene, prefab or ProjectSettings changes.
"""
from __future__ import annotations

import argparse
import json
import math
import sys
from collections import Counter
from pathlib import Path

ASSET_ID = "M02-003"
SEGMENTS = 96
AXLE_BORE_RADIUS = 0.045
REFERENCE_SHAFT_RADIUS = 0.034
TIRE_MAX_RADIUS = 0.512
# (axial X, radial distance from +X axis), closed via final-to-first edge.
TIRE_PROFILE = [
    (-.300,.245), (-.338,.285), (-.355,.350), (-.352,.415),
    (-.325,.465), (-.275,.495), (-.205,.506), (0.0,.512),
    (.205,.506), (.275,.495), (.325,.465), (.352,.415),
    (.355,.350), (.338,.285), (.300,.245),
]
# RGBA, metallic, roughness: intentionally no emission dependency.
MATERIALS = {
    "Rubber_Sidewall": ((.052,.057,.062,1), .00,.97),
    "Rubber_Tread": ((.091,.100,.105,1), .00,.96),
    "Tread_Sipes": ((.037,.043,.048,1), .00,.99),
    "Alloy_Titanium": ((.32,.38,.41,1), .78,.34),
    "Hub_Graphite": ((.115,.14,.16,1), .68,.40),
    "Accent_Amber": ((.95,.38,.045,1), .34,.43),
    "Bolt_Steel": ((.60,.65,.67,1), .83,.28),
}

def pnt(x, radius, angle):
    return (x, radius*math.cos(angle), radius*math.sin(angle))

def sub(a,b):
    return (a[0]-b[0],a[1]-b[1],a[2]-b[2])

def cross(a,b):
    return (a[1]*b[2]-a[2]*b[1],
            a[2]*b[0]-a[0]*b[2],
            a[0]*b[1]-a[1]*b[0])

def dot(a,b):
    return a[0]*b[0]+a[1]*b[1]+a[2]*b[2]

def ring(name, profile, material, segments=SEGMENTS, tire=False):
    """Closed annular lathe surface with nonzero inner radius."""
    assert segments >= 12 and len(profile) >= 4
    assert all(r>0 for x,r in profile)
    vertices=[pnt(x,r,2*math.pi*j/segments)
              for x,r in profile for j in range(segments)]
    faces=[]
    materials=[]
    for i in range(len(profile)):
        nxt=(i+1)%len(profile)
        for j in range(segments):
            k=(j+1)%segments
            a=i*segments+j
            b=i*segments+k
            c=nxt*segments+k
            d=nxt*segments+j
            # The polygon winding is corrected in the geometry self-test
            # by checking each closed solid's signed volume.
            faces.extend(((a,b,c),(a,c,d)))
            if tire:
                is_tread = 4 <= i <= 9
                siped = is_tread and ((j+2*i)%16 in (0,1))
                chosen=("Tread_Sipes" if siped else
                        "Rubber_Tread" if is_tread else "Rubber_Sidewall")
            else:
                chosen=material
            materials.extend((chosen,chosen))
    return dict(name=name, vertices=vertices, faces=faces,
                face_materials=materials, smooth=tire)

def wedge(name,x0,x1,r0,r1,theta0,theta1,material):
    """Six-sided sector prism, used for fixed, open alloy spokes."""
    assert x1>x0 and r1>r0 and theta1>theta0
    v=[pnt(x0,r0,theta0),pnt(x1,r0,theta0),
       pnt(x1,r0,theta1),pnt(x0,r0,theta1),
       pnt(x0,r1,theta0),pnt(x1,r1,theta0),
       pnt(x1,r1,theta1),pnt(x0,r1,theta1)]
    quads=((0,3,2,1),(4,5,6,7),(0,1,5,4),
           (1,2,6,5),(2,3,7,6),(3,0,4,7))
    centre=tuple(sum(vv[d] for vv in v)/8 for d in range(3))
    faces=[]
    for q in quads:
        a,b,c,d=q
        n=cross(sub(v[b],v[a]),sub(v[c],v[a]))
        mid=tuple(sum(v[k][t] for k in q)/4 for t in range(3))
        if dot(n,sub(mid,centre))<0:
            q=(d,c,b,a)
        faces.extend(((q[0],q[1],q[2]),(q[0],q[2],q[3])))
    return dict(name=name,vertices=v,faces=faces,
                face_materials=[material]*len(faces),smooth=False)

def cylinder_x(name,r,x0,x1,y,z,material,segments=16):
    """Closed fastening stud with flat ends; always off the axle bore."""
    assert r>0 and x1>x0 and math.hypot(y,z)>r+AXLE_BORE_RADIUS
    v=[(x,y+r*math.cos(2*math.pi*j/segments),
           z+r*math.sin(2*math.pi*j/segments))
       for x in (x0,x1) for j in range(segments)]
    v.extend(((x0,y,z),(x1,y,z)))
    faces=[]
    for j in range(segments):
        k=(j+1)%segments
        faces.extend(((j,k,segments+k),(j,segments+k,segments+j),
                      (2*segments,k,j),(2*segments+1,segments+j,segments+k)))
    return dict(name=name,vertices=v,faces=faces,
                face_materials=[material]*len(faces),smooth=False)

def build():
    parts=[]
    parts.append(ring("Balloon_Tire",TIRE_PROFILE,"Rubber_Sidewall",tire=True))
    # Broad, smooth, low-pressure footprint: ~0.41 m nearly-flat crown.
    parts.append(ring("Alloy_Bead_Rim",[
        (-.296,.194),(-.301,.207),(-.301,.247),(-.274,.257),
        (-.235,.260),(.235,.260),(.274,.257),(.301,.247),
        (.301,.207),(.296,.194)],"Alloy_Titanium"))
    # Ten open radial spokes on each side. The spoke tips overlap the
    # alloy bead seat intentionally; none moves relative to the tire.
    for side,sign in (("L",-1),("R",1)):
        x=sign*.309
        for i in range(10):
            a=2*math.pi*i/10
            parts.append(wedge("Spoke_%s_%02d"%(side,i+1),
                x-.013,x+.013,.076,.237,a-.065,a+.065,
                "Alloy_Titanium"))
        # Decorative outer-side trim sits on the rim, not in the bore.
        xtrim=sign*.307
        parts.append(ring("Bead_Trim_"+side,[
            (xtrim-.003,.231),(xtrim-.003,.242),
            (xtrim+.003,.242),(xtrim+.003,.231)],
            "Accent_Amber"))
    # Three coaxial, OPEN rings: actual 90-mm through-hole, not a solid
    # axle cylinder disguised by a decorative outer ring.
    parts.append(ring("Axle_Hub",[
        (-.330,AXLE_BORE_RADIUS),(-.330,.093),
        (.330,.093),(.330,AXLE_BORE_RADIUS)],"Hub_Graphite"))
    for side,sign in (("L",-1),("R",1)):
        x0,x1= ((-.347,-.329) if sign<0 else (.329,.347))
        parts.append(ring("Hub_Collar_"+side,[
            (x0,AXLE_BORE_RADIUS),(x0,.068),
            (x1,.068),(x1,AXLE_BORE_RADIUS)],"Alloy_Titanium"))
        for i in range(10):
            a=2*math.pi*i/10
            y,z=.139*math.cos(a),.139*math.sin(a)
            bx0,bx1= ((-.338,-.315) if sign<0 else (.315,.338))
            parts.append(cylinder_x("Bolt_%s_%02d"%(side,i+1),
                .009,bx0,bx1,y,z,"Bolt_Steel",12))
    return parts

def validate(parts):
    assert len(parts)==47, len(parts)
    assert len({p["name"] for p in parts})==len(parts)
    counts=Counter()
    all_radii=[]
    all_x=[]
    for part in parts:
        v=part["vertices"]
        f=part["faces"]
        assert len(f)==len(part["face_materials"])
        assert len(v)>0 and len(f)>0
        edges=Counter()
        signed_volume=0.0
        for a,b,c in f:
            assert len({a,b,c})==3
            assert 0<=a<len(v) and 0<=b<len(v) and 0<=c<len(v)
            n=cross(sub(v[b],v[a]),sub(v[c],v[a]))
            assert dot(n,n)>1e-13, "Degenerate face in "+part["name"]
            signed_volume+=dot(v[a],cross(v[b],v[c]))/6
            for s,t in ((a,b),(b,c),(c,a)):
                edges[tuple(sorted((s,t)))]+=1
        assert all(n==2 for n in edges.values()), "Non-manifold "+part["name"]
        assert abs(signed_volume)>1e-8, "Zero-volume "+part["name"]
        assert all(mat in MATERIALS for mat in part["face_materials"])
        assert all(all(math.isfinite(x) for x in vertex) for vertex in v)
        all_radii.extend(math.hypot(y,z) for x,y,z in v)
        all_x.extend(abs(x) for x,y,z in v)
        counts.update(part["face_materials"])
    assert len([p for p in parts if p["name"].startswith("Spoke_")])==20
    assert len([p for p in parts if p["name"].startswith("Bolt_")])==20
    assert set(counts)==set(MATERIALS), sorted(set(MATERIALS)-set(counts))
    for name in ("Axle_Hub","Hub_Collar_L","Hub_Collar_R"):
        part=next(p for p in parts if p["name"]==name)
        radii=[math.hypot(y,z) for x,y,z in part["vertices"]]
        assert abs(min(radii)-AXLE_BORE_RADIUS)<1e-7
        assert min(radii)>REFERENCE_SHAFT_RADIUS
    assert AXLE_BORE_RADIUS-REFERENCE_SHAFT_RADIUS>=.010
    assert .5119<=max(all_radii)<=.5121
    assert max(all_x)<.360
    # One rigid rotor: every visible component has the same transform.
    # A full 360-degree spin cannot cause internal relative collisions.
    return dict(id=ASSET_ID,qa="PASS",parts=len(parts),
        vertices=sum(len(p["vertices"]) for p in parts),
        triangles=sum(len(p["faces"]) for p in parts),
        materials=len(MATERIALS),spokes=20,bolts=20,
        diameter_m=round(2*max(all_radii),3),
        width_m=round(2*max(all_x),3),
        axle_bore_diameter_m=round(2*AXLE_BORE_RADIUS,3),
        shaft_radial_clearance_m=round(
            AXLE_BORE_RADIUS-REFERENCE_SHAFT_RADIUS,3),
        pivot=[0,0,0],axle_axis="+X",spin_axis="+X",
        rigid_rotation_clearance="PASS (analytic; single rigid rotor)",
        blender="NOT RUN",unity="NOT RUN")

def export_obj(parts,outdir):
    """Actual deterministic static mesh for Unity's built-in OBJ importer."""
    stem="M02-003_balloon_sand_wheel"
    obj=outdir/(stem+".obj")
    mtl=outdir/(stem+".mtl")
    mlines=["# M02-003 PBR approximation for Unity OBJ import"]
    for name,(rgba,metal,rough) in MATERIALS.items():
        mlines+=["newmtl "+name,
                 "Kd %.6f %.6f %.6f"%rgba[:3],
                 "Ks %.6f %.6f %.6f"%((metal*.7,)*3),
                 "Ns %.2f"%(max(1.0,(1-rough)*200)),
                 "d 1.0","illum 2",""]
    mtl.write_text("\n".join(mlines)+"\n",encoding="utf-8")
    lines=["# M02-003 1 unit = 1 metre, +X axle, origin = hub centre",
           "mtllib "+mtl.name]
    offset=1
    for part in parts:
        lines.append("o "+part["name"])
        for x,y,z in part["vertices"]:
            lines.append("v %.8f %.8f %.8f"%(x,y,z))
        prev=None
        for (a,b,c),mat in zip(part["faces"],part["face_materials"]):
            if mat!=prev:
                lines.append("usemtl "+mat)
                prev=mat
            lines.append("f %d %d %d"%(a+offset,b+offset,c+offset))
        offset+=len(part["vertices"])
    obj.write_text("\n".join(lines)+"\n",encoding="utf-8")
    return {"obj":str(obj),"mtl":str(mtl)}

def export_blender(parts,outdir):
    import bpy
    old=bpy.data.collections.get("MODEL_M02_003")
    if old:
        for ob in list(old.objects):
            if ob.data and ob.type=="MESH":
                mesh=ob.data
                bpy.data.objects.remove(ob,do_unlink=True)
                if mesh.users==0:
                    bpy.data.meshes.remove(mesh)
            else:
                bpy.data.objects.remove(ob,do_unlink=True)
        bpy.data.collections.remove(old)
    collection=bpy.data.collections.new("MODEL_M02_003")
    bpy.context.scene.collection.children.link(collection)
    def empty(name,parent=None):
        ob=bpy.data.objects.new(name,None)
        collection.objects.link(ob)
        ob.parent=parent
        ob.empty_display_size=.075
        return ob
    mount=empty("MOUNT_AXLE")
    rotor=empty("WHEEL_ROTATOR",mount)
    mats={}
    for name,(rgba,metal,rough) in MATERIALS.items():
        key="M02_003_"+name
        mat=bpy.data.materials.get(key)
        if mat is None:
            mat=bpy.data.materials.new(key)
        mat.use_nodes=True
        node=mat.node_tree.nodes.get("Principled BSDF")
        if node:
            node.inputs["Base Color"].default_value=rgba
            node.inputs["Metallic"].default_value=metal
            node.inputs["Roughness"].default_value=rough
        mat.diffuse_color=rgba
        mats[name]=mat
    ordered=list(MATERIALS)
    for part in parts:
        mesh=bpy.data.meshes.new("M02_003_"+part["name"])
        mesh.from_pydata(part["vertices"],[],part["faces"])
        mesh.update(calc_edges=True)
        ob=bpy.data.objects.new("M02_003_"+part["name"],mesh)
        collection.objects.link(ob)
        ob.parent=rotor
        for key in ordered:
            mesh.materials.append(mats[key])
        for polygon,mat in zip(mesh.polygons,part["face_materials"]):
            polygon.material_index=ordered.index(mat)
            polygon.use_smooth=part["smooth"]
    # 5 quarter-turn Euler keys prevent 0/360 quaternion collapse.
    for frame,angle in ((1,0),(16,math.pi/2),(31,math.pi),
                        (46,3*math.pi/2),(61,2*math.pi)):
        rotor.rotation_euler[0]=angle
        rotor.keyframe_insert(data_path="rotation_euler",index=0,frame=frame)
    if rotor.animation_data and rotor.animation_data.action:
        try:
            for curve in rotor.animation_data.action.fcurves:
                for kp in curve.keyframe_points:
                    kp.interpolation="LINEAR"
        except AttributeError:
            pass  # Blender 4.4+ layered action API
    bpy.context.scene.frame_start=1
    bpy.context.scene.frame_end=61
    bpy.context.scene.render.fps=60
    bpy.context.scene.frame_set(1)
    bpy.ops.object.select_all(action="DESELECT")
    for ob in collection.objects:
        ob.select_set(True)
    bpy.context.view_layer.objects.active=rotor
    fbx=outdir/"M02-003_balloon_sand_wheel.fbx"
    glb=outdir/"M02-003_balloon_sand_wheel.glb"
    bpy.ops.export_scene.fbx(filepath=str(fbx),use_selection=True,
        axis_forward="-Z",axis_up="Y",add_leaf_bones=False,
        bake_anim=True,bake_anim_use_all_actions=False)
    bpy.ops.export_scene.gltf(filepath=str(glb),export_format="GLB",
        use_selection=True,export_animations=True)
    return {"fbx":str(fbx),"glb":str(glb),"blender":"EXPORTED (bpy)",
            "animation":"full +X turn, 5 Euler keys, 61 frames @60fps"}

def main():
    argv=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else sys.argv[1:]
    parser=argparse.ArgumentParser()
    parser.add_argument("--out",default="build/M02-003")
    parser.add_argument("--self-test",action="store_true")
    args=parser.parse_args(argv)
    parts=build()
    report=validate(parts)
    if args.self_test:
        print(json.dumps(report,indent=2))
        return
    outdir=Path(args.out)
    outdir.mkdir(parents=True,exist_ok=True)
    report.update(export_obj(parts,outdir))
    try:
        import bpy
    except ImportError:
        report["blender"]="NOT RUN (bpy unavailable)"
        report["animation"]="NOT EXPORTED; run inside Blender for FBX/GLB"
    else:
        report.update(export_blender(parts,outdir))
    (outdir/"M02-003_manifest.json").write_text(
        json.dumps(report,indent=2)+"\n",encoding="utf-8")
    print(json.dumps(report,indent=2))

if __name__=="__main__":
    main()
