#!/usr/bin/env python3
"""Independent M04-018 export and animation QA (Python stdlib only)."""
import argparse
import json
import math
import struct
from pathlib import Path

STEM="M04-018_stabilizer_anchor"


def glb_check(path):
    raw=path.read_bytes()
    assert len(raw)>20 and raw[:4]==b"glTF","invalid GLB signature"
    version,total=struct.unpack_from("<II",raw,4)
    assert version==2 and total==len(raw),"invalid GLB length/version"
    size,kind=struct.unpack_from("<I4s",raw,12)
    assert kind==b"JSON","GLB JSON chunk missing"
    gltf=json.loads(raw[20:20+size].decode("utf-8"))
    names={i:n.get("name","") for i,n in enumerate(gltf.get("nodes",[]))}
    pivots={i for i,n in names.items() if n.startswith("CLAW_DEPLOY_PIVOT_Z_")}
    carriage={i for i,n in names.items() if n.startswith("CARRIAGE_PIVOT_Y_TELESCOPE")}
    mount={i for i,n in names.items() if n.startswith("MOUNT_UNDERSIDE_ORIGIN")}
    assert len(pivots)==4 and len(carriage)==1 and len(mount)==1,("pivots",names)
    rotations=set()
    translations=set()
    for animation in gltf.get("animations",[]):
        for ch in animation.get("channels",[]):
            target=ch.get("target",{})
            if target.get("path")=="rotation":
                rotations.add(target.get("node"))
            elif target.get("path")=="translation":
                translations.add(target.get("node"))
    assert pivots.issubset(rotations),"missing claw rotation channels"
    assert carriage.issubset(translations),"missing telescoping channel"
    assert len(gltf.get("meshes",[]))>=50,"mesh count loss"
    assert len(gltf.get("materials",[]))>=8,"material count loss"
    return {"glb_bytes":len(raw),"meshes":len(gltf["meshes"]),
            "materials":len(gltf["materials"]),
            "claw_rotation_channels":len(pivots),
            "carriage_translation_channels":len(carriage),
            "animation_clips":len(gltf.get("animations",[]))}


def obj_check(path):
    vertices=faces=0
    for line in path.read_text(encoding="utf-8",errors="replace").splitlines():
        if line.startswith("v "):
            xyz=[float(x) for x in line.split()[1:4]]
            assert len(xyz)==3 and all(math.isfinite(x) for x in xyz)
            vertices+=1
        elif line.startswith("f "):
            ids=[int(s.split("/")[0]) for s in line.split()[1:]]
            assert len(ids)>=3 and all(0<abs(i)<=vertices for i in ids)
            faces+=1
    assert vertices>=1000 and faces>=800,(vertices,faces)
    return {"obj_vertices":vertices,"obj_faces":faces}


def main():
    p=argparse.ArgumentParser()
    p.add_argument("--out",default="build/M04-018")
    out=Path(p.parse_args().out)
    manifest=json.loads((out/(STEM+"_manifest.json")).read_text(encoding="utf-8"))
    assert manifest["id"]=="M04-018"
    assert manifest["qa_geometry"]=="PASS"
    assert manifest["materials"]>=8 and manifest["meshes"]>=50
    assert manifest["claw_pivots"]==4
    assert manifest["stroke_m"]==.22 and manifest["claw_deploy_deg"]==52
    assert manifest["sampled_animation_frames"]>=25
    assert manifest["sampled_vertex_checks"]>10000
    assert manifest["min_radial_clearance_m"]>.02
    assert manifest["min_sector_angular_clearance_deg"]>12
    assert (out/(STEM+".blend")).stat().st_size>20000
    report={"id":"M04-018","status":"PASS",**glb_check(out/(STEM+".glb"))}
    obj=out/(STEM+".obj")
    if obj.exists():
        report.update(obj_check(obj))
        assert (out/(STEM+".mtl")).exists(),"OBJ MTL missing"
    else:
        report["obj_export"]="NOT RUN"
    (out/"M04-018_independent_qa.json").write_text(
        json.dumps(report,indent=2)+"\n",encoding="utf-8")
    print("M04-018 INDEPENDENT_QA_PASS",json.dumps(report))


if __name__=="__main__":
    main()
