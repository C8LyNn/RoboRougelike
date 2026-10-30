#!/usr/bin/env python3
"""B01-41 W23 exact immutable original V3 #729 and storage #669 source audit."""
import json, subprocess,sys
from pathlib import Path
BASE="Tools/B01/CI41/P17W23/"
OWNER="b8ca29c7707cd6b4338d0b9772d74dee676d49d2"
SAVE="d6ec0c82e388e65c1e02ddfd920bb50a1f6ef121"
EXPECTED={
 "Assets/B0123CanonicalSaveApi001/Core/CanonicalWorldGateway.cs":"21733278b6ab0669fccb6d085e0b6cf242792de2",
 "Assets/B0123CanonicalSaveApi001/Core/CanonicalWorldSlotLease.cs":"a9331dd620336d8c42165d134dc8a5e05ed51873",
 "Assets/B0123UnitySaveBridge001/Core/AtomicWorldSwap.cs":"8fd1dd9a1bc870433809da8fba897ec924fa4113",
 "Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs":"b52e511c370f5fca56c74029050844dc8cbb7f9a",
 "Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs":"fa1aa5e531927cbe73cf5fdfe161b4a981316db5",
 "Assets/B0123SecureReattachV2/Runtime/AtomicDockLedger.cs":"b6a4d04b0c6407c8c84451b4d599d96fe0e4d1a9",
 "Assets/B0123SecureReattachV2/Runtime/SecureRigSpec.cs":"b680ed0ec969ac23203fa55eab7b90f8daa86e43"}
SAVE_PATH="Tools/B01/B01-62/P17W20SingleFileSaveSlot/SingleFileSaveSlot.cs"
SAVE_BLOB="db7745635164d9eb6b10f3246538700158130b7b"
OWN={BASE+x for x in ("Program.cs","OriginalV3StorageAuthorityQA.csproj",
   "source_guard.py","test_source_guard.py","README.md","MANIFEST.json")}
OWN.add(".github/workflows/b01-41-p17w23-v3-669-two-lease-witness.yml")
def command(*args):
    p=subprocess.run(["git",*args],capture_output=True,text=True)
    if p.returncode:raise AssertionError("GIT_FAIL:"+str(args)+" "+p.stderr[:100])
    return p.stdout.strip()
def check_program(s):
    checks=["using B01.CanonicalWorldApi001;","using B01.SingleFileSaveSlot001;",
      "new CanonicalWorldGateway(", "new FileCheckpointStore(path)",
      "SingleFileSaveSlot.TryCreateNew", "CanonicalWorldSlotLease.TryAcquire",
      "store.Dispose();","gateway.AuthorityActive&&factory.Last.Active",
      'first=="BUSY"&&rb=="BUSY"', "a.WaitForExit(25000)&&b.WaitForExit(25000)",
      "Process.Start(psi)","File.WriteAllText(start,\"GO\")",
      "CanonicalSlotSessionR2", # check should not use it, removed
      "cold.TryRead(out long gen,out byte[] actual)","CheckIds(actual,1,false,true)",
      "CheckIds(oldSnapshot,10,true,false)","pairs==8&&workers==16&&bypass==4",
      "Path.GetTempPath()","Guid.NewGuid().ToString(\"N\")"]
    checks.remove("CanonicalSlotSessionR2")
    checks[8]='ra=="BUSY"&&rb=="BUSY"'
    for c in checks:
        if c not in s:raise AssertionError("MISSING_REAL_ORIGINAL_TEST:"+c)
    if any(x in s for x in ["class CanonicalWorldSlotLease","class FileCheckpointStore",
                             "class SingleFileSaveSlot","Application.persistentDataPath"]):
        raise AssertionError("FORBIDDEN_SUBSTITUTE_SOURCE_OR_PRIVATE_SAVE_PATH")
    return len(checks)
def check_project(s):
    if (s.count("<Compile Include=")!=9 or
        "<LangVersion>9.0</LangVersion>" not in s or
        "<TreatWarningsAsErrors>true</TreatWarningsAsErrors>" not in s):
        raise AssertionError("MISSING_EXACT_DIRECT_SOURCE_CSHARP9")
    for p in EXPECTED:
        if p not in s:raise AssertionError("MISSING_ORIGINAL_SOURCE:"+p)
    for p in ["Program.cs",".source/SingleFileSaveSlot.cs"]:
        if p not in s:raise AssertionError("MISSING_ORIGINAL_STORAGE_OR_TEST:"+p)
def audit():
    if command("rev-parse","--verify",OWNER+"^{commit}")!=OWNER:
        raise AssertionError("OWNER_VERSION_NOT_EXACT")
    if command("rev-parse","--verify",SAVE+"^{commit}")!=SAVE:
        raise AssertionError("STORAGE_VERSION_NOT_EXACT")
    command("merge-base","--is-ancestor",OWNER,"HEAD")
    diffs=set(command("diff","--name-only",OWNER,"HEAD").splitlines())
    if diffs!=OWN:raise AssertionError("NOT_EXACT_SEVEN_OWN_ONLY_PATHS")
    for p,blob in EXPECTED.items():
        if command("rev-parse","HEAD:"+p)!=blob or command("rev-parse",OWNER+":"+p)!=blob:
            raise AssertionError("FROZEN_OR_SELECTED_SOURCE_CHANGED:"+p)
    if command("rev-parse",SAVE+":"+SAVE_PATH)!=SAVE_BLOB:
        raise AssertionError("ORIGINAL_669_BLOB_DRIFT")
    if command("hash-object",BASE+".source/SingleFileSaveSlot.cs")!=SAVE_BLOB:
        raise AssertionError("STAGED_ORIGINAL_669_BLOB_DRIFT")
    s=Path(BASE+"Program.cs").read_text()
    n=check_program(s)
    check_project(Path(BASE+"OriginalV3StorageAuthorityQA.csproj").read_text())
    return {"status":"ORIGINAL_SHA_SCOPE_PASS","owner_v3":{"pr":729,"sha":OWNER},
       "original_storage":{"pr":669,"sha":SAVE,"blob":SAVE_BLOB},
       "immutable_git_blobs":EXPECTED,"own_added_files":sorted(OWN),
       "actual_source_guard_checks":n,"real_true_clr_pairs_each_os":8,
       "real_external_workers_each_os":16,"real_unity_g1":"NOT_RUN",
       "expected_witness":"TWO_INDEPENDENT_OWNER_LEASES_PRODUCT_HOLD",
       "g0":"EXACT_NEW_BRIDGE_PENDING","g2":"HOLD","codex_ack":False}
if __name__=="__main__":
    out=sys.argv[1] if len(sys.argv)==2 else "ci41-w23-receipt.json"
    try:r=audit();code=0
    except Exception as e:
        r={"status":"FAIL_CLOSED","error":str(e),"real_unity_g1":"NOT_RUN","g2":"HOLD"}
        code=2
    Path(out).write_text(json.dumps(r,indent=2,sort_keys=True)+"\n")
    print(json.dumps(r,sort_keys=True))
    sys.exit(code)
