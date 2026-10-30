#!/usr/bin/env python3
"""Real Git blob and physical original R5 scratch preflight tests, NOT native Unity."""
import json
from pathlib import Path
import re
import tempfile
import unittest
import r5_native as n

class R5W22Checks(unittest.TestCase):
 @classmethod
 def setUpClass(cls):
  cls.m=n.runner()
  cls.source=n.selected_source()
 def test_01_original_R5_pinned_blobs(self):
  for path,blob in n.BLOBS.items():
   self.assertEqual(n.git("rev-parse",n.R5+":"+path),blob)
  self.assertEqual(len(n.METHODS),7)
 def test_02_frozen_original_510_and_556(self):
  self.assertEqual(n.git("merge-base",n.GAME,n.R5),n.GAME)
  self.assertEqual(n.git("merge-base",n.SAVE,n.R5),n.SAVE)
 def test_03_source_fixture_seven_exact_unity_cases(self):
  s=self.source[n.FIXTURE].decode()
  self.assertEqual(tuple(re.findall(r"\[UnityTest\]\s*public\s+IEnumerator\s+(\w+)\s*\(",s)),n.METHODS)
 def test_04_source_guard_identifies_known_coldrestart_mismatch(self):
  s=self.source[n.FIXTURE].decode()
  start=s.index("public IEnumerator "+n.HOLD_METHOD+"()")
  end=s.index("public IEnumerator WrongGeneration_",start)
  self.assertIn("Assert.AreEqual(4,restored.Generation)",s[start:end])
  self.assertIn("long next=newest+1;",self.source[n.CORE].decode())
 def test_05_six_nonmatching_original_tests_only(self):
  self.assertEqual(len(n.PARTIAL),6)
  self.assertNotIn(n.HOLD_METHOD,n.PARTIAL)
  self.assertIn("ComponentOnlyDestroy_RetainsLeaseUntilRealChildRigidbodyDestruction",n.PARTIAL)
 def test_06_original_guid_uniqueness(self):
  self.assertGreaterEqual(self.m.guid_check(self.source),34)
 def test_07_injected_private_probe_and_unique_player_settings(self):
  first,product=n.private_source(self.source,0,"01aabbccddee",self.m)
  second,another=n.private_source(self.source,1,"01aabbccddee",self.m)
  self.assertNotEqual(product,another)
  self.assertNotEqual(first[n.PS],second[n.PS])
  self.assertNotEqual(first[n.PS],self.source[n.PS])
  self.assertIn(product.encode(),first[n.QA_PROBE])
  self.assertNotIn(b'RoboRougelike',first[n.QA_PROBE])
 def test_08_real_source_physical_stage_and_strict_rollback(self):
  with tempfile.TemporaryDirectory(prefix="b0160-w22-test-") as p:
   base=Path(p)
   files,_=n.private_source(self.source,0,"012345abcdef",self.m)
   receipt={"original_source_sha":n.R5,"native_unity":"NOT_RUN",
            "source_only_preflight":True,"codex_ack":False,"integrated":False,
            "native_meta_guids":self.m.guid_check(files),
            "file_sha256":{k:n.blake(v) for k,v in sorted(files.items())}}
   archive=self.m.zip_data(files,receipt)
   root=base/"P0"
   self.m.stage(archive,files,root)
   self.assertTrue((root/n.QA_PROBE).is_file())
   self.m.rollback(archive,files,root)
   self.assertFalse(root.exists())
 def test_09_strict_rollback_refuses_foreign_empty_directory(self):
  with tempfile.TemporaryDirectory(prefix="b0160-w22-test-") as p:
   base=Path(p)
   files,_=n.private_source(self.source,0,"012345abcdef",self.m)
   receipt={"original_source_sha":n.R5,"native_unity":"NOT_RUN",
            "source_only_preflight":True,"codex_ack":False,"integrated":False,
            "native_meta_guids":self.m.guid_check(files),
            "file_sha256":{k:n.blake(v) for k,v in sorted(files.items())}}
   archive=self.m.zip_data(files,receipt)
   root=base/"P0"
   self.m.stage(archive,files,root)
   (root/"UNOWNED_EMPTY").mkdir()
   with self.assertRaises(AssertionError):self.m.rollback(archive,files,root)
   self.assertTrue((root/"UNOWNED_EMPTY").exists())
 def test_10_missing_licensed_editor_or_consent_is_not_native(self):
  with tempfile.TemporaryDirectory(prefix="b0160-w22-test-") as p:
   with self.assertRaises(AssertionError): n.safe_root(n.ROOT)
   with self.assertRaises(AssertionError):
    n.main(["--mode","native","--unity","Z:\\missing\\6000.3.14f1\\Editor\\Unity.exe",
            "--scratch-parent",p])
 def test_11_source_preflight_real_six_projects(self):
  with tempfile.TemporaryDirectory(prefix="b0160-w22-test-") as p:
   status=n.main(["--mode","preflight","--scratch-parent",p])
   self.assertEqual(status,0)
   roots=list(Path(p).glob("B22-*"))
   self.assertEqual(len(roots),1)
   d=json.loads((roots[0]/"P22-receipt.json").read_text())
   self.assertEqual(d["source_preflight"],"PASS_6_STAGE_ROLLBACK_NO_UNITY")
   self.assertEqual(d["native_G1"],"NOT_RUN")
   self.assertEqual(d["R5_FULL_NATIVE_G1"],"HOLD_ORIGINAL_COLDRESTART_GEN4_EXPECTS_GEN5")
 def test_12_no_producer_asset_edit_from_source_preflight(self):
  self.assertEqual(n.git("rev-parse",n.R5+":"+n.HOST),n.BLOBS[n.HOST])
  self.assertEqual(n.git("rev-parse",n.R5+":"+n.FIXTURE),n.BLOBS[n.FIXTURE])

if __name__=="__main__":unittest.main(verbosity=2)
