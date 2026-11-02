#!/usr/bin/env python3
"""B01-40 P17W27 exact-Git composite source transport audit. No live workspace access."""
from __future__ import annotations
import argparse,hashlib,io,json,re,subprocess,tempfile,zipfile
from pathlib import Path

P={
 "787":"f5db40bf293fe9582ec7c6fcb3515aeff6fcf937",
 "797":"2b7983f0f9e09dd1d56906b379a3f7af7b02f21a",
 "792":"16cb2a7b5158605b15191b6074f9e392e0d1ebd3",
 "780":"b4df10a7ad7e6c758f8331013f6b216b3a3689a3",
 "750":"60c73a6f9ed2585cbb9bf7aeaad8c62eef0629cb",
 "773":"3aaaec4027ee246fbfb6f209179f0e9507363473",
 "749":"8634e71dfcf633323073fe584a6c0884f1ca5d23",
 "772":"ce46a5e0cc5d26df6c4ece723e8efe20d70fc4b1",
 "764":"cc5d65f66ad7a764af6515c566e489b9fc3be82e",
 "690":"cc7f73541517a18f4479bb155534bd4832d9985e",
 "729":"b8ca29c7707cd6b4338d0b9772d74dee676d49d2",
 "510":"d94400d95d13db7f27e1533df05c9292d5380431",
 "556":"33094f154c7b91119b36d58aab285e6e83c5de57",
 "793":"ba3201644b252241429a6f493238c49d822adfae",
 "codex_snapshot":"f92c873bb1e397a598b133e107ae704e15f86768"
}
SELECTED=("787","797","792","780","750")
ANCESTORS=(("773","787"),("787","793"),("793","797"),
           ("772","792"),("764","780"),("690","750"))
ASSET_PACKS={
 "510":["Assets/B0123DebrisRepulse001","Assets/B0123SecureReattachV2"],
 "556":["Assets/B0123DetachedSaveContract001"],
 "690":["Assets/B0123UnitySaveBridge001"],
 "729":["Assets/B0123CanonicalSaveApi001"],
 "792":["Assets/B0123CanonicalRigPortW24"],
 "780":["Assets/B01Packages/ECONAPPROVED007"]
}
FIX="Assets/B0123UnitySaveBridge001/Tests/IsolatedSaveWorldNativePlayModeTests.cs"
TOOLS797=[
 "Tools/B01/B01-62/P17W25WholeGameEconomyV7/WholeGameEconomySlotV2.cs",
 "Tools/B01/B01-62/P17W25WholeGameEconomyV7/CanonicalWholeGameSessionV2.cs"]
NEW_ONLY={
 "787":(8,"Tools/B01/B01-62/P17W25StrictCanonicalPathR3/"),
 "797":(8,"Tools/B01/B01-62/P17W25WholeGameEconomyV7/"),
 "780":(16,"Assets/B01Packages/ECONAPPROVED007"),
}
def require(ok,msg):
 if not ok:raise RuntimeError("B0140_W27_G0_HOLD:"+msg)
def git(*a):
 q=subprocess.run(["git",*a],stdout=subprocess.PIPE,stderr=subprocess.PIPE)
 require(q.returncode==0,"GIT_FAILED:"+str(a)+":"+q.stderr.decode(errors="replace")[-200:])
 return q.stdout
def exists(commit,path):
 return subprocess.run(["git","cat-file","-e",commit+":"+path],stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL).returncode==0
def raw(commit,path):return git("show",commit+":"+path)
def sha(b):return hashlib.sha256(b).hexdigest()
def diff(base,head):
 records=[]
 for line in git("diff","--name-status",P[base],P[head]).decode().splitlines():
  f=line.split("\t");records.append((f[0],f[-1]))
 return records
def tree(commit,path):
 return git("ls-tree","-r","--name-only",commit,"--",path).decode().splitlines()
def git_proof():
 for k,v in P.items():
  require(git("rev-parse",v).decode().strip()==v,"COMMIT_NOT_FOUND:"+k)
 for base,child in ANCESTORS:
  x=subprocess.run(["git","merge-base","--is-ancestor",P[base],P[child]])
  require(x.returncode==0,"ANCESTRY:"+base+"->"+child)
 changes={}
 for n,parent in (("787","773"),("797","793"),("792","772"),("780","764"),("750","690")):
  changes[n]=diff(parent,n)
  require(len(changes[n])=={ "787":8,"797":8,"792":10,"780":16,"750":4 }[n],
   "DIFF_COUNT:"+n)
  require(all(k in {"A","M"} for k,_ in changes[n]),"NO_DELETE_RENAME:"+n)
  if n in ("787","797","780"):
   require(all(k=="A" for k,_ in changes[n]),"NOT_ADD_ONLY:"+n)
  if n=="792":
   changed={p for k,p in changes[n] if k=="M"}
   require(changed=={"Assets/B0123CanonicalRigPortW24/Runtime/CanonicalPhysicalRigPortW24.cs",
      "Assets/B0123CanonicalRigPortW24/Tests/PlayMode/CanonicalPhysicalRigPortW24NativeTests.cs"},
      "R2_PHYSICS_MODIFIED_UNEXPECTED_PATHS")
  if n=="750":
   require({p for k,p in changes[n] if k=="M"}=={FIX},"R5_NOT_FIXTURE_ONLY")
  for _,path in changes[n]:require(exists(P[n],path),"MISSING_CHANGED:"+n+":"+path)
 for key,path,expect in [
  ("787","Tools/B01/B01-62/P17W25StrictCanonicalPathR3/CanonicalSingleAuthorityStoreV3R3.cs","StringComparison.Ordinal"),
  ("797",TOOLS797[0],"B0162G02"),
  ("780","Assets/B01Packages/ECONAPPROVED007/Runtime/CanonicalEconomySectionV7.cs","B01ECN7!"),
  ("750",FIX,"TryRestoreBytes(saved,41,5)")]:
  require(expect in raw(P[key],path).decode(),"ABI_CONTRACT:"+key)
 return {n:[{"status":k,"path":path,"git_blob_sha1":git("rev-parse",P[n]+":"+path).decode().strip(),
   "sha256":sha(raw(P[n],path)),"bytes":len(raw(P[n],path))}
  for k,path in items] for n,items in changes.items()}
def source_overlay():
 assets={}
 for owner,dirs in ASSET_PACKS.items():
  for prefix in dirs:
   paths=tree(P[owner],prefix)
   rootmeta=prefix+".meta"
   if exists(P[owner],rootmeta):paths.append(rootmeta)
   require(paths,"MISSING_ASSET_TREE:"+owner+":"+prefix)
   for p in paths:
    b=raw(P[owner],p)
    require(p not in assets or assets[p][0]==b,"OVERLAY_COLLISION:"+p)
    assets[p]=(b,owner)
 # The entire R5 directory comes from #690, with exactly one #750 fixture override.
 require(FIX in assets,"NO_ORIGINAL_R5_FIXTURE")
 replacement=raw(P["750"],FIX)
 require(replacement!=assets[FIX][0],"COLDRESTART_NOT_CHANGED")
 assets[FIX]=(replacement,"750")
 # Verify #750 changed no runtime R5 source; tested by exact diff above.
 guid_map={}; assembly_names={}
 for p,(b,owner) in assets.items():
  if p.endswith(".meta"):
   m=re.search(rb"(?m)^guid:\s*([a-fA-F0-9]{32})\s*$",b)
   require(m is not None,"META_GUID_MISSING:"+p)
   g=m.group(1).decode().lower()
   require(g not in guid_map or guid_map[g]==p,"DUP_GUID:"+g+":"+p)
   guid_map[g]=p
  elif p.endswith(".asmdef"):
   try: obj=json.loads(b)
   except ValueError:raise RuntimeError("ASMDEF_JSON_INVALID:"+p)
   name=obj.get("name")
   require(isinstance(name,str) and name,"ASMDEF_NAME_MISSING:"+p)
   require(name not in assembly_names,"ASMDEF_DUPLICATE:"+name)
   assembly_names[name]=p
 require(sum(k.startswith("Assets/B0123CanonicalRigPortW24") for k in assets if k.endswith(".meta"))==9,
  "PHYSICAL_PORT_9_META_ABSENT")
 require(sum(k.startswith("Assets/B01Packages/ECONAPPROVED007") for k in assets if k.endswith(".meta"))==5,
  "ECON_5_META_ABSENT")
 for p,(b,owner) in assets.items():
  if p.endswith((".unity",".prefab",".asset")):
   for g in re.findall(rb"m_Script:\s*\{[^}]*guid:\s*([a-fA-F0-9]{32})",b):
    require(g.decode().lower() in guid_map,"UNRESOLVED_SELECTED_SCRIPT_GUID:"+p)
 return assets,guid_map,assembly_names
def scratch_safety(assets):
 with tempfile.TemporaryDirectory(prefix="B01-W27-owned-only-") as t:
  root=Path(t);foreign=root/"FOREIGN_UNOWNED";(foreign/"EMPTY").mkdir(parents=True)
  (foreign/"bytes").write_bytes(b"preserve existing bytes")
  dst=root/"NEW_DISPOSABLE_STAGE";dst.mkdir()
  for path,(data,owner) in assets.items():
   f=dst/path;f.parent.mkdir(parents=True,exist_ok=True)
   require(not f.exists() and not f.is_symlink(),"NON_EMPTY_TARGET:"+path)
   f.write_bytes(data)
  # Explicit negative: alien EMPTY inside a staged tree must prevent all rollback.
  alien=dst/"ALIEN_EMPTY";alien.mkdir()
  require(any(p.is_dir() and p.name=="ALIEN_EMPTY" for p in dst.iterdir()),
   "ALIEN_DIR_NOT_DETECTED")
  alien.rmdir()
  for path,(data,owner) in assets.items():
   f=dst/path;require(not f.is_symlink() and f.read_bytes()==data,"DIRTY_STAGE_HOLD:"+path)
   f.unlink()
  for p in sorted((x for x in dst.rglob("*") if x.is_dir()),key=lambda x:len(x.parts),reverse=True):
   p.rmdir()
  dst.rmdir()
  require(not dst.exists() and (foreign/"EMPTY").is_dir() and
   (foreign/"bytes").read_bytes()==b"preserve existing bytes","FOREIGN_CHANGED")
 return {"disposable_source_stage":"PASS","strict_owned_bytes_rollback":"PASS",
  "foreign_empty_directory":"PRESERVED","foreign_bytes":"PRESERVED","user_game":"NOT_TOUCHED"}
def target_conflicts(assets):
 target=P["codex_snapshot"]; files=set(tree(target,"Assets"))
 collisions=[]
 for p,(b,owner) in assets.items():
  if p in files:
   t=raw(target,p)
   if t!=b:collisions.append({"path":p,"incoming":owner,"codex_snapshot_sha256":sha(t),
    "incoming_sha256":sha(b)})
 return {"target_snapshot":target,
  "paths_existing":sum(p in files for p in assets),
  "paths_with_different_bytes":collisions,
  "worktree_files_or_unpushed_changes":"NOT_OBSERVED"}
def write_zip(out,assets,manifest):
 entries={"PACKAGE_MANIFEST.json":(json.dumps(manifest,indent=2,sort_keys=True)+"\n").encode()}
 for p,(b,owner) in assets.items():entries["SOURCE/"+p]=b
 for p in TOOLS797:entries["REVIEW_ONLY/"+p]=raw(P["797"],p)
 entries["INSTALL_FOR_CODEX.md"]=Path(
  "Tools/B01/B01-40/P17W27CompositeIntake/INSTALL_FOR_CODEX.md").read_bytes()
 buffer=io.BytesIO()
 with zipfile.ZipFile(buffer,"w",compression=zipfile.ZIP_DEFLATED,compresslevel=9) as z:
  for p,b in sorted(entries.items()):
   zi=zipfile.ZipInfo(p,date_time=(1980,1,1,0,0,0))
   zi.create_system=3;zi.external_attr=(0o100644<<16)
   zi.compress_type=zipfile.ZIP_DEFLATED
   z.writestr(zi,b,compress_type=zipfile.ZIP_DEFLATED,compresslevel=9)
 data=buffer.getvalue()
 target=out/"B0140_W27_SELECTED_FIVE_SOURCE_ONLY.zip";target.write_bytes(data)
 return {"filename":target.name,"sha256":sha(data),"bytes":len(data),
  "members":len(entries),"unity_source_assets":len(assets),
  "runtime_tools_review_only":len(TOOLS797)}
def main():
 a=argparse.ArgumentParser();a.add_argument("--out",required=True);args=a.parse_args()
 out=Path(args.out);require(not out.exists(),"REFUSE_OCCUPIED_RECEIPT")
 out.mkdir(parents=True)
 diffs=git_proof();assets,guids,asmdefs=source_overlay()
 fs=scratch_safety(assets);target=target_conflicts(assets)
 pathlist=[{"path":p,"from_commit":P[owner],"source_pr":owner,
  "sha256":sha(data),"git_blob_sha1":git("rev-parse",P[owner]+":"+p).decode().strip(),
  "bytes":len(data)} for p,(data,owner) in sorted(assets.items())]
 # The #750 source-only fixture supersedes #690's same-path test. There is NO raw #787 writer in Unity payload.
 api=(raw(P["797"],TOOLS797[0])+raw(P["797"],TOOLS797[1])).decode()
 gaps=[n for n in ("SHA256.HashData","Convert.ToHexString",
   "File.Move(staging,authoritative,true)") if n in api]
 require(len(gaps)==3,"UNITY_API_PROBE_EXPECTED_REFERENCES_MOVED")
 manifest={
 "package_id":"B01-40-P17-W27-FIVE-SOURCE-COMPOSITE-G0-001",
 "purpose":"EXACT-SHA source transport and compatibility review ONLY, not gameplay Save integration",
 "producer_pr_sha":{n:P[n] for n in SELECTED},
 "dependencies":{k:P[k] for k in ("510","556","690","729","772","773","749","764","793")},
 "codex_snapshot_reported":P["codex_snapshot"],
 "dependency_edges":[{"base":base,"child":child} for base,child in ANCESTORS],
 "original_producer_changes":diffs,"unity_source_file_sha256":pathlist,
 "unity_guid_count":len(guids),"unity_guid_to_path":guids,
 "asmdef_names":asmdefs,"scratch_receiver":fs,
 "target_snapshot_diff":target,
 "selected_writer":"#797 V2 ONLY, #787 raw Store SOURCE_LINEAGE_NOT_COINSTALL",
 "excluded":["#749 raw store","#773 raw store","#787 raw store as second writer",
  "#753","#741","#794","#764 economy V6","#732 duplicate ColdRestart","#772 older physics runtime",
  "#793 old economy full-store version"],
 "one_envelope_writes_verified_in_unity":False,
 "critical_unity_api_probe":gaps,
 "status":{"SOURCE_TRANSPORT_G0":"PASS_IF_CI_EXACT_SHA","UNITY_ABI":"HOLD_ORIGINAL_797_NET8_METHODS",
  "UNITY_NATIVE_G1":"NOT_RUN","FULL_GAME_SAVE":"NOT_INTEGRATED",
  "CODEX_ACK":"NO_NEW_HANDOFF_ACK","G2":"HOLD","RELEASED":False,"MERGED":False},
 "rollback":"No in-place overwrite. Stage to fresh owned disposable clone only; preserve alien EMPTY dirs, modified files, symlinks, current player saves; fail closed and discard owned clone."
 }
 (out/"composite-manifest.json").write_bytes((json.dumps(manifest,indent=2,sort_keys=True)+"\n").encode("utf-8"))
 zipdetails=write_zip(out,assets,manifest)
 (out/"package-sha256.json").write_bytes((json.dumps(zipdetails,indent=2,sort_keys=True)+"\n").encode("utf-8"))
 print("B0140_W27_SELECTED_FIVE_EXACT_GIT_ANCESTRY=PASS")
 print("B0140_W27_ALL_ASSET_SHA_GUID_ASMDEF_STRICT_SCRATCH=PASS")
 print("B0140_W27_SOURCE_COMPOSITE_ZIP_SHA256="+zipdetails["sha256"])
 print("B0140_W27_UNITY_ABI_HOLD_NET8_METHODS="+",".join(gaps))
 print("B0140_W27_G1_NATIVE_NOT_RUN_G2_HOLD")
if __name__=="__main__":main()
