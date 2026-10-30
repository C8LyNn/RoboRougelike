#!/usr/bin/env python3
from pathlib import Path
import subprocess
R=Path(__file__).resolve().parents[4]
BASE="75f1631f9a36ce51b0f8edc0c7857e924cdf78ec"
D="Tools/B01/B01-61/P17W23Bridge741MarkerFixQA/"
OWN={D+x for x in ("Program.cs","Independent741MigrationQA.csproj","source_guard.py","README.md","manifest.json")}
OWN.add(".github/workflows/b01-61-w23-741-v2-marker-exclusion.yml")
PINS={
"Tools/B01/B01-62/P17W23OneAuthorityBridgeR3/CanonicalSessionR3.cs":"589079745a758467572d9ee8aa2bfc09d2aab63f",
"Tools/B01/B01-62/P17W23OneAuthorityBridgeR3/SingleAuthoritySaveSlotR3.cs":"52400444d32f7baadb1f8ae439e7ce7dd9e29665"
}
SOURCES={
"CanonicalWorldGatewayV3.cs":"21733278b6ab0669fccb6d085e0b6cf242792de2",
"CanonicalWorldSlotLease.cs":"a9331dd620336d8c42165d134dc8a5e05ed51873",
"DetachedSnapshotContract.cs":"b52e511c370f5fca56c74029050844dc8cbb7f9a",
"DebrisImpulseCore.cs":"fa1aa5e531927cbe73cf5fdfe161b4a981316db5",
"SecureRigSpec.cs":"b680ed0ec969ac23203fa55eab7b90f8daa86e43",
"Original669SingleFileSaveSlot.cs":"db7745635164d9eb6b10f3246538700158130b7b"
}
def git(*a):return subprocess.check_output(["git",*a],cwd=R,text=True).strip()
def main():
 subprocess.run(["git","merge-base","--is-ancestor",BASE,"HEAD"],cwd=R,check=True)
 changed=set(filter(None,git("diff","--name-only",BASE,"HEAD").splitlines()))
 assert changed==OWN,("QA_ONLY_SCOPE_CHANGED",sorted(changed^OWN))
 for f,sha in PINS.items():
  assert git("rev-parse",BASE+":"+f)==sha,("PRODUCER_FILE_CHANGED",f)
  assert git("rev-parse","HEAD:"+f)==sha,("PRODUCER_EDITED",f)
  assert git("hash-object",f)==sha,("PRODUCER_WORKTREE_CHANGED",f)
 for f,sha in SOURCES.items():
  assert git("hash-object",str(R/D/".source"/f))==sha,("ALTERNATIVE_OR_DRIFT",f)
 print("B0161_W23_EXACT_R3_741_BASE="+BASE)
 print("B0161_W23_ORIGINAL_BLOBS_8_8 PASS QA_ONLY_6_6")
 print("SELECTED_V3_729+R3_741+UNMODIFIED_669_SIMULATED_COINSTALL; NATIVE_G1_NOT_RUN")
if __name__=="__main__":main()
