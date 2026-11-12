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
