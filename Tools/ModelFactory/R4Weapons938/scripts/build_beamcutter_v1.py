# R4WeaponKind.BeamCutter - "WPN-003 Beam Cutter" - emitter head with focus ring, hinged radiator fin banks,
# coolant tank on the yaw; warm-up / continuous fire / heat / cooldown (beam length in game is drawn by the runtime).
import sys, os, math; sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import importlib, bpy, r4lib as L, r4qa as Q; importlib.reload(L); importlib.reload(Q)
from r4lib import F, P, M, box, cyl, cone, tube, hull, prism_x, revolve, hexbolt, Part, new_col
OUT = sys.argv[sys.argv.index("--") + 1]
L.reset_scene(); L.init_palette(); scn = bpy.context.scene
work = new_col("_build_tmp"); ID = "R4W_BeamCutter"
LENS_F = 0.432; MUZZLE_F = 0.440; DN, UP = 10.0, 35.0; FIN_OPEN = 35.0
base, _ = L.build_base(ID + "_Base", work, vents=4, glacis_top=0.190)
yaw = L.turntable(ID + "_Yaw", work)
y = Part(work, "y")
L.yoke(y, 0.108, 0.150, [(0.140, 0.035), (0.188, 0.085), (0.062, 0.212), (-0.090, 0.220), (-0.150, 0.160), (-0.150, 0.035)],
       [(0.112, 0.060), (0.158, 0.098), (0.052, 0.192), (-0.070, 0.198), (-0.122, 0.150), (-0.122, 0.060)],
       [(-0.108, 0.158), (0.080, 0.158), (0.060, 0.1745), (-0.090, 0.1745)])
TF, TZ = -0.192, 0.104
cyl("tank", 0.052, (-0.090, F(TF), TZ), (0.090, F(TF), TZ), M["Armor_Gunmetal"], y.U, seg=32, bevel=0.005)
for s in (-1, 1):
    cyl(f"tank_cap{s}", 0.056, (s*0.078, F(TF), TZ), (s*0.094, F(TF), TZ), M["Armor_Plate_Light"], y.U, seg=32, bevel=0.003)
    for i in range(6):
        a = 2*math.pi*(i + 0.5)/6
        hexbolt(f"tank_bolt{s}{i}", (s*0.093, F(TF) + 0.040*math.cos(a), TZ + 0.040*math.sin(a)), (s*0.098, F(TF) + 0.040*math.cos(a), TZ + 0.040*math.sin(a)), M["Steel_Light"], y.Pc)
tube("tank_team", 0.0545, 0.046, (-0.030, F(TF), TZ), (0.030, F(TF), TZ), M["TeamColor_Red"], y.Pc, seg=32)
for xx in (-0.050, 0.050):
    tube("tank_band", 0.0545, 0.046, (xx - 0.006, F(TF), TZ), (xx + 0.006, F(TF), TZ), M["Armor_Dark"], y.Pc, seg=32)
box("tank_cradle", -0.100, 0.100, F(TF - 0.046), F(TF + 0.050), 0.035, TZ - 0.028, M["Armor_Dark"], y.U, bevel=0.006)
box("tank_gauge", -0.012, 0.012, F(TF - 0.020), F(TF + 0.020), TZ + 0.048, TZ + 0.062, M["Armor_Dark"], y.U, bevel=0.003)
box("gauge_eye", -0.008, 0.008, F(TF - 0.016), F(TF + 0.016), TZ + 0.0615, TZ + 0.0635, M["Energy_Hot"], y.Pc)
for s in (-1, 1):
    hull(f"coolant_line{s}", [(x, F(f), z) for (f, z) in ((TF + 0.030, TZ + 0.040), (TF + 0.030, TZ + 0.054), (-0.112, 0.146), (-0.112, 0.160)) for x in (s*0.094, s*0.1095)],
         M["Steel_Light"], y.U, bevel=0.002)
box("haz_plate", -0.060, 0.060, F(-0.2450), F(-0.2462), 0.040, 0.068, M["Hazard_Yellow"], y.Pc)
for i in range(5):
    x0 = -0.058 + i*0.024
    hull(f"haz_chev{i}", [(xx, F(ff), zz) for ff in (-0.2448, -0.2466) for (xx, zz) in ((x0, 0.0405), (x0 + 0.010, 0.0405), (x0 + 0.020, 0.0675), (x0 + 0.010, 0.0675))], M["Armor_Dark"], y.Pc)
y.build(yaw)
# ---- pitch: receiver, emitter housing with collars, lens cone, sensor
p = Part(work, "p")
pitch = L.trunnion(ID + "_Pitch", work, p, half=0.100, r=0.048, axle=0.140)
hull("receiver", [(x, F(f), z) for (f, xw, z0, z1) in ((-0.075, 0.088, 0.092, 0.194), (0.090, 0.094, 0.086, 0.204), (0.170, 0.074, 0.098, 0.172))
                  for x in (-xw, xw) for z in (z0, z1)], M["Armor_Gunmetal"], p.U, bevel=0.005)
for s in (-1, 1):
    prism_x(f"rcv_appl{s}", s*0.0935, s*0.0985, [(-0.055, 0.100), (0.135, 0.100), (0.155, 0.140), (0.110, 0.190), (-0.055, 0.184)], M["Armor_Plate_Light"], p.U, bevel=0.002)
    box(f"rcv_team{s}", s*0.0978, s*0.0994, F(-0.040), F(0.110), 0.150, 0.162, M["TeamColor_Red"], p.Pc)
cyl("housing", 0.052, (0, F(0.150), P), (0, F(0.400), P), M["Armor_Gunmetal"], p.U, seg=24, bevel=0.004)
for (f0, f1) in ((0.160, 0.182), (0.376, 0.398)):
    cyl("collar", 0.059, (0, F(f0), P), (0, F(f1), P), M["Armor_Plate_Light"], p.U, seg=24, bevel=0.003)
cyl("lens_neck", 0.036, (0, F(0.398), P), (0, F(LENS_F), P), M["Armor_Dark"], p.U, seg=24)
tube("lens_lip", 0.042, 0.030, (0, F(LENS_F - 0.008), P), (0, F(LENS_F), P), M["Armor_Gunmetal"], p.U, seg=24)
cyl("lens_seat", 0.026, (0, F(LENS_F - 0.010), P), (0, F(LENS_F + 0.005), P), M["Armor_Dark"], p.D, seg=24)
cyl("lens", 0.024, (0, F(LENS_F - 0.012), P), (0, F(LENS_F - 0.004), P), M["Energy_Hot"], p.Pc, seg=24)
for s in (-1, 1):
    box(f"fin_spine_seat{s}", s*0.030, s*0.0565, F(0.196), F(0.364), P + 0.040, P + 0.047, M["Armor_Dark"], p.U)
for k in range(8):
    a = 2*math.pi*(k + 0.5)/8
    hexbolt(f"collar_bolt{k}", (0.059*math.cos(a), F(0.372), P + 0.059*math.sin(a)), (0.059*math.cos(a), F(0.368), P + 0.059*math.sin(a)), M["Steel_Light"], p.Pc, r=0.0045)
box("sensor", -0.070, -0.030, F(0.150), F(0.060), 0.196, 0.222, M["Armor_Dark"], p.U, bevel=0.004)
box("sensor_eye", -0.064, -0.036, F(0.1515), F(0.1485), 0.202, 0.216, M["Sensor_Eye_Red"], p.U)
for k in range(3):
    f0 = -0.050 + 0.030*k
    box(f"top_slot{k}", -0.010, 0.075, F(f0 + 0.010), F(f0), 0.190, 0.212, M["Armor_Dark"], p.D)
    box(f"top_glow{k}", -0.008, 0.073, F(f0 + 0.009), F(f0 + 0.001), 0.189, 0.193, M["Energy_Hot"], p.Pc)
p.build(pitch)
# ---- focus ring (rotates about the fire axis)
ring = L.tube(ID + "_FocusRing", 0.058, 0.0385, (0, F(0.404), P), (0, F(0.420), P), M["Steel_Light"], work, seg=36)
g = Part(work, "g")
for k in range(6):
    a = 2*math.pi*k/6
    hull(f"notch{k}", [(rr*math.cos(a) + d*math.sin(a), F(ff), P + rr*math.sin(a) - d*math.cos(a)) for rr in (0.053, 0.062) for d in (-0.006, 0.006) for ff in (0.402, 0.422)], M["Armor_Dark"], g.D)
tube("ring_glow", 0.0590, 0.0548, (0, F(0.411), P), (0, F(0.413), P), M["Energy_Hot"], g.Pc, seg=36)
g.build(ring)
# ---- radiator fin banks (hinged along the fire axis on the housing's upper flanks)
fins = []
for s, tag in ((-1, "L"), (1, "R")):
    fb = box(f"{ID}_Fin{tag}", s*0.057, s*0.065, F(0.198), F(0.362), P - 0.036, P + 0.038, M["Armor_Dark"], work, bevel=0.002)
    q = Part(work, "q" + tag)
    for k in range(7):
        f0 = 0.204 + 0.0225*k
        box(f"fin{k}", s*0.064, s*0.094, F(f0), F(f0 + 0.0045), P - 0.034, P + 0.034, M["Steel_Light"], q.U, bevel=0.0012)
    box("fin_team", s*0.0645, s*0.0665, F(0.214), F(0.346), P - 0.031, P - 0.025, M["TeamColor_Red"], q.Pc)
    cyl("fin_hinge", 0.0045, (s*0.057, F(0.196), P + 0.034), (s*0.057, F(0.364), P + 0.034), M["Steel_Light"], q.U, seg=10)
    q.build(fb); fins.append(fb)
# ---- FX
core = L.cyl("Beam_Core", 0.011, (0, F(LENS_F), P), (0, F(LENS_F + 0.70), P), M["Muzzle_Flash"], work, seg=12)
shell = L.cone("Beam_Sheath", 0.030, 0.014, (0, F(LENS_F), P), (0, F(LENS_F + 0.18), P), M["Energy_Hot"], work, seg=16)
eglow = L.revolve("Beam_EmitterGlow", [(0, -0.014), (0.022, -0.008), (0.026, 0.0), (0.020, 0.010), (0, 0.016)], M["Energy_Hot"], work, 16, (0, F(LENS_F - 0.004), P), (0, F(LENS_F + 0.026), P))
hglow = L.tube("Beam_HeatGlow", 0.0545, 0.0515, (0, F(0.200), P), (0, F(0.364), P), M["Energy_Hot"], work, seg=24)
col = L.gather(ID, [base, yaw, pitch, ring, core, shell, eglow, hglow] + fins)
root, pivot, socket = L.finish_rig(ID, col, base, yaw, pitch, pitch_range=(DN, UP))
scn.frame_set(1); bpy.context.view_layer.update()
L.attach(ring, pitch, (0, F(0.412), P))
for fb, s in zip(fins, (-1, 1)): L.attach(fb, pitch, (s*0.057, F(0.280), P + 0.034))
for o in (core, shell, eglow): L.attach(o, pitch, (0, F(LENS_F), P)); o.scale = (0.001,)*3
L.attach(hglow, pitch, (0, F(0.282), P)); hglow.scale = (0.001,)*3
muzzle = L.empty("Muzzle", pitch, (0, F(MUZZLE_F), 0), col, 'SINGLE_ARROW', 0.05, L.UP90)
L.root_props(root, "R4WeaponKind.BeamCutter")
L.set_props({pivot: {"role": "game-driven pivot", "physical": "(0,0.135,0)"}, yaw: {"role": "Yaw: rotate around local Y only"},
             pitch: {"role": "Pitch: rotate around local X only", "pitch_range_deg": f"-{DN:.0f}..+{UP:.0f}"},
             ring: {"role": "focus ring: rotates about local Z (fire axis) during warm-up / fire"},
             core: {"role": "beam core FX (0.70 m visual stub; game range 16 m drawn by runtime) - idle scale 0.001"},
             shell: {"role": "beam sheath FX near the lens - idle scale 0.001"}, eglow: {"role": "emitter warm-up glow FX - idle scale 0.001"},
             hglow: {"role": "housing heat glow FX - shown in Heat"}, socket: {"role": "mounting face centre, normal -Y, connects to cell (0,-1,0)"},
             muzzle: {"role": "beam origin (lens), +Z = beam direction", "physical": f"(0,0.135,{MUZZLE_F})"}})
for fb in fins: fb["role"] = f"radiator fin bank: rotates about local Z (upper hinge), opens {FIN_OPEN:.0f} deg on Heat/Cooldown"
PARTS = [base, yaw, pitch, ring] + fins; FX = [core, shell, eglow, hglow]
for o in PARTS + FX: L.shade_and_uv(o)
T = L.Timeline()
rest = [(yaw, "rotation_euler", 2, 0.0), (pitch, "rotation_euler", 0, 0.0), (ring, "rotation_euler", 1, 0.0), (fins[0], "rotation_euler", 1, 0.0), (fins[1], "rotation_euler", 1, 0.0)]
L.hold(T, rest, "Idle", 24, "rest hold, loopable")
L.standard_aim(T, yaw, pitch, UP, DN)
def sc3(o, f, v, interp='LINEAR'): L.key3(o, "scale", f, v if isinstance(v, tuple) else (v,)*3, interp)
def ringk(f, d, interp='LINEAR'): L.key(ring, "rotation_euler", 1, f, math.radians(d), interp)
s, e = T.clip("WarmUp", 18, desc="focus ring spins up 0 -> 180 deg, emitter glow grows")
for o in FX: sc3(o, s - 1, 0.001, 'CONSTANT'); sc3(o, s, 0.001, 'CONSTANT')
for t in range(0, 19): ringk(s + t, 180.0*(t/18.0)**2)
sc3(eglow, s, 0.001); sc3(eglow, e, 1.0); T.clips[-1]["render_frames"] = [e - 2]
s, e = T.clip("Fire", 24, loop=True, desc="continuous beam loop: core + sheath on (pulse), ring 1 rev / 24 f (6-fold symmetric, seamless)")
for t in range(0, 25): ringk(s + t, 360.0*t/24.0)
sc3(eglow, s, 1.0, 'CONSTANT'); sc3(eglow, e, 1.0, 'CONSTANT')
for t, k in ((0, 1.0), (6, 0.82), (12, 1.0), (18, 0.82), (24, 1.0)):
    sc3(core, s + t, (k, 1.0, k)); sc3(shell, s + t, (1.0 + (1.0 - k), 1.0, 1.0 + (1.0 - k)))
T.clips[-1]["render_frames"] = [s + 3]; HERO = s + 3
s, e = T.clip("Heat", 24, desc="sustained fire: housing heat glow on, radiator fins open 35 deg, beam stays on")
for t in range(0, 25): ringk(s + t, 360.0*t/24.0)
for o in (core, shell, eglow): sc3(o, s, 1.0, 'CONSTANT'); sc3(o, e, 1.0, 'CONSTANT')
sc3(hglow, s - 1, 0.001, 'CONSTANT'); sc3(hglow, s + 4, 1.0, 'CONSTANT'); sc3(hglow, e, 1.0, 'CONSTANT')
for fb, sg in zip(fins, (-1, 1)):
    L.key(fb, "rotation_euler", 1, s, 0.0); L.key(fb, "rotation_euler", 1, s + 12, math.radians(-sg*FIN_OPEN)); L.key(fb, "rotation_euler", 1, e, math.radians(-sg*FIN_OPEN))
T.clips[-1]["render_frames"] = [s + 16]; HERO2 = s + 16
s, e = T.clip("Cooldown", 36, desc="trigger released: beam off, glow fades, fins stay open then close, ring coasts to rest")
for o in (core, shell): sc3(o, s, 0.001, 'CONSTANT')
sc3(eglow, s, 1.0); sc3(eglow, s + 10, 0.001)
sc3(hglow, s, 1.0, 'CONSTANT'); sc3(hglow, s + 20, 0.001, 'CONSTANT')
for t in range(0, 13): ringk(s + t, 360.0*t/24.0 - (360.0/24.0)*t*t/(2*12.0))
for fb, sg in zip(fins, (-1, 1)):
    L.key(fb, "rotation_euler", 1, s, math.radians(-sg*FIN_OPEN)); L.key(fb, "rotation_euler", 1, s + 24, math.radians(-sg*FIN_OPEN)); L.key(fb, "rotation_euler", 1, e, 0.0)
T.clips[-1]["render_frames"] = [s + 8]
for o in FX: sc3(o, T.end, 0.001, 'CONSTANT')
for o, pth, idx, v in rest[:2] + rest[3:]: L.key(o, pth, idx, T.end, v)
for o in [yaw, pitch, ring, core, shell, eglow, hglow] + fins: L.name_action(o, o.name + "_Master")
scn.frame_set(1)
S = dict(id=ID, kind="R4WeaponKind.BeamCutter", game_name="WPN-003 Beam Cutter", out=OUT, root=root, pivot=pivot, yaw=yaw, pitch=pitch, socket=socket,
         export=[root, base, socket, pivot, yaw, pitch, ring] + fins + [core, shell, eglow, hglow, muzzle], parts=PARTS, fx=FX, muzzles=[muzzle],
         static=[base], yawparts=[yaw], gun=[pitch, ring] + fins, pitch_range=(DN, UP), frame_end=T.end, clips=T.clips,
         clash_pairs=[(ring, pitch), (fins[0], pitch), (fins[1], pitch), (fins[0], yaw), (fins[1], yaw), (ring, yaw), (pitch, yaw), (pitch, base), (yaw, base)],
         hero_frame=HERO, hero_frame2=HERO2, tri_budget=20000, exits_note="1 lens = 1 beam origin (Muzzle)",
         anim_extra={"beam_note": "Beam_Core is a 0.70 m visual stub; the gameplay beam (16 m, heat, cooling on release) stays in runtime code"},
         notes=["Focus ring end angle in Cooldown is 180 deg (6-fold symmetric: identical to rest)."])
Q.run(S)
