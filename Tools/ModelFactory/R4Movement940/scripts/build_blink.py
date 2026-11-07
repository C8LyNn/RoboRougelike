# R3PartKind.Blink / MOV003 - "Blink" - phase emitter: hex reactor drum mounted from BELOW (Socket_Mount on the -Y face),
# 6 hinged emitter petals around a hex crystal. Charge (petals open, crystal rises) -> Activation (flash + phase ring) -> Recover.
# The teleport itself (8 m, 6 s cooldown) stays in BlinkSettings - this is only its visible device.
import sys, os, math; sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import importlib, bpy, numpy as np, r4lib as L, r4qa as Q; importlib.reload(L); importlib.reload(Q)
from mathutils import Vector, Matrix
from r4lib import F, M, box, cyl, cone, tube, hull, prism_x, revolve, hexbolt, Part, new_col
OUT = sys.argv[sys.argv.index("--") + 1]
L.reset_scene(); L.init_palette(); scn = bpy.context.scene
work = new_col("_build_tmp"); ID = "R4M_Blink"
NPT = 6; RH = 0.168; ZH = 0.030; PL = 0.165; REST, OPEN = -16.0, 22.0
def hexa(r, z, rot=0.0): return [(r*math.cos(math.radians(60*k + rot)), r*math.sin(math.radians(60*k + rot)), z) for k in range(6)]
hous = hull(ID + "_Housing", hexa(0.200, -0.262) + hexa(0.205, -0.120) + hexa(0.185, ZH), M["Armor_Gunmetal"], work, bevel=0.005)
h = Part(work, "h")
cyl("base_flange", 0.215, (0, 0, -0.275), (0, 0, -0.258), M["Armor_Plate_Light"], h.U, seg=6, bevel=0.003)
for k in range(6):
    a = math.radians(30 + 60*k); ca, sa = math.cos(a), math.sin(a)
    hexbolt(f"flange_bolt{k}", (0.185*math.cos(math.radians(60*k)), 0.185*math.sin(math.radians(60*k)), -0.262), (0.185*math.cos(math.radians(60*k)), 0.185*math.sin(math.radians(60*k)), -0.254), M["Steel_Light"], h.Pc, r=0.008)
    made = [box("hex_panel", 0.170, 0.181, -0.060, 0.060, -0.210, -0.040, M["Armor_Plate_Light"], h.U, bevel=0.002),
            box("hex_slot", 0.168, 0.200, -0.040, 0.040, -0.180, -0.165, M["Armor_Dark"], h.D), box("hex_glow", 0.1665, 0.1695, -0.038, 0.038, -0.178, -0.167, M["Energy_Hot"], h.Pc)]
    if k % 2 == 0: made.append(box("hex_team", 0.1805, 0.1830, -0.045, 0.045, -0.090, -0.070, M["TeamColor_Red"], h.Pc))
    else: made.append(box("hex_haz", 0.1805, 0.1830, -0.045, 0.045, -0.090, -0.082, M["Hazard_Yellow"], h.Pc))
    for o in made: o.data.transform(Matrix.Rotation(a, 4, 'Z'))
for k in range(6):
    a = math.radians(60*k)
    hull(f"corner_rib{k}", [(rr*math.cos(a + d), rr*math.sin(a + d), z) for rr in (0.196, 0.214) for d in (-0.05, 0.05) for z in (-0.250, -0.020)], M["Armor_Dark"], h.U, bevel=0.002)
    for zc in (-0.200, -0.120): hexbolt(f"rib_bolt{k}", (0.213*math.cos(a), 0.213*math.sin(a), zc), (0.219*math.cos(a), 0.219*math.sin(a), zc), M["Steel_Light"], h.Pc, r=0.006)
tube("well_ring", 0.150, 0.080, (0, 0, ZH - 0.004), (0, 0, ZH + 0.016), M["Armor_Dark"], h.U, seg=36)
cyl("well", 0.078, (0, 0, -0.060), (0, 0, ZH + 0.030), M["Armor_Dark"], h.D, seg=36)
tube("well_glow", 0.079, 0.072, (0, 0, -0.040), (0, 0, -0.030), M["Energy_Hot"], h.Pc, seg=36)
for k in range(NPT):
    a = math.radians(60*k)
    hull(f"hinge_block{k}", [(rr*math.cos(a + d), rr*math.sin(a + d), z) for rr in (RH - 0.022, RH + 0.016) for d in (-0.23, 0.23) for z in (ZH - 0.006, ZH + 0.006)], M["Armor_Dark"], h.U)
h.build(hous)
crys = hull(ID + "_Crystal", hexa(0.050, 0.020, 30) + hexa(0.050, 0.110, 30) + [(0, 0, -0.035), (0, 0, 0.175)], M["Energy_Hot"], work)
cr = Part(work, "c")
tube("crys_collar", 0.058, 0.040, (0, 0, 0.004), (0, 0, 0.022), M["Steel_Light"], cr.U, seg=6)
cr.build(crys)
def petal(name):
    pts = [(x, y, z) for (x, z, y0) in ((-0.050, 0.0, 0.0), (0.050, 0.0, 0.0), (-0.030, PL*0.6, -0.006), (0.030, PL*0.6, -0.006), (-0.012, PL, -0.018), (0.012, PL, -0.018)) for y in (y0, y0 - 0.012)]
    pt = hull(name, pts, M["Armor_Gunmetal"], work, bevel=0.0015)
    q = Part(work, "p" + name[-1])
    hull("emitter_strip", [(x, y, z) for (x, z) in ((-0.010, 0.020), (0.010, 0.020), (-0.006, PL*0.85), (0.006, PL*0.85)) for y in (0.0015, -0.002)], M["Energy_Hot"], q.Pc)
    cyl("knuckle", 0.007, (-0.040, -0.006, 0.0), (0.040, -0.006, 0.0), M["Steel_Light"], q.U, seg=10)
    box("petal_team", -0.020, 0.020, -0.0135, -0.0115, 0.050, 0.075, M["TeamColor_Red"], q.Pc)
    q.build(pt); return pt
petals = [petal(f"{ID}_Petal_{k + 1}") for k in range(NPT)]
ring = L.tube("Blink_PhaseRing", 0.120, 0.105, (0, 0, -0.005), (0, 0, 0.005), M["Muzzle_Flash"], work, seg=48)
flash = L.revolve("Blink_Flash", [(0, -0.09), (0.07, -0.05), (0.09, 0.0), (0.07, 0.05), (0, 0.09)], M["Muzzle_Flash"], work, 16, local=True)
cglow = L.revolve("Blink_ChargeGlow", [(0, -0.05), (0.06, -0.02), (0.065, 0.02), (0, 0.10)], M["Energy_Hot"], work, 16, local=True)
col = L.gather(ID, [hous, crys, ring, flash, cglow] + petals)
root = L.empty(ID + "_ROOT", None, (0, 0, 0), col, 'PLAIN_AXES', 0.1)
hous.parent = root; crys.parent = root
hinges = []
for k, pt in enumerate(petals):
    a = math.radians(60*k); rad = Vector((math.cos(a), math.sin(a), 0)); yv = -rad; zv = Vector((0, 0, 1)); xv = yv.cross(zv)
    Mh = Matrix((xv, yv, zv)).transposed().to_4x4(); Mh.translation = rad*RH + Vector((0, 0, ZH + 0.016))
    em = L.empty(f"PetalHinge_{k + 1}", root, (0, 0, 0), col, 'SINGLE_ARROW', 0.03); em.matrix_basis = Mh
    pt.parent = em; pt.location = (0, 0, 0); pt.rotation_euler = (math.radians(REST), 0, 0); hinges.append(em)
for o, z in ((ring, 0.120), (flash, 0.110), (cglow, 0.060)): o.parent = root; o.location = (0, 0, z); o.scale = (0.001,)*3
import r4port as P4; P4.port(hous, work, -1)
socket = L.empty("Socket_Mount", root, (0, 0, -P4.FACE), col, 'SINGLE_ARROW', 0.08, (math.pi, 0, 0))
emit = L.empty("Blink_Emitter", root, (0, 0, 0.180), col, 'SINGLE_ARROW', 0.06)
L.root_props(root, "R3PartKind.Blink", version="v2-port")
L.set_props({hous: {"role": "static hex reactor drum; mounts from BELOW (Socket_Mount on the -Y face)"},
             crys: {"role": "phase crystal: translates along local Y and spins about local Y"},
             ring: {"role": "phase ring FX on activation"}, flash: {"role": "activation flash FX"}, cglow: {"role": "charge glow FX"},
             socket: {"role": "mount face centre on the cell BOTTOM, normal -Y"}, emit: {"role": "effect origin (visual)"}})
for pt in petals: pt["role"] = f"emitter petal: rotates about local X (PetalHinge_k): {REST:.0f} deg closed bud, {OPEN:.0f} deg open"
PARTS = [hous, crys] + petals; FX = [ring, flash, cglow]
for o in PARTS + FX: L.shade_and_uv(o)
T = L.Timeline()
def pk(f, d, i='BEZIER'):
    for pt in petals: L.key(pt, "rotation_euler", 0, f, math.radians(d), i)
def ck(f, z, spin, i='BEZIER'): L.key(crys, "location", 2, f, z, i); L.key(crys, "rotation_euler", 2, f, math.radians(spin), 'LINEAR')
def fx(o, f, sc, i='LINEAR'): L.key3(o, "scale", f, (sc,)*3, i)
s, e = T.clip("Idle", 48, loop=True, desc="standby: petals closed, crystal turns 60 deg (6-fold, seamless)")
pk(s, REST); pk(e, REST); ck(s, 0.0, 0.0); ck(e, 0.0, 60.0)
for o in FX: fx(o, s, 0.001, 'CONSTANT')
s, e = T.clip("Charge", 24, desc="charge: petals open to 22 deg, crystal rises 50 mm spinning up, glow grows")
pk(s, REST); pk(s + 18, OPEN); pk(e, OPEN)
for t in range(0, 25, 4): ck(s + t, 0.050*min(1.0, t/18.0), 120.0*(t/24.0)**2)
fx(cglow, s, 0.001); fx(cglow, e, 1.0)
T.clips[-1]["render_frames"] = [e - 2]; HERO2 = e - 2
s, e = T.clip("Activate", 10, desc="activation: flash + phase ring expands, petals kick to 30 deg and settle")
pk(s, OPEN); pk(s + 2, 30.0, 'LINEAR'); pk(e, OPEN)
for t in range(0, 11, 2): ck(s + t, 0.050, 120.0 + 30.0*t)
fx(cglow, s, 1.0, 'CONSTANT'); fx(cglow, s + 1, 0.001, 'CONSTANT')
L.flash_keys(flash, s, ((0, 1.0), (1, 1.3), (2, 0.6), (3, 0.001)))
fx(ring, s, 0.001, 'CONSTANT'); fx(ring, s + 1, 1.0); fx(ring, s + 5, 2.1); fx(ring, s + 6, 0.001, 'CONSTANT')
T.clips[-1]["render_frames"] = [s + 2]; HERO = s + 2
s, e = T.clip("Recover", 30, desc="recover: petals close, crystal lowers and coasts to 480 deg (= rest, 6-fold)")
pk(s, OPEN); pk(s + 22, REST); pk(e, REST)
ck(s, 0.050, 420.0); ck(s + 24, 0.0, 480.0); ck(e, 0.0, 480.0)
T.clips[-1]["render_frames"] = [s + 10]
for o in FX: fx(o, T.end, 0.001, 'CONSTANT')
L.key(crys, "rotation_euler", 2, T.end + 1, 0.0, 'CONSTANT')
for o in [crys] + FX + petals: L.name_action(o, o.name + "_Master")
scn.frame_set(1)
S = dict(id=ID, kind="R3PartKind.Blink", game_name="Blink (MOV003)", out=OUT, root=root, pivot=emit, yaw=None, pitch=None, socket=socket, movement=True,
         export=[root, hous, crys] + hinges + petals + FX + [socket, emit], parts=PARTS, fx=FX, muzzles=[], housing=[hous], ground_parts=[],
         ground_note="no ground contact (blink device inside the block volume; runtime placeholder = sphere d 0.442 at the block centre)",
         housing_strict=True, clearance_pairs=[(crys, hous), (petals[0], hous), (petals[0], crys)],
         clash_pairs=[(crys, hous)] + [(pt, hous) for pt in petals] + [(pt, crys) for pt in petals] + [(petals[i], petals[(i + 1) % NPT]) for i in range(NPT)],
         frame_end=T.end, clips=T.clips, hero_frame=HERO, hero_frame2=HERO2, tri_budget=20000, sil_center=(0, 0, 0.0), sil_scale=0.95,
         notes=["Petal knuckles sit 3 mm above the hinge blocks (pin clearance so the petal root clears the drum when opening).", "Mounted from below (cell -Y face) so the petals have the free upper half of the cell; all moving parts stay inside the cell."])
Q.run(S)
