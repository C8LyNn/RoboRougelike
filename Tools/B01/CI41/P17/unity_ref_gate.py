#!/usr/bin/env python3
"""CI41-UNITYREF-001: read-only Unity serialized GUID retirement gate.

Catches an integration regression which the existing validate_repository.py
cannot: a previously tracked local Unity GUID is removed/changed while a
serialized .unity/.prefab/.asset/.mat etc file still points at that GUID.

Only GUIDs *known in the BASE repository* are candidates: new unknown GUIDs
may legitimately belong to Unity packages or built-ins and are NOT declared
broken by this gate. It is a targeted pre-integration check, not Unity G1.
"""
from __future__ import annotations

import argparse
import json
import re
import subprocess
from pathlib import Path

SHA = re.compile(r"[0-9a-f]{40}\Z")
GUID = re.compile(r"(?m)^guid: ([0-9a-f]{32})\s*$")
REFERENCE = re.compile(r"(?<![a-zA-Z0-9_])guid:\s*([0-9a-f]{32})(?![0-9a-f])")
YAML_SUFFIXES = frozenset((
    ".unity", ".prefab", ".asset", ".mat", ".controller", ".anim",
    ".overridecontroller", ".playable", ".lighting", ".rendertexture",
    ".mixer", ".physicmaterial", ".physicsmaterial2d", ".terrainlayer",
))
MAX_YAML_BYTES = 24 * 1024 * 1024


class GateError(Exception):
    pass


def git(repo: Path, *args: str) -> bytes:
    try:
        p = subprocess.run(["git", "-C", str(repo), *args],
                           capture_output=True, check=False)
    except OSError as exc:
        raise GateError("GIT_UNAVAILABLE") from exc
    if p.returncode:
        raise GateError("GIT_FAILED:" + " ".join(args[:3]))
    return p.stdout


def tracked(repo: Path, sha: str) -> list[str]:
    data = git(repo, "ls-tree", "-r", "--name-only", "-z", sha, "--", "Assets")
    try:
        paths = [v.decode("utf-8") for v in data.split(b"\0") if v]
    except UnicodeDecodeError as exc:
        raise GateError("NON_UTF8_ASSET_PATH") from exc
    if len(paths) != len(set(paths)):
        raise GateError("DUPLICATE_TREE_PATH")
    return paths


def read_blob(repo: Path, sha: str, path: str) -> bytes:
    if not path.startswith("Assets/") or "\x00" in path or "\\" in path:
        raise GateError("UNSAFE_ASSET_PATH")
    return git(repo, "show", sha + ":" + path)


def meta_guid(repo: Path, sha: str, path: str) -> str:
    raw = read_blob(repo, sha, path)
    if len(raw) > 1_000_000:
        raise GateError("META_TOO_LARGE:" + path)
    try:
        value = raw.decode("utf-8-sig")
    except UnicodeDecodeError as exc:
        raise GateError("NON_UTF8_META:" + path) from exc
    match = GUID.search(value)
    if match is None:
        raise GateError("MISSING_META_GUID:" + path)
    return match.group(1)


def check(repo: Path, base: str, head: str) -> dict:
    if not SHA.fullmatch(base) or not SHA.fullmatch(head):
        raise GateError("EXACT_COMMIT_SHA_REQUIRED")
    for sha in (base, head):
        if git(repo, "rev-parse", "--verify", sha + "^{commit}").decode().strip() != sha:
            raise GateError("COMMIT_OBJECT_REQUIRED")
    git(repo, "merge-base", "--is-ancestor", base, head)
    before = tracked(repo, base)
    after = tracked(repo, head)
    old_paths, new_paths = set(before), set(after)

    changed = [p.decode("utf-8") for p in
               git(repo, "diff", "--no-renames", "--name-only", "-z",
                   base, head, "--", "Assets").split(b"\0") if p]
    # Read original .meta GUIDs only when the file changed or vanished.
    changed_meta = sorted(p for p in changed
                          if p.endswith(".meta") and p in old_paths)
    old_guid_by_path = {p: meta_guid(repo, base, p) for p in changed_meta}
    new_guid_by_path = {p: meta_guid(repo, head, p) for p in changed_meta
                        if p in new_paths}
    retired = {guid: p for p, guid in old_guid_by_path.items()
               if new_guid_by_path.get(p) != guid}

    current_guids = {}
    # Scan *all* tracked meta: moving same meta with GUID intact is valid.
    if retired:
        for path in sorted(p for p in after if p.endswith(".meta")):
            g = meta_guid(repo, head, path)
            current_guids.setdefault(g, []).append(path)
    retired = {g: p for g, p in retired.items() if g not in current_guids}

    dangling = []
    inspected = 0
    if retired:
        for path in sorted(p for p in after if
                           Path(p).suffix.lower() in YAML_SUFFIXES):
            content = read_blob(repo, head, path)
            if len(content) > MAX_YAML_BYTES:
                raise GateError("YAML_TOO_LARGE_TO_INSPECT:" + path)
            if not content.startswith((b"%YAML", b"--- !u!")):
                continue  # Binary/material non-YAML; must be reviewed natively.
            try:
                text = content.decode("utf-8-sig")
            except UnicodeDecodeError as exc:
                raise GateError("NON_UTF8_UNITY_YAML:" + path) from exc
            inspected += 1
            for number, line in enumerate(text.splitlines(), start=1):
                for match in REFERENCE.finditer(line):
                    guid = match.group(1)
                    if guid in retired:
                        dangling.append({
                            "file": path, "line": number, "guid": guid,
                            "former_meta": retired[guid],
                        })
    return {
        "package": "CI41-UNITYREF-001",
        "source": {"base": base, "head": head, "diff_authority": "ORIGINAL_GIT"},
        "status": "FAIL_DANGLING_RETIRED_LOCAL_GUID" if dangling else "PASS_NO_DANGLING_RETIRED_LOCAL_GUID",
        "changed_meta_count": len(changed_meta),
        "retired_local_guids": sorted(retired),
        "inspected_unity_yaml_files": inspected,
        "dangling_references": sorted(dangling, key=lambda x: (x["file"], x["line"], x["guid"])),
        "native_unity": "NOT_RUN",
        "g1": "NOT_RUN",
        "game_integrated": False,
    }


def main() -> int:
    a = argparse.ArgumentParser()
    a.add_argument("--repo", default=".")
    a.add_argument("--base", required=True)
    a.add_argument("--head", required=True)
    args = a.parse_args()
    try:
        result = check(Path(args.repo).resolve(strict=True), args.base, args.head)
        print(json.dumps(result, ensure_ascii=False, sort_keys=True, indent=2))
        return 0 if result["status"].startswith("PASS") else 2
    except (GateError, OSError, ValueError, UnicodeDecodeError) as exc:
        print(json.dumps({"package": "CI41-UNITYREF-001",
                          "status": "FAIL_CLOSED_SOURCE_UNAVAILABLE",
                          "error": str(exc), "native_unity": "NOT_RUN"}, sort_keys=True))
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
