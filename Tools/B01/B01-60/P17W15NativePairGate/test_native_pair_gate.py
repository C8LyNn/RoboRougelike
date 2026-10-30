#!/usr/bin/env python3
"""B01-60 W15: execute the exact frozen W14 main() to prove EditMode was unvalidated.

Every fake process and NUnit XML is a deliberate adversarial QA stub.
NO Unity, PhysX, legitimate activation, genuine Editor.log or native PASS is claimed.
"""
from __future__ import annotations
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import types
import xml.etree.ElementTree as ET

ROOT=Path(__file__).resolve().parents[4]
RUNNER="Tools/B01/B01-60/P17W12NativeSecureV2QA/runner.py"
PREVIOUS="4303e62b0339c6cdc828478a06b6e2c9c79e6e13"
PREVIOUS_BLOB="919d8db76eb38d55e5b900f44692e23806f6900b"
SELECTED="d94400d95d13db7f27e1533df05c9292d5380431"
SAVE="33094f154c7b91119b36d58aab285e6e83c5de57"
def git(*args):
 return subprocess.check_output(["git","-C",str(ROOT),*args],stderr=subprocess.PIPE)
def insist(ok,msg):
 if not ok:raise AssertionError(msg)
def load(name,raw):
 module=types.ModuleType(name)
 module.__file__=str(ROOT/RUNNER)
 exec(compile(raw,str(ROOT/RUNNER),"exec"),vars(module))
 return module
def synthetic_xml(path,mod,platform,scenario):
 expected=sorted(mod.expected_independent_native_cases(platform))
 count=len(expected)
 root=ET.Element("test-run",result="Passed",passed=str(count),failed="0")
 suite=ET.SubElement(root,"test-suite",name="QA-NOT-UNITY",type="Assembly")
 cases=expected
 if platform=="EditMode":
  if scenario=="edit_failed":root.attrib.update(result="Failed",passed="0",failed="2")
  if scenario=="edit_missing_case":cases=expected[:-1]
  if scenario=="edit_duplicate":cases=expected+[expected[0]]
  if scenario=="edit_skipped":pass
 for idx,fullname in enumerate(cases):
  result="Skipped" if platform=="EditMode" and scenario=="edit_skipped" and idx==0 else "Passed"
  ET.SubElement(suite,"test-case",id="synthetic-"+str(idx),
                fullname=fullname,result=result)
 ET.ElementTree(root).write(path,encoding="utf-8",xml_declaration=True)
def run_real_main_with_subprocess_stub(mod,path,scenario):
 """Only SOURCE control flow is real, subprocess is NOT Unity and is labeled stub."""
 out=path/("old-" if mod.__name__=="frozen_original_W14" else "new-")+scenario
 unity=path/"SYNTHETIC_NOT_UNITY_EXECUTABLE"
 unity.touch()
 # Override pure staging to avoid copying source; source-control logic is executed
 # unmodified, and the original XML parser + Editor.log validation are real.
 mod.named_sources=lambda:{"Assets/STUB-ONLY":b"this is never a real game source"}
 mod.extra_test_assets=lambda:{}
 mod.guid_check=lambda items:0
 mod.zip_data=lambda items,receipt:b"SYNTHETIC_QA_NOT_REAL_UNITY_ZIP"
 mod.verify=lambda archive,items:None
 mod.negatives=lambda archive,items:10
 mod.stage=lambda archive,items,root:root.mkdir(parents=False)
 def stub_process(command,**kwargs):
  platform=command[command.index("-testPlatform")+1]
  xml=Path(command[command.index("-testResults")+1])
  log=Path(command[command.index("-logFile")+1])
  synthetic_xml(xml,mod,platform,scenario)
  if platform=="PlayMode" or scenario not in ("edit_missing_log",):
   if platform=="EditMode" and scenario=="edit_wrong_editor":
    log.write_text("Unity Editor 2022.3.0f1 - SYNTHETIC TEST LOG\n")
   else:
    log.write_text("Unity 6000.3.14f1 - SYNTHETIC QA STUB LOG, NOT A REAL ENGINE\n")
  return types.SimpleNamespace(returncode=0,stdout=b"STUB_NOT_UNITY_NO_PHYSX")
 mod.subprocess=types.SimpleNamespace(run=stub_process,PIPE=-1,STDOUT=-2)
 previous_argv=sys.argv
 sys.argv=[str(ROOT/RUNNER),"--mode","native","--unity",str(unity),"--out",str(out)]
 try:
  return_code=mod.main()
 finally:
  sys.argv=previous_argv
 verdict=out/"native-verdict.json"
 return return_code,(json.loads(verdict.read_text()) if verdict.exists() else None),out
def denied(fn,label):
 try:fn()
 except (AssertionError,ValueError,ET.ParseError,KeyError):
  return label
 raise AssertionError("W15 incorrectly issued a native G1 PASS: "+label)
def main():
 insist(git("merge-base",PREVIOUS,"HEAD").decode().strip()==PREVIOUS,
        "QA W15 must be stacked on exact frozen W14 #567")
 insist(git("merge-base",SELECTED,"HEAD").decode().strip()==SELECTED,
        "frozen #510 is not ancestor")
 insist(git("rev-parse",PREVIOUS+":"+RUNNER).decode().strip()==PREVIOUS_BLOB,
        "original W14 QA runner Git blob changed")
 original=load("frozen_original_W14",git("show",PREVIOUS+":"+RUNNER))
 new=load("new_W15_fixed_G1_pair",(ROOT/RUNNER).read_bytes())
 cases=[
  "edit_missing_log",
  "edit_wrong_editor",
  "edit_failed",
  "edit_missing_case",
  "edit_duplicate",
  "edit_skipped",
 ]
 old_false_green=0
 new_failclosed=0
 dual_suite_positive=0
 observed=[]
 with tempfile.TemporaryDirectory(prefix="b0160-w15-python-controlflow-not-unity-") as tmp:
  root=Path(tmp)
  for case in cases:
   old_code,old_verdict,old_out=run_real_main_with_subprocess_stub(original,root,case)
   insist(old_code==0 and old_verdict is not None and
          old_verdict.get("G1")=="PASS" and
          set(old_verdict["results"])=={"PlayMode"},
          "old main did not reproduce only-PlayMode false G1: "+case)
   old_false_green+=1
   observed.append("OLD_SOURCE_FALSE_G1_SYNTHETIC_ONLY:"+case)
   try:
    new_code,new_verdict,new_out=run_real_main_with_subprocess_stub(new,root,case)
   except (AssertionError,ValueError,ET.ParseError,KeyError):
    # Fail-closed exception; any output remains under disposable QA-only temp root.
    new_failclosed+=1
   else:
    insist(new_code!=0 and new_verdict is None,
           "W15 accepted unsafe EditMode stub: "+case)
    new_failclosed+=1
  # Positive is also a stub, explicitly NOT a real Unity pass; verifies pair
  # branch flow only and would remain insufficient for G1 without genuine Editor.
  old_code,old_verdict,_=run_real_main_with_subprocess_stub(original,root,"both_valid_synthetic")
  new_code,new_verdict,_=run_real_main_with_subprocess_stub(new,root,"both_valid_synthetic")
  insist(old_code==0 and set(old_verdict["results"])=={"PlayMode"},
         "old original still must expose only last suite on fake positive")
  insist(new_code==0 and set(new_verdict["results"])=={"EditMode","PlayMode"},
         "new W15 native main does not validate both suite branches")
  insist(new_verdict["results"]["EditMode"]["verified_independent_cases"]==2 and
         new_verdict["results"]["PlayMode"]["verified_independent_cases"]==14,
         "new pair gate lacks 2+14 exact case identities")
  dual_suite_positive=1
 insist((old_false_green,new_failclosed,dual_suite_positive)==(6,6,1),
        "W15 expected 6 historical false-G1 +6 fail-closed +1 pair proof")
 print("B01_60_W15_ORIGINAL_FROZEN_W14_MAIN_FALSE_NATIVE_G1_REPRODUCED",old_false_green)
 print("B01_60_W15_FIXED_W15_BOTH_SUITES_FAIL_CLOSED",new_failclosed)
 print("B01_60_W15_FIXED_W15_BOTH_SUITES_SYNTHETIC_CONTROL_FLOW",dual_suite_positive)
 print("B01_60_W15_ALL_SUBPROCESS_XML_AND_LOG_SYNTHETIC_STUB_NOT_UNITY")
 print("B01_60_W15_GAMEPLAY_SHA",SELECTED,"SAVE_CONTRACT_SHA",SAVE)
 print("B01_60_W15_NATIVE_G1_NOT_RUN_NO_ACTIVATED_EDITOR_NO_PHYSX")
 out=ROOT/"qa-w15-independent-native-pair-proof"
 insist(not out.exists(),"do not overwrite existing QA evidence")
 out.mkdir()
 receipt={
  "test_kind":"ORIGINAL_REAL_PYTHON_MAIN_CONTROL_FLOW_WITH_SYNTHETIC_FAKE_UNITY_IO",
  "engine_exec":"NOT_RUN",
  "real_physx":"NOT_RUN",
  "actual_unity_nunit_xml":"NOT_CREATED",
  "original_editor_log":"NOT_CREATED",
  "frozen_original_w14_sha":PREVIOUS,
  "frozen_w14_runner_blob":PREVIOUS_BLOB,
  "selected_gameplay_source_sha":SELECTED,
  "only_selected_save_contract_sha":SAVE,
  "original_W14_main_false_native_G1_witnesses":old_false_green,
  "new_W15_main_failclosed_rejections":new_failclosed,
  "new_W15_two_suite_synthetic_positive":dual_suite_positive,
  "source_pr_sha":git("rev-parse","HEAD").decode().strip(),
  "G1":"NOT_RUN","G2":"HOLD","Codex_ACK":"NONE","merged":False,
  "scenarios":observed,
 }
 (out/"receipt.json").write_text(json.dumps(receipt,sort_keys=True,indent=2)+"\n")
 return 0
if __name__=="__main__":
 try:sys.exit(main())
 except Exception as e:
  print("B01_60_W15_QA_FAIL",repr(e),file=sys.stderr)
  sys.exit(2)
