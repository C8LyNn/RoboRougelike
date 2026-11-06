# R4WeaponKind.Railgun - "WPN-002 Railgun" - long rail pair through 5 accelerator coils, separate deployable emitter,
# capacitor bank on the yaw, charge -> shot -> cool cycle (visual only; game timing and penetration unchanged).
import sys, os, math; sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import importlib, bpy, r4lib as L, r4qa as Q; importlib.reload(L); importlib.reload(Q)
from r4lib import F, P, M, box, cyl, tube, hull, prism_x, hexbolt, Part, new_col
OUT = sys.argv[sys.argv.index("--") + 1]
L.reset_scene(); L.init_palette(); scn = bpy.context.scene
work = new_col("_build_tmp"); ID = "R4W_Railgun"
RECOIL = 0.040; RAIL_END = 0.745; MUZZLE_F = 0.840; DEPLOY = 0.025; DN, UP = 10.0, 35.0
COILS = [0.270, 0.360, 0.450, 0.540, 0.630]
base, _ = L.build_base(ID + "_Base", work, vents=5, hazard_corners=True, glacis_top=0.190)
yaw = L.turntable(ID + "_Yaw", work)
y = Part(work, "y")
L.yoke(y, 0.112, 0.152, [(0.150, 0.035), (0.200, 0.090), (0.062, 0.222), (-0.095, 0.230), (-0.160, 0.165), (-0.160, 0.035)],
       [(0.118, 0.060), (0.165, 0.098), (0.054, 0.200), (-0.074, 0.208), (-0.130, 0.155), (-0.130, 0.060)],
       [(-0.112, 0.166), (0.088, 0.166), (0.068, 0.184), (-0.096, 0.184)], bolts=((0.105, 0.082), (-0.118, 0.068), (-0.030, 0.196)))
box("cap_bank", -0.108, 0.108, F(-0.148), F(-0.236), 0.035, 0.150, M["Armor_Dark"], y.U, bevel=0.010)
box("cap_lid", -0.096, 0.096, F(-0.156), F(-0.228), 0.145, 0.1555, M["TeamColor_Red"], y.U, bevel=0.004)
for f_ in (-0.166, -0.186, -0.206, -0.226):
    cyl("cap_tube", 0.0090, (-0.092, F(f_), 0.1665), (0.092, F(f_), 0.1665), M["Energy_Hot"], y.U, seg=12)
    for xx in (-0.072, 0.0, 0.072):
        tube("cap_clamp", 0.0125, 0.0060, (xx - 0.005, F(f_), 0.1665), (xx + 0.005, F(f_), 0.1665), M["Steel_Light"], y.U, seg=8)
for s in (-1, 1):
    for k in range(5):
        zz = 0.050 + 0.020*k
        box(f"sink{s}{k}", s*0.106, s*0.118, F(-0.155), F(-0.230), zz, zz + 0.007, M["Steel_Light"], y.U, bevel=0.0015)
for xx in (-0.06, -0.02, 0.02, 0.06):
    box("bank_vent", xx - 0.008, xx + 0.008, F(-0.229), F(-0.245), 0.062, 0.124, M["Armor_Dark"], y.D)
    box("bank_glow", xx - 0.006, xx + 0.006, 0.2295, 0.2330, 0.065, 0.121, M["Energy_Hot"], y.Pc)
box("bank_haz", -0.100, 0.100, F(-0.2365), F(-0.2375), 0.037, 0.050, M["Hazard_Yellow"], y.Pc)
y.build(yaw)
# ---- pitch: breech housing with rail aperture, mantlet frame, vent-flap recesses, sensor
p = Part(work, "p")
pitch = L.trunnion(ID + "_Pitch", work, p, half=0.104, r=0.048, axle=0.140)
hull("housing", [(x, F(f), z) for (f, xw, z0, z1) in ((-0.090, 0.090, 0.092, 0.196), (0.090, 0.098, 0.084, 0.206), (0.170, 0.086, 0.090, 0.192))
                 for x in (-xw, xw) for z in (z0, z1)], M["Armor_Gunmetal"], p.U, bevel=0.006)
hull("mantlet", [(x, F(f), z) for (f, xw, z0, z1) in ((0.160, 0.092, 0.088, 0.206), (0.205, 0.080, 0.092, 0.200)) for x in (-xw, xw) for z in (z0, z1)],
     M["Armor_Dark"], p.U, bevel=0.005)
for s in (-1, 1):
    prism_x(f"hs_appl{s}", s*0.0975, s*0.1020, [(-0.070, 0.100), (0.130, 0.094), (0.150, 0.140), (0.110, 0.190), (-0.070, 0.184)], M["Armor_Plate_Light"], p.U, bevel=0.002)
    box(f"hs_team{s}", s*0.1015, s*0.1035, F(-0.055), F(0.110), 0.150, 0.162, M["TeamColor_Red"], p.Pc)
box("aperture", -0.0415, 0.0415, F(0.052), F(0.215), P - 0.0625, P + 0.0625, M["Armor_Dark"], p.D)
for s in (-1, 1):
    box(f"flap_recess{s}", s*0.006, s*0.086, F(-0.066), F(0.066), 0.192, 0.214, M["Armor_Dark"], p.D)
    box(f"flap_glow{s}", s*0.012, s*0.080, F(-0.060), F(0.060), 0.1925, 0.1945, M["Energy_Hot"], p.Pc)
box("sensor", -0.050, 0.050, F(0.168), F(0.120), 0.200, 0.222, M["Armor_Dark"], p.U, bevel=0.004)
box("sensor_eye", -0.040, 0.040, F(0.1695), F(0.1665), 0.205, 0.217, M["Sensor_Eye_Red"], p.U)
p.build(pitch)
# ---- rails carriage (recoils): carriage block, upper/lower rails with steel contact strips, 5 coil rings, team + hazard bands
rails = box(ID + "_Rails", -0.036, 0.036, F(0.100), F(0.210), P - 0.058, P + 0.058, M["Armor_Dark"], work, bevel=0.006)
r_ = Part(work, "r")
for s in (-1, 1):
    box(f"rail{s}", -0.020, 0.020, F(0.140), F(RAIL_END), P + s*0.030, P + s*0.054, M["Armor_Dark"], r_.U, bevel=0.003)
    box(f"strip{s}", -0.016, 0.016, F(0.150), F(RAIL_END - 0.004), P + s*0.0285, P + s*0.0315, M["Steel_Light"], r_.U)
    box(f"rail_tip{s}", -0.024, 0.024, F(RAIL_END - 0.030), F(RAIL_END + 0.004), P + s*0.026, P + s*0.058, M["Barrel_Heat"], r_.U, bevel=0.003)
for i, fc in enumerate(COILS):
    tube(f"coil{i}", 0.078, 0.0605, (0, F(fc - 0.016), P), (0, F(fc + 0.016), P), M["Armor_Gunmetal"], r_.U, seg=24)
    tube(f"coil_lip{i}", 0.081, 0.073, (0, F(fc - 0.004), P), (0, F(fc + 0.004), P), M["Armor_Plate_Light"], r_.U, seg=24)
    for s in (-1, 1):
        box(f"coil_spacer{i}{s}", -0.012, 0.012, F(fc - 0.014), F(fc + 0.014), P + s*0.052, P + s*0.0625, M["Armor_Dark"], r_.U)
        box(f"coil_side{i}{s}", s*0.0595, s*0.066, F(fc - 0.012), F(fc + 0.012), P - 0.010, P + 0.010, M["Armor_Dark"], r_.U)
    for k in range(6):
        a = 2*math.pi*(k + 0.5)/6
        hexbolt(f"coil_bolt{i}{k}", (0.069*math.cos(a), F(fc - 0.017), P + 0.069*math.sin(a)), (0.069*math.cos(a), F(fc - 0.021), P + 0.069*math.sin(a)), M["Steel_Light"], r_.Pc, r=0.0042)
tube("coil_team", 0.0785, 0.070, (0, F(COILS[2] - 0.012), P), (0, F(COILS[2] + 0.012), P), M["TeamColor_Red"], r_.Pc, seg=24)
tube("coil_haz", 0.0785, 0.070, (0, F(COILS[4] - 0.010), P), (0, F(COILS[4] + 0.010), P), M["Hazard_Yellow"], r_.Pc, seg=24)
r_.build(rails)
# ---- emitter (separate object, child of the rails): collimator ring with 4 field prongs, deploys forward when charging
emit = L.tube(ID + "_Emitter", 0.052, 0.034, (0, F(RAIL_END - 0.004), P), (0, F(RAIL_END + 0.040), P), M["Armor_Dark"], work, seg=24)
e_ = Part(work, "e")
for k in range(4):
    a = 2*math.pi*k/4 + math.pi/4
    hull(f"prong{k}", [(rr*math.cos(a) + d*math.cos(a + math.pi/2), F(ff), P + rr*math.sin(a) + d*math.sin(a + math.pi/2)) for rr in (0.040, 0.062) for d in (-0.006, 0.006) for ff in (RAIL_END + 0.002, RAIL_END + 0.034)]
         + [(0.050*math.cos(a), F(RAIL_END + 0.058), P + 0.050*math.sin(a))], M["Steel_Light"], e_.U)
tube("emit_glow", 0.0345, 0.0300, (0, F(RAIL_END + 0.030), P), (0, F(RAIL_END + 0.036), P), M["Energy_Hot"], e_.Pc, seg=24)
tube("emit_ring", 0.054, 0.047, (0, F(RAIL_END + 0.012), P), (0, F(RAIL_END + 0.020), P), M["Armor_Plate_Light"], e_.U, seg=24)
e_.build(emit)
# ---- vent flaps (hinged on their outer edge)
flaps = []
for s, tag in ((-1, "L"), (1, "R")):
    fl = box(f"{ID}_VentFlap_{tag}", s*0.010, s*0.082, F(-0.062), F(0.062), 0.195, 0.2035, M["Armor_Plate_Light"], work, bevel=0.002)
    g = Part(work, "f" + tag)
    for k in range(3):
        box(f"flap_rib{k}", s*0.016, s*0.076, F(-0.045 + 0.040*k), F(-0.037 + 0.040*k), 0.2030, 0.2060, M["Armor_Dark"], g.U)
    cyl("flap_hinge", 0.0042, (s*0.080, F(-0.064), 0.1995), (s*0.080, F(0.064), 0.1995), M["Steel_Light"], g.U, seg=10)
    g.build(fl); flaps.append(fl)
# ---- FX: coil glows, arc between rails, muzzle flash
glows = []
for i, fc in enumerate(COILS):
    g_ = L.tube(f"Rail_CoilGlow_{i + 1}", 0.0603, 0.0585, (0, F(fc - 0.012), P), (0, F(fc + 0.012), P), M["Energy_Hot"], work, seg=24)
    glows.append(g_)
arc = box("Rail_ArcFlash", -0.010, 0.010, F(0.150), F(RAIL_END), P - 0.0275, P + 0.0275, M["Muzzle_Flash"], work)
flash = L.build_flash("MuzzleFlash", work, work, length=0.260, radius=0.045, spikes=4)
col = L.gather(ID, [base, yaw, pitch, rails, emit, arc, flash] + flaps + glows)
root, pivot, socket = L.finish_rig(ID, col, base, yaw, pitch, pitch_range=(DN, UP))
scn.frame_set(1); bpy.context.view_layer.update()
L.attach(rails, pitch, (0, 0, P)); bpy.context.view_layer.update()
L.attach(emit, rails, (0, 0, P)); L.attach(arc, rails, (0, F(0.45), P))
for i, (g_, fc) in enumerate(zip(glows, COILS)): L.attach(g_, rails, (0, F(fc), P)); g_.scale = (0.001,)*3
arc.scale = (0.001,)*3
for fl, s in zip(flaps, (-1, 1)): L.attach(fl, pitch, (s*0.080, 0, 0.1995))
flash.parent = pitch; flash.location = (0, F(RAIL_END + 0.062), 0); flash.scale = (0.001,)*3
muzzle = L.empty("Muzzle", pitch, (0, F(MUZZLE_F), 0), col, 'SINGLE_ARROW', 0.06, L.UP90)
L.root_props(root, "R4WeaponKind.Railgun")
L.set_props({pivot: {"role": "game-driven pivot", "physical": "(0,0.135,0)"}, yaw: {"role": "Yaw: rotate around local Y only"},
             pitch: {"role": "Pitch: rotate around local X only", "pitch_range_deg": f"-{DN:.0f}..+{UP:.0f}"},
             rails: {"role": "rail carriage + 5 accelerator coils: recoils back along local -Z", "recoil_m": RECOIL},
             emit: {"role": "separate collimator emitter, child of rails: deploys forward along local +Z when charging", "deploy_m": DEPLOY},
             arc: {"role": "arc between the rails on the shot (FX, idle scale 0.001)"}, flash: {"role": "muzzle flash FX - idle scale 0.001"},
             socket: {"role": "mounting face centre, normal -Y, connects to cell (0,-1,0)"},
             muzzle: {"role": "projectile spawn", "physical": f"(0,0.135,{MUZZLE_F})"}})
for g_ in glows: g_["role"] = "coil charge glow FX: idle scale 0.001, scale 1 while charged"
for fl in flaps: fl["role"] = "cooling vent flap: rotates about local Z (outer hinge), opens 60 deg on Cool"
PARTS = [base, yaw, pitch, rails, emit] + flaps; FX = glows + [arc, flash]
for o in PARTS + FX: L.shade_and_uv(o)
T = L.Timeline()
rest = [(yaw, "rotation_euler", 2, 0.0), (pitch, "rotation_euler", 0, 0.0), (rails, "location", 1, 0.0), (emit, "location", 1, 0.0),
        (flaps[0], "rotation_euler", 1, 0.0), (flaps[1], "rotation_euler", 1, 0.0)]
L.hold(T, rest, "Idle", 24, "rest hold, loopable")
L.standard_aim(T, yaw, pitch, UP, DN)
def fx(o, f, sc): L.key3(o, "scale", f, (sc,)*3, 'CONSTANT')
s, e = T.clip("Charge", 24, desc="energy build-up: emitter deploys 25 mm, coils light rear -> front (cadence 2.4 s = Charge + Fire + Cool)")
for o in glows + [arc, flash]: fx(o, s - 1, 0.001); fx(o, s, 0.001)
L.key(emit, "location", 1, s, 0.0); L.key(emit, "location", 1, s + 8, -DEPLOY); L.key(emit, "location", 1, e, -DEPLOY)
for i, g_ in enumerate(glows): fx(g_, s + 6 + 4*i, 1.0)
T.clips[-1]["render_frames"] = [e - 1]; HERO = e - 1
s, e = T.clip("Fire", 6, desc="shot: arc + flash 2 f, coils dump, rails recoil 40 mm")
for g_ in glows: fx(g_, s, 1.0); fx(g_, s + 1, 0.001)
L.flash_keys(arc, s, ((0, 1.0), (1, 0.6), (2, 0.001))); L.flash_keys(flash, s, ((0, 1.0), (1, 0.7), (2, 0.001)))
L.key(rails, "location", 1, s, 0.0, 'LINEAR'); L.key(rails, "location", 1, s + 1, RECOIL, 'LINEAR'); L.key(rails, "location", 1, e, RECOIL*0.9, 'LINEAR')
L.key(emit, "location", 1, s, -DEPLOY); L.key(emit, "location", 1, e, -DEPLOY)
T.clips[-1]["render_frames"] = [s]; HERO2 = s
s, e = T.clip("Cool", 28, desc="rails run out, vent flaps open 60 deg, emitter retracts, flaps close")
L.key(rails, "location", 1, s, RECOIL*0.9); L.key(rails, "location", 1, s + 12, 0.0); L.key(rails, "location", 1, e, 0.0)
for fl, sg in zip(flaps, (-1, 1)):
    for df, d in ((0, 0.0), (6, 60.0), (20, 60.0), (28, 0.0)): L.key(fl, "rotation_euler", 1, s + df, math.radians(sg*d))
for df, v in ((0, -DEPLOY), (14, -DEPLOY), (22, 0.0)): L.key(emit, "location", 1, s + df, v)
L.key(emit, "location", 1, e, 0.0); T.clips[-1]["render_frames"] = [s + 10]
for o in FX: fx(o, T.end, 0.001)
for o, pth, idx, v in rest: L.key(o, pth, idx, T.end, v)
for o in [yaw, pitch, rails, emit, arc, flash] + flaps + glows: L.name_action(o, o.name + "_Master")
scn.frame_set(1)
S = dict(id=ID, kind="R4WeaponKind.Railgun", game_name="WPN-002 Railgun", out=OUT, root=root, pivot=pivot, yaw=yaw, pitch=pitch, socket=socket,
         export=[root, base, socket, pivot, yaw, pitch, rails, emit] + flaps + glows + [arc, flash, muzzle], parts=PARTS, fx=FX, muzzles=[muzzle],
         static=[base], yawparts=[yaw], gun=[pitch, rails, emit] + flaps, pitch_range=(DN, UP), frame_end=T.end, clips=T.clips,
         clash_pairs=[(rails, pitch), (emit, pitch), (rails, yaw), (emit, yaw), (rails, base), (flaps[0], pitch), (flaps[1], pitch), (flaps[0], yaw), (flaps[1], yaw),
                      (flaps[0], rails), (flaps[1], rails), (pitch, yaw), (pitch, base), (yaw, base)],
         hero_frame=HERO, hero_frame2=HERO2, tri_budget=22000, exits_note="1 emitter = 1 Muzzle (penetration up to 6 parts is gameplay, unchanged)",
         notes=["Charge glow is mesh FX (scale) so it survives FBX; emission strength itself is not animated."])
Q.run(S)
