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
    assert len(objects) == 66, len(objects)
    assert len(verts) - 1 == 2032, len(verts) - 1
    assert sum(len(v) for v in faces.values()) == 1436
    assert sum(n.startswith("NODE_") for n in objects) == 8
    assert sum("_BEAM_" in n for n in objects) == 12
    assert sum(n.startswith("DIAGONAL_") for n in objects) == 4
    assert sum(n.startswith("SOCKET_") for n in objects) == 24
    assert sum(n.startswith("STATUS_") for n in objects) == 4
    assert sum(n.startswith("MOUNT_PAD_") for n in objects) == 6
    assert sum(n.startswith("MOUNT_RIB_") for n in objects) == 8
    assert len(mats) == 5
    declared = {line.split(maxsplit=1)[1] for line in MTL.read_text(
        encoding="utf-8"
    ).splitlines() if line.startswith("newmtl ")}
    assert mats <= declared, mats - declared
    for axis in range(3):
        values = [v[axis] for v in verts[1:]]
        assert abs(min(values) + 0.5) < 0.00001
        assert abs(max(values) - 0.5) < 0.00001
    # A logical SNAP at +/-0.5 m is useless without an actual contact face.
    # Independently inspect OBJ geometry (not Blender source declarations).
    for label, axis, sign in (("PX", 0, 1), ("NX", 0, -1),
                              ("PY", 1, 1), ("NY", 1, -1),
                              ("PZ", 2, 1), ("NZ", 2, -1)):
        name = "MOUNT_PAD_" + label
        points = [verts[i] for polygon in faces[name] for i in polygon]
        assert len(points) >= 8, name
        along = [p[axis] for p in points]
        assert abs((max if sign > 0 else min)(along) - sign*0.5) < 1e-6, name
        assert abs((min if sign > 0 else max)(along) - sign*0.44) < 1e-6, name
        for transverse in range(3):
            if transverse != axis:
                side = [p[transverse] for p in points]
                assert abs(min(side) + 0.1) < 1e-6, name
                assert abs(max(side) - 0.1) < 1e-6, name
    # Top/bottom pads must be tied to all four structural perimeter sides.
    for label, sign in (("PZ", 1), ("NZ", -1)):
        for direction in ("PX", "NX", "PY", "NY"):
            name = "MOUNT_RIB_" + label + "_" + direction
            points = [verts[i] for polygon in faces[name] for i in polygon]
            assert points, name
            z_values = [p[2] for p in points]
            assert abs(min(z_values) - (sign*0.425 - 0.023)) < 1e-6
            assert abs(max(z_values) - (sign*0.425 + 0.023)) < 1e-6
            axis = 0 if direction.endswith("X") else 1
            coords = [p[axis] for p in points]
            sign_axis = 1 if direction.startswith("P") else -1
            assert abs((max if sign_axis > 0 else min)(coords) -
                       sign_axis*0.425) < 1e-6
            assert abs((min if sign_axis > 0 else max)(coords)) < 1e-6
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
    print("PASS M03-001 static: 66 watertight components; 2032 vertices;"
          " 1436 faces; 6 flush physical SNAP pads; 8 structural support ribs;"
          " 5 materials; bounds exactly 1m; bpy AST valid")


if __name__ == "__main__":
    validate()
