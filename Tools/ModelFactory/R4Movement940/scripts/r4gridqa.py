# Independent grid-fit / mounting-port check on the EXPORTED GLB (not on the build scene).
# usage: blender -b --factory-startup --python r4gridqa.py -- OUT.json DIR1 DIR2 ...   (DIR = model folder with <name>.glb + <name>_QA.json)
import sys, os, json, math, bpy, numpy as np
from mathutils import Vector
from mathutils.bvhtree import BVHTree
a = sys.argv[sys.argv.index("--") + 1:]; OUT, DIRS = a[0], a[1:]
FACE, CELL_H, TOL = 0.260, 0.2755, 0.0006
D = 0.2755
# per-model allowed extents (Blender axes after glTF import: x right, -y forward, z up) for device geometry beyond the cell,
# taken from the runtime placeholder contracts (R41PartGeometry.Payload); everything else must stay in the anchor cell.
ALLOW = {
    "R4M_Wheel": {"zmin": -0.660, "ymin": -0.400, "ymax": 0.400},
    "R4M_CrawlerTrack": {"zmin": -0.430, "ymin": -0.480, "ymax": 0.480},
    "R4M_HoverEngine": {"zmin": -0.330},
    "R4M_OmniDrive": {"zmin": -0.450},
    "R4M_Suspension": {"zmin": -0.400},
    "R4M_Booster": {"ymax": 0.600},
    "R4M_JumpDrive": {"zmin": -0.430},
    "R4M_MagneticGrapple": {"ymin": -1.400},
    "R4M_Blink": {}, "R4M_Gyroscope": {}, "R4M_IndependentSteering": {},
}
res = {}
for d in DIRS:
    nm = os.path.basename(os.path.normpath(d)); weapon = nm.startswith("R4W_")
    qa = json.load(open(os.path.join(d, nm + "_QA.json"), encoding="utf-8"))
    fxn = {m["object"] for m in qa.get("fx_meshes", [])}
    fr = qa.get("frame_end") or 1
    bpy.ops.wm.read_factory_settings(use_empty=True); scn = bpy.context.scene
    bpy.ops.import_scene.gltf(filepath=os.path.join(d, nm + ".glb"))
    meshes = [o for o in scn.objects if o.type == 'MESH' and o.name not in fxn and not any(t in o.name for t in ("Flash", "Backblast", "Glow", "FX", "Plume", "Flame", "Ring_FX", "PulseRing", "Thrust"))]
    def animated(o):
        p = o
        while p is not None:
            if p.animation_data and p.animation_data.action: return True
            p = p.parent
        return False
    static = [o for o in meshes if not animated(o)]; moving = [o for o in meshes if animated(o)]
    sock = [o for o in scn.objects if o.name.startswith("Socket_Mount")]
    r = {"weapon": weapon, "static": [o.name for o in static], "moving": [o.name for o in moving], "failures": []}
    scn.frame_set(1); bpy.context.view_layer.update()
    if not sock: r["failures"].append("no Socket_Mount"); res[nm] = r; continue
    sp = sock[0].matrix_world.translation.copy(); ax = int(np.argmax([abs(sp.x), abs(sp.y), abs(sp.z)])); sg = 1.0 if sp[ax] > 0 else -1.0
    tang = [sp[i] for i in range(3) if i != ax]
    r["socket"] = {"name": sock[0].name, "pos": [round(v, 5) for v in sp], "axis": "xyz"[ax], "sign": sg,
                   "on_block_face": abs(abs(sp[ax]) - FACE) <= TOL and all(abs(t) <= TOL for t in tang)}
    if not r["socket"]["on_block_face"]: r["failures"].append("socket not at block-face centre")
    def V(o):
        m = o.matrix_world; me = o.evaluated_get(bpy.context.evaluated_depsgraph_get()).to_mesh()
        v = np.array([list(m @ x.co) for x in me.vertices]) if len(me.vertices) else np.zeros((0, 3)); o.to_mesh_clear(); return v
    # flush contact patch (rays from outside along -n) at rest, all meshes
    import bmesh
    bm = bmesh.new()
    for o in static + moving:
        t = bmesh.new(); t.from_mesh(o.data); t.transform(o.matrix_world); tm = bpy.data.meshes.new("t"); t.to_mesh(tm); t.free(); bm.from_mesh(tm); bpy.data.meshes.remove(tm)
    tree = BVHTree.FromBMesh(bm)
    n = Vector((0, 0, 0)); n[ax] = sg
    ti = [i for i in range(3) if i != ax]; N = 13; flush = prot = rec = miss = 0
    for i in range(N):
        for j in range(N):
            p = Vector((0, 0, 0)); p[ax] = sg*0.45; p[ti[0]] = -0.21 + 0.42*i/(N - 1); p[ti[1]] = -0.21 + 0.42*j/(N - 1)
            hit = tree.ray_cast(p, -n, 1.0)
            if hit[0] is None: miss += 1; continue
            c = hit[0][ax]*sg
            if c > FACE + 0.0005: prot += 1
            elif abs(c - FACE) <= 0.0010: flush += 1
            else: rec += 1
    tot = N*N
    r["contact_patch"] = {"grid": f"{N}x{N} over 0.42 x 0.42 m", "flush_pct": round(100*flush/tot, 1), "recessed_pct": round(100*rec/tot, 1), "protruding": prot, "missing": miss}
    if flush/tot < 0.80 or prot or miss: r["failures"].append("mount face not a flush square contact patch")
    # static geometry inside the anchor cell (+ device allowance for movement)
    al = ALLOW.get(nm, {}); lim = {"xmin": -D, "xmax": D, "ymin": -D, "ymax": D, "zmin": -D, "zmax": D}; lim.update(al)
    sv = np.vstack([V(o) for o in static]) if static else np.zeros((0, 3))
    lo, hi = sv.min(0), sv.max(0)
    r["static_bounds"] = [[round(float(x), 4) for x in lo], [round(float(x), 4) for x in hi]]
    out = [k for k, v in (("xmin", lo[0]), ("ymin", lo[1]), ("zmin", lo[2])) if v < lim[k] - 1e-4] + [k for k, v in (("xmax", hi[0]), ("ymax", hi[1]), ("zmax", hi[2])) if v > lim[k] + 1e-4]
    r["static_in_cell"] = not out; r["static_out"] = out
    if out: r["failures"].append("static geometry leaves the anchor cell: " + ",".join(out))
    # all frames: nothing crosses the mount plane; movement moving parts inside cell + allowance
    cross = []; mlo = np.full(3, 9.0); mhi = np.full(3, -9.0); mout = set()
    for f in range(1, int(fr) + 1):
        scn.frame_set(f)
        for o in static + moving:
            if max(o.matrix_world.to_scale()) < 0.01: continue
            v = V(o)
            if not len(v): continue
            c = float((v[:, ax]*sg).max())
            if c > FACE + 0.0005: cross.append((o.name, f, round(c, 4)))
            if o in moving:
                mlo = np.minimum(mlo, v.min(0)); mhi = np.maximum(mhi, v.max(0))
                if not weapon:
                    for k, val in (("xmin", v[:, 0].min()), ("ymin", v[:, 1].min()), ("zmin", v[:, 2].min())):
                        if val < lim[k] - 1e-4: mout.add((o.name, k))
                    for k, val in (("xmax", v[:, 0].max()), ("ymax", v[:, 1].max()), ("zmax", v[:, 2].max())):
                        if val > lim[k] + 1e-4: mout.add((o.name, k))
    r["crosses_mount_plane"] = cross[:10]; r["n_cross"] = len(cross)
    if cross: r["failures"].append("geometry crosses the mount face plane")
    if moving: r["moving_bounds"] = [[round(float(x), 4) for x in mlo], [round(float(x), 4) for x in mhi]]
    r["moving_out"] = sorted(mout)
    if mout: r["failures"].append("moving part leaves the cell outside its device direction")
    r["frames_checked"] = int(fr); r["allowance"] = al; r["PASS"] = not r["failures"]
    res[nm] = r
json.dump(res, open(OUT, "w", encoding="utf-8"), indent=1)
print("GRIDQA_DONE")
