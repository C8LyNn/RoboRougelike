#!/usr/bin/env python3
"""B01-61 W14: verify exact #544 original source; stage untouched #510/W06 C# into QA-only local Input.
No gameplay, frozen producer, real save, or Codex edits. The staged originals are NOT committed.
"""
from pathlib import Path
import subprocess
ROOT=Path(__file__).resolve().parents[4]
P544="e77529262b8f03c6e9f0e25b642038c351872044"
P510="d94400d95d13db7f27e1533df05c9292d5380431"
W06="cb2b3a76e84266e2730fffc4ab048c1039f0652f"
PREFIX="Tools/B01/B01-61/P17W14Candidate544QA/"
PROD="Tools/B01/B01-23/P17W13SecureV2SaveContract001/StableIdSnapshotCodec.cs"
PIN="2dd3ee7f465a595ed42c2cbb6637200bb903185f"
ORIGINALS=[
 ("DebrisImpulseCore.cs","Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs",W06,
  "fa1aa5e531927cbe73cf5fdfe161b4a981316db5"),
 ("AtomicDockLedger.cs","Assets/B0123SecureReattachV2/Runtime/AtomicDockLedger.cs",P510,
  "b6a4d04b0c6407c8c84451b4d599d96fe0e4d1a9"),
 ("SecureRigSpec.cs","Assets/B0123SecureReattachV2/Runtime/SecureRigSpec.cs",P510,
  "b680ed0ec969ac23203fa55eab7b90f8daa86e43"),
]
OWN={PREFIX+n for n in ["Program.cs","Original544SecurityQA.csproj",
                         "source_guard.py","README.md","manifest.json"]}
OWN.add(".github/workflows/b01-61-p17w14-original544-selection-qa.yml")
def git(*args):
 return subprocess.check_output(["git","-C",str(ROOT),*args],text=True).strip()
def gitbytes(*args):
 return subprocess.check_output(["git","-C",str(ROOT),*args])
def assert_ok(ok,msg):
 if not ok:raise AssertionError(msg)
def main():
 assert_ok(git("rev-parse",P544+":"+PROD)==PIN and
           git("rev-parse","HEAD:"+PROD)==PIN and
           git("hash-object",PROD)==PIN,"ORIGINAL_PRODUCER_544_BLOB_DRIFT")
 delta=set(filter(None,git("diff","--name-only",P544,"HEAD").splitlines()))
 assert_ok(delta==OWN,"NOT_QA_ONLY_SCOPE: "+str(sorted(delta^OWN)))
 local=ROOT/PREFIX/"Input"
 local.mkdir(exist_ok=True)
 for name,path,ref,blob in ORIGINALS:
  assert_ok(git("rev-parse",ref+":"+path)==blob,"ORIGINAL_GIT_SOURCE_PIN_"+path)
  output=gitbytes("show",ref+":"+path)
  out=local/name
  out.write_bytes(output)
  assert_ok(git("hash-object",str(out.relative_to(ROOT)))==blob,
            "LOCAL_REAL_ORIGINAL_INPUT_MISMATCH_"+name)
 assert_ok(git("rev-parse",P510+":"+ORIGINALS[1][1])==ORIGINALS[1][3],
           "FROZEN_GAMEPLAY_SHA_DRIFT")
 print("QA_SOURCE_PINS_PASS=4/4; EXACT_SOURCE_544="+P544+"; FROZEN_510="+P510)
 print("QA_ONLY_PATHS=6/6; REAL_ORIGINAL_R2_W06_CSHARP_LINKED=YES")
 print("UNITY_NATIVE=NOT_RUN; CODEX_ACK=NONE; NO_MERGE")
if __name__=="__main__":main()
