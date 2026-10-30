#!/usr/bin/env python3
"""B01-61 W13 immutable #510 source provenance; NO copying/patching producer logic."""
from pathlib import Path
import re,subprocess
ROOT=Path(__file__).resolve().parents[4]
PRODUCER="d94400d95d13db7f27e1533df05c9292d5380431"
W06="cb2b3a76e84266e2730fffc4ab048c1039f0652f"
PINS={
 "Assets/B0123SecureReattachV2/Runtime/SecureDebrisPlaygroundV2.cs":"328977b4cdcdcd7c9f128c01f11ff67f8e46086a",
 "Assets/B0123SecureReattachV2/Runtime/SecureDockPolicy.cs":"9f80f705475bd312f209033a57bd07e03bf95628",
 "Assets/B0123SecureReattachV2/Runtime/SecureRigSpec.cs":"b680ed0ec969ac23203fa55eab7b90f8daa86e43",
 "Assets/B0123SecureReattachV2/Runtime/AtomicDockLedger.cs":"b6a4d04b0c6407c8c84451b4d599d96fe0e4d1a9",
 "Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs":"fa1aa5e531927cbe73cf5fdfe161b4a981316db5"
}
FOLDER="Tools/B01/B01-61/P17W13OriginalReloadQA/"
OWNED={FOLDER+n for n in ("Program.cs","SyntheticUnityBoundary.cs","OriginalV2ReloadQA.csproj",
                              "source_guard.py","README.md","manifest.json")}
OWNED.add(".github/workflows/b01-61-p17w13-original-reload.yml")
def g(*args):return subprocess.check_output(["git","-C",str(ROOT),*args],text=True).strip()
def assert_ok(ok,msg):
 if not ok:raise AssertionError(msg)
def main():
 current=set(filter(None,g("diff","--name-only",PRODUCER,"HEAD").splitlines()))
 assert_ok(current==OWNED,"QA_ONLY_SCOPE_UNEXPECTED: "+str(sorted(current^OWNED)))
 for path,expected in PINS.items():
  ref=W06 if "DebrisImpulseCore" in path else PRODUCER
  assert_ok(g("rev-parse",ref+":"+path)==expected,"ORIGINAL_REF_DRIFT "+path)
  assert_ok(g("rev-parse","HEAD:"+path)==expected,"PRODUCER_SOURCE_CHANGED "+path)
  assert_ok(g("hash-object",path)==expected,"ORIGINAL_WORKTREE_CHANGED "+path)
 host=(ROOT/"Assets/B0123SecureReattachV2/Runtime/SecureDebrisPlaygroundV2.cs").read_text()
 ledger=(ROOT/"Assets/B0123SecureReattachV2/Runtime/AtomicDockLedger.cs").read_text()
 # Missing original-public persistent restore is intentionally a release HOLD, not an
 # invitation to change immutable #510 or silently use test-only reflection as game code.
 required=("public void BuildDemo()","public void ResetForNewRun()",
           "public bool Reattach(int index)","public bool TrySafeReattach(int index)",
           "public bool ReattachFromGui(int index)","public int AppliedCount(int index)")
 for name in required:assert_ok(name in host,"ORIGINAL_METHOD_REMOVED "+name)
 for forbidden in ("LoadSnapshot(","RestoreSnapshot(","ImportDetachedState(",
                   "ExportSnapshot(","SerializeDetachedState("):
  assert_ok(forbidden not in host,"PRODUCER_SAVE_API_NEW_SHA_REQUIRED "+forbidden)
 assert_ok("private readonly DebrisImpulseLedger consumed" in ledger and
           "consumed.ClearForNewRun();" in ledger and
           "finally { busy = false; }" in ledger,"ORIGINAL_W06_LIFECYCLE_CHANGED")
 print("PASS immutable #510 original C# Git SHA blobs 5/5 and 7/7 new QA-only paths")
 print("ORIGINAL_510_MISSING_PERSISTENT_REHYDRATION_API=CONFIRMED")
 print("SOURCE_GATE_RELOAD=HOLD; UNITY_NATIVE=NOT_RUN; CODEX_ACK=NONE")
if __name__=="__main__":main()
