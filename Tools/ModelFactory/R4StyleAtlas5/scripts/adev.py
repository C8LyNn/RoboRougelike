"""dev: one asset in five styles side by side (pure-python GLB + three.js). usage: python3 adev.py out.jpg Id[,Id]"""
import os, sys, numpy as np
import akit as K, astyle as ST, adefs as AD
import glbio, render_shot
def b2g(P): P = np.asarray(P); return np.stack([P[:, 0], P[:, 2], -P[:, 1]], axis=1)
def write_glb(m, path, style, glow):
    by = {}
    for f in m["faces"]:
        z = ST.ZONE[style][f["group"]]; d = by.setdefault(z, dict(pos=[], nrm=[], idx=[])); b = len(d["pos"])
        for vi, nn in zip(f["v"], f["n"]): d["pos"].append(m["V"][vi]); d["nrm"].append(nn)
        for i in range(1, len(f["v"]) - 1): d["idx"].append([b, b + i, b + i + 1])
    W = glbio.Writer(); prims = []
    for z in ST.ZONE_ORDER:
        if z not in by: continue
        if z == "R4P_Glow": mat = W.material(z, [c*0.5 for c in glow], 0.0, 0.5, emit=[c*min(1.0, ST.GLOW_STRENGTH[style]) for c in glow]) if ST.GLOW_STRENGTH[style] > 0 else W.material(z, glow, 0.0, 0.8)
        else: rgb, me, ro = ST.MAT[style][z]; mat = W.material(z, rgb, me, ro)
        prims.append(dict(pos=b2g(np.array(by[z]["pos"])), nrm=b2g(np.array(by[z]["nrm"])), idx=np.array(by[z]["idx"]), material=mat))
    W.node("m", mesh=W.mesh("m", prims)); W.save(path)
def T(x, y, z): return [1, 0, 0, x, 0, 1, 0, y, 0, 0, 1, z, 0, 0, 0, 1]
if __name__ == "__main__":
    ids = sys.argv[2].split(","); root = "/tmp/devatlas"; os.makedirs(root + "/glb", exist_ok=True)
    if not os.path.exists(root + "/web"): os.symlink(os.path.join(K.paths25.PILOT, "scripts", "render_web"), root + "/web")
    parts = []
    for r, cid in enumerate(ids):
        a = AD.ASSETS[cid]
        for c, s in enumerate(ST.STYLES):
            m = K.build(a["fn"], s, False, a["env"], cid, bool(a.get("source_clips"))); st = K.stats(m); l = K.stats(K.build(a["fn"], s, True, a["env"], cid, bool(a.get("source_clips"))))
            lo, hi = m["V"].min(0), m["V"].max(0); ins = bool((lo >= np.array(a["env"][0]) - 1e-6).all() and (hi <= np.array(a["env"][1]) + 1e-6).all())
            print(f"{cid:28s} {s} tris {st['triangles']:5d} lod {l['triangles']:5d} ok={st['ok']} lodok={l['ok']} inside={ins} shells {st['shells']} nodes {list(m['nodes'])}")
            p = f"{root}/glb/{cid}_{s}.glb"; write_glb(m, p, s, a["glow"]); parts.append(dict(url=f"/glb/{cid}_{s}.glb", matrix=T(c*1.05 - (lo[0] + hi[0])/2, -a["env"][0][2], -r*1.2 + (lo[1] + hi[1])/2)))
    R = render_shot.Renderer(root); n = len(ids); d = np.array([0.35, 0.62, 1.0]); d /= np.linalg.norm(d); tx, tz = 2.1, -(n - 1)*0.6; dist = 5.0 + n*1.2
    sc = dict(parts=parts, width=1800, height=420 + 330*(n - 1), target=[tx, 0.22, tz], camera=dict(pos=[tx + d[0]*dist, 0.22 + d[1]*dist, tz + d[2]*dist], fov=20), groundY=-0.002, shadowExtent=4 + n,
              exposure=0.62, background=[0.19, 0.21, 0.24], groundColor=[0.16, 0.17, 0.19], keyIntensity=3.4, fillIntensity=0.5,
              env=dict(sky=[0.45, 0.52, 0.62], horizon=[0.62, 0.63, 0.63], ground=[0.10, 0.10, 0.11], panels=[[[8, 14, 6], [14, 8], [5, 5, 4.7]], [[-12, 6, -4], [10, 10], [1.6, 1.8, 2.1]], [[2, 5, -14], [16, 6], [1.8, 1.7, 1.5]]]))
    print(R.render(sc, sys.argv[1]))
