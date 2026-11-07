# R3PartKind.JumpDrive / MOV002 - "Napęd skoku" - pneumatic stomp piston: octagonal column, 3 pressure tanks, piston sleeve,
# ribbed foot pad. Charge (pad draws up, tanks glow) -> Impulse (piston slams 0.10 m) -> Recovery. Visual of the existing jump
# impulse only (JumpDriveSettings: 1.2 m, 6 s cooldown) - no legs / no new mechanic.
import sys, os, math; sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import importlib, bpy, numpy as np, r4lib as L, r4qa as Q; importlib.reload(L); importlib.reload(Q)
from mathutils import Vector, Matrix
from r4lib import F, M, box, cyl, cone, tube, hull, prism_x, revolve, hexbolt, Part, new_col
OUT = sys.argv[sys.argv.index("--") + 1]
L.reset_scene(); L.init_palette(); scn = bpy.context.scene
work = new_col("_build_tmp"); ID = "R4M_JumpDrive"
ZB = -0.317; PAD_T = 0.030; RT = 0.200; STROKE = 0.100; DRAW = 0.025
def octo(r, z): return [(r*math.cos(math.radians(22.5 + 45*k)), r*math.sin(math.radians(22.5 + 45*k)), z) for k in range(8)]
hous = hull(ID + "_Housing", octo(0.150, -0.150) + octo(0.165, 0.180) + octo(0.150, 0.262), M["Armor_Gunmetal"], work, bevel=0.005)
h = Part(work, "h")
cyl("top_flange", 0.120, (0, 0, 0.255), (0, 0, 0.270), M["Armor_Plate_Light"], h.U, seg=8, bevel=0.003)
for k in range(3):
    a = math.radians(30 + 120*k); ca, sa = math.cos(a), math.sin(a)
    tube(f"lift_eye{k}", 0.024, 0.012, (0.090*ca - 0.008*sa, 0.090*sa + 0.008*ca, 0.248), (0.090*ca + 0.008*sa, 0.090*sa - 0.008*ca, 0.248), M["Steel_Light"], h.U, seg=16)
for k in range(8):
    made = [box("oct_panel", 0.150, 0.158, -0.040, 0.040, -0.120, 0.150, M["Armor_Dark"], h.U, bevel=0.002)]
    if k % 2 == 0: made.append(box("oct_team", 0.1575, 0.1600, -0.030, 0.030, 0.090, 0.130, M["TeamColor_Red"], h.Pc))
    else: made += [box("oct_vent", 0.146, 0.170, -0.026, 0.026, -0.080, 0.040, M["Armor_Dark"], h.D), box("oct_glow", 0.1450, 0.1480, -0.024, 0.024, -0.078, 0.038, M["Energy_Hot"], h.Pc)]
    for o in made: o.data.transform(Matrix.Rotation(math.radians(45*k), 4, 'Z'))
TANKS = []
for k in range(3):
    a = math.radians(90 + 120*k); cx, cy = RT*math.cos(a), RT*math.sin(a); TANKS.append((cx, cy))
    revolve(f"tank{k}", [(0, -0.190), (0.030, -0.188), (0.044, -0.170), (0.046, -0.150), (0.046, 0.130), (0.044, 0.150), (0.030, 0.168), (0, 0.170)], M["Armor_Plate_Light"], h.U, 24, (cx, cy, -1.0), (cx, cy, 1.0))
    for zb in (-0.110, 0.070):
        tube(f"tank_band{k}", 0.0485, 0.040, (cx, cy, zb - 0.012), (cx, cy, zb + 0.012), M["Armor_Dark"], h.U, seg=24)
        hull(f"tank_strap{k}", [(rr*math.cos(a) + d*math.sin(a), rr*math.sin(a) - d*math.cos(a), z) for rr in (0.140, RT - 0.040) for d in (-0.012, 0.012) for z in (zb - 0.010, zb + 0.010)], M["Armor_Dark"], h.U)
    tube(f"tank_haz{k}", 0.0465, 0.040, (cx, cy, -0.010), (cx, cy, 0.008), M["Hazard_Yellow"], h.Pc, seg=24)
    cyl(f"valve{k}", 0.012, (cx, cy, 0.168), (cx, cy, 0.190), M["Steel_Light"], h.U, seg=12)
    hull(f"pipe{k}", [(cx*t + 0.0, cy*t, 0.186 + dz) for t in (1.0, 0.62) for dz in (-0.006, 0.006)] + [(cx*t + d*math.sin(a), cy*t - d*math.cos(a), 0.186) for t in (1.0, 0.62) for d in (-0.006, 0.006)], M["Steel_Light"], h.U)
cyl("sleeve", 0.112, (0, 0, -0.245), (0, 0, -0.140), M["Armor_Dark"], h.U, seg=40, bevel=0.006)
tube("sleeve_lip", 0.120, 0.080, (0, 0, -0.250), (0, 0, -0.238), M["Steel_Light"], h.U, seg=40)
tube("sleeve_glow", 0.1125, 0.104, (0, 0, -0.200), (0, 0, -0.194), M["Energy_Hot"], h.Pc, seg=40)
cyl("rod_bore", 0.079, (0, 0, -0.260), (0, 0, -0.100), M["Armor_Dark"], h.D, seg=32)
h.build(hous)
# ---- piston rod + foot pad (moves along local Y)
pis = cyl(ID + "_Piston", 0.055, (0, 0, ZB + PAD_T - 0.002), (0, 0, -0.130), M["Steel_Light"], work, seg=32)
p = Part(work, "p")
cyl("pad", 0.240, (0, 0, ZB), (0, 0, ZB + PAD_T), M["Armor_Gunmetal"], p.U, seg=48, bevel=0.006)
tube("pad_rim", 0.242, 0.220, (0, 0, ZB + PAD_T - 0.010), (0, 0, ZB + PAD_T + 0.002), M["Hazard_Yellow"], p.U, seg=48)
for k in range(12):
    a = 2*math.pi*k/12
    hull(f"pad_rib{k}", [(rr*math.cos(a + d), rr*math.sin(a + d), z) for rr in (0.128, 0.228) for d in (-0.030*0.228/rr, 0.030*0.228/rr) for z in (ZB + PAD_T - 0.002, ZB + PAD_T + 0.012)], M["Armor_Dark"], p.U)
cyl("pad_boss", 0.075, (0, 0, ZB + PAD_T - 0.002), (0, 0, ZB + PAD_T + 0.030), M["Armor_Dark"], p.U, seg=32, bevel=0.004)
tube("rod_team", 0.0555, 0.045, (0, 0, -0.230), (0, 0, -0.216), M["TeamColor_Red"], p.Pc, seg=32)
p.build(pis)
glows = [L.tube(f"Jump_TankGlow_{k + 1}", 0.0475, 0.0455, (cx, cy, -0.090), (cx, cy, 0.050), M["Energy_Hot"], work, seg=24) for k, (cx, cy) in enumerate(TANKS)]
blast = L.tube("Jump_BlastRing", 0.300, 0.250, (0, 0, -0.006), (0, 0, 0.006), M["Muzzle_Flash"], work, seg=48)
col = L.gather(ID, [hous, pis, blast] + glows)
root = L.empty(ID + "_ROOT", None, (0, 0, 0), col, 'PLAIN_AXES', 0.1)
hous.parent = root
pis.data.transform(Matrix.Translation((0, 0, 0.130))); pis.parent = root; pis.location = (0, 0, -0.130)
for g, (cx, cy) in zip(glows, TANKS):
    g.data.transform(Matrix.Translation((-cx, -cy, 0.020))); g.parent = root; g.location = (cx, cy, -0.020); g.scale = (0.001,)*3
blast.parent = root; blast.location = (0, 0, ZB - STROKE); blast.scale = (0.001,)*3
import r4port as P4; P4.port(hous, work, 1)
socket = L.empty("Socket_Mount", root, (0, 0, P4.FACE), col, 'SINGLE_ARROW', 0.08)
probe = L.empty("GroundProbe", root, (0, 0, ZB), col, 'SINGLE_ARROW', 0.06, (math.pi, 0, 0))
L.root_props(root, "R3PartKind.JumpDrive", version="v2-port")
L.set_props({hous: {"role": "static: octagonal column, 3 pressure tanks, piston sleeve"},
             pis: {"role": "piston rod + foot pad: translates along local -Y on Impulse (0.10 m), draws up 0.025 m on Charge"},
             blast: {"role": "ground blast ring FX on Impulse - idle scale 0.001"}, socket: {"role": "top flange face, normal +Y"},
             probe: {"role": "pad underside at rest (0,-0.317,0); game ground probe distance 0.3 m"}})
for g in glows: g["role"] = "tank charge glow FX - idle scale 0.001"
PARTS = [hous, pis]; FX = glows + [blast]
for o in PARTS + FX: L.shade_and_uv(o)
T = L.Timeline(); Z0 = -0.130
def pz(f, dz, i='BEZIER'): L.key(pis, "location", 2, f, Z0 + dz, i)
def fx(o, f, sc, i='LINEAR'): L.key3(o, "scale", f, (sc,)*3, i)
s, e = T.clip("Idle", 24, loop=True, desc="ready"); pz(s, 0); pz(e, 0)
for o in FX: fx(o, s, 0.001, 'CONSTANT')
s, e = T.clip("Charge", 36, desc="pressurise: foot pad draws up 25 mm, tank glow rises (game cooldown 6 s)")
pz(s, 0); pz(s + 28, DRAW); pz(e, DRAW)
for g in glows: fx(g, s, 0.001); fx(g, s + 8, 0.25); fx(g, e, 1.0)
T.clips[-1]["render_frames"] = [e - 2]; HERO2 = e - 2
s, e = T.clip("Impulse", 8, desc="release: piston slams 100 mm below rest in 2 f, blast ring, tanks dump")
pz(s, DRAW, 'LINEAR'); pz(s + 2, -STROKE, 'LINEAR'); pz(s + 4, -STROKE + 0.008, 'BEZIER'); pz(e, -STROKE + 0.010)
for g in glows: fx(g, s, 1.0, 'CONSTANT'); fx(g, s + 2, 0.001, 'CONSTANT')
fx(blast, s + 1, 0.001, 'CONSTANT'); fx(blast, s + 2, 0.6); fx(blast, s + 5, 1.6); fx(blast, s + 6, 0.001, 'CONSTANT')
T.clips[-1]["render_frames"] = [s + 3]; HERO = s + 3
s, e = T.clip("Recovery", 30, desc="retract: piston returns to rest, column vents")
pz(s, -STROKE + 0.010); pz(s + 22, 0.0); pz(e, 0.0)
T.clips[-1]["render_frames"] = [s + 10]
for o in FX: fx(o, T.end, 0.001, 'CONSTANT')
for o in [pis] + FX: L.name_action(o, o.name + "_Master")
scn.frame_set(1)
def envelope():
    co = np.array([list(pis.matrix_world @ v.co) for v in pis.data.vertices]); rr = np.hypot(co[:, 0], co[:, 1])
    r = dict(runtime="Unity cylinder d 0.52, h 0.166 at (0,-0.234,0)", pad_radius_m=round(float(rr.max()), 4), pad_bottom_y=round(float(co[:, 2].min()), 4),
             impulse_stroke_m=STROKE, game_target_jump_height_m=1.2)
    r["bottom_dev_mm"] = round((r["pad_bottom_y"] - ZB)*1000, 2); r["PASS"] = abs(r["bottom_dev_mm"]) <= 3 and r["pad_radius_m"] <= 0.262
    return r
S = dict(id=ID, kind="R3PartKind.JumpDrive", game_name="Napęd skoku (MOV002)", out=OUT, root=root, pivot=probe, yaw=None, pitch=None, socket=socket, movement=True,
         export=[root, hous, pis] + FX + [socket, probe], parts=PARTS, fx=FX, muzzles=[], housing=[hous], ground_parts=[pis],
         housing_strict=True, ground_contact=dict(runtime_y=ZB, tol_mm=3.0, source="R41PartGeometry.Payload jump disc bottom"), envelope_fn=envelope,
         clearance_pairs=[(pis, hous)], clash_pairs=[(pis, hous)], frame_end=T.end, clips=T.clips, hero_frame=HERO, hero_frame2=HERO2, tri_budget=26000,
         sil_center=(0, 0, -0.05), sil_scale=1.10, notes=["Impulse stroke 0.10 m is visual; the jump force/height stay in JumpDrive runtime settings."])
Q.run(S)
