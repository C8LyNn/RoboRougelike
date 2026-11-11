"""dev helper: module mesh dict -> pure-python GLB + quick three.js grid render (delivered GLBs come from Blender)."""
import os, sys, numpy as np
import mkit as K
import glbio, render_shot
COL = dict(K.R41); 
def zone_mat(W, z, glow=(0.2, 0.8, 1.0)):
    if z == "R4P_Glow": return W.material(z, [c*0.25 for c in glow], 0.0, 0.4, emit=glow, estr=1.0)
    rgb, me, ro = COL[K.ZONE_DEFAULT[z]]; return W.material(z, rgb, me, ro)
def b2g(P): P = np.asarray(P); return np.stack([P[:, 0], P[:, 2], -P[:, 1]], axis=1)
def write_glb(m, path, glow=(0.2, 0.8, 1.0)):
    by = {}
    for f in m["faces"]:
        z = K.ZONE[f["group"]]; d = by.setdefault(z, dict(pos=[], nrm=[], idx=[])); b = len(d["pos"])
        for vi, nn in zip(f["v"], f["n"]): d["pos"].append(m["V"][vi]); d["nrm"].append(nn)
        for i in range(1, len(f["v"]) - 1): d["idx"].append([b, b + i, b + i + 1])
    W = glbio.Writer(); prims = []
    for z in K.ZONE_ORDER:
        if z in by: prims.append(dict(pos=b2g(np.array(by[z]["pos"])), nrm=b2g(np.array(by[z]["nrm"])), idx=np.array(by[z]["idx"]), material=zone_mat(W, z, glow)))
    W.node("m", mesh=W.mesh("m", prims)); W.save(path)
def T(x, y, z): return [1, 0, 0, x, 0, 1, 0, y, 0, 0, 1, z, 0, 0, 0, 1]
_R = None
def render(items, out, cols=4, cam=(0.9, 0.75, 1.0), size=(1700, 1000), spacing=0.62, extra=None):
    global _R
    root = "/tmp/devmod"; os.makedirs(root + "/glb", exist_ok=True)
    if not os.path.exists(root + "/web"): os.symlink(os.path.join(K.paths25.PILOT, "scripts", "render_web"), root + "/web")
    parts = []; n = len(items); rows = (n + cols - 1)//cols
    for i, (name, m, glow) in enumerate(items):
        p = f"{root}/glb/{name}.glb"; write_glb(m, p, glow)
        parts.append(dict(url=f"/glb/{name}.glb", matrix=T((i % cols)*spacing*1.5, 0.0, -(i//cols)*spacing*1.7)))
    if _R is None: _R = render_shot.Renderer(root)
    tx = (min(n, cols) - 1)*spacing*1.5/2; tz = -(rows - 1)*spacing*1.7/2; span = max(min(n, cols)*1.5, rows*1.7)*spacing
    d = np.array(cam); d = d/np.linalg.norm(d); dist = span*1.05 + 1.5
    sc = dict(parts=parts, width=size[0], height=size[1], target=[tx, 0.0, tz], camera=dict(pos=[tx + d[0]*dist, d[1]*dist, tz + d[2]*dist], fov=26), groundY=-0.262, shadowExtent=span*0.9 + 1,
              exposure=0.62, background=[0.19, 0.21, 0.24], groundColor=[0.16, 0.17, 0.19], keyIntensity=3.4, fillIntensity=0.5,
              env=dict(sky=[0.45, 0.52, 0.62], horizon=[0.62, 0.63, 0.63], ground=[0.10, 0.10, 0.11], panels=[[[8, 14, 6], [14, 8], [5, 5, 4.7]], [[-12, 6, -4], [10, 10], [1.6, 1.8, 2.1]], [[2, 5, -14], [16, 6], [1.8, 1.7, 1.5]]]))
    if extra: sc.update(extra)
    return _R.render(sc, out)
if __name__ == "__main__":
    import modules as MD
    names = sys.argv[2].split(",") if len(sys.argv) > 2 else list(MD.ALL)
    cam = tuple(float(x) for x in sys.argv[3].split(",")) if len(sys.argv) > 3 else (0.9, 0.75, 1.0)
    items = []
    for k in names:
        d = MD.ALL[k]; m = K.build(d["fn"]); s = K.stats(m); l = K.stats(K.build(d["fn"], True))
        print(f"{k:22s} LOD0 {s['triangles']:5d} sh {s['shells']:2d} ok={s['ok']} bad={s['bad_shells']} | LOD1 {l['triangles']:4d} ok={l['ok']} | z {s['bounds'][0][2]:.3f}..{s['bounds'][1][2]:.3f} max|xy| {max(abs(x) for b in s['bounds'] for x in b[:2]):.3f} ports {[p['dir'][0] + p['kind'][0] for p in m['ports']]}")
        items.append((k, m, d.get("glow", (0.2, 0.8, 1.0))))
    print(render(items, sys.argv[1], cols=int(os.environ.get("COLS", "4")), cam=cam))
