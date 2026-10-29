#!/usr/bin/env python3
"""ENV01-001 — v002 revised original static steel/concrete protective barrier.
Blender Z-up, +Y = road-facing side, +X = barrier length.
1 Blender unit = 1 metre. Ground-centred pivot (0, 0, 0).
No game scripts, animations, textures, external geometry or downloaded assets.
Copyright 2026 RoboRougelike project contributors. SPDX-License-Identifier: CC0-1.0
"""
import bpy
import bmesh
import hashlib
import json
import math
import sys
from pathlib import Path
from mathutils import Vector

ID = "ENV01-001"
STEM = "ENV01-001_protective_barrier"
ROOT_NAME = "ENV01_001_ROOT"
REVISION = "v002"
OUT = Path(__file__).resolve().parent / "generated" / REVISION
if "--" in sys.argv:
    args = sys.argv[sys.argv.index("--") + 1:]
    if "--out" in args:
        OUT = Path(args[args.index("--out") + 1]).resolve()
OUT.mkdir(parents=True, exist_ok=True)

bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete(use_global=False)
for mesh in list(bpy.data.meshes):
    if mesh.users == 0:
        bpy.data.meshes.remove(mesh)
for material in list(bpy.data.materials):
    if material.users == 0:
        bpy.data.materials.remove(material)
scene = bpy.context.scene
scene.unit_settings.system = "METRIC"
scene.unit_settings.scale_length = 1.0
root = bpy.data.objects.new(ROOT_NAME, None)
scene.collection.objects.link(root)
root.empty_display_type = "PLAIN_AXES"
root.empty_display_size = 0.22

def make_material(name, color, metallic, roughness):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = (*color, 1.0)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (*color, 1.0)
    bsdf.inputs["Metallic"].default_value = metallic
    bsdf.inputs["Roughness"].default_value = roughness
    return mat

concrete = make_material("01_CONCRETE_COOL_GRAY", (0.43, 0.47, 0.48), 0.0, 0.93)
steel = make_material("02_STEEL_GRAPHITE", (0.11, 0.15, 0.18), 0.66, 0.48)
safety = make_material("03_SAFETY_AMBER", (0.95, 0.37, 0.075), 0.16, 0.52)

def finish(obj, material):
    obj.data.materials.append(material)
    obj.parent = root
    return obj

def block(name, location, dimensions, material, bevel=0.0):
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = dimensions
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel > 0:
        mod = obj.modifiers.new("single_segment_chamfer", "BEVEL")
        mod.width = bevel
        mod.segments = 1
        mod.affect = "EDGES"
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=mod.name)
    return finish(obj, material)

def cross_section_prism(name, x_min, x_max, yz_points, material):
    n = len(yz_points)
    vertices = [(x_min, y, z) for y, z in yz_points]
    vertices += [(x_max, y, z) for y, z in yz_points]
    faces = [tuple(range(n - 1, -1, -1)), tuple(range(n, 2 * n))]
    for i in range(n):
        j = (i + 1) % n
        faces.append((i, j, j + n, i + n))
    mesh = bpy.data.meshes.new(name + "_mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    bm = bmesh.new()
    bm.from_mesh(mesh)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new(name, mesh)
    scene.collection.objects.link(obj)
    return finish(obj, material)

def diagonal(name, start, end, thickness, material):
    a, b = Vector(start), Vector(end)
    obj = block(name, (a + b) * 0.5, (thickness, thickness, (b - a).length), material, 0.008)
    obj.rotation_mode = "QUATERNION"
    obj.rotation_quaternion = Vector((0, 0, 1)).rotation_difference((b - a).normalized())
    return obj

def bolt(name, x, y, z, front=True):
    bpy.ops.mesh.primitive_cylinder_add(vertices=8, radius=0.017, depth=0.012, location=(x, y, z))
    obj = bpy.context.object
    obj.name = name
    obj.rotation_euler[0] = math.pi / 2
    return finish(obj, steel)

# Original 6-sided sloped Jersey-style barrier section, intentionally low and legible.
# Y is depth, Z is height; concrete remains a closed watertight prism.
cross_section_prism(
    "Concrete_low_flared_core", -1.16, 1.16,
    [(-0.30, 0.00), (0.30, 0.00), (0.30, 0.16),
     (0.255, 0.29), (0.16, 0.68), (-0.16, 0.68),
     (-0.255, 0.29), (-0.30, 0.16)],
    concrete,
)

# Static steel frame behind the concrete: posts, feet, upper tie beam and braces.
for side, x in (("L", -1.06), ("R", 1.06)):
    block("Foot_plate_" + side, (x, -0.42, 0.0425), (0.23, 0.24, 0.085), steel, 0.014)
    block("Rear_post_" + side, (x, -0.355, 0.43), (0.095, 0.095, 0.73), steel, 0.012)
    block("Concrete_end_steel_strap_" + side,
          (math.copysign(1.22, x), 0.0, 0.32), (0.10, 0.64, 0.58), steel, 0.012)
    diagonal("Rear_brace_" + side, (x, -0.355, 0.18),
             (x * 0.68, -0.355, 0.74), 0.055, steel)
block("Rear_top_steel_tie", (0, -0.355, 0.79), (2.32, 0.095, 0.09), steel, 0.012)

# Two sacrificial face rails, reinforced end caps and high-contrast reflectors.
block("Front_impact_rail", (0, 0.27, 0.43), (2.36, 0.085, 0.105), steel, 0.012)
block("Front_toe_rail", (0, 0.322, 0.19), (2.36, 0.06, 0.062), steel, 0.009)
for i, x in enumerate((-0.81, -0.27, 0.27, 0.81)):
    block("Amber_reflector_%02d" % (i + 1),
          (x, 0.316, 0.432), (0.20, 0.018, 0.068), safety, 0.004)
for i, x in enumerate((-1.02, -0.62, -0.22, 0.22, 0.62, 1.02)):
    bolt("Front_bolt_%02d" % (i + 1), x, 0.322, 0.43)
for side, x in (("L", -1.06), ("R", 1.06)):
    for j, z in enumerate((0.25, 0.72)):
        bolt("Rear_bolt_" + side + str(j), x, -0.409, z, front=False)

# Four flush amber top markers improve recognition from an elevated gameplay camera.
# Marker undersides touch the concrete top at Z=0.68; no coplanar visible faces.
for i, x in enumerate((-0.84, -0.28, 0.28, 0.84)):
    block("Top_visibility_marker_%02d" % (i + 1),
          (x, 0.065, 0.69), (0.22, 0.085, 0.02), safety, 0.003)

# Apply rotations on all meshes, retaining the empty at ground-centred origin.
for obj in [o for o in scene.objects if o.type == "MESH"]:
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    obj.select_set(False)

mesh_objects = [o for o in scene.objects if o.type == "MESH"]
triangle_count = sum(sum(len(p.vertices) - 2 for p in o.data.polygons) for o in mesh_objects)
assert 200 <= triangle_count <= 1500, "Triangle budget: %s" % triangle_count
assert len({slot.material.name for o in mesh_objects for slot in o.material_slots}) == 3

# Only mesh + root are exported; Blender converts +Z up to glTF +Y up.
bpy.ops.object.select_all(action="DESELECT")
root.select_set(True)
for obj in mesh_objects:
    obj.select_set(True)
bpy.context.view_layer.objects.active = root
glb = OUT / (STEM + ".glb")
bpy.ops.export_scene.gltf(filepath=str(glb), export_format="GLB",
                          use_selection=True, export_yup=True,
                          export_cameras=False, export_lights=False)
bpy.ops.object.select_all(action="DESELECT")

# Two actual Workbench camera renders, front and rear, on a plain studio backdrop.
camera_data = bpy.data.cameras.new("ENV01_PREVIEW_CAMERA")
camera = bpy.data.objects.new("ENV01_PREVIEW_CAMERA", camera_data)
scene.collection.objects.link(camera)
scene.camera = camera
camera_data.type = "ORTHO"
camera_data.ortho_scale = 3.50
scene.render.engine = "BLENDER_WORKBENCH"
scene.display.shading.light = "STUDIO"
scene.display.shading.color_type = "MATERIAL"
scene.display.shading.show_shadows = True
scene.display.shading.show_cavity = True
scene.render.resolution_x = 960
scene.render.resolution_y = 640
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = "PNG"
scene.render.film_transparent = False
for name, location in (
    ("preview_front.png", (3.1, 4.8, 3.5)),
    ("preview_rear.png", (-3.1, -4.8, 3.5)),
):
    camera.location = location
    direction = Vector((0.0, 0.0, 0.38)) - camera.location
    camera.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
    scene.render.filepath = str(OUT / name)
    bpy.ops.render.render(write_still=True)

# The native Blender source includes preview camera but no animation/game logic.
bpy.ops.wm.save_as_mainfile(filepath=str(OUT / (STEM + ".blend")), compress=True)
report = {
    "id": ID, "revision": REVISION, "asset_type": "STATIC_ENVIRONMENT_ONLY",
    "triangles": triangle_count, "mesh_objects": len(mesh_objects),
    "materials": [m.name for m in (concrete, steel, safety)],
    "units": "1 Blender unit = 1 metre",
    "blender_axes": "+X length, +Y road-facing, +Z up",
    "glb_axes": "+X length, -Z road-facing, +Y up",
    "root_pivot_m": [0, 0, 0],
    "suggested_box_collider_blender_m": {
        "center": [0.0, -0.09, 0.42], "size": [2.62, 1.00, 0.90]
    },
    "animation": "NOT_APPLICABLE",
    "v001_source_commit": "265835419a7144799cc92fb4a7dac637edbb7cc3",
    "geometry_changes": ["ground-contact feet and concrete", "rear steel frame offset from concrete", "shorter frame silhouette", "four upper amber visibility markers", "end straps offset from concrete", "enclosing collider envelope"],
    "license": "CC0-1.0; original geometry and script, no third-party models",
    "source_file_sha256": hashlib.sha256(Path(__file__).read_bytes()).hexdigest(),
}
(OUT / "build_report.json").write_text(json.dumps(report, indent=2) + "\n")
print("ENV01_001_GENERATED", json.dumps(report, sort_keys=True))
