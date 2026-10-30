#!/usr/bin/env python3
"""Actual original owner-selected #642 Git source and physical native-project QA.

This invokes existing B01-60 W18 hardened original source stage / rollback,
NOT Unity. Test that the original R3 ColdRestart NUnit expectation contradicts
unchanged original R3 Save semantics. Never patch or import #644 alternative.
"""
from __future__ import annotations
import importlib.util
import json
from pathlib import Path
import sys
import tempfile
ROOT=Path(__file__).resolve().parents[4]
RUN=ROOT/"Tools/B01/B01-60/P17W19Selected642Native/run.py"
def must(b,msg):
 if not b:raise AssertionError(msg)
def entry():
 spec=importlib.util.spec_from_file_location("b0160_w19_native_selected_642",RUN)
 must(spec is not None and spec.loader is not None,"own selected native adapter missing")
 t=importlib.util.module_from_spec(spec);spec.loader.exec_module(t)
 return t
def main():
 w=entry();m=w.adapt()
 source=m.owner_original()
 witness=w.source_native_fixture_generation_witness(m,source)
 must(witness["actual_generation_encoded_by_TrySave"]==5 and
      witness["original_test_expected_restored_Generation"]==4,
      "the original #642 C# native fixture mismatch has changed")
 must(w.CORE in source and w.HOST in source and w.FIXTURE in source,
      "missing original #642 core/host/native NUnit source")
 must(len(source)>=57,"unexpected selected #642 owner Git files")
 must(not any(path.endswith("HostSlotLease.cs") or
              path.endswith("CheckpointFence.cs") for path in source),
      "unselected #644 or #604 foreign source would be mixed")
 old=m.load_w12()
 source_projects=0
 rejected=0
 identities=set()
 with tempfile.TemporaryDirectory(prefix="b01-60-w19-native642-physical-") as tmp:
  root=Path(tmp)
  token="a11a11a11a11a11a"
  for case in w.METHODS:
   files,company,product=m.isolated_case_files(source,case,token,old)
   must(product not in identities and b"RoboRougelike" not in product.encode(),
        "selected case product identity collision")
   identities.add(product)
   archive=m.source_archive(old,files,case)
   p=root/("selected-"+str(len(identities)))
   old.stage(archive,files,p)
   must(m.validate_project_source(p,files,old),
        "selected source project preflight rejected own exact Git bytes")
   must((p/m.QA_PROBE).is_file() and
        (p/(m.QA_PROBE+".meta")).is_file(),
        "own QA-only checkpoint clean-private guard not physically staged")
   old.rollback(archive,files,p)
   must(not p.exists(),"unsafe physical selected project rollback")
   source_projects+=1
  must(len(identities)==5 and source_projects==5,
       "selected original R3 #642 case count no longer five")
  own,company,product=m.isolated_case_files(source,w.METHODS[0],token,old)
  tests=(
   ("frozen #510 original ledger drift",
    "Assets/B0123SecureReattachV2/Runtime/AtomicDockLedger.cs","overwrite"),
   ("frozen #556 sole codec mutation",
    "Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs","overwrite"),
   ("selected #642 original real host changed",w.HOST,"overwrite"),
   ("selected #642 original stale-world core changed",w.CORE,"overwrite"),
   ("selected #642 original NUnit fixture altered",w.FIXTURE,"overwrite"),
   ("own pre-case clean-private probe deleted",m.QA_PROBE,"delete"),
   ("foreign #644 HostSlotLease injected",
    "Assets/B0123UnitySaveBridge001/Core/HostSlotLease.cs","insert"),
   ("foreign empty selected Assets folder",
    "Assets/B0123UnitySaveBridge001/ForeignEmptyFolder","emptydir"),
   ("unexpected private project product reversion",m.PS,"overwrite"),
   ("changed exact Unity ProjectVersion",
    "ProjectSettings/ProjectVersion.txt","overwrite"),
  )
  for idx,(label,path,action) in enumerate(tests):
   archive=m.source_archive(old,own,"source-adversarial")
   p=root/("tampered-"+str(idx))
   old.stage(archive,own,p)
   q=p/path
   if action=="overwrite":q.write_bytes(b"malicious owner/source/ProjectSettings drift\n")
   elif action=="delete":q.unlink()
   elif action=="insert":q.write_bytes(b"// NOT the selected #642 original R3 source\n")
   elif action=="emptydir":q.mkdir()
   else:raise AssertionError("unknown adversarial operation")
   try:m.validate_project_source(p,own,old)
   except (AssertionError,ValueError) as e:
    must("NATIVE_SOURCE_CHANGED" in str(e) or
         "changed injected isolated Unity PlayerSettings" in str(e),
         "wrong reject type: "+label+": "+repr(e))
    rejected+=1
   else:raise AssertionError("FAIL_OPEN accepted selected #642 altered source "+label)
   must(not (p/"native-verdict.json").exists(),
        "fake G1 native receipt created on adulterated original R3 source")
  # The test source's incorrect expected generation must remain an independently
  # verified RELEASE_HOLD, not silently corrected in our QA copy.
  forged=dict(source)
  forged[w.FIXTURE]=forged[w.FIXTURE].replace(
    b"Assert.AreEqual(4,restored.Generation)",
    b"Assert.AreEqual(5,restored.Generation)",1)
  try:w.source_native_fixture_generation_witness(m,forged)
  except AssertionError:rejected+=1
  else:raise AssertionError("original frozen native test modified without new owner SHA")
 must(rejected==11 and source_projects==5,
      "expected five projects and eleven physical/source tamper refusals")
 print("B01_60_W19_OWNER_SELECTED_R3_ONLY",w.SELECTED)
 print("B01_60_W19_ALT_R3_644_NEVER_INSTALLED",w.R3_ALT)
 print("B01_60_W19_ORIGINAL_R3_642_SOURCE_PROJECTS_REAL_STAGE_ROLLBACK_PASS",source_projects)
 print("B01_60_W19_ORIGINAL_R3_642_SOURCE_ADVERSARIAL_DENIALS_PASS",rejected)
 print("B01_60_W19_ORIGINAL_NATIVE_FIXTURE_GEN4_EXPECTS_GENERATED_GEN5_SOURCE_P0_WITNESS")
 print("B01_60_W19_NATIVE_UNITY_PHYSX_G1_NOT_RUN_NO_REAL_XML_OR_EDITORLOG")
 out=ROOT/"qa-w19-native642-source-only-evidence"
 must(not out.exists(),"refuse overwriting original QA receipt")
 out.mkdir()
 (out/"receipt.json").write_text(json.dumps({
  "package":"B01-60-P17W19-SELECTED642-NATIVE-RECEIVER",
  "source_only":True,"owner":"B01-60","selected_r3":w.SELECTED,
  "unselected_r3_alt_hold":w.R3_ALT,
  "unselected_604_alt_hold":w.LEGACY_ALT,
  "frozen_game":w.GAME,"frozen_save":w.SAVE,"frozen_r2":w.R2_FROZEN,
  "selected_r3_source_and_settings_paths":len(source),
  "selected_original_native_methods_authored":list(w.METHODS),
  "selected_native_private_projects_physical_stage_and_rollback":source_projects,
  "source_adversarial_failclosed":rejected,
  "source_native_fixture_bug":witness,
  "actual_native_test_count":0,
  "real_editor_log":"NOT_CREATED","real_nunit_xml":"NOT_CREATED",
  "Unity6000_3_14f1":"NOT_RUN","PhysX":"NOT_RUN","G1":"NOT_RUN",
  "selected_R3_G0":"PENDING","G2":"HOLD","codex_ack":"NONE",
  "integrated":False,"released":False,
 },indent=2,sort_keys=True)+"\n")
 return 0
if __name__=="__main__":
 try:sys.exit(main())
 except Exception as ex:
  print("B01_60_W19_SOURCE_GATE_FAIL",repr(ex),file=sys.stderr)
  sys.exit(2)
