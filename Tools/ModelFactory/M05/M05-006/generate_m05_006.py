#!/usr/bin/env python3
"""M05-006 — directional barrier projector; deterministic OBJ and Blender GLB.

Visual-only. Blender Z-up, metres; front is -Y. Mount origin at (0,0,0)
on the rear interface plane. Left/right hinge Z pivots are defined in
geometry.json. Run with plain Python for static OBJ/MTL and geometry QA,
or with Blender bpy for an animated, appendable .blend library and GLB.
"""
from __future__ import annotations

import argparse
import json
import math
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
SPEC_FILE = HERE / "geometry.json"


def spec_load():
    with SPEC_FILE.open(encoding="utf-8") as f:
        data = json.load(f)
    if data["id"] != "M05-006":
        raise ValueError("Unexpected model ID")
    return data


def primitive(part):
    shape = part["shape"]
    if shape == "box":
        x, y, z = [float(q)/2 for q in part["size"]]
        verts = [(-x,-y,-z),(x,-y,-z),(x,y,-z),(-x,y,-z),
                 (-x,-y,z),(x,-y,z),(x,y,z),(-x,y,z)]
        faces = [(3,2,1,0),(4,5,6,7),(0,1,5,4),(1,2,6,5),
                 (2,3,7,6),(3,0,4,7)]
    elif shape == "bevel_box":
        x, y, z = [float(q)/2 for q in part["size"]]
        c = float(part["bevel"])
        assert 0 < c < min(x,z), part["name"]
        outline = [(-x+c,-z),(x-c,-z),(x,-z+c),(x,z-c),
                   (x-c,z),(-x+c,z),(-x,z-c),(-x,-z+c)]
        verts = [(u,-y,w) for u,w in outline] + [(u,y,w) for u,w in outline]
        faces = [tuple(range(8)),tuple(reversed(range(8,16)))]
        faces += [(i,i+8,(i+1)%8+8,(i+1)%8) for i in range(8)]
    elif shape == "cylinder":
        n = int(part["segments"])
        r, h = float(part["radius"]), float(part["depth"])/2
        verts = []
        for sign in (-1,1):
            for i in range(n):
                a = 2*math.pi*i/n
                u,v = r*math.cos(a),r*math.sin(a)
                axis = part["axis"]
                if axis == "X":
                    verts.append((sign*h,u,v))
                elif axis == "Y":
                    verts.append((v,sign*h,u))
                elif axis == "Z":
                    verts.append((u,v,sign*h))
                else:
                    raise ValueError("Invalid cylinder axis")
        faces = [tuple(reversed(range(n))),tuple(range(n,2*n))]
        faces += [(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    elif shape == "torus":
        n, m = int(part["segments"]), int(part["tube_segments"])
        r, t = float(part["major"]),float(part["minor"])
        verts = []
        for i in range(n):
            a = 2*math.pi*i/n
            for j in range(m):
                b = 2*math.pi*j/m
                verts.append(((r+t*math.cos(b))*math.cos(a),
                              (r+t*math.cos(b))*math.sin(a),
                              t*math.sin(b)))
        faces = [(i*m+j,((i+1)%n)*m+j,
                  ((i+1)%n)*m+(j+1)%m,i*m+(j+1)%m)
                 for i in range(n) for j in range(m)]
    else:
        raise ValueError("Unknown primitive "+str(shape))
    x,y,z = [float(q) for q in part["center"]]
    return [(u+x,v+y,w+z) for u,v,w in verts], faces


def mesh_world(part, spec, angle=0):
    verts, faces = primitive(part)
    parent = part["parent"]
    if parent == "base":
        return verts, faces
    pivot = spec["hinges"][parent.split("_")[1]]["pivot"]
    a = math.radians(angle if parent == "arm_R" else -angle)
    cs,sn = math.cos(a),math.sin(a)
    return [(x*cs-y*sn+pivot[0],x*sn+y*cs+pivot[1],z+pivot[2])
            for x,y,z in verts], faces


def volume(vertices, faces):
    result = 0.0
    for face in faces:
        a = vertices[face[0]]
        for i in range(1,len(face)-1):
            b,c = vertices[face[i]],vertices[face[i+1]]
            cross = (b[1]*c[2]-b[2]*c[1],
                     b[2]*c[0]-b[0]*c[2],
                     b[0]*c[1]-b[1]*c[0])
            result += sum(a[k]*cross[k] for k in range(3))/6
    return result


def validate_manifold(vertices,faces,name):
    edges = {}
    for f in faces:
        assert len(f)>=3 and len(set(f))==len(f), name
        for k in f:
            assert 0<=k<len(vertices),name
        for a,b in zip(f,f[1:]+f[:1]):
            key = (min(a,b),max(a,b))
            edges.setdefault(key,[]).append((a,b))
    assert all(len(v)==2 and v[0]==v[1][::-1]
               for v in edges.values()), ("nonmanifold/winding",name)
    assert volume(vertices,faces)>1e-9, ("negative volume",name)


def aabb(vertices):
    return [(min(v[i] for v in vertices),max(v[i] for v in vertices))
            for i in range(3)]


def intersects(a,b,tolerance=1e-9):
    return all(min(a[i][1],b[i][1])-max(a[i][0],b[i][0])>tolerance
               for i in range(3))


def swept_collision_qa(spec):
    fixed = [p for p in spec["parts"] if p["parent"]=="base"]
    moving = [p for p in spec["parts"] if p["parent"]!="base"]
    fixed_boxes = [(p,aabb(mesh_world(p,spec)[0])) for p in fixed]
    checks = 0
    exempt = 0
    # The rotating axle runs concentrically INSIDE the stationary bearing
    # ring, with a verified 18 mm radial gap. AABB cannot represent the hole.
    gap = min(p["major"]-p["minor"] for p in fixed
              if p["name"].startswith("hinge_ring_")) - max(
              p["radius"] for p in moving if p["name"].startswith("axle_")
              and not p["name"].startswith("axle_cap"))
    assert gap >= .017, ("bearing gap",gap)
    for degrees in range(31):
        moving_boxes = [(p,aabb(mesh_world(p,spec,degrees)[0]))
                        for p in moving]
        for b,bb in fixed_boxes:
            for m,mb in moving_boxes:
                checks += 1
                same_side = b["name"].endswith(m["parent"][-1])
                if (same_side and b["name"].startswith(("hinge_ring_","hinge_lip_"))
                        and m["name"].startswith("axle_")
                        and not m["name"].startswith("axle_cap_")):
                    exempt += 1
                    continue
                assert not intersects(bb,mb), (
                    "fixed/moving intersection",degrees,b["name"],m["name"])
        left = [(p,bb) for p,bb in moving_boxes if p["parent"]=="arm_L"]
        right = [(p,bb) for p,bb in moving_boxes if p["parent"]=="arm_R"]
        for lp,lb in left:
            for rp,rb in right:
                checks += 1
                assert not intersects(lb,rb), (
                    "left/right arm intersection",degrees,lp["name"],rp["name"])
    return {"sweep_samples":31,"swept_aabb_pair_checks":checks,
            "bearing_ring_axle_exemptions":exempt,
            "minimum_bearing_radial_gap_m":round(gap,5),
            "swept_collision_qa":"PASS"}


def validate(spec):
    assert spec["mount_origin"]==[0,0,0]
    assert spec["mount_type"]=="front"
    assert len(spec["materials"])>=3
    names = [p["name"] for p in spec["parts"]]
    assert len(set(names))==len(names)
    vertices=polygons=triangles=0
    mins=[float("inf")]*3
    maxs=[-float("inf")]*3
    for p in spec["parts"]:
        assert p["parent"] in ("base","arm_L","arm_R")
        assert p["material"] in spec["materials"]
        v,f = mesh_world(p,spec,0)
        validate_manifold(v,f,p["name"])
        vertices+=len(v)
        polygons+=len(f)
        triangles+=sum(len(face)-2 for face in f)
        for axis in range(3):
            mins[axis]=min(mins[axis],*(q[axis] for q in v))
            maxs[axis]=max(maxs[axis],*(q[axis] for q in v))
    sweep=swept_collision_qa(spec)
    return {"model":spec["id"],"parts":len(names),"vertices":vertices,
            "polygons":polygons,"triangles":triangles,
            "materials":len(spec["materials"]),
            "bounds_min_m":[round(x,5) for x in mins],
            "bounds_max_m":[round(x,5) for x in maxs],
            "manifold_edges":"PASS","positive_winding":"PASS",
            **sweep,"blender_execution":"NOT_RUN","unity_import":"NOT_RUN"}


def write_static(spec,output):
    output.mkdir(parents=True,exist_ok=True)
    obj = ["# M05-006 directional barrier projector",
           "# metres; Z-up; front -Y; static neutral pose",
           "mtllib M05-006.mtl"]
    offset=1
    for p in spec["parts"]:
        verts,faces = mesh_world(p,spec)
        obj.extend(("o "+p["name"],"usemtl "+p["material"]))
        obj.extend("v {:.6f} {:.6f} {:.6f}".format(*v) for v in verts)
        obj.extend("f "+" ".join(str(offset+i) for i in f) for f in faces)
        offset+=len(verts)
    (output/"M05-006.obj").write_text("\n".join(obj)+"\n",encoding="utf-8")
    mtl=["# M05-006 materials"]
    for name,mat in spec["materials"].items():
        r,g,b,_ = mat["color"]
        mtl += ["newmtl "+name,"Ka 0.04 0.04 0.04",
                "Kd {:.5f} {:.5f} {:.5f}".format(r,g,b),
                "Ks 0.20 0.20 0.20",
                "Ns {:.1f}".format(20+120*mat["metallic"]),
                "d {:.3f}".format(mat.get("alpha",1.0)),
                "illum 2",""]
    (output/"M05-006.mtl").write_text("\n".join(mtl)+"\n",encoding="utf-8")


def make_blender(spec,output):
    import bpy
    collection_name="M05-006_ASSET"
    old=bpy.data.collections.get(collection_name)
    if old:
        for obj in tuple(old.objects):
            bpy.data.objects.remove(obj,do_unlink=True)
        bpy.data.collections.remove(old)
    # Remove orphaned datablocks left by an earlier generation, but NEVER
    # remove datablocks still referenced by the user's other objects.
    for mesh in tuple(bpy.data.meshes):
        if mesh.name.startswith("M05-006_") and mesh.users == 0:
            bpy.data.meshes.remove(mesh)
    for mat in tuple(bpy.data.materials):
        if mat.name.startswith("M05-006_") and mat.users == 0:
            bpy.data.materials.remove(mat)
    owned=bpy.data.collections.new(collection_name)
    bpy.context.scene.collection.children.link(owned)
    mats={}
    for name,info in spec["materials"].items():
        mat=bpy.data.materials.new("M05-006_"+name)
        mat.use_nodes=True
        bsdf=mat.node_tree.nodes.get("Principled BSDF")
        bsdf.inputs["Base Color"].default_value=tuple(info["color"])
        bsdf.inputs["Metallic"].default_value=float(info["metallic"])
        bsdf.inputs["Roughness"].default_value=float(info["roughness"])
        if "emission" in info:
            bsdf.inputs["Emission Color"].default_value=tuple(info["emission"])
            bsdf.inputs["Emission Strength"].default_value=float(info["emission_strength"])
        if "alpha" in info:
            bsdf.inputs["Alpha"].default_value=float(info["alpha"])
            if hasattr(mat,"surface_render_method"):
                mat.surface_render_method="DITHERED"
            elif hasattr(mat,"blend_method"):
                mat.blend_method="BLEND"
        mats[name]=mat
    root=bpy.data.objects.new("M05-006_MOUNT_ROOT",None)
    owned.objects.link(root)
    root.empty_display_type="PLAIN_AXES"
    root.empty_display_size=.12
    hinges={}
    for side in ("L","R"):
        info=spec["hinges"][side]
        obj=bpy.data.objects.new("M05-006_HINGE_"+side,None)
        owned.objects.link(obj)
        obj.parent=root
        obj.location=tuple(info["pivot"])
        obj.rotation_mode="XYZ"
        obj.empty_display_type="CIRCLE"
        obj.empty_display_size=.07
        hinges["arm_"+side]=obj
    for p in spec["parts"]:
        verts,faces=primitive(p)
        mesh=bpy.data.meshes.new("M05-006_"+p["name"]+"_mesh")
        mesh.from_pydata(verts,[],faces)
        mesh.update(calc_edges=True)
        obj=bpy.data.objects.new("M05-006_"+p["name"],mesh)
        owned.objects.link(obj)
        obj.parent=root if p["parent"]=="base" else hinges[p["parent"]]
        obj.data.materials.append(mats[p["material"]])
        for polygon in obj.data.polygons:
            polygon.use_smooth=False
        obj["model_id"]="M05-006"
        obj["mount_type"]="front"
        obj["kinematic_group"]=p["parent"]
    for side in ("L","R"):
        h=hinges["arm_"+side]
        info=spec["hinges"][side]
        for frame,angle in zip(info["frames"],info["angles_deg"]):
            h.rotation_euler[2]=math.radians(angle)
            h.keyframe_insert(data_path="rotation_euler",index=2,frame=frame)
        if h.animation_data and h.animation_data.action:
            action=h.animation_data.action
            if hasattr(action,"fcurves"):
                for curve in action.fcurves:
                    for key in curve.keyframe_points:
                        key.interpolation="LINEAR"
    scene=bpy.context.scene
    old_frame=scene.frame_current
    old_range=(scene.frame_start,scene.frame_end)
    old_selection=tuple(bpy.context.selected_objects)
    old_active=bpy.context.view_layer.objects.active
    output.mkdir(parents=True,exist_ok=True)
    try:
        scene.frame_start=1
        scene.frame_end=61
        scene.frame_set(1)
        bpy.context.view_layer.update()
        assert len(owned.objects)==len(spec["parts"])+3
        bpy.data.libraries.write(str(output/"M05-006.blend"),{owned},compress=True)
        bpy.ops.object.select_all(action="DESELECT")
        for obj in owned.objects:
            obj.select_set(True)
        bpy.context.view_layer.objects.active=root
        bpy.ops.export_scene.gltf(
            filepath=str(output/"M05-006_animated.glb"),
            export_format="GLB",use_selection=True,
            export_animations=True,export_apply=False)
    finally:
        bpy.ops.object.select_all(action="DESELECT")
        for obj in old_selection:
            if obj.name in bpy.data.objects:
                obj.select_set(True)
        if old_active and old_active.name in bpy.data.objects:
            bpy.context.view_layer.objects.active=old_active
        scene.frame_start,scene.frame_end=old_range
        scene.frame_set(old_frame)
    return {"blender_execution":"GENERATED_NOT_VISUALLY_REVIEWED",
            "blender_objects":len(owned.objects),
            "animated_glb":str(output/"M05-006_animated.glb"),
            "blender_library":str(output/"M05-006.blend")}


def main():
    argv=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else sys.argv[1:]
    parser=argparse.ArgumentParser()
    parser.add_argument("--output-dir",default=str(HERE/"generated"))
    parser.add_argument("--self-test",action="store_true")
    parser.add_argument("--require-blender",action="store_true")
    args=parser.parse_args(argv)
    spec=spec_load()
    report=validate(spec)
    if args.self_test:
        print(json.dumps(report,indent=2))
        return
    output=Path(args.output_dir).resolve()
    write_static(spec,output)
    try:
        import bpy
    except ImportError:
        if args.require_blender:
            raise RuntimeError("Blender bpy unavailable")
    else:
        report.update(make_blender(spec,output))
    (output/"M05-006.run-report.json").write_text(
        json.dumps(report,indent=2)+"\n",encoding="utf-8")
    print(json.dumps(report,indent=2))


if __name__=="__main__":
    main()
