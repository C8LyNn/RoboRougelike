#!/usr/bin/env python3
"""Real Blender QA: rerun safety, 61-frame swept geometry, binary glTF data."""
import importlib.util
import json
import math
from pathlib import Path
import struct
import sys
import tempfile

import bpy

HERE=Path(__file__).resolve().parent
spec=importlib.util.spec_from_file_location("m05_012_generator",HERE/"generate_m05_012.py")
g=importlib.util.module_from_spec(spec)
spec.loader.exec_module(g)


def glb_json(path):
    data=path.read_bytes()
    assert data[:4]==b"glTF"
    version,length=struct.unpack_from("<II",data,4)
    assert version==2 and length==len(data)
    chunk_length,chunk_type=struct.unpack_from("<II",data,12)
    assert chunk_type==0x4E4F534A
    return json.loads(data[20:20+chunk_length].decode("utf-8"))


def snapshot():
    return {
        "objects":len([o for o in bpy.data.objects if o.name.startswith(g.PREFIX)]),
        "meshes":len([o for o in bpy.data.objects if o.name.startswith(g.PREFIX) and o.type=="MESH"]),
        "materials":len([m for m in bpy.data.materials if m.name.startswith(g.PREFIX)]),
        "mesh_datablocks":len([m for m in bpy.data.meshes if m.name.startswith(g.PREFIX)]),
    }


def main():
    # A foreign sentinel must survive both rebuilds, without modifications.
    sentinel=bpy.data.objects.new("FOREIGN_QA_SENTINEL_M05_012",None)
    bpy.context.scene.collection.objects.link(sentinel)
    sentinel.location=(3.14,-2.72,1.62)
    first=g.build()
    one=snapshot()
    second=g.build()
    two=snapshot()
    assert one==two,("resource leak",one,two)
    assert bpy.data.objects.get(sentinel.name) is sentinel
    assert tuple(round(float(v),2) for v in sentinel.location)==(3.14,-2.72,1.62)
    assert first["mesh_objects"]==second["mesh_objects"]
    assert first["vertices"]==second["vertices"]
    assert first["triangles"]==second["triangles"]
    assert second["materials"]==9
    assert second["moving_meshes"]==22
    assert second["mounting_markers"]==5
    assert second["moving_vertex_samples"]>60000
    assert second["stator_radial_clearance_m"]>.015
    assert second["manifold_meshes"]==second["mesh_objects"]
    assert g.ROTOR.animation_data and g.ROTOR.animation_data.action
    assert len(g.ROTOR.animation_data.action.fcurves)==4
    with tempfile.TemporaryDirectory(prefix="m05-012-qa-") as td:
        out=Path(td)
        report=g.export(out,second)
        glb=glb_json(out/"M05-012_emergency_generator.glb")
        names=[node.get("name","") for node in glb.get("nodes",[])]
        assert any("ALTERNATOR_ROTOR_X_PIVOT" in n for n in names)
        assert all(any(n.endswith("MOUNT_FRAME_"+tag) for n in names)
                   for tag in ("PP","PN","NP","NN","ORIGIN"))
        assert len(glb.get("materials",[]))==9,len(glb.get("materials",[]))
        assert len(glb.get("animations",[]))>=1
        assert len(glb.get("meshes",[]))==report["mesh_objects"]
        channels=[ch for anim in glb["animations"] for ch in anim["channels"]]
        assert any(ch.get("target",{}).get("path")=="rotation" for ch in channels)
        assert report["export_glb"]==report["export_fbx"]==report["save_blend"]=="PASS"
        assert report["unity"]=="NOT RUN"
    assert bpy.data.objects.get(sentinel.name) is sentinel
    print("M05-012 QA PASS",json.dumps({
        "id":second["id"],"two_builds":"PASS","foreign_object":"PRESERVED",
        "resource_stability":one,"meshes":second["mesh_objects"],
        "vertices":second["vertices"],"triangles":second["triangles"],
        "moving":second["moving_meshes"],
        "sweep_samples":second["moving_vertex_samples"],
        "stator_clearance_m":second["stator_radial_clearance_m"],
        "glb_materials":len(glb["materials"]),
        "glb_animations":len(glb["animations"]),
        "glb_meshes":len(glb["meshes"]),
        "export":"PASS","unity":"NOT RUN"},sort_keys=True))


if __name__=="__main__":
    main()
