#!/usr/bin/env python3
"""B01-60 W22: independent, READ-ONLY strict follow-up to frozen W21 evidence QA.

Does not run Unity or attest that recorded execution was genuine. No game/source
writes, no external network. The W21 verifier and native runner stay unchanged.
"""
import argparse
import json
from pathlib import Path
import sys
import xml.etree.ElementTree as ET

W21 = Path(__file__).resolve().parents[1] / "P17W21WindowsUnityHandoff"
sys.path.insert(0, str(W21))
import verify_evidence as original  # noqa: E402


def need(ok, reason):
    if not ok:
        raise original.Reject("W22_STRICT_HOLD: " + reason)


def valid_integer(element, name):
    raw = element.attrib.get(name)
    need(raw is not None and raw.isdecimal(), "NUnit root missing/invalid " + name)
    return int(raw)


def check_nunit(xml_path, expected, mode, verdict):
    try:
        root = ET.parse(xml_path).getroot()
    except (OSError, ET.ParseError) as exc:
        raise original.Reject("W22_STRICT_HOLD: missing/invalid " + mode + " XML") from exc
    need(root.tag == "test-run" and root.get("result") == "Passed",
         mode + " NUnit test-run not Passed")
    cases = list(root.iter("test-case"))
    fullnames = [c.get("fullname", "").replace(" ", "") for c in cases]
    need(len(cases) >= len(expected), mode + " fewer cases than required")
    need(all(fullnames) and len(set(fullnames)) == len(fullnames),
         mode + " duplicate/empty NUnit fullname")
    need(set(expected).issubset(fullnames), mode + " missing required original V2 cases")
    need(all(c.get("result") == "Passed" for c in cases),
         mode + " contains failed, skipped, inconclusive or unknown case")
    total = valid_integer(root, "total")
    passed = valid_integer(root, "passed")
    failed = valid_integer(root, "failed")
    need(total == passed == len(cases) and failed == 0,
         mode + " NUnit counts inconsistent with actual test-case nodes")
    for field in ("skipped", "inconclusive"):
        value = root.attrib.get(field, "0")
        need(value.isdecimal() and int(value) == 0, mode + " nonzero/invalid " + field)
    need(isinstance(verdict, dict), mode + " native verdict results absent")
    need(type(verdict.get("passed")) is int and verdict["passed"] == passed,
         mode + " native-verdict passed disagrees with raw XML")
    need(type(verdict.get("verified_independent_cases")) is int
         and verdict["verified_independent_cases"] == len(expected),
         mode + " native-verdict independent cases inconsistent")
    return len(cases)


def audit(evidence):
    e = Path(evidence).absolute()
    original_status = original.audit(e)
    report = original.read_json(e / "operator-report.json")
    if report.get("native_g1") == "NOT_RUN":
        need("native_process_exit" not in report,
             "native process exit is claimed on source-only receipt")
        need(report.get("real_unity") in (None, "NOT_RUN"),
             "source-only receipt claims real Unity")
        return "SOURCE_ONLY_PASS_NATIVE_NOT_RUN"
    need(original_status == "STRUCTURALLY_CONSISTENT_V2_ONLY_NOT_NATIVE_ATTESTATION",
         "W21 original native structural check did not pass")
    verdict = original.read_json(e / "original-V2-native-only/native-verdict.json")
    results = verdict.get("results")
    need(isinstance(results, dict) and set(results) == {"EditMode", "PlayMode"},
         "original native verdict modes missing/foreign")
    totals = {}
    for mode, required in (("EditMode", original.EDIT), ("PlayMode", original.PLAY)):
        totals[mode] = check_nunit(
            e / "original-V2-native-only" / (mode + ".xml"),
            required, mode, results[mode])
    return "STRUCTURALLY_CONSISTENT_V2_ONLY_UNATTESTED_" + \
           str(totals["EditMode"]) + "_EDIT_" + str(totals["PlayMode"]) + "_PLAY"


def main():
    parser = argparse.ArgumentParser(description="Read-only W22 verification of W21 evidence")
    parser.add_argument("--evidence", required=True)
    args = parser.parse_args()
    try:
        status = audit(args.evidence)
        print("B0160_W22_EVIDENCE=" + status)
        print("B0160_W22_UNITY_PROCESS=NOT_STARTED_BY_THIS_AUDIT")
        print("B0160_W22_NATIVE_R5_690=NOT_VERIFIED CANONICAL_691=NOT_VERIFIED G2=HOLD")
        return 0
    except (original.Reject, OSError, UnicodeError, ValueError) as exc:
        print("B0160_W22_REJECT=" + str(exc), file=sys.stderr)
        return 2


if __name__ == "__main__":
    sys.exit(main())
