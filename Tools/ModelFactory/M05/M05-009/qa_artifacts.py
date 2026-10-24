#!/usr/bin/env python3
"""Independent M05-009 Blender export checks (Python stdlib only)."""
import json
from pathlib import Path
import struct
import sys

out = Path(sys.argv[1]) if len(sys.argv) > 1 else Path("build/M05-009")
report = json.loads((out / "M05-009_QA.json").read_text(encoding="utf-8"))
assert report["model"] == "M05-009" and report["status"] == "PASS"
assert report["fan_blades"] == 7
assert report["fan_full_rotation_deg"] == 360
assert report["fan_rotation_frames"] == [1, 61]
assert report["manifold"] == "PASS"
assert report["materials"] >= 8
assert report["meshes"] >= 70
assert report["radial_clearance_m"] >= 0.025
assert report["rear_axial_clearance_m"] >= 0.09
assert report["front_axial_clearance_m"] >= 0.055
assert report["repeatability"].startswith("PASS")

glb = (out / "M05-009_side_liquid_radiator.glb").read_bytes()
assert glb[:4] == b"glTF"
version, total = struct.unpack_from("<II", glb, 4)
assert version == 2 and total == len(glb)
json_length, chunk_type = struct.unpack_from("<I4s", glb, 12)
assert chunk_type == b"JSON"
scene = json.loads(glb[20:20 + json_length])
assert len(scene.get("meshes", [])) >= 70, len(scene.get("meshes", []))
assert len(scene.get("materials", [])) >= 8
animations = scene.get("animations", [])
assert animations, "missing actual animated glTF export"
assert sum(len(a.get("channels", [])) for a in animations) >= 1
names = [node.get("name", "") for node in scene.get("nodes", [])]
assert "ANIM_FAN_ROTOR_X" in names, "rotor pivot not exported"
assert "MOUNT_SIDE_SOCKET" in names, "mount socket not exported"
assert sum("Fan_Swept_Blade" in name for name in names) == 7

fbx = (out / "M05-009_side_liquid_radiator.fbx").read_bytes()
assert fbx.startswith(b"Kaydara FBX Binary")
blend = (out / "M05-009_side_liquid_radiator.blend").read_bytes()
assert blend.startswith(b"BLENDER")
assert len(glb) > 100_000 and len(fbx) > 100_000 and len(blend) > 100_000
print(json.dumps({"status": "PASS", "glb_meshes": len(scene["meshes"]),
                  "glb_materials": len(scene["materials"]),
                  "glb_animations": len(animations),
                  "glb_animation_channels": sum(len(a.get("channels", []))
                                                for a in animations),
                  "fbx_bytes": len(fbx), "blend_bytes": len(blend),
                  "unity": "NOT RUN"}, indent=2))
