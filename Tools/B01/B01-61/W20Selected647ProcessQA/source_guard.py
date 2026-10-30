#!/usr/bin/env python3
import subprocess,hashlib,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4]
BASE="09c1809b73324a6d31c13e5d0143de62ac1e3ca6"
OWN="Tools/B01/B01-61/W20Selected647ProcessQA/"
FLOW=".github/workflows/b01-61-w20-r4-647-process-transaction.yml"
PINS={
"Assets/B0123UnitySaveBridge001/Core/AtomicWorldSwap.cs":"8fd1dd9a1bc870433809da8fba897ec924fa4113",
"Assets/B0123UnitySaveBridge001/Unity/IsolatedTwinRigSaveHost.cs":"82147492a9205f8ef5986ceb61aeb10d6ea24ff2",
"Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs":"b52e511c370f5fca56c74029050844dc8cbb7f9a",
"Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs":"fa1aa5e531927cbe73cf5fdfe161b4a981316db5",
"Assets/B0123SecureReattachV2/Runtime/AtomicDockLedger.cs":"b6a4d04b0c6407c8c84451b4d599d96fe0e4d1a9"}
def git(*xs):return subprocess.check_output(["git",*xs],cwd=ROOT,stderr=subprocess.PIPE)
def main():
 changed=git("diff","--name-only",BASE,"HEAD").decode().splitlines()
 if len(changed)!=6 or any(not p.startswith(OWN) and p!=FLOW for p in changed):
  raise ValueError("B01_61_OWNERSHIP_SCOPE "+repr(changed))
 for path,sha in PINS.items():
  raw=git("show","HEAD:"+path)
  found=hashlib.sha1(b"blob "+str(len(raw)).encode()+b"\x00"+raw).hexdigest()
  if found!=sha or git("rev-parse","HEAD:"+path).decode().strip()!=sha:
   raise ValueError("ORIGINAL_PRODUCER_SOURCE_SHA_CHANGED "+path)
 print("B0161_W20_SELECTED647_ORIGINAL_BLOB_PINS=5/5 QA_ONLY_PATHS=6/6")
 print("R4="+BASE+" R3=22cec9888047994ae523cbe7bf1b0a483d64cec4 FROZEN_510_556_596=UNCHANGED")
if __name__=="__main__":
 try:main()
 except (ValueError,OSError,subprocess.CalledProcessError) as e:
  print("W20_PROVENANCE_FAIL_CLOSED "+str(e),file=sys.stderr);sys.exit(2)
