# R4WeaponKind.Heavy - "Ciężka armata" - heavy base, thick barrel with bore evacuator, slow long recoil, sliding-wedge breech cycle.
import sys, os, math; sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import importlib, bpy, r4lib as L, r4qa as Q, r4base as BS, r4heads as H; importlib.reload(L); importlib.reload(Q); importlib.reload(BS); importlib.reload(H)
from r4lib import F, P, M, box, cyl, cone, tube, hull, prism_x, hexbolt, Part, new_col
OUT = sys.argv[sys.argv.index("--") + 1]
L.reset_scene(); L.init_palette(); scn = bpy.context.scene
work = new_col("_build_tmp"); ID = "R4W_Heavy"
BR = 0.040; RECOIL = 0.075; BARREL_END = 0.560; MUZZLE_F = 0.600; DN, UP = 8.0, 30.0; BDROP = 0.030
BASE_STYLE, HEAD = "stepped", "low sponsons + drum mantlet + wide sloped gun shield + stubby howitzer barrel"
base, _ = BS.build_base_style(ID + "_Base", work, BASE_STYLE)
# ---- yaw: wide tall cheeks with double applique, heavy rear counterweight with team lid
yaw = L.turntable(ID + "_Yaw", work, r=0.192)
y = Part(work, "y")
H.sponsons(y)
box("counterweight", -0.112, 0.112, F(-0.150), F(-0.238), 0.035, 0.172, M["Armor_Dark"], y.U, bevel=0.012)
box("cw_plate", -0.100, 0.100, F(-0.236), F(-0.2445), 0.050, 0.158, M["Armor_Plate_Light"], y.U, bevel=0.003)
box("cw_lid", -0.098, 0.098, F(-0.160), F(-0.228), 0.167, 0.180, M["TeamColor_Red"], y.U, bevel=0.004)
for xx in (-0.070, 0.0, 0.070):
    box("cw_lift", xx - 0.012, xx + 0.012, F(-0.180), F(-0.208), 0.179, 0.192, M["Armor_Dark"], y.U, bevel=0.003)
for xx in (-0.080, -0.040, 0.0, 0.040, 0.080):
    box("cw_slot", xx - 0.007, xx + 0.007, F(-0.2435), F(-0.252), 0.070, 0.138, M["Armor_Dark"], y.D)
    box("cw_glow", xx - 0.005, xx + 0.005, 0.2435, 0.2465, 0.073, 0.135, M["Energy_Hot"], y.Pc)
for (xx, zz) in ((-0.088, 0.064), (0.088, 0.064), (-0.088, 0.144), (0.088, 0.144)):
    hexbolt("cw_bolt", (xx, F(-0.244), zz), (xx, F(-0.2495), zz), M["Steel_Light"], y.Pc, r=0.006)
box("cw_haz", -0.100, 0.100, F(-0.2445), F(-0.2455), 0.038, 0.046, M["Hazard_Yellow"], y.Pc)
y.build(yaw)
# ---- pitch: heavy trunnion, massive receiver with breech slot open underneath, thick mantlet, cradle sleeve, rangefinder
p = Part(work, "p")
pitch = H.drum_trunnion(ID + "_Pitch", work, p, half=0.140, r=0.088, axle=0.172)
hull("receiver", [(x, F(f), z) for (f, xw, z0, z1) in ((-0.100, 0.100, 0.092, 0.212), (0.100, 0.108, 0.076, 0.222), (0.200, 0.092, 0.088, 0.184))
                  for x in (-xw, xw) for z in (z0, z1)], M["Armor_Gunmetal"], p.U, bevel=0.006)
hull("mantlet", [(x, F(f), z) for (f, xw, z0, z1) in ((0.150, 0.128, 0.072, 0.212), (0.212, 0.112, 0.082, 0.198)) for x in (-xw, xw) for z in (z0, z1)],
     M["Armor_Dark"], p.U, bevel=0.005)
for s in (-1, 1):
    prism_x(f"rcv_appl{s}", s*0.1075, s*0.1115, [(-0.080, 0.100), (0.140, 0.092), (0.170, 0.150), (0.120, 0.206), (-0.080, 0.200)], M["Armor_Plate_Light"], p.U, bevel=0.002)
    box(f"rcv_team{s}", s*0.1110, s*0.1130, F(-0.060), F(0.120), 0.160, 0.172, M["TeamColor_Red"], p.Pc)
    for f_ in (-0.060, 0.050, 0.140):
        hexbolt(f"rcv_bolt{s}", (s*0.1100, F(f_), 0.112), (s*0.1132, F(f_), 0.112), M["Steel_Light"], p.Pc, r=0.0055)
cyl("cradle", 0.062, (0, F(0.205), P), (0, F(0.335), P), M["Armor_Gunmetal"], p.U, seg=16, bevel=0.004)
tube("cradle_ring", 0.067, 0.050, (0, F(0.318), P), (0, F(0.336), P), M["Armor_Dark"], p.U, seg=16)
tube("cradle_team", 0.0635, 0.050, (0, F(0.255), P), (0, F(0.275), P), M["TeamColor_Red"], p.Pc, seg=16)
cyl("bore", 0.0475, (0, F(-0.030), P), (0, F(0.350), P), M["Armor_Dark"], p.D, seg=20)
box("breech_slot", -0.049, 0.049, F(-0.024), F(0.106), P - 0.090, P + 0.040, M["Armor_Dark"], p.D)
box("rangefinder", -0.092, -0.040, F(0.150), F(0.020), 0.214, 0.250, M["Armor_Dark"], p.U, bevel=0.005)
box("rf_eye", -0.084, -0.048, F(0.1515), F(0.1485), 0.222, 0.242, M["Sensor_Eye_Red"], p.U)
for k in range(4):
    f0 = -0.075 + 0.030*k
    box(f"top_slot{k}", -0.020, 0.085, F(f0 + 0.011), F(f0), 0.205, 0.228, M["Armor_Dark"], p.D)
    box(f"top_glow{k}", -0.018, 0.083, F(f0 + 0.010), F(f0 + 0.001), 0.204, 0.208, M["Energy_Hot"], p.Pc)
box("top_haz", 0.010, 0.080, F(0.090), F(0.035), 0.2180, 0.2235, M["Hazard_Yellow"], p.Pc)
for i in range(4):
    x0 = 0.010 + i*0.0175
    hull(f"top_chev{i}", [(xx, F(ff), zz) for zz in (0.2190, 0.2245) for (xx, ff) in ((x0, 0.0355), (x0 + 0.007, 0.0355), (x0 + 0.016, 0.0895), (x0 + 0.009, 0.0895))], M["Armor_Dark"], p.Pc)
H.gun_shield(p)
p.build(pitch)
# ---- barrel (recoils) with evacuator, thermal sleeves, double-baffle brake
barrel = cyl(ID + "_Barrel", BR, (0, F(0.098), P), (0, F(0.520), P), M["Armor_Dark"], work, seg=20)
b = Part(work, "b")
cone("evac_r", 0.041, 0.055, (0, F(0.415), P), (0, F(0.430), P), M["Armor_Gunmetal"], b.U, seg=20)
cyl("evac", 0.055, (0, F(0.429), P), (0, F(0.474), P), M["Armor_Gunmetal"], b.U, seg=20)
cone("evac_f", 0.055, 0.041, (0, F(0.473), P), (0, F(0.488), P), M["Armor_Gunmetal"], b.U, seg=20)
tube("evac_band", 0.0565, 0.050, (0, F(0.444), P), (0, F(0.458), P), M["Steel_Light"], b.U, seg=20)
for (f0, f1) in ((0.344, 0.374), (0.378, 0.408)):
    cyl("sleeve", 0.045, (0, F(f0), P), (0, F(f1), P), M["Armor_Plate_Light"], b.U, seg=8, bevel=0.003)
box("brake", -0.074, 0.074, F(0.492), F(BARREL_END), P - 0.056, P + 0.056, M["Barrel_Heat"], b.U, bevel=0.006)
cyl("mz_bore", 0.023, (0, F(0.420), P), (0, F(BARREL_END + 0.02), P), M["Armor_Dark"], b.D, seg=20)
tube("mz_groove", 0.034, 0.028, (0, F(BARREL_END - 0.006), P), (0, F(BARREL_END + 0.02), P), M["Armor_Dark"], b.D, seg=20)
for (p0, p1) in ((0.504, 0.522), (0.532, 0.550)):
    box("baffle", -0.084, 0.084, F(p0), F(p1), P - 0.022, P + 0.022, M["Armor_Dark"], b.D)
tube("mz_glow", 0.0335, 0.0285, (0, F(BARREL_END - 0.0065), P), (0, F(BARREL_END - 0.0012), P), M["Energy_Hot"], b.Pc, seg=20)
b.build(barrel)
breech = box(ID + "_Breech", -0.040, 0.040, F(0.060), F(0.099), P - 0.034, P + 0.034, M["Steel_Light"], work, bevel=0.004)
k_ = Part(work, "k")
box("wedge_face", -0.036, 0.036, F(0.059), F(0.064), P - 0.030, P + 0.030, M["Armor_Dark"], k_.U)
cyl("firing_pin", 0.010, (0, F(0.052), P), (0, F(0.062), P), M["Barrel_Heat"], k_.U, seg=12)
box("wedge_haz", -0.030, 0.030, F(0.0715), F(0.0875), P - 0.0345, P - 0.0335, M["Hazard_Yellow"], k_.Pc)
k_.build(breech)
flash = L.build_flash("MuzzleFlash", work, work, length=0.190, radius=0.058, spikes=6)
col = L.gather(ID, (base, yaw, pitch, barrel, breech, flash))
root, pivot, socket = L.finish_rig(ID, col, base, yaw, pitch, pitch_range=(DN, UP))
scn.frame_set(1); bpy.context.view_layer.update()
L.attach(barrel, pitch, (0, 0, P)); bpy.context.view_layer.update(); L.attach(breech, barrel, (0, 0, P))
flash.parent = pitch; flash.location = (0, F(BARREL_END + 0.005), 0); flash.scale = (0.001,)*3
muzzle = L.empty("Muzzle", pitch, (0, F(MUZZLE_F), 0), col, 'SINGLE_ARROW', 0.06, L.UP90)
L.root_props(root, "R4WeaponKind.Heavy", version="v2")
L.set_props({pivot: {"role": "game-driven pivot", "physical": "(0,0.135,0)"}, yaw: {"role": "Yaw: rotate around local Y only"},
             pitch: {"role": "Pitch: rotate around local X only", "pitch_range_deg": f"-{DN:.0f}..+{UP:.0f}"},
             barrel: {"role": "recoiling barrel: slides back along local -Z", "recoil_m": RECOIL},
             breech: {"role": "sliding-wedge breech block, child of barrel: drops along local -Y after recoil, rises to close", "drop_m": BDROP},
             flash: {"role": "muzzle flash FX - idle scale 0.001"}, socket: {"role": "mounting face centre, normal -Y, connects to cell (0,-1,0)"},
             muzzle: {"role": "projectile spawn", "physical": f"(0,0.135,{MUZZLE_F})"}})
PARTS = [base, yaw, pitch, barrel, breech]
for o in PARTS + [flash]: L.shade_and_uv(o)
T = L.Timeline()
rest = [(yaw, "rotation_euler", 2, 0.0), (pitch, "rotation_euler", 0, 0.0), (barrel, "location", 1, 0.0), (breech, "location", 2, 0.0)]
L.hold(T, rest, "Idle", 24, "rest hold, loopable")
L.standard_aim(T, yaw, pitch, UP, DN)
s, e = T.clip("Fire", 30, desc="one heavy shot @1.25 s cadence: flash, 75 mm recoil, slow run-out, breech drop + close")
L.key3(flash, "scale", s - 1, (0.001,)*3, 'CONSTANT'); L.flash_keys(flash, s, ((0, 1.0), (1, 0.8), (2, 0.4), (3, 0.001)))
for df, k in ((0, 0.0), (2, 1.0), (5, 0.96), (12, 0.45), (20, 0.06), (24, 0.0)): L.key(barrel, "location", 1, s + df, RECOIL*k, 'BEZIER' if df > 2 else 'LINEAR')
for df, v in ((0, 0.0), (6, 0.0), (9, -BDROP), (19, -BDROP), (23, 0.0)): L.key(breech, "location", 2, s + df, v, 'BEZIER')
L.key(barrel, "location", 1, e, 0.0); L.key(breech, "location", 2, e, 0.0)
T.clips[-1]["render_frames"] = [s + 1, s + 12]; HERO = s + 1
s, e = T.clip("Recoil", 4, desc="barrel kick 0 -> 75 mm (no flash)")
L.key(barrel, "location", 1, s, 0.0, 'LINEAR'); L.key(barrel, "location", 1, s + 2, RECOIL, 'LINEAR'); L.key(barrel, "location", 1, e, RECOIL, 'LINEAR')
L.key(breech, "location", 2, s, 0.0); L.key(breech, "location", 2, e, 0.0); T.clips[-1]["render_frames"] = [e]
s, e = T.clip("Return", 24, desc="breech cycle + slow run-out: breech drops (extract), barrel returns, breech rises (closed)")
L.key(barrel, "location", 1, s, RECOIL); L.key(barrel, "location", 1, s + 8, 0.5*RECOIL); L.key(barrel, "location", 1, s + 18, 0.0); L.key(barrel, "location", 1, e, 0.0)
for df, v in ((0, 0.0), (3, -BDROP), (15, -BDROP), (20, 0.0)): L.key(breech, "location", 2, s + df, v)
L.key(breech, "location", 2, e, 0.0); T.clips[-1]["render_frames"] = [s + 6]; HERO2 = s + 6
L.key3(flash, "scale", T.end, (0.001,)*3, 'CONSTANT')
for o, pth, idx, v in rest: L.key(o, pth, idx, T.end, v)
for o in (yaw, pitch, barrel, breech, flash): L.name_action(o, o.name + "_Master")
scn.frame_set(1)
S = dict(id=ID, kind="R4WeaponKind.Heavy", game_name="Ciężka armata", out=OUT, root=root, pivot=pivot, yaw=yaw, pitch=pitch, socket=socket,
         export=[root, base, socket, pivot, yaw, pitch, barrel, breech, flash, muzzle], parts=PARTS, fx=[flash], muzzles=[muzzle],
         static=[base], yawparts=[yaw], gun=[pitch, barrel, breech], pitch_range=(DN, UP), frame_end=T.end, clips=T.clips,
         clash_pairs=[(barrel, pitch), (breech, pitch), (barrel, yaw), (breech, yaw), (barrel, base), (pitch, yaw), (pitch, base), (yaw, base)],
         hero_frame=HERO, hero_frame2=HERO2, tri_budget=22000, exits_note="1 visible barrel = 1 Muzzle",
         notes=["Pitch range -8..+30 deg (heavier mass; R41 -10..+35). Runtime pitch limits are not changed by this asset."])
S["notes"] = list(S.get("notes", [])) + [f"v2 silhouette pass (#939 follow-up): base style {BASE_STYLE}, head {HEAD}; rig, pivot, muzzle contract and clips unchanged."]
S["base_style"], S["head_style"] = BASE_STYLE, HEAD
Q.run(S)
