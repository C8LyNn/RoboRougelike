#!/usr/bin/env python3
"""B01-60 W23 independent read-only R5 W22 evidence consistency verifier.

Verifies W22's exact SHA256 inventory, authentic SOURCE vs merely structurally
plausible PARTIAL six-of-seven Unity NUnit receipts. No attestation/signatures.
No source modification, deletion, remote I/O, Unity launching, or full-G1 claim.
"""
from __future__ import annotations
import argparse
import hashlib
import json
from pathlib import Path, PurePosixPath
import re
import sys
import xml.etree.ElementTree as ET

R5="cc7f73541517a18f4479bb155534bd4832d9985e"
GAME="d94400d95d13db7f27e1533df05c9292d5380431"
SAVE="33094f154c7b91119b36d58aab285e6e83c5de57"
PACKAGE="B01-60-P17W22-R5-NATIVE-PARTIAL"
FULL_HOLD="HOLD_ORIGINAL_COLDRESTART_GEN4_EXPECTS_GEN5"
G0="B01-40_699_SOURCE_ONLY"
METHODS=(
 "ColdLoad_RealRigidbody_RoundTripLightHeavy",
 "CorruptLoad_LeavesPreviouslyActiveRigidbodyAndIdsUntouched",
 "ColdRestart_RestoresRealRigidbodyAndConsumedIds",
 "WrongGeneration_CannotPartiallyPublishRealPhysicalWorld",
 "SeparateRunSlots_CannotPoisonLaterRunOrReuseForeignOwner",
 "ComponentOnlyDestroy_RetainsLeaseUntilRealChildRigidbodyDestruction",
 "DisabledSameSlotHost_CannotStealOwnerUntilDestroyed")
PARTIAL=tuple(x for x in METHODS if x!="ColdRestart_RestoresRealRigidbodyAndConsumedIds")
PROBE="B01.UnitySaveBridge001.Tests.B0160W22ScopeProbe.OnlyPrivateSavePath"
SUITE="B01.UnitySaveBridge001.Tests.IsolatedSaveWorldNativePlayModeTests."
VERSION="6000.3.14f1"
EXCLUDES={"Library","Temp","Logs","obj"}
JSON_LIMIT=1024*1024
FILE_LIMIT=16*1024*1024
EVIDENCE="P22-sha256-manifest.json"
RECEIPT="P22-receipt.json"

class Rejected(ValueError):pass

def need(pred, reason):
 if not pred:raise Rejected(reason)
def sha(data):
 return hashlib.sha256(data).hexdigest()
def no_link(path):
 # lexical walk to reject symlinked root/file ancestor even for a deleted target
 p=Path(path).absolute()
 for q in (p,*p.parents):
  need(not q.is_symlink(), "Symlink ancestor not acceptable: "+str(q))
 return p
def read_file(path,limit=FILE_LIMIT):
 no_link(path)
 need(path.is_file(),"Missing evidence file: "+str(path))
 need(path.stat().st_size<=limit,"Oversized evidence file: "+str(path))
 return path.read_bytes()
def read_json(path):
 raw=read_file(path,JSON_LIMIT)
 try:return json.loads(raw.decode("utf-8"))
 except (ValueError,UnicodeDecodeError) as e:raise Rejected("Invalid UTF-8 JSON "+str(path)) from e
def safe_relative(text):
 need(isinstance(text,str) and text!="","Empty/non-text inventory path")
 p=PurePosixPath(text)
 need(not p.is_absolute() and "\\" not in text and
      str(p)==text and all(bit not in ("..",".","") for bit in p.parts) and
      ":" not in p.parts[0],"Traversal/absolute/noncanonical evidence path: "+text)
 return p
def scoped_hashes(root):
 inventory={}
 # Mirrors ORIGINAL frozen W22 author SHA256 selection, without ever walking
 # Unity-generated Library/Temp/Logs/obj (which are explicitly excluded).
 for child in root.iterdir():
  no_link(child)
  if child.is_file():
   rel=child.name
   if rel.startswith("P") or rel.endswith((".xml",".log")):
    if rel!=EVIDENCE:inventory[rel]=sha(read_file(child))
  elif child.is_dir():
   need(re.fullmatch(r"P[0-5]",child.name) is not None,
        "Foreign directory under R5 evidence: "+child.name)
   pending=[child]
   while pending:
    current=pending.pop()
    for p in current.iterdir():
     no_link(p)
     sub=p.relative_to(root)
     if p.is_dir():
      if p.name in EXCLUDES:continue
      pending.append(p)
     elif p.is_file():
      inventory[sub.as_posix()]=sha(read_file(p))
     else:raise Rejected("Special file in evidence: "+str(sub))
  else:raise Rejected("Special root evidence entry: "+child.name)
 return inventory

def exact_test_xml(path, fullname):
 data=read_file(path)
 try:root=ET.fromstring(data)
 except ET.ParseError as e:raise Rejected("Invalid original NUnit XML "+str(path)) from e
 need(root.tag=="test-run" and root.attrib.get("result")=="Passed",
      "NUnit root not Passed")
 for k,expected in (("failed",0),("skipped",0),("inconclusive",0),("passed",1),("total",1)):
  if k in root.attrib:need(int(root.attrib[k])==expected,"NUnit root "+k+" mismatch")
 need(root.attrib.get("failed")=="0" and root.attrib.get("passed")=="1",
      "NUnit counters missing/invalid")
 cases=list(root.iter("test-case"))
 need(len(cases)==1,"NUnit must contain exactly one original selected R5 test")
 c=cases[0]
 need(c.attrib.get("fullname")==fullname and c.attrib.get("result")=="Passed",
      "Selected original R5 test identity/result mismatch")
 need(c.attrib.get("label") not in ("Ignored","Skipped","Explicit"),
      "Selected NUnit case not actually run")
 need(all(x.attrib.get("result")!="Failed" for x in root.iter("test-suite")),
      "A containing NUnit suite failed")

def original_receipt(receipt):
 need(isinstance(receipt,dict),"Original W22 receipt is not object")
 expected={"package":PACKAGE,"selected_host_R5_SHA":R5,
           "original_510_SHA":GAME,"original_556_SHA":SAVE,
           "R5_FULL_NATIVE_G1":FULL_HOLD,"source_G0":G0,
           "Codex_ACK":"NONE","merged":False,"uploaded":False}
 for k,v in expected.items():need(type(receipt.get(k)) is type(v) and receipt.get(k)==v,
                                  "W22 source identity/status mismatch: "+k)
 need(receipt.get("source_methods")==list(METHODS),"Wrong seven original R5 test names")
 need(receipt.get("native_partial_scope")==list(PARTIAL),"Wrong partial original R5 test scope")
 need("source_or_native_error" not in receipt,"Original W22 reported an error; no acceptance")
 need(receipt.get("native_G1") in ("NOT_RUN","PARTIAL_R5_SIX_OF_SEVEN_ONLY"),
      "Prohibited full G1 promotion on source-only R5 fixture")
 return receipt["native_G1"]

def verify(root_path):
 root=no_link(Path(root_path))
 need(root.is_dir(),"Evidence directory does not exist")
 evidence=read_json(root/EVIDENCE)
 receipt=read_json(root/RECEIPT)
 status=original_receipt(receipt)
 need(isinstance(evidence,dict) and evidence.get("unsigned") is True,
      "Original W22 unsigned evidence descriptor invalid")
 need(evidence.get("receipt")==receipt,"Original W22 receipt and manifest disagree")
 need(evidence.get("note")=="SHA256 consistency, not native attestation",
      "Original W22 checksum semantics changed")
 provided=evidence.get("evidence_sha256")
 need(isinstance(provided,dict),"Missing evidence_sha256 inventory")
 need(all(re.fullmatch(r"[0-9a-f]{64}",v or "") for v in provided.values()),
      "Invalid SHA256 inventory")
 for name in provided:safe_relative(name)
 observed=scoped_hashes(root)
 need(provided==observed,"Unsigned original W22 manifest file inventory/SHA differs")
 need(provided.get(RECEIPT)==sha(read_file(root/RECEIPT)),
      "Receipt SHA missing in original W22 file inventory")
 if status=="NOT_RUN":
  need(receipt.get("source_preflight")=="PASS_6_STAGE_ROLLBACK_NO_UNITY",
       "Source-only receipt does not show full safe private stage rollback")
  need(set(provided)=={RECEIPT},
       "Source-only receipt has native/project evidence not rolled back")
  need(not any((root/("P"+str(i))).exists() for i in range(6)),
       "Source-only receipt left a staged private project")
  return {"source_preflight":"CONSISTENT_SOURCE_ONLY","native":"NOT_RUN",
          "full_R5_G1":"HOLD","files_audited":len(provided),
          "original_R5":R5,"attestation":"NONE"}
 need("source_preflight" not in receipt,"Native receipt falsely also claims preflight-only")
 # Six separate manually launched Unity project scopes, with the original guard
 # preceding the original R5 test process for each case. Mere files cannot prove
 # real Unity ran; the result is explicitly STRUCTURAL_ONLY.
 products=set()
 for i,method in enumerate(PARTIAL):
  project=root/("P"+str(i))
  need(project.is_dir() and not project.is_symlink(),"Missing original private Unity project")
  settings=read_file(project/"ProjectSettings"/"ProjectSettings.asset").decode("utf-8")
  pm=re.findall(r"(?m)^\s*productName:\s*(B0160W22Case"+str(i)+r"[0-9a-f]{12})\s*$",settings)
  need(len(pm)==1,"Missing/ambiguous original private checkpoint product")
  need(pm[0] not in products,"Duplicate Unity checkpoint product/namespace")
  products.add(pm[0])
  probe=read_file(project/"Assets"/"B0123UnitySaveBridge001"/"Tests"/"B0160W22ScopeProbe.cs").decode()
  need(pm[0] in probe and "OnlyPrivateSavePath" in probe and
       "b0123_one_save_host_v2.checkpoint" in probe,
       "Private checkpoint guard absent/does not match original product")
  for tag,name in (("guard"+str(i),PROBE),("r5case"+str(i),SUITE+method)):
   exact_test_xml(root/(tag+".xml"),name)
   log=read_file(root/(tag+"-Editor.log"))
   need(VERSION.encode() in log and len(log)>0,
        "Unity Editor.log missing exact 6000.3.14f1 version")
   process=read_file(root/(tag+"-process.log"))
   need(process.startswith(b"exit_code=0\n"),
        "Original Unity process return code missing or nonzero")
 # Require no unrelated original NUnit logs, stale runner outputs or extra tests.
 observed_xml={p for p in provided if p.endswith(".xml")}
 expected_xml={f"{tag}{i}.xml" for i in range(6) for tag in ("guard","r5case")}
 need(observed_xml==expected_xml,"Extra/missing original NUnit test execution")
 native_logs={p for p in provided if p.endswith(".log")}
 expected_logs={f"{tag}{i}{suffix}" for i in range(6)
                for tag in ("guard","r5case")
                for suffix in ("-Editor.log","-process.log")}
 need(native_logs==expected_logs,"Extra/missing original Unity process/editor logs")
 return {"source_preflight":"NOT_APPLICABLE","native":"STRUCTURALLY_CONSISTENT_R5_6_OF_7_ONLY",
         "full_R5_G1":"HOLD","files_audited":len(provided),"original_R5":R5,
         "attestation":"NONE_NO_INDEPENDENT_PROOF_OF_EDITOR_EXECUTION"}

def main(argv=None):
 p=argparse.ArgumentParser()
 p.add_argument("evidence_directory",help="Local W22 B22-random directory; read-only")
 args=p.parse_args(argv)
 try:
  response=verify(Path(args.evidence_directory))
  print(json.dumps(response,sort_keys=True))
  return 0
 except (Rejected, OSError, UnicodeError, ValueError) as e:
  print("B0160_W23_R5_EVIDENCE_REJECTED="+str(e),file=sys.stderr)
  return 2

if __name__=="__main__":sys.exit(main())
