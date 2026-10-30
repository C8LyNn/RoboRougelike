#!/usr/bin/env python3
"""Exact original candidate #642 R3 direct source, no alternative #644 or #604."""
from pathlib import Path
import subprocess
R=Path(__file__).resolve().parents[4]
P="22cec9888047994ae523cbe7bf1b0a483d64cec4"
F="33094f154c7b91119b36d58aab285e6e83c5de57"
G="d94400d95d13db7f27e1533df05c9292d5380431"
W="cb2b3a76e84266e2730fffc4ab048c1039f0652f"
D="Tools/B01/B01-61/P17W19R3Exact642UncertainCommitQA/"
OWN={D+x for x in ("Program.cs","R3IndependentQA.csproj","source_guard.py","README.md","manifest.json")}
OWN.update({D+"SyntheticUnityEngine.cs",D+"LifecycleProgram.cs",D+"R3UnityLifecycleQA.csproj"})
OWN.add(".github/workflows/b01-61-w19-r3-642-independent-commit-fault.yml")
PINS=[
 ("Assets/B0123UnitySaveBridge001/Core/AtomicWorldSwap.cs",P,"a0a1923216d88ff8f50c560196f82cdec694f15c"),
 ("Assets/B0123UnitySaveBridge001/Unity/IsolatedTwinRigSaveHost.cs",P,"7b8354bc4cd846227b4bdf6d25d81bed34a325bc"),
 ("Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs",F,"b52e511c370f5fca56c74029050844dc8cbb7f9a"),
 ("Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs",W,"fa1aa5e531927cbe73cf5fdfe161b4a981316db5"),
 ("Assets/B0123SecureReattachV2/Runtime/SecureRigSpec.cs",G,"b680ed0ec969ac23203fa55eab7b90f8daa86e43")]
def g(*a):return subprocess.check_output(["git",*a],cwd=R,text=True).strip()
def need(x,s):
 if not x:raise AssertionError(s)
def main():
 subprocess.run(["git","merge-base","--is-ancestor",P,"HEAD"],cwd=R,check=True)
 changes=set(filter(None,g("diff","--name-only",P,"HEAD").splitlines()))
 need(changes==OWN,"QA_ONLY_SCOPE_MISMATCH "+repr(sorted(changes^OWN)))
 for path,ref,blob in PINS:
  need(g("rev-parse",ref+":"+path)==blob,"SOURCE_BASE_PIN "+path)
  need(g("rev-parse","HEAD:"+path)==blob,"UNAUTHORIZED_SOURCE_CHANGE "+path)
  need(g("hash-object",path)==blob,"WORKTREE_SOURCE_CHANGE "+path)
 tree=g("ls-tree","-r","--name-only","HEAD","Assets/B0123UnitySaveBridge001/Core")
 need("CheckpointFence.cs" not in tree and "HostSlotLease.cs" not in tree,
      "COMPETING_R3_OR_604_SOURCE_MIXED")
 c=(R/PINS[0][0]).read_text()
 need("publishedGeneration" in c and "publishedHash" in c and
      "catch{verdict=WorldSwapVerdict.HostException;}" in c,
      "R3_TRANSACTION_SOURCE_BEHAVIOR_MOVED_RETEST")
 print("EXACT_ORIGINAL_CANDIDATE_642_SHA="+P)
 print("R3_SOURCE_PINS_PASS=5/5 QA_ONLY_PATHS_PASS=9/9")
 print("FROZEN_SELECTED_510_556_W06_UNCHANGED; NATIVE_G1_NOT_RUN")
if __name__=="__main__":main()
