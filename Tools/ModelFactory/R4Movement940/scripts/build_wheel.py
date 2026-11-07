# R3PartKind.Wheel - "Koło" - fork-mounted wheel: lugged tyre, 6-spoke rim, hub with brake disc and team caliper.
# Runtime contract (R41PartGeometry.Payload): cylinder r 0.39 m, width 0.364 m, axle = block X, centre 0.26 m below the
# block centre, convex MeshCollider = the tyre. blender -b --factory-startup --python build_wheel.py -- <out_dir>
import sys, os, math; sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import importlib, bpy, numpy as np, r4lib as L, r4qa as Q; importlib.reload(L); importlib.reload(Q)
from mathutils import Vector, Matrix
from r4lib import F, M, box, cyl, cone, tube, hull, prism_x, revolve, hexbolt, Part, new_col
OUT = sys.argv[sys.argv.index("--") + 1]
L.reset_scene(); L.init_palette(); scn = bpy.context.scene
work = new_col("_build_tmp"); ID = "R4M_Wheel"
AZ = -0.260; RT = 0.390; HW = 0.182; LUGS = 20
def ax(x, r, phi):   # point on a circle of radius r around the axle (phi 0 = forward, 90 = up) at lateral x
    return (x, F(r*math.cos(math.radians(phi))), AZ + r*math.sin(math.radians(phi)))
# ---- mount: chamfered top plate with team stripe + two forged fork legs down to the axle, axle, team brake caliper
mount = hull(ID + "_Mount", [(x, y, z) for (x, z) in ((-0.250, 0.150), (0.250, 0.150), (-0.236, 0.262), (0.236, 0.262)) for y in (-0.110, 0.110)] + [(x, y, 0.190) for x in (-0.252, 0.252) for y in (-0.125, 0.125)], M["Armor_Gunmetal"], work, bevel=0.006)
mp = Part(work, "m")
box("top_plate", -0.120, 0.120, -0.080, 0.080, 0.258, 0.270, M["Armor_Plate_Light"], mp.U, bevel=0.003)
box("top_team", -0.240, 0.240, -0.126, -0.120, 0.170, 0.215, M["TeamColor_Red"], mp.Pc)
box("top_team2", -0.240, 0.240, 0.120, 0.126, 0.170, 0.215, M["TeamColor_Red"], mp.Pc)
for s in (-1, 1):
    for t in (-1, 1):
        hexbolt("plate_bolt", (s*0.095, t*0.058, 0.255), (s*0.095, t*0.058, 0.2735), M["Steel_Light"], mp.Pc, r=0.008)
    leg = [(s*x, F(f), z) for x in (0.202, 0.246) for (f, z) in ((-0.105, 0.160), (0.105, 0.160), (0.060, -0.200), (-0.060, -0.200))]
    hull(f"leg{s}", leg, M["Armor_Dark"], mp.U, bevel=0.004)
    cyl(f"leg_boss{s}", 0.052, (s*0.200, 0, AZ), (s*0.250, 0, AZ), M["Armor_Dark"], mp.U, seg=24, bevel=0.004)
    hull(f"leg_web{s}", [(s*x, F(f), z) for x in (0.204, 0.244) for (f, z) in ((-0.050, -0.190), (0.050, -0.190), (0.040, AZ), (-0.040, AZ))], M["Armor_Dark"], mp.U)
    for k in range(4):   # hazard bars on the outer leg face
        z0 = -0.110 + k*0.056
        hull(f"leg_haz{s}{k}", [(s*x, F(f), z) for x in (0.2380, 0.2492) for (f, z) in ((0.027, z0), (0.027, z0 + 0.020), (-0.027, z0 + 0.040), (-0.027, z0 + 0.020))], M["Hazard_Yellow"], mp.Pc)
    tube(f"boss_glow{s}", 0.040, 0.030, (s*0.2490, 0, AZ), (s*0.2525, 0, AZ), M["Energy_Hot"], mp.Pc, seg=24)
    for k in range(6):
        a = 2*math.pi*(k + 0.5)/6
        hexbolt(f"boss_bolt{s}{k}", (s*0.2485, F(0.040*math.cos(a)), AZ + 0.040*math.sin(a)), (s*0.2545, F(0.040*math.cos(a)), AZ + 0.040*math.sin(a)), M["Steel_Light"], mp.Pc, r=0.0045)
cyl("axle", 0.022, (-0.252, 0, AZ), (0.252, 0, AZ), M["Steel_Light"], mp.U, seg=20)
box("caliper", 0.150, 0.198, F(-0.060), F(0.060), AZ + 0.120, AZ + 0.205, M["TeamColor_Red"], mp.U, bevel=0.008)
box("caliper_arm", 0.170, 0.214, F(-0.030), F(0.030), AZ + 0.135, AZ + 0.190, M["Armor_Gunmetal"], mp.U, bevel=0.004)
for f_ in (-0.035, 0.035):
    hexbolt("cal_bolt", (0.196, F(f_), AZ + 0.175), (0.2015, F(f_), AZ + 0.175), M["Steel_Light"], mp.Pc)
box("cal_haz", 0.1485, 0.1505, F(-0.045), F(0.045), AZ + 0.130, AZ + 0.140, M["Hazard_Yellow"], mp.Pc)
mp.build(mount)
# ---- rotor: tyre (rubber = Armor_Dark) with 20 chevron lugs, rim barrel, 6 spokes, hub, brake disc
def prof_tyre():
    return [(0.250, -HW + 0.010), (0.300, -HW), (0.352, -HW + 0.006), (0.368, -HW + 0.022), (0.372, -0.040), (0.372, 0.040), (0.368, HW - 0.022), (0.352, HW - 0.006), (0.300, HW), (0.250, HW - 0.010)]
rotor = revolve(ID + "_Rotor", prof_tyre(), M["Armor_Dark"], work, 72, (-1, 0, AZ), (1, 0, AZ), closed_ring=True)
rotor.data.transform(Matrix.Translation((0, 0, 0)))
# the revolve was oriented along X around (0,0,AZ) by _orient(p0,p1): centre at x=0
rp = Part(work, "r")
for i in range(LUGS):
    ph = i*360.0/LUGS
    for s in (-1, 1):
        pts = [ax(s*x, r, ph + d + (s*4.0 if x > 0.08 else 0.0)) for x in (0.012, 0.168) for r in (0.362, RT) for d in (-3.2, 3.2)]
        hull(f"lug{i}{s}", pts, M["Armor_Dark"], rp.U)
    for s in (-1, 1):
        hull(f"shoulder{i}{s}", [ax(s*x, r, ph + d) for x in (0.150, 0.181) for r in (0.330, 0.368) for d in (-2.4, 2.4)], M["Armor_Dark"], rp.U)
tube("sidewall_ring_L", 0.322, 0.308, (-HW - 0.002, 0, AZ), (-HW + 0.004, 0, AZ), M["Armor_Gunmetal"], rp.U, seg=72)
tube("sidewall_ring_R", 0.322, 0.308, (HW - 0.004, 0, AZ), (HW + 0.002, 0, AZ), M["Armor_Gunmetal"], rp.U, seg=72)
tube("rim", 0.262, 0.226, (-0.150, 0, AZ), (0.150, 0, AZ), M["Armor_Gunmetal"], rp.U, seg=48)
for s in (-1, 1):
    tube(f"rim_lip{s}", 0.272, 0.236, (s*0.150 - 0.008, 0, AZ), (s*0.150 + 0.008, 0, AZ), M["Steel_Light"], rp.U, seg=48)
for k in range(6):
    a = k*60.0
    hull(f"spoke{k}", [ax(x, r, a + d) for x in (-0.044, 0.012) for (r, d) in ((0.080, -9.0), (0.080, 9.0), (0.232, -6.0), (0.232, 6.0))], M["Armor_Plate_Light"], rp.U, bevel=0.003)
    hull(f"spoke_rib{k}", [ax(x, r, a + d) for x in (-0.050, -0.041) for (r, d) in ((0.090, -2.5), (0.090, 2.5), (0.222, -2.0), (0.222, 2.0))], M["Armor_Dark"], rp.Pc)
cyl("hub", 0.090, (-0.080, 0, AZ), (0.060, 0, AZ), M["Armor_Dark"], rp.U, seg=32, bevel=0.006)
cyl("hub_cap", 0.050, (-0.096, 0, AZ), (-0.078, 0, AZ), M["Steel_Light"], rp.U, seg=32, bevel=0.004)
tube("hub_team", 0.0905, 0.070, (-0.040, 0, AZ), (-0.020, 0, AZ), M["TeamColor_Red"], rp.Pc, seg=32)
for k in range(8):
    a = 2*math.pi*(k + 0.5)/8
    hexbolt(f"lug_nut{k}", (-0.081, F(0.068*math.cos(a)), AZ + 0.068*math.sin(a)), (-0.090, F(0.068*math.cos(a)), AZ + 0.068*math.sin(a)), M["Steel_Light"], rp.Pc, r=0.0065)
tube("disc", 0.196, 0.090, (0.098, 0, AZ), (0.110, 0, AZ), M["Steel_Light"], rp.U, seg=48)
cyl("disc_hat", 0.092, (0.058, 0, AZ), (0.100, 0, AZ), M["Armor_Gunmetal"], rp.U, seg=32)
for k in range(12):
    a = 2*math.pi*k/12
    cyl(f"disc_hole{k}", 0.008, (0.090, F(0.150*math.cos(a)), AZ + 0.150*math.sin(a)), (0.118, F(0.150*math.cos(a)), AZ + 0.150*math.sin(a)), M["Armor_Dark"], rp.D, seg=8)
cyl("axle_bore", 0.0255, (-0.200, 0, AZ), (0.200, 0, AZ), M["Armor_Dark"], rp.D, seg=20)
rp.build(rotor)
col = L.gather(ID, [mount, rotor])
root = L.empty(ID + "_ROOT", None, (0, 0, 0), col, 'PLAIN_AXES', 0.1)
mount.parent = root
axle_e = L.empty("Wheel_Axle", root, (0, 0, AZ), col, 'SINGLE_ARROW', 0.08, (0, math.radians(90), 0))
rotor.data.transform(Matrix.Translation((0, 0, -AZ))); rotor.parent = axle_e
bpy.context.view_layer.update()
# rotor must stay unrotated in world: give it the inverse of the axle empty's rotation as its rest transform
rotor.parent = root; rotor.location = (0, 0, AZ)
import r4port as P4; P4.port(mount, work, 1)
socket = L.empty("Socket_Mount", root, (0, 0, P4.FACE), col, 'SINGLE_ARROW', 0.08)
probe = L.empty("GroundProbe", root, (0, 0, AZ - RT), col, 'SINGLE_ARROW', 0.06, (math.pi, 0, 0))
L.root_props(root, "R3PartKind.Wheel", version="v2-port")
L.set_props({mount: {"role": "static fork mount (top plate = Socket_Mount face +Y)"},
             rotor: {"role": "rolling rotor: rotates about local X only (Wheel_Axle); +X angle = rolling forward (+Z)", "tyre_radius_m": RT, "tyre_width_m": 2*HW},
             axle_e: {"role": "axle axis marker: local +Z = file +X (axle)"}, socket: {"role": "mount face centre on the cell top, normal +Y; other faces per ContactMask"},
             probe: {"role": "ground contact point at rest (0,-0.65,0)", "runtime_ground_y": AZ - RT}})
PARTS = [mount, rotor]
for o in PARTS: L.shade_and_uv(o)
T = L.Timeline()
def ang(f, d, i='LINEAR'): L.key(rotor, "rotation_euler", 0, f, math.radians(d), i)
s, e = T.clip("Idle", 24, loop=True, desc="parked"); ang(s, 0); ang(e, 0)
s, e = T.clip("Roll", 24, loop=True, desc="rolling forward, 1 rev / 24 f (15 rpm*4 = 60 rpm at speed 1; drive from wheel angular velocity)")
for t in range(0, 25, 6): ang(s + t, 15.0*t)
T.clips[-1]["render_frames"] = [s + 3]
s, e = T.clip("Reverse", 24, loop=True, desc="rolling backwards, 1 rev / 24 f")
for t in range(0, 25, 6): ang(s + t, -15.0*t)
s, e = T.clip("Brake", 24, desc="braking from loop speed to stop (ends at 180 deg = rest-equivalent: 20 lugs / 6 spokes / 8 nuts)")
for t in range(0, 25): ang(s + t, 15.0*t - 15.0*t*t/(2*24.0))
ang(T.end + 1, 0.0, 'CONSTANT')
L.name_action(rotor, rotor.name + "_Master")
T.clips[-1]["render_frames"] = [s + 6]
scn.frame_set(1)
def envelope():
    co = np.array([list(rotor.matrix_world @ v.co) for v in rotor.data.vertices])
    rr = np.hypot(co[:, 1], co[:, 2] - AZ); hw = np.abs(co[:, 0]).max()
    tyre = co[rr > 0.30]
    r = dict(runtime="Unity cylinder r 0.390, width 0.364, centre (0,-0.26,0), convex MeshCollider", visual_tyre_radius_m=round(float(rr.max()), 4),
             visual_tyre_half_width_m=round(float(np.abs(tyre[:, 0]).max()), 4), radius_dev_mm=round((float(rr.max()) - RT)*1000, 2),
             width_dev_mm=round((float(np.abs(tyre[:, 0]).max()) - HW)*2000, 2))
    r["PASS"] = abs(r["radius_dev_mm"]) <= 3.0 and abs(r["width_dev_mm"]) <= 10.0
    return r
S = dict(id=ID, kind="R3PartKind.Wheel", game_name="Koło", out=OUT, root=root, pivot=axle_e, yaw=None, pitch=None, socket=socket, movement=True,
         export=[root, mount, rotor, axle_e, socket, probe], parts=PARTS, fx=[], muzzles=[], housing=[mount], ground_parts=[rotor],
         housing_strict=False, housing_note="fork legs/axle bosses reach 0.305 m below the cell centre because the runtime axle sits 0.26 m below it (PARTSIZE #141)",
         ground_contact=dict(runtime_y=AZ - RT, tol_mm=3.0, source="R41PartGeometry.Payload wheel cylinder"), envelope_fn=envelope,
         clearance_pairs=[(rotor, mount)], clash_pairs=[(rotor, mount)], frame_end=T.end, clips=T.clips, hero_frame=1, tri_budget=26000,
         sil_center=(0, 0, -0.18), sil_scale=1.45, notes=["L/R variant: mirror by transform (scale X -1); geometry is closed and axle-symmetric."])
Q.run(S)
