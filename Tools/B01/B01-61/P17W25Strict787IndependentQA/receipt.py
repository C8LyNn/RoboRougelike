#!/usr/bin/env python3
"""Independent original #787 strict source cross-OS receipt; verify raw Linux alias rejected BEFORE disk mutation."""
import argparse,hashlib,json,subprocess,platform
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4]
BASE="f5db40bf293fe9582ec7c6fcb3515aeff6fcf937"
OWN="Tools/B01/B01-61/P17W25Strict787IndependentQA/"
FLOW=".github/workflows/b01-61-w25-787-strict-casefold-original.yml"
PRODUCER={
"Tools/B01/B01-62/P17W25StrictCanonicalPathR3/CanonicalSingleAuthorityStoreV3R3.cs":"bc34c2df4375ac7abdd638e5db7f7a238e8fa2fd",
"Tools/B01/B01-62/P17W25StrictCanonicalPathR3/CanonicalSlotSessionV3R3.cs":"0fa335a67346ae600df163ec69c90e7550945ba1"}
FROZEN={
"CanonicalWorldGateway.cs":"21733278b6ab0669fccb6d085e0b6cf242792de2",
"CanonicalWorldSlotLease.cs":"a9331dd620336d8c42165d134dc8a5e05ed51873",
"DetachedSnapshotContract.cs":"b52e511c370f5fca56c74029050844dc8cbb7f9a",
"DebrisImpulseCore.cs":"fa1aa5e531927cbe73cf5fdfe161b4a981316db5",
"SecureRigSpec.cs":"b680ed0ec969ac23203fa55eab7b90f8daa86e43"}
def git(*x):return subprocess.check_output(["git",*x],cwd=ROOT,text=True,stderr=subprocess.DEVNULL).strip()
def main():
 p=argparse.ArgumentParser();p.add_argument("--log",required=True,type=Path);p.add_argument("--out",required=True,type=Path);a=p.parse_args()
 subprocess.run(["git","merge-base","--is-ancestor",BASE,"HEAD"],cwd=ROOT,check=True)
 for path,sha in PRODUCER.items():
  if git("rev-parse","HEAD:"+path)!=sha or git("hash-object",path)!=sha:
   raise AssertionError("NEW_787_PRODUCER_SHA_DRIFT "+path)
 for name,sha in FROZEN.items():
  if git("hash-object",str(ROOT/OWN/".original"/name))!=sha:
   raise AssertionError("ORIGINAL_729_556_510_SHA_DRIFT "+name)
 changed=git("diff","--name-only",BASE,"HEAD").splitlines()
 if len(changed)!=5 or len(set(changed))!=5 or any(not(x.startswith(OWN) or x==FLOW) for x in changed):
  raise AssertionError("QA_787_NOT_EXACT_5_NEW_FILES "+repr(changed))
 for path in changed:
  if subprocess.run(["git","cat-file","-e",BASE+":"+path],cwd=ROOT,stderr=subprocess.DEVNULL).returncode==0:
   raise AssertionError("QA_OVERWRITES_OWNER_SOURCE "+path)
 actual=a.log.read_text(encoding="utf8")
 required=["TOTAL 9 PASS 9 FAIL 0",
   "PASS GREEN_787_POSIX_CASEFOLD_ALTERNATE_RAW_PUBLIC_STORE_DENIED",
   "W25_787_ONE_TRUE_CANONICAL_SAVE_G1_AFTER_COLD_RETAKE=PASS",
   "QA_787_SOURCE_GATE=GREEN_STRICT_CANONICAL_ROOT_ONLY",
   "SOURCE_G0_787_COMBINED_PENDING_NATIVE_G1_NOT_RUN_PRIVATE_510_GAME_WIRING_HOLD_G2_HOLD"]
 if platform.system()=="Linux":
  required+=["W25_787_POSIX_CASEFOLD_ALTERNATE_PHYSICAL_DIR_DENIED_GREEN=PASS"]
 elif platform.system()=="Windows":
  required+=["W25_787_WINDOWS_CASEFOLD_RAW_ALIAS_DENIED_BEFORE_DISK_GREEN=PASS"]
 else:raise AssertionError("OS_NOT_IN_QA_SCOPE")
 for mark in required:
  if mark not in actual:raise AssertionError("REAL_ORIGINAL_787_CSHARP_STDOUT_MISSING "+mark)
 output={"qa_sha":git("rev-parse","HEAD"),"owner_candidate_787_sha":BASE,
  "prior_773_P0":"RED_LINUX_RAW_CASE_ALIAS","original_blobs":{**PRODUCER,**FROZEN},
  "qa_5_new_only_sha256":{x:hashlib.sha256((ROOT/x).read_bytes()).hexdigest() for x in changed},
  "os":platform.system(),"original_direct_source_assertions":9,
  "source_casefold_original_root":"GREEN_SCOPE_ONLY","G0_formal":"PENDING_B01_40",
  "native_Unity_G1":"NOT_RUN","full_game_SaveLoad":"HOLD","codex_ack":"NONE",
  "integrated":False,"released":False}
 a.out.parent.mkdir(parents=True,exist_ok=True);a.out.write_text(json.dumps(output,indent=2,sort_keys=True)+"\n")
 print(json.dumps(output,sort_keys=True))
if __name__=="__main__":main()
