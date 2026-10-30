#!/usr/bin/env python3
import hashlib,json,subprocess,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4]
NEW="33094f154c7b91119b36d58aab285e6e83c5de57"
OLD="6da2c523e03766cd5a6deaed0cb3cccf8765354b"
GAME="d94400d95d13db7f27e1533df05c9292d5380431"
W06="cb2b3a76e84266e2730fffc4ab048c1039f0652f"
SOURCE="Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs"
ROOTQA="Tools/B01/B01-61/P17W14ExactSave556RegressionQA/"
WF=".github/workflows/b01-61-p17w14-exact556-red-green.yml"
PINS={(NEW,SOURCE):"b52e511c370f5fca56c74029050844dc8cbb7f9a",
      (OLD,SOURCE):"6c036985411164372bf3d21d467780ff3777544e",
      (GAME,"Assets/B0123SecureReattachV2/Runtime/AtomicDockLedger.cs"):"b6a4d04b0c6407c8c84451b4d599d96fe0e4d1a9",
      (W06,"Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs"):"fa1aa5e531927cbe73cf5fdfe161b4a981316db5"}
def git(*a):return subprocess.check_output(["git",*a],cwd=ROOT,stderr=subprocess.PIPE)
def main():
  changed=git("diff","--name-only",NEW,"HEAD").decode().splitlines()
  if len(changed)!=6 or any(not x.startswith(ROOTQA) and x!=WF for x in changed):
    raise ValueError("QA_ONLY_SCOPE_DRIFT: "+repr(changed))
  for (ref,path),sha in PINS.items():
    raw=git("show",ref+":"+path)
    h=hashlib.sha1(b"blob "+str(len(raw)).encode()+bytes([0])+raw).hexdigest()
    if h!=sha:raise ValueError("ORIGINAL_BLOB_PIN_DRIFT: "+path)
    if ref==NEW and git("rev-parse","HEAD:"+path).decode().strip()!=sha:
       raise ValueError("SELECTED_556_PRODUCER_CHANGED")
  print("B0161_W14_DIRECT_ORIGINAL_PROVENANCE_GREEN selected_556="+NEW+
        " frozen_549="+OLD+" selected_game_510="+GAME+" source_blobs=4/4 QA_only=6/6")
  print(json.dumps({"selected_save_candidate":556,"exact_sha":NEW,"frozen_v1":OLD,
    "original_unity_game":GAME,"save_schema_live_game":"HOLD","unity_g1":"NOT_RUN",
    "codex_ack":False,"merged":False},sort_keys=True))
if __name__=="__main__":
 try:main()
 except (ValueError,subprocess.CalledProcessError,OSError) as err:
  print("QA_PIN_RED "+str(err),file=sys.stderr);sys.exit(2)
