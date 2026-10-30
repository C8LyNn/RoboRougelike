#!/usr/bin/env python3
import subprocess,hashlib,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4]
SAVE="33094f154c7b91119b36d58aab285e6e83c5de57"
GAME="d94400d95d13db7f27e1533df05c9292d5380431"
SHIM="671859a15e55c32dc2cf779d519b26443e6bd3d8"
BRIDGE="7f26c1852783062d05ff0cb61fe3d07b848bf998"
PINS={
("Assets/B0123SecureReattachV2/Runtime/SecureDebrisPlaygroundV2.cs",GAME):"328977b4cdcdcd7c9f128c01f11ff67f8e46086a",
("Assets/B0123SecureReattachV2/Runtime/SecureDockPolicy.cs",GAME):"9f80f705475bd312f209033a57bd07e03bf95628",
("Assets/B0123SecureReattachV2/Runtime/SecureRigSpec.cs",GAME):"b680ed0ec969ac23203fa55eab7b90f8daa86e43",
("Assets/B0123SecureReattachV2/Runtime/AtomicDockLedger.cs",GAME):"b6a4d04b0c6407c8c84451b4d599d96fe0e4d1a9",
("Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs",GAME):"fa1aa5e531927cbe73cf5fdfe161b4a981316db5",
("Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs",SAVE):"b52e511c370f5fca56c74029050844dc8cbb7f9a",
("Tools/B01/B01-61/P17W13OriginalReloadQA/SyntheticUnityBoundary.cs",SHIM):"40b4113a790bd8fededd138bfd6cde803666cb32",
("Tools/B01/B01-62/P17W15AtomicHostBridge/FrozenSaveHostBridge.cs",BRIDGE):"eeeab2670d99511e30dcac72ba2b1e4161cd2fe5"}
def git(*args):return subprocess.check_output(["git",*args],cwd=ROOT,stderr=subprocess.PIPE)
def main():
 changed=git("diff","--name-only",SAVE,"HEAD").decode().splitlines()
 if len(changed)!=8 or any(not x.startswith("Tools/B01/B01-61/P17W15OriginalHostBridgeGate/") and x!=".github/workflows/b01-61-p17w15-original-host-save-gate.yml" for x in changed):raise ValueError("QA_SCOPE_BREACH "+repr(changed))
 for (path,ref),expected in PINS.items():
  data=git("show",ref+":"+path)
  sha=hashlib.sha1(b"blob "+str(len(data)).encode()+bytes([0])+data).hexdigest()
  if sha!=expected:raise ValueError("EXACT_ORIGINAL_BLOB_DRIFT "+path)
  if ref in (SAVE,GAME) and git("rev-parse","HEAD:"+path).decode().strip()!=expected:raise ValueError("PRODUCER_CHANGED "+path)
 print("W15_ORIGINAL_GIT_SOURCE_PINS_PASS 8/8 QA_ONLY=8/8")
if __name__=="__main__":
 try:main()
 except (ValueError,subprocess.CalledProcessError,OSError) as e:
  print("PIN_FAIL "+str(e),file=sys.stderr);sys.exit(2)
