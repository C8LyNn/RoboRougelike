#!/usr/bin/env python3
"""B01-61 independent selected owner R5 exact-source and own QA-only scope."""
from pathlib import Path
import subprocess
R=Path(__file__).resolve().parents[4]
P="cc7f73541517a18f4479bb155534bd4832d9985e"
D="Tools/B01/B01-61/P17W21R5OwnerLifecycleQA/"
OWN={D+n for n in ("Program.cs","SyntheticUnityEngine.cs","OriginalR5HostQA.csproj","source_guard.py","README.md","manifest.json")}
OWN.add(".github/workflows/b01-61-w21-r5-690-owner-lifecycle.yml")
PIN={
"Assets/B0123UnitySaveBridge001/Core/AtomicWorldSwap.cs":"7da9a9efe489cf8c441d7bb9a47ba3f60ba4c58c",
"Assets/B0123UnitySaveBridge001/Unity/IsolatedTwinRigSaveHost.cs":"6b3936d82b5e1f175856a9e138124b14c9dfd19c",
"Assets/B0123UnitySaveBridge001/Core/OwnerWorldLeaseRetirement.cs":"1ef69de9b4029ce89ba2db5bf3331708da1b91e4",
"Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs":"b52e511c370f5fca56c74029050844dc8cbb7f9a",
"Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs":"fa1aa5e531927cbe73cf5fdfe161b4a981316db5",
"Assets/B0123SecureReattachV2/Runtime/SecureRigSpec.cs":"b680ed0ec969ac23203fa55eab7b90f8daa86e43"
}
def git(*args):return subprocess.check_output(["git",*args],cwd=R,text=True).strip()
def main():
 subprocess.run(["git","merge-base","--is-ancestor",P,"HEAD"],cwd=R,check=True)
 files=set(filter(None,git("diff","--name-only",P,"HEAD").splitlines()))
 assert files==OWN,("B01_61_QA_PATH_SCOPE",sorted(files^OWN))
 for path,blob in PIN.items():
  assert git("rev-parse",P+":"+path)==blob,("PIN_CHANGED",path)
  assert git("rev-parse","HEAD:"+path)==blob,("ORIGINAL_R5_MUTATED",path)
  assert git("hash-object",path)==blob,("ORIGINAL_WORKTREE_CHANGED",path)
 print("B01_61_W21_EXACT_R5_SHA="+P)
 print("B01_61_W21_PRODUCER_FROZEN_GIT_PINS_PASS=6/6 QA_ONLY_SCOPE_PASS=7/7")
 print("R5_NO_R4_652_OR_R3_644_OR_604_MIX; ACTUAL_NATIVE_UNITY=NOT_RUN")
if __name__=="__main__":main()
