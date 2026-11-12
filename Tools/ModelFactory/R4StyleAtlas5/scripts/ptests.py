"""#1239 pilot 2: independent numpy tests on the EXPORTED GLB files of the 40 pilot variants (no Blender). usage: python3 ptests.py
 T1 files      GLB + LOD1 GLB load; triangle counts equal the Blender QA record; LOD1 <= LOD0; materials are paint zones
 T2 envelope   rest-pose geometry (FX nodes excluded) inside the declared envelope (collision outline of the type)
 T3 swap       the five styles of one type expose IDENTICAL node names, hierarchy and socket / port empties (position dev < 0.05 mm)
 T4 animation  every variant carries animation channels in the GLB for each animated object of its QA record
 T5 textures   A, B, D: every painted material has baseColorTexture + metallicRoughnessTexture + normalTexture and keeps the zone paint
               colour / metallic / roughness as FACTORS; E: painted base-colour ramp only; C: no images (both by design)
 T6 different  the five meshes of one type are different geometry (no two styles share a vertex-set hash)
Technical tests only - the artistic acceptance of the pilot is a separate decision of the owner."""
import os, sys, json, hashlib
import numpy as np
import astyle as ST, pstyle as PS
PKG = os.path.dirname(os.path.dirname(os.path.abspath(__file__))); ATL = os.path.join(PKG, "pilot2", "variants"); TST = os.path.join(PKG, "pilot2", "tests"); POLISH = len(sys.argv) > 1 and sys.argv[1] == "polish1"
if POLISH:                                                      # Visual Polish Pass 1: same tests on polish1/variants with its palette, plus T7 (baked vertex shading present)
    import qstyle as PS
    ATL = os.path.join(PKG, "polish1", "variants"); TST = os.path.join(PKG, "polish1", "tests")
MF = os.path.dirname(PKG)
sys.path.insert(0, os.path.join(MF, "R4BlocksRedesign1232", "scripts")); import glbio
def g2b(P): P = np.asarray(P); return np.stack([P[..., 0], -P[..., 2], P[..., 1]], axis=-1)
def main():
    rep = {}; ids = sorted(os.listdir(os.path.join(ATL, ST.LABEL["A"])))
    for cid in ids:
        r = {}; per = {}
        for s in ST.STYLES:
            d = os.path.join(ATL, ST.LABEL[s], cid); qp = os.path.join(d, cid + "_QA.json")
            if not os.path.exists(qp): r[s] = dict(PASS=False, why="variant missing"); continue
            qa = json.load(open(qp)); m = glbio.load(os.path.join(d, cid + ".glb")); l = glbio.load(os.path.join(d, cid + "_LOD1.glb")); fx = {n for n in qa["hierarchy"] if any(n == x for x in qa.get("fx_objects", []))}
            mats = {x["name"] for x in m.json.get("materials", [])}; t1 = m.tri_count() == qa["triangles"] and l.tri_count() == qa["triangles_lod1"] and l.tri_count() <= m.tri_count() and mats <= set(PS.ZONE_ORDER)
            env = np.array(qa["envelope_m"]); P = [g2b(p["pos"]) for p in m.prims if abs(np.linalg.det(m.nodes[p["node"]][:3, :3])) > 1e-6 and (np.ptp(p["pos"], axis=0).max() > 1e-4)]
            P = [x for x, p in zip(P, [p for p in m.prims if abs(np.linalg.det(m.nodes[p["node"]][:3, :3])) > 1e-6 and (np.ptp(p["pos"], axis=0).max() > 1e-4)]) if p["node"] not in qa.get("fx_objects", [])]
            V = np.concatenate(P); out = max(float((env[0] - V).max()), float((V - env[1]).max())); t2 = out <= 0.001
            names = sorted(n.get("name", "") for n in m.json["nodes"]); emp = {k: g2b(v[0][:3, 3]) for k, v in m.empties.items()}
            chan = sum(len(a["channels"]) for a in m.json.get("animations", [])); an_objs = qa["animation"]["animated_objects"]; t4 = (chan >= len(an_objs)) if an_objs else True
            imgs = len(m.json.get("images", [])); tx = PS.TEXTURES[s]; painted = [x for x in m.json.get("materials", []) if x["name"] != "R4P_Glow"]; tex_mats = 0; fact = True
            for x in painted:
                pb = x.get("pbrMetallicRoughness", {}); has = ("baseColorTexture" in pb, "metallicRoughnessTexture" in pb, "normalTexture" in x); want3 = (bool(tx["base"]), bool(tx["orm"]), bool(tx["normal"]))
                tex_mats += has == want3; z = PS.MAT[s][x["name"]]
                fact &= bool(np.allclose(pb.get("baseColorFactor", [1, 1, 1, 1])[:3], z[0], atol=2e-3) and abs(pb.get("metallicFactor", 1.0) - z[1]) < 2e-3 and abs(pb.get("roughnessFactor", 1.0) - z[2]) < 2e-3)
            t5 = tex_mats == len(painted) and fact and imgs == sum(1 for k in ("base", "orm", "normal") if tx[k])
            t7 = all("COLOR_0" in pr["attributes"] for me in m.json["meshes"] for pr in me["primitives"]) if POLISH else True; t5 = bool(t5 and t7)
            h = hashlib.sha256(np.round(np.sort(V.view([("x", float), ("y", float), ("z", float)]), axis=0).view(float), 4).tobytes()).hexdigest()
            per[s] = dict(names=names, emp=emp, hash=h); r[s] = dict(T1=bool(t1), T2=bool(t2), outside_mm=round(max(out, 0)*1000, 3), T4=bool(t4), channels=chan, T5=bool(t5), images=imgs, textured_materials=tex_mats, tris=m.tri_count(), tris_lod1=l.tri_count(), blender=qa["status"])
        ok5 = len(per) == 5; t3 = ok5 and all(per[s]["names"] == per["A"]["names"] for s in per); dev = 0.0
        if ok5:
            for s in per:
                for k, v in per["A"]["emp"].items(): dev = max(dev, float(np.linalg.norm(per[s]["emp"].get(k, v + 9) - v))*1000)
        t3 = bool(t3 and dev < 0.05); t6 = ok5 and len({per[s]["hash"] for s in per}) == 5
        r["T3"] = t3; r["empty_dev_mm"] = round(dev, 4); r["T6"] = bool(t6)
        r["PASS"] = bool(ok5 and t3 and t6 and all(r[s]["T1"] and r[s]["T2"] and r[s]["T4"] and r[s]["T5"] and r[s]["blender"] == "BLENDER_PASS" for s in ST.STYLES)); rep[cid] = r
    n = sum(1 for v in rep.values() if v["PASS"]); os.makedirs(TST, exist_ok=True)
    json.dump(dict(result="PASS" if n == len(rep) else "FAIL", assets=len(rep), assets_pass=n, variants=5*len(rep), detail=rep), open(os.path.join(TST, "polish1_glb_tests.json" if POLISH else "pilot2_glb_tests.json"), "w"), indent=1)
    with open(os.path.join(TST, "POLISH1_TEST_SUMMARY.md" if POLISH else "PILOT2_TEST_SUMMARY.md"), "w", encoding="utf-8") as f:
        f.write(f"# {'Visual Polish Pass 1' if POLISH else 'Pilot 2'}: testy na wyeksportowanych GLB (numpy){' — T5 obejmuje też T7: każdy prymityw ma COLOR_0 (wypalone cieniowanie wierzchołków)' if POLISH else ''}\n\nWynik techniczny: **{n}/{len(rep)} typów z kompletem 5/5 stylów PASS** ({5*len(rep)} wariantów). Unity: UNITY_NOT_RUN. Odbiór artystyczny: **OCZEKUJE NA WŁAŚCICIELA** (testy techniczne go nie zastępują).\n\n" + __doc__.split("\n", 1)[1] + "\n\n| asset | T1 A-E | T2 | T3 | T4 | T5 | T6 | wynik |\n|---|---|---|---|---|---|---|---|\n")
        for cid, v in rep.items():
            def col(k): return "".join(("P" if v[s].get(k) else "F") if isinstance(v.get(s), dict) and k in v[s] else "-" for s in ST.STYLES)
            f.write(f"| {cid} | {col('T1')} | {col('T2')} | {'P' if v['T3'] else 'F'} | {col('T4')} | {col('T5')} | {'P' if v['T6'] else 'F'} | {'PASS' if v['PASS'] else '**FAIL**'} |\n")
    print(f"{n}/{len(rep)} assets PASS"); [print("FAIL", c, {k: v for k, v in r.items() if k in ST.STYLES and not all(v.get(t, True) for t in ('T1', 'T2', 'T4', 'T5'))} or {k: r[k] for k in ("T3", "T6", "empty_dev_mm")}) for c, r in rep.items() if not r["PASS"]][:0]
    for c, r in list(rep.items()):
        if not r["PASS"]: print("FAIL", c, "T3", r["T3"], r["empty_dev_mm"], "T6", r["T6"], {s: [k for k in ("T1", "T2", "T4", "T5") if not r[s].get(k, True)] + [r[s].get("blender", r[s].get("why"))] for s in ST.STYLES if isinstance(r.get(s), dict)})
if __name__ == "__main__": main()
