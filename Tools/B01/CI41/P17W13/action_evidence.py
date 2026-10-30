#!/usr/bin/env python3
"""B01-41 W13: Verify REAL pinned GitHub Actions job evidence before reporting Unity G1.

GitHub source CI success is *not* native Unity/PhysX. No secret is printed.
This tool cannot grant G0/G1, only evidence classification on exact runs.
"""
from __future__ import annotations
import argparse
import json
import os
from pathlib import Path
import sys
import urllib.error
import urllib.request

REPO="C8LyNn/RoboRougelike"
SELECTED="d94400d95d13db7f27e1533df05c9292d5380431"
CASES={
  "selected-510-source":{
    "run_id":41069853535,"sha":SELECTED,"workflow":"B01-23 W11 selected V2 R2 atomic docking original source QA",
    "kind":"source","job_count":2,
    "required_steps":(
      "Run actual original W06 ledger and R2 atomic transaction source tests",
      "Rerun original W10 public reattach CSharp regression",
      "Rerun original Git physical ZIP stage and rollback negatives",
    )},
  "native-532-readiness":{
    "run_id":41074147973,"sha":"5778023163d8b51d23abfebaa17119e21bd5e131",
    "workflow":"B01-60 P17 W12 SOURCE PREFLIGHT for native Unity",
    "kind":"source","job_count":2,
    "required_steps":(
      "Fetch exactly the selected producer",
      "Build exact-original portable Unity 6000.3.14f1 project and verify 10 adversarial negatives (NOT native)",
      "Publish original Unity importable test project ZIP plus exact-source receipt and NOT_RUN declaration",
    )},
  "blocked-edit-522":{
    "run_id":41073443777,"sha":"fca69ec1b51978ebb8e84871826331ebe2bfc5da",
    "workflow":"CI-02 Unity EditMode","kind":"blocked","job_count":1,
    "runner_step":"Unity 6000.3.14f1 EditMode"},
  "blocked-play-522":{
    "run_id":41073443940,"sha":"fca69ec1b51978ebb8e84871826331ebe2bfc5da",
    "workflow":"CI-03 Unity PlayMode","kind":"blocked","job_count":1,
    "runner_step":"Unity 6000.3.14f1 PlayMode"},
}
PREFIX=f"https://api.github.com/repos/{REPO}/actions/runs/"
MAX_JSON=1_200_000
class EvidenceError(Exception):
    pass

def get_json(url: str, token: str="") -> dict:
    if not url.startswith(PREFIX) or url.split("?")[0].count("/")<9:
        raise EvidenceError("UNAPPROVED_GITHUB_API_ENDPOINT")
    headers={"Accept":"application/vnd.github+json","User-Agent":"B01-41-evidence-gate",
             "X-GitHub-Api-Version":"2022-11-28"}
    if token:
        headers["Authorization"]="Bearer "+token
    request=urllib.request.Request(url,headers=headers)
    try:
        with urllib.request.urlopen(request,timeout=20) as response:
            data=response.read(MAX_JSON+1)
    except (urllib.error.URLError,TimeoutError) as exc:
        raise EvidenceError("GITHUB_API_UNAVAILABLE") from exc
    if len(data)>MAX_JSON:
        raise EvidenceError("API_PAYLOAD_TOO_LARGE")
    try:
        result=json.loads(data)
    except (ValueError,UnicodeError) as exc:
        raise EvidenceError("INVALID_API_JSON") from exc
    if not isinstance(result,dict):
        raise EvidenceError("INVALID_API_PAYLOAD_SHAPE")
    return result

def capture(case: str,token: str="") -> dict:
    if case not in CASES:
        raise EvidenceError("UNKNOWN_PINNED_RUN")
    runid=CASES[case]["run_id"]
    run=get_json(PREFIX+str(runid),token)
    jobs=get_json(PREFIX+str(runid)+"/jobs?per_page=100",token)
    if jobs.get("total_count")!=len(jobs.get("jobs",[])) or jobs.get("total_count",0)>100:
        raise EvidenceError("PAGINATED_OR_MISSING_JOBS")
    return {"case":case,"run":run,"jobs":jobs}

def demand(condition:bool,code:str)->None:
    if not condition:
        raise EvidenceError(code)

def validate(case: str,payload:dict) -> dict:
    demand(case in CASES,"UNKNOWN_PINNED_RUN")
    spec=CASES[case]
    demand(isinstance(payload,dict) and set(payload)=={"case","run","jobs"} and payload["case"]==case,
           "EVIDENCE_CASE_MISMATCH")
    run,jobs=payload["run"],payload["jobs"]
    demand(isinstance(run,dict) and isinstance(jobs,dict),"MISSING_RUN_OR_JOBS")
    demand(type(run.get("id")) is int and run["id"]==spec["run_id"],"RUN_ID_MISMATCH")
    demand(run.get("head_sha")==spec["sha"],"RUN_HEAD_SHA_MISMATCH")
    demand(run.get("name")==spec["workflow"],"WORKFLOW_IDENTITY_MISMATCH")
    demand(run.get("status")=="completed","RUN_NOT_COMPLETED")
    demand(run.get("run_attempt")==1,"RETRIED_RUN_NEEDS_SEPARATE_ATTESTATION")
    demand(run.get("repository",{}).get("full_name")==REPO,"CROSS_REPOSITORY_RUN")
    demand(run.get("event") in ("push","pull_request"),"UNEXPECTED_EVENT")
    entries=jobs.get("jobs")
    demand(type(jobs.get("total_count")) is int and
           isinstance(entries,list) and len(entries)==spec["job_count"]==jobs["total_count"],
           "MISSING_OR_DUPLICATED_JOBS")
    ids=set()
    for job in entries:
        demand(isinstance(job,dict),"MALFORMED_JOB")
        jid=job.get("id")
        demand(type(jid) is int and jid not in ids,"DUPLICATE_OR_INVALID_JOB_ID")
        ids.add(jid)
        demand(job.get("head_sha")==spec["sha"],"JOB_SHA_MISMATCH")
        demand(job.get("run_attempt")==1,"JOB_ATTEMPT_MISMATCH")
        demand(job.get("status")=="completed","INCOMPLETE_JOB")
        steps=job.get("steps")
        demand(isinstance(steps,list) and len(steps)>=4,"NO_ACTUAL_JOB_STEP_EVIDENCE")
        names=[s.get("name") for s in steps if isinstance(s,dict)]
        demand(len(names)==len(steps) and len(set(names))==len(names),
               "DUPLICATE_OR_MALFORMED_STEPS")
        if spec["kind"]=="source":
            demand(run.get("conclusion")=="success" and job.get("conclusion")=="success",
                   "SOURCE_JOB_NOT_SUCCESS")
            for name in spec["required_steps"]:
                relevant=[s for s in steps if s.get("name")==name]
                demand(len(relevant)==1 and relevant[0].get("conclusion")=="success"
                       and relevant[0].get("status")=="completed",
                       "MISSING_EXECUTED_SOURCE_STEP:"+name)
            demand(not any(s.get("name","").startswith("Unity 6000.3.14f1 ") and
                           s.get("conclusion")=="success" for s in steps),
                   "SOURCE_RUN_CLAIMS_NATIVE_STEP")
        else:
            demand(run.get("conclusion")=="failure" and job.get("conclusion")=="failure",
                   "BLOCKED_RUN_STATUS_TAMPERED")
            expected={
               "Require valid Unity activation credentials":"failure",
               spec["runner_step"]:"skipped",
               "Require nonempty, fully passing executed test cases":"skipped",
               "Preserve XML, Unity logs or activation blocker":"success",
            }
            for step_name,result in expected.items():
                relevant=[s for s in steps if s.get("name")==step_name]
                demand(len(relevant)==1 and relevant[0].get("conclusion")==result,
                       "ACTIVATION_OR_NATIVE_STEP_EVIDENCE_MISMATCH:"+step_name)
            demand(not any(s.get("name")=="Unity 6000.3.14f1 EditMode" and
                           s.get("conclusion")=="success" for s in steps),
                   "EDITMODE_EXECUTION_STATUS_TAMPERED")
    return {
        "package":"CI41-ACTION-EVIDENCE-001","version":"1.0.0","case":case,
        "run_id":spec["run_id"],"exact_run_head_sha":spec["sha"],
        "selected_reattach_source_sha":SELECTED,
        "github_run_evidence":"VALIDATED",
        "source_job_executed":"PASS" if spec["kind"]=="source" else "NOT_APPLICABLE",
        "native_unity_g1":"NOT_RUN",
        "save_load_persistence":"HOLD_UNTIL_INDEPENDENT_ADAPTER_ACCEPTANCE",
        "independent_g0":"NOT_EVALUATED_BY_CI41",
        "codex_ack":"NOT_ESTABLISHED",
        "integration":"NO",
        "release_gate":"HOLD",
        "classification":"SOURCE_ONLY_EXECUTED" if spec["kind"]=="source" else "NATIVE_BLOCKED_ACTIVATION",
    }

def read_capture(path:Path)->dict:
    if not path.is_file() or path.stat().st_size>3_000_000:
        raise EvidenceError("INVALID_OR_OVERSIZED_CAPTURE")
    try:
        value=json.loads(path.read_text(encoding="utf-8"))
    except (UnicodeError,ValueError) as exc:
        raise EvidenceError("INVALID_LOCAL_CAPTURE_JSON") from exc
    return value

def main()->int:
    a=argparse.ArgumentParser()
    a.add_argument("--case",required=True,choices=sorted(CASES))
    a.add_argument("--capture-out")
    a.add_argument("--from-capture")
    a.add_argument("--out")
    args=a.parse_args()
    try:
        demand(not (args.capture_out and args.from_capture),"CONFLICTING_CAPTURE_MODE")
        snap=read_capture(Path(args.from_capture)) if args.from_capture else capture(args.case,os.environ.get("GITHUB_TOKEN",""))
        result=validate(args.case,snap)
        if args.capture_out:
            Path(args.capture_out).write_text(json.dumps(snap,sort_keys=True,indent=2)+"\n",encoding="utf-8")
    except (EvidenceError,OSError) as exc:
        result={"package":"CI41-ACTION-EVIDENCE-001","case":args.case,
                "github_run_evidence":"FAIL_CLOSED","reason":str(exc),
                "native_unity_g1":"NOT_RUN",
                "save_load_persistence":"HOLD_UNTIL_INDEPENDENT_ADAPTER_ACCEPTANCE",
                "release_gate":"HOLD"}
    if args.out:
        Path(args.out).write_text(json.dumps(result,sort_keys=True,indent=2)+"\n",encoding="utf-8")
    print(json.dumps(result,sort_keys=True,indent=2))
    return 2 if result["github_run_evidence"]=="FAIL_CLOSED" else 0

if __name__=="__main__":
    sys.exit(main())
