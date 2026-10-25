#!/usr/bin/env python3
"""Independent M01-015 OBJ/MTL and optional Blender binary-export validator."""
import argparse
import json
import struct
from pathlib import Path

STEM="M01-015_bidirectional_turret"

def validate(out: Path, require_blender=False):
    report=json.loads((out/(STEM+"_qa.json")).read_text(encoding="utf-8"))
    assert report["asset_id"]=="M01-015"
    assert report["materials"]>=3
    assert report["watertight_oriented_positive_volume"]=="PASS"
    assert report["moving_vs_fixed_collisions"]==0
    assert report["cross_arm_collisions"]==0
    assert report["sampled_animation_frames"]==73
    assert report["minimum_radial_bearing_clearance_m"]>=.012
    assert report["minimum_separation_aabb_m"]>=.004
    assert report["aabb_pair_checks"]>10000
    assert report["cross_arm_aabb_checks"]>10000
    obj=(out/(STEM+".obj")).read_text(encoding="utf-8")
    mtl=(out/(STEM+".mtl")).read_text(encoding="utf-8")
    assert obj.count("\nv ")==report["vertices"]
    assert obj.count("\nf ")==report["triangles"]
    assert obj.count("\no ")==report["parts"]
    assert mtl.count("newmtl ")==report["materials"]
    for name in ("Pivot_Shaft_L","Pivot_Shaft_R","Arm_Barrel_L","Arm_Barrel_R",
                 "Muzzle_Emissive_L","Muzzle_Emissive_R","V_Central_Keel"):
        assert ("o "+name) in obj,name
    if not require_blender:
        return {"static":"PASS","blender":"NOT RUN",
                "parts":report["parts"],"triangles":report["triangles"],
                "materials":report["materials"]}
    assert report["backend"]=="blender-bpy"
    assert report["blender_repeatability"].startswith("PASS")
    blend=(out/(STEM+".blend")).read_bytes()
    fbx=(out/(STEM+".fbx")).read_bytes()
    glb=(out/(STEM+".glb")).read_bytes()
    assert blend.startswith(b"BLENDER")
    assert fbx.startswith(b"Kaydara FBX Binary")
    assert glb[:4]==b"glTF"
    magic,version,total=struct.unpack_from("<III",glb,0)
    assert magic==0x46546C67 and version==2 and total==len(glb)
    length,kind=struct.unpack_from("<II",glb,12)
    assert kind==0x4E4F534A
    data=json.loads(glb[20:20+length].decode("utf-8"))
    assert len(data.get("materials",[]))>=3
    names=[node.get("name","") for node in data["nodes"]]
    channels=[c for anim in data.get("animations",[]) for c in anim["channels"]]
    for target_name in ("ARM_L_YAW","ARM_R_YAW"):
        nodes=[i for i,n in enumerate(names) if target_name in n]
        assert nodes,("missing arm pivot",target_name)
        assert any(c["target"].get("node") in nodes and
                   c["target"]["path"]=="rotation" for c in channels),target_name
    assert len(data.get("meshes",[]))>=report["parts"]-2
    return {"static":"PASS","blender":"PASS",
            "glb_meshes":len(data["meshes"]),
            "glb_materials":len(data["materials"]),
            "glb_animations":len(data.get("animations",[])),
            "both_yaw_animations":"PASS",
            "blend_bytes":len(blend),"fbx_bytes":len(fbx),"glb_bytes":len(glb)}

if __name__=="__main__":
    parser=argparse.ArgumentParser()
    parser.add_argument("--out",required=True)
    parser.add_argument("--require-blender",action="store_true")
    args=parser.parse_args()
    print(json.dumps(validate(Path(args.out),args.require_blender),indent=2))
