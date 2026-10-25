#!/usr/bin/env python3
"""M01-016 — Fold-out rotary cannon. Blender 3.6+/4.x, no third-party packages.
Blender coordinates: Z up, -Y forward; GLB exported Y-up (+Z forward).
Origin (0,0,0) is the TOP MOUNT socket; 1 Blender unit = 1 Unity metre.
Run: blender -b --python generate_m01_016.py -- --out <directory> [--repeat]
"""
import argparse
import json
import math
import sys
from pathlib import Path

import bpy
import bmesh
from mathutils import Vector

ID = "M01-016"
FRAMES = 73
GROUPS = ("STATIC", "ROTOR", "LEFT", "RIGHT")
COLORS = {
    "Graphite": ((0.055, 0.072, 0.090, 1), 0.78, 0.34),
    "Titanium": ((0.31, 0.38, 0.43, 1), 0.82, 0.27),
    "Ceramic": ((0.82, 0.86, 0.79, 1), 0.18, 0.36),
    "Copper": ((0.55, 0.21, 0.085, 1), 0.79, 0.24),
    "SignalAmber": ((0.92, 0.32, 0.025, 1), 0.23, 0.3),
    "EmissionCyan": ((0.02, 0.67, 0.79, 1), 0.1, 0.18),
    "Rubber": ((0.025, 0.027, 0.030, 1), 0.0, 0.82),
}
materials = {}
parts = []
groups = {}

def reset():
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    for material in list(bpy.data.materials):
        bpy.data.materials.remove(material)
    for action in list(bpy.data.actions):
        bpy.data.actions.remove(action)
    bpy.context.scene.frame_set(1)
    bpy.context.scene.render.fps = 60
    bpy.context.scene.frame_start = 1
    bpy.context.scene.frame_end = FRAMES

def mat(name, rgba, metallic, roughness):
    m = bpy.data.materials.new(name)
    m.diffuse_color = rgba
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = rgba
    bsdf.inputs["Metallic"].default_value = metallic
    bsdf.inputs["Roughness"].default_value = roughness
    if name == "EmissionCyan":
        m.node_tree.nodes.new("ShaderNodeEmission").inputs["Color"].default_value = rgba
        # Principled emission is optional; colour remains readable without bloom.
        if "Emission Color" in bsdf.inputs:
            bsdf.inputs["Emission Color"].default_value = rgba
            bsdf.inputs["Emission Strength"].default_value = 2.4
    materials[name] = m

def finish(obj, material, group, parent=None, bevel=0):
    obj.name = ID + "_" + obj.name
    if bevel:
        mod = obj.modifiers.new("Machined_edge_radius", 'BEVEL')
        mod.width = bevel
        mod.segments = 2
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=mod.name)
    obj.data.materials.append(materials[material])
    if parent is not None:
        obj.parent = parent
        obj.matrix_parent_inverse.identity()
    obj["qa_group"] = group
    for poly in obj.data.polygons:
        poly.use_smooth = len(poly.vertices) > 4 and material not in ("Ceramic", "Graphite")
    parts.append(obj)
    return obj

def cube(name, loc, scale, material, group="STATIC", parent=None, bevel=.012):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return finish(obj, material, group, parent, bevel)

def cyl(name, loc, radius, depth, material, group="STATIC", parent=None,
        vertices=24, axis="Z"):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius,
                                        depth=depth, location=loc)
    obj = bpy.context.object
    obj.name = name
    if axis == "Y":
        obj.rotation_euler[0] = math.pi / 2
        bpy.ops.object.transform_apply(location=False, rotation=True, scale=False)
    return finish(obj, material, group, parent)

def torus(name, loc, major, minor, material, group="STATIC", parent=None, axis="Y"):
    bpy.ops.mesh.primitive_torus_add(major_segments=32, minor_segments=8,
        location=loc, major_radius=major, minor_radius=minor)
    obj = bpy.context.object
    obj.name = name
    if axis == "Y":
        obj.rotation_euler[0] = math.pi / 2
        bpy.ops.object.transform_apply(location=False, rotation=True, scale=False)
    return finish(obj, material, group, parent)

def pivot(name, location):
    o = bpy.data.objects.new(ID + "_" + name, None)
    bpy.context.scene.collection.objects.link(o)
    o.location = location
    o.empty_display_type = "ARROWS"
    o.empty_display_size = .09
    return o

def animate(o, axis, keys):
    for frame, val in keys:
        o.rotation_euler[axis] = val
        o.keyframe_insert(data_path="rotation_euler", index=axis, frame=frame)
    if o.animation_data and o.animation_data.action:
        for curve in o.animation_data.action.fcurves:
            for key in curve.keyframe_points:
                key.interpolation = "LINEAR"

def build():
    reset()
    for name, params in COLORS.items():
        mat(name, *params)
    root = pivot("MOUNT_TOP_SOCKET", (0, 0, 0))
    groups["root"] = root
    # Top socket's bottom face lies at z=0; all functional geometry is above.
    cyl("socket_octagonal", (0, .05, .065), .29, .13, "Titanium", vertices=8)
    cyl("socket_lock_ring", (0, .05, .15), .24, .045, "Copper", vertices=32)
    cube("low_profile_main_hull", (0, .08, .53), (.75, .94, .48), "Graphite", bevel=.06)
    cube("ceramic_dorsal_plate", (0, .13, .79), (.54, .65, .06), "Ceramic", bevel=.025)
    cube("rear_electronics", (0, .51, .46), (.48, .16, .29), "Titanium", bevel=.035)
    for side in (-1, 1):
        s = "L" if side < 0 else "R"
        cube(s+"_upper_cheek", (side*.32, .10, .70), (.09, .64, .13), "Ceramic", bevel=.018)
        cube(s+"_lower_strake", (side*.31, -.02, .35), (.075, .55, .08), "Titanium")
        for k in range(3):
            cube(s+"_rear_cooling_rib_"+str(k), (side*.29, .39-k*.13, .81),
                 (.12, .046, .045), "Copper", bevel=.008)
        cyl(s+"_hinge_bearing", (side*.57, -.10, .60), .10, .18, "Titanium")
        cyl(s+"_hinge_center", (side*.57, -.10, .70), .05, .035, "SignalAmber")
        cube(s+"_signal_panel", (side*.25, .30, .80), (.12, .13, .018), "SignalAmber")
    # The rotor is forward of the fixed hull; clearance to static parts >= .10 m.
    rotor = pivot("ROTARY_DRUM_Y_AXIS", (0, -.54, .62))
    groups["rotor"] = rotor
    cyl("rotor_spindle", (0, -.57, 0), .08, .19, "Titanium", "ROTOR", rotor, axis="Y")
    for i in range(4):
        a = i*math.pi/2
        x, z = .125*math.cos(a), .125*math.sin(a)
        cyl("barrel_"+str(i), (x, -.67, z), .044, 1.24,
            "Graphite", "ROTOR", rotor, vertices=24, axis="Y")
        cyl("muzzle_ring_"+str(i), (x, -1.265, z), .056, .045,
            "Copper", "ROTOR", rotor, axis="Y")
        torus("barrel_heat_ring_"+str(i), (x, -.43, z), .051, .009,
              "EmissionCyan", "ROTOR", rotor)
    for y in (-.27, -1.12):
        torus("rotor_brace_"+str(y), (0, y, 0), .213, .022,
              "Titanium", "ROTOR", rotor)
    cyl("rotor_front_hub", (0, -1.21, 0), .079, .09, "Copper",
        "ROTOR", rotor, axis="Y")
    animate(rotor, 1, [(1 + i*4.5, i*math.pi/2) for i in range(17)])
    # Two separate hinged arms; fold outward, never into the fixed hull.
    for side, label in ((-1,"LEFT"), (1,"RIGHT")):
        arm = pivot(label+"_FOLD_Z_AXIS", (side*.57, -.10, .60))
        groups[label.lower()] = arm
        cube(label+"_beam", (side*.23, -.38, .02), (.16, .64, .16),
             "Titanium", label, arm, bevel=.025)
        cube(label+"_upper_rail", (side*.23, -.37, .15), (.19, .59, .055),
             "Graphite", label, arm, bevel=.016)
        for j in range(4):
            cube(label+"_segment_shield_"+str(j),
                 (side*.23, -.13-j*.155, .205),
                 (.205, .115, .055), "Ceramic", label, arm, bevel=.014)
        for j in range(3):
            cube(label+"_copper_fin_"+str(j),
                 (side*.23, -.20-j*.18, -.087),
                 (.145, .04, .048), "Copper", label, arm, bevel=.006)
        cyl(label+"_front_emitter", (side*.23, -.75, .035), .09, .065,
            "Graphite", label, arm, axis="Y")
        torus(label+"_front_emission", (side*.23, -.79, .035), .079, .012,
              "EmissionCyan", label, arm)
        cube(label+"_amber_warning", (side*.23, -.50, .245),
             (.11, .045, .012), "SignalAmber", label, arm, bevel=.004)
        animate(arm, 2, [(1,0), (26,side*math.radians(30)),
                         (50,side*math.radians(30)), (73,0)])
    return root

def bounds(o):
    corners = [o.matrix_world @ Vector(v) for v in o.bound_box]
    return [(min(v[i] for v in corners), max(v[i] for v in corners)) for i in range(3)]

def separated(a,b,eps=.001):
    return any(a[i][1] + eps <= b[i][0] or b[i][1] + eps <= a[i][0] for i in range(3))

def validate():
    meshes = [o for o in bpy.data.objects if o.type=="MESH" and o.name.startswith(ID)]
    assert len(meshes) >= 40, "Too few detailed meshes"
    assert len(materials) >= 3
    assert groups["root"].location.length < 1e-8
    for o in meshes:
        bm = bmesh.new()
        bm.from_mesh(o.data)
        assert len(bm.faces) and all(f.calc_area() > 1e-9 for f in bm.faces), o.name
        assert all(e.is_manifold for e in bm.edges), "Nonmanifold: "+o.name
        bm.free()
    # Conservative world AABBs for moving vs fixed and opposite arm.
    # Components within a single articulated group intentionally touch.
    checks = 0
    min_gap = 999
    for frame in range(1, FRAMES+1):
        bpy.context.scene.frame_set(frame)
        bpy.context.view_layer.update()
        boxes = [(o, bounds(o)) for o in meshes]
        for i,(a,ba) in enumerate(boxes):
            ga = a["qa_group"]
            for b,bb in boxes[i+1:]:
                gb = b["qa_group"]
                if ga == gb:
                    continue
                # Only check moving assemblies against stationary or another assembly.
                checks += 1
                if not separated(ba,bb):
                    raise AssertionError("AABB collision frame %d: %s (%s) / %s (%s)" %
                                         (frame,a.name,ga,b.name,gb))
                gap = max(max(ba[k][0]-bb[k][1], bb[k][0]-ba[k][1], 0) for k in range(3))
                if gap > 0:
                    min_gap = min(min_gap, gap)
    bpy.context.scene.frame_set(1)
    tris = sum(len(p.vertices)-2 for o in meshes for p in o.data.polygons)
    verts = sum(len(o.data.vertices) for o in meshes)
    return {"id":ID,"meshes":len(meshes),"materials":len(materials),
            "vertices":verts,"triangles":tris,"frames_tested":FRAMES,
            "aabb_pair_checks":checks,"aabb_intersections":0,
            "minimum_axis_separation_m":round(min_gap,5),
            "mount":"MOUNT_TOP_SOCKET","mount_xyz_m":[0,0,0],
            "axis_blender":"Z up, -Y forward",
            "axis_gltf_unity":"Y up, +Z forward",
            "fold_degrees_each":30,"rotor_revolutions":4,
            "note":"Conservative AABB sampling, not continuous mesh collision detection"}

def export(out):
    out.mkdir(parents=True, exist_ok=True)
    stem = out / "M01-016_foldout_rotary_cannon"
    bpy.context.scene.frame_set(1)
    bpy.ops.wm.save_as_mainfile(filepath=str(stem)+".blend")
    bpy.ops.export_scene.gltf(filepath=str(stem)+".glb", export_format="GLB",
                              export_animations=True, export_yup=True)
    bpy.ops.export_scene.fbx(filepath=str(stem)+".fbx", use_selection=False,
                              add_leaf_bones=False, axis_forward="-Z", axis_up="Y",
                              bake_anim=True)
    if hasattr(bpy.ops.wm, "obj_export"):
        bpy.ops.wm.obj_export(filepath=str(stem)+".obj",
                              forward_axis="NEGATIVE_Z", up_axis="Y")
    else:
        bpy.ops.export_scene.obj(filepath=str(stem)+".obj",
                                 axis_forward="-Z", axis_up="Y", use_materials=True)
    return stem

def main():
    raw = sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else []
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", default="Tools/ModelFactory/M01/M01-016/generated")
    ap.add_argument("--repeat", action="store_true")
    args = ap.parse_args(raw)
    build()
    report = validate()
    if args.repeat:
        before = (len([o for o in bpy.data.objects if o.type=="MESH"]),len(bpy.data.materials))
        build()
        after = (len([o for o in bpy.data.objects if o.type=="MESH"]),len(bpy.data.materials))
        assert before == after, "Repeated build leaked objects or materials"
        report = validate()
        report["repeatability"] = "PASS"
    stem = export(Path(args.out))
    report["exports"] = [str(stem)+ext for ext in (".blend",".glb",".fbx",".obj",".mtl")]
    Path(str(stem)+"_qa.json").write_text(json.dumps(report,indent=2)+"\n",encoding="utf-8")
    print("M01-016 QA PASS:",json.dumps(report,sort_keys=True))

if __name__ == "__main__":
    main()
