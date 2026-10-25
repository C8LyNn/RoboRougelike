#!/usr/bin/env python3
"""Independent binary export audit for M01-016 (stdlib only)."""
import argparse
import json
import struct
from pathlib import Path

def audit(folder):
    stem = folder/"M01-016_foldout_rotary_cannon"
    required = {s:Path(str(stem)+s) for s in
                (".blend",".glb",".fbx",".obj",".mtl","_qa.json")}
    for suffix,path in required.items():
        assert path.is_file() and path.stat().st_size > 40, "Missing/empty "+str(path)
    report = json.loads(required["_qa.json"].read_text(encoding="utf-8"))
    assert report["id"] == "M01-016"
    assert report["materials"] >= 3 and report["meshes"] >= 40
    assert report["aabb_intersections"] == 0
    assert report["frames_tested"] >= 60
    assert required[".blend"].read_bytes()[:7] == b"BLENDER"
    assert required[".fbx"].read_bytes().startswith(b"Kaydara FBX Binary")
    glb = required[".glb"].read_bytes()
    magic,version,length = struct.unpack_from("<4sII",glb)
    assert magic == b"glTF" and version == 2 and length == len(glb)
    chunk_length,chunk_type = struct.unpack_from("<I4s",glb,12)
    assert chunk_type == b"JSON"
    gltf = json.loads(glb[20:20+chunk_length])
    nodes = gltf.get("nodes",[])
    names = [n.get("name","") for n in nodes]
    assert any("MOUNT_TOP_SOCKET" in n for n in names)
    assert any("ROTARY_DRUM_Y_AXIS" in n for n in names)
    assert any("LEFT_FOLD_Z_AXIS" in n for n in names)
    assert any("RIGHT_FOLD_Z_AXIS" in n for n in names)
    assert len(gltf.get("materials",[])) >= 3
    assert len(gltf.get("meshes",[])) >= 40
    animations = gltf.get("animations",[])
    assert animations, "No GLB animation"
    animated_nodes = set()
    for animation in animations:
        for channel in animation.get("channels",[]):
            target = channel.get("target",{})
            if target.get("path") == "rotation":
                animated_nodes.add(target.get("node"))
    assert any("ROTARY_DRUM_Y_AXIS" in names[i] for i in animated_nodes)
    assert any("LEFT_FOLD_Z_AXIS" in names[i] for i in animated_nodes)
    assert any("RIGHT_FOLD_Z_AXIS" in names[i] for i in animated_nodes)
    obj = required[".obj"].read_text(encoding="utf-8",errors="replace")
    mtl = required[".mtl"].read_text(encoding="utf-8",errors="replace")
    assert obj.count("\nf ") >= 1000
    assert mtl.count("newmtl ") >= 3
    summary = {"status":"PASS","id":"M01-016","meshes_glb":len(gltf["meshes"]),
               "materials_glb":len(gltf["materials"]),
               "animation_channels":sum(len(a.get("channels",[])) for a in animations),
               "rotating_nodes":len(animated_nodes),"files":{k:p.stat().st_size for k,p in required.items()}}
    print(json.dumps(summary,indent=2))
    return summary

if __name__=="__main__":
    p=argparse.ArgumentParser()
    p.add_argument("--out",default="Tools/ModelFactory/M01/M01-016/generated")
    a=p.parse_args()
    audit(Path(a.out))
