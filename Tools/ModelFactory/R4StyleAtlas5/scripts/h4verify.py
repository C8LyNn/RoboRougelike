"""Hero Benchmark Pass 4: do the committed recipes reproduce the delivered models?
Rebuilds the high-poly source and the game mesh of every proposal from h4defs.py (no bake) and compares triangle counts per node and node bounds with
the report written when the delivered model was built.
usage: blender -b --factory-startup --python h4verify.py -- OUT_JSON REPORT_P REPORT_Q"""
import bpy, sys, os, json, numpy as np
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import hf, h4defs as D
a = sys.argv[sys.argv.index("--") + 1:]; OUT, RP, RQ = a[0], a[1], a[2]
LABEL = dict(A="A_Sleek", B="B_Brutal", C="C_Arcade", D="D_Scrap", E="E_Toon"); CID = dict(Gun="R4W_Basic", Wheel="R4M_WheelM_Side_Offroad", Battery="R4X_Battery")
def bbox(o): V = np.array([o.matrix_world @ v.co for v in o.data.vertices]); return [V.min(0).round(4).tolist(), V.max(0).round(4).tolist()]
res = {}
for (typ, s, set_) in sorted(D.RECIPES):
    bpy.ops.wm.read_factory_settings(use_empty=True); col = bpy.context.scene.collection; fn = D.RECIPES[(typ, s, set_)]
    Bh = hf.Builder(True, s, col); fn(Bh); hi = hf.finish(Bh); Bl = hf.Builder(False, s, col); fn(Bl); lo = hf.finish(Bl)
    now = dict(tris_hi={n: sum(hf.tris(o) for o in v) for n, v in hi.items()}, tris_lo={n: hf.tris(o) for n, o in lo.items()}, bounds={n: bbox(o) for n, o in lo.items()})
    rep = json.load(open(os.path.join(RQ if set_ == "q" else RP, LABEL[s] + "_" + CID[typ] + ".json")))
    dev = max([abs(x - y) for n in now["bounds"] if n in rep["bounds"] for i in (0, 1) for x, y in zip(now["bounds"][n][i], rep["bounds"][n][i])] or [9])
    ok = now["tris_hi"] == rep["tris_hi"] and now["tris_lo"] == rep["tris_lo"] and set(now["bounds"]) == set(rep["bounds"]) and dev < 2e-4
    res["%s %s %s" % (LABEL[s], typ, set_.upper())] = dict(same=bool(ok), tris_lo=sum(now["tris_lo"].values()), tris_lo_delivered=sum(rep["tris_lo"].values()), tris_hi=sum(now["tris_hi"].values()), tris_hi_delivered=sum(rep["tris_hi"].values()), bounds_dev_mm=round(dev*1000, 3))
    print("[HERO4] VERIFY", typ, s, set_, res["%s %s %s" % (LABEL[s], typ, set_.upper())], flush=True)
json.dump(dict(what="recipes rebuilt without baking and compared with the build report of the delivered model", all_same=all(r["same"] for r in res.values()), models=res), open(OUT, "w"), indent=1)
print("[HERO4] VERIFY_DONE", sum(r["same"] for r in res.values()), "/", len(res), flush=True)
