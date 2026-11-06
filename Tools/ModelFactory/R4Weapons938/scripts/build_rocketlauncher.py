# R4WeaponKind.RocketLauncher v2 - "WPN-004 Rocket Launcher" - central yaw tower with radar mast, TWIN side pods
# (2 tubes each) on one through-axle, front blast hatch per pod (outer hinge), rear loading cover per pod (top hinge).
# Same clips as v1: launch impulse with backblast from tube 1, rear-loading reload of tube 1 (game: 10 rockets; unchanged).
import sys, os, math; sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import importlib, bpy, r4lib as L, r4qa as Q, r4base as BS, r4heads as H; importlib.reload(L); importlib.reload(Q); importlib.reload(BS); importlib.reload(H)
from mathutils import Vector
from r4lib import F, P, M, box, cyl, cone, tube, hull, prism_x, revolve, hexbolt, Part, new_col
OUT = sys.argv[sys.argv.index("--") + 1]
L.reset_scene(); L.init_palette(); scn = bpy.context.scene
work = new_col("_build_tmp"); ID = "R4W_RocketLauncher"
DN, UP = 10.0, 35.0
PF0, PF1 = 0.000, 0.360                   # pod rear (on the pitch axis) / front
XI, XO = 0.090, 0.240                     # pod inner / outer |x|
PZ0, PZ1 = P - 0.050, P + 0.050
TUBES = [(-0.130, P), (-0.200, P), (0.130, P), (0.200, P)]   # 1 L-inner, 2 L-outer, 3 R-inner, 4 R-outer
BORE = 0.029; MUZ_F = 0.382; RZ = 0.0
BASE_STYLE, HEAD = "tripod", "central radar tower + twin side pods"
base, _ = BS.build_base_style(ID + "_Base", work, BASE_STYLE)
yaw = L.turntable(ID + "_Yaw", work)
y = Part(work, "y")
H.tower(y, 0.064)
box("power_pack", -0.074, 0.074, F(-0.112), F(-0.200), 0.035, 0.100, M["Armor_Dark"], y.U, bevel=0.008)
box("pack_lid", -0.066, 0.066, F(-0.120), F(-0.192), 0.096, 0.1035, M["TeamColor_Red"], y.U, bevel=0.003)
for xx in (-0.045, -0.015, 0.015, 0.045):
    cyl("gland", 0.007, (xx, F(-0.160), 0.103), (xx, F(-0.160), 0.114), M["Steel_Light"], y.U, seg=10)
    box("pack_vent", xx - 0.007, xx + 0.007, F(-0.197), F(-0.208), 0.046, 0.086, M["Armor_Dark"], y.D)
    box("pack_glow", xx - 0.005, xx + 0.005, 0.1970, 0.2000, 0.049, 0.083, M["Energy_Hot"], y.Pc)
box("pack_haz", -0.070, 0.070, F(-0.1995), F(-0.2005), 0.088, 0.095, M["Hazard_Yellow"], y.Pc)
y.build(yaw)
# ---- pitch = through-axle + two pods
p = Part(work, "p")
pitch = cyl(ID + "_Pitch", 0.018, (-XI - 0.004, 0, P), (XI + 0.004, 0, P), M["Steel_Light"], work, seg=16)
for s in (-1, 1):
    a_, b_ = sorted((s*XI, s*XO))
    box(f"pod{s}", a_, b_, F(PF0), F(PF1), PZ0, PZ1, M["Armor_Gunmetal"], p.U, bevel=0.008)
    cyl(f"pod_hub{s}", 0.034, (s*(XI - 0.010), 0, P), (s*(XI + 0.002), 0, P), M["Armor_Dark"], p.U, seg=24, bevel=0.002)
    for f0 in (0.040, 0.160, 0.280):
        c_, d_ = sorted((s*(XI - 0.0035), s*(XO + 0.0035)))
        box(f"rib{s}", c_, d_, F(f0), F(f0 + 0.020), PZ0 - 0.0035, PZ1 + 0.0035, M["Armor_Dark"], p.U, bevel=0.003)
    for (f0, f1) in ((0.064, 0.156), (0.184, 0.276)):
        c_, d_ = sorted((s*(XO - 0.0005), s*(XO + 0.004)))
        box(f"pod_appl{s}", c_, d_, F(f0), F(f1), PZ0 + 0.012, PZ1 - 0.012, M["Armor_Plate_Light"], p.U, bevel=0.002)
        for f_ in (f0 + 0.012, f1 - 0.012):
            for z_ in (PZ0 + 0.020, PZ1 - 0.020):
                hexbolt(f"appl_bolt{s}", (s*(XO + 0.0035), F(f_), z_), (s*(XO + 0.0085), F(f_), z_), M["Steel_Light"], p.Pc)
    c_, d_ = sorted((s*(XO + 0.0035), s*(XO + 0.0055)))
    box(f"pod_team{s}", c_, d_, F(0.064), F(0.276), PZ1 - 0.010, PZ1 - 0.003, M["TeamColor_Red"], p.Pc)
    c_, d_ = sorted((s*(XI + 0.012), s*(XO - 0.012)))
    box(f"top_haz{s}", c_, d_, F(0.300), F(0.348), PZ1 - 0.001, PZ1 + 0.0015, M["Hazard_Yellow"], p.Pc)
    for i in range(4):
        x0 = XI + 0.016 + i*0.032
        hull(f"haz_chev{s}{i}", [(s*xx, F(ff), zz) for zz in (PZ1, PZ1 + 0.0025) for (xx, ff) in ((x0, 0.3005), (x0 + 0.012, 0.3005), (x0 + 0.026, 0.3475), (x0 + 0.014, 0.3475))], M["Armor_Dark"], p.Pc)
    for k in range(3):
        f0 = 0.070 + 0.026*k
        c_, d_ = sorted((s*(XI + 0.020), s*(XO - 0.020)))
        box(f"top_slot{s}{k}", c_, d_, F(f0 + 0.010), F(f0), PZ1 - 0.006, PZ1 + 0.010, M["Armor_Dark"], p.D)
        box(f"top_glow{s}{k}", c_, d_, F(f0 + 0.009), F(f0 + 0.001), PZ1 - 0.007, PZ1 - 0.003, M["Energy_Hot"], p.Pc)
for (x, z) in TUBES:
    cyl("tube_bore", BORE, (x, F(PF0 - 0.01), z), (x, F(PF1 + 0.01), z), M["Armor_Dark"], p.D, seg=20)
    tube("tube_lip", 0.034, BORE - 0.0006, (x, F(PF1 - 0.008), z), (x, F(PF1 - 0.0005), z), M["Steel_Light"], p.Pc, seg=20)
    tube("tube_glow", BORE + 0.0004, BORE - 0.0015, (x, F(PF0 + 0.010), z), (x, F(PF0 + 0.014), z), M["Energy_Hot"], p.Pc, seg=20)
p.build(pitch)
# ---- rockets (4, visible in the tubes)
def build_rocket(name, x, z):
    rk = cyl(name, 0.022, (x, F(0.022), z), (x, F(0.296), z), M["Armor_Plate_Light"], work, seg=16)
    k = Part(work, "k" + name[-1])
    cone("nose", 0.022, 0.004, (x, F(0.2955), z), (x, F(0.350), z), M["Armor_Gunmetal"], k.U, seg=16)
    tube("nose_band", 0.0226, 0.018, (x, F(0.262), z), (x, F(0.278), z), M["TeamColor_Red"], k.U, seg=16)
    tube("tail_band", 0.0226, 0.018, (x, F(0.058), z), (x, F(0.068), z), M["Hazard_Yellow"], k.U, seg=16)
    cone("nozzle", 0.013, 0.018, (x, F(0.0225), z), (x, F(0.008), z), M["Barrel_Heat"], k.U, seg=12)
    for j in range(4):
        a = j*math.pi/2 + math.pi/4
        hull(f"fin{j}", [(x + rr*math.cos(a) + d*math.sin(a), F(ff), z + rr*math.sin(a) - d*math.cos(a)) for rr in (0.017, 0.0258) for d in (-0.0015, 0.0015) for ff in (0.026, 0.072)], M["Armor_Dark"], k.U)
    k.build(rk); return rk
rockets = [build_rocket(f"{ID}_Rocket_{i + 1}", x, z) for i, (x, z) in enumerate(TUBES)]
# ---- front blast hatch per pod (outer vertical hinge) and rear loading cover per pod (top hinge)
hatches = []
for s, tag in ((-1, "L"), (1, "R")):
    a_, b_ = sorted((s*(XI + 0.004), s*XO))
    h = box(f"{ID}_Hatch_{tag}", a_, b_, F(PF1 + 0.001), F(PF1 + 0.011), PZ0 + 0.004, PZ1 - 0.004, M["Armor_Gunmetal"], work, bevel=0.002)
    g = Part(work, "h" + tag)
    c_, d_ = sorted((s*(XI + 0.014), s*(XO - 0.010)))
    box("hatch_plate", c_, d_, F(PF1 + 0.010), F(PF1 + 0.014), PZ0 + 0.014, PZ1 - 0.014, M["Armor_Plate_Light"], g.U, bevel=0.0015)
    c_, d_ = sorted((s*(XI + 0.030), s*(XO - 0.026)))
    box("hatch_team", c_, d_, F(PF1 + 0.0135), F(PF1 + 0.0155), PZ1 - 0.026, PZ1 - 0.019, M["TeamColor_Red"], g.Pc)
    for zz in (PZ0 + 0.018, PZ1 - 0.018):
        cyl("hatch_knuckle", 0.0050, (s*XO, F(PF1 + 0.006), zz - 0.010), (s*XO, F(PF1 + 0.006), zz + 0.010), M["Steel_Light"], g.U, seg=10)
    g.build(h); hatches.append(h)
covers = []
for s, tag in ((-1, "L"), (1, "R")):
    a_, b_ = sorted((s*(XI + 0.006), s*(XO - 0.006)))
    cv = box(f"{ID}_RearCover_{tag}", a_, b_, F(PF0 - 0.011), F(PF0 - 0.001), PZ0 + 0.006, PZ1 - 0.004, M["Armor_Gunmetal"], work, bevel=0.002)
    c_ = Part(work, "c" + tag)
    e_, f_ = sorted((s*(XI + 0.018), s*(XO - 0.018)))
    box("cover_plate", e_, f_, F(PF0 - 0.014), F(PF0 - 0.010), PZ0 + 0.016, PZ1 - 0.016, M["Armor_Plate_Light"], c_.U, bevel=0.0015)
    for (x, z) in TUBES:
        if x*s > 0: tube("cover_port", 0.018, 0.011, (x, F(PF0 - 0.016), z), (x, F(PF0 - 0.012), z), M["Armor_Dark"], c_.U, seg=16)
    cyl("cover_hinge", 0.0050, (a_ + 0.008, F(PF0 - 0.006), PZ1 - 0.004), (b_ - 0.008, F(PF0 - 0.006), PZ1 - 0.004), M["Steel_Light"], c_.U, seg=10)
    box("cover_haz", e_, f_, F(PF0 - 0.0145), F(PF0 - 0.0135), PZ0 + 0.010, PZ0 + 0.015, M["Hazard_Yellow"], c_.Pc)
    c_.build(cv); covers.append(cv)
cover = covers[0]
flash = L.build_flash("MuzzleFlash", work, work, length=0.120, radius=0.036, spikes=4)
blast = L.build_flash("Rocket_Backblast", work, work, length=0.200, radius=0.050, spikes=6)
col = L.gather(ID, [base, yaw, pitch, flash, blast] + covers + rockets + hatches)
root, pivot, socket = L.finish_rig(ID, col, base, yaw, pitch, pitch_range=(DN, UP))
scn.frame_set(1); bpy.context.view_layer.update()
for rk, (x, z) in zip(rockets, TUBES): L.attach(rk, pitch, (x, F(0.160), z))
for h, s in zip(hatches, (-1, 1)): L.attach(h, pitch, (s*XO, F(PF1 + 0.006), P))
for cv, s in zip(covers, (-1, 1)): L.attach(cv, pitch, (s*(XI + XO)/2, F(PF0 - 0.006), PZ1 - 0.004))
x1, z1 = TUBES[0]
flash.parent = pitch; flash.location = (x1, F(PF1 + 0.016), z1 - P); flash.scale = (0.001,)*3
blast.parent = pitch; blast.location = (x1, F(PF0 - 0.020), z1 - P); blast.rotation_euler = (0, 0, math.pi); blast.scale = (0.001,)*3
muzzle = L.empty("Muzzle", pitch, (0, F(MUZ_F), RZ), col, 'SINGLE_ARROW', 0.06, L.UP90)
mz = [L.empty(f"Muzzle_{i + 1}", pitch, (x, F(MUZ_F), z - P), col, 'SINGLE_ARROW', 0.04, L.UP90) for i, (x, z) in enumerate(TUBES)]
L.root_props(root, "R4WeaponKind.RocketLauncher", version="v2")
L.set_props({pivot: {"role": "game-driven pivot", "physical": "(0,0.135,0)"}, yaw: {"role": "Yaw: rotate around local Y only (tower + radar mast)"},
             pitch: {"role": "Pitch: rotate around local X only (through-axle carrying both pods)", "pitch_range_deg": f"-{DN:.0f}..+{UP:.0f}"},
             covers[0]: {"role": "left rear loading cover: rotates about local X (top hinge), opens 110 deg for reload"},
             covers[1]: {"role": "right rear loading cover: rotates about local X (top hinge); rest in the shipped clips"},
             flash: {"role": "tube-mouth launch flash FX (tube 1) - idle scale 0.001"}, blast: {"role": "backblast FX at the pod rear (tube 1) - idle scale 0.001"},
             socket: {"role": "mounting face centre, normal -Y, connects to cell (0,-1,0)"},
             muzzle: {"role": "logical rocket spawn (centre between the pods)", "physical": f"(0,{0.135 + RZ:.3f},{MUZ_F})"}})
for i, rk in enumerate(rockets): rk["role"] = f"visual rocket in tube {i + 1}: Fire moves it out along local +Z then hides it (scale 0.001); Reload slides it in from the rear"
for i, m_ in enumerate(mz): m_["role"] = f"visual tube {i + 1} mouth (not a gameplay shot count)"
for h in hatches: h["role"] = "front blast hatch of one pod: rotates about local Y (outer hinge), opens 100 deg before launch"
PARTS = [base, yaw, pitch] + covers + hatches + rockets; FX = [flash, blast]
for o in PARTS + FX: L.shade_and_uv(o)
T = L.Timeline()
R1 = rockets[0]; HL = hatches[0]; RY = R1.location[1]
rest = [(yaw, "rotation_euler", 2, 0.0), (pitch, "rotation_euler", 0, 0.0), (covers[0], "rotation_euler", 0, 0.0), (covers[1], "rotation_euler", 0, 0.0),
        (hatches[0], "rotation_euler", 2, 0.0), (hatches[1], "rotation_euler", 2, 0.0), (R1, "location", 1, RY)]
L.hold(T, rest, "Idle", 24, "rest hold, loopable, 4 rockets loaded")
L.standard_aim(T, yaw, pitch, UP, DN)
def fx(o, f, sc): L.key3(o, "scale", f, (sc,)*3, 'CONSTANT')
s, e = T.clip("Fire", 24, desc="launch tube 1 (left pod, inner): left hatch opens, rocket leaves (0.9 m in 3 f) with flash + backblast, pods kick, hatch closes; tube 1 stays empty")
for o in FX + [R1]: fx(o, s - 1, 1.0 if o is R1 else 0.001)
fx(R1, s, 1.0); L.key(R1, "location", 1, s, RY, 'LINEAR')
for df, d in ((0, 0.0), (5, -100.0), (14, -100.0), (20, 0.0)): L.key(HL, "rotation_euler", 2, s + df, math.radians(d))
L.key(R1, "location", 1, s + 6, RY, 'LINEAR'); L.key(R1, "location", 1, s + 9, RY - 0.90, 'LINEAR'); fx(R1, s + 9, 0.001); L.key(R1, "location", 1, s + 10, RY, 'CONSTANT')
L.flash_keys(flash, s + 7, ((0, 1.0), (1, 0.6), (2, 0.001))); L.flash_keys(blast, s + 6, ((0, 1.0), (1, 0.8), (2, 0.5), (3, 0.001)))
for df, d in ((6, 0.0), (8, 1.5), (11, -0.5), (15, 0.0)): L.key(pitch, "rotation_euler", 0, s + df, math.radians(-d))
L.key(R1, "location", 1, e, RY, 'CONSTANT'); fx(R1, e, 0.001)
T.clips[-1]["render_frames"] = [s + 7]; HERO = s + 7
s, e = T.clip("Reload", 36, desc="left rear cover opens 110 deg (top hinge), rocket 1 slides in from behind the pod, cover closes")
fx(R1, s, 0.001); L.key(R1, "location", 1, s, RY, 'CONSTANT')
for df, d in ((0, 0.0), (8, 110.0), (28, 110.0), (36, 0.0)): L.key(covers[0], "rotation_euler", 0, s + df, math.radians(d))
L.key(R1, "location", 1, s + 9, RY + 0.34, 'CONSTANT'); fx(R1, s + 9, 1.0)
L.key(R1, "location", 1, s + 10, RY + 0.34, 'BEZIER'); L.key(R1, "location", 1, s + 24, RY, 'BEZIER'); L.key(R1, "location", 1, e, RY)
T.clips[-1]["render_frames"] = [s + 16]; HERO2 = s + 16
fx(R1, T.end, 1.0)
for o in FX: fx(o, T.end, 0.001)
for o, pth, idx, v in rest: L.key(o, pth, idx, T.end, v)
for o in [yaw, pitch, R1, flash, blast] + covers + hatches: L.name_action(o, o.name + "_Master")
scn.frame_set(1)
def lof_setup(open_):
    for h, sg in zip(hatches, (-1, 1)): h.rotation_euler[2] = math.radians(sg*100.0) if open_ else 0.0
S = dict(id=ID, kind="R4WeaponKind.RocketLauncher", game_name="WPN-004 Rocket Launcher", out=OUT, root=root, pivot=pivot, yaw=yaw, pitch=pitch, socket=socket,
         export=[root, base, socket, pivot, yaw, pitch] + covers + hatches + rockets + [flash, blast, muzzle] + mz, parts=PARTS, fx=FX, muzzles=[muzzle] + mz,
         static=[base], yawparts=[yaw], gun=[pitch] + covers + hatches + rockets, pitch_range=(DN, UP), frame_end=T.end, clips=T.clips,
         lof_setup=lof_setup, lof_pose_note="hatches open (launch pose); rockets in tubes",
         clash_pairs=[(R1, pitch), (R1, hatches[0]), (R1, covers[0]), (R1, yaw), (covers[0], pitch), (covers[0], yaw), (covers[1], pitch), (hatches[0], pitch), (hatches[1], pitch),
                      (hatches[0], yaw), (hatches[1], yaw), (pitch, yaw), (pitch, base), (yaw, base)] + [(rk, pitch) for rk in rockets[1:]],
         hero_frame=HERO, hero_frame2=HERO2, tri_budget=22000,
         exits_note="4 visible tubes in 2 pods (Muzzle_1..4) + 1 logical Muzzle between the pods; gameplay keeps one rocket per shot and 10-rocket ammo",
         notes=["Fire clip launches tube 1 only; tube 1 is empty until Reload (master timeline: Fire is followed by Reload).",
                f"v2 silhouette pass (#939 follow-up): base style {BASE_STYLE}, head {HEAD}; rig, pivot, muzzle contract and clips unchanged (second rear cover added, rest in clips)."])
S["base_style"], S["head_style"] = BASE_STYLE, HEAD
Q.run(S)
