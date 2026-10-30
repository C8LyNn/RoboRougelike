#!/usr/bin/env python3
"""Original R4 #647 exact producer source and QA-only scope."""
from pathlib import Path
import subprocess
ROOT=Path(__file__).resolve().parents[4]
BASE="09c1809b73324a6d31c13e5d0143de62ac1e3ca6"
D="Tools/B01/B01-61/P17W20R4ComponentOnlyDestroyQA/"
OWN={D+n for n in ("Program.cs","SyntheticUnityEngine.cs","OriginalR4HostQA.csproj","source_guard.py","manifest.json","README.md")}
OWN.add(".github/workflows/b01-61-p17w20-r4-647-component-only-destroy-qa.yml")
PINS={
"Assets/B0123UnitySaveBridge001/Core/AtomicWorldSwap.cs":"8fd1dd9a1bc870433809da8fba897ec924fa4113",
"Assets/B0123UnitySaveBridge001/Unity/IsolatedTwinRigSaveHost.cs":"82147492a9205f8ef5986ceb61aeb10d6ea24ff2",
"Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs":"b52e511c370f5fca56c74029050844dc8cbb7f9a",
"Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs":"fa1aa5e531927cbe73cf5fdfe161b4a981316db5",
"Assets/B0123SecureReattachV2/Runtime/SecureRigSpec.cs":"b680ed0ec969ac23203fa55eab7b90f8daa86e43"
}
def git(*args):return subprocess.check_output(["git",*args],text=True,cwd=ROOT).strip()
def main():
 subprocess.run(["git","merge-base","--is-ancestor",BASE,"HEAD"],cwd=ROOT,check=True)
 files=set(filter(None,git("diff","--name-only",BASE,"HEAD").splitlines()))
 assert files==OWN,"WRONG_SCOPE "+repr(sorted(files^OWN))
 for path,blob in PINS.items():
  assert git("rev-parse",BASE+":"+path)==blob,"PRODUCER_NOT_PINNED "+path
  assert git("rev-parse","HEAD:"+path)==blob,"SOURCE_MUTATED "+path
  assert git("hash-object",path)==blob,"SOURCE_WORKTREE_DRIFT "+path
 tree=git("ls-tree","-r","--name-only","HEAD","Assets/B0123UnitySaveBridge001/Core")
 assert not any(x in tree for x in ["HostSlotLease.cs","CheckpointFence.cs"]),"ALTERNATIVE_IMPORTED"
 print("EXACT_ORIGINAL_R4_647_PRODUCER_SHA="+BASE)
 print("B0161_W20_ORIGINAL_SHA_PIN_PASS=5/5 QA_ONLY_SCOPE_PASS=7/7")
 print("R4_ONLY; NO_652_NO_644_NO_604; G1_NATIVE_NOT_RUN")
if __name__=="__main__":main()
