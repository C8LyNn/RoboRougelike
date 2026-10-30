#!/usr/bin/env python3
"""Exactly original selected producer source imports, immutable Git blob IDs; no alternatives."""
import argparse,hashlib,json,subprocess
from pathlib import Path
PIN={
"CanonicalWorldGatewayV3.cs":("b8ca29c7707cd6b4338d0b9772d74dee676d49d2","Assets/B0123CanonicalSaveApi001/Core/CanonicalWorldGateway.cs","21733278b6ab0669fccb6d085e0b6cf242792de2"),
"CanonicalWorldSlotLease.cs":("b8ca29c7707cd6b4338d0b9772d74dee676d49d2","Assets/B0123CanonicalSaveApi001/Core/CanonicalWorldSlotLease.cs","a9331dd620336d8c42165d134dc8a5e05ed51873"),
"DetachedSnapshotContract.cs":("33094f154c7b91119b36d58aab285e6e83c5de57","Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs","b52e511c370f5fca56c74029050844dc8cbb7f9a"),
"DebrisImpulseCore.cs":("d94400d95d13db7f27e1533df05c9292d5380431","Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs","fa1aa5e531927cbe73cf5fdfe161b4a981316db5"),
"SecureRigSpec.cs":("d94400d95d13db7f27e1533df05c9292d5380431","Assets/B0123SecureReattachV2/Runtime/SecureRigSpec.cs","b680ed0ec969ac23203fa55eab7b90f8daa86e43"),
}
HISTORY={
"original_669":("d6ec0c82e388e65c1e02ddfd920bb50a1f6ef121","Tools/B01/B01-62/P17W20SingleFileSaveSlot/SingleFileSaveSlot.cs","db7745635164d9eb6b10f3246538700158130b7b"),
"original_712":("bd8905ad549a7b26854b671be971f7d781308311","Tools/B01/B01-62/P17W21CanonicalSessionLeaseR2/CanonicalSlotSessionR2.cs","fefc2e263e068b913c6e59ec3c8375bbbe43db4e"),
}
def git(repo,*x):return subprocess.check_output(["git","-C",repo,*x],stderr=subprocess.PIPE)
def oid(data):return hashlib.sha1(b"blob "+str(len(data)).encode()+b"\0"+data).hexdigest()
def main(repo,out,receipt):
 dest=Path(out);rec=Path(receipt)
 if dest.exists() or rec.exists() or not dest.parent.is_dir() or not rec.parent.is_dir():
  raise RuntimeError("REFUSE_EXISTING_RECEIVER")
 ready={}
 for name,(commit,path,blob) in {**PIN,**HISTORY}.items():
  raw=git(repo,"show",commit+":"+path)
  actual=git(repo,"rev-parse",commit+":"+path).decode().strip()
  if actual!=blob or oid(raw)!=blob:raise RuntimeError("IMMUTABLE_BLOB_MISMATCH:"+name)
  if name in PIN:ready[name]=raw
 dest.mkdir()
 for name,raw in ready.items():(dest/name).write_bytes(raw)
 base=Path("Tools/B01/B01-62/P17W23OneAuthorityBridgeR3")
 storage=(base/"SingleAuthoritySaveSlotR3.cs").read_text()
 adapter=(base/"CanonicalSessionR3.cs").read_text()
 if "OpenExclusiveLease" in storage or 'private const string Lease = "slot.lease"' in storage or "ownerLease.Dispose()" in storage:
  raise RuntimeError("COMPETING_B01_669_LEASE_SURVIVES")
 if "new CanonicalWorldGateway(gate,factory,this,physicalOwner)" not in adapter:
  raise RuntimeError("NO_SELECTED_V3_GATEWAY_BINDING")
 if "physicalOwner.TryCancelUnbound()" not in adapter or "gateway.TryCloseAfterPhysicalRetirement(allOwnedRootsGone)" not in adapter:
  raise RuntimeError("UNVERIFIED_LIFETIME")
 data={"package":"B01-62-W23-ONE-AUTHORITY-003",
       "original_exact_pins":{n:{"commit":c,"path":p,"blob":h} for n,(c,p,h) in sorted({**PIN,**HISTORY}.items())},
       "authority":"ONE original #729 canonical .owner held across original #669 derived one-file save; old .lease omitted",
       "storage_derived_from_original_669_blob":HISTORY["original_669"][2],
       "session_derived_from_original_712_blob":HISTORY["original_712"][2],
       "selected_api_v3":"b8ca29c7707cd6b4338d0b9772d74dee676d49d2",
       "selected_host_r5":"cc7f73541517a18f4479bb155534bd4832d9985e",
       "alt_hold":["#719","#695","#693","#682","#604"],
       "frozen_510_private_game_port":"NOT_IMPLEMENTED",
       "native_unity":"NOT_RUN","codex_ack":False,"integrated":False}
 raw=(json.dumps(data,sort_keys=True,indent=2)+"\n").encode()
 rec.write_bytes(raw)
 print("B0162_W23_ORIGINAL_GIT_BLOBS_PINNED=7/7")
 print("B0162_W23_SOURCE_RECEIPT_SHA256="+hashlib.sha256(raw).hexdigest())
if __name__=="__main__":
 a=argparse.ArgumentParser()
 for n in ("repo","out","receipt"):a.add_argument("--"+n,required=True)
 z=a.parse_args();main(z.repo,z.out,z.receipt)
