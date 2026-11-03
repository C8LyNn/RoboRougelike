"""Actual original Git #873/#877 source coverage and hostile evidence negatives.

This is not an emulator, Unity Editor test, Windows Player boot or human test.
"""
import json
from pathlib import Path
import tempfile
import unittest
import verify as v

class W30RealCurrentGameAndPhysicalUnityAcceptanceSource(unittest.TestCase):
 @classmethod
 def setUpClass(cls):
  cls.scope=v.check_source()
  cls.cs=(v.ROOT/v.OWN_CS).read_text(encoding="utf-8")
  cls.build=v.original("Assets/R3/R3BuildValidation.cs")
  cls.view=v.original("Assets/R3/R3GarageView.cs")
  cls.model=v.original("Assets/R3/R3VehicleModel.cs")
  cls.arena=v.original("Assets/R5/R5Arena.cs")
 
 def test_01_final_exact_game877_873_w27_Git_ancestry(self):
  self.assertEqual(v.git("merge-base",v.BASE,"HEAD"),v.BASE)
  self.assertEqual(v.git("merge-base",v.GAME,v.BASE),v.GAME)
  self.assertEqual(v.git("merge-base",v.W27,v.GAME),v.W27)

 def test_02_own_eight_add_only_never_Codex_or_Save(self):
  files=[p.split("\t") for p in v.git("diff","--name-status",v.BASE,"HEAD").splitlines()]
  self.assertEqual(set(tuple(f) for f in files),{("A",p) for p in v.OWN})
  self.assertFalse(any("Save" in p or "R3VehicleModel.cs" in p for _,p in files))

 def test_03_all_nine_relevant_current_game_sources_byte_pinned(self):
  for path,blob in v.PINS.items():
   self.assertEqual(v.git("rev-parse","HEAD:"+path),blob,path)

 def test_04_real_two_Unity_EditorPlayMode_scenarios_not_simulations(self):
  self.assertEqual(self.cs.count("[UnityTest]"),2)
  for n in v.NATIVE_CASES:self.assertIn("public IEnumerator "+n+"()",self.cs)
  self.assertIn('EditorSceneManager.OpenScene(GameplayScene, OpenSceneMode.Single)',self.cs)
  self.assertIn("yield return new EnterPlayMode()",self.cs)
  self.assertNotIn("FakeUnity",self.cs)

 def test_05_physically_move_own_Collider_into_rendered_saw_center_after_valid_preview(self):
  for s in ('view.Preview.GetComponentsInChildren<Transform>',"own.transform.position = center.position + Vector3.up * 3f",
            "own.transform.position = center.position;", "Physics.SyncTransforms()",
            'Does.Contain("Fioletowa strefa")'):
   self.assertIn(s,self.cs)

 def test_06_dynamic_hard_gate_prevents_unspent_transaction_and_phantom_IDs(self):
  for s in ("view.PreviewValid","view.ClickTarget()","model.Scrap",
            "model.Inventory.Select(p => p.Id)","model.Parts.Select(p => p.Id)",
            "model.Parts.Concat(model.Inventory).Select(p => p.Id).Distinct().Count()"):
   self.assertIn(s,self.cs)

 def test_07_real_obstruction_clears_and_same_owned_Id_can_mount_without_cost(self):
  for s in ("own.SetActive(false);","Object.Destroy(own);",
            "Assert.That(view.ClickTarget(), Is.True);","model.Parts.Single(p => p.Id == id).Cell"):
   self.assertIn(s,self.cs)

 def test_08_rotated_ghost_uses_same_canonical_model_gate(self):
  self.assertIn("view.RotateSelected(1, 1)",self.cs)
  self.assertIn("model.OwnedPlacementError(id, ExteriorSaw, view.SelectedRotation)",self.cs)
  self.assertIn("view.PlacementIssue, Is.EqualTo(gateIssue)",self.cs)

 def test_09_Codex_877_existing_authoritative_physical_DISC_and_BUILD001_untouched(self):
  for s in ("BuildPlacementRules.CheckForInstallation(", "Physics.OverlapBox(",
            "return FunctionalWeaponPlacementIssue(candidate,probe);",
            "RotarySawContactMath.IsOnCuttingDisc("):
   self.assertIn(s,self.build)

 def test_10_model_pays_only_after_source_gate_and_ghost_same_authority(self):
  self.assertIn("canonical.SetInstallationGate(candidate=>gate!=null?gate.Validate(candidate)",self.build)
  self.assertIn("Owned!=null?run.VehicleModel.OwnedPlacementError",self.view)
  self.assertIn("string error=PlacementIssue;if(error!=null){Message=error;return false;}",self.view)
  self.assertIn("Scrap-=Price(kind,weapon)",self.model)

 def test_11_real_arena_current_game_has_physical_jumppad(self):
  self.assertIn("if(kind==R5ArenaKind.SalvageYard)JumpPad(",self.arena)
  self.assertIn("pad.AddComponent<B01.MapJumpPad001.MapJumpPad>()",self.arena)

 def test_12_true_Unity_G1_five_suites_required_not_source_PASSED(self):
  self.assertEqual(sum(count for _,_,count in v.SUITES.values()),51)
  self.assertEqual(self.scope["unity_g1"],"NOT_RUN")
  self.assertEqual(self.scope["player_windows_x64"],"NOT_RUN")

 def test_13_missing_real_Editor_XML_cannot_be_G1(self):
  with tempfile.TemporaryDirectory(prefix="b0160-w30-missing-") as d:
   with self.assertRaisesRegex(ValueError,"no original receipt file"):
    v.native_audit(d,"a"*40)

 def test_14_bad_source_sha_receipt_cannot_be_G1(self):
  with tempfile.TemporaryDirectory(prefix="b0160-w30-badsha-") as d:
   with self.assertRaisesRegex(ValueError,"invalid source SHA"):
    v.native_audit(d,"INVALID")

 def test_15_fake_native_execution_declaration_cannot_be_G1(self):
  with tempfile.TemporaryDirectory(prefix="b0160-w30-fake-") as d:
   path=Path(d)
   (path/"W30-native-receipt.json").write_text(json.dumps({
    "source_head":"a"*40,"game877_parent":v.BASE,"game873_parent":v.GAME,
    "unity_version":"6000.3.14f1","native_invoked":False}))
   with self.assertRaisesRegex(ValueError,"no actual Unity invoked"):
    v.native_audit(d,"a"*40)

if __name__=="__main__":unittest.main(verbosity=2)
