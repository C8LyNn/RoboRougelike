#!/usr/bin/env python3
"""Independent GLB binary, Blender report and Python syntax validation (stdlib only)."""
import argparse
import ast
import json
import pathlib
import struct

HERE = pathlib.Path(__file__).resolve().parent

def glb_info(path):
    data=path.read_bytes()
    assert len(data)>32
    magic,version,total=struct.unpack_from("<III",data)
    assert magic==0x46546C67 and version==2 and total==len(data), "invalid GLB"
    length,kind=struct.unpack_from("<II",data,12)
    assert kind==0x4E4F534A
    gltf=json.loads(data[20:20+length].decode("utf-8").rstrip(" \\x00"))
    nodes=[n.get("name","") for n in gltf.get("nodes",[])]
    assert len(gltf.get("materials",[]))>=9
    assert len(gltf.get("meshes",[]))>=30
    assert gltf.get("animations"), "missing flywheel animation"
    for marker in ("MOUNT_ROOT","PIVOT_FLYWHEEL_Y","MOUNT_SHAFT_"):
        assert any(marker in n for n in nodes), "missing "+marker
    return {"meshes":len(gltf["meshes"]),"materials":len(gltf["materials"]),
            "animations":len(gltf["animations"]),"nodes":len(nodes)}

def main():
    p=argparse.ArgumentParser()
    p.add_argument("--out",default=str(HERE/"output"))
    p.add_argument("--require-export",action="store_true")
    args=p.parse_args()
    src=HERE/"generate_m05_022.py"
    ast.parse(src.read_text(encoding="utf-8"),filename=str(src))
    result={"id":"M05-022","python_syntax":"PASS","blender":"NOT RUN","glb":"NOT RUN"}
    output=pathlib.Path(args.out)
    glb=output/"M05-022.glb"
    qa=output/"M05-022_qa.json"
    blend=output/"M05-022.blend"
    if glb.exists() and qa.exists() and blend.exists():
        result.update(glb_info(glb))
        data=json.loads(qa.read_text(encoding="utf-8"))
        assert data["status"]=="BLENDER_QA_PASS"
        assert data["frames_checked"]==61
        assert data["min_rotor_to_guard_radial_clearance_m"]>0.025
        assert data["glb_export"]=="PASS" and data["blend_export"]=="PASS"
        result["blender"]="PASS"
        result["glb"]="PASS"
    elif args.require_export:
        raise SystemExit("Missing Blender/GLB/QA artifacts")
    print(json.dumps(result,indent=2))

if __name__=="__main__":
    main()
