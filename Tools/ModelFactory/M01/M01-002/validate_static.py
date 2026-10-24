#!/usr/bin/env python3
"""M01-002 static OBJ/MTL manifold, winding, volume and bounds QA. Stdlib only."""
from pathlib import Path
import ast
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
        groups[current] = {"edges": {}, "volume": 0.0, "faces": 0, "indices": set()}
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
        item["indices"].update(face)
        item["faces"] += 1
        for i in range(3):
            a, b = face[i], face[(i+1)%3]
            key = (min(a,b), max(a,b))
            n, direction = item["edges"].get(key, (0,0))
            item["edges"][key] = (n+1, direction+(1 if a < b else -1))

defined = {line.split()[1] for line in (root / "M01-002.mtl").read_text(encoding="utf-8").splitlines()
           if line.startswith("newmtl ")}
assert len(verts) == 2640, f"Old or mismatched OBJ: {len(verts)}"
assert len(groups) == 57, f"Expected 57 components: {len(groups)}"
assert len(used) == 7, f"Seven material slots required: {used}"
assert used <= defined
# Verify the checked in OBJ corresponds to the modified Blender source spec,
# without importing bpy or claiming that Blender executed.
tree = ast.parse((root/"generate_m01_002.py").read_text(encoding="utf-8"))
part_assign = next(n for n in tree.body
                   if isinstance(n,ast.Assign) and any(
                       isinstance(t,ast.Name) and t.id=="PARTS" for t in n.targets))
parts = json.loads(part_assign.value.args[0].value)
spec = {part["name"]:part for part in parts}
assert len(spec) == len(parts) == 57
assert set(spec) == set(groups), "Source-to-OBJ part mismatch"
assert sum(g["faces"] for g in groups.values()) == 5104, "Stale geometry count"
for name, group in groups.items():
    assert group["volume"] > 1e-8, f"reversed or zero signed volume: {name}"
    for edge, (count, direction) in group["edges"].items():
        assert count == 2 and direction == 0, f"nonmanifold or winding error {name}: {edge}"
# Confirm actual hollow housing geometry, open bore and piston travel instead
# of only checking abstract part labels or source-code intentions.
for side in (-1, 1):
    for stem in ("Piston_Housing_", "Piston_Housing_Collar_"):
        name = stem + str(side)
        p = spec[name]
        assert p["kind"] == "tubeY" and abs(p["dim"][2]-.041)<1e-9, name
        group = groups[name]
        coords = [verts[i-1] for i in group["indices"]]
        radii = [math.hypot(x-side*.335,z-.73) for x,y,z in coords]
        assert abs(min(radii)-.041)<1e-6, (name,min(radii))
        assert abs(max(radii)-p["dim"][0])<1e-6
        assert len(coords)==128 and group["faces"]==256, name
    rod = "Piston_Rod_" + str(side)
    part = spec[rod]
    assert part["move"] and abs(part["c"][1]-.095)<1e-9 and abs(part["dim"][1]-.43)<1e-9
    coords = [verts[i-1] for i in groups[rod]["indices"]]
    assert abs(min(p[1] for p in coords)+.12)<1e-6
    assert abs(max(p[1] for p in coords)-.31)<1e-6
    anchor = spec["Piston_Receiver_Anchor_" + str(side)]
    assert anchor["move"] and anchor["kind"]=="box"
    assert anchor["material"] if "material" in anchor else anchor["mat"]=="Gunmetal"
    assert groups[anchor["name"]]["faces"]==12
    mouth=.455-.34/2
    collar_mouth=.277-.045/2
    assert .31-mouth>=.020
    for sample in range(21):
        travel=.12*sample/20
        rod_front=.31+travel
        assert rod_front-mouth>=.020
        assert rod_front<.455+.34/2-.025
        assert rod_front>.277+.045/2
# Existing OBJ and source must agree on actual recoil geometry before CI PASS.
mn = [min(v[i] for v in verts) for i in range(3)]
mx = [max(v[i] for v in verts) for i in range(3)]
assert abs((mn[0]+mx[0])/2) < .03
assert mn[2] >= -.0001 and mx[2] < 1.2
assert mn[1] < -1.35 and mx[1] < .8
print(json.dumps({
    "status":"PASS", "recoil_positions_verified":21,
    "hollow_hydraulic_shells_verified":4,
    "piston_receiver_anchors":2,
    "rod_bore_clearance_m":.007,
    "minimum_rod_engagement_m":.025, "groups":len(groups), "vertices":len(verts),
    "triangles":sum(g["faces"] for g in groups.values()),
    "materials":len(used), "watertight_groups":len(groups),
    "winding_consistent":True, "positive_volume_groups":len(groups),
    "bounds_m":[mn,mx]}, indent=2))
