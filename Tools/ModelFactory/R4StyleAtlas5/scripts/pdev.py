"""pilot 2 dev look: one type, five constructions side by side. usage: python3 pdev.py out.jpg Type [view: 34|front|side|low]"""
import os, sys, numpy as np
import akit as K, astyle as ST, pdefs as PD, glbio, render_shot, adev
if __name__ == "__main__":
    typ = sys.argv[2]; view = sys.argv[3] if len(sys.argv) > 3 else "34"; yaw = {"34": -38, "front": 0, "side": -90, "low": -38, "back": 142}[view]; root = "/tmp/devatlas"; os.makedirs(root + "/glb", exist_ok=True)
    if not os.path.exists(root + "/web"): os.symlink(os.path.join(K.paths25.PILOT, "scripts", "render_web"), root + "/web")
    t = PD.TYPES[typ]; env = np.array(t["env"]); parts = []; sp = float(np.linalg.norm(env[1][:2] - env[0][:2]))*0.9 + 0.25
    for c, s in enumerate(ST.STYLES):
        m = K.build(PD.P[typ][s], s, False, t["env"], t["cid"], bool(t.get("source_clips")), t.get("planes"), True)
        fx = set(t.get("fx", [])); V = []; F = []
        for n, mm in m["by_node"].items():
            if mm is None or n in fx: continue
            b = sum(len(v) for v in V); V.append(mm["V"]); F += [dict(f, v=[i + b for i in f["v"]]) for f in mm["faces"]]
        mv = dict(V=np.concatenate(V), faces=F); cx, cy = (env[0][0] + env[1][0])/2, (env[0][1] + env[1][1])/2; a = np.radians(yaw); ca, sa = np.cos(a), np.sin(a)
        p = f"{root}/glb/P_{typ}_{s}.glb"; adev.write_glb(mv, p, s, t["glow"])     # gl: x = bx - cx, z = -(by - cy), then yaw about gl Y
        parts.append(dict(url=f"/glb/P_{typ}_{s}.glb", matrix=[ca, 0, sa, c*sp - ca*cx + sa*cy, 0, 1, 0, -env[0][2], -sa, 0, ca, sa*cx + ca*cy, 0, 0, 0, 1]))
    R = render_shot.Renderer(root); d = np.array(dict([("34", [0, 0.5, 1.0]), ("front", [0, 0.1, 1.0]), ("side", [0, 0.1, 1.0]), ("low", [0, -0.3, 1.0]), ("back", [0, 0.5, 1.0])])[view], float); d /= np.linalg.norm(d)
    tx = 2*sp; hy = (env[1][2] - env[0][2])/2; depth = env[1][1] - env[0][1]; W = 4*sp + sp; asp = 2.9; dist = W/2/(np.tan(np.radians(10))*asp)
    sc = dict(parts=parts, width=2000, height=int(2000/asp), target=[tx, hy, 0], camera=dict(pos=[tx + d[0]*dist, hy + d[1]*dist, d[2]*dist], fov=20), groundY=-0.002 if view != "low" else -5, shadowExtent=5,
              exposure=0.62, background=[0.19, 0.21, 0.24], groundColor=[0.16, 0.17, 0.19], keyIntensity=3.4, fillIntensity=0.5,
              env=dict(sky=[0.45, 0.52, 0.62], horizon=[0.62, 0.63, 0.63], ground=[0.10, 0.10, 0.11], panels=[[[8, 14, 6], [14, 8], [5, 5, 4.7]], [[-12, 6, -4], [10, 10], [1.6, 1.8, 2.1]], [[2, 5, -14], [16, 6], [1.8, 1.7, 1.5]]]))
    print(R.render(sc, sys.argv[1]))
