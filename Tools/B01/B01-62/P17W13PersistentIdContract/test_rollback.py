#!/usr/bin/env python3
"""W13 B01-62 actual ORIGINAL #510 rollback RED vs accepted B01-40 #521 G0 safe rollback."""
import hashlib
import importlib.util
import os
from pathlib import Path
import tempfile
import unittest

def module(path,label):
 spec=importlib.util.spec_from_file_location(label,path)
 m=importlib.util.module_from_spec(spec)
 spec.loader.exec_module(m)
 return m

class OriginalRollbackProtection(unittest.TestCase):
 @classmethod
 def setUpClass(cls):
  producer=Path(os.environ["B01_W13_PRODUCER"])
  cls.old=module(producer/"Tools/B01/B01-23/P17W10SecureReattachV2/package.py","frozen510")
  cls.safe=module(os.environ["B01_W13_ACCEPTED_G0"],"acceptedG0")
  cls.raw=cls.old.build()
  cls.files=cls.old.verify(cls.raw)
 def test_01_pinned_original_zip_exact_35_members(self):
  self.assertEqual(len(self.files),35)
  self.assertEqual(len(self.raw),99666)
  self.assertEqual(hashlib.sha256(self.raw).hexdigest(),
   "d4ddfa3528ef0416aefffca72d884000f3b0c9959fb8dff53096487ef95c9387")
 def test_02_original_stage_positive_new_scratch(self):
  with tempfile.TemporaryDirectory() as t:
   dest=Path(t)/"staged";self.old.stage(self.raw,dest)
   self.assertTrue((dest/"PACKAGE_MANIFEST.json").is_file())
   self.old.rollback(self.raw,dest)
   self.assertFalse(dest.exists())
 def test_03_ACTUAL_ORIGINAL_510_RED_deletes_foreign_empty_folder(self):
  with tempfile.TemporaryDirectory() as t:
   dest=Path(t)/"old";self.old.stage(self.raw,dest)
   foreign=dest/"foreign-owner-empty"
   foreign.mkdir()
   self.old.rollback(self.raw,dest)
   # QA RED expected witness, NEVER acceptance: old producer removed someone else's folder.
   self.assertFalse(dest.exists(),"Unexpected original #510 rollback fix; review frozen SHA")
   self.assertFalse(foreign.exists(),"Original did not reproduce unsafe foreign-dir deletion")
 def test_04_ACTUAL_ORIGINAL_510_RED_deletes_nested_foreign_empty_folders(self):
  with tempfile.TemporaryDirectory() as t:
   dest=Path(t)/"old";self.old.stage(self.raw,dest)
   foreign=dest/"Assets"/"nested-foreign"/"empty"
   foreign.mkdir(parents=True)
   self.old.rollback(self.raw,dest)
   self.assertFalse(foreign.exists())
 def test_05_independent_G0_rejects_foreign_empty_folder_and_preserves_it(self):
  with tempfile.TemporaryDirectory() as t:
   dest=Path(t)/"safe";self.safe.stage_atomic(self.files,dest)
   foreign=dest/"new-owner-empty";foreign.mkdir()
   with self.assertRaises(self.safe.GateFailure):
    self.safe.rollback_strict(self.files,dest)
   self.assertTrue(foreign.is_dir())
   foreign.rmdir()
   self.safe.rollback_strict(self.files,dest)
   self.assertFalse(dest.exists())
 def test_06_independent_G0_rejects_nested_empty_folder(self):
  with tempfile.TemporaryDirectory() as t:
   dest=Path(t)/"safe";self.safe.stage_atomic(self.files,dest)
   foreign=dest/"Assets"/"nested-by-other"/"empty";foreign.mkdir(parents=True)
   with self.assertRaises(self.safe.GateFailure):
    self.safe.rollback_strict(self.files,dest)
   self.assertTrue(foreign.exists())
 def test_07_independent_G0_refuses_modified_owned_member(self):
  with tempfile.TemporaryDirectory() as t:
   dest=Path(t)/"safe";self.safe.stage_atomic(self.files,dest)
   marker=dest/"PACKAGE_MANIFEST.json";marker.write_text("modified")
   with self.assertRaises(self.safe.GateFailure):self.safe.rollback_strict(self.files,dest)
   self.assertEqual(marker.read_text(),"modified")
 def test_08_independent_G0_refuses_foreign_file(self):
  with tempfile.TemporaryDirectory() as t:
   dest=Path(t)/"safe";self.safe.stage_atomic(self.files,dest)
   marker=dest/"foreign.txt";marker.write_text("not ours")
   with self.assertRaises(self.safe.GateFailure):self.safe.rollback_strict(self.files,dest)
   self.assertEqual(marker.read_text(),"not ours")
 def test_09_independent_G0_refuses_missing_owned_member(self):
  with tempfile.TemporaryDirectory() as t:
   dest=Path(t)/"safe";self.safe.stage_atomic(self.files,dest)
   (dest/"PACKAGE_MANIFEST.json").unlink()
   with self.assertRaises(self.safe.GateFailure):self.safe.rollback_strict(self.files,dest)
   self.assertTrue(dest.exists())
 def test_10_independent_G0_refuses_symlink_in_stage_if_supported(self):
  with tempfile.TemporaryDirectory() as t:
   dest=Path(t)/"safe";self.safe.stage_atomic(self.files,dest)
   marker=dest/"external-link"
   try:marker.symlink_to(Path(t)/"other")
   except (OSError,NotImplementedError):self.skipTest("no symlink creation permission")
   with self.assertRaises(self.safe.GateFailure):self.safe.rollback_strict(self.files,dest)
   self.assertTrue(marker.is_symlink())
 def test_11_independent_G0_clean_rollback_succeeds(self):
  with tempfile.TemporaryDirectory() as t:
   dest=Path(t)/"safe";self.safe.stage_atomic(self.files,dest)
   self.safe.rollback_strict(self.files,dest)
   self.assertFalse(dest.exists())
 def test_12_independent_G0_atomic_stage_crash_does_not_touch_target(self):
  with tempfile.TemporaryDirectory() as t:
   dest=Path(t)/"safe"
   with self.assertRaises(self.safe.GateFailure):
    self.safe.stage_atomic(self.files,dest,inject_after=3)
   self.assertFalse(dest.exists())
   self.assertEqual(list(Path(t).iterdir()),[])
 def test_13_independent_G0_refuses_existing_destination(self):
  with tempfile.TemporaryDirectory() as t:
   dest=Path(t)/"safe";dest.mkdir()
   with self.assertRaises(self.safe.GateFailure):
    self.safe.stage_atomic(self.files,dest)
   self.assertTrue(dest.is_dir())
 def test_14_expected_fix_is_RECEIVER_ONLY_not_mutated_producer_source(self):
  original=(Path(os.environ["B01_W13_PRODUCER"])/"Tools/B01/B01-23/P17W10SecureReattachV2/package.py").read_text()
  self.assertIn("if found!=set(original):",original)
  self.assertNotIn("expected_dirs=set()",original)
  receiver=Path(os.environ["B01_W13_ACCEPTED_G0"]).read_text()
  self.assertIn("directories == expected_dirs",receiver)
if __name__=="__main__":unittest.main(verbosity=2)
