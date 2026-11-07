"""Rebuilds INDEX.json and SHA256SUMS.txt of the R4Weapons938 library from the real files and *_QA.json reports."""
import glob, hashlib, json, os, sys

ROOT = os.path.abspath(sys.argv[1] if len(sys.argv) > 1 else os.path.join(os.path.dirname(__file__), ".."))
ORDER = ["R4W_Basic", "R4W_RotarySaw", "R4W_Heavy", "R4W_Shotgun", "R4W_Railgun", "R4W_BeamCutter", "R4W_RocketLauncher", "R4W_EmpEmitter"]
STYLE = {  # (version, base style, head) - v1 = unchanged first commit of PR #939
    "R4W_Basic": ("v1+port", "r41", "R41 cheek yoke (reference look, unchanged)"),
    "R4W_RotarySaw": ("v2+port", "crab", "v1 saw arm"), "R4W_Heavy": ("v2+port", "stepped", "low sponsons + drum mantlet + wide gun shield + stubby barrel"),
    "R4W_Shotgun": ("v2+port", "box", "single cantilever arm + side drum magazine"), "R4W_Railgun": ("v2+port", "column", "thin tall fork + capacitor spires"),
    "R4W_BeamCutter": ("v2+port", "finned", "ball head in a gimbal ring"), "R4W_RocketLauncher": ("v2+port", "tripod", "radar tower + twin side pods"),
    "R4W_EmpEmitter": ("v2+port", "drum", "v1 pulse head"),
}
EXTRA_NOTES = {"R4W_Heavy": ["v2: visual Muzzle moved from 0.700 m to 0.600 m forward of the pivot (shorter howitzer barrel). Runtime placeholder positions are not changed."],
               "R4W_RocketLauncher": ["v2: 4 tubes in 2 side pods (x +-0.130 / +-0.200); logical Muzzle stays on the centre line between the pods; second rear cover added (rest pose in all clips)."]}


def sha(p):
    return hashlib.sha256(open(p, "rb").read()).hexdigest()


def main():
    old = json.load(open(os.path.join(ROOT, "INDEX.json"), encoding="utf-8"))
    silp = os.path.join(ROOT, "compare", "silhouette_check.json")
    sil = json.load(open(silp, encoding="utf-8")) if os.path.exists(silp) else None
    gp = os.path.join(ROOT, "compare", "grid_fit.json"); grid = json.load(open(gp))["after"] if os.path.exists(gp) else {}
    rows = []
    for n in ORDER:
        d = os.path.join(ROOT, n); q = json.load(open(os.path.join(d, n + "_QA.json"), encoding="utf-8"))
        am = json.load(open(os.path.join(d, n + "_animation_manifest.json"), encoding="utf-8"))
        sw = q["sweep"]; ver, base_style, head = STYLE[n]
        files = [n + ".blend", n + ".fbx", n + ".glb", n + "_colliders.glb", n + "_LOD1.fbx"]
        closest = None
        if sil:
            cand = [(k, v["mean"]) for k, v in sil[sil.get("current", "v2")]["pairs"].items() if n in k.split("|")]
            k, m = max(cand, key=lambda t: t[1]); closest = {"pair": k.replace("R4W_", ""), "mean_iou": m}
        rows.append(dict(
            id=n, weapon_kind=q["kind"], game_name=q["game_name"], version=ver, base_style=base_style, head=head,
            status=q["status"], failures=q["failures"], model_file=f"{n}/{n}.fbx", glb=f"{n}/{n}.glb", blend=f"{n}/{n}.blend",
            sha256={f: sha(os.path.join(d, f)) for f in files},
            clips=[[c["name"], c["start"], c["end"]] for c in am["clips"]],
            materials_4_palette=q["palette"]["family_share"], palette_in_band=q["palette"]["PASS"],
            triangles=q["triangles_total"], triangles_fx=q["triangles_fx"], lod1_triangles=q["lod1"]["triangles"],
            topology_PASS=q["topology_PASS"], aim_sweep_collisions=sw["aim_sweep"]["collisions"],
            animation_clash=q["animation_clash"]["collisions"], animation_frames_checked=q["animation_clash"]["frames_checked"],
            static_inside_cell=sw["static_occupied"]["inside_own_cell"], yaw_body_radius_m=sw["yaw_body_max_radius_m"],
            moving_sweep_max_radius_m=sw["moving_sweep"]["max_radius_from_pivot_axis_m"], moving_sweep_bounds_file=sw["moving_sweep"]["bounds_file"],
            neighbour_cells_entered=len(sw["moving_sweep"]["neighbour_cells_entered"]), fire_exit_clear=sw["fire_exit_clear_all"],
            fbx_reimport_worst=q["fbx_reimport"]["worst"], fbx_PASS=q["fbx_reimport"]["PASS"], glb_PASS=q["glb_reimport"]["PASS"],
            r41_compare=q.get("r41_compare"), unity="UNITY_NOT_RUN (Unity not available in this session)", extra=q.get("extra_checks", {}),
            mount_port="R4 Robocraft-style port: 0.509 m square plate on the bottom face, outer face flush with the block face (-0.26 m), face-centre coupling; Socket_Mount_Bottom on that face",
            grid_fit_PASS=(grid.get(n) or {}).get("PASS"), grid_fit={k: (grid.get(n) or {}).get(k) for k in ("socket", "contact_patch", "static_bounds", "n_cross")},
            silhouette_closest=closest, notes=q.get("notes", []) + EXTRA_NOTES.get(n, [])))
    idx = {k: old[k] for k in ("issue", "canon", "reference", "blender")}
    idx["revision_port"] = ("Standard R4 Robocraft-style mounting port on all 8 bases (square 0.509 m plate flush with the block face, face-centre coupling); "
                            "independent GLB grid-fit check compare/grid_fit.json: before 3/8, after 8/8.")
    idx["revision"] = ("v2 silhouette diversity pass (owner feedback: the 8 weapons looked too similar). 7 weapons rebuilt with distinct base and head "
                       "architectures; Basic keeps the R41 look as the reference. Rig contract, pivot, socket, palette and clip lists unchanged.")
    if sil:
        idx["silhouette_check"] = {"method": sil["method"], "threshold": sil["threshold"],
                                   "v1": {"flagged": sil["v1"]["flagged"], "mean_of_all_pairs": sil["v1"]["mean_of_all_pairs"]},
                                   "v2": {"flagged": sil["v2"]["flagged"], "mean_of_all_pairs": sil["v2"]["mean_of_all_pairs"]},
                                   "current": sil.get("current", "v2"),
                                   "current_result": {"flagged": sil[sil.get("current", "v2")]["flagged"], "mean_of_all_pairs": sil[sil.get("current", "v2")]["mean_of_all_pairs"],
                                                      "most_similar": sil[sil.get("current", "v2")]["most_similar"][:3]}}
    idx["weapons"] = rows
    json.dump(idx, open(os.path.join(ROOT, "INDEX.json"), "w", encoding="utf-8"), indent=1, ensure_ascii=False)
    lines = []
    for p in sorted(glob.glob(os.path.join(ROOT, "**", "*"), recursive=True)):
        if os.path.isfile(p) and os.path.basename(p) != "SHA256SUMS.txt":
            lines.append(sha(p) + "  ./" + os.path.relpath(p, ROOT).replace(os.sep, "/"))
    open(os.path.join(ROOT, "SHA256SUMS.txt"), "w").write("\n".join(lines) + "\n")
    print("rows", len(rows), "sums", len(lines))


if __name__ == "__main__":
    main()
