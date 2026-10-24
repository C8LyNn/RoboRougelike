#!/usr/bin/env python3
"""MODEL-05 M05-014 — procedural, visual-only dual-arm rear lift for Blender 3.6+.
Unity convention: 1 Blender unit = 1 metre. +Z up, +X right, -Y rearward.
The rear mounting socket is the origin; arm pivots rotate about their LOCAL +Z.
No gameplay behaviour or Unity scene modifications are generated.
"""
import argparse
import json
import math
import sys
from pathlib import Path

import bpy
import bmesh

ID = "M05-014"
PREFIX = "M05_014_"
COLLECTION = "M05_014_DUAL_ARM_LIFT"
FRAMES = (1, 16, 30, 48)
CLOSED_DEG = 12.0
OPEN_DEG = 34.0
PIVOT_X = 0.255
PIVOT_Y = -0.275
PIVOT_Z = 0.240
BOOM_LENGTH = 0.555
BOOM_WIDTH = 0.112
OUT_NAME = "M05-014_dual_arm_lift"

def clear_owned():
    col = bpy.data.collections.get(COLLECTION)
    if col:
        for obj in list(col.objects):
            bpy.data.objects.remove(obj, do_unlink=True)
        bpy.data.collections.remove(col)
    for mesh in list(bpy.data.meshes):
        if mesh.name.startswith(PREFIX) and mesh.users == 0:
            bpy.data.meshes.remove(mesh)

def new_collection():
    col = bpy.data.collections.new(COLLECTION)
    bpy.context.scene.collection.children.link(col)
    return col

def into(obj, col, parent=None):
    for c in list(obj.users_collection):
        c.objects.unlink(obj)
    col.objects.link(obj)
    if parent is not None:
        obj.parent = parent
    return obj

def empty(col, name, location=(0, 0, 0), parent=None):
    obj = bpy.data.objects.new(name, None)
    col.objects.link(obj)
    obj.empty_display_type = "PLAIN_AXES"
    obj.empty_display_size = 0.06
    if parent:
        obj.parent = parent
    obj.location = location
    return obj

def material(name, color, metallic=0.0, roughness=0.5, emission=None):
    full = PREFIX + name
    mat = bpy.data.materials.get(full) or bpy.data.materials.new(full)
    mat.diffuse_color = (*color, 1)
    mat.use_nodes = True
    pbr = mat.node_tree.nodes.get("Principled BSDF")
    pbr.inputs["Base Color"].default_value = (*color, 1)
    pbr.inputs["Metallic"].default_value = metallic
    pbr.inputs["Roughness"].default_value = roughness
    if emission is not None:
        if "Emission Color" in pbr.inputs:
            pbr.inputs["Emission Color"].default_value = (*emission, 1)
            pbr.inputs["Emission Strength"].default_value = 2.5
        elif "Emission" in pbr.inputs:
            pbr.inputs["Emission"].default_value = (*emission, 1)
    return mat

def make_materials():
    return {
        "graphite": material("ArmorGraphite", (.070, .091, .111), .75, .32),
        "titanium": material("Titanium", (.43, .49, .53), .84, .25),
        "ceramic": material("CeramicIvory", (.73, .78, .76), .12, .31),
        "copper": material("HydraulicCopper", (.48, .20, .075), .76, .28),
        "rubber": material("GripRubber", (.045, .050, .060), .02, .77),
        "hazard": material("HazardAmber", (.92, .39, .035), .18, .40),
        "cyan": material("StatusCyan", (.02, .47, .62), .18, .24, (.02, .62, .95)),
        "glass": material("DarkOptic", (.022, .06, .08), .25, .14),
    }

def finish_mesh(obj, col, parent, name, mat, loc, rot):
    obj.name = PREFIX + name
    obj.data.name = PREFIX + name + "_MESH"
    into(obj, col, parent)
    obj.location = loc
    obj.rotation_euler = rot
    obj.data.materials.append(mat)
    for face in obj.data.polygons:
        face.use_smooth = True
    return obj

def box(col, parent, name, center, dims, mat, bevel=0.008, rot=(0, 0, 0)):
    bpy.ops.mesh.primitive_cube_add(size=1)
    obj = bpy.context.object
    obj.dimensions = dims
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel:
        mod = obj.modifiers.new("MachinedEdges", "BEVEL")
        mod.width = min(bevel, min(dims) * .18)
        mod.segments = 2
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=mod.name)
    return finish_mesh(obj, col, parent, name, mat, center, rot)

def cyl(col, parent, name, center, radius, depth, mat, axis="Z", vertices=24):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth)
    obj = bpy.context.object
    rot = {"X": (0, math.pi / 2, 0),
           "Y": (math.pi / 2, 0, 0),
           "Z": (0, 0, 0)}[axis]
    return finish_mesh(obj, col, parent, name, mat, center, rot)

def torus(col, parent, name, center, major, minor, mat):
    bpy.ops.mesh.primitive_torus_add(major_segments=32, minor_segments=8,
                                    location=(0, 0, 0),
                                    major_radius=major, minor_radius=minor)
    return finish_mesh(bpy.context.object, col, parent, name, mat, center, (0, 0, 0))

def build():
    clear_owned()
    col = new_collection()
    mats = make_materials()
    root = empty(col, "MOUNT_REAR_SOCKET", (0, 0, 0))
    # Rear interface is at Y=0; geometry extends towards negative Y.
    box(col, root, "RearDockPlate", (0, -.021, .200), (.720, .042, .360), mats["titanium"], .012)
    box(col, root, "ServoEnclosure", (0, -.145, .218), (.630, .228, .240), mats["graphite"], .018)
    box(col, root, "UpperCeramicPanel", (0, -.132, .345), (.520, .160, .028), mats["ceramic"], .007)
    box(col, root, "LowerReinforcement", (0, -.144, .088), (.590, .180, .040), mats["titanium"], .008)
    box(col, root, "CenterHazardStrip", (0, -.253, .190), (.180, .010, .047), mats["hazard"], .002)
    for side, label in ((-1, "L"), (1, "R")):
        x = side * .298
        for z in (.075, .325):
            cyl(col, root, f"DockBolt_{label}_{int(z*1000)}", (x, -.045, z),
                .018, .012, mats["graphite"], "Y", 16)
        cyl(col, root, f"ServoBearing_{label}", (side * PIVOT_X, PIVOT_Y, PIVOT_Z),
            .094, .088, mats["titanium"], "Z", 32)
        torus(col, root, f"ServoCopperRing_{label}", (side * PIVOT_X, PIVOT_Y, .291),
              .066, .008, mats["copper"])
    # Two symmetric articulated forks; the entire arm assembly follows its own yaw pivot.
    pivots = []
    for side, label in ((-1, "L"), (1, "R")):
        pivot = empty(col, f"{PREFIX}ARM_{label}_PIVOT",
                      (side * PIVOT_X, PIVOT_Y, PIVOT_Z), root)
        pivots.append((side, label, pivot))
        cyl(col, pivot, f"ArmHub_{label}", (0, 0, 0), .069, .094,
            mats["graphite"], "Z", 32)
        cyl(col, pivot, f"ArmHubCap_{label}", (0, 0, .056), .055, .015,
            mats["hazard"], "Z", 32)
        box(col, pivot, f"MainBoom_{label}", (0, -.370, 0),
            (BOOM_WIDTH, BOOM_LENGTH, .115), mats["titanium"], .014)
        box(col, pivot, f"CeramicBoomCover_{label}", (0, -.363, .068),
            (.088, .435, .026), mats["ceramic"], .007)
        box(col, pivot, f"HydraulicHousing_{label}", (side * .081, -.310, -.018),
            (.040, .255, .043), mats["graphite"], .006)
        cyl(col, pivot, f"HydraulicRod_{label}", (side * .081, -.496, -.018),
            .014, .164, mats["copper"], "Y", 16)
        box(col, pivot, f"HydraulicEndStop_{label}", (side * .081, -.581, -.018),
            (.058, .038, .063), mats["titanium"], .004)
        box(col, pivot, f"BoomSignal_{label}", (0, -.285, .086),
            (.035, .122, .006), mats["cyan"], .002)
        box(col, pivot, f"TipCrosshead_{label}", (0, -.690, 0),
            (.195, .114, .125), mats["graphite"], .012)
        for fork, sx in (("INNER", -side), ("OUTER", side)):
            fx = sx * .073
            box(col, pivot, f"JawFinger_{label}_{fork}", (fx, -.795, -.005),
                (.044, .195, .093), mats["titanium"], .008)
            box(col, pivot, f"JawGrip_{label}_{fork}", (fx, -.875, .003),
                (.045, .043, .104), mats["rubber"], .006)
            box(col, pivot, f"JawTipMark_{label}_{fork}", (fx, -.885, .061),
                (.032, .023, .010), mats["hazard"], .002)
        cyl(col, pivot, f"TipInspectionOptic_{label}", (0, -.759, .071),
            .027, .012, mats["glass"], "Z", 24)
        cyl(col, pivot, f"TipReadyLED_{label}", (0, -.756, .079),
            .012, .009, mats["cyan"], "Z", 16)
        empty(col, f"{PREFIX}JAW_CONTACT_{label}", (0, -.885, .003), pivot)
        # The yaw animation is symmetric: left negative, right positive.
        for frame, angle in ((1, CLOSED_DEG), (16, OPEN_DEG),
                             (30, OPEN_DEG), (48, CLOSED_DEG)):
            pivot.rotation_euler = (0, 0, math.radians(side * angle))
            pivot.keyframe_insert(data_path="rotation_euler", frame=frame)
        if pivot.animation_data and pivot.animation_data.action:
            for curve in pivot.animation_data.action.fcurves:
                for key in curve.keyframe_points:
                    key.interpolation = "LINEAR"
    empty(col, PREFIX + "POWER_PORT", (0, -.036, .130), root)
    empty(col, PREFIX + "LIFT_CLEARANCE_REFERENCE", (0, -.940, .240), root)
    bpy.context.scene.frame_start = 1
    bpy.context.scene.frame_end = 48
    bpy.context.scene.render.fps = 30
    bpy.context.scene.frame_set(1)
    return col, pivots

def inspect(col, pivots):
    meshes = [o for o in col.objects if o.type == "MESH"]
    material_names = {m.name for o in meshes for m in o.data.materials}
    assert len(material_names) >= 3, "fewer than 3 materials"
    assert len(pivots) == 2
    for obj in meshes:
        assert len(obj.data.polygons) > 0, obj.name
        bm = bmesh.new()
        bm.from_mesh(obj.data)
        assert all(edge.is_manifold for edge in bm.edges), obj.name
        assert all(face.calc_area() > 1e-9 for face in bm.faces), obj.name
        bm.free()
    # Conservative swept-volume test: boom/jaw halves stay on opposite X sides
    # for every interpolated pose. The bearing/hub contact is intentional.
    min_inner_gap = float("inf")
    for step in range(121):
        a = math.radians(CLOSED_DEG + (OPEN_DEG - CLOSED_DEG) * step / 120)
        # At y <= -.10 in arm-local coordinates, each arm moves outward.
        # The jaw fingers extend no farther than 0.100m inward from boom center.
        x_inner = PIVOT_X + .10 * math.sin(a) - .102
        gap = 2 * x_inner
        min_inner_gap = min(min_inner_gap, gap)
        assert gap > .30, f"arm swept-envelope collision: {gap}"
    return {
        "id": ID, "mesh_count": len(meshes),
        "vertex_count": sum(len(o.data.vertices) for o in meshes),
        "triangle_count": sum(len(p.vertices) - 2 for o in meshes for p in o.data.polygons),
        "materials": sorted(material_names),
        "mount": {"name": "MOUNT_REAR_SOCKET", "xyz_m": [0, 0, 0]},
        "arm_pivots_m": [[-PIVOT_X, PIVOT_Y, PIVOT_Z],
                         [PIVOT_X, PIVOT_Y, PIVOT_Z]],
        "rotation_axis": "local +Z",
        "yaw_closed_degrees": [-CLOSED_DEG, CLOSED_DEG],
        "yaw_open_degrees": [-OPEN_DEG, OPEN_DEG],
        "animation_frames": list(FRAMES),
        "swept_envelope_samples": 121,
        "minimum_conservative_inner_gap_m": round(min_inner_gap, 6),
        "topology": "PASS: each mesh closed manifold, nonzero face area",
        "blender": "PASS",
        "unity_import": "NOT RUN",
        "unity_editmode": "NOT RUN",
        "unity_playmode": "NOT RUN",
        "visual_review": "NOT RUN",
    }

def run():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", default="build/M05-014")
    ap.add_argument("--repeat-check", action="store_true")
    args = ap.parse_args(argv)
    out = Path(args.out).resolve()
    out.mkdir(parents=True, exist_ok=True)
    foreign = bpy.data.objects.new("FOREIGN_OBJECT_QA_KEEP", None) if args.repeat_check else None
    if foreign:
        bpy.context.scene.collection.objects.link(foreign)
    col, pivots = build()
    first = inspect(col, pivots)
    if args.repeat_check:
        col, pivots = build()
        second = inspect(col, pivots)
        assert first == second, "nondeterministic second build"
        assert bpy.data.objects.get("FOREIGN_OBJECT_QA_KEEP") is foreign
        first["blender_repeat_build"] = "PASS"
        first["blender_foreign_object_preserved"] = "PASS"
    else:
        first["blender_repeat_build"] = "NOT RUN"
        first["blender_foreign_object_preserved"] = "NOT RUN"
    for o in bpy.context.selected_objects:
        o.select_set(False)
    for obj in col.objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = next(o for o in col.objects if o.type == "MESH")
    bpy.ops.export_scene.gltf(filepath=str(out / (OUT_NAME + ".glb")),
                              export_format="GLB", use_selection=True,
                              export_animations=True)
    bpy.ops.export_scene.fbx(filepath=str(out / (OUT_NAME + ".fbx")),
                             use_selection=True, add_leaf_bones=False)
    bpy.ops.wm.save_as_mainfile(filepath=str(out / (OUT_NAME + ".blend")))
    first["blender_export"] = "PASS"
    (out / (OUT_NAME + "_qa.json")).write_text(
        json.dumps(first, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print("MODEL-05 QA:", json.dumps(first, ensure_ascii=False))

if __name__ == "__main__":
    run()
