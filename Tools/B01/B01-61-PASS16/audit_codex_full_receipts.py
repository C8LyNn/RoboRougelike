#!/usr/bin/env python3
"""Independent original Codex Git-committed Unity NUnit XML audit.
Executes strict Python XML against original Git blobs + original process JSON.
It DOES NOT run Unity; a successful audit does not turn failed EditMode green.
"""
import argparse
import json
import pathlib
import subprocess
import sys
import xml.etree.ElementTree as ET

HERE=pathlib.Path(__file__).resolve().parent

class EvidenceError(ValueError):pass

def git(root,*args):
    p=subprocess.run(["git","-C",str(root),*args],stdout=subprocess.PIPE,
                     stderr=subprocess.PIPE,check=False)
    if p.returncode:raise EvidenceError("GIT_ERROR:"+repr(args)+":"+p.stderr.decode(errors="replace")[:250])
    return p.stdout

def original(root,ref,path,blob):
    actual=git(root,"rev-parse",f"{ref}:{path}").decode().strip()
    if actual!=blob:raise EvidenceError(f"WRONG_GIT_BLOB {path} {actual} != {blob}")
    return git(root,"show",f"{ref}:{path}")

def nunit(xmlbytes,expected,label):
    try:root=ET.fromstring(xmlbytes)
    except ET.ParseError as exc:raise EvidenceError(f"{label}: RAW_NUNIT_NOT_WELL_FORMED: {exc}") from exc
    if root.tag!="test-run":raise EvidenceError(f"{label}: NOT_NUNIT_TEST_RUN:{root.tag}")
    numbers={k:int(root.get(k,"-1")) for k in ("total","passed","failed","skipped","inconclusive")}
    wanted={k:expected[k] if k in expected else 0 for k in numbers}
    if numbers!=wanted:raise EvidenceError(f"{label}: FULL_RUN_TOTAL_MISMATCH {numbers} != {wanted}")
    if numbers["total"]!=sum(numbers[k] for k in ("passed","failed","skipped","inconclusive")):
        raise EvidenceError(f"{label}: TOTAL_ARITHMETIC_MISMATCH")
    if int(root.get("testcasecount","-1"))<numbers["total"]:
        raise EvidenceError(f"{label}: TESTCASECOUNT_BELOW_ACTUAL")
    correct_result="Passed" if numbers["failed"]==0 else "Failed"
    if not root.get("result","").startswith(correct_result):
        raise EvidenceError(f"{label}: RESULT_STATUS_SPOOF {root.get('result')}")
    cases=root.findall(".//test-case")
    if len(cases)!=numbers["total"]:
        raise EvidenceError(f"{label}: XML_CASES_{len(cases)}_VS_ROOT_{numbers['total']}")
    byname={}
    actual=dict(Passed=0,Failed=0,Skipped=0,Inconclusive=0)
    for case in cases:
        fullname=case.get("fullname")
        if not fullname or fullname in byname:raise EvidenceError(f"{label}: EMPTY_OR_DUPLICATE_FULLNAME:{fullname}")
        byname[fullname]=case.get("result","")
        if case.get("result") not in actual:
            raise EvidenceError(f"{label}: UNKNOWN_CASE_STATUS:{case.get('result')}")
        actual[case.get("result")]+=1
    if (actual["Passed"],actual["Failed"],actual["Skipped"],actual["Inconclusive"])!=(
        numbers["passed"],numbers["failed"],numbers["skipped"],numbers["inconclusive"]
    ):
        raise EvidenceError(f"{label}: CASE_COUNTS_DIFFER_FROM_ROOT:{actual}")
    print(f"ORIGINAL_NUNIT_STRICT={label} | XML_CASES={len(cases)} | PASS={numbers['passed']} | FAIL={numbers['failed']} | SKIP={numbers['skipped']} | ROOT={root.get('result')}")
    return byname

def process_receipt(raw,manifest,label):
    j=json.loads(raw)
    if j.get("source")!=manifest["game_sha"]:
        raise EvidenceError(f"{label}: PROCESS_SOURCE_MISMATCH:{j.get('source')}")
    if j.get("phase")!=label:raise EvidenceError(f"{label}: WRONG_TEST_PLATFORM")
    if j.get("exitCode")!=manifest["known_outcomes"]["full_"+("edit" if label=="EditMode" else "play")]["exit"]:
        raise EvidenceError(f"{label}: WRONG_PROCESS_EXIT")
    if not ("-runTests" in j.get("arguments",[]) and "-testPlatform" in j.get("arguments",[]) and label in j.get("arguments",[])):
        raise EvidenceError(f"{label}: NOT_AN_EXPLICIT_NATIVE_TEST_INVOCATION")
    print(f"ORIGINAL_NATIVE_PROCESS={label} | ORIGINAL_GAME_SHA={manifest['game_sha']} | EXIT={j['exitCode']} | UNITY_INVOKED_BY_CODEX=YES")

def strict_audit(root):
    m=json.loads((HERE/"manifest.json").read_text(encoding="utf-8"))
    ref=m["evidence_sha"]
    if git(root,"rev-parse",ref+"^{commit}").decode().strip()!=ref:
        raise EvidenceError("EVIDENCE_COMMIT_MISSING")
    if git(root,"rev-parse",m["game_sha"]+"^{commit}").decode().strip()!=m["game_sha"]:
        raise EvidenceError("GAME_COMMIT_MISSING")
    data={p:original(root,ref,p,blob) for p,blob in m["original_git_blobs"].items()}
    for label,mode in (("EditMode","edit"),("PlayMode","play")):
        process_receipt(data[m["full_"+mode+"_process"]],m,label)
    old=nunit(data[m["full_edit"]],m["known_outcomes"]["full_edit"],"FULL_EDITOR_ORIGINAL")
    play=nunit(data[m["full_play"]],m["known_outcomes"]["full_play"],"FULL_PLAY_ORIGINAL")
    targeted=nunit(data[m["targeted_edit"]],m["known_outcomes"]["targeted_edit"],"TARGETED_EDIT_RETEST")
    actual_failed={n for n,v in old.items() if v=="Failed"}
    actual_targeted=set(targeted)
    if actual_failed!=actual_targeted:
        print("ORIGINAL_FULL_FAILURES_NOT_SAME_AS_TARGETED_CASES",file=sys.stderr)
        print(f"MISSING_IN_TARGETED={sorted(actual_failed-actual_targeted)}",file=sys.stderr)
        print(f"ADDITIONAL_IN_TARGETED={sorted(actual_targeted-actual_failed)}",file=sys.stderr)
        raise EvidenceError("TARGETED_RERUN_NOT_EXACT_SET_OF_15_ORIGINAL_FAILURES")
    if any(v!="Passed" for v in targeted.values()):raise EvidenceError("TARGETED_RUN_HAS_NON_PASSING_CASE")
    if data[m["targeted_exit"]].decode().strip()!="0":
        raise EvidenceError("TARGETED_NATIVE_EXIT_NOT_ZERO")
    if len(play)!=474:raise EvidenceError("PLAYMODE_FULL_NOT_EXACT")
    print("TARGETED_15_EQUALS_ORIGINAL_FAILED_15=YES | TARGETED_ACTUALLY_EXECUTED=YES | TARGETED_GITHUB_BYTES_STRICT=YES")
    print("IMPORTANT: targeted 15 PASS does not replace full 1303 EditMode run on test-only patched SHA")
    print(f"PASS16_ORIGINAL_EVIDENCE=VERIFIED | EDIT_FULL=RED_1288/1303 | PLAY_FULL=GREEN_474/474 | EDIT_TARGETED=GREEN_15/15 | B01-61_NATIVE_EXECUTION=NOT_RUN | FULL_EDIT_AFTER_TEST_PATCH=NOT_RUN | G1_G2=HOLD")

def synthetic_negative_suite():
    # Prove fail-closed against count spoof, duplicate fullname, skip, status forgery, bad raw XML.
    def make(total=2,p=1,f=1,sk=0,status="Failed(Child)",casestatus=("Passed","Failed"),same=False):
        r=ET.Element("test-run",{"total":str(total),"passed":str(p),"failed":str(f),
             "skipped":str(sk),"inconclusive":"0","testcasecount":str(total),"result":status})
        for i,st in enumerate(casestatus):
            ET.SubElement(r,"test-case",{"fullname":"sample."+("0" if same else str(i)),
                                        "result":st})
        return ET.tostring(r)
    expect={"total":2,"passed":1,"failed":1,"skipped":0}
    nunit(make(),expect,"SYNTHETIC_POSITIVE")
    bad=(
       ("count_forgery",make(total=3)),
       ("duplicate_name",make(same=True)),
       ("hidden_skip",make(p=1,f=0,sk=1,status="Passed",casestatus=("Passed","Skipped"))),
       ("fake_pass",make(status="Passed")),
       ("truncated_xml",b"<test-run"),
       ("mismatched_case_status",make(casestatus=("Passed","Passed"))),
    )
    for label,raw in bad:
        try:nunit(raw,expect,"SYNTHETIC_"+label)
        except EvidenceError:print("NEGATIVE_REJECTED="+label)
        else:raise EvidenceError(f"NEGATIVE_FAIL_OPEN:{label}")
    print(f"SYNTHETIC_RECEIPT_GUARDS=6/6_PASS | REAL_GAME_NATIVE_EXECUTION=NOT_RUN")

def main():
    ap=argparse.ArgumentParser()
    ap.add_argument("--repo",default=".")
    ap.add_argument("--selftest",action="store_true")
    ap.add_argument("--require-release",action="store_true")
    opts=ap.parse_args()
    if opts.selftest:
        synthetic_negative_suite()
        return 0
    strict_audit(opts.repo)
    if opts.require_release:
        print("EXPECTED_RELEASE_BLOCK: original full EditMode FAILED, subsequent full EditMode not run")
        return 1
    return 0

if __name__=="__main__":
    try:sys.exit(main())
    except Exception as exc:
        print(f"STRICT_RECEIPT_ERROR={type(exc).__name__}:{exc}",file=sys.stderr)
        sys.exit(2)
