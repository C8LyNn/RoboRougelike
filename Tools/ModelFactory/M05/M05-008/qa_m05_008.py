#!/usr/bin/env python3
"""Independent stdlib QA of actual M05-008 generated OBJ/MTL and motion envelope."""
from __future__ import annotations
import hashlib
import json
import math
import subprocess
import sys
import tempfile
from pathlib import Path

HERE = Path(__file__).resolve().parent
GEN = HERE/"generate_m05_008.py"


def command(*args):
    r = subprocess.run([sys.executable,str(GEN),*args],
                       check=False,capture_output=True,text=True)
    if r.returncode:
        raise RuntimeError("Generator failed: "+r.stderr+"\n"+r.stdout)
    return json.loads(r.stdout)


def run():
    subprocess.run([sys.executable,"-m","py_compile",str(GEN)],
                   check=True,capture_output=True,text=True)
    first = command("--self-test")
    assert first["result"] == "PASS"
    assert first["asset"] == "M05-008"
    assert first["materials"] >= 3
    assert first["hinge_axis"] == "X"
    assert first["hinge_open_degrees"] == -70
    assert first["sampled_hatch_poses"] == 71
    assert first["min_front_separation_m"] > .004
    assert first["mount_pivot_m"] == [0,0,0]
    assert len(first["sockets"]) == 3
    assert first["socket_outward_axes"] == {
        "MOUNT_BOTTOM":[0.0,0.0,-1.0],
        "SERVICE_FRONT":[0.0,-1.0,0.0],
        "POWER_REAR":[0.0,1.0,0.0],
    }
    with tempfile.TemporaryDirectory() as td:
        folder = Path(td)
        a,b = folder/"a",folder/"b"
        ra = command("--static-obj","--out",str(a))
        rb = command("--static-obj","--out",str(b))
        assert ra["result"] == rb["result"] == "PASS"
        assert ra["backend"] == rb["backend"] == "python-stdlib"
        assert ra["blender"] == rb["blender"] == "NOT RUN"
        for name in ("M05-008_battery.obj","M05-008_battery.mtl",
                     "M05-008_manifest.json"):
            aa=(a/name).read_bytes()
            bb=(b/name).read_bytes()
            assert hashlib.sha256(aa).digest()==hashlib.sha256(bb).digest(),name
        obj=(a/ra["obj"]).read_text(encoding="utf-8")
        mtl=(a/ra["mtl"]).read_text(encoding="utf-8")
        names=[]
        vertices=[]
        faces=[]
        usemtl=set()
        for line in obj.splitlines():
            if line.startswith("o "):
                names.append(line[2:])
            elif line.startswith("v "):
                v=tuple(map(float,line.split()[1:]))
                assert len(v)==3 and all(math.isfinite(x) for x in v)
                vertices.append(v)
            elif line.startswith("f "):
                f=tuple(map(int,line.split()[1:]))
                assert len(f)==3 and len(set(f))==3
                faces.append(f)
            elif line.startswith("usemtl "):
                usemtl.add(line[7:])
        declared={line[7:] for line in mtl.splitlines()
                  if line.startswith("newmtl ")}
        assert len(names)==ra["parts"] and len(set(names))==len(names)
        assert len(vertices)==ra["vertices"]
        assert len(faces)==ra["triangles"]
        assert all(1<=i<=len(vertices) for f in faces for i in f)
        assert usemtl==declared and len(declared)==ra["materials"]
        assert any("HatchPanel" in name for name in names)
        assert sum("PowerCable_" in name for name in names)==12
        assert sum("CoolingBand_" in name for name in names)==6
        assert any("CyanLED" in line for line in mtl.splitlines())
        assert max(v[2] for v in vertices)<=.800
        assert min(v[2] for v in vertices)>=0
        print(json.dumps({
            "asset":"M05-008","result":"PASS","parts":len(names),
            "vertices":len(vertices),"triangles":len(faces),
            "materials":len(declared),"hatch_poses":71,
            "min_clearance_m":first["min_front_separation_m"],
            "sockets":len(first["sockets"]),
            "reproducible_exports":"2/2 MATCH",
            "blender":"NOT RUN","unity":"NOT RUN"},indent=2))


if __name__ == "__main__":
    run()
