#!/usr/bin/env python3
"""CI41 W20: independent, NON-privileged G1 evidence verifier for B01-60/#667.

No SSH, no Unity launch, no access to user's project; file provenance/consistency
is structural only. A self-authored log never constitutes independently witnessed
native Unity execution. "Release" fails closed without signed owner R4 selection
and a separate human-reviewed Unity attestation.
"""
from __future__ import annotations
import argparse
from datetime import datetime
import hashlib
import json
from pathlib import Path, PurePosixPath
import re
import sys
import xml.etree.ElementTree as ET

GAME="d94400d95d13db7f27e1533df05c9292d5380431"
SAVE="33094f154c7b91119b36d58aab285e6e83c5de57"
BASE="22cec9888047994ae523cbe7bf1b0a483d64cec4"
R4={
 "647":{"sha":"09c1809b73324a6d31c13e5d0143de62ac1e3ca6",
        "core_blob":"8fd1dd9a1bc870433809da8fba897ec924fa4113",
        "host_blob":"82147492a9205f8ef5986ceb61aeb10d6ea24ff2"},
 "652":{"sha":"ae5dbfa5e9c4689d2bf80fcabf8438e9c673c419",
        "core_blob":"04866a5682d4898a3f8ba0cfa9083cb684213269",
        "host_blob":"7b8354bc4cd846227b4bdf6d25d81bed34a325bc"}
}
# B01-23 OWNER signed exact #647 only at PR #647 issuecomment-6531727970.
# #652 remains ALT_HOLD despite historical own producer source tests.
OWNER_SIGNED_R4="647"
MIN_CASES={"EditMode":2,"PhysX":14,"SaveBridge":6}
SENTINEL={
 "EditMode":{"ExactOwnerRuntimeAndOriginalW06AreLinkableByUnity",
             "RealOriginalW06LedgerClearFailsClosedOnMissingIdBeforeCallbacks"},
 "PhysX":{"ForcedMidCommitExceptionRestoresRealRigidbodyAndConsumedId",
          "RollbackFailureIsQuarantinedRatherThanSilentlyAccepted"},
 "SaveBridge":{"ColdLoad_RealRigidbody_RoundTripLightHeavy",
               "ColdRestart_RestoresRealRigidbodyAndConsumedIds",
               "DisabledSameSlotHost_CannotStealOwnerUntilDestroyed"}
}
class Denied(Exception):pass
def require(yes,why):
    if not yes:raise Denied(why)

def safe_file(root,relative):
    require(isinstance(relative,str) and bool(relative) and
            "\\" not in relative and "\x00" not in relative and
            bool(re.fullmatch(r"[A-Za-z0-9._/-]+",relative)),
            "UNSAFE_EVIDENCE_PATH")
    p=PurePosixPath(relative)
    require(not p.is_absolute() and
            all(x not in ("..",".","") for x in p.parts),
            "PATH_ESCAPE_OR_DOT_COMPONENT")
    cur=root
    require(root.is_dir() and not root.is_symlink(),"ROOT_NOT_SAFE_DIRECTORY")
    for part in p.parts:
        cur=cur/part
        require(not cur.is_symlink(),"SYMLINK_IN_EVIDENCE_TREE")
    require(cur.is_file() and cur.stat().st_size>0,"MISSING_OR_EMPTY_EVIDENCE_FILE")
    require(cur.resolve().is_relative_to(root.resolve()),"EVIDENCE_PATH_ESCAPE")
    return cur

def file_content(root,desc):
    require(isinstance(desc,dict) and
            set(desc)=={"path","sha256"} and
            isinstance(desc["sha256"],str) and
            re.fullmatch(r"[a-f0-9]{64}",desc["sha256"]) is not None,
            "INVALID_SOURCE_FILE_DESCRIPTOR")
    f=safe_file(root,desc["path"])
    raw=f.read_bytes()
    require(hashlib.sha256(raw).hexdigest()==desc["sha256"],
            "EVIDENCE_SHA256_MISMATCH")
    require(len(raw)<=16_000_000,"EVIDENCE_FILE_TOO_LARGE")
    return raw

def time_value(s):
    require(isinstance(s,str) and bool(re.fullmatch(
        r"\d{4}-\d\d-\d\dT\d\d:\d\d:\d\d(?:\.\d+)?(?:Z|[+-]\d\d:\d\d)",s)),
        "TIME_NOT_OFFSET_AWARE_ISO8601")
    return datetime.fromisoformat(s.replace("Z","+00:00"))

def xml_cases(raw,suite):
    require(b"<!DOCTYPE" not in raw.upper() and b"<!ENTITY" not in raw.upper(),
            "XML_ENTITY_OR_DOCTYPE")
    try:root=ET.fromstring(raw)
    except ET.ParseError as e:raise Denied("NUNIT_XML_MALFORMED") from e
    require(root.tag=="test-run","NOT_NUNIT_TEST_RUN_XML")
    summary=root.attrib
    require(summary.get("result")=="Passed" and
            all(summary.get(k,"").isdigit() for k in ("total","passed","failed","skipped")),
            "NUNIT_NOT_PASSED_OR_SUMMARY_INCOMPLETE")
    total=int(summary["total"])
    require(total>=MIN_CASES[suite] and
            int(summary["passed"])==total and
            int(summary["failed"])==0 and int(summary["skipped"])==0,
            "NUNIT_NOT_ALL_CASES_EXECUTED_AND_PASSED")
    names=[]
    for case in root.iter("test-case"):
        require(case.attrib.get("result")=="Passed" and
                case.attrib.get("executed","True").lower()!="false",
                "NUNIT_FAILED_OR_SKIPPED_CASE")
        name=case.attrib.get("fullname") or case.attrib.get("name","")
        require(bool(name),"NUNIT_MISSING_CASE_IDENTITY")
        names.append(name)
    require(len(names)==total and len(set(names))==total,
            "NUNIT_SUMMARY_CASE_COUNT_OR_DUPLICATE")
    short={n.split(".")[-1] for n in names}
    require(SENTINEL[suite].issubset(short),"NUNIT_ORIGINAL_SUITE_SENTINELS_MISSING")
    return names

def verify(root):
    root=Path(root)
    manifest=safe_file(root,"manifest.json")
    try:m=json.loads(manifest.read_text(encoding="utf-8"))
    except (ValueError,UnicodeError) as e:raise Denied("INVALID_MANIFEST_JSON") from e
    require(m.get("schema")=="B01-CI41-UNITY-NATIVE-EVIDENCE-001/R1",
            "UNSUPPORTED_EVIDENCE_SCHEMA")
    p=str(m.get("producer_pr",""))
    require(p==OWNER_SIGNED_R4 and p in R4 and
            m.get("source_sha")==R4[p]["sha"],
            "UNTRUSTED_OR_MIXED_R4_SOURCE_SHA")
    require(m.get("source_r3_parent")==BASE and
            m.get("frozen_game_sha")==GAME and m.get("frozen_save_sha")==SAVE,
            "FROZEN_GIT_PROVENANCE_MISMATCH")
    require(m.get("unity_version")=="6000.3.14f1","WRONG_UNITY_EDITOR_VERSION")
    require(m.get("operator_invoked") is True,"NOT_USER_INITIATED_NATIVE_RUN")
    require(m.get("source_sha_after")==m["source_sha"],
            "SOURCE_MUTATED_DURING_UNITY_IMPORT")
    require(isinstance(m.get("sessions"),list) and len(m["sessions"])==3,
            "MISSING_OR_ADDITIONAL_NATIVE_SESSIONS")
    seen=set()
    results={}
    all_cases=set()
    for item in m["sessions"]:
        require(isinstance(item,dict),"BAD_SESSION")
        suite=item.get("suite")
        require(suite in MIN_CASES and suite not in seen,"DUPLICATE_OR_UNKNOWN_SUITE")
        seen.add(suite)
        require(item.get("exit_code") == 0 and type(item.get("exit_code")) is int,
                "UNITY_PROCESS_NONZERO_OR_MISSING")
        require(item.get("actual_test_platform")==
                ("EditMode" if suite=="EditMode" else "PlayMode"),
                "WRONG_NATIVE_TEST_PLATFORM")
        command=item.get("command")
        require(isinstance(command,list) and
                all(isinstance(x,str) for x in command),
                "MISSING_UNITY_COMMAND_ARRAY")
        require({"-batchmode","-runTests","-testResults","-testPlatform"}.issubset(
            set(command)), "NOT_NATIVE_UNITY_TEST_COMMAND")
        require(any(x.lower().endswith("unity.exe") for x in command[:1]),
                "NO_WINDOWS_UNITY_EDITOR_BINARY")
        start=time_value(item.get("started_at"))
        end=time_value(item.get("ended_at"))
        require(end>start,"INVALID_NATIVE_RUN_DURATION")
        log=file_content(root,item.get("editor_log"))
        stdout=file_content(root,item.get("stdout"))
        xml=file_content(root,item.get("nunit_xml"))
        require(b"6000.3.14f1" in log and
                re.search(rb"Unity|Initialize engine version",log,re.I) is not None,
                "UNITY_EDITOR_LOG_VERSION_OR_ENGINE_MARKER_MISSING")
        require(b"license activation failed" not in log.lower() and
                b"not activated" not in log.lower() and
                b"license error" not in log.lower(),
                "UNITY_LICENSE_ERROR_PRESENT")
        require(b"error: unable to activate" not in stdout.lower(),
                "UNITY_ACTIVATION_FAILURE_STDOUT")
        cases=xml_cases(xml,suite)
        require(not (all_cases&set(cases)),"NATIVE_TEST_CASE_REPLAY_BETWEEN_SUITES")
        all_cases.update(cases)
        results[suite]={"cases":len(cases),
                        "editor_log_sha256":item["editor_log"]["sha256"],
                        "xml_sha256":item["nunit_xml"]["sha256"]}
    require(seen==set(MIN_CASES),"NATIVE_SUITES_INCOMPLETE")
    return {"package":"CI41-W20-NATIVE-EVIDENCE-GATE-001/R1",
            "result":"STRUCTURALLY_CONSISTENT_NOT_PROVEN_NATIVE_EXECUTION",
            "candidate_pr":p,"candidate_source_sha":m["source_sha"],
            "owner_signed_r4":OWNER_SIGNED_R4,
            "g0":"EXTERNAL_B01_40_REQUIRED",
            "g1":"NOT_CERTIFIED_REQUIRE_LOCAL_HUMAN_EDITOR_EVIDENCE_REVIEW",
            "suites":results,"total_nunit_cases":len(all_cases),
            "g2":"HOLD","codex_ack":False,"merged":False}

def main():
    ap=argparse.ArgumentParser()
    ap.add_argument("--evidence",required=True)
    ap.add_argument("--output",required=True)
    ap.add_argument("--enforce",action="store_true",
                    help="fail closed until original #647 orphan-host P0 and independent native attestation are resolved")
    a=ap.parse_args()
    try:
        result=verify(a.evidence)
        code=0
        if a.enforce:
            result["release_gate"]="HOLD_HOST_P0_AND_NATIVE_UNATTESTED"
            code=3
    except (Denied,ValueError,TypeError,OSError,UnicodeError) as e:
        result={"result":"REJECTED_EVIDENCE","reason":str(e),
                "g1":"NOT_RUN_OR_UNVERIFIABLE","release_gate":"HOLD"}
        code=2
    Path(a.output).write_text(json.dumps(result,sort_keys=True,indent=2)+"\n",
                             encoding="utf-8")
    print(json.dumps(result,sort_keys=True))
    return code
if __name__=="__main__":sys.exit(main())
