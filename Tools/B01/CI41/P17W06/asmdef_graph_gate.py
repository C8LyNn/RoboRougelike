#!/usr/bin/env python3
"""CI41-ASMGRAPH-001: original Git object-backed Unity asmdef compiler-dependency gate.

Read only. Evaluates exact committed Assets/*.asmdef and .meta, not the working
Unity Library, without invoking Unity or touching Codex-owned game source.
"""
from __future__ import annotations

import argparse
from collections import defaultdict
import json
import re
from pathlib import Path
import subprocess
import sys

IDENT = re.compile(r"^[A-Za-z_][A-Za-z0-9_.-]*$")
GUID = re.compile(r"^[0-9a-fA-F]{32}$")
PIN = re.compile(r"^[0-9a-f]{40}$")
META_GUID = re.compile(r"^guid:\s*([^\s#]+)", re.M)
SCOPED_PREFIX = "RoboRougelike."
MAX_BYTES = 512_000


class GraphGateError(Exception):
    pass


def git(repo: Path, *args: str) -> bytes:
    try:
        p = subprocess.run(["git", "-C", str(repo), *args],
                           stdout=subprocess.PIPE, stderr=subprocess.PIPE,
                           check=False)
    except OSError as err:
        raise GraphGateError("GIT_NOT_AVAILABLE") from err
    if p.returncode != 0:
        raise GraphGateError("GIT_FAILED:" + " ".join(args[:2]))
    return p.stdout


def committed_files(repo: Path, sha: str) -> dict[str, str]:
    if not PIN.fullmatch(sha):
        raise GraphGateError("PINNED_40_HEX_COMMIT_SHA_REQUIRED")
    if git(repo, "rev-parse", "--verify", sha + "^{commit}").decode().strip() != sha:
        raise GraphGateError("COMMIT_SHA_MISMATCH")
    entries = git(repo, "ls-tree", "-r", "-z", sha, "--", "Assets").split(b"\0")
    result = {}
    for entry in entries:
        if not entry:
            continue
        try:
            lhs, pathbytes = entry.split(b"\t", 1)
            mode, kind, blob = lhs.decode("ascii").split()
            path = pathbytes.decode("utf-8")
        except (ValueError, UnicodeDecodeError) as err:
            raise GraphGateError("INVALID_GIT_TREE_ENTRY") from err
        if kind != "blob":
            continue
        if not path.startswith("Assets/"):
            continue
        if path.endswith(".asmdef") or path.endswith(".asmdef.meta"):
            result[path] = blob
    return result


def read_blob(repo: Path, blob: str) -> str:
    try:
        raw = git(repo, "cat-file", "blob", blob)
    except GraphGateError as err:
        raise GraphGateError("UNREADABLE_ASMDEF_OR_META_BLOB") from err
    if len(raw) > MAX_BYTES:
        raise GraphGateError("ASMDEF_OR_META_OVERSIZED")
    try:
        return raw.decode("utf-8-sig")
    except UnicodeDecodeError as err:
        raise GraphGateError("ASMDEF_OR_META_NON_UTF8") from err


def validate(repo: Path, sha: str, *, require_count: int | None = None) -> dict:
    entries = committed_files(repo, sha)
    definitions = sorted(p for p in entries if p.endswith(".asmdef"))
    errors: list[dict] = []
    warnings: list[dict] = []

    def issue(code: str, path: str, detail: str = "", *, warning: bool = False) -> None:
        (warnings if warning else errors).append(
            {"code": code, "path": path, "detail": detail})

    if not definitions:
        issue("NO_ASMDEFS_FOUND", "Assets")
    if require_count is not None and len(definitions) != require_count:
        issue("ASMDEF_COUNT_DRIFT", "Assets", f"expected={require_count}, actual={len(definitions)}")

    parsed: dict[str, dict] = {}
    names: dict[str, str] = {}
    fold_names: dict[str, str] = {}
    guid_paths: dict[str, str] = {}
    for path in definitions:
        try:
            obj = json.loads(read_blob(repo, entries[path]))
        except (GraphGateError, json.JSONDecodeError) as err:
            issue("INVALID_ASMDEF_JSON", path, str(err))
            continue
        if not isinstance(obj, dict):
            issue("ASMDEF_NOT_OBJECT", path)
            continue
        name = obj.get("name")
        if not isinstance(name, str) or not IDENT.fullmatch(name):
            issue("INVALID_ASSEMBLY_NAME", path, repr(name))
            continue
        if name in names:
            issue("DUPLICATE_ASSEMBLY_NAME", path, names[name])
        if name.casefold() in fold_names and fold_names[name.casefold()] != name:
            issue("CASE_INSENSITIVE_ASSEMBLY_COLLISION", path,
                  f"{fold_names[name.casefold()]} vs {name}")
        names.setdefault(name, path)
        fold_names.setdefault(name.casefold(), name)
        refs = obj.get("references", [])
        if not isinstance(refs, list) or any(not isinstance(v, str) for v in refs):
            issue("INVALID_ASSEMBLY_REFERENCES", path)
            refs = []
        if len(refs) != len(set(refs)):
            issue("DUPLICATE_ASSEMBLY_REFERENCE", path)
        for prop in ("includePlatforms", "excludePlatforms", "defineConstraints",
                     "precompiledReferences"):
            value = obj.get(prop, [])
            if not isinstance(value, list) or any(not isinstance(v, str) for v in value):
                issue("INVALID_ASMDEF_ARRAY", path, prop)
        inc = obj.get("includePlatforms", [])
        exc = obj.get("excludePlatforms", [])
        if isinstance(inc, list) and isinstance(exc, list) and inc and exc:
            issue("INCLUDE_AND_EXCLUDE_PLATFORMS", path)
        parsed[name] = {"path": path, "refs": refs, "obj": obj}
        meta = path + ".meta"
        if meta not in entries:
            issue("MISSING_ASMDEF_META", path)
            continue
        match = META_GUID.search(read_blob(repo, entries[meta]))
        if match is None or not GUID.fullmatch(match.group(1)):
            issue("INVALID_ASMDEF_GUID", meta)
            continue
        guid = match.group(1).lower()
        if guid in guid_paths:
            issue("DUPLICATE_ASMDEF_GUID", meta, f"existing={guid_paths[guid]}")
        guid_paths.setdefault(guid, name)

    # GUID-referenced assemblies use the meta GUID, whereas plain-name refs
    # refer to local assembly names. External packages are allowed unverified.
    edges: dict[str, list[str]] = defaultdict(list)
    for name, info in sorted(parsed.items()):
        path, obj = info["path"], info["obj"]
        test_edit = name.endswith(".EditModeTests")
        test_play = name.endswith(".PlayModeTests")
        inc = obj.get("includePlatforms", [])
        if not isinstance(inc, list):
            inc = []
        if test_edit:
            if inc != ["Editor"]:
                issue("EDITMODE_ASSEMBLY_MUST_INCLUDE_EDITOR_ONLY", path, repr(inc))
        if test_play and "Editor" in inc:
            issue("PLAYMODE_ASSEMBLY_EDITOR_ONLY", path)
        if test_edit or test_play:
            constraints = obj.get("defineConstraints", [])
            if not isinstance(constraints, list) or "UNITY_INCLUDE_TESTS" not in constraints:
                issue("TEST_ASSEMBLY_MISSING_UNITY_INCLUDE_TESTS", path)
            refs = obj.get("precompiledReferences", [])
            if not isinstance(refs, list) or "nunit.framework.dll" not in refs:
                issue("TEST_ASSEMBLY_NUNIT_REFERENCE_MISSING", path)
            if obj.get("overrideReferences") is not True:
                issue("TEST_ASSEMBLY_OVERRIDE_REFERENCES_REQUIRED", path)
        for ref in info["refs"]:
            target = None
            if ref.startswith("GUID:"):
                candidate = ref[5:].lower()
                if not GUID.fullmatch(candidate):
                    issue("INVALID_ASSEMBLY_REFERENCE_GUID", path, ref)
                    continue
                target = guid_paths.get(candidate)
                if target is None:
                    issue("MISSING_ASSEMBLY_REFERENCE_GUID", path, ref)
                    continue
            elif ref in names:
                target = ref
            elif ref.startswith(SCOPED_PREFIX):
                issue("MISSING_LOCAL_ASSEMBLY_REFERENCE", path, ref)
                continue
            else:
                # Unity packages can add external asmdefs that are not in the
                # committed Assets tree. Do not assert unknown deps are broken.
                issue("UNRESOLVED_EXTERNAL_ASSEMBLY_REFERENCE", path, ref, warning=True)
            if target is None:
                continue
            if target == name:
                issue("SELF_REFERENCE_ASSEMBLY", path, ref)
            if target in parsed:
                edges[name].append(target)
                if not (test_edit or test_play):
                    tinc = parsed[target]["obj"].get("includePlatforms", [])
                    if tinc == ["Editor"]:
                        issue("RUNTIME_REFERENCES_EDITOR_ONLY_ASSEMBLY", path, target)

    # Full DFS to detect cycles even when multiple errors are already present.
    state: dict[str, int] = {}
    stack: list[str] = []
    cycles: set[tuple[str, ...]] = set()

    def visit(node: str) -> None:
        state[node] = 1
        stack.append(node)
        for target in sorted(set(edges[node])):
            if state.get(target) == 1:
                index = stack.index(target)
                cycle = tuple(stack[index:] + [target])
                if cycle not in cycles:
                    cycles.add(cycle)
                    issue("ASSEMBLY_REFERENCE_CYCLE", parsed[node]["path"], " -> ".join(cycle))
            elif state.get(target, 0) == 0:
                visit(target)
        stack.pop()
        state[node] = 2

    for name in sorted(parsed):
        if state.get(name, 0) == 0:
            visit(name)

    return {
        "package": "CI41-ASMGRAPH-001", "version": "1.0.0",
        "source_sha": sha, "tracked_asmdef_count": len(definitions),
        "valid_parsed_asmdef_count": len(parsed),
        "guid_count": len(guid_paths),
        "dependency_edge_count": sum(len(set(x)) for x in edges.values()),
        "mode": "GIT_OBJECT_EXACT_COMMIT_SOURCE_ONLY",
        "native_unity": "NOT_RUN", "g1": "NOT_ASSESSED",
        "result": "PASS_SOURCE_GRAPH" if not errors else "FAIL_SOURCE_GRAPH",
        "errors": sorted(errors, key=lambda x: (x["code"], x["path"], x["detail"])),
        "warnings": sorted(warnings, key=lambda x: (x["code"], x["path"], x["detail"])),
    }


def main() -> int:
    parser = argparse.ArgumentParser(description="Validate Unity asmdef references from immutable original Git objects")
    parser.add_argument("--repo", default=".")
    parser.add_argument("--source-sha", required=True)
    parser.add_argument("--require-count", type=int)
    parser.add_argument("--out")
    a = parser.parse_args()
    try:
        report = validate(Path(a.repo).resolve(), a.source_sha, require_count=a.require_count)
    except GraphGateError as err:
        report = {"package": "CI41-ASMGRAPH-001", "result": "FAIL_TOOL",
                  "error": str(err), "native_unity": "NOT_RUN"}
    rendered = json.dumps(report, sort_keys=True, indent=2, ensure_ascii=False) + "\n"
    if a.out:
        out = Path(a.out).resolve()
        out.parent.mkdir(parents=True, exist_ok=True)
        tmp = out.with_name(out.name + ".tmp")
        tmp.write_text(rendered, encoding="utf-8")
        tmp.replace(out)
    print(rendered)
    return 0 if report["result"] == "PASS_SOURCE_GRAPH" else 2


if __name__ == "__main__":
    sys.exit(main())
