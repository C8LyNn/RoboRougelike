# R3PartKind.MagneticGrapple / MOV006 - "Hak magnetyczny" - box launcher with a forward launch tube, side cable spool (-X),
# electromagnet head with 3 hinged claws. Deploy -> Fire (head + cable out) -> Retract (spool winds in). The cable is a visual
# only; grappling stays in the runtime device.
import sys, os, math; sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import importlib, bpy, numpy as np, r4lib as L, r4qa as Q; importlib.reload(L); importlib.reload(Q)
from mathutils import Vector, Matrix
from r4lib import F, M, box, cyl, cone, tube, hull, prism_x, revolve, hexbolt, Part, new_col
OUT = sys.argv[sys.argv.index("--") + 1]
L.reset_scene(); L.init_palette(); scn = bpy.context.scene
work = new_col("_build_tmp"); ID = "R4M_MagneticGrapple"
YM = -0.360; HEAD_F = 0.414; RH = 0.070; SPX0, SPX1, SPY, SPZ, SPR = -0.240, -0.150, 0.040, -0.040, 0.085; SHOT = 0.90
# ---- static launcher: body, top mount saddle, launch tube, spool cheeks + cover
hous = hull(ID + "_Housing", [(x, F(f), z) for x in (-0.140, 0.140) for (f, z) in ((0.250, -0.120), (0.250, 0.120), (-0.150, -0.120), (-0.150, 0.150), (0.100, 0.150))], M["Armor_Gunmetal"], work, bevel=0.008)
h = Part(work, "h")
box("saddle", -0.110, 0.110, F(0.090), F(-0.110), 0.140, 0.262, M["Armor_Dark"], h.U, bevel=0.006)
box("saddle_plate", -0.095, 0.095, F(0.075), F(-0.095), 0.258, 0.270, M["Armor_Plate_Light"], h.U, bevel=0.003)
for s in (-1, 1): box(f"saddle_team{s}", s*0.1105, s*0.1135, F(0.060), F(-0.080), 0.180, 0.240, M["TeamColor_Red"], h.Pc)
cyl("tube", 0.092, (0, F(0.240), 0), (0, F(0.355), 0), M["Armor_Dark"], h.U, seg=40, bevel=0.006)
tube("tube_lip", 0.098, 0.074, (0, F(0.340), 0), (0, F(0.358), 0), M["Steel_Light"], h.U, seg=40)
tube("tube_haz", 0.0925, 0.080, (0, F(0.300), 0), (0, F(0.316), 0), M["Hazard_Yellow"], h.Pc, seg=40)
cyl("bore", 0.074, (0, F(0.200), 0), (0, F(0.380), 0), M["Armor_Dark"], h.D, seg=40)
for k in range(8):
    a = 2*math.pi*(k + 0.5)/8
    hexbolt("tube_bolt", (0.086*math.cos(a), F(0.262), 0.086*math.sin(a)), (0.096*math.cos(a), F(0.262), 0.096*math.sin(a)), M["Steel_Light"], h.Pc, r=0.005)
for x in (SPX0 - 0.012, SPX1 + 0.012):
    hull("spool_cheek", [(xx, F(f), z) for xx in (x - 0.006, x + 0.006) for (f, z) in ((-SPY - 0.110, SPZ - 0.110), (-SPY + 0.110, SPZ - 0.110), (-SPY + 0.110, SPZ + 0.060), (-SPY - 0.060, SPZ + 0.120))], M["Armor_Dark"], h.U, bevel=0.003)
hull("spool_cover", [(x, F(f), z) for x in (SPX0 - 0.018, SPX1 + 0.006) for (f, z) in ((-SPY - 0.060, SPZ + 0.100), (-SPY + 0.090, SPZ + 0.100), (-SPY + 0.090, SPZ + 0.125), (-SPY - 0.050, SPZ + 0.125))], M["Armor_Plate_Light"], h.U, bevel=0.003)
cyl("spool_bore", 0.0145, (SPX0 - 0.030, F(-SPY), SPZ), (SPX1 + 0.030, F(-SPY), SPZ), M["Armor_Dark"], h.D, seg=16)
hull("guide", [(x, F(f), z) for x in (-0.150, -0.110) for (f, z) in ((0.080, -0.030), (0.200, -0.030), (0.200, 0.010), (0.080, 0.010))], M["Armor_Dark"], h.U)
for f0 in (-0.100, 0.000, 0.100):
    box("body_slot", 0.120, 0.150, F(f0 + 0.030), F(f0), -0.080, 0.080, M["Armor_Dark"], h.D)
    box("body_glow", 0.1185, 0.1215, F(f0 + 0.028), F(f0 + 0.002), -0.078, 0.078, M["Energy_Hot"], h.Pc)
h.build(hous)
# ---- spool (spins about local X)
spool = cyl(ID + "_Spool", 0.055, (SPX0, 0, 0), (SPX1, 0, 0), M["Armor_Dark"], work, seg=32)
sp = Part(work, "s")
for x in (SPX0, SPX1 - 0.008):
    cyl("flange", SPR, (x, 0, 0), (x + 0.008, 0, 0), M["Armor_Gunmetal"], sp.U, seg=40, bevel=0.002)
for k in range(5):
    x0 = SPX0 + 0.012 + k*0.0145
    tube(f"wrap{k}", 0.066, 0.054, (x0, 0, 0), (x0 + 0.0125, 0, 0), M["Steel_Light"], sp.U, seg=32)
for k in range(4):
    a = 2*math.pi*k/4
    cyl(f"flange_hole{k}", 0.012, (SPX0 - 0.002, 0.042*math.cos(a), 0.042*math.sin(a)), (SPX0 + 0.010, 0.042*math.cos(a), 0.042*math.sin(a)), M["Armor_Dark"], sp.D, seg=10)
cyl("spool_axle", 0.013, (SPX0 - 0.026, 0, 0), (SPX1 + 0.026, 0, 0), M["Steel_Light"], sp.U, seg=16)
tube("spool_team", 0.0855, 0.070, (SPX1 - 0.0085, 0, 0), (SPX1 + 0.0005, 0, 0), M["TeamColor_Red"], sp.Pc, seg=40)
sp.build(spool)
# ---- electromagnet head + 3 claws
head = cyl(ID + "_Head", RH, (0, F(HEAD_F - 0.050), 0), (0, F(HEAD_F), 0), M["Armor_Dark"], work, seg=40, bevel=0.005)
hp = Part(work, "hd")
for k in range(3):
    tube(f"coil{k}", RH + 0.0005, RH - 0.012, (0, F(HEAD_F - 0.044 + k*0.012), 0), (0, F(HEAD_F - 0.038 + k*0.012), 0), M["Barrel_Heat"], hp.U, seg=40)
cyl("pole", 0.042, (0, F(HEAD_F - 0.004), 0), (0, F(HEAD_F + 0.002), 0), M["Steel_Light"], hp.U, seg=32, bevel=0.002)
tube("pole_team", 0.062, 0.048, (0, F(HEAD_F - 0.004), 0), (0, F(HEAD_F + 0.001), 0), M["TeamColor_Red"], hp.U, seg=40)
cyl("eye", 0.016, (0, F(HEAD_F - 0.080), 0), (0, F(HEAD_F - 0.049), 0), M["Steel_Light"], hp.U, seg=16)
for k in range(3):
    a = 2*math.pi*k/3 + math.pi/2
    hull(f"claw_lug{k}", [((RH - 0.010)*math.cos(a + d), F(f), (RH - 0.010)*math.sin(a + d)) for d in (-0.20, 0.20) for f in (HEAD_F - 0.048, HEAD_F - 0.034)] +
         [((RH + 0.004)*math.cos(a + d), F(f), (RH + 0.004)*math.sin(a + d)) for d in (-0.20, 0.20) for f in (HEAD_F - 0.048, HEAD_F - 0.034)], M["Armor_Gunmetal"], hp.U)
hp.build(head)
def claw(name):
    c = hull(name, [(x, y, z) for (x, y, z) in ((-0.014, 0.0, 0.0), (0.014, 0.0, 0.0), (-0.014, -0.070, 0.0), (0.014, -0.070, 0.0), (-0.009, -0.090, -0.016), (0.009, -0.090, -0.016),
                                               (-0.014, 0.0, 0.010), (0.014, 0.0, 0.010), (-0.014, -0.070, 0.010), (0.014, -0.070, 0.010))], M["Steel_Light"], work, bevel=0.0015)
    q = Part(work, "c" + name[-1])
    cyl("claw_knuckle", 0.0065, (-0.016, 0.0, 0.005), (0.016, 0.0, 0.005), M["Armor_Dark"], q.U, seg=10)
    q.build(c); return c
claws = [claw(f"{ID}_Claw_{k + 1}") for k in range(3)]
cable = L.cyl(ID + "_Cable", 0.0055, (0, 0, 0), (0, -1.0, 0), M["Steel_Light"], work, seg=8)
glow = L.cyl("Grapple_MagnetGlow", 0.044, (0, F(HEAD_F + 0.003), 0), (0, F(HEAD_F + 0.007), 0), M["Energy_Hot"], work, seg=32)
col = L.gather(ID, [hous, spool, head, cable, glow] + claws)
root = L.empty(ID + "_ROOT", None, (0, 0, 0), col, 'PLAIN_AXES', 0.1)
hous.parent = root
spool.parent = root; spool.location = (0, F(-SPY), SPZ)
head.data.transform(Matrix.Translation((0, -F(HEAD_F - 0.050), 0))); head.parent = root; head.location = (0, F(HEAD_F - 0.050), 0)
glow.data.transform(Matrix.Translation((0, -F(HEAD_F - 0.050), 0))); glow.parent = head; glow.location = (0, 0, 0); glow.scale = (0.001,)*3
hinges = []
for k, c in enumerate(claws):
    a = 2*math.pi*k/3 + math.pi/2; rd = Vector((math.cos(a), 0, math.sin(a))); yv = Vector((0, 1, 0)); xv = yv.cross(rd)
    Mh = Matrix((xv, yv, rd)).transposed().to_4x4(); Mh.translation = rd*(RH + 0.014) + Vector((0, -0.010, 0))
    em = L.empty(f"ClawHinge_{k + 1}", head, (0, 0, 0), col, 'SINGLE_ARROW', 0.02); em.matrix_basis = Mh
    c.parent = em; c.location = (0, 0, 0); c.rotation_euler = (0.0, 0, 0); hinges.append(em)
cable.parent = root; cable.location = (0, F(0.300), 0); cable.scale = (1, 0.001, 1)
import r4port as P4; P4.port(hous, work, 1)
socket = L.empty("Socket_Mount", root, (0, 0, P4.FACE), col, 'SINGLE_ARROW', 0.08)
muzzle = L.empty("Grapple_Muzzle", root, (0, F(0.362), 0), col, 'SINGLE_ARROW', 0.06, L.UP90)
L.root_props(root, "R3PartKind.MagneticGrapple", version="v2-port")
L.set_props({hous: {"role": "static launcher body, saddle, launch tube, spool cheeks"}, spool: {"role": "cable spool: rotates about local X"},
             head: {"role": "electromagnet head: translates along local +Z (file forward) on Fire"}, cable: {"role": "cable visual: local Y scale = paid-out length / 1 m"},
             glow: {"role": "magnet glow FX"}, socket: {"role": "saddle top face, normal +Y"}, muzzle: {"role": "launch tube mouth, +Z = launch direction"}})
for c in claws: c["role"] = "claw jaw: rotates about local X (ClawHinge_k): 0 deg closed, -38 deg open (outward)"
PARTS = [hous, spool, head] + claws; FX = [glow]
for o in PARTS + FX + [cable]: L.shade_and_uv(o)
T = L.Timeline(); H0 = head.location[1]
def hk(f, dy, i='BEZIER'): L.key(head, "location", 1, f, H0 - dy, i)
def ck(f, d, i='BEZIER'):   # +d = open (outward): local X rotation is -d
    for c in claws: L.key(c, "rotation_euler", 0, f, math.radians(-d), i)
def sk(f, d, i='LINEAR'): L.key(spool, "rotation_euler", 0, f, math.radians(d), i)
def cab(f, L_, i='LINEAR'): L.key3(cable, "scale", f, (1, max(0.001, L_), 1), i)
def fx(o, f, sc, i='LINEAR'): L.key3(o, "scale", f, (sc,)*3, i)
s, e = T.clip("Idle", 24, loop=True, desc="stowed: head seated in the tube mouth, claws closed")
hk(s, 0); hk(e, 0); ck(s, 0); ck(e, 0); sk(s, 0); sk(e, 0); cab(s, 0.001, 'CONSTANT'); cab(e, 0.001, 'CONSTANT'); fx(glow, s, 0.001, 'CONSTANT')
s, e = T.clip("Deploy", 10, desc="arm: head eases out 30 mm, claws open to 38 deg, magnet glow on")
hk(s, 0); hk(e, 0.030); ck(s, 0); ck(e, 38); sk(s, 0); sk(e, 0); cab(s, 0.001, 'CONSTANT'); cab(e, 0.001, 'CONSTANT')
fx(glow, s, 0.001); fx(glow, e, 1.0); T.clips[-1]["render_frames"] = [e]
s, e = T.clip("Fire", 10, desc="shot: head flies 0.90 m in 4 f, cable pays out, spool unwinds")
hk(s, 0.030, 'LINEAR'); hk(s + 4, 0.030 + SHOT, 'LINEAR'); hk(e, 0.030 + SHOT)
cab(s, 0.001 + 0.030 + 0.04, 'LINEAR'); cab(s + 4, 0.030 + SHOT + 0.04, 'LINEAR'); cab(e, 0.030 + SHOT + 0.04)
sk(s, 0); sk(s + 4, -math.degrees(SHOT/0.060)); sk(e, -math.degrees(SHOT/0.060))
ck(s, 38); ck(e, 38); fx(glow, s, 1.0, 'CONSTANT'); fx(glow, e, 1.0, 'CONSTANT')
T.clips[-1]["render_frames"] = [s + 5]; HERO = s + 5
s, e = T.clip("Retract", 30, desc="spool winds in, head returns and seats, claws close, glow off (spool ends at rest angle)")
hk(s, 0.030 + SHOT); hk(s + 24, 0.0); hk(e, 0.0)
cab(s, 0.030 + SHOT + 0.04, 'BEZIER'); cab(s + 24, 0.04, 'BEZIER'); cab(s + 25, 0.001, 'CONSTANT'); cab(e, 0.001, 'CONSTANT')
sk(s, -math.degrees(SHOT/0.060)); sk(s + 24, -720.0, 'BEZIER'); sk(e, -720.0)
ck(s, 38); ck(s + 22, 38); ck(e, 0); fx(glow, s, 1.0); fx(glow, s + 20, 0.001, 'CONSTANT')
T.clips[-1]["render_frames"] = [s + 10]; HERO2 = s + 10
sk(T.end + 1, 0, 'CONSTANT')
for o in [head, spool, cable, glow] + claws: L.name_action(o, o.name + "_Master")
scn.frame_set(1)
def envelope():
    co = np.array([list(o.matrix_world @ v.co) for o in (hous, head) for v in o.data.vertices]); fwd = -co[:, 1]
    r = dict(runtime="Unity cylinder d 0.338, length 0.208 along Z, centre (0,0,+0.312)", front_end_file_z=round(float(fwd.max()), 4), runtime_front_end_file_z=0.416,
             shot_length_m=SHOT, launch_dir_file=[0, 0, 1])
    r["front_dev_mm"] = round((r["front_end_file_z"] - 0.416)*1000, 2); r["PASS"] = abs(r["front_dev_mm"]) <= 5
    return r
S = dict(id=ID, kind="R3PartKind.MagneticGrapple", game_name="Hak magnetyczny (MOV006)", out=OUT, root=root, pivot=muzzle, yaw=None, pitch=None, socket=socket, movement=True,
         export=[root, hous, spool, head] + hinges + claws + [cable, glow, socket, muzzle], parts=PARTS, fx=FX, muzzles=[], housing=[hous], ground_parts=[],
         ground_note="no ground contact (front device)", housing_strict=False, housing_note="launch tube reaches 0.36 m forward (runtime grapple cylinder ends 0.416 m forward)",
         envelope_fn=envelope, clearance_pairs=[(spool, hous), (head, hous)],
         clash_pairs=[(spool, hous), (head, hous)] + [(c, hous) for c in claws] + [(c, head) for c in claws],
         frame_end=T.end, clips=T.clips, hero_frame=HERO, hero_frame2=HERO2, tri_budget=22000, sil_center=(-0.02, -0.08, 0.04), sil_scale=1.15,
         notes=["Cable object is a visual stretched along file +Z; no rope physics or new grapple mechanic."])
Q.run(S)
