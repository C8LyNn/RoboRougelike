#!/usr/bin/env python3
"""B01-40 / P17 W21: independently execute ORIGINAL owner API V2 #691 packager.

SOURCE-ONLY original Git ZIP / asmdef / Unity GUID / source byte identity /
NEW disposable isolated host staging+strict rollback. No Unity native G1, no
canonical Rig[]/AtomicDockLedger integration, no Codex permission/merge.
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

API_V2="809f82575c638e1b4d2806b5f417bddab6c0871a"
API_V1="eed2e39636edfd8b8db754c426db452164cbf9d7"
API_ALT="5387e829a5f6d38b713086e6fb906a9a755189ff"
R5="cc7f73541517a18f4479bb155534bd4832d9985e"
R4="09c1809b73324a6d31c13e5d0143de62ac1e3ca6"
GAME="d94400d95d13db7f27e1533df05c9292d5380431"
SAVE="33094f154c7b91119b36d58aab285e6e83c5de57"
SAFE="3b743acc52bb44ae6a50af56ed9aca46d22821a7"
GUIDFENCE="547b55560d1a33af182caeaf73f9a9b2d8b1b7fe"
API_PREFIX="Assets/B0123CanonicalSaveApi001"
HOST_PREFIX="Assets/B0123UnitySaveBridge001"
ZIP_SIZE=61736
ZIP_SHA256="90198e6429d912a40e4e2df3935281627282f14799c4f34479439af150f5bccd"
HOST_ZIP_SIZE=129088
HOST_ZIP_SHA256="a37e942d6a0cc5236570edaf7cba49384a84d0553f88fb29988a855d692a1496"
GATEWAY=API_PREFIX+"/Core/CanonicalWorldGateway.cs"
PACKAGER="Tools/B01/B01-23/P17W20CanonicalSaveApi001/package.py"
R5_PACKAGER="Tools/B01/B01-23/P17W15UnitySaveBridge001/package.py"
class Hold(Exception):pass
def require(ok,why):
    if not ok:raise Hold(why)
def sha(raw):return hashlib.sha256(raw).hexdigest()
def git(root,*args):
    p=subprocess.run(["git","-C",str(root),*args],capture_output=True)
    require(p.returncode==0,"GIT_CALL_FAILED:"+str(args)+":"+p.stderr.decode(errors="replace"))
    return p.stdout
def load(root,path,name):
    spec=importlib.util.spec_from_file_location(name,str(root/path))
    m=importlib.util.module_from_spec(spec)
    spec.loader.exec_module(m)
    return m
def exact_pin(roots):
    expected={"api":API_V2,"api_v1":API_V1,"api_alt":API_ALT,
        "r5":R5,"r4":R4,"game":GAME,"save":SAVE,
        "safe":SAFE,"guidfence":GUIDFENCE}
    for name,commit in expected.items():
        actual=git(roots[name],"rev-parse","HEAD").decode().strip()
        require(actual==commit,"ORIGINAL_COMMIT_PIN_FAILED:"+name)
    # ALL producer source candidates must be immutable: no merged API #682;
    # Git byte checks below are required for same GUID old API versions.
    v2=git(roots["api"],"show","HEAD:"+GATEWAY)
    v1=git(roots["api_v1"],"show","HEAD:"+GATEWAY)
    require(v1!=v2,"API_V2_EQUALS_UNCORRECTED_V1")
    require(b"ICanonicalStageOwnership" in v2 and
        b"CleanupUnpublishedAndVerifyDestroyed" in v2 and
        b"BeginOwnedStage" in v2,
        "API_V2_OWNER_TRACKING_SOURCE_NOT_PRESENT")
    require(b"ICanonicalStageOwnership" not in v1,
        "V1_ALREADY_V2_UNEXPECTED")
    return {"selected_api_v2":API_V2,"ancestor_api_v1":API_V1,
        "api_682_alt_hold":API_ALT,"host_r5_separate":R5,
        "r4_owner_frozen":R4,"gameplay_frozen":GAME,"save_frozen":SAVE,
        "v1_vs_v2_CSharp_same_path_different_bytes":True}

def original_api(roots,safe):
    module=load(roots["api"],PACKAGER,"b0140_original_api_v2_git_author")
    raw=module.build()   # actual ORIGINAL owner Git-provenance archive
    manifest=module.verify(raw)
    require(len(raw)==ZIP_SIZE and sha(raw)==ZIP_SHA256,
            "ORIGINAL_API_V2_INNER_ZIP_SHA_OR_SIZE_DIFFERENT")
    with zipfile.ZipFile(io.BytesIO(raw)) as z:
        names=z.namelist()
        require(len(names)==15 and len(set(names))==15,
                "ORIGINAL_API_V2_ZIP_WRONG_MEMBERS")
        entries={name:z.read(name) for name in names if name!="PACKAGE_MANIFEST.json"}
    require(manifest["package"]=="B01-23-CANONICAL-WORLD-API-001" and
            manifest["version"]=="2.0.0" and
            manifest["sha"]==API_V2 and
            manifest["selected_frozen_api_v1"]==API_V1 and
            manifest["parent_r4_selected"]==R4 and
            manifest["guids"]==4 and manifest["asmdefs"]==1 and
            manifest["native"]=="NOT_RUN" and
            manifest["canonical_510_private_adapter"]=="NOT_IMPLEMENTED" and
            manifest["crash_atomic_slot"]=="B01-62_PENDING" and
            manifest["codex_ack"] is False,
            "ORIGINAL_API_V2_AUTHOR_MANIFEST_GATES_INVALID")
    require(len(entries)==14 and
            set(entries)=={f["path"] for f in manifest["files"]},
            "ORIGINAL_API_V2_GIT_SOURCE_SET_MISMATCH")
    for path,rawfile in entries.items():
        source=git(roots["api"],"show","HEAD:"+path)
        require(source==rawfile and
                (roots["api"]/path).read_bytes()==source,
                "ORIGINAL_API_V2_GIT_BYTE_DRIFT:"+path)
        member=next(f for f in manifest["files"] if f["path"]==path)
        require(member["size"]==len(rawfile) and
                member["sha256"]==sha(rawfile),
                "ORIGINAL_API_V2_MANIFEST_BYTE_SHA_MISMATCH:"+path)
    assets={p:b for p,b in entries.items()
        if p==API_PREFIX+".meta" or p.startswith(API_PREFIX+"/")}
    require(len(safe.validate_guids(assets))==4,
            "API_V2_META_GUID_INVENTORY_NOT_FOUR")
    assemblies={p:json.loads(b) for p,b in assets.items() if p.endswith(".asmdef")}
    require(len(assemblies)==1 and
        next(iter(assemblies.values()))["name"]=="B0123.CanonicalSaveApi.Core",
        "API_V2_ASMDEF_OWNER_OR_COUNT_INVALID")
    require(GATEWAY in assets and
        assets[GATEWAY]==git(roots["api"],"show","HEAD:"+GATEWAY),
        "REAL_NEW_GATEWAY_CSHARP_GIT_MISSING")
    return module,raw,assets,{"api_original_zip_bytes":len(raw),
        "api_original_zip_sha256":sha(raw),"zip_members":len(names),
        "original_author_git_members":len(entries),
        "api_unity_asset_files":len(assets),"api_meta_guid_count":4,
        "asmdefs":1,"source_sha":API_V2}

def original_r5(roots,safe):
    owner=load(roots["r5"],R5_PACKAGER,"b0140_original_r5_author")
    raw=owner.build()
    members=owner.verify(raw)
    require(len(raw)==HOST_ZIP_SIZE and sha(raw)==HOST_ZIP_SHA256 and
            len(members)==27,
            "ORIGINAL_R5_SOURCE_AUTHOR_CHANGED")
    assets={p:b for p,b in members.items()
        if p==HOST_PREFIX+".meta" or p.startswith(HOST_PREFIX+"/")}
    require(len(assets)==18 and len(safe.validate_guids(assets))==11,
            "R5_ORIGINAL_HOST_ASSETS_OR_GUIDS_DRIFT")
    return assets

def adversarial_api_original_staging(module,raw,roots):
    with tempfile.TemporaryDirectory(prefix="b0140-w21-v2-author-") as td:
        root=Path(td)
        target=root/"new_disposable_author_package_only"
        module.stage(raw,target)
        require(target.is_dir(),"API_V2_ORIGINAL_AUTHOR_STAGE_NO_TARGET")
        with zipfile.ZipFile(io.BytesIO(raw)) as z:
            for name in z.namelist():
                require((target/name).is_file() and
                        (target/name).read_bytes()==z.read(name),
                        "API_V2_ORIGINAL_AUTHOR_STAGE_WRONG_BYTES:"+name)
        module.rollback(raw,target)
        require(not any(f.is_file() for f in target.rglob("*")),
                "API_V2_ORIGINAL_AUTHOR_ROLLBACK_LEFT_SOURCE_FILES")
        # Unowned directory must NEVER be adopted/replaced. No shared scratch.
        occupied=root/"occupied"
        (occupied/"Assets/FOREIGN_EMPTY").mkdir(parents=True)
        before=sorted(p.relative_to(occupied).as_posix()
             for p in occupied.rglob("*"))
        try:module.stage(raw,occupied)
        except ValueError:pass
        else:raise Hold("API_V2_ORIGINAL_AUTHOR_STAGE_ADMITTED_OCCUPIED_USER_DIR")
        require(before==sorted(p.relative_to(occupied).as_posix()
            for p in occupied.rglob("*")),
            "API_V2_ORIGINAL_AUTHOR_STAGE_CHANGED_FOREIGN_EMPTY_DIR")
        # ORIGINAL ZIP negative: a flipped member must fail original Git check.
        with zipfile.ZipFile(io.BytesIO(raw)) as z:
            content={name:z.read(name) for name in z.namelist()}
        content[GATEWAY]=b"// MALICIOUS SOURCE IN PLACE OF OWNER GATEWAY\n"
        output=io.BytesIO()
        with zipfile.ZipFile(output,"w",compression=zipfile.ZIP_STORED) as z:
            for name,data in content.items():z.writestr(name,data)
        require(sha(output.getvalue())!=ZIP_SHA256,"BROKEN_ZIP_NEGATIVE_NOT_DISTINCT")
        try:module.verify(output.getvalue())
        except (ValueError,AssertionError,zipfile.BadZipFile):pass
        else:raise Hold("API_V2_ORIGINAL_AUTHOR_VERIFIER_ACCEPTED_CHANGED_GATEWAY")
    return {"original_owner_stage":"PASS",
        "original_owner_rollback":"PASS",
        "occupied_foreign_empty_dir_refusal":"PASS",
        "original_owner_zip_corrupted_gateway_rejected":"PASS"}

def physical_combined_host(roots,safe,guid,api_assets,host_assets):
    frozen,summary=safe.source_files(roots["game"],roots["save"])
    require(len(frozen)==37 and len(safe.validate_guids(frozen))==23,
            "FROZEN_GAME_SAVE_W06_ORIGINAL_SOURCE_DRIFT")
    incoming={**frozen,**host_assets,**api_assets}
    require(len(incoming)==len(frozen)+len(host_assets)+len(api_assets),
            "OWNED_SOURCE_PATH_OVERLAP")
    source_guids=safe.validate_guids(incoming)
    require(len(source_guids)==38,"R5_AND_API_V2_UNITY_META_GUID_COLLISION")
    require(len(guid.meta_guids(incoming))==38,
            "CANONICAL_API_V2_CPP_SCRIPT_META_GUID_CLOSURE_DRIFT")
    # Same GUID, same path old V1 source cannot be installed in a V2 receipt.
    v2=api_assets[GATEWAY]
    v1=git(roots["api_v1"],"show","HEAD:"+GATEWAY)
    require(v1!=v2,"API_V1_GATEWAY_SAME_AS_V2")
    bad=dict(incoming);bad[GATEWAY]=v1
    require(safe.validate_guids(bad)==source_guids,
            "V1_BYTESWAP_SHOULD_BE_SAME_GUIDS")
    with tempfile.TemporaryDirectory(prefix="b0140-w21-v2-unified-") as td:
        root=Path(td)
        host=root/"foreign_real_unity_scratch"
        (host/"Assets/USER_PLACE").mkdir(parents=True)
        fixtures={
            "ProjectSettings/ProjectVersion.txt":
                git(roots["game"],"show","HEAD:ProjectSettings/ProjectVersion.txt"),
            "Packages/manifest.json":
                git(roots["game"],"show","HEAD:Packages/manifest.json"),
            "Assets/USER_PLACE/external_scene.unity":
                b"%YAML 1.1\n--- !u!1 &12\nGameObject:\n  m_Name: OwnedByOtherUser\n",
        }
        for path,data in fixtures.items():
            p=host/path;p.parent.mkdir(parents=True,exist_ok=True);p.write_bytes(data)
        before=safe.crawl(host)
        # A plain path/GUID checker is NOT enough to detect V1 substitution.
        safe.preflight_host(host,bad)
        require(guid.validate_script_refs(
            {**bad,**before[0]},guid.meta_guids({**bad,**before[0]}))=={},
            "OLD_SOURCE_GUID_VALIDATOR_REJECTED_UNEXPECTED_V1_FIXTURE")
        require(bad[GATEWAY]==v1 and bad[GATEWAY]!=git(
                roots["api"],"show","HEAD:"+GATEWAY),
                "ALTERNATE_API_V1_BYTESWAP_ACCEPTED_AS_EXACT_V2")
        require(all(b==git(roots["api"],"show","HEAD:"+p)
                    for p,b in bad.items() if p in api_assets and p!=GATEWAY),
                "API_V2_FOREIGN_SECONDARY_ORIGINAL_BYTES")
        require(safe.crawl(host)==before,"V1_NEGATIVE_CHANGED_FOREIGN_HOST")
        # Genuine original v2 + original selected R5 + frozen gameplay/Save,
        # strict new wholly disposable Unity source candidate and rollback.
        safe.preflight_host(host,incoming)
        require(guid.validate_script_refs({**incoming,**before[0]},
                guid.meta_guids({**incoming,**before[0]}))=={},
                "API_V2_VALID_YAML_SCAN_UNEXPECTED_REJECTION")
        receipt={"only_game_sha":GAME,"only_save_sha":SAVE,
            "original_r5_sha":R5,"original_canonical_api_v2_sha":API_V2,
            "native_unity":"NOT_RUN","codex_ack":False}
        dest=root/"new_owned_candidate_exclusive"
        exact_files,dirs=safe.create_candidate(host,dest,incoming,receipt,
            roots["game"],roots["save"],roots["r5"],roots["api"])
        require(dest.is_dir() and safe.crawl(dest)==(exact_files,dirs),
            "COMBINED_R5_API_V2_PHYSICAL_PUBLISH_WRONG_BYTES")
        # Adding alien user empty folder after source candidate is published
        # is a rollback denial, not an excuse to delete other people's data.
        foreign=dest/"Assets/NEW_FOREIGN_EMPTY"
        foreign.mkdir()
        try:safe.rollback_candidate(dest,exact_files,dirs)
        except Exception as e:
            require(e.__class__.__name__=="GateHold",
                    "ROLLBACK_DENIED_FOR_WRONG_CAUSE")
        else:raise Hold("FOREIGN_EMPTY_FOLDER_DELETED_BY_UNSAFE_ROLLBACK")
        require(foreign.is_dir(),"FOREIGN_EMPTY_FOLDER_DELETED")
        foreign.rmdir()
        safe.rollback_candidate(dest,exact_files,dirs)
        require(not dest.exists() and safe.crawl(host)==before,
                "R5_API_V2_STRICT_ROLLBACK_CHANGED_ORIGINAL_HOST")
        # Alien wrong physical Unity script GUID even without class string
        # fails closed before any staging.
        wrong_path="Assets/USER_PLACE/rogue.prefab"
        alien_guid="a9b8c7d6e5f40123456789abcdef0123"
        fake=(f"%YAML 1.1\n--- !u!114 &3\nMonoBehaviour:\n  m_Script: {{fileID: 11500000, guid: {alien_guid}, type: 3}}\n").encode()
        (host/wrong_path).write_bytes(fake)
        contaminated=safe.crawl(host)
        try:guid.validate_script_refs({**incoming,**contaminated[0]},
                guid.meta_guids({**incoming,**contaminated[0]}))
        except Exception as e:
            require(e.__class__.__name__=="Reject","ALIEN_GUID_REJECT_WRONG_CAUSE")
        else:raise Hold("ALIEN_SCRIPT_GUID_WAS_ALLOWED")
        require(safe.crawl(host)==contaminated,"ALIEN_GUID_PROBE_MODIFIED_HOST")
    return {"actual_source_paths":len(incoming),
        "original_R5_unique_guids":11,"original_API_V2_unique_guids":4,
        "frozen_unique_guids":23,"combined_unique_guids":len(source_guids),
        "strict_disposable_unity_source_stage_and_rollback":"PASS",
        "foreign_empty_dir_preserved_on_denied_rollback":"PASS",
        "same_guid_old_api_v1_csharp_source_swap_detected":"PASS",
        "alien_yaml_guid_only_script_denied":"PASS",
        "canonical_510_private_rig_integration":"NOT_IMPLEMENTED"}

def main():
    p=argparse.ArgumentParser()
    for name in ("api","api_v1","api_alt","r5","r4","game","save",
                 "safe","guidfence","output"):
        p.add_argument("--"+name,required=True,type=Path)
    args=p.parse_args()
    roots={n:getattr(args,n).resolve() for n in
           ("api","api_v1","api_alt","r5","r4","game","save","safe","guidfence")}
    source=exact_pin(roots)
    safe=load(roots["safe"],
        "Tools/B01/B01-40/P17W15AtomicReceiverMigration/migration.py",
        "b0140_w21_immutable_old_safe")
    guid=load(roots["guidfence"],
        "Tools/B01/B01-40/P17W18SerializedScriptGuidFence/fence.py",
        "b0140_w21_immutable_guid")
    original,raw,api_assets,api_receipt=original_api(roots,safe)
    stage=adversarial_api_original_staging(original,raw,roots)
    r5assets=original_r5(roots,safe)
    combined=physical_combined_host(roots,safe,guid,api_assets,r5assets)
    report={"package":"B01-40-P17W21-CANONICAL-API-691-V2-PHYSICAL-G0",
      "owner_selection":source,"original_author_zip":api_receipt,
      "original_author_stage_negative":stage,
      "physical_R5_plus_API_V2_scratch":combined,
      "physical_source_G0":"PASS_SOURCE_ONLY",
      "formal_canonical_SAVELOAD_G0":"HOLD_INDEPENDENT_SECURITY_TRUST_PRIVATE_RIG",
      "native_unity_G1":"NOT_RUN","gameplay_g2":"HOLD",
      "codex_ack":False,"no_merge":True,
      "six_stages":{"IMPLEMENTED":True,"TESTED":"SOURCE_PHYSICAL_ONLY",
          "QA_ACCEPTED":False,"READY_FOR_INTEGRATOR":False,
          "INTEGRATED":False,"RELEASED":False}}
    args.output.mkdir(parents=True,exist_ok=True)
    (args.output/"canonical-api-v2-691-g0-source-only.json").write_text(
        json.dumps(report,sort_keys=True,indent=2)+"\n")
    print("B0140_W21_API_691_ORIGINAL_ZIP",json.dumps(api_receipt,sort_keys=True))
    print("B0140_W21_API_691_AUTHOR_ORIGINAL_STAGE",json.dumps(stage,sort_keys=True))
    print("B0140_W21_API_691_R5_690_COMBINED_SAFE",json.dumps(combined,sort_keys=True))
    print("B0140_W21_API_V2_PHYSICAL_G0_PASS_CANONICAL_GAME_SAVE_HOLD_NATIVE_NOT_RUN_NO_MERGE")
if __name__=="__main__":
    try:main()
    except (Hold,AssertionError,KeyError,ValueError,OSError,
            subprocess.SubprocessError,zipfile.BadZipFile) as e:
        print("B0140_W21_API_V2_FAILURE",str(e));raise SystemExit(1)
