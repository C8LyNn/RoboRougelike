#!/usr/bin/env python3
"""Actual Git SHA/file-scope, fail-closed; independent QA cannot mutate producer."""
import subprocess,sys,hashlib
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4]
BASE="ad7904eca571c742257551fa8103f266fcb9b752"
GAME="d94400d95d13db7f27e1533df05c9292d5380431"
SAVE="33094f154c7b91119b36d58aab285e6e83c5de57"
OWN="Tools/B01/B01-61/P17W16SelectedHostQA/"
WORKFLOW=".github/workflows/b01-61-p17w16-selected587-atomicity.yml"
PINS={
("Assets/B0123UnitySaveBridge001/Core/AtomicWorldSwap.cs",BASE):"16a7d7043a2ecb59a41bbfb063170235872fd7fc",
("Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs",SAVE):"b52e511c370f5fca56c74029050844dc8cbb7f9a",
("Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs",GAME):"fa1aa5e531927cbe73cf5fdfe161b4a981316db5",
("Assets/B0123SecureReattachV2/Runtime/AtomicDockLedger.cs",GAME):"b6a4d04b0c6407c8c84451b4d599d96fe0e4d1a9",
("Assets/B0123SecureReattachV2/Runtime/SecureRigSpec.cs",GAME):"b680ed0ec969ac23203fa55eab7b90f8daa86e43"}
def git(*a):return subprocess.check_output(["git",*a],cwd=ROOT,stderr=subprocess.PIPE)
def main():
  paths=git("diff","--name-only",BASE,"HEAD").decode().splitlines()
  if len(paths)!=6 or any(not x.startswith(OWN) and x!=WORKFLOW for x in paths):
    raise ValueError("QA_ONLY_SCOPE_MISMATCH "+repr(paths))
  for (path,ref),pin in PINS.items():
    content=git("show",ref+":"+path)
    actual=hashlib.sha1(b"blob "+str(len(content)).encode()+b"\x00"+content).hexdigest()
    if actual!=pin:raise ValueError("SOURCE_REF_BLOB_CHANGED "+path)
    if git("rev-parse","HEAD:"+path).decode().strip()!=pin:
      raise ValueError("W16_ORIGINAL_PRODUCER_MODIFIED "+path)
  print("B0161_W16_ORIGINAL_PROVENANCE_PASS producer_blob_sha=5/5 QA_only=6/6")
  print("SELECTED_587_SOURCE_SHA="+BASE+" GAME_510_SHA="+GAME+" SAVE_556_SHA="+SAVE)
if __name__=="__main__":
  try:main()
  except (ValueError,OSError,subprocess.CalledProcessError) as e:
    print("QA_PROVENANCE_ERROR "+str(e),file=sys.stderr);sys.exit(2)
