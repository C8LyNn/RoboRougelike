#!/usr/bin/env python3
"""Independent B01-62 source/receiver QA against byte-exact original #587 physical package."""
import hashlib
import importlib.util
import os
from pathlib import Path
import subprocess
import tempfile
import unittest

SELECTED="ad7904eca571c742257551fa8103f266fcb9b752"
GAME="d94400d95d13db7f27e1533df05c9292d5380431"
SAVE="33094f154c7b91119b36d58aab285e6e83c5de57"
ZIP="fec19b2e244ca9f37f6e9c069f70c615509b8d4b06264bece877d2a38168e89c"

class ReceiverChecks(unittest.TestCase):
 @classmethod
 def setUpClass(cls):
  repo=Path(os.environ["B0162_SELECTED587_REPO"])
  cls.repo=repo
  selfpath=repo/"Tools/B01/B01-23/P17W15UnitySaveBridge001/package.py"
  spec=importlib.util.spec_from_file_location("unmodified_b0123_selected587_original",selfpath)
  cls.module=importlib.util.module_from_spec(spec)
  spec.loader.exec_module(cls.module)
  assert subprocess.check_output(["git","-C",str(repo),"rev-parse","HEAD"],text=True).strip()==SELECTED
  cls.raw=cls.module.build()
  cls.members=cls.module.verify(cls.raw)
 def test_01_actual_original_package_frozen_zip(self):
  self.assertEqual(len(self.raw),71047)
  self.assertEqual(len(self.members),23)
  self.assertEqual(hashlib.sha256(self.raw).hexdigest(),ZIP)
 def test_02_source_only_selected_exact_contract(self):
  self.assertIn(SAVE,self.module.source("Tools/B01/B01-23/P17W15UnitySaveBridge001/README.md").decode())
 def test_03_selected_only_excludes_585_alternative(self):
  self.assertTrue(all("B0123SecureV2HostBridge001" not in p for p in self.members))
 def test_04_selected_only_excludes_frozen_parent_game(self):
  self.assertTrue(all("B0123SecureReattachV2" not in p for p in self.members))
 def test_05_foreign_empty_dir_must_preserve_foreign_ownership(self):
  with tempfile.TemporaryDirectory(prefix="b0162-w16-receiver-") as t:
   target=Path(t)/"receiver"
   self.module.stage(self.raw,target)
   foreign=target/"B0123Foreign"/"subempty"
   foreign.mkdir(parents=True)
   with self.assertRaises(ValueError):self.module.rollback(self.raw,target)
   self.assertTrue(foreign.is_dir())
   foreign.rmdir();foreign.parent.rmdir()
   self.module.rollback(self.raw,target)
   self.assertFalse(target.exists())
 def test_06_foreign_nested_empty_in_assets_must_hold(self):
  with tempfile.TemporaryDirectory(prefix="b0162-w16-receiver-") as t:
   target=Path(t)/"receiver";self.module.stage(self.raw,target)
   alien=target/"Assets"/"B0123UnitySaveBridge001"/"not-owned-empty"
   alien.mkdir()
   with self.assertRaises(ValueError):self.module.rollback(self.raw,target)
   self.assertTrue(alien.is_dir())
 def test_07_foreign_file_must_preserve(self):
  with tempfile.TemporaryDirectory(prefix="b0162-w16-receiver-") as t:
   target=Path(t)/"receiver";self.module.stage(self.raw,target)
   alien=target/"do-not-delete.txt";alien.write_text("foreign")
   with self.assertRaises(ValueError):self.module.rollback(self.raw,target)
   self.assertEqual(alien.read_text(),"foreign")
 def test_08_dirty_original_file_must_preserve(self):
  with tempfile.TemporaryDirectory(prefix="b0162-w16-receiver-") as t:
   target=Path(t)/"receiver";self.module.stage(self.raw,target)
   f=target/next(x for x in self.members if x.endswith("AtomicWorldSwap.cs"))
   f.write_text("foreign-change")
   with self.assertRaises(ValueError):self.module.rollback(self.raw,target)
   self.assertEqual(f.read_text(),"foreign-change")
 def test_09_missing_owned_member_must_hold(self):
  with tempfile.TemporaryDirectory(prefix="b0162-w16-receiver-") as t:
   target=Path(t)/"receiver";self.module.stage(self.raw,target)
   (target/"PACKAGE_MANIFEST.json").unlink()
   with self.assertRaises(ValueError):self.module.rollback(self.raw,target)
   self.assertTrue(target.exists())
 def test_10_double_stage_must_refuse(self):
  with tempfile.TemporaryDirectory(prefix="b0162-w16-receiver-") as t:
   target=Path(t)/"receiver";self.module.stage(self.raw,target)
   with self.assertRaises(ValueError):self.module.stage(self.raw,target)
   self.assertTrue(target.exists())
 def test_11_owner_clean_staging_rollback(self):
  with tempfile.TemporaryDirectory(prefix="b0162-w16-receiver-") as t:
   target=Path(t)/"receiver"
   self.module.stage(self.raw,target)
   self.module.rollback(self.raw,target)
   self.assertFalse(target.exists())
 def test_12_symlink_must_fail_closed_if_supported(self):
  with tempfile.TemporaryDirectory(prefix="b0162-w16-receiver-") as t:
   target=Path(t)/"receiver";self.module.stage(self.raw,target)
   link=target/"FOREIGN_LINK"
   try:link.symlink_to(t,target_is_directory=True)
   except (OSError,NotImplementedError):self.skipTest("symlink permission absent")
   with self.assertRaises(ValueError):self.module.rollback(self.raw,target)
   self.assertTrue(link.is_symlink())
if __name__=="__main__":
 unittest.main(verbosity=2)
