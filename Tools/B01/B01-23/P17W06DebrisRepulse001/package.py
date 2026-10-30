#!/usr/bin/env python3
"""Deterministic, Git-object-pinned physical Unity source archive and safe receiver."""
import argparse
import hashlib
import io
import json
from pathlib import Path
import shutil
import subprocess
import sys
import zipfile

ROOT = Path(__file__).resolve().parents[4]
TOOL = "Tools/B01/B01-23/P17W06DebrisRepulse001/"
ASSETS = "Assets/B0123DebrisRepulse001"
FILES = tuple(sorted([
    ASSETS + ".meta",
    ASSETS + "/Runtime.meta",
    ASSETS + "/Runtime/B0123.DebrisRepulse.Runtime.asmdef",
    ASSETS + "/Runtime/B0123.DebrisRepulse.Runtime.asmdef.meta",
    ASSETS + "/Runtime/DebrisImpulseCore.cs",
    ASSETS + "/Runtime/DebrisImpulseCore.cs.meta",
    ASSETS + "/Runtime/DebrisRepulsionEmitter.cs",
    ASSETS + "/Runtime/DebrisRepulsionEmitter.cs.meta",
    TOOL + "README.md", TOOL + "DebrisRepulseTests.csproj",
    TOOL + "Program.cs", TOOL + "package.py", TOOL + "test_package.py",
]))
MANIFEST = "PACKAGE_MANIFEST.json"
PACKAGE_ID = "B01-23-DEBRIS-REPULSE-001"
VERSION = "1.0.0"
BASE_SHA = "5f06af81e2dd9d877e4300259bb3dd008cb9c9d1"

def git(*args):
    return subprocess.check_output(["git", *args], cwd=ROOT)

def sha(data):
    return hashlib.sha256(data).hexdigest()

def source_sha():
    return git("rev-parse", "HEAD").decode("ascii").strip()

def original_file(path):
    return git("show", "HEAD:" + path)

def canonical_info(path):
    item = zipfile.ZipInfo(path, date_time=(1980, 1, 1, 0, 0, 0))
    item.compress_type = zipfile.ZIP_STORED
    item.create_system = 3
    item.external_attr = 0o100644 << 16
    return item

def produce():
    source = source_sha()
    payloads = {path: original_file(path) for path in FILES}
    meta = {
        "schema": "b01-source-package-v1",
        "package_id": PACKAGE_ID,
        "revision": VERSION,
        "owner": "B01-23",
        "producer_pr": 421,
        "source_sha": source,
        "source_base_sha": BASE_SHA,
        "unity_version": "6000.3.14f1",
        "codex_target_sha": None,
        "codex_ack": False,
        "native_unity": "NOT_RUN",
        "qa_independent": "PENDING",
        "scope": ["Assets/B0123DebrisRepulse001/**", TOOL + "**"],
        "rollback": "Remove only optional adapter hook and isolated asset subtree from disposable receiver",
        "files": [{"path": p, "bytes": len(payloads[p]), "sha256": sha(payloads[p])} for p in FILES],
    }
    payloads[MANIFEST] = (json.dumps(meta, sort_keys=True, separators=(",", ":"), ensure_ascii=True) + "\n").encode("utf-8")
    output = io.BytesIO()
    with zipfile.ZipFile(output, "w", compression=zipfile.ZIP_STORED) as archive:
        for path in sorted(payloads):
            archive.writestr(canonical_info(path), payloads[path])
    return output.getvalue(), meta

def verified_members(data):
    with zipfile.ZipFile(io.BytesIO(data), "r") as archive:
        entries = archive.infolist()
        names = [member.filename for member in entries]
        if len(names) != len(set(names)) or set(names) != set(FILES) | {MANIFEST}:
            raise ValueError("missing, duplicate or unexpected member")
        if any(name.startswith("/") or ".." in name.split("/") or "\\" in name for name in names):
            raise ValueError("path traversal")
        payloads = {name: archive.read(name) for name in names}
    meta = json.loads(payloads[MANIFEST].decode("utf-8"))
    if meta.get("package_id") != PACKAGE_ID or meta.get("revision") != VERSION or meta.get("source_sha") != source_sha():
        raise ValueError("producer/source revision mismatch")
    records = meta.get("files")
    if not isinstance(records, list) or [x.get("path") for x in records] != list(FILES):
        raise ValueError("manifest files mismatch")
    for row in records:
        path = row["path"]
        data_bytes = payloads[path]
        if row.get("bytes") != len(data_bytes) or row.get("sha256") != sha(data_bytes) or data_bytes != original_file(path):
            raise ValueError("file size/hash or original Git blob mismatch: " + path)
    if meta.get("native_unity") != "NOT_RUN" or meta.get("codex_ack") is not False:
        raise ValueError("false native/Codex acceptance")
    return payloads, meta

def outside_repository(path):
    candidate = path.resolve()
    if candidate == ROOT.resolve() or ROOT.resolve() in candidate.parents:
        raise ValueError("receiver location inside source repo is forbidden")

def stage(data, destination):
    outside_repository(destination)
    if destination.exists():
        raise ValueError("destination exists; overwrite forbidden")
    payloads, _ = verified_members(data)
    destination.mkdir(parents=False, exist_ok=False)
    try:
        for name, content in payloads.items():
            out = destination / name
            out.parent.mkdir(parents=True, exist_ok=True)
            out.write_bytes(content)
    except Exception:
        shutil.rmtree(destination)
        raise

def rollback(data, destination):
    outside_repository(destination)
    payloads, _ = verified_members(data)
    if not destination.is_dir() or destination.is_symlink():
        raise ValueError("staged destination missing or unsafe")
    expected_files = set(payloads)
    expected_dirs = {""}
    for name in expected_files:
        parts = name.split("/")[:-1]
        for i in range(1, len(parts) + 1):
            expected_dirs.add("/".join(parts[:i]))
    for found in destination.rglob("*"):
        if found.is_symlink():
            raise ValueError("symlink in stage; rollback refused")
        relative = found.relative_to(destination).as_posix()
        if found.is_dir():
            if relative not in expected_dirs:
                raise ValueError("unknown directory; rollback refused")
        elif relative not in expected_files or found.read_bytes() != payloads[relative]:
            raise ValueError("modified/unowned file; rollback refused")
    actual_files = {x.relative_to(destination).as_posix() for x in destination.rglob("*") if x.is_file()}
    if actual_files != expected_files:
        raise ValueError("missing staged file; rollback refused")
    shutil.rmtree(destination)

def main():
    cli = argparse.ArgumentParser(description=__doc__)
    cli.add_argument("mode", choices=["build", "verify", "stage", "rollback"])
    cli.add_argument("--archive", required=True)
    cli.add_argument("--destination")
    args = cli.parse_args()
    path = Path(args.archive)
    if args.mode == "build":
        if path.exists():
            raise ValueError("archive exists; overwriting prohibited")
        data, _ = produce()
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_bytes(data)
        verified_members(data)
        print("PASS original Git-object ZIP", "bytes", len(data), "sha256", sha(data), "entries", len(FILES) + 1)
        print("SOURCE_SHA", source_sha(), "UNITY_NATIVE_NOT_RUN")
    else:
        data = path.read_bytes()
        if args.mode == "verify":
            verified_members(data)
            print("PASS original archive verified", "sha256", sha(data), "entries", len(FILES) + 1)
        elif args.mode == "stage":
            if not args.destination: raise ValueError("missing --destination")
            stage(data, Path(args.destination))
            print("PASS isolated stage")
        elif args.mode == "rollback":
            if not args.destination: raise ValueError("missing --destination")
            rollback(data, Path(args.destination))
            print("PASS non-destructive rollback")

if __name__ == "__main__":
    try:
        main()
    except (OSError, ValueError, KeyError, json.JSONDecodeError, subprocess.CalledProcessError, zipfile.BadZipFile) as exc:
        print("FAIL " + str(exc), file=sys.stderr)
        sys.exit(1)
