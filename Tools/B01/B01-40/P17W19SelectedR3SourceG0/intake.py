#!/usr/bin/env python3
"""B01-40 PASS17/W19: physical G0 for ONE owner-selected R3 #642, fail closed.

Real ORIGINAL selected #642 author Git ZIP; original #510+#556+W06 and
immutable #575/#625 real disposable host migration, original #635 GUID gate.
No #644/#604 installation. Source bytes pass != security-release acceptance:
B01-23 explicitly reports unresolved post-commit throw + OnDisable owner lease.
"""
from __future__ import annotations
import argparse
import hashlib
import importlib.util
import json
from pathlib import Path
import re
import shutil
import subprocess
import tempfile

R3="22cec9888047994ae523cbe7bf1b0a483d64cec4"
OTHER_R3="d9b95146e46a5d81d5a291d7887e456164f4d0c2"
R2="efb2b06eafedab5469b57f163152d96aca3c5915"
ALT604="1aa8a59ce1bb4fcd998f0b2b1ce25dfaea333f0a"
GAME="d94400d95d13db7f27e1533df05c9292d5380431"
SAVE="33094f154c7b91119b36d58aab285e6e83c5de57"
SAFE="3b743acc52bb44ae6a50af56ed9aca46d22821a7"
OLDG0="180849791c4b9594b12f26ef8d9dc0cf90adb61c"
GUIDFENCE="547b55560d1a33af182caeaf73f9a9b2d8b1b7fe"
SOURCE_ZIP_BYTES=96318
SOURCE_ZIP_SHA256="2fd97465358bddb7c9aeee91af2d4a0b75dd9b86525150e6bf708403291aa60d"
PREFIX="Assets/B0123UnitySaveBridge001"
PINS={
    PREFIX+"/Core/AtomicWorldSwap.cs":"a0a1923216d88ff8f50c560196f82cdec694f15c",
    PREFIX+"/Unity/IsolatedTwinRigSaveHost.cs":"7b8354bc4cd846227b4bdf6d25d81bed34a325bc",
    "Tools/B01/B01-23/P17W15UnitySaveBridge001/package.py":
      "d6ef2163fcd9c1790b820a886e54917ee50b6cd7",
}
class Hold(Exception):pass
def require(b,why):
    if not b:raise Hold(why)
def sha(data):return hashlib.sha256(data).hexdigest()
def git(root,*a):
    p=subprocess.run(["git","-C",str(root),*a],capture_output=True)
    require(p.returncode==0,"GIT_FAILED:"+repr(a)+p.stderr.decode(errors="replace"))
    return p.stdout
def blobsha(raw):
    return hashlib.sha1(b"blob "+str(len(raw)).encode()+b"\x00"+raw).hexdigest()
def load(path,name):
    spec=importlib.util.spec_from_file_location(name,str(path))
    m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m);return m

def pin_roots(roots):
    expected={"selected":R3,"loser_r3":OTHER_R3,"r2":R2,
              "alt604":ALT604,"game":GAME,"save":SAVE,"safe":SAFE,
              "oldg0":OLDG0,"guidfence":GUIDFENCE}
    for name,value in expected.items():
        got=git(roots[name],"rev-parse","HEAD").decode().strip()
        require(got==value,"ORIGINAL_GIT_SOURCE_COMMIT_PIN_MISMATCH:"+name)
    for path,b in PINS.items():
        raw=git(roots["selected"],"show","HEAD:"+path)
        require(blobsha(raw)==b,"R3_SELECTED_ORIGINAL_SOURCE_BLOB_CHANGED:"+path)
        require((roots["selected"]/path).read_bytes()==raw,
                "UNCOMMITTED_SELECTED_WORKTREE_EDIT:"+path)
    original=git(roots["r2"],"show","HEAD:"+PREFIX+"/Core/AtomicWorldSwap.cs")
    now=git(roots["selected"],"show","HEAD:"+PREFIX+"/Core/AtomicWorldSwap.cs")
    alt=git(roots["loser_r3"],"show","HEAD:"+PREFIX+"/Core/AtomicWorldSwap.cs")
    require(original!=now,"WRONG_R2_CORE_INSTEAD_OF_R3")
    require(alt==original,"LOSING_R3_CORE_NO_LONGER_HISTORICAL_R2_REVIEW_REQUIRED")
    require(alt!=now,"MERGED_COMPETING_R3_CORE_DENIED")
    # #644 adds independent HostSlotLease GUID and script, both forbidden.
    losing=git(roots["loser_r3"],"ls-tree","-r","--name-only","HEAD",PREFIX+"/").decode().splitlines()
    winner=git(roots["selected"],"ls-tree","-r","--name-only","HEAD",PREFIX+"/").decode().splitlines()
    forbid=PREFIX+"/Core/HostSlotLease.cs"
    require(forbid in losing and forbid not in winner and
            forbid+".meta" in losing and forbid+".meta" not in winner,
            "COMPETING_644_LEASE_SOURCE_OR_GUID_MIXED")
    # #604 separate fence is still never an authorized R3 source.
    require(PREFIX+"/Core/CheckpointFence.cs" not in winner,
            "OLD_ALT604_CHECKPOINT_FENCE_MIXED")
    return {"selected":R3,"other_r3_alt_hold":OTHER_R3,
            "old_604_alt_hold":ALT604,"frozen_R2":R2,
            "r3_changed_core_blob":PINS[PREFIX+"/Core/AtomicWorldSwap.cs"],
            "never_stage_alt_644_source":forbid}

def original_author(roots,safe):
    owner=load(roots["selected"]/
        "Tools/B01/B01-23/P17W15UnitySaveBridge001/package.py",
        "b0140_w19_exact_owner_r3_author")
    original_raw=owner.build()
    rawfiles=owner.verify(original_raw)
    require(len(original_raw)==SOURCE_ZIP_BYTES and sha(original_raw)==SOURCE_ZIP_SHA256,
            "R3_SOURCE_ARCHIVE_FINGERPRINT_UNEXPECTED")
    require(len(rawfiles)==23 and len(owner.scope())==22,
            "R3_WRONG_ORIGINAL_GIT_AUTHOR_PACKAGE_MEMBERS")
    manifest=json.loads(rawfiles["PACKAGE_MANIFEST.json"])
    require(manifest["package"]=="B01-23-UNITY-SAVE-HOST-BRIDGE-001" and
            manifest["version"]=="3.0.0" and manifest["own_head_sha"]==R3 and
            manifest["gameplay_510_sha"]==GAME and
            manifest["only_save_source_sha"]==SAVE and
            manifest["unity_guid_count"]==10 and manifest["asmdef_count"]==3 and
            manifest["native_unity"]=="NOT_RUN" and
            manifest["canonical_510_live_import"]=="HOLD" and
            manifest["codex_ack"] is False,
            "R3_ORIGINAL_OWNER_MANIFEST_OR_RELEASE_GATE_FORGED")
    entries=manifest["files"]
    require(len(entries)==22 and len({r["path"] for r in entries})==22,
            "R3_WRONG_AUTHOR_FILE_COUNT")
    require(set(rawfiles)=={r["path"] for r in entries}|{"PACKAGE_MANIFEST.json"},
            "R3_ZIP_CONTAINS_FOREIGN_OR_MISSING_AUTHOR_MEMBER")
    for r in entries:
        path=r["path"];raw=rawfiles[path]
        require(raw==git(roots["selected"],"show","HEAD:"+path) and
                r["bytes"]==len(raw) and r["sha256"]==sha(raw),
                "R3_AUTHORIGINAL_GIT_BYTES_MANIFEST_MISMATCH:"+path)
    assets={path:raw for path,raw in rawfiles.items()
            if path==PREFIX+".meta" or path.startswith(PREFIX+"/")}
    require(len(assets)==16 and len(safe.validate_guids(assets))==10,
            "R3_SELECTED_UNITY_ASSET_OR_META_SCOPE_DIFFERENT")
    assemblies={}
    for path,raw in assets.items():
        if path.endswith(".asmdef"):
            item=json.loads(raw);assemblies[item["name"]]=item
    require(set(assemblies)=={
        "B0123.UnitySaveBridge.Core",
        "B0123.UnitySaveBridge.Unity",
        "B0123.UnitySaveBridge.PlayModeTests"},"R3_ASSEMBLY_PROVENANCE_DRIFT")
    require(assemblies["B0123.UnitySaveBridge.Core"]["noEngineReferences"] is True and
            assemblies["B0123.UnitySaveBridge.Core"]["references"]==
                ["B0123.DetachedSaveContract.Runtime","B0123.DebrisRepulse.Runtime"] and
            "B0123.UnitySaveBridge.Core" in assemblies["B0123.UnitySaveBridge.Unity"]["references"] and
            "B0123.SecureReattachV2.Runtime" in assemblies["B0123.UnitySaveBridge.Unity"]["references"] and
            "UNITY_INCLUDE_TESTS" in
                assemblies["B0123.UnitySaveBridge.PlayModeTests"]["defineConstraints"],
            "R3_UNITY_ASMDEF_DEPENDENCY_GRAPH_DRIFT")
    return assets,{"head":R3,"physical_bytes":len(original_raw),
       "physical_members":len(rawfiles),"physical_zip_sha256":sha(original_raw),
       "original_git_members":22,"unity_asset_members":len(assets),
       "unity_meta_guids":10,"asmdefs":len(assemblies),"version":"3.0.0"}

def w19_guids_and_rollbacks(roots,safe,w17,w18,assets,source):
    # Original W17 executable physical clone + negative suite is reused
    # unchanged, with its sole parameter (source HEAD) bound to NEW exact R3.
    w17.BRIDGE_SHA=R3
    oldresult=w17.actual_tests(roots,safe,assets,
                              {"physical_zip_sha256":source["physical_zip_sha256"]})
    require(oldresult["negative_count"]==18 and
            len(oldresult["positive_runs"])==2 and
            oldresult["symlink"]=="PASS",
            "ORIGINAL_SAFE_RECEIVER_PHYSICAL_TEST_SUITE_FAILED")
    core,_=safe.source_files(roots["game"],roots["save"])
    require(len(core)==37 and len(safe.validate_guids(core))==23,
            "FROZEN_GAME_SAVE_SOURCE_DRIFT")
    combined={**core,**assets}
    require(len(combined)==53 and len(safe.validate_guids(combined))==33,
            "R3_GUID_CLOSURE_COLLISION_510_556_642")
    # Execute immutable previous GUID-only reference regression on true R3:
    # selected source permitlist from original source-owned .cs.meta only.
    validator=w18.validate_script_refs
    refs=w18.meta_guids(combined)
    require(len(refs)==33,"R3_ALL_META_GUID_REFERENCE_INVENTORY_CHANGED")
    with tempfile.TemporaryDirectory(prefix="b0140-w19-exact642-g0-") as tmp:
        root=Path(tmp)
        host=root/"isolated_unity_host"
        (host/"Assets/FOREIGN").mkdir(parents=True)
        for p,b in {
           "ProjectSettings/ProjectVersion.txt":
               git(roots["game"],"show","HEAD:ProjectSettings/ProjectVersion.txt"),
           "Packages/manifest.json":
               git(roots["game"],"show","HEAD:Packages/manifest.json"),
           "Assets/FOREIGN/myscene.unity":b"%YAML 1.1\n--- !u!1 &4\nGameObject:\n  m_Name: OtherUser\n",
        }.items():
            f=host/p;f.parent.mkdir(parents=True,exist_ok=True);f.write_bytes(b)
        before,_=safe.crawl(host)
        safe.preflight_host(host,combined)
        good={**combined,**before}
        require(validator(good,w18.meta_guids(good))=={},
                "R3_ORIGINAL_SOURCE_EMPTY_YAML_UNEXPECTED_M_SCRIPT")
        # R3 #644 *real* source GUID must be rejected despite legacy name absent.
        other_meta=git(roots["loser_r3"],"show","HEAD:"+
                       PREFIX+"/Core/HostSlotLease.cs.meta")
        match=w18.META_GUID_RE.search(other_meta.decode())
        require(match is not None,"R3_ALT_644_REAL_META_GUID_MISSING")
        alt_guid=match.group(1).lower()
        require(alt_guid not in refs,"ALT_644_META_GUID_ALREADY_INSTALLED")
        yaml=(f"%YAML 1.1\n--- !u!114 &114\nMonoBehaviour:\n"
              f"  m_Script: {{fileID: 11500000, guid: {alt_guid}, type: 3}}\n"
              f"  m_Name: NoVisibleTypeName\n").encode()
        foreign=host/"Assets/FOREIGN/rogue.prefab"
        foreign.write_bytes(yaml)
        foreignbefore,_=safe.crawl(host)
        # Old source receiver recognizes no ALT C# type name in serialized YAML.
        safe.preflight_host(host,combined)
        failed_dest=root/"must_not_publish"
        try:
            validator({**combined,**foreignbefore},
                      w18.meta_guids({**combined,**foreignbefore}))
        except Exception as e:
            require(e.__class__.__name__=="Reject" and
                    "UNRESOLVED_OR_LEGACY_SCRIPT_GUID" in str(e),
                    "R3_ALT_644_REJECTION_WRONG_CAUSE")
        else:raise Hold("RECEIVER_ADMITTED_REAL_ALT_644_SCRIPT_GUID")
        require(not failed_dest.exists() and safe.crawl(host)[0]==foreignbefore,
                "SOURCE_GUID_GATE_WROTE_TO_USER_HOST")
        # A valid source GUID-only serialized reference must be allowed.
        candidate_meta=next(p for p in assets if p.endswith(".cs.meta"))
        ok_guid=w18.META_GUID_RE.search(assets[candidate_meta].decode()).group(1)
        foreign.write_bytes(yaml.replace(alt_guid.encode(),ok_guid.encode()))
        legit,_=safe.crawl(host)
        result=validator({**combined,**legit},w18.meta_guids({**combined,**legit}))
        require("Assets/FOREIGN/rogue.prefab" in result and
                len(result["Assets/FOREIGN/rogue.prefab"])==1,
                "R3_SOURCE_SCRIPT_REFERENCE_FALSE_REJECTION")
        # Prove one guarded *actual* original stage & rollback with legitimate
        # foreign scene (no modification of canonical Unity project).
        source_summary=dict(oldresult["combined"])
        temp_dest=root/"accepted_new_scratch_only"
        ownership,dirs=safe.create_candidate(host,temp_dest,combined,source_summary,
                                         roots["game"],roots["save"],roots["selected"])
        require(temp_dest.exists() and safe.crawl(temp_dest)==(ownership,dirs),
                "GUARDED_R3_DISPOSABLE_HOST_PHYSICAL_STAGE_DID_NOT_PUBLISH")
        safe.rollback_candidate(temp_dest,ownership,dirs)
        require(not temp_dest.exists(),"R3_GUID_GUARDED_ROLLBACK_NOT_COMPLETE")
    return {"unchanged_old_receipt_negative_pass":oldresult["negative_count"],
      "unchanged_old_receipt_clone_and_rollback":len(oldresult["positive_runs"]),
      "symlink":oldresult["symlink"],"r3_combined_files":len(combined),
      "r3_combined_unique_guids":len(refs),
      "selected_r3_source_guid_only_positive":"PASS",
      "losing_r3_644_real_script_guid_only_negative":"PASS",
      "one_additional_guid_guarded_scratch_stage_rollback":"PASS",
      "losing_r3_644_GUID":alt_guid}

def main():
    ap=argparse.ArgumentParser()
    for key in ("selected","loser_r3","r2","alt604","game","save","safe",
                "oldg0","guidfence","output"):
        ap.add_argument("--"+key,type=Path,required=True)
    args=ap.parse_args()
    roots={key:getattr(args,key).resolve() for key in
           ("selected","loser_r3","r2","alt604","game","save","safe",
            "oldg0","guidfence")}
    # Immutable original W17 utility expects the candidate checkout key "bridge".
    # Bind it to the one verified owner-selected R3; never to a loser.
    roots["bridge"]=roots["selected"]
    provenance=pin_roots(roots)
    safe=load(roots["safe"]/
       "Tools/B01/B01-40/P17W15AtomicReceiverMigration/migration.py",
       "original_b0140_unmodified_w15_safe")
    oldg0=load(roots["oldg0"]/
       "Tools/B01/B01-40/P17W17Selected596G0/intake.py",
       "original_b0140_unmodified_w17_physical")
    guid=load(roots["guidfence"]/
       "Tools/B01/B01-40/P17W18SerializedScriptGuidFence/fence.py",
       "original_b0140_unmodified_w18_guid_only")
    selected,physical=original_author(roots,safe)
    migration=w19_guids_and_rollbacks(roots,safe,oldg0,guid,selected,physical)
    # Owner B01-23 explicitly documents two UNRESOLVED issues for exact #642:
    # #637 durable Commit may persist and then throw; OnDisable releases a
    # still-published active host lease. No claim of independent C# GREEN.
    record={"package_id":"B01-40-P17W19-ONE-R3-642-PHYSICAL-G0-HOLD",
      "source_selection":provenance,"physical_source":physical,
      "safe_source_migration":migration,
      "physical_zip_guid_asmdef_original_scratch":"PASS",
      "security_release":"HOLD_OWNER_REPORTED_COMMIT_THROW_AND_ONDISABLE_LEASE",
      "formal_r3_G0":"HOLD_PENDING_INDEPENDENT_R3_SECURITY_QA_AND_OWNER_FIX",
      "old_r2_596_source_G0":"ACCEPTED_SOURCE_ONLY_PRIOR_SHA_NOT_TRANSFERRED",
      "real_Unity_G1":"NOT_RUN","live_canonical_510_save_load":"HOLD",
      "g2":"HOLD","codex_ack":False,"no_merge":True,
      "integrator_feedback":"NOT_MEASURED",
      "lifecycle":{"IMPLEMENTED":True,"TESTED":"SOURCE_PHYSICAL_ONLY",
         "QA_ACCEPTED":False,"READY_FOR_INTEGRATOR":False,
         "INTEGRATED":False,"RELEASED":False}}
    args.output.mkdir(parents=True,exist_ok=True)
    (args.output/"selected-r3-642-physical-g0-hold.json").write_text(
        json.dumps(record,sort_keys=True,indent=2)+"\n")
    print("B0140_W19_OWNER_SELECTED_R3_SOURCE",json.dumps(provenance,sort_keys=True))
    print("B0140_W19_EXACT_AUTHOR_ORIGINAL_PHYSICAL",json.dumps(physical,sort_keys=True))
    print("B0140_W19_ORIGINAL_SAFE_SCRATCH_R3_MIGRATION",json.dumps(migration,sort_keys=True))
    print("B0140_W19_PHYSICAL_PASS_FORMAL_R3_G0_HOLD_UNRESOLVED_P0_NO_NATIVE_NO_MERGE")
if __name__=="__main__":
    try:main()
    except (Hold,AssertionError,KeyError,ValueError,OSError,
            subprocess.SubprocessError) as e:
        print("B0140_W19_FAILURE",str(e));raise SystemExit(1)
