# RoboRougelike #938 v2 - distinct mounting-base archetypes for the R4 weapon set (silhouette diversity pass).
# Every style keeps the R41 contract: mounting face at Z_PL0 (Socket_Mount_Bottom), all geometry inside the 0.55 m cell,
# top collar ending at z = +0.016 so the turntable (z 0.018..) and GunPivot (0, 0, 0.135) are unchanged.
# Only the shape between plate and collar changes: column, tripod, stepped octagon, drum, crab, box pedestal, finned column.
import math
from mathutils import Matrix
import r4lib as L
from r4lib import F, M, box, cyl, cone, tube, hull, revolve, hexbolt, prism_z, Part, Z_PL0, Z_PL1

Z_TOP = -0.004          # collar base (same as R41 z_top)
STYLES = ("r41", "column", "tripod", "stepped", "drum", "crab", "box", "finned")


def _rot(objs, a):
    for o in objs: o.data.transform(Matrix.Rotation(a, 4, 'Z'))


def _collar(pt, r=0.200, glow=True):
    revolve("collar", [(0, Z_TOP - 0.006), (r - 0.015, Z_TOP - 0.006), (r, Z_TOP + 0.002), (r, Z_TOP + 0.020), (0, Z_TOP + 0.020)],
            M["Armor_Dark"], pt.U, 48, local=True)
    if glow:
        cg = tube("collar_glow", r + 0.0015, r - 0.004, (0, 0, Z_TOP + 0.007), (0, 0, Z_TOP + 0.011), M["Energy_Hot"], pt.U, seg=48)
        cg.data.transform(Matrix.Rotation(math.pi/48, 4, 'Z'))


def _round_plate(name, work, r=0.250):
    return revolve(name, [(0, Z_PL0), (r - 0.006, Z_PL0), (r, Z_PL0 + 0.005), (r, Z_PL1 - 0.004), (r - 0.006, Z_PL1), (0, Z_PL1)],
                   M["Armor_Dark"], work, 48, local=True)


def _ngon(n, R, z, phase=0.0):
    return [(R*math.cos(phase + 2*math.pi*k/n), R*math.sin(phase + 2*math.pi*k/n), z) for k in range(n)]


def _plate_bolts(pt, r, n, phase=0.0, z=Z_PL1):
    for k in range(n):
        a = phase + 2*math.pi*(k + 0.5)/n
        cyl("plate_bolt", 0.009, (r*math.cos(a), r*math.sin(a), z - 0.002), (r*math.cos(a), r*math.sin(a), z + 0.006), M["Steel_Light"], pt.U, seg=6)


def _slot_glow(pt, r_face, z0, z1, w, n, phase=0.0, depth=0.012):
    """n vertical glowing slots cut into a round face of radius r_face"""
    for k in range(n):
        a = phase + 2*math.pi*k/n
        made = [box("slot", r_face - depth, r_face + 0.020, -w/2, w/2, z0, z1, M["Armor_Dark"], pt.D),
                box("slot_glow", r_face - depth - 0.002, r_face - depth + 0.0015, -w/2 + 0.001, w/2 - 0.001, z0 + 0.002, z1 - 0.002, M["Energy_Hot"], pt.Pc)]
        _rot(made, a)


def base_column(name, work):
    """slender pylon: round plate with six light gussets, narrow column, flared head, small collar (Railgun)"""
    pt = Part(work, "base")
    plate = _round_plate(name, work, 0.250)
    revolve("column", [(0, Z_PL1 - 0.002), (0.112, Z_PL1 - 0.002), (0.112, -0.196), (0.084, -0.176), (0.080, -0.070),
                       (0.104, -0.044), (0.150, -0.020), (0.156, Z_TOP - 0.004), (0, Z_TOP - 0.004)], M["Armor_Gunmetal"], pt.U, 32, local=True)
    tube("foot_team", 0.1150, 0.100, (0, 0, -0.214), (0, 0, -0.202), M["TeamColor_Red"], pt.Pc, seg=32)
    tube("neck_band", 0.0845, 0.070, (0, 0, -0.112), (0, 0, -0.100), M["Steel_Light"], pt.U, seg=32)
    for k in range(6):
        a = 2*math.pi*k/6 + math.pi/6
        made = [hull("gusset", [(x, s*0.007, z) for s in (-1, 1) for (x, z) in ((0.105, Z_PL1 - 0.001), (0.226, Z_PL1 - 0.001), (0.226, Z_PL1 + 0.012), (0.105, -0.150))],
                     M["Armor_Plate_Light"], pt.U, bevel=0.002),
                hexbolt("gusset_bolt", (0.212, 0.0065, Z_PL1 + 0.007), (0.212, 0.012, Z_PL1 + 0.007), M["Steel_Light"], pt.Pc)]
        _rot(made, a)
    for k in range(4):
        a = 2*math.pi*k/4 + math.pi/4
        made = [hull("head_pad", [(r, s*0.040, z) for s in (-1, 1) for (r, z) in ((0.118, -0.050), (0.150, -0.028), (0.148, -0.023), (0.112, -0.046))]
                     + [(r - 0.006, s*0.040, z) for s in (-1, 1) for (r, z) in ((0.118, -0.050), (0.150, -0.028))], M["Armor_Plate_Light"], pt.U)]
        _rot(made, a)
    _slot_glow(pt, 0.0815, -0.160, -0.122, 0.014, 4, math.pi/4)
    tube("plate_haz", 0.2505, 0.236, (0, 0, Z_PL1 - 0.0045), (0, 0, Z_PL1 + 0.0015), M["Hazard_Yellow"], pt.Pc, seg=48)
    _plate_bolts(pt, 0.170, 6, math.pi/6)
    _collar(pt, 0.172)
    pt.build(plate)
    return plate


def base_tripod(name, work):
    """three splayed legs with hydraulic struts on a triangular foot plate, central hub (RocketLauncher)"""
    pt = Part(work, "base")
    feet = [(0.200*math.cos(a), 0.200*math.sin(a)) for a in (math.radians(90), math.radians(210), math.radians(330))]
    pts = []
    for (fx, fy) in feet:
        for k in range(12):
            a = 2*math.pi*k/12
            pts += [(fx + 0.050*math.cos(a), fy + 0.050*math.sin(a), z) for z in (Z_PL0, Z_PL1)]
    plate = hull(name, pts, M["Armor_Dark"], work, bevel=0.003)
    revolve("hub", [(0, Z_PL1 - 0.002), (0.112, Z_PL1 - 0.002), (0.112, -0.212), (0.090, -0.196), (0.086, -0.050), (0.120, -0.030),
                    (0.150, Z_TOP - 0.004), (0, Z_TOP - 0.004)], M["Armor_Dark"], pt.U, 32, local=True)
    tube("hub_band", 0.0885, 0.075, (0, 0, -0.150), (0, 0, -0.130), M["Armor_Plate_Light"], pt.U, seg=32)
    tube("hub_team", 0.0890, 0.075, (0, 0, -0.112), (0, 0, -0.100), M["TeamColor_Red"], pt.Pc, seg=32)
    for k, a in enumerate((math.radians(90), math.radians(210), math.radians(330))):
        made = []
        made.append(hull("leg", [(r, s*w, z) for (r, z, w) in ((0.215, Z_PL1 - 0.001, 0.034), (0.230, Z_PL1 + 0.040, 0.030), (0.112, -0.050, 0.026), (0.094, -0.070, 0.028))
                                for s in (-1, 1)], M["Armor_Gunmetal"], pt.U, bevel=0.004))
        made.append(hull("leg_appl", [(r + d, s*w, z) for (r, z, w) in ((0.220, Z_PL1 + 0.034, 0.022), (0.128, -0.068, 0.018)) for d in (-0.004, 0.006) for s in (-1, 1)],
                         M["Armor_Plate_Light"], pt.U))
        made.append(hull("leg_team", [(r + d, s*0.0125, z) for (r, z) in ((0.198, Z_PL1 + 0.060), (0.170, Z_PL1 + 0.120)) for d in (0.004, 0.0085) for s in (-1, 1)],
                         M["TeamColor_Red"], pt.Pc))
        made.append(cyl("strut", 0.014, (0.172, 0.0, Z_PL1 + 0.004), (0.104, 0.0, -0.140), M["Armor_Dark"], pt.U, seg=12))
        made.append(cyl("strut_rod", 0.0085, (0.104, 0.0, -0.140), (0.090, 0.0, -0.118), M["Steel_Light"], pt.U, seg=12))
        made.append(cyl("foot", 0.054, (0.200, 0, Z_PL1 - 0.002), (0.200, 0, Z_PL1 + 0.010), M["Armor_Gunmetal"], pt.U, seg=24, bevel=0.003))
        made.append(tube("foot_haz", 0.0545, 0.046, (0.200, 0, Z_PL1 + 0.004), (0.200, 0, Z_PL1 + 0.0105), M["Hazard_Yellow"], pt.Pc, seg=24))
        for (bx, by) in ((0.200, 0.030), (0.200, -0.030), (0.226, 0.0)):
            made.append(hexbolt("foot_bolt", (bx, by, Z_PL1 + 0.009), (bx, by, Z_PL1 + 0.015), M["Steel_Light"], pt.Pc))
        _rot(made, a)
    _slot_glow(pt, 0.0865, -0.190, -0.160, 0.012, 3, math.radians(30))
    _collar(pt, 0.168)
    pt.build(plate)
    return plate


def base_stepped(name, work):
    """massive three-tier octagonal ziggurat with light skirt plates, team band, hot vents (Heavy)"""
    pt = Part(work, "base")
    ph = math.pi/8
    oct_ = lambda ap, z: _ngon(8, ap/math.cos(math.pi/8), z, ph)
    plate = hull(name, oct_(0.255, Z_PL0) + oct_(0.255, Z_PL1), M["Armor_Dark"], work, bevel=0.003)
    hull("tier1", oct_(0.246, Z_PL1 - 0.002) + oct_(0.246, -0.170), M["Armor_Gunmetal"], pt.U, bevel=0.005)
    hull("tier2", oct_(0.214, -0.172) + oct_(0.208, -0.086), M["Armor_Dark"], pt.U, bevel=0.005)
    hull("tier3", oct_(0.192, -0.088) + oct_(0.180, Z_TOP - 0.004), M["Armor_Gunmetal"], pt.U, bevel=0.004)
    for k in range(8):
        a = 2*math.pi*k/8
        made = [box("skirt", 0.244, 0.2535, -0.074, 0.074, -0.222, -0.180, M["Armor_Plate_Light"], pt.U, bevel=0.002),
                hexbolt("skirt_bolt", (0.252, -0.058, -0.201), (0.2575, -0.058, -0.201), M["Steel_Light"], pt.Pc, r=0.006),
                hexbolt("skirt_bolt", (0.252, 0.058, -0.201), (0.2575, 0.058, -0.201), M["Steel_Light"], pt.Pc, r=0.006)]
        if k % 2 == 0:
            made += [box("t2_team", 0.2085, 0.2135, -0.060, 0.060, -0.140, -0.122, M["TeamColor_Red"], pt.Pc),
                     box("t2_vent", 0.196, 0.230, -0.050, 0.050, -0.112, -0.096, M["Armor_Dark"], pt.D),
                     box("t2_glow", 0.1965, 0.2000, -0.048, 0.048, -0.110, -0.098, M["Energy_Hot"], pt.Pc)]
        else:
            made += [box("t3_appl", 0.1835, 0.1905, -0.050, 0.050, -0.070, -0.022, M["Armor_Plate_Light"], pt.U, bevel=0.002),
                     box("t1_haz", 0.2445, 0.2475, -0.040, 0.040, -0.176, -0.172, M["Hazard_Yellow"], pt.Pc)]
        _rot(made, a)
    _collar(pt, 0.192)
    pt.build(plate)
    return plate


def base_drum(name, work):
    """round ribbed drum with insulator stacks and a light mid band (EmpEmitter)"""
    pt = Part(work, "base")
    plate = _round_plate(name, work, 0.252)
    revolve("drum", [(0, Z_PL1 - 0.002), (0.214, Z_PL1 - 0.002), (0.214, -0.040), (0.200, -0.022), (0.190, Z_TOP - 0.004), (0, Z_TOP - 0.004)],
            M["Armor_Gunmetal"], pt.U, 48, local=True)
    tube("mid_band", 0.2215, 0.205, (0, 0, -0.172), (0, 0, -0.124), M["Armor_Plate_Light"], pt.U, seg=48)
    tube("team_band", 0.2225, 0.205, (0, 0, -0.068), (0, 0, -0.056), M["TeamColor_Red"], pt.Pc, seg=48)
    tube("haz_band", 0.2220, 0.205, (0, 0, -0.205), (0, 0, -0.200), M["Hazard_Yellow"], pt.Pc, seg=48)
    for k in range(16):
        a = 2*math.pi*(k + 0.5)/16
        _rot([box("rib", 0.212, 0.228, -0.006, 0.006, Z_PL1 - 0.001, -0.046, M["Armor_Dark"], pt.U, bevel=0.002)], a)
    for k in range(4):
        a = 2*math.pi*k/4 + math.pi/4
        made = [cyl("ins_post", 0.010, (0.236, 0, Z_PL1 - 0.001), (0.236, 0, -0.096), M["Armor_Dark"], pt.U, seg=12)]
        for j in range(5):
            z = -0.214 + 0.022*j
            made.append(cyl("ins_disc", 0.022, (0.236, 0, z), (0.236, 0, z + 0.008), M["Steel_Light"], pt.U, seg=16, bevel=0.002))
        made.append(cyl("ins_cap", 0.014, (0.236, 0, -0.098), (0.236, 0, -0.088), M["Energy_Hot"], pt.U, seg=12))
        made.append(hull("ins_bracket", [(r, s*0.010, z) for r in (0.206, 0.238) for s in (-1, 1) for z in (-0.104, -0.096)], M["Armor_Dark"], pt.U))
        _rot(made, a)
    _slot_glow(pt, 0.214, -0.108, -0.082, 0.020, 8, math.pi/8, depth=0.010)
    _plate_bolts(pt, 0.236, 8, 0.0)
    _collar(pt, 0.196)
    pt.build(plate)
    return plate


def base_crab(name, work):
    """low wide armoured belly on the square plate, narrow neck, four hydraulic outrigger struts (RotarySaw)"""
    pt = Part(work, "base")
    plate = hull(name, L.chamfer_square(L.PL_H, 0.040, Z_PL0) + L.chamfer_square(L.PL_H, 0.040, Z_PL1), M["Armor_Dark"], work, bevel=0.004)
    hull("belly", L.chamfer_square(0.246, 0.090, Z_PL1 - 0.003) + L.chamfer_square(0.232, 0.084, -0.198) + L.chamfer_square(0.170, 0.060, -0.168),
         M["Armor_Gunmetal"], pt.U, bevel=0.005)
    revolve("neck", [(0, -0.172), (0.108, -0.172), (0.104, -0.060), (0.130, -0.036), (0.168, -0.018), (0.170, Z_TOP - 0.004), (0, Z_TOP - 0.004)],
            M["Armor_Dark"], pt.U, 32, local=True)
    tube("neck_team", 0.1065, 0.095, (0, 0, -0.138), (0, 0, -0.106), M["TeamColor_Red"], pt.Pc, seg=32)
    for k in range(4):
        made = [hull("belly_appl", [(x, s*0.120, z) for (x, z) in ((0.240, -0.222), (0.229, -0.200)) for s in (-1, 1)]
                     + [(x, s*0.120, z) for (x, z) in ((0.236, -0.222), (0.225, -0.200)) for s in (-1, 1)], M["Armor_Plate_Light"], pt.U),
                box("belly_vent", 0.200, 0.250, -0.090, 0.090, -0.196, -0.186, M["Armor_Dark"], pt.D),
                box("belly_glow", 0.2125, 0.2165, -0.088, 0.088, -0.195, -0.187, M["Energy_Hot"], pt.Pc)]
        _rot(made, k*math.pi/2)
    for k in range(4):
        made = [cyl("pad", 0.034, (0.200, 0.0, Z_PL1 - 0.002), (0.200, 0.0, -0.212), M["Armor_Dark"], pt.U, seg=20, bevel=0.003),
                tube("pad_haz", 0.0345, 0.028, (0.200, 0, -0.2135), (0.200, 0, -0.2105), M["Hazard_Yellow"], pt.Pc, seg=20),
                cyl("outrigger", 0.016, (0.192, 0.0, -0.212), (0.100, 0.0, -0.090), M["Armor_Plate_Light"], pt.U, seg=12),
                cyl("outrigger_rod", 0.0095, (0.150, 0.0, -0.157), (0.098, 0.0, -0.088), M["Steel_Light"], pt.U, seg=12),
                cyl("outrigger_eye", 0.018, (0.100, -0.014, -0.086), (0.100, 0.014, -0.086), M["Armor_Dark"], pt.U, seg=12)]
        _rot(made, k*math.pi/2 + math.pi/4)
    _plate_bolts(pt, 0.218, 4, math.pi/4)
    _collar(pt, 0.172)
    pt.build(plate)
    return plate


def base_box(name, work):
    """upright rectangular pedestal (narrow in X, deep in fire direction) with side ammo crate and ribs (Shotgun)"""
    pt = Part(work, "base")
    plate = hull(name, L.chamfer_square(L.PL_H, 0.040, Z_PL0) + L.chamfer_square(L.PL_H, 0.040, Z_PL1), M["Armor_Dark"], work, bevel=0.004)
    box("pedestal", -0.150, 0.150, -0.214, 0.214, Z_PL1 - 0.002, -0.044, M["Armor_Gunmetal"], pt.U, bevel=0.012)
    hull("cap", [(sx*x, sy*y_, z) for sx in (-1, 1) for sy in (-1, 1) for (x, y_, z) in ((0.150, 0.200, -0.046), (0.150, 0.214, -0.056), (0.140, 0.170, Z_TOP - 0.004))],
         M["Armor_Dark"], pt.U, bevel=0.004)
    for s in (-1, 1):
        box(f"ped_appl{s}", s*0.1495, s*0.158, -0.170, 0.170, -0.200, -0.090, M["Armor_Plate_Light"], pt.U, bevel=0.003)
        box(f"ped_team{s}", s*0.1575, s*0.1615, -0.150, 0.150, -0.120, -0.106, M["TeamColor_Red"], pt.Pc)
        for f_ in (-0.130, -0.050, 0.030, 0.110):
            hexbolt(f"ped_bolt{s}", (s*0.157, f_, -0.186), (s*0.1625, f_, -0.186), M["Steel_Light"], pt.Pc)
    for sy in (-1, 1):
        for x_ in (-0.090, -0.030, 0.030, 0.090):
            box("ped_rib", x_ - 0.008, x_ + 0.008, sy*0.212, sy*0.224, -0.214, -0.060, M["Armor_Dark"], pt.U, bevel=0.002)
        box("ped_vent", -0.110, 0.110, sy*0.205, sy*0.230, -0.090, -0.068, M["Armor_Dark"], pt.D)
        box("ped_glow", -0.108, 0.108, sy*0.2055, sy*0.2085, -0.088, -0.070, M["Energy_Hot"], pt.Pc)
    box("crate", 0.164, 0.252, -0.120, 0.120, -0.230, -0.104, M["Armor_Gunmetal"], pt.U, bevel=0.006)
    box("crate_lid", 0.168, 0.248, -0.114, 0.114, -0.106, -0.096, M["Armor_Plate_Light"], pt.U, bevel=0.003)
    for f_ in (-0.090, -0.030, 0.030, 0.090):
        box("crate_haz", 0.2505, 0.2535, f_ - 0.012, f_ + 0.012, -0.214, -0.198, M["Hazard_Yellow"], pt.Pc)
    for f_ in (-0.060, 0.060):
        box("crate_latch", 0.247, 0.255, f_ - 0.014, f_ + 0.014, -0.130, -0.108, M["Steel_Light"], pt.U, bevel=0.002)
    _plate_bolts(pt, 0.218, 4, math.pi/4)
    _collar(pt, 0.170)
    pt.build(plate)
    return plate


def base_finned(name, work):
    """round column wrapped in a radial heat-sink of 18 fins (BeamCutter)"""
    pt = Part(work, "base")
    plate = _round_plate(name, work, 0.250)
    revolve("core", [(0, Z_PL1 - 0.002), (0.128, Z_PL1 - 0.002), (0.128, -0.050), (0.150, -0.030), (0.160, Z_TOP - 0.004), (0, Z_TOP - 0.004)],
            M["Armor_Dark"], pt.U, 36, local=True)
    tube("core_team", 0.1300, 0.115, (0, 0, -0.046), (0, 0, -0.026), M["TeamColor_Red"], pt.Pc, seg=36)
    for k in range(18):
        a = 2*math.pi*k/18
        mat = M["Armor_Plate_Light"] if k % 3 == 0 else M["Armor_Gunmetal"]
        made = [hull("fin", [(r, s*0.0045, z) for s in (-1, 1) for (r, z) in ((0.124, Z_PL1 - 0.001), (0.226, Z_PL1 - 0.001), (0.226, -0.150), (0.190, -0.072), (0.124, -0.060))],
                     mat, pt.U, bevel=0.0015)]
        if k % 3 == 1:
            made.append(box("fin_glow", 0.1290, 0.1310, -0.016, -0.006, -0.200, -0.090, M["Energy_Hot"], pt.Pc))
        _rot(made, a)
    tube("fin_band", 0.2285, 0.200, (0, 0, -0.214), (0, 0, -0.200), M["Armor_Dark"], pt.U, seg=36)
    tube("fin_haz", 0.2290, 0.2245, (0, 0, -0.2135), (0, 0, -0.2005), M["Hazard_Yellow"], pt.Pc, seg=36)
    _plate_bolts(pt, 0.236, 6, 0.0)
    _collar(pt, 0.176)
    pt.build(plate)
    return plate


def build_base_style(name, work, style, **kw):
    """returns (base_object, collar_top_z) exactly like r4lib.build_base"""
    if style == "r41": return L.build_base(name, work, **kw)
    fn = {"column": base_column, "tripod": base_tripod, "stepped": base_stepped, "drum": base_drum, "crab": base_crab,
          "box": base_box, "finned": base_finned}[style]
    ob = fn(name, work); ob["base_style"] = style
    return ob, Z_TOP + 0.020
