#!/usr/bin/env python3
"""M04-006: single wide flat-slot afterburner with five hinged exhaust louvers.

Plain Python:
  python3 generate_m04_006.py --self-test --output-dir /tmp/m04-006
Blender 3.4+:
  blender -b --factory-startup --python generate_m04_006.py -- --output-dir /tmp/m04-006 --require-blender

Blender Z-up; +Y points into the vehicle; exhaust points toward -Y.
Mount root at (0,0,0), on the rear vehicle frame plane. 1 unit = 1 metre.
Five horizontal louvers pivot about X at y=-0.705, z=-.20,-.10,0,.10,.20.
This is visual-only; no new Unity mechanics, thrust or effects scripts.
"""
from __future__ import annotations

import argparse
import json
import math
import sys
from collections import Counter
from pathlib import Path

HERE = Path(__file__).resolve().parent
MODEL = "M04-006"


def read_spec():
    spec = json.loads((HERE / "geometry.json").read_text(encoding="utf-8"))
    assert spec["id"] == MODEL and spec["mount"] == "rear frame"
    assert spec["mount_origin_m"] == [0, 0, 0]
    return spec


def signed_volume(vertices, faces):
    total = 0.0
    for face in faces:
        a = vertices[face[0]]
        for i in range(1, len(face) - 1):
            b, c = vertices[face[i]], vertices[face[i + 1]]
            total += (
                a[0] * (b[1]*c[2] - b[2]*c[1]) +
                a[1] * (b[2]*c[0] - b[0]*c[2]) +
                a[2] * (b[0]*c[1] - b[1]*c[0])
            ) / 6.0
    return total


def orient(vertices, faces):
    vol = signed_volume(vertices, faces)
    assert abs(vol) > 1e-10, "Degenerate closed mesh"
    if vol < 0:
        faces = [tuple(reversed(f)) for f in faces]
    return vertices, faces


def box(center, size):
    x, y, z = (v / 2 for v in size)
    vertices = [
        (-x,-y,-z), (x,-y,-z), (x,y,-z), (-x,y,-z),
        (-x,-y,z), (x,-y,z), (x,y,z), (-x,y,z)
    ]
    faces = [
        (3,2,1,0), (4,5,6,7), (0,1,5,4),
        (1,2,6,5), (2,3,7,6), (3,0,4,7)
    ]
    vertices = [tuple(v[i] + center[i] for i in range(3)) for v in vertices]
    return orient(vertices, faces)


def extrude_x(name, profile_yz, width, center_x=0.0):
    """Watertight extruded six/eight-sided aerodynamic cross-section, no ngons at sides."""
    assert width > 0 and len(profile_yz) >= 3
    n = len(profile_yz)
    vertices = [(center_x - width/2, y, z) for y,z in profile_yz]
    vertices += [(center_x + width/2, y, z) for y,z in profile_yz]
    faces = [tuple(reversed(range(n))), tuple(range(n, 2*n))]
    faces += [(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    return orient(vertices, faces)


def cylinder_x(center, radius, length, segments=20):
    assert radius > 0 and length > 0 and segments >= 8
    verts = []
    for side in (-1,1):
        for i in range(segments):
            a = math.tau*i/segments
            verts.append((center[0] + side*length/2,
                          center[1] + radius*math.cos(a),
                          center[2] + radius*math.sin(a)))
    faces = [tuple(reversed(range(segments))),tuple(range(segments,2*segments))]
    for i in range(segments):
        j = (i+1)%segments
        faces.append((i,j,j+segments,i+segments))
    return orient(verts, faces)


def build(spec):
    parts = []
    def add(name, material, geometry, **extra):
        v, f = geometry
        parts.append(dict(name=name, material=material,
                          vertices=v, faces=f, **extra))

    # Rear-facing vehicle mounting cassette, mechanically distinct from M04-005.
    add("mount_rubber_isolator", "rubber", box((0,-.012,0),(1.49,.024,.72)))
    add("mount_bulkhead", "graphite", box((0,-.084,0),(1.55,.124,.76)))
    add("mount_upper_rib", "titanium", box((0,-.154,.335),(1.38,.18,.09)))
    add("mount_lower_rib", "titanium", box((0,-.154,-.335),(1.38,.18,.09)))
    for sx in (-1,1):
        for sz in (-1,1):
            add("mount_fastener_%s_%s"%(sx,sz), "copper",
                cylinder_x((sx*.71,-.157,sz*.26),.025,.032,16))

    # Broad wedge shrouds with actual beveled cross-sections; central slot remains open.
    top_profile = [
        (-.17,.29),(-.25,.365),(-.60,.375),(-.755,.325),
        (-.755,.282),(-.61,.264),(-.28,.267)
    ]
    bottom_profile = [(y,-z) for y,z in reversed(top_profile)]
    add("upper_wedge_armor","ceramic",extrude_x("upper",top_profile,1.40))
    add("lower_wedge_armor","ceramic",extrude_x("lower",bottom_profile,1.40))
    add("upper_heat_shield","graphite",box((0,-.43,.389),(1.23,.36,.035)))
    add("lower_heat_shield","graphite",box((0,-.43,-.389),(1.23,.36,.035)))
    for sx in (-1,1):
        add("side_duct_wall_%s"%sx,"graphite",
            box((sx*.722,-.452,0),(.105,.545,.68)))
        add("side_ridge_%s"%sx,"titanium",
            box((sx*.780,-.45,0),(.033,.41,.52)))
        add("side_front_lock_%s"%sx,"copper",
            box((sx*.782,-.228,.22),(.041,.09,.08)))
        add("side_rear_lock_%s"%sx,"copper",
            box((sx*.782,-.676,-.22),(.041,.09,.08)))

    # Narrow rear aperture and recessed dual-color thermodynamic readout.
    add("rear_lip_top","titanium",box((0,-.752,.32),(1.46,.038,.052)))
    add("rear_lip_bottom","titanium",box((0,-.752,-.32),(1.46,.038,.052)))
    add("inner_heat_upper","copper",box((0,-.618,.258),(1.18,.034,.015)))
    add("inner_heat_lower","copper",box((0,-.618,-.258),(1.18,.034,.015)))
    for sx in (-1,1):
        add("rear_vertical_bezel_%s"%sx,"titanium",
            box((sx*.643,-.747,0),(.040,.042,.64)))
        add("recessed_ion_trace_%s"%sx,"ion_cyan",
            box((sx*.612,-.635,0),(.012,.014,.45)))
    for sz in (-1,1):
        add("inner_amber_trace_%s"%sz,"amber",
            box((0,-.634,sz*.246),(.88,.016,.009)))
        add("heat_spine_%s"%sz,"graphite",
            box((0,-.47,sz*.268),(1.12,.28,.022)))
    for i in range(9):
        x = (i-4)*.137
        add("upper_copper_fin_%02d"%i,"copper",
            box((x,-.43,.422),(.033,.235,.032)))
        add("lower_copper_fin_%02d"%i,"copper",
            box((x,-.43,-.422),(.033,.235,.032)))

    # Side-mounted hinge pins are outside the moving vane envelope.
    for i,z in enumerate(spec["louver"]["centers_z_m"]):
        for sx in (-1,1):
            add("louver_pin_%d_%s"%(i,sx),"copper",
                cylinder_x((sx*.665,-.705,z),.019,.050,20))
        profile = [
            (-.705,z-.008),(-.834,z-.012),(-.855,z-.006),
            (-.855,z+.006),(-.834,z+.012),(-.705,z+.008)
        ]
        add("animated_louver_%d"%i,"ceramic",
            extrude_x("louver",profile,spec["louver"]["width_m"]),
            moving=True,pivot=(0.0,-.705,z),axis=(1.0,0.0,0.0))
        # A separate dark stripe at the exhaust tip travels with each vane.
        tip = [
            (-.836,z-.007),(-.851,z-.005),(-.851,z+.005),
            (-.836,z+.007)
        ]
        add("animated_louver_tip_%d"%i,"graphite",
            extrude_x("tip",tip,1.07),
            moving=True,pivot=(0.0,-.705,z),axis=(1.0,0.0,0.0))

    add("status_left","ion_cyan",box((-.59,-.153,.32),(.10,.013,.014)))
    add("status_right","amber",box((.59,-.153,.32),(.10,.013,.014)))
    names = [p["name"] for p in parts]
    assert len(names) == len(set(names)), "Duplicate mesh name"
    return parts


def rotated_vertices(part, angle_degrees):
    if not part.get("moving"):
        return part["vertices"]
    px,py,pz = part["pivot"]
    a = math.radians(angle_degrees)
    cs,sn = math.cos(a),math.sin(a)
    return [(x,py+(y-py)*cs-(z-pz)*sn,pz+(y-py)*sn+(z-pz)*cs)
            for x,y,z in part["vertices"]]


def bounds(vertices):
    return [(min(v[i] for v in vertices),max(v[i] for v in vertices))
            for i in range(3)]


def overlap_volume(a,b):
    return math.prod(max(0,min(a[i][1],b[i][1])-max(a[i][0],b[i][0]))
                     for i in range(3))


def verify_mesh(part):
    v, f = part["vertices"], part["faces"]
    edges = Counter()
    triangles = 0
    for face in f:
        assert len(face) >= 3 and len(set(face)) == len(face), part["name"]
        for i,j in zip(face,face[1:]+face[:1]):
            assert 0 <= i < len(v) and 0 <= j < len(v)
            edges[tuple(sorted((i,j)))] += 1
        a = v[face[0]]
        for k in range(1,len(face)-1):
            b,c = v[face[k]],v[face[k+1]]
            ab = [b[i]-a[i] for i in range(3)]
            ac = [c[i]-a[i] for i in range(3)]
            cross = (ab[1]*ac[2]-ab[2]*ac[1],
                     ab[2]*ac[0]-ab[0]*ac[2],
                     ab[0]*ac[1]-ab[1]*ac[0])
            assert sum(x*x for x in cross)>1e-16,(part["name"],face)
            triangles += 1
    assert all(c==2 for c in edges.values()),part["name"]
    assert signed_volume(v,f)>1e-10,part["name"]
    return triangles


def qa(spec,parts):
    moving = [p for p in parts if p.get("moving")]
    fixed = [p for p in parts if not p.get("moving")]
    assert len(moving)==10 and len(spec["materials"])>=3
    assert spec["louver"]["angles_deg"] == [0,18,0,-8,0]
    assert spec["louver"]["frames"] == [1,20,40,60,80]
    assert all(p["material"] in spec["materials"] for p in parts)
    triangles = sum(verify_mesh(p) for p in parts)
    checks = 0
    fixed_boxes = [(p["name"],bounds(p["vertices"])) for p in fixed]
    for angle in range(-8,19):
        moving_boxes = [(p["name"],bounds(rotated_vertices(p,angle)))
                        for p in moving]
        for name,m in moving_boxes:
            for fname,f in fixed_boxes:
                volume = overlap_volume(m,f)
                assert volume < 1e-12,(angle,name,fname,volume)
                checks += 1
        # Two pieces belonging to the SAME vane intentionally overlap at their
        # decorative tip; distinct vanes must never touch at any angle.
        for i,(name,m) in enumerate(moving_boxes):
            for other,o in moving_boxes[i+1:]:
                if name.rsplit("_",1)[-1] == other.rsplit("_",1)[-1]:
                    continue
                volume = overlap_volume(m,o)
                assert volume < 1e-12,(angle,name,other,volume)
                checks += 1
    return {
        "id":MODEL, "mount":"rear_frame", "units":"metres",
        "parts":len(parts),"fixed_parts":len(fixed),"animated_parts":len(moving),
        "vertices":sum(len(p["vertices"]) for p in parts),
        "triangles":triangles,"materials":len(spec["materials"]),
        "closed_manifold_meshes":"PASS","outward_winding":"PASS",
        "sweep_angles_deg":[-8,18],"sweep_samples":27,
        "aabb_pairs_checked":checks,"moving_vs_static_and_other_vanes":"PASS",
        "blender":"NOT_RUN","animated_glb":"NOT_RUN","unity_import":"NOT_RUN",
        "visual_review":"NOT_RUN"
    }


def write_static(spec,parts,out):
    out.mkdir(parents=True,exist_ok=True)
    lines=["# M04-006 — single wide flat-slot exhaust, rest pose",
           "# Blender Z up; 1 unit = 1 metre; mounting plane y=0",
           "mtllib M04-006.mtl"]
    offset=1
    for p in parts:
        lines += ["o "+p["name"],"usemtl "+p["material"]]
        lines += ["v %.6f %.6f %.6f"%v for v in p["vertices"]]
        lines += ["f "+" ".join(str(offset+i) for i in f) for f in p["faces"]]
        offset += len(p["vertices"])
    (out/"M04-006.obj").write_text("\n".join(lines)+"\n",encoding="utf-8")
    mtl=["# M04-006 seven visual materials"]
    for name,info in spec["materials"].items():
        r,g,b,_ = info["color"]
        mtl += ["newmtl "+name,"Ka 0.04 0.04 0.04",
                "Kd %.5f %.5f %.5f"%(r,g,b),
                "Ks 0.30 0.30 0.30",
                "Ns %.1f"%(40+100*info["metallic"]),"d 1.0","illum 2",""]
    (out/"M04-006.mtl").write_text("\n".join(mtl)+"\n",encoding="utf-8")


def make_blender(spec,parts,out):
    import bpy
    from mathutils import Quaternion
    owned=bpy.data.collections.get("M04-006_ASSET")
    if owned is not None:
        for obj in tuple(owned.objects):
            bpy.data.objects.remove(obj,do_unlink=True)
        bpy.data.collections.remove(owned)
    owned=bpy.data.collections.new("M04-006_ASSET")
    bpy.context.scene.collection.children.link(owned)
    mats={}
    for key,info in spec["materials"].items():
        mat=bpy.data.materials.new("M04-006_"+key)
        mat.use_nodes=True
        bsdf=mat.node_tree.nodes.get("Principled BSDF")
        bsdf.inputs["Base Color"].default_value=tuple(info["color"])
        bsdf.inputs["Metallic"].default_value=info["metallic"]
        bsdf.inputs["Roughness"].default_value=info["roughness"]
        if "emission" in info:
            emission=bsdf.inputs.get("Emission Color") or bsdf.inputs.get("Emission")
            if emission is not None:
                emission.default_value=tuple(info["emission"])
            strength=bsdf.inputs.get("Emission Strength")
            if strength is not None:
                strength.default_value=info["emission_strength"]
        mats[key]=mat
    root=bpy.data.objects.new("M04-006_MOUNT_ROOT",None)
    owned.objects.link(root)
    root.empty_display_type="PLAIN_AXES"
    root.empty_display_size=.13
    root["mount"]="rear_frame_Y0"
    root["scale"]="1_unit_1_metre"
    for p in parts:
        pivot=p.get("pivot",(0,0,0))
        local=[tuple(v[i]-pivot[i] for i in range(3)) for v in p["vertices"]]
        mesh=bpy.data.meshes.new("M04-006_"+p["name"]+"_mesh")
        mesh.from_pydata(local,[],p["faces"])
        mesh.update(calc_edges=True)
        obj=bpy.data.objects.new("M04-006_"+p["name"],mesh)
        owned.objects.link(obj)
        obj.parent=root
        obj.location=pivot
        obj.data.materials.append(mats[p["material"]])
        obj["model_id"]=MODEL
        obj["animated"]=bool(p.get("moving"))
        for poly in mesh.polygons:
            poly.use_smooth=False
        if p.get("moving"):
            obj.rotation_mode="QUATERNION"
            for frame,angle in zip(spec["louver"]["frames"],
                                   spec["louver"]["angles_deg"]):
                obj.rotation_quaternion=Quaternion((1,0,0),math.radians(angle))
                obj.keyframe_insert(data_path="rotation_quaternion",
                                    frame=frame,group="SLOT_LOUVER")
            if obj.animation_data and obj.animation_data.action:
                action=obj.animation_data.action
                if hasattr(action,"fcurves"):
                    for curve in action.fcurves:
                        for key in curve.keyframe_points:
                            key.interpolation="LINEAR"
    scene=bpy.context.scene
    old_frame=scene.frame_current
    old_start,old_end=scene.frame_start,scene.frame_end
    old_selected=tuple(bpy.context.selected_objects)
    old_active=bpy.context.view_layer.objects.active
    try:
        scene.frame_start,scene.frame_end=1,80
        scene.frame_set(1)
        bpy.context.view_layer.update()
        assert len(owned.objects)==len(parts)+1
        out.mkdir(parents=True,exist_ok=True)
        # A collection library is intentionally NOT a full unrelated scene file.
        bpy.data.libraries.write(str(out/"M04-006.blend"),{owned},compress=True)
        bpy.ops.object.select_all(action="DESELECT")
        for obj in owned.objects:
            obj.select_set(True)
        bpy.context.view_layer.objects.active=root
        bpy.ops.export_scene.gltf(
            filepath=str(out/"M04-006_animated.glb"),
            export_format="GLB",use_selection=True,
            export_animations=True,export_apply=False)
    finally:
        bpy.ops.object.select_all(action="DESELECT")
        for obj in old_selected:
            if obj.name in bpy.data.objects:
                obj.select_set(True)
        if old_active is not None and old_active.name in bpy.data.objects:
            bpy.context.view_layer.objects.active=old_active
        scene.frame_start,scene.frame_end=old_start,old_end
        scene.frame_set(old_frame)


def main():
    args=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else sys.argv[1:]
    parser=argparse.ArgumentParser()
    parser.add_argument("--self-test",action="store_true")
    parser.add_argument("--require-blender",action="store_true")
    parser.add_argument("--output-dir",default=str(HERE/"generated"))
    opts=parser.parse_args(args)
    spec=read_spec()
    parts=build(spec)
    report=qa(spec,parts)
    out=Path(opts.output_dir).resolve()
    write_static(spec,parts,out)
    if not opts.self_test:
        try:
            import bpy  # noqa: F401
        except ImportError:
            if opts.require_blender:
                raise RuntimeError("Blender bpy unavailable")
        else:
            make_blender(spec,parts,out)
            report["blender"]="EXECUTED"
            report["animated_glb"]="GENERATED_NOT_UNITY_VERIFIED"
    (out/"M04-006.static-qa.json").write_text(
        json.dumps(report,indent=2,ensure_ascii=False)+"\n",encoding="utf-8")
    print(json.dumps(report,indent=2,ensure_ascii=False))


if __name__=="__main__":
    main()
