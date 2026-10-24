#!/usr/bin/env python3
"""Blender-only idempotence QA: rebuild same collection twice in one session."""
import importlib.util
import sys
import tempfile
from pathlib import Path

import bpy

here=Path(__file__).resolve().parent
spec=importlib.util.spec_from_file_location("m03_013",here/"generate_m03_013.py")
mod=importlib.util.module_from_spec(spec)
spec.loader.exec_module(mod)
parts=mod.build()
mod.validate(parts)

def state():
    collection=bpy.data.collections.get("MODEL_M03_013")
    assert collection is not None
    objects=list(collection.objects)
    assert len(objects)==43,len(objects)  # 38 meshes + root + 4 SNAP
    names={ob.name for ob in objects}
    assert len(names)==43
    meshes=[ob for ob in objects if ob.type=="MESH"]
    assert len(meshes)==38
    assert len([ob for ob in objects if ob.type=="EMPTY"])==5
    assert sum(m.name.startswith("M03_013_") for m in bpy.data.meshes)==38
    assert sum(mat.name.startswith("M03_013_") for mat in bpy.data.materials)==5
    return names

with tempfile.TemporaryDirectory() as tmp:
    out=Path(tmp)
    first=mod.blender_export(parts,out)
    names1=state()
    second=mod.blender_export(parts,out)
    names2=state()
    assert names1==names2,"non-idempotent scene objects"
    for key in ("glb","fbx","blend"):
        assert Path(first[key]).is_file() and Path(first[key]).stat().st_size>100
        assert Path(second[key]).is_file() and Path(second[key]).stat().st_size>100
    assert bpy.data.objects.get("MOUNT_BOTTOM_FRAME") is not None
    print("PASS Blender same-session rebuild: 38 meshes, 4 SNAP, 1 pivot, 5 materials, no duplicates")
