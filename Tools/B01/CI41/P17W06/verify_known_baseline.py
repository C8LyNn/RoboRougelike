#!/usr/bin/env python3
"""Fail-closed source baseline comparator for known pre-existing MOV005/006 asmdef defects.

A green CI orchestration step NEVER means this Unity tree is healthy: the
strict graph verdict remains FAIL_SOURCE_GRAPH, and this comparator emits
KNOWN_BASELINE_DEFECTS_OPEN. Any new/missing/edited known defect fails CI.
"""
from __future__ import annotations

import argparse
import json
from pathlib import Path
import sys

from asmdef_graph_gate import committed_files, GraphGateError

SCHEMA = "CI41-ASMGRAPH-KNOWN-RED-BASELINE-v1"


def compare(report: dict, baseline: dict, entries: dict[str, str], source: str) -> dict:
    checks = []
    if baseline.get("schema") != SCHEMA:
        checks.append("INVALID_BASELINE_SCHEMA")
    if source != report.get("source_sha"):
        checks.append("SOURCE_SHA_REPORT_MISMATCH")
    if report.get("result") != "FAIL_SOURCE_GRAPH":
        checks.append("STRICT_GRAPH_MUST_REMAIN_EXPLICIT_FAIL_UNTIL_REVIEWED")
    if report.get("tracked_asmdef_count") != baseline.get("expected_asmdef_count"):
        checks.append("ASMDEF_COUNT_DRIFT")
    if report.get("native_unity") != "NOT_RUN":
        checks.append("INVALID_NATIVE_SCOPE")
    expected = baseline.get("known_errors")
    if not isinstance(expected, list):
        checks.append("BASELINE_KNOWN_ERRORS_INVALID")
        expected = []
    actual = report.get("errors", [])
    if not isinstance(actual, list):
        checks.append("STRICT_REPORT_ERRORS_INVALID")
        actual = []
    normalize = lambda x: sorted((e.get("code"), e.get("path"), e.get("detail", "")) for e in x
                                  if isinstance(e, dict))
    if normalize(actual) != normalize(expected):
        checks.append("KNOWN_RED_ERROR_SET_DRIFT")
    for pin in baseline.get("known_source_blob_pins", []):
        if not isinstance(pin, dict) or pin.get("path") not in entries:
            checks.append("MISSING_BASELINE_SOURCE_BLOB")
            continue
        if entries[pin["path"]] != pin.get("blob"):
            checks.append("KNOWN_DEFECT_SOURCE_BLOB_CHANGED")
    return {
        "package": "CI41-ASMGRAPH-001", "status":
        "KNOWN_BASELINE_DEFECTS_OPEN" if not checks else "BASELINE_REVIEW_REQUIRED",
        "source_sha": source, "native_unity": "NOT_RUN", "strict_source_graph_result":
        report.get("result"), "original_known_errors": actual,
        "baseline_issues": checks,
        "independent_qa": "NOT_RUN",
        "full_game_compile": "NOT_RUN",
        "known_defect_count": len(expected),
        "no_new_source_graph_errors": not checks,
        "game_source_g0": "HOLD_KNOWN_DEFECTS",
    }


def main() -> int:
    parser = argparse.ArgumentParser(description="Check exactly pinned known original asmdef errors; never greenwash strict gate")
    parser.add_argument("--repo", required=True)
    parser.add_argument("--source-sha", required=True)
    parser.add_argument("--report", required=True)
    parser.add_argument("--baseline", required=True)
    parser.add_argument("--out", required=True)
    args = parser.parse_args()
    try:
        report = json.loads(Path(args.report).read_text(encoding="utf-8"))
        baseline = json.loads(Path(args.baseline).read_text(encoding="utf-8"))
        observed = compare(report, baseline,
                           committed_files(Path(args.repo).resolve(), args.source_sha),
                           args.source_sha)
    except (OSError, ValueError, GraphGateError) as exc:
        observed = {"package": "CI41-ASMGRAPH-001", "status": "BASELINE_REVIEW_REQUIRED",
                    "error": str(exc), "native_unity": "NOT_RUN",
                    "game_source_g0": "HOLD"}
    out = Path(args.out)
    out.parent.mkdir(parents=True, exist_ok=True)
    data = json.dumps(observed, sort_keys=True, indent=2) + "\n"
    tmp = out.with_name(out.name + ".tmp")
    tmp.write_text(data, encoding="utf-8")
    tmp.replace(out)
    print(data)
    return 0 if observed["status"] == "KNOWN_BASELINE_DEFECTS_OPEN" else 2


if __name__ == "__main__":
    sys.exit(main())
