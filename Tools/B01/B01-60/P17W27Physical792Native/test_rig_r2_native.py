"""Real original Git #792 four-case source & physical staging checks, NOT Unity G1."""
from pathlib import Path
import json
import re
import tempfile
import unittest
import rig_r2_native as r

class Original792PhysicalNativeSourceTests(unittest.TestCase):
 @classmethod
 def setUpClass(cls):
  cls.m=r.old()
  cls.src=r.selected(cls.m)
  cls.n=cls.m.original_module()
  cls.helper=cls.n.runner()

 def test_01_author_792_has_exact_772_ancestry_and_ten_changes(self):
  self.assertEqual(self.m.git("merge-base",r.PARENT,r.R2),r.PARENT)
  self.assertEqual(set(self.m.git("diff","--name-only",r.PARENT,r.R2).splitlines()),
                   r.ALLOWED_R2_DIFF)

 def test_02_two_exact_corrected_original_unity_blobs(self):
  for path,sha in r.R2_BLOBS.items():
   self.assertEqual(self.m.git("rev-parse",r.R2+":"+path),sha)
   self.assertNotEqual(self.m.git("rev-parse",r.PARENT+":"+path),sha)

 def test_03_all_other_unity_772_bits_stay_original_sha(self):
  originals=self.m.selected_source()
  self.assertEqual(set(originals),set(self.src))
  for path,bytes_ in originals.items():
   if path not in r.R2_BLOBS:self.assertEqual(self.src[path],bytes_,path)

 def test_04_all_four_real_nunit_playmode_test_names_exact_order(self):
  names=tuple(x.decode() for x in re.findall(
   rb"\[UnityTest\]\s*public\s+IEnumerator\s+(\w+)\s*\(",
   self.src[self.m.NATIVE_FIXTURE]))
  self.assertEqual(names,r.METHODS)

 def test_05_real_rigidbody_velocity_rollback_asserted(self):
  fixture=self.src[self.m.NATIVE_FIXTURE].decode()
  section=fixture.split("public IEnumerator AbortedCompetingWorld_",1)[1].split("[UnityTest]",1)[0]
  for needle in ("2.5f","0.5f","linearVelocity","angularVelocity","world.Deactivate()",
                 "world.Activate()","port.VisibleConsumedId(0,2310001)"):
   self.assertIn(needle,section)
  runtime=self.src[self.m.PHYSICAL].decode()
  self.assertIn("Old Rigidbody lost before rollback snapshot",runtime)

 def test_06_nine_original_unity_guid_closure_unchanged(self):
  self.assertEqual(self.helper.guid_check(self.src),
                   self.helper.guid_check(self.m.selected_source()))

 def test_07_four_independent_product_save_scopes(self):
  produced=[]
  for i in range(4):
   case,product=r.private_case(self.m,self.src,i,"012345abcdef",self.helper)
   produced.append(product)
   self.assertIn(product.encode(),case[r.PROBE])
   self.assertEqual(self.helper.guid_check(case),
                    self.helper.guid_check(self.src)+1)
  self.assertEqual(len(set(produced)),4)

 def test_08_original_four_native_filter_identifiers(self):
  for method in r.METHODS:
   self.assertTrue((r.SUITE+method).startswith(
    "B01.CanonicalRigPortW24.Tests.CanonicalPhysicalRigPortW24NativeTests."))

 def test_09_complete_four_private_projects_source_stage_and_rollback(self):
  with tempfile.TemporaryDirectory(prefix="b0160-w27-source-") as temp:
   self.assertEqual(r.main(["--mode","preflight","--scratch-parent",temp]),0)
   dirs=list(Path(temp).glob("B22-*"))
   self.assertEqual(len(dirs),1)
   receipt=json.loads((dirs[0]/"P27-receipt.json").read_text())
   self.assertEqual(receipt["original_physical_R2_sha"],r.R2)
   self.assertEqual(receipt["source_preflight"],
                    "PASS_4_PRIVATE_ORIGINAL_792_STAGES_ROLLED_BACK")
   self.assertEqual(receipt["native_G1"],"NOT_RUN")
   for i in range(4):
    self.assertFalse((dirs[0]/("P"+str(i))).exists())

 def test_10_native_manual_g0_and_consent_refusal(self):
  with tempfile.TemporaryDirectory(prefix="b0160-w27-native-deny-") as temp:
   with self.assertRaises(AssertionError):
    r.main(["--mode","native","--scratch-parent",temp,
            "--operator-consent","B0160 W27 PHYSICS NATIVE"])
   with self.assertRaises(AssertionError):
    r.main(["--mode","native","--scratch-parent",temp,
            "--source-g0-approved"])

 def test_11_exact_original_783_runner_unchanged(self):
  self.assertEqual(self.m.git("rev-parse",r.BASE+":"+r.OLD.relative_to(r.ROOT).as_posix()),
                   self.m.git("rev-parse","HEAD:"+r.OLD.relative_to(r.ROOT).as_posix()))

 def test_12_all_other_selected_packages_not_installed_as_second_writer(self):
  self.assertEqual(r.METHODS[1],
    "AbortedCompetingWorld_PreservesActualLiveVelocityAndAngularVelocity")
  self.assertFalse(any("WholeGameEconomy" in name or "StrictCanonicalPathR3" in name
                       for name in self.src))

if __name__=="__main__":
 unittest.main(verbosity=2)
