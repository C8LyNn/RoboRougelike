#!/usr/bin/env python3
"""B01-60 P17/W25 original #772 real Unity PlayMode operator, source-only default.

Reuses original frozen B01-60 #718/#751 W12 hardened source staging, strict
rollback and per-case native NUnit/Editor.log checks; no new competing runner
authority. Isolates three exact original #772 tests in three separate Unity6
private-checkpoint scoped projects. Canonical #749/#773 Tools-only Save has NO
author-approved Unity runtime/session test binding: full Save G1 remains HOLD.
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
PORT="ce46a5e0cc5d26df6c4ece723e8efe20d70fc4b1"
API="b8ca29c7707cd6b4338d0b9772d74dee676d49d2"
R5="cc7f73541517a18f4479bb155534bd4832d9985e"
SAVE_SELECTED="8634e71dfcf633323073fe584a6c0884f1ca5d23"
SAVE_FIX_CANDIDATE="3aaaec4027ee246fbfb6f209179f0e9507363473"
W22="481bfb1cf2804d94fb49d7e6a57134e96c535b86"
NATIVE_CONSENT="B0160 PORT772 THREE REAL UNITY G0 REVIEWED"
SUITE="B01.CanonicalRigPortW24.Tests.CanonicalPhysicalRigPortW24NativeTests."
CASES=(
 "Original510AtomicLedger_RealRigidbodyDetachedAndAttachedParentAndVelocity",
 "InvalidOriginal556Binary_CannotCreateAnyUnityBodyOrConsumeId",
 "QuiescenceRequired_BeforeRealPhysicalStage",
)
TEST="Assets/B0123CanonicalRigPortW24/Tests/PlayMode/CanonicalPhysicalRigPortW24NativeTests.cs"
RUNTIME="Assets/B0123CanonicalRigPortW24/Runtime/CanonicalPhysicalRigPortW24.cs"
HYDRATOR="Assets/B0123CanonicalRigPortW24/Runtime/CanonicalRigLedgerHydratorW24.cs"
RUNTIME_ASM="Assets/B0123CanonicalRigPortW24/Runtime/B0123.CanonicalRigPortW24.Runtime.asmdef"
PLAY_ASM="Assets/B0123CanonicalRigPortW24/Tests/PlayMode/B0123.CanonicalRigPortW24.PlayMode.asmdef"
OWNER="Tools/B01/B01-23/P17W24CanonicalPhysicalRigPort001"
WORKFLOW=".github/workflows/b01-23-p17w24-physical510-port.yml"
PINS={
 TEST:"6827351e8f578030de932b2d36a5c0c55337a2f7",
 RUNTIME:"2a9ac87d3028d5c5a51915a27c8d2f49aa363f85",
 HYDRATOR:"eb21cb12605e94d17dc7decdf040c9009ca0aafe",
 RUNTIME_ASM:"301f9b7a240bcbe0221fd1f4e320a99c9b86c08d",
 PLAY_ASM:"1ccfc74cca91c41086a0bb7716d88febdbe996a7",
}
SCOPE=(
 "Assets/B0123DebrisRepulse001",
 "Assets/B0123SecureReattachV2",
 "Assets/B0123DetachedSaveContract001",
 "Assets/B0123UnitySaveBridge001",
 "Assets/B0123CanonicalSaveApi001",
 "Assets/B0123CanonicalRigPortW24",
)
BASE_FILES=("Packages/manifest.json","Packages/packages-lock.json",
            "ProjectSettings/ProjectVersion.txt","ProjectSettings/ProjectSettings.asset")
W22_PATH=ROOT/"Tools/B01/B01-60/P17W22R5PartialNative/r5_native.py"
RECEIPT="P25-port772-receipt.json"

def need(condition,message):
 if not condition:raise AssertionError(message)
def w22():
 spec=importlib.util.spec_from_file_location("b0160_original_w22_native_p25",W22_PATH)
 need(W22_PATH.is_file() and spec and spec.loader,"Original W22 hardened native launcher missing")
 n=importlib.util.module_from_spec(spec)
 spec.loader.exec_module(n)
 need(n.W21=="02272e8b0facc00d3943672770251b1d01182b85" and
      n.R5==R5 and n.PROBE=="B01.UnitySaveBridge001.Tests.B0160W22ScopeProbe.OnlyPrivateSavePath",
      "Unexpected modified original W22 native helper/guard")
 need(n.git("rev-parse",W22+":Tools/B01/B01-60/P17W22R5PartialNative/r5_native.py")==
      "40ca11d886fc567a5a79f6abfeabdf2b37f4c36e",
      "Original selected SHA #718 W22 native packager changed")
 need(n.git("merge-base",W22,"HEAD")==W22,
      "W25 branch must descend unchanged frozen original W22 QA #718")
 return n

def owner_changes(n):
 need(n.git("cat-file","-t",PORT)=="commit","Original #772 Git commit unavailable locally: NO automatic fetch")
 need(n.git("merge-base",API,PORT)==API,"#772 not descendant of sole selected #729 API")
 changed=set(n.git("diff","--name-only",API,PORT).splitlines())
 # The owner PR has exactly its own 21 new assets/Tools/CI files; no producer
 # Asset files or legacy API may be changed in this source version.
 need(len(changed)==21 and all(
       x==WORKFLOW or x.startswith(OWNER+"/") or
       x=="Assets/B0123CanonicalRigPortW24.meta" or
       x.startswith("Assets/B0123CanonicalRigPortW24/")
       for x in changed),"Owner #772 source changed outside exact 21-file isolated scope")
 need(all(n.git("rev-parse",PORT+":"+p)==blob for p,blob in PINS.items()),
      "Owner #772 original authored source bytes/asmdefs no longer match")
 raw=subprocess.check_output(["git","-C",str(ROOT),"show",PORT+":"+TEST],stderr=subprocess.PIPE)
 cases=re.findall(rb"\[UnityTest\]\s*public\s+IEnumerator\s+(\w+)\s*\(",raw)
 need(tuple(x.decode() for x in cases)==CASES,"Owner #772 real native NUnit test identity changed")
 for ref in (SAVE_SELECTED,SAVE_FIX_CANDIDATE):
  need(n.git("cat-file","-t",ref)=="commit","Selected Save source Git object not in local cache: "+ref)
 need(n.git("merge-base",SAVE_SELECTED,SAVE_FIX_CANDIDATE)==SAVE_SELECTED,
      "Proposed #773 not descendant of the ONLY currently selected #749 Save")
 # Explicit source evidence of Unity integration absence: #773 has no new
 # Unity Assets/asmdef files, so do NOT advertise canonical Save G1 execution.
 changed_save=n.git("diff","--name-only",SAVE_SELECTED,SAVE_FIX_CANDIDATE).splitlines()
 need(len(changed_save)==8 and all(x.startswith("Tools/B01/B01-62/P17W24CanonicalPathFixR2/")
      or x==".github/workflows/b01-62-w24-749-canonical-path-r2.yml"
      for x in changed_save), "Selected Save fix #773 unexpectedly contains unreviewed Unity runtime")
 return n

def corrected_port_source(n,m):
 owner_changes(n)
 names=n.git("ls-tree","-r","--name-only",PORT).splitlines()
 selected={}
 for p in names:
  if p in BASE_FILES or any(p==scope+".meta" or p.startswith(scope+"/") for scope in SCOPE):
   selected[p]=subprocess.check_output(
    ["git","-C",str(ROOT),"show",PORT+":"+p],stderr=subprocess.PIPE)
 need(all(p in selected for p in PINS),"Original #772 native Unity source closure incomplete")
 need(all(p in selected for p in BASE_FILES),"Original Unity project source settings missing")
 need(b"6000.3.14f1" in selected["ProjectSettings/ProjectVersion.txt"],
      "Original Unity Editor version not 6000.3.14f1")
 for path,expected in PINS.items():
  import hashlib
  b=selected[path]
  blob=hashlib.sha1(b"blob "+str(len(b)).encode()+b"\0"+b).hexdigest()
  need(blob==expected,"Wrong original #772 Git blob for native source: "+path)
 meta_count=m.guid_check(selected)
 need(meta_count>=39,"Original #772 or canonical Unity GUID source closure incomplete")
 import json as j
 for p in (RUNTIME_ASM,PLAY_ASM):
  asm=j.loads(selected[p].decode())
  need(asm.get("name","").startswith("B0123.CanonicalRigPortW24"),
       "Wrong original owner C# assembly manifest")
  need("B0123.CanonicalSaveApi.Core" in asm["references"],
       "Original selected canonical API assembly missing")
 return selected

def run(args):
 n=w22()
 m=n.runner()
 need(m.SHA==R5,"Original W22 producer R5 in-memory module mutated")
 m.SHA=PORT # ONLY this separately loaded QA module; original frozen files untouched
 files=corrected_port_source(n,m)
 if args.mode=="native":
  need(os.name=="nt","Real Unity/PhysX allowed ONLY by local Windows manual operator")
  need(args.operator_consent==NATIVE_CONSENT,"Wrong/missing explicit operator consent")
  unity=Path(args.unity)
  need(unity.is_file() and re.search(r"[/\\]6000\.3\.14f1[/\\]Editor[/\\]Unity\.exe$",str(unity),re.I),
       "Real installed Unity Hub 6000.3.14f1 Editor executable required")
 else:unity=None
 root=n.safe_root(args.scratch_parent)
 receipt={
  "package":"B01-60-P17W25-PHYSICAL772-UNITYG1-001",
  "owner_port_772_SHA":PORT,"original_api_729_SHA":API,
  "selected_save_749_SHA":SAVE_SELECTED,"proposed_save_fix_773_SHA":SAVE_FIX_CANDIDATE,
  "native_G1":"NOT_RUN","physical_port_G1":"NOT_RUN",
  "canonical_save_G1":"NOT_RUN_NO_UNITY_SESSION_ASMDEF",
  "source_G0":"INDEPENDENT_NEW_772_G0_NOT_PROVEN_BY_THIS_RUNNER",
  "original_three_PlayMode_cases":list(CASES),
  "original_private_guard_test":n.PROBE,
  "native_claim_only_with_actual_Unity_Editor_log_and_XML":True,
  "merged":False,"released":False,"codex_ACK":"NONE","uploaded":False,
 }
 path=root/RECEIPT
 path.write_text(json.dumps(receipt,sort_keys=True,indent=2)+"\n")
 try:
  original_guid_count=m.guid_check(files)
  for i,case in enumerate(CASES):
   scoped,product=n.private_source(files,i,secrets.token_hex(6),m)
   need(m.guid_check(scoped)==original_guid_count+1,
        "Missing unique QA-only Unity persistentDataPath guard GUID")
   archive=m.zip_data(scoped,{
     "original_source_sha":PORT,"native_unity":"NOT_RUN",
     "source_only_preflight":True,"codex_ack":False,"integrated":False,
     "native_meta_guids":m.guid_check(scoped),
     "file_sha256":{k:n.blake(v) for k,v in sorted(scoped.items())}})
   project=root/("P"+str(i))
   m.stage(archive,scoped,project)
   need((project/n.PS).read_bytes()==scoped[n.PS],
        "Stage unexpectedly changed exact QA private project PlayerSettings")
   if args.mode=="native":
    n.run_editor(unity,project,root,"guard"+str(i),n.PROBE,scoped,m)
    n.run_editor(unity,project,root,"portcase"+str(i),SUITE+case,scoped,m)
   else:m.rollback(archive,scoped,project)
  if args.mode=="preflight":
   receipt["source_preflight"]="PASS_3_PRIVATE_UNITY_PORT_PROJECTS_STAGED_AND_ROLLED_BACK"
   print("B0160_W25_PORT772_THREE_ORIGINAL_PLAYMODE_IDENTITIES_AND_GUIDS_PASS")
   print("B0160_W25_PORT772_THREE_PRIVATE_STAGE_ROLLBACK_PASS")
   print("B0160_W25_PORT772_CANONICAL_SAVE_G1_NOT_RUN_UNITY_BINDING_MISSING")
   print("B0160_W25_PORT772_NATIVE_G1_NOT_RUN")
  else:
   receipt["native_G1"]="THREE_PORT_TESTS_EXECUTED_PENDING_INDEPENDENT_REVIEW"
   receipt["physical_port_G1"]="THREE_PORT_TESTS_EXECUTED_NOT_FULL_GAME"
   print("B0160_W25_REAL_UNITY_THREE_PORT_CASES_ONLY_QUEUED_FOR_MANUAL_EVIDENCE_REVIEW")
  path.write_text(json.dumps(receipt,sort_keys=True,indent=2)+"\n")
  (root/"P25-port772-sha256-manifest.json").write_text(
      json.dumps(n.manifest_files(root,receipt),sort_keys=True,indent=2)+"\n")
  print("B0160_W25_PRIVATE_EVIDENCE_NO_UPLOAD="+str(root))
  return 0
 except Exception as e:
  receipt["failure"]=str(e)
  path.write_text(json.dumps(receipt,sort_keys=True,indent=2)+"\n")
  print("B0160_W25_G1_FAIL_CLOSED="+str(e),file=sys.stderr)
  print("B0160_W25_PRIVATE_EVIDENCE_NO_UPLOAD="+str(root))
  return 4 if args.mode=="native" else 2

def main(argv=None):
 p=argparse.ArgumentParser()
 p.add_argument("--mode",choices=("preflight","native"),default="preflight")
 p.add_argument("--unity",default="")
 p.add_argument("--scratch-parent",default=os.environ.get("TEMP") or tempfile.gettempdir())
 p.add_argument("--operator-consent",default="")
 a=p.parse_args(argv)
 try:return run(a)
 except Exception as e:
  print("B0160_W25_PRESTAGE_HOLD="+repr(e),file=sys.stderr)
  return 3 if a.mode=="native" else 2
if __name__=="__main__":sys.exit(main())
