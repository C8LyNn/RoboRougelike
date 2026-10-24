#!/usr/bin/env python3
"""Independent M04-006 OBJ/MTL and optional GLB structural validation."""
import argparse
import json
import math
import struct
from pathlib import Path


def check_obj(folder):
    spec=json.loads((folder/"geometry.json").read_text()) if (folder/"geometry.json").exists() else None
    vertices=[]
    faces=[]
    objects=[]
    materials=set()
    current=None
    for line in (folder/"M04-006.obj").read_text().splitlines():
        if line.startswith("v "):
            v=tuple(float(x) for x in line.split()[1:])
            assert len(v)==3 and all(math.isfinite(x) for x in v)
            vertices.append(v)
        elif line.startswith("o "):
            current=line[2:].strip()
            objects.append(current)
        elif line.startswith("usemtl "):
            assert current is not None
            materials.add(line.split()[1])
        elif line.startswith("f "):
            f=[int(i) for i in line.split()[1:]]
            assert len(f)>=3 and len(f)==len(set(f))
            assert all(1<=i<=len(vertices) for i in f)
            faces.append(f)
    assert len(vertices)>300 and len(faces)>200
    assert len(objects)==len(set(objects)) and len(objects)>40
    assert len(materials)>=3
    assert "animated_louver_0" in objects and "animated_louver_4" in objects
    assert "animated_louver_tip_4" in objects
    mtl=(folder/"M04-006.mtl").read_text()
    for material in materials:
        assert "newmtl "+material in mtl,material
    if spec is not None:
        assert set(materials)==set(spec["materials"])
    return dict(objects=len(objects),vertices=len(vertices),faces=len(faces),
                materials=len(materials),status="PASS")


def check_glb(path,expected_objects,expected_materials):
    raw=path.read_bytes()
    assert len(raw)>4096
    magic,version,length=struct.unpack_from("<4sII",raw,0)
    assert magic==b"glTF" and version==2 and length==len(raw)
    size,kind=struct.unpack_from("<I4s",raw,12)
    assert kind==b"JSON"
    gltf=json.loads(raw[20:20+size].decode("utf-8"))
    nodes=gltf.get("nodes",[])
    names=[n.get("name","") for n in nodes]
    assert len(names)>=expected_objects+1
    assert all(n.startswith("M04-006_") for n in names),names
    assert len(gltf.get("meshes",[]))==expected_objects
    assert len(gltf.get("materials",[]))>=expected_materials
    animations=gltf.get("animations",[])
    assert animations
    channels=sum(len(a.get("channels",[])) for a in animations)
    assert channels>=10,channels
    return dict(nodes=len(nodes),meshes=len(gltf["meshes"]),
                materials=len(gltf["materials"]),animation_channels=channels,
                status="PASS")


def main():
    parser=argparse.ArgumentParser()
    parser.add_argument("--asset-dir",required=True)
    parser.add_argument("--glb")
    opts=parser.parse_args()
    folder=Path(opts.asset_dir)
    report=dict(obj=check_obj(folder))
    if opts.glb:
        report["glb"]=check_glb(Path(opts.glb),report["obj"]["objects"],
                                 report["obj"]["materials"])
    print(json.dumps(report,indent=2))


if __name__=="__main__":
    main()
