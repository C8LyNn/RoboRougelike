#!/usr/bin/env python3
"""M02-023 independent chain path, mesh export and animated GLB validation."""
import argparse
import ast
import json
import struct
from pathlib import Path
from kinematics_m02_023 import ID, LINKS, FRAMES, analytical_qa

HERE=Path(__file__).resolve().parent
OUT=HERE/"generated"

def check_source():
    source=(HERE/"generate_m02_023.py").read_text(encoding="utf-8")
    tree=ast.parse(source)
    compile(tree,str(HERE/"generate_m02_023.py"),"exec")
    functions={n.name for n in tree.body if isinstance(n,ast.FunctionDef)}
    assert {"build","reset","topology","main"}.issubset(functions)
    for required in ("bpy.ops.export_scene.gltf","bpy.ops.wm.save_as_mainfile",
                     "MOUNT_SIDE_SOCKET_UNITY_1M","keyframe_insert"):
        assert required in source,required
    assert "from kinematics_m02_023 import" in source

def check_glb():
    blend=OUT/(ID+".blend")
    glb=OUT/(ID+"_animated.glb")
    report=OUT/(ID+"_qa.json")
    assert blend.stat().st_size>10000
    data=glb.read_bytes()
    assert len(data)>10000
    magic,version,length=struct.unpack_from("<4sII",data,0)
    assert magic==b"glTF" and version==2 and length==len(data)
    json_length,json_type=struct.unpack_from("<I4s",data,12)
    assert json_type==b"JSON"
    root=json.loads(data[20:20+json_length].decode("utf-8"))
    nodes=root.get("nodes",[])
    names={node.get("name","") for node in nodes}
    assert len(root.get("meshes",[]))>=250,len(root.get("meshes",[]))
    assert len(root.get("materials",[]))>=8
    expected={f"CHAIN_LINK_{i:02d}_TRAVEL" for i in range(LINKS)}
    expected|={"INPUT_BIG_24T_SPIN_Y","OUTPUT_SMALL_16T_SPIN_Y"}
    assert expected.issubset(names),sorted(expected-names)
    animations=root.get("animations",[])
    assert len(animations)==1,("expected one synchronized NLA cycle",len(animations))
    channels=[]
    for anim in animations:
        channels.extend(anim.get("channels",[]))
    targets={}
    for channel in channels:
        target=channel.get("target",{})
        index=target.get("node")
        if not isinstance(index,int):continue
        name=nodes[index].get("name","")
        targets.setdefault(name,set()).add(target.get("path"))
    assert expected.issubset(targets),sorted(expected-set(targets))
    assert all("rotation" in targets[n] for n in expected)
    assert all("translation" in targets[n] for n in expected if n.startswith("CHAIN_LINK_"))
    for animation in animations:
        assert animation.get("samplers"),"animation without sampler"
    # Decode the actual glTF BIN accessors to verify nonzero animation motion.
    binary_header=20+json_length
    bin_size,bin_type=struct.unpack_from("<I4s",data,binary_header)
    assert bin_type==b"BIN"+bytes([0])
    bin_start=binary_header+8
    assert bin_start+bin_size<=len(data)
    def read_floats(accessor_index):
        accessor=root["accessors"][accessor_index]
        view=root["bufferViews"][accessor["bufferView"]]
        assert accessor["componentType"]==5126
        width={"SCALAR":1,"VEC3":3,"VEC4":4}[accessor["type"]]
        stride=view.get("byteStride",width*4)
        offset=bin_start+view.get("byteOffset",0)+accessor.get("byteOffset",0)
        return [struct.unpack_from("<"+"f"*width,data,offset+i*stride)
                for i in range(accessor["count"])]
    def movement(name,path):
        for animation in animations:
            for channel in animation["channels"]:
                target=channel.get("target",{})
                idx=target.get("node")
                if idx is None or nodes[idx].get("name")!=name or target.get("path")!=path:
                    continue
                sampler=animation["samplers"][channel["sampler"]]
                times=read_floats(sampler["input"])
                values=read_floats(sampler["output"])
                assert len(times)>=60 and times[-1][0]-times[0][0]>=3.9
                first=values[0]
                return max(sum((v[k]-first[k])**2 for k in range(len(first)))**.5
                           for v in values)
        raise AssertionError(("missing channel",name,path))
    assert movement("CHAIN_LINK_00_TRAVEL","translation")>.30
    assert movement("INPUT_BIG_24T_SPIN_Y","rotation")>.7
    assert movement("OUTPUT_SMALL_16T_SPIN_Y","rotation")>.7
    qa=json.loads(report.read_text(encoding="utf-8"))
    assert qa["id"]==ID
    assert qa["materials"]==8
    assert qa["mesh_objects"]>=250
    assert qa["reentrant"]==qa["manifold"]=="PASS"
    assert qa["analytical_qa"]["plate_sat_checks"]>100000
    return {"blend_bytes":blend.stat().st_size,
            "glb_bytes":len(data),"meshes":len(root["meshes"]),
            "materials":len(root["materials"]),"animations":len(animations),
            "animated_nodes":len(targets),
            "required_animated_groups":len(expected),
            "channel_paths":sorted({p for paths in targets.values() for p in paths})}

def main():
    parser=argparse.ArgumentParser()
    parser.add_argument("--math-only",action="store_true")
    args=parser.parse_args()
    check_source()
    math_report=analytical_qa()
    result={"id":ID,"source":"PASS","math":"PASS","kinematics":math_report}
    if not args.math_only:
        result["glb"]=check_glb()
    print(json.dumps(result,indent=2,sort_keys=True))

if __name__=="__main__":
    main()
