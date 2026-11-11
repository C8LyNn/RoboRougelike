"""step 5: offline viewer. preview/R4X_MODULES_PREVIEW.zip = index.html + models.js with the REAL exported GLB files embedded once (base64),
opens from disk without a server or internet. three.js r128 (MIT). usage: python3 build_viewer.py [--keep-stage]"""
import os, sys, json, base64, hashlib, shutil, zipfile
import mkit as K, paths_m as P, modules as MD, scenes
from concepts import NAME_PL, FUNC_PL, CONCEPT
OUT = os.path.join(P.PKG, "preview"); SRC = os.path.join(K.HERE, "viewer_src"); LIB = os.path.join(P.PILOT, "scripts", "render_web", "lib")
def b64(p): return base64.b64encode(open(p, "rb").read()).decode()
def sha(p): return hashlib.sha256(open(p, "rb").read()).hexdigest()
GT = [("A", "A · podstawowe"), ("B", "B · ruch i sterowanie"), ("C", "C · bonusy"), ("D", "D · walka / energia / obrona"), ("E", "E · informacja / ekonomia"), ("F", "F · wyrzutniki"), ("T", "Pułapki w świecie gry")]
def main(keep=False):
    stage = os.path.join(OUT, "_stage"); shutil.rmtree(stage, ignore_errors=True); os.makedirs(os.path.join(stage, "lib")); models = {}; src = {}; mods = {}
    def add(key, path):
        models[key] = b64(path); src[key] = {"file": os.path.relpath(path, os.path.dirname(P.PKG)).replace(os.sep, "/"), "sha256": sha(path), "bytes": os.path.getsize(path)}
    add("cube", P.cube_glb())
    for n, d in MD.ALL.items():
        i = ("R4D_" if d["group"] == "T" else "R4X_") + n; qa = json.load(open(os.path.join(P.mdir(i), i + "_QA.json"))); add(i, P.glb(i)); add("L/" + i, P.glb(i, True))
        mods[i] = dict(num=("T%d" % (d["id"] - 100)) if d["group"] == "T" else "%02d" % d["id"], name=n, pl=NAME_PL[n], func=FUNC_PL[n] + " Sylwetka: " + CONCEPT[n] + ".", tris=qa["triangles"], lod=qa["triangles_lod1"], glow=qa["glow_rgb"],
                       ports=[p["dir"] + ("" if p["kind"] == "full" else " (częściowy)") for p in qa["ports_proposal"]], mount=qa["mount_proposal"], world=qa["world_object"], nodes=sorted(qa["movable_nodes"]))
    groups = [dict(title=t, ids=[("R4D_" if g == "T" else "R4X_") + n for n, d in MD.ALL.items() if d["group"] == g]) for g, t in GT]
    liv = {k: scenes.LIVERY[k] for k in ("ember", "cobalt", "jade", "r41")}
    for k in liv:
        if liv[k]["hull"] is None: liv[k] = {"hull": None, "accent": None}
    data = dict(modules=mods, groups=groups, order=[i for g in groups for i in g["ids"]], livery=liv, liveryName={"r41": "domyślne materiały R41", "ember": "Ember", "cobalt": "Cobalt", "jade": "Jade"}, look=scenes.LOOK)
    with open(os.path.join(stage, "models.js"), "w") as f: f.write("window.R4_DATA=" + json.dumps(data, separators=(",", ":")) + ";\nwindow.R4_MODELS=" + json.dumps(models, separators=(",", ":")) + ";\n")
    for fn in os.listdir(LIB): shutil.copy(os.path.join(LIB, fn), os.path.join(stage, "lib", fn))
    for fn in os.listdir(SRC): shutil.copy(os.path.join(SRC, fn), os.path.join(stage, fn))
    z = os.path.join(OUT, "R4X_MODULES_PREVIEW.zip")
    with zipfile.ZipFile(z, "w", zipfile.ZIP_DEFLATED, compresslevel=9) as zf:
        for root, _, files in os.walk(stage):
            for fn in sorted(files):
                fp = os.path.join(root, fn); zi = zipfile.ZipInfo(os.path.relpath(fp, stage).replace(os.sep, "/"), date_time=(2026, 11, 11, 0, 0, 0)); zi.compress_type = zipfile.ZIP_DEFLATED; zf.writestr(zi, open(fp, "rb").read())
    json.dump(src, open(os.path.join(OUT, "embedded_models.json"), "w"), indent=1)
    print("viewer:", z, os.path.getsize(z), "bytes;", len(models), "GLB embedded")
    if not keep: shutil.rmtree(stage)
    return stage
if __name__ == "__main__": main("--keep-stage" in sys.argv)
