#!/usr/bin/env python3
"""Pin immutable R2 producer + original W06 Git blobs, restrict QA-owned scope."""
from pathlib import Path
import subprocess

ROOT=Path(__file__).resolve().parents[4]
R2="d94400d95d13db7f27e1533df05c9292d5380431"
W06="cb2b3a76e84266e2730fffc4ab048c1039f0652f"
PINS={
 "Assets/B0123SecureReattachV2/Runtime/SecureDebrisPlaygroundV2.cs":"328977b4cdcdcd7c9f128c01f11ff67f8e46086a",
 "Assets/B0123SecureReattachV2/Runtime/SecureDockPolicy.cs":"9f80f705475bd312f209033a57bd07e03bf95628",
 "Assets/B0123SecureReattachV2/Runtime/SecureRigSpec.cs":"b680ed0ec969ac23203fa55eab7b90f8daa86e43",
 "Assets/B0123SecureReattachV2/Runtime/AtomicDockLedger.cs":"b6a4d04b0c6407c8c84451b4d599d96fe0e4d1a9",
 "Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs":"fa1aa5e531927cbe73cf5fdfe161b4a981316db5"
}
D="Tools/B01/B01-61/P17W11AtomicR2Acceptance/"
OWN={D+n for n in ("Program.cs","OriginalR2AtomicHostQA.csproj",
                    "UnityBoundaryAndRealLedgerAdapter.cs","verify_source.py","README.md","manifest.json")}
OWN.add(".github/workflows/b01-61-p17w11-v2-atomic-r2-green.yml")
def git(*args):
 return subprocess.check_output(["git","-C",str(ROOT),*args],text=True).strip()
def require(test,msg):
 if not test:raise AssertionError(msg)
def main():
 changed=set(filter(None,git("diff","--name-only",R2,"HEAD").splitlines()))
 require(changed==OWN,"QA_SCOPE_MISMATCH "+str(sorted(changed^OWN)))
 for path,sha in PINS.items():
  parent=W06 if path.endswith("DebrisImpulseCore.cs") else R2
  require(git("rev-parse",parent+":"+path)==sha,"PRODUCER_GIT_PIN_CHANGED "+path)
  require(git("rev-parse","HEAD:"+path)==sha,"QA_CHANGED_ORIGINAL_SOURCE "+path)
  require(git("hash-object",path)==sha,"WORKTREE_ORIGINAL_FILE_CHANGED "+path)
 controller=(ROOT/"Assets/B0123SecureReattachV2/Runtime/SecureDebrisPlaygroundV2.cs").read_text()
 ledger=(ROOT/"Assets/B0123SecureReattachV2/Runtime/AtomicDockLedger.cs").read_text()
 command=(ROOT/"Assets/B0123SecureReattachV2/Runtime/SecureDockPolicy.cs").read_text()
 require("private bool CommitDock(int index)" in controller and
         "return rig.Ledger.TryReattach" in controller,
         "ORIGINAL_V2_ATOMIC_TRANSACTION_NOT_USED")
 require("consumed.RearmAfterReattach(id)" in ledger and
         ledger.index("consumed.RearmAfterReattach(id)")<ledger.index("applyPhysical();"),
         "ORIGINAL_ID_REARM_NOT_BEFORE_PHYSICAL_MUTATION")
 require("rollbackPhysical();" in ledger and "consumed.TryDetach(" in ledger and
         "finally { busy = false; }" in ledger,
         "MISSING_ATOMIC_ROLLBACK_OR_BUSY_RELEASE")
 require("DockResult.TransactionRejected" in command,
         "PUBLIC_ROUTE_TRANSACTION_DENIAL_RECEIPT_MISSING")
 for alias in ("public bool Reattach(int index) => DockGate().Reattach(index);",
               "public bool TrySafeReattach(int index) => DockGate().TrySafeReattach(index);",
               "public bool ReattachFromGui(int index) => DockGate().ReattachFromGui(index);"):
  require(alias in controller,"UNGUARDED_PUBLIC_ALIAS "+alias)
 print("PASS immutable original Git blob pins 5/5; scope 7/7; producer SHA "+R2)
 print("PASS original R2 rearm-first, rollback, same gate for all 3 public entrypoints")
 print("ORIGINAL_R2_SOURCE_QA_CONTRACT_PASS; NATIVE_UNITY_NOT_RUN")
if __name__=="__main__":main()
