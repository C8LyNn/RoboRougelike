#!/usr/bin/env python3
"""Actual Blender smoke test for M05-007. Execute: blender -b --python blender_smoke.py -- /tmp/out."""
import importlib.util
import json
import struct
import sys
from pathlib import Path
import bpy

HERE=Path(__file__).resolve().parent
spec=importlib.util.spec_from_file_location("m05_007_generator",HERE/"generate_m05_007.py")
mod=importlib.util.module_from_spec(spec)
spec.loader.exec_module(mod)
argv=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else []
out=Path(argv[0] if argv else "/tmp/m05-007-blender").resolve()
out.mkdir(parents=True,exist_ok=True)
sentinel=bpy.data.objects.new("M05-007_TEST_SENTINEL_DO_NOT_DELETE",None)
bpy.context.scene.collection.objects.link(sentinel)
stats=[]
for iteration in range(2):
    qa=mod.validate()
    mod.static_export(out)
    exported=mod.blender_export(out)
    assert sentinel.name in bpy.data.objects, "Unrelated object deleted"
    collection=bpy.data.collections["M05-007_ASSET"]
    owned=list(collection.objects)
    assert len(owned)==len(mod.PARTS)+3,(len(owned),len(mod.PARTS))
    for frame,left,right in ((1,0,0),(16,.21,0),(31,.21,.21),
                             (46,0,.21),(61,0,0)):
        bpy.context.scene.frame_set(frame)
        for label,target in (("L",left),("R",right)):
            ob=bpy.data.objects["M05-007_DRAWER_"+label]
            assert abs(ob.location.y-target)<1e-5,(frame,label,ob.location.y,target)
    stats.append((
        len(owned),
        len([m for m in bpy.data.meshes if m.name.startswith("M05-007_")]),
        len([m for m in bpy.data.materials if m.name.startswith("M05-007_")]),
    ))
assert stats[0]==stats[1],("Blender datablocks leaked",stats)
glb=out/"M05-007_animated.glb"
blend=out/"M05-007.blend"
assert glb.stat().st_size>10000
assert blend.stat().st_size>10000
raw=glb.read_bytes()
magic,version,length=struct.unpack_from("<4sII",raw,0)
assert magic==b"glTF" and version==2 and length==len(raw)
chunk_length,chunk_type=struct.unpack_from("<I4s",raw,12)
assert chunk_type==b"JSON"
gltf=json.loads(raw[20:20+chunk_length].decode("utf-8"))
names={n.get("name") for n in gltf["nodes"]}
assert "M05-007_DRAWER_L" in names
assert "M05-007_DRAWER_R" in names
assert gltf.get("animations"),"Animated GLB lacks animation"
animated={gltf["nodes"][channel["target"]["node"]].get("name")
          for anim in gltf["animations"] for channel in anim["channels"]}
assert {"M05-007_DRAWER_L","M05-007_DRAWER_R"}.issubset(animated),animated
assert len(gltf["materials"])>=3
report={"status":"BLENDER_PASS","iterations":2,"no_leaked_datablocks":True,
        "unrelated_object_preserved":True,"animated_nodes":sorted(animated),
        "motion_frames_verified":[1,16,31,46,61],
        "glb_bytes":glb.stat().st_size,"blend_bytes":blend.stat().st_size,
        "glb_materials":len(gltf["materials"]),
        "unity":"NOT_RUN"}
(out/"M05-007.blender-qa.json").write_text(json.dumps(report,indent=2)+"\n")
print("M05-007 BLENDER QA PASS",json.dumps(report,indent=2))
