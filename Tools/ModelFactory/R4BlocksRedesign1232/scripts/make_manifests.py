"""#1232 pilot - per-model manifests, INDEX.json and SHA256SUMS.txt from the files actually on disk. usage: python3 make_manifests.py"""
import os, json, hashlib
import paths, shapes, glbio
from styles import STYLES, R41, ZONE_ORDER

def sha(p): return hashlib.sha256(open(p, "rb").read()).hexdigest()
index = {"issue": 1232, "library": "R4BlocksRedesign1232", "status": "PILOT_5_OF_30 / OWNER_VISUAL_APPROVAL_REQUIRED", "unity": "UNITY_NOT_RUN",
         "interface_source": {"pr": 963, "branch": "art/r4-building-blocks", "commit": paths.REF_SHA["blocks"]}, "grid_m": 0.55, "block_m": 0.52, "face_m": 0.26, "models": []}
for key, st in STYLES.items():
    for kind in shapes.PILOT:
        d = paths.new_dir(st["label"], kind); ID = f"R4B_{kind}"; qa = json.load(open(os.path.join(d, ID + "_QA.json")))
        M = glbio.load(os.path.join(d, ID + ".glb")); L = glbio.load(os.path.join(d, ID + "_LOD1.glb")); lo, hi = M.bounds()
        zt = {}
        for pr in M.prims: zt[pr["material"]] = zt.get(pr["material"], 0) + len(pr["idx"])
        files = {}
        for root, _, fs in os.walk(d):
            for f in sorted(fs):
                if f.endswith(("_model_manifest.json", ".blend1")): continue
                fp = os.path.join(root, f); files[os.path.relpath(fp, d).replace(os.sep, "/")] = {"sha256": sha(fp), "bytes": os.path.getsize(fp)}
        man = {"id": ID, "issue": 1232, "art_direction": st["label"], "replaces_visual_of": f"PR #963 R4Blocks/{ID} (approved files untouched)",
               "status": qa["status"], "unity": "UNITY_NOT_RUN", "axes": "file: +Y up, Port_forward = +Z, Port_right = +X (same as the approved GLB)",
               "pivot": "cell centre (0,0,0), identical to PR #963", "bounds_file_axes_m": [[round(float(x), 4) for x in lo], [round(float(x), 4) for x in hi]],
               "triangles": {"LOD0": M.tri_count(), "LOD1": L.tri_count(), "approved_963": json.load(open(paths.ref_file("blocks", ID, ID + "_QA.json")))["mesh"]["triangles"]},
               "paint_zones": {z: {"triangles": zt.get(z, 0), "default_r41_colour": st["zone_default"][z], "rgb_linear": list(R41[st["zone_default"][z]][0])} for z in ZONE_ORDER},
               "interface_unchanged": {"cells": qa["cells"], "contact_mask_local_anchor": qa["contact_mask_local_anchor"], "ports": qa["ports"],
                                       "empties": sorted(n for n in M.empties), "colliders": f"use approved R4Blocks/{ID}/{ID}_colliders.glb (no new collider authored)"},
               "style_parameters_m": {k: st[k] for k in ("g", "w", "seg", "margin", "chamfer", "rho", "cseg")},
               "source": {"generator": "scripts/r4panel.py + scripts/styles.py", "mesh_source": f"mesh_source/{ID}_{key}.json", "blender_build": "scripts/build_blender.py", "blender": qa.get("blender")},
               "files": files}
        json.dump(man, open(os.path.join(d, ID + "_model_manifest.json"), "w", encoding="utf-8"), indent=1)
        index["models"].append({k: man[k] for k in ("id", "art_direction", "status", "unity", "triangles", "bounds_file_axes_m")} | {"dir": f"{st['label']}/{ID}", "contact_mask": qa["contact_mask_local_anchor"]})
json.dump(index, open(os.path.join(paths.PKG, "INDEX.json"), "w", encoding="utf-8"), indent=1)
lines = []
for root, dirs, fs in os.walk(paths.PKG):
    dirs[:] = sorted(x for x in dirs if x not in ("__pycache__", "_stage"))
    for f in sorted(fs):
        if f in ("SHA256SUMS.txt",) or f.endswith((".pyc", ".blend1")): continue
        fp = os.path.join(root, f); lines.append(f"{sha(fp)}  {os.path.relpath(fp, paths.PKG).replace(os.sep, '/')}")
open(os.path.join(paths.PKG, "SHA256SUMS.txt"), "w", newline="\n").write("\n".join(sorted(lines, key=lambda s: s.split('  ')[1])) + "\n")
print("manifests:", len(index["models"]), "models;", len(lines), "files hashed")
