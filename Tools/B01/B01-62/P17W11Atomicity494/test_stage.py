#!/usr/bin/env python3
"""W11 independent negative contract and actual immutable source file tests."""
import hashlib
import json
import pathlib
import subprocess
import tempfile
import unittest
from stage_source import TARGET, ORIGINALS,fetch,inspect,make_stub,stage,source_blob

V2_SHA="33a32b8bd9ab40467ab1a17b5684926a37dbd4d3"
V2_PATH="Assets/B0123SecureReattachV2/Runtime/SecureDebrisPlaygroundV2.cs"
V2_BLOB="491c89a18827bb027854bba5e6be6630ce4947b6"
class Original494Independent(unittest.TestCase):
 @classmethod
 def setUpClass(cls):
  cls.s=fetch(".")
 def change(self,key,old,new):
  src=dict(self.s)
  self.assertEqual(src[key].count(old),1,"mutation must uniquely target original source")
  src[key]=src[key].replace(old,new)
  return src
 def test_01_actual_five_git_blobs_locked(self):
  self.assertEqual(len(ORIGINALS),5)
  for filename,(path,blob) in ORIGINALS.items():
   data=subprocess.check_output(["git","show",TARGET+":"+path])
   self.assertEqual(source_blob(data),blob)
 def test_02_original_public_api_has_actual_physics(self):
  self.assertEqual(inspect(self.s)["public_reach_physics_guard"],"SOURCE_OBSERVED")
 def test_03_missing_id_may_fail_after_six_real_state_writes(self):
  self.assertEqual(inspect(self.s)["physical_mutations_before_failed_rearm"],6)
 def test_04_original_W06_emitter_has_public_reset(self):
  self.assertIn("public void ClearForNewRun() => ledger.ClearForNewRun();",self.s["DebrisRepulsionEmitter.cs"])
 def test_05_original_W06_ledger_must_actually_remove_seen_id(self):
  self.assertIn("id > 0 && seen.Remove(id)",self.s["DebrisImpulseCore.cs"])
 def test_06_missing_public_collision_check_is_detected(self):
  with self.assertRaisesRegex(ValueError,"DRIFT"):
   inspect(self.change("DebrisRigPlayground.cs","if (!ValidatePhysicalReattach(rig)) return false;","if (false) return false;"))
 def test_07_swapped_id_rearm_and_detached_mutation_is_new_revision(self):
  with self.assertRaisesRegex(ValueError,"DRIFT"):
   inspect(self.change("DebrisRigPlayground.cs",
    "rig.Detached = false;\n            if (!rig.Emitter.RearmAfterReattach(rig.Definition.PartId))",
    "if (!rig.Emitter.RearmAfterReattach(rig.Definition.PartId))\n            rig.Detached = false;"))
 def test_08_deleted_parent_mutation_detected(self):
  with self.assertRaisesRegex(ValueError,"DRIFT"):
   inspect(self.change("DebrisRigPlayground.cs",
    "rig.Part.transform.SetParent(rig.Root.transform, false);\n            rig.Part.transform.localPosition = rig.LocalMountPosition;\n            rig.Detached = false;",
    "rig.Part.transform.SetParent(null, true);\n            rig.Part.transform.localPosition = rig.LocalMountPosition;\n            rig.Detached = false;"))
 def test_09_missing_real_penetration_detected(self):
  s=self.change("DebrisRigPlayground.cs","Physics.ComputePenetration(","FakeComputePenetration(")
  with self.assertRaisesRegex(ValueError,"MISSING_PHYSICS_GUARD"):inspect(s)
 def test_10_missing_original_reset_detected(self):
  with self.assertRaisesRegex(ValueError,"DRIFT"):
   inspect(self.change("DebrisRepulsionEmitter.cs","public void ClearForNewRun() => ledger.ClearForNewRun();",
     "public void ClearForNewRun() => throw new System.Exception();"))
 def test_11_test_only_stub_does_not_replace_real_emitter(self):
  stub=make_stub(self.s)
  self.assertNotIn("namespace B01.DebrisRepulse001",stub)
  self.assertIn("public void AddForce(Vector3 impulse, ForceMode mode)",stub)
 def test_12_receipt_is_deterministic_and_explicit_hold(self):
  a=json.dumps(inspect(self.s),sort_keys=True)
  self.assertEqual(a,json.dumps(inspect(self.s),sort_keys=True))
  self.assertIn("RELEASE_HOLD",a)
  self.assertIn("NOT_RUN",a)
 def test_13_source_stage_is_write_once_and_scoped(self):
  with tempfile.TemporaryDirectory() as tmp:
   dest=pathlib.Path(tmp)/"stage"
   stage(".",dest)
   self.assertEqual(len(list(dest.iterdir())),6)
   with self.assertRaisesRegex(ValueError,"NEW_DISPOSABLE_STAGE_REQUIRED"):
    stage(".",dest)
 def test_14_staged_original_bytes_identical_to_git(self):
  with tempfile.TemporaryDirectory() as tmp:
   dest=pathlib.Path(tmp)/"stage"
   stage(".",dest)
   for name in ("DebrisRigPlayground.cs","RigDemoDefinition.cs","DebrisImpulseCore.cs","DebrisRepulsionEmitter.cs"):
    path=ORIGINALS[name][0]
    self.assertEqual((dest/name).read_bytes(),subprocess.check_output(["git","show",TARGET+":"+path]))
 def test_15_other_V2_candidate_also_has_late_rearm_in_frozen_version(self):
  data=subprocess.check_output(["git","show",V2_SHA+":"+V2_PATH])
  self.assertEqual(source_blob(data),V2_BLOB)
  src=data.decode()
  method=src[src.index("private bool CommitDock(int index)"):src.index("public void ResetForNewRun()",src.index("private bool CommitDock(int index)"))]
  self.assertLess(method.index("rig.Detached=false;"),method.index("rig.Emitter.RearmAfterReattach("))
 def test_16_two_versions_cannot_be_silently_equated(self):
  self.assertNotEqual(TARGET,V2_SHA)
  self.assertNotEqual(ORIGINALS["DebrisRigPlayground.cs"][1],V2_BLOB)
 def test_17_receipt_never_claims_native_unity(self):
  receipt=inspect(self.s)
  self.assertEqual(receipt["native_unity_physx"],"NOT_RUN")
  self.assertEqual(receipt["codex_ack"],"NONE")
if __name__=="__main__":unittest.main(verbosity=2)
