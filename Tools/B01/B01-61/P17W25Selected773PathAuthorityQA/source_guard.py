#!/usr/bin/env python3
"""Exact original #773 producer AND selected #729 source; new QA files only."""
from pathlib import Path
import subprocess
R=Path(__file__).resolve().parents[4]
BASE="3aaaec4027ee246fbfb6f209179f0e9507363473"
PARENT="8634e71dfcf633323073fe584a6c0884f1ca5d23"
D="Tools/B01/B01-61/P17W25Selected773PathAuthorityQA/"
OWN={D+x for x in ("Program.cs","Original773PathAuthorityQA.csproj","source_guard.py","README.md","manifest.json")}
OWN.add(".github/workflows/b01-61-w25-exact773-raw-path-qa.yml")
PINS={
"Tools/B01/B01-62/P17W24CanonicalPathFixR2/CanonicalSingleAuthorityStoreV3R2.cs":"3689238a16d706eda1bf4905cba8029cd2fe77aa",
"Tools/B01/B01-62/P17W24CanonicalPathFixR2/CanonicalSlotSessionV3R2.cs":"b3841132fbfc05da5708af4047f7db141a4abe91",
"Assets/B0123CanonicalSaveApi001/Core/CanonicalWorldGateway.cs":"21733278b6ab0669fccb6d085e0b6cf242792de2",
"Assets/B0123CanonicalSaveApi001/Core/CanonicalWorldSlotLease.cs":"a9331dd620336d8c42165d134dc8a5e05ed51873",
"Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs":"b52e511c370f5fca56c74029050844dc8cbb7f9a",
"Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs":"fa1aa5e531927cbe73cf5fdfe161b4a981316db5",
"Assets/B0123SecureReattachV2/Runtime/SecureRigSpec.cs":"b680ed0ec969ac23203fa55eab7b90f8daa86e43"
}
def git(*a):return subprocess.check_output(["git",*a],cwd=R,text=True).strip()
def main():
 for ref in (PARENT,BASE):
  subprocess.run(["git","merge-base","--is-ancestor",ref,"HEAD"],cwd=R,check=True)
 changed=set(filter(None,git("diff","--name-only",BASE,"HEAD").splitlines()))
 assert changed==OWN,("EXCLUSIVE_QA_PATHS",sorted(changed^OWN))
 for f,b in PINS.items():
  assert git("rev-parse",BASE+":"+f)==b,("ORIGINAL_BASE_DRIFT",f)
  assert git("rev-parse","HEAD:"+f)==b,("PRODUCER_MODIFIED",f)
  assert git("hash-object",f)==b,("WORKTREE_DRIFT",f)
 print("B01_61_W25_EXACT_773_SHA="+BASE)
 print("B01_61_W25_SEVEN_ORIGINAL_GIT_PINS_PASS=7/7 OWN_ONLY=6/6")
 print("SOURCE_QA_NO_ALTERNATIVE_741_753; NATIVE_UNITY_NOT_RUN")
if __name__=="__main__":main()
