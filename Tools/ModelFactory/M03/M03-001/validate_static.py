#!/usr/bin/env python3
"""Offline static QA for M03-001 OBJ, MTL and Blender source; no bpy required."""
import ast
from collections import Counter, defaultdict
from pathlib import Path

ROOT = Path(__file__).resolve().parent
OBJ = ROOT / "M03-001.obj"
MTL = ROOT / "M03-001.mtl"
GEN = ROOT / "generate_m03_001.py"


def validate():
    ast.parse(GEN.read_text(encoding="utf-8"), filename=str(GEN))
    lines = OBJ.read_text(encoding="utf-8").splitlines()
    verts = [None]  # OBJ vertex indices start at 1.
    faces = defaultdict(list)
    mats = set()
    objects = []
    active = None
    for line in lines:
        if line.startswith("v "):
            xyz = [float(x) for x in line.split()[1:]]
            assert len(xyz) == 3
            verts.append(xyz)
        elif line.startswith("o "):
            active = line[2:].strip()
            assert active not in faces, "Duplicate object: " + active
            objects.append(active)
            faces[active] = []
        elif line.startswith("usemtl "):
            mats.add(line.split(maxsplit=1)[1])
        elif line.startswith("f "):
            assert active, "Face without object"
            ids = [int(x.split("/")[0]) for x in line.split()[1:]]
            assert len(ids) >= 3 and len(set(ids)) == len(ids)
            faces[active].append(ids)
    assert len(objects) == 52, len(objects)
    assert len(verts) - 1 == 1920, len(verts) - 1
    assert sum(len(v) for v in faces.values()) == 1352
    assert sum(n.startswith("NODE_") for n in objects) == 8
    assert sum("_BEAM_" in n for n in objects) == 12
    assert sum(n.startswith("DIAGONAL_") for n in objects) == 4
    assert sum(n.startswith("SOCKET_") for n in objects) == 24
    assert sum(n.startswith("STATUS_") for n in objects) == 4
    assert len(mats) == 5
    declared = {line.split(maxsplit=1)[1] for line in MTL.read_text(
        encoding="utf-8"
    ).splitlines() if line.startswith("newmtl ")}
    assert mats <= declared, mats - declared
    for axis in range(3):
        values = [v[axis] for v in verts[1:]]
        assert abs(min(values) + 0.5) < 0.00001
        assert abs(max(values) - 0.5) < 0.00001
    # Each object must be closed (every undirected edge used exactly twice).
    for name, polys in faces.items():
        edges = Counter()
        for polygon in polys:
            for i, a in enumerate(polygon):
                b = polygon[(i + 1) % len(polygon)]
                assert 0 < a < len(verts) and 0 < b < len(verts)
                edges[tuple(sorted((a, b)))] += 1
        assert edges and set(edges.values()) == {2}, (
            "Non-manifold component", name,
            {key: value for key, value in edges.items() if value != 2}
        )
    print("PASS M03-001 static: 52 watertight components; 1920 vertices;"
          " 1352 faces; 5 materials; bounds exactly 1m; bpy AST valid")


if __name__ == "__main__":
    validate()
