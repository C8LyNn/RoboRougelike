# R3PartKind.Suspension / MOV009 - "Aktywne zawieszenie" - trailing arm on a tower bracket, coil-over damper (body + sliding rod
# + coil rings), stub-axle hub (+X) where a wheel bolts on; QA sweeps a wheel envelope (r 0.39, w 0.364) on the hub.
# Runtime: decorative piston (R41PartGeometry); SuspensionMode/HeightOffset stay runtime values.
import sys, os, math; sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import importlib, bpy, numpy as np, r4lib as L, r4qa as Q; importlib.reload(L); importlib.reload(Q)
from mathutils import Vector, Matrix
from r4lib import F, M, box, cyl, cone, tube, hull, prism_x, revolve, hexbolt, Part, new_col
OUT = sys.argv[sys.argv.index("--") + 1]
L.reset_scene(); L.init_palette(); scn = bpy.context.scene
work = new_col("_build_tmp"); ID = "R4M_Suspension"
PV = Vector((0, -0.150, -0.150)); HUB = Vector((0, 0.150, -0.300)); UP = Vector((0, 0.090, 0.110)); LO = Vector((0, 0.060, -0.232))
BODY_L, ROD_L, COLLAR = 0.200, 0.215, 0.110
def ang_of(d): return math.atan2(d.y, -d.z)
def lower_at(th): return PV + Matrix.Rotation(th, 3, 'X') @ (LO - PV)
# ---- tower bracket (static)
hous = hull(ID + "_Tower", [(x, y, z) for (x, y, z) in [(sx*0.150, sy*0.210, 0.262) for sx in (-1, 1) for sy in (-1, 1)] + [(sx*0.110, y, -0.120) for sx in (-1, 1) for y in (-0.205, -0.080)] + [(sx*0.110, 0.150, 0.150) for sx in (-1, 1)]], M["Armor_Gunmetal"], work, bevel=0.006)
h = Part(work, "h")
box("top_plate", -0.120, 0.120, -0.170, 0.170, 0.258, 0.270, M["Armor_Plate_Light"], h.U, bevel=0.003)
box("top_team", -0.1515, -0.1485, -0.150, 0.150, 0.200, 0.240, M["TeamColor_Red"], h.Pc)
box("top_team2", 0.1485, 0.1515, -0.150, 0.150, 0.200, 0.240, M["TeamColor_Red"], h.Pc)
for s in (-1, 1):
    hull(f"pivot_lug{s}", [(s*x, PV.y + dy, PV.z + dz) for x in (0.085, 0.112) for (dy, dz) in ((-0.040, 0.060), (0.040, 0.060), (-0.030, -0.030), (0.030, -0.030))], M["Armor_Dark"], h.U, bevel=0.003)
    cyl(f"pivot_boss{s}", 0.032, (s*0.084, PV.y, PV.z), (s*0.118, PV.y, PV.z), M["Armor_Dark"], h.U, seg=24, bevel=0.003)
    tube(f"pivot_glow{s}", 0.025, 0.017, (s*0.116, PV.y, PV.z), (s*0.1195, PV.y, PV.z), M["Energy_Hot"], h.Pc, seg=24)
    cyl(f"pivot_bore{s}", 0.0145, (s*0.070, PV.y, PV.z), (s*0.125, PV.y, PV.z), M["Armor_Dark"], h.D, seg=16)
    hull(f"ear{s}", [(s*x, UP.y + dy, UP.z + dz) for x in (0.030, 0.048) for (dy, dz) in ((-0.030, 0.060), (0.030, 0.060), (-0.024, -0.008), (0.024, -0.008))], M["Armor_Dark"], h.Pc)
cyl("eye_pin", 0.0095, (-0.050, UP.y, UP.z), (0.050, UP.y, UP.z), M["Steel_Light"], h.Pc, seg=12)
box("shock_bay", -0.056, 0.056, -0.020, 0.200, -0.140, 0.200, M["Armor_Dark"], h.D)
for k in range(3):
    zz = 0.060 + k*0.050
    for s in (-1, 1): box(f"vent{s}{k}", s*0.130, s*0.160, -0.180, -0.080, zz, zz + 0.016, M["Armor_Dark"], h.D)
h.build(hous)
# ---- trailing arm (rotates about local X at the pivot): twin beams, hub carrier, stub axle (+X) with 5 studs
arm = cyl(ID + "_Arm", 0.026, (-0.082, PV.y, PV.z), (0.082, PV.y, PV.z), M["Steel_Light"], work, seg=20)
a_ = Part(work, "a")
for s in (-1, 1):
    hull(f"beam{s}", [(s*x, p.y + dy, p.z + dz) for x in (0.058, 0.080) for (p, dy, dz) in ((PV, 0.0, 0.024), (PV, 0.0, -0.024), (HUB, -0.020, 0.032), (HUB, -0.020, -0.032))], M["Armor_Dark"], a_.U, bevel=0.004)
cyl("arm_axle", 0.0125, (-0.124, PV.y, PV.z), (0.124, PV.y, PV.z), M["Steel_Light"], a_.U, seg=12)
hull("hub_carrier", [(x, HUB.y + dy, HUB.z + dz) for x in (-0.082, 0.082) for (dy, dz) in ((-0.050, 0.050), (0.050, 0.050), (0.050, -0.050), (-0.050, -0.050))], M["Armor_Gunmetal"], a_.U, bevel=0.008)
cyl("hub", 0.060, (0.080, HUB.y, HUB.z), (0.150, HUB.y, HUB.z), M["Armor_Dark"], a_.U, seg=32, bevel=0.004)
cyl("hub_flange", 0.085, (0.150, HUB.y, HUB.z), (0.168, HUB.y, HUB.z), M["Steel_Light"], a_.U, seg=40, bevel=0.003)
tube("hub_team", 0.0605, 0.050, (0.100, HUB.y, HUB.z), (0.118, HUB.y, HUB.z), M["TeamColor_Red"], a_.Pc, seg=32)
for k in range(5):
    a = 2*math.pi*k/5
    cyl(f"stud{k}", 0.0065, (0.166, HUB.y + 0.058*math.cos(a), HUB.z + 0.058*math.sin(a)), (0.192, HUB.y + 0.058*math.cos(a), HUB.z + 0.058*math.sin(a)), M["Steel_Light"], a_.U, seg=8)
hull("lower_ear", [(x, LO.y + dy, LO.z + dz) for x in (-0.022, 0.022) for (dy, dz) in ((-0.020, 0.020), (0.020, 0.020), (-0.020, -0.050), (0.020, -0.050))], M["Armor_Dark"], a_.U)
box("arm_haz", 0.0795, 0.0815, 0.040, 0.100, -0.268, -0.236, M["Hazard_Yellow"], a_.Pc)
a_.build(arm)
# ---- damper body (pivot at UP, points along local -Z), rod (pivot at LO, points along local +Z), coil rings (child of body)
body = cyl(ID + "_ShockBody", 0.034, (0, 0, -0.026), (0, 0, -BODY_L), M["Armor_Gunmetal"], work, seg=24, bevel=0.004)
b_ = Part(work, "b")
tube("top_eye", 0.020, 0.010, (-0.024, 0, 0), (0.024, 0, 0), M["Armor_Dark"], b_.U, seg=16)
hull("eye_web", [(x, y, z) for x in (-0.016, 0.016) for (y, z) in ((-0.012, -0.013), (0.012, -0.013), (-0.020, -0.032), (0.020, -0.032))], M["Armor_Dark"], b_.U)
tube("collar", 0.050, 0.033, (0, 0, -COLLAR + 0.006), (0, 0, -COLLAR - 0.006), M["TeamColor_Red"], b_.U, seg=24)
tube("body_band", 0.0345, 0.028, (0, 0, -0.060), (0, 0, -0.072), M["Steel_Light"], b_.U, seg=24)
cyl("rod_bore", 0.0175, (0, 0, -0.040), (0, 0, -BODY_L - 0.010), M["Armor_Dark"], b_.D, seg=16)
b_.build(body)
rod = cyl(ID + "_ShockRod", 0.013, (0, 0, 0.020), (0, 0, ROD_L), M["Steel_Light"], work, seg=16)
r_ = Part(work, "r")
tube("rod_eye", 0.018, 0.008, (-0.016, 0, 0), (0.016, 0, 0), M["Armor_Dark"], r_.U, seg=16)
hull("rod_web", [(x, y, z) for x in (-0.012, 0.012) for (y, z) in ((-0.010, 0.011), (0.010, 0.011), (-0.014, 0.026), (0.014, 0.026))], M["Armor_Dark"], r_.U)
tube("seat", 0.050, 0.014, (0, 0, 0.040), (0, 0, 0.050), M["Armor_Gunmetal"], r_.U, seg=24)
cyl("dust_boot", 0.020, (0, 0, 0.050), (0, 0, 0.090), M["Armor_Dark"], r_.U, seg=16)
r_.build(rod)
spring = None
rings = []
L0 = (LO - UP).length; SP0 = L0 - COLLAR - 0.050
for k in range(9):
    rg = tube(f"coil{k}", 0.046, 0.039, (0, 0, -0.004), (0, 0, 0.004), M["Hazard_Yellow"] if k in (0, 8) else M["Steel_Light"], work, seg=24)
    rings.append(rg)
spring = rings[0]; sP = Part(work, "sp")
for rg in rings[1:]:
    for c_ in rg.users_collection: c_.objects.unlink(rg)
    sP.U.objects.link(rg)
for k, rg in enumerate(rings[1:], 1): rg.data.transform(Matrix.Translation((0, 0, -SP0*k/8.0)))
hull("coil_tie", [(x, y, z) for x in (-0.004, 0.004) for y in (0.040, 0.045) for z in (0.0, -SP0)], M["Steel_Light"], sP.U)
sP.build(spring); spring.name = ID + "_Spring"; spring.data.name = spring.name
env = L.cyl("SWEEP_WheelEnvelope", 0.390, (0.192, 0, 0), (0.556, 0, 0), bpy.data.materials.new("Collider_Preview"), work, seg=32)
col = L.gather(ID, [hous, arm, body, rod, spring, env])
root = L.empty(ID + "_ROOT", None, (0, 0, 0), col, 'PLAIN_AXES', 0.1)
hous.parent = root
arm.data.transform(Matrix.Translation(-PV)); arm.parent = root; arm.location = PV
body.parent = root; body.location = UP; rod.parent = root
spring.parent = body; spring.location = (0, 0, -COLLAR - 0.006)
env.parent = arm; env.location = HUB - PV; env.display_type = 'WIRE'; env.hide_render = True
import r4port as P4; P4.port(hous, work, 1)
socket = L.empty("Socket_Mount", root, (0, 0, P4.FACE), col, 'SINGLE_ARROW', 0.08)
hubm = L.empty("Wheel_Axle", arm, HUB - PV + Vector((0.192, 0, 0)), col, 'SINGLE_ARROW', 0.06, (0, math.radians(90), 0))
L.root_props(root, "R3PartKind.Suspension", version="v2-port")
L.set_props({hous: {"role": "static tower bracket with arm pivot lugs, damper ear, bump stop"},
             arm: {"role": "trailing arm: rotates about local X at the pivot; +14 deg = bump (hub up), -10 deg = droop"},
             body: {"role": "damper body: rotates about local X at the upper eye, follows the rod"}, rod: {"role": "damper rod: positioned at the arm's lower eye, slides into the body"},
             spring: {"role": "coil rings (child of body): local Z scale = spring length ratio"}, socket: {"role": "top plate face, normal +Y"},
             hubm: {"role": "wheel mounting face on the stub axle, local +Z = file +X"}})
PARTS = [hous, arm, body, rod, spring]
for o in PARTS: L.shade_and_uv(o)
T = L.Timeline()
def pose(f, deg, i='LINEAR'):
    th = math.radians(deg); lo = lower_at(th); d = lo - UP; a = ang_of(d)
    L.key(arm, "rotation_euler", 0, f, th, i)
    L.key(body, "rotation_euler", 0, f, a, i)
    L.key3(rod, "location", f, tuple(lo), i); L.key(rod, "rotation_euler", 0, f, a, i)
    L.key3(spring, "scale", f, (1, 1, (d.length - COLLAR - 0.050)/SP0), i)
def run(s, n, fn):
    for t in range(0, n + 1): pose(s + t, fn(t/n))
s, e = T.clip("Idle", 24, loop=True, desc="static ride height"); pose(s, 0); pose(e, 0)
s, e = T.clip("Compress", 16, desc="bump: hub rises (arm +14 deg), damper shortens, back to ride height"); run(s, 16, lambda u: 14.0*math.sin(math.pi*u))
T.clips[-1]["render_frames"] = [s + 8]; HERO = s + 8
s, e = T.clip("Rebound", 16, desc="droop: hub drops (arm -10 deg), damper extends, back"); run(s, 16, lambda u: -10.0*math.sin(math.pi*u))
T.clips[-1]["render_frames"] = [s + 8]; HERO2 = s + 8
s, e = T.clip("ActiveLift", 24, desc="active height offset: arm to +8 deg and hold (SuspensionHeightOffset visual)"); run(s, 24, lambda u: 8.0*min(1.0, u*2.0))
T.clips[-1]["render_frames"] = [e]
s, e = T.clip("Cycle", 48, loop=True, desc="travel test loop: +-10 deg sine (seamless)"); run(s, 48, lambda u: 10.0*math.sin(2*math.pi*u))
pose(T.end + 1, 0, 'CONSTANT')
for o in (arm, body, rod, spring): L.name_action(o, o.name + "_Master")
scn.frame_set(1)
def envelope():
    out = {}
    for deg in (-10.0, 0.0, 14.0):
        d = (lower_at(math.radians(deg)) - UP).length
        out[f"arm_{deg:+.0f}deg"] = dict(damper_length_m=round(d, 4), rod_insertion_m=round(BODY_L + ROD_L - d, 4))
    ins = min(v["rod_insertion_m"] for v in out.values()); hub_travel = (Matrix.Rotation(math.radians(14), 3, 'X') @ (HUB - PV)).z - (Matrix.Rotation(math.radians(-10), 3, 'X') @ (HUB - PV)).z
    r = dict(runtime="decorative piston (cylinder d 0.156, l 0.468 at (0,-0.286,0)); support = MOV009 runtime", positions=out, min_rod_insertion_m=round(ins, 4),
             hub_vertical_travel_m=round(hub_travel, 4), wheel_envelope="SWEEP_WheelEnvelope r 0.39 w 0.364 on the hub (QA clash vs tower over all clips)")
    r["PASS"] = ins >= 0.030
    return r
S = dict(id=ID, kind="R3PartKind.Suspension", game_name="Aktywne zawieszenie (MOV009)", out=OUT, root=root, pivot=hubm, yaw=None, pitch=None, socket=socket, movement=True,
         export=[root, hous, arm, body, rod, spring, socket, hubm], parts=PARTS, fx=[], muzzles=[], housing=[hous], ground_parts=[],
         ground_note="no direct ground contact: the wheel bolted to Wheel_Axle touches the ground", housing_strict=False,
         housing_note="trailing arm/hub reach 0.35 m below the cell centre (runtime piston reaches -0.52 m)", envelope_fn=envelope,
         clearance_pairs=[(arm, hous), (body, hous), (spring, hous), (env, hous)],
         clash_pairs=[(arm, hous), (body, hous), (rod, hous), (spring, hous), (env, hous), (body, arm), (spring, arm)],
         frame_end=T.end, clips=T.clips, hero_frame=HERO, hero_frame2=HERO2, tri_budget=20000, sil_center=(0, 0.02, -0.05), sil_scale=1.15,
         notes=["Damper body/rod/spring follow the arm analytically each frame (no constraints needed in Unity)."])
Q.run(S)
