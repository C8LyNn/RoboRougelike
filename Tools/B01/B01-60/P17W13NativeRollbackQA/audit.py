#!/usr/bin/env python3
"""B01-60 W13: original W12 rollback source RED -> isolated own receiver R2 GREEN.

This is NOT a new Unity runner. It executes the genuine frozen W12 runner.py
and the versioned W13 hardening against real disposable filesystem directories.
G1 remains NOT_RUN on hosts without licensed Unity Editor 6000.3.14f1.
"""
from __future__ import annotations
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import subprocess
import sys
import tempfile
import types

ROOT=Path(__file__).resolve().parents[4]
HERE=Path(__file__).resolve().parent
RUNNER=ROOT/"Tools/B01/B01-60/P17W12NativeSecureV2QA/runner.py"
PARENT="5778023163d8b51d23abfebaa17119e21bd5e131"
PARENT_RUNNER_BLOB="df944c854ab134b578a2b98d451f055717a16668"
SOURCE="d94400d95d13db7f27e1533df05c9292d5380431"
def require(cond,msg):
 if not cond:raise AssertionError(msg)
def git(*args):
 return subprocess.check_output(["git","-C",str(ROOT),*args],stderr=subprocess.PIPE)
def source_module(name,raw):
 instance=types.ModuleType(name)
 instance.__file__=str(RUNNER)
 exec(compile(raw,str(RUNNER),"exec"),vars(instance))
 return instance
def prepare(m,tmp,ordinal,kind):
 project=Path(tmp)/(kind+str(ordinal))
 data={"Assets/B0160NativeProbe/fixture.cs":b"source-only QA fixture",
       "Assets/B0160NativeProbe/fixture.cs.meta":b"fileFormatVersion: 2\nguid: 123456789012345678901234567890ab\n"}
 # This is a synthetic IO fixture testing the genuine receiver rollback code,
 # NOT a falsified owner Unity runtime or actual game/PhysX package.
 receipt={
  "original_source_sha":SOURCE,
  "native_unity":"NOT_RUN",
  "source_only_preflight":True,
  "codex_ack":False,
  "integrated":False,
  "native_meta_guids":m.guid_check(data),
  "file_sha256":{p:m.sha(v) for p,v in sorted(data.items())},
 }
 packed=m.zip_data(data,receipt)
 m.stage(packed,data,project)
 return project,packed,data
def main():
 old_text=git("show",PARENT+":Tools/B01/B01-60/P17W12NativeSecureV2QA/runner.py")
 require(git("rev-parse",PARENT+":Tools/B01/B01-60/P17W12NativeSecureV2QA/runner.py"
                ).decode().strip()==PARENT_RUNNER_BLOB,
         "frozen baseline #532 original runner changed")
 require(git("merge-base",PARENT,"HEAD").decode().strip()==PARENT,
         "W13 branch not based on exact frozen W12 receiver")
 require(git("merge-base",SOURCE,"HEAD").decode().strip()==SOURCE,
         "W13 branch missing selected exact owner #510")
 new_text=RUNNER.read_bytes()
 require(old_text != new_text, "W13 source not versioned")
 require(b"present_dirs==allowed_dirs" in new_text,"W13 directory fail-closed gate missing")
 old=source_module("frozen_w12_original",old_text)
 new=source_module("versioned_w13_hardened",new_text)
 # Real source checks: we execute the previous frozen receiver *as shipped*,
 # NOT a paraphrase or a rewritten clone of its vulnerable rollback.
 old_cases=[("foreign_empty_root","UNOWNED_EMPTY"),
            ("foreign_empty_nested","UserContent/PatientWork/Empty")]
 older_red=0
 fixed_cases=0
 pos=0
 events=[]
 with tempfile.TemporaryDirectory(prefix="b01-60-w13-actual-") as temp:
  index=0
  for name,foreign in old_cases:
   root,packed,files=prepare(old,temp,index,"old_");index+=1
   x=root/foreign;x.mkdir(parents=True)
   # This is the known failure: previous version deletes entire root even
   # when a foreign EMPTY directory existed outside its manifest.
   old.rollback(packed,files,root)
   require(not root.exists(),"old original receiver unexpectedly rejected foreign empty directory")
   older_red+=1
   events.append({"case":name,"original_W12":"UNSAFE_REMOVAL_OBSERVED",
                  "source":"exact frozen original #532"})
  denied_paths=[
    "FOREIGN_EMPTY",
    "Assets/B0160NativeProbe/foreign_empty",
    "UserContent/Deep/Nested/Empty",
    "Packages/_localUserData",
    "ProjectSettings/_localUserData",
    "Assets/ForeignParent/ForeignEmpty",
    "Assets/ForeignParent/ForeignNested/Leaf",
    "NewRoot/ForeignDeep/Leaf"
  ]
  for name in denied_paths:
   root,packed,files=prepare(new,temp,index,"new_");index+=1
   foreign=root/name
   foreign.mkdir(parents=True)
   before=sorted(str(p.relative_to(root)) for p in root.rglob("*"))
   try:new.rollback(packed,files,root)
   except AssertionError as e:
    require("directory" in str(e).lower(),"wrong rejection reason "+str(e))
    require(root.exists() and foreign.is_dir(),"rollback destroyed foreign empty dir")
    require(sorted(str(p.relative_to(root)) for p in root.rglob("*"))==before,
            "rollback partially modified target before fail-closed refusal")
    fixed_cases+=1
    events.append({"case":"foreign_empty_"+name,"W13":"REFUSED_NO_CHANGE"})
   else:raise AssertionError("W13 deleted foreign empty dir "+name)
  # Negative modified source bytes: refuse without any deletion.
  root,packed,files=prepare(new,temp,index,"new_");index+=1
  f=root/"Assets/B0160NativeProbe/fixture.cs"
  f.write_bytes(f.read_bytes()+b"tampered")
  try:new.rollback(packed,files,root)
  except AssertionError:
   require(f.is_file() and root.is_dir(),"changed file destroyed")
   fixed_cases+=1
  else:raise AssertionError("W13 deleted tampered file")
  # Negative foreign file: refuse.
  root,packed,files=prepare(new,temp,index,"new_");index+=1
  ff=root/"FOREIGN-USER-FILE.txt";ff.write_text("preserve")
  try:new.rollback(packed,files,root)
  except AssertionError:
   require(ff.is_file(),"foreign user file destroyed")
   fixed_cases+=1
  else:raise AssertionError("W13 deleted foreign file")
  # Negative original file removed: refuse.
  root,packed,files=prepare(new,temp,index,"new_");index+=1
  (root/"Assets/B0160NativeProbe/fixture.cs").unlink()
  try:new.rollback(packed,files,root)
  except AssertionError:
   require(root.is_dir(),"missing file target removed")
   fixed_cases+=1
  else:raise AssertionError("W13 deleted incomplete staged project")
  # Optional symlink negative. Windows GitHub runners may lack symlink privilege.
  symlinks="NOT_RUN_OS_NO_SYMLINK_PRIVILEGE"
  root,packed,files=prepare(new,temp,index,"new_");index+=1
  target=Path(temp)/"USER-OUTSIDE.txt";target.write_text("preserve me")
  lnk=root/"linked-user-data"
  try:
   lnk.symlink_to(target)
  except (OSError,NotImplementedError):
   require(root.is_dir() and target.read_text()=="preserve me","symlink probe damaged target")
  else:
   try:new.rollback(packed,files,root)
   except AssertionError:
    require(lnk.is_symlink() and target.read_text()=="preserve me",
            "symlink rollback damaged linked user data")
    symlinks="PASS_REJECTED"
    fixed_cases+=1
   else:raise AssertionError("W13 deleted linked target")
  # Legitimate owner-only fully-exact staging rolls back without leftover dirs.
  root,packed,files=prepare(new,temp,index,"new_");index+=1
  new.rollback(packed,files,root)
  require(not root.exists(),"known safe exact staging not fully removed")
  pos+=1
  events.append({"case":"clean_receiver_stage","W13":"PASS_ROLLBACK"})
 require(older_red==2 and fixed_cases>=11 and pos==1,
         "missing exact original RED->new GREEN or positive tests")
 receipt={
   "immutable_owner_source_sha":SOURCE,
   "frozen_previous_qa_sha":PARENT,
   "frozen_previous_qa_runner_git_blob":PARENT_RUNNER_BLOB,
   "new_qa_head":git("rev-parse","HEAD").decode().strip(),
   "real_original_W12_empty_directory_delete_REPRODUCED":older_red,
   "real_W13_predelete_failclosed_refusals":fixed_cases,
   "real_W13_safe_rollback_pass":pos,
   "symlink":symlinks,
   "native_Unity_6000_3_14f1":"NOT_RUN",
   "native_XML":"NOT_CREATED",
   "actual_Editor_log":"NOT_CREATED",
   "G2":"HOLD","Codex_ACK":"NONE",
   "events":events,
 }
 out=ROOT/"qa-w13-native-rollback-evidence"
 require(not out.exists(),"refuse overwrite evidence")
 out.mkdir()
 (out/"receipt.json").write_text(json.dumps(receipt,sort_keys=True,indent=2)+"\n")
 print("B01_60_W13_ORIGINAL_W12_ROLLBACK_EMPTY_DIR_RED",older_red)
 print("B01_60_W13_W13_REFUSED_FOREIGN_CONTENT",fixed_cases)
 print("B01_60_W13_W13_EXACT_SAFE_ROLLBACK_PASS",pos)
 print("B01_60_W13_SYMLINK",symlinks)
 print("B01_60_W13_SELECTED_PRODUCER_SHA",SOURCE)
 print("B01_60_W13_NATIVE_UNITY_G1_NOT_RUN NO_XML NO_EDITORLOG NO_CODEX_ACK")
 return 0
if __name__=="__main__":
 try:sys.exit(main())
 except Exception as e:
  print("B01_60_W13_AUDIT_FAIL",repr(e),file=sys.stderr)
  sys.exit(2)
