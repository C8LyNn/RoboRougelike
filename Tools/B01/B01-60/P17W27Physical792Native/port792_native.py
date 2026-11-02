#!/usr/bin/env python3
"""B01-60 W27: opt-in REAL Unity6 four original #792 PhysX PlayMode tests.

Extends immutable B01-60 #777 operator W25 with ONLY source-owned #792's
2 corrected Unity files, checking their exact Git blobs and original source
closure; avoids all competing Unity Save/economy installation.
Default source-only. Native only Windows, exact Unity Editor, typed consent.
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

ROOT=Path(__file__).resolve().parents[4]
OLD=ROOT/"Tools/B01/B01-60/P17W25Physical772Native/port772_native.py"
PORT772="ce46a5e0cc5d26df6c4ece723e8efe20d70fc4b1"
PORT792="16cb2a7b5158605b15191b6074f9e392e0d1ebd3"
SAVE787="f5db40bf293fe9582ec7c6fcb3515aeff6fcf937"
FULL797="2b7983f0f9e09dd1d56906b379a3f7af7b02f21a"
ECON780="b4df10a7ad7e6c758f8331013f6b216b3a3689a3"
R5_750="60c73a6f9ed2585cbb9bf7aeaad8c62eef0629cb"
RUNTIME="Assets/B0123CanonicalRigPortW24/Runtime/CanonicalPhysicalRigPortW24.cs"
FIXTURE="Assets/B0123CanonicalRigPortW24/Tests/PlayMode/CanonicalPhysicalRigPortW24NativeTests.cs"
RUNTIME_BLOB="7623f698b6e644b8d8247a011b453bf40adc2227"
FIXTURE_BLOB="358ef67f8f0f795d94a646e8e11c27158e2c8f0d"
CONSENT="B0160 W27 PORT792 FOUR NATIVE PHYSX G0 REVIEWED"
CASES=(
 "Original510AtomicLedger_RealRigidbodyDetachedAndAttachedParentAndVelocity",
 "AbortedCompetingWorld_PreservesActualLiveVelocityAndAngularVelocity",
 "InvalidOriginal556Binary_CannotCreateAnyUnityBodyOrConsumeId",
 "QuiescenceRequired_BeforeRealPhysicalStage",
)
RECEIPT="P27-port792-original-evidence.json"
GUARD="B01.UnitySaveBridge001.Tests.B0160W22ScopeProbe.OnlyPrivateSavePath"

def need(ok,msg):
 if not ok:raise AssertionError(msg)
def old_runner():
 need(OLD.is_file(),"Historical W25 #777 physical runner missing")
 spec=importlib.util.spec_from_file_location("b0160_w25_immutable_772",OLD)
 need(spec is not None and spec.loader is not None,"Historical B01-60 runner invalid")
 q=importlib.util.module_from_spec(spec)
 spec.loader.exec_module(q)
 need(q.PORT==PORT772 and q.TEST==FIXTURE and q.RUNTIME==RUNTIME and
      len(q.CASES)==3 and q.RECEIPT=="P25-port772-receipt.json",
      "Historical W25 QA contract drift")
 n=q.w22()
 need(n.git("rev-parse",
     "8057c4da7edc49f67ec2de505c4d56a57eba4483:Tools/B01/B01-60/P17W25Physical772Native/port772_native.py")==
     "5e1d879e837017bfe79a225dac3b8a137ff3287f",
     "Historical W25 QA file replaced instead of extending")
 need(n.git("merge-base","8057c4da7edc49f67ec2de505c4d56a57eba4483","HEAD")==
      "8057c4da7edc49f67ec2de505c4d56a57eba4483",
      "W27 QA must branch only from own original immutable #777")
 return q,n

def git_source(n,sha,path):
 return subprocess.check_output(["git","-C",str(ROOT),"show",sha+":"+path],
                                stderr=subprocess.PIPE)

def blob(data):
 return hashlib.sha1(b"blob "+str(len(data)).encode()+b"\0"+data).hexdigest()

def validate(n,q):
 need(n.git("cat-file","-t",PORT792)=="commit","Original owner #792 not fetched; operator must not auto-fetch")
 need(n.git("merge-base",PORT772,PORT792)==PORT772,"#792 not isolated descendant of owner #772")
 changed=set(n.git("diff","--name-only",PORT772,PORT792).splitlines())
 own="Tools/B01/B01-23/P17W25PhysicalRollbackR2/"
 need(len(changed)==10 and RUNTIME in changed and FIXTURE in changed and
      all(p in (RUNTIME,FIXTURE,".github/workflows/b01-23-p17w25-physical-rollback-r2.yml") or
          p.startswith(own) for p in changed),
      "#792 is no longer exactly 2 corrected original Unity files + 7 owner test/CI files")
 need(n.git("rev-parse",PORT792+":"+RUNTIME)==RUNTIME_BLOB and
      n.git("rev-parse",PORT792+":"+FIXTURE)==FIXTURE_BLOB,
      "Original #792 current real PhysX regression source blob mismatch")
 for p,b in q.PINS.items():
  if p not in (RUNTIME,FIXTURE):
   need(n.git("rev-parse",PORT792+":"+p)==b,
        "Frozen #772 source/asmdef GUID changed in #792: "+p)
 for sha in (SAVE787,FULL797,ECON780,R5_750):
  need(n.git("cat-file","-t",sha)=="commit","Selected W26 source object missing locally "+sha)
 need(n.git("merge-base",SAVE787,FULL797)==SAVE787,
      "Selected one-file #797 must descend exact selected strict canonical Save #787")
 for project in (
  "Assets/B0123CanonicalRigPortW24/Runtime/B0123.CanonicalRigPortW24.Runtime.asmdef",
  "Assets/B0123CanonicalRigPortW24/Tests/PlayMode/B0123.CanonicalRigPortW24.PlayMode.asmdef"):
  need(n.git("rev-parse",PORT792+":"+project)==n.git("rev-parse",PORT772+":"+project),
       "Original Unity asmdef unexpectedly changed")
 raw=git_source(n,PORT792,FIXTURE)
 named=tuple(x.decode() for x in re.findall(
   rb"\[UnityTest\]\s*public\s+IEnumerator\s+(\w+)\s*\(",raw))
 need(named==CASES,"Original #792 four real Unity PlayMode methods not exact")
 old=git_source(n,PORT772,RUNTIME)
 new=git_source(n,PORT792,RUNTIME)
 for token in (b"resumePending",b"resumeLinear",b"resumeAngular",
               b"r.Body.linearVelocity",b"r.Body.angularVelocity"):
  need(token in new,"Missing live-world current velocity rollback: "+token.decode())
 need(blob(raw)==FIXTURE_BLOB and blob(new)==RUNTIME_BLOB and old!=new,
      "Regression R2 must change original runtime/fixture, not merely README")
 return changed

def selected_project(n,q,m):
 validate(n,q)
 m.SHA=PORT772
 sources=q.corrected_port_source(n,m)
 # Overlay EXACT two original #792 owner files in the PRIVATE disposable Unity
 # project source-only map, NOT on disk in the repository or any Codex worktree.
 for path,want in ((RUNTIME,RUNTIME_BLOB),(FIXTURE,FIXTURE_BLOB)):
  data=git_source(n,PORT792,path)
  need(blob(data)==want,"Original selected #792 modified Git blob: "+path)
  sources[path]=data
 need(m.guid_check(sources)>=39,"Original source Unity GUID closure failed")
 return sources

def execute(args):
 q,n=old_runner()
 m=n.runner()
 need(m.SHA==q.R5,"Original frozen R5 W22 packager unexpectedly changed")
 sources=selected_project(n,q,m)
 m.SHA=PORT792
 if args.mode=="native":
  need(os.name=="nt","Actual licensed Unity 6000.3.14f1 PlayMode may run only local Windows")
  need(args.operator_consent==CONSENT,"Missing exact independently G0-reviewed operator typed consent")
  unity=Path(args.unity)
  need(unity.is_file() and re.search(r"[/\\]6000\.3\.14f1[/\\]Editor[/\\]Unity\.exe$",str(unity),re.I),
       "No exact locally licensed Unity Hub 6000.3.14f1")
 else:unity=None
 root=n.safe_root(args.scratch_parent)
 receipt={"package":"B01-60-P17W27-PHYSICAL792-FOUR-NATIVE-001",
          "physical_source":PORT792,"previous_physical":PORT772,
          "only_selected_save":SAVE787,"full_game_save":FULL797,
          "economy_v7":ECON780,"r5_fixture":R5_750,
          "cases":list(CASES),"guard":GUARD,
          "source_G0":"PENDING_EXACT_COMPOSITE_INDEPENDENT",
          "native_G1":"NOT_RUN","full_game_save_G1":"NOT_RUN",
          "operator_ack_G0":args.mode=="native",
          "integrated":False,"released":False,"codex_ACK":"NONE"}
 path=root/RECEIPT
 path.write_text(json.dumps(receipt,indent=2,sort_keys=True)+"\n")
 try:
  guid=m.guid_check(sources)
  for i,case in enumerate(CASES):
   private,_=n.private_source(sources,i,secrets.token_hex(6),m)
   need(m.guid_check(private)==guid+1,"Original per-case checkpoint GUID not unique")
   archive=m.zip_data(private,{
     "original_source_sha":PORT792,"native_unity":"NOT_RUN",
     "source_only_preflight":True,"codex_ack":False,"integrated":False,
     "native_meta_guids":m.guid_check(private),
     "file_sha256":{k:n.blake(v) for k,v in sorted(private.items())}})
   stage=root/("P"+str(i))
   m.stage(archive,private,stage)
   need((stage/n.PS).read_bytes()==private[n.PS],
        "Staged project changed unique PlayerSettings")
   if args.mode=="native":
    n.run_editor(unity,stage,root,f"guard{i}",n.PROBE,private,m)
    n.run_editor(unity,stage,root,f"physcase{i}",q.SUITE+case,private,m)
   else:
    m.rollback(archive,private,stage)
  if args.mode=="preflight":
   receipt["source_preflight"]="FOUR_ORIGINAL_792_PRIVATE_SOURCE_STAGED_AND_ROLLED_BACK"
   print("B0160_W27_FOUR_ORIGINAL_792_PLAYMODE_NAMES_AND_SOURCE_BLOBS_PASS")
   print("B0160_W27_FOUR_PRIVATE_SOURCE_STAGE_ROLLBACK_PASS")
   print("B0160_W27_FULL_SAVE_ECON_G1_NOT_RUN_COMPOSITE_G0_HOLD")
   print("B0160_W27_NATIVE_UNITY_G1_NOT_RUN")
  else:
   receipt["native_G1"]="FOUR_PLAYMODE_PROCESSES_COMPLETED_PENDING_INDEPENDENT_ORIGINAL_LOG_REVIEW"
   print("B0160_W27_792_NATIVE_LOGS_PENDING_INDEPENDENT_G1_REVIEW")
  path.write_text(json.dumps(receipt,indent=2,sort_keys=True)+"\n")
  (root/"P27-port792-sha256-manifest.json").write_text(
    json.dumps(n.manifest_files(root,receipt),indent=2,sort_keys=True)+"\n")
  print("B0160_W27_PRIVATE_EVIDENCE_NO_UPLOAD="+str(root))
  return 0
 except Exception as e:
  receipt["error"]=str(e)
  path.write_text(json.dumps(receipt,indent=2,sort_keys=True)+"\n")
  print("B0160_W27_FAIL_CLOSED="+str(e),file=sys.stderr)
  print("B0160_W27_PRIVATE_EVIDENCE_NO_UPLOAD="+str(root))
  return 4 if args.mode=="native" else 2

def main(argv=None):
 p=argparse.ArgumentParser()
 p.add_argument("--mode",choices=("preflight","native"),default="preflight")
 p.add_argument("--unity",default="")
 p.add_argument("--scratch-parent",default=os.environ.get("TEMP") or tempfile.gettempdir())
 p.add_argument("--operator-consent",default="")
 a=p.parse_args(argv)
 try:return execute(a)
 except Exception as e:
  print("B0160_W27_PRESTAGE_HOLD="+str(e),file=sys.stderr)
  return 3 if a.mode=="native" else 2
if __name__=="__main__":sys.exit(main())
