#!/usr/bin/env python3
"""B01-23 P17 W03: immutable-source Unity TextAsset archive, opt-in isolated stage, safe rollback.

This never writes into Codex's Unity project. Only B01-00/Codex can authorize real installation.
Python 3.11+, stdlib only. EXIT NONZERO on any mismatch.
"""
import argparse
import hashlib
import json
from pathlib import Path
import shutil
import sys
import zipfile

ROOT_MANIFEST="Docs/B01/PARTSIZE001/P17_W02_BRACING_MANIFEST.json"
ROOT_MANIFEST_BLOB="02221b524d2c106e90d179f16c09dcfef8815c6d"
SOURCE_COMMIT="5666a39eabd4cea91b2b605a9eb99ea2f1e226e8"
DEPENDENCY_PR=[286,348,361,375]
RECEIPT="P17_BRACING_ARCHIVE_RECEIPT.json"
MARKER="_B01_23_P17_ISOLATED_STAGE.json"
STAMP=(2020,1,1,0,0,0)
MAX_FILE=128*1024
EXPECTED={
"Assets/PARTSIZE001/Bracing.meta":"4c55946b3623dc5eeb003267120b54c70ef81adc",
"Assets/PARTSIZE001/Bracing/P17.meta":"041721c8ca141eccde7a6c8488b27bfc5db423ac",
"Assets/PARTSIZE001/Bracing/P17/P17_Bulwark_Reinforced.json":"8b00e5da047a22bc23644639e3292c447dd6b335",
"Assets/PARTSIZE001/Bracing/P17/P17_Bulwark_Reinforced.json.meta":"db00bb565ca0e60d237caa0a2719357ecf2c62de",
"Assets/PARTSIZE001/Bracing/P17/P17_Flanker_Reinforced.json":"7259e3454b2f7dd72ca66de83673c143075a807f",
"Assets/PARTSIZE001/Bracing/P17/P17_Flanker_Reinforced.json.meta":"355d74ba783df457d176cb62acf980b5886f1ff4",
"Assets/PARTSIZE001/Bracing/P17/P17_Skirmisher_Reinforced.json":"edf332a66645c5ea4374b1957d2cc1a7d356e4d2",
"Assets/PARTSIZE001/Bracing/P17/P17_Skirmisher_Reinforced.json.meta":"ff7a9e2a567840a020b1dceec42d7ee651b62d23",
"Assets/PARTSIZE001/Bracing/P17/P17_StructuralBracingKits.json":"9b3d2403e7f6e32ea6e20f9f5bb0a83b859bc6e1",
"Assets/PARTSIZE001/Bracing/P17/P17_StructuralBracingKits.json.meta":"1a195e371c3106b2e059781248b0a50c8963bb92",
}

def ensure(ok,code):
    if not ok:raise ValueError(code)

def sha(data):return hashlib.sha256(data).hexdigest()

def gitblob(data):
    return hashlib.sha1(b"blob "+str(len(data)).encode()+b"\0"+data).hexdigest()

def canon(obj):
    return (json.dumps(obj,sort_keys=True,separators=(",",":"),ensure_ascii=False)+"\n").encode("utf-8")

def safe_name(name):
    p=Path(name)
    return (not p.is_absolute() and len(p.parts)>0 and
            all(part not in ("", ".", "..") for part in p.parts) and
            "\\" not in name and ":" not in name and
            not name.endswith("/"))

def source_files(root):
    root=root.resolve(strict=True)
    manifest_path=root/ROOT_MANIFEST
    raw=manifest_path.read_bytes()
    ensure(gitblob(raw)==ROOT_MANIFEST_BLOB,"FROZEN_MANIFEST_DRIFT")
    manifest=json.loads(raw)
    ensure(manifest.get("package_id")=="PARTSIZE001-P17-W02-STRUCTURAL-BRACING-R1",
           "WRONG_SOURCE_PACKAGE")
    pins={f["path"]:f["git_blob_sha1"] for f in manifest["original_files"]}
    ensure(len(pins)==len(manifest["original_files"]),"DUPLICATE_MANIFEST_PATH")
    ensure(all(pins.get(k)==v for k,v in EXPECTED.items()),"ORIGINAL_GIT_BLOB_PINS_DIFFER")
    files={}
    for name,blob in sorted(EXPECTED.items()):
        ensure(safe_name(name),"UNSAFE_OWNED_SOURCE_PATH")
        p=root/name
        ensure(p.is_file() and not p.is_symlink(),"OWNED_SOURCE_MISSING_OR_SYMLINK:"+name)
        content=p.read_bytes()
        ensure(0<len(content)<=MAX_FILE,"OWNED_SOURCE_SIZE_INVALID:"+name)
        ensure(gitblob(content)==blob,"OWNED_ORIGINAL_CONTENT_CHANGED:"+name)
        files[name]=content
    return files

def receipt(files):
    return {
        "schema":"B01-P17-W03-BRACING-PORTABLE-ARCHIVE-v1",
        "producer":"B01-23","source_commit":SOURCE_COMMIT,
        "source_pr":375,
        "source_manifest_git_blob_sha1":ROOT_MANIFEST_BLOB,
        "dependency_prs":DEPENDENCY_PR,
        "scope":"TEN_ORIGINAL_JSON_AND_UNITY_META_ONLY",
        "unity_native_validation":"NOT_RUN",
        "game_integration":"NOT_INTEGRATED",
        "entries":[{"path":p,"size":len(content),"sha256":sha(content),
                    "git_blob_sha1":EXPECTED[p]} for p,content in sorted(files.items())]
    }

def zip_entry(name,contents):
    info=zipfile.ZipInfo(name,date_time=STAMP)
    info.create_system=3
    info.external_attr=0o100644<<16
    info.compress_type=zipfile.ZIP_STORED
    return (info,contents)

def make_archive(root,dest):
    files=source_files(root)
    payload=canon(receipt(files))
    ensure(len(payload)<MAX_FILE,"RECEIPT_TOO_LARGE")
    dest=Path(dest).resolve()
    ensure(dest!=Path(root).resolve()/ROOT_MANIFEST,"REFUSE_OVERWRITE_SOURCE_MANIFEST")
    ensure(not dest.exists(),"REFUSE_ARCHIVE_OVERWRITE")
    dest.parent.mkdir(parents=True,exist_ok=True)
    try:
        with zipfile.ZipFile(dest,"x") as archive:
            for name,bytes_ in sorted({**files,RECEIPT:payload}.items()):
                info,chunk=zip_entry(name,bytes_)
                archive.writestr(info,chunk)
        actual=verify_archive(root,dest)
        print("P17_BRACING_ARCHIVE_BUILT sha256="+sha(dest.read_bytes())+
              " bytes="+str(dest.stat().st_size)+" entries="+str(len(actual)))
    except Exception:
        dest.unlink(missing_ok=True)
        raise

def verify_archive(root,path):
    trusted=source_files(root)
    target=Path(path)
    ensure(target.is_file(),"MISSING_BRACING_ARCHIVE")
    ensure(target.stat().st_size<=2*1024*1024,"ARCHIVE_SIZE_EXCEEDED")
    expected=set(trusted)|{RECEIPT}
    with zipfile.ZipFile(target,"r") as archive:
        infos=archive.infolist()
        names=[i.filename for i in infos]
        ensure(len(names)==len(expected) and set(names)==expected,
               "ARCHIVE_UNEXPECTED_OR_DUPLICATE_ENTRIES")
        ensure(all(safe_name(n) for n in names),"ARCHIVE_PATH_TRAVERSAL")
        ensure(names==sorted(names),"ARCHIVE_NONDETERMINISTIC_ORDER")
        decoded={}
        for info in infos:
            ensure(info.file_size<=MAX_FILE and
                   info.compress_type==zipfile.ZIP_STORED and
                   info.date_time==STAMP and
                   info.create_system==3 and
                   (info.external_attr>>16)==0o100644,
                   "ARCHIVE_UNSAFE_MEMBER_METADATA")
            data=archive.read(info)
            ensure(len(data)==info.file_size,"TRUNCATED_ARCHIVE_MEMBER")
            decoded[info.filename]=data
        expected_receipt=canon(receipt(trusted))
        ensure(decoded[RECEIPT]==expected_receipt,"ARCHIVE_RECEIPT_MISMATCH")
        for name,contents in trusted.items():
            ensure(decoded[name]==contents,"ARCHIVE_DATA_TAMPERING:"+name)
    return decoded

def isolated_destination(root,dest):
    root=Path(root).resolve(strict=True)
    dest=Path(dest).resolve()
    ensure(not dest.is_relative_to(root),"REFUSE_STAGE_INSIDE_SOURCE_OR_CODEX_REPO")
    ensure(dest!=root and dest!=Path(dest.anchor),"REFUSE_DANGEROUS_DESTINATION")
    return dest

def stage_init(root,dest):
    dest=isolated_destination(root,dest)
    ensure(not dest.exists(),"STAGING_DEST_ALREADY_EXISTS")
    dest.mkdir(parents=True)
    (dest/MARKER).write_bytes(canon({
        "schema":"B01-P17-W03-ISOLATED-NEW-DIRECTORY-v1",
        "source_commit":SOURCE_COMMIT}))
    print("P17_BRACING_STAGE_INITIALIZED isolated="+str(dest))

def marker_bytes():
    return canon({"schema":"B01-P17-W03-ISOLATED-NEW-DIRECTORY-v1",
                  "source_commit":SOURCE_COMMIT})

def required_stage(dest):
    marker=dest/MARKER
    ensure(marker.is_file() and not marker.is_symlink() and
           marker.read_bytes()==marker_bytes(),"NO_TRUSTED_STAGE_MARKER")

def staged_paths(dest):
    return {str(p.relative_to(dest)).replace("\\","/"):p
            for p in dest.rglob("*") if p.is_file() or p.is_symlink()}

def run_stage(root,zip_path,dest):
    decoded=verify_archive(root,zip_path)
    dest=isolated_destination(root,dest)
    required_stage(dest)
    ensure(set(staged_paths(dest))=={MARKER},"STAGE_NOT_EMPTY")
    added=[]
    try:
        for name,data in sorted(decoded.items()):
            path=dest/name
            path.parent.mkdir(parents=True,exist_ok=True)
            ensure(not path.exists() and not path.is_symlink(),"STAGE_PATH_COLLISION")
            path.write_bytes(data)
            added.append(path)
        print("P17_BRACING_STAGE_PASS entries="+str(len(decoded))+
              " isolated_only=YES no_codex_changes=YES")
    except Exception:
        for path in reversed(added):path.unlink(missing_ok=True)
        for p in sorted(dest.rglob("*"),key=lambda v:len(v.parts),reverse=True):
            if p.is_dir():p.rmdir()
        raise

def rollback(root,zip_path,dest):
    decoded=verify_archive(root,zip_path)
    dest=isolated_destination(root,dest)
    required_stage(dest)
    paths=staged_paths(dest)
    ensure(set(paths)==set(decoded)|{MARKER},
           "REFUSE_ROLLBACK_UNKNOWN_OR_MISSING_STAGE_FILES")
    for name,original in decoded.items():
        p=paths[name]
        ensure(p.is_file() and not p.is_symlink() and p.read_bytes()==original,
               "REFUSE_ROLLBACK_CHANGED_CONTENT:"+name)
    for name in sorted(decoded,reverse=True):
        paths[name].unlink()
    for p in sorted(dest.rglob("*"),key=lambda v:len(v.parts),reverse=True):
        if p.is_dir():p.rmdir()
    (dest/MARKER).unlink()
    dest.rmdir()
    print("P17_BRACING_ROLLBACK_PASS 11/11_original_stage_members_removed dest_deleted=YES")

def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument("action",choices=("build","verify","init-stage","stage","rollback"))
    parser.add_argument("--repo",required=True)
    parser.add_argument("--zip",default=None)
    parser.add_argument("--dest",default=None)
    args=parser.parse_args()
    root=Path(args.repo).resolve(strict=True)
    ensure((root/ROOT_MANIFEST).is_file(),"NOT_AN_ORIGINAL_P17_REPO")
    if args.action=="build":
        ensure(args.zip is not None,"MISSING_ZIP_OUTPUT")
        make_archive(root,args.zip)
    elif args.action=="verify":
        ensure(args.zip is not None,"MISSING_ZIP")
        files=verify_archive(root,args.zip)
        print("P17_BRACING_ARCHIVE_VERIFY_PASS entries="+str(len(files))+
              " sha256="+sha(Path(args.zip).read_bytes()))
    elif args.action=="init-stage":
        ensure(args.dest is not None,"MISSING_STAGE_DEST")
        stage_init(root,args.dest)
    elif args.action=="stage":
        ensure(args.dest and args.zip,"MISSING_ZIP_OR_STAGE")
        run_stage(root,args.zip,args.dest)
    else:
        ensure(args.dest and args.zip,"MISSING_ZIP_OR_STAGE")
        rollback(root,args.zip,args.dest)

if __name__=="__main__":
    try:main()
    except (OSError,ValueError,KeyError,zipfile.BadZipFile,json.JSONDecodeError) as exc:
        print("P17_BRACING_DELIVERY_FAIL "+str(exc),file=sys.stderr)
        sys.exit(1)
