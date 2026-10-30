#!/usr/bin/env python3
"""Fail-closed read-only Windows Unity Player ZIP footprint inspector (NOT a native Unity test)."""
from __future__ import annotations
import argparse
import hashlib
import json
import re
import stat
import sys
import zipfile
from pathlib import Path

SCHEMA = "b01-50-playerfootprint-v1"
SHA = re.compile(r"^[0-9a-f]{64}$")
BAD_CHARS = set('<>:"\\|?*')
DEVICES = {"CON", "PRN", "AUX", "NUL", *(f"COM{i}" for i in range(1, 10)), *(f"LPT{i}" for i in range(1, 10))}
CHUNK = 1024 * 1024


def digest(path: Path) -> str:
    h = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(CHUNK), b""):
            h.update(chunk)
    return h.hexdigest()


def valid_windows_path(raw: str, directory: bool = False) -> str:
    if not isinstance(raw, str) or not raw or raw.startswith("/") or "\\" in raw:
        raise ValueError(f"absolute or invalid ZIP path: {raw!r}")
    candidate = raw[:-1] if directory and raw.endswith("/") else raw
    pieces = candidate.split("/")
    if not pieces or any(not p or p in (".", "..") for p in pieces):
        raise ValueError(f"unsafe ZIP path: {raw!r}")
    for piece in pieces:
        if (piece[-1] in " ." or any(c in BAD_CHARS or ord(c) < 32 for c in piece)
                or piece.split(".")[0].upper() in DEVICES):
            raise ValueError(f"Windows-incompatible ZIP path: {raw!r}")
    return candidate


def canonical_zip_members(z: zipfile.ZipFile, max_files: int, max_total: int, max_file: int, max_ratio: int):
    entries = []
    seen = set()
    total = 0
    for member in z.infolist():
        # zipfile.ZipInfo normalizes Windows backslashes to forward slashes
        # BEFORE exposing member.filename. Inspect the untouched local header,
        # which prevents a raw malicious name from disappearing on Windows.
        if z.fp is None:
            raise ValueError("ZIP source closed")
        old_position = z.fp.tell()
        try:
            z.fp.seek(member.header_offset)
            header = z.fp.read(30)
            if len(header) != 30 or header[:4] != b"PK\x03\x04":
                raise ValueError("invalid local ZIP header")
            name_length = int.from_bytes(header[26:28], "little")
            if not (1 <= name_length <= 4096):
                raise ValueError("unsafe local ZIP filename length")
            raw_filename = z.fp.read(name_length)
            if len(raw_filename) != name_length or b"\\" in raw_filename:
                raise ValueError("backslash or truncated raw local ZIP filename")
        finally:
            z.fp.seek(old_position)
        mode = member.external_attr >> 16
        if mode and stat.S_IFMT(mode) not in (0, stat.S_IFREG, stat.S_IFDIR):
            raise ValueError(f"link or special ZIP member refused: {member.filename}")
        path = valid_windows_path(member.filename, member.is_dir())
        folded = path.casefold()
        if folded in seen:
            raise ValueError(f"duplicate or Windows-case-colliding member: {path}")
        seen.add(folded)
        if member.flag_bits & 1:
            raise ValueError(f"encrypted ZIP member: {path}")
        if member.is_dir():
            if member.file_size or (mode and stat.S_IFMT(mode) == stat.S_IFREG):
                raise ValueError(f"invalid directory member: {path}")
            continue
        if mode and stat.S_IFMT(mode) == stat.S_IFDIR:
            raise ValueError(f"directory/file type mismatch: {path}")
        if member.file_size > max_file:
            raise ValueError(f"oversized ZIP entry: {path}")
        total += member.file_size
        if total > max_total:
            raise ValueError("uncompressed total budget exceeded")
        if len(entries) >= max_files:
            raise ValueError("member count budget exceeded")
        if member.file_size > 0 and (member.compress_size == 0 or
                                     member.file_size > max_ratio * member.compress_size):
            raise ValueError(f"suspicious compression ratio: {path}")
        entries.append((path, member))
    # Explicitly reject file/dir type conflicts, even for implicit folders.
    folded_files = {name.casefold() for name, _ in entries}
    for name, _ in entries:
        parts = name.split("/")
        if any("/".join(parts[:i]).casefold() in folded_files for i in range(1, len(parts))):
            raise ValueError(f"file used as parent directory: {name}")
    return entries


def detect_root(names: list[str], build_name: str) -> tuple[str, str]:
    required = [build_name + ".exe", "UnityPlayer.dll",
                build_name + "_Data/globalgamemanagers"]
    variants = [""] + sorted({name.split("/")[0] + "/" for name in names if "/" in name})
    matching = []
    for prefix in variants:
        if all((prefix + r).casefold() in {n.casefold() for n in names} for r in required):
            if prefix and any(not n.startswith(prefix) for n in names):
                continue
            matching.append(prefix)
    if len(matching) != 1:
        raise ValueError("Unity Windows Player layout missing or ambiguous (exe, UnityPlayer.dll, _Data/globalgamemanagers)")
    return matching[0], required[0]


def summarize(entries: list[dict]) -> dict:
    kinds: dict[str, dict[str, int]] = {}
    for e in entries:
        ext = Path(e["path"]).suffix.lower() or "[none]"
        s = kinds.setdefault(ext, {"files": 0, "bytes": 0})
        s["files"] += 1
        s["bytes"] += e["bytes"]
    return {"files": len(entries), "player_uncompressed_bytes": sum(e["bytes"] for e in entries),
            "by_extension": dict(sorted(kinds.items())),
            "largest_files": sorted(entries, key=lambda e: (-e["bytes"], e["path"]))[:20]}


def inspect(archive: Path, build_name: str = "RoboRougelike",
            max_files: int = 100000, max_total: int = 8 * 1024**3,
            max_file: int = 4 * 1024**3, max_ratio: int = 1000) -> dict:
    if archive.is_symlink() or not archive.is_file():
        raise ValueError("ZIP archive must be a regular, non-symlink file")
    if not re.fullmatch(r"[A-Za-z0-9_-]+", build_name):
        raise ValueError("build name must be a simple executable stem")
    if min(max_files, max_total, max_file, max_ratio) < 1:
        raise ValueError("invalid resource budget")
    archive_sha = digest(archive)
    archive_size = archive.stat().st_size
    with zipfile.ZipFile(archive) as z:
        members = canonical_zip_members(z, max_files, max_total, max_file, max_ratio)
        names = [n for n, _ in members]
        root, exe = detect_root(names, build_name)
        data = []
        for path, member in members:
            sha = hashlib.sha256()
            read = 0
            signature = b""
            with z.open(member, "r") as stream:
                for block in iter(lambda: stream.read(CHUNK), b""):
                    if read == 0:
                        signature = block[:2]
                    read += len(block)
                    if read > member.file_size:
                        raise ValueError(f"ZIP size drift: {path}")
                    sha.update(block)
            if read != member.file_size:
                raise ValueError(f"ZIP truncated: {path}")
            local = path[len(root):]
            if local.casefold() in (exe.casefold(), "unityplayer.dll") and signature != b"MZ":
                raise ValueError(f"not a valid PE signature: {path}")
            data.append({"path": local, "bytes": read, "sha256": sha.hexdigest()})
    data.sort(key=lambda e: e["path"])
    return {"schema": SCHEMA, "provenance": "WINDOWS_PLAYER_ZIP_BYTES_NOT_NATIVE_EXECUTION",
            "build_name": build_name, "archive_sha256": archive_sha, "archive_bytes": archive_size,
            "entries": data, "summary": summarize(data)}


def verify_report(report: object) -> dict:
    if not isinstance(report, dict) or set(report) != {"schema", "provenance", "build_name", "archive_sha256", "archive_bytes", "entries", "summary"}:
        raise ValueError("invalid report structure")
    if report["schema"] != SCHEMA or report["provenance"] != "WINDOWS_PLAYER_ZIP_BYTES_NOT_NATIVE_EXECUTION":
        raise ValueError("unsupported or untrusted report provenance")
    if not isinstance(report["archive_sha256"], str) or not SHA.fullmatch(report["archive_sha256"]):
        raise ValueError("invalid archive SHA")
    if type(report["archive_bytes"]) is not int or report["archive_bytes"] < 1:
        raise ValueError("invalid archive size")
    if not isinstance(report["build_name"], str) or not re.fullmatch(r"[A-Za-z0-9_-]+", report["build_name"]):
        raise ValueError("invalid build name")
    entries = report["entries"]
    if not isinstance(entries, list) or not entries:
        raise ValueError("empty report")
    seen = set()
    for e in entries:
        if not isinstance(e, dict) or set(e) != {"path", "bytes", "sha256"}:
            raise ValueError("invalid member fields")
        name = valid_windows_path(e["path"])
        if name.casefold() in seen:
            raise ValueError("duplicate member in report")
        seen.add(name.casefold())
        if type(e["bytes"]) is not int or e["bytes"] < 0 or not isinstance(e["sha256"], str) or not SHA.fullmatch(e["sha256"]):
            raise ValueError("invalid member hash/bytes")
    if entries != sorted(entries, key=lambda e: e["path"]) or report["summary"] != summarize(entries):
        raise ValueError("tampered or noncanonical summary")
    return report


def compare(base: dict, after: dict) -> dict:
    verify_report(base)
    verify_report(after)
    if base["build_name"] != after["build_name"]:
        raise ValueError("incompatible build names")
    b = {e["path"]: e for e in base["entries"]}
    a = {e["path"]: e for e in after["entries"]}
    return {"schema": SCHEMA, "provenance": "WINDOWS_PLAYER_ZIP_BYTES_NOT_NATIVE_EXECUTION",
            "build_name": base["build_name"],
            "before_archive_sha256": base["archive_sha256"], "after_archive_sha256": after["archive_sha256"],
            "compressed_delta_bytes": after["archive_bytes"]-base["archive_bytes"],
            "uncompressed_delta_bytes": after["summary"]["player_uncompressed_bytes"]-base["summary"]["player_uncompressed_bytes"],
            "added": sorted(a.keys()-b.keys()), "removed": sorted(b.keys()-a.keys()),
            "modified": sorted(k for k in a.keys() & b.keys() if a[k] != b[k])}


def exclusive_write(path: Path, data: dict) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("x", encoding="utf-8", newline="\n") as f:
        json.dump(data, f, sort_keys=True, ensure_ascii=False, indent=2)
        f.write("\n")


def main(args=None) -> int:
    p = argparse.ArgumentParser(description=__doc__)
    sub = p.add_subparsers(dest="action", required=True)
    i = sub.add_parser("inspect")
    i.add_argument("--zip", type=Path, required=True)
    i.add_argument("--out", type=Path, required=True)
    i.add_argument("--build-name", default="RoboRougelike")
    i.add_argument("--max-total-bytes", type=int, default=8 * 1024**3)
    i.add_argument("--max-entries", type=int, default=100000)
    c = sub.add_parser("compare")
    c.add_argument("--before", required=True, type=Path)
    c.add_argument("--after", required=True, type=Path)
    c.add_argument("--out", required=True, type=Path)
    c.add_argument("--max-uncompressed-growth", type=int)
    ns = p.parse_args(args)
    try:
        if ns.action == "inspect":
            if ns.zip.resolve() == ns.out.resolve():
                raise ValueError("output overlaps input")
            report = inspect(ns.zip, ns.build_name, max_total=ns.max_total_bytes, max_files=ns.max_entries)
            exclusive_write(ns.out, report)
            print(f"PLAYER_ZIP_AUDITED files={report['summary']['files']} bytes={report['summary']['player_uncompressed_bytes']} archive_sha256={report['archive_sha256']}")
        else:
            if ns.max_uncompressed_growth is not None and ns.max_uncompressed_growth < 0:
                raise ValueError("negative growth budget")
            report = compare(json.loads(ns.before.read_text(encoding="utf-8")),
                             json.loads(ns.after.read_text(encoding="utf-8")))
            exclusive_write(ns.out, report)
            print(f"PLAYER_ZIP_DELTA uncompressed_bytes={report['uncompressed_delta_bytes']}")
            if ns.max_uncompressed_growth is not None and report["uncompressed_delta_bytes"] > ns.max_uncompressed_growth:
                print("PLAYER_SIZE_BUDGET_EXCEEDED", file=sys.stderr)
                return 2
    except (ValueError, OSError, zipfile.BadZipFile, RuntimeError, json.JSONDecodeError) as exc:
        print(f"ERROR: {exc}", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
