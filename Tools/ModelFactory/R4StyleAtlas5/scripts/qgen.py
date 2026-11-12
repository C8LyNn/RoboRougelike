"""#1239 Visual Polish Pass 1, step 1.
 variants: polish1/mesh_source/<Style>/<Id>.json for the polished types (same builder contract as pilot 2, plus the vertex-shading recipe)
 scene:    polish1/scene_source/<Style>.json - the identical test robots (part placements) with the generated cosmetic hull layer of that style;
           polish1/scene_source/shots.json - stations, cameras and the neutral test arena shared by all five styles
usage: python3 qgen.py [variants] [scene] [styles]"""
import json, os, sys, math, numpy as np
import akit as K, astyle as ST, pdefs as PD, qstyle as QS, qdefs as Q, pgen
ROOT = os.path.join(K.PKG, "polish1"); pgen.PS = QS; pgen.OUT = os.path.join(ROOT, "mesh_source"); P0 = PD.P
NOTE = dict(Cube="rdzeń konstrukcyjny w kolorze podbudowy: płaskie ściany do krawędzi, bez własnej ramy i paneli (pancerz daje warstwa kadłuba)",
            Gun="płyta mocowania jako rdzeń w kolorze podbudowy (bez ramy); ", Wheel="płyta mocowania jako rdzeń w kolorze podbudowy (bez ramy); ", Hover="płyta mocowania jako rdzeń w kolorze podbudowy (bez ramy); ", Strut="klocki węzłowe jako rdzenie w kolorze podbudowy (bez ram i paneli); ")
def variants(styles):
    PD.P = Q.P; con = {t: dict(v) for t, v in PD.CONCEPT.items()}
    for t in Q.POLISHED:
        for s in styles: PD.CONCEPT[t][s] = NOTE[t] + ("" if t == "Cube" else ("dwie szyny akceleratora ze świecącym rdzeniem, obejmy" if (t, s) == ("Gun", "A") else con[t][s]))
    for t in Q.POLISHED:
        row = []
        for s in styles:
            r = pgen.gen(t, s); p = os.path.join(pgen.OUT, ST.LABEL[s], PD.CID[t] + ".json"); src = json.load(open(p)); src["vcol"] = QS.VCOL[s]; src["polish_pass"] = 1; json.dump(src, open(p, "w"), separators=(",", ":")); row.append(r)
        print(t, PD.CID[t], row, flush=True)
    PD.P = P0; PD.CONCEPT.update(con)
def Rz(yaw, x, y, z): a = math.radians(yaw); c, s = math.cos(a), math.sin(a); return [[c, -s, 0, x], [s, c, 0, y], [0, 0, 1, z], [0, 0, 0, 1]]
def shots():
    hero = np.array([0.62, -0.74, 0.40]); hero /= np.linalg.norm(hero)
    def cam(t, dist, d=hero, lens=50): t = np.array(t, float); return dict(pos=(t + np.array(d)*dist).round(3).tolist(), target=t.tolist(), lens=lens)
    S = []
    for mode in ("before", "after"):
        S.append(dict(name="1_parts_" + mode, sets=[dict(robot="parts", mode=mode, M=Rz(0, 0, 0, 0.90))], cam=cam((0, 0, 0.75), 9.8, np.array([0.30, -0.86, 0.40])/np.linalg.norm([0.30, -0.86, 0.40])),
                      stands=[[(i - 5)*K.G, 0.0, 0.32, 0.5, 0.5, 0.64] for i in (0, 3, 6)]))
        S.append(dict(name="2_wheeled_" + mode, sets=[dict(robot="wheeled", mode=mode, M=Rz(0, 0, 0, 0.39))], cam=cam((0, -0.1, 0.65), 10.0)))
        S.append(dict(name="3_hover_" + mode, sets=[dict(robot="hover", mode=mode, M=Rz(0, 0, 0, 1.12))], cam=cam((0, 0, 0.95), 10.5)))
        S.append(dict(name="6_gameplay_" + mode, sets=[dict(robot="wheeled", mode=mode, M=Rz(0, 0, 0, 0.39)), dict(robot="hover", mode=mode, M=Rz(150, -5.5, -9.5, 1.12)), dict(robot="wheeled", mode=mode, M=Rz(-155, 6.5, -13.5, 0.39)),
                                                       dict(robot="wheeled", mode=mode, M=Rz(170, -9.5, -19.0, 0.39)), dict(robot="r100", mode=mode, M=Rz(180, 2.5, -27.0, 0.39))],
                      cam=dict(pos=[0.0, 11.5, 9.5], target=[0.0, -5.5, 0.4], lens=32)))
    S.append(dict(name="4_robot100_after", sets=[dict(robot="r100", mode="after", M=Rz(0, 0, 0, 0.39))], cam=cam((0, -0.2, 0.9), 16.5)))
    S.append(dict(name="5_robot200_after", sets=[dict(robot="r200", mode="after", M=Rz(0, 0, 0, 0.39))], cam=cam((0, -0.2, 1.1), 22.0)))
    env = [  # neutral arena: [cx, cy, cz, sx, sy, sz, yaw, kind]
        [0, -36, 2.0, 60, 1.0, 4.0, 0, "concrete"], [-30, -12, 2.0, 1.0, 50, 4.0, 0, "concrete"], [30, -12, 2.0, 1.0, 50, 4.0, 0, "concrete"],
        [-9, -6, 1.5, 1.0, 1.0, 3.0, 0, "concrete"], [9.5, -8, 1.5, 1.0, 1.0, 3.0, 0, "concrete"], [-13, -15, 1.5, 1.2, 1.2, 3.0, 0, "concrete"], [12.5, -21, 1.5, 1.2, 1.2, 3.0, 0, "concrete"],
        [8.0, -3.5, 0.55, 1.1, 1.1, 1.1, 12, "crate"], [9.3, -3.9, 0.55, 1.1, 1.1, 1.1, -8, "crate"], [8.5, -3.7, 1.65, 1.1, 1.1, 1.1, 25, "crate"], [-7.0, -12.5, 0.55, 1.1, 1.1, 1.1, 30, "crate"], [-3.0, -17.0, 0.55, 1.1, 1.1, 1.1, 5, "crate"],
        [3.0, -18.5, 0.6, 5.0, 0.5, 1.2, 18, "barrier"], [-14.0, -7.5, 0.6, 0.5, 6.0, 1.2, 0, "barrier"], [15.0, -14.0, 0.6, 0.5, 7.0, 1.2, 10, "barrier"],
        [-17, -26, 1.75, 5.0, 5.0, 3.5, 0, "concrete"], [17, -29, 2.5, 7.0, 4.0, 5.0, 0, "concrete"], [-6, -31, 1.0, 8.0, 2.0, 2.0, 0, "concrete"], [-12.0, 2.0, 0.55, 1.1, 1.1, 1.1, 40, "crate"], [13.0, 3.0, 1.5, 1.0, 1.0, 3.0, 0, "concrete"]]
    return dict(shots=S, env=env, sun=dict(dir=[0.72, 0.22, 0.66], strength=5.5, color=[1.0, 0.94, 0.84], angle_deg=2.5), sky=dict(color=[0.62, 0.72, 0.86], strength=1.35), resolutions=[[1920, 1080], [1280, 720]])
def scene(styles):
    d = os.path.join(ROOT, "scene_source"); os.makedirs(d, exist_ok=True); json.dump(shots(), open(os.path.join(d, "shots.json"), "w"), indent=1)
    for s in styles:
        out = dict(style=s, materials={z: dict(rgb=list(v[0]), metallic=v[1], roughness=v[2]) for z, v in QS.MAT[s].items()}, glow=[0.2, 0.8, 1.0], glow_strength=QS.GLOW_STRENGTH[s], textures=QS.TEXTURES[s], robots={})
        for name, fn in Q.ROBOTS.items():
            parts = fn(); c = Q.centre(parts); m = K.build(lambda: Q.hull_layer(parts, s), s, False, None, "hull", False, None, True); st = K.stats(m); assert st["ok"], (s, name)
            env = [m["V"].min(0).tolist(), m["V"].max(0).tolist()]
            out["robots"][name] = dict(parts=[dict(type=p["type"], M=Q.matrix(p, c)) for p in parts], pieces=len(parts), hull_layer=pgen.pack(m, s, (0, 0, 0), env), hull_layer_triangles=st["triangles"], hull_layer_pieces=len(m["part_stats"]))
            print(s, name, len(parts), "parts; hull layer", st["triangles"], "tris", len(m["part_stats"]), "pieces", flush=True)
        json.dump(out, open(os.path.join(d, ST.LABEL[s] + ".json"), "w"), separators=(",", ":"))
if __name__ == "__main__":
    a = sys.argv[1:]; styles = [x for x in a if set(x) <= set("ABCDE")]; styles = list(styles[0]) if styles else ST.STYLES
    if "variants" in a or not [x for x in a if x in ("variants", "scene")]: variants(styles)
    if "scene" in a or not [x for x in a if x in ("variants", "scene")]: scene(styles)
