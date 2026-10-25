#!/usr/bin/env python3
"""Independent GLB and geometry manifest QA for M04-017 (no bpy dependency)."""
import argparse
import json
import math
import struct
from pathlib import Path

STEM="M04-017_teleport_visual_ring"


def glb_report(path):
    raw=path.read_bytes()
    assert raw[:4]==b"glTF" and len(raw)>=20,"not GLB"
    version, length=struct.unpack_from("<II",raw,4)
    assert version==2 and length==len(raw),"bad GLB header"
    chunk_length, chunk_type=struct.unpack_from("<I4s",raw,12)
    assert chunk_type==b"JSON","GLB JSON chunk missing"
    data=json.loads(raw[20:20+chunk_length].decode("utf-8"))
    nodes={i:node.get("name","") for i,node in enumerate(data.get("nodes",[]))}
    ring={i for i,n in nodes.items() if n.startswith("SPIN_PIVOT_Z_RING_CENTER")}
    sectors={i for i,n in nodes.items() if n.startswith("SEGMENT_PIVOT_")}
    assert len(ring)==1,("ring pivot count",len(ring))
    assert len(sectors)==8,("sector pivot count",len(sectors))
    rotations=set()
    translations=set()
    for animation in data.get("animations",[]):
        for channel in animation.get("channels",[]):
            target=channel.get("target",{})
            if target.get("path")=="rotation":
                rotations.add(target.get("node"))
            if target.get("path")=="translation":
                translations.add(target.get("node"))
    assert ring.issubset(rotations),"ring rotation channel missing"
    assert sectors.issubset(translations),"sector radial translation channels missing"
    assert len(data.get("materials",[]))>=7,"material loss"
    assert len(data.get("meshes",[]))>=50,"mesh loss"
    return {"glb_bytes":len(raw),"meshes":len(data["meshes"]),
            "materials":len(data["materials"]),"sector_translation_channels":len(sectors),
            "ring_rotation_channels":len(ring),
            "animation_clips":len(data.get("animations",[]))}


def obj_report(path):
    vertices=0
    faces=0
    for line in path.read_text(encoding="utf-8",errors="replace").splitlines():
        if line.startswith("v "):
            xyz=[float(v) for v in line.split()[1:4]]
            assert len(xyz)==3 and all(math.isfinite(v) for v in xyz)
            vertices+=1
        elif line.startswith("f "):
            indices=[int(tok.split("/")[0]) for tok in line.split()[1:]]
            assert len(indices)>=3
            assert all(0<abs(i)<=vertices for i in indices)
            faces+=1
    assert vertices>=2000 and faces>=1000,(vertices,faces)
    return {"obj_vertices":vertices,"obj_faces":faces}


def main():
    parser=argparse.ArgumentParser()
    parser.add_argument("--out",default="build/M04-017")
    out=Path(parser.parse_args().out)
    manifest=json.loads((out/(STEM+"_manifest.json")).read_text(encoding="utf-8"))
    assert manifest["id"]=="M04-017"
    assert manifest["qa_geometry"]=="PASS"
    assert manifest["sector_pivots"]==8
    assert manifest["materials"]>=7 and manifest["meshes"]>=50
    assert manifest["ring_rotation_degrees"]==360
    assert .074 <= manifest["sector_open_m"] <= .076
    assert manifest["min_adjacent_sector_angular_gap_m"]>.05
    assert manifest["min_radial_layer_gap_m"]>=.005
    assert manifest["min_support_axial_gap_m"]>.14
    assert (out/(STEM+".blend")).stat().st_size>20000
    report={"id":"M04-017","status":"PASS",**glb_report(out/(STEM+".glb"))}
    if (out/(STEM+".obj")).exists():
        report.update(obj_report(out/(STEM+".obj")))
        assert (out/(STEM+".mtl")).exists(),"OBJ material library missing"
    else:
        report["obj"]="NOT RUN"
    (out/"M04-017_independent_qa.json").write_text(
        json.dumps(report,indent=2)+"\n",encoding="utf-8")
    print("M04-017 INDEPENDENT_EXPORT_QA_PASS",json.dumps(report))


if __name__=="__main__":
    main()
