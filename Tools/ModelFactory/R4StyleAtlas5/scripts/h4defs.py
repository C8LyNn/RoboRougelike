"""#1239 Hero Benchmark Pass 4: NEW models. Recipes for hf.Builder - RUN INSIDE BLENDER. Each recipe is a new modelling iteration that starts from
the direction the owner preferred in survey 1 (Pass 1 = "old", Pass 2 = "new") and changes geometry, silhouette, proportions and mechanisms; the
docstring of a recipe says what the base was and what changed. A tie in the survey gives two different proposals: set "p" (from the old model) and
set "q" (from the new model). Contract unchanged: node names, pivots, node bounds, envelope, ports, mechanism nodes and their animation tracks are
those of the approved asset (templates of Pass 2). Wheel: axis X, outer face +X. Gun: barrel along -Y, pitch pivot (0, 0, 0.135). Battery: 0.52 m cell.
The visible barrel of every gun is still limited to the approved 80 mm outline (node bounds of "Barrel"); the barrel line is carried by a long jacket
on the Pitch node instead (see hero4/README, contract obstacles)."""
import math
from hf import spl, sect, AX
from mathutils import Vector, Matrix
from h2defs import carcass, tread, lugs, chev, rim, plate, foot, backblock, gun_foot, barrel
import h2pal
R, M = "Rotor", "Body"; TAU = 2*math.pi; Y, P_, BR = "Yaw", "Pitch", "Barrel"; BZ = 0.135
def RX(a): return Matrix.Rotation(a, 4, "X")
def RY(a): return Matrix.Rotation(a, 4, "Y")
def RZ(a): return Matrix.Rotation(a, 4, "Z")
def T(*v): return Matrix.Translation(v)
def ngon(r, n, ph=0.0): return [(r*math.cos(ph + TAU*i/n), r*math.sin(ph + TAU*i/n)) for i in range(n)]
def star(r0, r1, n, ph=0.0): return [((r1 if i % 2 == 0 else r0)*math.cos(ph + TAU*i/(2*n)), (r1 if i % 2 == 0 else r0)*math.sin(ph + TAU*i/(2*n))) for i in range(2*n)]
def ring(B, node, zone, r0, r1, t0, t1, seg=(48, 16), axis="x", at=(0, 0, 0), name="ring", phase=0.0, kind="solid"): return B.rev(node, zone, [(r0, t0), (r1, t0), (r1, t1), (r0, t1)], seg, axis, at, True, kind, name, phase=phase)
def disc(B, node, zone, r, t0, t1, seg=(48, 16), axis="x", at=(0, 0, 0), name="disc", phase=0.0, kind="solid"): return B.rev(node, zone, [(0.0, t0), (r, t0), (r, t1), (0.0, t1)], seg, axis, at, False, kind, name, phase=phase)


# ================================================================================================ WHEELS
def wheel_A(B):
    """Sleek, from the Pass 2 wheel (kept: fat tyre, layered hub, light). New: ONE row of directional chevron blocks with shoulder sipes instead of
    two rows of square blocks; five swept turbine blades behind a floating ceramic aero ring; the light is one continuous 16 mm ring (readable at
    36 m) instead of four thin arcs; single-sided faceted fairing with a visible damper; halo and two brake vanes re-cut as flat blades"""
    carcass(B, 0.362, sh=0.040, crown=0.004)
    tread(B, chev(-0.120, 0.166, 0.023, 0.046, 0.022), 0.352, 0.386, 22, 0.0, 0.005)
    lugs(B, 22, 0.318, 0.380, w=0.022, t=(-0.020, 0.020), phase=0.5, bev=0.003)
    rim(B, [(0.0, 0.118, 'c'), (0.046, 0.118, 'c'), (0.058, 0.108, 'c'), (0.150, 0.100, 'c'), (0.166, 0.150, 'c'), (0.212, 0.156, 'c'), (0.222, 0.166, 'c'), (0.228, 0.158, 'c'), (0.214, 0.140, 'c'), (0.208, 0.100, 'c'),
            (0.208, -0.100, 'c'), (0.222, -0.112, 'c'), (0.214, -0.122, 'c'), (0.196, -0.106, 'c'), (0.196, 0.040, 'c'), (0.0, 0.040, 'c')], "dark")
    bl = B.prism(R, "metal", [(-0.012, 0.050), (0.020, 0.050), (0.058, 0.196), (0.018, 0.196)], 0.092, 0.118, ("y", "z", "x"), name="blade"); B.bevel(bl, 0.004, (2, 1)); B.radial([bl], 5, "x")
    for o in B.arcs(R, "base", 0.172, 0.212, 0.150, 0.172, 5, 0.10, phase=18): B.bevel(o, 0.004, (3, 1), 30)                  # floating aero ring, five shells
    ring(B, R, "dark", 0.146, 0.172, 0.140, 0.158, (64, 24), name="lightbed"); ring(B, R, "glow", 0.150, 0.166, 0.156, 0.163, (64, 24), name="light")
    B.rev(R, "base", spl([(0.0, 0.176, 'c'), (0.030, 0.174), (0.052, 0.160), (0.060, 0.142, 'c'), (0.060, 0.118, 'c'), (0.0, 0.118, 'c')], B.q(5, 2)), (48, 16), "x", name="cap")
    ring(B, R, "sec", 0.0605, 0.066, 0.124, 0.134, (48, 16), name="capmark"); B.bolts(R, "bare", 0.104, 5, 0.108, 0.007, 0.006, phase=TAU/10)
    ring(B, R, "bare", 0.070, 0.184, -0.060, -0.048, (64, 24), name="brake")
    ring(B, "Halo", "dark", 0.186, 0.206, -0.140, -0.124, (72, 28), name="halo"); B.arcs("Halo", "glow", 0.190, 0.202, -0.1425, -0.1395, 3, 0.2, seg=(72, 24))
    for (nm, sz) in (("VaneU", 1), ("VaneL", -1)):
        v = B.prism(nm, "base", [(-0.150, 0.0), (-0.060, 0.018), (0.110, 0.018), (0.150, 0.0), (0.110, -0.012), (-0.060, -0.012)], -0.236, -0.196, ("y", "z", "x"), at=(0, 0, sz*0.170), name="vane"); B.bevel(v, 0.004, (2, 1))
    plate(B, "dark")
    f = B.rev(M, "base", [(0.0, -0.240), (0.236, -0.240), (0.236, -0.224), (0.120, -0.168), (0.090, -0.150), (0.0, -0.150)], (8, 8), "x", name="fairing", phase=TAU/16); B.bevel(f, 0.006, (3, 1), 25)
    ring(B, M, "glow", 0.094, 0.104, -0.172, -0.166, (8, 8), name="fairlight", phase=TAU/16)
    B.cyl(M, "metal", (-0.150, 0, 0), (-0.060, 0, 0), 0.050, (32, 14), name="axle")
    B.cyl(M, "bare", (-0.226, -0.160, 0.130), (-0.118, -0.050, 0.020), 0.009, (16, 8), name="damper"); B.cyl(M, "dark", (-0.226, -0.160, 0.130), (-0.176, -0.110, 0.078), 0.021, (20, 10), name="damper_body")
    B.cyl(M, "sec", (-0.176, -0.110, 0.078), (-0.166, -0.100, 0.068), 0.023, (20, 10), name="damper_mark")


def wheel_B_old(B):
    """Brutal, proposal P (from the Pass 1 wheel: blocky lugs, six-spoke rim, bolted hub). New: paddle blocks in two staggered rows that wrap over
    the shoulder; cast six-spoke centre with a bolted bead-lock ring; a planetary hub housing that stands proud of the tyre with six wheel nuts;
    ventilated brake disc with a caliper that clamps; heavy coil-over shock; gusseted carrier plate"""
    carcass(B, 0.350, sh=0.024, crown=0.002, soft=False)
    tread(B, [(-0.140, -0.052), (0.016, -0.052), (0.016, 0.052), (-0.140, 0.052)], 0.342, 0.388, 11, 0.0, 0.006)
    tread(B, [(0.030, -0.052), (0.186, -0.052), (0.186, 0.052), (0.030, 0.052)], 0.342, 0.388, 11, 0.5, 0.006)
    lugs(B, 11, 0.270, 0.384, w=0.030, t=(-0.052, 0.052), bev=0.005)
    rim(B, [(0.0, 0.070, 'c'), (0.090, 0.070, 'c'), (0.100, 0.060, 'c'), (0.196, 0.060, 'c'), (0.206, 0.150, 'c'), (0.226, 0.150, 'c'), (0.226, 0.128, 'c'), (0.214, 0.110, 'c'), (0.214, -0.112, 'c'), (0.226, -0.122, 'c'),
            (0.214, -0.130, 'c'), (0.198, -0.110, 'c'), (0.198, 0.030, 'c'), (0.0, 0.030, 'c')], "dark", (72, 24))
    sp = B.prism(R, "metal", [(-0.030, 0.070), (0.030, 0.070), (0.044, 0.204), (-0.044, 0.204)], 0.066, 0.132, ("y", "z", "x"), name="spoke"); B.bevel(sp, 0.008, (3, 1), 30); B.radial([sp], 6, "x")
    bd = ring(B, R, "base", 0.196, 0.232, 0.142, 0.164, (12, 12), name="beadlock", phase=TAU/24); B.bevel(bd, 0.004, (2, 1), 30); B.bolts(R, "bare", 0.214, 12, 0.164, 0.010, 0.010, kind="solid")
    hb = B.rev(R, "base", [(0.0, 0.184), (0.060, 0.184), (0.072, 0.174), (0.072, 0.150), (0.098, 0.140), (0.098, 0.090), (0.0, 0.090)], (12, 12), "x", name="planetary"); B.bevel(hb, 0.004, (2, 1), 30)
    B.bolts(R, "bare", 0.084, 6, 0.140, 0.013, 0.014, kind="solid", phase=TAU/12); B.cyl(R, "sec", (0.184, 0, 0), (0.190, 0, 0), 0.040, (6, 6), name="hubcap")
    dk = ring(B, R, "bare", 0.070, 0.190, -0.064, -0.046, (48, 16), name="brake")
    for i in range(10): h = B.cyl(R, "bare", (-0.070, 0, 0.150), (-0.040, 0, 0.150), 0.012, (8, 6), kind="cutter"); B.move(h, RX(i*TAU/10)); B.cut(dk, h, lo=False)
    c = B.prism("Caliper", "sec", [(0.110, -0.070), (0.200, -0.060), (0.200, 0.060), (0.110, 0.070)], -0.090, -0.020, ("y", "z", "x"), name="caliper"); B.bevel(c, 0.008, (3, 1), 30); B.bolts("Caliper", "bare", 0.0, 1, -0.020, 0.012, 0.008, at=(0, 0.160, 0.040), kind="solid"); B.bolts("Caliper", "bare", 0.0, 1, -0.020, 0.012, 0.008, at=(0, 0.160, -0.040), kind="solid")
    a, b_ = Vector((-0.238, 0, 0.226)), Vector((-0.090, 0, 0.060)); d = (b_ - a).normalized()
    B.cyl("Shock", "dark", a, a + d*0.100, 0.030, (16, 10), name="shock_body"); B.cyl("Shock", "bare", a + d*0.100, a + d*0.200, 0.014, (12, 8), name="shock_rod"); B.helix("Shock", "sec", a + d*0.050, a + d*0.190, 0.036, 0.008, 4.0, name="spring")
    plate(B, "base", rad=0.006); B.bolts(M, "bare", 0.280, 4, -0.238, 0.016, 0.012, kind="solid", phase=TAU/8)
    for sz in (-1, 1):
        g = B.prism(M, "base", [(-0.238, 0.060), (-0.238, 0.230), (-0.130, 0.060)], -0.012, 0.012, ("x", "z", "y"), name="gusset"); B.move(g, Matrix.Diagonal((1, 1, sz, 1)) @ T(0, sz*0.0, 0))
        if sz < 0 and g is not None: g.data.flip_normals()
    cr = B.rev(M, "base", [(0.0, -0.238), (0.130, -0.238), (0.110, -0.150), (0.080, -0.130), (0.0, -0.130)], (8, 8), "x", name="carrier", phase=TAU/16); B.bevel(cr, 0.006, (2, 1), 30)
    B.cyl(M, "metal", (-0.130, 0, 0), (-0.064, 0, 0), 0.056, (16, 10), name="axle"); B.weld(M, [(-0.236, 0.012, 0.064), (-0.236, 0.012, 0.226)], 0.005)


def wheel_B_new(B):
    """Brutal, proposal Q (from the Pass 2 wheel: mud tyre, yellow flange, geared hub). New: square-shouldered carcass with angled tractor bars
    instead of the soft round tyre; the flange becomes a thick bolted wheel-weight ring with eight cast lugs; eight-lobe cast hub with a locking
    nut; double brake disc; the shock is a twin-tube unit and the mount a forged trailing fork"""
    carcass(B, 0.354, sh=0.022, crown=0.003, soft=False)
    bar = B.prism(R, "rubber", [(-0.146, -0.078), (-0.146, -0.030), (0.018, 0.034), (0.018, -0.014)], 0.340, 0.377, ("x", "y", "z"), name="bar"); B.bevel(bar, 0.006, (2, 1), 40); B.radial([bar], 13, "x")
    bar2 = B.prism(R, "rubber", [(0.192, -0.078), (0.192, -0.030), (0.028, 0.034), (0.028, -0.014)], 0.340, 0.377, ("x", "y", "z"), name="bar"); B.bevel(bar2, 0.006, (2, 1), 40); B.radial([bar2], 13, "x", phase=TAU/26)
    lugs(B, 13, 0.286, 0.376, w=0.024, t=(-0.040, 0.040), bev=0.004)
    rim(B, [(0.0, 0.090, 'c'), (0.100, 0.090, 'c'), (0.112, 0.070, 'c'), (0.170, 0.070, 'c'), (0.186, 0.146, 'c'), (0.226, 0.146, 'c'), (0.226, 0.126, 'c'), (0.214, 0.110, 'c'), (0.214, -0.112, 'c'), (0.226, -0.122, 'c'),
            (0.214, -0.130, 'c'), (0.198, -0.110, 'c'), (0.198, 0.040, 'c'), (0.0, 0.040, 'c')], "dark", (72, 24))
    w = ring(B, R, "sec", 0.176, 0.230, 0.140, 0.168, (16, 16), name="weight", phase=TAU/32); B.bevel(w, 0.005, (2, 1), 30)
    lg = B.prism(R, "sec", [(-0.026, 0.150), (0.026, 0.150), (0.018, 0.186), (-0.018, 0.186)], 0.168, 0.182, ("y", "z", "x"), name="lug"); B.bevel(lg, 0.004, (2, 0)); B.radial([lg], 8, "x"); B.bolts(R, "bare", 0.212, 8, 0.168, 0.011, 0.012, kind="solid", phase=TAU/16)
    hb = B.prism(R, "rust", star(0.078, 0.118, 8), 0.090, 0.150, ("y", "z", "x"), name="hub"); B.bevel(hb, 0.010, (3, 1), 30)
    B.cyl(R, "bare", (0.150, 0, 0), (0.176, 0, 0), 0.046, (6, 6), name="nut"); B.cyl(R, "dark", (0.176, 0, 0), (0.186, 0, 0), 0.022, (12, 8), name="pin"); B.bolts(R, "bare", 0.098, 8, 0.150, 0.008, 0.008, phase=0.0)
    for x0 in (-0.086, -0.050): ring(B, R, "bare", 0.070, 0.188, x0, x0 + 0.014, (48, 16), name="brake")
    c = B.prism("Caliper", "base", [(0.108, -0.074), (0.204, -0.050), (0.204, 0.050), (0.108, 0.074)], -0.100, -0.024, ("y", "z", "x"), name="caliper"); B.bevel(c, 0.008, (3, 1), 30)
    a, b_ = Vector((-0.238, 0, 0.226)), Vector((-0.090, 0, 0.060)); d = (b_ - a).normalized(); s = Vector((0, 0.030, 0))
    for sy in (-1, 1): B.cyl("Shock", "sec", a + s*sy, a + s*sy + d*0.110, 0.022, (12, 8), name="shock_body"); B.cyl("Shock", "bare", a + s*sy + d*0.110, a + s*sy + d*0.200, 0.011, (10, 6), name="shock_rod")
    B.box("Shock", "dark", a + d*0.050 + Vector((-0.014, -0.050, -0.014)), a + d*0.050 + Vector((0.014, 0.050, 0.014)), name="shock_tie")
    plate(B, "dark", rad=0.006); B.bolts(M, "bare", 0.280, 4, -0.238, 0.016, 0.012, kind="solid", phase=TAU/8)
    for sy in (-1, 1):
        fk = B.prism(M, "base", [(-0.238, -0.110), (-0.238, 0.110), (-0.060, 0.050), (-0.040, 0.0), (-0.060, -0.050)], sy*0.070, sy*0.100, ("x", "z", "y"), name="fork"); B.bevel(fk, 0.008, (3, 1), 30)
    B.cyl(M, "metal", (-0.110, -0.106, 0), (-0.110, 0.106, 0), 0.030, (12, 8), name="crosspin"); B.cyl(M, "metal", (-0.130, 0, 0), (-0.090, 0, 0), 0.052, (16, 10), name="axle")


def wheel_C(B):
    """Arcade, from the Pass 2 wheel (kept: navy balloon tyre, orange hub disc, light arcs). New: nine big rounded blocks instead of many small
    ones; a two-step hub - deep orange dish with a thick white inner ring - and two fat light arcs that read at distance; hex cap; one chunky
    swing arm with a round pivot"""
    carcass(B, 0.358, sh=0.050, crown=0.010)
    tread(B, [(-0.128, -0.070), (0.174, -0.070), (0.174, 0.070), (-0.128, 0.070)], 0.350, 0.388, 9, 0.0, 0.016, seg=(3, 1))
    rim(B, [(0.0, 0.110, 'c'), (0.060, 0.110, 'c'), (0.076, 0.094), (0.150, 0.094, 'c'), (0.176, 0.150), (0.214, 0.158, 'c'), (0.226, 0.150, 'c'), (0.214, 0.134, 'c'), (0.214, -0.112, 'c'), (0.198, -0.110, 'c'), (0.198, 0.040, 'c'), (0.0, 0.040, 'c')], "sec", (40, 16))
    wr = ring(B, R, "base", 0.086, 0.132, 0.092, 0.122, (32, 16), name="white"); B.bevel(wr, 0.008, (3, 1), 30)
    B.arcs(R, "glow", 0.150, 0.178, 0.118, 0.132, 2, 0.30, seg=(48, 16), phase=45)
    cp = B.prism(R, "metal", ngon(0.070, 6), 0.110, 0.164, ("y", "z", "x"), name="cap"); B.bevel(cp, 0.014, (3, 1), 30)
    ring(B, R, "dark", 0.070, 0.180, -0.060, -0.044, (32, 12), name="brake")
    ar = B.hull("Arm", "base", [(-0.236, 0, 0.0, 0.110, 0.090, 5.0), (-0.190, 0, 0.0, 0.100, 0.080, 5.0), (-0.130, 0, 0.0, 0.074, 0.064, 4.0), (-0.066, 0, 0.0, 0.066, 0.060, 3.0)], (24, 10), (5, 2), "x", name="arm")
    disc(B, "Arm", "sec", 0.050, 0.064, 0.090, (24, 10), "y", (-0.150, 0, 0.0), name="pivot_l"); disc(B, "Arm", "sec", 0.050, -0.090, -0.064, (24, 10), "y", (-0.150, 0, 0.0), name="pivot_r")
    plate(B, "metal", rad=0.022); pk = B.box(M, "dark", (-0.240, -0.160, -0.160), (-0.212, 0.160, 0.160), name="pad"); B.bevel(pk, 0.020, (3, 1), 30)


def wheel_D(B):
    """Scrap, from the Pass 2 wheel (kept: worn mud tyre, dished steel rim, one striped salvage plate). New, as controlled asymmetry: three steel
    cleats bolted across the tread at uneven angles; a rim with five lightening holes of which one is closed by a welded patch; five studs with one
    missing; a brake drum from another vehicle; leaf-spring pack and a hanging mud flap"""
    carcass(B, 0.352, sh=0.030, crown=0.006)
    tread(B, [(-0.132, -0.036), (0.020, -0.050), (0.020, 0.030), (-0.132, 0.044)], 0.346, 0.380, 14, 0.0, 0.006)
    tread(B, [(0.178, -0.036), (0.030, -0.050), (0.030, 0.030), (0.178, 0.044)], 0.346, 0.380, 14, 0.5, 0.006)
    lugs(B, 14, 0.296, 0.376, w=0.022, t=(-0.034, 0.030), bev=0.004)
    for ang in (0.35, 2.30, 4.55):
        cl = B.box(R, "rust", (-0.150, -0.020, 0.372), (0.192, 0.020, 0.390), name="cleat"); B.bevel(cl, 0.003, (1, 0)); B.move(cl, RX(ang))
        for x in (-0.120, 0.160): bt = B.cyl(R, "bare", (x, 0, 0.386), (x, 0, 0.3895), 0.009, (6, 6), name="cleatbolt"); B.move(bt, RX(ang))
    rm = B.rev(R, "metal", spl([(0.0, 0.086, 'c'), (0.080, 0.086, 'c'), (0.110, 0.098), (0.170, 0.136), (0.206, 0.146, 'c'), (0.226, 0.150, 'c'), (0.226, 0.130, 'c'), (0.214, 0.110, 'c'), (0.214, -0.112, 'c'), (0.198, -0.110, 'c'), (0.198, 0.060, 'c'),
                             (0.150, 0.110), (0.100, 0.076), (0.0, 0.066, 'c')], B.q(4, 2)), (40, 20), "x", name="rim")
    for i in range(4): h = B.cyl(R, "metal", (0.040, 0, 0.146), (0.180, 0, 0.146), 0.030, (12, 8), kind="cutter"); B.move(h, RX((i + 1)*TAU/5)); B.cut(rm, h)
    pt = B.prism(R, "alt2", [(-0.046, 0.104), (0.040, 0.100), (0.050, 0.186), (-0.036, 0.192)], 0.122, 0.128, ("y", "z", "x"), name="rimpatch"); B.move(pt, T(0.0, 0, 0) @ RX(0.0)); B.weld(R, [(0.130, -0.046, 0.104), (0.130, 0.040, 0.100), (0.130, 0.050, 0.186)], 0.004)
    disc(B, R, "rust", 0.066, 0.086, 0.118, (10, 10), name="hub"); [B.move(o, RX(0.3)) for o in B.bolts(R, "bare", 0.046, 5, 0.118, 0.010, 0.016, kind="solid")[:4]]
    B.cyl(R, "rust", (0.118, 0, 0), (0.150, 0, 0), 0.022, (8, 8), name="spindle")
    dr = B.rev(R, "rust", [(0.0, -0.080), (0.150, -0.080), (0.158, -0.070), (0.158, -0.030), (0.0, -0.030)], (20, 12), "x", name="drum"); ring(B, R, "bare", 0.158, 0.164, -0.062, -0.050, (20, 12), name="drumband")
    for i in range(4): B.box("Spring", "rust" if i % 2 else "metal", (-0.200, -0.180 + i*0.022, -0.118 - i*0.010), (-0.140, 0.180 - i*0.022, -0.108 - i*0.010), name="leaf")
    for sy in (-1, 1): B.box("Spring", "bare", (-0.204, sy*0.070 - 0.012, -0.152), (-0.136, sy*0.070 + 0.012, -0.104), name="uclamp")
    fl = B.prism("Flap", "sec", [(0.0, 0.0), (0.090, 0.0), (0.096, -0.170), (0.052, -0.186), (0.004, -0.160)], 0.210, 0.215, ("x", "z", "y"), at=(-0.236, 0, 0.100), name="flap"); B.cyl("Flap", "bare", (-0.240, 0.2125, 0.100), (-0.140, 0.2125, 0.100), 0.007, (8, 6), name="flaprod")
    B.prism(M, "base", [(-0.246, -0.250), (0.240, -0.250), (0.250, 0.210), (0.200, 0.250), (-0.250, 0.240)], -0.260, -0.240, ("y", "z", "x"), name="plate")
    B.prism(M, "alt", [(-0.160, -0.130), (0.150, -0.150), (0.170, 0.120), (-0.140, 0.150)], -0.240, -0.232, ("y", "z", "x"), name="doubler"); B.weld(M, [(-0.232, -0.160, -0.130), (-0.232, 0.150, -0.150), (-0.232, 0.170, 0.120)], 0.005)
    B.cyl(M, "rust", (-0.232, 0, 0), (-0.066, 0, 0), 0.044, (10, 8), name="axle"); B.pipe(M, "rust", [(-0.236, -0.150, 0.180), (-0.170, -0.090, 0.110), (-0.110, -0.020, 0.030)], 0.014, (8, 6), (4, 2), name="brace")
    B.bolts(M, "bare", 0.270, 4, -0.240, 0.014, 0.010, kind="solid", phase=TAU/8 + 0.1)


def wheel_E(B):
    """Heroic Toon, from the Pass 2 wheel (kept: dark balloon tyre, orange hub with white segments). New: bigger balloon with seven fat arrow lugs
    that give a serrated silhouette; the hub is one deep orange bowl with three thick white keys, a navy boss and a raised five-point star;
    oversized bouncy coil and a rounded mount"""
    carcass(B, 0.364, sh=0.064, crown=0.014)
    tread(B, chev(-0.124, 0.170, 0.023, 0.110, 0.034), 0.346, 0.378, 7, 0.0, 0.018, seg=(3, 1))
    lugs(B, 7, 0.300, 0.380, w=0.032, t=(-0.060, 0.060), phase=0.5, bev=0.010)
    rim(B, [(0.0, 0.104, 'c'), (0.070, 0.104, 'c'), (0.096, 0.092), (0.150, 0.110), (0.186, 0.150), (0.216, 0.160, 'c'), (0.228, 0.150, 'c'), (0.214, 0.132, 'c'), (0.214, -0.112, 'c'), (0.198, -0.110, 'c'), (0.198, 0.040, 'c'), (0.0, 0.040, 'c')], "base", (40, 16))
    for o in B.arcs(R, "sec", 0.158, 0.206, 0.140, 0.170, 3, 0.36, seg=(48, 12), phase=30): B.bevel(o, 0.010, (3, 1), 30)
    bs = B.rev(R, "metal", spl([(0.0, 0.150, 'c'), (0.060, 0.150), (0.090, 0.134), (0.096, 0.104, 'c'), (0.0, 0.104, 'c')], B.q(5, 2)), (32, 12), "x", name="boss")
    st = B.prism(R, "sec", star(0.030, 0.074, 5, math.pi/2), 0.148, 0.166, ("y", "z", "x"), name="star"); B.bevel(st, 0.004, (2, 1), 30)
    ring(B, R, "glow", 0.108, 0.126, 0.110, 0.120, (40, 12), name="light")
    ring(B, R, "dark", 0.070, 0.176, -0.060, -0.044, (32, 12), name="brake")
    B.helix("Coil", "sec", (-0.206, 0, 0.196), (-0.110, 0, 0.060), 0.038, 0.012, 3.0, (12, 6), (16, 7), name="coil"); B.cyl("Coil", "bare", (-0.214, 0, 0.206), (-0.110, 0, 0.060), 0.016, (12, 8), name="coil_rod")
    plate(B, "dark", rad=0.030)
    B.hull(M, "base", [(-0.240, 0, 0, 0.190, 0.190, 4.0), (-0.200, 0, 0, 0.160, 0.160, 3.4), (-0.130, 0, 0, 0.092, 0.092, 2.6), (-0.066, 0, 0, 0.070, 0.070, 2.2)], (32, 12), (6, 2), "x", name="mount")
    ring(B, M, "sec", 0.094, 0.110, -0.150, -0.124, (32, 12), name="mountring")


# ================================================================================================ BATTERIES
def battery_A(B):
    """Sleek, from the Pass 1 battery (kept: a horizontal glass capsule with a lit core, lying in a light ceramic cradle). New: the capsule shows
    its five cells - lit glass bands between dark separator rings - instead of one plain tube; layered end caps (dark bulkhead, ceramic cover ring,
    lit contact, two clamps); two faceted saddle arms carry it over a stepped deck; a slotted cage turns around the cells; carry spine on top"""
    C = Vector((0.0, -0.045, 0.035)); foot(B, "dark"); backblock(B, "dark", w=0.130, z1=0.016)
    dk = B.prism(M, "base", [(-0.214, -0.214), (0.214, -0.214), (0.230, -0.150), (0.230, 0.130), (0.170, 0.196), (-0.170, 0.196), (-0.230, 0.130), (-0.230, -0.150)], -0.236, -0.212, ("x", "y", "z"), name="deck"); B.bevel(dk, 0.005, (3, 1), 30)
    for sx in (-1, 1):
        sd = B.prism(M, "base", [(-0.200, -0.212), (0.110, -0.212), (0.130, -0.150), (0.118, -0.050), (0.070, -0.012), (0.030, -0.072), (-0.120, -0.072), (-0.160, -0.012), (-0.208, -0.050), (-0.220, -0.150)], sx*0.094, sx*0.126, ("y", "z", "x"), name="saddle"); B.bevel(sd, 0.005, (3, 1), 30)
        B.prism(M, "dark", [(-0.180, -0.212), (0.090, -0.212), (0.104, -0.150), (0.020, -0.086), (-0.110, -0.086), (-0.196, -0.150)], sx*0.060, sx*0.094, ("y", "z", "x"), name="saddle_core")
        bh = disc(B, M, "dark", 0.124, sx*0.168, sx*0.204, (10, 10), "x", C, name="bulkhead", phase=TAU/20) if sx > 0 else disc(B, M, "dark", 0.124, -0.204, -0.168, (10, 10), "x", C, name="bulkhead", phase=TAU/20); B.bevel(bh, 0.004, (2, 1), 25)
        t0, t1 = (0.204, 0.226) if sx > 0 else (-0.226, -0.204)
        cv = ring(B, M, "base", 0.070, 0.134, t0, t1, (10, 10), "x", C, name="cover", phase=TAU/20); B.bevel(cv, 0.004, (3, 1), 25)
        disc(B, M, "glow", 0.050, min(t0, t1) + 0.004, max(t0, t1) - 0.006, (32, 12), "x", C, name="contact"); ring(B, M, "dark", 0.050, 0.070, t0, t1, (32, 12), "x", C, name="contact_bed")
        for sz in (-1, 1): k = B.box(M, "sec", C + Vector((sx*0.176, -0.022, sz*0.118)), C + Vector((sx*0.214, 0.022, sz*0.146)), name="clamp"); B.bevel(k, 0.004, (2, 1), 30)
    B.rev("Core", "glass", [(0.0, -0.160), (0.078, -0.160), (0.078, 0.160), (0.0, 0.160)], (40, 16), "x", C, name="cells")
    for i in range(6): t = -0.160 + i*0.064; ring(B, "Core", "metal", 0.0785, 0.088, t - 0.008, t + 0.008, (40, 16), "x", C, name="separator")
    for i in range(5): t = -0.128 + i*0.064; ring(B, "Core", "glow", 0.0782, 0.0805, t - 0.004, t + 0.004, (40, 16), "x", C, name="cellmark", kind="detail")
    for t in (-0.096, 0.096): ring(B, "Sleeve", "base", 0.094, 0.104, t - 0.018, t + 0.018, (40, 16), "x", C, name="cage_ring")
    br = B.box("Sleeve", "base", C + Vector((-0.112, -0.012, 0.094)), C + Vector((0.112, 0.012, 0.104)), name="cage_bar"); B.radial([br], 4, "x", C, phase=TAU/8)
    sp = B.prism(M, "base", [(-0.168, 0.0), (-0.150, 0.016), (0.150, 0.016), (0.168, 0.0)], -0.026, 0.026, ("x", "z", "y"), at=(0, -0.045, 0.176), name="spine"); B.bevel(sp, 0.004, (2, 1), 30)
    B.box(M, "glow", (-0.120, -0.047, 0.1925), (0.120, -0.043, 0.1945), name="spinelight"); B.box(M, "glow", (-0.110, -0.2305, -0.229), (0.110, -0.2285, -0.219), name="decklight")


def battery_B(B):
    """Brutal, from the Pass 1 battery (kept: a transformer - dark tank, fan panel, cooling fins, porcelain insulators with a bus bar). New: a
    riveted tank with a sloped top; fin banks on BOTH sides; the fan sits in an armoured octagonal shroud with guard bars and spins; three ribbed
    insulators with a copper bus bar and a contactor that lifts; two lit cell windows; lifting eyes; a cable into the socket"""
    foot(B, "dark"); backblock(B, "dark", w=0.120, z1=0.016)
    tk = B.prism(M, "base", [(-0.170, -0.236), (0.170, -0.236), (0.170, 0.070), (0.130, 0.110), (-0.130, 0.110), (-0.170, 0.070)], -0.200, 0.170, ("x", "z", "y"), name="tank"); B.bevel(tk, 0.006, (3, 1), 30)
    for sx in (-1, 1):
        for i in range(7): B.box(M, "dark", (sx*0.170, -0.176 + i*0.050, -0.214), (sx*0.238, -0.164 + i*0.050, 0.040), name="fin")
        B.box(M, "metal", (sx*0.170, -0.190, 0.040), (sx*0.244, 0.150, 0.056), name="fincap"); B.bolts(M, "bare", 0.0, 1, 0.056, 0.010, 0.006, "z", (sx*0.226, -0.150, 0), kind="solid"); B.bolts(M, "bare", 0.0, 1, 0.056, 0.010, 0.006, "z", (sx*0.226, 0.110, 0), kind="solid")
    sh = B.prism(M, "sec", ngon(0.158, 8, TAU/16), -0.232, -0.200, ("x", "z", "y"), at=(0, 0, -0.055), name="shroud"); B.cut(sh, B.cyl(M, "dark", (0, -0.24, -0.055), (0, -0.19, -0.055), 0.118, (24, 12), kind="cutter")); B.bevel(sh, 0.004, (2, 1), 30)
    disc(B, M, "dark", 0.118, -0.203, -0.199, (24, 12), "y", (0, 0, -0.055), name="fanwell")
    B.bolts(M, "bare", 0.140, 8, -0.232, 0.009, -0.008, "y", (0, 0, -0.055), kind="solid", phase=TAU/16)
    for dz in (-0.060, 0.0, 0.060): B.box(M, "bare", (-0.122, -0.244, -0.055 + dz - 0.005), (0.122, -0.236, -0.055 + dz + 0.005), name="guard")
    bl = B.prism("Fan", "metal", [(-0.016, 0.022), (0.022, 0.030), (0.044, 0.108), (-0.006, 0.108)], -0.226, -0.212, ("x", "z", "y"), at=(0, 0, -0.055), name="blade"); B.radial([bl], 6, "y", (0, 0, -0.055))
    B.cyl("Fan", "bare", (0, -0.230, -0.055), (0, -0.206, -0.055), 0.030, (12, 8), name="fanhub")
    for x in (-0.110, 0.0, 0.110):
        B.rev(M, "alt", spl([(0.0, 0.110, 'c'), (0.036, 0.110, 'c'), (0.030, 0.124), (0.046, 0.134), (0.028, 0.146), (0.044, 0.158), (0.026, 0.170), (0.040, 0.182), (0.022, 0.196), (0.022, 0.214, 'c'), (0.0, 0.214, 'c')], B.q(3, 1)), (16, 8), "z", (x, 0.090, 0), name="insulator")
        B.cyl(M, "bare", (x, 0.090, 0.214), (x, 0.090, 0.236), 0.010, (8, 6), name="stud")
    B.box(M, "alt2", (-0.130, 0.080, 0.224), (0.130, 0.100, 0.236), name="busbar")
    rl = B.box("Relay", "dark", (-0.070, -0.010, 0.110), (0.070, 0.050, 0.170), name="contactor"); B.bevel(rl, 0.005, (2, 1), 30); B.box("Relay", "sec", (-0.050, -0.016, 0.130), (0.050, -0.010, 0.158), name="relay_tag"); B.cyl("Relay", "alt2", (-0.040, 0.020, 0.170), (-0.040, 0.020, 0.186), 0.010, (8, 6), name="relay_post"); B.cyl("Relay", "alt2", (0.040, 0.020, 0.170), (0.040, 0.020, 0.186), 0.010, (8, 6), name="relay_post")
    for x in (-0.086, 0.086): B.box(M, "glass", (x - 0.050, -0.2025, 0.094), (x + 0.050, -0.199, 0.140), name="cellwindow"); fr = B.box(M, "metal", (x - 0.058, -0.206, 0.086), (x + 0.058, -0.200, 0.148), name="windowframe"); B.cut(fr, B.box(M, "metal", (x - 0.050, -0.21, 0.094), (x + 0.050, -0.19, 0.140), kind="cutter"))
    for x in (-0.150, 0.150): B.tor(M, "bare", 0.022, 0.007, (16, 8), (8, 4), "x", (x, -0.140, 0.126), name="eye")
    B.pipe(M, "dark", [(0.110, 0.100, 0.230), (0.150, 0.150, 0.220), (0.160, 0.200, 0.100), (0.110, 0.224, -0.050), (0.090, 0.230, -0.110)], 0.014, (10, 6), (5, 2), name="cable")
    for x in (-0.150, -0.075, 0.075, 0.150): B.bolts(M, "bare", 0.0, 1, 0.170, 0.007, 0.005, "y", (x, 0, 0.040), kind="detail")


def battery_C_old(B):
    """Arcade, proposal P (from the Pass 1 battery: a round orange canister on a stepped base with a cap). New: a much bigger, lower canister that
    fills the cell; a wide lit window band so the charge is visible from every side; a chunky cap with a grab loop that pumps; two big white side
    clamps and a navy collar instead of three small tabs"""
    c = (0.0, -0.050, 0.0); foot(B, "metal", rad=0.030)
    st = B.prism(M, "dark", ngon(0.236, 8, TAU/16), -0.236, -0.176, ("x", "y", "z"), name="step"); B.bevel(st, 0.016, (3, 1), 30)
    B.rev(M, "sec", spl([(0.0, -0.176, 'c'), (0.176, -0.176, 'c'), (0.186, -0.160), (0.186, -0.040, 'c'), (0.170, -0.040, 'c'), (0.170, 0.050, 'c'), (0.186, 0.050, 'c'), (0.186, 0.096), (0.160, 0.130), (0.0, 0.134, 'c')], B.q(4, 2)), (32, 16), "z", c, name="canister")
    ring(B, M, "glass", 0.168, 0.176, -0.036, 0.046, (32, 16), "z", c, name="window"); ring(B, M, "base", 0.1765, 0.190, -0.044, -0.034, (32, 16), "z", c, name="band_lo"); ring(B, M, "base", 0.1765, 0.190, 0.044, 0.054, (32, 16), "z", c, name="band_hi")
    for sx in (-1, 1): k = B.box(M, "base", (sx*0.176, -0.110, -0.130), (sx*0.238, 0.010, 0.086), name="clamp"); B.bevel(k, 0.018, (3, 1), 30)
    cp = B.rev("Cap", "dark", spl([(0.0, 0.134, 'c'), (0.118, 0.134, 'c'), (0.126, 0.144), (0.126, 0.174), (0.100, 0.198), (0.0, 0.202, 'c')], B.q(4, 2)), (24, 12), "z", (0, -0.065, 0), name="cap")
    B.pipe("Cap", "base", [(-0.070, -0.065, 0.190, 'c'), (-0.060, -0.065, 0.226), (0.060, -0.065, 0.226), (0.070, -0.065, 0.190, 'c')], 0.014, (10, 6), (5, 3), name="loop")
    ring(B, "Cap", "glow", 0.1265, 0.131, 0.152, 0.166, (24, 12), "z", (0, -0.065, 0), name="caplight"); backblock(B, "metal", w=0.110, z1=0.020, rad=0.012)


def battery_C_new(B):
    """Arcade, proposal Q (from the Pass 2 battery: a navy cube with windows and orange corners). New: an eight-sided chamfered body instead of a
    sharp cube, 15 % lower so the gun stays the tallest thing on the roof; FOUR full-height chamfered corner posts instead of eight small cubes;
    one large lit window per side; a white lid with a single big handle that pumps"""
    foot(B, "metal", rad=0.030)
    bd = B.prism(M, "dark", [(-0.150, -0.214), (0.150, -0.214), (0.214, -0.150), (0.214, 0.150), (0.150, 0.214), (-0.150, 0.214), (-0.214, 0.150), (-0.214, -0.150)], -0.236, 0.130, ("x", "y", "z"), name="body"); B.bevel(bd, 0.012, (3, 1), 30)
    for i in range(4):
        p = B.prism(M, "sec", [(0.150, 0.236), (0.236, 0.150), (0.236, 0.196), (0.196, 0.236)], -0.236, 0.146, ("x", "y", "z"), name="post"); B.bevel(p, 0.010, (3, 1), 30); B.move(p, RZ(i*TAU/4))
        w = B.box(M, "glass", (-0.110, -0.2175, -0.150), (0.110, -0.213, 0.060), name="window"); B.move(w, RZ(i*TAU/4)); f = B.box(M, "base", (-0.126, -0.222, -0.166), (0.126, -0.214, 0.076), name="frame"); B.cut(f, B.box(M, "base", (-0.110, -0.23, -0.150), (0.110, -0.20, 0.060), kind="cutter")); B.bevel(f, 0.006, (2, 1), 30); B.move(f, RZ(i*TAU/4))
    ld = B.prism("Cap", "base", [(-0.130, -0.186), (0.130, -0.186), (0.186, -0.130), (0.186, 0.130), (0.130, 0.186), (-0.130, 0.186), (-0.186, 0.130), (-0.186, -0.130)], 0.130, 0.166, ("x", "y", "z"), name="lid"); B.bevel(ld, 0.014, (3, 1), 30)
    hd = B.box("Cap", "sec", (-0.090, -0.100, 0.166), (0.090, -0.030, 0.196), name="handle"); B.bevel(hd, 0.012, (3, 1), 30); ring(B, "Cap", "glow", 0.040, 0.056, 0.1665, 0.171, (24, 10), "z", (0, 0.070, 0), name="lidlight"); backblock(B, "metal", w=0.090, z1=0.020, rad=0.012)


def battery_D(B):
    """Scrap, from the Pass 1 battery (kept: salvaged cells in a pipe cage, one colour family - red oxide and rust, no patchwork). New: three
    cells that do not match (a tall cased cell, a ribbed can, a drum on its side) held by steel straps; the cage has one bent post and a sleeved
    repair; jumper leads with a clamp that shakes; a pressure gauge with a live needle; weld beads on every joint"""
    B.prism(M, "base", [(-0.250, -0.250), (0.250, -0.250), (0.250, 0.214), (0.214, 0.250), (-0.250, 0.250)], -0.260, -0.238, ("x", "y", "z"), name="foot")
    c1 = B.box(M, "base", (-0.200, -0.060, -0.238), (-0.030, 0.170, 0.110), name="cell_tall"); B.bevel(c1, 0.006, (2, 1), 30); B.box(M, "rust", (-0.206, -0.066, 0.070), (-0.024, 0.176, 0.086), name="cell_tall_band")
    for (x, y) in ((-0.160, 0.010), (-0.070, 0.100)): B.cyl(M, "bare", (x, y, 0.110), (x, y, 0.140), 0.016, (8, 6), name="terminal")
    c2 = B.prism(M, "alt", [(-0.010, -0.170), (0.180, -0.170), (0.196, -0.150), (0.196, 0.040), (0.180, 0.060), (-0.010, 0.060)], -0.238, 0.030, ("x", "y", "z"), name="can"); B.bevel(c2, 0.008, (2, 1), 30)
    for z in (-0.180, -0.110, -0.040): B.box(M, "alt", (-0.014, -0.174, z - 0.010), (0.200, 0.064, z + 0.010), kind="detail", name="can_rib")
    B.cyl(M, "bare", (0.130, -0.110, 0.030), (0.130, -0.110, 0.062), 0.026, (10, 6), name="can_cap")
    dr = B.rev(M, "rust", spl([(0.0, -0.090, 'c'), (0.078, -0.090, 'c'), (0.084, -0.080), (0.084, 0.080), (0.078, 0.090, 'c'), (0.0, 0.090, 'c')], B.q(3, 1)), (14, 10), "x", (0.090, 0.150, -0.150), name="drum")
    for t in (-0.050, 0.050): ring(B, M, "bare", 0.0845, 0.090, t - 0.008, t + 0.008, (14, 10), "x", (0.090, 0.150, -0.150), name="drumhoop")
    for y in (-0.020, 0.130): B.box(M, "bare", (-0.208, y - 0.014, -0.236), (-0.022, y + 0.014, 0.116), kind="detail", name="strap"); B.box(M, "bare", (-0.206, y - 0.014, 0.110), (-0.024, y + 0.014, 0.115), name="strap_top")
    P = {(-1, -1): (-0.232, -0.232, 0.226), (1, -1): (0.232, -0.232, 0.214), (1, 1): (0.220, 0.232, 0.226), (-1, 1): (-0.232, 0.232, 0.196)}
    for (sx, sy), top in P.items():
        if (sx, sy) == (1, -1): B.pipe(M, "rust", [(sx*0.232, sy*0.232, -0.238), (sx*0.236, sy*0.236, -0.040), (sx*0.206, sy*0.226, 0.100), top], 0.011, (8, 6), (5, 2), name="post_bent")
        else: B.cyl(M, "rust", (sx*0.232, sy*0.232, -0.238), top, 0.011, (8, 6), name="post")
        B.weld(M, [(sx*0.232 - 0.016, sy*0.232, -0.236), (sx*0.232 + 0.016, sy*0.232, -0.236)], 0.005)
    K = [P[(-1, -1)], P[(1, -1)], P[(1, 1)], P[(-1, 1)]]
    for i in range(4): B.cyl(M, "rust" if i != 2 else "metal", K[i], K[(i + 1) % 4], 0.010, (8, 6), name="rail")
    B.cyl(M, "bare", (-0.232, 0.232, 0.060), (-0.232, 0.232, 0.130), 0.018, (8, 6), name="sleeve_repair"); B.cyl(M, "rust", (-0.232, -0.232, -0.060), (0.236, -0.236, -0.060), 0.009, (8, 6), name="midrail")
    B.pipe(M, "dark", [(-0.160, 0.010, 0.136), (-0.150, -0.060, 0.200), (0.020, -0.130, 0.180), (0.130, -0.110, 0.064)], 0.009, (8, 6), (6, 3), name="lead_a"); B.pipe(M, "dark", [(-0.070, 0.100, 0.136), (-0.020, 0.150, 0.170), (0.060, 0.160, -0.040), (0.090, 0.150, -0.066)], 0.009, (8, 6), (6, 3), name="lead_b")
    cl = B.prism("Clamp", "sec", [(-0.028, 0.0), (0.0, 0.030), (0.040, 0.012), (0.046, -0.010), (0.0, -0.020)], -0.012, 0.012, ("x", "z", "y"), at=(-0.170, -0.140, 0.105), name="clamp"); B.cyl("Clamp", "bare", (-0.170, -0.156, 0.105), (-0.170, -0.124, 0.105), 0.008, (6, 6), name="clamp_pin")
    gp = B.box(M, "alt2", (-0.190, -0.248, -0.180), (0.020, -0.240, 0.020), name="gauge_plate"); B.bevel(gp, 0.003, (1, 0)); B.weld(M, [(-0.190, -0.244, 0.020), (0.020, -0.244, 0.020)], 0.004)
    ring(B, M, "bare", 0.056, 0.066, -0.256, -0.248, (16, 10), "y", (-0.085, 0, -0.080), name="gauge_rim"); disc(B, M, "glass", 0.056, -0.251, -0.248, (16, 10), "y", (-0.085, 0, -0.080), name="gauge_face")
    B.prism("Needle", "bare", [(-0.004, -0.010), (0.004, -0.010), (0.002, 0.046), (-0.002, 0.046)], -0.2545, -0.252, ("x", "z", "y"), at=(-0.085, 0, -0.080), name="needle")
    B.weld(M, [(-0.030, -0.060, -0.236), (-0.030, 0.170, -0.236)], 0.005); B.weld(M, [(-0.010, -0.170, -0.236), (0.196, -0.170, -0.236)], 0.005)
    sp = B.box(M, "alt2", (-0.100, 0.236, -0.258), (0.100, 0.260, 0.020), name="socket_plate"); B.bevel(sp, 0.004, (2, 1), 30)                    # the back port: a flat salvaged plate, flush over the contact patch


def battery_E(B):
    """Heroic Toon, from the Pass 2 battery (kept: a lit capsule with a white dome between orange fins). New: the capsule becomes a fat bottle
    with two bold bands and a big neck ring; three thick fins that also read as legs; a round badge with a raised lightning bolt that pulses (the
    bolt is taken from the Pass 1 battery); squat, wide stance"""
    J = (0.0, -0.050, 0.0); foot(B, "dark", rad=0.034)
    B.rev(M, "metal", spl([(0.0, -0.236, 'c'), (0.200, -0.236, 'c'), (0.204, -0.214), (0.170, -0.190), (0.0, -0.186, 'c')], B.q(4, 2)), (32, 12), "z", J, name="dish")
    B.rev("Jug", "glass", spl([(0.0, -0.190, 'c'), (0.120, -0.190, 'c'), (0.156, -0.150), (0.164, -0.040), (0.150, 0.060), (0.100, 0.110, 'c'), (0.0, 0.110, 'c')], B.q(5, 2)), (36, 16), "z", J, name="bottle")
    for (z, w, r) in ((-0.110, 0.022, 0.1640), (0.010, 0.022, 0.1625)): ring(B, "Jug", "sec", r - 0.004, r + 0.010, z - w, z + w, (36, 16), "z", J, name="band")
    B.rev("Jug", "sec", spl([(0.0, 0.110, 'c'), (0.104, 0.110, 'c'), (0.110, 0.130), (0.092, 0.180), (0.050, 0.214), (0.0, 0.222, 'c')], B.q(5, 2)), (32, 14), "z", J, name="dome")
    ring(B, "Jug", "base", 0.100, 0.126, 0.092, 0.122, (32, 14), "z", J, name="neck"); B.rev("Jug", "base", spl([(0.0, 0.214, 'c'), (0.030, 0.214, 'c'), (0.034, 0.226), (0.020, 0.238), (0.0, 0.240, 'c')], B.q(3, 1)), (16, 8), "z", J, name="knob")
    for a in (TAU/4, TAU/4 + TAU/3, TAU/4 - TAU/3):
        f = B.prism(M, "base", [(0.150, -0.236), (0.250, -0.236), (0.252, -0.120), (0.214, 0.060), (0.172, 0.110), (0.168, -0.020)], -0.034, 0.034, ("x", "z", "y"), name="fin"); B.bevel(f, 0.016, (3, 1), 30); B.move(f, T(*J) @ RZ(a))
    bd = disc(B, "Emblem", "base", 0.070, -0.236, -0.216, (24, 12), "y", (0, 0, 0.045), name="badge"); ring(B, "Emblem", "dark", 0.070, 0.084, -0.230, -0.212, (24, 12), "y", (0, 0, 0.045), name="badge_rim")
    bolt = B.prism("Emblem", "glow", [(0.012, 0.050), (-0.030, -0.006), (-0.004, -0.006), (-0.016, -0.050), (0.030, 0.010), (0.004, 0.010)], -0.244, -0.236, ("x", "z", "y"), at=(0, 0, 0.045), name="bolt")
    backblock(B, "dark", w=0.110, z1=0.020, rad=0.012); B.box(M, "glow", (-0.070, 0.2585, -0.200), (0.070, 0.2605, -0.180), name="socketlight", kind="detail")


# ================================================================================================ GUNS
def gun_A(B):
    """Sleek, from the Pass 1 gun (kept: truncated-cone pedestal with a narrow neck, angular wedge receiver, a dark jacket that carries the barrel
    line, continuous light). New: LAYERED armour - ceramic shells stand off a dark frame on the pedestal, the yoke, the cheeks and along the
    jacket; the barrel is a twin-rail emitter with a lit slot, so the weapon reads as one 0.5 m line from breech to muzzle; faceted yoke with
    lit trunnions; light chevrons on the top rail (from the Pass 2 gun and the reference); the phase ring rides the jacket and slides on recoil"""
    gun_foot(B, "dark")
    disc(B, M, "dark", 0.226, -0.238, -0.200, (48, 16), "z", name="plinth"); B.rev(M, "dark", [(0.0, -0.200), (0.206, -0.200), (0.112, -0.050), (0.112, 0.014), (0.0, 0.014)], (48, 16), "z", name="core")
    sk = B.rev(M, "base", [(0.218, -0.236), (0.238, -0.236), (0.238, -0.196), (0.230, -0.186), (0.218, -0.186)], (48, 16), "z", closed=True, name="skirt")
    ring(B, M, "glow", 0.208, 0.222, -0.184, -0.176, (48, 16), "z", name="pedlight")
    cn = B.rev(M, "base", [(0.204, -0.170), (0.218, -0.170), (0.138, -0.054), (0.124, -0.054)], (48, 16), "z", closed=True, name="cone")
    for a in range(3): k = B.box(M, "dark", (0.090, -0.006, -0.176), (0.260, 0.006, -0.040), kind="cutter"); B.move(k, RZ(a*TAU/3 + TAU/12)); B.cut(cn, k, lo=False)
    ring(B, M, "metal", 0.112, 0.128, -0.030, 0.014, (48, 16), "z", name="neckring"); ring(B, M, "glow", 0.1125, 0.118, -0.046, -0.036, (48, 16), "z", name="necklight")
    k = B.box(M, "sec", (-0.030, -0.2395, -0.230), (0.030, -0.236, -0.204), name="tab")
    # yaw: turntable, faceted yoke in two layers, counterweight
    disc(B, Y, "dark", 0.150, 0.018, 0.044, (48, 16), "z", name="turntable"); ring(B, Y, "base", 0.150, 0.160, 0.022, 0.040, (48, 16), "z", name="turnshell")
    for sx in (-1, 1):
        yk = [(0.150, 0.044), (0.168, 0.110), (0.100, 0.200), (-0.050, 0.212), (-0.150, 0.150), (-0.170, 0.044)]
        B.prism(Y, "dark", yk, sx*0.146, sx*0.166, ("y", "z", "x"), name="yoke_core")
        sh = B.prism(Y, "base", [(0.140, 0.060), (0.154, 0.110), (0.094, 0.188), (-0.046, 0.200), (-0.136, 0.144), (-0.154, 0.060)], sx*0.166, sx*0.178, ("y", "z", "x"), name="yoke_shell"); B.bevel(sh, 0.004, (3, 1), 30)
        cap = disc(B, Y, "metal", 0.036, 0.178, 0.190, (8, 8), "x", (0, 0, BZ), name="trunnion") if sx > 0 else disc(B, Y, "metal", 0.036, -0.190, -0.178, (8, 8), "x", (0, 0, BZ), name="trunnion")
        ring(B, Y, "glow", 0.040, 0.048, sx*0.1785 - 0.001, sx*0.1785 + 0.0015, (24, 10), "x", (0, 0, BZ), name="trunnionlight")
        B.prism(Y, "sec", [(0.060, 0.186), (0.086, 0.186), (0.094, 0.176), (0.068, 0.176)], sx*0.178, sx*0.1805, ("y", "z", "x"), name="yoke_mark")
    cw = B.prism(Y, "dark", [(0.086, 0.046), (0.236, 0.046), (0.254, 0.070), (0.236, 0.118), (0.086, 0.140)], -0.118, 0.118, ("y", "z", "x"), name="counterweight"); B.bevel(cw, 0.005, (2, 1), 30)
    cs = B.prism(Y, "base", [(0.110, 0.140), (0.232, 0.120), (0.240, 0.128), (0.110, 0.152)], -0.100, 0.100, ("y", "z", "x"), name="counter_shell"); B.bevel(cs, 0.003, (2, 0)); B.box(Y, "glow", (-0.070, 0.2545, 0.074), (0.070, 0.2568, 0.084), name="tail")
    # pitch: wedge receiver, dark jacket, shells
    B.hull(P_, "dark", [(0.074, 0, 0.152, 0.066, 0.050, 7.0), (0.020, 0, 0.158, 0.100, 0.070, 7.0), (-0.130, 0, 0.158, 0.100, 0.070, 7.0), (-0.200, 0, 0.156, 0.062, 0.058, 5.0)], (24, 12), (6, 2), "y", name="receiver")
    B.rev(P_, "dark", [(0.0, -0.190), (0.052, -0.190), (0.052, -0.400), (0.0, -0.400)], (10, 10), "y", (0, 0, 0.156), name="jacket", phase=TAU/20)
    B.box(P_, "dark", (-0.034, -0.400, 0.090), (0.034, -0.196, 0.112), name="keel")
    rf = B.prism(P_, "base", [(-0.090, 0.064), (0.090, 0.064), (0.106, -0.100), (0.058, -0.196), (-0.058, -0.196), (-0.106, -0.100)], 0.224, 0.235, ("x", "y", "z"), name="roof"); B.bevel(rf, 0.004, (3, 1), 30)
    tr = B.prism(P_, "base", [(-0.034, -0.204), (0.034, -0.204), (0.034, -0.384), (0.022, -0.400), (-0.022, -0.400), (-0.034, -0.384)], 0.210, 0.220, ("x", "y", "z"), name="toprail"); B.bevel(tr, 0.003, (2, 1), 30)
    for i in range(4): B.prism(P_, "glow", [(-0.018, 0.0), (0.0, -0.014), (0.018, 0.0), (0.018, 0.012), (0.0, -0.002), (-0.018, 0.012)], 0.2195, 0.2215, ("x", "y", "z"), at=(0, -0.250 - i*0.034, 0), name="chevron")
    for sx in (-1, 1):
        ck = B.prism(P_, "base", [(0.052, 0.098), (0.040, 0.214), (-0.110, 0.214), (-0.194, 0.176), (-0.194, 0.120), (-0.100, 0.094)], sx*0.104, sx*0.116, ("y", "z", "x"), name="cheek"); B.bevel(ck, 0.004, (3, 1), 30)
        sr = B.prism(P_, "base", [(-0.204, 0.124), (-0.204, 0.188), (-0.376, 0.182), (-0.400, 0.160), (-0.400, 0.124)], sx*0.052, sx*0.062, ("y", "z", "x"), name="siderail"); B.bevel(sr, 0.003, (2, 1), 30)
        B.box(P_, "glow", (sx*0.0615 - 0.001, -0.384, 0.150), (sx*0.0615 + 0.002, -0.216, 0.157), name="railline")
        B.prism(P_, "sec", [(-0.160, 0.198), (-0.184, 0.180), (-0.172, 0.180), (-0.150, 0.198)], sx*0.116, sx*0.1185, ("y", "z", "x"), name="cheek_mark")
        v = B.prism("VentL" if sx < 0 else "VentR", "base", [(0.034, 0.126), (0.034, 0.196), (-0.066, 0.196), (-0.086, 0.160), (-0.066, 0.126)], sx*0.118, sx*0.128, ("y", "z", "x"), name="vent"); B.bevel(v, 0.003, (2, 1), 30)
    r = ring(B, "Ring", "base", 0.058, 0.074, -0.322, -0.278, (10, 10), "y", (0, 0, 0.160), name="phasering", phase=TAU/20); B.bevel(r, 0.003, (2, 1), 30); B.arcs("Ring", "glow", 0.0742, 0.0762, -0.306, -0.294, 5, 0.4, "y", (0, 0, 0.160), (40, 10))
    # barrel: two rails and a lit slot
    for (z0, z1) in ((0.099, 0.124), (0.146, 0.171)):
        rl = B.prism(BR, "metal", [(-0.036, -0.400), (0.036, -0.400), (0.036, -0.596), (0.026, -0.618), (-0.026, -0.618), (-0.036, -0.596)], z0, z1, ("x", "y", "z"), name="rail"); B.bevel(rl, 0.003, (2, 1), 30)
    B.box(BR, "glow", (-0.022, -0.606, 0.127), (0.022, -0.400, 0.143), name="slot"); B.box(BR, "dark", (-0.030, -0.440, 0.124), (0.030, -0.400, 0.146), name="breech_block")
    for y in (-0.470, -0.540): B.box(BR, "base", (-0.039, y - 0.012, 0.0985), (0.039, y + 0.012, 0.1715), name="clampband")


def gun_B(B):
    """Brutal, from the Pass 2 gun (kept: octagonal cast pedestal, toothed slewing ring, trunnion plates, yellow roof armour, muzzle brake). New
    proportions - the mass moves from the pedestal to the weapon: a thick finned barrel jacket with two recoil cylinders runs the full length of
    the cradle, the receiver is a box mantlet with a sloped yellow glacis, the muzzle brake is longer and double-baffled, the trunnion plates are
    thicker with hex bearing caps; ammunition box at the back; the recoil buffer, the bolt handle and the elevation gear are bigger and visible"""
    gun_foot(B, "dark", rad=0.006); B.bolts(M, "bare", 0.310, 4, -0.236, 0.014, 0.010, "z", kind="solid", phase=TAU/8)
    pd = B.rev(M, "base", [(0.0, -0.238), (0.232, -0.238), (0.232, -0.206), (0.176, -0.090), (0.176, -0.040), (0.0, -0.040)], (8, 8), "z", name="pedestal", phase=TAU/16); B.bevel(pd, 0.006, (3, 1), 30)
    g = B.prism(M, "base", [(0.168, -0.206), (0.250, -0.206), (0.250, -0.180), (0.186, -0.060), (0.168, -0.060)], -0.014, 0.014, ("x", "z", "y"), name="gusset"); B.bevel(g, 0.003, (1, 0)); B.radial([g], 8, "z")
    disc(B, M, "metal", 0.186, -0.040, 0.014, (32, 16), "z", name="race"); t = B.box(M, "metal", (0.184, -0.012, -0.034), (0.204, 0.012, 0.010), name="tooth"); B.bevel(t, 0.002, (1, 0)); B.radial([t], 20, "z")
    disc(B, Y, "dark", 0.172, 0.018, 0.046, (16, 16), "z", name="turntable"); B.bolts(Y, "bare", 0.150, 8, 0.046, 0.010, 0.008, "z", kind="solid")
    for sx in (-1, 1):
        tp = B.prism(Y, "base", [(0.150, 0.046), (0.150, 0.130), (0.090, 0.212), (-0.070, 0.212), (-0.160, 0.110), (-0.160, 0.046)], sx*0.142, sx*0.176, ("y", "z", "x"), name="trunnion_plate"); B.bevel(tp, 0.006, (3, 1), 30)
        cp = disc(B, Y, "metal", 0.052, 0.176, 0.190, (6, 6), "x", (0, 0, BZ), name="bearing") if sx > 0 else disc(B, Y, "metal", 0.052, -0.190, -0.176, (6, 6), "x", (0, 0, BZ), name="bearing")
        B.bolts(Y, "bare", 0.070, 6, sx*0.176, 0.008, sx*0.008, "x", (0, 0, BZ), kind="detail")
        rb = B.prism(Y, "base", [(-0.160, 0.046), (-0.186, 0.046), (-0.186, 0.070), (-0.160, 0.100)], sx*0.150 - 0.012, sx*0.150 + 0.012, ("y", "z", "x"), name="plate_rib")
    am = B.box(Y, "sec", (-0.110, 0.130, 0.046), (0.110, 0.252, 0.150), name="ammo_box"); B.bevel(am, 0.006, (3, 1), 30); B.box(Y, "dark", (-0.116, 0.150, 0.146), (0.116, 0.240, 0.160), name="ammo_lid")
    for x in (-0.080, 0.080): B.box(Y, "bare", (x - 0.012, 0.126, 0.090), (x + 0.012, 0.132, 0.156), name="latch")
    gr = disc(B, "Gear", "metal", 0.052, 0.150, 0.180, (12, 12), "x", (0, 0.176, 0.120), name="gear"); gt = B.box("Gear", "metal", (0.150, 0.168, 0.168), (0.180, 0.184, 0.184), name="geartooth"); B.radial([gt], 10, "x", (0, 0.176, 0.120)); B.cyl("Gear", "bare", (0.180, 0.176, 0.120), (0.190, 0.176, 0.120), 0.018, (6, 6), name="gearnut")
    # pitch: mantlet, glacis, jacket, recoil cylinders
    mt = B.box(P_, "base", (-0.110, -0.130, 0.090), (0.110, 0.074, 0.206), name="mantlet"); B.bevel(mt, 0.008, (3, 1), 30)
    gl = B.prism(P_, "sec", [(0.070, 0.206), (0.060, 0.232), (-0.090, 0.232), (-0.180, 0.196), (-0.180, 0.180), (-0.130, 0.206)], -0.118, 0.118, ("y", "z", "x"), name="glacis"); B.bevel(gl, 0.004, (2, 1), 30)
    B.bolts(P_, "bare", 0.0, 1, 0.232, 0.010, 0.004, "z", (-0.094, 0.030, 0), kind="solid"); B.bolts(P_, "bare", 0.0, 1, 0.232, 0.010, 0.004, "z", (0.094, 0.030, 0), kind="solid")
    jk = B.rev(P_, "metal", [(0.0, -0.120), (0.057, -0.120), (0.057, -0.356), (0.048, -0.364), (0.0, -0.364)], (12, 12), "y", (0, 0, 0.150), name="jacket", phase=TAU/24)
    for i in range(6): ring(B, P_, "dark", 0.057, 0.0645, -0.160 - i*0.034, -0.146 - i*0.034, (12, 12), "y", (0, 0, 0.150), name="fin", phase=TAU/24)
    for sx in (-1, 1): B.cyl(P_, "dark", (sx*0.086, -0.130, 0.196), (sx*0.086, -0.300, 0.196), 0.024, (12, 8), name="recoil_cyl"); B.cyl(P_, "bare", (sx*0.086, -0.300, 0.196), (sx*0.086, -0.396, 0.196), 0.012, (10, 6), name="recoil_rod"); B.box(P_, "base", (sx*0.086 - 0.030, -0.402, 0.168), (sx*0.086 + 0.030, -0.384, 0.222), name="rod_yoke")
    bf = B.rev("Buffer", "base", [(0.040, -0.400), (0.066, -0.400), (0.066, -0.372), (0.060, -0.364), (0.040, -0.364)], (8, 8), "y", (0, 0, 0.150), closed=True, name="buffer", phase=TAU/16); B.bevel(bf, 0.004, (2, 1), 30)
    bh = B.box("Bolt", "dark", (0.110, -0.090, 0.150), (0.136, -0.010, 0.180), name="bolt_block"); B.cyl("Bolt", "bare", (0.130, -0.050, 0.165), (0.139, -0.050, 0.165), 0.018, (10, 6), name="bolt_knob")
    barrel(B, [(0.0, -0.100), (0.031, -0.100), (0.031, -0.490), (0.037, -0.496), (0.037, -0.618), (0.022, -0.618), (0.022, -0.560), (0.0, -0.560)], "metal", (12, 12), TAU/24)
    for y in (-0.522, -0.566): pt = B.box(BR, "dark", (-0.0375, y - 0.016, 0.122), (0.0375, y + 0.016, 0.148), name="brakeport")
    ring(B, BR, "sec", 0.0312, 0.034, -0.440, -0.424, (12, 12), "y", (0, 0, BZ), name="barrelmark", phase=TAU/24)


def gun_C(B):
    """Arcade, from the Pass 2 gun (kept: navy octagon pedestal with an orange collar, blue receiver between white ear caps, orange muzzle).
    New: fewer, bigger parts - one fat white barrel sleeve that makes the weapon read as a blaster, a taller chamfered pedestal with a single
    belt, a rounded-box receiver, round ear caps with a lit centre, an orange slide on top that kicks back, a doughnut muzzle"""
    gun_foot(B, "metal", rad=0.030)
    pd = B.rev(M, "dark", [(0.0, -0.238), (0.230, -0.238), (0.230, -0.180), (0.150, -0.050), (0.150, -0.012), (0.0, -0.012)], (8, 8), "z", name="pedestal", phase=TAU/16); B.bevel(pd, 0.016, (3, 1), 30)
    bt = ring(B, M, "sec", 0.146, 0.176, -0.036, 0.014, (8, 8), "z", name="belt", phase=TAU/16); B.bevel(bt, 0.010, (3, 1), 30)
    tt = disc(B, Y, "base", 0.160, 0.018, 0.052, (8, 8), "z", name="turntable", phase=TAU/16); B.bevel(tt, 0.010, (3, 1), 30)
    for sx in (-1, 1):
        yk = B.hull(Y, "base", [(0.110, sx*0.160, 0.110, 0.026, 0.060, 4.0), (0.030, sx*0.160, 0.126, 0.028, 0.082, 4.0), (-0.060, sx*0.160, 0.124, 0.028, 0.078, 4.0), (-0.130, sx*0.160, 0.100, 0.024, 0.050, 4.0)], (20, 10), (5, 2), "y", name="yoke")
        e = B.rev(Y, "base", spl([(0.0, 0.156, 'c'), (0.066, 0.156, 'c'), (0.070, 0.170), (0.054, 0.186), (0.0, 0.190, 'c')], B.q(4, 2)), (24, 12), "x", (0, 0, BZ), name="ear")
        if sx < 0: B.move(e, Matrix.Diagonal((-1, 1, 1, 1))); e is not None and e.data.flip_normals()
        ring(B, Y, "glow", 0.022, 0.040, sx*0.189 - 0.0015, sx*0.189 + 0.0015, (20, 10), "x", (0, 0, BZ), name="earlight")
    bk = B.box(Y, "dark", (-0.100, 0.120, 0.052), (0.100, 0.246, 0.130), name="pack"); B.bevel(bk, 0.018, (3, 1), 30)
    B.hull(P_, "metal", [(0.074, 0, 0.150, 0.080, 0.050, 4.0), (0.030, 0, 0.156, 0.124, 0.068, 5.0), (-0.110, 0, 0.156, 0.124, 0.068, 5.0), (-0.170, 0, 0.150, 0.090, 0.058, 4.0)], (28, 12), (6, 2), "y", name="receiver")
    B.hull(P_, "base", [(-0.160, 0, 0.150, 0.060, 0.050, 3.5), (-0.186, 0, 0.150, 0.080, 0.064, 3.5), (-0.370, 0, 0.150, 0.080, 0.064, 3.5), (-0.398, 0, 0.150, 0.062, 0.050, 3.5)], (24, 12), (4, 2), "y", name="sleeve")
    B.hull(P_, "sec", [(-0.226, 0, 0.150, 0.0855, 0.0648, 3.5), (-0.264, 0, 0.150, 0.0855, 0.0648, 3.5)], (24, 12), (1, 1), "y", name="sleeveband")
    for sx in (-1, 1): B.box(P_, "glow", (sx*0.124 - 0.002, -0.090, 0.142), (sx*0.124 + 0.003, 0.010, 0.166), name="sidelight")
    sl = B.box("Slide", "sec", (-0.070, -0.150, 0.208), (0.070, 0.040, 0.236), name="slide"); B.bevel(sl, 0.012, (3, 1), 30)
    barrel(B, [(0.0, -0.100), (0.030, -0.100), (0.030, -0.540), (0.0, -0.540)], "metal", (16, 10))
    B.tor(BR, "sec", 0.026, 0.011, (24, 12), (12, 6), "y", (0, -0.604, BZ), name="muzzle"); ring(B, BR, "base", 0.0302, 0.036, -0.480, -0.440, (16, 10), "y", (0, 0, BZ), name="stripe")


def gun_D(B):
    """Scrap, from the Pass 1 gun (kept: one colour family - red oxide, brown and rust; a drum pedestal with braces; an open frame receiver; a
    pipe barrel). New, as controlled asymmetry and visible repair: three different leg braces; a doubler plate welded over a crack; the left
    bracket is a tall drilled plate, the right one a short angle with a patch; a hand wheel that turns when aiming; an armour shield on ONE
    side only; a half heat-shield with punched holes; the barrel is two pipes of different bore joined by a clamped sleeve and ends in a welded cage"""
    B.prism(M, "base", [(-0.250, -0.250), (0.250, -0.250), (0.250, 0.206), (0.206, 0.250), (-0.250, 0.250)], -0.260, -0.240, ("x", "y", "z"), name="foot")
    dr = B.rev(M, "base", [(0.0, -0.240), (0.190, -0.240), (0.176, -0.060), (0.0, -0.060)], (6, 6), "z", name="drum", phase=TAU/12); B.bevel(dr, 0.004, (1, 0))
    for (ang, w, top) in ((0.5, 0.012, -0.090), (2.7, 0.020, -0.130), (4.5, 0.012, -0.070)):
        br = B.prism(M, "sec", [(0.170, -0.240), (0.250, -0.240), (0.180, top)], -w, w, ("x", "z", "y"), name="brace"); B.move(br, RZ(ang)); c, s_ = math.cos(ang), math.sin(ang); B.weld(M, [(0.176*c, 0.176*s_, -0.238), (0.178*c, 0.178*s_, top)], 0.005)
    B.prism(M, "alt", [(-0.110, -0.220), (0.090, -0.226), (0.100, -0.100), (-0.090, -0.086)], -0.172, -0.165, ("x", "z", "y"), name="doubler"); B.weld(M, [(-0.110, -0.168, -0.220), (0.090, -0.168, -0.226), (0.100, -0.166, -0.100)], 0.005)
    disc(B, M, "rust", 0.170, -0.060, 0.014, (12, 12), "z", name="race"); B.bolts(M, "bare", 0.150, 7, 0.014, 0.009, -0.006, "z", kind="solid", phase=0.2)
    B.prism(Y, "rust", ngon(0.160, 7, 0.3), 0.018, 0.040, ("x", "y", "z"), name="turntable")
    bl = B.prism(Y, "base", [(0.140, 0.040), (0.150, 0.150), (0.060, 0.216), (-0.080, 0.204), (-0.166, 0.110), (-0.150, 0.040)], -0.176, -0.162, ("y", "z", "x"), name="bracket_l")
    for (y, z) in ((0.080, 0.090), (-0.090, 0.090), (-0.020, 0.170)): h = B.cyl(Y, "base", (-0.19, y, z), (-0.15, y, z), 0.020, (8, 6), kind="cutter"); B.cut(bl, h)
    B.prism(Y, "sec", [(0.150, 0.040), (0.110, 0.170), (-0.040, 0.184), (-0.120, 0.040)], 0.160, 0.172, ("y", "z", "x"), name="bracket_r"); B.box(Y, "sec", (0.100, -0.120, 0.040), (0.172, 0.150, 0.052), name="angle_foot")
    B.prism(Y, "alt", [(0.090, 0.060), (0.070, 0.150), (-0.020, 0.160), (-0.050, 0.070)], 0.172, 0.178, ("y", "z", "x"), name="bracket_patch"); B.weld(Y, [(0.176, 0.090, 0.060), (0.176, 0.070, 0.150), (0.176, -0.020, 0.160)], 0.004)
    for sx in (-1, 1): B.cyl(Y, "bare", (sx*0.160, 0, BZ), (sx*0.190, 0, BZ), 0.022, (6, 6), name="pin"); B.weld(Y, [(sx*0.166, 0.13, 0.042), (sx*0.166, -0.13, 0.042)], 0.004)
    B.box(Y, "rust", (-0.090, 0.110, 0.040), (0.070, 0.236, 0.110), name="toolbox"); B.box(Y, "bare", (-0.096, 0.150, 0.104), (0.076, 0.170, 0.114), name="toolbox_strap")
    ring(B, "Crank", "rust", 0.044, 0.056, -0.196, -0.184, (12, 8), "x", (0, 0.030, 0.120), name="handwheel")
    for a in range(3): sp = B.box("Crank", "rust", (-0.194, 0.030 - 0.005, 0.120), (-0.186, 0.030 + 0.005, 0.166), name="wheelspoke"); B.move(sp, T(0, 0.030, 0.120) @ RX(a*TAU/3) @ T(0, -0.030, -0.120))
    B.cyl("Crank", "bare", (-0.214, 0.030, 0.166), (-0.194, 0.030, 0.166), 0.009, (8, 6), name="handle"); B.cyl("Crank", "bare", (-0.196, 0.030, 0.120), (-0.176, 0.030, 0.120), 0.012, (8, 6), name="wheelhub")
    # pitch: two frame rails, cross plates, breech block, one-sided shield, half heat shield, loose cover
    for sx in (-1, 1): B.box(P_, "base", (sx*0.070 - 0.008, -0.180, 0.096), (sx*0.070 + 0.008, 0.072, 0.200), name="frame_rail")
    for y in (0.050, -0.060, -0.160): B.box(P_, "rust", (-0.078, y - 0.006, 0.100), (0.078, y + 0.006, 0.196), name="cross_plate")
    B.box(P_, "metal", (-0.052, -0.050, 0.108), (0.052, 0.040, 0.176), name="breech"); B.cyl(P_, "bare", (-0.090, -0.004, BZ), (0.090, -0.004, BZ), 0.014, (8, 6), name="breech_pin")
    sd = B.prism(P_, "sec", [(0.060, 0.090), (0.040, 0.226), (-0.150, 0.232), (-0.250, 0.180), (-0.240, 0.100)], 0.096, 0.106, ("y", "z", "x"), name="shield"); B.bevel(sd, 0.002, (1, 0))
    B.prism(P_, "alt2", [(-0.060, 0.120), (-0.050, 0.200), (-0.150, 0.206), (-0.160, 0.130)], 0.106, 0.111, ("y", "z", "x"), name="shield_patch")
    for (y, z) in ((0.030, 0.110), (0.020, 0.210), (-0.130, 0.214), (-0.220, 0.170), (-0.215, 0.116)): B.cyl(P_, "bare", (0.106, y, z), (0.113, y, z), 0.008, (6, 6), name="rivet")
    hs = B.rev(P_, "metal", [(0.046, -0.398), (0.052, -0.398), (0.052, -0.186), (0.046, -0.186)], (12, 12), "y", (0, 0, BZ), True, name="heatshield", arc=(-20, 200))
    for i in range(5):
        h = B.cyl(P_, "metal", (0, -0.212 - i*0.040, BZ), (0, -0.212 - i*0.040, BZ + 0.07), 0.011, (8, 6), kind="cutter"); [B.cut(hs, x, lo=False) for x in B.radial([h], 5, "y", (0, 0, BZ), phase=(i % 2)*TAU/10)]
    for y in (-0.200, -0.384): B.box(P_, "bare", (-0.056, y - 0.006, 0.092), (0.056, y + 0.006, 0.104), name="shield_strap")
    B.weld(P_, [(0.078, -0.160, 0.100), (0.078, -0.160, 0.196)], 0.004); B.weld(P_, [(-0.078, 0.050, 0.100), (-0.078, 0.050, 0.196)], 0.004)
    B.prism("Rattle", "alt", [(-0.080, -0.170), (0.010, -0.176), (0.016, 0.040), (-0.076, 0.046)], 0.204, 0.210, ("x", "y", "z"), at=(0.070, 0, 0), name="cover"); B.cyl("Rattle", "bare", (0.010, -0.150, 0.210), (0.010, -0.150, 0.216), 0.008, (6, 6), name="cover_bolt")
    barrel(B, [(0.0, -0.100), (0.030, -0.100), (0.030, -0.420), (0.0, -0.420)], "rust", (12, 8)); barrel(B, [(0.0, -0.400), (0.022, -0.400), (0.022, -0.600), (0.014, -0.600), (0.014, -0.560), (0.0, -0.560)], "metal", (10, 8), name="barrel_front")
    ring(B, BR, "bare", 0.030, 0.037, -0.448, -0.396, (10, 8), "y", (0, 0, BZ), name="sleeve"); B.box(BR, "bare", (-0.008, -0.440, BZ + 0.034), (0.008, -0.404, BZ + 0.0365), name="sleeve_lug"); B.weld(BR, [(0.0, -0.449, BZ + 0.024), (0.022, -0.449, BZ + 0.010), (0.024, -0.449, BZ - 0.008)], 0.003)
    for a in range(4): bar = B.box(BR, "rust", (0.028, -0.618, BZ - 0.005), (0.036, -0.540, BZ + 0.005), name="cagebar"); B.move(bar, T(0, 0, BZ) @ RY(a*TAU/4 + 0.4) @ T(0, 0, -BZ))
    for y in (-0.614, -0.548): ring(B, BR, "rust", 0.027, 0.037, y - 0.004, y + 0.004, (10, 8), "y", (0, 0, BZ), name="cagehoop")


def gun_E(B):
    """Heroic Toon, from the Pass 2 gun (kept: navy dome base with orange brows, orange head between white ear caps, antenna, navy barrel with an
    orange tip). New, pushed proportions: a squat wide base and a much bigger head; the barrel becomes a blunderbuss - a fat white jacket and a
    flared orange bell that swells on every shot; one big lit visor on the head; fins on the base; a thicker antenna with a ball"""
    gun_foot(B, "dark", rad=0.034)
    B.rev(M, "metal", spl([(0.0, -0.238, 'c'), (0.236, -0.238, 'c'), (0.240, -0.210), (0.214, -0.120), (0.150, -0.040), (0.110, -0.012, 'c'), (0.0, -0.012, 'c')], B.q(5, 2)), (36, 14), "z", name="dome")
    for a in range(4):
        f = B.prism(M, "base", [(0.150, -0.236), (0.254, -0.236), (0.250, -0.180), (0.170, -0.050), (0.130, -0.050)], -0.030, 0.030, ("x", "z", "y"), name="fin"); B.bevel(f, 0.014, (3, 1), 30); B.move(f, RZ(a*TAU/4 + TAU/8))
    cl = ring(B, M, "sec", 0.104, 0.140, -0.030, 0.014, (32, 12), "z", name="collar"); B.bevel(cl, 0.010, (3, 1), 30)
    disc(B, Y, "dark", 0.150, 0.018, 0.050, (32, 12), "z", name="turntable")
    for sx in (-1, 1):
        B.hull(Y, "base", [(0.120, sx*0.158, 0.096, 0.030, 0.046, 3.0), (0.030, sx*0.160, 0.124, 0.030, 0.084, 3.0), (-0.070, sx*0.160, 0.120, 0.030, 0.076, 3.0), (-0.140, sx*0.158, 0.092, 0.026, 0.042, 3.0)], (20, 10), (5, 2), "y", name="shoulder")
        e = B.rev(Y, "sec", spl([(0.0, 0.150, 'c'), (0.072, 0.150, 'c'), (0.080, 0.166), (0.060, 0.186), (0.0, 0.190, 'c')], B.q(4, 2)), (24, 12), "x", (0, 0, BZ), name="ear")
        if sx < 0: B.move(e, Matrix.Diagonal((-1, 1, 1, 1))); e is not None and e.data.flip_normals()
    B.hull(Y, "metal", [(0.110, 0, 0.090, 0.090, 0.038, 3.5), (0.180, 0, 0.096, 0.100, 0.046, 3.5), (0.240, 0, 0.092, 0.070, 0.034, 3.0)], (20, 10), (5, 2), "y", name="pack")
    B.pipe("Antenna", "dark", [(0.050, 0.190, 0.140, 'c'), (0.052, 0.200, 0.190), (0.060, 0.226, 0.214)], 0.008, (8, 6), (4, 2), name="whip"); B.rev("Antenna", "glow", spl([(0.0, -0.016, 'c'), (0.012, -0.012), (0.016, 0.0), (0.012, 0.012), (0.0, 0.016, 'c')], B.q(3, 1)), (12, 8), "z", (0.060, 0.226, 0.214), name="ball")
    B.hull(P_, "base", [(0.076, 0, 0.150, 0.070, 0.050, 3.0), (0.030, 0, 0.160, 0.130, 0.074, 3.4), (-0.100, 0, 0.160, 0.132, 0.074, 3.4), (-0.180, 0, 0.152, 0.092, 0.060, 3.0)], (32, 14), (6, 2), "y", name="head")
    vz = B.hull(P_, "glow", [(-0.060, 0, 0.180, 0.1335, 0.016, 5.0), (-0.130, 0, 0.180, 0.1195, 0.016, 5.0)], (24, 10), (2, 1), "y", name="visor")
    B.box(P_, "sec", (-0.050, -0.120, 0.226), (0.050, 0.040, 0.2355), name="crest")
    B.rev(P_, "sec", spl([(0.0, -0.170, 'c'), (0.056, -0.170, 'c'), (0.064, -0.190), (0.060, -0.290), (0.0, -0.290, 'c')], B.q(4, 2)), (24, 12), "y", (0, 0, 0.150), name="jacket")
    B.rev("Bell", "base", spl([(0.050, -0.280, 'c'), (0.058, -0.280), (0.064, -0.320), (0.076, -0.380), (0.076, -0.398, 'c'), (0.060, -0.398, 'c'), (0.052, -0.340), (0.050, -0.300, 'c')], B.q(4, 2)), (28, 12), "y", (0, 0, 0.160), True, name="bell")
    ring(B, "Bell", "sec", 0.0765, 0.0775, -0.398, -0.384, (28, 12), "y", (0, 0, 0.160), name="bell_lip", kind="detail")
    barrel(B, [(0.0, -0.100), (0.030, -0.100), (0.030, -0.560), (0.037, -0.566), (0.037, -0.618), (0.024, -0.618), (0.024, -0.580), (0.0, -0.580)], "metal", (20, 10))
    ring(B, BR, "base", 0.0302, 0.034, -0.540, -0.520, (20, 10), "y", (0, 0, BZ), name="tip"); ring(B, BR, "sec", 0.0302, 0.034, -0.500, -0.470, (20, 10), "y", (0, 0, BZ), name="stripe")


# ================================================================================================ registry (palettes and wear: h4pal.py)
RECIPES = {("Wheel", "A", "p"): wheel_A, ("Wheel", "B", "p"): wheel_B_old, ("Wheel", "B", "q"): wheel_B_new, ("Wheel", "C", "p"): wheel_C, ("Wheel", "D", "p"): wheel_D, ("Wheel", "E", "p"): wheel_E,
           ("Battery", "A", "p"): battery_A, ("Battery", "B", "p"): battery_B, ("Battery", "C", "p"): battery_C_old, ("Battery", "C", "q"): battery_C_new, ("Battery", "D", "p"): battery_D, ("Battery", "E", "p"): battery_E,
           ("Gun", "A", "p"): gun_A, ("Gun", "B", "p"): gun_B, ("Gun", "C", "p"): gun_C, ("Gun", "D", "p"): gun_D, ("Gun", "E", "p"): gun_E}
from h4pal import PAL, FX, BASELINE, skin_key
