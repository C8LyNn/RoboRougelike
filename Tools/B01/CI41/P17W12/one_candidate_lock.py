#!/usr/bin/env python3
"""B01-41: read-only immutable single-candidate intake policy for W12.

Policy PASS is strictly a source selection preflight, never G0/G1/Codex ACK.
Does not stage, import, deploy, merge or inspect Unity physics.
"""
from __future__ import annotations
import argparse
import hashlib
import json
from pathlib import Path
import re
import subprocess
import sys

SELECTED_PR = 510
SELECTED_SHA = "d94400d95d13db7f27e1533df05c9292d5380431"
FROZEN_PARENT = "33a32b8bd9ab40467ab1a17b5684926a37dbd4d3"
BASE_SHA = "5f06af81e2dd9d877e4300259bb3dd008cb9c9d1"
ALTERNATIVES = {
    511: "baf65f2664e38f3b9c38815c3f07285f8107c1b6",
    494: "6efe45961ed0ffdf183e8016e350978b9f3dc51c",
    488: FROZEN_PARENT,
}
V2_ROOT = "Assets/B0123SecureReattachV2"
REQUIRED = (
    V2_ROOT + ".meta",
    V2_ROOT + "/Runtime/AtomicDockLedger.cs",
    V2_ROOT + "/Runtime/AtomicDockLedger.cs.meta",
    V2_ROOT + "/Runtime/SecureDebrisPlaygroundV2.cs",
    V2_ROOT + "/Runtime/SecureDockPolicy.cs",
    V2_ROOT + "/Tests/PlayMode/SecureReattachV2PlayModeTests.cs",
    V2_ROOT + "/Runtime/B0123.SecureReattachV2.Runtime.asmdef",
)
FORBIDDEN_ROOTS = (
    "Assets/B0123DebrisPlayground001",
    "Assets/B0123DockHardening001",
    "Assets/B0123ReattachClearance001",
)
EXPECTED = {"candidate_pr", "candidate_sha", "imports", "variant",
            "intended_operation", "independent_g0", "native_unity_g1", "codex_ack"}
SHA_PATTERN = re.compile(r"[0-9a-f]{40}\Z")
MAX_PLAN_SIZE = 24_576
MAX_ASSET_PATHS = 20_000

class Rejected(Exception):
    pass

def git(repo: Path, *args: str) -> bytes:
    try:
        result = subprocess.run(["git", "-C", str(repo), *args],
                                capture_output=True, check=False)
    except OSError as exc:
        raise Rejected("GIT_UNAVAILABLE") from exc
    if result.returncode:
        raise Rejected("GIT_CHECK_FAILED:" + args[0])
    return result.stdout

def commit(repo: Path, sha: str) -> None:
    if not isinstance(sha, str) or not SHA_PATTERN.fullmatch(sha):
        raise Rejected("EXACT_COMMIT_SHA_REQUIRED")
    actual = git(repo, "rev-parse", "--verify", sha + "^{commit}").decode("ascii").strip()
    if actual != sha:
        raise Rejected("COMMIT_HASH_CHANGED")

def ancestor(repo: Path, older: str, newer: str) -> None:
    git(repo, "merge-base", "--is-ancestor", older, newer)

def assets(repo: Path, sha: str) -> dict[str,str]:
    result = git(repo, "ls-tree", "-r", "-z", sha, "--", "Assets")
    found: dict[str,str] = {}
    for item in result.split(b"\0"):
        if not item:
            continue
        try:
            mode_type, filename = item.split(b"\t", 1)
            mode, kind, blob = mode_type.decode("ascii").split()
            path = filename.decode("utf-8")
        except (UnicodeError, ValueError) as exc:
            raise Rejected("MALFORMED_GIT_TREE_RECORD") from exc
        if len(found) >= MAX_ASSET_PATHS:
            raise Rejected("OVERSIZED_GIT_ASSETS_TREE")
        if mode != "100644" or kind != "blob":
            raise Rejected("NONREGULAR_ASSET_GIT_ENTRY")
        if (not path.startswith("Assets/") or
                "\r" in path or "\n" in path or "\\" in path or
                any(x in ("", ".", "..") for x in path.split("/"))):
            raise Rejected("UNSAFE_ASSET_PATH")
        found[path] = blob
    return found

def under(path: str, root: str) -> bool:
    return path == root + ".meta" or path.startswith(root + "/")

def verify_plan(repo: Path, plan: dict,
                selected_sha: str = SELECTED_SHA,
                frozen_parent: str = FROZEN_PARENT,
                base_sha: str = BASE_SHA) -> dict:
    # Overrides are only for synthetic independent Git-commit fixtures.
    if not isinstance(plan, dict) or set(plan) != EXPECTED:
        raise Rejected("PLAN_SCHEMA_MUST_MATCH_EXACTLY")
    if type(plan["candidate_pr"]) is not int or plan["candidate_pr"] != SELECTED_PR:
        raise Rejected("ONLY_PR_510_AUTHORIZED")
    if plan["variant"] != "secure-v2-r2":
        raise Rejected("UNSELECTED_VARIANT")
    if plan["candidate_sha"] != selected_sha:
        raise Rejected("CANDIDATE_SHA_MISMATCH_OR_ALTERNATE")
    if (type(plan["imports"]) is not list or len(plan["imports"]) != 1
        or type(plan["imports"][0]) is not dict
        or set(plan["imports"][0]) != {"pr","sha"}
        or type(plan["imports"][0]["pr"]) is not int
        or plan["imports"][0] != {"pr":SELECTED_PR,"sha":selected_sha}):
        raise Rejected("MULTIPLE_OR_FOREIGN_PACKAGE_IMPORTS")
    if (plan["intended_operation"] != "source_only_preflight"
        or plan["independent_g0"] != "PENDING"
        or plan["native_unity_g1"] != "NOT_RUN"
        or plan["codex_ack"] is not False):
        raise Rejected("UNPROVEN_GATES_OR_UNAUTHORIZED_INTEGRATION_INTENT")
    for sha in (base_sha, frozen_parent, selected_sha):
        commit(repo, sha)
    ancestor(repo, base_sha, frozen_parent)
    ancestor(repo, frozen_parent, selected_sha)
    selected_assets = assets(repo, selected_sha)
    target_assets = assets(repo, base_sha)
    for path in REQUIRED:
        if path not in selected_assets:
            raise Rejected("ORIGINAL_SELECTED_SOURCE_REQUIRED:"+path)
    for path in selected_assets:
        if any(under(path, root) for root in FORBIDDEN_ROOTS):
            raise Rejected("ALTERNATIVE_OR_LEGACY_PRESENT_IN_SELECTED_SOURCE:"+path)
    for path in target_assets:
        if under(path, V2_ROOT) or any(under(path, root) for root in FORBIDDEN_ROOTS):
            raise Rejected("TARGET_HAS_LEGACY_OR_SELECTION_COLLISION:"+path)
    locked = {p:selected_assets[p] for p in REQUIRED}
    return {
        "package":"CI41-ONE-CANDIDATE-LOCK-001",
        "version":"1.0.0",
        "lock":"SELECTED_SOURCE_PREFLIGHT_PASS",
        "selected_pr":SELECTED_PR,
        "selected_source_sha":selected_sha,
        "frozen_parent":frozen_parent,
        "baseline_sha":base_sha,
        "allowed_import_count":1,
        "forbidden_alternative_prs":sorted(ALTERNATIVES),
        "verified_original_git_blobs":locked,
        "selection_fingerprint_sha256":hashlib.sha256(
            json.dumps(locked,sort_keys=True,separators=(",",":")).encode()
        ).hexdigest(),
        "independent_g0":"PENDING",
        "native_unity_g1":"NOT_RUN",
        "codex_ack":False,
        "integration":"NO",
        "release_gate":"HOLD",
    }

def read_plan(path: Path) -> dict:
    if not path.is_file() or path.stat().st_size > MAX_PLAN_SIZE:
        raise Rejected("MISSING_OR_OVERSIZED_PLAN")
    try:
        return json.loads(path.read_text(encoding="utf-8"))
    except (ValueError,UnicodeError) as exc:
        raise Rejected("INVALID_PLAN_JSON") from exc

def main() -> int:
    parser=argparse.ArgumentParser()
    parser.add_argument("--repo",default=".")
    parser.add_argument("--plan",required=True)
    parser.add_argument("--out")
    args=parser.parse_args()
    try:
        report=verify_plan(Path(args.repo).resolve(),read_plan(Path(args.plan)))
    except (Rejected,OSError) as exc:
        report={"package":"CI41-ONE-CANDIDATE-LOCK-001",
                "lock":"REJECTED_FAIL_CLOSED","reason":str(exc),
                "independent_g0":"PENDING","native_unity_g1":"NOT_RUN",
                "codex_ack":False,"integration":"NO","release_gate":"HOLD"}
    result=json.dumps(report,sort_keys=True,indent=2)+"\n"
    if args.out:
        Path(args.out).write_text(result,encoding="utf-8")
    print(result,end="")
    return 2 if report["lock"]=="REJECTED_FAIL_CLOSED" else 0

if __name__=="__main__":
    sys.exit(main())
