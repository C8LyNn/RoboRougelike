#!/usr/bin/env python3
"""Independent exact selected immutable #596 source, QA-only scope and executed RED witness."""
import argparse,hashlib,json,subprocess
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4]
BASE="efb2b06eafedab5469b57f163152d96aca3c5915"
DIR="Tools/B01/B01-61/P17W18UncertainCommitExceptionQA/"
WORKFLOW=".github/workflows/b01-61-w18-596-uncertain-durable-commit.yml"
PINS={
 "Assets/B0123UnitySaveBridge001/Core/AtomicWorldSwap.cs":"1f9e883c38101f214e4335867faa731f7ae67689",
 "Assets/B0123UnitySaveBridge001/Unity/IsolatedTwinRigSaveHost.cs":"4bac6f327c9b93ae98dd2061c0ca8ea2100aab42",
 "Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs":"b52e511c370f5fca56c74029050844dc8cbb7f9a",
 "Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs":"fa1aa5e531927cbe73cf5fdfe161b4a981316db5",
 "Assets/B0123SecureReattachV2/Runtime/SecureRigSpec.cs":"b680ed0ec969ac23203fa55eab7b90f8daa86e43"
}
def git(*args):
 return subprocess.check_output(["git",*args],cwd=ROOT,text=True).strip()
def main():
 ap=argparse.ArgumentParser()
 ap.add_argument("--log",required=True,type=Path)
 ap.add_argument("--out",required=True,type=Path)
 a=ap.parse_args()
 subprocess.run(["git","merge-base","--is-ancestor",BASE,"HEAD"],cwd=ROOT,check=True)
 for path,sha in PINS.items():
  if git("rev-parse","HEAD:"+path)!=sha or git("hash-object",path)!=sha:
   raise AssertionError("FROZEN_OR_SELECTED_SOURCE_DRIFT "+path)
 changed=git("diff","--name-only",BASE,"HEAD").splitlines()
 if len(changed)!=5 or len(set(changed))!=5 or any(
   not (x.startswith(DIR) or x==WORKFLOW) for x in changed):
  raise AssertionError("NOT_FIVE_EXCLUSIVE_NEW_QA_PATHS "+str(changed))
 for x in changed:
  if git("cat-file","-e",BASE+":"+x) is not None:
   pass
 # Original selected #596 uses the real FileCheckpointStore and never #604 CheckpointFence.
 if "Assets/B0123UnitySaveBridge001/Core/CheckpointFence.cs" in git(
    "ls-tree","-r","--name-only","HEAD","Assets/B0123UnitySaveBridge001/Core"):
  raise AssertionError("ALTERNATIVE_604_SOURCE_MIXED")
 data=a.log.read_text(encoding="utf8")
 for pattern in [
   "TOTAL 13 PASS 13 FAIL 0",
   "PASS RED_DURABLE_COMMIT_SUCCEEDED_BUT_THREW_HOST_NOT_QUARANTINED",
   "PASS RED_UNFENCED_OLD_WORLD_EXPORT_ESCALATES_TO_HIGHER_EPOCH",
   "PASS NORMAL_NEW_EPOCH_COMMITS_REAL_FILE_AND_HEAVY_LEDGER",
   "PASS COMMIT_EXPLICIT_FALSE_QUARANTINES_AND_PRESERVES_OLD",
   "PASS POST_ACTIVATION_ID_MISMATCH_ROLLS_BACK_BEFORE_DISK",
   "DURABLE_COMMIT_THROW_AFTER_REAL_WRITE_RED=REPRODUCED",
   "OLD_WORLD_STALE_SAVE_PROMOTION_RED=REPRODUCED",
   "SOURCE_RELEASE_GATE=HOLD_P0_RED",
   "NO_UNITY_PHYSX=NATIVE_G1_NOT_RUN LIVE_510_LEDGER=UNINTEGRATED G2=HOLD"]:
  if pattern not in data:raise AssertionError("ORIGINAL_EXECUTION_LOG_MISSING "+pattern)
 artifact={
  "qa_head_sha":git("rev-parse","HEAD"),"exact_selected_producer_596_sha":BASE,
  "frozen_game_510_sha":"d94400d95d13db7f27e1533df05c9292d5380431",
  "frozen_schema_556_sha":"33094f154c7b91119b36d58aab285e6e83c5de57",
  "original_git_blob_pins":PINS,
  "owned_qa_sha256":{p:hashlib.sha256((ROOT/p).read_bytes()).hexdigest() for p in changed},
  "selected_596_original_source_tests_pass":13,
  "selected_596_original_source_tests_fail":0,
  "expected_red_witness":"DURABLE_COMMIT_SUCCEEDS_BUT_THROWS_ACK",
  "release_verdict":"HOLD_P0_RED",
  "native_unity_g1":"NOT_RUN",
  "canonical_510_private_ledger":"UNINTEGRATED",
  "physical_g0":"PENDING_B01_40_62",
  "codex_ack":False,"merged":False,"g2":"HOLD"
 }
 a.out.parent.mkdir(parents=True,exist_ok=True)
 a.out.write_text(json.dumps(artifact,sort_keys=True,indent=2)+"\n")
 print(json.dumps(artifact,sort_keys=True))
if __name__=="__main__":main()
