"""Writes INDEX.json, INDEX.md and SHA256SUMS.txt of the R4Wheels library from the real files and *_QA.json reports."""
import glob, hashlib, json, os, sys

ROOT = os.path.abspath(sys.argv[1] if len(sys.argv) > 1 else os.path.join(os.path.dirname(__file__), ".."))
ORDER = ["WheelS_Fork_Road", "WheelS_Caster_Slick", "WheelM_Side_Offroad", "WheelM_Side_Slick", "WheelM_Steer_Road", "WheelM_SwingArm_Offroad",
         "WheelM_Twin_Road", "WheelM_Fork_Spiked", "WheelL_Side_Offroad", "WheelL_Fork_Balloon", "WheelL_Steer_Offroad", "WheelXL_Side_Monster"]
MOUNT_PL = {"fork": "widelec (port na górze)", "side": "piasta boczna (port na ścianie -X)", "steer": "zwrotnica skrętna (port na górze)",
            "caster": "obrotnica samonastawna (port na górze)", "swing": "wahacz wleczony z drążkiem skrętnym (port na górze)", "twin": "widelec, koła bliźniacze (port na górze)"}


def sha(p): return hashlib.sha256(open(p, "rb").read()).hexdigest()


def main():
    sil = json.load(open(os.path.join(ROOT, "compare", "silhouette_check.json")))
    gp = os.path.join(ROOT, "compare", "grid_fit.json"); grid = json.load(open(gp)) if os.path.exists(gp) else {}
    cm = os.path.join(ROOT, "compat", "compat_wheels.json"); cmx = json.load(open(cm)) if os.path.exists(cm) else None
    best = {}
    for pair, v in sil["pairs"].items():
        for a in pair.split("|"):
            if a not in best or v["mean"] > best[a][1]: best[a] = (pair.replace("R4M_", "").replace("|", " ↔ "), v["mean"])
    rows = []
    for k in ORDER:
        mid = "R4M_" + k; d = os.path.join(ROOT, mid)
        qa = json.load(open(os.path.join(d, mid + "_QA.json"), encoding="utf-8"))
        am = json.load(open(os.path.join(d, mid + "_animation_manifest.json"), encoding="utf-8"))
        mm = json.load(open(os.path.join(d, mid + "_model_manifest.json"), encoding="utf-8"))
        rp = mm["node_roles"][mid + "_ROOT"]; sw = qa["sweep"]
        nodes = qa["sweep"]["nodes_file"]
        rows.append({
            "id": mid, "part_kind": "R3PartKind.Wheel", "variant": k, "game_name": qa["game_name"], "size": rp["wheel_size"], "mount_type": rp["mount_type"],
            "mount_pl": MOUNT_PL[rp["mount_type"]], "tread": rp["tread"], "rim": rp["rim"], "tyre_radius_m": rp["tyre_radius_m"], "tyre_width_m": rp["tyre_width_m"],
            "socket": nodes["Socket_Mount"], "ground_probe": nodes["GroundProbe"], "wheel_axle": nodes["Wheel_Axle"],
            "clips": [{"name": c["name"], "start": c["start"], "end": c["end"], "loop": c["loop"]} for c in am["clips"]], "fps": am["fps"],
            "triangles": qa["triangles_total"], "topology_PASS": qa["topology_PASS"], "animation_clash": qa["animation_clash"].get("collisions"),
            "clearance_min_mm": sw.get("clearance_min_mm"), "moving_sweep_cells": sw["moving_sweep"]["cells"], "static_cells": sw["static_cells"],
            "fbx_reimport_worst": qa["fbx_reimport"].get("worst"), "glb_reimport_PASS": qa["glb_reimport"].get("PASS"),
            "silhouette": {"closest_pair": best.get(mid, ("-", 0))[0], "mean_iou": round(best.get(mid, ("-", 0))[1], 3), "flagged": any(mid in p for p in sil.get("flagged", []))},
            "grid_fit_PASS": (grid.get(mid) or {}).get("PASS"),
            "compat_blocks": (cmx or {}).get("summary", {}).get(mid),
            "sha256": {f: sha(os.path.join(d, f)) for f in (mid + ".blend", mid + ".fbx", mid + ".glb", mid + "_colliders.glb", mid + "_LOD1.fbx")},
            "blender_qa": qa["status"], "unity": "UNITY_NOT_RUN"})
    idx = {"library": "Tools/ModelFactory/R4Wheels", "standard": "R41_Turret_v6.1 palette + R4 Robocraft-style port (same as PR #939 / #941 / #963)",
           "blender": "5.2.3 LTS", "fps": 24, "export": {"fbx": "Unity 6 URP, -Z forward, Y up, FBX_SCALE_UNITS, bake_space_transform=False", "glb": "glTF 2.0 binary"},
           "unity_status": "UNITY_NOT_RUN for all variants (no Unity in this session)", "runtime_kind": "all variants are R3PartKind.Wheel visuals; runtime has one wheel placeholder (r 0.39 fork)",
           "silhouette_method": sil["method"], "silhouette_flagged_pairs": sil.get("flagged", []), "models": rows}
    if cmx: idx["compat_blocks"] = {"PASS": cmx["PASS"], "file": "compat/compat_wheels.json", "method": cmx["method"]}
    json.dump(idx, open(os.path.join(ROOT, "INDEX.json"), "w", encoding="utf-8"), indent=1, ensure_ascii=False)
    md = ["# R4Wheels — indeks", "", "| Model | Rozmiar | Mocowanie | Bieżnik / felga | r × szer. [m] | Klipy (⟲ pętla) | Tri | QA | Unity |", "|---|---|---|---|---|---|---|---|---|"]
    for r in rows:
        md.append(f"| `{r['id']}` | {r['size']} | {r['mount_pl']} | {r['tread']} / {r['rim']} | {r['tyre_radius_m']} × {r['tyre_width_m']} | "
                  + ", ".join(c["name"] + ("⟲" if c["loop"] else "") for c in r["clips"]) + f" | {r['triangles']} | {r['blender_qa']} | {r['unity']} |")
    open(os.path.join(ROOT, "INDEX.md"), "w", encoding="utf-8").write("\n".join(md) + "\n")
    lines = []
    for p in sorted(glob.glob(os.path.join(ROOT, "**", "*"), recursive=True)):
        if os.path.isfile(p) and os.path.basename(p) != "SHA256SUMS.txt":
            lines.append(sha(p) + "  " + os.path.relpath(p, ROOT).replace(os.sep, "/"))
    open(os.path.join(ROOT, "SHA256SUMS.txt"), "w").write("\n".join(lines) + "\n")
    print("rows", len(rows), "sums", len(lines))


if __name__ == "__main__":
    main()
