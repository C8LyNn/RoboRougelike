#!/usr/bin/env python3
"""M01-019 independent binary export audit, stdlib-only."""
import argparse
import json
import struct
from pathlib import Path

ID="M01-019"
STEM="M01-019_six_tube_smoke_launcher"


def audit(folder):
    prefix=folder/STEM
    paths={s:Path(str(prefix)+s) for s in
           (".blend",".glb",".fbx",".obj",".mtl","_qa.json")}
    for ext,p in paths.items():
        assert p.is_file() and p.stat().st_size>80, "Missing export "+str(p)
    report=json.loads(paths["_qa.json"].read_text(encoding="utf-8"))
    assert report["id"]==ID and report["status"]=="BLENDER_QA_PASS"
    assert report["materials"]>=3 and report["meshes"]>=55
    assert report["frames_tested"]==73
    assert report["aabb_checks"]>10000
    assert report["aabb_intersections"]==0
    assert report["min_aabb_clearance_m"]>.012
    assert abs(report["cassette_rotation_radians"]-6.283185)<1e-5
    assert paths[".blend"].read_bytes().startswith(b"BLENDER")
    assert paths[".fbx"].read_bytes().startswith(b"Kaydara FBX Binary")
    raw=paths[".glb"].read_bytes()
    magic,version,length=struct.unpack_from("<4sII",raw,0)
    assert magic==b"glTF" and version==2 and length==len(raw)
    chunk_len,chunk_type=struct.unpack_from("<I4s",raw,12)
    assert chunk_type==b"JSON"
    gltf=json.loads(raw[20:20+chunk_len])
    nodes=[n.get("name","") for n in gltf.get("nodes",[])]
    assert any("MOUNT_SIDE_SOCKET" in n for n in nodes)
    slide={i for i,n in enumerate(nodes) if "CASSETTE_INDEX_Y" in n}
    assert slide, "Missing cassette rotation pivot"
    assert len(gltf.get("materials",[]))>=6
    assert len(gltf.get("meshes",[]))>=55
    animations=gltf.get("animations",[])
    assert animations, "No exported animation"
    channels=[c for a in animations for c in a.get("channels",[])]
    assert any(c.get("target",{}).get("node") in slide and
               c.get("target",{}).get("path")=="rotation"
               for c in channels), "Missing cassette rotation channel"
    obj=paths[".obj"].read_text(encoding="utf-8",errors="replace")
    mtl=paths[".mtl"].read_text(encoding="utf-8",errors="replace")
    assert obj.count("\nf ")>800, "OBJ lacks geometry"
    assert mtl.count("newmtl ")>=3, "MTL lacks material variety"
    result={"id":ID,"status":"PASS","glb_meshes":len(gltf["meshes"]),
            "glb_materials":len(gltf["materials"]),
            "animation_channels":len(channels),
            "minimum_clearance_m":report["min_aabb_clearance_m"],
            "files":{k:v.stat().st_size for k,v in paths.items()}}
    print(json.dumps(result,indent=2))
    return result


if __name__=="__main__":
    parser=argparse.ArgumentParser()
    parser.add_argument("--out",default="generated")
    opts=parser.parse_args()
    audit(Path(opts.out))
