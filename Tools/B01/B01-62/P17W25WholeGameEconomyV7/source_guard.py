#!/usr/bin/env python3
"""Immutable ORIGINAL B01-22 #780 two-file V7 producer plus B01-62 V2 one-slot source guard."""
import hashlib,json,subprocess,sys
from pathlib import Path
BASE="ba3201644b252241429a6f493238c49d822adfae"
V7="b4df10a7ad7e6c758f8331013f6b216b3a3689a3"
V7FILES={
"ApprovedEconomyPolicyV7.cs":("Assets/B01Packages/ECONAPPROVED007/Runtime/ApprovedEconomyPolicyV7.cs","53ebce6c07388dc9c8a8ed79b6de4d3ccbee4773"),
"CanonicalEconomySectionV7.cs":("Assets/B01Packages/ECONAPPROVED007/Runtime/CanonicalEconomySectionV7.cs","d8b71de91db36295b6220e9c4baad71eb54455f6")
}
FROZEN={
"Assets/B0123CanonicalSaveApi001/Core/CanonicalWorldSlotLease.cs":"a9331dd620336d8c42165d134dc8a5e05ed51873",
"Assets/B0123CanonicalSaveApi001/Core/CanonicalWorldGateway.cs":"21733278b6ab0669fccb6d085e0b6cf242792de2",
"Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs":"b52e511c370f5fca56c74029050844dc8cbb7f9a",
"Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs":"fa1aa5e531927cbe73cf5fdfe161b4a981316db5",
"Assets/B0123SecureReattachV2/Runtime/SecureRigSpec.cs":"b680ed0ec969ac23203fa55eab7b90f8daa86e43"}
def git(*args):return subprocess.check_output(["git",*args],stderr=subprocess.PIPE).decode().strip()
def main(destination):
 base=Path(__file__).parent
 out=Path(destination)
 if out.exists():raise AssertionError("REFUSE_OCCUPIED_EVIDENCE")
 subprocess.run(["git","merge-base","--is-ancestor",BASE,"HEAD"],check=True)
 for filename,(path,sha) in V7FILES.items():
  if git("rev-parse",V7+":"+path)!=sha:raise AssertionError("V7_ORIGINAL_GIT_SHA_"+path)
  raw=(base/".source"/filename).read_bytes()
  test=hashlib.sha1(b"blob "+str(len(raw)).encode()+b"\0"+raw).hexdigest()
  if test!=sha:raise AssertionError("V7_ORIGINAL_BYTES_CHANGED_"+filename)
 for path,sha in FROZEN.items():
  if git("rev-parse",BASE+":"+path)!=sha or git("rev-parse","HEAD:"+path)!=sha:
   raise AssertionError("FROZEN_SELECTED_SOURCE_CHANGED_"+path)
 expected={"Tools/B01/B01-62/P17W25WholeGameEconomyV7/"+x for x in (
  "WholeGameEconomySlotV2.cs","CanonicalWholeGameSessionV2.cs",
  "Program.cs","WholeGameEconomyV7QA.csproj","source_guard.py",
  "README.md","manifest.json")}
 expected.add(".github/workflows/b01-62-w25-whole-game-econ-v7.yml")
 changes=set(git("diff","--name-only",BASE,"HEAD").splitlines())
 if changes!=expected:raise AssertionError("NEW_ONLY_8_FILES_MISMATCH:"+str(changes^expected))
 store=(base/"WholeGameEconomySlotV2.cs").read_text()
 adapter=(base/"CanonicalWholeGameSessionV2.cs").read_text()
 for key in ("CanonicalEconomySectionV7.TryEncode","CanonicalEconomySectionV7.TryDecode",
   "B0162G02","slot.canonical-game-v2","StringComparison.Ordinal",
   "file.Flush(true)","File.Move(staging,authoritative,true)",
   "owner.Live","CanonicalWorldSlotLease"):
  if key not in store:raise AssertionError("ATOMIC_STORAGE_ABI_MISSING_"+key)
 for key in ("new CanonicalWorldGateway(gate,factory,this,physical)",
    "WholeGameEconomySlotV2","CanonicalEconomySectionV7.TryEncode"):
  if key not in adapter:raise AssertionError("CANONICAL_ADAPTER_ABI_MISSING_"+key)
 if "StringComparison.OrdinalIgnoreCase" in store:
  raise AssertionError("LINUX_PATH_ALIAS_ACCEPTED")
 obj={"package":"B01-62-W25-WHOLE-GAME-ECON-V7-002","base":BASE,
  "original_producer_780":V7,"real_original_economy_files":V7FILES,
  "frozen_original_blobs":FROZEN,"new_additive_files":sorted(expected),
  "one_file":"save.envelope","native_G1":"NOT_RUN","integrated":False,"released":False}
 data=(json.dumps(obj,sort_keys=True,indent=2)+"\n").encode("utf-8")
 out.write_bytes(data)
 print("W25_V7_ORIGINAL_780_TWO_SOURCE_BLOBS_AND_729_556_FROZEN=PASS")
 print("W25_V7_NEW_ONLY_GIT_SCOPE=8/8")
 print("W25_V7_SOURCE_RECEIPT_SHA256="+hashlib.sha256(data).hexdigest())
if __name__=="__main__":main(sys.argv[1])
