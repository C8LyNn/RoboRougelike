#!/usr/bin/env python3
"""Source-of-truth only CI41 R2 #712 and original #691, #669, #556/W06 blob gate."""
import argparse,json
from pathlib import Path
import subprocess
import sys
OWN_BASE="Tools/B01/CI41/P17W22/"
FILES={OWN_BASE+s for s in
    ("Original712SessionLeaseQA.csproj","Program.cs","source_guard.py",
     "test_source_guard.py","README.md","MANIFEST.json")}
FILES.add(".github/workflows/b01-41-p17w22-original712-session-lease.yml")
COMMITS={
 "R2_712":"bd8905ad549a7b26854b671be971f7d781308311",
 "R1_697":"ab62b041e50f8aa0defada2aadc0987d6c63f9d9",
 "API_V2_691":"809f82575c638e1b4d2806b5f417bddab6c0871a",
 "API_SELECTED_681":"eed2e39636edfd8b8db754c426db452164cbf9d7",
 "STORAGE_669":"d6ec0c82e388e65c1e02ddfd920bb50a1f6ef121",
 "HOST_R5_690":"cc7f73541517a18f4479bb155534bd4832d9985e",
 "GAME_510":"d94400d95d13db7f27e1533df05c9292d5380431",
 "SAVE_556":"33094f154c7b91119b36d58aab285e6e83c5de57",
 "HOST_R4_647":"09c1809b73324a6d31c13e5d0143de62ac1e3ca6"
}
ORIGINS={
  "R2":("bd8905ad549a7b26854b671be971f7d781308311",
       "Tools/B01/B01-62/P17W21CanonicalSessionLeaseR2/CanonicalSlotSessionR2.cs",
       "fefc2e263e068b913c6e59ec3c8375bbbe43db4e"),
  "API":("809f82575c638e1b4d2806b5f417bddab6c0871a",
       "Assets/B0123CanonicalSaveApi001/Core/CanonicalWorldGateway.cs",
       "704e774dedd5be5af26e0791fba87ecdb109ae2a"),
  "STORE":("d6ec0c82e388e65c1e02ddfd920bb50a1f6ef121",
       "Tools/B01/B01-62/P17W20SingleFileSaveSlot/SingleFileSaveSlot.cs",
       "db7745635164d9eb6b10f3246538700158130b7b"),
  "SAVE":("33094f154c7b91119b36d58aab285e6e83c5de57",
       "Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs",
       "b52e511c370f5fca56c74029050844dc8cbb7f9a"),
  "W06":("d94400d95d13db7f27e1533df05c9292d5380431",
       "Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs",
       "fa1aa5e531927cbe73cf5fdfe161b4a981316db5"),
  "SPEC":("d94400d95d13db7f27e1533df05c9292d5380431",
       "Assets/B0123SecureReattachV2/Runtime/SecureRigSpec.cs",
       "b680ed0ec969ac23203fa55eab7b90f8daa86e43")
}
class Denied(Exception):pass
def require(a,msg):
    if not a:raise Denied(msg)
def git(*argv):
    p=subprocess.run(["git",*argv],capture_output=True)
    require(p.returncode==0,"GIT_MISSING_OR_INVALID:"+argv[0])
    return p.stdout.decode().strip()
def inspect(program,csproj):
    tests=["using B01.CanonicalSlotAdapter002;","CanonicalSlotSessionR2.TryOpen(",
      "new[]{Assembly.GetExecutingAssembly().Location", # deliberately checked separately below if adapted
      "Process.Start(psi)","File.WriteAllText(go,\"START\")",
      "a.WaitForExit(25000)&&b.WaitForExit(25000)",
      "first==\"BUSY\"&&second==\"BUSY\"",
      "first==\"ACCEPTED\"&&second==\"BUSY\"",
      "PrepareThrows=true","ReturnFalse","ThrowCleanup",
      "owner.Dispose();owner.Dispose();",
      "TryCloseAfterPhysicalRetirement(()=>false)",
      "UNVERIFIABLE_RIG_HIERARCHY",
      "f.TrustedPhysicalRetire()",
      "for(int i=0;i<2;i++)Pair(dir,variant+\"-after\",i,false)",
      "pairs==10&&workers==20","CanonicalSlotSessionR2.SourceHash(Bytes(1,true,false))",
      "CI41_W22_SOURCE_ONLY_OWNER_PROOF_CALLBACK_NOT_NATIVE_RIG_ATTESTATION",
      "Path.GetTempPath()","Guid.NewGuid().ToString(\"N\")"]
    tests.remove("new[]{Assembly.GetExecutingAssembly().Location")
    for test in tests:require(test in program,"MISSING_ACTUAL_EXTERNAL_OS_TEST:"+test[:60])
    require("class CanonicalSlotSessionR2" not in program and
            "class SingleFileSaveSlot" not in program and
            "class CanonicalWorldGateway" not in program,
            "FORBIDDEN_COPIED_PRODUCTION")
    require("Application.persistentDataPath" not in program,
            "FORBIDDEN_PLAYER_DATA_ACCESS")
    require(csproj.count("<Compile Include=")==7 and
            "<LangVersion>9.0</LangVersion>" in csproj and
            "<TreatWarningsAsErrors>true</TreatWarningsAsErrors>" in csproj,
            "MISSING_STRICT_ACTUAL_CSHARP_COMPILER_CLOSURE")
    for p in ("CanonicalSlotSessionR2.cs","CanonicalWorldGatewayV2.cs",
              "SingleFileSaveSlot.cs","DetachedSnapshotContract.cs",
              "DebrisImpulseCore.cs","SecureRigSpec.cs","Program.cs"):
        require(p in csproj,"MISSING_EXACT_DIRECT_CSHARP_SOURCE:"+p)
    return len(tests)
def check(root):
    for k,s in COMMITS.items():
        require(git("rev-parse","--verify",s+"^{commit}")==s,"MISSING_SHA:"+k)
    git("merge-base","--is-ancestor",COMMITS["R1_697"],COMMITS["R2_712"])
    git("merge-base","--is-ancestor",COMMITS["R2_712"],"HEAD")
    changed=set(git("diff","--name-only",COMMITS["R2_712"],"HEAD").splitlines())
    require(changed==FILES,"OWNER_CI41_PATH_VIOLATION")
    require(all(git("cat-file","-t","HEAD:"+p)=="blob" for p in FILES),
            "OWN_FILES_NOT_GIT_BLOBS")
    for k,(sha,path,blob) in ORIGINS.items():
        require(git("rev-parse",sha+":"+path)==blob,"ORIGINAL_SOURCE_DRIFT:"+k)
        # R2 original in base checkout must be unchanged.
        if k=="R2":require(git("rev-parse","HEAD:"+path)==blob,
                           "ORIGINAL_OWNER_R2_MODIFIED")
        if k!="R2":
            stage={"API":"CanonicalWorldGatewayV2.cs","STORE":"SingleFileSaveSlot.cs",
                   "SAVE":"DetachedSnapshotContract.cs","W06":"DebrisImpulseCore.cs",
                   "SPEC":"SecureRigSpec.cs"}[k]
            require(git("hash-object",str(root/(OWN_BASE+".source/"+stage)))==blob,
                    "STAGED_ORIGINAL_GIT_BLOB_DRIFT:"+k)
    n=inspect((root/(OWN_BASE+"Program.cs")).read_text(),
              (root/(OWN_BASE+"Original712SessionLeaseQA.csproj")).read_text())
    return {"package":"CI41-712-707-LEASE-ESCROW-001/R1",
            "status":"EXACT_SOURCE_GIT_SCOPE_PASS","original_commits":COMMITS,
            "original_source_blobs":{k:v[2] for k,v in ORIGINS.items()},
            "new_exclusive_ci41_paths":sorted(FILES),"hardening_tokens":n,
            "other_competing_proposals":["#652_ALT_HOLD","#682_ALT_HOLD","#693_ALT_HOLD","#695_ALT_HOLD"],
            "expected_real_os_races":10,"expected_child_CLR":20,
            "native_unity":"NOT_RUN","canonical_private_510":"NOT_WIRED",
            "g0":"R2_NEW_SHA_INDEPENDENT_PENDING","g2":"HOLD",
            "codex_ack":False,"integrated":False}
def main():
    p=argparse.ArgumentParser();p.add_argument("--output",required=True)
    a=p.parse_args()
    try:res=check(Path("."));code=0
    except (Denied,OSError,ValueError,UnicodeError) as e:
        res={"status":"FAIL_CLOSED","error":str(e),"native_unity":"NOT_RUN","g2":"HOLD"}
        code=2
    Path(a.output).write_text(json.dumps(res,indent=2,sort_keys=True)+"\n")
    print(json.dumps(res,sort_keys=True))
    return code
if __name__=="__main__":sys.exit(main())
