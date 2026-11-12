"""#1239 Visual Polish Pass 2: re-designed Basic Gun, Wheel, HoverEngine, Battery, ExplosiveTrapDeployer, ExplosiveTrap in the five styles.
Game contract unchanged (envelope, contact faces, source node names / pivots / bounds, sockets, clip timeline, events). New in this pass:
 * the volume of every source node is actually used (big readable primary forms instead of thin parts inside a large outline),
 * mount plates are structural cores in the hull substructure colour (they sit in the armour like sockets),
 * MECHANISM nodes: extra cosmetic mesh nodes parented to the approved nodes, each with its own motion written per style on the
   existing clip timeline (mech(name, pivot, parent, fn); fn(clip, u) -> T(...)). They never move sockets and add no events."""
import math, numpy as np
import akit as K, pdefs as PD, qstyle as QS, qdefs as QD
from akit import sbox, lathe, cyl, tube, rod, slab, torus, ball, cone, node, rot, R_to, X, Y, Z, port, empty, ngon
from qdefs import core_box
H = K.H; P = {}; MECH = {}
def D(typ, style):
    def deco(fn): P.setdefault(typ, {})[style] = fn; return fn
    return deco
def T(loc=(0, 0, 0), ax=(0, 0, 1), deg=0.0, s=1.0): return (tuple(loc), tuple(ax), float(deg), float(s))
def mech(name, pivot, parent, fn):
    if K._GRP: return                                             # inside a payload miniature the mechanism is frozen into the payload
    node(name, pivot, parent=parent); MECH[name] = fn
def body():
    if not K._GRP: node()
def sm(u, a=0.0, b=1.0): t = min(1.0, max(0.0, (u - a)/(b - a))); return t*t*(3 - 2*t)
def bump(u, a, m, b): return sm(u, a, m)*(1 - sm(u, m, b))
def spring(u, n=3.0, d=4.0): return math.exp(-d*u)*math.sin(2*math.pi*n*u)
def jit(u, k=1.0, s=0.0): return (math.sin(47.3*u*k + s) + 0.6*math.sin(113.1*u*k + 2.1*s) + 0.4*math.sin(201.7*u*k + 0.7 + s))/2.0
def deform(n, S, c=(0, 0, 0)):
    """non-uniform scale of the last n parts about c (normals corrected)"""
    S = np.asarray(S, float); c = np.asarray(c, float)
    for p in K.BASE.CUR.parts[-n:]:
        p["V"][:] = (p["V"] - c)*S + c
        for f in p["faces"]: nn = np.asarray(f["n"], float)/S; f["n"] = nn/np.linalg.norm(nn, axis=-1, keepdims=True)
def ring_of(n, fn, phase=0.0):
    for i in range(n): fn(rot(Z, phase + 360.0*i/n), i)
def bolt(at, n, r=0.014, grp="bolt"): lathe([(0, 0), (r, 0), (r, r*0.6), (r*0.65, r*0.95), (0, r*0.95)], grp, 6, crease=1.0, R=R_to(n), at=at)
PV = PD.PV

# =================================================================================================== BASIC GUN
@D("Gun", "A")
def gun_a():
    """Sleek 'Lance': low collar with a sunk light ring, cradle with swept cheeks and an exposed power cell, flattened teardrop receiver with recessed
    side channels, twin accelerator rails. Mechanisms: charge ring with studs travels to the muzzle and spins, side vents open on recoil."""
    core_box(-0.255, 0.255, -0.255, 0.255, -H, -0.215); port("down")
    lathe([(0, -0.215), (0.238, -0.215), (0.244, -0.19), (0.215, -0.13), (0.205, -0.10), (0.15, -0.03), (0.12, 0.012), (0, 0.012)], ["sec", "base", "base", "dark", "base", "base", "dark"], 40, crease=50)
    tube(0.207, 0.213, -0.128, -0.104, "glow", 0.002, 40)
    node("Yaw", PV, parent="GunPivot")
    lathe([(0, 0.02), (0.186, 0.02), (0.189, 0.045), (0.155, 0.075), (0, 0.086)], ["dark", "sec", "base", "base"], 40, crease=50); tube(0.187, 0.191, 0.028, 0.038, "glow", 0.002, 40)
    for sx in (-1, 1): slab([(-0.13, 0.07), (0.17, 0.07), (0.215, 0.115), (0.11, 0.216), (-0.03, 0.216), (-0.10, 0.14)], sx*0.158 - 0.016, sx*0.158 + 0.016, (Y, Z, X), "base"); cyl(0.034, 0.0, 0.012, "sec", 0.003, 16, R=R_to((sx, 0, 0)), at=(sx*0.174, 0, 0.135))
    cyl(0.03, -0.085, 0.085, "glow", 0.004, 16, R=R_to(X), at=(0, 0.185, 0.128))                                           # power cell: lit core in three sleeves
    for x0, x1 in ((-0.11, -0.05), (-0.03, 0.03), (0.05, 0.11)): tube(0.03, 0.05, x0, x1, "base", 0.006, 20, R=R_to(X), at=(0, 0.185, 0.128))
    slab([(0.12, 0.07), (0.25, 0.07), (0.25, 0.10), (0.21, 0.13), (0.12, 0.11)], -0.125, 0.125, (Y, Z, X), "sec")
    node("Pitch", PV, parent="Yaw")
    lathe([(0, -0.07), (0.046, -0.07), (0.07, -0.01), (0.074, 0.14), (0.06, 0.30), (0.047, 0.395), (0, 0.395)], ["dark", "base", "base", "base", "sec", "dark"], 28, crease=60, R=R_to(-Y), at=(0, 0, 0.16)); deform(1, (1.62, 1, 1), (0, 0, 0.16))
    for sx in (-1, 1): sbox(sx*0.108 - 0.012, sx*0.108 + 0.012, -0.30, -0.02, 0.146, 0.174, "dark", 0.004); sbox(sx*0.119 - 0.003, sx*0.119 + 0.003, -0.29, -0.03, 0.156, 0.164, "glow", 0.001)
    slab([(0.05, 0.224), (-0.27, 0.212), (-0.31, 0.222), (-0.27, 0.236), (0.03, 0.236)], -0.012, 0.012, (Y, Z, X), "sec"); cyl(0.032, -0.137, 0.137, "dark", 0.004, 14, R=R_to(X), at=PV)
    mech("Ring", (0, -0.30, 0.16), "Pitch", lambda c, u: T((0, -0.07*(sm(u) if c == "Fire" else 1.0 if c == "Recoil" else 1 - sm(u) if c == "Return" else 0), 0), Y, {"Fire": 240*u, "AimYaw": 360*sm(u), "AimPitch": -360*sm(u), "Idle": 0}.get(c, 240)))
    tube(0.062, 0.071, -0.012, 0.012, "sec", 0.003, 28, R=R_to(Y), at=(0, -0.30, 0.16)); deform(1, (1.5, 1, 1), (0, -0.30, 0.16)); tube(0.071, 0.0735, -0.005, 0.005, "glow", 0.001, 28, R=R_to(Y), at=(0, -0.30, 0.16)); deform(1, (1.5, 1, 1), (0, -0.30, 0.16))
    for sx in (-1, 1):
        mech("Vent" + ("L" if sx < 0 else "R"), (sx*0.118, 0.03, 0.16), "Pitch", lambda c, u, sx=sx: T(ax=Z, deg=-sx*20*(sm(u, 0, 0.5) if c == "Recoil" else 1 - sm(u) if c == "Return" else 0)))
        sbox(sx*0.126 - 0.006, sx*0.126 + 0.006, -0.17, 0.03, 0.128, 0.192, "base", 0.003); sbox(sx*0.133 - 0.002, sx*0.133 + 0.002, -0.15, 0.01, 0.156, 0.164, "glow", 0.001)
    node("Barrel", PV, parent="Pitch")
    for sx in (-1, 1): sbox(sx*0.029 - 0.009, sx*0.029 + 0.009, -0.615, -0.105, 0.104, 0.166, "sec", 0.004)
    sbox(-0.02, 0.02, -0.60, -0.11, 0.127, 0.143, "glow", 0.002)
    for y in (-0.44, -0.585): sbox(-0.039, 0.039, y - 0.014, y + 0.014, 0.100, 0.170, "base", 0.004)
    PD.flash()
@D("Gun", "B")
def gun_b():
    """Brutal autocannon: toothed slewing ring, bolted U-yoke with gussets and trunnion caps, ammunition drum with feed chute, box receiver,
    slotted barrel shroud, recoil buffers, heavy barrel with brake. Mechanisms: charging bolt cycles, drum gear indexes, buffer rods compress."""
    core_box(-0.255, 0.255, -0.255, 0.255, -H, -0.19); port("down")
    tube(0.15, 0.225, -0.19, -0.11, "dark", 0.006, 24); cyl(0.15, -0.19, 0.014, "base", 0.006, 20)
    ring_of(18, lambda R, i: sbox(0.222, 0.246, -0.017, 0.017, -0.18, -0.12, "rust", 0.003, R)); ring_of(10, lambda R, i: bolt(R @ np.array([0.19, 0, -0.11]), Z, 0.013))
    node("Yaw", PV, parent="GunPivot")
    sbox(-0.186, 0.186, -0.13, 0.16, 0.02, 0.056, "base", 0.006)
    for sx in (-1, 1):
        sbox(sx*0.163 - 0.026, sx*0.163 + 0.026, -0.115, 0.13, 0.056, 0.218, "base", 0.006); slab([(-0.115, 0.056), (-0.18, 0.056), (-0.115, 0.15)], sx*0.163 - 0.01, sx*0.163 + 0.01, (Y, Z, X), "dark")
        cyl(0.05, 0.0, 0.012, "metal", 0.004, 14, R=R_to((sx, 0, 0)), at=(sx*0.172, 0, 0.135))
        for a in range(6): bolt((sx*0.1835, 0.036*math.cos(a*math.pi/3), 0.135 + 0.036*math.sin(a*math.pi/3)), (sx, 0, 0), 0.007)
    cyl(0.072, -0.12, 0.12, "sec", 0.006, 16, R=R_to(X), at=(0, 0.176, 0.12))
    for x in (-0.09, 0.0, 0.09): tube(0.07, 0.078, x - 0.01, x + 0.01, "dark", 0.002, 16, R=R_to(X), at=(0, 0.176, 0.12))
    sbox(-0.05, 0.05, 0.06, 0.13, 0.11, 0.17, "dark", 0.006)
    mech("Gear", (0.128, 0.176, 0.12), "Yaw", lambda c, u: T(ax=X, deg={"Fire": 60*sm(u), "Recoil": 60, "Return": 60 + 60*sm(u), "AimYaw": 0, "AimPitch": 0}.get(c, 0)))
    cyl(0.05, 0.0, 0.016, "metal", 0.003, 12, R=R_to(X), at=(0.128, 0.176, 0.12))
    for a in range(6): sbox(0.128, 0.144, -0.012, 0.012, 0.045, 0.066, "metal", 0.003, rot(X, 60*a), (0, 0.176, 0.12))
    node("Pitch", PV, parent="Yaw")
    sbox(-0.115, 0.115, -0.20, 0.076, 0.09, 0.206, "base", 0.008); sbox(-0.075, 0.075, -0.17, 0.05, 0.206, 0.234, "dark", 0.006)
    sbox(-0.078, 0.078, -0.40, -0.20, 0.088, 0.19, "dark", 0.008)
    for y in (-0.37, -0.32, -0.27): sbox(-0.081, 0.081, y - 0.013, y + 0.013, 0.112, 0.166, "rust", 0.003)
    for sx in (-1, 1): cyl(0.022, 0.0, 0.26, "base", 0.004, 10, R=R_to(-Y), at=(sx*0.105, -0.12, 0.125)); bolt((sx*0.116, -0.06, 0.18), (sx, 0, 0), 0.012); bolt((sx*0.116, 0.03, 0.18), (sx, 0, 0), 0.012)
    cyl(0.042, -0.139, 0.139, "metal", 0.005, 12, R=R_to(X), at=PV)
    mech("Bolt", (0.12, -0.05, 0.165), "Pitch", lambda c, u: T((0, 0.065*(bump(u, 0, 0.55, 1.0) if c == "Recoil" else 0.25*bump(u, 0.6, 0.8, 1.0) if c == "Fire" else 0), 0)))
    sbox(0.112, 0.139, -0.085, -0.025, 0.148, 0.186, "metal", 0.005); rod((0.126, -0.03, 0.167), (0.126, 0.05, 0.167), 0.009, "metal", 0.002, 8)
    mech("Buffer", (0, -0.40, 0.125), "Pitch", lambda c, u: T((0, 0.04*(bump(u, 0, 0.4, 1.0) if c == "Recoil" else 0.3*(1 - sm(u)) if c == "Return" else 0), 0)))
    for sx in (-1, 1): cyl(0.013, 0.0, 0.07, "metal", 0.002, 8, R=R_to(-Y), at=(sx*0.105, -0.33, 0.125))
    node("Barrel", PV, parent="Pitch")
    cyl(0.034, 0.0, 0.45, "metal", 0.004, 14, R=R_to(-Y), at=(0, -0.105, 0.135))
    sbox(-0.039, 0.039, -0.618, -0.535, 0.099, 0.171, "dark", 0.006)
    for y in (-0.60, -0.57, -0.553): sbox(-0.04, 0.04, y - 0.005, y + 0.005, 0.118, 0.152, "glow", 0.002)
    PD.flash()
@D("Gun", "C")
def gun_c():
    """Arcade blaster: octagonal pedestal, one wedge cradle, one big accent-coloured receiver with a dark nose, fat light barrel.
    Mechanism: the top slide block snaps back on recoil - one clear, readable move."""
    core_box(-0.255, 0.255, -0.255, 0.255, -H, -0.20); port("down")
    lathe([(0, -0.20), (0.215, -0.20), (0.215, -0.12), (0.15, -0.045), (0.15, 0.014), (0, 0.014)], ["dark", "base", "base", "dark", "dark"], 8, crease=20, phase=math.pi/8)
    node("Yaw", PV, parent="GunPivot")
    slab([(-0.16, 0.02), (0.23, 0.02), (0.23, 0.13), (0.11, 0.216), (-0.05, 0.216), (-0.16, 0.10)], -0.172, 0.172, (Y, Z, X), "base"); slab([(0.115, 0.212), (0.232, 0.127), (0.238, 0.135), (0.118, 0.22)], -0.06, 0.06, (Y, Z, X), "sec")
    node("Pitch", PV, parent="Yaw")
    sbox(-0.125, 0.125, -0.33, 0.072, 0.09, 0.212, "sec", 0.016); sbox(-0.105, 0.105, -0.40, -0.33, 0.10, 0.20, "dark", 0.014)
    mech("Slide", (0, -0.08, 0.224), "Pitch", lambda c, u: T((0, 0.08*(bump(u, 0, 0.45, 1.0) if c == "Recoil" else 0), 0)))
    sbox(-0.085, 0.085, -0.22, 0.03, 0.212, 0.236, "dark", 0.008)
    node("Barrel", PV, parent="Pitch")
    cyl(0.036, 0.0, 0.47, "metal", 0.008, 16, R=R_to(-Y), at=(0, -0.105, 0.135)); tube(0.02, 0.0385, -0.02, 0.02, "sec", 0.006, 16, R=R_to(Y), at=(0, -0.595, 0.135))
    PD.flash()
@D("Gun", "D")
def gun_d():
    """Scrap pipe gun: cut drum base with welded gussets, frame of mismatched side plates and tubes, jerrycan ammunition box with belt, receiver from a
    steel section behind a welded two-plate gun shield, clamped pipe jacket, pipe barrel with a rod cage. Mechanisms: a loose shield plate rattles,
    the hand crank turns unevenly."""
    core_box(-0.255, 0.255, -0.255, 0.255, -H, -0.21); port("down")
    cyl(0.175, -0.21, -0.035, "rust", 0.006, 9); tube(0.173, 0.185, -0.15, -0.13, "metal", 0.002, 9); cyl(0.10, -0.035, 0.014, "metal", 0.004, 8)
    for a, h in ((20, 0.15), (150, 0.12), (265, 0.16)): slab([(0.17, -0.21), (0.25, -0.21), (0.17, -0.21 + h)], -0.008, 0.008, (X, Z, -Y), "patch", R=rot(Z, a))
    node("Yaw", PV, parent="GunPivot")
    slab([(-0.11, 0.03), (0.14, 0.02), (0.16, 0.12), (0.02, 0.215), (-0.08, 0.20)], -0.158, -0.146, (Y, Z, X), "base"); slab([(-0.13, 0.02), (0.11, 0.03), (0.12, 0.17), (-0.02, 0.212), (-0.12, 0.13)], 0.14, 0.152, (Y, Z, X), "patch")
    for (y, z) in ((-0.09, 0.05), (0.10, 0.045), (0.02, 0.20)): rod((-0.15, y, z), (0.145, y, z + 0.004), 0.013, "metal", 0.002, 6)
    sbox(0.01, 0.13, 0.13, 0.25, 0.03, 0.17, "rust", 0.006, rot(Z, 4)); sbox(0.05, 0.09, 0.02, 0.14, 0.15, 0.162, "dark", 0.003); cyl(0.016, 0.17, 0.195, "metal", 0.002, 6, at=(0.07, 0.19, 0))
    sbox(-0.13, -0.03, 0.14, 0.24, 0.03, 0.10, "patch", 0.004, rot(Z, -7))
    mech("Crank", (-0.16, 0.03, 0.12), "Yaw", lambda c, u: T(ax=X, deg={"AimYaw": 360*(u + 0.07*math.sin(6.28*3*u)), "AimPitch": -270*(u + 0.09*math.sin(6.28*2*u))}.get(c, 4*jit(u, 0.2) if c == "Idle" else 0)))
    torus(0.055, 0.008, "metal", 10, 5, R=R_to(X), at=(-0.175, 0.03, 0.12)); rod((-0.175, 0.03, 0.065), (-0.175, 0.03, 0.175), 0.006, "rust", 0.001, 5); rod((-0.175, 0.03, 0.172), (-0.189, 0.03, 0.172), 0.009, "dark", 0.002, 6)
    node("Pitch", PV, parent="Yaw")
    sbox(-0.07, 0.07, -0.17, 0.072, 0.10, 0.19, "metal", 0.005); sbox(-0.085, -0.07, -0.14, 0.04, 0.115, 0.175, "rust", 0.003)
    slab([(-0.135, 0.09), (0.06, 0.088), (0.075, 0.19), (0.0, 0.236), (-0.11, 0.226)], 0.19, 0.203, (X, Z, -Y), "base"); slab([(0.03, 0.10), (0.137, 0.094), (0.132, 0.205), (0.05, 0.218)], 0.203, 0.214, (X, Z, -Y), "patch")
    for (x, z) in ((-0.12, 0.10), (-0.10, 0.215), (0.06, 0.20), (0.125, 0.11), (0.12, 0.195)): bolt((x, -0.203 if x < 0.03 else -0.214, z), -Y, 0.008, "metal")
    cyl(0.046, 0.0, 0.20, "rust", 0.004, 9, R=R_to(-Y), at=(0, -0.20, 0.135))
    for y in (-0.25, -0.33, -0.385): tube(0.044, 0.05, -0.009, 0.009, "metal", 0.002, 9, R=R_to(Y), at=(0, y, 0.135))
    cyl(0.017, 0.0, 0.19, "dark", 0.002, 7, R=R_to(-Y), at=(0.05, -0.02, 0.212)); sbox(0.03, 0.07, -0.09, -0.07, 0.185, 0.226, "rust", 0.002)
    mech("Rattle", (0.10, -0.218, 0.215), "Pitch", lambda c, u: T(ax=Y, deg={"Fire": 9*jit(u, 1.0), "Recoil": 12*jit(u, 0.6, 1.0), "Return": 7*spring(u, 3, 3), "AimYaw": 2.5*jit(u, 0.35), "AimPitch": 3*jit(u, 0.3, 2.0), "Idle": 0.8*math.sin(6.283*u)}.get(c, 0)))
    slab([(0.045, 0.10), (0.125, 0.105), (0.118, 0.212), (0.05, 0.208)], 0.216, 0.224, (X, Z, -Y), "rust"); bolt((0.10, -0.224, 0.20), -Y, 0.009, "metal")
    node("Barrel", PV, parent="Pitch")
    cyl(0.027, 0.0, 0.50, "metal", 0.002, 8, R=R_to(-Y), at=(0, -0.105, 0.135))
    for a in (30, 150, 270): rod(rot(Y, a) @ np.array([0.033, -0.45, 0]) + np.array([0, 0, 0.135]), rot(Y, a) @ np.array([0.033, -0.615, 0]) + np.array([0, 0, 0.135]), 0.004, "rust", 0.001, 4)
    for y in (-0.46, -0.61): tube(0.027, 0.037, -0.006, 0.006, "rust", 0.002, 8, R=R_to(Y), at=(0, y, 0.135))
    PD.flash()
@D("Gun", "E")
def gun_e():
    """Heroic blaster: squat pedestal with a navy roll, cream saddle with capsule shoulder pods and a banded power pack, one fat tapering body that
    flares into a big cream nose bell, stubby barrel with a lit tip. Mechanisms: the nose bell squashes and stretches on every shot, the antenna whips."""
    core_box(-0.255, 0.255, -0.255, 0.255, -H, -0.19); port("down")
    lathe([(0, -0.19), (0.205, -0.19), (0.238, -0.15), (0.228, -0.09), (0.16, -0.03), (0.13, 0.014), (0, 0.014)], ["dark", "base", "base", "base", "sec", "dark"], 32, crease=75); torus(0.214, 0.026, "dark", 32, 10, at=(0, 0, -0.105))
    node("Yaw", PV, parent="GunPivot")
    lathe([(0, 0.02), (0.15, 0.02), (0.188, 0.06), (0.172, 0.10), (0.10, 0.13), (0, 0.13)], ["dark", "sec", "sec", "sec", "sec"], 28, crease=75)
    for sx in (-1, 1):
        lathe([(0, 0), (0.035, 0), (0.05, 0.03), (0.05, 0.17), (0.035, 0.20), (0, 0.20)], ["dark", "base", "base", "base", "dark"], 18, crease=75, R=R_to(-Y), at=(sx*0.138, 0.12, 0.155)); cyl(0.03, 0.0, 0.01, "glow", 0.003, 12, R=R_to(-Y), at=(sx*0.138, -0.082, 0.155))
    lathe([(0, -0.095), (0.05, -0.095), (0.072, -0.06), (0.072, 0.06), (0.05, 0.095), (0, 0.095)], ["sec", "dark", "dark", "dark", "sec"], 20, crease=75, R=R_to(X), at=(0, 0.176, 0.105))
    for x in (-0.035, 0.035): tube(0.07, 0.078, x - 0.01, x + 0.01, "sec", 0.004, 20, R=R_to(X), at=(0, 0.176, 0.105))
    mech("Antenna", (0.05, 0.19, 0.16), "Yaw", lambda c, u: T(ax=X, deg={"Fire": -28*sm(u, 0, 0.5), "Recoil": -28 + 20*u, "Return": -8*spring(u, 2.5, 2.5) - 8*(1 - sm(u, 0, 0.3)), "AimYaw": 14*spring(u, 4, 2.2), "AimPitch": -12*spring(u, 3, 2.0), "Idle": 3*math.sin(6.283*u)}.get(c, 0)))
    rod((0.05, 0.19, 0.16), (0.05, 0.205, 0.196), 0.006, "dark", 0.002, 6); ball(0.013, "glow", 10, 6, at=(0.05, 0.207, 0.202))
    node("Pitch", PV, parent="Yaw")
    lathe([(0, -0.07), (0.05, -0.07), (0.077, -0.01), (0.08, 0.11), (0.064, 0.24), (0.056, 0.30), (0, 0.30)], ["dark", "base", "base", "base", "dark", "dark"], 26, crease=75, R=R_to(-Y), at=(0, 0, 0.16)); deform(1, (1.55, 1, 0.93), (0, 0, 0.16))
    slab([(0.06, 0.226), (-0.12, 0.224), (-0.07, 0.236), (0.04, 0.236)], -0.02, 0.02, (Y, Z, X), "dark")
    for sx in (-1, 1): ball(0.02, "sec", 10, 6, at=(sx*0.118, -0.04, 0.16)); ball(0.02, "sec", 10, 6, at=(sx*0.112, -0.13, 0.16))
    cyl(0.036, -0.138, 0.138, "dark", 0.008, 14, R=R_to(X), at=PV)
    mech("Bell", (0, -0.31, 0.16), "Pitch", lambda c, u: T(s={"Fire": 1 + 0.30*bump(u, 0, 0.35, 1.0) - 0.10*bump(u, 0.5, 0.8, 1.0), "Recoil": 1 - 0.08*(1 - u), "Return": 1 + 0.07*spring(u, 2, 3.5)}.get(c, 1.0)))
    lathe([(0, 0), (0.05, 0), (0.058, 0.02), (0.078, 0.075), (0.074, 0.092), (0.045, 0.092), (0.04, 0.03), (0, 0.03)], ["dark", "sec", "sec", "sec", "dark", "dark", "dark"], 26, crease=75, R=R_to(-Y), at=(0, -0.308, 0.16)); deform(1, (1.5, 1, 0.93), (0, -0.308, 0.16))
    node("Barrel", PV, parent="Pitch")
    lathe([(0, 0.0), (0.03, 0.0), (0.036, 0.04), (0.036, 0.42), (0.03, 0.47), (0.037, 0.50), (0.03, 0.512), (0, 0.512)], ["dark", "metal", "metal", "metal", "sec", "sec", "glow"], 18, crease=75, R=R_to(-Y), at=(0, -0.105, 0.135))
    PD.flash()

# =================================================================================================== WHEEL (Mount / Rotor about +X; clips Idle, Roll, Reverse, Brake)
WP = PD.WP; W0, W1 = PD.W0, PD.W1
def wplate(): core_box(-H, -0.208, -0.25, 0.25, -0.25, 0.25); port("left")
def roll(c, u, k=1.0): return k*math.sin(2*math.pi*2*u) if c in ("Roll", "Reverse") else 0.0
@D("Wheel", "A")
def wheel_a():
    """Sleek: low-profile tyre on a deep turbine rim with twelve swept blades, lit rim channel and a hub cone; smooth fairing arm with a floating
    stator halo. Mechanisms: the halo pulses with the roll and flares on braking, two brake vanes swing in."""
    wplate(); lathe([(0, 0.0), (0.15, 0.0), (0.16, 0.03), (0.085, 0.12), (0.06, 0.262), (0, 0.262)], ["sec", "base", "base", "base", "dark"], 32, crease=60, R=R_to(X), at=(-0.208, 0, 0))
    mech("Halo", (-0.13, 0, 0), None, lambda c, u: T(s=1 + 0.05*roll(c, u) + (0.14*bump(u, 0, 0.3, 1.0) if c == "Brake" else 0)))
    tube(0.17, 0.19, -0.012, 0.012, "sec", 0.004, 36, R=R_to(X), at=(-0.13, 0, 0)); tube(0.19, 0.196, -0.006, 0.006, "glow", 0.002, 36, R=R_to(X), at=(-0.13, 0, 0))
    for sz in (-1, 1):
        mech("Vane" + ("L" if sz < 0 else "U"), (-0.19, 0, sz*0.17), None, lambda c, u, sz=sz: T(ax=Y, deg=sz*22*(bump(u, 0, 0.35, 1.0) if c == "Brake" else 0)))
        slab([(-0.205, 0.15), (-0.06, 0.19), (-0.05, 0.215), (-0.205, 0.235)], -0.07, 0.07, (X, Z, -Y), "base", R=rot(X, 0 if sz > 0 else 180))
    node("Rotor", WP, parent=None)
    with K.group(R=R_to(X), at=WP):
        a0, a1 = -0.115, 0.115
        PD.tyre([(0.25, a0 + 0.012)] + [(0.25 + 0.14*math.sin(t), -0.108*math.cos(t)) for t in np.linspace(0.15, math.pi - 0.15, 9)] + [(0.25, a1 - 0.012)], "dark", 40)
        lathe([(0, a0 + 0.01), (0.21, a0 + 0.01), (0.256, a0 + 0.035), (0.256, a1 - 0.035), (0.235, a1 - 0.012), (0.20, a1 - 0.06), (0.075, a1 - 0.075), (0.05, a1 + 0.03), (0, a1 + 0.04)], ["sec", "sec", "base", "base", "dark", "dark", "base", "base"], 40, crease=50)
        for i in range(12): sbox(0.07, 0.232, -0.006, 0.006, a1 - 0.07, a1 - 0.02, "base", 0.002, rot(Z, 30*i) @ rot(X, 24))
        tube(0.236, 0.256, a1 - 0.014, a1 - 0.006, "glow", 0.002, 40); tube(0.252, 0.259, a0 + 0.03, a0 + 0.04, "glow", 0.002, 40)
    node()
@D("Wheel", "B")
def wheel_b():
    """Brutal: chevron-lug tyre, five-spoke cast rim around a bolted reduction hub, brake disc; A-frame with a real shock absorber and a caliper.
    Mechanisms: the shock rod works with the road and dives on braking, the caliper clamps the disc."""
    wplate()
    for a in (0, 180): slab([(-0.208, 0.06), (-0.208, 0.24), (-0.05, 0.11), (-0.05, 0.06)], -0.016, 0.016, (X, Z, -Y), "base", R=rot(X, a + 90)); slab([(-0.208, 0.06), (-0.208, 0.22), (-0.06, 0.09), (-0.06, 0.06)], -0.012, 0.012, (X, Z, -Y), "dark", R=rot(X, a))
    cyl(0.07, 0.0, 0.158, "base", 0.006, 14, R=R_to(X), at=(-0.208, 0, 0)); tube(0.07, 0.21, 0.0, 0.012, "metal", 0.003, 24, R=R_to(X), at=(-0.118, 0, 0))
    cyl(0.03, 0.0, 0.11, "dark", 0.004, 10, R=rot(Y, 35), at=(-0.195, 0, 0.10))
    mech("Shock", (-0.14, 0, 0.18), None, lambda c, u: T((0.010*roll(c, u, 0.6) + (0.03*bump(u, 0, 0.3, 1.0) if c == "Brake" else 0))*np.array([-0.57, 0, -0.82])))
    cyl(0.016, 0.0, 0.10, "metal", 0.002, 8, R=rot(Y, 215), at=(-0.135, 0, 0.185)); cyl(0.028, 0.0, 0.02, "sec", 0.003, 10, R=rot(Y, 215), at=(-0.135, 0, 0.185))
    mech("Caliper", (-0.11, 0.17, 0.0), None, lambda c, u: T((0, -0.012*(bump(u, 0, 0.2, 1.0) if c == "Brake" else 0), 0)))
    sbox(-0.145, -0.085, 0.14, 0.235, -0.05, 0.05, "sec", 0.008); bolt((-0.085, 0.19, 0.03), X, 0.01); bolt((-0.085, 0.19, -0.03), X, 0.01)
    node("Rotor", WP, parent=None)
    with K.group(R=R_to(X), at=WP):
        PD.tyre([(0.25, W0 + 0.025), (0.33, W0), (0.355, W0 + 0.02), (0.355, W1 - 0.02), (0.33, W1), (0.25, W1 - 0.025)], "dark", 24)
        for i in range(12):
            for sgn in (-1, 1): sbox(0.348, 0.388, -0.036, 0.036, 0.004 if sgn > 0 else W0 + 0.012, W1 - 0.012 if sgn > 0 else -0.004, "dark", 0.004, rot(Z, 30*i + (15 if sgn > 0 else 0)) @ rot(X, 0))
        tube(0.205, 0.258, W0 + 0.03, W1 - 0.03, "base", 0.008, 24); cyl(0.095, W0 + 0.06, W1 - 0.018, "rust", 0.008, 14); cyl(0.045, W1 - 0.018, W1 - 0.002, "metal", 0.004, 10)
        for i in range(5): sbox(0.085, 0.215, -0.03, 0.03, W1 - 0.085, W1 - 0.04, "base", 0.006, rot(Z, 72*i))
        ring_of(10, lambda R, i: bolt(R @ np.array([0.072, 0, W1 - 0.018]), Z, 0.009)); ring_of(5, lambda R, i: bolt(R @ np.array([0.232, 0, W1 - 0.03]), Z, 0.011), 36)
    node()
@D("Wheel", "C")
def wheel_c():
    """Arcade: one chunky dark tyre with six blocks, one solid accent rim with four studs, one block arm. Mechanism: the arm block dips on braking."""
    wplate()
    mech("Arm", (-0.13, 0, 0), None, lambda c, u: T((0, 0, -0.022*(bump(u, 0, 0.3, 1.0) if c == "Brake" else 0) + 0.006*roll(c, u))))
    sbox(-0.208, -0.03, -0.11, 0.11, -0.11, 0.11, "base", 0.016)
    node("Rotor", WP, parent=None)
    with K.group(R=R_to(X), at=WP):
        PD.tyre([(0.21, W0 + 0.04), (0.33, W0), (0.385, W0 + 0.05), (0.385, W1 - 0.05), (0.33, W1), (0.21, W1 - 0.04)], "dark", 24)
        cyl(0.225, W0 + 0.02, W1 - 0.012, "sec", 0.03, 20); cyl(0.085, W1 - 0.012, W1, "dark", 0.004, 14)
        for i in range(4): cyl(0.036, W1 - 0.014, W1 - 0.002, "metal", 0.004, 10, at=rot(Z, 90*i + 45) @ np.array([0.15, 0, 0]))
    node()
@D("Wheel", "D")
def wheel_d():
    """Scrap: recapped tyre with uneven lugs, two welded spike plates and a wrapped chain, rim from a cut gear with a bolted bearing cover; angle-iron
    arm on a leaf-spring pack with a torn mud flap. Mechanisms: the spring pack flexes unevenly, the flap swings and slaps on braking."""
    wplate()
    mech("Spring", (-0.12, 0, -0.10), None, lambda c, u: T((0, 0, 0.010*roll(c, u) + 0.004*jit(u, 0.5) * (c in ("Roll", "Reverse")) - (0.02*bump(u, 0, 0.3, 1.0) if c == "Brake" else 0))))
    for k in range(4): sbox(-0.203, -0.025, -0.19 + k*0.006, 0.19 - k*0.035, -0.135 + k*0.017, -0.123 + k*0.017, "metal", 0.003)
    for y in (-0.10, 0.07): sbox(-0.12, -0.10, y - 0.012, y + 0.012, -0.145, -0.05, "rust", 0.002)
    node()
    sbox(-0.208, -0.035, -0.045, 0.03, -0.07, 0.20, "rust", 0.004, rot(X, 6)); rod((-0.2, 0.17, 0.2), (-0.035, 0.02, 0.05), 0.016, "rust", 0.003, 6); cyl(0.06, 0.0, 0.165, "metal", 0.004, 8, R=R_to(X), at=(-0.208, 0, 0))
    mech("Flap", (-0.19, 0.215, 0.10), None, lambda c, u: T(ax=X, deg={"Roll": 9*math.sin(6.283*2*u + 0.6) + 3*jit(u, 0.4), "Reverse": -8*math.sin(6.283*2*u) + 3*jit(u, 0.4, 1.0), "Brake": 26*spring(u, 2.5, 3.0), "Idle": 1.5*math.sin(6.283*u)}.get(c, 0)))
    slab([(-0.205, 0.10), (-0.07, 0.10), (-0.075, -0.06), (-0.12, -0.10), (-0.20, -0.05)], 0.21, 0.218, (X, Z, -Y), "patch", R=rot(X, 180)); rod((-0.205, 0.214, 0.10), (-0.065, 0.214, 0.10), 0.008, "metal", 0.002, 5)
    node("Rotor", WP, parent=None)
    with K.group(R=R_to(X), at=WP):
        PD.tyre([(0.24, W0 + 0.03), (0.33, W0 + 0.01), (0.365, W0 + 0.04), (0.37, W1 - 0.05), (0.33, W1 - 0.01), (0.24, W1 - 0.03)], "dark", 18)
        for i, a in enumerate((0, 37, 80, 118, 160, 205, 243, 290, 325)): sbox(0.355, 0.386 if i % 3 else 0.372, -0.035, 0.03, W0 + 0.03 + 0.02*(i % 2), W1 - 0.03 - 0.03*((i + 1) % 2), "dark", 0.004, rot(Z, a))
        for a in (58, 225): sbox(0.35, 0.378, -0.07, 0.07, -0.10, 0.10, "rust", 0.003, rot(Z, a)); cone(0.02, 0.003, 0.0, 0.012, "metal", 5, R=rot(Z, a) @ R_to(X), at=rot(Z, a) @ np.array([0.378, 0, 0.0]))
        for i in range(14): torus(0.02, 0.006, "metal", 8, 4, R=rot(Z, i*25.7) @ R_to(Y if i % 2 else X), at=rot(Z, i*25.7) @ np.array([0.36, 0, -0.105]))
        cyl(0.215, W0 + 0.04, W1 - 0.045, "rust", 0.006, 10)
        for i in range(10): sbox(0.20, 0.238, -0.028, 0.028, W1 - 0.07, W1 - 0.045, "metal", 0.003, rot(Z, 36*i))
        lathe([(0, W1 - 0.05), (0.13, W1 - 0.05), (0.11, W1 - 0.012), (0, W1 - 0.006)], "patch", 9, crease=0.5)
        for a in (10, 100, 200, 300): bolt(rot(Z, a) @ np.array([0.09, 0, W1 - 0.03]), Z, 0.012, "metal")
    node()
@D("Wheel", "E")
def wheel_e():
    """Heroic: fat balloon tyre with eight paw treads, deep cream rim, big dome cap with a lit star; stout rounded arm on a coil spring.
    Mechanisms: the coil squashes and overshoots on braking and bounces with the roll."""
    wplate(); lathe([(0, 0.0), (0.15, 0.0), (0.165, 0.045), (0.10, 0.14), (0.085, 0.245), (0, 0.245)], ["dark", "base", "base", "base", "dark"], 24, crease=75, R=R_to(X), at=(-0.208, 0, 0))
    mech("Coil", (-0.12, 0, 0.13), None, lambda c, u: T((0, 0, 0), s=1 + 0.07*roll(c, u) + ({"Brake": -0.24*bump(u, 0, 0.22, 0.6) + 0.12*spring(max(0.0, u - 0.3), 2.5, 4.0)}.get(c, 0))))
    for i in range(4): torus(0.05, 0.016, "metal", 16, 8, at=(-0.12, 0, 0.14 + i*0.027))
    cyl(0.03, 0.12, 0.25, "dark", 0.006, 10, at=(-0.12, 0, 0))
    node("Rotor", WP, parent=None)
    with K.group(R=R_to(X), at=WP):
        PD.tyre([(0.19, W0 + 0.05)] + [(0.215 + 0.165*math.sin(t), -0.168*math.cos(t)) for t in np.linspace(0.1, math.pi - 0.1, 11)] + [(0.19, W1 - 0.05)], "dark", 32)
        for i in range(8): ball(0.058, "dark", 10, 6, at=rot(Z, 45*i) @ np.array([0.333, 0, 0]), squash=1.7)
        lathe([(0, W0 + 0.04), (0.19, W0 + 0.04), (0.205, W1 - 0.07), (0.185, W1 - 0.03), (0.15, W1 - 0.045), (0.11, W1 - 0.045), (0.085, W1 - 0.024), (0, W1 - 0.012)], ["sec", "sec", "sec", "sec", "dark", "base", "base"], 28, crease=75)
        star = [(0.07*math.cos(math.radians(90 + 36*i))*(1 if i % 2 == 0 else 0.48), 0.07*math.sin(math.radians(90 + 36*i))*(1 if i % 2 == 0 else 0.48)) for i in range(10)]
        slab(star, W1 - 0.016, W1 - 0.001, "z", "glow")
    node()

# =================================================================================================== HOVER ENGINE (Housing / Rotor / Vane_1..5 / FX; clips Idle, SpinUp, Lift, Pulse, Cooldown)
RP_ = PD.RP_
def hplate(h=0.05): core_box(-H, H, -H, H, H - h, H); port("up")
def spin(c, u, turns=2.0): return {"SpinUp": 360*turns*u*u, "Lift": 360*turns*u, "Pulse": 360*turns*u, "Cooldown": 360*turns*(1 - (1 - u)**2)}.get(c, 0.0)
def lift(c, u): return {"SpinUp": sm(u), "Lift": 1.0, "Pulse": 1.0, "Cooldown": 1 - sm(u)}.get(c, 0.0)
@D("Hover", "A")
def hover_a():
    """Sleek: wide nacelle with a sunk light channel, shoulder fairing into the mount, stator fins in a dark throat. Mechanisms: three lower iris
    segments extend when the engine spins up, the light halo breathes with the lift and flares on the pulse."""
    hplate(0.045)
    lathe([(0.20, -0.285), (0.212, -0.30), (0.242, -0.292), (0.26, -0.20), (0.257, -0.10), (0.215, 0.0), (0.13, 0.085), (0.12, 0.215), (0.085, 0.215), (0.085, 0.06), (0.17, -0.03), (0.21, -0.12)],
          ["dark", "sec", "base", "base", "base", "base", "base", "dark", "dark", "dark", "dark", "dark"], 40, closed=True, crease=50)
    tube(0.252, 0.262, -0.168, -0.14, "dark", 0.002, 40)
    lathe([(0, -0.19), (0.05, -0.19), (0.08, -0.12), (0.075, 0.07), (0, 0.07)], ["glow", "base", "base", "dark"], 24, crease=60)
    for a in (0, 72, 144, 216, 288): slab([(0.075, -0.17), (0.21, -0.14), (0.21, -0.115), (0.075, -0.07)], -0.005, 0.005, (X, Z, -Y), "sec", R=rot(Z, a))
    mech("Halo", (0, 0, -0.154), None, lambda c, u: T(s=1 + 0.012*lift(c, u)*(1 + math.sin(6.283*(2*u if c == "Lift" else u))) + (0.035*bump(u, 0, 0.3, 1.0) if c == "Pulse" else 0)))
    tube(0.2535, 0.259, -0.163, -0.145, "glow", 0.002, 40)
    for i in range(3):
        mech("Iris%d" % (i + 1), (0, 0, -0.27), None, lambda c, u, i=i: T((0, 0, -0.022*lift(c, sm(u, 0.15*i, 0.6 + 0.15*i) if c == "SpinUp" else u))))
        for k in range(5): sbox(0.208, 0.246, -0.036, 0.036, -0.29, -0.265, "sec", 0.004, rot(Z, 120*i + 14 + 23*k))
    node("Rotor", RP_, parent=None); cyl(0.05, -0.246, -0.198, "dark", 0.006, 16)
    for i in range(9): sbox(0.045, 0.196, -0.018, 0.018, -0.004, 0.004, "metal", 0.0015, rot(Z, 40*i) @ rot(X, 28), (0, 0, -0.222))
    PD.hover_fx()
@D("Hover", "B")
def hover_b():
    """Brutal: welded square frame on four hydraulic legs, flanged duct, finned motor on struts, side gearbox with an exposed spur gear.
    Mechanisms: the leg rods stroke with the lift, the spur gear runs up and coasts down with the engine."""
    hplate(0.06)
    for sx in (-1, 1):
        for sy in (-1, 1): cyl(0.034, -0.20, 0.20, "base", 0.006, 10, at=(sx*0.214, sy*0.214, 0)); tube(0.034, 0.044, -0.215, -0.19, "dark", 0.003, 10, at=(sx*0.214, sy*0.214, 0))
    for z in (-0.05, 0.14):
        for a in (0, 90, 180, 270): sbox(-0.19, 0.19, 0.208, 0.24, z - 0.02, z + 0.02, "dark", 0.004, rot(Z, a))
    tube(0.165, 0.20, -0.30, -0.06, "base", 0.004, 18); tube(0.20, 0.222, -0.30, -0.275, "rust", 0.003, 18); tube(0.20, 0.215, -0.085, -0.06, "rust", 0.003, 18)
    cyl(0.085, -0.18, 0.20, "rust", 0.006, 12)
    for z in (-0.12, -0.07, -0.02, 0.03, 0.08, 0.13): tube(0.08, 0.125, z - 0.008, z + 0.008, "dark", 0.002, 12)
    for a in (0, 90): sbox(-0.168, 0.168, -0.01, 0.01, -0.19, -0.17, "metal", 0.003, rot(Z, a))
    sbox(0.13, 0.25, -0.075, 0.075, -0.02, 0.12, "sec", 0.008)
    mech("Gear", (0.256, 0, 0.05), None, lambda c, u: T(ax=X, deg=spin(c, u, 1.5)))
    cyl(0.055, 0.0, 0.006, "metal", 0.002, 12, R=R_to(X), at=(0.25, 0, 0.05))
    for a in range(8): sbox(0.25, 0.256, -0.01, 0.01, 0.05, 0.068, "metal", 0.002, rot(X, 45*a), (0, 0, 0.05))
    mech("Rods", (0, 0, -0.21), None, lambda c, u: T((0, 0, -0.03*lift(c, u) - (0.012*math.sin(6.283*2*u) if c == "Lift" else 0) - (0.02*bump(u, 0, 0.3, 1.0) if c == "Pulse" else 0))))
    for sx in (-1, 1):
        for sy in (-1, 1): cyl(0.02, -0.285, -0.20, "metal", 0.003, 8, at=(sx*0.214, sy*0.214, 0)); cyl(0.04, -0.287, -0.272, "dark", 0.004, 8, at=(sx*0.214, sy*0.214, 0))
    node("Rotor", RP_, parent=None); cyl(0.06, -0.246, -0.197, "dark", 0.004, 8)
    for i in range(4): sbox(0.05, 0.16, -0.05, 0.05, -0.006, 0.006, "metal", 0.002, rot(Z, 90*i) @ rot(X, 16), (0, 0, -0.221))
    PD.hover_fx()
@D("Hover", "C")
def hover_c():
    """Arcade: one thick ring, one accent column, three struts. Mechanism: the accent collar drops when the engine runs and bobs with the lift."""
    hplate(0.055); tube(0.17, 0.258, -0.315, -0.07, "base", 0.03, 24); cyl(0.085, -0.19, 0.21, "dark", 0.02, 16)
    for a in (0, 120, 240): sbox(0.07, 0.20, -0.03, 0.03, -0.11, -0.07, "dark", 0.012, rot(Z, a))
    mech("Collar", (0, 0, 0.12), None, lambda c, u: T((0, 0, -0.10*lift(c, u) + (0.02*math.sin(6.283*2*u) if c == "Lift" else 0) + (0.04*bump(u, 0, 0.3, 1.0) if c == "Pulse" else 0))))
    tube(0.085, 0.15, 0.09, 0.15, "sec", 0.016, 20)
    node("Rotor", RP_, parent=None); cyl(0.07, -0.246, -0.198, "sec", 0.012, 14)
    for i in range(3): sbox(0.05, 0.165, -0.045, 0.045, -0.008, 0.008, "metal", 0.003, rot(Z, 120*i) @ rot(X, 14), (0, 0, -0.222))
    PD.hover_fx()
@D("Hover", "D")
def hover_d():
    """Scrap: duct from an oil drum with hoops and a welded patch, three uneven pipe struts, a salvaged motor with belt drive strapped to the side.
    Mechanisms: the motor shakes and coughs on spin-up, a loose flap on the drum rattles while it flies."""
    hplate(0.05)
    tube(0.205, 0.236, -0.31, -0.02, "metal", 0.004, 10)
    for z in (-0.25, -0.13): tube(0.234, 0.244, z - 0.012, z + 0.012, "rust", 0.002, 10)
    sbox(0.10, 0.20, -0.08, 0.08, -0.22, -0.08, "patch", 0.003, rot(Z, 20))
    for (a, x) in ((15, 0.22), (140, 0.215), (250, 0.225)): rod(rot(Z, a) @ np.array([x, 0, -0.03]), rot(Z, a) @ np.array([0.06, 0, 0.205]), 0.017, "rust", 0.003, 6)
    cyl(0.06, -0.19, 0.205, "metal", 0.004, 8); cyl(0.03, 0.0, 0.02, "metal", 0.003, 7, at=(0, 0, 0.10))
    mech("Motor", (0.14, -0.15, 0.13), None, lambda c, u: T((0.004*jit(u, 1.0)*lift(c, u) + (0.007*jit(u, 2.0, 1.0)*bump(u, 0, 0.3, 0.8) if c == "SpinUp" else 0), 0, 0.003*jit(u, 1.3, 2.0)*lift(c, u)), X, 2.5*jit(u, 0.8, 3.0)*lift(c, u)))
    sbox(0.08, 0.20, -0.20, -0.09, 0.06, 0.20, "patch", 0.005, rot(Z, -8)); cyl(0.045, 0.0, 0.03, "rust", 0.004, 8, R=R_to(-Y), at=(0.14, -0.20, 0.12)); rod((0.14, -0.215, 0.12), (0.0, -0.06, 0.11), 0.007, "dark", 0.001, 4)
    mech("Flap", (-0.238, 0.0, -0.06), None, lambda c, u: T(ax=Y, deg=(10*jit(u, 0.7) + 8)*lift(c, u) + (18*bump(u, 0, 0.3, 1.0) if c == "Pulse" else 0)))
    slab([(-0.07, -0.06), (0.08, -0.07), (0.07, -0.19), (-0.05, -0.20)], -0.25, -0.24, (Y, Z, X), "rust"); bolt((-0.25, -0.05, -0.065), -X, 0.008, "metal"); bolt((-0.25, 0.06, -0.068), -X, 0.008, "metal")
    node("Rotor", RP_, parent=None); cyl(0.05, -0.246, -0.198, "rust", 0.004, 7)
    for i, (a, r1, w) in enumerate(((0, 0.19, 0.04), (70, 0.175, 0.05), (150, 0.19, 0.035), (215, 0.15, 0.045), (290, 0.185, 0.04))): sbox(0.04, r1, -w, w, -0.005, 0.005, "metal" if i % 2 else "rust", 0.002, rot(Z, a) @ rot(X, 18), (0, 0, -0.222))
    PD.hover_fx()
@D("Hover", "E")
def hover_e():
    """Heroic: fat doughnut duct with a cream lip and a lit ring, mushroom hub with a navy cap, three stout arms.
    Mechanisms: the cream lip squashes and stretches with the lift and pops on the pulse, the cap bobs."""
    hplate(0.055)
    lathe([(0.185 + 0.073*math.cos(t), -0.185 + 0.105*math.sin(t)) for t in np.linspace(0, 2*math.pi, 14, endpoint=False)], "base", 32, closed=True, crease=80); torus(0.185, 0.02, "dark", 32, 8, at=(0, 0, -0.082))
    lathe([(0, -0.19), (0.06, -0.19), (0.07, -0.02), (0.125, 0.05), (0.125, 0.10), (0.085, 0.14), (0, 0.14)], ["dark", "sec", "sec", "sec", "sec", "sec"], 24, crease=75)
    for a in (90, 210, 330): rod(rot(Z, a) @ np.array([0.19, 0, -0.10]), rot(Z, a) @ np.array([0.09, 0, 0.08]), 0.032, "sec", 0.012, 10)
    mech("Cap", (0, 0, 0.14), None, lambda c, u: T((0, 0, 0.012*lift(c, u)*(1 + math.sin(6.283*(2*u if c == "Lift" else u))) + (0.03*bump(u, 0, 0.25, 0.7) if c == "Pulse" else 0))))
    lathe([(0, 0.13), (0.10, 0.13), (0.11, 0.16), (0.07, 0.20), (0, 0.208)], ["dark", "dark", "dark", "dark"], 22, crease=75)
    mech("Lip", (0, 0, -0.29), None, lambda c, u: T(s=1 + 0.03*lift(c, u)*math.sin(6.283*(2*u if c == "Lift" else u)) + {"Pulse": 0.10*bump(u, 0, 0.25, 0.6) - 0.05*bump(u, 0.5, 0.75, 1.0), "SpinUp": 0.05*spring(u, 2, 2.5)}.get(c, 0)))
    lathe([(0.15, -0.275), (0.165, -0.305), (0.215, -0.317), (0.245, -0.30), (0.24, -0.275)], ["sec", "sec", "glow", "sec", "dark"], 32, closed=True, crease=75)
    node("Rotor", RP_, parent=None); ball(0.06, "sec", 14, 8, at=(0, 0, -0.222), squash=0.4)
    for i in range(4): sbox(0.04, 0.17, -0.05, 0.05, -0.008, 0.008, "base", 0.006, rot(Z, 90*i) @ rot(X, 16), (0, 0, -0.222))
    PD.hover_fx()

# =================================================================================================== BATTERY (clips Idle, Activate (loop), Hit)
def biface(h, pad): core_box(-H, H, -H, H, -H, -H + h); port("down"); core_box(-pad[0], pad[0], pad[1], H, pad[2], pad[3]); port("back", "partial", 0.14, (0, 0, -0.07))
def act(c, u, k=1.0): return math.sin(2*math.pi*k*u) if c == "Activate" else 0.0
@D("Battery", "A")
def bat_a():
    """Sleek reactor cell: capsule on two swept pylons, armoured end sleeves with emitter caps, a wide open core section. Mechanisms: the centre
    sleeve with four studs rotates while the cell works, the lit core breathes."""
    biface(0.045, (0.09, 0.16, -0.20, 0.03)); C0 = (0, -0.045, 0.035)
    for sx in (-1, 1):
        slab([(-0.16, -0.215), (0.12, -0.215), (0.10, -0.11), (0.045, -0.04), (-0.075, -0.04), (-0.14, -0.11)], sx*0.172 - 0.022, sx*0.172 + 0.022, (Y, Z, X), "base")
        tube(0.06, 0.118, 0.0, 0.105, "base", 0.012, 32, R=R_to((sx, 0, 0)), at=(sx*0.125, C0[1], C0[2])); lathe([(0, 0), (0.118, 0), (0.09, 0.022), (0.05, 0.03), (0, 0.03)], ["base", "sec", "sec", "glow"], 32, crease=60, R=R_to((sx, 0, 0)), at=(sx*0.23, C0[1], C0[2]))
        tube(0.119, 0.123, 0.03, 0.04, "glow", 0.001, 32, R=R_to((sx, 0, 0)), at=(sx*0.125, C0[1], C0[2]))
    sbox(-0.15, 0.15, -0.09, 0.0, -0.215, -0.19, "dark", 0.006); sbox(-0.12, 0.12, C0[1] - 0.02, C0[1] + 0.02, 0.158, 0.172, "sec", 0.004)
    mech("Core", C0, None, lambda c, u: T(s=1 + 0.07*act(c, u, 3) + (0.02*math.sin(6.283*u) if c == "Idle" else 0)))
    cyl(0.055, -0.125, 0.125, "glow", 0.006, 24, R=R_to(X), at=C0)
    mech("Sleeve", C0, None, lambda c, u: T(ax=X, deg=360*u if c == "Activate" else 0))
    tube(0.075, 0.108, -0.04, 0.04, "base", 0.01, 32, R=R_to(X), at=C0)
    for a in range(4): sbox(-0.03, 0.03, -0.014, 0.014, 0.106, 0.118, "sec", 0.003, rot(X, 90*a + 45), C0)
    body()
@D("Battery", "B")
def bat_b():
    """Brutal accumulator: finned transformer block, two ribbed insulator stacks with a copper busbar, hazard-painted fan housing.
    Mechanisms: the cooling fan runs, the relay plunger hammers while the bank is active."""
    biface(0.07, (0.10, 0.10, -0.185, 0.06))
    sbox(-0.165, 0.165, -0.19, 0.07, -0.19, 0.075, "base", 0.008); sbox(-0.175, 0.175, -0.20, 0.08, 0.075, 0.095, "dark", 0.006)
    for sx in (-1, 1):
        for y in (-0.15, -0.095, -0.04, 0.015): sbox(sx*0.19 - 0.03, sx*0.19 + 0.03, y - 0.011, y + 0.011, -0.175, 0.06, "dark", 0.003)
        lathe([(0, 0.095), (0.05, 0.095), (0.05, 0.11), (0.034, 0.12), (0.05, 0.135), (0.034, 0.15), (0.05, 0.165), (0.034, 0.18), (0.045, 0.195), (0.022, 0.205), (0.022, 0.235), (0, 0.235)], "metal", 14, crease=20, at=(sx*0.09, -0.06, 0))
        bolt((sx*0.09, -0.06, 0.235), Z, 0.02)
    rod((-0.09, -0.06, 0.225), (0.09, -0.06, 0.225), 0.014, "sec", 0.003, 8)
    sbox(-0.11, 0.11, -0.225, -0.19, -0.16, 0.05, "sec", 0.006); tube(0.07, 0.09, 0.0, 0.02, "dark", 0.003, 16, R=R_to(-Y), at=(0, -0.225, -0.055))
    mech("Fan", (0, -0.232, -0.055), None, lambda c, u: T(ax=Y, deg=720*u if c == "Activate" else 90*u if c == "Idle" else 0))
    cyl(0.022, 0.0, 0.018, "metal", 0.003, 8, R=R_to(-Y), at=(0, -0.225, -0.055))
    for a in range(6): sbox(0.018, 0.066, -0.004, 0.004, -0.014, 0.014, "metal", 0.002, rot(Y, 60*a) @ rot(X, 25), (0, -0.236, -0.055))
    mech("Relay", (0, 0.03, 0.095), None, lambda c, u: T((0, 0, 0.02*max(0.0, act(c, u, 4)))))
    sbox(-0.04, 0.04, 0.0, 0.06, 0.095, 0.15, "rust", 0.006); cyl(0.012, 0.15, 0.175, "metal", 0.002, 6, at=(0, 0.03, 0))
    body()
@D("Battery", "C")
def bat_c():
    """Arcade cell: one accent-coloured cell in a dark cradle with two clamps and a three-step charge window. Mechanism: the terminal cap pumps."""
    biface(0.06, (0.10, 0.12, -0.20, 0.02)); sbox(-0.21, 0.21, -0.23, 0.10, -0.20, -0.10, "dark", 0.016)
    cyl(0.155, -0.10, 0.15, "sec", 0.03, 22, at=(0, -0.065, 0)); cyl(0.115, 0.15, 0.19, "dark", 0.012, 18, at=(0, -0.065, 0))
    for sx in (-1, 1): sbox(sx*0.185 - 0.03, sx*0.185 + 0.03, -0.12, -0.01, -0.10, 0.06, "base", 0.014)
    for k in range(3): sbox(-0.035, 0.035, -0.226, -0.214, -0.05 + k*0.055, -0.01 + k*0.055, "glow", 0.004)
    mech("Cap", (0, -0.065, 0.19), None, lambda c, u: T((0, 0, 0.022*(0.5 - 0.5*math.cos(6.283*2*u)) if c == "Activate" else 0)))
    cyl(0.06, 0.19, 0.235, "metal", 0.012, 14, at=(0, -0.065, 0))
    body()
@D("Battery", "D")
def bat_d():
    """Scrap power pack: strapped truck battery with filler caps and a cracked gauge, jerrycan cell, rebar cage, jumper cables.
    Mechanisms: the gauge needle swings erratically, the jumper clamp twitches on its terminal."""
    biface(0.05, (0.10, 0.07, -0.20, 0.03))
    sbox(-0.215, 0.045, -0.215, 0.06, -0.21, 0.04, "sec", 0.006); sbox(-0.22, 0.05, -0.22, 0.065, 0.04, 0.062, "dark", 0.004)
    for x in (-0.16, -0.02): sbox(x - 0.016, x + 0.016, -0.223, 0.068, -0.21, 0.07, "rust", 0.003)
    for k in range(5): cyl(0.014, 0.062, 0.078, "dark", 0.002, 6, at=(-0.19 + k*0.05, 0.0, 0))
    for (x, g) in ((-0.17, "metal"), (-0.03, "rust")): cyl(0.02, 0.062, 0.105, g, 0.003, 7, at=(x, -0.14, 0))
    sbox(0.075, 0.225, -0.20, 0.0, -0.21, 0.12, "rust", 0.008, rot(Z, 5)); cyl(0.028, 0.12, 0.155, "metal", 0.004, 7, at=(0.16, -0.14, 0)); sbox(0.10, 0.20, -0.215, -0.203, -0.10, 0.03, "patch", 0.003, rot(Z, 5))
    for (x, y) in ((-0.235, -0.235), (0.24, -0.235), (0.24, 0.05), (-0.235, 0.05)): rod((x, y, -0.21), (x*0.97, y*0.97, 0.19), 0.008, "metal", 0.002, 5)
    rod((-0.228, -0.228, 0.19), (0.233, -0.228, 0.185), 0.008, "metal", 0.002, 5); rod((0.233, -0.228, 0.185), (0.233, 0.048, 0.19), 0.008, "rust", 0.002, 5); rod((-0.228, 0.048, 0.19), (-0.228, -0.228, 0.19), 0.008, "metal", 0.002, 5)
    rod((-0.03, -0.14, 0.10), (0.05, -0.19, 0.15), 0.008, "dark", 0.001, 5); rod((0.05, -0.19, 0.15), (0.15, -0.15, 0.15), 0.008, "dark", 0.001, 5)
    cyl(0.05, 0.0, 0.02, "patch", 0.003, 9, R=R_to(-Y), at=(-0.085, -0.222, -0.08))
    mech("Needle", (-0.085, -0.245, -0.08), None, lambda c, u: T(ax=Y, deg={"Activate": 35*math.sin(6.283*u) + 22*jit(u, 0.25), "Idle": 3*math.sin(6.283*u), "Hit": 60*spring(u, 3, 3)}.get(c, 0)))
    sbox(-0.089, -0.081, -0.248, -0.243, -0.08, -0.038, "glow", 0.001)
    mech("Clamp", (-0.17, -0.14, 0.105), None, lambda c, u: T(ax=Y, deg={"Activate": 7*jit(u, 0.3, 1.0), "Hit": 25*spring(u, 3, 3)}.get(c, 0)))
    slab([(-0.03, 0.0), (0.03, 0.0), (0.045, 0.055), (0.0, 0.03), (-0.045, 0.055)], -0.012, 0.012, (X, Z, -Y), "rust", org=(-0.17, -0.14, 0.105)); rod((-0.17, -0.14, 0.15), (-0.10, -0.20, 0.17), 0.008, "rust", 0.001, 5)
    body()
@D("Battery", "E")
def bat_e():
    """Heroic power jug: one big round-shouldered capsule with a navy belt and cap and a lit bolt emblem, on two cream feet.
    Mechanisms: the whole jug bounces and swells to the beat while active, the emblem pulses."""
    biface(0.07, (0.10, 0.14, -0.19, 0.03)); J = (0, -0.05, -0.19)
    for sx in (-1, 1): ball(0.05, "sec", 12, 8, at=(sx*0.15, -0.19, -0.165), squash=0.7)
    mech("Jug", J, None, lambda c, u: T((0, 0, 0.014*abs(act(c, u, 2))), s={"Activate": 1 + 0.035*act(c, u, 4), "Hit": 1 - 0.08*bump(u, 0, 0.25, 1.0) + 0.04*spring(u, 3, 4)}.get(c, 1.0)))
    lathe([(0, 0), (0.12, 0), (0.165, 0.05), (0.176, 0.19), (0.15, 0.29), (0.10, 0.35), (0.075, 0.36), (0, 0.36)], ["dark", "base", "base", "base", "sec", "sec", "dark"], 30, crease=75, at=J)
    torus(0.174, 0.022, "dark", 30, 10, at=(J[0], J[1], -0.02)); lathe([(0, 0.36), (0.08, 0.36), (0.085, 0.40), (0.05, 0.43), (0, 0.435)], ["dark", "dark", "dark", "metal"], 20, crease=75, at=J)
    mech("Emblem", (0, -0.232, 0.045), "Jug", lambda c, u: T(s=1 + (0.22*(0.5 - 0.5*math.cos(6.283*4*u)) if c == "Activate" else 0.05*math.sin(6.283*u) if c == "Idle" else 0)))
    slab([(0.03, 0.10), (-0.06, -0.012), (-0.008, -0.012), (-0.036, -0.10), (0.06, 0.018), (0.008, 0.018), (0.05, 0.10)], 0.218, 0.238, (X, Z, -Y), "glow", org=(0, 0, 0.045))
    body()

# =================================================================================================== EXPLOSIVE TRAP (world object; clips Idle, Arm, Triggered, Recovery; node Lamp)
def trap_k(c, u): return {"Arm": sm(u), "Triggered": 1.0, "Recovery": 1 - sm(u, 0, 0.5)}.get(c, 0.0)
def lamp(fn):
    if not K._GRP: node("Lamp", (0, 0, 0.10), dict(type="slide", axis=(0, 0, 1), amount=0.015, frames=24))
    fn(); body()
@D("Trap", "A")
def trap_a():
    """Sleek lotus mine: thin disc with a light ring and three armour petals. Mechanisms: the petals lift when it arms and snap flat on the trigger."""
    lathe([(0, 0), (0.17, 0), (0.2, 0.012), (0.2, 0.026), (0.13, 0.05), (0, 0.055)], ["dark", "sec", "sec", "base", "base"], 36, crease=50); tube(0.197, 0.203, 0.013, 0.023, "glow", 0.002, 36)
    for i, a in enumerate((30, 150, 270)):
        t = rot(Z, a) @ np.array([0, 1.0, 0]); mech("Petal%d" % (i + 1), tuple(rot(Z, a) @ np.array([0.105, 0, 0.034])), None, lambda c, u, t=t: T(ax=t, deg=-38*({"Arm": sm(u), "Triggered": 1 - sm(u, 0, 0.25)*1.12, "Recovery": -0.12*(1 - sm(u, 0, 0.3))}.get(c, 0))))
        slab([(0.10, -0.06), (0.235, -0.034), (0.255, 0.0), (0.235, 0.034), (0.10, 0.06)], 0.028, 0.042, "z", "base", R=rot(Z, a)); sbox(0.13, 0.235, -0.004, 0.004, 0.041, 0.045, "glow", 0.001, rot(Z, a))
    body(); lamp(lambda: lathe([(0, 0.05), (0.07, 0.05), (0.064, 0.08), (0.035, 0.10), (0, 0.105)], "glow", 22, crease=70))
@D("Trap", "B")
def trap_b():
    """Brutal contact mine: bolted flange, toothed locking collar, steel sphere with a belt and five horns. Mechanisms: the collar turns and locks
    when it arms, the horns push out and stay out until it recovers."""
    cyl(0.205, 0.0, 0.03, "dark", 0.004, 14); ring_of(8, lambda R, i: bolt(R @ np.array([0.172, 0, 0.03]), Z, 0.014)); cyl(0.07, 0.03, 0.07, "metal", 0.004, 10)
    ball(0.135, "base", 18, 9, at=(0, 0, 0.165)); torus(0.137, 0.014, "dark", 18, 4, at=(0, 0, 0.165))
    mech("Collar", (0, 0, 0.05), None, lambda c, u: T(ax=Z, deg=90*trap_k(c, u)))
    tube(0.085, 0.12, 0.032, 0.068, "rust", 0.004, 12); ring_of(6, lambda R, i: sbox(0.115, 0.145, -0.02, 0.02, 0.036, 0.064, "rust", 0.003, R))
    mech("Horns", (0, 0, 0.165), None, lambda c, u: T(s=1 + 0.13*trap_k(c, u) + (0.10*bump(u, 0, 0.2, 0.8) if c == "Triggered" else 0)))
    def horn(R, i):
        d = R @ np.array([math.cos(math.radians(35)), 0, math.sin(math.radians(35))]); c0 = np.array([0, 0, 0.165]); rod(c0 + d*0.11, c0 + d*0.168, 0.02, "metal", 0.004, 8); cyl(0.028, 0.0, 0.012, "dark", 0.003, 8, R=R_to(d), at=c0 + d*0.166)
    ring_of(5, horn); body(); lamp(lambda: cyl(0.035, 0.288, 0.30, "glow", 0.003, 10))
@D("Trap", "C")
def trap_c():
    """Arcade puck: flat body, accent ring, big button. Mechanism: the ring rises when armed and drops the moment it is triggered."""
    cyl(0.225, 0.0, 0.06, "base", 0.02, 24); cyl(0.20, 0.0, 0.012, "dark", 0.003, 20)
    mech("Ring", (0, 0, 0.06), None, lambda c, u: T((0, 0, 0.04*({"Arm": sm(u), "Triggered": 1 - sm(u, 0, 0.2)}.get(c, 0)))))
    tube(0.13, 0.20, 0.045, 0.078, "sec", 0.012, 24)
    body(); lamp(lambda: lathe([(0, 0.06), (0.115, 0.06), (0.105, 0.10), (0.06, 0.13), (0, 0.135)], "glow", 18, crease=70))
@D("Trap", "D")
def trap_d():
    """Scrap charge: banded gas bottle with nails on crossed planks, a tin-can detonator on a wire, a pressure plate cut from a road sign.
    Mechanisms: the plate rocks and slams, the aerial wire whips."""
    sbox(-0.23, 0.21, -0.11, 0.10, 0.0, 0.02, "patch", 0.004, rot(Z, 12)); sbox(-0.10, 0.12, -0.23, 0.22, 0.018, 0.034, "rust", 0.004, rot(Z, -20))
    cyl(0.085, 0.03, 0.22, "rust", 0.006, 9, at=(0.03, 0.03, 0)); lathe([(0, 0.22), (0.06, 0.22), (0.03, 0.26), (0.02, 0.285), (0, 0.285)], "metal", 7, crease=0.5, at=(0.03, 0.03, 0))
    for z in (0.08, 0.17): tube(0.083, 0.092, z - 0.008, z + 0.008, "metal", 0.002, 9, at=(0.03, 0.03, 0))
    for i, a in enumerate((10, 55, 120, 170, 230, 300)): cone(0.008, 0.002, 0.0, 0.07, "metal", 5, R=rot(Z, a) @ rot(Y, 75 + 8*(i % 3)), at=rot(Z, a) @ np.array([0.085, 0, 0.10 + 0.03*(i % 2)]) + np.array([0.03, 0.03, 0]))
    cyl(0.035, 0.03, 0.10, "metal", 0.003, 7, at=(-0.14, 0.10, 0)); rod((-0.14, 0.10, 0.10), (-0.04, 0.05, 0.19), 0.006, "dark", 0.001, 4)
    mech("Plate", (-0.10, -0.13, 0.04), None, lambda c, u: T(ax=(1, 0.3, 0), deg={"Arm": 7*sm(u) + 2*jit(u, 0.4), "Triggered": 7 - 16*sm(u, 0, 0.2) + 5*spring(u, 3, 3), "Recovery": -9*(1 - sm(u, 0, 0.4)) + 2*jit(u, 0.3)*(1 - u)}.get(c, 0)))
    slab(ngon(8, 0.105, 22.5), 0.036, 0.046, "z", "sec", org=(-0.10, -0.13, 0)); sbox(-0.14, -0.06, -0.14, -0.12, 0.045, 0.049, "glow", 0.001)
    mech("Wire", (0.03, 0.03, 0.285), None, lambda c, u: T(ax=(1, 1, 0), deg={"Arm": 6*jit(u, 0.5), "Triggered": 30*spring(u, 3, 2.5), "Recovery": 8*spring(u, 4, 3), "Idle": 2*math.sin(6.283*u)}.get(c, 0)))
    rod((0.03, 0.03, 0.283), (0.035, 0.03, 0.299), 0.004, "dark", 0.001, 4)
    body(); lamp(lambda: sbox(-0.155, -0.125, 0.085, 0.115, 0.10, 0.115, "glow", 0.004))
@D("Trap", "E")
def trap_e():
    """Heroic star popper: chunky star on five cream feet, navy crown star, big lit dome. Mechanisms: the crown star winds up and swells when
    armed, pops and spins on the trigger and springs back."""
    star = lambda r, k, ph=90: [(r*math.cos(math.radians(ph + 36*i))*(1 if i % 2 == 0 else k), r*math.sin(math.radians(ph + 36*i))*(1 if i % 2 == 0 else k)) for i in range(10)]
    slab(star(0.25, 0.56), 0.0, 0.05, "z", "base")
    for i in range(5): ball(0.052, "sec", 12, 8, at=(0.205*math.cos(math.radians(90 + 72*i)), 0.205*math.sin(math.radians(90 + 72*i)), 0.05), squash=0.8)
    lathe([(0, 0.045), (0.125, 0.045), (0.135, 0.075), (0, 0.075)], "sec", 24, crease=75)
    mech("Crown", (0, 0, 0.06), None, lambda c, u: T(ax=Z, deg={"Arm": 36*sm(u), "Triggered": 36 + 72*sm(u, 0, 0.5), "Recovery": 108 - 108*sm(u, 0, 0.6) + 10*spring(u, 3, 3)}.get(c, 0), s={"Arm": 1 + 0.10*sm(u), "Triggered": 1.1 + 0.28*bump(u, 0, 0.2, 0.9), "Recovery": 1 + 0.10*(1 - sm(u, 0, 0.5))}.get(c, 1.0)))
    slab(star(0.17, 0.5, 126), 0.05, 0.072, "z", "dark")
    body(); lamp(lambda: lathe([(0, 0.07), (0.10, 0.07), (0.098, 0.12), (0.058, 0.165), (0, 0.18)], "glow", 22, crease=70))

# =================================================================================================== EXPLOSIVE TRAP DEPLOYER (clips Idle, Deploy [Open 27 / Out 63 / Reset 85], Hit; node Payload)
def dface(h, pad):
    core_box(-H, H, -H, H, -H, -H + h); port("down"); core_box(-pad[0], pad[0], pad[1], H, pad[2], pad[3]); port("back", "partial", 0.2, (0, 0, 0.01)); empty("Deploy_Out", (0, 0.26, 0.0), (0, 1, 0))
def payload(style, s, at, R=None):
    with K.group(R=R, s=s, at=at, lod=True): P["Trap"][style]()
    node()
def dk(c, u): return sm(u, 0.02, 0.30)*(1 - sm(u, 0.80, 0.98)) if c == "Deploy" else 0.0        # open ... closed again (Open event at u = 0, Reset at 0.8)
def kick(c, u): return bump(u, 0.46, 0.53, 0.78) if c == "Deploy" else 0.0                       # the shove at the Deploy_Out event (u = 0.5)
@D("Deployer", "A")
def dep_a():
    """Sleek mag-cradle: two swept rails with light strips, lift pad with a light ring, rear pad with a fin. Mechanisms: two clamp arms swing open
    before release and close on reset, the lift pad kicks at the release."""
    dface(0.045, (0.11, 0.17, -0.20, 0.14)); slab([(0.17, 0.14), (0.26, 0.14), (0.26, 0.175), (0.215, 0.215)], -0.03, 0.03, (Y, Z, X), "sec"); sbox(-0.07, 0.07, 0.163, 0.171, -0.03, 0.10, "glow", 0.002)
    for sx in (-1, 1):
        slab([(-0.25, -0.215), (0.14, -0.215), (0.14, -0.07), (0.03, -0.02), (-0.21, -0.11)], sx*0.20 - 0.018, sx*0.20 + 0.018, (Y, Z, X), "base"); sbox(sx*0.20 - 0.021, sx*0.20 + 0.021, -0.18, 0.05, -0.006, 0.002, "glow", 0.001, rot(X, 20), (0, 0, -0.078))
        mech("Clamp" + ("L" if sx < 0 else "R"), (sx*0.185, -0.07, -0.05), None, lambda c, u, sx=sx: T(ax=Y, deg=-sx*55*dk(c, u)))
        pr = [(0.185, -0.05), (0.20, -0.02), (0.13, 0.035), (0.055, 0.04), (0.05, 0.02), (0.12, 0.012)]; slab(pr if sx > 0 else [(-x, z) for (x, z) in reversed(pr)], 0.03, 0.11, (X, Z, -Y), "sec")
    body(); sbox(-0.18, 0.18, -0.24, 0.13, -0.215, -0.17, "dark", 0.006)
    mech("Lift", (0, -0.07, -0.17), None, lambda c, u: T((0, 0, 0.010*dk(c, u) + 0.035*kick(c, u))))
    cyl(0.125, -0.17, -0.142, "sec", 0.006, 28, at=(0, -0.07, 0)); tube(0.125, 0.132, -0.15, -0.142, "glow", 0.002, 28, at=(0, -0.07, 0))
    body(); payload("A", 0.55, (0, -0.07, -0.142))
@D("Deployer", "B")
def dep_b():
    """Brutal ram chute: two frame beams, five rollers, hydraulic ram on a bracket, two latch hooks, sprocket drive. Mechanisms: the hooks open, the
    ram shoves the mine off the rollers at the release and retracts, the sprocket indexes the next one."""
    dface(0.075, (0.13, 0.17, -0.185, 0.14))
    for z in (0.02, 0.10): sbox(-0.137, 0.137, 0.158, 0.172, z - 0.014, z + 0.014, "dark", 0.003)
    for sx in (-1, 1): sbox(sx*0.215 - 0.03, sx*0.215 + 0.03, -0.25, 0.25, -0.185, -0.085, "base", 0.006); bolt((sx*0.246, -0.2, -0.13), (sx, 0, 0), 0.012); bolt((sx*0.246, 0.2, -0.13), (sx, 0, 0), 0.012)
    for k in range(5): cyl(0.02, -0.185, 0.185, "metal", 0.003, 8, R=R_to(X), at=(0, -0.10 + k*0.06, -0.125))
    sbox(-0.07, 0.07, -0.25, -0.21, -0.185, 0.0, "dark", 0.006); cyl(0.046, 0.0, 0.12, "rust", 0.005, 12, R=R_to(Y), at=(0, -0.25, -0.04)); tube(0.046, 0.056, 0.10, 0.12, "dark", 0.003, 12, R=R_to(Y), at=(0, -0.25, -0.04))
    mech("Ram", (0, -0.13, -0.04), None, lambda c, u: T((0, 0.09*kick(c, u), 0)))
    cyl(0.02, 0.0, 0.07, "metal", 0.003, 8, R=R_to(Y), at=(0, -0.14, -0.04)); sbox(-0.09, 0.09, -0.075, -0.055, -0.105, 0.02, "sec", 0.005)
    for sx in (-1, 1):
        mech("Hook" + ("L" if sx < 0 else "R"), (sx*0.185, 0.04, -0.085), None, lambda c, u, sx=sx: T(ax=Y, deg=-sx*42*dk(c, u)))
        pr = [(0.185, -0.085), (0.20, -0.04), (0.13, 0.03), (0.10, 0.02), (0.14, -0.05)]; slab(pr if sx > 0 else [(-x, z) for (x, z) in reversed(pr)], -0.06, -0.02, (X, Z, -Y), "metal")
    mech("Sprocket", (0.249, -0.12, -0.03), None, lambda c, u: T(ax=X, deg=(90*sm(u, 0, 0.3) + 90*sm(u, 0.8, 1.0)) if c == "Deploy" else 0))
    cyl(0.05, 0.0, 0.008, "metal", 0.002, 10, R=R_to(X), at=(0.247, -0.12, -0.03))
    for a in range(8): sbox(0.247, 0.255, -0.009, 0.009, 0.045, 0.062, "metal", 0.002, rot(X, 45*a), (0, -0.12, -0.03))
    body(); payload("B", 0.45, (0, 0.03, -0.105))
@D("Deployer", "C")
def dep_c():
    """Arcade ramp: one wedge, one dark rear block, one accent gate. Mechanisms: the gate drops flat before release, the kicker pad pops."""
    dface(0.06, (0.12, 0.14, -0.20, 0.13)); slab([(-0.25, -0.20), (0.14, -0.20), (0.14, -0.10), (-0.25, -0.16)], -0.21, 0.21, (Y, Z, X), "base")
    mech("Gate", (0, -0.24, -0.165), None, lambda c, u: T(ax=X, deg=85*dk(c, u))); sbox(-0.21, 0.21, -0.256, -0.226, -0.165, -0.075, "sec", 0.01)
    mech("Kicker", (0, -0.05, -0.13), None, lambda c, u: T((0, 0, 0.035*kick(c, u)))); cyl(0.11, -0.145, -0.115, "sec", 0.01, 18, R=rot(X, 8.7), at=(0, -0.05, 0))
    body(); payload("C", 0.6, (0, -0.05, -0.113), rot(X, 8.7))
@D("Deployer", "D")
def dep_d():
    """Scrap catapult: angle-iron frame, drum magazine, leaf spring, throwing arm with a basket cut from sheet. Mechanisms: the latch shakes loose,
    the arm snaps up at the release and wobbles down."""
    dface(0.05, (0.11, 0.19, -0.21, 0.13))
    for sx in (-1, 1): rod((sx*0.21, -0.24, -0.20), (sx*0.20, 0.17, -0.19), 0.016, "metal" if sx < 0 else "rust", 0.003, 6)
    for y in (-0.20, 0.0): rod((-0.21, y, -0.195), (0.21, y + 0.01, -0.195), 0.012, "rust", 0.002, 5)
    cyl(0.085, -0.21, 0.07, "rust", 0.006, 9, at=(-0.16, 0.09, 0)); tube(0.083, 0.092, -0.07, -0.05, "metal", 0.002, 9, at=(-0.16, 0.09, 0)); sbox(0.09, 0.24, 0.02, 0.15, -0.21, -0.12, "patch", 0.004, rot(Z, 8))
    for k in range(3): sbox(-0.04, 0.04, -0.18 + k*0.02, 0.10 - k*0.04, -0.19 + k*0.014, -0.18 + k*0.014, "metal", 0.002)
    mech("Arm", (0.02, 0.12, -0.14), None, lambda c, u: T(ax=X, deg=(48*bump(u, 0.46, 0.53, 0.7) + 9*spring(max(0.0, u - 0.53)*2.2, 3, 3)*(u > 0.53) + 3*jit(u, 0.3)*dk(c, u)) if c == "Deploy" else 0))
    rod((0.02, 0.12, -0.14), (0.02, -0.07, -0.12), 0.014, "metal", 0.003, 6); slab([(-0.10, -0.17), (0.13, -0.18), (0.14, -0.02), (0.02, 0.03), (-0.09, -0.01)], -0.125, -0.115, "z", "patch")
    mech("Latch", (0.20, -0.12, -0.12), None, lambda c, u: T(ax=Y, deg=(40*dk(c, u) + 5*jit(u, 0.6)*dk(c, u)) if c == "Deploy" else 0))
    slab([(0.20, -0.12), (0.215, -0.07), (0.15, -0.045), (0.14, -0.065), (0.185, -0.085)], -0.14, -0.10, (X, Z, -Y), "rust")
    body(); payload("D", 0.42, (0.02, -0.08, -0.114))
@D("Deployer", "E")
def dep_e():
    """Heroic flipper: red bowl on a stout stem, big cream paddle on a coil hinge, rounded rear pad. Mechanisms: the paddle winds back, slaps
    through at the release with overshoot, the coil squashes and rings."""
    dface(0.07, (0.12, 0.15, -0.19, 0.12)); lathe([(0, 0.0), (0.095, 0.0), (0.085, 0.035), (0.045, 0.06), (0, 0.066)], ["dark", "dark", "dark", "dark"], 20, crease=75, at=(0, 0.162, 0.12))
    lathe([(0, -0.19), (0.07, -0.19), (0.06, -0.13), (0.155, -0.10), (0.182, -0.055), (0.168, -0.045), (0.135, -0.085), (0, -0.095)], ["dark", "dark", "base", "base", "sec", "dark", "dark"], 28, crease=75, at=(0, -0.07, 0))
    mech("Coil", (0, 0.105, -0.19), None, lambda c, u: T(s=(1 - 0.22*sm(u, 0.05, 0.4)*(1 - sm(u, 0.46, 0.5)) + 0.2*bump(u, 0.46, 0.52, 0.62) + 0.08*spring(max(0.0, u - 0.52)*2.0, 3, 3)*(u > 0.52)) if c == "Deploy" else 1.0))
    for i in range(4): torus(0.045, 0.015, "metal", 16, 8, at=(0, 0.105, -0.175 + i*0.026))
    mech("Paddle", (0, 0.105, -0.075), None, lambda c, u: T(ax=X, deg=(22*sm(u, 0.05, 0.4)*(1 - sm(u, 0.46, 0.5)) - 62*bump(u, 0.46, 0.52, 0.72) - 10*spring(max(0.0, u - 0.52)*2.0, 3, 3)*(u > 0.52)) if c == "Deploy" else 0))
    lathe([(0, 0), (0.03, 0), (0.03, 0.03), (0, 0.03)], "dark", 12, crease=75, R=R_to(X), at=(-0.015, 0.105, -0.075))
    slab([(0.105, -0.085), (0.125, -0.06), (0.11, 0.07), (0.07, 0.115), (0.035, 0.115), (0.06, 0.06), (0.075, -0.07)], -0.07, 0.07, (Y, Z, X), "sec")
    body(); payload("E", 0.5, (0, -0.07, -0.088))
