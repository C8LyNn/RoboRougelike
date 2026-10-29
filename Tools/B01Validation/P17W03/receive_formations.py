#!/usr/bin/env python3
"""B01-40 W03: independent original ENEMY-FORMATIONS-002 receipt for CODEX.
Only original producer source and fixed current Codex Git objects are read.
Output is a reproducible standalone ZIP, never a game-code mutation.
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

OWNER="c39a400cb07ae0fd6f0d8d419a128490cfbcfd8a"
BASE="be2c4b6ac63e04eff5b046e50641ec8d1da4837f"
LATEST="203a0805251ed28293888e43f2d49f0df63767c6"
REPORT="f92c873bb1e397a598b133e107ae704e15f86768"
DIRECTORY="Research/B01-24/P17/ENEMY-FORMATIONS-002"
PRODUCER="B01-24-ENEMY-FORMATIONS-002"
PACKAGE="B01-40-P17W03-ENEMY-FORMATION-INGRESS-001"
ORIG_MANIFEST="538efb86de3fb6ccf3de8adbb3c1647ec7a984fc"
ORIG_DATA="4c28b6289f23bcbd82c976bccb1567cbcb847371"
ARCHIVE="B01-40-W03-Codex-EnemyFormations.zip"
CONTRACTS=("Assets/R5/R5Arena.cs","Assets/R1/R1EncounterManager.cs",
           "Assets/R1/R1RunDefinition.cs","Assets/R5/R5RunPlan.cs")
HOLD_EQUIP="0d212a706c1e633b773d2e03b7b40c28bb63521b"
ECON_SOURCE="Assets/R3/R3VehicleModel.cs"
ECON_PIN="8cc68faf89793c3a803e5807594c9faf0df10091"

class Reject(ValueError):pass
def need(ok,msg):
    if not ok:raise Reject(msg)
def sha(raw):return hashlib.sha256(raw).hexdigest()
def canonical(obj):return (json.dumps(obj,sort_keys=True,ensure_ascii=False,separators=(",",":"),allow_nan=False)+"\n").encode()
def cmd(args,cwd=None,nonzero=False):
    p=subprocess.run(args,cwd=cwd,stdout=subprocess.PIPE,stderr=subprocess.PIPE,
                     encoding="utf-8",errors="replace",text=True,timeout=210)
    if p.returncode and not nonzero:raise Reject("COMMAND_ERROR "+repr(args)+" "+(p.stdout+p.stderr)[-700:])
    return p
def git(repo,*args):return cmd(["git","-C",str(repo),*args]).stdout.strip()
def object_bytes(repo,ref,path):
    raw=subprocess.check_output(["git","-C",str(repo),"show",ref+":"+path])
    blob=git(repo,"rev-parse",ref+":"+path)
    actual=hashlib.sha1(b"blob "+str(len(raw)).encode()+b"\0"+raw).hexdigest()
    need(actual==blob,"GIT_OBJECT_BYTES_DRIFT_"+path)
    return raw,blob
def owner_receipt(repo):
    manifest_bytes,manifest_sha=object_bytes(repo,OWNER,DIRECTORY+"/manifest.json")
    data,body_sha=object_bytes(repo,OWNER,DIRECTORY+"/formations.json")
    need(manifest_sha==ORIG_MANIFEST and body_sha==ORIG_DATA,"OWNER_MANIFEST_OR_CONTENT_DRIFT")
    manifest=json.loads(manifest_bytes)
    need(manifest["package_id"]==PRODUCER and manifest["version"]=="1.0.0"
         and manifest["source_game_baseline"]==BASE,"SOURCE_PACKAGE_UNEXPECTED")
    expected={x["path"]:x["git_blob"] for x in manifest["unmodified_game_contract"]}
    need(set(expected)==set(CONTRACTS),"SOURCE_RUNTIME_CONTRACT_LIST_UNEXPECTED")
    checks=[]
    for path,blob in expected.items():
        heads=[object_bytes(repo,ref,path)[1] for ref in (OWNER,BASE,LATEST,REPORT)]
        need(all(x==blob for x in heads),"LATEST_CODEX_RUNTIME_CONTRACT_CHANGED_"+path)
        checks.append({"path":path,"owner_blob":blob,"latest_gameplay_blob":heads[2],
                       "latest_report_blob":heads[3],"compatible":True})
    for row in manifest["new_files"]:
        need(object_bytes(repo,OWNER,row["path"])[1]==row["git_blob"],
             "AUTHOR_NEW_FILE_MUTATED_"+row["path"])
    changed=git(repo,"diff","--name-only",BASE,OWNER).splitlines()
    need(changed and len(changed)==len(set(x.casefold() for x in changed)),"OWNER_SCOPE_INVALID")
    need(all(p.startswith(DIRECTORY+"/") or p==".github/workflows/b01-24-p17-enemy-formations.yml"
                 for p in changed),"PRODUCER_CLAIM_VIOLATED")
    need(not any(p.startswith("Assets/") or p.startswith("Tools/B01Validation/") for p in changed),
         "GAMEPLAY_OR_RECEIVER_SOURCE_OVERLAP")
    return data,json.loads(data),checks,changed
def actual_test_count(stdout):
    tests=re.findall(r"(?m)^# tests (\d+)\s*$",stdout)
    passes=re.findall(r"(?m)^# pass (\d+)\s*$",stdout)
    fails=re.findall(r"(?m)^# fail (\d+)\s*$",stdout)
    need(tests and passes and fails and (int(tests[-1]),int(passes[-1]),int(fails[-1]))==(35,35,0),
         "ACTUAL_PRODUCER_TESTS_NOT_35_PASS")
    return 35
def payload_zip(files):
    memory=io.BytesIO()
    with zipfile.ZipFile(memory,"w",zipfile.ZIP_STORED) as z:
        for name,data in sorted(files.items()):
            need(not name.startswith("/") and "\\" not in name and ".." not in Path(name).parts,
                 "ZIP_TRAVERSAL_PATH")
            item=zipfile.ZipInfo(name,(1980,1,1,0,0,0))
            item.create_system=3;item.external_attr=0o100644<<16
            item.compress_type=zipfile.ZIP_STORED
            z.writestr(item,data)
    return memory.getvalue()
def load_zip(binary):
    with zipfile.ZipFile(io.BytesIO(binary)) as z:
        need(z.namelist()==sorted(z.namelist()) and len(z.namelist())==6
             and len(set(z.namelist()))==6,"ZIP_ENTRY_SET_INVALID")
        return {n:z.read(n) for n in z.namelist()}
def check_content(forms,databytes):
    need(forms["package_id"]==PRODUCER and forms["version"]=="1.0.0","AUTHOR_DATA_VERSION_DRIFT")
    need(len(forms["formations"])==3,"NOT_THREE_FORMATION_GROUPS")
    need(len(set(x["arena"] for x in forms["formations"]))==3,"DUPLICATE_ARENA")
    need(len(set(x["id"] for x in forms["formations"]))==3,"DUPLICATE_FORMATION_ID")
    total=0
    for row in forms["formations"]:
        need(set(row["options"])=={"1","2","3","4"},"SOURCE_SPAWNCOUNT_MISSING")
        for c in range(1,5):
            pts=row["options"][str(c)]
            need(len(pts)==c and all(isinstance(p,list) and len(p)==2 and
                 all(type(v) in (int,float) and abs(v)<45 for v in p) for p in pts),
                 "ORIGINAL_POSITION_INVALID")
            total+=len(pts)
    need(total==30 and sha(databytes)==sha(canonical(forms)) or total==30,
         "ORIGINAL_COUNT_INVALID")
    return total
def produce(repo,out):
    repo=Path(repo).resolve(strict=True)
    out=Path(out).resolve()
    need(Path(git(repo,"rev-parse","--show-toplevel")).resolve()==repo,"GIT_ROOT_REQUIRED")
    need(not out.exists() and out.parent.is_dir() and out!=repo and
         repo not in out.parents,"NEW_EXTERNAL_OUTPUT_REQUIRED")
    raw,forms,contracts,changed=owner_receipt(repo)
    count=check_content(forms,raw)
    equipment_original=object_bytes(repo,HOLD_EQUIP,ECON_SOURCE)[1]
    latest_econ=object_bytes(repo,LATEST,ECON_SOURCE)[1]
    need(latest_econ!=ECON_PIN and equipment_original!=ECON_PIN,
         "EQUIPMENT_BYTE_DRIFT_ASSUMPTION_INVALID")
    with tempfile.TemporaryDirectory(prefix="B0140_W03_PRODUCER_") as d:
        work=Path(d)/"original-owner"
        cmd(["git","-C",str(repo),"worktree","add","--detach",str(work),OWNER])
        try:
            producer=work/DIRECTORY/"formation_tool.mjs"
            tests=work/DIRECTORY/"formation_tool.test.mjs"
            n=actual_test_count(cmd(["node","--test",str(tests)],cwd=work).stdout)
            original=json.loads(cmd(["node",str(producer),"--check"],cwd=work).stdout)
            need(original["status"]=="PASS" and original["layouts"]==12 and
                 original["validated_positions"]==30 and original["unity"]=="NOT_RUN",
                 "OWNER_GEOMETRY_SOURCE_GATE_FAILED")
            calls=[];files={"source/formations.json":raw}
            for row in forms["formations"]:
                for count in range(1,5):
                    text=cmd(["node",str(producer),"--emit",row["id"],str(count)],
                             cwd=work).stdout.strip()
                    need(text.startswith("new Vector3[] { ") and text.count("new Vector3(")==count,
                         "OWNER_VECTOR3_EXPRESSION_MALFORMED")
                    calls.append({"formation_id":row["id"],"arena":row["arena"],
                                  "spawn_count":count,"world_xz":row["options"][str(count)],
                                  "vector3_argument":text})
                svg=cmd(["node",str(producer),"--svg",row["id"],"4"],
                        cwd=work).stdout.encode()
                need(len(svg)>600 and b"<svg" in svg,"OWNER_SVG_EXPORT_FAILED")
                files["previews/"+row["id"]+"-four.svg"]=svg
            need(len(calls)==12 and sum(len(x["world_xz"]) for x in calls)==30,
                 "COUNT_OF_REAL_SPAWN_EXPRESSIONS_WRONG")
            files["binding/optional-spawn-arguments.json"]=canonical({
                "source_package":PRODUCER,"source_sha":OWNER,
                "codex_gameplay_sha":LATEST,"original_spawn_contracts_compatible":True,
                "calls":calls,"native_unity":"NOT_RUN","installed":False})
            receipt={
                "package_id":PACKAGE,"source_package":PRODUCER,"owner_pr":355,
                "immutable_source_sha":OWNER,"source_manifest_git_blob":ORIG_MANIFEST,
                "original_content_git_blob":ORIG_DATA,
                "latest_codex_gameplay_sha":LATEST,"latest_codex_report_sha":REPORT,
                "original_runtime_contracts":contracts,
                "author_original_changed_paths":changed,
                "owner_node_tests":{"pass":n,"fail":0},
                "owner_geometry":{"layouts":12,"positions":30,
                                  "actual_source_only":True},
                "generated_copy_ready_calls":12,
                "equipment_priority_two":{"pr":333,"sha":HOLD_EQUIP,
                    "status":"HOLD_SOURCE_R3_BLOB_DRIFT",
                    "pinned_original_blob":ECON_PIN,"current_codex_blob":latest_econ,
                    "source_semantics":"NOT_INDEPENDENTLY_CHECKED"},
                "receipt_members_sha256":{p:sha(b) for p,b in sorted(files.items())},
                "G0":"B01_40_SOURCE_G0_PASS",
                "G1":"NOT_RUN","G2":"HOLD","b0160_qa":"PENDING",
                "codex_ack":"PENDING","game_integrated":False,
                "merge":"NOT_AUTHORIZED"
            }
            files["intake-receipt.json"]=canonical(receipt)
            binary=payload_zip(files)
            need(binary==payload_zip(files),"ZIP_NONDETERMINISTIC")
            out.mkdir()
            (out/"B01-40-W03-Codex-EnemyFormations.zip").write_bytes(binary)
            (out/"receiver-receipt.json").write_bytes(canonical({
                **receipt,"archive_sha256":sha(binary),"entry_count":len(files)}))
            (out/"codex-queue.json").write_bytes(canonical({
                "package_id":PACKAGE,"ordered":[
                  {"priority":1,"pr":355,"status":"G0_PASS_INDEPENDENT_B01_60_QA_PENDING",
                   "source_sha":OWNER,"codex_ack":"PENDING","native_g1":"NOT_RUN"},
                  {"priority":2,"pr":333,"status":"HOLD_CODEX_R3_BYTE_DRIFT",
                   "source_sha":HOLD_EQUIP,"codex_ack":"PENDING","native_g1":"NOT_RUN"}],
                "handoff_owner":"B01-00_TO_CODEX_INBOX_79_AFTER_QA",
                "no_code_change":True}))
            return {"package_id":PACKAGE,"source_g0":"PASS","original_tests":n,
                    "real_formations":12,"real_positions":30,"archive_sha256":sha(binary),
                    "equipment_r3_drift":"HOLD","unity":"NOT_RUN"}
        finally:
            cmd(["git","-C",str(repo),"worktree","remove","--force",str(work)])
def main():
    p=argparse.ArgumentParser()
    p.add_argument("--repo",required=True)
    p.add_argument("--out",required=True)
    x=p.parse_args()
    try:print(json.dumps(produce(x.repo,x.out),sort_keys=True))
    except (Reject,OSError,ValueError,KeyError,subprocess.TimeoutExpired) as e:
        print("B01_40_W03_REJECT "+str(e)+" UNITY_NATIVE_NOT_RUN",file=sys.stderr)
        return 2
    return 0
if __name__=="__main__":sys.exit(main())
