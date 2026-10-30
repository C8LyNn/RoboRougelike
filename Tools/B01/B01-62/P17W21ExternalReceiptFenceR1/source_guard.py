#!/usr/bin/env python3
import argparse,subprocess,pathlib,hashlib,json
PINS={
"SingleFileSaveSlot.cs":("d6ec0c82e388e65c1e02ddfd920bb50a1f6ef121","Tools/B01/B01-62/P17W20SingleFileSaveSlot/SingleFileSaveSlot.cs","db7745635164d9eb6b10f3246538700158130b7b"),
"DetachedSnapshotContract.cs":("33094f154c7b91119b36d58aab285e6e83c5de57","Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs","b52e511c370f5fca56c74029050844dc8cbb7f9a"),
"DebrisImpulseCore.cs":("d94400d95d13db7f27e1533df05c9292d5380431","Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs","fa1aa5e531927cbe73cf5fdfe161b4a981316db5"),
"SecureRigSpec.cs":("d94400d95d13db7f27e1533df05c9292d5380431","Assets/B0123SecureReattachV2/Runtime/SecureRigSpec.cs","b680ed0ec969ac23203fa55eab7b90f8daa86e43")
}
def cmd(repo,*args):return subprocess.check_output(["git","-C",repo,*args],stderr=subprocess.PIPE)
def oid(v):return hashlib.sha1(b"blob "+str(len(v)).encode()+b"\x00"+v).hexdigest()
def main(repo,dest,receipt):
 out=pathlib.Path(dest); evidence=pathlib.Path(receipt)
 if out.exists() or evidence.exists() or not out.parent.is_dir() or not evidence.parent.is_dir():
  raise RuntimeError("REFUSE_OVERWRITE_OR_FOREIGN_DIRECTORY")
 checked={}
 for name,(commit,path,expected) in PINS.items():
  raw=cmd(repo,"show",commit+":"+path)
  sha=cmd(repo,"rev-parse",commit+":"+path).decode().strip()
  if oid(raw)!=expected or sha!=expected:raise RuntimeError("ORIGINAL_BLOB_MISMATCH_"+name)
  checked[name]=raw
 out.mkdir()
 for name,raw in checked.items():(out/name).write_bytes(raw)
 data={"package":"B01-62-W21-EXTERNAL-TRUST-WITNESS-001","dependency_669":"d6ec0c82e388e65c1e02ddfd920bb50a1f6ef121",
 "api":"#681->#691 selected; #682 ALT_HOLD","new_host":"#690; original #647 frozen",
 "original_git_pins":{k:{"sha":v[0],"path":v[1],"blob":v[2]} for k,v in sorted(PINS.items())},
 "trust_policy":"separate root MUST be independently trustworthy, not SHA authentication",
 "native_g1":"NOT_RUN","codex_ack":False,"integrated":False}
 packed=(json.dumps(data,sort_keys=True,indent=2)+"\n").encode()
 evidence.write_bytes(packed)
 print("B0162_W21_ORIGINAL_GIT_BLOBS_PINNED=4/4")
 print("B0162_W21_TRUST_SOURCE_RECEIPT_SHA256="+hashlib.sha256(packed).hexdigest())
if __name__=="__main__":
 p=argparse.ArgumentParser()
 for n in ("repo","out","receipt"):p.add_argument("--"+n,required=True)
 a=p.parse_args();main(a.repo,a.out,a.receipt)
