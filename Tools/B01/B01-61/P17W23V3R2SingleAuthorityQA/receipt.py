#!/usr/bin/env python3
"""Strict selected #729 original V3, #712 original R2, #669 original full file, #556/W06 SHA pins and real source execution."""
import argparse,hashlib,json,subprocess
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4]
BASE="b8ca29c7707cd6b4338d0b9772d74dee676d49d2"
OWN="Tools/B01/B01-61/P17W23V3R2SingleAuthorityQA/"
FLOW=".github/workflows/b01-61-p17w23-v3-r2-original-double-authority.yml"
SOURCE={
"CanonicalWorldGatewayV3.cs":"21733278b6ab0669fccb6d085e0b6cf242792de2",
"CanonicalWorldSlotLease.cs":"a9331dd620336d8c42165d134dc8a5e05ed51873",
"CanonicalSlotSessionR2.cs":"fefc2e263e068b913c6e59ec3c8375bbbe43db4e",
"SingleFileSaveSlot.cs":"db7745635164d9eb6b10f3246538700158130b7b",
"DetachedSnapshotContract.cs":"b52e511c370f5fca56c74029050844dc8cbb7f9a",
"DebrisImpulseCore.cs":"fa1aa5e531927cbe73cf5fdfe161b4a981316db5",
"SecureRigSpec.cs":"b680ed0ec969ac23203fa55eab7b90f8daa86e43"
}
ORIGINAL={
"Assets/B0123CanonicalSaveApi001/Core/CanonicalWorldGateway.cs":SOURCE["CanonicalWorldGatewayV3.cs"],
"Assets/B0123CanonicalSaveApi001/Core/CanonicalWorldSlotLease.cs":SOURCE["CanonicalWorldSlotLease.cs"]
}
def git(*parts):return subprocess.check_output(["git",*parts],cwd=ROOT,text=True,stderr=subprocess.DEVNULL).strip()
def main():
 parser=argparse.ArgumentParser();parser.add_argument("--log",required=True,type=Path)
 parser.add_argument("--out",required=True,type=Path);v=parser.parse_args()
 subprocess.run(["git","merge-base","--is-ancestor",BASE,"HEAD"],cwd=ROOT,check=True)
 for path,sha in ORIGINAL.items():
  if git("rev-parse","HEAD:"+path)!=sha or git("hash-object",path)!=sha:
   raise AssertionError("ORIGINAL_729_OWNER_MODIFIED "+path)
 for name,sha in SOURCE.items():
  if git("hash-object",str(ROOT/OWN/".source"/name))!=sha:
   raise AssertionError("ORIGINAL_CSHARP_SHA_PIN_MISMATCH "+name)
 changed=git("diff","--name-only",BASE,"HEAD").splitlines()
 if len(changed)!=5 or len(set(changed))!=5 or any(not(p.startswith(OWN) or p==FLOW) for p in changed):
  raise AssertionError("QA_NOT_EXACTLY_FIVE_OWN_ONLY_ADDED "+repr(changed))
 for p in changed:
  if subprocess.run(["git","cat-file","-e",BASE+":"+p],cwd=ROOT,stderr=subprocess.DEVNULL).returncode==0:
   raise AssertionError("FROZEN_FILE_OVERWRITTEN "+p)
 log=v.log.read_text(encoding="utf8")
 markers=[
  "TOTAL 11 PASS 11 FAIL 0",
  "PASS GREEN_V3_ONE_OWNER_OS_LEASE_BIND_ONCE",
  "PASS GREEN_V3_WRONG_RUN_DENIED_BEFORE_STAGING",
  "PASS GREEN_V3_FAILED_PARTIAL_STAGE_LEASE_RETAINED",
  "PASS GREEN_V3_FALSE_AND_THROW_RETIREMENT_PROOF_LOCKS_OS",
  "PASS GREEN_R2_ORIGINAL_669_COLD_STABLE_IDS_SAME_GEN_FORK_DENIED",
  "PASS RED_ORIGINAL_729_AND_712_SIMULTANEOUS_ACTIVE_SAME_SLOT",
  "PASS RED_REAL_CHILD_712_GEN2_COMMIT_WHILE_V3_GEN1_WORLD_LIVE",
  "W23_SELECTED_729_V3_AND_712_R2_TWO_ACTIVE_WORLDS_IN_ONE_SLOT_RED=REPRODUCED",
  "W23_REAL_ORIGINAL_OWNER_DOT_OWNER_AND_STORE_DOT_LEASE_BOTH_HELD_RED=REPRODUCED",
  "W23_729_OWNER_LIVE_WHILE_CHILD_712_PHYSICALLY_COMMITS_GEN2_RED=REPRODUCED",
  "W23_V3_STALE_PREVIEW_CHECKPOINT_FENCE_GREEN=PASS",
  "W23_CHILD_R2_CLR_EXIT=0",
  "B0161_W23_DIRECT_ORIGINAL_729_712_669_556_W06_TESTS",
  "SOURCE_RELEASE_GATE=HOLD_TWO_INDEPENDENT_DOT_OWNER_DOT_LEASE_AUTHORITIES",
  "NATIVE_UNITY_G1_NOT_RUN FROZEN_PRIVATE_510_RIG_LEDGER_NOT_WIRED G0_COMBINED_HOLD NO_MERGE"
 ]
 for m in markers:
  if m not in log:raise AssertionError("ACTUAL_B01_61_W23_WITNESS_MISSING "+m)
 r={"qa_sha":git("rev-parse","HEAD"),"original_729_sha":BASE,
    "original_712_sha":"bd8905ad549a7b26854b671be971f7d781308311",
    "original_669_sha":"d6ec0c82e388e65c1e02ddfd920bb50a1f6ef121",
    "original_blob_guards":SOURCE,
    "five_added_only_qa_files_sha256":{p:hashlib.sha256((ROOT/p).read_bytes()).hexdigest() for p in changed},
    "actual_original_source_assertions_per_os":11,"real_child_clr_processes_per_os":1,
    "source_gate":"HOLD_TWO_DISTINCT_OWNER_OS_LOCKS", "release_exit_expected":3,
    "source_G0":"NO_FULL_BRIDGE", "native_unity_G1":"NOT_RUN","live_game_SaveLoad":"HOLD",
    "integrated":False,"released":False,"codex_ack":"NONE"}
 v.out.parent.mkdir(parents=True,exist_ok=True)
 v.out.write_text(json.dumps(r,indent=2,sort_keys=True)+"\n")
 print(json.dumps(r,sort_keys=True))
if __name__=="__main__":main()
