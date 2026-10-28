#!/usr/bin/env python3
"""CI41-ASSETIMPACT-001: reproducible Unity asset -> prefab -> scene impact graph.

Read-only, Git-object based, exact-commit delta. This is NOT an orphan GUID
validator (CI41-UNITYREF-001 does that) and never grants native Unity PASS.
It answers: WHICH serialized consumers should Codex re-test after asset edits?
"""
from __future__ import annotations
import argparse
from collections import defaultdict, deque
import json
from pathlib import Path
import re
import subprocess
import sys

EXACT_SHA = re.compile(r"[0-9a-f]{40}\Z")
META_GUID = re.compile(rb"(?m)^guid: ([0-9a-f]{32})\s*$")
REF_GUID = re.compile(rb"(?<![A-Za-z0-9_])guid:\s*([0-9a-f]{32})(?![0-9a-f])")
SERIALIZED = frozenset({".unity", ".prefab", ".asset", ".mat", ".controller",
                        ".anim", ".overridecontroller", ".playable", ".lighting",
                        ".rendertexture", ".mixer", ".physicmaterial",
                        ".physicsmaterial2d", ".terrainlayer"})
MAX_FILES = 15000
MAX_BYTES = 256 * 1024 * 1024
MAX_SINGLE = 24 * 1024 * 1024


class ImpactError(Exception):
    pass


def git(repo: Path, *args: str, data: bytes | None = None) -> bytes:
    try:
        p = subprocess.run(["git", "-C", str(repo), *args],
                           input=data, capture_output=True, check=False)
    except OSError as exc:
        raise ImpactError("GIT_UNAVAILABLE") from exc
    if p.returncode:
        raise ImpactError("GIT_FAILED:" + " ".join(args[:3]))
    return p.stdout


def tree_blobs(repo: Path, sha: str) -> dict[str, str]:
    raw = git(repo, "ls-tree", "-r", "-z", sha, "--", "Assets")
    result = {}
    try:
        for entry in raw.split(b"\x00"):
            if not entry:
                continue
            info, filename = entry.split(b"\t", 1)
            mode, kind, blob = info.split(b" ")
            if kind != b"blob":
                raise ImpactError("NON_BLOB_ASSET_TREE_ENTRY")
            path = filename.decode("utf-8")
            if path in result:
                raise ImpactError("DUPLICATE_ASSET_PATH")
            result[path] = blob.decode("ascii")
    except (ValueError, UnicodeError) as exc:
        raise ImpactError("MALFORMED_GIT_ASSET_TREE") from exc
    if len(result) > MAX_FILES:
        raise ImpactError("ASSET_FILE_LIMIT_EXCEEDED")
    return result


def read_blobs(repo: Path, paths: list[str], tree: dict[str, str]) -> dict[str, bytes]:
    """One batch read rather than thousands of git-show subprocesses on Windows."""
    if not paths:
        return {}
    requested = [tree[path] for path in paths]
    raw = git(repo, "cat-file", "--batch", data=("\n".join(requested)+"\n").encode())
    pos, total, result = 0, 0, {}
    for path, sha in zip(paths, requested):
        end = raw.find(b"\n", pos)
        if end < 0:
            raise ImpactError("TRUNCATED_GIT_BLOB_HEADER")
        header = raw[pos:end].split(b" ")
        if len(header) != 3 or header[0].decode() != sha or header[1] != b"blob":
            raise ImpactError("GIT_BLOB_HEADER_MISMATCH")
        try:
            size = int(header[2])
        except ValueError as exc:
            raise ImpactError("INVALID_GIT_BLOB_SIZE") from exc
        if size > MAX_SINGLE or size < 0:
            raise ImpactError("BLOB_TOO_LARGE:" + path)
        total += size
        if total > MAX_BYTES:
            raise ImpactError("SCAN_BUDGET_EXCEEDED")
        pos = end + 1
        if pos + size >= len(raw) or raw[pos+size:pos+size+1] != b"\n":
            raise ImpactError("TRUNCATED_GIT_BLOB")
        result[path] = raw[pos:pos+size]
        pos += size + 1
    if pos != len(raw):
        raise ImpactError("EXTRA_GIT_BLOB_DATA")
    return result


def verify_commits(repo: Path, base: str, head: str) -> None:
    if not isinstance(base, str) or not isinstance(head, str) or not (
        EXACT_SHA.fullmatch(base) and EXACT_SHA.fullmatch(head)):
        raise ImpactError("EXACT_40_HEX_SHA_REQUIRED")
    for sha in (base, head):
        actual = git(repo, "rev-parse", "--verify", sha + "^{commit}").decode().strip()
        if actual != sha:
            raise ImpactError("NOT_EXACT_COMMIT_OBJECT")
    try:
        git(repo, "merge-base", "--is-ancestor", base, head)
    except ImpactError as exc:
        raise ImpactError("NON_ANCESTOR_OR_INCOMPLETE_HISTORY") from exc


def impact(repo: Path, base: str, head: str) -> dict:
    verify_commits(repo, base, head)
    old = tree_blobs(repo, base)
    new = tree_blobs(repo, head)
    changed = [
        item.decode("utf-8") for item in git(
            repo, "diff", "--no-renames", "--name-only", "-z",
            base, head, "--", "Assets").split(b"\x00") if item
    ]
    meta_paths = sorted(p for p in new if p.endswith(".meta"))
    serialized_paths = sorted(
        p for p in new if Path(p).suffix.lower() in SERIALIZED
    )
    originals = read_blobs(repo, meta_paths + serialized_paths, new)
    guid_to_path, path_to_guid = {}, {}
    for meta in meta_paths:
        m = META_GUID.search(originals[meta])
        if not m:
            raise ImpactError("INVALID_LOCAL_META_GUID:" + meta)
        guid = m.group(1).decode("ascii")
        if guid in guid_to_path:
            raise ImpactError("DUPLICATE_LOCAL_GUID:" + guid)
        path = meta[:-5]
        guid_to_path[guid], path_to_guid[path] = path, guid

    roots = []
    removed = set()
    unmapped = set()
    for item in changed:
        path = item[:-5] if item.endswith(".meta") else item
        # Unity tracks folder .meta files, but folders do not occur as blobs
        # in 'git ls-tree -r'. Their GUIDs are not smoke-test roots.
        if item.endswith(".meta") and path not in new and any(
                asset.startswith(path + "/") for asset in new):
            continue
        if path not in new:
            removed.add(path)
        elif path not in path_to_guid:
            unmapped.add(path)
        else:
            roots.append(path)
    roots = sorted(set(roots))
    if removed or unmapped:
        # CI41-UNITYREF-001 can evaluate retired GUIDs separately.
        # We MUST NOT green-light a partially computable impact list.
        return {
            "package": "CI41-ASSETIMPACT-001", "base_sha": base, "head_sha": head,
            "status": "INCOMPLETE_FAIL_CLOSED",
            "removed_assets": sorted(removed), "unmapped_assets": sorted(unmapped),
            "changed_roots": roots, "affected_scenes": [], "affected_prefabs": [],
            "reason": "ASSETS_REMOVED_OR_LACK_LOCAL_META",
            "native_unity": "NOT_RUN", "game_integrated": False,
        }

    reverse = defaultdict(set)
    inspected = 0
    for path in serialized_paths:
        payload = originals[path]
        if not payload.startswith((b"%YAML", b"--- !u!")):
            continue
        inspected += 1
        for match in REF_GUID.finditer(payload):
            guid = match.group(1).decode("ascii")
            if guid in guid_to_path and guid_to_path[guid] != path:
                reverse[guid].add(path)

    # Shortest dependency chain (changed asset -> consumer -> scene/prefab).
    # Cyclic prefab references and mutual scriptable objects cannot loop.
    paths_to_target = {p: [p] for p in roots}
    pending = deque(roots)
    while pending:
        source = pending.popleft()
        guid = path_to_guid.get(source)
        if guid is None:
            continue
        for dependent in sorted(reverse.get(guid, ())):
            if dependent in paths_to_target:
                continue
            paths_to_target[dependent] = paths_to_target[source] + [dependent]
            pending.append(dependent)

    def targets(extension: str) -> list[dict]:
        return [{"path": p, "via": paths_to_target[p]} for p in sorted(paths_to_target)
                if p.endswith(extension)]
    return {
        "package": "CI41-ASSETIMPACT-001", "base_sha": base, "head_sha": head,
        "status": "PASS_IMPACT_GRAPH_COMPUTED",
        "diff_authority": "ORIGINAL_GIT_OBJECTS", "changed_asset_paths": changed,
        "changed_roots": roots, "inspected_serialized_files": inspected,
        "affected_scenes": targets(".unity"), "affected_prefabs": targets(".prefab"),
        "affected_other_serialized": [
            {"path": p, "via": paths_to_target[p]} for p in sorted(paths_to_target)
            if p != paths_to_target[p][0] and
            not (p.endswith(".unity") or p.endswith(".prefab"))],
        "native_unity": "NOT_RUN", "game_integrated": False,
    }


def main() -> int:
    parser = argparse.ArgumentParser(description="Source-only Unity impact graph")
    parser.add_argument("--repo", default=".")
    parser.add_argument("--base", required=True)
    parser.add_argument("--head", required=True)
    args = parser.parse_args()
    try:
        result = impact(Path(args.repo).resolve(strict=True), args.base, args.head)
        print(json.dumps(result, sort_keys=True, ensure_ascii=False, indent=2))
        return 0 if result["status"] == "PASS_IMPACT_GRAPH_COMPUTED" else 2
    except (ImpactError, UnicodeError, ValueError, OSError) as exc:
        print(json.dumps({"status": "FAIL_CLOSED", "error": str(exc),
                          "package": "CI41-ASSETIMPACT-001", "native_unity": "NOT_RUN"},
                         sort_keys=True))
        return 2


if __name__ == "__main__":
    sys.exit(main())
