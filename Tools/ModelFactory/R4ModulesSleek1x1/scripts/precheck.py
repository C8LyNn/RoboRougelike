"""generator-side pre-check (numpy, no Blender): shell topology, 1x1x1 envelope, declared contact patches (approved patch_test ruler).
usage: python3 precheck.py [Name,...]"""
import sys, numpy as np
import mkit as K, modules as MD, raycast
ENV = K.H + 2e-5
def tris_of(m):
    out = []
    for f in m["faces"]:
        P = m["V"][f["v"]]
        for i in range(1, len(P) - 1): out.append([P[0], P[i], P[i + 1]])
    return np.array(out)
def check(name, verbose=True):
    ok = True; d = MD.ALL[name]; env = d.get("env", [[-ENV]*3, [ENV]*3])
    for lod in (False, True):
        m = K.build(d["fn"], lod); st = K.stats(m); T = tris_of(m); lo, hi = m["V"].min(0), m["V"].max(0)
        inside = bool((lo >= np.array(env[0]) - 1e-9).all() and (hi <= np.array(env[1]) + 1e-9).all()); bad = []
        for p in m["ports"]:
            r = raycast.patch_test(T, p["cell"], p["dir"], p["offset"], p["half"]); need = 80 if p["kind"] == "full" else 65
            if not (r["protruding"] == 0 and r["missing"] == 0 and r["flush_pct"] >= need): bad.append((p["dir"], p["kind"], r))
        good = st["ok"] and inside and not bad; ok &= good
        if verbose and not good: print(f"{name:22s} {'LOD1' if lod else 'LOD0'} topo {st['ok']} bad_shells {st['bad_shells']} inside {inside} {lo.round(4)} {hi.round(4)} ports {bad}")
    return ok
if __name__ == "__main__":
    names = sys.argv[1].split(",") if len(sys.argv) > 1 else list(MD.ALL)
    res = {k: check(k) for k in names}; print(f"{sum(res.values())}/{len(res)} OK" + ("" if all(res.values()) else " FAILED: " + ",".join(k for k, v in res.items() if not v)))
