# R3PartKind.CrawlerTrack / MOV008 - "Gąsienica" - LEFT track (inner frame on +X; Track_R = mirror by transform).
# Runtime contract (R41PartGeometry.Payload): box 0.442 x 0.260 x 0.936 m (X,Y,Z Unity), centre 0.286 m below the block;
# R3VehicleDesign: "Gąsienicę zamontuj po lewej lub prawej stronie" (cell.x != 0).
import sys, os, math; sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import importlib, bpy, numpy as np, r4lib as L, r4qa as Q; importlib.reload(L); importlib.reload(Q)
from mathutils import Vector, Matrix
from r4lib import F, M, box, cyl, cone, tube, hull, prism_x, revolve, hexbolt, Part, new_col
OUT = sys.argv[sys.argv.index("--") + 1]
L.reset_scene(); L.init_palette(); scn = bpy.context.scene
work = new_col("_build_tmp"); ID = "R4M_CrawlerTrack"
ZC = -0.286; HALF_L = 0.468; T_SHOE = 0.016; G = 0.008
RC = 0.130 - T_SHOE/2 - G             # path radius at shoe mid-thickness
YC = HALF_L - 0.130                    # arc centres (Blender Y)
N = 40; PERIM = 4*YC + 2*math.pi*RC; PITCH = PERIM/N
R_IN = RC - T_SHOE/2                   # inner running surface radius at the arcs
def path(s):
    s = s % PERIM; top = 2*YC; arc = math.pi*RC
    if s < top: return Vector((0, YC - s, ZC + RC)), 0.0
    s -= top
    if s < arc: th = s/RC; return Vector((0, -YC - RC*math.sin(th), ZC + RC*math.cos(th))), math.degrees(th)
    s -= arc
    if s < top: return Vector((0, -YC + s, ZC - RC)), 180.0
    s -= top; th = s/RC
    return Vector((0, YC + RC*math.sin(th), ZC - RC*math.cos(th))), 180.0 + math.degrees(th)
def roll_r(n): return PITCH*n/(2*math.pi)          # radius that turns 360/n per link pitch -> seamless loops
# ---- static housing: mount box, inner side frame (+X) with axle bosses, outer skirt (-X), tensioner
hous = hull(ID + "_Housing", [(x, y, z) for (x, y, z) in [(sx*0.215, sy*0.330, -0.135) for sx in (-1, 1) for sy in (-1, 1)] + [(sx*0.205, sy*0.250, -0.060) for sx in (-1, 1) for sy in (-1, 1)] + [(sx*0.180, sy*0.150, -0.020) for sx in (-1, 1) for sy in (-1, 1)]], M["Armor_Gunmetal"], work, bevel=0.006)
h = Part(work, "h")
box("post", -0.110, 0.110, -0.110, 0.110, -0.030, 0.250, M["Armor_Dark"], h.U, bevel=0.008)
cyl("post_flange", 0.150, (0, 0, 0.236), (0, 0, 0.264), M["Armor_Plate_Light"], h.U, seg=40, bevel=0.004)
tube("post_team", 0.1515, 0.140, (0, 0, 0.243), (0, 0, 0.257), M["TeamColor_Red"], h.Pc, seg=40)
for k in range(4):
    made = [box("post_rib", 0.108, 0.118, -0.020, 0.020, -0.015, 0.232, M["Armor_Gunmetal"], h.U, bevel=0.002), box("post_vent", 0.104, 0.120, -0.060, 0.060, 0.120, 0.150, M["Armor_Dark"], h.D), box("post_glow", 0.1020, 0.1050, -0.058, 0.058, 0.122, 0.148, M["Energy_Hot"], h.Pc)]
    L.rot_z(made, k)
for sy in (-1, 1): box("sponson_plate", -0.160, 0.160, sy*0.165, sy*0.245, -0.052, -0.034, M["Armor_Plate_Light"], h.U, bevel=0.002)
box("sponson_team", -0.2165, -0.2135, F(-0.300), F(0.300), -0.110, -0.090, M["TeamColor_Red"], h.Pc)
for k in range(6): hexbolt("flange_bolt", (0.128*math.cos(k*math.pi/3), 0.128*math.sin(k*math.pi/3), 0.262), (0.128*math.cos(k*math.pi/3), 0.128*math.sin(k*math.pi/3), 0.268), M["Steel_Light"], h.Pc, r=0.008)
hull("frame", [(x, F(f), z) for x in (0.206, 0.228) for (f, z) in ((-0.380, -0.175), (0.380, -0.175), (0.300, -0.335), (-0.300, -0.335), (0.150, -0.128), (-0.150, -0.128))], M["Armor_Dark"], h.U, bevel=0.004)
hull("frame_bridge", [(x, F(f), z) for x in (0.206, 0.228) for (f, z) in ((-0.150, -0.128), (0.150, -0.128), (0.120, -0.200), (-0.120, -0.200))], M["Armor_Dark"], h.U, bevel=0.004)
for f0 in (-0.250, -0.085, 0.085, 0.250):
    hull("frame_lighten", [(x, F(f), z) for x in (0.200, 0.236) for (f, z) in ((f0 - 0.040, -0.215), (f0 + 0.040, -0.215), (f0 + 0.028, -0.262), (f0 - 0.028, -0.262))], M["Armor_Dark"], h.D)
hull("frame_haz", [(x, F(f), z) for x in (0.2275, 0.2300) for (f, z) in ((-0.120, -0.150), (0.120, -0.150), (0.100, -0.168), (-0.100, -0.168))], M["Hazard_Yellow"], h.Pc)
hull("skirt", [(x, F(f), z) for x in (-0.228, -0.212) for (f, z) in ((-0.452, -0.143), (0.452, -0.143), (0.430, -0.246), (-0.430, -0.246))], M["Armor_Gunmetal"], h.U, bevel=0.003)
box("skirt_strut_f", -0.228, -0.150, F(0.230), F(0.190), -0.146, -0.128, M["Armor_Dark"], h.U, bevel=0.003)
box("skirt_strut_r", -0.228, -0.150, F(-0.190), F(-0.230), -0.146, -0.128, M["Armor_Dark"], h.U, bevel=0.003)
box("skirt_team", -0.2295, -0.2275, F(-0.300), F(0.300), -0.190, -0.172, M["TeamColor_Red"], h.Pc)
for f_ in (-0.380, -0.130, 0.130, 0.380): hexbolt("skirt_bolt", (-0.227, F(f_), -0.160), (-0.2315, F(f_), -0.160), M["Steel_Light"], h.Pc)
RW_R = roll_r(10); RR_R = roll_r(4); ID_R = roll_r(12)
zrw = ZC - R_IN + RW_R; zrr = ZC + R_IN - RR_R
for y, z, r in ((-YC, ZC, 0.034), (YC, ZC, 0.034), (-0.170, zrw, 0.028), (0.0, zrw, 0.028), (0.170, zrw, 0.028), (0.0, zrr, 0.020)):
    cyl("axle_boss", r + 0.012, (0.204, y, z), (0.240, y, z), M["Armor_Dark"], h.U, seg=20, bevel=0.003)
    tube("boss_glow", r + 0.004, r - 0.004, (0.2385, y, z), (0.2420, y, z), M["Energy_Hot"], h.Pc, seg=20)
    cyl("axle_bore", 0.0135, (0.196, y, z), (0.236, y, z), M["Armor_Dark"], h.D, seg=12)
hull("tension_arm", [(x, y, z) for x in (0.228, 0.240) for (y, z) in ((YC - 0.020, ZC + 0.040), (YC + 0.020, ZC + 0.040), (0.200, -0.250), (0.230, -0.230))], M["Armor_Plate_Light"], h.U, bevel=0.002)
h.build(hous)
# ---- running gear (each spins about local X)
def wheelpair(name, r, holes, tooth=0, single=False):
    """double running wheel straddling the guide horns (or a single disc on +X), with its own axle stub to the frame"""
    discs = ((0.030, 0.058),) if single else ((-0.104, -0.084), (-0.050, -0.030), (0.030, 0.050), (0.084, 0.104))
    w = cyl(name, 0.020 if single else (0.050 if tooth else 0.030), (0.030 if single else -0.096, 0, 0), (0.058 if single else 0.096, 0, 0), M["Armor_Dark"], work, seg=24)
    p = Part(work, name[-3:])
    for x0 in discs:
        cyl("disc", r, (x0[0], 0, 0), (x0[1], 0, 0), M["Armor_Gunmetal"], p.U, seg=max(20, holes*2), bevel=0.003)
        tube("rim", r + 0.0005, r - 0.008, (x0[0] - 0.001, 0, 0), (x0[1] + 0.001, 0, 0), M["Steel_Light"], p.U, seg=max(20, holes*2))
        for k in range(tooth):
            a = 2*math.pi*(k + 0.5)/tooth
            hull(f"tooth{k}", [(x, rr*math.cos(a + d), rr*math.sin(a + d)) for x in x0 for (rr, d) in ((r - 0.004, -0.11), (r - 0.004, 0.11), (r + 0.012, -0.04), (r + 0.012, 0.04))], M["Barrel_Heat"], p.U)
    for k in range(holes):
        a = 2*math.pi*k/holes
        if not single:
            cyl(f"hole{k}", 0.010 if r < 0.095 else 0.012, (-0.114, 0.62*r*math.cos(a), 0.62*r*math.sin(a)), (0.114, 0.62*r*math.cos(a), 0.62*r*math.sin(a)), M["Armor_Dark"], p.D, seg=8)
        else:
            hexbolt(f"bolt{k}", (0.057, 0.55*r*math.cos(a), 0.55*r*math.sin(a)), (0.062, 0.55*r*math.cos(a), 0.55*r*math.sin(a)), M["Steel_Light"], p.Pc, r=0.0035)
    x_out = 0.058 if single else 0.104
    cyl("axle", 0.012, (x_out - 0.004, 0, 0), (0.232, 0, 0), M["Steel_Light"], p.U, seg=12)
    cyl("hub_cap", 0.024, (x_out - 0.004, 0, 0), (x_out + 0.008, 0, 0), M["TeamColor_Red"] if tooth else M["Steel_Light"], p.U, seg=16)
    if not single: cyl("hub_cap_in", 0.024, (-0.112, 0, 0), (-0.100, 0, 0), M["Steel_Light"], p.U, seg=16)
    p.build(w)
    return w
sprocket = wheelpair(ID + "_Sprocket", R_IN - 0.016, 7, tooth=14)
idler = wheelpair(ID + "_Idler", ID_R - 0.0015, 12)
rws = [wheelpair(f"{ID}_RoadWheel_{i + 1}", RW_R - 0.0015, 10) for i in range(3)]
roller = wheelpair(ID + "_ReturnRoller", RR_R - 0.0015, 4, single=True)
# ---- links (one shared mesh)
link = box(ID + "_Link_01", -0.200, 0.200, -0.022, 0.022, -T_SHOE/2, T_SHOE/2, M["Armor_Gunmetal"], work, bevel=0.0015)
k_ = Part(work, "lk")
box("grouser", -0.190, 0.190, -0.006, 0.006, T_SHOE/2 - 0.001, T_SHOE/2 + G, M["Armor_Dark"], k_.U)
box("horn", -0.012, 0.012, -0.012, 0.012, -T_SHOE/2 - 0.030, -T_SHOE/2 + 0.001, M["Steel_Light"], k_.U, bevel=0.003)
for s in (-1, 1):
    cyl(f"pin{s}", 0.0055, (s*0.110 - 0.090, 0.0225, 0), (s*0.110 + 0.090, 0.0225, 0), M["Steel_Light"], k_.U, seg=6)
    box(f"shoe_rib{s}", s*0.120 - 0.004, s*0.120 + 0.004, -0.018, 0.018, T_SHOE/2 - 0.001, T_SHOE/2 + 0.004, M["Armor_Dark"], k_.U)
k_.build(link)
links = [link] + []
for i in range(1, N):
    c_ = link.copy(); c_.name = f"{ID}_Link_{i + 1:02d}"; work.objects.link(c_); links.append(c_)
# hierarchy
col = L.gather(ID, [hous, sprocket, idler, roller] + rws + links)
root = L.empty(ID + "_ROOT", None, (0, 0, 0), col, 'PLAIN_AXES', 0.1)
hous.parent = root
track = L.empty("Track_L", root, (0, 0, 0), col, 'PLAIN_AXES', 0.05)
for w, (y, z) in ((sprocket, (-YC, ZC)), (idler, (YC, ZC)), (rws[0], (-0.170, zrw)), (rws[1], (0.0, zrw)), (rws[2], (0.170, zrw)), (roller, (0.0, zrr))):
    w.parent = root; w.location = (0, y, z)
for i, lk in enumerate(links):
    p, phi = path(i*PITCH); lk.parent = track; lk.location = p; lk.rotation_euler = (math.radians(phi), 0, 0)
import r4port as P4; P4.port(hous, work, 1)
socket = L.empty("Socket_Mount", root, (0, 0, P4.FACE), col, 'SINGLE_ARROW', 0.08)
inner = L.empty("Mount_Inner_+X", root, (0.240, 0, -0.140), col, 'SINGLE_ARROW', 0.06, (0, math.radians(90), 0))
probe = L.empty("GroundProbe", root, (0, 0, ZC - 0.130), col, 'SINGLE_ARROW', 0.06, (math.pi, 0, 0))
L.root_props(root, "R3PartKind.CrawlerTrack", version="v2-port")
root["side"] = "LEFT (inner frame +X). Track_R = parent scale X -1."
L.set_props({hous: {"role": "static housing: mount box, inner frame (+X), outer skirt (-X)"},
             sprocket: {"role": "drive sprocket (front), 14 teeth = 1 tooth per link pitch, rotates about local X"},
             idler: {"role": "idler / tensioner wheel (rear), rotates about local X"}, roller: {"role": "return roller, rotates about local X"},
             track: {"role": f"track loop: {N} links, pitch {PITCH*1000:.1f} mm; links follow the loop path"},
             socket: {"role": "mount face centre on the cell top, normal +Y"}, inner: {"role": "inner side (toward the hull) for LEFT mounting"},
             probe: {"role": "ground contact at rest (0,-0.416,0)"}})
for w in rws: w["role"] = "road wheel pair, rotates about local X"
PARTS = [hous, sprocket, idler, roller] + rws + links
for o in [hous, sprocket, idler, roller] + rws + [links[0]]: L.shade_and_uv(o)
T = L.Timeline()
WH = [(sprocket, R_IN - 0.016, 14), (idler, ID_R, 12), (roller, RR_R, 4)] + [(w, RW_R, 10) for w in rws]
def key_state(f, s_off):
    """s_off = distance the track surface has moved (m); wheels turn by s_off / r (exact symmetric angles)"""
    for i, lk in enumerate(links):
        p, phi = path(i*PITCH + s_off); turns = math.floor((i*PITCH + s_off)/PERIM)
        L.key3(lk, "location", f, tuple(p), 'LINEAR'); L.key(lk, "rotation_euler", 0, f, math.radians(phi + 360.0*turns), 'LINEAR')
    for w, r, n in WH:
        L.key(w, "rotation_euler", 0, f, (s_off/PITCH)*(2*math.pi/n), 'LINEAR')
s, e = T.clip("Idle", 24, loop=True, desc="parked"); key_state(s, 0.0); key_state(e, 0.0)
s, e = T.clip("Travel", 12, loop=True, desc="driving forward: track advances 1 link pitch / 12 f (top run forward, ground run backward); seamless")
for t in range(0, 13): key_state(s + t, PITCH*t/12.0)
T.clips[-1]["render_frames"] = [s + 6]
s, e = T.clip("Reverse", 12, loop=True, desc="driving backwards, 1 pitch / 12 f, seamless")
for t in range(0, 13): key_state(s + t, -PITCH*t/12.0)
s, e = T.clip("Stop", 24, desc="braking from Travel speed to standstill: exactly 1 pitch travelled (rest-equivalent end)")
for t in range(0, 25): key_state(s + t, (PITCH/12.0)*t - (PITCH/12.0)*t*t/(2*24.0))
T.clips[-1]["render_frames"] = [s + 8]
key_state(T.end + 1, 0.0)
for o in links + [w for w, _, _ in WH]: L.name_action(o, o.name + "_Master")
scn.frame_set(1)
def envelope():
    pts = np.vstack([np.array([list(o.matrix_world @ v.co) for v in o.data.vertices]) for o in links])
    lo, hi = pts.min(0), pts.max(0)
    r = dict(runtime="Unity box 0.442 x 0.260 x 0.936 at (0,-0.286,0)", track_loop_length_m=round(float(hi[1] - lo[1]), 4), track_loop_height_m=round(float(hi[2] - lo[2]), 4),
             track_width_m=round(float(hi[0] - lo[0]), 4), length_dev_mm=round((float(hi[1] - lo[1]) - 0.936)*1000, 2), height_dev_mm=round((float(hi[2] - lo[2]) - 0.260)*1000, 2),
             centre_z_dev_mm=round(((hi[2] + lo[2])/2 - ZC)*1000, 2), link_pitch_mm=round(PITCH*1000, 2), links=N)
    r["PASS"] = abs(r["length_dev_mm"]) <= 4 and abs(r["height_dev_mm"]) <= 4 and abs(r["centre_z_dev_mm"]) <= 3 and r["track_width_m"] <= 0.442
    return r
gear = [sprocket, idler, roller] + rws
S = dict(id=ID, kind="R3PartKind.CrawlerTrack", game_name="Gąsienica (MOV008)", out=OUT, root=root, pivot=track, yaw=None, pitch=None, socket=socket, movement=True,
         export=[root, hous, track] + gear + links + [socket, inner, probe], parts=PARTS, fx=[], muzzles=[], housing=[hous], ground_parts=links,
         housing_strict=False, housing_note="inner frame/skirt span the track length (0.936 m) into the front/rear cells (PARTSIZE #141); mount box itself is inside the cell",
         ground_contact=dict(runtime_y=ZC - 0.130, tol_mm=3.0, source="R41PartGeometry.Payload track box"), envelope_fn=envelope,
         clearance_pairs=[(links[0], hous), (links[0], sprocket), (links[N//2], rws[1])],
         clash_pairs=[(lk, o) for lk in links for o in [hous] + gear] + [(a, hous) for a in gear],
         frame_end=T.end, clips=T.clips, hero_frame=1, tri_budget=32000, sil_center=(0, 0, -0.10), sil_scale=1.55,
         notes=[f"Wheel radii chosen so each turns exactly 360/n per link pitch (sprocket 14, idler 12, road 10, return 4) -> seamless loops."])
Q.run(S)
