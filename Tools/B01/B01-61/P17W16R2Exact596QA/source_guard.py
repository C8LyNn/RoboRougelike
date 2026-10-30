#!/usr/bin/env python3
import hashlib,subprocess,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4]
BASE="efb2b06eafedab5469b57f163152d96aca3c5915"
SAVE="33094f154c7b91119b36d58aab285e6e83c5de57"
GAME="d94400d95d13db7f27e1533df05c9292d5380431"
OWN="Tools/B01/B01-61/P17W16R2Exact596QA/"
FLOW=".github/workflows/b01-61-p17w16-r2-exact596.yml"
PINS={
("Assets/B0123UnitySaveBridge001/Core/AtomicWorldSwap.cs",BASE):"1f9e883c38101f214e4335867faa731f7ae67689",
("Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs",SAVE):"b52e511c370f5fca56c74029050844dc8cbb7f9a",
("Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs",GAME):"fa1aa5e531927cbe73cf5fdfe161b4a981316db5",
("Assets/B0123SecureReattachV2/Runtime/AtomicDockLedger.cs",GAME):"b6a4d04b0c6407c8c84451b4d599d96fe0e4d1a9"}
def git(*a):return subprocess.check_output(["git",*a],cwd=ROOT,stderr=subprocess.PIPE)
def main():
  paths=git("diff","--name-only",BASE,"HEAD").decode().splitlines()
  if len(paths)!=6 or any(not x.startswith(OWN) and x!=FLOW for x in paths):
    raise ValueError("NOT_QA_ONLY "+repr(paths))
  for (path,ref),expected in PINS.items():
    content=git("show",ref+":"+path)
    got=hashlib.sha1(b"blob "+str(len(content)).encode()+b"\0"+content).hexdigest()
    if got!=expected:raise ValueError("ORIGINAL_SHA_MISMATCH "+path)
    if git("rev-parse","HEAD:"+path).decode().strip()!=expected:
      raise ValueError("QA_MUST_NOT_TOUCH_ORIGINAL_SOURCE "+path)
  print("W16_EXACT_596_SOURCE_PINS_PASS blobs=4/4 QA_only_files=6/6")
  print("EXACT_NEW_PRODUCER="+BASE+" FROZEN_SAVE="+SAVE+" FROZEN_GAME="+GAME)
if __name__=="__main__":
 try:main()
 except (OSError,ValueError,subprocess.CalledProcessError) as e:
  print("W16_PROVENANCE_REJECT "+str(e),file=sys.stderr);sys.exit(2)
