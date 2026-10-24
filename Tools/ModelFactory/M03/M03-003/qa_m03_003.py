#!/usr/bin/env python3
"""M03-003 independent standard-library QA of source and real OBJ/MTL export."""
from __future__ import annotations

import json
import math
import subprocess
import sys
import tempfile
from pathlib import Path

HERE = Path(__file__).resolve().parent
GENERATOR = HERE / "generate_m03_003.py"


def run():
    subprocess.run([sys.executable, "-m", "py_compile", str(GENERATOR)],
                   check=True, capture_output=True, text=True)
    selftest = subprocess.run(
        [sys.executable, str(GENERATOR), "--self-test"],
        check=True, capture_output=True, text=True)
    report = json.loads(selftest.stdout)
    assert report["result"] == "PASS"
    assert report["size_m"] == [2,1,1]
    assert report["materials"] >= 3
    assert len(report["snap_points"]) == 6
    with tempfile.TemporaryDirectory() as td:
        output = Path(td)
        subprocess.run(
            [sys.executable, str(GENERATOR), "--static-obj",
             "--out", str(output)],
            check=True, capture_output=True, text=True)
        manifest = json.loads((output/"M03-003_manifest.json").read_text())
        assert manifest["backend"] == "python-stdlib"
        assert manifest["result"] == "PASS"
        assert manifest["blender"] == "NOT RUN"
        obj = (output/manifest["obj"]).read_text()
        mtl = (output/manifest["mtl"]).read_text()
        assert "mtllib " + manifest["mtl"] in obj
        vertices = []
        triangles = []
        objects = []
        used_materials = set()
        for line in obj.splitlines():
            if line.startswith("v "):
                xyz = tuple(map(float, line.split()[1:]))
                assert len(xyz) == 3 and all(math.isfinite(x) for x in xyz)
                vertices.append(xyz)
            elif line.startswith("f "):
                indices = tuple(int(x) for x in line.split()[1:])
                assert len(indices) == 3
                triangles.append(indices)
            elif line.startswith("o "):
                objects.append(line[2:])
            elif line.startswith("usemtl "):
                used_materials.add(line[7:])
        declared_materials = {
            line[7:] for line in mtl.splitlines() if line.startswith("newmtl ")}
        assert len(objects) == manifest["parts"]
        assert len(set(objects)) == len(objects)
        assert len(triangles) == manifest["triangles"]
        assert used_materials == declared_materials
        assert len(declared_materials) == manifest["materials"]
        assert all(1 <= i <= len(vertices) for tri in triangles for i in tri)
        assert max(abs(v[0]) for v in vertices) == 1.0
        assert max(abs(v[1]) for v in vertices) == 0.5
        assert max(abs(v[2]) for v in vertices) == 0.5
        assert any("MainGirder_L" in name for name in objects)
        assert any("MainGirder_R" in name for name in objects)
        print(json.dumps({
            "asset": "M03-003", "status": "PASS",
            "parts": len(objects), "triangles": len(triangles),
            "vertices": len(vertices), "materials": len(declared_materials),
            "size_m": [2,1,1], "snap_points": 6,
            "blender": "NOT RUN", "unity": "NOT RUN"
        }, indent=2))


if __name__ == "__main__":
    run()
