#!/usr/bin/env python3
"""Strict original #826 B01-62 W29 only-five-new-path source gate."""
import hashlib,json,subprocess,sys
from pathlib import Path
BASE="99fb08619c78b3834bb4c0450499babf991f218a"
PRODUCER="b4df10a7ad7e6c758f8331013f6b216b3a3689a3"
P="Tools/B01/B01-62/P17W29ColdReadV3/"
FILES=["CanonicalColdReadV3.cs","Program.cs","ColdReadV3Qa.csproj",
       "ColdReadV3Abi.csproj","source_guard.py","README.md","manifest.json"]
WORKFLOW=".github/workflows/b01-62-w29-cold-read-v3.yml"
ORIGINAL={
 "Tools/B01/B01-62/P17W28UnityCompatibleWholeGameV3/WholeGameEconomySlotV3.cs":"PLACEHOLDER1",
 "Tools/B01/B01-62/P17W28UnityCompatibleWholeGameV3/CanonicalWholeGameSessionV3.cs":"PLACEHOLDER2",
}
ECON={
 "ApprovedEconomyPolicyV7.cs":("Assets/B01Packages/ECONAPPROVED007/Runtime/ApprovedEconomyPolicyV7.cs","53ebce6c07388dc9c8a8ed79b6de4d3ccbee4773"),
 "CanonicalEconomySectionV7.cs":("Assets/B01Packages/ECONAPPROVED007/Runtime/CanonicalEconomySectionV7.cs","d8b71de91db36295b6220e9c4baad71eb54455f6")
}
def git(*args):return subprocess.check_output(["git",*args],stderr=subprocess.PIPE).decode().strip()
def main(dest):
 out=Path(dest);p=Path(__file__).parent
 if out.exists():raise AssertionError("REFUSE_EXISTING_RECEIPT")
 subprocess.run(["git","merge-base","--is-ancestor",BASE,"HEAD"],check=True)
 expected={P+x for x in FILES}|{WORKFLOW}
 actual=set(git("diff","--name-only",BASE,"HEAD").splitlines())
 if actual!=expected:raise AssertionError("UNEXPECTED_W29_CHANGE:"+repr(actual^expected))
 for path in ORIGINAL:
  base=git("rev-parse",BASE+":"+path)
  if git("rev-parse","HEAD:"+path)!=base:raise AssertionError("ORIGINAL_826_CHANGED")
 for name,(path,blob) in ECON.items():
  if git("rev-parse",PRODUCER+":"+path)!=blob:raise AssertionError("V7_WRONG_PRODUCER")
  raw=(p/".source"/name).read_bytes()
  if hashlib.sha1(b"blob "+str(len(raw)).encode()+b"\0"+raw).hexdigest()!=blob:
   raise AssertionError("V7_BYTES_CHANGED")
 text=(p/"CanonicalColdReadV3.cs").read_text()
 for unsafe in ["File.Move(","File.Replace(","File.WriteAllBytes(","File.Delete(","WholeGameEconomySlotV3.Create("]:
  if unsafe in text:raise AssertionError("COLD_READER_CONTAINS_WRITER:"+unsafe)
 for token in ["CanonicalWorldSlotLease.TryAcquire","new CanonicalWorldGateway",
   "WholeGameEconomySlotV3.Open","store.Read(","gateway.TryCloseAfterPhysicalRetirement",
   "ReadOnlyGate","NoWorldFactory","NoWritePort"]:
  if token not in text:raise AssertionError("MISSING_READ_AUTHORITY:"+token)
 obj={"base_826":BASE,"producer_v7":PRODUCER,"only_additions":sorted(expected),
      "provenance_blobs":{p:git("rev-parse",BASE+":"+p) for p in ORIGINAL},
      "one_file":"save.envelope","one_owner":"original #729 OS .owner",
      "new_writer":False,"native_unity":"NOT_RUN","composite_g0":"PENDING"}
 raw=(json.dumps(obj,sort_keys=True,indent=2)+"\n").encode()
 out.write_bytes(raw)
 print("W29_ORIGINAL_826_IMMUTABLE_AND_V7_BYTE_PIN=PASS")
 print("W29_ONLY_8_ADDITIVE_FILES=PASS")
 print("W29_READONLY_SOURCE_RECEIPT_SHA256="+hashlib.sha256(raw).hexdigest())
if __name__=="__main__":main(sys.argv[1])
