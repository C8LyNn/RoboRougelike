#!/usr/bin/env python3
"""Independent stdlib-only asset checks for the M04-010 centrifugal stabilizer.

Runs actual source geometry, reconstructs real OBJ twice and verifies parity,
inspects all exported faces/coordinates, and optionally checks Blender GLB.
No Blender or Unity needed for static checks.
"""
from __future__ import annotations

import argparse
from collections import Counter
import hashlib
import importlib.util
import json
import math
from pathlib import Path
import struct
import subprocess
import sys
import tempfile

HERE=Path(__file__).resolve().parent
GEN=HERE/"generate_m04_010.py"
STEM="M04-010_centrifugal_stabilizer"

def source():
    spec=importlib.util.spec_from_file_location("rrl_m04_010",GEN)
    mod=importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    return mod

def obj_parts(path):
    verts=[]
    meshes={}
    current=None
    material=None
    for line in path.read_text(encoding="utf-8").splitlines():
        if line.startswith("o "):
            current=line[2:]
            assert current not in meshes
            meshes[current]={"verts":[],"faces":[],"material":None}
        elif line.startswith("usemtl "):
            assert current
            material=line[7:]
            meshes[current]["material"]=material
        elif line.startswith("v "):
            assert current
            v=tuple(map(float,line.split()[1:]))
            assert len(v)==3 and all(math.isfinite(x) for x in v)
            verts.append(v)
            meshes[current]["verts"].append(len(verts)-1)
        elif line.startswith("f "):
            assert current
            tri=tuple(int(x)-1 for x in line.split()[1:])
            assert len(tri)==3 and len(set(tri))==3
            assert all(0<=x<len(verts) for x in tri)
            meshes[current]["faces"].append(tri)
    return verts,meshes

def static_check():
    mod=source()
    parts=mod.build()
    report=mod.validate(parts)
    assert report["asset"]=="M04-010" and report["status"]=="PASS"
    assert len(parts)==84, len(parts)
    assert report["moving_groups"]=={"LOW":23,"HIGH":23}
    assert report["materials"]==8
    assert report["checked_fixed_pairs"]==76
    assert report["conservative_min_static_clearance_m"]>.003
    assert report["swept_envelopes"]["LOW"]["z_max_m"]+.02<(
        report["swept_envelopes"]["HIGH"]["z_min_m"])
    with tempfile.TemporaryDirectory(prefix="m04-010-qa-") as tmp:
        p=Path(tmp)
        paths=[]
        for n in (1,2):
            out=p/str(n)
            result=subprocess.run([sys.executable,str(GEN),"--static-obj",
                                   "--out",str(out)],
                                  capture_output=True,text=True)
            assert result.returncode==0,(result.stdout,result.stderr)
            paths.append(out)
        for ext in ("obj","mtl"):
            fn=STEM+"."+ext
            hashes=[hashlib.sha256((d/fn).read_bytes()).hexdigest()
                    for d in paths]
            assert hashes[0]==hashes[1],("non-deterministic",fn,hashes)
        manifest=json.loads((paths[0]/"M04-010_manifest.json").read_text())
        for key in ("parts","vertices","triangles","materials",
                    "moving_groups","swept_envelopes"):
            assert manifest[key]==report[key],key
        verts,meshes=obj_parts(paths[0]/(STEM+".obj"))
        assert len(meshes)==report["parts"]
        assert len(verts)==report["vertices"]
        assert sum(len(x["faces"]) for x in meshes.values())==report["triangles"]
        assert len({m["material"] for m in meshes.values()})==8
        for part in parts:
            m=meshes[part["name"]]
            assert m["material"]==part["material"]
            assert len(m["verts"])==len(part["vertices"])
            assert len(m["faces"])==len(part["faces"])
            for vi,original in zip(m["verts"],part["vertices"]):
                expected=mod.transform(part,original,0)
                assert max(abs(verts[vi][axis]-expected[axis])
                           for axis in range(3))<5.1e-9,part["name"]
            owned=set(m["verts"])
            edges=Counter()
            volume=0.
            for a,b,c in m["faces"]:
                assert a in owned and b in owned and c in owned
                A,B,C=(verts[i] for i in (a,b,c))
                u=[B[k]-A[k] for k in range(3)]
                v=[C[k]-A[k] for k in range(3)]
                normal=(u[1]*v[2]-u[2]*v[1],
                        u[2]*v[0]-u[0]*v[2],
                        u[0]*v[1]-u[1]*v[0])
                assert sum(x*x for x in normal)>1e-17,part["name"]
                volume+=sum(A[k]*(
                    B[(k+1)%3]*C[(k+2)%3]-
                    B[(k+2)%3]*C[(k+1)%3]) for k in range(3))/6
                for a1,b1 in ((a,b),(b,c),(c,a)):
                    edges[tuple(sorted((a1,b1)))]+=1
            assert all(count==2 for count in edges.values()),part["name"]
            assert volume>1e-12,(part["name"],volume)
        print(json.dumps({
            "model":"M04-010","status":"PASS","parts":len(meshes),
            "vertices":len(verts),"triangles":report["triangles"],
            "materials":report["materials"],
            "static_fixed_pair_checks":report["checked_fixed_pairs"],
            "sampled_rotating_vertex_checks":report["sampled_rotating_vertex_checks"],
            "min_conservative_clearance_m":report["conservative_min_static_clearance_m"],
            "obj_mtl_regeneration":"BYTE_IDENTICAL",
            "blender":"NOT RUN","unity":"NOT RUN"},indent=2))

def glb_check(path):
    data=Path(path).read_bytes()
    assert data[:4]==b"glTF" and len(data)>1000
    magic,version,length=struct.unpack_from("<4sII",data,0)
    assert magic==b"glTF" and version==2 and length==len(data)
    json_size,json_tag=struct.unpack_from("<I4s",data,12)
    assert json_tag==b"JSON"
    j=json.loads(data[20:20+json_size].decode("utf-8"))
    bin_pos=20+json_size
    bin_size,bin_tag=struct.unpack_from("<I4s",data,bin_pos)
    assert bin_tag==b"BIN\x00"
    binary=data[bin_pos+8:bin_pos+8+bin_size]
    nodes=j.get("nodes",[])
    meshes=j.get("meshes",[])
    materials=j.get("materials",[])
    animations=j.get("animations",[])
    assert len(meshes)>=84,(len(meshes),"missing exported meshes")
    assert len(materials)>=8
    assert animations, "no animated glTF data"
    spin_targets={}
    for anim in animations:
        for ch in anim.get("channels",[]):
            target=ch["target"]
            if target["path"]!="rotation": continue
            node=nodes[target["node"]]["name"]
            if not (node.endswith("LOW_SPIN_Z") or
                    node.endswith("HIGH_SPIN_Z")): continue
            sampler=anim["samplers"][ch["sampler"]]
            accessor=j["accessors"][sampler["output"]]
            view=j["bufferViews"][accessor["bufferView"]]
            assert accessor["componentType"]==5126
            assert accessor["type"]=="VEC4"
            stride=view.get("byteStride",16)
            offset=view.get("byteOffset",0)+accessor.get("byteOffset",0)
            quats=[struct.unpack_from("<4f",binary,offset+i*stride)
                   for i in range(accessor["count"])]
            assert len(quats)>=5,(node,len(quats))
            assert all(abs(sum(x*x for x in q)-1)<.01 for q in quats)
            # Both rotors must animate; a 360-degree rotation should not
            # silently collapse to a constant quaternion.
            first=quats[0]
            assert max(sum((a-b)**2 for a,b in zip(first,q))
                       for q in quats)>.4,node
            spin_targets[node]=len(quats)
    assert len(spin_targets)==2,spin_targets
    print(json.dumps({"model":"M04-010","status":"PASS",
        "glb":str(path),"meshes":len(meshes),"materials":len(materials),
        "animations":len(animations),"spin_channels":spin_targets,
        "unity":"NOT RUN"},indent=2))

if __name__=="__main__":
    ap=argparse.ArgumentParser()
    ap.add_argument("--check-glb",type=Path)
    args=ap.parse_args()
    if args.check_glb: glb_check(args.check_glb)
    else: static_check()
