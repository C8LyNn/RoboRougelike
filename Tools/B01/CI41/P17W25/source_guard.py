#!/usr/bin/env python3
"""CI41 W25 exact original selected  #773 real OS source/legacy negative assurance."""
import json,subprocess,sys
from pathlib import Path

P="Tools/B01/CI41/P17W25/"
ORIG="3aaaec4027ee246fbfb6f209179f0e9507363473"
STORAGE="d6ec0c82e388e65c1e02ddfd920bb50a1f6ef121"
ORIGINAL={
"Tools/B01/B01-62/P17W24CanonicalPathFixR2/CanonicalSlotSessionV3R2.cs":"b3841132fbfc05da5708af4047f7db141a4abe91",
"Tools/B01/B01-62/P17W24CanonicalPathFixR2/CanonicalSingleAuthorityStoreV3R2.cs":"3689238a16d706eda1bf4905cba8029cd2fe77aa",
"Assets/B0123CanonicalSaveApi001/Core/CanonicalWorldGateway.cs":"21733278b6ab0669fccb6d085e0b6cf242792de2",
"Assets/B0123CanonicalSaveApi001/Core/CanonicalWorldSlotLease.cs":"a9331dd620336d8c42165d134dc8a5e05ed51873",
"Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs":"b52e511c370f5fca56c74029050844dc8cbb7f9a",
"Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs":"fa1aa5e531927cbe73cf5fdfe161b4a981316db5",
"Assets/B0123SecureReattachV2/Runtime/SecureRigSpec.cs":"b680ed0ec969ac23203fa55eab7b90f8daa86e43"}
STORPATH="Tools/B01/B01-62/P17W20SingleFileSaveSlot/SingleFileSaveSlot.cs"
STOR_BLOB="db7745635164d9eb6b10f3246538700158130b7b"
OWN={P+x for x in ("Program.cs","Original773CasefoldOSQA.csproj",
  "source_guard.py","test_source_guard.py","README.md","MANIFEST.json")}
OWN.add(".github/workflows/b01-41-p17w25-773-casefold-os-qa.yml")
def run(*args):
 p=subprocess.run(["git",*args],capture_output=True,text=True)
 if p.returncode:raise AssertionError("GIT_FAIL:"+str(args)+" "+p.stderr[:100])
 return p.stdout.strip()
def validate_program(s):
 must=["CanonicalSlotSessionV3R2.TryOpen","using Legacy=B01.SingleFileSaveSlot001.SingleFileSaveSlot;",
       "Legacy.TryOpenExisting","CanonicalSlotSessionV3R2.SourceHash","Process.Start(psi)",
       "a.WaitForExit(25000)&&b.WaitForExit(25000)","File.WriteAllText(start,\"GO\")",
       "session.Dispose();session.Dispose();",
       "session.TryCloseAfterPhysicalRetirement(()=>false)",
       "UNVERIFIED_RIGIDBODY_ROOTS",
       "session.TryCloseAfterPhysicalRetirement(f.RootsGone)",
       "Directory.GetFiles(authorityDir,\"*.owner\").Length==1",
       "Directory.GetFiles(root,\"*.lease\",SearchOption.AllDirectories).Length==0",
       "File.Exists(Path.Combine(saveDir,\"slot.canonical-v3\"))",
       "CanonicalGateResult.StaleGeneration","CleanupFalse=index==1,CleanupThrows=index==2",
       "f.CleanupCalls==1","newFactory.RetireAll()",
       "for(int i=0;i<3;i++)Scenario(root,i)",
       "pairs==13&&workers==26",
       "Path.GetTempPath()","Guid.NewGuid().ToString(\"N\")",
       "CI41_W25_REAL_OS_SOURCE_ONLY_PRODUCT_CASEFOLD_RED_UNITY_G1_NOT_RUN_G2_HOLD",
       "OperatingSystem.IsLinux()","RawCaseFoldWitness(root)",
       "CanonicalSingleAuthorityStoreV3R2.TryCreateNew(",
       "CI41_W25_773_LINUX_RAW_PUBLIC_CASE_ALIAS_P0_REPRODUCED_PRODUCT_RED"]
 for t in must:
  if t not in s:raise AssertionError("MISSING_TRUE_EXTERNAL_OR_ORIGINAL_SOURCE_CONTROL:"+t)
 if any(x in s for x in ("class CanonicalSlotSessionV3","class CanonicalSingleAuthorityStoreV3",
                           "class CanonicalWorldSlotLease","Application.persistentDataPath")):
  raise AssertionError("FAKE_OWNER_OR_PLAYER_SAVE_FORBIDDEN")
 return len(must)
def project(s):
 if s.count("<Compile Include=")!=9 or "<LangVersion>9.0</LangVersion>" not in s or "<TreatWarningsAsErrors>true</TreatWarningsAsErrors>" not in s:
  raise AssertionError("STRICT_DIRECT_ORIGINAL_CSHARP9_REQUIRED")
 for p in ORIGINAL:
  if p not in s:raise AssertionError("ORIGINAL_PRODUCER_NOT_COMPILED:"+p)
 if ".original/SingleFileSaveSlotV1.cs" not in s:raise AssertionError("ORIGINAL_669_NEGATIVE_WITNESS_MISSING")
def audit():
 if run("rev-parse","--verify",ORIG+"^{commit}")!=ORIG:raise AssertionError("WRONG_749_SHA")
 if run("rev-parse","--verify",STORAGE+"^{commit}")!=STORAGE:raise AssertionError("WRONG_669_SHA")
 run("merge-base","--is-ancestor",ORIG,"HEAD")
 changed=set(run("diff","--name-only",ORIG,"HEAD").splitlines())
 if changed!=OWN:raise AssertionError("OWN_PATHS_NOT_EXACTLY_SEVEN:"+repr(sorted(changed)))
 for p,sha in ORIGINAL.items():
  if run("rev-parse","HEAD:"+p)!=sha or run("rev-parse",ORIG+":"+p)!=sha:
   raise AssertionError("ORIGINAL_SHA_DRIFT:"+p)
 if run("rev-parse",STORAGE+":"+STORPATH)!=STOR_BLOB:raise AssertionError("FROZEN_STORAGE_SOURCE_DRIFT")
 if run("hash-object",P+".original/SingleFileSaveSlotV1.cs")!=STOR_BLOB:raise AssertionError("STAGED_ORIGINAL_669_SOURCE_NOT_EXACT")
 n=validate_program(Path(P+"Program.cs").read_text())
 project(Path(P+"Original773CasefoldOSQA.csproj").read_text())
 return {"status":"SELECTED_ORIGINAL_SHA_AND_TEST_SCOPE_PASS","selected_source":{"pr":773,"sha":ORIG},
         "legacy":{"pr":669,"sha":STORAGE,"git_blob":STOR_BLOB},
         "original_blobs":ORIGINAL,"added_owned_paths":sorted(OWN),"required_true_test_tokens":n,
         "expected_real_pair_races_each_os":13,"expected_external_clr_each_os":26,
         "alternatives":["#753_ALT_HOLD","#741_ALT_HOLD","#747_ALT_HOLD","#719_ALT_HOLD"],
         "G0":"SELECTED_ORIGINAL_749_FORMAL_INDEPENDENT_PENDING","G1":"NOT_RUN",
         "live_private_510":"HOLD","G2":"HOLD","CODEX_ACK":"NONE","merge":False}
if __name__=="__main__":
 target=sys.argv[1] if len(sys.argv)==2 else "ci41-w24-origin.json"
 try:r=audit();status=0
 except Exception as ex:r={"status":"REJECTED_ORIGINAL_SCOPE","error":str(ex),"G1":"NOT_RUN","G2":"HOLD"};status=2
 Path(target).write_text(json.dumps(r,sort_keys=True,indent=2)+"\n")
 print(json.dumps(r,sort_keys=True))
 sys.exit(status)
