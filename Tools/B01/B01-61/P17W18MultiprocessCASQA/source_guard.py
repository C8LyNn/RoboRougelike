#!/usr/bin/env python3
"""W18 no modified original producer and strict scope, exact frozen B01 selected only."""
import subprocess,hashlib,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4]
BASE="efb2b06eafedab5469b57f163152d96aca3c5915"
GAME="d94400d95d13db7f27e1533df05c9292d5380431"
SAVE="33094f154c7b91119b36d58aab285e6e83c5de57"
SCOPE="Tools/B01/B01-61/P17W18MultiprocessCASQA/"
FLOW=".github/workflows/b01-61-p17w18-exact596-process-checkpoint-race.yml"
PINS={
("Assets/B0123UnitySaveBridge001/Core/AtomicWorldSwap.cs",BASE):"1f9e883c38101f214e4335867faa731f7ae67689",
("Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs",SAVE):"b52e511c370f5fca56c74029050844dc8cbb7f9a",
("Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs",GAME):"fa1aa5e531927cbe73cf5fdfe161b4a981316db5",
("Assets/B0123SecureReattachV2/Runtime/AtomicDockLedger.cs",GAME):"b6a4d04b0c6407c8c84451b4d599d96fe0e4d1a9"}
def git(*xs):return subprocess.check_output(["git",*xs],cwd=ROOT,stderr=subprocess.PIPE)
def main():
 paths=git("diff","--name-only",BASE,"HEAD").decode().splitlines()
 if len(paths)!=6 or any(not p.startswith(SCOPE) and p!=FLOW for p in paths):
  raise ValueError("W18_OWN_QA_SCOPE_VIOLATION "+repr(paths))
 for (path,ref),expected in PINS.items():
  content=git("show",ref+":"+path)
  digest=hashlib.sha1(b"blob "+str(len(content)).encode()+b"\x00"+content).hexdigest()
  if digest!=expected or git("rev-parse","HEAD:"+path).decode().strip()!=expected:
   raise ValueError("W18_ORIGINAL_BLOB_MISMATCH "+path)
 print("B0161_W18_EXACT_SELECTED596_ORIGINAL_GIT_PINS_PASS 4/4 QA_ONLY_FILES 6/6")
 print("PRODUCER="+BASE+" GAME510="+GAME+" SAVE556="+SAVE)
if __name__=="__main__":
 try:main()
 except (ValueError,OSError,subprocess.CalledProcessError) as e:
  print("W18_FAIL_CLOSED_SOURCE_PROVENANCE "+str(e),file=sys.stderr);sys.exit(2)
