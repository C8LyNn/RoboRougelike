"""pass 2 dev look (pure-python GLB + three.js, pass-1 palette): python3 rdev.py out.jpg Type [34|front|side|back|top]"""
import os, sys, numpy as np
import akit as K, astyle as ST, pdefs as PD, qstyle as QS, rdefs as R, glbio, render_shot
def b2g(P): P = np.asarray(P); return np.stack([P[:, 0], P[:, 2], -P[:, 1]], axis=1)
def write_glb(V, faces, path, style, glow):
    by = {}
    for f in faces:
        z = QS.ZONE[style][f["group"]]; d = by.setdefault(z, dict(pos=[], nrm=[], idx=[])); b = len(d["pos"])
        for vi, nn in zip(f["v"], f["n"]): d["pos"].append(V[vi]); d["nrm"].append(nn)
        for i in range(1, len(f["v"]) - 1): d["idx"].append([b, b + i, b + i + 1])
    W = glbio.Writer(); prims = []
    for z, d in by.items():
        if z == "R4P_Glow": mat = W.material(z, [c*0.5 for c in glow], 0.0, 0.5, emit=[min(1.0, c*1.2) for c in glow]) if QS.GLOW_STRENGTH[style] > 0 else W.material(z, glow, 0.0, 0.8)
        else: rgb, me, ro = QS.MAT[style].get(z, QS.MAT[style]["R4P_Trim"]); mat = W.material(z, rgb, me, ro)
        prims.append(dict(pos=b2g(np.array(d["pos"])), nrm=b2g(np.array(d["nrm"])), idx=np.array(d["idx"]), material=mat))
    W.node("m", mesh=W.mesh("m", prims)); W.save(path)
if __name__ == "__main__":
    typ = sys.argv[2]; view = sys.argv[3] if len(sys.argv) > 3 else "34"; yaw = {"34": -38, "front": 0, "side": -90, "back": 142, "top": -38}[view]; root = "/tmp/devatlas"; os.makedirs(root + "/glb", exist_ok=True)
    if not os.path.exists(root + "/web"): os.symlink(os.path.join(K.paths25.PILOT, "scripts", "render_web"), root + "/web")
    t = PD.TYPES[typ]; env = np.array(t["env"]); parts = []; sp = float(np.linalg.norm(env[1][:2] - env[0][:2]))*0.9 + 0.25
    for c, s in enumerate(ST.STYLES):
        m = K.build(R.P[typ][s], s, False, t["env"], t["cid"], bool(t.get("source_clips")), t.get("planes"), True); fx = set(t.get("fx", [])); V = []; F = []
        for n, mm in m["by_node"].items():
            if mm is None or n in fx: continue
            b = sum(len(v) for v in V); V.append(mm["V"]); F += [dict(f, v=[i + b for i in f["v"]]) for f in mm["faces"]]
        cx, cy = (env[0][0] + env[1][0])/2, (env[0][1] + env[1][1])/2; a = np.radians(yaw); ca, sa = np.cos(a), np.sin(a); p = f"{root}/glb/R_{typ}_{s}.glb"; write_glb(np.concatenate(V), F, p, s, t["glow"])
        parts.append(dict(url=f"/glb/R_{typ}_{s}.glb", matrix=[ca, 0, sa, c*sp - ca*cx + sa*cy, 0, 1, 0, -env[0][2], -sa, 0, ca, sa*cx + ca*cy, 0, 0, 0, 1]))
    Rn = render_shot.Renderer(root); d = np.array({"34": [0, 0.5, 1.0], "front": [0, 0.12, 1.0], "side": [0, 0.12, 1.0], "back": [0, 0.5, 1.0], "top": [0, 1.0, 0.25]}[view], float); d /= np.linalg.norm(d)
    tx = 2*sp; hy = (env[1][2] - env[0][2])/2; W = 5*sp; asp = 2.9; dist = W/2/(np.tan(np.radians(10))*asp)
    sc = dict(parts=parts, width=2000, height=int(2000/asp), target=[tx, hy, 0], camera=dict(pos=[tx + d[0]*dist, hy + d[1]*dist, d[2]*dist], fov=20), groundY=-0.002, shadowExtent=5, exposure=0.7, background=[0.19, 0.21, 0.24], groundColor=[0.16, 0.17, 0.19],
              keyIntensity=3.4, fillIntensity=0.5, hideNodes=[], env=dict(sky=[0.45, 0.52, 0.62], horizon=[0.62, 0.63, 0.63], ground=[0.10, 0.10, 0.11], panels=[[[8, 14, 6], [14, 8], [5, 5, 4.7]], [[-12, 6, -4], [10, 10], [1.6, 1.8, 2.1]], [[2, 5, -14], [16, 6], [1.8, 1.7, 1.5]]]))
    print(Rn.render(sc, sys.argv[1]))
