#!/usr/bin/env python3
"""Actual bpy QA: two builds, foreign-scene preservation, swept vertices,
and GLB animation/material/node validation. Blender required.
"""
import json
import math
from pathlib import Path
import struct
import sys
import tempfile

import bpy

HERE=Path(__file__).resolve().parent
sys.path.insert(0,str(HERE))
import generate_m05_011 as model


def inspect_glb(path):
    data=path.read_bytes()
    assert data[:4]==b"glTF"
    version,length=struct.unpack_from("<II",data,4)
    assert version==2 and length==len(data)
    chunk_length,chunk_type=struct.unpack_from("<I4s",data,12)
    assert chunk_type==b"JSON"
    doc=json.loads(data[20:20+chunk_length].decode("utf-8"))
    mats=doc.get("materials",[])
    animations=doc.get("animations",[])
    nodes=doc.get("nodes",[])
    assert len(mats)>=9,(len(mats),"materials missing")
    assert len(animations)>=1,"rotor animation missing"
    assert any(channel.get("target",{}).get("path")=="rotation"
               for a in animations for channel in a.get("channels",[]))
    names={node.get("name","") for node in nodes}
    assert any("ROTOR_Z_PIVOT" in n for n in names),sorted(names)
    assert any("MOUNT_FRAME_ORIGIN" in n for n in names),sorted(names)
    assert len(doc.get("meshes",[]))>=50
    return {"glb_version":version,"materials":len(mats),
            "animations":len(animations),"nodes":len(nodes),
            "meshes":len(doc.get("meshes",[]))}


def vertex_position(obj):
    return obj.matrix_world@obj.data.vertices[0].co


def main():
    # This object is deliberately outside the model's tagged collection.
    foreign=bpy.data.objects.new("M05_011_QA_FOREIGN_PRESERVE",None)
    bpy.context.scene.collection.objects.link(foreign)
    a=model.build()
    first=(len(bpy.data.objects),len(bpy.data.meshes),
           len(bpy.data.materials),a["mesh_objects"],a["vertices"])
    assert bpy.data.objects.get(foreign.name) is foreign
    moving=model.MOVING[2]  # first vane, not an axis-symmetric torus
    bpy.context.scene.frame_set(1)
    bpy.context.view_layer.update()
    p1=vertex_position(moving).copy()
    bpy.context.scene.frame_set(16)
    bpy.context.view_layer.update()
    p2=vertex_position(moving).copy()
    assert (p1-p2).length>.1,("no actual rotor motion",p1,p2)
    bpy.context.scene.frame_set(1)
    b=model.build()
    second=(len(bpy.data.objects),len(bpy.data.meshes),
            len(bpy.data.materials),b["mesh_objects"],b["vertices"])
    assert first==second,("rerun data leak",first,second)
    assert bpy.data.objects.get(foreign.name) is foreign
    assert a["moving_meshes"]==b["moving_meshes"]==30
    assert a["materials"]==b["materials"]==9
    assert a["moving_vertex_samples"]>100000
    assert b["core_clearance_m"]>.06
    assert b["cage_clearance_m"]>.01
    assert b["rotation_clearance"].startswith("PASS")
    with tempfile.TemporaryDirectory(prefix="m05-011-qa-") as tmp:
        path=Path(tmp)
        report=model.export(path,b)
        glb=inspect_glb(path/"M05-011_caged_mini_reactor.glb")
        assert report["export_glb"]=="PASS"
        assert report["export_fbx"]=="PASS"
        assert report["save_blend"]=="PASS"
        assert all((path/("M05-011_caged_mini_reactor."+ext)).stat().st_size>1024
                   for ext in ("glb","fbx","blend"))
    print("M05-011 QA PASS",json.dumps({
        "two_consecutive_builds":"PASS","foreign_object":"PRESERVED",
        "scene_object_mesh_material_counts":second,
        "real_rotor_motion_m":round((p1-p2).length,4),
        "swept_clearance":"PASS","glb":glb,
        "blender":"PASS","unity":"NOT RUN"},sort_keys=True))


if __name__=="__main__":
    main()
