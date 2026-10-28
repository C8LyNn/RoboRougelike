#!/usr/bin/env python3
"""CI41-PATHPORT-001: Git-object-only Windows/Linux Unity asset path gate.

No modifications to Assets, native Unity, global CI, Git history, or files.
Checks Git-tracked asset paths at an exact commit SHA, including implicit
directories, for predictable cross-platform checkout/import hazards.
"""
from __future__ import annotations

import argparse
import json
import re
import subprocess
import sys
import unicodedata
from pathlib import Path

COMMIT = re.compile(r"[0-9a-f]{40}\Z")
DEVICE = re.compile(
    r"(?i)^(?:CON|PRN|AUX|NUL|CONIN\$|CONOUT\$|COM[1-9¹²³]|LPT[1-9¹²³])(?:\..*)?$"
)
ILLEGAL = frozenset('<>:"\\|?*')
MAX_TRACKED = 25000
MAX_REPORT = 5000


class PathGateError(Exception):
    pass


def git(repo: Path, *args: str) -> bytes:
    try:
        p = subprocess.run(["git", "-C", str(repo), *args],
                           capture_output=True, check=False)
    except OSError as exc:
        raise PathGateError("GIT_NOT_INSTALLED") from exc
    if p.returncode != 0:
        raise PathGateError("GIT_COMMAND_FAILED:" + " ".join(args[:3]))
    return p.stdout


def original_asset_paths(repo: Path, sha: str) -> list[str]:
    if not isinstance(sha, str) or not COMMIT.fullmatch(sha):
        raise PathGateError("EXACT_40_HEX_COMMIT_REQUIRED")
    rev = git(repo, "rev-parse", "--verify", sha + "^{commit}").decode().strip()
    if rev != sha:
        raise PathGateError("ORIGINAL_COMMIT_OBJECT_MISMATCH")
    raw = git(repo, "ls-tree", "-r", "-z", sha, "--", "Assets")
    paths: list[str] = []
    for record in raw.split(b"\0"):
        if not record:
            continue
        try:
            info, name = record.split(b"\t", 1)
            mode, kind, blob = info.split(b" ")
            path = name.decode("utf-8")
        except (ValueError, UnicodeDecodeError) as exc:
            raise PathGateError("NON_UTF8_OR_MALFORMED_GIT_TREE") from exc
        if kind != b"blob" or mode not in (b"100644", b"100755", b"120000"):
            raise PathGateError("UNEXPECTED_GIT_TREE_ENTRY")
        if not re.fullmatch(rb"[0-9a-f]{40}", blob):
            raise PathGateError("MALFORMED_GIT_BLOB_ID")
        if not path.startswith("Assets/"):
            raise PathGateError("UNEXPECTED_NON_ASSET_PATH")
        paths.append(path)
    if not paths or len(paths) > MAX_TRACKED or len(paths) != len(set(paths)):
        raise PathGateError("EMPTY_OVERSIZED_OR_DUPLICATED_GIT_ASSETS")
    return paths


def _issue(kind: str, path: str, component: str, detail: str) -> dict:
    return {"kind": kind, "path": path, "component": component, "detail": detail}


def audit_paths(paths: list[str]) -> dict:
    if not isinstance(paths, list) or not paths or len(paths) > MAX_TRACKED:
        raise PathGateError("INVALID_OR_EMPTY_PATH_SET")
    if any(not isinstance(p, str) or not p.startswith("Assets/") for p in paths):
        raise PathGateError("INVALID_ASSET_PATH_INPUT")
    if len(set(paths)) != len(paths):
        raise PathGateError("DUPLICATE_TRACKED_PATH")
    issues: list[dict] = []
    # Every implicit folder must be checked, not only complete file paths.
    # Git on Linux permits both Assets/Foo and Assets/foo as tree entries.
    seen: dict[str, str] = {}
    for path in sorted(paths):
        comps = path.split("/")
        for index, component in enumerate(comps):
            prefix = "/".join(comps[:index+1])
            if not component or component in (".", ".."):
                issues.append(_issue("INVALID_PATH_COMPONENT", path, component, prefix))
                continue
            if any(c in ILLEGAL or ord(c) < 32 for c in component):
                issues.append(_issue("WINDOWS_INVALID_CHARACTER", path, component, prefix))
            if component.endswith((" ", ".")):
                issues.append(_issue("WINDOWS_TRAILING_SPACE_OR_DOT", path, component, prefix))
            if DEVICE.fullmatch(component):
                issues.append(_issue("WINDOWS_DEVICE_NAME", path, component, prefix))
            if len(component.encode("utf-16-le")) // 2 > 255:
                issues.append(_issue("WINDOWS_COMPONENT_TOO_LONG", path, component, prefix))
            # Python Unicode NFC+casefold deliberately conservative for portable
            # names. It may flag filesystem-specific lookalikes; no auto-rename.
            alias = unicodedata.normalize("NFC", prefix).casefold()
            original = seen.setdefault(alias, prefix)
            if original != prefix:
                # Report one collision per actual non-identical path/prefix, not
                # duplicates for every file that uses the same folder alias.
                issue = _issue("CROSS_PLATFORM_PATH_COLLISION", path, component,
                               original + " <> " + prefix)
                if issue not in issues:
                    issues.append(issue)
        if len(issues) > MAX_REPORT:
            raise PathGateError("TOO_MANY_PATH_PORTABILITY_ISSUES")
    issues.sort(key=lambda z: (z["kind"], z["path"], z["component"], z["detail"]))
    return {
        "status": "PASS_PORTABLE_ASSET_PATHS" if not issues else "FAIL_PORTABILITY",
        "files_inspected": len(paths),
        "issues": issues,
        "issues_count": len(issues),
        "scope": "ORIGINAL_GIT_TRACKED_ASSETS_PATHS_AND_IMPLICIT_FOLDERS",
        "native_unity": "NOT_RUN",
        "g1": "NOT_RUN",
        "game_integrated": False,
    }


def run(repo: Path, sha: str) -> dict:
    paths = original_asset_paths(repo, sha)
    out = audit_paths(paths)
    return {"package": "CI41-PATHPORT-001", "original_commit_sha": sha, **out}


def main() -> int:
    parser = argparse.ArgumentParser(description="Unity tracked path cross-platform gate")
    parser.add_argument("--repo", default=".")
    parser.add_argument("--commit", required=True)
    args = parser.parse_args()
    try:
        result = run(Path(args.repo).resolve(strict=True), args.commit)
        print(json.dumps(result, ensure_ascii=False, indent=2, sort_keys=True))
        return 0 if result["status"].startswith("PASS") else 2
    except (OSError, PathGateError, ValueError, UnicodeError) as exc:
        print(json.dumps({
            "package": "CI41-PATHPORT-001", "status": "FAIL_CLOSED_SOURCE_UNAVAILABLE",
            "error": str(exc), "native_unity": "NOT_RUN", "g1": "NOT_RUN"
        }, sort_keys=True))
        return 2


if __name__ == "__main__":
    sys.exit(main())
