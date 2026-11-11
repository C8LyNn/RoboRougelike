"""Step 1 (python3 + numpy + scipy): generate the pilot meshes and write them as plain JSON for the Blender build step.
   usage: python3 gen_mesh_source.py"""
import os, json, numpy as np
import r4panel as R, shapes, paths
from styles import STYLES

def dump(kind, key, lod):
    st = STYLES[key]; pts = shapes.SOLIDS[kind]
    m = R.lod_build(pts, st) if lod else R.build(pts, st)
    stats = R.mesh_stats(m); assert stats["ok"], (kind, key, lod, stats)
    faces = []
    for f in m["faces"]:
        n_pl = m["planes"][f["plane"]][0]; u, v, _ = R.face_frame(n_pl)
        faces.append({"v": [int(i) for i in f["v"]], "n": [[round(float(x), 6) for x in nn] for nn in f["n"]],
                      "uv": [[round(float(m["V"][i] @ u + 0.5), 6), round(float(m["V"][i] @ v + 0.5), 6)] for i in f["v"]],
                      "group": f["group"], "zone": st["zone"][f["group"]], "smooth": bool(st["seg"] > 1 and f["group"] in ("bevel", "corner")) or bool(st.get("wall_smooth") and f["group"] == "plate_wall")})
    out = {"id": f"R4B_{kind}", "kind": kind, "direction": key, "label": st["label"], "lod": 1 if lod else 0, "axes": "Blender: X, Y back (-Y forward), Z up; metres",
           "style": {k: v for k, v in st.items() if k not in ("zone", "zone_default", "label")}, "zone_default": st["zone_default"],
           "solid_points": [list(p) for p in pts], "stats": {k: (float(v) if isinstance(v, (np.floating,)) else (bool(v) if isinstance(v, (np.bool_,)) else v)) for k, v in stats.items()},
           "verts": [[round(float(x), 7) for x in p] for p in m["V"]], "faces": faces}
    path = os.path.join(paths.PKG, "mesh_source", f"R4B_{kind}_{key}" + ("_LOD1" if lod else "") + ".json")
    json.dump(out, open(path, "w"), separators=(",", ":")); return stats

if __name__ == "__main__":
    os.makedirs(os.path.join(paths.PKG, "mesh_source"), exist_ok=True)
    for kind in shapes.PILOT:
        for key in STYLES:
            s0 = dump(kind, key, False); s1 = dump(kind, key, True)
            print(f"{kind:12s} {key}  LOD0 {s0['triangles']:4d} tris {s0['vertices']:4d} verts | LOD1 {s1['triangles']:3d} tris")
