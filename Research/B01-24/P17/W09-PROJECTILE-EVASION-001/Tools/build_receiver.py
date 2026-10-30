#!/usr/bin/env python3
"""B01-24 W09 original Unity CSharp asset ZIP creator; source-pinned and fail-closed."""
from __future__ import annotations
import argparse
import hashlib
import json
from pathlib import Path
import sys
import zipfile

BASE = Path("Research/B01-24/P17/W09-PROJECTILE-EVASION-001")
DEST = "Assets/B0124ProjectileEvasion"
NAME = "B01-24-PROJECTILE-EVASION-001-unity-source.zip"
STAMP = (2020, 1, 1, 0, 0, 0)

class Rejected(ValueError):
    pass

def blob(data: bytes) -> str:
    return hashlib.sha1(b"blob " + str(len(data)).encode("ascii") + b"\0" + data).hexdigest()

def validate(repo: Path) -> tuple[bytes, bytes, dict]:
    mf = repo / BASE / "manifest.json"
    if mf.is_symlink() or not mf.is_file():
        raise Rejected("Missing source manifest")
    manifest = json.loads(mf.read_text(encoding="utf-8"))
    if manifest.get("package_id") != "B01-24-PROJECTILE-EVASION-001":
        raise Rejected("Wrong package identity")
    pins = manifest.get("files", [])
    if not isinstance(pins, list) or len({e["path"] for e in pins}) != len(pins):
        raise Rejected("Invalid or duplicated source manifest paths")
    for entry in pins:
        p = Path(entry["path"])
        if p.is_absolute() or ".." in p.parts:
            raise Rejected("Path traversal in source manifest")
        full = repo / p
        if full.is_symlink() or not full.is_file():
            raise Rejected("Missing or symlinked source file: " + str(p))
        if blob(full.read_bytes()) != entry["blob"]:
            raise Rejected("SOURCE_DRIFT: " + str(p))
    original = repo / "Assets/R3/R3EnemyArchetype.cs"
    if original.is_symlink() or not original.is_file() or blob(original.read_bytes()) != manifest["original_codex_R3_blob"]:
        raise Rejected("Canonical source R3 ABI drift")
    s = BASE / "ProjectileEvasion.cs"
    m = BASE / "ProjectileEvasion.cs.meta"
    if s.as_posix() not in {x["path"] for x in pins} or m.as_posix() not in {x["path"] for x in pins}:
        raise Rejected("Unpinned Unity file")
    c, meta = (repo / s).read_bytes(), (repo / m).read_bytes()
    if b"guid: 9281e96fcb134097a079e468c2bb0a75" not in meta:
        raise Rejected("Unexpected original Unity GUID")
    if b"public sealed class ProjectileEvasionPlanner" not in c:
        raise Rejected("Original mechanic missing")
    return c, meta, manifest

def build(repo: Path, out: Path) -> dict:
    if out.exists() or out.is_symlink():
        raise Rejected("Output already exists: never overwrite")
    source, meta, manifest = validate(repo.resolve(strict=True))
    entries = {
        DEST + "/ProjectileEvasion.cs": source,
        DEST + "/ProjectileEvasion.cs.meta": meta,
    }
    out.mkdir(parents=True, exist_ok=False)
    archive = out / NAME
    with zipfile.ZipFile(archive, "x", compression=zipfile.ZIP_DEFLATED, compresslevel=9) as z:
        for filename, data in sorted(entries.items()):
            item = zipfile.ZipInfo(filename, date_time=STAMP)
            item.compress_type = zipfile.ZIP_DEFLATED
            item.create_system = 3
            item.external_attr = (0o644 & 0xFFFF) << 16
            z.writestr(item, data, compress_type=zipfile.ZIP_DEFLATED, compresslevel=9)
    with zipfile.ZipFile(archive) as z:
        if set(z.namelist()) != set(entries) or z.testzip() is not None:
            raise Rejected("ZIP entries or CRC incorrect")
        if any(z.read(path) != original for path, original in entries.items()):
            raise Rejected("ZIP contents differ from verified source")
    receipt = {
        "package_id": manifest["package_id"],
        "version": manifest["version"],
        "status": "SOURCE_ZIP_ONLY_UNITY_NATIVE_NOT_RUN",
        "codex_ack": "PENDING",
        "integration": "NOT_INTEGRATED",
        "archive": NAME,
        "archive_sha256": hashlib.sha256(archive.read_bytes()).hexdigest(),
        "source_git_blob": blob(source),
        "meta_git_blob": blob(meta),
        "entries": [
            {"path": p, "sha256": hashlib.sha256(d).hexdigest(), "bytes": len(d)}
            for p, d in sorted(entries.items())
        ],
        "native_unity_scene_or_physics": "NOT_RUN",
        "unpack": "Only after HQ/Codex permission, unpack inner ZIP at Unity project root in disposable checkout, verify no existing files",
        "rollback": "Remove only two new optional imported assets if not locally modified. No canonical game mutations",
    }
    (out / "receiver-receipt.json").write_text(json.dumps(receipt, indent=2, sort_keys=True)+"\n",encoding="utf-8")
    return receipt

if __name__ == "__main__":
    args=argparse.ArgumentParser()
    args.add_argument("--repo",type=Path,default=Path.cwd())
    args.add_argument("--out",type=Path,required=True)
    a=args.parse_args()
    try:
        print(json.dumps(build(a.repo,a.out),sort_keys=True))
    except (ValueError,OSError,KeyError,TypeError) as exc:
        print("REJECTED: "+str(exc),file=sys.stderr)
        sys.exit(2)
