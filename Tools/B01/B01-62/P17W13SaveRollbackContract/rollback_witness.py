#!/usr/bin/env python3
"""B01-62 W13: execute original immutable #510 package.rollback RED, then receiver guard GREEN.

Exercises ONLY a newly created disposable Git worktree & temp directories, never Codex.
"""
import argparse
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import shutil
import subprocess
import tempfile
import zipfile

SELECTED="d94400d95d13db7f27e1533df05c9292d5380431"
EXPECTED_ZIP="d4ddfa3528ef0416aefffca72d884000f3b0c9959fb8dff53096487ef95c9387"
PACKAGER="Tools/B01/B01-23/P17W10SecureReattachV2/package.py"
class Refuse(ValueError):pass

def strict_rollback(pkg,raw,target):
    """Reject unowned files AND *directories* including empty dirs; no blanket recursive deletion."""
    target=Path(target)
    if target.is_symlink() or not target.is_dir():raise Refuse("MISSING_OR_SYMLINKED_ROOT")
    original=pkg.verify(raw)
    allowed_files=set(original)
    allowed_dirs=set()
    for name in allowed_files:
        p=Path(name)
        for parent in p.parents:
            if str(parent)!=".":allowed_dirs.add(parent.as_posix())
    seen_files=set()
    for base,dirs,files in os.walk(target,topdown=True,followlinks=False):
        for n in dirs:
            f=Path(base)/n
            if f.is_symlink():raise Refuse("UNTRUSTED_DIRECTORY_SYMLINK")
            relative=f.relative_to(target).as_posix()
            if relative not in allowed_dirs:raise Refuse("UNOWNED_DIRECTORY:"+relative)
        for n in files:
            f=Path(base)/n
            if f.is_symlink():raise Refuse("UNTRUSTED_FILE_SYMLINK")
            relative=f.relative_to(target).as_posix()
            if relative not in allowed_files or f.read_bytes()!=original[relative]:
                raise Refuse("UNOWNED_OR_MODIFIED_FILE:"+relative)
            seen_files.add(relative)
    if seen_files!=allowed_files:raise Refuse("MISSING_ORIGINAL_FILE")
    # Explicit preflight, not a secure transactional lock against concurrent hostile writes:
    # receiver must hold an exclusive target ownership lock through removal.
    pkg.rollback(raw,target)

def _run(repo,workspace):
    worktree=workspace/"frozen-owner"
    subprocess.run(["git","-C",str(repo),"worktree","add","--detach",str(worktree),SELECTED],
                   check=True,stdout=subprocess.PIPE,stderr=subprocess.PIPE)
    try:
        spec=importlib.util.spec_from_file_location("original_frozen_510",worktree/PACKAGER)
        pkg=importlib.util.module_from_spec(spec);spec.loader.exec_module(pkg)
        raw=pkg.build()
        expected=hashlib.sha256(raw).hexdigest()
        if len(raw)!=99666 or expected!=EXPECTED_ZIP:raise Refuse("FROZEN_PHYSICAL_ZIP_DRIFT")
        with zipfile.ZipFile(__import__("io").BytesIO(raw)) as z:
            if len(z.infolist())!=35:raise Refuse("FROZEN_MEMBERS_DRIFT")
        # Real original rollback silently DELETES an alien empty directory.
        victim=workspace/"original-stage"
        pkg.stage(raw,victim)
        stranger=victim/"alien-empty-do-not-delete"
        stranger.mkdir()
        pkg.rollback(raw,victim)
        if victim.exists() or stranger.exists(): # failure to prove unsafe original deletion
            raise Refuse("EXPECTED_ORIGINAL_EMPTY_DIR_DELETE_NOT_REPRODUCED")
        # Same actual original packager/stage, but independent new strict preflight blocks deletion.
        guarded=workspace/"strict-stage"
        pkg.stage(raw,guarded)
        other=guarded/"alien-empty-do-not-delete"
        other.mkdir()
        refused=False
        try:strict_rollback(pkg,raw,guarded)
        except Refuse as e:refused="UNOWNED_DIRECTORY" in str(e)
        if not refused or not other.is_dir():raise Refuse("GUARD_FAILED_TO_PRESERVE_FOREIGN_EMPTY_DIR")
        other.rmdir()
        strict_rollback(pkg,raw,guarded)
        if guarded.exists():raise Refuse("CLEAN_ROLLBACK_FAILED")
        # Conflicting directory exists under an *authorized* parent; still unowned.
        nested=workspace/"nested-stage"
        pkg.stage(raw,nested)
        alien=nested/"Assets/B0123SecureReattachV2/alien-subdir"
        alien.mkdir(parents=True)
        try:strict_rollback(pkg,raw,nested)
        except Refuse:pass
        else:raise Refuse("NESTED_FOREIGN_DIRECTORY_NOT_REFUSED")
        if not alien.exists():raise Refuse("NESTED_ALIEN_WAS_DELETED")
        # foreign directory preservation also implies foreign file preservation
        (alien/"keep.txt").write_text("do not delete",encoding="utf-8")
        try:strict_rollback(pkg,raw,nested)
        except Refuse:pass
        else:raise Refuse("FOREIGN_FILE_NOT_REFUSED")
        if (alien/"keep.txt").read_text(encoding="utf-8")!="do not delete":
            raise Refuse("FOREIGN_FILE_CORRUPTED")
        (alien/"keep.txt").unlink();alien.rmdir()
        strict_rollback(pkg,raw,nested)
        return {"schema":"b01-62-p17w13-rollback-original-witness-v1",
                "selected_sha":SELECTED,"original_zip_sha256":expected,"original_zip_size":len(raw),
                "original_members":35,"original_rollback_foreign_empty_directory":"RED_CONFIRMED_DELETED",
                "strict_rollback_foreign_empty_directory":"PASS_PRESERVED",
                "strict_rollback_nested_empty_directory":"PASS_PRESERVED",
                "strict_rollback_foreign_file":"PASS_PRESERVED",
                "strict_rollback_clean":"PASS","native_unity":"NOT_RUN","real_receiver":"NOT_TOUCHED"}
    finally:
        subprocess.run(["git","-C",str(repo),"worktree","remove","--force",str(worktree)],
                       check=False,stdout=subprocess.PIPE,stderr=subprocess.PIPE)

def execute(repo):
    with tempfile.TemporaryDirectory(prefix="b01_62_w13_external_") as tmp:
        return _run(Path(repo).resolve(),Path(tmp))

def main():
    ap=argparse.ArgumentParser()
    ap.add_argument("--repo",required=True)
    ap.add_argument("--output",required=True)
    args=ap.parse_args()
    path=Path(args.output)
    if path.exists() or not path.parent.is_dir():raise Refuse("OUTPUT_MUST_BE_NEW")
    result=execute(args.repo)
    raw=(json.dumps(result,sort_keys=True,indent=2)+"\n").encode()
    path.write_bytes(raw)
    print("ORIGINAL_FROZEN_ROLLBACK_EMPTY_DIR=RED_CONFIRMED")
    print("ISOLATED_STRICT_RECEIVER_GUARD=PASS")
    print("RECEIPT_SHA256="+hashlib.sha256(raw).hexdigest())
    print("NATIVE_UNITY=NOT_RUN")

if __name__=="__main__":main()
