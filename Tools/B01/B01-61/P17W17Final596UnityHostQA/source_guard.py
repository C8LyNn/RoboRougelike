#!/usr/bin/env python3
"""W17 selected final #596 exact unchanged Git source & B01-61 ownership."""
import hashlib, subprocess, sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4]
BASE="efb2b06eafedab5469b57f163152d96aca3c5915"
GAME="d94400d95d13db7f27e1533df05c9292d5380431"
SAVE="33094f154c7b91119b36d58aab285e6e83c5de57"
OWN="Tools/B01/B01-61/P17W17Final596UnityHostQA/"
FLOW=".github/workflows/b01-61-p17w17-exact596-unity-source.yml"
PINS={
("Assets/B0123UnitySaveBridge001/Unity/IsolatedTwinRigSaveHost.cs",BASE):"4bac6f327c9b93ae98dd2061c0ca8ea2100aab42",
("Assets/B0123UnitySaveBridge001/Core/AtomicWorldSwap.cs",BASE):"1f9e883c38101f214e4335867faa731f7ae67689",
("Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs",SAVE):"b52e511c370f5fca56c74029050844dc8cbb7f9a",
("Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs",GAME):"fa1aa5e531927cbe73cf5fdfe161b4a981316db5",
("Assets/B0123SecureReattachV2/Runtime/SecureRigSpec.cs",GAME):"b680ed0ec969ac23203fa55eab7b90f8daa86e43"}
def git(*a):return subprocess.check_output(["git",*a],cwd=ROOT,stderr=subprocess.PIPE)
def main():
 files=git("diff","--name-only",BASE,"HEAD").decode().splitlines()
 if len(files)!=7 or any(not s.startswith(OWN) and s!=FLOW for s in files):
  raise ValueError("QA_ONLY_SCOPE_FAILURE "+repr(files))
 for (path,source),expected in PINS.items():
  content=git("show",source+":"+path)
  actual=hashlib.sha1(b"blob "+str(len(content)).encode()+b"\x00"+content).hexdigest()
  if actual!=expected or git("rev-parse","HEAD:"+path).decode().strip()!=expected:
   raise ValueError("ORIGINAL_SELECTED596_SOURCE_MUTATED "+path)
 print("B0161_W17_ORIGINAL596_GIT_SOURCE_PIN_PASS=5/5 QA_ONLY_FILES=7/7")
 print("SELECTED_PRODUCER="+BASE+" FROZEN_GAME="+GAME+" FROZEN_SAVE="+SAVE)
if __name__=="__main__":
 try:main()
 except (ValueError,OSError,subprocess.CalledProcessError) as e:
  print("W17_SOURCE_SCOPE_OR_SHA_ERROR "+str(e),file=sys.stderr);sys.exit(2)
