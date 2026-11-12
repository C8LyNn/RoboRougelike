"""#1239 Hero Benchmark Pass 2: orchestrates the whole build on the workstation (models in batches -> pbuild -> scenes -> archive). Runs inside a
background Blender used as the Python host. usage: blender -b --factory-startup --python h2run.py -- WORK SCRIPTS SCENE H1 REF RUN"""
import bpy, sys, os, subprocess, time, json, zipfile
WORK, SCR, SCN, H1, REF, RUN = sys.argv[sys.argv.index("--") + 1:][:6]; B = bpy.app.binary_path; ST = "ABCDE"; LOGS = os.path.join(WORK, RUN + "_logs"); os.makedirs(LOGS, exist_ok=True); STATUS = os.path.join(WORK, RUN + "_status.json"); S = {"steps": [], "t0": time.time()}
def note(k, **v): S["steps"].append(dict(step=k, t=round(time.time() - S["t0"]), **v)); json.dump(S, open(STATUS, "w"), indent=1)
def spawn(tag, args): lg = open(os.path.join(LOGS, tag + ".log"), "w"); return subprocess.Popen([B, "-b", "--factory-startup", "--python"] + args, stdout=lg, stderr=subprocess.STDOUT, creationflags=0x08000000)
def wait(ps):
    for p in ps: p.wait()
def grep(tag, keys): return [l[:260] for l in open(os.path.join(LOGS, tag + ".log")).read().splitlines() if any(k in l for k in keys)]
M = os.path.join(WORK, RUN, "m"); V = os.path.join(WORK, RUN, "variants")
for typ, size in (("Wheel", "2048"), ("Gun", "2048"), ("Battery", "1024")):
    wait([spawn(f"model_{typ}_{s}", [os.path.join(SCR, "h2model.py"), "--", M, os.path.join(SCR, "tpl"), typ, s, size]) for s in ST])
    note("models_" + typ, result={s: ("OK" if grep(f"model_{typ}_{s}", ["MODEL_DONE"]) else "FAIL") for s in ST}, violations={s: [l[l.find("'violations'"):][:200] for l in grep(f"model_{typ}_{s}", ["MODEL_DONE"]) if "'violations': []" not in l] for s in ST})
wait([spawn("build_" + s, [os.path.join(SCR, "pbuild.py"), "--", V, os.path.join(M, "mesh_source"), REF, "R4W_Basic,R4M_WheelM_Side_Offroad,R4X_Battery", s]) for s in ST])
note("pbuild", result={s: [l[9:] for l in grep("build_" + s, ["VARIANT", "BUILD_DONE"])] for s in ST})
def scene(tag, shots, out): return [spawn(f"{tag}_{s}", [os.path.join(SCN, "h2scene.py"), "--", os.path.join(WORK, RUN, out), os.path.join(SCN, "scene_source"), os.path.join(H1, "in2", "kit_source"), os.path.join(SCN, shots), os.path.join(H1, "v3"), os.path.join(H1, "v3"), V, os.path.join(H1, "in1", "textures"), os.path.join(WORK, RUN, "kit"), s]) for s in ST]
wait(scene("scene", "shots.json", "renders") + scene("neutral", "shots_neutral.json", "renders_neutral"))
note("scenes", result={s: bool(grep("scene_" + s, ["ALL_DONE"])) and bool(grep("neutral_" + s, ["ALL_DONE"])) for s in ST})
def pack(name, pairs):
    z = zipfile.ZipFile(os.path.join(WORK, RUN + "_" + name + ".zip"), "w", zipfile.ZIP_DEFLATED); n = 0
    for root_, arc in pairs:
        for r, _, fs in os.walk(root_):
            for f in fs: p = os.path.join(r, f); z.write(p, arc + "/" + os.path.relpath(p, root_).replace(os.sep, "/")); n += 1
    z.close(); return [n, round(os.path.getsize(os.path.join(WORK, RUN + "_" + name + ".zip"))/1e6, 1)]
out = {"variants": pack("variants", [(V, "variants")]), "textures": pack("textures", [(os.path.join(M, "textures"), "textures")]), "source": pack("source", [(os.path.join(M, "source"), "source")]),
       "misc": pack("misc", [(os.path.join(M, "preview"), "preview"), (os.path.join(M, "report"), "report"), (os.path.join(WORK, RUN, "renders"), "renders"), (os.path.join(WORK, RUN, "renders_neutral"), "renders_neutral"), (LOGS, "logs")])}
note("ALL_DONE", zips=out)
