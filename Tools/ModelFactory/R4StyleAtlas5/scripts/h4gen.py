"""#1239 Hero Benchmark Pass 4: what is compared with what, and the shots (cloud side, plain Python).
LEFT  = the direction the owner preferred in survey 1 (the exported model of Hero Pass 1 "old" or Hero Pass 2 "new")
RIGHT = the NEW Pass 4 model built from that direction ("p"); where the survey was a tie there are two pairs: old -> "p" and new -> "q".
Arena, cameras, light and exposure are taken unchanged from hero2/shots.json, so both columns and all earlier boards are comparable.
usage: python3 h4gen.py   -> hero4/shots.json"""
import os, json
import h4pal
PKG = os.path.dirname(os.path.dirname(os.path.abspath(__file__))); H2 = os.path.join(PKG, "hero2"); H4 = os.path.join(PKG, "hero4")
STYLES = "ABCDE"; LABEL = dict(A="A_Sleek", B="B_Brutal", C="C_Arcade", D="D_Scrap", E="E_Toon"); TYPES = ("chassis", "Gun", "Wheel", "Battery")
# survey 1: preferred direction per category; a tuple = tie (two proposals)
SURVEY = {"A": dict(chassis="new", Gun="old", Wheel="new", Battery="old"), "B": dict(chassis=("old", "new"), Gun="new", Wheel=("old", "new"), Battery="old"),
          "C": dict(chassis="new", Gun="new", Wheel="new", Battery=("old", "new")), "D": dict(chassis="old", Gun="old", Wheel="new", Battery="old"), "E": dict(chassis="new", Gun="new", Wheel="new", Battery="new")}
KIT = {"A": dict(p="A_Sleek"), "B": dict(p="B_Brutal_P", q="B_Brutal_Q"), "C": dict(p="C_Arcade"), "D": dict(p="D_Scrap"), "E": dict(p="E_Toon")}       # hero4/hull_kit/<dir>
KITPAL = {"A": dict(p="A"), "B": dict(p="Bo", q="B"), "C": dict(p="C"), "D": dict(p="D"), "E": dict(p="E")}                                             # palette of the hull = palette of the cube core and the deployer under it
GLOWK = dict(A=3.0, B=2.5, C=2.0, D=2.5, E=2.5)


def ties(s): return [t for t in TYPES if isinstance(SURVEY[s][t], tuple)]
def configs():
    C = {}
    for s in STYLES:
        sv = SURVEY[s]; c = {"L": dict(mix={t: (sv[t][0] if isinstance(sv[t], tuple) else sv[t]) for t in TYPES}), "R": dict(mix=dict.fromkeys(TYPES, "p"))}
        if ties(s): c["Lq"] = dict(mix={t: (sv[t][1] if isinstance(sv[t], tuple) else sv[t]) for t in TYPES}); c["Rq"] = dict(mix={t: ("q" if isinstance(sv[t], tuple) else "p") for t in TYPES})
        C[s] = c
    return C
def p4pal():
    out = {}
    for s in STYLES:
        out[s] = {}
        for k, key in KITPAL[s].items():
            P = h4pal.PAL[key]; m = lambda z: dict(rgb=list(P[z][0]), metallic=P[z][1], roughness=P[z][2])
            core = dict(rgb=[0.070, 0.028, 0.020], metallic=0.3, roughness=0.86) if key == "D" else m("dark")      # Scrap: a dark oxide core, so the uncovered cube faces stay in the one colour family
            out[s][k] = dict(materials={"R4P_Base": m("base"), "R4P_Secondary": core, "R4P_Trim": core, "R4P_Metal": m("metal"), "R4P_Wear": m("rust" if "rust" in P else "dark")}, glow=list(P["glow"][0]), glow_k=GLOWK[s], palette=key,
                             note="the cube is a structural core under the armour: its faces take the dark frame colour of the hull, as in Hero Pass 2 (Scrap: dark oxide)")
    return out


def shots():
    S2 = json.load(open(os.path.join(H2, "shots.json"))); by = {sh["name"]: sh for sh in S2["shots"]}; one = [[1920, 1080]]; HM = by["1_hero_after"]["sets"][0]["M"]; C = configs(); S = []
    stage = dict(gun=("2_weapon_after", False), wheel=("3_drive_after", False), bat=("4_energy_after", False), cube=("4_energy_after", True))
    def add(view, cfg, styles):
        if view in stage:
            sh = by[stage[view][0]]; st = dict(sh["sets"][0]); st.pop("mode", None); cam = dict(sh["cam"])
            if view == "cube": cam = dict(pos=[1.75, -2.25, 1.05], target=[0.0, 0.0, 0.30], lens=60, dof=2.9)                    # the lone cube under the part: its own plates on four sides
            S.append(dict(name=view + "_" + cfg, res=one, styles=styles, sets=[dict(st, cfg=cfg, hull=stage[view][1])], cam=cam))
        elif view == "dist": S.append(dict(name="dist_" + cfg, crops=True, res=one, styles=styles, sets=[dict({k: v for k, v in st.items() if k != "mode"}, cfg=cfg) for st in by["7_distance_after"]["sets"]], cam=by["7_distance_after"]["cam"]))
        else:
            src = dict(hero="1_hero_after", side="1b_hero_side_after", low="1c_hero_low_after", panels="5_material_hull_after", prow="5b_material_prow_after", deck="6_material_mech_after", clay="1_hero_after", clayprow="5b_material_prow_after")[view]
            cam = dict(by[src]["cam"]); clay = view.startswith("clay")
            if clay: cam.pop("dof", None)
            S.append(dict(name=view + "_" + cfg, res=one, styles=styles, sets=[dict(robot="hero", cfg=cfg, M=HM)], cam=cam, **({"clay": True} if clay else {})))
    ALL = ("hero", "low", "side", "panels", "prow", "deck", "cube", "dist", "clay", "clayprow", "gun", "wheel", "bat")
    for cfg in ("L", "R"):
        for v in ALL: add(v, cfg, STYLES)
    for cfg in ("Lq", "Rq"):
        for v in ALL:
            st = "".join(s for s in STYLES if ties(s) and (v not in ("gun", "wheel", "bat") or dict(gun="Gun", wheel="Wheel", bat="Battery")[v] in ties(s)) and (v not in ("panels", "prow", "cube", "clay", "clayprow", "side", "deck") or "chassis" in ties(s)))
            if st: add(v, cfg, st)
    out = dict(S2); out["shots"] = S; out["configs"] = C; out["resolutions"] = one; out["survey"] = {s: {t: list(v) if isinstance(v, tuple) else v for t, v in SURVEY[s].items()} for s in STYLES}; out["p4pal"] = p4pal(); out["p4kit"] = KIT
    out["note"] = "Hero Benchmark Pass 4: L = preferred direction of survey 1, R = new Pass 4 model (q = second proposal of a tie). gun / wheel / bat: the part alone on a bare cube core (no armour), same camera as the stage shots of Hero Pass 2."
    os.makedirs(H4, exist_ok=True); json.dump(out, open(os.path.join(H4, "shots.json"), "w"), indent=1); print("shots", len(S), {s: len([x for x in S if s in x["styles"]]) for s in STYLES}, {s: C[s] for s in "BC"})


if __name__ == "__main__": shots()
