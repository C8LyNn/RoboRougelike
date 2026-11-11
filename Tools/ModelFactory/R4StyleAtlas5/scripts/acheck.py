"""generator-side check of asset x style: shells, envelope (FX nodes excluded), declared contacts. usage: python3 acheck.py [ids|pilot|all]"""
import sys, numpy as np
import akit as K, astyle as ST, adefs as AD, raycast
def tris_of(m):
    return np.array([[m["V"][f["v"][0]], m["V"][f["v"][i]], m["V"][f["v"][i + 1]]] for f in m["faces"] for i in range(1, len(f["v"]) - 1)])
def ports_of(a, m):
    out = []
    for p in (a.get("ports") or m["ports"]):
        if "position_m" in p: continue
        out.append(p)
    return out
def check(cid, verbose=True):
    a = AD.ASSETS[cid]; env = np.array(a["env"]); ok_all = True; row = []
    for s in ST.STYLES:
        for lod in (False, True):
            try: m = K.build(a["fn"], s, lod, a["env"], cid, bool(a.get("source_clips")))
            except Exception as ex: print(cid, s, lod, "BUILD ERROR", repr(ex)[:150]); ok_all = False; continue
            st = K.stats(m); fx = set(a.get("fx", [])); V = np.concatenate([mm["V"] for n, mm in m["by_node"].items() if mm is not None and n not in fx])
            ins = bool((V.min(0) >= env[0] - 5e-4).all() and (V.max(0) <= env[1] + 5e-4).all()); T = tris_of(m); bad = []
            for p in ports_of(a, m):
                r = raycast.patch_test(T, p["cell"], p["dir"], p["offset"], p["half"]); need = 80 if p["kind"] == "full" else 65
                if not (r["protruding"] == 0 and r["missing"] == 0 and r["flush_pct"] >= need): bad.append((p["dir"], p["cell"], r["flush_pct"], r["protruding"], r["missing"]))
            good = st["ok"] and ins and not bad; ok_all &= good
            if not lod: row.append(st["triangles"])
            if not good and verbose: print(f"{cid:28s} {s} {'LOD1' if lod else 'LOD0'} topo={st['ok']} bad_shells={st['bad_shells'][:4]} inside={ins} {V.min(0).round(3)} {V.max(0).round(3)} ports={bad[:3]}")
    return ok_all, row
if __name__ == "__main__":
    arg = sys.argv[1] if len(sys.argv) > 1 else "pilot"; ids = AD.PHASE[arg] if arg in AD.PHASE else (list(AD.ASSETS) if arg == "all" else arg.split(","))
    res = {}
    for c in ids: res[c], row = check(c); print(f"{c:30s} {'OK  ' if res[c] else 'FAIL'} tris A-E {row}")
    print(f"{sum(res.values())}/{len(res)} assets OK in all five styles")
