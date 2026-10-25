#!/usr/bin/env python3
"""Independent binary glTF and manifest QA for M04-020; Python standard library."""
import json
import struct
import sys
from pathlib import Path

def validate(directory):
    root=Path(directory)
    glb=(root/"M04-020.glb").read_bytes()
    assert len(glb)>1024, "GLB unexpectedly small"
    magic,version,total=struct.unpack_from("<4sII",glb,0)
    assert magic==b"glTF" and version==2 and total==len(glb), "Invalid GLB header"
    offset=12
    chunks=[]
    while offset<len(glb):
        size,kind=struct.unpack_from("<I4s",glb,offset)
        offset+=8
        assert offset+size<=len(glb), "GLB chunk overflow"
        chunks.append((kind,glb[offset:offset+size]))
        offset+=size
    assert chunks[0][0]==b"JSON", "Missing glTF JSON chunk"
    gltf=json.loads(chunks[0][1].decode("utf-8"))
    manifest=json.loads((root/"M04-020_QA.json").read_text(encoding="utf-8"))
    assert manifest["id"]=="M04-020"
    assert manifest["mesh_count"]>=35
    assert manifest["aabb_pair_checks"]>5000
    assert len(gltf.get("meshes",[]))>=35
    assert len(gltf.get("materials",[]))>=3
    paths={ch["target"]["path"] for a in gltf.get("animations",[]) for ch in a.get("channels",[])}
    assert "translation" in paths, "No telescopic translation exported"
    assert "rotation" in paths, "No rotor rotation exported"
    assert (root/"M04-020.blend").stat().st_size>1024
    result={"id":"M04-020","glb_bytes":len(glb),
            "glb_meshes":len(gltf["meshes"]),
            "glb_materials":len(gltf["materials"]),
            "glb_animations":len(gltf["animations"]),
            "animated_paths":sorted(paths),
            "blender_geometry":"PASS (manifest from executed generator)",
            "independent_glb":"PASS",
            "unity":"NOT RUN"}
    print(json.dumps(result,indent=2))
    return result

if __name__=="__main__":
    validate(sys.argv[1] if len(sys.argv)>1 else "build/M04-020")
