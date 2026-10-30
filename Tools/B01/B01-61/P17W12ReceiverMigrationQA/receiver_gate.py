#!/usr/bin/env python3
"""B01-61 W12: read-only receiver scan + actual frozen #510 source archive staging/rollback QA.

Runs producer's unchanged original package.py and SHA-locked original Git objects in a
temporary detached --no-checkout worktree at #510. Does NOT access/modify Codex receiver.
An actual receiving workspace is necessary for production G0; synthetic fixtures aren't G0.
"""
import argparse
import contextlib
import hashlib
import importlib.util
import json
import pathlib
import re
import subprocess
import tempfile
from typing import Iterator

ROOT=pathlib.Path(__file__).resolve().parents[4]
PRODUCER="d94400d95d13db7f27e1533df05c9292d5380431"
PACKAGE_PATH="Tools/B01/B01-23/P17W10SecureReattachV2/package.py"
PACKAGE_BLOB="a4dd7df4ea2ae83049ff76c460953c2b6cb50850"
EXPECTED_ARCHIVE_SHA256="d4ddfa3528ef0416aefffca72d884000f3b0c9959fb8dff53096487ef95c9387"
SELECTED_GUID="b01a0a10000000000000000000000009"
LEGACY_GUIDS={
    "b017d070000000000000000000000008":"W07 unsafe DebrisRigPlayground",
    "b018d080000000000000000000000008":"W08 optional gate",
    "b019d090000000000000000000000008":"W09 GUI-only hardening",
}
# Unity serialised references may be on INACTIVE objects: inspect all source YAML,
# including disabled components, not only names or currently instantiated scenes.
UNITY_EXT={".unity",".prefab",".asset",".controller",".overridecontroller",".playable",".anim"}
LEGACY_SOURCE=re.compile(r"\b(DebrisRigPlayground|DebrisSafeReattachGate|HardenedDockDemo)\b|B01\.DebrisPlayground001")
CS_LINE_COMMENT=re.compile(r"//[^\n]*")
CS_BLOCK_COMMENT=re.compile(r"/\*.*?\*/",re.DOTALL)
def git(*args,cwd=ROOT):
    return subprocess.check_output(["git",*args],cwd=cwd,text=True,stderr=subprocess.PIPE).strip()
def require(ok,msg):
    if not ok:raise AssertionError(msg)
def sha(b):return hashlib.sha256(b).hexdigest()

@contextlib.contextmanager
def frozen_producer()->Iterator[object]:
    """A separate exact Git HEAD, no producer files written and no checkout needed."""
    require(git("rev-parse","HEAD:"+PACKAGE_PATH)==PACKAGE_BLOB,"PRODUCER_PACKAGE_SOURCE_BLOB_CHANGED")
    require(git("rev-parse",PRODUCER+":"+PACKAGE_PATH)==PACKAGE_BLOB,"PRODUCER_PACKAGE_BLOB_PIN_CHANGED")
    with tempfile.TemporaryDirectory(prefix="b0161_w12_frozen_") as tmp:
        wd=pathlib.Path(tmp)/"source"
        git("worktree","add","--no-checkout","--detach",str(wd),PRODUCER)
        try:
            spec=importlib.util.spec_from_file_location("frozen_b0123_original_package",
                        ROOT/PACKAGE_PATH)
            module=importlib.util.module_from_spec(spec)
            spec.loader.exec_module(module)
            module.ROOT=wd
            require(module.source_sha()==PRODUCER,"GIT_ORIGINAL_W12_PRODUCER_PIN")
            yield module
        finally:
            git("worktree","remove","--force",str(wd))
def archive_attestation(module):
    raw=module.build()
    require(sha(raw)==EXPECTED_ARCHIVE_SHA256,"ORIGINAL_INNER_ZIP_SHA256_CHANGED")
    files=module.verify(raw)
    manifest=json.loads(files[module.MANIFEST])
    require(len(files)==35 and len(manifest["files"])==34,
            "ORIGINAL_ZIP_FILE_COUNT_CHANGED")
    require(manifest["source_sha"]==PRODUCER and
            manifest["version"]=="2.0.0" and manifest["codex_ack"] is False and
            manifest["native_unity"]=="NOT_RUN",
            "SOURCE_VERSION_OR_PROVENANCE_INCONSISTENT")
    require(manifest["owned_meta_guids"]==14,"UNITY_GUID_COUNT_DRIFT")
    assembly=sum(x["path"].endswith(".asmdef") for x in manifest["files"])
    require(assembly==3,"UNITY_ASMDEF_GRAPH_COUNT_DRIFT")
    return raw,manifest

def scan_receiver(root):
    """Strict read-only receiver migration scan, *no writes*. Does not claim full game equivalence."""
    root=pathlib.Path(root)
    require(root.is_dir() and not root.is_symlink(),"RECEIVER_ROOT_MISSING_OR_SYMLINK")
    assets=root/"Assets"
    require(assets.is_dir() and not assets.is_symlink(),"RECEIVER_ASSETS_NOT_PRESENT")
    violations=[]
    scanned=0
    for path in sorted(assets.rglob("*")):
        rel=path.relative_to(root).as_posix()
        if path.is_symlink():
            violations.append(rel+":SYMLINK_NOT_SCANNABLE")
            continue
        if not path.is_file():continue
        suffix=path.suffix.lower()
        if suffix not in UNITY_EXT|{".cs"}:continue
        scanned+=1
        try:txt=path.read_text(encoding="utf-8")
        except UnicodeDecodeError:
            violations.append(rel+":NON_UTF8_UNINSPECTED")
            continue
        if suffix in UNITY_EXT:
            for guid,label in LEGACY_GUIDS.items():
                if re.search(r"(?i)\bguid:\s*"+guid+r"\b",txt):
                    violations.append(rel+":LEGACY_GUID_"+label.split()[0])
            # Class name in Unity serialized string does not instantiate a MonoBehaviour;
            # do not label mere display strings as a source-level API invocation.
        else:
            # Strip comments, but be conservative in source token scanning: false
            # positives must prompt receiver review, not authorize silent import.
            code=CS_BLOCK_COMMENT.sub("",CS_LINE_COMMENT.sub("",txt))
            if LEGACY_SOURCE.search(code):
                violations.append(rel+":LEGACY_SOURCE_TYPE_OR_NAMESPACE")
            # New candidate can only be installed by pinned exact byte archive:
            # alternate #511 source at same path is detected by archive-bytes attestation.
    return {"inspected_files":scanned,"violations":violations,
            "status":"PASS_SOURCE_SCAN_ONLY" if not violations else "HOLD_LEGACY_REACHABLE"}

def stage_and_scan(module,raw,receiver_dir):
    target=pathlib.Path(receiver_dir)
    require(not target.exists(),"REFUSE_OVERWRITE_RECEIVER_PATH")
    module.stage(raw,target)
    result=scan_receiver(target)
    return target,result

def report():
    with frozen_producer() as producer:
        raw,manifest=archive_attestation(producer)
        with tempfile.TemporaryDirectory(prefix="b0161_w12_receiver_") as parent:
            target,scan=stage_and_scan(producer,raw,pathlib.Path(parent)/"selected_only")
            require(not scan["violations"] and scan["inspected_files"]>0,
                    "SELECTED_SOURCE_CONTAINS_UNEXPECTED_LEGACY")
            producer.rollback(raw,target)
            require(not target.exists(),"SELECTED_STAGE_ROLLBACK_LEFTOVER")
    return {"role":"B01-61","source_pr":510,"source_sha":PRODUCER,
        "archive_sha256":sha(raw),"archive_members":len(manifest["files"])+1,
        "meta_guids":manifest["owned_meta_guids"],"asmdefs":3,
        "source_receiver_scan":scan["status"],
        "synthetic_receiver":True,"actual_codex_receiver":"NOT_SCANNED",
        "unity_native":"NOT_RUN","G0":"SOURCE_PRECHECK_ONLY_RECEIVER_PENDING",
        "G1":"NOT_RUN","G2":"HOLD","ACK":False,"integrated":False}

def main():
    ap=argparse.ArgumentParser()
    ap.add_argument("--receiver",type=pathlib.Path,help="read-only existing receiver workspace; does NOT install")
    ap.add_argument("--release",action="store_true",
                    help="fail closed if external receiver not supplied or actual migration not approved")
    args=ap.parse_args()
    receipt=report()
    if args.receiver:
        receiver=scan_receiver(args.receiver)
        receipt["actual_codex_receiver"]=receiver
        # A read-only scan cannot prove ownership, ACK, Unity native, or integration.
    print(json.dumps(receipt,sort_keys=True))
    if args.receiver and receipt["actual_codex_receiver"]["violations"]:
        print("RECEIVER_SOURCE_MIGRATION_HOLD_LEGACY",flush=True)
        raise SystemExit(3)
    if args.release:
        print("RELEASE_GATE_HOLD: native Unity G1/receiver owner ACK required; no integration permission",flush=True)
        raise SystemExit(3)
    print("QA_SOURCE_PRECHECK_PASS; REAL_CODEX_RECEIVER_NOT_VERIFIED; NATIVE_UNITY_NOT_RUN")
if __name__=="__main__":main()
