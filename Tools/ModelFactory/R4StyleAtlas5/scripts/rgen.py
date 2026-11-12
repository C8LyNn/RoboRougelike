"""#1239 Visual Polish Pass 2, step 1 (all outputs under polish2/, the generated sources are git-ignored):
 variants  mesh_source/<Style>/<Id>.json - eight types; mechanism tracks sampled from rdefs.MECH on the unchanged clip timeline
 kit       kit_source/<Style>.json       - hull kit meshes (16 plate tiles x variants, gasket, wheel lip)
 scene     scene_source/<Style>.json     - blueprints, owned fragments (manifest), destruction scenarios, pass-1 hull for BEFORE; shots.json
 manifest  hull_layer/                   - the Codex-facing manifest: rules, kit index, fragments of every test blueprint and scenario
usage: python3 rgen.py [variants] [kit] [scene] [styles]"""
import json, os, sys, math, numpy as np
import akit as K, astyle as ST, aanim as AN, pdefs as PD, qstyle as QS, qdefs as QD, rstyle as RS, rdefs as R, rhull as RH, pgen
ROOT = os.path.join(K.PKG, "polish2"); P0 = PD.P
def sample(fn, clips, F):
    loc = [[0.0, 0.0, 0.0] for _ in range(F)]; quat = [[1.0, 0.0, 0.0, 0.0] for _ in range(F)]; scl = [1.0]*F; last = None
    for f in range(1, F + 1):
        c = next((c for c in clips if c["start"] <= f <= c["end"]), None)
        if c is not None: last = (c["name"], (f - c["start"])/float(c["end"] - c["start"]))
        elif last is not None: last = (last[0], 1.0)                 # gap frames between clips hold the end of the previous clip
        if last is None: continue
        l, ax, deg, s = fn(*last); loc[f - 1] = [round(float(x), 6) for x in l]; quat[f - 1] = [round(float(x), 6) for x in AN.qaa(ax, deg)]; scl[f - 1] = round(float(s), 5)
    return dict(loc=loc, quat=quat, scl=scl)
def variants(styles):
    PD.P = R.P; pgen.PS = RS; pgen.OUT = os.path.join(ROOT, "mesh_source"); con = {t: dict(v) for t, v in PD.CONCEPT.items()}
    for t in R.ORDER:
        row = []
        for s in styles:
            PD.CONCEPT[t][s] = " ".join((R.P[t][s].__doc__ or "structural core block (armour comes from the hull layer)").split()); R.MECH.clear(); r = pgen.gen(t, s); mech = dict(R.MECH)
            p = os.path.join(pgen.OUT, ST.LABEL[s], PD.CID[t] + ".json"); src = json.load(open(p)); an = src["animation"]; tr = {n: sample(fn, an["clips"], an["frames"]) for n, fn in mech.items() if n in src["nodes"]}
            if an["mode"] == "procedural": an["tracks"].update(tr)
            else: an["extra_tracks"] = tr
            src["vcol"] = RS.VCOL[s]; src["polish_pass"] = 2; src["mechanisms"] = {n: dict(parent=src["nodes"][n].get("parent") or "root", pivot=src["nodes"][n]["pivot"]) for n in tr}
            json.dump(src, open(p, "w"), separators=(",", ":")); row.append((r[0], r[1], len(tr)))
        print(t, PD.CID[t], row, flush=True)
    PD.P = P0; PD.CONCEPT.update(con)
def mats(S, s): return dict(style=s, materials={z: dict(rgb=list(v[0]), metallic=v[1], roughness=v[2]) for z, v in S.MAT[s].items()}, glow=[0.2, 0.8, 1.0], glow_strength=S.GLOW_STRENGTH[s], textures=S.TEXTURES[s])
def kit(styles):
    d = os.path.join(ROOT, "kit_source"); os.makedirs(d, exist_ok=True); pgen.PS = RS
    for s in styles:
        out = mats(RS, s); out["meshes"] = {}; tot = 0
        for name, fn in RH.kit(s).items():
            m = K.build(fn, s, False, None, "kit", False, None, True); st = K.stats(m); assert st["ok"], (s, name); out["meshes"][name] = dict(pgen.pack(m, s, (0, 0, 0), [[-0.3, -0.3, 0.2], [0.3, 0.3, 0.3]]), triangles=st["triangles"]); tot += st["triangles"]
        json.dump(out, open(os.path.join(d, ST.LABEL[s] + ".json"), "w"), separators=(",", ":")); print(s, "kit", len(out["meshes"]), "meshes", tot, "tris", flush=True)
def Rz(yaw, x, y, z): a = math.radians(yaw); c, s = math.cos(a), math.sin(a); return [[c, -s, 0, x], [s, c, 0, y], [0, 0, 1, z], [0, 0, 0, 1]]
def testbed():
    p = [RH._p("Cube", i, j, 0) for i in range(4) for j in range(5)] + [RH._p("Cube", 1, 0, 1), RH._p("Cube", 2, 0, 1)] + RH._wheels([(4, 1, 0, 1), (4, 3, 0, 1), (-1, 1, 0, -1), (-1, 3, 0, -1)])
    return p + RH._on("Gun", [(1, 4, 1)]) + RH._on("Battery", [(2, 0, 2)]) + RH._on("Deployer", [(1, 0, 2)])
SCEN = [("S0_intact", "stan wyjściowy", []), ("S1_plate_centre", "1 klocek ze środka płyty", [(1, 2, 0)]), ("S2_two_neighbours", "2 sąsiednie klocki", [(1, 2, 0), (2, 2, 0)]), ("S3_section", "cała sekcja (przedni rząd)", [(0, 4, 0), (1, 4, 0), (2, 4, 0), (3, 4, 0)]),
        ("S4_wheel", "koło", [(4, 3, 0)]), ("S5_weapon_mount", "klocek pod bronią", [(1, 4, 0)]), ("S6_plate_edge", "klocek na krawędzi płyty (narożnik)", [(0, 0, 0)])]
def shots():
    hero = np.array([0.62, -0.74, 0.40]); hero /= np.linalg.norm(hero); top = np.array([0.50, -0.58, 0.64]); top /= np.linalg.norm(top)
    def cam(t, dist, d=hero, lens=50): t = np.array(t, float); return dict(pos=(t + np.array(d)*dist).round(3).tolist(), target=t.tolist(), lens=lens)
    S = []; both = [[1920, 1080], [1280, 720]]
    for mode in ("before", "after"):
        S.append(dict(name="1a_models_" + mode, sets=[dict(robot="models1", mode=mode, M=Rz(0, 0, 0, 0.90), hull=False)], cam=cam((0, 0, 0.75), 7.6, np.array([0.30, -0.86, 0.40])/np.linalg.norm([0.30, -0.86, 0.40])), stands=[[(i - 3.5)*K.G, 0.0, 0.32, 0.5, 0.5, 0.64] for i in (0, 3)]))
        S.append(dict(name="1b_models_" + mode, sets=[dict(robot="models2", mode=mode, M=Rz(0, -0.9, 0, 0.26), hull=False), dict(robot="trapstand", mode=mode, M=Rz(0, 1.6, 0, 0.0), hull=False)], cam=cam((0.25, 0, 0.45), 6.4, np.array([0.30, -0.86, 0.40])/np.linalg.norm([0.30, -0.86, 0.40]))))
        S.append(dict(name="2_buggy_" + mode, sets=[dict(robot="buggy", mode=mode, M=Rz(0, 0, 0.3, 0.39))], cam=cam((0, -0.2, 0.6), 11.5)))
        S.append(dict(name="3_skimmer_" + mode, sets=[dict(robot="skimmer", mode=mode, M=Rz(0, 0, 0, 1.12))], cam=cam((0, 0, 0.95), 11.5)))
        S.append(dict(name="4_crawler_" + mode, sets=[dict(robot="crawler", mode=mode, M=Rz(0, 0, 0, 0.39))], cam=cam((0, -0.3, 1.0), 19.0)))
        S.append(dict(name="5_dreadnought_" + mode, sets=[dict(robot="dreadnought", mode=mode, M=Rz(0, 0, 0, 0.39))], cam=cam((0, -0.3, 1.2), 27.0)))
        S.append(dict(name="6_gameplay_" + mode, sets=[dict(robot="buggy", mode=mode, M=Rz(0, 0, 0, 0.39)), dict(robot="skimmer", mode=mode, M=Rz(150, -6.0, -10.0, 1.12)), dict(robot="buggy", mode=mode, M=Rz(-150, 7.0, -14.0, 0.39)),
                                                       dict(robot="buggy", mode=mode, M=Rz(165, -10.0, -20.0, 0.39)), dict(robot="crawler", mode=mode, M=Rz(180, 2.0, -29.0, 0.39))], cam=dict(pos=[0.0, 11.5, 9.5], target=[0.0, -6.0, 0.4], lens=32)))
        S.append(dict(name="7_distant_" + mode, crops=True, sets=[dict(robot="buggy", mode=mode, M=Rz(150, -9.0, -22.0, 0.39), tag="buggy_33m"), dict(robot="skimmer", mode=mode, M=Rz(-160, 6.0, -30.0, 1.12), tag="skimmer_42m"), dict(robot="buggy", mode=mode, M=Rz(200, -3.0, -44.0, 0.39), tag="buggy_56m"),
                                                                 dict(robot="crawler", mode=mode, M=Rz(170, 12.0, -58.0, 0.39), tag="crawler_71m"), dict(robot="skimmer", mode=mode, M=Rz(140, -16.0, -70.0, 1.12), tag="skimmer_83m"), dict(robot="buggy", mode=mode, M=Rz(180, 3.0, -82.0, 0.39), tag="buggy_94m")],
                      cam=dict(pos=[0.0, 11.5, 9.5], target=[0.0, -45.0, 0.5], lens=32)))
    for name, _, _ in SCEN: S.append(dict(name="8_destroy_" + name, res=[[1920, 1080]], sets=[dict(robot="testbed", mode="after", M=Rz(0, 0, 0, 0.39), scenario=name)], cam=cam((0, 0, 0.5), 9.0, top)))
    env = [[0, -110, 2.5, 90, 1.0, 5.0, 0, "concrete"], [-34, -45, 2.0, 1.0, 130, 4.0, 0, "concrete"], [34, -45, 2.0, 1.0, 130, 4.0, 0, "concrete"],
           [-9.5, -6, 1.5, 1.0, 1.0, 3.0, 0, "concrete"], [10.5, -8, 1.5, 1.0, 1.0, 3.0, 0, "concrete"], [-14, -15, 1.5, 1.2, 1.2, 3.0, 0, "concrete"], [14.5, -21, 1.5, 1.2, 1.2, 3.0, 0, "concrete"],
           [9.0, -3.0, 0.55, 1.1, 1.1, 1.1, 12, "crate"], [10.3, -3.4, 0.55, 1.1, 1.1, 1.1, -8, "crate"], [9.5, -3.2, 1.65, 1.1, 1.1, 1.1, 25, "crate"], [-7.5, -15.5, 0.55, 1.1, 1.1, 1.1, 30, "crate"], [-2.5, -19.5, 0.55, 1.1, 1.1, 1.1, 5, "crate"],
           [5.5, -21.0, 0.6, 5.0, 0.5, 1.2, 18, "barrier"], [-15.5, -8.5, 0.6, 0.5, 6.0, 1.2, 0, "barrier"], [17.0, -14.0, 0.6, 0.5, 7.0, 1.2, 10, "barrier"],
           [-22, -36, 1.75, 5.0, 5.0, 3.5, 0, "concrete"], [22, -40, 2.5, 7.0, 4.0, 5.0, 0, "concrete"], [-13.0, 2.0, 0.55, 1.1, 1.1, 1.1, 40, "crate"], [14.0, 3.0, 1.5, 1.0, 1.0, 3.0, 0, "concrete"],
           [-24, -64, 2.0, 4.0, 4.0, 4.0, 0, "concrete"], [24, -74, 1.5, 6.0, 3.0, 3.0, 0, "concrete"], [-8.0, -52.0, 0.6, 6.0, 0.5, 1.2, -12, "barrier"], [22.0, -50.0, 0.55, 1.1, 1.1, 1.1, 15, "crate"]]
    return dict(shots=S, env=env, resolutions=both, sun=dict(dir=[0.72, 0.22, 0.66], strength=5.5, color=[1.0, 0.94, 0.84], angle_deg=2.5), sky=dict(color=[0.62, 0.72, 0.86], strength=1.35),
                scenarios=[dict(name=n, title=t, removed_cells=[list(c) for c in cells]) for n, t, cells in SCEN])
def aabb(parts, c):
    W = np.array([QD.world(p["cell"], c) for p in parts] + [QD.world((p["cell"][0], p["cell"][1] + 2, p["cell"][2]), c) for p in parts if p["type"] == "Strut"]); return [(W.min(0) - 0.42).round(3).tolist(), (W.max(0) + 0.42).round(3).tolist()]
def scene(styles):
    d = os.path.join(ROOT, "scene_source"); os.makedirs(d, exist_ok=True); hd = os.path.join(ROOT, "hull_layer"); os.makedirs(hd, exist_ok=True); json.dump(shots(), open(os.path.join(ROOT, "shots.json"), "w"), indent=1)
    BP = dict(RH.BLUEPRINTS, testbed=testbed); report = {}
    for s in styles:
        out = dict(mats(RS, s), legacy=mats(QS, s), robots={}); man = dict(style=s, style_label=ST.LABEL[s], kit="hull_kit/%s/R4H_HullKit.glb" % ST.LABEL[s], plate=RH.PL[s], variants=RH.VARIANTS[s], blueprints={})
        for name, fn in BP.items():
            parts = fn(); c = QD.centre(parts); fr = RH.fragments(parts, s); errs = RH.check(parts, fr, range(len(parts))); assert not errs, (s, name, errs[:2])
            rb = dict(parts=[dict(type=p["type"], M=QD.matrix(p, c)) for p in parts], pieces=len(parts), aabb=aabb(parts, c), fragments=[dict(mesh=f["mesh"], M=f["M"]) for f in fr], scenarios={})
            mrb = dict(parts=[dict(id=n, type=p["type"], cell=list(p["cell"]), yaw=p["yaw"], mount=list(p["mount"]) if p["mount"] else None) for n, p in enumerate(parts)], centre=list(c), fragments=fr, scenarios={})
            if name == "testbed":
                idx = {p["cell"]: n for n, p in enumerate(parts)}
                for sn, title, cells in SCEN:
                    gone = RH.dependents(parts, {idx[tuple(q)] for q in cells}); alive = sorted(set(range(len(parts))) - gone); f2 = RH.fragments(parts, s, alive); e2 = RH.check(parts, f2, alive)
                    rb["scenarios"][sn] = dict(alive=alive, fragments=[dict(mesh=f["mesh"], M=f["M"]) for f in f2]); mrb["scenarios"][sn] = dict(title=title, removed=sorted(gone), alive=alive, fragments=f2)
                    k0 = {(f["kind"], tuple(f["cell"]), f.get("face", f.get("axis"))): f["mesh"] for f in fr}; k1 = {(f["kind"], tuple(f["cell"]), f.get("face", f.get("axis"))): f["mesh"] for f in f2}
                    report.setdefault(sn, {})[s] = dict(removed_parts=len(gone), fragments_before=len(fr), fragments_after=len(f2), fragments_removed=len(set(k0) - set(k1)), tiles_reshaped=sum(1 for k in k1 if k in k0 and k0[k] != k1[k]), fragments_of_removed_parts=sum(1 for e in e2 if e[0].startswith("fragment")),
                                                        open_edges=sum(1 for e in e2 if e[0].startswith("open")), every_fragment_has_live_owner=all(f["owner"] in alive for f in f2), PASS=not e2)
            elif not name.startswith("models"):
                pgen.PS = QS; m = K.build(lambda: QD.hull_layer(parts, s), s, False, None, "hull", False, None, True); rb["legacy_hull"] = pgen.pack(m, s, (0, 0, 0), [m["V"].min(0).tolist(), m["V"].max(0).tolist()]); rb["legacy_hull_triangles"] = K.stats(m)["triangles"]; pgen.PS = RS
            if not name.startswith("models"): RH.validate(parts)
            out["robots"][name] = rb; man["blueprints"][name] = mrb; print(s, name, len(parts), "parts;", len(fr), "fragments", flush=True)
        out["robots"]["trapstand"] = dict(parts=[dict(type="Trap", M=Rz(0, 0, 0, 0))], pieces=1, aabb=[[-0.3, -0.3, 0], [0.3, 0.3, 0.3]], fragments=[], scenarios={})
        json.dump(out, open(os.path.join(d, ST.LABEL[s] + ".json"), "w"), separators=(",", ":")); json.dump(man, open(os.path.join(hd, "manifest_%s.json" % ST.LABEL[s]), "w"), separators=(",", ":"))
    json.dump(dict(test="hull layer destruction (generator side)", pass_conditions=["no fragment is owned by or requires a removed part", "every plate edge that faced a removed cell is closed (mask bit cleared) - no hanging plate, no open edge", "every remaining fragment has a live owner part (hit identification)"],
                   result="PASS" if all(v["PASS"] for sc in report.values() for v in sc.values()) else "FAIL", scenarios=report), open(os.path.join(hd, "destruction_test.json"), "w"), indent=1)
    print("destruction test:", {k: all(v["PASS"] for v in sc.values()) for k, sc in report.items()})
if __name__ == "__main__":
    a = sys.argv[1:]; styles = [x for x in a if set(x) <= set("ABCDE")]; styles = list(styles[0]) if styles else ST.STYLES; steps = [x for x in a if x in ("variants", "kit", "scene")] or ["variants", "kit", "scene"]
    if "variants" in steps: variants(styles)
    if "kit" in steps: kit(styles)
    if "scene" in steps: scene(styles)
