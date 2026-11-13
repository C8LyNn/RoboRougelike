"""#1239 Hero Benchmark Pass 4: independent numpy tests of the NEW models on the exported GLB files (no Blender, no Unity). Every Pass 4 model is
compared with the approved model it replaces (the survey baseline) - it must be the same part to the game.
 P1 nodes      contract node names and parents identical to the baseline (cosmetic mechanism nodes included: Pass 4 keeps the mechanism set)
 P2 mounting   every node (pivots, sockets, muzzle, port empties) at the baseline rest position (dev < 0.05 mm); identical ports and envelope record
 P3 outline    rest-pose geometry (FX excluded) inside the declared envelope (<= 1 mm); pbuild status BLENDER_PASS, no violations
 P4 animation  identical clips, events, fps, length; every animated node of the baseline is animated; key values of all shared channels agree
 P5 new        the model is NEW geometry: its mesh is not the baseline mesh (vertex count, triangle count and bounding boxes per node differ) and
               the file is not byte-identical to any Pass 1 / Pass 2 file
 P6 files      GLB + FBX + LOD1 GLB + LOD1 FBX + .blend + QA record exist; textures (base, ORM, normal, emission) exist; source .blend exists
Also checked: hero1/, hero2/ untouched. Hull kits: K1 every mesh name the hull-layer rules can ask for exists; K2 plate footprint inside the cell
pitch and not thicker than 60 mm; K3 fairings inside their cell + 190 mm nose reach; K4 files (GLB, FBX, source .blend, four textures).
Technical tests only - the artistic verdict is the owner's. usage: python3 h4tests.py"""
import os, sys, json, subprocess, hashlib, glob
import numpy as np
import h4gen as G, h4pal
PKG = G.PKG; MF = os.path.dirname(PKG); sys.path.insert(0, os.path.join(MF, "R4BlocksRedesign1232", "scripts")); import glbio
CID = dict(Gun="R4W_Basic", Wheel="R4M_WheelM_Side_Offroad", Battery="R4X_Battery"); ROOT = dict(old="hero1/variants", new="hero2/variants", p="hero4/variants", q="hero4/variants_q")
def g2b(P): P = np.asarray(P); return np.stack([P[..., 0], -P[..., 2], P[..., 1]], axis=-1)
def git(*a): return subprocess.run(["git", "-C", PKG] + list(a), capture_output=True, text=True).stdout.strip()
def load(which, s, typ):
    cid = CID[typ]; d = os.path.join(PKG, ROOT[which], G.LABEL[s], cid); p = os.path.join(d, cid + ".glb"); qa = json.load(open(os.path.join(d, cid + "_QA.json"))); m = glbio.load(p); par = {}
    for n in m.json["nodes"]:
        for c in n.get("children", []): par[m.json["nodes"][c].get("name")] = n.get("name")
    names = {n.get("name"): par.get(n.get("name")) for n in m.json["nodes"]}; pos = {k: g2b(v[:3, 3]) for k, v in m.nodes.items()}; j, bin_ = glbio.read_json_bin(p); keys = {}
    for an in j.get("animations", []):
        for ch in an["channels"]:
            nm = j["nodes"][ch["target"]["node"]].get("name"); sm = an["samplers"][ch["sampler"]]; keys[(an.get("name"), nm, ch["target"]["path"])] = (np.array(glbio.accessor(j, bin_, sm["input"]), float), np.array(glbio.accessor(j, bin_, sm["output"]), float))
    fx = set(qa.get("fx_objects", [])); per = {}
    for pr in m.prims:
        if pr["node"] in fx or np.ptp(pr["pos"], axis=0).max() < 1e-4: continue
        d_ = per.setdefault(pr["node"], [0, 0, []]); d_[0] += len(pr["pos"]); d_[1] += len(pr["idx"]); d_[2].append(g2b(pr["pos"]))
    sig = {n: (v[0], v[1], np.concatenate(v[2]).min(0).round(3).tolist(), np.concatenate(v[2]).max(0).round(3).tolist()) for n, v in per.items()}; V = np.concatenate([x for v in per.values() for x in v[2]])
    return dict(dir=d, path=p, qa=qa, names=names, pos=pos, keys=keys, V=V, sig=sig, tris=m.tri_count(), sha256=hashlib.sha256(open(p, "rb").read()).hexdigest())
def compare(b, n, s, typ, set_):
    r = {}; r["P1"] = b["names"] == n["names"]; dev = max((float(np.linalg.norm(b["pos"][k] - n["pos"][k]))*1000 for k in b["pos"] if k in n["pos"]), default=0.0)
    r["P2"] = bool(r["P1"] and dev < 0.05 and b["qa"]["ports"] == n["qa"]["ports"] and b["qa"]["envelope_m"] == n["qa"]["envelope_m"]); r["node_dev_mm"] = round(dev, 4)
    env = np.array(n["qa"]["envelope_m"]); out = max(float((env[0] - n["V"]).max()), float((n["V"] - env[1]).max())); r["P3"] = bool(out <= 0.001 and n["qa"]["status"] == "BLENDER_PASS" and not n["qa"].get("failures") and n["qa"].get("inside_envelope", True)); r["outside_mm"] = round(max(out, 0)*1000, 3); r["status"] = n["qa"]["status"]
    ab, an = b["qa"]["animation"], n["qa"]["animation"]; same = all(ab.get(k) == an.get(k) for k in ("clips", "events", "fps", "frames")); cov = all(any(k[1] == x for k in n["keys"]) for x in {k[1] for k in b["keys"]}); kd = 0.0; nc = 0
    for k, (t, v) in b["keys"].items():
        if k not in n["keys"]: kd = 9.0; continue
        t2, v2 = n["keys"][k]
        if t.shape == t2.shape and v.shape == v2.shape:
            if k[2] == "rotation": v2 = np.where((np.sum(v*v2, axis=-1, keepdims=True) < 0), -v2, v2)
            kd = max(kd, float(np.abs(v - v2).max())); nc += 1
        else: kd = max(kd, 9.0)
    r["P4"] = bool(same and cov and kd < 2e-3); r["anim_key_dev"] = round(kd, 5); r["anim_channels"] = nc; r["clips"] = [c["name"] for c in ab["clips"]]
    old_hashes = HASHES; diff = [k for k in n["sig"] if k not in b["sig"] or b["sig"][k] != n["sig"][k]]; r["P5"] = bool(n["sha256"] not in old_hashes and len(diff) == len(n["sig"]) and n["tris"] != b["tris"]); r["nodes_with_new_geometry"] = "%d/%d" % (len(diff), len(n["sig"]))
    cid = CID[typ]; d = n["dir"]; tex = os.path.join(PKG, "hero4", "textures" + ("_q" if set_ == "q" else ""), G.LABEL[s]); srcb = os.path.join(PKG, "hero4", "source" + ("_q" if set_ == "q" else ""), G.LABEL[s], cid + "_SOURCE.blend")
    need = [os.path.join(d, cid + e) for e in (".glb", ".fbx", "_LOD1.glb", "_LOD1.fbx", ".blend", "_QA.json")] + [os.path.join(tex, cid + "_" + k) for k in ("base.jpg", "orm.jpg", "normal.png", "emission.jpg")] + [srcb]
    r["P6"] = all(os.path.exists(x) for x in need); r["missing"] = [os.path.relpath(x, PKG) for x in need if not os.path.exists(x)]
    r["triangles"] = dict(baseline=b["tris"], pass4=n["tris"], lod1=n["qa"].get("triangles_lod1")); r["PASS"] = all(r[k] for k in ("P1", "P2", "P3", "P4", "P5", "P6")); return r
def kits():
    S = json.load(open(os.path.join(PKG, "hero4", "shots.json"))); out = {}; H = 0.26; HP = 0.275
    for s in G.STYLES:
        for k, lab in S["p4kit"][s].items():
            d = os.path.join(PKG, "hero4", "hull_kit", lab); info = json.load(open(os.path.join(d, "hullkit.json"))); m = glbio.load(os.path.join(d, "R4H_HullKit.glb")); names = set(info["names"]); nv = info["variants"]; r = {}
            want = {"gasket", "lip", "ramp", "cheek"} | {"nose_%d" % i for i in range(4)} | {"plate_%02d" % mk + ("_%d" % v if nv > 1 else "") for mk in range(16) for v in range(nv)}
            glbn = {pr["node"] for pr in m.prims}; r["K1"] = bool(want <= names and want <= glbn); r["pieces"] = len(names); bb = {}
            for pr in m.prims: V = g2b(pr["pos"]); a = bb.setdefault(pr["node"], [V.min(0), V.max(0)]); a[0] = np.minimum(a[0], V.min(0)); a[1] = np.maximum(a[1], V.max(0))
            pl = [n for n in bb if n.startswith("plate_")]; r["K2"] = all(bb[n][0][0] >= -HP - 1e-3 and bb[n][1][0] <= HP + 1e-3 and bb[n][0][1] >= -HP - 1e-3 and bb[n][1][1] <= HP + 1e-3 and bb[n][0][2] >= H - 0.004 and bb[n][1][2] <= H + 0.060 for n in pl); r["plate_thickness_mm"] = round(max(float(bb[n][1][2]) for n in pl)*1000 - H*1000, 1)
            fa = [n for n in bb if n in ("ramp", "cheek") or n.startswith("nose")]; r["K3"] = all((np.abs(bb[n][0]) <= 0.30).all() and (np.abs(bb[n][1]) <= (0.30 if not n.startswith("nose") else 0.46)).all() for n in fa); r["nose_reach_mm"] = round(max(float(bb[n][1][2]) for n in bb if n.startswith("nose"))*1000 - H*1000, 1)
            need = [os.path.join(d, f) for f in ("R4H_HullKit.glb", "R4H_HullKit.fbx", "R4H_HullKit_SOURCE.blend", "hullkit_base.jpg", "hullkit_orm.jpg", "hullkit_normal.jpg", "hullkit_emission.jpg")]; r["K4"] = all(os.path.exists(x) for x in need)
            r["triangles_kit"] = m.tri_count(); r["triangles_plate_00"] = info["tris_lo"].get("plate_00" + ("_0" if nv > 1 else "")); r["PASS"] = all(r[x] for x in ("K1", "K2", "K3", "K4")); out[s + ":" + k] = r
    return out
HASHES = set()
def main():
    global HASHES
    for f in glob.glob(os.path.join(PKG, "hero1", "variants", "*", "*", "*.glb")) + glob.glob(os.path.join(PKG, "hero2", "variants", "*", "*", "*.glb")): HASHES.add(hashlib.sha256(open(f, "rb").read()).hexdigest())
    touched = git("diff", "--stat", "origin/art/1239-atlas5-hero2", "--", "hero1", "hero2", "polish2", "polish1", "pilot2", "atlas"); untouched = touched == ""; rep = {}; ok = True
    for (typ, s, set_), base in sorted(h4pal.BASELINE.items(), key=lambda x: (x[0][1], x[0][0], x[0][2])):
        r = compare(load(base, s, typ), load(set_, s, typ), s, typ, set_); r["baseline"] = base; rep["%s %s %s" % (G.LABEL[s], typ, set_.upper())] = r; ok &= r["PASS"]
    K = kits(); ok &= all(v["PASS"] for v in K.values()) and untouched
    T = os.path.join(PKG, "hero4", "tests"); os.makedirs(T, exist_ok=True); json.dump(dict(result="PASS" if ok else "FAIL", previous_passes_untouched=untouched, unity="UNITY_NOT_RUN", models=rep, hull_kits=K), open(os.path.join(T, "hero4_tests.json"), "w"), indent=1)
    with open(os.path.join(T, "HERO4_TEST_SUMMARY.md"), "w", encoding="utf-8") as f:
        f.write("# Hero Benchmark Pass 4: testy nowych modeli na wyeksportowanych GLB (numpy, bez Blendera i bez Unity)\n\nWynik: **" + ("PASS" if ok else "FAIL") + f"** — modele {sum(1 for v in rep.values() if v['PASS'])}/{len(rep)}, zestawy pancerza {sum(1 for v in K.values() if v['PASS'])}/{len(K)}. `hero1/` i `hero2/` bez zmian: **{'TAK' if untouched else 'NIE'}**. Unity: **UNITY_NOT_RUN**.\n\n" + __doc__.split("\n", 2)[2].rsplit("\n", 1)[0] + "\n\n| model | wzorzec | P1 | P2 | odch. węzłów mm | P3 | poza obrysem mm | P4 | odch. kluczy | kanały | P5 nowa geometria | P6 | trójkąty wzorzec → Pass 4 | wynik |\n|---|---|---|---|---|---|---|---|---|---|---|---|---|---|\n")
        for k, r in rep.items(): c = lambda x: "P" if r[x] else "**F**"; f.write(f"| {k} | {r['baseline']} | {c('P1')} | {c('P2')} | {r['node_dev_mm']} | {c('P3')} | {r['outside_mm']} | {c('P4')} | {r['anim_key_dev']} | {r['anim_channels']} | {c('P5')} {r['nodes_with_new_geometry']} | {c('P6')} | {r['triangles']['baseline']} → {r['triangles']['pass4']} | {'PASS' if r['PASS'] else '**FAIL**'} |\n")
        f.write("\n| zestaw pancerza | części | K1 | K2 | grubość płyty mm | K3 | wysięg dzioba mm | K4 | trójkąty zestawu | wynik |\n|---|---|---|---|---|---|---|---|---|---|\n")
        for k, r in K.items(): c = lambda x: "P" if r[x] else "**F**"; f.write(f"| {k} | {r['pieces']} | {c('K1')} | {c('K2')} | {r['plate_thickness_mm']} | {c('K3')} | {r['nose_reach_mm']} | {c('K4')} | {r['triangles_kit']} | {'PASS' if r['PASS'] else '**FAIL**'} |\n")
    print("PASS" if ok else "FAIL"); [print(k, {x: r[x] for x in ("P1", "P2", "P3", "P4", "P5", "P6", "node_dev_mm", "outside_mm", "anim_key_dev", "nodes_with_new_geometry", "status")}, r["missing"][:3]) for k, r in rep.items() if not r["PASS"]]; [print(k, r) for k, r in K.items() if not r["PASS"]]
if __name__ == "__main__": main()
