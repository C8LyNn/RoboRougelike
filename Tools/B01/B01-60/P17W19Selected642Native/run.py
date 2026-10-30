#!/usr/bin/env python3
"""B01-60 W19: adapt OWN existing #643 native launcher to ONE owner-selected R3.

NO competing R3/alternative source. This reuses exact original W18 QA runner,
rather than forking/reimplementing game, physical host, or Unity lifecycle.
Actual Editor/PhysX only if legally activated and executed (otherwise NOT_RUN).
"""
from __future__ import annotations
import importlib.util
import re
from pathlib import Path
import sys

ROOT=Path(__file__).resolve().parents[4]
EXISTING=ROOT/"Tools/B01/B01-60/P17W18Selected596Native/run.py"
GAME="d94400d95d13db7f27e1533df05c9292d5380431"
SAVE="33094f154c7b91119b36d58aab285e6e83c5de57"
BASE="acddb0b5226df2354b2255e0af510e39fe464381"
SELECTED="22cec9888047994ae523cbe7bf1b0a483d64cec4" # B01-23 only R3 #642
R3_ALT="d9b95146e46a5d81d5a291d7887e456164f4d0c2" # #644 ALT_HOLD
R2_FROZEN="efb2b06eafedab5469b57f163152d96aca3c5915"
LEGACY_ALT="1aa8a59ce1bb4fcd998f0b2b1ce25dfaea333f0a" # #604 ALT_HOLD
METHODS=(
 "ColdLoad_RealRigidbody_RoundTripLightHeavy",
 "CorruptLoad_LeavesPreviouslyActiveRigidbodyAndIdsUntouched",
 "ColdRestart_RestoresRealRigidbodyAndConsumedIds",
 "WrongGeneration_CannotPartiallyPublishRealPhysicalWorld",
 "SeparateRunSlots_CannotPoisonLaterRunOrReuseForeignOwner",
)
FILES={
 "Tools/B01/B01-60/P17W19Selected642Native/run.py",
 "Tools/B01/B01-60/P17W19Selected642Native/test_source_fixture.py",
 "Tools/B01/B01-60/P17W19Selected642Native/README.md",
 "Tools/B01/B01-60/P17W19Selected642Native/manifest.json",
 ".github/workflows/b01-60-p17w19-selected642-native-fixture.yml",
}
FIXTURE="Assets/B0123UnitySaveBridge001/Tests/IsolatedSaveWorldNativePlayModeTests.cs"
CORE="Assets/B0123UnitySaveBridge001/Core/AtomicWorldSwap.cs"
HOST="Assets/B0123UnitySaveBridge001/Unity/IsolatedTwinRigSaveHost.cs"
FIXTURE_BLOB="e398b1fde8dcbd8e6d30a65b8f4b91a4965b9dd8"
CORE_BLOB="a0a1923216d88ff8f50c560196f82cdec694f15c"
HOST_BLOB="7b8354bc4cd846227b4bdf6d25d81bed34a325bc"
def require(condition,message):
 if not condition:raise AssertionError(message)
def adapt():
 spec=importlib.util.spec_from_file_location("b0160_native_r3_original_W18_launcher",EXISTING)
 require(spec is not None and spec.loader is not None,"original own W18 native launcher missing")
 m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
 require(m.ONLY==R2_FROZEN and m.ALT==LEGACY_ALT and
         m.BASE=="f4e26ac21ade9a54c0c38927ee48cea50ed8377c" and
         m.git("rev-parse",BASE+":Tools/B01/B01-60/P17W18Selected596Native/run.py").decode().strip()=="25448069aec7a0e71473169e2ddae56437e8723a",
         "W18 original QA launcher source unexpectedly modified")
 m.ONLY=SELECTED
 m.ALT=R3_ALT
 m.BASE=BASE
 m.METHODS=METHODS
 # Old #532 source provenance ZIP receiver recognizes all own W19 files in its
 # exclusive OWN set; only the difference between W18 and W19 is W19-owned.
 historical_own_W18_files=set(m.W18)
 m.W18=FILES
 # Original #532 source receiver checks the COMPLETE owned QA path inventory
 # from frozen #510 to current HEAD. W18's Git files still exist in that tree,
 # even though W18->W19 diff contains ONLY the new five W19 files.
 # Keep the W18->W19 diff strict in source_scope_preflight, but include the
 # frozen prior W18-owned paths when invoking the SAME trusted #532 packager.
 original_load=m.load_w12
 def load_w12_with_full_prior_owned_inventory():
  m.W18=historical_own_W18_files|FILES
  try:return original_load()
  finally:m.W18=FILES
 m.load_w12=load_w12_with_full_prior_owned_inventory
 m.EXACT_SOURCE_BLOBS[FIXTURE]=FIXTURE_BLOB
 m.EXACT_SOURCE_BLOBS[CORE]=CORE_BLOB
 m.EXACT_SOURCE_BLOBS[HOST]=HOST_BLOB
 require(m.GAME==GAME and m.SAVE==SAVE,
         "frozen original #510/#556 source gate misconfigured")
 return m
def source_native_fixture_generation_witness(m,files=None):
 if files is None:files=m.owner_original()
 test=files[FIXTURE].decode("utf-8")
 core=files[CORE].decode("utf-8")
 require(
  m.git("rev-parse",SELECTED+":"+FIXTURE).decode().strip()==FIXTURE_BLOB and
  m.git("rev-parse",SELECTED+":"+CORE).decode().strip()==CORE_BLOB and
  m.git("rev-parse",SELECTED+":"+HOST).decode().strip()==HOST_BLOB,
  "owner-selected R3 original exact Git native fixture/core/host SHA changed")
 names=re.findall(r"\[UnityTest\]\s*public\s+IEnumerator\s+(\w+)\s*\(",test)
 require(tuple(names)==METHODS,
         "selected #642 original five native NUnit tests do not match pinned fixture")
 start=test.index("public IEnumerator ColdRestart_RestoresRealRigidbodyAndConsumedIds()")
 end=test.index("public IEnumerator WrongGeneration_CannotPartiallyPublishRealPhysicalWorld()")
 restart=test[start:end]
 checks=(
  "MakeFixture(41,4,true,false)",
  "Assert.IsTrue(original.TrySaveBytes(out byte[] saved))",
  "Assert.IsTrue(restored.TryRestoreBytes(saved,41,4))",
  "Assert.AreEqual(4,restored.Generation)",
 )
 require(all(s in restart for s in checks),
         "original #642 ColdRestart fixture changed; re-evaluate test expected generation")
 require("long next=newest+1;" in core and
         "new RunSnapshot(snap.RunId,next,snap.Parts)" in core,
         "owner R3 Save generation semantics changed; do not assume stale test witness")
 # Original untouched core makes Save generation 5 when current source is gen4.
 # Restoring its exact Save bytes with no further changed state hydrates gen5;
 # original selected test asserts gen4, so native fixture test needs owner fix.
 return {"source_status":"CONFIRMED_NATIVE_TEST_EXPECTATION_MISMATCH_FROM_ORIGINAL_C_SHARP",
         "test":"ColdRestart_RestoresRealRigidbodyAndConsumedIds",
         "original_snapshot_generation":4,
         "actual_generation_encoded_by_TrySave":5,
         "original_test_expected_restored_Generation":4,
         "real_Unity_observed":"NOT_RUN",
         "required_action":"B01-23 new versioned owner fixture and separate independent QA/G0"}
def main():
 m=adapt()
 # Do not change original runner semantics. Actual Unity native acceptance remains
 # the genuine -runTests/Editor.log/XML host gate of existing W18 OWN runner.
 m.source_scope_preflight()
 selected=m.owner_original()
 bad=source_native_fixture_generation_witness(m,selected)
 print("B01_60_W19_SOLE_R3_642_SELECTED_GIT_SHA",SELECTED)
 print("B01_60_W19_R3_644_ALT_HOLD_NO_SOURCE_MIX",R3_ALT)
 print("B01_60_W19_ORIGINAL_R3_NATIVE_FIXTURE_COLD_RESTART_GEN4_EXPECTS_GEN5_HOLD")
 print("B01_60_W19_UNITY_EDITOR_NOT_OBSERVED_BY_SOURCE_WITNESS")
 return m.main()
if __name__=="__main__":
 try:sys.exit(main())
 except Exception as e:
  print("B01_60_W19_ORIGINAL_SOURCE_QA_FAIL",repr(e),file=sys.stderr)
  sys.exit(2)
