#!/usr/bin/env python3
"""Independent stdlib-only validation of exported M04-019 BLEND/GLB/manifest."""
import argparse
import json
import math
import struct
from pathlib import Path

ID="M04-019"
STEM="M04-019_segment_turbine"


def inspect_glb(path):
    raw=path.read_bytes()
    assert len(raw)>20000 and raw[:4]==b"glTF","GLB signature/size"
    version,total=struct.unpack_from("<II",raw,4)
    assert version==2 and total==len(raw),"GLB header"
    chunk_len,kind=struct.unpack_from("<I4s",raw,12)
    assert kind==b"JSON","missing GLB JSON chunk"
    gltf=json.loads(raw[20:20+chunk_len].decode("utf-8"))
    nodes={i:node.get("name","") for i,node in enumerate(gltf["nodes"])}
    rotor={i for i,n in nodes.items() if n.endswith("ROTOR_PIVOT_AXIS_X")}
    petals={i for i,n in nodes.items() if "PETAL_TANGENT_Z_PIVOT_" in n}
    mounts={i for i,n in nodes.items() if n.endswith("SIDE_MOUNT_X0_ORIGIN")}
    assert len(rotor)==1 and len(petals)==6 and len(mounts)==1,(
        "missing mount/rotor/petal pivots",rotor,petals,mounts)
    animated=set()
    for a in gltf.get("animations",[]):
        for ch in a.get("channels",[]):
            target=ch.get("target",{})
            if target.get("path")=="rotation":
                animated.add(target.get("node"))
    assert rotor.issubset(animated),"rotor rotation missing"
    assert petals.issubset(animated),"hinged shell rotation missing"
    assert len(gltf.get("meshes",[]))>=35,"mesh export incomplete"
    assert len(gltf.get("materials",[]))>=8,"materials lost"
    assert len(gltf.get("animations",[]))>=1,"animation missing"
    return dict(glb_meshes=len(gltf["meshes"]),
                glb_materials=len(gltf["materials"]),
                glb_animation_clips=len(gltf["animations"]),
                animated_petal_pivots=len(petals),
                animated_rotor_pivots=len(rotor),
                glb_bytes=len(raw))


def main():
    p=argparse.ArgumentParser()
    p.add_argument("--out",default="build/M04-019")
    out=Path(p.parse_args().out)
    manifest=json.loads((out/(STEM+"_manifest.json")).read_text("utf-8"))
    assert manifest["id"]==ID
    assert manifest["materials"]>=8 and manifest["meshes"]>=35
    assert manifest["petals"]==6 and manifest["blades"]==7
    assert manifest["animation_frames"]==121 and manifest["fps"]==30
    assert manifest["shell_deploy_deg"]==30
    assert manifest["sampled_frames"]==41
    assert manifest["transformed_vertex_checks"]>10000
    assert manifest["qa_geometry"].startswith("PASS")
    assert manifest["min_shell_stator_clearance_m"]>.017
    assert manifest["min_rotor_bore_clearance_m"]>.022
    assert manifest["min_adjacent_petal_angular_gap_deg"]>2.5
    assert "SIDE X=0" in manifest["mount"]
    assert (out/(STEM+".blend")).stat().st_size>10000
    result=dict(id=ID,status="PASS",
                sampled_clearance="PASS — vertex and angular envelope only",
                unity_import="NOT RUN",
                unity_editmode="NOT RUN",
                unity_playmode="NOT RUN",
                exact_swept_triangle_collision="NOT RUN",
                manual_visual_review="NOT RUN")
    result.update(inspect_glb(out/(STEM+".glb")))
    (out/(STEM+"_qa.json")).write_text(
        json.dumps(result,indent=2,ensure_ascii=False)+"\n",encoding="utf-8")
    print(json.dumps(result,indent=2,ensure_ascii=False))


if __name__=="__main__":
    main()
