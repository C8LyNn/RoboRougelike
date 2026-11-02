#!/usr/bin/env python3
"""B01-40 W25 original-SHA independent source-only intake (never Unity G1)."""
import argparse, hashlib, json, os, re, shutil, subprocess, tempfile
from pathlib import Path

PINS={
 "r2":"3aaaec4027ee246fbfb6f209179f0e9507363473",
 "base":"8634e71dfcf633323073fe584a6c0884f1ca5d23",
 "api":"b8ca29c7707cd6b4338d0b9772d74dee676d49d2",
 "port":"ce46a5e0cc5d26df6c4ece723e8efe20d70fc4b1",
 "r5fix":"60c73a6f9ed2585cbb9bf7aeaad8c62eef0629cb",
 "r5":"cc7f73541517a18f4479bb155534bd4832d9985e",
 "game":"d94400d95d13db7f27e1533df05c9292d5380431",
 "codec":"33094f154c7b91119b36d58aab285e6e83c5de57",
}
STORE="Tools/B01/B01-62/P17W24CanonicalPathFixR2/CanonicalSingleAuthorityStoreV3R2.cs"
FIXTURE="Assets/B0123UnitySaveBridge001/Tests/IsolatedSaveWorldNativePlayModeTests.cs"
PREFIX="Assets/B0123CanonicalRigPortW24/"
PACKAGES={
 "game":("Assets/B0123DebrisRepulse001","Assets/B0123SecureReattachV2"),
 "codec":("Assets/B0123DetachedSaveContract001",),
 "api":("Assets/B0123CanonicalSaveApi001",),
 "r5fix":("Assets/B0123UnitySaveBridge001",),
 "port":("Assets/B0123CanonicalRigPortW24",),
}
def git(*args,check=True):
 p=subprocess.run(["git",*args],stdout=subprocess.PIPE,stderr=subprocess.PIPE)
 if check and p.returncode:raise RuntimeError("git "+str(args)+": "+p.stderr.decode(errors="replace")[-400:])
 return p.stdout
def file(commit,path):return git("show",commit+":"+path)
def tree(commit,prefix):
 return [p for p in git("ls-tree","-r","--name-only",commit,"--",prefix).decode().splitlines() if p.startswith(prefix)]
def diff(base,head):
 rows=git("diff","--name-status",PINS[base],PINS[head]).decode().splitlines()
 return [(r.split("\t")[0],r.split("\t")[-1]) for r in rows]
def require(test,why):
 if not test:raise RuntimeError("G0_HOLD: "+why)
def sha(data):return hashlib.sha256(data).hexdigest()
def check_pins():
 for name,ref in PINS.items():
  if git("cat-file","-e",ref+"^{commit}",check=False) is None:pass
  probe=subprocess.run(["git","cat-file","-e",ref+"^{commit}"],capture_output=True)
  if probe.returncode:
   git("fetch","--no-tags","origin",ref)
  require(git("rev-parse",ref).decode().strip()==ref,"MISSING_EXACT_SHA_"+name)
 for head,base in (("r2","base"),("port","api"),("r5fix","r5")):
  ok=subprocess.run(["git","merge-base","--is-ancestor",PINS[base],PINS[head]])
  require(ok.returncode==0,"NOT_DIRECT_SELECTED_LINEAGE_"+head)
 changed={n:diff(*parents) for n,parents in {
  "r2":("base","r2"),"port":("api","port"),"r5fix":("r5","r5fix")}.items()}
 require(len(changed["r2"])==8 and all(s=="A" for s,p in changed["r2"])
  and all(p.startswith("Tools/B01/B01-62/P17W24CanonicalPathFixR2/")
   or p==".github/workflows/b01-62-w24-749-canonical-path-r2.yml" for s,p in changed["r2"]),
  "R2_NOT_8_ADDITIVE_ONLY_TO_749")
 require(len(changed["port"])==21 and all(s=="A" for s,p in changed["port"])
  and all(p.startswith("Assets/B0123CanonicalRigPortW24")
   or p.startswith("Tools/B01/B01-23/P17W24CanonicalPhysicalRigPort001/")
   or p==".github/workflows/b01-23-p17w24-physical510-port.yml" for s,p in changed["port"]),
  "PORT_NOT_21_OWNER_ONLY_ADDITIONS")
 require(len(changed["r5fix"])==4 and
  [p for s,p in changed["r5fix"] if s=="M"]==[FIXTURE] and
  sum(1 for s,p in changed["r5fix"] if s=="A")==3,
  "R5_FIXTURE_NOT_EXCLUSIVE_GEN_FIX")
 return changed
def verify_abi():
 store=file(PINS["r2"],STORE).decode()
 require("CanonicalWorldSlotLease owner" in store and
  "CanonicalSingleAuthorityStoreV3R2" in store and
  "StringComparison.OrdinalIgnoreCase" in store,
  "STORE_SOURCE_UNEXPECTED_REVIEW_REQUIRED")
 fixture=file(PINS["r5fix"],FIXTURE).decode()
 old=file(PINS["r5"],FIXTURE).decode()
 require(old!=fixture and "Assert.AreEqual(5,savedSnapshot.Generation)" in fixture
  and "TryRestoreBytes(saved,41,5)" in fixture
  and "Assert.AreEqual(5,restored.Generation)" in fixture,
  "R5_SAVED_GENERATION_5_SOURCE_CONTRACT_MISSING")
 ledger=file(PINS["port"],PREFIX+"Runtime/CanonicalRigLedgerHydratorW24.cs").decode()
 port=file(PINS["port"],PREFIX+"Runtime/CanonicalPhysicalRigPortW24.cs").decode()
 require("AtomicDockLedger" in ledger and "ColdRestore" in ledger
  and "Rigidbody" in port and "ICanonicalRigFactory" in port
  and "SetExternalWritersQuiesced" in port and "AllOwnedPhysicalRootsDestroyed" in port,
  "PHYSICAL_OWNER_ABI_OR_LEDGER_MISSING")
 require("NOT_IMPORTED" in file(PINS["port"],"Tools/B01/B01-23/P17W24CanonicalPhysicalRigPort001/manifest.json").decode(),
  "PRIVATE_FROZEN_510_NOT_DECLARED_UNWIRED")
 return {"r5_fixture_g4_to_g5":"SOURCE_MATCH", "physical_port":"NEW_OWNER_NOT_EXISTING_PRIVATE_510",
  "selected_session":"R2_TOOLS_ONLY_NO_UNITY_BRIDGE"}
def guid_closure():
 owned={}; by_guid={}
 for owner,dirs in PACKAGES.items():
  for prefix in dirs:
   rootmeta=prefix+".meta"
   has_root_meta=subprocess.run(["git","cat-file","-e",PINS[owner]+":"+rootmeta],
    stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL).returncode==0
   for path in tree(PINS[owner],prefix)+([rootmeta] if has_root_meta else []):
    if not path.endswith(".meta"):continue
    data=file(PINS[owner],path)
    m=re.search(rb"(?m)^guid:\s*([a-fA-F0-9]{32})\s*$",data)
    require(m is not None,"MISSING_GUID_"+owner+":"+path)
    guid=m.group(1).decode().lower()
    old=owned.get(path)
    require(old is None or old==data,"SAME_PATH_DIFFERENT_META_"+path)
    owned[path]=data
    prior=by_guid.get(guid)
    require(prior is None or prior==path,"DUPLICATE_UNITY_GUID_"+guid+":"+path)
    by_guid[guid]=path
 portpaths=[p for p in owned if p.startswith(PREFIX.rstrip('/'))]
 require(len(portpaths)==9,"PORT_NEW_META_NOT_NINE")
 asmdefs=[p for p in tree(PINS["port"],PREFIX) if p.endswith(".asmdef")]
 require(len(asmdefs)==2,"PORT_ASMDEF_NOT_TWO")
 asmnames=[json.loads(file(PINS["port"],p)).get("name") for p in asmdefs]
 require(all(asmnames) and len(set(asmnames))==2,"DUPLICATE_ASMDEF_NAME")
 return {"count_unique_guid":len(by_guid),"new_port_meta":len(portpaths),
         "new_port_asmdefs":asmnames, "yaml_script_native":"NOT_RUN"}
def scratch(changed):
 newfiles={}
 for name,info in changed.items():
  for state,path in info:
   if name=="r5fix" and state!="M":continue
   if path.startswith("Assets/"):
    data=file(PINS[name],path)
    require(path not in newfiles,"COLLIDING_NEW_ASSET_"+path)
    newfiles[path]=data
 with tempfile.TemporaryDirectory(prefix="B01W25G0-") as root:
  root=Path(root);user=root/"FOREIGN"/"EMPTY";user.mkdir(parents=True)
  (root/"FOREIGN"/"bytes.bin").write_bytes(b"PRESERVE_FOREIGN")
  stage=root/"NEW_OWNED_ONLY";stage.mkdir()
  for path,data in newfiles.items():
   dest=stage/path;dest.parent.mkdir(parents=True,exist_ok=True)
   require(not dest.exists(),"OCCUPIED_PATH_"+path);dest.write_bytes(data)
  require(all((stage/p).read_bytes()==v for p,v in newfiles.items()),"BYTE_DRIFT")
  foreign_empty=stage/"FOREIGN_EMPTY";foreign_empty.mkdir()
  require(foreign_empty.exists(),"FOREIGN_EMPTY_NOT_CREATED")
  # Explicit rejection gate: no attempted rollback while unowned folder exists.
  cannot_remove=any(p not in {stage/q for q in newfiles}
    for p in stage.rglob("*") if p.is_file())
  require(not cannot_remove and foreign_empty.is_dir(),"ALIEN_SCRATCH_GATE")
  foreign_empty.rmdir()
  for path,data in newfiles.items():
   dest=stage/path
   require(not dest.is_symlink() and dest.read_bytes()==data,"MUTATED_BYTES_DELETE_DENIED")
   dest.unlink()
  for d in sorted((p for p in stage.rglob("*") if p.is_dir()),key=lambda x:len(x.parts),reverse=True):
   d.rmdir()
  stage.rmdir()
  require(user.is_dir() and (root/"FOREIGN"/"bytes.bin").read_bytes()==b"PRESERVE_FOREIGN",
   "FOREIGN_DATA_MUTATED")
 return {"new_or_replaced_unity_paths":len(newfiles),"byte_exact_scratch":"PASS",
   "foreign_empty_and_bytes":"PRESERVED","live_user_unity":"UNTOUCHED"}
def main():
 ap=argparse.ArgumentParser();ap.add_argument("--out",required=True);a=ap.parse_args()
 out=Path(a.out);out.mkdir(parents=True,exist_ok=True)
 changed=check_pins()
 body={"package":"B01-40-W25-INDEPENDENT-773-772-750-G0-001","pr_pins":PINS,
  "source_lineage":{k:v for k,v in changed.items()},"abi":verify_abi(),
  "guid":guid_closure(),"safe_physical_source_scratch":scratch(changed),
  "source_transport":"CHECKS_COMPLETE_NOT_PRODUCT_ACCEPTANCE",
  "native_g1":"NOT_RUN","live_game":"HOLD","g2":"HOLD","codex_ack":False,
  "merged":False,"existing_rig_private_510":"NOT_BOUND"}
 raw=(json.dumps(body,indent=2,sort_keys=True)+"\n").encode()
 (out/"intake.json").write_bytes(raw)
 print("B0140_W25_ORIGINAL_SHA_PROVENANCE_GUID_ABI_SCRATCH_PASS")
 print("B0140_W25_G0_INTAKE_SHA256="+sha(raw))
 print("B0140_W25_NATIVE_UNITY_G1_NOT_RUN_LIVE_SAVE_HOLD")
if __name__=="__main__":main()
