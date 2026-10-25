#!/usr/bin/env python3
"""Independent stdlib-only analytic and actual GLB structural QA for M02-024."""
import argparse
import json
import math
import struct
from pathlib import Path

from kinematics_m02_024 import (
    ID, FRAMES, ROLLER_COUNT, analytical_qa,
    roller_pose, wheel_angle, segment_distance, ROLLER_RADIUS,
)

HERE = Path(__file__).resolve().parent
OUT = HERE / "generated"

def math_checks():
    qa = analytical_qa()
    assert qa["pairwise_roller_checks"] == FRAMES * 66
    assert qa["minimum_roller_capsule_clearance_m"] > .015
    assert qa["radial_rim_clearance_m"] > .010
    # 90 degree periodicity of the assembly; independent phase samples.
    for frame in (1, 17, 31, 61, 91, 121):
        poses = [roller_pose(i, wheel_angle(frame)) for i in range(ROLLER_COUNT)]
        for i, (a, u) in enumerate(poses):
            assert abs(sum(v * v for v in u) - 1) < 1e-10
            b, v = poses[(i + 1) % ROLLER_COUNT]
            assert segment_distance(a, u, b, v) > 2 * ROLLER_RADIUS + .015
    return qa

def read_glb(path):
    data = path.read_bytes()
    assert len(data) >= 20 and data[:4] == b"glTF"
    version, total = struct.unpack_from("<II", data, 4)
    assert version == 2 and total == len(data)
    offset = 12
    chunks = {}
    while offset < len(data):
        length, kind = struct.unpack_from("<II", data, offset)
        offset += 8
        chunks[kind] = data[offset:offset + length]
        offset += length
    assert offset == len(data)
    assert 0x4E4F534A in chunks and 0x004E4942 in chunks
    return json.loads(chunks[0x4E4F534A].decode("utf-8")), chunks[0x004E4942]

def accessor_floats(gltf, binary, index):
    accessor = gltf["accessors"][index]
    view = gltf["bufferViews"][accessor["bufferView"]]
    assert accessor["componentType"] == 5126
    components = {"SCALAR": 1, "VEC3": 3, "VEC4": 4}[accessor["type"]]
    size = components * 4
    stride = view.get("byteStride", size)
    start = view.get("byteOffset", 0) + accessor.get("byteOffset", 0)
    return [
        struct.unpack_from("<" + "f" * components, binary, start + i * stride)
        for i in range(accessor["count"])
    ]

def glb_checks():
    glb = OUT / (ID + "_animated.glb")
    blend = OUT / (ID + ".blend")
    report = OUT / (ID + "_qa.json")
    assert glb.stat().st_size > 10000
    assert blend.stat().st_size > 10000
    assert report.stat().st_size > 100
    gltf, binary = read_glb(glb)
    assert gltf["asset"]["version"] == "2.0"
    assert len(gltf.get("materials", [])) >= 8
    assert len(gltf.get("meshes", [])) >= 100
    nodes = gltf["nodes"]
    assert any(n.get("name", "").startswith("MOUNT_SIDE_AXLE_ORIGIN") for n in nodes)
    animations = gltf.get("animations", [])
    assert animations
    animated = set()
    moving_samples = 0
    for animation in animations:
        for channel in animation["channels"]:
            if channel["target"]["path"] != "rotation":
                continue
            name = nodes[channel["target"]["node"]].get("name", "")
            animated.add(name)
            sampler = animation["samplers"][channel["sampler"]]
            values = accessor_floats(gltf, binary, sampler["output"])
            if len(values) >= 3 and len({tuple(round(x, 4) for x in q) for q in values}) >= 3:
                moving_samples += 1
    assert "WHEEL_SPIN_Y" in animated, animated
    rollers = {n for n in animated if n.startswith("ROLLER_SPIN_")}
    assert len(rollers) == ROLLER_COUNT, sorted(rollers)
    assert moving_samples >= 13, moving_samples
    return {
        "glb_bytes": glb.stat().st_size,
        "blend_bytes": blend.stat().st_size,
        "meshes": len(gltf["meshes"]),
        "materials": len(gltf["materials"]),
        "animations": len(animations),
        "animated_rotations": len(animated),
        "nontrivial_sampled_rotations": moving_samples,
        "status": "PASS",
    }

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--math-only", action="store_true")
    args = parser.parse_args()
    result = {"id": ID, "math": math_checks()}
    if not args.math_only:
        result["glb"] = glb_checks()
    print(json.dumps(result, indent=2))

if __name__ == "__main__":
    main()
