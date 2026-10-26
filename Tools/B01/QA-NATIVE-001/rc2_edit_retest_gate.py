#!/usr/bin/env python3
"""B01-60 QA-only: reconcile original full RC2 NUnit RED and later focused GREEN.

The gate NEVER converts a split-SHA targeted retest into full-suite PASS.
"""
from __future__ import annotations

import argparse
import json
import re
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

SHA_RE = re.compile(r"^[0-9a-f]{40}$")
ALLOWED_EDITOR_FILES = {
    "Assets/Tests/Editor/R1PlayIntegrationTests.cs",
    "Assets/Tests/Editor/RRLW34ReviewRegressionTests.cs",
}


def parse_nunit(path: Path) -> dict:
    root = ET.parse(path).getroot()
    if root.tag != "test-run":
        raise ValueError("WRONG_NUNIT_ROOT")
    cases = list(root.iter("test-case"))
    if not cases:
        raise ValueError("EMPTY_TEST_RUN")
    identities = [c.get("fullname") or c.get("name") or "" for c in cases]
    if not all(identities) or len(identities) != len(set(identities)):
        raise ValueError("DUPLICATE_OR_EMPTY_CASE_ID")
    counts = {
        "total": len(cases),
        "passed": sum(c.get("result") == "Passed" for c in cases),
        "failed": sum(c.get("result") == "Failed" for c in cases),
        "skipped": sum(c.get("result") == "Skipped" for c in cases),
    }
    if any(c.get("result") not in {"Passed", "Failed", "Skipped"} for c in cases):
        raise ValueError("UNKNOWN_CASE_RESULT")
    if any(int(root.get(k, "-1")) != v for k, v in counts.items()):
        raise ValueError("NUNIT_COUNT_MISMATCH")
    if int(root.get("testcasecount", str(counts["total"]))) != counts["total"]:
        raise ValueError("NUNIT_CASECOUNT_MISMATCH")
    expected_result = "Passed" if counts["failed"] == 0 else "Failed(Child)"
    if root.get("result") != expected_result:
        raise ValueError("NUNIT_STATUS_MISMATCH")
    return {**counts, "cases": identities, "failed_names": [
        identities[i] for i, case in enumerate(cases) if case.get("result") == "Failed"
    ]}


def _load(path: Path) -> dict:
    obj = json.loads(path.read_text(encoding="utf-8-sig"))
    if not isinstance(obj, dict):
        raise ValueError("INVALID_JSON_OBJECT")
    return obj


def evaluate(full_xml: Path, full_process: Path, retest_xml: Path,
             retest_process: Path, equivalence: Path) -> dict:
    full = parse_nunit(full_xml)
    retest = parse_nunit(retest_xml)
    p0, p1, eq = _load(full_process), _load(retest_process), _load(equivalence)
    if (p0.get("phase"), p1.get("phase")) != ("EditMode", "EditMode"):
        raise ValueError("NOT_EDITMODE")
    original, revised = p0.get("source"), p1.get("source")
    if not all(isinstance(s, str) and SHA_RE.fullmatch(s) for s in (original, revised)):
        raise ValueError("INVALID_SHA")
    if original == revised:
        raise ValueError("EXPECTED_DISTINCT_TEST_REVISION")
    if p0.get("exitCode") != 2 or p1.get("exitCode") != 0:
        raise ValueError("NATIVE_EXIT_MISMATCH")
    if full["total"] != 1068 or full["passed"] != 1066 or full["failed"] != 2 or full["skipped"] != 0:
        raise ValueError("UNEXPECTED_ORIGINAL_RC2_COUNTS")
    if retest["total"] != 2 or retest["passed"] != 2 or retest["failed"] != 0 or retest["skipped"] != 0:
        raise ValueError("NOT_EXACT_TWO_CASE_RETEST")
    if set(retest["cases"]) != set(full["failed_names"]):
        raise ValueError("RETEST_CASES_DO_NOT_MATCH_ORIGINAL_FAILURES")
    args0, args1 = p0.get("arguments"), p1.get("arguments")
    if not isinstance(args0, list) or not isinstance(args1, list):
        raise ValueError("MISSING_PROCESS_ARGUMENTS")
    if "-testFilter" in args0:
        raise ValueError("ORIGINAL_SUITE_FILTERED")
    if "-testFilter" not in args1:
        raise ValueError("TARGETED_RETEST_FILTER_MISSING")
    joined = " ".join(str(x) for x in args1)
    if not all(name in joined for name in retest["cases"]):
        raise ValueError("TARGETED_FILTER_CASE_MISMATCH")
    if eq.get("productionBuildSHA") != original or eq.get("editorTestRevision") != revised:
        raise ValueError("EQUIVALENCE_SHA_MISMATCH")
    if eq.get("productionChangesExcludingEditorTests") != []:
        raise ValueError("PRODUCTION_CHANGED_BETWEEN_SNAPSHOTS")
    changed = eq.get("changedUnityFiles")
    if (not isinstance(changed, list) or set(changed) != ALLOWED_EDITOR_FILES
            or len(changed) != len(ALLOWED_EDITOR_FILES)):
        raise ValueError("EDITOR_DIFF_NOT_WHITELISTED")
    if eq.get("buildAndExtractedAssemblyIdentical") is not True:
        raise ValueError("ASSEMBLY_IDENTITY_UNPROVEN")
    digest = eq.get("assemblySHA256")
    if not isinstance(digest, str) or not re.fullmatch(r"[0-9a-f]{64}", digest):
        raise ValueError("ASSEMBLY_DIGEST_INVALID")
    if eq.get("status") != "PRODUCTION_UNCHANGED; ONLY_TWO_EDITOR_FIXTURES_CORRECTED":
        raise ValueError("PRODUCTION_EQUIVALENCE_STATUS_INVALID")
    return {
        "schema": "B01-60-RC2-EDIT-RETEST-GATE-V1",
        "evidence_verdict": "PASS_SPLIT_SHA_RECEIPT_CONSISTENCY",
        "full_editmode_final_sha": "NOT_RUN",
        "independent_unity_execution": "NOT_RUN",
        "g2": "HOLD",
        "original_source_sha": original,
        "targeted_retest_source_sha": revised,
        "original": {k: full[k] for k in ("total", "passed", "failed", "skipped")},
        "targeted": {k: retest[k] for k in ("total", "passed", "failed", "skipped")},
        "retested_case_names": sorted(retest["cases"]),
        "next_native_action": "Run an unfiltered EditMode suite on the revised test-only SHA; capture original NUnit XML/log/exit."
    }


def main() -> int:
    p = argparse.ArgumentParser(description=__doc__)
    for arg in ("full-xml", "full-process", "retest-xml", "retest-process", "equivalence"):
        p.add_argument("--" + arg, required=True, type=Path)
    p.add_argument("--out", type=Path)
    a = p.parse_args()
    try:
        result = evaluate(a.full_xml, a.full_process, a.retest_xml,
                          a.retest_process, a.equivalence)
        code = 0
    except (ValueError, OSError, ET.ParseError, TypeError, KeyError, json.JSONDecodeError) as err:
        result = {"schema": "B01-60-RC2-EDIT-RETEST-GATE-V1", "evidence_verdict": "FAIL",
                  "problem": str(err), "full_editmode_final_sha": "NOT_RUN", "g2": "HOLD"}
        code = 2
    output = json.dumps(result, indent=2, sort_keys=True) + "\n"
    if a.out:
        a.out.write_text(output, encoding="utf-8")
    sys.stdout.write(output)
    return code


if __name__ == "__main__":
    raise SystemExit(main())
