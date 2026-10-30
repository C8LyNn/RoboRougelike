#!/usr/bin/env python3
"""Exact OWNER-selected 729 V3 + original external 669 source SHA provenance."""
import subprocess
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4]
BASE="b8ca29c7707cd6b4338d0b9772d74dee676d49d2"
D="Tools/B01/B01-61/P17W23Canonical729AuthorityQA/"
OWN={D+x for x in ("Program.cs","Exact729And669QA.csproj","source_guard.py","README.md","manifest.json")}
OWN.add(".github/workflows/b01-61-w23-exact729-dual-lease-qa.yml")
PIN={
"Assets/B0123CanonicalSaveApi001/Core/CanonicalWorldGateway.cs":"21733278b6ab0669fccb6d085e0b6cf242792de2",
"Assets/B0123CanonicalSaveApi001/Core/CanonicalWorldSlotLease.cs":"a9331dd620336d8c42165d134dc8a5e05ed51873",
"Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs":"b52e511c370f5fca56c74029050844dc8cbb7f9a",
"Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs":"fa1aa5e531927cbe73cf5fdfe161b4a981316db5",
"Assets/B0123SecureReattachV2/Runtime/SecureRigSpec.cs":"b680ed0ec969ac23203fa55eab7b90f8daa86e43"
}
EXTERNAL="db7745635164d9eb6b10f3246538700158130b7b"
def git(*args):return subprocess.check_output(["git",*args],cwd=ROOT,text=True).strip()
def main():
 subprocess.run(["git","merge-base","--is-ancestor",BASE,"HEAD"],cwd=ROOT,check=True)
 actual=set(filter(None,git("diff","--name-only",BASE,"HEAD").splitlines()))
 assert actual==OWN,("MUTATED_FOREIGN_SOURCE",sorted(actual^OWN))
 for p,sha in PIN.items():
  assert git("rev-parse",BASE+":"+p)==sha,("PRODUCER_BASE_DRIFT",p)
  assert git("rev-parse","HEAD:"+p)==sha,("PRODUCER_CHANGED",p)
  assert git("hash-object",p)==sha,("DIRTY_ORIGINAL",p)
 assert git("hash-object",str(ROOT/D/".source/SingleFileSaveSlot.cs"))==EXTERNAL,"ORIGINAL_669_DRIFT"
 print("W23_EXACT_SOLE_729_ORIGINAL_SHA="+BASE)
 print("W23_ORIGINAL_GIT_BLOBS_6_6_PASS B01_61_OWN_ONLY_6_6_PASS")
 print("ALT_719_682_695_NONE; SINGLE_INTEGRATION_AUTHORITY_NOT_ASSUMED")
if __name__=="__main__":main()
