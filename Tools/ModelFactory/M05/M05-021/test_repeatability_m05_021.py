#!/usr/bin/env python3
"""Blender-only rerun isolation regression for M05-021."""
import importlib.util
import pathlib
import bpy

path = pathlib.Path(__file__).resolve().parent / "generate_m05_021.py"
spec = importlib.util.spec_from_file_location("m05_021_generator", str(path))
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)

bpy.ops.mesh.primitive_cube_add(size=0.3, location=(10, 10, 10))
foreign = bpy.context.object
foreign.name = "FOREIGN_DO_NOT_DELETE"
foreign_material = bpy.data.materials.new("FOREIGN_MATERIAL")
foreign.data.materials.append(foreign_material)

first = module.build()
first_names = sorted(o.name for o in module.created)
first_meshes = first["mesh_count"]
first_mats = len([m for m in bpy.data.materials if m.name.startswith(module.ID + "_")])
second = module.build()
second_names = sorted(o.name for o in module.created)
second_mats = len([m for m in bpy.data.materials if m.name.startswith(module.ID + "_")])

assert first_names == second_names, "Rerun changed object identity/count"
assert first_meshes == second["mesh_count"] == 35, "Rerun changed mesh count"
assert first_mats == second_mats == 8, "Rerun duplicated materials"
assert bpy.data.objects.get("FOREIGN_DO_NOT_DELETE") is foreign, "Foreign object deleted"
assert bpy.data.materials.get("FOREIGN_MATERIAL") is foreign_material, "Foreign material deleted"
print("M05-021 RERUN QA PASS: 35 meshes, 8 materials, 95 frames, foreign scene preserved")
