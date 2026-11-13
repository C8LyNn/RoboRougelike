# -*- coding: utf-8 -*-
"""#1239 Hero Benchmark Pass 4: the list of proposals as data and as a table (hero4/report/models.json, hero4/MODELS.md).
usage: python3 h4report.py"""
import os, json, hashlib
import h4gen as G, h4notes, h4pal
PKG = G.PKG; H4 = os.path.join(PKG, "hero4"); CID = dict(Gun="R4W_Basic", Wheel="R4M_WheelM_Side_Offroad", Battery="R4X_Battery"); CAT = dict(Gun="Działko", Wheel="Koło", Battery="Bateria", chassis="Kadłub (pancerz)")
SN = dict(A="A Sleek", B="B Brutal", C="C Arcade", D="D Scrap", E="E Heroic Toon"); PL = dict(old="Pass 1 (#1270)", new="Pass 2 (#1274)"); ROOT = dict(old="hero1/variants", new="hero2/variants")
def sha(p): return hashlib.sha256(open(p, "rb").read()).hexdigest() if os.path.exists(p) else None
def main():
    S = json.load(open(os.path.join(H4, "shots.json"))); rows = []
    for cat in ("chassis", "Gun", "Wheel", "Battery"):
        for s in G.STYLES:
            sv = G.SURVEY[s][cat]
            for set_, base in ((("p", sv[0]), ("q", sv[1])) if isinstance(sv, tuple) else (("p", sv),)):
                keep, new = h4notes.N[(cat, s, set_)]; q = "_q" if set_ == "q" else ""; r = dict(category=cat, style=s, proposal=set_.upper() if isinstance(sv, tuple) else "", tie=isinstance(sv, tuple), baseline=base, kept=keep, new=new)
                if cat == "chassis":
                    lab = S["p4kit"][s][set_]; d = os.path.join(H4, "hull_kit", lab); info = json.load(open(os.path.join(d, "hullkit.json"))); r.update(dir="hero4/hull_kit/" + lab, pieces=info["pieces"], variants=info["variants"], triangles_game=info["triangles_total"], triangles_source=info["tris_hi_total"], texture_px=info["size"], palette=G.KITPAL[s][set_], sha256=sha(os.path.join(d, "R4H_HullKit.glb")),
                             files=["R4H_HullKit.glb", "R4H_HullKit.fbx", "R4H_HullKit_SOURCE.blend", "hullkit_base.jpg", "hullkit_orm.jpg", "hullkit_normal.jpg", "hullkit_emission.jpg"], baseline_dir="hero1/hull_kit/" + G.LABEL[s] + " (geometry of Pass 1 in the palette of " + PL[base] + ")")
                else:
                    cid = CID[cat]; d = os.path.join(H4, "variants" + q, G.LABEL[s], cid); t = json.load(open(os.path.join(H4, "report" + q, G.LABEL[s] + "_" + cid + ".json"))); qa = json.load(open(os.path.join(d, cid + "_QA.json"))); bq = json.load(open(os.path.join(PKG, ROOT[base], G.LABEL[s], cid, cid + "_QA.json")))
                    r.update(dir="hero4/variants%s/%s/%s" % (q, G.LABEL[s], cid), id=cid, triangles_source=t["tris_hi_total"], triangles_game=qa["triangles"], triangles_lod1=qa.get("triangles_lod1"), triangles_baseline=bq["triangles"], status=qa["status"], unity=qa.get("unity"), nodes=sorted(t["tris_lo"]), skin=t["skin"], sha256=sha(os.path.join(d, cid + ".glb")),
                             source="hero4/source%s/%s/%s_SOURCE.blend" % (q, G.LABEL[s], cid), textures="hero4/textures%s/%s/%s_{base,orm,normal,emission}" % (q, G.LABEL[s], cid), baseline_dir="%s/%s/%s" % (ROOT[base], G.LABEL[s], cid), clips=[c["name"] for c in qa["animation"]["clips"]], mechanisms=sorted(qa.get("mechanisms") or {}))
                rows.append(r)
    os.makedirs(os.path.join(H4, "report"), exist_ok=True); json.dump(dict(proposals=rows, survey=S["survey"]), open(os.path.join(H4, "report", "models.json"), "w"), indent=1, ensure_ascii=False)
    with open(os.path.join(H4, "MODELS.md"), "w", encoding="utf-8") as f:
        f.write("# Hero Benchmark Pass 4 — lista propozycji\n\n%d propozycji: %d modeli części i %d zestawów pancerza. „Wzorzec” to kierunek preferowany w ankiecie 1; remis = dwie propozycje (P ze starego, Q z nowego).\n" % (len(rows), sum(1 for r in rows if r["category"] != "chassis"), sum(1 for r in rows if r["category"] == "chassis")))
        for cat in ("chassis", "Gun", "Wheel", "Battery"):
            f.write("\n## %s\n\n| Styl | Wzorzec | Zachowane ze wzorca | Nowe w Pass 4 | Trójkąty: źródło / gra%s | Pliki |\n|---|---|---|---|---|---|\n" % (CAT[cat], " / LOD1 (wzorzec)" if cat != "chassis" else ""))
            for r in rows:
                if r["category"] != cat: continue
                tr = ("%s / %s / %s (%s)" % (r["triangles_source"], r["triangles_game"], r["triangles_lod1"], r["triangles_baseline"])) if cat != "chassis" else ("%s / %s w %d siatkach" % (r["triangles_source"], r["triangles_game"], r["pieces"]))
                f.write("| %s%s | %s | %s | %s | %s | `%s` |\n" % (SN[r["style"]], (" — **" + r["proposal"] + "**") if r["proposal"] else "", PL[r["baseline"]], r["kept"], r["new"], tr, r["dir"]))
    print("proposals", len(rows))
if __name__ == "__main__": main()
