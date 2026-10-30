#!/usr/bin/env python3
"""Original immutable B01-13 W17 file bytes/Git blob audit, not Unity evidence."""
import hashlib,json,pathlib,sys
ROOT=pathlib.Path(__file__).resolve().parents[3]
def run():
 m=json.loads(pathlib.Path(__file__).with_name("manifest.json").read_text(encoding="utf8"))
 assert m["id"]=="B01-13-OFFLINE-REVIEW-BOARD-001/R1"
 assert m["upstream_w16"]=="3f83937874c69cb8744247cb51606dcb2fa0fd91"
 assert m["upstream_w16_blob"]=="56d158bb6c107a04fc42e7ee78d6a07317997729"
 assert m["upstream_w15_blob"]=="6ec383f0b3e341d58f9b5e971fc435a74d78a583"
 assert m["only_game"]=="d94400d95d13db7f27e1533df05c9292d5380431"
 assert m["only_save"]=="33094f154c7b91119b36d58aab285e6e83c5de57"
 assert len(m["files"])==5
 for name,entry in sorted(m["files"].items()):
  assert name.startswith("Research/B01-13/P17O/") or name==".github/workflows/b01-13-p17w17-review-board.yml"
  f=ROOT/name
  assert f.is_file() and not f.is_symlink(),"NOT_REGULAR:"+name
  b=f.read_bytes()
  blob=hashlib.sha1(b"blob "+str(len(b)).encode("ascii")+b"\x00"+b).hexdigest()
  assert blob==entry["git_blob_sha1"] and len(b)==entry["bytes"],"SOURCE_DRIFT:"+name
  print("SOURCE_PASS",name,"bytes",len(b),"git",blob,"sha256",hashlib.sha256(b).hexdigest(),flush=True)
 print("MANIFEST_PASS",len(m["files"]),flush=True)
if __name__=="__main__":
 try:run()
 except Exception as exc:print("MANIFEST_FAIL",str(exc),file=sys.stderr);sys.exit(2)
