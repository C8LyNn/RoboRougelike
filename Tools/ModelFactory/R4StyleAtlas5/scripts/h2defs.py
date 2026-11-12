"""#1239 Hero Benchmark Pass 2: the 15 authored models (Basic Gun, Wheel, Battery x 5 styles). Recipes for hf.Builder - RUN INSIDE BLENDER.
Every recipe is designed against the owner's reference board of its style (hero2/reference) and against the R4 contract: node names, pivots, node
bounds, envelope and ports are those of the approved asset; the mechanism nodes and their tracks are those of Visual Polish Pass 2 (rdefs.MECH).
Wheel: axis X, outer face at +X. Gun: barrel along -Y at z = 0.135, pitch pivot (0, 0, 0.135). Battery: 0.52 m cell."""
import math, random
from hf import spl, sect, AX
from mathutils import Vector, Matrix
R, M = "Rotor", "Body"; TAU = 2*math.pi


# ------------------------------------------------------------------------------------------------ shared wheel parts
def carcass(B, rt=0.350, rb=0.212, x0=-0.142, x1=0.188, sh=0.032, crown=0.004, soft=True, zone="rubber"):
    xm = (x0 + x1)/2; c = () if soft else ('c',)
    prof = spl([(rb, x0 + 0.026, 'c'), (rb + 0.030, x0 + 0.006), ((rb + rt)/2 + 0.012, x0), (rt - 0.020, x0 + 0.008), (rt, x0 + sh) + c, (rt + crown, xm), (rt, x1 - sh) + c, (rt - 0.020, x1 - 0.008),
                ((rb + rt)/2 + 0.012, x1), (rb + 0.030, x1 - 0.006), (rb, x1 - 0.026, 'c'), (rb - 0.008, x1 - 0.036, 'c'), (rb - 0.008, x0 + 0.036, 'c')], B.q(6, 2))
    return B.rev(R, zone, prof, (128, 40), "x", closed=True, name="carcass")


def tread(B, poly, r0, r1, n, phase=0.0, bev=0.005, zone="rubber", seg=(2, 1)):
    b = B.prism(R, zone, poly, r0, r1, ("x", "y", "z"), name="block"); B.bevel(b, bev, seg, 40); return B.radial([b], n, "x", phase=phase*TAU/n)


def lugs(B, n, r0, r1, x0=-0.142, x1=0.188, w=0.036, t=(-0.034, 0.026), phase=0.0, bev=0.004, zone="rubber"):
    out = []
    for (a, b, ph) in ((x0 - 0.004, x0 + w, phase), (x1 - w, x1 + 0.003, phase + 0.5)):
        l = B.prism(R, zone, [(r0, t[0]*0.8), (r1, t[0]), (r1, t[1]), (r0, t[1]*0.6)], a, b, ("z", "y", "x"), name="lug"); B.bevel(l, bev, (2, 0), 40); out += B.radial([l], n, "x", phase=ph*TAU/n)
    return out


def chev(x0, x1, xm, w, d): return [(x0, -w/2 - d), (xm, -w/2 + d), (x1, -w/2 - d), (x1, w/2 - d), (xm, w/2 + d), (x0, w/2 - d)]


def rim(B, prof, zone="metal", seg=(96, 32), bev=0.0025): r = B.rev(R, zone, spl(prof, B.q(5, 2)), seg, "x", name="rim"); return r


def plate(B, zone="dark", h=0.245, x0=-0.260, x1=-0.238, rad=0.014, node=M):
    p = B.box(node, zone, (x0, -h, -h), (x1, h, h), name="plate"); B.bevel(p, rad, (4, 2), 30); return p


# ------------------------------------------------------------------------------------------------ WHEELS
def wheel_A(B):
    """Sleek: fat tyre with two rows of chamfered blocks; layered hub - dark turned rim, segmented ceramic ring, four light arcs, capped centre;
    ceramic fairing over the mount, orange coil-over, light halo behind the wheel, two brake vanes"""
    carcass(B, 0.364, sh=0.036, crown=0.005)
    for (xa, xb, ph) in ((-0.124, 0.014, 0.0), (0.032, 0.170, 0.5)):
        tread(B, [(xa, -0.044), (xb - 0.020, -0.044), (xb, -0.026), (xb, 0.044), (xa, 0.044)] if ph == 0 else [(xa, -0.044), (xb, -0.044), (xb, 0.044), (xa + 0.020, 0.044), (xa, 0.026)], 0.356, 0.388, 16, ph, 0.005)
    lugs(B, 16, 0.310, 0.384, w=0.026, t=(-0.040, 0.040), bev=0.004)
    rim(B, [(0.0, 0.142, 'c'), (0.040, 0.142, 'c'), (0.052, 0.132), (0.058, 0.112, 'c'), (0.128, 0.104, 'c'), (0.146, 0.150, 'c'), (0.150, 0.162, 'c'), (0.212, 0.162, 'c'), (0.222, 0.172, 'c'), (0.228, 0.164, 'c'),
            (0.214, 0.146, 'c'), (0.208, 0.100, 'c'), (0.208, -0.100, 'c'), (0.222, -0.112, 'c'), (0.214, -0.122, 'c'), (0.196, -0.106, 'c'), (0.196, 0.050, 'c'), (0.0, 0.050, 'c')], "dark")
    for o in B.arcs(R, "base", 0.176, 0.210, 0.160, 0.174, 4, 0.16, phase=45): B.bevel(o, 0.004, (3, 1), 30)                   # ceramic ring segments
    B.arcs(R, "glow", 0.154, 0.168, 0.160, 0.1665, 4, 0.34, phase=45)                                                       # light arcs
    for o in B.arcs(R, "sec", 0.176, 0.210, 0.160, 0.170, 4, 0.86, phase=0): pass                                           # orange keys between the segments
    sp = B.prism(R, "metal", [(-0.014, 0.056), (0.014, 0.056), (0.022, 0.132), (-0.022, 0.132)], 0.100, 0.124, ("y", "z", "x"), name="spoke"); B.bevel(sp, 0.004, (2, 1)); B.radial([sp], 6, "x")
    B.rev(R, "base", spl([(0.0, 0.160, 'c'), (0.026, 0.158), (0.040, 0.150), (0.046, 0.140, 'c'), (0.0, 0.140, 'c')], B.q(5, 2)), (48, 16), "x", name="cap")
    B.rev(R, "glow", [(0.047, 0.1405), (0.053, 0.1405), (0.053, 0.146), (0.047, 0.146)], (48, 16), "x", closed=True, name="capring")
    B.bolts(R, "bare", 0.092, 6, 0.106, 0.008, 0.008, phase=TAU/12)
    B.rev(R, "bare", [(0.060, -0.058), (0.178, -0.058), (0.178, -0.046), (0.060, -0.046)], (64, 24), "x", closed=True, name="disc")
    # mechanisms
    B.rev("Halo", "dark", [(0.186, -0.138), (0.204, -0.138), (0.204, -0.124), (0.186, -0.124)], (72, 28), "x", closed=True, name="halo"); B.arcs("Halo", "glow", 0.190, 0.200, -0.140, -0.1385, 6, 0.3, seg=(72, 24))
    for (nm, sz) in (("VaneU", 1), ("VaneL", -1)):
        B.hull(nm, "base", [(-0.150, -0.238, sz*0.170, 0.004, 0.006, 2.0), (-0.090, -0.215, sz*0.170, 0.018, 0.012, 2.5), (0.090, -0.200, sz*0.170, 0.030, 0.013, 2.5), (0.150, -0.214, sz*0.170, 0.008, 0.008, 2.0)], (20, 8), (5, 2), "y", name="vane")
    # mount
    plate(B, "dark")
    B.hull(M, "base", [(-0.240, 0, 0, 0.200, 0.200, 7.0), (-0.216, 0, 0, 0.176, 0.176, 5.0), (-0.176, 0, 0, 0.100, 0.100, 3.0), (-0.150, 0, 0, 0.082, 0.082, 2.2)], (48, 16), (6, 2), "x", name="fairing")
    B.cyl(M, "metal", (-0.150, 0, 0), (-0.060, 0, 0), 0.050, (32, 14), name="axle")
    for o in B.arcs(M, "glow", 0.086, 0.094, -0.178, -0.172, 3, 0.4, seg=(48, 12)): pass
    B.cyl(M, "bare", (-0.228, -0.168, 0.110), (-0.120, -0.060, 0.016), 0.010, (16, 8), name="damper"); B.cyl(M, "dark", (-0.228, -0.168, 0.110), (-0.180, -0.120, 0.068), 0.022, (20, 10), name="damper_body")
    B.helix(M, "sec", (-0.176, -0.116, 0.065), (-0.126, -0.066, 0.021), 0.024, 0.0055, 4.5, name="spring")
    for (y, z) in ((-0.2, -0.2), (0.2, -0.2), (-0.2, 0.2), (0.2, 0.2)): B.cyl(M, "bare", (-0.238, y, z), (-0.230, y, z), 0.012, (6, 6), "detail")


def wheel_B(B):
    """Brutal: square-shouldered tyre with huge angular blocks; bolted flange rim with a hazard band and a hex hub nut; gusseted knuckle,
    hydraulic strut (moving rod), brake disc with a sliding caliper, hose"""
    carcass(B, 0.360, sh=0.022, crown=0.002, soft=False)
    for (xa, xb, ph, sg) in ((-0.132, 0.012, 0.0, 1), (0.034, 0.178, 0.5, -1)):
        tread(B, [(xa, -0.058), (xb, -0.058 + sg*0.016), (xb, 0.050 + sg*0.016), (xa + 0.03, 0.058), (xa, 0.044)], 0.352, 0.384, 11, ph, 0.006)
    lugs(B, 11, 0.286, 0.382, w=0.040, t=(-0.054, 0.050), bev=0.006)
    rim(B, [(0.0, 0.128, 'c'), (0.072, 0.128, 'c'), (0.080, 0.118, 'c'), (0.080, 0.104, 'c'), (0.122, 0.104, 'c'), (0.134, 0.146, 'c'), (0.150, 0.164, 'c'), (0.214, 0.164, 'c'), (0.226, 0.172, 'c'), (0.228, 0.162, 'c'),
            (0.214, 0.140, 'c'), (0.208, 0.100, 'c'), (0.208, -0.100, 'c'), (0.224, -0.114, 'c'), (0.214, -0.124, 'c'), (0.194, -0.106, 'c'), (0.194, 0.050, 'c'), (0.0, 0.050, 'c')], "base")
    B.rev(R, "sec", [(0.170, 0.1635), (0.198, 0.1635), (0.198, 0.168), (0.170, 0.168)], (72, 28), "x", closed=True, name="band")
    B.bolts(R, "bare", 0.158, 16, 0.164, 0.0085, 0.010, kind="solid"); B.bolts(R, "bare", 0.205, 16, 0.164, 0.006, 0.006, phase=TAU/32)
    n = B.cyl(R, "metal", (0.128, 0, 0), (0.162, 0, 0), 0.048, (6, 6), name="hexnut"); B.bevel(n, 0.004, (2, 1)); B.cyl(R, "bare", (0.160, 0, 0), (0.172, 0, 0), 0.022, (20, 10), name="stub")
    B.bolts(R, "bare", 0.100, 8, 0.104, 0.009, 0.010, kind="solid", phase=TAU/16)
    w = B.prism(R, "base", [(-0.016, 0.086), (0.016, 0.086), (0.016, 0.128), (-0.016, 0.128)], 0.104, 0.150, ("y", "z", "x"), name="web"); B.bevel(w, 0.003, (2, 0)); B.radial([w], 8, "x")
    d = B.rev(R, "bare", [(0.060, -0.062), (0.180, -0.062), (0.180, -0.046), (0.060, -0.046)], (64, 24), "x", closed=True, name="disc")
    h = B.cyl(R, "bare", (-0.07, 0, 0.150), (-0.03, 0, 0.150), 0.008, (10, 6), kind="cutter"); [B.cut(d, x, lo=False) for x in B.radial([h], 10, "x")]
    # mount: thick plate, cast knuckle with gussets
    p = plate(B, "base", rad=0.006); B.bolts(M, "bare", 0.290, 4, -0.238, 0.014, 0.010, kind="solid", phase=TAU/8)
    k = B.hull(M, "base", [(-0.240, 0, 0, 0.150, 0.150, 8.0), (-0.200, 0, 0, 0.120, 0.120, 8.0), (-0.160, 0, 0, 0.080, 0.080, 8.0), (-0.064, 0, 0, 0.060, 0.060, 8.0)], (8, 8), (3, 2), "x", name="knuckle"); B.bevel(k, 0.004, (2, 1), 20)
    for a in range(4):
        g = B.prism(M, "base", [(-0.238, 0.110), (-0.238, 0.215), (-0.150, 0.075)], -0.010, 0.010, ("x", "z", "y"), name="gusset"); B.bevel(g, 0.002, (1, 0)); B.move(g, Matrix.Rotation(a*TAU/4 + TAU/8, 4, "X"))
    B.cyl(M, "dark", (-0.232, 0, 0.048), (-0.186, 0, 0.114), 0.030, (20, 10), name="strut_cyl"); B.cyl(M, "bare", (-0.236, 0, 0.042), (-0.226, 0, 0.056), 0.030, (8, 8), name="strut_cap")
    B.cyl("Shock", "bare", (-0.200, 0, 0.094), (-0.134, 0, 0.189), 0.013, (16, 8), name="rod"); c = B.box("Shock", "metal", (-0.150, -0.022, 0.172), (-0.118, 0.022, 0.212), name="clevis"); B.bevel(c, 0.006, (2, 1))
    c = B.hull("Caliper", "sec", [(-0.118, 0, 0.168, 0.030, 0.020, 8.0), (-0.104, 0, 0.176, 0.060, 0.034, 6.0), (-0.030, 0, 0.176, 0.060, 0.034, 6.0), (-0.016, 0, 0.168, 0.030, 0.020, 8.0)], (12, 8), (3, 2), "x", name="caliper")
    B.move(c, Matrix.Rotation(TAU/4, 4, "X")); B.bolts("Caliper", "bare", 0.0, 1, -0.012, 0.010, 0.006, at=(0, 0.176, 0.0), kind="detail")
    B.pipe(M, "rubber", [(-0.196, 0.020, 0.110), (-0.200, 0.090, 0.130), (-0.180, 0.170, 0.070), (-0.130, 0.186, 0.020)], 0.008, (10, 6), (6, 3), name="hose")
    B.weld(M, [(-0.238, -0.12, 0.12), (-0.238, 0.12, 0.12)], 0.004)


def wheel_C(B):
    """Arcade: round chunky tyre with eight big blocks per row; flat hub - one bold orange ring with two light arcs on a navy disc; simple
    cream mount with a chunky arm"""
    carcass(B, 0.356, sh=0.052, crown=0.010)
    for (xa, xb, ph) in ((-0.118, 0.010, 0.0), (0.036, 0.164, 0.5)): tread(B, [(xa, -0.078), (xb, -0.078), (xb, 0.062), (xa, 0.062)], 0.344, 0.380, 8, ph, 0.012, seg=(3, 1))
    lugs(B, 8, 0.300, 0.378, w=0.032, t=(-0.070, 0.056), bev=0.008)
    rim(B, [(0.0, 0.150, 'c'), (0.084, 0.150, 'c'), (0.096, 0.138, 'c'), (0.100, 0.150, 'c'), (0.110, 0.160, 'c'), (0.214, 0.160, 'c'), (0.222, 0.152, 'c'), (0.210, 0.130, 'c'), (0.208, -0.100, 'c'), (0.218, -0.116, 'c'),
            (0.196, -0.106, 'c'), (0.196, 0.050, 'c'), (0.0, 0.050, 'c')], "metal", (48, 24))
    o = B.rev(R, "sec", [(0.122, 0.158, ), (0.180, 0.158), (0.172, 0.180), (0.130, 0.180)], (48, 24), "x", closed=True, name="ring"); B.bevel(o, 0.004, (2, 1), 20)
    B.arcs(R, "glow", 0.140, 0.162, 0.178, 0.1825, 2, 0.5, seg=(48, 16), phase=20)
    c = B.rev(R, "dark", [(0.0, 0.172, 'c'), (0.050, 0.172, 'c'), (0.066, 0.156, 'c'), (0.066, 0.150, 'c'), (0.0, 0.150, 'c')], (8, 8), "x", name="cap"); B.bevel(c, 0.004, (2, 1))
    a = B.hull("Arm", "dark", [(-0.236, 0, 0, 0.070, 0.070, 5.0), (-0.180, 0, 0, 0.062, 0.062, 4.0), (-0.060, 0, 0, 0.050, 0.050, 4.0)], (8, 8), (3, 2), "x", name="arm"); B.bevel(a, 0.008, (3, 1), 20)
    o = B.rev("Arm", "sec", [(0.066, -0.170), (0.080, -0.170), (0.080, -0.146), (0.066, -0.146)], (8, 8), "x", closed=True, name="collar"); B.bevel(o, 0.003, (2, 0))
    plate(B, "dark", rad=0.03)
    f = B.hull(M, "base", [(-0.240, 0, 0, 0.200, 0.200, 6.0), (-0.214, 0, 0, 0.190, 0.190, 6.0), (-0.196, 0, 0, 0.150, 0.150, 5.0)], (8, 8), (3, 2), "x", name="pad"); B.bevel(f, 0.012, (3, 1), 20)
    for sy in (-1, 1):
        b = B.box(M, "sec", (-0.214, sy*0.110 - 0.036, 0.120), (-0.190, sy*0.110 + 0.036, 0.176), name="tab"); B.bevel(b, 0.008, (3, 1))
    B.box(M, "glow", (-0.2145, -0.050, -0.166), (-0.193, 0.050, -0.136), name="light")


def wheel_D(B):
    """Scrap: worn tyre, every block different, some torn off; rust-pitted steel rim with five holes, a bolted red patch plate over a crack
    and a heavy lug hub; cut-corner mount plate, welded brackets, leaf-spring pack (moving), hanging mud flap (swinging)"""
    rnd = random.Random(41); carcass(B, 0.360, sh=0.030, crown=0.003)
    for (xa, xb, row) in ((-0.130, 0.018, 0), (0.030, 0.178, 1)):
        for i in range(13):
            if rnd.random() < 0.10: continue                                          # torn-off block
            h = rnd.uniform(0.368, 0.384); s0 = rnd.uniform(-0.052, -0.040); s1 = rnd.uniform(0.030, 0.042); sk = rnd.uniform(-0.010, 0.010)
            b = B.prism(R, "rubber", [(xa, s0), (xb, s0 + sk), (xb, s1 + sk), (xa + rnd.uniform(0.0, 0.03), s1)], 0.352, h, ("x", "y", "z"), name="block"); B.bevel(b, 0.007, (2, 0), 40)
            B.move(b, Matrix.Rotation((i + 0.5*row)*TAU/13, 4, "X"))
    lugs(B, 13, 0.304, 0.380, w=0.028, t=(-0.044, 0.036))
    r = rim(B, [(0.0, 0.126, 'c'), (0.060, 0.126, 'c'), (0.064, 0.110, 'c'), (0.100, 0.104), (0.160, 0.112), (0.196, 0.150, 'c'), (0.214, 0.158, 'c'), (0.224, 0.170, 'c'), (0.228, 0.162, 'c'), (0.214, 0.140, 'c'),
                (0.208, 0.100, 'c'), (0.208, -0.100, 'c'), (0.222, -0.112, 'c'), (0.214, -0.122, 'c'), (0.196, -0.106, 'c'), (0.196, 0.050, 'c'), (0.0, 0.050, 'c')], "metal")
    h = B.cyl(R, "metal", (0.0, 0, 0.150), (0.2, 0, 0.150), 0.024, (16, 8), kind="cutter"); [B.cut(r, x) for x in B.radial([h], 5, "x", phase=0.3)]
    pp = B.prism(R, "sec", [(-0.060, 0.092), (0.050, 0.100), (0.074, 0.196), (0.010, 0.214), (-0.066, 0.190)], 0.152, 0.160, ("y", "z", "x"), name="patch"); B.bevel(pp, 0.0015, (1, 0)); B.move(pp, Matrix.Rotation(0.9, 4, "X"))
    for (y, z) in ((-0.045, 0.110), (0.040, 0.116), (0.056, 0.186), (-0.050, 0.180)): bb = B.cyl(R, "bare", (0.158, y, z), (0.168, y, z), 0.008, (6, 6), name="pbolt"); B.move(bb, Matrix.Rotation(0.9, 4, "X"))
    B.cyl(R, "rust", (0.110, 0, 0), (0.150, 0, 0), 0.052, (20, 10), name="hub"); B.cyl(R, "bare", (0.150, 0, 0), (0.166, 0, 0), 0.020, (6, 6), name="nut")
    B.bolts(R, "bare", 0.036, 5, 0.150, 0.008, 0.009, kind="solid"); B.bolts(R, "bare", 0.084, 8, 0.108, 0.009, 0.012, kind="solid", phase=0.2)
    B.rev(R, "rust", [(0.060, -0.060), (0.176, -0.060), (0.176, -0.048), (0.060, -0.048)], (48, 20), "x", closed=True, name="drum")
    # mount
    B.prism(M, "base", [(-0.245, -0.245), (0.200, -0.245), (0.245, -0.190), (0.245, 0.245), (-0.190, 0.245), (-0.245, 0.200)], -0.260, -0.240, ("y", "z", "x"), name="plate")
    B.prism(M, "alt", [(-0.170, -0.100), (0.130, -0.130), (0.150, 0.110), (-0.120, 0.150)], -0.240, -0.232, ("y", "z", "x"), name="doubler")
    B.cyl(M, "rust", (-0.240, 0, 0), (-0.064, 0, 0), 0.046, (20, 10), name="axle"); B.rev(M, "bare", [(0.046, -0.200), (0.070, -0.200), (0.070, -0.180), (0.046, -0.180)], (24, 10), "x", closed=True, name="flange")
    for sy in (-1, 1): B.prism(M, "base", [(-0.240, 0.050), (-0.240, 0.170), (-0.170, 0.050)], sy*0.050 - 0.004, sy*0.050 + 0.004, ("x", "z", "y"), name="bracket")
    B.weld(M, [(-0.240, -0.17, -0.10), (-0.240, 0.13, -0.13)], 0.005); B.weld(M, [(-0.240, 0.15, 0.11), (-0.240, -0.12, 0.15)], 0.005); B.weld(M, [(-0.238, -0.05, 0.05), (-0.238, -0.05, 0.17)], 0.004)
    B.bolts(M, "bare", 0.270, 4, -0.240, 0.013, 0.010, kind="solid", phase=TAU/8)
    for i, (L, z) in enumerate(((0.200, -0.100), (0.160, -0.112), (0.116, -0.124))):
        B.loft("Spring", "bare" if i else "rust", [[(-0.204, y, zz - 0.005), (-0.156, y, zz - 0.005), (-0.156, y, zz + 0.005), (-0.204, y, zz + 0.005)] for (y, zz) in [(-L + 2*L*k/8, z + 0.024*((2*k/8 - 1)**2)) for k in range(9)]], True, name="leaf")
    for y in (-0.05, 0.05): B.box("Spring", "bare", (-0.208, y - 0.008, -0.132), (-0.152, y + 0.008, -0.090), name="ubolt")
    B.cyl(M, "rust", (-0.250, 0.218, 0.100), (-0.130, 0.218, 0.100), 0.008, (8, 6), name="flap_bar")
    B.loft("Flap", "rubber", [[(-0.246, 0.214 + 0.012*math.sin(k*0.9), 0.100 - 0.26*k/6), (-0.136, 0.214 + 0.012*math.sin(k*0.9 + 0.5), 0.100 - 0.26*k/6 - 0.012*(k/6)), (-0.136, 0.220 + 0.012*math.sin(k*0.9 + 0.5), 0.100 - 0.26*k/6 - 0.012*(k/6)), (-0.246, 0.220 + 0.012*math.sin(k*0.9), 0.100 - 0.26*k/6)] for k in range(7)], True, name="flap")


def wheel_E(B):
    """Heroic Toon: fat rounded tyre with twelve chevrons; hub as a face - three bold orange ring segments, cream inner arcs, navy disc with a
    raised emblem, light arcs; soft cream fairing, orange coil spring (squashing)"""
    carcass(B, 0.356, sh=0.054, crown=0.012)
    c = B.prism(R, "rubber", chev(-0.122, 0.168, 0.023, 0.104, 0.030), 0.344, 0.383, ("x", "y", "z"), name="chev"); B.bevel(c, 0.012, (3, 1), 30); B.radial([c], 12, "x")
    rim(B, [(0.0, 0.140, 'c'), (0.090, 0.140, 'c'), (0.104, 0.150), (0.120, 0.158, 'c'), (0.212, 0.158, 'c'), (0.224, 0.150), (0.214, 0.130, 'c'), (0.208, -0.100, 'c'), (0.218, -0.116, 'c'), (0.196, -0.106, 'c'),
            (0.196, 0.050, 'c'), (0.0, 0.050, 'c')], "dark", (64, 24))
    for o in B.arcs(R, "base", 0.150, 0.206, 0.154, 0.182, 3, 0.20, seg=(60, 18), phase=30): B.bevel(o, 0.010, (3, 1), 30)
    for o in B.arcs(R, "sec", 0.112, 0.142, 0.154, 0.172, 3, 0.45, seg=(60, 18), phase=90): B.bevel(o, 0.006, (3, 1), 30)
    B.arcs(R, "glow", 0.166, 0.190, 0.181, 0.1845, 3, 0.62, seg=(60, 18), phase=30)
    B.rev(R, "metal", spl([(0.0, 0.176, 'c'), (0.060, 0.174), (0.088, 0.160), (0.096, 0.140, 'c'), (0.0, 0.140, 'c')], B.q(5, 2)), (40, 16), "x", name="dome")
    for (y0, y1, z0, z1) in ((-0.040, 0.004, -0.030, 0.034), (0.010, 0.046, -0.006, 0.034)):
        e = B.prism(R, "sec", [(y0, z0), (y1 - 0.014, z0), (y1, z1), (y0 + 0.014, z1)], 0.170, 0.182, ("y", "z", "x"), name="emblem"); B.bevel(e, 0.003, (2, 0))
    B.helix("Coil", "base", (-0.130, 0, 0.060), (-0.130, 0, 0.184), 0.030, 0.0085, 3.5, name="coil")
    B.cyl(M, "metal", (-0.130, 0, 0.040), (-0.130, 0, 0.196), 0.012, (16, 8), name="damper")
    plate(B, "dark", rad=0.04)
    B.hull(M, "sec", [(-0.240, 0, 0.0, 0.205, 0.205, 5.0), (-0.214, 0, 0.0, 0.190, 0.190, 4.0), (-0.180, 0, 0.0, 0.120, 0.110, 3.0), (-0.150, 0, 0.0, 0.085, 0.080, 2.4)], (40, 14), (6, 2), "x", name="fairing")
    B.hull(M, "dark", [(-0.160, 0, 0, 0.070, 0.070, 2.5), (-0.060, 0, 0, 0.058, 0.058, 2.2)], (24, 10), (3, 2), "x", name="axle")
    o = B.hull(M, "base", [(-0.214, 0, 0.170, 0.070, 0.022, 4.0), (-0.160, 0, 0.196, 0.060, 0.024, 3.0), (-0.100, 0, 0.200, 0.040, 0.014, 3.0)], (20, 8), (4, 2), "x", name="brow")
    B.box(M, "glow", (-0.2155, -0.060, -0.168), (-0.200, 0.060, -0.140), name="light")


# ------------------------------------------------------------------------------------------------ BATTERIES (0.52 m cell; ports: bottom full, back partial)
def foot(B, zone="dark", rad=0.016, h=0.245, z1=-0.236):
    p = B.box(M, zone, (-h, -h, -0.260), (h, h, z1), name="foot"); B.bevel(p, rad, (4, 2), 30); return p
def backblock(B, zone="dark", w=0.160, z1=0.030, y0=0.200, rad=0.008):
    p = B.box(M, zone, (-w, y0, -0.258), (w, 0.260, z1), name="socket"); B.bevel(p, rad, (3, 1), 30); return p


def battery_A(B):
    """Sleek: a horizontal energy cell in a cradle - glowing core tube behind glass (pulsing), a slotted sleeve turning around it, octagonal
    bulkheads, ceramic top shell with a carry handle, orange corner brackets, light strip on the socket"""
    C = Vector((0.0, -0.045, 0.035)); foot(B, "dark"); backblock(B, "dark")
    for sx in (-1, 1):
        b = B.rev(M, "dark", [(0.0, 0.176), (0.170, 0.176), (0.196, 0.200), (0.196, 0.236), (0.0, 0.236)] if sx > 0 else [(0.0, -0.236), (0.196, -0.236), (0.196, -0.200), (0.170, -0.176), (0.0, -0.176)], (8, 8), "x", C, name="bulkhead", phase=TAU/16); B.bevel(b, 0.004, (2, 1), 25)
        o = B.rev(M, "base", [(0.090, sx*0.236), (0.150, sx*0.236), (0.140, sx*0.250), (0.100, sx*0.250)], (8, 8), "x", C, True, name="cap", phase=TAU/16); B.bevel(o, 0.003, (2, 0), 25)
        B.rev(M, "glow", [(0.062, sx*0.2365), (0.078, sx*0.2365), (0.078, sx*0.243), (0.062, sx*0.243)], (32, 12), "x", C, True, name="capglow")
    B.rev("Core", "glass", spl([(0.0, -0.172, 'c'), (0.066, -0.172, 'c'), (0.082, -0.156), (0.082, 0.156), (0.066, 0.172, 'c'), (0.0, 0.172, 'c')], B.q(4, 2)), (40, 16), "x", C, name="core")
    for t in (-0.059, 0.059): B.rev("Core", "glow", [(0.0825, t - 0.004), (0.085, t - 0.004), (0.085, t + 0.004), (0.0825, t + 0.004)], (40, 16), "x", C, True, name="corering")
    for t in (-0.118, 0.0, 0.118):
        s = B.rev("Sleeve", "metal", [(0.092, t - 0.026), (0.106, t - 0.026), (0.106, t + 0.026), (0.092, t + 0.026)], (40, 16), "x", C, True, name="sleeve")
        k = B.box("Sleeve", "metal", C + Vector((t - 0.014, -0.02, 0.080)), C + Vector((t + 0.014, 0.02, 0.120)), kind="cutter"); [B.cut(s, x, lo=False) for x in B.radial([k], 8, "x", C)]
    B.arcs("Sleeve", "sec", 0.1065, 0.111, -0.008, 0.008, 4, 0.5, "x", C, (40, 12))
    for (yy, zz) in ((-0.21, 0.19), (0.12, 0.19), (-0.21, -0.12), (0.12, -0.12)): B.cyl(M, "metal", (-0.180, yy, zz), (0.180, yy, zz), 0.012, (12, 6), name="rail")
    top = B.hull(M, "base", [(-0.176, -0.045, 0.214, 0.150, 0.016, 3.0), (-0.090, -0.045, 0.232, 0.168, 0.022, 3.0), (0.090, -0.045, 0.232, 0.168, 0.022, 3.0), (0.176, -0.045, 0.214, 0.150, 0.016, 3.0)], (28, 10), (5, 2), "x", name="shell")
    B.pipe(M, "dark", [(-0.092, -0.045, 0.232, 'c'), (-0.076, -0.045, 0.248), (0.076, -0.045, 0.248), (0.092, -0.045, 0.232, 'c')], 0.008, (10, 6), (5, 3), name="handle")
    for sx in (-1, 1):
        for sy in (-1, 1):
            k = B.prism(M, "sec", [(0.0, 0.0), (0.060, 0.0), (0.060, 0.016), (0.016, 0.016), (0.016, 0.060), (0.0, 0.060)], -0.236, -0.180, ("x", "y", "z"), at=(0, 0, 0), name="bracket"); B.bevel(k, 0.003, (2, 0))
            B.move(k, Matrix.Translation((sx*0.246, sy*0.246, 0)) @ Matrix.Diagonal((-sx, -sy, 1, 1)))
            if sx*sy < 0 and k is not None: k.data.flip_normals()
    B.box(M, "glow", (-0.110, -0.2465, -0.226), (0.110, -0.240, -0.212), name="strip")


def battery_B(B):
    """Brutal: a generator block - cast housing with cooling ribs, fan in a guarded shroud (spinning), two amber cells in cages, a contactor
    that lifts when it switches, tubular roll cage, corrugated hoses, bolted lid"""
    foot(B, "base", 0.006); backblock(B, "base")
    h = B.box(M, "base", (-0.200, -0.190, -0.236), (0.200, 0.200, 0.060), name="housing"); B.bevel(h, 0.010, (3, 1), 30)
    for i in range(7): r = B.box(M, "dark", (-0.206, -0.150 + i*0.050, -0.200), (0.206, -0.136 + i*0.050, 0.030), name="rib"); B.bevel(r, 0.003, (2, 0))
    s = B.rev(M, "dark", [(0.118, -0.238), (0.146, -0.238), (0.146, -0.188), (0.118, -0.188)], (10, 10), "y", (0, 0, -0.055), True, name="shroud"); B.bevel(s, 0.004, (2, 1))
    for a in range(3): B.cyl(M, "bare", (0.130*math.cos(a*TAU/3 + 0.5), -0.244, -0.055 + 0.130*math.sin(a*TAU/3 + 0.5)), (-0.130*math.cos(a*TAU/3 + 0.5), -0.244, -0.055 - 0.130*math.sin(a*TAU/3 + 0.5)), 0.005, (8, 6), name="guard")
    B.cyl("Fan", "bare", (0, -0.236, -0.055), (0, -0.196, -0.055), 0.030, (16, 8), name="fanhub")
    bl = B.prism("Fan", "metal", [(0.024, -0.016), (0.110, -0.036), (0.114, 0.020), (0.024, 0.014)], -0.004, 0.004, ("x", "z", "y"), at=(0, -0.214, -0.055), name="blade")
    if bl is not None: bl.data.transform(Matrix.Translation((0, -0.214, -0.055)) @ Matrix.Rotation(0.5, 4, "X") @ Matrix.Translation((0, 0.214, 0.055)))
    B.radial([bl], 5, "y", (0, 0, -0.055))
    lid = B.box(M, "sec", (-0.190, -0.180, 0.060), (0.190, 0.190, 0.082), name="lid"); B.bevel(lid, 0.006, (2, 1))
    for (x, y) in ((-0.17, -0.16), (0.17, -0.16), (-0.17, 0.17), (0.17, 0.17), (0, -0.16), (0, 0.17)): B.cyl(M, "bare", (x, y, 0.082), (x, y, 0.092), 0.011, (6, 6), name="lidbolt")
    for sx in (-1, 1):
        B.rev(M, "glass", [(0.040, 0.094), (0.046, 0.094), (0.046, 0.214), (0.040, 0.214)], (20, 10), "z", (sx*0.120, -0.050, 0), True, name="cell")
        for zz in (0.082, 0.212): c = B.rev(M, "dark", [(0.0, zz), (0.056, zz), (0.056, zz + 0.020), (0.0, zz + 0.020)], (8, 8), "z", (sx*0.120, -0.050, 0), name="cellcap"); B.bevel(c, 0.003, (2, 0))
        for a in range(4): B.cyl(M, "bare", (sx*0.120 + 0.052*math.cos(a*TAU/4 + 0.78), -0.050 + 0.052*math.sin(a*TAU/4 + 0.78), 0.096), (sx*0.120 + 0.052*math.cos(a*TAU/4 + 0.78), -0.050 + 0.052*math.sin(a*TAU/4 + 0.78), 0.214), 0.005, (6, 6), name="cellbar")
        B.pipe(M, "sec", [(sx*0.226, -0.226, -0.236, 'c'), (sx*0.226, -0.226, 0.196), (sx*0.226, -0.186, 0.238), (sx*0.226, 0.186, 0.238), (sx*0.226, 0.226, 0.196), (sx*0.226, 0.226, -0.236, 'c')], 0.014, (10, 6), (5, 2), name="cage")
        B.pipe(M, "rubber", [(sx*0.120, 0.006, 0.222), (sx*0.120, 0.070, 0.222), (sx*0.100, 0.150, 0.170), (sx*0.090, 0.180, 0.090)], 0.011, (10, 6), (6, 3), name="hose", rfn=(lambda t: 1.0 + 0.10*math.sin(t*60)) if B.hi else None)
    B.cyl(M, "sec", (-0.226, 0, 0.238), (0.226, 0, 0.238), 0.012, (10, 6), name="crossbar")
    r = B.box("Relay", "sec", (-0.050, -0.020, 0.084), (0.050, 0.080, 0.150), name="relay"); B.bevel(r, 0.006, (2, 1)); B.cyl("Relay", "bare", (-0.026, 0.030, 0.150), (-0.026, 0.030, 0.172), 0.012, (6, 6), name="term"); B.cyl("Relay", "bare", (0.026, 0.030, 0.150), (0.026, 0.030, 0.172), 0.012, (6, 6), name="term")
    B.box(M, "glow", (-0.030, -0.1915, -0.020), (0.030, -0.186, 0.004), name="lamp")


def battery_C(B):
    """Arcade: one friendly canister - navy body with a big cyan window, orange corner caps, cream side pads, bobbing lid"""
    foot(B, "dark", 0.04); backblock(B, "dark", rad=0.02)
    b = B.hull(M, "dark", [(-0.236, 0, 0, 0.215, 0.215, 6.0), (-0.200, 0, 0, 0.225, 0.225, 6.0), (0.130, 0, 0, 0.225, 0.225, 6.0), (0.170, 0, 0, 0.200, 0.200, 6.0)], (8, 8), (3, 2), "z", name="body"); B.bevel(b, 0.012, (3, 1), 25)
    w = B.box(M, "glow", (-0.130, -0.232, -0.120), (0.130, -0.214, 0.080), name="window"); B.bevel(w, 0.016, (3, 1), 25)
    f = B.box(M, "metal", (-0.156, -0.229, -0.146), (0.156, -0.212, 0.106), name="frame"); B.bevel(f, 0.02, (3, 1), 25)
    for sx in (-1, 1):
        for sy in (-1, 1):
            for (z0, z1) in ((-0.236, -0.150), (0.090, 0.176)):
                c = B.box(M, "sec", (sx*0.150, sy*0.150, z0), (sx*0.246, sy*0.246, z1), name="corner"); B.bevel(c, 0.014, (3, 1), 25)
        p = B.box(M, "base", (sx*0.222, -0.110, -0.110), (sx*0.242, 0.110, 0.060), name="pad"); B.bevel(p, 0.012, (3, 1), 25)
    l = B.hull("Cap", "base", [(0.176, 0, -0.065, 0.170, 0.150, 5.0), (0.208, 0, -0.065, 0.160, 0.140, 5.0), (0.228, 0, -0.065, 0.110, 0.090, 4.0)], (8, 8), (3, 2), "z", name="lid"); B.bevel(l, 0.008, (3, 1), 25)
    k = B.box("Cap", "sec", (-0.060, -0.105, 0.224), (0.060, -0.025, 0.244), name="grip"); B.bevel(k, 0.008, (3, 1), 25)


def battery_D(B):
    """Scrap: a salvaged power pack - angle-iron frame, a dented fuel-cell drum and two amber valves under a bar cage, radiator from another
    machine, bent exhaust pipe, pressure gauge with a twitching needle, a jump-lead clamp hanging off the terminal, patch plates and welds"""
    B.prism(M, "base", [(-0.245, -0.245), (0.245, -0.245), (0.245, 0.200), (0.200, 0.245), (-0.245, 0.245)], -0.260, -0.240, ("x", "y", "z"), name="foot"); backblock(B, "rust", rad=0.002)
    for (x, y) in ((-0.236, -0.236), (0.236, -0.236), (-0.236, 0.236), (0.236, 0.236)):
        sx, sy = (1 if x < 0 else -1), (1 if y < 0 else -1); B.prism(M, "rust", [(0, 0), (sx*0.040, 0), (sx*0.040, sy*0.006), (sx*0.006, sy*0.006), (sx*0.006, sy*0.040), (0, sy*0.040)], -0.240, 0.220 if (x, y) != (0.236, 0.236) else 0.170, ("x", "y", "z"), at=(x, y, 0), name="post")
    for (a, b_) in (((-0.236, -0.236, 0.200), (0.236, -0.236, 0.210)), ((-0.236, -0.236, 0.206), (-0.236, 0.236, 0.198)), ((0.236, -0.236, 0.204), (0.236, 0.236, 0.160))): B.cyl(M, "rust", a, b_, 0.012, (6, 6), name="rail")
    dr = B.rev(M, "alt", spl([(0.0, -0.200, 'c'), (0.110, -0.200, 'c'), (0.120, -0.188), (0.120, -0.070), (0.126, -0.062), (0.120, -0.054), (0.120, 0.060), (0.110, 0.074, 'c'), (0.0, 0.074, 'c')], B.q(4, 2)), (32, 14), "z", (-0.085, 0.060, -0.030), name="drum")
    B.rev(M, "bare", [(0.030, 0.044), (0.044, 0.044), (0.044, 0.070), (0.030, 0.070)], (12, 6), "z", (-0.085, 0.060, 0), True, name="bung")
    rad = B.box(M, "metal", (0.060, -0.200, -0.230), (0.215, 0.180, 0.020), name="radiator"); B.bevel(rad, 0.003, (1, 0))
    for i in range(12): B.box(M, "dark", (0.056, -0.186 + i*0.030, -0.214), (0.219, -0.176 + i*0.030, 0.006), kind="detail", name="fin")
    B.prism(M, "sec", [(-0.070, -0.090), (0.076, -0.100), (0.080, 0.020), (0.010, 0.040), (-0.074, 0.010)], 0.215, 0.221, ("y", "z", "x"), at=(0, -0.02, -0.07), name="patch")
    for (x, y) in ((0.090, -0.150), (0.170, -0.070)):
        B.rev(M, "glass", [(0.030, 0.030), (0.035, 0.030), (0.035, 0.150), (0.030, 0.150)], (16, 8), "z", (x, y, 0), True, name="valve")
        B.rev(M, "rust", [(0.0, 0.020), (0.042, 0.020), (0.042, 0.034), (0.0, 0.034)], (10, 6), "z", (x, y, 0), name="socket"); B.rev(M, "bare", [(0.0, 0.148), (0.038, 0.148), (0.030, 0.164), (0.0, 0.168)], (10, 6), "z", (x, y, 0), name="cap")
        for a in range(3): B.cyl(M, "bare", (x + 0.040*math.cos(a*TAU/3), y + 0.040*math.sin(a*TAU/3), 0.022), (x + 0.040*math.cos(a*TAU/3), y + 0.040*math.sin(a*TAU/3), 0.160), 0.004, (6, 6), name="bar")
    B.pipe(M, "rust", [(-0.085, 0.060, 0.050), (-0.085, 0.060, 0.150), (-0.060, 0.120, 0.200), (0.060, 0.170, 0.190), (0.150, 0.170, 0.110), (0.150, 0.150, 0.020)], 0.020, (12, 6), (6, 3), name="pipe")
    B.rev(M, "bare", [(0.021, -0.010), (0.028, -0.010), (0.028, 0.010), (0.021, 0.010)], (12, 6), "z", (-0.085, 0.060, 0.120), True, name="clampring")
    g = B.rev(M, "bare", [(0.0, -0.250), (0.050, -0.250), (0.054, -0.244), (0.054, -0.226), (0.0, -0.226)], (20, 10), "y", (-0.085, 0, -0.080), name="gauge"); B.rev(M, "base", [(0.0, -0.2515), (0.044, -0.2515), (0.044, -0.2495), (0.0, -0.2495)], (20, 10), "y", (-0.085, 0, -0.080), name="dial")
    B.prism("Needle", "sec", [(-0.004, -0.008), (0.004, -0.008), (0.0015, 0.040), (-0.0015, 0.040)], -0.2545, -0.2520, ("x", "z", "y"), at=(-0.085, 0, -0.080), name="needle")
    B.cyl(M, "rust", (-0.085, -0.226, -0.080), (-0.085, -0.120, -0.060), 0.008, (6, 6), name="gaugepipe")
    B.cyl(M, "bare", (-0.170, -0.140, 0.040), (-0.170, -0.140, 0.100), 0.014, (8, 6), name="terminal")
    for s_ in (-1, 1): j = B.prism("Clamp", "sec" if s_ > 0 else "bare", [(-0.010, 0.0), (0.010, 0.006), (0.020, 0.060), (0.004, 0.090), (-0.012, 0.050)], -0.010, 0.010, ("x", "z", "y"), at=(-0.170 + s_*0.012, -0.140, 0.090), name="jaw")
    B.pipe("Clamp", "rubber", [(-0.170, -0.140, 0.178), (-0.190, -0.170, 0.210), (-0.215, -0.215, 0.150), (-0.222, -0.228, 0.020)], 0.007, (8, 6), (6, 3), name="lead")
    B.weld(M, [(-0.20, -0.240, -0.238), (0.20, -0.240, -0.238)], 0.005); B.weld(M, [(0.218, -0.19, 0.022), (0.218, 0.17, 0.022)], 0.004); B.weld(M, [(-0.236, 0.20, -0.238), (-0.236, -0.20, -0.238)], 0.005)
    B.prism(M, "alt2", [(-0.110, -0.060), (0.090, -0.070), (0.100, 0.050), (-0.100, 0.060)], -0.2445, -0.2400, ("x", "z", "y"), at=(0.110, 0, -0.150), name="plate")


def battery_E(B):
    """Heroic Toon: a hero energy jug - big glass canister with a glowing core (squashes and stretches), cream caps, navy cradle with chunky
    orange clamps and a carry bar, pulsing emblem badge on the front"""
    foot(B, "dark", 0.05); backblock(B, "dark", rad=0.03)
    c = B.hull(M, "dark", [(-0.238, 0, -0.02, 0.205, 0.205, 4.0), (-0.200, 0, -0.03, 0.180, 0.170, 3.5), (-0.170, 0, -0.05, 0.150, 0.140, 3.0)], (36, 12), (5, 2), "z", name="cradle")
    J = Vector((0.0, -0.05, 0.0))
    B.rev("Jug", "glass", spl([(0.0, -0.180, 'c'), (0.120, -0.180, 'c'), (0.142, -0.150), (0.150, -0.020), (0.142, 0.110), (0.120, 0.136, 'c'), (0.0, 0.136, 'c')], B.q(5, 2)), (40, 16), "z", J, name="jug")
    for t in (-0.090, 0.050): B.rev("Jug", "glow", [(0.1492, t - 0.006), (0.1525, t - 0.006), (0.1525, t + 0.006), (0.1492, t + 0.006)], (40, 16), "z", J, True, name="band")
    B.rev("Jug", "sec", spl([(0.0, 0.130, 'c'), (0.128, 0.130, 'c'), (0.134, 0.150), (0.110, 0.186), (0.060, 0.204), (0.0, 0.206, 'c')], B.q(5, 2)), (36, 14), "z", J, name="cap")
    B.rev("Jug", "base", [(0.050, 0.200), (0.074, 0.196), (0.074, 0.216), (0.050, 0.226)], (24, 10), "z", J, True, name="plug")
    B.rev("Jug", "sec", [(0.124, -0.186), (0.156, -0.186), (0.156, -0.156), (0.146, -0.146)], (36, 14), "z", J, True, name="basering")
    for sx in (-1, 1):
        k = B.hull(M, "base", [(-0.236, sx*0.196, -0.05, 0.050, 0.100, 4.0), (-0.100, sx*0.186, -0.05, 0.046, 0.080, 4.0), (0.020, sx*0.170, -0.05, 0.034, 0.060, 3.5), (0.060, sx*0.160, -0.05, 0.022, 0.040, 3.0)], (20, 8), (5, 2), "z", name="clamp")
        B.cyl(M, "bare", (sx*0.214, -0.05, -0.120), (sx*0.232, -0.05, -0.120), 0.020, (12, 8), name="bolt")
    e = B.rev("Emblem", "sec", [(0.0, -0.202), (0.050, -0.202), (0.056, -0.196), (0.056, -0.184), (0.0, -0.184)], (24, 10), "y", (0.0, 0, 0.045), name="badge"); B.rev("Emblem", "glow", [(0.0, -0.2045), (0.034, -0.2045), (0.034, -0.2015), (0.0, -0.2015)], (20, 8), "y", (0.0, 0, 0.045), name="badgeglow")
    B.box(M, "glow", (-0.070, -0.2505, -0.226), (0.070, -0.244, -0.206), name="strip")


# ------------------------------------------------------------------------------------------------ GUNS
# Contract (approved rig): Body z -0.26..0.016; Yaw |x| <= 0.191, y -0.191..0.257, z 0.018..0.22; Pitch |x| <= 0.14, y -0.402..0.078, z 0.085..0.236;
# Barrel radius <= 0.04 around z = 0.135, y -0.62..-0.10. The reference boards draw far bigger barrels: here the mass is put into the pitch shroud
# (allowed up to 0.28 x 0.15 m) and the barrel itself stays inside its approved 80 mm outline (see hero2/README, "obstacles").
Y, P_, BR = "Yaw", "Pitch", "Barrel"; BZ = 0.135


def gun_foot(B, zone="dark", rad=0.016):
    p = B.box(M, zone, (-0.250, -0.250, -0.260), (0.250, 0.250, -0.236), name="foot"); B.bevel(p, rad, (4, 2), 30); return p
def barrel(B, prof, zone="metal", seg=(40, 14), phase=0.0, name="barrel"): return B.rev(BR, zone, prof, seg, "y", (0, 0, BZ), name=name, phase=phase)


def gun_A(B):
    """Sleek: a rail cannon - ceramic skirt pedestal with a segmented light ring, swept yoke fairings and two sensor blades, layered receiver
    (dark frame, ceramic roof and cheek plates, orange collar), slotted shroud with light chevrons, phase ring that spins and slides back on
    recoil, two cooling flaps that open, stepped barrel with a vented muzzle"""
    gun_foot(B, "dark")
    sk = B.hull(M, "base", [(-0.238, 0, 0, 0.236, 0.236, 6.0), (-0.170, 0, 0, 0.226, 0.226, 5.0), (-0.070, 0, 0, 0.186, 0.186, 3.4), (-0.012, 0, 0, 0.160, 0.160, 2.2)], (48, 16), (7, 2), "z", name="skirt")
    B.hull(M, "dark", [(-0.238, 0, 0, 0.224, 0.224, 6.0), (-0.170, 0, 0, 0.214, 0.214, 5.0), (-0.070, 0, 0, 0.174, 0.174, 3.4), (-0.012, 0, 0, 0.150, 0.150, 2.2)], (48, 16), (7, 2), "z", name="core")
    for a in range(4): k = B.box(M, "dark", (0.080, -0.013, -0.236), (0.340, 0.013, 0.020), kind="cutter"); B.move(k, Matrix.Rotation(a*TAU/4, 4, "Z")); B.cut(sk, k)
    B.cut(sk, B.rev(M, "dark", [(0.150, -0.124), (0.340, -0.124), (0.340, -0.112), (0.150, -0.112)], (24, 12), "z", closed=True, kind="cutter"), lo=False)
    for a in range(4): g = B.box(M, "glow", (0.213, -0.008, -0.200), (0.218, 0.008, -0.060), name="pedlight"); B.move(g, Matrix.Rotation(a*TAU/4, 4, "Z"))
    B.rev(M, "dark", [(0.0, -0.012), (0.150, -0.012), (0.150, 0.014), (0.0, 0.014)], (48, 20), "z", name="collar"); B.arcs(M, "glow", 0.151, 0.158, -0.004, 0.008, 6, 0.3, "z", seg=(48, 12))
    for a in range(4):
        k = B.box(M, "sec", (0.150, -0.030, -0.232), (0.238, 0.030, -0.200), name="key"); B.bevel(k, 0.004, (2, 1)); B.move(k, Matrix.Rotation(a*TAU/4 + TAU/8, 4, "Z"))
    # yaw
    B.rev(Y, "dark", spl([(0.0, 0.018, 'c'), (0.150, 0.018, 'c'), (0.150, 0.034, 'c'), (0.128, 0.050), (0.100, 0.060, 'c'), (0.0, 0.060, 'c')], B.q(4, 2)), (48, 20), "z", name="turntable")
    for sx in (-1, 1):
        B.hull(Y, "base", [(0.170, sx*0.166, 0.090, 0.014, 0.040, 3.0), (0.060, sx*0.168, 0.118, 0.022, 0.080, 3.0), (-0.060, sx*0.168, 0.116, 0.022, 0.074, 3.0), (-0.150, sx*0.166, 0.090, 0.014, 0.040, 3.0), (-0.186, sx*0.164, 0.070, 0.006, 0.016, 2.5)], (24, 10), (6, 2), "y", name="yoke")
        B.rev(Y, "metal", [(0.0, sx*0.148), (0.034, sx*0.148), (0.034, sx*0.190), (0.0, sx*0.190)] if sx > 0 else [(0.0, -0.190), (0.034, -0.190), (0.034, -0.148), (0.0, -0.148)], (24, 10), "x", (0, 0, BZ), name="trunnion")
        B.arcs(Y, "glow", 0.022, 0.028, sx*0.1895 - 0.001, sx*0.1895 + 0.0012, 3, 0.35, "x", (0, 0, BZ), (24, 9))
        f = B.prism(Y, "dark", [(0.150, 0.150), (0.200, 0.150), (0.252, 0.218), (0.236, 0.218)], sx*0.110 - 0.006, sx*0.110 + 0.006, ("y", "z", "x"), name="blade"); B.bevel(f, 0.002, (2, 0))
    B.hull(Y, "dark", [(0.090, 0, 0.100, 0.130, 0.050, 5.0), (0.170, 0, 0.100, 0.120, 0.056, 5.0), (0.240, 0, 0.096, 0.080, 0.040, 4.0), (0.255, 0, 0.094, 0.050, 0.024, 3.0)], (24, 10), (5, 2), "y", name="pack")
    B.box(Y, "glow", (-0.050, 0.2545, 0.086), (0.050, 0.2568, 0.100), name="tail")
    # pitch: frame, roof, cheeks, shroud
    B.hull(P_, "dark", [(0.074, 0, 0.158, 0.060, 0.044, 4.0), (0.020, 0, 0.160, 0.104, 0.064, 4.5), (-0.120, 0, 0.160, 0.108, 0.066, 4.5), (-0.200, 0, 0.156, 0.078, 0.060, 4.0), (-0.400, 0, 0.150, 0.062, 0.056, 3.6)], (28, 12), (8, 3), "y", name="frame")
    B.hull(P_, "base", [(0.060, 0, 0.212, 0.050, 0.012, 3.0), (0.010, 0, 0.218, 0.112, 0.016, 3.0), (-0.110, 0, 0.218, 0.116, 0.016, 3.0), (-0.190, 0, 0.212, 0.070, 0.014, 3.0), (-0.300, 0, 0.204, 0.040, 0.010, 3.0)], (24, 8), (7, 2), "y", name="roof")
    for sx in (-1, 1):
        c = B.prism(P_, "base", [(0.050, 0.118), (0.030, 0.200), (-0.130, 0.200), (-0.186, 0.168), (-0.170, 0.110), (-0.020, 0.100)], sx*0.108, sx*0.122, ("y", "z", "x"), name="cheek"); B.bevel(c, 0.004, (3, 1), 30)
        B.prism(P_, "sec", [(-0.136, 0.196), (-0.156, 0.196), (-0.206, 0.166), (-0.192, 0.160)], sx*0.110, sx*0.1235, ("y", "z", "x"), name="accent")
        for i in range(4): B.prism(P_, "glow", [(0.0, -0.014), (-0.012, 0.0), (0.0, 0.014), (-0.014, 0.014), (-0.026, 0.0), (-0.014, -0.014)], sx*0.0615, sx*0.064, ("y", "z", "x"), at=(0, -0.250 - i*0.034, 0.152), name="chevron")
    B.rev(P_, "sec", [(0.060, -0.232), (0.070, -0.232), (0.070, -0.214), (0.060, -0.214)], (32, 12), "y", (0, 0, 0.156), True, name="band")
    r = B.rev("Ring", "base", [(0.060, -0.318), (0.072, -0.318), (0.074, -0.300), (0.072, -0.282), (0.060, -0.282)], (32, 12), "y", (0, 0, 0.160), True, name="ring")
    k = B.box("Ring", "dark", (-0.010, -0.330, 0.224), (0.010, -0.270, 0.240), kind="cutter"); [B.cut(r, x, lo=False) for x in B.radial([k], 6, "y", (0, 0, 0.160))]
    B.arcs("Ring", "glow", 0.0742, 0.0762, -0.304, -0.296, 6, 0.5, "y", (0, 0, 0.160), (36, 12))
    for (nm, sx) in (("VentL", -1), ("VentR", 1)):
        v = B.prism(nm, "base", [(0.030, 0.134), (0.030, 0.190), (-0.060, 0.186), (-0.076, 0.150), (-0.060, 0.138)], sx*0.123, sx*0.131, ("y", "z", "x"), name="vent"); B.bevel(v, 0.003, (2, 1), 30)
    barrel(B, [(0.0, -0.100), (0.024, -0.100), (0.024, -0.400), (0.030, -0.404), (0.030, -0.470), (0.026, -0.476), (0.026, -0.520), (0.034, -0.526), (0.038, -0.540), (0.038, -0.608), (0.034, -0.618), (0.022, -0.618), (0.022, -0.560), (0.0, -0.560)])
    B.rev(BR, "sec", [(0.0302, -0.452), (0.0325, -0.452), (0.0325, -0.438), (0.0302, -0.438)], (32, 12), "y", (0, 0, BZ), True, name="mark")
    for i in range(3): B.arcs(BR, "glow", 0.0382, 0.0396, -0.556 - i*0.016, -0.550 - i*0.016, 4, 0.55, "y", (0, 0, BZ), (32, 8), phase=45)


def gun_B(B):
    """Brutal: a field gun - octagonal cast pedestal with a toothed ring, bolted trunnion plates with lightening holes, forged breech block,
    armour plate with hazard paint, two hydraulic recoil cylinders with chrome rods, recoil collar and charging handle that slide, drive gear
    that turns, corrugated hoses, octagonal barrel and slotted muzzle brake"""
    gun_foot(B, "base", 0.006); B.bolts(M, "bare", 0.310, 4, -0.236, 0.016, 0.012, "z", kind="solid", phase=TAU/8)
    p = B.rev(M, "base", [(0.0, -0.236), (0.236, -0.236), (0.236, -0.170), (0.180, -0.060), (0.180, -0.020), (0.0, -0.020)], (8, 8), "z", name="pedestal", phase=TAU/16); B.bevel(p, 0.005, (2, 1), 25)
    B.rev(M, "dark", [(0.0, -0.020), (0.168, -0.020), (0.168, 0.014), (0.0, 0.014)], (40, 20), "z", name="race")
    t = B.box(M, "bare", (0.164, -0.010, -0.016), (0.184, 0.010, 0.012), name="tooth"); B.radial([t], 20, "z")
    for a in range(4):
        g = B.prism(M, "base", [(0.150, -0.230), (0.236, -0.230), (0.172, -0.060)], -0.010, 0.010, ("x", "z", "y"), name="gusset"); B.move(g, Matrix.Rotation(a*TAU/4, 4, "Z"))
    B.rev(Y, "base", [(0.0, 0.018), (0.160, 0.018), (0.160, 0.046), (0.120, 0.060), (0.0, 0.060)], (8, 8), "z", name="turntable", phase=TAU/16)
    for sx in (-1, 1):
        c = B.prism(Y, "base", [(0.150, 0.050), (0.150, 0.130), (0.060, 0.206), (-0.060, 0.206), (-0.150, 0.120), (-0.170, 0.050)], sx*0.150, sx*0.184, ("y", "z", "x"), name="plate"); B.bevel(c, 0.004, (2, 1), 30)
        h = B.cyl(Y, "base", (sx*0.14, 0.085, 0.100), (sx*0.20, 0.085, 0.100), 0.022, (12, 8), kind="cutter"); B.cut(c, h); h2 = B.cyl(Y, "base", (sx*0.14, -0.095, 0.092), (sx*0.20, -0.095, 0.092), 0.018, (12, 8), kind="cutter"); B.cut(c, h2)
        o = B.rev(Y, "metal", [(0.0, sx*0.184), (0.050, sx*0.184), (0.050, sx*0.190), (0.0, sx*0.190)] if sx > 0 else [(0.0, -0.190), (0.050, -0.190), (0.050, -0.184), (0.0, -0.184)], (8, 8), "x", (0, 0, BZ), name="bearing")
        B.bolts(Y, "bare", 0.038, 6, sx*0.186, 0.006, sx*0.004, "x", (0, 0, BZ), kind="detail")
    cw = B.box(Y, "dark", (-0.130, 0.110, 0.060), (0.100, 0.250, 0.170), name="counterweight"); B.bevel(cw, 0.008, (2, 1))
    for i in range(4): B.box(Y, "base", (-0.134, 0.126 + i*0.030, 0.070), (0.104, 0.138 + i*0.030, 0.176), kind="detail", name="rib")
    g = B.cyl("Gear", "bare", (0.110, 0.176, 0.120), (0.146, 0.176, 0.120), 0.044, (12, 12), name="gear"); tt = B.box("Gear", "bare", (0.112, 0.168, 0.158), (0.144, 0.184, 0.176), name="gt"); B.radial([tt], 10, "x", (0, 0.176, 0.120)); B.cyl("Gear", "dark", (0.146, 0.176, 0.120), (0.156, 0.176, 0.120), 0.016, (6, 6), name="gearnut")
    br = B.box(P_, "base", (-0.100, -0.160, 0.094), (0.100, 0.070, 0.206), name="breech"); B.bevel(br, 0.010, (3, 1), 30)
    ap = B.prism(P_, "sec", [(0.060, 0.206), (0.040, 0.230), (-0.130, 0.230), (-0.180, 0.200), (-0.180, 0.190), (0.060, 0.190)], -0.112, 0.112, ("y", "z", "x"), name="armour"); B.bevel(ap, 0.003, (2, 0))
    for (x, y) in ((-0.090, 0.020), (0.090, 0.020), (-0.090, -0.110), (0.090, -0.110)): B.cyl(P_, "bare", (x, y, 0.230), (x, y, 0.2355), 0.009, (6, 6), name="abolt")
    yk = B.box(P_, "base", (-0.130, -0.400, 0.092), (0.130, -0.366, 0.186), name="yoke"); B.bevel(yk, 0.006, (2, 1))
    for sx in (-1, 1):
        B.cyl(P_, "dark", (sx*0.090, -0.150, BZ), (sx*0.090, -0.290, BZ), 0.026, (16, 8), name="cyl"); B.cyl(P_, "bare", (sx*0.090, -0.290, BZ), (sx*0.090, -0.370, BZ), 0.013, (12, 6), name="rod")
        B.rev(P_, "sec", [(0.0265, -0.176), (0.029, -0.176), (0.029, -0.160), (0.0265, -0.160)], (16, 8), "y", (sx*0.090, 0, BZ), True, name="cylband")
        B.pipe(P_, "rubber", [(sx*0.090, -0.170, BZ + 0.026), (sx*0.100, -0.140, 0.200), (sx*0.090, -0.060, 0.214), (sx*0.060, 0.040, 0.212)], 0.009, (8, 6), (6, 3), name="hose", rfn=(lambda t: 1.0 + 0.12*math.sin(t*50)) if B.hi else None)
    bf = B.rev("Buffer", "dark", [(0.036, -0.446), (0.052, -0.446), (0.052, -0.406), (0.036, -0.406)], (8, 8), "y", (0, 0, BZ), True, name="buffer", phase=TAU/16); B.bevel(bf, 0.003, (2, 0))
    bo = B.box("Bolt", "bare", (0.100, -0.070, 0.150), (0.128, -0.030, 0.180), name="bolt"); B.bevel(bo, 0.004, (2, 1)); B.cyl("Bolt", "dark", (0.128, -0.050, 0.165), (0.150, -0.050, 0.165), 0.012, (10, 6), name="knob")
    barrel(B, [(0.0, -0.100), (0.034, -0.100), (0.034, -0.500), (0.0, -0.500)], "metal", (8, 8), TAU/16)
    mb = barrel(B, [(0.0, -0.500), (0.040, -0.500), (0.040, -0.618), (0.026, -0.618), (0.026, -0.560), (0.0, -0.560)], "base", (8, 8), TAU/16, "brake")
    for yy in (-0.530, -0.570): k = B.box(BR, "base", (-0.060, yy - 0.012, BZ - 0.010), (0.060, yy + 0.012, BZ + 0.010), kind="cutter"); B.cut(mb, k)
    B.rev(BR, "sec", [(0.0345, -0.330), (0.037, -0.330), (0.037, -0.300), (0.0345, -0.300)], (8, 8), "y", (0, 0, BZ), True, name="mark", phase=TAU/16)


def gun_C(B):
    """Arcade: a clear toy-soldier silhouette done properly - navy octagonal pedestal with an orange ring, cream capsule plates on a navy core,
    orange top slide (recoils), five-segment light bar, stubby eight-sided barrel with a bold orange muzzle collar"""
    gun_foot(B, "dark", 0.04)
    p = B.rev(M, "metal", [(0.0, -0.236), (0.230, -0.236), (0.230, -0.180), (0.170, -0.050), (0.0, -0.050)], (8, 8), "z", name="pedestal", phase=TAU/16); B.bevel(p, 0.010, (3, 1), 25)
    o = B.rev(M, "sec", [(0.0, -0.050), (0.176, -0.050), (0.176, -0.010), (0.160, 0.014), (0.0, 0.014)], (8, 8), "z", name="ring", phase=TAU/16); B.bevel(o, 0.006, (3, 1), 25)
    t = B.rev(Y, "dark", [(0.0, 0.018), (0.150, 0.018), (0.150, 0.050), (0.110, 0.066), (0.0, 0.066)], (8, 8), "z", name="turntable", phase=TAU/16); B.bevel(t, 0.005, (2, 1), 25)
    for sx in (-1, 1):
        c = B.hull(Y, "base", [(0.150, sx*0.166, 0.110, 0.022, 0.056, 4.0), (0.060, sx*0.166, 0.120, 0.022, 0.086, 4.0), (-0.090, sx*0.166, 0.118, 0.022, 0.080, 4.0), (-0.160, sx*0.166, 0.100, 0.020, 0.046, 4.0)], (8, 8), (4, 2), "y", name="cheek"); B.bevel(c, 0.006, (3, 1), 25)
        B.cyl(Y, "sec", (sx*0.186, 0, BZ), (sx*0.1905, 0, BZ), 0.034, (8, 8), name="pin")
    r = B.box(Y, "metal", (-0.110, 0.110, 0.060), (0.110, 0.240, 0.170), name="pack"); B.bevel(r, 0.016, (3, 1), 25); B.box(Y, "glow", (-0.060, 0.2395, 0.100), (0.060, 0.2425, 0.130), name="tail")
    k = B.hull(P_, "metal", [(0.070, 0, 0.156, 0.090, 0.060, 6.0), (-0.130, 0, 0.156, 0.104, 0.066, 6.0), (-0.300, 0, 0.150, 0.080, 0.060, 6.0), (-0.398, 0, 0.146, 0.064, 0.054, 6.0)], (8, 8), (4, 2), "y", name="core"); B.bevel(k, 0.008, (3, 1), 25)
    for sx in (-1, 1):
        c = B.hull(P_, "base", [(0.050, sx*0.122, 0.158, 0.014, 0.036, 5.0), (0.000, sx*0.124, 0.158, 0.014, 0.060, 5.0), (-0.150, sx*0.124, 0.158, 0.014, 0.060, 5.0), (-0.210, sx*0.118, 0.156, 0.012, 0.034, 5.0)], (8, 8), (4, 2), "y", name="capsule"); B.bevel(c, 0.006, (3, 1), 25)
        B.cyl(P_, "glow", (sx*0.1385, -0.060, 0.160), (sx*0.1395, -0.060, 0.160), 0.022, (8, 8), name="eye")
        for i in range(5): B.box(P_, "glow", (sx*0.0795, -0.250 - i*0.026, 0.144), (sx*0.0820, -0.232 - i*0.026, 0.176), name="lightbar")
    s = B.box("Slide", "sec", (-0.070, -0.170, 0.206), (0.070, 0.010, 0.236), name="slide"); B.bevel(s, 0.008, (3, 1), 25)
    b = barrel(B, [(0.0, -0.100), (0.032, -0.100), (0.032, -0.540), (0.0, -0.540)], "metal", (8, 8), TAU/16)
    m = barrel(B, [(0.0, -0.530), (0.040, -0.530), (0.040, -0.618), (0.026, -0.618), (0.026, -0.570), (0.0, -0.570)], "sec", (8, 8), TAU/16, "muzzle"); B.bevel(m, 0.004, (2, 1), 25)
    B.rev(BR, "base", [(0.0325, -0.470), (0.036, -0.470), (0.036, -0.430), (0.0325, -0.430)], (8, 8), "y", (0, 0, BZ), True, name="band", phase=TAU/16)


def gun_D(B):
    """Scrap: a gun built from what was lying around - welded sheet pedestal with patches, rusty turntable, two different bracket plates, a gas
    bottle strapped on, hand crank (turns when aiming), box receiver with a striped plate and a green plate from other machines, perforated
    cooling jacket, loose lid that rattles, bent pipe, pipe barrel with a perforated muzzle sleeve and a rag wrap"""
    B.prism(M, "base", [(-0.250, -0.250), (0.250, -0.250), (0.250, 0.210), (0.210, 0.250), (-0.250, 0.250)], -0.260, -0.240, ("x", "y", "z"), name="foot")
    p = B.rev(M, "metal", [(0.0, -0.240), (0.226, -0.240), (0.176, -0.040), (0.0, -0.040)], (4, 4), "z", name="pedestal", phase=TAU/8)
    B.prism(M, "sec", [(-0.120, -0.200), (0.100, -0.214), (0.086, -0.070), (-0.090, -0.060)], -0.2135, -0.208, ("x", "z", "y"), name="patch_f"); B.prism(M, "alt", [(-0.100, -0.190), (0.130, -0.170), (0.070, -0.066), (-0.080, -0.080)], 0.150, 0.1555, ("y", "z", "x"), name="patch_s")
    B.rev(M, "rust", [(0.0, -0.040), (0.172, -0.040), (0.172, 0.014), (0.0, 0.014)], (14, 14), "z", name="race")
    B.weld(M, [(-0.20, -0.228, -0.238), (0.20, -0.228, -0.238)], 0.005); B.weld(M, [(-0.118, -0.212, -0.20), (0.098, -0.216, -0.212)], 0.004); B.weld(M, [(0.154, -0.10, -0.19), (0.154, 0.13, -0.17)], 0.004)
    B.pipe(M, "rust", [(-0.200, 0.140, -0.236), (-0.200, 0.140, -0.120), (-0.150, 0.130, -0.040)], 0.016, (8, 6), (5, 2), name="stub")
    B.rev(Y, "rust", [(0.0, 0.018), (0.156, 0.018), (0.156, 0.040), (0.0, 0.040)], (14, 14), "z", name="turntable")
    B.prism(Y, "metal", [(0.140, 0.040), (0.150, 0.150), (0.040, 0.214), (-0.080, 0.200), (-0.170, 0.110), (-0.150, 0.040)], -0.176, -0.164, ("y", "z", "x"), name="bracket_l")
    B.prism(Y, "base", [(0.160, 0.040), (0.100, 0.180), (-0.050, 0.196), (-0.120, 0.040)], 0.160, 0.170, ("y", "z", "x"), name="bracket_r"); B.prism(Y, "alt2", [(0.090, 0.060), (0.060, 0.150), (-0.020, 0.160), (-0.050, 0.070)], 0.170, 0.175, ("y", "z", "x"), name="bracket_patch")
    for sx in (-1, 1): B.cyl(Y, "bare", (sx*0.160, 0, BZ), (sx*0.190, 0, BZ), 0.020, (6, 6), name="pin"); B.weld(Y, [(sx*0.166, 0.13, 0.042), (sx*0.166, -0.13, 0.042)], 0.004)
    B.rev(Y, "alt", spl([(0.0, 0.085, 'c'), (0.040, 0.085, 'c'), (0.050, 0.100), (0.050, 0.216), (0.030, 0.240), (0.012, 0.246, 'c'), (0.0, 0.246, 'c')], B.q(4, 2)), (20, 10), "y", (0.085, 0, 0.100), name="bottle")
    B.rev(Y, "bare", [(0.0505, 0.130), (0.054, 0.130), (0.054, 0.148), (0.0505, 0.148)], (20, 10), "y", (0.085, 0, 0.100), True, name="strap"); B.box(Y, "rust", (-0.110, 0.100, 0.040), (0.020, 0.220, 0.090), name="box")
    B.rev("Crank", "rust", [(0.0, -0.190), (0.050, -0.190), (0.050, -0.180), (0.0, -0.180)], (12, 12), "x", (0, 0.030, 0.120), name="crank"); B.cyl("Crank", "bare", (-0.190, 0.030, 0.158), (-0.184, 0.030, 0.158), 0.010, (8, 6), name="handle")
    rc = B.box(P_, "rust", (-0.094, -0.150, 0.092), (0.094, 0.072, 0.204), name="receiver"); B.bevel(rc, 0.003, (1, 0))
    B.prism(P_, "sec", [(0.066, -0.090), (0.060, 0.086), (-0.130, 0.092), (-0.146, -0.080)], 0.204, 0.210, ("y", "x", "z"), name="lid_fixed")
    B.prism(P_, "alt", [(0.060, 0.100), (0.050, 0.196), (-0.120, 0.200), (-0.140, 0.120)], -0.102, -0.094, ("y", "z", "x"), name="plate_l"); B.prism(P_, "base", [(0.040, 0.096), (0.066, 0.190), (-0.090, 0.180), (-0.120, 0.104)], 0.094, 0.101, ("y", "z", "x"), name="plate_r")
    for (y, z) in ((0.040, 0.110), (0.030, 0.186), (-0.110, 0.186), (-0.120, 0.130)): B.cyl(P_, "bare", (-0.108, y, z), (-0.102, y, z), 0.007, (6, 6), name="rivet")
    jk = B.rev(P_, "metal", [(0.042, -0.398), (0.049, -0.398), (0.049, -0.150), (0.042, -0.150)], (16, 16), "y", (0, 0, BZ), True, name="jacket")
    for i in range(5):
        h = B.cyl(P_, "metal", (0, -0.185 - i*0.045, BZ), (0, -0.185 - i*0.045, BZ + 0.07), 0.012, (8, 6), kind="cutter"); [B.cut(jk, x, lo=False) for x in B.radial([h], 6, "y", (0, 0, BZ), phase=(i % 2)*TAU/12)]
    B.pipe(P_, "rust", [(0.060, 0.050, 0.204), (0.070, 0.000, 0.218), (0.066, -0.130, 0.216), (0.040, -0.200, 0.190)], 0.010, (8, 6), (6, 3), name="pipe")
    for sx in (-0.060, 0.050): B.pipe(P_, "bare", [(sx, 0.060, 0.206), (sx, 0.040, 0.228), (sx*0.9, -0.110, 0.230), (sx*0.8, -0.146, 0.206)], 0.005, (6, 6), (5, 2), name="guard")
    B.bolts(Y, "bare", 0.130, 7, 0.040, 0.008, 0.006, "z", kind="solid", phase=0.3); B.prism(M, "alt2", [(-0.090, -0.180), (0.080, -0.196), (0.060, -0.090), (-0.070, -0.100)], -0.156, -0.150, ("y", "z", "x"), name="patch_b")
    B.weld(P_, [(-0.094, -0.148, 0.094), (-0.094, -0.148, 0.202)], 0.004); B.weld(P_, [(0.094, 0.07, 0.094), (0.094, -0.148, 0.096)], 0.004)
    B.prism("Rattle", "alt2", [(-0.076, -0.300), (0.004, -0.306), (0.010, -0.140), (-0.070, -0.146)], 0.214, 0.219, ("x", "y", "z"), at=(0.090, 0, 0), name="rattle")
    barrel(B, [(0.0, -0.100), (0.026, -0.100), (0.026, -0.540), (0.0, -0.540)], "rust", (14, 10))
    ms = B.rev(BR, "metal", [(0.031, -0.618), (0.038, -0.618), (0.038, -0.500), (0.031, -0.500)], (16, 16), "y", (0, 0, BZ), True, name="sleeve")
    for i in range(3):
        h = B.cyl(BR, "metal", (0, -0.524 - i*0.034, BZ), (0, -0.524 - i*0.034, BZ + 0.06), 0.009, (8, 6), kind="cutter"); [B.cut(ms, x, lo=False) for x in B.radial([h], 6, "y", (0, 0, BZ), phase=(i % 2)*TAU/12)]
    B.rev(BR, "sec", spl([(0.027, -0.474, 'c'), (0.036, -0.470), (0.039, -0.455), (0.035, -0.440), (0.027, -0.436, 'c')], B.q(3, 1)), (18, 10), "y", (0, 0, BZ), True, name="rag")
    B.prism(BR, "sec", [(-0.010, 0.0), (0.012, 0.0), (0.008, -0.022), (0.002, -0.018), (-0.006, -0.020)], -0.462, -0.456, ("x", "z", "y"), at=(0.0, 0, BZ - 0.012), name="ragtail")


def gun_E(B):
    """Heroic Toon: a blaster with a face - soft orange pedestal on a navy base, puffy cream shoulder pads, big orange shroud with a cream brow,
    a round lens eye on each side, three slanted light slits, hex muzzle bell that swells when it fires, springy antenna, navy barrel with a
    chunky orange hex muzzle"""
    gun_foot(B, "dark", 0.05)
    B.hull(M, "metal", [(-0.238, 0, 0, 0.226, 0.226, 4.5), (-0.170, 0, 0, 0.214, 0.214, 4.0), (-0.070, 0, 0, 0.180, 0.180, 3.0), (-0.020, 0, 0, 0.160, 0.160, 2.4)], (40, 14), (7, 2), "z", name="pedestal")
    B.rev(M, "base", spl([(0.186, -0.118, 'c'), (0.204, -0.112), (0.206, -0.092), (0.196, -0.080, 'c')], B.q(3, 1)), (40, 16), "z", closed=True, name="belt")
    B.rev(M, "sec", spl([(0.0, -0.022, 'c'), (0.164, -0.022, 'c'), (0.172, -0.006), (0.164, 0.012), (0.0, 0.014, 'c')], B.q(4, 2)), (40, 16), "z", name="ring")
    for a in range(4):
        k = B.hull(M, "base", [(-0.236, 0.196, 0, 0.040, 0.044, 3.5), (-0.200, 0.200, 0, 0.040, 0.050, 3.5), (-0.170, 0.190, 0, 0.026, 0.034, 3.0)], (16, 8), (4, 2), "z", name="toe"); B.move(k, Matrix.Rotation(a*TAU/4 + TAU/8, 4, "Z"))
    B.rev(Y, "dark", spl([(0.0, 0.018, 'c'), (0.150, 0.018, 'c'), (0.154, 0.036), (0.130, 0.056), (0.0, 0.062, 'c')], B.q(4, 2)), (40, 16), "z", name="turntable")
    for sx in (-1, 1):
        B.hull(Y, "sec", [(0.150, sx*0.160, 0.104, 0.016, 0.040, 2.6), (0.070, sx*0.164, 0.122, 0.026, 0.084, 2.8), (-0.070, sx*0.164, 0.120, 0.026, 0.080, 2.8), (-0.160, sx*0.160, 0.096, 0.014, 0.036, 2.6)], (24, 10), (6, 2), "y", name="pad")
        B.rev(Y, "base", [(0.0, sx*0.186), (0.030, sx*0.186), (0.026, sx*0.1905), (0.0, sx*0.1905)] if sx > 0 else [(0.0, -0.1905), (0.026, -0.1905), (0.030, -0.186), (0.0, -0.186)], (20, 8), "x", (0, 0, BZ), name="pin")
    B.hull(Y, "base", [(0.100, 0, 0.104, 0.110, 0.050, 3.2), (0.180, 0, 0.108, 0.104, 0.060, 3.2), (0.250, 0, 0.100, 0.060, 0.036, 2.8)], (28, 10), (5, 2), "y", name="pack"); B.box(Y, "glow", (-0.036, 0.2485, 0.092), (0.036, 0.2525, 0.112), name="tail")
    B.cyl("Antenna", "metal", (0.050, 0.190, 0.160), (0.050, 0.204, 0.222), 0.006, (10, 6), name="stalk"); B.rev("Antenna", "base", spl([(0.0, -0.014, 'c'), (0.012, -0.010), (0.015, 0.0), (0.012, 0.010), (0.0, 0.014, 'c')], B.q(3, 2)), (14, 8), "z", (0.050, 0.205, 0.222), name="ball")
    B.hull(P_, "base", [(0.076, 0, 0.158, 0.060, 0.044, 2.8), (0.010, 0, 0.162, 0.112, 0.070, 3.0), (-0.130, 0, 0.162, 0.120, 0.072, 3.0), (-0.230, 0, 0.160, 0.096, 0.066, 2.8), (-0.272, 0, 0.160, 0.074, 0.058, 2.6)], (36, 14), (8, 3), "y", name="shroud")
    B.hull(P_, "dark", [(0.060, 0, 0.112, 0.070, 0.022, 3.0), (-0.100, 0, 0.106, 0.084, 0.020, 3.0), (-0.240, 0, 0.112, 0.060, 0.018, 3.0)], (20, 8), (5, 2), "y", name="belly")
    B.hull(P_, "sec", [(0.040, 0, 0.222, 0.060, 0.012, 3.0), (-0.040, 0, 0.226, 0.084, 0.010, 3.0), (-0.130, 0, 0.222, 0.070, 0.010, 3.0)], (20, 8), (5, 2), "y", name="brow")
    for sx in (-1, 1):
        B.rev(P_, "sec", [(0.030, sx*0.118), (0.046, sx*0.118), (0.046, sx*0.136), (0.030, sx*0.138)], (24, 10), "x", (0, -0.020, 0.160), True, name="eyering")
        B.rev(P_, "glow", [(0.0, sx*0.136), (0.020, sx*0.135), (0.030, sx*0.130), (0.030, sx*0.124), (0.0, sx*0.124)] if sx > 0 else [(0.0, -0.124), (0.030, -0.124), (0.030, -0.130), (0.020, -0.135), (0.0, -0.136)], (24, 10), "x", (0, -0.020, 0.160), name="lens")
    for i in range(3): s = B.box(P_, "glow", (-0.050, -0.170 - i*0.034, 0.2275), (0.050, -0.152 - i*0.034, 0.2345), name="slit"); B.move(s, Matrix.Translation((0, 0, -i*0.003)))
    bl = B.rev("Bell", "base", [(0.050, -0.346), (0.072, -0.346), (0.076, -0.320), (0.070, -0.276), (0.050, -0.276)], (6, 6), "y", (0, 0, 0.160), True, name="bell", phase=TAU/12); B.bevel(bl, 0.008, (3, 1), 25)
    B.rev("Bell", "sec", [(0.040, -0.350), (0.052, -0.350), (0.052, -0.340), (0.040, -0.340)], (24, 10), "y", (0, 0, 0.156), True, name="bellring")
    barrel(B, [(0.0, -0.100), (0.030, -0.100), (0.030, -0.550), (0.0, -0.550)], "dark", (24, 10))
    m = barrel(B, [(0.0, -0.540), (0.040, -0.540), (0.040, -0.618), (0.027, -0.618), (0.027, -0.570), (0.0, -0.570)], "base", (6, 6), TAU/12, "muzzle"); B.bevel(m, 0.006, (3, 1), 25)
    B.rev(BR, "glow", [(0.0305, -0.470), (0.033, -0.470), (0.033, -0.455), (0.0305, -0.455)], (24, 10), "y", (0, 0, BZ), True, name="band")


RECIPES = {("Wheel", s): f for s, f in zip("ABCDE", (wheel_A, wheel_B, wheel_C, wheel_D, wheel_E))}
RECIPES.update({("Battery", s): f for s, f in zip("ABCDE", (battery_A, battery_B, battery_C, battery_D, battery_E))})

# ------------------------------------------------------------------------------------------------ skins (palette + wear recipe per style)
from h2pal import PAL, FX
RECIPES.update({("Gun", s): f for s, f in zip("ABCDE", (gun_A, gun_B, gun_C, gun_D, gun_E))})
