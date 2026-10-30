#!/usr/bin/env python3
"""Exact physical W22 owner files only. Not a human, Unity or security attestation."""
import hashlib,json,pathlib,sys
ROOT=pathlib.Path(__file__).resolve().parents[3]
def main():
 m=json.loads(pathlib.Path(__file__).with_name("manifest.json").read_text(encoding="utf8"))
 assert m["package"]=="B01-13-SOFTLOCK-RECOVERY-ROUTES-001/R1"
 assert m["frozen_game"]=="d94400d95d13db7f27e1533df05c9292d5380431"
 assert m["frozen_save"]=="33094f154c7b91119b36d58aab285e6e83c5de57"
 assert m["frozen_host"]=="efb2b06eafedab5469b57f163152d96aca3c5915"
 assert m["selected_r4"]=="09c1809b73324a6d31c13e5d0143de62ac1e3ca6"
 assert m["frozen_econ"]=="12572117a3dce369257feabc57021cde58023369"
 assert len(m["files"])==5
 for name,expected in sorted(m["files"].items()):
  assert name.startswith("Research/B01-13/P17T/") or name==".github/workflows/b01-13-p17w22-recovery-routes.yml","OUTSIDE_OWNER:"+name
  f=ROOT/name
  assert f.is_file() and not f.is_symlink(),"MISSING_OR_SYMLINK:"+name
  b=f.read_bytes()
  sha=hashlib.sha1(b"blob "+str(len(b)).encode("ascii")+b"\x00"+b).hexdigest()
  assert sha==expected["git_blob_sha1"] and len(b)==expected["bytes"],"SOURCE_DRIFT:"+name
  print("SOURCE_PASS",name,"bytes",len(b),"git",sha,"sha256",hashlib.sha256(b).hexdigest(),flush=True)
 print("MANIFEST_PASS",len(m["files"]),flush=True)
if __name__=="__main__":
 try:main()
 except Exception as e:print("MANIFEST_FAIL",repr(e),file=sys.stderr);sys.exit(2)
