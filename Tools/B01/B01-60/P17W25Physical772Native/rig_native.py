#!/usr/bin/env python3
"""B01-60 P17/W25 owner-opt-in Unity G1 for B01-23 exact #772 physical rig.

SOURCE ONLY by default. Reuses frozen #718 hardened ZIP/scratch/Unity process
runner; only original B01-23 Unity files from #772 are staged into a separate
new scratch project. #773 storage is NOT co-installed or credited as native.
"""
import argparse
import importlib.util
import json
import os
from pathlib import Path
import re
import secrets
import subprocess
import sys
import tempfile

ROOT=Path(__file__).resolve().parents[4]
W24="6a3589ff23bd811fb456d375e14b45c22b93337d"
RIG="ce46a5e0cc5d26df6c4ece723e8efe20d70fc4b1"
API="b8ca29c7707cd6b4338d0b9772d74dee676d49d2"
GAME="d94400d95d13db7f27e1533df05c9292d5380431"
SAVE="33094f154c7b91119b36d58aab285e6e83c5de57"
STORE_CANDIDATE="3aaaec4027ee246fbfb6f209179f0e9507363473"
LEGACY_SELECTED="8634e71dfcf633323073fe584a6c0884f1ca5d23"
BASE=ROOT/"Tools/B01/B01-60/P17W22R5PartialNative/r5_native.py"
PROJECT="ProjectSettings/ProjectSettings.asset"
NATIVE_FIXTURE="Assets/B0123CanonicalRigPortW24/Tests/PlayMode/CanonicalPhysicalRigPortW24NativeTests.cs"
PHYSICAL="Assets/B0123CanonicalRigPortW24/Runtime/CanonicalPhysicalRigPortW24.cs"
LEDGER="Assets/B0123CanonicalRigPortW24/Runtime/CanonicalRigLedgerHydratorW24.cs"
SCOPE=(
  "Assets/B0123DebrisRepulse001",
  "Assets/B0123SecureReattachV2",
  "Assets/B0123DetachedSaveContract001",
  "Assets/B0123CanonicalSaveApi001",
  "Assets/B0123CanonicalRigPortW24"
)
EXTRAS=("Packages/manifest.json","Packages/packages-lock.json",
        "ProjectSettings/ProjectVersion.txt",PROJECT)
PROBE="Assets/B0123CanonicalRigPortW24/Tests/PlayMode/B0160W25ScopeProbe.cs"
PROBE_NAME="B01.CanonicalRigPortW24.Tests.B0160W25ScopeProbe.OnlyPrivateSaveScope"
SUITE="B01.CanonicalRigPortW24.Tests.CanonicalPhysicalRigPortW24NativeTests."
METHODS=(
  "Original510AtomicLedger_RealRigidbodyDetachedAndAttachedParentAndVelocity",
  "InvalidOriginal556Binary_CannotCreateAnyUnityBodyOrConsumeId",
  "QuiescenceRequired_BeforeRealPhysicalStage",
)
PINS={
 NATIVE_FIXTURE:"6827351e8f578030de932b2d36a5c0c55337a2f7",
 PHYSICAL:"",LEDGER:"", # assigned by independent original Git blob identity call below
 "Assets/B0123CanonicalRigPortW24/Runtime/B0123.CanonicalRigPortW24.Runtime.asmdef":
    "301f9b7a240bcbe0221fd1f4e320a99c9b86c08d",
 "Assets/B0123CanonicalRigPortW24/Tests/PlayMode/B0123.CanonicalRigPortW24.PlayMode.asmdef":
    "1ccfc74cca91c41086a0bb7716d88febdbe996a7"
}
OWN_SRC="Tools/B01/B01-23/P17W24CanonicalPhysicalRigPort001/"
OWN_CI=".github/workflows/b01-23-p17w24-physical510-port.yml"

def need(ok,reason):
    if not ok:raise AssertionError("B0160_W25_HOLD: "+reason)
def git(*args):
    return subprocess.check_output(["git","-C",str(ROOT),*args],
                                   stderr=subprocess.PIPE).decode().strip()
def git_bytes(revision,path):
    return subprocess.check_output(["git","-C",str(ROOT),"show",revision+":"+path],
                                   stderr=subprocess.PIPE)
def original_module():
    spec=importlib.util.spec_from_file_location("b0160_w25_frozen_r5_receiver",BASE)
    need(spec is not None and spec.loader is not None,"missing frozen #718 original runner")
    m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
    need(m.R5=="cc7f73541517a18f4479bb155534bd4832d9985e",
         "wrong historical #718 original")
    need(git("rev-parse",W24+":"+BASE.relative_to(ROOT).as_posix())==
         "40ca11d886fc567a5a79f6abfeabdf2b37f4c36e",
         "frozen original R5 packager modified")
    return m
def selected_source(n):
    need(git("merge-base",W24,"HEAD")==W24,"W25 must descend exact frozen W24")
    need(git("cat-file","-t",RIG)=="commit","exact original B01-23 #772 commit missing (no auto fetch)")
    need(git("merge-base",API,RIG)==API,"physical port not descendant of selected #729")
    changed=set(git("diff","--name-only",API,RIG).splitlines())
    need(len(changed)==21 and all(
      p=="Assets/B0123CanonicalRigPortW24.meta" or
      p.startswith("Assets/B0123CanonicalRigPortW24/") or
      p.startswith(OWN_SRC) or p==OWN_CI
      for p in changed),"#772 original modifies unexpected assets/source; HOLD")
    for p,expected in PINS.items():
        actual=git("rev-parse",RIG+":"+p)
        need(len(actual)==40 and (not expected or expected==actual),
             "original #772 source blob mismatch: "+p)
    # Check frozen historical sources never modified in #772.
    for p,ref in (
        ("Assets/B0123CanonicalSaveApi001/Core/CanonicalWorldGateway.cs",API),
        ("Assets/B0123CanonicalSaveApi001/Core/CanonicalWorldSlotLease.cs",API),
        ("Assets/B0123SecureReattachV2/Runtime/AtomicDockLedger.cs",GAME),
        ("Assets/B0123SecureReattachV2/Runtime/SecureRigSpec.cs",GAME),
        ("Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs",SAVE)):
        need(git("rev-parse",ref+":"+p)==git("rev-parse",RIG+":"+p),
             "frozen base source changed: "+p)
    names=git("ls-tree","-r","--name-only",RIG).splitlines()
    source={}
    for p in names:
        if p in EXTRAS or any(
            p==root+".meta" or p.startswith(root+"/") for root in SCOPE):
            source[p]=git_bytes(RIG,p)
    need(len(source)>=30 and NATIVE_FIXTURE in source and PHYSICAL in source and
         LEDGER in source,"original Unity project selected-source closure incomplete")
    proj=json.loads(source["Packages/manifest.json"])
    need(proj["dependencies"].get("com.unity.test-framework")=="1.6.0" and
         "com.unity.modules.physics" in proj["dependencies"],
         "Unity UTF/PhysX manifest mismatch")
    need("6000.3.14f1" in source["ProjectSettings/ProjectVersion.txt"].decode(),
         "not the exact owner Unity Editor")
    cases=tuple(re.findall(rb"\[UnityTest\]\s*public\s+IEnumerator\s+(\w+)\s*\(",
                           source[NATIVE_FIXTURE]))
    need(tuple(s.decode() for s in cases)==METHODS,
         "original B01-23 three PlayMode cases changed")
    unity_meta=[p for p in source if p.startswith("Assets/B0123CanonicalRigPortW24")
                and p.endswith(".meta")]
    need(len(unity_meta)==9,"#772 original 9 Unity GUIDs missing")
    m=n.runner()
    need(m.guid_check(source)>=30,"missing Unity asset GUID closure")
    return source

def probe_bytes(product):
    need(bool(re.fullmatch(r"B0160W25Rig[0-2][0-9a-f]{12}",product)),
         "invalid distinct QA product scope")
    return ("""using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;
namespace B01.CanonicalRigPortW24.Tests {
 public sealed class B0160W25ScopeProbe {
  [Test] public void OnlyPrivateSaveScope() {
   string p=Application.persistentDataPath;
   Assert.IsTrue(p.IndexOf(\""""+product+"""\",StringComparison.OrdinalIgnoreCase)>=0,
       \"B0160_W25_PRIVATE_SCOPE_INVALID\");
   Assert.IsTrue(Path.IsPathRooted(Application.temporaryCachePath),
       \"B0160_W25_TEMP_PATH_INVALID\");
  }
 }
}
""").encode("utf-8")
def private_case(source,index,nonce,helper):
    need(0<=index<3 and re.fullmatch("[0-9a-f]{12}",nonce) is not None,
         "invalid per-project test identity")
    product="B0160W25Rig"+str(index)+nonce
    ps=source[PROJECT].decode("utf-8")
    for field,val in (("companyName","B0160W25"+nonce),("productName",product)):
        pattern=r"(?m)^([ \t]*"+field+r": )[^\r\n]+$"
        need(len(re.findall(pattern,ps))==1,"ambiguous Unity PlayerSettings "+field)
        ps=re.sub(pattern,lambda m:m.group(1)+val,ps,count=1)
    out=dict(source)
    out[PROJECT]=ps.encode("utf-8")
    need(PROBE not in out and PROBE+".meta" not in out,
         "QA-only temporary scope guard already collides")
    out[PROBE]=probe_bytes(product)
    out[PROBE+".meta"]=helper.meta(PROBE)
    need(helper.guid_check(out)==helper.guid_check(source)+1,
         "new QA-only probe Unity GUID collision")
    return out,product

def main(argv=None):
    ap=argparse.ArgumentParser()
    ap.add_argument("--mode",choices=("preflight","native"),default="preflight")
    ap.add_argument("--scratch-parent",default=os.environ.get("TEMP") or tempfile.gettempdir())
    ap.add_argument("--unity",default="")
    ap.add_argument("--operator-consent",default="")
    ap.add_argument("--source-g0-approved",action="store_true")
    args=ap.parse_args(argv)
    n=original_module();source=selected_source(n);helper=n.runner()
    # W12 is frozen and verifies original SHA, rebind ONLY in this ephemeral module
    # to the exact chosen B01-23 source. Never alter any on-disk historical source.
    need(helper.SHA==n.R5,"frozen verifier expected exact previous host")
    helper.SHA=RIG
    if args.mode=="native":
        need(os.name=="nt","physical native G1 is owner Windows only")
        need(args.source_g0_approved,"B01-40/41 independent #772 source G0 required")
        need(args.operator_consent=="B0160 W25 RIG NATIVE",
             "explicit owner typed consent missing")
        exe=Path(args.unity)
        need(exe.is_file() and re.search(
            r"[/\\]6000\.3\.14f1[/\\]Editor[/\\]Unity\.exe$",str(exe),re.I),
             "legitimate exact Unity Hub Editor missing")
        need(not exe.is_symlink(),"linked Editor denied")
    else:exe=None
    base=n.safe_root(args.scratch_parent)
    receipt={
       "package":"B01-60-P17W25-772-RIG-NATIVE-G1",
       "original_rig_SHA":RIG,"source_api_729_SHA":API,
       "original510_SHA":GAME,"original556_SHA":SAVE,
       "one_selected_749_successor_773_CANDIDATE_SHA":STORE_CANDIDATE,
       "old_selected_749_SHA":LEGACY_SELECTED,
       "native_G1":"NOT_RUN","product_source_G0":"PENDING_INDEPENDENT",
       "original_physical_port_existing_private_510":"NOT_WIRED",
       "canonical_773_save_unity_G1":"NOT_RUN",
       "economy_loaner_native_G1":"NOT_RUN","full_game_G2":"HOLD",
       "test_methods":list(METHODS),"merged":False,"uploaded":False,
       "codex_ack":False,"integrated":False}
    receipt_path=base/"P25-receipt.json"
    receipt_path.write_text(json.dumps(receipt,sort_keys=True,indent=2)+"\n")
    try:
        for i,method in enumerate(METHODS):
            staged,product=private_case(source,i,secrets.token_hex(6),helper)
            original_receipt={
              "original_source_sha":RIG,"native_unity":"NOT_RUN",
              "source_only_preflight":True,"integrated":False,"codex_ack":False,
              "native_meta_guids":helper.guid_check(staged),
              "file_sha256":{p:n.blake(b) for p,b in sorted(staged.items())}}
            data=helper.zip_data(staged,original_receipt)
            project=base/("P"+str(i))
            helper.stage(data,staged,project)
            need((project/PROJECT).read_bytes()==staged[PROJECT],
                 "private Unity PlayerSettings bytes drift")
            if args.mode=="native":
                n.run_editor(exe,project,base,"guard"+str(i),PROBE_NAME,staged,helper)
                n.run_editor(exe,project,base,"rigcase"+str(i),SUITE+method,staged,helper)
            else:
                helper.rollback(data,staged,project)
        if args.mode=="preflight":
            receipt["source_preflight"]="PASS_3_GIT_EXACT_PRIVATE_PROJECT_STAGES_ROLLED_BACK"
            print("B0160_W25_RIG_772_THREE_PRIVATE_SCRATCH_PROJECTS_SOURCE_PASS")
            print("B0160_W25_NATIVE_G1_NOT_RUN")
        else:
            receipt["native_G1"]="THREE_RIG_CASES_EXECUTED_INDEPENDENT_REVIEW_PENDING"
            receipt["operator_consent"]=True
            print("B0160_W25_RIG_772_REAL_NUNIT_EDITOR_EVIDENCE_INDEPENDENT_HOLD")
        receipt_path.write_text(json.dumps(receipt,sort_keys=True,indent=2)+"\n")
        (base/"P25-sha256-manifest.json").write_text(json.dumps(
            n.manifest_files(base,receipt),sort_keys=True,indent=2)+"\n")
        print("B0160_W25_LOCAL_NO_UPLOAD="+str(base))
        return 0
    except Exception as ex:
        receipt["error"]=str(ex)
        receipt_path.write_text(json.dumps(receipt,sort_keys=True,indent=2)+"\n")
        print("B0160_W25_G1_HOLD="+str(ex),file=sys.stderr)
        print("B0160_W25_LOCAL_NO_UPLOAD="+str(base))
        return 4 if args.mode=="native" else 2
if __name__=="__main__":
    try:sys.exit(main())
    except Exception as ex:
        print("B0160_W25_PRECHECK_HOLD="+repr(ex),file=sys.stderr)
        sys.exit(3 if "--mode" in sys.argv and "native" in sys.argv else 2)
