#!/usr/bin/env python3
"""Fail-closed evidence validator for the optional live-game frame interval capture.
No native-player/performance PASS is possible from this parser alone.
"""
import argparse,csv,hashlib,json,math
from pathlib import Path
class CaptureError(ValueError): pass

def need(ok,text):
    if not ok: raise CaptureError(text)
def nonnegative(v):
    try: x=float(v)
    except (ValueError,TypeError): raise CaptureError("not numeric") from None
    need(math.isfinite(x) and x>=0,"invalid numeric")
    return x
def rows(path,fields):
    with path.open(newline="",encoding="utf-8-sig") as f:
        reader=csv.DictReader(f)
        need(reader.fieldnames==fields,"invalid header "+path.name)
        data=list(reader)
        need(all(None not in r and None not in r.values() for r in data),"malformed CSV")
        return data
def audit(folder,game_sha,build_sha,scenario):
    folder=Path(folder)
    for f in ("receipt.json","frames.csv","objects.csv"):
        need((folder/f).is_file(),"missing "+f)
    meta=json.loads((folder/"receipt.json").read_text(encoding="utf-8-sig"))
    need(meta.get("classification")=="GAME_FRAME_TIME_REVIEW_REQUIRED_NOT_DISPLAY_PRESENTMON_OR_NATIVE_PERF_PASS","classification mismatch")
    need(meta.get("game_sha")==game_sha and len(game_sha)==40,"game SHA mismatch")
    need(meta.get("build_zip_sha256")==build_sha and len(build_sha)==64,"build SHA mismatch")
    need(meta.get("scenario_id")==scenario,"scenario mismatch")
    need(meta.get("unity_version")=="6000.3.14f1","wrong Unity version")
    need(meta.get("capture_complete") is True,"incomplete capture")
    need(nonnegative(meta.get("observed_capture_seconds"))>=120,"duration below 120s")
    need(meta.get("profiler_main_render_gpu_gc")=="NOT_MEASURED","fake Unity profiler metrics")
    frames=rows(folder/"frames.csv",["frame_index","elapsed_ms","delta_ms"])
    scans=rows(folder/"objects.csv",["elapsed_ms","scene","active_parts","active_projectiles","active_enemies","rigidbodies","scan_ms"])
    need(len(frames)==meta.get("frame_rows") and len(frames)>=120,"frame row count mismatch")
    need(len(scans)==meta.get("object_scans") and len(scans)>=90,"insufficient object scans")
    previous=-1
    for index,row in enumerate(frames):
        need(row["frame_index"]==str(index),"bad frame index")
        age=nonnegative(row["elapsed_ms"]);delta=nonnegative(row["delta_ms"])
        need(age>previous and age<120000,"nonmonotonic/outside capture interval")
        need(delta>0,"zero frame interval")
        previous=age
    previous=-1
    for row in scans:
        age=nonnegative(row["elapsed_ms"])
        need(age>previous and age<120000,"nonmonotonic object scan")
        need(row["scene"]!="","missing scene identity")
        for key in ("active_parts","active_projectiles","active_enemies","rigidbodies"):
            need(row[key].isdecimal(),"invalid object count "+key)
        nonnegative(row["scan_ms"])
        previous=age
    return {"status":"LIVE_RAW_REVIEW_REQUIRED_NOT_PERFORMANCE_ACCEPTANCE",
        "frame_rows":len(frames),"object_scans":len(scans),"scenario":scenario,
        "raw_sha256":{f:hashlib.sha256((folder/f).read_bytes()).hexdigest()
            for f in ("receipt.json","frames.csv","objects.csv")},
        "limits":"Time.unscaledDeltaTime is not presented frame interval. Verify original Player.log, build source, process exit, hardware and genuine gameplay."}

if __name__=="__main__":
    p=argparse.ArgumentParser()
    p.add_argument("--folder",required=True);p.add_argument("--game-sha",required=True)
    p.add_argument("--build-sha",required=True);p.add_argument("--scenario",required=True)
    p.add_argument("--out",required=True)
    a=p.parse_args()
    result=audit(a.folder,a.game_sha,a.build_sha,a.scenario)
    out=Path(a.out);need(not out.exists(),"refuse output overwrite")
    out.write_text(json.dumps(result,indent=2,sort_keys=True)+"\n")
    print(result["status"])
