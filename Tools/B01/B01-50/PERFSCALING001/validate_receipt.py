#!/usr/bin/env python3
"""B01-50 PERFSCALING001: validate original Unity Physics proxy capture receipts.

Never infers a real Unity run from a synthetic/unit-test receipt.
"""
import argparse
import csv
import json
import math
import statistics
from pathlib import Path
from collections import defaultdict
from hashlib import sha256

EXPECTED={
    "baseline_24_0_0":(24,0,0),
    "parts_72_0_0":(72,0,0),
    "parts_144_0_0":(144,0,0),
    "projectiles_24_32_0":(24,32,0),
    "projectiles_24_128_0":(24,128,0),
    "enemies_24_0_8":(24,0,8),
    "enemies_24_0_32":(24,0,32),
    "combined_144_128_32":(144,128,32),
}
SAMPLE_COLS=("case_id","repetition","step","simulate_ms")
SUMMARY_COLS=("case_id","repetition","parts","projectiles","enemy_proxies","compound_colliders","rigidbodies","p50_ms","p95_ms","p99_ms","mean_ms","managed_alloc_bytes","gc0_collections")

class ReceiptError(ValueError): pass

def require(condition, message):
    if not condition: raise ReceiptError(message)

def realnum(raw, name):
    try: value=float(raw)
    except (ValueError,TypeError): raise ReceiptError("invalid number "+name) from None
    require(math.isfinite(value) and value>=0, "invalid nonnegative "+name)
    return value

def natural(raw,name):
    try: value=int(raw)
    except (ValueError,TypeError): raise ReceiptError("invalid integer "+name) from None
    require(value>=0 and str(value)==str(raw),"noncanonical integer "+name)
    return value

def rows(path,header):
    with Path(path).open(newline="",encoding="utf-8-sig") as f:
        reader=csv.DictReader(f)
        require(reader.fieldnames==list(header),"wrong CSV header "+path.name)
        out=list(reader)
    for row in out:
        require(None not in row and None not in row.values(),"extra or missing CSV cell")
    return out

def quantile(values,q):
    values=sorted(values)
    index=(len(values)-1)*q
    lo=math.floor(index);hi=math.ceil(index)
    return values[lo]+(values[hi]-values[lo])*(index-lo)

def sha40(s):
    return isinstance(s,str) and len(s)==40 and all(c in "0123456789abcdef" for c in s)

def audit(folder,source_sha,fixture_sha):
    folder=Path(folder)
    require(sha40(source_sha) and sha40(fixture_sha),"invalid expected SHA")
    required=("receipt.json","simulations.csv","summary.csv")
    for filename in required: require((folder/filename).is_file(),"missing "+filename)
    receipt=json.loads((folder/"receipt.json").read_text(encoding="utf-8-sig"))
    require(receipt.get("classification")=="NATIVE_PHYSICS_PROXY_CAPTURE_REVIEW_REQUIRED_NOT_GAME_FPS","wrong classification")
    require(receipt.get("source_sha")==source_sha,"source SHA mismatch")
    require(receipt.get("fixture_sha")==fixture_sha,"fixture SHA mismatch")
    require(receipt.get("unity_version")=="6000.3.14f1","wrong Unity version")
    require(bool(receipt.get("cpu")) and bool(receipt.get("os")),"no host evidence")
    for key in ("native_player_fps","real_enemy_ai","real_r4_projectiles","gpu_main_render"):
        require(receipt.get(key)=="NOT_MEASURED","false game metric "+key)
    for key,count in (("cases",8),("repetitions",3),("measured_steps",200),("warmup_steps",30)):
        require(receipt.get(key)==count,"wrong fixture steps: "+key)
    require(abs(realnum(receipt.get("fixed_delta_s"),"timestep")-.02)<.00001,"wrong timestep")
    samples=rows(folder/"simulations.csv",SAMPLE_COLS)
    bycase=defaultdict(dict)
    for row in samples:
        case=row["case_id"];rep=natural(row["repetition"],"rep");step=natural(row["step"],"step")
        require(case in EXPECTED and 1<=rep<=3 and 0<=step<200,"unexpected case/step")
        key=(case,rep)
        require(step not in bycase[key],"duplicate step")
        bycase[key][step]=realnum(row["simulate_ms"],"simulate_ms")
    require(len(bycase)==24,"incomplete case matrix")
    for key,steps in bycase.items():require(set(steps)==set(range(200)),"missing step "+str(key))
    summaries=rows(folder/"summary.csv",SUMMARY_COLS)
    seen=set()
    for row in summaries:
        case=row["case_id"];rep=natural(row["repetition"],"rep")
        require(case in EXPECTED and 1<=rep<=3,"invalid summary case")
        key=(case,rep);require(key not in seen,"duplicate summary");seen.add(key)
        p,pro,e=EXPECTED[case]
        for name,expected in (("parts",p),("projectiles",pro),("enemy_proxies",e),
            ("compound_colliders",1+p+6*e+pro),("rigidbodies",1+e+pro)):
            require(natural(row[name],name)==expected,"wrong object count "+name)
        values=[bycase[key][step] for step in range(200)]
        for name,target in (("p50_ms",quantile(values,.5)),("p95_ms",quantile(values,.95)),
            ("p99_ms",quantile(values,.99)),("mean_ms",statistics.mean(values))):
            actual=realnum(row[name],name)
            require(abs(actual-target)<=max(1e-6,abs(target)*1e-6),"wrong calculated "+name)
        natural(row["managed_alloc_bytes"],"allocations")
        natural(row["gc0_collections"],"GC0")
    require(seen==set(bycase),"missing summary")
    return {
        "status":"STRUCTURAL_REVIEW_REQUIRED_NOT_NATIVE_ACCEPTANCE",
        "source_sha":source_sha,"fixture_sha":fixture_sha,
        "claimed_proxy_physics_steps":len(samples),
        "raw_sha256":{f:sha256((folder/f).read_bytes()).hexdigest() for f in required},
        "limits":"This is only schema/stat validation; require external Editor.log, exit and authorized hardware/license evidence."
    }

if __name__=="__main__":
    parser=argparse.ArgumentParser()
    parser.add_argument("--folder",required=True)
    parser.add_argument("--source-sha",required=True)
    parser.add_argument("--fixture-sha",required=True)
    parser.add_argument("--out",required=True)
    a=parser.parse_args()
    out=audit(a.folder,a.source_sha,a.fixture_sha)
    path=Path(a.out)
    require(not path.exists(),"refuse overwrite validation")
    path.write_text(json.dumps(out,indent=2,sort_keys=True)+"\n")
    print(out["status"])
