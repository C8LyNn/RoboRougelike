#!/usr/bin/env python3
"""Read the actual binary glTF 2.0 file; no bpy dependency."""
import json
import struct
import sys
from pathlib import Path

path = Path(sys.argv[1])
raw = path.read_bytes()
assert len(raw) > 1000
magic, version, total = struct.unpack_from("<4sII", raw, 0)
assert magic == b"glTF" and version == 2 and total == len(raw)
chunk_len, chunk_type = struct.unpack_from("<I4s", raw, 12)
assert chunk_type == b"JSON"
doc = json.loads(raw[20:20 + chunk_len].decode("utf-8"))
meshes = doc.get("meshes", [])
mats = doc.get("materials", [])
nodes = doc.get("nodes", [])
names = [n.get("name", "") for n in nodes]
assert len(meshes) == 78, ("meshes", len(meshes))
assert len(mats) == 6, ("materials", len(mats))
assert len([n for n in names if "SNAP_REAR_" in n]) == 4
assert any("ROOT_REAR_MOUNT" in n for n in names)
assert len(doc.get("animations", [])) == 0
assert all("pbrMetallicRoughness" in m for m in mats)
assert len({m["name"] for m in mats}) == 6
assert len([n for n in names if "FloorBar_" in n]) == 9
assert len([n for n in names if "WallVertical_" in n]) == 18
print("M03-020 GLB QA PASS", {
    "meshes": len(meshes), "materials": len(mats),
    "nodes": len(nodes), "mounts": 4, "animations": 0,
    "bytes": len(raw),
})
