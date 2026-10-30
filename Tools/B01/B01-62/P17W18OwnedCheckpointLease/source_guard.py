#!/usr/bin/env python3
"""B01-62 W18: source pin for real chosen ORIGINAL #596 and immutable #556/W06."""
import argparse,hashlib,json,subprocess
from pathlib import Path
PIN={
 "AtomicWorldSwap.cs":("efb2b06eafedab5469b57f163152d96aca3c5915",
 "Assets/B0123UnitySaveBridge001/Core/AtomicWorldSwap.cs",
 "1f9e883c38101f214e4335867faa731f7ae67689"),
 "DetachedSnapshotContract.cs":("33094f154c7b91119b36d58aab285e6e83c5de57",
 "Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs",
 "b52e511c370f5fca56c74029050844dc8cbb7f9a"),
 "DebrisImpulseCore.cs":("d94400d95d13db7f27e1533df05c9292d5380431",
 "Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs",
 "fa1aa5e531927cbe73cf5fdfe161b4a981316db5")
}
def gitblob(buf):
 return hashlib.sha1(b"blob "+str(len(buf)).encode()+b"\0"+buf).hexdigest()
def go(repo,out,receipt):
 out,receipt=Path(out),Path(receipt)
 if not out.parent.is_dir() or not receipt.parent.is_dir() or out.exists() or receipt.exists():
  raise RuntimeError("REFUSE_EXISTING_INPUT_OR_RECEIPT")
 results={}
 for name,(sha,path,blob) in PIN.items():
  raw=subprocess.check_output(["git","-C",repo,"show",sha+":"+path])
  original=subprocess.check_output(["git","-C",repo,"rev-parse",sha+":"+path]).decode().strip()
  if gitblob(raw)!=blob or original!=blob:raise RuntimeError("FROZEN_GIT_SOURCE_DRIFT:"+name)
  results[name]=raw
 out.mkdir()
 for n,raw in results.items():(out/n).write_bytes(raw)
 state={"package":"B01-62-W18-OWNED-CHECKPOINT-SLOT-LEASE-R1",
 "source_selected_pr":596,"source_sha":"efb2b06eafedab5469b57f163152d96aca3c5915",
 "source_alt_hold_pr":604,"game_sha":"d94400d95d13db7f27e1533df05c9292d5380431",
 "schema_sha":"33094f154c7b91119b36d58aab285e6e83c5de57",
 "original_git_sources":{name:{"sha":sha,"path":path,"git_blob":blob} for name,(sha,path,blob) in sorted(PIN.items())},
 "host_integration":"NOT_IMPLEMENTED_REQUIRES_OWNER_NEW_VERSION",
 "native_unity":"NOT_RUN","integrated":False,"merged":False,"codex_ack":False}
 buf=(json.dumps(state,sort_keys=True,indent=2)+"\n").encode()
 receipt.write_bytes(buf)
 print("B0162_W18_EXACT_ORIGINAL_GIT_BLOBS=3/3")
 print("B0162_W18_CROSS_OS_SOURCE_RECEIPT_SHA256="+hashlib.sha256(buf).hexdigest())
if __name__=="__main__":
 p=argparse.ArgumentParser()
 for arg in ["repo","out","receipt"]:p.add_argument("--"+arg,required=True)
 a=p.parse_args();go(a.repo,a.out,a.receipt)
