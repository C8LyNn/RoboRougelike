#!/usr/bin/env python3
"""B01-13 W23 checks original immutable W22 source plus scoped W23 files."""
import pathlib,hashlib,json,sys
ROOT=pathlib.Path(__file__).resolve().parents[3]
def run():
 m=json.loads(pathlib.Path(__file__).with_name("manifest.json").read_text(encoding="utf8"))
 assert m["package"]=="B01-13-ECON-SAFETY-PLAYTEST-PROTOCOL-001/R1"
 assert m["parent_commit"]=="5eb72088015062962ba8e9ac60ae684c8fba4bdb"
 assert m["parent_blob"]=="d40993ae55940477a8086a403dede831d6419d62"
 assert len(m["files"])==6
 assert m["files"]["Research/B01-13/P17T/recovery_routes.mjs"]["git_blob_sha1"]==m["parent_blob"]
 for name,expected in sorted(m["files"].items()):
  assert name=="Research/B01-13/P17T/recovery_routes.mjs" or name.startswith("Research/B01-13/P17U/") or name==".github/workflows/b01-13-p17w23-econ-human-protocol.yml","SCOPE:"+name
  f=ROOT/name
  assert f.is_file() and not f.is_symlink(),"NOT_REGULAR:"+name
  b=f.read_bytes()
  git=hashlib.sha1(b"blob "+str(len(b)).encode()+b"\0"+b).hexdigest()
  assert git==expected["git_blob_sha1"] and len(b)==expected["bytes"],"SOURCE_DRIFT:"+name
  print("SOURCE_PASS",name,"bytes",len(b),"git",git,"sha256",hashlib.sha256(b).hexdigest(),flush=True)
 print("MANIFEST_PASS",len(m["files"]),flush=True)
if __name__=="__main__":
 try:run()
 except Exception as exc:print("FAIL_CLOSED",repr(exc),file=sys.stderr);sys.exit(2)
