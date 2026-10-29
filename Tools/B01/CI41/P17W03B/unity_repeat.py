#!/usr/bin/env python3
"""CI41-REPLAY-001: fail-closed, repeatable Unity test repro harness.

Executes a *provided* Unity Editor executable on an exact clean original Git
game source. This Python module cannot grant native Unity pass without actually
running that Editor. Preserve every original test XML/log and process result.

Run only on an exclusive project checkout: Unity Library can be written by the
Editor even though no Assets/ game source is changed by this tool.
"""
from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path
import re
import subprocess
import sys
import time
import xml.etree.ElementTree as ET

SHA = re.compile(r"[0-9a-f]{40}\Z")
VALID_CASE = re.compile(r"[A-Za-z_][A-Za-z0-9_.+]*(?:\([A-Za-z0-9_, .:+-]*\))?\Z")
MAX_BYTES = 8_000_000


class ReplayError(Exception):
    pass


def git(repo: Path, *args: str) -> bytes:
    try:
        p = subprocess.run(["git", "-C", str(repo), *args],
                           capture_output=True, check=False)
    except OSError as exc:
        raise ReplayError("GIT_NOT_AVAILABLE") from exc
    if p.returncode:
        raise ReplayError("GIT_COMMAND_FAILED:" + " ".join(args[:2]))
    return p.stdout


def source_guard(project: Path, source: str) -> None:
    if not SHA.fullmatch(source):
        raise ReplayError("EXACT_40_HEX_SOURCE_SHA_REQUIRED")
    actual = git(project, "rev-parse", "--verify", "HEAD^{commit}").decode().strip()
    if actual != source:
        raise ReplayError("WORKTREE_SOURCE_SHA_MISMATCH")
    # Purely source tracked dirtiness check. Unity local Library can be dirty,
    # but Codex-owned assets and project settings must be unchanged.
    dirty = git(project, "status", "--porcelain", "--untracked-files=no", "--",
                "Assets", "Packages", "ProjectSettings")
    if dirty:
        raise ReplayError("TRACKED_UNITY_SOURCE_DIRTY")


def validate_names(filters: list[str]) -> list[str]:
    if not filters or len(filters) > 8 or len(set(filters)) != len(filters):
        raise ReplayError("INVALID_FILTER_SET")
    if any(not VALID_CASE.fullmatch(value) for value in filters):
        raise ReplayError("UNSAFE_OR_INVALID_UNITY_TEST_FILTER")
    return filters


def parse_unity_xml(xml_file: Path, expected: list[str]) -> dict:
    if not xml_file.exists() or not xml_file.is_file():
        raise ReplayError("UNITY_XML_MISSING")
    size = xml_file.stat().st_size
    if size < 25 or size > MAX_BYTES:
        raise ReplayError("UNITY_XML_EMPTY_OR_OVERSIZED")
    raw = xml_file.read_bytes()
    if b"<!DOCTYPE" in raw or b"<!ENTITY" in raw:
        raise ReplayError("UNITY_XML_DTD_NOT_ALLOWED")
    try:
        root = ET.fromstring(raw)
    except ET.ParseError as exc:
        raise ReplayError("UNITY_XML_UNPARSABLE") from exc
    if root.tag != "test-run":
        raise ReplayError("NOT_NUNIT_TEST_RUN")
    try:
        totals = {k: int(root.attrib[k]) for k in ("total", "passed", "failed", "skipped", "inconclusive")}
    except (KeyError, ValueError) as exc:
        raise ReplayError("UNITY_NUNIT_COUNTERS_INVALID") from exc
    if any(x < 0 for x in totals.values()) or totals["total"] != sum(totals[k] for k in
                             ("passed", "failed", "skipped", "inconclusive")):
        raise ReplayError("UNITY_NUNIT_COUNTERS_CONTRADICTORY")
    results = {}
    for node in root.iter("test-case"):
        key = node.get("fullname")
        value = node.get("result")
        if not key or key in results or value not in {"Passed", "Failed", "Skipped", "Inconclusive"}:
            raise ReplayError("DUPLICATE_OR_INVALID_UNITY_TESTCASE")
        results[key] = value
    # A filtered Unity run must truly execute EXACTLY the requested original
    # tests. A CLI success without the named tests is a blocking error.
    if set(results) != set(expected) or len(results) != totals["total"]:
        raise ReplayError("UNITY_FILTER_COVERAGE_MISMATCH")
    counted = {k: sum(1 for v in results.values() if v == k) for k in
               ("Passed", "Failed", "Skipped", "Inconclusive")}
    if any(counted[key] != totals[key.lower()] for key in counted):
        raise ReplayError("UNITY_NUNIT_ROOT_LEAF_COUNT_MISMATCH")
    if totals["failed"] or totals["inconclusive"] or totals["skipped"]:
        if root.get("result") == "Passed":
            raise ReplayError("UNITY_NUNIT_FALSE_GREEN_ROOT")
    else:
        if root.get("result") != "Passed":
            raise ReplayError("UNITY_NUNIT_FALSE_RED_ROOT")
    return {"totals": totals, "cases": results,
            "xml_sha256": hashlib.sha256(raw).hexdigest(),
            "root_result": root.get("result")}


def execute_once(*, project: Path, editor: Path, editor_args: list[str],
                 test_names: list[str], source: str, attempt: int, output: Path,
                 mode: str, timeout: int) -> dict:
    source_guard(project, source)
    run_dir = output / ("attempt-%02d" % attempt)
    run_dir.mkdir(parents=True, exist_ok=False)
    xml_path = run_dir / "results.xml"
    log_path = run_dir / "unity-editor.log"
    args = [str(editor), *editor_args, "-batchmode", "-nographics",
            "-projectPath", str(project), "-runTests", "-testPlatform", mode,
            "-testFilter", ";".join(test_names), "-testResults", str(xml_path),
            "-logFile", str(log_path), "-quit"]
    start = time.monotonic()
    stdout, stderr, exitcode, timed_out = b"", b"", None, False
    try:
        result = subprocess.run(args, capture_output=True, check=False,
                                timeout=timeout)
        stdout, stderr, exitcode = result.stdout, result.stderr, result.returncode
    except subprocess.TimeoutExpired as exc:
        stdout, stderr = exc.stdout or b"", exc.stderr or b""
        timed_out = True
    except OSError as exc:
        stderr = str(exc).encode("utf-8", errors="replace")
    (run_dir / "stdout.txt").write_bytes(stdout[:MAX_BYTES])
    (run_dir / "stderr.txt").write_bytes(stderr[:MAX_BYTES])
    item = {"attempt": attempt, "exit_code": exitcode, "timeout": timed_out,
            "duration_seconds": round(time.monotonic() - start, 3),
            "native_editor_invoked": exitcode is not None or timed_out,
            "filter": test_names, "source_sha": source, "mode": mode,
            "xml_path": str(xml_path), "log_path": str(log_path)}
    if timed_out:
        item["status"] = "HOLD_TIMEOUT"
    elif exitcode is None:
        item["status"] = "HOLD_EDITOR_LAUNCH_FAILED"
    else:
        try:
            parsed = parse_unity_xml(xml_path, test_names)
            item.update(parsed)
            item["status"] = ("NATIVE_TRIAL_PASS" if exitcode == 0 and
                              all(v == "Passed" for v in parsed["cases"].values())
                              else "NATIVE_TRIAL_FAIL")
            if exitcode != 0 and item["status"] == "NATIVE_TRIAL_FAIL":
                item["native_process_exit_failed"] = True
            if exitcode == 0 and item["status"] == "NATIVE_TRIAL_FAIL":
                item["status"] = "HOLD_XML_NONPASS_WITH_ZERO_EXIT"
        except ReplayError as exc:
            item["status"] = "HOLD_INVALID_OR_MISSING_XML"
            item["evidence_error"] = str(exc)
    try:
        source_guard(project, source)
    except ReplayError as exc:
        item["status"] = "HOLD_SOURCE_CHANGED_DURING_RUN"
        item["source_error"] = str(exc)
    return item


def replay(*, project: Path, editor: Path, output: Path, source: str,
           test_names: list[str], repeats: int, timeout: int,
           editor_args: list[str] | None = None, mode: str = "EditMode") -> dict:
    if mode not in ("EditMode", "PlayMode") or not 1 <= repeats <= 20 or not 1 <= timeout <= 3600:
        raise ReplayError("INVALID_REPLAY_SETTINGS")
    validate_names(test_names)
    if not editor.is_file():
        raise ReplayError("UNITY_EDITOR_EXECUTABLE_NOT_FOUND")
    if not project.is_dir() or not (project / ".git").exists():
        raise ReplayError("PROJECT_GIT_CHECKOUT_REQUIRED")
    source_guard(project, source)
    if output.exists():
        raise ReplayError("OUTPUT_DIRECTORY_MUST_NOT_EXIST")
    output.mkdir(parents=True)
    data = {"package": "CI41-REPLAY-001", "source_sha": source, "editor": str(editor),
            "mode": mode, "filters": test_names, "requested_trials": repeats,
            "results": [], "native_unity": "EXECUTED_ONLY_IF_RUNNER_ACTUALLY_INVOKED",
            "full_suite_g1": "NOT_ASSESSED_BY_FILTERED_REPLAY",
            "status": "HOLD_INCOMPLETE"}
    try:
        for index in range(1, repeats + 1):
            res = execute_once(project=project, editor=editor,
                               editor_args=editor_args or [], test_names=test_names,
                               source=source, attempt=index, output=output,
                               mode=mode, timeout=timeout)
            data["results"].append(res)
            if res["status"] in ("HOLD_SOURCE_CHANGED_DURING_RUN", "HOLD_TIMEOUT",
                                 "HOLD_EDITOR_LAUNCH_FAILED"):
                break
        status = {entry["status"] for entry in data["results"]}
        data["status"] = ("REPEAT_PASS" if status == {"NATIVE_TRIAL_PASS"} and
                          len(data["results"]) == repeats else "HOLD_REPRO_OR_INVALID_EVIDENCE")
        data["trials_passed"] = sum(x["status"] == "NATIVE_TRIAL_PASS" for x in data["results"])
        data["trials_failed_or_invalid"] = len(data["results"]) - data["trials_passed"]
    finally:
        payload = json.dumps(data, ensure_ascii=False, sort_keys=True, indent=2) + "\n"
        temp = output / "summary.json.tmp"
        temp.write_text(payload, encoding="utf-8")
        temp.replace(output / "summary.json")
    return data


def main() -> int:
    p = argparse.ArgumentParser(description="Invoke and audit repeated Unity Editor filtered test runs")
    p.add_argument("--project", required=True)
    p.add_argument("--editor-exe", required=True)
    p.add_argument("--editor-launch-arg", action="append", default=[])
    p.add_argument("--source-sha", required=True)
    p.add_argument("--test", action="append", required=True)
    p.add_argument("--repeats", type=int, default=3)
    p.add_argument("--timeout", type=int, default=600)
    p.add_argument("--mode", choices=["EditMode", "PlayMode"], default="EditMode")
    p.add_argument("--output", required=True)
    args = p.parse_args()
    try:
        result = replay(project=Path(args.project).resolve(),
                        editor=Path(args.editor_exe).resolve(),
                        editor_args=args.editor_launch_arg, test_names=args.test,
                        source=args.source_sha, repeats=args.repeats,
                        timeout=args.timeout, mode=args.mode,
                        output=Path(args.output).resolve())
        print(json.dumps({"status": result["status"],
                          "summary": str((Path(args.output).resolve() / "summary.json")),
                          "trials": len(result["results"]), "full_suite_g1": "NOT_ASSESSED"},
                         sort_keys=True))
        return 0 if result["status"] == "REPEAT_PASS" else 2
    except (ReplayError, OSError, ValueError) as exc:
        print(json.dumps({"status": "FAIL_CLOSED", "error": str(exc),
                          "native_unity": "NOT_CONFIRMED",
                          "package": "CI41-REPLAY-001"}, sort_keys=True))
        return 2


if __name__ == "__main__":
    sys.exit(main())
