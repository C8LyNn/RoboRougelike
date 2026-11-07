# R4WeaponKind.RocketLauncher - "WPN-004 Rocket Launcher" - 2x2 tube pod, front blast hatches, top-hinged rear loading
# cover, launch impulse with backblast, rear-loading reload of the fired tube (game: 10 rockets, T reload; unchanged).
import sys, os, math; sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import importlib, bpy, r4lib as L, r4qa as Q; importlib.reload(L); importlib.reload(Q)
from mathutils import Vector
from r4lib import F, P, M, box, cyl, cone, tube, hull, prism_x, revolve, hexbolt, Part, new_col
OUT = sys.argv[sys.argv.index("--") + 1]
L.reset_scene(); L.init_palette(); scn = bpy.context.scene
work = new_col("_build_tmp"); ID = "R4W_RocketLauncher"
DN, UP = 10.0, 35.0
PF0, PF1 = 0.000, 0.380                  # pod rear / front (trunnions at the pod rear: clears the turntable over -10..+35)
PZ0, PZ1 = P - 0.055, P + 0.102
TUBES = [(-0.050, P + 0.059), (0.050, P + 0.059), (-0.050, P - 0.012), (0.050, P - 0.012)]   # 1 TL, 2 TR, 3 BL, 4 BR
BORE = 0.033; MUZ_F = 0.402; RZ = 0.0235
base, _ = L.build_base(ID + "_Base", work, vents=5, extra_plates=True, glacis_top=0.190)
yaw = L.turntable(ID + "_Yaw", work)
y = Part(work, "y")
L.yoke(y, 0.124, 0.164, [(0.130, 0.035), (0.172, 0.078), (0.080, 0.212), (-0.080, 0.218), (-0.140, 0.160), (-0.140, 0.035)],
       [(0.104, 0.058), (0.146, 0.092), (0.068, 0.194), (-0.062, 0.198), (-0.114, 0.150), (-0.114, 0.058)],
       [(-0.096, 0.152), (0.090, 0.152), (0.072, 0.168), (-0.080, 0.168)], bolts=((0.092, 0.078), (-0.105, 0.066), (-0.010, 0.186)))
box("power_pack", -0.100, 0.100, F(-0.148), F(-0.232), 0.035, 0.090, M["Armor_Dark"], y.U, bevel=0.008)
box("pack_lid", -0.090, 0.090, F(-0.156), F(-0.224), 0.086, 0.0935, M["TeamColor_Red"], y.U, bevel=0.003)
for xx in (-0.060, -0.020, 0.020, 0.060):
    cyl("gland", 0.008, (xx, F(-0.180), 0.093), (xx, F(-0.180), 0.104), M["Steel_Light"], y.U, seg=10)
    box("pack_vent", xx - 0.008, xx + 0.008, F(-0.229), F(-0.240), 0.046, 0.078, M["Armor_Dark"], y.D)
    box("pack_glow", xx - 0.006, xx + 0.006, 0.2290, 0.2320, 0.049, 0.075, M["Energy_Hot"], y.Pc)
box("pack_haz", -0.094, 0.094, F(-0.2315), F(-0.2325), 0.081, 0.088, M["Hazard_Yellow"], y.Pc)
y.build(yaw)
# ---- pitch = launcher pod: trunnions, armoured box, frame ribs, applique + team, 4 lined tubes, top hazard band, sensor
p = Part(work, "p")
pitch = cyl(ID + "_Pitch", 0.050, (0.106, 0, P), (0.118, 0, P), M["Armor_Dark"], work, seg=32, bevel=0.004)
cyl("stub_l", 0.050, (-0.118, 0, P), (-0.106, 0, P), M["Armor_Dark"], p.U, seg=32, bevel=0.004)
for s_ in (-1, 1): cyl("axle", 0.018, (s_*0.108, 0, P), (s_*0.150, 0, P), M["Steel_Light"], p.U, seg=16)
box("pod", -0.112, 0.112, F(PF0), F(PF1), PZ0, PZ1, M["Armor_Gunmetal"], p.U, bevel=0.008)
for f0 in (0.040, 0.170, 0.290):
    box("rib", -0.1165, 0.1165, F(f0), F(f0 + 0.022), PZ0 + 0.0015, PZ1 + 0.0045, M["Armor_Dark"], p.U, bevel=0.003)
for s in (-1, 1):
    box(f"pod_appl{s}", s*0.1115, s*0.1160, F(0.066), F(0.166), PZ0 + 0.014, PZ1 - 0.014, M["Armor_Plate_Light"], p.U, bevel=0.002)
    box(f"pod_appl2{s}", s*0.1115, s*0.1160, F(0.196), F(0.286), PZ0 + 0.014, PZ1 - 0.014, M["Armor_Plate_Light"], p.U, bevel=0.002)
    box(f"pod_team{s}", s*0.1155, s*0.1175, F(0.070), F(0.282), PZ1 - 0.030, PZ1 - 0.020, M["TeamColor_Red"], p.Pc)
    for f_ in (0.080, 0.152, 0.210, 0.272):
        for z_ in (PZ0 + 0.024, PZ1 - 0.040):
            hexbolt(f"appl_bolt{s}", (s*0.1155, F(f_), z_), (s*0.1205, F(f_), z_), M["Steel_Light"], p.Pc)
for (x, z) in TUBES:
    cyl("tube_bore", BORE, (x, F(PF0 - 0.01), z), (x, F(PF1 + 0.01), z), M["Armor_Dark"], p.D, seg=20)
    tube("tube_lip", 0.039, BORE - 0.0006, (x, F(PF1 - 0.008), z), (x, F(PF1 - 0.0005), z), M["Steel_Light"], p.Pc, seg=20)
    tube("tube_glow", BORE + 0.0004, BORE - 0.0015, (x, F(PF0 + 0.010), z), (x, F(PF0 + 0.014), z), M["Energy_Hot"], p.Pc, seg=20)
box("top_haz", -0.090, 0.090, F(0.312), F(0.362), PZ1 - 0.001, PZ1 + 0.0015, M["Hazard_Yellow"], p.Pc)
for i in range(6):
    x0 = -0.088 + i*0.030
    hull(f"haz_chev{i}", [(xx, F(ff), zz) for zz in (PZ1, PZ1 + 0.0025) for (xx, ff) in ((x0, 0.3125), (x0 + 0.012, 0.3125), (x0 + 0.026, 0.3615), (x0 + 0.014, 0.3615))], M["Armor_Dark"], p.Pc)
box("sensor", -0.060, -0.010, F(0.280), F(0.190), PZ1 + 0.002, PZ1 + 0.026, M["Armor_Dark"], p.U, bevel=0.004)
box("sensor_eye", -0.054, -0.016, F(0.2815), F(0.2785), PZ1 + 0.008, PZ1 + 0.020, M["Sensor_Eye_Red"], p.U)
for k in range(4):
    f0 = 0.070 + 0.026*k
    box(f"top_slot{k}", 0.010, 0.095, F(f0 + 0.010), F(f0), PZ1 - 0.006, PZ1 + 0.010, M["Armor_Dark"], p.D)
    box(f"top_glow{k}", 0.012, 0.093, F(f0 + 0.009), F(f0 + 0.001), PZ1 - 0.007, PZ1 - 0.003, M["Energy_Hot"], p.Pc)
p.build(pitch)
# ---- rockets (4, visible in the tubes)
def build_rocket(name, x, z):
    rk = cyl(name, 0.024, (x, F(0.022), z), (x, F(0.302), z), M["Armor_Plate_Light"], work, seg=16)
    k = Part(work, "k" + name[-1])
    cone("nose", 0.024, 0.004, (x, F(0.3015), z), (x, F(0.362), z), M["Armor_Gunmetal"], k.U, seg=16)
    tube("nose_band", 0.0246, 0.020, (x, F(0.268), z), (x, F(0.284), z), M["TeamColor_Red"], k.U, seg=16)
    tube("tail_band", 0.0246, 0.020, (x, F(0.058), z), (x, F(0.068), z), M["Hazard_Yellow"], k.U, seg=16)
    cone("nozzle", 0.014, 0.019, (x, F(0.0225), z), (x, F(0.008), z), M["Barrel_Heat"], k.U, seg=12)
    for j in range(4):
        a = j*math.pi/2 + math.pi/4
        hull(f"fin{j}", [(x + rr*math.cos(a) + d*math.sin(a), F(ff), z + rr*math.sin(a) - d*math.cos(a)) for rr in (0.020, 0.031) for d in (-0.0015, 0.0015) for ff in (0.026, 0.072)], M["Armor_Dark"], k.U)
    k.build(rk); return rk
rockets = [build_rocket(f"{ID}_Rocket_{i + 1}", x, z) for i, (x, z) in enumerate(TUBES)]
# ---- front blast hatches (outer vertical hinges) and rear loading cover (top hinge)
hatches = []
for s, tag in ((-1, "L"), (1, "R")):
    h = box(f"{ID}_Hatch_{tag}", s*0.006, s*0.110, F(PF1 + 0.001), F(PF1 + 0.011), PZ0 + 0.004, PZ1 - 0.004, M["Armor_Gunmetal"], work, bevel=0.002)
    g = Part(work, "h" + tag)
    box("hatch_plate", s*0.016, s*0.100, F(PF1 + 0.010), F(PF1 + 0.014), PZ0 + 0.016, PZ1 - 0.016, M["Armor_Plate_Light"], g.U, bevel=0.0015)
    box("hatch_team", s*0.030, s*0.086, F(PF1 + 0.0135), F(PF1 + 0.0155), PZ1 - 0.032, PZ1 - 0.024, M["TeamColor_Red"], g.Pc)
    for zz in (PZ0 + 0.020, PZ1 - 0.020):
        cyl("hatch_knuckle", 0.0050, (s*0.110, F(PF1 + 0.006), zz - 0.010), (s*0.110, F(PF1 + 0.006), zz + 0.010), M["Steel_Light"], g.U, seg=10)
    g.build(h); hatches.append(h)
cover = box(f"{ID}_RearCover", -0.104, 0.104, F(PF0 - 0.011), F(PF0 - 0.001), PZ0 + 0.006, PZ1 - 0.004, M["Armor_Gunmetal"], work, bevel=0.002)
c_ = Part(work, "c")
box("cover_plate", -0.090, 0.090, F(PF0 - 0.014), F(PF0 - 0.010), PZ0 + 0.020, PZ1 - 0.020, M["Armor_Plate_Light"], c_.U, bevel=0.0015)
for (x, z) in TUBES:
    tube("cover_port", 0.020, 0.012, (x, F(PF0 - 0.016), z), (x, F(PF0 - 0.012), z), M["Armor_Dark"], c_.U, seg=16)
cyl("cover_hinge", 0.0050, (-0.096, F(PF0 - 0.006), PZ1 - 0.004), (0.096, F(PF0 - 0.006), PZ1 - 0.004), M["Steel_Light"], c_.U, seg=10)
box("cover_haz", -0.080, 0.080, F(PF0 - 0.0145), F(PF0 - 0.0135), PZ0 + 0.012, PZ0 + 0.018, M["Hazard_Yellow"], c_.Pc)
c_.build(cover)
flash = L.build_flash("MuzzleFlash", work, work, length=0.120, radius=0.040, spikes=4)
blast = L.build_flash("Rocket_Backblast", work, work, length=0.200, radius=0.055, spikes=6)
col = L.gather(ID, [base, yaw, pitch, cover, flash, blast] + rockets + hatches)
root, pivot, socket = L.finish_rig(ID, col, base, yaw, pitch, pitch_range=(DN, UP))
scn.frame_set(1); bpy.context.view_layer.update()
for rk, (x, z) in zip(rockets, TUBES): L.attach(rk, pitch, (x, F(0.160), z))
for h, s in zip(hatches, (-1, 1)): L.attach(h, pitch, (s*0.110, F(PF1 + 0.006), P))
L.attach(cover, pitch, (0, F(PF0 - 0.006), PZ1 - 0.004))
x1, z1 = TUBES[0]
flash.parent = pitch; flash.location = (x1, F(PF1 + 0.016), z1 - P); flash.scale = (0.001,)*3
blast.parent = pitch; blast.location = (x1, F(PF0 - 0.020), z1 - P); blast.rotation_euler = (0, 0, math.pi); blast.scale = (0.001,)*3
muzzle = L.empty("Muzzle", pitch, (0, F(MUZ_F), RZ), col, 'SINGLE_ARROW', 0.06, L.UP90)
mz = [L.empty(f"Muzzle_{i + 1}", pitch, (x, F(MUZ_F), z - P), col, 'SINGLE_ARROW', 0.04, L.UP90) for i, (x, z) in enumerate(TUBES)]
L.root_props(root, "R4WeaponKind.RocketLauncher")
L.set_props({pivot: {"role": "game-driven pivot", "physical": "(0,0.135,0)"}, yaw: {"role": "Yaw: rotate around local Y only"},
             pitch: {"role": "Pitch: rotate around local X only (launcher pod)", "pitch_range_deg": f"-{DN:.0f}..+{UP:.0f}"},
             cover: {"role": "rear loading cover: rotates about local X (top hinge), opens 110 deg for reload"},
             flash: {"role": "tube-mouth launch flash FX (tube 1) - idle scale 0.001"}, blast: {"role": "backblast FX at the pod rear (tube 1) - idle scale 0.001"},
             socket: {"role": "mounting face centre, normal -Y, connects to cell (0,-1,0)"},
             muzzle: {"role": "logical rocket spawn (centre of the 2x2 pod)", "physical": f"(0,{0.135 + RZ:.3f},{MUZ_F})"}})
for i, rk in enumerate(rockets): rk["role"] = f"visual rocket in tube {i + 1}: Fire moves it out along local +Z then hides it (scale 0.001); Reload slides it in from the rear"
for i, m_ in enumerate(mz): m_["role"] = f"visual tube {i + 1} mouth (not a gameplay shot count)"
for h in hatches: h["role"] = "front blast hatch: rotates about local Y (outer hinge), opens 100 deg before launch"
PARTS = [base, yaw, pitch, cover] + hatches + rockets; FX = [flash, blast]
for o in PARTS + FX: L.shade_and_uv(o)
T = L.Timeline()
R1 = rockets[0]; HL = hatches[0]; RY = R1.location[1]
rest = [(yaw, "rotation_euler", 2, 0.0), (pitch, "rotation_euler", 0, 0.0), (cover, "rotation_euler", 0, 0.0), (hatches[0], "rotation_euler", 2, 0.0),
        (hatches[1], "rotation_euler", 2, 0.0), (R1, "location", 1, RY)]
L.hold(T, rest, "Idle", 24, "rest hold, loopable, 4 rockets loaded")
L.standard_aim(T, yaw, pitch, UP, DN)
def fx(o, f, sc): L.key3(o, "scale", f, (sc,)*3, 'CONSTANT')
s, e = T.clip("Fire", 24, desc="launch tube 1: left hatch opens, rocket leaves (0.9 m in 3 f) with flash + backblast, pod kick, hatch closes; tube 1 stays empty")
for o in FX + [R1]: fx(o, s - 1, 1.0 if o is R1 else 0.001)
fx(R1, s, 1.0); L.key(R1, "location", 1, s, RY, 'LINEAR')
for df, d in ((0, 0.0), (5, -100.0), (14, -100.0), (20, 0.0)): L.key(HL, "rotation_euler", 2, s + df, math.radians(d))
L.key(R1, "location", 1, s + 6, RY, 'LINEAR'); L.key(R1, "location", 1, s + 9, RY - 0.90, 'LINEAR'); fx(R1, s + 9, 0.001); L.key(R1, "location", 1, s + 10, RY, 'CONSTANT')
L.flash_keys(flash, s + 7, ((0, 1.0), (1, 0.6), (2, 0.001))); L.flash_keys(blast, s + 6, ((0, 1.0), (1, 0.8), (2, 0.5), (3, 0.001)))
for df, d in ((6, 0.0), (8, 1.5), (11, -0.5), (15, 0.0)): L.key(pitch, "rotation_euler", 0, s + df, math.radians(-d))
L.key(R1, "location", 1, e, RY, 'CONSTANT'); fx(R1, e, 0.001)
T.clips[-1]["render_frames"] = [s + 7]; HERO = s + 7
s, e = T.clip("Reload", 36, desc="rear cover opens 110 deg (top hinge), rocket 1 slides in from behind the pod, cover closes")
fx(R1, s, 0.001); L.key(R1, "location", 1, s, RY, 'CONSTANT')
for df, d in ((0, 0.0), (8, 110.0), (28, 110.0), (36, 0.0)): L.key(cover, "rotation_euler", 0, s + df, math.radians(d))
L.key(R1, "location", 1, s + 9, RY + 0.34, 'CONSTANT'); fx(R1, s + 9, 1.0)
L.key(R1, "location", 1, s + 10, RY + 0.34, 'BEZIER'); L.key(R1, "location", 1, s + 24, RY, 'BEZIER'); L.key(R1, "location", 1, e, RY)
T.clips[-1]["render_frames"] = [s + 16]; HERO2 = s + 16
fx(R1, T.end, 1.0)
for o in FX: fx(o, T.end, 0.001)
for o, pth, idx, v in rest: L.key(o, pth, idx, T.end, v)
for o in [yaw, pitch, cover, R1, flash, blast] + hatches: L.name_action(o, o.name + "_Master")
scn.frame_set(1)
def lof_setup(open_):
    for h, sg in zip(hatches, (-1, 1)): h.rotation_euler[2] = math.radians(sg*100.0) if open_ else 0.0
S = dict(id=ID, kind="R4WeaponKind.RocketLauncher", game_name="WPN-004 Rocket Launcher", out=OUT, root=root, pivot=pivot, yaw=yaw, pitch=pitch, socket=socket,
         export=[root, base, socket, pivot, yaw, pitch, cover] + hatches + rockets + [flash, blast, muzzle] + mz, parts=PARTS, fx=FX, muzzles=[muzzle] + mz,
         static=[base], yawparts=[yaw], gun=[pitch, cover] + hatches + rockets, pitch_range=(DN, UP), frame_end=T.end, clips=T.clips,
         lof_setup=lof_setup, lof_pose_note="hatches open (launch pose); rockets in tubes",
         clash_pairs=[(R1, pitch), (R1, hatches[0]), (R1, cover), (R1, yaw), (cover, pitch), (cover, yaw), (hatches[0], pitch), (hatches[1], pitch),
                      (hatches[0], yaw), (hatches[1], yaw), (pitch, yaw), (pitch, base), (yaw, base)] + [(rk, pitch) for rk in rockets[1:]],
         hero_frame=HERO, hero_frame2=HERO2, tri_budget=22000,
         exits_note="4 visible tubes (Muzzle_1..4) + 1 logical Muzzle; gameplay keeps one rocket per shot and 10-rocket ammo",
         notes=["Fire clip launches tube 1 only; tube 1 is empty until Reload (master timeline: Fire is followed by Reload)."])
Q.run(S)
