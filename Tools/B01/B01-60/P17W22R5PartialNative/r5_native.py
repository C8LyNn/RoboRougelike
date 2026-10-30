#!/usr/bin/env python3
"""B01-60 P17/W22: versioned original R5 #690 native receiver.

Native means *real Unity* only if user explicitly approves a local invocation.
The original R5 ColdRestart test expects gen4 after a Save that emits gen5;
that test is NOT silently changed, skipped for release, or called PASS.
The optional partial 6-case run is NOT a complete R5 G1 acceptance.
"""
import argparse
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import re
import secrets
import subprocess
import sys
import tempfile
import xml.etree.ElementTree as ET

ROOT=Path(__file__).resolve().parents[4]
W21="02272e8b0facc00d3943672770251b1d01182b85"
R5="cc7f73541517a18f4479bb155534bd4832d9985e"
GAME="d94400d95d13db7f27e1533df05c9292d5380431"
SAVE="33094f154c7b91119b36d58aab285e6e83c5de57"
W06="cb2b3a76e84266e2730fffc4ab048c1039f0652f"
W12="Tools/B01/B01-60/P17W12NativeSecureV2QA/runner.py"
PS="ProjectSettings/ProjectSettings.asset"
FIXTURE="Assets/B0123UnitySaveBridge001/Tests/IsolatedSaveWorldNativePlayModeTests.cs"
CORE="Assets/B0123UnitySaveBridge001/Core/AtomicWorldSwap.cs"
HOST="Assets/B0123UnitySaveBridge001/Unity/IsolatedTwinRigSaveHost.cs"
RETIRE="Assets/B0123UnitySaveBridge001/Core/OwnerWorldLeaseRetirement.cs"
CODEC="Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs"
BLOBS={FIXTURE:"c8c603451a0668d50ba3af91ed888a64910bdc9f",
       CORE:"7da9a9efe489cf8c441d7bb9a47ba3f60ba4c58c",
       HOST:"6b3936d82b5e1f175856a9e138124b14c9dfd19c",
       RETIRE:"1ef69de9b4029ce89ba2db5bf3331708da1b91e4",
       CODEC:"b52e511c370f5fca56c74029050844dc8cbb7f9a"}
METHODS=(
 "ColdLoad_RealRigidbody_RoundTripLightHeavy",
 "CorruptLoad_LeavesPreviouslyActiveRigidbodyAndIdsUntouched",
 "ColdRestart_RestoresRealRigidbodyAndConsumedIds",
 "WrongGeneration_CannotPartiallyPublishRealPhysicalWorld",
 "SeparateRunSlots_CannotPoisonLaterRunOrReuseForeignOwner",
 "ComponentOnlyDestroy_RetainsLeaseUntilRealChildRigidbodyDestruction",
 "DisabledSameSlotHost_CannotStealOwnerUntilDestroyed")
HOLD_METHOD="ColdRestart_RestoresRealRigidbodyAndConsumedIds"
PARTIAL=tuple(n for n in METHODS if n!=HOLD_METHOD)
SCOPE=("Assets/B0123DebrisRepulse001","Assets/B0123SecureReattachV2",
       "Assets/B0123DetachedSaveContract001","Assets/B0123UnitySaveBridge001")
OTHER=("Packages/manifest.json","Packages/packages-lock.json",
       "ProjectSettings/ProjectVersion.txt",PS)
QA_PROBE="Assets/B0123UnitySaveBridge001/Tests/B0160W22ScopeProbe.cs"
PROBE="B01.UnitySaveBridge001.Tests.B0160W22ScopeProbe.OnlyPrivateSavePath"
SUITE="B01.UnitySaveBridge001.Tests.IsolatedSaveWorldNativePlayModeTests."
OWNED=("Tools/B01/B01-60/P17W22R5PartialNative/","qa/b01-60-p17w22-r5-native-partial-safety-r1")

def need(ok,why):
 if not ok:raise AssertionError(why)
def git(*args):
 return subprocess.check_output(["git","-C",str(ROOT),*args],stderr=subprocess.PIPE).decode().strip()
def blake(raw):
 return hashlib.sha256(raw).hexdigest()
def runner():
 path=ROOT/W12
 spec=importlib.util.spec_from_file_location("b0160_original_hardened_w12",path)
 need(spec and spec.loader,"Frozen #532 runner missing")
 module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module)
 need(module.SHA==GAME and module.RECEIPT=="B0160_W12_SOURCE_PROVENANCE.json",
      "Historical native runner changed identity")
 need(git("rev-parse",W21+":"+W12)=="99003aa6a1a07f3bfde0aaa98b91a06159efae18",
      "Historic source runner original blob changed")
 module.SHA=R5  # local imported module only, NEVER modify historical disk source
 return module
def selected_source():
 need(git("merge-base",W21,"HEAD")==W21,"W22 must descend sole W21 #711")
 need(git("cat-file","-t",R5)=="commit","R5 #690 Git commit missing locally: never network fetch in operator")
 need(git("merge-base",GAME,R5)==GAME and git("merge-base",SAVE,R5)==SAVE,
      "R5 is not a descendant of original #510/#556 sources")
 for path,blob in BLOBS.items():
  need(git("rev-parse",R5+":"+path)==blob,"Selected R5 original Git blob changed: "+path)
 names=git("ls-tree","-r","--name-only",R5).splitlines()
 selected={}
 for p in names:
  if p in OTHER or any(p==pref+".meta" or p.startswith(pref+"/") for pref in SCOPE):
   selected[p]=subprocess.check_output(["git","-C",str(ROOT),"show",R5+":"+p],stderr=subprocess.PIPE)
 need(len(selected)>35 and all(k in selected for k in BLOBS),
      "R5 exact Unity source closure incomplete")
 fixtures=re.findall(rb"\[UnityTest\]\s*public\s+IEnumerator\s+(\w+)\s*\(",
                    selected[FIXTURE])
 need(tuple(k.decode() for k in fixtures)==METHODS,"Original R5 seven NUnit names changed")
 fixture=selected[FIXTURE].decode("utf-8")
 begin=fixture.index("public IEnumerator "+HOLD_METHOD+"()")
 end=fixture.index("public IEnumerator WrongGeneration_",begin)
 cold=fixture[begin:end]
 core=selected[CORE].decode("utf-8")
 # Confirm the original native fixture itself cannot pass the R5 source semantics.
 need("MakeFixture(41,4,true,false)" in cold and
      "Assert.IsTrue(original.TrySaveBytes(out byte[] saved))" in cold and
      "Assert.AreEqual(4,restored.Generation)" in cold and
      "long next=newest+1;" in core and
      "new RunSnapshot(snap.RunId,next,snap.Parts)" in core,
      "Historical ColdRestart generation witness changed: re-audit before native")
 manifest=json.loads(selected["Packages/manifest.json"])
 need(manifest["dependencies"].get("com.unity.test-framework")=="1.6.0",
      "Original R5 Unity Test Framework unexpectedly changed")
 need(manifest["dependencies"].get("com.unity.modules.physics")!=None,"Original Physics module absent")
 need("6000.3.14f1" in selected["ProjectSettings/ProjectVersion.txt"].decode(),
      "Wrong Unity Editor version")
 need(len({k for k in selected if k.endswith(".meta")})>=34,"Incomplete R5 GUID closure")
 return selected
def probe_source(product):
 need(bool(re.fullmatch(r"B0160W22Case[0-9][0-9a-f]{12}",product)),"Wrong private player product")
 return ('''using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;
namespace B01.UnitySaveBridge001.Tests {
 public sealed class B0160W22ScopeProbe {
  [Test] public void OnlyPrivateSavePath() {
   string actual=Application.persistentDataPath;
   Assert.IsTrue(actual.IndexOf("'''+product+'''",StringComparison.OrdinalIgnoreCase)>=0,
      "B0160_R5_PRIVATE_SCOPE_FAILED");
   Assert.IsFalse(File.Exists(Path.Combine(actual,"b0123_one_save_host_v2.checkpoint")),
      "B0160_R5_CHECKPOINT_SCOPE_NOT_CLEAN");
  }
 }
}
''').encode()
def private_source(original,index,nonce,m):
 product="B0160W22Case"+str(index)+nonce
 need(product!="RoboRougelike","Canonical game product forbidden")
 need(len(nonce)==12,"R5 nonce must be 12 hex digits")
 src=original[PS].decode("utf-8")
 for field,value in (("companyName","B0160W22"+nonce),
                     ("productName",product)):
  pat=r"(?m)^([ \t]*"+field+r": )[^\r\n]+$"
  need(len(re.findall(pat,src))==1,"Ambiguous Unity PlayerSettings "+field)
  src=re.sub(pat,lambda k:k.group(1)+value,src,count=1)
 new=dict(original)
 new[PS]=src.encode("utf-8")
 need(QA_PROBE not in new and QA_PROBE+".meta" not in new,
      "QA-only probe collides with actual R5 source")
 new[QA_PROBE]=probe_source(product)
 new[QA_PROBE+".meta"]=m.meta(QA_PROBE)
 need(m.guid_check(new)==m.guid_check(original)+1,"GUID collision or non-unique new scope")
 return new,product
def safe_root(parent):
 p=Path(parent).expanduser().resolve(strict=True)
 repo=ROOT.resolve()
 need(p.is_dir() and not p.is_symlink(),"Invalid scratch root")
 need(p!=repo and p not in repo.parents and repo not in p.parents,"Scratch overlaps original checkout")
 need(not str(p).startswith("\\\\"),"UNC scratch refused")
 for x in (p,*p.parents):
  need(not x.is_symlink(),"Symlink scratch ancestor")
 child=p/("B22-"+secrets.token_hex(6))
 need(not child.exists(),"Refuse previous scratch directory")
 child.mkdir()
 return child
def exact_xml(path,fullname):
 need(path.is_file(),"Unity original NUnit XML missing")
 root=ET.parse(path).getroot()
 need(root.tag=="test-run" and root.get("result")=="Passed" and
      int(root.get("failed","-1"))==0 and int(root.get("passed","-1"))==1,
      "Unity original NUnit XML not exactly one passing test")
 tests=list(root.iter("test-case"))
 need(len(tests)==1 and tests[0].get("fullname","").replace(" ","")==fullname.replace(" ","")
      and tests[0].get("result")=="Passed",
      "Wrong or duplicated selected original R5 NUnit case")
def run_editor(exe,project,root,tag,filter_name,selected,m):
 xml=root/(tag+".xml")
 log=root/(tag+"-Editor.log")
 cmd=[str(exe),"-batchmode","-nographics","-quit",
      "-projectPath",str(project),"-runTests",
      "-testPlatform","PlayMode","-testFilter",filter_name,
      "-testResults",str(xml),"-logFile",str(log)]
 completed=subprocess.run(cmd,stdout=subprocess.PIPE,
                          stderr=subprocess.STDOUT,timeout=1200)
 (root/(tag+"-process.log")).write_bytes(
     ("exit_code="+str(completed.returncode)+"\n").encode()+completed.stdout)
 need(completed.returncode==0,"Unity executable or activation/test returned nonzero")
 m.require_editor_version_log(log)
 exact_xml(xml,filter_name)
 m.require_native_source_integrity(project,selected)
 need((project/PS).read_bytes()==selected[PS],"Unity PlayerSettings mutated during test")
 return completed.returncode
def manifest_files(root,receipt):
 files={}
 for f in root.rglob("*"):
  if f.is_file() and not f.is_symlink() and not any(
     part in ("Library","Temp","Logs","obj") for part in f.relative_to(root).parts):
   rel=f.relative_to(root).as_posix()
   if rel.startswith("P") or rel.endswith(".xml") or rel.endswith(".log"):
    files[rel]=blake(f.read_bytes())
 return {"unsigned":True,"note":"SHA256 consistency, not native attestation",
         "receipt":receipt,"evidence_sha256":files}
def main(argv=None):
 a=argparse.ArgumentParser()
 a.add_argument("--mode",choices=("preflight","native"),default="preflight")
 a.add_argument("--unity",default="")
 a.add_argument("--scratch-parent",default=os.environ.get("TEMP") or tempfile.gettempdir())
 a.add_argument("--operator-consent",default="")
 args=a.parse_args(argv)
 m=runner()
 items=selected_source()
 need(m.guid_check(items)>=34,"R5 original project GUID count insufficient")
 if args.mode=="native":
  need(os.name=="nt","Real native R5 operator only permitted on local Windows")
  need(args.operator_consent=="B0160 R5 NATIVE PARTIAL","No typed manual operator consent")
  u=Path(args.unity)
  need(u.is_file() and re.search(r"[/\\]6000\.3\.14f1[/\\]Editor[/\\]Unity\.exe$",
                                  str(u),flags=re.I),"Exact Unity Hub 6000.3.14f1 executable required")
 else:u=None
 root=safe_root(args.scratch_parent)
 receipt={"package":"B01-60-P17W22-R5-NATIVE-PARTIAL","selected_host_R5_SHA":R5,
          "original_510_SHA":GAME,"original_556_SHA":SAVE,
          "native_G1":"NOT_RUN","R5_FULL_NATIVE_G1":"HOLD_ORIGINAL_COLDRESTART_GEN4_EXPECTS_GEN5",
          "source_G0":"B01-40_699_SOURCE_ONLY","Codex_ACK":"NONE",
          "source_methods":METHODS,"native_partial_scope":PARTIAL,
          "merged":False,"uploaded":False}
 (root/"P22-receipt.json").write_text(json.dumps(receipt,sort_keys=True,indent=2)+"\n")
 try:
  for i,method in enumerate(PARTIAL):
   scope,product=private_source(items,i,secrets.token_hex(6),m)
   qa_receipt={"original_source_sha":R5,"native_unity":"NOT_RUN",
      "source_only_preflight":True,"codex_ack":False,"integrated":False,
      "native_meta_guids":m.guid_check(scope),
      "file_sha256":{k:blake(v) for k,v in sorted(scope.items())}}
   archive=m.zip_data(scope,qa_receipt)
   project=root/("P"+str(i))
   m.stage(archive,scope,project)
   need((project/PS).read_bytes()==scope[PS],"Physical stage private PlayerSettings changed")
   if args.mode=="native":
    run_editor(u,project,root,"guard"+str(i),PROBE,scope,m)
    run_editor(u,project,root,"r5case"+str(i),SUITE+method,scope,m)
   else:
    m.rollback(archive,scope,project)
  if args.mode=="preflight":
   receipt["source_preflight"]="PASS_6_STAGE_ROLLBACK_NO_UNITY"
   print("B0160_W22_R5_PINNED_SEVEN_ORIGINAL_NUNIT_CASES_AND_34_PLUS_GUIDS_PASS")
   print("B0160_W22_R5_SIX_PRIVATE_PROJECTS_STAGE_ROLLBACK_PASS")
   print("B0160_W22_ORIGINAL_COLD_RESTART_GEN4_VS_GEN5_FULL_G1_HOLD")
   print("B0160_W22_G1_NOT_RUN_SOURCE_ONLY")
  else:
   receipt["native_G1"]="PARTIAL_R5_SIX_OF_SEVEN_ONLY"
   print("B0160_W22_REAL_UNITY_SIX_R5_CASES_ONLY_NOT_FULL_G1")
  (root/"P22-receipt.json").write_text(json.dumps(receipt,sort_keys=True,indent=2)+"\n")
  (root/"P22-sha256-manifest.json").write_text(json.dumps(
      manifest_files(root,receipt),sort_keys=True,indent=2)+"\n")
  print("B0160_W22_LOCAL_EVIDENCE_NO_UPLOAD="+str(root))
  return 0
 except Exception as exc:
  receipt["source_or_native_error"]=str(exc)
  (root/"P22-receipt.json").write_text(json.dumps(receipt,sort_keys=True,indent=2)+"\n")
  print("B0160_W22_FAIL_CLOSED="+str(exc),file=sys.stderr)
  print("B0160_W22_LOCAL_EVIDENCE_NO_UPLOAD="+str(root))
  return 4 if args.mode=="native" else 2
if __name__=="__main__":
 try:sys.exit(main())
 except Exception as exc:
  print("B0160_W22_PREFLIGHT_HOLD="+repr(exc),file=sys.stderr)
  sys.exit(3 if "--mode" in sys.argv and "native" in sys.argv else 2)
