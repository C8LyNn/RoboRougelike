#!/usr/bin/env python3
"""Read-only physical source-asset footprint inventory; NOT a Unity Player build profiler."""
from __future__ import annotations
import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import sys

SCHEMA = "b01-50-assetfootprint-v1"
SHA_RE = re.compile(r"^[0-9a-f]{64}$")


def sha256_file(path: Path) -> str:
    h = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            h.update(block)
    return h.hexdigest()


def checked_path(value: str) -> str:
    if not isinstance(value, str) or not value or "\\" in value or value.startswith("/"):
        raise ValueError(f"invalid relative path: {value!r}")
    parts = value.split("/")
    if any(part in ("", ".", "..") for part in parts):
        raise ValueError(f"unsafe relative path: {value!r}")
    return value


def suffix(path: str) -> str:
    name = path.rsplit("/", 1)[-1]
    return ("." + name.rsplit(".", 1)[-1].lower()) if "." in name and not name.startswith(".") else "[none]"


def summarize(entries: list[dict]) -> dict:
    ext = {}
    groups = {}
    for e in entries:
        kind = suffix(e["path"])
        s = ext.setdefault(kind, {"files": 0, "bytes": 0})
        s["files"] += 1
        s["bytes"] += e["bytes"]
        groups.setdefault((e["sha256"], e["bytes"]), []).append(e["path"])
    duplicates = [{"sha256": sh, "bytes_each": n, "paths": paths, "potential_redundant_bytes": (len(paths)-1)*n}
                  for (sh, n), paths in groups.items() if len(paths) > 1]
    duplicates.sort(key=lambda d: (-d["potential_redundant_bytes"], d["paths"]))
    largest = sorted(entries, key=lambda e: (-e["bytes"], e["path"]))[:20]
    return {"file_count": len(entries), "source_bytes": sum(e["bytes"] for e in entries),
            "by_extension": dict(sorted(ext.items())), "exact_duplicate_groups": duplicates,
            "largest_source_files": largest}


def scan(root: Path) -> dict:
    if root.is_symlink() or not root.is_dir():
        raise ValueError("source must be an existing non-symlink directory")
    root = root.resolve(strict=True)
    files = []
    for directory, folders, names in os.walk(root, topdown=True, followlinks=False):
        # Fail closed for symlinked directories, including those that os.walk would otherwise skip.
        for name in folders + names:
            p = Path(directory) / name
            if p.is_symlink():
                raise ValueError(f"symlink refused: {p}")
        folders[:] = sorted(n for n in folders if n not in (".git", "Library", "Temp"))
        for name in sorted(names):
            if name.endswith(".meta") or name in (".DS_Store", "Thumbs.db"):
                continue
            p = Path(directory) / name
            if not p.is_file():
                raise ValueError(f"non-regular file refused: {p}")
            rel = checked_path(p.relative_to(root).as_posix())
            # Verify size did not change while hashing; live-mutating source cannot yield a stable receipt.
            before = p.stat()
            h = sha256_file(p)
            after = p.stat()
            if (before.st_size, before.st_mtime_ns, before.st_ino) != (after.st_size, after.st_mtime_ns, after.st_ino):
                raise ValueError(f"source mutated during scan: {rel}")
            files.append({"path": rel, "bytes": before.st_size, "sha256": h})
    files.sort(key=lambda e: e["path"])
    return {"schema": SCHEMA, "input_kind": "UNITY_SOURCE_ASSETS_NOT_PLAYER_BUILD",
            "entries": files, "summary": summarize(files)}


def validate(doc: object) -> dict:
    if not isinstance(doc, dict) or doc.get("schema") != SCHEMA or doc.get("input_kind") != "UNITY_SOURCE_ASSETS_NOT_PLAYER_BUILD":
        raise ValueError("unknown manifest schema or source provenance")
    es = doc.get("entries")
    if not isinstance(es, list):
        raise ValueError("missing entries")
    seen = set()
    clean = []
    for e in es:
        if not isinstance(e, dict) or set(e) != {"path", "bytes", "sha256"}:
            raise ValueError("invalid entry fields")
        p = checked_path(e["path"])
        if p in seen:
            raise ValueError(f"duplicate path: {p}")
        seen.add(p)
        if type(e["bytes"]) is not int or e["bytes"] < 0 or not isinstance(e["sha256"], str) or not SHA_RE.fullmatch(e["sha256"]):
            raise ValueError(f"invalid size/hash for {p}")
        clean.append(e)
    if clean != sorted(clean, key=lambda x: x["path"]):
        raise ValueError("entries not in canonical sorted order")
    expected = summarize(clean)
    if doc.get("summary") != expected:
        raise ValueError("manifest summary inconsistent with original entries")
    return doc


def delta(base: dict, candidate: dict) -> dict:
    validate(base)
    validate(candidate)
    old = {e["path"]: e for e in base["entries"]}
    new = {e["path"]: e for e in candidate["entries"]}
    added = [new[p] for p in sorted(new.keys() - old.keys())]
    removed = [old[p] for p in sorted(old.keys() - new.keys())]
    changed = [{"path": p, "before_bytes": old[p]["bytes"], "after_bytes": new[p]["bytes"],
                "delta_bytes": new[p]["bytes"] - old[p]["bytes"],
                "content_changed": old[p]["sha256"] != new[p]["sha256"]}
               for p in sorted(old.keys() & new.keys()) if old[p] != new[p]]
    b = base["summary"]["source_bytes"]
    c = candidate["summary"]["source_bytes"]
    return {"schema": SCHEMA, "input_kind": "UNITY_SOURCE_ASSETS_NOT_PLAYER_BUILD",
            "base_source_bytes": b, "candidate_source_bytes": c, "delta_source_bytes": c-b,
            "added": added, "removed": removed, "changed": changed}


def save_exclusive(doc: dict, path: Path):
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("x", encoding="utf-8", newline="\n") as out:
        json.dump(doc, out, sort_keys=True, ensure_ascii=False, indent=2)
        out.write("\n")


def main(argv=None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    sub = parser.add_subparsers(dest="command", required=True)
    a = sub.add_parser("scan", help="inventory Unity Assets SOURCE bytes, not build size")
    a.add_argument("--assets", required=True, type=Path)
    a.add_argument("--out", required=True, type=Path)
    b = sub.add_parser("compare", help="compare verified source inventories")
    b.add_argument("--base", required=True, type=Path)
    b.add_argument("--candidate", required=True, type=Path)
    b.add_argument("--out", required=True, type=Path)
    b.add_argument("--max-growth-bytes", type=int)
    args = parser.parse_args(argv)
    try:
        if args.command == "scan":
            if args.out.resolve().is_relative_to(args.assets.resolve()):
                raise ValueError("output must be outside the scanned source tree")
            report = scan(args.assets)
            save_exclusive(report, args.out)
            print(f"SOURCE_SCAN files={report['summary']['file_count']} bytes={report['summary']['source_bytes']} output={args.out}")
        else:
            if args.max_growth_bytes is not None and args.max_growth_bytes < 0:
                raise ValueError("max growth must be nonnegative")
            base = json.loads(args.base.read_text(encoding="utf-8"))
            cand = json.loads(args.candidate.read_text(encoding="utf-8"))
            result = delta(base, cand)
            save_exclusive(result, args.out)
            print(f"SOURCE_DELTA bytes={result['delta_source_bytes']} output={args.out}")
            if args.max_growth_bytes is not None and result["delta_source_bytes"] > args.max_growth_bytes:
                print("SOURCE_GROWTH_BUDGET_EXCEEDED", file=sys.stderr)
                return 2
    except (ValueError, OSError, json.JSONDecodeError) as exc:
        print(f"ERROR: {exc}", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
