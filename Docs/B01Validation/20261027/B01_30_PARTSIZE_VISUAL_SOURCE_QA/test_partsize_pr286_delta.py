"""W16 PR286 actual pinned source delta + adversarial mutation QA, Unity NOT_RUN."""
import csv
import importlib.util
import json
import os
from pathlib import Path
import shutil
import tempfile
import unittest

BASE=Path(__file__).resolve().parent
S=importlib.util.spec_from_file_location("audit286",BASE/"partsize_pr286_delta.py")
audit=importlib.util.module_from_spec(S);S.loader.exec_module(audit)
PRODUCER=Path(os.environ.get("B01_PARTSIZE_PR286_SOURCE",str(BASE/"NO_PINNED_PRODUCER")))
CANONICAL=Path(os.environ.get("B01_PARTSIZE_PINNED_SOURCE",str(BASE/"NO_PINNED_CODEX")))

class PR286SourceComparativeQA(unittest.TestCase):
 @classmethod
 def setUpClass(cls):
  if not (PRODUCER/"PartSizeNativePhysics.cs").exists():
   raise unittest.SkipTest("exact real pinned PR286 producer sources missing")
  if not (CANONICAL/"R41PartGeometry.cs").exists():
   raise unittest.SkipTest("exact real pinned Codex R41 sources missing")
  cls.report=audit.evaluate(PRODUCER,CANONICAL)

 def mutate(self,name,old,new,reason):
  with tempfile.TemporaryDirectory() as tmp:
   p=Path(tmp)/"producer"
   shutil.copytree(PRODUCER,p)
   sources=list(p.rglob(name))
   self.assertEqual(len(sources),1)
   before=sources[0].read_text()
   self.assertIn(old,before)
   sources[0].write_text(before.replace(old,new,1))
   with self.assertRaisesRegex(ValueError,reason):
    audit.evaluate(p,CANONICAL)

 def test_actual_19_catalog_parts(self):
  self.assertEqual(self.report["pr286CatalogKinds"],19)
 def test_actual_wide_kinds_6(self):
  self.assertEqual(len(audit.WIDE),6)
 def test_actual_11_unmeasured_retained(self):
  self.assertEqual(len(self.report["physicalUnmeasuredCanonicalKinds"]),11)
  self.assertIn("Gun",self.report["physicalUnmeasuredCanonicalKinds"])
  self.assertIn("Wheel",self.report["physicalUnmeasuredCanonicalKinds"])
 def test_32_source_measurement_rows(self):
  self.assertEqual(len(self.report["records"]),32)
 def test_original_wheel_remains_unmeasured(self):
  wheel=next(r for r in self.report["records"] if r["legacyVariant"]=="Wheel" and r["yawQuarterTurns"]==0)
  self.assertEqual(wheel["sourcePredictedPhysicalAABBSizeXYZ"],[.52,.91,.78])
  self.assertEqual(wheel["catalogProof"],"CATALOG_UNMEASURED_REQUIRES_NATIVE")
  self.assertFalse(wheel["physicalMountApproval"])
 def test_original_heavy_gun_exceeds_one_cell(self):
  gun=next(r for r in self.report["records"] if r["legacyVariant"]=="GunHeavy" and r["yawQuarterTurns"]==0)
  self.assertEqual(gun["sourcePredictedColliderOverrunXYZ"],[0,.141,.817])
 def test_demo_is_real_serialized_scene_with_script_guid(self):
  self.assertTrue(self.report["demo"]["sceneAndPrefabReferToExactMonoScriptGUID"])
 def test_demo_does_not_model_actual_wheels(self):
  self.assertFalse(self.report["demo"]["hasPhysicalWheelOrTrackCase"])
 def test_actual_broadphase_is_caller_claim_only(self):
  risk=self.report["externalBroadphase"]
  self.assertTrue(risk["callerCanPassEmptyListAndCompleteTrue"])
  self.assertFalse(risk["recomputesExternalCandidatesWithPhysicsOverlap"])
  self.assertTrue(risk["potentialFalseGreenIfCallerOmitsNeighbor"])
 def test_forged_grid_constant_rejected(self):
  self.mutate("PartSizeContract.cs",
    "public const double GridMeters=.55;",
    "public const double GridMeters=.60;","PR286_SOURCE_DRIFT")
 def test_forced_physics_PASS_in_csv_rejected(self):
  self.mutate("observed_r3_size_matrix.csv",
    '"Wheel","1","1","1","","","","","","","NOT_RUN"',
    '"Wheel","1","1","1","","","","","","","PASS"',"unverifiable native")
 def test_forged_shape_size_claim_rejected(self):
  self.mutate("observed_r3_size_matrix.csv",
    '"Wheel","1","1","1","","","","","","","NOT_RUN","",""',
    '"Wheel","1","1","1","","","","","","","NOT_RUN","verified"',"unverifiable mesh")
 def test_wrong_monobehaviour_guid_rejected(self):
  self.mutate("PartSizeDemoEntry.cs.meta",
    "b0123100000000000000000000000007",
    "ffffffffffffffffffffffffffffffff","scene/prefab do not reference")
 def test_missing_explicit_external_inventory_gate_rejected(self):
  self.mutate("PartSizeNativePhysics.cs",
    "fullExternalBroadphase==null||!completeExternalBroadphase",
    "fullExternalBroadphase==null","PR286_SOURCE_DRIFT")
 def test_catalog_name_drift_rejected(self):
  self.mutate("PartSizeBaselineCatalog.cs",'"CrawlerTrack"', '"FakeTrack"',"canonical part catalog list drift")
 def test_native_is_not_marked_pass(self):
  self.assertTrue(all(r["unityActualColliderBounds"]=="NOT_RUN" for r in self.report["records"]))
  self.assertEqual(self.report["demo"]["renderResult720And1080"],"NOT_RUN")
 def test_actual_7_source_anchors_evaluated(self):
  self.assertEqual(set(self.report["sourcesVerified"]),set(["R41PartGeometry.cs","R3VehicleAssembly.cs","R3BonusModuleGeometry.cs","R3VehicleModel.cs"]))

if __name__=="__main__":unittest.main(verbosity=2)
