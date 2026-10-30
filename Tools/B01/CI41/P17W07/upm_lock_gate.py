#!/usr/bin/env python3
"""CI41-UPMLOCK-001: exact-Git-object Unity Package Manager dependency closure gate.

Source-only. Does not invoke the Unity package resolver or make network calls.
"""
from __future__ import annotations

import argparse
from collections import deque
import json
from pathlib import Path
import re
import subprocess
import sys
from urllib.parse import urlsplit

SHA = re.compile(r"^[0-9a-f]{40}$")
PACKAGE = re.compile(r"^[a-z0-9][a-z0-9._-]*$")
VERSION = re.compile(r"^[0-9]+(?:\.[0-9]+){1,3}(?:[-+][a-zA-Z0-9.+-]+)?$")
MAX_INPUT = 2_000_000
PATHS = ("Packages/manifest.json", "Packages/packages-lock.json",
         "ProjectSettings/ProjectVersion.txt")
EDITOR = re.compile(r"^m_EditorVersion:\s*(\S+)\s*$", re.M)


class LockGateError(Exception):
    pass


def run_git(repo: Path, *args: str) -> bytes:
    try:
        proc = subprocess.run(["git", "-C", str(repo), *args],
                              capture_output=True, check=False)
    except OSError as err:
        raise LockGateError("GIT_NOT_FOUND") from err
    if proc.returncode:
        raise LockGateError("GIT_COMMAND_FAILED:" + " ".join(args[:2]))
    return proc.stdout


def load_original(repo: Path, sha: str) -> dict[str, str]:
    if not SHA.fullmatch(sha):
        raise LockGateError("EXACT_LOWERCASE_40_HEX_COMMIT_REQUIRED")
    actual = run_git(repo, "rev-parse", "--verify", sha + "^{commit}").decode().strip()
    if actual != sha:
        raise LockGateError("SOURCE_COMMIT_MISMATCH")
    tree = run_git(repo, "ls-tree", "-r", "-z", sha, "--", *PATHS)
    found: dict[str, str] = {}
    for entry in tree.split(b"\0"):
        if not entry:
            continue
        try:
            head, name = entry.split(b"\t", 1)
            mode, kind, blob = head.decode("ascii").split()
            name = name.decode("utf-8")
        except (ValueError, UnicodeDecodeError) as err:
            raise LockGateError("INVALID_GIT_TREE_RECORD") from err
        if name not in PATHS:
            continue
        if kind != "blob" or mode != "100644":
            raise LockGateError("NONSTANDARD_UPM_FILE_GIT_TYPE")
        if name in found:
            raise LockGateError("DUPLICATE_UPM_TREE_ENTRY")
        data = run_git(repo, "cat-file", "blob", blob)
        if len(data) > MAX_INPUT:
            raise LockGateError("UPM_FILE_TOO_LARGE")
        try:
            found[name] = data.decode("utf-8-sig")
        except UnicodeDecodeError as err:
            raise LockGateError("UPM_FILE_NOT_UTF8") from err
    missing = set(PATHS) - set(found)
    if missing:
        raise LockGateError("MISSING_COMMITTED_UPM_FILES:" + ",".join(sorted(missing)))
    return found


def no_duplicate_object_keys(pairs: list[tuple]) -> dict:
    output = {}
    for name, value in pairs:
        if name in output:
            raise LockGateError("DUPLICATE_JSON_KEY:" + name)
        output[name] = value
    return output


def decode(raw: str, path: str) -> dict:
    try:
        value = json.loads(raw, object_pairs_hook=no_duplicate_object_keys,
                           parse_constant=lambda c: (_ for _ in ()).throw(
                               LockGateError("INVALID_JSON_CONSTANT:" + c)))
    except (ValueError, UnicodeDecodeError) as err:
        raise LockGateError("INVALID_JSON:" + path) from err
    if not isinstance(value, dict):
        raise LockGateError("UPM_JSON_ROOT_NOT_OBJECT:" + path)
    return value


def validate(manifest: dict, locked: dict, unity_version: str,
             source_sha: str, *, expected_direct: int | None = None,
             expected_resolved: int | None = None) -> dict:
    errors: list[dict] = []
    warnings: list[dict] = []

    def error(code: str, package: str = "", detail: str = "") -> None:
        errors.append({"code": code, "package": package, "detail": detail})

    def warn(code: str, package: str = "", detail: str = "") -> None:
        warnings.append({"code": code, "package": package, "detail": detail})

    if not EDITOR.search(unity_version):
        error("MISSING_UNITY_EDITOR_VERSION", "ProjectSettings/ProjectVersion.txt")
    direct = manifest.get("dependencies")
    resolved = locked.get("dependencies")
    if not isinstance(direct, dict) or not isinstance(resolved, dict):
        error("INVALID_UPM_DEPENDENCY_MAP")
        direct = direct if isinstance(direct, dict) else {}
        resolved = resolved if isinstance(resolved, dict) else {}

    if not direct:
        error("EMPTY_MANIFEST_DEPENDENCIES")
    if not resolved:
        error("EMPTY_PACKAGE_LOCK")
    if expected_direct is not None and len(direct) != expected_direct:
        error("MANIFEST_PACKAGE_COUNT_DRIFT", detail=f"expected={expected_direct},actual={len(direct)}")
    if expected_resolved is not None and len(resolved) != expected_resolved:
        error("LOCK_PACKAGE_COUNT_DRIFT", detail=f"expected={expected_resolved},actual={len(resolved)}")

    graph: dict[str, list[str]] = {}
    direct_set = set(direct)
    valid: dict[str, dict] = {}
    for name, obj in sorted(resolved.items()):
        if not PACKAGE.fullmatch(name):
            error("INVALID_LOCK_PACKAGE_NAME", name)
        if not isinstance(obj, dict):
            error("INVALID_LOCK_ENTRY", name)
            continue
        version = obj.get("version")
        depth = obj.get("depth")
        origin = obj.get("source")
        dependencies = obj.get("dependencies")
        if not isinstance(version, str) or not version:
            error("INVALID_LOCK_VERSION", name)
        if type(depth) is not int or depth < 0:
            error("INVALID_LOCK_DEPTH", name, repr(depth))
        if origin not in ("registry", "builtin"):
            error("UNSUPPORTED_LOCK_ORIGIN_REQUIRES_REVIEW", name, repr(origin))
        if origin == "registry":
            url = obj.get("url")
            parts = urlsplit(url) if isinstance(url, str) else None
            if not parts or parts.scheme != "https" or not parts.hostname or parts.username or parts.password:
                error("INSECURE_OR_MISSING_REGISTRY_URL", name)
        if not isinstance(dependencies, dict):
            error("INVALID_TRANSITIVE_DEPENDENCY_MAP", name)
            dependencies = {}
        deps = []
        for child, request in sorted(dependencies.items()):
            if not PACKAGE.fullmatch(child) or not isinstance(request, str) or not request:
                error("INVALID_TRANSITIVE_REQUIREMENT", name, repr(child))
                continue
            if child not in resolved:
                error("MISSING_TRANSITIVE_LOCK_ENTRY", name, child)
            deps.append(child)
        graph[name] = deps
        valid[name] = obj

    for name, requested in sorted(direct.items()):
        if not PACKAGE.fullmatch(name) or not isinstance(requested, str) or not requested:
            error("INVALID_DIRECT_REQUIREMENT", name)
            continue
        obj = valid.get(name)
        if obj is None:
            error("MISSING_DIRECT_LOCK_ENTRY", name)
            continue
        if obj.get("version") != requested:
            error("DIRECT_VERSION_LOCK_MISMATCH", name,
                  f"manifest={requested!r}, lock={obj.get('version')!r}")
        if obj.get("depth") != 0:
            error("DIRECT_DEPENDENCY_DEPTH_NOT_ZERO", name, str(obj.get("depth")))
    # Breadth-first traversal validates shortest locked Unity depth for every
    # reachable dependency and that there are no orphan/unexplained lock entries.
    # Unity's transitive minimum requests may differ from its resolved version
    # (e.g., NUnit 1.x requested and 2.x resolved). This is NOT an error.
    distances: dict[str, int] = {}
    q = deque((n, 0) for n in sorted(direct) if n in valid)
    while q:
        name, depth = q.popleft()
        if name in distances and distances[name] <= depth:
            continue
        distances[name] = depth
        for child in graph.get(name, []):
            if child in valid and (child not in distances or distances[child] > depth + 1):
                q.append((child, depth + 1))
    for name, obj in sorted(valid.items()):
        if name not in distances:
            error("ORPHAN_LOCK_ENTRY", name)
        elif type(obj.get("depth")) is int and obj["depth"] != distances[name]:
            error("LOCK_DEPTH_NOT_SHORTEST_REACHABLE", name,
                  f"lock={obj['depth']}, expected={distances[name]}")
        for dep, minimum in sorted(obj.get("dependencies", {}).items()):
            if dep not in valid or not isinstance(minimum, str):
                continue
            target = valid[dep].get("version")
            if target != minimum:
                warn("UPM_TRANSITIVE_REQUEST_DIFFERS_FROM_RESOLVED", name,
                     f"{dep}: requested={minimum}, resolved={target}")
    return {
        "package": "CI41-UPMLOCK-001", "version": "1.0.0", "source_sha": source_sha,
        "unity_editor": EDITOR.search(unity_version).group(1) if EDITOR.search(unity_version) else None,
        "manifest_count": len(direct), "lock_count": len(resolved),
        "transitive_count": len(resolved)-len(direct_set & set(resolved)),
        "dependency_edge_count": sum(len(x) for x in graph.values()),
        "result": "PASS_SOURCE_UPM_LOCK" if not errors else "FAIL_SOURCE_UPM_LOCK",
        "errors": sorted(errors, key=lambda x: (x["code"], x["package"], x["detail"])),
        "warnings": sorted(warnings, key=lambda x: (x["code"], x["package"], x["detail"])),
        "native_unity": "NOT_RUN", "package_resolver": "NOT_RUN",
        "game_G1": "NOT_ASSESSED",
        "scope": "IMMUTABLE_ORIGINAL_GIT_OBJECTS_ONLY",
    }


def main() -> int:
    parser = argparse.ArgumentParser(description="Check original Unity UPM manifest/lock closure at exact Git commit")
    parser.add_argument("--repo", default=".")
    parser.add_argument("--source-sha", required=True)
    parser.add_argument("--expected-direct", type=int)
    parser.add_argument("--expected-resolved", type=int)
    parser.add_argument("--out")
    args = parser.parse_args()
    try:
        content = load_original(Path(args.repo).resolve(), args.source_sha)
        manifest = decode(content[PATHS[0]], PATHS[0])
        locked = decode(content[PATHS[1]], PATHS[1])
        report = validate(manifest, locked, content[PATHS[2]], args.source_sha,
                          expected_direct=args.expected_direct,
                          expected_resolved=args.expected_resolved)
    except (LockGateError, OSError, ValueError) as err:
        report = {"package": "CI41-UPMLOCK-001", "result": "FAIL_SOURCE_UPM_LOCK",
                  "error": str(err), "native_unity": "NOT_RUN",
                  "package_resolver": "NOT_RUN", "game_G1": "NOT_ASSESSED"}
    payload = json.dumps(report, ensure_ascii=False, sort_keys=True, indent=2) + "\n"
    if args.out:
        path = Path(args.out)
        path.parent.mkdir(parents=True, exist_ok=True)
        tmp = path.with_name(path.name+".tmp")
        tmp.write_text(payload, encoding="utf-8")
        tmp.replace(path)
    print(payload)
    return 0 if report["result"] == "PASS_SOURCE_UPM_LOCK" else 2


if __name__ == "__main__":
    sys.exit(main())
