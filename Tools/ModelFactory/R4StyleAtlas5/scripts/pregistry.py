"""#1239 pilot 2: registry of the 40 variants from the Blender QA records + GLB tests + silhouette metrics -> pilot2/pilot2_registry.json / .csv and the table block of pilot2/README.md. usage: python3 pregistry.py"""
import os, json, csv
import astyle as ST, pdefs as PD
PKG = os.path.dirname(os.path.dirname(os.path.abspath(__file__))); P2 = os.path.join(PKG, "pilot2"); VAR = os.path.join(P2, "variants")
def main():
    tests = json.load(open(os.path.join(P2, "tests", "pilot2_glb_tests.json"))); met = json.load(open(os.path.join(P2, "compare", "silhouette_metrics.json"))); rows = []
    for t in PD.ORDER:
        cid = PD.CID[t]
        for s in ST.STYLES:
            d = os.path.join(VAR, ST.LABEL[s], cid); q = json.load(open(os.path.join(d, cid + "_QA.json"))); g = tests["detail"][cid][s]; an = q["animation"]
            rows.append(dict(type=t, id=cid, style=s, style_label=ST.LABEL[s], path=os.path.relpath(d, PKG).replace(os.sep, "/"), construction=PD.CONCEPT[t][s], blender=q["status"], export_reimport=q["export_reimport"], glb_tests="PASS" if all(g[k] for k in ("T1", "T2", "T4", "T5")) and tests["detail"][cid]["T3"] and tests["detail"][cid]["T6"] else "FAIL",
                             unity=q["unity"], artistic_acceptance=q["artistic_acceptance"], triangles=q["triangles"], triangles_lod1=q["triangles_lod1"], parts=q.get("parts"), mesh_objects=q["mesh_objects"], materials=q["materials"], textures=[v for v in (q["textures"].get("base"), q["textures"].get("orm"), q["textures"].get("normal")) if v],
                             ports_pass=all(p["PASS"] for p in q["port_contact_test"]) and all(p["PASS"] for p in q["lod1_port_contact_test"]), ports=len(q["port_contact_test"]), interface_vs_source=(q["interface_vs_source"] or {}).get("PASS"), node_bounds_vs_source_mm=(q["node_bounds_vs_source"] or {}).get("max_outside_source_node_bounds_mm"),
                             inside_envelope=q["inside_envelope"] and q["lod1_inside_envelope"], animation_mode=an["mode"], clips=[c["name"] for c in an["clips"]], animated_objects=len(an["animated_objects"]), sweep_outside_mm=an["sweep_outside_envelope_mm"], files=sorted(q["files"])))
    json.dump(dict(issue=1239, pilot=2, variants=len(rows), blender_pass=sum(r["blender"] == "BLENDER_PASS" for r in rows), glb_tests_pass=sum(r["glb_tests"] == "PASS" for r in rows), unity="UNITY_NOT_RUN", artistic_acceptance="PENDING_OWNER_REVIEW", silhouette=met["summary"], rows=rows),
              open(os.path.join(P2, "pilot2_registry.json"), "w"), indent=1, ensure_ascii=False)
    with open(os.path.join(P2, "pilot2_registry.csv"), "w", newline="", encoding="utf-8") as f:
        w = csv.writer(f); k = ["type", "id", "style_label", "blender", "export_reimport", "glb_tests", "unity", "artistic_acceptance", "triangles", "triangles_lod1", "parts", "mesh_objects", "ports", "ports_pass", "interface_vs_source", "node_bounds_vs_source_mm", "animation_mode", "animated_objects", "construction", "path"]
        w.writerow(k); [w.writerow([r[x] for x in k]) for r in rows]
    L = ["| typ | styl | konstrukcja | tris / LOD1 | części | kontakty | interfejs źródła | Blender | GLB | ", "|---|---|---|---|---|---|---|---|---|"]
    for r in rows:
        L.append(f"| {PD.NAME_PL[r['type']] if r['style'] == 'A' else ''} | {r['style']} | {r['construction']} | {r['triangles']} / {r['triangles_lod1']} | {r['parts']} | {r['ports']} {'PASS' if r['ports_pass'] else 'FAIL'} | {'zgodny' if r['interface_vs_source'] else ('—' if r['interface_vs_source'] is None else 'FAIL')}{'' if r['node_bounds_vs_source_mm'] is None else ' (węzły +%.1f mm)' % r['node_bounds_vs_source_mm']} | {r['blender'].replace('BLENDER_', '')} | {r['glb_tests']} |")
    M = ["| typ | IoU sylwetek 3/4 (średnio) | max pary | wszystkie widoki (średnio) | poprzedni atlas 3/4 |", "|---|---|---|---|---|"]
    for t in PD.ORDER: m = met["types"][t]; M.append(f"| {PD.NAME_PL[t]} | {m['mean_iou_34']:.2f} | {m['max_iou_34']:.2f} | {m['mean_iou_all_views']:.2f} | {m['previous_atlas_mean_iou_34']:.2f} |")
    open(os.path.join(P2, "_tables.md"), "w", encoding="utf-8").write("\n".join(L) + "\n\n" + "\n".join(M) + "\n")
    print(len(rows), "variants;", sum(r["blender"] == "BLENDER_PASS" for r in rows), "BLENDER_PASS;", sum(r["glb_tests"] == "PASS" for r in rows), "GLB PASS; ports all", all(r["ports_pass"] for r in rows), "; iface", [r["id"][:8] + r["style"] for r in rows if r["interface_vs_source"] is False],
          "; max node dev", max((r["node_bounds_vs_source_mm"] or 0) for r in rows), "; tris total", sum(r["triangles"] for r in rows), "; sweep max", max(r["sweep_outside_mm"] for r in rows))
if __name__ == "__main__": main()
