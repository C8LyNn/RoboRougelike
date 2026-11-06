# RoboRougelike #938 v2 - alternative yaw / pitch architectures (silhouette diversity pass).
# All parts keep the R41 rig contract: GunPivot (0, 0, P), Yaw about Z, Pitch about X, yaw body inside r 0.275.
import math
from mathutils import Matrix
import r4lib as L
from r4lib import F, P, M, box, cyl, cone, tube, hull, prism_x, revolve, hexbolt, arc_plate


def sphere(name, r, centre, mat, col, n=16, seg=32):
    prof = [(0.0, -r)] + [(r*math.sin(math.pi*i/n), -r*math.cos(math.pi*i/n)) for i in range(1, n)] + [(0.0, r)]
    ob = revolve(name, prof, mat, col, seg, local=True)
    ob.data.transform(Matrix.Translation(centre))
    return ob


# ---------------- Heavy: low wide sponsons + drum mantlet ------------------------------------------------------
def sponsons(y, x_in=0.156, x_out=0.214, f0=-0.150, f1=0.118):
    prof = [(f1, 0.035), (f1, 0.088), (0.060, 0.162), (-0.120, 0.168), (f0, 0.140), (f0, 0.035)]
    def xo(z): return x_out - 0.040*(z - 0.035)/0.133
    for s in (-1, 1):
        hull(f"sponson{s}", [(s*x, F(f), z) for (f, z) in prof for x in (x_in, xo(z))], M["Armor_Gunmetal"], y.U, bevel=0.006)
        hull(f"sp_appl{s}", [(s*(xo(z) + d), F(f), z) for (f, z) in ((0.090, 0.050), (0.090, 0.120), (-0.110, 0.140), (-0.130, 0.120), (-0.130, 0.050)) for d in (-0.003, 0.007)],
             M["Armor_Plate_Light"], y.U, bevel=0.002)
        hull(f"sp_team{s}", [(s*x, F(f), z) for (f, z) in ((0.050, 0.166), (0.050, 0.172), (-0.118, 0.176), (-0.118, 0.170)) for x in (x_in + 0.010, xo(0.170) - 0.004)],
             M["TeamColor_Red"], y.Pc)
        cyl(f"boss{s}", 0.050, (s*(x_in - 0.002), 0, P), (s*(xo(P) + 0.012), 0, P), M["Armor_Dark"], y.U, seg=24, bevel=0.004)
        tube(f"boss_glow{s}", 0.042, 0.033, (s*(xo(P) + 0.010), 0, P), (s*(xo(P) + 0.0145), 0, P), M["Energy_Hot"], y.Pc, seg=24)
        cyl(f"bore{s}", 0.021, (s*(x_in - 0.012), 0, P), (s*(x_in + 0.026), 0, P), M["Armor_Dark"], y.D, seg=20)
        for (f_, z_) in ((0.070, 0.062), (-0.100, 0.062), (0.040, 0.110), (-0.100, 0.118)):
            hexbolt(f"sp_bolt{s}", (s*(xo(z_) + 0.006), F(f_), z_), (s*(xo(z_) + 0.012), F(f_), z_), M["Steel_Light"], y.Pc, r=0.006)
        for k in range(3):   # track-like skirt teeth along the sponson foot
            f_ = 0.080 - 0.090*k
            box(f"sp_tooth{s}", s*(x_out - 0.004), s*(x_out + 0.010), F(f_ - 0.030), F(f_ + 0.030), 0.036, 0.058, M["Armor_Dark"], y.U, bevel=0.003)
        box(f"sp_vent{s}", s*(x_in + 0.006), s*(x_out + 0.004), F(-0.060), F(-0.020), 0.150, 0.172, M["Armor_Dark"], y.D)


def drum_trunnion(name, work, pt, half=0.150, r=0.088, axle=0.172):
    ob = cyl(name, r, (-half, 0, P), (half, 0, P), M["Armor_Dark"], work, seg=40, bevel=0.006)
    cyl("axle", 0.018, (-axle, 0, P), (axle, 0, P), M["Steel_Light"], pt.U, seg=16)
    for s in (-1, 1):
        cyl(f"drum_cap{s}", r - 0.016, (s*(half - 0.004), 0, P), (s*(half + 0.004), 0, P), M["Armor_Plate_Light"], pt.U, seg=40, bevel=0.002)
        tube(f"drum_team{s}", r + 0.0015, r - 0.010, (s*(half - 0.034), 0, P), (s*(half - 0.022), 0, P), M["TeamColor_Red"], pt.Pc, seg=40)
        for k in range(8):
            a = 2*math.pi*(k + 0.5)/8
            hexbolt(f"cap_bolt{s}", (s*(half + 0.003), 0.058*math.cos(a), P + 0.058*math.sin(a)), (s*(half + 0.008), 0.058*math.cos(a), P + 0.058*math.sin(a)), M["Steel_Light"], pt.Pc)
    return ob


# ---------------- Shotgun: single cantilever arm (+X) + side drum magazine on the pitch (-X) --------------------
def single_arm(y, s=1, x_in=0.122, x_out=0.172):
    cheek = [(0.135, 0.035), (0.175, 0.085), (0.080, 0.218), (-0.060, 0.226), (-0.140, 0.170), (-0.160, 0.035)]
    prism_x("arm", s*x_in, s*x_out, cheek, M["Armor_Gunmetal"], y.U, bevel=0.006)
    prism_x("arm_appl", s*(x_out - 0.0005), s*(x_out + 0.009), [(0.110, 0.060), (0.150, 0.094), (0.070, 0.198), (-0.048, 0.204), (-0.118, 0.160), (-0.130, 0.060)],
            M["Armor_Plate_Light"], y.U, bevel=0.003)
    prism_x("arm_team", s*(x_out + 0.0085), s*(x_out + 0.0130), [(-0.104, 0.170), (0.074, 0.170), (0.058, 0.186), (-0.090, 0.186)], M["TeamColor_Red"], y.Pc)
    cyl("arm_boss", 0.058, (s*(x_in - 0.004), 0, P), (s*(x_out + 0.020), 0, P), M["Armor_Dark"], y.U, seg=28, bevel=0.005)
    tube("arm_glow", 0.048, 0.036, (s*(x_out + 0.018), 0, P), (s*(x_out + 0.0225), 0, P), M["Energy_Hot"], y.Pc, seg=28)
    cyl("arm_bore", 0.021, (s*(x_in - 0.012), 0, P), (s*(x_out + 0.004), 0, P), M["Armor_Dark"], y.D, seg=20)
    # diagonal brace from the turntable to the arm (on the arm side only)
    hull("arm_brace", [(s*x, F(f), z) for x in (x_in + 0.006, x_out - 0.006) for (f, z) in ((0.150, 0.036), (0.190, 0.036), (0.150, 0.080))], M["Armor_Dark"], y.U, bevel=0.003)
    for (f_, z_) in ((0.092, 0.080), (-0.105, 0.068), (-0.020, 0.196), (0.060, 0.190)):
        hexbolt("arm_bolt", (s*(x_out + 0.007), F(f_), z_), (s*(x_out + 0.0125), F(f_), z_), M["Steel_Light"], y.Pc)
    for (f0, f1) in ((-0.110, -0.100), (-0.090, -0.080), (-0.070, -0.060)):
        box("arm_vent", s*(x_out + 0.002), s*(x_out + 0.020), F(f0), F(f1), 0.070, 0.140, M["Armor_Dark"], y.D)
        box("arm_vglow", s*(x_out + 0.0005), s*(x_out + 0.0040), F(f0 + 0.001), F(f1 - 0.001), 0.072, 0.138, M["Energy_Hot"], y.Pc)
    # low counter-bearing pedestal on the free side (does not reach the pitch drum)
    box("free_pedestal", -s*0.070, -s*0.150, F(-0.120), F(-0.010), 0.036, 0.052, M["Armor_Dark"], y.U, bevel=0.004)


def side_drum(p, s=-1, x0=0.110, x1=0.204, r=0.080):
    cyl("drum_mag", r, (s*x0, 0, P), (s*x1, 0, P), M["Armor_Gunmetal"], p.U, seg=40, bevel=0.006)
    cyl("drum_lid", r - 0.012, (s*(x1 - 0.002), 0, P), (s*(x1 + 0.006), 0, P), M["Armor_Plate_Light"], p.U, seg=40, bevel=0.002)
    cyl("drum_hub", 0.026, (s*(x1 + 0.004), 0, P), (s*(x1 + 0.014), 0, P), M["Armor_Dark"], p.U, seg=20, bevel=0.002)
    tube("drum_team", r + 0.0015, r - 0.010, (s*(x0 + 0.030), 0, P), (s*(x0 + 0.044), 0, P), M["TeamColor_Red"], p.Pc, seg=40)
    tube("drum_haz", r + 0.0015, r - 0.010, (s*(x1 - 0.018), 0, P), (s*(x1 - 0.012), 0, P), M["Hazard_Yellow"], p.Pc, seg=40)
    for k in range(10):   # shell-count window ribs on the lid face
        a = 2*math.pi*k/10
        hexbolt("lid_bolt", (s*(x1 + 0.005), 0.052*math.cos(a), P + 0.052*math.sin(a)), (s*(x1 + 0.010), 0.052*math.cos(a), P + 0.052*math.sin(a)), M["Steel_Light"], p.Pc)
    for k in range(6):
        a = 2*math.pi*(k + 0.5)/6
        box("drum_flute", s*(x0 + 0.050), s*(x1 - 0.024), -0.006, 0.006, P + r - 0.006, P + r + 0.004, M["Armor_Dark"], p.D).data.transform(
            Matrix.Translation((0, 0, P)) @ Matrix.Rotation(a, 4, 'X') @ Matrix.Translation((0, 0, -P)))


# ---------------- Railgun: thin tall fork + vertical capacitor spires ----------------------------------------------
def fork(y, x_in=0.112, x_out=0.126):
    prof = [(0.125, 0.035), (0.140, 0.080), (0.068, 0.248), (0.010, 0.270), (-0.070, 0.244), (-0.135, 0.110), (-0.135, 0.035)]
    for s in (-1, 1):
        prism_x(f"fork{s}", s*x_in, s*x_out, prof, M["Armor_Gunmetal"], y.U, bevel=0.004)
        prism_x(f"fork_appl{s}", s*(x_out - 0.0005), s*(x_out + 0.006), [(0.100, 0.060), (0.112, 0.090), (0.052, 0.226), (0.004, 0.244), (-0.050, 0.224), (-0.105, 0.110), (-0.105, 0.060)],
                M["Armor_Plate_Light"], y.U, bevel=0.002)
        prism_x(f"fork_team{s}", s*(x_out + 0.0055), s*(x_out + 0.0095), [(-0.034, 0.214), (0.040, 0.214), (0.026, 0.232), (-0.024, 0.236)], M["TeamColor_Red"], y.Pc)
        cyl(f"fork_boss{s}", 0.040, (s*(x_in - 0.004), 0, P), (s*(x_out + 0.018), 0, P), M["Armor_Dark"], y.U, seg=24, bevel=0.004)
        tube(f"fork_glow{s}", 0.032, 0.024, (s*(x_out + 0.016), 0, P), (s*(x_out + 0.0205), 0, P), M["Energy_Hot"], y.Pc, seg=24)
        cyl(f"fork_bore{s}", 0.021, (s*(x_in - 0.012), 0, P), (s*(x_out + 0.024), 0, P), M["Armor_Dark"], y.D, seg=20)
        for (f_, z_, r_) in ((0.020, 0.215, 0.020), (-0.070, 0.092, 0.018), (0.072, 0.090, 0.016)):   # lightening holes with hot rims
            cyl(f"fork_hole{s}", r_, (s*(x_in - 0.004), F(f_), z_), (s*(x_out + 0.010), F(f_), z_), M["Armor_Dark"], y.D, seg=20)
            tube(f"fork_hrim{s}", r_ + 0.004, r_ - 0.003, (s*(x_in + 0.004), F(f_), z_), (s*(x_in + 0.008), F(f_), z_), M["Energy_Hot"], y.Pc, seg=20)
        hexbolt(f"fork_bolt{s}", (s*(x_out + 0.005), F(-0.090), 0.150), (s*(x_out + 0.010), F(-0.090), 0.150), M["Steel_Light"], y.Pc)


def cap_spires(y, f=-0.196):
    box("cap_block", -0.100, 0.100, F(-0.150), F(-0.238), 0.035, 0.094, M["Armor_Dark"], y.U, bevel=0.008)
    box("cap_lid", -0.092, 0.092, F(-0.158), F(-0.230), 0.090, 0.0985, M["TeamColor_Red"], y.U, bevel=0.003)
    for xx in (-0.064, 0.0, 0.064):
        cyl("spire", 0.024, (xx, F(f), 0.094), (xx, F(f), 0.270), M["Armor_Gunmetal"], y.U, seg=20, bevel=0.003)
        tube("spire_band", 0.0255, 0.018, (xx, F(f), 0.150), (xx, F(f), 0.190), M["Armor_Plate_Light"], y.U, seg=20)
        for zz in (0.118, 0.214, 0.240):
            tube("spire_ring", 0.0285, 0.020, (xx, F(f), zz - 0.004), (xx, F(f), zz + 0.004), M["Steel_Light"], y.U, seg=20)
        cone("spire_tip", 0.020, 0.008, (xx, F(f), 0.269), (xx, F(f), 0.292), M["Energy_Hot"], y.U, seg=16)
        tube("spire_haz", 0.0255, 0.018, (xx, F(f), 0.100), (xx, F(f), 0.106), M["Hazard_Yellow"], y.Pc, seg=20)
    box("bus_bar", -0.076, 0.076, F(f - 0.006), F(f + 0.006), 0.226, 0.232, M["Steel_Light"], y.U)
    for xx in (-0.06, -0.02, 0.02, 0.06):
        box("blk_vent", xx - 0.008, xx + 0.008, F(-0.229), F(-0.245), 0.046, 0.082, M["Armor_Dark"], y.D)
        box("blk_glow", xx - 0.006, xx + 0.006, 0.2295, 0.2330, 0.049, 0.079, M["Energy_Hot"], y.Pc)


# ---------------- BeamCutter: ball head in a gimbal ring --------------------------------------------------------
def gimbal(y, x0=0.098, x1=0.112, r0=0.100, r1=0.120):
    for s in (-1, 1):
        a_, b_ = sorted((s*x0, s*x1))
        arc_plate(f"gimbal{s}", r0, r1, -35.0, 215.0, a_, b_, M["Armor_Gunmetal"], y.U, (0, 0, P), n=40)
        c_, d_ = sorted((s*(x1 - 0.0005), s*(x1 + 0.004)))
        arc_plate(f"gimbal_appl{s}", r0 + 0.002, r1 - 0.002, 125.0, 200.0, c_, d_, M["Armor_Plate_Light"], y.U, (0, 0, P), n=16)
        e_, g_ = sorted((s*(x1 - 0.0005), s*(x1 + 0.0035)))
        arc_plate(f"gimbal_team{s}", r0 + 0.004, r1 - 0.004, 50.0, 115.0, e_, g_, M["TeamColor_Red"], y.Pc, (0, 0, P), n=8)
        for fpost in (0.090, -0.090):
            box(f"gimbal_post{s}", s*x0, s*x1, F(fpost - 0.017), F(fpost + 0.017), 0.036, 0.080, M["Armor_Dark"], y.U, bevel=0.003)
        cyl(f"gimbal_hub{s}", 0.034, (s*(x0 + 0.002), 0, P), (s*(x1 + 0.020), 0, P), M["Armor_Dark"], y.U, seg=24, bevel=0.004)
        tube(f"hub_glow{s}", 0.027, 0.019, (s*(x1 + 0.018), 0, P), (s*(x1 + 0.0225), 0, P), M["Energy_Hot"], y.Pc, seg=24)
        for a in (math.radians(90), math.radians(-10), math.radians(190)):   # spokes hub -> ring (outer face)
            ca, sa = math.cos(a), math.sin(a)
            hull(f"spoke{s}", [(s*x, F(rr*ca) + w*sa, P + rr*sa + w*ca) for x in (x0 + 0.004, x1) for rr in (0.030, r0 + 0.004) for w in (-0.007, 0.007)], M["Armor_Dark"], y.U)
        for a in range(-20, 211, 46):
            ar = math.radians(a)
            hexbolt(f"ring_bolt{s}", (s*x1, F((r0 + r1)/2*math.cos(ar)), P + (r0 + r1)/2*math.sin(ar)), (s*(x1 + 0.005), F((r0 + r1)/2*math.cos(ar)), P + (r0 + r1)/2*math.sin(ar)),
                    M["Steel_Light"], y.Pc)


def ball(name, work, pt, r=0.090, axle=0.096):
    ob = sphere(name, r, (0, 0, P), M["Armor_Gunmetal"], work, n=18, seg=36)
    cyl("axle", 0.018, (-axle, 0, P), (axle, 0, P), M["Steel_Light"], pt.U, seg=16)
    for s in (-1, 1):
        cyl(f"ball_cap{s}", 0.052, (s*(r - 0.022), 0, P), (s*(r - 0.008), 0, P), M["Armor_Plate_Light"], pt.U, seg=32, bevel=0.003)
        for k in range(6):
            a = 2*math.pi*(k + 0.5)/6
            hexbolt(f"cap_bolt{s}", (s*(r - 0.009), 0.040*math.cos(a), P + 0.040*math.sin(a)), (s*(r - 0.004), 0.040*math.cos(a), P + 0.040*math.sin(a)), M["Steel_Light"], pt.Pc)
    t = tube("ball_team", r + 0.0015, r - 0.012, (-0.006, 0, P), (0.006, 0, P), M["TeamColor_Red"], pt.Pc, seg=36)
    box("ball_seam", -0.002, 0.002, -r - 0.01, r + 0.01, P - 0.004, P + 0.004, M["Armor_Dark"], pt.D)
    box("sensor", -0.024, 0.024, F(-0.012), F(0.052), P + 0.074, P + 0.104, M["Armor_Dark"], pt.U, bevel=0.004)
    box("sensor_eye", -0.016, 0.016, F(0.0515), F(0.0545), P + 0.080, P + 0.098, M["Sensor_Eye_Red"], pt.U)
    for k in range(3):
        f0 = -0.064 + 0.020*k
        box(f"top_slot{k}", -0.030, 0.030, F(f0 + 0.008), F(f0), P + 0.070, P + 0.100, M["Armor_Dark"], pt.D)
        box(f"top_glow{k}", -0.028, 0.028, F(f0 + 0.007), F(f0 + 0.001), P + 0.069, P + 0.072, M["Energy_Hot"], pt.Pc)
    return ob


# ---------------- Rocket: central tower with radar mast -----------------------------------------------------------
def tower(y, xw=0.064):
    box("tower", -xw, xw, F(-0.112), F(0.092), 0.035, 0.204, M["Armor_Gunmetal"], y.U, bevel=0.010)
    box("tower_front", -xw + 0.010, xw - 0.010, F(0.091), F(0.098), 0.060, 0.186, M["Armor_Plate_Light"], y.U, bevel=0.003)
    for s in (-1, 1):
        cyl(f"tw_boss{s}", 0.046, (s*(xw - 0.006), 0, P), (s*(xw + 0.012), 0, P), M["Armor_Dark"], y.U, seg=24, bevel=0.004)
        tube(f"tw_glow{s}", 0.038, 0.028, (s*(xw + 0.010), 0, P), (s*(xw + 0.0145), 0, P), M["Energy_Hot"], y.Pc, seg=24)
        box(f"tw_team{s}", s*(xw - 0.0005), s*(xw + 0.003), F(-0.096), F(-0.050), 0.060, 0.186, M["TeamColor_Red"], y.Pc)
        for (f_, z_) in ((0.060, 0.060), (0.060, 0.186), (-0.030, 0.186)):
            hexbolt(f"tw_bolt{s}", (s*(xw - 0.001), F(f_), z_), (s*(xw + 0.004), F(f_), z_), M["Steel_Light"], y.Pc)
    cyl("tw_bore", 0.021, (-(xw + 0.020), 0, P), (xw + 0.020, 0, P), M["Armor_Dark"], y.D, seg=20)
    cyl("mast", 0.012, (0, F(-0.040), 0.200), (0, F(-0.040), 0.262), M["Steel_Light"], y.U, seg=12)
    box("radar", -0.058, 0.058, F(-0.052), F(-0.030), 0.252, 0.290, M["Armor_Dark"], y.U, bevel=0.004)
    box("radar_face", -0.050, 0.050, F(-0.030), F(-0.026), 0.258, 0.284, M["Armor_Plate_Light"], y.U, bevel=0.0015)
    box("radar_eye", -0.020, 0.020, F(-0.0265), F(-0.0245), 0.266, 0.276, M["Sensor_Eye_Red"], y.U)
    box("radar_haz", -0.058, 0.058, F(-0.052), F(-0.030), 0.2895, 0.2915, M["Hazard_Yellow"], y.Pc)


def gun_shield(p, f0=0.206, t=0.024, xb=0.206, xt=0.176, z0=P - 0.068, z1=P + 0.086, lean=0.016):
    """wide sloped armoured gun shield in front of the sponsons (pitch part, bored by the barrel bore)"""
    def fz(z): return f0 + lean*(z1 - z)/(z1 - z0)
    def xw(z): return xb + (xt - xb)*(z - z0)/(z1 - z0)
    hull("shield", [(sx*xw(z), F(fz(z) + d), z) for z in (z0, z1) for sx in (-1, 1) for d in (0.0, t)], M["Armor_Gunmetal"], p.U, bevel=0.006)
    for sx in (-1, 1):
        hull(f"shield_appl{sx}", [(sx*x, F(fz(z) + t + d), z) for z in (z0 + 0.016, z1 - 0.030) for x in (0.090, xw(z) - 0.020) for d in (-0.003, 0.004)],
             M["Armor_Plate_Light"], p.U, bevel=0.002)
        hull(f"shield_team{sx}", [(sx*x, F(fz(z) + t + d), z) for z in (z1 - 0.024, z1 - 0.012) for x in (0.090, xw(z) - 0.022) for d in (-0.002, 0.0055)], M["TeamColor_Red"], p.Pc)
        for (x_, z_) in ((0.110, z0 + 0.026), (xb - 0.044, z0 + 0.026), (0.110, z1 - 0.040), (xt - 0.040, z1 - 0.040)):
            hexbolt(f"shield_bolt{sx}", (sx*x_, F(fz(z_) + t + 0.002), z_), (sx*x_, F(fz(z_) + t + 0.009), z_), M["Steel_Light"], p.Pc, r=0.006)
    for i in range(7):   # hazard chevrons along the lower edge
        x0 = -0.150 + i*0.044
        hull(f"shield_haz{i}", [(xx, F(fz(z0 + 0.008) + t + d), zz) for d in (-0.001, 0.003) for (xx, zz) in ((x0, z0 + 0.003), (x0 + 0.018, z0 + 0.003), (x0 + 0.030, z0 + 0.013), (x0 + 0.012, z0 + 0.013))],
             M["Hazard_Yellow"], p.Pc)
    box("vision_slit", 0.100, 0.150, F(fz(z1 - 0.050) - 0.01), F(fz(z1 - 0.050) + t + 0.02), z1 - 0.058, z1 - 0.046, M["Armor_Dark"], p.D)
    box("vision_eye", 0.102, 0.148, F(fz(z1 - 0.050) + 0.004), F(fz(z1 - 0.050) + 0.008), z1 - 0.056, z1 - 0.048, M["Sensor_Eye_Red"], p.Pc)
