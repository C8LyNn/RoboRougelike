#!/usr/bin/env python3
"""Pin ONE original producer #549 + original W06 bytes; fail closed on drift/mixing."""
import argparse,hashlib,json,subprocess
from pathlib import Path

SELECTED_GAME="d94400d95d13db7f27e1533df05c9292d5380431"
SELECTED_SAVE="6da2c523e03766cd5a6deaed0cb3cccf8765354b"
PINS={
 "save":(SELECTED_SAVE,"Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs","6c036985411164372bf3d21d467780ff3777544e"),
 "w06":(SELECTED_GAME,"Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs","fa1aa5e531927cbe73cf5fdfe161b4a981316db5")
}
class FailClosed(Exception):pass
def blob(raw):return hashlib.sha1(b"blob "+str(len(raw)).encode()+b"\0"+raw).hexdigest()
def verify(repo):
    ret={}
    for label,(ref,path,expected) in PINS.items():
        raw=subprocess.check_output(["git","-C",str(repo),"show",ref+":"+path])
        actual=subprocess.check_output(["git","-C",str(repo),"rev-parse",ref+":"+path]).decode().strip()
        if actual!=expected or blob(raw)!=expected:raise FailClosed("ORIGINAL_SOURCE_BLOB_DRIFT_"+label)
        ret[label]=raw
    source=ret["save"].decode("utf-8-sig")
    for t in ("public readonly PersistedPart[] Parts;","public DebrisImpulseLedger GetLedger(int slot)=>rebuilt[slot];",
              "public RestoreVerdict TrySwap(byte[] bytes,long expectedRunId,long minGeneration,"):
        if t not in source:raise FailClosed("SOURCE_API_CHANGED_REVERIFY_REQUIRED")
    return ret
def run(repo,dest):
    p=Path(dest)
    if p.exists() or not p.parent.is_dir():raise FailClosed("SCRATCH_MUST_BE_NEW")
    data=verify(repo)
    p.mkdir()
    (p/"DetachedSnapshotContract.cs").write_bytes(data["save"])
    (p/"DebrisImpulseCore.cs").write_bytes(data["w06"])
    receipt={"schema":"b01-62-w14-exact-original-549-v1","selected_save_pr":549,"selected_save_sha":SELECTED_SAVE,
         "selected_game_sha":SELECTED_GAME,"source_pins":{n:{"commit":ref,"path":path,"git_blob":hash_}
         for n,(ref,path,hash_) in sorted(PINS.items())},
         "producer_original_direct_api":"RED_UNCHANGED","receiver_facade":"SEPARATE_QA_ONLY",
         "native_unity":"NOT_RUN","save_host_integrated":False,"ack":False,"merged":False}
    raw=(json.dumps(receipt,sort_keys=True,indent=2)+"\n").encode()
    (p.parent/"source_receipt.json").write_bytes(raw)
    print("ORIGINAL_SELECTED_549_AND_W06_GIT_BLOBS=2/2")
    print("SELECTED_SAVE_SCHEMA=549 ONLY; 544=HOLD")
    print("RECEIPT_SHA256="+hashlib.sha256(raw).hexdigest())
if __name__=="__main__":
    a=argparse.ArgumentParser();a.add_argument("--repo",required=True);a.add_argument("--input-dir",required=True)
    o=a.parse_args();run(o.repo,o.input_dir)
