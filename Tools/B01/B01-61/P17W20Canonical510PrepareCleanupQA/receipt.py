#!/usr/bin/env python3
"""W20 exact original new owner API immutable Git bytes, scope, log, source release RED."""
import argparse,hashlib,json,subprocess
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4]
BASE="5387e829a5f6d38b713086e6fb906a9a755189ff"
DIR="Tools/B01/B01-61/P17W20Canonical510PrepareCleanupQA/"
WORKFLOW=".github/workflows/b01-61-p17w20-canonical510-prepare-cleanup.yml"
PINS={
"Tools/B01/B01-23/P17W20Canonical510Port001/Canonical510OwnerPort.cs":"aa0124488bc0f7036d65baf4e98c62e53a8f9d0b",
"Assets/B0123UnitySaveBridge001/Core/AtomicWorldSwap.cs":"8fd1dd9a1bc870433809da8fba897ec924fa4113",
"Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs":"b52e511c370f5fca56c74029050844dc8cbb7f9a",
"Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs":"fa1aa5e531927cbe73cf5fdfe161b4a981316db5",
"Assets/B0123SecureReattachV2/Runtime/SecureRigSpec.cs":"b680ed0ec969ac23203fa55eab7b90f8daa86e43"
}
def git(*args):return subprocess.check_output(["git",*args],cwd=ROOT,stderr=subprocess.DEVNULL,text=True).strip()
def main():
 p=argparse.ArgumentParser();p.add_argument("--log",required=True,type=Path);p.add_argument("--out",required=True,type=Path);a=p.parse_args()
 subprocess.run(["git","merge-base","--is-ancestor",BASE,"HEAD"],cwd=ROOT,check=True)
 for f,sha in PINS.items():
  if git("rev-parse","HEAD:"+f)!=sha or git("hash-object",f)!=sha:
   raise AssertionError("SOURCE_PIN_MISMATCH "+f)
 changed=git("diff","--name-only",BASE,"HEAD").splitlines()
 if len(changed)!=5 or len(set(changed))!=5 or any(not(f.startswith(DIR) or f==WORKFLOW)for f in changed):
  raise AssertionError("FIVE_NEW_OWN_QA_FILES_ONLY_SCOPE_FAIL "+repr(changed))
 for f in changed:
  if subprocess.run(["git","cat-file","-e",BASE+":"+f],cwd=ROOT,stderr=subprocess.DEVNULL).returncode==0:
   raise AssertionError("FROZEN_OR_PRODUCER_EXISTING_FILE_MUTATED "+f)
 if "Assets/B0123UnitySaveBridge001/Core/HostSlotLease.cs" in git("ls-tree","-r","--name-only","HEAD","Assets/B0123UnitySaveBridge001/Core"):
  raise AssertionError("R4_ALT_SOURCE_IMPORTED")
 raw=a.log.read_text(encoding="utf8")
 for marker in [
  "SOURCE_QA_CASES_TOTAL=9 PASS=9 FAIL=0",
  "PASS RED_ORIGINAL_OWNERFACTORY_PREPARE_CLEANUP_FAILURE_NO_QUARANTINE_ORPHAN_AND_RETRY",
  "W20_CANONICAL_510_PORT_PREPARE_FAILED_CLEANUP_ORPHAN_SOURCE_RED=REPRODUCED",
  "W20_CANONICAL_510_PORT_RETRY_PUBLISHES_WITH_ORPHAN_RIG=REPRODUCED",
  "PASS W20_ORIGINAL_556_W06_ALL_FOUR_STABLE_LIGHT_HEAVY_STATES",
  "PASS W20_NORMAL_GENERATION_ADVANCE_SAVES_HEAVY_WITH_ORIGINAL_IDS",
  "B0161_W20_EXACT_ORIGINAL_682_OWNER_PORT_DIRECT_CSHARP_EXECUTED",
  "SOURCE_RELEASE_GATE=HOLD_P0_PREPARE_CLEANUP_ORPHAN",
  "NATIVE_UNITY_G1_NOT_RUN LIVE_CANONICAL_510_OWNER_NOT_IMPLEMENTED G2_HOLD NO_MERGE"
 ]:
  if marker not in raw:raise AssertionError("ACTUAL_ORIGINAL_SOURCE_STDOUT_MISSING "+marker)
 receipt={
  "tested_producer_canonical_api_pr":682,
  "tested_original_682_head_sha":BASE,
  "qa_head_sha":git("rev-parse","HEAD"),
  "exact_original_source_git_pins":PINS,
  "qa_five_added_sha256":{f:hashlib.sha256((ROOT/f).read_bytes()).hexdigest() for f in changed},
  "original_executed_case_pass":9,
  "source_release":"HOLD_P0_UNSAFE_PREPARE_TEARDOWN",
  "expected_source_release_exit":3,
  "selected_r4":647,
  "r4_selected_sha":"09c1809b73324a6d31c13e5d0143de62ac1e3ca6",
  "r4_formal_g0":"HOLD_COMPONENT_ONLY_DESTROY_P0",
  "native_unity_g1":"NOT_RUN",
  "canonical_510_real_implementation":"NOT_IMPLEMENTED",
  "codex_ack":False,
  "merged":False
 }
 a.out.parent.mkdir(parents=True,exist_ok=True)
 a.out.write_text(json.dumps(receipt,sort_keys=True,indent=2)+"\n")
 print(json.dumps(receipt,sort_keys=True))
if __name__=="__main__":main()
