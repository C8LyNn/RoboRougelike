#!/usr/bin/env python3
"""New versioned selected #749-only descendant R2 scope/immutable original source gate."""
import hashlib,json,subprocess
from pathlib import Path
BASE="8634e71dfcf633323073fe584a6c0884f1ca5d23"
PINS={
"Tools/B01/B01-62/P17W23OneAuthorityV3/CanonicalSingleAuthorityStoreV3.cs":"f8fab0b35147a23ae31bcb4a0cf91590d0896d97",
"Tools/B01/B01-62/P17W23OneAuthorityV3/CanonicalSlotSessionV3.cs":"9d86581626c8657b6662cb281f7621fa43343fb3",
"Assets/B0123CanonicalSaveApi001/Core/CanonicalWorldGateway.cs":"21733278b6ab0669fccb6d085e0b6cf242792de2",
"Assets/B0123CanonicalSaveApi001/Core/CanonicalWorldSlotLease.cs":"a9331dd620336d8c42165d134dc8a5e05ed51873",
"Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs":"b52e511c370f5fca56c74029050844dc8cbb7f9a",
"Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs":"fa1aa5e531927cbe73cf5fdfe161b4a981316db5",
"Assets/B0123SecureReattachV2/Runtime/SecureRigSpec.cs":"b680ed0ec969ac23203fa55eab7b90f8daa86e43",
}
def git(*a):return subprocess.check_output(["git",*a],stderr=subprocess.PIPE).decode().strip()
def main(path):
 out=Path(path)
 if out.exists():raise AssertionError("OCCUPIED_RECEIPT")
 subprocess.run(["git","merge-base","--is-ancestor",BASE,"HEAD"],check=True)
 root="Tools/B01/B01-62/P17W24CanonicalPathFixR2/"
 required={root+x for x in ("CanonicalSingleAuthorityStoreV3R2.cs",
 "CanonicalSlotSessionV3R2.cs","Program.cs","Selected749CanonicalPathFixR2.csproj",
 "README.md","manifest.json","source_guard.py")}
 required.add(".github/workflows/b01-62-w24-749-canonical-path-r2.yml")
 changed=set(git("diff","--name-only",BASE,"HEAD").splitlines())
 if changed!=required:raise AssertionError("FOREIGN_CHANGE_OR_MISSING:"+repr(changed^required))
 for p,sha in PINS.items():
  if git("rev-parse",BASE+":"+p)!=sha or git("rev-parse","HEAD:"+p)!=sha:
   raise AssertionError("FROZEN_ORIGINAL_CHANGED:"+p)
 store=Path(root+"CanonicalSingleAuthorityStoreV3R2.cs").read_text()
 session=Path(root+"CanonicalSlotSessionV3R2.cs").read_text()
 for x in ("OwnerMatches(owner,run,slot,directory)","SingleStoreGate",
           "DuplicateStore(owner)","TryCreateNewCore(","TryOpenExistingCore(",
           "b0162_canonical_single_authority_v3","FileShare.None"):
  # FileShare.None is present only in original #729, store doesn't contain it.
  if x=="FileShare.None":continue
  if x not in store:raise AssertionError("R2_GUARD_MISSING:"+x)
 if "new CanonicalWorldGateway(gate,factory,this,authority)" not in session or
    "CanonicalSingleAuthorityStoreV3R2" not in session:
  raise AssertionError("R2_SESSION_NOT_SELECTED_ORIGINAL_V3")
 for alt in ("743e36478ae8cf99e8429af1a8209da9d7ca64de",
             "75f1631f9a36ce51b0f8edc0c7857e924cdf78ec"):
  if subprocess.run(["git","merge-base","--is-ancestor",alt,"HEAD"],
                    stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL).returncode==0:
   raise AssertionError("ALT_SOURCE_ANCESTOR_NOT_ALLOWED")
 data={"package":"B01-62-W24-SELECTED749-ONE-CANONICAL-PATH-004",
       "original_selected_parent":BASE,"original_unmodified_git_blobs":PINS,
       "new_only_paths":sorted(required),
       "selected_status":"NEW_VERSION_NOT_YET_OWNER_SELECTED",
       "no_alt_753_741":True,"native_unity":"NOT_RUN",
       "economy_save":"HOLD_NEW_ATOMIC_SCHEMA_REQUIRED","integrated":False,
       "released":False,"codex_ack":False}
 raw=(json.dumps(data,sort_keys=True,indent=2)+"\n").encode()
 out.write_bytes(raw)
 print("W24_749_R2_EXACT_ORIGINAL_GIT_PINS=7/7")
 print("W24_749_R2_ONLY_NEW_PATHS=8/8")
 print("W24_749_R2_SOURCE_RECEIPT_SHA256="+hashlib.sha256(raw).hexdigest())
if __name__=="__main__":
 import argparse
 a=argparse.ArgumentParser();a.add_argument("--receipt",required=True);z=a.parse_args()
 main(z.receipt)
