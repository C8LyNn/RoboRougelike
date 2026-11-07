# Generates the v2 (silhouette diversity) weapon builders from the v1 builders of PR #939 by explicit text patches.
# v1 sources are kept untouched as build_<name>_v1.py; every patch must match exactly once.
import os, re, shutil, sys
D = os.path.dirname(os.path.abspath(__file__))
IMP_A = "import importlib, bpy, r4lib as L, r4qa as Q; importlib.reload(L); importlib.reload(Q)"
IMP_B = "import importlib, bpy, r4lib as L, r4qa as Q, r4base as BS, r4heads as H; importlib.reload(L); importlib.reload(Q); importlib.reload(BS); importlib.reload(H)"

def between(s, start, end_incl):
    i = s.index(start); j = s.index(end_incl, i) + len(end_incl); return s[i:j]

def common(s, style, head):
    assert s.count(IMP_A) == 1; s = s.replace(IMP_A, IMP_B)
    m = re.search(r'base, _ = L\.build_base\(ID \+ "_Base", work[^\n]*\)\n', s); assert m
    s = s[:m.start()] + f'BASE_STYLE, HEAD = "{style}", "{head}"\nbase, _ = BS.build_base_style(ID + "_Base", work, BASE_STYLE)\n' + s[m.end():]
    s, n = re.subn(r'L\.root_props\(root, ("R4WeaponKind\.\w+")\)', r'L.root_props(root, \1, version="v2")', s)
    if n == 0: s, n = re.subn(r'("unit": "meter", "version": )"v1"', r'\1"v2"', s)
    assert n == 1
    assert s.count("\nQ.run(S)") == 1
    s = s.replace("\nQ.run(S)", '\nS["notes"] = list(S.get("notes", [])) + [f"v2 silhouette pass (#939 follow-up): base style {BASE_STYLE}, head {HEAD}; rig, pivot, muzzle contract and clips unchanged."]\nS["base_style"], S["head_style"] = BASE_STYLE, HEAD\nQ.run(S)')
    return s

def rep(s, a, b):
    assert s.count(a) == 1, ("patch miss", a[:80], s.count(a)); return s.replace(a, b)

def heavy(s):
    s = common(s, "stepped", "low sponsons + drum mantlet + wide sloped gun shield + stubby howitzer barrel")
    s = rep(s, between(s, "L.yoke(y, 0.118, 0.160,", 'hexbolt(f"skirt_bolt{s}", (s*0.174, F(f_), 0.078), (s*0.179, F(f_), 0.078), M["Steel_Light"], y.Pc)\n'), "H.sponsons(y)\n")
    s = rep(s, 'pitch = L.trunnion(ID + "_Pitch", work, p, half=0.112, r=0.056, axle=0.150)', 'pitch = H.drum_trunnion(ID + "_Pitch", work, p, half=0.140, r=0.088, axle=0.172)')
    s = rep(s, '((0.170, 0.100, 0.082, 0.194), (0.218, 0.084, 0.092, 0.182))', '((0.150, 0.128, 0.072, 0.212), (0.212, 0.112, 0.082, 0.198))')
    i = s.index("p.build(pitch)"); s = s[:i] + "H.gun_shield(p)\n" + s[i:]
    # stubby howitzer barrel: 0.10 m shorter, chunkier double-baffle brake (evacuator position unchanged: clears the cradle in recoil)
    s = rep(s, "BARREL_END = 0.660; MUZZLE_F = 0.700;", "BARREL_END = 0.560; MUZZLE_F = 0.600;")
    s = rep(s, 'barrel = cyl(ID + "_Barrel", BR, (0, F(0.098), P), (0, F(0.600), P)', 'barrel = cyl(ID + "_Barrel", BR, (0, F(0.098), P), (0, F(0.520), P)')
    s = rep(s, "for (f0, f1) in ((0.344, 0.374), (0.378, 0.408), (0.496, 0.524), (0.528, 0.558)):", "for (f0, f1) in ((0.344, 0.374), (0.378, 0.408)):")
    s = rep(s, 'box("brake", -0.062, 0.062, F(0.580), F(BARREL_END), P - 0.046, P + 0.046,', 'box("brake", -0.074, 0.074, F(0.492), F(BARREL_END), P - 0.056, P + 0.056,')
    s = rep(s, 'cyl("mz_bore", 0.023, (0, F(0.500), P)', 'cyl("mz_bore", 0.023, (0, F(0.420), P)')
    s = rep(s, "for (p0, p1) in ((0.592, 0.610), (0.620, 0.638)):\n    box(\"baffle\", -0.070, 0.070,", "for (p0, p1) in ((0.504, 0.522), (0.532, 0.550)):\n    box(\"baffle\", -0.084, 0.084,")
    return s

def shotgun(s):
    s = common(s, "box", "single cantilever arm + side drum magazine")
    s = rep(s, between(s, "L.yoke(y, 0.122, 0.162,", "bolts=((0.098, 0.078), (-0.112, 0.066)))\n"), "H.single_arm(y, 1, 0.122, 0.170)\n")
    i = s.index("p.build(pitch)"); s = s[:i] + "H.side_drum(p, -1, 0.110, 0.204, 0.080)\n" + s[i:]
    return s

def railgun(s):
    s = common(s, "column", "thin tall fork + capacitor spires")
    s = rep(s, between(s, "L.yoke(y, 0.112, 0.152,", "bolts=((0.105, 0.082), (-0.118, 0.068), (-0.030, 0.196)))\n"), "H.fork(y)\n")
    s = rep(s, between(s, 'box("cap_bank"', 'box("bank_haz", -0.100, 0.100, F(-0.2365), F(-0.2375), 0.037, 0.050, M["Hazard_Yellow"], y.Pc)\n'), "H.cap_spires(y)\n")
    return s

def beamcutter(s):
    s = common(s, "finned", "ball head in a gimbal ring")
    s = rep(s, between(s, "L.yoke(y, 0.108, 0.150,", "(-0.090, 0.1745)])\n"), "H.gimbal(y)\n")
    s = rep(s, 'pitch = L.trunnion(ID + "_Pitch", work, p, half=0.100, r=0.048, axle=0.140)', 'pitch = H.ball(ID + "_Pitch", work, p)')
    s = rep(s, between(s, 'hull("receiver"', 'box(f"rcv_team{s}", s*0.0978, s*0.0994, F(-0.040), F(0.110), 0.150, 0.162, M["TeamColor_Red"], p.Pc)\n'), "")
    s = rep(s, 'cyl("housing", 0.052, (0, F(0.150), P)', 'cyl("housing", 0.052, (0, F(0.062), P)')
    s = rep(s, between(s, 'box("sensor", -0.070, -0.030, F(0.150), F(0.060)', 'box(f"top_glow{k}", -0.008, 0.073, F(f0 + 0.009), F(f0 + 0.001), 0.189, 0.193, M["Energy_Hot"], p.Pc)\n'), "")
    return s

def rotarysaw(s): return common(s, "crab", "unchanged v1 saw arm")
def empemitter(s):
    s = common(s, "drum", "unchanged v1 pulse head")
    # R4 port plate adds ~1.5k tris: same 22k budget as Heavy/Railgun/Rocket/Saw
    return rep(s, "hero_frame=HERO, hero_frame2=HERO2, tri_budget=20000, lof_pose_note", "hero_frame=HERO, hero_frame2=HERO2, tri_budget=22000, lof_pose_note")

JOBS = {"build_heavy.py": heavy, "build_shotgun.py": shotgun, "build_railgun.py": railgun, "build_beamcutter.py": beamcutter,
        "build_rotarysaw.py": rotarysaw, "build_empemitter.py": empemitter}
for fn, f in JOBS.items():
    v1 = os.path.join(D, fn.replace(".py", "_v1.py"))
    if not os.path.exists(v1): shutil.copy(os.path.join(D, fn), v1)
    s = open(v1, encoding="utf-8").read()
    out = f(s)
    open(os.path.join(D, fn), "w", encoding="utf-8").write(out)
    print("patched", fn, len(s), "->", len(out))
