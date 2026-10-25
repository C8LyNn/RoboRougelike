#!/usr/bin/env python3
"""Independent exported artifact checks for M04-016; no Blender imports."""
import argparse
import json
import math
import struct
from pathlib import Path

BASE="M04-016_corner_microthrusters"

def check_glb(path):
    data=path.read_bytes()
    assert data[:4]==b"glTF" and len(data)>=20, "Missing binary GLB"
    version,length=struct.unpack_from("<II",data,4)
    assert version==2 and length==len(data), "GLB header invalid"
    chunk_len,chunk_type=struct.unpack_from("<I4s",data,12)
    assert chunk_type==b"JSON"
    document=json.loads(data[20:20+chunk_len].decode("utf-8"))
    names={i:n.get("name","") for i,n in enumerate(document.get("nodes",[]))}
    pivots={i for i,name in names.items() if name.startswith("NOZZLE_PIVOT_")}
    assert len(pivots)==4, f"GLB has {len(pivots)} nozzle pivots, expected four"
    assert len(document.get("materials",[]))>=3, "Missing PBR materials"
    assert len(document.get("meshes",[]))>=60, "Missing nozzle geometry"
    animated=set()
    for animation in document.get("animations",[]):
        for channel in animation.get("channels",[]):
            target=channel.get("target",{})
            if target.get("path")=="rotation" and target.get("node") in pivots:
                animated.add(target["node"])
    assert animated==pivots, "All four gimbals must have actual exported rotation animation"
    return {"glb_bytes":len(data),"glb_meshes":len(document["meshes"]),
            "glb_materials":len(document["materials"]),
            "animated_nozzle_pivots":len(animated),
            "animation_clips":len(document.get("animations",[]))}

def check_obj(path):
    verts=[]
    faces=0
    for line in path.read_text(encoding="utf-8",errors="replace").splitlines():
        if line.startswith("v "):
            verts.append(tuple(float(n) for n in line.split()[1:4]))
        elif line.startswith("f "):
            indices=[int(tok.split("/")[0]) for tok in line.split()[1:]]
            assert len(indices)>=3
            assert all(0<j<=len(verts) for j in indices), "Invalid OBJ index"
            faces+=1
    assert len(verts)>1500 and faces>500, "OBJ incomplete"
    assert all(math.isfinite(v) for xyz in verts for v in xyz)
    return {"obj_vertices":len(verts),"obj_faces":faces}

def main():
    ap=argparse.ArgumentParser()
    ap.add_argument("--out",default="build/M04-016")
    out=Path(ap.parse_args().out)
    manifest=json.loads((out/(BASE+"_manifest.json")).read_text(encoding="utf-8"))
    assert manifest["id"]=="M04-016"
    assert manifest["nozzles"]==4
    assert manifest["animated_pivots"]==4
    assert manifest["materials"]>=3
    assert manifest["min_inter_nozzle_analytical_gap_m"]>.015
    assert manifest["min_pedestal_to_nozzle_analytical_gap_m"]>.09
    assert (out/(BASE+".blend")).stat().st_size>20000
    assert (out/(BASE+".mtl")).stat().st_size>200
    result={"id":"M04-016","status":"PASS",**check_glb(out/(BASE+".glb")),
            **check_obj(out/(BASE+".obj"))}
    (out/"M04-016_independent_qa.json").write_text(
        json.dumps(result,indent=2)+"\n",encoding="utf-8")
    print("M04-016 EXPORTED_ARTIFACT_QA_PASS",json.dumps(result))

if __name__=="__main__":
    main()
