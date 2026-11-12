"""#1239 Hero Benchmark Pass 2: scene sources for the five representative robots and the part stages (cloud side, numpy only).
The robots are the hero blueprint of the previous pass (32 gameplay parts) with the new Basic Gun, Wheel and Battery; cube, deployer and the owned
hull layer are kept. BEFORE = the same blueprint entirely from Hero Benchmark Pass 1. usage: python3 h2gen.py"""
import os, json, copy
import numpy as np
import astyle as ST, hgen, h2pal
PKG = os.path.dirname(os.path.dirname(os.path.abspath(__file__))); H1 = os.path.join(PKG, "hero1"); H2 = os.path.join(PKG, "hero2")
ZP = (("base", "R4P_Base"), ("sec", "R4P_Secondary"), ("dark", "R4P_Trim"), ("metal", "R4P_Metal"), ("rust", "R4P_Wear"))
GLOW_STRENGTH = {"A": 3.0, "B": 2.5, "C": 2.0, "D": 2.5, "E": 2.5}


def scene():
    d = os.path.join(H2, "scene_source"); os.makedirs(d, exist_ok=True)
    for s in ST.STYLES:
        a = json.load(open(os.path.join(H1, "scene_source", ST.LABEL[s] + ".json"))); pal = h2pal.PAL[s]
        a["legacy"] = {k: copy.deepcopy(a[k]) for k in ("style", "materials", "glow", "glow_strength", "textures")}            # BEFORE = previous pass palette
        a["materials"] = {z: dict(rgb=list(pal[k][0]), metallic=pal[k][1], roughness=pal[k][2]) for k, z in ZP if k in pal}; a["glow"] = list(pal["glow"][0]); a["glow_strength"] = GLOW_STRENGTH[s]
        for r in a["robots"].values():
            for f in r["fragments"]: f.pop("hero", None)                                                                    # the fairings belong to both passes
        json.dump(a, open(os.path.join(d, ST.LABEL[s] + ".json"), "w"), separators=(",", ":")); print(s, "scene", {k: len(v["parts"]) for k, v in a["robots"].items()})


def shots():
    S = hgen.shots(); json.dump(S, open(os.path.join(H2, "shots.json"), "w"), indent=1)
    N = copy.deepcopy(S); keep = []
    for sh in N["shots"]:
        if sh["name"] in ("1_hero_after", "1b_hero_side_after", "2_weapon_after", "3_drive_after", "4_energy_after", "1_hero_before", "2_weapon_before", "3_drive_before", "4_energy_before"):
            sh["name"] = "n" + sh["name"]; sh["cam"].pop("dof", None); keep.append(sh)
    N["shots"] = keep; N["sun"] = dict(dir=[0.45, -0.55, 0.70], strength=3.2, color=[1.0, 1.0, 1.0], angle_deg=8.0); N["rim"] = dict(dir=[-0.5, 0.6, 0.5], strength=0.0, color=[1, 1, 1]); N["sky"] = dict(color=[0.80, 0.80, 0.80], strength=1.0); N["toon"] = {}
    N["note"] = "neutral technical lighting: one white sun, grey sky, no rim light, no depth of field, no toon shader"
    json.dump(N, open(os.path.join(H2, "shots_neutral.json"), "w"), indent=1); print("shots", len(S["shots"]), "neutral", len(keep))


if __name__ == "__main__": scene(); shots()
