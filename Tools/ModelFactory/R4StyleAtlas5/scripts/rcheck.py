"""pass 2 generator check: shells, envelope, source-node bounds (mechanism nodes are tested against the bounds of the source node they hang on),
contacts, identical SOURCE node set in the five styles. usage: python3 rcheck.py [Type,..]"""
import sys, numpy as np
import akit as K, astyle as ST, pdefs as PD, rdefs as R, raycast, acheck, pcheck
def build(typ, s, lod=False):
    t = PD.TYPES[typ]; R.MECH.clear(); m = K.build(R.P[typ][s], s, lod, t["env"], t["cid"], bool(t.get("source_clips")), t.get("planes"), True); return m, dict(R.MECH)
def top_source(m, n, nb):
    while n not in nb and n != "Body": n = m["nodes"][n].get("parent") or "Body"
    return n
if __name__ == "__main__":
    types = sys.argv[1].split(",") if len(sys.argv) > 1 else list(R.P); allok = True
    for typ in types:
        t = PD.TYPES[typ]; env = np.array(t["env"]); row = []; srcsets = []
        for s in ST.STYLES:
            for lod in (False, True):
                try: m, mech = build(typ, s, lod)
                except Exception as ex: print(typ, s, lod, "BUILD ERROR", repr(ex)[:300]); allok = False; continue
                st = K.stats(m); fx = set(t.get("fx", [])); V = np.concatenate([mm["V"] for n, mm in m["by_node"].items() if mm is not None and n not in fx]); ins = bool((V.min(0) >= env[0] - 5e-4).all() and (V.max(0) <= env[1] + 5e-4).all()); bad = []; nbad = []; T = acheck.tris_of(m)
                if t["family"] == "connector": bad += pcheck.conn_ports(t, T)
                for p in acheck.ports_of(t, m):
                    r = raycast.patch_test(T, p["cell"], p["dir"], p["offset"], p["half"])
                    if not (r["protruding"] == 0 and r["missing"] == 0 and r["flush_pct"] >= (80 if p["kind"] == "full" else 65)): bad.append((p["dir"], r["flush_pct"], r["protruding"]))
                nb = t.get("nb") or {}
                for n, mm in m["by_node"].items():
                    if mm is None or n in fx or not nb: continue
                    b = nb.get(top_source(m, n, nb))
                    if b is None: continue
                    d = max(float((np.array(b[0]) - mm["V"].min(0)).max()), float((mm["V"].max(0) - np.array(b[1])).max()))
                    if d > 0.003: nbad.append((n, round(d*1000, 1), mm["V"].min(0).round(3).tolist(), mm["V"].max(0).round(3).tolist()))
                good = st["ok"] and ins and not bad and not nbad; allok &= good
                if not lod: row.append((st["triangles"], len(mech))); srcsets.append(tuple(sorted(n for n in m["by_node"] if n not in mech and m["by_node"][n] is not None)))
                if not good: print(f"{typ} {s} {'LOD1' if lod else 'LOD0'} topo={st['ok']} bad_shells={st['bad_shells'][:5]} inside={ins} {V.min(0).round(3)} {V.max(0).round(3)} ports={bad} nodes={nbad}")
        same = len(set(srcsets)) == 1; allok &= same; print(f"{typ:9s} (tris, mechanisms) A-E {row} same_source_nodes={same}")
    print("ALL OK" if allok else "FAILURES")
def sweep(typ, s, clips):
    """generator-side copy of the builder's sweep test for procedural types: mechanism nodes moved by their own tracks (nested mechanisms follow
    their parent), worst distance outside the envelope in metres"""
    from scipy.spatial.transform import Rotation as Rot
    t = PD.TYPES[typ]; env = np.array(t["env"]); m, mech = build(typ, s); worst = (0.0, None, None)
    def xf(name, c, u, V):
        l, ax, deg, sc = mech[name](c, u); piv = np.array(m["nodes"][name]["pivot"], float); Rm = Rot.from_rotvec(np.radians(deg)*np.array(ax, float)/np.linalg.norm(ax)).as_matrix(); V = (V - piv)*sc @ Rm.T + piv + np.array(l)
        par = m["nodes"][name].get("parent"); return xf(par, c, u, V) if par in mech else V
    for name in mech:
        V0 = m["by_node"][name]["V"]
        for c in clips:
            for u in np.linspace(0, 1, 41):
                V = xf(name, c, float(u), V0); d = max(float((env[0] - V).max()), float((V - env[1]).max()))
                if d > worst[0]: worst = (d, name, (c, round(float(u), 2)))
    return worst
if __name__ == "__main__" and len(sys.argv) > 2 and sys.argv[2] == "sweep":
    import aanim as AN
    for typ in sys.argv[1].split(","):
        cl = [c[0] for c in AN.CLIPS[AN.clip_set(PD.TYPES[typ]["family"], True)]]
        for s in ST.STYLES: w = sweep(typ, s, cl); print(typ, s, "max outside envelope %.1f mm" % (w[0]*1000), w[1], w[2], "" if w[0] <= 0.010 else "<-- FAIL")
