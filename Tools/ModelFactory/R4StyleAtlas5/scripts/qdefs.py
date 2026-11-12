"""#1239 Visual Polish Pass 1.
1. v2 constructions of Cube / Gun / Wheel / Hover: the mount plates become plain structural cores in ONE dark substructure colour (no private
   frame per part), so neighbours read as one chassis; the armour look moves to
2. the cosmetic hull layer (PROPOSAL, visual only, no collision, no mounting rules): merged armour plates that span several blocks, gaskets that
   close the 30 mm seams, wheel arches. It is generated from the occupancy of a robot; nothing in the game contract changes.
3. identical test robots (about 30 / 30 / 100 / 200 parts) for the game-scale comparison."""
import math, numpy as np
import akit as K, pdefs as PD, qstyle as QS
from akit import sbox, lathe, cyl, tube, rod, slab, node, rot, R_to, X, Y, Z, port
H = K.H; G = K.G
def core_box(x0, x1, y0, y1, z0, z1, *a, **kw): return sbox(x0, x1, y0, y1, z0, z1, QS.CORE[K.STYLE], QS.CORE_BEVEL[K.STYLE])
def core_nodes2(seams=None):
    for y in (0.0, PD.YB): core_box(-H, H, y - H, y + H, -H, H)
def with_core(fn):
    """pilot-2 construction with its framed mount plate replaced by the structural core plate"""
    def run():
        old = PD.box, PD.nodes2; PD.box = core_box; PD.nodes2 = core_nodes2
        try: fn()
        finally: PD.box, PD.nodes2 = old
    run.__name__ = fn.__name__ + "_core"; return run
P = {t: dict(v) for t, v in PD.P.items()}
for _t in ("Gun", "Wheel", "Hover", "Strut"):
    for _s in "ABCDE": P[_t][_s] = with_core(PD.P[_t][_s])
def _cube(): sbox(-H, H, -H, H, -H, H, QS.CORE[K.STYLE], QS.CORE_BEVEL[K.STYLE])
for _s in "ABCDE": P["Cube"][_s] = _cube
def gun_a2():
    """A: as pilot 2, but the weapon reads from a distance - twin accelerator rails with a lit core instead of a thin rod"""
    PV = PD.PV
    core_box(-0.255, 0.255, -0.255, 0.255, -H, -0.215); port("down")
    lathe([(0, -0.215), (0.235, -0.215), (0.21, -0.12), (0.12, -0.02), (0.10, 0.014), (0, 0.014)], ["sec", "base", "base", "sec", "dark"], 32, crease=50)
    tube(0.20, 0.208, -0.14, -0.122, "glow", 0.002, 32)
    node("Yaw", PV, parent="GunPivot")
    lathe([(0, 0.02), (0.185, 0.02), (0.185, 0.04), (0.14, 0.085), (0.05, 0.10), (0, 0.10)], ["dark", "sec", "base", "base", "base"], 32, crease=50)
    tube(0.186, 0.19, 0.043, 0.054, "glow", 0.002, 32)
    for sx in (-1, 1): slab([(-0.10, 0.08), (0.11, 0.08), (0.06, 0.215), (-0.03, 0.215)], sx*0.16 - 0.014, sx*0.16 + 0.014, (Y, Z, X), "base")
    node("Pitch", PV, parent="Yaw")
    lathe([(0, -0.07), (0.066, -0.07), (0.072, 0.0), (0.062, 0.20), (0.05, 0.395), (0, 0.395)], ["dark", "base", "base", "sec", "dark"], 6, crease=40, R=R_to(-Y), at=(0, 0, 0.16), phase=math.pi/6)
    slab([(0.06, 0.215), (-0.30, 0.19), (-0.30, 0.205), (0.02, 0.235)], -0.01, 0.01, (Y, Z, X), "sec")
    for sx in (-1, 1): sbox(sx*0.068 - 0.004, sx*0.068 + 0.004, -0.30, 0.03, 0.152, 0.168, "glow", 0.001)
    cyl(0.03, -0.136, 0.136, "dark", 0.004, 12, R=R_to(X), at=PV)
    node("Barrel", PV, parent="Pitch")
    for sx in (-1, 1): sbox(sx*0.029 - 0.009, sx*0.029 + 0.009, -0.615, -0.105, 0.106, 0.164, "sec", 0.004)            # two rails
    sbox(-0.02, 0.02, -0.60, -0.11, 0.128, 0.142, "glow", 0.002)                                                       # lit core between them
    for y in (-0.22, -0.40, -0.58): sbox(-0.039, 0.039, y - 0.012, y + 0.012, 0.100, 0.170, "base", 0.004)             # clamps
    PD.flash()
P["Gun"]["A"] = gun_a2
POLISHED = ["Cube", "Gun", "Wheel", "Hover", "Strut"]

# ------------------------------------------------------------------------------------------------ robots (cells: i right, j forward, k up)
def _part(t, i, j, k, yaw=0, mount=None): return dict(type=t, cell=(i, j, k), yaw=yaw, mount=mount)
def _wheels(js, i_right, k=0): return [_part("Wheel", i_right, j, k, 0, (-1, 0, 0)) for j in js] + [_part("Wheel", -1, j, k, 180, (1, 0, 0)) for j in js]
def _on(t, cells): return [_part(t, i, j, k, 0, (0, 0, -1)) for (i, j, k) in cells]
def robot_wheeled():
    p = [_part("Cube", i, j, 0) for i in (0, 1) for j in range(5)] + [_part("Cube", i, j, 1) for i in (0, 1) for j in (1, 2)] + _wheels((0, 2, 4), 2)
    return p + _on("Gun", [(0, 2, 2), (1, 2, 2)]) + _on("Battery", [(0, 1, 2), (1, 1, 2), (1, 0, 1)]) + _on("Deployer", [(0, 0, 1)]) + _on("Strut", [(0, 3, 1), (1, 3, 1)])
def robot_hover():
    p = [_part("Cube", i, j, 0) for i in range(3) for j in range(4)] + [_part("Cube", 1, j, 1) for j in (1, 2)]
    p += [_part("Hover", i, j, -1, 0, (0, 0, 1)) for (i, j) in ((0, 0), (2, 0), (0, 3), (2, 3), (1, 1), (1, 2))]
    return p + _on("Gun", [(0, 3, 1), (2, 3, 1), (1, 2, 2)]) + _on("Battery", [(0, 0, 1), (2, 0, 1), (1, 1, 2)]) + _on("Deployer", [(1, 0, 1)])
def robot_100():
    p = [_part("Cube", i, j, 0) for i in range(4) for j in range(8)] + [_part("Cube", i, j, 1) for i in range(4) for j in range(1, 7)] + [_part("Cube", i, j, 2) for i in (1, 2) for j in (2, 3, 4)] + _wheels((0, 2, 4, 6, 7), 4)
    p += _on("Gun", [(0, 7, 1), (3, 7, 1), (0, 6, 2), (3, 6, 2), (0, 3, 2), (3, 3, 2), (1, 5, 2), (2, 5, 2), (1, 4, 3), (2, 4, 3), (1, 3, 3), (2, 3, 3)])
    p += _on("Battery", [(0, 0, 1), (3, 0, 1), (0, 1, 2), (3, 1, 2), (1, 1, 2), (2, 1, 2), (1, 2, 3), (2, 2, 3)]) + _on("Deployer", [(1, 0, 1), (2, 0, 1)]) + _on("Strut", [(1, 7, 1), (2, 7, 1)])
    return p
def robot_200():
    p = [_part("Cube", i, j, 0) for i in range(6) for j in range(11)] + [_part("Cube", i, j, 1) for i in range(1, 5) for j in range(1, 10)] + [_part("Cube", i, j, 2) for i in range(1, 5) for j in range(2, 8)] + [_part("Cube", i, j, 3) for i in (2, 3) for j in range(3, 7)]
    p += _wheels((0, 2, 4, 5, 6, 8, 10), 6)
    p += _on("Gun", [(i, j, 1) for i in (0, 5) for j in (1, 3, 5, 7, 9, 10)] + [(1, 9, 2), (4, 9, 2), (2, 8, 2), (3, 8, 2), (1, 4, 3), (4, 4, 3), (1, 6, 3), (4, 6, 3), (2, 7, 3), (3, 7, 3), (2, 6, 4), (3, 6, 4), (2, 4, 4), (3, 4, 4)])
    p += _on("Battery", [(i, j, 1) for i in (0, 5) for j in (2, 4, 6, 8)] + [(1, 1, 2), (2, 1, 2), (3, 1, 2), (4, 1, 2), (2, 2, 3), (3, 2, 3), (2, 3, 4), (3, 3, 4)])
    return p + _on("Deployer", [(0, 0, 1), (5, 0, 1), (2, 0, 1), (3, 0, 1)]) + _on("Strut", [(1, 10, 1), (4, 10, 1)])
def robot_parts():
    """close-up station: the four polished types on a structural cube each"""
    return [_part("Cube", 0, 0, 0), _part("Cube", 3, 0, 0), _part("Gun", 3, 0, 1, 0, (0, 0, -1)), _part("Cube", 6, 0, 0), _part("Wheel", 7, 0, 0, 0, (-1, 0, 0)), _part("Cube", 10, 0, 0), _part("Hover", 10, 0, -1, 0, (0, 0, 1))]
ROBOTS = dict(parts=robot_parts, wheeled=robot_wheeled, hover=robot_hover, r100=robot_100, r200=robot_200)
def occupancy(parts):
    occ = {}
    for p in parts:
        occ[p["cell"]] = p["type"]
        if p["type"] == "Strut": i, j, k = p["cell"]; occ[(i, j + 2, k)] = "Strut"; occ[(i, j + 1, k)] = "StrutMember"
    return occ
def centre(parts):
    c = np.array([p["cell"] for p in parts if p["type"] == "Cube"], float); return (float(c[:, 0].min() + c[:, 0].max())/2, float(c[:, 1].min() + c[:, 1].max())/2)
def world(cell, c): return np.array([(cell[0] - c[0])*G, -(cell[1] - c[1])*G, cell[2]*G])
def matrix(p, c):
    a = math.radians(p["yaw"]); cs, sn = math.cos(a), math.sin(a); t = world(p["cell"], c); return [[cs, -sn, 0, t[0]], [sn, cs, 0, t[1]], [0, 0, 1, t[2]], [0, 0, 0, 1]]

# ------------------------------------------------------------------------------------------------ cosmetic hull layer (generated, visual only)
AX = [(1, 0, 0), (0, 1, 0), (0, 0, 1)]
PLATE = {  # max merged size (cells), margin to the rectangle edge, thickness, bevel
 "A": dict(big=(3, 2), m=0.022, t=0.010, b=0.004), "B": dict(big=(2, 2), m=0.030, t=0.016, b=0.003), "C": dict(big=(3, 3), m=0.034, t=0.012, b=0.012),
 "D": dict(big=(2, 1), m=0.020, t=0.009, b=0.003), "E": dict(big=(2, 2), m=0.030, t=0.024, b=0.02)}
def _rects(cells, big, rng):
    """greedy merge of unit cells (a, b) into rectangles not larger than `big` (either orientation)"""
    left = set(cells); out = []
    for c in sorted(cells):
        if c not in left: continue
        best = (1, 1)
        for (wa, wb) in sorted({big, big[::-1], (big[0], 1), (1, big[1]), (2, 1), (1, 2), (1, 1)}, key=lambda s: -s[0]*s[1]):
            if all((c[0] + u, c[1] + v) in left for u in range(wa) for v in range(wb)): best = (wa, wb); break
        for u in range(best[0]):
            for v in range(best[1]): left.discard((c[0] + u, c[1] + v))
        out.append((c[0], c[1], best[0], best[1]))
    return out
def _wbox(lo, hi, grp, b): return sbox(lo[0], hi[0], lo[1], hi[1], lo[2], hi[2], grp, b)
def hull_layer(parts, style):
    """builds the cosmetic layer of one robot in robot-local Blender coordinates (inside akit.build, authored)"""
    occ = occupancy(parts); c = centre(parts); st = PLATE[style]; rng = np.random.RandomState(1239 + len(parts)); sgn = (1, -1, 1); m, t, b = st["m"], st["t"], st["b"]
    cubes = [k for k, v in occ.items() if v in ("Cube", "Strut")]                      # strut node blocks are hull blocks too
    # 1. gaskets: every 30 mm seam between a cube and an attached neighbour becomes a closed, recessed joint
    seams = set()
    for q in cubes:
        for ax in range(3):
            n = tuple(q[i] + AX[ax][i] for i in range(3))
            if occ.get(n) in ("Cube", "Strut"): seams.add((q, ax))
    for p in parts:
        if p["mount"] and p["type"] != "Cube":
            sup = tuple(p["cell"][i] + p["mount"][i] for i in range(3)); ax = [abs(x) for x in p["mount"]].index(1); lo = p["cell"] if p["mount"][ax] > 0 else sup
            if occ.get(sup) in ("Cube", "Strut"): seams.add((lo, ax))
            if p["type"] == "Strut": i, j, k = p["cell"]; seams.add(((i, j + 2, k - 1), 2)) if occ.get((i, j + 2, k - 1)) == "Cube" else None
    ins = 0.014 if style != "E" else 0.024
    for (q, ax) in sorted(seams):
        w = world(q, c); lo = w - (H - ins); hi = w + (H - ins); d = sgn[ax]; a0 = w[ax] + d*H; a1 = w[ax] + d*(H + 0.03); lo[ax] = min(a0, a1) - 0.001; hi[ax] = max(a0, a1) + 0.001
        _wbox(lo, hi, "dark", 0.002)
    # 2. armour plates on the exposed cube faces, merged across blocks
    for ax in range(3):
        a1, a2 = [i for i in range(3) if i != ax]
        for s in (1, -1):
            layers = {}
            for q in cubes:
                n = tuple(q[i] + s*AX[ax][i] for i in range(3))
                if n not in occ: layers.setdefault(q[ax], []).append((q[a1], q[a2]))
            for pl, cells in sorted(layers.items()):
                for (u, v, wu, wv) in _rects(cells, st["big"], rng):
                    if style == "D" and rng.rand() < 0.14: continue                                   # a missing sheet shows the dark structure
                    q0 = [0, 0, 0]; q0[ax] = pl; q0[a1] = u; q0[a2] = v; q1 = list(q0); q1[a1] = u + wu - 1; q1[a2] = v + wv - 1; w0 = world(q0, c); w1 = world(q1, c)
                    lo = np.minimum(w0, w1) - H; hi = np.maximum(w0, w1) + H; mm = [m, m, m, m]
                    if style == "D": mm = list(m + rng.uniform(0.0, 0.035, 4))
                    lo[a1] += mm[0]; hi[a1] -= mm[1]; lo[a2] += mm[2]; hi[a2] -= mm[3]; d = s*sgn[ax]; face = w0[ax] + d*H; th = t*(rng.uniform(0.7, 1.4) if style == "D" else 1.0)
                    lo[ax] = min(face - d*0.003, face + d*th); hi[ax] = max(face - d*0.003, face + d*th)
                    grp = "base"
                    if style == "C" and ax != 2 and pl == 0 and False: grp = "sec"
                    if style == "D": grp = rng.choice(["base", "base", "patch", "rust"])
                    if style == "E" and ax == 2 and s == 1: grp = "base"
                    _wbox(lo, hi, grp, b); nrm = np.zeros(3); nrm[ax] = d; top = face + d*th; ctr = (lo + hi)/2
                    def at(du, dv, dn=0.0): x = ctr.copy(); x[a1] += du; x[a2] += dv; x[ax] = top + d*dn; return x
                    eu = (hi[a1] - lo[a1])/2; ev = (hi[a2] - lo[a2])/2; long1 = eu >= ev
                    if style == "A":                                                                  # one inset graphite strip + a light line along the long side of bigger plates
                        if wu*wv >= 2:
                            l2 = ctr.copy(); h2 = ctr.copy(); l2[a1] -= eu - 0.03 if long1 else 0.012; h2[a1] += eu - 0.03 if long1 else 0.012; l2[a2] -= 0.012 if long1 else ev - 0.03; h2[a2] += 0.012 if long1 else ev - 0.03
                            off = (ev - 0.06) if long1 else (eu - 0.06); l2[a2 if long1 else a1] += off; h2[a2 if long1 else a1] += off; l2[ax] = min(top - d*0.004, top + d*0.0015); h2[ax] = max(top - d*0.004, top + d*0.0015); _wbox(l2, h2, "glow", 0.001)
                    elif style == "B":                                                                # corner bolts + a welded rib on big plates
                        for su in (-1, 1):
                            for sv in (-1, 1): lathe([(0, 0), (0.017, 0), (0.017, 0.008), (0.011, 0.013), (0, 0.013)], "bolt", 6, crease=1.0, R=R_to(nrm), at=at(su*(eu - 0.04), sv*(ev - 0.04), -0.001))
                        if wu*wv >= 2:
                            l2 = ctr.copy(); h2 = ctr.copy(); l2[a1] -= eu - 0.09 if long1 else 0.02; h2[a1] += eu - 0.09 if long1 else 0.02; l2[a2] -= 0.02 if long1 else ev - 0.09; h2[a2] += 0.02 if long1 else ev - 0.09
                            l2[ax] = min(top - d*0.002, top + d*0.012); h2[ax] = max(top - d*0.002, top + d*0.012); _wbox(l2, h2, "dark", 0.003)
                    elif style == "D":
                        for (su, sv) in ((-1, -1), (1, 1), (-1, 1))[:int(rng.randint(1, 4))]: lathe([(0, 0), (0.011, 0), (0.008, 0.006), (0, 0.007)], "metal", 5, crease=1.0, R=R_to(nrm), at=at(su*(eu - 0.03), sv*(ev - 0.03), -0.001))
    # 3. wheel arches where the cell above the wheel is free
    for p in parts:
        if p["type"] != "Wheel": continue
        i, j, k = p["cell"]
        if (i, j, k + 1) in occ: continue
        w = world(p["cell"], c); sx = -p["mount"][0]; x_in = w[0] - sx*0.262; x_out = w[0] + sx*0.20; x0, x1 = min(x_in, x_out), max(x_in, x_out)
        nseg, r0, th, grp, a0, a1 = dict(A=(9, 0.405, 0.012, "base", 20, 160), B=(3, 0.41, 0.022, "base", 28, 152), C=(4, 0.41, 0.03, "base", 25, 155), D=(3, 0.405, 0.010, "rust", 35, 150), E=(7, 0.41, 0.036, "base", 15, 165))[style]
        da = (a1 - a0)/nseg
        for n in range(nseg):
            if style == "D" and n == 2: continue                                                     # a torn-off section
            ang = a0 + (n + 0.5)*da; half = (r0 + th)*math.tan(math.radians(da/2)) + 0.004
            sbox(x0 + (0.02 if style == "D" and n == 1 else 0), x1, -half, half, r0, r0 + th, grp if not (style == "D" and n == 0) else "patch", min(b, th*0.4), rot(X, ang - 90), (0, w[1], w[2]))
        if style == "A": sbox(x_out - sx*0.004 if sx > 0 else x_out, x_out if sx > 0 else x_out + 0.004, -0.16, 0.16, r0 + 0.002, r0 + 0.008, "glow", 0.001, None, (0, w[1], w[2]))
        if style == "B":
            for yy in (-0.2, 0.2): rod((x_in, w[1] + yy, w[2] + 0.15), (x_out - sx*0.03, w[1] + yy*1.2, w[2] + 0.36), 0.014, "metal", 0.003, 6)
