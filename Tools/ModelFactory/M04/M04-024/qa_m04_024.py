#!/usr/bin/env python3
"""Independent, standard-library GLB structural/animation QA for M04-024."""
import argparse
import json
import struct
from pathlib import Path

def read_glb(path):
    data = Path(path).read_bytes()
    assert len(data) >= 20, "Truncated GLB"
    magic, version, total = struct.unpack_from("<III", data, 0)
    assert magic == 0x46546C67 and version == 2 and total == len(data), "Invalid GLB header"
    offset = 12
    chunks = []
    while offset < total:
        size, kind = struct.unpack_from("<II", data, offset)
        offset += 8
        chunks.append((kind, data[offset:offset+size]))
        offset += size
    assert offset == total and chunks[0][0] == 0x4E4F534A, "Missing GLB JSON chunk"
    return json.loads(chunks[0][1].decode("utf-8").strip())

def main():
    p = argparse.ArgumentParser()
    p.add_argument("--glb", required=True)
    p.add_argument("--manifest", required=True)
    p.add_argument("--report", required=True)
    args = p.parse_args()
    doc = read_glb(args.glb)
    manifest = json.loads(Path(args.manifest).read_text(encoding="utf-8"))
    meshes = doc.get("meshes", [])
    materials = doc.get("materials", [])
    nodes = doc.get("nodes", [])
    animations = doc.get("animations", [])
    assert manifest["model"] == "M04-024"
    assert manifest["nozzle_pivots"] == 4
    assert manifest["min_inter_nozzle_xy_aabb_gap_m"] > .25
    assert manifest["min_mount_plate_vertical_aabb_gap_m"] > .20
    assert len(meshes) >= 75, "GLB missing meshes"
    assert len(materials) >= 8, "GLB missing materials"
    pivots = {i for i,node in enumerate(nodes) if "GIMBAL_PIVOT" in node.get("name","")}
    assert len(pivots) == 4, "Four named gimbal nodes required"
    animated = set()
    rotation_channels = 0
    for animation in animations:
        for channel in animation.get("channels", []):
            target = channel.get("target", {})
            if target.get("path") == "rotation" and target.get("node") in pivots:
                animated.add(target["node"])
                rotation_channels += 1
    assert animated == pivots, "Each of the four pivots must have an exported rotation channel"
    emissive = [m for m in materials if "emissiveFactor" in m]
    assert len(emissive) >= 2, "Cyan and amber emissive materials required"
    report = {
        "model": "M04-024",
        "result": "PASS",
        "glb_meshes": len(meshes),
        "glb_materials": len(materials),
        "glb_animations": len(animations),
        "animated_gimbal_nodes": len(animated),
        "rotation_channels": rotation_channels,
        "emissive_materials": len(emissive),
        "blender_manifest": manifest,
        "not_run": ["Unity import", "Unity EditMode", "Unity PlayMode",
                    "URP visual review", "continuous triangle-triangle collision"]
    }
    Path(args.report).write_text(json.dumps(report,indent=2)+"\n",encoding="utf-8")
    print("M04_024_GLB_QA_PASS "+json.dumps(report,sort_keys=True))

if __name__=="__main__":
    main()
