#!/usr/bin/env python3
"""Independent B01-40 P17/W16 physical source intake of ONE B01-23 host bridge.

Runs original #587 author build/verify on exact Git SHA, then original W15
B01-40 atomic disposable-clone migration and strict rollback against ALL three
immutable source lineages (#510+#556+#587, plus selected W06 dependencies).
ALWAYS REJECTS game-release admission for uncorrected W15 core #588 source RED.
No live Codex / Game project touched, no native Unity claim.
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
import zipfile

GAME_SHA="d94400d95d13db7f27e1533df05c9292d5380431"
SAVE_SHA="33094f154c7b91119b36d58aab285e6e83c5de57"
BRIDGE_SHA="ad7904eca571c742257551fa8103f266fcb9b752"
W15_QA_SHA="3b743acc52bb44ae6a50af56ed9aca46d22821a7"
BRIDGE_ZIP_SHA="fec19b2e244ca9f37f6e9c069f70c615509b8d4b06264bece877d2a38168e89c"
BRIDGE_BYTES=71047
BRIDGE_FILES=22
BRIDGE_MEMBERS=23
BRIDGE_ASSET="Assets/B0123UnitySaveBridge001"
HOLD_ALTERNATES=(
    "Assets/B0123SecureV2HostBridge001",  # B01-23 #585
    "Assets/B0123SecureReattachV2R11Alternative",
    "Assets/B0123PublicGuardAlternate",
    "Assets/B0123DebrisPlayground001",
)
SCOPE_BLOBS={
    "Assets/B0123UnitySaveBridge001/Core/AtomicWorldSwap.cs":
        "16a7d7043a2ecb59a41bbfb063170235872fd7fc",
    "Assets/B0123UnitySaveBridge001/Unity/IsolatedTwinRigSaveHost.cs":
        "ff5da20e3c98ab3b540f37fb5652666c47228ed3",
    "Tools/B01/B01-23/P17W15UnitySaveBridge001/package.py":
        "d4c6a64f7d0af3346fe3e6f0b2048ce805f82c9c",
}
class Hold(Exception): pass
def require(cond,reason):
    if not cond: raise Hold(reason)
def sha(data):return hashlib.sha256(data).hexdigest()
def git(root,*args):
    proc=subprocess.run(["git","-C",str(root),*args],capture_output=True)
    require(proc.returncode==0,"GIT_SOURCE_FAIL "+repr(args)+proc.stderr.decode(errors="replace"))
    return proc.stdout
def git_blob(data):
    return hashlib.sha1(b"blob "+str(len(data)).encode()+b"\x00"+data).hexdigest()
def import_file(path,name):
    spec=importlib.util.spec_from_file_location(name,str(path))
    mod=importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    return mod
def source_pr_pins(roots):
    for key,expected in (("game",GAME_SHA),("save",SAVE_SHA),
                         ("bridge",BRIDGE_SHA),("receiver",W15_QA_SHA)):
        actual=git(roots[key],"rev-parse","HEAD").decode().strip()
        require(actual==expected,"FROZEN_SELECTED_COMMIT_MISMATCH:"+key)
    for path,hash_ in SCOPE_BLOBS.items():
        raw=git(roots["bridge"],"show","HEAD:"+path)
        require(git_blob(raw)==hash_,"BRIDGE_GIT_SOURCE_BLOB_DRIFT:"+path)
        require((roots["bridge"]/path).read_bytes()==raw,"BRIDGE_LOCAL_CHANGED:"+path)
    return True

def original_bridge_bytes(roots,old):
    original=import_file(roots["bridge"]/
        "Tools/B01/B01-23/P17W15UnitySaveBridge001/package.py",
        "original_author_587")
    blob=original.build()
    payload=original.verify(blob)
    require(len(blob)==BRIDGE_BYTES and sha(blob)==BRIDGE_ZIP_SHA,
            "EXACT_587_PHYSICAL_INNER_ZIP_DRIFT")
    require(len(payload)==BRIDGE_MEMBERS and len(original.scope())==BRIDGE_FILES,
            "WRONG_BRIDGE_PACKAGE_MEMBERS")
    mf=json.loads(payload["PACKAGE_MANIFEST.json"])
    require(mf["package"]=="B01-23-UNITY-SAVE-HOST-BRIDGE-001" and
            mf["version"]=="1.0.0" and mf["own_head_sha"]==BRIDGE_SHA and
            mf["only_save_source_sha"]==SAVE_SHA and
            mf["gameplay_510_sha"]==GAME_SHA and
            mf["unity_guid_count"]==10 and mf["asmdef_count"]==3 and
            mf["native_unity"]=="NOT_RUN" and mf["canonical_510_live_import"]=="HOLD" and
            mf["codex_ack"] is False,"BRIDGE_PROVENANCE_FORGED")
    original_paths={x["path"] for x in mf["files"]}
    require(len(original_paths)==BRIDGE_FILES and
            set(payload)==original_paths|{"PACKAGE_MANIFEST.json"},"BRIDGE_MEMBER_SET_TAMPER")
    for item in mf["files"]:
        p=item["path"];raw=payload[p]
        require(raw==git(roots["bridge"],"show","HEAD:"+p) and
                len(raw)==item["bytes"] and sha(raw)==item["sha256"],
                "BRIDGE_ORIGINAL_GIT_FILE_MISMATCH:"+p)
    imports={}
    for path,raw in payload.items():
        if path==BRIDGE_ASSET+".meta" or path.startswith(BRIDGE_ASSET+"/"):
            require(path not in imports,"BRIDGE_DUPLICATE_ASSET")
            imports[path]=raw
    require(len(old.validate_guids(imports))==10,"BRIDGE_NOT_TEN_UNIQUE_META_GUIDS")
    asm={json.loads(raw)["name"]:json.loads(raw)
         for n,raw in imports.items() if n.endswith(".asmdef")}
    require(set(asm)=={
        "B0123.UnitySaveBridge.Core",
        "B0123.UnitySaveBridge.Unity",
        "B0123.UnitySaveBridge.PlayModeTests"},"BRIDGE_ASMDEF_SET_UNEXPECTED")
    require(asm["B0123.UnitySaveBridge.Core"]["references"]==
            ["B0123.DetachedSaveContract.Runtime","B0123.DebrisRepulse.Runtime"] and
            asm["B0123.UnitySaveBridge.Core"]["noEngineReferences"] is True,
            "BRIDGE_CORE_DEPENDENCIES")
    require("B0123.SecureReattachV2.Runtime" in asm["B0123.UnitySaveBridge.Unity"]["references"] and
            "B0123.UnitySaveBridge.Core" in asm["B0123.UnitySaveBridge.Unity"]["references"],
            "BRIDGE_UNITY_ASMDEF_DEPENDENCIES")
    require("UNITY_INCLUDE_TESTS" in asm["B0123.UnitySaveBridge.PlayModeTests"]["defineConstraints"] and
            asm["B0123.UnitySaveBridge.PlayModeTests"]["includePlatforms"]==["Editor"],
            "BRIDGE_PLAYMODE_ASMDEF_SCOPE")
    return imports,{"head":BRIDGE_SHA,"physical_zip_sha256":sha(blob),"physical_bytes":len(blob),
                    "physical_members":len(payload),"original_git_members":len(original_paths),
                    "unity_asset_members":len(imports),"unity_meta_guids":10,"asmdef_count":3}

def known_original_source_release_red(imports):
    """Fail-closed SECURITY gate for known W15 #588, independently inspect exact Git bytes.

    This is an unpatched-source CODE PATH probe, not a stand-in for B01-61
    executing correct W16 version and NOT an assertion of a native PhysX bug.
    """
    code=imports[BRIDGE_ASSET+"/Core/AtomicWorldSwap.cs"].decode()
    require("if(!Consistent(candidate.Capture(),snapshot))" in code,
            "POST_ACTIVATE_READBACK_GUARD_NOT_PRESENT")
    require("snapshot.Generation<currentCold.Snapshot.Generation" in code,
            "ORIGINAL_GENERATION_COMPARISON_CHANGED_REVIEW_NEEDED")
    # A source-level equality branch is not enough alone to prove fix.
    # The exact Git SHA is uncorrected independently witnessed historical W15.
    require(BRIDGE_SHA=="ad7904eca571c742257551fa8103f266fcb9b752",
            "DIFFERENT_SHA_NEEDS_INDEPENDENT_ASSESSMENT")
    return {
        "post_activation_recapture":"SOURCE_GUARD_PRESENT_NOT_NATIVE_TESTED",
        "same_run_same_generation_different_world":"RELEASE_RED_CONFIRMED_IN_W15_B01_61_588",
        "persistent_across_restarts_generation_authority":"ABSENT_FOR_REAL_CANONICAL_HOST",
        "original_private_510_rigs_ledger_integration":"NOT_AVAILABLE",
        "security_release":"HOLD_DO_NOT_INSTALL_OR_OFFER"}

def test_neg(fn,label):
    try:fn()
    except Exception as exc:
        # The pinned original W15 receiver is imported as a separate module,
        # so its own GateHold class is not identical to this W16 GateHold.
        if isinstance(exc,(Hold,ValueError,OSError,AssertionError)) or exc.__class__.__name__=="GateHold":
            return label
        raise
    raise AssertionError("DENIAL_TEST_FALSE_ACCEPT:"+label)
def actual_tests(roots,old,selected,summary):
    # W15 B01-40 original executable is reused exactly, no rebuilt migration
    # algorithm, no edits to old #575 source.
    before,origin=old.source_files(roots["game"],roots["save"])
    require(origin["only_game_sha"]==GAME_SHA and origin["only_save_sha"]==SAVE_SHA,
            "W15_PINNED_SELECTED_CORE")
    require(len(before)==37 and len(old.validate_guids(before))==23,
            "W15_510_556_W06_SOURCE_CHANGED")
    require(not set(before).intersection(selected),"BRIDGE_COLLIDES_WITH_FROZEN_CORE")
    incoming={**before,**selected}
    count_guid=len(old.validate_guids(incoming))
    require(count_guid==33,"CROSS_PACKAGE_META_GUID_COLLISION")
    origin=dict(origin)
    origin["only_bridge_sha"]=BRIDGE_SHA
    origin["bridge_zip_sha256"]=summary["physical_zip_sha256"]
    origin["combined_unity_files"]=len(incoming)
    origin["combined_unity_guids"]=count_guid
    checks=[]
    outcomes={}
    with tempfile.TemporaryDirectory(prefix="b01-40-w16-source-only-") as d:
        root=Path(d)
        host=root/"synthetic-authorized-host"
        files={
          "ProjectSettings/ProjectVersion.txt":
             git(roots["game"],"show","HEAD:ProjectSettings/ProjectVersion.txt"),
          "Packages/manifest.json":
             git(roots["game"],"show","HEAD:Packages/manifest.json"),
          "Assets/USER_UNRELATED/owner.cs":b"namespace UserOriginal { class Owned {} }\n",
          "Assets/USER_UNRELATED/owner.cs.meta":
             b"fileFormatVersion: 2\nguid: 19283746556473829102938475647382\n",
          "Assets/USER_UNRELATED/original.unity":b"%YAML 1.1\n--- !u!1 &123\nGameObject:\n  m_Name: OriginalHostOnly\n",
        }
        host.mkdir()
        for p,blob in files.items():
            f=host/p;f.parent.mkdir(parents=True,exist_ok=True);f.write_bytes(blob)
        # A host that already contains either candidate or even an empty
        # alternative source subtree may never be adopted/mixed.
        def deny_host(mutator,label):
            h=root/("bad_"+label)
            shutil.copytree(host,h)
            mutator(h)
            state=old.crawl(h)
            checks.append(test_neg(lambda:guard_host(h,incoming,old),label))
            require(old.crawl(h)==state,"DENIED_HOST_WAS_MODIFIED:"+label)
        deny_host(lambda h:(h/"Assets/B0123SecureV2HostBridge001").mkdir(),
                  "deny_unowned_EMPTY_alt_585")
        deny_host(lambda h:(h/"Assets/B0123UnitySaveBridge001/Core").mkdir(parents=True),
                  "deny_unowned_EMPTY_already_selected_587")
        deny_host(lambda h:(h/"Assets/B0123DebrisPlayground001").mkdir(),
                  "deny_legacy_W07_EMPTY")
        deny_host(lambda h:(h/"Assets/USER_UNRELATED/original.unity").write_text(
                  "MonoBehaviour:\n  m_Name: DebrisRigPlayground.Reattach\n"),
                  "deny_legacy_W07_serialized_scene")
        deny_host(lambda h:(h/"Assets/USER_UNRELATED/owner.cs.meta").write_text(
                  "fileFormatVersion: 2\nguid: "+next(iter(old.validate_guids(incoming)))+"\n"),
                  "deny_cross_package_guid_collision")
        deny_host(lambda h:(h/"Assets/USER_UNRELATED/owner.cs.meta").write_text(
                  "fileFormatVersion: 2\nguid: broken\n"),"deny_host_bad_guid")
        deny_host(lambda h:(h/"ProjectSettings/ProjectVersion.txt").write_text(
                  "m_EditorVersion: 2022.3.0f1\n"),"deny_wrong_unity")
        deny_host(lambda h:(h/"Packages/manifest.json").write_text(
                  '{"dependencies":{"com.unity.test-framework":"1.4.0"}}'),"deny_wrong_test_framework")
        duplicate=dict(incoming)
        first=next(p for p in selected if p.endswith(".meta"))
        second=next(p for p in selected if p.endswith(".meta") and p!=first)
        duplicate[first]=duplicate[second]
        checks.append(test_neg(lambda:old.preflight_host(host,duplicate),
                               "deny_duplicate_new_bridge_guid"))
        checks.append(test_neg(lambda:old.create_candidate(host,host,incoming,origin,
                               roots["game"],roots["save"],roots["bridge"]),
                               "deny_overlap_source_and_target"))
        symlink="NOT_RUN_NO_PERMISSION"
        victim=root/"outside.txt";victim.write_text("PRESERVE")
        link=host/"Assets"/"USER_UNRELATED"/"user_link"
        try:link.symlink_to(victim)
        except (OSError,NotImplementedError):pass
        else:
            checks.append(test_neg(lambda:guard_host(host,incoming,old),
                                   "deny_symlink"))
            require(victim.read_text()=="PRESERVE","MUTATED_LINK_TARGET")
            link.unlink();symlink="PASS"
        failed_dest=root/"failed_before_publish"
        checks.append(test_neg(lambda:old.create_candidate(host,failed_dest,incoming,
                     origin,roots["game"],roots["save"],roots["bridge"],inject_failure=True),
                     "mid_stage_fault_no_partial_candidate"))
        require(not failed_dest.exists(),"PRE_COMMIT_FAILURE_LEFT_DESTINATION")
        for i in range(2):
            target=root/("candidate_"+str(i))
            guard_host(host,incoming,old)
            owned,folders=old.create_candidate(host,target,incoming,origin,
                              roots["game"],roots["save"],roots["bridge"])
            actual,dirs=old.crawl(target)
            require(owned==actual and dirs==folders and
                    all(actual[p]==v for p,v in incoming.items()),"PUBLISHED_CHANGED_SOURCE_BYTES")
            require(all(actual[p]==v for p,v in files.items()),"USER_HOST_SOURCE_MUTATED")
            require(not any(p.startswith("Tools/") for p in actual),"TOOLS_COPIED_TO_UNITY")
            outcomes["disposable_combined_stage_rollback_"+str(i)]="PASS"
            if i==0:
                checks.append(test_neg(lambda:old.create_candidate(host,target,incoming,
                  origin,roots["game"],roots["save"],roots["bridge"]),
                  "deny_existing_target"))
                alien=target/"Assets"/"USER_UNRELATED"/"FOREIGN_EMPTY"/"nested"
                alien.mkdir(parents=True)
                checks.append(test_neg(lambda:old.rollback_candidate(target,owned,folders),
                                       "deny_foreign_nested_EMPTY_during_rollback"))
                require(alien.is_dir(),"FOREIGN_EMPTY_DIRECTORY_DELETED")
                alien.rmdir();alien.parent.rmdir()
                orig=target/(BRIDGE_ASSET+"/Core/AtomicWorldSwap.cs")
                earlier=orig.read_bytes()
                orig.write_bytes(b"MUTATED NEW BRIDGE")
                checks.append(test_neg(lambda:old.rollback_candidate(target,owned,folders),
                                       "deny_dirty_selected_bridge_file"))
                require(orig.read_bytes()==b"MUTATED NEW BRIDGE","DIRTY_BRIDGE_ERASED")
                orig.write_bytes(earlier)
                foreign=target/"FOREIGN.txt";foreign.write_text("STAY")
                checks.append(test_neg(lambda:old.rollback_candidate(target,owned,folders),
                                       "deny_foreign_file"))
                require(foreign.read_text()=="STAY","FOREIGN_FILE_ERASED")
                foreign.unlink()
                marker=target/old.BASENAME
                marker_original=marker.read_bytes()
                marker.write_text('{"owner":"FAKE"}')
                checks.append(test_neg(lambda:old.rollback_candidate(target,owned,folders),
                                       "deny_forged_receipt"))
                require(target.exists(),"FORGED_MARKER_DELETED")
                marker.write_bytes(marker_original)
                missing=target/(BRIDGE_ASSET+"/Unity/IsolatedTwinRigSaveHost.cs")
                original=missing.read_bytes();missing.unlink()
                checks.append(test_neg(lambda:old.rollback_candidate(target,owned,folders),
                                       "deny_missing_selected_unity_host"))
                missing.write_bytes(original)
            old.rollback_candidate(target,owned,folders)
            require(not target.exists(),"CLEAN_SELECTED_HOST_ROLLBACK_FAILED")
        require(old.crawl(host)[0]==files,"ORIGINAL_DISPOSABLE_INPUT_MODIFIED")
    return {"new_bridge_candidate":summary,"combined":origin,
            "negative_checks":checks,"negative_count":len(checks),
            "positive_runs":outcomes,"symlink":symlink,
            "live_original_510_host":"NOT_TOUCHED",
            "native_unity":"NOT_RUN"}

def guard_host(host,incoming,old):
    fs,dirs=old.preflight_host(host,incoming)
    for folder in dirs:
        require(not any(folder==name or folder.startswith(name+"/")
                        for name in HOLD_ALTERNATES),"UNSELECTED_BRIDGE_PRESENT:"+folder)
    for name,blob in fs.items():
        require(not any(name==p or name.startswith(p+"/") for p in HOLD_ALTERNATES),
                "COMPETING_BRIDGE_FILE_PRESENT:"+name)
        if name.endswith((".unity",".prefab",".asset",".cs")):
            require("B0123SecureV2HostBridge001" not in blob.decode("utf-8",errors="replace"),
                    "COMPETING_BRIDGE_SERIALIZED_COMPONENT:"+name)
    return True

def main():
    p=argparse.ArgumentParser()
    for key in ("game","save","bridge","receiver","out"):
        p.add_argument("--"+key,required=True,type=Path)
    args=p.parse_args()
    roots={k:getattr(args,k).resolve() for k in ("game","save","bridge","receiver")}
    source_pr_pins(roots)
    previous=import_file(roots["receiver"]/
        "Tools/B01/B01-40/P17W15AtomicReceiverMigration/migration.py",
        "original_b0140_w15_receiver")
    imported,summary=original_bridge_bytes(roots,previous)
    release=known_original_source_release_red(imported)
    result=actual_tests(roots,previous,imported,summary)
    result["security_source_review"]=release
    result["selected_bridge_status"]="PROVISIONAL_587_HQ_PREFERENCE_ONLY"
    result["formal_bridge_intake"]="HOLD_SECURITY_P0_SAME_GENERATION_FORK"
    result["native_unity"]="NOT_RUN"
    result["codex_ack"]=False
    result["game_source_g0"]="ACCEPTED_SOURCE_ONLY_PRIOR"
    result["save_source_g0"]="ACCEPTED_SOURCE_ONLY_PRIOR"
    result["bridge_physical_zip_identity"]="PASS_VERIFIED_ORIGINAL_GIT_BYTES"
    result["g2"]="HOLD"
    args.out.mkdir(parents=True,exist_ok=True)
    (args.out/"w16-bridge-intake.json").write_text(
        json.dumps(result,sort_keys=True,indent=2)+"\n")
    print("B0140_W16_ORIGINAL_BRIDGE_PHYSICAL",
          json.dumps(summary,sort_keys=True))
    print("B0140_W16_COMBINED_SOURCE",
          json.dumps(result["combined"],sort_keys=True))
    print("B0140_W16_NEGATIVE_PASS",result["negative_count"],
          "SYMLINK",result["symlink"])
    print("B0140_W16_CLEAN_COMBINED_CLONE_ROLLBACK",
          len(result["positive_runs"]))
    print("B0140_W16_RELEASE_HOLD_SAME_GENERATION_FORK_NEEDED_NEW_VERSION")
    print("B0140_W16_GAME510_SAVE556_UNCHANGED NATIVE_NOT_RUN LIVE_CANONICAL_HOLD G2_HOLD NO_CODEX_ACK NO_MERGE")
if __name__=="__main__":
    try:main()
    except (Hold,ValueError,AssertionError,OSError,KeyError,
            subprocess.SubprocessError,zipfile.BadZipFile) as e:
        print("B0140_W16_INTAKE_GATE_FAIL",str(e))
        raise SystemExit(1)
