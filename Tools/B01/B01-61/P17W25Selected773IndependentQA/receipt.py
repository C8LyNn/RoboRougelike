#!/usr/bin/env python3
"""Pin exact original 773 producer source, 729+556+W06+510 Git blobs and actual original OS stdout."""
import argparse,hashlib,json,subprocess,platform
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4]
BASE="3aaaec4027ee246fbfb6f209179f0e9507363473"
OWN="Tools/B01/B01-61/P17W25Selected773IndependentQA/"
WORKFLOW=".github/workflows/b01-61-w25-773-casefold-physical-original.yml"
PRODUCER={
"Tools/B01/B01-62/P17W24CanonicalPathFixR2/CanonicalSingleAuthorityStoreV3R2.cs":"3689238a16d706eda1bf4905cba8029cd2fe77aa",
"Tools/B01/B01-62/P17W24CanonicalPathFixR2/CanonicalSlotSessionV3R2.cs":"b3841132fbfc05da5708af4047f7db141a4abe91"}
ORIGINAL={
"CanonicalWorldGateway.cs":"21733278b6ab0669fccb6d085e0b6cf242792de2",
"CanonicalWorldSlotLease.cs":"a9331dd620336d8c42165d134dc8a5e05ed51873",
"DetachedSnapshotContract.cs":"b52e511c370f5fca56c74029050844dc8cbb7f9a",
"DebrisImpulseCore.cs":"fa1aa5e531927cbe73cf5fdfe161b4a981316db5",
"SecureRigSpec.cs":"b680ed0ec969ac23203fa55eab7b90f8daa86e43"}
def git(*args):return subprocess.check_output(["git",*args],cwd=ROOT,text=True,stderr=subprocess.DEVNULL).strip()
def main():
 p=argparse.ArgumentParser();p.add_argument("--log",required=True,type=Path);p.add_argument("--out",required=True,type=Path);a=p.parse_args()
 subprocess.run(["git","merge-base","--is-ancestor",BASE,"HEAD"],cwd=ROOT,check=True)
 for path,sha in PRODUCER.items():
  if git("rev-parse","HEAD:"+path)!=sha or git("hash-object",path)!=sha:raise AssertionError("SELECTED_773_PRODUCER_MUTATED "+path)
 for file,sha in ORIGINAL.items():
  if git("hash-object",str(ROOT/OWN/".original"/file))!=sha:raise AssertionError("FROZEN_SOURCE_PIN_DRIFT "+file)
 changed=git("diff","--name-only",BASE,"HEAD").splitlines()
 if len(changed)!=5 or len(set(changed))!=5 or any(not (x.startswith(OWN) or x==WORKFLOW) for x in changed):
  raise AssertionError("NOT_EXACTLY_5_ADDED_ONLY_B0161 "+repr(changed))
 for path in changed:
  if subprocess.run(["git","cat-file","-e",BASE+":"+path],cwd=ROOT,stderr=subprocess.DEVNULL).returncode==0:
   raise AssertionError("FROZEN_PRODUCER_OVERWRITE "+path)
 log=a.log.read_text(encoding="utf8")
 markers=["TOTAL 9 PASS 9 FAIL 0","PASS GREEN_773_ONE_CANONICAL_EXACT_STORE_AND_SECOND_CLR_EXCLUSION",
  "PASS GREEN_773_COLD_GEN2_EXACT_LEDGER_AND_SAME_GEN_FORK_REJECTED",
  "PASS GREEN_773_PARTIAL_ROOT_DISPOSE_OS_LOCK_RETAINED",
  "PASS GREEN_773_LOST_ACK_AFTER_REAL_DISK_RENAME_COLD_RESTART",
  "PASS W25_773_LINUX_CASEFOLD_PHYSICAL_SAVE_FORK_NEGATIVE_WITNESS",
  "QA_WITNESS_773_SOURCE_GATE=HOLD_IF_CASEFOLD_REPRODUCED_ON_POSIX",
  "SOURCE_G0_FIX773_HOLD_NATIVE_G1_NOT_RUN_PRIVATE_510_GAME_WIRING_HOLD_G2_HOLD"]
 if platform.system()=="Linux":
  markers+=["W25_773_LINUX_NONCANONICAL_CASE_PATH_TWO_REAL_SAVE_FILES=REPRODUCED",
     "W25_773_LINUX_SAME_RUN_GUID_GEN1_CONFLICTING_LIGHT_HEAVY_REPRODUCED"]
 elif platform.system()=="Windows":
  markers+=["W25_773_WINDOWS_CASE_ALIAS_SAME_PHYSICAL_DIR_DENIED=PASS"]
 else:raise AssertionError("UNSUPPORTED_OS")
 for m in markers:
  if m not in log:raise AssertionError("REAL_W25_773_SOURCE_WITNESS_MISSING "+m)
 receipt={"head_sha":git("rev-parse","HEAD"),"producer_773_sha":BASE,
   "base_selected_749_sha":"8634e71dfcf633323073fe584a6c0884f1ca5d23",
   "source_blob_pins":{**PRODUCER,**ORIGINAL},
   "qa_added_only_sha256":{x:hashlib.sha256((ROOT/x).read_bytes()).hexdigest() for x in changed},
   "os":platform.system(),"source_executed_assertions":9,
   "public_raw_store_source_gate":"RED_LINUX_CASEFOLD_FORK" if platform.system()=="Linux" else "WINDOWS_ALIAS_OS_REJECTED",
   "formal_G0":"HOLD_UNTIL_OWNER_FIX","native_Unity_G1":"NOT_RUN",
   "real_private_510_Rig_adapter":"NOT_CONNECTED","G2":"HOLD","codex_ack":"NONE",
   "merged":False,"released":False}
 a.out.parent.mkdir(parents=True,exist_ok=True)
 a.out.write_text(json.dumps(receipt,sort_keys=True,indent=2)+"\n")
 print(json.dumps(receipt,sort_keys=True))
if __name__=="__main__":main()
