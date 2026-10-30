#!/usr/bin/env python3
"""22 adversarial mock receipts, including exact REAL #732 Git blob fixtures.
All NUnit XML + Editor.log here are MOCK data: never claim native Unity PASS.
"""
from pathlib import Path
import json
import subprocess
import tempfile
import unittest
import xml.etree.ElementTree as ET
import audit_fixed7 as v

ROOT=Path(__file__).resolve().parents[4]
def write(p,raw):
 p.parent.mkdir(parents=True,exist_ok=True)
 p.write_bytes(raw.encode() if isinstance(raw,str) else raw)
def fixture(full):
 root=ET.Element("test-run",{"result":"Passed","total":"1","passed":"1",
                            "failed":"0","skipped":"0"})
 ET.SubElement(root,"test-case",{"fullname":full,"result":"Passed"})
 return ET.tostring(root,encoding="utf-8")
def receipt(native=False):
 r={"package":"B01-60-P17W23-R5-COLDRESTART-FIXED-SEVEN-NATIVE",
   "source":v.SOURCE,"parent_selected_R5":v.R5,"parent_W22_QA":v.W22,
   "source_G0":"OWNER_CORRECTED_732_PENDING_INDEPENDENT_G0",
   "native_G1":"SEVEN_ORIGINAL_R5_NATIVE_CASES_COMPLETED_REQUIRES_INDEPENDENT_REVIEW" if native else "NOT_RUN",
   "full_R5_G1":"HOLD_PENDING_INDEPENDENT_SOURCE_G0_AND_OWNER_ATTESTATION" if native else "NOT_RUN",
   "coldrestart_generation":"g4->g5 original #556",
   "original_case_methods":list(v.CASES),
   "operator_g0_review_claim":native,
   "codex_ACK":"NONE","integrated":False,"released":False,
   "evidence_is_self_hashed_not_attested":True}
 if not native:r["source_preflight"]="PASS_7_PRIVATE_PROJECTS_STAGE_ROLLBACK_NO_UNITY"
 return r
def gen(root,native=False):
 r=receipt(native)
 if native:
  for i,name in enumerate(v.CASES):
   product=f"B0160W22Case{i}{i:012x}"
   write(root/f"P{i}"/v.SETTINGS,
         f"companyName: B0160W22{i:012x}\nproductName: {product}\n")
   write(root/f"P{i}"/"ProjectSettings/ProjectVersion.txt","m_EditorVersion: 6000.3.14f1\n")
   write(root/f"P{i}"/v.PROBE_FILE,
         f"OnlyPrivateSavePath {product} b0123_one_save_host_v2.checkpoint\n")
   for path in v.OTHER_BLOBS:
    raw=subprocess.check_output(["git","-C",str(ROOT),"show",v.SOURCE+":"+path],
                                stderr=subprocess.PIPE)
    if v.gitblob(raw)!=v.OTHER_BLOBS[path]:raise AssertionError("Source original SHA changed "+path)
    write(root/f"P{i}"/path,raw)
   for prefix,fullname in ((f"guard{i}",v.GUARD),(f"r5case{i}",v.SUITE+name)):
    write(root/(prefix+".xml"),fixture(fullname))
    write(root/(prefix+"-Editor.log"),"MOCK UNITY TEST RUNNER version 6000.3.14f1 never executed\n")
    write(root/(prefix+"-process.log"),"exit_code=0\nMOCK TEST DATA ONLY\n")
 return r,seal(root,r)
def seal(root,r):
 write(root/v.RECEIPT,json.dumps(r,indent=2,sort_keys=True)+"\n")
 got=v.selected_inventory(root)
 m={"unsigned":True,"note":"SHA256 consistency, not native attestation",
    "receipt":r,"evidence_sha256":got}
 write(root/v.MANIFEST,json.dumps(m,indent=2,sort_keys=True)+"\n")
 return m

class W24SevenNativeAudit(unittest.TestCase):
 @classmethod
 def setUpClass(cls):
  # Verify owner #732 commit exists locally and distinguishes B01-23 competing #750.
  p=subprocess.check_output(["git","-C",str(ROOT),"rev-parse",v.SOURCE+":"+v.FIXTURE]).decode().strip()
  assert p==v.OTHER_BLOBS[v.FIXTURE],p
 def setUp(self):
  self.t=tempfile.TemporaryDirectory(prefix="b0160w24-")
  self.root=Path(self.t.name)
  self.r,self.m=gen(self.root)
 def tearDown(self):self.t.cleanup()
 def bad(self):
  with self.assertRaises((v.Hold,OSError,ValueError,UnicodeError,AssertionError)):
   v.verify(self.root)
 def set_native(self):
  self.r,self.m=gen(self.root,True)
 def test_01_source_only_seven_case_preflight_valid(self):
  x=v.verify(self.root)
  self.assertEqual((x["native_G1"],x["verdict"]),("NOT_RUN","CONSISTENT_SOURCE_ONLY"))
 def test_02_source_only_is_not_real_native_attestation(self):
  self.assertEqual(v.verify(self.root)["independent_native_attestation"],"NONE")
 def test_03_reject_claimed_native_success_in_source_only(self):
  self.r["native_G1"]="PASS";seal(self.root,self.r);self.bad()
 def test_04_wrong_corrected_owner_source_sha(self):
  self.r["source"]="60c73a6f9ed2585cbb9bf7aeaad8c62eef0629cb"
  seal(self.root,self.r);self.bad()
 def test_05_wrong_original_selected_R5_parent(self):
  self.r["parent_selected_R5"]="0"*40;seal(self.root,self.r);self.bad()
 def test_06_missing_original_receipt(self):
  (self.root/v.RECEIPT).unlink();self.bad()
 def test_07_missing_original_SHA256_inventory(self):
  (self.root/v.MANIFEST).unlink();self.bad()
 def test_08_corrupt_receipt_hash_no_reseal(self):
  write(self.root/v.RECEIPT,(self.root/v.RECEIPT).read_bytes()+b"x")
  self.bad()
 def test_09_no_fake_full_R5_G1(self):
  self.r["full_R5_G1"]="PASS";seal(self.root,self.r);self.bad()
 def test_10_reject_correction_to_original_seven_names(self):
  self.r["original_case_methods"][2]="Attacker_Fake_ColdRestart"
  seal(self.root,self.r);self.bad()
 def test_11_no_forged_g0_ack_in_preflight(self):
  self.r["operator_g0_review_claim"]=True;seal(self.root,self.r);self.bad()
 def test_12_missing_source_rollback_marker(self):
  del self.r["source_preflight"];seal(self.root,self.r);self.bad()
 def test_13_foreign_root_file_even_resealed_rejected(self):
  write(self.root/"P99_foreign.txt","alien")
  self.bad()
 def test_14_path_traversal_inventory_rejected(self):
  self.m["evidence_sha256"]["../out"]="0"*64
  write(self.root/v.MANIFEST,json.dumps(self.m));self.bad()
 def test_15_symlinked_manifest_rejected(self):
  (self.root/v.MANIFEST).unlink()
  (self.root/v.MANIFEST).symlink_to(self.root/v.RECEIPT)
  self.bad()
 def test_16_native_seven_correction_source_structurally_valid_mock(self):
  self.set_native()
  x=v.verify(self.root)
  self.assertEqual(x["verdict"],"STRUCTURALLY_CONSISTENT_7_OF_7_ORIGINAL_R5")
  self.assertEqual(x["native_G1"],"UNATTESTED_FILES_ONLY")
  self.assertEqual(x["full_R5_G1"],"HOLD_PENDING_INDEPENDENT_REVIEW")
 def test_17_native_missing_corrected_seventh_NUnit_xml(self):
  self.set_native();(self.root/"r5case6.xml").unlink();self.bad()
 def test_18_native_wrong_coldrestart_generation_test_source_blob(self):
  self.set_native()
  write(self.root/f"P2/{v.FIXTURE}","FROZEN g4 ORIGINAL")
  seal(self.root,self.r);self.bad()
 def test_19_native_faked_extra_NUnit_case(self):
  self.set_native()
  tree=ET.fromstring((self.root/"r5case2.xml").read_bytes())
  ET.SubElement(tree,"test-case",{"fullname":v.SUITE+v.CASES[2],"result":"Passed"})
  write(self.root/"r5case2.xml",ET.tostring(tree))
  seal(self.root,self.r);self.bad()
 def test_20_native_missing_guard_session_scope(self):
  self.set_native();write(self.root/f"P0/{v.PROBE_FILE}","NO PRIVATE PATH PROOF")
  seal(self.root,self.r);self.bad()
 def test_21_native_nonzero_real_process_exit(self):
  self.set_native()
  write(self.root/"guard0-process.log","exit_code=3\n")
  seal(self.root,self.r);self.bad()
 def test_22_native_wrong_editor_version(self):
  self.set_native()
  write(self.root/"r5case3-Editor.log","OTHER Unity version6000.4.0f1\n")
  seal(self.root,self.r);self.bad()
 def test_23_native_duplicate_private_product(self):
  self.set_native()
  write(self.root/"P1/ProjectSettings/ProjectSettings.asset",
        "companyName: B0160W22000000000000\nproductName: B0160W22Case0000000000000\n")
  seal(self.root,self.r);self.bad()
 def test_24_native_declares_no_operator_g0_review(self):
  self.set_native()
  self.r["operator_g0_review_claim"]=False;seal(self.root,self.r);self.bad()
 def test_25_native_extra_uninventoried_XML_rejected(self):
  self.set_native()
  write(self.root/"r5case99.xml",fixture("Fake.Test"))
  self.bad()
 def test_26_native_source_reported_error_rejected(self):
  self.set_native()
  self.r["source_or_native_error"]="Unity failed"
  seal(self.root,self.r);self.bad()

if __name__=="__main__":unittest.main(verbosity=2)
