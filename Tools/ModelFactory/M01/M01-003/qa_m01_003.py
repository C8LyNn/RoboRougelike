#!/usr/bin/env python3
"""Independent, stdlib-only QA of generated M01-003 OBJ/MTL and manifest."""
import importlib.util
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
    assert manifest["geometry_revision"]=="M01-003_V2_CONNECTED_SHIELD_SLIDES"
    assert manifest["track_attached_to_chassis"]==2
    assert manifest["moving_rail_shoes"]==2
    assert manifest["shield_track_poses_verified"]==42
    assert manifest["moving_groups"]=={"L":4,"R":4}
    assert manifest["parts"]==37
    assert manifest["vertices"]==1670
    assert manifest["triangles"]==3232
    assert manifest["backend"]=="plain-python-static-obj"
    assert manifest["unity_import"]=="NOT RUN"
    # Independently read the actual static export (not only manifest claims).
    # Check moving shoe contact through the entire outward 115 mm travel.
    by_name={}
    current=None
    global_idx=0
    for line in lines:
        if line.startswith("o "):
            current=line[2:]
            assert current not in by_name
            by_name[current]=[]
        elif line.startswith("v "):
            by_name[current].append(tuple(map(float,line.split()[1:])))
            global_idx+=1
    assert len(by_name)==37
    def bounding(name):
        v=by_name[name]
        assert v and len(v)==18, (name,len(v))
        return [(min(p[axis] for p in v),max(p[axis] for p in v))
                for axis in range(3)]
    chassis=bounding("Emitter_Chassis")
    verified=0
    for side,label in ((-1,"L"),(1,"R")):
        rail=bounding("Shield_Guide_"+label)
        shoe=bounding("Shield_Slider_Shoe_"+label)
        shield=bounding("Shield_"+label)
        overlap=[min(chassis[k][1],rail[k][1])-
                 max(chassis[k][0],rail[k][0]) for k in range(3)]
        assert min(overlap)>=.0149, (label,"unanchored rail",overlap)
        assert abs(rail[1][1]-shoe[1][0])<1e-7,(label,"rail/shoe gap")
        assert shoe[1][1]-shield[1][0]>=.0029
        assert shoe[2][0]>=shield[2][0] and shoe[2][1]<=shield[2][1]
        for position in range(21):
            delta=side*.115*position/20
            assert shoe[0][0]+delta>=rail[0][0]+.001
            assert shoe[0][1]+delta<=rail[0][1]-.001
            verified+=1
    assert verified==42
    # Every generated OBJ mesh also exactly matches its corresponding source
    # vertices, so CI cannot PASS while a saved preview is out of date.
    spec=importlib.util.spec_from_file_location(
        "m01_003_source",HERE/"generate_m01_003.py")
    module=importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    for part in module.build():
        actual=by_name[part["name"]]
        assert len(actual)==len(part["vertices"])
        for a,b in zip(actual,part["vertices"]):
            assert max(abs(a[i]-b[i]) for i in range(3))<5.1e-9
    print(json.dumps({"result":"PASS","parts":len(objects),
                      "vertices":len(vs),"triangles":len(faces),
                      "materials":len(mats),"actual_static_obj":True,
                      "anchored_guides":2,"sliding_shoes":2,
                      "continuous_travel_positions":verified,
                      "blender":"NOT RUN","unity":"NOT RUN"},indent=2))
