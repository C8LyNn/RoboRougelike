#!/usr/bin/env python3
"""Exact Git provenance + independent source closure, never test synthetic PR merge as SHA acceptance."""
from pathlib import Path
import subprocess
ROOT=Path(__file__).resolve().parents[4]
PRODUCER="ad7904eca571c742257551fa8103f266fcb9b752"
SAVE="33094f154c7b91119b36d58aab285e6e83c5de57"
GAME="d94400d95d13db7f27e1533df05c9292d5380431"
W06="cb2b3a76e84266e2730fffc4ab048c1039f0652f"
PIN=[
("Assets/B0123UnitySaveBridge001/Core/AtomicWorldSwap.cs",PRODUCER,"16a7d7043a2ecb59a41bbfb063170235872fd7fc"),
("Assets/B0123UnitySaveBridge001/Unity/IsolatedTwinRigSaveHost.cs",PRODUCER,"ff5da20e3c98ab3b540f37fb5652666c47228ed3"),
("Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs",SAVE,"b52e511c370f5fca56c74029050844dc8cbb7f9a"),
("Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs",W06,"fa1aa5e531927cbe73cf5fdfe161b4a981316db5"),
("Assets/B0123SecureReattachV2/Runtime/AtomicDockLedger.cs",GAME,"b6a4d04b0c6407c8c84451b4d599d96fe0e4d1a9"),
("Assets/B0123SecureReattachV2/Runtime/SecureRigSpec.cs",GAME,"b680ed0ec969ac23203fa55eab7b90f8daa86e43")]
DIR="Tools/B01/B01-61/P17W16ExactBridgeAcceptance/"
OWNED={DIR+n for n in ("Program.cs","Original587W16QA.csproj","source_guard.py","README.md","manifest.json")}
OWNED.add(".github/workflows/b01-61-p17w16-exact587-acceptance.yml")
def git(*a):return subprocess.check_output(["git","-C",str(ROOT),*a],text=True).strip()
def need(ok,m):
 if not ok:raise AssertionError(m)
def main():
 parents=git("rev-list","--parents","-n","1","HEAD").split()
 qahead=parents[2] if len(parents)==3 else "HEAD"
 changed=set(filter(None,git("diff","--name-only",PRODUCER,qahead).splitlines()))
 need(changed==OWNED,"QA_SCOPE_DIFF "+repr(sorted(changed^OWNED)))
 for path,ref,expected in PIN:
  need(git("rev-parse",ref+":"+path)==expected,"ORIGINAL_REF_PIN "+path)
  need(git("rev-parse","HEAD:"+path)==expected,"BASE_DRIFT_PRODUCER_NOT_ACCEPTED "+path)
  need(git("hash-object",path)==expected,"WORKTREE_DRIFT "+path)
 code=(ROOT/PIN[0][0]).read_text()
 need("snapshot.Generation<currentCold.Snapshot.Generation" in code,
      "EQUAL_GENERATION_BEHAVIOR_CHANGED_NEW_SHA_RETEST")
 need("candidate.Activate();" in code and
      "if(!Consistent(candidate.Capture(),snapshot))" in code,
      "ORIGINAL_PRODUCER_POSTACTIVATION_READBACK_CHANGED")
 unity=(ROOT/PIN[1][0]).read_text()
 need("part.SetActive(true);" in unity and "PhysicalIdle" in unity,
      "UNITY_HOST_FIXED_W15_SOURCE_BLOB_CHANGED")
 print("B0161_W16_EXACT_SOURCE_PINS_PASS 6/6 original Git blobs; QA_ONLY_SCOPE 6/6")
 print("SELECTED_SCHEMA_556="+SAVE+" SELECTED_GAME_510="+GAME)
 print("NATIVE_UNITY_PHYSX_NOT_RUN; G2_HOLD; CODEX_ACK_NONE")
if __name__=="__main__":main()
