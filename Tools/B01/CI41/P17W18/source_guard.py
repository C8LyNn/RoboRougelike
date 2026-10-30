#!/usr/bin/env python3
"""Immutable selected #596 concurrent REAL CLR process CAS gate, source-only G1 NOT_RUN."""
import argparse
import hashlib
import json
from pathlib import Path
import re
import subprocess
import sys

SELECTED="efb2b06eafedab5469b57f163152d96aca3c5915"
GAME="d94400d95d13db7f27e1533df05c9292d5380431"
SAVE="33094f154c7b91119b36d58aab285e6e83c5de57"
BASE="Tools/B01/CI41/P17W18/"
ORIGINAL={
 "Assets/B0123UnitySaveBridge001/Core/AtomicWorldSwap.cs":
    "1f9e883c38101f214e4335867faa731f7ae67689",
 "Assets/B0123UnitySaveBridge001/Unity/IsolatedTwinRigSaveHost.cs":
    "4bac6f327c9b93ae98dd2061c0ca8ea2100aab42",
 "Assets/B0123UnitySaveBridge001/Tests/IsolatedSaveWorldNativePlayModeTests.cs":
    "1ad6239b8e4e932dc1b87bb355d225867b2f4a4b",
 "Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs":
    "b52e511c370f5fca56c74029050844dc8cbb7f9a",
 "Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs":
    "fa1aa5e531927cbe73cf5fdfe161b4a981316db5",
}
OWNED={
 BASE+"Original596ParallelCheckpointQA.csproj",
 BASE+"Program.cs",BASE+"source_guard.py",BASE+"test_source_guard.py",
 BASE+"README.md",BASE+"MANIFEST.json",
 ".github/workflows/b01-41-p17w18-596-real-concurrent-cas.yml",
}
class Refuse(Exception):pass
def must(ok,code):
    if not ok:raise Refuse(code)
def git(root,*cmd):
    p=subprocess.run(["git","-C",str(root),*cmd],capture_output=True)
    must(p.returncode==0,"GIT_OBJECT_UNAVAILABLE:"+cmd[0])
    return p.stdout.decode("utf-8","replace").strip()

def check_runner_source(s):
    required=[
     'new FileCheckpointStore(file).Commit(run,0,"",1,hash)',
     "Process.Start(info)","ProcessStartInfo(\"dotnet\")",
     "File.WriteAllText(start,\"GO\")",
     "a.WaitForExit(25000)&&b.WaitForExit(25000)",
     '(ra=="COMMITTED") != (rb=="COMMITTED")',
     "for(int i=0;i<12;i++)Race(root,i,i%3!=2,i%3==1)",
     "childProcesses==24",
     "deniedWhileLocked==2&&damagedRefusals==2",
     "FileShare.None",
     "Guid.NewGuid().ToString(\"N\")",
     "Directory.Delete(root,true)",
     "ORIGINAL_HOST_MULTI_INSTANCE_LEASE_AND_NATIVE_UNITY_G1_NOT_RUN_HOLD",
    ]
    for token in required:
        must(token in s,"MISSING_REAL_CONCURRENT_OR_ISOLATION_ASSERT:"+token[:35])
    must("Application.persistentDataPath" not in s and
         "File.Delete(\"b0123_one_save_host_v2.checkpoint\")" not in s,
         "UNAUTHORIZED_SHARED_CHECKPOINT_PATH")
    must(s.count("Process.Start(info)")==1,"UNEXPECTED_CHILD_PROCESS_PRODUCER")
    must("class FileCheckpointStore" not in s,"FORBIDDEN_COPIED_SOURCE_CHECKPOINT")
    return len(required)

def check_csproj(s):
    must(s.count("Compile Include=")==5,"ORIGINAL_SOURCE_COMPILE_CLOSURE_COUNT")
    for path in ("Assets/B0123UnitySaveBridge001/Core/AtomicWorldSwap.cs",
                 "Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs",
                 "Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs",
                 "Assets/B0123SecureReattachV2/Runtime/AtomicDockLedger.cs"):
        must(path in s,"MISSING_ORIGINAL_COMPILE_REFERENCE:"+path)
    must("TreatWarningsAsErrors>true" in s and "<LangVersion>9.0</LangVersion>" in s,
         "ORIGINAL_COMPILER_WARNINGS_NOT_STRICT")
    must("Compile Include=\"Program.cs\"" in s,"MISSING_OWN_TEST_DRIVER")

def audit(repo):
    for ref in (SELECTED,GAME,SAVE):
        must(git(repo,"rev-parse","--verify",ref+"^{commit}")==ref,
             "FROZEN_OR_SELECTED_SOURCE_UNAVAILABLE")
    for ref in (GAME,SAVE):
        git(repo,"merge-base","--is-ancestor",ref,SELECTED)
    git(repo,"merge-base","--is-ancestor",SELECTED,"HEAD")
    diff=set(git(repo,"diff","--name-only",SELECTED,"HEAD").splitlines())
    must(diff==OWNED,"FOREIGN_MODIFICATION_OR_MISSING_OWN_FILE")
    for path,blob in ORIGINAL.items():
        must(git(repo,"rev-parse",SELECTED+":"+path)==blob,
             "ORIGINAL_SOURCE_GIT_BLOB_PIN_DRIFT:"+path)
        must(git(repo,"rev-parse","HEAD:"+path)==blob,
             "ORIGINAL_FROZEN_SOURCE_OVERWRITTEN:"+path)
    check_csproj((repo/(BASE+"Original596ParallelCheckpointQA.csproj")).read_text())
    cnt=check_runner_source((repo/(BASE+"Program.cs")).read_text())
    return {
      "package":"CI41-596-CROSSPROCESS-CAS-001","version":"R1",
      "source_guard":"PASS_FOR_ORIGINAL_IMMUTABLE_SOURCE",
      "actual_executable_tests":"DOTNET_RUNTIME_JOB_SEPARATE",
      "selected_original_596":SELECTED,"alternative_604":"ALT_HOLD",
      "frozen_game_510":GAME,"frozen_save_556":SAVE,
      "original_git_blobs":ORIGINAL,"owner_only_added_paths":sorted(OWNED),
      "runner_required_concurrency_asserts":cnt,
      "expected_real_child_clr_processes":24,
      "expected_real_concurrent_pairs":12,
      "native_unity_g1":"NOT_RUN","canonical_game_510_save":"HOLD",
      "full_host_lifetime_lease":"NOT_TESTED_NOT_ACCEPTED",
      "release_gate":"HOLD","g2":"HOLD","codex_ack":False,
      "integrated":False,
      "source_evidence_fingerprint":hashlib.sha256(
          json.dumps(ORIGINAL,sort_keys=True).encode()).hexdigest(),
    }
def main():
    p=argparse.ArgumentParser()
    p.add_argument("--repo",default=".")
    p.add_argument("--output",required=True)
    a=p.parse_args()
    try:
        x=audit(Path(a.repo));code=0
    except (Refuse,ValueError,OSError,UnicodeError) as e:
        x={"source_guard":"FAIL_CLOSED","error":str(e),
           "native_unity_g1":"NOT_RUN","release_gate":"HOLD"}
        code=2
    Path(a.output).write_text(json.dumps(x,indent=2,sort_keys=True)+"\n")
    print(json.dumps(x,sort_keys=True))
    return code
if __name__=="__main__":sys.exit(main())
