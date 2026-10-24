#!/usr/bin/env python3
"""Independent binary export validator for M01-014. Does not use bpy."""
import argparse
import json
import math
import struct
from pathlib import Path

STEM="M01-014_electromagnetic_pulser"

def validate(out: Path, require_blender=False):
    report=json.loads((out/(STEM+"_qa.json")).read_text(encoding="utf-8"))
    assert report["asset_id"]=="M01-014"
    assert report["materials"]>=3
    assert report["watertight_oriented_positive_volume"]=="PASS"
    assert report["moving_vs_fixed_collisions"]==0
    assert report["sampled_animation_frames"]==61
    assert report["minimum_radial_bore_clearance_m"]>=.04
    assert abs(report["core_travel_m"]-.18)<1e-9
    obj=(out/(STEM+".obj")).read_text(encoding="utf-8")
    mtl=(out/(STEM+".mtl")).read_text(encoding="utf-8")
    assert obj.count("\nv ")==report["vertices"]
    assert obj.count("\nf ")==report["triangles"]
    assert mtl.count("newmtl ")==report["materials"]
    assert "o Ferrite_Core_Shaft" in obj
    assert "o Muzzle_Cyan_Annulus" in obj
    if not require_blender:
        return {"static":"PASS","blender":"NOT RUN","parts":report["parts"],
                "triangles":report["triangles"],"materials":report["materials"]}
    assert report["backend"]=="blender-bpy"
    assert report["blender_repeatability"].startswith("PASS")
    blend=(out/(STEM+".blend")).read_bytes()
    fbx=(out/(STEM+".fbx")).read_bytes()
    glb=(out/(STEM+".glb")).read_bytes()
    assert blend[:7]==b"BLENDER"
    assert fbx[:21]==b"Kaydara FBX Binary  "
    assert glb[:4]==b"glTF"
    magic,version,total=struct.unpack_from("<III",glb,0)
    assert magic==0x46546C67 and version==2 and total==len(glb)
    length,kind=struct.unpack_from("<II",glb,12)
    assert kind==0x4E4F534A
    data=json.loads(glb[20:20+length].decode("utf-8"))
    assert len(data["materials"])>=3
    names=[node.get("name","") for node in data["nodes"]]
    target=[i for i,name in enumerate(names) if "FERRITE_CORE_SLIDE_Z" in name]
    assert target, "Missing animated core pivot in GLB"
    channels=[c for anim in data.get("animations",[]) for c in anim["channels"]]
    assert any(c["target"].get("node") in target and
               c["target"]["path"]=="translation" for c in channels),channels
    assert len(data.get("meshes",[]))>=report["parts"]-2
    return {"static":"PASS","blender":"PASS",
            "glb_meshes":len(data["meshes"]),
            "glb_materials":len(data["materials"]),
            "glb_animations":len(data.get("animations",[])),
            "animated_core_pivot":"PASS",
            "blend_bytes":len(blend),"fbx_bytes":len(fbx),"glb_bytes":len(glb)}

if __name__=="__main__":
    p=argparse.ArgumentParser()
    p.add_argument("--out",required=True)
    p.add_argument("--require-blender",action="store_true")
    a=p.parse_args()
    print(json.dumps(validate(Path(a.out),a.require_blender),indent=2))
