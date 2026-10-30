#!/usr/bin/env python3
"""Exact #697/#691/#669 directly compiled source, 5 new QA-only paths, actual CLR witness stdout."""
import argparse,hashlib,json,subprocess
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4]
BASE="ab62b041e50f8aa0defada2aadc0987d6c63f9d9"
OWN="Tools/B01/B01-61/P17W21Adapter697OrphanLeaseQA/"
FLOW=".github/workflows/b01-61-w21-adapter697-partial-owner-lease.yml"
PINS={
"Tools/B01/B01-62/P17W21Canonical681SlotAdapter001/CanonicalSlotCommitAdapter.cs":"bcecb87c8b1b198e8c086d30f26846bf202ea1c4",
}
SOURCE={
"CanonicalWorldGatewayV2.cs":"704e774dedd5be5af26e0791fba87ecdb109ae2a",
"SingleFileSaveSlot.cs":"db7745635164d9eb6b10f3246538700158130b7b",
"DetachedSnapshotContract.cs":"b52e511c370f5fca56c74029050844dc8cbb7f9a",
"DebrisImpulseCore.cs":"fa1aa5e531927cbe73cf5fdfe161b4a981316db5",
"SecureRigSpec.cs":"b680ed0ec969ac23203fa55eab7b90f8daa86e43"
}
def git(*args): return subprocess.check_output(["git",*args],cwd=ROOT,text=True,stderr=subprocess.DEVNULL).strip()
def main():
 a=argparse.ArgumentParser();a.add_argument("--log",required=True,type=Path);a.add_argument("--out",required=True,type=Path);v=a.parse_args()
 subprocess.run(["git","merge-base","--is-ancestor",BASE,"HEAD"],cwd=ROOT,check=True)
 for path,expected in PINS.items():
  if git("rev-parse","HEAD:"+path)!=expected or git("hash-object",path)!=expected:
   raise AssertionError("ORIGINAL_PRODUCER_697_GIT_BLOB_DRIFT "+path)
 source=ROOT/OWN/".source"
 for path,expected in SOURCE.items():
  if git("hash-object",str(source/path))!=expected:
   raise AssertionError("ORIGINAL_691_669_FROZEN_CODEC_SHA_PIN_FAIL "+path)
 changed=git("diff","--name-only",BASE,"HEAD").splitlines()
 if len(changed)!=5 or len(set(changed))!=5 or any(not(p.startswith(OWN) or p==FLOW) for p in changed):
  raise AssertionError("FIVE_NEW_OWN_ONLY_FILE_GUARD_FAIL "+repr(changed))
 for path in changed:
  if subprocess.run(["git","cat-file","-e",BASE+":"+path],cwd=ROOT,stderr=subprocess.DEVNULL).returncode==0:
   raise AssertionError("OVERWROTE_FOREIGN_OR_FROZEN_FILE "+path)
 log=v.log.read_text(encoding="utf8")
 for marker in (
  "TOTAL 7 PASS 7 FAIL 0",
  "PASS RED_697_FAILED_CLEANUP_FALSE_ALLOWS_UNSAFE_DISPOSE_UNLOCK",
  "PASS RED_697_FAILED_CLEANUP_THROW_ALLOWS_UNSAFE_DISPOSE_UNLOCK",
  "PASS RED_697_REAL_CHILD_RESTORE_WHILE_PARTIAL_OLD_RIG_IS_ACTIVE",
  "PASS ORIGINAL_697_HEALTHY_ACCEPTED_WORLD_KEEPS_EXCLUSIVE_OS_LEASE",
  "W21_697_DISPOSE_UNLOCKS_SLOT_DURING_FAILED_PARTIAL_WORLD_CLEANUP_RED=REPRODUCED",
  "W21_697_EXTERNAL_CHILD_PUBLISHES_WITH_ABANDONED_ACTIVE_RIG_RED=REPRODUCED",
  "W21_697_PHYSICAL_DISK_ENVELOPE_G1_WITH_OLD_ACTIVE_ORPHAN_RED=REPRODUCED",
  "W21_697_CHILD_CLR_EXIT=0",
  "B0161_W21_EXACT_697_691_669_ORIGINAL_CSHARP_SOURCES_DIRECT_EXECUTED",
  "SOURCE_RELEASE_GATE=HOLD_ORPHAN_LEASE_LOSS_ON_DISPOSE",
  "ORIGINAL_510_PRIVATE_RIG_NOT_IMPLEMENTED NATIVE_G1_NOT_RUN G2_HOLD NO_MERGE"
 ):
  if marker not in log:raise AssertionError("REAL_SOURCE_WITNESS_MISSING "+marker)
 receipt={
  "qa_commit":git("rev-parse","HEAD"),"original_producer_697_sha":BASE,
  "original_owner_api_v2_691_sha":"809f82575c638e1b4d2806b5f417bddab6c0871a",
  "original_store_669_sha":"d6ec0c82e388e65c1e02ddfd920bb50a1f6ef121",
  "direct_original_git_blob_sha":{**PINS,**SOURCE},
  "five_added_only_qa_source_sha256":{p:hashlib.sha256((ROOT/p).read_bytes()).hexdigest() for p in changed},
  "direct_original_CSharp9_assertions_per_os":7,
  "real_additional_child_clr_processes_per_os":1,
  "release_test_expected_exit":3,
  "source_release":"HOLD_P0_ADAPTER_DISPOSE_BEFORE_PARTIAL_STAGE_OWNER_CLEANUP",
  "native_unity_g1":"NOT_RUN",
  "real_original_private_510_ledger":"UNIMPLEMENTED",
  "integrated":False,"released":False,"codex_ack":False
 }
 v.out.parent.mkdir(parents=True,exist_ok=True)
 v.out.write_text(json.dumps(receipt,sort_keys=True,indent=2)+"\n")
 print(json.dumps(receipt,sort_keys=True))
if __name__=="__main__":main()
