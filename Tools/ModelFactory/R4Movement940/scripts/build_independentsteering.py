# R3PartKind.IndependentSteering / MOV011 - "Moduł niezależnego skrętu" - servo drum on the cell top, pinion driving a gear sector
# on a steering knuckle that yaws about a vertical kingpin; knuckle ends in a hub flange (+X) - no wheel (wheels are separate parts).
import sys, os, math; sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import importlib, bpy, numpy as np, r4lib as L, r4qa as Q; importlib.reload(L); importlib.reload(Q)
from mathutils import Vector, Matrix
from r4lib import F, M, box, cyl, cone, tube, hull, prism_x, revolve, hexbolt, Part, new_col
OUT = sys.argv[sys.argv.index("--") + 1]
L.reset_scene(); L.init_palette(); scn = bpy.context.scene
work = new_col("_build_tmp"); ID = "R4M_IndependentSteering"
RS, RP, CX, ZG = 0.090, 0.034, 0.1265, 0.080; HZ = -0.180; YAW = 35.0; RATIO = RS/RP
# ---- static servo drum + motor + pinion bearing + scale ring
hous = cyl(ID + "_ServoDrum", 0.200, (0, 0, 0.118), (0, 0, 0.240), M["Armor_Gunmetal"], work, seg=48, bevel=0.008)
h = Part(work, "h")
cyl("cap", 0.150, (0, 0, 0.236), (0, 0, 0.268), M["Armor_Dark"], h.U, seg=48, bevel=0.005)
cyl("cap_plate", 0.105, (0, 0, 0.262), (0, 0, 0.272), M["Armor_Plate_Light"], h.U, seg=48, bevel=0.002)
tube("drum_team", 0.2015, 0.190, (0, 0, 0.180), (0, 0, 0.198), M["TeamColor_Red"], h.Pc, seg=48)
for k in range(24):
    a = 2*math.pi*k/24
    box(f"scale_tick{k}", 0.186, 0.200, -0.0025, 0.0025, 0.1175, 0.1205, M["Hazard_Yellow"] if k % 6 == 0 else M["Steel_Light"], h.Pc).data.transform(Matrix.Rotation(a, 4, 'Z'))
cyl("motor", 0.052, (CX + 0.010, -0.150, 0.180), (CX + 0.010, 0.150, 0.180), M["Armor_Dark"], h.U, seg=32, bevel=0.006)
for k in range(8):
    yy = -0.120 + k*0.034
    tube(f"motor_fin{k}", 0.060, 0.050, (CX + 0.010, yy, 0.180), (CX + 0.010, yy + 0.008, 0.180), M["Steel_Light"], h.U, seg=32)
cyl("pinion_bearing", 0.030, (CX, 0, 0.100), (CX, 0, 0.122), M["Armor_Dark"], h.U, seg=24)
cyl("kingpin_bore", 0.0255, (0, 0, 0.095), (0, 0, 0.140), M["Armor_Dark"], h.D, seg=24)
cyl("pinion_bore", 0.0140, (CX, 0, 0.095), (CX, 0, 0.126), M["Armor_Dark"], h.D, seg=16)
for k in range(8):
    a = 2*math.pi*(k + 0.5)/8
    hexbolt("cap_bolt", (0.128*math.cos(a), 0.128*math.sin(a), 0.264), (0.128*math.cos(a), 0.128*math.sin(a), 0.2705), M["Steel_Light"], h.Pc, r=0.0075)
h.build(hous)
# ---- pinion (rotates about local Y Unity = Blender Z)
pin = cyl(ID + "_Pinion", RP - 0.006, (0, 0, ZG - 0.010), (0, 0, ZG + 0.010), M["Steel_Light"], work, seg=24)
p_ = Part(work, "p")
for k in range(12):
    a = 2*math.pi*k/12
    hull(f"ptooth{k}", [(rr*math.cos(a + d), rr*math.sin(a + d), z) for (rr, d) in ((RP - 0.007, -0.16), (RP - 0.007, 0.16), (RP, -0.07), (RP, 0.07)) for z in (ZG - 0.010, ZG + 0.010)], M["Steel_Light"], p_.U)
cyl("pinion_shaft", 0.012, (0, 0, ZG), (0, 0, 0.118), M["Steel_Light"], p_.U, seg=12)
p_.build(pin)
# ---- knuckle: kingpin, collar, gear sector (toward +X), spindle, C-arm, hub flange (+X), steering lever with ball joint
kn = cyl(ID + "_Knuckle", 0.024, (0, 0, -0.020), (0, 0, 0.132), M["Steel_Light"], work, seg=24)
k_ = Part(work, "k")
cyl("collar", 0.055, (0, 0, ZG - 0.012), (0, 0, ZG + 0.012), M["Armor_Dark"], k_.U, seg=32, bevel=0.003)
hull("sector_web", [(rr*math.cos(a), rr*math.sin(a), z) for rr in (0.050, RS - 0.012) for a in (math.radians(-55), math.radians(55)) for z in (ZG - 0.0085, ZG + 0.0085)], M["Armor_Gunmetal"], k_.U)
for k in range(15):
    a = math.radians(-56 + 8*k)
    hull(f"stooth{k}", [(rr*math.cos(a + d), rr*math.sin(a + d), z) for (rr, d) in ((RS - 0.012, -0.040), (RS - 0.012, 0.040), (RS, -0.018), (RS, 0.018)) for z in (ZG - 0.008, ZG + 0.008)], M["Steel_Light"], k_.U)
cyl("spindle", 0.046, (0, 0, HZ - 0.040), (0, 0, ZG - 0.010), M["Armor_Gunmetal"], k_.U, seg=32, bevel=0.006)
tube("spindle_team", 0.0465, 0.038, (0, 0, -0.020), (0, 0, 0.000), M["TeamColor_Red"], k_.Pc, seg=32)
hull("c_arm", [(x, y, z) for (x, z) in ((0.020, HZ + 0.040), (0.020, HZ - 0.040), (0.118, HZ + 0.050), (0.118, HZ - 0.050)) for y in (-0.040, 0.040)], M["Armor_Dark"], k_.U, bevel=0.005)
cyl("hub", 0.060, (0.100, 0, HZ), (0.128, 0, HZ), M["Armor_Dark"], k_.U, seg=32, bevel=0.004)
cyl("flange", 0.080, (0.128, 0, HZ), (0.144, 0, HZ), M["Steel_Light"], k_.U, seg=40, bevel=0.003)
for k in range(5):
    a = 2*math.pi*k/5
    cyl(f"stud{k}", 0.0065, (0.142, 0.056*math.cos(a), HZ + 0.056*math.sin(a)), (0.166, 0.056*math.cos(a), HZ + 0.056*math.sin(a)), M["Steel_Light"], k_.U, seg=8)
hull("lever", [(x, y, z) for (x, y) in ((-0.018, 0.020), (0.018, 0.020), (-0.012, 0.130), (0.012, 0.130)) for z in (-0.070, -0.048)], M["Armor_Gunmetal"], k_.U, bevel=0.003)
revolve("ball", [(0, -0.018), (0.012, -0.014), (0.017, 0.0), (0.012, 0.014), (0, 0.018)], M["Steel_Light"], k_.U, 16, (0, 0.130, -0.059 - 1), (0, 0.130, -0.059 + 1))
box("pointer", -0.186, -0.150, -0.004, 0.004, 0.106, 0.113, M["Hazard_Yellow"], k_.U)
hull("pointer_arm", [(x, y, z) for x in (-0.152, -0.040) for y in (-0.006, 0.006) for z in (0.098, 0.112)], M["Armor_Dark"], k_.U)
k_.build(kn)
col = L.gather(ID, [hous, pin, kn])
root = L.empty(ID + "_ROOT", None, (0, 0, 0), col, 'PLAIN_AXES', 0.1)
hous.parent = root
pin.parent = root; pin.location = (CX, 0, 0)   # pinion geometry is built around its own axis
kn.parent = root
socket = L.empty("Socket_Mount", root, (0, 0, L.CELL_HALF), col, 'SINGLE_ARROW', 0.08)
kingpin = L.empty("Steer_Kingpin", root, (0, 0, 0), col, 'SINGLE_ARROW', 0.08, (math.radians(-90), 0, 0))
hubm = L.empty("Hub_Mount", kn, (0.166, 0, HZ), col, 'SINGLE_ARROW', 0.06, (0, math.radians(90), 0))
L.root_props(root, "R3PartKind.IndependentSteering")
L.set_props({hous: {"role": "static servo drum, motor, pinion bearing, yaw scale ring"},
             pin: {"role": f"servo pinion: rotates about local Y (Unity up); ratio {RATIO:.2f} : 1 to the knuckle"},
             kn: {"role": f"steering knuckle: yaws about local Y (kingpin through the cell centre), visual limit +-{YAW:.0f} deg"},
             socket: {"role": "cap top face, normal +Y"}, kingpin: {"role": "steering axis marker (file +Y)"}, hubm: {"role": "hub flange face (+X) for the steered wheel/part"}})
PARTS = [hous, pin, kn]
for o in PARTS: L.shade_and_uv(o)
T = L.Timeline()
def yk(f, d, i='BEZIER'):
    L.key(kn, "rotation_euler", 2, f, math.radians(d), i); L.key(pin, "rotation_euler", 2, f, math.radians(-d*RATIO), i)
s, e = T.clip("Idle", 24, loop=True, desc="centred"); yk(s, 0); yk(e, 0)
s, e = T.clip("SteerLeft", 12, desc=f"slew to +{YAW:.0f} deg and hold"); yk(s, 0); yk(s + 9, YAW); yk(e, YAW); T.clips[-1]["render_frames"] = [e]; HERO = e
s, e = T.clip("SteerRight", 12, desc=f"slew to -{YAW:.0f} deg and hold"); yk(s, 0); yk(s + 9, -YAW); yk(e, -YAW); T.clips[-1]["render_frames"] = [e]; HERO2 = e
s, e = T.clip("Slew", 48, loop=True, desc=f"lock-to-lock sweep +-{YAW:.0f} deg (seamless sine)")
for t in range(0, 49, 4): yk(s + t, YAW*math.sin(2*math.pi*t/48.0), 'LINEAR')
s, e = T.clip("Return", 12, desc=f"self-centre from +{YAW:.0f} deg"); yk(s, YAW); yk(e - 2, 0); yk(e, 0)
for o in (kn, pin): L.name_action(o, o.name + "_Master")
scn.frame_set(1)
S = dict(id=ID, kind="R3PartKind.IndependentSteering", game_name="Moduł niezależnego skrętu (MOV011)", out=OUT, root=root, pivot=kingpin, yaw=None, pitch=None, socket=socket, movement=True,
         export=[root, hous, pin, kn, socket, kingpin, hubm], parts=PARTS, fx=[], muzzles=[], housing=[hous], ground_parts=[],
         ground_note="no ground contact: the steered wheel/part on Hub_Mount touches the ground", housing_strict=True,
         clearance_pairs=[(kn, hous), (pin, kn)], clash_pairs=[(kn, hous), (pin, hous), (pin, kn)],
         frame_end=T.end, clips=T.clips, hero_frame=HERO, hero_frame2=HERO2, tri_budget=20000, sil_center=(0.02, 0, 0.0), sil_scale=0.95,
         notes=["No runtime placeholder geometry exists for IndependentSteering (block only); yaw limit is visual - SteeringMode / IndependentYaw stay runtime."])
Q.run(S)
