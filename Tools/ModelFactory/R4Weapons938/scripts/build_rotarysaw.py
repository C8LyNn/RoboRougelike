# R4WeaponKind.RotarySaw - "WPN-005 Rotary Saw" - exposed physical blade on a side arm, guard, motor pack, spin/jam.
# blender -b --factory-startup --python build_rotarysaw.py -- <out_dir>
import sys, os, math; sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import importlib, bpy, r4lib as L, r4qa as Q, r4base as BS, r4heads as H; importlib.reload(L); importlib.reload(Q); importlib.reload(BS); importlib.reload(H)
from mathutils import Matrix, Vector
from r4lib import F, P, M, box, cyl, tube, hull, prism_x, revolve, hexbolt, Part, new_col
OUT = sys.argv[sys.argv.index("--") + 1]
L.reset_scene(); L.init_palette(); scn = bpy.context.scene
work = new_col("_build_tmp")
ID = "R4W_RotarySaw"
DN, UP = 10.0, 35.0
BF = 0.620                 # blade centre forward of the pivot
RB = 0.300                 # blade radius at tooth tips
TEETH = 24
G0, G1 = 62.0, 205.0       # guard sector (deg, 0 = forward, 90 = up)
def ring_pt(r, phi, x, f0=BF, z0=P):
    return (x, F(f0 + r*math.cos(math.radians(phi))), z0 + r*math.sin(math.radians(phi)))   # phi 0 = forward, 90 = up
def sector(name, r0, r1, a0, a1, x0, x1, mat, col, step=None):
    return L.arc_plate(name, r0, r1, a0, a1, x0, x1, mat, col, (0, BF, P))
# ---- base: hazard-cornered heavy variant with skirts (contact weapon)
BASE_STYLE, HEAD = "crab", "unchanged v1 saw arm"
base, _ = BS.build_base_style(ID + "_Base", work, BASE_STYLE)
# ---- yaw: turntable + low raked cheeks + rear motor pack with radiator and fan
yaw = L.turntable(ID + "_Yaw", work)
y = Part(work, "y")
CHEEK = [(0.120, 0.035), (0.165, 0.080), (0.070, 0.200), (-0.080, 0.205), (-0.135, 0.150), (-0.135, 0.035)]
for s in (-1, 1):
    prism_x(f"cheek{s}", s*0.108, s*0.150, CHEEK, M["Armor_Gunmetal"], y.U, bevel=0.005)
    prism_x(f"cheek_appl{s}", s*0.1495, s*0.158, [(0.095, 0.058), (0.135, 0.090), (0.058, 0.182), (-0.060, 0.186), (-0.110, 0.142), (-0.110, 0.058)],
            M["Armor_Plate_Light"], y.U, bevel=0.003)
    prism_x(f"cheek_team{s}", s*0.1575, s*0.1620, [(-0.095, 0.1495), (0.075, 0.1495), (0.060, 0.1638), (-0.080, 0.1638)], M["TeamColor_Red"], y.Pc)
    cyl(f"boss{s}", 0.044, (s*0.104, 0, P), (s*0.165, 0, P), M["Armor_Dark"], y.U, seg=24, bevel=0.004)
    tube(f"boss_glow{s}", 0.036, 0.027, (s*0.163, 0, P), (s*0.1675, 0, P), M["Energy_Hot"], y.Pc, seg=24)
    cyl(f"bore{s}", 0.021, (s*0.098, 0, P), (s*0.152, 0, P), M["Armor_Dark"], y.D, seg=20)
    for (f_, z_) in ((0.085, 0.080), (-0.100, 0.070), (-0.010, 0.177)):
        hexbolt(f"appl_bolt{s}", (s*0.156, F(f_), z_), (s*0.1615, F(f_), z_), M["Steel_Light"], y.Pc)
box("motor", -0.102, 0.102, F(-0.140), F(-0.240), 0.035, 0.158, M["Armor_Dark"], y.U, bevel=0.010)
box("motor_lid", -0.090, 0.090, F(-0.150), F(-0.230), 0.153, 0.166, M["TeamColor_Red"], y.U, bevel=0.004)
for i in range(7):
    xx = -0.075 + i*0.025
    box(f"rad_fin{i}", xx - 0.004, xx + 0.004, F(-0.150), F(-0.232), 0.166, 0.188, M["Steel_Light"], y.U, bevel=0.0015)
tube("fan_ring", 0.050, 0.041, (0, F(-0.236), 0.096), (0, F(-0.252), 0.096), M["Armor_Gunmetal"], y.U, seg=32)
cyl("fan_cut", 0.043, (0, F(-0.225), 0.096), (0, F(-0.260), 0.096), M["Armor_Dark"], y.D, seg=32)
cyl("fan_hub", 0.014, (0, F(-0.228), 0.096), (0, F(-0.246), 0.096), M["Steel_Light"], y.Pc, seg=16)
for k in range(5):
    a = 2*math.pi*k/5
    hull(f"fan_blade{k}", [(0.010*math.cos(a) + d*math.cos(a + 1.2), F(-0.238) + t, 0.096 + 0.010*math.sin(a) + d*math.sin(a + 1.2)) for d in (0, 0.006) for t in (-0.003, 0.003)]
         + [(0.040*math.cos(a + 0.35), F(-0.238), 0.096 + 0.040*math.sin(a + 0.35))], M["Armor_Gunmetal"], y.Pc)
cyl("fan_glow", 0.042, (0, F(-0.226), 0.096), (0, F(-0.228), 0.096), M["Energy_Hot"], y.Pc, seg=32)
box("haz_plate", -0.090, -0.058, F(-0.2395), F(-0.2412), 0.040, 0.150, M["Hazard_Yellow"], y.Pc)
box("haz_plate2", 0.058, 0.090, F(-0.2395), F(-0.2412), 0.040, 0.150, M["Hazard_Yellow"], y.Pc)
for s in (-1, 1):
    for k in range(4):
        z0 = 0.046 + k*0.027
        hull(f"haz_chev{s}{k}", [(xx, F(ff), zz) for ff in (-0.2398, -0.2418) for (xx, zz) in ((s*0.0586, z0), (s*0.0586, z0 + 0.010), (s*0.0894, z0 + 0.022), (s*0.0894, z0 + 0.012))], M["Armor_Dark"], y.Pc)
y.build(yaw)
# ---- pitch: trunnion drum, gearbox, side arm (+X) with belt cover, spindle housing, blade guard
pitch = cyl(ID + "_Pitch", 0.048, (-0.100, 0, P), (0.100, 0, P), M["Armor_Dark"], work, seg=32, bevel=0.004)
p = Part(work, "p")
cyl("axle", 0.018, (-0.140, 0, P), (0.140, 0, P), M["Steel_Light"], p.U, seg=16)
hull("gearbox", [(x, F(f), z) for (f, xw, z0, z1) in ((-0.080, 0.088, 0.088, 0.192), (0.080, 0.094, 0.084, 0.200), (0.170, 0.080, 0.096, 0.170))
                 for x in (-xw, xw) for z in (z0, z1)], M["Armor_Gunmetal"], p.U, bevel=0.005)
for s in (-1, 1):
    prism_x(f"gb_appl{s}", s*0.0935, s*0.0990, [(-0.060, 0.098), (0.130, 0.098), (0.150, 0.140), (0.110, 0.184), (-0.060, 0.180)], M["Armor_Plate_Light"], p.U, bevel=0.002)
for k in range(4):
    f0 = -0.055 + 0.028*k
    box(f"gb_slot{k}", -0.070, 0.070, F(f0 + 0.010), F(f0), 0.188, 0.210, M["Armor_Dark"], p.D)
    box(f"gb_glow{k}", -0.068, 0.068, F(f0 + 0.009), F(f0 + 0.001), 0.187, 0.191, M["Energy_Hot"], p.Pc)
box("sensor", -0.070, -0.030, F(0.120), F(0.050), 0.196, 0.222, M["Armor_Dark"], p.U, bevel=0.004)
box("sensor_eye", -0.064, -0.036, F(0.1215), F(0.1185), 0.202, 0.216, M["Sensor_Eye_Red"], p.U)
# side arm: tapered beam from the gearbox to the spindle, on +X
hull("arm", [(x, F(f), z) for (f, z0, z1) in ((0.120, P - 0.040, P + 0.050), (BF, P - 0.034, P + 0.034)) for x in (0.030, 0.072) for z in (z0, z1)],
     M["Armor_Gunmetal"], p.U, bevel=0.005)
hull("belt_cover", [(x, F(f), z) for (f, z0, z1) in ((0.150, P - 0.032, P + 0.042), (BF - 0.010, P - 0.030, P + 0.030)) for x in (0.071, 0.080) for z in (z0, z1)],
     M["Armor_Plate_Light"], p.U, bevel=0.003)
box("arm_team", 0.0795, 0.0835, F(0.200), F(0.500), P + 0.012, P + 0.022, M["TeamColor_Red"], p.Pc)
for f_ in (0.190, 0.340, 0.490):
    hexbolt("cover_bolt", (0.079, F(f_), P - 0.018), (0.0845, F(f_), P - 0.018), M["Steel_Light"], p.Pc)
cyl("spindle", 0.048, (0.022, F(BF), P), (0.084, F(BF), P), M["Armor_Dark"], p.U, seg=32, bevel=0.004)
tube("spindle_glow", 0.040, 0.030, (0.083, F(BF), P), (0.0865, F(BF), P), M["Energy_Hot"], p.Pc, seg=32)
for i in range(6):
    a = 2*math.pi*(i + 0.5)/6
    hexbolt("spindle_bolt", (0.083, F(BF) + 0.036*math.cos(a), P + 0.036*math.sin(a)), (0.0885, F(BF) + 0.036*math.cos(a), P + 0.036*math.sin(a)), M["Steel_Light"], p.Pc)
cyl("spindle_bore", 0.0195, (0.015, F(BF), P), (0.064, F(BF), P), M["Armor_Dark"], p.D, seg=24)
# guard: two side plates + rim band over the upper-rear sector, struts to the arm
sector("guard_in", 0.200, 0.335, G0, G1, 0.018, 0.024, M["Armor_Gunmetal"], p.U)
sector("guard_out", 0.230, 0.335, G0, G1, -0.024, -0.018, M["Armor_Gunmetal"], p.U)
for (a0, a1) in ((80.0, 104.0), (116.0, 140.0), (152.0, 176.0)):
    sector("guard_vent", 0.262, 0.300, a0, a1, -0.030, -0.012, M["Armor_Dark"], p.D)
    sector("guard_vent_glow", 0.264, 0.298, a0 + 1.0, a1 - 1.0, -0.0205, -0.0195, M["Energy_Hot"], p.Pc)
sector("guard_rim", 0.3255, 0.3380, G0 + 0.3, G1 - 0.3, -0.0245, 0.0245, M["Armor_Dark"], p.U)
sector("guard_lip", 0.314, 0.343, G0 - 0.5, G0 + 4.0, -0.0262, 0.0262, M["Armor_Plate_Light"], p.U)
sector("guard_lip2", 0.314, 0.343, G1 - 4.0, G1 + 0.5, -0.0262, 0.0262, M["Armor_Plate_Light"], p.U)
sector("guard_haz", 0.3368, 0.3395, 95.0, 125.0, -0.0162, 0.0162, M["Hazard_Yellow"], p.Pc)
for k in range(5):
    ph = 96.0 + 6.0*k
    hull(f"guard_chev{k}", [ring_pt(r, a, x) for r in (0.3373, 0.3405) for (a, x) in ((ph, -0.0155), (ph + 2.0, -0.0155), (ph + 4.5, 0.0155), (ph + 2.5, 0.0155))], M["Armor_Dark"], p.Pc)
for ph in (70.0, 110.0, 150.0, 190.0):
    for x0 in (-0.0255, 0.0235):
        cyl("guard_bolt", 0.0045, ring_pt(0.315, ph, x0), ring_pt(0.315, ph, x0 + 0.002), M["Steel_Light"], p.Pc, seg=6)
hull("guard_strut", [ring_pt(r, ph, x) for r in (0.200, 0.222) for ph in (178.0, 196.0) for x in (0.020, 0.040)], M["Armor_Gunmetal"], p.U, bevel=0.002)
hull("guard_strut2", [ring_pt(r, ph, x) for r in (0.200, 0.222) for ph in (120.0, 138.0) for x in (0.020, 0.040)], M["Armor_Gunmetal"], p.U, bevel=0.002)
p.build(pitch)
# ---- blade: dark coated disc, ground steel edge band, heat-tempered teeth, lightening holes, expansion slots, flanged hub
blade = cyl(ID + "_Blade", 0.272, (-0.0030, F(BF), P), (0.0030, F(BF), P), M["Armor_Gunmetal"], work, seg=72)
b = Part(work, "b")
tube("edge_band", 0.287, 0.268, (-0.0032, F(BF), P), (0.0032, F(BF), P), M["Steel_Light"], b.U, seg=72)
for i in range(TEETH):
    ph = i*360.0/TEETH
    pts = [ring_pt(0.283, ph - 6.0, x) for x in (-0.0055, 0.0055)] + [ring_pt(0.283, ph + 7.0, x) for x in (-0.0055, 0.0055)] \
        + [ring_pt(RB, ph - 4.5, x) for x in (-0.0055, 0.0055)] + [ring_pt(0.294, ph + 1.5, x) for x in (-0.0055, 0.0055)]
    hull(f"tooth{i}", pts, M["Barrel_Heat"], b.U)
for i in range(6):
    ph = i*60.0 + 30.0
    cyl(f"hole{i}", 0.030, ring_pt(0.175, ph, -0.01), ring_pt(0.175, ph, 0.01), M["Armor_Dark"], b.D, seg=20)
    tube(f"hole_rim{i}", 0.036, 0.0292, ring_pt(0.175, ph, -0.0036), ring_pt(0.175, ph, 0.0036), M["Steel_Light"], b.U, seg=20)
    hull(f"slot{i}", [ring_pt(r, i*60.0 + d, x) for r in (0.205, 0.262) for d in (-0.40, 0.40) for x in (-0.01, 0.01)], M["Armor_Dark"], b.D)
    cyl(f"slot_end{i}", 0.006, ring_pt(0.205, i*60.0, -0.01), ring_pt(0.205, i*60.0, 0.01), M["Armor_Dark"], b.D, seg=10)
for s in (-1, 1):
    cyl(f"flange{s}", 0.072, (-s*0.002, F(BF), P), (s*0.012, F(BF), P), M["Armor_Dark"], b.U, seg=36, bevel=0.003)
    tube(f"flange_glow{s}", 0.060, 0.052, (s*0.0115, F(BF), P), (s*0.0135, F(BF), P), M["Energy_Hot"], b.Pc, seg=36)
    for i in range(6):
        a = 2*math.pi*(i + 0.5)/6
        hexbolt(f"hub_bolt{s}{i}", (s*0.011, F(BF) + 0.040*math.cos(a), P + 0.040*math.sin(a)), (s*0.017, F(BF) + 0.040*math.cos(a), P + 0.040*math.sin(a)), M["Steel_Light"], b.Pc, r=0.0055)
cyl("arbor", 0.016, (-0.016, F(BF), P), (0.046, F(BF), P), M["Steel_Light"], b.U, seg=24)
cyl("arbor_nut", 0.024, (-0.024, F(BF), P), (-0.0105, F(BF), P), M["Steel_Light"], b.U, seg=6)
b.build(blade)
# ---- FX: speed-blur disc (visible only while spinning fast) + jam sparks
blur = tube("Saw_BlurDisc", 0.302, 0.262, (-0.0075, F(BF), P), (0.0075, F(BF), P), M["Steel_Light"], work, seg=72)
sparks = L.build_flash("Saw_Sparks", work, work, length=0.090, radius=0.030, spikes=5)
# sweep volume (cutting disc envelope) for the colliders GLB / QA preview
sweepm = bpy.data.materials.new("Collider_Preview"); sweepm.diffuse_color = (0.7, 0.2, 1.0, 1)
sweep = cyl("SWEEP_Saw_CuttingDisc", RB + 0.002, (-0.0060, F(BF), P), (0.0060, F(BF), P), sweepm, work, seg=72)
# ---- hierarchy
col = new_col("R4W_RotarySaw")
for o in (base, yaw, pitch, blade, blur, sparks, sweep):
    for c_ in o.users_collection: c_.objects.unlink(o)
    col.objects.link(o)
bpy.data.collections.remove(work)
root, pivot, socket = L.finish_rig(ID, col, base, yaw, pitch, pitch_range=(DN, UP))
scn.frame_set(1); bpy.context.view_layer.update()
BC = (0, F(BF), P)
L.attach(blade, pitch, BC); L.attach(blur, pitch, BC); L.attach(sweep, pitch, BC)
sweep.display_type = 'WIRE'; sweep.hide_render = True
blur.scale = (0.001,)*3
CUT_PH = -35.0
sp = ring_pt(RB + 0.004, CUT_PH, 0.0)
sparks.parent = pitch; sparks.location = Vector(sp) - Vector((0, 0, P)); sparks.scale = (0.001,)*3
sparks.rotation_euler = (math.radians(125.0), 0, 0)   # along the tooth path at the cut point (down-cut)
muzzle = L.empty("Muzzle", pitch, (0, F(BF), 0), col, 'SINGLE_ARROW', 0.06, L.UP90)
L.set_props({root: {"unity_up": "+Y", "unity_forward": "+Z", "unit": "meter", "version": "v2", "weapon_kind": "R4WeaponKind.RotarySaw",
                    "style": "R41_Turret_v6.1 standard (heavy industrial armour + team colour)", "issue": "#938"},
             pivot: {"role": "game-driven pivot", "physical": "(0,0.135,0)"},
             yaw: {"role": "Yaw: rotate around local Y only"}, pitch: {"role": "Pitch: rotate around local X only (arm + guard)", "pitch_range_deg": f"-{DN:.0f}..+{UP:.0f}"},
             blade: {"role": "physical saw blade: spins around local X (game axle = muzzle.right)", "radius_m": RB, "teeth": TEETH, "max_rpm_game": 2800},
             blur: {"role": "speed-blur disc FX: scale 0.001 idle/slow, 1 above ~450 rpm (game minimumCutRpm)"},
             sparks: {"role": "jam/contact sparks FX at the cutting point - idle scale 0.001"},
             socket: {"role": "mounting face centre, normal -Y, connects to cell (0,-1,0)"},
             muzzle: {"role": "saw disc centre (contact origin); local X = blade axle", "physical": f"(0,0.135,{BF})"},
             sweep: {"role": "cutting-disc envelope (QA / colliders GLB only, not rendered)"}})
PARTS = [base, yaw, pitch, blade]; FX = [blur, sparks]
for o in PARTS + FX: L.shade_and_uv(o)
# ---- animation: 24 fps. Spin loop = 1 rev / 12 frames (120 rpm at playback speed 1). Runtime drives speed from RPM.
T = L.Timeline()
RA = (blade, "rotation_euler", 0)
rest = [(yaw, "rotation_euler", 2, 0.0), (pitch, "rotation_euler", 0, 0.0), (blade, "rotation_euler", 0, 0.0)]
L.hold(T, rest, "Idle", 24, "motor off, blade parked")
L.standard_aim(T, yaw, pitch, UP, DN)
W_ = 30.0   # deg/frame at loop speed
def ang(f, deg, interp='LINEAR'): L.key(blade, "rotation_euler", 0, f, math.radians(deg), interp)
def fx(o, f, sc): L.key3(o, "scale", f, (sc,)*3, 'CONSTANT')
s, e = T.clip("SpinUp", 24, desc="motor start: angular speed 0 -> loop speed (quadratic), blur disc fades in at the end")
fx(blur, s - 1, 0.001); fx(sparks, s - 1, 0.001)
for t in range(0, 25): ang(s + t, W_*t*t/(2*24.0))
fx(blur, s, 0.001); fx(blur, s + 16, 1.0)
T.clips[-1]["render_frames"] = [s + 20]
s, e = T.clip("Spin", 12, loop=True, desc="steady spin, seamless loop: 1 rev / 12 f; play at x23.33 for 2800 rpm or drive Blade local X from runtime RPM")
fx(blur, s, 1.0)
for t in range(0, 13): ang(s + t, W_*t)
fx(blur, e, 1.0); T.clips[-1]["render_frames"] = [s + 5]; HERO = s + 5
s, e = T.clip("SpinDown", 36, desc="motor off: deceleration to rest (game decel 2200 vs accel 3500 rpm/s -> longer than SpinUp); blur fades out")
fx(blur, s, 1.0)
for t in range(0, 37): ang(s + t, W_*t - W_*t*t/(2*36.0))
fx(blur, s + 12, 0.001); T.clips[-1]["render_frames"] = [s + 4]
s, e = T.clip("Jam", 36, desc="hard target: blade stops in 3 f with kick-back, arm judder, sparks; held (game jamSeconds 1.2) then released")
fx(blur, s, 1.0); fx(sparks, s, 0.001)
ang(s, 0.0); ang(s + 1, 30.0); ang(s + 2, 52.0); ang(s + 3, 58.0, 'BEZIER'); ang(s + 5, 50.0, 'BEZIER'); ang(s + 8, 54.0, 'BEZIER'); ang(e - 6, 54.0, 'BEZIER'); ang(e, 0.0 + 60.0, 'BEZIER')
fx(blur, s + 2, 0.001)
fx(sparks, s + 1, 1.0); fx(sparks, s + 3, 0.6); fx(sparks, s + 5, 1.0); fx(sparks, s + 7, 0.001)
for df, d in ((0, 0.0), (2, 2.2), (4, -1.4), (6, 0.9), (9, -0.4), (12, 0.0)):
    L.key(pitch, "rotation_euler", 0, s + df, math.radians(-d), 'BEZIER')
L.key(pitch, "rotation_euler", 0, e, 0.0)
T.clips[-1]["render_frames"] = [s + 2]; HERO2 = s + 2
fx(blur, T.end, 0.001); fx(sparks, T.end, 0.001)
for o, pth, idx, v in rest[:2]: L.key(o, pth, idx, T.end, v)
for o in (yaw, pitch, blade, blur, sparks): L.name_action(o, o.name + "_Master")
scn.frame_set(1)
def game_disc_check():
    """game RotarySawConfiguration (bladeRadius 0.9, halfThickness 0.14, hubDeadZone 0.06, axle = muzzle.right):
    report how the runtime contact disc relates to this model's own armour (report only - gameplay is not changed)"""
    c = muzzle.matrix_world.translation; ax = (muzzle.matrix_world.to_3x3() @ Vector((1, 0, 0))).normalized()
    near = 9.0; hit = 0
    for o in (base, yaw, pitch):
        for v in o.data.vertices:
            w = o.matrix_world @ v.co; d = w - c; axial = abs(d.dot(ax)); planar = (d - ax*d.dot(ax)).length
            near = min(near, d.length)
            if axial <= 0.14 and 0.06 <= planar <= 0.9: hit += 1
    vis_hit = 0
    for o in (base, yaw):
        for v in o.data.vertices:
            w = o.matrix_world @ v.co; d = w - c; axial = abs(d.dot(ax)); planar = (d - ax*d.dot(ax)).length
            if axial <= 0.0055 and planar <= RB: vis_hit += 1
    return dict(visual_blade_radius_m=RB, game_bladeRadius_m=0.9, game_halfThickness_m=0.14, nearest_own_armour_vertex_to_disc_centre_m=round(near, 4),
                own_vertices_inside_game_contact_disc=hit, own_vertices_inside_visual_blade_disc=vis_hit, PASS=vis_hit == 0,
                risk="game contact disc (r=0.9 m, +-0.14 m) is larger than the visual blade; it is a runtime value - integration (B01) decides; not changed here")
S = dict(id=ID, kind="R4WeaponKind.RotarySaw", game_name="WPN-005 Rotary Saw", out=OUT, root=root, pivot=pivot, yaw=yaw, pitch=pitch, socket=socket,
         export=[root, base, socket, pivot, yaw, pitch, blade, blur, sparks, muzzle], parts=PARTS, fx=FX, muzzles=[muzzle],
         static=[base], yawparts=[yaw], gun=[pitch, blade], pitch_range=(DN, UP), frame_end=T.end, clips=T.clips, skip_lof=True,
         clash_pairs=[(blade, pitch), (blade, yaw), (blade, base), (pitch, yaw), (pitch, base), (yaw, base)],
         hero_frame=HERO, hero_frame2=HERO2, tri_budget=22000, extra_checks=lambda: {"game_contact_disc": game_disc_check()},
         anim_extra={"spin_loop": "1 rev / 12 frames = 120 rpm at speed 1", "playback_speed_for_2800rpm": round(2800/120.0, 3),
                     "blade_rotation": "Blade local X (file/Unity X), positive = front teeth moving down (down-cut, sparks thrown down/back)", "runtime_drive": "recommended: RRLSawBinding RPM -> Blade.localRotation; BlurDisc on above 450 rpm"},
         exits_note="contact weapon: Muzzle = disc centre (no projectile)",
         notes=["Blade 0.60 m diameter on a +X side arm; disc centre 0.620 m forward of the pivot.",
                "Runtime placeholder rotor (R41PartGeometry.SawRotor) and RotarySawConfiguration are NOT changed (see extra_checks.game_contact_disc)."])
S["notes"] = list(S.get("notes", [])) + [f"v2 silhouette pass (#939 follow-up): base style {BASE_STYLE}, head {HEAD}; rig, pivot, muzzle contract and clips unchanged."]
S["base_style"], S["head_style"] = BASE_STYLE, HEAD
Q.run(S)
