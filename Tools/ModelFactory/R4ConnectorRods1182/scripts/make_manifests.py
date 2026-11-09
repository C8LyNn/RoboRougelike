"""#1182: writes per-part connector_manifest.json + model_manifest.json, the 24-rotation / mirror check, INDEX.json / INDEX.md
and SHA256SUMS.txt of the R4ConnectorRods1182 library from the real *_QA.json reports and compat/compat_all.json (no Blender needed).
usage: python make_manifests.py [library root]"""
import glob, hashlib, itertools, json, os, sys

ROOT = os.path.abspath(sys.argv[1] if len(sys.argv) > 1 else os.path.join(os.path.dirname(__file__), ".."))
G, H, GAP = 0.55, 0.26, 0.030
VERSION = "1182-v1"
DIRS_U = {"right": (1, 0, 0), "left": (-1, 0, 0), "up": (0, 1, 0), "down": (0, -1, 0), "forward": (0, 0, 1), "back": (0, 0, -1)}
NAME = {v: k for k, v in DIRS_U.items()}
BIT = {"right": 0, "left": 1, "up": 2, "down": 3, "forward": 4, "back": 5}          # R3MountingGrid bit order
PILOT = ["ROD_SHORT", "ROD_LONG", "ROD_DIAG_2D_LONG", "STRUT_LONG", "STRUT_ARC"]
ORDER = ["ROD_SHORT", "ROD_STANDARD", "ROD_LONG", "ROD_ARC_SHORT", "ROD_ARC_LONG", "ROD_DIAG_2D_SHORT", "ROD_DIAG_2D_LONG", "ROD_DIAG_3D",
         "ROD_CROSS", "ROD_PLUS", "ROD_FRAME", "ROD_T", "STRUT_SHORT", "STRUT_STANDARD", "STRUT_LONG", "STRUT_ARC", "STRUT_DIAG_2D",
         "STRUT_DIAG_3D", "STRUT_CROSS", "STRUT_RAMP"]
RULES = {
    "rod_mini": {"allow": ["R4B block full port", "R4B block partial port", "R4C r4_full port", "R4C rod_mini port"],
                 "deny": ["weapon / movement / wheel module socket (needs a full 0.509 m R4 port)"]},
    "r4_full": {"allow": ["R4B block full port", "R4B block partial port", "R4C r4_full port", "R4C rod_mini port", "weapon / movement / wheel module socket"],
                "deny": []},
}


def sha(p): return hashlib.sha256(open(p, "rb").read()).hexdigest()


def rotations():
    """the 24 proper integer rotations (signed permutation matrices, det +1)"""
    out = []
    for perm in itertools.permutations(range(3)):
        for s in itertools.product((1, -1), repeat=3):
            R = [[0]*3 for _ in range(3)]
            for i in range(3): R[i][perm[i]] = s[i]
            det = (R[0][0]*(R[1][1]*R[2][2] - R[1][2]*R[2][1]) - R[0][1]*(R[1][0]*R[2][2] - R[1][2]*R[2][0]) + R[0][2]*(R[1][0]*R[2][1] - R[1][1]*R[2][0]))
            if det == 1: out.append(R)
    return out
ROTS = rotations(); MIRROR = [[-1, 0, 0], [0, 1, 0], [0, 0, 1]]
def mul(R, v): return tuple(sum(R[i][j]*v[j] for j in range(3)) for i in range(3))
def mm(A, B): return [[sum(A[i][k]*B[k][j] for k in range(3)) for j in range(3)] for i in range(3)]


def norm_shape(cells, ports):
    lo = [min(c[i] for c in cells) for i in range(3)]
    sub = lambda c: tuple(c[i] - lo[i] for i in range(3))
    return (frozenset(sub(c) for c in cells), frozenset((sub(c), d) for c, d in ports))


def rotation_check(q):
    cells = [tuple(c["cell"]) for c in q["occupied_cells_unity"]]; ports = [(tuple(p["cell"]), p["dir"]) for p in q["ports"]]
    base = norm_shape(cells, ports); shapes = set(); fails = []
    for mir in (False, True):
        for k, R0 in enumerate(ROTS):
            R = mm(MIRROR, R0) if mir else R0
            C = [mul(R, c) for c in cells]; P = [(mul(R, c), NAME[mul(R, DIRS_U[d])]) for c, d in ports]
            occ = set(C); ok = len(occ) == len(cells) and len(set(P)) == len(P)
            for c, d in P:
                nb = tuple(c[i] + DIRS_U[d][i] for i in range(3))
                if c not in occ or nb in occ: ok = False
            if not ok: fails.append({"mirror": mir, "rotation_index": k})
            s = norm_shape(C, P)
            if not mir: shapes.add(s)
            elif k == 0: mirrored = s
    return {"transforms_tested": 48, "rotations": 24, "mirror": "scale X -1 then each of the 24 rotations",
            "checks": ["cells stay unique", "every port stays on an occupied cell", "every port faces a free cell", "no duplicate port faces"],
            "failures": fails, "PASS": not fails, "distinct_orientations": len(shapes),
            "mirror_equals_a_rotation": mirrored in shapes,
            "mirror_note": "mirrored footprint + ports equal a rotated copy: no separate mirrored model needed" if mirrored in shapes else
                           "mirrored footprint is NOT reachable by rotation: the game must allow a mirrored instance (scale X -1, ports left<->right) or ship a mirrored variant"}


def proposal(q):
    L = sum(m["length_m"] for m in q["members"]); n = len(q["nodes"]); rod = q["family"] == "rod"
    mass = (1.5*n + 2.5*L) if rod else (12.0*n + 18.0*L)
    return {"status": "PROPOSAL_ONLY", "note": "numbers are art-side suggestions for balance; final values are a Codex / design decision (#1153, #141)",
            "mass_kg": round(mass, 1), "hp_relative_to_R4B_Cube": round((0.12*n + 0.08*L/G) if rod else (0.9*n + 0.35*L/G), 2),
            "cost_relative_to_R4B_Cube": round((0.3*n + 0.15*L/G) if rod else (1.0*n + 0.4*L/G), 2), "basis": {"nodes": n, "member_length_m": round(L, 3)}}


def main():
    cmx = json.load(open(os.path.join(ROOT, "compat", "compat_all.json"), encoding="utf-8"))
    asm_p = os.path.join(ROOT, "assemblies", "assemblies_summary.json"); asm = json.load(open(asm_p, encoding="utf-8")) if os.path.exists(asm_p) else None
    rows = []
    for pid in ORDER:
        mid = "R4C_" + pid; d = os.path.join(ROOT, mid)
        q = json.load(open(os.path.join(d, mid + "_QA.json"), encoding="utf-8"))
        cells = q["occupied_cells_unity"]; cc = [c["cell"] for c in cells]
        rc = rotation_check(q); cx = cmx["connectors"].get(mid, {})
        files = {f: {"sha256": sha(os.path.join(d, f)), "bytes": os.path.getsize(os.path.join(d, f))}
                 for f in sorted(os.listdir(d)) if os.path.isfile(os.path.join(d, f)) and not f.endswith(".json")}
        ports = []
        for p in q["ports"]:
            ports.append({"id": p["id"], "node": p["node"], "cell": p["cell"], "face": p["dir"], "normal": p["normal"], "position_m": p["position_m"],
                          "type": p["kind"], "patch_half_cells": p["half"], "contact_mask_bit": BIT[p["dir"]],
                          "allow": RULES[p["kind"]]["allow"], "deny": RULES[p["kind"]]["deny"], "gap_to_partner_m": GAP})
        cm = {"part_id": pid, "id": mid, "family": q["family"], "version": VERSION, "description": q["description"], "pilot": pid in PILOT,
              "grid": {"cell_m": G, "block_m": 2*H, "face_offset_m": H, "gap_between_neighbours_m": GAP, "unity_axes": "+X right, +Y up, +Z forward",
                       "blender_axes": "Blender (x, y, z) = Unity (x, -z, y)", "standard": "measured from the real R4 GLBs (compat/compat_all.json standard_measured)"},
              "pivot": "model origin = centre of cell (0,0,0); all cells / ports in Unity cells relative to it",
              "bbox_cells": [[min(c[i] for c in cc) for i in range(3)], [max(c[i] for c in cc) for i in range(3)]], "bounds_m": q["bounds_unity_m"],
              "occupied_cells": cells,
              "occupied_cells_note": "explicit list; role node = cell with a hub/node, span = member passes through, crossing = member clips the cell (thickness) - every listed cell is reserved",
              "nodes": q["nodes"], "members": q["members"],
              "members_note": "members carry NO ports: nothing connects along a rod / beam (no automatic ports)",
              "connection_ports": ports, "sockets": {k: next(p for p in ports if p["id"] == v) for k, v in q["sockets"].items()},
              "contact_mask_anchor": sum(1 << BIT[p["dir"]] for p in q["ports"] if p["cell"] == [0, 0, 0]),
              "connection_rule": "two parts are connected ONLY through a pair of declared ports on opposite faces of neighbouring cells; physical adjacency without such a pair is NOT a connection",
              "rotations": {"allowed": "all 24 grid rotations", "mirror": "allowed (scale X -1)", "check": rc},
              "geometry": {"visual": mid + ".glb / .fbx", "collision_proposal": mid + "_colliders.glb (UCX convex pieces: node boxes + member segments)",
                           "visual_vs_collision": "visual mesh is detailed (port pattern, bolts); colliders are coarse convex pieces inside the occupied cells"},
              "damage_routing": {"internal_graph": [{"from": m["from_node"], "to": m["to_node"], "via": m["id"]} for m in q["members"]],
                                 "external": "only via connection_ports", "rules": "PENDING_CODEX_DESIGN (#1153)"},
              "proposal": proposal(q),
              "compat": cx.get("summary"), "compat_PASS": cx.get("PASS"), "status": q["status"], "unity": "UNITY_NOT_RUN"}
        json.dump(cm, open(os.path.join(d, mid + "_connector_manifest.json"), "w", encoding="utf-8"), indent=1, ensure_ascii=False)
        mmf = {"id": mid, "version": VERSION, "files": files, "units": "metres", "blender": q.get("blender"),
               "export": {"fbx": "Unity 6 URP, -Z forward, Y up, FBX_SCALE_UNITS, bake_space_transform=False", "glb": "glTF 2.0 binary, +Y up"},
               "triangles": q["mesh"]["triangles"], "triangle_budget": q["triangle_budget"], "materials": q["materials"],
               "palette": "R41 locked palette (same as R4 blocks / weapons / movement)", "mesh_check": q["mesh"], "reimport": q["reimport"],
               "port_contact_test": q["port_contact_test"], "renders": q["renders"], "generator": "scripts/build_connectors.py",
               "status": q["status"], "failures": q["failures"], "unity": "UNITY_NOT_RUN"}
        json.dump(mmf, open(os.path.join(d, mid + "_model_manifest.json"), "w", encoding="utf-8"), indent=1, ensure_ascii=False)
        rows.append({"id": mid, "family": q["family"], "pilot": pid in PILOT, "description": q["description"], "cells": len(cc),
                     "bbox_cells": cm["bbox_cells"], "nodes": len(q["nodes"]), "ports": len(ports),
                     "port_types": sorted({p["type"] for p in ports}), "triangles": q["mesh"]["triangles"], "blender_qa": q["status"],
                     "rotation_check": rc["PASS"], "distinct_orientations": rc["distinct_orientations"], "mirror_equals_rotation": rc["mirror_equals_a_rotation"],
                     "compat": cx.get("summary"), "compat_PASS": cx.get("PASS"), "unity": "UNITY_NOT_RUN"})
    idx = {"library": "Tools/ModelFactory/R4ConnectorRods1182", "issue": "#1182", "version": VERSION, "unity_status": "UNITY_NOT_RUN for all parts",
           "standard_measured": cmx.get("standard_measured"), "compat_PASS": cmx.get("PASS"), "assemblies": asm, "parts": rows}
    json.dump(idx, open(os.path.join(ROOT, "INDEX.json"), "w", encoding="utf-8"), indent=1, ensure_ascii=False)
    md = ["# R4ConnectorRods1182 — indeks", "", "| Model | Rodzina | Pilot | Komórki (bbox) | Węzły | Porty | Tri | Obroty 24 + lustro | Zgodność | QA | Unity |",
          "|---|---|---|---|---|---|---|---|---|---|---|"]
    for r in rows:
        bb = "×".join(str(r["bbox_cells"][1][i] - r["bbox_cells"][0][i] + 1) for i in range(3))
        s = r["compat"] or {}
        md.append(f"| `{r['id']}` | {r['family']} | {'tak' if r['pilot'] else ''} | {r['cells']} ({bb}) | {r['nodes']} | {r['ports']} {'/'.join(r['port_types'])} | {r['triangles']} | "
                  f"{'PASS' if r['rotation_check'] else 'FAIL'} ({r['distinct_orientations']} orient.) | {s.get('mountings', '-')} montaży, styk fail {s.get('contact_fail', '-')} | {r['blender_qa']} | {r['unity']} |")
    open(os.path.join(ROOT, "INDEX.md"), "w", encoding="utf-8").write("\n".join(md) + "\n")
    lines = [sha(p) + "  " + os.path.relpath(p, ROOT).replace(os.sep, "/") for p in sorted(glob.glob(os.path.join(ROOT, "**", "*"), recursive=True))
             if os.path.isfile(p) and os.path.basename(p) != "SHA256SUMS.txt" and "/preview/" not in p.replace(os.sep, "/")]
    open(os.path.join(ROOT, "SHA256SUMS.txt"), "w").write("\n".join(lines) + "\n")
    print("parts", len(rows), "rot PASS", sum(r["rotation_check"] for r in rows), "sums", len(lines))


if __name__ == "__main__":
    main()
