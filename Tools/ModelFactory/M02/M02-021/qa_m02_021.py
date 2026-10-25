#!/usr/bin/env python3
"""Independent CPython kinematics and exported GLB inspection for M02-021."""
import json
import math
import struct
import sys
from pathlib import Path

ID="M02-021"
OUT=Path(__file__).resolve().parent/"generated"

def kinematics():
    mins=[float("inf")]*4
    for frame in range(1,122):
        s=(1-math.cos(2*math.pi*(frame-1)/120))/2
        dz=-.250*s
        values=[
            (-.340+dz)-(-.700),
            (-.700)-(-.738+dz),
            (-.355+dz)-(-.650),
            (-.580+dz)-(-.940)
        ]
        mins=[min(a,b) for a,b in zip(mins,values)]
    assert mins[0]>.100 and mins[1]>.030
    assert mins[2]>.040 and mins[3]>.090
    assert min(.086-.078,.055-.052,.031-.028,.018-.012)>.002
    return {"frames":121,"stroke_m":.250,"minimum_overlap_m":mins[0],
            "minimum_pad_to_cage_gap_m":mins[1],
            "minimum_nut_engagement_margin_m":mins[2],
            "minimum_guide_rod_margin_m":mins[3]}

def glb_check():
    blob=(OUT/(ID+"_animated.glb")).read_bytes()
    magic,version,total=struct.unpack_from("<4sII",blob,0)
    assert magic==b"glTF" and version==2 and total==len(blob)
    chunk_len,chunk_type=struct.unpack_from("<I4s",blob,12)
    assert chunk_type==b"JSON"
    doc=json.loads(blob[20:20+chunk_len].decode("utf-8"))
    assert len(doc.get("materials",[]))>=3
    assert len(doc.get("meshes",[]))>=55
    nodes=[n.get("name","") for n in doc.get("nodes",[])]
    targets={}
    for anim in doc.get("animations",[]):
        for ch in anim.get("channels",[]):
            name=nodes[ch["target"]["node"]]
            path=ch["target"]["path"]
            targets[(name,path)]=(anim,ch)
    for key in [("SLIDE_TELESCOPIC_Z","translation"),
                ("LEAD_SCREW_ROTATE_Z","rotation"),
                ("DRIVE_KNOB_ROTATE_Z","rotation")]:
        assert key in targets, ("missing animation",key,sorted(targets))
    # Check that glTF contains nonconstant keyframe data, not just empty channels.
    offset=20+chunk_len
    offset+=(4-offset%4)%4
    binary=None
    while offset+8<=len(blob):
        length,kind=struct.unpack_from("<I4s",blob,offset)
        offset+=8
        if kind==b"BIN\x00": binary=blob[offset:offset+length]
        offset+=length
    assert binary is not None
    spans={}
    for key,(anim,ch) in targets.items():
        sampler=anim["samplers"][ch["sampler"]]
        accessor=doc["accessors"][sampler["output"]]
        assert accessor["componentType"]==5126
        count=accessor["count"]
        assert count>=20,(key,count)
        view=doc["bufferViews"][accessor["bufferView"]]
        stride=4*(3 if accessor["type"]=="VEC3" else 4)
        step=view.get("byteStride",stride)
        start=view.get("byteOffset",0)+accessor.get("byteOffset",0)
        fmt="<"+("fff" if accessor["type"]=="VEC3" else "ffff")
        vals=[struct.unpack_from(fmt,binary,start+i*step) for i in range(count)]
        diffs=[math.dist(vals[i],vals[0]) for i in range(1,count)]
        spans[key]=max(diffs,default=0)
    assert spans[("SLIDE_TELESCOPIC_Z","translation")]>.245,spans
    assert spans[("LEAD_SCREW_ROTATE_Z","rotation")]>.1,spans
    assert spans[("DRIVE_KNOB_ROTATE_Z","rotation")]>.1,spans
    return {"bytes":len(blob),"meshes":len(doc["meshes"]),
            "materials":len(doc["materials"]),"nodes":len(nodes),
            "animations":len(doc["animations"]),"nonconstant_targets":len(spans),
            "maximum_sample_distance":{str(k):v for k,v in spans.items()}}

if __name__=="__main__":
    report={"id":ID,"independent_kinematics":kinematics()}
    if "--math-only" not in sys.argv:
        generated=json.loads((OUT/(ID+"_qa.json")).read_text(encoding="utf-8"))
        assert generated["id"]==ID
        assert generated["reentrant"]=="PASS" and generated["manifold"]=="PASS"
        assert generated["geometry"]["materials"]>=3
        assert generated["topology_meshes_checked"]>=55
        assert (OUT/(ID+".blend")).stat().st_size>10000
        report["blender"]=generated["blender"]
        report["export"]=glb_check()
    print(json.dumps(report,indent=2,ensure_ascii=False))
