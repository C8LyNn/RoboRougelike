
import subprocess, os, json, time
W = r"%USERPROFILE%\Desktop\Blender_Modele\wynik\R4_Movement_940\scripts"; R = os.path.dirname(W)
B = r"C:\Program Files (x86)\Steam\steamapps\common\Blender\blender.exe"
ALL = [("R4M_Wheel", "build_wheel.py"), ("R4M_CrawlerTrack", "build_crawlertrack.py"), ("R4M_HoverEngine", "build_hoverengine.py"), ("R4M_OmniDrive", "build_omnidrive.py"),
       ("R4M_Suspension", "build_suspension.py"), ("R4M_Booster", "build_booster.py"), ("R4M_JumpDrive", "build_jumpdrive.py"), ("R4M_Blink", "build_blink.py"),
       ("R4M_Gyroscope", "build_gyroscope.py"), ("R4M_MagneticGrapple", "build_magneticgrapple.py"), ("R4M_IndependentSteering", "build_independentsteering.py")]
log = open(os.path.join(R, "_batch_log.txt"), "w", encoding="utf-8")
for nm, sc in ALL:
    t = time.time()
    r = subprocess.run([B, "-b", "--factory-startup", "--python", os.path.join(W, sc), "--", os.path.join(R, nm)], capture_output=True, text=True)
    open(os.path.join(R, nm, "blender_stdout.log"), "w", encoding="utf-8").write(r.stdout[-150000:] + "\nSTDERR\n" + r.stderr[-20000:])
    try: d = json.load(open(os.path.join(R, nm, nm + "_QA.json"), encoding="utf-8")); st = (d["status"], d["failures"], d.get("video"))
    except Exception as ex: st = ("NOJSON", str(ex))
    log.write(f"{nm} rc={r.returncode} {round(time.time()-t,1)}s {st}\n"); log.flush()
log.write("BATCH_DONE\n"); log.close()
