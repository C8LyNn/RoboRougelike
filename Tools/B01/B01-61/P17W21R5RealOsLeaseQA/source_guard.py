#!/usr/bin/env python3
import subprocess,hashlib,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4]
BASE="cc7f73541517a18f4479bb155534bd4832d9985e"
OWN="Tools/B01/B01-61/P17W21R5RealOsLeaseQA/"
FLOW=".github/workflows/b01-61-w21-r5-690-os-lease.yml"
PINS={
"Assets/B0123UnitySaveBridge001/Core/AtomicWorldSwap.cs":"7da9a9efe489cf8c441d7bb9a47ba3f60ba4c58c",
"Assets/B0123UnitySaveBridge001/Core/OwnerWorldLeaseRetirement.cs":"1ef69de9b4029ce89ba2db5bf3331708da1b91e4",
"Assets/B0123UnitySaveBridge001/Unity/IsolatedTwinRigSaveHost.cs":"6b3936d82b5e1f175856a9e138124b14c9dfd19c",
"Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs":"b52e511c370f5fca56c74029050844dc8cbb7f9a",
"Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs":"fa1aa5e531927cbe73cf5fdfe161b4a981316db5"}
def git(*a):return subprocess.check_output(["git",*a],cwd=ROOT,stderr=subprocess.PIPE)
def main():
 changed=git("diff","--name-only",BASE,"HEAD").decode().splitlines()
 if len(changed)!=6 or any(not p.startswith(OWN) and p!=FLOW for p in changed):
  raise ValueError("QA_ONLY_SCOPE "+repr(changed))
 for p,expected in PINS.items():
  raw=git("show","HEAD:"+p)
  sha=hashlib.sha1(b"blob "+str(len(raw)).encode()+b"\x00"+raw).hexdigest()
  if sha!=expected or git("rev-parse","HEAD:"+p).decode().strip()!=expected:
   raise ValueError("PRODUCER_BLOB_CHANGED "+p)
 print("B0161_W21_R5_690_SOURCE_PINS_PASS=5/5 OWN_QA_PATHS=6/6")
if __name__=="__main__":
 try:main()
 except (ValueError,OSError,subprocess.CalledProcessError) as e:
  print("FAIL_CLOSED_PROVENANCE "+str(e),file=sys.stderr);sys.exit(2)
