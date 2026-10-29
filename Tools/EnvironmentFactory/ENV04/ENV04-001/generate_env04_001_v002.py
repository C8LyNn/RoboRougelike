#!/usr/bin/env python3
"""ENV04-001 v002 — refined low industrial sci-fi ventilation crate / grate.
Blender +Z up, +Y front, +X width; 1 unit = 1 metre.
Static environment concept, no mechanics or animation.
SPDX-License-Identifier: CC0-1.0
"""
import bpy
import hashlib
import json
import math
import sys
from pathlib import Path
from mathutils import Vector

ASSET_ID = "ENV04-001"
STEM = "ENV04-001_industrial_vent_grate"
ROOT_NAME = "ENV04_001_ROOT"
OUT = Path(__file__).resolve().parent / "generated" / "v002"
if "--" in sys.argv:
    argv = sys.argv[sys.argv.index("--") + 1:]
    if "--out" in argv:
        OUT = Path(argv[argv.index("--out") + 1]).resolve()
OUT.mkdir(parents=True, exist_ok=True)

bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete(use_global=False)
scene = bpy.context.scene
scene.unit_settings.system = "METRIC"
scene.unit_settings.scale_length = 1.0
root = bpy.data.objects.new(ROOT_NAME, None)
scene.collection.objects.link(root)
root.empty_display_type = "PLAIN_AXES"
root.empty_display_size = 0.18

def material(name, rgb, metallic, roughness):
    m = bpy.data.materials.new(name)
    m.diffuse_color = (*rgb, 1.0)
    m.use_nodes = True
    shader = m.node_tree.nodes.get("Principled BSDF")
    shader.inputs["Base Color"].default_value = (*rgb, 1.0)
    shader.inputs["Metallic"].default_value = metallic
    shader.inputs["Roughness"].default_value = roughness
    return m

graphite = material("01_GRAPHITE_POWDERCOAT", (0.105, 0.137, 0.155), 0.55, 0.65)
alloy = material("02_BRUSHED_STEEL", (0.49, 0.56, 0.57), 0.77, 0.40)
amber = material("03_SAFETY_AMBER", (0.90, 0.34, 0.055), 0.18, 0.55)

def finish(obj, mat):
    obj.data.materials.append(mat)
    obj.parent = root
    return obj

def box(name, xyz, dims, mat, bevel=0.0, tilt_x=0.0):
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=xyz)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = dims
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel:
        mod = obj.modifiers.new("lowpoly_edge_chamfer", "BEVEL")
        mod.width = bevel
        mod.segments = 1
        mod.affect = "EDGES"
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=mod.name)
    obj.rotation_euler.x = math.radians(tilt_x)
    return finish(obj, mat)

def bolt(name, x, y, z):
    bpy.ops.mesh.primitive_cylinder_add(vertices=10, radius=0.022,
                                        depth=0.014, location=(x, y, z))
    obj = bpy.context.object
    obj.name = name
    return finish(obj, graphite)

# Ground-contact plinth, deliberately dark recessed well below the grate.
box("Vent_lower_plinth", (0, 0, 0.065), (1.56, 1.00, 0.13), graphite, 0.018)
box("Vent_recess_floor", (0, 0, 0.1475), (1.24, 0.70, 0.035), graphite, 0.008)

# Four readable load-bearing uprights and the side shell.
for sx, label_x in ((-1, "L"), (1, "R")):
    for sy, label_y in ((-1, "B"), (1, "F")):
        box("Corner_post_" + label_x + label_y,
            (sx * 0.696, sy * 0.425, 0.2775),
            (0.092, 0.092, 0.295), graphite, 0.010)
    box("Side_armor_" + label_x, (sx * 0.622, 0, 0.27),
        (0.055, 0.74, 0.19), graphite, 0.006)
for sy, side in ((-1, "rear"), (1, "front")):
    box("End_armor_" + side, (0, sy * 0.373, 0.275),
        (1.16, 0.055, 0.19), graphite, 0.006)

# Four-piece raised lip leaves a genuine open slot field at the top.
for sx, label in ((-1, "L"), (1, "R")):
    box("Raised_grate_side_rail_" + label,
        (sx * 0.723, 0, 0.471), (0.104, 1.02, 0.092), alloy, 0.011)
for sy, label in ((-1, "rear"), (1, "front")):
    box("Raised_grate_end_rail_" + label,
        (0, sy * 0.459, 0.471), (1.342, 0.102, 0.092), alloy, 0.011)

# Six thick stationary canted vanes: large slots remain legible from game camera.
# No fan, airflow, hazard, animation, or fake mechanical behavior.
for i, y in enumerate((-0.335, -0.201, -0.067, 0.067, 0.201, 0.335)):
    box("Static_louver_%02d" % (i + 1),
        (0, y, 0.472), (1.30, 0.085, 0.055),
        alloy, 0.008, tilt_x=28.0)
    # Thin flush end brackets bridge each louver to the inside of the side rails.
    # Contact faces are coplanar, not buried inside the slat or rail geometry.
    for sx, side in ((-1, "L"), (1, "R")):
        box("Louver_%02d_mount_%s" % (i + 1, side),
            (sx * 0.6605, y, 0.472), (0.021, 0.062, 0.034),
            graphite, bevel=0.0)

# Restrained industrial identification markings, not glowing game effects.
for x, side in ((-0.54, "L"), (0.54, "R")):
    box("Front_amber_mark_" + side, (x, 0.4065, 0.272),
        (0.235, 0.014, 0.052), amber, 0.004)
    box("Rear_amber_mark_" + side, (x, -0.4065, 0.272),
        (0.235, 0.014, 0.052), amber, 0.004)

# Four coarse bolt heads on the top rim.
for x, side in ((-0.723, "L"), (0.723, "R")):
    for y, end in ((-0.455, "B"), (0.455, "F")):
        bolt("Top_fastener_" + side + end, x, y, 0.524)

# Apply object rotations/scales, keeping the intentionally ground-centred root.
mesh_objects = [obj for obj in scene.objects if obj.type == "MESH"]
for obj in mesh_objects:
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    obj.select_set(False)

# Explicit metric, world-projected UVs: 1 UV tile = 1 metre on each face.
# This avoids stretched box/cylinder default UVs, including on chamfer faces.
for obj in mesh_objects:
    mesh = obj.data
    while mesh.uv_layers:
        mesh.uv_layers.remove(mesh.uv_layers[0])
    uv_layer = mesh.uv_layers.new(name="UV_METRIC_1M")
    for face in mesh.polygons:
        normal = obj.matrix_world.to_3x3() @ face.normal
        axis = max(range(3), key=lambda i: abs(normal[i]))
        for li in face.loop_indices:
            co = obj.matrix_world @ mesh.vertices[mesh.loops[li].vertex_index].co
            if axis == 0:
                uv_layer.data[li].uv = (co.y, co.z)
            elif axis == 1:
                uv_layer.data[li].uv = (co.x, co.z)
            else:
                uv_layer.data[li].uv = (co.x, co.y)

triangle_count = sum(sum(len(poly.vertices) - 2 for poly in obj.data.polygons)
                     for obj in mesh_objects)
assert 200 <= triangle_count <= 1500, "Triangle budget: %d" % triangle_count
assert len({slot.material.name for obj in mesh_objects
            for slot in obj.material_slots}) == 3
assert not bpy.data.actions

# Only static mesh hierarchy is exported, never preview cameras/lights.
bpy.ops.object.select_all(action="DESELECT")
root.select_set(True)
for obj in mesh_objects:
    obj.select_set(True)
bpy.context.view_layer.objects.active = root
glb_path = OUT / (STEM + ".glb")
bpy.ops.export_scene.gltf(filepath=str(glb_path), export_format="GLB",
                          use_selection=True, export_yup=True,
                          export_cameras=False, export_lights=False)
bpy.ops.object.select_all(action="DESELECT")

# Two real, different workbench renders for human visual review.
cam_data = bpy.data.cameras.new("ENV04_PREVIEW_CAMERA")
cam = bpy.data.objects.new("ENV04_PREVIEW_CAMERA", cam_data)
scene.collection.objects.link(cam)
scene.camera = cam
cam_data.type = "ORTHO"
cam_data.ortho_scale = 2.65
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
for filename, pos in (
    ("preview_front.png", (2.6, 3.4, 2.2)),
    ("preview_rear.png", (-2.6, -3.4, 2.2)),
    ("preview_top.png", (1.0, 1.3, 4.0)),
    ("preview_side.png", (2.6, 3.4, 0.95)),
):
    cam.location = pos
    cam.rotation_euler = (Vector((0, 0, 0.26)) - cam.location).to_track_quat("-Z", "Y").to_euler()
    scene.render.filepath = str(OUT / filename)
    bpy.ops.render.render(write_still=True)

bpy.ops.wm.save_as_mainfile(filepath=str(OUT / (STEM + ".blend")), compress=True)
report = {
    "id": ASSET_ID, "revision": "v002", "asset_type": "STATIC_ENVIRONMENT_CONCEPT",
    "mesh_objects": len(mesh_objects), "triangles": triangle_count,
    "materials": [graphite.name, alloy.name, amber.name],
    "unit": "1 Blender unit = 1 m",
    "blender_axes": "+X width, +Y front, +Z up",
    "glb_axes": "+X width, -Z front, +Y up",
    "root_pivot_m": [0, 0, 0],
    "suggested_single_box_collider_blender_m": {
        "center": [0, 0, 0.27], "size": [1.65, 1.08, 0.58]
    },
    "animation": "NOT_APPLICABLE",
    "uv_contract": "UV_METRIC_1M, world projected, 1 tile per metre",
    "rework": "separated butt-jointed rim, six slimmer canted slats with 12 flush end brackets, flush seated floor/posts, separated armor and surface markers",
    "license": "CC0-1.0; original authored geometry, no external meshes/textures",
    "source_sha256": hashlib.sha256(Path(__file__).read_bytes()).hexdigest(),
    "unity": "NOT_RUN", "windows": "NOT_RUN"
}
(OUT / "build_report.json").write_text(json.dumps(report, indent=2) + "\n")
print("ENV04_001_GENERATED", json.dumps(report, sort_keys=True))
