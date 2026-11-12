"""#1239 Hero Benchmark Pass 3 (owner artistic selection): the per-style choice of historical models and the shots of the OLD / NEW / HYBRID
comparison (cloud side, plain Python). Nothing is modelled or regenerated in this pass: a HYBRID robot is the hero blueprint of the two previous
passes with, per part type, the exported model of Hero Benchmark Pass 1 (PR #1270, "old") or Hero Benchmark Pass 2 (PR #1274, "new").
"chassis" = cube + deployer + hull kit and the palette they are painted in (their geometry is the same in both passes).
usage: python3 h3gen.py   -> hero3/shots.json (cameras, arena and light are taken unchanged from hero2/shots.json)"""
import os, json, copy
PKG = os.path.dirname(os.path.dirname(os.path.abspath(__file__))); H2 = os.path.join(PKG, "hero2"); H3 = os.path.join(PKG, "hero3")
STYLES = "ABCDE"; LABEL = dict(A="A_Sleek", B="B_Brutal", C="C_Arcade", D="D_Scrap", E="E_Toon"); TYPES = ("chassis", "Gun", "Wheel", "Battery")
# owner scores, old (-2) .. new (+2); 0 = comparable, to be decided on the whole robot
SCORES = {"A": dict(chassis=+1, Gun=-1, Wheel=+2, Battery=-2), "B": dict(chassis=0, Gun=+1, Wheel=0, Battery=-1), "C": dict(chassis=+1, Gun=+1, Wheel=+2, Battery=0),
          "D": dict(chassis=-1, Gun=-1, Wheel=+1, Battery=-1), "E": dict(chassis=+2, Gun=+2, Wheel=+2, Battery=+2)}
# the three elements scored 0, decided after the variants were rendered on the whole robot (reasons: hero3/README.md)
ZERO = {"B": dict(chassis="old", Wheel="new"), "C": dict(Battery="new")}
# HYBRID = the historical models exactly as exported. HYBRID+ = a PROPOSAL shown next to it, scene-side only (see h3scene.py "harm"): Sleek mixes two
# light colours (old gun / battery: icy white, strength 6; new chassis and wheel: cyan), so the old parts take the light of the chassis.
PLUS = {"A": dict(Gun=dict(glow="chassis", glow_strength=4.0), Battery=dict(glow="chassis", glow_strength=4.0))}


def mix(s, zero=None):
    m = {t: ("new" if v > 0 else "old" if v < 0 else None) for t, v in SCORES[s].items()}
    for t, v in {**ZERO.get(s, {}), **(zero or {})}.items(): m[t] = v
    return m


def configs():
    C = {}
    for s in STYLES:
        c = {"old": dict(mix=dict.fromkeys(TYPES, "old")), "new": dict(mix=dict.fromkeys(TYPES, "new"))}
        if s == "B":
            for r in ("old", "new"):
                for w in ("old", "new"): c["b_r%s_w%s" % (r[0], w[0])] = dict(mix=mix(s, dict(chassis=r, Wheel=w)))
        if s == "C":
            for b in ("old", "new"): c["c_bat%s" % b[0]] = dict(mix=mix(s, dict(Battery=b)))
        m = mix(s); assert all(m.values()), (s, m); c["hybrid"] = dict(mix=m)
        if s in PLUS: c["hybrid_plus"] = dict(mix=m, harm=PLUS[s])
        C[s] = c
    return C


def shots():
    S2 = json.load(open(os.path.join(H2, "shots.json"))); by = {sh["name"]: sh for sh in S2["shots"]}; one = [[1920, 1080]]; HM = by["1_hero_after"]["sets"][0]["M"]; C = configs(); S = []
    def cam(t, d, dist, lens=50, dof=None):
        n = sum(x*x for x in d)**0.5; return dict(pos=[round(t[i] + d[i]/n*dist, 3) for i in range(3)], target=list(t), lens=lens, **({"dof": dof} if dof else {}))
    def w(p, o=(0, 0, 0)): return [p[0] + o[0], p[1] + 0.3 + o[1], p[2] + 0.39 + o[2]]                       # blueprint coordinates -> arena (the robot stands at HM)
    VIEW = dict(hero=by["1_hero_after"]["cam"], side=by["1b_hero_side_after"]["cam"], low=by["1c_hero_low_after"]["cam"],
                top=cam(w((0, 0.86, 1.1), (0, 0, 0.10)), (0.50, -0.80, 0.42), 3.6, 60, 3.6),                     # roof group: gun between the two batteries, deployer behind
                fgun=cam(w((0, -0.825, 0.55), (0, 0, 0.12)), (0.62, -0.70, 0.38), 2.6, 60, 2.6),                 # front gun on the lower deck
                wheel=cam(w((1.1, -0.825, 0.0), (0.05, 0, 0.02)), (0.86, -0.46, 0.22), 2.9, 60, 2.9))            # front right wheel with the hull behind it
    def add(view, cfg, styles):
        if view == "dist": S.append(dict(name="dist_" + cfg, crops=True, res=one, styles=styles, sets=[dict(st, cfg=cfg) for st in by["7_distance_after"]["sets"]], cam=by["7_distance_after"]["cam"]))
        elif view.startswith("clay_"): S.append(dict(name=view + "_" + cfg, res=one, styles=styles, clay=True, sets=[dict(robot="hero", cfg=cfg, M=HM)], cam={k: v for k, v in VIEW[view[5:]].items() if k != "dof"}))
        else: S.append(dict(name=view + "_" + cfg, res=one, styles=styles, sets=[dict(robot="hero", cfg=cfg, M=HM)], cam=VIEW[view]))
    names = sorted({k for s in STYLES for k in C[s]})
    for cfg in names:
        st = "".join(s for s in STYLES if cfg in C[s]); full = cfg in ("old", "new", "hybrid")
        for v in (("hero", "low", "side", "top", "fgun", "wheel", "dist", "clay_hero", "clay_top") if full else ("hero", "top", "fgun") if cfg == "hybrid_plus" else ("hero", "low", "top", "wheel", "dist")): add(v, cfg, st)
    for sh in S: sh["sets"] = [{k: v for k, v in st.items() if k != "mode"} for st in sh["sets"]]
    out = dict(S2); out["shots"] = S; out["configs"] = C; out["resolutions"] = one
    out["note"] = "Hero Benchmark Pass 3: arena, light, exposure and toon look-dev are those of hero2/shots.json; configs = per-type choice of the historical models; clay = one grey material on everything (real geometry only, no normal maps)"
    out["scores"] = SCORES; out["zero_decisions"] = ZERO
    os.makedirs(H3, exist_ok=True); json.dump(out, open(os.path.join(H3, "shots.json"), "w"), indent=1); print("shots", len(S), {s: sorted(C[s]) for s in STYLES})


if __name__ == "__main__": shots()
