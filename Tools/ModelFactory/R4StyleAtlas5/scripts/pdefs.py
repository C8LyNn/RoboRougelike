"""#1239 pilot 2: eight asset types, each with FIVE independently designed constructions (one function per style).
Shared between the five styles of a type: only the game contract - envelope, contact faces, node names / pivots, sockets, clip timeline.
Everything else (mechanical idea, part list, proportions, detail layout) is authored separately per style."""
import math, json, os
import numpy as np
import akit as K
from akit import box, sbx, sbox, prism, srev, hull, lathe, cyl, rod, tube, torus, ball, cone, slab, node, rot, R_to, X, Y, Z, ngon, arc2, port, empty, frame
import adefs as AD
H = K.H; INV = AD.INV
P = {}                                              # P[type][style] = fn
def D(typ, style):
    def deco(fn): P.setdefault(typ, {})[style] = fn; return fn
    return deco
def ring_of(n, fn, phase=0.0):
    for i in range(n): fn(rot(Z, phase + 360.0*i/n), i)
def glow_seams(pos):
    """A: light strips lying in the panel seams of a full cube (2 mm under the contact plane)"""
    for d in K.DIRS.values():
        u, v, n = frame(d); R = np.stack([u, v, n], axis=1)
        for p in pos:
            sbox(-0.2, 0.2, p - 0.0016, p + 0.0016, H - 0.0045, H - 0.002, "glow", 0.0005, R)
            sbox(p - 0.0016, p + 0.0016, -0.2, 0.2, H - 0.0045, H - 0.002, "glow", 0.0005, R)

# =================================================================================================== 1. CUBE
@D("Cube", "A")
def cube_a(): sbx(-H, H, -H, H, -H, H, sa=(0.1375,), sb=(0.1375,), st=(0.1375,)); glow_seams((0.1375,))
@D("Cube", "B")
def cube_b(): sbx(-H, H, -H, H, -H, H, sa=(-0.0275, 0.0275), sb=(-0.0275, 0.0275), st=(-0.0275, 0.0275))      # cross channel between four bolted plates
@D("Cube", "C")
def cube_c(): box(-H, H, -H, H, -H, H)
@D("Cube", "D")
def cube_d(): sbx(-H, H, -H, H, -H, H, sa=(0.0825,), sb=(-0.1375,), st=(-0.0825, 0.1375))                      # unequal sheets
@D("Cube", "E")
def cube_e(): box(-H, H, -H, H, -H, H)

# =================================================================================================== 2. STRUT (R4C_STRUT_STANDARD: node A at 0, node B at y = -1.1)
YB = -1.1
def nodes2(seams=None):
    for y in (0.0, YB):
        if seams: sbx(-H, H, y - H, y + H, -H, H, **{k: tuple((v + y) if k == "sb" else v for v in vs) for k, vs in seams.items()})
        else: box(-H, H, y - H, y + H, -H, H)
@D("Strut", "A")
def strut_a():
    nodes2(dict(sa=(0.1375,), sb=(0.1375,), st=(0.1375,)))
    prism(ngon(6, 0.125, 30), 0.25, 0.85, (X, Z, -Y), s=0.6)
    for sx in (-1, 1): sbox(sx*0.11 - 0.004, sx*0.11 + 0.004, -0.80, -0.30, -0.006, 0.006, "glow", 0.002)
    for y in (-0.31, -0.79): tube(0.118, 0.15, -0.02, 0.02, "sec", 0.008, 24, R=R_to(Y), at=(0, y, 0))
    tube(0.128, 0.134, -0.006, 0.006, "glow", 0.002, 24, R=R_to(Y), at=(0, -0.55, 0))
@D("Strut", "B")
def strut_b():
    nodes2(dict(sa=(-0.0275, 0.0275), sb=(-0.0275, 0.0275), st=(-0.0275, 0.0275)))
    for z in (-0.19, 0.19): sbox(-0.18, 0.18, -0.85, -0.25, z - 0.022, z + 0.022, "base", 0.006)
    sbox(-0.016, 0.016, -0.85, -0.25, -0.17, 0.17, "base", 0.004)
    for y in (-0.40, -0.55, -0.70):
        for sx in (-1, 1): sbox(sx*0.095 - 0.08, sx*0.095 + 0.08, y - 0.008, y + 0.008, -0.168, 0.168, "dark", 0.003)
    for sx in (-1, 1):
        rod((sx*0.15, -0.28, -0.16), (sx*0.15, -0.82, 0.16), 0.014, "metal", 0.003, 6); rod((sx*0.15, -0.28, 0.16), (sx*0.15, -0.82, -0.16), 0.014, "metal", 0.003, 6)
        for y in (-0.275, -0.825):
            for z in (-0.19, 0.19): lathe([(0, 0), (0.016, 0), (0.016, 0.01), (0.01, 0.016), (0, 0.016)], "bolt", 6, crease=1.0, R=R_to((0, 0, np.sign(z))), at=(sx*0.13, y, z + np.sign(z)*0.02))
@D("Strut", "C")
def strut_c():
    nodes2(); cyl(0.17, 0.0, 0.62, "sec", 0.03, 20, R=R_to(-Y), at=(0, -0.24, 0))
@D("Strut", "D")
def strut_d():
    nodes2(dict(sa=(0.0825,), sb=(-0.1375,), st=(-0.0825, 0.1375)))
    rod((0.06, -0.24, 0.07), (0.06, -0.86, 0.07), 0.085, "metal", 0.006, 9); rod((-0.10, -0.24, -0.09), (-0.10, -0.86, -0.11), 0.055, "rust", 0.004, 7)
    sbox(-0.17, 0.16, -0.50, -0.38, -0.03, 0.0, "patch", 0.004, rot(Y, 28)); sbox(-0.16, 0.15, -0.72, -0.64, -0.02, 0.01, "rust", 0.004, rot(Y, -18))
    rod((-0.16, -0.27, 0.18), (0.14, -0.83, -0.17), 0.012, "rust", 0.002, 5)
    for i in range(6): torus(0.022, 0.006, "metal", 8, 4, R=R_to(X if i % 2 else Y), at=(-0.17, -0.34 - i*0.036, 0.16 - 0.03*math.sin(i*0.9)))
@D("Strut", "E")
def strut_e():
    nodes2()
    pr = [(0, 0.0), (0.20, 0.0), (0.215, 0.05), (0.10, 0.22), (0.085, 0.30), (0.10, 0.38), (0.215, 0.55), (0.20, 0.60), (0, 0.60)]
    lathe(pr, ["sec", "base", "base", "base", "base", "base", "base", "sec"], 24, crease=60, R=R_to(-Y), at=(0, -0.25, 0))
    torus(0.105, 0.028, "sec", 24, 10, R=R_to(Y), at=(0, -0.55, 0))

# =================================================================================================== 3. BASIC GUN (rig of R4W_Basic: Base / Yaw / Pitch / Barrel / MuzzleFlash)
PV = (0, 0, 0.135)
def flash(): node("MuzzleFlash", (0, -0.624, 0.135), parent="Pitch"); lathe([(0, 0.0), (0.045, 0.03), (0.02, 0.06), (0, 0.13)], "glow", 8, crease=10, R=R_to(-Y), at=(0, -0.624, 0.135)); node()
@D("Gun", "A")
def gun_a():
    box(-0.255, 0.255, -0.255, 0.255, -H, -0.215); port("down")
    lathe([(0, -0.215), (0.235, -0.215), (0.21, -0.12), (0.12, -0.02), (0.10, 0.014), (0, 0.014)], ["sec", "base", "base", "sec", "dark"], 32, crease=50)
    tube(0.20, 0.206, -0.135, -0.125, "glow", 0.002, 32)
    node("Yaw", PV, parent="GunPivot")
    lathe([(0, 0.02), (0.185, 0.02), (0.185, 0.04), (0.14, 0.085), (0.05, 0.10), (0, 0.10)], ["dark", "sec", "base", "base", "base"], 32, crease=50)
    tube(0.186, 0.19, 0.045, 0.052, "glow", 0.002, 32)
    for sx in (-1, 1): slab([(-0.10, 0.08), (0.11, 0.08), (0.06, 0.215), (-0.03, 0.215)], sx*0.16 - 0.012, sx*0.16 + 0.012, (Y, Z, X), "base")
    node("Pitch", PV, parent="Yaw")
    lathe([(0, -0.07), (0.07, -0.07), (0.075, 0.0), (0.06, 0.20), (0.04, 0.395), (0, 0.395)], ["dark", "base", "base", "sec", "dark"], 6, crease=40, R=R_to(-Y), at=(0, 0, 0.16), phase=math.pi/6)
    slab([(0.06, 0.215), (-0.30, 0.19), (-0.30, 0.205), (0.02, 0.235)], -0.008, 0.008, (Y, Z, X), "sec")
    for sx in (-1, 1): sbox(sx*0.066 - 0.003, sx*0.066 + 0.003, -0.30, 0.03, 0.155, 0.165, "glow", 0.001)
    cyl(0.03, -0.136, 0.136, "dark", 0.004, 12, R=R_to(X), at=PV)
    node("Barrel", PV, parent="Pitch")
    cyl(0.016, 0.0, 0.50, "metal", 0.003, 12, R=R_to(-Y), at=(0, -0.105, 0.135))
    for y in (-0.42, -0.47, -0.52, -0.57): tube(0.016, 0.03, -0.006, 0.006, "glow", 0.002, 14, R=R_to(Y), at=(0, y, 0.135))
    cone(0.024, 0.006, 0.0, 0.03, "metal", 12, R=R_to(-Y), at=(0, -0.588, 0.135))
    flash()
@D("Gun", "B")
def gun_b():
    box(-0.255, 0.255, -0.255, 0.255, -H, -0.16); port("down")
    tube(0.13, 0.215, -0.16, -0.02, "dark", 0.008, 20); cyl(0.13, -0.16, 0.014, "base", 0.006, 20)
    ring_of(12, lambda R, i: lathe([(0, 0), (0.013, 0), (0.013, 0.008), (0.008, 0.012), (0, 0.012)], "bolt", 6, crease=1.0, at=R @ np.array([0.175, 0, -0.02])))
    for a in (45, 135, 225, 315): slab([(0.13, -0.16), (0.24, -0.16), (0.13, -0.03)], -0.012, 0.012, (X, Z, -Y), "dark", R=rot(Z, a))
    node("Yaw", PV, parent="GunPivot")
    sbox(-0.185, 0.185, -0.12, 0.14, 0.02, 0.05, "base", 0.006)
    for sx in (-1, 1):
        sbox(sx*0.165 - 0.022, sx*0.165 + 0.022, -0.10, 0.12, 0.05, 0.215, "base", 0.006)
        slab([(-0.12, 0.05), (-0.10, 0.05), (-0.10, 0.14)], sx*0.165 - 0.01, sx*0.165 + 0.01, (Y, Z, X), "dark")
        lathe([(0, 0), (0.03, 0), (0.03, 0.012), (0.02, 0.018), (0, 0.018)], "bolt", 6, crease=1.0, R=R_to((sx, 0, 0)), at=(sx*0.187, 0, 0.135) if abs(sx*0.187 + sx*0.004) < 0.191 else (sx*0.172, 0, 0.135))
    sbox(-0.13, 0.13, 0.15, 0.25, 0.05, 0.19, "sec", 0.008)
    for z in (0.09, 0.13, 0.17): sbox(-0.135, 0.135, 0.245, 0.256, z - 0.012, z + 0.012, "dark", 0.003)
    rod((0.09, 0.10, 0.06), (0.09, -0.06, 0.10), 0.022, "metal", 0.004, 8)
    node("Pitch", PV, parent="Yaw")
    sbox(-0.11, 0.11, -0.19, 0.075, 0.09, 0.20, "base", 0.008)
    sbox(-0.14, -0.11, -0.12, 0.05, 0.09, 0.17, "sec", 0.005)
    for y in (-0.22, -0.255, -0.29, -0.325, -0.36): tube(0.034, 0.05, -0.01, 0.01, "dark", 0.003, 12, R=R_to(Y), at=(0, y, 0.135))
    cyl(0.04, 0.0, 0.21, "metal", 0.004, 12, R=R_to(-Y), at=(0, -0.19, 0.135))
    sbox(-0.06, 0.06, -0.16, 0.05, 0.20, 0.234, "dark", 0.005); cyl(0.04, -0.138, 0.138, "metal", 0.005, 10, R=R_to(X), at=PV)
    node("Barrel", PV, parent="Pitch")
    cyl(0.03, 0.0, 0.45, "metal", 0.004, 12, R=R_to(-Y), at=(0, -0.105, 0.135))
    sbox(-0.039, 0.039, -0.618, -0.545, 0.10, 0.17, "dark", 0.006)
    for y in (-0.60, -0.57): sbox(-0.04, 0.04, y - 0.006, y + 0.006, 0.12, 0.15, "glow", 0.002)
    flash()
@D("Gun", "C")
def gun_c():
    box(-0.255, 0.255, -0.255, 0.255, -H, -0.17); port("down"); cyl(0.17, -0.17, 0.014, "sec", 0.03, 20)
    node("Yaw", PV, parent="GunPivot"); sbox(-0.17, 0.17, -0.13, 0.20, 0.02, 0.12, "base", 0.03)
    node("Pitch", PV, parent="Yaw"); sbox(-0.12, 0.12, -0.36, 0.07, 0.09, 0.23, "sec", 0.035)
    node("Barrel", PV, parent="Pitch"); cyl(0.036, 0.0, 0.50, "base", 0.012, 14, R=R_to(-Y), at=(0, -0.105, 0.135)); tube(0.02, 0.039, -0.02, 0.02, "sec", 0.008, 14, R=R_to(Y), at=(0, -0.58, 0.135))
    flash()
@D("Gun", "D")
def gun_d():
    box(-0.255, 0.255, -0.255, 0.255, -H, -0.20); port("down")
    cyl(0.16, -0.20, -0.04, "metal", 0.006, 9); sbox(-0.22, 0.05, 0.10, 0.22, -0.20, -0.12, "rust", 0.004, rot(Z, 9)); cyl(0.07, -0.04, 0.014, "rust", 0.004, 8)
    rod((0.20, -0.2, -0.2), (0.09, -0.05, -0.05), 0.014, "rust", 0.002, 5)
    node("Yaw", PV, parent="GunPivot")
    sbox(-0.16, 0.14, -0.10, 0.12, 0.02, 0.042, "patch", 0.004, rot(Z, -4))
    for (x, y) in ((-0.15, -0.08), (0.14, -0.06), (0.15, 0.10), (-0.14, 0.12)): rod((x, y, 0.03), (x*0.95, y*0.3, 0.21), 0.013, "metal", 0.002, 6)
    rod((-0.15, 0.03, 0.20), (0.15, 0.03, 0.205), 0.012, "metal", 0.002, 6)
    cyl(0.055, 0.045, 0.19, "rust", 0.005, 8, at=(0.07, 0.19, 0)); cyl(0.02, 0.19, 0.215, "metal", 0.003, 6, at=(0.07, 0.19, 0))
    sbox(-0.17, -0.07, 0.13, 0.24, 0.04, 0.12, "patch", 0.004, rot(Z, 6))
    node("Pitch", PV, parent="Yaw")
    cyl(0.05, 0.0, 0.24, "metal", 0.004, 8, R=R_to(-Y), at=(0, 0.07, 0.14))
    slab([(-0.135, 0.09), (0.02, 0.09), (0.035, 0.20), (-0.06, 0.232), (-0.135, 0.20)], 0.20, 0.212, (X, Z, -Y), "rust", R=rot(Z, 0))
    slab([(0.04, 0.10), (0.13, 0.11), (0.12, 0.19), (0.05, 0.17)], 0.185, 0.196, (X, Z, -Y), "patch")
    cyl(0.018, 0.0, 0.20, "dark", 0.002, 7, R=R_to(-Y), at=(0.045, -0.02, 0.21)); sbox(0.02, 0.07, -0.10, -0.08, 0.14, 0.225, "rust", 0.002)
    for y in (-0.22, -0.30, -0.37): tube(0.03, 0.045, -0.02, 0.02, "rust", 0.003, 8, R=R_to(Y), at=(0, y, 0.135))
    node("Barrel", PV, parent="Pitch")
    cyl(0.024, 0.0, 0.51, "metal", 0.002, 8, R=R_to(-Y), at=(0, -0.105, 0.135))
    for y in (-0.43, -0.455, -0.48): tube(0.022, 0.03, -0.006, 0.006, "rust", 0.002, 7, R=R_to(Y), at=(0, y, 0.135))
    sbox(-0.008, 0.008, -0.61, -0.50, 0.158, 0.17, "rust", 0.002)
    flash()
@D("Gun", "E")
def gun_e():
    box(-0.255, 0.255, -0.255, 0.255, -H, -0.17); port("down")
    lathe([(0, -0.17), (0.20, -0.17), (0.22, -0.10), (0.15, 0.0), (0.10, 0.014), (0, 0.014)], ["sec", "base", "base", "sec", "dark"], 28, crease=70)
    node("Yaw", PV, parent="GunPivot")
    lathe([(0, 0.02), (0.13, 0.02), (0.15, 0.07), (0.10, 0.12), (0, 0.12)], ["dark", "base", "base", "sec"], 24, crease=70)
    for sx in (-1, 1): cyl(0.078, 0.0, 0.062, "base", 0.022, 18, R=R_to((sx, 0, 0)), at=(sx*0.10, 0.0, 0.135)); cyl(0.04, 0.0, 0.024, "sec", 0.008, 14, R=R_to((sx, 0, 0)), at=(sx*0.162, 0.0, 0.135)); sbox(sx*0.13 - 0.03, sx*0.13 + 0.03, -0.05, 0.05, 0.06, 0.13, "base", 0.02)
    sbox(-0.09, 0.09, 0.12, 0.25, 0.03, 0.13, "sec", 0.035)
    node("Pitch", PV, parent="Yaw")
    lathe([(0, -0.07), (0.05, -0.07), (0.072, -0.02), (0.072, 0.08), (0.05, 0.20), (0.047, 0.39), (0, 0.39)], ["sec", "base", "base", "base", "sec", "dark"], 22, crease=70, R=R_to(-Y), at=(0, 0, 0.16))
    slab([(0.07, 0.20), (-0.10, 0.20), (-0.02, 0.234), (0.06, 0.234)], -0.016, 0.016, (Y, Z, X), "sec")
    torus(0.05, 0.016, "sec", 20, 8, R=R_to(Y), at=(0, -0.38, 0.16))
    node("Barrel", PV, parent="Pitch")
    lathe([(0, 0.0), (0.036, 0.0), (0.028, 0.44), (0.038, 0.50), (0.03, 0.512), (0, 0.512)], ["dark", "base", "sec", "sec", "glow"], 16, crease=70, R=R_to(-Y), at=(0, -0.105, 0.135))
    flash()

# =================================================================================================== 4. WHEEL (rig of R4M_WheelM_Side_Offroad: Mount / Rotor about +X, pivot (0.022, 0, 0), tyre radius 0.39)
WP = (0.022, 0, 0); W0, W1 = -0.170, 0.170
def wmount(): box(-H, -0.205, -0.25, 0.25, -0.25, 0.25); port("left")
def tyre(profile, grp="dark", N=28): lathe(profile, grp, N, closed=True, crease=40)
@D("Wheel", "A")
def wheel_a():
    wmount(); lathe([(0, 0.0), (0.13, 0.0), (0.07, 0.10), (0.05, 0.26), (0, 0.26)], ["sec", "base", "base", "dark"], 28, crease=60, R=R_to(X), at=(-0.205, 0, 0))
    tube(0.125, 0.13, 0.004, 0.012, "glow", 0.002, 28, R=R_to(X), at=(-0.205, 0, 0))
    node("Rotor", WP, parent=None)
    with K.group(R=R_to(X), at=WP):
        a0, a1 = -0.10, 0.10                                    # a narrow low-profile tyre on a deep aero disc (the tyre radius is the shared rolling radius)
        tyre([(0.24, a0 + 0.012)] + [(0.24 + 0.15*math.sin(t), -0.095*math.cos(t)) for t in np.linspace(0.15, math.pi - 0.15, 9)] + [(0.24, a1 - 0.012)], "dark", 36)
        lathe([(0, a0 + 0.01), (0.20, a0 + 0.01), (0.247, a0 + 0.03), (0.247, a1 - 0.03), (0.20, a1 - 0.01), (0.06, a1 + 0.02), (0, a1 + 0.02)], ["base", "base", "sec", "base", "base", "sec"], 36, crease=50)
        tube(0.243, 0.251, a1 - 0.034, a1 - 0.027, "glow", 0.002, 36); tube(0.243, 0.251, a0 + 0.027, a0 + 0.034, "glow", 0.002, 36)
    node()
@D("Wheel", "B")
def wheel_b():
    wmount()
    for a in (0, 90, 180, 270): slab([(-0.205, 0.07), (-0.205, 0.24), (-0.06, 0.07)], -0.012, 0.012, (X, Z, -Y), "dark", R=rot(X, a))
    cyl(0.075, 0.0, 0.15, "metal", 0.006, 12, R=R_to(X), at=(-0.205, 0, 0)); tube(0.06, 0.19, 0.0, 0.014, "metal", 0.003, 20, R=R_to(X), at=(-0.115, 0, 0))
    sbox(-0.15, -0.08, -0.05, 0.05, 0.13, 0.215, "sec", 0.006)
    ring_of(8, lambda R, i: lathe([(0, 0), (0.014, 0), (0.014, 0.01), (0.009, 0.014), (0, 0.014)], "bolt", 6, crease=1.0, R=R_to(X), at=(-0.205, 0.21*math.cos(i*math.pi/4), 0.21*math.sin(i*math.pi/4))))
    node("Rotor", WP, parent=None)
    with K.group(R=R_to(X), at=WP):
        tyre([(0.25, W0 + 0.025), (0.335, W0), (0.36, W0 + 0.02), (0.36, W1 - 0.02), (0.335, W1), (0.25, W1 - 0.025)], "dark", 24)
        for i in range(16): sbox(0.352, 0.388, -0.03, 0.03, W0 + 0.01, W1 - 0.01, "dark", 0.004, rot(Z, 22.5*i))
        tube(0.215, 0.255, W0 + 0.03, W1 - 0.03, "base", 0.008, 24); cyl(0.075, W0 + 0.06, W1 - 0.01, "metal", 0.008, 12)
        for i in range(8): sbox(0.07, 0.22, -0.016, 0.016, W1 - 0.07, W1 - 0.035, "base", 0.004, rot(Z, 45*i)); lathe([(0, 0), (0.012, 0), (0.012, 0.008), (0.008, 0.012), (0, 0.012)], "bolt", 6, crease=1.0, at=rot(Z, 45*i + 22.5) @ np.array([0.235, 0, W1 - 0.03]))
    node()
@D("Wheel", "C")
def wheel_c():
    wmount(); cyl(0.10, 0.0, 0.20, "sec", 0.03, 16, R=R_to(X), at=(-0.205, 0, 0))
    node("Rotor", WP, parent=None)
    with K.group(R=R_to(X), at=WP):
        tyre([(0.20, W0 + 0.04), (0.33, W0), (0.385, W0 + 0.05), (0.385, W1 - 0.05), (0.33, W1), (0.20, W1 - 0.04)], "dark", 20)
        cyl(0.21, W0 + 0.02, W1 - 0.01, "base", 0.03, 18)
        for i in range(5): cyl(0.04, W1 - 0.012, W1, "sec", 0.004, 10, at=rot(Z, 72*i) @ np.array([0.125, 0, 0]))
    node()
@D("Wheel", "D")
def wheel_d():
    wmount()
    for k in range(4): sbox(-0.2, -0.02, -0.19 + k*0.004, 0.19 - k*0.03, -0.13 + k*0.017, -0.118 + k*0.017, "metal", 0.003)
    sbox(-0.205, -0.03, -0.04, 0.03, -0.10, 0.20, "rust", 0.004, rot(X, 7)); rod((-0.2, 0.16, 0.19), (-0.03, 0.02, 0.04), 0.016, "rust", 0.003, 6)
    cyl(0.06, 0.0, 0.17, "metal", 0.004, 8, R=R_to(X), at=(-0.205, 0, 0))
    node("Rotor", WP, parent=None)
    with K.group(R=R_to(X), at=WP):
        tyre([(0.24, W0 + 0.03), (0.33, W0 + 0.01), (0.365, W0 + 0.04), (0.37, W1 - 0.05), (0.33, W1 - 0.01), (0.24, W1 - 0.03)], "dark", 18)
        for i, a in enumerate((0, 37, 80, 118, 160, 205, 243, 290, 325)): sbox(0.355, 0.385 if i % 3 else 0.372, -0.035, 0.03, W0 + 0.03 + 0.02*(i % 2), W1 - 0.03 - 0.03*((i + 1) % 2), "dark", 0.004, rot(Z, a))
        for a in (20, 140, 265): cone(0.02, 0.003, 0.0, 0.035, "metal", 6, R=rot(Z, a) @ R_to(X), at=rot(Z, a) @ np.array([0.352, 0, 0.05]))
        for i in range(14): torus(0.02, 0.006, "rust", 8, 4, R=rot(Z, i*25.7) @ R_to(Y if i % 2 else X), at=rot(Z, i*25.7) @ np.array([0.358, 0, -0.085]))
        cyl(0.215, W0 + 0.04, W1 - 0.04, "rust", 0.006, 10); lathe([(0, W1 - 0.04), (0.16, W1 - 0.04), (0.13, W1 - 0.012), (0, W1 - 0.008)], "patch", 9, crease=0.5)
        for a in (10, 130, 200, 300): cyl(0.014, W1 - 0.03, W1 - 0.004, "metal", 0.002, 6, at=rot(Z, a) @ np.array([0.185, 0, 0]))
    node()
@D("Wheel", "E")
def wheel_e():
    wmount(); lathe([(0, 0.0), (0.16, 0.0), (0.17, 0.05), (0.09, 0.16), (0.07, 0.24), (0, 0.24)], ["sec", "base", "base", "sec", "dark"], 24, crease=70, R=R_to(X), at=(-0.205, 0, 0))
    node("Rotor", WP, parent=None)
    with K.group(R=R_to(X), at=WP):
        tyre([(0.19, W0 + 0.05)] + [(0.215 + 0.165*math.sin(t), -0.168*math.cos(t)) for t in np.linspace(0.1, math.pi - 0.1, 11)] + [(0.19, W1 - 0.05)], "dark", 32)
        for i in range(8): ball(0.055, "dark", 10, 6, at=rot(Z, 45*i) @ np.array([0.335, 0, 0]), squash=1.6)
        lathe([(0, W0 + 0.04), (0.19, W0 + 0.04), (0.20, W1 - 0.06), (0.14, W1 - 0.02), (0, W1 - 0.005)], ["sec", "base", "base", "sec"], 28, crease=70)
        star = [(0.16*math.cos(math.radians(90 + 36*i))*(1 if i % 2 == 0 else 0.45), 0.16*math.sin(math.radians(90 + 36*i))*(1 if i % 2 == 0 else 0.45)) for i in range(10)]
        slab(star, W1 - 0.022, W1 - 0.002, "z", "glow")
    node()

# =================================================================================================== 5. HOVER ENGINE (rig of R4M_HoverEngine: Housing / Rotor / Vane_1..5 / PulseRing / ThrustGlow)
RP_ = (0, 0, -0.212)
def hover_fx(vane_t=0.0045):
    for i, (y, hx) in enumerate(((-0.164, 0.10), (-0.082, 0.185), (0.0, 0.214), (0.082, 0.185), (0.164, 0.10))):
        node("Vane_%d" % (i + 1), (0, y, -0.293), parent=None); sbox(-hx, hx, y - 0.032, y + 0.032, -0.293 - vane_t, -0.293 + vane_t, "base", 0.003)
    node("PulseRing", (0, 0, -0.337), parent=None); torus(0.20, 0.012, "glow", 24, 6, at=(0, 0, -0.337 + 0.014))
    node("ThrustGlow", (0, 0, -0.324), parent=None); cyl(0.19, -0.322, -0.316, "glow", 0.002, 20)
    node()
def hcap(h=0.055): box(-H, H, -H, H, H - h, H); port("up")
@D("Hover", "A")
def hover_a():
    hcap(0.045)
    lathe([(0.205, -0.30), (0.215, -0.317), (0.24, -0.31), (0.26, -0.22), (0.255, -0.08), (0.20, 0.03), (0.10, 0.10), (0.10, 0.215), (0.07, 0.215), (0.07, 0.07), (0.19, -0.02), (0.215, -0.10)],
          ["glow", "sec", "base", "base", "base", "base", "sec", "dark", "dark", "dark", "dark", "glow"], 36, closed=True, crease=50)
    tube(0.256, 0.2595, -0.16, -0.152, "glow", 0.002, 36)
    lathe([(0, -0.19), (0.05, -0.19), (0.075, -0.12), (0.07, 0.08), (0, 0.08)], ["dark", "base", "base", "dark"], 24, crease=60)
    for a in (30, 150, 270): slab([(0.07, -0.16), (0.215, -0.13), (0.215, -0.10), (0.07, -0.06)], -0.005, 0.005, (X, Z, -Y), "sec", R=rot(Z, a))
    node("Rotor", RP_, parent=None); cyl(0.05, -0.246, -0.198, "dark", 0.006, 16)
    for i in range(9): sbox(0.045, 0.196, -0.018, 0.018, -0.004, 0.004, "metal", 0.0015, rot(Z, 40*i) @ rot(X, 28), (0, 0, -0.222))
    hover_fx()
@D("Hover", "B")
def hover_b():
    hcap(0.06)
    for sx in (-1, 1):
        for sy in (-1, 1):
            sbox(sx*0.225 - 0.03, sx*0.225 + 0.03, sy*0.225 - 0.03, sy*0.225 + 0.03, -0.31, 0.20, "base", 0.006); sbox(sx*0.225 - 0.012, sx*0.225 + 0.012, sy*0.225 - 0.012, sy*0.225 + 0.012, -0.317, -0.30, "dark", 0.003)
    for z in (-0.28, -0.05):
        for a in (0, 90, 180, 270): sbox(-0.20, 0.20, 0.225, 0.25, z - 0.016, z + 0.016, "dark", 0.004, rot(Z, a))
    tube(0.20, 0.222, -0.30, -0.03, "base", 0.004, 16)
    cyl(0.085, -0.18, 0.205, "metal", 0.006, 12)
    for z in (-0.12, -0.07, -0.02, 0.03, 0.08, 0.13): tube(0.08, 0.125, z - 0.008, z + 0.008, "dark", 0.002, 12)
    for a in (45, 135, 225, 315): rod(rot(Z, a) @ np.array([0.12, 0, 0.16]), rot(Z, a) @ np.array([0.27, 0, 0.0]), 0.02, "metal", 0.004, 6)
    for a in (0, 90): sbox(-0.205, 0.205, -0.008, 0.008, -0.185, -0.17, "metal", 0.003, rot(Z, a))
    node("Rotor", RP_, parent=None); cyl(0.06, -0.246, -0.197, "dark", 0.004, 8); lathe([(0, -0.246), (0.03, -0.246), (0.03, -0.236), (0, -0.236)], "bolt", 6, crease=1.0)
    for i in range(4): sbox(0.05, 0.196, -0.05, 0.05, -0.006, 0.006, "metal", 0.002, rot(Z, 90*i) @ rot(X, 16), (0, 0, -0.221))
    hover_fx(0.0045)
@D("Hover", "C")
def hover_c():
    hcap(0.055); tube(0.17, 0.258, -0.315, -0.06, "base", 0.03, 24); cyl(0.09, -0.19, 0.21, "sec", 0.03, 18)
    for a in (0, 120, 240): sbox(0.07, 0.20, -0.03, 0.03, -0.10, -0.06, "sec", 0.012, rot(Z, a))
    node("Rotor", RP_, parent=None); cyl(0.07, -0.246, -0.198, "sec", 0.012, 14)
    for i in range(3): sbox(0.05, 0.165, -0.045, 0.045, -0.008, 0.008, "base", 0.003, rot(Z, 120*i) @ rot(X, 14), (0, 0, -0.222))
    hover_fx()
@D("Hover", "D")
def hover_d():
    hcap(0.05)
    tube(0.205, 0.235, -0.31, -0.02, "metal", 0.004, 10)
    for z in (-0.25, -0.13): tube(0.233, 0.243, z - 0.012, z + 0.012, "rust", 0.002, 10)
    sbox(0.10, 0.20, -0.08, 0.08, -0.22, -0.08, "patch", 0.003, rot(Z, 20))
    for (a, x) in ((15, 0.22), (140, 0.215), (250, 0.225)): rod(rot(Z, a) @ np.array([x, 0, -0.03]), rot(Z, a) @ np.array([0.06, 0, 0.205]), 0.017, "rust", 0.003, 6)
    cyl(0.06, -0.19, 0.205, "metal", 0.004, 8)
    sbox(0.08, 0.20, -0.20, -0.09, 0.06, 0.20, "patch", 0.005, rot(Z, -8)); cyl(0.045, 0.0, 0.03, "rust", 0.004, 8, R=R_to(-Y), at=(0.14, -0.20, 0.12)); cyl(0.03, 0.0, 0.02, "metal", 0.003, 7, at=(0, 0, 0.10))
    rod((0.14, -0.215, 0.12), (0.0, -0.06, 0.11), 0.007, "dark", 0.001, 4); rod((-0.2, 0.1, 0.2), (-0.21, 0.12, -0.05), 0.008, "dark", 0.001, 4)
    node("Rotor", RP_, parent=None); cyl(0.05, -0.246, -0.198, "rust", 0.004, 7)
    for i, (a, r1, w) in enumerate(((0, 0.19, 0.04), (70, 0.175, 0.05), (150, 0.19, 0.035), (215, 0.15, 0.045), (290, 0.185, 0.04))): sbox(0.04, r1, -w, w, -0.005, 0.005, "metal" if i % 2 else "rust", 0.002, rot(Z, a) @ rot(X, 18), (0, 0, -0.222))
    hover_fx()
@D("Hover", "E")
def hover_e():
    hcap(0.055)
    lathe([(0.185 + 0.073*math.cos(t), -0.20 + 0.115*math.sin(t)) for t in np.linspace(0, 2*math.pi, 14, endpoint=False)], "base", 32, closed=True, crease=80)
    torus(0.185, 0.02, "sec", 32, 8, at=(0, 0, -0.085)); tube(0.20, 0.235, -0.317, -0.305, "glow", 0.004, 32)
    lathe([(0, -0.19), (0.06, -0.19), (0.07, -0.02), (0.13, 0.06), (0.13, 0.12), (0.06, 0.205), (0, 0.205)], ["dark", "sec", "base", "base", "base", "sec"], 24, crease=70)
    for a in (90, 210, 330): rod(rot(Z, a) @ np.array([0.19, 0, -0.10]), rot(Z, a) @ np.array([0.09, 0, 0.09]), 0.03, "sec", 0.012, 10)
    node("Rotor", RP_, parent=None); ball(0.06, "sec", 14, 8, at=(0, 0, -0.222), squash=0.4)
    for i in range(4): lathe([(0, -0.008), (0.05, -0.008), (0.06, 0.0), (0.05, 0.008), (0, 0.008)], "base", 14, crease=70, R=rot(Z, 90*i) @ rot(X, 16), at=rot(Z, 90*i) @ np.array([0.125, 0, -0.222]))
    for i in range(4): K.BASE.CUR.parts[-1 - i]["V"][:] = (K.BASE.CUR.parts[-1 - i]["V"] - rot(Z, 90*(3 - i)) @ np.array([0.125, 0, -0.222])) @ (rot(Z, 90*(3 - i)) @ np.diag([1.25, 0.9, 1.0]) @ rot(Z, 90*(3 - i)).T).T + rot(Z, 90*(3 - i)) @ np.array([0.125, 0, -0.222])
    hover_fx()

# =================================================================================================== 6. BATTERY (contacts: down full, back partial)
def bat_iface(h=0.06, pad=(0.10, 0.10, -0.20, 0.06)): box(-H, H, -H, H, -H, -H + h); port("down"); box(-pad[0], pad[0], pad[1], H, pad[2], pad[3]); port("back", "partial", 0.14, (0, 0, -0.07))
@D("Battery", "A")
def bat_a():
    bat_iface(0.045, (0.09, 0.16, -0.20, 0.03))
    for sx in (-1, 1): prism([(-0.13, -0.215), (0.13, -0.215), (0.13, -0.10), (0.09, -0.02), (-0.09, -0.02), (-0.13, -0.10)], sx*0.17 - 0.03, sx*0.17 + 0.03, (Y, Z, X), s=0.3, org=(0, -0.05, 0))
    cyl(0.07, -0.22, 0.22, "glow", 0.006, 24, R=R_to(X), at=(0, -0.05, 0.02))
    for (x0, x1) in ((-0.235, -0.115), (-0.075, 0.075), (0.115, 0.235)): tube(0.07, 0.115, x0, x1, "base", 0.012, 32, R=R_to(X), at=(0, -0.05, 0.02))
    for x in (-0.095, 0.095): tube(0.085, 0.095, x - 0.012, x + 0.012, "dark", 0.003, 32, R=R_to(X), at=(0, -0.05, 0.02))
    sbox(-0.06, 0.06, -0.075, -0.025, 0.133, 0.142, "glow", 0.002); sbox(-0.09, 0.09, -0.09, -0.01, 0.125, 0.135, "dark", 0.003)
@D("Battery", "B")
def bat_b():
    bat_iface(0.075)
    for sx in (-1, 1):
        cyl(0.10, -0.185, 0.15, "base", 0.006, 14, at=(sx*0.125, -0.06, 0))
        for z in (-0.16, -0.02, 0.12): tube(0.098, 0.112, z - 0.012, z + 0.012, "dark", 0.003, 14, at=(sx*0.125, -0.06, 0))
        cyl(0.03, 0.15, 0.225, "metal", 0.004, 8, at=(sx*0.125, -0.06, 0)); lathe([(0, 0.225), (0.045, 0.225), (0.045, 0.238), (0.03, 0.25), (0, 0.25)], "bolt", 6, crease=1.0, at=(sx*0.125, -0.06, 0))
        sbox(sx*0.245 - 0.012, sx*0.245 + 0.012, -0.16, 0.04, -0.185, 0.16, "dark", 0.004)
        for y in (-0.13, -0.06, 0.01): sbox(sx*0.238 - 0.02, sx*0.238 + 0.02, y - 0.006, y + 0.006, -0.17, 0.14, "metal", 0.002)
    rod((-0.125, -0.06, 0.21), (0.125, -0.06, 0.21), 0.02, "sec", 0.004, 8)
    for z in (-0.11, 0.07): sbox(-0.235, 0.235, -0.178, -0.158, z - 0.025, z + 0.025, "dark", 0.005)                 # two bolted clamp bands, the drums stay visible
    sbox(-0.05, 0.05, -0.20, -0.172, -0.07, 0.03, "sec", 0.004); sbox(-0.03, 0.03, -0.207, -0.198, -0.045, 0.005, "glow", 0.003)
@D("Battery", "C")
def bat_c():
    bat_iface(0.06, (0.10, 0.12, -0.20, 0.02)); cyl(0.19, -0.20, 0.17, "base", 0.045, 22, at=(0, -0.05, 0)); cyl(0.07, 0.17, 0.235, "sec", 0.02, 14, at=(0, -0.05, 0))
    tube(0.185, 0.197, -0.06, 0.03, "sec", 0.004, 22, at=(0, -0.05, 0))
@D("Battery", "D")
def bat_d():
    bat_iface(0.05, (0.10, 0.07, -0.20, 0.03))
    sbx(-0.20, 0.06, -0.21, 0.07, -0.21, 0.07, sa=(-0.07,), s=0.5)
    for x in (-0.15, 0.0): sbox(x - 0.015, x + 0.015, -0.218, 0.078, -0.21, 0.08, "rust", 0.003)
    for (x, g) in ((-0.13, "metal"), (-0.01, "rust")): cyl(0.022, 0.07, 0.12, g, 0.003, 7, at=(x, -0.13, 0))
    cyl(0.075, 0.0, 0.30, "rust", 0.006, 9, R=rot(X, -14), at=(0.16, -0.10, -0.205)); cyl(0.03, 0.30, 0.335, "metal", 0.004, 6, R=rot(X, -14), at=(0.16, -0.10, -0.205))
    rod((-0.13, -0.13, 0.115), (-0.05, -0.2, 0.17), 0.009, "dark", 0.001, 5); rod((-0.05, -0.2, 0.17), (0.14, -0.17, 0.10), 0.009, "dark", 0.001, 5); rod((-0.01, -0.13, 0.115), (0.02, 0.02, 0.15), 0.009, "rust", 0.001, 5); rod((0.02, 0.02, 0.15), (0.0, 0.12, 0.05), 0.009, "rust", 0.001, 5)
    sbox(-0.19, -0.10, 0.068, 0.08, -0.16, -0.02, "patch", 0.003, rot(Y, 12))
@D("Battery", "E")
def bat_e():
    bat_iface(0.07, (0.10, 0.14, -0.19, 0.03))
    Rt = rot(Y, 22)
    with K.group(R=Rt, at=(0.0, -0.06, 0.02)):
        lathe([(0, -0.20), (0.09, -0.20), (0.12, -0.15), (0.15, 0.0), (0.12, 0.15), (0.09, 0.20), (0, 0.20)], ["sec", "sec", "base", "base", "sec", "sec"], 26, crease=70)
        for z in (-0.175, 0.175): torus(0.105, 0.024, "sec", 26, 8, at=(0, 0, z))
        cyl(0.045, 0.20, 0.235, "metal", 0.015, 14)
        bolt = [(0.03, 0.11), (-0.065, -0.01), (-0.01, -0.01), (-0.04, -0.11), (0.065, 0.02), (0.01, 0.02), (0.055, 0.11)]
        slab(bolt, 0.135, 0.158, (X, Z, -Y), "glow")
    for sx in (-1, 1): slab([(0.0, -0.19), (0.13, -0.19), (0.0, -0.02)], sx*0.18 - 0.02, sx*0.18 + 0.02, (Y, Z, X), "sec", org=(0, -0.20, 0))

# =================================================================================================== 8. EXPLOSIVE TRAP (world object on the ground; node Lamp) - defined before the deployer, which carries its miniature
def lamp(fn):
    node("Lamp", (0, 0, 0.10), dict(type="slide", axis=(0, 0, 1), amount=0.015, frames=24)) if not K._GRP else None
    fn()
    if not K._GRP: node()
@D("Trap", "A")
def trap_a():
    lathe([(0, 0), (0.17, 0), (0.20, 0.012), (0.20, 0.022), (0.12, 0.05), (0, 0.055)], ["dark", "sec", "sec", "base", "base"], 32, crease=50)
    tube(0.196, 0.202, 0.012, 0.02, "glow", 0.002, 32)
    for a in (30, 150, 270): slab([(0.10, -0.05), (0.245, -0.022), (0.255, 0.0), (0.245, 0.022), (0.10, 0.05)], 0.026, 0.038, "z", "base", R=rot(Z, a)); sbox(0.14, 0.23, -0.003, 0.003, 0.037, 0.041, "glow", 0.001, rot(Z, a))
    lamp(lambda: lathe([(0, 0.05), (0.06, 0.05), (0.055, 0.075), (0.03, 0.095), (0, 0.10)], "glow", 20, crease=70))
@D("Trap", "B")
def trap_b():
    cyl(0.20, 0.0, 0.03, "dark", 0.004, 14); ring_of(8, lambda R, i: lathe([(0, 0), (0.014, 0), (0.014, 0.01), (0.009, 0.014), (0, 0.014)], "bolt", 6, crease=1.0, at=R @ np.array([0.17, 0, 0.03])))
    cyl(0.07, 0.03, 0.07, "metal", 0.004, 10); ball(0.135, "base", 16, 8, at=(0, 0, 0.165))
    torus(0.137, 0.014, "dark", 16, 4, at=(0, 0, 0.165))
    def horn(R, i):
        d = R @ np.array([math.cos(math.radians(35)), 0, math.sin(math.radians(35))]); c = np.array([0, 0, 0.165]); rod(c + d*0.12, c + d*0.18, 0.02, "metal", 0.004, 8); cyl(0.027, 0.0, 0.012, "dark", 0.003, 8, R=R_to(d), at=c + d*0.178)
    ring_of(5, horn)
    lamp(lambda: cyl(0.035, 0.288, 0.30, "glow", 0.003, 10))
@D("Trap", "C")
def trap_c():
    cyl(0.21, 0.0, 0.07, "base", 0.03, 22)
    lamp(lambda: lathe([(0, 0.06), (0.12, 0.06), (0.11, 0.10), (0.06, 0.13), (0, 0.135)], "glow", 18, crease=70))
@D("Trap", "D")
def trap_d():
    sbox(-0.22, 0.20, -0.11, 0.10, 0.0, 0.02, "patch", 0.004, rot(Z, 12)); sbox(-0.10, 0.12, -0.22, 0.21, 0.018, 0.034, "rust", 0.004, rot(Z, -20))
    cyl(0.085, 0.03, 0.22, "rust", 0.006, 9, at=(0.02, 0.01, 0)); lathe([(0, 0.22), (0.06, 0.22), (0.03, 0.26), (0.02, 0.285), (0, 0.285)], "metal", 7, crease=0.5, at=(0.02, 0.01, 0))
    for z in (0.08, 0.17): tube(0.083, 0.092, z - 0.008, z + 0.008, "metal", 0.002, 9, at=(0.02, 0.01, 0))
    for i, a in enumerate((10, 55, 120, 170, 230, 300)): cone(0.008, 0.002, 0.0, 0.075, "metal", 5, R=rot(Z, a) @ rot(Y, 75 + 8*(i % 3)), at=rot(Z, a) @ np.array([0.085, 0, 0.10 + 0.03*(i % 2)]) + np.array([0.02, 0.01, 0]))
    cyl(0.035, 0.03, 0.10, "metal", 0.003, 7, at=(-0.13, -0.05, 0)); rod((-0.13, -0.05, 0.10), (-0.05, -0.02, 0.19), 0.006, "dark", 0.001, 4)
    lamp(lambda: sbox(-0.145, -0.115, -0.065, -0.035, 0.10, 0.115, "glow", 0.004))
@D("Trap", "E")
def trap_e():
    star = [(0.25*math.cos(math.radians(90 + 36*i))*(1 if i % 2 == 0 else 0.52), 0.25*math.sin(math.radians(90 + 36*i))*(1 if i % 2 == 0 else 0.52)) for i in range(10)]
    slab(star, 0.0, 0.045, "z", "base")
    for i in range(5): ball(0.05, "sec", 12, 8, at=(0.205*math.cos(math.radians(90 + 72*i)), 0.205*math.sin(math.radians(90 + 72*i)), 0.045), squash=0.8)
    lathe([(0, 0.04), (0.12, 0.04), (0.13, 0.07), (0, 0.07)], "sec", 22, crease=70)
    lamp(lambda: lathe([(0, 0.07), (0.10, 0.07), (0.095, 0.12), (0.055, 0.165), (0, 0.18)], "glow", 20, crease=70))

# =================================================================================================== 7. EXPLOSIVE TRAP DEPLOYER (contacts: down full, back partial; Payload = miniature of the style's own trap; drops to the rear)
def dep_iface(h=0.06, tower=(0.12, 0.13, -0.20, 0.22)):
    """shared contract only: bottom contact plate, rear contact pad (its size and shape belong to the style), Deploy_Out socket"""
    box(-H, H, -H, H, -H, -H + h); port("down")
    box(-tower[0], tower[0], tower[1], H, tower[2], tower[3]); port("back", "partial", 0.2, (0, 0, 0.01)); empty("Deploy_Out", (0, 0.26, 0.0), (0, 1, 0))
def payload(style, s, at, R=None):
    with K.group(R=R, s=s, at=at, lod=True): P["Trap"][style]()
@D("Deployer", "A")
def dep_a():
    dep_iface(0.045, (0.11, 0.17, -0.20, 0.14))
    slab([(0.17, 0.14), (0.26, 0.14), (0.26, 0.175), (0.215, 0.215)], -0.03, 0.03, (Y, Z, X), "sec")                      # swept fin on the pad
    lathe([(0, -0.215), (0.20, -0.215), (0.222, -0.17), (0.19, -0.12), (0, -0.12)], ["sec", "base", "base", "dark"], 32, crease=50, at=(0, -0.03, 0))
    for sx in (-1, 1): slab([(-0.24, -0.13), (0.10, -0.13), (0.10, -0.02), (0.0, 0.03), (-0.20, -0.06)], sx*0.20 - 0.01, sx*0.20 + 0.01, (Y, Z, X), "base"); sbox(sx*0.20 - 0.013, sx*0.20 + 0.013, -0.19, 0.06, -0.035, -0.028, "glow", 0.001, rot(X, 15), (0, 0, 0.02))
    for sx in (-1, 1): rod((sx*0.20, -0.02, -0.03), (sx*0.165, -0.05, 0.03), 0.012, "sec", 0.003, 8)
    tube(0.15, 0.17, -0.01, 0.01, "sec", 0.004, 32, R=rot(X, 15), at=(0, -0.05, 0.03)); tube(0.168, 0.172, -0.004, 0.004, "glow", 0.001, 32, R=rot(X, 15), at=(0, -0.05, 0.03))
    payload("A", 0.62, (0, -0.05, -0.115), rot(X, 10))
    sbox(-0.07, 0.07, 0.163, 0.171, -0.03, 0.10, "glow", 0.002)
@D("Deployer", "B")
def dep_b():
    dep_iface(0.075, (0.13, 0.12, -0.185, 0.22))
    for sx in (-1, 1): sbox(sx*0.22 - 0.025, sx*0.22 + 0.025, -0.25, 0.13, -0.185, -0.03, "base", 0.006, rot(X, 10)); slab([(-0.24, -0.185), (-0.10, -0.185), (-0.24, -0.06)], sx*0.22 - 0.008, sx*0.22 + 0.008, (Y, Z, X), "dark")
    for k in range(6): cyl(0.022, -0.19, 0.19, "metal", 0.003, 8, R=R_to(X), at=(0, -0.22 + k*0.062, -0.165 + k*0.011))
    cyl(0.06, 0.0, 0.02, "dark", 0.004, 10, R=R_to(X), at=(0.245 - 0.02, 0.05, -0.10)); cyl(0.035, 0.0, 0.02, "dark", 0.004, 8, R=R_to(X), at=(0.225, -0.15, -0.14))
    rod((0.0, 0.12, 0.16), (0.0, -0.02, 0.05), 0.022, "metal", 0.004, 8); cyl(0.03, 0.0, 0.03, "dark", 0.004, 8, R=R_to(-Y), at=(0, -0.02, 0.05))
    payload("B", 0.55, (0, -0.07, -0.135), rot(X, 10))
    for z in (0.02, 0.10, 0.18): sbox(-0.137, 0.137, 0.11, 0.125, z - 0.012, z + 0.012, "dark", 0.003)
@D("Deployer", "C")
def dep_c():
    dep_iface(0.06, (0.12, 0.14, -0.20, 0.13))
    slab([(-0.25, -0.20), (0.13, -0.20), (0.13, -0.08), (-0.25, -0.17)], -0.20, 0.20, (Y, Z, X), "sec")
    payload("C", 0.72, (0, -0.06, -0.125), rot(X, 13.5))
@D("Deployer", "D")
def dep_d():
    dep_iface(0.05, (0.11, 0.19, -0.21, 0.13))
    for sx, z in ((-1, -0.13), (1, -0.15)): rod((sx*0.19, -0.25, z - 0.06), (sx*0.19, 0.12, z + 0.04), 0.02, "metal" if sx < 0 else "rust", 0.003, 7)
    for y in (-0.18, -0.02): sbox(-0.2, 0.2, y - 0.012, y + 0.012, -0.012, 0.0, "patch", 0.003, rot(X, 15), (0, 0, -0.14 + (y + 0.25)*0.26))
    cyl(0.095, -0.20, 0.10, "rust", 0.006, 9, at=(-0.15, 0.03, 0)); tube(0.093, 0.102, -0.06, -0.04, "metal", 0.002, 9, at=(-0.15, 0.03, 0))
    rod((0.22, -0.2, -0.2), (0.21, 0.1, 0.0), 0.01, "rust", 0.002, 5); sbox(0.10, 0.24, -0.05, 0.10, -0.205, -0.19, "patch", 0.003, rot(Z, 10))
    payload("D", 0.5, (0.06, -0.08, -0.125), rot(X, 15))
@D("Deployer", "E")
def dep_e():
    dep_iface(0.07, (0.12, 0.15, -0.19, 0.15))
    lathe([(0, 0.0), (0.092, 0.0), (0.084, 0.035), (0.045, 0.06), (0, 0.066)], ["dark", "sec", "sec", "sec"], 20, crease=70, at=(0, 0.166, 0.15))
    for i in range(4): torus(0.07, 0.022, "metal", 18, 8, at=(0, -0.04, -0.165 + i*0.04))
    lathe([(0, -0.03), (0.09, -0.03), (0.18, 0.0), (0.21, 0.05), (0.195, 0.06), (0.155, 0.02), (0, 0.0)], ["sec", "base", "base", "sec", "dark", "dark"], 28, crease=70, R=rot(X, 12), at=(0, -0.04, -0.02))
    payload("E", 0.5, (0, -0.04, -0.012), rot(X, 12))


# =================================================================================================== registry of the pilot: type -> approved contract of the canonical asset (unchanged) + the five constructions
CID = dict(Cube="R4B_Cube", Strut="R4C_STRUT_STANDARD", Gun="R4W_Basic", Wheel="R4M_WheelM_Side_Offroad", Hover="R4M_HoverEngine", Battery="R4X_Battery", Deployer="R4X_ExplosiveTrapDeployer", Trap="R4D_ExplosiveTrap")
NB = {  # bounds of the approved source nodes (Blender axes, m): a restyled node must stay inside its source node (collision outline of the rig)
 "Gun": {"Body": [[-0.257, -0.257, -0.26], [0.257, 0.257, 0.016]], "Yaw": [[-0.191, -0.191, 0.018], [0.191, 0.257, 0.22]], "Pitch": [[-0.14, -0.402, 0.085], [0.14, 0.078, 0.236]], "Barrel": [[-0.04, -0.62, 0.098], [0.04, -0.10, 0.172]]},
 "Wheel": {"Body": [[-0.26, -0.256, -0.256], [0.062, 0.256, 0.256]], "Rotor": [[-0.148, -0.39, -0.39], [0.192, 0.39, 0.39]]},
 "Hover": {"Body": [[-0.262, -0.262, -0.319], [0.262, 0.262, 0.26]], "Rotor": [[-0.2, -0.2, -0.247], [0.2, 0.2, -0.196]]},
}
TYPES = {t: dict(AD.ASSETS[c], cid=c, typ=t, nb=NB.get(t), glow=list(AD.ASSETS[c]["glow"])) for t, c in CID.items()}
ORDER = ["Cube", "Strut", "Gun", "Wheel", "Hover", "Battery", "Deployer", "Trap"]

NAME_PL = dict(Cube="Klocek sześcienny", Strut="Łącznik konstrukcyjny STRUT", Gun="Działko podstawowe (Basic Gun)", Wheel="Koło z napędem", Hover="Silnik poduszkowy (HoverEngine)", Battery="Bateria", Deployer="Wyrzutnik pułapek wybuchowych", Trap="Pułapka wybuchowa")
CONCEPT = {  # the mechanical idea of each of the 40 constructions (shown on the boards and in the viewer)
 "Cube": dict(A="lakierowana skorupa: 3 x 3 panele o nierównym podziale, w szczelinach paski światła", B="cztery przykręcone płyty pancerne na twarz, krzyżowy kanał ramy, śruby w narożach",
              C="jedna gładka bryła z miękkim zaokrągleniem, bez paneli", D="pozszywane blachy różnej wielkości, łaty, nity, rdza", E="jedna wypukła poducha na twarz w grubej kremowej ramie"),
 "Strut": dict(A="sześciokątna belka-kręgosłup z kołnierzami, szynami światła i pierścieniem", B="dwuteownik z żebrami, krzyżulcami z prętów i śrubami pasów", C="jedna gruba rura między klockami",
               D="dwie niedopasowane rury, przyspawane blachy, pręt zbrojeniowy i łańcuch", E="belka-kość: dwa kielichy z talią i obręczą"),
 "Gun": dict(A="niska kopuła, wieża-dysk z dwoma płetwami, sześciokątny smukły korpus, cienka lufa emitera z pierścieniami światła", B="pierścień łożyska na śrubach, jarzmo w kształcie U z zastrzałami, skrzynka amunicyjna, żebrowany płaszcz lufy i hamulec wylotowy",
             C="walec, zaokrąglony blok i gruba lufa - trzy bryły", D="beczka-podstawa, rama z rurek, asymetryczna osłona z blachy, kanister, celownik na opasce, lufa z rury w tulejach", E="pękata podstawa, bębny czopów po bokach, baniasty blaster z płetwą i rozszerzonym wylotem"),
 "Wheel": dict(A="gładka opona-torus, pełna tarcza aero, pierścienie światła na obręczy, ramię-stożek", B="opona z 16 klockami, obręcz ze szprychami na śrubach, tarcza hamulca z zaciskiem, mocowanie z zastrzałami",
               C="masywna opona ze ściętym profilem, prosta piasta z pięcioma kołkami", D="łatana opona z nierównymi klockami, kolce, łańcuch, pokrywa piasty na śrubach, resor piórowy i kątownik", E="opona-balon z ośmioma obłymi wypustkami, pękata piasta i kołpak-gwiazda"),
 "Hover": dict(A="zamknięta gładka gondola z pierścieniem światła, kopuła piasty, 9 cienkich łopatek", B="kwadratowa klatka z kształtowników, kanał, żebrowany silnik, zastrzały, 4 łopaty-wiosła",
               C="gruby pierścień, kolumna i trzy szerokie łopaty", D="kanał z beczki z obręczami, krzywe rozpory z rur, boczny silnik z paskiem, 5 niedopasowanych łopat", E="kanał-pączek z obręczą, piasta-grzyb, 4 płatki"),
 "Battery": dict(A="pozioma kapsuła w dwóch kołyskach, widoczny świecący rdzeń między tulejami", B="dwa stojące bębny z kołnierzami, zaciski, szyna łącząca, opaski na śrubach, radiatory po bokach",
                 C="jedno grube ogniwo z opaską i biegunem", D="akumulator samochodowy w taśmach, przechylony kanister, kable rozruchowe", E="przechylona pękata kapsuła z piorunem i grubymi obręczami"),
 "Deployer": dict(A="kołyska-dysk z bocznymi płetwami, pierścień magnetyczny, szyny światła", B="pochylnia rolkowa w ramie z blach, koła łańcuchowe, tłok dociskowy, magazyn z żebrami",
                  C="klin-rampa i blok magazynu", D="prowadnice z dwóch rur, magazyn z beczki, łaty, pręt", E="misa na sprężynie z obłą wieżą i gwiazdą"),
 "Trap": dict(A="cienki dysk z trzema płatami i kopułką światła", B="mina morska: kula z rogami zapalników na kołnierzu ze śrubami", C="płaski krążek z dużym przyciskiem",
              D="butla z gwoździami na skrzyżowanych blachach, zapalnik z puszki na kablu", E="gwiazda z kulami na ramionach i dużą kopułą"),
}
