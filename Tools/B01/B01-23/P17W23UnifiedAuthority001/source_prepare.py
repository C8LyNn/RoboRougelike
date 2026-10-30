#!/usr/bin/env python3
"""Fetch ORIGINAL frozen B01-62 #669 C# by exact Git blob, not a code copy."""
from pathlib import Path
import subprocess,hashlib
ROOT=Path(__file__).resolve().parents[4]
OWNER=Path(__file__).parent
B0162_REF="d6ec0c82e388e65c1e02ddfd920bb50a1f6ef121"
B0162_PATH="Tools/B01/B01-62/P17W20SingleFileSaveSlot/SingleFileSaveSlot.cs"
B0162_BLOB="db7745635164d9eb6b10f3246538700158130b7b"
R2_REF="bd8905ad549a7b26854b671be971f7d781308311"
R2_PATH="Tools/B01/B01-62/P17W21CanonicalSessionLeaseR2/CanonicalSlotSessionR2.cs"
R2_BLOB="fefc2e263e068b913c6e59ec3c8375bbbe43db4e"
V3_REF="b8ca29c7707cd6b4338d0b9772d74dee676d49d2"
ORIGINAL={
"Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs":"b52e511c370f5fca56c74029050844dc8cbb7f9a",
"Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs":"fa1aa5e531927cbe73cf5fdfe161b4a981316db5"}
def git(*cmd):return subprocess.check_output(["git",*cmd],cwd=ROOT).decode().strip()
def check():
    subprocess.run(["git","merge-base","--is-ancestor",V3_REF,"HEAD"],cwd=ROOT,check=True)
    if git("rev-parse",B0162_REF+":"+B0162_PATH)!=B0162_BLOB:raise ValueError("original #669 SHA changed")
    if git("rev-parse",R2_REF+":"+R2_PATH)!=R2_BLOB:raise ValueError("frozen #712 R2 SHA changed")
    for path,blob in ORIGINAL.items():
        if git("rev-parse","HEAD:"+path)!=blob:raise ValueError("frozen source modified: "+path)
def prepare():
    check()
    original=subprocess.check_output(["git","show",B0162_REF+":"+B0162_PATH],cwd=ROOT)
    out=OWNER/".generated"/"SingleFileSaveSlot.cs"
    out.parent.mkdir(exist_ok=True)
    out.write_bytes(original)
    print("B0123_W23_COMPILE_ORIGINAL_669_BLOB",B0162_BLOB,"SHA256",hashlib.sha256(original).hexdigest())
    return out
if __name__=="__main__":prepare()
