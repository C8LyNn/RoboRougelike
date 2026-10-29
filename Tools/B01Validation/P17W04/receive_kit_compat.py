#!/usr/bin/env python3
"""Independent B01-40 original B01-22 v1.0.2 intake and strict R3 semantic drift proof.

Never changes B01-22, Codex or Assets. SHA gate stays RED until owner re-pins.
Run actual ORIGINAL author tests and original real 10-offer staging in detached worktree.
"""
import argparse
import hashlib
import importlib.util
import io
import json
from pathlib import Path
import re
import subprocess
import sys
import tempfile
import zipfile

PACKAGE="B01-40-P17W04-KIT-R3-COMPAT-RECEIVER-001"
OWNER="0d212a706c1e633b773d2e03b7b40c28bb63521b"
BASE="5f06af81e2dd9d877e4300259bb3dd008cb9c9d1"
PINNED="aa7a917f177a9b1f07203868fb7d1d871eb3ee46"
GAME="203a0805251ed28293888e43f2d49f0df63767c6"
REPORT="f92c873bb1e397a598b133e107ae704e15f86768"
FOLDER="Research/B01-22/P17/EQUIPMENT-KITS-001"
MANIFEST_BLOB="4b876414b904cddb7c5638c5ed53a68d0891d68e"
R3="Assets/R3/R3VehicleModel.cs"
R4="Assets/R4/R4WeaponDefinition.cs"
R3_OLD_BLOB="8cc68faf89793c3a803e5807594c9faf0df10091"
R3_NEW_BLOB="1d0f378dc598e08b3d7c16975f419d84231511bb"
R4_BLOB="502f6d7f70d4e8078260de632ab181f244a4d730"
OLD=b"public sealed class R3VehicleModel"
NEW=b"public sealed partial class R3VehicleModel"
ZIPNAME="B01-40-W04-Original-Kit-Offers-QA-ONLY.zip"
class Reject(ValueError): pass
def need(cond,msg):
    if not cond: raise Reject(msg)
def digest(raw):return hashlib.sha256(raw).hexdigest()
def jsonbytes(obj):return (json.dumps(obj,ensure_ascii=False,sort_keys=True,separators=(",",":"),allow_nan=False)+"\n").encode()
def run(argv,cwd=None,allow_failure=False):
    q=subprocess.run(argv,cwd=cwd,stdout=subprocess.PIPE,stderr=subprocess.PIPE,text=True,
                     encoding="utf-8",errors="replace",timeout=240)
    if q.returncode and not allow_failure:raise Reject("ORIGINAL_COMMAND_FAILED:"+repr(argv)+" "+(q.stdout+q.stderr)[-1200:])
    return q
def git(repo,*args):return run(["git","-C",str(repo),*args]).stdout.strip()
def blob(repo,ref,path):
    v=subprocess.check_output(["git","-C",str(repo),"show",ref+":"+path])
    actual=git(repo,"rev-parse",ref+":"+path)
    expected=hashlib.sha1(b"blob "+str(len(v)).encode()+b"\0"+v).hexdigest()
    need(actual==expected,"GIT_SHA_BYTES_NOT_MATCHING:"+path)
    return v,actual
def node_count(log):
    a=re.findall(r"(?m)^# tests (\d+)\s*$",log)
    b=re.findall(r"(?m)^# pass (\d+)\s*$",log)
    c=re.findall(r"(?m)^# fail (\d+)\s*$",log)
    need(a and b and c and (int(a[-1]),int(b[-1]),int(c[-1]))==(26,26,0),"ORIGINAL_NODE_26_NOT_GREEN")
    return 26
def py_count(log):
    a=re.findall(r"Ran (\d+) tests? in",log)
    need(a and int(a[-1])==18 and re.search(r"(?m)^OK\s*$",log),"ORIGINAL_PYTHON_18_NOT_GREEN")
    return 18
def newzip(files):
    memory=io.BytesIO()
    with zipfile.ZipFile(memory,"w",compression=zipfile.ZIP_STORED) as z:
        for name,raw in sorted(files.items()):
            need(not name.startswith("/") and "\\" not in name and ".." not in Path(name).parts,"UNSAFE_ZIP_MEMBER")
            info=zipfile.ZipInfo(name,(1980,1,1,0,0,0))
            info.create_system=3
            info.external_attr=0o100644<<16
            z.writestr(info,raw)
    return memory.getvalue()
def validate_source(repo):
    raw,hashid=blob(repo,OWNER,FOLDER+"/manifest.json")
    need(hashid==MANIFEST_BLOB,"ORIGINAL_MANIFEST_GIT_SHA_CHANGED")
    manifest=json.loads(raw)
    need(manifest["package_id"]=="B01-22-EQUIPMENT-KITS-001" and
         manifest["revision"]=="1.0.2" and len(manifest["files"])==13,
         "ORIGINAL_PACKAGE_CONTRACT_CHANGED")
    paths=[]
    for entry in manifest["files"]:
        path=entry["path"]
        need(path.startswith(FOLDER+"/") and path not in paths and
             ".." not in Path(path).parts,"ORIGINAL_UNSAFE_PAYLOAD_PATH")
        data,_=blob(repo,OWNER,path)
        need(len(data)==entry["bytes"] and digest(data)==entry["sha256"],
             "ORIGINAL_PAYLOAD_DIGEST_MISMATCH:"+path)
        paths.append(path)
    changes=git(repo,"diff","--name-only",BASE,OWNER).splitlines()
    need(changes and all(x.startswith(FOLDER+"/") or
                        x==".github/workflows/b01-22-p17-equipment-kits.yml" for x in changes),
         "ORIGINAL_PRODUCER_SCOPE_VIOLATION")
    original,old=blob(repo,PINNED,R3)
    owner,owner_r3=blob(repo,OWNER,R3)
    current,new=blob(repo,GAME,R3)
    latest,report_r3=blob(repo,REPORT,R3)
    need(old==R3_OLD_BLOB and owner_r3==R3_OLD_BLOB and
         new==R3_NEW_BLOB and report_r3==R3_NEW_BLOB,
         "EXPECTED_EXACT_CODEX_R3_SHA_DRIFT_CHANGED")
    need(original==owner and current==latest and
         original.count(OLD)==1 and current.count(NEW)==1 and
         len(current)==len(original)+8 and original.replace(OLD,NEW,1)==current,
         "UNAPPROVED_R3_DIFFERENCE_BEYOND_PARTIAL_KEYWORD")
    oldr4,oldr4id=blob(repo,PINNED,R4)
    r4,r4id=blob(repo,GAME,R4)
    report_r4,report_r4id=blob(repo,REPORT,R4)
    need(oldr4id==r4id==report_r4id==R4_BLOB and oldr4==r4==report_r4,
         "UNAPPROVED_R4_PRICE_ENUM_DRIFT")
    catalogue=json.loads(blob(repo,OWNER,FOLDER+"/catalog.json")[0])
    need(catalogue["provenance"]["codex_sha"]==PINNED and
         catalogue["provenance"]["r3_blob"]==R3_OLD_BLOB and
         catalogue["provenance"]["r4_blob"]==R4_BLOB and
         len(catalogue["kits"])==10,"ORIGINAL_KIT_PRICE_PROVENANCE_CHANGED")
    return {"owner_manifest_git_blob":hashid,"manifest_original_sha256":digest(raw),
            "13_payloads_sha256_verified":True,"original_scope_paths":changes,
            "original_r3_blob":old,"latest_r3_blob":new,"r4_blob":r4id,
            "r3_only_diff":"EXACT_ADD_8_BYTES_PARTIAL_KEYWORD",
            "latest_gameplay_sha":GAME,"latest_report_sha":REPORT},catalogue,current,r4
def receive(repo,out):
    repo=Path(repo).resolve(strict=True);out=Path(out).resolve()
    need(Path(git(repo,"rev-parse","--show-toplevel")).resolve()==repo,"GIT_ROOT_REQUIRED")
    need(not out.exists() and out.parent.is_dir() and out!=repo and repo not in out.parents,
         "OUTPUT_MUST_BE_NEW_OUTSIDE_GIT")
    source,catalogue,r3,r4=validate_source(repo)
    with tempfile.TemporaryDirectory(prefix="b0140_w04_kit_") as tmp:
        tmp=Path(tmp)
        work=tmp/"original-owner"
        run(["git","-C",str(repo),"worktree","add","--detach",str(work),OWNER])
        try:
            folder=work/FOLDER
            n=node_count(run(["node","--test",str(folder/"kit_core.test.mjs")],cwd=work).stdout)
            p=run([sys.executable,"-m","unittest","discover","-s",str(folder),
                   "-p","test_stage_kit_payload.py","-v"],cwd=work)
            m=py_count(p.stderr)
            verify=run([sys.executable,str(folder/"stage_kit_payload.py"),"--verify-only"],cwd=work)
            need(json.loads(verify.stdout)["status"]=="VERIFIED","ORIGINAL_STAGING_VERIFY_FAILED")
            staging=tmp/"staged-kit-offers"
            run([sys.executable,str(folder/"stage_kit_payload.py"),"--stage",str(staging)],cwd=work)
            receipt=json.loads((staging/"receipt.json").read_bytes())
            offers=json.loads((staging/"offers.json").read_bytes())
            need(len(offers["offers"])==10 and offers["data_only"] is True and
                 offers["host_transaction_required"] is True and receipt["modified_host"] is False,
                 "REAL_AUTHOR_STAGING_RESULT_INVALID")
            origfiles={n:(staging/n).read_bytes() for n in ("catalog.json","offers.json","receipt.json")}
            run([sys.executable,str(folder/"stage_kit_payload.py"),"--rollback",str(staging)],cwd=work)
            need(not staging.exists(),"REAL_AUTHOR_ROLLBACK_FAILED")
            # The original pin verifier must continue to fail on newly changed Git bytes.
            latest=run([sys.executable,str(folder/"verify_host_source.py"),
                        "--current-ref",GAME],cwd=work,allow_failure=True)
            need(latest.returncode==2 and "LATEST_CODEX_BLOB_DRIFT" in latest.stderr,
                 "ORIGINAL_SHA_GATE_NOT_FAIL_CLOSED")
            old=run([sys.executable,str(folder/"verify_host_source.py"),
                     "--current-ref",PINNED,"--selftest"],cwd=work)
            old_receipt=json.loads(old.stdout)
            need(old_receipt["result"]=="PASS" and len(old_receipt["rejected_mutations"])==7,
                 "AUTHOR_ORIGINAL_SEVEN_NEGATIVES_NOT_RUN")
            spec=importlib.util.spec_from_file_location("author_gate",folder/"verify_host_source.py")
            module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module)
            need(module.check(catalogue,r3.decode(),r4.decode())==10,
                 "ACTUAL_CURRENT_CODEX_ENUM_PRICE_SEMANTICS_NOT_PASS")
            mutated=r3.decode().replace("R3PartKind.Booster=>35","R3PartKind.Booster=>39",1)
            need(mutated!=r3.decode(),"PRICE_MUTATION_TARGET_MISSING")
            try:module.check(catalogue,mutated,r4.decode())
            except ValueError:pass
            else:raise Reject("INDEPENDENT_PRICE_NEGATIVE_FALSE_GREEN")
            output={
                "package_id":PACKAGE,"original_producer_pr":333,
                "original_producer_sha":OWNER,"latest_codex_gameplay_sha":GAME,
                "source_checks":source,"author_original_node_pass":n,
                "author_original_python_pass":m,
                "author_source_selftest_negatives":len(old_receipt["rejected_mutations"]),
                "author_pin_gate_current_exit":latest.returncode,
                "author_pin_gate_current_reason":"LATEST_CODEX_BLOB_DRIFT",
                "original_author_stage_and_rollback":"PASS_REAL_BYTES",
                "kits":10,
                "current_r3_r4_author_semantic_price_enum_check":"PASS_10_OF_10",
                "added_independent_current_price_negative":"REJECTED",
                "producer_frozen_sha_gate":"REMAINS_FAIL_CLOSED",
                "recommendation":"OWNER_B01_22_REPIN_IN_NEW_IMMUTABLE_VERSION_ONLY_WITH_HQ_CODEX_APPROVAL",
                "original_producer_version":"1.0.2","native_unity":"NOT_RUN",
                "b0160_qa":"PENDING","codex_ack":"PENDING","G2":"HOLD",
                "direct_game_import_authorized":False}
            files={"staged/"+n:raw for n,raw in sorted(origfiles.items())}
            files["receiver-compatibility-receipt.json"]=jsonbytes(output)
            zipped=newzip(files)
            need(zipped==newzip(files),"ZIP_NOT_DETERMINISTIC")
            out.mkdir()
            (out/ZIPNAME).write_bytes(zipped)
            (out/"compatibility-receipt.json").write_bytes(jsonbytes({**output,
                "qa_only_zip_sha256":digest(zipped),
                "qa_only_zip_files":len(files),
                "payload_sha256":{n:digest(v) for n,v in files.items()}}))
            return {"source_g0":"COMPATIBILITY_PROVEN_SHA_GATE_STILL_HOLD",
                    "original_node":n,"original_python":m,
                    "original_bad_inputs":len(old_receipt["rejected_mutations"]),
                    "actual_offers":len(offers["offers"]),
                    "current_game_semantic_prices":"PASS_10",
                    "original_pinned_gate":"EXPECTED_FAIL",
                    "qa_zip_sha256":digest(zipped),"unity":"NOT_RUN"}
        finally:
            run(["git","-C",str(repo),"worktree","remove","--force",str(work)])
def main():
    parser=argparse.ArgumentParser()
    parser.add_argument("--repo",required=True)
    parser.add_argument("--out",required=True)
    a=parser.parse_args()
    try:print(json.dumps(receive(a.repo,a.out),sort_keys=True))
    except (Reject,ValueError,KeyError,OSError,subprocess.TimeoutExpired) as exc:
        print("B0140_W04_KIT_REJECT "+str(exc)+" UNITY_NATIVE_NOT_RUN",file=sys.stderr)
        return 2
    return 0
if __name__=="__main__":sys.exit(main())
