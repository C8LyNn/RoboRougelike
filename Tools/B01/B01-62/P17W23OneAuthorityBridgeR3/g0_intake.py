#!/usr/bin/env python3
"""W23 independent SOURCE G0: execute ORIGINAL R5 #690 and V3 #729 source packagers.
Only UUID-owned disposable temp; no project installation and no main edits.
"""
import hashlib,importlib.util,io,json,re,shutil,subprocess,tempfile,zipfile
from pathlib import Path
COMMIT={
 "host":{"sha":"cc7f73541517a18f4479bb155534bd4832d9985e",
         "pack":"Tools/B01/B01-23/P17W15UnitySaveBridge001/package.py",
         "guids":11,"zip_sha":"a37e942d6a0cc5236570edaf7cba49384a84d0553f88fb29988a855d692a1496"},
 "api":{"sha":"b8ca29c7707cd6b4338d0b9772d74dee676d49d2",
        "pack":"Tools/B01/B01-23/P17W20CanonicalSaveApi001/package.py",
        "guids":5}
}
ROOTS=[
 ("510","d94400d95d13db7f27e1533df05c9292d5380431","Assets/B0123SecureReattachV2",14),
 ("W06","d94400d95d13db7f27e1533df05c9292d5380431","Assets/B0123DebrisRepulse001",5),
 ("556","33094f154c7b91119b36d58aab285e6e83c5de57","Assets/B0123DetachedSaveContract001",4)]
checks=0
def require(yes,name):
 global checks
 if not yes:raise AssertionError(name)
 checks+=1;print("PASS",name)
def git(root,*args):
 return subprocess.check_output(["git","-C",str(root),*args],stderr=subprocess.PIPE)
def read(root,commit,path):
 return git(root,"show",commit+":"+path)
def meta(raw):
 tags=re.findall(r"(?m)^guid:\s*([a-f0-9]{32})\s*$",raw.decode("utf8"))
 if len(tags)!=1:raise AssertionError("invalid owner Unity guid")
 return tags[0]
def module(path,key):
 spec=importlib.util.spec_from_file_location("original_g0_owner_"+key,path)
 if spec is None or spec.loader is None:raise AssertionError("original producer unavailable")
 x=importlib.util.module_from_spec(spec);spec.loader.exec_module(x);return x
def refused(fn,label):
 ok=False
 try:fn()
 except (ValueError,OSError,RuntimeError,AssertionError,zipfile.BadZipFile):
  ok=True
 require(ok,label)
def main(repo,out):
 repo=Path(repo).resolve();out=Path(out)
 if out.exists() or not out.parent.exists():raise ValueError("evidence receiver exists")
 packages={};owned=[];by_guid={};by_path={}
 with tempfile.TemporaryDirectory(prefix="b0162-w23-selected-source-g0-") as scratch:
  t=Path(scratch)
  try:
   for name,pin in COMMIT.items():
    tmp=t/("original-"+name)
    git(repo,"worktree","add","--detach",str(tmp),pin["sha"])
    owned.append(tmp)
    require(git(tmp,"rev-parse","HEAD").decode().strip()==pin["sha"],
            name+" exact original producer Git SHA")
    pack=module(tmp/pin["pack"],name)
    raw=pack.build()
    pack.verify(raw)
    digest=hashlib.sha256(raw).hexdigest()
    if "zip_sha" in pin:
     require(digest==pin["zip_sha"],name+" original physical SHA256")
    with zipfile.ZipFile(io.BytesIO(raw)) as z:
     names=z.namelist()
     require(len(names)==len(set(names)) and names.count("PACKAGE_MANIFEST.json")==1,
             name+" original ZIP unique paths and owner manifest")
     assert all(not n.startswith("/") and "\\" not in n and ".." not in Path(n).parts for n in names)
     m=json.loads(z.read("PACKAGE_MANIFEST.json"))
     require(m.get("sha",m.get("own_head_sha"))==pin["sha"],
             name+" original author manifest exact SHA")
     if name=="api":
      require(m.get("version")=="3.0.0" and m.get("guids")==5 and
              m.get("integrated_b0162_712")=="NO_SEPARATE_OWNER_ADAPTER_REQUIRED",
              "selected ORIGINAL API V3 one-owner manifest, not ALT_HOLD")
     tagged={n:meta(z.read(n)) for n in names if n.endswith(".meta")}
     require(len(tagged)==pin["guids"] and len(set(tagged.values()))==len(tagged),
             name+" owner original Unity meta GUID")
     for path,g in tagged.items():
      require(g not in by_guid,name+" cross original Unity GUID unique "+path)
      by_guid[g]=name+":"+path
     for path in names:
      if path=="PACKAGE_MANIFEST.json":continue
      require(path not in by_path,name+" original Git source path collision-free "+path)
      by_path[path]=z.read(path)
     packages[name]={"original_sha":pin["sha"],"original_zip_size":len(raw),
                     "original_zip_sha256":digest,"original_members":len(names),
                     "original_unity_guids":len(tagged),"original_manifest":m}
     # Invoke actual ORIGINAL producer safe scratch stage and rollback.
     target=t/("scratch-receiver-"+name)
     pack.stage(raw,target)
     require(target.is_dir(),name+" original author stage scratch")
     refused(lambda:pack.stage(raw,target),name+" refuses occupied target")
     foreign=t/("foreign-EMPTY-"+name);foreign.mkdir()
     refused(lambda:pack.stage(raw,foreign),name+" refuses alien EMPTY receiver")
     require(foreign.is_dir() and not any(foreign.iterdir()),
             name+" preserves alien EMPTY receiver")
     pack.rollback(raw,target)
     require(not any(p.is_file() or p.is_symlink() for p in target.rglob("*"))
             if target.exists() else True,
             name+" original clean rollback no files or symlink")
     # Never delete FOREIGN dirs. Recreate ONLY our exact UUID scratch.
     if target.exists():shutil.rmtree(target)
     pack.stage(raw,target)
     edited=next(target/n for n in names if n.endswith(".cs"))
     before=edited.read_bytes()
     edited.write_bytes(before+b"\n// B0162 test-owned DIRTY bytes\n")
     refused(lambda:pack.rollback(raw,target),
             name+" original rollback refuses dirty source")
     require(edited.read_bytes()!=before,name+" dirty foreign byte preservation")
     edited.write_bytes(before)
     alien=target/"B0162_TEST_FOREIGN_EMPTY"
     alien.mkdir()
     if name=="host":
      refused(lambda:pack.rollback(raw,target),
              name+" original rollback refuses nested alien EMPTY")
     else:
      pack.rollback(raw,target)
     require(alien.is_dir(),name+" original rollback preserves nested foreign EMPTY")
   for name,sha,prefix,count in ROOTS:
    paths=git(repo,"ls-tree","-r","--name-only",sha,"--",prefix).decode().splitlines()
    paths=[x for x in paths if x.startswith(prefix+"/")]
    if git(repo,"cat-file","-e",sha+":"+prefix+".meta")==b"":paths.append(prefix+".meta")
    ids={x:meta(read(repo,sha,x)) for x in paths if x.endswith(".meta")}
    require(len(ids)==count,name+" frozen unchanged original GUID count")
    for path,g in ids.items():
     require(g not in by_guid,name+" frozen GUID does not collide "+path)
     by_guid[g]=name+":"+path
    for path in paths:
     require(path not in by_path,name+" frozen original path not duplicated "+path)
     by_path[path]=read(repo,sha,path)
   require(len(by_guid)==39,"combined original R5+V3+frozen source GUID total 39/39")
   # One protected scratch receiver reconstructed from actual author Git bytes.
   joined=t/"combined-src-only";joined.mkdir()
   for path,raw in by_path.items():
    p=joined/path
    p.parent.mkdir(parents=True,exist_ok=True)
    with p.open("xb") as f:f.write(raw)
   require(all((joined/p).read_bytes()==raw for p,raw in by_path.items()),
           "combined receiver exact original Git member bytes")
   require(len([x for x in joined.rglob("*.meta")])==39,
           "combined physical disposable Unity roots 39 meta")
   require("PACKAGE_MANIFEST.json" not in by_path,
           "both ORIGINAL author root manifests MUST NOT be overlaid")
   # Retain receipts as independent G0 metadata, not installed root files.
   require(packages["host"]["original_manifest"]!=packages["api"]["original_manifest"],
           "distinct source owner root manifest collision detected")
   payload={"package":"B01-62-W23-ONE-AUTHORITY-003-G0",
            "selected":packages,"cross_guids":len(by_guid),
            "source_files":len(by_path),
            "root_manifest_collision":"MUST_KEEP_R5_AND_V3_SOURCE_MANIFESTS_SEPARATE",
            "safety":"DISPOSABLE_ONLY",
            "native_unity":"NOT_RUN","integrated":False,"codex_ack":False}
   data=(json.dumps(payload,sort_keys=True,indent=2)+"\n").encode()
   out.write_bytes(data)
   print("B0162_W23_SELECTED_R5_V3_COMBINED_G0_CHECKS_PASS="+str(checks))
   print("B0162_W23_SELECTED_R5_V3_GUIDS=39/39")
   print("B0162_W23_SELECTED_API_V3_ORIGINAL_INNER_ZIP_SHA256="+packages["api"]["original_zip_sha256"])
   print("B0162_W23_SOURCE_G0=ACCEPTED_TEST_SCRATCH_ONLY")
   print("B0162_W23_COMBINED_G0_RECEIPT_SHA256="+hashlib.sha256(data).hexdigest())
  finally:
   for path in reversed(owned):
    git(repo,"worktree","remove","--force",str(path))
if __name__=="__main__":
 import argparse
 a=argparse.ArgumentParser()
 a.add_argument("--repo",required=True)
 a.add_argument("--receipt",required=True)
 q=a.parse_args();main(q.repo,q.receipt)
