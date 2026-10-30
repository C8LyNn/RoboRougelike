#!/usr/bin/env python3
"""B01-22 KITREADINESS-001 physical producer: exact Unity C# + JSON preview artifacts.
No game directory writes. Fails closed on absent/unexpected/modified outputs.
"""
import argparse
import hashlib
import io
import json
import pathlib
import subprocess
import sys
import zipfile

ROOT = pathlib.Path(__file__).resolve().parents[4]
SOURCE_SHA = "b1d4e341d846c9397e236809a1d7e4c4fff9c935"
CATALOG = "Research/B01-22/P17/EQUIPMENT-KITS-001/catalog.json"
CATALOG_BLOB = "372b3bc74553ea459e89be38378f4429090e00a2"
ASSETS = tuple(sorted((
    "Assets/B01KitReadiness001.meta",
    "Assets/B01KitReadiness001/KitReadinessPlanner.cs",
    "Assets/B01KitReadiness001/KitReadinessPlanner.cs.meta",
)))
PREVIEWS = ("starter-cache-preview.json", "siege-cache-preview.json",
            "shared-gyro-preview.json", "user-preview.json", "priority-preview.json")
FILES = frozenset(("kitreadiness-unity.zip", "receipt.json", *PREVIEWS))


class SourceError(ValueError):
    pass


def sha256(raw):
    return hashlib.sha256(raw).hexdigest()


def git_blob_oid(raw):
    # Git's EXACT canonical blob identity, not hash of the file alone.
    # The manifest's SHA1 refers to Git object header + raw on-disk bytes.
    if not isinstance(raw, bytes):
        raise SourceError("INVALID_SOURCE_BYTES")
    return hashlib.sha1(b"blob " + str(len(raw)).encode("ascii") + b"\0" + raw).hexdigest()


def no_symlink(path):
    if any(p.is_symlink() for p in (path, *path.parents)):
        raise SourceError("SYMLINK_PATH_REFUSED")


def validate_sources():
    manifest_path = pathlib.Path(__file__).with_name("manifest.json")
    no_symlink(manifest_path)
    original_bytes = manifest_path.read_bytes()
    expected_blob = subprocess.run(
        ["git", "rev-parse", "HEAD:" + manifest_path.relative_to(ROOT).as_posix()],
        cwd=ROOT, check=True, capture_output=True, text=True).stdout.strip()
    if git_blob_oid(original_bytes) != expected_blob:
        raise SourceError("DIRTY_OR_UNAPPROVED_MANIFEST")
    manifest = json.loads(original_bytes.decode("utf-8"))
    if manifest["package_id"] != "B01-22-KITREADINESS-001" or \
       manifest["source_sha"] != SOURCE_SHA:
        raise SourceError("WRONG_MANIFEST")
    rows = manifest["git_blobs"]
    if len(rows) != 19 or len(set(rows)) != 19:
        raise SourceError("BAD_MANIFEST_MEMBER_COUNT")
    for name, expected in rows.items():
        if not (name.startswith("Assets/B01KitReadiness001") or
                name.startswith("Research/B01-22/P17/KITREADINESS001/") or
                name == ".github/workflows/b01-22-p17w08-kitreadiness.yml"):
            raise SourceError("FOREIGN_OR_UNSAFE_SOURCE")
        if pathlib.PurePosixPath(name).is_absolute() or ".." in pathlib.PurePosixPath(name).parts:
            raise SourceError("UNSAFE_SOURCE_PATH")
        value = subprocess.run(["git", "rev-parse", "HEAD:" + name], cwd=ROOT,
                               check=True, capture_output=True, text=True).stdout.strip()
        physical = ROOT/name
        if value != expected or not physical.is_file() or physical.is_symlink() or \
           git_blob_oid(physical.read_bytes()) != expected:
            raise SourceError("MANIFEST_GIT_BLOB_MISMATCH:" + name)
    original = subprocess.run(["git","rev-parse",SOURCE_SHA+":"+CATALOG],
                              cwd=ROOT,check=True,capture_output=True,text=True).stdout.strip()
    now = subprocess.run(["git","rev-parse","HEAD:"+CATALOG],
                         cwd=ROOT,check=True,capture_output=True,text=True).stdout.strip()
    catalog_file = ROOT/CATALOG
    if original != CATALOG_BLOB or now != original or \
       not catalog_file.is_file() or catalog_file.is_symlink() or \
       git_blob_oid(catalog_file.read_bytes()) != CATALOG_BLOB:
        raise SourceError("FROZEN_CATALOG_DRIFT")
    cat = json.loads(catalog_file.read_text(encoding="utf-8"))
    if cat["version"] != "1.0.3" or len(cat["kits"]) != 10 or \
       cat["provenance"]["codex_sha"] != "203a0805251ed28293888e43f2d49f0df63767c6":
        raise SourceError("ORIGINAL_CATALOG_VERSION_DRIFT")
    return len(rows)


def physical_unity_files(root=ROOT):
    files = {}
    for name in ASSETS:
        p=root/name
        if p.is_symlink() or not p.is_file():
            raise SourceError("UNITY_ASSET_NOT_PRESENT:" + name)
        files[name] = p.read_bytes()
    return files


def make_zip(files):
    if set(files) != set(ASSETS):
        raise SourceError("UNEXPECTED_ASSETS")
    out = io.BytesIO()
    with zipfile.ZipFile(out, "w", compression=zipfile.ZIP_DEFLATED, compresslevel=9) as z:
        for name in ASSETS:
            meta=zipfile.ZipInfo(name, date_time=(2020,1,1,0,0,0))
            meta.create_system=3
            meta.external_attr=0o100644<<16
            meta.compress_type=zipfile.ZIP_DEFLATED
            z.writestr(meta,files[name],compress_type=zipfile.ZIP_DEFLATED,compresslevel=9)
    return out.getvalue()


def verify_zip(zipped, files):
    with zipfile.ZipFile(io.BytesIO(zipped)) as z:
        if z.namelist() != list(ASSETS) or z.testzip() is not None:
            raise SourceError("ZIP_MISMATCH")
        for name in ASSETS:
            if z.read(name) != files[name]:
                raise SourceError("UNAUTHORIZED_ASSET_BYTES")
    return True


def read_previews(path):
    previews={}
    for name in PREVIEWS:
        current=path/name
        if current.is_symlink() or not current.is_file():
            raise SourceError("MISSING_PREVIEW:"+name)
        data=current.read_bytes()
        if b"\r" in data or not data.endswith(b"\n"):
            raise SourceError("NONCANONICAL_PREVIEW_LINE_ENDINGS:"+name)
        parsed=json.loads(data)
        if parsed["schema"] != "b01-kitreadiness-preview-v1" or \
           parsed["source_sha"] != SOURCE_SHA or parsed["reference_only"] is not True or \
           parsed["modifies_game"] is not False or \
           len(parsed["result"]["Requests"]) < 1:
            raise SourceError("INVALID_PREVIEW_SOURCE:"+name)
        previews[name]=data
    if len(set(previews)) != 5:
        raise SourceError("MISSING_PREVIEW_VARIANTS")
    return previews


def verify_approved_physical(zipped, previews):
    """Require actual C# preview bytes to match original approved Git manifest."""
    approved = json.loads(pathlib.Path(__file__).with_name(
        "manifest.json").read_text(encoding="utf-8")).get("approved_physical_sha256")
    received = {"kitreadiness-unity.zip": zipped, **previews}
    if not isinstance(approved, dict) or set(approved) != set(received):
        raise SourceError("MISSING_APPROVED_PHYSICAL_PINS")
    for name, raw in received.items():
        if approved[name] != {"sha256": sha256(raw), "bytes": len(raw)}:
            raise SourceError("UNAPPROVED_PHYSICAL_PAYLOAD:" + name)
    return True


def receipt_for(zipped, files, previews):
    hashes = {"kitreadiness-unity.zip":{"sha256":sha256(zipped),"bytes":len(zipped)}}
    hashes.update({k:{"sha256":sha256(v),"bytes":len(v)} for k,v in previews.items()})
    return {"schema":"b01-22-kitreadiness-physical-v1",
            "source_sha":SOURCE_SHA,"original_catalog_git_blob":CATALOG_BLOB,
            "no_game_writes":True,"unity_assets":
            {k:{"sha256":sha256(v),"bytes":len(v)} for k,v in files.items()},
            "files":hashes}


def verify_receipt(zipped, files, previews, receipt):
    if receipt.get("schema") != "b01-22-kitreadiness-physical-v1" or \
       receipt.get("source_sha") != SOURCE_SHA or \
       receipt.get("original_catalog_git_blob") != CATALOG_BLOB or \
       receipt.get("no_game_writes") is not True or \
       receipt.get("unity_assets") != {
           k:{"sha256":sha256(v),"bytes":len(v)} for k,v in files.items()}:
        raise SourceError("RECEIPT_INVALID")
    verify_zip(zipped, files)
    if receipt.get("files") != receipt_for(zipped,files,previews)["files"]:
        raise SourceError("RECEIPT_HASH_MISMATCH")
    return True


def stage(dest, preview_dir, source_check=True):
    dest=pathlib.Path(dest)
    no_symlink(dest)
    if dest.exists() or not dest.parent.is_dir():
        raise SourceError("DEST_EXISTS_OR_PARENT_MISSING")
    if source_check:validate_sources()
    files=physical_unity_files()
    # Re-check the actual archived snapshot after the initial source gate.
    # A mutation between validate_sources() and this read must not enter ZIP.
    approved = json.loads(pathlib.Path(__file__).with_name("manifest.json").read_text(encoding="utf-8"))["git_blobs"]
    if any(git_blob_oid(files[name]) != approved[name] for name in ASSETS):
        raise SourceError("UNITY_ASSET_CHANGED_BEFORE_PACKAGE")
    previews=read_previews(pathlib.Path(preview_dir))
    zipped=make_zip(files)
    receipt=receipt_for(zipped,files,previews)
    verify_receipt(zipped,files,previews,receipt)
    if source_check: verify_approved_physical(zipped,previews)
    dest.mkdir(mode=0o700)
    try:
        (dest/"kitreadiness-unity.zip").write_bytes(zipped)
        for k,v in previews.items():(dest/k).write_bytes(v)
        (dest/"receipt.json").write_text(json.dumps(receipt,sort_keys=True,indent=2)+"\n",encoding="utf-8")
    except BaseException:
        # No blind recursive delete. Partially created directory is left inspectable.
        raise
    return receipt


def owned_rollback_snapshot(dest):
    # Reconstruct complete direct-child state every time. A foreign EMPTY
    # directory is a foreign owner even though it contributes zero file bytes.
    no_symlink(dest)
    if not dest.is_dir():
        raise SourceError("REFUSE_ROLLBACK_FOREIGN_STATE")
    children=list(dest.iterdir())
    if {p.name for p in children} != FILES or \
       any(not p.is_file() or p.is_symlink() for p in children):
        raise SourceError("REFUSE_ROLLBACK_FOREIGN_STATE")
    # Hashing alone would be insufficient if the producer changed one byte.
    # Save real bytes so we can compare all seven files after receipt parsing.
    return {name:(dest/name).read_bytes() for name in sorted(FILES)}



def verify_stage(dest):
    """Read-only source and physical-receipt acceptance; NEVER unlink anything.

    Require an unmodified original checkout and frozen #411 catalog. Then
    inspect ALL 7 files and byte-level receipt under a stable dual snapshot.
    A changed receiver or an unowned *empty* directory fails closed.
    """
    dest=pathlib.Path(dest)
    validate_sources()
    before=owned_rollback_snapshot(dest)
    previews=read_previews(dest)
    zipped=(dest/"kitreadiness-unity.zip").read_bytes()
    receipt=json.loads((dest/"receipt.json").read_text(encoding="utf-8"))
    verify_receipt(zipped, physical_unity_files(), previews, receipt)
    verify_approved_physical(zipped, previews)
    if owned_rollback_snapshot(dest) != before:
        raise SourceError("VERIFY_STAGE_CHANGED_DURING_READ")
    return receipt


def rollback(dest):
    dest=pathlib.Path(dest)
    before=owned_rollback_snapshot(dest)
    previews=read_previews(dest)
    zipped=(dest/"kitreadiness-unity.zip").read_bytes()
    receipt=json.loads((dest/"receipt.json").read_text(encoding="utf-8"))
    verify_receipt(zipped,physical_unity_files(),previews,receipt)
    # Recheck after all validation callbacks and source read operations, not
    # merely before them. The previous version unlinked its files first and
    # discovered a newly added foreign empty directory only at rmdir().
    # This guards a changing receiver state during preflight. Exclusive
    # receiver ownership is still required during the final deletion phase.
    if owned_rollback_snapshot(dest) != before:
        raise SourceError("REFUSE_ROLLBACK_CHANGED_AFTER_VERIFY")
    for name in sorted(FILES):
        (dest/name).unlink()
    dest.rmdir()
    return "ROLLED_BACK_PREVIEW_ONLY"


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    p=parser.add_mutually_exclusive_group(required=True)
    p.add_argument("--stage")
    p.add_argument("--rollback")
    p.add_argument("--verify-stage")
    parser.add_argument("--previews")
    args=parser.parse_args()
    try:
        if args.stage:
            if not args.previews:raise SourceError("PREVIEW_INPUT_REQUIRED")
            rec=stage(args.stage,args.previews)
            print(json.dumps({"result":"B01_22_W16_PHYSICAL_SOURCE_PASS","receipt":rec},sort_keys=True))
        elif args.verify_stage:
            if args.previews:raise SourceError("PREVIEWS_NOT_ALLOWED_FOR_READONLY_VERIFY")
            rec=verify_stage(args.verify_stage)
            print(json.dumps({"result":"VERIFIED_STAGE_READ_ONLY",
                              "source_sha":SOURCE_SHA,
                              "verified_members":sorted(FILES),
                              "receipt":rec},sort_keys=True))
        else:
            if args.previews:raise SourceError("PREVIEWS_NOT_ALLOWED_FOR_ROLLBACK")
            print(rollback(args.rollback))
    except (SourceError, OSError, ValueError, KeyError, subprocess.CalledProcessError) as e:
        print("FAIL_CLOSED:"+str(e),file=sys.stderr)
        raise SystemExit(2)


if __name__=="__main__":main()
