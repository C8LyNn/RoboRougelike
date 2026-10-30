#!/usr/bin/env python3
"""Pin real original selected #787 physical store ancestry and original B01-22 #764 economy reducer."""
import hashlib,json,subprocess,sys
from pathlib import Path
BASE="f5db40bf293fe9582ec7c6fcb3515aeff6fcf937"
ECON="cc5d65f66ad7a764af6515c566e489b9fc3be82e"
ECON_PATH="Assets/B01Packages/ECONAPPROVED006/Runtime/ApprovedEconomyPolicyV6.cs"
PIN_ECON="c72db78548f3e21765c54eb4609b748153118392"
PINS={
 "Assets/B0123CanonicalSaveApi001/Core/CanonicalWorldSlotLease.cs":"a9331dd620336d8c42165d134dc8a5e05ed51873",
 "Assets/B0123CanonicalSaveApi001/Core/CanonicalWorldGateway.cs":"21733278b6ab0669fccb6d085e0b6cf242792de2",
 "Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs":"b52e511c370f5fca56c74029050844dc8cbb7f9a",
 "Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs":"fa1aa5e531927cbe73cf5fdfe161b4a981316db5",
 "Assets/B0123SecureReattachV2/Runtime/SecureRigSpec.cs":"b680ed0ec969ac23203fa55eab7b90f8daa86e43",
}
def git(*args):return subprocess.check_output(["git",*args],stderr=subprocess.PIPE).decode().strip()
def main(out):
 root=Path(__file__).parent
 receiver=Path(out)
 if receiver.exists():raise AssertionError("FOREIGN_RECEIPT")
 subprocess.run(["git","merge-base","--is-ancestor",BASE,"HEAD"],check=True)
 for path,blob in PINS.items():
  if git("rev-parse",BASE+":"+path)!=blob or git("rev-parse","HEAD:"+path)!=blob:
   raise AssertionError("IMMUTABLE_ORIGINAL_SOURCE_CHANGED_"+path)
 if git("rev-parse",ECON+":"+ECON_PATH)!=PIN_ECON:
  raise AssertionError("B01_22_ORIGINAL_REDUCER_SHA_UNEXPECTED")
 source=(root/".source"/"ApprovedEconomyPolicyV6.cs")
 if not source.is_file():raise AssertionError("MISSING_REAL_ORIGINAL_764_REDUCER")
 raw=source.read_bytes()
 actual=hashlib.sha1(b"blob "+str(len(raw)).encode()+b"\0"+raw).hexdigest()
 if actual!=PIN_ECON:raise AssertionError("NOT_ORIGINAL_UNMODIFIED_764_BINARY")
 expected={"Tools/B01/B01-62/P17W25WholeGameEconomy001/"+n for n in (
 "WholeGameEconomySlotV1.cs","Program.cs","WholeGameEconomyQA.csproj",
 "source_guard.py","manifest.json","README.md")}
 expected.add(".github/workflows/b01-62-w25-unified-economy-save.yml")
 if set(git("diff","--name-only",BASE,"HEAD").splitlines())!=expected:
  raise AssertionError("NEW_ONLY_SOURCE_OWNERSHIP_MISMATCH")
 store=(root/"WholeGameEconomySlotV1.cs").read_text()
 if "StringComparison.OrdinalIgnoreCase" in store:
  raise AssertionError("CASE_ALIAS_NOT_FIXED")
 for key in ("File.Move(staging,authoritative,true)","FileOptions.WriteThrough",
  "file.Flush(true)","ApprovedEconomyPolicyV6.Valid","ColdRestore.TryStage",
  "CanonicalWorldSlotLease","slot.canonical-game-v1","save.envelope",
  "one canonical" ):
  if key=="one canonical":continue
  if key not in store:raise AssertionError("MISSING_SINGLE_FILE_SECURITY_"+key)
 data={"package":"B01-62-W25-WHOLE-GAME-ECONOMY-001",
  "parent_fixed_773_R3":BASE,"economic_producer_764":ECON,
  "economic_blob":PIN_ECON,"original_frozen_blobs":PINS,
  "new_only_paths":sorted(expected),
  "future_release":"OWNER_APPROVAL_G0_NATIVE_G1_PENDING",
  "native_G1":"NOT_RUN","frozen_modified":False,"integrated":False,"code_ACK":False}
 receiver.write_text(json.dumps(data,sort_keys=True,indent=2)+"\n")
 print("W25_WHOLE_GAME_ORIGINAL_PINNED_764_AND_556_729=PASS")
 print("W25_WHOLE_GAME_NEW_ONLY_GITHUB_SCOPE=7/7")
 print("W25_WHOLE_GAME_SOURCE_RECEIPT_SHA256="+hashlib.sha256(receiver.read_bytes()).hexdigest())
if __name__=="__main__":main(sys.argv[1])
