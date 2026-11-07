# R3PartKind.Gyroscope / MOV004 - "Żyroskop" - open gimbal: hanging U-yoke, outer ring (about X), inner ring (about Y,
# Blender) and a flywheel rotor on its own axle; counter-rotating stabilisation, precession correction, spin up / down.
import sys, os, math; sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import importlib, bpy, numpy as np, r4lib as L, r4qa as Q; importlib.reload(L); importlib.reload(Q)
from mathutils import Vector, Matrix
from r4lib import F, M, box, cyl, cone, tube, hull, prism_x, revolve, hexbolt, Part, new_col
OUT = sys.argv[sys.argv.index("--") + 1]
L.reset_scene(); L.init_palette(); scn = bpy.context.scene
work = new_col("_build_tmp"); ID = "R4M_Gyroscope"
R1, R2, RR = 0.218, 0.172, 0.122
# ---- static yoke hanging from a round top mount
hous = cyl(ID + "_Yoke", 0.150, (0, 0, 0.236), (0, 0, 0.262), M["Armor_Gunmetal"], work, seg=48, bevel=0.005)
h = Part(work, "h")
cyl("top_disc", 0.112, (0, 0, 0.258), (0, 0, 0.270), M["Armor_Plate_Light"], h.U, seg=48, bevel=0.003)
tube("top_team", 0.1515, 0.136, (0, 0, 0.242), (0, 0, 0.256), M["TeamColor_Red"], h.Pc, seg=48)
for s in (-1, 1):
    hull(f"arm{s}", [(s*x, y, z) for x in (0.238, 0.262) for (y, z) in ((-0.060, 0.240), (0.060, 0.240), (-0.036, -0.030), (0.036, -0.030))], M["Armor_Dark"], h.U, bevel=0.004)
    hull(f"arm_brace{s}", [(s*x, y, z) for x in (0.150, 0.250) for (y, z) in ((-0.030, 0.240), (0.030, 0.240), (-0.020, 0.200), (0.020, 0.200))], M["Armor_Dark"], h.U, bevel=0.003)
    cyl(f"bearing{s}", 0.034, (s*0.236, 0, 0), (s*0.268, 0, 0), M["Armor_Gunmetal"], h.U, seg=24, bevel=0.003)
    tube(f"bearing_glow{s}", 0.026, 0.018, (s*0.266, 0, 0), (s*0.2705, 0, 0), M["Energy_Hot"], h.Pc, seg=24)
    cyl(f"bearing_bore{s}", 0.0135, (s*0.226, 0, 0), (s*0.250, 0, 0), M["Armor_Dark"], h.D, seg=16)
    for k in range(3):
        z0 = 0.040 + k*0.050
        hull(f"arm_haz{s}{k}", [(s*x, y, z) for x in (0.2610, 0.2635) for (y, z) in ((-0.030, z0), (-0.030, z0 + 0.014), (0.030, z0 + 0.034), (0.030, z0 + 0.020))], M["Hazard_Yellow"], h.Pc)
for k in range(6):
    a = 2*math.pi*(k + 0.5)/6
    hexbolt("top_bolt", (0.128*math.cos(a), 0.128*math.sin(a), 0.2600), (0.128*math.cos(a), 0.128*math.sin(a), 0.2665), M["Steel_Light"], h.Pc, r=0.0075)
h.build(hous)
# ---- outer ring: horizontal band (axis Z), trunnion pins on +-X
outer = tube(ID + "_OuterRing", R1 + 0.012, R1 - 0.010, (0, 0, -0.014), (0, 0, 0.014), M["Armor_Gunmetal"], work, seg=64)
o_ = Part(work, "o")
for s in (-1, 1):
    cyl(f"o_pin{s}", 0.012, (s*(R1 + 0.006), 0, 0), (s*0.246, 0, 0), M["Steel_Light"], o_.U, seg=16)
    cyl(f"o_boss{s}", 0.022, (s*(R1 - 0.012), 0, 0), (s*(R1 + 0.016), 0, 0), M["Armor_Dark"], o_.U, seg=20)
    cyl(f"o_ibore{s}", 0.0095, (0, s*(R1 - 0.020), 0), (0, s*(R1 + 0.020), 0), M["Armor_Dark"], o_.D, seg=12)
    cyl(f"o_iboss{s}", 0.020, (0, s*(R1 - 0.012), 0), (0, s*(R1 + 0.016), 0), M["Armor_Dark"], o_.U, seg=20)
tube("o_lip", R1 + 0.0125, R1 + 0.004, (0, 0, -0.004), (0, 0, 0.004), M["Steel_Light"], o_.U, seg=64)
for k in range(8):
    a = 2*math.pi*(k + 0.5)/8
    hexbolt("o_bolt", (R1*math.cos(a), R1*math.sin(a), 0.0135), (R1*math.cos(a), R1*math.sin(a), 0.019), M["Steel_Light"], o_.Pc, r=0.0045)
o_.build(outer)
# ---- inner ring: vertical band in the Y-Z plane (axis X), pins on +-Y into the outer ring, rotor bearings on +-Z
inner = tube(ID + "_InnerRing", R2 + 0.010, R2 - 0.008, (-0.012, 0, 0), (0.012, 0, 0), M["Armor_Plate_Light"], work, seg=56)
i_ = Part(work, "i")
for s in (-1, 1):
    cyl(f"i_pin{s}", 0.009, (0, s*(R2 + 0.004), 0), (0, s*(R1 + 0.006), 0), M["Steel_Light"], i_.U, seg=12)
    cyl(f"i_boss{s}", 0.018, (0, s*(R2 - 0.010), 0), (0, s*(R2 + 0.014), 0), M["Armor_Dark"], i_.U, seg=16)
    cyl(f"r_boss{s}", 0.020, (0, 0, s*(R2 - 0.012)), (0, 0, s*(R2 + 0.012)), M["Armor_Dark"], i_.U, seg=16)
    cyl(f"r_bore{s}", 0.0095, (0, 0, s*(R2 - 0.020)), (0, 0, s*(R2 + 0.020)), M["Armor_Dark"], i_.D, seg=12)
tube("i_team", R2 + 0.0105, R2 + 0.004, (-0.0125, 0, 0), (-0.004, 0, 0), M["TeamColor_Red"], i_.Pc, seg=56)
i_.build(inner)
# ---- flywheel rotor (axle Z Blender = file up)
rotor = revolve(ID + "_Rotor", [(0, -0.030), (0.040, -0.030), (0.050, -0.018), (RR - 0.012, -0.022), (RR, -0.016), (RR, 0.016), (RR - 0.012, 0.022), (0.050, 0.018), (0.040, 0.030), (0, 0.030)],
                M["Steel_Light"], work, 64, local=True)
r_ = Part(work, "r")
cyl("axle", 0.009, (0, 0, -(R2 - 0.004)), (0, 0, R2 - 0.004), M["Steel_Light"], r_.U, seg=12)
for k in range(12):
    a = 2*math.pi*k/12
    cyl(f"web_hole{k}", 0.012, (0.086*math.cos(a), 0.086*math.sin(a), -0.040), (0.086*math.cos(a), 0.086*math.sin(a), 0.040), M["Armor_Dark"], r_.D, seg=10)
tube("rim_team", RR + 0.0005, RR - 0.010, (0, 0, -0.006), (0, 0, 0.006), M["TeamColor_Red"], r_.Pc, seg=64)
cyl("hub", 0.040, (0, 0, -0.034), (0, 0, 0.034), M["Armor_Dark"], r_.U, seg=32, bevel=0.004)
r_.build(rotor)
col = L.gather(ID, [hous, outer, inner, rotor])
root = L.empty(ID + "_ROOT", None, (0, 0, 0), col, 'PLAIN_AXES', 0.1)
hous.parent = root; outer.parent = root; inner.parent = outer; rotor.parent = inner
import r4port as P4; P4.port(hous, work, 1)
socket = L.empty("Socket_Mount", root, (0, 0, P4.FACE), col, 'SINGLE_ARROW', 0.08)
L.root_props(root, "R3PartKind.Gyroscope", version="v2-port")
L.set_props({hous: {"role": "static U-yoke hanging from the round top mount"},
             outer: {"role": "outer gimbal ring: rotates about local X (file X)"}, inner: {"role": "inner gimbal ring (child of outer): rotates about file Z (Blender Y)"},
             rotor: {"role": "flywheel rotor (child of inner): spins about its axle, file Y at rest"}, socket: {"role": "top mount face, normal +Y"}})
PARTS = [hous, outer, inner, rotor]
for o in PARTS: L.shade_and_uv(o)
T = L.Timeline(); W_ = 90.0/12.0
def ok_(f, d, i='BEZIER'): L.key(outer, "rotation_euler", 0, f, math.radians(d), i)
def ik(f, d, i='BEZIER'): L.key(inner, "rotation_euler", 1, f, math.radians(d), i)
def rk(f, d, i='LINEAR'): L.key(rotor, "rotation_euler", 2, f, math.radians(d), i)
s, e = T.clip("Idle", 24, loop=True, desc="stopped"); ok_(s, 0); ok_(e, 0); ik(s, 0); ik(e, 0); rk(s, 0); rk(e, 0)
s, e = T.clip("SpinUp", 24, desc="rotor spins up to working speed (ends at 90 deg = 3 web holes)")
for t in range(0, 25): rk(s + t, W_*t*t/(2*24.0))
ok_(s, 0); ok_(e, 0); ik(s, 0); ik(e, 0); T.clips[-1]["render_frames"] = [s + 18]
s, e = T.clip("Stabilize", 48, loop=True, desc="stabilising: rotor 90 deg / 12 f (12-fold, seamless) with counter-rotating ring sway: outer +-12 deg, inner -+8 deg")
for t in range(0, 49): rk(s + t, W_*t)
for t in range(0, 49, 4):
    ph = 2*math.pi*t/48.0; ok_(s + t, 12.0*math.sin(ph), 'LINEAR'); ik(s + t, -8.0*math.sin(ph), 'LINEAR')
T.clips[-1]["render_frames"] = [s + 12, s + 36]; HERO = s + 12
s, e = T.clip("Correct", 24, desc="precession correction after a hit: outer ring 25 deg, inner -20 deg, back to level")
for t in range(0, 25): rk(s + t, W_*t)
ok_(s, 0); ok_(s + 6, 25); ok_(s + 14, -6); ok_(e, 0); ik(s, 0); ik(s + 7, -20); ik(s + 15, 5); ik(e, 0)
T.clips[-1]["render_frames"] = [s + 6]; HERO2 = s + 6
s, e = T.clip("SpinDown", 36, desc="rotor runs down (ends 135 deg = rest-equivalent, 12-fold)")
for t in range(0, 37): rk(s + t, W_*t - W_*t*t/(2*36.0))
ok_(s, 0); ok_(e, 0); ik(s, 0); ik(e, 0); T.clips[-1]["render_frames"] = [s + 10]
rk(T.end + 1, 0, 'CONSTANT')
for o in (outer, inner, rotor): L.name_action(o, o.name + "_Master")
scn.frame_set(1)
S = dict(id=ID, kind="R3PartKind.Gyroscope", game_name="Żyroskop (MOV004)", out=OUT, root=root, pivot=socket, yaw=None, pitch=None, socket=socket, movement=True,
         export=[root, hous, outer, inner, rotor, socket], parts=PARTS, fx=[], muzzles=[], housing=[hous], ground_parts=[],
         ground_note="no ground contact (stabiliser inside the block volume; runtime placeholder sphere d 0.442 + ring d 0.598)",
         housing_strict=True, clearance_pairs=[(outer, hous), (inner, outer), (rotor, inner)],
         clash_pairs=[(outer, hous), (inner, hous), (rotor, hous), (inner, outer), (rotor, outer), (rotor, inner)],
         frame_end=T.end, clips=T.clips, hero_frame=HERO, hero_frame2=HERO2, tri_budget=20000, sil_center=(0, 0, 0.0), sil_scale=0.95,
         notes=["Runtime placeholder ring is d 0.598 (outside the 0.55 cell); the visual gimbal keeps every part inside the cell."])
Q.run(S)
