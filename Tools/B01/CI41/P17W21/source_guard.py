#!/usr/bin/env python3
"""Exact original canonical V2 #691 + R4 #647 / frozen #556/W06/#510 guard.

No owner-source rewrite, no ALT #682/#652, no GameObjects or Unity invocation.
Git integrity is checked before real executable filesystem tests in CI.
"""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess
import sys

ROOTS={
  "game510":"d94400d95d13db7f27e1533df05c9292d5380431",
  "save556":"33094f154c7b91119b36d58aab285e6e83c5de57",
  "host596":"efb2b06eafedab5469b57f163152d96aca3c5915",
  "r3_642":"22cec9888047994ae523cbe7bf1b0a483d64cec4",
  "host_r4_647":"09c1809b73324a6d31c13e5d0143de62ac1e3ca6",
  "api_v1_681":"eed2e39636edfd8b8db754c426db452164cbf9d7",
  "api_v2_691":"809f82575c638e1b4d2806b5f417bddab6c0871a",
}
ORIGINAL={
  "Assets/B0123CanonicalSaveApi001/Core/CanonicalWorldGateway.cs":
      "704e774dedd5be5af26e0791fba87ecdb109ae2a",
  "Assets/B0123UnitySaveBridge001/Core/AtomicWorldSwap.cs":
      "8fd1dd9a1bc870433809da8fba897ec924fa4113",
  "Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs":
      "b52e511c370f5fca56c74029050844dc8cbb7f9a",
  "Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs":
      "fa1aa5e531927cbe73cf5fdfe161b4a981316db5",
  "Assets/B0123SecureReattachV2/Runtime/AtomicDockLedger.cs":
      "b6a4d04b0c6407c8c84451b4d599d96fe0e4d1a9",
  "Assets/B0123SecureReattachV2/Runtime/SecureRigSpec.cs":
      "b680ed0ec969ac23203fa55eab7b90f8daa86e43",
}
BASE="Tools/B01/CI41/P17W21/"
FILES={BASE+s for s in ("Original691RealSlotQA.csproj","Program.cs","source_guard.py",
                         "test_source_guard.py","README.md","MANIFEST.json")}
FILES.add(".github/workflows/b01-41-p17w21-original691-real-slot-qa.yml")
class Denied(Exception):pass
def require(ok,what):
    if not ok:raise Denied(what)
def git(repo,*args):
    p=subprocess.run(["git","-C",str(repo),*args],capture_output=True)
    require(p.returncode==0,"GIT_OPERATION_FAILED:"+args[0])
    return p.stdout.decode("utf-8","replace").strip()

def check_program(s):
    tokens=(
       "using B01.CanonicalWorldApi001;",
       "using B01.UnitySaveBridge001;",
       "new FileCheckpointStore(file)",
       "new CanonicalWorldGateway(",
       "CleanupUnpublishedAndVerifyDestroyed()",
       "PrepareInactive()",
       "PrepareThrowsAfterAlloc",
       "CleanupReturnsFalse",
       "CleanupThrows",
       "ThrowAfterDurable",
       "SnapshotCodec.Encode(new RunSnapshot",
       "ColdRestore.TryStage(old,out ColdRestore? cold)",
       "Process.Start(info)",
       "a.WaitForExit(25000)&&b.WaitForExit(25000)",
       '(ra=="Accepted")!=(rb=="Accepted")',
       "for(int i=0;i<8;i++)Race(root,i)",
       "races==8&&workers==16",
       "new OriginalPhysicalPort(path)",
       "Directory.Delete(root,true)",
       "Guid.NewGuid().ToString(\"N\")",
       "CI41_W21_MULTI_GATEWAY_OWNER_LEASE_EXTERNAL_NOT_CERTIFIED",
       "CI41_W21_NATIVE_UNITY_PHYSX_NOT_RUN_CANONICAL510_PRIVATE_ADAPTER_HOLD_G2_HOLD",
    )
    for t in tokens:require(t in s,"MISSING_REAL_ORIGINAL_GATEWAY_CONTROL:"+t[:50])
    require("class FileCheckpointStore" not in s and
            "class CanonicalWorldGateway" not in s,
            "FORBIDDEN_LOCAL_REIMPLEMENTATION_OF_ORIGINAL")
    require("Application.persistentDataPath" not in s and
            "Path.GetTempPath()" in s,
            "UNSAFE_PRIVATE_UNITY_STORAGE")
    return len(tokens)

def check_project(s):
    paths=list(ORIGINAL)
    require(s.count("<Compile Include=")==len(paths)+1,
            "COMPILATION_MUST_CONTAIN_EXACT_ALL_ORIGINAL_CSHARP_AND_TEST")
    for p in paths:require(p in s,"MISSING_EXACT_ORIGINAL_SOURCE:"+p)
    require('<Compile Include="Program.cs"/>' in s,
            "MISSING_NEW_QA_PROGRAM")
    require("<LangVersion>9.0</LangVersion>" in s and
            "<TreatWarningsAsErrors>true</TreatWarningsAsErrors>" in s,
            "MUST_COMPILE_REAL_ORIGINAL_AS_CSHARP9_WARNINGS_AS_ERRORS")

def audit(repo):
    for k,v in ROOTS.items():
        require(git(repo,"rev-parse","--verify",v+"^{commit}")==v,
                "MISSING_FROZEN_OR_SELECTED_GIT_COMMIT:"+k)
    for k in ("game510","save556","host596","r3_642",
              "host_r4_647","api_v1_681"):
        git(repo,"merge-base","--is-ancestor",ROOTS[k],ROOTS["api_v2_691"])
    git(repo,"merge-base","--is-ancestor",ROOTS["api_v2_691"],"HEAD")
    diff=set(git(repo,"diff","--name-only",ROOTS["api_v2_691"],"HEAD").splitlines())
    require(diff==FILES,"NON_CI41_MODIFICATION_OR_MISSING_OWN_W21_FILE")
    for path,blob in ORIGINAL.items():
        require(git(repo,"rev-parse",ROOTS["api_v2_691"]+":"+path)==blob,
                "ORIGINAL_PRODUCER_BLOB_DRIFT:"+path)
        require(git(repo,"rev-parse","HEAD:"+path)==blob,
                "PROTECTED_ORIGINAL_UNEXPECTEDLY_EDITED:"+path)
    check_project((repo/(BASE+"Original691RealSlotQA.csproj")).read_text())
    n=check_program((repo/(BASE+"Program.cs")).read_text())
    return {
       "package":"CI41-691-ORIGINAL-CROSSPROCESS-SLOT-001/R1",
       "status":"SOURCE_SHA_SCOPE_PASS",
       "source_origins":ROOTS,
       "original_csharp_blobs":ORIGINAL,
       "own_added_paths":sorted(FILES),
       "runner_control_signatures":n,
       "api_owner_selected":"681_ONLY",
       "api_alternative_682":"ALT_HOLD",
       "host_r4_owner_selected":"647_ONLY",
       "host_alt_652":"ALT_HOLD",
       "host_r5_690":"SEPARATE_NOT_INSTALLED",
       "expected_real_crossprocess_pairs_per_os":8,
       "expected_child_clr_processes_per_os":16,
       "native_unity":"NOT_RUN",
       "live_game_510_save":"HOLD",
       "independent_receiver_g0":"PENDING",
       "release":"HOLD",
       "codex_ack":False,
       "merged":False,
       "evidence_digest":hashlib.sha256(
            json.dumps(ORIGINAL,sort_keys=True).encode()).hexdigest()
    }
def main():
    p=argparse.ArgumentParser()
    p.add_argument("--repo",default=".")
    p.add_argument("--output",required=True)
    a=p.parse_args()
    try:
        result=audit(Path(a.repo))
        status=0
    except (Denied,OSError,ValueError,UnicodeError) as e:
        result={"status":"FAIL_CLOSED","error":str(e),
                "native_unity":"NOT_RUN","release":"HOLD"}
        status=2
    Path(a.output).write_text(json.dumps(result,sort_keys=True,indent=2)+"\n")
    print(json.dumps(result,sort_keys=True))
    return status
if __name__=="__main__":sys.exit(main())
