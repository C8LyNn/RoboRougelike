"""Scene descriptions (camera / light / paint) shared by the preview renderer and the offline viewer."""
import numpy as np, layout
G = 0.55
REFURL = {"blocks": "/ref/blocks/Tools/ModelFactory/R4Blocks", "weapons": "/ref/weapons/Tools/ModelFactory/R4Weapons938", "movement": "/ref/movement/Tools/ModelFactory/R4Movement940",
          "wheels": "/ref/wheels/Tools/ModelFactory/R4Wheels", "connectors": "/ref/connectors/Tools/ModelFactory/R4ConnectorRods1182"}
LABEL = {"A": "A_Sleek", "B": "B_Industrial"}
def module_url(model):
    lib, name = model.split(":"); return f"{REFURL[lib]}/{name}/{name}.glb"
def block_url(variant, kind, lod=False):
    if variant == "before": return f"{REFURL['blocks']}/R4B_{kind}/R4B_{kind}.glb"
    return f"/pkg/{LABEL[variant]}/R4B_{kind}/R4B_{kind}" + ("_LOD1" if lod else "") + ".glb"

# paint presets (PROPOSAL / demo only - no paint mechanic is added): zone -> (r, g, b, metallic, roughness), linear values
DARK = (0.035, 0.038, 0.045)
def P(base, sec, trim=DARK, bm=0.25, br=0.45, sm=0.30, sr=0.50):
    return {"R4P_Base": [*base, bm, br], "R4P_Secondary": [*sec, sm, sr], "R4P_Trim": [*trim, 0.55, 0.45]}
BELLY_A = P((0.13, 0.14, 0.155), (0.06, 0.065, 0.075))
BELLY_B = P((0.16, 0.17, 0.18), DARK, bm=0.35, br=0.55, sm=0.55, sr=0.45)
LIVERY = {
 "r41": {k: None for k in ("hull", "accent", "belly")},                  # the R41 default colours stored in the files
 # for direction A: vivid, glossy, piping = darker tone of the plate colour
 "ember": {"hull": P((0.80, 0.78, 0.72), (0.50, 0.49, 0.46), br=0.38), "accent": P((0.72, 0.045, 0.03), (0.38, 0.025, 0.02), br=0.38), "belly": BELLY_A},
 "cobalt": {"hull": P((0.03, 0.17, 0.58), (0.015, 0.085, 0.30), br=0.38), "accent": P((0.95, 0.60, 0.03), (0.55, 0.33, 0.02), br=0.40), "belly": BELLY_A},
 "jade": {"hull": P((0.02, 0.36, 0.30), (0.01, 0.19, 0.16), br=0.38), "accent": P((0.80, 0.78, 0.72), (0.50, 0.49, 0.46), br=0.38), "belly": BELLY_A},
 # for direction B: restrained R41 neutrals on the dark chassis frame, colour only as a team accent
 "steel": {"hull": P((0.27, 0.28, 0.285), DARK, bm=0.30, br=0.58, sm=0.55, sr=0.45), "accent": P((0.60, 0.035, 0.025), DARK, bm=0.35, br=0.38, sm=0.55, sr=0.45), "belly": BELLY_B},
 "bone": {"hull": P((0.62, 0.60, 0.54), DARK, bm=0.20, br=0.55, sm=0.55, sr=0.45), "accent": P((0.95, 0.60, 0.03), DARK, bm=0.15, br=0.50, sm=0.55, sr=0.45), "belly": BELLY_B},
}
LOOK = dict(exposure=0.62, background=[0.19, 0.21, 0.24], groundColor=[0.16, 0.17, 0.19], keyIntensity=3.4, fillIntensity=0.5,
            env=dict(sky=[0.45, 0.52, 0.62], horizon=[0.62, 0.63, 0.63], ground=[0.10, 0.10, 0.11], panels=[[[8, 14, 6], [14, 8], [5, 5, 4.7]], [[-12, 6, -4], [10, 10], [1.6, 1.8, 2.1]], [[2, 5, -14], [16, 6], [1.8, 1.7, 1.5]]]))

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
    C = np.array([p["cell"] for p in R.parts] + [c for p in R.parts for c in p.get("cells", [])], float)*G
    return C.min(0) - 0.3, C.max(0) + 0.3

def robot_scene(R, variant, livery, view, lod=False, **kw):
    lo, hi = robot_bounds(R); ctr = (lo + hi)/2; size = float(np.linalg.norm(hi - lo)); gy = float(lo[1]) - 0.26 if not any(p["type"] == "wheel" for p in R.parts) else None
    if gy is None:
        import tests_ground; gy = tests_ground.ground_y(R)
    V = {"34front": ((0.55, 0.40, 0.74), 24, 1.55), "34rear": ((-0.56, 0.36, -0.74), 24, 1.55), "side": ((1.0, 0.13, 0.05), 22, 1.60), "top": ((0.02, 1.0, -0.06), 24, 1.45),
         "game": ((0.0, 0.62, -0.78), 40, 1.75), "close": ((0.75, 0.33, 0.58), 20, 0.62)}[view]
    d = np.array(V[0]); d = d/np.linalg.norm(d); dist = size*V[2]
    tgt = ctr + (np.array([0.0, -0.12, 0.55])*size*0.12 if view == "close" else np.array([0, -0.05*size*0.2, 0]))
    sc = dict(parts=robot_parts(R, variant, livery, lod), target=tgt.tolist(), camera=dict(pos=(tgt + d*dist).tolist(), fov=V[1]), groundY=gy,
              shadowExtent=size*0.75, keyDir=[4.5*np.sign(d[0] or 1), 7, 3.5*np.sign(d[2] or 1)] if view != "game" else [4.5, 7, 3.5], **LOOK)
    if view == "game": sc.update(grid=[60, 60], fog=[size*2.5, size*9])
    sc.update(kw); return sc
