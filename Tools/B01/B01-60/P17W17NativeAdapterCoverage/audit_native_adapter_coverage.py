#!/usr/bin/env python3
"""B01-60 W17: original Git object exact-source audit of adapter native coverage.

NOT a Unity runner, NOT PhysX, NOT a producer G0 packager/receiver.
Proves a 16-case #532-only G1 receipt CANNOT prove #604's 5 new adapter cases.
NEVER accepts simulated XML as Unity evidence.
"""
from __future__ import annotations
import hashlib
import json
from pathlib import Path
import re
import subprocess
import sys

ROOT=Path(__file__).resolve().parents[4]
GAME="d94400d95d13db7f27e1533df05c9292d5380431"
SAVE="33094f154c7b91119b36d58aab285e6e83c5de57"
PROPOSED="1aa8a59ce1bb4fcd998f0b2b1ce25dfaea333f0a"
ALTERNATIVE="efb2b06eafedab5469b57f163152d96aca3c5915"
BASEQA="dd088a8c4823a6d128085e5ef08aca4b7f9307ea"
QA_NATIVE="Tools/B01/B01-60/P17W12NativeSecureV2QA/runner.py"
GAME_SOURCE="Assets/B0123SecureReattachV2/Runtime/SecureDebrisPlaygroundV2.cs"
GAME_LEDGER="Assets/B0123SecureReattachV2/Runtime/AtomicDockLedger.cs"
SAVE_SOURCE="Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs"
NATIVE_SOURCE="Assets/B0123UnitySaveBridge001/Tests/IsolatedSaveWorldNativePlayModeTests.cs"
BRIDGE_CORE="Assets/B0123UnitySaveBridge001/Core/AtomicWorldSwap.cs"
FENCE_CORE="Assets/B0123UnitySaveBridge001/Core/CheckpointFence.cs"
BRIDGE_UNITY="Assets/B0123UnitySaveBridge001/Unity/IsolatedTwinRigSaveHost.cs"
ASMTEST="Assets/B0123UnitySaveBridge001/Tests/B0123.UnitySaveBridge.PlayModeTests.asmdef"
ASMCORE="Assets/B0123UnitySaveBridge001/Core/B0123.UnitySaveBridge.Core.asmdef"
ASMUNITY="Assets/B0123UnitySaveBridge001/Unity/B0123.UnitySaveBridge.Unity.asmdef"
SOURCE_PINS={} # populated from ORIGINAL frozen Git source, not current working tree
NATIVE_CASES=(
 "ColdLoad_RealRigidbody_RoundTripLightHeavy",
 "CorruptLoad_LeavesPreviouslyActiveRigidbodyAndIdsUntouched",
 "ColdRestart_RestoresRealRigidbodyAndConsumedIds",
 "PersistentFence_DeniesConflictingSameGenerationAcrossHosts",
 "WrongGeneration_CannotPartiallyPublishRealPhysicalWorld",
)
PROVEN_GIT_BLOBS={
 GAME_SOURCE:"328977b4cdcdcd7c9f128c01f11ff67f8e46086a",
 GAME_LEDGER:"b6a4d04b0c6407c8c84451b4d599d96fe0e4d1a9",
 SAVE_SOURCE:"b52e511c370f5fca56c74029050844dc8cbb7f9a",
 NATIVE_SOURCE:"9b836abacaa4d3a995f765e0c964e19173da1528",
}
def git(*args):
 return subprocess.check_output(["git","-C",str(ROOT),*args],stderr=subprocess.PIPE)
def fail(message):
 raise AssertionError(message)
def require(predicate,message):
 if not predicate:fail(message)
def orig(sha,path):
 return git("show",sha+":"+path)
def blob(sha,path):
 return git("rev-parse",sha+":"+path).decode().strip()
def git_has(sha,path):
 return subprocess.run(
  ["git","-C",str(ROOT),"cat-file","-e",sha+":"+path],
  stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL,check=False
 ).returncode==0
def methods(text):
 # Enumerate real NUnit UnityTest method declarations, not mentions in prose.
 return re.findall(r"\[UnityTest\]\s*public\s+IEnumerator\s+([A-Za-z_][A-Za-z0-9_]*)\s*\(",text)
def check_assets(assets):
 require(set(assets)=={NATIVE_SOURCE,BRIDGE_CORE,FENCE_CORE,BRIDGE_UNITY,
                            ASMTEST,ASMCORE,ASMUNITY,GAME_SOURCE,GAME_LEDGER,
                            SAVE_SOURCE},"native exact source provenance not complete / extra source")
 require(set(SOURCE_PINS)==set(assets),"original immutable Git hashes not initialized")
 for name,raw in assets.items():
  require(hashlib.sha256(raw).hexdigest()==SOURCE_PINS[name],
          "original frozen Git byte integrity mismatch "+name)
 text=assets[NATIVE_SOURCE].decode("utf8")
 require(tuple(methods(text))==NATIVE_CASES,
         "required FIVE exact selected-adapter NUnit method identities missing/swapped")
 require(text.count("[UnityTest]")==5,
         "unexpected UnityTest count in selected exact adapter")
 require("PersistentFence_DeniesConflictingSameGenerationAcrossHosts" in text,
         "persistent checkpoint restart physical test missing")
 require("GetComponentsInChildren<Rigidbody>(true)" in text
         and "activeInHierarchy" in text and
         "IsConsumed" in text and
         "TrySaveBytes" in text and "TryRestoreBytes" in text,
         "actual PhysX/ID/Save cases not authored")
 require("FileCheckpointFence" in assets[FENCE_CORE].decode(),
         "real filesystem fence source not present")
 require("candidate.Capture()" in assets[BRIDGE_CORE].decode(),
         "post-activation readback source unavailable")
 require("UsePersistentCheckpointSlot" in assets[BRIDGE_UNITY].decode(),
         "Unity host lacks persistent slot switch")
 expected={
  ASMTEST:("B0123.UnitySaveBridge.PlayModeTests",{
   "B0123.UnitySaveBridge.Core","B0123.UnitySaveBridge.Unity",
   "B0123.DetachedSaveContract.Runtime"}),
  ASMCORE:("B0123.UnitySaveBridge.Core",{
   "B0123.DetachedSaveContract.Runtime","B0123.DebrisRepulse.Runtime"}),
  ASMUNITY:("B0123.UnitySaveBridge.Unity",{
   "B0123.UnitySaveBridge.Core","B0123.DetachedSaveContract.Runtime",
   "B0123.DebrisRepulse.Runtime","B0123.SecureReattachV2.Runtime"}),
 }
 for path,(name,refs) in expected.items():
  asm=json.loads(assets[path])
  require(asm["name"]==name and set(asm["references"])==refs,
          "native assembly dependency exact source mismatch: "+path)
  require(len(asm["references"])==len(refs),
          "duplicate dependency in native assembly: "+path)
  if path==ASMTEST:
   require("UNITY_INCLUDE_TESTS" in asm["defineConstraints"]
          and asm["includePlatforms"]==["Editor"]
          and "nunit.framework.dll" in asm["precompiledReferences"]
          and "TestAssemblies" in asm["optionalUnityReferences"],
          "Unity PlayMode test assembly NUnit discovery prerequisites missing")
 return len(NATIVE_CASES)
def denial(callback,name):
 try:callback()
 except (AssertionError,KeyError,ValueError,UnicodeDecodeError,TypeError):
  return name
 raise AssertionError("FAIL_OPEN source/native package accepted invalid scenario "+name)
def main():
 require(git("merge-base",BASEQA,"HEAD").decode().strip()==BASEQA,
         "must use exact frozen #601 QA baseline; no producer/canonical mutation")
 for sha in (GAME,SAVE,PROPOSED,ALTERNATIVE):
  git("cat-file","-e",sha+"^{commit}")
 require(blob(GAME,GAME_SOURCE)==PROVEN_GIT_BLOBS[GAME_SOURCE]
         and blob(PROPOSED,GAME_SOURCE)==PROVEN_GIT_BLOBS[GAME_SOURCE],
         "original #510 Unity runtime code does not match candidate #604")
 require(blob(GAME,GAME_LEDGER)==PROVEN_GIT_BLOBS[GAME_LEDGER]
         and blob(PROPOSED,GAME_LEDGER)==PROVEN_GIT_BLOBS[GAME_LEDGER],
         "original #510 private consumed ledger differs in candidate #604")
 require(blob(SAVE,SAVE_SOURCE)==PROVEN_GIT_BLOBS[SAVE_SOURCE]
         and blob(PROPOSED,SAVE_SOURCE)==PROVEN_GIT_BLOBS[SAVE_SOURCE],
         "only selected frozen #556 Save source differs in candidate #604")
 require(blob(PROPOSED,NATIVE_SOURCE)==PROVEN_GIT_BLOBS[NATIVE_SOURCE],
         "original frozen full #604 native tests source blob changed")
 oldrunner=orig(BASEQA,QA_NATIVE).decode()
 require(re.search(r'(?m)^SHA="d94400d95d13db7f27e1533df05c9292d5380431"$',oldrunner)
         and re.search(r'(?m)^PREFIXES=\("Assets/B0123DebrisRepulse001","Assets/B0123SecureReattachV2"\)',oldrunner),
         "old #532 runner source input no longer source-only selected gameplay")
 require(not git_has(GAME,SAVE_SOURCE) and not git_has(GAME,NATIVE_SOURCE),
         "exact #510 native project unexpectedly includes Save contract/adapter")
 require(git_has(PROPOSED,NATIVE_SOURCE) and git_has(PROPOSED,FENCE_CORE),
         "candidate #604 Unity host has no physical native fixture/fence")
 require(not git_has(BASEQA,NATIVE_SOURCE),
         "original #532/#601 QA receiver unexpectedly includes selected adapter")
 # Construct original immutable source sample in memory; do NOT co-install
 # producer candidates or create a competing native runner/package.
 paths=(NATIVE_SOURCE,BRIDGE_CORE,FENCE_CORE,BRIDGE_UNITY,
        ASMTEST,ASMCORE,ASMUNITY,GAME_SOURCE,GAME_LEDGER,SAVE_SOURCE)
 originals={
  p:orig(GAME if p in (GAME_SOURCE,GAME_LEDGER) else
         SAVE if p==SAVE_SOURCE else PROPOSED,p) for p in paths
 }
 SOURCE_PINS.update({p:hashlib.sha256(raw).hexdigest() for p,raw in originals.items()})
 positive=check_assets(originals)
 require(positive==5,"exact source five-case sample not verified")
 # Alternative #596 is NEVER blended with #604, only a negative mismatched
 # source sample. #596's FOUR original authored cases are not five #604 cases.
 alternate=orig(ALTERNATIVE,NATIVE_SOURCE).decode()
 require(len(methods(alternate))==4,
         "competing producer alternative changed: must re-evaluate negative")
 neg=0
 bad=dict(originals)
 bad[NATIVE_SOURCE]=orig(ALTERNATIVE,NATIVE_SOURCE)
 denial(lambda:check_assets(bad),"alternate #596 4-case test source substituted");neg+=1
 bad=dict(originals);bad[NATIVE_SOURCE]=bad[NATIVE_SOURCE].replace(
  b"PersistentFence_DeniesConflictingSameGenerationAcrossHosts",
  b"MissingPhysicalFenceTest",1)
 denial(lambda:check_assets(bad),"same-count but forged native suite case");neg+=1
 bad=dict(originals);bad.pop(FENCE_CORE)
 denial(lambda:check_assets(bad),"missing selected original persistent fence source");neg+=1
 bad=dict(originals);bad[GAME_SOURCE]=b"foreign V2 runtime"
 denial(lambda:check_assets(bad),"game V2 runtime differs from exact source");neg+=1
 bad=dict(originals);bad[ASMTEST]=bad[ASMTEST].replace(b"UNITY_INCLUDE_TESTS",
                                                        b"B01_FAKE_TESTS",1)
 denial(lambda:check_assets(bad),"NUnit TestRunner discovery disabled");neg+=1
 bad=dict(originals);bad[ASMUNITY]=bad[ASMUNITY].replace(
  b"B0123.SecureReattachV2.Runtime",b"FOREIGN_GAME_V2",1)
 denial(lambda:check_assets(bad),"native host original #510 dependency replaced");neg+=1
 bad=dict(originals);bad[NATIVE_SOURCE]=bad[NATIVE_SOURCE].replace(
  b"GetComponentsInChildren<Rigidbody>(true)",b"GetFakeBodies()",1)
 denial(lambda:check_assets(bad),"real Rigidbody source test removed");neg+=1
 bad=dict(originals);bad[SAVE_SOURCE]=b"foreign second Save format"
 denial(lambda:check_assets(bad),"Save contract SHA source switched");neg+=1
 require(neg==8,"expected 8 negative source/native coverage checks")
 print("B01_60_W17_SELECTED_GIT_SOURCE_510_556_604_BLOB_PINS_PASS",4)
 print("B01_60_W17_PROPOSED_604_FIVE_UNITY_PLAYMODE_CASES_SOURCE_AUTHORED",positive)
 print("B01_60_W17_ALTERNATIVE_596_NO_MIXING_4_VS_5_NEGATIVE_PASS")
 print("B01_60_W17_NATIVE_ADAPTER_SOURCE_SCOPE_NEGATIVE_PASS",neg)
 print("B01_60_W17_ORIGINAL_532_NATIVE_PROJECT_MISSING_556_AND_ADAPTER_604_WITNESSED")
 print("B01_60_W17_ACTUAL_UNITY_EDIT_PLAY_PHYSX_NOT_RUN_NO_ACTIVATED_EDITOR")
 out=ROOT/"qa-w17-native-adapter-coverage-evidence"
 require(not out.exists(),"refuse existing output folder")
 out.mkdir()
 receipt={
  "qa_owner":"B01-60","qa_head_sha":git("rev-parse","HEAD").decode().strip(),
  "game_510_source_sha":GAME,"only_save_556_source_sha":SAVE,
  "full_candidate_under_review_604_sha":PROPOSED,
  "unselected_competitor_596_sha":ALTERNATIVE,
  "producer_choice":"B01-23_AND_62_ONLY; QA_DOES_NOT_SELECT",
  "original_532_existing_native_receiver_only_game_510":True,
  "original_532_includes_556_or_604_native_adapter":False,
  "additional_native_playmode_cases_authored_exact_604":positive,
  "original_independent_532_unity_cases_authored":16,
  "physical_native_adaptor_cases_executed":0,
  "exact_original_files_audited":len(originals),
  "adversarial_no_mix_negative_pass":neg,
  "native_CI_evidence":"NOT_RUN",
  "licensed_unity_6000_3_14f1":"NOT_AVAILABLE",
  "real_nunit_xml":"NOT_CREATED","real_editor_log":"NOT_CREATED",
  "PhysX":"NOT_RUN","G1":"NOT_RUN","G2":"HOLD",
  "canonical_510_private_ledger_hydration":"HOLD",
  "Codex_ACK":"NONE","merged":False,
  "source_blobs_sha1":{p:blob(GAME if p in (GAME_SOURCE,GAME_LEDGER)
    else SAVE if p==SAVE_SOURCE else PROPOSED,p) for p in paths},
  "source_blobs_sha256":{p:hashlib.sha256(raw).hexdigest()
                                for p,raw in originals.items()},
  "native_case_names":NATIVE_CASES,
 }
 (out/"receipt.json").write_text(json.dumps(receipt,sort_keys=True,indent=2)+"\n")
 return 0
if __name__=="__main__":
 try:sys.exit(main())
 except Exception as exc:
  print("B01_60_W17_SOURCE_AUDIT_FAIL",repr(exc),file=sys.stderr)
  sys.exit(2)
