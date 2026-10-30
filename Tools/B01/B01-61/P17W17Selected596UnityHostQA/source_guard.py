#!/usr/bin/env python3
"""Independent B01-61 W17 exact one selected #596. Reject competing #604 source."""
from pathlib import Path
import subprocess
ROOT=Path(__file__).resolve().parents[4]
PRODUCER="efb2b06eafedab5469b57f163152d96aca3c5915"
SAVE="33094f154c7b91119b36d58aab285e6e83c5de57"
GAME="d94400d95d13db7f27e1533df05c9292d5380431"
W06="cb2b3a76e84266e2730fffc4ab048c1039f0652f"
PINS=[
("Assets/B0123UnitySaveBridge001/Core/AtomicWorldSwap.cs",PRODUCER,"1f9e883c38101f214e4335867faa731f7ae67689"),
("Assets/B0123UnitySaveBridge001/Unity/IsolatedTwinRigSaveHost.cs",PRODUCER,"4bac6f327c9b93ae98dd2061c0ca8ea2100aab42"),
("Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs",SAVE,"b52e511c370f5fca56c74029050844dc8cbb7f9a"),
("Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs",W06,"fa1aa5e531927cbe73cf5fdfe161b4a981316db5"),
("Assets/B0123SecureReattachV2/Runtime/SecureRigSpec.cs",GAME,"b680ed0ec969ac23203fa55eab7b90f8daa86e43")]
D="Tools/B01/B01-61/P17W17Selected596UnityHostQA/"
OWN={D+n for n in ("SyntheticUnityEngine.cs","Original596UnityHostQA.csproj",
                      "Program.cs","source_guard.py","README.md","manifest.json")}
OWN.add(".github/workflows/b01-61-p17w17-selected596-monobehaviour.yml")
def g(*args):return subprocess.check_output(["git","-C",str(ROOT),*args],text=True).strip()
def check(x,m):
 if not x:raise AssertionError(m)
def main():
 parents=g("rev-list","--parents","-n","1","HEAD").split()
 ownhead=parents[2] if len(parents)==3 else "HEAD"
 diff=set(filter(None,g("diff","--name-only",PRODUCER,ownhead).splitlines()))
 check(diff==OWN,"QA_EXCLUSIVE_OWNERSHIP_CHANGED "+repr(sorted(diff^OWN)))
 for path,ref,blob in PINS:
  check(g("rev-parse",ref+":"+path)==blob,"ORIGINAL_SELECTED_REF_BLOB_CHANGED "+path)
  check(g("rev-parse","HEAD:"+path)==blob,"ORIGINAL_SELECTED_PRODUCER_CHANGED "+path)
  check(g("hash-object",path)==blob,"WORKTREE_SELECTED_ORIGINAL_CHANGED "+path)
 src=(ROOT/PINS[0][0]).read_text()
 unity=(ROOT/PINS[1][0]).read_text()
 check("public sealed class FileCheckpointStore" in src and
       "saveGeneration=next;" in src and "checkpoint.Commit(" in src,
       "NEW_SELECTED_596_COMMIT_OR_SAVE_GENERATION_DIFF")
 check('new FileCheckpointStore(sidecar)' in unity and
       "b0123_one_save_host_v2.checkpoint" in unity and
       "FindObjectsByType<MonoBehaviour>" in unity,
       "ACTUAL_SELECTED_UNITY_HOST_BOUNDARY_CHANGED_NEW_SHA_REQUIRED")
 check("Assets/B0123UnitySaveBridge001/Core/CheckpointFence.cs" not in
       g("ls-tree","-r","--name-only","HEAD","Assets/B0123UnitySaveBridge001/Core/"),
       "UNSELECTED_604_FILE_COINSTALLED")
 print("W17_ONLY_SELECTED_596_GIT_ORIGINAL_PINS_PASS=5/5")
 print("W17_QA_ONLY_NEW_PATHS_PASS=7/7; COMPETING_604_NOT_INSTALLED")
 print("REAL_UNITY_EDITOR_PHYSX=NOT_RUN; SYNTHETIC_UNITY_API_ONLY")
if __name__=="__main__":main()
