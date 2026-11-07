# R4WeaponKind.EmpEmitter - "WPN-006 EMP Emitter" - radial pulse head: core in a cage, counter-rotating charge rings,
# 4 hinged antennas, LOS sensor on the yaw. No barrel and no projectile: EMP is an area effect (game: 9 m, LOS, 12 s cooldown).
import sys, os, math; sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import importlib, bpy, r4lib as L, r4qa as Q; importlib.reload(L); importlib.reload(Q)
from mathutils import Vector, Matrix
from r4lib import F, P, M, box, cyl, cone, tube, hull, prism_x, revolve, hexbolt, Part, new_col
OUT = sys.argv[sys.argv.index("--") + 1]
L.reset_scene(); L.init_palette(); scn = bpy.context.scene
work = new_col("_build_tmp"); ID = "R4W_EmpEmitter"
CZ = 0.232; ANT_R = 0.118; ANT_Z = 0.122; PED_Z = 0.108; ANT_A = [45.0, 135.0, 225.0, 315.0]; RL0, RU0 = 0.142, 0.186; SLIDE = 0.006
base, _ = L.build_base(ID + "_Base", work, vents=4, hazard_corners=True, extra_plates=True, glacis_top=0.190)
# ---- yaw: turntable, chamfered pedestal, LOS sensor head, core column, cage, top cap
yaw = L.turntable(ID + "_Yaw", work)
y = Part(work, "y")
hull("pedestal", L.chamfer_square(0.150, 0.050, 0.040) + L.chamfer_square(0.136, 0.046, PED_Z), M["Armor_Gunmetal"], y.U, bevel=0.005)
for k in range(4):
    made = [hull("ped_appl", [(x, yy, z) for x in (0.1395, 0.1475) for yy in (-0.060, 0.060) for z in (0.056, 0.098)], M["Armor_Plate_Light"], y.U, bevel=0.002),
            box("ped_team", 0.1470, 0.1495, -0.050, 0.050, 0.086, 0.093, M["TeamColor_Red"], y.Pc),
            box("ped_vent", 0.138, 0.152, -0.040, 0.040, 0.060, 0.078, M["Armor_Dark"], y.D),
            box("ped_glow", 0.1365, 0.1395, -0.038, 0.038, 0.062, 0.076, M["Energy_Hot"], y.Pc)]
    for yy in (-0.050, 0.050):
        made.append(hexbolt("ped_bolt", (0.147, yy, 0.064), (0.152, yy, 0.064), M["Steel_Light"], y.Pc))
    L.rot_z(made, k)
cyl("column", 0.036, (0, 0, PED_Z - 0.006), (0, 0, CZ - 0.020), M["Armor_Dark"], y.U, seg=24)
for zz in (0.160, 0.178):
    tube("col_band", 0.0385, 0.030, (0, 0, zz - 0.003), (0, 0, zz + 0.003), M["Steel_Light"], y.U, seg=24)
revolve("core_seat", [(0, CZ - 0.032), (0.040, CZ - 0.032), (0.052, CZ - 0.018), (0.030, CZ - 0.010), (0, CZ - 0.010)], M["Armor_Gunmetal"], y.U, 32, local=True)
revolve("core", [(0, CZ - 0.040), (0.022, CZ - 0.036), (0.034, CZ - 0.014), (0.036, CZ + 0.004), (0.026, CZ + 0.030), (0, CZ + 0.038)], M["Energy_Hot"], y.U, 24, local=True)
for k in range(4):
    a = math.radians(k*90.0)
    hull(f"cage_rib{k}", [(rr*math.cos(a) + d*math.sin(a), rr*math.sin(a) - d*math.cos(a), z) for rr in (0.044, 0.052) for d in (-0.005, 0.005) for z in (CZ - 0.022, CZ + 0.036)]
         + [(0.030*math.cos(a), 0.030*math.sin(a), CZ + 0.052)], M["Armor_Dark"], y.U)
tube("cap_ring", 0.056, 0.036, (0, 0, CZ + 0.040), (0, 0, CZ + 0.050), M["Armor_Gunmetal"], y.U, seg=32)
cyl("cap_dome", 0.024, (0, 0, CZ + 0.044), (0, 0, CZ + 0.060), M["Armor_Dark"], y.U, seg=24, bevel=0.004)
tube("cap_haz", 0.0565, 0.050, (0, 0, CZ + 0.0435), (0, 0, CZ + 0.0465), M["Hazard_Yellow"], y.Pc, seg=32)
for k in range(4):
    a = math.radians(k*90.0 + 45.0)
    hull(f"cap_strut{k}", [(rr*math.cos(a) + d*math.sin(a), rr*math.sin(a) - d*math.cos(a), z) for rr in (0.036, 0.054) for d in (-0.004, 0.004) for z in (CZ + 0.041, CZ + 0.049)]
         + [(rr*math.cos(a + 0.06), rr*math.sin(a + 0.06), CZ + 0.040) for rr in (0.020, 0.024)], M["Armor_Dark"], y.U)
# LOS sensor head on the front of the pedestal ("visible target" requirement)
box("sensor", -0.050, 0.050, F(0.170), F(0.118), 0.064, 0.104, M["Armor_Dark"], y.U, bevel=0.006)
box("sensor_eye", -0.036, 0.036, F(0.1715), F(0.1685), 0.074, 0.094, M["Sensor_Eye_Red"], y.U)
for s in (-1, 1):
    cyl(f"sensor_lens{s}", 0.006, (s*0.044, F(0.169), 0.084), (s*0.044, F(0.1735), 0.084), M["Sensor_Eye_Red"], y.U, seg=12)
box("sensor_haz", -0.040, 0.040, F(0.150), F(0.122), 0.1035, 0.1045, M["Hazard_Yellow"], y.Pc)
for k, a in enumerate(ANT_A):   # antenna hinge brackets
    ar = math.radians(a); cx, cy = ANT_R*math.cos(ar), ANT_R*math.sin(ar)
    tx, ty = -math.sin(ar), math.cos(ar)
    hull(f"ant_mount{k}", [(cx + tx*d + math.cos(ar)*rr, cy + ty*d + math.sin(ar)*rr, z) for d in (-0.016, 0.016) for rr in (-0.016, 0.014) for z in (PED_Z - 0.004, PED_Z + 0.003)],
         M["Armor_Dark"], y.U)
y.build(yaw)
# ---- charge rings (counter-rotating about the vertical axis, slide toward the core when charging)
def charge_ring(name, z):
    rg = L.tube(name, 0.084, 0.0405, (0, 0, z - 0.008), (0, 0, z + 0.008), M["Armor_Gunmetal"], work, seg=48)
    q = Part(work, "r" + name[-1])
    tube("ring_lip", 0.087, 0.078, (0, 0, z - 0.003), (0, 0, z + 0.003), M["Armor_Plate_Light"], q.U, seg=48)
    tube("ring_glow", 0.0430, 0.0395, (0, 0, z - 0.0035), (0, 0, z + 0.0035), M["Energy_Hot"], q.Pc, seg=48)
    for k in range(3):
        a = math.radians(k*120.0 + 30.0); ca, sa = math.cos(a), math.sin(a)
        hull(f"pod{k}", [(rr*ca + d*sa, rr*sa - d*ca, zz) for rr in (0.060, 0.094) for d in (-0.016, 0.016) for zz in (z - 0.013, z + 0.013)], M["Armor_Dark"], q.U, bevel=0.002)
        hull(f"pod_team{k}", [(rr*ca + d*sa, rr*sa - d*ca, zz) for rr in (0.0935, 0.0960) for d in (-0.010, 0.010) for zz in (z - 0.006, z + 0.006)], M["TeamColor_Red"], q.Pc)
    q.build(rg); return rg
ring_lo = charge_ring(ID + "_ChargeRing_Lower", RL0)
ring_up = charge_ring(ID + "_ChargeRing_Upper", RU0)
# ---- antennas (built pointing up, outward = local -Y; hinge at origin)
def antenna(name):
    a_ = cyl(name, 0.0065, (0, 0, 0.004), (0, 0, 0.165), M["Armor_Dark"], work, seg=10)
    q = Part(work, "a" + name[-1])
    cyl("ant_hub", 0.008, (-0.012, 0, 0.0), (0.012, 0, 0.0), M["Steel_Light"], q.U, seg=12)
    box("ant_root", -0.008, 0.008, -0.008, 0.008, 0.0, 0.030, M["Armor_Gunmetal"], q.U, bevel=0.002)
    for zz, rr in ((0.070, 0.020), (0.105, 0.016), (0.138, 0.012)):
        cyl("ant_disc", rr, (0, 0, zz - 0.0025), (0, 0, zz + 0.0025), M["Steel_Light"], q.U, seg=16)
    revolve("ant_tip", [(0, 0.160), (0.010, 0.166), (0.011, 0.172), (0.006, 0.180), (0, 0.182)], M["Energy_Hot"], q.U, 12, local=True)
    q.build(a_); return a_
ants = [antenna(f"{ID}_Antenna_{k + 1}") for k in range(4)]
# ---- FX
pulse = L.tube("EMP_PulseRing", 0.120, 0.104, (0, 0, -0.004), (0, 0, 0.004), M["Energy_Hot"], work, seg=64)
cflash = L.revolve("EMP_CoreFlash", [(0, -0.070), (0.046, -0.040), (0.062, 0.0), (0.046, 0.040), (0, 0.070)], M["Muzzle_Flash"], work, 16, local=True)
cglow = L.tube("EMP_ChargeGlow", 0.0395, 0.0375, (0, 0, -0.030), (0, 0, 0.030), M["Energy_Hot"], work, seg=32)
col = L.gather(ID, [base, yaw, ring_lo, ring_up, pulse, cflash, cglow] + ants)
root, pivot, socket = L.finish_rig(ID, col, base, yaw, None)
scn.frame_set(1); bpy.context.view_layer.update()
L.attach(ring_lo, yaw, (0, 0, RL0)); L.attach(ring_up, yaw, (0, 0, RU0))
for a_, ang in zip(ants, ANT_A):
    ar = math.radians(ang)
    a_.parent = yaw; a_.location = Vector((ANT_R*math.cos(ar), ANT_R*math.sin(ar), ANT_Z)) - Vector((0, 0, P))
    a_.rotation_euler = (0.0, 0.0, ar + math.pi/2)      # local -Y -> outward
for o, z in ((pulse, CZ), (cflash, CZ), (cglow, (RL0 + RU0)/2)):
    o.parent = yaw; o.location = (0, 0, z - P); o.scale = (0.001,)*3
muzzle = L.empty("Muzzle", yaw, (0, 0, CZ + 0.080 - P), col, 'SINGLE_ARROW', 0.06, (0, 0, 0))
L.root_props(root, "R4WeaponKind.EmpEmitter")
L.set_props({pivot: {"role": "game-driven pivot", "physical": "(0,0.135,0)"},
             yaw: {"role": "Yaw: rotate around local Y only (turns the LOS sensor; the pulse itself is radial)"},
             ring_lo: {"role": "lower charge ring: rotates about local Y, slides up when charging"}, ring_up: {"role": "upper charge ring: counter-rotates, slides down when charging"},
             pulse: {"role": "radial EMP shock ring FX: scale 0.001 idle -> 4.5 on discharge (visual only; game radius 9 m)"},
             cflash: {"role": "core discharge flash FX"}, cglow: {"role": "charge glow FX between the rings"},
             socket: {"role": "mounting face centre, normal -Y, connects to cell (0,-1,0)"},
             muzzle: {"role": "pulse origin above the core; local +Z = up (radial area effect, no projectile)", "physical": f"(0,{CZ + 0.080:.3f},0)"}})
for a_ in ants: a_["role"] = "antenna: tilts outward about its local X hinge on Charge/Discharge"
PARTS = [base, yaw, ring_lo, ring_up] + ants; FX = [pulse, cflash, cglow]
for o in PARTS + FX: L.shade_and_uv(o)
T = L.Timeline()
rest = [(yaw, "rotation_euler", 2, 0.0), (ring_lo, "rotation_euler", 2, 0.0), (ring_up, "rotation_euler", 2, 0.0),
        (ring_lo, "location", 2, 0.0), (ring_up, "location", 2, 0.0)] + [(a_, "rotation_euler", 0, 0.0) for a_ in ants]
for o, z in ((ring_lo, RL0), (ring_up, RU0)): o.location[2] = z - P
rest[3] = (ring_lo, "location", 2, RL0 - P); rest[4] = (ring_up, "location", 2, RU0 - P)
def rk(o, f, d, interp='LINEAR'): L.key(o, "rotation_euler", 2, f, math.radians(d), interp)
def fx(o, f, sc, interp='CONSTANT'): L.key3(o, "scale", f, (sc,)*3, interp)
s, e = T.clip("Idle", 48, loop=True, desc="standby: charge rings counter-rotate 120 deg (3-fold symmetric, seamless loop)")
for o in FX: fx(o, s, 0.001); fx(o, e, 0.001)
rk(ring_lo, s, 0.0); rk(ring_lo, e, 120.0); rk(ring_up, s, 0.0); rk(ring_up, e, -120.0)
for o, pth, idx, v in rest[3:]: L.key(o, pth, idx, s, v); L.key(o, pth, idx, e, v)
L.key(yaw, "rotation_euler", 2, s, 0.0); L.key(yaw, "rotation_euler", 2, e, 0.0)
L.standard_aim(T, yaw, None)
s, e = T.clip("Charge", 36, desc="charge: rings spin up (counter-rotating) and close toward the core, antennas tilt out 18 deg, charge glow rises")
rk(ring_lo, s, 0.0); rk(ring_up, s, 0.0)
for t in range(0, 37): rk(ring_lo, s + t, 10.0*t*t/36.0); rk(ring_up, s + t, -10.0*t*t/36.0)
L.key(ring_lo, "location", 2, s, RL0 - P); L.key(ring_lo, "location", 2, e, RL0 - P + SLIDE)
L.key(ring_up, "location", 2, s, RU0 - P); L.key(ring_up, "location", 2, e, RU0 - P - SLIDE)
for a_ in ants: L.key(a_, "rotation_euler", 0, s, 0.0); L.key(a_, "rotation_euler", 0, e, math.radians(18.0))
fx(cglow, s, 0.001, 'LINEAR'); fx(cglow, s + 6, 0.2, 'LINEAR'); fx(cglow, e, 1.0, 'LINEAR')
T.clips[-1]["render_frames"] = [e - 2]; HERO2 = e - 2
s, e = T.clip("EMP", 12, desc="discharge: core flash 2 f, radial shock ring expands to 4.5x and vanishes, antennas kick, rings snap back")
fx(cglow, s, 1.0); fx(cglow, s + 1, 0.001)
L.flash_keys(cflash, s, ((0, 1.0), (1, 0.8), (2, 0.001)))
for df, sc in ((0, 1.0), (2, 2.2), (4, 3.4), (6, 4.5)): fx(pulse, s + df, sc, 'LINEAR')
fx(pulse, s + 7, 0.001)
rk(ring_lo, s, 360.0); rk(ring_up, s, -360.0); rk(ring_lo, s + 3, 360.0 + 25.0, 'BEZIER'); rk(ring_up, s + 3, -360.0 - 25.0, 'BEZIER')
rk(ring_lo, e, 480.0, 'BEZIER'); rk(ring_up, e, -480.0, 'BEZIER')
L.key(ring_lo, "location", 2, s, RL0 - P + SLIDE); L.key(ring_lo, "location", 2, s + 3, RL0 - P, 'BEZIER'); L.key(ring_lo, "location", 2, e, RL0 - P)
L.key(ring_up, "location", 2, s, RU0 - P - SLIDE); L.key(ring_up, "location", 2, s + 3, RU0 - P, 'BEZIER'); L.key(ring_up, "location", 2, e, RU0 - P)
for a_ in ants:
    L.key(a_, "rotation_euler", 0, s, math.radians(18.0)); L.key(a_, "rotation_euler", 0, s + 2, math.radians(26.0)); L.key(a_, "rotation_euler", 0, e, math.radians(20.0))
T.clips[-1]["render_frames"] = [s + 3]; HERO = s + 3
s, e = T.clip("Recovery", 36, desc="recovery: antennas fold back, rings coast to rest (480 deg = 120 deg x 4, symmetric)")
rk(ring_lo, s, 480.0); rk(ring_up, s, -480.0); rk(ring_lo, s + 30, 480.0 + 0.0, 'BEZIER'); rk(ring_up, s + 30, -480.0, 'BEZIER')
rk(ring_lo, e, 480.0, 'CONSTANT'); rk(ring_up, e, -480.0, 'CONSTANT')
for a_ in ants: L.key(a_, "rotation_euler", 0, s, math.radians(20.0)); L.key(a_, "rotation_euler", 0, s + 24, 0.0); L.key(a_, "rotation_euler", 0, e, 0.0)
T.clips[-1]["render_frames"] = [s + 10]
for o in FX: fx(o, T.end, 0.001)
rk(ring_lo, T.end + 1, 0.0, 'CONSTANT'); rk(ring_up, T.end + 1, 0.0, 'CONSTANT')
for o, pth, idx, v in [rest[0]] + rest[3:]: L.key(o, pth, idx, T.end, v)
for o in [yaw, ring_lo, ring_up, pulse, cflash, cglow] + ants: L.name_action(o, o.name + "_Master")
scn.frame_set(1)
S = dict(id=ID, kind="R4WeaponKind.EmpEmitter", game_name="WPN-006 EMP Emitter", out=OUT, root=root, pivot=pivot, yaw=yaw, pitch=None, socket=socket,
         export=[root, base, socket, pivot, yaw, ring_lo, ring_up] + ants + FX + [muzzle], parts=PARTS, fx=FX, muzzles=[muzzle], muzzle_forward=[0, 1, 0],
         static=[base], yawparts=[yaw, ring_lo, ring_up] + ants, gun=[], pitch_range=(0.0, 0.0), frame_end=T.end, clips=T.clips,
         clash_pairs=[(ring_lo, yaw), (ring_up, yaw), (ring_lo, ring_up)] + [(a_, yaw) for a_ in ants] + [(a_, r_) for a_ in ants for r_ in (ring_lo, ring_up)] + [(yaw, base)],
         hero_frame=HERO, hero_frame2=HERO2, tri_budget=20000, lof_pose_note="rest pose; ray straight up from the pulse origin",
         exits_note="no barrel, no projectile: one radial pulse origin (Muzzle, +Y up)",
         anim_extra={"area_note": "EMP_PulseRing is a visual 0.5 m ring; game effect radius 9 m, LOS ('widoczny cel') and 12 s cooldown stay in runtime"},
         notes=["Pitch intentionally absent: EMP acts radially; Yaw only orients the LOS sensor.", "Ring angles at clip ends are multiples of 120 deg (3-fold symmetric pods)."])
Q.run(S)
