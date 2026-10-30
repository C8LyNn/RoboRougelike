#!/usr/bin/env python3
"""B01-60 P17/W23: corrected owner #732 original R5 FULL SEVEN native runner.

Reuses exact frozen original W22 #718 harness and W12 hardened staging, Unity
process, NUnit/Editor.log validators. Only owner #732 corrected selected R5
source is staged. No competing #693/#719 code, no frozen-source edits.
No actual native claim until real Unity ran and XML/logs exist.
"""
import argparse
import json
import os
from pathlib import Path
import re
import secrets
import subprocess
import sys
import tempfile
import importlib.util

ROOT=Path(__file__).resolve().parents[4]
W22="481bfb1cf2804d94fb49d7e6a57134e96c535b86"
R5="cc7f73541517a18f4479bb155534bd4832d9985e"
FIXED="0bee4ce9928354433ec719c45a8186378361d46e"
FIXTURE_BLOB="d096f65eeb37393204a35445dd87cbdffa642ce0"
FILE="Assets/B0123UnitySaveBridge001/Tests/IsolatedSaveWorldNativePlayModeTests.cs"
W22_RUNNER=ROOT/"Tools/B01/B01-60/P17W22R5PartialNative/r5_native.py"
CORRECTION_FILES={
 ".github/workflows/b01-23-p17w23-r5-coldrestart-fixture.yml",
 FILE,
 "Tools/B01/B01-23/P17W23R5ColdRestartFixture001/README.md",
 "Tools/B01/B01-23/P17W23R5ColdRestartFixture001/test_fixture.py",
}
NATIVE_CONSENT="B0160 R5 FIXED7 G0 REVIEWED NATIVE"
FULL_METHOD="ColdRestart_RestoresRealRigidbodyAndConsumedIds"

def require(ok,reason):
 if not ok:raise AssertionError(reason)

def w22():
 require(W22_RUNNER.is_file(),"Original W22 #718 runner missing")
 spec=importlib.util.spec_from_file_location("b0160_w22_original_R5_native",W22_RUNNER)
 require(spec is not None and spec.loader is not None,
         "Original W22 runner not importable")
 m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
 require(m.R5==R5 and len(m.METHODS)==7 and
         m.HOLD_METHOD==FULL_METHOD and m.FIXTURE==FILE,
         "Original W22 selected native runner source contract changed")
 require(m.git("rev-parse",W22+":Tools/B01/B01-60/P17W22R5PartialNative/r5_native.py")==
         "40ca11d886fc567a5a79f6abfeabdf2b37f4c36e",
         "Previous W22 native source Git blob changed")
 require(m.git("merge-base",W22,"HEAD")==W22,
         "Current QA must descend exact frozen W22 PR #718")
 return m

def original_corrected_source(n,m):
 require(n.git("cat-file","-t",FIXED)=="commit",
         "Corrected original B01-23 #732 SHA not in local Git history: no automatic fetch")
 require(n.git("merge-base",R5,FIXED)==R5,
         "Owner-corrected R5 not descendant of original selected #690")
 change=set(n.git("diff","--name-only",R5,FIXED).splitlines())
 require(change==CORRECTION_FILES,
         "Unexpected correction scope; refusing ALT source or producer code drift")
 for path,blob in n.BLOBS.items():
  target=FIXTURE_BLOB if path==FILE else blob
  require(n.git("rev-parse",FIXED+":"+path)==target,
          "Corrected selected original R5 Git source changed: "+path)
 require(n.git("rev-parse",R5+":"+FILE)==n.BLOBS[FILE],
         "Frozen original R5 fixture must not be overwritten")
 selected={}
 for p in n.git("ls-tree","-r","--name-only",FIXED).splitlines():
  if p in n.OTHER or any(p==prefix+".meta" or p.startswith(prefix+"/")
                          for prefix in n.SCOPE):
   selected[p]=subprocess.check_output(["git","-C",str(ROOT),"show",FIXED+":"+p],
                                        stderr=subprocess.PIPE)
 require(len(selected)>35 and selected[FILE], "Corrected original Unity source closure incomplete")
 names=re.findall(rb"\[UnityTest\]\s*public\s+IEnumerator\s+(\w+)\s*\(",selected[FILE])
 require(tuple(x.decode() for x in names)==n.METHODS,
         "Corrected R5 native fixture must preserve exactly seven original test cases")
 fixture=selected[FILE].decode()
 a=fixture.index("public IEnumerator "+FULL_METHOD+"()")
 b=fixture.index("public IEnumerator WrongGeneration_",a)
 cold=fixture[a:b]
 checks=("MakeFixture(41,4,true,false)",
         "Assert.IsTrue(original.TrySaveBytes(out byte[] saved))",
         "Assert.AreEqual(5,decoded.Generation)",
         "TryRestoreBytes(saved,41,5)",
         "Assert.AreEqual(5,restored.Generation)")
 require(all(x in cold for x in checks),
         "Corrected original R5 ColdRestart source does not prove g4→g5")
 require("Assert.AreEqual(4,restored.Generation)" not in cold,
         "Frozen broken generation4 expectation remained")
 core=selected[n.CORE].decode()
 require("long next=newest+1;" in core and
         "new RunSnapshot(snap.RunId,next,snap.Parts)" in core,
         "Original selected Save generation semantics changed")
 require(n.VERSION if hasattr(n,"VERSION") else True, "Version control missing")
 require("6000.3.14f1" in selected["ProjectSettings/ProjectVersion.txt"].decode(),
         "Exact Unity 6000.3.14f1 required")
 require(n.runner().guid_check(selected)>=34,"Original R5 GUID closure broken")
 return selected

def run(argv=None):
 parser=argparse.ArgumentParser()
 parser.add_argument("--mode",choices=("preflight","native"),default="preflight")
 parser.add_argument("--unity",default="")
 parser.add_argument("--scratch-parent",
                     default=os.environ.get("TEMP") or tempfile.gettempdir())
 parser.add_argument("--operator-consent",default="")
 args=parser.parse_args(argv)
 n=w22()
 m=n.runner()
 selected=original_corrected_source(n,m)
 if args.mode=="native":
  require(os.name=="nt","Native run authorized only on Windows")
  require(args.operator_consent==NATIVE_CONSENT,
          "Independent manual exact G0-reviewed operator consent missing")
  unity=Path(args.unity)
  require(unity.is_file() and re.search(r"[/\\]6000\.3\.14f1[/\\]Editor[/\\]Unity\.exe$",
             str(unity),re.I),"Real licensed Unity Hub6000.3.14f1 Editor missing")
 else:unity=None
 out=n.safe_root(args.scratch_parent)
 receipt={
  "package":"B01-60-P17W23-R5-COLDRESTART-FIXED-SEVEN-NATIVE",
  "source":FIXED,"parent_selected_R5":R5,"parent_W22_QA":W22,
  "source_G0":"OWNER_CORRECTED_732_PENDING_INDEPENDENT_G0",
  "native_G1":"NOT_RUN","full_R5_G1":"NOT_RUN",
  "coldrestart_generation":"g4->g5 original #556",
  "original_case_methods":n.METHODS,
  "operator_g0_review_claim":args.mode=="native",
  "codex_ACK":"NONE","integrated":False,"released":False,
  "evidence_is_self_hashed_not_attested":True
 }
 receipt_file=out/"P23-fixed7-receipt.json"
 receipt_file.write_text(json.dumps(receipt,sort_keys=True,indent=2)+"\n")
 try:
  nonce=secrets.token_hex(6)
  guid=m.guid_check(selected)
  for i,method in enumerate(n.METHODS):
   # Exact W22 original native project builders; seven distinct private products.
   files,product=n.private_source(selected,i,nonce,m)
   require(m.guid_check(files)==guid+1,
           "Missing original private checkpoint probe GUID")
   archive=m.zip_data(files,{
      "original_source_sha":FIXED,"native_unity":"NOT_RUN",
      "source_only_preflight":True,"codex_ack":False,"integrated":False,
      "native_meta_guids":m.guid_check(files),
      "file_sha256":{k:n.blake(v) for k,v in sorted(files.items())}})
   project=out/("P"+str(i))
   m.stage(archive,files,project)
   require((project/n.PS).read_bytes()==files[n.PS],
           "Original private Unity settings mutated at physical stage")
   if args.mode=="native":
    n.run_editor(unity,project,out,"guard"+str(i),n.PROBE,files,m)
    n.run_editor(unity,project,out,"r5case"+str(i),n.SUITE+method,files,m)
   else:m.rollback(archive,files,project)
  if args.mode=="native":
   receipt["native_G1"]="SEVEN_ORIGINAL_R5_NATIVE_CASES_COMPLETED_REQUIRES_INDEPENDENT_REVIEW"
   receipt["full_R5_G1"]="HOLD_PENDING_INDEPENDENT_SOURCE_G0_AND_OWNER_ATTESTATION"
   print("B0160_W23_FIXED7_ORIGINAL_UNITY_EXECUTED_REQUIRES_INDEPENDENT_G0_AND_LOG_REVIEW")
  else:
   receipt["source_preflight"]="PASS_7_PRIVATE_PROJECTS_STAGE_ROLLBACK_NO_UNITY"
   print("B0160_W23_FIXED7_CORRECTED_OWNER_GIT_SHA="+FIXED)
   print("B0160_W23_FIXED7_SEVEN_UNMODIFIED_ORIGINAL_NUNIT_NAMES_PLUS_G5_EXPECTATION")
   print("B0160_W23_FIXED7_SEVEN_PRIVATE_SCRATCH_STAGE_ROLLBACK_PASS")
   print("B0160_W23_FIXED7_NATIVE_UNITY_G1_NOT_RUN")
  receipt_file.write_text(json.dumps(receipt,sort_keys=True,indent=2)+"\n")
  (out/"P23-fixed7-sha256-manifest.json").write_text(json.dumps(
      n.manifest_files(out,receipt),sort_keys=True,indent=2)+"\n")
  print("B0160_W23_FIXED7_LOCAL_EVIDENCE_NO_UPLOAD="+str(out))
  return 0
 except Exception as exc:
  receipt["source_or_native_error"]=str(exc)
  receipt_file.write_text(json.dumps(receipt,sort_keys=True,indent=2)+"\n")
  print("B0160_W23_FIXED7_FAIL_CLOSED="+str(exc),file=sys.stderr)
  print("B0160_W23_FIXED7_LOCAL_EVIDENCE_NO_UPLOAD="+str(out))
  return 4 if args.mode=="native" else 2

def main(argv=None):
 try:return run(argv)
 except Exception as e:
  print("B0160_W23_FIXED7_HOLD="+repr(e),file=sys.stderr)
  return 3 if argv and "--mode" in argv and "native" in argv else 2
if __name__=="__main__":sys.exit(main(sys.argv[1:]))
