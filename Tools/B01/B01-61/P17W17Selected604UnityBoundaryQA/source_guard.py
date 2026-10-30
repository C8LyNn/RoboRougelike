#!/usr/bin/env python3
"""B01-61 W17: exact real #604 Unity host + native test contract, no mixed #596."""
from pathlib import Path
import subprocess
ROOT=Path(__file__).resolve().parents[4]
PRODUCER="1aa8a59ce1bb4fcd998f0b2b1ce25dfaea333f0a"
SAVE="33094f154c7b91119b36d58aab285e6e83c5de57"
GAME="d94400d95d13db7f27e1533df05c9292d5380431"
W06="cb2b3a76e84266e2730fffc4ab048c1039f0652f"
PINS=[
("Assets/B0123UnitySaveBridge001/Core/AtomicWorldSwap.cs",PRODUCER,"399d3cb2074a2659af8f4e941edb28a3d2dd2832"),
("Assets/B0123UnitySaveBridge001/Core/CheckpointFence.cs",PRODUCER,"53f907125b09531ebdf5ffdfbb9c5c9b48eca217"),
("Assets/B0123UnitySaveBridge001/Unity/IsolatedTwinRigSaveHost.cs",PRODUCER,"5012e98bba786c6b9702992c317586a0015678c1"),
("Assets/B0123UnitySaveBridge001/Tests/IsolatedSaveWorldNativePlayModeTests.cs",PRODUCER,"9b836abacaa4d3a995f765e0c964e19173da1528"),
("Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs",SAVE,"b52e511c370f5fca56c74029050844dc8cbb7f9a"),
("Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs",W06,"fa1aa5e531927cbe73cf5fdfe161b4a981316db5"),
("Assets/B0123SecureReattachV2/Runtime/SecureRigSpec.cs",GAME,"b680ed0ec969ac23203fa55eab7b90f8daa86e43")]
D="Tools/B01/B01-61/P17W17Selected604UnityBoundaryQA/"
OWN={D+n for n in ("SyntheticUnityEngine.cs","Original604UnityHostQA.csproj","Program.cs","source_guard.py","README.md","manifest.json")}
OWN.add(".github/workflows/b01-61-p17w17-selected604-unity-boundary.yml")
def git(*args):
 return subprocess.check_output(["git","-C",str(ROOT),*args],text=True).strip()
def require(x,m):
 if not x:raise AssertionError(m)
def main():
 parents=git("rev-list","--parents","-n","1","HEAD").split()
 qhead=parents[2] if len(parents)==3 else "HEAD"
 diff=set(filter(None,git("diff","--name-only",PRODUCER,qhead).splitlines()))
 require(diff==OWN,"QA_EXCLUSIVE_SCOPE_INVALID "+repr(sorted(diff^OWN)))
 for path,ref,blob in PINS:
  require(git("rev-parse",ref+":"+path)==blob,"SELECTED_GIT_BLOB_REF_CHANGED "+path)
  require(git("rev-parse","HEAD:"+path)==blob,"SELECTED_SOURCE_CHANGED "+path)
  require(git("hash-object",path)==blob,"SOURCE_WORKTREE_BYTES_CHANGED "+path)
 native=(ROOT/PINS[3][0]).read_text()
 unity=(ROOT/PINS[2][0]).read_text()
 require('"W16Native_"+System.Guid.NewGuid().ToString("N")' in native,
         "AUTHORED_NATIVE_SLOT_BUG_FIX_DETECTED_NEW_SHA_REQUIRED")
 require("slot.Length>32" in unity and "UsePersistentCheckpointSlot" in unity,
         "UNITY_PUBLIC_SLOT_POLICY_CHANGED_NEW_SHA_REQUIRED")
 require("part.SetActive(true);" in unity and "TwinWorld.Build(snapshot,transform)" in unity,
         "ACTUAL_SELECTED_UNITY_PHYSICAL_HOST_CHANGED")
 require("new FileCheckpointFence(Path.Combine(folder,slot+\".fence\"))" in unity,
         "REAL_SELECTED_UNITY_DISK_FENCE_MISSING")
 print("W17_EXACT_ORIGINAL_604_GIT_BLOBS_PASS=7/7")
 print("W17_EXCLUSIVE_B01_61_QA_SCOPE_PASS=7/7")
 print("ONLY_604_PRODUCER="+PRODUCER+" ONLY_556_SAVE="+SAVE+" ONLY_510_GAME="+GAME)
 print("SYNTHETIC_UNITY_BOUNDARY_ONLY_NATIVE_EDIT_PLAY_PHYSX_NOT_RUN")
if __name__=="__main__":main()
