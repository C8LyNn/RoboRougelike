#!/usr/bin/env python3
"""Run inside actual Blender: reentrant scene, real transforms, BLEND and GLB."""
import importlib.util
import json
import math
import sys
from pathlib import Path
import bpy

argv=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else []
out=Path(argv[0] if argv else "build/M02-013").resolve()
out.mkdir(parents=True,exist_ok=True)
here=Path(__file__).resolve().parent
spec=importlib.util.spec_from_file_location(
    "m02_013_blender_generator",here/"generate_m02_013.py")
gen=importlib.util.module_from_spec(spec)
spec.loader.exec_module(gen)
parts=gen.build()
report=gen.validate(parts)
report["static_files"]=gen.export_obj(parts,out)
# Repeat inside ONE Blender process: generator must replace, not accumulate.
for iteration in range(2):
    result=gen.blender_export(parts,out)
    collection=bpy.data.collections["MODEL_M02_013"]
    objects=list(collection.objects)
    assert len(objects)==len(parts)+2,(iteration,len(objects),len(parts))
    assert len({ob.name for ob in objects})==len(objects)
    rotor=bpy.data.objects["ROTOR_SPIN_Z"]
    mount=bpy.data.objects["MOUNT_FRAME"]
    assert rotor.parent==mount
    moving=[ob for ob in objects if ob.parent==rotor]
    stationary=[ob for ob in objects if ob.parent==mount]
    assert len(moving)==report["rotor_meshes"]
    assert len(stationary)==report["static_meshes"]+1
    assert all(ob.type=="MESH" for ob in moving)
    # Evaluate Blender scene (not just the procedural input constants).
    sample=bpy.data.objects["M02_013_Rotor_Magnet_00"]
    static=bpy.data.objects["M02_013_Stator_Copper_Winding_00"]
    for frame,expected in ((1,0.0),(16,math.pi/2),
                           (31,math.pi),(46,3*math.pi/2),
                           (61,2*math.pi)):
        bpy.context.scene.frame_set(frame)
        assert abs(rotor.rotation_euler.z-expected)<1e-4,(
            frame,rotor.rotation_euler.z,expected)
        bpy.context.view_layer.update()
        p=sample.matrix_world @ sample.data.vertices[0].co
        local=sample.data.vertices[0].co
        target_x=local.x*math.cos(expected)-local.y*math.sin(expected)
        target_y=local.x*math.sin(expected)+local.y*math.cos(expected)
        assert abs(p.x-target_x)<1e-4,(frame,p.x,target_x)
        assert abs(p.y-target_y)<1e-4,(frame,p.y,target_y)
        assert abs(p.z-local.z)<1e-4
        q=static.matrix_world @ static.data.vertices[0].co
        assert (q-static.data.vertices[0].co).length<1e-5
    bpy.context.scene.frame_set(1)
report.update(result)
report["blender_reentrant_scene"]="PASS: 2 builds in one Blender session"
report["blender_transform_samples"]="PASS: frames 1,16,31,46,61"
report["backend"]="Blender-bpy"
(out/"M02-013_qa.json").write_text(
    json.dumps(report,indent=2)+"\n",encoding="utf8")
print("M02-013 BLENDER QA PASS:",json.dumps({
    "version":bpy.app.version_string,
    "parts":len(parts),"rotor_meshes":report["rotor_meshes"],
    "rebuilds":2,"frames_checked":5,
    "blend_bytes":(out/"M02-013.blend").stat().st_size,
    "glb_bytes":(out/"M02-013_animated.glb").stat().st_size
}))
