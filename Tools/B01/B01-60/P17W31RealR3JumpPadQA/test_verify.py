import unittest
import verify as v
class OriginalGameJumpPadW31(unittest.TestCase):
 @classmethod
 def setUpClass(cls):
  cls.r=v.verify();cls.c=(v.ROOT/v.SOURCE).read_text()
 def test_01_real_exact_W30_parent(self):self.assertEqual(v.git("merge-base",v.BASE,"HEAD"),v.BASE)
 def test_02_only_7_new_QA_paths(self):
  self.assertEqual({tuple(x.split("\t",1)) for x in v.git("diff","--name-status",v.BASE,"HEAD").splitlines()},
                   {("A",p) for p in v.FILES})
 def test_03_all_8_original_R1_R3_R5_PhysX_Codex_blobs(self):
  for p,s in v.PINS.items():self.assertEqual(v.git("rev-parse","HEAD:"+p),s)
 def test_04_real_r1_combat_not_a_detached_cube(self):
  for token in ("run.StartRun()","run.ChooseRoute(0)","run.Player.GetComponent<R3VehicleAssembly>()",
                "run.Player.GetComponent<Rigidbody>()","run.Arena.Build(R5ArenaKind.SalvageYard)"):
   self.assertIn(token,self.c)
 def test_05_real_r3_multi_collider_not_fake_10kg(self):
  for token in ("body.mass, Is.GreaterThan(20f)","GetComponentsInChildren<Collider>",
                "c.attachedRigidbody == body","body.isKinematic"): self.assertIn(token,self.c)
 def test_06_real_physics_velocity_and_height_both_checked(self):
  for token in ("p.body.linearVelocity.y","p.body.position.y","maxUpVelocity, Is.GreaterThan(5f)",
                "maxHeight, Is.GreaterThan(2.3f)"):self.assertIn(token,self.c)
 def test_07_compound_wheels_must_not_multiply_impulse(self):
  self.assertIn("maxUpVelocity, Is.LessThan(15f)",self.c)
 def test_08_no_launch_when_actual_map_pad_disabled(self):
  for token in ("p.pad.SetActive(false);","p.pad.CanLaunch(p.body)","maxUpVelocity, Is.LessThan(4f)"):
   self.assertIn(token,self.c)
 def test_09_actual_original_tank_freeze_must_be_overridden_by_real_R6(self):
  a=v.game("Assets/Pack/P12_PipelineSafeTankController.cs")
  b=v.game("Assets/R6/R6VehicleDrive.cs")
  self.assertIn("RigidbodyConstraints.FreezePositionY",a)
  self.assertIn("body.constraints=RigidbodyConstraints.None",b)
  self.assertIn("p.body.constraints & RigidbodyConstraints.FreezePositionY",self.c)
 def test_10_actual_pad_trigger_contact_proof_for_active_and_disabled(self):
  self.assertIn("BodyActuallyOverlapsPadTrigger",self.c)
  self.assertIn("c.bounds.Intersects(trigger.bounds)",self.c)
  self.assertEqual(self.c.count("actualPadContactObserved |= BodyActuallyOverlapsPadTrigger"),2)
  self.assertEqual(self.c.count("Assert.That(actualPadContactObserved, Is.True"),2)
 def test_11_scope_receipt_cannot_be_mistaken_for_native(self):
  self.assertEqual(self.r["real_Unity_6000_3_14f1"],"NOT_RUN")
  self.assertEqual(self.r["real_PhysX"],"NOT_RUN")
  self.assertFalse(self.r["qa_accepted"])
if __name__=="__main__":unittest.main()
