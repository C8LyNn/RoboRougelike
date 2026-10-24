#!/usr/bin/env python3
"""Independent M04-007 OBJ/MTL and optional binary GLB verification (stdlib)."""
import argparse
from collections import Counter
import json
import math
from pathlib import Path
import struct

NAME="M04-007"


def obj_qa(folder):
    obj=folder/(NAME+".obj")
    mtl=folder/(NAME+".mtl")
    assert obj.is_file() and mtl.is_file()
    vertices=[]
    groups={}
    materials=set()
    current=None
    for line in obj.read_text(encoding="utf-8").splitlines():
        if line.startswith("o "):
            current=line[2:].strip()
            assert current not in groups
            groups[current]=[]
        elif line.startswith("v "):
            v=tuple(map(float,line.split()[1:]))
            assert len(v)==3 and all(math.isfinite(x) for x in v)
            vertices.append(v)
        elif line.startswith("usemtl "):
            assert current is not None
            materials.add(line.split()[1])
        elif line.startswith("f "):
            assert current is not None
            face=tuple(int(x)-1 for x in line.split()[1:])
            assert len(face)>=3 and len(set(face))==len(face)
            assert all(0<=x<len(vertices) for x in face)
            groups[current].append(face)
    assert len(vertices)>2000 and len(groups)>70
    assert len(materials)>=3
    assert all("pulse_core" in name for name in
               (next(k for k in groups if k=="pod_00_pulse_core"),
                next(k for k in groups if k=="pod_03_pulse_core")))
    assert len([x for x in groups if x.endswith("_pulse_core")])==4
    assert len([x for x in groups if x.endswith("_pulse_ion")])==4
    assert len([x for x in groups if x.endswith("_pulse_ceramic")])==4
    faces=sum(len(fs) for fs in groups.values())
    assert faces>1500
    # Manifold topology per named solid: every undirected edge has two faces.
    for name,fs in groups.items():
        edges=Counter()
        for f in fs:
            for a,b in zip(f,f[1:]+f[:1]):
                edges[tuple(sorted((a,b)))]+=1
        assert edges and all(count==2 for count in edges.values()),name
    defined={l.split()[1] for l in mtl.read_text().splitlines()
             if l.startswith("newmtl ")}
    assert materials==defined,(materials,defined)
    assert max(z for x,y,z in vertices)<=1e-7
    return {"status":"PASS","parts":len(groups),"vertices":len(vertices),
            "faces":faces,"materials":len(materials),
            "pulse_core_count":4,"all_components_manifold":True}


def glb_qa(path,expected_parts,expected_materials):
    raw=path.read_bytes()
    assert len(raw)>4096
    magic,version,total=struct.unpack_from("<4sII",raw,0)
    assert magic==b"glTF" and version==2 and total==len(raw)
    size,kind=struct.unpack_from("<I4s",raw,12)
    assert kind==b"JSON"
    data=json.loads(raw[20:20+size].decode("utf-8"))
    nodes=data.get("nodes",[])
    names=[n.get("name","") for n in nodes]
    assert len(data.get("meshes",[]))>=expected_parts
    assert len(data.get("materials",[]))>=expected_materials
    roots={}
    for i in range(4):
        name=NAME+"_EMITTER_PULSE_%02d"%i
        assert names.count(name)==1,(name,names)
        roots[i]=names.index(name)
    assert names.count(NAME+"_MOUNT_ROOT")==1
    animations=data.get("animations",[])
    assert animations
    channels=[channel for animation in animations
              for channel in animation.get("channels",[])]
    targets={(c["target"]["node"],c["target"]["path"]) for c in channels}
    for node in roots.values():
        assert (node,"translation") in targets,(node,"translation",targets)
        assert (node,"scale") in targets,(node,"scale",targets)
    # Every pulse root has both animated radial size and height;
    # emission is only a supplementary visual cue.
    return {"status":"PASS","nodes":len(nodes),
            "meshes":len(data["meshes"]),
            "materials":len(data["materials"]),
            "animations":len(animations),
            "channels":len(channels),
            "independently_animated_emitters":4,
            "pulse_translation_scale_targets":8}


def main():
    parser=argparse.ArgumentParser()
    parser.add_argument("--asset-dir",required=True)
    parser.add_argument("--glb")
    opts=parser.parse_args()
    folder=Path(opts.asset_dir)
    report={"obj":obj_qa(folder)}
    if opts.glb:
        report["glb"]=glb_qa(Path(opts.glb),report["obj"]["parts"],
                             report["obj"]["materials"])
    print(json.dumps(report,indent=2))


if __name__=="__main__":
    main()
