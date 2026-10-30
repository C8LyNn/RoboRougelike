#!/usr/bin/env python3
"""B01-61 W21 exact selected #681 descendant #691 V2 original provenance."""
from pathlib import Path
import subprocess
R=Path(__file__).resolve().parents[4]
P="809f82575c638e1b4d2806b5f417bddab6c0871a"
V1="eed2e39636edfd8b8db754c426db452164cbf9d7"
D="Tools/B01/B01-61/P17W21CanonicalV2IndependentQA/"
OWN={D+x for x in ("Program.cs","CanonicalV2IndependentQA.csproj","source_guard.py","README.md","manifest.json")}
OWN.add(".github/workflows/b01-61-w21-canonical691-v2-negative-qa.yml")
PINS={
"Assets/B0123CanonicalSaveApi001/Core/CanonicalWorldGateway.cs":"704e774dedd5be5af26e0791fba87ecdb109ae2a",
"Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs":"b52e511c370f5fca56c74029050844dc8cbb7f9a",
"Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs":"fa1aa5e531927cbe73cf5fdfe161b4a981316db5"
}
def git(*args):return subprocess.check_output(["git",*args],cwd=R,text=True).strip()
def main():
 subprocess.run(["git","merge-base","--is-ancestor",P,"HEAD"],cwd=R,check=True)
 subprocess.run(["git","merge-base","--is-ancestor",V1,P],cwd=R,check=True)
 diff=set(filter(None,git("diff","--name-only",P,"HEAD").splitlines()))
 assert diff==OWN,("ONLY_SIX_NEW_B01_61_QA_PATHS",sorted(diff^OWN))
 for f,sha in PINS.items():
  assert git("rev-parse",P+":"+f)==sha,("WRONG_BASE",f)
  assert git("rev-parse","HEAD:"+f)==sha,("PRODUCER_MUTATED",f)
  assert git("hash-object",f)==sha,("WORKTREE_CHANGED",f)
 alt="Tools/B01/B01-23/P17W20Canonical510Port001/Canonical510OwnerPort.cs"
 assert alt not in git("ls-tree","-r","--name-only","HEAD","Tools/B01/B01-23"),"ALT_682_COINSTALLED"
 print("W21_EXACT_SELECTED_V2_691_SOURCE_SHA="+P)
 print("W21_V2_PRODUCER_ORIGINAL_PINS_PASS=3/3 QA_ONLY_PATHS_PASS=6/6")
 print("SELECTED_681_ANCESTOR; ALT_682_NOT_INSTALLED; NATIVE_G1_NOT_RUN")
if __name__=="__main__":main()
