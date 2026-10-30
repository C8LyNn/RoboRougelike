#!/usr/bin/env python3
"""B01-61 W15 test only: pin actual producer AtomicWorldSwap, #556, #510, W06."""
from pathlib import Path
import subprocess
ROOT=Path(__file__).resolve().parents[4]
CORE_REF="39f03fd6a0d5fae10be5419db4beb79201147bc8"
SAVE_REF="33094f154c7b91119b36d58aab285e6e83c5de57"
GAME_REF="d94400d95d13db7f27e1533df05c9292d5380431"
W06_REF="cb2b3a76e84266e2730fffc4ab048c1039f0652f"
PIN=[
("Assets/B0123UnitySaveBridge001/Core/AtomicWorldSwap.cs",CORE_REF,"db69bae2bf4cf106da68f13e0b067499f0da02f3"),
("Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs",SAVE_REF,"b52e511c370f5fca56c74029050844dc8cbb7f9a"),
("Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs",W06_REF,"fa1aa5e531927cbe73cf5fdfe161b4a981316db5"),
("Assets/B0123SecureReattachV2/Runtime/AtomicDockLedger.cs",GAME_REF,"b6a4d04b0c6407c8c84451b4d599d96fe0e4d1a9"),
("Assets/B0123SecureReattachV2/Runtime/SecureRigSpec.cs",GAME_REF,"b680ed0ec969ac23203fa55eab7b90f8daa86e43")]
D="Tools/B01/B01-61/P17W15OriginalAtomicBridgeQA/"
FILES={D+n for n in ("Program.cs","OriginalBridgeQA.csproj","source_guard.py","README.md","manifest.json")}
FILES.add(".github/workflows/b01-61-p17w15-atomic-bridge-source.yml")
def g(*args):return subprocess.check_output(["git","-C",str(ROOT),*args],text=True).strip()
def ensure(ok,why):
 if not ok:raise AssertionError(why)
def main():
 # PR workflows can checkout a synthetic merge whose first parent contains
 # extra producer changes; isolate owned PR HEAD from its merge second parent.
 parents=g("rev-list","--parents","-n","1","HEAD").split()
 own_head=parents[2] if len(parents)==3 else "HEAD"
 diff=set(filter(None,g("diff","--name-only",CORE_REF,own_head).splitlines()))
 ensure(diff==FILES,"INDEPENDENT_QA_OWNERSHIP_VIOLATION "+repr(sorted(diff^FILES)))
 for path,ref,expected in PIN:
  ensure(g("rev-parse",ref+":"+path)==expected,"ORIGINAL_GIT_BLOB_PIN_CHANGED "+path)
  ensure(g("rev-parse","HEAD:"+path)==expected,"ORIGINAL_PRODUCER_MODIFIED "+path)
  ensure(g("hash-object",path)==expected,"ORIGINAL_WORKTREE_BYTES_CHANGED "+path)
 src=(ROOT/PIN[0][0]).read_text()
 ensure("snapshot.Generation<currentCold.Snapshot.Generation" in src,
        "HOST_GENERATION_SEMANTICS_CHANGED_RETEST_NEW_VERSION")
 ensure("candidate=factory.Prepare(snapshot);" in src and
        "currentCold=restored;" in src and "quarantined=true;" in src,
        "ORIGINAL_NEW_BRIDGE_ATOMIC_TRANSACTION_CHANGED")
 print("SOURCE_GIT_PROVENANCE_PASS=5/5 ORIGINAL B01-23 NEW BRIDGE + #556 + #510 + W06")
 print("QA_PATHS_ONLY=6/6, producer source unchanged, no Codex edits")
 print("ACTUAL_UNITY_PHYSX_NOT_RUN; NO_MERGE; CODEX_ACK_NONE")
if __name__=="__main__":main()
