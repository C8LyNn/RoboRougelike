#!/usr/bin/env python3
"""Exact original #642 source blob, immutable dependencies, five QA-only files + real stdout."""
import argparse,hashlib,json,subprocess
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4]
BASE="22cec9888047994ae523cbe7bf1b0a483d64cec4"
OWN="Tools/B01/B01-61/P17W19R3Selected642AmbiguousCommitQA/"
WF=".github/workflows/b01-61-p17w19-r3-642-ambiguous-commit.yml"
PINS={
 "Assets/B0123UnitySaveBridge001/Core/AtomicWorldSwap.cs":"a0a1923216d88ff8f50c560196f82cdec694f15c",
 "Assets/B0123UnitySaveBridge001/Unity/IsolatedTwinRigSaveHost.cs":"7b8354bc4cd846227b4bdf6d25d81bed34a325bc",
 "Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs":"b52e511c370f5fca56c74029050844dc8cbb7f9a",
 "Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs":"fa1aa5e531927cbe73cf5fdfe161b4a981316db5",
 "Assets/B0123SecureReattachV2/Runtime/SecureRigSpec.cs":"b680ed0ec969ac23203fa55eab7b90f8daa86e43"
}
def git(*args):return subprocess.check_output(["git",*args],cwd=ROOT,text=True,stderr=subprocess.DEVNULL).strip()
def main():
 ap=argparse.ArgumentParser();ap.add_argument("--log",type=Path,required=True);ap.add_argument("--out",type=Path,required=True)
 a=ap.parse_args()
 subprocess.run(["git","merge-base","--is-ancestor",BASE,"HEAD"],cwd=ROOT,check=True)
 for file,blob in PINS.items():
  if git("rev-parse","HEAD:"+file)!=blob or git("hash-object",file)!=blob:
   raise AssertionError("SELECTED_R3_SOURCE_OR_DEPENDENCY_CHANGED "+file)
 files=git("diff","--name-only",BASE,"HEAD").splitlines()
 if len(files)!=5 or len(set(files))!=5 or any(not(x.startswith(OWN)or x==WF)for x in files):
  raise AssertionError("QA_ONLY_FIVE_ADDED_FILES_SCOPE_FAIL "+repr(files))
 for f in files:
  if subprocess.run(["git","cat-file","-e",BASE+":"+f],cwd=ROOT,stderr=subprocess.DEVNULL).returncode==0:
   raise AssertionError("QA_FILE_OVERWROTE_PRODUCER "+f)
 if "Assets/B0123UnitySaveBridge001/Core/HostSlotLease.cs" in git("ls-tree","-r","--name-only","HEAD","Assets/B0123UnitySaveBridge001/Core"):
  raise AssertionError("COMPETING_R3_644_IMPORTED")
 host=(ROOT/"Assets/B0123UnitySaveBridge001/Unity/IsolatedTwinRigSaveHost.cs").read_text()
 for required in ("TrySelectPersistentRunSlot", "slotOwnerLease", "AcquireSlot()", "OnDisable()", "FileShare.None", "CheckpointSlotPath.ForRun"):
  if required not in host:raise AssertionError("R3_642_UNITY_SOURCE_MISSING "+required)
 log=a.log.read_text(encoding="utf8")
 for required in (
    "TOTAL 16 PASS 16 FAIL 0",
    "PASS RED_DURABLE_COMMIT_SUCCEEDED_BUT_THREW_HOST_NOT_QUARANTINED",
    "PASS R3_STALE_OLD_WORLD_CANNOT_EXPORT_AFTER_DURABLE_EXCEPTION",
    "PASS R3_TWO_COORDINATORS_SAME_SLOT_STALE_WRITER_DENIED",
    "PASS R3_CURRENT_DURABLE_OWNER_SAVES_NEXT_WITH_ORIGINAL_W06_IDS",
    "PASS R3_UNIQUE_RUN_AND_GUID_SLOT_PATHS_NO_PATH_ESCAPE",
    "PASS FAILED_PHYSICAL_ROLLBACK_STICKY_QUARANTINE_FIRST_CALL",
    "PASS FRESH_FILE_LOCK_CAS_RACE_REFUSES_STALE_SECOND_WRITER",
    "B01_61_ORIGINAL_R3_642_EXACT_SOURCE_EXECUTED=TRUE",
    "DURABLE_COMMIT_THROW_AFTER_REAL_WRITE_RED=REPRODUCED",
    "R3_STALE_WORLD_SAVE_BLOCK=PASS",
    "SOURCE_RELEASE_GATE=HOLD_P0_RED",
    "NO_UNITY_PHYSX=NATIVE_G1_NOT_RUN LIVE_510_LEDGER=UNINTEGRATED G2=HOLD",
 ):
  if required not in log:raise AssertionError("EXECUTED_EVIDENCE_MISSING "+required)
 record={
   "qa_exact_head_sha":git("rev-parse","HEAD"),
   "original_selected_candidate_642_sha":BASE,
   "candidate_644":"ALTERNATIVE_NOT_IMPORTED_OWNER_SELECTION_PENDING",
   "immutable_596_sha":"efb2b06eafedab5469b57f163152d96aca3c5915",
   "immutable_510_sha":"d94400d95d13db7f27e1533df05c9292d5380431",
   "immutable_556_sha":"33094f154c7b91119b36d58aab285e6e83c5de57",
   "original_git_blob_pins":PINS,
   "new_qa_only_sha256":{f:hashlib.sha256((ROOT/f).read_bytes()).hexdigest() for f in files},
   "original_r3_direct_source_cases_pass":16,"original_r3_cases_fail":0,
   "expected_source_release_exit":3,
   "release_gate":"HOLD_P0_RED_ON_AMBIGUOUS_DURABLE_COMMIT",
   "stale_world_export":"DENIED_SOURCE_ONLY",
   "native_unity_g1":"NOT_RUN",
   "formal_r3_physical_g0":"PENDING_B01_40_62",
   "canonical_510_private_ledger":"NOT_INTEGRATED",
   "codex_ack":False,"merged":False
 }
 a.out.parent.mkdir(parents=True,exist_ok=True);a.out.write_text(json.dumps(record,sort_keys=True,indent=2)+"\n")
 print(json.dumps(record,sort_keys=True))
if __name__=="__main__":main()
