"""RoboRougelike - R4 Sleek module library 1x1x1: the module definitions (robot-mounted versions).
Every function composes one module from mkit primitives inside the 0.52 m envelope. Ports / mounts / animation clips are PROPOSALS."""
import math
import numpy as np
import mkit as K
from mkit import H, X, Y, Z, box, sbox, prism, srev, hull, lathe, cyl, rod, tube, torus, ball, cone, port, empty, node, rot, R_to, arc2

ALL = {}
CYAN = (0.20, 0.80, 1.00); GREEN = (0.25, 1.00, 0.45); AMBER = (1.00, 0.62, 0.10); RED = (1.00, 0.18, 0.12); VIOLET = (0.70, 0.35, 1.00); BLUE = (0.25, 0.45, 1.00); WHITE = (0.9, 0.95, 1.0); YELLOW = (1.0, 0.9, 0.2)


def mod(num, name, group, pl, func, glow=CYAN, wave=None):
    def deco(fn):
        ALL[name] = dict(fn=fn, id=num, name=name, group=group, pl=pl, function=func, glow=glow, wave=wave or {"A": 1, "B": 1, "C": 2, "D": 2, "E": 3, "F": 3, "T": 3}[group]); return fn
    return deco


def plinth(h=0.07, faces=None):
    """standard mount plinth: full 0.52 x 0.52 footprint, flush bottom plate = full 'down' contact"""
    box(-H, H, -H, H, -H, -H + h, faces=faces); port("down")


def cap(h=0.06):
    box(-H, H, -H, H, H - h, H); port("up")


def pad(d, a=0.11, depth=0.05, c=(0.0, 0.0)):
    """side contact pad: small sleek box whose outer plate lies on the contact plane (partial contact)"""
    u, v, n = K.frame(K.DIRS[d])
    K.prism([(-a, -a), (a, -a), (a, a), (-a, a)], H - depth, H, (np.asarray(u), np.asarray(v), np.asarray(n)), s=K.auto_s(2*a, depth*2.2), org=c[0]*np.asarray(u) + c[1]*np.asarray(v))
    port(d, "partial", round((a - 0.035)/(0.85*K.G), 3), c[0]*u + c[1]*v)


def ring_of(n, fn, phase=0.0):
    for i in range(n): fn(rot(Z, phase + 360.0*i/n), i)


# =================================================================================================== A. basic modules
@mod(1, "Battery", "A", "Bateria", "Magazyn energii robota.", AMBER)
def battery():
    plinth(0.06)
    for sx in (-0.125, 0.125):
        srev([(0, -0.20), (0.108, -0.20), (0.108, 0.17), (0, 0.17)], 20, 0.7, at=(sx, -0.02, 0))
        tube(0.100, 0.113, -0.035, 0.005, "glow", N=20, at=(sx, -0.02, 0))
        cyl(0.045, 0.17, 0.215, "metal", N=14, at=(sx, -0.02, 0))
    sbox(-0.16, 0.16, -0.055, 0.015, 0.195, 0.235, "dark")
    box(-0.10, 0.10, 0.10, H, -0.20, 0.06); port("back", "partial", 0.14, (0, 0, -0.07))


@mod(2, "EnergyRegenerator", "A", "Regenerator energii", "Stale odnawia energie.", GREEN)
def energy_regenerator():
    plinth(0.06)
    cyl(0.062, -0.20, 0.20, "glow", N=16)
    for z in (-0.12, -0.04, 0.04, 0.12): torus(0.105, 0.026, "sec", 20, 8, at=(0, 0, z))
    ring_of(4, lambda R, i: box(0.12, 0.235, -0.022, 0.022, -0.20, 0.16, R=R @ rot(Z, 45)))
    srev([(0, 0.17), (0.15, 0.17), (0.15, H), (0, H)], 20, 0.55); port("up", "partial", 0.2)


@mod(3, "DrivePower", "A", "Moc napedu", "Zwieksza moc ukladu napedowego.", AMBER)
def drive_power():
    plinth(0.07)
    box(-0.15, 0.15, -0.20, 0.20, -0.19, -0.06, faces="yY")
    srev([(0, -H), (0.185, -H), (0.185, H), (0, H)], 24, 0.9, R=R_to(X), at=(0, 0, 0.045)); port("left", "partial", 0.27, (0, 0, 0.045)); port("right", "partial", 0.27, (0, 0, 0.045))
    for x in (-0.09, 0.0, 0.09): torus(0.185, 0.014, "dark", 24, 6, R=R_to(X), at=(x, 0, 0.045))
    cyl(0.07, 0.0, 0.07, "metal", N=16, R=R_to(-Y), at=(0, -0.185, 0.045))
    cyl(0.03, 0.0, 0.035, "glow", N=12, R=R_to(-Y), at=(0, -0.225, 0.045))


@mod(4, "RepairPulse", "A", "Impuls naprawczy", "Aktywny impuls naprawiajacy pobliskie czesci.", GREEN)
def repair_pulse():
    plinth(0.07)
    a, b = 0.075, 0.215
    prism([(-a, -b), (a, -b), (a, -a), (b, -a), (b, a), (a, a), (a, b), (-a, b), (-a, a), (-b, a), (-b, -a), (-a, -a)], -0.19, 0.06, "z", s=0.5)
    tube(0.085, 0.125, 0.06, 0.10, "dark", N=20)
    ball(0.085, "glow", 16, 8, at=(0, 0, 0.085), squash=1.25)
    torus(0.16, 0.014, "glow", 24, 6, at=(0, 0, 0.075))


@mod(5, "DirectionalShield", "A", "Tarcza kierunkowa", "Tarcza energetyczna oslaniajaca jeden kierunek.", CYAN)
def directional_shield():
    plinth(0.07)
    box(-0.11, 0.11, 0.02, H, -0.19, 0.10); port("back", "partial", 0.2, (0, 0, -0.045))
    # curved projector blade facing forward (-Y): annulus sector extruded along Z
    c = (0.0, 0.26); ro, ri = 0.50, 0.44
    angs = (240, 258, 282, 300)
    outer = [(c[0] + ro*math.cos(math.radians(a)), c[1] + ro*math.sin(math.radians(a))) for a in angs]
    inner = [(c[0] + ri*math.cos(math.radians(a)), c[1] + ri*math.sin(math.radians(a))) for a in angs[::-1]]
    prism(outer + inner, -0.19, 0.25, "z", s=0.42)
    for z in (-0.10, 0.03, 0.16):
        sbox(-0.095, 0.095, -0.248, -0.225, z - 0.014, z + 0.014, "glow")
    sbox(-0.035, 0.035, -0.19, 0.03, -0.06, 0.04, "dark")


@mod(6, "PointDefense", "A", "Obrona punktowa", "Automatycznie zestrzeliwuje nadlatujace pociski.", RED)
def point_defense():
    plinth(0.07)
    srev([(0, -0.19), (0.15, -0.19), (0.11, -0.07), (0, -0.07)], 20, 0.5)
    node("Turret", (0, 0, 0.04), dict(type="swing", axis=(0, 0, 1), amount=35, frames=48))
    ball(0.135, "base", 20, 10, at=(0, 0, 0.04))
    torus(0.136, 0.012, "dark", 20, 6, R=R_to(X), at=(0, 0, 0.04))
    for sx in (-0.062, 0.062):
        cyl(0.026, 0.0, 0.16, "metal", N=10, R=R_to(-Y), at=(sx, -0.05, 0.05))
        cyl(0.034, 0.0, 0.05, "dark", N=10, R=R_to(-Y), at=(sx, -0.09, 0.05))
    cyl(0.03, 0.0, 0.02, "glow", N=12, R=R_to(-Y), at=(0, -0.118, 0.11))
    node()


@mod(7, "StructuralScanner", "A", "Skaner strukturalny", "Pokazuje strukture i stan czesci robota.", CYAN)
def structural_scanner():
    plinth(0.06)
    for sx in (-1, 1):
        box(sx*0.21 - 0.05, sx*0.21 + 0.05, -0.09, 0.09, -0.20, 0.25)
        port("left" if sx < 0 else "right", "partial", 0.12, (0, 0, 0.02))
    box(-0.16, 0.16, -0.07, 0.07, 0.16, 0.25, faces="Zy")
    sbox(-0.155, 0.155, -0.012, 0.012, 0.135, 0.162, "glow")
    # scanned sample: little wire cube on the deck
    hull([(x, y, z) for x in (-0.07, 0.07) for y in (-0.07, 0.07) for z in (-0.20, -0.08)], 0.3, grp={"plate": "dark"})
    sbox(-0.16, 0.16, -0.004, 0.004, -0.20, 0.14, "glow", b=0.001)


@mod(8, "ScannerPulse", "A", "Impuls skanujacy", "Aktywny impuls wykrywajacy obiekty w poblizu.", BLUE)
def scanner_pulse():
    plinth(0.06)
    lathe([(0, -0.20), (0.10, -0.20), (0.245, -0.03), (0.245, 0.0), (0.225, 0.0), (0.09, -0.15), (0, -0.15)], ["sec", "base", "sec", "sec", "dark", "dark"], 28)
    for r, z in ((0.20, -0.035), (0.14, -0.10)): torus(r, 0.011, "glow", 28, 6, at=(0, 0, z))
    cone(0.035, 0.012, -0.16, 0.20, "metal", 12)
    ball(0.04, "glow", 14, 8, at=(0, 0, 0.21))
    torus(0.075, 0.009, "sec", 20, 6, at=(0, 0, 0.11))
    ring_of(3, lambda R, i: rod(R @ np.array([0.012, 0, 0.11]), R @ np.array([0.075, 0, 0.11]), 0.006, "sec", 0.001, 6))


# =================================================================================================== B. movement / control
@mod(9, "Booster", "B", "Dopalacz", "Krotki, silny ciag do przodu.", AMBER)
def booster():
    box(-H, H, -H, 0.02, -H, H); port("forward")
    for d in ("down", "up", "left", "right"): port(d, "partial", 0.2, (0, -0.12, 0))
    lathe([(0, 0.0), (0.15, 0.0), (0.12, 0.07), (0.235, 0.235), (0.215, 0.24), (0.10, 0.09), (0, 0.09)], ["dark", "sec", "base", "sec", "glow", "glow"], 24, R=R_to(Y), at=(0, 0.02, 0))
    for z in (0.10, 0.17): torus(0.135 + (z - 0.10)*0.66 + 0.02, 0.010, "dark", 24, 6, R=R_to(Y), at=(0, 0.02 + z, 0))
    empty("FX_Thrust", (0, 0.26, 0), (0, 1, 0))


@mod(10, "JumpDrive", "B", "Naped skoku", "Wyrzuca robota w gore.", CYAN)
def jump_drive():
    srev([(0, 0.17), (0.20, 0.17), (0.20, H), (0, H)], 24, 0.6); port("up", "partial", 0.25)
    cyl(0.105, -0.02, 0.18, "sec", 0.01, 16)
    for z in (0.03, 0.09): torus(0.108, 0.014, "dark", 16, 6, at=(0, 0, z))
    lathe([(0, -0.07), (0.09, -0.07), (0.215, -0.25), (0.235, -0.245), (0.11, -0.02), (0, -0.02)], ["glow", "glow", "sec", "base", "dark"], 24)
    def strut(R, i): rod(R @ np.array([0.17, 0, 0.18]), R @ np.array([0.175, 0, -0.14]), 0.016, "metal", 0.003, 6)
    ring_of(3, strut, 30)
    empty("FX_Thrust", (0, 0, -0.26), (0, 0, -1))


@mod(11, "Blink", "B", "Blink", "Krotka teleportacja robota.", VIOLET)
def blink():
    plinth(0.07)
    box(-0.09, 0.09, -0.09, 0.09, -0.19, -0.09)
    for sx in (-1, 1):
        pr = [(sx*0.10, -0.19), (sx*0.19, -0.19), (sx*0.255, -0.02), (sx*0.215, 0.25), (sx*0.15, 0.25), (sx*0.185, -0.02)]
        prism(pr if sx > 0 else pr[::-1], -0.055, 0.055, "y", s=0.38)
        sbox(sx*0.165 - 0.012, sx*0.165 + 0.012, -0.03, 0.03, 0.02, 0.22, "glow", R=rot(Y, -sx*8), at=(0, 0, 0))
    ball(0.07, "glow", 16, 8, at=(0, 0, 0.09))
    torus(0.10, 0.008, "metal", 20, 6, R=R_to(Y), at=(0, 0, 0.09))


@mod(12, "Gyroscope", "B", "Zyroskop", "Stabilizuje i prostuje robota.", CYAN)
def gyroscope():
    plinth(0.06)
    for sx in (-1, 1): box(sx*0.215 - 0.04, sx*0.215 + 0.04, -0.06, 0.06, -0.20, 0.075)
    for sx in (-1, 1): cyl(0.022, 0.0, 0.03, "metal", N=10, R=R_to((-sx, 0, 0)), at=(sx*0.178, 0, 0.03))
    node("RingOuter", (0, 0, 0.03), dict(type="swing", axis=(1, 0, 0), amount=25, frames=48))
    torus(0.165, 0.018, "base", 28, 8, R=R_to(Y), at=(0, 0, 0.03))
    node("RingInner", (0, 0, 0.03), dict(type="swing", axis=(0, 0, 1), amount=35, frames=48))
    torus(0.128, 0.014, "sec", 24, 8, R=R_to(X), at=(0, 0, 0.03))
    node("Rotor", (0, 0, 0.03), dict(type="spin", axis=(0, 0, 1), frames=24))
    lathe([(0, -0.03), (0.07, -0.03), (0.10, -0.012), (0.10, 0.012), (0.07, 0.03), (0, 0.03)], ["dark", "metal", "glow", "metal", "dark"], 20, at=(0, 0, 0.03))
    cyl(0.014, -0.115, 0.115, "metal", N=8, at=(0, 0, 0.03))
    node()


@mod(13, "MagneticGrapple", "B", "Chwytak magnetyczny", "Przyciaga robota do celu / cel do robota.", RED)
def magnetic_grapple():
    plinth(0.06)
    box(-0.15, 0.15, 0.05, H, -0.20, 0.12); port("back", "partial", 0.25, (0, 0, -0.04))
    cyl(0.105, -0.115, 0.115, "sec", N=20, R=R_to(X), at=(0, 0.02, 0.0))
    for sx in (-1, 1): cyl(0.13, 0.0, 0.02, "dark", N=20, R=R_to(X), at=(sx*0.125 - 0.01, 0.02, 0.0))
    # horseshoe magnet head, open towards -Y
    pr = [(-0.17, -0.255), (-0.085, -0.255), (-0.085, -0.13), (0.085, -0.13), (0.085, -0.255), (0.17, -0.255), (0.17, -0.05), (-0.17, -0.05)]
    prism(pr, 0.03, 0.15, "z", s=0.42, grp={"plate": "base"})
    for sx in (-1, 1): sbox(sx*0.1275 - 0.046, sx*0.1275 + 0.046, -0.258, -0.215, 0.026, 0.154, "glow")
    rod((0, -0.05, 0.09), (0, 0.02, 0.09), 0.02, "metal")
    empty("FX_Grapple", (0, -0.26, 0.09), (0, -1, 0))


@mod(14, "Suspension", "B", "Zawieszenie", "Amortyzuje ruch czesci napedowych.", AMBER)
def suspension():
    plinth(0.055); cap(0.055)
    cyl(0.07, -0.21, -0.02, "dark", N=16)
    node("Piston", (0, 0, 0), dict(type="slide", axis=(0, 0, 1), amount=-0.03, frames=24))
    cyl(0.042, -0.06, 0.21, "metal", N=14)
    node()
    for i in range(5): torus(0.135, 0.021, "base", 24, 8, at=(0, 0, -0.165 + i*0.0825))
    for i in range(4): torus(0.135, 0.012, "sec", 24, 6, at=(0, 0, -0.124 + i*0.0825))
    sbox(-0.02, 0.02, -0.078, -0.064, -0.19, -0.05, "glow")


@mod(15, "IndependentSteering", "B", "Niezalezne sterowanie", "Pozwala skrecac kolami niezaleznie.", GREEN)
def independent_steering():
    cap(0.05)
    srev([(0, 0.02), (0.125, 0.02), (0.125, 0.21), (0, 0.21)], 20, 0.5)
    tube(0.12, 0.215, -0.035, 0.02, "dark", N=24)
    for a in range(0, 360, 30): sbox(0.205, 0.228, -0.012, 0.012, -0.03, 0.015, "metal", R=rot(Z, a))
    node("Turntable", (0, 0, 0), dict(type="swing", axis=(0, 0, 1), amount=35, frames=48))
    srev([(0, -H), (0.19, -H), (0.19, -0.035), (0, -0.035)], 24, 0.75); port("down", "partial", 0.24)
    sbox(-0.03, 0.03, -0.205, -0.16, -0.20, -0.09, "glow")
    box(0.10, 0.25, -0.05, 0.05, -0.17, -0.09, s=0.3)
    node()
    box(0.11, H, -0.08, 0.08, 0.04, 0.21); port("right", "partial", 0.1, (0, 0, 0.125))
    rod((0.20, 0, 0.04), (0.20, 0, -0.09), 0.018, "metal", 0.003, 8)


@mod(16, "HoverEngine", "B", "Silnik poduszkowy", "Unosi robota nad ziemia.", CYAN)
def hover_engine():
    srev([(0, 0.15), (0.13, 0.15), (0.13, H), (0, H)], 20, 0.5); port("up", "partial", 0.18)
    lathe([(0.19, -0.245), (0.215, -0.26), (0.245, -0.25), (0.258, -0.20), (0.258, -0.09), (0.235, -0.05), (0.21, -0.09), (0.215, -0.20)], ["glow", "sec", "sec", "base", "sec", "dark", "dark", "glow"], 28, closed=True)
    def strut(R, i): rod(R @ np.array([0.235, 0, -0.07]), R @ np.array([0.08, 0, 0.17]), 0.02, "metal", 0.004, 8)
    ring_of(4, strut, 45)
    cyl(0.07, -0.18, 0.16, "sec", N=16)
    node("Fan", (0, 0, -0.14), dict(type="spin", axis=(0, 0, 1), frames=16))
    ring_of(5, lambda R, i: sbox(0.06, 0.212, -0.035, 0.035, -0.008, 0.008, "metal", R=R @ rot(X, 22), at=(0, 0, -0.14)))
    cone(0.07, 0.03, -0.235, -0.18, "dark", 16)
    node()
    empty("FX_Thrust", (0, 0, -0.26), (0, 0, -1))


# =================================================================================================== C. bonus boosters
from mkit import slab, ngon


@mod(17, "HullBooster", "C", "Wzmacniacz kadluba", "Pasywnie zwieksza wytrzymalosc kadluba.", BLUE)
def hull_booster():
    box(-0.17, 0.17, -0.17, 0.17, -H, H); port("down", "partial", 0.24); port("up", "partial", 0.24)
    for sx in (-1, 1):
        prism([(sx*0.26, -0.20), (sx*0.26, 0.20), (sx*0.15, 0.26), (sx*0.15, -0.26)][::sx], -0.22, 0.22, "z", s=0.5)
        port("left" if sx < 0 else "right", "partial", 0.3)
    # chevron armour plates on the front and the back
    for sy in (-1, 1):
        ch = [(-0.14, -0.20), (0.0, -0.11), (0.14, -0.20), (0.14, 0.11), (0.0, 0.20), (-0.14, 0.11)]
        prism(ch, 0.165, 0.245, (X, Z, -Y) if sy < 0 else (-X, Z, Y), s=0.4)
    sbox(-0.012, 0.012, -0.252, -0.24, -0.09, 0.15, "glow")


@mod(18, "DamageBooster", "C", "Wzmacniacz obrazen", "Pasywnie zwieksza obrazenia broni.", RED)
def damage_booster():
    plinth(0.07)
    prism(ngon(8, 0.19, 22.5), -0.19, -0.10, "z", s=0.45)
    hull([(-0.11, -0.11, -0.10), (0.11, -0.11, -0.10), (0.11, 0.11, -0.10), (-0.11, 0.11, -0.10), (-0.012, -0.012, 0.255), (0.012, -0.012, 0.255), (0.012, 0.012, 0.255), (-0.012, 0.012, 0.255)], 0.3)
    def spike(R, i):
        b = R @ np.array([0.165, 0.0, -0.10]); cone(0.05, 0.004, 0.0, 0.20, "metal", 10, R=rot(R @ Y, 16), at=b)
    ring_of(4, spike, 45)
    tube(0.115, 0.135, -0.06, -0.03, "glow", N=20)
    tube(0.075, 0.09, 0.05, 0.07, "glow", N=16)


@mod(19, "ReloadBooster", "C", "Wzmacniacz przeladowania", "Pasywnie skraca czas przeladowania.", AMBER)
def reload_booster():
    plinth(0.06)
    for sy in (-1, 1): box(-0.07, 0.07, sy*0.20 - 0.045, sy*0.20 + 0.045, -0.20, 0.06)
    cyl(0.03, -0.25, 0.25, "metal", N=10, R=R_to(Y), at=(0, 0, 0.03))
    node("Drum", (0, 0, 0.03), dict(type="spin", axis=(0, 1, 0), frames=48))
    cyl(0.20, -0.145, 0.145, "base", 0.012, 24, R=R_to(Y), at=(0, 0, 0.03))
    def chamber(R, i):
        c = R @ np.array([0.125, 0, 0])
        for sy in (-1, 1): cyl(0.045, 0.0, 0.012, "dark", 0.002, 12, R=R_to((0, sy, 0)), at=(c[0], sy*0.14, 0.03 + c[2]))
        cyl(0.03, 0.0, 0.016, "glow" if i % 2 == 0 else "metal", 0.002, 10, R=R_to(-Y), at=(c[0], -0.146, 0.03 + c[2]))
    for i in range(6):
        a = math.radians(60*i + 30); chamber(np.array([[math.cos(a), 0, 0], [0, 1, 0], [math.sin(a), 0, 0]]), i)
    torus(0.20, 0.012, "sec", 24, 6, R=R_to(Y), at=(0, 0, 0.03))
    node()


@mod(20, "MobilityBooster", "C", "Wzmacniacz mobilnosci", "Pasywnie zwieksza predkosc i zwrotnosc.", GREEN)
def mobility_booster():
    plinth(0.06)
    # arrow-shaped pod pointing forward with two swept fins
    prism([(0.0, -0.255), (0.11, -0.05), (0.11, 0.22), (-0.11, 0.22), (-0.11, -0.05)], -0.20, 0.0, "z", s=0.5)
    for sx in (-1, 1):
        fin = [(-0.12, -0.20), (0.25, -0.20), (0.25, 0.24), (0.17, 0.24)]
        prism(fin, sx*0.19 - 0.03, sx*0.19 + 0.03, (Y, Z, X), s=0.3)
        sbox(sx*0.19 - 0.034, sx*0.19 + 0.034, 0.238, 0.256, -0.15, 0.19, "glow", b=0.003)
    sbox(-0.045, 0.045, -0.02, 0.20, 0.0, 0.035, "dark")
    for y in (0.02, 0.085, 0.15): sbox(-0.05, 0.05, y, y + 0.03, 0.03, 0.045, "glow", b=0.002)


@mod(21, "RepairBooster", "C", "Wzmacniacz naprawy", "Pasywnie wzmacnia efekty naprawy.", GREEN)
def repair_booster():
    plinth(0.07)
    box(-0.06, 0.06, 0.03, 0.17, -0.19, 0.0)
    cyl(0.17, -0.05, 0.05, "base", 0.012, 24, R=R_to(Y), at=(0, 0.05, 0.035))
    def tooth(R, i): sbox(0.155, 0.218, -0.05, 0.05, -0.036, 0.036, "sec", R=rot(Y, 36*i), at=(0, 0.05, 0.035))
    ring_of(10, tooth)
    cyl(0.11, 0.0, 0.016, "dark", 0.003, 20, R=R_to(-Y), at=(0, 0.0, 0.035))
    for a in (0, 90): sbox(-0.085, 0.085, -0.03, -0.012, -0.026, 0.026, "glow", R=rot(Y, a), at=(0, 0, 0.035))
    cyl(0.04, 0.0, 0.07, "metal", N=12, R=R_to(Y), at=(0, 0.10, 0.035))


@mod(22, "CoreBooster", "C", "Wzmacniacz rdzenia", "Pasywnie wzmacnia rdzen robota.", VIOLET)
def core_booster():
    plinth(0.06); cap(0.06)
    def pil(R, i): box(0.17, 0.25, 0.17, 0.25, -0.20, 0.20, R=R)
    ring_of(4, pil)
    lathe([(0, -0.17), (0.105, 0.0), (0, 0.17)], "glow", 8, crease=10)
    torus(0.135, 0.016, "metal", 24, 8)
    for z in (-0.185, 0.185): cone(0.06, 0.02, 0.0, 0.04, "dark", 12, R=R_to((0, 0, -np.sign(z))), at=(0, 0, z))


# =================================================================================================== D. combat / energy / defence
@mod(23, "Overdrive", "D", "Overdrive", "Aktywne, chwilowe przeciazenie systemow robota.", RED)
def overdrive():
    plinth(0.06)
    prism([(-0.20, -0.20), (0.20, -0.20), (0.11, 0.04), (-0.11, 0.04)], -0.21, 0.21, (X, Z, -Y), s=0.5)
    for sx in (-1, 1):
        for y in (-0.13, 0.0, 0.13):
            d = np.array([sx*0.34, 0, 0.94]); p0 = np.array([sx*0.115, y, -0.05])
            rod(p0, p0 + d*0.27, 0.036, "metal", 0.004, 10); rod(p0 + d*0.235, p0 + d*0.273, 0.027, "glow", 0.002, 10)
    sbox(-0.05, 0.05, -0.19, 0.19, 0.04, 0.075, "dark")
    for y in (-0.13, 0.0, 0.13): sbox(-0.035, 0.035, y - 0.035, y + 0.035, 0.07, 0.085, "glow", b=0.003)


@mod(24, "RapidFire", "D", "Szybki ostrzal", "Aktywnie zwieksza szybkostrzelnosc broni.", AMBER)
def rapid_fire():
    plinth(0.06)
    box(-0.15, 0.15, 0.04, H, -0.20, 0.17); port("back", "partial", 0.25, (0, 0, -0.015))
    node("Barrels", (0, 0, 0.0), dict(type="spin", axis=(0, 1, 0), frames=12))
    for i in range(6):
        a = math.radians(60*i); rod((0.085*math.cos(a), -0.25, 0.085*math.sin(a)), (0.085*math.cos(a), 0.04, 0.085*math.sin(a)), 0.03, "metal", 0.004, 8)
    for y in (-0.21, -0.07): cyl(0.128, 0.0, 0.035, "sec", 0.005, 20, R=R_to(-Y), at=(0, y + 0.035, 0.0))
    cyl(0.035, 0.0, 0.26, "dark", N=10, R=R_to(-Y), at=(0, 0.04, 0.0))
    node()
    sbox(-0.10, 0.10, 0.09, 0.21, 0.17, 0.185, "glow", b=0.003)


@mod(25, "Stealth", "D", "Kamuflaz", "Aktywnie ukrywa robota przed wykryciem.", VIOLET)
def stealth():
    plinth(0.05, faces="z")
    pts = [(x, y, -0.21) for x in (-0.21, 0.21) for y in (-0.21, 0.21)] + [(x, y, -0.07) for x in (-0.256, 0.256) for y in (-0.256, 0.256)] + [(x, y, 0.20) for x in (-0.06, 0.06) for y in (-0.14, 0.06)]
    hull(pts, 0.4, plates=False, grp={"frame": "dark", "bevel": "sec", "corner": "sec"}, seg=2, cseg=2, g=0.0)
    sbox(-0.04, 0.04, -0.10, 0.02, 0.196, 0.206, "glow", b=0.002)
    for a in (0, 90, 180, 270): sbox(0.25, 0.258, -0.14, 0.14, -0.082, -0.058, "glow", b=0.001, R=rot(Z, a))


@mod(26, "DirectionalEMP", "D", "EMP kierunkowy", "Impuls EMP wystrzeliwany w jednym kierunku.", BLUE)
def directional_emp():
    plinth(0.06)
    box(-0.13, 0.13, 0.06, H, -0.20, 0.13); port("back", "partial", 0.22, (0, 0, -0.035))
    c = np.array([0, 0.0, -0.02])
    cyl(0.085, 0.0, 0.09, "dark", N=16, R=R_to(-Y), at=(0, 0.07, -0.02))
    for i in range(3):
        a = math.radians(90 + 120*i); o = np.array([0.15*math.cos(a), 0.0, 0.15*math.sin(a)]); tip = np.array([0.07*math.cos(a), -0.225, 0.07*math.sin(a)])
        rod(c + o*0.6 + np.array([0, 0.02, 0]), c + o + np.array([0, -0.06, 0]), 0.03, "base", 0.004, 8); rod(c + o + np.array([0, -0.06, 0]), c + tip, 0.024, "base", 0.004, 8)
        ball(0.03, "base", 8, 6, at=c + o + np.array([0, -0.06, 0])); ball(0.026, "metal", 8, 6, at=c + tip)
    ball(0.055, "glow", 14, 8, at=(0, -0.16, -0.02))
    for y in (-0.03, -0.075): torus(0.05 - y*0.2, 0.01, "glow", 16, 6, R=R_to(Y), at=(0, y, -0.02))
    empty("FX_Emit", (0, -0.26, -0.02), (0, -1, 0))


@mod(27, "AreaEMP", "D", "EMP obszarowy", "Impuls EMP razacy wszystko dookola.", BLUE)
def area_emp():
    plinth(0.06)
    srev([(0, -0.20), (0.14, -0.20), (0.10, -0.13), (0, -0.13)], 16, 0.4)
    cone(0.07, 0.045, -0.13, 0.12, "dark", 14)
    for i in range(9): torus(0.071 - i*0.003, 0.011, "metal", 14, 6, at=(0, 0, -0.115 + i*0.026))
    torus(0.15, 0.062, "base", 28, 10, at=(0, 0, 0.165))
    lathe([(0, 0.13), (0.10, 0.13), (0.10, 0.20), (0, 0.20)], "sec", 14)
    ball(0.05, "glow", 14, 8, at=(0, 0, 0.21))
    torus(0.212, 0.008, "glow", 28, 6, at=(0, 0, 0.165))


@mod(28, "PersonalShield", "D", "Tarcza osobista", "Tarcza energetyczna wokol calego robota.", CYAN)
def personal_shield():
    plinth(0.07)
    pr = [(0, -0.19), (0.235, -0.19), (0.235, -0.10)] + arc2((0, -0.10), 0.235, 0, 90, 8)[1:-1] + [(0, 0.135)]
    lathe(pr, ["sec", "sec"] + ["base"]*8, 28, crease=50)
    for th in (30, 58): torus(0.237*math.cos(math.radians(th)), 0.008, "sec", 28, 6, at=(0, 0, -0.10 + 0.237*math.sin(math.radians(th))))
    tube(0.228, 0.246, -0.115, -0.085, "glow", N=28)
    cyl(0.06, 0.12, 0.165, "dark", N=14)
    ball(0.045, "glow", 14, 8, at=(0, 0, 0.17))


@mod(29, "DeployableShieldGenerator", "D", "Rozstawiany generator tarczy", "Wyrzutnik generatora tarczy stawianego na ziemi (wersja montowana na robocie).", CYAN)
def deployable_shield_generator():
    plinth(0.06)
    # open silo: four walls, the generator puck sits in the mouth
    for sx in (-1, 1): box(sx*0.215 - 0.045, sx*0.215 + 0.045, -H, H, -0.20, 0.12); port("left" if sx < 0 else "right", "partial", 0.2, (0, 0, -0.04))
    for sy in (-1, 1): box(-0.17, 0.17, sy*0.215 - 0.045, sy*0.215 + 0.045, -0.20, 0.06)
    node("Payload", (0, 0, 0.0), dict(type="slide", axis=(0, 0, 1), amount=-0.04, frames=24))
    cyl(0.155, -0.10, 0.10, "base", 0.012, 24)
    tube(0.12, 0.158, 0.035, 0.065, "glow", N=24)
    lathe([(0, 0.10), (0.10, 0.10)] + arc2((0, 0.10), 0.10, 0, 90, 5)[1:], "dark", 20, crease=60)
    ball(0.035, "glow", 12, 6, at=(0, 0, 0.20))
    node()
    empty("Deploy_Out", (0, 0, 0.26), (0, 0, 1))


@mod(30, "DoubleShotPassive", "D", "Podwojny strzal (pasywny)", "Pasywna szansa na podwojny strzal.", AMBER)
def double_shot_passive():
    plinth(0.06)
    box(-0.22, 0.22, -0.02, H, -0.20, 0.14); port("back", "partial", 0.3, (0, 0, -0.03))
    for sx in (-1, 1):
        cyl(0.085, 0.0, 0.235, "base", 0.01, 18, R=R_to(-Y), at=(sx*0.115, -0.02, -0.03))
        tube(0.05, 0.092, 0.0, 0.035, "sec", 0.006, 18, R=R_to(-Y), at=(sx*0.115, -0.22, -0.03))
        cyl(0.05, 0.0, 0.01, "dark", 0.002, 14, R=R_to(-Y), at=(sx*0.115, -0.225, -0.03))
        sbox(sx*0.115 - 0.02, sx*0.115 + 0.02, 0.02, 0.20, 0.14, 0.155, "glow", b=0.003)


@mod(31, "DoubleShotOverdrive", "D", "Podwojny strzal (overdrive)", "Aktywny tryb: kazdy strzal jest podwojny.", RED)
def double_shot_overdrive():
    plinth(0.06)
    box(-0.11, 0.11, 0.0, H, -0.20, H); port("back", "partial", 0.2); port("up", "partial", 0.18, (0, 0.13, 0))
    for z in (-0.09, 0.11):
        cyl(0.075, 0.0, 0.25, "base", 0.01, 18, R=R_to(-Y), at=(0, 0.0, z))
        tube(0.045, 0.085, 0.0, 0.04, "dark", 0.006, 18, R=R_to(-Y), at=(0, -0.215, z))
        cyl(0.045, 0.0, 0.01, "glow", 0.002, 14, R=R_to(-Y), at=(0, -0.222, z))
    for sx in (-1, 1):
        cyl(0.06, -0.19, 0.16, "sec", 0.01, 14, at=(sx*0.185, 0.14, 0))
        for z in (-0.10, 0.0, 0.10): torus(0.06, 0.012, "glow" if z == 0.0 else "dark", 14, 6, at=(sx*0.185, 0.14, z))
        for k in range(4): sbox(sx*0.13 - 0.02, sx*0.13 + 0.02, -0.17 + k*0.045, -0.15 + k*0.045, -0.15, 0.17, "metal", b=0.003)


@mod(32, "RechargePulse", "D", "Impuls doladowania", "Aktywny impuls doladowujacy pobliskie moduly.", YELLOW)
def recharge_pulse():
    plinth(0.07)
    box(-0.10, 0.10, -0.07, 0.07, -0.19, -0.12)
    torus(0.17, 0.034, "base", 28, 10, R=R_to(Y), at=(0, 0, 0.045))
    for a in (45, 135, 225, 315): sbox(0.15, 0.215, -0.045, 0.045, -0.03, 0.03, "sec", R=rot(Y, a), at=(0, 0, 0.045))
    bolt = [(0.03, 0.125), (-0.075, -0.015), (-0.01, -0.015), (-0.045, -0.125), (0.075, 0.02), (0.01, 0.02), (0.065, 0.125)]
    slab(bolt[::-1], -0.016, 0.016, (X, Z, -Y), "glow", org=(0, 0, 0.045))


@mod(33, "ChargeRegenerator", "D", "Regenerator ladunkow", "Pasywnie odnawia ladunki zdolnosci.", BLUE)
def charge_regenerator():
    plinth(0.06)
    def capc(R, i):
        c = R @ np.array([0.135, 0, 0])
        cyl(0.078, -0.20, 0.10, "base", 0.012, 16, at=c); tube(0.07, 0.083, -0.10, -0.07, "glow", N=16, at=c); cyl(0.03, 0.10, 0.145, "metal", N=10, at=c)
    ring_of(3, capc, 90)
    torus(0.135, 0.022, "sec", 28, 8, at=(0, 0, 0.16))
    cyl(0.045, -0.20, 0.215, "dark", N=12)
    ball(0.04, "glow", 12, 6, at=(0, 0, 0.215))
    ring_of(3, lambda R, i: rod(R @ np.array([0.03, 0, 0.19]), R @ np.array([0.125, 0, 0.165]), 0.012, "metal", 0.002, 6), 30)


@mod(34, "ChargeCapacity", "D", "Pojemnosc ladunkow", "Pasywnie zwieksza maksymalna liczbe ladunkow.", BLUE)
def charge_capacity():
    plinth(0.05)
    prism(ngon(6, 0.19, 0), -0.21, 0.13, "z", s=0.6)
    lathe([(0, 0.13), (0.15, 0.13), (0.15, 0.16), (0.06, 0.225), (0.06, 0.255), (0, 0.255)], ["sec", "sec", "base", "metal", "metal"], 6)
    for k in range(4):
        z = -0.16 + k*0.068
        for a in (-90, 30, 150): sbox(0.162, 0.171, -0.06, 0.06, z, z + 0.045, "glow" if k < 3 else "dark", b=0.002, R=rot(Z, a))


# =================================================================================================== E. information / economy / passive
@mod(35, "SalvageOptimizer", "E", "Optymalizator zlomu", "Pasywnie zwieksza zysk ze zbieranego zlomu.", AMBER)
def salvage_optimizer():
    plinth(0.06)
    # square hopper (4-sided lathe) with scrap cubes and a sorting chute
    q = 1/math.cos(math.pi/4)
    lathe([(0, -0.20), (0.09*q, -0.20), (0.245*q, 0.14), (0.245*q, 0.19), (0.215*q, 0.19), (0.08*q, -0.10), (0, -0.10)], ["sec", "base", "sec", "sec", "dark", "dark"], 4, phase=math.pi/4)
    for (x, y, z, a) in ((-0.06, 0.03, -0.04, 20), (0.07, -0.05, 0.0, 50), (0.0, 0.08, 0.05, 75)): sbox(-0.055, 0.055, -0.055, 0.055, -0.055, 0.055, "metal", b=0.008, R=rot((1, 1, 0.3), a), at=(x, y, z))
    for a in (0, 90, 180, 270): sbox(0.20, 0.228, -0.08, 0.08, 0.145, 0.185, "glow", b=0.003, R=rot(Z, a))


@mod(36, "CargoUplink", "E", "Lacze ladunkowe", "Przesyla zebrany ladunek do bazy.", GREEN)
def cargo_uplink():
    plinth(0.06)
    box(-0.20, 0.20, -0.20, 0.06, -0.20, 0.02)
    for x in (-0.11, 0.11): sbox(x - 0.022, x + 0.022, -0.208, 0.068, -0.205, 0.028, "dark", b=0.004)
    cyl(0.05, -0.20, -0.08, "sec", N=12, at=(0, 0.16, 0))
    rod((0, 0.16, -0.08), (0, 0.16, 0.20), 0.016, "metal", 0.002, 8)
    arrow = [(0.0, 0.07), (0.085, -0.02), (0.04, -0.02), (0.04, -0.07), (-0.04, -0.07), (-0.04, -0.02), (-0.085, -0.02)]
    for z in (0.07, 0.185): slab(arrow, -0.012, 0.012, (X, Z, -Y), "glow", org=(0, 0.16, z))
    lathe([(0, 0.02), (0.10, 0.02), (0.13, 0.06), (0.11, 0.065), (0.085, 0.035), (0, 0.035)], ["dark", "base", "sec", "dark", "dark"], 16, at=(0, -0.07, 0))


@mod(37, "RewardReroll", "E", "Przerzut nagrod", "Pozwala ponownie wylosowac nagrody.", VIOLET)
def reward_reroll():
    plinth(0.06)
    srev([(0, -0.20), (0.13, -0.20), (0.09, -0.13), (0, -0.13)], 16, 0.4)
    node("Die", (0, 0, 0.045), dict(type="spin", axis=(0, 0, 1), frames=48))
    Rd = rot((1, -1, 0), math.degrees(math.atan(math.sqrt(2)))); a = 0.098
    with K.group(R=Rd, at=(0, 0, 0.045)):
        box(-a, a, -a, a, -a, a, s=0.6)
        for ax_, pips in ((X, [(0, 0)]), (Y, [(-0.045, -0.045), (0.045, 0.045)]), (Z, [(-0.05, -0.05), (0, 0), (0.05, 0.05)]), (-X, [(-0.045, -0.045), (0.045, 0.045), (-0.045, 0.045), (0.045, -0.045)])):
            u, v, n = K.frame(ax_)
            for (pu, pv) in pips: cyl(0.02, 0.0, 0.004, "glow", 0.001, 8, R=R_to(n), at=n*(a - 0.001) + u*pu + v*pv)
    node()
    torus(0.215, 0.014, "sec", 28, 6, at=(0, 0, -0.10))
    for a_ in (0, 180): cone(0.035, 0.004, 0.0, 0.07, "glow", 8, R=R_to(rot(Z, a_) @ Y), at=rot(Z, a_) @ np.array([0.215, 0, -0.10]))


def brace(z1=0.20):
    """shared motif of the three durability boosters: two reinforcement hoops over the protected part"""
    for y in (-0.15, 0.15):
        pr = [(-0.25, -0.20), (-0.19, -0.20), (-0.19, z1 - 0.10), (-0.11, z1 - 0.045), (0.11, z1 - 0.045), (0.19, z1 - 0.10), (0.19, -0.20), (0.25, -0.20), (0.25, z1 - 0.07), (0.14, z1), (-0.14, z1), (-0.25, z1 - 0.07)]
        prism(pr, y - 0.035, y + 0.035, (X, Z, -Y), s=0.3)


@mod(38, "WeaponDurabilityBooster", "E", "Wytrzymalosc broni", "Pasywnie zwieksza wytrzymalosc broni.", RED)
def weapon_durability_booster():
    plinth(0.06); brace(0.17)
    cyl(0.085, -0.10, 0.22, "base", 0.012, 16, R=R_to(-Y), at=(0, 0, -0.06))
    cyl(0.05, 0.0, 0.14, "metal", 0.006, 14, R=R_to(-Y), at=(0, -0.11, -0.06))
    cyl(0.03, 0.0, 0.006, "dark", 0.001, 10, R=R_to(-Y), at=(0, -0.25, -0.06))
    for y in (-0.19, -0.13): torus(0.052, 0.012, "glow" if y < -0.15 else "dark", 14, 6, R=R_to(Y), at=(0, y, -0.06))
    sbox(-0.05, 0.05, 0.10, 0.22, -0.20, -0.14, "dark")


@mod(39, "MovementDurabilityBooster", "E", "Wytrzymalosc napedu", "Pasywnie zwieksza wytrzymalosc czesci napedowych.", GREEN)
def movement_durability_booster():
    plinth(0.06); brace(0.24)
    node("Wheel", (0, 0, -0.03), dict(type="spin", axis=(0, 1, 0), frames=48))
    torus(0.10, 0.055, "dark", 24, 10, R=R_to(Y), at=(0, 0, -0.03))
    cyl(0.07, -0.045, 0.045, "base", 0.008, 16, R=R_to(Y), at=(0, 0, -0.03))
    for a in range(0, 360, 60):
        p = rot(Y, a) @ np.array([0.04, 0, 0]); cyl(0.012, 0.0, 0.096, "glow", 0.002, 6, R=R_to(Y), at=(p[0], -0.048, -0.03 + p[2]))
    node()
    cyl(0.022, -0.115, 0.115, "metal", N=8, R=R_to(Y), at=(0, 0, -0.03))


@mod(40, "ModuleDurabilityBooster", "E", "Wytrzymalosc modulow", "Pasywnie zwieksza wytrzymalosc modulow.", BLUE)
def module_durability_booster():
    plinth(0.06); brace(0.20)
    box(-0.105, 0.105, -0.105, 0.105, -0.20, 0.01, s=0.6)
    for a in (0, 90, 180, 270): sbox(0.10, 0.11, -0.05, 0.05, -0.13, -0.06, "glow", b=0.002, R=rot(Z, a))
    for (x, y) in ((-0.07, -0.07), (0.07, -0.07), (0.07, 0.07), (-0.07, 0.07)): cyl(0.016, 0.01, 0.03, "metal", 0.002, 8, at=(x, y, 0))
    sbox(-0.04, 0.04, -0.04, 0.04, 0.01, 0.022, "dark", b=0.003)


@mod(41, "OrbAmplifier", "E", "Wzmacniacz orbow", "Pasywnie wzmacnia efekty zbieranych orbow.", VIOLET)
def orb_amplifier():
    plinth(0.06)
    lathe([(0, -0.20), (0.15, -0.20), (0.17, -0.16), (0.12, -0.10), (0.05, -0.085), (0, -0.085)], ["sec", "sec", "base", "dark", "dark"], 20)
    ball(0.135, "glow", 20, 10, at=(0, 0, 0.06))
    Rt = rot(X, 24)
    node("Ring", (0, 0, 0.06), dict(type="spin", axis=tuple(Rt @ Z), frames=48))
    tube(0.175, 0.228, -0.008, 0.008, "base", 0.003, 28, R=Rt, at=(0, 0, 0.06))
    for a in (0, 120, 240): ball(0.022, "metal", 8, 6, at=np.array([0, 0, 0.06]) + Rt @ (rot(Z, a) @ np.array([0.202, 0, 0.012])))
    node()
    def claw(R, i): rod(R @ np.array([0.10, 0, -0.11]), R @ np.array([0.125, 0, -0.01]), 0.016, "metal", 0.003, 6)
    ring_of(3, claw, 30)


@mod(42, "ModuleAmplifier", "E", "Wzmacniacz modulow", "Pasywnie wzmacnia dzialanie sasiednich modulow.", CYAN)
def module_amplifier():
    plinth(0.06)
    for k, h in enumerate((0.10, 0.20, 0.31, 0.44)):
        x = -0.195 + k*0.13
        box(x - 0.05, x + 0.05, -0.17, 0.17, -0.20, -0.20 + h, s=0.45)
        sbox(x - 0.035, x + 0.035, -0.15, 0.15, -0.20 + h, -0.20 + h + 0.012, "glow", b=0.003)
    for sy in (-1, 1): pad("forward" if sy < 0 else "back", 0.09, 0.07, (0.0, -0.11))


@mod(43, "DeployableAmplifier", "E", "Rozstawiany wzmacniacz", "Wyrzutnik wzmacniacza stawianego na ziemi (wersja montowana na robocie).", AMBER)
def deployable_amplifier():
    plinth(0.06)
    for sx in (-1, 1): box(sx*0.20 - 0.04, sx*0.20 + 0.04, -0.07, 0.07, -0.20, 0.03)
    cyl(0.025, -0.24, 0.24, "metal", N=8, R=R_to(X), at=(0, 0, -0.02))
    Rh = rot(X, 38)
    node("Horn", (0, 0, -0.02), dict(type="swing", axis=(1, 0, 0), amount=10, frames=48))
    with K.group(R=Rh, s=0.76, at=(0, 0, -0.02)):
        cyl(0.075, -0.05, 0.06, "sec", 0.01, 16)
        lathe([(0, 0.06), (0.06, 0.06), (0.21, 0.25), (0.19, 0.255), (0.045, 0.085), (0, 0.085)], ["dark", "base", "sec", "glow", "glow"], 24)
        for z in (0.12, 0.185): torus(0.06 + (z - 0.06)*0.79 + 0.008, 0.008, "dark", 24, 6, at=(0, 0, z))
    node()
    empty("Deploy_Out", (0, -0.18, 0.22), (0, -0.6, 0.8))


@mod(44, "Radar", "E", "Radar", "Wykrywa wrogow w duzym zasiegu.", GREEN)
def radar():
    plinth(0.06)
    srev([(0, -0.20), (0.12, -0.20), (0.08, -0.12), (0, -0.12)], 16, 0.4)
    cyl(0.03, -0.12, 0.02, "metal", N=10)
    node("Dish", (0, 0, 0.02), dict(type="spin", axis=(0, 0, 1), frames=48))
    cyl(0.05, -0.01, 0.05, "sec", 0.006, 12, at=(0, 0, 0.02))
    with K.group(R=rot(X, 62), s=0.84, at=(0, 0.02, 0.05)):
        lathe([(0, 0.0), (0.06, 0.0), (0.20, 0.085), (0.20, 0.10), (0.185, 0.10), (0.05, 0.02), (0, 0.02)], ["sec", "base", "sec", "sec", "dark", "dark"], 24)
        rod((0, 0, 0.02), (0, 0, 0.15), 0.01, "metal", 0.002, 6); ball(0.026, "glow", 10, 6, at=(0, 0, 0.155))
        for a in (0, 120, 240): rod(rot(Z, a) @ np.array([0.15, 0, 0.075]), (0, 0, 0.15), 0.006, "metal", 0.001, 5)
    node()


@mod(45, "Minimap", "E", "Minimapa / modul kartograficzny", "Odkrywa mape i pokazuje otoczenie.", CYAN)
def minimap():
    plinth(0.06)
    Rt = rot(X, 20)
    box(-0.10, 0.10, 0.04, 0.20, -0.20, -0.04)
    with K.group(R=Rt, at=(0, 0, -0.03)):
        box(-0.235, 0.235, -0.20, 0.20, -0.03, 0.03, s=0.45)
        sbox(-0.20, 0.20, -0.165, 0.165, 0.028, 0.036, "dark", b=0.002)
        for x in (-0.10, 0.0, 0.10): sbox(x - 0.003, x + 0.003, -0.16, 0.16, 0.034, 0.039, "glow", b=0.001)
        for y in (-0.08, 0.0, 0.08): sbox(-0.195, 0.195, y - 0.003, y + 0.003, 0.034, 0.039, "glow", b=0.001)
        for (x, y, h, r) in ((-0.12, 0.07, 0.05, 0.05), (-0.04, 0.10, 0.075, 0.06), (0.10, -0.08, 0.04, 0.045)): lathe([(0, 0.036), (r, 0.036), (0.004, 0.036 + h), (0, 0.036 + h)], "sec", 4, crease=5, phase=0.5, at=(x, y, 0))
        cone(0.004, 0.03, 0.036, 0.11, "glow", 8, at=(0.09, 0.08, 0)); ball(0.03, "glow", 10, 6, at=(0.09, 0.08, 0.125))


@mod(46, "WeakPointScanner", "E", "Skaner slabych punktow", "Podswietla slabe punkty wrogow.", RED)
def weak_point_scanner():
    plinth(0.06)
    box(-0.07, 0.07, -0.02, 0.12, -0.20, -0.08)
    cyl(0.085, -0.05, 0.19, "base", 0.012, 18, R=R_to(-Y), at=(0, 0.07, 0.01))
    tube(0.06, 0.10, 0.0, 0.03, "sec", 0.006, 18, R=R_to(-Y), at=(0, -0.09, 0.01))
    cyl(0.062, 0.0, 0.008, "glow", 0.002, 16, R=R_to(-Y), at=(0, -0.105, 0.01))
    cyl(0.05, 0.0, 0.05, "dark", 0.006, 12, R=R_to(Y), at=(0, 0.12, 0.01))
    # reticle held in front of the lens
    torus(0.15, 0.011, "metal", 28, 6, R=R_to(Y), at=(0, -0.235, 0.01))
    for a in (0, 90, 180, 270): sbox(0.085, 0.20, -0.008, 0.008, -0.008, 0.008, "metal", b=0.002, R=rot(Y, a), at=(0, -0.235, 0.01))
    for sx in (-1, 1): rod((sx*0.20, -0.235, -0.20), (sx*0.15, -0.235, 0.01), 0.011, "sec", 0.002, 6)
    ball(0.02, "glow", 8, 6, at=(0, -0.235, 0.01))


# =================================================================================================== world traps (deployed objects) + their miniatures
# origin on the ground (z = 0), footprint inside one cell. full(anim) builds the world model; the same function builds the payload miniature.
def t_explosive(anim=False):
    lathe([(0, 0), (0.20, 0), (0.215, 0.02), (0.16, 0.075), (0.07, 0.10), (0, 0.10)], ["dark", "sec", "base", "base", "dark"], 20)
    def horn(R, i):
        rod(R @ np.array([0.14, 0, 0.055]), R @ np.array([0.215, 0, 0.115]), 0.016, "metal", 0.003, 6); ball(0.026, "metal", 8, 6, at=R @ np.array([0.222, 0, 0.12]))
    ring_of(6, horn)
    tube(0.165, 0.19, 0.045, 0.062, "glow", 0.003, 20)
    if anim: node("Lamp", (0, 0, 0.10), dict(type="slide", axis=(0, 0, 1), amount=0.015, frames=24))
    ball(0.055, "glow", 12, 8, at=(0, 0, 0.105))
    if anim: node()


def t_emp(anim=False):
    lathe([(0, 0), (0.21, 0), (0.22, 0.02), (0.20, 0.045), (0, 0.045)], ["dark", "sec", "base", "base"], 20)
    cone(0.05, 0.035, 0.045, 0.16, "dark", 10)
    for i in range(5): torus(0.052 - i*0.003, 0.010, "metal", 12, 6, at=(0, 0, 0.06 + i*0.022))
    def prong(R, i):
        rod(R @ np.array([0.17, 0, 0.04]), R @ np.array([0.19, 0, 0.17]), 0.014, "base", 0.003, 6); rod(R @ np.array([0.19, 0, 0.17]), R @ np.array([0.075, 0, 0.215]), 0.012, "base", 0.003, 6)
        ball(0.016, "base", 6, 4, at=R @ np.array([0.19, 0, 0.17]))
    ring_of(3, prong)
    if anim: node("Core", (0, 0, 0.205), dict(type="spin", axis=(0, 0, 1), frames=24))
    ball(0.045, "glow", 12, 8, at=(0, 0, 0.205)); torus(0.065, 0.006, "glow", 14, 4, at=(0, 0, 0.205))
    if anim: node()


def t_knockback(anim=False):
    sbox(-0.22, 0.22, -0.22, 0.22, 0.0, 0.04, "sec", b=0.008)
    for (x, y) in ((-0.15, -0.15), (0.15, -0.15), (0.15, 0.15), (-0.15, 0.15)): cyl(0.03, 0.04, 0.11, "metal", 0.004, 8, at=(x, y, 0))
    cyl(0.07, 0.04, 0.10, "dark", 0.006, 12)
    if anim: node("Plate", (0, 0, 0.0), dict(type="slide", axis=(0, 0, 1), amount=0.06, frames=12))
    box(-0.24, 0.24, -0.24, 0.24, 0.10, 0.15, s=0.4)
    chev = [(0.0, 0.05), (0.10, -0.03), (0.10, -0.065), (0.0, 0.015), (-0.10, -0.065), (-0.10, -0.03)]
    for dy in (-0.07, 0.03, 0.13): slab(chev, 0.148, 0.156, "z", "glow", org=(0, dy, 0))
    if anim: node()


def t_snare(anim=False):
    tube(0.15, 0.235, 0.0, 0.03, "sec", 0.006, 24)
    cyl(0.10, 0.0, 0.035, "glow", 0.006, 16)
    for sx in (-1, 1):
        if anim: node("JawL" if sx < 0 else "JawR", (sx*0.0, 0, 0.03), dict(type="swing", axis=(0, sx, 0), amount=12, frames=24))
        with K.group(R=rot(Y, -sx*35), at=(0, 0, 0.03)):
            n = 8; R0 = 0.21
            arc = [(R0*math.cos(a), R0*math.sin(a)) for a in np.linspace(-math.pi/2, math.pi/2, n + 1)]
            pts = arc + [(0.82*x, 0.82*y) for (x, y) in arc[::-1]]
            slab([(sx*x, y) for (x, y) in pts], -0.014, 0.014, "z", "metal")
            for k in range(1, n):
                a = -math.pi/2 + math.pi*k/n; c = np.array([sx*0.905*R0*math.cos(a), 0.905*R0*math.sin(a), 0.012])
                cone(0.02, 0.003, 0.0, 0.06, "base", 6, at=c)
        if anim: node()
    for sy in (-1, 1): cyl(0.03, 0.0, 0.05, "dark", 0.004, 8, at=(0, sy*0.19, 0))


def t_slow(anim=False):
    lathe([(0, 0), (0.225, 0), (0.24, 0.02), (0.21, 0.045), (0, 0.045)], ["dark", "sec", "base", "base"], 6)
    lathe([(0, 0.045), (0.15, 0.045), (0.155, 0.08), (0.10, 0.125), (0, 0.135)], ["sec", "glow", "glow", "glow"], 16, crease=70)
    torus(0.156, 0.011, "sec", 16, 6, at=(0, 0, 0.075))
    cyl(0.04, 0.125, 0.15, "metal", 0.005, 10)
    def pod(R, i):
        rod(R @ np.array([0.11, 0, 0.05]), R @ np.array([0.235, 0, 0.065]), 0.032, "base", 0.005, 8); cyl(0.02, 0.0, 0.008, "glow", 0.002, 8, R=R_to(R @ np.array([0.99, 0, 0.12])), at=R @ np.array([0.234, 0, 0.065]))
    ring_of(3, pod, 30)


def t_burn(anim=False):
    lathe([(0, 0), (0.17, 0), (0.185, 0.02), (0.16, 0.05), (0, 0.05)], ["dark", "sec", "base", "base"], 20)
    def bottle(R, i):
        c = R @ np.array([0.115, 0, 0]); cyl(0.045, 0.05, 0.17, "base", 0.012, 10, at=c); cyl(0.018, 0.17, 0.20, "metal", 0.003, 8, at=c); torus(0.046, 0.008, "glow", 10, 4, at=c + np.array([0, 0, 0.10]))
    ring_of(3, bottle, 60)
    cyl(0.05, 0.05, 0.20, "dark", 0.006, 12)
    cone(0.035, 0.06, 0.20, 0.25, "metal", 12)
    if anim: node("Flame", (0, 0, 0.24), dict(type="slide", axis=(0, 0, 1), amount=0.012, frames=12))
    lathe([(0, 0.235), (0.035, 0.235), (0.045, 0.255), (0.015, 0.285), (0, 0.288)], "glow", 8, crease=70)
    if anim: node()


TRAPS = {"Explosive": (t_explosive, RED, "Pulapka wybuchowa", "Wybucha, gdy wrog wejdzie w zasieg."), "EMP": (t_emp, BLUE, "Pulapka EMP", "Wylacza systemy wrogow w zasiegu."),
         "Knockback": (t_knockback, AMBER, "Pulapka odrzucajaca", "Odrzuca wrogow, ktorzy na nia wjada."), "Snare": (t_snare, VIOLET, "Pulapka unieruchamiajaca", "Unieruchamia wroga na krotki czas."),
         "Slow": (t_slow, CYAN, "Pulapka spowalniajaca", "Spowalnia wrogow w polu dzialania."), "Burn": (t_burn, (1.0, 0.45, 0.08), "Pulapka podpalajaca", "Podpala wrogow w zasiegu.")}
TRAP_ENV = [[-0.26, -0.26, 0.0], [0.26, 0.26, 0.30]]


def _reg_trap(key, num):
    fn, glow, pl, func = TRAPS[key]
    def world(): fn(True)
    ALL[key + "Trap"] = dict(fn=world, id=num, name=key + "Trap", group="T", pl=pl, function=func, glow=glow, wave=3, env=TRAP_ENV,
                             notes=["world object deployed by R4X_%sTrapDeployer; origin on the ground, footprint inside one 0.55 m cell" % key])


# =================================================================================================== F. deployers (robot-mounted)
def trap_deployer(key, style_no):
    """trap layers. Every deployer carries the miniature of ITS trap as visible payload and has its own launch chassis:
    0 rear drop chute  1 drum with launch tray  2 catapult arm  3 upright disc rack  4 tank tower with shelf  5 saddle tanks with pedestal"""
    fn = TRAPS[key][0]; plinth(0.06)
    def mini(s, at, R=None):
        with K.group(R=R, s=s, at=at, lod=True): fn()
    out = ((0, 0.26, 0.0), (0, 1, 0))
    if style_no == 0:
        for sx in (-1, 1): prism([(-0.25, -0.20), (0.25, -0.20), (0.25, -0.02), (-0.25, 0.10)], sx*0.215 - 0.04, sx*0.215 + 0.04, (Y, Z, X), s=0.35)
        Rt = rot(X, 13.5)
        sbox(-0.175, 0.175, -0.24, 0.24, -0.012, 0.012, "dark", b=0.004, R=Rt, at=(0, 0.0, -0.085))
        mini(0.62, (0, -0.03, -0.068), Rt)
        box(-0.12, 0.12, 0.13, H, -0.20, 0.22); port("back", "partial", 0.2, (0, 0, 0.01))
        sbox(-0.08, 0.08, 0.125, 0.135, 0.05, 0.17, "glow", b=0.003)
    elif style_no == 1:
        srev([(0, -0.20), (0.20, -0.20), (0.20, -0.02), (0, -0.02)], 20, 0.6)
        for z in (-0.16, -0.11, -0.06): tube(0.195, 0.208, z - 0.012, z + 0.012, "glow" if z == -0.11 else "dark", 0.003, 20)
        lathe([(0, -0.02), (0.15, -0.02), (0.245, 0.03), (0.245, 0.055), (0.225, 0.055), (0.14, 0.01), (0, 0.01)], ["dark", "base", "sec", "sec", "dark", "dark"], 24)
        mini(0.66, (0, 0, 0.012)); out = ((0, 0, 0.26), (0, 0, 1))
    elif style_no == 2:
        box(-0.20, 0.20, 0.05, H, -0.20, 0.0); port("back", "partial", 0.12, (0, 0, -0.10))
        for sx in (-1, 1): box(sx*0.20 - 0.035, sx*0.20 + 0.035, -0.02, 0.12, 0.0, 0.14)
        cyl(0.022, -0.23, 0.23, "metal", N=8, R=R_to(X), at=(0, 0.05, 0.09))
        with K.group(R=rot(X, -14), at=(0, 0.05, 0.09)):
            sbox(-0.035, 0.035, -0.21, 0.02, -0.02, 0.02, "sec", b=0.005)
            lathe([(0, -0.012), (0.14, -0.012), (0.155, 0.0), (0.155, 0.02), (0.14, 0.02), (0.13, 0.008), (0, 0.008)], ["dark", "sec", "sec", "sec", "dark", "dark"], 20, at=(0, -0.155, 0.02))
            mini(0.5, (0, -0.155, 0.03))
        sbox(-0.15, 0.15, 0.045, 0.055, -0.15, -0.05, "glow", b=0.003); out = ((0, -0.26, 0.2), (0, -0.7, 0.7))
    elif style_no == 3:
        for sy in (-1, 1): box(-0.09, 0.09, sy*0.215 - 0.04, sy*0.215 + 0.04, -0.20, -0.06)
        for y in (-0.10, 0.03, 0.16):
            mini(0.58, (0, y - 0.01, -0.045), rot(X, 90))
            for sx in (-1, 1): sbox(sx*0.17 - 0.02, sx*0.17 + 0.02, y - 0.05, y + 0.02, -0.20, -0.10, "sec", b=0.005)
        sbox(-0.03, 0.03, -0.25, 0.25, -0.20, -0.17, "glow", b=0.003); out = ((0.26, 0, 0.0), (1, 0, 0))
    elif style_no == 4:
        srev([(0, -0.20), (0.125, -0.20), (0.125, 0.20), (0, 0.20)], 20, 0.6, at=(0, 0.125, 0))
        for z in (-0.10, 0.0, 0.10): tube(0.118, 0.131, z - 0.02, z + 0.02, "glow", 0.003, 20, at=(0, 0.125, 0))
        cyl(0.05, 0.20, 0.25, "metal", 0.006, 12, at=(0, 0.125, 0))
        box(-0.20, 0.20, -0.25, -0.01, -0.20, -0.12, faces="Zy")
        mini(0.48, (0, -0.13, -0.12))
        rod((0.09, 0.05, 0.12), (0.09, -0.10, -0.10), 0.014, "dark", 0.003, 6); out = ((0, -0.26, 0.0), (0, -1, 0))
    else:
        for sx in (-1, 1):
            cyl(0.075, -0.23, 0.23, "base", 0.014, 16, R=R_to(Y), at=(sx*0.172, 0, -0.115))
            for y in (-0.12, 0.12): torus(0.076, 0.010, "dark", 16, 6, R=R_to(Y), at=(sx*0.172, y, -0.115))
            cyl(0.03, 0.0, 0.02, "glow", 0.003, 10, R=R_to(-Y), at=(sx*0.172, -0.225, -0.115))
        srev([(0, -0.20), (0.09, -0.20), (0.09, 0.05), (0, 0.05)], 16, 0.45)
        lathe([(0, 0.05), (0.07, 0.05), (0.13, 0.085), (0.13, 0.10), (0.115, 0.10), (0.06, 0.07), (0, 0.07)], ["dark", "base", "sec", "sec", "dark", "dark"], 20)
        mini(0.5, (0, 0, 0.072)); out = ((0, 0, 0.26), (0, 0, 1))
    empty("Deploy_Out", *out)


for _i, (_k, _style) in enumerate((("Explosive", 0), ("EMP", 1), ("Knockback", 2), ("Snare", 3), ("Slow", 4), ("Burn", 5))):
    _f = (lambda k=_k, s=_style: trap_deployer(k, s))
    mod(47 + _i, _k + "TrapDeployer", "F", "Wyrzutnik: " + TRAPS[_k][2].lower(), "Rozstawia pulapke: " + TRAPS[_k][3], TRAPS[_k][1])(_f)
for _i, _k in enumerate(TRAPS): _reg_trap(_k, 101 + _i)


def turret_pod(emblem, arms=(0, 90, 180, 270), r=0.15, top=0.07):
    """turret deployers (the turret itself is assembled from existing weapons / drives / blocks): a drop-pod cradle; the clamp layout
    and the role head differ per deployer so that each one reads from its silhouette."""
    plinth(0.06)
    for a in arms:
        box(r + 0.015, 0.25, -0.05, 0.05, -0.20, top - 0.02, R=rot(Z, a))
        sbox(r - 0.02, r + 0.05, -0.03, 0.03, top - 0.05, top - 0.01, "metal", b=0.006, R=rot(Z, a))
    node("Pod", (0, 0, 0), dict(type="slide", axis=(0, 0, 1), amount=-0.03, frames=24))
    prism(ngon(8, r, 22.5), -0.19, top, "z", s=0.5)
    tube(r - 0.012, r + 0.008, top - 0.09, top - 0.06, "glow", 0.003, 8, R=rot(Z, 22.5))
    emblem(top)
    node()
    empty("Deploy_Out", (0, 0, 0.26), (0, 0, 1))


@mod(53, "OffensiveTurretDeployer", "F", "Wyrzutnik wiezyczki ofensywnej", "Rozstawia wiezyczke atakujaca wrogow.", RED)
def offensive_turret_deployer():
    def emblem(t):
        d = rot(X, 62) @ -Y
        sbox(-0.12, 0.12, -0.02, 0.14, t, t + 0.07, "base", b=0.014)
        for sx in (-0.065, 0.065):
            rod(np.array([sx, 0.07, t + 0.04]), np.array([sx, 0.07, t + 0.04]) + d*0.215, 0.034, "metal", 0.004, 10)
            rod(np.array([sx, 0.07, t + 0.04]) + d*0.15, np.array([sx, 0.07, t + 0.04]) + d*0.20, 0.044, "dark", 0.004, 10)
        sbox(-0.02, 0.02, -0.03, -0.018, t + 0.015, t + 0.055, "glow", b=0.002)
    turret_pod(emblem, arms=(0, 180), r=0.15, top=0.0)
    for sx in (-1, 1): sbox(sx*0.215 - 0.035, sx*0.215 + 0.035, -0.10, 0.10, -0.02, 0.06, "dark", b=0.008)


@mod(54, "DefensiveTurretDeployer", "F", "Wyrzutnik wiezyczki obronnej / tarczowej", "Rozstawia wiezyczke oslaniajaca tarcza.", CYAN)
def defensive_turret_deployer():
    def emblem(t):
        sh = [(-0.23, 0.0), (0.23, 0.0), (0.23, 0.26), (0.12, 0.42), (-0.12, 0.42), (-0.23, 0.26)]
        prism(sh, 0.185, 0.245, (X, Z, -Y), s=0.3, org=(0, 0, -0.19))
        sbox(-0.012, 0.012, -0.252, -0.24, -0.12, 0.17, "glow", b=0.002)
        sbox(-0.15, 0.15, -0.252, -0.24, 0.03, 0.054, "glow", b=0.002)
        cyl(0.09, t, t + 0.04, "dark", 0.006, 16)
    turret_pod(emblem, arms=(90,), r=0.13, top=-0.02)


@mod(55, "RepairTurretDeployer", "F", "Wyrzutnik wiezyczki naprawczej", "Rozstawia wiezyczke naprawiajaca sojusznikow.", GREEN)
def repair_turret_deployer():
    def emblem(t):
        cyl(0.07, t, t + 0.03, "dark", 0.006, 14); cyl(0.03, t + 0.03, t + 0.06, "metal", 0.004, 10)
        a, b = 0.036, 0.10
        cross = [(-a, -b), (a, -b), (a, -a), (b, -a), (b, a), (a, a), (a, b), (-a, b), (-a, a), (-b, a), (-b, -a), (-a, -a)]
        slab(cross, -0.03, 0.03, (X, Z, -Y), "glow", org=(0, 0, t + 0.155))
        for sy in (-1, 1): slab([(x*1.2, y*1.2) for (x, y) in cross], sy*0.042 - 0.012, sy*0.042 + 0.012, (X, Z, -Y), "base", org=(0, 0, t + 0.155))
    turret_pod(emblem, arms=(30, 150, 270), r=0.105, top=-0.02)


@mod(56, "SupportTurretDeployer", "F", "Wyrzutnik wiezyczki wspierajacej", "Rozstawia wiezyczke wzmacniajaca sojusznikow.", AMBER)
def support_turret_deployer():
    def emblem(t):
        cone(0.06, 0.02, t, t + 0.19, "metal", 10)
        for z, r_ in ((t + 0.05, 0.235), (t + 0.115, 0.16), (t + 0.175, 0.085)): torus(r_, 0.014, "glow" if z != t + 0.115 else "base", 28, 6, at=(0, 0, z))
        ring_of(3, lambda R, i: rod(R @ np.array([0.03, 0, t + 0.03]), R @ np.array([0.235, 0, t + 0.05]), 0.009, "metal", 0.002, 5))
        ring_of(3, lambda R, i: rod(R @ np.array([0.02, 0, t + 0.10]), R @ np.array([0.16, 0, t + 0.115]), 0.008, "metal", 0.002, 5), 60)
    turret_pod(emblem, arms=(), r=0.12, top=0.05)


@mod(57, "FlyingDroneDeployer", "F", "Wyrzutnik dronow latajacych", "Wypuszcza drony latajace (dron skladany z istniejacych czesci).", CYAN)
def flying_drone_deployer():
    plinth(0.06)
    lathe([(0, -0.20), (0.235, -0.20), (0.245, -0.17), (0.245, -0.12), (0.20, -0.12), (0.19, -0.14), (0, -0.14)], ["sec", "sec", "base", "sec", "dark", "dark"], 28)
    tube(0.15, 0.185, -0.142, -0.132, "glow", 0.002, 24)
    hmark = [(-0.07, -0.08), (-0.035, -0.08), (-0.035, -0.018), (0.035, -0.018), (0.035, -0.08), (0.07, -0.08), (0.07, 0.08), (0.035, 0.08), (0.035, 0.018), (-0.035, 0.018), (-0.035, 0.08), (-0.07, 0.08)]
    slab(hmark, -0.142, -0.132, "z", "glow")
    def mast(R, i):
        rod(R @ np.array([0.215, 0, -0.13]), R @ np.array([0.235, 0, 0.21]), 0.018, "metal", 0.003, 8)
        with K.group(R=R, at=R @ np.array([0.235, 0, 0.215])):
            slab([(-0.03, -0.045), (0.022, -0.045), (0.022, 0.045), (-0.03, 0.045), (-0.085, 0.0)], -0.012, 0.03, "z", "base")
            sbox(-0.02, 0.02, -0.03, 0.03, 0.03, 0.038, "glow", b=0.002)
    ring_of(4, mast, 45)
    node("Iris", (0, 0, 0.10), dict(type="spin", axis=(0, 0, 1), frames=48))
    torus(0.13, 0.014, "sec", 24, 6, at=(0, 0, 0.10))
    for a in (0, 120, 240): slab([(0.02, -0.02), (0.125, -0.035), (0.125, 0.035)], 0.094, 0.106, "z", "base", R=rot(Z, a))
    node()
    for a in (45, 135, 225, 315): rod(rot(Z, a) @ np.array([0.13, 0, 0.10]), rot(Z, a) @ np.array([0.228, 0, 0.12]), 0.008, "metal", 0.002, 5)
    empty("Deploy_Out", (0, 0, 0.26), (0, 0, 1))


@mod(58, "GroundDroneDeployer", "F", "Wyrzutnik dronow naziemnych", "Wypuszcza drony naziemne (dron skladany z istniejacych czesci).", AMBER)
def ground_drone_deployer():
    plinth(0.05)
    arch = [(-0.25, -0.21), (-0.18, -0.21), (-0.18, -0.06), (-0.12, 0.0), (0.12, 0.0), (0.18, -0.06), (0.18, -0.21), (0.25, -0.21), (0.25, -0.02), (0.16, 0.07), (-0.16, 0.07), (-0.25, -0.02)]
    prism(arch, -0.06, 0.25, (X, Z, -Y), s=0.3)
    box(-0.12, 0.12, 0.19, H, -0.21, 0.0); port("back", "partial", 0.14, (0, 0, -0.105))
    sbox(-0.18, 0.18, -0.06, 0.19, -0.21, 0.0, "dark", b=0.002)
    slab([(-0.255, -0.21), (-0.06, -0.21), (-0.06, -0.165)], -0.165, 0.165, (Y, Z, X), "sec")
    for x in (-0.09, 0.09): sbox(x - 0.03, x + 0.03, -0.24, -0.08, -0.004, 0.004, "glow", b=0.001, R=rot(X, -13), at=(0, 0, -0.183))
    for sx in (-1, 1): sbox(sx*0.215 - 0.012, sx*0.215 + 0.012, -0.256, -0.245, -0.17, -0.05, "glow", b=0.002)
    rod((0.17, 0.12, 0.07), (0.17, 0.12, 0.22), 0.012, "metal", 0.002, 6); ball(0.025, "glow", 8, 6, at=(0.17, 0.12, 0.225))
    empty("Deploy_Out", (0, -0.26, -0.15), (0, -1, 0))


@mod(59, "RampDeployer", "F", "Wyrzutnik rampy", "Rozstawia rampe (wersja montowana na robocie).", AMBER)
def ramp_deployer():
    plinth(0.06)
    for sx in (-1, 1): box(sx*0.215 - 0.04, sx*0.215 + 0.04, 0.10, 0.25, -0.20, -0.02)
    cyl(0.025, -0.245, 0.245, "metal", N=8, R=R_to(X), at=(0, 0.175, -0.07))
    # folded ramp: three wedge leaves fanned around the hinge
    for k, ang in enumerate((-8, -30, -52)):
        Rk = rot(X, ang)
        with K.group(R=Rk, at=(0, 0.175, -0.07)):
            prism([(-0.40 + k*0.035, -0.012), (0.0, -0.03), (0.0, 0.03), (-0.40 + k*0.035, 0.012)], -0.17, 0.17, (Y, Z, X), s=0.25)
            if k == 2:
                for x in (-0.10, 0.0, 0.10): sbox(x - 0.025, x + 0.025, -0.33, -0.06, 0.02, 0.03, "glow", b=0.002)
    empty("Deploy_Out", (0, -0.26, -0.1), (0, -1, 0))


@mod(60, "WallDeployer", "F", "Wyrzutnik sciany", "Rozstawia sciane oslonowa (wersja montowana na robocie).", BLUE)
def wall_deployer():
    plinth(0.06)
    for sx in (-1, 1): box(sx*0.225 - 0.035, sx*0.225 + 0.035, -0.17, 0.17, -0.20, 0.25); port("left" if sx < 0 else "right", "partial", 0.12, (0, 0, 0.02))
    node("Panels", (0, 0, 0), dict(type="slide", axis=(0, 0, 1), amount=0.02, frames=24))
    for k, y in enumerate((-0.10, 0.0, 0.10)):
        box(-0.185, 0.185, y - 0.03, y + 0.03, -0.19, 0.12 + k*0.05, s=0.3)
        sbox(-0.15, 0.15, y - 0.012, y + 0.012, 0.12 + k*0.05, 0.13 + k*0.05, "glow", b=0.003)
    node()
    empty("Deploy_Out", (0, -0.26, 0.0), (0, -1, 0))


@mod(61, "JumpPadDeployer", "F", "Wyrzutnik jump pada", "Rozstawia plyte wybijajaca (wersja montowana na robocie).", GREEN)
def jump_pad_deployer():
    plinth(0.06)
    cyl(0.09, -0.20, -0.13, "dark", 0.008, 14)
    for i in range(3): torus(0.10, 0.016, "metal", 20, 6, at=(0, 0, -0.115 + i*0.045))
    node("Pad", (0, 0, 0), dict(type="slide", axis=(0, 0, 1), amount=0.06, frames=12))
    lathe([(0, 0.0), (0.20, 0.0), (0.245, 0.035), (0.245, 0.07), (0.21, 0.07), (0.19, 0.05), (0, 0.05)], ["dark", "sec", "base", "sec", "glow", "dark"], 28)
    arrow = [(0.0, 0.11), (0.11, -0.01), (0.05, -0.01), (0.05, -0.10), (-0.05, -0.10), (-0.05, -0.01), (-0.11, -0.01)]
    slab(arrow, 0.048, 0.058, "z", "glow")
    node()
    for a in (45, 135, 225, 315): rod(rot(Z, a) @ np.array([0.20, 0, -0.20]), rot(Z, a) @ np.array([0.20, 0, -0.03]), 0.014, "sec", 0.003, 6)
    empty("Deploy_Out", (0, 0, 0.26), (0, 0, 1))
