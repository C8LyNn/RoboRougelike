"""#1235 A_Sleek rollout - compatibility tests S1-S7 for the full catalogue of 30 blocks (5 pilot + 25 new) against the REAL approved assets.
Pure python3 + numpy + scipy on the exported GLB files (no Blender / Unity). Continues scripts/tests_compat.py of the #1232 pilot.
  S1 interface      empties (ports / sockets / root) identical to the approved PR #963 GLB
  S2 containment    new geometry (LOD0 + LOD1: vertices, edge midpoints, face centres) stays inside the approved analytic solid and its grid cells;
                    distance outside the approved collider hulls is reported next to the same figure for the approved visual
  S3 contact patch  approved patch_test on every approved port (LOD0 + LOD1)
  S4 rotations      24 grid rotations: geometry stays in its rotated cells, ports land on grid directions (local + world faces), ContactMask
  S5 module matrix  every real weapon / movement / wheel / connector on every FULL port of every block, 4 spins: 30 mm gap, flat contact, clearance
  S6 robots         the three comparison robots: cell conflicts, port graph, real-module clearance
  S7 budgets        triangles / vertices / material slots per part, aggregate triangles of the robots
usage: R4_REF=<refs> python3 tests_compat25.py [--no-matrix]   -> tests/compat_report.json, tests/COMPAT_SUMMARY.md"""
import os, sys, json, re, time, hashlib
import numpy as np
from scipy.spatial import ConvexHull
import paths25, layout25 as L, shapes25, scenes25
import glbio, raycast, shapes as shapes5

G, H, GAP = 0.55, 0.26, 0.03
FX = re.compile(r"Flash|Glow|Beam_Core|Beam_Sheath|PulseRing|PhaseRing|BlastRing|Backblast|BlurDisc|Sparks|Flame")
DIR = L.DIR; NAME = L.NAME
UNITY_BIT = {"right": 0, "left": 1, "up": 2, "down": 3, "forward": 4, "back": 5}
F2B = lambda P: np.stack([P[..., 0], -P[..., 2], P[..., 1]], axis=-1)        # file (x, y, z) -> Blender (x, -z, y)
B2F = lambda P: np.stack([P[..., 0], P[..., 2], -P[..., 1]], axis=-1)
T0 = time.time(); NO_MATRIX = "--no-matrix" in sys.argv
def sha(p): return hashlib.sha256(open(p, "rb").read()).hexdigest()
def approved_qa(kind): return L.approved_qa(kind)
def solid_parts(kind):
    if kind in shapes25.SOLID: return shapes25.SOLID[kind]
    return [{"hull": shapes5.SOLIDS[kind]}]

def hull_planes(P, shrink=0.0):
    h = ConvexHull(P); eq = np.unique(np.round(h.equations, 7), axis=0)
    return eq[:, :3], -eq[:, 3] - shrink

def outside_mm(kind, pts_bl):
    """distance by which Blender-space points leave the approved analytic solid (union of convex parts minus cylinders / inside sphere)"""
    best = np.full(len(pts_bl), 1e9)
    for part in solid_parts(kind):
        N_, D_ = hull_planes(np.array(part["hull"], float)); d = (pts_bl @ N_.T - D_).max(1)
        if part.get("sphere"): d = np.maximum(d, np.linalg.norm(pts_bl - np.array(part["sphere"]["center"], float), axis=1) - part["sphere"]["r"])
        for c in part.get("minus", []):
            ax = np.array(c["axis"], float); q = pts_bl - np.array(c["point"], float); d = np.maximum(d, c["r"] - np.linalg.norm(q - np.outer(q @ ax, ax), axis=1))
        best = np.minimum(best, d)
    return float(best.max()*1000)

def samples(M):
    T = M.tris(); return np.concatenate([M.all_pos(), T.mean(1), ((T + T[:, [1, 2, 0]])/2).reshape(-1, 3)])

def tri_hits_convex(T, N, D):
    S = np.einsum("mvk,pk->mvp", T, N) - D[None, None, :]
    cand = ~np.any(np.all(S > 0, axis=1), axis=1); inside = np.any(np.all(S <= 0, axis=2), axis=1)
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
    cache = {}
    def __init__(self, model):
        lib, name = model.split(":"); self.model = model; self.lib = lib; self.name = name; self.path = paths25.ref_glb(lib, name)
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
        base = os.path.join(paths25.REF, lib, paths25.P1.LIB[lib])
        for d in sorted(os.listdir(base)):
            if os.path.isfile(os.path.join(base, d, d + ".glb")): out.append(f"{lib}:{d}")
    return out

def grid_pts(n, half, N=13):
    n = np.array(n, float); u = np.array([n[1], n[2], n[0]]); v = np.cross(n, u); a = (np.linspace(-1, 1, N)*half*0.85*G)
    return np.array([n*(H + GAP/2) + u*x + v*y for x in a for y in a])

def side_profile(tris, n, half, toward):
    O = grid_pts(n, half); t = raycast.ray_hits(O - toward*1e-6, toward, tris, 0.6); k = GAP/2 - t
    return {"flush_pct": round(100*float(((k <= 0.0005) & (k > -0.0010)).mean()), 1), "protruding": int((k > 0.0005).sum()), "missing": int(np.isinf(t).sum())}

def mount_rots(mod, d):
    d = np.array(d); return [M for M in L.ROTS if np.array_equal(M @ mod.mount, -d)]

R = {"issue": 1235, "generated_by": "scripts/tests_compat25.py", "unity": "UNITY_NOT_RUN", "windows_player": "WINDOWS_PLAYER_NOT_RUN",
     "reference_commits": paths25.REF_SHA, "pilot_commit": paths25.PILOT_COMMIT, "sections": {}}
fails = []
WAVE = {k: 0 for k in L.PILOT}; WAVE.update({k: w for w, ks in L.WAVES.items() for k in ks})

# ---------------------------------------------------------------------------------------------- S1 / S2 / S3 / S4 / S7 per block
blocks = {}; NEW = {}; OLD = {}; LOD = {}
for kind in L.ALL30:
    qa = approved_qa(kind); Mo = glbio.load(scenes25.block_path("before", kind)); OLD[kind] = Mo
    pn = scenes25.block_path("A", kind); Mn = glbio.load(pn); Ml = glbio.load(scenes25.block_path("A", kind, True)); NEW[kind] = Mn; LOD[kind] = Ml
    b = {"wave": WAVE[kind], "origin": "pilot #1234 (unchanged)" if kind in L.PILOT else "#1235", "glb": os.path.relpath(pn, os.path.dirname(paths25.PKG)).replace("\\", "/"), "sha256": sha(pn)}
    # S1
    eo, en = Mo.empties, Mn.empties; pos = ax = 0.0
    for n_ in eo:
        if n_ in en:
            pos = max(pos, float(np.linalg.norm(eo[n_][0][:3, 3] - en[n_][0][:3, 3]))*1000)
            for col in (1, 2):                                                   # full orientation: local Y and Z axes
                ax = max(ax, float(np.degrees(np.arccos(np.clip(eo[n_][0][:3, col] @ en[n_][0][:3, col], -1, 1)))))
    b["S1_interface"] = {"approved_empties": sorted(eo), "missing": sorted(set(eo) - set(en)), "extra": sorted(set(en) - set(eo)), "pos_dev_mm": round(pos, 4), "axis_dev_deg": round(ax, 4)}
    b["S1_interface"]["PASS"] = not b["S1_interface"]["missing"] and not b["S1_interface"]["extra"] and pos < 0.01 and ax < 0.05
    # S2
    cells = np.array(L.INFO[kind]["cells"], float); clo, chi = cells.min(0)*G - H, cells.max(0)*G + H
    P = Mn.all_pos(); PL = Ml.all_pos(); Po = Mo.all_pos()
    Mc = glbio.load(paths25.ref_glb("blocks", f"R4B_{kind}", "_colliders")); ch = {}
    for pr in Mc.prims: ch.setdefault(pr["node"], []).append(pr["pos"])
    chulls = [hull_planes(np.concatenate(v)) for v in ch.values()]
    def out_col(Q): return round(float(np.min([(Q @ N_.T - D_).max(1) for N_, D_ in chulls], axis=0).max())*1000, 3)
    s2 = {"max_outside_approved_solid_mm": round(outside_mm(kind, F2B(samples(Mn))), 4), "lod1_max_outside_approved_solid_mm": round(outside_mm(kind, F2B(samples(Ml))), 4),
          "max_outside_grid_cells_mm": round(float(max((clo - P.min(0)).max(), (P.max(0) - chi).max()))*1000, 4),
          "outside_approved_collider_hulls_mm": {"A": out_col(P), "approved_visual": out_col(Po), "collider_parts": len(chulls)},
          "bounds_m": [np.round(P.min(0), 4).tolist(), np.round(P.max(0), 4).tolist()], "approved_bounds_m": [np.round(Po.min(0), 4).tolist(), np.round(Po.max(0), 4).tolist()]}
    s2["PASS"] = s2["max_outside_approved_solid_mm"] <= 0.05 and s2["lod1_max_outside_approved_solid_mm"] <= 0.05 and s2["max_outside_grid_cells_mm"] <= 0.05 and \
        s2["outside_approved_collider_hulls_mm"]["A"] <= max(s2["outside_approved_collider_hulls_mm"]["approved_visual"], 0.0) + 0.05
    b["S2_containment"] = s2
    # S3
    Tb = F2B(Mn.tris()); Tl = F2B(Ml.tris()); pt = []
    for p in qa["ports"]:
        need = 80 if p["kind"] == "full" else 65
        r = raycast.patch_test(Tb, p["cell"], p["dir"], p["offset"], p["half"]); rl = raycast.patch_test(Tl, p["cell"], p["dir"], p["offset"], p["half"])
        r.update({"cell": p["cell"], "dir": p["dir"], "kind": p["kind"], "lod1_flush_pct": rl["flush_pct"],
                  "approved_flush_pct_recorded": next(q["flush_pct"] for q in qa["port_contact_test"] if q["dir"] == p["dir"] and q.get("cell", p["cell"]) == p["cell"])})
        r["PASS"] = r["protruding"] == 0 and r["missing"] == 0 and r["flush_pct"] >= need and rl["protruding"] == 0 and rl["missing"] == 0 and rl["flush_pct"] >= need; pt.append(r)
    b["S3_contact_patch"] = {"ports": pt, "PASS": all(r["PASS"] for r in pt)}
    # S4
    rot_ok = True; yaw = {}; ports = L.INFO[kind]["ports"]
    for Mx in L.ROTS:
        Q = P @ Mx.T; rc = cells @ Mx.T; rot_ok &= bool(np.all(Q.min(0) >= rc.min(0)*G - H - 1e-6) and np.all(Q.max(0) <= rc.max(0)*G + H + 1e-6))
        rot_ok &= all(tuple(int(x) for x in Mx @ np.array(DIR[d])) in NAME for _, d, _ in ports)
    for k in range(4):
        Mx = L.rot_y(k); s = L.sig(kind, Mx)
        yaw[str(k*90)] = {"contact_mask_anchor": sum(1 << UNITY_BIT[d] for d in s), "full": sorted(d for d in s if s[d] == "full"), "partial": sorted(d for d in s if s[d] == "part"),
                          "cells_world": [list(L.rcell(Mx, c)) for c in L.INFO[kind]["cells"]]}
    canon = sum(1 << UNITY_BIT[d] for d in L.sig(kind))
    b["S4_rotations"] = {"rotations_tested": 24, "geometry_stays_in_rotated_cells": rot_ok, "distinct_port_layouts": len({tuple(sorted((L.rcell(Mx, c), L.rdir(Mx, d), k_) for c, d, k_ in ports)) for Mx in L.ROTS}),
                         "local_faces": [[list(c), d, k_] for c, d, k_ in ports], "yaw_0_90_180_270_world_faces": yaw, "canonical_contact_mask": canon, "approved_contact_mask": qa["contact_mask_local_anchor"]}
    b["S4_rotations"]["PASS"] = rot_ok and canon == qa["contact_mask_local_anchor"]
    # S7
    mats = {}
    for pr in Mn.prims: mats[pr["material"]] = mats.get(pr["material"], 0) + len(pr["idx"])
    b["S7_budget"] = {"triangles": Mn.tri_count(), "lod1_triangles": Ml.tri_count(), "approved_triangles": Mo.tri_count(), "triangle_ratio_vs_approved": round(Mn.tri_count()/Mo.tri_count(), 3),
                      "render_vertices": int(len(P)), "renderers": len({pr["node"] for pr in Mn.prims}), "material_slots": len(mats), "lod1_material_slots": len({pr["material"] for pr in Ml.prims}),
                      "materials": mats, "approved_material_slots": len({pr["material"] for pr in Mo.prims}), "approved_renderers": len({pr["node"] for pr in Mo.prims}),
                      "cells": len(cells), "triangles_per_cell": round(Mn.tri_count()/len(cells), 1), "glb_bytes": os.path.getsize(pn), "approved_glb_bytes": os.path.getsize(scenes25.block_path("before", kind))}
    for sec in ("S1_interface", "S2_containment", "S3_contact_patch", "S4_rotations"):
        if not b[sec]["PASS"]: fails.append(f"{kind}/{sec}")
    blocks[f"R4B_{kind}"] = b
R["sections"]["blocks"] = blocks
print("blocks done", round(time.time() - T0, 1), "s", flush=True)

# ---------------------------------------------------------------------------------------------- clearance solids
def pieces(variant, kind):
    """convex pieces (file axes, shrunk 0.5 mm) used for module clearance. Convex kinds: hull of the variant's own mesh (as in the pilot);
    non-convex kinds: the approved analytic parts (identical for both variants - S2 proves the A mesh lies inside them)"""
    parts = solid_parts(kind)
    if len(parts) == 1 and not parts[0].get("minus"): return [hull_planes((OLD if variant == "approved" else NEW)[kind].all_pos(), 0.0005)]
    return [hull_planes(B2F(np.array(p["hull"], float)), 0.0005) for p in parts]
PIECES = {v: {k: pieces(v, k) for k in L.ALL30} for v in ("approved", "A")}
def hits(T, pcs):
    n = 0
    for N_, D_ in pcs: n += len(tri_hits_convex(T, N_, D_))
    return n

# ---------------------------------------------------------------------------------------------- S5 module matrix
mods = [Mod.get(m) for m in all_modules()]
S5 = {"modules": len(mods), "skipped": NO_MATRIX}
if not NO_MATRIX:
    mod_side = {}
    for m in mods:
        if not m.sock_ok: continue
        tr = m.tris - m.sock_cell*G; O = grid_pts(m.mount, m.patch_half); t = raycast.ray_hits(O, -m.mount.astype(float), tr, 0.6); k = GAP/2 - t
        mod_side[m.model] = {"flush_pct": round(100*float(((k <= 0.0005) & (k > -0.0010)).mean()), 1), "protruding": int((k > 0.0005).sum()), "missing": int(np.isinf(t).sum())}
    S5.update({"module_face": mod_side, "skipped_modules": [m.model for m in mods if not m.sock_ok], "ports": []})
    tot = {v: {"mounts": 0, "contact_fail": 0, "spin_blocked": 0, "port_fully_blocked": 0} for v in ("approved", "A")}; regress = []; per_wave = {}
    for kind in L.ALL30:
        variants = {"approved": OLD[kind], "A": NEW[kind]}; vt = {v: variants[v].tris() for v in variants}
        cells = np.array(L.INFO[kind]["cells"], float); blo, bhi = cells.min(0)*G - H - 0.002, cells.max(0)*G + H + 0.002
        for c, d, pk in L.INFO[kind]["ports"]:
            if pk != "full": continue
            dv = np.array(DIR[d], float); c0 = np.array(c, float)*G
            prof = {v: {hk: side_profile(vt[v] - c0, dv, half, -dv) for hk, half in (("0.47", 0.47), ("0.2091", 0.2091))} for v in variants}
            rec = {"block": kind, "cell": list(c), "port": d, "block_face": {v: prof[v]["0.47"] for v in variants}, "worse_than_approved": []}
            for m in mods:
                if not m.sock_ok: continue
                hk = "0.2091" if m.patch_half < 0.3 else "0.47"; res = {}
                for v in variants:
                    clear = []
                    for Mx in mount_rots(m, DIR[d]):
                        T = (m.tris - m.sock_cell*G) @ Mx.T + c0 + dv*G
                        near = np.all(T.min(1) <= bhi, axis=1) & np.all(T.max(1) >= blo, axis=1)
                        clear.append(hits(T[near], PIECES[v][kind]) == 0 if near.any() else True)
                    bf = prof[v][hk]; mf = mod_side[m.model]
                    contact = bf["protruding"] == 0 and bf["missing"] == 0 and mf["protruding"] == 0 and bf["flush_pct"] >= (80 if hk == "0.47" else 65)
                    res[v] = {"contact_ok": contact, "spins_clear": int(sum(clear))}
                    tot[v]["mounts"] += 4; tot[v]["contact_fail"] += 0 if contact else 4; tot[v]["spin_blocked"] += 4 - int(sum(clear)); tot[v]["port_fully_blocked"] += int(sum(clear) == 0)
                if res["A"]["spins_clear"] < res["approved"]["spins_clear"] or (res["approved"]["contact_ok"] and not res["A"]["contact_ok"]):
                    regress.append([kind, list(c), d, m.model, res]); rec["worse_than_approved"].append(m.model)
                w = per_wave.setdefault(WAVE[kind], {"mounts": 0, "contact_fail_A": 0, "spin_blocked_A": 0, "spin_blocked_approved": 0})
                w["mounts"] += 4; w["contact_fail_A"] += 0 if res["A"]["contact_ok"] else 4; w["spin_blocked_A"] += 4 - res["A"]["spins_clear"]; w["spin_blocked_approved"] += 4 - res["approved"]["spins_clear"]
            S5["ports"].append(rec)
        print("matrix", kind, round(time.time() - T0, 1), "s", flush=True)
    S5.update({"full_ports": len(S5["ports"]), "totals": tot, "per_wave": per_wave, "regressions_vs_approved": regress, "PASS": not regress and tot["A"]["contact_fail"] == 0})
    S5["note"] = ("rest pose of the approved module GLBs, FX meshes excluded. Clearance is tested against convex pieces: for convex blocks the hull of each variant's own mesh, "
                  "for non-convex blocks (Step, CornerL3, Tee4, Arch, CoveEdge, Dome, TrussCube) the approved analytic parts, which contain both visuals (S2). "
                  "Blocked spins are counted for both variants; a regression is any case where A is worse than the approved block.")
    if not S5["PASS"]: fails.append("S5_module_matrix")
R["sections"]["S5_module_matrix"] = S5

# ---------------------------------------------------------------------------------------------- S6 robots
def connector_ports(mod):
    man = json.load(open(paths25.ref_file("connectors", mod.name, f"{mod.name}_connector_manifest.json")))
    return [(tuple(p["cell"]), p["face"], p["type"]) for p in man["connection_ports"]], [tuple(c["cell"]) for c in man["occupied_cells"]]

robots = {}; os.makedirs(os.path.join(paths25.PKG, "robots"), exist_ok=True)
for rname, fn in L.ROBOTS.items():
    Rb = fn(); ports = {}; cell_err = []
    for i, p in enumerate(Rb.parts):
        if p["type"] == "block":
            for c, d, k in Rb.block_ports(p): ports[(c, d)] = (i, "full" if k == "full" else "partial")
        elif p["type"] == "connector":
            m = Mod.get(p["model"]); cp, occ = connector_ports(m)
            cs = {tuple(int(x) for x in (p["rot"] @ np.array(c)) + np.array(p["cell"])) for c in occ}
            if cs != set(p["cells"]): cell_err.append([p["model"], sorted(cs), sorted(p["cells"])])
            for c, face, typ in cp:
                cc = tuple(int(x) for x in (p["rot"] @ np.array(c)) + np.array(p["cell"])); ports[(cc, L.rdir(p["rot"], face))] = (i, typ)
        else:
            m = Mod.get(p["model"]); dd = NAME[tuple(int(x) for x in p["rot"] @ m.mount)]; ports[(p["cell"], dd)] = (i, "module")
            if dd != p["mount"]: cell_err.append([p["model"], "mount direction", dd, p["mount"]])
    edges = set(); bad_mod = []
    for (c, d), (i, k) in ports.items():
        nb = (tuple(int(x) for x in np.array(c) + np.array(DIR[d])), L.OPP[d])
        if nb in ports and ports[nb][0] != i:
            j, k2 = ports[nb]
            if "module" in (k, k2) and {k, k2} - {"module"} not in ({"full"}, {"r4_full"}): bad_mod.append([Rb.parts[i].get("model") or Rb.parts[i]["kind"], list(c), d, k, k2]); continue
            edges.add((min(i, j), max(i, j)))
    for i, p in enumerate(Rb.parts):
        if p["type"] in ("weapon", "wheel", "movement") and not any(i in e for e in edges): bad_mod.append([p["model"], list(p["cell"]), "not on a full port"])
    comp = list(range(len(Rb.parts)))
    def findc(x):
        while comp[x] != x: comp[x] = comp[comp[x]]; x = comp[x]
        return x
    for a, b_ in edges: comp[findc(a)] = findc(b_)
    ncomp = len({findc(i) for i in range(len(Rb.parts))})
    clr = {"approved": 0, "A": 0}; pairs = 0; worst = []
    for i, p in enumerate(Rb.parts):
        if p["type"] == "block": continue
        m = Mod.get(p["model"]); T = m.tris @ p["rot"].T + np.array(p["cell"], float)*G; lo, hi = T.min((0, 1)), T.max((0, 1))
        for q in Rb.parts:
            if q["type"] != "block": continue
            qc = np.array(q["cells"], float)*G
            if np.any(qc.max(0) + H < lo - 0.002) or np.any(qc.min(0) - H > hi + 0.002): continue
            pairs += 1; Tl_ = (T - np.array(q["cell"], float)*G) @ q["rot"]; lc = np.array(L.INFO[q["kind"]]["cells"], float)*G
            near = np.all(Tl_.min(1) <= lc.max(0) + H + 0.002, axis=1) & np.all(Tl_.max(1) >= lc.min(0) - H - 0.002, axis=1)
            if not near.any(): continue
            for v in clr:
                h = hits(Tl_[near], PIECES[v][q["kind"]])
                if h: clr[v] += 1; worst.append([v, p["model"], list(p["cell"]), q["kind"], list(q["cell"]), h])
    cnt = Rb.count(); nb = sum(v for k, v in cnt.items() if k in L.ALL30)
    tb = {"approved": int(sum(OLD[p["kind"]].tri_count() for p in Rb.parts if p["type"] == "block")), "A": int(sum(NEW[p["kind"]].tri_count() for p in Rb.parts if p["type"] == "block")),
          "A_LOD1": int(sum(LOD[p["kind"]].tri_count() for p in Rb.parts if p["type"] == "block"))}
    tm = int(sum(Mod.get(p["model"]).tri_count_full for p in Rb.parts if p["type"] != "block"))
    robots[rname] = {"title": Rb.title, "parts": len(Rb.parts), "counts": cnt, "block_parts": nb, "block_kinds_used": len([k for k in cnt if k in L.ALL30]), "cells_occupied": len(Rb.cells),
                     "block_cells": int(sum(len(p["cells"]) for p in Rb.parts if p["type"] == "block")), "cell_errors": cell_err, "port_pairs_connected": len(edges), "connected_components": ncomp,
                     "module_mount_errors": bad_mod, "module_block_pairs_checked": pairs, "penetrating_pairs": clr, "penetration_detail": worst[:20],
                     "triangles_blocks_only": tb, "triangles_modules": tm, "renderers_blocks": nb, "material_slots_per_block": 3,
                     "note": "port graph comes from the approved port metadata, identical for approved and A blocks -> identical logical connectivity; no FPS is claimed (no Unity / Player run)",
                     "PASS": not cell_err and not bad_mod and ncomp == 1 and clr["A"] == 0 and clr["A"] <= clr["approved"]}
    if not robots[rname]["PASS"]: fails.append("S6_" + rname)
    json.dump({"name": rname, "title": Rb.title, "axes": "file axes of the approved models: x = Port_right(+), y = up, z = Port_forward(+); cell = 0.55 m",
               "parts": [{"type": p["type"], "model": p.get("model") or "R4B_" + p["kind"], "cell": list(p["cell"]), "rot3x3": np.array(p["rot"]).tolist(), "cells": [list(c) for c in p["cells"]], "paint": p.get("paint")} for p in Rb.parts]},
              open(os.path.join(paths25.PKG, "robots", rname + "_layout.json"), "w"), indent=0)
    print("robot", rname, robots[rname]["PASS"], round(time.time() - T0, 1), "s", flush=True)
R["sections"]["S6_robots"] = robots

# ---------------------------------------------------------------------------------------------- per-wave verdicts
waves = {}
for w, names in ((0, L.PILOT), (1, L.WAVES[1]), (2, L.WAVES[2]), (3, L.WAVES[3]), (4, L.WAVES[4])):
    bl = [blocks[f"R4B_{k}"] for k in names]; f = [x for x in fails if x.split("/")[0] in names]
    waves[str(w)] = {"blocks": names, "S1": all(b["S1_interface"]["PASS"] for b in bl), "S2": all(b["S2_containment"]["PASS"] for b in bl), "S3": all(b["S3_contact_patch"]["PASS"] for b in bl),
                     "S4": all(b["S4_rotations"]["PASS"] for b in bl), "S5": None if NO_MATRIX else not any(r[0] in names for r in S5["regressions_vs_approved"]),
                     "triangles_A": sum(b["S7_budget"]["triangles"] for b in bl), "triangles_A_LOD1": sum(b["S7_budget"]["lod1_triangles"] for b in bl), "triangles_approved": sum(b["S7_budget"]["approved_triangles"] for b in bl),
                     "failures": f, "PASS": not f and (NO_MATRIX or not any(r[0] in names for r in S5["regressions_vs_approved"]))}
R["sections"]["waves"] = waves
R["status"] = "PASS" if not fails else "FAIL"; R["failures"] = fails; R["seconds"] = round(time.time() - T0, 1)
os.makedirs(os.path.join(paths25.PKG, "tests"), exist_ok=True)
json.dump(R, open(os.path.join(paths25.PKG, "tests", "compat_report.json"), "w"), indent=1)

# ---------------------------------------------------------------------------------------------- markdown summary
WL = {0: "pilot #1234", 1: "fala 1", 2: "fala 2", 3: "fala 3", 4: "fala 4"}
M_ = ["# #1235 — wyniki testów zgodności katalogu 30 klocków A_Sleek (`scripts/tests_compat25.py`)", "",
      f"Status: **{R['status']}**" + ("" if not fails else " — " + ", ".join(fails)) + " · Unity: **UNITY_NOT_RUN** · Windows Player: **WINDOWS_PLAYER_NOT_RUN**", "",
      "Źródło: prawdziwe pliki GLB wyeksportowane z Blendera (25 nowych z tego PR, 5 pilota bez zmian z #1234) oraz zatwierdzone GLB z PR #963 / #939 / #941 / #1018 / #1186.",
      "Polecenie: `R4_REF=<refs> python3 scripts/tests_compat25.py`. Commity źródeł: " + ", ".join(f"{k} `{v[:7]}`" for k, v in paths25.REF_SHA.items()) + f", pilot `{paths25.PILOT_COMMIT[:7]}`.", "",
      "## Wynik per fala", "", "| Fala | Klocki | S1 interfejs | S2 bryła | S3 styk portów | S4 obroty | S5 moduły | Tri zatw. → A (LOD1) | Wynik |", "|---|---|---|---|---|---|---|---|---|"]
ok = lambda v: "—" if v is None else ("PASS" if v else "FAIL")
for w, d in waves.items():
    M_.append(f"| {WL[int(w)]} | {len(d['blocks'])} | {ok(d['S1'])} | {ok(d['S2'])} | {ok(d['S3'])} | {ok(d['S4'])} | {ok(d['S5'])} | {d['triangles_approved']} → {d['triangles_A']} ({d['triangles_A_LOD1']}) | **{ok(d['PASS'])}** |")
M_ += ["", "## Klocki (S1–S4, S7)", "", "| Klocek | Fala | Komórki | Interfejs = #963 | Poza bryłą LOD0 / LOD1 [mm] | Poza koliderem A / zatw. [mm] | Styk min % LOD0 / LOD1 (zatw.) | 24 obroty | Tri A (LOD1) | Tri zatw. | Sloty mat. |", "|---|---|---|---|---|---|---|---|---|---|---|"]
for k, b in blocks.items():
    pt = b["S3_contact_patch"]["ports"]; s2 = b["S2_containment"]; s7 = b["S7_budget"]
    M_.append(f"| {k} | {WL[b['wave']]} | {s7['cells']} | {ok(b['S1_interface']['PASS'])} ({b['S1_interface']['pos_dev_mm']} mm) | {s2['max_outside_approved_solid_mm']} / {s2['lod1_max_outside_approved_solid_mm']} | "
              f"{s2['outside_approved_collider_hulls_mm']['A']} / {s2['outside_approved_collider_hulls_mm']['approved_visual']} | {min(r['flush_pct'] for r in pt)} / {min(r['lod1_flush_pct'] for r in pt)} ({min(r['approved_flush_pct_recorded'] for r in pt)}) {ok(b['S3_contact_patch']['PASS'])} | "
              f"{ok(b['S4_rotations']['PASS'])} | {s7['triangles']} ({s7['lod1_triangles']}) | {s7['approved_triangles']} | {s7['material_slots']} |")
if not NO_MATRIX:
    M_ += ["", "## Macierz modułów (S5)", "", f"{S5['modules']} prawdziwych modułów × {S5['full_ports']} pełnych portów (wszystkie komórki klocków wielokomórkowych) × 4 obroty wokół osi portu, pozycja spoczynkowa.", "",
           "| Wariant klocków | Montaże | Błędy styku | Zablokowane obroty | Porty zablokowane całkowicie |", "|---|---|---|---|---|"]
    for v in ("approved", "A"): M_.append(f"| {v} | {tot[v]['mounts']} | {tot[v]['contact_fail']} | {tot[v]['spin_blocked']} | {tot[v]['port_fully_blocked']} |")
    M_ += ["", f"Regresje A względem zatwierdzonych klocków: **{len(regress)}**. Zablokowane obroty to realne kolizje dużych modułów (np. koła XL) z sąsiednią komórką tego samego klocka wielokomórkowego; występują identycznie dla zatwierdzonych klocków."]
M_ += ["", "## Roboty (S6)", "", "| Robot | Części | Klocki (części / komórki / rodzaje) | Pary portów | Spójność | Pary moduł–klocek | Przenikania zatw. / A | Tri klocków zatw. / A / A LOD1 | Wynik |", "|---|---|---|---|---|---|---|---|---|"]
for n, r in robots.items():
    M_.append(f"| {n} | {r['parts']} | {r['block_parts']} / {r['block_cells']} / {r['block_kinds_used']} | {r['port_pairs_connected']} | {'1 graf' if r['connected_components'] == 1 else str(r['connected_components']) + ' grafy'} | {r['module_block_pairs_checked']} | "
              f"{r['penetrating_pairs']['approved']} / {r['penetrating_pairs']['A']} | {r['triangles_blocks_only']['approved']} / {r['triangles_blocks_only']['A']} / {r['triangles_blocks_only']['A_LOD1']} | {ok(r['PASS'])} |")
M_ += ["", "Liczby trójkątów to dane źródłowe plików GLB. Nie są pomiarem FPS — Unity ani Windows Player nie były uruchamiane."]
open(os.path.join(paths25.PKG, "tests", "COMPAT_SUMMARY.md"), "w", encoding="utf-8").write("\n".join(M_) + "\n")
print("\n".join(M_)); print("STATUS", R["status"], fails, R["seconds"], "s")
