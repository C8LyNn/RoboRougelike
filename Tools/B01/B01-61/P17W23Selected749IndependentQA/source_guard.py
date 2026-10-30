#!/usr/bin/env python3
"""W23 B01-61 exact selected #749 Git provenance and reproducible security scope."""
from pathlib import Path
import subprocess,hashlib,json
R=Path(__file__).resolve().parents[4]
B="8634e71dfcf633323073fe584a6c0884f1ca5d23"
D="Tools/B01/B01-61/P17W23Selected749IndependentQA/"
OWN={D+x for x in ("source_guard.py","independent_checks.py","README.md","manifest.json")}
OWN.add(".github/workflows/b01-61-w23-selected749-original-source.yml")
PINS={
"Tools/B01/B01-62/P17W23OneAuthorityV3/CanonicalSlotSessionV3.cs":"9d86581626c8657b6662cb281f7621fa43343fb3",
"Tools/B01/B01-62/P17W23OneAuthorityV3/CanonicalSingleAuthorityStoreV3.cs":"f8fab0b35147a23ae31bcb4a0cf91590d0896d97",
"Tools/B01/B01-62/P17W23OneAuthorityV3/Program.cs":"e573e6473eafb752b5bc590a193481a771bd8f63",
"Tools/B01/B01-62/P17W23OneAuthorityV3/Original729OneAuthorityQA.csproj":"8d89968881d00845957de00848ec3fa5a859f6de"}
def git(*a):return subprocess.check_output(["git",*a],cwd=R,text=True).strip()
def main():
 subprocess.run(["git","merge-base","--is-ancestor",B,"HEAD"],check=True,cwd=R)
 diff=set(filter(None,git("diff","--name-only",B,"HEAD").splitlines()))
 assert diff==OWN,("SCOPE_NOT_QA_ONLY",sorted(diff^OWN))
 for p,h in PINS.items():
  assert git("rev-parse",B+":"+p)==h,(p,"producer changed")
  assert git("rev-parse","HEAD:"+p)==h,(p,"source mutated")
  assert git("hash-object",p)==h,(p,"worktree changed")
 assert git("rev-parse","HEAD:Assets/B0123CanonicalSaveApi001/Core/CanonicalWorldGateway.cs")=="21733278b6ab0669fccb6d085e0b6cf242792de2"
 assert git("rev-parse","HEAD:Assets/B0123CanonicalSaveApi001/Core/CanonicalWorldSlotLease.cs")=="a9331dd620336d8c42165d134dc8a5e05ed51873"
 print("W23_SELECTED_749_ORIGINAL_EXACT_SHA="+B)
 print("W23_SELECTED_749_PINNED_ORIGINAL_6_6_PASS AND QA_ONLY_5_5_PASS")
if __name__=="__main__":main()
