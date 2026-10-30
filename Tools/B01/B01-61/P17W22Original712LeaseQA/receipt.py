#!/usr/bin/env python3
"""B01-61 W22 strict exact original #712 source and executed real CLR process lease receipt."""
import argparse,hashlib,json,subprocess
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4]
BASE="bd8905ad549a7b26854b671be971f7d781308311"
OWN="Tools/B01/B01-61/P17W22Original712LeaseQA/"
WORKFLOW=".github/workflows/b01-61-w22-712-real-owner-lease.yml"
PINS={"Tools/B01/B01-62/P17W21CanonicalSessionLeaseR2/CanonicalSlotSessionR2.cs":"fefc2e263e068b913c6e59ec3c8375bbbe43db4e"}
SOURCE={
"CanonicalWorldGatewayV2.cs":"704e774dedd5be5af26e0791fba87ecdb109ae2a",
"SingleFileSaveSlot.cs":"db7745635164d9eb6b10f3246538700158130b7b",
"DetachedSnapshotContract.cs":"b52e511c370f5fca56c74029050844dc8cbb7f9a",
"DebrisImpulseCore.cs":"fa1aa5e531927cbe73cf5fdfe161b4a981316db5",
"SecureRigSpec.cs":"b680ed0ec969ac23203fa55eab7b90f8daa86e43"
}
def git(*x): return subprocess.check_output(["git",*x],cwd=ROOT,text=True,stderr=subprocess.DEVNULL).strip()
def main():
 ap=argparse.ArgumentParser();ap.add_argument("--log",type=Path,required=True);ap.add_argument("--out",type=Path,required=True);a=ap.parse_args()
 subprocess.run(["git","merge-base","--is-ancestor",BASE,"HEAD"],cwd=ROOT,check=True)
 for path,sha in PINS.items():
  if git("rev-parse","HEAD:"+path)!=sha or git("hash-object",path)!=sha:raise AssertionError("P0_PRODUCER_SHA_DRIFT "+path)
 for name,sha in SOURCE.items():
  if git("hash-object",str(ROOT/OWN/".source"/name))!=sha:raise AssertionError("ORIGINAL_FROZEN_OWNER_BLOB_DRIFT "+name)
 changed=git("diff","--name-only",BASE,"HEAD").splitlines()
 if len(changed)!=5 or len(set(changed))!=5 or any(not(s.startswith(OWN) or s==WORKFLOW) for s in changed):
  raise AssertionError("NOT_EXACT_5_OWN_ADDED_ONLY "+repr(changed))
 for path in changed:
  if subprocess.run(["git","cat-file","-e",BASE+":"+path],cwd=ROOT,stderr=subprocess.DEVNULL).returncode==0:
   raise AssertionError("FROZEN_OR_OWNER_OVERWRITE "+path)
 log=a.log.read_text(encoding="utf8")
 markers=[
  "TOTAL 11 PASS 11 FAIL 0",
  "PASS GREEN_712_FALSE_CLEANUP_RETINS_REAL_OS_LEASE",
  "PASS GREEN_712_THROW_CLEANUP_RETINS_REAL_OS_LEASE",
  "PASS GREEN_712_REAL_CHILD_DENIED_THEN_RESTORE_ONLY_AFTER_ROOT_RETIRE",
  "PASS ORIGINAL_712_GEN1_TO_GEN2_EXACT_IDS_COLD_FORK_DENIAL",
  "W22_712_ORPHAN_PARTIAL_OWNER_DISPOSE_AND_CHILD_DENIED_GREEN=PASS",
  "W22_712_CHILD_CAN_PUBLISH_ONLY_AFTER_VERIFIED_RETIREMENT_GREEN=PASS",
  "W22_712_PHYSICAL_G1_ONLY_AFTER_OLD_PARTIAL_OWNER_RETIRED_GREEN=PASS",
  "W22_712_CHILD_CLR_EXIT=0",
  "B0161_W22_ORIGINAL_712_691_669_DIRECT_SOURCE_CSHARP9",
  "SOURCE_RELEASE_GATE=GREEN_TESTED_707_SCOPE_ONLY",
  "SOURCE_G0_REQUIRES_FORMAL_G0 NATIVE_UNITY_G1_NOT_RUN GAME_510_PRIVATE_RIG_NOT_IMPLEMENTED"
 ]
 for marker in markers:
  if marker not in log:raise AssertionError("ACTUAL_ORIGINAL_EXECUTION_MISSING "+marker)
 r={"head":git("rev-parse","HEAD"),"owner_712_sha":BASE,
    "original_git_blob_source_pins":{**PINS,**SOURCE},
    "five_new_qa_only_sha256":{p:hashlib.sha256((ROOT/p).read_bytes()).hexdigest() for p in changed},
    "source_test_cases_per_os":11,"actual_extra_clr_child_processes_per_os":"multiple",
    "release_gate":"GREEN_EXACT_707_SCOPE_ONLY_HOLD_FULL_GAME",
    "source_G0":"NOT_FORMALLY_ACCEPTED", "unity_G1":"NOT_RUN",
    "live_frozen_510_private_rig":"UNIMPLEMENTED", "G2":"HOLD", "codex_ack":"NONE",
    "integrated":False,"released":False}
 a.out.parent.mkdir(parents=True,exist_ok=True)
 a.out.write_text(json.dumps(r,sort_keys=True,indent=2)+"\n")
 print(json.dumps(r,sort_keys=True))
if __name__=="__main__":main()
