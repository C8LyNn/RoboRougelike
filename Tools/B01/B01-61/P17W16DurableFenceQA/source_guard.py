#!/usr/bin/env python3
"""Actual W16 producer source pin, original frozen #510/#556/W06, strict 6-file QA scope."""
from pathlib import Path
import subprocess
ROOT=Path(__file__).resolve().parents[4]
P="7d206c63e1e9ab4ee4ce52eb314507ce49721d2c"
SAVE="33094f154c7b91119b36d58aab285e6e83c5de57"
GAME="d94400d95d13db7f27e1533df05c9292d5380431"
W06="cb2b3a76e84266e2730fffc4ab048c1039f0652f"
PINS=[
("Assets/B0123UnitySaveBridge001/Core/AtomicWorldSwap.cs",P,"399d3cb2074a2659af8f4e941edb28a3d2dd2832"),
("Assets/B0123UnitySaveBridge001/Core/CheckpointFence.cs",P,"53f907125b09531ebdf5ffdfbb9c5c9b48eca217"),
("Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs",SAVE,"b52e511c370f5fca56c74029050844dc8cbb7f9a"),
("Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs",W06,"fa1aa5e531927cbe73cf5fdfe161b4a981316db5"),
("Assets/B0123SecureReattachV2/Runtime/AtomicDockLedger.cs",GAME,"b6a4d04b0c6407c8c84451b4d599d96fe0e4d1a9"),
("Assets/B0123SecureReattachV2/Runtime/SecureRigSpec.cs",GAME,"b680ed0ec969ac23203fa55eab7b90f8daa86e43")]
D="Tools/B01/B01-61/P17W16DurableFenceQA/"
OWN={D+n for n in ("Program.cs","OriginalW16FenceQA.csproj","source_guard.py","README.md","manifest.json")}
OWN.add(".github/workflows/b01-61-p17w16-durable-checkpoint-fence.yml")
def git(*x):return subprocess.check_output(["git","-C",str(ROOT),*x],text=True).strip()
def check(b,m):
 if not b:raise AssertionError(m)
def main():
 parents=git("rev-list","--parents","-n","1","HEAD").split()
 head=parents[2] if len(parents)==3 else "HEAD"
 observed=set(filter(None,git("diff","--name-only",P,head).splitlines()))
 check(observed==OWN,"QA_SCOPE_INVALID "+repr(sorted(OWN^observed)))
 for path,ref,blob in PINS:
  check(git("rev-parse",ref+":"+path)==blob,"ORIGINAL_REF_DRIFT "+path)
  check(git("rev-parse","HEAD:"+path)==blob,"UNAUTHORIZED_PRODUCER_SOURCE_CHANGE "+path)
  check(git("hash-object",path)==blob,"ORIGINAL_WORKTREE_DRIFT "+path)
 core=(ROOT/PINS[0][0]).read_text()
 check("snapshot.Generation<=currentCold.Snapshot.Generation" in core,
       "NEW_VERSION_DIFF_EQUALGEN_POLICY_RETEST")
 check("return quarantined?WorldSwapVerdict.RollbackQuarantine:verdict;" in core,
       "NEW_VERSION_DIFF_QUARANTINE_POLICY_RETEST")
 check("checkpoint.Seal(" in core,"NEW_VERSION_DIFF_SEAL_POLICY_RETEST")
 fence=(ROOT/PINS[1][0]).read_text()
 check("public sealed class FileCheckpointFence" in fence and "stream.Flush(true);" in fence,
       "ACTUAL_ORIGINAL_DISK_FENCE_MISSING")
 print("B0161_W16_SOURCE_PINS_PASS=6/6; QA_ONLY_ADDED_PATHS=6/6")
 print("W16_PRODUCER="+P+" FROZEN_SAVE="+SAVE+" FROZEN_GAME="+GAME)
 print("NATIVE_UNITY_NOT_RUN; CANONICAL_510_HOST_NOT_INTEGRATED; NO_MERGE")
if __name__=="__main__":main()
