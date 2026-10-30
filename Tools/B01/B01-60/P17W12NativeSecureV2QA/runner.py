#!/usr/bin/env python3
"""B01-60 W12: real Unity native receiver; NEVER claim G1 from source-only validation.

CI builds the exact source-locked disposable Unity project and validates rollback
without a license; the native mode needs user-supplied authorized Unity executable.
No permanent changes to producer, Codex, or target Unity project.
"""
from __future__ import annotations
import argparse
import copy
import hashlib
import io
import json
import os
from pathlib import Path, PurePosixPath
import re
import shutil
import subprocess
import sys
import tempfile
import xml.etree.ElementTree as ET
import zipfile

ROOT=Path(__file__).resolve().parents[4]
HERE=Path(__file__).resolve().parent
SHA="d94400d95d13db7f27e1533df05c9292d5380431"
PARENT="33a32b8bd9ab40467ab1a17b5684926a37dbd4d3"
W06="cb2b3a76e84266e2730fffc4ab048c1039f0652f"
BASE="Tools/B01/B01-60/P17W12NativeSecureV2QA/"
WF=".github/workflows/b01-60-p17w12-native-unity-readiness.yml"
OWN={BASE+s for s in ("runner.py","IndependentSecureV2PlayMode.cs",
                       "IndependentSecureV2EditMode.cs","README.md","manifest.json")}|{WF}
# W13 own versioned rollback hardening only; exact #510 and frozen W12 #532 unchanged.
OWN|={"Tools/B01/B01-60/P17W13NativeRollbackQA/"+n for n in ("audit.py","README.md","manifest.json")}
OWN.add(".github/workflows/b01-60-p17w13-native-rollback-audit.yml")
# W14 isolated QA-only native XML case identity gate; never touches immutable #510.
OWN|={"Tools/B01/B01-60/P17W14NativeResultProof/"+n for n in
     ("test_native_result_gate.py","README.md","manifest.json")}
OWN.add(".github/workflows/b01-60-p17w14-native-xml-case-gate.yml")
# W15 versioned B01-60-only QA gate: both EditMode AND PlayMode verified.
OWN|={"Tools/B01/B01-60/P17W15NativePairGate/"+n for n in
     ("test_native_pair_gate.py","README.md","manifest.json")}
OWN.add(".github/workflows/b01-60-p17w15-native-dual-suite-gate.yml")
# W16 QA-only: reject native PASS if Unity import mutates selected owner or adds code.
OWN|={"Tools/B01/B01-60/P17W16NativeSourceIntegrity/"+n for n in
     ("test_source_integrity.py","README.md","manifest.json")}
OWN.add(".github/workflows/b01-60-p17w16-native-source-integrity.yml")
PREFIXES=("Assets/B0123DebrisRepulse001","Assets/B0123SecureReattachV2")
NATIVE="Assets/B0160SecureV2NativeQA"
PROJECT_EXTRA=("Packages/manifest.json","Packages/packages-lock.json",
               "ProjectSettings/ProjectVersion.txt")
RECEIPT="B0160_W12_SOURCE_PROVENANCE.json"
PINS={
"Assets/B0123SecureReattachV2/Runtime/AtomicDockLedger.cs":"b6a4d04b0c6407c8c84451b4d599d96fe0e4d1a9",
"Assets/B0123SecureReattachV2/Runtime/SecureDebrisPlaygroundV2.cs":"328977b4cdcdcd7c9f128c01f11ff67f8e46086a",
"Assets/B0123SecureReattachV2/Runtime/SecureDockPolicy.cs":"9f80f705475bd312f209033a57bd07e03bf95628",
"Assets/B0123SecureReattachV2/Tests/PlayMode/SecureReattachV2PlayModeTests.cs":"77c1962ae5cb2c1d58b5c9f0a18148387eff5d1a",
"Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs":"fa1aa5e531927cbe73cf5fdfe161b4a981316db5",
}
def git(*args):
 return subprocess.check_output(["git","-C",str(ROOT),*args],stderr=subprocess.PIPE)
def sha(raw):return hashlib.sha256(raw).hexdigest()
def require(ok,msg):
 if not ok:raise AssertionError(msg)
def owner_bytes(path):
 raw=git("show",SHA+":"+path)
 require(git("rev-parse",SHA+":"+path).strip()==
         git("rev-parse","HEAD:"+path).strip(),"owner file modified by QA "+path)
 if path in PINS:
  require(git("rev-parse",SHA+":"+path).decode().strip()==PINS[path],
          "immutable producer object changed "+path)
 return raw
def named_sources():
 require(git("merge-base",SHA,"HEAD").decode().strip()==SHA,
         "QA must be stacked on selected #510, no #511/#494")
 require(set(git("diff","--name-only",SHA,"HEAD").decode().splitlines())==OWN,
         "non-owned QA diff or missing own file")
 require(git("merge-base",PARENT,SHA).decode().strip()==PARENT,
         "selected owner R2 is not based on original V2 #488")
 require(git("merge-base",W06,SHA).decode().strip()==W06,
         "selected owner R2 wrong immutable W06 ancestor")
 owner_tree=git("ls-tree","-r","--name-only",SHA).decode().splitlines()
 original={}
 for p in owner_tree:
  if p in PROJECT_EXTRA or any(p==pr+".meta" or p.startswith(pr+"/") for pr in PREFIXES):
   original[p]=owner_bytes(p)
 require(len(original)>=25,"missing original selected V2/W06 Unity source")
 require(all(p in original for p in PINS),"critical producer source missing")
 require(not any(x.startswith("Assets/B0123DebrisPlayground001") or
                 x.startswith("Assets/B0123DockHardening001") or
                 x.startswith("Assets/B0123ReattachClearance001") or
                 x.startswith("Assets/B0123SecureReattachV2R11Alternative")
                 for x in original),"legacy or alternative installed")
 manifest=json.loads(original["Packages/manifest.json"])
 require(manifest["dependencies"].get("com.unity.test-framework")=="1.6.0",
         "selected original Unity test-framework package mismatch")
 version=original["ProjectSettings/ProjectVersion.txt"].decode()
 require("6000.3.14f1" in version,"selected native Unity editor version mismatch")
 return original
def extra_test_assets():
 play=(HERE/"IndependentSecureV2PlayMode.cs").read_bytes()
 edit=(HERE/"IndependentSecureV2EditMode.cs").read_bytes()
 play_text=play.decode("utf-8")
 edit_text=edit.decode("utf-8")
 require(play_text.count("[TestCase(")>=12 and "Physics.SyncTransforms()" in play_text and
         "body.linearVelocity" in play_text and "AssertUnchanged" in play_text and
         "TryReattach" in play_text and "ClearForNewRun" in play_text and
         "quarantine" in play_text,"invalid original independent PhysX test source")
 require(edit_text.count("[Test]")>=2 and "AtomicDockLedger" in edit_text,
         "missing Unity EditMode fixture")
 playasm={
  "name":"B0160.SecureV2.NativeQA.PlayMode",
  "rootNamespace":"B01.SecureReattachV2.NativeQA",
  "references":["B0123.SecureReattachV2.Runtime","B0123.DebrisRepulse.Runtime"],
  "includePlatforms":[],"excludePlatforms":[],"allowUnsafeCode":False,
  "overrideReferences":True,"precompiledReferences":["nunit.framework.dll"],
  "autoReferenced":False,"defineConstraints":["UNITY_INCLUDE_TESTS"],
  "versionDefines":[],"noEngineReferences":False,
  "optionalUnityReferences":["TestAssemblies"]
 }
 editasm=copy.deepcopy(playasm)
 editasm["name"]="B0160.SecureV2.NativeQA.EditMode"
 editasm["includePlatforms"]=["Editor"]
 files={
  NATIVE+"/Tests/PlayMode/IndependentSecureV2PlayMode.cs":play,
  NATIVE+"/Tests/EditMode/IndependentSecureV2EditMode.cs":edit,
  NATIVE+"/Tests/PlayMode/B0160.SecureV2.NativeQA.PlayMode.asmdef":
    (json.dumps(playasm,indent=2)+"\n").encode(),
  NATIVE+"/Tests/EditMode/B0160.SecureV2.NativeQA.EditMode.asmdef":
    (json.dumps(editasm,indent=2)+"\n").encode()
 }
 dirs=[NATIVE,NATIVE+"/Tests",NATIVE+"/Tests/PlayMode",NATIVE+"/Tests/EditMode"]
 for p in dirs:
  files[p+".meta"]=meta(p,is_folder=True)
 for p in list(files):
  if not p.endswith(".meta"):
   files[p+".meta"]=meta(p)
 return files
def meta(path,is_folder=False):
 guid=hashlib.sha256(("B01-60-W12-UNITY-NATIVE-RECEIVER-ONLY/"+path).encode()).hexdigest()[:32]
 if is_folder:
  settings="folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
 elif path.endswith(".cs"):
  settings="MonoImporter:\n  externalObjects: {}\n  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n  icon: {instanceID: 0}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
 elif path.endswith(".asmdef"):
  settings="AssemblyDefinitionImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
 else:
  raise AssertionError("unsupported generated asset type "+path)
 return ("fileFormatVersion: 2\n"+"guid: "+guid+"\n"+settings).encode()
def guid_check(raws):
 lookup={}
 for p,raw in raws.items():
  if not p.endswith(".meta"):continue
  match=re.search(rb"^guid:\s*([0-9a-f]{32})\s*$",raw,re.M)
  require(match is not None,"missing real Unity GUID for "+p)
  guid=match.group(1)
  require(guid not in lookup,"Unity GUID collision "+p+" <-> "+lookup.get(guid,""))
  lookup[guid]=p
 return len(lookup)
def zip_data(items,receipt):
 with io.BytesIO() as buf:
  with zipfile.ZipFile(buf,"w",compression=zipfile.ZIP_STORED) as out:
   entries={**items,RECEIPT:(json.dumps(receipt,sort_keys=True,indent=2)+"\n").encode()}
   for p in sorted(entries):
    inf=zipfile.ZipInfo(p,date_time=(1980,1,1,0,0,0))
    inf.compress_type=zipfile.ZIP_STORED
    inf.create_system=3;inf.external_attr=0o100644<<16
    out.writestr(inf,entries[p])
  return buf.getvalue()
def verify(raw,expected):
 with zipfile.ZipFile(io.BytesIO(raw)) as z:
  names=z.namelist()
  require(len(names)==len(set(names))==len(expected)+1,"wrong zip membership")
  require(set(names)==set(expected)|{RECEIPT},"foreign or missing members")
  content={}
  for x in z.infolist():
   p=x.filename
   require(p and not p.startswith("/") and
           "\\" not in p and
           p==str(PurePosixPath(p)) and
           not any(seg==".." for seg in PurePosixPath(p).parts) and
           (x.external_attr>>16 & 0o170000)==0o100000,
           "unsafe ZIP entry "+p)
   content[p]=z.read(x)
  receipt=json.loads(content.pop(RECEIPT))
  require(receipt["original_source_sha"]==SHA and
          receipt["native_unity"]=="NOT_RUN" and
          receipt["integrated"] is False and
          receipt["codex_ack"] is False and
          receipt["source_only_preflight"] is True,
          "forged native/ACK/source receipt")
  for path,rawfile in expected.items():
   require(content[path]==rawfile,"source bytes mutated "+path)
  require(receipt["file_sha256"]=={p:sha(v) for p,v in sorted(expected.items())},
          "forged file SHA256 receipt")
  require(receipt["native_meta_guids"]==guid_check(expected),"wrong GUID count")
  return content
def negatives(good,expected):
 count=0
 def mutation(items,override=None,drop=None):
  items=dict(items)
  if drop:items.pop(drop)
  if override:items.update(override)
  meta_receipt={"original_source_sha":SHA,"native_unity":"NOT_RUN",
      "source_only_preflight":True,"codex_ack":False,"integrated":False,
      "native_meta_guids":guid_check(items),
      "file_sha256":{p:sha(v) for p,v in sorted(items.items())}}
  return zip_data(items,meta_receipt)
 def red(mutant):
  nonlocal count
  try:verify(mutant,expected)
  except (AssertionError,ValueError,KeyError,zipfile.BadZipFile):
   count+=1;return
  raise AssertionError("negative mutation falsely accepted")
 first=next(p for p in sorted(expected) if p.endswith(".cs"))
 red(mutation(expected,override={first:expected[first]+b"\n// malicious source edit"}))
 red(mutation(expected,drop=first))
 red(mutation(expected,override={"Assets/B0123DebrisPlayground001/Runtime/Legacy.cs":b"legacy"}))
 red(mutation(expected,override={"Assets/B0123PublicGuardAlternate/Runtime/Alternate.cs":b"not selected #494"}))
 red(mutation(expected,override={"../escape.cs":b"breakout"}))
 with zipfile.ZipFile(io.BytesIO(good)) as z:
  entries={x:z.read(x) for x in z.namelist()}
 rc=json.loads(entries[RECEIPT])
 for key,value in (("native_unity","PASS"),("codex_ack",True),("original_source_sha",PARENT)):
  forged=dict(rc);forged[key]=value
  red(zip_data(expected,forged))
 # Strict staging/rollback refuses all user modifications and foreign files.
 with tempfile.TemporaryDirectory(prefix="b01-60-w12-") as tmp:
  dest=Path(tmp)/"project"
  stage(good,expected,dest)
  firstfile=dest/first
  before=firstfile.read_bytes()
  firstfile.write_bytes(before+b"bad")
  try:rollback(good,expected,dest)
  except AssertionError:count+=1
  else:raise AssertionError("unsafe rollback erased changed user data")
  firstfile.write_bytes(before)
  foreign=dest/"USER_DATA.txt";foreign.write_text("preserve")
  try:rollback(good,expected,dest)
  except AssertionError:count+=1
  else:raise AssertionError("unsafe rollback erased foreign user data")
  foreign.unlink()
  rollback(good,expected,dest)
 require(count==10,"expected 10 valid negative proofs got "+str(count))
 return count
def stage(data,items,root):
 require(not root.exists(),"refuse overwrite Unity project")
 require(ROOT.resolve()!=root.resolve() and
         ROOT.resolve() not in root.resolve().parents,"refuse stage into repository")
 files=verify(data,items)
 root.mkdir(parents=False)
 try:
  for path,raw in files.items():
   target=root/path;target.parent.mkdir(parents=True,exist_ok=True)
   target.write_bytes(raw)
 except Exception:
  shutil.rmtree(root,ignore_errors=True)
  raise
def rollback(data,items,root):
 expected=verify(data,items)
 require(root.is_dir() and not root.is_symlink(),"no safe staged Unity project")
 # W13 security fix: a file-only inventory can be identical while unowned EMPTY
 # directories remain. Never recursively delete those foreign user directories.
 allowed_dirs=set()
 for name in expected:
  parent=PurePosixPath(name).parent
  while str(parent)!=".":
   allowed_dirs.add(str(parent))
   parent=parent.parent
 present={}
 present_dirs=set()
 for f in root.rglob("*"):
  require(not f.is_symlink(),"stage contains symlink")
  relative=f.relative_to(root).as_posix()
  if f.is_dir():
   present_dirs.add(relative)
  elif f.is_file():
   present[relative]=f.read_bytes()
  else:
   raise AssertionError("unexpected special receiver entry "+relative)
 require(present==expected,"rollback refused changed/unowned staged file")
 require(present_dirs==allowed_dirs,
         "rollback refused foreign/missing directory, including EMPTY directory: "+
         repr(sorted(present_dirs.symmetric_difference(allowed_dirs))))
 # Fail-closed scan completes BEFORE the first destructive operation.
 shutil.rmtree(root)
 require(not root.exists(),"rollback did not remove QA project")
def require_native_source_integrity(project, selected):
 # W16: Source-locked inputs must stay byte-identical after EACH real Unity
 # Editor suite, rather than trusting the original preflight ZIP alone.
 # Unity-generated Library/Temp/Logs caches are deliberately out of scope.
 require(project.is_dir() and not project.is_symlink(),
         "NATIVE_SOURCE_CHANGED: selected disposable project missing/symlink")
 expected_assets={p:raw for p,raw in selected.items() if p.startswith("Assets/")}
 require(expected_assets,"NATIVE_SOURCE_CHANGED: empty selected Assets contract")
 expected_dirs={"Assets"}
 for p in expected_assets:
  parent=PurePosixPath(p).parent
  while str(parent)!=".":
   expected_dirs.add(str(parent))
   parent=parent.parent
 observed_assets={}
 observed_dirs={"Assets"}
 assets=project/"Assets"
 require(assets.is_dir() and not assets.is_symlink(),
         "NATIVE_SOURCE_CHANGED: Assets root missing/symlink")
 for entry in assets.rglob("*"):
  require(not entry.is_symlink(),
          "NATIVE_SOURCE_CHANGED: symlink introduced by Unity import: "+str(entry))
  rel=entry.relative_to(project).as_posix()
  if entry.is_dir():
   observed_dirs.add(rel)
  elif entry.is_file():
   observed_assets[rel]=entry.read_bytes()
  else:
   raise AssertionError("NATIVE_SOURCE_CHANGED: special Assets node "+rel)
 require(set(observed_assets)==set(expected_assets),
         "NATIVE_SOURCE_CHANGED: missing/injected Assets: "+
         repr(sorted(set(observed_assets)^set(expected_assets))))
 require(observed_dirs==expected_dirs,
         "NATIVE_SOURCE_CHANGED: foreign/missing Assets directory: "+
         repr(sorted(observed_dirs^expected_dirs)))
 for path,raw in expected_assets.items():
  require(observed_assets[path]==raw,
          "NATIVE_SOURCE_CHANGED: selected Unity source byte drift "+path)
 # These original project dependency/version inputs are also frozen.
 for path in PROJECT_EXTRA:
  if path in selected:
   check=project/path
   require(check.is_file() and not check.is_symlink() and
           check.read_bytes()==selected[path],
           "NATIVE_SOURCE_CHANGED: selected project metadata drift "+path)
 return len(expected_assets)

def expected_independent_native_cases(platform):
 # Test identities from the existing frozen #532 independent Unity NUnit source,
 # not merely a potentially forged summary 'passed=14' counter.
 require(platform in ("EditMode","PlayMode"),"unsupported native platform")
 if platform=="EditMode":
  names=(
   "ExactOwnerRuntimeAndOriginalW06AreLinkableByUnity",
   "RealOriginalW06LedgerClearFailsClosedOnMissingIdBeforeCallbacks",
  )
  typename="SecureV2IndependentNativeEditMode"
 else:
  aliases=(
   "ColliderOnLayer31RefusesWithoutAnyBodyOrLedgerMutation",
   "MissingConsumedIdRefusesWithoutRealRigidbodyPartialCommit",
  )
  names=tuple(
   name+"("+str(index)+","+str(route)+")"
   for name in aliases for index in (0,1) for route in (0,1,2)
  )+(
   "ForcedMidCommitExceptionRestoresRealRigidbodyAndConsumedId",
   "RollbackFailureIsQuarantinedRatherThanSilentlyAccepted",
  )
  typename="SecureV2IndependentNativePlayMode"
 return {"B01.SecureReattachV2.NativeQA."+typename+"."+name for name in names}

def parse_xml(file,minimum):
 require(file.is_file(),"Unity native missing NUnit XML "+str(file))
 platform={2:"EditMode",14:"PlayMode"}.get(minimum)
 require(platform is not None,"unrecognized native suite threshold")
 root=ET.parse(file).getroot()
 result=root.attrib.get("result","")
 passed=int(root.attrib.get("passed",0))
 failed=int(root.attrib.get("failed",0))
 require(root.tag=="test-run" and result=="Passed" and
         passed>=minimum and failed==0,
         "native Unity XML failed or unverified run "+str(root.attrib))
 expected={x.replace(" ","") for x in expected_independent_native_cases(platform)}
 found={}
 for case in root.iter("test-case"):
  fullname=case.attrib.get("fullname","").replace(" ","")
  if fullname in expected:
   found[fullname]=found.get(fullname,0)+1
   require(case.attrib.get("result")=="Passed",
           "independent native NUnit case failed/skipped/inconclusive: "+fullname)
 require(set(found)==expected and all(n==1 for n in found.values()),
         "NATIVE_NOT_VERIFIED: absent, duplicate, or substituted B01-60 NUnit cases: "+
         repr(sorted(expected-set(found)))+" duplicated="+
         repr(sorted(k for k,v in found.items() if v!=1)))
 return {"result":result,"passed":passed,"failed":failed,
         "verified_independent_cases":len(expected),"xml":str(file)}

def require_editor_version_log(path):
 # Unity -logFile is mandatory, not an optional cosmetic artifact.
 # The exact Editor.log can be preserved for independent host inspection.
 require(path.is_file() and path.stat().st_size>0,
         "NATIVE_NOT_VERIFIED: real Unity Editor.log missing/empty "+str(path))
 log=path.read_text(encoding="utf-8",errors="replace")
 require("6000.3.14f1" in log,
         "NATIVE_NOT_VERIFIED: Unity Editor.log lacks selected editor version 6000.3.14f1")
 return True
def main():
 p=argparse.ArgumentParser()
 p.add_argument("--mode",choices=("preflight","native"),default="preflight")
 p.add_argument("--unity",help="absolute authorized Unity 6000.3.14f1 executable")
 p.add_argument("--out",default=str(ROOT/"qa-w12-v2-native-evidence"))
 args=p.parse_args()
 out=Path(args.out).resolve()
 require(not out.exists(),"evidence directory already exists (no overwrite)")
 originals=named_sources()
 fixtures=extra_test_assets()
 combined={**originals,**fixtures}
 require(len(combined)==len(originals)+len(fixtures),"overlapping source paths")
 guids=guid_check(combined)
 rec={"owner":"B01-60","original_pr":510,"original_source_sha":SHA,
      "original_parent_v2_sha":PARENT,"frozen_w06_sha":W06,
      "native_unity":"NOT_RUN","source_only_preflight":True,
      "codex_ack":False,"integrated":False,"native_g1":"NOT_RUN",
      "native_meta_guids":guids,
      "file_sha256":{p:sha(v) for p,v in sorted(combined.items())},
      "independent_playmode_cases_authored":14,
      "independent_editmode_cases_authored":2,
      "native_test_result":"NOT_RUN"}
 archive=zip_data(combined,rec)
 verify(archive,combined)
 failclosed=negatives(archive,combined)
 out.mkdir(parents=True)
 (out/"selected-510-native-receiver.zip").write_bytes(archive)
 (out/"receipt.json").write_text(json.dumps({
     "source_sha":SHA,"zip_sha256":sha(archive),"zip_bytes":len(archive),
     "source_and_fixture_files":len(combined),"meta_guids":guids,
     "negative_checks_passed":failclosed,
     "source_preflight":"PASS","native_unity":"NOT_RUN",
     "editmode":"NOT_RUN","playmode":"NOT_RUN",
     "fixture_case_counts":{"EditMode":2,"PlayMode":14},
     "code_ack":"PENDING","integrated":False
 },indent=2,sort_keys=True)+"\n")
 print("B01_60_W12_SELECTED_SOURCE_SHA",SHA)
 print("B01_60_W12_ZIP_SHA256",sha(archive),"SIZE",len(archive),
       "MEMBERS",len(combined)+1,"ORIGINAL_SOURCES",len(originals),
       "NEW_QA_ASSETS",len(fixtures),"GUIDS",guids)
 print("B01_60_W12_SOURCE_PREFLIGHT_PASS = exact original source and Unity-test-project ZIP")
 print("B01_60_W12_NEGATIVE_TESTS_PASS",failclosed)
 if args.mode=="preflight":
  print("B01_60_W12_NATIVE_UNITY_NOT_RUN (no authorized Editor was invoked)")
  return 0
 unity=Path(args.unity).expanduser() if args.unity else None
 if unity is None or not unity.is_file():
  print("B01_60_W12_NATIVE_NOT_RUN: authorized Unity executable missing")
  return 3
 # Isolated source-locked temporary copy only; never edit the supplied workspace.
 with tempfile.TemporaryDirectory(prefix="b01-60-w12-unity-") as tmp:
  project=Path(tmp)/"disposable-unity-project"
  stage(archive,combined,project)
  try:
   real={}
   for platform,min_tests in (("EditMode",2),("PlayMode",14)):
    xml=out/(platform+".xml");log=out/(platform+"-Editor.log")
    command=[str(unity),"-batchmode","-nographics","-quit",
             "-projectPath",str(project),"-runTests",
             "-testPlatform",platform,"-testResults",str(xml),
             "-logFile",str(log)]
    started=subprocess.run(command,stdout=subprocess.PIPE,
                           stderr=subprocess.STDOUT,timeout=1200)
    (out/(platform+"-process.txt")).write_bytes(
       "returncode=".encode()+str(started.returncode).encode()+b"\n"+started.stdout)
    if started.returncode!=0 or not xml.is_file():
     print("B01_60_W12_NATIVE_FAILED_OR_ACTIVATION_BLOCKED",platform,
           "returncode",started.returncode,"xml_exists",xml.exists())
     return 4
    # W16: verify original gameplay W06/#510 and QA fixture bytes are still
    # exact after Unity import; no foreign Assets code or metadata rewrite.
    require_native_source_integrity(project,combined)
    # W15: BOTH native suites must have their own valid original Editor.log
    # and all exact independent NUnit cases; never trust only the last suite.
    require_editor_version_log(log)
    real[platform]=parse_xml(xml,min_tests)
   require(set(real)=={"EditMode","PlayMode"},
           "NATIVE_NOT_VERIFIED: both original Unity EditMode/PlayMode suites required")
   (out/"native-verdict.json").write_text(json.dumps(
     {"native_unity":"EXECUTED","G1":"PASS","owner_sha":SHA,"results":real},
     sort_keys=True,indent=2)+"\n")
   print("B01_60_W12_NATIVE_EDIT_PLAY_PASS",real)
  finally:
   # Unity creates Library/Logs/Temp; delete the entire disposable project only.
   # This never changes the original repository or an external host project.
   pass
 return 0
if __name__=="__main__":
 try:sys.exit(main())
 except Exception as exc:
  print("B01_60_W12_GATE_FAIL",repr(exc),file=sys.stderr)
  sys.exit(2)
