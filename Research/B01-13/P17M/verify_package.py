#!/usr/bin/env python3
"""Validate exact original source/doc/workflow physical byte pins against Git."""
import hashlib,json,pathlib,sys
root=pathlib.Path(__file__).resolve().parents[3]
def run():
 m=json.loads(pathlib.Path(__file__).with_name("manifest.json").read_text(encoding="utf8"))
 assert m["package"]=="B01-13-SAVELOAD-RECOVERY-METRICS-001/R1"
 assert m["selected_reattach_sha"]=="d94400d95d13db7f27e1533df05c9292d5380431"
 assert m["selected_save_sha"]=="33094f154c7b91119b36d58aab285e6e83c5de57"
 assert len(m["files"])==5
 for name,pin in sorted(m["files"].items()):
  assert name.startswith("Research/B01-13/P17M/") or name==".github/workflows/b01-13-p17w15-recovery-timeline.yml"
  f=root/name
  assert f.is_file() and not f.is_symlink(),"MISSING_OR_SYMLINK:"+name
  b=f.read_bytes()
  git=hashlib.sha1(b"blob "+str(len(b)).encode("ascii")+b"\x00"+b).hexdigest()
  assert len(b)==pin["bytes"] and git==pin["git_blob_sha1"],"PHYSICAL_DRIFT:"+name
  print("SOURCE_PASS",name,"bytes",len(b),"blob",git,"sha256",hashlib.sha256(b).hexdigest(),flush=True)
 print("MANIFEST_PASS",len(m["files"]))
if __name__=="__main__":
 try:run()
 except Exception as exc:print("MANIFEST_FAIL",str(exc),file=sys.stderr);sys.exit(2)
