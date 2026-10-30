#!/usr/bin/env python3
"""Source-only QA tests: exact native #772 Unity sources and safe scratch; NO Unity."""
import json
from pathlib import Path
import subprocess
import tempfile
import unittest
import port772_native as v

class Physical772QA(unittest.TestCase):
 @classmethod
 def setUpClass(cls):
  cls.n=v.w22()
  cls.m=cls.n.runner()
  assert cls.m.SHA==v.R5
  cls.m.SHA=v.PORT
  cls.selected=v.corrected_port_source(cls.n,cls.m)
 def test_01_original_port_sha_present(self):
  self.assertEqual(self.n.git("cat-file","-t",v.PORT),"commit")
 def test_02_port_direct_selected_api_parent(self):
  self.assertEqual(self.n.git("merge-base",v.API,v.PORT),v.API)
 def test_03_selected_save_749_single_authority_untouched(self):
  self.assertEqual(self.n.git("merge-base",v.SAVE_SELECTED,v.SAVE_FIX_CANDIDATE),v.SAVE_SELECTED)
 def test_04_fix_773_tools_only_cannot_prove_unity_save_g1(self):
  changes=self.n.git("diff","--name-only",v.SAVE_SELECTED,v.SAVE_FIX_CANDIDATE).splitlines()
  self.assertEqual(len(changes),8)
  self.assertFalse(any(x.startswith("Assets/") or x.endswith(".asmdef") for x in changes))
 def test_05_original_port_three_NUnit_names(self):
  import re
  data=self.selected[v.TEST]
  self.assertEqual(tuple(x.decode() for x in
    re.findall(rb"\[UnityTest\]\s*public\s+IEnumerator\s+(\w+)\s*\(",data)),v.CASES)
 def test_06_all_original_csharp_blobs_exact(self):
  self.assertTrue(all(self.n.git("rev-parse",v.PORT+":"+path)==expected
                     for path,expected in v.PINS.items()))
 def test_07_unmodified_selected_api_source_bytes(self):
  p="Assets/B0123CanonicalSaveApi001/Core/CanonicalWorldGateway.cs"
  self.assertEqual(self.n.git("rev-parse",v.API+":"+p),
                   self.n.git("rev-parse",v.PORT+":"+p))
 def test_08_original_game_510_ledger_bytes(self):
  p="Assets/B0123SecureReattachV2/Runtime/AtomicDockLedger.cs"
  self.assertEqual(self.n.git("rev-parse",v.PORT+":"+p),
                   self.n.git("rev-parse","d94400d95d13db7f27e1533df05c9292d5380431:"+p))
 def test_09_original_port_9_meta_guids_two_asmdefs(self):
  added=self.n.git("diff","--name-only",v.API,v.PORT).splitlines()
  self.assertEqual(len([x for x in added if x.endswith(".meta")]),9)
  self.assertEqual(len([x for x in added if x.endswith(".asmdef")]),2)
  self.assertGreaterEqual(self.m.guid_check(self.selected),39)
 def test_10_separate_3_QA_only_checkpoints(self):
  products=set()
  for i in range(3):
   scoped,product=self.n.private_source(self.selected,i,"ab12cd34ef56",self.m)
   self.assertNotIn(product,products)
   self.assertEqual(self.m.guid_check(scoped),self.m.guid_check(self.selected)+1)
   self.assertIn(product.encode(),scoped[self.n.QA_PROBE])
   products.add(product)
  self.assertEqual(len(products),3)
 def test_11_real_original_physical_stage_rollback_three_cases(self):
  with tempfile.TemporaryDirectory(prefix="b0160w25-port-source-") as td:
   for i in range(3):
    scoped,_=self.n.private_source(self.selected,i,"ab12cd34ef56",self.m)
    archive=self.m.zip_data(scoped,{
     "original_source_sha":v.PORT,"native_unity":"NOT_RUN",
     "source_only_preflight":True,"codex_ack":False,"integrated":False,
     "native_meta_guids":self.m.guid_check(scoped),
     "file_sha256":{k:self.n.blake(val) for k,val in sorted(scoped.items())}})
    dst=Path(td)/("P"+str(i))
    self.m.stage(archive,scoped,dst)
    self.assertEqual((dst/self.n.PS).read_bytes(),scoped[self.n.PS])
    self.m.rollback(archive,scoped,dst)
    self.assertFalse(dst.exists())
 def test_12_foreign_empty_directory_strict_rollback_denied(self):
  with tempfile.TemporaryDirectory(prefix="b0160w25-port-source-") as td:
   scoped,_=self.n.private_source(self.selected,0,"ab12cd34ef56",self.m)
   archive=self.m.zip_data(scoped,{
     "original_source_sha":v.PORT,"native_unity":"NOT_RUN",
     "source_only_preflight":True,"codex_ack":False,"integrated":False,
     "native_meta_guids":self.m.guid_check(scoped),
     "file_sha256":{k:self.n.blake(val) for k,val in sorted(scoped.items())}})
   dst=Path(td)/"P0";self.m.stage(archive,scoped,dst)
   (dst/"FOREIGN_OWNER_EMPTY_DIR").mkdir()
   with self.assertRaises(AssertionError):self.m.rollback(archive,scoped,dst)
   self.assertTrue((dst/"FOREIGN_OWNER_EMPTY_DIR").exists())
 def test_13_real_direct_port_preflight_three_isolated_projects(self):
  with tempfile.TemporaryDirectory(prefix="b0160w25-port-source-") as td:
   self.assertEqual(v.main(["--mode","preflight","--scratch-parent",td]),0)
   roots=list(Path(td).glob("B22-*"))
   self.assertEqual(len(roots),1)
   r=json.loads((roots[0]/v.RECEIPT).read_text())
   self.assertEqual(r["native_G1"],"NOT_RUN")
   self.assertEqual(r["source_preflight"],
                    "PASS_3_PRIVATE_UNITY_PORT_PROJECTS_STAGED_AND_ROLLED_BACK")
   self.assertEqual(r["canonical_save_G1"],"NOT_RUN_NO_UNITY_SESSION_ASMDEF")
 def test_14_native_without_explicit_consent_denied(self):
  with tempfile.TemporaryDirectory(prefix="b0160w25-port-source-") as td:
   self.assertEqual(v.main(["--mode","native","--scratch-parent",td]),3)
 def test_15_scratch_inside_original_Git_checkout_denied(self):
  with self.assertRaises(AssertionError):
   self.n.safe_root(v.ROOT)
 def test_16_reject_owner_port_alt_paths(self):
  changed=self.n.git("diff","--name-only",v.API,v.PORT).splitlines()
  self.assertEqual(len(changed),21)
  self.assertTrue(all(x==v.WORKFLOW or x.startswith(v.OWNER+"/") or
     x=="Assets/B0123CanonicalRigPortW24.meta" or x.startswith("Assets/B0123CanonicalRigPortW24/")
     for x in changed))

if __name__=="__main__":unittest.main(verbosity=2)
