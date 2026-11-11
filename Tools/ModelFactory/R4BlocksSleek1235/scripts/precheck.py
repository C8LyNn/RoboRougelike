"""#1235 generator-side pre-check (numpy, no Blender): topology, approved port contact patches, containment in the approved solid.
The authoritative QA is the Blender pass (build_blender25.py) and tests_compat25.py on the exported GLB. usage: python3 precheck.py [Kind,...]"""
import sys, json, numpy as np
from scipy.spatial import ConvexHull
import paths25, shapes25, r4panel as RP, raycast

def tris_of(m):
    out = []
    for f in m["faces"]:
        P = m["V"][f["v"]]
        for i in range(1, len(P) - 1): out.append([P[0], P[i], P[i + 1]])
    return np.array(out)

def outside_mm(kind, pts):
    best = np.full(len(pts), 1e9)
    for part in shapes25.SOLID[kind]:
        eq = ConvexHull(np.array(part["hull"])).equations; d = (pts @ eq[:, :3].T + eq[:, 3]).max(1)
        if part.get("sphere"): d = np.maximum(d, np.linalg.norm(pts - np.array(part["sphere"]["center"], float), axis=1) - part["sphere"]["r"])
        for c in part.get("minus", []):
            ax = np.array(c["axis"], float); q = pts - np.array(c["point"], float); rad = np.linalg.norm(q - np.outer(q @ ax, ax), axis=1); d = np.maximum(d, c["r"] - rad)
        best = np.minimum(best, d)
    return float(best.max()*1000)

def check(kind, verbose=True):
    qa = json.load(open(paths25.ref_file("blocks", f"R4B_{kind}", f"R4B_{kind}_QA.json"))); ok = True; rows = []
    for lod in (False, True):
        m = shapes25.build(kind, lod); st = RP.mesh_stats(m); T = tris_of(m)
        S = np.concatenate([m["V"], T.mean(1), (T[:, [0, 1, 2]] + T[:, [1, 2, 0]]).reshape(-1, 3)/2])
        out = outside_mm(kind, S); pt = []
        for p in qa["ports"]:
            r = raycast.patch_test(T, p["cell"], p["dir"], p["offset"], p["half"]); need = 80 if p["kind"] == "full" else 65
            r["PASS"] = r["protruding"] == 0 and r["missing"] == 0 and r["flush_pct"] >= need; pt.append((p["dir"], p["cell"], p["kind"][0], r["flush_pct"], r["protruding"], r["missing"], r["PASS"]))
        good = st["ok"] and out <= 0.02 and all(x[-1] for x in pt); ok &= good
        rows.append((lod, st["triangles"], st["ok"], round(out, 3), [x for x in pt if not x[-1]], min((x[3] for x in pt), default=None)))
    if verbose:
        for lod, tr, tok, out, bad, mn in rows: print(f"{kind:12s} {'LOD1' if lod else 'LOD0'} tris {tr:5d} topo {tok} outside {out:6.3f} mm min flush {mn} " + ("OK" if not bad and tok and out <= 0.02 else f"FAIL {bad}"))
    return ok

if __name__ == "__main__":
    kinds = sys.argv[1].split(",") if len(sys.argv) > 1 else list(shapes25.ALL)
    res = {k: check(k) for k in kinds}; print("ALL OK" if all(res.values()) else "FAILED: " + ",".join(k for k, v in res.items() if not v))
