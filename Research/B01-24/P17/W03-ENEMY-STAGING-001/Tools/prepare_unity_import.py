#!/usr/bin/env python3
"""B01-24 W03: exact-source-pinned, deterministic Unity Editor asset ZIP.

Produces just two Unity assets (a C# Editor tool and its tracked .meta).
It does not invoke Unity, modify the source checkout or claim playable scenes.
Fail closed on changed producer data, canonical arena, owned source, or destination.
"""
from __future__ import annotations
import argparse
import hashlib
import json
from pathlib import Path
import sys
import zipfile

ROOT = Path("Research/B01-24/P17/W03-ENEMY-STAGING-001")
EDITOR = ROOT / "Unity/Editor/B0124EnemyStageBuilder.cs"
META = ROOT / "Unity/Editor/B0124EnemyStageBuilder.cs.meta"
MANIFEST = ROOT / "manifest.json"
DEST = "Assets/Editor/B0124EnemyStagingW03"
STAMP = (2020, 1, 1, 0, 0, 0)


class UnsafePackage(ValueError):
    pass


def git_blob(raw: bytes) -> str:
    return hashlib.sha1(b"blob " + str(len(raw)).encode("ascii") + b"\0" + raw).hexdigest()


def verified_bytes(root: Path, relative: str, expected: str) -> bytes:
    candidate = root / relative
    if candidate.is_symlink() or not candidate.is_file():
        raise UnsafePackage("Missing or symlink source: " + relative)
    data = candidate.read_bytes()
    actual = git_blob(data)
    if actual != expected:
        raise UnsafePackage("SOURCE_DRIFT " + relative + " expected " + expected + " got " + actual)
    return data


def source_inputs(repo: Path) -> tuple[dict, bytes, bytes]:
    manifest_path = repo / MANIFEST
    if manifest_path.is_symlink() or not manifest_path.is_file():
        raise UnsafePackage("Missing or symlinked W03 manifest")
    manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    if manifest.get("package_id") != "B01-24-ENEMY-STAGING-001" or manifest.get("version") != "1.0.0":
        raise UnsafePackage("Unexpected W03 manifest identity")
    pins = {row["path"]: row["blob"] for row in manifest["files"]}
    if len(pins) != len(manifest["files"]):
        raise UnsafePackage("Duplicate or ambiguous manifest file path")
    if EDITOR.as_posix() not in pins or META.as_posix() not in pins:
        raise UnsafePackage("Manifest missing original editor source and meta")
    dep = manifest["dependency"]
    if dep["producer_SHA"] != "c39a400cb07ae0fd6f0d8d419a128490cfbcfd8a":
        raise UnsafePackage("Different parent producer; requires independent QA")
    verified_bytes(repo, dep["source_file"], dep["blob"])
    verified_bytes(repo, dep["source_arena"], dep["arena_blob"])
    # All seven original owner inputs must match, not just the two ZIP entries.
    for path, sha in sorted(pins.items()):
        verified_bytes(repo, path, sha)
    editor, meta = verified_bytes(repo, EDITOR.as_posix(), pins[EDITOR.as_posix()]), verified_bytes(repo, META.as_posix(), pins[META.as_posix()])
    if "guid: 7a85dd4f01bb4e6ca241c78bdec78d24" not in meta.decode("utf-8"):
        raise UnsafePackage("Missing required stable original .meta GUID")
    if "NewSceneMode.Additive" not in editor.decode("utf-8") or "File.Exists(path)" not in editor.decode("utf-8"):
        raise UnsafePackage("Unsafe or unexpectedly rewritten original scene generator")
    return manifest, editor, meta


def package(repo: Path, out: Path) -> dict:
    repo = repo.resolve(strict=True)
    manifest, editor, meta = source_inputs(repo)
    if out.exists() or out.is_symlink():
        raise UnsafePackage("Refusing to overwrite existing output directory: " + str(out))
    # Check all inputs BEFORE making the destination.
    entries = {
        DEST + "/B0124EnemyStageBuilder.cs": editor,
        DEST + "/B0124EnemyStageBuilder.cs.meta": meta,
    }
    out.mkdir(parents=True, exist_ok=False)
    zip_path = out / "B01-24-ENEMY-STAGING-001-unity-editor.zip"
    with zipfile.ZipFile(zip_path, mode="x", compression=zipfile.ZIP_DEFLATED, compresslevel=9) as archive:
        for name, data in sorted(entries.items()):
            info = zipfile.ZipInfo(name, date_time=STAMP)
            info.compress_type = zipfile.ZIP_DEFLATED
            info.external_attr = (0o644 & 0xFFFF) << 16
            info.create_system = 3
            archive.writestr(info, data, compress_type=zipfile.ZIP_DEFLATED, compresslevel=9)
    with zipfile.ZipFile(zip_path) as archive:
        if set(archive.namelist()) != set(entries) or archive.testzip() is not None:
            raise UnsafePackage("ZIP payload failed strict entry and CRC verification")
        for name, contents in entries.items():
            if archive.read(name) != contents:
                raise UnsafePackage("ZIP payload differs from owned original editor source: " + name)
    receipt = {
        "package": "B01-24-ENEMY-STAGING-001",
        "kind": "SOURCE_PINNED_UNITY_EDITOR_IMPORT_ZIP_NOT_NATIVE_EDITOR_RUN",
        "producer_PR": 387,
        "dependency_PR": 355,
        "dependency_commit": manifest["dependency"]["producer_SHA"],
        "source_arena_git_blob": manifest["dependency"]["arena_blob"],
        "original_editor_git_blob": git_blob(editor),
        "original_meta_git_blob": git_blob(meta),
        "zip_name": zip_path.name,
        "zip_sha256": hashlib.sha256(zip_path.read_bytes()).hexdigest(),
        "entries": [{"path": n, "size": len(data), "sha256": hashlib.sha256(data).hexdigest()}
                    for n, data in sorted(entries.items())],
        "unity_native_import": "NOT_RUN",
        "unity_scene_generation": "NOT_RUN",
        "integration": "NOT_INTEGRATED",
        "install": "Extract only into disposable Unity project root after Codex approval; preserves existing source checkout.",
        "rollback": "Remove only freshly extracted files after checking locally edited content; never delete existing scenes.",
    }
    (out / "receiver-receipt.json").write_text(
        json.dumps(receipt, indent=2, sort_keys=True) + "\n", encoding="utf-8"
    )
    return receipt


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--repo-root", type=Path, default=Path.cwd())
    ap.add_argument("--out-dir", type=Path, required=True)
    args = ap.parse_args()
    try:
        print(json.dumps(package(args.repo_root, args.out_dir), sort_keys=True))
        return 0
    except (UnsafePackage, OSError, KeyError, ValueError) as error:
        print("REJECTED " + str(error), file=sys.stderr)
        return 2


if __name__ == "__main__":
    sys.exit(main())
