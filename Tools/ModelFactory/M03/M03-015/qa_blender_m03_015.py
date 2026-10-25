#!/usr/bin/env python3
"""Run with blender -b --python qa_blender_m03_015.py."""
import importlib.util
import tempfile
from pathlib import Path
import bpy

HERE=Path(__file__).resolve().parent
spec=importlib.util.spec_from_file_location("m03_015_generator",HERE/"generate_m03_015.py")
m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
m.build()
with tempfile.TemporaryDirectory() as td:
    sizes=[]
    for _ in range(2):
        assert m.write_blender(Path(td))==len(m.PARTS)
        coll=bpy.data.collections[m.COLLECTION]
        assert len([o for o in coll.objects if o.type=="MESH"])==len(m.PARTS)
        assert len([o for o in coll.objects if o.name.startswith("SNAP_INNER_")])==4
        door=coll.objects["HINGE_DOOR_PIVOT_Z"]
        assert door.animation_data and door.animation_data.action
        for name in ("LATCH_00_PIVOT_X","LATCH_01_PIVOT_X"):
            assert coll.objects[name].animation_data
        sizes.append((len(bpy.data.objects),len(bpy.data.meshes),len(coll.objects)))
    assert sizes[0]==sizes[1],sizes
print("PASS repeated bpy generation, door/latches animated, no mesh leaks")
