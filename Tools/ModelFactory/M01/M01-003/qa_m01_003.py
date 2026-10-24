#!/usr/bin/env python3
"""Independent, stdlib-only QA of generated M01-003 OBJ/MTL and manifest."""
import json
import math
import subprocess
import sys
import tempfile
from pathlib import Path

HERE=Path(__file__).resolve().parent
GEN=HERE/"generate_m01_003.py"
with tempfile.TemporaryDirectory() as temp:
    out=Path(temp)
    subprocess.run([sys.executable,"-m","py_compile",str(GEN)],check=True)
    subprocess.run([sys.executable,str(GEN),"--self-test"],check=True)
    subprocess.run([sys.executable,str(GEN),"--out",str(out)],check=True)
    manifest=json.loads((out/"M01-003_manifest.json").read_text())
    obj=(out/"M01-003_dual_arc_emitter.obj").read_text()
    mtl=(out/"M01-003_dual_arc_emitter.mtl").read_text()
    lines=obj.splitlines()
    vs=[tuple(map(float,line.split()[1:])) for line in lines if line.startswith("v ")]
    faces=[tuple(map(int,line.split()[1:])) for line in lines if line.startswith("f ")]
    objects=[line[2:] for line in lines if line.startswith("o ")]
    mats={line.split()[1] for line in mtl.splitlines() if line.startswith("newmtl ")}
    used={line.split()[1] for line in lines if line.startswith("usemtl ")}
    assert len(vs)==manifest["vertices"],(len(vs),manifest)
    assert len(faces)==manifest["triangles"],(len(faces),manifest)
    assert len(objects)==manifest["parts"],(len(objects),manifest)
    assert len(mats)==manifest["materials"]>=3
    assert mats==used
    assert len(set(objects))==len(objects)
    assert {"Shield_L","Shield_R","Core_L","Core_R"}.issubset(objects)
    assert manifest["watertight_per_part"]=="PASS"
    assert manifest["extreme_frame_clearance"]=="PASS (conservative AABB)"
    assert manifest["shield_fixed_chassis_clearance_m"]>=0.019
    assert manifest["shield_travel_m"]==0.115
    assert all(all(math.isfinite(v) for v in xyz) for xyz in vs)
    assert all(len(face)==3 and all(1<=i<=len(vs) for i in face)
               for face in faces)
    assert min(y for x,y,z in vs)>=-1e-8
    assert manifest["backend"]=="plain-python-static-obj"
    assert manifest["unity_import"]=="NOT RUN"
    print(json.dumps({"result":"PASS","parts":len(objects),
                      "vertices":len(vs),"triangles":len(faces),
                      "materials":len(mats),"actual_static_obj":True,
                      "blender":"NOT RUN","unity":"NOT RUN"},indent=2))
