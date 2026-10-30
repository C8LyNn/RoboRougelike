#!/usr/bin/env python3
"""W24 exact original selected 749 Git source sha provenance and executed source stdout."""
import hashlib,json,subprocess,argparse
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4]
BASE="8634e71dfcf633323073fe584a6c0884f1ca5d23"
OWN="Tools/B01/B01-61/P17W24Selected749IndependentQA/"
FLOW=".github/workflows/b01-61-w24-749-original-regression.yml"
PRODUCER={
 "Tools/B01/B01-62/P17W23OneAuthorityV3/CanonicalSingleAuthorityStoreV3.cs":"f8fab0b35147a23ae31bcb4a0cf91590d0896d97",
 "Tools/B01/B01-62/P17W23OneAuthorityV3/CanonicalSlotSessionV3.cs":"9d86581626c8657b6662cb281f7621fa43343fb3"
}
SOURCE={
"CanonicalWorldGatewayV3.cs":"21733278b6ab0669fccb6d085e0b6cf242792de2",
"CanonicalWorldSlotLease.cs":"a9331dd620336d8c42165d134dc8a5e05ed51873",
"SingleFileSaveSlotV1.cs":"db7745635164d9eb6b10f3246538700158130b7b",
"DetachedSnapshotContract.cs":"b52e511c370f5fca56c74029050844dc8cbb7f9a",
"DebrisImpulseCore.cs":"fa1aa5e531927cbe73cf5fdfe161b4a981316db5",
"SecureRigSpec.cs":"b680ed0ec969ac23203fa55eab7b90f8daa86e43"}
def git(*x):return subprocess.check_output(["git",*x],cwd=ROOT,text=True,stderr=subprocess.DEVNULL).strip()
def main():
 p=argparse.ArgumentParser();p.add_argument("--log",type=Path,required=True);p.add_argument("--out",type=Path,required=True);a=p.parse_args()
 subprocess.run(["git","merge-base","--is-ancestor",BASE,"HEAD"],cwd=ROOT,check=True)
 for path,sha in PRODUCER.items():
  if git("rev-parse","HEAD:"+path)!=sha or git("hash-object",path)!=sha:raise AssertionError("AUTHOR_749_SOURCE_MODIFIED "+path)
 for path,sha in SOURCE.items():
  if git("hash-object",str(ROOT/OWN/".original"/path))!=sha:raise AssertionError("FROZEN_729_OR_669_OR_556_SHA_DRIFT "+path)
 changed=git("diff","--name-only",BASE,"HEAD").splitlines()
 if len(changed)!=5 or len(set(changed))!=5 or any(not (x.startswith(OWN) or x==FLOW) for x in changed):raise AssertionError("SCOPE_NOT_5_NEW_ONLY "+repr(changed))
 for x in changed:
  if subprocess.run(["git","cat-file","-e",BASE+":"+x],cwd=ROOT,stderr=subprocess.DEVNULL).returncode==0:raise AssertionError("AUTHOR_OVERWRITTEN "+x)
 log=a.log.read_text(encoding="utf8")
 markers=[
 "TOTAL 12 PASS 12 FAIL 0",
 "PASS GREEN_749_ONE_OWNER_REAL_CHILD_AND_THREE_GENERATION_COLD",
 "PASS GREEN_749_PARTIAL_CLEANUP_FALSE_NO_EARLY_RELEASE",
 "PASS GREEN_749_PARTIAL_CLEANUP_THROW_NO_EARLY_RELEASE",
 "PASS GREEN_749_LOST_ACK_POST_RENAME_REAL_COLD",
 "PASS GREEN_749_FAULT_BEFORE_RENAME_REAL_COLD",
 "PASS GREEN_749_QUIESCENCE_REENTRY_BUSY_FENCE",
 "PASS GREEN_749_LEGACY_RAW_669_MARKER_AND_FOREIGN_RUN_DENIED",
 "PASS GREEN_749_CORRUPT_PHYSICAL_ENVELOPE_STICKY_QUARANTINE",
 "W24_SELECTED_749_ORPHAN_RETIRE_PROOF_GREEN",
 "W24_SELECTED_749_LOST_ACK_REAL_COMMIT_G2_COLD_GREEN",
 "W24_SELECTED_749_PRE_RENAME_FAULT_G1_COLD_GREEN",
 "B0161_W24_SELECTED_749_DIRECT_UNMODIFIED_729_GATEWAY_669_LEGACY_556_W06",
 "SOURCE_RELEASE_GATE=GREEN_SCOPE_ONLY_FROZEN_PRIVATE_510_HOLD",
 "SOURCE_G0_REQUIRES_W24_INDEPENDENT_REVIEW NATIVE_G1_NOT_RUN PRIVATE_RIG_510_NOT_WIRED G2_HOLD"]
 for m in markers:
  if m not in log:raise AssertionError("ACTUAL_SOURCE_ASSERTION_MISSING "+m)
 r={"head":git("rev-parse","HEAD"),"selected_producer_749_sha":BASE,
    "source_original_producer_git_blobs":PRODUCER,"source_exact_frozen_git_blobs":SOURCE,
    "added_only_qa_sha256":{x:hashlib.sha256((ROOT/x).read_bytes()).hexdigest() for x in changed},
    "original_direct_Csharp9_assertions_each_OS":12,"Native_Unity_G1":"NOT_RUN",
    "SOURCE_G0":"PENDING_B01_40","source_QA":"ACCEPTED_TESTED_SCOPE_ONLY",
    "game_private_510_Rig_Ledger":"NOT_IMPLEMENTED","G2":"HOLD","Codex_ACK":"NONE",
    "release":False,"merge":False}
 a.out.parent.mkdir(parents=True,exist_ok=True);a.out.write_text(json.dumps(r,indent=2,sort_keys=True)+"\n")
 print(json.dumps(r,sort_keys=True))
if __name__=="__main__":main()
