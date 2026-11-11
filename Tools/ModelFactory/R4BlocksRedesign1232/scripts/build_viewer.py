"""#1232 pilot - offline viewer: preview/index.html + models.js with the REAL GLB files embedded (base64), so it opens from disk
without a server or internet. three.js r128 (MIT) from lib/. usage: python3 build_viewer.py"""
import os, sys, json, base64, hashlib, shutil, zipfile
import numpy as np
import paths, layout, scenes, shapes, tests_ground

OUT = os.path.join(paths.PKG, "preview"); SRC = os.path.join(paths.HERE, "viewer_src")
def b64(p): return base64.b64encode(open(p, "rb").read()).decode()
def sha(p): return hashlib.sha256(open(p, "rb").read()).hexdigest()

def main():
    stage = os.path.join(OUT, "_stage"); shutil.rmtree(stage, ignore_errors=True); os.makedirs(os.path.join(stage, "lib"))
    models, src = {}, {}
    def add(key, path, origin):
        models[key] = b64(path); src[key] = {"file": origin, "sha256": sha(path), "bytes": os.path.getsize(path)}
    for kind in shapes.PILOT:
        add(f"before/{kind}", paths.ref_glb("blocks", f"R4B_{kind}"), f"PR #963 @{paths.REF_SHA['blocks'][:7]} R4Blocks/R4B_{kind}/R4B_{kind}.glb")
        for v, lab in scenes.LABEL.items():
            for lod in ("", "_LOD1"):
                p = os.path.join(paths.PKG, lab, f"R4B_{kind}", f"R4B_{kind}{lod}.glb"); add(f"{v}{lod}/{kind}", p, os.path.relpath(p, paths.PKG).replace(os.sep, "/"))
    robots = {}
    for rname, fn in layout.ROBOTS.items():
        Rb = fn(); parts = []
        for p in Rb.parts:
            m = [round(float(x), 6) for x in Rb.mat4(p).ravel()]
            if p["type"] == "block": parts.append({"k": p["kind"], "m": m, "p": p["paint"]})
            else:
                lib, name = p["model"].split(":")
                if p["model"] not in models: add(p["model"], paths.ref_glb(lib, name), f"{lib} @{paths.REF_SHA[lib][:7]} {name}.glb")
                parts.append({"u": p["model"], "m": m})
        lo, hi = scenes.robot_bounds(Rb)
        robots[rname] = {"parts": parts, "ground": tests_ground.ground_y(Rb), "center": [float(x) for x in (lo + hi)/2], "size": float(np.linalg.norm(hi - lo)), "count": Rb.count()}
    data = {"robots": robots, "livery": scenes.LIVERY, "look": scenes.LOOK, "kinds": shapes.PILOT, "sources": src}
    with open(os.path.join(stage, "models.js"), "w") as f:
        f.write("window.R4_DATA=" + json.dumps(data, separators=(",", ":")) + ";\nwindow.R4_MODELS=" + json.dumps(models, separators=(",", ":")) + ";\n")
    for fn_ in os.listdir(os.path.join(paths.HERE, "render_web", "lib")):
        if fn_ != "GLTFLoader.js" or True: shutil.copy(os.path.join(paths.HERE, "render_web", "lib", fn_), os.path.join(stage, "lib", fn_))
    for fn_ in os.listdir(SRC): shutil.copy(os.path.join(SRC, fn_), os.path.join(stage, fn_))
    z = os.path.join(OUT, "BLOCKS_1232_PREVIEW.zip")
    with zipfile.ZipFile(z, "w", zipfile.ZIP_DEFLATED, compresslevel=9) as zf:
        for root, _, files in os.walk(stage):
            for fn_ in sorted(files):
                fp = os.path.join(root, fn_); zi = zipfile.ZipInfo(os.path.relpath(fp, stage).replace(os.sep, "/"), date_time=(2026, 11, 11, 0, 0, 0)); zi.compress_type = zipfile.ZIP_DEFLATED
                zf.writestr(zi, open(fp, "rb").read())
    json.dump(src, open(os.path.join(OUT, "embedded_models.json"), "w"), indent=1)
    print("viewer:", z, os.path.getsize(z), "bytes;", len(models), "GLB embedded"); return stage

if __name__ == "__main__": main()
