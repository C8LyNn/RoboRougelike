#!/usr/bin/env python3
import subprocess,hashlib,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4]
BASE="22cec9888047994ae523cbe7bf1b0a483d64cec4"
SAVE="33094f154c7b91119b36d58aab285e6e83c5de57"
GAME="d94400d95d13db7f27e1533df05c9292d5380431"
OWN="Tools/B01/B01-61/P17W19Selected642OriginalQA/"
FLOW=".github/workflows/b01-61-w19-r3-642-uncertain-commit-process.yml"
PINS={
("Assets/B0123UnitySaveBridge001/Core/AtomicWorldSwap.cs",BASE):"a0a1923216d88ff8f50c560196f82cdec694f15c",
("Assets/B0123UnitySaveBridge001/Unity/IsolatedTwinRigSaveHost.cs",BASE):"7b8354bc4cd846227b4bdf6d25d81bed34a325bc",
("Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs",SAVE):"b52e511c370f5fca56c74029050844dc8cbb7f9a",
("Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs",GAME):"fa1aa5e531927cbe73cf5fdfe161b4a981316db5",
("Assets/B0123SecureReattachV2/Runtime/AtomicDockLedger.cs",GAME):"b6a4d04b0c6407c8c84451b4d599d96fe0e4d1a9"}
def git(*a):return subprocess.check_output(["git",*a],cwd=ROOT,stderr=subprocess.PIPE)
def main():
 paths=git("diff","--name-only",BASE,"HEAD").decode().splitlines()
 if len(paths)!=6 or any(not x.startswith(OWN) and x!=FLOW for x in paths):
  raise ValueError("QA_SCOPE_W19_MISMATCH "+repr(paths))
 for (p,ref),sha in PINS.items():
  raw=git("show",ref+":"+p)
  blob=hashlib.sha1(b"blob "+str(len(raw)).encode()+b"\x00"+raw).hexdigest()
  if blob!=sha or git("rev-parse","HEAD:"+p).decode().strip()!=sha:
   raise ValueError("ORIGINAL_SOURCE_SHA_CHANGED "+p)
 print("B0161_W19_ORIGINAL_R3_642_GIT_PINS_PASS=5/5 QA_ONLY=6/6")
 print("SELECTED_642="+BASE+" FROZEN_510="+GAME+" FROZEN_556="+SAVE+" ALT_644=HOLD_FOR_THIS_QA")
if __name__=="__main__":
 try:main()
 except (ValueError,OSError,subprocess.CalledProcessError) as e:
  print("W19_FAIL_CLOSED_PROVENANCE "+str(e),file=sys.stderr);sys.exit(2)
