#!/usr/bin/env python3
"""Physical original code/test/docs/workflow Git blob and byte integrity, not Unity."""
import hashlib,json,pathlib,sys
ROOT=pathlib.Path(__file__).resolve().parents[3]
def run():
 m=json.loads(pathlib.Path(__file__).with_name("manifest.json").read_text(encoding="utf8"))
 assert m["package"]=="B01-13-REVIEW-DECISION-HANDOFF-001/R1"
 assert m["original_w17_commit"]=="f05790e274f31ac39c6ae4b2e873db896a3b36d8"
 assert m["original_w17_blob"]=="d9f4267ad2f827a84e08f63e4c5880af1b8c739e"
 assert m["original_w16_blob"]=="56d158bb6c107a04fc42e7ee78d6a07317997729"
 assert m["original_w15_blob"]=="6ec383f0b3e341d58f9b5e971fc435a74d78a583"
 assert m["selected_game"]=="d94400d95d13db7f27e1533df05c9292d5380431"
 assert m["selected_save"]=="33094f154c7b91119b36d58aab285e6e83c5de57"
 assert m["selected_host"]=="efb2b06eafedab5469b57f163152d96aca3c5915"
 assert len(m["files"])==5
 for name,x in sorted(m["files"].items()):
  assert name.startswith("Research/B01-13/P17P/") or name==".github/workflows/b01-13-p17w18-decision-handoff.yml","SCOPE:"+name
  f=ROOT/name
  assert f.is_file() and not f.is_symlink(),"NOT_REGULAR:"+name
  b=f.read_bytes()
  git=hashlib.sha1(b"blob "+str(len(b)).encode("ascii")+b"\x00"+b).hexdigest()
  assert git==x["git_blob_sha1"] and len(b)==x["bytes"],"SOURCE_DRIFT:"+name
  print("SOURCE_PASS",name,"bytes",len(b),"blob",git,"sha256",hashlib.sha256(b).hexdigest(),flush=True)
 print("MANIFEST_PASS",len(m["files"]),flush=True)
if __name__=="__main__":
 try:run()
 except Exception as e:print("MANIFEST_FAIL",str(e),file=sys.stderr);sys.exit(2)
