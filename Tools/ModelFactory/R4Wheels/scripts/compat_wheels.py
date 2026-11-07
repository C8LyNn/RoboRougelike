# Wheel-variant compatibility matrix (copy of R4Blocks/scripts/compat_matrix.py with the module set = R4_Wheels/R4M_Wheel*).
# Every wheel variant is mounted on every full R4 port of every
# R4 block, in 4 roll orientations, over sampled animation frames. Checks per mounting:
#   contact - rays across the 0.42 m patch: module face flush 30 mm from the block face (two 0.52 m blocks on a 0.55 m grid),
#             nothing of either part protruding into the gap;
#   overlap - BVH overlap of the module (all non-FX meshes, sampled frames of every clip) with the whole block (all its cells).
# FX meshes (names not starting with R4W_/R4M_: flashes, glows, flames, beams) are visual only and excluded.
# usage: blender -b --factory-startup --python compat_wheels.py -- <wynik dir> <out json>
import bpy, json, os, sys, math, time, hashlib
from mathutils import Vector, Matrix, Quaternion
from mathutils.bvhtree import BVHTree

G, H, GAP = 0.55, 0.26, 0.030
DIRS = {"right": (1, 0, 0), "left": (-1, 0, 0), "up": (0, 0, 1), "down": (0, 0, -1), "forward": (0, -1, 0), "back": (0, 1, 0)}
a = sys.argv[sys.argv.index("--") + 1:]; ROOT, OUT = a[0], a[1]
WD, MD, BD = os.path.join(ROOT, "R4_Weapons_938_port"), os.path.join(ROOT, "R4_Movement_940_port"), os.path.join(ROOT, "R4_Blocks")
NFR = 10
ROLLS = [Matrix.Rotation(k*math.pi/2, 4, 'Z') for k in range(4)]


def clear():
    for o in list(bpy.data.objects): bpy.data.objects.remove(o, do_unlink=True)
    for c in (bpy.data.meshes, bpy.data.actions, bpy.data.materials, bpy.data.images):
        for x in list(c):
            if x.users == 0: c.remove(x)


def load(path):
    before = set(bpy.data.objects); acts0 = set(bpy.data.actions)
    bpy.ops.import_scene.gltf(filepath=path)
    new = [o for o in bpy.data.objects if o not in before]; acts = [x for x in bpy.data.actions if x not in acts0]
    return new, acts


def world_tris(objs, keep):
    dg = bpy.context.evaluated_depsgraph_get(); V, P = [], []
    for o in objs:
        if o.type != 'MESH' or not keep(o.name): continue
        e = o.evaluated_get(dg); me = e.to_mesh(); mw = e.matrix_world
        base = len(V); V.extend(mw @ v.co for v in me.vertices); P.extend([base + i for i in p.vertices] for p in me.polygons)
        e.to_mesh_clear()
    return V, P


def is_solid(n): return n.startswith(("R4W_", "R4M_"))


def sha(p): return hashlib.sha256(open(p, "rb").read()).hexdigest()


# ---------------- modules: canonical frame = socket at (0,0,GAP), socket outward direction -> -Z (mounting face looks down at the block)
modules = {}
XD = os.path.join(ROOT, "R4_Wheels")
mods = [(n, os.path.join(XD, n, n + ".glb"), "wheel") for n in sorted(os.listdir(XD)) if n.startswith("R4M_Wheel") and os.path.exists(os.path.join(XD, n, n + ".glb"))]
for n, p, kind in mods:
    clear(); objs, acts = load(p)
    s = [o for o in objs if o.name.startswith("Socket_Mount")][0]; sl = s.matrix_world.translation.copy(); d = sl.normalized()
    R = d.rotation_difference(Vector((0, 0, -1))).to_matrix().to_4x4()
    C = Matrix.Translation((0, 0, GAP)) @ R @ Matrix.Translation(-sl)
    f0 = int(min(x.frame_range[0] for x in acts)) if acts else 1; f1 = int(max(x.frame_range[1] for x in acts)) if acts else 1
    frames = sorted({int(round(f0 + (f1 - f0)*i/(NFR - 1))) for i in range(NFR)})
    per_frame = []
    for f in frames:
        bpy.context.scene.frame_set(f); V, P = world_tris(objs, is_solid)
        per_frame.append(([C @ v for v in V], P))
    bpy.context.scene.frame_set(f0); Vr, Pr = world_tris(objs, is_solid)
    modules[n] = {"kind": kind, "glb": p, "sha256": sha(p), "socket_local": [round(x, 4) for x in sl], "frames": frames,
                  "fx_excluded": sorted(o.name for o in objs if o.type == 'MESH' and not is_solid(o.name)),
                  "bvh": [[BVHTree.FromPolygons([Rl @ v for v in V], P) for (V, P) in per_frame] for Rl in ROLLS],
                  "rest": BVHTree.FromPolygons([C @ v for v in Vr], Pr)}
    print("MODULE", n, len(frames), "frames", flush=True)

# ---------------- blocks
def port_frame(cell, d):
    n = Vector(DIRS[d]); P = Vector(cell)*G + n*H
    return n.rotation_difference(Vector((0, 0, 1))).to_matrix().to_4x4() @ Matrix.Translation(-P)


def contact(mod_bvh, blk_bvh, N=13, half=0.21):
    both = prot = miss = 0
    for i in range(N):
        for j in range(N):
            x = -half + 2*half*i/(N - 1); y = -half + 2*half*j/(N - 1); o = Vector((x, y, GAP/2))
            hu = mod_bvh.ray_cast(o, Vector((0, 0, 1)), 0.3); hd = blk_bvh.ray_cast(o, Vector((0, 0, -1)), 0.3)
            if hu[0] is None or hd[0] is None: miss += 1; continue
            du, dd = hu[3], hd[3]
            if du < GAP/2 - 0.0006 or dd < GAP/2 - 0.0006: prot += 1
            elif du < GAP/2 + 0.0006 and dd < GAP/2 + 0.0006: both += 1
    return {"flush_both_pct": round(100*both/(N*N), 1), "protruding": prot, "missing": miss}


blocks = sorted(n for n in os.listdir(BD) if n.startswith("R4B_"))
res = {"method": __doc__ if False else "see header of compat_matrix.py", "gap_m": GAP, "rolls_deg": [0, 90, 180, 270], "frames_per_module": NFR,
       "modules": {k: {x: v[x] for x in ("kind", "glb", "sha256", "socket_local", "frames", "fx_excluded")} for k, v in modules.items()}, "blocks": {}, "pairs": {}}
t0 = time.time()
for b in blocks:
    q = json.load(open(os.path.join(BD, b, b + "_QA.json"), encoding="utf-8"))
    clear(); objs, _ = load(os.path.join(BD, b, b + ".glb")); V, P = world_tris(objs, lambda n: n.startswith("R4B_"))
    full = [p for p in q["ports"] if p["kind"] == "full"]; part = [p for p in q["ports"] if p["kind"] != "full"]
    res["blocks"][b] = {"full_ports": len(full), "partial_ports": len(part), "sha256_glb": sha(os.path.join(BD, b, b + ".glb"))}
    for p in full:
        F = port_frame(p["cell"], p["dir"]); bb = BVHTree.FromPolygons([F @ v for v in V], P)
        key = f"{p['dir']}_{''.join(str(c) for c in p['cell'])}"
        for m, M_ in modules.items():
            c = contact(M_["rest"], bb)
            rolls = []
            for r in range(4):
                hits = [M_["frames"][k] for k, mb in enumerate(M_["bvh"][r]) if mb.overlap(bb)]
                rolls.append(hits)
            ok_c = c["protruding"] == 0 and c["missing"] == 0 and c["flush_both_pct"] >= 70
            res["pairs"].setdefault(m, {}).setdefault(b, []).append({"port": key, "contact": c, "contact_PASS": ok_c,
                                                                       "rolls_clear": [len(h) == 0 for h in rolls], "overlap_frames": rolls})
    print("BLOCK", b, len(full), "full ports", round(time.time() - t0, 1), "s", flush=True)

# ---------------- summary
summ = {}
for m, bl in res["pairs"].items():
    s = {"blocks_with_full_port": 0, "ports": 0, "ports_all_rolls": 0, "ports_some_rolls": 0, "ports_blocked": 0, "contact_fail": 0}
    for b, ps in bl.items():
        if ps: s["blocks_with_full_port"] += 1
        for p in ps:
            s["ports"] += 1; s["contact_fail"] += 0 if p["contact_PASS"] else 1
            n = sum(p["rolls_clear"]); s["ports_all_rolls" if n == 4 else ("ports_some_rolls" if n else "ports_blocked")] += 1
    summ[m] = s
res["summary"] = summ
res["blocks_without_full_port"] = [b for b, v in res["blocks"].items() if v["full_ports"] == 0]
res["PASS"] = all(s["contact_fail"] == 0 for s in summ.values())
json.dump(res, open(OUT, "w", encoding="utf-8"), indent=1)
print("MATRIX_DONE", round(time.time() - t0, 1), flush=True)
