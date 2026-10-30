#!/usr/bin/env python3
"""Exact frozen #556 + #510 + W06 source provenance; generates ONLY isolated CI Input."""
import argparse,hashlib,json,subprocess
from pathlib import Path
SAVE="33094f154c7b91119b36d58aab285e6e83c5de57"
GAME="d94400d95d13db7f27e1533df05c9292d5380431"
PINS={
"DetachedSnapshotContract.cs":(SAVE,"Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs","b52e511c370f5fca56c74029050844dc8cbb7f9a"),
"DebrisImpulseCore.cs":(GAME,"Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs","fa1aa5e531927cbe73cf5fdfe161b4a981316db5"),
"AtomicDockLedger.cs":(GAME,"Assets/B0123SecureReattachV2/Runtime/AtomicDockLedger.cs","b6a4d04b0c6407c8c84451b4d599d96fe0e4d1a9")
}
def gitblob(raw):return hashlib.sha1(b"blob "+str(len(raw)).encode()+b"\0"+raw).hexdigest()
def do(repo,target,receipt):
    target=Path(target);receipt=Path(receipt)
    if target.exists() or receipt.exists() or not target.parent.is_dir() or not receipt.parent.is_dir():
        raise RuntimeError("REFUSE_EXISTING_OR_MISSING_DESTINATION")
    got={}
    for filename,(commit,path,expected) in PINS.items():
        ref=f"{commit}:{path}"
        raw=subprocess.check_output(["git","-C",str(repo),"show",ref])
        obj=subprocess.check_output(["git","-C",str(repo),"rev-parse",ref]).decode().strip()
        if gitblob(raw)!=expected or obj!=expected:raise RuntimeError("SOURCE_DRIFT:"+filename)
        got[filename]=raw
    target.mkdir()
    for filename,raw in got.items():(target/filename).write_bytes(raw)
    result={"package":"B01-62-P17W15-ATOMIC-HOST-PORT/1.0.0",
       "frozen_save_pr":556,"save_source_sha":SAVE,"frozen_game_pr":510,
       "game_source_sha":GAME,"sources":{name:{"commit":commit,"path":path,"blob":hash_} for name,(commit,path,hash_) in sorted(PINS.items())},
       "live_private_510_hydration":"BLOCKED_UNEXPOSED_API",
       "native_unity":"NOT_RUN","codex_ack":False,"merged":False}
    raw=(json.dumps(result,sort_keys=True,indent=2)+"\n").encode()
    receipt.write_bytes(raw)
    print("B01_62_W15_ORIGINAL_SOURCE_BLOBS=3/3")
    print("SOURCE_RECEIPT_SHA256="+hashlib.sha256(raw).hexdigest())
if __name__=="__main__":
    a=argparse.ArgumentParser();a.add_argument("--repo",required=True);a.add_argument("--out",required=True);a.add_argument("--receipt",required=True);o=a.parse_args();do(o.repo,o.out,o.receipt)
