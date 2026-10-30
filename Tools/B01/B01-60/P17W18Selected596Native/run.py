#!/usr/bin/env python3
"""B01-60 W18: native Unity launcher for FOUR original selected #596 PlayMode cases.

Native evidence is NOT_RUN unless a user-supplied, legally activated real
Unity 6000.3.14f1 Editor actually executes each ORIGINAL case AND an independent
per-project checkpoint isolation probe, plus the existing 16 #532 QA tests.

NO producer modifications, mixed #604 sources, Codex integration or G2 release.
"""
from __future__ import annotations
import argparse
import hashlib
import importlib.util
import json
import os
from pathlib import Path, PurePosixPath
import re
import secrets
import subprocess
import sys
import tempfile
import xml.etree.ElementTree as ET

ROOT=Path(__file__).resolve().parents[4]
GAME="d94400d95d13db7f27e1533df05c9292d5380431"
SAVE="33094f154c7b91119b36d58aab285e6e83c5de57"
ONLY="efb2b06eafedab5469b57f163152d96aca3c5915"
ALT="1aa8a59ce1bb4fcd998f0b2b1ce25dfaea333f0a"
BASE="f4e26ac21ade9a54c0c38927ee48cea50ed8377c"
EXISTING="Tools/B01/B01-60/P17W12NativeSecureV2QA/runner.py"
QA_BASE="Tools/B01/B01-60/P17W18Selected596Native/"
QA_PROBE="Assets/B0123UnitySaveBridge001/Tests/B0160W18PrivateCheckpointProbe.cs"
PS="ProjectSettings/ProjectSettings.asset"
GUARD_FULL="B01.UnitySaveBridge001.Tests.B0160W18PrivateCheckpointProbe.CleanPrivateCheckpointScope"
SUITE="B01.UnitySaveBridge001.Tests.IsolatedSaveWorldNativePlayModeTests."
METHODS=(
 "ColdLoad_RealRigidbody_RoundTripLightHeavy",
 "CorruptLoad_LeavesPreviouslyActiveRigidbodyAndIdsUntouched",
 "ColdRestart_RestoresRealRigidbodyAndConsumedIds",
 "WrongGeneration_CannotPartiallyPublishRealPhysicalWorld",
)
SCOPE=("Assets/B0123DebrisRepulse001","Assets/B0123SecureReattachV2",
       "Assets/B0123DetachedSaveContract001","Assets/B0123UnitySaveBridge001")
META_EXTRA=("Packages/manifest.json","Packages/packages-lock.json",
            "ProjectSettings/ProjectVersion.txt",PS)
EXACT_SOURCE_BLOBS={
 "Assets/B0123SecureReattachV2/Runtime/AtomicDockLedger.cs":"b6a4d04b0c6407c8c84451b4d599d96fe0e4d1a9",
 "Assets/B0123SecureReattachV2/Runtime/SecureDebrisPlaygroundV2.cs":"328977b4cdcdcd7c9f128c01f11ff67f8e46086a",
 "Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs":"b52e511c370f5fca56c74029050844dc8cbb7f9a",
 "Assets/B0123UnitySaveBridge001/Unity/IsolatedTwinRigSaveHost.cs":"4bac6f327c9b93ae98dd2061c0ca8ea2100aab42",
 "Assets/B0123UnitySaveBridge001/Tests/IsolatedSaveWorldNativePlayModeTests.cs":"1ad6239b8e4e932dc1b87bb355d225867b2f4a4b",
}
W17={
 "Tools/B01/B01-60/P17W17NativeAdapterCoverage/audit_native_adapter_coverage.py",
 "Tools/B01/B01-60/P17W17NativeAdapterCoverage/README.md",
 "Tools/B01/B01-60/P17W17NativeAdapterCoverage/manifest.json",
 ".github/workflows/b01-60-p17w17-native-adapter-coverage.yml",
}
W18={
 QA_BASE+"run.py",QA_BASE+"test_preflight.py",
 QA_BASE+"README.md",QA_BASE+"manifest.json",
 ".github/workflows/b01-60-p17w18-selected596-native-isolated.yml",
}
def git(*args):
 return subprocess.check_output(["git","-C",str(ROOT),*args],stderr=subprocess.PIPE)
def require(cond,msg):
 if not cond:raise AssertionError(msg)
def h(raw):
 return hashlib.sha256(raw).hexdigest()
def load_w12():
 spec=importlib.util.spec_from_file_location("b0160_existing_safe_runner_w18",ROOT/EXISTING)
 require(spec is not None and spec.loader is not None,"original #532 native runner missing")
 m=importlib.util.module_from_spec(spec)
 spec.loader.exec_module(m)
 m.OWN.update(W17|W18)  # only own new QA files; old runner unchanged
 return m
def source_scope_preflight():
 require(git("merge-base",BASE,"HEAD").decode().strip()==BASE,
         "B01-60 W18 not based on exact previous OWN QA #621")
 require(git("merge-base",GAME,"HEAD").decode().strip()==GAME,
         "W18 B01-60 not based on selected #510 owner source")
 for sha in (GAME,SAVE,ONLY,ALT):
  git("cat-file","-e",sha+"^{commit}")
 require(set(git("diff","--name-only",BASE,"HEAD").decode().splitlines())==W18,
         "W18 changed non-owned path or missing own required file")
 # These path sets can be compared without extracting either competing producer.
 require(git("rev-parse",ONLY+":Assets/B0123UnitySaveBridge001/Core/AtomicWorldSwap.cs")!=
         git("rev-parse",ALT+":Assets/B0123UnitySaveBridge001/Core/AtomicWorldSwap.cs"),
         "#596/#604 source unexpectedly identical; mandatory provenance re-audit")
 for path,sha in EXACT_SOURCE_BLOBS.items():
  require(git("rev-parse",ONLY+":"+path).decode().strip()==sha,
          "immutable sole selected #596 Git blob changed "+path)
 for path in ("Assets/B0123SecureReattachV2/Runtime/AtomicDockLedger.cs",
              "Assets/B0123SecureReattachV2/Runtime/SecureDebrisPlaygroundV2.cs"):
  require(git("rev-parse",GAME+":"+path)==git("rev-parse",ONLY+":"+path),
          "frozen original #510 source mismatch "+path)
 path="Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs"
 require(git("rev-parse",SAVE+":"+path)==git("rev-parse",ONLY+":"+path),
         "frozen only #556 Save schema source mismatch")
 require(git("cat-file","-e",ONLY+":ProjectSettings/ProjectSettings.asset")==b"",
         "Unity PlayerSettings missing at selected producer source")
 return True
def owner_original():
 source_scope_preflight()
 paths=git("ls-tree","-r","--name-only",ONLY).decode().splitlines()
 own={}
 for path in paths:
  if path in META_EXTRA or any(
     path==prefix+".meta" or path.startswith(prefix+"/") for prefix in SCOPE):
   require(path not in own,"duplicate selected package file")
   own[path]=git("show",ONLY+":"+path)
 require(len(own)>35,"selected #596 original runtime or Unity meta closure missing")
 require(all(path in own for path in EXACT_SOURCE_BLOBS),
         "selected #596 closure missing critical source")
 require(PS in own,"original Unity company/product settings missing")
 require(not any("CheckpointFence.cs" in p for p in own),
         "unselected #604 CheckpointFence.cs mixed into frozen #596")
 # The selected #596 NUnit tests are physically authored, but not executed here.
 raw=own["Assets/B0123UnitySaveBridge001/Tests/IsolatedSaveWorldNativePlayModeTests.cs"]
 names=re.findall(rb"\[UnityTest\]\s*public\s+IEnumerator\s+([A-Za-z_]\w*)\s*\(",raw)
 require(tuple(x.decode() for x in names)==METHODS,
         "source exact FOUR selected #596 NUnit case identities changed")
 return own
def private_settings(original,company,product):
 require(re.fullmatch(r"B0160W18[A-Za-z0-9]{8,28}",company)!=None and
         re.fullmatch(r"B0160Case[A-Za-z0-9]{8,30}",product)!=None,
         "private Unity test PlayerSettings invalid")
 src=original.decode("utf-8")
 # Pinned original #596 project settings, ONLY isolated disposable copies patched.
 company_pat=r"(?m)^([ \t]*companyName: )[^\r\n]+$"
 product_pat=r"(?m)^([ \t]*productName: )[^\r\n]+$"
 require(len(re.findall(company_pat,src))==1 and
         len(re.findall(product_pat,src))==1,
         "missing/ambiguous ORIGINAL Unity companyName/productName")
 src=re.sub(company_pat,lambda match:match.group(1)+company,src,count=1)
 src=re.sub(product_pat,lambda match:match.group(1)+product,src,count=1)
 return src.encode()
def probe_source(product):
 require(re.fullmatch(r"B0160Case[A-Za-z0-9]{8,30}",product)!=None,
         "invalid private test product")
 return ('''using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;
namespace B01.UnitySaveBridge001.Tests {
 public sealed class B0160W18PrivateCheckpointProbe {
  [Test]
  public void CleanPrivateCheckpointScope() {
   string path=Application.persistentDataPath;
   Assert.IsTrue(path.IndexOf("'''+product+'''",StringComparison.OrdinalIgnoreCase)>=0,
      "NATIVE_G1_HOLD: Unity persistentDataPath is NOT private W18 product; do not run host tests");
   Assert.IsFalse(File.Exists(Path.Combine(path,"b0123_one_save_host_v2.checkpoint")),
      "NATIVE_G1_HOLD: private QA checkpoint unexpectedly already exists");
  }
 }
}
''').encode()
def isolated_case_files(base,case,nonce,m):
 require(case in METHODS,"unknown original #596 native test")
 index=METHODS.index(case)
 company="B0160W18"+nonce[:12]
 product="B0160Case"+str(index)+nonce
 require(product!=("RoboRougelike"),"refuse canonical game checkpoint")
 result=dict(base)
 result[PS]=private_settings(base[PS],company,product)
 result[QA_PROBE]=probe_source(product)
 result[QA_PROBE+".meta"]=m.meta(QA_PROBE)
 require(len(result)==len(base)+2,"accidental overwritten owner Source")
 m.guid_check(result)
 return result,company,product
def validate_process_xml(xml,full):
 require(xml.is_file(),"G1_NATIVE_FAIL: genuine Unity original NUnit XML missing")
 root=ET.parse(xml).getroot()
 require(root.tag=="test-run" and root.get("result")=="Passed" and
         int(root.get("failed","-1"))==0 and int(root.get("passed","0"))>=1,
         "G1_NATIVE_FAIL: original Unity NUnit aggregate failed/absent")
 cases=list(root.iter("test-case"))
 require(len(cases)==1,"G1_NATIVE_FAIL: expected EXACTLY ONE filtered original NUnit case")
 found=cases[0].get("fullname","").replace(" ","")
 require(found==full.replace(" ","") and cases[0].get("result")=="Passed",
         "G1_NATIVE_FAIL: wrong/absent/failed original native NUnit identity "+repr(found))
 return {"verified":full,"xml":str(xml),"result":"Passed"}
def run_real_editor(unity,project,out,label,platform,testcase,files,m):
 xml=out/(label+".xml")
 log=out/(label+"-Editor.log")
 command=[str(unity),"-batchmode","-nographics","-quit",
          "-projectPath",str(project),"-runTests",
          "-testPlatform",platform,"-testFilter",testcase,
          "-testResults",str(xml),"-logFile",str(log)]
 result=subprocess.run(command,stdout=subprocess.PIPE,
                       stderr=subprocess.STDOUT,timeout=1200)
 (out/(label+"-process.txt")).write_bytes(
   ("returncode="+str(result.returncode)+"\n").encode()+result.stdout)
 require(result.returncode==0,
         "G1_NATIVE_FAIL: Unity process failed/activation blocked "+label)
 m.require_native_source_integrity(project,files)
 require((project/PS).is_file() and (project/PS).read_bytes()==files[PS],
         "G1_NATIVE_FAIL: modified private Unity PlayerSettings")
 m.require_editor_version_log(log)
 return validate_process_xml(xml,testcase)
def validate_project_source(project,items,m):
 m.require_native_source_integrity(project,items)
 require((project/PS).read_bytes()==items[PS],
         "G1_SOURCE_FAIL: changed injected isolated Unity PlayerSettings")
 return True
def main():
 p=argparse.ArgumentParser()
 p.add_argument("--mode",choices=("preflight","native"),default="preflight")
 p.add_argument("--unity",help="absolute legally activated Unity 6000.3.14f1 executable")
 p.add_argument("--out",default=str(ROOT/"qa-w18-selected596-native-evidence"))
 args=p.parse_args()
 dest=Path(args.out).expanduser().resolve()
 require(not dest.exists(),"W18 evidence already exists; never overwrite")
 m=load_w12()
 base=owner_original()
 # Exact original Unity project and all original immutable #596 sources, not #604.
 guid_count=m.guid_check(base)
 token=secrets.token_hex(8)
 # Do not write outside dedicated QA output; never touch actual user saves.
 dest.mkdir(parents=True)
 manifest={
  "owner":"B01-60","package":"P17W18Selected596Native",
  "game_SHA":GAME,"save_SHA":SAVE,"sole_bridge_SHA":ONLY,
  "unselected_ALT_SHA":ALT,"W17_QA_base":BASE,
  "original_file_count":len(base),"original_meta_GUIDs":guid_count,
  "original_source_sha256":{k:h(v) for k,v in sorted(base.items())},
  "Unity6000_3_14f1":"NOT_RUN","native_G1":"NOT_RUN","G2":"HOLD",
  "canonical_live_save":"HOLD","Codex_ACK":"NONE","merged":False,
  "original_selected_native_test_methods_authored":METHODS,
  "case_scope_isolation":"distinct QA-only company/product per case; guard must execute before selected native case",
 }
 (dest/"receipt.json").write_text(json.dumps(manifest,indent=2,sort_keys=True)+"\n")
 with tempfile.TemporaryDirectory(prefix="b0160-w18-isolated596-") as temp:
  root=Path(temp)
  for case in METHODS:
   files,company,product=isolated_case_files(base,case,token,m)
   label=METHODS.index(case)
   archive=m.zip_data(files,{"owner":"B01-60","source_SHA":ONLY,
                             "native":"NOT_RUN","private_company":company,
                             "private_product":product,"case":case})
   project=root/("selected-private-"+str(label))
   m.stage(archive,files,project)
   validate_project_source(project,files,m)
   # Source-only preflight can physically stage/rollback complete original #596
   # without launching an Editor or altering any external Unity project.
   if args.mode=="preflight":
    m.rollback(archive,files,project)
  print("B01_60_W18_SELECTED_596_ORIGINAL_SOURCE_SCOPE_PASS",len(base))
  print("B01_60_W18_PRIVATE_PROJECTS_SAFE_PHYSICAL_STAGING_PASS",len(METHODS))
  print("B01_60_W18_WILL_REQUIRE_EXACT_596_FOUR_NATIVE_CASES_AND_4_PROBES")
  if args.mode=="preflight":
   print("B01_60_W18_G1_NOT_RUN_SOURCE_PREFLIGHT_ONLY")
   return 0
  unity=Path(args.unity).expanduser() if args.unity else None
  if unity is None or not unity.is_file():
   print("B01_60_W18_G1_NOT_RUN_NO_AUTHORIZED_ACTIVATED_UNITY_EDITOR")
   return 3
  # The four separate projects have different private application product IDs.
  # Test runner may run ONLY the explicit per-case isolation guard first.
  native={}
  try:
   for case in METHODS:
    idx=METHODS.index(case)
    files,company,product=isolated_case_files(base,case,token,m)
    project=root/("selected-private-"+str(idx))
    probe=run_real_editor(unity,project,dest,"guard-"+str(idx),"PlayMode",
                          GUARD_FULL,files,m)
    native["guard-"+str(idx)]=probe
    accepted=run_real_editor(unity,project,dest,"selected-"+str(idx),"PlayMode",
                              SUITE+case,files,m)
    native["selected-"+str(idx)]=accepted
  except Exception as exc:
   (dest/"native-failure.txt").write_text(repr(exc)+"\n")
   print("B01_60_W18_NATIVE_G1_HOLD",repr(exc),file=sys.stderr)
   return 4
  # This is the selected isolated bridge's FOUR physical tests + guards;
  # original #532's independent 16 tests are a SEPARATE, still required gate.
  (dest/"selected-bridge-native-verdict.json").write_text(json.dumps({
    "selected_SHA":ONLY,"source_original":GAME,"save_original":SAVE,
    "selected_host_native_cases":4,"private_checkpoint_guards":4,
    "Unity6000_3_14f1":"EXECUTED_SELECTED_ONLY",
    "selected_native":"PASS_SELECTED_ONLY",
    "original_independent_532_native":"REQUIRED_SEPARATELY",
    "canonical_game_native_G1":"NOT_VERIFIED_BY_SELECTED_BRIDGE_ALONE",
    "G2":"HOLD","merged":False,"results":native,
  },indent=2,sort_keys=True)+"\n")
  print("B01_60_W18_NATIVE_SELECTED_596_FOUR_TESTS_AND_GUARDS_PASS_NEEDS_ORIGINAL_532_G1")
  return 0
if __name__=="__main__":
 try:sys.exit(main())
 except Exception as exc:
  print("B01_60_W18_SOURCE_OR_NATIVE_FAIL",repr(exc),file=sys.stderr)
  sys.exit(2)
