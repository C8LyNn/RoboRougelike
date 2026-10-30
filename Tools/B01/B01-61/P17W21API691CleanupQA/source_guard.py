#!/usr/bin/env python3
import subprocess,hashlib,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4]
BASE="809f82575c638e1b4d2806b5f417bddab6c0871a"
OWN="Tools/B01/B01-61/P17W21API691CleanupQA/"
FLOW=".github/workflows/b01-61-w21-api691-owned-stage-source.yml"
PINS={
"Assets/B0123CanonicalSaveApi001/Core/CanonicalWorldGateway.cs":"704e774dedd5be5af26e0791fba87ecdb109ae2a",
"Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs":"b52e511c370f5fca56c74029050844dc8cbb7f9a",
"Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs":"fa1aa5e531927cbe73cf5fdfe161b4a981316db5",
"Assets/B0123UnitySaveBridge001/Core/AtomicWorldSwap.cs":"8fd1dd9a1bc870433809da8fba897ec924fa4113"}
def git(*a):return subprocess.check_output(["git",*a],cwd=ROOT,stderr=subprocess.PIPE)
def main():
 changed=git("diff","--name-only",BASE,"HEAD").decode().splitlines()
 if len(changed)!=6 or any(not p.startswith(OWN) and p!=FLOW for p in changed):
  raise ValueError("FORBIDDEN_PRODUCER_CHANGE "+repr(changed))
 for path,sha in PINS.items():
  raw=git("show","HEAD:"+path)
  blob=hashlib.sha1(b"blob "+str(len(raw)).encode()+b"\x00"+raw).hexdigest()
  if blob!=sha or git("rev-parse","HEAD:"+path).decode().strip()!=sha:raise ValueError("SOURCE_DRIFT "+path)
 print("B0161_W21_691_EXACT_ORIGINAL_GIT_PINS_PASS=4/4 QA_ONLY_6/6")
if __name__=="__main__":
 try:main()
 except (ValueError,OSError,subprocess.CalledProcessError) as e:
  print("FAIL_CLOSED_"+str(e),file=sys.stderr);sys.exit(2)
