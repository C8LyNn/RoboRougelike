# R4WeaponKind.Basic - "Działo podstawowe" - single recoiling barrel, ammo drum fed through the trunnion.
# blender -b --factory-startup --python build_basic.py -- <out_dir>
import sys, os, math; sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import importlib, r4lib as L, r4qa as Q; importlib.reload(L); importlib.reload(Q)
from r4lib import F, P, M, box, cyl, tube, hull, prism_x, revolve, hexbolt, Part, new_col
OUT = sys.argv[sys.argv.index("--") + 1]
L.reset_scene(); L.init_palette(); scn = __import__("bpy").context.scene
work = new_col("_build_tmp")
ID = "R4W_Basic"
BR = 0.028; RECOIL = 0.045; BARREL_END = 0.620; MUZZLE_F = 0.660; DN, UP = 10.0, 35.0
# ---- base: R41 family, plain variant
base, collar_top = L.build_base(ID + "_Base", work, vents=5)
# ---- yaw: turntable + compact cheek yoke + ammo drum feeding the left trunnion
yaw = L.turntable(ID + "_Yaw", work)
y = Part(work, "y")
CHEEK = [(0.140, 0.035), (0.190, 0.085), (0.062, 0.212), (-0.090, 0.220), (-0.150, 0.160), (-0.150, 0.035)]
APPL = [(0.112, 0.060), (0.158, 0.098), (0.052, 0.192), (-0.070, 0.198), (-0.122, 0.150), (-0.122, 0.060)]
for s in (-1, 1):
    prism_x(f"cheek{s}", s*0.108, s*0.150, CHEEK, M["Armor_Gunmetal"], y.U, bevel=0.005)
    prism_x(f"cheek_appl{s}", s*0.1495, s*0.159, APPL, M["Armor_Plate_Light"], y.U, bevel=0.003)
    prism_x(f"cheek_team{s}", s*0.1585, s*0.1630, [(-0.108, 0.158), (0.080, 0.158), (0.060, 0.176), (-0.090, 0.176)], M["TeamColor_Red"], y.Pc)
    cyl(f"boss{s}", 0.044, (s*0.104, 0, P), (s*0.166, 0, P), M["Armor_Dark"], y.U, seg=24, bevel=0.004)
    tube(f"boss_glow{s}", 0.036, 0.027, (s*0.164, 0, P), (s*0.1685, 0, P), M["Energy_Hot"], y.Pc, seg=24)
    cyl(f"bore{s}", 0.021, (s*0.098, 0, P), (s*0.152, 0, P), M["Armor_Dark"], y.D, seg=20)
    for (f0, f1) in ((-0.110, -0.100), (-0.090, -0.080)):
        box(f"vent{s}", s*0.152, s*0.170, F(f0), F(f1), 0.070, 0.140, M["Armor_Dark"], y.D)
        box(f"vent_glow{s}", s*0.1505, s*0.1540, F(f0 + 0.001), F(f1 - 0.001), 0.072, 0.138, M["Energy_Hot"], y.Pc)
    for (f_, z_) in ((0.100, 0.082), (-0.115, 0.068), (-0.020, 0.185)):
        hexbolt(f"appl_bolt{s}", (s*0.157, F(f_), z_), (s*0.1625, F(f_), z_), M["Steel_Light"], y.Pc)
# ammo drum (behind the cheeks), cradle and feed chute into the LEFT boss
DF, DZ, DR = -0.193, 0.106, 0.052
cyl("drum", DR, (-0.092, F(DF), DZ), (0.092, F(DF), DZ), M["Armor_Dark"], y.U, seg=32, bevel=0.005)
for s in (-1, 1):
    cyl(f"drum_cap{s}", DR + 0.004, (s*0.080, F(DF), DZ), (s*0.096, F(DF), DZ), M["Armor_Plate_Light"], y.U, seg=32, bevel=0.003)
    for i in range(6):
        a = 2*math.pi*(i + 0.5)/6
        hexbolt(f"cap_bolt{s}{i}", (s*0.095, F(DF) + 0.038*math.cos(a), DZ + 0.038*math.sin(a)), (s*0.100, F(DF) + 0.038*math.cos(a), DZ + 0.038*math.sin(a)), M["Steel_Light"], y.Pc)
tube("drum_team", DR + 0.0025, DR - 0.004, (-0.020, F(DF), DZ), (0.020, F(DF), DZ), M["TeamColor_Red"], y.Pc, seg=32)
box("drum_cradle", -0.100, 0.100, F(DF - 0.046), F(DF + 0.050), 0.035, DZ - 0.030, M["Armor_Gunmetal"], y.U, bevel=0.006)
hull("feed_chute", [(x, F(f), z) for (x, f, z) in ((-0.150, -0.150, 0.122), (-0.150, -0.150, 0.152), (-0.120, -0.150, 0.122), (-0.120, -0.150, 0.152),
                                                    (-0.150, -0.045, 0.118), (-0.150, -0.045, 0.150), (-0.120, -0.045, 0.118), (-0.120, -0.045, 0.150))],
     M["Armor_Gunmetal"], y.U, bevel=0.003)
for f_ in (-0.130, -0.100, -0.070):
    box("chute_link", -0.1525, -0.1175, F(f_ + 0.004), F(f_ - 0.004), 0.153, 0.157, M["Steel_Light"], y.Pc)
box("haz_plate", -0.060, 0.060, F(-0.2555), F(-0.2565), 0.045, 0.072, M["Hazard_Yellow"], y.Pc)
for i in range(5):
    x0 = -0.060 + i*0.024
    hull("haz_chev", [(xx, F(ff), zz) for ff in (-0.2550, -0.2570) for (xx, zz) in ((x0, 0.045), (x0 + 0.010, 0.045), (x0 + 0.022, 0.072), (x0 + 0.012, 0.072))], M["Armor_Dark"], y.Pc)
for s in (-1, 1):
    prism_x(f"blade{s}", s*0.070, s*0.077, [(-0.150, 0.035), (-0.255, 0.035), (-0.255, 0.070), (-0.185, 0.165), (-0.160, 0.165)], M["Armor_Dark"], y.U)
y.build(yaw)
# ---- pitch: trunnion drum, wedge receiver, mantlet, sight block, barrel shroud; slide bore for the barrel
pitch = cyl(ID + "_Pitch", 0.048, (-0.100, 0, P), (0.100, 0, P), M["Armor_Dark"], work, seg=32, bevel=0.004)
p = Part(work, "p")
cyl("axle", 0.018, (-0.140, 0, P), (0.140, 0, P), M["Steel_Light"], p.U, seg=16)
hull("receiver", [(x, F(f), z) for (f, xw, z0, z1) in ((-0.078, 0.088, 0.090, 0.196), (0.090, 0.094, 0.086, 0.205), (0.195, 0.078, 0.096, 0.170))
                  for x in (-xw, xw) for z in (z0, z1)], M["Armor_Gunmetal"], p.U, bevel=0.005)
hull("mantlet", [(x, F(f), z) for (f, xw, z0, z1) in ((0.165, 0.086, 0.092, 0.178), (0.205, 0.070, 0.100, 0.168)) for x in (-xw, xw) for z in (z0, z1)],
     M["Armor_Dark"], p.U, bevel=0.004)
for s in (-1, 1):
    prism_x(f"rcv_appl{s}", s*0.0935, s*0.0990, [(-0.060, 0.100), (0.150, 0.100), (0.170, 0.150), (0.120, 0.192), (-0.060, 0.186)], M["Armor_Plate_Light"], p.U, bevel=0.002)
    box(f"rcv_team{s}", s*0.0985, s*0.1010, F(-0.045), F(0.120), 0.150, 0.162, M["TeamColor_Red"], p.Pc)
    for f_ in (-0.040, 0.130):
        hexbolt(f"rcv_bolt{s}", (s*0.098, F(f_), 0.112), (s*0.1025, F(f_), 0.112), M["Steel_Light"], p.Pc)
# shroud (static part of the pitch body) with cooling slots
cyl("shroud", 0.044, (0, F(0.195), P), (0, F(0.400), P), M["Armor_Plate_Light"], p.U, seg=12, bevel=0.003)
tube("shroud_ring_f", 0.049, 0.030, (0, F(0.385), P), (0, F(0.402), P), M["Armor_Dark"], p.U, seg=12)
tube("shroud_ring_r", 0.050, 0.030, (0, F(0.200), P), (0, F(0.222), P), M["Armor_Dark"], p.U, seg=12)
for k in range(3):
    f0 = 0.240 + k*0.045
    for s in (-1, 1):
        box(f"slot{k}{s}", s*0.030, s*0.050, F(f0), F(f0 + 0.028), P - 0.008, P + 0.008, M["Armor_Dark"], p.D)
        box(f"slot_glow{k}{s}", s*0.0315, s*0.0345, F(f0 + 0.002), F(f0 + 0.026), P - 0.006, P + 0.006, M["Energy_Hot"], p.Pc)
cyl("slide_bore", 0.0325, (0, F(0.040), P), (0, F(0.420), P), M["Armor_Dark"], p.D, seg=16)
# sight block with red sensor eye (top left, as R41 visor)
box("sight", -0.080, -0.030, F(0.140), F(0.040), 0.200, 0.236, M["Armor_Dark"], p.U, bevel=0.005)
box("sight_eye", -0.072, -0.038, F(0.1415), F(0.1385), 0.208, 0.228, M["Sensor_Eye_Red"], p.U)
cyl("sight_lens", 0.006, (0.020, F(0.172), 0.186), (0.020, F(0.178), 0.186), M["Sensor_Eye_Red"], p.U, seg=12)
box("lens_hood", 0.008, 0.032, F(0.150), F(0.176), 0.176, 0.198, M["Armor_Dark"], p.U, bevel=0.003)
for k in range(3):
    f0 = -0.050 + 0.030*k
    box(f"top_slot{k}", -0.020, 0.075, F(f0 + 0.010), F(f0), 0.192, 0.215, M["Armor_Dark"], p.D)
    box(f"top_glow{k}", -0.018, 0.073, F(f0 + 0.009), F(f0 + 0.001), 0.191, 0.195, M["Energy_Hot"], p.Pc)
box("top_haz", 0.010, 0.070, F(0.150), F(0.110), 0.2005, 0.2015, M["Hazard_Yellow"], p.Pc)
p.build(pitch)
# ---- recoiling barrel (separate mesh, slides back along local -Z / Blender +Y)
barrel = cyl(ID + "_Barrel", BR, (0, F(0.100), P), (0, F(0.560), P), M["Armor_Dark"], work, seg=16)
b = Part(work, "b")
for f_ in (0.470, 0.486, 0.502, 0.518):
    cyl("fin", 0.037, (0, F(f_), P), (0, F(f_ + 0.007), P), M["Steel_Light"], b.U, seg=16)
tube("breech_ring", 0.031, 0.020, (0, F(0.100), P), (0, F(0.120), P), M["Steel_Light"], b.U, seg=16)
box("brake", -0.040, 0.040, F(0.552), F(BARREL_END), P - 0.031, P + 0.031, M["Barrel_Heat"], b.U, bevel=0.005)
tube("mz_groove", 0.025, 0.020, (0, F(BARREL_END - 0.005), P), (0, F(BARREL_END + 0.02), P), M["Armor_Dark"], b.D, seg=16)
cyl("bore", 0.016, (0, F(0.480), P), (0, F(BARREL_END + 0.02), P), M["Armor_Dark"], b.D, seg=16)
for (p0, p1) in ((0.566, 0.580), (0.590, 0.604)):
    box("baffle", -0.048, 0.048, F(p0), F(p1), P - 0.015, P + 0.015, M["Armor_Dark"], b.D)
tube("mz_glow", 0.0245, 0.0205, (0, F(BARREL_END - 0.0055), P), (0, F(BARREL_END - 0.0010), P), M["Energy_Hot"], b.Pc, seg=16)
b.build(barrel)
flash = L.build_flash("MuzzleFlash", work, work, length=0.150, radius=0.040)
# ---- hierarchy
col = new_col("R4W_Basic")
for o in (base, yaw, pitch, barrel, flash):
    for c_ in o.users_collection: c_.objects.unlink(o)
    col.objects.link(o)
__import__("bpy").data.collections.remove(work)
root, pivot, socket = L.finish_rig(ID, col, base, yaw, pitch, pitch_range=(DN, UP))
scn.frame_set(1); __import__("bpy").context.view_layer.update()
L.attach(barrel, pitch, (0, 0, P))
flash.parent = pitch; flash.location = (0, F(BARREL_END + 0.004), 0); flash.scale = (0.001,)*3
muzzle = L.empty("Muzzle", pitch, (0, F(MUZZLE_F), 0), col, 'SINGLE_ARROW', 0.05, L.UP90)
L.set_props({root: {"unity_up": "+Y", "unity_forward": "+Z", "unit": "meter", "version": "v1", "weapon_kind": "R4WeaponKind.Basic",
                    "style": "R41_Turret_v6.1 standard (heavy industrial armour + team colour)", "issue": "#938"},
             pivot: {"role": "game-driven pivot", "physical": "(0,0.135,0)"},
             yaw: {"role": "Yaw: rotate around local Y only"}, pitch: {"role": "Pitch: rotate around local X only", "pitch_range_deg": f"-{DN:.0f}..+{UP:.0f}"},
             barrel: {"role": "recoiling barrel: slides back along local -Z", "recoil_m": RECOIL},
             flash: {"role": "muzzle flash FX - idle scale 0.001, scale 1 on shot"},
             socket: {"role": "mounting face centre, normal -Y, connects to cell (0,-1,0)"},
             muzzle: {"role": "projectile spawn", "physical": f"(0,0.135,{MUZZLE_F})"}})
PARTS = [base, yaw, pitch, barrel]
for o in PARTS + [flash]: L.shade_and_uv(o)
# ---- animation (master timeline, 24 fps)
T = L.Timeline()
rest = [(yaw, "rotation_euler", 2, 0.0), (pitch, "rotation_euler", 0, 0.0), (barrel, "location", 1, 0.0)]
s, e = L.hold(T, rest, "Idle", 24, "rest hold, loopable")
L.standard_aim(T, yaw, pitch, UP, DN)
def shot(s0):
    for df, dv in ((0, 0.0), (1, RECOIL), (3, 0.62*RECOIL), (6, 0.15*RECOIL), (9, 0.0)): L.key(barrel, "location", 1, s0 + df, dv, 'LINEAR')
    for df, sc in ((0, 1.0), (1, 0.65), (2, 0.001)): L.key3(flash, "scale", s0 + df, (sc,)*3, 'CONSTANT')
s, e = T.clip("Fire", 9, desc="one shot @0.38 s cadence: flash 2 f, recoil 45 mm, return")
L.key3(flash, "scale", s - 1, (0.001,)*3, 'CONSTANT'); shot(s); T.clips[-1]["render_frames"] = [s + 1]
s, e = T.clip("Recoil", 3, desc="barrel kick back 0 -> 45 mm (no flash)")
L.key(barrel, "location", 1, s, 0.0, 'LINEAR'); L.key(barrel, "location", 1, s + 1, RECOIL, 'LINEAR'); L.key(barrel, "location", 1, e, RECOIL, 'LINEAR')
T.clips[-1]["render_frames"] = [e]
s, e = T.clip("Return", 8, desc="barrel run-out 45 mm -> 0")
L.key(barrel, "location", 1, s, RECOIL, 'LINEAR'); L.key(barrel, "location", 1, s + 3, 0.5*RECOIL, 'BEZIER'); L.key(barrel, "location", 1, e, 0.0, 'BEZIER')
L.key3(flash, "scale", T.end, (0.001,)*3, 'CONSTANT')
for o, pth, idx, v in rest: L.key(o, pth, idx, T.end, v)
L.name_action(yaw, ID + "_Yaw_Master"); L.name_action(pitch, ID + "_Pitch_Master"); L.name_action(barrel, ID + "_Barrel_Master"); L.name_action(flash, ID + "_Flash_Master")
scn.frame_set(1)
S = dict(id=ID, kind="R4WeaponKind.Basic", game_name="Działo podstawowe", out=OUT, root=root, pivot=pivot, yaw=yaw, pitch=pitch, socket=socket,
         export=[root, base, socket, pivot, yaw, pitch, barrel, flash, muzzle], parts=PARTS, fx=[flash], muzzles=[muzzle],
         static=[base], yawparts=[yaw], gun=[pitch, barrel], pitch_range=(DN, UP), frame_end=T.end, clips=T.clips,
         clash_pairs=[(barrel, pitch), (barrel, yaw), (barrel, base), (pitch, yaw), (pitch, base), (yaw, base)],
         hero_frame=[c for c in T.clips if c["name"] == "Fire"][0]["start"], tri_budget=16000,
         exits_note="1 visible barrel = 1 Muzzle (gameplay shot count unchanged)",
         notes=["Visual muzzle 0.660 m forward of the pivot (R41 v6.1: 0.700). Runtime placeholder positions are not changed."])
Q.run(S)
