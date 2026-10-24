#!/usr/bin/env python3
"""M01-002 static OBJ/MTL manifold, winding, volume and bounds QA. Stdlib only."""
from pathlib import Path
import json
import math

root = Path(__file__).resolve().parent
verts, groups, used = [], {}, set()
current = None
for line in (root / "M01-002.obj").read_text(encoding="utf-8").splitlines():
    tokens = line.split()
    if not tokens:
        continue
    tag = tokens[0]
    if tag == "v":
        xyz = tuple(map(float, tokens[1:]))
        assert len(xyz) == 3 and all(math.isfinite(x) for x in xyz)
        verts.append(xyz)
    elif tag == "o":
        current = tokens[1]
        assert current not in groups
        groups[current] = {"edges": {}, "volume": 0.0, "faces": 0}
    elif tag == "usemtl":
        used.add(tokens[1])
    elif tag == "f":
        face = [int(x.split("/")[0]) for x in tokens[1:]]
        assert len(face) == 3 and len(set(face)) == 3
        assert all(1 <= i <= len(verts) for i in face)
        assert current is not None
        p, q, r = [verts[i-1] for i in face]
        ab = [q[i]-p[i] for i in range(3)]
        ac = [r[i]-p[i] for i in range(3)]
        cross = (
            ab[1]*ac[2]-ab[2]*ac[1],
            ab[2]*ac[0]-ab[0]*ac[2],
            ab[0]*ac[1]-ab[1]*ac[0]
        )
        assert math.sqrt(sum(x*x for x in cross)) > 1e-8, "degenerate triangle"
        item = groups[current]
        item["volume"] += sum(p[i]*cross[i] for i in range(3))/6.0
        item["faces"] += 1
        for i in range(3):
            a, b = face[i], face[(i+1)%3]
            key = (min(a,b), max(a,b))
            n, direction = item["edges"].get(key, (0,0))
            item["edges"][key] = (n+1, direction+(1 if a < b else -1))

defined = {line.split()[1] for line in (root / "M01-002.mtl").read_text(encoding="utf-8").splitlines()
           if line.startswith("newmtl ")}
assert len(verts) > 1000 and len(groups) >= 45 and len(used) >= 3
assert used <= defined
for name, group in groups.items():
    assert group["volume"] > 1e-8, f"reversed or zero signed volume: {name}"
    for edge, (count, direction) in group["edges"].items():
        assert count == 2 and direction == 0, f"nonmanifold or winding error {name}: {edge}"
mn = [min(v[i] for v in verts) for i in range(3)]
mx = [max(v[i] for v in verts) for i in range(3)]
assert abs((mn[0]+mx[0])/2) < .03
assert mn[2] >= -.0001 and mx[2] < 1.2
assert mn[1] < -1.35 and mx[1] < .8
print(json.dumps({
    "status":"PASS", "groups":len(groups), "vertices":len(verts),
    "triangles":sum(g["faces"] for g in groups.values()),
    "materials":len(used), "watertight_groups":len(groups),
    "winding_consistent":True, "positive_volume_groups":len(groups),
    "bounds_m":[mn,mx]}, indent=2))
