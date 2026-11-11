"""step 1: module definitions -> mesh_source/<ID>.json (pure python). usage: python3 gen_mesh_source.py [Name,...]"""
import json, os, sys, numpy as np
import mkit as K, modules as MD
OUT = os.path.join(K.PKG, "mesh_source")
def pack(m, pivot=(0, 0, 0)):
    if m is None: return None
    V = np.round(m["V"] - np.asarray(pivot, float), 6)
    return dict(verts=V.tolist(), faces=[dict(v=f["v"], n=np.round(np.array(f["n"]), 5).tolist(), uv=np.round(np.array(f["uv"]), 5).tolist(), zone=K.ZONE[f["group"]]) for f in m["faces"]])
def prefix(d): return "R4D_" if d["group"] == "T" else "R4X_"
def gen(name):
    d = MD.ALL[name]; m = K.build(d["fn"]); l = K.build(d["fn"], True); ID = prefix(d) + name; st = K.stats(m); ls = K.stats(l)
    assert st["ok"] and ls["ok"], name
    src = dict(id=ID, name=name, number=d["id"], group=d["group"], wave=d["wave"], name_pl=d["pl"], function_pl=d["function"], glow=list(d["glow"]), zone_default=K.ZONE_DEFAULT,
               ports=m["ports"], mount=d.get("mount") or (m["ports"][0]["dir"] if m["ports"] else "down"), empties=m["empties"], nodes=m["nodes"], world_object=d["group"] == "T",
               envelope=d.get("env", [[-K.H]*3, [K.H]*3]), meshes={n: pack(mm, (0, 0, 0) if n == "Body" else m["nodes"][n]["pivot"]) for n, mm in m["by_node"].items() if mm is not None},
               lod1=pack(dict(V=l["V"], faces=l["faces"])), stats=dict(triangles=st["triangles"], shells=st["shells"], lod1_triangles=ls["triangles"]), notes=d.get("notes", []))
    os.makedirs(OUT, exist_ok=True); json.dump(src, open(os.path.join(OUT, ID + ".json"), "w"), separators=(",", ":"))
    return ID, st["triangles"], ls["triangles"]
if __name__ == "__main__":
    names = sys.argv[1].split(",") if len(sys.argv) > 1 else list(MD.ALL)
    for n in names: print(*gen(n))
