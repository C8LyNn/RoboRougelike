import subprocess, sys, os, json, time
W = os.path.dirname(os.path.abspath(__file__)); R2 = os.path.dirname(W)
B = r"C:\Program Files (x86)\Steam\steamapps\common\Blender\blender.exe"
ALL = [('R4M_Wheel', 'build_wheel.py'), ('R4M_CrawlerTrack', 'build_crawlertrack.py'), ('R4M_HoverEngine', 'build_hoverengine.py'), ('R4M_OmniDrive', 'build_omnidrive.py'), ('R4M_Suspension', 'build_suspension.py'), ('R4M_Booster', 'build_booster.py'), ('R4M_JumpDrive', 'build_jumpdrive.py'), ('R4M_Blink', 'build_blink.py'), ('R4M_Gyroscope', 'build_gyroscope.py'), ('R4M_MagneticGrapple', 'build_magneticgrapple.py'), ('R4M_IndependentSteering', 'build_independentsteering.py')]
only = sys.argv[1].split(",") if len(sys.argv) > 1 and sys.argv[1] else None
log = open(os.path.join(R2, "_batch_log.txt"), "a", encoding="utf-8")
log.write("BATCH_START " + time.strftime("%Y-%m-%d %H:%M:%S") + " " + str(only) + "\n"); log.flush()
for nm, sc in ALL:
    if only and nm not in only: continue
    t = time.time(); od = os.path.join(R2, nm); os.makedirs(od, exist_ok=True)
    q = os.path.join(od, nm + "_QA.json")
    if os.path.exists(q): os.remove(q)
    r = subprocess.run([B, "-b", "--factory-startup", "--python", os.path.join(W, sc), "--", od], capture_output=True, text=True)
    open(os.path.join(od, "blender_stdout.log"), "w", encoding="utf-8").write(r.stdout[-200000:] + "\nSTDERR\n" + r.stderr[-20000:])
    try: d = json.load(open(q, encoding="utf-8")); st = (d["status"], d["failures"])
    except Exception as ex: st = ("NOJSON", str(ex), r.stderr[-300:])
    log.write(f"{nm} rc={r.returncode} {round(time.time()-t,1)}s {st}\n"); log.flush()
log.write("BATCH_DONE\n"); log.close()
