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
    assert report["structural_revision"] == "M03-003_V2_VISIBLE_SNAP_MARKERS"
    assert report["exposed_snap_markers"] == 6
    assert report["physical_snap_contact_faces_flush"] == 6
    assert abs(report["marker_side_exposure_m"]-.006) < 1e-9
    assert abs(report["marker_pad_engagement_m"]-.002) < 1e-9
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
        object_vertices = {}
        current_object = None
        used_materials = set()
        for line in obj.splitlines():
            if line.startswith("v "):
                xyz = tuple(map(float, line.split()[1:]))
                assert len(xyz) == 3 and all(math.isfinite(x) for x in xyz)
                vertices.append(xyz)
                assert current_object is not None
                object_vertices[current_object].append(xyz)
            elif line.startswith("f "):
                indices = tuple(int(x) for x in line.split()[1:])
                assert len(indices) == 3
                triangles.append(indices)
            elif line.startswith("o "):
                current_object = line[2:]
                objects.append(current_object)
                object_vertices[current_object] = []
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
        # Independently test actual exported geometry, not just the source
        # manifest's claimed mark locations.
        def bounds(name):
            coords=object_vertices[name]
            assert len(coords)==8,name
            return [(min(v[i] for v in coords),max(v[i] for v in coords))
                    for i in range(3)]
        for port, side_axis, mate_axis, limit in (
                ("End",1,0,1.0),("Side",0,1,0.5),("Vertical",0,2,0.5)):
            for side in (-1,1):
                pad=bounds(f"{port}SocketPad_{side:+d}")
                mark=bounds(f"{port}SocketMark_{side:+d}")
                assert abs(mark[side_axis][1]-pad[side_axis][1]-.006)<1e-7
                assert abs(pad[side_axis][1]-mark[side_axis][0]-.002)<1e-7
                assert all(min(pad[a][1],mark[a][1]) -
                           max(pad[a][0],mark[a][0])>.001
                           for a in range(3))
                contact=pad[mate_axis][1] if side>0 else pad[mate_axis][0]
                assert abs(contact-side*limit)<1e-7
                assert all(mark[a][0]>=-lim-1e-7 and
                           mark[a][1]<=lim+1e-7
                           for a,lim in enumerate((1.0,.5,.5)))
        print(json.dumps({
            "asset": "M03-003", "status": "PASS",
            "parts": len(objects), "triangles": len(triangles),
            "vertices": len(vertices), "materials": len(declared_materials),
            "size_m": [2,1,1], "snap_points": 6,
            "visible_snap_markers": 6, "flush_contact_pads": 6,
            "blender": "NOT RUN", "unity": "NOT RUN"
        }, indent=2))


if __name__ == "__main__":
    run()
