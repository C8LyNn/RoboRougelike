#!/usr/bin/env python3
"""Read-only Git-object intake for B01-24 original enemy data vs Codex #383 game."""
import json, subprocess, sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
PRODUCER="c39a400cb07ae0fd6f0d8d419a128490cfbcfd8a"
CODEX="203a0805251ed28293888e43f2d49f0df63767c6"
ORIGINAL={
 "Assets/R5/R5Arena.cs":"fbe1537036910e0dcc2b2c7e502e66848fcd14a5",
 "Assets/R1/R1EncounterManager.cs":"0a1c36b246c059a8bd9ec93a9d531cbe916b64bf",
 "Assets/R1/R1RunDefinition.cs":"5665e64d3d665c67d352eb5a9809a66fc5136698",
 "Assets/R5/R5RunPlan.cs":"6fde58f1fc621cde6488d9ee81c3510aa729839a",
}
CONTENT={
 "Research/B01-24/P17/ENEMY-FORMATIONS-002/formations.json":"4c28b6289f23bcbd82c976bccb1567cbcb847371",
 "Research/B01-24/P17/ENEMY-FORMATIONS-002/formation_tool.mjs":"5d1231249ce15a3ee4de7e5abebc5b0fe7147f8c",
 "Research/B01-24/P17/ENEMY-FORMATIONS-002/formation_tool.test.mjs":"8a84494f5bb77270b8fff1799f382f069c7d7b87",
}
def git(*args):
 return subprocess.check_output(["git","-C",str(ROOT),*args],text=True).strip()
def check(ok,what):
 if not ok:raise AssertionError(what)
def main():
 check(subprocess.run(["git","-C",str(ROOT),"merge-base","--is-ancestor",PRODUCER,"HEAD"]).returncode==0,"PRODUCER_NOT_ANCESTOR")
 for path,expected in {**ORIGINAL,**CONTENT}.items():
  actual=git("rev-parse","HEAD:"+path)
  check(actual==expected,"PIN_MISMATCH "+path+" "+actual)
  if path in ORIGINAL:
   original=git("rev-parse",CODEX+":"+path)
   check(original==expected,"CODEX_383_SOURCE_DRIFT "+path+" "+original)
 for path in CONTENT:
  check(git("rev-parse",PRODUCER+":"+path)==CONTENT[path],"MOVED_PRODUCER_DATA "+path)
 source=(ROOT/"Assets/R1/R1EncounterManager.cs").read_text(encoding="utf-8")
 check("Vector3[] spawnPositions=null" in source and "spawnPositions[i]" in source,"SPAWN_API_CONTRACT_CHANGED")
 runplan=(ROOT/"Assets/R5/R5RunPlan.cs").read_text(encoding="utf-8")
 check("public enum R5ArenaKind { SalvageYard, Wasteland, Industrial }" in runplan,"ARENA_ENUM_CHANGED")
 check("IsBoss?1:Mathf.Clamp(EnemyCount+1,1,4)" in
       (ROOT/"Assets/R1/R1RunDefinition.cs").read_text(encoding="utf-8"),
       "SPAWN_COUNT_RULE_CHANGED")
 report={"status":"PIN_PASS","producer":PRODUCER,"codex_gameplay":CODEX,
         "source_blobs":len(ORIGINAL),"content_blobs":len(CONTENT),
         "codex_source_equal":True,"native_unity":"NOT_RUN"}
 print(json.dumps(report,indent=2))
 return 0
if __name__=="__main__":
 try:sys.exit(main())
 except Exception as e:
  print("QA_SOURCE_BLOCKED: "+str(e),file=sys.stderr)
  sys.exit(2)
