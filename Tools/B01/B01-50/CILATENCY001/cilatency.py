#!/usr/bin/env python3
"""Offline GitHub Actions latency analysis; never infer Unity-native or integration success."""
import argparse
import hashlib
import json
import math
import re
from datetime import datetime, timezone
from pathlib import Path

class EvidenceError(ValueError):
    pass

def require(valid, why):
    if not valid:
        raise EvidenceError(why)

def utc(value):
    require(isinstance(value, str) and re.fullmatch(r"\d{4}-\d\d-\d\dT\d\d:\d\d:\d\dZ", value), "Invalid UTC timestamp")
    try:
        return datetime.strptime(value, "%Y-%m-%dT%H:%M:%SZ").replace(tzinfo=timezone.utc)
    except ValueError as exc:
        raise EvidenceError("Impossible calendar date") from exc

def seconds(start, end):
    value = int((utc(end)-utc(start)).total_seconds())
    require(0 <= value <= 31_536_000, "Negative or implausible duration")
    return value

def stats(values):
    require(len(values)>0, "No observations")
    ordered=sorted(values)
    return {"n": len(values), "min_s": ordered[0], "median_s": ordered[math.ceil(len(values)*0.5)-1],
            "p95_nearest_rank_s": ordered[math.ceil(len(values)*0.95)-1], "max_s": ordered[-1]}

def analyze(payload, input_sha256=""):
    require(isinstance(payload,dict) and payload.get("schema")=="b0150-cilatency-snapshot-v1"
            and payload.get("repository")=="C8LyNn/RoboRougelike", "Invalid schema/repository")
    require(isinstance(payload.get("source"),str) and payload["source"].startswith("GitHub REST"),
            "Missing provenance")
    runs=payload.get("workflow_runs")
    require(isinstance(runs,list) and 1<=len(runs)<=100, "Missing/oversize run collection")
    allowed={"success","failure","cancelled","skipped","neutral","timed_out","action_required"}
    seen_runs,seen_jobs,result_runs,result_jobs=set(),set(),[],[]
    for run in runs:
        require(isinstance(run,dict), "Run must be object")
        rid=run.get("id")
        require(type(rid) is int and rid>0 and rid not in seen_runs, "Invalid/duplicate run ID")
        seen_runs.add(rid)
        require(isinstance(run.get("head_sha"),str) and re.fullmatch("[0-9a-f]{40}",run["head_sha"]),"Invalid SHA")
        require(isinstance(run.get("name"),str) and len(run["name"])>3,"Invalid name")
        require(run.get("status")=="completed" and run.get("conclusion") in allowed, "Nonterminal run")
        require(type(run.get("run_attempt")) is int and run["run_attempt"]>0, "Invalid attempt")
        wall=seconds(run.get("created_at"),run.get("updated_at"))
        jobs=run.get("jobs")
        require(isinstance(jobs,list) and 1<=len(jobs)<=100,"Missing/truncated job page")
        max_wait=0
        for job in jobs:
            require(isinstance(job,dict),"Job must be object")
            jid=job.get("id")
            require(type(jid) is int and jid>0 and jid not in seen_jobs and job.get("run_id")==rid,
                    "Duplicate/foreign job")
            seen_jobs.add(jid)
            require(job.get("status")=="completed" and job.get("conclusion") in allowed, "Nonterminal job")
            require(isinstance(job.get("name"),str) and len(job["name"])>3,"Invalid job name")
            require(seconds(run["created_at"],job["created_at"])<=wall,"Job created outside run")
            wait=seconds(job["created_at"],job["started_at"])
            execution=seconds(job["started_at"],job["completed_at"])
            require(seconds(run["created_at"],job["completed_at"])<=wall,"Job completed after run update")
            match=re.search(r"\((ubuntu-24\.04|windows-2025)\)$",job["name"])
            result_jobs.append({"run_id":rid,"job_id":jid,"name":job["name"],
                                "runner":match.group(1) if match else "unclassified",
                                "head_sha":run["head_sha"],"conclusion":job["conclusion"],
                                "wait_until_start_seconds":wait,"execution_seconds":execution})
            max_wait=max(max_wait,wait)
        result_runs.append({"run_id":rid,"head_sha":run["head_sha"],"name":run["name"],
                            "conclusion":run["conclusion"],"run_attempt":run["run_attempt"],
                            "created_to_last_update_seconds":wall,
                            "maximum_job_wait_seconds":max_wait})
    result_runs.sort(key=lambda r:r["run_id"])
    result_jobs.sort(key=lambda j:(j["run_id"],j["job_id"]))
    return {"schema":"b0150-cilatency-report-v1","repository":"C8LyNn/RoboRougelike",
            "input_sha256":input_sha256,"run_count":len(result_runs),"job_count":len(result_jobs),
            "run_conclusions":{v:sum(r["conclusion"]==v for r in result_runs)
                               for v in sorted({r["conclusion"] for r in result_runs})},
            "wait":stats([j["wait_until_start_seconds"] for j in result_jobs]),
            "execution":stats([j["execution_seconds"] for j in result_jobs]),
            "workflow_wall":stats([r["created_to_last_update_seconds"] for r in result_runs]),
            "runs":result_runs,"jobs":result_jobs,
            "unity_native_g1":"NOT_ASSESSED_BY_ACTIONS_LATENCY",
            "game_fps_gpu":"NOT_MEASURED","integrator_accepted":"NOT_OBSERVED",
            "integrator_elapsed":"NOT_MEASURED",
            "warning":"Six selected historical B01-50 CI runs; not random or representative. Timestamp deltas do not establish cause."}

def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--input",required=True)
    parser.add_argument("--output",required=True)
    args=parser.parse_args()
    src,dst=Path(args.input),Path(args.output)
    require(src.is_file() and not src.is_symlink() and src.stat().st_size<=2_000_000,
            "Missing/symlink/oversized input")
    require(not dst.exists() and not dst.is_symlink() and src.resolve()!=dst.resolve(),
            "Refusing receipt overwrite")
    raw=src.read_bytes()
    try:
        doc=json.loads(raw)
    except (UnicodeError,ValueError) as exc:
        raise EvidenceError("Invalid JSON") from exc
    result=analyze(doc,hashlib.sha256(raw).hexdigest())
    require(dst.parent.is_dir() and not dst.parent.is_symlink(),"Unsafe output parent")
    with dst.open("x",encoding="utf-8") as f:
        json.dump(result,f,sort_keys=True,indent=2)
        f.write("\n")
    print("CI_LATENCY_SOURCE_EVIDENCE_PASS",result["run_count"],"runs",result["job_count"],"jobs",
          "max_wait_s",result["wait"]["max_s"])
    print("NATIVE_UNITY_NOT_ASSESSED | INTEGRATOR_NOT_OBSERVED")

if __name__=="__main__":
    try:
        main()
    except (OSError,EvidenceError) as err:
        raise SystemExit("CI_LATENCY_EVIDENCE_REJECTED: "+str(err))
