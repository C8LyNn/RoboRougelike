#!/usr/bin/env python3
"""ENV03-001 — original, static low-poly arena perimeter wall for Blender 4.x.
Run: blender -b --python generate_env03_001.py -- --output-dir /tmp/env03-001
1 Blender unit = 1 m; X=wall length, Y=wall depth (+Y outside arena), Z=up.
"""
import argparse
import bmesh
import hashlib
import json
import math
import os
import sys
from mathutils import Vector
import bpy

ASSET_ID = "ENV03-001"
LENGTH = 2.0
ROOT_NAME = "ENV03-001_ROOT"

def options():
    args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    parser = argparse.ArgumentParser()
    parser.add_argument("--output-dir", required=True)
    return parser.parse_args(args)

def material(name, color, metallic, roughness):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = (*color, 1.0)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (*color, 1.0)
    bsdf.inputs["Metallic"].default_value = metallic
    bsdf.inputs["Roughness"].default_value = roughness
    return mat

def add_to_root(obj, root, mat):
    obj.parent = root
    obj.data.materials.append(mat)
    return obj

def extruded_profile(name, profile_yz, x_min, x_max, root, mat):
    """Closed prism with identical left/right end profiles, no end decorations."""
    n = len(profile_yz)
    verts = [(x_min, y, z) for y, z in profile_yz]
    verts += [(x_max, y, z) for y, z in profile_yz]
    faces = [tuple(range(n - 1, -1, -1)), tuple(range(n, 2 * n))]
    for i in range(n):
        j = (i + 1) % n
        faces.append((i, j, j + n, i + n))
    mesh = bpy.data.meshes.new(name + "_Mesh")
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    bm = bmesh.new()
    bm.from_mesh(mesh)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    return add_to_root(obj, root, mat)

def box(name, center, dims, root, mat):
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=center)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = dims
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return add_to_root(obj, root, mat)

def bolt(name, x, side, z, root, mat):
    # Plate outer face is Y = +/-0.214; bolt starts exactly on that face.
    center_y = side * (0.214 + 0.006)
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=12, radius=0.017, depth=0.012,
        end_fill_type="NGON", location=(x, center_y, z),
        rotation=(math.pi / 2, 0, 0)
    )
    obj = bpy.context.object
    obj.name = name
    return add_to_root(obj, root, mat)

def set_camera(scene, name, position, target, ortho=3.25):
    bpy.ops.object.camera_add(location=position)
    cam = bpy.context.object
    cam.name = name
    direction = Vector(target) - cam.location
    cam.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
    cam.data.type = "ORTHO"
    cam.data.ortho_scale = ortho
    scene.camera = cam
    return cam

def add_area_light(name, pos, energy, size):
    bpy.ops.object.light_add(type="AREA", location=pos)
    light = bpy.context.object
    light.name = name
    light.data.energy = energy
    light.data.shape = "DISK"
    light.data.size = size
    light.rotation_euler = (Vector((0, 0, 0.4)) - light.location).to_track_quat("-Z", "Y").to_euler()
    return light

def preview(scene, path, position):
    cam = set_camera(scene, "PREVIEW_CAMERA", position, (0, 0, 0.43))
    scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
    bpy.data.objects.remove(cam, do_unlink=True)

def sha256(path):
    digest = hashlib.sha256()
    with open(path, "rb") as f:
        for chunk in iter(lambda: f.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()

def main():
    out = os.path.abspath(options().output_dir)
    os.makedirs(out, exist_ok=True)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    scene.unit_settings.system = "METRIC"
    scene.unit_settings.scale_length = 1.0
    root = bpy.data.objects.new(ROOT_NAME, None)
    bpy.context.collection.objects.link(root)
    root.location = (0.0, 0.0, 0.0)
    root.empty_display_type = "CUBE"
    root.empty_display_size = 0.2

    concrete = material("ENV03_Concrete", (0.38, 0.41, 0.43), 0.0, 0.91)
    steel = material("ENV03_DarkSteel", (0.095, 0.13, 0.16), 0.72, 0.46)
    safety = material("ENV03_SafetyOchre", (0.85, 0.43, 0.095), 0.18, 0.59)

    # Body's flat front/back surfaces: Y=+/-0.20, Z=0.19..0.70.
    # End caps at X=+/-1.00 are identical, so adjacent copies meet cleanly.
    body_yz = [
        (-0.24, 0.00), (0.24, 0.00), (0.24, 0.12),
        (0.20, 0.19), (0.20, 0.70), (0.16, 0.78),
        (-0.16, 0.78), (-0.20, 0.70), (-0.20, 0.19),
        (-0.24, 0.12),
    ]
    extruded_profile("Concrete_Core", body_yz, -1.0, 1.0, root, concrete)

    # Longitudinal metal coping touches the concrete at Z=0.78; same
    # cross-section at both ends, with no end bolts or overhanging tabs.
    coping_yz = [
        (-0.16, 0.78), (0.16, 0.78), (0.16, 0.835),
        (0.12, 0.88), (-0.12, 0.88), (-0.16, 0.835),
    ]
    extruded_profile("Continuous_Steel_Coping", coping_yz, -1.0, 1.0, root, steel)

    # All detail stays within X=+/-0.75, leaving clean mating ends.
    for side, face in [(-1, "Arena"), (1, "Outer")]:
        for ix, x in enumerate((-0.64, 0.64)):
            box("TiePlate_%s_%d" % (face, ix), (x, side * 0.207, 0.42),
                (0.14, 0.014, 0.28), root, steel)
            for iz, z in enumerate((0.32, 0.52)):
                bolt("Bolt_%s_%d_%d" % (face, ix, iz), x, side, z, root, steel)
        for ix, x in enumerate((-0.32, 0.32)):
            box("Reflector_%s_%d" % (face, ix), (x, side * 0.2035, 0.45),
                (0.18, 0.007, 0.11), root, safety)

    # Export root and mesh descendants only: no preview cameras/lights.
    bpy.ops.object.select_all(action="DESELECT")
    root.select_set(True)
    for obj in bpy.data.objects:
        if obj.type == "MESH" and obj.parent == root:
            obj.select_set(True)
    bpy.context.view_layer.objects.active = root

    glb = os.path.join(out, "ENV03-001_ArenaWall.glb")
    blend = os.path.join(out, "ENV03-001_ArenaWall.blend")
    bpy.ops.export_scene.gltf(filepath=glb, export_format="GLB",
                              use_selection=True, export_yup=True)

    # Preview setup is saved in BLEND but never exported to GLB.
    scene.render.engine = "CYCLES"
    scene.cycles.device = "CPU"
    scene.cycles.samples = 12
    scene.render.resolution_x = 960
    scene.render.resolution_y = 540
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.world = bpy.data.worlds.new("ENV03_Preview_World")
    scene.world.use_nodes = True
    background = scene.world.node_tree.nodes.get("Background")
    background.inputs["Color"].default_value = (0.35, 0.38, 0.41, 1.0)
    background.inputs["Strength"].default_value = 0.75
    scene.view_settings.view_transform = "Standard"
    add_area_light("Preview_Key", (1.3, -2.5, 3.5), 550, 3.0)
    add_area_light("Preview_Fill", (-1.9, 2.2, 2.6), 350, 2.8)
    bpy.ops.wm.save_as_mainfile(filepath=blend)

    previews = {}
    for label, position in (
        ("arena_front", (3.0, -4.0, 2.2)),
        ("rear_end", (-3.1, 3.6, 2.0)),
    ):
        path = os.path.join(out, "ENV03-001_preview_%s.png" % label)
        try:
            preview(scene, path, position)
            previews[label] = "PASS" if os.path.getsize(path) > 1024 else "FAIL"
        except Exception as exc:
            previews[label] = "NOT_RUN: " + str(exc)
            print("ENV03_PREVIEW_WARNING %s %s" % (label, exc))

    files = {}
    for name in sorted(os.listdir(out)):
        p = os.path.join(out, name)
        if os.path.isfile(p):
            files[name] = {"bytes": os.path.getsize(p), "sha256": sha256(p)}
    report = {
        "id": ASSET_ID, "source": "Original procedural geometry; no third-party assets",
        "scale": "1 Blender unit = 1 m", "pivot": [0, 0, 0],
        "axes": {"X": "along wall", "Y": "positive away from arena", "Z": "up"},
        "preview": previews, "files": files,
    }
    with open(os.path.join(out, "export_manifest.json"), "w", encoding="utf-8") as f:
        json.dump(report, f, indent=2, ensure_ascii=False)
    print("ENV03_EXPORT_COMPLETE " + json.dumps(report, ensure_ascii=False))

if __name__ == "__main__":
    main()
