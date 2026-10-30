#!/usr/bin/env python3
"""B01-61 original W13 producer contract audit, exact original Git blob and QA-only scope."""
from pathlib import Path
import subprocess
ROOT=Path(__file__).resolve().parents[4]
SOURCE="c2c179317af8155d17a1027283fe991565779ede"
FROZEN="d94400d95d13db7f27e1533df05c9292d5380431"
W06="cb2b3a76e84266e2730fffc4ab048c1039f0652f"
SCHEMA="Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs"
SCHEMA_BLOB="6c036985411164372bf3d21d467780ff3777544e"
W06FILE="Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs"
W06BLOB="fa1aa5e531927cbe73cf5fdfe161b4a981316db5"
HOST="Assets/B0123SecureReattachV2/Runtime/SecureDebrisPlaygroundV2.cs"
HOSTBLOB="328977b4cdcdcd7c9f128c01f11ff67f8e46086a"
D="Tools/B01/B01-61/P17W13ContractBoundaryQA/"
OWN={D+n for n in ["Program.cs","OriginalProducerSchemaQA.csproj","source_guard.py","README.md","manifest.json"]}
OWN.add(".github/workflows/b01-61-p17w13-contract-boundary.yml")
def git(*a):return subprocess.check_output(["git","-C",str(ROOT),*a],text=True).strip()
def check(b,m):
 if not b:raise AssertionError(m)
def main():
 diff=set(filter(None,git("diff","--name-only",SOURCE,"HEAD").splitlines()))
 check(diff==OWN,"QA_SCOPE_DIFFERENCE "+str(sorted(diff^OWN)))
 for path,blob,ref in [(SCHEMA,SCHEMA_BLOB,SOURCE),(W06FILE,W06BLOB,W06),(HOST,HOSTBLOB,FROZEN)]:
  check(git("rev-parse",ref+":"+path)==blob,"FROZEN_PROVENANCE_DRIFT "+path)
  check(git("rev-parse","HEAD:"+path)==blob,"QA_MUTATED_ORIGINAL_SOURCE "+path)
  check(git("hash-object",path)==blob,"WORKTREE_ORIGINAL_SOURCE_DRIFT "+path)
 schema=(ROOT/SCHEMA).read_text()
 check("public readonly PersistedPart[] Parts;" in schema,
       "SOURCE_SNAPSHOT_EXPOSURE_CHANGED_NEW_SHA_RETEST_REQUIRED")
 check("public DebrisImpulseLedger GetLedger(int slot)=>rebuilt[slot];" in schema,
       "SOURCE_W06_LEDGER_EXPOSURE_CHANGED_NEW_SHA_RETEST_REQUIRED")
 check("private readonly DebrisImpulseLedger[] rebuilt;" in schema,
       "ORIGINAL_W06_LEDGER_LINK_CHANGED")
 check("Current=ready;" in schema,"SESSION_PUBLISH_CHANGED")
 print("SOURCE_PROVENANCE_PASS 3/3 original source blobs; 6/6 new QA-only paths")
 print("SOURCE_MUTABILITY_NEGATIVE_CANDIDATES_PRESENT=2; unity_native=NOT_RUN")
if __name__=="__main__":main()
