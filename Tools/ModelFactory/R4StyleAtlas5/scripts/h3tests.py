"""#1239 Hero Benchmark Pass 3: independent numpy tests of the OWNER SELECTION on the exported GLB files (no Blender). A HYBRID robot mixes, per part
type, the model of Hero Benchmark Pass 1 (hero1/, PR #1270) or Pass 2 (hero2/, PR #1274); the mix is safe only if both models of a type are the
same part to the game. Per style and type (Gun, Wheel, Battery) the OLD and the NEW model are compared, and the selected one is checked.
 X1 nodes      identical contract node names and parents (cosmetic mechanism nodes listed in *_QA.json are style-specific extras, reported apart)
 X2 mounting   every contract node (pivots, sockets, muzzle, port empties) has the same rest position in both models (dev < 0.05 mm);
               identical port records and identical declared envelope (collision outline of the type)
 X3 outline    rest-pose geometry of the SELECTED model (FX nodes excluded) inside the declared envelope (<= 1 mm)
 X4 animation  identical clips (name, start, end, loop), events, fps and length; the GLB of both models animates every contract node of the QA
               record, and the key values of the contract nodes agree (max deviation reported; cosmetic mechanism tracks may differ)
 X5 source     the selected file is byte-identical to the blob of its PR branch (git), i.e. the historical model itself, not a regeneration;
               hero1/ and hero2/ are untouched in this branch
Technical tests only - which hybrid looks best is the owner's decision. usage: python3 h3tests.py"""
import os, sys, json, subprocess, hashlib
import numpy as np
import h3gen as G
PKG = G.PKG; MF = os.path.dirname(PKG); sys.path.insert(0, os.path.join(MF, "R4BlocksRedesign1232", "scripts")); import glbio
CID = dict(Gun="R4W_Basic", Wheel="R4M_WheelM_Side_Offroad", Battery="R4X_Battery", Cube="R4B_Cube", Deployer="R4X_ExplosiveTrapDeployer"); PASS = dict(old="hero1", new="hero2"); BRANCH = dict(old="origin/art/1239-atlas5-hero1", new="origin/art/1239-atlas5-hero2")
def g2b(P): P = np.asarray(P); return np.stack([P[..., 0], -P[..., 2], P[..., 1]], axis=-1)
def git(*a): return subprocess.run(["git", "-C", PKG] + list(a), capture_output=True, text=True).stdout.strip()
def load(which, s, typ):
    cid = CID[typ]; d = os.path.join(PKG, PASS[which], "variants", G.LABEL[s], cid); p = os.path.join(d, cid + ".glb"); qa = json.load(open(os.path.join(d, cid + "_QA.json"))); m = glbio.load(p)
    mech = {f"{cid}_{k}" for k in (qa.get("mechanisms") or {})}; par = {}
    for i, n in enumerate(m.json["nodes"]):
        for c in n.get("children", []): par[m.json["nodes"][c].get("name")] = n.get("name")
    names = {n.get("name"): par.get(n.get("name")) for n in m.json["nodes"] if n.get("name") not in mech}
    pos = {k: g2b(v[:3, 3]) for k, v in m.nodes.items() if k not in mech}; idx = {n.get("name"): i for i, n in enumerate(m.json["nodes"])}; j, bin_ = glbio.read_json_bin(p); keys = {}
    for an in j.get("animations", []):
        for ch in an["channels"]:
            nm = j["nodes"][ch["target"]["node"]].get("name"); sm = an["samplers"][ch["sampler"]]
            keys[(an.get("name"), nm, ch["target"]["path"])] = (np.array(glbio.accessor(j, bin_, sm["input"]), float), np.array(glbio.accessor(j, bin_, sm["output"]), float))
    fx = set(qa.get("fx_objects", [])); V = np.concatenate([g2b(pr["pos"]) for pr in m.prims if pr["node"] not in fx and np.ptp(pr["pos"], axis=0).max() > 1e-4 and abs(np.linalg.det(m.nodes[pr["node"]][:3, :3])) > 1e-6])
    rel = os.path.relpath(p, git("rev-parse", "--show-toplevel")).replace(os.sep, "/")
    return dict(path=p, rel=rel, qa=qa, mech=sorted(mech), names=names, pos=pos, keys=keys, V=V, tris=m.tri_count(), sha256=hashlib.sha256(open(p, "rb").read()).hexdigest(), blob=git("hash-object", p), blob_pr=git("rev-parse", f"{BRANCH[which]}:{rel}"))
def compare(o, n):
    r = {}; r["X1"] = o["names"] == n["names"]; dev = max((float(np.linalg.norm(o["pos"][k] - n["pos"][k]))*1000 for k in o["pos"] if k in n["pos"]), default=0.0)
    r["X2"] = bool(r["X1"] and dev < 0.05 and o["qa"]["ports"] == n["qa"]["ports"] and o["qa"]["envelope_m"] == n["qa"]["envelope_m"]); r["node_dev_mm"] = round(dev, 4)
    ao, an = o["qa"]["animation"], n["qa"]["animation"]; same = all(ao.get(k) == an.get(k) for k in ("clips", "events", "fps", "frames")); contract = [x for x in ao["animated_objects"] if x not in o["mech"]]
    cov = all(any(k[1] == x for k in m["keys"]) for m in (o, n) for x in contract if x not in o["qa"].get("fx_objects", [])); kd = 0.0; ncmp = 0
    for k, (t, v) in o["keys"].items():
        if k[1] in o["mech"] or k not in n["keys"]: continue
        t2, v2 = n["keys"][k]
        if t.shape == t2.shape and v.shape == v2.shape:
            if k[2] == "rotation": v2 = np.where((np.sum(v*v2, axis=-1, keepdims=True) < 0), -v2, v2)
            kd = max(kd, float(np.abs(v - v2).max())); ncmp += 1
        else: kd = max(kd, 9.0)
    r["X4"] = bool(same and cov and kd < 2e-3); r["anim_key_dev"] = round(kd, 5); r["anim_channels_compared"] = ncmp; r["clips"] = [c["name"] for c in ao["clips"]]
    r["mechanism_nodes_old"] = o["mech"]; r["mechanism_nodes_new"] = n["mech"]; return r
def main():
    S = json.load(open(os.path.join(PKG, "hero3", "shots.json")))["configs"]; rep = {}; ok_all = True
    touched = git("diff", "--stat", "origin/art/1239-atlas5-hero2", "--", "hero1", "hero2", "polish2", "polish1", "pilot2", "atlas"); untouched = touched == ""
    for s in G.STYLES:
        mix = S[s]["hybrid"]["mix"]; rs = dict(selection=mix, parts={})
        for typ in ("Gun", "Wheel", "Battery"):
            o, n = load("old", s, typ), load("new", s, typ); r = compare(o, n); sel = o if mix[typ] == "old" else n; env = np.array(sel["qa"]["envelope_m"]); out = max(float((env[0] - sel["V"]).max()), float((sel["V"] - env[1]).max()))
            r["X3"] = bool(out <= 0.001 and sel["qa"]["status"] == "BLENDER_PASS"); r["outside_mm"] = round(max(out, 0)*1000, 3); r["X5"] = bool(sel["blob"] == sel["blob_pr"] and o["blob"] == o["blob_pr"] and n["blob"] == n["blob_pr"] and untouched)
            r["selected"] = dict(source=PASS[mix[typ]], pr="#1270" if mix[typ] == "old" else "#1274", file=sel["rel"], sha256=sel["sha256"], git_blob=sel["blob"], triangles=sel["tris"]); r["PASS"] = bool(r["X1"] and r["X2"] and r["X3"] and r["X4"] and r["X5"]); rs["parts"][typ] = r
        for typ in ("Cube", "Deployer"):                                                         # chassis geometry: one historical model in both passes
            cid = CID[typ]; p = os.path.join(PKG, "hero1", "variants", G.LABEL[s], cid, cid + ".glb"); rel = os.path.relpath(p, git("rev-parse", "--show-toplevel")).replace(os.sep, "/"); b = git("hash-object", p)
            rs["parts"][typ] = dict(selected=dict(source="hero1", pr="#1270", file=rel, sha256=hashlib.sha256(open(p, "rb").read()).hexdigest(), git_blob=b, palette="hero2" if mix["chassis"] == "new" else "hero1"), X5=bool(b == git("rev-parse", f"{BRANCH['old']}:{rel}") and untouched), PASS=bool(b == git("rev-parse", f"{BRANCH['old']}:{rel}") and untouched))
        rs["PASS"] = all(v["PASS"] for v in rs["parts"].values()); ok_all &= rs["PASS"]; rep[s] = rs
    T = os.path.join(PKG, "hero3", "tests"); os.makedirs(T, exist_ok=True)
    json.dump(dict(result="PASS" if ok_all else "FAIL", previous_passes_untouched=untouched, detail=rep), open(os.path.join(T, "hero3_selection_tests.json"), "w"), indent=1)
    with open(os.path.join(T, "HERO3_TEST_SUMMARY.md"), "w", encoding="utf-8") as f:
        f.write("# Hero Benchmark Pass 3: testy wyboru właściciela na wyeksportowanych GLB (numpy)\n\nWynik techniczny: **" + ("PASS" if ok_all else "FAIL") + f"** — {sum(1 for v in rep.values() if v['PASS'])}/5 robotów HYBRID. `hero1/` i `hero2/` bez zmian względem gałęzi PR #1274: **{'TAK' if untouched else 'NIE'}**. Unity: UNITY_NOT_RUN. Odbiór artystyczny: **OCZEKUJE NA WŁAŚCICIELA**.\n\n" + __doc__.split("\n", 3)[3].rsplit("\n", 1)[0] + "\n\n| styl | część | wybrana | X1 | X2 | odch. węzłów mm | X3 | poza obrysem mm | X4 | odch. kluczy | X5 | wynik |\n|---|---|---|---|---|---|---|---|---|---|---|---|\n")
        for s, rs in rep.items():
            for typ, r in rs["parts"].items():
                c = lambda k: ("P" if r[k] else "**F**") if k in r else "—"
                f.write(f"| {G.LABEL[s]} | {typ} | {r['selected']['source']} ({r['selected']['pr']}) | {c('X1')} | {c('X2')} | {r.get('node_dev_mm', '—')} | {c('X3')} | {r.get('outside_mm', '—')} | {c('X4')} | {r.get('anim_key_dev', '—')} | {c('X5')} | {'PASS' if r['PASS'] else '**FAIL**'} |\n")
    print("PASS" if ok_all else "FAIL", {s: {t: [k for k in ("X1", "X2", "X3", "X4", "X5") if k in r and not r[k]] for t, r in rs["parts"].items() if not r["PASS"]} for s, rs in rep.items()})
    for s, rs in rep.items(): print(s, {t: (r.get("node_dev_mm"), r.get("anim_key_dev"), r.get("anim_channels_compared"), r.get("outside_mm")) for t, r in rs["parts"].items() if "X1" in r})
if __name__ == "__main__": main()
