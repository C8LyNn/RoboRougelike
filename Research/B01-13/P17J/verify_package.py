#!/usr/bin/env python3
import hashlib,json,pathlib,sys
root=pathlib.Path(__file__).resolve().parents[3]
def run():
 m=json.loads((pathlib.Path(__file__).with_name("manifest.json")).read_text())
 assert m["id"]=="B01-13-REATTACH-TRIAL-MATRIX-001/R1"
 for name,item in sorted(m["files"].items()):
  f=root/name
  assert f.is_file() and not f.is_symlink(),name
  b=f.read_bytes()
  git=hashlib.sha1(b"blob "+str(len(b)).encode()+b"\0"+b).hexdigest()
  assert len(b)==item["bytes"] and git==item["git_sha1"],"DRIFT "+name
  print("PASS",name,"bytes",len(b),"git",git,"sha256",hashlib.sha256(b).hexdigest(),flush=True)
 print("MANIFEST_PASS",len(m["files"]))
if __name__=="__main__":
 try:run()
 except Exception as e:print("FAIL_CLOSED",str(e),file=sys.stderr);sys.exit(2)
