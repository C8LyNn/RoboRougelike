#!/usr/bin/env python3
"""B01-40 P17 W14. Own receiver-only gate: exact Save A/B original physical
packages, real #536 and #542 rollback paths, and DENY unsafe source handoff.
Actual vulnerable #544 C#9 source executed by separate original-linked .csproj.
No mutable game source changed; no current Codex receiver touched.
"""
import argparse
import hashlib
import importlib.util
import json
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile

SHAS={
 "game":"d94400d95d13db7f27e1533df05c9292d5380431",
 "save544":"e77529262b8f03c6e9f0e25b642038c351872044",
 "save549":"6da2c523e03766cd5a6deaed0cb3cccf8765354b",
 "wrapper536":"77d75d73467d236f6e3404caa58271ffec9d45cb",
 "wrapper542":"ed0234d8a3b1b3bc85f3d0a07bbe44b065c96508"
}
PINS={
 "save544":("Tools/B01/B01-23/P17W13SecureV2SaveContract001/StableIdSnapshotCodec.cs",
            "2dd3ee7f465a595ed42c2cbb6637200bb903185f"),
 "save549":("Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs",
            "6c036985411164372bf3d21d467780ff3777544e"),
 "wrapper536":("Tools/B01/B01-40/P17W13RollbackPreservation/receiver.py",
               "27ec9c682e31c8acbf7863854d9fd47829f23c21"),
 "wrapper542":("Tools/B01/B01-60/P17W12NativeSecureV2QA/runner.py",
               "269aed3ede17bfe614455e6e0edd446f60ae3b0f")
}
ORIGINAL_549_SHA256="97656f6cb2a08820f8682079c9b896cd58ca9df0f94150ae9bdbf8e4b6a13eee"
ORIGINAL_GAME_SHA256="d4ddfa3528ef0416aefffca72d884000f3b0c9959fb8dff53096487ef95c9387"

class Hold(Exception):pass
def demand(ok, msg):
    if not ok:raise Hold(msg)
def git(repo,*cmd):
    r=subprocess.run(["git","-C",str(repo),*cmd],capture_output=True)
    demand(r.returncode==0,"git "+repr(cmd)+" "+r.stderr.decode(errors="replace"))
    return r.stdout
def blobhash(data):
    return hashlib.sha1(b"blob "+str(len(data)).encode()+b"\0"+data).hexdigest()
def sha(data):
    return hashlib.sha256(data).hexdigest()
def module(name,path):
    spec=importlib.util.spec_from_file_location(name,str(path))
    mod=importlib.util.module_from_spec(spec);spec.loader.exec_module(mod)
    return mod
def original_source(roots):
    for name,repo in roots.items():
        sha_head=git(repo,"rev-parse","HEAD").decode().strip()
        demand(sha_head==SHAS[name], "PINNED_SHA_MOVED:"+name)
        if name in PINS:
            path,actual_blob=PINS[name]
            original=git(repo,"show","HEAD:"+path)
            demand(blobhash(original)==actual_blob, "SOURCE_BLOB_CHANGED:"+name)
            demand((repo/path).read_bytes()==original,"LOCAL_WORKTREE_CHANGED:"+name)
    # G0 depends on a physical immutable #510 package already accepted in #521.
    src=roots["game"]
    demand(git(src,"rev-parse","HEAD:Assets/B0123SecureReattachV2/Runtime/AtomicDockLedger.cs").decode().strip()
           =="b6a4d04b0c6407c8c84451b4d599d96fe0e4d1a9","FROZEN_GAME_ATOMIC_LEDGER_DRIFT")
    return True

def build_source_packages(roots, scratch):
    a=module("original544packager",roots["save544"]/
             "Tools/B01/B01-23/P17W13SecureV2SaveContract001/package.py")
    ar=a.original()
    demand(a.verify(ar) is True,"ORIGINAL_544_PACKAGER_VERIFY_FAILED")
    with __import__("zipfile").ZipFile(__import__("io").BytesIO(ar)) as z:
        manifest=json.loads(z.read("PACKAGE_MANIFEST.json"))
        demand(len(z.namelist())==8 and len(manifest["members"])==7, "ORIGINAL_544_MEMBER_COUNT")
        demand(manifest["own_source_sha"]==SHAS["save544"] and
               manifest["only_game_candidate_sha"]==SHAS["game"] and
               manifest["unity_native"]=="NOT_RUN" and manifest["codex_ack"] is False and
               manifest["canonical_save_restored"] is False, "ORIGINAL_544_PROVENANCE")
        for rec in manifest["members"]:
            raw=z.read(rec["path"])
            demand(raw==git(roots["save544"],"show","HEAD:"+rec["path"]) and
                   len(raw)==rec["bytes"] and sha(raw)==rec["sha256"],
                   "ORIGINAL_544_GIT_BYTES_MISMATCH")
    b=module("original549packager",roots["save549"]/
             "Tools/B01/B01-23/P17W13DetachedSaveContract001/package.py")
    br=b.build()
    b.verify(br)
    demand(len(br)==55682 and sha(br)==ORIGINAL_549_SHA256, "ORIGINAL_549_ZIP_SHA_MISMATCH")
    with __import__("zipfile").ZipFile(__import__("io").BytesIO(br)) as z:
        mf=json.loads(z.read("PACKAGE_MANIFEST.json"))
        demand(len(z.namelist())==13 and len(mf["files"])==12, "ORIGINAL_549_MEMBER_COUNT")
        demand(mf["own_head_sha"]==SHAS["save549"] and
               mf["selected_secure_v2_sha"]==SHAS["game"] and
               mf["native_unity"]=="NOT_RUN" and mf["savegame_host"]=="NOT_INTEGRATED" and
               mf["codex_ack"] is False,"ORIGINAL_549_PROVENANCE")
        for rec in mf["files"]:
            raw=z.read(rec["path"])
            demand(raw==git(roots["save549"],"show","HEAD:"+rec["path"]) and
                   len(raw)==rec["size"] and sha(raw)==rec["sha256"],"ORIGINAL_549_GIT_BYTES")
    # Incompatible save models: A is tool-only and B is importable Unity C# asset,
    # but both require exclusive *selection*, not an automatic "best" inference.
    (scratch/"source544-original-inner.zip").write_bytes(ar)
    (scratch/"source549-original-inner.zip").write_bytes(br)
    return {
      "544":{"sha":SHAS["save544"],"inner_sha256":sha(ar),"inner_bytes":len(ar),
             "members":8,"managed_unity_asset":False},
      "549":{"sha":SHAS["save549"],"inner_sha256":sha(br),"inner_bytes":len(br),
             "members":13,"managed_unity_asset":True,"known_independent_red":"#550"}
    }

def expected_hold(fn,label):
    try:fn()
    except (Hold,AssertionError,ValueError,OSError):
        return label
    raise Hold("GATE_FALSE_ACCEPTED:"+label)

def test_receiver536(roots,root):
    prev=module("originalB0140safe536",roots["wrapper536"]/PINS["wrapper536"][0])
    author=prev.source_author(roots["game"])
    with tempfile.TemporaryDirectory(prefix="b01-40-w14-real-536-zip-") as d:
        archive,files,_=prev.source_zip(roots["game"],author,Path(d)/"source.zip")
    demand(sha(archive)==ORIGINAL_GAME_SHA256,"FROZEN_GAME_PHYSICAL_SHA_DRIFT")
    project=root/"B01-40-W13-safe-stage"
    prev.stage_v2(roots["game"],files,project)
    foreign=project/"Assets"/"FOREIGN_W14_EMPTY_PARENT"/"nested_empty"
    foreign.mkdir(parents=True)
    neg=[]
    neg.append(expected_hold(lambda:prev.rollback_strict(roots["game"],files,project),
                             "B01-40-foreign-empty-nested-refused"))
    demand(foreign.is_dir(),"B01-40-FOREIGN_EMPTY_WAS_DELETED")
    foreign.rmdir();foreign.parent.rmdir()
    owned=project/next(p for p in sorted(files) if p.endswith(".cs"))
    old=owned.read_bytes()
    owned.write_bytes(b"USER MODIFIED")
    neg.append(expected_hold(lambda:prev.rollback_strict(roots["game"],files,project),
                             "B01-40-modified-owned-refused"))
    demand(owned.read_bytes()==b"USER MODIFIED","B01-40-MODIFIED_FILE_DESTROYED")
    owned.write_bytes(old)
    prev.rollback_strict(roots["game"],files,project)
    demand(not project.exists(),"B01-40-CLEAN_ROLLBACK_FAILED")
    return {"real_original_module":"#536","selected_source_only":True,
            "source_zip_sha256":sha(archive),"negative_checks":neg,
            "clean_rollback":True}

def test_receiver542(roots,root):
    runner=module("originalB0160safe542",roots["wrapper542"]/PINS["wrapper542"][0])
    entries={
     "Assets/B0160NativeProbe/fixture.cs":b"source-only physical receiver fixture",
     "Assets/B0160NativeProbe/fixture.cs.meta":
      b"fileFormatVersion: 2\nguid: 123456789012345678901234567890ab\n"
    }
    receipt={
      "original_source_sha":SHAS["game"],
      "native_unity":"NOT_RUN",
      "source_only_preflight":True,
      "codex_ack":False,
      "integrated":False,
      "native_meta_guids":runner.guid_check(entries),
      "file_sha256":{p:runner.sha(v) for p,v in sorted(entries.items())}
    }
    package=runner.zip_data(entries,receipt)
    expected=runner.verify(package,entries)
    demand(len(expected)==len(entries),"B01-60-NATIVE_SOURCE_STAGER_INVALID")
    project=root/"B01-60-W13-safe-native-stage"
    runner.stage(package,entries,project)
    foreign=project/"ProjectSettings"/"FOREIGN_W14_EMPTY_PARENT"/"nested_empty"
    foreign.mkdir(parents=True)
    neg=[]
    neg.append(expected_hold(lambda:runner.rollback(package,entries,project),
                             "B01-60-foreign-empty-nested-refused"))
    demand(foreign.is_dir(),"B01-60-FOREIGN_EMPTY_WAS_DELETED")
    foreign.rmdir();foreign.parent.rmdir()
    f=project/"FOREIGN_W14.txt";f.write_text("user")
    neg.append(expected_hold(lambda:runner.rollback(package,entries,project),
                             "B01-60-foreign-file-refused"))
    demand(f.read_text()=="user","B01-60-FOREIGN_FILE_ERASED")
    f.unlink()
    runner.rollback(package,entries,project)
    demand(not project.exists(),"B01-60-CLEAN_ROLLBACK_FAILED")
    return {"real_original_module":"#542","source_only_fixture":True,
            "negative_checks":neg,"clean_rollback":True,
            "native_unity":"NOT_RUN"}

def negative_save_gate(packages):
    # Explicit future owner choice NOT PRESENT in HQ #552: no B01-40 selection.
    # Even if selected, both W13 versions expose security sensitive mutable APIs.
    def choose(candidate,found):
        demand(candidate in ("544","549"),"NO_SINGLE_AUTHORIZED_SAVE_CONTRACT")
        demand(found=={candidate},"MULTIPLE_OR_MISSING_SOURCE_CONTRACT")
        demand(False,"SECURITY_HOLD_ORIGINAL_UNVERSIONED_MUTABLE_API_#550_#544")
    cases=[]
    for candidate,found,label in [
        (None,{"544"},"unselected-544"),
        (None,{"549"},"unselected-549"),
        ("544",{"544","549"},"dual-contract-mix-with-544"),
        ("549",{"544","549"},"dual-contract-mix-with-549"),
        ("549",{"549"},"original-549-confirmed-#550-red"),
        ("544",{"544"},"original-544-pending-CSharp-red"),
        ("544",set(),"missing-selected-archive"),
    ]:
        cases.append(expected_hold(lambda c=candidate,f=found:choose(c,f),label))
    return cases

def prepare_original_cs(roots,project):
    inp=project/"Input"
    inp.mkdir(exist_ok=True)
    paths={
      "StableIdSnapshotCodec.cs":("save544","Tools/B01/B01-23/P17W13SecureV2SaveContract001/StableIdSnapshotCodec.cs"),
      "DebrisImpulseCore.cs":("game","Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs"),
      "AtomicDockLedger.cs":("game","Assets/B0123SecureReattachV2/Runtime/AtomicDockLedger.cs"),
      "SecureRigSpec.cs":("game","Assets/B0123SecureReattachV2/Runtime/SecureRigSpec.cs")
    }
    result={}
    for name,(repo_key,p) in paths.items():
        data=git(roots[repo_key],"show","HEAD:"+p)
        (inp/name).write_bytes(data)
        result[name]=blobhash(data)
    demand(result["StableIdSnapshotCodec.cs"]==PINS["save544"][1] and
           result["DebrisImpulseCore.cs"]=="fa1aa5e531927cbe73cf5fdfe161b4a981316db5" and
           result["AtomicDockLedger.cs"]=="b6a4d04b0c6407c8c84451b4d599d96fe0e4d1a9",
           "CSharp_INPUT_SOURCE_DRIFT")
    return result

def main():
    parser=argparse.ArgumentParser()
    for name in ["game","save544","save549","wrapper536","wrapper542","project","out"]:
        parser.add_argument("--"+name,required=True,type=Path)
    a=parser.parse_args()
    roots={k:getattr(a,k).resolve() for k in SHAS}
    own=a.project.resolve()
    original_source(roots)
    a.out.mkdir(parents=True,exist_ok=True)
    with tempfile.TemporaryDirectory(prefix="B01-40-W14-SINGLE-PACKAGE-") as td:
        p=Path(td)
        packages=build_source_packages(roots,p)
        r536=test_receiver536(roots,p)
        r542=test_receiver542(roots,p)
        denials=negative_save_gate(packages)
    original_cs=prepare_original_cs(roots,own)
    record={
        "role":"B01-40", "wave":"P17W14",
        "selected_frozen_game":"#510",
        "frozen_game_sha":SHAS["game"],
        "save_contract_selection":"PENDING_B01_23_B01_62_ONLY",
        "original_save_contracts":packages,
        "source_transport":"ORIGINAL_BYTES_VALID_BOTH_CONTRACTS",
        "save_release":"HOLD_ORIGINAL_CONTRACT_549_550_RED_AND_544_BOUNDARY_RED",
        "rollback":"EXISTING_536_AND_542_SOURCE_FIXTURE_VERIFIED",
        "actual_receiver536":r536,
        "actual_receiver542":r542,
        "negative_save_release_denials":denials,
        "source_input_blobs":original_cs,
        "original_544_direct_CSharp_mutability_test":"SEE_CSharp_RUNTIME_STDOUT",
        "unity_native":"NOT_RUN","live_save_load":"NOT_INTEGRATED",
        "live_codex_migration":"NOT_RUN",
        "codex_ack":False,"game_merged":False,"g2":"HOLD"
    }
    out=a.out/"W14-save-receiver-gate.json"
    out.write_text(json.dumps(record,indent=2,sort_keys=True)+"\n")
    print("B01_40_W14_ORIGINAL_PACKAGES",json.dumps(packages,sort_keys=True))
    print("B01_40_W14_ROLLBACK_RECEIVERS_PASS",
          json.dumps({"536":r536["negative_checks"],"542":r542["negative_checks"]},sort_keys=True))
    print("B01_40_W14_RELEASE_DENIAL_PASS",len(denials),"SAVE_SCHEMA_CHOICE_PENDING #510_FROZEN")
    print("NATIVE_UNITY_NOT_RUN G2_HOLD CODEX_ACK_NONE NO_MERGE")
if __name__=="__main__":
    try:main()
    except (Hold,ValueError,OSError,subprocess.SubprocessError,KeyError,AssertionError) as exc:
        print("B01_40_W14_GATE_FAILURE",str(exc),file=sys.stderr)
        sys.exit(1)
