#!/usr/bin/env python3
"""B01-60 W16: genuine frozen W15 source main() accepts tampered Unity project.

Execute OLD and NEW exact original Python main() on REAL throwaway filesystem
with an explicitly SYNTHETIC test subprocess. Not Unity / not PhysX / not G1.
"""
from __future__ import annotations
import importlib.util
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import xml.etree.ElementTree as ET
ROOT=Path(__file__).resolve().parents[4]
HERE=Path(__file__).resolve().parent
RUNNER="Tools/B01/B01-60/P17W12NativeSecureV2QA/runner.py"
W15_HELPER="Tools/B01/B01-60/P17W15NativePairGate/test_native_pair_gate.py"
PREV="95843dc3c5f1d899091749cf7208bf43858abc6a"
PREV_RUNNER_GIT_BLOB="27293bdbbb112c0fbe1256a9c9708e2e2289cc3b"
GAME="d94400d95d13db7f27e1533df05c9292d5380431"
SAVE="33094f154c7b91119b36d58aab285e6e83c5de57"
def git(*args):
 return subprocess.check_output(["git","-C",str(ROOT),*args],stderr=subprocess.PIPE)
def must(ok,msg):
 if not ok:raise AssertionError(msg)
def main():
 must(git("merge-base",PREV,"HEAD").decode().strip()==PREV,
      "W16 QA not stacked on exact W15 native gate frozen source")
 must(git("rev-parse",PREV+":"+RUNNER).decode().strip()==PREV_RUNNER_GIT_BLOB,
      "W15 original own runner Git source pin changed")
 must(git("merge-base",GAME,"HEAD").decode().strip()==GAME,
      "selected original game source missing")
 # #556 Save contract is on a DIFFERENT producer lineage, not a #510 ancestor.
 # Require exact immutable commit object via Git, without co-installing it.
 git("cat-file","-e",SAVE+"^{commit}")
 spec=importlib.util.spec_from_file_location("b0160_w15_source_test_helpers",ROOT/W15_HELPER)
 must(spec is not None and spec.loader is not None,"missing owned W15 helpers")
 helpers=importlib.util.module_from_spec(spec)
 spec.loader.exec_module(helpers)
 old=helpers.load("frozen_original_W15",git("show",PREV+":"+RUNNER))
 new=helpers.load("new_W16_source_provenance",(ROOT/RUNNER).read_bytes())
 must(not hasattr(old,"require_native_source_integrity"),
      "prior immutable native source was not genuinely unguarded")
 must(hasattr(new,"require_native_source_integrity"),
      "W16 source integrity check was not installed")
 scenarios=(
  "source_modified",
  "source_removed",
  "source_new_code",
  "source_new_empty",
  "source_new_asmdef",
  "source_manifest_modified",
  "source_version_modified",
  "source_meta_added",
 )
 original_false_success=0
 new_denials=0
 real_fs_integrity=0
 synthetic_positive=0
 with tempfile.TemporaryDirectory(prefix="b01-60-w16-real-IO-fake-editor-") as temp:
  root=Path(temp)
  for scenario in scenarios:
   # Execute the actual original W15 main() control flow, original-source
   # ZIP receipt, literal staged file I/O + intentionally injected filesystem
   # mutation from a fake process. Only the process/XML/log are synthetic.
   old_code,old_verdict,old_out=helpers.run_real_main_with_subprocess_stub(
       old,root,scenario)
   must(old_code==0 and old_verdict is not None and
        old_verdict.get("G1")=="PASS" and
        set(old_verdict["results"])=={"EditMode","PlayMode"},
        "old frozen W15 did not reproduce native source integrity false PASS")
   original_false_success+=1
   must((old_out/"native-verdict.json").is_file(),
        "expected real old false-success receipt not produced")
   # Every W16 denial occurs AFTER an actually mutated physical on-disk
   # disposable source file, directory, or selected dependency/version.
   try:
    new_code,new_verdict,new_out=helpers.run_real_main_with_subprocess_stub(
        new,root,scenario)
   except AssertionError as err:
    must("NATIVE_SOURCE_CHANGED" in str(err),
         "expected source integrity denial rather than unrelated error "+str(err))
    must(not (root/("new-"+scenario)/"native-verdict.json").exists(),
         "unsafe final G1 receipt created before rollback/denial")
    new_denials+=1
    real_fs_integrity+=1
   else:
    raise AssertionError("W16 incorrectly certified modified Unity source: "+scenario)
  # This final success is a NON-UNITY test of source provenance API only,
  # under a synthetic process with all unchanged byte-identical file inputs.
  result,receipt,_=helpers.run_real_main_with_subprocess_stub(
      new,root,"both_valid_synthetic")
  must(result==0 and receipt is not None and
       set(receipt["results"])=={"EditMode","PlayMode"},
       "W16 source gate rejected clean synthetic source")
  synthetic_positive+=1
 must((original_false_success,new_denials,real_fs_integrity,synthetic_positive)==
      (8,8,8,1),"expected W16 exact previous false-G1 x8 and fixed x8")
 print("B01_60_W16_FROZEN_W15_SOURCE_MUTATION_FALSE_NATIVE_G1_REPRODUCED",original_false_success)
 print("B01_60_W16_FIXED_W16_IMPORTED_ASSETS_SOURCE_SHA_GUARD_PASS",new_denials)
 print("B01_60_W16_REAL_DISPOSABLE_FILESYSTEM_MUTATIONS_EXECUTED",real_fs_integrity)
 print("B01_60_W16_CLEAN_SOURCE_SYNTHETIC_ONLY_POSITIVE",synthetic_positive)
 print("B01_60_W16_SUBPROCESS_XML_AND_LOG_ARE_FAKE_NOT_UNITY_NO_PHYSX")
 print("B01_60_W16_FROZEN_GAME_SHA",GAME,"FROZEN_SAVE_SHA",SAVE)
 print("B01_60_W16_NATIVE_G1_NOT_RUN_EDITOR_NOT_INSTALLED")
 out=ROOT/"qa-w16-native-source-integrity-evidence"
 must(not out.exists(),"W16 source QA evidence already exists")
 out.mkdir()
 evidence={
  "G1":"NOT_RUN","native_unity":"NO_EXECUTABLE_OR_ACTIVATION","physx":"NOT_RUN",
  "original_unity_xml":"NOT_CREATED","original_unity_EditorLog":"NOT_CREATED",
  "actual_test":"EXACT_FROZEN_RUNNER_MAIN_WITH_REAL_FILESYSTEM_AND_FAKE_NON_UNITY_PROCESS",
  "frozen_original_W15_runner_git_blob":PREV_RUNNER_GIT_BLOB,
  "frozen_old_qa_sha":PREV,
  "game_sha":GAME,"save_contract_sha":SAVE,
  "W15_original_source_false_native_G1_reproduced":original_false_success,
  "W16_native_import_source_drift_fail_closed":new_denials,
  "actual_disposable_physical_mutations":real_fs_integrity,
  "control_flow_clean_source_positive_synthetic_only":synthetic_positive,
  "test_cases":scenarios,"G2":"HOLD","codex_ack":False,"integrated":False
 }
 (out/"receipt.json").write_text(json.dumps(evidence,sort_keys=True,indent=2)+"\n")
 return 0
if __name__=="__main__":
 try:sys.exit(main())
 except Exception as e:
  print("B01_60_W16_SOURCE_PROOF_FAIL",repr(e),file=sys.stderr)
  sys.exit(2)
