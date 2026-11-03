#!/usr/bin/env python3
"""B01-62 W30 immutable producer #855 direct original, exclusive one writer scope."""
import hashlib,json,subprocess,sys
from pathlib import Path
BASE="e41c17d9e79317dbe8d4a16cee54baf301f8ee37"
V7="b4df10a7ad7e6c758f8331013f6b216b3a3689a3"
ADMISSION="c9c19665a4f9385e6479f7151097deb797026ade"
ROOT="Tools/B01/B01-62/P17W30AdmissionV3/"
MODIFIED={
"Tools/B01/B01-62/P17W28UnityCompatibleWholeGameV3/WholeGameEconomySlotV3.cs",
"Tools/B01/B01-62/P17W28UnityCompatibleWholeGameV3/CanonicalWholeGameSessionV3.cs",
".github/workflows/b01-62-w28-unity-compatible-whole-game-v3.yml",
".github/workflows/b01-62-w29-cold-read-v3.yml"}
OWN={"Program.cs","W30AdmissionQA.csproj","W28OriginalRegression.csproj",
"W29ColdRegression.csproj","W30UnityAbi.csproj","source_guard.py","README.md","manifest.json"}
WORKFLOW=".github/workflows/b01-62-w30-single-writer-admission.yml"
SRC={
"ApprovedEconomyPolicyV7.cs":(V7,"Assets/B01Packages/ECONAPPROVED007/Runtime/ApprovedEconomyPolicyV7.cs","53ebce6c07388dc9c8a8ed79b6de4d3ccbee4773"),
"CanonicalEconomySectionV7.cs":(V7,"Assets/B01Packages/ECONAPPROVED007/Runtime/CanonicalEconomySectionV7.cs","d8b71de91db36295b6220e9c4baad71eb54455f6"),
"EconomyV7AdmissionW29.cs":(ADMISSION,"Assets/B01Packages/ECONADMISSIONW29/Runtime/EconomyV7AdmissionW29.cs","59ad0f42cec5385cc132c1a3917cffd8ebf65d2c")}
def git(*a):return subprocess.check_output(["git",*a],stderr=subprocess.PIPE).decode().strip()
def main(destination):
 out=Path(destination);p=Path(__file__).parent
 if out.exists():raise AssertionError("REFUSE_OCCUPIED_RECEIPT")
 subprocess.run(["git","merge-base","--is-ancestor",BASE,"HEAD"],check=True)
 wanted=MODIFIED|{ROOT+n for n in OWN}|{WORKFLOW}
 seen=set(git("diff","--name-only",BASE,"HEAD").splitlines())
 if seen!=wanted:raise AssertionError("WRONG_13_SCOPE:"+str(wanted^seen))
 for name,(ref,path,expected) in SRC.items():
  if git("rev-parse",ref+":"+path)!=expected:raise AssertionError("SOURCE_BLOB_SHA:"+name)
  data=(p/".source"/name).read_bytes()
  if hashlib.sha1(b"blob "+str(len(data)).encode()+b"\0"+data).hexdigest()!=expected:
   raise AssertionError("ORIGINAL_PRODUCER_MUTATED:"+name)
 for path in ("Assets/B0123CanonicalSaveApi001/Core/CanonicalWorldGateway.cs",
 "Assets/B0123CanonicalSaveApi001/Core/CanonicalWorldSlotLease.cs",
 "Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs",
 "Assets/B0123SecureReattachV2/Runtime/SecureRigSpec.cs"):
  if git("rev-parse",BASE+":"+path)!=git("rev-parse","HEAD:"+path):
   raise AssertionError("FROZEN_SOURCE_MUTATED:"+path)
 path="Tools/B01/B01-62/P17W28UnityCompatibleWholeGameV3/"
 store=Path(path+"WholeGameEconomySlotV3.cs").read_text()
 session=Path(path+"CanonicalWholeGameSessionV3.cs").read_text()
 for word in ("EconomyV7AdmissionW29.TryAdmit",
 "EconomyV7AdmissionW29.TryEncodeForCanonicalOwner",
 "EconomyV7AdmissionW29.TryDecodeForCanonicalOwner",
 "File.Replace(staging,authoritative,null)",
 "file.Flush(true)","B0162G02","slot.canonical-game-v2"):
  if word not in store:raise AssertionError("ONE_WRITER_GUARD_MISSING:"+word)
 if "EconomyV7AdmissionW29.TryAdmit" not in session:
  raise AssertionError("MISSING_PRE_STAGE_ADMISSION")
 for word in ("SHA256.HashData(","Convert.ToHexString(",
   "File.Move(staging,authoritative,true)","File.Delete(authoritative)"):
  if word in store+session:raise AssertionError("UNSAFE_OLD_API:"+word)
 for path in (".github/workflows/b01-62-w28-unity-compatible-whole-game-v3.yml",
 ".github/workflows/b01-62-w29-cold-read-v3.yml"):
  if "  pull_request:" in Path(path).read_text():raise AssertionError("INHERITED_GUARD_TRIGGER")
 receipt={"base":BASE,"v7_producer":V7,"admission_855":ADMISSION,
 "changed_paths":sorted(wanted),"one_writer":True,"magic":"B0162G02",
 "native_unity_g1":"NOT_RUN","fresh_independent_composite_g0":"PENDING"}
 raw=(json.dumps(receipt,sort_keys=True,indent=2)+"\n").encode()
 out.write_bytes(raw)
 print("W30_ORIGINAL_V7_855_BYTE_EXACT_PRODUCERS=PASS")
 print("W30_EXCLUSIVE_ONE_WRITER_13_PATHS=PASS")
 print("W30_RECEIPT_SHA256="+hashlib.sha256(raw).hexdigest())
if __name__=="__main__":main(sys.argv[1])
