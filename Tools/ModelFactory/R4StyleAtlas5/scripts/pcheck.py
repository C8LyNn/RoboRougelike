"""pilot 2 generator-side check: shells, envelope, per-node source bounds (rigs), contacts, same node set in all five styles. usage: python3 pcheck.py [Type,..]"""
import sys, numpy as np
import akit as K, astyle as ST, pdefs as PD, raycast, acheck
def build(typ, s, lod=False):
    t = PD.TYPES[typ]; return K.build(PD.P[typ][s], s, lod, t["env"], t["cid"], bool(t.get("source_clips")), t.get("planes"), True)
def conn_ports(t, T):
    """connector contact faces (file axes in the inventory): node cube translated to the origin, approved ruler"""
    out = []
    for p in PD.INV[t["cid"]]["ports"]:
        c = np.array(PD.AD._b(p["position_m"])); n = np.array(PD.AD._b(p["normal"])); d = next(k for k, v in K.DIRS.items() if np.allclose(v, n)); r = raycast.patch_test(T - (c - n*K.H), [0, 0, 0], d, [0, 0], p["half"])
        if not (r["protruding"] == 0 and r["missing"] == 0 and r["flush_pct"] >= 80): out.append((p["id"], r["flush_pct"], r["protruding"], r["missing"]))
    return out
if __name__ == "__main__":
    types = sys.argv[1].split(",") if len(sys.argv) > 1 else list(PD.TYPES); allok = True
    for typ in types:
        t = PD.TYPES[typ]; env = np.array(t["env"]); nodesets = []; row = []; nports = 0
        for s in ST.STYLES:
            for lod in (False, True):
                try: m = build(typ, s, lod)
                except Exception as ex: print(typ, s, lod, "BUILD ERROR", repr(ex)[:200]); allok = False; continue
                st = K.stats(m); fx = set(t.get("fx", [])); V = np.concatenate([mm["V"] for n, mm in m["by_node"].items() if mm is not None and n not in fx])
                ins = bool((V.min(0) >= env[0] - 5e-4).all() and (V.max(0) <= env[1] + 5e-4).all()); bad = []; nbad = []
                T = acheck.tris_of(m)
                if t["family"] == "connector": bad += conn_ports(t, T)
                for p in acheck.ports_of(t, m):
                    r = raycast.patch_test(T, p["cell"], p["dir"], p["offset"], p["half"])
                    if not (r["protruding"] == 0 and r["missing"] == 0 and r["flush_pct"] >= (80 if p["kind"] == "full" else 65)): bad.append((p["dir"], r["flush_pct"], r["protruding"]))
                for n, b in (t.get("nb") or {}).items():
                    mm = m["by_node"].get(n)
                    if mm is None: nbad.append((n, "missing")); continue
                    d = max(float((np.array(b[0]) - mm["V"].min(0)).max()), float((mm["V"].max(0) - np.array(b[1])).max()))
                    if d > 0.003: nbad.append((n, round(d*1000, 1), mm["V"].min(0).round(3).tolist(), mm["V"].max(0).round(3).tolist()))
                good = st["ok"] and ins and not bad and not nbad; allok &= good
                if not lod: nodesets.append(tuple(sorted(n for n, mm in m["by_node"].items() if mm is not None))); row.append(st["triangles"])
                if not good: print(f"{typ} {s} {'LOD1' if lod else 'LOD0'} topo={st['ok']} bad_shells={st['bad_shells'][:5]} inside={ins} {V.min(0).round(3)} {V.max(0).round(3)} ports={bad} nodes={nbad}")
        same = len(set(nodesets)) == 1; allok &= same
        print(f"{typ:9s} tris A-E {row} same_nodes={same}")
    print("ALL OK" if allok else "FAILURES")
