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
            "shared-gyro-preview.json", "user-preview.json")
FILES = frozenset(("kitreadiness-unity.zip", "receipt.json", *PREVIEWS))


class SourceError(ValueError):
    pass


def sha256(raw):
    return hashlib.sha256(raw).hexdigest()


def no_symlink(path):
    if any(p.is_symlink() for p in (path, *path.parents)):
        raise SourceError("SYMLINK_PATH_REFUSED")


def validate_sources():
    manifest_path = pathlib.Path(__file__).with_name("manifest.json")
    manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    if manifest["package_id"] != "B01-22-KITREADINESS-001" or \
       manifest["source_sha"] != SOURCE_SHA:
        raise SourceError("WRONG_MANIFEST")
    rows = manifest["git_blobs"]
    if len(rows) != 17 or len(set(rows)) != 17:
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
        if value != expected or not (ROOT/name).is_file() or (ROOT/name).is_symlink():
            raise SourceError("MANIFEST_GIT_BLOB_MISMATCH:" + name)
    original = subprocess.run(["git","rev-parse",SOURCE_SHA+":"+CATALOG],
                              cwd=ROOT,check=True,capture_output=True,text=True).stdout.strip()
    now = subprocess.run(["git","rev-parse","HEAD:"+CATALOG],
                         cwd=ROOT,check=True,capture_output=True,text=True).stdout.strip()
    if original != CATALOG_BLOB or now != original:
        raise SourceError("FROZEN_CATALOG_DRIFT")
    cat = json.loads((ROOT/CATALOG).read_text(encoding="utf-8"))
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
    if len(set(previews)) != 4:
        raise SourceError("MISSING_PREVIEW_VARIANTS")
    return previews


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
    previews=read_previews(pathlib.Path(preview_dir))
    zipped=make_zip(files)
    receipt=receipt_for(zipped,files,previews)
    verify_receipt(zipped,files,previews,receipt)
    dest.mkdir(mode=0o700)
    try:
        (dest/"kitreadiness-unity.zip").write_bytes(zipped)
        for k,v in previews.items():(dest/k).write_bytes(v)
        (dest/"receipt.json").write_text(json.dumps(receipt,sort_keys=True,indent=2)+"\n",encoding="utf-8")
    except BaseException:
        # No blind recursive delete. Partially created directory is left inspectable.
        raise
    return receipt


def rollback(dest):
    dest=pathlib.Path(dest)
    no_symlink(dest)
    if not dest.is_dir() or {x.name for x in dest.iterdir()} != FILES or \
       any(not x.is_file() or x.is_symlink() for x in dest.iterdir()):
        raise SourceError("REFUSE_ROLLBACK_FOREIGN_STATE")
    previews=read_previews(dest)
    zipped=(dest/"kitreadiness-unity.zip").read_bytes()
    receipt=json.loads((dest/"receipt.json").read_text(encoding="utf-8"))
    verify_receipt(zipped,physical_unity_files(),previews,receipt)
    for name in FILES:(dest/name).unlink()
    dest.rmdir()
    return "ROLLED_BACK_PREVIEW_ONLY"


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    p=parser.add_mutually_exclusive_group(required=True)
    p.add_argument("--stage")
    p.add_argument("--rollback")
    parser.add_argument("--previews")
    args=parser.parse_args()
    try:
        if args.stage:
            if not args.previews:raise SourceError("PREVIEW_INPUT_REQUIRED")
            rec=stage(args.stage,args.previews)
            print(json.dumps({"result":"B01_22_W11_PHYSICAL_SOURCE_PASS","receipt":rec},sort_keys=True))
        else:
            print(rollback(args.rollback))
    except (SourceError, OSError, ValueError, KeyError, subprocess.CalledProcessError) as e:
        print("FAIL_CLOSED:"+str(e),file=sys.stderr)
        raise SystemExit(2)


if __name__=="__main__":main()
