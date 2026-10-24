#!/usr/bin/env python3
# RoboRougelike / MODEL-03 / M03-001 — modular 1 x 1 x 1 m base cage
# Blender 3.6+ / 4.x: blender -b --factory-startup --python generate_m03_001.py
# Visual geometry only. No changes to game logic or Unity scenes.
from __future__ import annotations

import json
import math
import os
from pathlib import Path

import bpy
from mathutils import Vector

ASSET_ID = "M03-001"
COLLECTION = "RRL_M03_001"
ROOT_NAME = "M03_001_ROOT"
GRID = 1.0
CORNER = 0.425
NODE_SIZE = 0.150
BEAM_INNER = 0.350
BEAM_THICKNESS = 0.075
BRACE_THICKNESS = 0.046
PORT_PAD_SIDE = 0.200
PORT_PAD_DEPTH = 0.060
PORT_PAD_CENTRE = 0.470
PORT_SUPPORT_THICKNESS = 0.046
EXPORT_DIR = Path(os.environ.get(
    "RRL_MODEL_OUTPUT", str(Path(__file__).resolve().parent / "export")
)).expanduser().resolve()


def reset_owned_collection():
    """Remove only this asset's previous objects, never other scene content."""
    old = bpy.data.collections.get(COLLECTION)
    if old:
        for obj in list(old.objects):
            bpy.data.objects.remove(obj, do_unlink=True)
        bpy.data.collections.remove(old)
    coll = bpy.data.collections.new(COLLECTION)
    bpy.context.scene.collection.children.link(coll)
    return coll


def material(name, rgba, metallic, roughness, emission=None, strength=0.0):
    mat = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    mat.diffuse_color = rgba
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = rgba
        bsdf.inputs["Metallic"].default_value = metallic
        bsdf.inputs["Roughness"].default_value = roughness
        if emission is not None:
            inp = bsdf.inputs.get("Emission Color") or bsdf.inputs.get("Emission")
            if inp:
                inp.default_value = emission
            power = bsdf.inputs.get("Emission Strength")
            if power:
                power.default_value = strength
    return mat


def into_collection(obj, coll, parent=None):
    for old in tuple(obj.users_collection):
        old.objects.unlink(obj)
    coll.objects.link(obj)
    if parent is not None:
        obj.parent = parent
    return obj


def bevelled_box(name, center, size, mat, coll, root, bevel_width=0.005):
    bpy.ops.mesh.primitive_cube_add(size=1, location=center)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    into_collection(obj, coll, root)
    obj.data.materials.append(mat)
    for poly in obj.data.polygons:
        poly.use_smooth = True
    bevel = obj.modifiers.new("Manufactured_edge_radius", "BEVEL")
    bevel.width = bevel_width
    bevel.segments = 3
    bevel.limit_method = "ANGLE"
    bevel.angle_limit = math.radians(30)
    bevel.loop_slide = True
    normals = obj.modifiers.new("Weighted_face_normals", "WEIGHTED_NORMAL")
    normals.keep_sharp = True
    return obj


def strut(name, start, end, thickness, mat, coll, root, bevel_width=0.004):
    a, b = Vector(start), Vector(end)
    axis = b - a
    assert axis.length > 0.01, name
    obj = bevelled_box(
        name, (a + b) / 2.0,
        (thickness, thickness, axis.length),
        mat, coll, root, bevel_width
    )
    obj.rotation_euler = axis.to_track_quat("Z", "Y").to_euler()
    return obj


def flush_disc(name, center, outward, radius, mat, coll, root):
    # A 2.4 mm face decal intentionally embedded in the outer corner-node skin.
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=24, radius=radius, depth=0.0024, location=center
    )
    obj = bpy.context.object
    obj.name = name
    into_collection(obj, coll, root)
    obj.rotation_euler = Vector(outward).to_track_quat("Z", "Y").to_euler()
    obj.data.materials.append(mat)
    for poly in obj.data.polygons:
        poly.use_smooth = True
    return obj


def snap_empty(name, position, normal, coll, root):
    obj = bpy.data.objects.new(name, None)
    coll.objects.link(obj)
    obj.parent = root
    obj.location = position
    obj.rotation_euler = Vector(normal).to_track_quat("Z", "Y").to_euler()
    obj.empty_display_type = "ARROWS"
    obj.empty_display_size = 0.09
    obj["rrl_socket"] = "FRAME_GRID_1M"
    obj["rrl_outward"] = list(normal)
    obj["rrl_contact_plate"] = "MOUNT_PAD_" + name.removeprefix("SNAP_")
    obj["rrl_port_type"] = "physical_face_pad_1m_grid"
    return obj


def create_geometry(coll, root):
    steel = material("M03_001_Graphite_Steel", (0.095, 0.125, 0.155, 1), 0.76, 0.31)
    titanium = material("M03_001_Brushed_Titanium", (0.47, 0.52, 0.55, 1), 0.83, 0.27)
    copper = material("M03_001_Brace_Copper", (0.58, 0.28, 0.10, 1), 0.71, 0.30)
    socket = material("M03_001_Socket_Graphite", (0.025, 0.037, 0.048, 1), 0.28, 0.47)
    light = material(
        "M03_001_Indicator_Cyan", (0.03, 0.42, 0.58, 1), 0.18, 0.26,
        emission=(0.015, 0.50, 0.78, 1), strength=2.2
    )

    # Eight 150 mm cast corner nodes; outer dimensions exactly +/- 0.5 m.
    for sx in (-1, 1):
        for sy in (-1, 1):
            for sz in (-1, 1):
                node = (sx * CORNER, sy * CORNER, sz * CORNER)
                bevelled_box(
                    "NODE_%s%s%s" % (("P" if sx > 0 else "N"),
                                      ("P" if sy > 0 else "N"),
                                      ("P" if sz > 0 else "N")),
                    node, (NODE_SIZE,) * 3, titanium, coll, root, 0.010
                )
                # Decorative recessed fastener on each of the three outward faces.
                for axis, sign in enumerate((sx, sy, sz)):
                    pos = list(node)
                    pos[axis] = sign * 0.4987
                    outward = [0, 0, 0]
                    outward[axis] = sign
                    flush_disc(
                        "SOCKET_%d_%s%s%s" % (
                            axis, "P" if sx > 0 else "N",
                            "P" if sy > 0 else "N",
                            "P" if sz > 0 else "N"
                        ),
                        pos, outward, 0.023, socket, coll, root
                    )
                # Tiny cyan status dot on the top face, clear of the top socket.
                if sz > 0:
                    flush_disc(
                        "STATUS_%s%s" % ("P" if sx > 0 else "N",
                                         "P" if sy > 0 else "N"),
                        (sx * (CORNER + 0.046), sy * CORNER, 0.4987),
                        (0, 0, 1), 0.0075, light, coll, root
                    )

    # Twelve axis-aligned beams terminate at node inner faces: butt joints.
    for sy in (-1, 1):
        for sz in (-1, 1):
            strut(
                "X_BEAM_%s%s" % (sy, sz),
                (-BEAM_INNER, sy * CORNER, sz * CORNER),
                ( BEAM_INNER, sy * CORNER, sz * CORNER),
                BEAM_THICKNESS, steel, coll, root
            )
    for sx in (-1, 1):
        for sz in (-1, 1):
            strut(
                "Y_BEAM_%s%s" % (sx, sz),
                (sx * CORNER, -BEAM_INNER, sz * CORNER),
                (sx * CORNER,  BEAM_INNER, sz * CORNER),
                BEAM_THICKNESS, steel, coll, root
            )
    for sx in (-1, 1):
        for sy in (-1, 1):
            strut(
                "Z_BEAM_%s%s" % (sx, sy),
                (sx * CORNER, sy * CORNER, -BEAM_INNER),
                (sx * CORNER, sy * CORNER,  BEAM_INNER),
                BEAM_THICKNESS, steel, coll, root
            )

    # Exactly four diagonal braces, one per vertical face. Only the brace
    # tips deliberately seat 20 mm into the corner castings; no brace crosses
    # another brace in free space. They do not move or require animation.
    diagonal = [
        ("FRONT", (-0.375, -CORNER, -0.375), (0.375, -CORNER, 0.375)),
        ("BACK",  (-0.375,  CORNER,  0.375), (0.375,  CORNER, -0.375)),
        ("LEFT",  (-CORNER, -0.375, -0.375), (-CORNER, 0.375, 0.375)),
        ("RIGHT", ( CORNER, -0.375,  0.375), ( CORNER, 0.375, -0.375)),
    ]
    for label, a, b in diagonal:
        strut(
            "DIAGONAL_" + label, a, b, BRACE_THICKNESS,
            copper, coll, root, bevel_width=0.003
        )

    # All six grid-centre ports must have *real*, flush outward-facing
    # contact plates. The original SNAP empties floated above hollow faces.
    # Face plates overlap vertical diagonal bracing at their inner end.
    # Top/bottom braces below tie their plates into the structural perimeter.
    pad_specs = [
        ("PX", ( PORT_PAD_CENTRE, 0, 0), (PORT_PAD_DEPTH, PORT_PAD_SIDE, PORT_PAD_SIDE)),
        ("NX", (-PORT_PAD_CENTRE, 0, 0), (PORT_PAD_DEPTH, PORT_PAD_SIDE, PORT_PAD_SIDE)),
        ("PY", (0,  PORT_PAD_CENTRE, 0), (PORT_PAD_SIDE, PORT_PAD_DEPTH, PORT_PAD_SIDE)),
        ("NY", (0, -PORT_PAD_CENTRE, 0), (PORT_PAD_SIDE, PORT_PAD_DEPTH, PORT_PAD_SIDE)),
        ("PZ", (0, 0,  PORT_PAD_CENTRE), (PORT_PAD_SIDE, PORT_PAD_SIDE, PORT_PAD_DEPTH)),
        ("NZ", (0, 0, -PORT_PAD_CENTRE), (PORT_PAD_SIDE, PORT_PAD_SIDE, PORT_PAD_DEPTH)),
    ]
    for label, pos, dims in pad_specs:
        bevelled_box("MOUNT_PAD_" + label, pos, dims,
                      titanium, coll, root, bevel_width=0.006)
    # Four stiffener ribs per top/bottom port. They intersect the perimeter
    # rails and pad intentionally; none protrude beyond the 1 m bounding cube.
    for sign, label in ((1, "PZ"), (-1, "NZ")):
        centre = (0, 0, sign * CORNER)
        for direction, end in (
            ("PX", ( CORNER, 0, sign * CORNER)),
            ("NX", (-CORNER, 0, sign * CORNER)),
            ("PY", (0,  CORNER, sign * CORNER)),
            ("NY", (0, -CORNER, sign * CORNER)),
        ):
            strut("MOUNT_RIB_" + label + "_" + direction,
                  centre, end, PORT_SUPPORT_THICKNESS, steel,
                  coll, root, bevel_width=0.003)

    # All six logical SNAP transforms align to the actual contact pad outer
    # planes at +/- 0.5 m, instead of unused empty space.
    for label, pos, normal in [
        ("PX", ( 0.5, 0, 0), ( 1, 0, 0)),
        ("NX", (-0.5, 0, 0), (-1, 0, 0)),
        ("PY", (0,  0.5, 0), (0,  1, 0)),
        ("NY", (0, -0.5, 0), (0, -1, 0)),
        ("PZ", (0, 0,  0.5), (0, 0,  1)),
        ("NZ", (0, 0, -0.5), (0, 0, -1)),
    ]:
        snap_empty("SNAP_" + label, pos, normal, coll, root)


def validate_geometry(coll):
    bpy.context.view_layer.update()
    meshes = [obj for obj in coll.objects if obj.type == "MESH"]
    names = [obj.name for obj in meshes]
    assert len([n for n in names if n.startswith("NODE_")]) == 8
    assert len([n for n in names if "_BEAM_" in n]) == 12
    assert len([n for n in names if n.startswith("DIAGONAL_")]) == 4
    assert len([n for n in names if n.startswith("SOCKET_")]) == 24
    assert len([n for n in names if n.startswith("STATUS_")]) == 4
    assert len([n for n in names if n.startswith("MOUNT_PAD_")]) == 6
    assert len([n for n in names if n.startswith("MOUNT_RIB_")]) == 8
    assert len([obj for obj in coll.objects if obj.name.startswith("SNAP_")]) == 6
    # Verify each SNAP references a physical pad flush with its outward
    # face; use world bounds rather than trusting the model manifest alone.
    for snap in (obj for obj in coll.objects if obj.name.startswith("SNAP_")):
        pad = coll.objects.get(snap["rrl_contact_plate"])
        assert pad is not None, "Missing physical contact plate: " + snap.name
        normal = tuple(snap["rrl_outward"])
        axis = next(i for i, v in enumerate(normal) if v)
        actual = [pad.matrix_world @ Vector(v) for v in pad.bound_box]
        outward_surface = (max if normal[axis] > 0 else min)(
            p[axis] for p in actual
        )
        assert abs(outward_surface - snap.location[axis]) < 1e-5, (
            snap.name, outward_surface, snap.location[axis]
        )
    used = {mat.name for obj in meshes for mat in obj.data.materials if mat}
    assert len(used) >= 3, "At least 3 material slots must be used"
    bounds = [[], [], []]
    for obj in meshes:
        for corner in obj.bound_box:
            point = obj.matrix_world @ Vector(corner)
            for axis in range(3):
                bounds[axis].append(point[axis])
    limits = [(min(axis), max(axis)) for axis in bounds]
    for low, high in limits:
        assert low >= -0.501 and high <= 0.501, limits
    return {
        "asset_id": ASSET_ID,
        "nominal_size_m": [GRID, GRID, GRID],
        "bounds_m": [[round(a, 5), round(b, 5)] for a, b in limits],
        "mesh_objects": len(meshes),
        "corner_nodes": 8,
        "straight_beams": 12,
        "diagonal_braces": 4,
        "physical_mount_pads": 6,
        "top_bottom_support_ribs": 8,
        "snap_points": 6,
        "materials_used": sorted(used),
        "pivot": [0, 0, 0],
        "movement": "none; rigid modular frame",
        "intersections": "Intentional brace-to-node, pad-to-brace, rib-to-rail contact and flush face decals",
        "export_qa": "Geometry checks executed in Blender; Unity import not tested"
    }


def export_asset(coll, manifest):
    EXPORT_DIR.mkdir(parents=True, exist_ok=True)
    bpy.ops.object.select_all(action="DESELECT")
    for obj in coll.objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = bpy.data.objects[ROOT_NAME]
    fbx = EXPORT_DIR / (ASSET_ID + ".fbx")
    bpy.ops.export_scene.fbx(
        filepath=str(fbx), use_selection=True, object_types={"MESH", "EMPTY"},
        apply_unit_scale=True, global_scale=1.0, bake_anim=False,
        use_mesh_modifiers=True, use_custom_props=True,
        add_leaf_bones=False, axis_forward="-Z", axis_up="Y"
    )
    manifest["fbx"] = fbx.name
    with (EXPORT_DIR / (ASSET_ID + ".manifest.json")).open(
        "w", encoding="utf-8"
    ) as handle:
        json.dump(manifest, handle, ensure_ascii=False, indent=2)
        handle.write("\n")
    print("M03-001: FBX and manifest exported to", EXPORT_DIR)


def main():
    coll = reset_owned_collection()
    root = bpy.data.objects.new(ROOT_NAME, None)
    coll.objects.link(root)
    root.empty_display_type = "CUBE"
    root.empty_display_size = 0.13
    root["asset_id"] = ASSET_ID
    root["grid_m"] = GRID
    root["category"] = "vehicle_frame"
    root["mounting"] = "six face-centre SNAP_ empties at +/- 0.5 m"
    create_geometry(coll, root)
    manifest = validate_geometry(coll)
    if os.environ.get("RRL_SKIP_EXPORT", "0") != "1":
        export_asset(coll, manifest)
    print("M03-001: geometry QA passed:", json.dumps(manifest, ensure_ascii=False))


if __name__ == "__main__":
    main()
