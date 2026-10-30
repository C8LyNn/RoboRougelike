#!/usr/bin/env python3
import hashlib,json,pathlib,sys
ROOT=pathlib.Path(__file__).resolve().parents[3]
def run():
 m=json.loads(pathlib.Path(__file__).with_name("manifest.json").read_text(encoding="utf8"))
 assert m["id"]=="B01-13-RATER-CONSENSUS-001/R1"
 assert m["original_w15_commit"]=="acc2288f72d47bd3969f840524cf8c5d07c0c6d2"
 assert m["original_w15_blob"]=="6ec383f0b3e341d58f9b5e971fc435a74d78a583"
 assert m["selected_game_sha"]=="d94400d95d13db7f27e1533df05c9292d5380431"
 assert m["selected_save_sha"]=="33094f154c7b91119b36d58aab285e6e83c5de57"
 assert len(m["files"])==5
 for name,entry in sorted(m["files"].items()):
  assert name.startswith("Research/B01-13/P17N/") or name==".github/workflows/b01-13-p17w16-rater-consensus.yml"
  f=ROOT/name
  assert f.is_file() and not f.is_symlink(),"NONREGULAR:"+name
  b=f.read_bytes()
  git=hashlib.sha1(b"blob "+str(len(b)).encode("ascii")+b"\x00"+b).hexdigest()
  assert git==entry["git_blob_sha1"] and len(b)==entry["bytes"],"SOURCE_DRIFT:"+name
  print("PASS",name,"bytes",len(b),"blob",git,"sha256",hashlib.sha256(b).hexdigest(),flush=True)
 print("MANIFEST_PASS",len(m["files"]),flush=True)
if __name__=="__main__":
 try:run()
 except Exception as exc:print("MANIFEST_FAIL",str(exc),file=sys.stderr);sys.exit(2)
