#!/usr/bin/env python3
"""B01-40 W05: independently receive frozen B01-22 v1.0.3, not old #333.

Executes unchanged original author validator and stage/rollback in a detached
source worktree. Verifies exactly seven allowed source changes from v1.0.2,
all original 13 SHA-256 payloads, old/new kit/prices equality, current Codex
R3/R4 Git blob pins, and emits a deterministic physical QA-only receipt ZIP.
No Unity, actual Codex/producer Assets mutations, merge, or ACK.
"""
import argparse
import hashlib
import io
import json
from pathlib import Path
import re
import subprocess
import sys
import tempfile
import zipfile

ID="B01-40-P17W05-NEW-KIT-V103-RECEIVER-001"
OLD="0d212a706c1e633b773d2e03b7b40c28bb63521b"
NEW="a3dd45a4c1f222ec5cc387302eafe9104f91ab52"
CODEX="203a0805251ed28293888e43f2d49f0df63767c6"
CODEX_REPORT="f92c873bb1e397a598b133e107ae704e15f86768"
PACKAGE="B01-22-EQUIPMENT-KITS-001"
R3="Assets/R3/R3VehicleModel.cs"
R4="Assets/R4/R4WeaponDefinition.cs"
OLD_R3="8cc68faf89793c803e5807594c9faf0df10091"
LATEST_R3="1d0f378dc598e08b3d7c16975f419d84231511bb"
R4_BLOB="502f6d7f70d4e8078260de632ab181f244a4d730"
OLD_GATE_BLOB="c2f0578bff7c450a522f89584941e445c8c675af"
NEW_MANIFEST_BLOB="bd4b759522ca1ebd385abf90680ef9f885ca3bb3"
NEW_CATALOG_BLOB="372b3bc74553ea459e89be38378f4429090e00a2"
DIR="Research/B01-22/P17/EQUIPMENT-KITS-001"
ARCHIVE="B01-40-W05-Actual-10-Kit-V103-Source-Ingress.zip"
EXPECTED_DIFF={
".github/workflows/b01-22-p17-equipment-kits.yml",
*(DIR+"/"+f for f in ("CHANGELOG.md","INSTALL_FOR_CODEX.md","README.md",
                         "catalog.json","manifest.json","stage_kit_payload.py"))
}
PREVIOUS_PIN="aa7a917f177a9b1f07203868fb7d1d871eb3ee46"
class Reject(ValueError):pass
def need(c,reason):
    if not c:raise Reject(reason)
def digest(x):return hashlib.sha256(x).hexdigest()
def canon(o):return (json.dumps(o,ensure_ascii=False,sort_keys=True,separators=(",",":"),allow_nan=False)+"\n").encode("utf-8")
def run(argv,cwd=None,allow_failed=False):
    p=subprocess.run(argv,cwd=cwd,capture_output=True,text=True,encoding="utf-8",
                     errors="replace",timeout=200)
    if p.returncode and not allow_failed:
        raise Reject("EXECUTION_FAILED "+str(p.returncode)+" "+repr(argv)+" "+(p.stdout+p.stderr)[-800:])
    return p
def git(repo,*args):return run(["git","-C",str(repo),*args]).stdout.strip()
def obj(repo,sha,path):
    data=subprocess.check_output(["git","-C",str(repo),"show",sha+":"+path])
    got=git(repo,"rev-parse",sha+":"+path)
    good=hashlib.sha1(b"blob "+str(len(data)).encode()+b"\x00"+data).hexdigest()
    need(got==good,"GIT_BLOB_CONTENT_INTEGRITY:"+path)
    return data,got
def verify_source(repo):
    need(git(repo,"rev-parse",NEW+"^")==OLD,"NEW_REVISION_NOT_SINGLE_CHILD_OF_FROZEN_V102")
    changed=git(repo,"diff","--name-only",OLD,NEW).splitlines()
    need(set(changed)==EXPECTED_DIFF and len(changed)==len(EXPECTED_DIFF),
         "UNEXPECTED_AUTHOR_REVISION_SCOPE:"+str(changed))
    need(not any(p.startswith("Assets/") or p.startswith("Tools/") for p in changed),
         "CODEX_OR_QA_OWNERSHIP_OVERLAP")
    catalog_bytes,cat_blob=obj(repo,NEW,DIR+"/catalog.json")
    manifest_bytes,manifest_blob=obj(repo,NEW,DIR+"/manifest.json")
    need(cat_blob==NEW_CATALOG_BLOB and manifest_blob==NEW_MANIFEST_BLOB,
         "FROZEN_NEW_CATALOG_OR_MANIFEST_CHANGED")
    c=json.loads(catalog_bytes)
    m=json.loads(manifest_bytes)
    old=json.loads(obj(repo,OLD,DIR+"/catalog.json")[0])
    need(m["package_id"]==PACKAGE and m["revision"]=="1.0.3" and
         m["previous_frozen_source_sha"]==OLD and
         m["source_base_sha"]==OLD and m["codex_target_sha"]==CODEX,
         "NEW_MANIFEST_VERSION_LINEAGE_INVALID")
    need(c["package_id"]==old["package_id"]==PACKAGE and
         c["version"]=="1.0.3" and old["version"]=="1.0.0",
         "NEW_CATALOG_REVISION_INVALID")
    need(c["kits"]==old["kits"] and c["part_prices"]==old["part_prices"]
         and c["weapon_prices"]==old["weapon_prices"] and
         len(c["kits"])==10 and len(c["part_prices"])==17 and len(c["weapon_prices"])==9
         and sorted(x["id"] for x in c["kits"])==["EK%02d"%i for i in range(1,11)],
         "KIT_BOM_OR_PRICES_CHANGED_FROM_IMMUTABLE_OLD")
    for field in set(c)|set(old):
        if field not in ("version","provenance"):
            need(c.get(field)==old.get(field),"UNEXPECTED_NON_PRICE_CATALOG_CHANGE:"+field)
    need(c["provenance"]=={"codex_pr":383,"codex_sha":CODEX,
         "r3_blob":LATEST_R3,"r4_blob":R4_BLOB},"NEW_CATALOG_CODEX_PROVENANCE_INVALID")
    need(old["provenance"]["codex_sha"]==PREVIOUS_PIN and
         old["provenance"]["r3_blob"]==OLD_R3,"OLD_PROVENANCE_MUST_REMAIN_FROZEN")
    need(len(m["files"])==13,"NOT_EXACTLY_13_REAL_SOURCE_ENTRIES")
    paths=set()
    for f in m["files"]:
        path=f["path"]
        need(path.startswith(DIR+"/") and path not in paths and
             ".." not in Path(path).parts,"UNSAFE_DUPLICATE_SOURCE_PATH")
        actual,_=obj(repo,NEW,path)
        need(digest(actual)==f["sha256"] and len(actual)==f["bytes"],
             "NEW_SOURCE_SHA256_BYTES_MISMATCH:"+path)
        paths.add(path)
    for source_name in ("verify_host_source.py","kit_core.mjs","kit_core.test.mjs",
                        "kit_cli.mjs","test_stage_kit_payload.py"):
        p=DIR+"/"+source_name
        need(obj(repo,NEW,p)[1]==obj(repo,OLD,p)[1],
             "AUTHOR_ALGORITHM_OR_TEST_CHANGED:"+p)
    need(obj(repo,NEW,DIR+"/verify_host_source.py")[1]==OLD_GATE_BLOB,
         "PRODUCER_STRICT_PIN_CHECK_MUST_STAY_IDENTICAL")
    old_r3,pinned=obj(repo,PREVIOUS_PIN,R3)
    latest_r3,current=obj(repo,CODEX,R3)
    report_r3,report_id=obj(repo,CODEX_REPORT,R3)
    need(pinned==OLD_R3 and current==report_id==LATEST_R3 and
         old_r3.replace(b"public sealed class R3VehicleModel",
                        b"public sealed partial class R3VehicleModel",1)==latest_r3
         and latest_r3==report_r3,"REAL_CODEX_R3_NOT_PARTIAL_ONLY")
    need(obj(repo,CODEX,R4)[1]==obj(repo,CODEX_REPORT,R4)[1]==R4_BLOB,
         "REAL_CODEX_R4_PRICES_CHANGED")
    return {"source_SHA":NEW,"source_old_SHA":OLD,
            "new_catalog_git_blob":cat_blob,"new_manifest_git_blob":manifest_blob,
            "author_changed_paths":changed,"source_13_sha256_and_sizes":"PASS",
            "old_and_new_kit_BOMs_10":"IDENTICAL",
            "old_and_new_part_prices_17":"IDENTICAL",
            "old_and_new_weapon_prices_9":"IDENTICAL",
            "new_codex_sha":CODEX,"current_r3_blob":current,"r4_blob":R4_BLOB},c
def parse_node(log):
    tests=re.findall(r"(?m)^# tests (\d+)\s*$",log)
    passed=re.findall(r"(?m)^# pass (\d+)\s*$",log)
    failed=re.findall(r"(?m)^# fail (\d+)\s*$",log)
    need(tests and passed and failed and (int(tests[-1]),int(passed[-1]),int(failed[-1]))==(26,26,0),
         "ORIGINAL_AUTHOR_NODE_26_FAILED")
    return 26
def parse_py(log):
    tests=re.findall(r"Ran (\d+) tests? in",log)
    need(tests and int(tests[-1])==18 and re.search(r"(?m)^OK\s*$",log)
         and "FAILED (" not in log,"ORIGINAL_AUTHOR_PY_18_FAILED")
    return 18
def archive(files):
    buf=io.BytesIO()
    with zipfile.ZipFile(buf,"w",zipfile.ZIP_STORED) as z:
        for name,raw in sorted(files.items()):
            need(name and not name.startswith("/") and "\\" not in name and
                 ".." not in Path(name).parts,"UNSAFE_QA_ARCHIVE_FILENAME")
            entry=zipfile.ZipInfo(name,(1980,1,1,0,0,0))
            entry.create_system=3
            entry.external_attr=0o100644<<16
            z.writestr(entry,raw)
    return buf.getvalue()
def receive(repo,out):
    repo=Path(repo).resolve(strict=True)
    out=Path(out).absolute()
    need(Path(git(repo,"rev-parse","--show-toplevel")).resolve()==repo,"REPO_GIT_ROOT_REQUIRED")
    need(not out.exists() and out.parent.is_dir() and repo not in out.parents
         and not any(p.is_symlink() for p in (out,*out.parents)),"EXTERNAL_NEW_OUTPUT_REQUIRED")
    source,oldnew=verify_source(repo)
    with tempfile.TemporaryDirectory(prefix="B0140_W05_ACCEPT_") as t:
        work=Path(t)/"immutable_owner"
        run(["git","-C",str(repo),"worktree","add","--detach",str(work),NEW])
        try:
            p=work/DIR
            n=parse_node(run(["node","--test",str(p/"kit_core.test.mjs")],cwd=work).stdout)
            py=run([sys.executable,"-m","unittest","discover","-s",str(p),
                    "-p","test_stage_kit_payload.py","-v"],cwd=work)
            q=parse_py(py.stderr)
            source_gate=run([sys.executable,str(p/"verify_host_source.py"),
                             "--current-ref",CODEX,"--selftest"],cwd=work)
            s=json.loads(source_gate.stdout)
            need(s["result"]=="PASS" and s["kits"]==10 and
                 len(s["rejected_mutations"])==7 and
                 s["original_sha"]==CODEX,"NEW_AUTHOR_STRICT_CODEX_GATE_FAILED")
            source_verify=run([sys.executable,str(p/"stage_kit_payload.py"),
                               "--verify-only"],cwd=work)
            need(json.loads(source_verify.stdout)["status"]=="VERIFIED",
                 "NEW_OWNER_MANIFEST_VERIFY_FAILED")
            staged=Path(t)/"actual_offers"
            run([sys.executable,str(p/"stage_kit_payload.py"),
                 "--stage",str(staged)],cwd=work)
            staged_files={k:(staged/k).read_bytes() for k in
                          ("catalog.json","offers.json","receipt.json")}
            offers=json.loads(staged_files["offers.json"])
            receipt=json.loads(staged_files["receipt.json"])
            need(offers["data_only"] is True and
                 offers["host_transaction_required"] is True and
                 offers["source_codex_sha"]==CODEX and
                 len(offers["offers"])==10 and receipt["modified_host"] is False,
                 "NEW_AUTHOR_REAL_STAGE_WRONG")
            need(staged_files["catalog.json"]==obj(repo,NEW,DIR+"/catalog.json")[0],
                 "NEW_STAGE_CATALOG_DIFFERENT_FROM_ORIGINAL")
            # Reconcile every offered cost with actual frozen BOM and prices,
            # not just the original author's 26 self-tests.
            keyed={o["id"]:o for o in offers["offers"]}
            for kit in oldnew["kits"]:
                given=keyed[kit["id"]]
                expected=sum(x["count"]*(oldnew["weapon_prices"][x["weapon"]]
                         if x["kind"]=="Gun" else oldnew["part_prices"][x["kind"]])
                         for x in kit["parts"])
                need(given["reference_total_scrap"]==expected and
                     given["item_count"]==sum(x["count"] for x in kit["parts"]),
                     "PRICE_OR_ITEM_COUNT_DRIFT:"+kit["id"])
            rb=run([sys.executable,str(p/"stage_kit_payload.py"),
                    "--rollback",str(staged)],cwd=work)
            need(json.loads(rb.stdout)["status"]=="ROLLED_BACK" and not staged.exists(),
                 "ORIGINAL_SAFE_ROLLBACK_DID_NOT_DELETE_STAGED_ONLY")
            # Strong negative: re-pin to an older R3 source must fail, and
            # original author must reject (not update) with exit code 2.
            older=run([sys.executable,str(p/"verify_host_source.py"),
                       "--current-ref",PREVIOUS_PIN],cwd=work,allow_failed=True)
            need(older.returncode==2 and "LATEST_CODEX_BLOB_DRIFT" in older.stderr,
                 "LATEST_PRODUCER_SOURCE_GATE_NOT_FAIL_CLOSED")
            report={"receiver_package":ID,"producer_package":PACKAGE,
                    "producer_pr":411,"producer_sha":NEW,
                    "source_checks":source,"author_node_pass":n,
                    "author_python_pass":q,"author_seven_negative_mutants":7,
                    "new_sha_gate_latest_codex":"PASS",
                    "new_sha_gate_old_codex":"EXPECTED_REJECT_EXIT_2",
                    "real_ten_kit_original_staging_and_rollback":"PASS",
                    "independent_ten_staged_prices":"PASS_10_OF_10",
                    "real_kit_rows":10,
                    "qa_G0":"B01_40_SOURCE_ACCEPT_PASS",
                    "b01_60_independent_G0":"PENDING",
                    "native_G1":"NOT_RUN",
                    "HQ_G2":"HOLD","CODEX_SENT":"NO","CODEX_ACK":"PENDING",
                    "GAME_INTEGRATED":False,"CANONICAL_GAME_FILES_MUTATED":False}
            files={"staged-original/"+p:raw for p,raw in staged_files.items()}
            files["source-receipt.json"]=canon(report)
            data=archive(files)
            need(data==archive(files),"NONDETERMINISTIC_ZIP")
            out.mkdir()
            (out/ARCHIVE).write_bytes(data)
            (out/"qa-source-receipt.json").write_bytes(canon({
                **report,"inner_archive_sha256":digest(data),
                "archive_member_sha256":{p:digest(v) for p,v in sorted(files.items())},
                "actual_archive_members":len(files)}))
            return {"receiver":ID,"original_author_node_pass":n,"author_python_pass":q,
                    "author_negative_rejects":7,"kit_offers_staged":10,
                    "old_new_BOM_prices":"IDENTICAL","new_codex_R3_gate":"PASS",
                    "old_codex_R3_gate":"EXPECTED_REJECT",
                    "source_G0":"PASS","native_G1":"NOT_RUN",
                    "inner_zip_sha256":digest(data)}
        finally:
            run(["git","-C",str(repo),"worktree","remove","--force",str(work)])
if __name__=="__main__":
    parser=argparse.ArgumentParser()
    parser.add_argument("--repo",required=True)
    parser.add_argument("--out",required=True)
    args=parser.parse_args()
    try:print(json.dumps(receive(args.repo,args.out),sort_keys=True))
    except (Reject,KeyError,ValueError,OSError,subprocess.TimeoutExpired) as e:
        print("B0140_W05_INDEPENDENT_SOURCE_REJECT "+str(e)+" UNITY_NATIVE_NOT_RUN",
              file=sys.stderr)
        sys.exit(2)
