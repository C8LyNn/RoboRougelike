#!/usr/bin/env python3
"""Run inside Blender to prove bpy regeneration is idempotent."""
import importlib.util
import tempfile
from pathlib import Path
import bpy

HERE=Path(__file__).resolve().parent
spec=importlib.util.spec_from_file_location('m03_014_generator',HERE/'generate_m03_014.py')
mod=importlib.util.module_from_spec(spec)
spec.loader.exec_module(mod)
mod.build()
with tempfile.TemporaryDirectory() as tmp:
    counts=[]
    for run in range(2):
        assert mod.write_blender(Path(tmp))==18
        coll=bpy.data.collections['MODEL_M03_014']
        meshes=[o for o in coll.objects if o.type=='MESH']
        snaps=[o for o in coll.objects if o.name.startswith('SNAP_BOTTOM_')]
        assert len(meshes)==18 and len(snaps)==4
        assert 'MOUNT_BOTTOM_FRAME' in coll.objects
        counts.append((len(bpy.data.objects),len(bpy.data.meshes),len(coll.objects)))
    assert counts[0]==counts[1],f'orphan/duplicate data: {counts}'
print('PASS bpy repeated twice, 18 meshes, 4 SNAP empties, no orphan mesh growth')
