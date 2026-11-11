"""#1235 - offline viewer: preview/R4_SLEEK_30_PREVIEW.zip = index.html + models.js with the REAL GLB files embedded once (base64),
so it opens from disk without a server or internet. three.js r128 (MIT). usage: R4_REF=<refs> python3 build_viewer25.py"""
import os, sys, json, base64, hashlib, shutil, zipfile
import numpy as np
import paths25, layout25 as L, scenes25, glbio

OUT = os.path.join(paths25.PKG, "preview"); SRC = os.path.join(paths25.HERE, "viewer_src"); LIB = os.path.join(paths25.PILOT, "scripts", "render_web", "lib")
def b64(p): return base64.b64encode(open(p, "rb").read()).decode()
def sha(p): return hashlib.sha256(open(p, "rb").read()).hexdigest()

def main(keep_stage=False):
    stage = os.path.join(OUT, "_stage"); shutil.rmtree(stage, ignore_errors=True); os.makedirs(os.path.join(stage, "lib"))
    models, src, extent, budget = {}, {}, {}, {}
    def add(key, path, origin):
        models[key] = b64(path); src[key] = {"file": origin, "sha256": sha(path), "bytes": os.path.getsize(path)}
    for kind in L.ALL30:
        add(f"before/{kind}", scenes25.block_path("before", kind), f"PR #963 @{paths25.REF_SHA['blocks'][:7]} R4Blocks/R4B_{kind}/R4B_{kind}.glb")
        for lod in (False, True):
            p = scenes25.block_path("A", kind, lod); add(f"A{'_LOD1' if lod else ''}/{kind}", p, os.path.relpath(p, os.path.dirname(paths25.PKG)).replace(os.sep, "/"))
        c = np.array(L.INFO[kind]["cells"], float)*0.55; extent[kind] = [(c.min(0) - 0.26).round(4).tolist(), (c.max(0) + 0.26).round(4).tolist()]
        budget[kind] = [glbio.load(scenes25.block_path("before", kind)).tri_count(), glbio.load(scenes25.block_path("A", kind)).tri_count(), glbio.load(scenes25.block_path("A", kind, True)).tri_count()]
    robots = {}
    for rname, fn in L.ROBOTS.items():
        Rb = fn(); parts = []
        for p in Rb.parts:
            m = [round(float(x), 6) for x in Rb.mat4(p).ravel()]
            if p["type"] == "block": parts.append({"k": p["kind"], "m": m, "p": p["paint"]})
            else:
                lib, name = p["model"].split(":")
                if p["model"] not in models: add(p["model"], paths25.ref_glb(lib, name), f"{lib} @{paths25.REF_SHA[lib][:7]} {name}.glb")
                parts.append({"u": p["model"], "m": m})
        lo, hi = scenes25.robot_bounds(Rb)
        robots[rname] = {"parts": parts, "ground": scenes25.ground_y(Rb), "center": [float(x) for x in (lo + hi)/2], "size": float(np.linalg.norm(hi - lo)), "count": Rb.count()}
    liv = {k: scenes25.LIVERY[k] for k in ("r41", "ember", "cobalt", "jade")}
    data = {"robots": robots, "livery": liv, "look": scenes25.LOOK, "kinds": L.ALL30, "extent": extent, "budget": budget}
    with open(os.path.join(stage, "models.js"), "w") as f:
        f.write("window.R4_DATA=" + json.dumps(data, separators=(",", ":")) + ";\nwindow.R4_MODELS=" + json.dumps(models, separators=(",", ":")) + ";\n")
    for fn_ in os.listdir(LIB): shutil.copy(os.path.join(LIB, fn_), os.path.join(stage, "lib", fn_))
    for fn_ in os.listdir(SRC): shutil.copy(os.path.join(SRC, fn_), os.path.join(stage, fn_))
    z = os.path.join(OUT, "R4_SLEEK_30_PREVIEW.zip")
    with zipfile.ZipFile(z, "w", zipfile.ZIP_DEFLATED, compresslevel=9) as zf:
        for root, _, files in os.walk(stage):
            for fn_ in sorted(files):
                fp = os.path.join(root, fn_); zi = zipfile.ZipInfo(os.path.relpath(fp, stage).replace(os.sep, "/"), date_time=(2026, 11, 11, 0, 0, 0)); zi.compress_type = zipfile.ZIP_DEFLATED
                zf.writestr(zi, open(fp, "rb").read())
    json.dump(src, open(os.path.join(OUT, "embedded_models.json"), "w"), indent=1)
    print("viewer:", z, os.path.getsize(z), "bytes;", len(models), "GLB embedded once;", sum(v["bytes"] for v in src.values()), "bytes of GLB")
    if not keep_stage: shutil.rmtree(stage)
    return stage

if __name__ == "__main__": main("--keep-stage" in sys.argv)
