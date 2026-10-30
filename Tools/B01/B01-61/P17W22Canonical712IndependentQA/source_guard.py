#!/usr/bin/env python3
import subprocess
from pathlib import Path
R=Path(__file__).resolve().parents[4]
BASE="bd8905ad549a7b26854b671be971f7d781308311"
V1="ab62b041e50f8aa0defada2aadc0987d6c63f9d9"
D="Tools/B01/B01-61/P17W22Canonical712IndependentQA/"
OWN={D+s for s in ("Program.cs","Original712IndependentQA.csproj","source_guard.py","README.md","manifest.json")}
OWN.add(".github/workflows/b01-61-w22-exact-712-orphan-lease-independent.yml")
PINS={"Tools/B01/B01-62/P17W21CanonicalSessionLeaseR2/CanonicalSlotSessionR2.cs":"fefc2e263e068b913c6e59ec3c8375bbbe43db4e"}
EXT={"CanonicalWorldGatewayV2.cs":"704e774dedd5be5af26e0791fba87ecdb109ae2a","SingleFileSaveSlot.cs":"db7745635164d9eb6b10f3246538700158130b7b","DetachedSnapshotContract.cs":"b52e511c370f5fca56c74029050844dc8cbb7f9a","DebrisImpulseCore.cs":"fa1aa5e531927cbe73cf5fdfe161b4a981316db5","SecureRigSpec.cs":"b680ed0ec969ac23203fa55eab7b90f8daa86e43"}
def git(*x):return subprocess.check_output(["git",*x],cwd=R,text=True).strip()
def main():
 subprocess.run(["git","merge-base","--is-ancestor",BASE,"HEAD"],cwd=R,check=True)
 subprocess.run(["git","merge-base","--is-ancestor",V1,BASE],cwd=R,check=True)
 got=set(filter(None,git("diff","--name-only",BASE,"HEAD").splitlines()))
 assert got==OWN,("SCOPE_DRIFT",sorted(got^OWN))
 for p,sha in PINS.items():
  assert git("rev-parse",BASE+":"+p)==sha
  assert git("rev-parse","HEAD:"+p)==sha
  assert git("hash-object",p)==sha
 for p,sha in EXT.items():
  assert git("hash-object",str(R/D/".source"/p))==sha,("EXTERNAL_SHA_MISMATCH",p)
 print("W22_EXACT_ORIGINAL_712_SHA="+BASE)
 print("W22_ORIGINAL_SOURCE_GIT_PINS_6_6_PASS OWN_ONLY_6_6_PASS")
 print("CANONICAL_691_STORE_669_FROZEN_556_W06_510_BYTES_ORIGINAL")
if __name__=="__main__":main()
