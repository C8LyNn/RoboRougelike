#!/usr/bin/env python3
"""Pin selected R4 647 and all ORIGINAL immutable C# business logic. Never copy alternatives."""
import argparse,hashlib,json,pathlib,subprocess
PINS={
"AtomicWorldSwap.cs":("09c1809b73324a6d31c13e5d0143de62ac1e3ca6",
 "Assets/B0123UnitySaveBridge001/Core/AtomicWorldSwap.cs","8fd1dd9a1bc870433809da8fba897ec924fa4113"),
"DetachedSnapshotContract.cs":("33094f154c7b91119b36d58aab285e6e83c5de57",
 "Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs","b52e511c370f5fca56c74029050844dc8cbb7f9a"),
"DebrisImpulseCore.cs":("d94400d95d13db7f27e1533df05c9292d5380431",
 "Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs","fa1aa5e531927cbe73cf5fdfe161b4a981316db5"),
"SecureRigSpec.cs":("d94400d95d13db7f27e1533df05c9292d5380431",
 "Assets/B0123SecureReattachV2/Runtime/SecureRigSpec.cs","b680ed0ec969ac23203fa55eab7b90f8daa86e43")
}
def git(repo,*args):
 return subprocess.check_output(["git","-C",str(repo),*args],stderr=subprocess.PIPE)
def oid(raw):
 return hashlib.sha1(b"blob "+str(len(raw)).encode()+b"\x00"+raw).hexdigest()
def main(repo,out,receipt):
 dst=pathlib.Path(out)
 dstparent=dst.parent
 rc=pathlib.Path(receipt)
 if not dstparent.is_dir() or dst.exists() or not rc.parent.is_dir() or rc.exists():
  raise RuntimeError("REFUSE_UNOWNED_OVERWRITE")
 actual={}
 for name,(commit,path,expected) in PINS.items():
  raw=git(repo,"show",commit+":"+path)
  sha=git(repo,"rev-parse",commit+":"+path).decode().strip()
  if oid(raw)!=expected or sha!=expected:
   raise AssertionError("ORIGINAL_GIT_PIN_DRIFT:"+name)
  actual[name]=raw
 dst.mkdir()
 for name,raw in actual.items():(dst/name).write_bytes(raw)
 result={"schema":"B01-62-W19-R4-647-ORIGINAL-CSharp-v1","candidate_pr":647,
 "candidate_sha":"09c1809b73324a6d31c13e5d0143de62ac1e3ca6",
 "alt_hold_pr":644,"alt_hold_sha":"d9b95146e46a5d81d5a291d7887e456164f4d0c2",
 "sources":{name:{"sha":commit,"path":path,"blob":expected} for name,(commit,path,expected) in sorted(PINS.items())},
 "native_g1":"NOT_RUN","g0_release":"HOLD_UNCERTAIN_COMMIT","code_x_ack":False}
 buf=(json.dumps(result,sort_keys=True,indent=2)+"\n").encode()
 rc.write_bytes(buf)
 print("B0162_W19_SELECTED_R3_642_ORIGINAL_GIT_BLOBS=4/4")
 print("B0162_W19_SOURCE_RECEIPT_SHA256="+hashlib.sha256(buf).hexdigest())
if __name__=="__main__":
 p=argparse.ArgumentParser()
 for s in ("repo","out","receipt"):p.add_argument("--"+s,required=True)
 a=p.parse_args();main(a.repo,a.out,a.receipt)
