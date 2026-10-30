#!/usr/bin/env python3
"""Independent B01-61 exact selected #554 original source/QA-only provenance guard."""
from pathlib import Path
import subprocess
ROOT=Path(__file__).resolve().parents[4]
SELECTED="72b39a7bf214fc6b76e95853159950c3991ddfa3"
P510="d94400d95d13db7f27e1533df05c9292d5380431"
W06="cb2b3a76e84266e2730fffc4ab048c1039f0652f"
P549="6da2c523e03766cd5a6deaed0cb3cccf8765354b"
OWN="Tools/B01/B01-61/P17W14SelectedV2IntegrityQA/"
FILES={OWN+n for n in ["Program.cs","OriginalSelectedV2QA.csproj","source_guard.py","README.md","manifest.json"]}
FILES.add(".github/workflows/b01-61-p17w14-selected-v2-554-qa.yml")
PINS=[
 ("Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs",SELECTED,
  "4cd6e2d7312d09fb658863a35ed4fc7fa4c88720"),
 ("Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs",W06,
  "fa1aa5e531927cbe73cf5fdfe161b4a981316db5"),
 ("Assets/B0123SecureReattachV2/Runtime/AtomicDockLedger.cs",P510,
  "b6a4d04b0c6407c8c84451b4d599d96fe0e4d1a9"),
 ("Assets/B0123SecureReattachV2/Runtime/SecureRigSpec.cs",P510,
  "b680ed0ec969ac23203fa55eab7b90f8daa86e43")
]
def g(*args):
 return subprocess.check_output(["git","-C",str(ROOT),*args],text=True).strip()
def require(x,m):
 if not x:raise AssertionError(m)
def main():
 diff=set(filter(None,g("diff","--name-only",SELECTED,"HEAD").splitlines()))
 require(diff==FILES,"QA_SCOPE_NOT_EXCLUSIVE "+repr(sorted(diff^FILES)))
 for path,ref,sha in PINS:
  require(g("rev-parse",ref+":"+path)==sha,"IMMUTABLE_SOURCE_REF_FAIL "+path)
  require(g("rev-parse","HEAD:"+path)==sha,"ORIGINAL_PRODUCER_MUTATION "+path)
  require(g("hash-object",path)==sha,"WORKTREE_PRODUCER_BYTES_CHANGED "+path)
 old=g("show",P549+":Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs")
 require("public DebrisImpulseLedger GetLedger(int slot)" in old,
         "HISTORICAL_550_GET_LEDGER_RED_PIN_MISSING")
 new=(ROOT/PINS[0][0]).read_text()
 require("private readonly PersistedPart[] parts;" in new and
         "public PersistedPart[] Parts => (PersistedPart[])parts.Clone();" in new,
         "ORIGINAL_550_SNAPSHOT_FIX_NOT_FOUND")
 require("public bool IsConsumed(int slot,long id)" in new and
         "public int ConsumedCount(int slot)" in new and
         "public DebrisImpulseLedger GetLedger(" not in new,
         "ORIGINAL_550_LEDGER_FIX_NOT_FOUND")
 require("private bool swapping;" in new and
         new.index("idle=hostIdle();") < new.index("swapping=true;"),
         "SOURCE_REENTRANT_HOSTIDLE_WITNESS_CHANGED")
 print("PINNED_EXACT_SELECTED_W14_SOURCE=4/4; QA_ONLY_FILES=6/6")
 print("SELECTED_PRODUCER_SHA="+SELECTED+"; FROZEN_GAMEPLAY_SHA="+P510)
 print("SOURCE_FIX_550_PRESENT=YES; HOSTIDLE_REENTRANCY_NEGATIVE_TO_EXECUTE=YES")
 print("UNITY_NATIVE=NOT_RUN; ACK=NONE; NO_MERGE")
if __name__=="__main__":main()
