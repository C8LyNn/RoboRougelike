"""#1232 pilot - compatibility tests on the REAL exported GLBs (Blender-exported new blocks vs the approved models).
Pure python3 + numpy + scipy, no Blender / Unity. Writes tests/compat_report.json and tests/COMPAT_SUMMARY.md.
  S1 interface      empties (ports / sockets / root) identical to the approved PR #963 GLB
  S2 containment    new geometry stays inside the approved cell solid and inside the approved collider hull
  S3 contact patch  approved patch_test re-run on the exported GLB triangles (cross-check of the Blender QA)
  S4 rotations      24 grid rotations: geometry stays in its cell, ports land on grid directions, yaw ContactMask table
  S5 module matrix  every real weapon / movement / wheel / connector on every FULL port, 4 spins: 30 mm gap, flat contact,
                    penetration; identical test on the approved block -> regression flag
  S6 robots         the two comparison robots: cell conflicts, port graph, real-module clearance
  S7 budgets        triangles / vertices / materials, robot totals
usage: python3 tests_compat.py
"""
import os, re, json, itertools, time, hashlib
import numpy as np
from scipy.spatial import ConvexHull
import glbio, paths, raycast, layout, shapes

G, H, GAP = 0.55, 0.26, 0.03
DIRECTIONS = {"A": "A_Sleek", "B": "B_Industrial"}
FX = re.compile(r"Flash|Glow|Beam_Core|Beam_Sheath|PulseRing|PhaseRing|BlastRing|Backblast|BlurDisc|Sparks|Flame")
DIR = layout.DIR; NAME = layout.NAME
UNITY_BIT = {"right": 0, "left": 1, "up": 2, "down": 3, "forward": 4, "back": 5}
F2B = lambda P: np.stack([P[..., 0], -P[..., 2], P[..., 1]], axis=-1)        # file (x, y, z) -> Blender (x, -z, y)
T0 = time.time()


def sha(p): return hashlib.sha256(open(p, "rb").read()).hexdigest()
def new_glb(key, kind, lod=False): return os.path.join(paths.PKG, DIRECTIONS[key], f"R4B_{kind}", f"R4B_{kind}" + ("_LOD1" if lod else "") + ".glb")
def old_glb(kind): return paths.ref_glb("blocks", f"R4B_{kind}")
def approved_qa(kind): return json.load(open(paths.ref_file("blocks", f"R4B_{kind}", f"R4B_{kind}_QA.json")))


def hull_planes(P, shrink=0.0):
    h = ConvexHull(P); eq = np.unique(np.round(h.equations, 7), axis=0)
    return eq[:, :3], -eq[:, 3] - shrink                                   # n.x <= d


def tri_hits_convex(T, N, D):
    """indices of triangles (T: M,3,3) that intersect the convex polytope {x: N x <= D}"""
    S = np.einsum("mvk,pk->mvp", T, N) - D[None, None, :]                  # signed distance of each vertex to each plane
    cand = ~np.any(np.all(S > 0, axis=1), axis=1)                          # not fully outside one plane
    inside = np.any(np.all(S <= 0, axis=2), axis=1)                        # a vertex inside
    hits = list(np.where(cand & inside)[0])
    for i in np.where(cand & ~inside)[0]:
        poly = [p for p in T[i]]
        for n, d in zip(N, D):
            out = []
            for k in range(len(poly)):
                p0, p1 = poly[k], poly[(k + 1) % len(poly)]; s0, s1 = p0 @ n - d, p1 @ n - d
                if s0 <= 0: out.append(p0)
                if (s0 < 0 < s1) or (s1 < 0 < s0): out.append(p0 + (p1 - p0)*(s0/(s0 - s1)))
            poly = out
            if len(poly) < 3: break
        if len(poly) >= 3:
            Q = np.array(poly); a = np.linalg.norm(sum(np.cross(Q[k] - Q[0], Q[(k + 1) % len(Q)] - Q[0]) for k in range(len(Q))))/2
            if a > 1e-10: hits.append(i)
    return hits


class Mod:
    """a real approved module GLB (rest pose, FX meshes removed)"""
    cache = {}
    def __init__(self, model):
        lib, name = model.split(":"); self.model = model; self.lib = lib; self.name = name; self.path = paths.ref_glb(lib, name)
        M = glbio.load(self.path, skip_name=lambda n: bool(FX.search(n)))
        self.tris = np.concatenate([p["pos"][p["idx"]] for p in M.prims if p["material"] != "Muzzle_Flash"])
        self.tri_count_full = glbio.load(self.path).tri_count()
        sk = [n for n in M.empties if n in ("Socket_Mount_Bottom", "Socket_Mount", "Socket_Start")]
        self.socket_name = sk[0]; ps = M.empties[sk[0]][0][:3, 3]
        self.sock_cell = np.round(ps/G).astype(int); m = (ps - self.sock_cell*G)/H
        self.mount = np.round(m).astype(int); self.sock_ok = bool(np.allclose(m, self.mount, atol=1e-3) and abs(self.mount).sum() == 1)
        self.patch_half = 0.2091 if "R4C_ROD" in name else 0.47
    @classmethod
    def get(cls, model):
        if model not in cls.cache: cls.cache[model] = cls(model)
        return cls.cache[model]


def all_modules():
    out = []
    for lib in ("weapons", "movement", "wheels", "connectors"):
        base = os.path.join(paths.REF, lib, paths.LIB[lib])
        for d in sorted(os.listdir(base)):
            if os.path.isfile(os.path.join(base, d, d + ".glb")): out.append(f"{lib}:{d}")
    return out


def grid_pts(n, half, N=13):
    """N x N sample grid on the mid-gap plane of a port with outward direction n (file axes), origin = block cell centre"""
    n = np.array(n, float); u = np.array([n[1], n[2], n[0]]); v = np.cross(n, u)
    a = (np.linspace(-1, 1, N)*half*0.85*G)
    return np.array([n*(H + GAP/2) + u*x + v*y for x in a for y in a])


def side_profile(tris, n, half, toward):
    """rays from the mid-gap plane along `toward` (+-n): returns flush %, protruding count (inside the gap), missing"""
    O = grid_pts(n, half); n = np.array(n, float)
    t = raycast.ray_hits(O - toward*1e-6, toward, tris, 0.6)
    k = GAP/2 - t                                                            # > 0: surface enters the gap, < 0: recessed
    return {"flush_pct": round(100*float(((k <= 0.0005) & (k > -0.0010)).mean()), 1), "protruding": int((k > 0.0005).sum()), "missing": int(np.isinf(t).sum())}


def mount_rots(mod, d):
    d = np.array(d); return [M for M in layout.ROTS if np.array_equal(M @ mod.mount, -d)]


R = {"issue": 1232, "generated_by": "scripts/tests_compat.py", "unity": "UNITY_NOT_RUN",
     "reference_commits": paths.REF_SHA, "sections": {}}
fails = []

# ---------------------------------------------------------------------------------------------- S1 / S2 / S3 / S4 / S7 per block
blocks = {}; NEW = {}; OLD = {}
for kind in shapes.PILOT:
    qa = approved_qa(kind); Mo = glbio.load(old_glb(kind)); OLD[kind] = Mo
    solid = np.array(shapes.SOLIDS[kind]); solid_f = np.stack([solid[:, 0], solid[:, 2], -solid[:, 1]], axis=1)      # Blender -> file
    SN, SD = hull_planes(solid_f)
    Mc = glbio.load(paths.ref_glb("blocks", f"R4B_{kind}", "_colliders")); CN, CD = hull_planes(Mc.all_pos())
    for key in DIRECTIONS:
        Mn = glbio.load(new_glb(key, kind)); Ml = glbio.load(new_glb(key, kind, True)); NEW[(key, kind)] = Mn
        b = {"glb": os.path.relpath(new_glb(key, kind), paths.PKG).replace("\\", "/"), "sha256": sha(new_glb(key, kind))}
        # S1
        eo, en = Mo.empties, Mn.empties; pos = ax = 0.0
        for n_ in eo:
            if n_ in en:
                pos = max(pos, float(np.linalg.norm(eo[n_][0][:3, 3] - en[n_][0][:3, 3]))*1000)
                ax = max(ax, float(np.degrees(np.arccos(np.clip(eo[n_][0][:3, 2] @ en[n_][0][:3, 2], -1, 1)))))
        b["S1_interface"] = {"approved_empties": sorted(eo), "missing": sorted(set(eo) - set(en)), "extra": sorted(set(en) - set(eo)), "pos_dev_mm": round(pos, 4), "axis_dev_deg": round(ax, 4)}
        b["S1_interface"]["PASS"] = not b["S1_interface"]["missing"] and not b["S1_interface"]["extra"] and pos < 0.01 and ax < 0.05
        # S2
        P = Mn.all_pos(); PL = Ml.all_pos()
        b["S2_containment"] = {"max_outside_cell_solid_mm": round(float((P @ SN.T - SD).max())*1000, 4), "max_outside_approved_collider_hull_mm": round(float((P @ CN.T - CD).max())*1000, 4),
                               "lod1_max_outside_cell_solid_mm": round(float((PL @ SN.T - SD).max())*1000, 4),
                               "bounds_m": [np.round(P.min(0), 4).tolist(), np.round(P.max(0), 4).tolist()], "approved_bounds_m": [np.round(Mo.all_pos().min(0), 4).tolist(), np.round(Mo.all_pos().max(0), 4).tolist()]}
        b["S2_containment"]["PASS"] = max(b["S2_containment"]["max_outside_cell_solid_mm"], b["S2_containment"]["max_outside_approved_collider_hull_mm"], b["S2_containment"]["lod1_max_outside_cell_solid_mm"]) <= 0.05
        # S3
        Tb = F2B(Mn.tris()); To = F2B(Mo.tris()); pt = []
        for p in qa["ports"]:
            r = raycast.patch_test(Tb, p["cell"], p["dir"], p["offset"], p["half"]); ro = raycast.patch_test(To, p["cell"], p["dir"], p["offset"], p["half"])
            r.update({"dir": p["dir"], "kind": p["kind"], "approved_flush_pct_recomputed": ro["flush_pct"],
                      "approved_flush_pct_recorded": next(q["flush_pct"] for q in qa["port_contact_test"] if q["dir"] == p["dir"])})
            r["PASS"] = r["protruding"] == 0 and r["missing"] == 0 and r["flush_pct"] >= (80 if p["kind"] == "full" else 65); pt.append(r)
        b["S3_contact_patch"] = {"ports": pt, "PASS": all(r["PASS"] for r in pt)}
        # S4
        rot_ok = True; yaw = {}
        sig = layout.SIG[kind]
        for Mx in layout.ROTS:
            Q = P @ Mx.T; rot_ok &= bool(np.abs(Q).max() <= H + 1e-6)
            rot_ok &= all(tuple(int(x) for x in Mx @ np.array(DIR[d])) in NAME for d in sig)
        for k in range(4):
            s = layout.rsig(kind, layout.rot_y(k)); yaw[str(k*90)] = {"contact_mask": sum(1 << UNITY_BIT[d] for d in s), "full": sorted(d for d in s if s[d] == "full"), "partial": sorted(d for d in s if s[d] == "part")}
        distinct = len({tuple(sorted(layout.rsig(kind, Mx).items())) for Mx in layout.ROTS})
        b["S4_rotations"] = {"rotations_tested": 24, "geometry_stays_in_cell": rot_ok, "distinct_port_layouts": distinct, "yaw_0_90_180_270": yaw,
                             "canonical_contact_mask": sum(1 << UNITY_BIT[d] for d in sig), "approved_contact_mask": qa["contact_mask_local_anchor"],
                             "mirror": "mirror image of the solid equals one of the 24 rotations (cube / half / wedge symmetric; tetra + corner wedge have a diagonal mirror plane)"}
        b["S4_rotations"]["PASS"] = rot_ok and b["S4_rotations"]["canonical_contact_mask"] == qa["contact_mask_local_anchor"]
        # S7
        mats = {}
        for pr in Mn.prims: mats[pr["material"]] = mats.get(pr["material"], 0) + len(pr["idx"])
        b["S7_budget"] = {"triangles": Mn.tri_count(), "lod1_triangles": Ml.tri_count(), "approved_triangles": Mo.tri_count(), "triangle_ratio_vs_approved": round(Mn.tri_count()/Mo.tri_count(), 3),
                          "unique_positions": int(len(np.unique(np.round(P, 6), axis=0))), "render_vertices": int(len(P)), "materials": mats, "approved_materials": sorted({pr["material"] for pr in Mo.prims}),
                          "glb_bytes": os.path.getsize(new_glb(key, kind)), "approved_glb_bytes": os.path.getsize(old_glb(kind))}
        for sec in ("S1_interface", "S2_containment", "S3_contact_patch", "S4_rotations"):
            if not b[sec]["PASS"]: fails.append(f"{key}/{kind}/{sec}")
        blocks[f"{DIRECTIONS[key]}/R4B_{kind}"] = b
R["sections"]["blocks"] = blocks
print("blocks done", round(time.time() - T0, 1), "s", flush=True)

# ---------------------------------------------------------------------------------------------- S5 module matrix
mods = [Mod.get(m) for m in all_modules()]
variants = {"approved": {k: OLD[k] for k in shapes.PILOT}, "A": {k: NEW[("A", k)] for k in shapes.PILOT}, "B": {k: NEW[("B", k)] for k in shapes.PILOT}}
hulls = {v: {k: hull_planes(M.all_pos(), 0.0005) for k, M in d.items()} for v, d in variants.items()}
mod_side = {}
for m in mods:                                                               # module mounting face profile (independent of the block)
    if not m.sock_ok: continue
    n = -m.mount.astype(float)                                               # in module space the block lies along +mount; gap plane on the module's own face
    tr = m.tris - m.sock_cell*G
    O = grid_pts(m.mount, m.patch_half); t = raycast.ray_hits(O, -m.mount.astype(float), tr, 0.6); k = GAP/2 - t
    mod_side[m.model] = {"flush_pct": round(100*float(((k <= 0.0005) & (k > -0.0010)).mean()), 1), "protruding": int((k > 0.0005).sum()), "missing": int(np.isinf(t).sum())}
S5 = {"modules": len(mods), "module_face": mod_side, "skipped_modules": [m.model for m in mods if not m.sock_ok], "ports": [], "totals": {}}
tot = {v: {"mounts": 0, "contact_fail": 0, "spin_blocked": 0, "port_fully_blocked": 0} for v in variants}; regress = []
for kind in shapes.PILOT:
    for d, pk in layout.SIG[kind].items():
        if pk != "full": continue
        dv = np.array(DIR[d], float)
        prof = {v: {hk: side_profile(variants[v][kind].tris(), dv, half, -dv) for hk, half in (("0.47", 0.47), ("0.2091", 0.2091))} for v in variants}
        rec = {"block": kind, "port": d, "block_face": {v: prof[v]["0.47"] for v in variants}, "modules": {}}
        for m in mods:
            if not m.sock_ok: continue
            hk = "0.2091" if m.patch_half < 0.3 else "0.47"; res = {}
            for v in variants:
                N_, D_ = hulls[v][kind]; clear = []
                for Mx in mount_rots(m, DIR[d]):
                    T = (m.tris - m.sock_cell*G) @ Mx.T + np.array(DIR[d], float)*G
                    near = np.all(T.min(1) <= H + 0.002, axis=1) & np.all(T.max(1) >= -H - 0.002, axis=1)
                    clear.append(len(tri_hits_convex(T[near], N_, D_)) == 0 if near.any() else True)
                bf = prof[v][hk]; mf = mod_side[m.model]
                contact = bf["protruding"] == 0 and bf["missing"] == 0 and mf["protruding"] == 0 and bf["flush_pct"] >= (80 if hk == "0.47" else 65)   # same thresholds as #963 (full) / #1186 (rod_mini)
                res[v] = {"contact_ok": contact, "spins_clear": int(sum(clear))}
                tot[v]["mounts"] += 4; tot[v]["contact_fail"] += 0 if contact else 4; tot[v]["spin_blocked"] += 4 - int(sum(clear)); tot[v]["port_fully_blocked"] += int(sum(clear) == 0)
            for v in ("A", "B"):
                if res[v]["spins_clear"] < res["approved"]["spins_clear"] or (res["approved"]["contact_ok"] and not res[v]["contact_ok"]): regress.append([v, kind, d, m.model, res])
            rec["modules"][m.model] = res
        S5["ports"].append(rec)
    print("matrix", kind, round(time.time() - T0, 1), "s", flush=True)
S5["totals"] = tot; S5["regressions_vs_approved"] = regress; S5["PASS"] = not regress and tot["A"]["contact_fail"] == 0 and tot["B"]["contact_fail"] == 0
S5["note"] = ("rest pose of the approved module GLBs, FX meshes excluded; animation sweep not repeated here - S2 proves the new blocks lie inside the approved "
              "solid, so every clearance the approved Blender matrix (PR #963/#1186) established for any animation frame still holds")
if not S5["PASS"]: fails.append("S5_module_matrix")
R["sections"]["S5_module_matrix"] = S5

# ---------------------------------------------------------------------------------------------- S6 robots
def connector_ports(mod):
    man = json.load(open(paths.ref_file("connectors", mod.name, f"{mod.name}_connector_manifest.json")))
    return [(tuple(p["cell"]), p["face"], p["type"]) for p in man["connection_ports"]], [tuple(c["cell"]) for c in man["occupied_cells"]]

robots = {}
for rname, fn in layout.ROBOTS.items():
    Rb = fn(); ports = {}                                                    # (cell, dir) -> (part index, kind)
    cell_err = []
    for i, p in enumerate(Rb.parts):
        if p["type"] == "block":
            for d, k in layout.rsig(p["kind"], p["rot"]).items(): ports[(p["cell"], d)] = (i, "full" if k == "full" else "partial")
        elif p["type"] == "connector":
            m = Mod.get(p["model"]); cp, occ = connector_ports(m)
            cells = {tuple(int(x) for x in (p["rot"] @ np.array(c)) + np.array(p["cell"])) for c in occ}
            if cells != set(p["cells"]): cell_err.append([p["model"], sorted(cells), sorted(p["cells"])])
            for c, face, typ in cp:
                cc = tuple(int(x) for x in (p["rot"] @ np.array(c)) + np.array(p["cell"])); dd = NAME[tuple(int(x) for x in p["rot"] @ np.array(DIR[face]))]
                ports[(cc, dd)] = (i, typ)
        else:
            m = Mod.get(p["model"]); dd = NAME[tuple(int(x) for x in p["rot"] @ m.mount)]
            ports[(p["cell"], dd)] = (i, "module")
            if dd != p["mount"]: cell_err.append([p["model"], "mount direction", dd, p["mount"]])
    edges = set(); bad_mod = []
    for (c, d), (i, k) in ports.items():
        nb = (tuple(int(x) for x in np.array(c) + np.array(DIR[d])), layout.OPP[d])
        if nb in ports and ports[nb][0] != i:
            j, k2 = ports[nb]
            if "module" in (k, k2) and {k, k2} - {"module"} not in ({"full"}, {"r4_full"}): bad_mod.append([Rb.parts[i].get("model") or Rb.parts[i]["kind"], c, d, k, k2]); continue
            edges.add((min(i, j), max(i, j)))
    for i, p in enumerate(Rb.parts):
        if p["type"] in ("weapon", "wheel") and not any(i in e for e in edges): bad_mod.append([p["model"], p["cell"], "not on a full port"])
    comp = list(range(len(Rb.parts)))
    def find(x):
        while comp[x] != x: comp[x] = comp[comp[x]]; x = comp[x]
        return x
    for a, b_ in edges: comp[find(a)] = find(b_)
    ncomp = len({find(i) for i in range(len(Rb.parts))})
    # clearance of every real module against every block near it, for approved / A / B blocks
    clr = {v: 0 for v in variants}; pairs = 0; worst = []
    for i, p in enumerate(Rb.parts):
        if p["type"] == "block": continue
        m = Mod.get(p["model"]); T = m.tris @ p["rot"].T + np.array(p["cell"], float)*G; lo, hi = T.min((0, 1)), T.max((0, 1))
        for q in Rb.parts:
            if q["type"] != "block": continue
            c = np.array(q["cell"], float)*G
            if np.any(c + H < lo - 0.002) or np.any(c - H > hi + 0.002): continue
            pairs += 1; Tl = (T - c) @ q["rot"]                                 # into the block's local frame (rot is orthonormal)
            near = np.all(Tl.min(1) <= H + 0.002, axis=1) & np.all(Tl.max(1) >= -H - 0.002, axis=1)
            for v in variants:
                N_, D_ = hulls[v][q["kind"]]; h = tri_hits_convex(Tl[near], N_, D_) if near.any() else []
                if h: clr[v] += 1; worst.append([v, p["model"], list(p["cell"]), q["kind"], list(q["cell"]), len(h)])
    cnt = Rb.count(); nb = sum(v for k, v in cnt.items() if k in shapes.PILOT)
    tri = {v: int(sum((variants[v][p["kind"]].tri_count() if p["type"] == "block" else Mod.get(p["model"]).tri_count_full) for p in Rb.parts)) for v in variants}
    tri_blocks = {v: int(sum(variants[v][p["kind"]].tri_count() for p in Rb.parts if p["type"] == "block")) for v in variants}
    tri_blocks["A_LOD1"] = int(sum(glbio.load(new_glb("A", p["kind"], True)).tri_count() for p in Rb.parts if p["type"] == "block"))
    robots[rname] = {"parts": len(Rb.parts), "counts": cnt, "pilot_blocks": nb, "cells_occupied": len(Rb.cells), "cell_errors": cell_err, "port_pairs_connected": len(edges),
                     "connected_components": ncomp, "module_mount_errors": bad_mod, "module_block_pairs_checked": pairs, "penetrating_pairs": clr, "penetration_detail": worst[:20],
                     "triangles_total": tri, "triangles_blocks_only": tri_blocks,
                     "note": "port graph is computed from the port metadata, which is byte-identical for approved / A / B blocks -> identical logical connectivity",
                     "PASS": not cell_err and not bad_mod and ncomp == 1 and clr["A"] <= clr["approved"] and clr["B"] <= clr["approved"] and clr["A"] == 0 and clr["B"] == 0}
    if not robots[rname]["PASS"]: fails.append("S6_" + rname)
    json.dump({"name": rname, "axes": "file axes of the approved models: x = Port_right(+), y = up, z = Port_forward(+); cell = 0.55 m",
               "parts": [{"type": p["type"], "model": p.get("model") or "R4B_" + p["kind"], "cell": list(p["cell"]), "rot3x3": p["rot"].tolist(), "paint": p.get("paint")} for p in Rb.parts]},
              open(os.path.join(paths.PKG, "robots", rname + "_layout.json"), "w"), indent=0) if os.makedirs(os.path.join(paths.PKG, "robots"), exist_ok=True) is None else None
    print("robot", rname, round(time.time() - T0, 1), "s", flush=True)
R["sections"]["S6_robots"] = robots
R["status"] = "PASS" if not fails else "FAIL"; R["failures"] = fails; R["seconds"] = round(time.time() - T0, 1)
os.makedirs(os.path.join(paths.PKG, "tests"), exist_ok=True)
json.dump(R, open(os.path.join(paths.PKG, "tests", "compat_report.json"), "w"), indent=1)

# ---------------------------------------------------------------------------------------------- markdown summary
L = ["# #1232 pilot — wyniki testów zgodności (`scripts/tests_compat.py`)", "",
     f"Status: **{R['status']}**" + ("" if not fails else " — " + ", ".join(fails)) + " · Unity: **UNITY_NOT_RUN** · źródło: prawdziwe GLB wyeksportowane z Blendera oraz zatwierdzone GLB z PR #963 / #939 / #941 / #1018 / #1186.", "",
     "## Klocki (S1–S4, S7)", "", "| Kierunek / klocek | Interfejs = #963 | Poza bryłą [mm] | Styk portów (min %; zatw.) | 24 obroty | Tri (LOD1) | Tri zatw. |", "|---|---|---|---|---|---|---|"]
for k, b in blocks.items():
    pt = b["S3_contact_patch"]["ports"]
    L.append(f"| {k} | {'PASS' if b['S1_interface']['PASS'] else 'FAIL'} ({b['S1_interface']['pos_dev_mm']} mm) | {b['S2_containment']['max_outside_cell_solid_mm']} | "
             f"{min(r['flush_pct'] for r in pt)} ({min(r['approved_flush_pct_recorded'] for r in pt)}) {'PASS' if b['S3_contact_patch']['PASS'] else 'FAIL'} | {'PASS' if b['S4_rotations']['PASS'] else 'FAIL'} | "
             f"{b['S7_budget']['triangles']} ({b['S7_budget']['lod1_triangles']}) | {b['S7_budget']['approved_triangles']} |")
L += ["", "## Macierz modułów (S5)", "", f"{S5['modules']} prawdziwych modułów × {len(S5['ports'])} pełnych portów × 4 obroty wokół osi portu, pozycja spoczynkowa.", "",
      "| Wariant klocków | Montaże | Błędy styku | Zablokowane obroty | Porty zablokowane całkowicie |", "|---|---|---|---|---|"]
for v in variants: L.append(f"| {v} | {tot[v]['mounts']} | {tot[v]['contact_fail']} | {tot[v]['spin_blocked']} | {tot[v]['port_fully_blocked']} |")
L += ["", f"Regresje względem zatwierdzonych klocków: **{len(regress)}**.", "", "## Roboty (S6)", "", "| Robot | Części | Klocki pilota | Pary portów | Spójność | Pary moduł–klocek | Przenikania (zatw. / A / B) | Tri klocków (zatw. / A / B / A LOD1) |", "|---|---|---|---|---|---|---|---|"]
for n, r in robots.items():
    L.append(f"| {n} | {r['parts']} | {r['pilot_blocks']} | {r['port_pairs_connected']} | {'1 graf' if r['connected_components'] == 1 else str(r['connected_components']) + ' grafy'} | {r['module_block_pairs_checked']} | "
             f"{r['penetrating_pairs']['approved']} / {r['penetrating_pairs']['A']} / {r['penetrating_pairs']['B']} | {r['triangles_blocks_only']['approved']} / {r['triangles_blocks_only']['A']} / {r['triangles_blocks_only']['B']} / {r['triangles_blocks_only']['A_LOD1']} |")
open(os.path.join(paths.PKG, "tests", "COMPAT_SUMMARY.md"), "w", encoding="utf-8").write("\n".join(L) + "\n")
print("\n".join(L)); print("STATUS", R["status"], fails, R["seconds"], "s")
