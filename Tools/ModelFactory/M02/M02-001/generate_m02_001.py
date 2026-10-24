#!/usr/bin/env python3
"""M02-001 segmented off-road wheel for RoboRougelike.

Authoring: Blender 4.2+ / 5.x via bpy. Headless preview/export: Python 3.11+
and trimesh (static GLB only). 1 unit = 1 metre.
Pivot: centre of axle, axle/rotation axis: local +X.
Do not integrate into gameplay automatically.
"""
from __future__ import annotations

import argparse
import json
import math
import os
import sys
from pathlib import Path

ASSET_ID = "M02-001"
RADIUS_TIRE = 0.500
RADIUS_LUG = 0.534
WIDTH = 0.340
# This is an actual open mechanical interface, not just a marker at (0, 0, 0).
# Shaft <= 0.034 m radius => >= 9 mm nominal radial clearance.
AXLE_BORE_RADIUS = 0.043
REFERENCE_SHAFT_RADIUS = 0.034
HUB_OUTER_RADIUS = 0.094
HUB_CAP_OUTER_RADIUS = 0.058
LUG_COUNT = 20
LANES = (-0.109, 0.0, 0.109)
SEGMENTS = 72

# base_color RGBA, metallic, roughness
MATERIALS = {
    "Tire_Sidewall": ((0.055, 0.061, 0.067, 1), 0.02, 0.92),
    "Tread_Graphite": ((0.085, 0.092, 0.099, 1), 0.00, 0.97),
    "Tread_Dark": ((0.065, 0.074, 0.082, 1), 0.00, 0.94),
    "Rim_Anodized": ((0.19, 0.235, 0.27, 1), 0.79, 0.33),
    "Spokes_Steel": ((0.35, 0.41, 0.43, 1), 0.82, 0.30),
    "Accent_Amber": ((0.94, 0.31, 0.047, 1), 0.52, 0.32),
    "Fastener_Silver": ((0.56, 0.61, 0.62, 1), 0.88, 0.27),
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
    parts = []
    add = parts.append
    # Rubber torus: profiled sidewalls and flat central tread carcass.
    add(ring("Tire_Carcass",
        [(-.155,.295),(-.170,.337),(-.170,.438),(-.167,.470),
         (-.160,.500),(.160,.500),(.167,.470),
         (.170,.438),(.170,.337),(.155,.295)],
        "Tire_Sidewall"))
    # Interleaved three-lane lugs; separated in X and by 4+ cm radial gaps.
    pitch = 2*math.pi/LUG_COUNT
    offsets = (-0.055, 0.045, -0.055)
    for lane, xcentre in enumerate(LANES):
        for i in range(LUG_COUNT):
            a = i*pitch + offsets[lane]
            add(wedge(f"Lug_{lane+1}_{i+1:02d}",
                      xcentre-.044, xcentre+.044,
                      .497, RADIUS_LUG, a-.113, a+.113,
                      "Tread_Graphite" if (lane+i)%2 else "Tread_Dark"))
    # Metal bead seat and outer alloy rim, both separated from outer lugs.
    add(ring("Rim_Outer",
        [(-.148,.258),(-.154,.270),(-.154,.296),(-.140,.316),
         (-.123,.322),(.123,.322),(.140,.316),(.154,.296),
         (.154,.270),(.148,.258)], "Rim_Anodized"))
    for side, sx in (("L",-1),("R",1)):
        x = sx*.158
        add(ring(f"Rim_Accent_{side}",
            [(x-.006,.277),(x-.006,.288),(x+.006,.288),(x+.006,.277)],
            "Accent_Amber"))
        # Eight exposed steel spokes, with intentional overlaps into rim and hub.
        for i in range(8):
            a = 2*math.pi*i/8
            add(wedge(f"Spoke_{side}_{i+1}",
                      x-.012, x+.012, .086, .290,
                      a-.085, a+.085, "Spokes_Steel"))
    # Critical clearance repair: the old axle hub and both hub caps were
    # SOLID cylinders spanning the X axis. Such geometry blocked any actual
    # axle entering the wheel. Use closed annular meshes with a through-bore;
    # no Boolean/voxel operation, so triangulation stays deterministic.
    add(ring("Axle_Hub",
        [(-.177, AXLE_BORE_RADIUS),(-.177,HUB_OUTER_RADIUS),
         (.177,HUB_OUTER_RADIUS),(.177,AXLE_BORE_RADIUS)],
        "Spokes_Steel"))
    for side, sx in (("L",-1),("R",1)):
        x0,x1 = ((-.192,-.176) if sx<0 else (.176,.192))
        add(ring(f"Hub_Cap_{side}",
            [(x0,AXLE_BORE_RADIUS),(x0,HUB_CAP_OUTER_RADIUS),
             (x1,HUB_CAP_OUTER_RADIUS),(x1,AXLE_BORE_RADIUS)],
            "Rim_Anodized"))
        for i in range(8):
            a = 2*math.pi*i/8
            y,z = .115*math.cos(a),.115*math.sin(a)
            x0,x1 = ((-.186,-.168) if sx<0 else (.168,.186))
            add(cylinder_x(f"Bolt_{side}_{i+1:02d}",.011,x0,x1,y,z,
                           "Fastener_Silver",12))
    # Contrasting valve stem; no protrusion beyond the tire.
    add(cylinder_x("Valve_Stem",.008,.148,.186,
                   .243, .145,"Fastener_Silver",12))
    return parts

def validate_axle_bore(parts):
    """The mechanical axle opening must remain continuous at all X stations.

    Validate the actual vertices and triangle-edge manifold, not just a
    nominal dimension written in a manifest. The cylinder around the origin
    must remain unoccupied by hub meshes at the axial mount region.
    """
    by_name = {p["name"]: p for p in parts}
    clearance = AXLE_BORE_RADIUS - REFERENCE_SHAFT_RADIUS
    assert clearance >= 0.008, "Insufficient axial shaft clearance"
    for name in ("Axle_Hub", "Hub_Cap_L", "Hub_Cap_R"):
        part = by_name[name]
        radii = [math.hypot(y, z) for x, y, z in part["vertices"]]
        assert min(radii) > REFERENCE_SHAFT_RADIUS, (
            f"{name} closes the axle passage")
        assert abs(min(radii) - AXLE_BORE_RADIUS) < 1e-7, (
            f"{name} must meet the same bore radius")
        edges = {}
        for triangle in part["faces"]:
            for a, b in zip(triangle, triangle[1:] + triangle[:1]):
                edge = (min(a, b), max(a, b))
                edges[edge] = edges.get(edge, 0) + 1
        assert all(n == 2 for n in edges.values()), (
            f"{name} has non-manifold or open boundaries")
        assert len(part["faces"]) > 0
    return {"bore_diameter_m": round(2*AXLE_BORE_RADIUS, 3),
            "reference_shaft_diameter_m": round(2*REFERENCE_SHAFT_RADIUS, 3),
            "shaft_radial_clearance_m": round(clearance, 3),
            "hub_manifold_closed": "3/3 PASS"}


def validate(parts):
    names=set()
    tri_count=0
    min_area=1e-13
    maxrad=0.0
    maxx=0.0
    for p in parts:
        assert p["name"] not in names, "Duplicate part name"
        names.add(p["name"])
        assert p["material"] in MATERIALS
        vertices=p["vertices"]
        assert vertices and p["faces"]
        for x,y,z in vertices:
            assert all(math.isfinite(c) for c in (x,y,z))
            maxrad=max(maxrad,math.hypot(y,z))
            maxx=max(maxx,abs(x))
        for face in p["faces"]:
            assert len(face)==3 and len(set(face))==3
            assert all(0<=i<len(vertices) for i in face)
            a,b,c=[vertices[i] for i in face]
            n=_cross(_sub(b,a),_sub(c,a))
            assert _dot(n,n)>min_area, f"Degenerate triangle: {p['name']}"
            tri_count+=1
    assert len([p for p in parts if p["name"].startswith("Lug_")])==60
    assert len([p for p in parts if p["name"].startswith("Bolt_")])==16
    assert .532 < maxrad < .536, maxrad
    assert maxx < .205, maxx
    bore_qa = validate_axle_bore(parts)
    return dict(**bore_qa,parts=len(parts),triangles=tri_count,
                materials=len(MATERIALS),lugs=60,bolts=16,
                diameter_m=round(2*maxrad,3),
                width_m=round(2*maxx,3),
                pivot=[0,0,0],axle_axis="+X",spin_axis="+X",
                qa_geometry="PASS",
                qa_motion_clearance="analytic: rotating rigid assembly only")

def blender_export(parts,outdir):
    import bpy
    # Safe repeatability: delete only objects in dedicated asset collection.
    collection=bpy.data.collections.get("MODEL_M02_001")
    if collection:
        for ob in list(collection.objects):
            bpy.data.objects.remove(ob,do_unlink=True)
        bpy.data.collections.remove(collection)
    collection=bpy.data.collections.new("MODEL_M02_001")
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
        mat=bpy.data.materials.get("M02_001_"+name)
        if mat is None:
            mat=bpy.data.materials.new("M02_001_"+name)
        mat.use_nodes=True
        shader=mat.node_tree.nodes.get("Principled BSDF")
        if shader:
            shader.inputs["Base Color"].default_value=rgba
            shader.inputs["Metallic"].default_value=metallic
            shader.inputs["Roughness"].default_value=roughness
        mat.diffuse_color=rgba
        mats[name]=mat
    for p in parts:
        mesh=bpy.data.meshes.new("M02_001_"+p["name"])
        mesh.from_pydata(p["vertices"],[],p["faces"])
        mesh.update(calc_edges=True)
        ob=bpy.data.objects.new("M02_001_"+p["name"],mesh)
        collection.objects.link(ob)
        ob.parent=rotator
        ob.data.materials.append(mats[p["material"]])
        for polygon in mesh.polygons:
            polygon.use_smooth=p["smooth"]
    # Quarter-turn keys keep adjacent quaternion samples distinct; the
    # existing GLB animation is a visual reference, not wheel physics.
    # A second collision gate verifies the centre bore independently.
    validate_axle_bore(parts)
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
    fbx=outdir/"M02-001_segmented_offroad_wheel.fbx"
    glb=outdir/"M02-001_segmented_offroad_wheel.glb"
    bpy.ops.export_scene.fbx(filepath=str(fbx),use_selection=True,
        axis_forward="-Z",axis_up="Y",add_leaf_bones=False,
        bake_anim=True,bake_anim_use_all_actions=False)
    bpy.ops.export_scene.gltf(filepath=str(glb),export_format="GLB",
        use_selection=True,export_animations=True)
    return {"backend":"bpy","fbx":str(fbx),"glb":str(glb),
            "animation":"61 frames / 60 fps / +X axis"}

def static_export(parts,outdir):
    """Preview fallback when Blender is absent: static GLB, no baked animation."""
    import trimesh
    scene=trimesh.Scene()
    scene.graph.update(frame_from="world",frame_to="MOUNT_AXLE")
    scene.graph.update(frame_from="MOUNT_AXLE",frame_to="WHEEL_ROTATOR")
    for p in parts:
        mesh=trimesh.Trimesh(vertices=p["vertices"],faces=p["faces"],
                             process=False)
        rgba,metal,rough=MATERIALS[p["material"]]
        color=[int(255*c) for c in rgba]
        mesh.visual=trimesh.visual.TextureVisuals(
            material=trimesh.visual.material.PBRMaterial(
                name=p["material"],baseColorFactor=color,
                metallicFactor=metal,roughnessFactor=rough))
        scene.add_geometry(mesh,geom_name=p["name"],node_name=p["name"],
                           parent_node_name="WHEEL_ROTATOR")
    glb=outdir/"M02-001_segmented_offroad_wheel.glb"
    glb.write_bytes(scene.export(file_type="glb"))
    return {"backend":"trimesh","glb":str(glb),
            "animation":"NOT EXPORTED — Blender bpy required for animated FBX/GLB"}

def main():
    argv=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else sys.argv[1:]
    parser=argparse.ArgumentParser()
    parser.add_argument("--out",default="build/M02-001")
    parser.add_argument("--self-test",action="store_true")
    args=parser.parse_args(argv)
    parts=build()
    report=validate(parts)
    print(json.dumps(report,indent=2))
    if args.self_test:
        return
    outdir=Path(args.out)
    outdir.mkdir(parents=True,exist_ok=True)
    try:
        import bpy
    except ImportError:
        result=static_export(parts,outdir)
    else:
        result=blender_export(parts,outdir)
    report.update(result)
    (outdir/"M02-001_manifest.json").write_text(
        json.dumps(report,indent=2)+"\n",encoding="utf-8")
    print(json.dumps(result,indent=2))

if __name__=="__main__":
    main()
