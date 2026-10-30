#!/usr/bin/env python3
"""Verify exact physical scoped original B01-13 W21 files; NOT a Unity/QA attestation."""
import hashlib,json,pathlib,sys
ROOT=pathlib.Path(__file__).resolve().parents[3]
def go():
 m=json.loads(pathlib.Path(__file__).with_name("manifest.json").read_text(encoding="utf8"))
 assert m["package"]=="B01-13-REBUILD-CHOICE-REHEARSAL-001/R1"
 assert m["original_game"]=="d94400d95d13db7f27e1533df05c9292d5380431"
 assert m["original_save"]=="33094f154c7b91119b36d58aab285e6e83c5de57"
 assert m["original_host"]=="efb2b06eafedab5469b57f163152d96aca3c5915"
 assert m["original_econ"]=="12572117a3dce369257feabc57021cde58023369"
 assert m["selected_r4"]=="09c1809b73324a6d31c13e5d0143de62ac1e3ca6"
 assert len(m["files"])==5
 for name,expected in sorted(m["files"].items()):
  assert name.startswith("Research/B01-13/P17S/") or name==".github/workflows/b01-13-p17w21-rebuild-choice.yml","OUT_OF_SCOPE:"+name
  f=ROOT/name
  assert f.is_file() and not f.is_symlink(),"NOT_REGULAR:"+name
  b=f.read_bytes()
  sha=hashlib.sha1(b"blob "+str(len(b)).encode("ascii")+b"\0"+b).hexdigest()
  assert sha==expected["git_blob_sha1"] and len(b)==expected["bytes"],"SOURCE_DRIFT:"+name
  print("SOURCE_PASS",name,"bytes",len(b),"gitblob",sha,"sha256",hashlib.sha256(b).hexdigest(),flush=True)
 print("MANIFEST_PASS",len(m["files"]),flush=True)
if __name__=="__main__":
 try:go()
 except Exception as e:print("MANIFEST_FAIL",repr(e),file=sys.stderr);sys.exit(2)
