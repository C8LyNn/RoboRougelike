#!/usr/bin/env python3
"""Independent GLB and QA-report checks for M05-023 (stdlib only)."""
import json
import struct
import sys
from pathlib import Path

def check(folder):
    folder = Path(folder)
    glb = folder / "M05-023_spare_parts_container.glb"
    blend = folder / "M05-023_spare_parts_container.blend"
    qa_path = folder / "qa_report.json"
    assert glb.is_file() and blend.is_file() and qa_path.is_file(), "Missing exports"
    data = glb.read_bytes()
    assert len(data) > 5000, "GLB too small"
    magic, version, total = struct.unpack_from("<4sII", data, 0)
    assert magic == b"glTF" and version == 2 and total == len(data), "Invalid GLB header"
    json_length, json_type = struct.unpack_from("<I4s", data, 12)
    assert json_type == b"JSON", "Missing GLB JSON chunk"
    doc = json.loads(data[20:20 + json_length].decode("utf-8"))
    meshes = doc.get("meshes", [])
    materials = doc.get("materials", [])
    animations = doc.get("animations", [])
    nodes = doc.get("nodes", [])
    names = {n.get("name", "") for n in nodes}
    assert len(meshes) >= 55, "GLB lost geometry: %d" % len(meshes)
    assert len(materials) >= 8, "GLB lost materials: %d" % len(materials)
    assert len(animations) >= 1, "GLB lost door animation"
    for name in ("M05_023_MOUNT_ROOT", "M05_023_HINGE_PIVOT_L",
                 "M05_023_HINGE_PIVOT_R", "M05_023_MOUNT_L_LOW",
                 "M05_023_MOUNT_R_HIGH", "M05_023_CARGO_ACCESS_FORWARD"):
        assert name in names, "Missing GLB node: " + name
    assert all(m.get("primitives") for m in meshes), "Empty mesh primitives"
    qa = json.loads(qa_path.read_text(encoding="utf-8"))
    assert qa["status"] == "BLENDER_QA_PASS"
    assert qa["repeatability"] == "PASS"
    assert qa["material_count"] == 8
    assert qa["min_front_frame_clearance_m"] >= 0.02
    assert qa["min_leaf_separation_m"] >= 0.04
    assert blend.stat().st_size > 5000
    print(json.dumps({
        "result": "PASS", "id": "M05-023", "glb_meshes": len(meshes),
        "glb_materials": len(materials), "glb_animations": len(animations),
        "glb_nodes": len(nodes), "glb_bytes": len(data),
        "blend_bytes": blend.stat().st_size
    }, indent=2))

if __name__ == "__main__":
    check(sys.argv[1] if len(sys.argv) > 1 else "out")
