# R4WeaponKind.Shotgun - "WPN-001 Shotgun" - short wide recoiling 6-bore block, pump fore-end, top loading gate.
# 6 visible bores are a visual choke block; gameplay keeps 8 pellets from one logical Muzzle.
import sys, os, math; sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import importlib, bpy, r4lib as L, r4qa as Q; importlib.reload(L); importlib.reload(Q)
from mathutils import Vector
from r4lib import F, P, M, box, cyl, tube, hull, prism_x, hexbolt, Part, new_col
OUT = sys.argv[sys.argv.index("--") + 1]
L.reset_scene(); L.init_palette(); scn = bpy.context.scene
work = new_col("_build_tmp"); ID = "R4W_Shotgun"
RECOIL = 0.030; PUMP = 0.060; FRONT = 0.410; MUZZLE_F = 0.450; DN, UP = 10.0, 35.0
BORES = [(x, z) for x in (-0.050, 0.0, 0.050) for z in (P - 0.022, P + 0.022)]
base, _ = L.build_base(ID + "_Base", work, vents=5, glacis_top=0.190)
yaw = L.turntable(ID + "_Yaw", work)
y = Part(work, "y")
L.yoke(y, 0.122, 0.162, [(0.140, 0.035), (0.180, 0.080), (0.070, 0.196), (-0.085, 0.200), (-0.150, 0.150), (-0.150, 0.035)],
       [(0.112, 0.058), (0.152, 0.092), (0.060, 0.178), (-0.066, 0.182), (-0.124, 0.140), (-0.124, 0.058)],
       [(-0.100, 0.143), (0.082, 0.143), (0.066, 0.158), (-0.086, 0.158)], bolts=((0.098, 0.078), (-0.112, 0.066)))
# rear shell magazine with brass shell heads in a window, team lid
box("magazine", -0.104, 0.104, F(-0.148), F(-0.236), 0.035, 0.152, M["Armor_Dark"], y.U, bevel=0.010)
box("mag_lid", -0.094, 0.094, F(-0.156), F(-0.226), 0.147, 0.160, M["TeamColor_Red"], y.U, bevel=0.004)
box("mag_window", -0.080, 0.080, F(-0.226), F(-0.245), 0.066, 0.106, M["Armor_Dark"], y.D)
for i in range(6):
    xx = -0.065 + i*0.026
    cyl(f"shell{i}", 0.0115, (xx, F(-0.200), 0.086), (xx, F(-0.233), 0.086), M["TeamColor_Red"], y.U, seg=16)
    cyl(f"shell_head{i}", 0.0125, (xx, F(-0.226), 0.086), (xx, F(-0.234), 0.086), M["Hazard_Yellow"], y.U, seg=16)
    cyl(f"primer{i}", 0.0040, (xx, F(-0.2335), 0.086), (xx, F(-0.2350), 0.086), M["Steel_Light"], y.Pc, seg=8)
for xx in (-0.080, 0.080):
    for zz in (0.056, 0.126):
        hexbolt("mag_bolt", (xx, F(-0.235), zz), (xx, F(-0.2405), zz), M["Steel_Light"], y.Pc, r=0.0055)
box("mag_strap", -0.104, 0.104, F(-0.190), F(-0.206), 0.034, 0.1535, M["Armor_Gunmetal"], y.U, bevel=0.003)
y.build(yaw)
# ---- pitch: wide receiver with a cradle socket for the recoiling block, top loading-gate recess, sensor
p = Part(work, "p")
pitch = L.trunnion(ID + "_Pitch", work, p, half=0.116, r=0.050, axle=0.146)
hull("receiver", [(x, F(f), z) for (f, xw, z0, z1) in ((-0.070, 0.106, 0.094, 0.190), (0.100, 0.112, 0.086, 0.200), (0.172, 0.100, 0.092, 0.184))
                  for x in (-xw, xw) for z in (z0, z1)], M["Armor_Gunmetal"], p.U, bevel=0.006)
hull("mantlet", [(x, F(f), z) for (f, xw, z0, z1) in ((0.150, 0.110, 0.084, 0.190), (0.196, 0.100, 0.090, 0.182)) for x in (-xw, xw) for z in (z0, z1)],
     M["Armor_Dark"], p.U, bevel=0.005)
for s in (-1, 1):
    prism_x(f"rcv_appl{s}", s*0.1115, s*0.1160, [(-0.050, 0.100), (0.130, 0.094), (0.150, 0.140), (0.110, 0.192), (-0.050, 0.186)], M["Armor_Plate_Light"], p.U, bevel=0.002)
    box(f"rcv_team{s}", s*0.1155, s*0.1175, F(-0.035), F(0.110), 0.148, 0.160, M["TeamColor_Red"], p.Pc)
box("cradle_socket", -0.0915, 0.0915, F(0.112), F(0.210), P - 0.0475, P + 0.0475, M["Armor_Dark"], p.D)
box("pump_channel", -0.0640, 0.0640, F(0.120), F(0.212), P - 0.080, P - 0.0405, M["Armor_Dark"], p.D)
box("gate_recess", 0.016, 0.097, F(-0.004), F(0.094), 0.192, 0.215, M["Armor_Dark"], p.D)
box("sensor", -0.092, -0.050, F(0.150), F(0.060), 0.196, 0.226, M["Armor_Dark"], p.U, bevel=0.004)
box("sensor_eye", -0.086, -0.056, F(0.1515), F(0.1485), 0.203, 0.220, M["Sensor_Eye_Red"], p.U)
for k in range(3):
    f0 = -0.058 + 0.024*k
    box(f"top_slot{k}", -0.090, -0.010, F(f0 + 0.009), F(f0), 0.186, 0.210, M["Armor_Dark"], p.D)
    box(f"top_glow{k}", -0.088, -0.012, F(f0 + 0.008), F(f0 + 0.001), 0.185, 0.189, M["Energy_Hot"], p.Pc)
p.build(pitch)
# ---- recoiling 6-bore block with heat shield and choke plate
blk = box(ID + "_Barrels", -0.082, 0.082, F(0.150), F(0.384), P - 0.044, P + 0.044, M["Armor_Dark"], work, bevel=0.008)
b = Part(work, "b")
box("choke", -0.090, 0.090, F(0.380), F(FRONT), P - 0.050, P + 0.050, M["Barrel_Heat"], b.U, bevel=0.006)
box("shield", -0.076, 0.076, F(0.236), F(0.372), P + 0.042, P + 0.056, M["Armor_Plate_Light"], b.U, bevel=0.004)
for k in range(5):
    f0 = 0.246 + 0.024*k
    box(f"sh_slot{k}", -0.060, 0.060, F(f0), F(f0 + 0.012), P + 0.049, P + 0.062, M["Armor_Dark"], b.D)
    box(f"sh_glow{k}", -0.058, 0.058, F(f0 + 0.001), F(f0 + 0.011), P + 0.0485, P + 0.0505, M["Energy_Hot"], b.Pc)
for s in (-1, 1):
    for k in range(4):
        f0 = 0.242 + 0.031*k
        box(f"rib{s}{k}", s*0.081, s*0.089, F(f0), F(f0 + 0.012), P - 0.034, P + 0.034, M["Armor_Gunmetal"], b.U, bevel=0.002)
    box(f"blk_team{s}", s*0.0885, s*0.0905, F(0.228), F(0.346), P - 0.006, P + 0.006, M["TeamColor_Red"], b.Pc)
for (x, z) in BORES:
    cyl("bore", 0.0125, (x, F(0.320), z), (x, F(FRONT + 0.02), z), M["Armor_Dark"], b.D, seg=16)
    tube("bore_glow", 0.0122, 0.0095, (x, F(0.336), z), (x, F(0.340), z), M["Energy_Hot"], b.Pc, seg=16)
    tube("bore_lip", 0.0170, 0.0124, (x, F(FRONT - 0.0005), z), (x, F(FRONT + 0.003), z), M["Steel_Light"], b.Pc, seg=16)
box("choke_haz", -0.084, 0.084, F(0.382), F(0.406), P + 0.0495, P + 0.0510, M["Hazard_Yellow"], b.Pc)
for i in range(6):
    x0 = -0.084 + i*0.028
    hull(f"choke_chev{i}", [(xx, F(ff), zz) for zz in (P + 0.0500, P + 0.0515) for (xx, ff) in ((x0, 0.3825), (x0 + 0.012, 0.3825), (x0 + 0.026, 0.4055), (x0 + 0.014, 0.4055))], M["Armor_Dark"], b.Pc)
b.build(blk)
# ---- pump fore-end (child of the block) with slide rods running under the receiver
pump = box(ID + "_Pump", -0.060, 0.060, F(0.220), F(0.330), P - 0.072, P - 0.0465, M["Armor_Gunmetal"], work, bevel=0.005)
u = Part(work, "u")
for k in range(5):
    f0 = 0.236 + 0.018*k
    box(f"grip{k}", -0.064, 0.064, F(f0), F(f0 + 0.007), P - 0.076, P - 0.068, M["Armor_Dark"], u.D)
for s in (-1, 1):
    cyl(f"rod{s}", 0.0065, (s*0.040, F(0.196), P - 0.051), (s*0.040, F(0.222), P - 0.051), M["Steel_Light"], u.U, seg=10)
box("pump_team", -0.030, 0.030, F(0.320), F(0.3305), P - 0.064, P - 0.055, M["TeamColor_Red"], u.Pc)
u.build(pump)
# ---- top loading gate (hinged on its +X edge, opens upward/outward)
gate = box(ID + "_LoadingGate", 0.020, 0.092, F(0.0), F(0.090), 0.196, 0.2045, M["Armor_Plate_Light"], work, bevel=0.002)
g = Part(work, "g")
box("gate_haz", 0.026, 0.086, F(0.010), F(0.080), 0.2040, 0.2055, M["Hazard_Yellow"], g.Pc)
for i in range(4):
    x0 = 0.026 + i*0.015
    hull(f"gate_chev{i}", [(xx, F(ff), zz) for zz in (0.2045, 0.2060) for (xx, ff) in ((x0, 0.0105), (x0 + 0.006, 0.0105), (x0 + 0.014, 0.0795), (x0 + 0.008, 0.0795))], M["Armor_Dark"], g.Pc)
cyl("gate_hinge", 0.0045, (0.090, F(-0.002), 0.200), (0.090, F(0.092), 0.200), M["Steel_Light"], g.U, seg=10)
g.build(gate)
flash = L.build_flash("MuzzleFlash", work, work, length=0.150, radius=0.075, spikes=6)
col = L.gather(ID, (base, yaw, pitch, blk, pump, gate, flash))
root, pivot, socket = L.finish_rig(ID, col, base, yaw, pitch, pitch_range=(DN, UP))
scn.frame_set(1); bpy.context.view_layer.update()
L.attach(blk, pitch, (0, 0, P)); bpy.context.view_layer.update(); L.attach(pump, blk, (0, 0, P))
L.attach(gate, pitch, (0.090, F(0.045), 0.200))
flash.parent = pitch; flash.location = (0, F(FRONT + 0.004), 0); flash.scale = (0.001,)*3
muzzle = L.empty("Muzzle", pitch, (0, F(MUZZLE_F), 0), col, 'SINGLE_ARROW', 0.06, L.UP90)
L.root_props(root, "R4WeaponKind.Shotgun")
L.set_props({pivot: {"role": "game-driven pivot", "physical": "(0,0.135,0)"}, yaw: {"role": "Yaw: rotate around local Y only"},
             pitch: {"role": "Pitch: rotate around local X only", "pitch_range_deg": f"-{DN:.0f}..+{UP:.0f}"},
             blk: {"role": "recoiling 6-bore choke block: slides back along local -Z", "recoil_m": RECOIL, "visible_bores": 6},
             pump: {"role": "pump fore-end, child of block: slides back along local -Z and returns", "stroke_m": PUMP},
             gate: {"role": "top loading gate: rotates about local Z (hinge on its +X edge), opens to 100 deg"},
             flash: {"role": "salvo flash FX - idle scale 0.001"}, socket: {"role": "mounting face centre, normal -Y, connects to cell (0,-1,0)"},
             muzzle: {"role": "single logical salvo origin (8 pellets in gameplay)", "physical": f"(0,0.135,{MUZZLE_F})"}})
PARTS = [base, yaw, pitch, blk, pump, gate]
for o in PARTS + [flash]: L.shade_and_uv(o)
T = L.Timeline()
rest = [(yaw, "rotation_euler", 2, 0.0), (pitch, "rotation_euler", 0, 0.0), (blk, "location", 1, 0.0), (pump, "location", 1, 0.0), (gate, "rotation_euler", 1, 0.0)]
L.hold(T, rest, "Idle", 24, "rest hold, loopable")
L.standard_aim(T, yaw, pitch, UP, DN)
s, e = T.clip("Fire", 8, desc="salvo: wide flash, 30 mm block recoil and return (cadence 0.85 s = Fire + Pump)")
L.key3(flash, "scale", s - 1, (0.001,)*3, 'CONSTANT'); L.flash_keys(flash, s, ((0, 1.0), (1, 0.75), (2, 0.001)))
L.recoil_keys(blk, s, RECOIL, ((0, 0.0), (1, 1.0), (3, 0.55), (6, 0.1), (8, 0.0))); T.clips[-1]["render_frames"] = [s]; HERO = s
s, e = T.clip("Pump", 12, desc="mechanical cycle: fore-end back 60 mm, forward")
for df, k in ((0, 0.0), (5, 1.0), (7, 1.0), (12, 0.0)): L.key(pump, "location", 1, s + df, PUMP*k, 'BEZIER')
T.clips[-1]["render_frames"] = [s + 6]
s, e = T.clip("Reload", 40, desc="top gate opens, 4 shell-feed beats (gate held), gate closes, pump cycle (game: 12 salvos, T reload)")
for df, d in ((0, 0.0), (6, 100.0), (26, 100.0), (31, 0.0)): L.key(gate, "rotation_euler", 1, s + df, math.radians(d), 'BEZIER')
for k in range(4):
    L.key(blk, "location", 1, s + 8 + 4*k, 0.0, 'LINEAR'); L.key(blk, "location", 1, s + 9 + 4*k, 0.004, 'LINEAR'); L.key(blk, "location", 1, s + 11 + 4*k, 0.0, 'LINEAR')
for df, k in ((31, 0.0), (35, 1.0), (40, 0.0)): L.key(pump, "location", 1, s + df, PUMP*k, 'BEZIER')
T.clips[-1]["render_frames"] = [s + 12]; HERO2 = s + 12
L.key3(flash, "scale", T.end, (0.001,)*3, 'CONSTANT')
for o, pth, idx, v in rest: L.key(o, pth, idx, T.end, v)
for o in (yaw, pitch, blk, pump, gate, flash): L.name_action(o, o.name + "_Master")
scn.frame_set(1)
S = dict(id=ID, kind="R4WeaponKind.Shotgun", game_name="WPN-001 Shotgun", out=OUT, root=root, pivot=pivot, yaw=yaw, pitch=pitch, socket=socket,
         export=[root, base, socket, pivot, yaw, pitch, blk, pump, gate, flash, muzzle], parts=PARTS, fx=[flash], muzzles=[muzzle],
         static=[base], yawparts=[yaw], gun=[pitch, blk, pump, gate], pitch_range=(DN, UP), frame_end=T.end, clips=T.clips,
         clash_pairs=[(blk, pitch), (pump, pitch), (gate, pitch), (blk, yaw), (pump, yaw), (gate, yaw), (pump, base), (blk, base), (pitch, yaw), (pitch, base), (yaw, base)],
         hero_frame=HERO, hero_frame2=HERO2, tri_budget=20000,
         exits_note="6 visible bores in one choke block = 1 logical Muzzle; gameplay pellets (8) are NOT tied to bore count",
         notes=["Choke block front 0.410 m, Muzzle 0.450 m forward of the pivot."])
Q.run(S)
