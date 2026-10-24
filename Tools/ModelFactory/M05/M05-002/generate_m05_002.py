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


def part_geometry(part, pivot_z=0.535, world=True):
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
        cz += pivot_z
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
    # Conservative envelope check: max panel rear extent versus front of mast.
    max_rear = 0.055*math.cos(math.radians(22)) + 0.34*math.sin(math.radians(22))
    mast_front = 0.36 - 0.14/2
    clearance = mast_front - max_rear
    assert clearance > 0.10, "Panel could collide with the mast"
    assert vertices_total == 1160 and polygons_total == 834
    return {
        "model": spec["id"], "parts": len(names), "vertices": vertices_total,
        "polygons": polygons_total, "materials": len(spec["materials"]),
        "mast_clearance_m": round(clearance, 5), "primitive_winding": "PASS",
        "tilt_extremes_deg": [-22, 22],
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
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    bpy.context.scene.unit_settings.system = "METRIC"
    bpy.context.scene.unit_settings.scale_length = 1.0

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
    bpy.context.collection.objects.link(root)
    root.empty_display_type = "PLAIN_AXES"
    root.empty_display_size = 0.12
    tilt = bpy.data.objects.new("M05-002_TILT_X", None)
    bpy.context.collection.objects.link(tilt)
    tilt.parent = root
    tilt.location = tuple(spec["hinge_pivot"])
    tilt.rotation_mode = "XYZ"
    tilt.empty_display_type = "CIRCLE"
    tilt.empty_display_size = 0.09

    for part in spec["parts"]:
        vertices, faces = part_geometry(part, world=False)
        mesh = bpy.data.meshes.new(part["name"]+"_mesh")
        mesh.from_pydata(vertices, [], faces)
        mesh.update(calc_edges=True)
        obj = bpy.data.objects.new(part["name"], mesh)
        bpy.context.collection.objects.link(obj)
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
    bpy.context.scene.frame_start = min(spec["keyframes"])
    bpy.context.scene.frame_end = max(spec["keyframes"])
    bpy.context.scene.frame_set(spec["keyframes"][1])
    bpy.context.view_layer.update()

    output_dir.mkdir(parents=True, exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(output_dir / "M05-002.blend"))
    # Export animated GLB. glTF exporter converts Blender Z-up to glTF Y-up.
    bpy.ops.export_scene.gltf(
        filepath=str(output_dir / "M05-002_animated.glb"),
        export_format="GLB",
        export_animations=True,
        export_apply=False,
    )
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
