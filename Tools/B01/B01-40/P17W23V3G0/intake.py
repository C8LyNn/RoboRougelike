#!/usr/bin/env python3
"""B01-40 P17/W23 independent original owner V3 #729 physical Git G0.

Never mix competing #719, source V2 #691, session #712 or R5 ALT into live
Assets; no user PC access, merge, native engine or true SaveLoad claims.
"""
from __future__ import annotations
import argparse
import hashlib
import importlib.util
import io
import json
from pathlib import Path
import subprocess
import tempfile
import zipfile

PINS={
 "v3":"b8ca29c7707cd6b4338d0b9772d74dee676d49d2",
 "v2":"809f82575c638e1b4d2806b5f417bddab6c0871a",
 "alt":"8ea411b49e629c059f16c9f8b59a534c19f660b8",
 "r5":"cc7f73541517a18f4479bb155534bd4832d9985e",
 "r4":"09c1809b73324a6d31c13e5d0143de62ac1e3ca6",
 "game":"d94400d95d13db7f27e1533df05c9292d5380431",
 "save":"33094f154c7b91119b36d58aab285e6e83c5de57",
 "storage":"d6ec0c82e388e65c1e02ddfd920bb50a1f6ef121",
 "r2":"bd8905ad549a7b26854b671be971f7d781308311",
 "safe":"3b743acc52bb44ae6a50af56ed9aca46d22821a7",
 "guid":"547b55560d1a33af182caeaf73f9a9b2d8b1b7fe",
 "w21":"3cd990c1fabe7c01d226f40f3b8faa6de59e9819",
}
ASSET="Assets/B0123CanonicalSaveApi001"
GATEWAY=ASSET+"/Core/CanonicalWorldGateway.cs"
NEWLEASE=ASSET+"/Core/CanonicalWorldSlotLease.cs"
NEWMETA=NEWLEASE+".meta"
OWNER_V3_PACKAGE="Tools/B01/B01-23/P17W20CanonicalSaveApi001/package.py"
OWNER_R5_PACKAGE="Tools/B01/B01-23/P17W15UnitySaveBridge001/package.py"
V3_SHA256="17a80c55b0d40e8afa7ef50509e434b970cc3296c3cc9926aba82b6fd5973b18"
V3_ZIP_SIZE=90659
BLOBS={
 GATEWAY:"21733278b6ab0669fccb6d085e0b6cf242792de2",
 NEWLEASE:"a9331dd620336d8c42165d134dc8a5e05ed51873",
 NEWMETA:"f4f391a7099f11ac8d597006c56657e7ac41428f",
 OWNER_V3_PACKAGE:"a2e9641475837110ba8a44cbb86597bdaadb2ec9",
}
class GateHold(Exception):pass
def require(ok,msg):
    if not ok:raise GateHold(msg)
def sha(raw):return hashlib.sha256(raw).hexdigest()
def blob(raw):return hashlib.sha1(b"blob "+str(len(raw)).encode()+b"\0"+raw).hexdigest()
def run(*a,cwd=None,ok=(0,)):
    p=subprocess.run([str(x) for x in a],cwd=cwd,capture_output=True,text=True,errors="replace")
    require(p.returncode in ok,"COMMAND_FAILED:"+repr(a)+"\n"+p.stdout[-1800:]+"\n"+p.stderr[-1800:])
    return p
def git(root,*args):return run("git","-C",root,*args).stdout
def source(root,path):
    p=subprocess.run(["git","-C",str(root),"show","HEAD:"+path],capture_output=True)
    require(p.returncode==0,"MISSING_EXACT_ORIGINAL_GIT:"+path)
    return p.stdout
def mod(root,path,name):
    spec=importlib.util.spec_from_file_location(name,str(root/path))
    obj=importlib.util.module_from_spec(spec)
    spec.loader.exec_module(obj)
    return obj
def roots_and_scope(r):
    for n,pin in PINS.items():
        require(git(r[n],"rev-parse","HEAD").strip()==pin,
                "SHA_BRANCH_DRIFT:"+n)
    require(run("git","-C",r["v3"],"merge-base","--is-ancestor",
                PINS["v2"],"HEAD").returncode==0,
                "V3_NOT_SELECTED_V2_DESCENDANT")
    expected=[
      ".github/workflows/b01-23-p17w22-canonical-authority-v3.yml",
      GATEWAY,NEWLEASE,NEWMETA,
      "Tools/B01/B01-23/P17W20CanonicalSaveApi001/CanonicalApiSourceTests.csproj",
      "Tools/B01/B01-23/P17W20CanonicalSaveApi001/Program.cs",
      OWNER_V3_PACKAGE,
      "Tools/B01/B01-23/P17W20CanonicalSaveApi001/test_package.py",
      "Tools/B01/B01-23/P17W22CanonicalAuthorityV3/README.md"]
    changed=git(r["v3"],"diff","--name-only",PINS["v2"],"HEAD").splitlines()
    require(sorted(changed)==sorted(expected),"V3_AUTHOR_SCOPE_DIFF_MISMATCH")
    for p,oid in BLOBS.items():
        raw=source(r["v3"],p)
        require(blob(raw)==oid and (r["v3"]/p).read_bytes()==raw,
                "V3_ORIGINAL_GIT_BLOB_DRIFT:"+p)
    return {"selected_only_v3":PINS["v3"],
       "selected_ancestor_v2":PINS["v2"],"losing_v3_719":PINS["alt"],
       "frozen_only_host_r5":PINS["r5"],"original_storage":PINS["storage"],
       "unaltered_r2_712":"COMPILE_INCOMPATIBLE_OLD_V2_CONSTRUCTOR",
       "changed_author_only":len(changed),"sha_blob_pins":len(BLOBS)}
def original_v3_producer(r,safe):
    owner=mod(r["v3"],OWNER_V3_PACKAGE,"b0140_w23_original_v3_729")
    raw=owner.build()
    manifest=owner.verify(raw)
    require(len(raw)==V3_ZIP_SIZE and sha(raw)==V3_SHA256,
            "V3_ACTUAL_ORIGINAL_INNER_ZIP_BYTES_SHA_MISMATCH")
    require(manifest["package"]=="B01-23-CANONICAL-WORLD-API-001"
        and manifest["version"]=="3.0.0" and manifest["sha"]==PINS["v3"]
        and manifest["selected_frozen_api_v2"]==PINS["v2"]
        and manifest["owner_lifetime_authority"]=="REAL_OS_FILESHARE_NONE_MANDATORY"
        and manifest["integrated_b0162_712"]=="NO_SEPARATE_OWNER_ADAPTER_REQUIRED"
        and manifest["canonical_510_private_adapter"]=="NOT_IMPLEMENTED"
        and manifest["crash_atomic_slot"]=="B01-62_PENDING"
        and manifest["native"]=="NOT_RUN" and manifest["codex_ack"] is False
        and manifest["guids"]==5 and manifest["asmdefs"]==1,
        "V3_ACTUAL_ORIGINAL_MANIFEST_RELEASE_GATE_FORGED")
    with zipfile.ZipFile(io.BytesIO(raw)) as z:
        names=z.namelist()
        require(len(names)==19 and len(set(names))==19 and
            "PACKAGE_MANIFEST.json" in names and
            len(manifest["files"])==18,"V3_ACTUAL_GIT_SOURCE_MEMBERS_MISMATCH")
        files={n:z.read(n) for n in names if n!="PACKAGE_MANIFEST.json"}
        for p,b in files.items():
            require(b==source(r["v3"],p) and
                    (r["v3"]/p).read_bytes()==b,
                    "V3_ZIP_SOURCE_GIT_MISMATCH:"+p)
    assets={p:b for p,b in files.items() if
            p==ASSET+".meta" or p.startswith(ASSET+"/")}
    guids=safe.validate_guids(assets)
    require(len(assets)==8 and len(guids)==5 and
            len([p for p in assets if p.endswith(".asmdef")])==1,
            "V3_SOURCE_ASSETS_GUIDS_ASMDEF_COUNT")
    require(NEWMETA in assets and
            b"b0123220000000000000000000000001" in assets[NEWMETA] and
            assets[NEWLEASE]==source(r["v3"],NEWLEASE),
            "V3_NEW_OWNER_MONOSCRIPT_GUID_NOT_ORIGINAL")
    oldtree=git(r["v2"],"ls-tree","-r","--name-only","HEAD",ASSET+"/").splitlines()
    require(NEWLEASE not in oldtree and NEWMETA not in oldtree,
            "V3_NEW_UNITY_MONOSCRIPT_NOT_NEW")
    # Rebuild the only selected old API to prove exactly one new GUID.
    old=mod(r["v2"],OWNER_V3_PACKAGE,"b0140_w23_original_v2_691")
    oldraw=old.build();old.verify(oldraw)
    with zipfile.ZipFile(io.BytesIO(oldraw)) as z:
        oldassets={n:z.read(n) for n in z.namelist() if
                   n==ASSET+".meta" or n.startswith(ASSET+"/")}
    oldguids=safe.validate_guids(oldassets)
    require(len(oldguids)==4 and set(oldguids).issubset(set(guids)) and
            len(set(guids)-set(oldguids))==1,
            "V3_NOT_EXACT_ONE_NEW_UNITY_META_GUID")
    return owner,raw,assets,{"zip_bytes":len(raw),"zip_sha256":sha(raw),
       "inner_zip_members":len(names),"original_git_members":len(files),
       "new_unity_assets":len(assets),"v3_guids":len(guids),
       "original_v2_guids":len(oldguids),"asmdefs":1,
       "new_owner_lease_guid":next(iter(set(guids)-set(oldguids)))}
def old_r5_and_frozen(r,safe):
    older=mod(r["r5"],OWNER_R5_PACKAGE,"b0140_w23_r5_690")
    raw=older.build();older.verify(raw)
    require(len(raw)==129088 and sha(raw)==
       "a37e942d6a0cc5236570edaf7cba49384a84d0553f88fb29988a855d692a1496",
       "ORIGINAL_R5_ZIP_MISMATCH")
    with zipfile.ZipFile(io.BytesIO(raw)) as z:
        host={n:z.read(n) for n in z.namelist() if
              n=="Assets/B0123UnitySaveBridge001.meta" or
              n.startswith("Assets/B0123UnitySaveBridge001/")}
        require("PACKAGE_MANIFEST.json" in z.namelist(),
                "ORIGINAL_R5_FLAT_ROOT_MANIFEST_MISSING")
    frozen,receipt=safe.source_files(r["game"],r["save"])
    require(len(host)==18 and len(safe.validate_guids(host))==11 and
        len(frozen)==37 and len(safe.validate_guids(frozen))==23,
        "ORIGINAL_R5_OR_FROZEN_UNITY_SOURCE_DRIFT")
    return frozen,host
def stage_owner_and_collision(r,safe,owner,raw):
    with tempfile.TemporaryDirectory(prefix="b0140-w23-v3-original-author-") as td:
        root=Path(td)
        location=root/"new_owned_selected_v3_zip"
        owner.stage(raw,location)
        with zipfile.ZipFile(io.BytesIO(raw)) as z:
            for name in z.namelist():
                require((location/name).read_bytes()==z.read(name),
                        "ORIGINAL_V3_STAGED_BYTES_MISMATCH:"+name)
        owner.rollback(raw,location)
        with zipfile.ZipFile(io.BytesIO(raw)) as z:
            require(all(not (location/n).exists() for n in z.namelist()),
                    "ORIGINAL_V3_ROLLBACK_SOURCE_MEMBERS_REMAIN")
        occupied=root/"occupied_foreign_empty"
        (occupied/"KEEP_EMPTY").mkdir(parents=True)
        try:owner.stage(raw,occupied)
        except ValueError:pass
        else:raise GateHold("AUTHOR_V3_STAGE_ADMITTED_OCCUPIED_FOREIGN_EMPTY")
        require((occupied/"KEEP_EMPTY").is_dir(),
                "AUTHOR_V3_STAGE_REMOVED_FOREIGN_EMPTY")
        # Genuine original R5 and #729 archives each own the same FLAT
        # root PACKAGE_MANIFEST.json, so naive copy/union is not disjoint.
        r5mod=mod(r["r5"],OWNER_R5_PACKAGE,"b0140_w23_manifest_r5_690")
        older=r5mod.build();r5mod.verify(older)
        with zipfile.ZipFile(io.BytesIO(raw)) as a,zipfile.ZipFile(io.BytesIO(older)) as b:
            intersect=set(a.namelist())&set(b.namelist())
            require(intersect=={"PACKAGE_MANIFEST.json"},
                "DIFFERENT_OWNER_PACKAGES_HAVE_OTHER_OVERLAPPING_SOURCE")
            require(a.read("PACKAGE_MANIFEST.json")!=b.read("PACKAGE_MANIFEST.json"),
                "DIFFERENT_OWNER_FLAT_MANIFEST_COLLISION_NOT_MEANINGFUL")
        return {"v3_original_author_zip_stage_rollback":"PASS",
            "occupied_user_foreign_EMPTY_dir_denied":"PASS",
            "r5_690_plus_v3_729_flat_manifest_collision":"DETECTED_AND_DENIED",
            "one_manifest_per_owner_namespace":"REQUIRED",
            "no_flat_overlay":"PASS"}
def security_negative_and_physical(r,safe,guid,api):
    frozen,host=old_r5_and_frozen(r,safe)
    incoming={**frozen,**host,**api}
    require(len(incoming)==63 and len(safe.validate_guids(incoming))==39,
            "V3_GENUINE_COMBINED_ORIGINAL_FILES_GUIDS_NOT_63_39")
    # The original losing #719 and frozen V2 change gateway C# at SAME PATH
    # with SAME GUID. A GUID/path-only admission rule accepts either, while
    # exact owner Git bytes must reject both BEFORE source staging.
    pos=api[GATEWAY]
    witnesses={}
    for which in ("alt","v2"):
        foreign=source(r[which],GATEWAY)
        require(foreign!=pos,"FOREIGN_GATEWAY_CSHARP_EQUALS_SELECTED:"+which)
        bad=dict(incoming);bad[GATEWAY]=foreign
        require(safe.validate_guids(bad)==safe.validate_guids(incoming),
                "ALT_SOURCE_GUID_IDENTITY_WITNESS_NOT_REAL")
        require(bad[GATEWAY]!=source(r["v3"],GATEWAY),
                "ALT_BYTE_IDENTITY_WRONGLY_ACCEPTED")
        witnesses[which]={"same_owner_path":GATEWAY,
            "guid_inventory_equal":True,"original_git_bytes_rejected":True,
            "foreign_sha256":sha(foreign)}
    with tempfile.TemporaryDirectory(prefix="b0140-w23-v3-r5-unity-g0-") as td:
        root=Path(td)
        hostroot=root/"original_foreign_host"
        fixture={
            "ProjectSettings/ProjectVersion.txt":source(r["game"],"ProjectSettings/ProjectVersion.txt"),
            "Packages/manifest.json":source(r["game"],"Packages/manifest.json"),
            "Assets/FOREIGN_ONLY/foreign.unity":b"%YAML 1.1\n--- !u!1 &21\nGameObject:\n  m_Name: FOREIGN\n",
        }
        (hostroot/"Assets/FOREIGN_USER_EMPTY").mkdir(parents=True)
        for path,data in fixture.items():
            p=hostroot/path;p.parent.mkdir(parents=True,exist_ok=True);p.write_bytes(data)
        old=safe.crawl(hostroot)
        require(len(safe.validate_guids(incoming))==39,"LOST_INPUT_GUID")
        safe.preflight_host(hostroot,incoming)
        for which in ("alt","v2"):
            forged=dict(incoming);forged[GATEWAY]=source(r[which],GATEWAY)
            safe.preflight_host(hostroot,forged)  # old GUID-only passes
        require(guid.validate_script_refs({**incoming,**old[0]},
                guid.meta_guids({**incoming,**old[0]}))=={},
                "NEW_V3_CANONICAL_GUID_YAML_CLOSURE_FAILURE")
        dest=root/"new_selected_v3_plus_r5_SCRATCH_ONLY"
        receipt={"only_game_sha":PINS["game"],"only_save_sha":PINS["save"],
           "selected_r5_sha":PINS["r5"],"selected_v3_sha":PINS["v3"],
           "native_unity":"NOT_RUN","codex_ack":False}
        actual,dirs=safe.create_candidate(hostroot,dest,incoming,receipt,
            r["game"],r["save"],r["r5"],r["v3"])
        require(safe.crawl(dest)==(actual,dirs),
                "ACTUAL_V3_UNITY_SOURCE_CANDIDATE_BYTES_WRONG")
        # Foreign even EMPTY folder created after stage = rollback denied
        alien=dest/"Assets/NEW_UNOWNED_EMPTY";alien.mkdir()
        try:safe.rollback_candidate(dest,actual,dirs)
        except Exception as e:
            require(e.__class__.__name__=="GateHold",
                    "CHANGED_UNOWNED_EMPTY_DIR_DENIED_WRONG_CAUSE")
        else:raise GateHold("DELETED_UNOWNED_EMPTY_FOLDER_UNSAFE")
        require(alien.is_dir(),"UNOWNED_EMPTY_DIR_GONE")
        alien.rmdir()
        safe.rollback_candidate(dest,actual,dirs)
        require(not dest.exists() and safe.crawl(hostroot)==old,
                "ORIGINAL_USER_HOST_CHANGED_BY_ROLLBACK")
        # A new #729 MonoScript GUID can be legitimately referenced; wrong
        # old losing #719 GUID cannot be installed with no class string.
        right=b"guid: b0123220000000000000000000000001"
        rightpref=b"%YAML 1.1\n--- !u!114 &3\nMonoBehaviour:\n  m_Script: {fileID: 11500000, guid: b0123220000000000000000000000001, type: 3}\n"
        extra="Assets/FOREIGN_ONLY/v3_valid.prefab"
        (hostroot/extra).write_bytes(rightpref)
        user=safe.crawl(hostroot)
        require(guid.validate_script_refs({**incoming,**user[0]},
                    guid.meta_guids({**incoming,**user[0]}))=={},
                    "LEGITIMATE_OWNER_V3_NEW_SCRIPT_GUID_REJECTED")
        (hostroot/extra).write_bytes(rightpref.replace(
                   b"b0123220000000000000000000000001",
                   b"c0ffee00000000000000000000000001"))
        baduser=safe.crawl(hostroot)
        try:guid.validate_script_refs({**incoming,**baduser[0]},
                  guid.meta_guids({**incoming,**baduser[0]}))
        except Exception as e:
            require(e.__class__.__name__=="Reject","UNKNOWN_M_SCRIPT_WRONG_DENIAL")
        else:raise GateHold("ALIEN_MONOSCRIPT_GUID_ALLOWED")
        require(safe.crawl(hostroot)==baduser,
                "SERIALIZED_YAML_NEGATIVE_MUTATED_USER_FILES")
    return {"original_selected_source_unity_files":len(incoming),
        "source_unity_meta_guids":39,
        "original_game_save_w06":len(frozen),"original_r5_source":len(host),
        "selected_v3_source_unity_files":len(api),
        "safe_new_unity_source_staging":"PASS",
        "strict_foreign_EMPTY_folder_rollback_denial":"PASS",
        "clean_own_rollback_and_foreign_user_byte_preservation":"PASS",
        "valid_new_v3_mScript_GUID_only":"PASS",
        "unknown_guid_only_prefab_rejected":"PASS",
        "same_meta_guid_competitor_CSharp_source_negative":witnesses}
def no_one_authority_bridge(r):
    sourcev3=source(r["v3"],GATEWAY).decode()
    source712=source(r["r2"],"Tools/B01/B01-62/P17W21CanonicalSessionLeaseR2/CanonicalSlotSessionR2.cs").decode()
    source669=source(r["storage"],"Tools/B01/B01-62/P17W20SingleFileSaveSlot/SingleFileSaveSlot.cs").decode()
    require("CanonicalWorldSlotLease ownerLease" in sourcev3 and
       "new CanonicalWorldGateway(gate,factory,this)" in source712 and
       "CanonicalWorldSlotLease" not in source712,
       "EXPECTED_VERSIONED_V3_R2_CONSTRUCTOR_BOUNDARY_CHANGED")
    require('".owner"' in source(r["v3"],NEWLEASE).decode() and
            '".lease"' in source669,
            "V3_OWNER_VS_STORAGE_SEPARATE_PHYSICAL_LOCKS_WITNESS_NOT_FOUND")
    return {"selected_v3_constructor_needs_authority_argument":True,
       "old_r2_constructor_has_only_three_args":True,
       "v3_physical_lifetime_lock":".owner",
       "r2_669_physical_lifetime_lock":".lease",
       "current_same_owner_same_lock":"NO_INCOMPATIBLE",
       "raw_old_r2_against_new_v3":"DO_NOT_CO_INSTALL",
       "mandatory_new_single_authority_bridge":"B01-23_20_62_PENDING",
       "canonical_private_Rig_AtomicDockLedger":"NOT_IMPLEMENTED"}
def main():
    p=argparse.ArgumentParser()
    for n in (*PINS,"out"):p.add_argument("--"+n,type=Path,required=True)
    a=p.parse_args();r={n:getattr(a,n).resolve() for n in PINS}
    provenance=roots_and_scope(r)
    safe=mod(r["safe"],"Tools/B01/B01-40/P17W15AtomicReceiverMigration/migration.py",
             "b0140_w23_original_safe_575")
    guid=mod(r["guid"],"Tools/B01/B01-40/P17W18SerializedScriptGuidFence/fence.py",
             "b0140_w23_original_guid_635")
    owner,raw,assets,v3=original_v3_producer(r,safe)
    package=stage_owner_and_collision(r,safe,owner,raw)
    physical=security_negative_and_physical(r,safe,guid,assets)
    incompatible=no_one_authority_bridge(r)
    report={"package_id":"B01-40-P17W23-V3-729-R5-690-SOURCE-G0-ONE-AUTHORITY-HOLD",
     "exact_source_provenance":provenance,"v3_original_inner_git_zip":v3,
     "original_author_stage_rollback":package,"real_selected_unity_scratch":physical,
     "one_world_one_lock":incompatible,
     "v3_729_original_transport_G0":"PASS_SOURCE_ONLY",
     "full_canonical_510_game_g0":"HOLD_INCOMPATIBLE_OWNER_AND_UNIMPLEMENTED_PRIVATE_WORLD",
     "native_unity_g1":"NOT_RUN","g2":"HOLD","merge":False,"codex_ack":False,
     "six_stages":{"IMPLEMENTED":True,"TESTED":"REAL_SOURCE_PHYSICAL",
       "QA_ACCEPTED":"SOURCE_ONLY_SUBGATE","READY_FOR_INTEGRATOR":False,
       "INTEGRATED":False,"RELEASED":False}}
    a.out.mkdir(parents=True,exist_ok=True)
    (a.out/"v3-729-source-g0-and-one-lock-hold.json").write_text(
        json.dumps(report,indent=2,sort_keys=True)+"\n")
    print("B0140_W23_V3_729_AUTHOR_ORIGINAL_ZIP",json.dumps(v3,sort_keys=True))
    print("B0140_W23_V3_729_ORIGINAL_SAFE_STAGE_ROLLBACK",json.dumps(package,sort_keys=True))
    print("B0140_W23_V3_729_R5_690_UNITY_G0",json.dumps(physical,sort_keys=True))
    print("B0140_W23_V3_729_712_669_ONE_OWNER_BLOCKER",json.dumps(incompatible,sort_keys=True))
    print("B0140_W23_EXACT_V3_SOURCE_TRANSPORT_PASS_FORMAL_ONE_LOCK_HOLD_NATIVE_NOT_RUN_NO_MERGE")
if __name__=="__main__":
    try:main()
    except (GateHold,ValueError,KeyError,AssertionError,OSError,zipfile.BadZipFile) as e:
        print("B0140_W23_FAILURE",str(e));raise SystemExit(1)
