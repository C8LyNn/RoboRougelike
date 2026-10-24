#!/usr/bin/env python3
"""Independent deterministic source/asset QA for M04-003. No Blender required."""
import hashlib
import json
from pathlib import Path
import subprocess
import sys
import tempfile

HERE = Path(__file__).resolve().parent
GEN = HERE / "generate_m04_003.py"


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def run():
    subprocess.run([sys.executable, "-m", "py_compile", str(GEN)], check=True)
    subprocess.run([sys.executable, str(GEN), "--self-test"], check=True)
    with tempfile.TemporaryDirectory() as d:
        out = Path(d)
        cmd = [sys.executable, str(GEN), "--out", str(out)]
        subprocess.run(cmd, check=True)
        m = json.loads((out / "M04-003_manifest.json").read_text())
        assert m["id"] == "M04-003" and m["qa"] == "PASS"
        assert m["parts"] == 51 and m["materials"] == 7
        assert m["rotating_parts"] == 9
        assert m["radial_duct_clearance_m"] >= 0.044
        assert m["radial_shaft_clearance_m"] >= 0.015
        assert m["lower_grille_axial_clearance_m"] >= 0.095
        obj = out / "M04-003_lower_ducted_fan.obj"
        mtl = out / "M04-003_lower_ducted_fan.mtl"
        assert obj.exists() and mtl.exists()
        lines = obj.read_text(encoding="utf-8").splitlines()
        assert sum(s.startswith("v ") for s in lines) == m["vertices"]
        assert sum(s.startswith("f ") for s in lines) == m["triangles"]
        assert sum(s.startswith("o ") for s in lines) == m["parts"]
        materials = {s.split()[1] for s in mtl.read_text().splitlines()
                     if s.startswith("newmtl ")}
        assert len(materials) == m["materials"]
        assert {s.split()[1] for s in lines if s.startswith("usemtl ")} == materials
        before = {p.name: sha(p) for p in out.glob("M04-003*")
                  if p.suffix in (".obj", ".mtl", ".glb")}
        subprocess.run(cmd, check=True)
        after = {p.name: sha(p) for p in out.glob("M04-003*")
                 if p.suffix in (".obj", ".mtl", ".glb")}
        assert before == after, (before, after)
        glb = out / "M04-003_lower_ducted_fan_static.glb"
        if glb.exists():
            import trimesh
            scene = trimesh.load(glb, force="scene")
            assert len(scene.geometry) == m["parts"]
            assert sum(len(g.faces) for g in scene.geometry.values()) == m["triangles"]
            assert scene.bounds[1][1] <= 0
        print(json.dumps({"asset": "M04-003", "qa": "PASS",
                          "obj_mtl": "PASS", "glb": "PASS" if glb.exists() else "NOT RUN",
                          "repeatability": "PASS", "sha256": before,
                          "blender": "NOT RUN", "unity": "NOT RUN"}, indent=2))


if __name__ == "__main__":
    run()
