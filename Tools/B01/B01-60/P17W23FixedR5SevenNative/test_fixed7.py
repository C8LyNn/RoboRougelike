#!/usr/bin/env python3
"""15 actual source Git/physical scratch checks, no native Unity executed."""
from pathlib import Path
import tempfile
import unittest
import run_fixed7 as v

class FixedR5SevenOriginal(unittest.TestCase):
 @classmethod
 def setUpClass(cls):
  cls.n=v.w22()
  cls.m=v.corrected_stage_runner(cls.n)
  cls.files=v.original_corrected_source(cls.n,cls.m)
 def test_01_owner_R5_old_exact_frozen_commit(self):
  self.assertEqual(self.n.git("merge-base",v.R5,v.FIXED),v.R5)
 def test_02_corrected_fixture_exact_git_blob(self):
  self.assertEqual(self.n.git("rev-parse",v.FIXED+":"+v.FILE),v.FIXTURE_BLOB)
 def test_03_original_frozen_fixture_unchanged(self):
  self.assertEqual(self.n.git("rev-parse",v.R5+":"+v.FILE),self.n.BLOBS[v.FILE])
 def test_04_host_byte_identical(self):
  self.assertEqual(self.files[self.n.HOST],
                   __import__("subprocess").check_output(
                    ["git","-C",str(v.ROOT),"show",v.R5+":"+self.n.HOST]))
 def test_05_core_byte_identical(self):
  self.assertEqual(self.n.git("rev-parse",v.FIXED+":"+self.n.CORE),
                   self.n.BLOBS[self.n.CORE])
 def test_06_retirement_lease_byte_identical(self):
  self.assertEqual(self.n.git("rev-parse",v.FIXED+":"+self.n.RETIRE),
                   self.n.BLOBS[self.n.RETIRE])
 def test_07_exact_seven_original_NUnit_names(self):
  import re
  names=re.findall(rb"\[UnityTest\]\s*public\s+IEnumerator\s+(\w+)\s*\(",self.files[v.FILE])
  self.assertEqual(tuple(x.decode() for x in names),self.n.METHODS)
 def test_08_true_original_g4_to_g5_source(self):
  s=self.files[v.FILE].decode()
  start=s.index("public IEnumerator "+v.FULL_METHOD+"()")
  end=s.index("public IEnumerator WrongGeneration_",start)
  self.assertIn("TryRestoreBytes(saved,41,5)",s[start:end])
  self.assertIn("Assert.AreEqual(5,decoded.Generation)",s[start:end])
  self.assertNotIn("Assert.AreEqual(4,restored.Generation)",s[start:end])
 def test_09_exact_producer_scope_four_paths(self):
  self.assertEqual(set(self.n.git("diff","--name-only",v.R5,v.FIXED).splitlines()),
                   v.CORRECTION_FILES)
 def test_10_guid_closure_and_all_scenes_original(self):
  self.assertGreaterEqual(self.m.guid_check(self.files),34)
  self.assertIn("6000.3.14f1",self.files["ProjectSettings/ProjectVersion.txt"].decode())
 def test_11_seven_separate_private_products(self):
  products=set()
  for i in range(7):
   scope,product=self.n.private_source(self.files,i,"0a12b34c56de",self.m)
   self.assertEqual(self.m.guid_check(scope),self.m.guid_check(self.files)+1)
   self.assertNotIn(product,products)
   self.assertIn(product.encode(),scope[self.n.QA_PROBE])
   products.add(product)
  self.assertEqual(len(products),7)
 def test_12_physically_stage_7_and_rollback(self):
  with tempfile.TemporaryDirectory(prefix="b0160-w23-fixed-") as td:
   for i in range(7):
    items,_=self.n.private_source(self.files,i,"012345abcdef",self.m)
    archive=self.m.zip_data(items,{
     "original_source_sha":v.FIXED,"native_unity":"NOT_RUN",
     "source_only_preflight":True,"codex_ack":False,"integrated":False,
     "native_meta_guids":self.m.guid_check(items),
     "file_sha256":{k:self.n.blake(b) for k,b in sorted(items.items())}})
    proj=Path(td)/("P"+str(i))
    self.m.stage(archive,items,proj)
    self.assertEqual((proj/self.n.PS).read_bytes(),items[self.n.PS])
    self.m.rollback(archive,items,proj)
    self.assertFalse(proj.exists())
 def test_13_foreign_empty_directory_rollback_refused(self):
  with tempfile.TemporaryDirectory(prefix="b0160-w23-fixed-") as td:
   items,_=self.n.private_source(self.files,0,"012345abcdef",self.m)
   archive=self.m.zip_data(items,{
     "original_source_sha":v.FIXED,"native_unity":"NOT_RUN",
     "source_only_preflight":True,"codex_ack":False,"integrated":False,
     "native_meta_guids":self.m.guid_check(items),
     "file_sha256":{k:self.n.blake(b) for k,b in sorted(items.items())}})
   proj=Path(td)/"P0";self.m.stage(archive,items,proj)
   (proj/"FOREIGN_EMPTY").mkdir()
   with self.assertRaises(AssertionError):self.m.rollback(archive,items,proj)
   self.assertTrue((proj/"FOREIGN_EMPTY").exists())
 def test_14_missing_native_consent_never_runs_unity(self):
  with tempfile.TemporaryDirectory(prefix="b0160-w23-fixed-") as td:
   self.assertEqual(v.main(["--mode","native","--scratch-parent",td]),3)
 def test_15_real_original_fixed7_source_only_preflight(self):
  with tempfile.TemporaryDirectory(prefix="b0160-w23-fixed-") as td:
   self.assertEqual(v.main(["--mode","preflight","--scratch-parent",td]),0)
   roots=list(Path(td).glob("B22-*"))
   self.assertEqual(len(roots),1)
   import json
   receipt=json.loads((roots[0]/"P23-fixed7-receipt.json").read_text())
   self.assertEqual(receipt["native_G1"],"NOT_RUN")
   self.assertEqual(receipt["source_preflight"],"PASS_7_PRIVATE_PROJECTS_STAGE_ROLLBACK_NO_UNITY")

if __name__=="__main__":unittest.main(verbosity=2)
