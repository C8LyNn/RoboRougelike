# #1182 compatibility test of the R4 connectors against the REAL earlier models (exported GLBs, not proxies).
#  A) port mating: on every connector port, mount real partners with their own R4 socket, 4 rolls (0/90/180/270 deg):
#     blocks R4B_Cube / Wedge / Plate2x2 / HalfBlock / Beam3 (PR #963) on every port; on full (strut) ports also every weapon
#     (PR #939), movement part (PR #941) and wheel variant (PR #1018) over 10 sampled animation frames.
#     contact = 13x13 rays over the connector port patch in the 30 mm gap: both faces flush (+-0.6 mm), nothing protruding;
#     overlap = BVH of the partner (non-FX meshes) vs the WHOLE connector.
#  B) neighbour test (triforcing): a real R4B_Cube in every free cell face-adjacent to the connector: must not overlap; it is
#     logically connected ONLY if a declared connector port faces it - otherwise it is merely adjacent (no connection).
#  C) standard check: measured bounds / socket planes of the real old GLBs (grid 0.55, face 0.26) recorded as evidence.
# usage: blender -b --factory-startup --python connector_compat.py -- <wynik dir> <out json> [part ids]
import bpy, json, os, sys, math, time, hashlib
from mathutils import Vector, Matrix
from mathutils.bvhtree import BVHTree

G, H, GAP = 0.55, 0.26, 0.030
a = sys.argv[sys.argv.index("--") + 1:]; ROOT, OUT = a[0], a[1]; ONLY = a[2].split(",") if len(a) > 2 else None
CD = os.path.join(ROOT, "R4_Connectors1182")
DIRS_U = {"right": (1, 0, 0), "left": (-1, 0, 0), "up": (0, 1, 0), "down": (0, -1, 0), "forward": (0, 0, 1), "back": (0, 0, -1)}
def Bv(u): return Vector((u[0], -u[2], u[1]))
DIRS = {k: Bv(v) for k, v in DIRS_U.items()}
ROLLS = [Matrix.Rotation(k*math.pi/2, 4, 'Z') for k in range(4)]
NFR = 10
# both faces flush over the connector patch; on the small rod_mini patch the concentric recesses of BOTH ports (rod 0.45-scale
# pattern + the partner's full-size groove ring / socket) cover ~36 % of the area, so the flush share there is lower by design
NEED = {"r4_full": 70, "rod_mini": 55}


def clear():
    for o in list(bpy.data.objects): bpy.data.objects.remove(o, do_unlink=True)
    for c in (bpy.data.meshes, bpy.data.actions, bpy.data.materials, bpy.data.images):
        for x in list(c):
            if x.users == 0: c.remove(x)


def load(path):
    before = set(bpy.data.objects); acts0 = set(bpy.data.actions)
    bpy.ops.import_scene.gltf(filepath=path)
    return [o for o in bpy.data.objects if o not in before], [x for x in bpy.data.actions if x not in acts0]


def world_tris(objs, keep):
    dg = bpy.context.evaluated_depsgraph_get(); V, P = [], []
    for o in objs:
        if o.type != 'MESH' or not keep(o.name): continue
        e = o.evaluated_get(dg); me = e.to_mesh(); mw = e.matrix_world
        b = len(V); V.extend(mw @ v.co for v in me.vertices); P.extend([b + i for i in p.vertices] for p in me.polygons); e.to_mesh_clear()
    return V, P


def solid(n): return n.startswith(("R4W_", "R4M_", "R4B_", "R4C_"))
def sha(p): return hashlib.sha256(open(p, "rb").read()).hexdigest()


# ---------------------------------------------------------------- partners (canonical: socket at (0,0,GAP), socket outward -> -Z)
PARTNERS = {}
def add_partner(name, path, kind, animated):
    clear(); objs, acts = load(path)
    s = [o for o in objs if o.name.startswith("Socket_Mount")][0]; sl = s.matrix_world.translation.copy(); d = sl.normalized()
    C = Matrix.Translation((0, 0, GAP)) @ d.rotation_difference(Vector((0, 0, -1))).to_matrix().to_4x4() @ Matrix.Translation(-sl)
    frames = [1]
    if animated and acts:
        f0 = int(min(x.frame_range[0] for x in acts)); f1 = int(max(x.frame_range[1] for x in acts))
        frames = sorted({int(round(f0 + (f1 - f0)*i/(NFR - 1))) for i in range(NFR)})
    per = []
    for f in frames:
        bpy.context.scene.frame_set(f); V, P = world_tris(objs, solid); per.append(([C @ v for v in V], P))
    bpy.context.scene.frame_set(frames[0]); Vr, Pr = world_tris(objs, solid)
    PARTNERS[name] = {"kind": kind, "path": path, "sha256": sha(path), "frames": frames,
                      "bvh": [[BVHTree.FromPolygons([R @ v for v in V], P) for (V, P) in per] for R in ROLLS],
                      "rest": [BVHTree.FromPolygons([R @ (C @ v) for v in Vr], Pr) for R in ROLLS]}


BD = os.path.join(ROOT, "R4_Blocks")
for b in ("Cube", "Wedge", "Plate2x2", "HalfBlock", "Beam3"):
    add_partner("R4B_" + b, os.path.join(BD, "R4B_" + b, "R4B_" + b + ".glb"), "block", False)
for d, pre, kind in (("R4_Weapons_938_port", "R4W_", "weapon"), ("R4_Movement_940_port", "R4M_", "movement"), ("R4_Wheels", "R4M_Wheel", "wheel")):
    for n in sorted(os.listdir(os.path.join(ROOT, d))):
        p = os.path.join(ROOT, d, n, n + ".glb")
        if n.startswith(pre) and os.path.exists(p): add_partner(n, p, kind, True)
print("PARTNERS", len(PARTNERS), flush=True)


def contact(mod_bvh, blk_bvh, half_m, N=13):
    both = prot = miss = 0
    for i in range(N):
        for j in range(N):
            x = -half_m + 2*half_m*i/(N - 1); y = -half_m + 2*half_m*j/(N - 1); o = Vector((x, y, GAP/2))
            hu = mod_bvh.ray_cast(o, Vector((0, 0, 1)), 0.3); hd = blk_bvh.ray_cast(o, Vector((0, 0, -1)), 0.3)
            if hu[0] is None or hd[0] is None: miss += 1; continue
            if hu[3] < GAP/2 - 0.0006 or hd[3] < GAP/2 - 0.0006: prot += 1
            elif hu[3] < GAP/2 + 0.0006 and hd[3] < GAP/2 + 0.0006: both += 1
    return {"flush_both_pct": round(100*both/(N*N), 1), "protruding": prot, "missing": miss}


# ---------------------------------------------------------------- standard check on the real files
std = {}
for n, d in (("R4B_Cube", "R4_Blocks"), ("R4B_Wedge", "R4_Blocks"), ("R4W_Basic", "R4_Weapons_938_port"), ("R4M_Wheel", "R4_Movement_940_port"), ("R4M_WheelM_Side_Offroad", "R4_Wheels")):
    clear(); objs, _ = load(os.path.join(ROOT, d, n, n + ".glb")); bpy.context.view_layer.update()
    V, _P = world_tris(objs, solid); s = [o for o in objs if o.name.startswith("Socket_Mount")][0]; sl = s.matrix_world.translation
    nrm = sl.normalized(); face = max(v.dot(nrm) for v in V)
    plate = [v for v in V if abs(v.dot(nrm) - 0.26) < 0.0161]
    tang = [i for i in range(3) if abs(nrm[i]) < 0.5]
    std[n] = {"socket_m": [round(x, 4) for x in sl], "outermost_along_socket_normal_m": round(face, 4),
              "port_face_half_extent_m": [round(max(abs(v[i]) for v in plate), 4) for i in tang] if plate else None,
              "bounds_m": [[round(min(v[i] for v in V), 4) for i in range(3)], [round(max(v[i] for v in V), 4) for i in range(3)]]}

# ---------------------------------------------------------------- connectors
res = {"gap_m": GAP, "rolls_deg": [0, 90, 180, 270], "frames_per_animated_partner": NFR, "standard_measured": std,
       "partners": {k: {x: v[x] for x in ("kind", "sha256", "frames")} for k, v in PARTNERS.items()}, "connectors": {}}
conns = sorted(n for n in os.listdir(CD) if n.startswith("R4C_") and os.path.exists(os.path.join(CD, n, n + "_QA.json")))
if ONLY: conns = [c for c in conns if c[4:] in ONLY]
t0 = time.time()
for cn in conns:
    q = json.load(open(os.path.join(CD, cn, cn + "_QA.json"), encoding="utf-8"))
    clear(); objs, _ = load(os.path.join(CD, cn, cn + ".glb")); V, P = world_tris(objs, solid)
    whole = BVHTree.FromPolygons(V, P)
    occ = {tuple(c["cell"]) for c in q["occupied_cells_unity"]}
    out = {"ports": [], "neighbours": None}
    for p in q["ports"]:
        n = DIRS[p["dir"]]; Pp = Bv(p["cell"])*G + n*H
        F = n.rotation_difference(Vector((0, 0, 1))).to_matrix().to_4x4() @ Matrix.Translation(-Pp)
        cb = BVHTree.FromPolygons([F @ v for v in V], P)
        half_m = (p["half"][0]*G)*0.85
        rows = []
        for pn, pr in PARTNERS.items():
            if pr["kind"] != "block" and p["kind"] != "r4_full": continue      # modules need a full R4 port (rule)
            c = contact(pr["rest"][0], cb, half_m)
            clear_rolls = [not any(b.overlap(cb) for b in pr["bvh"][r]) for r in range(4)]
            ok = c["protruding"] == 0 and c["missing"] == 0 and c["flush_both_pct"] >= NEED[p["kind"]]
            rows.append({"partner": pn, "kind": pr["kind"], "contact": c, "contact_PASS": ok, "rolls_clear": clear_rolls})
        out["ports"].append({"port": p["id"], "kind": p["kind"], "dir": p["dir"], "cell": p["cell"], "tests": rows,
                             "modules_allowed": p["kind"] == "r4_full"})
    # neighbour cubes
    clear(); objs, _ = load(os.path.join(BD, "R4B_Cube", "R4B_Cube.glb")); Vc, Pc_ = world_tris(objs, solid)
    portfaces = {(tuple(p["cell"]), p["dir"]) for p in q["ports"]}
    nb = []
    for c in sorted(occ):
        for d, du in DIRS_U.items():
            m = (c[0] + du[0], c[1] + du[1], c[2] + du[2])
            if m in occ: continue
            off = Bv(m)*G; cube = BVHTree.FromPolygons([v + off for v in Vc], Pc_)
            ov = bool(cube.overlap(whole))
            # min gap between the cube face looking at the connector cell and the connector (rays along -n from the cube face)
            n = DIRS[d]; u = Vector((1, 0, 0)) if abs(n.x) < 0.9 else Vector((0, 1, 0)); u = (u - n*u.dot(n)).normalized(); w = n.cross(u)
            gaps = []
            for i in range(7):
                for j in range(7):
                    o = off - n*H + u*(-0.22 + 0.44*i/6) + w*(-0.22 + 0.44*j/6)
                    h = whole.ray_cast(o, -n, 0.6)
                    if h[0] is not None: gaps.append(h[3])
            nb.append({"cube_cell": list(m), "faces_connector_cell": list(c), "face_dir": d, "overlap": ov,
                       "connected": (tuple(c), d) in portfaces, "min_gap_m": round(min(gaps), 4) if gaps else None})
    out["neighbours"] = {"cases": nb, "count": len(nb), "overlaps": sum(x["overlap"] for x in nb),
                         "connected": sum(x["connected"] for x in nb), "adjacent_not_connected": sum(not x["connected"] for x in nb),
                         "min_gap_not_connected_m": min([x["min_gap_m"] for x in nb if not x["connected"] and x["min_gap_m"] is not None] or [None]) if any(not x["connected"] for x in nb) else None}
    # summary per connector
    T = [t for p in out["ports"] for t in p["tests"]]
    out["summary"] = {"ports": len(out["ports"]), "mountings": len(T), "contact_fail": sum(not t["contact_PASS"] for t in T),
                      "all_rolls_clear": sum(all(t["rolls_clear"]) for t in T), "some_rolls_clear": sum(any(t["rolls_clear"]) and not all(t["rolls_clear"]) for t in T),
                      "blocked": sum(not any(t["rolls_clear"]) for t in T), "neighbour_overlaps": out["neighbours"]["overlaps"]}
    # PASS = every mounting has a valid flush contact and no neighbour overlaps; partners blocked in some/all rolls are reported
    # (inner faces of arcs / brackets - same class as the CornerL3 / Tee4 inner faces in PR #963), not hidden
    out["PASS"] = out["summary"]["contact_fail"] == 0 and out["neighbours"]["overlaps"] == 0
    res["connectors"][cn] = out
    print("CONN", cn, out["summary"], round(time.time() - t0, 1), flush=True)
res["PASS"] = all(v["PASS"] for v in res["connectors"].values())
json.dump(res, open(OUT, "w", encoding="utf-8"), indent=1)
print("COMPAT_DONE", res["PASS"], flush=True)
