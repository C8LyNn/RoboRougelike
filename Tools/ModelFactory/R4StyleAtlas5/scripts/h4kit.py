"""#1239 Hero Benchmark Pass 4: NEW hull armour. The cube stays the approved structural core (44 triangles, "armour comes from the hull layer"); what
the player sees of a hull is the cosmetic, owned, reversible hull layer of Visual Polish Pass 2 / Hero Pass 1 (rhull.py, hhull.py). This script
re-models every mesh of that layer with the high-fidelity kit of Pass 2 (source model + game mesh + bake + skin) and keeps its RULES untouched:
same mesh names, same canonical frames, same footprints, so fragments(parts, style) places the new armour without any change - a lone cube still
gets its own plates, a destroyed cube still takes exactly its fragments with it.
  plate_MM[_V]  one tile per exposed cube face; 4-bit mask = sides on which the tile continues into a neighbour (bit 0 +u, 1 -u, 2 +v, 3 -v)
  gasket, lip   seam between two parts; hull-side wheel fender          ramp, cheek, nose_M   silhouette fairings of Hero Pass 1
KEY: A, C, D, E = the direction preferred in survey 1; the Brutal hull was a tie, so it has two proposals: Bo (from Pass 1: painted bolted
plate) and Bn (from Pass 2: black cast steel). Worn styles get wear variants of each tile (B: 2, D: 3) so that neighbours do not repeat.
usage: blender -b --factory-startup --python h4kit.py -- OUT KEY [SIZE]"""
import bpy, bmesh, sys, os, math, time, json, zlib
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import numpy as np
import hf, hbake, hskin, h4defs as D
from hf import spl
from mathutils import Vector, Matrix
a = sys.argv[sys.argv.index("--") + 1:]; OUT, KEY = a[:2]; SIZE = int(a[2]) if len(a) > 2 else 4096; STYLE = KEY[0]
LABEL = dict(A="A_Sleek", Bo="B_Brutal_P", Bn="B_Brutal_Q", C="C_Arcade", D="D_Scrap", E="E_Toon")[KEY]; SK = dict(Bn="B").get(KEY, KEY)
H = 0.26; G = 0.55; HP = G/2; S45 = math.sqrt(0.5); L45 = 0.389; TAU = 2*math.pi
NVAR = dict(A=1, Bo=2, Bn=2, C=1, D=3, E=1)[KEY]                                                    # wear / layout variants per tile
MARGIN = dict(A=0.022, Bo=0.030, Bn=0.030, C=0.036, D=0.020, E=0.034)[KEY]; CH = dict(A=0.035, Bo=0.014, Bn=0.020, C=0.065, D=0.0, E=0.075)[KEY]
def _h(*a): return (zlib.crc32(repr(a).encode()) % 1000)/1000.0
def P(*p): d = os.path.join(OUT, *p[:-1]); os.makedirs(d, exist_ok=True); return os.path.join(d, p[-1])
def rect(mask, m=None, extra=0.0):
    e, w, n, s = [(mask >> i) & 1 for i in range(4)]; m = MARGIN if m is None else m
    return (-HP if w else -(H - m - extra), HP if e else H - m - extra, -HP if s else -(H - m - extra), HP if n else H - m - extra)
def closed(mask): e, w, n, s = [(mask >> i) & 1 for i in range(4)]; return (not w, not e, not s, not n)         # (west, east, south, north) is a real border
def inset(r, d, cl): return (r[0] + d*cl[0], r[1] - d*cl[1], r[2] + d*cl[2], r[3] - d*cl[3])
def poly(r, c, cl):
    u0, u1, v0, v1 = r; cw, ce, cs, cn = cl; p = []
    for (cu, cv, o1, o2, a, b) in ((u0, v0, cw, cs, (0, c), (c, 0)), (u1, v0, ce, cs, (-c, 0), (0, c)), (u1, v1, ce, cn, (0, -c), (-c, 0)), (u0, v1, cw, cn, (c, 0), (0, -c))):
        p += [(cu + a[0], cv + a[1]), (cu + b[0], cv + b[1])] if (c > 0 and o1 and o2) else [(cu, cv)]
    return p
def corners(r, cl, d):
    u0, u1, v0, v1 = r; cw, ce, cs, cn = cl
    return [(cu + (d if cu == u0 else -d), cv + (d if cv == v0 else -d)) for (cu, cv, o1, o2) in ((u0, v0, cw, cs), (u1, v0, ce, cs), (u1, v1, ce, cn), (u0, v1, cw, cn)) if o1 and o2]


def armour(B, nd, r, cl, z0, var=0, seed=0):
    """one armour panel of the style: rectangle r = (u0, u1, v0, v1) on the plane z = z0, facing +Z; cl = which of its four edges are real borders.
    Returns nothing; the pieces are added to node nd in the canonical frame (ramp / cheek move them afterwards)"""
    u0, u1, v0, v1 = r; cw, ce, cs, cn = cl; pr = lambda zone, rr, c, a, b, name, bev=None, seg=(3, 1), kind="solid": B.bevel(B.prism(nd, zone, poly(rr, c, cl), a, b, ("x", "y", "z"), kind=kind, name=name), bev, seg, 30) if bev else B.prism(nd, zone, poly(rr, c, cl), a, b, ("x", "y", "z"), kind=kind, name=name)
    ua, ub = u0 + 0.034*cw, u1 - 0.034*ce; va, vb = v0 + 0.034*cs, v1 - 0.034*cn
    if KEY == "A":                                                                           # LAYERED: dark sub-frame; a chin shell, a lit gap, the main shell and a raised lip standing off it
        pr("dark", r, 0.026, z0, z0 + 0.008, "subframe"); r1 = inset(r, 0.012, cl); vm = (v0 + 0.074) if cs else v0
        B.bevel(B.prism(nd, "base", poly((r1[0], r1[1], vm, r1[3]), 0.050, (cw, ce, False, cn)), z0 + 0.010, z0 + 0.024, ("x", "y", "z"), name="shell"), 0.004, (3, 1), 30)
        if cs:
            B.bevel(B.prism(nd, "base", poly((r1[0] + 0.020*cw, r1[1] - 0.020*ce, r1[2], v0 + 0.060), 0.030, (cw, ce, True, False)), z0 + 0.010, z0 + 0.020, ("x", "y", "z"), name="chin"), 0.003, (2, 1), 30)
            B.box(nd, "glow", (ua, v0 + 0.062, z0 + 0.008), (ub, v0 + 0.072, z0 + 0.0125), name="lightline")
        if cn:
            r2 = (r1[0] + 0.040*cw, r1[1] - 0.040*ce, v1 - 0.110, v1 - 0.024); B.bevel(B.prism(nd, "base", poly(r2, 0.024, (cw, ce, True, True)), z0 + 0.024, z0 + 0.036, ("x", "y", "z"), name="lip"), 0.004, (3, 1), 30)
            if ce: B.box(nd, "sec", (r2[1] - 0.070, v1 - 0.060, z0 + 0.0355), (r2[1] - 0.024, v1 - 0.046, z0 + 0.0375), name="tab")
        if all(cl): B.box(nd, "dark", (-0.100, -0.020, z0 + 0.0215), (0.100, 0.014, z0 + 0.0248), name="sensor"); B.box(nd, "glow", (0.066, -0.012, z0 + 0.0248), (0.088, 0.006, z0 + 0.0256), name="sensor_led", kind="detail")
        for (x, y) in corners(r, cl, 0.022): B.cyl(nd, "bare", (x, y, z0 + 0.008), (x, y, z0 + 0.0105), 0.007, (10, 6), "detail", "fastener")
    elif KEY == "Bo":                                                                        # painted plate + bolted applique slab + angle-iron frame
        pr("base", r, CH, z0, z0 + 0.016, "plate", 0.003, (2, 1)); ra = (u0 + 0.070*cw, u1 - 0.070*ce, v0 + 0.070*cs, v1 - 0.070*cn); pr("base", ra, 0.030, z0 + 0.016, z0 + 0.032, "applique", 0.006)
        for (on, bx) in ((cs, (u0, u1, v0, v0 + 0.036)), (cn, (u0, u1, v1 - 0.036, v1)), (cw, (u0, u0 + 0.036, v0, v1)), (ce, (u1 - 0.036, u1, v0, v1))):
            if on: f = B.box(nd, "dark", (bx[0], bx[2], z0 + 0.014), (bx[1], bx[3], z0 + 0.026), name="frame"); B.bevel(f, 0.003, (2, 0))
        for (x, y) in corners(r, cl, 0.018): B.cyl(nd, "bare", (x, y, z0 + 0.026), (x, y, z0 + 0.036), 0.013, (6, 6), name="bolt")
        if cs: B.box(nd, "sec", (ra[0] + 0.020, ra[2] + 0.016, z0 + 0.0315), (ra[1] - 0.020, ra[2] + 0.050, z0 + 0.0345), name="hazard")
        if cn and cw and ce: lg = B.prism(nd, "metal", [(-0.036, 0.0), (0.036, 0.0), (0.026, 0.034), (-0.026, 0.034)], v1 - 0.030, v1 - 0.014, ("x", "z", "y"), at=(0, 0, z0 + 0.026), name="lug"); B.cut(lg, B.cyl(nd, "metal", (0, v1 - 0.04, z0 + 0.044), (0, v1, z0 + 0.044), 0.009, (10, 6), kind="cutter"))
        n = max(2, int((ub - ua)/0.070))
        for i in range(n + 1):
            x = ua + (ub - ua)*i/n
            if cs: B.cyl(nd, "bare", (x, v0 + 0.018, z0 + 0.026), (x, v0 + 0.018, z0 + 0.030), 0.006, (8, 6), "detail", "rivet")
            if cn: B.cyl(nd, "bare", (x, v1 - 0.018, z0 + 0.026), (x, v1 - 0.018, z0 + 0.030), 0.006, (8, 6), "detail", "rivet")
        if cw or ce: B.weld(nd, [(ra[0], ra[2], z0 + 0.017), (ra[0], ra[3], z0 + 0.017)] if cw else [(ra[1], ra[2], z0 + 0.017), (ra[1], ra[3], z0 + 0.017)], 0.004)
    elif KEY == "Bn":                                                                        # one cast slab with sloped borders, recessed field, edge guard
        lo_, hi_ = poly(r, CH, cl), poly(inset(r, 0.032, cl), CH, cl); sl = B.loft(nd, "base", [[(x, y, z0) for (x, y) in lo_], [(x, y, z0 + 0.008) for (x, y) in lo_], [(x, y, z0 + 0.034) for (x, y) in hi_]], True, name="slab"); B.bevel(sl, 0.004, (3, 1), 25)
        rf = inset(r, 0.090, cl); B.cut(sl, B.prism(nd, "base", poly(rf, 0.030, cl), z0 + 0.029, z0 + 0.05, ("x", "y", "z"), kind="cutter"), lo=False)
        for (x, y) in corners(r, cl, 0.060): B.cyl(nd, "bare", (x, y, z0 + 0.034), (x, y, z0 + 0.046), 0.016, (6, 6), name="bolt"); B.cyl(nd, "dark", (x, y, z0 + 0.034), (x, y, z0 + 0.037), 0.024, (12, 6), "detail", "washer")
        if cs: gd = B.prism(nd, "sec", [(v0 - 0.002, z0 + 0.004), (v0 + 0.040, z0 + 0.036), (v0 + 0.070, z0 + 0.036), (v0 + 0.070, z0 + 0.042), (v0 + 0.034, z0 + 0.042), (v0 - 0.006, z0 + 0.010)], max(ua - 0.010, u0), min(ub + 0.010, u1), ("y", "z", "x"), name="guard"); B.bevel(gd, 0.002, (2, 0))
        if cn and cw and ce:
            for x in (-0.080, 0.080): B.pipe(nd, "metal", [(x - 0.030, v1 - 0.050, z0 + 0.030, 'c'), (x - 0.026, v1 - 0.050, z0 + 0.046), (x + 0.026, v1 - 0.050, z0 + 0.046), (x + 0.030, v1 - 0.050, z0 + 0.030, 'c')], 0.008, (8, 6), (4, 2), name="tiedown")
    elif KEY == "C":                                                                         # navy frame, one fat white pillow, one orange band
        pr("dark", r, 0.060, z0, z0 + 0.010, "frame", 0.004, (2, 1)); r1 = inset(r, 0.018, cl); pr("base", r1, 0.066, z0 + 0.008, z0 + 0.028, "pillow", 0.012)
        if cs: rb = (r1[0] + 0.050*cw, r1[1] - 0.050*ce, v0 + 0.064, v0 + 0.124); B.bevel(B.prism(nd, "sec", poly(rb, 0.020, (cw, ce, True, True)), z0 + 0.026, z0 + 0.034, ("x", "y", "z"), name="band"), 0.006, (3, 1), 30)
        rp = inset(r, 0.080, cl); rp = (rp[0], rp[1], (v0 + 0.150) if cs else rp[2], rp[3]); B.bevel(B.prism(nd, "base", poly(rp, 0.040, cl), z0 + 0.026, z0 + 0.038, ("x", "y", "z"), name="plateau"), 0.008, (3, 1), 30)
        for (x, y) in corners(r, (cw, ce, False, cn), 0.052): B.rev(nd, "dark", spl([(0.0, 0.0, 'c'), (0.016, 0.0, 'c'), (0.016, 0.004), (0.010, 0.010), (0.0, 0.011, 'c')], B.q(3, 1)), (12, 8), "z", (x, y, z0 + 0.027), name="stud")
    elif KEY == "D":                                                                         # salvaged sheets, overlapped; strap; patch; welds. One colour family
        j = lambda k: 0.022*_h(seed, k, var); z1 = z0 + 0.008; zones = (("base", "rust", "alt"), ("alt", "base", "sec"), ("rust", "sec", "base"))[var % 3]
        cut = (-0.060, 0.070, 0.010)[var % 3]; ra = (u0 + j(1)*cw, u1 - j(2)*ce, v0 + j(3)*cs, (cut + 0.050) if (cs or cn) else v1); rb = (u0 + j(4)*cw, u1 - j(5)*ce, (cut - 0.030) if (cs or cn) else v0, v1 - j(6)*cn)
        s1 = B.prism(nd, zones[0], [(ra[0], ra[2]), (ra[1], ra[2] + 0.014*cs), (ra[1], ra[3]), (ra[0], ra[3])], z0, z1, ("x", "y", "z"), name="sheet_a")
        nx = (-0.090, 0.060, -0.020)[var % 3]; clip = 0.070*ce*cn*(var != 1)
        if cs or cn: s2 = B.prism(nd, zones[1], [(rb[0] + 0.020*cw, rb[2]), (nx - 0.034, rb[2] + 0.004), (nx, rb[2] + 0.040), (nx + 0.030, rb[2] + 0.008), (rb[1], rb[2] + 0.012), (rb[1], rb[3] - clip), (rb[1] - clip, rb[3]), (rb[0], rb[3] - 0.018*cw*cn)] if clip else
                                     [(rb[0] + 0.020*cw, rb[2]), (nx - 0.034, rb[2] + 0.004), (nx, rb[2] + 0.040), (nx + 0.030, rb[2] + 0.008), (rb[1], rb[2] + 0.012), (rb[1] - 0.016*ce*cn, rb[3]), (rb[0], rb[3] - 0.018*cw*cn)], z1, z1 + 0.008, ("x", "y", "z"), name="sheet_b"); B.weld(nd, [(rb[0] + 0.03, rb[2] + 0.002, z1 + 0.001), (0.0, rb[2] + 0.008, z1 + 0.001), (rb[1] - 0.03, rb[2] + 0.012, z1 + 0.001)], 0.005)
        if cw or ce:
            x = (u0 + 0.090) if cw else (u1 - 0.120); st = B.box(nd, "metal", (x, v0 + 0.02*cs, z1 + 0.006), (x + 0.044, v1 - 0.03*cn, z1 + 0.016), name="strap"); B.bevel(st, 0.002, (1, 0))
            for y in (v0 + 0.07, 0.0, v1 - 0.08): B.cyl(nd, "bare", (x + 0.022, y, z1 + 0.016), (x + 0.022, y, z1 + 0.023), 0.010, (5, 5), name="bolt")
        if var == 1 and cn: pt = B.prism(nd, "alt2", [(-0.110, 0.060), (0.080, 0.050), (0.100, 0.170), (-0.090, 0.190)], z1 + 0.008, z1 + 0.014, ("x", "y", "z"), name="patch"); B.weld(nd, [(-0.110, 0.060, z1 + 0.009), (0.080, 0.050, z1 + 0.009), (0.100, 0.170, z1 + 0.009), (-0.090, 0.190, z1 + 0.009), (-0.110, 0.060, z1 + 0.009)], 0.004)
        for (x, y) in corners(r, cl, 0.040):
            if _h(seed, x, y, var) > 0.2: B.cyl(nd, "bare", (x, y, z1), (x, y, z1 + 0.014), 0.011, (5, 5), name="bolt")
        if cn and ce and var == 1: B.prism(nd, zones[2], [(u1 - 0.110, v1 - 0.004), (u1 - 0.004, v1 - 0.004), (u1 - 0.004, v1 - 0.110)], z1 + 0.008, z1 + 0.014, ("x", "y", "z"), name="corner_gusset")
        if cs and cw and var != 0: tb = B.box(nd, zones[1], (u0 + 0.010, v0 + 0.004, z0), (u0 + 0.080, v0 + 0.050, z0 + 0.006), name="bent_tab"); B.move(tb, Matrix.Translation((u0 + 0.010, v0 + 0.004, z0)) @ Matrix.Rotation(math.radians(22), 4, "X") @ Matrix.Translation((-(u0 + 0.010), -(v0 + 0.004), -z0)))
    else:                                                                                    # E: thick navy outline plate, puffy orange pillow, bold marking, dome rivets
        pr("dark", r, CH, z0, z0 + 0.014, "outline", 0.005, (2, 1)); r1 = inset(r, 0.022, cl); pl = pr("base", r1, CH*0.85, z0 + 0.010, z0 + 0.040, "pillow", 0.020, (4, 1)); B.cut(pl, B.prism(nd, "base", poly(inset(r1, 0.040, cl), CH*0.6, cl), z0 + 0.0375, z0 + 0.06, ("x", "y", "z"), kind="cutter"), lo=False); B.prism(nd, "base", poly(inset(r1, 0.046, cl), CH*0.55, cl), z0 + 0.036, z0 + 0.0405, ("x", "y", "z"), kind="detail", name="pillow_field")
        if cs: B.bevel(B.prism(nd, "sec", [(-0.110, -0.030), (0.0, -0.090), (0.110, -0.030), (0.110, 0.020), (0.0, -0.040), (-0.110, 0.020)], z0 + 0.038, z0 + 0.046, ("x", "y", "z"), at=((u0 + u1)/2, v0 + 0.170, 0), name="chevron"), 0.004, (2, 1), 30)
        for (x, y) in corners(r, cl, 0.050): B.rev(nd, "sec", spl([(0.0, 0.0, 'c'), (0.020, 0.0, 'c'), (0.020, 0.006), (0.012, 0.014), (0.0, 0.016, 'c')], B.q(3, 1)), (14, 8), "z", (x, y, z0 + 0.012), name="rivet")
def tile(B, nd, mask, var): armour(B, nd, rect(mask), closed(mask), H - 0.003, var, mask)
def moved(B, n0, Mx, c0=0):
    for o in B.objs[n0:] + B.cutters[c0:]: o.data.transform(Mx)
def R4(R3): Mx = Matrix.Identity(4); [Mx[i].__setitem__(j, float(R3[i][j])) for i in range(3) for j in range(3)]; return Mx


def gasket(B, nd): ins = 0.024 if KEY == "E" else 0.014; B.bevel(B.box(nd, "dark", (-(H - ins), -(H - ins), H - 0.001), (H - ins, H - ins, H + 0.031), name="gasket"), 0.002, (2, 0))
def ramp(B, nd):
    """canonical: the empty cell at the origin, lower cube under it, upper cube at +Y; the slope falls towards -Y"""
    if KEY == "D":
        for x in (-0.20, 0.19): B.cyl(nd, "metal", (x, -0.22, -0.275), (x, 0.26, 0.21), 0.013, (6, 6), name="prop"); B.cyl(nd, "rust", (x, 0.10, -0.275), (x, 0.10, 0.05), 0.011, (6, 6), name="post"); B.weld(nd, [(x - 0.015, 0.10, 0.05), (x + 0.015, 0.10, 0.06)], 0.004)
    else: B.prism(nd, "dark", [(-0.245, -0.275), (0.29, -0.275), (0.29, 0.245)], -0.236, 0.236, ("y", "z", "x"), name="closure")
    n0, c0 = len(B.objs), len(B.cutters); A_, B_ = 0.25, L45 - 0.014; armour(B, nd, (-A_, A_, -B_, B_), (True, True, True, True), -0.021, 0, 101); moved(B, n0, Matrix.Rotation(math.radians(45), 4, "X"), c0)
def cheek(B, nd):
    """canonical: the empty cell at the origin, hull at +Y and at +X; the armour face runs diagonally, facing -X -Y"""
    t = dict(A=0.011, Bo=0.018, Bn=0.018, C=0.013, D=0.009, E=0.024)[KEY]
    B.prism(nd, "dark", [(-0.235, 0.29), (0.29, 0.29), (0.29, -0.235)], -H, H - 0.004, ("x", "y", "z"), name="closure"); dk = B.prism(nd, "rust" if KEY == "D" else "base", [(-0.262, 0.29), (0.29, 0.29), (0.29, -0.262)], H - 0.004, H + t, ("x", "y", "z"), name="deck"); B.bevel(dk, 0.003, (2, 0))
    n0, c0 = len(B.objs), len(B.cutters); armour(B, nd, (-(L45 - 0.014), L45 - 0.014, -0.25, 0.25), (True, True, True, True), -0.021, 1, 202); moved(B, n0, R4([[S45, 0.0, -S45], [-S45, 0.0, -S45], [0.0, 1.0, 0.0]]), c0)
def nose(B, nd, mask):
    """canonical = plate tile frame (face +Z, u = +X, v = +Y = up on a side face). bit 0 / 1: the bumper continues into the +u / -u neighbour"""
    e, w = mask & 1, (mask >> 1) & 1; u0, u1 = (-HP if w else -H + 0.02), (HP if e else H - 0.02); VN = ("y", "z", "x")
    if KEY == "A":                                                                           # layered prow: dark wedge core, two ceramic shells with a lit gap, splitter blade, end caps
        B.prism(nd, "dark", [(-0.236, H), (0.150, H), (-0.040, H + 0.100), (-0.186, H + 0.100)], u0, u1, VN, name="core")
        up = B.prism(nd, "base", [(0.170, H + 0.004), (0.176, H + 0.016), (-0.022, H + 0.118), (-0.034, H + 0.106)], u0 + 0.012*(not w), u1 - 0.012*(not e), VN, name="shell_up"); B.bevel(up, 0.003, (2, 1), 30)
        lw = B.prism(nd, "base", [(-0.050, H + 0.104), (-0.050, H + 0.118), (-0.190, H + 0.118), (-0.236, H + 0.060), (-0.236, H + 0.040), (-0.190, H + 0.104)], u0 + 0.012*(not w), u1 - 0.012*(not e), VN, name="shell_low"); B.bevel(lw, 0.003, (2, 1), 30)
        B.box(nd, "glow", (u0 + 0.030, -0.048, H + 0.104), (u1 - 0.030, -0.036, H + 0.1075), name="light"); sp = B.prism(nd, "dark", [(-0.262, H), (-0.244, H), (-0.244, H + 0.150), (-0.256, H + 0.162)], u0, u1, VN, name="splitter")
        for (on, x) in ((not w, u0), (not e, u1 - 0.012)):
            if on: B.box(nd, "sec", (x, -0.236, H + 0.020), (x + 0.012, -0.060, H + 0.094), name="endcap")
    elif KEY == "Bo":                                                                        # bull bar: two tube rails on gusseted brackets, glacis lip, hazard board
        gl = B.prism(nd, "base", [(-0.020, H), (0.210, H), (0.210, H + 0.022), (0.000, H + 0.075)], u0, u1, VN, name="glacis"); B.bevel(gl, 0.003, (2, 0))
        for v in (-0.200, -0.090): B.cyl(nd, "metal", (u0, v, H + 0.092), (u1, v, H + 0.092), 0.024, (10, 8), name="tube")
        for x in (-0.150, 0.150):
            bk = B.prism(nd, "dark", [(-0.236, H), (-0.050, H), (-0.066, H + 0.080), (-0.224, H + 0.080)], x - 0.014, x + 0.014, VN, name="bracket"); B.bevel(bk, 0.003, (1, 0)); B.weld(nd, [(x + 0.016, -0.230, H + 0.002), (x + 0.016, -0.056, H + 0.002)], 0.004)
        B.bevel(B.box(nd, "sec", (-0.090, -0.186, H + 0.110), (0.090, -0.104, H + 0.120), name="board"), 0.003, (2, 0)); B.bolts(nd, "bare", 0.0, 1, H + 0.120, 0.009, 0.006, "z", (-0.070, -0.145, 0), kind="solid"); B.bolts(nd, "bare", 0.0, 1, H + 0.120, 0.009, 0.006, "z", (0.070, -0.145, 0), kind="solid")
    elif KEY == "Bn":                                                                        # cast ram: wedge block with a chamfered face, two tow hooks, yellow lower guard
        rm = B.prism(nd, "base", [(-0.240, H), (0.060, H), (0.020, H + 0.060), (-0.060, H + 0.112), (-0.200, H + 0.112), (-0.240, H + 0.070)], u0, u1, VN, name="ram"); B.bevel(rm, 0.006, (3, 1), 30)
        B.prism(nd, "sec", [(-0.246, H + 0.004), (-0.200, H + 0.004), (-0.200, H + 0.118), (-0.214, H + 0.118), (-0.246, H + 0.076)], u0 + 0.020, u1 - 0.020, VN, name="guard")
        for x in (-0.140, 0.140): hk = B.prism(nd, "metal", [(-0.130, H + 0.110), (-0.060, H + 0.110), (-0.050, H + 0.150), (-0.070, H + 0.172), (-0.110, H + 0.172), (-0.110, H + 0.150), (-0.084, H + 0.150), (-0.090, H + 0.130), (-0.130, H + 0.130)], x - 0.012, x + 0.012, VN, name="hook"); B.bevel(hk, 0.003, (2, 0))
        B.bolts(nd, "bare", 0.0, 1, H + 0.112, 0.014, 0.010, "z", (0, -0.130, 0), kind="solid")
    elif KEY == "C":                                                                         # one chunky bumper with a pad and white ends
        B.bevel(B.box(nd, "dark", (u0, -0.240, H - 0.002), (u1, -0.030, H + 0.100), name="bumper"), 0.024, (3, 1), 30); B.bevel(B.box(nd, "sec", (-0.120, -0.196, H + 0.094), (0.120, -0.074, H + 0.110), name="pad"), 0.014, (3, 1), 30)
        for (on, x) in ((not w, u0 - 0.004), (not e, u1 - 0.036)):
            if on: B.bevel(B.box(nd, "base", (x, -0.246, H + 0.010), (x + 0.040, -0.024, H + 0.106), name="end"), 0.012, (3, 1), 30)
    elif KEY == "D":                                                                         # welded ram: two unequal pipes on cut brackets, three teeth of different length, a patch
        B.cyl(nd, "rust", (u0, -0.200, H + 0.075), (u1, -0.200, H + 0.075), 0.022, (7, 7), name="ram_pipe"); B.cyl(nd, "metal", (u0 + 0.03*(not w), -0.075, H + 0.050), (u1, -0.075, H + 0.050), 0.016, (6, 6), name="ram_pipe2")
        for a_ in (-0.160, 0.140): B.cyl(nd, "metal", (a_, -0.200, H - 0.002), (a_, -0.200, H + 0.075), 0.014, (6, 6), name="stay"); B.weld(nd, [(a_ - 0.02, -0.200, H + 0.004), (a_ + 0.02, -0.200, H + 0.004)], 0.005)
        for (a_, L) in ((-0.150, 0.110), (0.020, 0.132), (0.170, 0.090)): B.prism(nd, "metal", [(a_ - 0.055, H + 0.060), (a_ + 0.055, H + 0.060), (a_ + 0.012, H + 0.060 + L)], -0.215, -0.199, ("x", "z", "y"), name="tooth")
        B.prism(nd, "alt", [(-0.210, -0.020), (0.090, -0.006), (0.080, 0.170), (-0.200, 0.160)], H + 0.008, H + 0.018, ("x", "y", "z"), name="patch"); B.weld(nd, [(-0.210, -0.020, H + 0.010), (0.090, -0.006, H + 0.010)], 0.004)
    else:                                                                                    # E: fat rounded bumper, orange lip, one big lamp
        B.hull(nd, "dark", [(u0, -0.135, H + 0.050, 0.110, 0.058, 3.0), ((u0 + u1)/2, -0.135, H + 0.056, 0.116, 0.064, 3.0), (u1, -0.135, H + 0.050, 0.110, 0.058, 3.0)], (20, 10), (3, 1), "x", name="bumper")
        B.bevel(B.box(nd, "base", (u0 + 0.050, -0.190, H + 0.100), (u1 - 0.050, -0.090, H + 0.122), name="lip"), 0.010, (3, 1), 30)
        if not (e and w): B.rev(nd, "glow", spl([(0.0, 0.0, 'c'), (0.052, 0.0, 'c'), (0.052, 0.014), (0.030, 0.026), (0.0, 0.028, 'c')], B.q(4, 2)), (20, 10), "z", (0.0, 0.100, H + 0.020), name="lamp"); B.rev(nd, "dark", [(0.052, 0.0), (0.072, 0.0), (0.072, 0.024), (0.052, 0.024)], (20, 10), "z", (0.0, 0.100, H + 0.018), True, name="lamp_ring")
def lip(B, nd):
    """hull-side wheel fender, canonical: wheel cell at the origin, hull towards -X; never over the tyre (ends at x = -0.168)"""
    arc = lambda zone, r0, r1, a0, a1, x0=-0.262, x1=-0.168, seg=(40, 10), name="fender": B.rev(nd, zone, [(r0, x0), (r1, x0), (r1, x1), (r0, x1)], seg, "x", (0, 0, 0), True, name=name, arc=(a0, a1))
    if KEY == "A": arc("dark", 0.402, 0.410, 22, 158); [B.bevel(arc("base", 0.412, 0.424, a0, a1), 0.003, (2, 0)) for (a0, a1) in ((20, 64), (68, 112), (116, 160))]; arc("glow", 0.404, 0.409, 30, 150, -0.170, -0.1665, name="edge_light")
    elif KEY == "Bo":
        for (a0, a1) in ((28, 68), (70, 110), (112, 152)): B.bevel(arc("base", 0.410, 0.430, a0, a1, seg=(3, 1)), 0.003, (2, 0))
        arc("dark", 0.430, 0.440, 28, 152, -0.262, -0.236, seg=(9, 3), name="rim")
        for yy in (-0.2, 0.2): B.cyl(nd, "metal", (-0.262, yy, 0.15), (-0.20, yy*1.2, 0.36), 0.014, (6, 6), name="stay")
    elif KEY == "Bn":
        B.bevel(arc("base", 0.408, 0.436, 26, 154, seg=(10, 4)), 0.004, (2, 1))
        for ang in (50, 90, 130): rb = B.box(nd, "dark", (-0.262, -0.014, 0.434), (-0.168, 0.014, 0.448), name="rib"); B.move(rb, Matrix.Rotation(math.radians(ang - 90), 4, "X"))
        arc("sec", 0.4365, 0.441, 26, 154, -0.184, -0.168, seg=(10, 4), name="guard")
    elif KEY == "C": B.bevel(arc("base", 0.410, 0.440, 24, 156, seg=(24, 8)), 0.010, (3, 1)); arc("sec", 0.4405, 0.446, 40, 140, -0.236, -0.194, seg=(24, 8), name="band")
    elif KEY == "D":
        for (zone, a0, a1, th) in (("rust", 35, 72, 0.010), ("alt", 76, 110, 0.014)): arc(zone, 0.405, 0.405 + th, a0, a1, seg=(2, 1))
        B.cyl(nd, "metal", (-0.262, 0.10, 0.20), (-0.215, 0.14, 0.39), 0.010, (6, 6), name="stay"); B.weld(nd, [(-0.250, -0.02, 0.418), (-0.180, -0.02, 0.418)], 0.004)
    else: B.bevel(arc("dark", 0.408, 0.420, 14, 166, seg=(28, 8)), 0.004, (2, 0)); B.bevel(arc("base", 0.418, 0.450, 18, 162, -0.256, -0.174, seg=(28, 8)), 0.012, (3, 1))


def kit(B):
    names = []
    for mask in range(16):
        for v in range(NVAR): n = "plate_%02d" % mask + ("_%d" % v if NVAR > 1 else ""); tile(B, n, mask, v); names.append(n)
    gasket(B, "gasket"); lip(B, "lip"); ramp(B, "ramp"); cheek(B, "cheek"); names += ["gasket", "lip", "ramp", "cheek"]
    for m in range(4): nose(B, "nose_%d" % m, m); names.append("nose_%d" % m)
    return names


def main():
    T = dict(key=KEY, style=STYLE, size=SIZE); t = time.time(); bpy.ops.wm.read_factory_settings(use_empty=True); sc = bpy.context.scene; col = sc.collection
    Bh = hf.Builder(True, STYLE, col); names = kit(Bh); hi = hf.finish(Bh); Bl = hf.Builder(False, STYLE, col); kit(Bl); lo = hf.finish(Bl); T["s_build"] = round(time.time() - t, 1); t = time.time()
    T["tris_lo"] = {n: hf.tris(o) for n, o in lo.items()}; T["tris_hi_total"] = sum(hf.tris(o) for v in hi.values() for o in v); T["pieces"] = len(lo)
    PT = 1.2; off = {n: Vector(((i % 8)*PT, (i//8)*PT, 0.0)) for i, n in enumerate(names)}              # spread the pieces: every piece gets its own place in the noise that drives wear, and the sheet bakes in one go
    for n in names:
        for o in hi.get(n, []) + [lo[n]]: o.data.transform(Matrix.Translation(off[n]))
    env = [[-0.6, -0.6, -0.30], [8*PT, (len(names)//8 + 1)*PT, 0.50]]
    hbake.unwrap(lo, margin=0.003); w = bpy.data.worlds.new("w"); sc.world = w; w.light_settings.distance = 0.25                    # AO looks 0.25 m far: the pieces are 1.2 m apart and never shade each other
    bpy.ops.object.select_all(action="DESELECT"); cps = []
    for o in lo.values(): c = o.copy(); c.data = o.data.copy(); col.objects.link(c); c.select_set(True); cps.append(c); o.hide_render = True
    bpy.context.view_layer.objects.active = cps[0]; bpy.ops.object.join(); J = bpy.context.view_layer.objects.active
    HI = {"sheet": [o for v in hi.values() for o in v]}
    if SIZE >= 4096:                    # the sharp maps at full size; ambient occlusion (32 of the 48 samples per texel, and low-frequency) at half size, dilated and enlarged
        P0 = hbake.PASSES; hbake.PASSES = [p for p in P0 if p[0] != "ao"]; maps, tb = hbake.bake(HI, {"sheet": J}, SIZE, env)
        hbake.PASSES = [p for p in P0 if p[0] in ("id", "ao")]; m2, tb2 = hbake.bake(HI, {"sheet": J}, SIZE//2, env); hbake.PASSES = P0
        ao = hbake.dilate({"id": m2["id"], "ao": m2["ao"]}, 8)["ao"]; ao = np.repeat(np.repeat(ao, 2, 0), 2, 1); ao = (ao + np.roll(ao, 1, 0) + np.roll(ao, 1, 1) + np.roll(np.roll(ao, 1, 0), 1, 1))/4
        maps["ao"] = ao.astype(np.float32); tb = dict(tb, ao_half=tb2.get("ao", 0), id_half=tb2.get("id", 0)); T["ao_bake_px"] = SIZE//2
    else: maps, tb = hbake.bake(HI, {"sheet": J}, SIZE, env)
    bpy.data.objects.remove(J, do_unlink=True)
    for o in lo.values(): o.hide_render = False
    T["s_bake"] = round(sum(tb.values()), 1); maps = hbake.dilate(maps, 10); t = time.time()
    fx = {k: v for k, v in D.FX[SK].items() if k not in ("dust", "grime", "stripes", "gradient")}             # direction-dependent wear is left out: one tile is used on side faces and on top faces
    if KEY == "D": fx["rust"] = (0.50,) + tuple(fx["rust"][1:])
    if KEY in ("Bo", "Bn"): fx["rust"] = (0.20,) + tuple(fx["rust"][1:])
    tex = hskin.compose(maps, env, D.PAL[SK], fx, fx.get("seed", 0) + 300); T["s_skin"] = round(time.time() - t, 1); t = time.time()
    for k, srgb in (("base", True), ("orm", False), ("normal", False), ("emission", True)): hbake.save(tex[k], P(LABEL, "hullkit_" + k + ".jpg"), srgb)
    m = bpy.data.materials.new("R4H_HullKit_" + LABEL); m.use_nodes = True; nt = m.node_tree; b = nt.nodes["Principled BSDF"]
    def tx(k, cs): n = nt.nodes.new("ShaderNodeTexImage"); n.image = bpy.data.images.load(P(LABEL, "hullkit_" + k + ".jpg")); n.image.colorspace_settings.name = cs; return n
    nt.links.new(tx("base", "sRGB").outputs[0], b.inputs["Base Color"]); sp = nt.nodes.new("ShaderNodeSeparateColor"); nt.links.new(tx("orm", "Non-Color").outputs[0], sp.inputs[0]); nt.links.new(sp.outputs["Green"], b.inputs["Roughness"]); nt.links.new(sp.outputs["Blue"], b.inputs["Metallic"])
    nm = nt.nodes.new("ShaderNodeNormalMap"); nt.links.new(tx("normal", "Non-Color").outputs[0], nm.inputs["Color"]); nt.links.new(nm.outputs[0], b.inputs["Normal"]); nt.links.new(tx("emission", "sRGB").outputs[0], b.inputs["Emission Color"]); b.inputs["Emission Strength"].default_value = dict(A=3.0, B=2.5, C=2.0, D=2.5, E=2.5)[STYLE]
    clay = bpy.data.materials.new("CLAY"); clay.use_nodes = True; clay.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (0.55, 0.55, 0.55, 1)
    for n, o in lo.items(): o.data.materials.clear(); o.data.materials.append(m); o.name = n; o.data.name = n
    for v in hi.values():
        for o in v: o.data.materials.clear(); o.data.materials.append(clay)
    # preview of the sheet (pieces still spread out), then back to the canonical frames for export
    cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam")); col.objects.link(cam); sc.camera = cam; cam.data.lens = 50
    for (rot, e_, c) in (((math.radians(50), 0, math.radians(35)), 3.6, (1.0, 0.96, 0.9)), ((math.radians(65), 0, math.radians(-140)), 1.2, (0.75, 0.83, 1.0))):
        l = bpy.data.lights.new("sun", "SUN"); l.energy = e_; l.color = c; l.angle = math.radians(6); lo2 = bpy.data.objects.new("sun", l); lo2.rotation_euler = rot; col.objects.link(lo2)
    w.use_nodes = True; w.node_tree.nodes["Background"].inputs[0].default_value = (0.42, 0.44, 0.48, 1)
    eng = [e_.identifier for e_ in bpy.types.RenderSettings.bl_rna.properties['engine'].enum_items]; sc.render.engine = 'BLENDER_EEVEE' if 'BLENDER_EEVEE' in eng else 'BLENDER_EEVEE_NEXT'; sc.view_settings.view_transform = "AgX"
    rows = len(names)//8 + 1; ctr = Vector((3.5*PT, (rows - 1)*PT/2, 0.3)); sc.render.resolution_x, sc.render.resolution_y = 2400, 1500; sc.render.image_settings.file_format = "JPEG"; sc.render.image_settings.quality = 88
    for o in bpy.data.objects:
        if o.type == "MESH": o.hide_render = o.name not in lo
    for (tag, loc) in (("sheet", ctr + Vector((2.6, -(5.0 + rows*0.9), 7.0 + rows*1.1))),): cam.location = loc; cam.rotation_euler = (ctr - loc).to_track_quat("-Z", "Y").to_euler(); sc.render.filepath = P(LABEL, "hullkit_" + tag + ".jpg"); bpy.ops.render.render(write_still=True)
    for n in names:
        for o in hi.get(n, []) + [lo[n]]: o.data.transform(Matrix.Translation(-off[n]))
    hc = bpy.data.collections.new("SOURCE_highpoly"); gc = bpy.data.collections.new("GAME_lowpoly"); [sc.collection.children.link(c) for c in (hc, gc)]
    for v in hi.values():
        for o in v: col.objects.unlink(o); hc.objects.link(o)
    for o in lo.values(): col.objects.unlink(o); gc.objects.link(o)
    for im in bpy.data.images:
        if im.filepath: im.filepath = "//" + os.path.basename(im.filepath)
    bpy.ops.wm.save_as_mainfile(filepath=P(LABEL, "R4H_HullKit_SOURCE.blend"), compress=True)
    bpy.ops.object.select_all(action="DESELECT")
    for o in lo.values(): o.select_set(True)
    bpy.context.view_layer.objects.active = lo[names[0]]
    bpy.ops.export_scene.gltf(filepath=P(LABEL, "R4H_HullKit.glb"), export_format="GLB", use_selection=True, export_apply=True, export_image_format="AUTO", export_animations=False)
    bpy.ops.export_scene.fbx(filepath=P(LABEL, "R4H_HullKit.fbx"), use_selection=True, object_types={"MESH"}, mesh_smooth_type="FACE", path_mode="COPY", embed_textures=False, axis_forward="-Z", axis_up="Y")
    bb = {}
    for n, o in lo.items(): V = np.array([v.co for v in o.data.vertices]); bb[n] = [V.min(0).round(4).tolist(), V.max(0).round(4).tolist()]
    T["bounds"] = bb; T["names"] = names; T["triangles_total"] = sum(T["tris_lo"].values()); T["variants"] = NVAR; T["margin"] = MARGIN; T["s_export"] = round(time.time() - t, 1)
    json.dump(T, open(P(LABEL, "hullkit.json"), "w"), indent=1); print("[HERO4] KIT_DONE", KEY, {k: v for k, v in T.items() if k not in ("bounds", "tris_lo", "names")}, flush=True)


try: main()
except Exception:
    import traceback; traceback.print_exc(); print("[HERO4] KIT_FAILED", KEY, flush=True)
