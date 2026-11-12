"""#1239 asset matrix: style_registry.json / .csv = every canonical id x five styles with the REAL delivered files (sha-256), triangles,
QA status and tests; the completeness check fails on a missing item / style. usage: python3 aregistry.py"""
import os, json, csv, hashlib
import astyle as ST
PKG = os.path.dirname(os.path.dirname(os.path.abspath(__file__))); ATL = os.path.join(PKG, "atlas")
def sha(p): return hashlib.sha256(open(p, "rb").read()).hexdigest()
def main():
    inv = json.load(open(os.path.join(PKG, "inventory", "canonical_inventory.json"))); tp = os.path.join(PKG, "tests", "atlas_glb_tests.json"); tests = json.load(open(tp))["detail"] if os.path.exists(tp) else {}
    rows = []; done = 0; full = 0
    for it in inv["items"]:
        cid = it["CanonicalPartId"]; st = {}
        for s in ST.STYLES:
            d = os.path.join(ATL, ST.LABEL[s], cid); qp = os.path.join(d, cid + "_QA.json")
            if not os.path.exists(qp): st[s] = dict(status="NOT_STARTED", asset=None); continue
            q = json.load(open(qp)); t = tests.get(cid, {}).get(s, {})
            st[s] = dict(status=q["status"], export_reimport=q["export_reimport"], glb_test=("GLB_TEST_PASS" if t and all(t.get(k) for k in ("T1", "T2", "T4", "T5")) else ("GLB_TEST_FAIL" if t else "NOT_RUN")), unity="UNITY_NOT_RUN",
                         asset="atlas/%s/%s/%s.glb" % (ST.LABEL[s], cid, cid), sha256={f: v["sha256"] for f, v in q["files"].items() if f.endswith((".glb", ".fbx", ".blend"))}, triangles=q["triangles"], triangles_lod1=q["triangles_lod1"],
                         mesh_objects=q["mesh_objects"], material_slots=q["material_slots_total"], clips=[c["name"] for c in q["animation"]["clips"]], events=q["animation"]["events"], animated_objects=len(q["animation"]["animated_objects"]),
                         animation_source=q["animation"]["mode"], failures=q["failures"])
            done += q["status"] == "BLENDER_PASS"
        allp = all(st[s]["status"] == "BLENDER_PASS" for s in ST.STYLES); full += allp
        rows.append(dict(CanonicalPartId=cid, family=it["family"], source_pr=it["source"]["pr"], source_sha256=it["source"]["sha256"], five_of_five=allp, swap_test=tests.get(cid, {}).get("T3"), geometry_distinct=tests.get(cid, {}).get("T6"), styles=st))
    out = dict(issue=1239, canonical_ids=len(rows), target_variants=5*len(rows), variants_blender_pass=done, ids_five_of_five=full, ids_incomplete=[r["CanonicalPartId"] for r in rows if not r["five_of_five"]], unity="UNITY_NOT_RUN", items=rows)
    json.dump(out, open(os.path.join(PKG, "style_registry.json"), "w"), indent=1)
    with open(os.path.join(PKG, "style_registry.csv"), "w", newline="") as f:
        w = csv.writer(f); w.writerow(["CanonicalPartId", "family", "five_of_five"] + [f"{s}_{k}" for s in ST.STYLES for k in ("status", "tris", "lod1", "glb_test")])
        for r in rows: w.writerow([r["CanonicalPartId"], r["family"], r["five_of_five"]] + [x for s in ST.STYLES for x in (r["styles"][s]["status"], r["styles"][s].get("triangles"), r["styles"][s].get("triangles_lod1"), r["styles"][s].get("glb_test"))])
    print("ids", len(rows), "variants BLENDER_PASS", done, "/", 5*len(rows), "ids 5/5", full)
if __name__ == "__main__": main()
