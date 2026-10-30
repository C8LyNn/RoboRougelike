#!/usr/bin/env python3
"""Exact unchanged Git source pin + 7 only QA paths, not a Unity build."""
import subprocess,hashlib,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4]
BASE="1aa8a59ce1bb4fcd998f0b2b1ce25dfaea333f0a"
GAME="d94400d95d13db7f27e1533df05c9292d5380431"
SAVE="33094f154c7b91119b36d58aab285e6e83c5de57"
OWN="Tools/B01/B01-61/P17W17Final604UnityHostQA/"
FLOW=".github/workflows/b01-61-p17w17-selected604-unity-host-source.yml"
PINS={
("Assets/B0123UnitySaveBridge001/Unity/IsolatedTwinRigSaveHost.cs",BASE):"5012e98bba786c6b9702992c317586a0015678c1",
("Assets/B0123UnitySaveBridge001/Core/AtomicWorldSwap.cs",BASE):"399d3cb2074a2659af8f4e941edb28a3d2dd2832",
("Assets/B0123UnitySaveBridge001/Core/CheckpointFence.cs",BASE):"53f907125b09531ebdf5ffdfbb9c5c9b48eca217",
("Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs",SAVE):"b52e511c370f5fca56c74029050844dc8cbb7f9a",
("Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs",GAME):"fa1aa5e531927cbe73cf5fdfe161b4a981316db5",
("Assets/B0123SecureReattachV2/Runtime/SecureRigSpec.cs",GAME):"b680ed0ec969ac23203fa55eab7b90f8daa86e43"}
def git(*a):return subprocess.check_output(["git",*a],cwd=ROOT,stderr=subprocess.PIPE)
def main():
 diff=git("diff","--name-only",BASE,"HEAD").decode().splitlines()
 if len(diff)!=7 or any(not path.startswith(OWN) and path!=FLOW for path in diff):
  raise ValueError("W17_QA_SCOPE_VIOLATION "+repr(diff))
 for (path,ref),sha in PINS.items():
  b=git("show",ref+":"+path)
  actual=hashlib.sha1(b"blob "+str(len(b)).encode()+b"\x00"+b).hexdigest()
  if actual!=sha or git("rev-parse","HEAD:"+path).decode().strip()!=sha:
   raise ValueError("W17_ORIGINAL_SOURCE_CHANGED "+path)
 print("W17_SELECTED604_ORIGINAL_BLOB_PINS_PASS=6/6 QA_ONLY=7/7")
 print("ORIGINAL604="+BASE+" FROZEN510="+GAME+" FROZEN556="+SAVE)
if __name__=="__main__":
 try:main()
 except (ValueError,OSError,subprocess.CalledProcessError) as e:
  print("W17_SOURCE_PROVENANCE_ERROR "+str(e),file=sys.stderr);sys.exit(2)
