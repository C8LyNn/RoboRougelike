#!/usr/bin/env python3
"""B01-40 P16: inspect ORIGINAL Codex #310 uploaded Unity evidence without claiming G1.

No Unity executed. Validates red full-suite XML remains red despite green targeted
retest; PlayMode process proof, stage pins, intact actual original Git objects.
Red full-suite must not be promoted to integration-ready.
"""
import argparse
import datetime
import hashlib
import json
from pathlib import Path
import subprocess
import sys
import xml.etree.ElementTree as ET

CODEX_EVIDENCE_SHA="589b60a912096d1e9917cb0bbac73a53e7543b4f"
ACTUAL_GAME_SHA="aa7a917f177a9b1f07203868fb7d1d871eb3ee46"
RETEST_SOURCE_SHA="e94f861825e66bb20881102f9f6c8ea2cc19e805"
BASE="Docs/CXFinal/Evidence/"
OBJECTS={
    "EditMode.xml":"9b63d9152994cbc47c3cce8f4a151e30fa60eb95",
    "PlayMode.xml":"e2fed78f63662347fe455aeda05d0ff0830321b5",
    "retest.xml":"23666b1b538d1e2cbf2cbbf870f1002e54146e71",
    "EditMode-process.json":"8bdfded67305c2ad78738832cdf61af22c25a1ac",
    "PlayMode-process.json":"38ddcc761a9142f841972206a7de8d80ccd5a5a4",
    "Compile-process.json":"f6171437090e907068292747c581eaf369501057",
    "Build-process.json":"6710beef9ea3940134ff5f854488e04c375fa4a7",
}
class EvidenceReject(ValueError):pass

def need(condition,message):
    if not condition:raise EvidenceReject(message)

def git(repo,*args):
    p=subprocess.run(["git","-C",str(repo),*args],stdout=subprocess.PIPE,stderr=subprocess.PIPE)
    if p.returncode:raise EvidenceReject("GIT_PINNED_EVIDENCE_MISSING:"+p.stderr.decode(errors="replace")[:140])
    return p.stdout

def original(repo):
    data={}
    for name,oid in OBJECTS.items():
        path=BASE+name
        found=git(repo,"rev-parse",CODEX_EVIDENCE_SHA+":"+path).decode().strip()
        need(found==oid,"EVIDENCE_GIT_BLOB_DRIFT:"+name)
        raw=git(repo,"show",CODEX_EVIDENCE_SHA+":"+path)
        need(hashlib.sha1(b"blob "+str(len(raw)).encode()+b"\0"+raw).hexdigest()==oid,
             "EVIDENCE_BYTES_NOT_REAL_GIT_BLOB:"+name)
        data[name]=raw
    return data

def run_info(xml):
    try:r=ET.fromstring(xml)
    except ET.ParseError as e:raise EvidenceReject("INVALID_ORIGINAL_NUNIT_XML") from e
    need(r.tag=="test-run","NOT_NUNIT_TEST_RUN")
    counts={}
    for field in ("total","passed","failed","skipped"):
        try:counts[field]=int(r.get(field))
        except (TypeError,ValueError) as e:raise EvidenceReject("NUNIT_COUNT_INVALID:"+field) from e
    need(counts["total"]==counts["passed"]+counts["failed"]+counts["skipped"],
         "NUNIT_ROOT_COUNTS_INCONSISTENT")
    failures=[x for x in r.iter("test-case") if x.get("result")=="Failed"]
    passes=[x for x in r.iter("test-case") if x.get("result")=="Passed"]
    need(len(failures)==counts["failed"] and len(passes)==counts["passed"],
         "NUNIT_ACTUAL_CASES_DIFFER_FROM_HEADER")
    names=[x.get("fullname") for x in r.iter("test-case")]
    need(all(names) and len(names)==len(set(names)),"DUPLICATE_OR_MISSING_NUNIT_CASE")
    return r.get("result"),counts,sorted(x.get("fullname") for x in failures)

def evaluate(data):
    full,ful_counts,full_failures=run_info(data["EditMode.xml"])
    play,play_counts,play_failures=run_info(data["PlayMode.xml"])
    targeted,tgt_counts,tgt_failures=run_info(data["retest.xml"])
    need(ful_counts=={"total":1303,"passed":1288,"failed":15,"skipped":0} and
         full in ("Failed","Failed(Child)") and len(full_failures)==15,
         "HISTORICAL_FULL_EDITOR_RED_NOT_PRESERVED")
    need(play_counts=={"total":474,"passed":474,"failed":0,"skipped":0} and
         play=="Passed" and not play_failures,
         "PLAYMODE_ORIGINAL_RECEIPT_NOT_PASS")
    need(tgt_counts=={"total":15,"passed":15,"failed":0,"skipped":0} and
         targeted=="Passed" and not tgt_failures,
         "TARGETED_RETEST_ORIGINAL_RECEIPT_NOT_PASS")
    processes={}
    for phase in ("Compile","Build","EditMode","PlayMode"):
        try:d=json.loads(data[phase+"-process.json"])
        except (ValueError,UnicodeDecodeError) as e:raise EvidenceReject("BAD_PROCESS_JSON:"+phase) from e
        need(d.get("source")==ACTUAL_GAME_SHA and d.get("phase")==phase and
             d.get("stage")=="CX-Final" and type(d.get("exitCode")) is int,
             "PROCESS_SOURCE_OR_PHASE_MISMATCH:"+phase)
        need(d["exitCode"]==({"EditMode":2}.get(phase,0)),
             "PROCESS_EXIT_DOES_NOT_MATCH_NUNIT:"+phase)
        try:
            start=datetime.datetime.fromisoformat(d["startedUtc"].replace("Z","+00:00"))
            end=datetime.datetime.fromisoformat(d["finishedUtc"].replace("Z","+00:00"))
        except (ValueError,KeyError,TypeError) as e:raise EvidenceReject("PROCESS_TIME_INVALID:"+phase) from e
        need(end>=start,"PROCESS_TIME_BACKWARDS:"+phase)
        processes[phase]={"exit":d["exitCode"],"wall_seconds":round((end-start).total_seconds(),3)}
    # Full failure followed by targeted correction is NOT a full-suite pass.
    return {"status":"P16_CODEX_NATIVE_RECEIPT_ORIGINAL_BYTES_VALIDATED",
            "release_gate":"HOLD_FULL_EDITOR_WAS_RED__FULL_NEW_SHA_NOT_RERUN",
            "evidence_source_commit":CODEX_EVIDENCE_SHA,
            "original_game_sha":ACTUAL_GAME_SHA,"test_only_correction_sha":RETEST_SOURCE_SHA,
            "editmode_original":ful_counts,"playmode_original":play_counts,
            "targeted_editor_retest":tgt_counts,"failed_editor_original_case_names":full_failures,
            "processes":processes,"original_receipt_blob_oids":OBJECTS,
            "independent_unity_execution":"NOT_RUN",
            "independent_g1":"NOT_RUN","g2":"HOLD",
            "limitations":"XML+process metadata integrity only; no Editor rerun/Player verification"}

def main():
    ap=argparse.ArgumentParser()
    ap.add_argument("--repo",default=".")
    ap.add_argument("--out")
    a=ap.parse_args()
    try:
        root=Path(a.repo).resolve(strict=True)
        result=evaluate(original(root))
        raw=(json.dumps(result,sort_keys=True,indent=2)+"\n").encode()
        if a.out:
            out=Path(a.out).resolve(strict=False)
            need(out.parent.is_dir() and out.suffix==".json" and not out.exists() and
                 not out.is_symlink(),"UNSAFE_RECEIPT_OUTPUT")
            with out.open("xb") as f:f.write(raw)
        print(raw.decode())
        return 0
    except (EvidenceReject,KeyError,ValueError,OSError) as e:
        print("P16_CODEX_EVIDENCE_REJECT "+str(e)+" INDEPENDENT_UNITY_NOT_RUN",file=sys.stderr)
        return 2

if __name__=="__main__":sys.exit(main())
