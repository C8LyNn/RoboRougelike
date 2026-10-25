#!/usr/bin/env python3
"""Parse the actual exported GLB binary; independent of Blender."""
import json
import struct
import sys
from pathlib import Path

path = Path(sys.argv[1])
data = path.read_bytes()
assert len(data) > 1024
magic, version, total = struct.unpack_from("<III", data, 0)
assert magic == 0x46546C67 and version == 2 and total == len(data)
chunk_len, chunk_type = struct.unpack_from("<II", data, 12)
assert chunk_type == 0x4E4F534A
gltf = json.loads(data[20:20 + chunk_len].decode("utf-8"))
nodes = [n.get("name", "") for n in gltf.get("nodes", [])]
materials = [m.get("name", "") for m in gltf.get("materials", [])]
meshes = gltf.get("meshes", [])
animations = gltf.get("animations", [])
targets = []
for anim in animations:
    for channel in anim.get("channels", []):
        idx = channel.get("target", {}).get("node", -1)
        if 0 <= idx < len(nodes):
            targets.append((nodes[idx], channel.get("target", {}).get("path")))
assert len(meshes) >= 35, len(meshes)
assert len(materials) >= 6, materials
assert sum("SNAP_BOTTOM_" in n for n in nodes) == 4, nodes
for name in ("DOOR_L_HINGE_Y", "DOOR_R_HINGE_Y",
             "LATCH_L_AXIS_Z", "LATCH_R_AXIS_Z"):
    assert any(name in n for n in nodes), name
    assert any(name in n and path == "rotation" for n, path in targets), (name, targets)
assert animations, "no exported animation"
print("M03-019 GLB QA PASS", {
    "mesh_count": len(meshes), "material_count": len(materials),
    "node_count": len(nodes), "animation_count": len(animations),
    "rotation_targets": targets})
