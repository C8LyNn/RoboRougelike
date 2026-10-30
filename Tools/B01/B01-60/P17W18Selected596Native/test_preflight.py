#!/usr/bin/env python3
"""W18 executable ORIGINAL selected #596 physical staging/negative source QA.

Actual real filesystem, original selected Git bytes, existing native #532 safe
stage + byte checker. NO Unity Editor or PhysX is simulated or claimed.
"""
from __future__ import annotations
import importlib.util
import json
from pathlib import Path
import tempfile
import sys

ROOT=Path(__file__).resolve().parents[4]
SRC=ROOT/"Tools/B01/B01-60/P17W18Selected596Native/run.py"
def yes(condition,msg):
 if not condition:raise AssertionError(msg)
def main():
 spec=importlib.util.spec_from_file_location("b0160_w18_selected_native_entrypoint",SRC)
 yes(spec is not None and spec.loader is not None,"W18 original source missing")
 m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
 old=m.load_w12()
 source=m.owner_original()
 yes(source["Assets/B0123UnitySaveBridge001/Tests/IsolatedSaveWorldNativePlayModeTests.cs"].count(b"[UnityTest]")==4,
     "selected original four physical test cases missing")
 token="ca11abcdca11abcd"
 samples={}
 tests=0
 guards=0
 with tempfile.TemporaryDirectory(prefix="b0160-w18-physical-source-qa-") as tmp:
  root=Path(tmp)
  for case in m.METHODS:
   files,company,product=m.isolated_case_files(source,case,token,old)
   yes(product not in samples and "RoboRougelike" not in product,
       "W18 private test identity collides with game or another case")
   samples[product]=case
   yes(files[m.PS]!=source[m.PS] and files[m.QA_PROBE] and
       files[m.QA_PROBE+".meta"],"missing private test company/product/probe")
   yes(company.encode() in files[m.PS] and product.encode() in files[m.PS],
       "private PlayerSettings not saved in expected project")
   receipt={"original_game_sha":m.GAME,"only_save_sha":m.SAVE,
            "only_adapter_sha":m.ONLY,"case":case,"private_product":product,
            "engine":"NOT_RUN","integration":"NONE"}
   archive=m.source_archive(old,files,case)
   staged=root/("clean-"+str(len(samples)))
   old.stage(archive,files,staged)
   yes(m.validate_project_source(staged,files,old),"clean project refused")
   yes((staged/m.QA_PROBE).is_file(),"native isolated private guard missing")
   old.rollback(archive,files,staged)
   yes(not staged.exists(),"safe exact owned-only rollback failed")
   tests+=1
  yes(len(samples)==4 and tests==4,"not exactly four isolated selected tests")
  case=m.METHODS[0]
  base,company,product=m.isolated_case_files(source,case,token,old)
  checks=(
   ("source_byte_changed","Assets/B0123UnitySaveBridge001/Unity/IsolatedTwinRigSaveHost.cs",
       "overwrite"),
   ("frozen_game_changed","Assets/B0123SecureReattachV2/Runtime/AtomicDockLedger.cs",
       "overwrite"),
   ("frozen_save_changed","Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs",
       "overwrite"),
   ("original_source_deleted","Assets/B0123UnitySaveBridge001/Core/AtomicWorldSwap.cs",
       "delete"),
   ("foreign_cpp_or_script","Assets/B0123UnitySaveBridge001/Tests/UnownedOther.cs",
       "insert"),
   ("missing_preflight_probe",m.QA_PROBE,"delete"),
   ("foreign_empty_assets_folder","Assets/B0123UnitySaveBridge001/UnexpectedEmpty","emptydir"),
   ("private_product_tampered",m.PS,"overwrite"),
   ("project_version_tampered","ProjectSettings/ProjectVersion.txt","overwrite"),
   ("manifest_tampered","Packages/manifest.json","overwrite"),
  )
  for i,(label,name,action) in enumerate(checks):
   project=root/("red-"+str(i))
   archive=m.source_archive(old,base,"adversarial-only")
   old.stage(archive,base,project)
   place=project/name
   if action=="overwrite":place.write_bytes(b"source integrity changed after Unity import\n")
   elif action=="insert":place.write_bytes(b"// non-selected QA mutation: NOT another adapter\n")
   elif action=="delete":place.unlink()
   elif action=="emptydir":place.mkdir()
   else:raise AssertionError("unknown source mutation")
   try:
    m.validate_project_source(project,base,old)
   except (AssertionError,ValueError) as exc:
    yes("NATIVE_SOURCE_CHANGED" in str(exc) or
        "changed injected isolated Unity PlayerSettings" in str(exc),
        "unexpected refusal reason "+label+": "+repr(exc))
    yes(not (project/"native-verdict.json").exists(),
        "false native success published after "+label)
    guards+=1
   else:raise AssertionError("W18 accepted tampered selected Unity source: "+label)
  yes(guards==10,"expected exactly ten physical source mutation refusals")
 print("B01_60_W18_EXACT_FROZEN_510_556_SELECTED_596_ONLY_SOURCE_PASS")
 print("B01_60_W18_REAL_FILESYSTEM_ISOLATED_UNITY_PROJECTS_STAGED_ROLLED_BACK",tests)
 print("B01_60_W18_REAL_FILESYSTEM_SOURCE_AND_PROJECT_SETTINGS_MUTATION_REFUSALS",guards)
 print("B01_60_W18_DISJOINT_PRIVATE_APP_PRODUCT_IDENTITIES",len(samples))
 print("B01_60_W18_REAL_EDITOR_PHYSX_NOT_RUN_NO_FAKE_NATIVE_XML_OR_EDITORLOG")
 out=ROOT/"qa-w18-selected596-source-check-evidence"
 yes(not out.exists(),"refuse existing QA output")
 out.mkdir()
 record={"owner":"B01-60","role":"SOURCE_QA_ONLY","W18_selected":m.ONLY,
         "game":m.GAME,"save":m.SAVE,"alternative_excluded":m.ALT,
         "projects_physically_staged_and_safe_rolled_back":tests,
         "physical_adversarial_failclosed":guards,
         "private_product_names":[x for x in samples],
         "native_unity":"NOT_RUN","physx":"NOT_RUN",
         "actual_Editor_log":"NOT_CREATED","native_nunit_xml":"NOT_CREATED",
         "G1":"NOT_RUN","G2":"HOLD","canonical_game_save":"HOLD","merged":False}
 (out/"receipt.json").write_text(json.dumps(record,indent=2,sort_keys=True)+"\n")
 return 0
if __name__=="__main__":
 try:sys.exit(main())
 except Exception as exc:
  print("B01_60_W18_SOURCE_TEST_FAIL",repr(exc),file=sys.stderr)
  sys.exit(2)
