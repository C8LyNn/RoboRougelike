#!/usr/bin/env python3
"""Read only exact ORIGINAL #556 codec/W06 and frozen #510 identity from Git objects."""
import argparse,hashlib,json,subprocess
from pathlib import Path
PINS={
 "DetachedSnapshotContract.cs":("33094f154c7b91119b36d58aab285e6e83c5de57",
 "Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs",
 "b52e511c370f5fca56c74029050844dc8cbb7f9a"),
 "DebrisImpulseCore.cs":("d94400d95d13db7f27e1533df05c9292d5380431",
 "Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs",
 "fa1aa5e531927cbe73cf5fdfe161b4a981316db5"),
 "SecureRigSpec.cs":("d94400d95d13db7f27e1533df05c9292d5380431",
 "Assets/B0123SecureReattachV2/Runtime/SecureRigSpec.cs",
 "b680ed0ec969ac23203fa55eab7b90f8daa86e43")
}
def git(repo,*a):
 return subprocess.check_output(["git","-C",str(repo),*a],stderr=subprocess.PIPE)
def oid(raw):
 return hashlib.sha1(b"blob "+str(len(raw)).encode()+b"\x00"+raw).hexdigest()
def main(repo,out,receipt):
 dest,rc=Path(out),Path(receipt)
 if dest.exists() or rc.exists() or not dest.parent.is_dir() or not rc.parent.is_dir():
  raise ValueError("refuse replacing existing source or receipt")
 data={}
 for name,(commit,path,sha) in PINS.items():
  raw=git(repo,"show",commit+":"+path)
  ref=git(repo,"rev-parse",commit+":"+path).decode().strip()
  if oid(raw)!=sha or ref!=sha:raise ValueError("frozen source mismatch "+name)
  data[name]=raw
 dest.mkdir()
 for name,raw in data.items():(dest/name).write_bytes(raw)
 rec={
  "package":"B01-62-SLOTSAVE-001-v1",
  "source":"frozen original #556 codec, W06 and frozen #510 ID spec",
  "selected_R4":647,
  "selected_R4_sha":"09c1809b73324a6d31c13e5d0143de62ac1e3ca6",
  "alt_hold_R4":652,
  "frozen_510_sha":"d94400d95d13db7f27e1533df05c9292d5380431",
  "frozen_556_sha":"33094f154c7b91119b36d58aab285e6e83c5de57",
  "pinned_blobs":{name:{"commit":c,"path":p,"blob":v} for name,(c,p,v) in sorted(PINS.items())},
  "protocol":"one authoritative atomic envelope file, no separate checkpoint sidecar",
  "native_g1":"NOT_RUN","canonical_510_bound":False,
  "fsync_parent_directory_powerloss":"NOT_PROVEN","codex_ack":False,
  "merged":False
 }
 payload=(json.dumps(rec,sort_keys=True,indent=2)+"\n").encode()
 rc.write_bytes(payload)
 print("B0162_W20_ORIGINAL_FROZEN_GIT_BLOBS=3/3")
 print("B0162_W20_ORIGINAL_SOURCE_RECEIPT_SHA256="+hashlib.sha256(payload).hexdigest())
if __name__=="__main__":
 p=argparse.ArgumentParser()
 for a in ("repo","out","receipt"):p.add_argument("--"+a,required=True)
 v=p.parse_args()
 main(v.repo,v.out,v.receipt)
