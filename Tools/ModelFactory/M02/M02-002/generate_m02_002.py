#!/usr/bin/env python3
"""M02-002 narrow speed wheel for RoboRougelike.

Authoring: Blender 4.2+ / 5.x via bpy. Dependency-free OBJ/MTL: Python 3.10+; Blender bpy: FBX and animated GLB. 1 unit = 1 metre.
Pivot: X=0 side-mount plane; axle/rotation axis: local +X.
Do not integrate into gameplay automatically.
"""
from __future__ import annotations

import argparse
import json
import math
import os
import sys
from pathlib import Path

ASSET_ID = "M02-002"
RADIUS_TIRE = 0.523
RADIUS_LUG = 0.523
WIDTH = 0.180
LUG_COUNT = 0
LANES = ()
SEGMENTS = 96

# base_color RGBA, metallic, roughness
MATERIALS = {
    "Tire_Graphite": ((.114,.133,.161,1), .00,.87),
    "Rim_Titanium": ((.604,.655,.686,1), .82,.28),
    "Spoke_Ceramic": ((.831,.847,.835,1), .36,.30),
    "Hub_Copper": ((.729,.467,.314,1), .79,.33),
    "Accent_Amber": ((.914,.675,.247,1), .35,.32),
    "Mount_Steel": ((.259,.298,.337,1), .83,.46),
    "Brake_Graphite": ((.160,.185,.203,1), .76,.49),
}

def point(x: float, r: float, a: float) -> tuple[float, float, float]:
    """Wheel revolves around local X; radial plane is YZ."""
    return (x, r * math.cos(a), r * math.sin(a))

def ring(name, profile, material, segments=SEGMENTS):
    """Closed revolved cross-section with smooth, non-degenerate radius > 0."""
    assert min(r for x, r in profile) > 0
    vertices = [
        point(x, r, 2 * math.pi * j / segments)
        for x, r in profile for j in range(segments)
    ]
    faces = []
    for i in range(len(profile)):
        nxt = (i + 1) % len(profile)
        for j in range(segments):
            k = (j + 1) % segments
            a, b = i * segments + j, i * segments + k
            c, d = nxt * segments + k, nxt * segments + j
            faces.extend(((a, b, c), (a, c, d)))
    return dict(name=name, material=material, vertices=vertices, faces=faces, smooth=True)

def _cross(u, v):
    return (u[1]*v[2]-u[2]*v[1],
            u[2]*v[0]-u[0]*v[2],
            u[0]*v[1]-u[1]*v[0])

def _sub(a, b):
    return (a[0]-b[0], a[1]-b[1], a[2]-b[2])

def _dot(a, b):
    return sum(x*y for x, y in zip(a, b))

def wedge(name, x0, x1, r0, r1, theta0, theta1, material):
    """Closed six-face solid. Faces are outward-facing; no zero-area triangles."""
    assert x1 > x0 and r1 > r0 and theta1 > theta0
    verts = [
        point(x0, r0, theta0), point(x1, r0, theta0),
        point(x1, r0, theta1), point(x0, r0, theta1),
        point(x0, r1, theta0), point(x1, r1, theta0),
        point(x1, r1, theta1), point(x0, r1, theta1),
    ]
    quads = [(0,3,2,1), (4,5,6,7), (0,1,5,4),
             (1,2,6,5), (2,3,7,6), (3,0,4,7)]
    centre = tuple(sum(p[i] for p in verts)/8 for i in range(3))
    faces = []
    for q in quads:
        a,b,c,d = q
        n = _cross(_sub(verts[b],verts[a]), _sub(verts[c],verts[a]))
        facecentre = tuple(sum(verts[k][i] for k in q)/4 for i in range(3))
        if _dot(n,_sub(facecentre,centre)) < 0:
            q = (d,c,b,a)
        faces.extend(((q[0],q[1],q[2]), (q[0],q[2],q[3])))
    return dict(name=name, material=material, vertices=verts, faces=faces, smooth=False)

def cylinder_x(name, radius, x0, x1, y, z, material, segments=24):
    """Solid cylinder with caps, aligned with the axle."""
    assert radius > 0 and x1 > x0
    vertices = [
        (x, y + radius*math.cos(2*math.pi*j/segments),
         z + radius*math.sin(2*math.pi*j/segments))
        for x in (x0,x1) for j in range(segments)
    ]
    vertices.extend(((x0,y,z),(x1,y,z)))
    faces = []
    for j in range(segments):
        k = (j+1)%segments
        faces.extend(((j,k,segments+k),(j,segments+k,segments+j)))
        faces.extend(((2*segments,k,j),
                      (2*segments+1,segments+j,segments+k)))
    return dict(name=name, material=material, vertices=vertices, faces=faces, smooth=False)

def build():
    """0.523-m radius, 0.180-m tire, side-mount X=0, +X spin."""
    parts=[];add=parts.append
    c=.185
    profile=[
      (-.075,.335),(-.082,.349),(-.087,.383),(-.090,.412),
      (-.085,.458),(-.079,.485),(-.068,.502),(-.052,.517),
      (-.046,.517),(-.041,.509),(-.032,.509),(-.027,.519),
      (0,.523),(.027,.519),(.032,.509),(.041,.509),
      (.046,.517),(.052,.517),(.068,.502),(.079,.485),
      (.085,.458),(.090,.412),(.087,.383),(.082,.349),(.075,.335)]
    add(ring("Narrow_Rubber_Tire",[(c+x,r) for x,r in profile],"Tire_Graphite"))
    def ann(name,mat,x0,x1,r0,r1,stationary=False):
        part=ring(name,[(x0,r0),(x0,r1),(x1,r1),(x1,r0)],mat)
        part["stationary"]=stationary
        add(part)
    ann("Forged_Rim","Rim_Titanium",.143,.228,.286,.340)
    ann("Rear_Rim_Lip","Mount_Steel",.127,.144,.295,.331)
    ann("Amber_Front_Ring","Accent_Amber",.224,.237,.299,.310)
    ann("Hub_Carrier","Hub_Copper",.145,.230,.043,.112)
    ann("Hub_Front","Rim_Titanium",.227,.246,.050,.098)
    ann("Brake_Rotor","Brake_Graphite",.135,.151,.108,.273)
    for i in range(10):
        a=2*math.pi*(i+.08)/10
        add(wedge(f"Spoke_Alloy_{i:02}",.150,.225,.106,.305,
                  a-.09,a+.09,"Rim_Titanium"))
        add(wedge(f"Spoke_Ceramic_{i:02}",.221,.233,.157,.281,
                  a-.046,a+.046,"Spoke_Ceramic"))
    ann("Mount_Flange_STATIC","Mount_Steel",-.040,.000,.013,.072,True)
    ann("Axle_Shaft_STATIC","Rim_Titanium",.000,.258,.012,.033,True)
    ann("Axle_Retainer_STATIC","Hub_Copper",.256,.275,.012,.038,True)
    return parts

def validate(parts):
    """Check closed, non-degenerate manifold geometry, materials and clearances."""
    from collections import Counter
    names=set();triangles=0;vertices=0
    for p in parts:
        assert p["name"] not in names
        names.add(p["name"])
        assert p["material"] in MATERIALS
        edges=Counter()
        vs=p["vertices"]
        assert vs and p["faces"]
        for x,y,z in vs:assert all(math.isfinite(v) for v in (x,y,z))
        for face in p["faces"]:
            assert len(face)==3 and len(set(face))==3
            assert all(0<=i<len(vs) for i in face)
            a,b,c=[vs[i] for i in face]
            n=_cross(_sub(b,a),_sub(c,a))
            assert _dot(n,n)>1e-15,p["name"]
            for i,j in zip(face,face[1:]+face[:1]):
                edges[tuple(sorted((i,j)))]+=1
            triangles+=1
        assert all(n==2 for n in edges.values()),p["name"]
        vertices+=len(vs)
    # Assembly continuity gate: visual solids must intersect at their
    # mounting faces. V1 had a floating tire (2 mm radial gap), ceramic
    # spoke overlays, a hub cap, accent ring, and brake disc separated
    # from the structural wheel assembly. These are not articulated joints.
    components={p["name"]:p for p in parts}
    def x_bounds(part):
        return min(v[0] for v in part["vertices"]), max(v[0] for v in part["vertices"])
    def radial_bounds(part):
        rr=[math.hypot(v[1],v[2]) for v in part["vertices"]]
        return min(rr),max(rr)
    def overlap(interval1,interval2):
        return min(interval1[1],interval2[1])-max(interval1[0],interval2[0])
    contact_pairs=[
        ("Narrow_Rubber_Tire","Forged_Rim",.004,.02),
        ("Forged_Rim","Amber_Front_Ring",.003,.005),
        ("Hub_Carrier","Hub_Front",.002,.04),
        ("Hub_Carrier","Brake_Rotor",.004,.003),
        ("Axle_Shaft_STATIC","Axle_Retainer_STATIC",.001,.015),
    ]
    for a,b,min_x,min_radial in contact_pairs:
        lhs,rhs=components[a],components[b]
        assert overlap(x_bounds(lhs),x_bounds(rhs))>=min_x-1e-8,(a,b,"axial gap")
        assert overlap(radial_bounds(lhs),radial_bounds(rhs))>=min_radial-1e-8,(a,b,"radial gap")
    for i in range(10):
        base=components[f"Spoke_Alloy_{i:02}"]
        ceramic=components[f"Spoke_Ceramic_{i:02}"]
        assert overlap(x_bounds(base),x_bounds(ceramic))>=.003-1e-8
        assert overlap(radial_bounds(base),radial_bounds(ceramic))>.10
    # Still preserve a REAL shaft-to-hub bore radial air gap through travel.
    assert min(radial_bounds(components[n])[0] for n in
               ("Hub_Carrier","Hub_Front")) >= .043-1e-8
    assert radial_bounds(components["Axle_Shaft_STATIC"])[1] <= .033+1e-8
    assert len([p for p in parts if p["name"].startswith("Spoke_Alloy")])==10
    assert len([p for p in parts if p.get("stationary")])==3
    assert abs(.043-.033-.010)<1e-9
    return dict(model="M02-002",parts=len(parts),vertices=vertices,
       triangles=triangles,materials=len(MATERIALS),spokes=10,
       tire_radius_m=.523,tire_width_m=.180,mount_plane_x_m=0,
       wheel_center_x_m=.185,axle_axis="+X",
       axle_radial_clearance_m=.010,all_parts_closed_manifold=True,
       fixed_contact_pairs_verified=len(contact_pairs),
       spoke_ceramic_contacts_verified=10,
       structural_revision="M02-002_V2_CONNECTED_ASSEMBLY",
       qa_geometry="PASS_STATIC",blender="NOT_RUN",unity="NOT_RUN")

def blender_export(parts,outdir):
    import bpy
    # Safe repeatability: delete only objects in dedicated asset collection.
    collection=bpy.data.collections.get("MODEL_M02_002")
    if collection:
        for ob in list(collection.objects):
            bpy.data.objects.remove(ob,do_unlink=True)
        bpy.data.collections.remove(collection)
    collection=bpy.data.collections.new("MODEL_M02_002")
    bpy.context.scene.collection.children.link(collection)
    def empty(name,parent=None):
        ob=bpy.data.objects.new(name,None)
        collection.objects.link(ob)
        ob.empty_display_size=.07
        ob.parent=parent
        return ob
    mount=empty("MOUNT_AXLE")
    rotator=empty("WHEEL_ROTATOR",mount)
    mats={}
    for name,(rgba,metallic,roughness) in MATERIALS.items():
        mat=bpy.data.materials.get("M02_002_"+name)
        if mat is None:
            mat=bpy.data.materials.new("M02_002_"+name)
        mat.use_nodes=True
        shader=mat.node_tree.nodes.get("Principled BSDF")
        if shader:
            shader.inputs["Base Color"].default_value=rgba
            shader.inputs["Metallic"].default_value=metallic
            shader.inputs["Roughness"].default_value=roughness
        mat.diffuse_color=rgba
        mats[name]=mat
    for p in parts:
        mesh=bpy.data.meshes.new("M02_002_"+p["name"])
        mesh.from_pydata(p["vertices"],[],p["faces"])
        mesh.update(calc_edges=True)
        ob=bpy.data.objects.new("M02_002_"+p["name"],mesh)
        collection.objects.link(ob)
        ob.parent=mount if p.get('stationary') else rotator
        ob.data.materials.append(mats[p["material"]])
        for polygon in mesh.polygons:
            polygon.use_smooth=p["smooth"]
    # Full rotation with five keys: avoid quaternion 0==360 interpolation collapse.
    for frame,angle in ((1,0),(16,math.pi/2),(31,math.pi),
                        (46,3*math.pi/2),(61,2*math.pi)):
        rotator.rotation_euler[0]=angle
        rotator.keyframe_insert(data_path="rotation_euler",index=0,frame=frame)
    if rotator.animation_data and rotator.animation_data.action:
        action=rotator.animation_data.action
        try:
            curves=action.fcurves
            for curve in curves:
                for kp in curve.keyframe_points:
                    kp.interpolation="LINEAR"
        except AttributeError:
            pass  # Blender 4.4+ layered action API; keys remain valid.
    bpy.context.scene.frame_start=1
    bpy.context.scene.frame_end=61
    bpy.context.scene.render.fps=60
    bpy.context.scene.frame_set(1)
    bpy.ops.object.select_all(action="DESELECT")
    for ob in collection.objects:
        ob.select_set(True)
    bpy.context.view_layer.objects.active=rotator
    # Export both: FBX is Unity-native; GLB is portable and animated.
    fbx=outdir/"M02-002_narrow_speed_wheel.fbx"
    glb=outdir/"M02-002_narrow_speed_wheel.glb"
    bpy.ops.export_scene.fbx(filepath=str(fbx),use_selection=True,
        axis_forward="-Z",axis_up="Y",add_leaf_bones=False,
        bake_anim=True,bake_anim_use_all_actions=False)
    bpy.ops.export_scene.gltf(filepath=str(glb),export_format="GLB",
        use_selection=True,export_animations=True)
    return {"backend":"bpy","fbx":str(fbx),"glb":str(glb),
            "animation":"61 frames / 60 fps / +X axis"}

def static_export(parts,outdir):
    """Dependency-free static OBJ/MTL export; Blender is optional."""
    obj=outdir/"M02-002_narrow_speed_wheel.obj"
    mtl=outdir/"M02-002_narrow_speed_wheel.mtl"
    lines=["# M02-002 Z-up, meters, X axle",f"mtllib {mtl.name}"]
    offset=1
    for p in parts:
        lines.extend([f"o {p['name']}",f"usemtl {p['material']}"])
        lines.extend(f"v {x:.7f} {y:.7f} {z:.7f}" for x,y,z in p["vertices"])
        lines.extend("f "+" ".join(str(offset+i) for i in face) for face in p["faces"])
        offset+=len(p["vertices"])
    obj.write_text("\n".join(lines)+"\n",encoding="utf-8")
    ml=[]
    for name,(rgba,metal,rough) in MATERIALS.items():
        ml.extend([f"newmtl {name}",f"Kd {rgba[0]:.5f} {rgba[1]:.5f} {rgba[2]:.5f}",
          f"Ks {metal*.45:.5f} {metal*.45:.5f} {metal*.45:.5f}",
          f"Ns {(1-rough)*400:.1f}","d 1.0","illum 2",""])
    mtl.write_text("\n".join(ml)+"\n",encoding="utf-8")
    return {"backend":"stdlib","obj":str(obj),"mtl":str(mtl),
            "animation":"NOT EXPORTED: run Blender for animated FBX/GLB"}

def main():
    argv=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else sys.argv[1:]
    parser=argparse.ArgumentParser()
    parser.add_argument("--out",default="build/M02-002")
    parser.add_argument("--self-test",action="store_true")
    args=parser.parse_args(argv)
    parts=build()
    report=validate(parts)
    print(json.dumps(report,indent=2))
    if args.self_test:
        return
    outdir=Path(args.out)
    outdir.mkdir(parents=True,exist_ok=True)
    result=static_export(parts,outdir)
    try:
        import bpy
    except ImportError:
        pass
    else:
        result.update(blender_export(parts,outdir))
    report.update(result)
    (outdir/"M02-002_manifest.json").write_text(
        json.dumps(report,indent=2)+"\n",encoding="utf-8")
    print(json.dumps(result,indent=2))

if __name__=="__main__":
    main()
