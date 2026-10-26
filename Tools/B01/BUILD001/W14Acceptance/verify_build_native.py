#!/usr/bin/env python3
"""BUILD-only native evidence verifier; complements B01-60 qa_gate_v2.py.

Does not generate native evidence. Reports NOT_RUN until ORIGINAL test XML, Editor
logs and process records exist on disk. Passing this parser is not an independent
witness, live game approval, G1 or G2.
"""
import argparse
import json
from pathlib import Path
import xml.etree.ElementTree as ET

SOURCE_SHA = "95707c68c8071a949b5d9fec7ff57926e92f8198"
EDIT_TOTAL = 35  # R1.3 original18 + R1.4 new14 + original RC2 external3
PLAY_TOTAL = 6

def parse_nunit(raw, expected_total, expected_counts):
    root = ET.fromstring(raw)
    if root.tag != "test-run":
        raise ValueError("NOT_AN_NUNIT_TEST_RUN")
    cases = list(root.iter("test-case"))
    if len(cases) != expected_total or int(root.get("total", "-1")) != expected_total:
        raise ValueError("WRONG_TEST_TOTAL")
    if root.get("result") != "Passed" or any(x.get("result") != "Passed" for x in cases):
        raise ValueError("NATIVE_TEST_FAILURE_OR_SKIP")
    if int(root.get("passed", "-1")) != expected_total:
        raise ValueError("NUNIT_RUNNER_COUNT_MISMATCH")
    for class_fragment, expected in expected_counts.items():
        selected = [x for x in cases if class_fragment in (
            x.get("fullname", "") + " " + x.get("classname", ""))]
        if len(selected) != expected:
            raise ValueError("MISSING_OR_EXTRA_CASES:" + class_fragment)
    if expected_total == EDIT_TOTAL:
        receiver = [x for x in cases if "BuildRC2SafetyTests" in
                    (x.get("fullname", "")+" "+x.get("classname", ""))]
        if sum("NestedPatchMustSatisfyActualMinimumContactArea" in
               x.get("name", "") for x in receiver) != 2:
            raise ValueError("UNMODIFIED_RECEIVER_NEGATIVE_AND_POSITIVE_MISSING")
        if sum("AcceptedNestedPatchReportsItsTrueGeometricArea" in
               x.get("name", "") for x in receiver) != 1:
            raise ValueError("UNMODIFIED_RECEIVER_EXACT_AREA_MISSING")
    return {"total":len(cases), "passed":len(cases), "skipped":0, "failed":0}


def check_evidence(plan, evidence):
    output = {"schema":"B01-BUILD-R14-NATIVE-EVIDENCE-W14",
              "source_sha": SOURCE_SHA, "target_sha": plan.get("target_sha"),
              "status":"NOT_RUN", "g2":"HOLD", "attribution":"EVIDENCE_INSPECTION_NOT_NATIVE_WITNESS",
              "stages":{}, "problems":[]}
    problems = output["problems"]
    if plan.get("source_sha") != SOURCE_SHA or not plan.get("target_sha"):
        problems.append("STALE_SOURCE_OR_TARGET")
    expected = {
      "EditMode": (EDIT_TOTAL,
                   {"B01.BUILD001.Tests.Build001R14ContactAreaTests":14,
                    "BuildRC2SafetyTests":3}),
      "PlayMode": (PLAY_TOTAL,
                   {"B01.BUILD001.Tests.Build001PhysicalHitTests":2,
                    "B01.BUILD001.Tests.Build001R13PhysicalTests":4})
    }
    any_evidence = False
    for stage in ("Compile","EditMode","PlayMode"):
        folder = Path(evidence)/stage
        receipt = folder/"process.json"
        if not receipt.is_file():
            output["stages"][stage] = "NOT_RUN"
            problems.append(stage+":MISSING_PROCESS_RECEIPT")
            continue
        any_evidence = True
        try:
            record = json.loads(receipt.read_text(encoding="utf-8-sig"))
            if (record.get("stage") != stage or
                record.get("attribution") != "B01-60_CONTROLLED_RUNNER" or
                record.get("unity_version") != "6000.3.14f1" or
                record.get("source_sha") != SOURCE_SHA or
                record.get("target_sha") != plan["target_sha"] or
                type(record.get("exit_code")) is not int or
                record["exit_code"] != 0 or record.get("timeout") is not False):
                raise ValueError("INVALID_OR_NONZERO_NATIVE_RECEIPT")
            log = folder/"Editor.log"
            if not log.is_file() or not log.stat().st_size:
                raise ValueError("MISSING_EDITOR_LOG")
            if stage in expected:
                xmlpath=folder/"results.xml"
                if not xmlpath.is_file():raise ValueError("MISSING_ORIGINAL_NUNIT_XML")
                expected_total, expected_classes = expected[stage]
                output["stages"][stage] = parse_nunit(
                    xmlpath.read_text(encoding="utf8"),expected_total,expected_classes)
            else:
                output["stages"][stage] = "RECEIPT_PRESENT"
        except (KeyError,ValueError,OSError,ET.ParseError) as err:
            problems.append(stage+":"+str(err))
            output["stages"][stage] = "FAIL"
    if any(x == "FAIL" for x in output["stages"].values()) or (
       any_evidence and problems):
        output["status"] = "FAIL"
    elif problems:
        output["status"] = "NOT_RUN"
    else:
        output["status"] = "COMPLETE_EVIDENCE_REVIEW_REQUIRED"
    return output


def main():
    parser=argparse.ArgumentParser()
    parser.add_argument("--plan",required=True)
    parser.add_argument("--evidence",required=True)
    parser.add_argument("--out")
    args=parser.parse_args()
    plan=json.loads(Path(args.plan).read_text(encoding="utf8"))
    report=check_evidence(plan,Path(args.evidence))
    payload=json.dumps(report,indent=2,sort_keys=True)+"\n"
    if args.out:
        result=Path(args.out)
        if result.exists():raise SystemExit("REFUSE_TO_OVERWRITE_EXISTING_VERDICT")
        result.write_text(payload,encoding="utf8")
    print(payload)
    # Only status COMPLETE_EVIDENCE can be considered candidate for independent QA,
    # never a claim that producer executed or witnessed real Unity.
    return 0 if report["status"]=="COMPLETE_EVIDENCE_REVIEW_REQUIRED" else 2

if __name__=="__main__":
    raise SystemExit(main())
