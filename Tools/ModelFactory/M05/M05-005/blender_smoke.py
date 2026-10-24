"""Blender-only smoke test for M05-005, never run under plain Python."""
import importlib.util
import json
import struct
from pathlib import Path
import sys
import bpy

here=Path(__file__).resolve().parent
output=Path(sys.argv[sys.argv.index("--")+1]).resolve() if "--" in sys.argv else here/"smoke-out"
spec=importlib.util.spec_from_file_location("m05_005_generator",here/"generate_m05_005.py")
module=importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)
data=module.spec_load()
assert module.validate(data)["manifold_edges"]=="PASS"
bpy.ops.mesh.primitive_cube_add(size=0.1,location=(4,4,4))
sentinel=bpy.context.object
sentinel.name="MODEL05_SENTINEL_KEEP"
for run in (1,2):
    module.blender_scene(data,output)
    owned=bpy.data.collections.get("M05-005_ASSET")
    assert owned is not None
    assert len(owned.objects)==len(data["parts"])+2
    assert sentinel.name in bpy.data.objects, "Generator deleted an unrelated scene object"
    assert len([c for c in bpy.data.collections if c.name.startswith("M05-005_ASSET")])==1
    assert len([o for o in bpy.data.objects if o.name.startswith("M05-005_")])==len(data["parts"])+2
    core=bpy.data.objects["M05-005_CORE_PULSE"]
    bpy.context.scene.frame_set(1)
    low=core.scale[0]
    bpy.context.scene.frame_set(16)
    high=core.scale[0]
    assert abs(low-0.90)<1e-5 and abs(high-1.04)<1e-5,(low,high)
    assert abs(core.location.z-0.36)<1e-5
    bpy.context.scene.frame_set(1)

blend=output/"M05-005.blend"
glb=output/"M05-005_animated.glb"
assert blend.exists() and blend.stat().st_size>1000
assert glb.exists() and glb.stat().st_size>1000
with bpy.data.libraries.load(str(blend),link=False) as (source, target):
    assert "M05-005_ASSET" in source.collections
raw=glb.read_bytes()
assert raw[:4]==b"glTF" and struct.unpack_from("<I",raw,4)[0]==2
chunk_size,chunk_type=struct.unpack_from("<I4s",raw,12)
assert chunk_type==b"JSON"
gltf=json.loads(raw[20:20+chunk_size].decode("utf-8"))
assert gltf.get("animations"),"Animated GLB has no animation"
nodes=[n.get("name","") for n in gltf.get("nodes",[])]
assert any("CORE_PULSE" in n for n in nodes),nodes
assert all("SENTINEL" not in n for n in nodes)
assert len(gltf.get("materials",[]))>=7
print("BLENDER SMOKE PASS: rerun idempotent, sentinel safe, 0.90->1.04 pulse, library and animated GLB")
