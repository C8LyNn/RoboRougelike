#!/usr/bin/env python3
"""Independent M04-013 QA: actual OBJ/MTL, topology, geometry, determinism,
motion envelopes, and optional animated Blender GLB structural validation."""
import argparse
import hashlib
import importlib.util
import json
import math
import struct
import subprocess
import sys
import tempfile
from pathlib import Path

HERE=Path(__file__).resolve().parent
GEN=HERE/"generate_m04_013.py"
STEM="M04-013_ringed_water_propeller"

def read_obj(path):
    groups={}
    verts=[]
    faces=[]
    material_for={}
    name=None
    for line in path.read_text(encoding="utf-8").splitlines():
        if line.startswith("o "):
            name=line[2:]
            assert name not in groups,("duplicate OBJ group",name)
            groups[name]=[]
        elif line.startswith("usemtl "):
            assert name is not None
            material_for[name]=line.split(maxsplit=1)[1]
        elif line.startswith("v "):
            assert name is not None
            v=tuple(map(float,line.split()[1:]))
            assert len(v)==3 and all(math.isfinite(q) for q in v)
            verts.append(v)
            groups[name].append(v)
        elif line.startswith("f "):
            f=tuple(map(int,line.split()[1:]))
            assert len(f)==3 and all(1<=q<=len(verts) for q in f)
            faces.append(f)
    return groups,verts,faces,material_for

def glb_check(path):
    raw=path.read_bytes()
    magic,version,length=struct.unpack_from("<III",raw,0)
    assert magic==0x46546C67 and version==2 and length==len(raw)
    off=12
    chunklen,chunktyp=struct.unpack_from("<II",raw,off)
    assert chunktyp==0x4E4F534A
    document=json.loads(raw[off+8:off+8+chunklen].decode("utf-8"))
    meshes=document.get("meshes",[])
    mats=document.get("materials",[])
    animations=document.get("animations",[])
    nodes=document.get("nodes",[])
    assert len(meshes)>=47,(len(meshes),"missing meshes")
    assert len(mats)>=7,(len(mats),"missing materials")
    assert animations,"missing animation"
    assert any("ROTOR_Z" in n.get("name","") for n in nodes)
    rotations=[]
    for anim in animations:
        for channel in anim.get("channels",[]):
            if channel.get("target",{}).get("path")=="rotation":
                node=channel["target"]["node"]
                rotations.append(nodes[node].get("name",""))
    assert any("ROTOR_Z" in name for name in rotations),rotations
    return {"glb":"PASS","meshes":len(meshes),"materials":len(mats),
            "animations":len(animations),"rotating_nodes":rotations}

def run():
    parser=argparse.ArgumentParser()
    parser.add_argument("--glb",type=Path)
    args=parser.parse_args()
    subprocess.run([sys.executable,"-m","py_compile",str(GEN)],check=True)
    spec=importlib.util.spec_from_file_location("m04_013_generator",GEN)
    mod=importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    pieces=mod.build()
    report=mod.validate(pieces)
    assert report["asset_id"]=="M04-013"
    assert report["moving_meshes"]==9
    assert report["blade_count"]==5
    assert report["radial_clearance_m"]>=.018
    assert report["grille_axial_clearance_m"]>=.070
    assert report["motion_vertex_checks"]>=60000
    assert report["mount"].startswith("MOUNT_REAR_SOCKET")
    assert report["rotor_pivot"]==[0,0,.65]
    assert report["unity_import"]=="NOT RUN"
    with tempfile.TemporaryDirectory() as tmp:
        out1=Path(tmp)/"first";out2=Path(tmp)/"second"
        for out in (out1,out2):
            subprocess.run([sys.executable,str(GEN),"--out",str(out)],
                           check=True,capture_output=True,text=True)
        for filename in (STEM+".obj",STEM+".mtl","M04-013_manifest.json"):
            a=(out1/filename).read_bytes()
            b=(out2/filename).read_bytes()
            assert hashlib.sha256(a).digest()==hashlib.sha256(b).digest(),filename
        manifest=json.loads((out1/"M04-013_manifest.json").read_text())
        assert manifest["backend"]=="plain-python-static-obj"
        assert manifest["export_blender"]=="NOT RUN"
        assert manifest["unity_import"]=="NOT RUN"
        groups,verts,faces,used=read_obj(out1/(STEM+".obj"))
        mats={line.split()[1] for line in
              (out1/(STEM+".mtl")).read_text().splitlines()
              if line.startswith("newmtl ")}
        assert len(groups)==report["parts"]==manifest["parts"]
        assert len(verts)==report["vertices"]==manifest["vertices"]
        assert len(faces)==report["triangles"]==manifest["triangles"]
        assert len(mats)==report["materials"]==manifest["materials"]
        assert set(used.values())==mats
        assert set(groups)==set(p["name"] for p in pieces)
        for piece in pieces:
            actual=groups[piece["name"]]
            assert len(actual)==len(piece["vertices"])
            for a,b in zip(actual,piece["vertices"]):
                assert max(abs(a[k]-b[k]) for k in range(3))<5.1e-9
        # Independent check from exported OBJ coordinates: every blade's
        # outer radius stays inside the duct for all rotation angles.
        rotor_names=[p["name"] for p in pieces if p["moving"]]
        assert len(rotor_names)==9
        max_r=max(math.hypot(x,y) for name in rotor_names
                  for x,y,z in groups[name])
        assert .315-max_r>.018
        grille_min=min(z for v in groups["Grille_Rear_Ring"] for z in [v[2]])
        rotor_max_z=max(z for name in rotor_names
                        for x,y,z in groups[name])
        assert grille_min-rotor_max_z>.070
        # The rear-facing grille is after the propeller, not in the swept
        # blade plane. The propeller remains visible through its open ring.
        assert max(v[2] for v in groups["Rotor_Spinner"])<grille_min
        assert len([n for n in groups if n.startswith("Grille_Radial_Spoke_")])==8
        assert len([n for n in groups if n.startswith("Rotor_Twisted_Blade_")])==5
        result={"result":"PASS","id":"M04-013","parts":len(groups),
                "vertices":len(verts),"triangles":len(faces),
                "materials":len(mats),"rotor_meshes":len(rotor_names),
                "radial_clearance_m":round(.315-max_r,5),
                "axial_grille_clearance_m":round(grille_min-rotor_max_z,5),
                "motion_vertex_checks":report["motion_vertex_checks"],
                "deterministic_OBJ_MTL_manifest":True,
                "blender":"NOT RUN (static QA process)",
                "unity":"NOT RUN"}
    if args.glb:
        result.update(glb_check(args.glb))
    print(json.dumps(result,indent=2))
if __name__=="__main__":
    run()
