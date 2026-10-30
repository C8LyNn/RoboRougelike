#!/usr/bin/env python3
"""CI41-BRIDGE-QA-FRESHNESS-001: fail-closed exact-head candidate vs QA source.

Read-only, never grants G0/G1 or Codex ACK. No transaction/game source edits.
"""
from __future__ import annotations
import argparse
import hashlib
import json
import os
from pathlib import Path
import sys
import urllib.error
import urllib.parse
import urllib.request

REPO="C8LyNn/RoboRougelike"
BASE=f"https://api.github.com/repos/{REPO}"
GAME="d94400d95d13db7f27e1533df05c9292d5380431"
SAVE="33094f154c7b91119b36d58aab285e6e83c5de57"
PINS={
 "510":GAME,"556":SAVE,
 "585":"135a67d42acfccd59ca9145e09630d0b4bbade1c",
 "587":"ad7904eca571c742257551fa8103f266fcb9b752",
 "572":"0f48225173e835015b0c5cfcce6e32bdcfcd7711",
 "588":"a64b612a28e8864dbf00f77d19d49559bc2a991a",
}
PROPOSALS={
 "585":{"qa":"572",
   "path":"Assets/B0123SecureV2HostBridge001/Runtime/AtomicColdUnityHostBridge.cs",
   "qa_original_target_sha":PINS["585"],
   "qa_original_target_blob":"e9e9dc6d437e96dad6b08c1639e8bd94d62c4f4b",
   "risk":"POST_ACTIVATION_VALIDATION_RED_AND_DURABLE_WATERMARK_REQUIRED",
   "qa_run_id":41093307691},
 "587":{"qa":"588",
   "path":"Assets/B0123UnitySaveBridge001/Core/AtomicWorldSwap.cs",
   "qa_original_target_sha":"39f03fd6a0d5fae10be5419db4beb79201147bc8",
   "qa_original_target_blob":"db69bae2bf4cf106da68f13e0b067499f0da02f3",
   "risk":"SAME_GENERATION_DIFFERENT_WORLD_RELEASE_RED",
   "qa_run_id":41093235665},
}
MAX_API=2_000_000
class FenceReject(Exception):pass

def demand(flag:bool,msg:str)->None:
    if not flag: raise FenceReject(msg)

def fetch_json(url:str,token:str="")->dict:
    demand(url.startswith(BASE+"/pulls/") or url.startswith(BASE+"/contents/")
           or url.startswith(BASE+"/actions/runs/"),"URL_OUTSIDE_EXACT_REPOSITORY")
    headers={"Accept":"application/vnd.github+json","User-Agent":"B01-41-bridge-qa-freshness",
             "X-GitHub-Api-Version":"2022-11-28"}
    if token: headers["Authorization"]="Bearer "+token
    try:
        with urllib.request.urlopen(urllib.request.Request(url,headers=headers),timeout=25) as r:
            raw=r.read(MAX_API+1)
    except (urllib.error.URLError,TimeoutError) as exc:
        raise FenceReject("GITHUB_SOURCE_UNAVAILABLE") from exc
    demand(len(raw)<=MAX_API,"OVERSIZED_SOURCE_RESPONSE")
    try: result=json.loads(raw)
    except (ValueError,UnicodeError) as exc:
        raise FenceReject("UNPARSABLE_GITHUB_SOURCE") from exc
    demand(type(result) is dict,"UNEXPECTED_GITHUB_RESPONSE")
    return result

def original_capture(token:str="")->dict:
    prs={}
    for n in PINS:
        pr=fetch_json(f"{BASE}/pulls/{n}",token)
        prs[n]={
          "number":pr.get("number"),"head_sha":pr.get("head",{}).get("sha"),
          "base_sha":pr.get("base",{}).get("sha"),
          "state":pr.get("state"),"draft":pr.get("draft"),
          "repository":pr.get("head",{}).get("repo",{}).get("full_name"),
        }
    src={}
    qa={}
    for n,spec in PROPOSALS.items():
        path=urllib.parse.quote(spec["path"],safe="/")
        for label,sha in (("current",prs[n]["head_sha"]),
                          ("tested",spec["qa_original_target_sha"])):
            doc=fetch_json(f"{BASE}/contents/{path}?ref={sha}",token)
            src[n+":"+label]={"path":doc.get("path"),"blob":doc.get("sha"),
                              "type":doc.get("type"),"size":doc.get("size")}
        run=fetch_json(f"{BASE}/actions/runs/{spec['qa_run_id']}",token)
        qa[n]={"id":run.get("id"),"head_sha":run.get("head_sha"),
               "status":run.get("status"),"conclusion":run.get("conclusion"),
               "repository":run.get("repository",{}).get("full_name")}
    return {"pulls":prs,"sources":src,"qa_runs":qa}

def inspect(capture:dict)->dict:
    demand(type(capture) is dict and set(capture)=={"pulls","sources","qa_runs"},
           "INVALID_CAPTURE_SHAPE")
    prs=capture["pulls"]; src=capture["sources"]; qa=capture["qa_runs"]
    demand(type(prs) is dict and set(prs)==set(PINS),"MISSING_OR_FOREIGN_PULLS")
    demand(type(src) is dict and set(src)=={x+":"+y for x in PROPOSALS for y in ("current","tested")},
           "MISSING_OR_FOREIGN_SOURCES")
    demand(type(qa) is dict and set(qa)==set(PROPOSALS),"MISSING_OR_FOREIGN_QA_RUNS")
    for n,expected in PINS.items():
        p=prs[n]
        demand(type(p) is dict and p.get("number")==int(n)
               and p.get("repository")==REPO and p.get("head_sha")==expected,
               "ORIGINAL_PR_IDENTITY_OR_SHA_DRIFT:"+n)
        demand(p.get("state")=="open" and p.get("draft") is True,
               "ORIGINAL_PR_STATE_CHANGED_REVALIDATE:"+n)
    demand(prs["510"]["head_sha"]==GAME and prs["556"]["head_sha"]==SAVE,
           "FROZEN_GAME_OR_SAVE_MOVED")
    for n in PROPOSALS:
        demand(prs[n]["base_sha"]==SAVE,"HOST_BRIDGE_NOT_BASED_ON_SELECTED_SAVE:"+n)
    result={}
    for n,spec in PROPOSALS.items():
        checked=src[n+":current"]; old=src[n+":tested"]; run=qa[n]
        demand(run.get("id")==spec["qa_run_id"] and run.get("head_sha")==PINS[spec["qa"]]
               and run.get("status")=="completed" and run.get("conclusion")=="success"
               and run.get("repository")==REPO,
               "ORIGINAL_QA_RUN_PROVENANCE_MISMATCH:"+n)
        demand(old.get("path")==spec["path"] and old.get("type")=="file"
               and old.get("blob")==spec["qa_original_target_blob"],"QA_TESTED_SOURCE_BLOB_MISMATCH:"+n)
        demand(checked.get("path")==spec["path"] and checked.get("type")=="file"
               and isinstance(checked.get("blob"),str) and len(checked["blob"])==40,
               "MISSING_CURRENT_HOST_TRANSACTION_CORE:"+n)
        demand(type(checked.get("size")) is int and checked["size"]>300
               and type(old.get("size")) is int and old["size"]>300,
               "INVALID_SOURCE_BYTES:"+n)
        same=checked["blob"]==old["blob"]
        exact=prs[n]["head_sha"]==spec["qa_original_target_sha"]
        freshness="EXACT_PRODUCER_SHA_TESTED" if same and exact else (
            "HEAD_SHA_CHANGED_BUT_CORE_BLOB_SAME_RETEST_ENTIRE_TREE" if same else
            "QA_ORIGINAL_CORE_BLOB_STALE_RETEST_NEW_SHA")
        result[n]={
          "candidate_pr":int(n),"candidate_head_sha":prs[n]["head_sha"],
          "tested_producer_sha":spec["qa_original_target_sha"],
          "actual_current_core_git_blob":checked["blob"],
          "actual_qa_tested_core_git_blob":old["blob"],
          "actual_qa_run_id":spec["qa_run_id"],
          "qa_run_executed":"PASS_WITH_EXPECTED_RELEASE_RED",
          "source_test_freshness":freshness,
          "independent_release_blocker":spec["risk"],
          "source_admission":"HOLD",
        }
    demand(result["585"]["source_test_freshness"]=="EXACT_PRODUCER_SHA_TESTED"
           and result["587"]["source_test_freshness"]=="QA_ORIGINAL_CORE_BLOB_STALE_RETEST_NEW_SHA",
           "HISTORICAL_BASELINE_CHANGED_REEVALUATE_CAUTION")
    decision={
      "package_id":"CI41-BRIDGE-QA-FRESHNESS-001","version":"1.0.0",
      "frozen_game_sha":GAME,"frozen_save_sha":SAVE,
      "candidate_selection":"NOT_AUTHORIZED_BY_B01_41",
      "candidates":result,
      "qa_observation":"ORIGINAL_QA_SUCCESS_DOES_NOT_MEAN_RELEASE_SUCCESS",
      "host_save_load":"HOLD","source_bridge_g0":"PENDING_OWNER_AND_INDEPENDENT_QA",
      "unity_native_g1":"NOT_RUN","g2":"HOLD","codex_ack":False,
      "integrated":False,"release_gate":"HOLD",
    }
    # Stable source selection evidence can be compared across Windows/Linux.
    digest=json.dumps(result,sort_keys=True,separators=(",",":")).encode()
    decision["evidence_sha256"]=hashlib.sha256(digest).hexdigest()
    return decision

def main()->int:
    p=argparse.ArgumentParser()
    p.add_argument("--mode",choices=["audit","enforce"],default="audit")
    p.add_argument("--capture-out")
    p.add_argument("--from-capture")
    p.add_argument("--output",required=True)
    args=p.parse_args()
    try:
        if args.from_capture:
            raw=Path(args.from_capture).read_bytes()
            demand(len(raw)<=MAX_API*4,"OVERSIZED_CAPTURE")
            source=json.loads(raw)
        else:
            source=original_capture(os.environ.get("GITHUB_TOKEN",""))
        report=inspect(source)
        if args.capture_out:
            Path(args.capture_out).write_text(json.dumps(source,indent=2,sort_keys=True)+"\n")
        code=0 if args.mode=="audit" else 3
    except (FenceReject,OSError,UnicodeError,ValueError) as exc:
        report={"package_id":"CI41-BRIDGE-QA-FRESHNESS-001",
                "status":"UNVERIFIED_FAIL_CLOSED","reason":str(exc),
                "host_save_load":"HOLD","unity_native_g1":"NOT_RUN",
                "release_gate":"HOLD"}
        code=2
    Path(args.output).write_text(json.dumps(report,sort_keys=True,indent=2)+"\n")
    print(json.dumps(report,sort_keys=True,indent=2))
    return code

if __name__=="__main__":
    sys.exit(main())
