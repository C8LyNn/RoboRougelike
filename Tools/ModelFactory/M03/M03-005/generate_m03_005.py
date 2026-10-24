#!/usr/bin/env python3
"""M03-005 universal octagonal adapter: procedural bpy + deterministic OBJ.

All dimensions are metres. X right, Y forward, Z up. Root (0,0,0) is a
real rear hub contact surface. The eight hollow front ports and flange rotate
together 360 degrees around the Y axis. This is visual-only asset generation.
"""
from __future__ import annotations
import argparse
import json
import math
import sys
from collections import defaultdict
from pathlib import Path

ID = "M03-005"
ROOT = Path(__file__).resolve().parent
TAU = math.tau


def read_spec():
    spec = json.loads((ROOT / "geometry.json").read_text(encoding="utf-8"))
    assert spec["id"] == ID and len(spec["parts"]) == 32
    assert len(spec["ports"]) == 8 and len(spec["palette"]) == 5
    assert spec["root_pivot"] == [0, 0, 0]
    return spec


def mesh_for(p):
    """Return vertices and outward-oriented faces of a closed analytic solid."""
    vertices, faces = [], []
    n = p["segments"]
    if p["shape"] == "cylinder":
        for y in (p["y0"], p["y1"]):
            for i in range(n):
                angle = TAU * i / n
                vertices.append((p["cx"] + p["r"] * math.cos(angle),
                                 y, p["cz"] + p["r"] * math.sin(angle)))
        vertices += [(p["cx"], p["y0"], p["cz"]),
                     (p["cx"], p["y1"], p["cz"])]
        for i in range(n):
            j = (i + 1) % n
            faces += [(i, n+i, n+j, j), (2*n, i, j),
                      (2*n+1, n+j, n+i)]
    else:
        full = "sweep" not in p
        count = n if full else n+1
        start = p.get("start", 0)
        sweep = p.get("sweep", TAU)
        for y, radius in ((p["y0"], p["outer"]), (p["y1"], p["outer"]),
                          (p["y0"], p["inner"]), (p["y1"], p["inner"])):
            for i in range(count):
                angle = start + sweep * i / n
                r = radius
                if p.get("octagonal") and radius == p["outer"]:
                    octant = TAU / 8
                    delta = (angle + octant/2) % octant - octant/2
                    r = radius * math.cos(octant/2) / math.cos(octant/2 - abs(delta))
                vertices.append((p["cx"] + r * math.cos(angle), y,
                                 p["cz"] + r * math.sin(angle)))
        a, b, c, d = 0, count, 2*count, 3*count
        for i in range(n):
            j = (i + 1) % count
            faces += [(a+i,b+i,b+j,a+j), (c+i,c+j,d+j,d+i),
                      (b+i,d+i,d+j,b+j), (a+i,a+j,c+j,c+i)]
        if not full:
            faces += [(a,c,d,b), (a+n,b+n,d+n,c+n)]
    return vertices, faces


def build_parts(spec):
    return [dict(p, vertices=mesh_for(p)[0], faces=mesh_for(p)[1])
            for p in spec["parts"]]


def cross(a, b):
    return (a[1]*b[2]-a[2]*b[1], a[2]*b[0]-a[0]*b[2],
            a[0]*b[1]-a[1]*b[0])


def sub(a, b):
    return tuple(x-y for x,y in zip(a,b))


def dot(a, b):
    return sum(x*y for x,y in zip(a,b))


def verify_manifold(part):
    vertices = part["vertices"]
    edges = defaultdict(list)
    volume = 0.0
    for face in part["faces"]:
        assert len(face) >= 3 and len(set(face)) == len(face), part["name"]
        assert all(0 <= i < len(vertices) for i in face)
        a,b,c = (vertices[i] for i in face[:3])
        assert dot(cross(sub(b,a),sub(c,a)),
                   cross(sub(b,a),sub(c,a))) > 1e-17, part["name"]
        for i in range(len(face)):
            a,b = face[i],face[(i+1)%len(face)]
            edges[(min(a,b),max(a,b))].append(1 if a<b else -1)
        for i in range(1,len(face)-1):
            a,b,c = (vertices[j] for j in (face[0],face[i],face[i+1]))
            volume += dot(a,cross(b,c))/6
    assert all(len(s)==2 and sum(s)==0 for s in edges.values()), part["name"]
    assert volume > 1e-8, (part["name"],volume)
    return volume


def radial_y_bounds(part):
    r = [math.hypot(v[0],v[2]) for v in part["vertices"]]
    y = [v[1] for v in part["vertices"]]
    return min(r),max(r),min(y),max(y)


def validate(spec, parts):
    assert len(parts)==32
    assert len({p["name"] for p in parts})==32
    assert set(p["material"] for p in parts)==set(spec["palette"])
    assert sum(p["group"]=="fixed" for p in parts)==7
    assert sum(p["group"]=="rotor" for p in parts)==25
    volumes=[verify_manifold(p) for p in parts]
    assert len(volumes)==32 and min(volumes)>0
    all_vertices=[v for p in parts for v in p["vertices"]]
    bounds=[[min(v[i] for v in all_vertices),
             max(v[i] for v in all_vertices)] for i in range(3)]
    assert abs(bounds[1][0])<1e-10 and abs(bounds[1][1]-.266)<1e-10
    assert max(abs(x) for b in (bounds[0],bounds[2]) for x in b)<.425
    for i,port in enumerate(spec["ports"]):
        assert port["name"]=="OCT_%02d"%i and port["parent"]=="rotor"
        assert port["normal"]==[0,1,0] and port["bore_radius"]==.027
        socket=next(p for p in parts if p["name"]=="Socket_%02d_through_bore"%i)
        radii=[math.hypot(v[0]-port["position"][0],
                          v[2]-port["position"][2]) for v in socket["vertices"]]
        assert abs(min(radii)-.027)<1e-8 and abs(max(radii)-.063)<1e-8
        assert math.hypot(port["position"][0],port["position"][2])-.027>.300
    fixed=[p for p in parts if p["group"]=="fixed"]
    moving=[p for p in parts if p["group"]=="rotor"]
    gap=float("inf")
    checks=0
    # Cylindrical radial and axial intervals are invariant under Y rotation.
    # Non-overlap on either axis is a conservative all-angle proof.
    for angle in range(0,361,5):
        for a in fixed:
            r0,r1,y0,y1=radial_y_bounds(a)
            for b in moving:
                s0,s1,z0,z1=radial_y_bounds(b)
                axial=max(z0-y1,y0-z1)
                radial=max(s0-r1,r0-s1)
                assert axial>1e-8 or radial>1e-8, (a["name"],b["name"],angle)
                gap=min(gap,max(axial,radial))
                checks+=1
    assert checks==12775
    return dict(id=ID,status="STATIC_GEOMETRY_PASS",
                parts=32,fixed_parts=7,rotating_parts=25,
                vertices=sum(len(p["vertices"]) for p in parts),
                polygon_faces=sum(len(p["faces"]) for p in parts),
                triangles=sum(sum(len(face)-2 for face in p["faces"]) for p in parts),
                closed_manifold_parts=32,positive_volumes=32,
                materials=list(spec["palette"]),ports=spec["ports"],
                bounds_xyz_m=[[round(x,6) for x in b] for b in bounds],
                rear_mount_pivot=[0,0,0],rotation_axis="+Y",
                rotation_range_deg=[0,360],poses_checked=73,
                base_rotor_pair_checks=checks,
                minimum_conservative_clearance_m=round(gap,6),
                collision_result="PASS_ALL_ANGLES_BY_RADIAL_AXIAL_SEPARATION",
                same_group_welds="INTENTIONAL: socket-to-flange, hub-to-plate, trim-to-socket",
                blender="NOT RUN",unity="NOT RUN")


def export_obj(spec, parts, out):
    out.mkdir(parents=True,exist_ok=True)
    with (out/(ID+".mtl")).open("w",encoding="utf-8",newline="\n") as f:
        f.write("# M03-005 octagonal adapter, five metallic/ceramic materials\n")
        for name,(rgb,metal,rough) in spec["palette"].items():
            f.write("newmtl "+name+"\n")
            f.write("Kd "+" ".join("%.6f"%x for x in rgb)+"\n")
            f.write("Pm %.6f\nPr %.6f\n\n"%(metal,rough))
    with (out/(ID+".obj")).open("w",encoding="utf-8",newline="\n") as f:
        f.write("# M03-005 eight-socket universal octagonal adapter, 1 unit = 1 metre\n")
        f.write("# Root pivot: physical solid rear hub face (0,0,0), +Y forward\n")
        f.write("mtllib M03-005.mtl\n")
        offset=1
        for p in parts:
            f.write("o "+p["name"]+"\nusemtl "+p["material"]+"\n")
            for v in p["vertices"]:
                f.write("v "+" ".join("%.6f"%x for x in v)+"\n")
            for face in p["faces"]:
                f.write("f "+" ".join(str(offset+i) for i in face)+"\n")
            offset+=len(p["vertices"])


def export_blender(spec,parts,out):
    import bpy
    name=ID+"_ASSET"
    old=bpy.data.collections.get(name)
    if old:
        meshes=[o.data for o in old.objects if o.type=="MESH"]
        for ob in tuple(old.objects):
            bpy.data.objects.remove(ob,do_unlink=True)
        bpy.data.collections.remove(old)
        for mesh in meshes:
            if mesh.users==0:
                bpy.data.meshes.remove(mesh)
    for action in tuple(bpy.data.actions):
        if action.name.startswith(ID+"_FLANGE_360_Y") and action.users==0:
            bpy.data.actions.remove(action)
    col=bpy.data.collections.new(name)
    bpy.context.scene.collection.children.link(col)
    materials={}
    for key,(rgb,metal,rough) in spec["palette"].items():
        mat=bpy.data.materials.get(ID+"_"+key) or bpy.data.materials.new(ID+"_"+key)
        mat.diffuse_color=(*rgb,1)
        mat.use_nodes=True
        bsdf=mat.node_tree.nodes.get("Principled BSDF")
        if bsdf:
            bsdf.inputs["Base Color"].default_value=(*rgb,1)
            bsdf.inputs["Metallic"].default_value=metal
            bsdf.inputs["Roughness"].default_value=rough
        materials[key]=mat
    root=bpy.data.objects.new(ID+"_REAR_FRAME_MOUNT",None)
    col.objects.link(root)
    root.empty_display_type="PLAIN_AXES"
    root["mount"]="physical rear hub face at (0,0,0), +Y forward"
    rotor=bpy.data.objects.new(ID+"_ROTATING_FLANGE",None)
    col.objects.link(rotor)
    rotor.parent=root
    rotor.rotation_mode="XYZ"
    rotor["rotation_axis"]="+Y"
    for port in spec["ports"]:
        marker=bpy.data.objects.new(ID+"_PORT_"+port["name"],None)
        col.objects.link(marker)
        marker.parent=rotor
        marker.location=port["position"]
        marker.empty_display_type="CIRCLE"
        marker.empty_display_size=.028
        marker["normal_xyz"]=port["normal"]
        marker["bore_radius_m"]=port["bore_radius"]
        marker["port_type"]="OCTAGON_FRAME_SOCKET"
    for p in parts:
        mesh=bpy.data.meshes.new(ID+"_"+p["name"]+"_mesh")
        mesh.from_pydata(p["vertices"],[],p["faces"])
        mesh.update(calc_edges=True)
        obj=bpy.data.objects.new(ID+"_"+p["name"],mesh)
        col.objects.link(obj)
        obj.parent=rotor if p["group"]=="rotor" else root
        obj.data.materials.append(materials[p["material"]])
        obj["group"]=p["group"]
        bevel=obj.modifiers.new("Machined_edges_2mm","BEVEL")
        bevel.width=.002
        bevel.segments=2
        bevel.limit_method="ANGLE"
        bevel.angle_limit=math.radians(30)
        normals=obj.modifiers.new("Weighted_face_normals","WEIGHTED_NORMAL")
        normals.keep_sharp=True
    for frame,angle in spec["rotation_keyframes"]:
        rotor.rotation_euler=(0,math.radians(angle),0)
        rotor.keyframe_insert(data_path="rotation_euler",frame=frame)
    if rotor.animation_data and rotor.animation_data.action:
        action=rotor.animation_data.action
        action.name=ID+"_FLANGE_360_Y"
        if hasattr(action,"fcurves"):
            for curve in action.fcurves:
                for key in curve.keyframe_points:
                    key.interpolation="LINEAR"
    out.mkdir(parents=True,exist_ok=True)
    scene=bpy.context.scene
    previous=(scene.frame_current,scene.frame_start,scene.frame_end)
    selected=tuple(bpy.context.selected_objects)
    active=bpy.context.view_layer.objects.active
    try:
        scene.frame_start=1
        scene.frame_end=181
        scene.frame_set(1)
        bpy.context.view_layer.update()
        assert len(col.objects)==42
        bpy.data.libraries.write(str(out/(ID+".blend")),{col},compress=True)
        bpy.ops.object.select_all(action="DESELECT")
        for obj in col.objects:
            obj.select_set(True)
        bpy.context.view_layer.objects.active=root
        result=bpy.ops.export_scene.gltf(
            filepath=str(out/(ID+".glb")),export_format="GLB",
            use_selection=True,export_animations=True,
            export_yup=True,export_apply=False)
        assert "FINISHED" in result and (out/(ID+".glb")).stat().st_size>4096
    finally:
        bpy.ops.object.select_all(action="DESELECT")
        for obj in selected:
            if obj.name in bpy.data.objects:
                obj.select_set(True)
        if active and active.name in bpy.data.objects:
            bpy.context.view_layer.objects.active=active
        scene.frame_start,scene.frame_end=previous[1:]
        scene.frame_set(previous[0])
    return [ID+".blend",ID+".glb"]


def main():
    argv=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else sys.argv[1:]
    ap=argparse.ArgumentParser()
    ap.add_argument("--output-dir",default=str(ROOT))
    ap.add_argument("--self-test",action="store_true")
    ap.add_argument("--require-blender",action="store_true")
    args=ap.parse_args(argv)
    out=Path(args.output_dir).resolve()
    spec=read_spec()
    parts=build_parts(spec)
    qa=validate(spec,parts)
    export_obj(spec,parts,out)
    if not args.self_test:
        try:
            import bpy
        except ImportError:
            if args.require_blender:
                raise RuntimeError("bpy unavailable: Blender export NOT RUN")
        else:
            qa["blender_files"]=export_blender(spec,parts,out)
            qa["blender"]="EXECUTED"
    (out/(ID+".static-qa.json")).write_text(
        json.dumps(qa,indent=2,ensure_ascii=False)+"\n",encoding="utf-8")
    print(json.dumps(qa,indent=2,ensure_ascii=False))


if __name__=="__main__":
    main()
