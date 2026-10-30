#!/usr/bin/env python3
"""Exact original #681 source, 5 exclusive QA added paths and two real CLR-process negative witnesses."""
import argparse, hashlib, json, subprocess
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4]
BASE="eed2e39636edfd8b8db754c426db452164cbf9d7"
OWN="Tools/B01/B01-61/P17W21Canonical681AuthorityQA/"
FLOW=".github/workflows/b01-61-p17w21-canonical681-authority.yml"
PINS={
 "Assets/B0123CanonicalSaveApi001/Core/CanonicalWorldGateway.cs":"f47dacc4ce6dd4ed1c66b6d3a07b432d2bc572c7",
 "Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs":"b52e511c370f5fca56c74029050844dc8cbb7f9a",
 "Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs":"fa1aa5e531927cbe73cf5fdfe161b4a981316db5"
}
def git(*args): return subprocess.check_output(["git",*args],cwd=ROOT,text=True,stderr=subprocess.DEVNULL).strip()
def main():
 p=argparse.ArgumentParser();p.add_argument("--log",type=Path,required=True);p.add_argument("--out",type=Path,required=True);a=p.parse_args()
 subprocess.run(["git","merge-base","--is-ancestor",BASE,"HEAD"],cwd=ROOT,check=True)
 for f,h in PINS.items():
  if git("rev-parse","HEAD:"+f)!=h or git("hash-object",f)!=h:raise AssertionError("SOURCE_DRIFT "+f)
 changed=git("diff","--name-only",BASE,"HEAD").splitlines()
 if len(changed)!=5 or len(set(changed))!=5 or any(not(f.startswith(OWN) or f==FLOW) for f in changed):
  raise AssertionError("MUST_ADD_ONLY_FIVE_QA_FILES "+repr(changed))
 for f in changed:
  if subprocess.run(["git","cat-file","-e",BASE+":"+f],cwd=ROOT,stderr=subprocess.DEVNULL).returncode==0:
   raise AssertionError("OLD_OWNER_OR_FROZEN_FILE_OVERWRITE "+f)
 # The selected #681 branch must not import the competing #682 original owner gateway.
 if "Tools/B01/B01-23/P17W20Canonical510Port001/Canonical510OwnerPort.cs" in git("ls-tree","-r","--name-only","HEAD","Tools/B01/B01-23"):
  raise AssertionError("COMPETING_CANONICAL_API_682_IMPORT")
 log=a.log.read_text(encoding="utf-8")
 for marker in (
  "TOTAL 10 PASS 10 FAIL 0",
  "PASS RED_681_TWO_LIVE_GATEWAYS_SAME_COLD_REPLAY_NO_LEASE",
  "PASS RED_681_CHILD_PROCESS_CAN_PUBLISH_NEWER_EPOCH_WHILE_OLD_WORLD_ACTIVE",
  "W21_681_SAME_SLOT_TWO_GATEWAYS_REPLAY_TWO_ACTIVE_WORLDS_RED=REPRODUCED",
  "W21_681_CROSS_PROCESS_NEW_HEAVY_WHILE_OLD_LIGHT_ACTIVE_RED=REPRODUCED",
  "ORIGINAL_681_CHILD_PROCESS_EXIT=0",
  "PASS SOURCE_REAL_FILE_DURABLE_COMMIT_LOST_ACK_STICKY_QUARANTINE",
  "PASS SOURCE_INCOMPLETE_ABORT_STICKY_QUARANTINE",
  "B0161_W21_ORIGINAL_681_SOURCE_GIT_DIRECT_CSHARP_EXECUTED",
  "SOURCE_RELEASE_GATE=HOLD_UNFENCED_MULTI_OWNER_681",
  "UNITY_G1_NOT_RUN CANONICAL_PRIVATE_510_UNIMPLEMENTED G2_HOLD NO_MERGE",
 ):
  if marker not in log:raise AssertionError("MISSING_ACTUAL_EXECUTED_CASE "+marker)
 entry={
  "qa_head_sha":git("rev-parse","HEAD"),"original_681_owner_sha":BASE,
  "original_source_git_blob_sha":PINS,
  "qa_exclusive_new_file_sha256":{f:hashlib.sha256((ROOT/f).read_bytes()).hexdigest() for f in changed},
  "qa_cases_per_os":10,
  "real_child_clr_processes_per_os":1,
  "expected_release_exit":3,
  "source_release_gate":"HOLD_AUTHORITY_REQUIRES_EXTERNAL_EXCLUSIVE_OWNER_LEASE",
  "native_unity_g1":"NOT_RUN","real_510_private_rigs":"UNIMPLEMENTED",
  "winner_681_vs_682":"OWNER_SELECTION_PENDING",
  "selected_host_r4_sha":"09c1809b73324a6d31c13e5d0143de62ac1e3ca6",
  "r4_formal_g0":"HOLD_COMPONENT_DESTROY_ORPHAN_P0",
  "codex_ack":False,"integrated":False,"released":False
 }
 a.out.parent.mkdir(parents=True,exist_ok=True)
 a.out.write_text(json.dumps(entry,sort_keys=True,indent=2)+"\n")
 print(json.dumps(entry,sort_keys=True))
if __name__=="__main__":main()
