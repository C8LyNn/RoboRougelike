# R4 wheel variants (R3PartKind.Wheel family): sizes S/M/L/XL, tread types and mount types, all on the standard R4 port.
# Mount types: fork (top port, wheel under the cell), side (Robocraft-style hub on the -X face, wheel beside the block),
# steer (top port + yawing knuckle), caster (top port + free swivel with trail), swing (top port + trailing arm on a
# torsion housing), twin (top fork with dual tyres).  Tread: offroad, road, slick, spiked, balloon, monster.
# blender -b --factory-startup --python build_wheelvar.py -- <out_dir> <variant id>
import sys, os, math; sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import importlib, bpy, numpy as np, r4lib as L, r4qa as Q; importlib.reload(L); importlib.reload(Q)
from mathutils import Vector, Matrix
from r4lib import F, M, box, cyl, cone, tube, hull, revolve, hexbolt, Part, new_col
import r4port as P4

VAR = {
    "R4M_WheelS_Fork_Road":        dict(size="S", mount="fork", tread="road", rim="disc6", r=0.205, hw=0.075, name="Małe koło drogowe (widelec)"),
    "R4M_WheelS_Caster_Slick":     dict(size="S", mount="caster", tread="slick", rim="spoke5", r=0.170, hw=0.060, name="Kółko samonastawne (caster)"),
    "R4M_WheelM_Side_Offroad":     dict(size="M", mount="side", tread="offroad", rim="spoke6", r=0.390, hw=0.170, name="Koło terenowe, montaż boczny"),
    "R4M_WheelM_Side_Slick":       dict(size="M", mount="side", tread="slick", rim="spoke5", r=0.370, hw=0.150, name="Koło wyścigowe slick, montaż boczny"),
    "R4M_WheelM_Steer_Road":       dict(size="M", mount="steer", tread="road", rim="spoke8", r=0.340, hw=0.130, name="Koło skrętne drogowe"),
    "R4M_WheelM_SwingArm_Offroad": dict(size="M", mount="swing", tread="offroad", rim="spoke6", r=0.330, hw=0.130, name="Koło terenowe na wahaczu"),
    "R4M_WheelM_Twin_Road":        dict(size="M", mount="twin", tread="road", rim="disc8", r=0.330, hw=0.080, name="Koło bliźniacze (ciężarowe)"),
    "R4M_WheelM_Fork_Spiked":      dict(size="M", mount="fork", tread="spiked", rim="spoke6", r=0.370, hw=0.150, name="Koło z kolcami (widelec)"),
    "R4M_WheelL_Side_Offroad":     dict(size="L", mount="side", tread="offroad", rim="spoke8", r=0.560, hw=0.210, name="Duże koło terenowe, montaż boczny"),
    "R4M_WheelL_Fork_Balloon":     dict(size="L", mount="fork", tread="balloon", rim="disc8", r=0.500, hw=0.215, name="Duże koło balonowe (widelec)"),
    "R4M_WheelL_Steer_Offroad":    dict(size="L", mount="steer", tread="offroad", rim="spoke6", r=0.480, hw=0.170, name="Duże koło skrętne terenowe"),
    "R4M_WheelXL_Side_Monster":    dict(size="XL", mount="side", tread="monster", rim="spoke8", r=0.800, hw=0.290, name="Koło monster XL, montaż boczny"),
}
a_ = sys.argv[sys.argv.index("--") + 1:]; OUT, ID = a_[0], a_[1]; V = VAR[ID]
R_, HW, MT = V["r"], V["hw"], V["mount"]
SC = R_/0.39
L.reset_scene(); L.init_palette(); scn = bpy.context.scene
work = new_col("_build_tmp")
TOP = P4.FACE

# ------------------------------------------------------------------------------------------------ layout per mount
if MT == "side":
    XC, ZC, YC = -0.26 + 0.100 + HW + 0.012, 0.0, 0.0        # wheel beside the block (port on -X)
    CENTRES = [XC]
elif MT == "fork":
    XC, YC, ZC = 0.0, 0.0, 0.130 - R_; CENTRES = [0.0]
elif MT == "twin":
    XC, YC, ZC = 0.0, 0.0, 0.130 - R_; CENTRES = [-(HW + 0.010), HW + 0.010]
elif MT == "steer":
    XC, YC, ZC = 0.0, 0.0, 0.025 - R_; CENTRES = [0.0]
elif MT == "caster":
    XC, YC, ZC = 0.0, 0.075, 0.040 - R_; CENTRES = [0.0]
elif MT == "swing":
    XC, YC, ZC = 0.0, 0.0, -0.300; CENTRES = [0.0]
SPAN = max(abs(c) + HW for c in CENTRES)          # half axial extent of the tyres around x = 0 (non-side mounts)


def ax(x, r, phi):      # point on a circle of radius r around the axle (phi 0 = forward, 90 = up) at axial x
    return (x, YC + F(r*math.cos(math.radians(phi))), ZC + r*math.sin(math.radians(phi)))


# ------------------------------------------------------------------------------------------------ rotor (tyre + rim)
LUG_H = {"offroad": 0.115, "monster": 0.140, "spiked": 0.060, "road": 0.0, "slick": 0.0, "balloon": 0.035}[V["tread"]]*R_
RC = R_ - LUG_H                        # carcass radius
RR = R_*0.63 if V["tread"] != "balloon" else R_*0.55     # rim radius
RI = RR + 0.008


def tyre_profile(hw):
    if V["tread"] == "balloon":
        hr = (RC - RI)*0.62; rcen = RC - hr
        pts = [(RI, -hw*0.90)] + [(rcen + hr*math.cos(math.radians(a)), hw*math.sin(math.radians(a))) for a in range(-90, 91, 15)] + [(RI, hw*0.90)]
        return pts
    return [(RI, -hw*0.94), (RC*0.80, -hw), (RC*0.93, -hw*0.97), (RC*0.975, -hw*0.88), (RC, -hw*0.25), (RC, hw*0.25),
            (RC*0.975, hw*0.88), (RC*0.93, hw*0.97), (RC*0.80, hw), (RI, hw*0.94)]


def tread(rp, xc, hw, tag):
    t = V["tread"]
    if t in ("offroad", "monster"):
        n = max(12, int(round((20 if t == "offroad" else 16)*SC/(1 if t == "offroad" else 2.05))) // 2*2)
        dd = 3.2*0.39/R_*(1.0 if t == "offroad" else 2.6)
        for i in range(n):
            ph = i*360.0/n
            for s in (-1, 1):
                x0, x1 = (0.010, hw*0.92) if t == "offroad" else (-0.004, hw*0.97)
                sk = (4.0*0.39/R_ if t == "offroad" else 0.0)
                pts = [ax(xc + s*x, r, ph + d + (s*sk if x > hw*0.45 else 0.0)) for x in (x0, x1) for r in (RC - 0.010, R_) for d in (-dd, dd)]
                hull(f"lug{tag}{i}{s}", pts, M["Armor_Dark"], rp.U)
                if t == "offroad":
                    hull(f"sh{tag}{i}{s}", [ax(xc + s*x, r, ph + d) for x in (hw*0.82, hw*0.995) for r in (RC*0.86, RC - 0.004) for d in (-dd*0.75, dd*0.75)], M["Armor_Dark"], rp.U)
    elif t in ("road", "spiked"):
        for g in (-0.45, 0.0, 0.45):
            w = 0.006*max(SC, 0.7)
            tube(f"groove{tag}{g}", RC + 0.01, RC - 0.009*max(SC, 0.7), (xc + g*hw - w, YC, ZC), (xc + g*hw + w, YC, ZC), M["Armor_Dark"], rp.D, seg=72)
        n = int(round(40*SC));
        for i in range(n):
            ph = i*360.0/n; s = 1 if i % 2 else -1
            hull(f"sipe{tag}{i}", [ax(xc + s*x, r, ph + d) for x in (hw*0.52, hw*0.98) for r in (RC - 0.0095, RC + 0.01) for d in (-0.5*0.39/R_, 0.5*0.39/R_)], M["Armor_Dark"], rp.D)
        if t == "spiked":
            nn = int(round(12*SC))
            for row, xo in enumerate((-0.62, -0.22, 0.22, 0.62)):        # rows clear of the three grooves
                for i in range(nn):
                    ph = (i + 0.5*(row % 2))*360.0/nn + 1.7
                    p0 = ax(xc + xo*hw, RC - 0.012, ph); p1 = ax(xc + xo*hw, R_, ph)
                    cone(f"stud{tag}{row}{i}", 0.017, 0.004, p0, p1, M["Steel_Light"], rp.U, seg=8)
    elif t == "slick":
        for s in (-1, 1):
            tube(f"stripe{tag}{s}", RC*0.90, RC*0.86, (xc + s*hw*0.955, YC, ZC), (xc + s*hw*1.012, YC, ZC), M["TeamColor_Red"], rp.Pc, seg=72)
    elif t == "balloon":
        n = int(round(30*SC))
        for i in range(n):
            ph = i*360.0/n
            for s in (-1, 1):
                hull(f"blk{tag}{i}{s}", [ax(xc + s*x, r, ph + (s*360.0/n/2) + d) for x in (hw*0.10, hw*0.55) for r in (RC - 0.030, R_) for d in (-2.2*0.39/R_, 2.2*0.39/R_)], M["Armor_Dark"], rp.U)


def rim(rp, xc, hw, tag, disc_side):
    rb = 0.036*max(SC, 0.6)
    tube(f"rim{tag}", RR, RR - rb, (xc - hw*0.83, YC, ZC), (xc + hw*0.83, YC, ZC), M["Armor_Gunmetal"], rp.U, seg=48)
    for s in (-1, 1):
        tube(f"rim_lip{tag}{s}", RR + 0.010*max(SC, .6), RR - rb, (xc + s*hw*0.83 - 0.008, YC, ZC), (xc + s*hw*0.83 + 0.008, YC, ZC), M["Steel_Light"], rp.U, seg=48)
    hub_r = max(0.050, 0.090*SC); hub_w = min(0.080, hw*0.75)
    cyl(f"hub{tag}", hub_r, (xc - hub_w, YC, ZC), (xc + hub_w, YC, ZC), M["Armor_Dark"], rp.U, seg=32, bevel=0.006)
    cap_x = xc - disc_side*hub_w
    cyl(f"cap{tag}", hub_r*0.56, (cap_x - disc_side*0.016, YC, ZC), (cap_x + disc_side*0.002, YC, ZC), M["Steel_Light"], rp.U, seg=32, bevel=0.003)
    for k in range(8 if hub_r > 0.06 else 5):
        a = 2*math.pi*(k + 0.5)/(8 if hub_r > 0.06 else 5); rr_ = hub_r*0.74
        p = (YC + F(rr_*math.cos(a)), ZC + rr_*math.sin(a))
        hexbolt(f"nut{tag}{k}", (cap_x + disc_side*0.002, p[0], p[1]), (cap_x - disc_side*0.009, p[0], p[1]), M["Steel_Light"], rp.Pc, r=0.0055)
    kind, n = V["rim"][:-1], int(V["rim"][-1])
    if kind == "spoke":
        for k in range(n):
            a = k*360.0/n; d0 = 9.0*0.39/R_ if R_ < 0.5 else 6.0; d1 = 6.0*0.39/R_ if R_ < 0.5 else 4.0
            xs = (xc - disc_side*hw*0.25, xc - disc_side*(hw*0.25 - 0.056*max(SC, .6)))
            hull(f"spoke{tag}{k}", [ax(x, r, a + d) for x in xs for (r, d) in ((hub_r*0.9, -d0), (hub_r*0.9, d0), (RR - rb + 0.004, -d1), (RR - rb + 0.004, d1))], M["Armor_Plate_Light"], rp.U, bevel=0.003)
    else:
        x0 = xc - disc_side*hw*0.30; x1 = x0 + disc_side*0.022*max(SC, .7)
        tube(f"disc_plate{tag}", RR - rb + 0.004, hub_r*0.9, (min(x0, x1), YC, ZC), (max(x0, x1), YC, ZC), M["Armor_Plate_Light"], rp.U, seg=48)
        rm = (hub_r + RR - rb)/2; hr = min(0.4*(RR - rb - hub_r), 0.16*rm*2*math.pi/n)
        for k in range(n):
            a = 2*math.pi*(k + 0.5)/n
            cyl(f"disc_hole{tag}{k}", hr, (min(x0, x1) - 0.01, YC + F(rm*math.cos(a)), ZC + rm*math.sin(a)), (max(x0, x1) + 0.01, YC + F(rm*math.cos(a)), ZC + rm*math.sin(a)), M["Armor_Dark"], rp.D, seg=16)
    # brake disc on the inner side of the rim barrel
    bx = xc + disc_side*min(hw*0.55, hub_w + 0.03)
    tube(f"bdisc{tag}", min(RR - rb - 0.012, 0.20*SC + 0.02), hub_r - 0.006, (bx - 0.006, YC, ZC), (bx + 0.006, YC, ZC), M["Steel_Light"], rp.U, seg=48)


def build_rotor():
    rot = None; rp = None
    disc_side = -1 if MT == "side" else 1           # side mount: brake on the inner (-X) side
    for i, xc in enumerate(CENTRES):
        x_ = xc if MT != "side" else XC
        ob = revolve(f"{ID}_Rotor" if i == 0 else f"tyre{i}", tyre_profile(HW), M["Armor_Dark"], work if i == 0 else rp.U, 72,
                     (x_ - 1, YC, ZC), (x_ + 1, YC, ZC), closed_ring=True)
        if i == 0: rot = ob; rp = Part(work, "r")
        tread(rp, x_, HW, str(i)); rim(rp, x_, HW, str(i), disc_side if MT != "twin" else (-1 if xc > 0 else 1))
    if MT == "twin":
        cyl("twin_drum", max(0.050, 0.090*SC)*0.8, (CENTRES[0] + 0.02, YC, ZC), (CENTRES[1] - 0.02, YC, ZC), M["Armor_Gunmetal"], rp.U, seg=32)
    bore_r = 0.0255
    if MT == "side":
        cyl("bore", bore_r, (XC - HW - 0.05, YC, ZC), (XC + min(0.080, HW*0.75)*0.55, YC, ZC), M["Armor_Dark"], rp.D, seg=20)
    else:
        cyl("bore", bore_r, (-SPAN - 0.05, YC, ZC), (SPAN + 0.05, YC, ZC), M["Armor_Dark"], rp.D, seg=20)
    rp.build(rot)
    return rot


# ------------------------------------------------------------------------------------------------ mounts
def top_body(mp_target_name, half_x, z0=0.165, colname=None):
    hx = max(0.17, min(half_x, 0.252))
    ob = hull(mp_target_name, [(x, y, z) for (x, z) in ((-hx, z0), (hx, z0), (-hx + 0.014, 0.262), (hx - 0.014, 0.262)) for y in (-0.110, 0.110)] +
              [(x, y, z0 + 0.04) for x in (-hx - 0.002, hx + 0.002) for y in (-0.125, 0.125)], M["Armor_Gunmetal"], work, bevel=0.006)
    mp = Part(work, "m")
    box("top_plate", -0.120, 0.120, -0.080, 0.080, 0.250, 0.270, M["Armor_Plate_Light"], mp.U, bevel=0.003)
    for t in (-1, 1):
        box("top_team", -hx + 0.01, hx - 0.01, t*0.126 - 0.003, t*0.126 + 0.003, z0 + 0.012, z0 + 0.050, M["TeamColor_Red"], mp.Pc)
    for s in (-1, 1):
        for t in (-1, 1):
            hexbolt("plate_bolt", (s*0.095, t*0.058, 0.245), (s*0.095, t*0.058, 0.2735), M["Steel_Light"], mp.Pc, r=0.008)
    return ob, mp


def fork_legs(mp, x_in, x_out, z_top, z_ax, y_ax=0.0, tag="", col=None):
    for s in (-1, 1):
        leg = [(s*x, y_ax*0.0 + F(f), z) for x in (x_in, x_out) for (f, z) in ((-0.100, z_top), (0.100, z_top))] + \
              [(s*x, YC + F(f), z) for x in (x_in, x_out) for (f, z) in ((0.055, z_ax + 0.070), (-0.055, z_ax + 0.070))]
        hull(f"leg{tag}{s}", leg, M["Armor_Dark"], mp.U, bevel=0.004)
        hull(f"web{tag}{s}", [(s*x, YC + F(f), z) for x in (x_in + 0.002, x_out - 0.002) for (f, z) in ((-0.045, z_ax + 0.075), (0.045, z_ax + 0.075), (0.040, z_ax), (-0.040, z_ax))], M["Armor_Dark"], mp.U)
        cyl(f"boss{tag}{s}", 0.050, (s*x_in, YC, z_ax), (s*x_out, YC, z_ax), M["Armor_Dark"], mp.U, seg=24, bevel=0.004)
        tube(f"boss_glow{tag}{s}", 0.038, 0.029, (s*(x_out - 0.0015), YC, z_ax), (s*(x_out + 0.0020), YC, z_ax), M["Energy_Hot"], mp.Pc, seg=24)
        nh = max(1, min(4, int((z_top - z_ax - 0.156)/0.056) + 1))
        for k in range(nh):
            zz = z_ax + 0.100 + k*0.056
            hull(f"haz{tag}{s}{k}", [(s*x, YC*0.5 + F(f), z) for x in (x_out - 0.008, x_out + 0.0015) for (f, z) in ((0.026, zz), (0.026, zz + 0.018), (-0.026, zz + 0.036), (-0.026, zz + 0.018))], M["Hazard_Yellow"], mp.Pc)
    cyl(f"axle{tag}", 0.022, (-x_out + 0.003, YC, z_ax), (x_out - 0.003, YC, z_ax), M["Steel_Light"], mp.U, seg=20)
    xa, xb = max(c_ + HW*0.83 + 0.014 for c_ in CENTRES), x_in - 0.006           # between the rim lip and the fork leg
    zt = math.sqrt(max(0.0, (RI - 0.022)**2 - 0.045**2))
    if xb - xa >= 0.018 and zt > 0.10:                                             # team brake caliper (radially inside the tyre bead)
        box(f"caliper{tag}", xa, xb + 0.010, YC - 0.045, YC + 0.045, z_ax + zt - 0.065, z_ax + zt, M["TeamColor_Red"], mp.U, bevel=0.006)


def build_mount():
    """returns (static mount, animated carrier or None, carrier pivot)"""
    if MT in ("fork", "twin"):
        x_in = SPAN + 0.020; x_out = SPAN + 0.064
        ob, mp = top_body(f"{ID}_Mount", x_out + 0.004)
        fork_legs(mp, x_in, x_out, 0.170, ZC)
        mp.build(ob); P4.port(ob, work, 1)
        return ob, None, None
    if MT == "side":
        # authored with the port up (+Z) and the axle vertical; rotated so the port faces -X and the axle runs along X
        ob = hull(f"{ID}_Mount", [(x, y, z) for (x, y) in ((0.21, 0.17), (0.17, 0.21), (-0.17, 0.21), (-0.21, 0.17), (-0.21, -0.17), (-0.17, -0.21), (0.17, -0.21), (0.21, -0.17)) for z in (0.205, 0.262)], M["Armor_Gunmetal"], work, bevel=0.006)
        mp = Part(work, "m")
        zc_up = -XC                                    # wheel centre along the port-up axle
        cyl("bearing", 0.115*max(SC, .8) if SC < 1.3 else 0.15, (0, 0, 0.212), (0, 0, 0.162), M["Armor_Dark"], mp.U, seg=32, bevel=0.006)
        cyl("stub", 0.022, (0, 0, 0.165), (0, 0, zc_up - min(0.080, HW*0.75)*0.55 + 0.004), M["Steel_Light"], mp.U, seg=20)
        for k in range(4):
            a = math.pi/4 + k*math.pi/2
            hull(f"gusset{k}", [(0.10*math.cos(a) + dx, 0.10*math.sin(a) + dy, z) for z in (0.165, 0.206) for (dx, dy) in ((0.012*math.cos(a + math.pi/2), 0.012*math.sin(a + math.pi/2)), (-0.012*math.cos(a + math.pi/2), -0.012*math.sin(a + math.pi/2)))] +
                 [(0.19*math.cos(a) + dx, 0.19*math.sin(a) + dy, 0.206) for (dx, dy) in ((0.012*math.cos(a + math.pi/2), 0.012*math.sin(a + math.pi/2)), (-0.012*math.cos(a + math.pi/2), -0.012*math.sin(a + math.pi/2)))], M["Armor_Gunmetal"], mp.U)
        tube("bearing_glow", 0.060, 0.048, (0, 0, 0.1605), (0, 0, 0.1645), M["Energy_Hot"], mp.Pc, seg=32)
        box("caliper", -0.050, 0.050, F(0.14) - 0.0, F(0.14) + 0.045, 0.150, 0.205, M["TeamColor_Red"], mp.U, bevel=0.008)
        for s in (-1, 1):
            hull(f"haz{s}", [(x, s*y, z) for x in (-0.16, 0.16) for (y, z) in ((0.2105, 0.212), (0.2125, 0.212), (0.2105, 0.236), (0.2125, 0.236))], M["Hazard_Yellow"], mp.Pc)
            box(f"team{s}", s*0.2095 - 0.002, s*0.2095 + 0.002, -0.12, 0.12, 0.214, 0.236, M["TeamColor_Red"], mp.Pc)
        mp.build(ob); P4.port(ob, work, 1)
        ob.data.transform(Matrix.Rotation(-math.pi/2, 4, 'Y'))
        return ob, None, None
    if MT in ("steer", "caster"):
        ob, mp = top_body(f"{ID}_Mount", 0.20)
        cyl("kingpin_housing", 0.072, (0, 0, 0.167), (0, 0, 0.122), M["Armor_Dark"], mp.U, seg=32, bevel=0.005)
        tube("kp_glow", 0.074, 0.060, (0, 0, 0.1215), (0, 0, 0.1245), M["Energy_Hot"], mp.Pc, seg=32)
        mp.build(ob); P4.port(ob, work, 1)
        piv = Vector((0, 0, 0.100))
        cr = cyl(f"{ID}_{'Knuckle' if MT == 'steer' else 'Swivel'}", 0.058, (0, 0, 0.119), (0, 0, 0.080), M["Armor_Gunmetal"], work, seg=32, bevel=0.004)
        cp = Part(work, "c")
        x_in = SPAN + 0.020; x_out = SPAN + 0.060
        zb = 0.080 if MT == "steer" else 0.095
        box("crossbar", -x_out + 0.004, x_out - 0.004, -0.080, 0.080, zb - 0.040, zb + 0.004, M["Armor_Gunmetal"], cp.U, bevel=0.006)
        if MT == "steer":
            box("steer_arm", -0.030, 0.030, -0.205, -0.07, zb - 0.030, zb - 0.006, M["Armor_Dark"], cp.U, bevel=0.004)
            cyl("tie_ball", 0.020, (0, -0.195, zb - 0.034), (0, -0.195, zb - 0.002), M["Steel_Light"], cp.U, seg=16)
        fork_legs(cp, x_in, x_out, zb - 0.030, ZC, tag="k")
        for t in (-1, 1):
            box("team_k", -x_out + 0.01, x_out - 0.01, t*0.0805 - 0.0015, t*0.0805 + 0.0015, zb - 0.032, zb - 0.010, M["TeamColor_Red"], cp.Pc)
        cp.build(cr)
        return ob, cr, piv
    if MT == "swing":
        ob, mp = top_body(f"{ID}_Mount", 0.20)
        PY, PZ = -0.205, 0.110                          # pivot: forward, high
        box("hanger", -0.17, min(0.17, SPAN + 0.012), PY - 0.045, PY + 0.045, PZ + 0.03, 0.172, M["Armor_Dark"], mp.U, bevel=0.005)
        cyl("torsion_housing", 0.058, (-0.17, PY, PZ), (SPAN + 0.018, PY, PZ), M["Armor_Gunmetal"], mp.U, seg=32, bevel=0.005)
        for k in range(3):
            tube(f"torsion_band{k}", 0.0595, 0.050, (-0.12 + k*0.09, PY, PZ), (-0.11 + k*0.09, PY, PZ), M["Hazard_Yellow"], mp.Pc, seg=32)
        mp.build(ob); P4.port(ob, work, 1)
        piv = Vector((0, PY, PZ))
        xa0, xa1 = SPAN + 0.022, SPAN + 0.066
        cr = cyl(f"{ID}_Arm", 0.060, (xa0, PY, PZ), (xa1, PY, PZ), M["Armor_Dark"], work, seg=32, bevel=0.005)
        cp = Part(work, "c")
        hull("arm_beam", [(x, y, z) for x in (xa0 + 0.004, xa1 - 0.004) for (y, z) in ((PY + 0.02, PZ + 0.045), (PY - 0.02, PZ - 0.045), (YC + 0.03, ZC + 0.040), (YC - 0.03, ZC - 0.040))], M["Armor_Dark"], cp.U, bevel=0.004)
        cyl("arm_boss", 0.050, (xa0, YC, ZC), (xa1, YC, ZC), M["Armor_Dark"], cp.U, seg=24, bevel=0.004)
        cyl("axle", 0.022, (-SPAN - 0.03, YC, ZC), (xa0 + 0.002, YC, ZC), M["Steel_Light"], cp.U, seg=20)
        cyl("axle_end", 0.030, (-SPAN - 0.045, YC, ZC), (-SPAN - 0.028, YC, ZC), M["Steel_Light"], cp.U, seg=20)
        tube("arm_glow", 0.040, 0.030, (xa1 - 0.0015, YC, ZC), (xa1 + 0.002, YC, ZC), M["Energy_Hot"], cp.Pc, seg=24)
        dy, dz = YC - PY, ZC - PZ; ln = math.hypot(dy, dz); ny, nz = -dz/ln, dy/ln
        hull("arm_haz", [(x, PY + t*dy + o*ny, PZ + t*dz + o*nz) for x in (xa1 - 0.002, xa1 + 0.0015) for t in (0.35, 0.50) for o in (-0.012, 0.012)], M["Hazard_Yellow"], cp.Pc)
        cp.build(cr)
        return ob, cr, piv


mount, carrier, piv = build_mount()
rotor = build_rotor()
centre = Vector((XC if MT == "side" else 0.0, YC, ZC))
col = L.gather(ID, [o for o in (mount, carrier, rotor) if o])
root = L.empty(ID + "_ROOT", None, (0, 0, 0), col, 'PLAIN_AXES', 0.1)
mount.parent = root
if carrier:
    carrier.data.transform(Matrix.Translation(-piv)); carrier.parent = root; carrier.location = piv
rotor.data.transform(Matrix.Translation(-centre))
rotor.parent = carrier if carrier else root
rotor.location = centre - (piv if carrier else Vector())
axle_e = L.empty("Wheel_Axle", rotor.parent, rotor.location, col, 'SINGLE_ARROW', 0.08, (0, math.radians(90), 0))
if MT == "side":
    socket = L.empty("Socket_Mount", root, (-TOP, 0, 0), col, 'SINGLE_ARROW', 0.08, (0, -math.pi/2, 0))
else:
    socket = L.empty("Socket_Mount", root, (0, 0, TOP), col, 'SINGLE_ARROW', 0.08)
probe = L.empty("GroundProbe", root, (centre.x, centre.y, centre.z - R_), col, 'SINGLE_ARROW', 0.06, (math.pi, 0, 0))
L.root_props(root, "R3PartKind.Wheel", version="r4-wheel-variant")
root["wheel_variant"] = ID; root["wheel_size"] = V["size"]; root["mount_type"] = MT; root["tread"] = V["tread"]; root["rim"] = V["rim"]
root["tyre_radius_m"] = R_; root["tyre_width_m"] = round(2*HW, 3); root["issue"] = "wheel variants (R4)"
props = {mount: {"role": f"static {MT} mount; R4 port on the {'-X (side)' if MT == 'side' else 'top (+Y)'} face = Socket_Mount"},
         rotor: {"role": "rolling rotor: rotates about local X only; +X angle = rolling forward (+Z)", "tyre_radius_m": R_, "tyre_width_m": round(2*HW, 3)},
         axle_e: {"role": "axle axis marker: local +Z = file +X (axle)"}, socket: {"role": "mount face centre (R4 port)"},
         probe: {"role": "ground contact point at rest", "ground_y_m": round(centre.z - R_, 4)}}
if carrier:
    props[carrier] = {"role": {"steer": "steering knuckle: yaw about local Y (Unity), +angle = right, limit +-30 deg",
                               "caster": "free swivel: yaw about local Y (Unity), trail 0.075 m behind the swivel axis",
                               "swing": "trailing arm: pitch about local X at the torsion pivot, +angle = compression (wheel up), +10/-8 deg"}[MT]}
L.set_props(props)
PARTS = [o for o in (mount, carrier, rotor) if o]
for o in PARTS: L.shade_and_uv(o)

# ------------------------------------------------------------------------------------------------ animation
T = L.Timeline()
def roll(f, d, i='LINEAR'): L.key(rotor, "rotation_euler", 0, f, math.radians(d), i)
def yaw(f, d, i='BEZIER'): L.key(carrier, "rotation_euler", 2, f, math.radians(d), i)
def arm(f, d, i='BEZIER'): L.key(carrier, "rotation_euler", 0, f, math.radians(d), i)
CARR = {"steer": yaw, "caster": yaw, "swing": arm}.get(MT)
REV = 24
def roll_loop(s, n=24, sign=1):
    for t in range(0, n + 1, 6): roll(s + t, sign*360.0*t/REV)
s, e = T.clip("Idle", 24, loop=True, desc="parked"); roll(s, 0); roll(e, 0)
if CARR: CARR(s, 0); CARR(e, 0)
s, e = T.clip("Roll", 24, loop=True, desc="rolling forward, 1 rev / 24 f (drive from wheel angular velocity)"); roll_loop(s)
if CARR: CARR(s, 0); CARR(e, 0)
T.clips[-1]["render_frames"] = [s + 3]
s, e = T.clip("Reverse", 24, loop=True, desc="rolling backwards, 1 rev / 24 f"); roll_loop(s, sign=-1)
if CARR: CARR(s, 0); CARR(e, 0)
s, e = T.clip("Brake", 24, desc="braking from loop speed to stop")
n_rest = 1
for t in range(0, 25): roll(s + t, 15.0*t - 15.0*t*t/(2*24.0))
roll(e + 1, 0.0, 'CONSTANT')
if CARR: CARR(s, 0); CARR(e, 0)
if MT == "steer":
    for nm, sg in (("SteerLeft", -1), ("SteerRight", 1)):
        s, e = T.clip(nm, 36, desc=f"steer {nm[5:].lower()} to {sg*30:+d} deg and back while rolling"); roll(s, 0); roll(e, 0)
        yaw(s, 0); yaw(s + 12, sg*30); yaw(s + 24, sg*30); yaw(e, 0)
        T.clips[-1]["render_frames"] = [s + 18]
    s, e = T.clip("Slalom", 48, loop=True, desc="rolling with steering +-30 deg (sine)"); roll_loop(s, 48)
    for t in range(0, 49, 6): yaw(s + t, 30*math.sin(2*math.pi*t/48))
if MT == "caster":
    s, e = T.clip("Swivel", 24, desc="direction change: swivel 0 -> 180 deg (trail flips behind)"); roll(s, 0); roll(e, 0); yaw(s, 0); yaw(e, 180)
    T.clips[-1]["render_frames"] = [s + 12]
    s, e = T.clip("Return", 24, desc="swivel 180 -> 0 deg"); roll(s, 0); roll(e, 0); yaw(s, 180); yaw(e, 0)
if MT == "swing":
    s, e = T.clip("Compress", 24, desc="bump: arm +10 deg (wheel up) and back"); roll(s, 0); roll(e, 0); arm(s, 0); arm(s + 8, 10); arm(e, 0)
    T.clips[-1]["render_frames"] = [s + 8]
    s, e = T.clip("Rebound", 24, desc="droop: arm -8 deg and back"); roll(s, 0); roll(e, 0); arm(s, 0); arm(s + 10, -8); arm(e, 0)
    s, e = T.clip("Bump", 48, loop=True, desc="rolling over rough ground (arm oscillation)"); roll_loop(s, 48)
    for t in range(0, 49, 6): arm(s + t, 6*math.sin(2*math.pi*t/24))
roll(T.end + 1, 0.0, 'CONSTANT')
if CARR: CARR(T.end + 1, 0.0, 'CONSTANT')
L.name_action(rotor, rotor.name + "_Master")
if carrier: L.name_action(carrier, carrier.name + "_Master")
scn.frame_set(1)

# ------------------------------------------------------------------------------------------------ QA
ext = {"side": "tyre beside the block (+X); fork-less hub carrier inside the mount cell",
       "fork": "fork legs and tyre reach below the cell", "twin": "dual tyres below the cell", "steer": "knuckle + tyre below the cell, yaw sweep +-30 deg",
       "caster": "swivel sweep 360 deg below the cell", "swing": "trailing arm and tyre below the cell"}[MT]
cp = [(rotor, mount)] + ([(rotor, carrier), (carrier, mount)] if carrier else [])
S = dict(id=ID, kind="R3PartKind.Wheel", game_name=V["name"], out=OUT, root=root, pivot=axle_e, yaw=None, pitch=None, socket=socket, movement=True,
         export=[root] + PARTS + [axle_e, socket, probe], parts=PARTS, fx=[], muzzles=[], housing=[mount], ground_parts=[rotor],
         housing_strict=False, housing_note=f"wheel variant ({V['size']}, {MT}): {ext}; there is no runtime placeholder for this variant yet",
         ground_note=f"design ground contact y = {centre.z - R_:.3f} m (GroundProbe); no runtime contract yet",
         clearance_pairs=cp, clash_pairs=cp, frame_end=T.end, clips=T.clips, hero_frame=1, tri_budget=32000,
         sil_center=(centre.x*0.5, 0, (0.26 + centre.z - R_)/2), sil_scale=max(1.2, 2*R_ + 0.7 + abs(centre.x)),
         notes=[f"variant {ID}: size {V['size']}, mount {MT}, tread {V['tread']}, rim {V['rim']}, tyre r {R_} m, width {2*HW:.3f} m",
                "L/R variant: mirror by transform (scale X -1)." + (" Side mount: the mirrored copy has its port on +X." if MT == "side" else "")])
Q.run(S)
