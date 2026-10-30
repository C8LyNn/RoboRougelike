#!/usr/bin/env python3
"""Physical Git SHA1 blob, byte length and SHA256 check; does not claim Unity G1."""
import hashlib,json,pathlib,sys
ROOT=pathlib.Path(__file__).resolve().parents[3]
def main():
 m=json.loads(pathlib.Path(__file__).with_name("manifest.json").read_text(encoding="utf8"))
 assert m["package"]=="B01-13-DAMAGE-CUE-TUTORIAL-001/R1"
 assert m["selected_game"]=="d94400d95d13db7f27e1533df05c9292d5380431"
 assert m["selected_save"]=="33094f154c7b91119b36d58aab285e6e83c5de57"
 assert m["selected_host"]=="efb2b06eafedab5469b57f163152d96aca3c5915"
 assert m["alternative"]=="#604 ALT_HOLD"
 assert len(m["files"])==5
 for name,entry in sorted(m["files"].items()):
  assert name.startswith("Research/B01-13/P17Q/") or name==".github/workflows/b01-13-p17w19-damage-cues.yml", "OWNER_SCOPE:"+name
  f=ROOT/name
  assert f.is_file() and not f.is_symlink(),"NOT_REGULAR:"+name
  b=f.read_bytes()
  sha=hashlib.sha1(b"blob "+str(len(b)).encode("ascii")+b"\x00"+b).hexdigest()
  assert sha==entry["git_blob_sha1"] and len(b)==entry["bytes"],"SOURCE_DRIFT:"+name
  print("SOURCE_PASS",name,"bytes",len(b),"git",sha,"sha256",hashlib.sha256(b).hexdigest(),flush=True)
 print("MANIFEST_PASS",len(m["files"]),flush=True)
if __name__=="__main__":
 try:main()
 except Exception as exc:print("MANIFEST_FAIL",repr(exc),file=sys.stderr);sys.exit(2)
