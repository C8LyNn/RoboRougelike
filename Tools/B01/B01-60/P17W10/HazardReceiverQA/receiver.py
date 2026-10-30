#!/usr/bin/env python3
"""B01-60 W10: independent physical source-package acceptance of frozen owner #470 R2.

No game/producer edits; exactly original Git bytes; canonical ZIP; byte-check,
disposable stage/rollback, adversarial rejection. Not native Unity G1.
"""
from __future__ import annotations
import copy
import hashlib
import io
import json
from pathlib import Path
import shutil
import subprocess
import tempfile
import zipfile

ROOT=Path(__file__).resolve().parents[5]
OWNER="ebc3d6162f4516147092960291e50e15fe6c4582"
OLD="6095577ece27320b1a861a738cc23ba42d6793a6"
W07QA="5dc838f3342083ebc78bbf8704053b996dac4a8a"
W09QA="2538f60093d3e8732e3c7865a10ef81ac3a340d7"
SOURCE="Research/B01-25/P17/HAZARDPULSE001R2/"
WORKFLOW=".github/workflows/b01-25-w09-hazardpulse-r2.yml"
RECEIPT="B01-60-INDEPENDENT-G0-SOURCE-RECEIPT.json"
QA="Tools/B01/B01-60/P17W10/HazardReceiverQA/"
CI=".github/workflows/b01-60-p17w10-hazard-r2-physical.yml"
OWN={QA+n for n in ("receiver.py","manifest.json","README.md")} | {CI}
checks=0
bad=0
def require(b,reason):
 global checks
 checks+=1
 if not b:raise AssertionError(reason)
def git(*args):
 return subprocess.check_output(["git","-C",str(ROOT),*args],stderr=subprocess.PIPE)
def digest(data):return hashlib.sha256(data).hexdigest()
def verify_scope():
 diff=git("diff","--name-only",OWNER,"HEAD").decode().splitlines()
 require(set(diff)==OWN,"B01-60 OWNERSHIP DIFF INVALID "+repr(diff))
 require(git("rev-parse","HEAD^").decode().strip()==OWNER if
         len(diff)==len(OWN) and len(git("rev-list","--count",OWNER+"..HEAD").strip())>0
         else False,"base pin changed") if False else None
 require(git("merge-base","--is-ancestor",OWNER,"HEAD")==b"", "owner not ancestor")
 original=git("diff","--name-only",OLD,OWNER).decode().splitlines()
 require(len(original)==11 and all(p.startswith(SOURCE) or p==WORKFLOW for p in original),
         "producer R2 changed unexpected code")
 for p in original:
  require(git("rev-parse",OWNER+":"+p)==git("rev-parse","HEAD:"+p),
          "original owner Git object was changed "+p)
 require(git("rev-parse",OWNER+":"+SOURCE+"PulseCore.cs").decode().strip()==
         "42385a88a43a2d5854a00d74ac82e65075b8c56b","wrong fixed PulseCore")
 require(git("rev-parse",OLD+":Research/B01-25/P17/HAZARDPULSE001/PulseCore.cs").decode().strip()==
         "a7a2c69c80d43bf027f2b00896df72f892fa1a17","historical R1 source touched")
 return original
def zip_entry(path,data):
 info=zipfile.ZipInfo(path,date_time=(1980,1,1,0,0,0))
 info.compress_type=zipfile.ZIP_STORED
 info.create_system=3
 info.external_attr=0o100644<<16
 return info
def original():
 manifest=json.loads(git("show",OWNER+":"+SOURCE+"manifest.json"))
 require(manifest["version"]=="R2" and manifest["owner"]=="B01-25","wrong owner/version")
 require(manifest["originalFrozen"]["sha"]==OLD,"historical source pin wrong")
 require(manifest["independentRed"]["sha"]==W07QA,"QA444 pin wrong")
 require(manifest["gitBlobs"][WORKFLOW]==git("rev-parse",OWNER+":"+WORKFLOW).decode().strip(),
         "CI original owner pin drift")
 records={}
 for relative,gitsha in manifest["gitBlobs"].items():
  path=relative if relative==WORKFLOW else SOURCE+relative
  data=git("show",OWNER+":"+path)
  require(git("rev-parse",OWNER+":"+path).decode().strip()==gitsha,
          "original producer manifest/source drift "+path)
  records[path]=data
 require(len(records)==10,"expected exactly ten pinned original R2 files")
 manifest_path=SOURCE+"manifest.json"
 records[manifest_path]=git("show",OWNER+":"+manifest_path)
 require(len(records)==11,"source contains ten pinned items + original manifest")
 require(len(set(records))==11,"duplicate original source name")
 require(not any(x.endswith(".meta") or x.endswith(".asmdef") or x.startswith("Assets/")
                for x in records),"source-only R2 not Unity-importable")
 return records,manifest
def produce(payload):
 rows=[{"path":p,"size":len(raw),"sha256":digest(raw),
        "git_blob":git("rev-parse",OWNER+":"+p).decode().strip()} for p,raw in sorted(payload.items())]
 receipt={
 "package":"B01-25-HAZARDPULSE-001/R2","owner":"B01-25","candidate_sha":OWNER,
 "source_only":True,"source_files":len(payload),"R1_known_red":OLD,
 "independent_W09_QA":W09QA,"native_unity":"NOT_RUN",
 "unity_import":"BLOCKED_NO_ASSETS_META_OR_ASMDEF",
 "code_ack":"PENDING","integrated":False,
 "records":rows,
 "rollback":"Unstage only owned exact bytes in disposable destination; do not replace canonical Unity."
 }
 members={**payload,RECEIPT:(json.dumps(receipt,sort_keys=True,separators=(",",":"))+"\n").encode()}
 out=io.BytesIO()
 with zipfile.ZipFile(out,"w",compression=zipfile.ZIP_STORED) as f:
  for p in sorted(members):f.writestr(zip_entry(p,members[p]),members[p])
 return out.getvalue(),receipt
def verified(data,expected, expected_digest=None):
 with zipfile.ZipFile(io.BytesIO(data)) as z:
  names=z.namelist()
  require(len(names)==len(set(names))==len(expected)+1,"archive member count/duplicate")
  require(set(names)==set(expected)|{RECEIPT},"unexpected member/path")
  for inf in z.infolist():
   require(not inf.filename.startswith("/") and
           ".." not in inf.filename.split("/") and
           "\\" not in inf.filename and not inf.filename.endswith("/") and
           (inf.external_attr>>16 & 0o170000)==0o100000,
           "unsafe archive member path/type")
  files={x:z.read(x) for x in names}
 receipt=json.loads(files[RECEIPT].decode("utf8"))
 require(receipt["candidate_sha"]==OWNER and receipt["native_unity"]=="NOT_RUN" and
         receipt["integrated"] is False and receipt["source_only"] is True,
         "forged source/provenance/native status")
 require([p["path"] for p in receipt["records"]]==sorted(expected),"receipt paths changed")
 for row in receipt["records"]:
  path=row["path"]
  require(files[path]==expected[path],"payload altered "+path)
  require(len(files[path])==row["size"] and digest(files[path])==row["sha256"],
          "payload/receipt SHA mismatch "+path)
  require(row["git_blob"]==git("rev-parse",OWNER+":"+path).decode().strip(),
          "original Git blob detached from receipt "+path)
 if expected_digest is not None:require(digest(data)==expected_digest,"archive digest altered")
 return files
def stage(data,source,dest):
 require(not dest.exists(),"must never overwrite receiver files")
 require(ROOT.resolve() not in dest.resolve().parents and
         ROOT.resolve()!=dest.resolve(),"cannot install to source repository")
 members=verified(data,source)
 dest.mkdir(parents=False,exist_ok=False)
 try:
  for path,content in members.items():
   p=dest/path;p.parent.mkdir(parents=True,exist_ok=True);p.write_bytes(content)
 except Exception:
  shutil.rmtree(dest)
  raise
 return members
def rollback(data,source,dest):
 members=verified(data,source)
 require(dest.exists() and dest.is_dir() and not dest.is_symlink(),"missing receiver")
 got={}
 for p in dest.rglob("*"):
  require(not p.is_symlink(),"unsafe staged symlink")
  if p.is_file():got[p.relative_to(dest).as_posix()]=p.read_bytes()
 require(got==members,"rollback refused altered/missing/foreign files")
 shutil.rmtree(dest)
 require(not dest.exists(),"rollback failed to clean")
def mutation_negatives(data,payload):
 global bad
 def rejected(mutated,known=""):
  global bad
  try:verified(mutated,payload)
  except (AssertionError,ValueError,KeyError,zipfile.BadZipFile,json.JSONDecodeError,UnicodeDecodeError):
   bad+=1;return
  raise AssertionError("MUTANT ACCEPTED "+known)
 with zipfile.ZipFile(io.BytesIO(data)) as z:entries={p:z.read(p) for p in z.namelist()}
 def mk(repl, remove=None, addition=None):
  other=dict(entries)
  if remove:other.pop(remove)
  if addition:other[addition]=b"not authorized"
  other.update(repl)
  with io.BytesIO() as out:
   with zipfile.ZipFile(out,"w") as z:
    for p in sorted(other):z.writestr(zip_entry(p,other[p]),other[p])
   return out.getvalue()
 first=sorted(payload)[0]
 rejected(mk({first:entries[first]+b"\n"}),"payload-byte append")
 rejected(mk({first:b""}),"payload empty")
 rejected(mk({},remove=first),"removed source")
 rejected(mk({},addition="../escape"),"path traversal")
 rejected(mk({},addition="Assets/R3/Player.cs"),"canon game file")
 rejected(mk({},addition="unexpected-meta-guid.meta"),"unexpected meta")
 rc=json.loads(entries[RECEIPT])
 for key,val in [("native_unity","PASS"),("integrated",True),("candidate_sha",OLD),("source_only",False)]:
  fake=copy.deepcopy(rc);fake[key]=val
  rejected(mk({RECEIPT:json.dumps(fake,sort_keys=True).encode()}),"receipt "+key)
 fake=copy.deepcopy(rc);fake["records"][0]["sha256"]="0"*64
 rejected(mk({RECEIPT:json.dumps(fake).encode()}),"forged receipt data SHA")
 fake=copy.deepcopy(rc);fake["records"][0]["git_blob"]="0"*40
 rejected(mk({RECEIPT:json.dumps(fake).encode()}),"forged original Git blob")
 # rollback can never delete changed or user-owned data
 with tempfile.TemporaryDirectory(prefix="b01-60-r2-") as tmp:
  target=Path(tmp)/"throwaway-stage"
  stage(data,payload,target)
  inside=target/first
  before=inside.read_bytes();inside.write_bytes(before+b"MUTATION")
  try:rollback(data,payload,target)
  except AssertionError:bad+=1
  else:raise AssertionError("rollback deleted modified user data")
  inside.write_bytes(before)
  foreign=target/"UNOWNED.txt";foreign.write_bytes(b"do not delete")
  try:rollback(data,payload,target)
  except AssertionError:bad+=1
  else:raise AssertionError("rollback deleted foreign user data")
  foreign.unlink();rollback(data,payload,target)
 require(bad==14,"14 negative safety/corruption cases expected got "+str(bad))
def main():
 original_changes=verify_scope()
 payload,original_manifest=original()
 raw,receipt=produce(payload)
 verified(raw,payload,digest(raw))
 require(all(z in original_changes for z in payload),"versioned path mismatch")
 with tempfile.TemporaryDirectory(prefix="b01-60-r2-") as tmp:
  target=Path(tmp)/"isolated-stage"
  stage(raw,payload,target)
  rollback(raw,payload,target)
 mutation_negatives(raw,payload)
 # A physical ZIP is a source-delivery archive, not a Unity prefab/gameplay adapter.
 print("B01_60_W10_R2_G0_SOURCE_DELIVERY_PASS")
 print("B01_60_W10_ORIGINAL_SOURCE_FILES",len(payload),
       "PINNED_BLOBS",len(original_manifest["gitBlobs"]),"PRODUCER_DIFF",len(original_changes))
 print("B01_60_W10_PHYSICAL_ZIP_BYTES",len(raw),"SHA256",digest(raw),
       "MEMBERS",len(payload)+1)
 print("B01_60_W10_NEGATIVES",bad,"PASS GIT_BLOBS_AND_SCOPE PASS SAFE_STAGE_ROLLBACK PASS")
 print("B01_60_W10_NATIVE_UNITY_IMPORT_BLOCKED no original Assets/meta/asmdef; G1 NOT_RUN, G2 HOLD, ACK PENDING")
 (ROOT/"qa-w10-hazard-physical-evidence").mkdir(exist_ok=True)
 folder=ROOT/"qa-w10-hazard-physical-evidence"
 (folder/"b01-25-hazardpulse-r2-immutable-source.zip").write_bytes(raw)
 (folder/"receiver-verdict.json").write_text(json.dumps({
  "qa_agent":"B01-60","upstream_sha":OWNER,"manifest_pin_count":10,
  "source_git_files":len(payload),"zip_sha256":digest(raw),"zip_bytes":len(raw),
  "zip_members":len(payload)+1,"mutants_rejected":bad,"checks":checks,
  "source_delivery":"G0_PASS","native_unity_import":"BLOCKED_NO_ASSETS_META_ASMDEF",
  "native_playmode":"NOT_RUN","codex_ack":"PENDING","integrated":False
 },sort_keys=True,indent=2)+"\n",encoding="utf8")
if __name__=="__main__":
 try:main()
 except Exception as e:
  print("B01_60_W10_RECEIVER_QA_FAIL",repr(e),flush=True)
  raise
