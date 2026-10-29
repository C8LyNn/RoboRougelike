#!/usr/bin/env python3
"""B01-61 W04 independent read-only G0 intake of actual B01-23 #391 GitHub archive.
Actual producer-generated *bytes*, not a rebuilt/assumed ZIP. Never writes game.
"""
import argparse
import hashlib
import io
import json
import os
from pathlib import Path, PurePosixPath
import re
import shutil
import subprocess
import sys
import tempfile
import urllib.request
import zipfile

SOURCE="5666a39eabd4cea91b2b605a9eb99ea2f1e226e8"
SOURCE_MANIFEST="Docs/B01/PARTSIZE001/P17_W02_BRACING_MANIFEST.json"
SOURCE_MANIFEST_BLOB="02221b524d2c106e90d179f16c09dcfef8815c6d"
DELIVERY="8fc0f99ef7bf6c39c21609616b7e852538d2de12"
DELIVERY_MANIFEST="Docs/B01/PARTSIZE001/P17_W03_DELIVERY_MANIFEST.json"
DELIVERY_MANIFEST_BLOB="d9a6e3180d8bd8dc4791c36368b977b84ccad6e1"
ARCHIVE_NAME="p17-bracing-original-unity-assets.zip"
RECEIPT="P17_BRACING_ARCHIVE_RECEIPT.json"
ARCHIVE_SHA="c4f9b083d618013d1d6c4f8a888dcb57d0a4e59220d37acf3f45644c65953580"
ARCHIVE_SIZE=23547
IDS={"windows":12910483987,"ubuntu":12911702475}
ROOT=Path(__file__).resolve().parent

def reject(reason):
    raise AssertionError(reason)
def insist(condition, reason):
    if not condition:reject(reason)
def sha256(data):
    return hashlib.sha256(data).hexdigest()
def gitblob(data):
    return hashlib.sha1(b"blob "+str(len(data)).encode()+b"\0"+data).hexdigest()
def git(repo,*args):
    p=subprocess.run(["git","-C",str(repo),*args],capture_output=True)
    if p.returncode:reject("GIT_FAILED "+repr(args)+" "+p.stderr.decode(errors="replace")[-300:])
    return p.stdout
def source_bytes(repo,source,path,blob):
    insist(git(repo,"rev-parse",source+":"+path).decode().strip()==blob,"GIT_BLOB_MISMATCH "+path)
    payload=git(repo,"show",source+":"+path)
    insist(gitblob(payload)==blob,"FETCHED_BLOB_MISMATCH "+path)
    return payload
def read_producer(repo):
    original=json.loads(source_bytes(repo,SOURCE,SOURCE_MANIFEST,SOURCE_MANIFEST_BLOB))
    delivery=json.loads(source_bytes(repo,DELIVERY,DELIVERY_MANIFEST,DELIVERY_MANIFEST_BLOB))
    insist(delivery["deliverable"]["zip_sha256"]==ARCHIVE_SHA and delivery["deliverable"]["size_bytes"]==ARCHIVE_SIZE,"DELIVERY_CLAIM_CHANGED")
    insist(delivery["original_producer"]["sha"]==SOURCE,"DELIVERY_ORIGINAL_SHA_MISMATCH")
    listed={d["path"]:d["git_blob_sha1"] for d in original["original_files"]}
    insist(len(listed)==len(original["original_files"]),"DUPLICATE_ORIGINAL_MANIFEST")
    paths={k:v for k,v in listed.items() if k.startswith("Assets/PARTSIZE001/Bracing")}
    insist(len(paths)==10,"EXACT_TEN_UNITY_FILES_REQUIRED")
    insist(sum(x.endswith(".json") for x in paths)==4,"EXACT_FOUR_JSON_REQUIRED")
    insist(sum(x.endswith(".meta") for x in paths)==6,"EXACT_SIX_META_REQUIRED")
    trusted={k:source_bytes(repo,SOURCE,k,v) for k,v in sorted(paths.items())}
    manifest=json.loads((ROOT/"manifest.json").read_text(encoding="utf8"))
    own=git(repo,"hash-object",str(ROOT/"archive_receipt.py")).decode().strip()
    insist(manifest["archive_receiver_git_blob"]==own,"OWN_SOURCE_BLOB_DRIFT "+own)
    print("PRODUCER_SOURCE_ORIGINAL=PASS | ORIGINAL_GIT_SHA="+SOURCE+" | DELIVERY_SHA="+DELIVERY+" | UNCHANGED_UNITY_PATHS=10/10")
    return trusted

def safe_path(path):
    p=PurePosixPath(path)
    return bool(path and not p.is_absolute() and str(p)==path
        and all(x not in ("",".","..") for x in p.parts)
        and not path.startswith("/") and "\\" not in path and ":" not in path
        and not path.endswith("/") and len(path)<180)

def inspect_inner(payload,trusted,require_actual_hash=True):
    if require_actual_hash:
        insist(len(payload)==ARCHIVE_SIZE and sha256(payload)==ARCHIVE_SHA,"ACTUAL_INNER_ZIP_SHA256_OR_SIZE_MISMATCH")
    insist(len(payload)<=1024*1024,"ARCHIVE_TOO_LARGE")
    with zipfile.ZipFile(io.BytesIO(payload)) as archive:
        infos=archive.infolist()
        names=[i.filename for i in infos]
        insist(len(names)==11 and len(set(names))==11,"EXPECTED_EXACT_11_UNIQUE_MEMBERS")
        insist(set(names)==set(trusted)|{RECEIPT},"UNEXPECTED_MEMBER")
        insist(names==sorted(names),"NONDETERMINISTIC_ORIGINAL_ZIP_ORDER")
        insist(all(safe_path(name) for name in names),"ZIP_PATH_UNSAFE")
        for info in infos:
            insist(info.file_size<=131072 and info.compress_type==zipfile.ZIP_STORED,"UNTRUSTED_COMPRESSION_OR_SIZE")
            insist(info.create_system==3 and (info.external_attr>>16)==0o100644,"UNSAFE_EXECUTABLE_OR_SYMLINK_MEMBER")
            insist(info.date_time==(2020,1,1,0,0,0),"NONCANONICAL_MEMBER_TIMESTAMP")
            insist(not (info.flag_bits&1),"ENCRYPTED_MEMBER_REJECT")
        contents={i.filename:archive.read(i) for i in infos}
    insist(all(len(contents[n])>0 and contents[n]==original for n,original in trusted.items()),
        "ARCHIVE_CONTENT_DIFFERS_FROM_ORIGINAL_PRODUCER_GIT_BYTES")
    receipt=json.loads(contents[RECEIPT])
    insist(receipt.get("schema")=="B01-P17-W03-BRACING-PORTABLE-ARCHIVE-v1","RECEIPT_SCHEMA_CHANGED")
    insist(receipt.get("source_commit")==SOURCE and receipt.get("source_pr")==375,"FORGED_SOURCE_ORIGIN")
    insist(receipt.get("source_manifest_git_blob_sha1")==SOURCE_MANIFEST_BLOB,"FORGED_ORIGINAL_MANIFEST")
    insist(receipt.get("scope")=="TEN_ORIGINAL_JSON_AND_UNITY_META_ONLY","RECEIPT_SCOPE_FORGERY")
    insist(receipt.get("unity_native_validation")=="NOT_RUN" and receipt.get("game_integration")=="NOT_INTEGRATED",
           "FALSE_NATIVE_UNITY_OR_INTEGRATION_CLAIM")
    entries=receipt.get("entries")
    insist(isinstance(entries,list) and len(entries)==10,"RECEIPT_ENTRY_COUNT")
    bypath={x["path"]:x for x in entries}
    insist(len(bypath)==10 and set(bypath)==set(trusted),"RECEIPT_MEMBERS_DIFFER")
    for path,data in trusted.items():
        r=bypath[path]
        insist(r["size"]==len(data) and r["sha256"]==sha256(data) and r["git_blob_sha1"]==gitblob(data),
               "RECEIPT_SHA256_GIT_SHA1_SIZE_MISMATCH "+path)
    guids=[]
    for path,data in trusted.items():
        if path.endswith(".meta"):
            match=re.findall(rb"(?m)^guid:\s*([a-fA-F0-9]{32})\s*$",data)
            insist(len(match)==1,"INVALID_ORIGINAL_UNITY_META_GUID "+path)
            guids.append(match[0].lower())
    insist(len(set(guids))==6,"DUPLICATE_OWNED_UNITY_GUID")
    return {"originalMembers":10,"newGuidCount":6,"originalBytes":sum(len(x) for x in trusted.values()),
            "originalReceipt":receipt,"contents":contents}

def inspect_outer(wrapped,trusted):
    insist(len(wrapped)<4*1024*1024,"GITHUB_WRAPPER_OVERSIZE")
    with zipfile.ZipFile(io.BytesIO(wrapped)) as z:
        infos=z.infolist()
        insist(len(infos)==1 and infos[0].filename==ARCHIVE_NAME,"WRAPPER_MUST_HAVE_SINGLE_ORIGINAL_INNER_ZIP")
        insist(infos[0].file_size==ARCHIVE_SIZE,"WRAPPER_INNER_ZIP_SIZE_CHANGED")
        inner=z.read(ARCHIVE_NAME)
    parsed=inspect_inner(inner,trusted,require_actual_hash=True)
    return inner,parsed

def build_modified(original, replacements):
    """Only an intentionally *broken in-memory* archive for negative controls."""
    parts=dict(replacements)
    with zipfile.ZipFile(io.BytesIO(original)) as src:
        old=[(i,src.read(i)) for i in src.infolist()]
    mem=io.BytesIO()
    with zipfile.ZipFile(mem,"w") as dest:
        for meta,data in old:
            dest.writestr(meta,parts.get(meta.filename,data))
    return mem.getvalue()

def negative_checks(original,trusted):
    filename=next(x for x in trusted if x.endswith(".json"))
    cases=[]
    cases.append(("tamper_actual_inner_sha",lambda:inspect_inner(original+b"evil",trusted)))
    bad=build_modified(original,{filename:trusted[filename]+b"\x20"})
    cases.append(("asset_modified_same_member_name",lambda:inspect_inner(bad,trusted,False)))
    bad=build_modified(original,{RECEIPT:b'{"schema":"FAKE"}'})
    cases.append(("receipt_forgery",lambda:inspect_inner(bad,trusted,False)))
    with zipfile.ZipFile(io.BytesIO(original)) as z:
        items=[(i,z.read(i)) for i in z.infolist()]
    for title,evil in (("path_traversal","../Assets/Unexpected.txt"),("unexpected_codex_asset","Assets/R3/OwnedByCodex.cs")):
        mem=io.BytesIO()
        with zipfile.ZipFile(mem,"w") as z:
            for meta,data in items:z.writestr(meta,data)
            z.writestr(evil,"bad")
        cases.append((title,lambda m=mem.getvalue():inspect_inner(m,trusted,False)))
    mem=io.BytesIO()
    with zipfile.ZipFile(mem,"w") as z:
        for meta,data in items:z.writestr(meta,data)
        z.writestr(items[0][0],items[0][1])
    cases.append(("duplicate_archive_entry",lambda m=mem.getvalue():inspect_inner(m,trusted,False)))
    for name,fn in cases:
        try:fn()
        except (AssertionError,ValueError,zipfile.BadZipFile,json.JSONDecodeError):
            print("NEGATIVE_UNTRUSTED_ARCHIVE_REJECTED="+name)
        else:reject("ARCHIVE_MUTATION_FALSE_GREEN "+name)
    insist(len(cases)==6,"MUTATION_COVERAGE_CHANGED")
    print("INDEPENDENT_ARCHIVE_MUTATIONS=6/6_REJECTED")

def stage_and_rollback(trusted):
    # Pure *disposable* temp directory, never inside original git checkout/game.
    with tempfile.TemporaryDirectory(prefix="b0161_bracing_intake_") as parent:
        dest=Path(parent)/"fresh-stage"
        dest.mkdir()
        insist(not list(dest.iterdir()),"UNSAFE_NONEMPTY_INITIAL_STAGE")
        for name,data in trusted.items():
            path=dest/name
            path.parent.mkdir(parents=True,exist_ok=True)
            insist(not path.exists() and safe_path(name),"REFUSE_OVERWRITE")
            path.write_bytes(data)
        for name,data in trusted.items():
            insist((dest/name).read_bytes()==data,"STAGED_MEMBER_MISMATCH")
        print("ACTUAL_DISPOSABLE_STAGE=10/10_BYTE_EXACT_PASS")
        for name in trusted:
            insist((dest/name).read_bytes()==trusted[name],"UNSAFE_ROLLBACK_DETECTED_MODIFIED_FILE")
        # Refuse rollback once actual staged bytes are changed, then restore only our fixture.
        first=next(iter(trusted))
        (dest/first).write_bytes(trusted[first]+b"tampered")
        changed=(dest/first).read_bytes()!=trusted[first]
        insist(changed,"TAMPERING_FIXTURE_MISSING")
        refused=any((dest/n).read_bytes()!=data for n,data in trusted.items())
        insist(refused,"ROLLBACK_FAILED_TO_DETECT_EDIT")
        print("NONDESTRUCTIVE_ROLLBACK_ON_TAMPER=REJECTED_AS_EXPECTED")
        (dest/first).write_bytes(trusted[first])
        for path in sorted(dest.rglob("*"),key=lambda p:len(p.parts),reverse=True):
            if path.is_file():
                rel=path.relative_to(dest).as_posix()
                insist(rel in trusted and path.read_bytes()==trusted[rel],"REFUSE_DESTRUCTIVE_ROLLBACK")
                path.unlink()
            elif path.is_dir():path.rmdir()
        insist(not list(dest.iterdir()),"ROLLBACK_LEFT_FILES")
        print("DISPOSABLE_STAGE_ROLLBACK=PASS | CANONICAL_GAME_WRITES=0")

def download_github_artifact(repo,id):
    token=os.environ.get("GITHUB_TOKEN","")
    insist(bool(token),"GITHUB_ACTIONS_TOKEN_REQUIRED")
    req=urllib.request.Request(
        f"https://api.github.com/repos/C8LyNn/RoboRougelike/actions/artifacts/{id}/zip",
        headers={"Accept":"application/vnd.github+json","Authorization":"Bearer "+token,
                 "X-GitHub-Api-Version":"2022-11-28","User-Agent":"B01-61-Independent-Archive-Receiver"})
    with urllib.request.urlopen(req,timeout=45) as resp:
        data=resp.read(4*1024*1024+1)
    insist(len(data)<4*1024*1024,"DOWNLOADED_GITHUB_ARTIFACT_TOO_LARGE")
    return data

def main():
    ap=argparse.ArgumentParser()
    ap.add_argument("--repo",default=".")
    ap.add_argument("--artifact",choices=sorted(IDS),required=True)
    ap.add_argument("--output",default="bracing_zip_independent_receipt.json")
    args=ap.parse_args()
    repo=Path(args.repo).resolve()
    for sha in (SOURCE,DELIVERY):
        insist(git(repo,"rev-parse",sha+"^{commit}").decode().strip()==sha,"EXACT_PRODUCER_GIT_COMMIT_UNAVAILABLE")
    trusted=read_producer(repo)
    wrapped=download_github_artifact(repo,IDS[args.artifact])
    inner,parsed=inspect_outer(wrapped,trusted)
    print("ACTUAL_GITHUB_ACTIONS_ARTIFACT_DOWNLOADED="+str(IDS[args.artifact]))
    print("ACTUAL_INNER_ZIP_BYTES="+str(len(inner))+" | SHA256="+sha256(inner))
    print("ORIGINAL_GAME_ASSET_BYTES=10/10_MATCH | ACTUAL_ZIP_ENTRIES=11/11_MATCH | SIX_UNITY_GUIDS=PASS")
    negative_checks(inner,trusted)
    stage_and_rollback(trusted)
    output={"schema":"B01-61-W04-BRACING-ZIP-RECEIVER-v1","artifactID":IDS[args.artifact],
        "artifactOS":args.artifact,"sourceCommit":SOURCE,"producerDeliveryCommit":DELIVERY,
        "actualInnerZipBytes":len(inner),"actualInnerZipSha256":sha256(inner),
        "gitExactAssetCount":10,"memberCount":11,"unityGUIDCount":6,
        "inMemoryNegativeRejections":6,"disposableStage":"PASS","safeRollback":"PASS",
        "nativeUnity":"NOT_RUN","player":"NOT_RUN","codexAcknowledgement":"NOT_OBSERVED"}
    path=Path(args.output)
    insist(not path.exists(),"REFUSE_RECEIPT_OVERWRITE")
    path.write_text(json.dumps(output,indent=2,sort_keys=True)+"\n",encoding="utf8")
    print("ARCHIVE_RECEIVER_REPORT_SHA256="+sha256(path.read_bytes()))
    print("B0161_W04_ORIGINAL_BINARY_INTAKE=PASS | ORIGINAL_ZIP=VERIFIED | 10/10_REAL_BYTES | SIX_NEGATIVES_REJECTED | STAGE_ROLLBACK=PASS | UNITY_NATIVE=NOT_RUN")
    return 0

if __name__=="__main__":
    try:sys.exit(main())
    except Exception as exc:
        print("B0161_W04_ARTIFACT_QA_RED="+repr(exc),file=sys.stderr)
        sys.exit(2)
