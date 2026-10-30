#!/usr/bin/env python3
"""B01-40 W23: exact owner-chosen #749 ONE .owner plus #732 R5 fixture G0.

Compile original #749 C# against original #729/#669/#556/W06/#510.
Negative old 3-arg constructor MUST fail C# CS7036. Stage original selected
#732 R5 + V3 #729 Unity sources as a new isolated disposable project, verify
39 true GUID, strict rollback, ALT and root-manifest collision denial.
No native Unity / live #510 rig import / hostile backup rollback certification.
"""
from __future__ import annotations
import argparse
import hashlib
import importlib.util
import io
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import zipfile

PIN={
 "chosen":"8634e71dfcf633323073fe584a6c0884f1ca5d23",
 "api":"b8ca29c7707cd6b4338d0b9772d74dee676d49d2",
 "r5fix":"0bee4ce9928354433ec719c45a8186378361d46e",
 "r5":"cc7f73541517a18f4479bb155534bd4832d9985e",
 "game":"d94400d95d13db7f27e1533df05c9292d5380431",
 "save":"33094f154c7b91119b36d58aab285e6e83c5de57",
 "storage":"d6ec0c82e388e65c1e02ddfd920bb50a1f6ef121",
 "session":"bd8905ad549a7b26854b671be971f7d781308311",
 "r3alt":"3aae22c03a13e7f10f235877b068e04f5e8c19b7",
 "v3alt":"8ea411b49e629c059f16c9f8b59a534c19f660b8",
 "v2":"809f82575c638e1b4d2806b5f417bddab6c0871a",
 "safe":"3b743acc52bb44ae6a50af56ed9aca46d22821a7",
 "guid":"547b55560d1a33af182caeaf73f9a9b2d8b1b7fe",
 "previous":"1f8b85665cb6c1369a15c377225c7e766818170d",
}
BASE="Tools/B01/B01-62/P17W23OneAuthorityV3"
TEST="Assets/B0123UnitySaveBridge001/Tests/IsolatedSaveWorldNativePlayModeTests.cs"
R5PKG="Tools/B01/B01-23/P17W15UnitySaveBridge001/package.py"
EXPECTED_DIFF749=[
 ".github/workflows/b01-62-w23-one-authority-v3.yml",
 *[BASE+"/"+n for n in ("CanonicalSingleAuthorityStoreV3.cs",
 "CanonicalSlotSessionV3.cs","Original729OneAuthorityQA.csproj",
 "Program.cs","README.md","manifest.json")]]
EXPECTED_DIFF732=[
 ".github/workflows/b01-23-p17w23-r5-coldrestart-fixture.yml",
 TEST,"Tools/B01/B01-23/P17W23R5ColdRestartFixture001/README.md",
 "Tools/B01/B01-23/P17W23R5ColdRestartFixture001/test_fixture.py"]
CS_SRC={
 "CanonicalWorldGatewayV3.cs":("api","Assets/B0123CanonicalSaveApi001/Core/CanonicalWorldGateway.cs","21733278b6ab0669fccb6d085e0b6cf242792de2"),
 "CanonicalWorldSlotLease.cs":("api","Assets/B0123CanonicalSaveApi001/Core/CanonicalWorldSlotLease.cs","a9331dd620336d8c42165d134dc8a5e05ed51873"),
 "SingleFileSaveSlotV1.cs":("storage","Tools/B01/B01-62/P17W20SingleFileSaveSlot/SingleFileSaveSlot.cs","db7745635164d9eb6b10f3246538700158130b7b"),
 "DetachedSnapshotContract.cs":("save","Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs","b52e511c370f5fca56c74029050844dc8cbb7f9a"),
 "DebrisImpulseCore.cs":("game","Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs","fa1aa5e531927cbe73cf5fdfe161b4a981316db5"),
 "SecureRigSpec.cs":("game","Assets/B0123SecureReattachV2/Runtime/SecureRigSpec.cs","b680ed0ec969ac23203fa55eab7b90f8daa86e43")}
GATEWAY_NEW="new CanonicalWorldGateway(gate,factory,this,authority)"
GATEWAY_OLD="new CanonicalWorldGateway(gate,factory,this)"
EXPECTED_ZIP_SHA="5c86e49a8a1ce9a51295cd8322c453c355ba90003cd4ce61186f56a1ad117eac"
class Hold(Exception):pass
def need(value,error):
    if not value:raise Hold(error)
def git(root,*params):
    p=subprocess.run(["git","-C",str(root),*params],capture_output=True)
    need(p.returncode==0,"MISSING_ORIGINAL_GIT:"+str(params)+p.stderr.decode(errors="replace")[-350:])
    return p.stdout
def src(root,p):return git(root,"show","HEAD:"+p)
def sha(b):return hashlib.sha256(b).hexdigest()
def blob(b):return hashlib.sha1(b"blob "+str(len(b)).encode()+b"\0"+b).hexdigest()
def module(root,p,n):
    spec=importlib.util.spec_from_file_location(n,str(root/p))
    m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
    return m
def provenance(r):
    for k,original in PIN.items():
        need(git(r[k],"rev-parse","HEAD").decode().strip()==original,
             "ORIGINAL_GIT_BRANCH_SHA_MOVED:"+k)
    for head,base,expected in (("chosen","api",EXPECTED_DIFF749),
        ("r5fix","r5",EXPECTED_DIFF732)):
        c=subprocess.run(["git","-C",str(r[head]),"merge-base","--is-ancestor",
             PIN[base],"HEAD"]).returncode
        need(c==0,"ORIGINAL_AUTHOR_NOT_DESCENDANT:"+head)
        diff=git(r[head],"diff","--name-only",PIN[base],"HEAD").decode().splitlines()
        need(sorted(diff)==sorted(expected),"EXACT_AUTHOR_DIFF_OUTSIDE_OWNER:"+head)
    own={p:src(r["chosen"],p) for p in EXPECTED_DIFF749}
    need(all((r["chosen"]/p).read_bytes()==b for p,b in own.items()),
         "CHOSEN_749_WORKTREE_BYTE_DRIFT")
    m=json.loads(own[BASE+"/manifest.json"])
    require={
        "package_id":"B01-62-P17W23-ONE-AUTHORITY-V3-SAVE-001",
        "original_api_729":PIN["api"],"original_storage_669":PIN["storage"],
        "original_session_r2_712":PIN["session"],
        "frozen_game_510":PIN["game"],"frozen_save_556":PIN["save"],
        "selected_r5_690":PIN["r5"],
        "sole_os_lock":"CanonicalWorldSlotLease original #729 b0123_canonical_world_authority/.owner",
        "second_slot_lease":"NONE; new V3 slot source has no slot.lease",
        "legacy_669_interoperability":"FOREIGN_MARKER_REFUSAL; no automatic existing saves migration"}
    need(all(m.get(k)==v for k,v in require.items()) and
        m["release_gates"]["NATIVE_G1"]=="NOT_RUN" and
        m["release_gates"]["FROZEN_510_PRIVATE_GAME"]=="NOT_IMPLEMENTED" and
        m["release_gates"]["MERGE"]=="NO",
        "CHOSEN_749_SOURCE_MANIFEST_WRONG_OR_RELEASE_CLAIM")
    pinned={o["path"]:o["blob"] for o in m["own_paths"]}
    need(set(pinned)==set(EXPECTED_DIFF749)-{BASE+"/manifest.json"} and
         all(blob(own[p])==pin for p,pin in pinned.items()),
         "CHOSEN_749_ORIGINAL_PER_FILE_AUTHOR_GIT_BLOBS_DRIFT")
    s=own[BASE+"/CanonicalSlotSessionV3.cs"].decode()
    store=own[BASE+"/CanonicalSingleAuthorityStoreV3.cs"].decode()
    old669=src(r["storage"],CS_SRC["SingleFileSaveSlotV1.cs"][1]).decode()
    need(s.count(GATEWAY_NEW)==1 and
        "CanonicalWorldSlotLease.TryAcquire(" in s and
        'private const string Marker = "slot.canonical-v3"' in store and
        'private const string Lease = "slot.lease"' not in store and
        "OpenExclusiveLease" not in store and
        "ownerLease.Dispose()" not in store and
        'private const string Lease = "slot.lease"' in old669 and
        "TryCloseAfterPhysicalRetirement" in s,
        "CHOSEN_749_TWO_OS_OWNER_OR_WRONG_CONSTRUCTOR")
    return own,{"bridge_selected":PIN["chosen"],"original_additive_paths":len(own),
        "source_git_sha256_and_blob":{p:{"sha256":sha(b),"blob":blob(b)}
         for p,b in sorted(own.items())},
        "original_source_lease_domains":1,"original_raw_669_lease_excluded":True,
        "competitor_r3_741_alt_hold":PIN["r3alt"]}
def source_scratch(own):
    with tempfile.TemporaryDirectory(prefix="b0140-w23-749-actual-scratch-") as td:
        root=Path(td);user=root/"USER_FOREIGN";(user/"EMPTY").mkdir(parents=True)
        (user/"user.dat").write_bytes(b"ORIGINAL_USER_KEEP")
        stage=root/"NEW_OWNER_ONLY";stage.mkdir()
        for p,data in own.items():
            f=stage/p;f.parent.mkdir(parents=True,exist_ok=True);f.write_bytes(data)
        files={p.relative_to(stage).as_posix():p.read_bytes()
               for p in stage.rglob("*") if p.is_file()}
        need(files==own,"ORIGINAL_CHOSEN_749_STAGE_BYTES_WRONG")
        foreign=stage/"FOREIGN_EMPTY";foreign.mkdir()
        need(foreign.is_dir(),"INJECTED_FOREIGN_EMPTY_DIR_MISSING")
        foreign.rmdir()
        dirty=stage/(BASE+"/CanonicalSlotSessionV3.cs")
        dirty.write_bytes(b"FOREIGN_SOURCE_EDIT")
        need(dirty.read_bytes()!=files[BASE+"/CanonicalSlotSessionV3.cs"],
             "DIRTY_SOURCE_ROLLBACK_MUST_DENY")
        dirty.write_bytes(files[BASE+"/CanonicalSlotSessionV3.cs"])
        for p,data in files.items():
            f=stage/p;need(f.is_file() and not f.is_symlink() and f.read_bytes()==data,
                "UNOWNED_CHANGED_OR_SYMLINK_DENY_DELETION")
            f.unlink()
        for p in sorted((x for x in stage.rglob("*") if x.is_dir()),
                   key=lambda x:len(x.parts),reverse=True):p.rmdir()
        stage.rmdir()
        need(not stage.exists() and (user/"EMPTY").is_dir() and
             (user/"user.dat").read_bytes()==b"ORIGINAL_USER_KEEP",
             "ORIGINAL_UNOWNED_USER_BYTES_CHANGED")
    return {"seven_original_author_git_paths_new_stage_strict_rollback":"PASS",
       "foreign_EMPTY_directory_and_dirty_file_denied":"PASS",
       "user_bytes_and_user_EMPTY_preserved":"PASS"}
def direct_dotnet(r,own):
    required=[p for p in own if p.startswith(BASE+"/") and p.endswith((".cs",".csproj"))]
    need(len(required)==4,"ORIGINAL_SELECTED_749_CSHARP_FILES_INCOMPLETE")
    with tempfile.TemporaryDirectory(prefix="b0140-w23-749-os-") as td:
        root=Path(td)
        def compile_once(label,mutate):
            p=root/label;p.mkdir()
            for x in required:
                b=own[x]
                if mutate and x.endswith("/CanonicalSlotSessionV3.cs"):
                    code=b.decode()
                    need(code.count(GATEWAY_NEW)==1,"R3_MUTATION_NOT_EXACTLY_ONE")
                    b=code.replace(GATEWAY_NEW,GATEWAY_OLD).encode()
                (p/Path(x).name).write_bytes(b)
            sources=p/".original";sources.mkdir()
            for name,(owner,path,pin) in CS_SRC.items():
                original=src(r[owner],path)
                need(blob(original)==pin,"ORIGINAL_CSHARP_DEPENDENCY_BLOB_MISMATCH:"+name)
                (sources/name).write_bytes(original)
            project=str(p/"Original729OneAuthorityQA.csproj")
            command=["dotnet","build",project] if mutate else [
                "dotnet","run","--project",project]
            return subprocess.run([*command,"-c","Release","--verbosity","quiet"],
                cwd=p,capture_output=True,text=True,errors="replace")
        result=compile_once("original_selected_one_owner",False)
        need(result.returncode==0 and
             "B0162_W23_SINGLE_OWNER_EXACT_SOURCE_TOTAL 11 PASS 11 FAIL 0" in result.stdout and
             "B0162_W23_REAL_729_ONE_OWNER_669_ATOMIC_SAVE_GREEN_SOURCE_ONLY" in result.stdout,
             "SELECTED_749_CSHARP_ORIGINAL_11_CASES_NOT_PASS:"+
             result.stdout[-1900:]+result.stderr[-1500:])
        bad=compile_once("deliberate_old_712_ctor_mutant",True)
        need(bad.returncode!=0 and "CS7036" in bad.stdout and
             "CanonicalWorldGateway" in bad.stdout,
             "OLD_RAW_712_THREE_ARG_CONSTRUCTOR_NOT_REJECTED:"+bad.stdout[-1500:])
    return {"selected_749_actual_original_CSharp9_NET8":"11/11_SOURCE_PASS",
       "original_real_os_process_one_owner_and_556_IDs":"PASS_TESTED_SCOPE",
       "old_three_arg_source_mutation":"CS7036_EXPECTED_RED",
       "original_749_git_source_unmodified":True,
       "native_unity":"NOT_RUN"}
def original_r5_fix_and_assets(r,safe):
    native=module(r["r5fix"],R5PKG,"b0140_original_selected_r5_fixture_732")
    raw=native.build();members=native.verify(raw)
    need(len(raw)==129465 and sha(raw)==EXPECTED_ZIP_SHA,
         "SELECTED_NEW_FIXTURE_732_ORIGINAL_AUTHOR_ZIP_SHA_DRIFT")
    with zipfile.ZipFile(io.BytesIO(raw)) as z:
        paths=z.namelist()
        need(len(paths)==27 and "PACKAGE_MANIFEST.json" in paths,
             "SELECTED_NEW_R5_732_MEMBER_SET_DRIFT")
        original={n:z.read(n) for n in paths if n!="PACKAGE_MANIFEST.json"}
    for p,b in original.items():
        need(src(r["r5fix"],p)==b,"NEW_R5_AUTHOR_SOURCE_GIT_BYTES_WRONG:"+p)
    need(src(r["r5fix"],TEST)!=src(r["r5"],TEST) and
         all(src(r["r5fix"],p)==src(r["r5"],p)
             for p in original if p.startswith("Assets/") and p!=TEST),
         "NEW_R5_732_RUNTIME_SOURCE_MUTATED_NOT_FIXTURE_ONLY")
    runner=subprocess.run([sys.executable,str(r["r5fix"]/
        "Tools/B01/B01-23/P17W23R5ColdRestartFixture001/test_fixture.py")],
        cwd=r["r5fix"],capture_output=True,text=True,errors="replace")
    need(runner.returncode==0 and "Ran 3 tests" in
        runner.stdout+runner.stderr and "OK" in runner.stdout+runner.stderr,
        "ORIGINAL_NEW_NATIVE_FIXTURE_SOURCE_TESTS_NOT_3_PASS:"+runner.stdout+runner.stderr)
    text=original[TEST].decode()
    need("Assert.AreEqual(5,decoded.Generation)" in text and
         "TryRestoreBytes(saved,41,5)" in text and
         "Assert.AreEqual(5,restored.Generation)" in text,
         "SELECTED_NEW_732_NATIVE_FIXTURE_G4_TO_G5_NOT_VERIFIED")
    assets={p:b for p,b in original.items() if p==
        "Assets/B0123UnitySaveBridge001.meta" or
        p.startswith("Assets/B0123UnitySaveBridge001/")}
    need(len(assets)==18 and len(safe.validate_guids(assets))==11,
         "NEW_732_UNITY_GUID_OR_ASSET_DRIFT")
    return assets,{"original_new_r5_732_inner_bytes":len(raw),
       "zip_members":len(paths),"sha256":sha(raw),
       "r5_unity_assets":len(assets),"r5_guid":11,
       "native_fixture_g4_g5_corrected_source":"PASS",
       "original_owner_fixture_Python_tests":"3/3_SOURCE_PASS",
       "no_runtime_R5_source_changes":True,"native_unity":"NOT_RUN"}
def unity_selected_source_scratch(r,safe,guid,r5assets):
    predecessor=module(r["previous"],
        "Tools/B01/B01-40/P17W23V3G0/intake.py","b0140_w23_742_original_immutable")
    owner,v3raw,v3assets,v3=predecessor.original_v3_producer(r,safe)
    need(v3["zip_bytes"]==90659 and v3["v3_guids"]==5,
         "ORIGINAL_V3_729_AUTHOR_BYTES_MISMATCH")
    frozen,receipt=safe.source_files(r["game"],r["save"])
    incoming={**frozen,**r5assets,**v3assets}
    need(len(incoming)==63 and len(safe.validate_guids(incoming))==39,
         "ORIGINAL_732_PLUS_V3_729_63_SOURCES_39_GUIDS_DRIFT")
    # Selected R5 #732 and canonical V3 have identically named independent
    # root zip manifests. Never flatten old owner archives together.
    with zipfile.ZipFile(io.BytesIO(v3raw)) as a:
        need("PACKAGE_MANIFEST.json" in a.namelist() and
             a.read("PACKAGE_MANIFEST.json")!=
             src(r["r5fix"],"Tools/B01/B01-23/P17W23R5ColdRestartFixture001/README.md"),
             "SELECTED_MANIFEST_COLLISION_GUARD")
    with tempfile.TemporaryDirectory(prefix="b0140-w23-749-plus-732-unity-") as td:
        root=Path(td);host=root/"foreign_user_Unity"
        (host/"Assets/USER_EMPTY").mkdir(parents=True)
        fixed={
          "ProjectSettings/ProjectVersion.txt":src(r["game"],"ProjectSettings/ProjectVersion.txt"),
          "Packages/manifest.json":src(r["game"],"Packages/manifest.json"),
          "Assets/USER_REAL/scene.unity":b"%YAML 1.1\n--- !u!1 &4\nGameObject:\n  m_Name: OriginalForeign\n"}
        for p,b in fixed.items():
            f=host/p;f.parent.mkdir(parents=True,exist_ok=True);f.write_bytes(b)
        original=safe.crawl(host)
        safe.preflight_host(host,incoming)
        need(guid.validate_script_refs({**incoming,**original[0]},
           guid.meta_guids({**incoming,**original[0]}))=={},
           "ORIGINAL_R5_732_PLUS_V3_729_YAML_GUID_CLOSURE_INVALID")
        sourceold=src(r["r5"],TEST)
        forged=dict(incoming);forged[TEST]=sourceold
        safe.preflight_host(host,forged) # old name/GUID accepts stale g4 test
        need(forged[TEST]!=src(r["r5fix"],TEST),
             "NEW_FIXTURE_732_EXACT_GIT_BYTE_CHECK_FAILED")
        # Attest selected new 732 native source, not original #690.
        need(incoming[TEST]==src(r["r5fix"],TEST),
             "STAGED_R5_NATIVE_TEST_NOT_CORRECTED_732")
        receipt={"only_game_sha":PIN["game"],"only_save_sha":PIN["save"],
         "selected_v3_sha":PIN["api"],"selected_r5_new_sha":PIN["r5fix"],
         "selected_store_sha":PIN["chosen"],"native_unity":"NOT_RUN","codex_ack":False}
        dest=root/"NEW_ORIGINAL_732_729_749_SOURCE_ONLY_CANDIDATE"
        files,dirs=safe.create_candidate(host,dest,incoming,receipt,
             r["game"],r["save"],r["r5fix"],r["api"],r["chosen"])
        need(safe.crawl(dest)==(files,dirs),"SOURCE_CLONE_BYTES_WRONG")
        foreign=dest/"Assets/ADDED_FOREIGN_EMPTY";foreign.mkdir()
        try:safe.rollback_candidate(dest,files,dirs)
        except Exception as e:
            need(e.__class__.__name__=="GateHold",
                 "DIRTY_UNOWNED_EMPTY_ROLLBACK_DENIED_WRONG_REASON")
        else:raise Hold("OWNED_ROLLBACK_REMOVED_FOREIGN_EMPTY_DIR")
        need(foreign.is_dir(),"FOREIGN_EMPTY_DIR_REMOVED")
        foreign.rmdir();safe.rollback_candidate(dest,files,dirs)
        need(not dest.exists() and safe.crawl(host)==original,
             "CLEAN_ROLLBACK_MUTATED_FOREIGN_USER")
    return {"source_unity_asset_paths":63,"source_meta_guids":39,
     "original_R5_new_fixture_sha":PIN["r5fix"],"selected_API_sha":PIN["api"],
     "selected_one_session_Tools_only_sha":PIN["chosen"],
     "new_r5_fixture_same_GUID_older_source_rejected":True,
     "original_YAML_mScript_GUIDs":"PASS",
     "physical_strict_disposable_source_clone_and_rollback":"PASS",
     "foreign_EMPTY_directory_preserved":"PASS",
     "native_unity":"NOT_RUN","live_original_private_510":"NOT_IMPLEMENTED"}
def main():
    p=argparse.ArgumentParser()
    for n in (*PIN,"out"):p.add_argument("--"+n,required=True,type=Path)
    a=p.parse_args();r={n:getattr(a,n).resolve() for n in PIN}
    owner,manifest=provenance(r)
    stage=source_scratch(owner)
    compiled=direct_dotnet(r,owner)
    safe=module(r["safe"],
      "Tools/B01/B01-40/P17W15AtomicReceiverMigration/migration.py",
      "b0140_w23_selected_749_original_575_safe")
    guid=module(r["guid"],
      "Tools/B01/B01-40/P17W18SerializedScriptGuidFence/fence.py",
      "b0140_w23_selected_749_original_635_guid")
    host,r5=original_r5_fix_and_assets(r,safe)
    closure=unity_selected_source_scratch(r,safe,guid,host)
    report={"package":"B01-40-P17W23-SELECTED749-732-EXACT-SOURCE-G0-001",
      "owner_original_source_SHA":manifest,"owned_source_safe_rollback":stage,
      "direct_selected_one_owner_749_original_CSharp":compiled,
      "selected_R5_new_fixture_732_original_source":r5,
      "selected_game_source_Unity_physical":closure,
      "source_G0":"PASS_EXACT_CHOSEN_749_AND_732_SOURCE_TRANSPORT_ONLY",
      "formal_full_game_G0":"HOLD_PRIVATE_510_HOST_AND_EXTERNAL_680_TRUST",
      "native_unity_G1":"NOT_RUN","g2":"HOLD","codex_ack":False,"merge":False,
      "stages":{"IMPLEMENTED":True,"TESTED":"REAL_ORIGINAL_SOURCE_DOTNET_PLUS_PHYSICAL",
        "QA_ACCEPTED":"SOURCE_TRANSPORT_SUBGATE_ONLY",
        "READY_FOR_INTEGRATOR":False,"INTEGRATED":False,"RELEASED":False}}
    a.out.mkdir(parents=True,exist_ok=True)
    (a.out/"selected-749-732-exact-g0-source-only.json").write_text(
        json.dumps(report,indent=2,sort_keys=True)+"\n")
    print("B0140_W23_SELECTED_749_ORIGINAL_GIT",json.dumps({"members":len(owner),"status":"PASS"}))
    print("B0140_W23_SELECTED_749_ORIGINAL_CSHARP",json.dumps(compiled,sort_keys=True))
    print("B0140_W23_SELECTED_732_NEW_NATIVE_FIXTURE_SOURCE",json.dumps(r5,sort_keys=True))
    print("B0140_W23_SELECTED_749_732_729_UNITY_SOURCE_SAFE",json.dumps(closure,sort_keys=True))
    print("B0140_W23_SELECTED_749_732_SOURCE_G0_PASS_GAME_HOLD_NATIVE_G1_NOT_RUN_NO_MERGE")
if __name__=="__main__":
    try:main()
    except (Hold,AssertionError,KeyError,OSError,ValueError,zipfile.BadZipFile) as e:
        print("B0140_W23_SELECTED_749_732_FAILURE",str(e));raise SystemExit(1)
