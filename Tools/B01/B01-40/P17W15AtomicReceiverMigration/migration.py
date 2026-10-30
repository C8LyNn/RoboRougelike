#!/usr/bin/env python3
"""B01-40/W15 physical admission of one frozen Save #556 + frozen Secure #510.

Produces ONLY a fresh disposable Unity host clone. No live workspace changes,
no Gameplay/Save producer edits, no Canonical Codex claim. Real original source
ZIPs and original W06 assets are transported unchanged. Native Unity NOT_RUN.
"""
from __future__ import annotations
import argparse
import hashlib
import importlib.util
import json
import os
from pathlib import Path, PurePosixPath
import re
import shutil
import stat
import subprocess
import tempfile

SHA_GAME="d94400d95d13db7f27e1533df05c9292d5380431"
SHA_SAVE="33094f154c7b91119b36d58aab285e6e83c5de57"
SHA_GAME_ZIP="d4ddfa3528ef0416aefffca72d884000f3b0c9959fb8dff53096487ef95c9387"
SHA_SAVE_ZIP="20c3c17c252f084014e9766010ab4651770a41f73ce534fc2aa7fd2edc697060"
BASENAME=".b0140-w15-disposable-owned.json"
ASSETS=("Assets/B0123SecureReattachV2", "Assets/B0123DetachedSaveContract001",
        "Assets/B0123DebrisRepulse001")
DISALLOWED=("Assets/B0123DebrisPlayground001", "Assets/B0123DockHardening001",
"Assets/B0123ReattachClearance001", "Assets/B0123SecureReattachV2R11Alternative",
"Assets/B0123PublicGuardAlternate", "Assets/B0123Legacy")
LEGACY=("DebrisRigPlayground.Reattach", "B0123DebrisPlayground001",
"SecureReattachV2R11Alternative", "B0123PublicGuardAlternate")
GUID_RE=re.compile(r"^guid:\s*([0-9a-f]{32})\s*$",re.MULTILINE)
class GateHold(Exception): pass
def require(ok,msg):
    if not ok: raise GateHold(msg)
def sha(b):return hashlib.sha256(b).hexdigest()
def git(root,*args):
    p=subprocess.run(["git","-C",str(root),*args],capture_output=True)
    require(p.returncode==0,"GIT_DENIED "+str(args)+": "+p.stderr.decode(errors="replace"))
    return p.stdout
def author_module(path,name):
    spec=importlib.util.spec_from_file_location(name,str(path))
    m=importlib.util.module_from_spec(spec)
    spec.loader.exec_module(m)
    return m
def source_files(game,save):
    require(git(game,"rev-parse","HEAD").decode().strip()==SHA_GAME,"FROZEN_510_SHA_DRIFT")
    require(git(save,"rev-parse","HEAD").decode().strip()==SHA_SAVE,"FROZEN_556_SHA_DRIFT")
    a=author_module(game/"Tools/B01/B01-23/P17W10SecureReattachV2/package.py","original_selected510")
    b=author_module(save/"Tools/B01/B01-23/P17W13DetachedSaveContract001/package.py","original_selected556")
    ar=a.build();br=b.build()
    require(sha(ar)==SHA_GAME_ZIP and len(ar)==99666,"ORIGINAL_510_ZIP_DRIFT")
    require(sha(br)==SHA_SAVE_ZIP and len(br)==67581,"ORIGINAL_556_ZIP_DRIFT")
    ga=a.verify(ar);sa=b.verify(br)
    require(len(ga)==35 and len(sa)==17,"FROZEN_ZIP_MEMBERS_DRIFT")
    mg=json.loads(ga["PACKAGE_MANIFEST.json"])
    ms=json.loads(sa["PACKAGE_MANIFEST.json"])
    require(mg["source_sha"]==SHA_GAME and mg["version"]=="2.0.0" and
        mg["codex_ack"] is False and mg["native_unity"]=="NOT_RUN","GAME_ORIGINAL_MANIFEST")
    require(ms["own_head_sha"]==SHA_SAVE and ms["version"]=="2.0.0" and
        ms["selected_secure_v2_sha"]==SHA_GAME and
        ms["savegame_host"]=="NOT_INTEGRATED" and ms["codex_ack"] is False,
        "SAVE_ORIGINAL_MANIFEST")
    choice=json.loads(git(save,"show","HEAD:Tools/B01/B01-23/P17W14DetachedSaveContractR2/contract_selection.json"))
    require(choice["selected_save_lineage"]=="#549" and
        choice["other_save_contract_544"]=="ALT_HOLD" and
        choice["frozen_gameplay_pr_510"]==SHA_GAME and
        choice["version"]=="2.0.0","WRONG_SELECTED_SAVE")
    output={}
    for d,owner in ((ga,SHA_GAME),(sa,SHA_SAVE)):
        for name,raw in d.items():
            if not name.startswith("Assets/"):continue
            require(name==owner_prefix(name)+".meta" or name.startswith(owner_prefix(name)+"/"),
                    "OWNER_SCOPE_ESCAPE")
            require(name not in output,"SOURCE_FILE_COLLISION:"+name)
            require(git(game if owner==SHA_GAME else save,"show","HEAD:"+name)==raw,
                    "OWNER_GIT_BYTES_DRIFT:"+name)
            output[name]=raw
    # Original W06 runtime is an actual original dependency of #510, excluded
    # from the original game ZIP because it is a separate ancestor package.
    w06=git(game,"ls-tree","-r","--name-only","HEAD","Assets/B0123DebrisRepulse001/").decode().splitlines()
    w06 += ["Assets/B0123DebrisRepulse001.meta"]
    require(len(w06)>=4,"ORIGINAL_W06_ASSETS_MISSING")
    for name in w06:
        require(name not in output,"W06_OVERLAP:"+name)
        output[name]=git(game,"show","HEAD:"+name)
    require(all(n.startswith(ASSETS) for n in output),"SOURCE_SET_CONTAINS_UNKNOWN_PATH")
    validate_guids(output)
    asm={n:json.loads(raw) for n,raw in output.items() if n.endswith(".asmdef")}
    require(len(asm)>=4,"MISSING_W06_OR_SELECTED_ASMDEFS")
    runtime=json.loads(output["Assets/B0123DetachedSaveContract001/Runtime/B0123.DetachedSaveContract.Runtime.asmdef"])
    require(runtime["name"]=="B0123.DetachedSaveContract.Runtime" and
            runtime["references"]==["B0123.DebrisRepulse.Runtime"],
            "SAVE_DEPENDENCY_GRAPH")
    check=output["Assets/B0123SecureReattachV2/Runtime/SecureDebrisPlaygroundV2.cs"].decode()
    require("private" in check and "AtomicDockLedger" in check and
            "TryReattach" in check, "FROZEN_PRIVATE_LEDGER_BOUNDARY_UNEXPECTED")
    return output,{"game_zip_sha256":sha(ar),"save_zip_sha256":sha(br),
        "game_zip_bytes":len(ar),"save_zip_bytes":len(br),
        "game_zip_members":len(ga),"save_zip_members":len(sa),
        "file_count":len(output),"unity_meta_guids":len(validate_guids(output)),
        "only_save_sha":SHA_SAVE,"only_game_sha":SHA_GAME}

def owner_prefix(p):
    for prefix in ASSETS:
        if p==prefix+".meta" or p.startswith(prefix+"/"):return prefix
    raise GateHold("FORBIDDEN_OWNER_SOURCE:"+p)
def valid_name(name):
    p=PurePosixPath(name)
    require(name and not name.startswith("/") and "\\" not in name and
            str(p)==name and ".." not in p.parts and not name.endswith("/"),
            "PATH_TRAVERSAL_OR_BAD_NAME:"+name)
def dirs_for(names):
    dirs=set()
    for n in names:
        valid_name(n)
        parent=PurePosixPath(n).parent
        while str(parent)!=".":
            dirs.add(str(parent))
            parent=parent.parent
    return dirs
def validate_guids(files):
    found={}
    for name,raw in files.items():
        valid_name(name)
        if name.endswith(".meta"):
            matches=GUID_RE.findall(raw.decode())
            require(len(matches)==1,"INVALID_META_GUID:"+name)
            guid=matches[0]
            require(guid not in found,"DUPLICATE_UNITY_META_GUID:"+name+":"+found.get(guid,""))
            found[guid]=name
    return found
def ensure_external(path,*repos):
    path=Path(os.path.abspath(path))
    for repo in repos:
        repo=Path(os.path.abspath(repo))
        require(path!=repo and repo not in path.parents,"REFUSE_WRITE_TO_SOURCE_REPO")
    return path
def crawl(root):
    # Does not follow symlinks, including junctions via os.path.isjunction on
    # Windows Python. Reject all non-regular inodes. Check each path component.
    root=Path(root)
    require(root.is_dir() and not root.is_symlink(),"INVALID_HOST_ROOT")
    fs={}
    folders=set()
    for path in root.rglob("*"):
        r=path.relative_to(root).as_posix()
        valid_name(r)
        mode=path.lstat().st_mode
        if hasattr(os.path,"isjunction"):
            require(not os.path.isjunction(path),"JUNCTION_DENIED:"+r)
        require(not stat.S_ISLNK(mode),"SYMLINK_DENIED:"+r)
        if stat.S_ISDIR(mode):folders.add(r)
        elif stat.S_ISREG(mode):fs[r]=path.read_bytes()
        else:raise GateHold("SPECIAL_INODE_DENIED:"+r)
    require(folders==dirs_for(fs.keys()) or folders.issuperset(dirs_for(fs.keys())),
            "MISSING_PARENT_DIR")
    return fs,folders
def preflight_host(host,incoming):
    fs,folders=crawl(host)
    source_guid=validate_guids(incoming)
    host_guids=validate_guids(fs)
    require(not(set(source_guid)&set(host_guids)),"FOREIGN_UNITY_GUID_COLLISION")
    lower=set()
    for name,raw in fs.items():
        folded=name.casefold()
        require(folded not in lower,"WINDOWS_CASEFOLD_FILE_COLLISION")
        lower.add(folded)
        require(name not in incoming and name.casefold() not in
                {x.casefold() for x in incoming},"WOULD_OVERWRITE_EXISTING_HOST_ASSET")
        require(not any(name==x or name.startswith(x+"/") for x in DISALLOWED),
                "LEGACY_OR_ALTERNATIVE_PATH_DENIED:"+name)
        if name.endswith((".unity",".prefab",".asset",".cs",".asmdef")):
            t=raw.decode("utf-8",errors="replace")
            require(not any(s in t for s in LEGACY),
                    "LEGACY_SCENE_OR_CALLER_SERIALIZATION:"+name)
    # Even an EMPTY folder that claims an owner source path is a pre-existing
    # unowned directory: no implicit adoption.
    for n in folders:
        require(n not in dirs_for(incoming) or n in ("Assets",),
                "PREEXISTING_OWNER_DIRECTORY:"+n)
    require(fs.get("ProjectSettings/ProjectVersion.txt",b"").decode().find("6000.3.14f1")>=0,
            "RECEIVER_UNITY_VERSION_MISMATCH")
    require("Packages/manifest.json" in fs,"NO_REAL_PACKAGE_MANIFEST")
    manifest=json.loads(fs["Packages/manifest.json"])
    require(manifest["dependencies"].get("com.unity.test-framework")=="1.6.0",
            "RECEIVER_TEST_FRAMEWORK_DRIFT")
    return fs,folders

def receipt_for(files,dirs,summary):
    return (json.dumps({"owner":"B01-40-P17W15-NEW-DISPOSABLE-HOST-CLONE",
        "source":summary,"files":{n:sha(v) for n,v in sorted(files.items())},
        "directories":sorted(dirs),
        "live_codex_receiver":"NOT_TOUCHED",
        "original_510_556_immutable":True,
        "native_unity":"NOT_RUN",
        "game_save_load":"NOT_INTEGRATED",
        "ack":False},sort_keys=True,separators=(",",":"))+"\n").encode()

def create_candidate(host,destination,incoming,summary,*repo_paths,inject_failure=False):
    host=ensure_external(host,*repo_paths)
    destination=ensure_external(destination,*repo_paths)
    require(not os.path.lexists(destination),"DESTINATION_ALREADY_EXISTS")
    require(not host.is_relative_to(destination) and not destination.is_relative_to(host),
            "OVERLAPPING_INPUT_OUTPUT")
    fs,folders=preflight_host(host,incoming)
    require(BASENAME not in fs,"UNOWNED_MARKER_IN_HOST")
    destination.parent.mkdir(parents=True,exist_ok=True)
    with tempfile.TemporaryDirectory(prefix=".b0140-w15-prepare-",dir=destination.parent) as td:
        temp=Path(td)/"clone"
        shutil.copytree(host,temp,symlinks=False)
        expected=dict(fs)
        for name,raw in incoming.items():
            p=temp/name;p.parent.mkdir(parents=True,exist_ok=True)
            # Preflight prevents an overwrite or adoption; repeated check before write.
            require(not p.exists() and not p.is_symlink(),"STAGE_WOULD_OVERWRITE")
            p.write_bytes(raw)
            expected[name]=raw
        require(not inject_failure,"INJECTED_PRE_COMMIT_FAILURE")
        # Verify original+new entries and complete folder topology before publish.
        actual,dirset=crawl(temp)
        require(expected==actual,"POST_STAGE_SOURCE_OR_HOST_MUTATED")
        expected[BASENAME]=receipt_for(actual,dirset,summary)
        (temp/BASENAME).write_bytes(expected[BASENAME])
        actual,dirset=crawl(temp)
        require(actual==expected,"BAD_PREPARE_RECEIPT")
        # Never mutate host. Publish one complete new candidate folder atomically
        # if dest absent (caller holds exclusive ownership of disposable output).
        require(not os.path.lexists(destination),"RACE_DEST_EXISTS")
        os.replace(temp,destination)
    after,dirs=crawl(destination)
    require(after==expected,"POST_COMMIT_CONTENT_CHANGED")
    require(dirs==dirset,"POST_COMMIT_DIRECTORY_CHANGED")
    # Caller holds expected immutable bytes; receipt never self-authorizes deletion.
    return expected,dirs

def rollback_candidate(destination,expected,expected_dirs):
    require(expected.get(BASENAME) and destination.is_dir() and
            not destination.is_symlink(),"UNOWNED_OR_MISSING_CANDIDATE")
    found,dirs=crawl(destination)
    require(found==expected,"CHANGED_FOREIGN_OR_MISSING_FILE_REFUSE_ROLLBACK")
    require(dirs==expected_dirs,"CHANGED_FOREIGN_OR_MISSING_EMPTY_DIRECTORY_REFUSE_ROLLBACK")
    receipt=json.loads(found[BASENAME])
    require(receipt["owner"]=="B01-40-P17W15-NEW-DISPOSABLE-HOST-CLONE" and
            receipt["source"]["only_game_sha"]==SHA_GAME and
            receipt["source"]["only_save_sha"]==SHA_SAVE and
            receipt["ack"] is False and receipt["native_unity"]=="NOT_RUN",
            "FORGED_RECEIPT")
    # Exact-owned files only; do not call original destructive #510/#532 rollback.
    for name in sorted(expected,reverse=True):
        p=destination/name
        require(p.is_file() and not p.is_symlink() and p.read_bytes()==expected[name],
                "LATE_FILE_MUTATION_DENY")
        p.unlink()
    for name in sorted(expected_dirs,key=lambda x:(x.count("/"),len(x)),reverse=True):
        (destination/name).rmdir()
    destination.rmdir()
    require(not destination.exists(),"CLEAN_ROLLBACK_FAILED")

def negative(fn, label,exc=(GateHold,)):
    try:fn()
    except exc:return label
    raise AssertionError("NEGATIVE_FALSE_ACCEPTED:"+label)

def self_test(game,save,work,output):
    incoming,src=source_files(game,save)
    checks=[]
    tests={}
    with tempfile.TemporaryDirectory(prefix="b01-40-w15-scratch-") as td:
        scratch=Path(td)
        host=scratch/"host";host.mkdir()
        fixed={
          "ProjectSettings/ProjectVersion.txt":git(game,"show","HEAD:ProjectSettings/ProjectVersion.txt"),
          "Packages/manifest.json":git(game,"show","HEAD:Packages/manifest.json"),
          "Assets/HostOnly/health.cs":b"namespace HostOnly { public class Health {} }\n",
          "Assets/HostOnly/health.cs.meta":b"fileFormatVersion: 2\nguid: fedcba0987654321fedcba0987654321\n",
          "Assets/HostOnly/fixture.unity":b"%YAML 1.1\n--- !u!1 &123\nGameObject:\n  m_Name: HostOnly\n"}
        for name,b in fixed.items():
            p=host/name;p.parent.mkdir(parents=True,exist_ok=True);p.write_bytes(b)
        def denied(mut,tag):
            h=scratch/("negative_"+tag);shutil.copytree(host,h)
            mut(h)
            expected_files,expected_dirs=crawl(h)
            checks.append(negative(lambda:preflight_host(h,incoming),tag))
            require(crawl(h)==(expected_files,expected_dirs),"NEGATIVE_PREFLIGHT_MUTATED_HOST")
        denied(lambda h:(h/"Assets/B0123DebrisPlayground001").mkdir(), "foreign_empty_legacy_dir")
        denied(lambda h:(h/"Assets/HostOnly/fixture.unity").write_text(
             "%YAML 1.1\nMonoBehaviour:\n  m_Name: DebrisRigPlayground.Reattach\n"),"legacy_serialized_scene")
        denied(lambda h:(h/"Assets/HostOnly/health.cs.meta").write_text(
             "fileFormatVersion: 2\nguid: "+next(iter(validate_guids(incoming)))+"\n"),"unity_guid_collision")
        denied(lambda h:(h/"Assets/HostOnly/health.cs").write_text(
             "class B0123PublicGuardAlternate {}"),"alternative_script")
        denied(lambda h:(h/"Assets/B0123SecureReattachV2/Runtime").mkdir(parents=True),
             "foreign_empty_owner_directory")
        denied(lambda h:(h/"ProjectSettings/ProjectVersion.txt").write_text(
             "m_EditorVersion: 2022.3.0f1\n"),"wrong_unity_version")
        denied(lambda h:(h/"Packages/manifest.json").write_text(
             '{"dependencies":{"com.unity.test-framework":"1.4.0"}}'),"wrong_test_framework")
        denied(lambda h:(h/"Assets/HostOnly/health.cs.meta").write_text(
             "fileFormatVersion: 2\nguid: duplicate\n"),"corrupt_host_meta")
        checks.append(negative(lambda:preflight_host(host,{
             **incoming,"Assets/B0123SecureReattachV2/Runtime/SecureDebrisPlaygroundV2.cs":b"overwritten"}),
             "conflicting_frozen_archive_bytes"))
        duplicated=dict(incoming); one=next(k for k in incoming if k.endswith(".meta"))
        duplicated[one]=b"fileFormatVersion: 2\nguid: 00000000000000000000000000000000\n"
        other=next(k for k in duplicated if k.endswith(".meta") and k!=one)
        duplicated[other]=duplicated[one]
        checks.append(negative(lambda:preflight_host(host,duplicated),"duplicate_incoming_guid"))
        denied(lambda h:(h/"Assets/B0123SecureReattachV2R11Alternative").mkdir(),
            "alternative_empty_directory")
        # Symlink only if permitted by host (Windows privilege may vary).
        symlink_state="NOT_RUN_NO_PERMISSION"
        victim=scratch/"untouched.txt";victim.write_text("KEEP")
        link=host/"Assets"/"linked"
        try:link.symlink_to(victim)
        except (OSError,NotImplementedError):pass
        else:
            checks.append(negative(lambda:preflight_host(host,incoming),"source_host_symlink"))
            require(victim.read_text()=="KEEP","SYMLINK_TARGET_DAMAGED")
            link.unlink();symlink_state="PASS"
        # Failed prepare must not publish anything / modify source.
        candidate=scratch/"candidate_crash"
        checks.append(negative(lambda:create_candidate(host,candidate,incoming,src,game,save,
                          inject_failure=True),"injected_prepare_crash"))
        require(not candidate.exists(),"PARTIAL_PREPARE_PUBLISHED")
        checks.append(negative(lambda:create_candidate(host,host,incoming,src,game,save),
                               "host_target_overlap"))
        for n in range(2):
            target=scratch/("candidate"+str(n))
            expected,folders=create_candidate(host,target,incoming,src,game,save)
            require(crawl(host)[0]==fixed,"CANONICAL_INPUT_HOST_CHANGED")
            check,dirs=crawl(target)
            require(all(check[x]==incoming[x] for x in incoming),"PHYSICAL_SELECTED_SOURCE_BYTES_CHANGED")
            require(check["Assets/HostOnly/health.cs"]==fixed["Assets/HostOnly/health.cs"],
                   "FOREIGN_HOST_ASSET_DROPPED")
            tests["safe_publish_roundtrip_"+str(n)]="PASS"
            if n==0:
                checks.append(negative(lambda:create_candidate(host,target,incoming,src,game,save),
                                       "refuse_existing_target"))
                foreign=target/"Assets/HostOnly/USER_EMPTY"/"deep";foreign.mkdir(parents=True)
                checks.append(negative(lambda:rollback_candidate(target,expected,folders),
                                       "preserve_foreign_nested_empty_folder"))
                require(foreign.is_dir(),"FOREIGN_EMPTY_DIR_DELETED")
                foreign.rmdir();foreign.parent.rmdir()
                original=target/"Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs"
                data=original.read_bytes();original.write_bytes(b"alien bytes")
                checks.append(negative(lambda:rollback_candidate(target,expected,folders),
                                       "preserve_tampered_selected_save"))
                require(original.read_bytes()==b"alien bytes","CORRUPTED_OWN_SOURCE_ERASED")
                original.write_bytes(data)
                foreign_file=target/"USER-NEW.txt";foreign_file.write_text("stay")
                checks.append(negative(lambda:rollback_candidate(target,expected,folders),
                                       "preserve_foreign_user_file"))
                require(foreign_file.read_text()=="stay","FOREIGN_USER_FILE_ERASED")
                foreign_file.unlink()
                marker=target/BASENAME;original_marker=marker.read_bytes()
                marker.write_bytes(b'{"fake":true}')
                checks.append(negative(lambda:rollback_candidate(target,expected,folders),
                                       "reject_forged_ownership_receipt"))
                require(target.is_dir(),"FORGED_MARKER_PARTIAL_ROLLBACK")
                marker.write_bytes(original_marker)
                missing=target/"Assets/B0123SecureReattachV2/Runtime/AtomicDockLedger.cs"
                lost=missing.read_bytes();missing.unlink()
                checks.append(negative(lambda:rollback_candidate(target,expected,folders),
                                       "refuse_missing_original_selected_asset"))
                missing.write_bytes(lost)
            rollback_candidate(target,expected,folders)
            require(not target.exists(),"FAILED_CLEAN_DISPOSABLE_ROLLBACK")
        require(crawl(host)[0]==fixed,"SOURCE_HOST_MODIFIED_AFTER_ROLLBACK")
    report={"package":"B01-40-P17W15-DISPOSABLE-HOST-MIGRATION-001",
      "source":src,"source_assets":len(incoming),
      "original_source_sha":SHA_GAME,"original_save_sha":SHA_SAVE,
      "source_only":True,"native_unity":"NOT_RUN","canonical_codex_scanned":False,
      "live_save_restore":"HOLD_NO_510_PRIVATE_LEDGER_CAPTURE_REHYDRATE",
      "rollback":"PASS_DISPOSABLE_CLEAN_FILES_ONLY",
      "atomic_host":"NEW_CLONE_PUBLISH_ONLY_NOT_LIVE_PROJECT_REPLACE",
      "negative_checks":checks,"negative_count":len(checks),
      "two_positive_roundtrips":tests,
      "symlink":symlink_state,"g2":"HOLD","codex_ack":False,"merge":False}
    output.mkdir(parents=True,exist_ok=True)
    (output/"source-only-receipt.json").write_text(json.dumps(report,sort_keys=True,indent=2)+"\n")
    print("B0140_W15_SOURCE_SELECTED",json.dumps(src,sort_keys=True))
    print("B0140_W15_NEGATIVE_PASS",len(checks),"SYMLINK",symlink_state)
    print("B0140_W15_ATOMIC_DISPOSABLE_CLONE_PASS",len(tests))
    print("B0140_W15_G0_DISPOSABLE_MIGRATION_SOURCE_PASS G1_NATIVE_NOT_RUN LIVE_SAVELOAD_HOLD G2_HOLD CODEX_ACK_NONE")
    return report

def main():
    ap=argparse.ArgumentParser()
    ap.add_argument("--game",required=True,type=Path)
    ap.add_argument("--save",required=True,type=Path)
    ap.add_argument("--out",required=True,type=Path)
    a=ap.parse_args()
    self_test(a.game.resolve(),a.save.resolve(),Path(__file__).resolve(),a.out.resolve())
if __name__=="__main__":
    try:main()
    except (GateHold,ValueError,OSError,AssertionError,subprocess.SubprocessError) as exc:
        print("B0140_W15_GATE_FAILURE",str(exc))
        raise SystemExit(1)
