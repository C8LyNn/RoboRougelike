#!/usr/bin/env python3
"""M05-005: deterministic procedural ring shield generator (visual asset only).

Python 3: --self-test, or --output-dir DIR to generate OBJ/MTL/QA.
Blender: blender -b --python generate_m05_005.py -- --output-dir DIR
One unit = one metre; source Blender Z-up; mount pivot at (0,0,0).
No gameplay, shield or damage mechanics.
"""
from __future__ import annotations

import argparse
import json
import math
import sys
from collections import Counter
from pathlib import Path

HERE = Path(__file__).resolve().parent
TAU = 2.0 * math.pi


def spec_load():
    data = json.loads((HERE / "geometry.json").read_text(encoding="utf-8"))
    if data["id"] != "M05-005":
        raise ValueError("Incorrect model identifier")
    return data


def mesh_for(part, pivot=(0, 0, 0.36), world=True):
    """Produce an outward-wound, closed manifold mesh, no bpy dependency."""
    shape = part["shape"]
    v, f = [], []
    if shape == "box":
        x, y, z = [a / 2.0 for a in part["size"]]
        v = [(-x,-y,-z),(x,-y,-z),(x,y,-z),(-x,y,-z),
             (-x,-y,z),(x,-y,z),(x,y,z),(-x,y,z)]
        f = [(3,2,1,0),(4,5,6,7),(0,1,5,4),(1,2,6,5),
             (2,3,7,6),(3,0,4,7)]
    elif shape == "cylinder":
        n, r, h = part["segments"], part["radius"], part["depth"] / 2
        for z in (-h, h):
            v.extend((r*math.cos(TAU*i/n),r*math.sin(TAU*i/n),z)
                     for i in range(n))
        f = [tuple(reversed(range(n))),tuple(range(n,2*n))]
        f += [(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    elif shape == "torus":
        n, m = part["major_segments"], part["minor_segments"]
        R, r = part["major_radius"], part["tube_radius"]
        for i in range(n):
            theta = TAU*i/n
            for j in range(m):
                phi = TAU*j/m
                radial = R + r*math.cos(phi)
                v.append((radial*math.cos(theta),radial*math.sin(theta),
                          r*math.sin(phi)))
        f = [(i*m+j,((i+1)%n)*m+j,
              ((i+1)%n)*m+(j+1)%m,i*m+(j+1)%m)
             for i in range(n) for j in range(m)]
    elif shape == "annulus":
        n = part["segments"]
        ri, ro, h = part["inner_radius"],part["outer_radius"],part["depth"]/2
        for z,r in [(-h,ri),(-h,ro),(h,ri),(h,ro)]:
            v.extend((r*math.cos(TAU*i/n),r*math.sin(TAU*i/n),z)
                     for i in range(n))
        for i in range(n):
            k=(i+1)%n
            f.extend(((i,k,n+k,n+i),
                      (3*n+i,3*n+k,2*n+k,2*n+i),
                      (n+i,n+k,3*n+k,3*n+i),
                      (i,2*n+i,2*n+k,k)))
    elif shape == "crystal":
        n,r,h=part["segments"],part["radius"],part["half_height"]
        v=[(0,0,-h),(0,0,h)]
        v.extend((r*math.cos(TAU*i/n),r*math.sin(TAU*i/n),0)
                 for i in range(n))
        for i in range(n):
            a,b=2+i,2+(i+1)%n
            f.extend(((1,a,b),(0,b,a)))
    else:
        raise ValueError("Unsupported shape: " + shape)
    ox,oy,oz=part["center"]
    if world and part["parent"]=="core":
        ox+=pivot[0];oy+=pivot[1];oz+=pivot[2]
    v=[(x+ox,y+oy,z+oz) for x,y,z in v]
    return v,f


def signed_volume(v, faces):
    total=0.0
    for face in faces:
        a=v[face[0]]
        for i in range(1,len(face)-1):
            b,c=v[face[i]],v[face[i+1]]
            total+=(a[0]*(b[1]*c[2]-b[2]*c[1])
                   +a[1]*(b[2]*c[0]-b[0]*c[2])
                   +a[2]*(b[0]*c[1]-b[1]*c[0]))/6.0
    return total


def validate(data):
    assert data["mount_origin"]==[0,0,0]
    assert data["core_pivot"]==[0,0,0.36]
    assert len(data["materials"])>=3
    assert len(data["parts"])==47
    names=[p["name"] for p in data["parts"]]
    assert len(set(names))==len(names)
    assert [f for f,s in data["core_scale_keyframes"]]==[1,16,31,46,61]
    assert all(0.85<=s<=1.05 for _,s in data["core_scale_keyframes"])
    vertices=polygons=triangles=0
    min_xyz=[float("inf")]*3
    max_xyz=[float("-inf")]*3
    core=next(p for p in data["parts"] if p["name"]=="pulse_crystal")
    assert core["parent"]=="core"
    core_max_radius=core["radius"]*1.04
    core_z_lo=data["core_pivot"][2]-core["half_height"]*1.04
    core_z_hi=data["core_pivot"][2]+core["half_height"]*1.04
    min_radial_gap=float("inf")
    for p in data["parts"]:
        assert p["material"] in data["materials"],p["name"]
        assert p["parent"] in ("root","core")
        v,faces=mesh_for(p,data["core_pivot"])
        assert signed_volume(v,faces)>1e-9,("winding",p["name"])
        edges=Counter()
        for face in faces:
            assert len(face)>=3 and len(set(face))==len(face)
            for i in face:
                assert 0<=i<len(v)
            for a,b in zip(face,face[1:]+face[:1]):
                edges[tuple(sorted((a,b)))]+=1
            for i in range(1,len(face)-1):
                a,b,c=v[face[0]],v[face[i]],v[face[i+1]]
                ab=[b[k]-a[k] for k in range(3)]
                ac=[c[k]-a[k] for k in range(3)]
                cross=(ab[1]*ac[2]-ab[2]*ac[1],
                       ab[2]*ac[0]-ab[0]*ac[2],
                       ab[0]*ac[1]-ab[1]*ac[0])
                assert sum(x*x for x in cross)>1e-19,("degenerate",p["name"])
                triangles+=1
        assert all(count==2 for count in edges.values()),("nonmanifold",p["name"])
        vertices+=len(v);polygons+=len(faces)
        for axis in range(3):
            min_xyz[axis]=min(min_xyz[axis],min(x[axis] for x in v))
            max_xyz[axis]=max(max_xyz[axis],max(x[axis] for x in v))
        if p["parent"]=="root":
            z_min=min(q[2] for q in v)
            z_max=max(q[2] for q in v)
            if z_max<=core_z_lo or z_min>=core_z_hi:
                continue
            # All objects at the crystal's height are hollow annuli or
            # outward cage members. Their *closest mesh vertex* bounds
            # the radial separation; boxes are axis aligned.
            radial=min(math.hypot(q[0],q[1]) for q in v)
            gap=radial-core_max_radius
            min_radial_gap=min(min_radial_gap,gap)
            assert gap>=0.035,("crystal collision",p["name"],gap)
    assert abs(min_xyz[2])<1e-8
    assert max_xyz[2]<0.65
    assert min_xyz[0]>=-0.5 and max_xyz[0]<=0.5
    assert min_xyz[1]>=-0.5 and max_xyz[1]<=0.5
    assert min_radial_gap>=0.035
    return {
        "model":data["id"],"parts":len(names),"vertices":vertices,
        "polygons":polygons,"triangles":triangles,
        "materials":len(data["materials"]),"manifold_edges":"PASS",
        "positive_winding":"PASS","degenerate_triangles":0,
        "mount_origin_m":data["mount_origin"],
        "core_pivot_m":data["core_pivot"],
        "pulse_frames":[1,16,31,46,61],
        "core_scale_range":[0.90,1.04],
        "minimum_core_radial_gap_m":round(min_radial_gap,5),
        "bounding_min_m":[round(x,5) for x in min_xyz],
        "bounding_max_m":[round(x,5) for x in max_xyz],
        "blender_execution":"NOT_RUN","animated_glb":"NOT_RUN",
        "unity_import":"NOT_RUN","visual_review":"NOT_RUN",
    }


def export_obj(data,destination):
    destination.mkdir(parents=True,exist_ok=True)
    obj=destination/"M05-005.obj"
    mtl=destination/"M05-005.mtl"
    lines=["# M05-005 ring shield generator: visual asset, no gameplay",
           "# Blender Z-up, 1 unit = 1 metre; neutral core scale 1",
           "mtllib M05-005.mtl"]
    offset=1
    for p in data["parts"]:
        v,faces=mesh_for(p,data["core_pivot"])
        lines.extend(("o "+p["name"],"usemtl "+p["material"]))
        lines.extend("v "+" ".join("%.6f"%c for c in vert) for vert in v)
        lines.extend("f "+" ".join(str(offset+i) for i in face)
                     for face in faces)
        offset+=len(v)
    obj.write_text("\n".join(lines)+"\n",encoding="utf-8")
    mats=["# M05-005 material palette"]
    for name,material in data["materials"].items():
        r,g,b,a=material["rgba"]
        mats.extend(("newmtl "+name,"Ka 0.05 0.05 0.05",
                     "Kd %.5f %.5f %.5f"%(r,g,b),
                     "Ks 0.25 0.25 0.25",
                     "Ns %.1f"%(30+100*material["metallic"]),
                     "d %.5f"%a,"illum 2",""))
    mtl.write_text("\n".join(mats)+"\n",encoding="utf-8")
    return obj,mtl


def blender_scene(data,destination):
    import bpy
    name="M05-005_ASSET"
    old=bpy.data.collections.get(name)
    if old is not None:
        for obj in tuple(old.objects):
            bpy.data.objects.remove(obj,do_unlink=True)
        bpy.data.collections.remove(old)
    collection=bpy.data.collections.new(name)
    bpy.context.scene.collection.children.link(collection)
    for mat in tuple(bpy.data.materials):
        if mat.name.startswith("M05-005_") and mat.users==0:
            bpy.data.materials.remove(mat)

    materials={}
    for key,info in data["materials"].items():
        mat=bpy.data.materials.new("M05-005_"+key)
        mat.use_nodes=True
        bsdf=mat.node_tree.nodes.get("Principled BSDF")
        bsdf.inputs["Base Color"].default_value=tuple(info["rgba"])
        bsdf.inputs["Metallic"].default_value=info["metallic"]
        bsdf.inputs["Roughness"].default_value=info["roughness"]
        if "emission" in info:
            emission=bsdf.inputs.get("Emission Color") or bsdf.inputs.get("Emission")
            if emission is not None:
                emission.default_value=tuple(info["emission"])
            strength=bsdf.inputs.get("Emission Strength")
            if strength is not None:
                strength.default_value=info["strength"]
        if info["rgba"][3]<1:
            if hasattr(mat,"surface_render_method"):
                mat.surface_render_method="DITHERED"
            elif hasattr(mat,"blend_method"):
                mat.blend_method="BLEND"
            bsdf.inputs["Alpha"].default_value=info["rgba"][3]
        materials[key]=mat

    root=bpy.data.objects.new("M05-005_MOUNT_ROOT",None)
    collection.objects.link(root)
    root.empty_display_type="PLAIN_AXES"
    root.empty_display_size=0.12
    core=bpy.data.objects.new("M05-005_CORE_PULSE",None)
    collection.objects.link(core)
    core.parent=root
    core.location=tuple(data["core_pivot"])
    core.empty_display_type="SPHERE"
    core.empty_display_size=0.10

    for part in data["parts"]:
        v,faces=mesh_for(part,data["core_pivot"],world=False)
        mesh=bpy.data.meshes.new("M05-005_"+part["name"]+"_mesh")
        mesh.from_pydata(v,[],faces)
        mesh.update(calc_edges=True)
        obj=bpy.data.objects.new("M05-005_"+part["name"],mesh)
        collection.objects.link(obj)
        obj.parent=core if part["parent"]=="core" else root
        mesh.materials.append(materials[part["material"]])
        for poly in mesh.polygons:
            # Smooth round sidewalls, but keep the cylinder caps and all
            # planar hard-surface boundaries crisp (no shading bleed).
            poly.use_smooth=(part["shape"]=="torus" or
                             (part["shape"]=="cylinder" and len(poly.vertices)==4))
        obj["model_id"]="M05-005"
        obj["mount_type"]="internal_frame"
        obj["group"]=part["parent"]

    for frame,scale in data["core_scale_keyframes"]:
        core.scale=(scale,scale,scale)
        core.keyframe_insert(data_path="scale",frame=frame)
    if core.animation_data and core.animation_data.action:
        action=core.animation_data.action
        if hasattr(action,"fcurves"):
            for curve in action.fcurves:
                for key in curve.keyframe_points:
                    key.interpolation="BEZIER"
    scene=bpy.context.scene
    saved=(scene.frame_current,scene.frame_start,scene.frame_end,
           tuple(bpy.context.selected_objects),bpy.context.view_layer.objects.active)
    destination.mkdir(parents=True,exist_ok=True)
    try:
        scene.frame_start=1
        scene.frame_end=61
        scene.frame_set(1)
        bpy.context.view_layer.update()
        assert len(collection.objects)==len(data["parts"])+2
        bpy.data.libraries.write(str(destination/"M05-005.blend"),
                                 {collection},compress=True)
        bpy.ops.object.select_all(action="DESELECT")
        for obj in collection.objects:
            obj.select_set(True)
        bpy.context.view_layer.objects.active=root
        bpy.ops.export_scene.gltf(filepath=str(destination/"M05-005_animated.glb"),
                                  export_format="GLB",use_selection=True,
                                  export_animations=True,export_apply=False)
    finally:
        bpy.ops.object.select_all(action="DESELECT")
        for obj in saved[3]:
            if obj.name in bpy.data.objects:
                obj.select_set(True)
        if saved[4] is not None and saved[4].name in bpy.data.objects:
            bpy.context.view_layer.objects.active=saved[4]
        scene.frame_start=saved[1]
        scene.frame_end=saved[2]
        scene.frame_set(saved[0])
    return ["M05-005.blend","M05-005_animated.glb"]


def main():
    argv=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else sys.argv[1:]
    parser=argparse.ArgumentParser()
    parser.add_argument("--output-dir",default=str(HERE/"generated"))
    parser.add_argument("--self-test",action="store_true")
    args=parser.parse_args(argv)
    data=spec_load()
    report=validate(data)
    if args.self_test:
        print(json.dumps(report,indent=2,ensure_ascii=False))
        return
    destination=Path(args.output_dir).resolve()
    export_obj(data,destination)
    try:
        import bpy
    except ImportError:
        pass
    else:
        blender_scene(data,destination)
        report["blender_execution"]="GENERATED_REQUIRES_VISUAL_QA"
        report["animated_glb"]="GENERATED_REQUIRES_IMPORT_QA"
    (destination/"M05-005.run-report.json").write_text(
        json.dumps(report,indent=2,ensure_ascii=False)+"\n",encoding="utf-8")
    print(json.dumps(report,indent=2,ensure_ascii=False))


if __name__=="__main__":
    main()
