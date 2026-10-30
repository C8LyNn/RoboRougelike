#!/usr/bin/env python3
"""Deterministic owner-exclusive actual Git source ZIP for #510 additive contract ONLY."""
import hashlib,io,json,subprocess,sys,zipfile
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4]
DIR="Tools/B01/B01-23/P17W13SecureV2SaveContract001/"
SOURCE="d94400d95d13db7f27e1533df05c9292d5380431"
WF=".github/workflows/b01-23-p17-w13-secure-v2-save.yml"
FILES=[
DIR+"StableIdSnapshotCodec.cs", DIR+"Program.cs",
DIR+"SaveContractTests.csproj", DIR+"README.md",
DIR+"package.py",DIR+"test_package.py",WF]
def git(*args):return subprocess.check_output(["git",*args],cwd=ROOT)
def digest(b):return hashlib.sha256(b).hexdigest()
def info(name):
 z=zipfile.ZipInfo(name,(1980,1,1,0,0,0));z.compress_type=zipfile.ZIP_STORED
 z.create_system=3;z.external_attr=0o100644<<16
 return z
def original():
 head=git("rev-parse","HEAD").decode().strip()
 blobs={p:git("show",head+":"+p) for p in FILES}
 manifest={"package_id":"B01-23-SECUREV2-SAVE-CONTRACT-001","schema_version":1,
 "only_game_candidate_sha":SOURCE,"own_source_sha":head,"unity_native":"NOT_RUN",
 "canonical_save_restored":False,"codex_ack":False,
 "members":[{"path":p,"bytes":len(blobs[p]),"sha256":digest(blobs[p])} for p in sorted(blobs)]}
 blobs["PACKAGE_MANIFEST.json"]=(json.dumps(manifest,sort_keys=True,separators=(",",":"))+"\n").encode()
 output=io.BytesIO()
 with zipfile.ZipFile(output,"w") as z:
  for name in sorted(blobs):z.writestr(info(name),blobs[name])
 return output.getvalue()
def verify(raw):
 with zipfile.ZipFile(io.BytesIO(raw)) as z:
  names=z.namelist()
  if len(names)!=len(FILES)+1 or set(names)!=set(FILES)|{"PACKAGE_MANIFEST.json"} or len(set(names))!=len(names):raise ValueError("FILE_SET")
  if any(p.startswith("/") or "\\" in p or ".." in p.split("/") for p in names):raise ValueError("PATH_TRAVERSAL")
  m=json.loads(z.read("PACKAGE_MANIFEST.json"))
  if m.get("only_game_candidate_sha")!=SOURCE or m.get("schema_version")!=1 or m.get("unity_native")!="NOT_RUN" or m.get("codex_ack") is not False or m.get("canonical_save_restored") is not False:raise ValueError("FORGED_STATUS")
  head=git("rev-parse","HEAD").decode().strip()
  if m.get("own_source_sha")!=head:raise ValueError("UNPINNED_SOURCE")
  expected={p:git("show",head+":"+p) for p in FILES}
  if m["members"]!=[{"path":p,"bytes":len(expected[p]),"sha256":digest(expected[p])} for p in sorted(expected)]:raise ValueError("MANIFEST_MISMATCH")
  if any(z.read(p)!=expected[p] for p in FILES):raise ValueError("GIT_SOURCE_MISMATCH")
 return True
if __name__=="__main__":
 from pathlib import Path
 path=Path(sys.argv[1])
 if path.exists():raise ValueError("REFUSE_OVERWRITE")
 raw=original();verify(raw);path.write_bytes(raw)
 print("ORIGINAL_W13_SAVE_CONTRACT_ZIP_BYTES="+str(len(raw))+" SHA256="+digest(raw)+" SOURCE_SHA="+git("rev-parse","HEAD").decode().strip())
