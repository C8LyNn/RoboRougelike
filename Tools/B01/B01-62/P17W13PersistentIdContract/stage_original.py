#!/usr/bin/env python3
"""Stage exact Git-original W06/R2 pure source (never edit source tree)."""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess
SELECTED="d94400d95d13db7f27e1533df05c9292d5380431"
FILES={
 "DebrisImpulseCore.cs":("Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs","fa1aa5e531927cbe73cf5fdfe161b4a981316db5"),
 "AtomicDockLedger.cs":("Assets/B0123SecureReattachV2/Runtime/AtomicDockLedger.cs","b6a4d04b0c6407c8c84451b4d599d96fe0e4d1a9"),
 "SecureRigSpec.cs":("Assets/B0123SecureReattachV2/Runtime/SecureRigSpec.cs","b680ed0ec969ac23203fa55eab7b90f8daa86e43")
}
def gitblob(b):
 return hashlib.sha1(b"blob "+str(len(b)).encode()+b"\0"+b).hexdigest()
def stage(repo,dest):
 dest=Path(dest)
 if dest.exists() or not dest.parent.is_dir():raise ValueError("STAGE_NEW_DIRECTORY_ONLY")
 values={}
 for name,(path,pin) in FILES.items():
  original=subprocess.check_output(["git","-C",str(repo),"show",SELECTED+":"+path])
  if gitblob(original)!=pin:raise ValueError("IMMUTABLE_510_SOURCE_DRIFT:"+path)
  values[name]=original
 dest.mkdir()
 for name,b in values.items():(dest/name).write_bytes(b)
 receipt={"selected_source_sha":SELECTED,"actual_original_source_files":
          {name:{"git_blob":FILES[name][1],"sha256":hashlib.sha256(b).hexdigest()} for name,b in sorted(values.items())},
          "native_unity":"NOT_RUN","game_save_adapter":"NOT_INTEGRATED"}
 r=json.dumps(receipt,indent=2,sort_keys=True)+"\n"
 (dest/"source_receipt.json").write_text(r,encoding="utf-8")
 print("ORIGINAL_W06_R2_SOURCE_3_OF_3_PINNED")
 print("SOURCE_RECEIPT_SHA256="+hashlib.sha256(r.encode()).hexdigest())
if __name__=="__main__":
 p=argparse.ArgumentParser()
 p.add_argument("--repo",default=".")
 p.add_argument("--dest",required=True)
 a=p.parse_args()
 stage(a.repo,a.dest)
