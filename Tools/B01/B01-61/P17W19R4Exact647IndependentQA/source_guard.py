#!/usr/bin/env python3
"""B01-61 W19 original owner R4 #647 EXACT source+Unity host SHA, no #644."""
from pathlib import Path
import subprocess
ROOT=Path(__file__).resolve().parents[4]
P="09c1809b73324a6d31c13e5d0143de62ac1e3ca6"
SAVE="33094f154c7b91119b36d58aab285e6e83c5de57"
GAME="d94400d95d13db7f27e1533df05c9292d5380431"
W06="cb2b3a76e84266e2730fffc4ab048c1039f0652f"
D="Tools/B01/B01-61/P17W19R4Exact647IndependentQA/"
OWN={D+n for n in ("Program.cs","LifecycleProgram.cs","SyntheticUnityEngine.cs","R4IndependentQA.csproj","R4UnityLifecycleQA.csproj","source_guard.py","README.md","manifest.json")}
OWN.add(".github/workflows/b01-61-w19-r4-647-independent-qa.yml")
PINS=[
("Assets/B0123UnitySaveBridge001/Core/AtomicWorldSwap.cs",P,"8fd1dd9a1bc870433809da8fba897ec924fa4113"),
("Assets/B0123UnitySaveBridge001/Unity/IsolatedTwinRigSaveHost.cs",P,"82147492a9205f8ef5986ceb61aeb10d6ea24ff2"),
("Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs",SAVE,"b52e511c370f5fca56c74029050844dc8cbb7f9a"),
("Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs",W06,"fa1aa5e531927cbe73cf5fdfe161b4a981316db5"),
("Assets/B0123SecureReattachV2/Runtime/SecureRigSpec.cs",GAME,"b680ed0ec969ac23203fa55eab7b90f8daa86e43")]
def git(*a):return subprocess.check_output(["git",*a],cwd=ROOT,text=True).strip()
def need(a,m):
 if not a:raise AssertionError(m)
def main():
 need(git("rev-parse","HEAD^")==P,"EXACT_OWNER_R4_PARENT_MISMATCH")
 changed=set(filter(None,git("diff","--name-only",P,"HEAD").splitlines()))
 need(changed==OWN,"QA_ONLY_SCOPE_MISMATCH "+repr(sorted(changed^OWN)))
 for path,ref,blob in PINS:
  need(git("rev-parse",ref+":"+path)==blob,"ORIGINAL_REF_PIN_"+path)
  need(git("rev-parse","HEAD:"+path)==blob,"ORIGINAL_PRODUCER_MUTATION_"+path)
  need(git("hash-object",path)==blob,"WORKTREE_ORIGINAL_DRIFT_"+path)
 tree=git("ls-tree","-r","--name-only","HEAD","Assets/B0123UnitySaveBridge001/Core")
 need("HostSlotLease.cs" not in tree and "CheckpointFence.cs" not in tree,
      "FORBIDDEN_R3_ALT_644_OR_604_IMPORTED")
 core=(ROOT/PINS[0][0]).read_text()
 host=(ROOT/PINS[1][0]).read_text()
 need("commitAttempted=true;" in core and "if(commitAttempted)" in core,
      "R4_COMMIT_FAULT_SOURCE_CHANGED_NEW_SHA")
 need("OnDisable(){/* keep slot lifetime owner while a world survives */}" in host and
      "OnDestroy(){ReleaseSlot();}" in host,
      "R4_HOST_LEASE_LIFECYCLE_CHANGED_NEW_SHA")
 print("R4_ORIGINAL_SOURCE_GIT_PINS_PASS=5/5 QA_ONLY_SCOPE_PASS=9/9")
 print("OWNER_SELECTED_R3_642_BASE; NEW_VERSION_R4_647="+P)
 print("ALT_R3_644_AND_604_ABSENT; NATIVE_G1_NOT_RUN")
if __name__=="__main__":main()
