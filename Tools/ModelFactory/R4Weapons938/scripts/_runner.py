
import subprocess, json, sys
def run_weapon(nm, script):
    W = r"C:\Users\kubap\Desktop\Blender_Modele\wynik\R4_Weapons_938\scripts"
    OUTD = r"C:\Users\kubap\Desktop\Blender_Modele\wynik\R4_Weapons_938" + "\\" + nm
    B = r"C:\Program Files (x86)\Steam\steamapps\common\Blender\blender.exe"
    r = subprocess.run([B, "-b", "--factory-startup", "--python", W + "\\" + script, "--", OUTD], capture_output=True, text=True, timeout=900)
    try:
        d = json.load(open(OUTD + "\\" + nm + "_QA.json", encoding="utf-8"))
    except Exception as ex:
        return {"ex": str(ex), "err": r.stderr[-1800:], "out": r.stdout[-600:]}
    return {"status": d["status"], "fail": d["failures"], "err": (d.get("error") or "")[-1200:], "tris": d.get("triangles_total"),
            "bad": [(m["object"], m["non_manifold_edges"], m["boundary_edges"], m["zero_area_faces"], m["inconsistent_normals"], m["nonmanifold_samples_blender"]) for m in d.get("meshes", []) + d.get("fx_meshes", []) if not m["ok"]],
            "pal": d.get("palette", {}).get("family_share"), "pal_ok": d.get("palette", {}).get("PASS"), "aim": (d.get("sweep") or {}).get("aim_sweep"),
            "clash": (d.get("animation_clash") or {}).get("samples"), "yawR": (d.get("sweep") or {}).get("yaw_body_max_radius_m"),
            "lof": [(x["muzzle"], x["elevation"], x["own_hit_within_6m"]) for x in (d.get("sweep") or {}).get("line_of_fire", []) if not x["exit_clear"]],
            "extra": d.get("extra_checks"), "fbx": (d.get("fbx_reimport") or {}).get("PASS"), "glb": (d.get("glb_reimport") or {}).get("PASS"), "sec": d.get("seconds")}
