#!/usr/bin/env python3
import hashlib, json, pathlib, sys
ROOT=pathlib.Path(__file__).resolve().parents[3]
P=pathlib.Path(__file__).with_name("manifest.json")
def run():
 m=json.loads(P.read_text(encoding="utf8"))
 assert m["package"]=="B01-13-SAVE-SCREENSHOT-WITNESS-001/R1"
 assert m["upstream_w13"]=="aee4315304ca3e2fe3e155242d7d181260089efc"
 assert m["upstream_blob"]=="94c621f54fffd7e1325700b88b441652755c0feb"
 assert m["selected_reattach"]=="d94400d95d13db7f27e1533df05c9292d5380431"
 assert len(m["files"])==5
 for name,pin in sorted(m["files"].items()):
  assert name.startswith("Research/B01-13/P17L/") or name==".github/workflows/b01-13-w14-screenshot-witness.yml"
  f=ROOT/name
  assert f.is_file() and not f.is_symlink(), "FILE_MISSING_OR_LINK:"+name
  b=f.read_bytes()
  blob=hashlib.sha1(b"blob "+str(len(b)).encode()+b"\x00"+b).hexdigest()
  assert len(b)==pin["bytes"] and blob==pin["git_blob_sha1"], "SOURCE_DRIFT:"+name
  print("PASS",name,"bytes",len(b),"git",blob,"sha256",hashlib.sha256(b).hexdigest(),flush=True)
 print("MANIFEST_PASS",len(m["files"]))
if __name__=="__main__":
 try: run()
 except Exception as e: print("MANIFEST_FAIL",str(e),file=sys.stderr);sys.exit(2)
