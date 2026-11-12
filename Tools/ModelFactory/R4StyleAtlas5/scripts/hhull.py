"""#1239 Hero Benchmark Pass: SILHOUETTE FAIRINGS on top of the owned hull layer of rhull (same rules: cosmetic, no collider, no HP, nothing saved; a
pure function of (occupied cells, part types, style); every fragment has an owner and a `requires` list and leaves with them).
  ramp   fills a STEP: an empty cell that has hull below and a hull cell beside it. A 45 deg armour slope from the edge of the lower deck to the top
         edge of the upper block. OWNER = the lower cube, REQUIRES lower + upper cube. Turns stair-steps into a wedge / glacis.
  cheek  fills a concave corner IN PLAN: an empty cell with hull behind (or in front) and hull to one side. A vertical diagonal armour face - the hull
         gets a pointed prow instead of a notch. OWNER = the first neighbour, REQUIRES both.
  nose   a bumper / prow on the forward face of the lowest hull row (115 mm proud). OWNER = that cube.
Fairings only ever stand in EMPTY cells inside the convex outline of the hull (ramp, cheek) or within 115 mm of a face (nose); mounting a part in the
cell removes the fairing (the layer is recomputed from occupancy). They never stand in or over a wheel cell. Decision for the owner: they are visual
only, so a shot that seems to hit a slope in fact hits the block behind it (at most half a cell deeper)."""
import math, numpy as np
import akit as K, qdefs as QD, rhull as RH
from akit import sbox, lathe, slab, rod, rot, X, Y, Z
H = K.H; G = K.G; HP = G/2; S45 = math.sqrt(0.5); L45 = 0.389
def panel(style, R, A, B, d, var=0):
    """one armour panel of the style in a rotated frame (a across, b up the slope / up, n outwards): a in [-A, A], b in [-B, B], n from d"""
    P = lambda a, b, n: tuple(R @ np.array([a, b, n], float))
    if style == "A":
        th = 0.013; top = d + th; sbox(-A, A, -B, B, d, top, "base", 0.004, R)
        sbox(-A + 0.04, A - 0.04, -B + 0.030, -B + 0.038, top - 0.003, top + 0.0015, "glow", 0.001, R)                   # one thin light line low on the panel
        sbox(-A*0.62, A*0.62, B*0.18, B*0.56, top - 0.004, top + 0.0025, "dark", 0.001, R)                               # flush dark sensor glass
        sbox(-A*0.62, A*0.62, B*0.60, B*0.60 + 0.007, top - 0.003, top + 0.002, "sec", 0.001, R)
        for a in (-A + 0.012, A - 0.022): sbox(a, a + 0.010, -B + 0.06, B - 0.06, top - 0.003, top + 0.002, "sec", 0.001, R)
    elif style == "B":
        th = 0.026; top = d + th; sbox(-A, A, -B, B, d, top, "base", 0.003, R)
        for (a0, a1, b0, b1) in ((-A, A, -B, -B + 0.034), (-A, A, B - 0.034, B), (-A, -A + 0.034, -B, B), (A - 0.034, A, -B, B)): sbox(a0, a1, b0, b1, top - 0.004, top + 0.009, "dark", 0.003, R)
        for a in (-A*0.42, A*0.42): sbox(a - 0.022, a + 0.022, -B + 0.05, B - 0.05, top - 0.002, top + 0.020, "metal", 0.004, R)   # load-bearing ribs
        sbox(-A*0.25, A*0.25, -B*0.30, B*0.30, top - 0.002, top + 0.006, "sec", 0.002, R)                                 # hazard field
        for a in (-A + 0.07, A - 0.07):
            for b in (-B + 0.07, B - 0.07): lathe([(0, 0), (0.016, 0), (0.016, 0.009), (0.01, 0.014), (0, 0.014)], "bolt", 6, crease=1.0, R=R, at=P(a, b, top + 0.008))
    elif style == "C":
        th = 0.018; top = d + th; sbox(-A, A, -B, B, d, top, "base", None, R); sbox(-A, A, -B + 0.06, -B + 0.125, top - 0.005, top + 0.002, "sec", 0.001, R)
    elif style == "D":
        top = d + 0.010; cut = (-0.05, 0.08, 0.02)[var % 3]
        sbox(-A, A*0.92, -B, cut + 0.04, d, top, "rust", 0.001, R); sbox(-A*0.86, A, cut - 0.03, B*0.94, d + 0.008, top + 0.009, "patch" if var % 2 == 0 else "base", 0.001, R)   # two salvaged sheets, overlapped
        sbox(-A*0.5, -A*0.5 + 0.045, -B*0.8, B*0.7, top + 0.007, top + 0.016, "metal", 0.002, R)                          # welded strap
        for (a, b) in ((-A + 0.04, cut), (A - 0.05, cut + 0.01), (0.03, cut - 0.005), (A - 0.05, B - 0.06), (-A + 0.09, -B + 0.05)): lathe([(0, 0), (0.011, 0), (0.008, 0.006), (0, 0.007)], "metal", 5, crease=1.0, R=R, at=P(a, b, top + 0.008))
    else:
        th = 0.034; sbox(-A, A, -B, B, d, d + 0.012, "sec", 0.004, R); sbox(-A + 0.018, A - 0.018, -B + 0.018, B - 0.018, d + 0.008, d + th, "base", 0.02, R)
        sbox(-A*0.45, A*0.45, -B + 0.07, -B + 0.15, d + th - 0.006, d + th + 0.003, "sec", 0.002, R)                    # one bold marking
def ramp(style):
    """canonical: the empty cell at the origin, lower cube under it, upper cube at +Y; the slope falls towards -Y"""
    R = rot(X, 45)
    if style == "D":
        for x in (-0.20, 0.19): rod((x, -0.22, -0.275), (x, 0.26, 0.21), 0.013, "metal", 0.002, 6); rod((x, 0.10, -0.275), (x, 0.10, 0.05), 0.011, "rust", 0.002, 6)     # visible props instead of a closed box
    else: slab([(-0.245, -0.275), (0.29, -0.275), (0.29, 0.245)], -0.236, 0.236, (Y, Z, X), "dark")
    panel(style, R, 0.25, L45 - 0.014, -0.021)
def cheek(style):
    """canonical: the empty cell at the origin, hull at +Y and at +X; the armour face runs diagonally, facing -X -Y"""
    R = np.array([[S45, -S45, 0.0], [0.0, 0.0, 1.0], [-S45, -S45, 0.0]]).T; t = RH.PL[style]["t"]
    slab([(-0.235, 0.29), (0.29, 0.29), (0.29, -0.235)], -H, H - 0.004, "z", "dark"); slab([(-0.262, 0.29), (0.29, 0.29), (0.29, -0.262)], H - 0.004, H + t, "z", "rust" if style == "D" else "base")
    panel(style, R, L45 - 0.014, 0.25, -0.021, 1)
def nose(style, mask):
    """canonical = plate tile frame (face +Z, u = +X, v = +Y = up on a side face). bit 0 / 1: the bumper continues into the +u / -u neighbour"""
    e, w = mask & 1, (mask >> 1) & 1; u0, u1 = (-HP if w else -H + 0.02), (HP if e else H - 0.02); VN = (Y, Z, X)
    if style == "A":
        slab([(-0.24, H), (0.17, H), (-0.03, H + 0.115), (-0.19, H + 0.115)], u0, u1, VN, "base"); sbox(u0, u1, -0.262, -0.246, H, H + 0.15, "dark", 0.003)          # prow + splitter blade
        sbox(u0 + 0.03, u1 - 0.03, -0.118, -0.108, H + 0.112, H + 0.1185, "glow", 0.001); sbox(u0 + 0.03, u1 - 0.03, -0.175, -0.150, H + 0.112, H + 0.118, "dark", 0.001)
    elif style == "B":
        sbox(u0, u1, -0.235, -0.055, H + 0.045, H + 0.115, "dark", 0.006); slab([(-0.02, H), (0.21, H), (0.21, H + 0.022), (0.0, H + 0.075)], u0, u1, VN, "base")       # bull bar + glacis lip
        for a in (-0.15, 0.15): sbox(a - 0.03, a + 0.03, -0.21, -0.08, H, H + 0.05, "metal", 0.004); sbox(a - 0.045, a + 0.045, -0.25, -0.04, H + 0.112, H + 0.124, "metal", 0.003)
        sbox(-0.075, 0.075, -0.20, -0.09, H + 0.112, H + 0.121, "sec", 0.002)
    elif style == "C":
        sbox(u0, u1, -0.235, -0.035, H - 0.002, H + 0.095, "dark"); sbox(-0.11, 0.11, -0.185, -0.085, H + 0.088, H + 0.100, "sec", 0.002)
    elif style == "D":
        rod((u0, -0.20, H + 0.075), (u1, -0.20, H + 0.075), 0.022, "rust", 0.003, 7); rod((u0, -0.075, H + 0.05), (u1, -0.075, H + 0.05), 0.016, "metal", 0.003, 6)     # welded ram bars
        for a in (-0.16, 0.14): rod((a, -0.20, H - 0.002), (a, -0.20, H + 0.075), 0.014, "metal", 0.002, 6)
        for a in (-0.15, 0.02, 0.17): slab([(a - 0.055, H + 0.06), (a + 0.055, H + 0.06), (a + 0.008, H + 0.185)], -0.215, -0.199, (X, Z, Y), "metal")                 # cut-steel teeth
        sbox(-0.21, 0.09, -0.02, 0.17, H + 0.008, H + 0.019, "patch", 0.001, rot(Z, 4))
    else:
        sbox(u0, u1, -0.245, -0.025, H - 0.002, H + 0.11, "dark"); sbox(u0 + 0.05, u1 - 0.05, -0.175, -0.095, H + 0.102, H + 0.116, "sec", 0.004)
        if not (e and w): lathe([(0, 0), (0.052, 0), (0.052, 0.016), (0.03, 0.026), (0, 0.028)], "glow", 14, at=(0.0, 0.10, H + 0.022)); lathe([(0.052, 0), (0.07, 0), (0.07, 0.022), (0.052, 0.022)], "dark", 14, closed=True, at=(0.0, 0.10, H + 0.020))
def kit(style):
    out = {"ramp": lambda: ramp(style), "cheek": lambda: cheek(style)}
    for m in range(4): out["nose_%d" % m] = (lambda m=m: nose(style, m))
    return out
# ------------------------------------------------------------------------------------------------ hero blueprint
def striker():
    """hero blueprint, 32 gameplay parts: low front deck with a single-cube prow, raised rear block, spine running forward. No struts."""
    p = RH._rows(["###", "###", "###", "###", "###", ".#."], 0) + RH._rows(["###", "###", ".#."], 1) + RH._wheels([(3, 1, 0, 1), (3, 4, 0, 1), (-1, 1, 0, -1), (-1, 4, 0, -1)])
    return p + RH._on("Gun", [(1, 1, 2), (1, 4, 1)]) + RH._on("Battery", [(0, 1, 2), (2, 1, 2)]) + RH._on("Deployer", [(1, 0, 2)])
def _Rz(deg): a = math.radians(deg); c, s = math.cos(a), math.sin(a); return np.array([[c, -s, 0.0], [s, c, 0.0], [0.0, 0.0, 1.0]])
def _M(pos, R): M = np.eye(4); M[:3, :3] = R; M[:3, 3] = pos; return M.round(6).tolist()
def fairings(parts, style, alive=None):
    """fairings of the INTACT blueprint, minus those whose parts are gone (a destroyed part never makes new fairings appear before repair)"""
    full = _fairings(parts, style)
    if alive is None: return full
    alive = set(alive); keep = [dict(f) for f in full if all(r in alive for r in f["requires"])]; noses = {tuple(f["cell"]) for f in keep if f["kind"] == "nose"}; du = RH.cdelta(RH.FR["front"][0])
    for f in keep:
        if f["kind"] == "nose": q = tuple(f["cell"]); f["mesh"] = "nose_%d" % (((q[0] + du[0], q[1] + du[1], q[2] + du[2]) in noses) | (((q[0] - du[0], q[1] - du[1], q[2] - du[2]) in noses) << 1))
    return keep
def _fairings(parts, style):
    alive = None
    occ0 = QD.occupancy(parts); c = QD.centre(parts); idx = {p["cell"]: n for n, p in enumerate(parts)}; alive = set(range(len(parts))) if alive is None else set(alive); out = []
    hull = lambda q: occ0.get(q) == "Cube"; add = lambda q, a: (q[0] + a[0], q[1] + a[1], q[2] + a[2])
    cand = {add(q, d) for q in occ0 if hull(q) for d in ((0, 0, 1), (1, 0, 0), (-1, 0, 0), (0, 1, 0), (0, -1, 0))} - set(occ0)
    def put(kind, mesh, q, R, req): 
        if all(r in alive for r in req): out.append(dict(kind=kind, mesh=mesh, owner=req[0], requires=sorted(req), cell=list(q), hero=True, M=_M(QD.world(q, c), R)))
    # canonical ramp: upper cube at world +Y = cell (0, -1, 0). yaw about Z takes +Y to the neighbour direction
    DIRS = (((0, -1, 0), 0), ((0, 1, 0), 180), ((1, 0, 0), -90), ((-1, 0, 0), 90))                    # cell delta of the upper cube, yaw
    ramps = set()
    for q in sorted(cand):
        below = add(q, (0, 0, -1))
        if not hull(below): continue
        for d, yaw in DIRS:
            if hull(add(q, d)): put("ramp", "ramp", q, _Rz(yaw), [idx[below], idx[add(q, d)]]); ramps.add(q); break
    # canonical cheek: hull at world +Y (cell delta (0,-1,0)) and world +X (cell delta (1,0,0))
    CORN = (((0, -1, 0), (1, 0, 0), 0), ((1, 0, 0), (0, 1, 0), -90), ((0, 1, 0), (-1, 0, 0), 180), ((-1, 0, 0), (0, -1, 0), 90))
    for q in sorted(cand - ramps):
        for a, b, yaw in CORN:
            if hull(add(q, a)) and hull(add(q, b)): put("cheek", "cheek", q, _Rz(yaw), [idx[add(q, a)], idx[add(q, b)]]); break
    kmin = min(q[2] for q in occ0 if hull(q)); f = RH.FR["front"]; front = {q for q in occ0 if hull(q) and q[2] == kmin and add(q, (0, 1, 0)) not in occ0 and add(q, (0, 1, 0)) not in {tuple(o["cell"]) for o in out}}
    for q in sorted(front):
        if idx[q] not in alive: continue
        du = RH.cdelta(f[0]); m = (add(q, du) in front and idx[add(q, du)] in alive) | ((add(q, tuple(-x for x in du)) in front and idx[add(q, tuple(-x for x in du))] in alive) << 1)
        out.append(dict(kind="nose", mesh="nose_%d" % m, owner=idx[q], requires=[idx[q]], cell=list(q), hero=True, M=RH._M(QD.world(q, c), f)))
    return out
def fragments(parts, style, alive=None): return RH.fragments(parts, style, alive) + fairings(parts, style, alive)
