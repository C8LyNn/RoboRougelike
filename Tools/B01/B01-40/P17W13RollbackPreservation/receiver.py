#!/usr/bin/env python3
"""P17/W13 B01-40 original #510 rollback RED witness + versioned fail-closed receiver.

Exactly one immutable Secure Reattach R2 source, no main/Codex/Assets write.
Deliberately uses original frozen producer package.py to reproduce its real flaw.
"""
import argparse
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import shutil
import stat
import subprocess
import sys
import tempfile

SOURCE_SHA = "d94400d95d13db7f27e1533df05c9292d5380431"
PRODUCER_PY = "Tools/B01/B01-23/P17W10SecureReattachV2/package.py"
PRODUCER_BLOB = "a4dd7df4ea2ae83049ff76c460953c2b6cb50850"
ZIP_SHA = "d4ddfa3528ef0416aefffca72d884000f3b0c9959fb8dff53096487ef95c9387"
VERSION = "2.0.0"
OWNER = "B01-40-P17W13-ROLLBACK-SAFE-RECEIVER-R2"
MARKER = ".b0140-w13-owner.json"
EXPECTED_FILES = 35
SOURCE_FILES = 34
EXPECTED_BYTES = 99666

class Hold(Exception):
    pass

def ensure(condition, error):
    if not condition:
        raise Hold(error)

def digest(data):
    return hashlib.sha256(data).hexdigest()

def git(repo, *args):
    p = subprocess.run(["git", "-C", str(repo), *args],capture_output=True)
    ensure(p.returncode == 0, "GIT_FAILED:" + " ".join(args) + ":" + p.stderr.decode(errors="replace"))
    return p.stdout

def git_blob(data):
    return hashlib.sha1(b"blob " + str(len(data)).encode() + b"\0" + data).hexdigest()

def source_author(repo):
    ensure(git(repo,"rev-parse","HEAD").decode().strip() == SOURCE_SHA, "WRONG_SOURCE_SHA")
    script = git(repo,"show","HEAD:" + PRODUCER_PY)
    ensure(git_blob(script) == PRODUCER_BLOB, "FROZEN_ORIGINAL_ROLLBACK_CODE_DRIFT")
    ensure((repo / PRODUCER_PY).read_bytes() == script, "SOURCE_WORKSPACE_DIRTY")
    spec = importlib.util.spec_from_file_location("actual_frozen_author510", str(repo / PRODUCER_PY))
    mod = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    return mod

def source_zip(repo, mod, out):
    raw = mod.build()  # Real original producer's authored frozen build(), not a reimplementation.
    ensure(len(raw) == EXPECTED_BYTES and digest(raw) == ZIP_SHA,"ORIGINAL_PRODUCER_ARCHIVE_DRIFT")
    files = mod.verify(raw)  # Actual original Git blob verification from producer.
    ensure(len(files) == EXPECTED_FILES and len(mod.scope()) == SOURCE_FILES, "ORIGINAL_MEMBER_DRIFT")
    manifest = json.loads(files[mod.MANIFEST])
    ensure(manifest["source_sha"] == SOURCE_SHA and manifest["version"] == VERSION and
           manifest["native_unity"] == "NOT_RUN" and manifest["codex_ack"] is False,
           "ORIGINAL_PROVENANCE_DRIFT")
    ensure(len({name.casefold() for name in files}) == len(files),
           "WINDOWS_CASEFOLD_SOURCE_COLLISION")
    out.write_bytes(raw)
    return raw,files,manifest

def unsafe_repro(mod, raw, scratch):
    """An actual physical test of OLD frozen #510 rollback(), not a static grep."""
    staged=scratch/"original-stage"
    mod.stage(raw,staged)
    foreign=staged/"FOREIGN-EMPTY-DIRECTORY"/"DEEPER-EMPTY-DIRECTORY"
    foreign.mkdir(parents=True)
    ensure(foreign.is_dir() and not any(foreign.iterdir()),"BAD_NEGATIVE_FIXTURE")
    # Actual frozen producer rollback recursively removes all dirs, although only files are checked.
    original_error=None
    try:
        mod.rollback(raw,staged)
    except Exception as exc:
        original_error=type(exc).__name__+":"+str(exc)
    was_deleted = not staged.exists() and not foreign.exists()
    # If the original was fixed without changing its frozen SHA, report drift, not false RED.
    ensure(original_error is None and was_deleted,
           "ORIGINAL_FOREIGN_EMPTY_DIR_WITNESS_NOT_REPRODUCED:"+str(original_error))
    return {"original_source_sha":SOURCE_SHA,"original_rollback":"ACTUALLY_EXECUTED",
            "foreign_empty_nested_directory_was_deleted":True,
            "expected_security_result":"RED_DESTRUCTIVE_ROLLBACK",
            "old_rollback_called_on_disposable_temp_only":True}

def is_link(p):
    try:
        return p.is_symlink() or bool(getattr(p,"is_junction",lambda:False)())
    except OSError:
        return True

def safe_target(dest, repo):
    # No real project/producer checkout staging. Keep all W13 writes only in temporary scratch.
    ensure(dest.is_absolute() and dest.parent.is_dir(),"TARGET_PARENT_OR_ABSOLUTE_PATH")
    for p in [dest] + list(dest.parents):
        ensure(not is_link(p),"SYMLINK_OR_JUNCTION_IN_TARGET_PATH:"+str(p))
    for root in [repo]:
        real = root.resolve()
        ensure(not (dest.resolve() == real or real in dest.resolve().parents),
               "REPO_WRITE_FORBIDDEN")
    # Refuse *any* Git-managed destination; never alter canonical/user clones.
    p=subprocess.run(["git","-C",str(dest.parent),"rev-parse","--show-toplevel"],
                     capture_output=True)
    ensure(p.returncode != 0,"GIT_MANAGED_TARGET_FORBIDDEN")
    ensure(dest.name not in (".","..",""),"INVALID_TARGET_NAME")

def marker_bytes():
    return (json.dumps({"owner":OWNER,"selected_source_sha":SOURCE_SHA,
                       "original_zip_sha256":ZIP_SHA,"file_count":EXPECTED_FILES,
                       "native_unity":"NOT_RUN","codex_ack":False},
                       sort_keys=True,separators=(",",":"))+"\n").encode()

def expected_tree(files):
    ensure(len(files)==EXPECTED_FILES and modifiable_source_paths(files),"SOURCE_MEMBER_CONTRACT")
    expected=dict(files)
    expected[MARKER]=marker_bytes()
    dirs=set()
    for path in expected:
        pieces=Path(path).parts
        for depth in range(1,len(pieces)):
            dirs.add(Path(*pieces[:depth]).as_posix())
    return expected,dirs

def modifiable_source_paths(files):
    return all(name and not name.startswith("/") and
               ".." not in Path(name).parts and "\\" not in name and
               not name.startswith("Assets/B0123DebrisPlayground001/") and
               not name.startswith("Assets/B0123DockHardening001/") and
               not name.startswith("Assets/B0123ReattachClearance001/") for name in files)

def stage_v2(repo, files, dest, failure_after=None):
    safe_target(dest,repo)
    ensure(not dest.exists() and not is_link(dest),"TARGET_ALREADY_EXISTS")
    expected,_ = expected_tree(files)
    scratch=Path(tempfile.mkdtemp(prefix=".B0140-W13-incomplete-",dir=dest.parent))
    try:
        for index,(name,data) in enumerate(sorted(expected.items()),1):
            p=scratch/name
            p.parent.mkdir(parents=True,exist_ok=True)
            p.write_bytes(data)
            if failure_after == index:
                raise Hold("INJECTED_MID_STAGE_FAILURE")
        ensure(not dest.exists() and not is_link(dest),"RACE_DESTINATION_OCCUPIED")
        os.replace(scratch,dest) # one atomic rename on this volume
    except BaseException:
        if scratch.exists():
            shutil.rmtree(scratch)
        raise

def census(dest):
    files={}
    dirs=set()
    ensure(dest.is_dir() and not is_link(dest),"STAGE_MISSING_OR_LINK")
    for root, subdirs, filenames in os.walk(dest,topdown=True,followlinks=False):
        here=Path(root)
        for name in subdirs[:]:
            p=here/name
            ensure(not is_link(p),"FOREIGN_DIR_SYMLINK_OR_JUNCTION:"+str(p))
            ensure(p.is_dir(),"UNRECOGNIZED_DIRECTORY:"+str(p))
            dirs.add(p.relative_to(dest).as_posix())
        for name in filenames:
            p=here/name
            ensure(not is_link(p),"FOREIGN_FILE_SYMLINK:"+str(p))
            mode=p.lstat().st_mode
            ensure(stat.S_ISREG(mode) and p.stat().st_nlink == 1,"NONREGULAR_OR_HARDLINK:"+str(p))
            files[p.relative_to(dest).as_posix()]=p.read_bytes()
    return files,dirs

def rollback_strict(repo, files, dest):
    safe_target(dest,repo)
    expected, expected_dirs=expected_tree(files)
    real_files,real_dirs=census(dest)
    ensure(real_dirs == expected_dirs,
           "FOREIGN_OR_MISSING_DIRECTORY_DENIED:"+
           repr(sorted(real_dirs ^ expected_dirs)[:10]))
    ensure(real_files == expected,"FOREIGN_CHANGED_MISSING_FILE_DENIED")
    # Forbid mixed receiver fallback by exact contents, not just GUID.
    ensure(real_files[MARKER] == marker_bytes(),"WRONG_OWNERSHIP_MARKER")
    # Never call original unsafe author rollback(). Remove only enumerated owned objects.
    for name in sorted(expected,reverse=True):
        p=dest/name
        ensure(p.is_file() and not is_link(p) and p.read_bytes()==expected[name],
               "CHANGED_DURING_PRE_REMOVE")
        p.unlink()
    for name in sorted(expected_dirs,key=lambda n:(-len(Path(n).parts),n)):
        (dest/name).rmdir() # fails closed if anything appeared, no recursive deletion
    dest.rmdir() # fails closed on unexpected entries; does NOT call shutil.rmtree

def denial(fn, name):
    try:
        fn()
    except (Hold,ValueError,OSError):
        return name
    raise Hold("UNSAFE_NEGATIVE_PROBE_ACCEPTED:"+name)

def test_full(repo, mod,raw,files):
    result={}
    with tempfile.TemporaryDirectory(prefix="B01-40-W13-original-deletion-proof-") as tmp:
        base=Path(tmp)
        result["original_witness"]=unsafe_repro(mod,raw,base)
        checks=[]
        target=base/"w13-safe"
        # On crash, no partially staged visible receiver; internal scratch cleaned.
        checks.append(denial(lambda:stage_v2(repo,files,target,failure_after=4),"mid-stage-fault"))
        ensure(not target.exists() and not list(base.glob(".B0140-W13-incomplete-*")),
               "MID_STAGE_TEMP_LEAK")
        stage_v2(repo,files,target)
        checks.append(denial(lambda:stage_v2(repo,files,target),"existing-target"))
        nested=target/"FOREIGN_EMPTY_ROOT"/"FOREIGN_EMPTY_CHILD"
        nested.mkdir(parents=True)
        checks.append(denial(lambda:rollback_strict(repo,files,target),"foreign-empty-nested-directories"))
        ensure(nested.is_dir() and target.is_dir(),"FOREIGN_EMPTY_DIRECTORY_WAS_REMOVED")
        nested.rmdir();nested.parent.rmdir()
        foreign=(target/"Assets"/"UNRELATED_EMPTY")
        foreign.mkdir()
        checks.append(denial(lambda:rollback_strict(repo,files,target),"foreign-empty-directory-under-assets"))
        ensure(foreign.is_dir(),"FOREIGN_ASSET_DIR_REMOVED")
        foreign.rmdir()
        f=target/"FOREIGN.txt";f.write_text("do not delete")
        checks.append(denial(lambda:rollback_strict(repo,files,target),"foreign-file"))
        ensure(f.exists() and f.read_text()=="do not delete","FOREIGN_FILE_REMOVED")
        f.unlink()
        changed=target/next(k for k in sorted(files) if k.endswith(".cs"))
        prior=changed.read_bytes();changed.write_bytes(b"MODIFIED")
        checks.append(denial(lambda:rollback_strict(repo,files,target),"changed-file"))
        ensure(changed.read_bytes()==b"MODIFIED","MODIFIED_FILE_REMOVED")
        changed.write_bytes(prior)
        changed.unlink()
        checks.append(denial(lambda:rollback_strict(repo,files,target),"missing-file"))
        changed.parent.mkdir(parents=True,exist_ok=True)
        changed.write_bytes(prior)
        m=target/MARKER
        before=m.read_bytes();m.write_text('{"forged":true}')
        checks.append(denial(lambda:rollback_strict(repo,files,target),"forged-ownership-marker"))
        m.write_bytes(before)
        # Symlink checks are actual only when OS permissions permit creation.
        link=target/"FOREIGN-SYMLINK"
        try:
            link.symlink_to(base,target_is_directory=True)
            checks.append(denial(lambda:rollback_strict(repo,files,target),"foreign-directory-symlink"))
            ensure(link.is_symlink(),"SYMLINK_PROBE_DID_NOT_MATERIALIZE")
            link.unlink()
            result["symlink_probe"]="PASS"
        except (OSError,NotImplementedError):
            result["symlink_probe"]="NOT_RUN_PERMISSION"
            if link.is_symlink(): link.unlink()
        foreignLegacy=target/"Assets"/"B0123DebrisPlayground001"
        foreignLegacy.mkdir()
        checks.append(denial(lambda:rollback_strict(repo,files,target),"legacy-W07-foreign-directory"))
        foreignLegacy.rmdir()
        checks.append(denial(lambda:stage_v2(repo,{"../escape":b"x"},base/"bad-target"),
                             "archive-path-traversal"))
        ensure(not (base/"bad-target").exists(),"TRAVERSAL_STAGE_WRITTEN")
        cleanFiles=dict(files);cleanFiles["unexpected"]="untrusted".encode()
        checks.append(denial(lambda:rollback_strict(repo,cleanFiles,target),
                             "modified-expected-source-file-set"))
        # Recover authentic source, then exact directory census + clean rollback must pass.
        rollback_strict(repo,files,target)
        ensure(not target.exists(),"CLEAN_ROLLBACK_DID_NOT_FINISH")
        stage_v2(repo,files,target)
        rollback_strict(repo,files,target)
        ensure(not target.exists(),"SECOND_CLEAN_ROLLBACK_DID_NOT_FINISH")
        result["negative_checks"]=checks
        result["negative_count"]=len(checks)
        result["clean_stage_rollback_roundtrips"]=2
        result["unsafe_original_evidence_archived"]=True
    return result

def run():
    ap=argparse.ArgumentParser()
    ap.add_argument("--producer",required=True,type=Path)
    ap.add_argument("--out",required=True,type=Path)
    args=ap.parse_args()
    repo=args.producer.resolve()
    producer=source_author(repo)
    with tempfile.TemporaryDirectory(prefix="B01-40-W13-original-zip-") as td:
        original=Path(td)/"original-author.zip"
        raw,files,manifest=source_zip(repo,producer,original)
        result=test_full(repo,producer,raw,files)
    args.out.mkdir(exist_ok=True,parents=True)
    record={
      "receiver":"B01-40-P17W13-ROLLBACK-SAFE-RECEIVER-R2",
      "producer_frozen_sha":SOURCE_SHA, "producer_pr":510,
      "original_author_package_blob":PRODUCER_BLOB,
      "inner_zip_sha256":ZIP_SHA,"inner_zip_bytes":len(raw),
      "original_members":len(files),
      "source_scope":"B01-40 NEW VERSIONED WRAPPER ONLY; NO EDITS TO FROZEN PRODUCER",
      "selected_candidate":"#510 ONLY",
      "alternatives_not_installed":["#511","#494","legacy W07/W08/W09"],
      "source_g0_previous":"ACCEPTED_SOURCE_ONLY #521 remains preserved",
      "original_producer_rollback":"SECURITY_RED",
      "wrapper_rollback":"FAIL_CLOSED_SOURCE_FIXTURE_ACCEPTED",
      "rollback_test_evidence":result,
      "save_load_status":"HOLD_NO_PERSISTENT_CONSUMED_DETACHED_ID_SCHEMA",
      "native_unity":"NOT_RUN","g2":"HOLD","codex_ack":False,
      "live_codex_receiver_scanned":False,"game_migrated":False,
      "merged":False,
      "limitations":["Original immutable #510 contains destructive rollback for foreign empty dirs",
                     "No real current Codex workspace or native Unity host imported",
                     "Wrapper protects controlled scratch folder only, not production concurrent adversarial modifications",
                     "Live save/load remains explicitly blocked pending independent versioned contract"]
    }
    (args.out/"B01-40-W13-original-rollback-red-and-wrapper-green.json").write_text(
         json.dumps(record,indent=2,sort_keys=True)+"\n")
    print("ACTUAL_FROZEN_510_ROLLBACK_RED",json.dumps(result["original_witness"],sort_keys=True))
    print("VERSIONED_B0140_WRAPPER_GREEN",
          json.dumps({"negative_checks":result["negative_count"],
                      "clean_roundtrips":result["clean_stage_rollback_roundtrips"],
                      "symlink":result["symlink_probe"]},sort_keys=True))
    print("SINGLE_SELECTED_SHA",SOURCE_SHA,"ZIP_SHA256",ZIP_SHA,
          "SAVE_RELOAD_HOLD G1_NOT_RUN G2_HOLD NO_CODEX_ACK NO_MERGE")
if __name__=="__main__":
    try:run()
    except (Hold,OSError,KeyError,ValueError,subprocess.SubprocessError,PermissionError) as err:
        print("B01-40 W13 RELEASE_HOLD",str(err),file=sys.stderr)
        sys.exit(1)
