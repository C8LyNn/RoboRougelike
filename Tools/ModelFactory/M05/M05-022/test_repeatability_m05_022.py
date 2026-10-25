#!/usr/bin/env python3
"""M05-022: Blender scene isolation and deterministic regeneration smoke test."""
import importlib.util
import pathlib
import bpy

source = pathlib.Path(__file__).resolve().parent / "generate_m05_022.py"
spec = importlib.util.spec_from_file_location("m05_022",str(source))
generator = importlib.util.module_from_spec(spec)
spec.loader.exec_module(generator)

bpy.ops.mesh.primitive_cube_add(size=0.1,location=(9,9,9))
foreign = bpy.context.object
foreign.name = "FOREIGN_DO_NOT_TOUCH"
foreign_mat = bpy.data.materials.new("FOREIGN_MATERIAL")
foreign.data.materials.append(foreign_mat)

first = generator.build()
names_a = sorted(o.name for o in generator.created)
mats_a = len([m for m in bpy.data.materials if m.name.startswith(generator.ID+"_")])
second = generator.build()
names_b = sorted(o.name for o in generator.created)
mats_b = len([m for m in bpy.data.materials if m.name.startswith(generator.ID+"_")])
assert first["status"] == second["status"] == "BLENDER_QA_PASS"
assert names_a == names_b, "object names changed or multiplied"
assert first["mesh_count"] == second["mesh_count"]
assert mats_a == mats_b == 9, "material datablocks multiplied"
assert bpy.data.objects.get(foreign.name) is foreign, "foreign object removed"
assert bpy.data.materials.get(foreign_mat.name) is foreign_mat, "foreign material removed"
assert second["frames_checked"] == 61
assert second["min_rotor_to_guard_radial_clearance_m"] > 0.025
print("M05-022 Blender repeatability and foreign-object isolation PASS",second)
