"""RoboRougelike - six-style series 1 (R4W_Basic, R4M_Wheel, R4X_FlyingDroneDeployer, FlyingDrone). RUNS INSIDE BLENDER (hf.Builder).
Contracts are copied from the source assets (probe of the GLBs on the art branches): node names, parents, pivots, node bounds, envelope, empties, clips.
Axes: Blender X right, -Y forward, Z up (glTF / Unity: +Z forward, +Y up). Units: metres. Cell 0.52 m on the 0.55 m grid.
Styles: 1 Clean Futuristic, 2 Brutal Industrial, 3 Arcade Low-Poly, 4 Gritty Industrial, 5 Comic Outline, 6 R41 Modular Tech."""
import math, bpy
from hf import spl, sect, AX
from mathutils import Vector, Matrix
TAU = 2*math.pi
STYLES = {"1": "01_CleanFuturistic", "2": "02_BrutalIndustrial", "3": "03_ArcadeLowPoly", "4": "04_GrittyIndustrial", "5": "05_ComicOutline", "6": "06_R41ModularTech"}
def RX(a): return Matrix.Rotation(a, 4, "X")
def RY(a): return Matrix.Rotation(a, 4, "Y")
def RZ(a): return Matrix.Rotation(a, 4, "Z")
def T(*v): return Matrix.Translation(v)
def ngon(r, n, ph=0.0, cx=0.0, cy=0.0): return [(cx + r*math.cos(ph + TAU*i/n), cy + r*math.sin(ph + TAU*i/n)) for i in range(n)]
def star(r0, r1, n, ph=0.0): return [((r1 if i % 2 == 0 else r0)*math.cos(ph + TAU*i/(2*n)), (r1 if i % 2 == 0 else r0)*math.sin(ph + TAU*i/(2*n))) for i in range(2*n)]
def rrect(w, h, r, n=4):
    """rounded rectangle (half sizes w, h, corner radius r) as a polygon"""
    out = []
    for (cx, cy, a0) in ((w - r, h - r, 0), (-w + r, h - r, 90), (-w + r, -h + r, 180), (w - r, -h + r, 270)):
        for k in range(n + 1): a = math.radians(a0 + 90*k/n); out.append((cx + r*math.cos(a), cy + r*math.sin(a)))
    return out
def chamf(w, h, c): return [(w, h - c), (w - c, h), (-w + c, h), (-w, h - c), (-w, -h + c), (-w + c, -h), (w - c, -h), (w, -h + c)]
def ring(B, node, zone, r0, r1, t0, t1, seg=(48, 16), axis="z", at=(0, 0, 0), name="ring", phase=0.0, kind="solid"): return B.rev(node, zone, [(r0, t0), (r1, t0), (r1, t1), (r0, t1)], seg, axis, at, True, kind, name, phase=phase)
def disc(B, node, zone, r, t0, t1, seg=(48, 16), axis="z", at=(0, 0, 0), name="disc", phase=0.0, kind="solid"): return B.rev(node, zone, [(0.0, t0), (r, t0), (r, t1), (0.0, t1)], seg, axis, at, False, kind, name, phase=phase)
def bev(B, o, w, seg=(3, 1), ang=30): return B.bevel(o, w, seg, ang)
def poly_at(poly, cx, cy): return [(x + cx, y + cy) for (x, y) in poly]

# ================================================================================================ palettes (linear RGB, metallic, roughness)
PAL = {
 "1": dict(base=((0.72, 0.73, 0.74), 0.0, 0.26), sec=((0.010, 0.022, 0.085), 0.2, 0.36), dark=((0.026, 0.030, 0.038), 0.85, 0.40), metal=((0.17, 0.18, 0.20), 1.0, 0.30),
           bare=((0.72, 0.73, 0.75), 1.0, 0.16), rubber=((0.013, 0.013, 0.015), 0.0, 0.80), glow=((0.02, 0.72, 1.0), 0.0, 0.3), glass=((0.01, 0.05, 0.07), 0.0, 0.06), alt=((0.55, 0.57, 0.60), 0.0, 0.30)),
 "2": dict(base=((0.090, 0.093, 0.098), 0.88, 0.50), sec=((0.70, 0.40, 0.015), 0.15, 0.50), dark=((0.022, 0.022, 0.026), 0.8, 0.60), metal=((0.24, 0.24, 0.25), 1.0, 0.40),
           bare=((0.70, 0.70, 0.71), 1.0, 0.18), rubber=((0.013, 0.013, 0.014), 0.0, 0.86), glow=((1.0, 0.30, 0.02), 0.0, 0.3), glass=((0.10, 0.04, 0.01), 0.0, 0.08), rust=((0.07, 0.035, 0.02), 0.3, 0.8), alt=((0.20, 0.205, 0.21), 0.8, 0.5)),
 "3": dict(base=((0.040, 0.48, 0.42), 0.0, 0.66), sec=((0.30, 0.62, 0.020), 0.0, 0.62), dark=((0.010, 0.030, 0.140), 0.0, 0.66), metal=((0.020, 0.140, 0.62), 0.0, 0.60),
           bare=((0.40, 0.46, 0.56), 0.3, 0.45), rubber=((0.03, 0.032, 0.058), 0.0, 0.78), glow=((0.02, 0.75, 1.0), 0.0, 0.3), glass=((0.03, 0.20, 0.30), 0.0, 0.15), alt=((0.95, 0.62, 0.02), 0.0, 0.55)),
 "4": dict(base=((0.185, 0.042, 0.028), 0.3, 0.80), sec=((0.165, 0.080, 0.045), 0.3, 0.84), alt=((0.125, 0.036, 0.026), 0.3, 0.86), alt2=((0.20, 0.195, 0.175), 0.3, 0.70), dark=((0.016, 0.014, 0.012), 0.6, 0.82),
           metal=((0.105, 0.098, 0.090), 1.0, 0.55), bare=((0.34, 0.32, 0.30), 1.0, 0.42), rust=((0.06, 0.028, 0.014), 0.1, 0.92), rubber=((0.018, 0.016, 0.014), 0.0, 0.90), glow=((1.0, 0.36, 0.03), 0.0, 0.3), glass=((0.12, 0.05, 0.01), 0.0, 0.10)),
 "5": dict(base=((0.88, 0.20, 0.012), 0.0, 0.50), sec=((0.20, 0.035, 0.45), 0.0, 0.52), dark=((0.022, 0.026, 0.085), 0.0, 0.58), metal=((0.075, 0.10, 0.24), 0.1, 0.48),
           bare=((0.52, 0.58, 0.72), 0.5, 0.35), rubber=((0.035, 0.030, 0.055), 0.0, 0.78), glow=((0.02, 0.55, 1.0), 0.0, 0.3), glass=((0.02, 0.10, 0.22), 0.0, 0.10), alt=((0.95, 0.72, 0.03), 0.0, 0.5)),
 "6": dict(base=((0.16, 0.17, 0.18), 0.35, 0.55), alt=((0.27, 0.28, 0.285), 0.3, 0.58), dark=((0.035, 0.038, 0.045), 0.55, 0.45), sec=((0.60, 0.035, 0.025), 0.35, 0.38), alt2=((0.95, 0.60, 0.03), 0.15, 0.50),
           metal=((0.55, 0.62, 0.68), 0.9, 0.25), bare=((0.55, 0.62, 0.68), 0.9, 0.25), rubber=((0.02, 0.021, 0.024), 0.0, 0.85), glow=((1.0, 0.38, 0.06), 0.0, 0.25), glass=((0.10, 0.04, 0.01), 0.0, 0.10)),
}
FX = {
 "1": {"cavity": (0.38, (0.5, 0.54, 0.62)), "edge_light": (0.35, 0.10), "scratches": 0.2, "zones_paint": ["base", "sec"], "seed": 3},
 "2": {"edge_wear": (0.55, (0.5, 0.5, 0.49), 1.0, 0.28), "cavity": (0.80, (0.30, 0.27, 0.24)), "grime": (0.25, (0.026, 0.022, 0.018)), "dust": (0.12, (0.17, 0.145, 0.11)),
       "rust": (0.05, (0.05, 0.022, 0.01), (0.14, 0.06, 0.022)), "chips": (0.45, (0.115, 0.12, 0.125), 1.0, ["sec"]), "scratches": 0.8, "zones_paint": ["sec", "base", "dark"], "seed": 5},
 "3": {"cavity": (0.22, (0.6, 0.62, 0.78)), "gradient": (0.06,), "zones_paint": [], "seed": 1},
 "4": {"edge_wear": (0.66, (0.3, 0.28, 0.26), 1.0, 0.42), "cavity": (0.95, (0.22, 0.17, 0.13)), "grime": (0.75, (0.03, 0.024, 0.017)), "dust": (0.45, (0.2, 0.16, 0.11)),
       "rust": (0.34, (0.04, 0.018, 0.009), (0.15, 0.062, 0.022)), "chips": (0.55, (0.070, 0.034, 0.017), 0.1, ["base", "sec", "alt", "alt2"]), "scratches": 1.0, "zones_paint": ["base", "sec", "alt", "alt2", "metal", "dark"], "seed": 9},
 "5": {"cavity": (0.55, (0.45, 0.32, 0.62)), "gradient": (0.30,), "edge_light": (0.55, 0.26), "zones_paint": ["base", "sec", "dark", "metal"], "seed": 2},
 "6": {"cavity": (0.40, (0.42, 0.42, 0.45)), "edge_wear": (0.25, (0.45, 0.47, 0.50), 1.0, 0.30), "scratches": 0.35, "zones_paint": ["base", "alt", "sec", "alt2"], "seed": 6},
}
GLOW_K = {"1": 3.0, "2": 2.5, "3": 2.0, "4": 2.5, "5": 2.5, "6": 2.5}
OUTLINE = {"5": dict(width=0.012, rgb=(0.006, 0.006, 0.012))}             # Comic Outline (12 mm after the moodboard TEST 128 PX - 7.5 mm vanished at 128 px): inverted-hull contour, part of the mesh (works in Unity URP Lit with back-face culling)

# ================================================================================================ contracts (from the source GLBs)
H = 0.26
CONTRACT = {
 "R4W_Basic": dict(
    source="Tools/ModelFactory/R4Weapons938/R4W_Basic (PR #939, branch art/938-r4-weapon-models)",
    env=[(-0.26, -0.66, -0.26), (0.26, 0.26, 0.24)],
    nodes={"Base": dict(parent="ROOT", pivot=(0, 0, 0), bounds=[(-0.2565, -0.2565, -0.26), (0.2565, 0.2565, 0.016)]),
           "Yaw": dict(parent="GunPivot", pivot=(0, 0, 0.135), bounds=[(-0.1911, -0.1911, 0.018), (0.1911, 0.257, 0.2197)]),
           "Pitch": dict(parent="Yaw", pivot=(0, 0, 0.135), bounds=[(-0.14, -0.402, 0.085), (0.14, 0.078, 0.236)]),
           "Barrel": dict(parent="Pitch", pivot=(0, 0, 0.135), bounds=[(-0.04, -0.62, 0.098), (0.04, -0.10, 0.172)]),
           "MuzzleFlash": dict(parent="Pitch", pivot=(0, -0.624, 0.135), bounds=None)},
    empties={"GunPivot": dict(parent="ROOT", loc=(0, 0, 0.135), rot=(0, 0, 0)), "Socket_Mount_Bottom": dict(parent="ROOT", loc=(0, 0, -0.26), rot=(math.pi, 0, 0)),
             "Muzzle": dict(parent="Pitch", loc=(0, -0.66, 0.135), rot=(math.pi/2, 0, 0))},
    ports=[dict(dir="down", z=-0.26, half=0.235)],
    clips=[("Idle", 1, 25, True), ("AimYaw", 27, 75, False), ("AimPitch", 77, 113, False), ("Fire", 115, 124, False), ("Recoil", 126, 129, False), ("Return", 131, 139, False)]),
 "R4M_Wheel": dict(
    source="Tools/ModelFactory/R4Movement940/R4M_Wheel (PR #941, branch art/940-r4-movement-models)",
    env=[(-0.26, -0.40, -0.66), (0.26, 0.40, 0.26)],
    nodes={"Mount": dict(parent="ROOT", pivot=(0, 0, 0), bounds=[(-0.2563, -0.2563, -0.312), (0.2563, 0.2563, 0.26)]),
           "Rotor": dict(parent="ROOT", pivot=(0, 0, -0.26), bounds=[(-0.184, -0.39, -0.65), (0.184, 0.39, 0.13)])},
    empties={"Wheel_Axle": dict(parent="ROOT", loc=(0, 0, -0.26), rot=(0, math.pi/2, 0)), "Socket_Mount": dict(parent="ROOT", loc=(0, 0, 0.26), rot=(0, 0, 0)),
             "GroundProbe": dict(parent="ROOT", loc=(0, 0, -0.65), rot=(math.pi, 0, 0))},
    ports=[dict(dir="up", z=0.26, half=0.235)],
    clips=[("Idle", 1, 25, True), ("Roll", 27, 51, True), ("Reverse", 53, 77, True), ("Brake", 79, 103, False)]),
 "R4X_FlyingDroneDeployer": dict(
    source="Tools/ModelFactory/R4ModulesSleek1x1/modules/R4X_FlyingDroneDeployer (PR #1238) + atlas A-E (R4StyleAtlas5/atlas)",
    env=[(-0.26, -0.26, -0.26), (0.26, 0.26, 0.26)],
    nodes={"Body": dict(parent="ROOT", pivot=(0, 0, 0), bounds=[(-0.26, -0.26, -0.26), (0.26, 0.26, 0.26)]),
           "Iris": dict(parent="ROOT", pivot=(0, 0, 0.1), bounds=[(-0.26, -0.26, -0.26), (0.26, 0.26, 0.26)])},
    empties={"Port_down_000": dict(parent="ROOT", loc=(0, 0, -0.26), rot=(0, math.pi, 0)), "Socket_Mount": dict(parent="ROOT", loc=(0, 0, -0.26), rot=(0, math.pi, 0)),
             "Deploy_Out": dict(parent="ROOT", loc=(0, 0, 0.26), rot=(0, 0, math.pi))},
    ports=[dict(dir="down", z=-0.26, half=0.235)],
    clips=[("Active", 1, 49, True)]),
 "FlyingDrone": dict(
    source="NEW - no drone model exists in the repository or in the game code (catalogue #57: 'wypuszcza drony latajace'). Contract = PROPOSAL.",
    env=[(-0.23, -0.23, -0.005), (0.23, 0.23, 0.24)],
    nodes={"Body": dict(parent="ROOT", pivot=None, bounds=[(-0.23, -0.23, 0.0), (0.23, 0.23, 0.24)])},           # + Rotor_1..n, pivots from the recipe
    empties={"Socket_Dock": dict(parent="ROOT", loc=(0, 0, 0), rot=(math.pi, 0, 0)), "Muzzle": dict(parent="Body", loc=None, rot=(math.pi/2, 0, 0))},
    ports=[],
    clips=[("Docked", 1, 25, True), ("Launch", 27, 51, False), ("Hover", 53, 77, True), ("Fly", 79, 103, True)]),
}

# ================================================================================================ DEPLOYER + DRONE, style 1 Clean Futuristic
# family cue: the launch well rim and the drone's duct rings are the same turned profile with the same thin cyan light ring
DUCT1 = [(0.064, -0.016), (0.070, -0.020), (0.079, -0.016), (0.082, 0.004), (0.079, 0.016), (0.072, 0.020), (0.064, 0.016)]
def dep_1(B):
    """Clean Futuristic: low ceramic pod, the launch well is sunk flush into one calm shell (narrow seam, no frame around it); six thin iris
    shutters slide in a slot of the top skin; mechanism hidden; one cyan light ring at the bottom of the well and one along the front seam"""
    f = B.box("Body", "dark", (-0.250, -0.250, -0.260), (0.250, 0.250, -0.238), name="foot"); bev(B, f, 0.010, (3, 1))
    sh = B.hull("Body", "base", [(-0.240, 0, 0, 0.246, 0.246, 5.5), (-0.120, 0, 0, 0.248, 0.248, 5.5), (-0.020, 0, 0, 0.242, 0.242, 5.0), (0.016, 0, 0, 0.230, 0.230, 4.6), (0.030, 0, 0, 0.216, 0.216, 4.3)],
                n=(64, 24), m=(8, 3), axis="z", name="shell")
    c2 = B.cyl("Body", "base", (0, 0, -0.050), (0, 0, 0.2), 0.170, (96, 40), "cutter"); B.cut(sh, c2)
    for (a, b) in (((-0.252, -0.120, -0.132), (0.252, 0.120, -0.126)), ((-0.120, -0.252, -0.132), (0.120, 0.252, -0.126))):               # narrow seam round the shell
        B.cut(sh, B.box("Body", "base", a, b, "cutter"), lo=False)
    for x in (-0.110, 0.110): B.cut(sh, B.box("Body", "base", (x - 0.002, -0.252, -0.126), (x + 0.002, -0.200, 0.040), "cutter"), lo=False)
    ring(B, "Body", "metal", 0.168, 0.172, -0.050, 0.030, (96, 32), name="wellwall")
    disc(B, "Body", "dark", 0.170, -0.052, -0.040, (96, 32), name="pad")
    ring(B, "Body", "glow", 0.150, 0.160, -0.040, -0.0365, (96, 32), name="padlight")
    ring(B, "Body", "alt", 0.050, 0.110, -0.040, -0.037, (64, 24), name="padmark")
    disc(B, "Body", "glass", 0.030, -0.040, -0.034, (48, 16), name="lens")
    ring(B, "Body", "dark", 0.1705, 0.2105, 0.026, 0.031, (96, 32), name="slot")
    for o in B.arcs("Iris", "base", 0.173, 0.208, 0.0312, 0.0422, 6, 0.035, axis="z", seg=(96, 30), phase=15): bev(B, o, 0.003, (2, 1), 30)
    ring(B, "Iris", "glow", 0.194, 0.199, 0.0420, 0.0428, (96, 30), name="irislight")
    s = B.box("Body", "glow", (-0.140, -0.2495, -0.212), (0.140, -0.2440, -0.204), name="frontlight"); bev(B, s, 0.002, (2, 1))
    for x in (-0.180, 0.180):
        l = B.cyl("Body", "glass", (x, -0.224, -0.020), (x, -0.244, -0.020), 0.012, (24, 12), name="sensor")
    B.meta = dict(dock_z=0.0230, family="turned duct profile + thin cyan ring = launch-well rim")


def drone_1(B):
    """Clean Futuristic drone: a blended flying wing - one calm rounded-square slab with the four fans sunk into it (no arms, nothing exposed),
    a low canopy with a cyan visor; every fan opening is rimmed with the same turned profile and cyan ring as the launch well of the deployer"""
    zc = 0.040; B.meta = dict(body_pivot=(0, 0, zc), rotors={}, muzzle=(0, -0.150, zc), dock_z=0.0)
    sl = B.hull("Body", "base", [(zc - 0.020, 0, 0, 0.180, 0.180, 4.0), (zc - 0.012, 0, 0, 0.198, 0.198, 4.2), (zc + 0.004, 0, 0, 0.202, 0.202, 4.2), (zc + 0.016, 0, 0, 0.192, 0.192, 4.0), (zc + 0.020, 0, 0, 0.176, 0.176, 3.8)],
                n=(64, 24), m=(6, 3), axis="z", name="slab")
    F = [(-0.108, -0.108), (0.108, -0.108), (-0.108, 0.108), (0.108, 0.108)]
    for (x, y) in F: B.cut(sl, B.cyl("Body", "base", (x, y, zc - 0.05), (x, y, zc + 0.05), 0.070, (64, 24), "cutter"))
    for (a, b) in (((-0.205, -0.0012, zc - 0.03), (0.205, 0.0012, zc + 0.03)), ((-0.0012, -0.205, zc - 0.03), (0.0012, 0.205, zc + 0.03))):
        B.cut(sl, B.box("Body", "base", a, b, "cutter"), lo=False)                                       # narrow panel seams
    B.hull("Body", "alt", [(-0.070, 0, zc + 0.016, 0.018, 0.006, 3), (-0.040, 0, zc + 0.024, 0.040, 0.016, 3), (0.040, 0, zc + 0.022, 0.034, 0.014, 3), (0.066, 0, zc + 0.016, 0.014, 0.006, 3)],
           n=(40, 16), m=(6, 2), axis="y", name="canopy")
    v = B.box("Body", "glow", (-0.026, -0.068, zc + 0.026), (0.026, -0.060, zc + 0.030), name="visor"); bev(B, v, 0.002, (2, 1))
    s1 = B.box("Body", "glow", (-0.090, -0.2005, zc - 0.002), (0.090, -0.1975, zc + 0.002), name="frontlight")
    B.cyl("Body", "glass", (0, -0.186, zc - 0.006), (0, -0.204, zc - 0.006), 0.010, (24, 12), name="eye")
    B.hull("Body", "dark", [(-0.110, 0, zc - 0.022, 0.050, 0.008, 3), (0.110, 0, zc - 0.022, 0.050, 0.008, 3)], n=(32, 12), m=(4, 2), axis="y", name="belly")
    for sx in (-1, 1):
        B.hull("Body", "dark", [(-0.080, sx*0.060, 0.006, 0.006, 0.006, 3), (0.080, sx*0.060, 0.006, 0.006, 0.006, 3)], n=(16, 8), m=(4, 2), axis="y", name="skid")
        for y in (-0.050, 0.050): B.box("Body", "dark", (sx*0.060 - 0.004, y - 0.004, 0.008), (sx*0.060 + 0.004, y + 0.004, 0.024), name="skidstrut")
    for k, (x, y) in enumerate(F):
        c = (x, y, zc); nm = "Rotor_%d" % (k + 1)
        B.rev("Body", "base", spl([(r, t) for (r, t) in DUCT1] + [DUCT1[0]], B.q(4, 1)), (96, 32), "z", c, True, name="duct")
        ring(B, "Body", "glow", 0.0805, 0.0835, 0.0180, 0.0200, (96, 32), at=c, name="ductlight")
        for i in range(3):                                                                               # stator vanes holding the motor
            st = B.box("Body", "dark", (0.014, -0.003, -0.010), (0.066, 0.003, -0.004), name="stator"); B.move(st, T(*c) @ RZ(TAU*i/3 + 0.5))
        B.cyl("Body", "dark", (x, y, zc - 0.014), (x, y, zc - 0.002), 0.016, (24, 12), name="motor")
        B.cyl(nm, "metal", (x, y, zc - 0.002), (x, y, zc + 0.008), 0.012, (24, 12), name="hub")
        for i in range(5):
            bl = B.prism(nm, "alt", [(0.010, -0.008), (0.062, -0.006), (0.062, 0.004), (0.010, 0.007)], -0.0012, 0.0012, ("x", "y", "z"), name="blade"); B.move(bl, T(x, y, zc + 0.003) @ RZ(TAU*i/5 + 0.3) @ RX(0.30))
        B.meta["rotors"][nm] = c


# ================================================================================================ style 2 Brutal Industrial
# family cue: square box frames with yellow hazard edges, and four clamp lugs on the drone that the four clamp jaws of the launcher hold
def dep_2(B):
    """Brutal Industrial: a launch cradle - thick base plate, four heavy box columns with gussets carry a square ring beam; the square launch
    platform sits on two hydraulic rams that go straight down to the base (the load path is visible); a turntable with four clamp jaws
    holds the drone; yellow hazard border round the platform; armour cheeks between the columns"""
    p = B.box("Body", "dark", (-0.250, -0.250, -0.260), (0.250, 0.250, -0.215), name="base"); bev(B, p, 0.006, (2, 1))
    for (sx, sy) in ((-1, -1), (1, -1), (-1, 1), (1, 1)):
        x0, x1 = sorted((sx*0.250, sx*0.175)); y0, y1 = sorted((sy*0.250, sy*0.175))
        c = B.box("Body", "base", (x0, y0, -0.215), (x1, y1, 0.150), name="column"); bev(B, c, 0.008, (3, 1))
        g = B.prism("Body", "base", [(0.0, 0.0), (0.070, 0.0), (0.0, 0.090)], sy*0.2125 - 0.010, sy*0.2125 + 0.010, ("x", "z", "y"), name="gusset"); B.move(g, T(sx*0.175, 0, -0.215) @ Matrix.Diagonal((-sx, 1, 1, 1)))
        g = B.prism("Body", "base", [(0.0, 0.0), (0.070, 0.0), (0.0, 0.090)], sx*0.2125 - 0.010, sx*0.2125 + 0.010, ("y", "z", "x"), name="gusset"); B.move(g, T(0, sy*0.175, -0.215) @ Matrix.Diagonal((1, -sy, 1, 1)))
    for (a, b) in (((-0.250, -0.250, 0.095), (0.250, -0.180, 0.150)), ((-0.250, 0.180, 0.095), (0.250, 0.250, 0.150)), ((-0.250, -0.180, 0.095), (-0.180, 0.180, 0.150)), ((0.180, -0.180, 0.095), (0.250, 0.180, 0.150))):
        r = B.box("Body", "base", a, b, name="ringbeam"); bev(B, r, 0.006, (3, 1))
    for (a, b) in (((-0.240, -0.2515, 0.120), (0.240, -0.2455, 0.146)), ((-0.240, 0.2455, 0.120), (0.240, 0.2515, 0.146))):
        h = B.box("Body", "sec", a, b, name="hazard"); bev(B, h, 0.002, (2, 1))
    for (sx, sy) in ((-1, -1), (1, -1), (-1, 1), (1, 1)):
        for k in range(2): B.cyl("Body", "bare", (sx*0.213, sy*(0.130 - 0.090*k), 0.150), (sx*0.213, sy*(0.130 - 0.090*k), 0.157), 0.010, (6, 6), "detail", "bolt")
    for (a, b) in (((-0.175, -0.252, -0.200), (0.175, -0.235, -0.020)), ((-0.175, 0.235, -0.200), (0.175, 0.252, -0.020)), ((-0.252, -0.175, -0.200), (-0.235, 0.175, -0.020)), ((0.235, -0.175, -0.200), (0.252, 0.175, -0.020))):
        ch = B.box("Body", "alt", a, b, name="cheek"); bev(B, ch, 0.006, (2, 1))
    for x in (-0.085, 0.085):                                                                                   # hydraulic rams: platform -> base
        B.cyl("Body", "metal", (x, 0, -0.215), (x, 0, -0.080), 0.034, (24, 12), name="ramcyl")
        B.cyl("Body", "bare", (x, 0, -0.080), (x, 0, 0.010), 0.022, (24, 12), name="ramrod")
        B.cyl("Body", "dark", (x, 0, -0.090), (x, 0, -0.072), 0.040, (24, 12), name="ramcollar")
    bm = B.box("Body", "base", (-0.140, -0.040, -0.004), (0.140, 0.040, 0.020), name="crossbeam"); bev(B, bm, 0.005, (2, 1))
    pl = B.box("Body", "dark", (-0.160, -0.160, 0.020), (0.160, 0.160, 0.050), name="platform"); bev(B, pl, 0.006, (2, 1))
    for (a, b) in (((-0.160, -0.160, 0.050), (0.160, -0.128, 0.054)), ((-0.160, 0.128, 0.050), (0.160, 0.160, 0.054)), ((-0.160, -0.128, 0.050), (-0.128, 0.128, 0.054)), ((0.128, -0.128, 0.050), (0.160, 0.128, 0.054))):
        B.box("Body", "sec", a, b, name="border")
    for i in range(12):                                                                                         # black chevrons on the yellow border (source only, baked)
        t = -0.150 + i*0.026
        for (sx, sy, rot) in ((t, -0.144, 0), (t, 0.144, 0), (-0.144, t, 1), (0.144, t, 1)):
            st = B.box("Body", "dark", (-0.005, -0.016, 0.054), (0.005, 0.016, 0.0548), "detail", "stripe")
            B.move(st, T(sx, sy, 0) @ RZ(math.pi/4 + rot*math.pi/2))
    tt = ring(B, "Iris", "metal", 0.060, 0.126, 0.0505, 0.074, (64, 24), name="turntable"); bev(B, tt, 0.004, (2, 1))
    for i in range(4):
        j = B.box("Iris", "sec", (0.108, -0.020, 0.074), (0.136, 0.020, 0.104), name="jaw"); bev(B, j, 0.004, (2, 1)); B.move(j, RZ(TAU*i/4 + math.pi/4))
        jd = B.box("Iris", "dark", (0.102, -0.012, 0.080), (0.109, 0.012, 0.100), name="jawpad"); B.move(jd, RZ(TAU*i/4 + math.pi/4))
    B.meta = dict(dock_z=0.074, family="square frame + hazard edges; 4 clamp jaws = 4 clamp lugs of the drone")


def drone_2(B):
    """Brutal Industrial drone: an H-frame heavy lifter - two thick box rails carry the four rotors at their ends, the armoured box hangs between
    the rails on two cross beams, four clamp lugs under the box (what the launcher's jaws hold), square guard frames with yellow hazard
    corners, a heavy sensor block in front"""
    zc = 0.064; B.meta = dict(body_pivot=(0, 0, zc), rotors={}, muzzle=(0, -0.112, zc - 0.004), dock_z=0.0)
    for sx in (-1, 1):
        r = B.box("Body", "base", (sx*0.118 - 0.020, -0.168, zc + 0.004), (sx*0.118 + 0.020, 0.168, zc + 0.040), name="rail"); bev(B, r, 0.005, (3, 1))
        for sy in (-1, 1):
            y0, y1 = sorted((sy*0.1685, sy*0.146)); B.box("Body", "sec", (sx*0.118 - 0.0205, y0, zc + 0.0035), (sx*0.118 + 0.0205, y1, zc + 0.0405), name="railend")
    for y in (-0.050, 0.050):
        cb = B.box("Body", "base", (-0.100, y - 0.016, zc + 0.008), (0.100, y + 0.016, zc + 0.034), name="crossbeam"); bev(B, cb, 0.004, (2, 1))
    b = B.prism("Body", "alt", chamf(0.070, 0.080, 0.020), zc - 0.044, zc + 0.012, ("x", "y", "z"), name="box"); bev(B, b, 0.006, (3, 1))
    s = B.box("Body", "dark", (-0.036, -0.112, zc - 0.034), (0.036, -0.078, zc + 0.004), name="sensorblock"); bev(B, s, 0.005, (2, 1))
    B.cyl("Body", "glow", (0, -0.108, zc - 0.014), (0, -0.116, zc - 0.014), 0.011, (20, 10), name="eye")
    for (sx, sy) in ((-1, -1), (1, -1), (-1, 1), (1, 1)):
        lug = B.box("Body", "dark", (sx*0.058 - 0.012, sy*0.058 - 0.012, 0.0), (sx*0.058 + 0.012, sy*0.058 + 0.012, zc - 0.044), name="lug"); bev(B, lug, 0.003, (2, 1))
    k = 0
    for (sx, sy) in ((-1, -1), (1, -1), (-1, 1), (1, 1)):
        k += 1; c = (sx*0.150, sy*0.160, zc + 0.054); nm = "Rotor_%d" % k
        B.box("Body", "base", (c[0] - 0.016, c[1] - 0.016, zc + 0.040), (c[0] + 0.016, c[1] + 0.016, c[2] - 0.014), name="pylon")
        for (a, b2) in (((-0.066, -0.066, -0.010), (0.066, -0.054, 0.010)), ((-0.066, 0.054, -0.010), (0.066, 0.066, 0.010)), ((-0.066, -0.054, -0.010), (-0.054, 0.054, 0.010)), ((0.054, -0.054, -0.010), (0.066, 0.054, 0.010))):
            g = B.box("Body", "base", a, b2, name="guard"); bev(B, g, 0.003, (2, 1)); B.move(g, T(c[0], c[1], c[2] - 0.004))
        for (qx, qy) in ((-1, -1), (1, -1), (-1, 1), (1, 1)):
            cr = B.box("Body", "sec", (-0.010, -0.010, 0.0), (0.010, 0.010, 0.006), name="corner"); B.move(cr, T(c[0] + qx*0.060, c[1] + qy*0.060, c[2] + 0.006))
        B.cyl(nm, "metal", (c[0], c[1], c[2] - 0.014), (c[0], c[1], c[2] + 0.008), 0.013, (16, 8), name="hub")
        for i in range(2):
            bl = B.prism(nm, "dark", [(0.012, -0.012), (0.054, -0.010), (0.054, 0.010), (0.012, 0.012)], -0.002, 0.002, ("x", "y", "z"), name="blade"); B.move(bl, T(*c) @ RZ(math.pi*i + 0.4) @ RX(0.18))
        B.meta["rotors"][nm] = c


# ================================================================================================ style 3 Arcade Low-Poly
# family cue: the hexagon and the big orange triangle; everything is a few large flat facets
def dep_3(B):
    """Arcade Low-Poly: a hexagonal deck on three wedge legs with open gaps between them (light shows through), a square foot plate for the
    port; the pad is a sunk hexagon with ONE big lime triangle pointing forward (orange before the moodboard palette review); six wedge petals round the pad spin; every surface is a
    flat facet - no bevel smoothing, segment counts of 6"""
    f = B.prism("Body", "metal", chamf(0.250, 0.250, 0.060), -0.260, -0.226, ("x", "y", "z"), name="foot")
    B.loft("Body", "dark", [[(x, y, -0.226) for (x, y) in ngon(0.240, 6, TAU/12)], [(x, y, -0.200) for (x, y) in ngon(0.225, 6, TAU/12)]], name="hexplate")
    # each leg: a tapering slab from the hex plate to the deck, at radius 0.12-0.20
    for i in range(3):
        a = TAU*i/3 + TAU/6; ca, sa = math.cos(a), math.sin(a)
        pts0 = [(0.090, -0.050), (0.215, -0.065), (0.215, 0.065), (0.090, 0.050)]; pts1 = [(0.080, -0.040), (0.190, -0.050), (0.190, 0.050), (0.080, 0.040)]
        r0 = [(ca*x - sa*y, sa*x + ca*y, -0.200) for (x, y) in pts0]; r1 = [(ca*x - sa*y, sa*x + ca*y, -0.040) for (x, y) in pts1]
        B.loft("Body", "base", [r0, r1], name="legslab")
    dk = B.loft("Body", "base", [[(x, y, -0.050) for (x, y) in ngon(0.215, 6, TAU/12)], [(x, y, -0.010) for (x, y) in ngon(0.245, 6, TAU/12)], [(x, y, 0.040) for (x, y) in ngon(0.228, 6, TAU/12)]], name="deck")
    B.cut(dk, B.loft("Body", "base", [[(x, y, 0.022) for (x, y) in ngon(0.165, 6, TAU/12)], [(x, y, 0.080) for (x, y) in ngon(0.165, 6, TAU/12)]], kind="cutter"))
    B.loft("Body", "sec", [[(x, y, -0.012) for (x, y) in ngon(0.2465, 6, TAU/12)], [(x, y, 0.004) for (x, y) in ngon(0.2465, 6, TAU/12)]], name="band")
    B.loft("Body", "dark", [[(x, y, 0.018) for (x, y) in ngon(0.166, 6, TAU/12)], [(x, y, 0.024) for (x, y) in ngon(0.166, 6, TAU/12)]], name="padwell")
    tri = B.prism("Body", "sec", [(0.0, -0.120), (0.104, 0.060), (-0.104, 0.060)], 0.018, 0.0242, ("x", "y", "z"), name="triangle")          # flush with the pad: the skids stand on it
    for i in range(6):
        a = TAU*i/6 + TAU/12; ca, sa = math.cos(a), math.sin(a)
        pts = [(0.168, -0.040), (0.222, -0.062), (0.222, 0.062), (0.168, 0.040)]
        p = B.prism("Iris", "base" if i % 2 else "metal", [(ca*x - sa*y, sa*x + ca*y) for (x, y) in pts], 0.0405, 0.066, ("x", "y", "z"), name="petal")
        B.move(p, T(0, 0, 0))
    B.meta = dict(dock_z=0.0245, family="hexagon + big lime triangle")


def drone_3(B):
    """Arcade Low-Poly drone - rebuilt after the moodboard review: a blocky toy helicopter (moodboard 03: modules read as cubes and wedges;
    ref. Trailmakers helicopter). A box cabin with a wedge nose, a hexagonal mast with TWO stacked two-blade rotors (coaxial, counter-rotating,
    so no tail rotor is needed), a tail boom ending in the big lime triangle fin, block skids. Family cue with the launcher: the hexagon
    (mast, roof plate) and the lime triangle. (The earlier gem tri-copter with ring guards read like the 06 quad in greyscale at 128 px -
    moodboard boundary 03 <-> 06.)"""
    zc = 0.075; B.meta = dict(body_pivot=(0, 0, zc), rotors={}, muzzle=(0, -0.152, 0.060), dock_z=0.0)
    for sx in (-1, 1):
        B.box("Body", "dark", (sx*0.058, -0.105, 0.000), (sx*0.074, 0.100, 0.012), name="skid")
        for y in (-0.050, 0.050): B.box("Body", "dark", (sx*0.060, y - 0.008, 0.012), (sx*0.068, y + 0.008, 0.037), name="strut")
    B.box("Body", "base", (-0.062, -0.062, 0.035), (0.062, 0.072, 0.118), name="cabin")
    B.prism("Body", "metal", [(-0.062, 0.035), (-0.062, 0.118), (-0.128, 0.078), (-0.128, 0.046)], -0.052, 0.052, ("y", "z", "x"), name="nose")
    B.prism("Body", "glass", [(-0.066, 0.1196), (-0.110, 0.0929), (-0.110, 0.0849), (-0.066, 0.1116)], -0.040, 0.040, ("y", "z", "x"), name="windscreen")
    B.prism("Body", "dark", ngon(0.011, 6, TAU/12), -0.152, -0.124, ("x", "z", "y"), name="gun", at=(0, 0, 0.060))
    B.prism("Body", "dark", ngon(0.046, 6, TAU/12), 0.118, 0.126, ("x", "y", "z"), name="roofhex", at=(0, 0.004, 0))
    B.prism("Body", "dark", ngon(0.018, 6, TAU/12), 0.126, 0.212, ("x", "y", "z"), name="mast")
    B.box("Body", "base", (-0.017, 0.072, 0.074), (0.017, 0.196, 0.098), name="boom")
    B.prism("Body", "sec", [(0.140, 0.098), (0.205, 0.098), (0.205, 0.160)], -0.005, 0.005, ("y", "z", "x"), name="triangle")
    B.prism("Body", "metal", [(-0.052, 0.166), (0.052, 0.166), (0.034, 0.196), (-0.034, 0.196)], 0.083, 0.090, ("x", "y", "z"), name="stabiliser")
    for k, (zr, rad, zone) in enumerate(((0.176, 0.212, "sec"), (0.206, 0.196, "alt"))):
        nm = "Rotor_%d" % (k + 1); c = (0.0, 0.0, zr)
        B.prism(nm, "dark", ngon(0.026, 6, TAU/12), zr - 0.007, zr + 0.007, ("x", "y", "z"), name="hub")
        for b in range(2):
            bl = B.prism(nm, zone, [(0.022, -0.015), (rad, -0.019), (rad, 0.019), (0.022, 0.015)], -0.003, 0.003, ("x", "y", "z"), name="blade")
            B.move(bl, T(*c) @ RZ(math.pi*b + (math.pi/2)*k))
        B.meta["rotors"][nm] = c


# ================================================================================================ style 4 Gritty Industrial
# family cue: the oil drum - the launcher is a cut-down drum on a welded frame, the drone's body is a small drum with the same ribs and strap
def dep_4(B):
    """Gritty Industrial: readable history - angle-iron carrier frame (original), half an oil drum stood on it as the launch tube (re-used),
    an old wheel rim inside as the pad turntable (Iris), a bent shield plate bolted on later in front, one replaced side plate in primer,
    a hose clamp and a taped cable run up the side, a scavenged battery box strapped on one corner; asymmetric outline"""
    bp = B.prism("Body", "metal", [(-0.250, -0.250), (0.236, -0.250), (0.250, -0.226), (0.250, 0.250), (-0.226, 0.250), (-0.250, 0.214)], -0.260, -0.240, ("x", "y", "z"), name="plate")
    for (sx, sy) in ((-1, -1), (1, -1), (-1, 1), (1, 1)):                                                          # angle-iron legs
        a = B.prism("Body", "dark", [(0, 0), (0.044, 0), (0.044, 0.008), (0.008, 0.008), (0.008, 0.044), (0, 0.044)], -0.240, -0.020 if (sx, sy) != (1, 1) else -0.075, ("x", "y", "z"), name="angle")
        B.move(a, T(sx*0.228, sy*0.228, 0) @ Matrix.Diagonal((-sx, -sy, 1, 1)))
        B.weld("Body", [(sx*0.228, sy*0.228, -0.240), (sx*0.228 - sx*0.044, sy*0.228, -0.240)], 0.004)
    for (a0, b0) in (((-0.228, -0.232, -0.140), (0.228, -0.224, -0.104)), ((-0.228, 0.224, -0.140), (0.228, 0.232, -0.104)), ((-0.232, -0.228, -0.140), (-0.224, 0.228, -0.104))):
        B.box("Body", "dark", a0, b0, name="rail")
    rp = B.box("Body", "alt2", (0.226, -0.180, -0.230), (0.238, 0.150, -0.050), name="replacedplate")                 # replaced side plate (primer)
    for (y, z) in ((-0.165, -0.215), (0.135, -0.215), (-0.165, -0.065), (0.135, -0.065)): B.cyl("Body", "bare", (0.238, y, z), (0.244, y, z), 0.008, (6, 6), "detail", "bolt")
    dprof = [(0.186, -0.215), (0.194, -0.205), (0.194, -0.160), (0.200, -0.152), (0.200, -0.140), (0.194, -0.132), (0.194, -0.010), (0.200, -0.002), (0.200, 0.010), (0.194, 0.018), (0.194, 0.104),
             (0.202, 0.110), (0.204, 0.118), (0.198, 0.124), (0.188, 0.120), (0.186, 0.110)]
    d = B.rev("Body", "base", dprof, (64, 20), "z", closed=True, name="drum")
    B.rev("Body", "dark", [(0.0, -0.035), (0.186, -0.035), (0.186, -0.025), (0.0, -0.025)], (64, 20), "z", name="floor")
    for z in (-0.146, 0.004): B.weld("Body", [(0.199*math.cos(TAU*i/24), 0.199*math.sin(TAU*i/24), z) for i in range(25)], 0.003, "bare") if B.hi else None
    ring(B, "Body", "metal", 0.199, 0.212, -0.080, -0.064, (48, 16), name="clamp")
    B.box("Body", "metal", (0.205, -0.014, -0.090), (0.232, 0.014, -0.054), name="clampscrew")
    sh = B.prism("Body", "alt", [(-0.150, 0.0), (0.140, 0.0), (0.166, 0.040), (0.120, 0.172), (-0.120, 0.180), (-0.162, 0.060)], -0.214, -0.200, ("x", "z", "y"), name="shield")
    B.move(sh, T(0, 0, -0.205) @ RX(math.radians(8)) @ T(0, -0.002, 0))
    for (x, z) in ((-0.120, -0.180), (0.110, -0.180), (-0.100, -0.060), (0.090, -0.050)): B.cyl("Body", "bare", (x, -0.212, z), (x, -0.222, z), 0.009, (6, 6), "detail", "bolt")
    br = B.box("Body", "metal", (-0.030, -0.222, -0.110), (0.030, -0.196, -0.090), name="bracket")
    bx = B.box("Body", "sec", (-0.248, -0.248, -0.238), (-0.130, -0.150, -0.150), name="battbox"); B.bevel(bx, 0.004, (2, 1))
    B.box("Body", "dark", (-0.250, -0.205, -0.240), (-0.128, -0.190, -0.146), name="strap")
    B.pipe("Body", "rubber", [(-0.190, -0.150, -0.150), (-0.205, -0.100, -0.080), (-0.205, -0.020, 0.020), (-0.192, 0.030, 0.100), (-0.150, 0.060, 0.118)], 0.010, (10, 6), (6, 3), name="cable")
    for t in ((-0.205, -0.090, -0.060), (-0.200, 0.000, 0.040)): B.cyl("Body", "alt2", (t[0], t[1] - 0.012, t[2]), (t[0], t[1] + 0.012, t[2]), 0.014, (10, 6), name="tape")
    rim = B.rev("Iris", "metal", [(0.0, 0.002), (0.040, 0.002), (0.046, 0.010), (0.130, 0.010), (0.150, 0.024), (0.168, 0.026), (0.168, 0.034), (0.144, 0.032), (0.128, 0.018), (0.040, 0.018), (0.0, 0.018)],
                (48, 16), "z", (0, 0, -0.025), name="rim")
    for i in range(5):
        h = B.cyl("Iris", "dark", (0.085*math.cos(TAU*i/5), 0.085*math.sin(TAU*i/5), -0.030), (0.085*math.cos(TAU*i/5), 0.085*math.sin(TAU*i/5), 0.0), 0.012, (12, 6), "cutter")
        B.cut(rim, h)
    ring(B, "Iris", "alt2", 0.100, 0.126, -0.007, -0.0062, (48, 16), name="paintring")
    B.meta = dict(dock_z=-0.0035, family="oil drum with two ribs, hose clamp, primer patch")


def drone_4(B):
    """Gritty Industrial drone: a small drum lying along the flight axis (same ribs and hose clamp as the launch tube), a welded roll bar over
    it, four pipe arms of different length welded on, mismatched rotors (two big two-blade props, one three-blade, one inside a bent wire
    guard), a scavenged battery strapped on top, a hook underneath, a bent antenna - asymmetric on purpose"""
    zc = 0.070; B.meta = dict(body_pivot=(0, 0, zc), rotors={}, muzzle=(0, -0.130, zc), dock_z=0.0)
    prof = [(0.0, -0.115), (0.056, -0.115), (0.064, -0.108), (0.064, -0.082), (0.069, -0.077), (0.069, -0.067), (0.064, -0.062), (0.064, 0.058), (0.069, 0.063), (0.069, 0.073), (0.064, 0.078), (0.064, 0.108), (0.056, 0.115), (0.0, 0.115)]
    B.rev("Body", "base", prof, (40, 14), "y", (0, 0, zc), name="can")
    ring(B, "Body", "metal", 0.065, 0.072, -0.012, 0.006, (40, 14), "y", (0, 0, zc), name="clamp")
    B.box("Body", "metal", (0.064, -0.012, zc - 0.006), (0.084, 0.006, zc + 0.008), name="clampscrew")
    B.cyl("Body", "glass", (0, -0.110, zc), (0, -0.128, zc), 0.022, (16, 8), name="lamp"); ring(B, "Body", "metal", 0.022, 0.029, -0.130, -0.114, (16, 8), "y", (0, 0, zc), name="lampring")
    B.pipe("Body", "metal", [(-0.066, -0.060, zc), (-0.050, -0.060, zc + 0.058), (0.050, -0.060, zc + 0.058), (0.066, -0.060, zc)], 0.006, (10, 6), (6, 3), name="rollbar")
    bt = B.box("Body", "alt2", (-0.036, -0.030, zc + 0.058), (0.032, 0.060, zc + 0.092), name="battery"); B.bevel(bt, 0.003, (2, 1))
    B.box("Body", "dark", (-0.040, 0.004, zc + 0.050), (0.036, 0.020, zc + 0.096), name="strap")
    B.pipe("Body", "bare", [(0.022, 0.090, zc + 0.060), (0.034, 0.110, zc + 0.120), (0.066, 0.120, zc + 0.150)], 0.0022, (6, 4), (6, 3), name="antenna")
    B.pipe("Body", "metal", [(0.0, 0.020, zc - 0.064), (0.0, 0.020, 0.012), (0.0, 0.004, 0.004), (0.0, -0.010, 0.012)], 0.004, (8, 6), (5, 3), name="hook")
    for x in (-0.040, 0.040):
        B.pipe("Body", "dark", [(x, -0.080, zc - 0.050), (x, -0.080, 0.004), (x, 0.080, 0.004), (x, 0.080, zc - 0.050)], 0.005, (8, 6), (4, 2), name="skid")
    spec = [((-1, -1), 0.150, 0.074, 2, False), ((1, -1), 0.148, 0.064, 3, False), ((-1, 1), 0.140, 0.060, 2, True), ((1, 1), 0.156, 0.076, 2, False)]
    for k, ((sx, sy), Rr, rr, nb, wire) in enumerate(spec):
        a = math.atan2(sy, sx); c = (Rr*math.cos(a), Rr*math.sin(a), zc + 0.076 + (0.008 if k % 2 else 0.0)); nm = "Rotor_%d" % (k + 1)
        B.pipe("Body", "metal", [(0.040*sx, 0.050*sy, zc + 0.010), (0.55*c[0], 0.55*c[1], zc + 0.020), (c[0], c[1], c[2] - 0.024)], 0.009, (10, 6), (5, 3), name="arm")
        B.weld("Body", [(0.044*sx, 0.050*sy - 0.012, zc + 0.020), (0.044*sx, 0.050*sy + 0.012, zc + 0.020)], 0.004)
        B.cyl("Body", "dark", (c[0], c[1], c[2] - 0.030), (c[0], c[1], c[2] - 0.006), 0.022, (16, 8), name="motor")
        B.cyl(nm, "metal", (c[0], c[1], c[2] - 0.006), (c[0], c[1], c[2] + 0.005), 0.009, (12, 6), name="hub")
        for i in range(nb):
            bl = B.prism(nm, "dark" if i else "alt", [(0.008, -0.011), (rr, -0.009), (rr, 0.009), (0.008, 0.011)], -0.002, 0.002, ("x", "y", "z"), name="blade"); B.move(bl, T(*c) @ RZ(TAU*i/nb + 0.7*k) @ RX(0.2))
        if wire:
            B.tor("Body", "bare", rr + 0.010, 0.0028, (40, 12), (6, 4), "z", (c[0], c[1], c[2] - 0.004), name="wireguard")
            for i in range(3): B.pipe("Body", "bare", [(c[0], c[1], c[2] - 0.026), (c[0] + (rr + 0.010)*math.cos(TAU*i/3), c[1] + (rr + 0.010)*math.sin(TAU*i/3), c[2] - 0.004)], 0.0022, (6, 4), (2, 1), name="spoke")
        B.meta["rotors"][nm] = c


# ================================================================================================ style 5 Comic Outline
# family cue: the fat bumper torus - the launcher's porthole ring and the drone's single huge eye ring are the same fat ring, navy with an orange inner band
def dep_5(B):
    """Comic Outline: exaggerated function - a squat rounded bun with a huge tilted launch porthole (fat navy bumper ring, oversized petal
    shutter that spins), a star badge on the front, stubby feet; proportions pushed: the porthole is almost the full width of the cell"""
    f = B.box("Body", "dark", (-0.240, -0.240, -0.260), (0.240, 0.240, -0.236), name="foot"); bev(B, f, 0.018, (4, 2))
    bun = B.rev("Body", "base", spl([(0.0, -0.236, 'c'), (0.200, -0.236), (0.236, -0.205), (0.244, -0.150), (0.236, -0.090), (0.214, -0.050), (0.180, -0.030), (0.0, -0.030, 'c')], B.q(6, 3)),
                (64, 24), "z", name="bun")
    tilt = RX(0.0); at = T(0, 0.0, -0.006)
    for (o, M) in ((B.tor("Body", "dark", 0.190, 0.038, (64, 24), (16, 8), "z", (0, 0, 0), name="bumper"), None), (B.tor("Body", "sec", 0.152, 0.010, (64, 24), (10, 6), "z", (0, 0, 0.012), name="innerband"), None),
                   (disc(B, "Body", "metal", 0.160, -0.030, 0.004, (64, 24), name="bay"), None)):
        B.move(o, at @ tilt)
    for i in range(5):
        p = B.prism("Iris", "alt" if i % 2 == 0 else "sec", [(0.0, 0.0), (0.132, -0.045), (0.132, 0.045)], 0.0045, 0.022, ("x", "y", "z"), name="petal")
        bev(B, p, 0.008, (3, 1), 30); B.move(p, at @ tilt @ RZ(TAU*i/5))
    hub = B.rev("Iris", "dark", spl([(0.0, 0.040, 'c'), (0.030, 0.034), (0.040, 0.020), (0.040, 0.004, 'c'), (0.0, 0.004, 'c')], B.q(5, 2)), (32, 12), "z", name="hub"); B.move(hub, at @ tilt)
    st = B.prism("Body", "alt", star(0.024, 0.056, 5, math.pi/2), -0.010, 0.010, ("x", "z", "y"), name="star", at=(0, -0.236, -0.140)); bev(B, st, 0.004, (2, 1))
    for (sx, sy) in ((-1, -1), (1, -1), (-1, 1), (1, 1)):
        ft = B.rev("Body", "metal", spl([(0.0, -0.240, 'c'), (0.040, -0.240), (0.048, -0.226), (0.040, -0.212), (0.0, -0.210, 'c')], B.q(5, 2)), (24, 10), "z", (sx*0.170, sy*0.170, 0), name="toe")
    B.meta = dict(dock_z=0.0305, family="fat navy bumper ring with orange inner band = the drone's eye ring")


def drone_5(B):
    """Comic Outline drone: a bubble body with ONE huge eye (the same fat ring as the launcher's porthole), two oversized rotors on short
    stalks like ears (a bicopter), stubby fins; the function part - the eye and the rotors - is drawn bigger than it would be"""
    zc = 0.078; B.meta = dict(body_pivot=(0, 0, zc), rotors={}, muzzle=(0, -0.110, zc), dock_z=0.0)
    B.rev("Body", "base", spl([(0.0, zc - 0.074, 'c'), (0.052, zc - 0.064), (0.080, zc - 0.030), (0.084, zc + 0.004), (0.074, zc + 0.040), (0.044, zc + 0.066), (0.0, zc + 0.072, 'c')], B.q(6, 3)), (48, 20), "z", name="bubble")
    eye = B.tor("Body", "dark", 0.050, 0.016, (48, 20), (12, 6), "y", (0, -0.070, zc + 0.002), name="eyering")
    B.tor("Body", "sec", 0.040, 0.0045, (48, 20), (8, 4), "y", (0, -0.080, zc + 0.002), name="eyeband")
    B.rev("Body", "glass", spl([(0.0, -0.090, 'c'), (0.024, -0.087), (0.036, -0.078), (0.040, -0.066, 'c'), (0.0, -0.060, 'c')], B.q(5, 2)), (40, 16), "y", (0, 0, zc + 0.002), name="lens")
    B.rev("Body", "glow", [(0.0, -0.0905), (0.010, -0.0905), (0.010, -0.088), (0.0, -0.088)], (24, 10), "y", (0, 0, zc + 0.002), name="pupil")
    for sx in (-1, 1):
        fin = B.prism("Body", "dark", [(0.0, 0.0), (0.060, -0.010), (0.070, 0.030), (0.0, 0.040)], -0.008, 0.008, ("y", "z", "x"), name="fin"); bev(B, fin, 0.006, (3, 1))
        B.move(fin, T(sx*0.060, 0.040, zc - 0.060) @ RZ(sx*0.3))
        c = (sx*0.128, 0.010, zc + 0.084); nm = "Rotor_%d" % (1 if sx < 0 else 2)
        B.pipe("Body", "metal", [(sx*0.050, 0.010, zc + 0.050), (sx*0.098, 0.010, zc + 0.058), (c[0], c[1], c[2] - 0.016)], 0.011, (16, 8), (5, 3), name="stalk")
        B.rev("Body", "dark", spl([(0.0, -0.030, 'c'), (0.020, -0.028), (0.026, -0.014), (0.022, -0.006, 'c'), (0.0, -0.006, 'c')], B.q(5, 2)), (24, 10), "z", c, name="pod")
        B.rev(nm, "alt", spl([(0.0, 0.010, 'c'), (0.016, 0.008), (0.020, 0.0, 'c'), (0.0, -0.006, 'c')], B.q(5, 2)), (24, 10), "z", c, name="cap")
        for i in range(2):
            bl = B.hull(nm, "sec", [(0.010, 0, 0, 0.012, 0.003, 2.4), (0.050, 0, 0, 0.022, 0.004, 2.4), (0.086, 0, 0, 0.016, 0.003, 2.4), (0.094, 0, 0, 0.006, 0.002, 2.4)], n=(16, 8), m=(5, 2), axis="x", name="blade")
            B.move(bl, T(*c) @ RZ(math.pi*i + 0.5) @ RX(0.25))
        B.meta["rotors"][nm] = c


# ================================================================================================ style 6 R41 Modular Tech
# three features read off the real R4.1-family models (R41_Turret_v6.1, R4W_Basic PR #939, R4M_Wheel PR #941):
#  1. gunmetal plating with chamfered octagonal skirts, recessed panel seams, louvre grilles, flat-headed bolts
#  2. the horizontal TEAM-RED band round the body and yellow hazard trim on the black square plinth with corner tabs
#  3. the orange "eye" ring - a turned ring inlay on a side face, repeated on the hub / mount plate
R41_FEATURES = ["gunmetal chamfered octagonal plating with panel seams, louvres and bolts", "horizontal team-red band + yellow hazard trim on a black square plinth with corner tabs",
                "orange turned 'eye' ring inlay on a side face (turret cheek, wheel mount plate, hub)"]
def plinth6(B, node="Body", z0=-0.260, z1=-0.226):
    p = B.prism(node, "dark", chamf(0.252, 0.252, 0.030), z0, z1, ("x", "y", "z"), name="plinth"); bev(B, p, 0.003, (2, 1))
    for (sx, sy) in ((-1, -1), (1, -1), (-1, 1), (1, 1)):
        t = B.prism(node, "dark", [(0, 0), (0.060, 0), (0, 0.060)], z0, z1 + 0.006, ("x", "y", "z"), name="tab"); B.move(t, T(sx*0.246, sy*0.246, 0) @ Matrix.Diagonal((-sx, -sy, 1, 1)))
        h = B.box(node, "alt2", (-0.050, -0.003, z1 - 0.012), (0.050, 0.003, z1), name="hazard"); B.move(h, T(sx*0.180, sy*0.252, 0)) if True else None
    for (sx, sy) in ((-1, -1), (1, -1), (-1, 1), (1, 1)): B.cyl(node, "bare", (sx*0.226, sy*0.226, z1), (sx*0.226, sy*0.226, z1 + 0.005), 0.009, (6, 6), "detail", "bolt")


def dep_6(B):
    """R41 Modular Tech: the R4.1 pedestal language applied to a launcher - black square plinth with corner tabs and hazard trim, a chamfered
    octagonal gunmetal skirt with louvres and the team-red band, an orange eye ring on the front face; on top a round landing pad with the
    light "H" plate (kept from the R4 1x1 deployer) and an orange guide ring with four short guide blocks (the four masts of the 1x1 model
    cut down so the pad stays visible from above)"""
    plinth6(B)
    oc = lambda r, z: [(x, y, z) for (x, y) in ngon(r, 8, TAU/16)]
    sk = B.loft("Body", "base", [oc(0.236, -0.226), oc(0.236, -0.120), oc(0.226, -0.060), oc(0.205, 0.040)], name="skirt")
    B.loft("Body", "alt", [oc(0.205, 0.040), oc(0.198, 0.060)], name="cap")
    B.loft("Body", "sec", [oc(0.240, -0.112), oc(0.240, -0.080), oc(0.2385, -0.075)], name="redband")
    for a in (0.0, math.pi):                                                                                    # louvres on the two side faces (source grooves, baked)
        for k in range(5):
            z = -0.205 + k*0.016
            l = B.box("Body", "dark", (-0.062, -0.006, z), (0.062, 0.006, z + 0.006), "cutter"); B.move(l, RZ(a - math.pi/2) @ T(0, 0.218, 0)); B.cut(sk, l, lo=False)
    ring(B, "Body", "glow", 0.034, 0.048, -0.209, -0.203, (32, 12), "y", (0, 0, -0.030), name="eyering")
    B.cyl("Body", "dark", (0, -0.198, -0.030), (0, -0.207, -0.030), 0.034, (24, 10), name="eyesocket")
    disc(B, "Body", "dark", 0.168, 0.040, 0.062, (64, 20), name="pad")
    for (a, b) in (((-0.072, -0.090, 0.062), (-0.040, 0.090, 0.066)), ((0.040, -0.090, 0.062), (0.072, 0.090, 0.066)), ((-0.040, -0.016, 0.062), (0.040, 0.016, 0.066))):
        B.box("Body", "alt", a, b, name="H")
    ring(B, "Body", "glow", 0.126, 0.134, 0.062, 0.064, (64, 20), name="padring")
    ring(B, "Iris", "alt2", 0.172, 0.198, 0.0605, 0.078, (64, 20), name="guidering")
    for i in range(4):
        g = B.prism("Iris", "base", chamf(0.018, 0.022, 0.006), 0.078, 0.100, ("x", "y", "z"), name="guide"); bev(B, g, 0.003, (2, 1)); B.move(g, RZ(TAU*i/4 + math.pi/4) @ T(0.186, 0, 0))
        tip = B.box("Iris", "glow", (0.176, -0.010, 0.100), (0.196, 0.010, 0.103), name="guidelight"); B.move(tip, RZ(TAU*i/4 + math.pi/4))
    B.meta = dict(dock_z=0.066, family="orange eye ring + team-red band + octagonal chamfered gunmetal")


def drone_6(B):
    """R41 Modular Tech drone: a flat octagonal chamfered gunmetal body (the turret-head shape of R41_Turret_v6.1 laid flat), team-red band,
    orange eye ring in front, four chamfered box arms, rotors inside eight-sided guard rings with yellow hazard tips, black skids"""
    zc = 0.058; B.meta = dict(body_pivot=(0, 0, zc), rotors={}, muzzle=(0, -0.104, zc), dock_z=0.0)
    oc = lambda r, z: [(x, y, z) for (x, y) in ngon(r, 8, TAU/16)]
    B.loft("Body", "base", [oc(0.070, zc - 0.034), oc(0.090, zc - 0.014), oc(0.090, zc + 0.012), oc(0.068, zc + 0.034)], name="body")
    B.loft("Body", "sec", [oc(0.0915, zc - 0.006), oc(0.0915, zc + 0.006)], name="redband")
    B.loft("Body", "alt", [oc(0.050, zc + 0.034), oc(0.040, zc + 0.044)], name="hatch")
    ring(B, "Body", "glow", 0.014, 0.022, -0.094, -0.088, (24, 10), "y", (0, 0, zc - 0.002), name="eyering")
    B.cyl("Body", "dark", (0, -0.084, zc - 0.002), (0, -0.093, zc - 0.002), 0.014, (16, 8), name="eye")
    for sx in (-1, 1):
        sk = B.box("Body", "dark", (sx*0.050 - 0.006, -0.066, 0.0), (sx*0.050 + 0.006, 0.066, 0.008), name="skid"); bev(B, sk, 0.003, (2, 1))
        for y in (-0.040, 0.040): B.box("Body", "dark", (sx*0.050 - 0.004, y - 0.004, 0.008), (sx*0.050 + 0.004, y + 0.004, zc - 0.030), name="strut")
    k = 0
    for (sx, sy) in ((-1, -1), (1, -1), (-1, 1), (1, 1)):
        k += 1; c = (sx*0.145, sy*0.145, zc + 0.012); nm = "Rotor_%d" % k
        arm = B.prism("Body", "base", chamf(0.5, 0.014, 0.006), zc - 0.016, zc - 0.002, ("x", "y", "z"), name="arm")
        L = math.hypot(c[0], c[1]) - 0.075; B.move(arm, T(sx*0.053, sy*0.053, 0) @ RZ(math.atan2(sy, sx)) @ T(L/2, 0, 0) @ Matrix.Diagonal((L, 1, 1, 1)))
        B.cyl("Body", "dark", (c[0], c[1], zc - 0.016), (c[0], c[1], c[2] - 0.012), 0.016, (16, 8), name="motor")
        go = B.loft("Body", "base", [[(c[0] + x, c[1] + y, c[2] - 0.016) for (x, y) in ngon(0.068, 8, TAU/16)], [(c[0] + x, c[1] + y, c[2] + 0.010) for (x, y) in ngon(0.068, 8, TAU/16)]], name="guard")
        gi = B.loft("Body", "base", [[(c[0] + x, c[1] + y, c[2] - 0.018) for (x, y) in ngon(0.058, 8, TAU/16)], [(c[0] + x, c[1] + y, c[2] + 0.012) for (x, y) in ngon(0.058, 8, TAU/16)]], name="gcut", kind="cutter")
        B.cut(go, gi); bev(B, go, 0.002, (2, 1))
        tp = B.box("Body", "alt2", (-0.012, -0.004, -0.010), (0.012, 0.004, 0.010), name="hazardtip"); B.move(tp, T(c[0], c[1], c[2]) @ RZ(math.atan2(sy, sx) - math.pi/2) @ T(0, 0.064, 0))
        B.cyl(nm, "metal", (c[0], c[1], c[2] - 0.012), (c[0], c[1], c[2] + 0.008), 0.012, (16, 8), name="hub")
        for i in range(3):
            bl = B.prism(nm, "dark", [(0.010, -0.009), (0.049, -0.007), (0.049, 0.007), (0.010, 0.009)], -0.0018, 0.0018, ("x", "y", "z"), name="blade")
            B.move(bl, T(*c) @ RZ(TAU*i/3 + 0.2) @ RX(0.2))
        B.meta["rotors"][nm] = c


RECIPES = {("R4X_FlyingDroneDeployer", s): f for s, f in zip("123456", (dep_1, dep_2, dep_3, dep_4, dep_5, dep_6))}
RECIPES.update({("FlyingDrone", s): f for s, f in zip("123456", (drone_1, drone_2, drone_3, drone_4, drone_5, drone_6))})


# ================================================================================================ WHEEL R4M_Wheel (rotor ported from Pass 4 where it fits, new mount)
# R4M_Wheel contract: Mount = the static fork under a block (top plate = Socket_Mount face, +Z), rotor axis X at z -0.26, tyre r 0.39, half width
# 0.184, ground at z -0.65. Outer face +X (L/R mirror by scale X -1). The Pass 4 rotors were made for the side-mounted R4M_WheelM_Side_Offroad
# (same 0.39 radius, axis X, outer face +X, x -0.142..0.188): they are moved by (-0.023, 0, -0.26) and keep their Pass 4 geometry.
def port_rotor(B, fn, dx=-0.023, sleeve=None):
    """runs a Pass 4 side-wheel recipe and keeps only its Rotor, moved onto the R4M_Wheel axle. sleeve=(x0, r): a hub sleeve from x0 to the
    rim face (x 0.020) - it carries the brake discs and takes the axle stub of the new mount (sense check: without it the discs float)"""
    n0 = len(B.objs); c0 = len(B.cutters); fn(B); M = T(dx, 0, -0.26); keep = []
    for o in B.objs[n0:]:
        if o["node"] == "Rotor": o.data.transform(M); keep.append(o)
        else: bpy.data.objects.remove(o, do_unlink=True)
    B.objs[n0:] = keep
    for c in B.cutters[c0:]: c.data.transform(M)
    if sleeve: B.cyl("Rotor", "metal", (sleeve[0], 0, -0.26), (0.020, 0, -0.26), sleeve[1], (32, 10), name="hubsleeve")


def top_plate(B, zone, rad=0.012, th=0.036, poly=None):
    if poly: p = B.prism("Mount", zone, poly, 0.260 - th, 0.260, ("x", "y", "z"), name="plate")
    else: p = B.box("Mount", zone, (-0.250, -0.250, 0.260 - th), (0.250, 0.250, 0.260), name="plate")
    bev(B, p, rad, (3, 1)); return p


def fender(B, zone, x0, x1, r0, r1, a=33, seg=(64, 16), bv=0.004):
    """half-width fender over the INNER half of the tread: it ties the top plate to the arm and leaves the outer half of the tread in view"""
    f = B.rev("Mount", zone, [(r0, x0), (r1, x0), (r1, x1), (r0, x1)], seg, "x", (0, 0, -0.26), True, name="fender", arc=(90 - a, 90 + a))
    if bv: bev(B, f, bv, (2, 1), 30)
    br = B.box("Mount", zone, (x0, -0.050, -0.26 + r1 - 0.004), (x0 + 0.030, 0.050, 0.226), name="fenderbracket"); return f


def wheel_1(B):
    """Clean Futuristic wheel - rebuilt after the moodboard review (rr-style-directions: one long smooth profile, panels hide the mechanism,
    'light rims of the wheels run into thin fairing lines', test 128 px = slim body + light). Rotor: new - a smooth slick tyre with rounded
    shoulders, a closed ceramic disc with a cyan light ring at the rim and three narrow seams (spin cue). Mount: one smooth fairing from the
    plate down to an enclosed motor; a near full-width aero fender whose outer edge is a thin cyan line - the light ring continues in it.
    (The Pass 4 chevron rotor read exactly like the 02 wheel in greyscale - moodboard boundary 01 <-> 02.)"""
    at = (0, 0, -0.26)
    B.rev("Rotor", "rubber", spl([(0.262, -0.166, 'c'), (0.322, -0.170), (0.364, -0.164), (0.384, -0.140), (0.390, -0.090), (0.390, 0.080), (0.384, 0.128),
                                  (0.364, 0.150), (0.322, 0.156), (0.262, 0.152, 'c')], B.q(6, 2)), (96, 28), "x", at, True, name="tyre")
    B.rev("Rotor", "base", spl([(0.0, 0.176, 'c'), (0.120, 0.173), (0.220, 0.165), (0.266, 0.156, 'c'), (0.266, 0.138, 'c'), (0.0, 0.138, 'c')], B.q(6, 2)), (96, 28), "x", at, name="disc")
    ring(B, "Rotor", "glow", 0.236, 0.250, 0.1625, 0.1660, (96, 28), "x", at, name="rimlight")
    ring(B, "Rotor", "dark", 0.084, 0.090, 0.1735, 0.1755, (48, 16), "x", at, name="discseam")
    B.rev("Rotor", "dark", [(0.0, -0.150), (0.266, -0.150), (0.266, -0.120), (0.0, -0.120)], (64, 16), "x", at, True, name="innerdisc")
    for i in range(3):
        n = B.prism("Rotor", "dark", [(0.110, 0.160), (0.200, 0.160), (0.200, 0.1677), (0.110, 0.1745)], -0.0035, 0.0035, ("z", "x", "y"), name="seam"); B.move(n, T(*at) @ RX(TAU*i/3))
    top_plate(B, "base", 0.014)
    B.box("Mount", "dark", (-0.240, -0.240, 0.214), (0.240, 0.240, 0.226), name="gasket")
    f = B.hull("Mount", "base", [(0.226, -0.214, 0, 0.032, 0.150, 4.0), (0.120, -0.218, 0, 0.026, 0.110, 3.6), (-0.080, -0.218, 0, 0.022, 0.076, 3.2), (-0.230, -0.214, 0, 0.026, 0.060, 3.0)],
               n=(48, 16), m=(6, 3), axis="z", name="fairing")
    B.rev("Mount", "base", spl([(0.0, -0.250, 'c'), (0.034, -0.248), (0.046, -0.236), (0.050, -0.216), (0.048, -0.190, 'c'), (0.0, -0.190, 'c')], B.q(6, 2)), (64, 24), "x", at, name="motor")
    B.cyl("Rotor", "dark", (-0.170, 0, -0.26), (-0.148, 0, -0.26), 0.042, (48, 12), name="hubboss")
    ring(B, "Mount", "glow", 0.032, 0.038, -0.2505, -0.2490, (64, 24), "x", at, name="motorlight")
    B.cyl("Mount", "metal", (-0.192, 0, -0.26), (-0.160, 0, -0.26), 0.030, (32, 12), name="axle")
    B.box("Mount", "glow", (-0.2475, -0.004, -0.150), (-0.2445, 0.004, 0.160), name="line")
    fender(B, "base", -0.206, 0.160, 0.404, 0.416, 36, (96, 24), 0.005)
    B.rev("Mount", "glow", [(0.405, 0.160), (0.4165, 0.160), (0.4165, 0.1645), (0.405, 0.1645)], (96, 24), "x", at, True, name="fenderlight", arc=(55, 125))


def wheel_2(B):
    """Brutal Industrial wheel. Rotor: the Pass 4 Brutal rotor (tractor bars, bolted wheel-weight ring, cast hub) moved onto the axle. Mount:
    new - the load path is the shape: a thick plate, one massive box arm with two gussets straight down to a big axle boss, a coil-over
    damper beside it from plate to boss, a brake caliper on the inner disc, hazard bands on the arm"""
    import h4defs as H4
    port_rotor(B, H4.wheel_B_new, sleeve=(-0.150, 0.072))
    top_plate(B, "dark", 0.008, 0.040)
    B.cyl("Mount", "metal", (-0.192, 0, -0.26), (-0.146, 0, -0.26), 0.030, (16, 8), name="axlestub")
    for (x, y) in ((-0.200, -0.200), (0.200, -0.200), (-0.200, 0.200), (0.200, 0.200)): B.cyl("Mount", "bare", (x, y, 0.214), (x, y, 0.221), 0.012, (6, 6), "detail", "bolt")
    a = B.box("Mount", "base", (-0.252, -0.066, -0.250), (-0.192, 0.066, 0.220), name="arm"); bev(B, a, 0.008, (3, 1))
    for sy in (-1, 1):
        g = B.prism("Mount", "base", [(0.0, 0.0), (0.150, 0.0), (0.0, -0.110)], -0.246, -0.198, ("y", "z", "x"), name="gusset"); B.move(g, T(0, sy*0.066, 0.220) @ Matrix.Diagonal((1, sy, 1, 1)))
    B.cyl("Mount", "base", (-0.252, 0, -0.26), (-0.190, 0, -0.26), 0.052, (24, 10), name="boss")
    B.cyl("Mount", "bare", (-0.190, 0, -0.26), (-0.172, 0, -0.26), 0.034, (6, 6), name="nut")
    for z in (-0.020, 0.090): B.box("Mount", "sec", (-0.2535, -0.067, z), (-0.1905, 0.067, z + 0.040), name="hazard")
    for (z0, z1) in ((0.214, 0.190), (-0.170, -0.196)): B.box("Mount", "dark", (-0.250, 0.090, z1), (-0.205, 0.150, z0), name="damperlug")
    B.cyl("Mount", "metal", (-0.226, 0.120, 0.190), (-0.226, 0.120, 0.020), 0.024, (20, 8), name="dampercyl")
    B.cyl("Mount", "bare", (-0.226, 0.120, 0.020), (-0.226, 0.120, -0.170), 0.012, (16, 8), name="damperrod")
    B.helix("Mount", "sec", (-0.226, 0.120, 0.150), (-0.226, 0.120, -0.150), 0.023, 0.006, 6.0, (10, 6), (14, 6), name="spring")
    fender(B, "base", -0.206, -0.004, 0.400, 0.428, 34, (48, 12), 0.004)
    for a in (70, 90, 110): B.rev("Mount", "dark", [(0.426, -0.150), (0.436, -0.150), (0.436, -0.020), (0.426, -0.020)], (48, 12), "x", (0, 0, -0.26), True, name="rib", arc=(a - 3, a + 3))


def wheel_3(B):
    """Arcade Low-Poly wheel - new from scratch (the Pass 4 Arcade rotor is round and soft, the opposite of this style): a twelve-sided tyre with
    twelve chunky trapezoid lugs, a white twelve-sided rim, six navy wedge spokes, a big orange hexagonal hub nut; the mount is one wedge arm
    with an orange stripe and a hexagonal boss. Flat facets only, every part readable at small size"""
    at = (0, 0, -0.26)
    B.rev("Rotor", "rubber", [(0.300, -0.158), (0.368, -0.158), (0.372, -0.120), (0.372, 0.120), (0.368, 0.158), (0.300, 0.158)], (12, 12), "x", at, True, name="tyre", phase=TAU/24)
    for i in range(12):
        lg = B.prism("Rotor", "rubber", [(-0.150, -0.050), (0.150, -0.050), (0.120, 0.050), (-0.120, 0.050)], 0.352, 0.389, ("x", "y", "z"), name="lug")
        B.move(lg, T(*at) @ RX(TAU*i/12) @ Matrix.Diagonal((1, 0.92, 1, 1)))
    B.rev("Rotor", "base", [(0.180, -0.120), (0.302, -0.130), (0.302, 0.150), (0.180, 0.136)], (12, 12), "x", at, True, name="rim", phase=TAU/24)
    for i in range(6):
        sp = B.prism("Rotor", "metal", [(-0.024, 0.060), (0.024, 0.060), (0.034, 0.190), (-0.034, 0.190)], 0.040, 0.130, ("y", "z", "x"), name="spoke"); B.move(sp, T(*at) @ RX(TAU*i/6))
    h = B.prism("Rotor", "sec", ngon(0.074, 6, TAU/12), 0.100, 0.168, ("y", "z", "x"), name="hub"); B.move(h, T(*at))
    hc = B.prism("Rotor", "base", ngon(0.040, 6, TAU/12), 0.168, 0.178, ("y", "z", "x"), name="hubcap"); B.move(hc, T(*at))
    inn = B.prism("Rotor", "dark", ngon(0.150, 12, TAU/24), -0.150, -0.120, ("y", "z", "x"), name="inner"); B.move(inn, T(*at))
    p = B.prism("Mount", "metal", chamf(0.250, 0.250, 0.050), 0.222, 0.260, ("x", "y", "z"), name="plate")
    B.prism("Mount", "sec", chamf(0.250, 0.250, 0.050), 0.214, 0.222, ("x", "y", "z"), name="plateband")
    B.prism("Mount", "base", [(-0.110, 0.214), (0.110, 0.214), (0.052, -0.250), (-0.052, -0.250)], -0.250, -0.196, ("y", "z", "x"), name="arm")
    B.prism("Mount", "sec", [(-0.075, 0.100), (0.072, 0.100), (0.062, 0.020), (-0.064, 0.020)], -0.254, -0.249, ("y", "z", "x"), name="stripe")
    bo = B.prism("Mount", "metal", ngon(0.060, 6, 0.0), -0.252, -0.172, ("y", "z", "x"), name="boss"); B.move(bo, T(0, 0, -0.26))
    ax = B.prism("Mount", "dark", ngon(0.028, 6, 0.0), -0.176, -0.136, ("y", "z", "x"), name="axle"); B.move(ax, T(0, 0, -0.26))
    fender(B, "base", -0.206, -0.010, 0.402, 0.426, 33, (6, 6), 0)
    B.rev("Mount", "sec", [(0.426, -0.120), (0.430, -0.120), (0.430, -0.060), (0.426, -0.060)], (6, 6), "x", (0, 0, -0.26), True, name="fenderstripe", arc=(57, 123))


def wheel_4(B):
    """Gritty Industrial wheel. Rotor: the Pass 4 Scrap rotor (bolted steel cleats, rim with a welded patch, a drum from another vehicle)
    moved onto the axle. Mount: new, readable history - an original angle-iron bracket, a leaf-spring pack added later with two U-bolts,
    a primer repair plate welded over a crack in the top plate, a taped cable, one bolt missing on the plate"""
    import h4defs as H4
    port_rotor(B, H4.wheel_D)
    top_plate(B, "metal", 0.004, 0.030, [(-0.250, -0.250), (0.250, -0.250), (0.250, 0.206), (0.214, 0.250), (-0.250, 0.250)])
    for (x, y) in ((-0.210, -0.210), (0.210, -0.210), (-0.210, 0.210)): B.cyl("Mount", "bare", (x, y, 0.222), (x, y, 0.230), 0.012, (6, 6), "detail", "bolt")
    for sy in (-1, 1):
        a = B.prism("Mount", "dark", [(0, 0), (0.050, 0), (0.050, 0.008), (0.008, 0.008), (0.008, 0.050), (0, 0.050)], -0.300, 0.230, ("x", "y", "z"), name="angle")
        B.move(a, T(-0.250, sy*0.030, 0) @ Matrix.Diagonal((1, sy, 1, 1)))
        B.weld("Mount", [(-0.250, sy*0.030, 0.230), (-0.200, sy*0.030, 0.230)], 0.004)
    for i in range(4):
        B.box("Mount", "rust" if i % 2 else "metal", (-0.238, -0.150 + i*0.020, 0.150 - i*0.012), (-0.218, 0.150 - i*0.020, 0.160 - i*0.012), name="leaf")
    for sy in (-1, 1): B.pipe("Mount", "bare", [(-0.244, sy*0.060, 0.212), (-0.244, sy*0.060, 0.106), (-0.216, sy*0.060, 0.106), (-0.216, sy*0.060, 0.212)], 0.005, (8, 6), (3, 2), name="ubolt")
    pt = B.prism("Mount", "alt2", [(-0.060, 0.060), (0.070, 0.050), (0.080, 0.130), (-0.050, 0.140)], -0.256, -0.250, ("y", "z", "x"), name="repair")
    B.weld("Mount", [(-0.256, -0.060, 0.060), (-0.256, 0.070, 0.050), (-0.256, 0.080, 0.130)], 0.004)
    B.cyl("Mount", "metal", (-0.250, 0, -0.26), (-0.190, 0, -0.26), 0.046, (8, 6), name="boss")
    B.cyl("Mount", "bare", (-0.190, 0, -0.26), (-0.174, 0, -0.26), 0.028, (6, 6), name="nut")
    B.pipe("Mount", "rubber", [(-0.240, -0.150, 0.222), (-0.246, -0.110, 0.120), (-0.246, -0.070, -0.050), (-0.226, -0.040, -0.200)], 0.008, (8, 6), (5, 3), name="cable")
    for z in (0.140, -0.020): B.cyl("Mount", "alt2", (-0.246, -0.100 + 0.0*z, z - 0.012), (-0.246, -0.100, z + 0.012), 0.012, (8, 6), name="tape")
    fender(B, "metal", -0.204, 0.020, 0.420, 0.426, 30, (24, 8), 0.002)
    fp = B.rev("Mount", "alt", [(0.426, -0.120), (0.429, -0.120), (0.429, -0.030), (0.426, -0.030)], (12, 6), "x", (0, 0, -0.26), True, name="fenderpatch", arc=(78, 104))
    # added later (greyscale review: from the outer side 02 and 04 had the same silhouette): a crude bolted-on side shield over the upper
    # front quadrant of the OUTER face, hung on an exposed strap and a bent rod brace; one segment is a primer replacement plate.
    # Clearance: the rotor ends at x 0.161, the shield starts at x 0.186; it stays above the axle (z >= -0.245), inside the Mount bounds
    # the plate is clipped at y = -0.245 so the Mount stays inside its 1x1 cell bounds (+-0.2563)
    AZ = -0.26; seg = [8, 30, 52, 74, 100]
    def rout(t, k): return min(0.350 if k % 2 == 0 else 0.338, 0.245/max(math.cos(t), 1e-6) if math.cos(t) > 0 else 9.0)
    for k in range(len(seg) - 1):
        t0, t1 = math.radians(seg[k]), math.radians(seg[k + 1]); r0 = 0.140; ra, rb = rout(t0, k), rout(t1, k)
        q = [(-r0*math.cos(t0), AZ + r0*math.sin(t0)), (-ra*math.cos(t0), AZ + ra*math.sin(t0)), (-rb*math.cos(t1), AZ + rb*math.sin(t1)), (-r0*math.cos(t1), AZ + r0*math.sin(t1))]
        B.prism("Mount", "alt2" if k == 2 else ("rust" if k == 0 else "metal"), q, 0.186 + 0.002*(k % 2), 0.196 + 0.002*(k % 2), ("y", "z", "x"), name="shield")
        tm = (t0 + t1)/2
        for rr in (0.170, min(ra, rb) - 0.035): B.cyl("Mount", "bare", (0.196, -rr*math.cos(tm), AZ + rr*math.sin(tm)), (0.204, -rr*math.cos(tm), AZ + rr*math.sin(tm)), 0.009, (6, 6), "detail", "bolt")
    B.box("Mount", "rust", (0.226, -0.150, -0.012), (0.236, -0.120, 0.230), name="strap")
    B.box("Mount", "rust", (0.196, -0.150, -0.012), (0.236, -0.120, -0.002), name="strap_tab")
    for x in (0.206, 0.226): B.cyl("Mount", "bare", (x, -0.135, -0.002), (x, -0.135, 0.006), 0.007, (6, 6), "detail", "bolt")
    B.pipe("Mount", "rust", [(0.236, 0.110, 0.226), (0.236, 0.090, 0.150), (0.214, 0.050, 0.090), (0.200, 0.030, 0.050)], 0.006, (8, 6), (4, 2), name="brace")
    B.weld("Mount", [(0.236, -0.150, 0.226), (0.236, -0.120, 0.226)], 0.004)


def wheel_5(B):
    """Comic Outline wheel. Rotor: the Pass 4 Heroic Toon rotor (balloon tyre with seven fat arrow lugs, orange bowl hub, raised star) moved
    onto the axle - the hub is the part that decides the function, and it stays oversized. Mount: new - a fat rounded arm curving down to a big
    round boss with a star cap, a rounded plate. The outline is checked on the hub and in the tyre gap at game distance"""
    import h4defs as H4
    port_rotor(B, H4.wheel_E, sleeve=(-0.181, 0.072))
    top_plate(B, "dark", 0.028, 0.040)
    B.pipe("Mount", "base", [(-0.212, 0.0, 0.222), (-0.220, 0.0, 0.120), (-0.220, 0.0, -0.060), (-0.216, 0.0, -0.244)], 0.034, (24, 10), (8, 3), name="arm")
    B.rev("Mount", "metal", spl([(0.0, -0.250, 'c'), (0.036, -0.248), (0.048, -0.234), (0.050, -0.204), (0.042, -0.184, 'c'), (0.0, -0.184, 'c')], B.q(6, 2)), (40, 16), "x", (0, 0, -0.26), name="boss")
    st = B.prism("Mount", "alt", star(0.016, 0.040, 5, math.pi/2), 0.249, 0.256, ("y", "z", "x"), name="star"); B.move(st, T(0, 0, -0.26) @ Matrix.Diagonal((-1, 1, 1, 1)))
    fender(B, "base", -0.206, -0.006, 0.404, 0.436, 32, (48, 16), 0.012)


# ================================================================================================ GUN R4W_Basic, style 3 (the other guns are kept / outlined, see the README)
def gun_3(B):
    """Arcade Low-Poly gun - new from scratch (the Pass 4 Arcade gun is soft and rounded): a hexagonal stepped pedestal, two big wedge cheek
    plates on the yaw ring, a faceted receiver whose top is one orange triangle fin pointing forward, a hexagonal shroud and a hexagonal barrel
    with an orange muzzle cap. Few large flat facets; the aiming direction is the triangle"""
    BZ = 0.135
    B.prism("Base", "metal", chamf(0.250, 0.250, 0.050), -0.260, -0.234, ("x", "y", "z"), name="foot")
    B.loft("Base", "base", [[(x, y, -0.234) for (x, y) in ngon(0.236, 6, TAU/12)], [(x, y, -0.120) for (x, y) in ngon(0.220, 6, TAU/12)], [(x, y, -0.040) for (x, y) in ngon(0.176, 6, TAU/12)]], name="pedestal")
    B.loft("Base", "sec", [[(x, y, -0.040) for (x, y) in ngon(0.178, 6, TAU/12)], [(x, y, -0.010) for (x, y) in ngon(0.178, 6, TAU/12)]], name="band")
    B.loft("Base", "metal", [[(x, y, -0.010) for (x, y) in ngon(0.160, 6, TAU/12)], [(x, y, 0.016) for (x, y) in ngon(0.150, 6, TAU/12)]], name="race")
    B.loft("Yaw", "base", [[(x, y, 0.018) for (x, y) in ngon(0.172, 6, TAU/12)], [(x, y, 0.052) for (x, y) in ngon(0.160, 6, TAU/12)]], name="ring")
    for sx in (-1, 1):
        ch = B.prism("Yaw", "base", [(-0.150, 0.052), (0.120, 0.052), (0.070, 0.215), (-0.060, 0.215)], 0.150, 0.186, ("y", "z", "x"), name="cheek"); B.move(ch, Matrix.Diagonal((sx, 1, 1, 1)))
        st = B.prism("Yaw", "sec", [(-0.100, 0.110), (0.060, 0.110), (0.040, 0.150), (-0.080, 0.150)], 0.1862, 0.1905, ("y", "z", "x"), name="stripe"); B.move(st, Matrix.Diagonal((sx, 1, 1, 1)))
    B.prism("Yaw", "metal", [(-0.150, 0.052), (0.150, 0.052), (0.150, 0.110), (-0.150, 0.150)], 0.120, 0.250, ("x", "z", "y"), name="counterweight")
    for sx in (-1, 1):
        B.prism("Yaw", "dark", ngon(0.026, 6, 0.0), *sorted((sx*0.104, sx*0.168)), ("y", "z", "x"), name="trunnion", at=(0, 0, BZ))
        B.prism("Yaw", "sec", ngon(0.036, 6, 0.0), *sorted((sx*0.1855, sx*0.1910)), ("y", "z", "x"), name="trunnioncap", at=(0, 0, BZ))
    hx = lambda w, z0, z1, y: [(w, y, (z0 + z1)/2), (w*0.58, y, z1), (-w*0.58, y, z1), (-w, y, (z0 + z1)/2), (-w*0.58, y, z0), (w*0.58, y, z0)]
    B.loft("Pitch", "base", [hx(0.122, 0.092, 0.226, 0.070), hx(0.122, 0.092, 0.226, -0.020), hx(0.090, 0.098, 0.214, -0.205)], name="receiver")
    tri = B.prism("Pitch", "sec", [(-0.070, 0.060), (0.070, 0.060), (0.0, -0.190)], BZ + 0.092, BZ + 0.100, ("x", "y", "z"), name="triangle")
    B.loft("Pitch", "metal", [[(x, -0.200, BZ + y) for (x, y) in ngon(0.050, 6, 0.0)], [(x, -0.400, BZ + y) for (x, y) in ngon(0.050, 6, 0.0)]], name="shroud")
    B.loft("Barrel", "metal", [[(x, -0.100, BZ + y) for (x, y) in ngon(0.034, 6, 0.0)], [(x, -0.580, BZ + y) for (x, y) in ngon(0.034, 6, 0.0)]], name="barrel")
    B.loft("Barrel", "sec", [[(x, -0.580, BZ + y) for (x, y) in ngon(0.039, 6, 0.0)], [(x, -0.618, BZ + y) for (x, y) in ngon(0.039, 6, 0.0)]], name="muzzle")


RECIPES.update({("R4M_Wheel", s): f for s, f in zip("12345", (wheel_1, wheel_2, wheel_3, wheel_4, wheel_5))})
RECIPES[("R4W_Basic", "3")] = gun_3
