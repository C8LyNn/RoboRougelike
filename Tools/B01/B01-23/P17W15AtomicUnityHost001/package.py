#!/usr/bin/env python3
"""B01-23 W15 owner-only exact Git source ZIP + strict disposable receiver rollback."""
import argparse,hashlib,io,json,os,shutil,subprocess,sys,zipfile
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4]
ASSET="Assets/B0123SecureV2HostBridge001"
TOOLS="Tools/B01/B01-23/P17W15AtomicUnityHost001/"
WORKFLOW=".github/workflows/b01-23-p17w15-atomic-unity-host.yml"
GAME="d94400d95d13db7f27e1533df05c9292d5380431"
SAVE="33094f154c7b91119b36d58aab285e6e83c5de57"
SOURCE_BLOB="b52e511c370f5fca56c74029050844dc8cbb7f9a"
W06_BLOB="fa1aa5e531927cbe73cf5fdfe161b4a981316db5"
MANIFEST="PACKAGE_MANIFEST.json"
def git(*args):
 return subprocess.check_output(["git",*args],cwd=ROOT).decode().strip()
def blob(p):return subprocess.check_output(["git","show","HEAD:"+p],cwd=ROOT)
def digest(v):return hashlib.sha256(v).hexdigest()
def paths():
 if git("rev-parse",SAVE)!=SAVE or git("rev-parse",GAME)!=GAME:raise ValueError("FROZEN_SOURCE_MISSING")
 if git("rev-parse",SAVE+":Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs")!=SOURCE_BLOB:
  raise ValueError("SAVE556_SOURCE_BLOB_CHANGED")
 if git("rev-parse",SAVE+":Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs")!=W06_BLOB:
  raise ValueError("ORIGINAL_W06_BLOB_CHANGED")
 x=sorted([ASSET+".meta",WORKFLOW]+git("ls-tree","-r","--name-only","HEAD",ASSET+"/").splitlines()+
    git("ls-tree","-r","--name-only","HEAD",TOOLS).splitlines())
 if len(x)!=20 or len(x)!=len(set(x)):raise ValueError("OWNED_FILE_COUNT_OR_DUPLICATE")
 if any(not(p==ASSET+".meta" or p==WORKFLOW or p.startswith(ASSET+"/") or p.startswith(TOOLS)) for p in x):
  raise ValueError("OUT_OF_SCOPE_SOURCE")
 ids=[];asm={}
 for p in x:
  if p.endswith(".meta"):
   gs=[line.split(":",1)[1].strip() for line in blob(p).decode().splitlines() if line.startswith("guid:")]
   if len(gs)!=1 or len(gs[0])!=32:raise ValueError("BAD_UNITY_META "+p)
   ids+=gs
  if p.endswith(".asmdef"):
   j=json.loads(blob(p));asm[j["name"]]=j["references"]
 if len(ids)!=9 or len(set(ids))!=9 or asm!={"B0123.SecureV2HostBridge.Runtime":["B0123.DetachedSaveContract.Runtime","B0123.DebrisRepulse.Runtime"],"B0123.SecureV2HostBridge.PlayMode":["B0123.SecureV2HostBridge.Runtime","B0123.DetachedSaveContract.Runtime"]}:
  raise ValueError("UNITY_META_OR_ASMDEF_GRAPH_DRIFT")
 return x
def info(path):
 z=zipfile.ZipInfo(path,(1980,1,1,0,0,0));z.compress_type=zipfile.ZIP_STORED
 z.create_system=3;z.external_attr=0o100644<<16
 return z
def make():
 names=paths();files={p:blob(p) for p in names}
 m={"package":"B01-23-UNITY-ATOMIC-SAVEHOST-001","version":"1.0.0","source_sha":git("rev-parse","HEAD"),
    "selected_gameplay_sha":GAME,"selected_save_sha":SAVE,"frozen_save_source_blob":SOURCE_BLOB,
    "frozen_w06_source_blob":W06_BLOB,"native_unity":"NOT_RUN","game510_private_ledger_hydrated":False,
    "codex_ack":False,"unity_meta_guids":9,
    "files":[{"path":p,"bytes":len(files[p]),"sha256":digest(files[p])} for p in names]}
 files[MANIFEST]=(json.dumps(m,sort_keys=True,separators=(",",":"))+"\n").encode()
 buf=io.BytesIO()
 with zipfile.ZipFile(buf,"w") as z:
  for p in sorted(files):z.writestr(info(p),files[p])
 return buf.getvalue()
def verify(raw):
 members=paths()
 with zipfile.ZipFile(io.BytesIO(raw)) as z:
  names=z.namelist()
  if len(names)!=21 or len(set(names))!=21 or set(names)!=set(members)|{MANIFEST}:
   raise ValueError("ZIP_FILE_SCOPE")
  if any(n.startswith("/") or "\\" in n or ".." in n.split("/") for n in names):
   raise ValueError("ZIP_PATH_TRAVERSAL")
  m=json.loads(z.read(MANIFEST))
  if m.get("package")!="B01-23-UNITY-ATOMIC-SAVEHOST-001" or m.get("version")!="1.0.0" or \
     m.get("source_sha")!=git("rev-parse","HEAD") or m.get("selected_gameplay_sha")!=GAME or \
     m.get("selected_save_sha")!=SAVE or m.get("frozen_save_source_blob")!=SOURCE_BLOB or \
     m.get("frozen_w06_source_blob")!=W06_BLOB or m.get("native_unity")!="NOT_RUN" or \
     m.get("codex_ack") is not False or m.get("game510_private_ledger_hydrated") is not False or \
     m.get("unity_meta_guids")!=9:
   raise ValueError("MANIFEST_GATE_OR_ORIGIN")
  expected=[{"path":p,"bytes":len(blob(p)),"sha256":digest(blob(p))} for p in members]
  if m.get("files")!=expected:raise ValueError("MANIFEST_BYTES")
  result={p:z.read(p) for p in members}
  if any(result[p]!=blob(p) for p in members):raise ValueError("SOURCE_GIT_BYTES_CHANGED")
  result[MANIFEST]=z.read(MANIFEST)
  return result
def scratch_guard(path):
 absolute=path.resolve()
 if absolute==ROOT.resolve() or ROOT.resolve() in absolute.parents:raise ValueError("REFUSE_REPO_PATH")
 cursor=path.absolute()
 while True:
  if cursor.is_symlink():raise ValueError("SYMLINK_IN_RECEIVER_PARENT")
  if cursor==cursor.parent:break
  cursor=cursor.parent
def stage(data,target):
 scratch_guard(target)
 if os.path.lexists(str(target)):raise ValueError("REFUSE_OVERWRITE_EXISTING_RECEIVER")
 files=verify(data)
 target.mkdir()
 try:
  for p,b in files.items():
   f=target/p;f.parent.mkdir(parents=True,exist_ok=True);f.write_bytes(b)
 except Exception:
  # Only this newly-created exclusive scratch; do not invoke on a shared path.
  shutil.rmtree(target);raise
def rollback(data,target):
 scratch_guard(target)
 files=verify(data)
 if not target.is_dir() or target.is_symlink():raise ValueError("SCRATCH_NOT_DIRECTORY")
 allowed={Path(".")}
 for p in files:
  parent=Path(p).parent
  while parent!=Path("."):
   allowed.add(parent);parent=parent.parent
 found=set()
 for p in target.rglob("*"):
  if p.is_symlink():raise ValueError("FOREIGN_SYMLINK")
  rel=p.relative_to(target)
  if p.is_dir():
   if rel not in allowed:raise ValueError("FOREIGN_EMPTY_OR_NONEMPTY_DIRECTORY "+str(rel))
  elif p.is_file():
   key=rel.as_posix()
   if key not in files or p.read_bytes()!=files[key]:raise ValueError("FOREIGN_OR_DIRTY_FILE "+key)
   found.add(key)
  else:raise ValueError("UNSUPPORTED_FILE_TYPE")
 if found!=set(files):raise ValueError("MISSING_PACKAGE_FILES")
 shutil.rmtree(target)
if __name__=="__main__":
 ap=argparse.ArgumentParser()
 ap.add_argument("cmd",choices=("build","verify","stage","rollback"));ap.add_argument("--zip",required=True)
 ap.add_argument("--target");a=ap.parse_args()
 try:
  z=Path(a.zip)
  if a.cmd=="build":
   if z.exists():raise ValueError("REFUSE_ZIP_OVERWRITE")
   raw=make();verify(raw);z.parent.mkdir(parents=True,exist_ok=True);z.write_bytes(raw)
   print("ORIGINAL_W15_UNITY_HOST_INNER_ZIP="+str(len(raw))+" SHA256="+digest(raw)+" MEMBERS=21 SOURCE_SHA="+git("rev-parse","HEAD"))
  elif a.cmd=="verify":verify(z.read_bytes());print("ORIGINAL_W15_SOURCE_ZIP_VERIFIED "+digest(z.read_bytes()))
  else:
   if not a.target:raise ValueError("EXTERNAL_TARGET_REQUIRED")
   if a.cmd=="stage":stage(z.read_bytes(),Path(a.target));print("EXCLUSIVE_SCRATCH_STAGE_PASS")
   else:rollback(z.read_bytes(),Path(a.target));print("EXCLUSIVE_SCRATCH_ROLLBACK_PASS")
 except (ValueError,OSError,KeyError,subprocess.CalledProcessError,zipfile.BadZipFile) as e:
  print("W15_SAFE_TRANSPORT_RED "+str(e),file=sys.stderr);sys.exit(3)
