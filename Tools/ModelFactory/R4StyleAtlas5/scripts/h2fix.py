"""#1239 Hero Benchmark Pass 2: repair of already baked mesh_source files without baking again (node vertices relative to their pivots, LOD1 rebuilt).
usage: blender -b --factory-startup --python h2fix.py -- MESH_SOURCE_DIR"""
import bpy, sys, os, json, glob
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import hf, hlod
D = sys.argv[sys.argv.index("--") + 1]
for p in sorted(glob.glob(os.path.join(D, "*", "*.json"))):
    src = json.load(open(p)); bpy.ops.wm.read_factory_settings(use_empty=True); col = bpy.context.scene.collection; piv = lambda n: (0, 0, 0) if n == "Body" else tuple(src["nodes"][n]["pivot"]); solid = [n for n in src["meshes"] if n not in src["fx"]]
    if not src.get("pivot_relative"):
        for n in solid:
            q = piv(n); src["meshes"][n]["verts"] = [[round(c - k, 5) for c, k in zip(v, q)] for v in src["meshes"][n]["verts"]]
        src["pivot_relative"] = True
    o = hlod.from_json([(src["meshes"][n], piv(n)) for n in solid], "LO_ALL", col); hf.shade(o, 38, False); lod, info = hlod.lod1(o, col)
    src["lod1"] = hlod.mesh_json(lod, zone_of=lambda name: name.split(".")[0]); src["stats"]["triangles_lod1"] = info["triangles"]; src["stats"]["lod1"] = info
    json.dump(src, open(p, "w"), separators=(",", ":")); print("[HERO2] FIXED", os.path.basename(os.path.dirname(p)), src["id"], info, hlod.report(o), flush=True)
print("[HERO2] FIX_DONE", flush=True)
