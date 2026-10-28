#!/usr/bin/env python3
"""Independently inspect ORIGINAL committed Codex NUnit XML. No Unity execution."""
import argparse
import json
import subprocess
import sys
import xml.etree.ElementTree as ET

EVIDENCE = {
 "EditMode": "Docs/SuspensionUx309/Evidence/EditMode.xml",
 "PlayMode": "Docs/SuspensionUx309/Evidence/PlayMode.xml",
}
REQUIRED_NATIVE_CLASSES = {
 "garage": ("GarageEditFreedomTests", "WeaponGroupTests"),
 "map": ("MapEditorIntegrationTests", "MapEditorExpansionTests"),
 "MOV": ("MOV009IntegrationTests", "MOV009ModelTests", "SuspensionUxTests", "ActiveSuspensionPlayModeTests"),
}
OPEN_NEGATIVE_CASES = {
 "D03_dual_critical_while_tips_OFF": ("Critical", "BothWarnings", "DualWarning"),
 "D04_camera_null_held_release": ("CameraLoss", "CameraMissing", "CameraNull"),
 "R07_remount_B_only_same_id": ("RemountB", "RemountWeaponGroupB", "RemountRestoresB"),
 "B0161_cross_feature_map_test": ("EditorGarageMapTestReturnPreservesGroupsSuspensionIdsAndScrap",),
}
def source(repo, sha, path):
    return subprocess.check_output(["git", "-C", repo, "show", sha+":"+path])
def main():
    a=argparse.ArgumentParser()
    a.add_argument("--repo", default=".")
    a.add_argument("--sha", required=True)
    a.add_argument("--require-full", action="store_true")
    opt=a.parse_args()
    output={"codex_sha":opt.sha,"evidence_kind":"ORIGINAL_CODEX_COMMITTED_XML_READ_ONLY_NOT_B0161_UNITY_RUN",
            "suites":{},"required_coverage":{},"negative_case_gaps":{},"release_ready":False}
    all_cases=[]
    checks=[]
    for platform,path in EVIDENCE.items():
        raw_xml=source(opt.repo,opt.sha,path)
        raw_well_formed=True
        redacted_placeholder_count=raw_xml.count(b"<WORKSPACE>")
        try:
            root=ET.fromstring(raw_xml)
        except ET.ParseError as e:
            # Original Codex QA XML puts literal '<WORKSPACE>' inside fullname
            # attributes, which is INVALID XML. Do not silently certify raw bytes.
            if not redacted_placeholder_count:
                raise
            raw_well_formed=False
            # Normalize ONLY this known public redaction in memory for testcase
            # enumeration; the immutable Git XML remains invalid and is recorded.
            root=ET.fromstring(raw_xml.replace(b"<WORKSPACE>",b"WORKSPACE_REDACTED"))
        actual=root.findall(".//test-case")
        names=[c.attrib.get("fullname") or c.attrib.get("name","") for c in actual]
        all_cases+=names
        count=int(root.attrib.get("total","-1"))
        passed=int(root.attrib.get("passed","-1"))
        discovered=max(int(x.attrib.get("testcasecount","0"))
                       for x in root.iter("test-suite"))
        run_ok=root.tag=="test-run" and root.attrib.get("result")=="Passed" \
               and count==len(actual)==passed and int(root.attrib.get("failed","-1"))==0
        checks.append(run_ok)
        output["suites"][platform]={
          "source_path":path,"reported_total":count,"parsed_actual_cases":len(actual),
          "passed":passed,"discovered_suite_cases":discovered,"all_passed":run_ok,
          "full_discovered_suite_executed":run_ok and count==discovered,
          "raw_original_xml_well_formed":raw_well_formed,
          "invalid_redaction_PLACEHOLDER_count":redacted_placeholder_count,
          "raw_invalid_xml_is_release_blocker":not raw_well_formed,
          "classes":sorted(set(n.rsplit(".",1)[0].rsplit(".",1)[-1] for n in names))
        }
    for group,cl in REQUIRED_NATIVE_CLASSES.items():
        output["required_coverage"][group]={c:any(c in x for x in all_cases) for c in cl}
    for issue,signals in OPEN_NEGATIVE_CASES.items():
        output["negative_case_gaps"][issue]={
         "explicit_case_in_selected_xml":any(any(s in n for s in signals) for n in all_cases),
         "signals":list(signals)
        }
    full=all(s["full_discovered_suite_executed"] and
             s["raw_original_xml_well_formed"] for s in output["suites"].values())
    negatives=all(v["explicit_case_in_selected_xml"] for v in output["negative_case_gaps"].values())
    output["release_ready"]=all(checks) and full and negatives
    print(json.dumps(output,indent=2,sort_keys=True))
    print("NUNIT_XML_AUDIT="+("PASS_TARGETED_CASE_ENUMERATION_AFTER_EXPLICIT_REDACTION_NORMALIZATION" if all(checks) else "FAIL_XML_CASES"))
    if any(not s["raw_original_xml_well_formed"] for s in output["suites"].values()):
        print("RAW_XML_INVALID_UNESCAPED_WORKSPACE_REDACTION=YES | ORIGINAL_BYTES_NOT_VALID_XML")
    print("RELEASE_GATE="+("PASS" if output["release_ready"] else "BLOCKED_NO_FULL_AND_NEGATIVE_NATIVE_PROOF"))
    return int(not all(checks) or (opt.require_full and not output["release_ready"]))
if __name__=="__main__":
    sys.exit(main())
