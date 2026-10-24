#!/usr/bin/env python3
"""Dependency-free QA for procedural model-factory assets and generated GLB."""
import json
import math
import struct
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
M02 = ROOT / "Tools/ModelFactory/M02/M02-001"
ASSET = M02 / "generated"

def verify_m02():
    generator = M02 / "generate_m02_001.py"
    subprocess.run([sys.executable, "-m", "py_compile", str(generator)], check=True)
    subprocess.run([sys.executable, str(generator), "--self-test"], check=True)
    obj = (ASSET / "M02-001_segmented_offroad_wheel.obj").read_text("utf-8")
    mtl = (ASSET / "M02-001_segmented_offroad_wheel.mtl").read_text("utf-8")
    lines = obj.splitlines()
    vertices = [list(map(float, l.split()[1:])) for l in lines if l.startswith("v ")]
    faces = [list(map(int, l.split()[1:])) for l in lines if l.startswith("f ")]
    objects = [l for l in lines if l.startswith("o ")]
    materials = {l.split()[1] for l in mtl.splitlines() if l.startswith("newmtl ")}
    used = {l.split()[1] for l in lines if l.startswith("usemtl ")}
    assert len(vertices) == 3360, len(vertices)
    assert len(faces) == 6336, len(faces)
    assert len(objects) == 100, len(objects)
    assert len(materials) == 7 and used == materials
    for face in faces:
        assert len(face) == 3 and all(1 <= i <= len(vertices) for i in face)
    assert all(all(math.isfinite(v) for v in row) for row in vertices)
    assert .533 <= max(math.hypot(y, z) for x, y, z in vertices) <= .535
    assert max(abs(x) for x, y, z in vertices) < .205

    glb = (ASSET / "M02-001_segmented_offroad_wheel.glb").read_bytes()
    magic, version, total = struct.unpack_from("<III", glb, 0)
    assert (magic, version, total) == (0x46546c67, 2, len(glb))
    json_length, json_type = struct.unpack_from("<II", glb, 12)
    assert json_type == 0x4e4f534a
    gltf = json.loads(glb[20:20+json_length].decode("utf-8"))
    bin_start = 20 + json_length
    bin_length, bin_type = struct.unpack_from("<II", glb, bin_start)
    assert bin_type == 0x004e4942
    assert bin_start + 8 + bin_length == len(glb)
    assert gltf["buffers"][0]["byteLength"] <= bin_length
    assert len(gltf["materials"]) == 7
    assert len(gltf["meshes"][0]["primitives"]) == 7
    assert gltf["nodes"][0]["name"] == "MOUNT_AXLE"
    assert gltf["nodes"][1]["name"] == "WHEEL_ROTATOR"
    assert gltf["animations"][0]["channels"][0]["target"] == {"node": 1, "path": "rotation"}
    indices = gltf["accessors"]
    assert sum(indices[p["indices"]]["count"] for p in gltf["meshes"][0]["primitives"]) == 6336 * 3
    sampler = gltf["animations"][0]["samplers"][0]
    def read_f32(accessor_id):
        acc = gltf["accessors"][accessor_id]
        view = gltf["bufferViews"][acc["bufferView"]]
        offset = bin_start + 8 + view["byteOffset"] + acc.get("byteOffset", 0)
        width = {"SCALAR": 1, "VEC4": 4}[acc["type"]]
        return struct.unpack_from("<" + "f"*(acc["count"]*width), glb, offset)
    times = read_f32(sampler["input"])
    quats = read_f32(sampler["output"])
    assert len(times) == 5 and all(abs(a-b)<1e-6 for a,b in zip(times, (0,.25,.5,.75,1)))
    assert len(quats) == 20
    for i in range(0,20,4):
        assert abs(sum(q*q for q in quats[i:i+4]) - 1) < 1e-5
    assert abs(quats[0])<1e-6 and abs(quats[3]-1)<1e-6
    assert abs(quats[-4])<1e-6 and abs(quats[-1]+1)<1e-6
    return {"asset":"M02-001","obj_vertices":len(vertices),"obj_triangles":len(faces),
            "glb_bytes":len(glb),"glb_animation_keys":len(times),"materials":len(materials),
            "result":"PASS","blender_execution":"NOT RUN","unity_execution":"NOT RUN"}

if __name__ == "__main__":
    print(json.dumps(verify_m02(),indent=2))
