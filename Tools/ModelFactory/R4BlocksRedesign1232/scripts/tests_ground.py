"""ground height under a robot = lowest GroundProbe / lowest vertex of its real wheel models"""
import numpy as np, glbio, paths
_c = {}
def ground_y(R):
    ys = []
    for p in R.parts:
        if p["type"] != "wheel": continue
        lib, name = p["model"].split(":")
        if name not in _c: _c[name] = float(glbio.load(paths.ref_glb(lib, name)).bounds()[0][1])
        ys.append(p["cell"][1]*0.55 + _c[name])
    return min(ys) if ys else None
