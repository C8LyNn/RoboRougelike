#!/usr/bin/env python3
import hashlib,json,pathlib,sys
root=pathlib.Path(__file__).resolve().parents[3]
def run():
 m=json.loads((pathlib.Path(__file__).with_name("manifest.json")).read_text(encoding="utf8"))
 assert m["id"]=="B01-13-SAVE-VISUAL-PAIRS-001/R1"
 for name,pin in sorted(m["files"].items()):
  f=root/name
  assert f.is_file() and not f.is_symlink(),"MISSING_OR_SYMLINK:"+name
  data=f.read_bytes()
  blob=hashlib.sha1(b"blob "+str(len(data)).encode()+b"\x00"+data).hexdigest()
  assert len(data)==pin["bytes"] and blob==pin["git_blob_sha1"],"BYTES_DRIFT:"+name
  print("PASS",name,"bytes",len(data),"blob",blob,"sha256",hashlib.sha256(data).hexdigest(),flush=True)
 print("MANIFEST_PASS",len(m["files"]))
if __name__=="__main__":
 try:run()
 except Exception as e:print("MANIFEST_FAIL",str(e),file=sys.stderr);sys.exit(2)
