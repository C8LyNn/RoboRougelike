#!/usr/bin/env python3
"""Independently stage EXACT Git original R3A/R3B test-only source; never coinstall.

Git SHA + Git blob identity check, no copying another producer implementation.
This merely materializes 4 immutable original C# files in TWO separate OS scratch
build roots for direct compilation, never modifies the repository's Unity Assets.
"""
import argparse
import hashlib
import json
from pathlib import Path
import re
import subprocess
import sys

FROZEN={
    "510":"d94400d95d13db7f27e1533df05c9292d5380431",
    "556":"33094f154c7b91119b36d58aab285e6e83c5de57",
    "596":"efb2b06eafedab5469b57f163152d96aca3c5915",
}
R3={
    "A":{"pr":642,"sha":"22cec9888047994ae523cbe7bf1b0a483d64cec4",
         "core_blob":"a0a1923216d88ff8f50c560196f82cdec694f15c",
         "host_blob":"7b8354bc4cd846227b4bdf6d25d81bed34a325bc"},
    "B":{"pr":644,"sha":"d9b95146e46a5d81d5a291d7887e456164f4d0c2",
         "core_blob":"1f9e883c38101f214e4335867faa731f7ae67689",
         "host_blob":"2bcade1ba2ebe839db5240a0ca0ecf1eaaafccd0"},
    "C":{"pr":652,"sha":"ae5dbfa5e9c4689d2bf80fcabf8438e9c673c419",
         "core_blob":"04866a5682d4898a3f8ba0cfa9083cb684213269",
         "host_blob":"7b8354bc4cd846227b4bdf6d25d81bed34a325bc"}
}
FILES={
 "Assets/B0123UnitySaveBridge001/Core/AtomicWorldSwap.cs":None,
 "Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs":
    "b52e511c370f5fca56c74029050844dc8cbb7f9a",
 "Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs":
    "fa1aa5e531927cbe73cf5fdfe161b4a981316db5",
 "Assets/B0123SecureReattachV2/Runtime/AtomicDockLedger.cs":
    "b6a4d04b0c6407c8c84451b4d599d96fe0e4d1a9",
 "Assets/B0123SecureReattachV2/Runtime/SecureRigSpec.cs":
    "b680ed0ec969ac23203fa55eab7b90f8daa86e43",
}
HOST="Assets/B0123UnitySaveBridge001/Unity/IsolatedTwinRigSaveHost.cs"
OWNED={
"Tools/B01/CI41/P17W19/R3OriginalFaultQA.csproj",
"Tools/B01/CI41/P17W19/Program.cs",
"Tools/B01/CI41/P17W19/stage_original.py",
"Tools/B01/CI41/P17W19/test_stage_original.py",
"Tools/B01/CI41/P17W19/README.md",
"Tools/B01/CI41/P17W19/MANIFEST.json",
".github/workflows/b01-41-p17w19-dual-r3-original-fault.yml",
}
class Reject(Exception):pass
def require(ok,reason):
    if not ok:raise Reject(reason)
def git(*args,binary=False):
    p=subprocess.run(["git",*args],capture_output=True)
    require(p.returncode==0,"GIT_OBJECT_OR_REF_MISSING:"+args[0])
    return p.stdout if binary else p.stdout.decode("utf-8","replace").strip()

def inspect_source(candidate,core,host):
    require(candidate in R3,"CANDIDATE_UNKNOWN")
    require("public sealed class AtomicWorldSwap" in core and
            "public sealed class FileCheckpointStore" in core,
            "NOT_ORIGINAL_FROZEN_SOURCE_CORE")
    require("public bool TrySave" in core and "public WorldSwapVerdict TryLoad" in core,
            "MISSING_ORIGINAL_TRANSACTIONS")
    require("IsolatedTwinRigSaveHost" in host and "FileCheckpointStore" in host,
            "MISSING_ORIGINAL_PUBLIC_UNITY_HOST")
    require("CheckpointFence" not in core and "CheckpointFence" not in host,
            "ALT_604_FORBIDDEN")
    if candidate=="A":
        require("publishedGeneration" in core and "publishedHash" in core and
                "durable!=publishedGeneration" in core and
                "diskHash!=publishedHash" in core,"MISSING_R3A_PUBLISHED_FINGERPRINT_GUARD")
        require("OnDisable(){ReleaseSlot();}" in host and
                "TrySelectPersistentRunSlot" in host,
                "R3A_ON_DISABLE_AND_RUN_SLOT_OWNER_WIRING_CHANGED")
    elif candidate=="B":
        require("durable!=publishedGeneration" not in core and
                "publishedGeneration" not in core,
                "R3B_CORE_CHANGED_RECHECK_ORIGINAL_SECURITY")
        require("HostSlotLease.TryAcquire" in host and
                "private void OnDestroy()" in host and
                "ConfigureCheckpointSlotBeforeAwake" in host,
                "R3B_RETAINED_OWNER_SLOT_WIRING_CHANGED")
    else:
        require("commitMayHaveChangedAuthority" in core and
                "quarantined=true" in core and
                "durable!=publishedGeneration" in core and
                "diskHash!=publishedHash" in core,
                "R4_PENDING_COMMIT_STICKY_QUARANTINE_GUARD_MISSING")
        require("OnDisable(){ReleaseSlot();}" in host and
                "TrySelectPersistentRunSlot" in host,
                "R4_MUST_RETAIN_SELECTED_R3A_HOST_NOT_ALT_R3B")

def stage(candidate,root):
    require(candidate in R3,"UNKNOWN_VARIANT")
    original=R3[candidate]
    for n,sha in FROZEN.items():
        require(git("rev-parse","--verify",sha+"^{commit}")==sha,
                "MISSING_FROZEN_SOURCE:"+n)
    require(git("rev-parse","--verify",original["sha"]+"^{commit}")==
            original["sha"],"WRONG_R3_HEAD")
    git("merge-base","--is-ancestor",FROZEN["596"],original["sha"])
    if candidate=="C":
        git("merge-base","--is-ancestor",R3["A"]["sha"],original["sha"])
    # QA branch itself derives solely from #596, and contains own seven ADDED paths.
    changed=set(git("diff","--name-only",FROZEN["596"],"HEAD").splitlines())
    require(changed==OWNED,"QA_SCOPE_NOT_7_NEW_EXCLUSIVE_FILES")
    for path in OWNED:
        require(git("cat-file","-t","HEAD:"+path)=="blob",
                "NOT_REAL_QA_GIT_BLOB:"+path)
    files={}
    for path,wanted in FILES.items():
        blob=git("rev-parse",original["sha"]+":"+path)
        if path.endswith("/AtomicWorldSwap.cs"):wanted=original["core_blob"]
        require(blob==wanted,"ORIGINAL_R3_GIT_BLOB_MISMATCH:"+path)
        raw=git("show",original["sha"]+":"+path,binary=True)
        destination=root/path
        destination.parent.mkdir(parents=True,exist_ok=True)
        destination.write_bytes(raw)
        require(git("hash-object",str(destination))==blob,
                "STAGED_CSHARP_BYTES_DO_NOT_MATCH_ORIGINAL:"+path)
        files[path]={"blob":blob,"sha256":hashlib.sha256(raw).hexdigest(),
                     "bytes":len(raw)}
    blob=git("rev-parse",original["sha"]+":"+HOST)
    require(blob==original["host_blob"],"ORIGINAL_UNITY_MONOBEHAVIOUR_SHA_DRIFT")
    host=git("show",original["sha"]+":"+HOST,binary=True).decode("utf-8")
    core=(root/next(p for p in FILES if p.endswith("/AtomicWorldSwap.cs"))).read_text()
    inspect_source(candidate,core,host)
    return {
      "package":"CI41-DUAL-R3-AMBIGUOUS-COMMIT-001/R1",
      "variant":candidate,"producer_pr":original["pr"],
      "original_exact_sha":original["sha"],
      "original_frozen":FROZEN,
      "stage":"EXACT_ORIGINAL_GIT_BLOBS_ONLY",
      "no_mixed_assets":True,"selected_r3":"A_642_EXPLICIT_OWNER_CHOICE",
      "variant_role":{"A":"OWNER_SELECTED_R3_FROZEN",
                      "B":"ALT_HOLD_NEVER_CO_INSTALL",
                      "C":"OWNER_R4_DESCENDANT_PENDING_G0"}[candidate],
      "source_files":files,
      "original_public_unity_host_blob":blob,
      "runtime_compilation":"PENDING_DOTNET_RUN",
      "unity_g1":"NOT_RUN","release_gate":"HOLD",
      "codex_ack":False,"integrated":False,
    }

def main():
    p=argparse.ArgumentParser()
    p.add_argument("--candidate",required=True,choices=["A","B","C"])
    p.add_argument("--dest",required=True)
    args=p.parse_args()
    root=Path(args.dest)
    try:
        require(not root.exists(),"DESTINATION_EXISTS_DO_NOT_OVERWRITE")
        data=stage(args.candidate,root)
        print(json.dumps(data,indent=2,sort_keys=True))
        return 0
    except (OSError,Reject,UnicodeError,ValueError) as e:
        print(json.dumps({"stage":"FAIL_CLOSED","reason":str(e),
                          "release_gate":"HOLD","unity_g1":"NOT_RUN"}))
        return 2

if __name__=="__main__":sys.exit(main())
