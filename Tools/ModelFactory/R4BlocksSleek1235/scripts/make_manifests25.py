"""#1235 - per-model manifests, catalogue INDEX.json (30 blocks), WAVE_EVIDENCE.md, CHANGED_FILES.txt and SHA256SUMS.txt from the files on disk.
usage: R4_REF=<refs> python3 make_manifests25.py"""
import os, json, hashlib, subprocess
import paths25, layout25 as L, shapes25, glbio, scenes25
from styles import STYLES, R41, ZONE_ORDER

A = STYLES["A"]
def sha(p): return hashlib.sha256(open(p, "rb").read()).hexdigest()
rep = json.load(open(os.path.join(paths25.PKG, "tests", "compat_report.json")))
index = {"issue": 1235, "library": "R4BlocksSleek1235", "art_direction": "A_Sleek", "status": "ART_SOURCE_ONLY / NOT_INSTALLED_IN_GAME / OWNER_REVIEW_REQUIRED",
         "blender": "BLENDER_PASS 25/25 (new) + 5/5 (pilot, #1234)", "source_tests": rep["status"], "unity": "UNITY_NOT_RUN", "windows_player": "WINDOWS_PLAYER_NOT_RUN",
         "interface_source": {"pr": 963, "branch": "art/r4-building-blocks", "commit": paths25.REF_SHA["blocks"]}, "pilot_source": {"pr": 1234, "commit": paths25.PILOT_COMMIT, "dir": "Tools/ModelFactory/R4BlocksRedesign1232/A_Sleek"},
         "reference_commits": paths25.REF_SHA, "grid_m": 0.55, "block_m": 0.52, "face_m": 0.26, "gap_m": 0.03, "paint_zones": ZONE_ORDER, "models": []}
for kind in L.ALL30:
    ID = f"R4B_{kind}"; new = kind not in L.PILOT; d = paths25.new_dir(kind) if new else paths25.pilot_dir(kind)
    qa = json.load(open(os.path.join(d, ID + "_QA.json"))); b = rep["sections"]["blocks"][ID]
    entry = {"id": ID, "kind": kind, "wave": b["wave"], "origin": "#1235" if new else "pilot #1234 (unchanged, referenced)", "dir": os.path.relpath(d, os.path.dirname(paths25.PKG)).replace(os.sep, "/"),
             "status": qa["status"], "unity": "UNITY_NOT_RUN", "cells": len(L.INFO[kind]["cells"]), "contact_mask": qa["contact_mask_local_anchor"],
             "triangles": {"LOD0": b["S7_budget"]["triangles"], "LOD1": b["S7_budget"]["lod1_triangles"], "approved_963": b["S7_budget"]["approved_triangles"]},
             "renderers": b["S7_budget"]["renderers"], "material_slots": b["S7_budget"]["material_slots"], "glb_sha256": sha(os.path.join(d, ID + ".glb")),
             "source_tests": {s: b[s]["PASS"] for s in ("S1_interface", "S2_containment", "S3_contact_patch", "S4_rotations")}}
    index["models"].append(entry)
    if not new: continue
    M = glbio.load(os.path.join(d, ID + ".glb")); lo, hi = M.bounds(); zt = {}
    for pr in M.prims: zt[pr["material"]] = zt.get(pr["material"], 0) + len(pr["idx"])
    files = {}
    for root, _, fs in os.walk(d):
        for f in sorted(fs):
            if f.endswith(("_model_manifest.json", ".blend1")): continue
            fp = os.path.join(root, f); files[os.path.relpath(fp, d).replace(os.sep, "/")] = {"sha256": sha(fp), "bytes": os.path.getsize(fp)}
    man = {"id": ID, "part_kind": kind, "issue": 1235, "wave": b["wave"], "art_direction": "A_Sleek", "replaces_visual_of": f"PR #963 R4Blocks/{ID} (approved files untouched; same part id / kind)",
           "status": qa["status"], "unity": "UNITY_NOT_RUN", "windows_player": "WINDOWS_PLAYER_NOT_RUN", "installed_in_game": False,
           "axes": "file: +Y up, Port_forward = +Z, Port_right = +X (same as the approved GLB)", "pivot": "anchor cell centre (0,0,0), identical to PR #963",
           "bounds_file_axes_m": [[round(float(x), 4) for x in lo], [round(float(x), 4) for x in hi]], "triangles": entry["triangles"], "renderers": 1, "material_slots": len(zt),
           "paint_zones": {z: {"triangles": zt.get(z, 0), "default_r41_colour": A["zone_default"][z], "rgb_linear": list(R41[A["zone_default"][z]][0])} for z in ZONE_ORDER},
           "interface_unchanged": {"cells": qa["cells"], "bonds": qa.get("bonds", []), "contact_mask_local_anchor": qa["contact_mask_local_anchor"], "ports": qa["ports"], "empties": sorted(M.empties),
                                   "colliders": f"use approved R4Blocks/{ID}/{ID}_colliders.glb (no new collider authored)"},
           "style_parameters_m": {k: shapes25.A[k] for k in ("g", "w", "seg", "margin", "chamfer", "rho", "cseg", "seam_h", "min_plate")},
           "source": {"generator": "scripts/shapes25.py (r4sleek.py / r4convex.py / roundcorner.py / geo2d.py) + pilot scripts/r4panel.py, styles.py", "mesh_source": f"mesh_source/{ID}_A.json, mesh_source/{ID}_A_LOD1.json",
                      "blender_build": "scripts/build_blender25.py", "blender": qa.get("blender")},
           "blender_qa": {k: qa[k] for k in ("mesh", "mesh_lod1", "inside_cells", "max_outside_approved_solid_mm", "lod1_max_outside_approved_solid_mm")} | {"reimport_pass": {k: v["PASS"] for k, v in qa["reimport"].items()}},
           "files": files}
    json.dump(man, open(os.path.join(d, ID + "_model_manifest.json"), "w", encoding="utf-8"), indent=1)
json.dump(index, open(os.path.join(paths25.PKG, "INDEX.json"), "w", encoding="utf-8"), indent=1)

# ---- wave evidence
W = rep["sections"]["waves"]; S5 = rep["sections"]["S5_module_matrix"]; gate = open(os.path.join(paths25.PKG, "tests", "build_logs", "gate_chain.state")).read().strip().splitlines()
NAME = {1: "Fala 1 — 13 wypukłych / prostych", 2: "Fala 2 — 6 obłych", 3: "Fala 3 — 3 wklęsłe", 4: "Fala 4 — tunel, łuk wklęsły, kratownica"}
ok = lambda v: "PASS" if v else "FAIL"
E = ["# #1235 — dowody per fala (bramki jakości)", "", "Każda fala była budowana w Blenderze dopiero po przejściu poprzedniej (`tests/build_logs/gate_chain.state`). "
     "Testy źródłowe S1–S7 działają na wyeksportowanych plikach GLB. **UNITY_NOT_RUN · WINDOWS_PLAYER_NOT_RUN.**", ""]
for w in (1, 2, 3, 4):
    d = W[str(w)]; g = next(l for l in gate if l.startswith(f"wave{w} ")); pw = (S5.get("per_wave") or {}).get(str(w)) or (S5.get("per_wave") or {}).get(w)
    E += [f"## {NAME[w]}", "", f"- Klocki: {', '.join('`' + k + '`' for k in d['blocks'])}.", f"- Bramka Blendera: `{g}`.",
          f"- Blender 5.3.0 dla każdego klocka: siatka manifold, spójne normalne, brak zdegenerowanych ścian, UV, eksport FBX i GLB z ponownym importem, LOD1. Log: `tests/build_logs/build_wave{w}.log`.",
          f"- Testy źródłowe: S1 interfejs {ok(d['S1'])} · S2 bryła {ok(d['S2'])} · S3 styk portów {ok(d['S3'])} · S4 obroty {ok(d['S4'])} · S5 moduły {ok(d['S5']) if d['S5'] is not None else 'nie uruchomiono'}."]
    if pw: E.append(f"- Macierz modułów: {pw['mounts']} montaży, błędy styku A: {pw['contact_fail_A']}, zablokowane obroty A / zatwierdzone: {pw['spin_blocked_A']} / {pw['spin_blocked_approved']}.")
    E += [f"- Trójkąty: {d['triangles_approved']} (zatwierdzone) → {d['triangles_A']} (A LOD0), {d['triangles_A_LOD1']} (A LOD1).",
          f"- Rendery: `A_Sleek/R4B_*/renders/` (Blender: front, bok, 3/4 przód, 3/4 tył, spód), plansza `compare/wave{w}_blocks_before_after.jpg`.", f"- Wynik fali: **{ok(d['PASS'])}**.", ""]
open(os.path.join(paths25.PKG, "WAVE_EVIDENCE.md"), "w", encoding="utf-8").write("\n".join(E))

# ---- hashes + changed file list
lines = []
for root, dirs, fs in os.walk(paths25.PKG):
    dirs[:] = sorted(x for x in dirs if x not in ("__pycache__", "_stage"))
    for f in sorted(fs):
        if f in ("SHA256SUMS.txt", "CHANGED_FILES.txt") or f.endswith((".pyc", ".blend1")): continue
        fp = os.path.join(root, f); lines.append(f"{sha(fp)}  {os.path.relpath(fp, paths25.PKG).replace(os.sep, '/')}")
lines.sort(key=lambda s: s.split("  ")[1])
open(os.path.join(paths25.PKG, "SHA256SUMS.txt"), "w", newline="\n").write("\n".join(lines) + "\n")
rel = os.path.relpath(paths25.PKG, os.path.dirname(os.path.dirname(os.path.dirname(paths25.PKG)))).replace(os.sep, "/")
open(os.path.join(paths25.PKG, "CHANGED_FILES.txt"), "w", newline="\n").write(
    f"# #1235: every file of this PR lives under {rel}/ (all added; nothing outside this directory is modified).\n# Pilot directory Tools/ModelFactory/R4BlocksRedesign1232/ is unchanged (commit {paths25.PILOT_COMMIT}).\n"
    + "\n".join(f"{rel}/{l.split('  ')[1]}" for l in lines) + f"\n{rel}/SHA256SUMS.txt\n{rel}/CHANGED_FILES.txt\n")
print("manifests:", len(index["models"]), "catalogue entries;", len(lines), "files hashed")
