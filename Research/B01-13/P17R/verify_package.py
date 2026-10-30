#!/usr/bin/env python3
"""B01-13 W20 original source byte/blob integrity; not native Unity or real economy."""
import hashlib,json,pathlib,sys
ROOT=pathlib.Path(__file__).resolve().parents[3]
def verify():
 m=json.loads(pathlib.Path(__file__).with_name("manifest.json").read_text(encoding="utf-8"))
 assert m["package"]=="B01-13-REBUILD-ECON-LAB-001/R1"
 assert m["original_game"]=="d94400d95d13db7f27e1533df05c9292d5380431"
 assert m["original_save"]=="33094f154c7b91119b36d58aab285e6e83c5de57"
 assert m["original_host"]=="efb2b06eafedab5469b57f163152d96aca3c5915"
 assert m["original_econ"]=="12572117a3dce369257feabc57021cde58023369"
 assert len(m["files"])==5
 for p,x in sorted(m["files"].items()):
  assert p.startswith("Research/B01-13/P17R/") or p==".github/workflows/b01-13-p17w20-economy-lab.yml","WRONG_OWNER_SCOPE:"+p
  path=ROOT/p
  assert path.is_file() and not path.is_symlink(),"NOT_REGULAR:"+p
  b=path.read_bytes()
  sha1=hashlib.sha1(b"blob "+str(len(b)).encode("ascii")+b"\0"+b).hexdigest()
  assert sha1==x["git_blob_sha1"] and len(b)==x["bytes"],"SOURCE_DRIFT:"+p
  print("SOURCE_PASS",p,"bytes",len(b),"blob",sha1,"sha256",hashlib.sha256(b).hexdigest(),flush=True)
 print("MANIFEST_PASS",len(m["files"]),flush=True)
if __name__=="__main__":
 try:verify()
 except Exception as ex:print("MANIFEST_FAIL",repr(ex),file=sys.stderr);sys.exit(2)
