#!/usr/bin/env python3
"""Independent stdlib-only binary export audit for M01-017."""
import argparse
import json
import struct
from pathlib import Path

ID = "M01-017"
STEM = "M01-017_rapid_ball_thrower"

def audit(folder):
    stem = folder/STEM
    paths = {s:Path(str(stem)+s) for s in
             (".blend",".glb",".fbx",".obj",".mtl","_qa.json")}
    for suffix,path in paths.items():
        assert path.is_file() and path.stat().st_size > 80, "Missing or empty "+str(path)
    report = json.loads(paths["_qa.json"].read_text(encoding="utf-8"))
    assert report["id"] == ID and report["status"] == "BLENDER_QA_PASS"
    assert report["materials"] >= 3 and report["meshes"] >= 35
    assert report["frames_tested"] == 73 and report["aabb_checks"] >= 20000
    assert report["aabb_intersections"] == 0
    assert report["min_aabb_clearance_m"] > 0.005
    assert paths[".blend"].read_bytes().startswith(b"BLENDER")
    assert paths[".fbx"].read_bytes().startswith(b"Kaydara FBX Binary")
    raw = paths[".glb"].read_bytes()
    magic,version,total = struct.unpack_from("<4sII",raw,0)
    assert magic == b"glTF" and version == 2 and total == len(raw)
    chunk_length,chunk_type = struct.unpack_from("<I4s",raw,12)
    assert chunk_type == b"JSON"
    gltf = json.loads(raw[20:20+chunk_length])
    nodes = [n.get("name","") for n in gltf.get("nodes",[])]
    assert any("MOUNT_TOP_SOCKET" in n for n in nodes)
    rotor_nodes = {i for i,n in enumerate(nodes) if "FEED_ROTOR_X_AXIS" in n}
    assert rotor_nodes, "Missing rotating feeder pivot"
    assert len(gltf.get("materials",[])) >= 3
    assert len(gltf.get("meshes",[])) >= 35
    assert len(gltf.get("animations",[])) >= 1
    channels = [ch for anim in gltf["animations"] for ch in anim.get("channels",[])]
    assert any(ch.get("target",{}).get("node") in rotor_nodes and
               ch.get("target",{}).get("path") == "rotation" for ch in channels), "Missing rotor animation"
    obj = paths[".obj"].read_text(encoding="utf-8",errors="replace")
    mtl = paths[".mtl"].read_text(encoding="utf-8",errors="replace")
    assert obj.count("\nf ") >= 1000
    assert mtl.count("newmtl ") >= 3
    result = {"id":ID,"status":"PASS","meshes_glb":len(gltf["meshes"]),
              "materials_glb":len(gltf["materials"]),"animation_channels":len(channels),
              "min_clearance_m":report["min_aabb_clearance_m"],
              "files":{suffix:path.stat().st_size for suffix,path in paths.items()}}
    print(json.dumps(result,indent=2))
    return result

if __name__ == "__main__":
    parser=argparse.ArgumentParser()
    parser.add_argument("--out",default="generated")
    args=parser.parse_args()
    audit(Path(args.out))
