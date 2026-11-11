"""dev helper: mesh dict -> GLB (pure python) + quick three.js render. Not part of the delivered pipeline (delivered GLBs come from Blender)."""
import os, sys, tempfile
import numpy as np
import paths25, glbio, r4panel as RP
from styles import STYLES, R41, ZONE_ORDER
import render_shot

ZONE = STYLES["A"]["zone"]; ZD = STYLES["A"]["zone_default"]
EMBER = {"R4P_Base": ((0.80, 0.78, 0.72), 0.25, 0.38), "R4P_Secondary": ((0.50, 0.49, 0.46), 0.30, 0.50), "R4P_Trim": ((0.035, 0.038, 0.045), 0.55, 0.45)}
def b2g(P): P = np.asarray(P); return np.stack([P[:, 0], P[:, 2], -P[:, 1]], axis=1)
def write_glb(m, path, colors=EMBER):
    by = {}
    for f in m["faces"]:
        z = ZONE[f["group"]]; d = by.setdefault(z, dict(pos=[], nrm=[], idx=[])); b = len(d["pos"])
        for vi, nn in zip(f["v"], f["n"]): d["pos"].append(m["V"][vi]); d["nrm"].append(nn)
        for i in range(1, len(f["v"]) - 1): d["idx"].append([b, b + i, b + i + 1])
    W = glbio.Writer(); prims = []
    for z in ZONE_ORDER:
        if z in by:
            rgb, me, ro = colors[z]; prims.append(dict(pos=b2g(np.array(by[z]["pos"])), nrm=b2g(np.array(by[z]["nrm"])), idx=np.array(by[z]["idx"]), material=W.material(z, rgb, me, ro)))
    W.node("m", mesh=W.mesh("m", prims)); W.save(path)

def T(x, y, z): return [1, 0, 0, x, 0, 1, 0, y, 0, 0, 1, z, 0, 0, 0, 1]
_R = None
def render(items, out, cols=5, cam=(0.9, 0.75, 1.0), dist=None, size=(1700, 1000), spacing=1.0, extra=None):
    """items: list of (name, mesh) -> grid render"""
    global _R
    root = "/tmp/dev1235"; os.makedirs(root + "/glb", exist_ok=True)
    if not os.path.exists(root + "/web"): os.symlink(os.path.join(paths25.PILOT, "scripts", "render_web"), root + "/web")
    parts = []; n = len(items); rows = (n + cols - 1)//cols
    for i, (name, m) in enumerate(items):
        p = f"{root}/glb/{name}.glb"; write_glb(m, p)
        lo, hi = m["V"].min(0), m["V"].max(0); c = (lo + hi)/2
        x = (i % cols)*spacing*1.5; z = -(i//cols)*spacing*1.7
        parts.append(dict(url=f"/glb/{name}.glb", matrix=T(x - c[0], 0.0, z + c[1])))
    if _R is None: _R = render_shot.Renderer(root)
    tx = (min(n, cols) - 1)*spacing*1.5/2; tz = -(rows - 1)*spacing*1.7/2; span = max(min(n, cols)*1.5, rows*1.7)*spacing
    d = np.array(cam); d = d/np.linalg.norm(d); dist = dist or span*0.95 + 1.3
    sc = dict(parts=parts, width=size[0], height=size[1], target=[tx, 0.0, tz], camera=dict(pos=[tx + d[0]*dist, d[1]*dist, tz + d[2]*dist], fov=26), groundY=-0.262, shadowExtent=span*0.9 + 1,
              exposure=0.62, background=[0.19, 0.21, 0.24], groundColor=[0.16, 0.17, 0.19], keyIntensity=3.4, fillIntensity=0.5,
              env=dict(sky=[0.45, 0.52, 0.62], horizon=[0.62, 0.63, 0.63], ground=[0.10, 0.10, 0.11], panels=[[[8, 14, 6], [14, 8], [5, 5, 4.7]], [[-12, 6, -4], [10, 10], [1.6, 1.8, 2.1]], [[2, 5, -14], [16, 6], [1.8, 1.7, 1.5]]]))
    if extra: sc.update(extra)
    return _R.render(sc, out)

if __name__ == "__main__":
    import shapes25
    kinds = sys.argv[2].split(",") if len(sys.argv) > 2 else list(shapes25.ALL)
    items = []
    for k in kinds:
        m = shapes25.build(k); s = RP.mesh_stats(m); l = RP.mesh_stats(shapes25.build(k, True))
        print(f"{k:12s} LOD0 {s['triangles']:5d} tris ok={s['ok']} nm={s['non_manifold_edges']} bd={s['boundary_edges']} inc={s['inconsistent_normals']} z={s['zero_area_faces']} | LOD1 {l['triangles']:4d} ok={l['ok']}")
        items.append((k, m))
    print(render(items, sys.argv[1]))
