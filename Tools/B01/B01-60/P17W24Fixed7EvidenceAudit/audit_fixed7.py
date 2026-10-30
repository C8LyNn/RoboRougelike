#!/usr/bin/env python3
"""B01-60 W24: independent, read-only FULL corrected R5 seven-case evidence audit.

Consumes ONLY original B01-60 #751 P23-fixed7-* local evidence. Validates the
exact owner-selected #732 fixture and original R5 #690 runtime Git blob SHA
for *each* Unity project. Checks full 7 guards + 7 real case results; no fake
native promotion, no automatic network, Unity invocation, attestation or G2.
"""
import argparse
import hashlib
import json
from pathlib import Path, PurePosixPath
import re
import sys
import xml.etree.ElementTree as ET

SOURCE="0bee4ce9928354433ec719c45a8186378361d46e"
R5="cc7f73541517a18f4479bb155534bd4832d9985e"
W22="481bfb1cf2804d94fb49d7e6a57134e96c535b86"
GAME="d94400d95d13db7f27e1533df05c9292d5380431"
FIXTURE="Assets/B0123UnitySaveBridge001/Tests/IsolatedSaveWorldNativePlayModeTests.cs"
PROBE_FILE="Assets/B0123UnitySaveBridge001/Tests/B0160W22ScopeProbe.cs"
SETTINGS="ProjectSettings/ProjectSettings.asset"
OTHER_BLOBS={
 FIXTURE:"d096f65eeb37393204a35445dd87cbdffa642ce0",
 "Assets/B0123UnitySaveBridge001/Core/AtomicWorldSwap.cs":
     "7da9a9efe489cf8c441d7bb9a47ba3f60ba4c58c",
 "Assets/B0123UnitySaveBridge001/Unity/IsolatedTwinRigSaveHost.cs":
     "6b3936d82b5e1f175856a9e138124b14c9dfd19c",
 "Assets/B0123UnitySaveBridge001/Core/OwnerWorldLeaseRetirement.cs":
     "1ef69de9b4029ce89ba2db5bf3331708da1b91e4",
 "Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs":
     "b52e511c370f5fca56c74029050844dc8cbb7f9a"
}
CASES=(
 "ColdLoad_RealRigidbody_RoundTripLightHeavy",
 "CorruptLoad_LeavesPreviouslyActiveRigidbodyAndIdsUntouched",
 "ColdRestart_RestoresRealRigidbodyAndConsumedIds",
 "WrongGeneration_CannotPartiallyPublishRealPhysicalWorld",
 "SeparateRunSlots_CannotPoisonLaterRunOrReuseForeignOwner",
 "ComponentOnlyDestroy_RetainsLeaseUntilRealChildRigidbodyDestruction",
 "DisabledSameSlotHost_CannotStealOwnerUntilDestroyed"
)
SUITE="B01.UnitySaveBridge001.Tests.IsolatedSaveWorldNativePlayModeTests."
GUARD="B01.UnitySaveBridge001.Tests.B0160W22ScopeProbe.OnlyPrivateSavePath"
RECEIPT="P23-fixed7-receipt.json"
MANIFEST="P23-fixed7-sha256-manifest.json"
PROJECT_PREFIXES=tuple("P"+str(i) for i in range(7))
EXCLUDE={"Library","Temp","Logs","obj"}
OWNER_VERSION="6000.3.14f1"
MAX_FILE=16*1024*1024

class Hold(ValueError):pass

def need(ok,why):
 if not ok:raise Hold(why)
def sha(raw):return hashlib.sha256(raw).hexdigest()
def gitblob(raw):return hashlib.sha1(b"blob "+str(len(raw)).encode("ascii")+b"\0"+raw).hexdigest()
def no_symlink(path):
 p=Path(path).absolute()
 for item in (p,*p.parents):
  need(not item.is_symlink(),"symlink/reparse evidence path: "+str(item))
 return p
def load(path,limit=MAX_FILE):
 path=no_symlink(path)
 need(path.is_file(),"required evidence missing: "+str(path))
 need(path.stat().st_size<=limit,"oversized evidence file: "+str(path))
 return path.read_bytes()
def obj(path):
 def unique(items):
  d={}
  for k,v in items:
   need(k not in d,"duplicate JSON key: "+k)
   d[k]=v
  return d
 try:return json.loads(load(path,1024*1024).decode("utf-8"),object_pairs_hook=unique)
 except (UnicodeDecodeError,json.JSONDecodeError) as e:
  raise Hold("bad UTF-8 JSON: "+str(path)) from e
def safe_rel(s):
 need(isinstance(s,str) and s!="","bad inventory key")
 p=PurePosixPath(s)
 need(not p.is_absolute() and p.as_posix()==s and "\\" not in s and
      ":" not in s and "\x00" not in s and
      all(x not in ("",".","..") for x in s.split("/")),
      "noncanonical/unsafe path: "+s)
 return p
def allowed_root():
 out={RECEIPT,MANIFEST}
 for i in range(7):
  for prefix in ("guard","r5case"):
   for suffix in (".xml","-Editor.log","-process.log"):
    out.add(f"{prefix}{i}{suffix}")
 return out
ROOT_ALLOW=allowed_root()

def selected_inventory(root):
 # Exact original #751 parent source inventory semantics plus fail-closed
 # root filename allowlist and complete disjoint native project membership.
 got={}
 for item in root.iterdir():
  no_symlink(item)
  if item.is_file():
   need(item.name in ROOT_ALLOW,"unexpected root evidence file: "+item.name)
   if item.name!=MANIFEST:got[item.name]=sha(load(item))
   continue
  need(item.is_dir() and item.name in PROJECT_PREFIXES,
       "foreign root evidence directory: "+item.name)
  stack=[item]
  while stack:
   folder=stack.pop()
   for child in folder.iterdir():
    no_symlink(child)
    rel=child.relative_to(root).as_posix()
    if child.is_dir():
     if child.name in EXCLUDE:continue
     stack.append(child)
    else:
     need(child.is_file(),"special evidence file: "+rel)
     got[rel]=sha(load(child))
 return got

def exact_xml(path,fullname):
 try:root=ET.fromstring(load(path))
 except ET.ParseError as e:raise Hold("invalid NUnit3 XML: "+str(path)) from e
 need(root.tag=="test-run" and root.get("result")=="Passed",
      "NUnit run is not Passed")
 for key,val in (("passed",1),("failed",0),("total",1),("skipped",0),("inconclusive",0)):
  if key in ("passed","failed","total") or key in root.attrib:
   need(root.get(key)==str(val),"NUnit root "+key+" is inconsistent")
 cases=list(root.iter("test-case"))
 need(len(cases)==1,"not exactly one filtered NUnit3 test")
 case=cases[0]
 need(case.get("fullname")==fullname and case.get("result")=="Passed" and
      case.get("label") not in ("Skipped","Ignored","Explicit"),
      "wrong, ignored, duplicate or failing original NUnit test")
 need(all(x.get("result")!="Failed" for x in root.iter("test-suite")),
      "failed NUnit suite despite passing root")

def selected_receipt(r):
 need(isinstance(r,dict),"original #751 receipt JSON must be object")
 required={
  "package":"B01-60-P17W23-R5-COLDRESTART-FIXED-SEVEN-NATIVE",
  "source":SOURCE,"parent_selected_R5":R5,"parent_W22_QA":W22,
  "coldrestart_generation":"g4->g5 original #556",
  "codex_ACK":"NONE","integrated":False,"released":False,
  "evidence_is_self_hashed_not_attested":True}
 for k,v in required.items():
  need(type(r.get(k)) is type(v) and r.get(k)==v,
       "original fixture/source/claim drift: "+k)
 need(r.get("original_case_methods")==list(CASES),"not exactly seven original R5 cases")
 need("source_or_native_error" not in r,"original runner failed: reject any evidence")
 need(r.get("source_G0")=="OWNER_CORRECTED_732_PENDING_INDEPENDENT_G0",
      "operator receipt unexpectedly claims independent formal G0")
 need(r.get("native_G1") in ("NOT_RUN",
      "SEVEN_ORIGINAL_R5_NATIVE_CASES_COMPLETED_REQUIRES_INDEPENDENT_REVIEW"),
      "native G1 falsely promoted or unknown")
 need(r.get("full_R5_G1") in ("NOT_RUN",
      "HOLD_PENDING_INDEPENDENT_SOURCE_G0_AND_OWNER_ATTESTATION"),
      "original R5 G1 falsely accepted")
 return r["native_G1"]

def verify(directory):
 root=no_symlink(Path(directory))
 need(root.is_dir(),"evidence directory unavailable")
 r=obj(root/RECEIPT)
 m=obj(root/MANIFEST)
 status=selected_receipt(r)
 need(isinstance(m,dict) and m.get("unsigned") is True and
      m.get("note")=="SHA256 consistency, not native attestation" and
      m.get("receipt")==r,"incorrect original unsigned receipt wrapper")
 claimed=m.get("evidence_sha256")
 need(isinstance(claimed,dict),"original SHA256 inventory missing")
 for k,v in claimed.items():
  safe_rel(k)
  need(isinstance(v,str) and re.fullmatch("[0-9a-f]{64}",v) is not None,
       "bad inventory SHA256: "+k)
 found=selected_inventory(root)
 need(found==claimed,"missing/extra/modified original hash-inventoried evidence")
 need(claimed.get(RECEIPT)==sha(load(root/RECEIPT)),
      "original receipt SHA256 unavailable")
 if status=="NOT_RUN":
  need(r.get("source_preflight")=="PASS_7_PRIVATE_PROJECTS_STAGE_ROLLBACK_NO_UNITY" and
       r.get("full_R5_G1")=="NOT_RUN" and
       r.get("operator_g0_review_claim") is False and
       set(claimed)=={RECEIPT} and
       all(not (root/p).exists() for p in PROJECT_PREFIXES),
       "source-only receipt cannot contain partial Unity evidence or G0 claim")
  return {"verdict":"CONSISTENT_SOURCE_ONLY","native_G1":"NOT_RUN",
          "full_R5_G1":"NOT_RUN","selected_732_SHA":SOURCE,
          "file_count":len(found),"independent_native_attestation":"NONE"}
 need(r.get("full_R5_G1")=="HOLD_PENDING_INDEPENDENT_SOURCE_G0_AND_OWNER_ATTESTATION" and
      r.get("operator_g0_review_claim") is True and
      "source_preflight" not in r,
      "native receipt missing operator acknowledgement/HOLD semantics")
 for i,method in enumerate(CASES):
  project=root/("P"+str(i))
  need(project.is_dir() and not project.is_symlink(),
       "missing original per-case isolated project")
  ps=load(project/SETTINGS).decode("utf-8")
  products=re.findall(r"(?m)^[ \t]*productName:[ \t]*(B0160W22Case"+str(i)+r"[0-9a-f]{12})[ \t]*$",ps)
  need(len(products)==1,"invalid/duplicate private Unity productName")
  company=re.findall(r"(?m)^[ \t]*companyName:[ \t]*(B0160W22[0-9a-f]{12})[ \t]*$",ps)
  need(len(company)==1,"invalid/duplicate private Unity companyName")
  probe=load(project/PROBE_FILE).decode("utf-8")
  need(products[0] in probe and "OnlyPrivateSavePath" in probe and
       "b0123_one_save_host_v2.checkpoint" in probe,
       "missing original private checkpoint scope probe")
  for name,expected_blob in OTHER_BLOBS.items():
   need(gitblob(load(project/name))==expected_blob,
        "original owner-selected source bytes altered: "+name)
  version=load(project/"ProjectSettings"/"ProjectVersion.txt")
  need(OWNER_VERSION.encode() in version,"wrong project Unity version")
  for key,fullname in ((f"guard{i}",GUARD),(f"r5case{i}",SUITE+method)):
   exact_xml(root/(key+".xml"),fullname)
   editor=load(root/(key+"-Editor.log"))
   need(OWNER_VERSION.encode() in editor and len(editor)>32,
        "missing genuine-looking original Unity Editor.log/version")
   process=load(root/(key+"-process.log"))
   need(process.startswith(b"exit_code=0\n"),
        "nonzero/absent original Unity process return")
 expected=set(ROOT_ALLOW)-{RECEIPT,MANIFEST}
 present={p for p in claimed if "/" not in p and p!=RECEIPT}
 need(present==expected,"original 14 native test jobs missing/unexpected")
 products=[]
 for i in range(7):
  s=load(root/f"P{i}"/SETTINGS).decode()
  products.append(re.search(r"(?m)^[ \t]*productName:[ \t]*(\S+)",s).group(1))
 need(len(set(products))==7,"reused Unity checkpoint across native cases")
 return {"verdict":"STRUCTURALLY_CONSISTENT_7_OF_7_ORIGINAL_R5",
         "native_G1":"UNATTESTED_FILES_ONLY",
         "full_R5_G1":"HOLD_PENDING_INDEPENDENT_REVIEW",
         "selected_732_SHA":SOURCE,"file_count":len(found),
         "independent_native_attestation":"NONE_MANUAL_ACTUAL_EDITOR_REVIEW_REQUIRED"}

def main(argv=None):
 p=argparse.ArgumentParser()
 p.add_argument("receipt_dir",help="Existing local B22-* #751 evidence directory, READ-ONLY")
 a=p.parse_args(argv)
 try:
  print(json.dumps(verify(Path(a.receipt_dir)),sort_keys=True))
  return 0
 except (OSError,ValueError,UnicodeError) as e:
  print("B0160_W24_FIXED7_EVIDENCE_REJECTED="+str(e),file=sys.stderr)
  return 2
if __name__=="__main__":sys.exit(main())
