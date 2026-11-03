#!/usr/bin/env python3
"""Exact git-object-only Unity source intake: no Codex worktree writes, no Save install, no merge."""
from __future__ import annotations
import argparse, hashlib, io, json, os, pathlib, re, shutil, subprocess, sys, tempfile, zipfile

REPO=pathlib.Path(__file__).resolve().parents[4]
SHA={
 "frozen_510":"d94400d95d13db7f27e1533df05c9292d5380431",
 "frozen_556":"33094f154c7b91119b36d58aab285e6e83c5de57",
 "canonical_729":"b8ca29c7707cd6b4338d0b9772d74dee676d49d2",
 "selected_787":"f5db40bf293fe9582ec7c6fcb3515aeff6fcf937",
 "whole_game_826":"99fb08619c78b3834bb4c0450499babf991f218a",
 "physical_792":"16cb2a7b5158605b15191b6074f9e392e0d1ebd3",
 "economy_780":"b4df10a7ad7e6c758f8331013f6b216b3a3689a3",
 "r5_fixture_750":"60c73a6f9ed2585cbb9bf7aeaad8c62eef0629cb",
}
ROOTS=[
 ("frozen_510","Assets/B0123DebrisRepulse001"),
 ("frozen_510","Assets/B0123SecureReattachV2"),
 ("frozen_556","Assets/B0123DetachedSaveContract001"),
 ("canonical_729","Assets/B0123CanonicalSaveApi001"),
 ("r5_fixture_750","Assets/B0123UnitySaveBridge001"),
 ("physical_792","Assets/B0123CanonicalRigPortW24"),
 ("economy_780","Assets/B01Packages/ECONAPPROVED007"),
]
SOURCES_ONLY=[
 ("whole_game_826","Tools/B01/B01-62/P17W28UnityCompatibleWholeGameV3/WholeGameEconomySlotV3.cs"),
 ("whole_game_826","Tools/B01/B01-62/P17W28UnityCompatibleWholeGameV3/CanonicalWholeGameSessionV3.cs"),
]
REFERENCE_ONLY=[
 ("selected_787","Tools/B01/B01-62/P17W25StrictCanonicalPathR3/CanonicalSingleAuthorityStoreV3R3.cs"),
 ("selected_787","Tools/B01/B01-62/P17W25StrictCanonicalPathR3/CanonicalSlotSessionV3R3.cs"),
]
PINS={
 "Assets/B0123CanonicalSaveApi001/Core/CanonicalWorldGateway.cs":"21733278b6ab0669fccb6d085e0b6cf242792de2",
 "Assets/B0123CanonicalSaveApi001/Core/CanonicalWorldSlotLease.cs":"a9331dd620336d8c42165d134dc8a5e05ed51873",
 "Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs":"b52e511c370f5fca56c74029050844dc8cbb7f9a",
 "Assets/B0123SecureReattachV2/Runtime/AtomicDockLedger.cs":"b6a4d04b0c6407c8c84451b4d599d96fe0e4d1a9",
 "Assets/B01Packages/ECONAPPROVED007/Runtime/ApprovedEconomyPolicyV7.cs":"53ebce6c07388dc9c8a8ed79b6de4d3ccbee4773",
 "Assets/B01Packages/ECONAPPROVED007/Runtime/CanonicalEconomySectionV7.cs":"d8b71de91db36295b6220e9c4baad71eb54455f6",
 "Tools/B01/B01-62/P17W25StrictCanonicalPathR3/CanonicalSingleAuthorityStoreV3R3.cs":"bc34c2df4375ac7abdd638e5db7f7a238e8fa2fd",
 "Tools/B01/B01-62/P17W25StrictCanonicalPathR3/CanonicalSlotSessionV3R3.cs":"0fa335a67346ae600df163ec69c90e7550945ba1",
 "Tools/B01/B01-62/P17W28UnityCompatibleWholeGameV3/WholeGameEconomySlotV3.cs":"247ab912b73a0e6f17e86aa1a3c74343cb4d9af6",
 "Tools/B01/B01-62/P17W28UnityCompatibleWholeGameV3/CanonicalWholeGameSessionV3.cs":"927b286e76a358716d69e76da5d8351c2564d613",
}
def git(*args, binary=False):
 proc=subprocess.run(["git",*args],cwd=REPO,capture_output=True,check=True)
 return proc.stdout if binary else proc.stdout.decode("utf-8").strip()
def hashblob(raw:bytes):
 return hashlib.sha1(b"blob "+str(len(raw)).encode()+b"\0"+raw).hexdigest()
def file_object(ref:str,path:str):
 return git("show",f"{SHA[ref]}:{path}",binary=True)
def list_tree(ref:str,prefix:str):
 raw=git("ls-tree","-r","--name-only",SHA[ref],"--",prefix)
 return [x for x in raw.splitlines() if x and (x.startswith(prefix+"/") or x==prefix)]
def assemble():
 data={}; origin={}; reference_only={}
 def add(ref,path,mode="unity"):
  raw=file_object(ref,path)
  exact=git("rev-parse",f"{SHA[ref]}:{path}")
  if hashblob(raw)!=exact:raise AssertionError("GIT_BLOB_HASH_MISMATCH "+path)
  if path in PINS and exact!=PINS[path]:raise AssertionError("EXACT_PIN_MISMATCH "+path+" "+exact)
  if mode=="reference":
   reference_only[path]={"producer":ref,"blob":exact,"sha256":hashlib.sha256(raw).hexdigest()}
   return
  target=path if mode=="unity" else "SourceOnly/"+path
  if target in data:
   if data[target]!=raw:raise AssertionError("DUPLICATE_PATH_CONFLICT "+target)
   raise AssertionError("DUPLICATE_IMPORT_PATH "+target)
  data[target]=raw;origin[target]={"producer":ref,"sha":SHA[ref],
    "git_blob":exact,"sha256":hashlib.sha256(raw).hexdigest(),"bytes":len(raw),"mode":mode}
 for ref,root in ROOTS:
  listing=list_tree(ref,root)
  if not listing:raise AssertionError("MISSING_ROOT "+root)
  for path in listing:add(ref,path)
  add(ref,root+".meta")
 # This parent is SHARED in existing Codex workspaces. The manifest MUST flag
 # matching-existing-or-manual, never overwrite it. Fresh source clone includes it.
 add("economy_780","Assets/B01Packages.meta")
 for ref,path in SOURCES_ONLY:add(ref,path,mode="source_only")
 for ref,path in REFERENCE_ONLY:add(ref,path,mode="reference")
 if set(PINS)-{x.removeprefix("SourceOnly/") for x in data}-set(reference_only):
  raise AssertionError("SOME_HARD_SOURCE_BLOB_PINS_MISSING")
 # Raw #787 is NOT an active import. One #826 store only.
 if any("CanonicalSingleAuthorityStoreV3R3.cs" in x for x in data):
  raise AssertionError("DUPLICATE_RAW_STORE_SELECTED")
 # The whole economy and world Store has NO runtime Unity asmdef/binder in
 # selected source. It is intentionally an unintegrated Tools-only source.
 if any(x.startswith("Assets/") and "WholeGameEconomy" in x for x in data):
  raise AssertionError("UNAUTHORIZED_IMPLICIT_UNITY_WRITER")
 return data,origin,reference_only
def meta_asmdef(data):
 guids={}; assembly_names=set(); assemblies=[]
 for path,raw in data.items():
  if path.endswith(".meta"):
   m=re.search(rb"(?m)^guid:\s*([0-9a-f]{32})\s*$",raw)
   if not m:raise AssertionError("MISSING_GUID "+path)
   g=m.group(1).decode()
   if g in guids:raise AssertionError("DUPLICATE_UNITY_GUID "+g+" "+path+" "+guids[g])
   guids[g]=path
   if path[:-5] not in data and not any(x.startswith(path[:-5]+"/") for x in data):
    raise AssertionError("ORPHAN_META "+path)
 for path in data:
  if path.startswith("Assets/") and not path.endswith(".meta"):
   is_dir=False
   if path+".meta" not in data:raise AssertionError("MISSING_ASSET_META "+path)
  if path.endswith(".asmdef"):
   j=json.loads(data[path].decode())
   name=j["name"]
   if name in assembly_names:raise AssertionError("DUPLICATE_ASMDEF_NAME "+name)
   assembly_names.add(name);assemblies.append((name,j.get("references",[])))
 missing={}
 for name,refs in assemblies:
  # Unity Test Framework built-ins/external defined separately; no duplicate auto import.
  missing[name]=[x for x in refs if not x.startswith("GUID:") and
      x not in assembly_names and not x.startswith("Unity.") and x not in ["Assembly-CSharp","nunit.framework"]]
 return guids,assembly_names,missing
def payload_zip(data,manifest):
 bio=io.BytesIO()
 with zipfile.ZipFile(bio,"w",compression=zipfile.ZIP_STORED) as z:
  items={f"SELECTED_SOURCE/{p}":v for p,v in data.items()}
  items["B01-61/W29/COMPOSITE_MANIFEST.json"]=(json.dumps(manifest,sort_keys=True,indent=2)+"\n").encode()
  for name,raw in sorted(items.items()):
   info=zipfile.ZipInfo(name,(1980,1,1,0,0,0))
   info.create_system=3
   info.external_attr=(0o100644<<16)
   info.compress_type=zipfile.ZIP_STORED
   z.writestr(info,raw)
 return bio.getvalue()
def verified_files(root,data):
 for path,raw in data.items():
  file=root/path
  if not file.is_file() or file.is_symlink() or file.read_bytes()!=raw:
   raise AssertionError("STAGING_CONTENT_MISMATCH "+path)
def deploy_new_only(root,data):
 if root.exists() or root.is_symlink():
  raise FileExistsError("REFUSE_OCCUPIED_EVEN_IF_EMPTY_OR_IDENTICAL")
 root.mkdir(mode=0o700)
 written=[]
 try:
  for path,raw in sorted(data.items()):
   dest=root/path;dest.parent.mkdir(parents=True,exist_ok=True)
   if dest.exists() or dest.is_symlink():raise FileExistsError(path)
   with dest.open("xb") as f:f.write(raw)
   written.append(path)
  verified_files(root,data)
  return written
 except:
  # Remove ONLY files that are still byte-identical to our own, never a foreign
  # user-edited file and never arbitrary existing empty directories.
  for path in reversed(written):
   dest=root/path
   if dest.exists() and not dest.is_symlink() and dest.read_bytes()==data[path]:
    dest.unlink()
  raise
def protected_rollback(root,owned,original):
 # Preflight ALL owned paths before deleting a single file.
 for path in owned:
  f=root/path
  if not f.is_file() or f.is_symlink() or f.read_bytes()!=original[path]:
   raise PermissionError("ROLLBACK_DIRTY_OR_FOREIGN_HOLD "+path)
 for path in owned:(root/path).unlink()
 # Only the caller's newly created root may be deleted after verified ownership.
def physical_scratch_tests(data):
 checks=[]
 with tempfile.TemporaryDirectory(prefix="b0161-w27-fresh-") as td:
  top=pathlib.Path(td)
  first=top/"new"
  written=deploy_new_only(first,data);checks.append("FRESH_EXACT_GIT_BYTES_INSTALL_PASS")
  try:deploy_new_only(first,data)
  except FileExistsError:checks.append("NONEMPTY_RECEIVER_REJECT_PASS")
  else:raise AssertionError("SHOULD_REFUSE_OCCUPIED")
  foreign=first/"FOREIGN_EMPTY_NESTED"
  foreign.mkdir()
  protected_rollback(first,written,data)
  if not foreign.is_dir():raise AssertionError("FOREIGN_EMPTY_DIRECTORY_DELETED")
  checks.append("ROLLBACK_PRESERVES_FOREIGN_EMPTY_PASS")
  guard=top/"foreign_empty";guard.mkdir()
  try:deploy_new_only(guard,data)
  except FileExistsError:checks.append("OCCUPIED_FOREIGN_EMPTY_ROOT_REJECT_PASS")
  else:raise AssertionError("FOREIGN_EMPTY_OVERWRITTEN")
  other=top/"dirty"
  owned=deploy_new_only(other,data)
  altered=other/owned[0];altered.write_bytes(b"EXTERNAL_MODIFICATION")
  try:protected_rollback(other,owned,data)
  except PermissionError:
   if altered.read_bytes()!=b"EXTERNAL_MODIFICATION":raise AssertionError("DIRTY_DELETED")
   checks.append("DIRTY_ROLLBACK_ATOMIC_REFUSE_PASS")
  else:raise AssertionError("DIRTY_OVERWRITTEN")
  if hasattr(os,"symlink"):
   link=top/"symlink_receiver"
   try:
    link.symlink_to(other,target_is_directory=True)
   except (OSError,NotImplementedError):
    checks.append("SYMLINK_CREATE_NOT_PERMITTED_NOT_RUN")
   else:
    try:deploy_new_only(link,data)
    except FileExistsError:checks.append("SYMLINK_RECEIVER_DENIED_PASS")
    else:raise AssertionError("SYMLINK_RECEIVER_TRAVERSED")
 return checks
def run(out_dir):
 subprocess.run(["git","merge-base","--is-ancestor",SHA["whole_game_826"],"HEAD"],cwd=REPO,check=True)
 # verify selected original sources WITHOUT modifying currently checked-out producers
 data,origin,refs=assemble()
 guid,assemblies,missing=meta_asmdef(data)
 manifest={
  "schema":"B01-61-W29-COMPOSITE-EXACT-GIT-SOURCE-G0-001",
  "selected_prs":{"787":SHA["selected_787"],"826":SHA["whole_game_826"],"792":SHA["physical_792"],"780":SHA["economy_780"],"750":SHA["r5_fixture_750"]},
  "frozen_deps":{"510":SHA["frozen_510"],"556":SHA["frozen_556"],"729":SHA["canonical_729"]},
  "import_mode":"SOURCE_TRANSPORT_DISPOSABLE_ONLY_NO_CODEX_WORKTREE",
  "active_writer":"#826 WholeGameEconomySlotV3 B0162G02 ONLY (not actually installed)",
  "excluded_active_writers":["#787 raw CanonicalSingleAuthorityStoreV3R3 (REFERENCE_ONLY)",
    "#749","#773","#753","#741","#793","#764"],
  "source_only_requires_new_Unity_asmdef_and_owner_approved_binder":True,
  "unity_scene_and_prefab_live_integration":"NOT_PROVEN",
  "native_Unity_G1":"NOT_RUN",
  "formal_composite_game_G0":"HOLD_MISSING_ACTUAL_UNITY_BINDER_AND_NATIVE",
  "requires_codex_explicit_scope_ack":True,
  "source_files":origin,"reference_only_pins":refs,
  "meta_guid_count":len(guid),"asmdef_count":len(assemblies),
  "missing_assembly_references":missing,
  "safe_intake":"EMPTY_DISPOSABLE_RECEIVER_ONLY; NO_SHARED_OVERWRITE; NO_SHARED_ROLLBACK",
  "migration":"NO_IMPLICIT_OLD_SAVE_MIGRATION; BACKUP_AND_EXPLICIT_APPROVAL_REQUIRED",
 }
 if any(x for x in missing.values()):raise AssertionError("ASSEMBLY_REFERENCE_NOT_INCLUDED "+repr(missing))
 tests=physical_scratch_tests(data)
 archive=payload_zip(data,manifest)
 out_dir.mkdir(parents=True,exist_ok=True)
 (out_dir/"COMPOSITE_MANIFEST.json").write_text(json.dumps(manifest,sort_keys=True,indent=2)+"\n")
 (out_dir/"B0161_W29_EXACT_SOURCE_ONLY.zip").write_bytes(archive)
 result={"package":"B01-61-P17W29-EXACT-COMPOSITE-SOURCE-001","sha256":hashlib.sha256(archive).hexdigest(),
 "zip_bytes":len(archive),"source_files":len(data),"referenced_only_files":len(refs),
 "unity_guid_count":len(guid),"asmdefs":len(assemblies),"checks":tests,
 "source_git_blobs_pinned":len(origin)+len(refs),"source_transport_test":"PASS",
 "formal_combined_game_g0":"HOLD_UNTIL_BINDER_ABI_G0_AND_RECEIVER_ACK",
 "native_unity_g1":"NOT_RUN","integrated":False,"released":False}
 (out_dir/"SOURCE_RECEIPT.json").write_text(json.dumps(result,sort_keys=True,indent=2)+"\n")
 print(json.dumps(result,sort_keys=True))
 return result
if __name__=="__main__":
 parser=argparse.ArgumentParser();parser.add_argument("--out",type=pathlib.Path,required=True)
 args=parser.parse_args();run(args.out)
