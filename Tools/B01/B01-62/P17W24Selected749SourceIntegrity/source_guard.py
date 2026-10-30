#!/usr/bin/env python3
"""G0 source admission: immutable owner-selected 749, no ALT source, exact original owner blobs.
Test-only independent negative 749 RAW multi-Save root/fork and selected R5 economy wire gap.
"""
import argparse,hashlib,json,subprocess
from pathlib import Path
SELECTED="8634e71dfcf633323073fe584a6c0884f1ca5d23"
IMMUTABLE={
 "Tools/B01/B01-62/P17W23OneAuthorityV3/CanonicalSingleAuthorityStoreV3.cs":"f8fab0b35147a23ae31bcb4a0cf91590d0896d97",
 "Tools/B01/B01-62/P17W23OneAuthorityV3/CanonicalSlotSessionV3.cs":"9d86581626c8657b6662cb281f7621fa43343fb3",
 "Assets/B0123CanonicalSaveApi001/Core/CanonicalWorldGateway.cs":"21733278b6ab0669fccb6d085e0b6cf242792de2",
 "Assets/B0123CanonicalSaveApi001/Core/CanonicalWorldSlotLease.cs":"a9331dd620336d8c42165d134dc8a5e05ed51873",
 "Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs":"b52e511c370f5fca56c74029050844dc8cbb7f9a",
 "Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs":"fa1aa5e531927cbe73cf5fdfe161b4a981316db5",
 "Assets/B0123SecureReattachV2/Runtime/SecureRigSpec.cs":"b680ed0ec969ac23203fa55eab7b90f8daa86e43",
}
def git(*args):
 return subprocess.check_output(["git",*args],stderr=subprocess.PIPE).decode("utf-8").strip()
def blob(payload):
 data=payload.encode("utf-8")
 return hashlib.sha1(b"blob "+str(len(data)).encode()+b"\0"+data).hexdigest()
def verify(out):
 if out.exists() or not out.parent.is_dir():raise ValueError("REFUSE_EXISTING_EVIDENCE_RECEIVER")
 subprocess.run(["git","merge-base","--is-ancestor",SELECTED,"HEAD"],check=True)
 names=git("diff","--name-only",SELECTED,"HEAD").splitlines()
 base="Tools/B01/B01-62/P17W24Selected749SourceIntegrity/"
 expected={base+"Program.cs",base+"OriginalSelected749W24QA.csproj",
           base+"source_guard.py",base+"README.md",base+"manifest.json",
           ".github/workflows/b01-62-p17w24-selected-749-original-negative.yml"}
 if set(names)!=expected:raise AssertionError("ONLY_NEW_OWNER_749_QA_PATHS:"+str(set(names)^expected))
 values={}
 for path,expected_blob in sorted(IMMUTABLE.items()):
  actual=git("rev-parse","HEAD:"+path)
  frozen=git("rev-parse",SELECTED+":"+path)
  if actual!=expected_blob or actual!=frozen:raise AssertionError("CHANGED_ORIGINAL_"+path)
  raw=git("show","HEAD:"+path)
  if blob(raw)!=expected_blob:raise AssertionError("ORIGINAL_BYTES_MISMATCH_"+path)
  values[path]=expected_blob
 store=git("show","HEAD:Tools/B01/B01-62/P17W23OneAuthorityV3/CanonicalSingleAuthorityStoreV3.cs")
 assert 'public static SlotStatus TryCreateNew(' in store
 assert "private static bool OwnerMatches(" in store
 assert "=> owner!=null&&owner.RunId==run&&owner.SlotId==slot" in store
 assert 'private const string Marker = "slot.canonical-v3"' in store
 assert 'private const string Final = "save.envelope"' in store
 assert "File.Move(stage,final,true)" in store
 source=git("show","HEAD:Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs")
 for token in ("VictoryId","ScrapBalance","LoanerUsed","WalletBalance","LoanerIssued"):
  if token in source:raise AssertionError("NEW_ECON_STATE_PRESENT_REASSESS_TEST")
 for reject in ("Assets/","Packages/","ProjectSettings/"):
  if any(x.startswith(reject) for x in names):raise AssertionError("CHANGED_FROZEN")
 for alt in ("743e36478ae8cf99e8429af1a8209da9d7ca64de",
             "75f1631f9a36ce51b0f8edc0c7857e924cdf78ec"):
  if subprocess.run(["git","merge-base","--is-ancestor",alt,"HEAD"],
                    stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL).returncode==0:
   raise AssertionError("UNAUTHORIZED_ALT_ANCESTRY")
 receipt={"package":"B01-62-W24-749-INDEPENDENT-REAL-SOURCE-NEGATIVE-001",
   "selected_original_749":SELECTED,
   "original_immutable_blobs":values,
   "new_qa_paths":sorted(names),
   "selected_not_753_or_741":True,"changes":"only additive QA",
   "original_R5_selected":"cc7f73541517a18f4479bb155534bd4832d9985e",
   "formal_G0":"HOLD_RAW_PUBLIC_STORE_MULTIPATH",
   "native_G1":"NOT_RUN",
   "full_game_510":"NOT_IMPLEMENTED",
   "integrated":False,"released":False,"codex_ack":False}
 serialized=(json.dumps(receipt,sort_keys=True,indent=2)+"\n").encode()
 out.write_bytes(serialized)
 print("W24_B0162_SELECTED_ORIGINAL_749_SOURCE_PINS=7/7")
 print("W24_B0162_ONLY_QA_ADDS=6/6")
 print("W24_B0162_ORIGINAL_SOURCE_SHA256="+hashlib.sha256(serialized).hexdigest())
if __name__=="__main__":
 p=argparse.ArgumentParser()
 p.add_argument("--receipt",required=True)
 q=p.parse_args()
 verify(Path(q.receipt))
