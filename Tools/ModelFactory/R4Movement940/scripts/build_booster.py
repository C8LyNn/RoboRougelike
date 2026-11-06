# R3PartKind.Booster / MOV001 - "Booster" - horizontal thrust engine clamped in a saddle: intake turbine, combustion chamber,
# bell nozzle with an 8-petal iris; exhaust to the rear (file -Z) -> thrust +Z (forward). Ignition / thrust / spool-down.
# Runtime contract (R41PartGeometry.Payload): cylinder d 0.338, length 0.416 along Z, centre 0.312 m behind the block.
import sys, os, math; sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import importlib, bpy, numpy as np, r4lib as L, r4qa as Q; importlib.reload(L); importlib.reload(Q)
from mathutils import Vector, Matrix
from r4lib import F, M, box, cyl, cone, tube, hull, prism_x, revolve, hexbolt, Part, new_col
OUT = sys.argv[sys.argv.index("--") + 1]
L.reset_scene(); L.init_palette(); scn = bpy.context.scene
work = new_col("_build_tmp"); ID = "R4M_Booster"
YE = 0.520; RIM = 0.150; NP = 8; NB = 11; YT = -0.170
def yz(y, r, a): return (r*math.sin(a), y, r*math.cos(a))
# ---- static: top pylon, saddle bands, chamber, bell nozzle, intake grille
hous = hull(ID + "_Housing", [(x, y, z) for (x, z) in ((-0.130, 0.262), (0.130, 0.262), (-0.090, 0.140), (0.090, 0.140)) for y in (-0.120, 0.110)], M["Armor_Gunmetal"], work, bevel=0.006)
h = Part(work, "h")
box("pylon_plate", -0.110, 0.110, -0.100, 0.090, 0.258, 0.270, M["Armor_Plate_Light"], h.U, bevel=0.003)
box("pylon_team", -0.1315, -0.1285, -0.090, 0.080, 0.180, 0.230, M["TeamColor_Red"], h.Pc)
box("pylon_team2", 0.1285, 0.1315, -0.090, 0.080, 0.180, 0.230, M["TeamColor_Red"], h.Pc)
cyl("chamber", 0.150, (0, -0.205, 0), (0, 0.262, 0), M["Armor_Dark"], h.U, seg=48, bevel=0.010)
for yb in (-0.090, 0.080):
    tube("saddle_band", 0.168, 0.148, (0, yb - 0.020, 0), (0, yb + 0.020, 0), M["Armor_Gunmetal"], h.U, seg=48)
    box("band_lug", -0.030, 0.030, yb - 0.020, yb + 0.020, 0.150, 0.180, M["Armor_Gunmetal"], h.U, bevel=0.003)
    for s in (-1, 1): hexbolt("band_bolt", (s*0.168, yb, 0.0), (s*0.174, yb, 0.0), M["Steel_Light"], h.Pc, r=0.007)
for k in range(10):
    a = 2*math.pi*k/10
    hull(f"cool_rib{k}", [yz(y, rr, a + d) for y in (-0.050, 0.040) for rr in (0.148, 0.160) for d in (-0.035, 0.035)], M["Steel_Light"], h.U)
tube("chamber_team", 0.1515, 0.140, (0, 0.120, 0), (0, 0.160, 0), M["TeamColor_Red"], h.Pc, seg=48)
tube("chamber_haz", 0.1515, 0.140, (0, 0.190, 0), (0, 0.204, 0), M["Hazard_Yellow"], h.Pc, seg=48)
revolve("bell", [(0.080, 0.258), (0.112, 0.258), (0.120, 0.300), (0.142, 0.420), (0.162, YE), (0.150, YE), (0.130, 0.425), (0.108, 0.310), (0.080, 0.290)],
        M["Barrel_Heat"], h.U, 48, (0, -1, 0), (0, 1, 0), closed_ring=True)
tube("throat_ring", 0.128, 0.104, (0, 0.270, 0), (0, 0.300, 0), M["Armor_Gunmetal"], h.U, seg=48)
for k in range(NP):
    a = 2*math.pi*k/NP
    hull(f"hinge_lug{k}", [yz(y, rr, a + d) for y in (YE - 0.022, YE + 0.002) for rr in (0.150, 0.170) for d in (-0.06, 0.06)], M["Armor_Gunmetal"], h.U)
tube("grille_ring", 0.152, 0.126, (0, -0.222, 0), (0, -0.200, 0), M["Steel_Light"], h.U, seg=48)
cyl("intake_cut", 0.128, (0, -0.230, 0), (0, YT + 0.034, 0), M["Armor_Dark"], h.D, seg=48)
for k in range(3):
    a = 2*math.pi*k/3
    hull(f"grille_bar{k}", [yz(y, rr, a + d) for y in (-0.218, -0.206) for (rr, d) in ((0.020, -0.25), (0.020, 0.25), (0.140, -0.04), (0.140, 0.04))], M["Steel_Light"], h.U)
cyl("grille_hub", 0.026, (0, -0.220, 0), (0, -0.204, 0), M["Steel_Light"], h.U, seg=20)
cyl("turbine_shaft", 0.014, (0, YT + 0.020, 0), (0, YT + 0.040, 0), M["Steel_Light"], h.U, seg=12)
h.build(hous)
# ---- turbine (spins about local Z = file forward axis)
tur = cyl(ID + "_Turbine", 0.034, (0, YT - 0.010, 0), (0, YT + 0.018, 0), M["Armor_Gunmetal"], work, seg=24, bevel=0.003)
t_ = Part(work, "t")
for k in range(NB):
    a = 2*math.pi*k/NB; pts = []
    for (rr, tw) in ((0.030, 38.0), (0.118, 24.0)):
        for side in (-1, 1):
            ch = 0.026; t0 = math.radians(tw); dy, dt = side*ch/2*math.sin(t0), side*ch/2*math.cos(t0)
            ang = a + dt/rr
            for th in (-0.0018, 0.0018): pts.append(yz(YT + 0.004 + dy, rr, ang + th/rr))
    hull(f"blade{k}", pts, M["Steel_Light"], t_.U)
cone("tur_spinner", 0.034, 0.010, (0, YT - 0.009, 0), (0, YT - 0.030, 0), M["TeamColor_Red"], t_.U, seg=24)
t_.build(tur)
# ---- iris petals (8): canonical petal, hinge along local X, extends along local +Y (rear)
def petal(name):
    pt = hull(name, [(x, y, z) for (x, y, z) in ((-0.040, 0.0, 0.0), (0.040, 0.0, 0.0), (-0.030, 0.070, 0.0), (0.030, 0.070, 0.0),
                                                 (-0.040, 0.0, 0.007), (0.040, 0.0, 0.007), (-0.030, 0.070, 0.007), (0.030, 0.070, 0.007))], M["Armor_Plate_Light"], work, bevel=0.0015)
    q = Part(work, "pt" + name[-2:])
    cyl("petal_knuckle", 0.0055, (-0.034, 0.0, 0.0035), (0.034, 0.0, 0.0035), M["Steel_Light"], q.U, seg=10)
    box("petal_rib", -0.006, 0.006, 0.006, 0.066, 0.0065, 0.010, M["Armor_Dark"], q.U)
    q.build(pt); return pt
petals = [petal(f"{ID}_Petal_{k + 1}") for k in range(NP)]
flame = L.cone("Booster_Flame", 0.105, 0.012, (0, YE, 0), (0, YE + 0.420, 0), M["Muzzle_Flash"], work, seg=24)
core = L.cone("Booster_Core", 0.090, 0.050, (0, 0.300, 0), (0, YE, 0), M["Energy_Hot"], work, seg=24)
col = L.gather(ID, [hous, tur, flame, core] + petals)
root = L.empty(ID + "_ROOT", None, (0, 0, 0), col, 'PLAIN_AXES', 0.1)
hous.parent = root
tur.data.transform(Matrix.Translation((0, -YT, 0))); tur.parent = root; tur.location = (0, YT, 0)
hinges = []
for k, pt in enumerate(petals):
    a = 2*math.pi*(k + 0.5)/NP; rd = Vector((math.sin(a), 0, math.cos(a))); yv = Vector((0, 1, 0)); xv = yv.cross(rd)
    Mh = Matrix((xv, yv, rd)).transposed().to_4x4(); Mh.translation = Vector((0, YE - 0.002, 0)) + rd*(RIM + 0.020)
    em = L.empty(f"PetalHinge_{k + 1}", root, (0, 0, 0), col, 'SINGLE_ARROW', 0.03); em.matrix_basis = Mh
    pt.parent = em; pt.location = (0, 0, 0); hinges.append(em)
for o, y0 in ((flame, YE), (core, 0.300)): o.data.transform(Matrix.Translation((0, -y0, 0))); o.parent = root; o.location = (0, y0, 0); o.scale = (0.001,)*3
socket = L.empty("Socket_Mount", root, (0, 0, L.CELL_HALF), col, 'SINGLE_ARROW', 0.08)
nozzle = L.empty("Thruster_Nozzle", root, (0, YE, 0), col, 'SINGLE_ARROW', 0.08, (math.radians(-90), 0, 0))
L.root_props(root, "R3PartKind.Booster")
L.set_props({hous: {"role": "static: pylon, saddle bands, combustion chamber, bell nozzle, intake grille"},
             tur: {"role": "intake turbine: rotates about local Z (file forward axis)"},
             flame: {"role": "exhaust flame FX (rear, file -Z) - idle scale 0.001"}, core: {"role": "chamber glow FX seen through the throat"},
             socket: {"role": "pylon top face, normal +Y"}, nozzle: {"role": "nozzle exit; local +Z = exhaust (file -Z) -> thrust +Z forward"}})
for pt in petals: pt["role"] = "iris petal: rotates about local X (PetalHinge_k); -12 deg closed, +24 deg open"
PARTS = [hous, tur] + petals; FX = [flame, core]
for o in PARTS + FX: L.shade_and_uv(o)
T = L.Timeline(); BL = 360.0/NB; W_ = 3*BL/12.0
def tk(f, d, i='LINEAR'): L.key(tur, "rotation_euler", 1, f, math.radians(d), i)
def pk(f, d, i='BEZIER'):
    for pt in petals: L.key(pt, "rotation_euler", 0, f, math.radians(d), i)
def fx(o, f, sc, i='LINEAR'): L.key3(o, "scale", f, sc if isinstance(sc, tuple) else (sc,)*3, i)
s, e = T.clip("Idle", 24, loop=True, desc="cold: iris closed (-12 deg), turbine stopped")
tk(s, 0); tk(e, 0); pk(s, -12); pk(e, -12); fx(flame, s, 0.001, 'CONSTANT'); fx(core, s, 0.001, 'CONSTANT')
s, e = T.clip("Ignition", 12, desc="ignition: iris snaps open, turbine spools up, chamber glow, flame lights (flicker)")
for t in range(0, 13): tk(s + t, W_*t*t/(2*12.0))
pk(s, -12); pk(s + 4, 24); pk(e, 24)
fx(core, s, 0.001); fx(core, s + 4, 1.0); fx(flame, s, 0.001, 'CONSTANT'); fx(flame, s + 4, (0.5, 0.6, 0.5)); fx(flame, s + 6, (1.1, 1.2, 1.1)); fx(flame, e, 1.0)
T.clips[-1]["render_frames"] = [s + 6]
s, e = T.clip("ThrustLoop", 12, loop=True, desc="sustained thrust: turbine 3 blades / 12 f (11-fold, seamless), flame pulses, iris open")
for t in range(0, 13): tk(s + t, W_*t)
pk(s, 24); pk(e, 24); fx(core, s, 1.0, 'CONSTANT'); fx(core, e, 1.0, 'CONSTANT')
for t, k in ((0, 1.0), (3, 0.92), (6, 1.06), (9, 0.95), (12, 1.0)): fx(flame, s + t, (k, k*1.04, k))
T.clips[-1]["render_frames"] = [s + 6]; HERO = s + 6
s, e = T.clip("SpoolDown", 24, desc="cut-off: flame out, glow fades, iris closes, turbine runs down (ends 3 blades on)")
for t in range(0, 25): tk(s + t, W_*t - W_*t*t/(2*24.0))
pk(s, 24); pk(s + 14, -12); pk(e, -12)
fx(flame, s, 1.0); fx(flame, s + 3, 0.001, 'CONSTANT'); fx(core, s, 1.0); fx(core, s + 12, 0.001, 'CONSTANT')
T.clips[-1]["render_frames"] = [s + 8]
tk(T.end + 1, 0, 'CONSTANT')
for o in [tur, flame, core] + petals: L.name_action(o, o.name + "_Master")
scn.frame_set(1)
def envelope():
    co = np.array([list(hous.matrix_world @ v.co) for v in hous.data.vertices]); eng = co[co[:, 2] < 0.150]
    rr = np.hypot(eng[:, 0], eng[:, 2])
    r = dict(runtime="Unity cylinder d 0.338, length 0.416 along Z, centre (0,0,-0.312)", engine_radius_m=round(float(rr.max()), 4),
             rear_end_file_z=round(float(-eng[:, 1].max()), 4), front_end_file_z=round(float(-eng[:, 1].min()), 4),
             runtime_rear_end_file_z=-0.520, thrust_dir_file=[0, 0, 1], exhaust_dir_file=[0, 0, -1])
    r["rear_dev_mm"] = round((r["rear_end_file_z"] + 0.520)*1000, 2)
    r["PASS"] = abs(r["rear_dev_mm"]) <= 3 and r["engine_radius_m"] <= 0.180
    return r
S = dict(id=ID, kind="R3PartKind.Booster", game_name="Booster (MOV001)", out=OUT, root=root, pivot=nozzle, yaw=None, pitch=None, socket=socket, movement=True,
         export=[root, hous, tur] + hinges + petals + FX + [socket, nozzle], parts=PARTS, fx=FX, muzzles=[], housing=[hous], ground_parts=[],
         ground_note="no ground contact (thrust device)", housing_strict=False, housing_note="bell nozzle reaches 0.52 m behind the cell centre (runtime engine cylinder ends there)",
         envelope_fn=envelope, clearance_pairs=[(tur, hous), (petals[0], hous)],
         clash_pairs=[(tur, hous)] + [(pt, hous) for pt in petals] + [(petals[i], petals[(i + 1) % NP]) for i in range(NP)],
         frame_end=T.end, clips=T.clips, hero_frame=HERO, tri_budget=26000, sil_center=(0, 0.16, 0.06), sil_scale=1.25,
         notes=["Force direction: exhaust along file -Z, thrust +Z (matches the runtime booster placed behind the block)."])
Q.run(S)
