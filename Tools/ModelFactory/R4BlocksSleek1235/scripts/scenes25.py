"""Scene descriptions (camera / light / paint) shared by the #1235 preview renderer and the offline viewer.
Same look, liveries and conventions as scripts/scenes.py of the #1232 pilot."""
import os
import numpy as np
import paths25, layout25, glbio
import scenes as S1                                   # pilot: LOOK, LIVERY, P()

G = 0.55; LOOK = S1.LOOK; LIVERY = S1.LIVERY
REFURL = S1.REFURL
def module_url(model):
    lib, name = model.split(":"); return f"{REFURL[lib]}/{name}/{name}.glb"
def block_url(variant, kind, lod=False):
    if variant == "before": return f"{REFURL['blocks']}/R4B_{kind}/R4B_{kind}.glb"
    root = "/pilot" if kind in layout25.PILOT else "/pkg"
    return f"{root}/A_Sleek/R4B_{kind}/R4B_{kind}" + ("_LOD1" if lod else "") + ".glb"
def block_path(variant, kind, lod=False):
    if variant == "before": return paths25.ref_glb("blocks", f"R4B_{kind}")
    d = paths25.pilot_dir(kind) if kind in layout25.PILOT else paths25.new_dir(kind)
    return os.path.join(d, f"R4B_{kind}" + ("_LOD1" if lod else "") + ".glb")

def robot_parts(R, variant, livery="r41", lod=False):
    parts = []
    for p in R.parts:
        m = R.mat4(p).ravel().tolist()
        if p["type"] == "block":
            d = dict(url=block_url(variant, p["kind"], lod), matrix=m); pp = LIVERY[livery][p["paint"]]
            if pp: d["paint"] = {"Armor_Gunmetal": pp["R4P_Base"]} if variant == "before" else pp      # approved blocks: one paintable body material
            parts.append(d)
        else: parts.append(dict(url=module_url(p["model"]), matrix=m))
    return parts

def robot_bounds(R):
    C = np.array([c for p in R.parts for c in p.get("cells", [p["cell"]])], float)*G
    return C.min(0) - 0.3, C.max(0) + 0.3

_low = {}
def ground_y(R):
    """wheels: lowest point of the real wheel models; hover craft: 0.35 m under the lowest real module"""
    ys = []; wheels = False
    for p in R.parts:
        if p["type"] not in ("wheel", "movement"): continue
        lib, name = p["model"].split(":")
        if name not in _low: _low[name] = float(glbio.load(paths25.ref_glb(lib, name)).bounds()[0][1])
        ys.append(p["cell"][1]*G + _low[name]); wheels |= p["type"] == "wheel"
    if not ys: return float(robot_bounds(R)[0][1])
    return min(ys) - (0.0 if wheels else 0.35)

VIEWS = {"34front": ((0.55, 0.40, 0.74), 24, 1.55), "34rear": ((-0.56, 0.36, -0.74), 24, 1.55), "side": ((1.0, 0.13, 0.05), 22, 1.60), "top": ((0.02, 1.0, 0.06), 24, 1.45),
         "game": ((0.0, 0.62, -0.78), 40, 1.75), "close": ((0.75, 0.33, 0.58), 20, 0.62), "garage": ((0.62, 0.30, 0.72), 22, 0.80)}
def robot_scene(R, variant, livery, view, lod=False, focus=None, **kw):
    lo, hi = robot_bounds(R); ctr = (lo + hi)/2; size = float(np.linalg.norm(hi - lo)); gy = ground_y(R)
    V = VIEWS[view]; d = np.array(V[0]); d = d/np.linalg.norm(d); dist = size*V[2]
    tgt = ctr + np.array([0, -0.05*size*0.2, 0])
    if focus is not None: tgt = np.array(focus, float)*G
    sc = dict(parts=robot_parts(R, variant, livery, lod), target=tgt.tolist(), camera=dict(pos=(tgt + d*dist).tolist(), fov=V[1]), groundY=gy,
              shadowExtent=size*0.75, keyDir=[4.5*np.sign(d[0] or 1), 7, 3.5*np.sign(d[2] or 1)] if view != "game" else [4.5, 7, 3.5], **LOOK)
    if view == "game": sc.update(grid=[80, 80], fog=[size*2.5, size*9])
    sc.update(kw); return sc
