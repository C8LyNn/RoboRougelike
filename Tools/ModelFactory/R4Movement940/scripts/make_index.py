"""Writes INDEX.json, INDEX.md and SHA256SUMS.txt for the R4Movement940 library from the real files + QA reports."""
import glob, hashlib, json, os, sys

ROOT = os.path.abspath(sys.argv[1] if len(sys.argv) > 1 else os.path.join(os.path.dirname(__file__), ".."))
ORDER = ["Wheel", "CrawlerTrack", "HoverEngine", "OmniDrive", "Suspension", "Booster",
         "JumpDrive", "Blink", "Gyroscope", "MagneticGrapple", "IndependentSteering"]
RISKS = {
    "Wheel": ["fork legs reach below the cell (runtime axle is 0.26 m below cell centre) - documented, non-strict housing"],
    "CrawlerTrack": ["left track only; right side = mirror transform (scale X -1)", "~30.8k tris, highest in the set (budget 32k)", "track spans front/rear cells like the runtime 0.936 m box"],
    "HoverEngine": ["duct bottom at -0.317 m (runtime disc) - non-strict housing"],
    "OmniDrive": ["socket offset x 0.16 (motor pod on +X); mount on the outer face", "roller spin is visual; strafe stays runtime"],
    "Suspension": ["damper/spring follow the arm by baked keys (no constraints)", "SWEEP_WheelEnvelope is a QA helper, not exported as visible geometry"],
    "Booster": ["bell nozzle reaches 0.52 m behind the cell (runtime engine cylinder)"],
    "JumpDrive": ["0.10 m stroke is visual; jump height/cooldown remain runtime (1.2 m / 6 s)"],
    "Blink": ["3 mm petal knuckle pin gap (documented)", "silhouette closest pair is JumpDrive|Blink (IoU 0.735, below 0.85)"],
    "Gyroscope": ["runtime placeholder ring d 0.598 exceeds the cell; the model stays inside"],
    "MagneticGrapple": ["cable is a scaled visual, no rope physics; head travel visual only"],
    "IndependentSteering": ["no runtime placeholder exists for this kind; yaw limit visual only"],
}


def sha(p):
    return hashlib.sha256(open(p, "rb").read()).hexdigest()


def main():
    sil = json.load(open(os.path.join(ROOT, "compare", "silhouette_check.json")))
    best = {}
    for pair, v in sil["pairs"].items():
        for a in pair.split("|"):
            if a not in best or v["mean"] > best[a][1]:
                best[a] = (pair.replace("R4M_", "").replace("|", " ↔ "), v["mean"])
    rows = []
    for k in ORDER:
        mid = "R4M_" + k
        d = os.path.join(ROOT, mid)
        qa = json.load(open(os.path.join(d, mid + "_QA.json")))
        am = json.load(open(os.path.join(d, mid + "_animation_manifest.json")))
        sw = qa["sweep"]
        gc = sw.get("ground_contact") if isinstance(sw.get("ground_contact"), dict) else None
        f = {n: sha(os.path.join(d, n)) for n in [mid + ".blend", mid + ".fbx", mid + ".glb"]}
        rows.append({
            "id": mid, "part_kind": qa["kind"], "game_name": qa["game_name"],
            "source_blend": mid + "/" + mid + ".blend", "sha256_blend": f[mid + ".blend"],
            "sha256_fbx": f[mid + ".fbx"], "sha256_glb": f[mid + ".glb"],
            "clips": [{"name": c["name"], "start": c["start"], "end": c["end"], "loop": c["loop"]} for c in am["clips"]],
            "fps": am["fps"],
            "silhouette": {"closest_pair": best.get(mid, ("-", 0))[0], "mean_iou": round(best.get(mid, ("-", 0))[1], 3),
                           "flagged": any(mid in p for p in sil.get("flagged", []))},
            "triangles": qa["triangles_total"], "topology_PASS": qa["topology_PASS"],
            "mount_cell": sw.get("mount_cell"), "housing_inside_mount_cell": sw.get("housing_inside_mount_cell"),
            "housing_policy": sw.get("housing_policy"),
            "moving_sweep_cells": len(sw.get("moving_sweep", {}).get("cells", [])),
            "ground_contact": gc and {"runtime_y": gc.get("runtime_y"), "deviation_mm": gc.get("deviation_mm")},
            "collision_envelope_PASS": (sw.get("collision_envelope") or {}).get("PASS"),
            "animation_clash": qa["animation_clash"].get("collisions"),
            "fbx_reimport_worst": qa["fbx_reimport"].get("worst"),
            "glb_reimport_PASS": qa["glb_reimport"].get("PASS"),
            "palette_PASS": qa["palette"].get("PASS"),
            "blender_qa": qa["status"], "unity": "UNITY_NOT_RUN",
            "risks": RISKS[k],
        })
    idx = {"issue": 940, "library": "Tools/ModelFactory/R4Movement940", "standard": "R41_Turret_v6.1",
           "blender": "5.2.3 LTS", "fps": 24, "export": {"fbx": "Unity 6 URP, -Z forward, Y up, FBX_SCALE_UNITS, bake_space_transform=False",
                                                        "glb": "glTF 2.0 binary, per-node animations on one master timeline"},
           "unity_status": "UNITY_NOT_RUN for all models (no Unity in this session)",
           "silhouette_method": sil["method"], "silhouette_flagged_pairs": sil.get("flagged", []),
           "models": rows}
    json.dump(idx, open(os.path.join(ROOT, "INDEX.json"), "w"), indent=1, ensure_ascii=False)

    md = ["# R4Movement940 — indeks biblioteki", "",
          "| part_kind | source | FBX SHA | GLB SHA | klipy | sylwetka (najbliższa para, IoU) | topologia / montaż / sweep / eksport | Blender QA | Unity | ryzyka |",
          "|---|---|---|---|---|---|---|---|---|---|"]
    for r in rows:
        clips = ", ".join(c["name"] + ("⟲" if c["loop"] else "") for c in r["clips"])
        gc = r["ground_contact"]
        tm = "topo {} · {} tri · obudowa w komórce: {} · sweep {} kom. · grunt {} · kolizje anim. {} · FBX {} mm / GLB {}".format(
            "PASS" if r["topology_PASS"] else "FAIL", r["triangles"], "tak" if r["housing_inside_mount_cell"] else "nie (opisane)",
            r["moving_sweep_cells"], ("{} m, {} mm".format(gc["runtime_y"], gc["deviation_mm"]) if gc else "n/d"),
            r["animation_clash"], r["fbx_reimport_worst"]["geom_mm"], "PASS" if r["glb_reimport_PASS"] else "FAIL")
        md.append("| {} | `{}` | `{}…` | `{}…` | {} | {} {} | {} | {} | {} | {} |".format(
            r["part_kind"].replace("R3PartKind.", ""), r["source_blend"], r["sha256_fbx"][:12], r["sha256_glb"][:12], clips,
            r["silhouette"]["closest_pair"], r["silhouette"]["mean_iou"], tm, r["blender_qa"], r["unity"], "; ".join(r["risks"])))
    open(os.path.join(ROOT, "INDEX.md"), "w").write("\n".join(md) + "\n")

    lines = []
    for p in sorted(glob.glob(os.path.join(ROOT, "**", "*"), recursive=True)):
        if os.path.isfile(p) and os.path.basename(p) != "SHA256SUMS.txt":
            lines.append(sha(p) + "  " + os.path.relpath(p, ROOT).replace(os.sep, "/"))
    open(os.path.join(ROOT, "SHA256SUMS.txt"), "w").write("\n".join(lines) + "\n")
    print("index rows", len(rows), "sha lines", len(lines))


if __name__ == "__main__":
    main()
