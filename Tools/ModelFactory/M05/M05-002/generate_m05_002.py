#!/usr/bin/env python3
"""M05-002 Flat Panel Radar: deterministic static OBJ and Blender animated GLB.

Run without Blender: python generate_m05_002.py --self-test
Run in Blender: blender -b --python generate_m05_002.py -- --output-dir /path/to/output
Coordinates: Blender Z up, 1 unit = 1 metre; mounting origin at (0,0,0).
Hinge pivot (0,0,0.535); animated tilt about local X, -22..+22 degrees.
No gameplay logic. Uses geometry.json as the single source of truth.
"""
from __future__ import annotations

import argparse
import json
import math
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
SPEC_PATH = HERE / "geometry.json"


def load_spec():
    with SPEC_PATH.open("r", encoding="utf-8") as f:
        spec = json.load(f)
    if spec["id"] != "M05-002":
        raise ValueError("Unexpected model ID")
    return spec


def part_geometry(part, pivot=(0.0, 0.235, 0.535), world=True):
    """Return closed manifold primitive vertices/faces, outward winding."""
    if part["shape"] == "box":
        a, b, d = (float(n) / 2.0 for n in part["size"])
        vertices = [
            (-a, -b, -d), (a, -b, -d), (a, b, -d), (-a, b, -d),
            (-a, -b, d), (a, -b, d), (a, b, d), (-a, b, d),
        ]
        faces = [
            (3, 2, 1, 0), (4, 5, 6, 7), (0, 1, 5, 4),
            (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7),
        ]
    elif part["shape"] == "cylinder":
        n = int(part["segments"])
        radius = float(part["radius"])
        half = float(part["depth"]) / 2.0
        axis = part["axis"]
        vertices = []
        for layer in (-1, 1):
            for i in range(n):
                angle = 2.0 * math.pi * i / n
                u, v = radius * math.cos(angle), radius * math.sin(angle)
                if axis == "X":
                    vertices.append((layer * half, u, v))
                elif axis == "Y":
                    vertices.append((v, layer * half, u))
                elif axis == "Z":
                    vertices.append((u, v, layer * half))
                else:
                    raise ValueError("Unknown cylinder axis")
        faces = [tuple(reversed(range(n))), tuple(range(n, 2*n))]
        faces.extend((i, (i+1) % n, (i+1) % n+n, i+n) for i in range(n))
    else:
        raise ValueError("Unknown shape: " + str(part["shape"]))

    cx, cy, cz = (float(x) for x in part["center"])
    if world and part["parent"] == "tilt":
        cx += pivot[0]
        cy += pivot[1]
        cz += pivot[2]
    vertices = [(x+cx, y+cy, z+cz) for x, y, z in vertices]
    return vertices, faces


def signed_volume(vertices, faces):
    total = 0.0
    for face in faces:
        a = vertices[face[0]]
        for i in range(1, len(face)-1):
            b, c = vertices[face[i]], vertices[face[i+1]]
            cross = (
                b[1]*c[2]-b[2]*c[1],
                b[2]*c[0]-b[0]*c[2],
                b[0]*c[1]-b[1]*c[0],
            )
            total += (a[0]*cross[0]+a[1]*cross[1]+a[2]*cross[2])/6.0
    return total


def animated_bounds(part, pivot, degrees):
    """World AABB of actual polygon vertices at the requested hinge angle."""
    verts, _ = part_geometry(part, pivot=pivot, world=False)
    if part["parent"] == "tilt":
        angle = math.radians(degrees)
        cs, sn = math.cos(angle), math.sin(angle)
        verts = [(x + pivot[0], y*cs - z*sn + pivot[1],
                  y*sn + z*cs + pivot[2]) for x,y,z in verts]
    return [(min(v[i] for v in verts), max(v[i] for v in verts))
            for i in range(3)]


def swept_clearance(spec):
    """Check ALL 13 fixed vs 114 moving pieces across 45 one-degree poses.

    Conservative AABB non-overlap: PASS excludes inter-part mesh penetration.
    AABB collisions can over-report near misses, not under-report them.
    """
    base = [p for p in spec["parts"] if p["parent"] == "base"]
    moving = [p for p in spec["parts"] if p["parent"] == "tilt"]
    pivot = spec["hinge_pivot"]
    checked = 0
    min_mast_y_gap = float("inf")
    for degrees in range(-22, 23):
        fixed_bounds = [(p, animated_bounds(p,pivot,0)) for p in base]
        moving_bounds = [(p, animated_bounds(p,pivot,degrees))
                         for p in moving]
        for b, bb in fixed_bounds:
            for m, mb in moving_bounds:
                overlaps = [max(0, min(bb[i][1], mb[i][1]) -
                                    max(bb[i][0], mb[i][0])) for i in range(3)]
                volume = math.prod(overlaps)
                assert volume <= 1e-10, (
                    "M05-002 animated base/panel overlap",
                    b["name"], m["name"], degrees, volume)
                checked += 1
                if b["name"] == "mast_center":
                    min_mast_y_gap = min(min_mast_y_gap,
                        bb[1][0] - mb[1][1])
    assert checked == 45 * len(base) * len(moving), checked
    return {
        "swept_collision_samples": 45,
        "fixed_parts":len(base), "animated_parts":len(moving),
        "swept_aabb_pairs_checked":checked,
        "swept_base_vs_panel_aabb":"PASS",
        "mast_to_panel_y_gap_lower_bound_m":round(min_mast_y_gap,5),
    }


def validate(spec):
    assert spec["mount_origin"] == [0, 0, 0]
    assert len(spec["materials"]) >= 3
    names = [p["name"] for p in spec["parts"]]
    assert len(names) == len(set(names)), "Duplicate mesh names"
    assert len(names) == 127, "Unexpected primitive count"
    vertices_total = polygons_total = 0
    for part in spec["parts"]:
        assert part["material"] in spec["materials"]
        assert part["parent"] in ("base", "tilt")
        vertices, faces = part_geometry(part)
        assert all(len(face) >= 3 and len(set(face)) == len(face) for face in faces)
        assert all(0 <= idx < len(vertices) for face in faces for idx in face)
        assert signed_volume(vertices, faces) > 1e-10, part["name"]
        vertices_total += len(vertices)
        polygons_total += len(faces)
    # Earlier version checked only panel rear vs mast at +/-22 degrees,
    # ignoring the hinge bridge, shaft, collars and every intervening pose.
    assert spec["hinge_pivot"] == [0, 0.235, 0.535]
    assert vertices_total == 1160 and polygons_total == 834
    sweep = swept_clearance(spec)
    assert sweep["mast_to_panel_y_gap_lower_bound_m"] > 0.06, sweep
    return {
        "model": spec["id"], "parts": len(names), "vertices": vertices_total,
        "polygons": polygons_total, "materials": len(spec["materials"]),
        "primitive_winding": "PASS", "tilt_extremes_deg": [-22, 22],
        "hinge_pivot_m":spec["hinge_pivot"], **sweep,
        "blender_render": "NOT RUN by self-test", "unity_import": "NOT RUN",
    }


def write_static_obj(spec, output_dir):
    output_dir.mkdir(parents=True, exist_ok=True)
    path = output_dir / "M05-002.obj"
    mtl = output_dir / "M05-002.mtl"
    lines = [
        "# M05-002 — Flat Panel Radar",
        "# Unity 1 unit = 1 meter. Static neutral pose; animation only in bpy source/GLB.",
        "mtllib M05-002.mtl",
    ]
    offset = 1
    for part in spec["parts"]:
        vertices, faces = part_geometry(part)
        lines += ["o " + part["name"], "usemtl " + part["material"]]
        lines += ["v {:.6f} {:.6f} {:.6f}".format(*v) for v in vertices]
        lines += ["f " + " ".join(str(offset+i) for i in f) for f in faces]
        offset += len(vertices)
    path.write_text("\n".join(lines)+"\n", encoding="utf-8")
    material_lines = ["# M05-002 materials"]
    for name, mat in spec["materials"].items():
        r, g, b, _ = mat["color"]
        material_lines += [
            "newmtl "+name, "Ka 0.05 0.05 0.05",
            "Kd {:.5f} {:.5f} {:.5f}".format(r,g,b),
            "Ks 0.25 0.25 0.25",
            "Ns {:.1f}".format(30+100*mat["metallic"]),
            "d 1.0", "illum 2", "",
        ]
    mtl.write_text("\n".join(material_lines)+"\n", encoding="utf-8")
    return path, mtl


def make_blender_scene(spec, output_dir):
    import bpy
    # The original generator deleted EVERY object in the user's open scene.
    # Replace only our owned collection and leave unrelated objects untouched.
    collection_name = "M05-002_ASSET"
    owned = bpy.data.collections.get(collection_name)
    if owned is not None:
        for obj in tuple(owned.objects):
            bpy.data.objects.remove(obj, do_unlink=True)
        bpy.data.collections.remove(owned)
    owned = bpy.data.collections.new(collection_name)
    bpy.context.scene.collection.children.link(owned)
    for old in tuple(bpy.data.materials):
        if old.name.startswith("M05-002_") and old.users == 0:
            bpy.data.materials.remove(old)

    material_map = {}
    for name, info in spec["materials"].items():
        mat = bpy.data.materials.new("M05-002_" + name)
        mat.use_nodes = True
        bsdf = mat.node_tree.nodes.get("Principled BSDF")
        bsdf.inputs["Base Color"].default_value = tuple(info["color"])
        bsdf.inputs["Metallic"].default_value = float(info["metallic"])
        bsdf.inputs["Roughness"].default_value = float(info["roughness"])
        if "emission" in info:
            bsdf.inputs["Emission Color"].default_value = tuple(info["emission"])
            bsdf.inputs["Emission Strength"].default_value = float(info["emission_strength"])
        material_map[name] = mat

    root = bpy.data.objects.new("M05-002_MOUNT_ROOT", None)
    owned.objects.link(root)
    root.empty_display_type = "PLAIN_AXES"
    root.empty_display_size = 0.12
    tilt = bpy.data.objects.new("M05-002_TILT_X", None)
    owned.objects.link(tilt)
    tilt.parent = root
    tilt.location = tuple(spec["hinge_pivot"])
    tilt.rotation_mode = "XYZ"
    tilt.empty_display_type = "CIRCLE"
    tilt.empty_display_size = 0.09

    for part in spec["parts"]:
        vertices, faces = part_geometry(part, world=False)
        mesh = bpy.data.meshes.new("M05-002_"+part["name"]+"_mesh")
        mesh.from_pydata(vertices, [], faces)
        mesh.update(calc_edges=True)
        obj = bpy.data.objects.new("M05-002_"+part["name"], mesh)
        owned.objects.link(obj)
        obj.parent = tilt if part["parent"] == "tilt" else root
        obj.data.materials.append(material_map[part["material"]])
        # Flat hard-surface surfaces: deliberate sharp normals, no auto smoothing artifacts.
        for poly in obj.data.polygons:
            poly.use_smooth = False
        obj["model_id"] = spec["id"]
        obj["mount_type"] = "top"
        obj["group"] = part["parent"]

    for frame, angle in zip(spec["keyframes"], spec["tilt_degrees"]):
        tilt.rotation_euler[0] = math.radians(angle)
        tilt.keyframe_insert(data_path="rotation_euler", index=0, frame=frame)
    # Linear interpolation keeps scanning motion predictable.
    if tilt.animation_data and tilt.animation_data.action:
        action = tilt.animation_data.action
        if hasattr(action, "fcurves"):
            for curve in action.fcurves:
                for key in curve.keyframe_points:
                    key.interpolation = "LINEAR"
    scene = bpy.context.scene
    previous_frame = scene.frame_current
    previous_start, previous_end = scene.frame_start, scene.frame_end
    previous_selection = tuple(bpy.context.selected_objects)
    previous_active = bpy.context.view_layer.objects.active
    output_dir.mkdir(parents=True, exist_ok=True)
    try:
        scene.frame_start = min(spec["keyframes"])
        scene.frame_end = max(spec["keyframes"])
        scene.frame_set(spec["keyframes"][1])
        bpy.context.view_layer.update()
        assert len(owned.objects) == len(spec["parts"]) + 2
        assert all(obj.name.startswith("M05-002_") for obj in owned.objects)
        # Write ONLY our collection into an appendable Blender .blend library,
        # instead of saving unrelated scene content to the asset file.
        bpy.data.libraries.write(
            str(output_dir / "M05-002.blend"), {owned}, compress=True)
        # Export ONLY the owned objects, not other scene geometry.
        bpy.ops.object.select_all(action="DESELECT")
        for obj in owned.objects:
            obj.select_set(True)
        bpy.context.view_layer.objects.active = root
        bpy.ops.export_scene.gltf(
            filepath=str(output_dir / "M05-002_animated.glb"),
            export_format="GLB",
            use_selection=True,
            export_animations=True,
            export_apply=False,
        )
    finally:
        bpy.ops.object.select_all(action="DESELECT")
        for obj in previous_selection:
            if obj.name in bpy.data.objects:
                obj.select_set(True)
        if previous_active is not None and previous_active.name in bpy.data.objects:
            bpy.context.view_layer.objects.active = previous_active
        scene.frame_start, scene.frame_end = previous_start, previous_end
        scene.frame_set(previous_frame)
    return ["M05-002.blend", "M05-002_animated.glb"]


def main():
    argv = sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else sys.argv[1:]
    parser = argparse.ArgumentParser()
    parser.add_argument("--output-dir", default=str(HERE / "generated"))
    parser.add_argument("--self-test", action="store_true")
    args = parser.parse_args(argv)
    spec = load_spec()
    report = validate(spec)
    if args.self_test:
        print(json.dumps(report, indent=2))
        return
    output_dir = Path(args.output_dir).resolve()
    obj, mtl = write_static_obj(spec, output_dir)
    report["static_obj"] = str(obj)
    report["static_mtl"] = str(mtl)
    try:
        import bpy  # noqa: F401
    except ImportError:
        report["blender_execution"] = "NOT RUN (bpy unavailable)"
    else:
        report["blender_outputs"] = make_blender_scene(spec, output_dir)
        report["blender_execution"] = "GENERATED (visual QA still required)"
    (output_dir / "M05-002.run-report.json").write_text(
        json.dumps(report, indent=2)+"\n", encoding="utf-8"
    )
    print(json.dumps(report, indent=2))


if __name__ == "__main__":
    main()
