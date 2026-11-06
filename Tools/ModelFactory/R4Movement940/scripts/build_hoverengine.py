# R3PartKind.HoverEngine / MOV007 - "Hover" - ducted lift fan on a tapered tripod pylon: 7-blade rotor, stator cross,
# 5 louvre vanes in the outlet (open/close), thrust glow + pulse ring. Physical lift pod, not a floating ball.
# Runtime contract (R41PartGeometry.Payload): cylinder d 0.52 m, h 0.166 m, centre 0.234 m below the block; HoverLiftPod keeps the
# hover height (Model.HoverHeightMeters) - the asset does not change it.
import sys, os, math; sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import importlib, bpy, numpy as np, r4lib as L, r4qa as Q; importlib.reload(L); importlib.reload(Q)
from mathutils import Vector, Matrix
from r4lib import F, M, box, cyl, cone, tube, hull, prism_x, revolve, hexbolt, Part, new_col
OUT = sys.argv[sys.argv.index("--") + 1]
L.reset_scene(); L.init_palette(); scn = bpy.context.scene
work = new_col("_build_tmp"); ID = "R4M_HoverEngine"
ZC = -0.234; HH = 0.083; RO = 0.260; RI = 0.226; ZB, ZT = ZC - HH, ZC + HH; ZR = -0.212; BLADES = 7
VY = [-0.164, -0.082, 0.0, 0.082, 0.164]; ZV = ZB + 0.024
# ---- static housing: mount plate, tapered pylon, tripod struts, duct shroud, stator cross + motor nacelle
hous = cyl(ID + "_Housing", 0.150, (0, 0, 0.214), (0, 0, 0.262), M["Armor_Gunmetal"], work, seg=48, bevel=0.006)
h = Part(work, "h")
hull("pylon", L.chamfer_square(0.120, 0.034, 0.220) + L.chamfer_square(0.072, 0.022, -0.090), M["Armor_Dark"], h.U, bevel=0.005)
cyl("cap_disc", 0.110, (0, 0, 0.258), (0, 0, 0.270), M["Armor_Plate_Light"], h.U, seg=48, bevel=0.003)
tube("cap_team", 0.1515, 0.138, (0, 0, 0.226), (0, 0, 0.246), M["TeamColor_Red"], h.Pc, seg=48)
for k in range(8): hexbolt("cap_bolt", (0.128*math.cos(k*math.pi/4), 0.128*math.sin(k*math.pi/4), 0.260), (0.128*math.cos(k*math.pi/4), 0.128*math.sin(k*math.pi/4), 0.2665), M["Steel_Light"], h.Pc, r=0.0075)
for k in range(4):
    made = [hull("pylon_rib", [(x, y, z) for (x, z) in ((0.104, 0.215), (0.124, 0.215), (0.062, -0.080), (0.078, -0.080)) for y in (-0.010, 0.010)], M["Armor_Plate_Light"], h.U),
            box("pylon_vent", 0.090, 0.130, -0.030, 0.030, 0.060, 0.110, M["Armor_Dark"], h.D),
            box("pylon_glow", 0.0915, 0.0945, -0.028, 0.028, 0.062, 0.108, M["Energy_Hot"], h.Pc)]
    L.rot_z(made, k)
for k in range(3):
    a = math.radians(90 + 120*k); ca, sa = math.cos(a), math.sin(a)
    hull(f"strut{k}", [(rr*ca + d*sa, rr*sa - d*ca, z) for (rr, z) in ((0.060, -0.060), (0.075, -0.090), (0.236, ZT + 0.004), (0.236, ZT - 0.012)) for d in (-0.016, 0.016)], M["Armor_Gunmetal"], h.U, bevel=0.003)
revolve("duct", [(RI, ZB + 0.004), (RI + 0.006, ZB), (RO - 0.004, ZB), (RO, ZB + 0.008), (RO, ZT - 0.012), (RO - 0.010, ZT), (RI + 0.010, ZT), (RI, ZT - 0.016)],
        M["Armor_Gunmetal"], h.U, 64, local=True, closed_ring=True)
tube("duct_band", RO + 0.0015, RO - 0.006, (0, 0, ZC - 0.020), (0, 0, ZC + 0.008), M["TeamColor_Red"], h.Pc, seg=64)
tube("duct_lip", RO + 0.002, RI + 0.004, (0, 0, ZB - 0.002), (0, 0, ZB + 0.006), M["Steel_Light"], h.U, seg=64)
for k in range(12):
    a = 2*math.pi*(k + 0.5)/12
    hexbolt(f"duct_bolt{k}", (0.243*math.cos(a), 0.243*math.sin(a), ZT - 0.004), (0.243*math.cos(a), 0.243*math.sin(a), ZT + 0.003), M["Steel_Light"], h.Pc, r=0.0055)
for k in range(6):
    a = math.radians(60*k + 30)
    hull(f"duct_haz{k}", [(rr*math.cos(a + d), rr*math.sin(a + d), z) for rr in (0.232, 0.252) for d in (-0.07, 0.07) for z in (ZT - 0.003, ZT + 0.0015)], M["Hazard_Yellow"], h.Pc)
cyl("nacelle", 0.060, (0, 0, ZR + 0.018), (0, 0, -0.090), M["Armor_Dark"], h.U, seg=32, bevel=0.006)
tube("nacelle_glow", 0.0605, 0.054, (0, 0, -0.150), (0, 0, -0.144), M["Energy_Hot"], h.Pc, seg=32)
for k in range(4):
    a = math.radians(45 + 90*k); ca, sa = math.cos(a), math.sin(a)
    hull(f"stator{k}", [(rr*ca + d*sa, rr*sa - d*ca, z) for rr in (0.050, RI + 0.006) for d in (-0.006, 0.006) for z in (ZR + 0.022, ZR + 0.046)], M["Armor_Plate_Light"], h.U)
h.build(hous)
# ---- rotor (spins about local Y in Unity = Blender Z)
rotor = cyl(ID + "_Rotor", 0.046, (0, 0, ZR - 0.014), (0, 0, ZR + 0.016), M["Steel_Light"], work, seg=32, bevel=0.004)
rp = Part(work, "r")
for k in range(BLADES):
    a = 2*math.pi*k/BLADES
    pts = []
    for (rr, tw, ch) in ((0.040, 32.0, 0.034), (0.130, 24.0, 0.040), (0.214, 14.0, 0.030)):
        for side in (-1, 1):
            t_ = math.radians(tw); dx, dz = side*ch/2*math.cos(t_), side*ch/2*math.sin(t_)
            ang = a + math.atan2(dx, rr)
            for thick in (-0.002, 0.002):
                pts.append((rr*math.cos(ang), rr*math.sin(ang), ZR + dz + thick))
    hull(f"blade{k}", pts, M["Armor_Gunmetal"], rp.U)
cone("spinner", 0.046, 0.014, (0, 0, ZR - 0.014), (0, 0, ZR - 0.034), M["TeamColor_Red"], rp.U, seg=32)
rp.build(rotor)
# ---- louvre vanes (5, each about its own Unity X axis)
vanes = []
for i, yv in enumerate(VY):
    half = math.sqrt(RI**2 - (abs(yv) + 0.034)**2) - 0.012
    v = box(f"{ID}_Vane_{i + 1}", -half, half, yv - 0.033, yv + 0.033, ZV - 0.004, ZV + 0.004, M["Armor_Plate_Light"], work, bevel=0.002)
    vp = Part(work, f"v{i}")
    cyl("vane_spar", 0.0055, (-half - 0.004, yv, ZV), (half + 0.004, yv, ZV), M["Steel_Light"], vp.U, seg=10)
    box("vane_edge", -half + 0.004, half - 0.004, yv - 0.034, yv - 0.028, ZV - 0.0045, ZV + 0.0045, M["Armor_Dark"], vp.U)
    vp.build(v); vanes.append(v)
glow = L.cyl("Hover_ThrustGlow", RI - 0.012, (0, 0, ZB - 0.004), (0, 0, ZB - 0.010), M["Energy_Hot"], work, seg=48)
pulse = L.tube("Hover_PulseRing", 0.200, 0.180, (0, 0, -0.004), (0, 0, 0.004), M["Muzzle_Flash"], work, seg=48)
col = L.gather(ID, [hous, rotor, glow, pulse] + vanes)
root = L.empty(ID + "_ROOT", None, (0, 0, 0), col, 'PLAIN_AXES', 0.1)
hous.parent = root
for o, piv in [(rotor, (0, 0, ZR))] + [(v, (0, yv, ZV)) for v, yv in zip(vanes, VY)] + [(glow, (0, 0, ZB - 0.007))]:
    o.data.transform(Matrix.Translation(-Vector(piv))); o.parent = root; o.location = piv
pulse.parent = root; pulse.location = (0, 0, ZB - 0.020)
glow.scale = (0.001,)*3; pulse.scale = (0.001,)*3
socket = L.empty("Socket_Mount", root, (0, 0, L.CELL_HALF), col, 'SINGLE_ARROW', 0.08)
nozzle = L.empty("Thruster_Nozzle", root, (0, 0, ZB), col, 'SINGLE_ARROW', 0.08, (math.pi, 0, 0))
probe = L.empty("GroundProbe", root, (0, 0, ZB - 0.002), col, 'SINGLE_ARROW', 0.05, (math.pi, 0, 0))
L.root_props(root, "R3PartKind.HoverEngine")
L.set_props({hous: {"role": "static: mount plate, tapered pylon, tripod struts, duct shroud, stator + motor nacelle"},
             rotor: {"role": f"lift fan rotor ({BLADES} blades): rotates about local Y (Unity up)"},
             glow: {"role": "thrust glow FX at the outlet - idle scale 0.001"}, pulse: {"role": "thrust pulse ring FX - idle scale 0.001"},
             socket: {"role": "mount face centre on the cell top, normal +Y"},
             nozzle: {"role": "outlet centre; local +Z = exhaust direction (down) -> lift force +Y"},
             probe: {"role": "lowest point of the pod (hover height measured from here by HoverLiftPod)"}})
for v in vanes: v["role"] = "outlet louvre vane: rotates about local X; 0 = closed, 70 deg = open"
PARTS = [hous, rotor] + vanes; FX = [glow, pulse]
for o in PARTS + FX: L.shade_and_uv(o)
T = L.Timeline(); STEP = 360.0/BLADES; W_ = 3*STEP/12.0
def rk(f, d, i='LINEAR'): L.key(rotor, "rotation_euler", 2, f, math.radians(d), i)
def vk(f, d, i='BEZIER'):
    for j, v in enumerate(vanes): L.key(v, "rotation_euler", 0, f, math.radians(d*(1 if j % 2 == 0 else -1)), i)
def fx(o, f, sc, i='LINEAR'): L.key3(o, "scale", f, (sc,)*3 if not isinstance(sc, tuple) else sc, i)
s, e = T.clip("Idle", 24, loop=True, desc="off: rotor stopped, louvres closed")
rk(s, 0); rk(e, 0); vk(s, 0); vk(e, 0); fx(glow, s, 0.001, 'CONSTANT'); fx(pulse, s, 0.001, 'CONSTANT')
s, e = T.clip("SpinUp", 24, desc="start: rotor accelerates to lift speed, louvres open to 70 deg, outlet glow rises")
for t in range(0, 25): rk(s + t, W_*t*t/(2*24.0))
vk(s, 0); vk(s + 16, 70); vk(e, 70); fx(glow, s, 0.001); fx(glow, e, 1.0)
T.clips[-1]["render_frames"] = [e - 4]
s, e = T.clip("Lift", 12, loop=True, desc=f"hovering: steady spin 3 blades / 12 f (seamless, {BLADES}-fold), louvres open, glow on")
for t in range(0, 13): rk(s + t, W_*t)
vk(s, 70); vk(e, 70); fx(glow, s, 1.0, 'CONSTANT'); fx(glow, e, 1.0, 'CONSTANT')
T.clips[-1]["render_frames"] = [s + 6]; HERO = s + 6
s, e = T.clip("Pulse", 12, desc="thrust pulse: louvres snap to 85 deg, pulse ring expands, glow flares, rotor at speed")
for t in range(0, 13): rk(s + t, W_*t)
vk(s, 70); vk(s + 2, 85, 'LINEAR'); vk(s + 8, 85); vk(e, 70)
fx(glow, s, 1.0); fx(glow, s + 2, (1.25, 1.25, 1.25)); fx(glow, e, 1.0)
fx(pulse, s, 0.001, 'CONSTANT'); fx(pulse, s + 1, 1.0); fx(pulse, s + 6, 2.2); fx(pulse, s + 7, 0.001, 'CONSTANT')
T.clips[-1]["render_frames"] = [s + 3]; HERO2 = s + 3
s, e = T.clip("Cooldown", 32, desc="shutdown: rotor spins down, louvres close, glow fades (ends 4 blades on = rest-equivalent)")
for t in range(0, 33): rk(s + t, W_*t - W_*t*t/(2*32.0))
vk(s, 70); vk(s + 20, 0); vk(e, 0); fx(glow, s, 1.0); fx(glow, s + 14, 0.001, 'CONSTANT')
T.clips[-1]["render_frames"] = [s + 10]
rk(T.end + 1, 0, 'CONSTANT'); fx(glow, T.end, 0.001, 'CONSTANT'); fx(pulse, T.end, 0.001, 'CONSTANT')
for o in [rotor, glow, pulse] + vanes: L.name_action(o, o.name + "_Master")
scn.frame_set(1)
def envelope():
    co = np.array([list(hous.matrix_world @ v.co) for v in hous.data.vertices]); duct = co[co[:, 2] < ZT + 0.006]
    rr = np.hypot(duct[:, 0], duct[:, 1])
    r = dict(runtime="Unity cylinder d 0.52, h 0.166 at (0,-0.234,0)", duct_outer_radius_m=round(float(rr.max()), 4), duct_bottom_y=round(float(duct[:, 2].min()), 4),
             duct_top_y=round(float(duct[:, 2].max()), 4))
    r["radius_dev_mm"] = round((r["duct_outer_radius_m"] - 0.26)*1000, 2); r["bottom_dev_mm"] = round((r["duct_bottom_y"] - ZB)*1000, 2)
    r["PASS"] = abs(r["radius_dev_mm"]) <= 3 and abs(r["bottom_dev_mm"]) <= 3
    return r
S = dict(id=ID, kind="R3PartKind.HoverEngine", game_name="Hover (MOV007)", out=OUT, root=root, pivot=nozzle, yaw=None, pitch=None, socket=socket, movement=True,
         export=[root, hous, rotor] + vanes + FX + [socket, nozzle, probe], parts=PARTS, fx=FX, muzzles=[], housing=[hous], ground_parts=[],
         ground_note="no ground contact: HoverLiftPod holds Model.HoverHeightMeters below GroundProbe (runtime value, unchanged)",
         housing_strict=False, housing_note="duct shroud bottom at -0.317 m (runtime pod disc reaches below the cell; PARTSIZE #141)", envelope_fn=envelope,
         clearance_pairs=[(rotor, hous), (vanes[2], rotor), (vanes[0], hous)], clash_pairs=[(rotor, hous)] + [(v, hous) for v in vanes] + [(v, rotor) for v in vanes] + [(vanes[i], vanes[i + 1]) for i in range(4)],
         frame_end=T.end, clips=T.clips, hero_frame=HERO, hero_frame2=HERO2, tri_budget=26000, sil_center=(0, 0, -0.03), sil_scale=1.10,
         notes=["Thrust direction: exhaust -Y (down), lift +Y; louvres are visual vanes, not a vectoring mechanic."])
Q.run(S)
