"""#1239 Visual Polish Pass 2: the cosmetic hull layer as an OWNED, per-cell system (proposal for a reversible runtime prototype; no HP, no colliders,
nothing saved). A robot's layer is a pure function of (occupied cells, part types, style):
  plate tiles  one tile per exposed cube face, OWNER = that cube. Tiles of one merged plate share a group id; a 4-bit mask says on which sides the
               tile continues into a neighbour of the same group (bit 0 +u, 1 -u, 2 +v, 3 -v) and selects one of 16 kit meshes - so a 3x2 plate
               looks like one plate but disappears cell by cell.
  gaskets      close the 30 mm seam between two attached parts; OWNER = the lower-index part, REQUIRES both.
  wheel lips   a narrow fender on the hull side of a wheel (never over the tyre); OWNER = the wheel, REQUIRES its support cube.
Destruction (event, local): drop every fragment owned by / requiring a removed part; neighbours in the same plate group clear the mask bit that
pointed at the removed cell (their edge closes). Faces uncovered by the hole get NO new armour until repair - the dark core shows the damage.
Blueprints: four gameplay-valid test robots built only from the eight pilot types."""
import math, zlib, numpy as np
import akit as K, qstyle as QS, qdefs as QD
from akit import sbox, lathe, slab, rod, rot, R_to, X, Y, Z
H = K.H; G = K.G; HP = G/2
PL = {"A": dict(big=(3, 2), m=0.022, t=0.011, c=0.035), "B": dict(big=(2, 2), m=0.030, t=0.018, c=0.014), "C": dict(big=(3, 3), m=0.036, t=0.013, c=0.065),
      "D": dict(big=(2, 1), m=0.020, t=0.009, c=0.0), "E": dict(big=(2, 2), m=0.034, t=0.024, c=0.075)}
VARIANTS = {"A": 1, "B": 1, "C": 1, "D": 3, "E": 1}; DVAR = ["base", "patch", "rust"]
# face frames (world Blender axes): u, v, n. Side faces have v = up, the top face has v = forward
FR = {"right": (-Y, Z, X), "left": (Y, Z, -X), "back": (X, Z, Y), "front": (-X, Z, -Y), "up": (X, -Y, Z)}
def cdelta(v): return (int(round(v[0])), int(round(-v[1])), int(round(v[2])))                    # world unit vector -> cell index delta (j runs forward = -Y)
def _h(*a): return (zlib.crc32(repr(a).encode()) % 1000)/1000.0
def tile(style, mask, var=0):
    """kit mesh of one plate tile in the canonical frame (face = +Z of a cell at the origin, u = +X, v = +Y)"""
    st = PL[style]; e, w, n, s = [(mask >> i) & 1 for i in range(4)]; m = st["m"]; t = st["t"]; c = st["c"]; jm = (lambda k: m + 0.03*_h(mask, k, var)) if style == "D" else (lambda k: m)
    def rect(extra=0.0): return (-HP if w else -(H - jm(1) - extra), HP if e else H - jm(0) - extra, -HP if s else -(H - jm(3) - extra), HP if n else H - jm(2) - extra)
    def poly(r, c):
        u0, u1, v0, v1 = r; p = []
        for (cu, cv, o1, o2, a, b) in ((u0, v0, w, s, (0, c), (c, 0)), (u1, v0, e, s, (-c, 0), (0, c)), (u1, v1, e, n, (0, -c), (-c, 0)), (u0, v1, w, n, (c, 0), (0, -c))):      # counter-clockwise
            p += [(cu + a[0], cv + a[1]), (cu + b[0], cv + b[1])] if (c > 0 and not o1 and not o2) else [(cu, cv)]
        return p
    r = rect(); u0, u1, v0, v1 = r; top = H + t; grp = DVAR[var] if style == "D" else "base"
    if style == "E": slab(poly(r, c), H - 0.003, H + 0.009, "z", "sec"); r2 = rect(0.016); slab(poly(r2, c*0.8), H + 0.006, top, "z", "base")
    else: slab(poly(r, c), H - 0.003, top, "z", grp)
    ua, ub = u0 + (0.03 if not w else 0), u1 - (0.03 if not e else 0); va, vb = v0 + (0.03 if not s else 0), v1 - (0.03 if not n else 0)
    if style == "A":
        if not s: sbox(ua, ub, v0 + 0.014, v0 + 0.021, top - 0.004, top + 0.0012, "glow", 0.001)
        if not n: sbox(ua, ub, v1 - 0.03, v1 - 0.018, top - 0.004, top + 0.0015, "sec", 0.001)
    elif style == "B":
        for (out, a, b_) in ((not s, (u0, u1, v0, v0 + 0.032), None), (not n, (u0, u1, v1 - 0.032, v1), None), (not w, (u0, u0 + 0.032, v0, v1), None), (not e, (u1 - 0.032, u1, v0, v1), None)):
            if out: sbox(a[0], a[1], a[2], a[3], top - 0.004, top + 0.008, "dark", 0.003)
        for (cu, cv, o1, o2) in ((u0, v0, w, s), (u1, v0, e, s), (u1, v1, e, n), (u0, v1, w, n)):
            if not o1 and not o2: lathe([(0, 0), (0.016, 0), (0.016, 0.009), (0.01, 0.014), (0, 0.014)], "bolt", 6, crease=1.0, at=(cu + (0.06 if cu < 0 else -0.06), cv + (0.06 if cv < 0 else -0.06), top - 0.001))
    elif style == "C":
        if not s: sbox(u0 if w else u0 + 0.0, u1, v0 + 0.035, v0 + 0.085, top - 0.004, top + 0.002, "sec", 0.001)
    elif style == "D":
        for (cu, cv, o1, o2) in ((u0, v0, w, s), (u1, v1, e, n), (u1, v0, e, s)):
            if not o1 and not o2 and _h(mask, cu, cv, var) > 0.25: lathe([(0, 0), (0.011, 0), (0.008, 0.006), (0, 0.007)], "metal", 5, crease=1.0, at=(cu + (0.035 if cu < 0 else -0.035), cv + (0.035 if cv < 0 else -0.035), top - 0.001))
def gasket(style):
    ins = 0.024 if style == "E" else 0.014; sbox(-(H - ins), H - ins, -(H - ins), H - ins, H - 0.001, H + 0.031, "dark", 0.002)
def lip(style):
    """hull-side wheel fender, canonical: wheel cell at the origin, hull towards -X. It ends 20 mm before the tyre can start, the wheel stays visible from above."""
    nseg, r0, th, grp, a0, a1 = dict(A=(9, 0.405, 0.012, "base", 20, 160), B=(3, 0.41, 0.022, "base", 28, 152), C=(4, 0.41, 0.03, "base", 25, 155), D=(3, 0.405, 0.010, "rust", 35, 150), E=(7, 0.41, 0.034, "base", 15, 165))[style]; da = (a1 - a0)/nseg
    for n in range(nseg):
        if style == "D" and n == 2: continue
        ang = a0 + (n + 0.5)*da; half = (r0 + th)*math.tan(math.radians(da/2)) + 0.004; sbox(-0.262, -0.168, -half, half, r0, r0 + th, grp if not (style == "D" and n == 0) else "patch", min(0.004, th*0.4), rot(X, ang - 90))
    if style == "A": sbox(-0.172, -0.168, -0.15, 0.15, r0 + 0.002, r0 + 0.008, "glow", 0.001)
    if style == "B":
        for yy in (-0.2, 0.2): rod((-0.262, yy, 0.15), (-0.20, yy*1.2, 0.36), 0.014, "metal", 0.003, 6)
def kit(style):
    """{mesh name: akit construction} of one style"""
    out = {"gasket": lambda: gasket(style), "lip": lambda: lip(style)}
    for mask in range(16):
        for v in range(VARIANTS[style]): out["plate_%02d" % mask + ("_%d" % v if VARIANTS[style] > 1 else "")] = (lambda mask=mask, v=v: tile(style, mask, v))
    return out

# ------------------------------------------------------------------------------------------------ blueprints
def _p(t, i, j, k, yaw=0, mount=None): return dict(type=t, cell=(i, j, k), yaw=yaw, mount=mount)
def _on(t, cells): return [_p(t, i, j, k, 0, (0, 0, -1)) for (i, j, k) in cells]
def _rows(rows, k, j0=0): return [_p("Cube", i, j0 + j, k) for j, r in enumerate(rows) for i, ch in enumerate(r) if ch == "#"]          # rows listed from the rear (j0) to the front
def _wheels(cells): return [_p("Wheel", i, j, k, 0 if side > 0 else 180, (-side, 0, 0)) for (i, j, k, side) in cells]
def buggy():
    p = _rows([".#.", "###", "###", "###", "###", ".#."], 0) + _rows(["###", ".#."], 1, 1) + _wheels([(3, 1, 0, 1), (3, 4, 0, 1), (-1, 1, 0, -1), (-1, 4, 0, -1)])
    return p + _on("Gun", [(1, 1, 2), (1, 4, 1)]) + _on("Battery", [(0, 1, 2), (2, 1, 2)]) + _on("Deployer", [(1, 0, 1)]) + [_p("Strut", 0, 5, 0, 0, (0, -1, 0)), _p("Strut", 2, 5, 0, 0, (0, -1, 0))]
def skimmer():
    p = _rows([".##.", "####", "####", "####", ".##."], 0) + _rows(["##", "##"], 1, 1)
    p = [dict(q, cell=(q["cell"][0] + 1, q["cell"][1], 1)) if q["cell"][2] == 1 else q for q in p]
    p += [_p("Hover", i, j, -1, 0, (0, 0, 1)) for (i, j) in ((0, 1), (3, 1), (0, 3), (3, 3), (1, 2), (2, 2))]
    return p + _on("Gun", [(1, 3, 1), (2, 3, 1), (0, 2, 1), (3, 2, 1), (1, 2, 2)]) + _on("Battery", [(0, 1, 1), (3, 1, 1), (2, 2, 2), (1, 1, 2)]) + _on("Deployer", [(1, 0, 1)])
def crawler():
    well, full = ".###.", "#####"; p = _rows([well, full, well, full, well, full, well, full, well], 0) + [_p("Cube", i, j, 1) for i in (1, 2, 3) for j in range(1, 8)] + [_p("Cube", i, j, 2) for i in (1, 2, 3) for j in (3, 4, 5)] + [_p("Cube", 2, 4, 3)]
    p += _wheels([(4, j, 0, 1) for j in (2, 4, 6)] + [(0, j, 0, -1) for j in (2, 4, 6)])
    p += _on("Gun", [(0, 7, 1), (4, 7, 1), (0, 1, 1), (4, 1, 1), (1, 7, 2), (3, 7, 2), (2, 6, 2), (2, 2, 2), (1, 5, 3), (3, 5, 3), (2, 4, 4), (2, 8, 1)])
    p += _on("Battery", [(0, 3, 1), (4, 3, 1), (0, 5, 1), (4, 5, 1), (2, 7, 2), (1, 6, 2), (3, 6, 2), (2, 1, 2), (1, 2, 2), (3, 2, 2), (1, 3, 3), (3, 3, 3), (2, 3, 3), (1, 4, 3), (3, 4, 3)])
    return p + _on("Deployer", [(1, 1, 2), (3, 1, 2)]) + _on("Strut", [(1, 8, 1), (3, 8, 1)])
def dreadnought():
    well, full = ".#####.", "#######"; p = _rows([".#####."] + [full, well]*5 + [full, "..###.."], 0) + [_p("Cube", i, j, 1) for i in range(1, 6) for j in range(1, 12)]
    p += [_p("Cube", i, j, 2) for i in (2, 3, 4) for j in (2, 3, 4, 7, 8, 9)] + [_p("Cube", 3, j, 2) for j in (5, 6)] + [_p("Cube", 3, 8, 3), _p("Cube", 3, 3, 3)]
    p += _wheels([(6, j, 0, 1) for j in (2, 4, 6, 8, 10)] + [(0, j, 0, -1) for j in (2, 4, 6, 8, 10)])
    p += _on("Gun", [(i, j, 1) for i in (0, 6) for j in (3, 7, 11)] + [(1, 11, 2), (5, 11, 2), (3, 11, 2), (1, 5, 2), (5, 5, 2), (1, 9, 2), (5, 9, 2), (2, 9, 3), (4, 9, 3), (3, 7, 3), (2, 2, 3), (4, 2, 3), (3, 4, 3), (3, 8, 4), (3, 3, 4), (3, 12, 1)])
    p += _on("Battery", [(i, j, 1) for i in (0, 6) for j in (1, 5, 9)] + [(2, 10, 2), (4, 10, 2), (3, 1, 2), (1, 3, 2), (5, 3, 2), (1, 7, 2), (5, 7, 2), (2, 7, 3), (4, 7, 3), (2, 4, 3), (4, 4, 3), (3, 2, 3)])
    return p + _on("Deployer", [(1, 1, 2), (5, 1, 2), (2, 1, 2), (4, 1, 2)]) + _on("Strut", [(2, 12, 1), (4, 12, 1)])
def models1(): return [_p("Cube", 0, 0, 0), _p("Gun", 0, 0, 1, 0, (0, 0, -1)), _p("Cube", 3, 0, 0), _p("Wheel", 4, 0, 0, 0, (-1, 0, 0)), _p("Cube", 7, 0, 0), _p("Hover", 7, 0, -1, 0, (0, 0, 1))]
def models2(): return [_p("Cube", 0, 0, 0), _p("Battery", 0, 0, 1, 0, (0, 0, -1)), _p("Cube", 3, 0, 0), _p("Deployer", 3, 0, 1, 0, (0, 0, -1))]
BLUEPRINTS = dict(models1=models1, models2=models2, buggy=buggy, skimmer=skimmer, crawler=crawler, dreadnought=dreadnought)
def validate(parts):
    """gameplay sanity of a blueprint: unique cells, every non-cube part has its support cube, the cube set is 6-connected"""
    occ = QD.occupancy(parts); cells = [p["cell"] for p in parts]; assert len(set(cells)) == len(cells), "overlapping cells"
    for p in parts:
        if p["mount"]: sup = tuple(p["cell"][i] + p["mount"][i] for i in range(3)); assert occ.get(sup) in ("Cube", "Strut"), ("no support", p)
        if p["type"] == "Strut": i, j, k = p["cell"]; assert (i, j + 1, k) not in cells and (i, j + 2, k) not in cells, ("strut blocked", p)
    cubes = {c for c, v in occ.items() if v in ("Cube", "Strut", "StrutMember")}; seen = set(); todo = [next(iter(cubes))]
    while todo:
        c = todo.pop()
        if c in seen: continue
        seen.add(c); todo += [n for n in ((c[0] + 1, c[1], c[2]), (c[0] - 1, c[1], c[2]), (c[0], c[1] + 1, c[2]), (c[0], c[1] - 1, c[2]), (c[0], c[1], c[2] + 1), (c[0], c[1], c[2] - 1)) if n in cubes]
    assert seen == cubes, "hull not connected"; return True

# ------------------------------------------------------------------------------------------------ manifest
def _M(pos, f): u, v, n = f; return [[float(u[0]), float(v[0]), float(n[0]), float(pos[0])], [float(u[1]), float(v[1]), float(n[1]), float(pos[1])], [float(u[2]), float(v[2]), float(n[2]), float(pos[2])], [0, 0, 0, 1]]
def fragments(parts, style, alive=None):
    """the hull layer of a robot: list of owned fragments. `alive` = indices of the parts that still exist (default all); plate GROUPS are always
    those of the intact blueprint (built / repaired state), a destroyed part only removes fragments and closes the edges of its neighbours."""
    occ0 = QD.occupancy(parts); c = QD.centre(parts); st = PL[style]; idx = {p["cell"]: n for n, p in enumerate(parts)}; alive = set(range(len(parts))) if alive is None else set(alive); out = []
    for n, p in enumerate(parts):
        if p["type"] == "Strut": i, j, k = p["cell"]; idx[(i, j + 2, k)] = n
    hull = lambda q: occ0.get(q) in ("Cube", "Strut"); live = lambda q: idx.get(q) in alive
    for d, f in FR.items():                                         # plates (no plates on the underside)
        du, dv, dn = cdelta(f[0]), cdelta(f[1]), cdelta(f[2]); add = lambda q, a: (q[0] + a[0], q[1] + a[1], q[2] + a[2]); layers = {}
        for q in sorted(occ0):
            if hull(q) and add(q, dn) not in occ0: layers.setdefault(sum(q[i]*dn[i] for i in range(3)), {})[(sum(q[i]*du[i] for i in range(3)), sum(q[i]*dv[i] for i in range(3)))] = q
        for pl, cells in sorted(layers.items()):
            for gi, (a, b, wa, wb) in enumerate(QD._rects(list(cells), st["big"], None)):
                gid = "%s:%d:%d" % (d, pl, gi); members = {cells[(a + x, b + y)] for x in range(wa) for y in range(wb)}
                if style == "D" and _h(gid, len(parts)) < 0.12: continue                              # a sheet that was never fitted
                var = int(_h(gid, "v")*VARIANTS[style])
                for q in sorted(members):
                    if not live(q): continue
                    mask = sum(((add(q, s) in members and live(add(q, s))) << b_) for b_, s in enumerate((du, tuple(-x for x in du), dv, tuple(-x for x in dv))))
                    out.append(dict(kind="plate", mesh="plate_%02d" % mask + ("_%d" % var if VARIANTS[style] > 1 else ""), owner=idx[q], requires=[idx[q]], group=gid, mask=mask, cell=list(q), face=d, M=_M(QD.world(q, c), f)))
    AXF = {0: (-Y, Z, X), 1: (X, Z, -Y), 2: (X, Y, Z)}; seams = set()
    for q in occ0:
        if not hull(q): continue
        for ax in range(3):
            n = tuple(q[i] + QD.AX[ax][i] for i in range(3))
            if hull(n): seams.add((q, ax, idx[q], idx[n]))
    for n, p in enumerate(parts):
        if p["mount"] and p["type"] not in ("Cube", "Strut"):
            sup = tuple(p["cell"][i] + p["mount"][i] for i in range(3)); ax = [abs(x) for x in p["mount"]].index(1); lo = p["cell"] if p["mount"][ax] > 0 else sup; seams.add((lo, ax, n, idx[sup]))
    for (q, ax, a, b) in sorted(seams):
        if a in alive and b in alive and a != b: out.append(dict(kind="gasket", mesh="gasket", owner=min(a, b), requires=sorted((a, b)), cell=list(q), axis=ax, M=_M(QD.world(q, c), AXF[ax])))
    for n, p in enumerate(parts):
        if p["type"] != "Wheel" or n not in alive: continue
        i, j, k = p["cell"]; sup = idx[tuple(p["cell"][x] + p["mount"][x] for x in range(3))]
        if (i, j, k + 1) in occ0 or sup not in alive: continue
        out.append(dict(kind="lip", mesh="lip", owner=n, requires=[n, sup], cell=list(p["cell"]), M=QD.matrix(p, c)))
    return out
def dependents(parts, removed):
    """parts that leave with the removed ones in the TEST (their support cube is gone); the game decides this itself - the layer only follows"""
    idx = {p["cell"]: n for n, p in enumerate(parts)}; gone = set(removed); ch = True
    while ch:
        ch = False
        for n, p in enumerate(parts):
            if n not in gone and p["mount"] and idx.get(tuple(p["cell"][i] + p["mount"][i] for i in range(3))) in gone: gone.add(n); ch = True
    return gone
def check(parts, frags, alive):
    """PASS conditions of the destruction test"""
    alive = set(alive); idx = {p["cell"]: n for n, p in enumerate(parts)}; errs = []; tiles = {(tuple(f["cell"]), f["face"]): f for f in frags if f["kind"] == "plate"}
    for f in frags:
        if any(r not in alive for r in f["requires"]): errs.append(("fragment of a removed part", f["kind"], f["cell"]))
    for (q, d), f in tiles.items():
        fr = FR[d]; du, dv = cdelta(fr[0]), cdelta(fr[1])
        for b_, s in enumerate((du, tuple(-x for x in du), dv, tuple(-x for x in dv))):
            nq = (q[0] + s[0], q[1] + s[1], q[2] + s[2]); nb = tiles.get((nq, d)); want = bool(nb is not None and nb["group"] == f["group"])
            if bool((f["mask"] >> b_) & 1) != want: errs.append(("open edge / hanging plate", q, d, b_))
    return errs
