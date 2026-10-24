#!/usr/bin/env python3
"""Independent on-disk export QA for M01-011. Standard Python only."""
import json
from pathlib import Path
import struct
import sys

ID = "M01-011"
STEM = "M01-011_long_precision_rifle"


def require(cond, msg):
    if not cond:
        raise AssertionError(msg)


def parse_glb(path):
    blob = path.read_bytes()
    require(len(blob)>1024,"GLB too small")
    magic,version,length = struct.unpack_from("<4sII",blob,0)
    require((magic,version,length)==(b"glTF",2,len(blob)),
            "invalid GLB 2.0 header")
    pos=12
    json_chunk=None
    bin_chunk=None
    while pos<len(blob):
        size,kind=struct.unpack_from("<I4s",blob,pos)
        pos+=8
        data=blob[pos:pos+size]
        require(len(data)==size,"truncated GLB chunk")
        if kind==b"JSON":
            json_chunk=json.loads(data.decode("utf-8"))
        if kind==b"BIN\x00":
            bin_chunk=data
        pos+=size
    require(json_chunk is not None and bin_chunk is not None,
            "GLB JSON/BIN missing")
    return json_chunk,bin_chunk


def accessor_data(glb,buffer,index):
    accessor=glb["accessors"][index]
    require(accessor["componentType"]==5126,"animation must use float32")
    n={"SCALAR":1,"VEC3":3,"VEC4":4}[accessor["type"]]
    view=glb["bufferViews"][accessor["bufferView"]]
    offset=view.get("byteOffset",0)+accessor.get("byteOffset",0)
    stride=view.get("byteStride",4*n)
    result=[]
    for i in range(accessor["count"]):
        values=struct.unpack_from("<"+"f"*n,buffer,offset+i*stride)
        result.append(values)
    return result


def validate(folder):
    paths={suffix:folder/(STEM+suffix)
           for suffix in (".blend","_animated.glb","_animated.fbx",
                          ".obj",".mtl")}
    for suffix,path in paths.items():
        require(path.exists() and path.stat().st_size>100,
                "missing/empty "+suffix)
    require(paths[".blend"].read_bytes()[:7]==b"BLENDER",
            "not a real Blender project")
    require(paths["_animated.fbx"].read_bytes()[:18]==b"Kaydara FBX Binary",
            "not a binary FBX")
    glb,bin_data=parse_glb(paths["_animated.glb"])
    require(len(glb.get("meshes",[]))>=50,"GLB mesh count too small")
    require(len(glb.get("materials",[]))>=3,"GLB materials missing")
    names={i:n.get("name","") for i,n in enumerate(glb["nodes"])}
    require("MOUNT_TOP_SOCKET" in names.values(),"mount node absent")
    require("BOLT_CARRIER_R" in names.values(),"animated bolt node absent")
    bolt_index=next(i for i,n in names.items() if n=="BOLT_CARRIER_R")
    channels=[]
    travel_values=[]
    for animation in glb.get("animations",[]):
        for channel in animation["channels"]:
            target=channel["target"]
            if target.get("node")==bolt_index and target["path"]=="translation":
                sampler=animation["samplers"][channel["sampler"]]
                times=accessor_data(glb,bin_data,sampler["input"])
                translations=accessor_data(glb,bin_data,sampler["output"])
                require(len(times)==len(translations) and len(times)>=3,
                        "bolt animation insufficient samples")
                require(times[0][0]<=.001 and times[-1][0]>.5,
                        "bolt animation timing invalid")
                for vec in translations:
                    travel_values.append(vec)
                channels.append({"samples":len(times),
                                 "duration_s":round(times[-1][0],3)})
    require(channels,"bolt translation animation absent")
    excursions=[max(v[i] for v in travel_values)-min(v[i] for v in travel_values)
                for i in range(3)]
    require(.115<=max(excursions)<=.125,
            "bolt stroke not 120mm: "+str(excursions))
    # OBJ contains only the intended model, not the startup cube.
    obj=paths[".obj"].read_text(encoding="utf-8",errors="replace").splitlines()
    vertices=sum(line.startswith("v ") for line in obj)
    faces=sum(line.startswith("f ") for line in obj)
    names_obj=[line[2:].strip() for line in obj if line.startswith("o ")]
    require(vertices>2000 and faces>2000,"OBJ geometry missing")
    require(not any(n in ("Cube","Camera","Light") for n in names_obj),
            "factory startup objects leaked into OBJ")
    require(any("Bored_Long_Barrel" in n for n in names_obj),
            "true bored barrel absent")
    require(any("Bolt_Sliding_Shoe" in n for n in names_obj),
            "bolt shoe absent")
    mtl=paths[".mtl"].read_text(encoding="utf-8",errors="replace")
    materials=sum(line.startswith("newmtl ") for line in mtl.splitlines())
    require(materials>=3,"MTL material count too low")
    report=json.loads((folder/(ID+"_qa.json")).read_text(encoding="utf-8"))
    require(report["id"]==ID and report["status"]=="BLENDER_EXPORTED",
            "Blender did not report actual export")
    require(report["mesh_objects"]>=50 and report["materials"]>=3,
            "Blender geometry thresholds")
    require(report["motion_samples"]==48 and report["aabb_comparisons"]>1000,
            "motion QA incomplete")
    require(report["closed_manifold_positive_volume"]=="PASS",
            "mesh topology QA missing")
    require(report["bolt_track_engagement_and_moving_fixed_aabb"]=="PASS",
            "rail/bolt collision QA missing")
    require(report["unity_import"]=="NOT RUN","false Unity claim")
    result={"id":ID,"status":"PASS","real_blend":True,"real_fbx":True,
            "real_animated_glb":True,"bolt_animation":channels,
            "bolt_excursion_m":round(max(excursions),5),
            "obj_vertices":vertices,"obj_faces":faces,
            "mtl_materials":materials,
            "glb_meshes":len(glb["meshes"]),
            "glb_materials":len(glb["materials"]),
            "unity":"NOT RUN"}
    print(json.dumps(result,indent=2))
    return result


if __name__=="__main__":
    validate(Path(sys.argv[1]) if len(sys.argv)>1 else Path("/tmp/m01-011"))
