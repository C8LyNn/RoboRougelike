#!/usr/bin/env python3
"""Independent M04-023 binary GLB and shutter animation validator.
No bpy dependency: validates exported mesh/material/node/animation structures.
The generator performs manifold checks and sampled analytical clearances.
"""
import argparse
import json
import math
import struct
from pathlib import Path

EXPECTED = ["SHUTTER_%02d_SLIDE" % i for i in range(1, 7)]

def read_glb(path):
    data = Path(path).read_bytes()
    assert len(data) >= 20, "GLB too short"
    magic, version, length = struct.unpack_from("<III", data, 0)
    assert magic == 0x46546C67 and version == 2 and length == len(data), "Invalid GLB header"
    pos, chunks = 12, {}
    while pos < len(data):
        size, kind = struct.unpack_from("<II", data, pos)
        pos += 8
        assert pos + size <= len(data), "Truncated GLB chunk"
        chunks[kind] = data[pos:pos + size]
        pos += size
    assert 0x4E4F534A in chunks and 0x004E4942 in chunks, "Missing JSON or BIN chunk"
    return json.loads(chunks[0x4E4F534A].decode("utf-8")), chunks[0x004E4942]

def accessor_vec3(gltf, binary, index):
    acc = gltf["accessors"][index]
    assert acc["componentType"] == 5126 and acc["type"] == "VEC3", "Unexpected animation accessor"
    view = gltf["bufferViews"][acc["bufferView"]]
    offset = view.get("byteOffset", 0) + acc.get("byteOffset", 0)
    stride = view.get("byteStride", 12)
    assert stride >= 12
    return [struct.unpack_from("<fff", binary, offset + i * stride)
            for i in range(acc["count"])]

def main():
    p = argparse.ArgumentParser()
    p.add_argument("--glb", required=True)
    p.add_argument("--manifest", required=True)
    p.add_argument("--report", required=True)
    args = p.parse_args()
    gltf, binary = read_glb(args.glb)
    manifest = json.loads(Path(args.manifest).read_text(encoding="utf-8"))
    meshes = gltf.get("meshes", [])
    materials = gltf.get("materials", [])
    nodes = gltf.get("nodes", [])
    animations = gltf.get("animations", [])
    assert len(meshes) >= 50, "Missing modeled details"
    assert len(materials) == 8, "Expected eight PBR materials"
    assert len(animations) >= 1, "No shutter animation exported"
    names = [m["name"] for m in materials]
    assert any("Ion_blue_lens" in name for name in names), "Blue optical lenses absent"
    assert any("Cyan_ion_trace" in name for name in names), "Ion emission absent"
    assert any("Blackened_iridium_shutters" in name for name in names), "Shutter material absent"
    node_names = [node.get("name", "") for node in nodes]
    assert "ROOT_MOUNT_Y0" in node_names, "Root mount not exported"
    for name in EXPECTED:
        assert name in node_names, "Missing shutter pivot: " + name
    mesh_materials = set()
    for mesh in meshes:
        for primitive in mesh.get("primitives", []):
            if "material" in primitive:
                mesh_materials.add(primitive["material"])
            assert "POSITION" in primitive.get("attributes", {}), "Mesh without positions"
    assert len(mesh_materials) >= 7, "Materials not assigned to geometry"

    motion = {}
    for anim in animations:
        for channel in anim.get("channels", []):
            target = channel.get("target", {})
            if target.get("path") != "translation":
                continue
            name = nodes[target["node"]].get("name", "")
            if name not in EXPECTED:
                continue
            sampler = anim["samplers"][channel["sampler"]]
            vectors = accessor_vec3(gltf, binary, sampler["output"])
            assert len(vectors) >= 3, "Shutter lacks keyframes"
            assert all(all(math.isfinite(v) for v in xyz) for xyz in vectors)
            # glTF is Y-up; Blender Z translation is mapped to glTF Y.
            # Measure Euclidean displacement in all three exported axes.
            travel = max(math.dist(v, vectors[0]) for v in vectors)
            motion[name] = travel
    assert len(motion) == 6, "Expected six independently animated shutter translations"
    assert all(.068 <= t <= .072 for t in motion.values()), "Shutter travel outside 70 mm: " + repr(motion)
    assert manifest["moving_shutters"] == 6
    assert manifest["materials"] == 8
    assert manifest["meshes"] == len(meshes), "GLB mesh count differs from Blender"
    assert manifest["frames_checked"] == 121
    assert manifest["min_housing_radial_gap_m"] >= .049
    assert manifest["min_lens_radial_gap_m"] >= .069
    assert manifest["adjacent_sector_angular_gap_deg"] >= 20
    report = {
        "model": "M04-023",
        "status": "PASS",
        "exported_meshes": len(meshes),
        "exported_materials": len(materials),
        "animations": len(animations),
        "animated_shutters": sorted(motion),
        "measured_translation_m": {k: round(v, 6) for k, v in sorted(motion.items())},
        "manifold_edges_checked_in_blender": manifest["manifold_edges_checked"],
        "sampled_frames_in_blender": manifest["frames_checked"],
        "min_housing_gap_m": manifest["min_housing_radial_gap_m"],
        "min_lens_gap_m": manifest["min_lens_radial_gap_m"],
        "limitations": "Not a continuous triangle-triangle collision test; Unity import, physics, EditMode and PlayMode NOT RUN."
    }
    Path(args.report).parent.mkdir(parents=True, exist_ok=True)
    Path(args.report).write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print("M04-023 GLB_QA_PASS " + json.dumps(report, sort_keys=True))

if __name__ == "__main__":
    main()
