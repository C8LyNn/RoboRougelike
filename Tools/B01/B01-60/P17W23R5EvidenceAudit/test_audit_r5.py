#!/usr/bin/env python3
"""Synthetic adversarial tests; NEVER evidence of actual Unity/PhysX execution."""
import copy
import json
from pathlib import Path
import tempfile
import unittest
import xml.etree.ElementTree as ET
import audit_r5 as v

def receipt(native=False):
 out={"package":v.PACKAGE,"selected_host_R5_SHA":v.R5,
      "original_510_SHA":v.GAME,"original_556_SHA":v.SAVE,
      "native_G1":"PARTIAL_R5_SIX_OF_SEVEN_ONLY" if native else "NOT_RUN",
      "R5_FULL_NATIVE_G1":v.FULL_HOLD,"source_G0":v.G0,
      "Codex_ACK":"NONE","source_methods":list(v.METHODS),
      "native_partial_scope":list(v.PARTIAL),"merged":False,"uploaded":False}
 if not native:out["source_preflight"]="PASS_6_STAGE_ROLLBACK_NO_UNITY"
 return out
def write(path,bytes_or_text):
 path.parent.mkdir(parents=True,exist_ok=True)
 path.write_bytes(bytes_or_text.encode() if isinstance(bytes_or_text,str) else bytes_or_text)
def one_xml(fullname):
 root=ET.Element("test-run",{"result":"Passed","total":"1","passed":"1",
                             "failed":"0","skipped":"0"})
 ET.SubElement(root,"test-case",{"fullname":fullname,"result":"Passed"})
 return ET.tostring(root,encoding="utf-8")
def reseal(root,r):
 write(root/v.RECEIPT,json.dumps(r,sort_keys=True,indent=2)+"\n")
 # The original W22 computes checksums of root P-prefixed entries,
 # and every original XML/Editor/process log; no fabricated signing.
 hashes=v.scoped_hashes(root)
 inventory={"unsigned":True,
            "note":"SHA256 consistency, not native attestation",
            "receipt":r,"evidence_sha256":hashes}
 write(root/v.EVIDENCE,json.dumps(inventory,sort_keys=True,indent=2)+"\n")
 return inventory
def synthetic(root, native=False):
 r=receipt(native)
 if native:
  for i,method in enumerate(v.PARTIAL):
   product=f"B0160W22Case{i}"+f"{i:012x}"
   write(root/f"P{i}/ProjectSettings/ProjectSettings.asset",
         "companyName: B0160W22\nproductName: "+product+"\n")
   write(root/f"P{i}/Assets/B0123UnitySaveBridge001/Tests/B0160W22ScopeProbe.cs",
         "OnlyPrivateSavePath "+product+" b0123_one_save_host_v2.checkpoint\n")
   for tag,name in ((f"guard{i}",v.PROBE),(f"r5case{i}",v.SUITE+method)):
    write(root/(tag+".xml"),one_xml(name))
    write(root/(tag+"-Editor.log"),"Unity "+v.VERSION+" synthetic fixture; NOT real editor\n")
    write(root/(tag+"-process.log"),"exit_code=0\nSYNTHETIC_ONLY\n")
 return r,reseal(root,r)

class W23Adversarial(unittest.TestCase):
 def setUp(self):
  self.tmp=tempfile.TemporaryDirectory(prefix="b0160-w23-")
  self.root=Path(self.tmp.name)
  self.r,self.manifest=synthetic(self.root)
 def tearDown(self):self.tmp.cleanup()
 def is_red(self):
  with self.assertRaises((v.Rejected,ValueError,UnicodeError,OSError)):
   v.verify(self.root)
 def switch_native(self):
  for f in self.root.iterdir(): f.unlink()
  self.r,self.manifest=synthetic(self.root,True)
 def test_01_source_only_exact_accept(self):
  self.assertEqual(v.verify(self.root)["native"],"NOT_RUN")
 def test_02_only_signed_sha_semantics_never_attestation(self):
  self.assertEqual(v.verify(self.root)["attestation"],"NONE")
 def test_03_manipulated_source_receipt_file_sha_rejected(self):
  write(self.root/v.RECEIPT,(self.root/v.RECEIPT).read_bytes()+b"x")
  self.is_red()
 def test_04_missing_sha_manifest_rejected(self):
  (self.root/v.EVIDENCE).unlink();self.is_red()
 def test_05_missing_receipt_rejected(self):
  (self.root/v.RECEIPT).unlink();self.is_red()
 def test_06_false_native_G1_pass_rejected(self):
  self.r["native_G1"]="PASS";reseal(self.root,self.r);self.is_red()
 def test_07_false_full_R5_G1_pass_rejected(self):
  self.r["R5_FULL_NATIVE_G1"]="PASS";reseal(self.root,self.r);self.is_red()
 def test_08_wrong_author_selected_sha_rejected(self):
  self.r["selected_host_R5_SHA"]="0"*40;reseal(self.root,self.r);self.is_red()
 def test_09_wrong_original_game_sha_rejected(self):
  self.r["original_510_SHA"]="0"*40;reseal(self.root,self.r);self.is_red()
 def test_10_source_only_with_rogue_xml_rejected(self):
  write(self.root/"guard0.xml",one_xml(v.PROBE))
  reseal(self.root,self.r);self.is_red()
 def test_11_source_only_with_project_rejected(self):
  write(self.root/"P0/Assets/fake.cs","a")
  reseal(self.root,self.r);self.is_red()
 def test_12_source_only_bad_rollback_marker_rejected(self):
  self.r["source_preflight"]="PASS_NATIVE";reseal(self.root,self.r);self.is_red()
 def test_13_forged_Codex_ACK_rejected(self):
  self.r["Codex_ACK"]="ACCEPTED";reseal(self.root,self.r);self.is_red()
 def test_14_unsigned_manifest_false_rejected(self):
  self.manifest["unsigned"]=False
  write(self.root/v.EVIDENCE,json.dumps(self.manifest));self.is_red()
 def test_15_empty_SHA_inventory_rejected(self):
  self.manifest["evidence_sha256"]={}
  write(self.root/v.EVIDENCE,json.dumps(self.manifest));self.is_red()
 def test_16_traversal_SHA_key_rejected(self):
  self.manifest["evidence_sha256"]["../escape.txt"]="0"*64
  write(self.root/v.EVIDENCE,json.dumps(self.manifest));self.is_red()
 def test_17_symlinked_source_manifest_rejected(self):
  (self.root/v.EVIDENCE).unlink()
  (self.root/v.EVIDENCE).symlink_to(self.root/v.RECEIPT)
  self.is_red()
 def test_18_native_six_structurally_consistent_only(self):
  self.switch_native()
  d=v.verify(self.root)
  self.assertEqual(d["native"],"STRUCTURALLY_CONSISTENT_R5_6_OF_7_ONLY")
  self.assertEqual(d["attestation"],"NONE_NO_INDEPENDENT_PROOF_OF_EDITOR_EXECUTION")
  self.assertEqual(d["full_R5_G1"],"HOLD")
 def test_19_native_corrupted_one_xml_rejected(self):
  self.switch_native()
  write(self.root/"guard2.xml","<broken")
  reseal(self.root,self.r);self.is_red()
 def test_20_native_wrong_full_NUnit_identity_rejected(self):
  self.switch_native()
  write(self.root/"r5case3.xml",one_xml("Attacker.FakePass"))
  reseal(self.root,self.r);self.is_red()
 def test_21_native_duplicate_extra_test_case_rejected(self):
  self.switch_native()
  root=ET.fromstring((self.root/"r5case0.xml").read_bytes())
  ET.SubElement(root,"test-case",{"fullname":v.SUITE+v.PARTIAL[0],"result":"Passed"})
  write(self.root/"r5case0.xml",ET.tostring(root,encoding="utf-8"))
  reseal(self.root,self.r);self.is_red()
 def test_22_native_lied_aggregate_NUnit_counters_rejected(self):
  self.switch_native()
  root=ET.fromstring((self.root/"guard1.xml").read_bytes())
  root.set("total","2")
  write(self.root/"guard1.xml",ET.tostring(root,encoding="utf-8"))
  reseal(self.root,self.r);self.is_red()
 def test_23_native_skipped_test_rejected(self):
  self.switch_native()
  root=ET.fromstring((self.root/"r5case0.xml").read_bytes())
  list(root)[0].set("result","Skipped")
  write(self.root/"r5case0.xml",ET.tostring(root,encoding="utf-8"))
  reseal(self.root,self.r);self.is_red()
 def test_24_native_wrong_editor_version_rejected(self):
  self.switch_native()
  write(self.root/"guard0-Editor.log","Unity 6000.4.0f1 SYNTHETIC\n")
  reseal(self.root,self.r);self.is_red()
 def test_25_native_process_exit_nonzero_rejected(self):
  self.switch_native()
  write(self.root/"r5case4-process.log","exit_code=4\n")
  reseal(self.root,self.r);self.is_red()
 def test_26_native_missing_guard_XML_rejected(self):
  self.switch_native()
  (self.root/"guard5.xml").unlink()
  reseal(self.root,self.r);self.is_red()
 def test_27_native_uninventoried_xml_rejected(self):
  self.switch_native()
  write(self.root/"foreign.xml",one_xml("Unknown.Fake"))
  reseal(self.root,self.r);self.is_red()
 def test_28_native_duplicate_private_product_rejected(self):
  self.switch_native()
  write(self.root/"P1/ProjectSettings/ProjectSettings.asset",
        "companyName: B0160W22\nproductName: B0160W22Case0"+"0"*12+"\n")
  reseal(self.root,self.r);self.is_red()
 def test_29_native_guard_probe_wrong_product_rejected(self):
  self.switch_native()
  write(self.root/"P0/Assets/B0123UnitySaveBridge001/Tests/B0160W22ScopeProbe.cs",
        "OnlyPrivateSavePath B0160W22Case0"+"0"*12+" MISSING GUARD\n")
  reseal(self.root,self.r);self.is_red()
 def test_30_native_missing_core_source_guard_rejected(self):
  self.switch_native()
  (self.root/"P0/Assets/B0123UnitySaveBridge001/Tests/B0160W22ScopeProbe.cs").unlink()
  reseal(self.root,self.r);self.is_red()
 def test_31_native_error_report_must_reject(self):
  self.switch_native()
  self.r["source_or_native_error"]="Unity crashed"
  reseal(self.root,self.r);self.is_red()
 def test_32_source_only_claim_missing_runner_outputs_rejected(self):
  self.r["native_G1"]="PARTIAL_R5_SIX_OF_SEVEN_ONLY"
  self.r.pop("source_preflight")
  reseal(self.root,self.r);self.is_red()
 def test_33_native_symlinked_project_file_rejected(self):
  self.switch_native()
  p=self.root/"P0/ProjectSettings/ProjectSettings.asset"
  original=p.read_bytes();p.unlink()
  a=self.root/"P0/ProjectSettings/linked-data"
  a.write_bytes(original)
  p.symlink_to(a)
  self.is_red()

 def test_34_foreign_root_text_rejected_even_if_resealed(self):
  write(self.root/"PRIVATE_OWNER_DATA.txt","not an original W22 native evidence item")
  reseal(self.root,self.r);self.is_red()
 def test_35_fake_root_prefix_not_allowed_in_native(self):
  self.switch_native()
  write(self.root/"P99-extra-claims.txt","fake selected SHA or G1")
  reseal(self.root,self.r);self.is_red()

if __name__=="__main__":unittest.main(verbosity=2)
