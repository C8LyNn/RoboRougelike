
import subprocess, sys, os, json, time
W = r"C:\Users\kubap\Desktop\Blender_Modele\wynik\R4_Weapons_938\scripts"
R = r"C:\Users\kubap\Desktop\Blender_Modele\wynik\R4_Weapons_938"
B = r"C:\Program Files (x86)\Steam\steamapps\common\Blender\blender.exe"
ALL = [("R4W_Basic", "build_basic.py"), ("R4W_RotarySaw", "build_rotarysaw.py"), ("R4W_Heavy", "build_heavy.py"), ("R4W_Shotgun", "build_shotgun.py"),
       ("R4W_Railgun", "build_railgun.py"), ("R4W_BeamCutter", "build_beamcutter.py"), ("R4W_RocketLauncher", "build_rocketlauncher.py"), ("R4W_EmpEmitter", "build_empemitter.py")]
log = open(os.path.join(R, "_batch_log.txt"), "w", encoding="utf-8")
for nm, sc in ALL:
    t = time.time()
    r = subprocess.run([B, "-b", "--factory-startup", "--python", os.path.join(W, sc), "--", os.path.join(R, nm)], capture_output=True, text=True)
    open(os.path.join(R, nm, "blender_stdout.log"), "w", encoding="utf-8").write(r.stdout[-200000:] + "\nSTDERR\n" + r.stderr[-20000:])
    try: d = json.load(open(os.path.join(R, nm, nm + "_QA.json"), encoding="utf-8")); st = (d["status"], d["failures"], d.get("r41_compare", "")[:60])
    except Exception as ex: st = ("NOJSON", str(ex))
    log.write(f"{nm} rc={r.returncode} {round(time.time()-t,1)}s {st}\n"); log.flush()
log.write("BATCH_DONE\n"); log.close()
