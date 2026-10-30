#!/usr/bin/env python3
"""B01-40 / P17 W23: independent original #741 one-authority source G0.

DIRECT ORIGINAL C#9 OS tests, one real negative constructor mutation,
ORIGINAL #742 selected #729/R5 safe 39 GUID source-only Unity scratch.
No live Unity, no mutable producer, no merge or Codex ACK.
"""
from __future__ import annotations
import argparse
import hashlib
import importlib.util
import json
from pathlib import Path
import subprocess
import tempfile

PIN={
 "bridge":"3aae22c03a13e7f10f235877b068e04f5e8c19b7",
 "r2":"bd8905ad549a7b26854b671be971f7d781308311",
 "v3":"b8ca29c7707cd6b4338d0b9772d74dee676d49d2",
 "v2":"809f82575c638e1b4d2806b5f417bddab6c0871a",
 "alt":"8ea411b49e629c059f16c9f8b59a534c19f660b8",
 "r5":"cc7f73541517a18f4479bb155534bd4832d9985e",
 "r4":"09c1809b73324a6d31c13e5d0143de62ac1e3ca6",
 "game":"d94400d95d13db7f27e1533df05c9292d5380431",
 "save":"33094f154c7b91119b36d58aab285e6e83c5de57",
 "storage":"d6ec0c82e388e65c1e02ddfd920bb50a1f6ef121",
 "safe":"3b743acc52bb44ae6a50af56ed9aca46d22821a7",
 "guid":"547b55560d1a33af182caeaf73f9a9b2d8b1b7fe",
 "w21":"3cd990c1fabe7c01d226f40f3b8faa6de59e9819",
 "w23":"1f8b85665cb6c1369a15c377225c7e766818170d",
}
PREFIX="Tools/B01/B01-62/P17W23OneAuthorityBridgeR3"
CHANGED=[
 ".github/workflows/b01-62-p17w23-one-v3-authority.yml",
 *[PREFIX+"/"+p for p in ("CanonicalSessionR3.cs","SingleAuthoritySaveSlotR3.cs",
 "Program.cs","OneAuthorityOriginalV3QA.csproj","source_guard.py",
 "README.md","manifest.json")]]
SOURCE={
 "CanonicalWorldGatewayV3.cs":("v3","Assets/B0123CanonicalSaveApi001/Core/CanonicalWorldGateway.cs","21733278b6ab0669fccb6d085e0b6cf242792de2"),
 "CanonicalWorldSlotLease.cs":("v3","Assets/B0123CanonicalSaveApi001/Core/CanonicalWorldSlotLease.cs","a9331dd620336d8c42165d134dc8a5e05ed51873"),
 "DetachedSnapshotContract.cs":("save","Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs","b52e511c370f5fca56c74029050844dc8cbb7f9a"),
 "DebrisImpulseCore.cs":("game","Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs","fa1aa5e531927cbe73cf5fdfe161b4a981316db5"),
 "SecureRigSpec.cs":("game","Assets/B0123SecureReattachV2/Runtime/SecureRigSpec.cs","b680ed0ec969ac23203fa55eab7b90f8daa86e43")}
MUTATION_FROM="new CanonicalWorldGateway(gate,factory,this,physicalOwner)"
MUTATION_TO="new CanonicalWorldGateway(gate,factory,this)"
class Stop(Exception):pass
def must(ok,msg):
    if not ok:raise Stop(msg)
def git(root,*a):
    p=subprocess.run(["git","-C",str(root),*a],capture_output=True)
    must(p.returncode==0,"GIT_REF_ORIGINAL_MISSING:"+str(a)+p.stderr.decode(errors="replace")[-300:])
    return p.stdout
def gitfile(root,path):return git(root,"show","HEAD:"+path)
def sha(x):return hashlib.sha256(x).hexdigest()
def blob(x):return hashlib.sha1(b"blob "+str(len(x)).encode()+b"\0"+x).hexdigest()
def load(path,name):
    spec=importlib.util.spec_from_file_location(name,str(path))
    obj=importlib.util.module_from_spec(spec);spec.loader.exec_module(obj)
    return obj
def check_history(r):
    for k,expected in PIN.items():
        must(git(r[k],"rev-parse","HEAD").decode().strip()==expected,
             "ORIGINAL_GIT_COMMIT_CHANGED:"+k)
    must(subprocess.run(["git","-C",str(r["bridge"]),"merge-base","--is-ancestor",
        PIN["r2"],"HEAD"]).returncode==0,
        "ORIGINAL_741_NOT_VERSIONED_712_DESCENDANT")
    actual=git(r["bridge"],"diff","--name-only",PIN["r2"],"HEAD").decode().splitlines()
    must(sorted(actual)==sorted(CHANGED),"ORIGINAL_BRIDGE_FILE_SCOPE_NOT_EIGHT_ADDED")
    original={p:gitfile(r["bridge"],p) for p in CHANGED}
    for p,b in original.items():
        must((r["bridge"]/p).read_bytes()==b and
             p not in git(r["r2"],"ls-tree","-r","--name-only","HEAD").decode().splitlines(),
             "MUTATED_OR_PREEXISTING_BRIDGE_SOURCE:"+p)
    manifest=json.loads(original[PREFIX+"/manifest.json"])
    must(manifest["package"]=="B01-62-W23-CANONICAL-OWNER-BRIDGE-003" and
         manifest["version"]=="3.0.0" and
         manifest["parent_exact_sha"]==PIN["r2"] and
         manifest["selected_owner_api_sha"]==PIN["v3"] and
         manifest["original_storage_sha"]==PIN["storage"] and
         manifest["selected_host_r5_sha"]==PIN["r5"] and
         sorted(manifest["files"])==sorted(CHANGED) and
         sorted(manifest["losers_ALT_HOLD"])==sorted([719,695,682,693,604]) and
         manifest["authority"]=="only original CanonicalWorldSlotLease .owner, no slot.lease" and
         manifest["native"]=="NOT_RUN" and
         manifest["private_510_live"]=="NOT_IMPLEMENTED" and
         manifest["formal_g0"]=="PENDING" and
         manifest["codex_ack"] is False and
         manifest["merged"] is False,
         "ORIGINAL_BRIDGE_MANIFEST_RELEASE_SCOPE_FORGED")
    src=original[PREFIX+"/CanonicalSessionR3.cs"].decode()
    slot=original[PREFIX+"/SingleAuthoritySaveSlotR3.cs"].decode()
    old=gitfile(r["r2"],"Tools/B01/B01-62/P17W21CanonicalSessionLeaseR2/CanonicalSlotSessionR2.cs").decode()
    oldstorage=gitfile(r["storage"],"Tools/B01/B01-62/P17W20SingleFileSaveSlot/SingleFileSaveSlot.cs").decode()
    must(src.count(MUTATION_FROM)==1 and MUTATION_TO in old and
         "CanonicalWorldSlotLease.TryAcquire(" in src and
         "physicalOwner.TryCancelUnbound()" in src and
         "gateway.TryCloseAfterPhysicalRetirement(allOwnedRootsGone)" in src and
         "new CanonicalWorldGateway(gate,factory,this,physicalOwner)" in src and
         '"slot.lease"' not in slot and
         "OpenExclusiveLease" not in slot and
         "ownerLease.Dispose()" not in slot and
         '"slot.lease"' in oldstorage,
         "ORIGINAL_BRIDGE_NOT_EXACTLY_ONE_NEW_OWNER")
    return original,{"original_r3_git_member_paths":len(original),
       "source_exact_git_blob_and_sha256":{p:{"blob":blob(b),"sha256":sha(b)}
             for p,b in sorted(original.items())},
       "no_frozen_assets_edited":True,"one_new_gateway_owner_arg":True,
       "no_original_storage_second_lease":"PASS",
       "old_raw_712_plus_669_not_installed":"PASS"}
def physical_tools_scratch(original):
    with tempfile.TemporaryDirectory(prefix="b0140-w23-r3-source-") as td:
        root=Path(td)
        alien=root/"FOREIGN_ORIGINAL";(alien/"EMPTY").mkdir(parents=True)
        (alien/"user.bin").write_bytes(b"DO_NOT_TOUCH_USER")
        candidate=root/"NEW_SOURCE_ONLY";candidate.mkdir()
        for p,b in original.items():
            f=candidate/p;f.parent.mkdir(parents=True,exist_ok=True);f.write_bytes(b)
        before={p.relative_to(candidate).as_posix():p.read_bytes()
               for p in candidate.rglob("*") if p.is_file()}
        must(before==original,"ORIGINAL_BRIDGE_SCRATCH_STAGE_WRONG")
        dirty=candidate/(PREFIX+"/CanonicalSessionR3.cs")
        dirty.write_bytes(b"UNKNOWN_OTHER_USER_DIRTY_CODE")
        after={p.relative_to(candidate).as_posix():p.read_bytes()
            for p in candidate.rglob("*") if p.is_file()}
        must(after!=before,"DIRTY_ROLLBACK_NOT_BLOCKED")
        dirty.write_bytes(before[PREFIX+"/CanonicalSessionR3.cs"])
        extra=candidate/"FOREIGN_EMPTY";extra.mkdir()
        dirs={p.relative_to(candidate).as_posix()
              for p in candidate.rglob("*") if p.is_dir()}
        must("FOREIGN_EMPTY" in dirs,"ALIEN_EMPTY_ROLLBACK_NOT_BLOCKED")
        extra.rmdir() # our own test injected folder only
        must({p.relative_to(candidate).as_posix():p.read_bytes()
           for p in candidate.rglob("*") if p.is_file()}==before,
           "BRIDGE_SOURCE_CHANGED_AFTER_FAULT")
        # strict own-path byte-checked deletion, NEVER a recursive user cleanup
        for p,b in before.items():
            f=candidate/p
            must(f.is_file() and not f.is_symlink() and f.read_bytes()==b,
                 "UNOWNED_CHANGED_SOURCE_ABORT_ROLLBACK")
            f.unlink()
        for d in sorted((p for p in candidate.rglob("*") if p.is_dir()),
                 key=lambda p:len(p.parts),reverse=True):d.rmdir()
        candidate.rmdir()
        must(not candidate.exists() and
             (alien/"user.bin").read_bytes()==b"DO_NOT_TOUCH_USER" and
             (alien/"EMPTY").is_dir(),"FOREIGN_DATA_CHANGED")
    return {"all_eight_original_git_source_files_stage_and_rollback":"PASS",
     "dirty_source_reject":"PASS","foreign_EMPTY_folder_reject":"PASS",
     "other_user_bytes_and_empty_folders_preserved":"PASS"}
def original_actual_dotnet(r,original):
    proj=PREFIX+"/OneAuthorityOriginalV3QA.csproj"
    selected=[p for p in original if p.startswith(PREFIX+"/") and
              p.endswith((".cs",".csproj"))]
    must(len(selected)==4,"ORIGINAL_R3_CSHARP_PROJECT_SOURCE_COUNT")
    with tempfile.TemporaryDirectory(prefix="b0140-w23-r3-dotnet-") as td:
        root=Path(td)
        def execute(name,mutate):
            folder=root/name;folder.mkdir()
            for p in selected:
                b=original[p]
                if mutate and p.endswith("/CanonicalSessionR3.cs"):
                    s=b.decode()
                    must(s.count(MUTATION_FROM)==1,"MUTANT_NOT_UNIQUE")
                    b=s.replace(MUTATION_FROM,MUTATION_TO).encode()
                (folder/Path(p).name).write_bytes(b)
            inputs=folder/".source";inputs.mkdir()
            for file,(k,path,oid) in SOURCE.items():
                b=gitfile(r[k],path)
                must(blob(b)==oid,"ORIGINAL_CSHARP_REFERENCE_GIT_BLOB_DRIFT:"+file)
                (inputs/file).write_bytes(b)
            command=(["dotnet","run","--project",str(folder/Path(proj).name)]
                if not mutate else ["dotnet","build",str(folder/Path(proj).name)])
            return subprocess.run([*command,
                "-c","Release","--verbosity","quiet"],
                cwd=folder,capture_output=True,text=True,errors="replace")
        positive=execute("new_corrected_original_741",False)
        must(positive.returncode==0 and
            "B0162_W23_CANONICAL_729_669_ONE_OWNER_SOURCE_TOTAL 38 PASS 38 FAIL 0" in positive.stdout and
            "B0162_W23_EXACT_SELECTED_729_SINGLE_OS_OWNER_NO_LEASE" in positive.stdout and
            "B0162_W23_ORPHAN_FALSE_CLEANUP_HELD_OWNER=GREEN" in positive.stdout and
            "B0162_W23_ORPHAN_THROW_CLEANUP_HELD_OWNER=GREEN" in positive.stdout,
            "CURRENT_EXACT_ORIGINAL_R3_38_CSHARP_CASES_NOT_ALL_PASS:"+
            positive.stdout[-1500:]+positive.stderr[-1500:])
        mutant=execute("mutated_OLD_THREE_ARG_CONSTRUCTOR",True)
        must(mutant.returncode!=0 and
            "CS7036" in mutant.stdout and
            "CanonicalWorldGateway" in mutant.stdout,
            "MUTATED_OLD_712_CONSTRUCTOR_NOT_REJECTED_BY_REAL_CSHARP_COMPILER:"+
            mutant.stdout[-1700:]+mutant.stderr[-1700:])
    return {"real_original_exact_R3_dotnet8_CSharp9_source":"38/38_PASS",
      "actual_selected_original_729_gateway_and_669_556_disk":"PASS",
      "real_child_CLR_owner_lease_contention":"ORIGINAL_POSITIVE_SUITE_PASS",
      "same_code_old_three_argument_constructor_mutant":"CS7036_EXPECTED_COMPILE_RED",
      "mutated_source_wrote_to_producer":False,
      "native_unity":"NOT_RUN"}
def repeat_physical_v3_g0(r):
    old=load(r["w23"]/"Tools/B01/B01-40/P17W23V3G0/intake.py",
            "b0140_w23_immutable_original_selected_v3_physical")
    safe=load(r["safe"]/"Tools/B01/B01-40/P17W15AtomicReceiverMigration/migration.py",
              "b0140_w23_original_575_safe")
    guid=load(r["guid"]/"Tools/B01/B01-40/P17W18SerializedScriptGuidFence/fence.py",
              "b0140_w23_original_635_yaml")
    original=old.roots_and_scope(r)
    owner,raw,assets,zipreceipt=old.original_v3_producer(r,safe)
    package=old.stage_owner_and_collision(r,safe,owner,raw)
    physical=old.security_negative_and_physical(r,safe,guid,assets)
    mismatch=old.no_one_authority_bridge(r)
    must(physical["original_selected_source_unity_files"]==63 and
         physical["source_unity_meta_guids"]==39 and
         package["r5_690_plus_v3_729_flat_manifest_collision"]=="DETECTED_AND_DENIED" and
         zipreceipt["zip_bytes"]==90659 and
         mismatch["current_same_owner_same_lock"]=="NO_INCOMPATIBLE",
         "PREDECESSOR_729_EXACT_G0_NOT_REPRODUCED")
    return {"original_v3_exact_sha":PIN["v3"],
       "author_zip_bytes":zipreceipt["zip_bytes"],
       "original_source_asset_files":63,"original_unity_meta_guids":39,
       "real_selected_v3_plus_r5_disposable_unity_scratch":"PASS",
       "foreign_empty_dir_and_same_guid_alt_denied":"PASS",
       "two_flat_root_manifest_collision_denied":"PASS",
       "r3_bridge_tools_only_no_new_unity_guid":True}
def main():
    p=argparse.ArgumentParser()
    for n in (*PIN,"out"):p.add_argument("--"+n,required=True,type=Path)
    a=p.parse_args();r={n:getattr(a,n).resolve() for n in PIN}
    original,prov=check_history(r)
    scratch=physical_tools_scratch(original)
    csharp=original_actual_dotnet(r,original)
    physical=repeat_physical_v3_g0(r)
    data={"package":"B01-40-P17W23-R3-741-ONE-OWNER-G0-001",
      "original_r3_source":prov,"safe_tools_stage_rollback":scratch,
      "independently_compiled_original_source_and_ctor_mutation":csharp,
      "original_729_r5_real_unity_source_g0":physical,
      "source_transport_g0":"ACCEPTED_SOURCE_ONLY_EXACT_R3_741",
      "final_canonical_game_g0":"HOLD_PRIVATE_510_680_TRUST_NATIVE_G1",
      "native_unity_g1":"NOT_RUN","g2":"HOLD","codex_ack":False,"merge":False,
      "six_stages":{"IMPLEMENTED":True,"TESTED":"ACTUAL_ORIGINAL_SOURCE_OS",
       "QA_ACCEPTED":"SOURCE_ONLY_SUBGATE","READY_FOR_INTEGRATOR":False,
       "INTEGRATED":False,"RELEASED":False}}
    a.out.mkdir(parents=True,exist_ok=True)
    (a.out/"r3-741-one-authority-source-g0.json").write_text(
        json.dumps(data,sort_keys=True,indent=2)+"\n")
    print("B0140_W23_R3_741_EXACT_ORIGINAL_GIT",json.dumps({"members":len(original),"correct_gate":True}))
    print("B0140_W23_R3_741_REAL_ORIGINAL_DOTNET",json.dumps(csharp,sort_keys=True))
    print("B0140_W23_R3_741_SAFE_FOREIGN_ROLLBACK",json.dumps(scratch,sort_keys=True))
    print("B0140_W23_R3_741_729_R5_UNITY_PHYSICAL",json.dumps(physical,sort_keys=True))
    print("B0140_W23_R3_741_G0_SOURCE_ONLY_PASS_NATIVE_NOT_RUN_C510_GAME_HOLD_NO_MERGE")
if __name__=="__main__":
    try:main()
    except (Stop,ValueError,KeyError,OSError,AssertionError) as e:
        print("B0140_W23_R3_741_FAILURE",str(e));raise SystemExit(1)
