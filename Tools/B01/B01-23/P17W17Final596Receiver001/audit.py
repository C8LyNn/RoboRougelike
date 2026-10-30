#!/usr/bin/env python3
"""B01-23 W17 independent read-only *selected #596* source/receiver preflight.

This NEVER stages, deletes, merges or modifies any existing receiver.
Supply a dedicated immutable #596 Git checkout; do not substitute #604.
"""
import argparse
import hashlib
import importlib.util
import json
import os
import subprocess
from pathlib import Path

SELECTED_SHA = "efb2b06eafedab5469b57f163152d96aca3c5915"
GAME_SHA = "d94400d95d13db7f27e1533df05c9292d5380431"
SAVE_SHA = "33094f154c7b91119b36d58aab285e6e83c5de57"
ALT_604_SHA = "1aa8a59ce1bb4fcd998f0b2b1ce25dfaea333f0a"
CORE_BLOB = "1f9e883c38101f214e4335867faa731f7ae67689"
UNITY_BLOB = "4bac6f327c9b93ae98dd2061c0ca8ea2100aab42"
PACKAGER_BLOB = "f0fb2f5764c83ed011c309c5a48e4a67ad9b6fa7"
ZIP_SHA256 = "005ac910ca9476203bc3ff1748d9198d45835abad21d97e12ad6c1bfe50484e9"
ZIP_SIZE = 81190
ZIP_ENTRIES = 23
SOURCE_COUNT = 22
PACKAGE_PATH = "Tools/B01/B01-23/P17W15UnitySaveBridge001/package.py"

def git(root, *args):
    return subprocess.check_output(["git", "-C", str(root), *args], text=True).strip()

def load_selected(root):
    root = Path(root).resolve()
    if git(root, "rev-parse", "HEAD") != SELECTED_SHA:
        raise ValueError("SELECTED_SHA_MISMATCH_NOT_596")
    if git(root, "rev-parse", GAME_SHA) != GAME_SHA:
        raise ValueError("GAME_510_UNAVAILABLE")
    if git(root, "rev-parse", SAVE_SHA) != SAVE_SHA:
        raise ValueError("SAVE_556_UNAVAILABLE")
    for path, blob in (
        ("Assets/B0123UnitySaveBridge001/Core/AtomicWorldSwap.cs", CORE_BLOB),
        ("Assets/B0123UnitySaveBridge001/Unity/IsolatedTwinRigSaveHost.cs", UNITY_BLOB),
        (PACKAGE_PATH, PACKAGER_BLOB),
    ):
        if git(root, "rev-parse", "HEAD:" + path) != blob:
            raise ValueError("SELECTED_SOURCE_BLOB_MISMATCH_" + path)
    # #604 introduces this alternate checkpoint implementation. The selected
    # #596 source must NOT include it, regardless of whose PR is being checked.
    alt = root / "Assets/B0123UnitySaveBridge001/Core/CheckpointFence.cs"
    if alt.exists() or alt.is_symlink():
        raise ValueError("COMPETING_604_FILE_PRESENT")
    file = root / PACKAGE_PATH
    spec = importlib.util.spec_from_file_location("b0123_selected_596_original_packager", file)
    if spec is None or spec.loader is None:
        raise ValueError("ORIGINAL_PACKAGER_NOT_IMPORTABLE")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    if module.ROOT.resolve() != root:
        raise ValueError("PACKAGER_ROOT_MISMATCH")
    if module.SELECTED_SAVE != SAVE_SHA or module.SELECTED_GAME != GAME_SHA:
        raise ValueError("WRONG_GAME_OR_SAVE_SCHEMA")
    return module

def selected_archive(root):
    p = load_selected(root)
    source_paths = p.scope()
    if len(source_paths) != SOURCE_COUNT:
        raise ValueError("SOURCE_SCOPE_MISMATCH")
    raw = p.build()
    if len(raw) != ZIP_SIZE or hashlib.sha256(raw).hexdigest() != ZIP_SHA256:
        raise ValueError("PHYSICAL_INNER_ZIP_IDENTITY_MISMATCH")
    files = p.verify(raw)
    if len(files) != ZIP_ENTRIES or set(files) != set(source_paths) | {p.MANIFEST}:
        raise ValueError("MANIFEST_ORIGINAL_MEMBER_MISMATCH")
    manifest = json.loads(files[p.MANIFEST])
    if (manifest["own_head_sha"] != SELECTED_SHA
        or manifest["native_unity"] != "NOT_RUN"
        or manifest["canonical_510_live_import"] != "HOLD"
        or manifest["codex_ack"] is not False
        or manifest["unity_guid_count"] != 10
        or manifest["asmdef_count"] != 3
        or manifest["version"] != "2.0.0"):
        raise ValueError("FORGED_ACCEPTANCE_OR_SOURCE_RECEIPT")
    return raw, files, manifest

def scan_exclusive_receiver(receiver, files):
    """READ ONLY. The receiver must be an exclusive package scratch, NOT a
    shared Unity project. Never run destructive rollback on a shared tree."""
    root = Path(receiver).absolute()
    if root.is_symlink() or not root.is_dir():
        raise ValueError("RECEIVER_MISSING_OR_SYMLINK")
    allowed_dirs = set()
    for name in files:
        for parent in Path(name).parents:
            if str(parent) != ".":
                allowed_dirs.add(parent.as_posix())
    observed = set()
    for path in root.rglob("*"):
        rel = path.relative_to(root).as_posix()
        if path.is_symlink():
            raise ValueError("SYMLINK_INSIDE_RECEIVER:" + rel)
        if path.is_dir():
            if rel not in allowed_dirs:
                raise ValueError("FOREIGN_EMPTY_OR_NONEMPTY_DIRECTORY:" + rel)
        elif path.is_file():
            if rel not in files:
                raise ValueError("FOREIGN_OR_COMPETING_FILE:" + rel)
            if path.read_bytes() != files[rel]:
                raise ValueError("DIRTY_OR_ALTERED_SELECTED_BYTES:" + rel)
            observed.add(rel)
        else:
            raise ValueError("UNSUPPORTED_INODE:" + rel)
    if observed != set(files):
        raise ValueError("MISSING_ORIGINAL_SELECTED_FILES")
    return len(observed)

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--selected", required=True,
                    help="immutable ORIGINAL #596 checkout at exact commit; never #604")
    ap.add_argument("--receipt", help="new file outside selected checkout for results")
    ap.add_argument("--zip", help="new output ORIGINAL selected inner ZIP")
    ap.add_argument("--receiver", help="optional already-existing exclusive scratch receiver; read-only")
    args = ap.parse_args()
    raw, files, manifest = selected_archive(args.selected)
    if args.receiver:
        scan_exclusive_receiver(args.receiver, files)
    receipt = {
        "selection": "B01-23-UNITY-SAVE-HOST-BRIDGE-001/2.0.0",
        "selected_producer_pr": 596, "selected_sha": SELECTED_SHA,
        "alternative_604": "ALT_HOLD", "alternative_sha": ALT_604_SHA,
        "game_sha": GAME_SHA, "save_sha": SAVE_SHA,
        "original_inner_zip_bytes": len(raw),
        "original_inner_zip_entries": len(files),
        "original_inner_zip_sha256": hashlib.sha256(raw).hexdigest(),
        "source_files": SOURCE_COUNT, "unity_guid_count": 10, "asmdefs": 3,
        "own_host_source": "SELECTED_ORIGINAL_MATCH_NOT_INDEPENDENT_G0",
        "native_unity": "NOT_RUN", "canonical_510_live_import": "HOLD",
        "codex_ack": False, "g2": "HOLD", "independent_g0": "PENDING",
    }
    if args.zip:
        output = Path(args.zip)
        if output.exists() or output.is_symlink():
            raise ValueError("REFUSE_ARCHIVE_OVERWRITE")
        output.parent.mkdir(parents=True, exist_ok=True)
        output.write_bytes(raw)
    if args.receipt:
        path = Path(args.receipt)
        if path.exists() or path.is_symlink():
            raise ValueError("REFUSE_RECEIPT_OVERWRITE")
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(json.dumps(receipt, indent=2, sort_keys=True) + "\n")
    print("B01_23_W17_SELECTED_596_ORIGINAL_SOURCE_RECEIVER_PREFLIGHT_PASS_NOT_G0")
    print(json.dumps(receipt, sort_keys=True))

if __name__ == "__main__":
    main()
