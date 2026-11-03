"""B01-60 W29 direct original #828 source regression, NOT actual Unity G1."""
import unittest
import tempfile
from pathlib import Path
import json
import verify as v

class SawOverflowW29OriginalSourceTests(unittest.TestCase):
 @classmethod
 def setUpClass(cls):
  cls.result=v.source()
  cls.old=v.original(v.GAME)
  cls.current=(v.ROOT/v.GAME).read_text()
  cls.testold=v.original(v.NUNIT)
  cls.testcurrent=(v.ROOT/v.NUNIT).read_text()

 def test_01_only_two_original_module_files_modified_and_six_owned_new(self):
  lines=v.owner_diff()
  self.assertEqual(len(lines),8)
  self.assertEqual({p for op,p in lines if op=="M"},{v.GAME,v.NUNIT})
  self.assertTrue(all(op=="A" for op,p in lines if p not in (v.GAME,v.NUNIT)))

 def test_02_exact_original_candidate_sha_and_git_blob_closure(self):
  self.assertEqual(v.git("merge-base",v.W27,v.BASE),v.W27)
  self.assertEqual(v.git("rev-parse",v.BASE+":"+v.GAME),v.GAME_BASE_BLOB)
  self.assertEqual(v.git("rev-parse",v.BASE+":"+v.NUNIT),v.NUNIT_BASE_BLOB)

 def test_03_original_saw_damage_motor_and_contact_semantics_byte_identical(self):
  self.assertEqual(self.current,self.old.replace(v.OLD,v.NEW,1))
  self.assertIn("motor.Tick(requestUnblocked, energyAvailable, ready, dt)",self.current)
  self.assertIn("sink.ApplyDamage(new SawDamageEvent(",self.current)

 def test_04_saturated_owner_scan_reports_before_fail_closed_return(self):
  own=self.current.split("private bool IntersectsOtherOwnedPart()",1)[1].split("private bool automaticSimulation",1)[0]
  self.assertIn("if (count >= overlapBuffer.Length)",own)
  self.assertIn("OverflowedLastTick = true;",own)
  self.assertLess(own.index("OverflowedLastTick = true;"),own.index("return true;",own.index("if (count >=")))

 def test_05_first_and_second_scan_both_set_same_overflow_diagnostic(self):
  self.assertEqual(self.current.count("OverflowedLastTick = true;"),2)
  self.assertIn("OverflowedLastTick = false;",self.current)
  self.assertIn("OwnGeometryBlockedLastTick = ready && motorRequested && IntersectsOtherOwnedPart()",self.current)

 def test_06_only_one_genuine_original_playmode_added(self):
  receipt=v.patched_tests()
  self.assertEqual(receipt["new_playmode_cases"],receipt["baseline_playmode_cases"]+1)
  self.assertEqual(receipt["added_scenario"],v.METHOD)

 def test_07_full_original_828_tests_preserved_exact(self):
  delta=self.testcurrent.split(v.TEST_MARKER,1)[1].split(v.TEST_ANCHOR,1)[0]
  restored=self.testcurrent.replace(v.TEST_MARKER+delta,"",1)
  self.assertEqual(restored,self.testold)
  self.assertIn("OverlapOverflowDoesNotPartiallyDamage",self.testcurrent)

 def test_08_real_contacts_in_saturation_and_recovery_are_observed(self):
  for s in ("saw.OverflowedLastTick","saw.OwnGeometryBlockedLastTick",
            "config.maxOverlapHits = 4","WaitForFixedUpdate","enemy.TotalDamage",
            "candidate.gameObject.SetActive(false)","Physics.SyncTransforms"):
   self.assertIn(s,self.testcurrent)

 def test_09_forged_receipt_directory_cannot_promote_native(self):
  with tempfile.TemporaryDirectory(prefix="b0160-w29-fake-") as p:
   with self.assertRaisesRegex(ValueError,"missing"):
    v.native_receipt(p,"a"*40)

 def test_10_wrong_commit_cannot_promote_native(self):
  with tempfile.TemporaryDirectory(prefix="b0160-w29-fake-sha-") as p:
   with self.assertRaisesRegex(ValueError,"invalid expected exact source HEAD"):
    v.native_receipt(p,"invalid")

 def test_11_source_no_Unity_run_no_anti_softlock_or_player_claim(self):
  self.assertEqual(self.result["unity_g1"],"NOT_RUN")
  self.assertEqual(self.result["windows_player"],"NOT_RUN")
  self.assertEqual(self.result["native_default_player_enemy_geometry"],"KNOWN_FAIL_UNRESOLVED")
  self.assertFalse(self.result["game_integrated"])

if __name__=="__main__":
 unittest.main(verbosity=2)
