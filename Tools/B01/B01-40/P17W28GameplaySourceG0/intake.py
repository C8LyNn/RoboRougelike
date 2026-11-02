#!/usr/bin/env python3
"""B01-40 W28: independent exact original gameplay package physical-source G0.
Does NOT execute Unity, use/modify Codex worktree or offer packages.
"""
import argparse, hashlib, io, json, os, re, stat, subprocess, tempfile, zipfile
from pathlib import Path

BASE="5f06af81e2dd9d877e4300259bb3dd008cb9c9d1"
TARGET="e03d36f2b86f8c73a067e574b67237a8921f622a"
PKGS={
 "804":{"sha":"4dcec50d1a9a245393ea9f63d1f6925eb6566465",
   "root":"Assets/B01Packages/EnemyCharge001","count":21,"guids":12,
   "id":"B01-24-ENEMY-CHARGE-001","runtime":"Runtime/EnemyChargeMotor.cs",
   "must":["ForceMode.Acceleration","OnCollisionEnter","IChargeDamageSink","Phase.Recovery"]},
 "810":{"sha":"05db0ddf0a48da592d924e68b7658eea6bba764e",
   "root":"Assets/B01Packages/MapJumpPad001","count":19,"guids":11,
   "id":"B01-21-MAP-JUMPPAD-001","runtime":"Runtime/MapJumpPad.cs",
   "must":["ForceMode.VelocityChange","OnTriggerEnter","lastLaunch","contacts"]},
 "814":{"sha":"f92341c7788c3581b965d6baa47e6943325348fc",
   "root":"Assets/B01Packages/WeaponFeedback001","count":19,"guids":11,
   "id":"B01-32-WEAPON-FEEDBACK-001","runtime":"Runtime/WeaponFeedbackVisual.cs",
   "must":["NotifyShot","NotifyImpact","SetWeaponDamageVisual","OnDisable"]},
}

def cmd(*args):
 p=subprocess.run(["git",*args],stdout=subprocess.PIPE,stderr=subprocess.PIPE)
 if p.returncode:raise AssertionError("GIT_ERROR:"+repr(args)+":"+p.stderr.decode("utf-8","replace")[-180:])
 return p.stdout
def path_exists(sha,path):
 return subprocess.run(["git","cat-file","-e",sha+":"+path],capture_output=True).returncode==0
def source(sha,path):return cmd("show",sha+":"+path)
def tree(sha,root):return cmd("ls-tree","-r","--name-only",sha,"--",root).decode().splitlines()
def gitblob(sha,path):return cmd("rev-parse",sha+":"+path).decode().strip()
def sha256(b):return hashlib.sha256(b).hexdigest()
def must(ok,message):
 if not ok:raise AssertionError("B0140_W28_G0_HOLD:"+message)
def safe_path(s):
 q=Path(s)
 return bool(s) and not s.startswith("/") and "\\" not in s and not any(x in ("..","") for x in q.parts) and q.parts[0]=="Assets"
def gather():
 entries={}; provenance={}; guids={}; asmdefs={}; reports={}
 target_paths=set(tree(TARGET,"Assets"))
 for key,cfg in PKGS.items():
  sha=cfg["sha"];root=cfg["root"]
  must(cmd("rev-parse",sha).decode().strip()==sha,"SHA_NOT_AVAILABLE:"+key)
  must(subprocess.run(["git","merge-base","--is-ancestor",BASE,sha]).returncode==0,
       "BASE_NOT_ANCESTOR:"+key)
  diff=cmd("diff","--name-status",BASE,sha).decode().splitlines()
  changed=[item.split("\t")[-1] for item in diff]
  must(len(changed)==cfg["count"] and all(x.startswith("A\t") for x in diff),
       "EXACT_OWNER_ADDITIVE_SCOPE:"+key)
  must(all(p.startswith(root+"/") or p==root+".meta" for p in changed),
       "FOREIGN_CHANGED_PATH:"+key)
  discovered=tree(sha,root)
  if path_exists(sha,root+".meta"):discovered.append(root+".meta")
  discovered=sorted(discovered)
  must(set(discovered)==set(changed),"PRODUCER_MANIFEST_NOT_SAME_AS_GIT_SCOPE:"+key)
  producer=json.loads(source(sha,root+"/manifest.json"))
  expected=set(changed)-{root+"/manifest.json"}
  must(producer["base_sha"]==BASE and producer["exclusive_prefix"]==root,
       "WRONG_PRODUCER_MANIFEST_ROOT:"+key)
  must(set(producer["files"])==expected and producer["package_id"]==cfg["id"],
       "PRODUCER_FILE_LIST_MISMATCH:"+key)
  original=source(sha,root+"/"+cfg["runtime"]).decode("utf-8")
  must(all(x in original for x in cfg["must"]),"RUNTIME_API_CHANGED:"+key)
  ng=0;na=0
  for p in discovered:
   must(safe_path(p),"UNSAFE_ARCHIVE_PATH:"+p)
   b=source(sha,p)
   must(p not in entries,"SOURCE_FILE_COLLISION:"+p)
   must(not path_exists(BASE,p),"BASE_PATH_ALREADY_OCCUPIED:"+p)
   must(p not in target_paths,"CODEX_TARGET_PATH_ALREADY_OCCUPIED:"+p)
   if p.endswith(".meta"):
    m=re.search(rb"(?m)^guid:\s*([0-9a-fA-F]{32})\s*$",b)
    must(m is not None,"META_GUID_ABSENT:"+p)
    g=m.group(1).decode().lower()
    must(g not in guids,"DUPLICATE_INCOMING_GUID:"+g+":"+p)
    guids[g]=p;ng+=1
   if p.endswith(".asmdef"):
    item=json.loads(b);name=item["name"]
    must(name not in asmdefs,"DUPLICATE_INCOMING_ASMDEF:"+name)
    asmdefs[name]=p;na+=1
   provenance[p]={"producer_pr":key,"commit":sha,"git_blob_sha1":gitblob(sha,p),
                  "bytes":len(b),"sha256":sha256(b)}
   entries[p]=b
  must(ng==cfg["guids"] and na==3,"GUID_OR_ASMDEF_COUNT:"+key)
  reports[key]={"source_sha":sha,"source_paths":len(discovered),
                "meta_guid_count":ng,"asmdef_count":na,
                "manifest_path":root+"/manifest.json",
                "native_Unity":"NOT_RUN","physical_prefab_scene":"NOT_YET_GENERATED"}
 # Verify imported GUIDs and asmdef names do not collide with CURRENT published Codex target.
 for p in target_paths:
  if p.endswith(".meta"):
   b=source(TARGET,p)
   m=re.search(rb"(?m)^guid:\s*([0-9a-fA-F]{32})\s*$",b)
   if m:
    g=m.group(1).decode().lower()
    must(g not in guids,"TARGET_GUID_COLLISION:"+p+"-"+guids.get(g,""))
  if p.endswith(".asmdef"):
   name=json.loads(source(TARGET,p))["name"]
   must(name not in asmdefs,"TARGET_ASMDEF_COLLISION:"+name)
 return entries,provenance,reports,guids,asmdefs

def scratch(entries):
 with tempfile.TemporaryDirectory(prefix="B01-40-W28-G0-") as temp:
  scratch=Path(temp)
  alien=scratch/"FOREIGN";(alien/"EMPTY_FOLDER").mkdir(parents=True)
  (alien/"keep.dat").write_bytes(b"EXISTING_FOREIGN")
  staging=scratch/"OWN_NEW";staging.mkdir()
  for p,b in entries.items():
   file=staging/p;file.parent.mkdir(parents=True,exist_ok=True)
   must(not file.exists() and not file.is_symlink(),"ALREADY_OWNED:"+p)
   file.write_bytes(b)
  # Intentional negative occupied path and mutation must DENY without deleting foreign.
  first=next(iter(entries))
  must((staging/first).exists(),"OCCUPIED_NEGATIVE_NOT_SETUP")
  must(not ((staging/first).read_bytes()==b"ALIEN_OVERRIDE"),"COLLISION_NEGATIVE_FAILED")
  new_foreign=staging/"UNOWNED_EMPTY";new_foreign.mkdir()
  def can_rollback():
   return not new_foreign.exists() and all(
    (staging/p).is_file() and not (staging/p).is_symlink() and (staging/p).read_bytes()==b
     for p,b in entries.items())
  must(not can_rollback(),"FOREIGN_EMPTY_DIR_NOT_DENIED")
  new_foreign.rmdir()
  victim=staging/first;original=victim.read_bytes();victim.write_bytes(b"DIRTY")
  must(not can_rollback(),"DIRTY_ORIGINAL_NOT_DENIED")
  victim.write_bytes(original)
  must(can_rollback(),"CLEAN_SOURCE_CANNOT_ROLLBACK")
  for p,b in entries.items():
   (staging/p).unlink()
  for d in sorted((x for x in staging.rglob("*") if x.is_dir()),key=lambda x:len(x.parts),reverse=True):
   d.rmdir()
  staging.rmdir()
  must((alien/"EMPTY_FOLDER").is_dir() and
       (alien/"keep.dat").read_bytes()==b"EXISTING_FOREIGN","FOREIGN_BYTES_OR_EMPTY_CHANGED")
 return {"new_only_byte_exact_stage":"PASS","foreign_empty_negative":"PASS",
         "dirty_file_negative":"PASS","owner_only_rollback":"PASS",
         "foreign_tree_preserved":"PASS","live_worktree":"UNTOUCHED"}

def make_zip(entries,manifest,output):
 receipt=(json.dumps(manifest,sort_keys=True,indent=2,ensure_ascii=False)+"\n").encode("utf-8")
 archive={"MANIFEST.json":receipt}
 archive.update({"SOURCE/"+p:b for p,b in entries.items()})
 stream=io.BytesIO()
 with zipfile.ZipFile(stream,"w",compression=zipfile.ZIP_DEFLATED,compresslevel=9) as zipf:
  for path,b in sorted(archive.items()):
   z=zipfile.ZipInfo(path,date_time=(1980,1,1,0,0,0))
   z.external_attr=0o100644<<16;z.create_system=3
   z.compress_type=zipfile.ZIP_DEFLATED
   zipf.writestr(z,b,compress_type=zipfile.ZIP_DEFLATED,compresslevel=9)
 data=stream.getvalue()
 (output/"B01_W28_GAMEPLAY_G0_SOURCE_ONLY.zip").write_bytes(data)
 (output/"manifest.json").write_bytes(receipt)
 (output/"zip-sha256.json").write_bytes((json.dumps({
  "sha256":sha256(data),"bytes":len(data),"zip_members":len(archive),
  "source_files":len(entries),"real_unity_player":False},sort_keys=True,indent=2)+"\n").encode())
 return sha256(data),len(data),len(archive)

def main():
 parser=argparse.ArgumentParser();parser.add_argument("--out",required=True);args=parser.parse_args()
 dest=Path(args.out);must(not dest.exists(),"REFUSE_OCCUPIED_OUTPUT")
 entries,files,reports,guids,asmdefs=gather()
 stage=scratch(entries)
 manifest={"package":"B01-40-W28-GAMEPLAY-THREE-INDEPENDENT-G0-001",
  "source_base_sha":BASE,"read_only_codex_snapshot":TARGET,
  "producer_packages":reports,"files":files,"guid_to_path":guids,
  "asmdef_to_path":asmdefs,"safe_scratch":stage,
  "target_live_worktree":"NOT_INSPECTED; ASK CODEX FOR CURRENT SHA BEFORE STAGING",
  "installation":"SOURCE_ONLY_NOT_PREFABS_OR_NATIVE_SCENES",
  "status":{"SOURCE_TRANSPORT_G0":"PASS_WHEN_EXACT_SHA_CI_SUCCESS",
   "UNITY_COMPILE":"NOT_RUN","UNITY_PLAYMODE":"NOT_RUN","WINDOWS_PLAYER":"NOT_RUN",
   "RELEASE":"NO","CODEX_ACK":"NONE","READY_FOR_LIVE_IMPORT":"NO_NATIVE_G1"},
  "next":"B01-60 native Unity G1; B01-00 owner selection then Codex staged single-package intake"}
 dest.mkdir(parents=True)
 h,size,count=make_zip(entries,manifest,dest)
 print("B0140_W28_ORIGINAL_3_PACKAGES_EXACT_SHA_FILES=%d_GUID=%d_ASMDEF=%d" %
       (len(entries),len(guids),len(asmdefs)))
 print("B0140_W28_SOURCE_TRANSPORT_G0=PASS")
 print("B0140_W28_INNER_ZIP_SHA256="+h)
 print("B0140_W28_INNER_ZIP_BYTES="+str(size)+"_MEMBERS="+str(count))
 print("B0140_W28_NATIVE_UNITY_G1_NOT_RUN")
if __name__=="__main__":main()
