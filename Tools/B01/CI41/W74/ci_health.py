#!/usr/bin/env python3
"""CI-HEALTH-001: fail-closed source-diff classifier, not a Unity test runner.

A QA_ONLY result permits *only a clearly labelled non-native bypass* in an
ALWAYS-EMITTED required check. Never use trigger-level paths/paths-ignore on
a required native check; never map NOT_APPLICABLE to Unity G1 PASS.
This module does not modify shared CI-02/03, activation credentials or rulesets.
"""
from __future__ import annotations
import argparse
import json
import re
import subprocess
from pathlib import Path

MAIN_SHA = "5f06af81e2dd9d877e4300259bb3dd008cb9c9d1"
ORIGINAL_CI_BLOBS = {
    ".github/workflows/ci-02-editmode.yml": "6c6b06819daa6154e3ecd6cccff2e6147b952cfd",
    ".github/workflows/ci-03-playmode.yml": "4a323a569d9a51a274132b3acab934f191128c5b",
}
QA_PREFIXES = ("Tools/B01/", "Docs/B01/", "Docs/B01Validation/")
QA_WORKFLOW = re.compile(r"^\.github/workflows/b01-[a-z0-9-]+\.ya?ml$")


def git(repo: str | Path, *args: str) -> bytes:
    p = subprocess.run(["git", "-C", str(repo), *args], capture_output=True)
    if p.returncode:
        raise RuntimeError("GIT_DIFF_UNAVAILABLE_FAIL_CLOSED:" + p.stderr.decode(errors="replace")[-300:])
    return p.stdout


def safe_qa_path(path: str) -> bool:
    if not isinstance(path, str) or not path or "\x00" in path or "\\" in path:
        return False
    if path.startswith("/") or any(s in ("", ".", "..") for s in path.split("/")):
        return False
    if path.startswith(QA_PREFIXES):
        return True
    return QA_WORKFLOW.fullmatch(path) is not None


def classify(paths: list[str]) -> dict:
    """Unknown/empty diff and ANY non-allowlisted path require native gate."""
    qa = bool(paths) and all(safe_qa_path(x) for x in paths)
    return {
        "classification": "QA_ONLY" if qa else "NATIVE_REQUIRED",
        "must_execute_unity": not qa,
        "check_context": "KEEP_STABLE_ALWAYS_EMITTED",
        "non_native_label": "NOT_APPLICABLE_QA_ONLY_NOT_UNITY_G1" if qa else None,
        "credential_gate_when_unavailable": "NOT_INVOKED_QA_ONLY" if qa else "FAIL_CLOSED",
        "G1": "NOT_RUN",
        "G2": "HOLD",
    }


def original_git_paths(repo: str | Path, base: str, head: str) -> list[str]:
    for v in (base, head):
        if not re.fullmatch("[a-f0-9]{40}", v):
            raise ValueError("EXACT_40_HEX_REF_REQUIRED")
    raw = git(repo, "diff", "--no-renames", "--name-only", "-z", base, head, "--")
    return [x.decode("utf-8", errors="surrogateescape") for x in raw.split(b"\0") if x]


def audit_original_workflows(repo: str | Path) -> dict:
    actual = git(repo, "rev-parse", "refs/remotes/origin/main").decode().strip()
    if actual != MAIN_SHA:
        raise RuntimeError("MAIN_MOVED_REPIN_ORIGINAL_SOURCE_REQUIRED")
    out = {}
    for path, expected in ORIGINAL_CI_BLOBS.items():
        blob = git(repo, "rev-parse", f"{MAIN_SHA}:{path}").decode().strip()
        if blob != expected:
            raise RuntimeError("ORIGINAL_WORKFLOW_BLOB_MISMATCH:" + path)
        raw = git(repo, "show", f"{MAIN_SHA}:{path}").decode("utf-8")
        # Original exact YAML happens to have no event filters; adding an
        # event-level path filter is risky if this check is later required.
        if not re.search(r"(?m)^on:$", raw) or not re.search(r"(?m)^  pull_request:$", raw):
            raise RuntimeError("MISSING_STABLE_PR_TRIGGER:" + path)
        if re.search(r"(?m)^    paths(?:-ignore)?:", raw):
            raise RuntimeError("WORKFLOW_LEVEL_PATH_FILTER_RISK:" + path)
        if "continue-on-error: true" in raw:
            raise RuntimeError("NATIVE_FAILURE_MUST_NOT_BE_IGNORED:" + path)
        if "Require valid Unity activation credentials" not in raw or "Require nonempty, fully passing executed test cases" not in raw:
            raise RuntimeError("ORIGINAL_NATIVE_GATE_MISSING:" + path)
        out[path] = {"blob_sha": blob, "trigger": "UNCONDITIONAL_PR", "native_gate": "PRESENT"}
    return out


def required_context_risk(*, workflow_skipped: bool, required: bool) -> str:
    """Conservative GitHub status-context model, not a live branch rule read."""
    if required and workflow_skipped:
        return "REQUIRED_CONTEXT_MAY_BE_MISSING_OR_PENDING"
    if workflow_skipped:
        return "NO_CHECK_EMITTED"
    return "CHECK_EMITTED_NATIVE_OR_EXPLICIT_NOT_APPLICABLE"


def main() -> int:
    p = argparse.ArgumentParser()
    p.add_argument("--repo", default=".")
    p.add_argument("--base", required=True)
    p.add_argument("--head", required=True)
    p.add_argument("--audit-original-ci", action="store_true")
    a = p.parse_args()
    try:
        paths = original_git_paths(a.repo, a.base, a.head)
        result = {"paths": paths, "decision": classify(paths)}
        if a.audit_original_ci:
            result["original_ci"] = audit_original_workflows(a.repo)
        print(json.dumps(result, sort_keys=True, indent=2))
        return 0
    except (RuntimeError, ValueError, OSError) as exc:
        print(json.dumps({"classification": "NATIVE_REQUIRED", "G1": "NOT_RUN",
                          "status": "FAIL_CLOSED", "error": str(exc)}, sort_keys=True))
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
