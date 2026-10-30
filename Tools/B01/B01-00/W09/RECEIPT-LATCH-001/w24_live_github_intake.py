#!/usr/bin/env python3
"""B01-00 W24: read-only LIVE GitHub intake for exactly one selected Save chain.

Checks real remote PR heads, owner policy and Actions runs, not a synthetic source
model. A passing intake is only a source receipt: NEVER a product G0/G1 or merge.
GITHUB_TOKEN is accepted for GET, never logged, never used for writes.
"""
import argparse
import hashlib
import json
import os
from pathlib import Path
import sys
from urllib.request import Request, urlopen
from urllib.error import HTTPError, URLError

SCHEMA="b01-w24-live-source-intake-v1"
REPO="C8LyNn/RoboRougelike"
PR_PINS={
    690:"cc7f73541517a18f4479bb155534bd4832d9985e",
    729:"b8ca29c7707cd6b4338d0b9772d74dee676d49d2",
    749:"8634e71dfcf633323073fe584a6c0884f1ca5d23",
    753:"743e36478ae8cf99e8429af1a8209da9d7ca64de",
    741:"75f1631f9a36ce51b0f8edc0c7857e924cdf78ec",
    750:"60c73a6f9ed2585cbb9bf7aeaad8c62eef0629cb",
    732:"0bee4ce9928354433ec719c45a8186378361d46e",
    755:"f1bff0839e297df971d097231c4627a07005ee17",
}
ACTION_RUN_PINS={
    41133131736:749,
    41134559111:755,
    41133166507:750,
    41132037039:732,
}
ALT_HOLD=[753,741,719,695,693,682,652,644,604]
POLICY_LINES={
 "free_per_victory":"One free destroyed-part rebuild per verified distinct victory",
 "next_victory":"next distinct, authentic win",
 "emergency_loaner":"One-time weaker emergency loaner robot",
 "loaner_core_and_challenge":"living Core AND an authentic eligible next challenge",
 "repair_65":"rebuild price = 65%",
 "functional_gate":"Block fight start ONLY for a genuinely non-functional robot",
 "honest_crash_safety":"honest local crash, torn write, OS process interruption, corruption",
 "anti_tamper_out_of_scope":"NOT a release requirement",
}
class IntakeRejected(ValueError):pass
def require(ok,label):
    if not ok:raise IntakeRejected(label)
def github_get(path,token=None):
    require(path.startswith("/repos/C8LyNn/RoboRougelike/"),"disallowed path")
    req=Request("https://api.github.com"+path,
                headers={"Accept":"application/vnd.github+json",
                "User-Agent":"b01-00-w24-readonly-intake",
                **({"Authorization":"Bearer "+token} if token else {})})
    with urlopen(req,timeout=20) as response:
        raw=response.read(250_000)
    return json.loads(raw.decode("utf-8"))
def obtain(get=github_get,token=None):
    # Supports deterministic test injection. Never expose token or raw response.
    return {
      "pulls":{str(n):get(f"/repos/{REPO}/pulls/{n}",token)
               for n in sorted(PR_PINS)},
      "runs":{str(run):get(f"/repos/{REPO}/actions/runs/{run}",token)
              for run in sorted(ACTION_RUN_PINS)},
      "hq":get(f"/repos/{REPO}/issues/731",token)}
def validate(payload):
    require(type(payload) is dict and set(payload)=={"pulls","runs","hq"},
            "schema of external response")
    prs=payload["pulls"];runs=payload["runs"];hq=payload["hq"]
    require(type(prs) is dict and set(prs)==set(map(str,PR_PINS)),
            "missing original pull receipts")
    require(type(runs) is dict and set(runs)==set(map(str,ACTION_RUN_PINS)),
            "missing original Actions receipts")
    require(type(hq) is dict and hq.get("number")==731,
            "owner issue identity")
    body=hq.get("body")
    require(isinstance(body,str) and
            "OWNER DECISIONS" in body and
            "APPROVED" in body.split("OWNER DECISIONS",1)[1][:160],
            "not an approved owner policy")
    for key,phrase in POLICY_LINES.items():
        require(phrase in body,"missing approved owner policy "+key)
    require("#749 is the currently stated selected bridge" in body,
            "owner selection not pinned in HQ")
    for n,sha in PR_PINS.items():
        pr=prs[str(n)]
        require(type(pr) is dict and pr.get("number")==n,
                f"PR {n} swapped response")
        require(pr.get("head",{}).get("sha")==sha,
                f"PR {n} HEAD drift: new owner-review required")
        require(pr.get("state")=="open" and pr.get("draft") is True
                and not pr.get("merged",False),
                f"PR {n} state/merge changed")
        require(pr.get("base",{}).get("repo",{}).get("full_name")==REPO,
                f"PR {n} unexpected repository")
    for run,pr_no in ACTION_RUN_PINS.items():
        obj=runs[str(run)]
        require(type(obj) is dict and obj.get("id")==run,
                f"run {run} swapped response")
        require(obj.get("head_sha")==PR_PINS[pr_no],
                f"run {run} is not exact original PR {pr_no} head")
        require(obj.get("status")=="completed" and
                obj.get("conclusion")=="success",
                f"run {run} is not successful source CI")
    # This gate deliberately distinguishes independent source transport from
    # authentic native gameplay. Never auto-promote due to workflow success.
    selected=prs["749"]
    alternative=prs["753"]
    require("ONE" in selected.get("title","").upper() and
            "lease" in alternative.get("body","").lower(),
            "pr source variants require reviewer confirmation")
    g0=prs["755"].get("body","")
    require("#749" in g0 and
            "ACCEPTED_SOURCE_ONLY" in g0 and
            "#732" in g0,
            "B01-40 SOURCE_ONLY G0 acceptance missing")
    note=prs["750"].get("body","")
    require("ColdRestart" in note and
            "generation" in note.lower(),
            "ColdRestart fixture source correction missing")
    return {
       "schema":SCHEMA,
       "verdict":"SOURCE_INTAKE_CONSISTENT_PRODUCT_HOLD",
       "canonical_chain":[690,729,749],
       "selected_save_bridge":749,
       "selected_save_sha":PR_PINS[749],
       "non_selected_verified":[{"pr":753,"status":"ALT_HOLD"},
                                 {"pr":741,"status":"ALT_HOLD"}],
       "other_alternatives":ALT_HOLD[2:],
       "original_pr_pins":{str(k):v for k,v in PR_PINS.items()},
       "original_ci_pins":{str(k):ACTION_RUN_PINS[k]
                           for k in sorted(ACTION_RUN_PINS)},
       "host_fixture_750":"SOURCE_PASS_ONLY_NOT_SELECTED_NATIVE_G0",
       "host_fixture_732":"ACCEPTED_IN_755_SOURCE_ONLY",
       "formal_749_transport_g0":"ACCEPTED_SOURCE_ONLY",
       "independent_749_SECURITY":"PENDING_EXACT_ORIGINAL_CROSSPROCESS_QA",
       "same_physical_owner":".owner",
       "legacy_669_raw_store_coinstall":"FORBIDDEN",
       "actual_private_510_world_adapter":"NOT_IMPLEMENTED",
       "gameplay_policies":"OWNER_APPROVED_IMPLEMENTATION_NOT_CERTIFIED",
       "economy_atomic_journal":"NOT_PROVEN_IN_749",
       "crash_corrupt_torn_protection":"SOURCE_TESTED_NOT_NATIVE",
       "malicious_external_whole_root_tamper":"OWNER_APPROVED_OUT_OF_RELEASE_SCOPE",
       "real_native_unity_g1":"NOT_RUN",
       "product_save_load":"HOLD",
       "g2":"HOLD","codex_ack":None,
       "integration_feedback":"NOT_MEASURED",
       "acceptance_latency":None,"merge":False,
       "receivers":{
           "unity_world_adapter":"B01-23/20/62",
           "economy_atomic_journal":"B01-22/10/23/62",
           "loaner_real_game":"B01-20/23/62",
           "independent_original_sources":"B01-61/41",
           "formal_assembled_g0":"B01-40/62",
           "native_g1":"B01-60+LOCAL_OPERATOR",
           "future_authorized_integrator":"B01-00+OWNER"},
       "original_source_success_accepted":True,
       "ready_for_install":False,
       "release":False}
def main(argv=None):
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument("--snapshot",type=Path,help="test-only read-only captured GH data")
    p.add_argument("--output",type=Path,help="create only if path does not exist")
    p.add_argument("--require-install-ready",action="store_true")
    args=p.parse_args(argv)
    try:
        if args.snapshot is None:
            data=obtain(token=os.environ.get("GITHUB_TOKEN"))
        else:
            data=json.loads(args.snapshot.read_text(encoding="utf-8"))
        result=validate(data)
        encoded=json.dumps(result,sort_keys=True,indent=2)+"\n"
        result_sha=hashlib.sha256(encoded.encode("utf-8")).hexdigest()
        result["result_sha256"]=result_sha
        out=json.dumps(result,sort_keys=True,indent=2)+"\n"
        if args.output:
            require(args.snapshot is None or
                    args.output.resolve()!=args.snapshot.resolve(),
                    "must not overwrite source")
            require(not args.output.exists(),"never overwrite evidence/user file")
            with args.output.open("x",encoding="utf-8") as fh:fh.write(out)
        else:
            print(out,end="")
        return 3 if args.require_install_ready else 0
    except (OSError,ValueError,TypeError,HTTPError,URLError) as exc:
        # Do not log credentials, request body or network auth header.
        print("B01_W24_INTAKE_REJECT:"+type(exc).__name__+
              (": "+str(exc) if isinstance(exc,IntakeRejected) else ""),
              file=sys.stderr)
        return 2
if __name__=="__main__":
    raise SystemExit(main())
