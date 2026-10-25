#!/usr/bin/env python3
"""Independent standard-library checks for M05-021 GLB export and source."""
import argparse
import ast
import json
import pathlib
import struct

HERE = pathlib.Path(__file__).resolve().parent

def validate_glb(path):
    data = path.read_bytes()
    assert len(data) > 20, "GLB too small"
    magic, version, total = struct.unpack_from("<III", data)
    assert magic == 0x46546C67 and version == 2 and total == len(data), "invalid GLB header"
    chunk_length, chunk_type = struct.unpack_from("<II", data, 12)
    assert chunk_type == 0x4E4F534A, "missing GLB JSON chunk"
    gltf = json.loads(data[20:20+chunk_length].decode("utf-8").rstrip(" \x00"))
    materials = gltf.get("materials", [])
    meshes = gltf.get("meshes", [])
    animations = gltf.get("animations", [])
    nodes = [n.get("name", "") for n in gltf.get("nodes", [])]
    assert len(materials) >= 3, "fewer than three exported materials"
    assert len(meshes) >= 20, "unexpectedly few exported meshes"
    assert animations, "missing telescopic animation"
    assert any("MOUNT_ROOT" in n for n in nodes), "missing root mount marker"
    assert any("MOUNT_BEACON_TOP" in n for n in nodes), "missing beacon mount marker"
    assert any("PIVOT_MIDDLE_STAGE" in n for n in nodes), "missing middle pivot"
    assert any("PIVOT_INNER_STAGE" in n for n in nodes), "missing inner pivot"
    return {"glb": "PASS", "materials": len(materials), "meshes": len(meshes),
            "animations": len(animations), "nodes": len(nodes)}

def main():
    p = argparse.ArgumentParser()
    p.add_argument("--out", default=str(HERE / "output"))
    p.add_argument("--require-export", action="store_true")
    args = p.parse_args()
    src = HERE / "generate_m05_021.py"
    ast.parse(src.read_text(encoding="utf-8"), filename=str(src))
    result = {"source_syntax": "PASS", "id": "M05-021"}
    glb = pathlib.Path(args.out) / "M05-021.glb"
    qa = pathlib.Path(args.out) / "M05-021_qa.json"
    if glb.exists() and qa.exists():
        result.update(validate_glb(glb))
        blender_qa = json.loads(qa.read_text(encoding="utf-8"))
        assert blender_qa.get("status") == "BLENDER_QA_PASS"
        assert blender_qa.get("frames_checked") == 95
        assert blender_qa.get("glb_export") == "PASS"
        assert blender_qa.get("blend_export") == "PASS"
        result["blender_qa"] = "PASS"
    elif args.require_export:
        raise SystemExit("Missing Blender export or QA JSON")
    else:
        result["blender_qa"] = "NOT RUN"
        result["glb"] = "NOT RUN"
    print(json.dumps(result, indent=2))

if __name__ == "__main__":
    main()
