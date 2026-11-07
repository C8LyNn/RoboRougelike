# R3PartKind.OmniDrive / MOV005 - "Napęd omni" - mecanum wheel: 10 free rollers at 45 deg between two side plates, driven
# by a single-sided hub motor pod (+X inner side). Not a tyre: the rollers are the contact surface.
# Runtime contract (R41PartGeometry.Payload): cylinder d 0.468, width 0.187, axle = X, centre 0.208 m below the block.
import sys, os, math; sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import importlib, bpy, numpy as np, r4lib as L, r4qa as Q; importlib.reload(L); importlib.reload(Q)
from mathutils import Vector, Matrix
from r4lib import F, M, box, cyl, cone, tube, hull, prism_x, revolve, hexbolt, Part, new_col
OUT = sys.argv[sys.argv.index("--") + 1]
L.reset_scene(); L.init_palette(); scn = bpy.context.scene
work = new_col("_build_tmp"); ID = "R4M_OmniDrive"
AZ = -0.208; RT = 0.234; HW = 0.0935; NR = 10; R0 = RT - 0.032; RL = 0.068
# ---- static motor pod on +X with finned motor can, upper bracket to a dovetail mount rail at the cell top
hous = cyl(ID + "_MotorPod", 0.105, (0.110, 0, AZ), (0.250, 0, AZ), M["Armor_Dark"], work, seg=40, bevel=0.008)
h = Part(work, "h")
for k in range(12):
    a = 2*math.pi*k/12
    hull(f"fin{k}", [(x, F(rr*math.cos(a + d)), AZ + rr*math.sin(a + d)) for x in (0.140, 0.236) for rr in (0.100, 0.122) for d in (-0.05, 0.05)], M["Steel_Light"], h.U)
tube("pod_team", 0.107, 0.095, (0.118, 0, AZ), (0.134, 0, AZ), M["TeamColor_Red"], h.Pc, seg=40)
cyl("pod_cap", 0.070, (0.248, 0, AZ), (0.262, 0, AZ), M["Armor_Gunmetal"], h.U, seg=32, bevel=0.004)
for k in range(6):
    a = 2*math.pi*(k + 0.5)/6
    hexbolt("cap_bolt", (0.260, F(0.052*math.cos(a)), AZ + 0.052*math.sin(a)), (0.266, F(0.052*math.cos(a)), AZ + 0.052*math.sin(a)), M["Steel_Light"], h.Pc, r=0.005)
hull("bracket", [(x, F(f), z) for x in (0.150, 0.250) for (f, z) in ((-0.070, AZ + 0.090), (0.070, AZ + 0.090), (-0.050, 0.200), (0.050, 0.200))], M["Armor_Gunmetal"], h.U, bevel=0.005)
box("rail", 0.060, 0.262, F(-0.120), F(0.120), 0.196, 0.236, M["Armor_Dark"], h.U, bevel=0.004)
hull("dovetail", [(x, F(f), z) for x in (0.070, 0.252) for (f, z) in ((-0.090, 0.235), (0.090, 0.235), (-0.065, 0.268), (0.065, 0.268))], M["Armor_Plate_Light"], h.U, bevel=0.002)
box("rail_haz", 0.072, 0.250, F(0.1215), F(0.1205), 0.200, 0.232, M["Hazard_Yellow"], h.Pc)
for k in range(5):
    x0 = 0.074 + k*0.036
    hull(f"rail_chev{k}", [(xx, F(ff), zz) for ff in (0.1210, 0.1230) for (xx, zz) in ((x0, 0.201), (x0 + 0.012, 0.201), (x0 + 0.026, 0.231), (x0 + 0.014, 0.231))], M["Armor_Dark"], h.Pc)
for f_ in (-0.090, 0.090): hexbolt("rail_bolt", (0.200, F(f_), 0.2660), (0.200, F(f_), 0.2735), M["Steel_Light"], h.Pc, r=0.0075)
cyl("pod_bore", 0.0245, (0.100, 0, AZ), (0.150, 0, AZ), M["Armor_Dark"], h.D, seg=20)
h.build(hous)
# ---- hub: drum + two lightened side plates + roller fork lugs + drive shaft into the pod
hub = cyl(ID + "_Hub", 0.085, (-0.080, 0, AZ), (0.080, 0, AZ), M["Armor_Gunmetal"], work, seg=40, bevel=0.005)
p = Part(work, "p")
for s in (-1, 1):
    cyl(f"plate{s}", 0.205, (s*0.080, 0, AZ), (s*HW, 0, AZ), M["Armor_Plate_Light"], p.U, seg=60, bevel=0.003)
    for k in range(5):
        a = 2*math.pi*(k + 0.5)/5
        cyl(f"lighten{s}{k}", 0.034, (s*0.070, F(0.130*math.cos(a)), AZ + 0.130*math.sin(a)), (s*0.100, F(0.130*math.cos(a)), AZ + 0.130*math.sin(a)), M["Armor_Dark"], p.D, seg=16)
tube("hub_team", 0.0855, 0.070, (-0.020, 0, AZ), (0.020, 0, AZ), M["TeamColor_Red"], p.Pc, seg=40)
cyl("hub_cap", 0.050, (-0.108, 0, AZ), (-0.090, 0, AZ), M["Steel_Light"], p.U, seg=24, bevel=0.003)
cyl("shaft", 0.022, (0.090, 0, AZ), (0.140, 0, AZ), M["Steel_Light"], p.U, seg=20)
p.build(hub)
# ---- rollers (body of revolution, spin about their own local X)
def roller(name):
    prof = [(0, -RL), (0.016, -RL), (0.026, -RL*0.55), (0.032, 0.0), (0.026, RL*0.55), (0.016, RL), (0, RL)]
    ro = revolve(name, prof, M["Armor_Dark"], work, 20, (-1, 0, 0), (1, 0, 0))
    q = Part(work, name[-2:])
    for x0 in (-RL*0.32, RL*0.32):
        tube("roller_band", 0.0315, 0.020, (x0 - 0.004, 0, 0), (x0 + 0.004, 0, 0), M["Armor_Gunmetal"], q.U, seg=20)
    cyl("roller_pin", 0.0075, (-RL - 0.010, 0, 0), (RL + 0.010, 0, 0), M["Steel_Light"], q.U, seg=10)
    q.build(ro); return ro
rollers = [roller(f"{ID}_Roller_{i + 1:02d}") for i in range(NR)]
col = L.gather(ID, [hous, hub] + rollers)
root = L.empty(ID + "_ROOT", None, (0, 0, 0), col, 'PLAIN_AXES', 0.1)
hous.parent = root
hub.data.transform(Matrix.Translation((0, 0, -AZ))); hub.parent = root; hub.location = (0, 0, AZ)
mounts = []
for i, ro in enumerate(rollers):
    th = 2*math.pi*(i + 0.5)/NR      # one roller straight down at rest
    radial = Vector((0, -math.cos(th), math.sin(th))); tang = Vector((0, math.sin(th), math.cos(th)))
    axis = (Vector((1, 0, 0))*math.cos(math.radians(45)) + tang*math.sin(math.radians(45))).normalized()
    zax = radial; yax = zax.cross(axis).normalized()
    Mr = Matrix((axis, yax, zax)).transposed().to_4x4(); Mr.translation = radial*R0
    em = L.empty(f"RollerAxle_{i + 1:02d}", hub, (0, 0, 0), col, 'SINGLE_ARROW', 0.03)
    em.matrix_parent_inverse = Matrix.Identity(4); em.matrix_basis = Mr
    ro.parent = em; ro.location = (0, 0, 0); mounts.append(em)
axle_e = L.empty("Wheel_Axle", root, (0, 0, AZ), col, 'SINGLE_ARROW', 0.08, (0, math.radians(90), 0))
import r4port as P4; P4.port(hous, work, 1)
socket = L.empty("Socket_Mount", root, (0, 0, P4.FACE), col, 'SINGLE_ARROW', 0.08)
probe = L.empty("GroundProbe", root, (0, 0, AZ - RT), col, 'SINGLE_ARROW', 0.06, (math.pi, 0, 0))
L.root_props(root, "R3PartKind.OmniDrive", version="v2-port")
root["side"] = "motor pod on +X (inner side when mounted on the LEFT); right side = parent scale X -1"
L.set_props({hous: {"role": "static motor pod (+X) with fins, bracket and dovetail mount rail"},
             hub: {"role": "driven hub + side plates: rotates about local X (Wheel_Axle)"},
             axle_e: {"role": "axle axis marker: local +Z = file +X"}, socket: {"role": "dovetail rail top face, normal +Y"},
             probe: {"role": "ground contact (lowest roller) at rest (0,-0.442,0)"}})
for ro in rollers: ro["role"] = "free roller at 45 deg: spins about its own local X (RollerAxle_xx)"
PARTS = [hous, hub] + rollers
for o in [hous, hub, rollers[0]] + rollers[1:]: L.shade_and_uv(o)
T = L.Timeline(); STEP = 360.0/NR
def hk(f, d, i='LINEAR'): L.key(hub, "rotation_euler", 0, f, math.radians(d), i)
def rk(f, d, i='LINEAR'):
    for ro in rollers: L.key(ro, "rotation_euler", 0, f, math.radians(d), i)
s, e = T.clip("Idle", 24, loop=True, desc="parked"); hk(s, 0); hk(e, 0); rk(s, 0); rk(e, 0)
s, e = T.clip("Forward", 12, loop=True, desc="drive forward: hub turns 1 roller pitch (36 deg) / 12 f, rollers idle; seamless")
for t in (0, 6, 12): hk(s + t, STEP*t/12.0)
rk(s, 0); rk(e, 0); T.clips[-1]["render_frames"] = [s + 6]
s, e = T.clip("Strafe", 12, loop=True, desc="sideways: hub holds, rollers spin 180 deg / 12 f (bodies of revolution: seamless)")
hk(s, 0); hk(e, 0)
for t in (0, 6, 12): rk(s + t, 180.0*t/12.0)
T.clips[-1]["render_frames"] = [s + 6]
s, e = T.clip("Diagonal", 12, loop=True, desc="diagonal: hub 1 pitch / 12 f and rollers 90 deg / 12 f together")
for t in (0, 6, 12): hk(s + t, STEP*t/12.0); rk(s + t, 90.0*t/12.0)
T.clips[-1]["render_frames"] = [s + 6]
hk(T.end + 1, 0, 'CONSTANT'); rk(T.end + 1, 0, 'CONSTANT')
for o in [hub] + rollers: L.name_action(o, o.name + "_Master")
scn.frame_set(1)
def envelope():
    co = np.vstack([np.array([list(o.matrix_world @ v.co) for v in o.data.vertices]) for o in rollers])
    rr = np.hypot(co[:, 1], co[:, 2] - AZ)
    r = dict(runtime="Unity cylinder d 0.468, width 0.187 at (0,-0.208,0), axle X", roller_contact_radius_m=round(float(rr.max()), 4),
             radius_dev_mm=round((float(rr.max()) - RT)*1000, 2), roller_half_width_m=round(float(np.abs(co[:, 0]).max()), 4))
    r["PASS"] = abs(r["radius_dev_mm"]) <= 3 and r["roller_half_width_m"] <= HW + 0.001
    return r
S = dict(id=ID, kind="R3PartKind.OmniDrive", game_name="Napęd omni (MOV005)", out=OUT, root=root, pivot=axle_e, yaw=None, pitch=None, socket=socket, movement=True,
         export=[root, hous, hub] + mounts + rollers + [axle_e, socket, probe], parts=PARTS, fx=[], muzzles=[], housing=[hous], ground_parts=rollers,
         housing_strict=False, housing_note="motor pod sits on the axle 0.208 m below the cell centre and reaches -0.33 m (PARTSIZE #141)",
         ground_contact=dict(runtime_y=AZ - RT, tol_mm=3.0, source="R41PartGeometry.Payload omni cylinder"), envelope_fn=envelope,
         clearance_pairs=[(hub, hous), (rollers[0], hous), (rollers[0], hub)],
         clash_pairs=[(hub, hous)] + [(ro, hous) for ro in rollers] + [(ro, hub) for ro in rollers] + [(rollers[i], rollers[(i + 1) % NR]) for i in range(NR)],
         frame_end=T.end, clips=T.clips, hero_frame=1, tri_budget=26000, sil_center=(0.03, 0, -0.10), sil_scale=1.25,
         notes=["Rollers are separate free bodies on RollerAxle_xx empties at 45 deg; strafe = roller spin with the hub held."])
Q.run(S)
