#!/usr/bin/env python3
"""Read-only exact original source and owner-scope manifest gate for B01-60 W07."""
from __future__ import annotations
import json,subprocess,sys
from pathlib import Path

ROOT=Path(__file__).resolve().parents[5]
PRODUCER="4d13f76e6c0c08033e305900daf61c0a7267a725"
CODEX="203a0805251ed28293888e43f2d49f0df63767c6"
OWNER="Research/B01-24/P17/W06-SQUAD-PRESSURE-001/"
CORE="3402fe61153e307ba164340956afd76aec6e8603"
R3="66cd1ee734b129117b3753d1205469a0496cb673"

def git(*args):
 return subprocess.check_output(["git","-C",str(ROOT),*args],stderr=subprocess.STDOUT).decode().strip()
def expect(ok,message):
 if not ok:raise AssertionError(message)
def main():
 expect(git("rev-parse",PRODUCER+":"+OWNER+"SquadPressure.cs")==CORE,"PRODUCER_CODE_DRIFT")
 expect(git("rev-parse",CODEX+":Assets/R3/R3EnemyArchetype.cs")==R3,"CODEX_R3_ORIGINAL_DRIFT")
 expect(git("rev-parse",PRODUCER+":Assets/R3/R3EnemyArchetype.cs")==R3,"PRODUCER_BASE_NOT_CODEX_R3")
 expect(git("rev-parse","HEAD:"+OWNER+"SquadPressure.cs")==CORE,"QA_CHANGED_PRODUCER_CODE")
 expect(git("rev-parse","HEAD:Assets/R3/R3EnemyArchetype.cs")==R3,"QA_CHANGED_ORIGINAL_CODEX_R3")
 raw=git("show",PRODUCER+":"+OWNER+"manifest.json")
 m=json.loads(raw)
 assert m["package_id"]=="B01-24-SQUAD-PRESSURE-001" and m["version"]=="1.0.0"
 assert m["original_codex"]["source_commit"]==CODEX
 assert m["original_codex"]["original_R3_git_blob"]==R3
 assert len(m["files"])==7
 for entry in m["files"]:
  p=entry["path"]
  expect(p.startswith(OWNER) or p==".github/workflows/b01-24-p17-w06-squad-pressure.yml","PRODUCER_SCOPE")
  expect(git("rev-parse",PRODUCER+":"+p)==entry["blob"],"ORIGINAL_MANIFEST_BLOB_"+p)
  expect(git("rev-parse","HEAD:"+p)==entry["blob"],"QA_TOUCHES_PRODUCER_"+p)
 changes=git("diff","--name-only",PRODUCER,"HEAD").splitlines()
 expect(len(changes)>=3,"INDEPENDENT_QA_FILES_MISSING")
 for p in changes:
  expect(p.startswith("Tools/B01/B01-60/P17W07/SquadPressureQA/") or
         p==".github/workflows/b01-60-p17w07-squad-pressure-independent.yml",
         "OWNER_SCOPE_COLLISION: "+p)
 print(json.dumps({"status":"SOURCE_PINS_PASS","producer_pr":429,"source_sha":PRODUCER,
                   "code_blob":CORE,"codex_sha":CODEX,"r3_blob":R3,
                   "original_manifest_blobs":len(m["files"]),
                   "qa_only_paths":len(changes),"native_unity":"NOT_RUN"},sort_keys=True))
if __name__=="__main__":
 try:main()
 except Exception as e:
  print("B01_60_W07_PIN_FAIL "+repr(e),file=sys.stderr)
  sys.exit(2)
