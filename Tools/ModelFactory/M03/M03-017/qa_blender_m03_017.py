#!/usr/bin/env python3
"""M03-017: Blender mesh, animation, repeatability and exported GLB QA."""
import json
import struct
import sys
from pathlib import Path

import bpy

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import generate_m03_017 as model

def inventory():
    return {
        "objects": len([o for o in bpy.data.objects if o.name.startswith(model.PREFIX)]),
        "meshes": len([m for m in bpy.data.meshes if m.name.startswith(model.PREFIX)]),
        "materials": len([m for m in bpy.data.materials if m.name.startswith(model.PREFIX)]),
        "actions": len([a for a in bpy.data.actions if a.name.startswith(model.PREFIX)]),
    }

def inspect_glb(path):
    blob = path.read_bytes()
    assert len(blob) > 1024
    magic, version, length = struct.unpack_from("<4sII", blob, 0)
    assert magic == b"glTF" and version == 2 and length == len(blob)
    chunk_len, chunk_type = struct.unpack_from("<I4s", blob, 12)
    assert chunk_type == b"JSON"
    gltf = json.loads(blob[20:20 + chunk_len].decode("utf-8"))
    nodes = [node.get("name", "") for node in gltf.get("nodes", [])]
    assert any("ROOT_PIVOT" in name for name in nodes), nodes
    assert sum("SNAP_" in name for name in nodes) == 4, nodes
    assert any("ANIM_LOCK_BOLT" in name for name in nodes), nodes
    assert len(gltf.get("materials", [])) >= 6, gltf.get("materials")
    assert len(gltf.get("meshes", [])) >= 20, len(gltf.get("meshes", []))
    animations = gltf.get("animations", [])
    assert animations, "No exported animation"
    channels = sum(len(a.get("channels", [])) for a in animations)
    assert channels >= 3, channels
    return {"glb_bytes": len(blob), "glb_meshes": len(gltf["meshes"]),
            "glb_materials": len(gltf["materials"]),
            "glb_animations": len(animations), "glb_channels": channels}

def main():
    out = Path(sys.argv[sys.argv.index("--") + 1]) if "--" in sys.argv else Path.cwd()
    first = model.create()
    report1 = model.verify(first)
    count1 = inventory()
    second = model.create()
    report2 = model.verify(second)
    count2 = inventory()
    assert count1 == count2, (count1, count2)
    assert report1 == report2
    assert count2["actions"] == 3, count2
    # Continuous sweep samples, not only extreme keyframes.
    for frame in range(1, 73):
        bpy.context.scene.frame_set(frame)
        bolt = bpy.data.objects[model.PREFIX + "ANIM_LOCK_BOLT_PLUS_X"]
        red = bpy.data.objects[model.PREFIX + "ANIM_UNLOCKED_RED_FLAG_Z"]
        green = bpy.data.objects[model.PREFIX + "ANIM_LOCKED_GREEN_FLAG_Z"]
        assert 0 <= bolt.location.x <= 0.100001, (frame, bolt.location.x)
        # Handle center X ranges [-0.315, -0.215]; closest to guide ring -0.18
        # is 0.035 m, more than 0.018 m handle radius + 0.014 m tube.
        handle_x = -0.315 + bolt.location.x
        assert abs(handle_x - (-0.18)) > 0.032
        assert 0.0929 <= red.location.z <= 0.1511
        assert 0.0929 <= green.location.z <= 0.1511
    bpy.context.scene.frame_set(1)
    glb = inspect_glb(out / "M03-017.glb")
    result = {"id": "M03-017", "repeat_build": "PASS",
              "continuous_sweep_frames": 72, "inventory": count2, **glb}
    print("M03-017 BLENDER_QA_PASS " + json.dumps(result, sort_keys=True))
    (out / "M03-017.blender-qa.json").write_text(
        json.dumps(result, indent=2) + "\n", encoding="utf-8")

if __name__ == "__main__":
    main()
