#!/usr/bin/env python3
"""Exact original #923 parent and 4 own add-only files; source only, no Unity."""
import pathlib,subprocess,sys
ROOT=pathlib.Path(__file__).resolve().parents[4]
PARENT="e6eb24b7a45a66a5fa5d039f59c80b7e4be4439b"
GAME="3bd9b159997924dcecb4e1cd1d41137896e5199e"
PREFIX="Tools/B01/B01-60/P17W34R3PhysicalIdentity/"
PATHS={PREFIX+x for x in ("Program.cs","Original923OsQA.csproj","source_gate.py","README.md")}
PATHS.add(".github/workflows/b01-60-w34-original-923-physical-id.yml")
PINS={
 "Tools/B01/B01-62/P17W33R3World/CanonicalR3WorldV1.cs":"029021b32538c309fc0e85eb48ca5202c96e239c",
 "Tools/B01/B01-62/P17W28UnityCompatibleWholeGameV3/WholeGameEconomySlotV3.cs":"65e58e579becedb86f1162d9a511508cb77dfb7b",
 "Tools/B01/B01-62/P17W33R3World/R3WorldActualOsQA.csproj":"PLACEHOLDER_ORIGINAL_PROJECT_BLOB"
}
def git(*args):
 return subprocess.check_output(("git","-C",str(ROOT),*args),stderr=subprocess.PIPE).decode().strip()
def failif(test,why):
 if test:raise RuntimeError(why)
def check():
 failif(git("rev-parse","HEAD^")!=PARENT,"parent MUST equal immutable original B01-62 #923 exact")
 names=[tuple(x.split("\t",1)) for x in git("diff","--name-status",PARENT,"HEAD").splitlines()]
 failif(set(names)!={("A",p) for p in PATHS} or len(names)!=len(PATHS),
     "ONLY 5 B01-60 QA new files; no B01-62 original writer or Codex game changes")
 for path,sha in PINS.items():
  if sha!="PLACEHOLDER_ORIGINAL_PROJECT_BLOB":
   failif(git("rev-parse","HEAD:"+path)!=sha,"original #923 production blob mutated: "+path)
 original=git("show",GAME+":Assets/R3/R3VehicleCatalog.cs")
 failif("class R3VehicleCatalog" not in original or "R3VehicleKind.Light" not in original,
     "original live game factory not available for independent OS QA")
 source=(ROOT/PREFIX/"Program.cs").read_text()
 for marker in ("StablePartIdsAcrossTwoAtomicGenerationsAndActualColdRead",
   "SameOwnedGuidCanChangeItsPersistentPhysicalLedgerId",
   "DuplicatePhysicalIdsWithinGenerationAreStillRejected",
   "WholeGameEconomySlotV3.Create","CanonicalColdReadV3.ReadClosedSlot",
   "R3VehicleCatalog.Create(R3VehicleKind.Light)",
   "oldPhysical+1000000","B0160_W34_SOURCE_WITNESS_PASS_PRODUCT_R3_LEDGER_IDENTITY_P0_RED",
   "B0160_W34_NATIVE_UNITY=NOT_RUN"):
  failif(marker not in source,"missing real existing Save/codec OS witness: "+marker)
 print("B0160_W34_ORIGINAL_923_EXACT_PARENT_AND_FIVE_QA_ADD_ONLY_PATHS_PASS")
 print("B0160_W34_OS_SOURCE_NEGATIVE_TEST_AUTHORED_NOT_UNITY")
if __name__=="__main__":
 try:check()
 except Exception as ex:print("B0160_W34_SOURCE_GATE_RED "+str(ex),file=sys.stderr);sys.exit(3)
