"""#1239 Hero Benchmark Pass, step 1 (outputs under hero1/; generated sources are git-ignored). Same constructions as Visual Polish Pass 2 (rdefs),
re-targeted to the benchmark palettes of hstyle and built with REAL bevelled edges (src["bevel"], applied by pbuild in Blender), clear coat for Sleek
and one signature light colour for Sleek / Toon. Also: hull kit in the hero palette, the hero blueprint with its owned fragments, part stages, shots.
usage: python3 hgen.py [variants] [kit] [scene] [manifest] [styles]"""
import json, os, sys, math, numpy as np
import akit as K, astyle as ST, pdefs as PD, qdefs as QD, rstyle as RS, hstyle as HS, rdefs as R, rhull as RH, hhull as HH, rgen, pgen
ROOT = os.path.join(K.PKG, "hero1"); TYPES = ["Cube", "Strut", "Gun", "Wheel", "Battery", "Deployer"]
def variants(styles):
    rgen.RS = HS; rgen.ROOT = ROOT; R.RS = HS; order = R.ORDER; R.ORDER = TYPES
    try: rgen.variants(styles)
    finally: R.ORDER = order; R.RS = RS; rgen.RS = RS
    for t in TYPES:
        for s in styles:
            p = os.path.join(ROOT, "mesh_source", ST.LABEL[s], PD.CID[t] + ".json"); src = json.load(open(p)); src["bevel"] = HS.BEVEL[s]; src["vcol"] = HS.VCOL[s]; src["polish_pass"] = "hero1"
            for z, v in HS.MAT[s].items():
                if len(v) > 3: src["materials"][z]["coat"] = v[3]
            if HS.GLOW_RGB[s]: src["glow"] = list(HS.GLOW_RGB[s])
            json.dump(src, open(p, "w"), separators=(",", ":"))
def mats(S, s, glow=None): return dict(style=s, materials={z: dict(rgb=list(v[0]), metallic=v[1], roughness=v[2], **({"coat": v[3]} if len(v) > 3 else {})) for z, v in S.MAT[s].items()}, glow=list(glow or (0.2, 0.8, 1.0)), glow_strength=S.GLOW_STRENGTH[s], textures=S.TEXTURES[s])
def kit(styles):
    d = os.path.join(ROOT, "kit_source"); os.makedirs(d, exist_ok=True); pgen.PS = HS
    for s in styles:
        out = mats(HS, s, HS.GLOW_RGB[s]); out["meshes"] = {}
        for name, fn in list(RH.kit(s).items()) + list(HH.kit(s).items()):
            m = K.build(fn, s, False, None, "kit", False, None, True); st = K.stats(m); assert st["ok"], (s, name); out["meshes"][name] = dict(pgen.pack(m, s, (0, 0, 0), [[-0.3, -0.3, 0.2], [0.3, 0.3, 0.3]]), triangles=st["triangles"])
        json.dump(out, open(os.path.join(d, ST.LABEL[s] + ".json"), "w"), separators=(",", ":")); print(s, "kit", len(out["meshes"]), flush=True)
    pgen.PS = RS
Rz = rgen.Rz
STAGES = dict(st_gun=lambda: [RH._p("Cube", 0, 0, 0), RH._p("Gun", 0, 0, 1, 0, (0, 0, -1))], st_wheel=lambda: [RH._p("Cube", 0, 0, 0), RH._p("Wheel", 1, 0, 0, 0, (-1, 0, 0))], st_energy=lambda: [RH._p("Cube", 0, 0, 0), RH._p("Battery", 0, 0, 1, 0, (0, 0, -1))])
def shots():
    def cam(t, d, dist, lens=50, dof=None): t = np.array(t, float); d = np.array(d, float)/np.linalg.norm(d); return dict(pos=(t + d*dist).round(3).tolist(), target=t.tolist(), lens=lens, **({"dof": dof} if dof else {}))
    S = []; one = [[1920, 1080]]; parts = HH.striker(); c = QD.centre(parts); HM = Rz(0, 0, 0.3, 0.39); W = lambda cell, o=(0, 0, 0): (np.array(QD.world(cell, c)) + (0, 0.3, 0.39) + np.array(o)).tolist()
    for mode in ("before", "after"):
        S.append(dict(name="1_hero_" + mode, res=one, sets=[dict(robot="hero", mode=mode, M=HM)], cam=cam((0, 0.15, 0.62), (0.62, -0.74, 0.34), 10.5)))
        S.append(dict(name="1b_hero_side_" + mode, res=one, sets=[dict(robot="hero", mode=mode, M=HM)], cam=cam((0, 0.15, 0.62), (1.0, -0.10, 0.10), 11.5)))
        S.append(dict(name="1c_hero_low_" + mode, res=one, sets=[dict(robot="hero", mode=mode, M=HM)], cam=cam((0, 0.0, 0.66), (-0.52, -0.84, 0.13), 8.6, 60, 8.6)))
        S.append(dict(name="2_weapon_" + mode, res=one, sets=[dict(robot="st_gun", mode=mode, M=Rz(0, 0, 0, 0.26))], cam=cam((0, -0.12, 0.62), (0.55, -0.75, 0.36), 2.9, 60, 2.9)))
        S.append(dict(name="3_drive_" + mode, res=one, sets=[dict(robot="st_wheel", mode=mode, M=Rz(0, 0, 0, 0.39))], cam=cam((0.30, 0, 0.40), (0.78, -0.55, 0.28), 3.0, 60, 3.0)))
        S.append(dict(name="4_energy_" + mode, res=one, sets=[dict(robot="st_energy", mode=mode, M=Rz(0, 0, 0, 0.26))], cam=cam((0, 0, 0.60), (0.50, -0.78, 0.38), 2.7, 60, 2.7)))
        S.append(dict(name="7_distance_" + mode, crops=True, sets=[dict(robot="hero", mode=mode, M=Rz(25, 2.5, -6.5, 0.39), tag="hero_20m"), dict(robot="hero", mode=mode, M=Rz(150, -7.0, -22.0, 0.39), tag="hero_36m"), dict(robot="hero", mode=mode, M=Rz(200, 6.0, -46.0, 0.39), tag="hero_60m")],
                      cam=dict(pos=[0.0, 11.5, 9.5], target=[0.0, -20.0, 0.5], lens=32)))
    S.append(dict(name="5_material_hull_after", res=one, sets=[dict(robot="hero", mode="after", M=HM)], cam=cam(W((2, 2, 1), (0.1, 0, -0.1)), (0.78, -0.52, 0.36), 1.9, 70, 1.9)))
    S.append(dict(name="5b_material_prow_after", res=one, sets=[dict(robot="hero", mode="after", M=HM)], cam=cam(W((1, 5, 0), (0.1, -0.1, 0.05)), (0.55, -0.78, 0.32), 2.0, 70, 2.0)))
    S.append(dict(name="6_material_mech_after", res=one, sets=[dict(robot="hero", mode="after", M=HM)], cam=cam(W((1, 4, 1), (0, -0.05, 0.02)), (0.60, -0.70, 0.40), 1.6, 70, 1.6)))
    env = [e for e in rgen.shots()["env"]]
    return dict(shots=S, env=env, resolutions=[[1920, 1080], [1280, 720]], sun=dict(dir=[0.70, -0.25, 0.62], strength=5.2, color=[1.0, 0.93, 0.82], angle_deg=2.5), rim=dict(dir=[-0.55, 0.65, 0.45], strength=2.2, color=[0.70, 0.82, 1.0]), sky=dict(color=[0.62, 0.72, 0.86], strength=1.2), exposure=0.35, toon=HS.TOON)
def scene(styles):
    d = os.path.join(ROOT, "scene_source"); os.makedirs(d, exist_ok=True); json.dump(shots(), open(os.path.join(ROOT, "shots.json"), "w"), indent=1); BP = dict(STAGES, hero=HH.striker)
    for s in styles:
        out = dict(mats(HS, s, HS.GLOW_RGB[s]), legacy=mats(RS, s), robots={})
        for name, fn in BP.items():
            parts = fn(); c = QD.centre(parts); fr = HH.fragments(parts, s) if name == "hero" else RH.fragments(parts, s); assert RH.validate(parts) and not RH.check(parts, fr, range(len(parts)))
            out["robots"][name] = dict(parts=[dict(type=p["type"], M=QD.matrix(p, c)) for p in parts], pieces=len(parts), aabb=rgen.aabb(parts, c), fragments=[dict(mesh=f["mesh"], M=f["M"], **({"hero": True} if f.get("hero") else {})) for f in fr], scenarios={})
        json.dump(out, open(os.path.join(d, ST.LABEL[s] + ".json"), "w"), separators=(",", ":")); print(s, "scene", {k: len(v["fragments"]) for k, v in out["robots"].items()}, flush=True)
def manifest(styles):
    """owned-fragment manifest of the hero blueprint (same schema as polish2/hull_layer) + destruction test of the silhouette fairings"""
    d = os.path.join(ROOT, "hull_layer"); os.makedirs(d, exist_ok=True); parts = HH.striker(); idx = {p["cell"]: n for n, p in enumerate(parts)}; test = {}
    SC = [("F0_intact", []), ("F1_lower_cube_under_ramp", [(0, 2, 0)]), ("F2_upper_cube_behind_ramp", [(0, 1, 1)]), ("F3_prow_cube", [(1, 5, 0)]), ("F4_cheek_neighbour", [(0, 4, 0)]), ("F5_spine", [(1, 2, 1)]), ("F6_front_section", [(0, 4, 0), (1, 4, 0), (2, 4, 0)])]
    for s in styles:
        fr = HH.fragments(parts, s); res = []
        json.dump(dict(style=s, blueprint="hero_striker", cell_m=K.G, parts=[dict(index=n, type=p["type"], cell=list(p["cell"]), yaw=p["yaw"], mount=list(p["mount"]) if p["mount"] else None) for n, p in enumerate(parts)],
                       fragments=[dict(kind=f["kind"], mesh=f["mesh"], owner=f["owner"], requires=f["requires"], cell=f["cell"], **({k: f[k] for k in ("group", "mask", "face") if k in f}), **({"fairing": True} if f.get("hero") else {}), M=f["M"]) for f in fr]),
                  open(os.path.join(d, "hero_striker_%s.json" % ST.LABEL[s]), "w"), separators=(",", ":"))
        for name, cells in SC:
            gone = RH.dependents(parts, [idx[c] for c in cells]); alive = [n for n in range(len(parts)) if n not in gone]; f2 = HH.fragments(parts, s, alive); errs = RH.check(parts, f2, alive)
            left = [f for f in f2 if f.get("hero")]; bad = [f for f in left if any(r in gone for r in f["requires"])]
            res.append(dict(scenario=name, removed_cells=[list(c) for c in cells], parts_gone=len(gone), fragments=len(f2), fairings_left=len(left), fairings_of_removed_parts=len(bad), errors=len(errs), result="PASS" if not errs and not bad else "FAIL"))
        test[s] = res; print(s, "manifest", len(fr), [(r["scenario"][:2], r["fairings_left"], r["result"]) for r in res], flush=True)
    json.dump(dict(note="generator-side test (numpy), not a Unity test", scenarios=test), open(os.path.join(d, "fairing_destruction_test.json"), "w"), indent=1)
if __name__ == "__main__":
    a = sys.argv[1:]; styles = [x for x in a if set(x) <= set("ABCDE")]; styles = list(styles[0]) if styles else ST.STYLES; steps = [x for x in a if x in ("variants", "kit", "scene", "manifest")] or ["variants", "kit", "scene", "manifest"]
    if "variants" in steps: variants(styles)
    if "kit" in steps: kit(styles)
    if "scene" in steps: scene(styles)
    if "manifest" in steps: manifest(styles)
