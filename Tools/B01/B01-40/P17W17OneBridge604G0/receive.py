#!/usr/bin/env python3
"""P17/W17 B01-40 final single-candidate SOURCE G0, physical ZIP and safe receiver.

Independent exact-commit original #604 archive; original #510 + #556 + W06
imported by unchanged B01-40 #575 fail-closed disposable-host publisher.
Only ONE corrected W16 bridge is staged; #596 is read-only ALT_HOLD.
Native Unity, original #510 private ledger integration, Codex ACK NOT_RUN/NOT_DONE.
"""
from __future__ import annotations
import argparse, hashlib, importlib.util, json, os, re, shutil, subprocess, tempfile
from pathlib import Path, PurePosixPath

GAME="d94400d95d13db7f27e1533df05c9292d5380431"
SAVE="33094f154c7b91119b36d58aab285e6e83c5de57"
SELECT="1aa8a59ce1bb4fcd998f0b2b1ce25dfaea333f0a"
ALT="efb2b06eafedab5469b57f163152d96aca3c5915"
RECEIVER="3b743acc52bb44ae6a50af56ed9aca46d22821a7"
QA_FENCE="7d206c63e1e9ab4ee4ce52eb314507ce49721d2c"
W15="ad7904eca571c742257551fa8103f266fcb9b752"
ZIP_BYTES=101354
ZIP_SHA="ef110b5906d385cbc51938a35510c97516a0942b28ec055519a78d1e3fe5afb8"
PREFIX="Assets/B0123UnitySaveBridge001"
PINS={
PREFIX+"/Core/AtomicWorldSwap.cs":"399d3cb2074a2659af8f4e941edb28a3d2dd2832",
PREFIX+"/Core/CheckpointFence.cs":"53f907125b09531ebdf5ffdfbb9c5c9b48eca217",
PREFIX+"/Unity/IsolatedTwinRigSaveHost.cs":"5012e98bba786c6b9702992c317586a0015678c1",
"Tools/B01/B01-23/P17W15UnitySaveBridge001/package.py":"96dc5475505172a63ed2510098eb6fc8a234ee18",
}
ALTERNATE_FOLDERS=(
"Assets/B0123SecureV2HostBridge001",  # old #585
"Assets/B0123DebrisPlayground001",    # W07
"Assets/B0123PublicGuardAlternate",
"Assets/B0123SecureReattachV2R11Alternative",
)
class Hold(Exception):pass
def ensure(ok,msg):
    if not ok:raise Hold(msg)
def git(root,*argv):
    x=subprocess.run(["git","-C",str(root),*argv],capture_output=True)
    ensure(x.returncode==0,"GIT_SOURCE_ERROR:"+repr(argv)+x.stderr.decode(errors="replace"))
    return x.stdout
def blobsha(data):
    return hashlib.sha1(b"blob "+str(len(data)).encode()+bytes([0])+data).hexdigest()
def sha(data):return hashlib.sha256(data).hexdigest()
def mod(path,name):
    spec=importlib.util.spec_from_file_location(name,str(path))
    m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m);return m
def sources(roots):
    for label,ref in (("game",GAME),("save",SAVE),("chosen",SELECT),
                      ("alternative",ALT),("receiver",RECEIVER)):
        ensure(git(roots[label],"rev-parse","HEAD").decode().strip()==ref,
               "WRONG_PINNED_GIT_SOURCE:"+label)
    for path,pin in PINS.items():
        data=git(roots["chosen"],"show","HEAD:"+path)
        ensure(blobsha(data)==pin,"DRIFT_SELECTED_GIT_BLOB:"+path)
        ensure((roots["chosen"]/path).read_bytes()==data,"DIRTY_SELECTED_SOURCE:"+path)
    altcore=git(roots["alternative"],"show","HEAD:"+PREFIX+"/Core/AtomicWorldSwap.cs")
    ensure(blobsha(altcore)!=PINS[PREFIX+"/Core/AtomicWorldSwap.cs"],
           "ALTERNATIVE_CORE_FINGERPRINT_IDENTICAL_NEEDS_REVIEW")
    # The #596 R2 does not contain the NEW #604 checkpoint fence source.
    altfiles=git(roots["alternative"],"ls-tree","-r","--name-only","HEAD",PREFIX).decode()
    ensure(PREFIX+"/Core/CheckpointFence.cs" not in altfiles.splitlines(),
           "ALTERNATIVE_596_ALREADY_CONTAINS_604_FENCE_NEEDS_REVIEW")
    # Check the immutable W16 *core+fence* QA source identity. Earlier #603
    # tested these two original Git blobs at a distinct producer commit.
    for path in (PREFIX+"/Core/AtomicWorldSwap.cs",PREFIX+"/Core/CheckpointFence.cs"):
        ensure(git(roots["qa_core"],"show",QA_FENCE+":"+path)==
               git(roots["chosen"],"show","HEAD:"+path),
               "OLDER_QA_CORE_OR_FENCE_MISMATCH_REQUIRES_NEW_CSHARP_QA")
    return {"chosen_sha":SELECT,"losing_pr_596_sha":ALT,
            "loser_core_git_blob":blobsha(altcore),
            "selected_core_git_blob":PINS[PREFIX+"/Core/AtomicWorldSwap.cs"],
            "independent_603_core_fence_blob_identical_to_chosen":True}
def original_package(roots,old):
    author=mod(roots["chosen"]/
        "Tools/B01/B01-23/P17W15UnitySaveBridge001/package.py","p17w17_author604")
    archive=author.build();contents=author.verify(archive)
    ensure(len(archive)==ZIP_BYTES and sha(archive)==ZIP_SHA,
           "ORIGINAL_604_PHYSICAL_ZIP_SHA_OR_SIZE_DRIFT")
    ensure(len(contents)==27 and len(author.scope())==26,
           "ORIGINAL_604_MEMBER_COUNT_DRIFT")
    manifest=json.loads(contents["PACKAGE_MANIFEST.json"])
    ensure(manifest["package"]=="B01-23-UNITY-SAVE-HOST-BRIDGE-001" and
           manifest["version"]=="2.0.0" and manifest["own_head_sha"]==SELECT and
           manifest["supersedes_w15_sha"]==W15 and
           manifest["only_save_source_sha"]==SAVE and
           manifest["gameplay_510_sha"]==GAME and
           manifest["unity_guid_count"]==11 and manifest["asmdef_count"]==3 and
           manifest["native_unity"]=="NOT_RUN" and
           manifest["canonical_510_live_import"]=="HOLD" and
           manifest["codex_ack"] is False,"FORGED_OR_WRONG_PRODUCER_MANIFEST")
    records=manifest["files"]
    ensure(len(records)==26 and len(set(x["path"] for x in records))==26,
           "DUPLICATE_OR_MISSING_MANIFEST_FILES")
    for r in records:
        p=r["path"];data=contents[p]
        ensure(data==git(roots["chosen"],"show","HEAD:"+p) and
               r["bytes"]==len(data) and r["sha256"]==sha(data),
               "ORIGINAL_MEMBER_GIT_BYTE_MISMATCH:"+p)
    assets={}
    for path,data in contents.items():
        if path==PREFIX+".meta" or path.startswith(PREFIX+"/"):
            assets[path]=data
    ensure(len(assets)==18,"EXPECTED_18_ORIGINAL_BRIDGE_ASSETS")
    ensure(len(old.validate_guids(assets))==11,"BAD_11_BRIDGE_META_GUIDS")
    asm={json.loads(data)["name"]:json.loads(data)
         for n,data in assets.items() if n.endswith(".asmdef")}
    ensure(set(asm)=={
        "B0123.UnitySaveBridge.Core","B0123.UnitySaveBridge.Unity",
        "B0123.UnitySaveBridge.PlayModeTests"},"WRONG_SELECTED_THREE_ASMDEF_GRAPH")
    ensure(asm["B0123.UnitySaveBridge.Core"]["references"]==
           ["B0123.DetachedSaveContract.Runtime","B0123.DebrisRepulse.Runtime"] and
           asm["B0123.UnitySaveBridge.Core"]["noEngineReferences"] is True and
           "B0123.SecureReattachV2.Runtime" in asm["B0123.UnitySaveBridge.Unity"]["references"] and
           "B0123.UnitySaveBridge.Core" in asm["B0123.UnitySaveBridge.Unity"]["references"] and
           "UNITY_INCLUDE_TESTS" in asm["B0123.UnitySaveBridge.PlayModeTests"]["defineConstraints"],
           "INVALID_SELECTED_W16_ASMDEF_DEPENDENCIES")
    code=assets[PREFIX+"/Core/AtomicWorldSwap.cs"].decode()
    unity=assets[PREFIX+"/Unity/IsolatedTwinRigSaveHost.cs"].decode()
    ensure("ready.Snapshot.Generation<=currentCold.Snapshot.Generation" in code and
           "candidate.Capture(),snapshot" in code and
           "return quarantined?WorldSwapVerdict.RollbackQuarantine:verdict;" in code and
           "checkpoint.Seal(" in code and
           "UsePersistentCheckpointSlot(string slot)" in unity and
           "new FileCheckpointFence(" in unity,
           "MISSING_CURRENT_SOURCE_SECURITY_PATH")
    return assets,{"archive_bytes":len(archive),"archive_entries":len(contents),
                   "source_sha":SELECT,"zip_sha256":sha(archive),
                   "original_git_members":len(records),"unity_assets":len(assets),
                   "unique_guids":11,"asmdefs":len(asm)}

def deny(call,label):
    try:call()
    except Exception as e:
        if isinstance(e,(Hold,ValueError,AssertionError,OSError)) or e.__class__.__name__=="GateHold":
            return label
        raise
    raise AssertionError("FALSE_ADMISSION:"+label)

def admitted_host(host,imports,old):
    # Original immutable W15 receiver explicitly handles 510/556 original
    # serialized W07/alternate controls, exact GUIDs and casefolded collision.
    old.preflight_host(host,imports)
    files,dirs=old.crawl(host)
    for name in [*files,*dirs]:
        ensure(not any(name==p or name.startswith(p+"/") for p in ALTERNATE_FOLDERS),
               "LOSING_OR_LEGACY_COMPONENT_IN_RECEIVER:"+name)
    for path,b in files.items():
        if path.endswith((".unity",".prefab",".asset",".cs")):
            val=b.decode("utf-8",errors="replace")
            ensure("B0123SecureV2HostBridge001" not in val and
                   "DebrisRigPlayground.Reattach" not in val,
                   "COMPETING_HOST_SERIALIZED:"+path)
    return True

def physical_migration(roots,old,bridge,source_record,report):
    core,coreinfo=old.source_files(roots["game"],roots["save"])
    ensure(len(core)==37 and len(old.validate_guids(core))==23,"OLD_RECEIVER_FROZEN_SOURCE_MISMATCH")
    ensure(set(core).isdisjoint(bridge),"FROZEN_SOURCE_OVERWRITE")
    combined={**core,**bridge}
    ensure(len(combined)==55 and len(old.validate_guids(combined))==34,
           "CROSS_PACKAGE_GUID_OR_MEMBERSHIP_COLLISION")
    proof=dict(coreinfo)
    proof.update({"only_bridge_sha":SELECT,"bridge_zip_sha256":report["zip_sha256"],
                  "combined_unity_assets":55,"combined_unity_guids":34})
    checks=[];cycles=[]
    with tempfile.TemporaryDirectory(prefix="b0140-w17-clean-only-") as tmp:
        outer=Path(tmp);host=outer/"authorized-synthetic-host";host.mkdir()
        originals={
            "ProjectSettings/ProjectVersion.txt":
                git(roots["game"],"show","HEAD:ProjectSettings/ProjectVersion.txt"),
            "Packages/manifest.json":
                git(roots["game"],"show","HEAD:Packages/manifest.json"),
            "Assets/OWNER_ONLY/fixture.cs":b"namespace B0140 { class Kept{} }\n",
            "Assets/OWNER_ONLY/fixture.cs.meta":
                b"fileFormatVersion: 2\nguid: aabbccddeeff00112233445566778899\n",
            "Assets/OWNER_ONLY/entry.unity":b"%YAML 1.1\nGameObject:\n  m_Name: OwnerOnly\n",
        }
        for n,b in originals.items():
            p=host/n;p.parent.mkdir(parents=True,exist_ok=True);p.write_bytes(b)
        def mutate_test(op,label):
            h=outer/("bad_"+label);shutil.copytree(host,h)
            op(h);before=old.crawl(h)
            checks.append(deny(lambda:admitted_host(h,combined,old),label))
            ensure(old.crawl(h)==before,"FAILED_ADMISSION_MUTATED_FOREIGN_HOST")
        mutate_test(lambda h:(h/"Assets/B0123SecureV2HostBridge001").mkdir(),
                    "losing_original_585_empty")
        mutate_test(lambda h:(h/"Assets/B0123DebrisPlayground001").mkdir(),
                    "old_w07_empty")
        mutate_test(lambda h:(h/(PREFIX+"/Unity")).mkdir(parents=True),
                    "existing_selected_604_empty")
        mutate_test(lambda h:(h/"Assets/OWNER_ONLY/entry.unity").write_text(
                    "MonoBehaviour:\n  m_Name: B0123SecureV2HostBridge001\n"),
                    "competing_585_serialized_reference")
        mutate_test(lambda h:(h/"Assets/OWNER_ONLY/entry.unity").write_text(
                    "MonoBehaviour:\n  m_Name: DebrisRigPlayground.Reattach\n"),
                    "legacy_w07_serialized_reference")
        mutate_test(lambda h:(h/"Assets/OWNER_ONLY/fixture.cs.meta").write_text(
                    "fileFormatVersion: 2\nguid: "+next(iter(old.validate_guids(bridge)))+"\n"),
                    "cross_bundle_GUID_collision")
        mutate_test(lambda h:(h/"Assets/OWNER_ONLY/fixture.cs.meta").write_text(
                    "fileFormatVersion: 2\nguid: BAD\n"),"invalid_meta")
        mutate_test(lambda h:(h/"ProjectSettings/ProjectVersion.txt").write_text(
                    "m_EditorVersion: 2022.3.0f1\n"),"wrong_unity")
        mutate_test(lambda h:(h/"Packages/manifest.json").write_text(
                    '{"dependencies":{"com.unity.test-framework":"1.4.0"}}'),
                    "wrong_test_framework")
        duplicate=dict(combined)
        a,b=list(x for x in bridge if x.endswith(".meta"))[:2]
        duplicate[a]=duplicate[b]
        checks.append(deny(lambda:old.preflight_host(host,duplicate),
                           "duplicate_source_guid"))
        # original W15 receiver refuses any existing destination incl HOST itself
        checks.append(deny(lambda:old.create_candidate(host,host,combined,proof,
               roots["game"],roots["save"],roots["chosen"]),
               "no_in_place_overlay"))
        symlink="NOT_RUN_NO_PRIVILEGE"
        sentinel=outer/"external_target";sentinel.write_text("KEEP")
        link=host/"Assets/OWNER_ONLY"/"alien_link"
        try:link.symlink_to(sentinel)
        except (OSError,NotImplementedError):pass
        else:
            checks.append(deny(lambda:admitted_host(host,combined,old),"symlink"))
            ensure(sentinel.read_text()=="KEEP","SYMLINK_TARGET_MUTATED")
            link.unlink()
            symlink="PASS"
        interrupted=outer/"mid_stage"
        checks.append(deny(lambda:old.create_candidate(host,interrupted,combined,
              proof,roots["game"],roots["save"],roots["chosen"],
              inject_failure=True),"pre_commit_fault_no_destination"))
        ensure(not interrupted.exists(),"PARTIAL_PUBLICATION")
        for number in range(2):
            target=outer/("staged_"+str(number))
            admitted_host(host,combined,old)
            expected,folders=old.create_candidate(host,target,combined,proof,
                                      roots["game"],roots["save"],roots["chosen"])
            actual,current=old.crawl(target)
            ensure(actual==expected and current==folders,"UNVERIFIED_RECEIVER_COMMIT")
            ensure(all(actual[n]==v for n,v in combined.items()),"BRIDGE_GIT_BYTES_REPLACED")
            ensure(all(actual[n]==v for n,v in originals.items()),"HOST_FOREIGN_FILE_DROPPED")
            ensure(not any(n.startswith("Tools/") for n in actual),
                   "TOOLING_COPIED_TO_UNITY_ASSETS")
            cycles.append("stage_complete_and_rollback_"+str(number))
            if number==0:
                checks.append(deny(lambda:old.create_candidate(host,target,combined,
                       proof,roots["game"],roots["save"],roots["chosen"]),
                       "existing_target_refused"))
                empty=target/"Assets/OWNER_ONLY/FOREIGN_EMPTY/nested"
                empty.mkdir(parents=True)
                checks.append(deny(lambda:old.rollback_candidate(target,expected,folders),
                                   "foreign_nested_empty_folder_survives"))
                ensure(empty.is_dir(),"FOREIGN_EMPTY_WAS_DELETED")
                empty.rmdir();empty.parent.rmdir()
                dirty=target/(PREFIX+"/Core/CheckpointFence.cs")
                original=dirty.read_bytes();dirty.write_bytes(b"UNKNOWN WRITER")
                checks.append(deny(lambda:old.rollback_candidate(target,expected,folders),
                                   "modified_new_checkpoint_code_survives"))
                ensure(dirty.read_bytes()==b"UNKNOWN WRITER","DIRTY_CODE_ERASED")
                dirty.write_bytes(original)
                user=target/"USER_FILE.txt";user.write_text("KEPT")
                checks.append(deny(lambda:old.rollback_candidate(target,expected,folders),
                                   "foreign_file_preserved"))
                ensure(user.read_text()=="KEPT","ALIEN_USER_DATA_ERASED")
                user.unlink()
                marker=target/old.BASENAME;valid=marker.read_bytes()
                marker.write_text('{"owner":"FORGERY"}')
                checks.append(deny(lambda:old.rollback_candidate(target,expected,folders),
                                   "forged_owner_receipt"))
                marker.write_bytes(valid)
                absent=target/(PREFIX+"/Unity/IsolatedTwinRigSaveHost.cs")
                original=absent.read_bytes();absent.unlink()
                checks.append(deny(lambda:old.rollback_candidate(target,expected,folders),
                                   "missing_original_unity_host"))
                absent.write_bytes(original)
            old.rollback_candidate(target,expected,folders)
            ensure(not target.exists(),"ROLLBACK_DID_NOT_REMOVE_CLEAN_OWNED_TARGET")
        ensure(old.crawl(host)[0]==originals,"SYNTHETIC_ORIGINAL_HOST_MUTATED")
    return {"combined_assets":55,"combined_guids":34,
            "negative_checks":checks,"negative_count":len(checks),
            "positive_atomic_clone_and_rollback_cycles":cycles,
            "symlink_status":symlink}
def main():
    ap=argparse.ArgumentParser()
    for name in ("game","save","chosen","alternative","receiver","qa_core","output"):
        ap.add_argument("--"+name,required=True,type=Path)
    args=ap.parse_args()
    root={k:getattr(args,k).resolve() for k in
          ("game","save","chosen","alternative","receiver","qa_core")}
    decision=sources(root)
    old=mod(root["receiver"]/ 
        "Tools/B01/B01-40/P17W15AtomicReceiverMigration/migration.py",
        "existing_unchanged_B0140_receiver")
    assets,identity=original_package(root,old)
    migration=physical_migration(root,old,assets,decision,identity)
    receipt={"package":"B01-40-P17W17-ONE-SAVE-HOST-604-PHYSICAL-G0",
             "source_chosen":decision,"original_author_zip":identity,
             "migration":migration,
             "g0":"ACCEPTED_SOURCE_ONLY_CONDITIONAL_OWNER_SELECTION_604",
             "native_g1":"NOT_RUN","live_510_private_ledger":"NOT_INTEGRATED",
             "g2":"HOLD","codex_ack":False,
             "loser_596_staged":False,"touches_canonical":False,
             "race_free_shared_host_rollback":"NOT_CLAIMED"}
    args.output.mkdir(parents=True,exist_ok=True)
    (args.output/"w17-source-receipt.json").write_text(
        json.dumps(receipt,sort_keys=True,indent=2)+"\n")
    print("B0140_W17_ORIGINAL_PHYSICAL_ZIP",json.dumps(identity,sort_keys=True))
    print("B0140_W17_EXCLUSIVE_FINAL_CANDIDATE",json.dumps(decision,sort_keys=True))
    print("B0140_W17_COMBINED_RECEIVER",json.dumps({
        "files":migration["combined_assets"],"guids":migration["combined_guids"],
        "negative_pass":migration["negative_count"],
        "positive_cycles":len(migration["positive_atomic_clone_and_rollback_cycles"]),
        "symlink":migration["symlink_status"]},sort_keys=True))
    print("B0140_W17_SOURCE_G0_CONDITIONAL_604_ACCEPTED_NATIVE_NOT_RUN_LIVE_HOLD_G2_HOLD_NO_MERGE")
if __name__=="__main__":
    try:main()
    except (Hold,ValueError,AssertionError,OSError,subprocess.SubprocessError) as exc:
        print("B0140_W17_G0_SOURCE_FAILURE",str(exc))
        raise SystemExit(1)
