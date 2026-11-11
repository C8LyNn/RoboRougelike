"""The five pilot solids: EXACTLY the corner points used by the approved R4Blocks/scripts/build_blocks.py (PR #963 @03af520)."""
G, H = 0.55, 0.26
HT = -0.015
def corners(x0, x1, y0, y1, z0, z1): return [(x, y, z) for x in (x0, x1) for y in (y0, y1) for z in (z0, z1)]
SOLIDS = {
 "Cube": corners(-H, H, -H, H, -H, H),
 "Wedge": corners(-H, H, -H, H, -H, -H) + [(x, H, H) for x in (-H, H)],
 "Tetra": [(-H, H, -H), (H, H, -H), (-H, -H, -H), (-H, H, H)],
 "CornerWedge": corners(-H, H, -H, H, -H, -H) + [(-H, H, H)],
 "HalfBlock": corners(-H, H, -H, H, -H, HT),
}
PILOT = ["Cube", "Wedge", "Tetra", "CornerWedge", "HalfBlock"]
