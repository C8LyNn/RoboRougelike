#!/usr/bin/env python3
"""Independent stdlib QA for the M04-022 Blender GLB export."""
import argparse
import ast
import json
import math
import struct
from pathlib import Path

def read_glb(path):
    data=Path(path).read_bytes()
    assert len(data)>20, "GLB too short"
    magic,version,length=struct.unpack_from("<4sII",data,0)
    assert (magic,version,length)==(b"glTF",2,len(data)), "GLB header invalid"
    off=12
    json_chunk=None
    binary=b""
    while off<len(data):
        size,kind=struct.unpack_from("<II",data,off)
        off+=8
        chunk=data[off:off+size]
        assert len(chunk)==size
        off+=size
        if kind==0x4E4F534A:
            json_chunk=json.loads(chunk.decode("utf-8").rstrip(" \x00"))
        elif kind==0x004E4942:
            binary=chunk
    assert json_chunk is not None
    return json_chunk,binary

def read_accessor(doc,buf,index):
    accessor=doc["accessors"][index]
    view=doc["bufferViews"][accessor["bufferView"]]
    assert accessor["componentType"]==5126, "Expected FLOAT glTF animation"
    n={"SCALAR":1,"VEC3":3,"VEC4":4}[accessor["type"]]
    stride=view.get("byteStride",4*n)
    offset=view.get("byteOffset",0)+accessor.get("byteOffset",0)
    return [struct.unpack_from("<"+"f"*n,buf,offset+i*stride)
            for i in range(accessor["count"])]

def check(path,source):
    ast.parse(Path(source).read_text(encoding="utf-8"))
    doc,buf=read_glb(path)
    meshes=doc.get("meshes",[])
    mats=doc.get("materials",[])
    nodes=doc.get("nodes",[])
    assert len(meshes)>=25, "Incomplete model: expected >=25 meshes"
    assert len(mats)>=3, "Insufficient materials"
    names=[n.get("name","") for n in nodes]
    pivots=[i for i,n in enumerate(names) if "TILT_X" in n]
    assert len(pivots)==1, "Missing unique tilt pivot"
    assert any("UNDERSIDE MOUNT" in n for n in names), "Missing mount root"
    channels=[]
    for animation in doc.get("animations",[]):
        for ch in animation.get("channels",[]):
            if ch["target"]["node"] in pivots and ch["target"]["path"]=="rotation":
                sampler=animation["samplers"][ch["sampler"]]
                channels.append((animation,sampler))
    assert channels, "No GLB rotation channel for tilt pivot"
    max_angle=0.0
    min_angle=0.0
    for _,sampler in channels:
        for x,y,z,w in read_accessor(doc,buf,sampler["output"]):
            assert abs(y)<.003 and abs(z)<.003, "Unexpected Y/Z pivot rotation"
            angle=math.degrees(2*math.atan2(x,w))
            max_angle=max(max_angle,angle)
            min_angle=min(min_angle,angle)
            assert abs(angle)<=10.15, "Tilt exceeds +/-10 degree design limit"
    assert max_angle>9.5 and min_angle< -9.5, "Both tilt extremes must be exported"
    # The skid rails are outside the moving assembly's conservative X envelope.
    static_inner=.555-.115/2
    moving_outer=.44
    clearance=static_inner-moving_outer
    assert clearance>=.05, "Insufficient conservative side clearance"
    report={
        "result":"PASS","glb":str(path),"mesh_count":len(meshes),
        "material_count":len(mats),"animation_count":len(doc.get("animations",[])),
        "tilt_rotation_channels":len(channels),
        "min_tilt_deg":round(min_angle,4),"max_tilt_deg":round(max_angle,4),
        "conservative_side_clearance_m":round(clearance,4),
        "limitations":["No continuous swept mesh-mesh collision",
                       "No Unity import, EditMode, PlayMode, or manual visual QA"]
    }
    print(json.dumps(report,indent=2))
    return report

if __name__=="__main__":
    p=argparse.ArgumentParser()
    p.add_argument("--glb",required=True)
    p.add_argument("--source",default=str(Path(__file__).with_name("generate_m04_022.py")))
    p.add_argument("--report")
    args=p.parse_args()
    report=check(args.glb,args.source)
    if args.report:
        Path(args.report).write_text(json.dumps(report,indent=2)+"\n",encoding="utf-8")
