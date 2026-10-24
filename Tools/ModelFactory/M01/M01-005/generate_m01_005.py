#!/usr/bin/env python3
"""M01-005 side-mounted twin disc launcher — deterministic OBJ + animated bpy GLB.

Visual-only module. Blender Z-up, 1 unit = 1 metre; mounting plane X=0.
The side mounting pivot is (0,0,0). Front points towards negative Y.
The two magazine rotors turn around local X at the pivots in geometry.json.
No gameplay/WPN code. Blender export is isolated to M01-005_ASSET.
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
    spec = json.loads(SPEC_PATH.read_text(encoding="utf-8"))
    if spec["id"] != "M01-005":
        raise ValueError("Wrong model ID")
    return spec


def mesh_for_part(part, pivots, world=True):
    """Closed hard-surface mesh with outward polygon winding."""
    kind = part["shape"]
    if kind == "box":
        a, b, c = (float(x) * 0.5 for x in part["size"])
        vertices = [
            (-a, -b, -c), (a, -b, -c), (a, b, -c), (-a, b, -c),
            (-a, -b, c), (a, -b, c), (a, b, c), (-a, b, c),
        ]
        faces = [
            (3, 2, 1, 0), (4, 5, 6, 7), (0, 1, 5, 4),
            (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7),
        ]
    elif kind in ("cylinder", "ring"):
        if part["axis"] != "X":
            raise ValueError("M01-005 cylindrical axis must be X")
        n = int(part["segments"])
        r = float(part["radius"])
        half = float(part["depth"]) * 0.5
        assert n >= 12 and r > 0 and half > 0
        vertices = []
        for x in (-half, half):
            for i in range(n):
                angle = math.tau * i / n
                vertices.append((x, r * math.cos(angle), r * math.sin(angle)))
        if kind == "cylinder":
            faces = [
                tuple(reversed(range(n))), tuple(range(n, 2*n)),
            ]
            faces.extend((i, (i+1)%n, (i+1)%n+n, i+n)
                         for i in range(n))
        else:
            inner = float(part["inner_radius"])
            assert 0 < inner < r
            for x in (-half, half):
                for i in range(n):
                    angle = math.tau * i / n
                    vertices.append(
                        (x, inner * math.cos(angle), inner * math.sin(angle)))
            faces = []
            for i in range(n):
                j = (i + 1) % n
                faces.extend([
                    (i, j, j+n, i+n),                         # outer wall
                    (i+2*n, i+3*n, j+3*n, j+2*n),             # inner wall
                    (i, i+2*n, j+2*n, j),                     # back face
                    (i+n, j+n, j+3*n, i+3*n),                 # front face
                ])
    else:
        raise ValueError("Unsupported primitive: " + str(kind))

    translation = list(map(float, part["center"]))
    if world and part["parent"] != "base":
        pivot = pivots[part["parent"]]
        translation = [translation[i] + pivot[i] for i in range(3)]
    vertices = [tuple(p[i] + translation[i] for i in range(3))
                for p in vertices]
    return vertices, faces


def signed_volume(vertices, faces):
    volume = 0.0
    for face in faces:
        a = vertices[face[0]]
        for j in range(1, len(face)-1):
            b, c = vertices[face[j]], vertices[face[j+1]]
            cross = (
                b[1]*c[2]-b[2]*c[1],
                b[2]*c[0]-b[0]*c[2],
                b[0]*c[1]-b[1]*c[0],
            )
            volume += sum(a[k]*cross[k] for k in range(3)) / 6.0
    return volume


def bounds_at(part, pivots, angle):
    vertices, _ = mesh_for_part(part, pivots, world=False)
    if part["parent"] != "base":
        p = pivots[part["parent"]]
        co, si = math.cos(angle), math.sin(angle)
        vertices = [
            (x + p[0], y*co-z*si+p[1], y*si+z*co+p[2])
            for x, y, z in vertices
        ]
    return [(min(v[k] for v in vertices), max(v[k] for v in vertices))
            for k in range(3)]


def intersect_volume(a, b):
    return math.prod(
        max(0.0, min(a[k][1], b[k][1]) - max(a[k][0], b[k][0]))
        for k in range(3)
    )


def validate(spec):
    assert spec["mount_origin"] == [0, 0, 0]
    assert spec["mount_type"] == "side"
    assert len(spec["materials"]) >= 3
    assert spec["keyframes"] == [1, 49, 97]
    parts = spec["parts"]
    names = [p["name"] for p in parts]
    assert len(names) == len(set(names)), "Duplicate part names"
    assert all(p["material"] in spec["materials"] for p in parts)
    assert all(p["parent"] in ("base", "rotor_left", "rotor_right")
               for p in parts)
    vertices_total = polygons_total = 0
    for part in parts:
        vertices, faces = mesh_for_part(part, spec["pivots"])
        assert all(len(f) >= 3 and len(set(f)) == len(f) for f in faces)
        assert all(0 <= i < len(vertices) for f in faces for i in f)
        volume = signed_volume(vertices, faces)
        assert volume > 1e-9, (part["name"], volume)
        vertices_total += len(vertices)
        polygons_total += len(faces)

    fixed = [p for p in parts if p["parent"] == "base"]
    left = [p for p in parts if p["parent"] == "rotor_left"]
    right = [p for p in parts if p["parent"] == "rotor_right"]
    assert len(fixed) > 20 and len(left) == len(right) > 10
    fixed_bounds = [(p["name"], bounds_at(p, spec["pivots"], 0))
                    for p in fixed]
    checks = 0
    # 73 samples include the full rotation and every 5-degree intermediate pose.
    for step in range(73):
        angle = math.radians(step * 5)
        lb = [(p["name"], bounds_at(p, spec["pivots"], angle))
              for p in left]
        rb = [(p["name"], bounds_at(p, spec["pivots"], -angle))
              for p in right]
        for moving_name, moving_bounds in lb + rb:
            for fixed_name, fixed_box in fixed_bounds:
                v = intersect_volume(moving_bounds, fixed_box)
                assert v <= 1e-10, (
                    "moving/stationary overlap", step*5, moving_name,
                    fixed_name, v)
                checks += 1
        for left_name, left_box in lb:
            for right_name, right_box in rb:
                v = intersect_volume(left_box, right_box)
                assert v <= 1e-10, (
                    "rotor/rotor overlap", step*5, left_name, right_name, v)
                checks += 1

    return {
        "model":spec["id"], "parts":len(parts),
        "fixed_parts":len(fixed), "left_parts":len(left),
        "right_parts":len(right),
        "vertices":vertices_total, "polygons":polygons_total,
        "materials":len(spec["materials"]),
        "mesh_signed_volume":"PASS",
        "rotation_samples":73, "rotor_rotation_deg":[0,360],
        "aabb_pairs_checked":checks,
        "base_vs_rotors":"PASS", "rotor_vs_rotor":"PASS",
        "blender_export":"NOT RUN by static self-test",
        "unity_import":"NOT RUN",
    }


def write_obj(spec, output_dir):
    output_dir.mkdir(parents=True, exist_ok=True)
    obj = output_dir / "M01-005.obj"
    mtl = output_dir / "M01-005.mtl"
    lines = [
        "# M01-005 Side-mounted twin disc launcher",
        "# Static neutral pose; rotation is only in animated Blender/GLB.",
        "mtllib M01-005.mtl",
    ]
    offset = 1
    for part in spec["parts"]:
        vertices, faces = mesh_for_part(part, spec["pivots"])
        lines.extend(("o " + part["name"], "usemtl " + part["material"]))
        lines.extend("v {:.6f} {:.6f} {:.6f}".format(*v) for v in vertices)
        lines.extend("f " + " ".join(str(offset+i) for i in f) for f in faces)
        offset += len(vertices)
    obj.write_text("\n".join(lines)+"\n", encoding="utf-8")
    materials = ["# M01-005 material library"]
    for name, info in spec["materials"].items():
        r,g,b,_ = info["color"]
        materials.extend([
            "newmtl "+name,
            "Ka 0.05000 0.05000 0.05000",
            "Kd {:.5f} {:.5f} {:.5f}".format(r,g,b),
            "Ks 0.25000 0.25000 0.25000",
            "Ns {:.1f}".format(30+100*info["metallic"]),
            "d 1.0", "illum 2", "",
        ])
    mtl.write_text("\n".join(materials)+"\n", encoding="utf-8")
    return [obj.name, mtl.name]


def make_blender_scene(spec, output_dir):
    import bpy

    name = "M01-005_ASSET"
    previous = bpy.data.collections.get(name)
    if previous:
        for obj in tuple(previous.objects):
            bpy.data.objects.remove(obj, do_unlink=True)
        bpy.data.collections.remove(previous)
    owned = bpy.data.collections.new(name)
    bpy.context.scene.collection.children.link(owned)

    materials = {}
    for material_name, info in spec["materials"].items():
        mat = bpy.data.materials.new("M01-005_"+material_name)
        mat.diffuse_color = tuple(info["color"])
        mat.use_nodes = True
        bsdf = mat.node_tree.nodes.get("Principled BSDF")
        bsdf.inputs["Base Color"].default_value = tuple(info["color"])
        bsdf.inputs["Metallic"].default_value = info["metallic"]
        bsdf.inputs["Roughness"].default_value = info["roughness"]
        if "emission" in info:
            em = bsdf.inputs.get("Emission Color") or bsdf.inputs.get("Emission")
            if em:
                em.default_value = tuple(info["emission"])
            strength = bsdf.inputs.get("Emission Strength")
            if strength:
                strength.default_value = info["emission_strength"]
        materials[material_name] = mat

    root = bpy.data.objects.new("M01-005_SIDE_MOUNT_ROOT", None)
    owned.objects.link(root)
    root.empty_display_type = "PLAIN_AXES"
    root.empty_display_size = .10
    parents = {"base":root}
    for rotor_name in ("rotor_left", "rotor_right"):
        rotor = bpy.data.objects.new("M01-005_"+rotor_name.upper()+"_X", None)
        owned.objects.link(rotor)
        rotor.parent = root
        rotor.location = tuple(spec["pivots"][rotor_name])
        rotor.rotation_mode = "XYZ"
        rotor.empty_display_type = "CIRCLE"
        rotor.empty_display_size = .065
        parents[rotor_name] = rotor

    for part in spec["parts"]:
        verts, faces = mesh_for_part(part, spec["pivots"], world=False)
        mesh = bpy.data.meshes.new("M01-005_"+part["name"]+"_mesh")
        mesh.from_pydata(verts, [], faces)
        mesh.update(calc_edges=True)
        obj = bpy.data.objects.new("M01-005_"+part["name"], mesh)
        owned.objects.link(obj)
        obj.parent = parents[part["parent"]]
        obj.data.materials.append(materials[part["material"]])
        # Flat caps and machined planar surfaces; cylinder walls smooth.
        if part["shape"] in ("cylinder", "ring"):
            for polygon in mesh.polygons:
                if part["shape"] == "cylinder":
                    polygon.use_smooth = len(polygon.vertices) == 4
                else:
                    polygon.use_smooth = polygon.index % 4 in (0, 1)
        obj["model_id"] = spec["id"]
        obj["group"] = part["parent"]
        obj["mount_type"] = "side"

    # Quaternion-safe incremental 15-degree keys prevent 360-degree wrap.
    for step in range(25):
        frame = 1 + 4*step
        for rotor_name, sign in (("rotor_left", 1), ("rotor_right", -1)):
            rotor = parents[rotor_name]
            rotor.rotation_euler[0] = math.radians(sign * step * 15)
            rotor.keyframe_insert(data_path="rotation_euler", index=0,
                                  frame=frame)
    for rotor_name in ("rotor_left", "rotor_right"):
        action = parents[rotor_name].animation_data.action
        if action and hasattr(action, "fcurves"):
            for curve in action.fcurves:
                for key in curve.keyframe_points:
                    key.interpolation = "LINEAR"

    scene = bpy.context.scene
    old_start, old_end = scene.frame_start, scene.frame_end
    old_frame = scene.frame_current
    old_selection = tuple(bpy.context.selected_objects)
    old_active = bpy.context.view_layer.objects.active
    try:
        scene.frame_start, scene.frame_end = 1, 97
        scene.frame_set(1)
        bpy.context.view_layer.update()
        assert len(owned.objects) == len(spec["parts"]) + 3
        output_dir.mkdir(parents=True, exist_ok=True)
        # A collection-only .blend library, suitable for File > Append.
        bpy.data.libraries.write(
            str(output_dir / "M01-005.blend"), {owned}, compress=True)
        bpy.ops.object.select_all(action="DESELECT")
        for obj in owned.objects:
            obj.select_set(True)
        bpy.context.view_layer.objects.active = root
        bpy.ops.export_scene.gltf(
            filepath=str(output_dir / "M01-005_animated.glb"),
            export_format="GLB",
            use_selection=True,
            export_animations=True,
            export_apply=False,
        )
    finally:
        bpy.ops.object.select_all(action="DESELECT")
        for obj in old_selection:
            if obj.name in bpy.data.objects:
                obj.select_set(True)
        if old_active and old_active.name in bpy.data.objects:
            bpy.context.view_layer.objects.active = old_active
        scene.frame_start, scene.frame_end = old_start, old_end
        scene.frame_set(old_frame)
    return ["M01-005.blend", "M01-005_animated.glb"]


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
    target = Path(args.output_dir).resolve()
    report["static_files"] = write_obj(spec, target)
    try:
        import bpy  # noqa: F401
    except ImportError:
        report["blender_export"] = "NOT RUN (bpy unavailable)"
    else:
        report["animated_files"] = make_blender_scene(spec, target)
        report["blender_export"] = "GENERATED (runtime visual QA pending)"
    (target/"M01-005.run-report.json").write_text(
        json.dumps(report, indent=2)+"\n", encoding="utf-8")
    print(json.dumps(report, indent=2))


if __name__ == "__main__":
    main()
