#!/usr/bin/env python3
"""M05-013 independent stdlib QA: actual OBJ/MTL, hierarchy, kinematics."""
import importlib.util
import json
import math
import subprocess
import sys
import tempfile
from pathlib import Path

HERE=Path(__file__).resolve().parent
GEN=HERE/"generate_m05_013.py"
with tempfile.TemporaryDirectory() as tmp:
    out=Path(tmp)
    subprocess.run([sys.executable,"-m","py_compile",str(GEN)],check=True)
    subprocess.run([sys.executable,str(GEN),"--self-test"],
                   check=True,stdout=subprocess.PIPE,text=True)
    subprocess.run([sys.executable,str(GEN),"--out",str(out)],
                   check=True,stdout=subprocess.PIPE,text=True)
    report=json.loads((out/"M05-013_manifest.json").read_text())
    assert report["asset_id"]=="M05-013"
    assert report["mesh_manifold_oriented"]=="PASS"
    assert report["kinematic_samples"]==361
    assert report["min_link_base_clearance_m"]>.03
    assert report["min_nonadjacent_centerline_distance_m"]>.13
    assert report["material_names"] and len(report["material_names"])==8
    assert report["joint_pivots_m"]=={
        "shoulder":[.27,0,0],"elbow":[.65,0,0],"wrist":[.99,0,0]}
    assert report["animation"]["frame_end"]==36
    assert len(report["animation"]["keys"])==5
    assert report["unity_import"]=="NOT RUN"
    assert report["visual_review"]=="NOT RUN"
    assert report["backend"]=="plain-python-OBJ"
    obj=(out/"M05-013_repair_manipulator.obj").read_text()
    mtl=(out/"M05-013_repair_manipulator.mtl").read_text()
    groups={}
    current=None
    materials=set()
    faces=[]
    for line in obj.splitlines():
        if line.startswith("o "):
            current=line[2:]
            assert current not in groups
            groups[current]=[]
        elif line.startswith("v "):
            assert current is not None
            xyz=tuple(map(float,line.split()[1:]))
            assert len(xyz)==3 and all(map(math.isfinite,xyz))
            groups[current].append(xyz)
        elif line.startswith("f "):
            faces.append(tuple(map(int,line.split()[1:])))
        elif line.startswith("usemtl "):
            materials.add(line[7:])
    defined={line[7:] for line in mtl.splitlines()
             if line.startswith("newmtl ")}
    assert defined==materials==set(report["material_names"])
    assert len(groups)==report["parts"]>=40
    assert len(faces)==report["triangles"]
    assert sum(map(len,groups.values()))==report["vertices"]
    assert len(set(groups))==len(groups)
    assert {"Mount_Flush_Plate","Shoulder_Link","Elbow_Link",
            "Wrist_Link","Repair_Nozzle","Nozzle_Cyan_Lip",
            "Inspection_Camera"}.issubset(groups)
    assert all(len(f)==3 and all(1<=idx<=report["vertices"] for idx in f)
               for f in faces)
    assert min(x for v in groups.values() for x,y,z in v)>=-1e-9
    spec=importlib.util.spec_from_file_location("m05_013",GEN)
    mod=importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    parts=mod.build()
    assert len(parts)==len(groups)
    for part in parts:
        saved=groups[part["name"]]
        expected=[mod.addv(v,mod.ORIGINS[part["group"]])
                  for v in part["vertices"]]
        assert len(saved)==len(expected)
        for a,b in zip(saved,expected):
            assert max(abs(x-y) for x,y in zip(a,b))<5.1e-10
    # Independent kinematic checks, including the end effector tip.
    for frame in range(1,37):
        (s,e,w),(a,b,c)=mod.poses(frame)
        tip=mod.plus2(w,mod.rotate_xy((.326,0),c))
        assert tip[0]>.9,(frame,tip)
        assert all(map(math.isfinite,tip))
    print(json.dumps({"result":"PASS","parts":len(groups),
                      "vertices":report["vertices"],
                      "triangles":len(faces),"materials":len(defined),
                      "kinematic_samples":report["kinematic_samples"],
                      "source_obj_vertex_match":"PASS",
                      "blender":"NOT RUN","unity":"NOT RUN"},indent=2))
