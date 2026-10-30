#!/usr/bin/env python3
"""CI41-TESTIMPACT-001: exact-Git Unity test routing (never a G1 waiver).

Produces a real targeted precheck plan for MOV modules, or requires a complete
native suite on broad/missing/unknown changes. No Unity Editor is invoked.
"""
from __future__ import annotations

import argparse
import json
from pathlib import Path
import re
import subprocess
import sys

SHA = re.compile(r"^[0-9a-f]{40}$")
MOV = re.compile(r"^Assets/RoboRougelikeMechanics/(MOV\d{3}[A-Za-z0-9_-]+)/")
LIMIT = 12_000
MAX_BLOB = 128_000


class PlanError(Exception):
    pass


def git(repo: Path, *args: str, expected=(0,)) -> tuple[int, bytes]:
    try:
        cmd = subprocess.run(["git", "-C", str(repo), *args],
                             capture_output=True, check=False)
    except OSError as exc:
        raise PlanError("GIT_UNAVAILABLE") from exc
    if cmd.returncode not in expected:
        raise PlanError("GIT_COMMAND_FAILED:" + " ".join(args[:2]))
    return cmd.returncode, cmd.stdout


def require_commit(repo: Path, sha: str) -> None:
    if not SHA.fullmatch(sha):
        raise PlanError("EXACT_40_HEX_COMMIT_REQUIRED")
    if git(repo, "rev-parse", "--verify", sha + "^{commit}")[1].decode().strip() != sha:
        raise PlanError("SOURCE_COMMIT_MISMATCH")


def inventory(repo: Path, sha: str) -> dict:
    require_commit(repo, sha)
    raw = git(repo, "ls-tree", "-r", "-z", sha, "--", "Assets")[1]
    if len(raw) > 3_000_000:
        raise PlanError("GIT_TREE_TOO_LARGE")
    entries = {}
    for line in raw.split(b"\0"):
        if not line:
            continue
        try:
            header, rawpath = line.split(b"\t", 1)
            _, kind, blob = header.decode("ascii").split()
            path = rawpath.decode("utf-8")
        except (UnicodeError, ValueError) as exc:
            raise PlanError("INVALID_TREE_ENTRY") from exc
        if kind == "blob":
            entries[path] = blob
    modules: dict[str, dict] = {}
    root_tests = sorted(p for p in entries if p.startswith("Assets/Tests/Editor/") and p.endswith(".cs"))
    for path, blob in sorted(entries.items()):
        match = MOV.match(path)
        if not match:
            continue
        mod = match.group(1)
        rec = modules.setdefault(mod, {"test_paths": {"EditMode": [], "PlayMode": []},
                                        "assembly_names": {}, "has_runtime": False})
        if "/Runtime/" in path and path.endswith(".asmdef"):
            rec["has_runtime"] = True
        for suite in ("EditMode", "PlayMode"):
            if f"/Tests/{suite}/" not in path:
                continue
            if path.endswith(".cs"):
                rec["test_paths"][suite].append(path)
            if path.endswith(".asmdef"):
                raw_obj = git(repo, "cat-file", "blob", blob)[1]
                if len(raw_obj) > MAX_BLOB:
                    raise PlanError("ASSEMBLY_DEFINITION_TOO_LARGE")
                try:
                    data = json.loads(raw_obj)
                except (ValueError, UnicodeError) as exc:
                    raise PlanError("INVALID_COMMITTED_ASMDEF:" + path) from exc
                name = data.get("name") if isinstance(data, dict) else None
                if not isinstance(name, str) or not name.endswith("." + suite + "Tests"):
                    raise PlanError("INVALID_TEST_ASSEMBLY_NAME:" + path)
                if suite in rec["assembly_names"]:
                    raise PlanError("AMBIGUOUS_TEST_ASSEMBLY:" + mod + ":" + suite)
                rec["assembly_names"][suite] = name
    return {"source_sha": sha, "modules": modules, "root_editor_test_paths": root_tests,
            "module_count": len(modules),
            "module_test_file_count": sum(len(v["test_paths"][k]) for v in modules.values()
                                          for k in ("EditMode", "PlayMode")),
            "central_editor_test_file_count": len(root_tests)}


def changed_paths(repo: Path, base: str, head: str) -> list[dict]:
    require_commit(repo, base)
    require_commit(repo, head)
    git(repo, "merge-base", "--is-ancestor", base, head)
    raw = git(repo, "diff", "--name-status", "-z", "-M", base, head)[1].split(b"\0")
    if len(raw) > LIMIT * 3:
        raise PlanError("CHANGE_SET_TOO_LARGE")
    out = []
    i = 0
    while i < len(raw):
        if not raw[i]:
            i += 1
            continue
        try:
            status = raw[i].decode("ascii")
        except UnicodeDecodeError as exc:
            raise PlanError("INVALID_GIT_CHANGE_STATUS") from exc
        i += 1
        if not status or status[0] not in "ACDMRTUXB":
            raise PlanError("UNKNOWN_GIT_CHANGE_STATUS:" + status)
        count = 2 if status[0] in ("R", "C") else 1
        if i + count > len(raw):
            raise PlanError("TRUNCATED_GIT_CHANGE_RECORD")
        try:
            paths = [raw[i+j].decode("utf-8") for j in range(count)]
        except UnicodeDecodeError as exc:
            raise PlanError("NON_UTF8_GIT_PATH") from exc
        if any(not p or "\n" in p or "\r" in p for p in paths):
            raise PlanError("UNSAFE_OR_EMPTY_GIT_PATH")
        i += count
        out.append({"status": status, "paths": paths})
        if len(out) > LIMIT:
            raise PlanError("CHANGE_SET_TOO_LARGE")
    return out


def decide(data: dict, changes: list[dict], base: str, head: str) -> dict:
    mods = data["modules"]
    touched = set()
    reasons: list[str] = []
    changed = []
    full = False
    for event in changes:
        code = event["status"][0]
        for path in event["paths"]:
            changed.append(path)
            if path.startswith(("Packages/", "ProjectSettings/", "Assets/Tests/")):
                full = True
                reasons.append("GLOBAL_OR_CENTRAL_TEST_CHANGE:" + path)
            elif path.startswith("Assets/"):
                m = MOV.match(path)
                if m:
                    touched.add(m.group(1))
                    if path.endswith((".asmdef", ".asmdef.meta")) or "/Tests/" in path and code in ("D", "R", "C"):
                        full = True
                        reasons.append("TEST_ASSEMBLY_OR_DELETION_CHANGE:" + path)
                else:
                    full = True
                    reasons.append("UNSCOPED_GAME_ASSET_CHANGE:" + path)
            elif not path.startswith(("Tools/", ".github/", "Docs/")):
                full = True
                reasons.append("UNCLASSIFIED_PROJECT_CHANGE:" + path)
        if code in ("U", "X", "B", "T"):
            full = True
            reasons.append("UNSAFE_GIT_CHANGE_STATUS:" + event["status"])
    if len(touched) > 1:
        full = True
        reasons.append("CROSS_MODULE_CHANGES_REQUIRE_FULL_SUITE")
    selected = []
    for module in sorted(touched):
        rec = mods.get(module)
        if not rec:
            full = True
            reasons.append("MISSING_TARGET_MODULE_TEST_INVENTORY:" + module)
            continue
        if not rec["has_runtime"]:
            full = True
            reasons.append("MODULE_RUNTIME_ASMDEF_MISSING:" + module)
        for suite in ("EditMode", "PlayMode"):
            if suite not in rec["assembly_names"] or not rec["test_paths"][suite]:
                full = True
                reasons.append("MISSING_MODULE_TEST_COVERAGE:" + module + ":" + suite)
                continue
            selected.append({"module": module, "mode": suite,
                             "assembly_name": rec["assembly_names"][suite],
                             "test_file_count": len(rec["test_paths"][suite]),
                             "test_paths": rec["test_paths"][suite]})
    if full:
        mode = "FULL_UNITY_SUITE_REQUIRED"
    elif selected:
        mode = "TARGETED_PRECHECK_PLUS_FULL_G1_LATER"
    elif changed:
        mode = "SOURCE_ONLY_NO_GAME_ASSETS_CHANGED"
        reasons.append("CHANGES_OUTSIDE_TRACKED_GAME_ASSETS_REQUIRE_OWN_SOURCE_CI")
    else:
        mode = "NO_CHANGE"
    return {
        "package": "CI41-TESTIMPACT-001", "version": "1.0.0",
        "base_sha": base, "head_sha": head,
        "source_inventory_sha": data["source_sha"],
        "mode": mode, "full_suite_required_now": full,
        "selected_precheck_assemblies": selected,
        "modules_affected": sorted(touched),
        "changes": changes, "changed_path_count": len(set(changed)),
        "inventory": {k: data[k] for k in ("module_count", "module_test_file_count",
                                           "central_editor_test_file_count")},
        "reasons": sorted(set(reasons)),
        "native_unity": "NOT_RUN", "game_g1": "NOT_ASSESSED",
        "release_gate": "INDEPENDENT_FULL_NATIVE_SUITE_ALWAYS_REQUIRED_FOR_G1",
        "status": "SOURCE_PLAN_GENERATED",
    }


def main() -> int:
    p = argparse.ArgumentParser(description="Read original Git diff and produce safe Unity test impact plan")
    p.add_argument("--repo", default=".")
    p.add_argument("--head-sha", required=True)
    p.add_argument("--base-sha")
    p.add_argument("--inventory-only", action="store_true")
    p.add_argument("--expect-module-count", type=int)
    p.add_argument("--out")
    args = p.parse_args()
    try:
        if args.inventory_only and args.base_sha:
            raise PlanError("INVENTORY_MODE_FORBIDS_BASE")
        if not args.inventory_only and not args.base_sha:
            raise PlanError("CHANGE_PLAN_REQUIRES_BASE")
        repo = Path(args.repo).resolve()
        data = inventory(repo, args.head_sha)
        if args.expect_module_count is not None and data["module_count"] != args.expect_module_count:
            raise PlanError("MODULE_INVENTORY_COUNT_DRIFT")
        result = ({"package": "CI41-TESTIMPACT-001", "mode": "REAL_GAME_TEST_INVENTORY",
                   **data, "native_unity": "NOT_RUN", "game_g1": "NOT_ASSESSED"}
                  if args.inventory_only else
                  decide(data, changed_paths(repo, args.base_sha, args.head_sha),
                         args.base_sha, args.head_sha))
    except (PlanError, OSError, ValueError) as exc:
        result = {"package": "CI41-TESTIMPACT-001", "mode": "FAIL_CLOSED",
                  "error": str(exc), "native_unity": "NOT_RUN", "game_g1": "NOT_ASSESSED"}
    rendered = json.dumps(result, indent=2, sort_keys=True, ensure_ascii=False) + "\n"
    if args.out:
        output = Path(args.out)
        output.parent.mkdir(parents=True, exist_ok=True)
        temp = output.with_suffix(output.suffix + ".tmp")
        temp.write_text(rendered, encoding="utf-8")
        temp.replace(output)
    print(rendered)
    return 2 if result["mode"] == "FAIL_CLOSED" else 0


if __name__ == "__main__":
    sys.exit(main())
