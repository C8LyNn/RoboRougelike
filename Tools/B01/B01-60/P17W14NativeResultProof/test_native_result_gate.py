#!/usr/bin/env python3
"""B01-60 W14: execute exact old original native XML validator RED, new validator GREEN.

All XML fixtures here are SYNTHETIC ADVERSARIAL INPUT, never real Unity evidence.
No Unity or PhysX is executed. A PASS means QA result parser is fail-closed, not G1.
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
OLD="ed0234d8a3b1b3bc85f3d0a07bbe44b065c96508"
OLD_GIT_BLOB="269aed3ede17bfe614455e6e0edd446f60ae3b0f"
OWNER="d94400d95d13db7f27e1533df05c9292d5380431"
def git(*args):
 return subprocess.check_output(["git","-C",str(ROOT),*args],stderr=subprocess.PIPE)
def check(ok,msg):
 if not ok: raise AssertionError(msg)
def as_module(name,bytes_):
 mod=types.ModuleType(name)
 mod.__file__=str(ROOT/RUNNER)
 exec(compile(bytes_,str(ROOT/RUNNER),"exec"),vars(mod))
 return mod
def xml_file(path,platform,identities=None,result="Passed",passed=None,failed=0,duplicate=False,case_results=None):
 assert platform in ("EditMode","PlayMode")
 minimum={"EditMode":2,"PlayMode":14}[platform]
 root=ET.Element("test-run",result=result,passed=str(minimum if passed is None else passed),failed=str(failed))
 if identities is not None:
  suite=ET.SubElement(root,"test-suite",type="Assembly",name="QA")
  for idx,x in enumerate(identities):
   ET.SubElement(suite,"test-case",fullname=x,result=(case_results or {}).get(x,"Passed"),id=str(idx))
  if duplicate:
   ET.SubElement(suite,"test-case",fullname=identities[0],result="Passed",id="REPEAT")
 ET.ElementTree(root).write(path,encoding="utf-8",xml_declaration=True)
 return path
def denied(fn,desc):
 try:fn()
 except (AssertionError,ValueError,ET.ParseError):
  return desc
 raise AssertionError("W14 false G1 acceptance: "+desc)
def main():
 check(git("merge-base",OLD,"HEAD").decode().strip()==OLD,"W14 not on frozen W13 receiver")
 check(git("merge-base",OWNER,"HEAD").decode().strip()==OWNER,"not on selected #510")
 old_blob=git("rev-parse",OLD+":"+RUNNER).decode().strip()
 check(old_blob==OLD_GIT_BLOB,"frozen old validator Git blob changed")
 old=as_module("frozen_w13_native_result_parser",git("show",OLD+":"+RUNNER))
 new=as_module("new_w14_native_result_parser",(ROOT/RUNNER).read_bytes())
 check(old.parse_xml.__code__.co_code != new.parse_xml.__code__.co_code,
       "new case identity gate was not modified")
 witness=0
 neg=0
 pos=0
 events=[]
 with tempfile.TemporaryDirectory(prefix="b01-60-w14-real-original-source-") as tmp:
  path=Path(tmp)/"NUnit.xml"
  # The ACTUAL original W13 validator accepts fabricated bare counters.
  for platform,n in (("EditMode",2),("PlayMode",14)):
   xml_file(path,platform)
   accepted=old.parse_xml(path,n) # actually executes old Git blob logic
   check(accepted["result"]=="Passed" and accepted["passed"]==n,
         "original false-GREEN cannot be reproduced")
   witness+=1
   events.append("historical original W13 accepted empty synthetic "+platform+" summary")
   denied(lambda:new.parse_xml(path,n),platform+" empty synthetic NUnit counter")
   neg+=1
  for platform,n in (("EditMode",2),("PlayMode",14)):
   original=sorted(new.expected_independent_native_cases(platform))
   check(len(original)==n,"case catalog wrong "+platform)
   # Positives contain every exact original independent NUnit case identifier.
   xml_file(path,platform,original,passed=n)
   parsed=new.parse_xml(path,n)
   check(parsed["verified_independent_cases"]==n and parsed["failed"]==0,
         "case-positive gate failed")
   pos+=1
   cases=[
    ("missing_last",original[:-1],None,False,None),
    ("wrong_class",[x.replace("NativeQA.","FakeQA.") for x in original],None,False,None),
    ("duplicate_first",original,None,True,None),
    ("one_case_skipped",original,None,False,{original[0]:"Skipped"}),
    ("one_case_failed_aggregate_forged",original,None,False,{original[0]:"Failed"}),
    ("one_name_typo",[x+"-typo" if i==0 else x for i,x in enumerate(original)],None,False,None),
    ("one_case_missing_plus_high_counter",original[:-1],n+1200,False,None),
   ]
   for label,identities,pas,duplicate,results in cases:
    xml_file(path,platform,identities,passed=pas,duplicate=duplicate,case_results=results)
    denied(lambda:new.parse_xml(path,n),platform+" "+label)
    neg+=1
   for label,pas,failed,result in (
     ("root_failed",n,1,"Failed"),("summary_insufficient",n-1,0,"Passed"),
     ("summary_not_complete",n,0,"Inconclusive")
   ):
    xml_file(path,platform,original,result=result,passed=pas,failed=failed)
    denied(lambda:new.parse_xml(path,n),platform+" "+label)
    neg+=1
   events.append("W14 "+platform+" verified "+str(n)+" exact QA cases and 10 hostile variants")
  logfile=Path(tmp)/"Editor.log"
  denied(lambda:new.require_editor_version_log(logfile),"missing real Editor.log")
  neg+=1
  logfile.write_text("")
  denied(lambda:new.require_editor_version_log(logfile),"empty real Editor.log")
  neg+=1
  logfile.write_text("Initialize engine version: 2021.3.99f1\n")
  denied(lambda:new.require_editor_version_log(logfile),"wrong native Unity version")
  neg+=1
  logfile.write_text("Initialize engine version: 6000.3.14f1 (original Unity Editor output fixture)\n")
  check(new.require_editor_version_log(logfile),"native-version marker positive fixture denied")
  pos+=1
  path.write_text("<test-run")
  denied(lambda:new.parse_xml(path,2),"truncated malformed XML")
  neg+=1
 check(witness==2 and neg==26 and pos==3,
       "expected 2 original RED, 26 new refusals and 3 positives")
 print("B01_60_W14_GENUINE_ORIGINAL_W13_XML_SUMMARY_FALSE_GREEN_RED",witness)
 print("B01_60_W14_CORRECTED_NATIVE_G1_PARSER_NEGATIVE_PASS",neg)
 print("B01_60_W14_CORRECTED_NATIVE_G1_PARSER_SYNTHETIC_CASES_PASS",pos)
 print("B01_60_W14_XML_FIXTURES_SYNTHETIC_ONLY_NOT_EDITOR_EXECUTION")
 print("B01_60_W14_SELECTED_GAMEPLAY_SOURCE_SHA",OWNER)
 print("B01_60_W14_ACTUAL_UNITY_EDITOR_G1=NOT_RUN_NO_ACTIVATION")
 out=ROOT/"qa-w14-native-case-proof"
 check(not out.exists(),"will not overwrite existing evidence")
 out.mkdir()
 evidence={
  "owner":"B01-60","selected_source_sha":OWNER,"frozen_previous_QA_sha":OLD,
  "original_parser_git_blob":OLD_GIT_BLOB,
  "real_original_validator_false_pass_reproductions":witness,
  "corrected_validator_adversarial_fail_closed":neg,
  "corrected_validator_positive_synthetic_nunit":pos,
  "fixtures":"SYNTHETIC_NUNIT_XML_NOT_REAL_UNITY",
  "Unity_6000_3_14f1":"NOT_RUN","Unity_PhysX":"NOT_RUN",
  "original_unity_Editor_log":"NOT_CREATED",
  "native_xml":"NOT_CREATED",
  "SaveLoad_live":"HOLD",
  "G0":"OWNER_510_ACCEPTED_SOURCE_ONLY_BY_B01_40",
  "G2":"HOLD","CODEX_ACK":"NONE","INTEGRATED":False,
  "cases":events
 }
 (out/"receipt.json").write_text(json.dumps(evidence,sort_keys=True,indent=2)+"\n")
 return 0
if __name__=="__main__":
 try:sys.exit(main())
 except Exception as e:
  print("B01_60_W14_TEST_FAIL",repr(e),file=sys.stderr)
  sys.exit(2)
