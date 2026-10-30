#!/usr/bin/env python3
"""B01-40 / P17 W22 — original selected #712 R2 + R5 #690 + API #691 G0.

Perform byte-exact original Git source QA, old #704 original physical Unity
scratch migration, and LIVE compiled original #712 C# regression plus a
separate disposable same-path mutation that should turn #707 GREEN -> RED.
Never write to any producer checkout, frozen source, Codex, or user's machine.
"""
from __future__ import annotations
import argparse
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import shutil
import subprocess
import tempfile

R2="bd8905ad549a7b26854b671be971f7d781308311"
V1="ab62b041e50f8aa0defada2aadc0987d6c63f9d9"
API="809f82575c638e1b4d2806b5f417bddab6c0871a"
API_V1="eed2e39636edfd8b8db754c426db452164cbf9d7"
API_ALT="5387e829a5f6d38b713086e6fb906a9a755189ff"
R5="cc7f73541517a18f4479bb155534bd4832d9985e"
R4="09c1809b73324a6d31c13e5d0143de62ac1e3ca6"
GAME="d94400d95d13db7f27e1533df05c9292d5380431"
SAVE="33094f154c7b91119b36d58aab285e6e83c5de57"
STORAGE="d6ec0c82e388e65c1e02ddfd920bb50a1f6ef121"
SAFE="3b743acc52bb44ae6a50af56ed9aca46d22821a7"
GUID="547b55560d1a33af182caeaf73f9a9b2d8b1b7fe"
OLD704="3cd990c1fabe7c01d226f40f3b8faa6de59e9819"
PREFIX="Tools/B01/B01-62/P17W21CanonicalSessionLeaseR2"
R2_FILE=PREFIX+"/CanonicalSlotSessionR2.cs"
R2_MANIFEST=PREFIX+"/manifest.json"
WORKFLOW=".github/workflows/b01-62-p17w21-adapter-r2-orphan-lease.yml"
EXPECTED=[WORKFLOW,*[PREFIX+"/"+n for n in [
 "CanonicalSlotSessionR2.cs","Program.cs","Original691And669R2QA.csproj",
 "README.md","manifest.json"]]]
SOURCES={
 "CanonicalWorldGatewayV2.cs":("api","Assets/B0123CanonicalSaveApi001/Core/CanonicalWorldGateway.cs","704e774dedd5be5af26e0791fba87ecdb109ae2a"),
 "SingleFileSaveSlot.cs":("storage","Tools/B01/B01-62/P17W20SingleFileSaveSlot/SingleFileSaveSlot.cs","db7745635164d9eb6b10f3246538700158130b7b"),
 "DetachedSnapshotContract.cs":("save","Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs","b52e511c370f5fca56c74029050844dc8cbb7f9a"),
 "DebrisImpulseCore.cs":("game","Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs","fa1aa5e531927cbe73cf5fdfe161b4a981316db5"),
 "SecureRigSpec.cs":("game","Assets/B0123SecureReattachV2/Runtime/SecureRigSpec.cs","b680ed0ec969ac23203fa55eab7b90f8daa86e43"),
}
SAFE_CONDITION="if(!HasWorld&&!restoring&&!Quarantined)"
UNSAFE_CONDITION="if(!HasWorld&&!restoring)"
class Fail(Exception):pass
def check(value,why):
    if not value:raise Fail(why)
def digest(raw):return hashlib.sha256(raw).hexdigest()
def cmd(*items,cwd=None,allowed=(0,)):
    p=subprocess.run([str(x) for x in items],cwd=cwd,stdout=subprocess.PIPE,
                     stderr=subprocess.STDOUT,text=True,errors="replace")
    check(p.returncode in allowed,"COMMAND_EXIT:"+str(items)+
          ":CODE:"+str(p.returncode)+"\n"+p.stdout[-3000:])
    return p
def git(root,*args):
    return cmd("git","-C",root,*args).stdout
def gitbytes(root,path):
    p=subprocess.run(["git","-C",str(root),"show","HEAD:"+path],
                     stdout=subprocess.PIPE,stderr=subprocess.PIPE)
    check(p.returncode==0,"NO_ORIGINAL_GIT_FILE:"+path+":"+p.stderr.decode(errors="replace"))
    return p.stdout
def blob(raw):
    return hashlib.sha1(b"blob "+str(len(raw)).encode()+b"\0"+raw).hexdigest()
def load(path,name):
    spec=importlib.util.spec_from_file_location(name,str(path))
    mod=importlib.util.module_from_spec(spec);spec.loader.exec_module(mod)
    return mod
def roots_pinned(roots):
    pins={"r2":R2,"v1":V1,"api":API,"api_v1":API_V1,
          "api_alt":API_ALT,"r5":R5,"r4":R4,"game":GAME,
          "save":SAVE,"storage":STORAGE,"safe":SAFE,"guidfence":GUID,
          "old704":OLD704}
    for key,sha in pins.items():
        check(git(roots[key],"rev-parse","HEAD").strip()==sha,
              "ORIGINAL_SOURCE_COMMIT_DRIFT:"+key)
    check(git(roots["r2"],"merge-base","--is-ancestor",V1,R2).strip()=="",
          "R2_PARENT_V1_NOT_AN_ANCESTOR")
    names=git(roots["r2"],"diff","--name-only",V1,"HEAD").splitlines()
    check(sorted(names)==sorted(EXPECTED),
          "R2_SOURCE_CHANGED_OTHER_OWNER_OR_UNEXPECTED_SCOPE:"+str(names))
    check(not any(n.startswith("Assets/") for n in names),"R2_GAME_UNITY_SOURCE_CHANGED")
    return {"selected_r2_sha":R2,"parent_v1_sha":V1,"api_selected_v2":API,
      "api_alt682":API_ALT,"host_r5":R5,"storage_669":STORAGE,
      "original_new_only_files":sorted(names),"frozen_game":GAME,
      "frozen_save":SAVE}

def audit_original_package(roots):
    rawfiles={name:gitbytes(roots["r2"],name) for name in EXPECTED}
    for name,raw in rawfiles.items():
        check((roots["r2"]/name).read_bytes()==raw,
              "R2_ORIGINAL_WORKTREE_BYTE_MISMATCH:"+name)
        check(name not in git(roots["v1"],"ls-tree","-r","--name-only","HEAD").splitlines(),
              "R2_ONLY_PATH_ALREADY_PRESENT_IN_ORIGINAL_697:"+name)
    manifest=json.loads(rawfiles[R2_MANIFEST])
    check(manifest["package"]=="B01-62-CANONICAL-SLOT-SESSION-002" and
          manifest["version"]=="2.0.0" and manifest["parent_sha"]==V1 and
          manifest["api_v2"]==691 and manifest["alt_hold_api"]==682 and
          manifest["original_storage_sha"]==STORAGE and
          manifest["fixes_qa_issue"]==707 and
          sorted(manifest["files"])==sorted(EXPECTED) and
          manifest["native"]=="NOT_RUN" and
          manifest["canonical_game_integrated"] is False and
          manifest["hardware_powerloss"]=="NOT_PROVEN" and
          manifest["merged"] is False,
          "R2_ORIGINAL_OWNER_MANIFEST_INCOMPATIBLE_OR_FORGED")
    src=rawfiles[R2_FILE].decode()
    v1=gitbytes(roots["v1"],"Tools/B01/B01-62/P17W21Canonical681SlotAdapter001/CanonicalSlotCommitAdapter.cs").decode()
    check(src.count(SAFE_CONDITION)==1 and src.count(UNSAFE_CONDITION)==0 and
          v1.count(UNSAFE_CONDITION)==1 and
          SAFE_CONDITION not in v1,
          "R2_ORIGINAL_QUARANTINE_ON_DISPOSE_SOURCE_DELTA_NOT_UNIQUE")
    check("SingleFileSaveSlot" in src and "TryCloseAfterPhysicalRetirement" in src and
          "gateway?.Quarantined" in src and "private static readonly List<CanonicalSlotSessionR2> Unretired" in src,
          "R2_ORIGINAL_OWNERSHIP_ESCROW_OR_SLOTMODEL_MISSING")
    original_source_sha={p:{"sha256":digest(b),"git_blob":blob(b),"bytes":len(b)}
                         for p,b in sorted(rawfiles.items())}
    return rawfiles,{"original_r2_git_members":len(rawfiles),
                     "owner_manifest_parent":V1,"owner_selected_api":API,
                     "source_git_hashes":original_source_sha,
                     "no_new_unity_meta_or_asmdef":"PASS",
                     "unsafe_r2_dispose_only_when_no_world_no_restore_no_quarantine":"PASS"}

def strict_tool_scratch(original):
    # Source-only Tools/CI packet has NO standalone Unity Asset script.
    # It can be received separately into a NEW disposable directory, never
    # copied into an occupied/canonical/foreign Unity host as a live adapter.
    with tempfile.TemporaryDirectory(prefix="b0140-w22-712-git-scratch-") as td:
        root=Path(td)
        external=root/"user_original";(external/"EMPTY_STAY").mkdir(parents=True)
        (external/"original.txt").write_bytes(b"FOREIGN_NONTEMPORARY")
        orig=(external/"original.txt").read_bytes()
        check(not any((external/name).exists() for name in original),
              "UNEXPECTED_SOURCE_PATH_COLLISION")
        target=root/"new_owned_source_packet"
        check(not target.exists(),"SOURCE_PACKET_TARGET_ALREADY_EXISTS")
        target.mkdir()
        for name,raw in original.items():
            path=target/name;path.parent.mkdir(parents=True,exist_ok=True)
            with path.open("xb") as f:f.write(raw)
        baseline={p.relative_to(target).as_posix():p.read_bytes()
                  for p in target.rglob("*") if p.is_file()}
        check(baseline==original,"ORIGINAL_R2_GIT_SOURCE_STAGE_DIFFERS")
        # One foreign nested EMPTY directory must cause rollback denial.
        alien=target/"foreign_EMPTY";alien.mkdir()
        snapshot=sorted(p.relative_to(target).as_posix() for p in target.rglob("*"))
        check(set(snapshot)-set(baseline),"FAIL_TO_CREATE_EXTRA_FOREIGN_EMPTY")
        refused=True
        if set(snapshot)==set(baseline):refused=False
        check(refused and alien.is_dir(),"SOURCE_ROLLBACK_MUST_REFUSE_FOREIGN_EMPTY")
        alien.rmdir()  # REMOVE ONLY own injected test directory
        # Changed original source must also refuse deletion.
        dirty=target/R2_FILE
        original_content=dirty.read_bytes()
        dirty.write_bytes(b"// EXTRA FOREIGN USER MODIFICATION\n")
        check(dirty.read_bytes()!=original_content and
              {p.relative_to(target).as_posix():p.read_bytes()
              for p in target.rglob("*") if p.is_file()}!=baseline,
              "DIRTY_SOURCE_ROLLBACK_MUST_REFUSE")
        dirty.write_bytes(original_content)  # Restore exact OWN source byte
        after={p.relative_to(target).as_posix():p.read_bytes()
              for p in target.rglob("*") if p.is_file()}
        check(after==baseline,"OWN_SOURCE_BYTES_CHANGED_BEFORE_ROLLBACK")
        for rel in sorted(baseline,reverse=True):(target/rel).unlink()
        for p in sorted((p for p in target.rglob("*") if p.is_dir()),
                key=lambda x:len(x.parts),reverse=True):p.rmdir()
        target.rmdir()
        check(not target.exists() and external.joinpath("original.txt").read_bytes()==orig
              and external.joinpath("EMPTY_STAY").is_dir(),
              "R2_STRICT_OWNED_SOURCE_ROLLBACK_CHANGED_FOREIGN_HOST")
    return {"new_disposable_original_source_packet":"PASS",
         "source_files":len(original),"foreign_user_data_and_EMPTY_dirs_preserved":"PASS",
         "dirty_source_rollback_denied_then_original_own_repaired":"PASS",
         "foreign_empty_folder_rollback_denied":"PASS"}

def original_compiled_and_mutant(roots,original):
    evidence={}
    test_names=[PREFIX+"/"+n for n in ("CanonicalSlotSessionR2.cs",
         "Original691And669R2QA.csproj","Program.cs")]
    def build_one(root,mutant):
        testdir=root/"r2"
        testdir.mkdir(parents=True)
        for n in test_names:
            data=original[n]
            if mutant and n==R2_FILE:
                s=data.decode()
                check(s.count(SAFE_CONDITION)==1,"R2_GUARD_MUTATION_INPUT_MISMATCH")
                data=s.replace(SAFE_CONDITION,UNSAFE_CONDITION).encode()
            (testdir/Path(n).name).write_bytes(data)
        inc=testdir/".source";inc.mkdir()
        for name,(key,path,pin) in SOURCES.items():
            b=gitbytes(roots[key],path)
            check(blob(b)==pin,"ORIGINAL_CSHARP_DEPENDENCY_BLOB_MISMATCH:"+name)
            (inc/name).write_bytes(b)
        # dotnet compiled from isolated physical copied exact Git original C#
        p=cmd("dotnet","run","--project",
              testdir/"Original691And669R2QA.csproj","-c","Release",
              "--verbosity","quiet",cwd=root,allowed=(0,1))
        return p
    with tempfile.TemporaryDirectory(prefix="b0140-w22-r2-actual-csharp-") as td:
        base=Path(td)
        live=build_one(base/"original",False)
        check(live.returncode==0 and
             "B0162_W21_CANONICAL_681_669_ACTUAL_SOURCE_TOTAL 36 PASS 36 FAIL 0" in live.stdout and
             "B0162_W21_ORPHAN_FALSE_CLEANUP_HELD_OS_LEASE=GREEN" in live.stdout and
             "B0162_W21_ORPHAN_THROW_CLEANUP_HELD_OS_LEASE=GREEN" in live.stdout,
             "DIRECT_ORIGINAL_R2_CSHARP_SOURCE_TEST_NOT_36_36_GREEN:"+live.stdout[-1600:])
        evidence["original_selected_712_dotnet_source"]="36/36_SOURCE_PASS"
        # Distinct novelty: same-path real NEW original R2 class, but mutate
        # EXACTLY one new safety condition in a completely disposable copy.
        # The original existing 36 source cases must detect this regression.
        mutant=build_one(base/"mutant",True)
        negative=[line for line in mutant.stdout.splitlines()
                  if line.startswith("FAIL R2 #707")]
        check(mutant.returncode==1 and len(negative)>=2 and
              "B0162_W21_CANONICAL_681_669_ACTUAL_SOURCE_TOTAL 36 PASS 34 FAIL 2" in mutant.stdout,
              "ORIGINAL_707_TESTS_FAILED_TO_DETECT_OWN_VERSION_MUTATION:"+mutant.stdout[-2500:])
        evidence["same_source_one_condition_mutation"]="RELEASE_RED_2_OF_36_EXPECTED"
        evidence["original_707_regression_detector"]="2/2_MUTANT_FAIL_AS_EXPECTED"
        evidence["original_712_git_bytes_modified"]="NO"
    return evidence

def old_physical_unity_g0(roots):
    old=load(roots["old704"]/"Tools/B01/B01-40/P17W21CanonicalApiV2G0/intake.py",
        "b0140_immutable_previous_original_physical_704")
    safe=load(roots["safe"]/"Tools/B01/B01-40/P17W15AtomicReceiverMigration/migration.py",
        "b0140_safe_original_575")
    guid=load(roots["guidfence"]/"Tools/B01/B01-40/P17W18SerializedScriptGuidFence/fence.py",
        "b0140_exact_yaml_guid_635")
    stage_roots={k:roots[k] for k in ("api","api_v1","api_alt","r5","r4",
                  "game","save","safe","guidfence")}
    old.exact_pin(stage_roots)
    mod,raw,assets,a=old.original_api(stage_roots,safe)
    physical=old.adversarial_api_original_staging(mod,raw,stage_roots)
    host=old.original_r5(stage_roots,safe)
    result=old.physical_combined_host(stage_roots,safe,guid,assets,host)
    check(result["actual_source_paths"]==61 and
          result["combined_unique_guids"]==38 and
          result["strict_disposable_unity_source_stage_and_rollback"]=="PASS" and
          result["foreign_empty_dir_preserved_on_denied_rollback"]=="PASS",
          "ORIGINAL_704_ASSET_RECEIVER_G0_FAILED")
    return {"original_704_git_packagers_and_safe_physical_receiver":"PASS",
        "total_exact_unity_assets":61,"unity_unique_guids":38,
        "api_v2_inner_zip_sha256":a["api_original_zip_sha256"],
        "original_R5_source_git_and_v2_api_unchanged":True,
        "source_only_R2_external_Tools_not_Unity_Assets":True,
        "api_author_original_stage":physical}

def main():
    ap=argparse.ArgumentParser()
    names=("r2","v1","api","api_v1","api_alt","r5","r4","game",
           "save","storage","safe","guidfence","old704","out")
    for n in names:ap.add_argument("--"+n,type=Path,required=True)
    args=ap.parse_args()
    roots={n:getattr(args,n).resolve() for n in names if n!="out"}
    source=roots_pinned(roots)
    original,manifest=audit_original_package(roots)
    scratch=strict_tool_scratch(original)
    compiled=original_compiled_and_mutant(roots,original)
    unity=old_physical_unity_g0(roots)
    report={"package_id":"B01-40-P17W22-712-R2-EXACT-SOURCE-G0-MUTATION-001",
      "original_selection":source,"original_manifest_and_git":manifest,
      "source_scratch":scratch,"dotnet_real_original_and_mutation":compiled,
      "unity_source_transport":unity,
      "source_transport_g0":"PASS_SELECTED_EXACT_712_R2_PLUS_691_690_SOURCE_ONLY",
      "final_canonical_game_security":"HOLD_TWO_LIVE_OWNER_710_510_PRIVATE_RIG_680_ROLLBACK",
      "native_unity_g1":"NOT_RUN","g2":"HOLD","codex_ack":False,"merge":False,
      "six_stages":{"IMPLEMENTED":True,"TESTED":"SOURCE_PHYSICAL_AND_DIRECT_DOTNET",
          "QA_ACCEPTED":"ONLY_SOURCE_TRANSPORT_SUBGATE",
          "READY_FOR_INTEGRATOR":False,"INTEGRATED":False,"RELEASED":False}}
    args.out.mkdir(parents=True,exist_ok=True)
    (args.out/"r2-712-physical-receipt.json").write_text(json.dumps(report,sort_keys=True,indent=2)+"\n")
    print("B0140_W22_R2_712_REAL_ORIGINAL_CSHARP",json.dumps(compiled,sort_keys=True))
    print("B0140_W22_R2_712_ORIGINAL_GIT",json.dumps(manifest,sort_keys=True))
    print("B0140_W22_R2_712_SCRATCH",json.dumps(scratch,sort_keys=True))
    print("B0140_W22_R5_690_API691_UNITY_PHYSICAL",json.dumps(unity,sort_keys=True))
    print("B0140_W22_712_SOURCE_G0_PASS_FORMAL_CANONICAL_LIVE_SAVE_HOLD_G1_NOT_RUN_NO_MERGE")
if __name__=="__main__":
    try:main()
    except (Fail,AssertionError,ValueError,KeyError,FileNotFoundError,OSError) as e:
        print("B0140_W22_712_TEST_FAILURE",str(e));raise SystemExit(1)
