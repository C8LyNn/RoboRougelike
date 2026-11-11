"""#1235 step 1 (python3 + numpy + scipy [+ scikit-image for two non-convex caps]): generate the 25 A_Sleek meshes and write them as
plain JSON for the Blender build step. usage: python3 gen_mesh_source25.py [Kind,Kind...]"""
import os, sys, json, numpy as np
import paths25, shapes25, r4panel as RP
from styles import STYLES

A = STYLES["A"]; ZONE = A["zone"]
def clean(v):
    if isinstance(v, (np.floating,)): return float(v)
    if isinstance(v, (np.bool_,)): return bool(v)
    if isinstance(v, (np.integer,)): return int(v)
    return v

def dump(kind, lod):
    m = shapes25.build(kind, lod); stats = RP.mesh_stats(m); assert stats["ok"], (kind, lod, stats)
    faces = []
    for f in m["faces"]:
        if "uv" in f: uv = [[round(float(u[0]) + 0.5, 6), round(float(u[1]) + 0.5, 6)] for u in f["uv"]]
        else:
            n_pl = m["planes"][f["plane"]][0]; u, v, _ = RP.face_frame(n_pl); uv = [[round(float(m["V"][i] @ u + 0.5), 6), round(float(m["V"][i] @ v + 0.5), 6)] for i in f["v"]]
        faces.append({"v": [int(i) for i in f["v"]], "n": [[round(float(x), 6) for x in nn] for nn in f["n"]], "uv": uv, "group": f["group"], "zone": ZONE[f["group"]]})
    out = {"id": f"R4B_{kind}", "kind": kind, "direction": "A", "label": "A_Sleek", "lod": 1 if lod else 0, "issue": 1235, "wave": shapes25.WAVE_OF[kind],
           "axes": "Blender: X, Y back (-Y forward), Z up; metres", "style": {k: clean(v) for k, v in shapes25.A.items() if k not in ("zone", "zone_default", "label")},
           "zone_default": A["zone_default"], "solid": [{"hull": [[float(c) for c in p] for p in part["hull"]], "minus": part.get("minus", []), "sphere": part.get("sphere")} for part in shapes25.SOLID[kind]],
           "stats": {k: clean(v) for k, v in stats.items()}, "verts": [[round(float(x), 7) for x in p] for p in m["V"]], "faces": faces}
    path = os.path.join(paths25.PKG, "mesh_source", f"R4B_{kind}_A" + ("_LOD1" if lod else "") + ".json")
    json.dump(out, open(path, "w"), separators=(",", ":")); return stats

if __name__ == "__main__":
    os.makedirs(os.path.join(paths25.PKG, "mesh_source"), exist_ok=True)
    kinds = sys.argv[1].split(",") if len(sys.argv) > 1 else list(shapes25.ALL)
    for kind in kinds:
        s0 = dump(kind, False); s1 = dump(kind, True)
        print(f"wave {shapes25.WAVE_OF[kind]} {kind:12s} LOD0 {s0['triangles']:5d} tris {s0['vertices']:5d} verts | LOD1 {s1['triangles']:4d} tris")
