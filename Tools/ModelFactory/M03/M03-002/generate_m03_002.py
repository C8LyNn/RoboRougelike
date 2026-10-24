#!/usr/bin/env python3
# M03-002 | Telescopic structural beam, 1x2x1 m at rest.
# Python 3 stdlib creates static OBJ/MTL + QA; Blender bpy additionally creates .blend and animated GLB.
# The Blender scene is Z-up; longitudinal telescoping axis is +Y; Unity GLB importer applies axis conversion.
import argparse
import json
import math
from pathlib import Path
import sys

SPEC = json.loads(r'''{"id":"M03-002","name":"Belka teleskopowa 1x2x1","version":1,"units":"meters","axes":"Blender X width, Y longitudinal, Z up; rear mounting origin=(0,0,0)","extension_m":0.55,"sleeve_front_y":1.36,"slider_rear_y":0.52,"minimum_overlap_m":0.29,"palette":{"graphite":{"rgb":[0.064,0.077,0.098],"metallic":0.62,"roughness":0.42},"titanium":{"rgb":[0.32,0.38,0.43],"metallic":0.82,"roughness":0.3},"ceramic":{"rgb":[0.72,0.75,0.72],"metallic":0.16,"roughness":0.28},"safety_yellow":{"rgb":[0.98,0.69,0.045],"metallic":0.12,"roughness":0.36},"rubber":{"rgb":[0.035,0.038,0.044],"metallic":0.02,"roughness":0.79},"signal_cyan":{"rgb":[0.06,0.47,0.88],"metallic":0.25,"roughness":0.22,"emission":1.5}},"parts":[{"name":"Rear_mount_flange","center":[0,0.05,0],"size":[1,0.1,1],"material":"titanium","group":"rear"},{"name":"Sleeve_side_-1","center":[-0.3775,0.73,0],"size":[0.085,1.26,0.64],"material":"graphite","group":"sleeve"},{"name":"Sleeve_lateral_rail_top_-1","center":[-0.435,0.73,0.24],"size":[0.03,0.93,0.05],"material":"titanium","group":"sleeve"},{"name":"Sleeve_lateral_rail_bottom_-1","center":[-0.435,0.73,-0.24],"size":[0.03,0.93,0.05],"material":"titanium","group":"sleeve"},{"name":"Safety_stripe_-1_0","center":[-0.428,0.22,0],"size":[0.016,0.042,0.18],"material":"safety_yellow","group":"sleeve"},{"name":"Safety_stripe_-1_1","center":[-0.428,0.312,0],"size":[0.016,0.042,0.18],"material":"safety_yellow","group":"sleeve"},{"name":"Safety_stripe_-1_2","center":[-0.428,0.404,0],"size":[0.016,0.042,0.18],"material":"safety_yellow","group":"sleeve"},{"name":"Safety_stripe_-1_3","center":[-0.428,0.496,0],"size":[0.016,0.042,0.18],"material":"safety_yellow","group":"sleeve"},{"name":"Safety_stripe_-1_4","center":[-0.428,0.588,0],"size":[0.016,0.042,0.18],"material":"safety_yellow","group":"sleeve"},{"name":"Safety_stripe_-1_5","center":[-0.428,0.6799999999999999,0],"size":[0.016,0.042,0.18],"material":"safety_yellow","group":"sleeve"},{"name":"Safety_stripe_-1_6","center":[-0.428,0.772,0],"size":[0.016,0.042,0.18],"material":"safety_yellow","group":"sleeve"},{"name":"Safety_stripe_-1_7","center":[-0.428,0.864,0],"size":[0.016,0.042,0.18],"material":"safety_yellow","group":"sleeve"},{"name":"Safety_stripe_-1_8","center":[-0.428,0.956,0],"size":[0.016,0.042,0.18],"material":"safety_yellow","group":"sleeve"},{"name":"Safety_stripe_-1_9","center":[-0.428,1.048,0],"size":[0.016,0.042,0.18],"material":"safety_yellow","group":"sleeve"},{"name":"Slider_side_-1","center":[-0.2575,1.11,0],"size":[0.055,1.18,0.38],"material":"titanium","group":"slide"},{"name":"Slider_guide_-1_-0.11","center":[-0.292,1.15,-0.11],"size":[0.014,1,0.026],"material":"ceramic","group":"slide"},{"name":"Slider_guide_-1_0.11","center":[-0.292,1.15,0.11],"size":[0.014,1,0.026],"material":"ceramic","group":"slide"},{"name":"Slider_measure_-1_0","center":[-0.293,1.38,0],"size":[0.016,0.012,0.09],"material":"safety_yellow","group":"slide"},{"name":"Slider_measure_-1_1","center":[-0.293,1.422,0],"size":[0.016,0.012,0.055],"material":"safety_yellow","group":"slide"},{"name":"Slider_measure_-1_2","center":[-0.293,1.464,0],"size":[0.016,0.012,0.09],"material":"safety_yellow","group":"slide"},{"name":"Slider_measure_-1_3","center":[-0.293,1.5059999999999998,0],"size":[0.016,0.012,0.055],"material":"safety_yellow","group":"slide"},{"name":"Slider_measure_-1_4","center":[-0.293,1.5479999999999998,0],"size":[0.016,0.012,0.09],"material":"safety_yellow","group":"slide"},{"name":"Slider_measure_-1_5","center":[-0.293,1.5899999999999999,0],"size":[0.016,0.012,0.055],"material":"safety_yellow","group":"slide"},{"name":"Slider_measure_-1_6","center":[-0.293,1.632,0],"size":[0.016,0.012,0.09],"material":"safety_yellow","group":"slide"},{"name":"Slider_measure_-1_7","center":[-0.293,1.674,0],"size":[0.016,0.012,0.055],"material":"safety_yellow","group":"slide"},{"name":"Rear_bolt_-1_-0.44","center":[-0.46,0.11,-0.44],"size":[0.04,0.02,0.04],"material":"ceramic","group":"rear"},{"name":"Rear_bolt_-1_0.44","center":[-0.46,0.11,0.44],"size":[0.04,0.02,0.04],"material":"ceramic","group":"rear"},{"name":"Front_side_accent_-1","center":[-0.458,1.91,0.24],"size":[0.016,0.12,0.07],"material":"safety_yellow","group":"slide"},{"name":"Sleeve_side_1","center":[0.3775,0.73,0],"size":[0.085,1.26,0.64],"material":"graphite","group":"sleeve"},{"name":"Sleeve_lateral_rail_top_1","center":[0.435,0.73,0.24],"size":[0.03,0.93,0.05],"material":"titanium","group":"sleeve"},{"name":"Sleeve_lateral_rail_bottom_1","center":[0.435,0.73,-0.24],"size":[0.03,0.93,0.05],"material":"titanium","group":"sleeve"},{"name":"Safety_stripe_1_0","center":[0.428,0.22,0],"size":[0.016,0.042,0.18],"material":"safety_yellow","group":"sleeve"},{"name":"Safety_stripe_1_1","center":[0.428,0.312,0],"size":[0.016,0.042,0.18],"material":"safety_yellow","group":"sleeve"},{"name":"Safety_stripe_1_2","center":[0.428,0.404,0],"size":[0.016,0.042,0.18],"material":"safety_yellow","group":"sleeve"},{"name":"Safety_stripe_1_3","center":[0.428,0.496,0],"size":[0.016,0.042,0.18],"material":"safety_yellow","group":"sleeve"},{"name":"Safety_stripe_1_4","center":[0.428,0.588,0],"size":[0.016,0.042,0.18],"material":"safety_yellow","group":"sleeve"},{"name":"Safety_stripe_1_5","center":[0.428,0.6799999999999999,0],"size":[0.016,0.042,0.18],"material":"safety_yellow","group":"sleeve"},{"name":"Safety_stripe_1_6","center":[0.428,0.772,0],"size":[0.016,0.042,0.18],"material":"safety_yellow","group":"sleeve"},{"name":"Safety_stripe_1_7","center":[0.428,0.864,0],"size":[0.016,0.042,0.18],"material":"safety_yellow","group":"sleeve"},{"name":"Safety_stripe_1_8","center":[0.428,0.956,0],"size":[0.016,0.042,0.18],"material":"safety_yellow","group":"sleeve"},{"name":"Safety_stripe_1_9","center":[0.428,1.048,0],"size":[0.016,0.042,0.18],"material":"safety_yellow","group":"sleeve"},{"name":"Slider_side_1","center":[0.2575,1.11,0],"size":[0.055,1.18,0.38],"material":"titanium","group":"slide"},{"name":"Slider_guide_1_-0.11","center":[0.292,1.15,-0.11],"size":[0.014,1,0.026],"material":"ceramic","group":"slide"},{"name":"Slider_guide_1_0.11","center":[0.292,1.15,0.11],"size":[0.014,1,0.026],"material":"ceramic","group":"slide"},{"name":"Slider_measure_1_0","center":[0.293,1.38,0],"size":[0.016,0.012,0.09],"material":"safety_yellow","group":"slide"},{"name":"Slider_measure_1_1","center":[0.293,1.422,0],"size":[0.016,0.012,0.055],"material":"safety_yellow","group":"slide"},{"name":"Slider_measure_1_2","center":[0.293,1.464,0],"size":[0.016,0.012,0.09],"material":"safety_yellow","group":"slide"},{"name":"Slider_measure_1_3","center":[0.293,1.5059999999999998,0],"size":[0.016,0.012,0.055],"material":"safety_yellow","group":"slide"},{"name":"Slider_measure_1_4","center":[0.293,1.5479999999999998,0],"size":[0.016,0.012,0.09],"material":"safety_yellow","group":"slide"},{"name":"Slider_measure_1_5","center":[0.293,1.5899999999999999,0],"size":[0.016,0.012,0.055],"material":"safety_yellow","group":"slide"},{"name":"Slider_measure_1_6","center":[0.293,1.632,0],"size":[0.016,0.012,0.09],"material":"safety_yellow","group":"slide"},{"name":"Slider_measure_1_7","center":[0.293,1.674,0],"size":[0.016,0.012,0.055],"material":"safety_yellow","group":"slide"},{"name":"Rear_bolt_1_-0.44","center":[0.46,0.11,-0.44],"size":[0.04,0.02,0.04],"material":"ceramic","group":"rear"},{"name":"Rear_bolt_1_0.44","center":[0.46,0.11,0.44],"size":[0.04,0.02,0.04],"material":"ceramic","group":"rear"},{"name":"Front_side_accent_1","center":[0.458,1.91,0.24],"size":[0.016,0.12,0.07],"material":"safety_yellow","group":"slide"},{"name":"Sleeve_bottom","center":[0,0.73,-0.2775],"size":[0.67,1.26,0.085],"material":"graphite","group":"sleeve"},{"name":"Sleeve_top_rail_-1_-0.24","center":[-0.24,0.73,-0.335],"size":[0.055,0.93,0.03],"material":"titanium","group":"sleeve"},{"name":"Sleeve_top_rail_-1_0.24","center":[0.24,0.73,-0.335],"size":[0.055,0.93,0.03],"material":"titanium","group":"sleeve"},{"name":"Slider_bottom","center":[0,1.11,-0.1625],"size":[0.46,1.18,0.055],"material":"titanium","group":"slide"},{"name":"Front_top_accent_-1_-0.3","center":[-0.3,1.91,-0.408],"size":[0.04,0.1,0.016],"material":"safety_yellow","group":"slide"},{"name":"Front_top_accent_-1_0.3","center":[0.3,1.91,-0.408],"size":[0.04,0.1,0.016],"material":"safety_yellow","group":"slide"},{"name":"Sleeve_top","center":[0,0.73,0.2775],"size":[0.67,1.26,0.085],"material":"graphite","group":"sleeve"},{"name":"Sleeve_top_rail_1_-0.24","center":[-0.24,0.73,0.335],"size":[0.055,0.93,0.03],"material":"titanium","group":"sleeve"},{"name":"Sleeve_top_rail_1_0.24","center":[0.24,0.73,0.335],"size":[0.055,0.93,0.03],"material":"titanium","group":"sleeve"},{"name":"Slider_top","center":[0,1.11,0.1625],"size":[0.46,1.18,0.055],"material":"titanium","group":"slide"},{"name":"Front_top_accent_1_-0.3","center":[-0.3,1.91,0.408],"size":[0.04,0.1,0.016],"material":"safety_yellow","group":"slide"},{"name":"Front_top_accent_1_0.3","center":[0.3,1.91,0.408],"size":[0.04,0.1,0.016],"material":"safety_yellow","group":"slide"},{"name":"Slider_nose_cap","center":[0,1.76,0],"size":[0.76,0.12,0.56],"material":"ceramic","group":"slide"},{"name":"Front_mount_flange","center":[0,1.91,0],"size":[0.9,0.18,0.8],"material":"titanium","group":"slide"}]}''')
# V2: 8 removable low-friction guide liners inside the stationary sleeve.
# x inner face = 0.312 m; moving guide maximum x = 0.299 m.
# A 13 mm static design clearance remains over the full 550 mm stroke.
# Each liner is seated 3 mm into the stationary wall (wall starts x=0.335).
LINER_INNER_X = 0.312
LINER_OUTER_X = 0.338
SLIDING_GUIDE_OUTER_X = 0.299
LINER_CLEARANCE_M = LINER_INNER_X - SLIDING_GUIDE_OUTER_X
LINER_ANCHOR_OVERLAP_M = LINER_OUTER_X - 0.335
LINER_SPECS = []
for side in (-1, 1):
    for z in (-0.11, 0.11):
        for y in (0.48, 1.06):
            suffix = ("N" if side < 0 else "P") + ("N" if z < 0 else "P") + ("REAR" if y < .75 else "FRONT")
            LINER_SPECS.append({
                "name": "Sleeve_WearLiner_" + suffix,
                "center": [side * 0.325, y, z],
                "size": [0.026, 0.30, 0.060],
                "material": "rubber",
                "group": "sleeve",
            })
SPEC["parts"].extend(LINER_SPECS)
ID = SPEC["id"]

def bounds(part, extension=0.0):
    c = list(part["center"])
    if part["group"] == "slide":
        c[1] += extension
    s = part["size"]
    return ([c[i] - s[i] / 2 for i in range(3)],
            [c[i] + s[i] / 2 for i in range(3)])

def overlap_volume(a, b):
    return math.prod(max(0.0, min(a[1][i], b[1][i]) - max(a[0][i], b[0][i])) for i in range(3))

def validate():
    parts = SPEC["parts"]
    assert len(parts) == len(set(p["name"] for p in parts)), "Duplicate object names"
    assert len(set(p["material"] for p in parts)) >= 3
    for p in parts:
        assert all(v > 0 for v in p["size"]), p["name"]
        assert p["material"] in SPEC["palette"], p["name"]
        b = bounds(p)
        for i, limits in enumerate(((-.5, .5), (0, 2), (-.5, .5))):
            assert b[0][i] >= limits[0] - 1e-8, (p["name"], i, b)
            assert b[1][i] <= limits[1] + 1e-8, (p["name"], i, b)
    fixed = [p for p in parts if p["group"] != "slide"]
    sliding = [p for p in parts if p["group"] == "slide"]
    assert len(LINER_SPECS) == 8
    assert LINER_CLEARANCE_M >= 0.012, LINER_CLEARANCE_M
    assert LINER_ANCHOR_OVERLAP_M >= .002, LINER_ANCHOR_OVERLAP_M
    for liner in LINER_SPECS:
        assert liner in fixed
        # Check the liner's actual face relative to both moving guide strips.
        lo, hi = bounds(liner)
        side = 1 if liner["center"][0] > 0 else -1
        nearest_x = lo[0] if side > 0 else hi[0]
        assert abs(nearest_x - side * LINER_INNER_X) < 1e-8
        assert liner["material"] == "rubber"
    count = 0
    for sample in range(21):
        ext = SPEC["extension_m"] * sample / 20.0
        assert SPEC["sleeve_front_y"] - (SPEC["slider_rear_y"] + ext) >= SPEC["minimum_overlap_m"] - 1e-8
        for moving in sliding:
            for static in fixed:
                assert overlap_volume(bounds(moving, ext), bounds(static)) < 1e-10, (
                    "Unexpected moving/fixed penetration", moving["name"], static["name"], ext)
                count += 1
    return {
        "id": ID, "static_geometry": "PASS", "parts": len(parts),
        "obj_vertices": len(parts) * 8, "obj_quads": len(parts) * 6,
        "materials": sorted(set(p["material"] for p in parts)),
        "envelope_retracted_m": [1.0, 2.0, 1.0],
        "extension_m": SPEC["extension_m"],
        "minimum_overlap_m": SPEC["minimum_overlap_m"],
        "collision_samples": 21, "moving_fixed_aabb_checks": count,
        "moving_fixed_collision": "PASS (conservative AABB; not a Unity physics test)",
        "fixed_wear_liners": len(LINER_SPECS),
        "liner_slider_nominal_clearance_m": round(LINER_CLEARANCE_M, 3),
        "liner_wall_anchor_overlap_m": round(LINER_ANCHOR_OVERLAP_M, 3),
        "moving_slide_sample_envelope": [0, SPEC["extension_m"]],
        "blender": "NOT RUN", "unity": "NOT RUN", "visual": "NOT RUN"
    }

# Face winding follows outward normals for a cube.
FACES = ((0, 3, 2, 1), (4, 5, 6, 7), (0, 1, 5, 4),
         (3, 7, 6, 2), (0, 4, 7, 3), (1, 2, 6, 5))

def corners(p):
    lo, hi = bounds(p)
    x0, y0, z0 = lo
    x1, y1, z1 = hi
    return ((x0, y0, z0), (x1, y0, z0), (x1, y1, z0), (x0, y1, z0),
            (x0, y0, z1), (x1, y0, z1), (x1, y1, z1), (x0, y1, z1))

def export_static_obj(out_dir):
    out_dir.mkdir(parents=True, exist_ok=True)
    obj_path = out_dir / (ID + ".obj")
    mtl_path = out_dir / (ID + ".mtl")
    with mtl_path.open("w", encoding="utf-8") as f:
        f.write("# M03-002 material palette\n")
        for name, mat in SPEC["palette"].items():
            r, g, b = mat["rgb"]
            f.write("newmtl " + name + "\n")
            f.write("Kd %.6f %.6f %.6f\n" % (r, g, b))
            f.write("Ka 0.03 0.03 0.03\nKs 0.25 0.25 0.25\nNs 90\n")
            if mat.get("emission"):
                f.write("Ke %.6f %.6f %.6f\n" % (r, g, b))
            f.write("\n")
    with obj_path.open("w", encoding="utf-8") as f:
        f.write("# M03-002 telescopic beam, retracted static pose\n")
        f.write("mtllib " + mtl_path.name + "\n")
        offset = 1
        for p in SPEC["parts"]:
            f.write("o " + p["name"] + "\nusemtl " + p["material"] + "\n")
            for v in corners(p):
                f.write("v %.6f %.6f %.6f\n" % v)
            for face in FACES:
                f.write("f " + " ".join(str(offset + k) for k in face) + "\n")
            offset += 8
    return obj_path, mtl_path

def blender_build(out_dir):
    import bpy
    from mathutils import Vector
    scene = bpy.context.scene
    old = bpy.data.collections.get(ID + "_ASSET")
    if old is not None:
        for ob in list(old.objects):
            bpy.data.objects.remove(ob, do_unlink=True)
        bpy.data.collections.remove(old)
    col = bpy.data.collections.new(ID + "_ASSET")
    scene.collection.children.link(col)
    materials = {}
    for name, mat in SPEC["palette"].items():
        material = bpy.data.materials.get(ID + "_" + name) or bpy.data.materials.new(ID + "_" + name)
        material.diffuse_color = (*mat["rgb"], 1.0)
        material.use_nodes = True
        bsdf = material.node_tree.nodes.get("Principled BSDF")
        if bsdf:
            bsdf.inputs["Base Color"].default_value = (*mat["rgb"], 1.0)
            bsdf.inputs["Metallic"].default_value = mat["metallic"]
            bsdf.inputs["Roughness"].default_value = mat["roughness"]
            if mat.get("emission"):
                bsdf.inputs["Emission Color"].default_value = (*mat["rgb"], 1.0)
                bsdf.inputs["Emission Strength"].default_value = mat["emission"]
        materials[name] = material
    def empty(name, parent=None):
        ob = bpy.data.objects.new(name, None)
        col.objects.link(ob)
        ob.empty_display_type = "PLAIN_AXES"
        ob.empty_display_size = .12
        if parent is not None:
            ob.parent = parent
        return ob
    mount = empty(ID + "_MOUNT_REAR")
    mount["port"] = "REAR_FRAME_FACE_Y0"
    mount["retracted_envelope_m"] = "1x2x1"
    mount["axis"] = "+Y longitudinal, +Z up (Blender)"
    slide = empty(ID + "_SLIDER", mount)
    slide["max_extension_m"] = SPEC["extension_m"]
    slide["minimum_overlap_m"] = SPEC["minimum_overlap_m"]
    front = empty(ID + "_MOUNT_FRONT", slide)
    front.location = (0, 2, 0)
    front["port"] = "FRONT_FRAME_FACE"
    for p in SPEC["parts"]:
        bpy.ops.mesh.primitive_cube_add(size=1, location=tuple(p["center"]))
        ob = bpy.context.object
        ob.name = ID + "_" + p["name"]
        for old_col in list(ob.users_collection):
            old_col.objects.unlink(ob)
        col.objects.link(ob)
        ob.dimensions = tuple(p["size"])
        bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
        ob.data.materials.append(materials[p["material"]])
        # Rounded manufactured edges; bevel is applied so exported GLB has actual geometry.
        bev = ob.modifiers.new("Machined_edge_bevel", "BEVEL")
        bev.width = min(.012, min(p["size"]) * .12)
        bev.segments = 3
        bpy.context.view_layer.objects.active = ob
        bpy.ops.object.modifier_apply(modifier=bev.name)
        ob.parent = slide if p["group"] == "slide" else mount
        # Parent empties have identity transforms at generation; local coordinates stay unchanged.
        ob["module_id"] = ID
        ob["role"] = p["group"]
    for frame, ext in ((1, 0.0), (30, SPEC["extension_m"]), (60, 0.0)):
        slide.location.y = ext
        slide.keyframe_insert(data_path="location", index=1, frame=frame)
    if slide.animation_data and slide.animation_data.action:
        action = slide.animation_data.action
        # Blender 4.x/5.x support different action APIs; keyframes already exist if this is unavailable.
        try:
            for fc in action.fcurves:
                for kp in fc.keyframe_points:
                    kp.interpolation = "BEZIER"
        except AttributeError:
            pass
    scene.frame_start = 1
    scene.frame_end = 60
    scene.frame_set(1)
    bpy.ops.wm.save_as_mainfile(filepath=str(out_dir / (ID + ".blend")))
    # CRITICAL: default glTF export exports the entire open Blender scene
    # (camera, lights, unrelated vehicle parts, other model collections).
    # Restrict export to this asset's root, slider, sockets and own meshes.
    # Do not mutate another artist's scene or selection on return.
    prior_selection = tuple(bpy.context.selected_objects)
    prior_active = bpy.context.view_layer.objects.active
    try:
        bpy.ops.object.select_all(action="DESELECT")
        for obj in col.objects:
            obj.select_set(True)
        bpy.context.view_layer.objects.active = mount
        bpy.ops.export_scene.gltf(
            filepath=str(out_dir / (ID + "_animated.glb")),
            export_format="GLB",
            use_selection=True,
            export_animations=True,
            export_yup=True,
        )
    finally:
        bpy.ops.object.select_all(action="DESELECT")
        for obj in prior_selection:
            if obj.name in bpy.data.objects:
                obj.select_set(True)
        if prior_active is not None and prior_active.name in bpy.data.objects:
            bpy.context.view_layer.objects.active = prior_active
    return {"blend": str(out_dir / (ID + ".blend")),
            "animated_glb": str(out_dir / (ID + "_animated.glb"))}

def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else sys.argv[1:]
    parser = argparse.ArgumentParser(description="M03-002 procedural telescopic structural beam")
    parser.add_argument("--output-dir", default=str(Path(__file__).resolve().parent))
    parser.add_argument("--self-test", action="store_true")
    parser.add_argument("--require-blender", action="store_true")
    args = parser.parse_args(argv)
    output = Path(args.output_dir).resolve()
    qa = validate()
    obj, mtl = export_static_obj(output)
    qa["obj"] = str(obj)
    qa["mtl"] = str(mtl)
    if not args.self_test:
        try:
            import bpy
        except ImportError:
            if args.require_blender:
                raise RuntimeError("Blender bpy unavailable; animated GLB and .blend NOT RUN")
        else:
            qa["blender_outputs"] = blender_build(output)
            qa["blender"] = "EXECUTED"
    (output / (ID + ".static-qa.json")).write_text(json.dumps(qa, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(qa, indent=2))

if __name__ == "__main__":
    main()
