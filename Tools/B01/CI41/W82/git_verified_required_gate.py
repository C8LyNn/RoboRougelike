#!/usr/bin/env python3
"""B01-41 P16: original-Git scope gate for safe required-check decisions.

DO NOT trust a JSON claim of changed paths. Compare two verified 40-hex Git
commits, check ancestry and classify the real byte-level Git diff. Delegates
native status interpretation to W78, but never promotes a claimed receipt to
real G1 or G2. This is a standalone QA tool, not deployed in shared CI02/03.
"""
from __future__ import annotations
import argparse
import json
from pathlib import Path
import re
import subprocess
import sys

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE.parent / "W74"))
sys.path.insert(0, str(HERE.parent / "W78"))
from ci_health import classify
from required_aggregate import evaluate

EXACT_SHA = re.compile(r"[0-9a-f]{40}\Z")


class ScopeGateError(Exception):
    pass


def git(repo: Path, *args: str) -> bytes:
    try:
        proc = subprocess.run(["git", "-C", str(repo), *args],
                              capture_output=True, check=False)
    except OSError as exc:
        raise ScopeGateError("GIT_EXEC_UNAVAILABLE") from exc
    if proc.returncode != 0:
        raise ScopeGateError("GIT_QUERY_FAILURE:" + " ".join(args[:2]))
    return proc.stdout


def original_diff(repo: Path, base: str, head: str) -> list[str]:
    if not (isinstance(base, str) and isinstance(head, str) and
            EXACT_SHA.fullmatch(base) and EXACT_SHA.fullmatch(head)):
        raise ScopeGateError("EXACT_GIT_SHA_REQUIRED")
    # Refs must be actual Git commits in the same repository, not arbitrary
    # user-provided labels, branch heads or unattested JSON source strings.
    for sha in (base, head):
        if git(repo, "rev-parse", "--verify", sha + "^{commit}").decode().strip() != sha:
            raise ScopeGateError("SOURCE_OBJECT_NOT_EXACT_COMMIT")
    try:
        git(repo, "merge-base", "--is-ancestor", base, head)
    except ScopeGateError as exc:
        raise ScopeGateError("BASE_NOT_ANCESTOR_OR_UNVERIFIED") from exc
    raw = git(repo, "diff", "--no-renames", "--name-only", "-z", base, head, "--")
    paths = [p.decode("utf-8", errors="surrogateescape")
             for p in raw.split(b"\0") if p]
    if not paths or len(paths) != len(set(paths)):
        raise ScopeGateError("EMPTY_OR_AMBIGUOUS_GIT_DIFF")
    return paths


def evaluate_verified(repo: Path, base: str, head: str,
                      observed: dict | None = None,
                      claimed_paths: list[str] | None = None) -> dict:
    paths = original_diff(repo, base, head)
    # No external claim can narrow the actual path list. Require exact set.
    if claimed_paths is not None:
        if not isinstance(claimed_paths, list) or not all(
            isinstance(p, str) for p in claimed_paths
        ) or sorted(paths) != sorted(claimed_paths):
            raise ScopeGateError("UNTRUSTED_CHANGED_PATH_CLAIM_MISMATCH")
    decision = evaluate(paths, head, observed)
    return {
        "source": {"base_sha": base, "head_sha": head,
                   "changed_paths": paths, "path_count": len(paths),
                   "provenance": "ACTUAL_GIT_DIFF_EXACT_COMMITS"},
        "decision": decision,
        "native_unity": "NOT_EXECUTED_BY_THIS_TOOL",
        "integration": "NO_MERGE_NO_G2",
    }


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--repo", default=".")
    parser.add_argument("--base", required=True)
    parser.add_argument("--head", required=True)
    parser.add_argument("--observed-json",
                        help="Optional untrusted job observation; not original native proof")
    parser.add_argument("--claim-paths-json",
                        help="Optional untrusted claimed path list; must match original Git")
    args = parser.parse_args()
    try:
        repo = Path(args.repo).resolve(strict=True)
        observed = (json.loads(Path(args.observed_json).read_text("utf-8"))
                    if args.observed_json else None)
        claimed = (json.loads(Path(args.claim_paths_json).read_text("utf-8"))
                   if args.claim_paths_json else None)
        output = evaluate_verified(repo, args.base, args.head, observed, claimed)
        print(json.dumps(output, sort_keys=True, indent=2))
        # A native-required scope is always a blocking result here. No runtime
        # evidence is independently verified, so don't return PASS on claims.
        return 0 if output["decision"]["state"] == "QA_ONLY_NOT_REQUIRED" else 2
    except (ScopeGateError, ValueError, TypeError, KeyError, OSError) as exc:
        print(json.dumps({"status": "SOURCE_SCOPE_FAIL_CLOSED", "native_g1": "NOT_RUN",
                          "error": str(exc)}, sort_keys=True))
        return 2


if __name__ == "__main__":
    sys.exit(main())
