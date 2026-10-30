import hashlib,io,json,re,subprocess,sys,zipfile
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4]
BASE="ce46a5e0cc5d26df6c4ece723e8efe20d70fc4b1"
PARENT="b8ca29c7707cd6b4338d0b9772d74dee676d49d2"
W24="Assets/B0123CanonicalRigPortW24/"
NEW="Tools/B01/B01-23/P17W25PhysicalRollbackR2/"
WORK=".github/workflows/b01-23-p17w25-physical-rollback-r2.yml"
MANIFEST="B01-23/W25/PACKAGE_MANIFEST.json"
FIXES={W24+"Runtime/CanonicalPhysicalRigPortW24.cs",
       W24+"Tests/PlayMode/CanonicalPhysicalRigPortW24NativeTests.cs"}
FROZEN=[
("d94400d95d13db7f27e1533df05c9292d5380431",
 "Assets/B0123SecureReattachV2/Runtime/AtomicDockLedger.cs"),
("33094f154c7b91119b36d58aab285e6e83c5de57",
 "Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs"),
(PARENT,"Assets/B0123CanonicalSaveApi001/Core/CanonicalWorldGateway.cs")]
def git(*args):return subprocess.check_output(["git",*args],cwd=ROOT).decode().strip()
def content(path):return subprocess.check_output(["git","show","HEAD:"+path],cwd=ROOT)
def paths():
 all=git("ls-tree","-r","--name-only","HEAD").splitlines()
 return sorted(x for x in all if x=="Assets/B0123CanonicalRigPortW24.meta" or
     x.startswith(W24) or x.startswith(NEW) or x==WORK)
def check():
 subprocess.run(["git","merge-base","--is-ancestor",BASE,"HEAD"],cwd=ROOT,check=True)
 changed=set(git("diff","--name-only",BASE,"HEAD").splitlines())
 if changed!=FIXES|{x for x in paths() if x.startswith(NEW) or x==WORK}:
  raise ValueError("Foreign source modifications "+str(changed))
 for ref,path in FROZEN:
  if git("rev-parse",ref+":"+path)!=git("rev-parse","HEAD:"+path):
   raise ValueError("Frozen original SHA drift "+path)
 root=W24+"Runtime/CanonicalPhysicalRigPortW24.cs"
 runtime=content(root).decode()
 for token in ("resumePending","resumeLinear[i]","resumeAngular[i]",
               "r.Body.linearVelocity","r.Body.angularVelocity","world"):
  if token not in runtime:raise ValueError("Lost true live-velocity rollback "+token)
 old=content(W24+"Tests/PlayMode/CanonicalPhysicalRigPortW24NativeTests.cs").decode()
 if old.count("[UnityTest]")!=4 or "AbortedCompetingWorld_PreservesActualLiveVelocityAndAngularVelocity" not in old:
  raise ValueError("Native Unity regression is not authored")
 meta=[p for p in paths() if p.endswith(".meta")]
 guids=[]
 for x in meta:
  m=re.search(r"(?m)^guid: ([0-9a-f]{32})$",content(x).decode())
  if not m:raise ValueError("Missing original W24 GUID")
  guids.append(m.group(1))
 if len(guids)!=9 or len(set(guids))!=9:
  raise ValueError("Existing nine W24 GUIDs must remain stable")
def info(name):
 z=zipfile.ZipInfo(name,(1980,1,1,0,0,0))
 z.compress_type=zipfile.ZIP_STORED;z.external_attr=0o100644<<16
 z.create_system=3;z.flag_bits=0
 return z
def build():
 check()
 members={x:content(x) for x in paths()}
 receipt={"package":"B01-23-PHYSICAL-ROLLBACK-R2-001","version":"2.0.0",
 "base_exact_original_W24":BASE,"original_parent_api":PARENT,
 "head":git("rev-parse","HEAD"),
 "members":{x:hashlib.sha256(b).hexdigest() for x,b in members.items()},
 "guid_count":9,"asmdef_count":2,"native_unity":"NOT_RUN",
 "physx_native":"NOT_RUN","live_510_legacy_rigs":"NOT_IMPORTED",
 "session_773":"P0_HOLD_UNSAFE_NONCANONICAL_PATH",
 "codex_ack":False,"ready_for_integrator":False}
 out=io.BytesIO()
 with zipfile.ZipFile(out,"w") as z:
  for x in sorted(members):z.writestr(info(x),members[x])
  z.writestr(info(MANIFEST),json.dumps(receipt,sort_keys=True,
   separators=(",",":")).encode())
 return out.getvalue()
def verify(raw):
 with zipfile.ZipFile(io.BytesIO(raw)) as z:
  names=z.namelist()
  if z.testzip()!=None or len(names)!=len(set(names)) or set(names)!=set(paths())|{MANIFEST}:
   raise ValueError("Foreign/missing member")
  data=json.loads(z.read(MANIFEST))
  if data["native_unity"]!="NOT_RUN" or data["codex_ack"] or data["session_773"]!="P0_HOLD_UNSAFE_NONCANONICAL_PATH":
   raise ValueError("Forged Unity native PASS or unsafe selected storage")
  for path in paths():
   if z.read(path)!=content(path) or data["members"][path]!=hashlib.sha256(content(path)).hexdigest():
    raise ValueError("Mutated original source")
 return data
if __name__=="__main__":
 raw=build();verify(raw)
 print("W25_PHYSICAL_ROLLBACK_REAL_GIT_INNER_ZIP",len(raw),"bytes",
       len(paths())+1,"entries SHA256",hashlib.sha256(raw).hexdigest(),
       "HEAD",git("rev-parse","HEAD"))
 if len(sys.argv)==3 and sys.argv[1]=="build":
  dest=ROOT/sys.argv[2];dest.parent.mkdir(parents=True,exist_ok=True);dest.write_bytes(raw)
