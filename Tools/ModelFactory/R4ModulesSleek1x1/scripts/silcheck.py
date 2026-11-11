"""numpy silhouette pre-check on the generator meshes (same metric as tests_modules.M6)"""
import numpy as np, mkit as K, modules as MD, precheck as PC, tests_modules as TM
M = {}
for n, d in MD.ALL.items():
    m = K.build(d["fn"]); T = PC.tris_of(m); env = np.array(d.get("env", [[-K.H]*3, [K.H]*3]))
    if d["group"] != "T": T = T[T.mean(1)[:, 2] > -0.185]
    M[n] = TM.masks(T, env)
bad = 0
for n in M:
    best = (0, None)
    for o in M:
        if o == n: continue
        v = min(float((a & b).sum()/max((a | b).sum(), 1)) for a, b in zip(M[n], M[o]))
        if v > best[0]: best = (v, o)
    if best[0] > 0.74: print(f"{n:28s} {best[1]:28s} {best[0]:.3f}" + ("  FAIL" if best[0] > 0.80 else "")); bad += best[0] > 0.80
print("fails:", bad)
