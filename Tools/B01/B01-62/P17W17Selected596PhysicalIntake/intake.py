#!/usr/bin/env python3
"""B01-62 P17/W17: independent ORIGINAL Git #596 physical #510+#556 coexistence/migration.
No canonical receiver and no producer source writes. #604 ALTERNATIVE_HOLD.
"""
import argparse, hashlib, importlib.util, io, json, os, pathlib, shutil, subprocess, tempfile, zipfile

PRODUCER="efb2b06eafedab5469b57f163152d96aca3c5915"
FROZEN_GAME="d94400d95d13db7f27e1533df05c9292d5380431"
FROZEN_SAVE="33094f154c7b91119b36d58aab285e6e83c5de57"
ALT="1aa8a59ce1bb4fcd998f0b2b1ce25dfaea333f0a"
EXPECTED_SHA256="005ac910ca9476203bc3ff1748d9198d45835abad21d97e12ad6c1bfe50484e9"
EXPECTED_BYTES=81190
EXPECTED_MEMBERS=23
PACKAGER="Tools/B01/B01-23/P17W15UnitySaveBridge001/package.py"
PACKAGER_BLOB="f0fb2f5764c83ed011c309c5a48e4a67ad9b6fa7"
CORE="Assets/B0123UnitySaveBridge001/Core/AtomicWorldSwap.cs"
CORE_BLOB="1f9e883c38101f214e4335867faa731f7ae67689"
UNITY="Assets/B0123UnitySaveBridge001/Unity/IsolatedTwinRigSaveHost.cs"
UNITY_BLOB="4bac6f327c9b93ae98dd2061c0ca8ea2100aab42"
FROZEN_PINS=[
 (FROZEN_GAME,"Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs","fa1aa5e531927cbe73cf5fdfe161b4a981316db5"),
 (FROZEN_GAME,"Assets/B0123SecureReattachV2/Runtime/SecureRigSpec.cs","b680ed0ec969ac23203fa55eab7b90f8daa86e43"),
 (FROZEN_GAME,"Assets/B0123SecureReattachV2/Runtime/SecureDebrisPlaygroundV2.cs","328977b4cdcdcd7c9f128c01f11ff67f8e46086a"),
 (FROZEN_GAME,"Assets/B0123SecureReattachV2/Runtime/AtomicDockLedger.cs","b6a4d04b0c6407c8c84451b4d599d96fe0e4d1a9"),
 (FROZEN_SAVE,"Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs","b52e511c370f5fca56c74029050844dc8cbb7f9a")
]
def git(repo,*args):
    return subprocess.check_output(["git","-C",str(repo),*args],stderr=subprocess.PIPE).strip()
def orig(repo,sha,path):
    return subprocess.check_output(["git","-C",str(repo),"show",sha+":"+path],stderr=subprocess.PIPE)
def blob(raw):
    return hashlib.sha1(b"blob "+str(len(raw)).encode()+b"\0"+raw).hexdigest()
def sha(raw):return hashlib.sha256(raw).hexdigest()
def need(ok,msg):
    if not ok:raise AssertionError("SOURCE_QA_FAIL:"+msg)
def denied(call,msg):
    try:call()
    except (ValueError, OSError, RuntimeError, AssertionError):
        return
    raise AssertionError("MUST_REFUSE:"+msg)
def parent_meta(repo,ref,prefix):
    files=git(repo,"ls-tree","-r","--name-only",ref,prefix).decode().splitlines()
    roots=[prefix+".meta"]+files
    refs={}
    for path in roots:
        if not path.endswith(".meta"):continue
        data=orig(repo,ref,path).decode()
        tags=[ln.split(":",1)[1].strip() for ln in data.splitlines() if ln.startswith("guid:")]
        need(len(tags)==1 and len(tags[0])==32 and all(c in "0123456789abcdef" for c in tags[0]),
             "invalid_original_guid:"+path)
        need(tags[0] not in refs,"duplicate_original_guid:"+path)
        refs[tags[0]]=path
    return refs

def execute(repo,output):
    repo=pathlib.Path(repo).resolve()
    output=pathlib.Path(output).resolve()
    need(output.parent.is_dir() and not output.exists(),"output_must_be_new")
    need(git(repo,"rev-parse",PRODUCER).decode()==PRODUCER,"missing_selected_original_commit")
    check_paths={PACKAGER:PACKAGER_BLOB,CORE:CORE_BLOB,UNITY:UNITY_BLOB}
    for path,expected in check_paths.items():
        raw=orig(repo,PRODUCER,path)
        need(git(repo,"rev-parse",PRODUCER+":"+path).decode()==expected and blob(raw)==expected,
             "selected_producer_original_git_blob_drift:"+path)
    for ref,p,expected in FROZEN_PINS:
        raw=orig(repo,ref,p)
        need(git(repo,"rev-parse",ref+":"+p).decode()==expected and blob(raw)==expected,
             "frozen_source_drift:"+p)
    checks=9
    with tempfile.TemporaryDirectory(prefix="b01-62-w17-") as directory:
        scratch=pathlib.Path(directory)
        checkout=scratch/"selected_original_git_worktree"
        subprocess.run(["git","-C",str(repo),"worktree","add","--detach",str(checkout),PRODUCER],
                       stdout=subprocess.DEVNULL,stderr=subprocess.PIPE,check=True)
        try:
            spec=importlib.util.spec_from_file_location("b0123_selected_original_package",checkout/PACKAGER)
            need(spec is not None and spec.loader is not None,"owner_package_load")
            pack=importlib.util.module_from_spec(spec)
            spec.loader.exec_module(pack)
            need(pack.ROOT.resolve()==checkout.resolve(),"own_packager_not_bound_to_selected_original")
            raw=pack.build()  # EXACT ORIGINAL B01-23 producer package.py executes in selected worktree
            pack.verify(raw)
            need(len(raw)==EXPECTED_BYTES and sha(raw)==EXPECTED_SHA256,"original_inner_zip_hash_mismatch")
            with zipfile.ZipFile(io.BytesIO(raw)) as z:
                names=z.namelist()
                need(len(names)==EXPECTED_MEMBERS and len(names)==len(set(names)),"zip_members")
                contents={name:z.read(name) for name in names}
            manifest=json.loads(contents["PACKAGE_MANIFEST.json"])
            paths=[entry["path"] for entry in manifest["files"]]
            need(len(paths)==22 and set(paths)==set(names)-{"PACKAGE_MANIFEST.json"},
                 "original_26_exact_member_files")
            need(manifest["own_head_sha"]==PRODUCER and
                 manifest["gameplay_510_sha"]==FROZEN_GAME and
                 manifest["only_save_source_sha"]==FROZEN_SAVE and
                 manifest["native_unity"]=="NOT_RUN" and
                 manifest["canonical_510_live_import"]=="HOLD" and
                 manifest["codex_ack"] is False,
                 "forged_source_release_attestation")
            for rec in manifest["files"]:
                path=rec["path"]
                src=orig(repo,PRODUCER,path)
                need(contents[path]==src and rec["bytes"]==len(src) and
                     rec["sha256"]==sha(src),"phys_zip_not_original_git:"+path)
            checks+=len(paths)+6
            # Distinct new acceptance: triple closure of producer physical .meta GUIDs,
            # actual immutable selected gameplay/Save original Git history.
            selected={}
            for path in paths:
                if not path.endswith(".meta"):continue
                tags=[ln.split(":",1)[1].strip() for ln in contents[path].decode().splitlines() if ln.startswith("guid:")]
                need(len(tags)==1 and len(tags[0])==32,"producer_meta_invalid:"+path)
                need(tags[0] not in selected,"producer_duplicate_guid:"+path)
                selected[tags[0]]=path
            game=parent_meta(repo,FROZEN_GAME,"Assets/B0123SecureReattachV2")
            save=parent_meta(repo,FROZEN_SAVE,"Assets/B0123DetachedSaveContract001")
            # Exact original Git trees: the frozen #556 CONTRACT has FOUR .meta objects,
            # not nine. Earlier 9 was a false QA fixture assumption, not producer RED.
            print(f"ORIGINAL_META_COUNTS:selected={len(selected)} game={len(game)} save={len(save)}")
            need(len(selected)==10 and len(game)==14 and len(save)==4,
                 "selected_dependency_guid_counts")
            need(len(set(selected)|set(game)|set(save))==28,
                 "CROSS_DEPENDENCY_META_GUID_COLLISION")
            need(not (set(selected.values())&set(game.values()) or
                      set(selected.values())&set(save.values()) or
                      set(save.values())&set(game.values())),
                 "CROSS_PACKAGE_PATH_COLLISION")
            asmdefs=[json.loads(contents[p])["name"] for p in paths if p.endswith(".asmdef")]
            need(sorted(asmdefs)==sorted(["B0123.UnitySaveBridge.Core",
                "B0123.UnitySaveBridge.Unity","B0123.UnitySaveBridge.PlayModeTests"]),
                 "UNEXPECTED_ASMDEF_CLOSURE")
            need(not any("B0123SecureV2HostBridge001" in p or "DebrisRigPlayground" in p
                         for p in paths),"UNSELECTED_ALTERNATIVE_OR_LEGACY_IN_PACKAGE")
            checks+=7
            # Execute the ORIGINAL #596 stage/rollback, outside any Git/Codex/project.
            target=scratch/"fresh_released_candidate"
            pack.stage(raw,target)
            need(len([p for p in target.rglob("*") if p.is_file()])==EXPECTED_MEMBERS,
                 "physical_stage_member_count")
            pack.rollback(raw,target)
            need(not target.exists(),"original_owner_clean_rollback")
            checks+=3
            # Original rollback refusing ANY foreign EMPTY directory, even one with no
            # file to be caught by a file-only walk (historical #510 failure).
            pack.stage(raw,target)
            empty=target/"UNOWNED_EMPTY_ROOT"
            empty.mkdir()
            denied(lambda:pack.rollback(raw,target),"foreign_empty_root")
            need(empty.is_dir() and target.exists(),"foreign_empty_preserved")
            empty.rmdir()
            nested=target/"Assets/B0123UnitySaveBridge001/UNOWNED_EMPTY_NESTED"
            nested.mkdir()
            denied(lambda:pack.rollback(raw,target),"foreign_empty_nested")
            need(nested.is_dir(),"foreign_nested_preserved")
            nested.rmdir()
            checks+=4
            # Extra changed tracked original cannot be removed during cleanup.
            changed=target/paths[0]
            baseline=changed.read_bytes()
            changed.write_bytes(baseline+b"\nFOREIGN")
            denied(lambda:pack.rollback(raw,target),"modified_owned_bytes")
            need(changed.read_bytes().endswith(b"FOREIGN"),"modified_bytes_preserved")
            changed.write_bytes(baseline)
            # Unknown, symlink, missing source and existing destination refusal.
            alien=target/"alien_user_owned.txt";alien.write_bytes(b"do not erase")
            denied(lambda:pack.rollback(raw,target),"alien_file")
            need(alien.read_bytes()==b"do not erase","alien_file_preserved")
            alien.unlink()
            symlink=target/"alien_symlink"
            try:
                symlink.symlink_to(target/paths[0])
                denied(lambda:pack.rollback(raw,target),"symlink_receiver")
                need(symlink.is_symlink(),"symlink_preserved")
                symlink.unlink()
                checks+=2
            except (OSError,NotImplementedError):
                # Windows symlink privilege may be disabled; explicitly NOT_RUN
                print("SYMLINK_NEGATIVE_NOT_RUN_PRIVILEGE")
            removed=target/paths[1]
            removed_bytes=removed.read_bytes();removed.unlink()
            denied(lambda:pack.rollback(raw,target),"missing_owned")
            need(target.exists(),"missing_owned_no_destructive_cleanup")
            removed.parent.mkdir(exist_ok=True,parents=True)
            removed.write_bytes(removed_bytes)
            denied(lambda:pack.stage(raw,target),"occupied_target")
            need(target.exists(),"existing_path_preserved")
            pack.rollback(raw,target)
            need(not target.exists(),"final_original_owner_clean_rollback")
            checks+=10
            # Forged release claims inside an otherwise original ZIP must FAIL.
            forged=dict(manifest);forged["native_unity"]="PASS";forged["codex_ack"]=True
            forged_zip=io.BytesIO()
            with zipfile.ZipFile(forged_zip,"w") as z:
                for name,data in contents.items():
                    if name=="PACKAGE_MANIFEST.json":
                        data=(json.dumps(forged,sort_keys=True)+"\n").encode()
                    z.writestr(name,data)
            denied(lambda:pack.verify(forged_zip.getvalue()),"forged_release_status")
            # Forbid staging the actual current repo or outside scratch.
            denied(lambda:pack.stage(raw,checkout),"overwrite_selected_source_worktree")
            checks+=2
            receipt={
              "schema":"B01-62-P17W17-SELECTED596-PHYSICAL-INTAKE-v2",
              "chosen_pr":596,"chosen_sha":PRODUCER,"alt_hold_pr":604,"alt_hold_sha":ALT,
              "game_510_sha":FROZEN_GAME,"save_556_sha":FROZEN_SAVE,
              "inner_zip_sha256":sha(raw),"inner_zip_bytes":len(raw),"inner_members":len(names),
              "original_b01_23_source_files":len(paths),
              "guid_counts":{"selected_596":len(selected),"frozen_510":len(game),"frozen_556":len(save),
                 "triple_no_duplicates":len(set(selected)|set(game)|set(save))},
              "selected_asmdefs":sorted(asmdefs),
              "tests_pass":checks,"source_physical_gate":"PASS",
              "formal_b01_40_g0":"PENDING_INDEPENDENT","unity_physx":"NOT_RUN",
              "canonical_510_private_rigs_save":"HOLD","codex_ack":False,"integrated":False,
              "merged":False,
            }
            output.write_bytes((json.dumps(receipt,sort_keys=True,indent=2)+"\n").encode())
            print("SELECTED_596_ORIGINAL_INNER_ZIP_SHA256="+sha(raw))
            print("SELECTED_596_REAL_GIT_FILES=22 ZIP_MEMBERS=23 UNITY_META_GUIDS=10")
            print("TRIPLE_596_510_556_GUID_CLOSURE=28/28 UNIQUE ASMDEFS=3")
            print("B0162_W17_INDEPENDENT_PHYSICAL_CHECKS_PASS="+str(checks))
            print("SOURCE_G0_RECEIPT_PASS FORMAL_G0_PENDING UNITY_G1_NOT_RUN")
            print("CROSS_OS_RECEIPT_SHA256="+sha(output.read_bytes()))
        finally:
            subprocess.run(["git","-C",str(repo),"worktree","remove","--force",str(checkout)],
                           capture_output=True,check=False)

if __name__=="__main__":
    p=argparse.ArgumentParser()
    p.add_argument("--repo",required=True);p.add_argument("--output",required=True)
    args=p.parse_args()
    execute(args.repo,args.output)
