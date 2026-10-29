#!/usr/bin/env python3
"""CI41-NATIVE-TRIAGE-001: fact-checked Unity NUnit receipt triage.

Reads immutable *original Git blob bytes*, never invokes Unity, never
synthesizes native evidence and NEVER changes an original failure to PASS
because a later filtered retest succeeded. Python 3 stdlib.
"""
from __future__ import annotations

import argparse
import json
from pathlib import Path
import re
import subprocess
import sys
import xml.etree.ElementTree as ET

SHA = re.compile(r"[0-9a-f]{40}\Z")
ROOT = "Docs/P17Reload/Evidence/"
G1_SOURCE_LABEL = "SOURCE_RECEIPT_TRIAGE_NOT_NATIVE_EXECUTION"
SPECS = {
    "full_edit": ("EditMode.xml", "EditMode-process.json", "EditMode", False),
    "full_play": ("PlayMode.xml", "PlayMode-process.json", "PlayMode", False),
    "target_edit": ("Targeted-EditMode.xml", "Targeted-EditMode-process.json", "EditMode", True),
    "failure_retest": ("Failure-retest.xml", "Failure-retest-process.json", "EditMode", True),
}
MAX_XML = 4_000_000
MAX_JSON = 20_000


class ReceiptError(Exception):
    pass


def git(repo: Path, *args: str) -> bytes:
    try:
        r = subprocess.run(["git", "-C", str(repo), *args],
                           capture_output=True, check=False)
    except OSError as exc:
        raise ReceiptError("GIT_NOT_AVAILABLE") from exc
    if r.returncode:
        raise ReceiptError("GIT_FAILED:" + " ".join(args[:3]))
    return r.stdout


def validate_source(repo: Path, receipt_commit: str, gameplay_sha: str) -> None:
    for name, sha in (("RECEIPT", receipt_commit), ("GAMEPLAY", gameplay_sha)):
        if not isinstance(sha, str) or not SHA.fullmatch(sha):
            raise ReceiptError(name + "_EXACT_SHA_REQUIRED")
        actual = git(repo, "rev-parse", "--verify", sha + "^{commit}").decode().strip()
        if actual != sha:
            raise ReceiptError(name + "_OBJECT_MISMATCH")
    try:
        git(repo, "merge-base", "--is-ancestor", gameplay_sha, receipt_commit)
    except ReceiptError as exc:
        raise ReceiptError("GAMEPLAY_NOT_ANCESTOR_OF_RECEIPT") from exc


def original_blob(repo: Path, sha: str, filename: str, limit: int) -> bytes:
    if filename not in {entry for pair in SPECS.values() for entry in pair[:2]}:
        raise ReceiptError("UNTRUSTED_EVIDENCE_PATH")
    # Limit checked after immutable Git object extraction.
    data = git(repo, "show", sha + ":" + ROOT + filename)
    if not data or len(data) > limit:
        raise ReceiptError("MISSING_OR_OVERSIZED_ORIGINAL_EVIDENCE:" + filename)
    return data


def integer_count(element: ET.Element, key: str) -> int:
    try:
        num = int(element.attrib[key])
    except (KeyError, ValueError) as exc:
        raise ReceiptError("INVALID_NUNIT_COUNT:" + key) from exc
    if num < 0 or num > 50000:
        raise ReceiptError("NUNIT_COUNT_OUT_OF_RANGE:" + key)
    return num


def parse_nunit(blob: bytes, name: str) -> dict:
    if len(blob) > MAX_XML or not blob.lstrip().startswith(b"<?xml"):
        raise ReceiptError("INVALID_XML_SOURCE:" + name)
    # Expat inside ElementTree does not fetch external resources; reject
    # DTD/entities as an extra fail-closed policy for untrusted XML bytes.
    if b"<!DOCTYPE" in blob or b"<!ENTITY" in blob:
        raise ReceiptError("XML_DTD_FORBIDDEN:" + name)
    try:
        root = ET.fromstring(blob)
    except ET.ParseError as exc:
        raise ReceiptError("MALFORMED_NUNIT_XML:" + name) from exc
    if root.tag != "test-run":
        raise ReceiptError("NOT_NUNIT_TEST_RUN:" + name)
    reported = {k: integer_count(root, k) for k in
                ("total", "passed", "failed", "inconclusive", "skipped")}
    if reported["total"] != sum(reported[k] for k in
                               ("passed", "failed", "inconclusive", "skipped")):
        raise ReceiptError("INCONSISTENT_NUNIT_ROOT_COUNTS:" + name)

    counts = {"passed": 0, "failed": 0, "inconclusive": 0, "skipped": 0}
    seen = set()
    failing = []
    skipped = []
    tests = root.iter("test-case")
    for test in tests:
        full = test.get("fullname")
        if not full or full in seen:
            raise ReceiptError("MISSING_OR_DUPLICATE_TESTCASE:" + name)
        seen.add(full)
        result = test.get("result")
        category = {
            "Passed": "passed", "Failed": "failed",
            "Inconclusive": "inconclusive", "Skipped": "skipped",
        }.get(result)
        if category is None:
            raise ReceiptError("UNRECOGNIZED_NUNIT_RESULT:" + str(result))
        counts[category] += 1
        if category == "failed":
            msg = test.findtext("./failure/message") or ""
            stack = test.findtext("./failure/stack-trace") or ""
            failing.append({"fullname": full, "message": msg.strip()[:450],
                            "first_stack_frame": next(iter(stack.strip().splitlines()), "")[:330]})
        if category == "skipped":
            reason = test.findtext("./reason/message") or ""
            skipped.append({"fullname": full, "reason": reason.strip()[:330]})
    if reported != {"total": len(seen), **counts} or len(seen) == 0:
        raise ReceiptError("ROOT_LEAF_TESTCASE_COUNT_MISMATCH:" + name)
    success = (reported["failed"] == 0 and reported["inconclusive"] == 0
               and reported["skipped"] == 0 and root.get("result") == "Passed")
    if not success and root.get("result") == "Passed":
        raise ReceiptError("NUNIT_FALSE_GREEN_ROOT:" + name)
    if success and root.get("result") != "Passed":
        raise ReceiptError("NUNIT_FALSE_RED_ROOT:" + name)
    return {
        "name": name, "total": reported["total"],
        "passed": reported["passed"], "failed": reported["failed"],
        "inconclusive": reported["inconclusive"], "skipped": reported["skipped"],
        "root_result": root.get("result"), "failed_tests": sorted(failing, key=lambda t: t["fullname"]),
        "skipped_tests": sorted(skipped, key=lambda t: t["fullname"]),
        "all_testcases_passed_without_skip": success,
    }


def parse_process(blob: bytes, mode: str, filtered: bool, source: str) -> dict:
    if len(blob) > MAX_JSON:
        raise ReceiptError("OVERSIZED_PROCESS_RECEIPT")
    try:
        p = json.loads(blob)
    except (ValueError, UnicodeDecodeError) as exc:
        raise ReceiptError("INVALID_PROCESS_JSON") from exc
    if not isinstance(p, dict) or p.get("source") != source:
        raise ReceiptError("PROCESS_GAMEPLAY_SHA_MISMATCH")
    if p.get("phase") != mode or not isinstance(p.get("exitCode"), int) or isinstance(p.get("exitCode"), bool):
        raise ReceiptError("PROCESS_MODE_OR_EXIT_INVALID")
    args = p.get("arguments")
    if not isinstance(args, list) or any(not isinstance(a, str) for a in args):
        raise ReceiptError("PROCESS_ARGUMENTS_MISSING")
    if ("-runTests" not in args or "-testPlatform" not in args or
            args[args.index("-testPlatform") + 1: args.index("-testPlatform") + 2] != [mode]):
        raise ReceiptError("NOT_NATIVE_TEST_COMMAND_FOR_MODE")
    if "-testResults" not in args or not any(".xml" in a for a in args):
        raise ReceiptError("PROCESS_XML_OUTPUT_NOT_DECLARED")
    if ("-testFilter" in args) != filtered:
        raise ReceiptError("UNDECLARED_OR_UNEXPECTED_TARGET_FILTER")
    if filtered and not args[args.index("-testFilter") + 1:]:
        raise ReceiptError("EMPTY_NATIVE_TEST_FILTER")
    return {"exit_code": p["exitCode"], "mode": mode,
            "source_sha": source, "filtered": filtered, "stage": str(p.get("stage", ""))}


def evaluate_receipts(evidence: dict[str, tuple[bytes, bytes]], gameplay_sha: str) -> dict:
    if set(evidence) != set(SPECS):
        raise ReceiptError("INCOMPLETE_NATIVE_EVIDENCE_SET")
    details = {}
    for key, (xmlfile, processfile, mode, filtered) in SPECS.items():
        xml, process = evidence[key]
        tests = parse_nunit(xml, xmlfile)
        proc = parse_process(process, mode, filtered, gameplay_sha)
        # We are validating historic native evidence. We do NOT claim this
        # Python invocation ran the Unity Editor.
        native_green = tests["all_testcases_passed_without_skip"]
        if proc["exit_code"] == 0 and not native_green:
            raise ReceiptError("ZERO_NATIVE_EXIT_WITH_FAILED_XML:" + key)
        if proc["exit_code"] != 0 and native_green:
            raise ReceiptError("NONZERO_NATIVE_EXIT_WITH_GREEN_XML:" + key)
        details[key] = {**tests, **proc}

    full_edit, full_play = details["full_edit"], details["full_play"]
    failed = {x["fullname"] for x in full_edit["failed_tests"]}
    retested = {x["fullname"] for x in details["failure_retest"]["failed_tests"]}
    passed_retests = set()
    # original retest success is scoped: just named testcases in the retest
    # XML, NEVER a substitute for full suite pass.
    retest_xml = evidence["failure_retest"][0]
    retest_root = ET.fromstring(retest_xml)
    for case in retest_root.iter("test-case"):
        if case.get("result") == "Passed" and case.get("fullname") in failed:
            passed_retests.add(case.get("fullname"))
    failed_not_retested = sorted(failed - passed_retests)
    any_full_failure = (not full_edit["all_testcases_passed_without_skip"]
                        or not full_play["all_testcases_passed_without_skip"])
    return {
        "package": "CI41-NATIVE-TRIAGE-001",
        "gameplay_sha": gameplay_sha,
        "evidence_kind": G1_SOURCE_LABEL,
        "status": "HOLD_FULL_NATIVE_REGRESSION" if any_full_failure else "SOURCE_EVIDENCE_ALL_FULL_PASSED_REQUIRES_QA",
        "full_edit": full_edit,
        "full_play": full_play,
        "targeted_edit": details["target_edit"],
        "failure_retest": details["failure_retest"],
        "original_full_edit_failed_but_retest_passed": sorted(passed_retests),
        "original_full_edit_failed_without_green_retest": failed_not_retested,
        "independent_native_qa": "NOT_RUN_BY_THIS_TOOL",
        "game_integrated_by_this_tool": False,
        "g1_assessment": "HOLD" if any_full_failure else "SOURCE_ONLY_REVIEW_REQUIRED",
    }


def inspect(repo: Path, evidence_sha: str, gameplay_sha: str) -> dict:
    validate_source(repo, evidence_sha, gameplay_sha)
    contents = {
        key: (original_blob(repo, evidence_sha, xmlfile, MAX_XML),
              original_blob(repo, evidence_sha, processfile, MAX_JSON))
        for key, (xmlfile, processfile, _mode, _filtered) in SPECS.items()
    }
    return {"evidence_commit_sha": evidence_sha, **evaluate_receipts(contents, gameplay_sha)}


def main() -> int:
    parser = argparse.ArgumentParser(description="Read-only, original-native-XML triage; not Unity execution")
    parser.add_argument("--repo", default=".")
    parser.add_argument("--evidence-commit", required=True)
    parser.add_argument("--gameplay-sha", required=True)
    args = parser.parse_args()
    try:
        data = inspect(Path(args.repo).resolve(strict=True), args.evidence_commit, args.gameplay_sha)
        print(json.dumps(data, indent=2, sort_keys=True, ensure_ascii=False))
        return 0 if data["status"] == "SOURCE_EVIDENCE_ALL_FULL_PASSED_REQUIRES_QA" else 2
    except (ReceiptError, OSError, ValueError, ET.ParseError, UnicodeError) as exc:
        print(json.dumps({"status": "FAIL_CLOSED_UNVERIFIABLE_NATIVE_RECEIPT",
                          "package": "CI41-NATIVE-TRIAGE-001",
                          "error": str(exc), "native_unity": "NOT_RUN_BY_THIS_TOOL"}, sort_keys=True))
        return 2


if __name__ == "__main__":
    sys.exit(main())
