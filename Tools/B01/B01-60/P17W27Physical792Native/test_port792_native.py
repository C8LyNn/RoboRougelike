#!/usr/bin/env python3
"""Original Git SHA source/scratch QA for selected #792 rollback regression.

These are NOT Unity Editor/PhysX results. Never label PlayMode PASS from CI.
"""
import json
import tempfile
from pathlib import Path
import unittest
import port792_native as v

class Original792FourNative(unittest.TestCase):
 @classmethod
 def setUpClass(cls):
  cls.q,cls.n=v.old_runner()
  cls.m=cls.n.runner()
  cls.source=v.selected_project(cls.n,cls.q,cls.m)
 def test_01_exact_792_owner_sha(self):
  self.assertEqual(self.n.git("cat-file","-t",v.PORT792),"commit")
 def test_02_792_derived_only_772(self):
  self.assertEqual(self.n.git("merge-base",v.PORT772,v.PORT792),v.PORT772)
 def test_03_four_original_unity_playmode_methods(self):
  import re
  names=tuple(x.decode() for x in re.findall(
   rb"\[UnityTest\]\s*public\s+IEnumerator\s+(\w+)\s*\(",
   self.source[v.FIXTURE]))
  self.assertEqual(names,v.CASES)
 def test_04_current_velocity_regression_is_real_updated_source(self):
  self.assertNotEqual(self.n.git("rev-parse",v.PORT772+":"+v.RUNTIME),
                      self.n.git("rev-parse",v.PORT792+":"+v.RUNTIME))
  self.assertIn(b"resumeLinear",self.source[v.RUNTIME])
  self.assertIn(b"resumeAngular",self.source[v.RUNTIME])
 def test_05_owner_fixture_blob_exact(self):
  self.assertEqual(v.blob(self.source[v.FIXTURE]),v.FIXTURE_BLOB)
 def test_06_owner_physics_runtime_blob_exact(self):
  self.assertEqual(v.blob(self.source[v.RUNTIME]),v.RUNTIME_BLOB)
 def test_07_no_competing_owner_save_source_overlay(self):
  self.assertFalse(any("CanonicalWholeGame" in p or
     "CanonicalSingleAuthority" in p for p in self.source))
 def test_08_selected_787_797_single_save_ancestry(self):
  self.assertEqual(self.n.git("merge-base",v.SAVE787,v.FULL797),v.SAVE787)
 def test_09_selected_780_and_750_objects_exist(self):
  self.assertEqual(self.n.git("cat-file","-t",v.ECON780),"commit")
  self.assertEqual(self.n.git("cat-file","-t",v.R5_750),"commit")
 def test_10_owner_asmdef_unchanged_from_old_772(self):
  for p in (self.q.RUNTIME_ASM,self.q.PLAY_ASM):
   self.assertEqual(self.n.git("rev-parse",v.PORT772+":"+p),
                    self.n.git("rev-parse",v.PORT792+":"+p))
 def test_11_all_original_unity_meta_guids_unique(self):
  self.assertGreaterEqual(self.m.guid_check(self.source),39)
 def test_12_four_separate_private_app_data_scopes(self):
  prod=set()
  for i in range(4):
   scoped,product=self.n.private_source(self.source,i,"ab12cd34ef56",self.m)
   self.assertNotIn(product,prod)
   self.assertEqual(self.m.guid_check(scoped),self.m.guid_check(self.source)+1)
   self.assertIn(product.encode(),scoped[self.n.QA_PROBE])
   prod.add(product)
 def test_13_original_physical_source_stage_and_strict_rollback(self):
  with tempfile.TemporaryDirectory(prefix="b0160w27-original792-") as t:
   for i in range(4):
    scoped,_=self.n.private_source(self.source,i,"ab12cd34ef56",self.m)
    arc=self.m.zip_data(scoped,{
     "original_source_sha":v.PORT792,"native_unity":"NOT_RUN",
     "source_only_preflight":True,"codex_ack":False,"integrated":False,
     "native_meta_guids":self.m.guid_check(scoped),
     "file_sha256":{k:self.n.blake(data) for k,data in sorted(scoped.items())}})
    target=Path(t)/("P"+str(i))
    self.m.stage(arc,scoped,target)
    self.assertTrue((target/v.FIXTURE).is_file())
    self.assertEqual((target/v.FIXTURE).read_bytes(),scoped[v.FIXTURE])
    self.m.rollback(arc,scoped,target)
    self.assertFalse(target.exists())
 def test_14_preserve_foreign_directory_on_rollback_denial(self):
  with tempfile.TemporaryDirectory(prefix="b0160w27-original792-") as t:
   scoped,_=self.n.private_source(self.source,1,"ab12cd34ef56",self.m)
   arc=self.m.zip_data(scoped,{
    "original_source_sha":v.PORT792,"native_unity":"NOT_RUN",
    "source_only_preflight":True,"codex_ack":False,"integrated":False,
    "native_meta_guids":self.m.guid_check(scoped),
    "file_sha256":{k:self.n.blake(data) for k,data in sorted(scoped.items())}})
   path=Path(t)/"P1"
   self.m.stage(arc,scoped,path)
   foreign=path/"EXTERNAL_EMPTY_OWNED_DIR"
   foreign.mkdir()
   with self.assertRaises(AssertionError):self.m.rollback(arc,scoped,path)
   self.assertTrue(foreign.exists())
 def test_15_safe_four_original_source_only_cli_no_native_claim(self):
  with tempfile.TemporaryDirectory(prefix="b0160w27-original792-") as t:
   self.assertEqual(v.main(["--scratch-parent",t]),0)
   paths=list(Path(t).glob("B22-*"))
   self.assertEqual(len(paths),1)
   r=json.loads((paths[0]/v.RECEIPT).read_text())
   self.assertEqual(r["source_preflight"],"FOUR_ORIGINAL_792_PRIVATE_SOURCE_STAGED_AND_ROLLED_BACK")
   self.assertEqual(r["native_G1"],"NOT_RUN")
   self.assertEqual(r["full_game_save_G1"],"NOT_RUN")
   self.assertEqual(len(r["cases"]),4)
 def test_16_native_without_user_consent_is_denied(self):
  with tempfile.TemporaryDirectory(prefix="b0160w27-original792-") as t:
   self.assertEqual(v.main(["--mode","native","--scratch-parent",t]),3)
 def test_17_game_checkout_scratch_is_denied(self):
  with self.assertRaises(AssertionError):
   self.n.safe_root(v.ROOT)
 def test_18_old_772_fixture_is_not_exact_new_792(self):
  self.assertNotEqual(self.n.git("rev-parse",v.PORT772+":"+v.FIXTURE),
                      v.FIXTURE_BLOB)
 def test_19_792_only_two_original_asset_modifications(self):
  c=v.validate(self.n,self.q)
  changed={p for p in c if p.startswith("Assets/")}
  self.assertEqual(changed,{v.RUNTIME,v.FIXTURE})

if __name__=="__main__":unittest.main(verbosity=2)
