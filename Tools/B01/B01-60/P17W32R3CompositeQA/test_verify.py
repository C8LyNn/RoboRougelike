import unittest
import verify as v

class CompositeActualProductW33(unittest.TestCase):
 @classmethod
 def setUpClass(cls):
  cls.receipt=v.verify()
  cls.product=v.get("Assets/R3/R3VehicleModel.cs")
  cls.test=v.get(v.TEST_PATH)
 def test_01_exact_latest_game_ancestry(self):
  self.assertEqual(v.git("merge-base",v.BASE,"HEAD"),v.BASE)
  self.assertEqual(v.git("merge-base",v.MANIFEST["w32_source_green_original_immutable"],"HEAD"),v.MANIFEST["w32_source_green_original_immutable"])
 def test_02_all_new_files_are_add_only(self):
  self.assertEqual({tuple(s.split("\t",1)) for s in v.git("diff","--name-status",v.BASE,"HEAD").splitlines()},
      {("A",p) for p in v.MANIFEST["new_paths_exactly"]})
 def test_03_physical_source_blobs_pinned(self):
  for path,sha in v.MANIFEST["product_pins"].items():
   self.assertEqual(v.git("rev-parse","HEAD:"+path),sha)
 def test_04_imported_4_latest_original_unity_tests_byte_pinned(self):
  for x in v.MANIFEST["legacy_qa_sources"]:
   self.assertEqual(v.git("rev-parse","HEAD:"+x["path"]),x["source_git_blob"])
   self.assertEqual(v.git("rev-parse","HEAD:"+x["path"]+".meta"),x["meta_git_blob"])
 def test_05_zero_mounted_has_real_model_action(self):
  self.assertIn("model.Repair(Vector3Int.right)",self.test)
 def test_06_zero_bulk_has_real_model_action(self):
  self.assertIn("model.RepairAffordable()",self.test)
 def test_07_zero_emergency_has_real_model_action(self):
  self.assertIn("model.EmergencyRepair()",self.test)
 def test_08_partial_hp_nonzero_policy_stays(self):
  self.assertIn("ExistingNonzeroPartialHpRepairRateAndOneDebitRemainUnchanged",self.test)
 def test_09_bad_quote_real_original_source_witness(self):
  self.assertTrue(v.defect_witness(self.product)["destroyed_quote_old_35ceil_RED"])
 def test_10_mounted_inventory_emergency_real_source_negative_witness(self):
  self.assertTrue(v.defect_witness(self.product)["mounted_inventory_emergency_paid0_denied_RED"])
 def test_11_bulk_inherits_mounted_zero_cost_refusal(self):
  self.assertTrue(v.defect_witness(self.product)["bulk_inherits_paid0_denial_RED"])
 def test_12_quote_fix_mutant_cannot_be_falsely_marked_red(self):
  sample=self.product.replace("purchase*0.35f","purchase*0.65f")
  self.assertFalse(v.defect_witness(sample)["destroyed_quote_old_35ceil_RED"])
 def test_13_zero_paid_acceptance_mutant_cannot_be_falsely_marked_red(self):
  sample=self.product.replace("price <= 0","price < 0").replace("cost<=0","cost<0").replace("RepairCost(p)<=0","RepairCost(p)<0")
  self.assertFalse(v.defect_witness(sample)["mounted_inventory_emergency_paid0_denied_RED"])
 def test_14_source_success_never_means_unity(self):
  self.assertEqual(self.receipt["real_Unity_Editor_G1"],"NOT_RUN")
  self.assertEqual(self.receipt["real_Windows_x64_Player"],"NOT_RUN")
  self.assertFalse(self.receipt["game_fully_accepted"])
 def test_15_all_zero_hp_actions_do_not_claim_immediate_repair(self):
  self.assertIn("ZeroQuotedMountedWheelBooksOnceButStaysAtZeroHpUntilNextTrueVictory",self.test)
  self.assertIn("ZeroQuotedBulkRebuildBooksOnceWithoutImmediateRestoration",self.test)
  self.assertIn("ZeroQuotedEmergencyBooksPendingStillCannotDeployAndPreservesIds",self.test)
  self.assertIn("Assert.That(part.Health, Is.Zero",self.test)
  self.assertIn("Assert.That(model.At(Vector3Int.right).Health, Is.Zero",self.test)
 def test_16_current_producer_897_and_899_not_old_instant_cases(self):
  f=[v.get(x["path"]) for x in v.MANIFEST["legacy_qa_sources"]]
  self.assertIn("PurchasePriceOneAcceptsZeroPaidOrderButMustNotImmediatelyRestoreHP",f[2])
  self.assertIn("DestroyedOwnedMountedPartPaysV7QuoteButStaysAtZeroUntilAuthenticatedNextWin",f[3])
  self.assertIn("LiveDefaultBasicGun_DestroyedEmergencyOrderMustWaitForRealNextVictory",f[0])
 def test_17_full_actual_light_standard_heavy_catalog(self):
  for kind,n in [("Light",20),("Standard",83),("Heavy",139)]:
   self.assertIn("[TestCase(R3VehicleKind."+kind+", "+str(n)+")]",self.test)
  self.assertEqual(self.receipt["real_R3_Light_Standard_Heavy_part_counts"],[20,83,139])
  self.assertEqual(self.receipt["real_game_Save_G1"],"BLOCKED_BY_PRODUCER_913")
 def test_18_original_instant_restore_product_bug_and_mutant(self):
  key="destroyed_zeroHP_instant_without_authenticated_next_win_RED"
  self.assertTrue(v.defect_witness(self.product)[key])
  fixed=self.product.replace("part.Health = 1; return true;","part.Health = 0; return true;")
  self.assertFalse(v.defect_witness(fixed)[key])
 def test_19_one_authoritative_QA_bundle_scope(self):
  self.assertEqual(self.receipt["new_QA_only_paths"],15)
  self.assertEqual(self.receipt["imported_original_Unity_test_blob_pairs"],4)
  self.assertEqual(self.receipt["authored_genuine_original_Unity_NUnit_cases"],21)
if __name__=="__main__":unittest.main()
