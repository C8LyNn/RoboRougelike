#!/usr/bin/env python3
"""Real Blender bpy QA for M05-008, including rerun, animation and binary export."""
from __future__ import annotations
import argparse
import importlib.util
import json
import struct
import sys
from pathlib import Path

import bpy

HERE = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location("m05_008_generator",HERE/"generate_m05_008.py")
generator = importlib.util.module_from_spec(spec)
spec.loader.exec_module(generator)


def read_glb_json(path):
    data=path.read_bytes()
    assert data[:4]==b"glTF"
    version,length=struct.unpack_from("<II",data,4)
    assert version==2 and length==len(data)
    chunk_len,chunk_type=struct.unpack_from("<II",data,12)
    assert chunk_type==0x4E4F534A
    return json.loads(data[20:20+chunk_len].decode("utf-8"))


def run(folder):
    parts=generator.build_parts()
    assert generator.validate(parts)["result"]=="PASS"
    # This sentinel must survive every repeated generator invocation.
    sentinel=bpy.data.objects.new("QA_KEEP_FOREIGN_OBJECT",None)
    bpy.context.scene.collection.objects.link(sentinel)
    result1=generator.export_blender(parts,folder)
    col=bpy.data.collections[generator.COLLECTION]
    expected=len(parts)+2+len(generator.SOCKETS)
    assert len(col.objects)==expected,(len(col.objects),expected)
    assert "QA_KEEP_FOREIGN_OBJECT" in bpy.data.objects
    hinge=bpy.data.objects[generator.PREFIX+"HATCH_HINGE_X"]
    assert hinge.animation_data and hinge.animation_data.action
    action=hinge.animation_data.action
    assert len(action.fcurves)>=1
    keys=[k.co[0] for fc in action.fcurves for k in fc.keyframe_points]
    assert {1,32,64}.issubset(set(keys))
    for frame,target in ((1,0.0),(32,generator.OPEN_DEGREES),(64,0.0)):
        bpy.context.scene.frame_set(frame)
        assert abs(hinge.rotation_euler[0] -
                   __import__("math").radians(target)) < 1e-5
    bpy.context.scene.frame_set(1)
    count1=sum(1 for mesh in bpy.data.meshes
               if mesh.name.startswith(generator.PREFIX))
    assert count1==len(parts),(count1,len(parts))
    result2=generator.export_blender(parts,folder)
    col=bpy.data.collections[generator.COLLECTION]
    count2=sum(1 for mesh in bpy.data.meshes
               if mesh.name.startswith(generator.PREFIX))
    assert len(col.objects)==expected
    assert count2==count1==len(parts),(count1,count2,len(parts))
    assert "QA_KEEP_FOREIGN_OBJECT" in bpy.data.objects
    fbx=folder/result2["fbx"]
    glb=folder/result2["glb"]
    blend=folder/result2["blend"]
    assert fbx.read_bytes().startswith(b"Kaydara FBX Binary")
    assert blend.read_bytes().startswith(b"BLENDER")
    gltf=read_glb_json(glb)
    assert gltf.get("animations"),"GLB must contain actual hatch keyframes"
    assert len(gltf.get("materials",[]))>=3
    report=dict(asset="M05-008",result="PASS",
                blender_version=bpy.app.version_string,
                parts=len(parts),collection_objects=expected,
                mesh_datablocks_before=count1,mesh_datablocks_after=count2,
                foreign_object_preserved=True,
                animated_hinge="X / -70 degrees / 3 keys",
                glb_animations=len(gltf["animations"]),
                glb_materials=len(gltf["materials"]),
                exports=[result2["fbx"],result2["glb"],result2["blend"]],
                unity="NOT RUN")
    (folder/"M05-008_blender_QA.json").write_text(
        json.dumps(report,indent=2)+"\n",encoding="utf-8")
    print(json.dumps(report,indent=2))


if __name__=="__main__":
    args=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else sys.argv[1:]
    parser=argparse.ArgumentParser()
    parser.add_argument("--out",default="build/M05-008/blender")
    opts=parser.parse_args(args)
    run(Path(opts.out).resolve())
