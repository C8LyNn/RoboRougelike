"""Writes INDEX.json and SHA256SUMS.txt for the R4Blocks library from the real files and *_QA.json reports."""
import glob, hashlib, json, os, sys

ROOT = os.path.abspath(sys.argv[1] if len(sys.argv) > 1 else os.path.join(os.path.dirname(__file__), ".."))
ORDER = ["Cube", "Wedge", "Tetra", "InnerCorner", "CornerWedge", "RoundEdge", "RoundCorner", "HalfBlock",
         "HalfWedge", "Cylinder", "Beam2", "Beam3", "LongWedge", "Plate2x2", "TrussCube"]


def sha(p):
    return hashlib.sha256(open(p, "rb").read()).hexdigest()


def main():
    rows = []
    for k in ORDER:
        mid = "R4B_" + k; d = os.path.join(ROOT, mid); q = json.load(open(os.path.join(d, mid + "_QA.json"), encoding="utf-8"))
        files = [mid + ".blend", mid + ".fbx", mid + ".glb", mid + "_colliders.glb"]
        rows.append(dict(
            id=mid, block_kind=q["block_kind"], description=q["description"], status=q["status"], failures=q["failures"],
            unity="UNITY_NOT_RUN", cells=q["cells"], bonds=q["bonds"], contact_mask_local_anchor=q["contact_mask_local_anchor"],
            contact_mask_bits=q["contact_mask_bits"],
            ports=[{k2: p[k2] for k2 in ("cell", "dir", "kind", "offset", "half")} for p in q["ports"]],
            port_contact_PASS=all(t["PASS"] for t in q["port_contact_test"]), triangles=q["mesh"]["triangles"],
            topology_PASS=q["mesh"]["ok"], inside_cells=q["inside_cells"],
            fbx_reimport_PASS=q["reimport"][".fbx"]["PASS"], glb_reimport_PASS=q["reimport"][".glb"]["PASS"],
            model_file=f"{mid}/{mid}.fbx", glb=f"{mid}/{mid}.glb", blend=f"{mid}/{mid}.blend",
            sha256={f: sha(os.path.join(d, f)) for f in files}, note=q.get("note")))
    cj = json.load(open(os.path.join(ROOT, "compat", "compat_assembly.json"), encoding="utf-8"))
    idx = {"library": "Tools/ModelFactory/R4Blocks", "standard": "R41_Turret_v6.1 palette + R4 Robocraft-style port (same as PR #939 / #941)",
           "grid": {"cell_m": 0.55, "block_face_m": 0.26, "block_size_m": 0.52, "port_plate_m": 0.509,
                    "contact_mask_order": "R3MountingGrid.FaceDirections: right, left, up, down, forward, back (bit 0..5, Unity axes)"},
           "blender": "5.2.3 LTS", "export": {"fbx": "Unity 6 URP, -Z forward, Y up, FBX_SCALE_UNITS, bake_space_transform=False", "glb": "glTF 2.0 binary"},
           "unity_status": "UNITY_NOT_RUN for all blocks (no Unity in this session)",
           "compat_test": {"PASS": cj["PASS"], "port_pairs": cj["n_pairs"], "overlaps": len(cj["overlaps"]), "modules_on_ports": cj["modules_on_ports"],
                           "note": cj["note"], "file": "compat/compat_assembly.json"},
           "blocks": rows}
    json.dump(idx, open(os.path.join(ROOT, "INDEX.json"), "w", encoding="utf-8"), indent=1, ensure_ascii=False)
    lines = []
    for p in sorted(glob.glob(os.path.join(ROOT, "**", "*"), recursive=True)):
        if os.path.isfile(p) and os.path.basename(p) != "SHA256SUMS.txt":
            lines.append(sha(p) + "  " + os.path.relpath(p, ROOT).replace(os.sep, "/"))
    open(os.path.join(ROOT, "SHA256SUMS.txt"), "w").write("\n".join(lines) + "\n")
    print("rows", len(rows), "sums", len(lines))


if __name__ == "__main__":
    main()
