"""Executed static negative controls on ACTUAL frozen CHL #285 C# source bytes.

Fixtures intentionally INJECT bad code only IN MEMORY; PR285 remains untouched.
Unity gameplay/native wallet isolation is NOT executed here.
"""
import json
import pathlib
import sys
import unittest

HERE=pathlib.Path(__file__).resolve().parent
sys.path.insert(0,str(HERE))
import audit

ROOT=HERE/"_pinned/Assets/RoboRougelikeChallenges/CHL003"
RECEIPT=HERE/"_pinned/receipt.json"

class OriginalCHL285NegativeTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        receipt=json.loads(RECEIPT.read_text(encoding="utf8"))
        if receipt.get("chl_commit")!="43f8fb6cae682c89d12618d883b387b0bf5bc30d":
            raise RuntimeError("PINNED_CHL285_SHA_CHANGED")
        cls.code=[(str(p),p.read_text(encoding="utf8")) for p in sorted(ROOT.rglob("*.cs"))]
        cls.core=(ROOT/"Runtime/CHL003CourseCore.cs").read_text(encoding="utf8")
        cls.controller=(ROOT/"Runtime/CHL003CourseController.cs").read_text(encoding="utf8")
        cls.demo=(ROOT/"Demo/CHL003DemoBootstrap.cs").read_text(encoding="utf8")

    def test_16_actual_original_seven_Csharp_files_are_reward_free(self):
        self.assertEqual(audit.inspect_chl_files(self.code),14)

    def test_17_runtime_asmdef_has_no_host_references(self):
        obj=json.loads((ROOT/"Runtime/B01.CHL003.Runtime.asmdef").read_text())
        self.assertEqual(obj["name"],"B01.CHL003.Runtime")
        self.assertEqual(obj["references"],[])

    def test_18_mutated_actual_controller_direct_scrap_award_denied(self):
        patched=self.controller.replace("public void Retry()", "public void Retry()") if False else (
            self.controller.replace("public void Cancel()",
              "public void CheatingAward(){ VehicleModel.Award(777); }\npublic void Cancel()",1))
        self.assertNotEqual(patched,self.controller)
        with self.assertRaisesRegex(audit.QAError,"DIRECT_REWARD_WRITE"):
            audit.inspect_chl_files([("CHL003/Runtime/CHL003CourseController.cs",patched)])

    def test_19_mutated_actual_core_physical_hp_damage_denied(self):
        patched=self.core.replace("public void Retry()",
            "public void MutateHealth(){ ownedPart.Health = 0f; }\npublic void Retry()",1)
        self.assertNotEqual(patched,self.core)
        with self.assertRaisesRegex(audit.QAError,"PART_DAMAGE_WRITE"):
            audit.inspect_chl_files([("CHL003/Runtime/CHL003CourseCore.cs",patched)])

    def test_20_mutated_real_demo_inventory_insert_denied(self):
        patched=self.demo.replace("private void Start()",
            "private void Inject(){ Inventory.Add(reward); }\nprivate void Start()",1)
        self.assertNotEqual(patched,self.demo)
        with self.assertRaisesRegex(audit.QAError,"INVENTORY_MUTATION"):
            audit.inspect_chl_files([("CHL003/Demo/CHL003DemoBootstrap.cs",patched)])

    def test_21_mutated_real_demo_saved_wallet_denied(self):
        patched=self.demo.replace("private void Start()",
            "private void Inject(){ PlayerPrefs.SetInt(\"Scrap\",999); }\nprivate void Start()",1)
        self.assertNotEqual(patched,self.demo)
        with self.assertRaisesRegex(audit.QAError,"GARAGE_OR_SAVE_WRITE"):
            audit.inspect_chl_files([("CHL003/Demo/CHL003DemoBootstrap.cs",patched)])

    def test_22_mutated_original_core_host_namespace_denied(self):
        patched="using RoboRougelike.R3;\n"+self.core
        with self.assertRaisesRegex(audit.QAError,"CHL_HOST_IMPORT_IN_CORE"):
            audit.inspect_chl_files([("CHL003/Runtime/CHL003CourseCore.cs",patched)])

    def test_23_actual_retry_affects_only_physics_actor_pose_not_saved_hp(self):
        self.assertIn("activeActor.position = spawnPosition;",self.controller)
        self.assertIn("activeActor.rotation = spawnRotation;",self.controller)
        self.assertIn("activeActor.linearVelocity = Vector3.zero;",self.controller)
        self.assertIn("activeActor.angularVelocity = Vector3.zero;",self.controller)
        self.assertNotRegex(self.controller,r"\b(?:Health|Inventory|Scrap)\s*(?:[+\-*/]?=|\.)")
        self.assertIn("if (vehicle != null) Destroy(vehicle.gameObject);",self.demo)

    def test_24_completed_event_lacks_durable_attempt_id_for_receiver(self):
        self.assertIn("public event Action<float> Completed;",self.core)
        self.assertIn("Completed?.Invoke(ElapsedSeconds);",self.core)
        self.assertIn("public void Retry()",self.core)
        self.assertNotIn("VictoryEventId",self.core)
        self.assertNotIn("TransactionId",self.core)

    def test_26_new_trial_metrics_only_output_csv_not_r3_credit(self):
        t=(ROOT/"Runtime/CHL003TrialMetrics.cs").read_text(encoding="utf8")
        self.assertIn("public string ToCsv()",t)
        self.assertIn("public void Reset(",t)
        self.assertNotIn("System.IO",t)
        self.assertNotIn("VehicleModel",t)

    def test_27_new_finish_arbiter_requires_later_physics_step(self):
        t=(ROOT/"Runtime/CHL003FinishArbiter.cs").read_text(encoding="utf8")
        self.assertIn("fixedStep <= queuedStep.Value",t)
        self.assertIn("stillPhysicallyOverlapping",t)
        self.assertIn("public void CancelPending()",t)

    def test_28_demo_diagnostic_logging_not_game_persistence(self):
        self.assertIn('Debug.Log("CHL003_ACTUAL_TRIAL_SUMMARY_CSV',self.demo)
        self.assertIn('Debug.Log("CHL003_ACTUAL_TRIAL_EVENTS_CSV',self.demo)
        self.assertNotIn("PlayerPrefs",self.demo)
        self.assertNotIn("File.Write",self.demo)

    def test_29_new_w26_gate_crossing_cannot_write_rewards(self):
        source=(ROOT/"Runtime/CHL003GateCrossing.cs").read_text(encoding="utf8")
        self.assertIn("public static bool IsForwardFullBodyCrossing(",source)
        self.assertIn("facingDot >= .5f",source)
        self.assertEqual(audit.inspect_chl_files([("CHL003/Runtime/CHL003GateCrossing.cs",source)]),1)
        mutated=source.replace("public static bool IsForwardFullBodyCrossing(",
            "public static void Grant(){ VehicleModel.Award(999); }\\npublic static bool IsForwardFullBodyCrossing(",1)
        self.assertNotEqual(source,mutated)
        with self.assertRaisesRegex(audit.QAError,"DIRECT_REWARD_WRITE"):
            audit.inspect_chl_files([("CHL003/Runtime/CHL003GateCrossing.cs",mutated)])

    def test_30_w26_demo_assembly_rejects_host_dependency(self):
        asm=json.loads((ROOT/"Demo/B01.CHL003.Demo.asmdef").read_text())
        self.assertEqual(asm["references"],["B01.CHL003.Runtime"])
        self.assertNotIn("Assembly-CSharp",asm["references"])
        self.assertNotIn("RoboRougelike.R3",asm["references"])

    def test_31_current_wheel_source_has_no_rewards_and_near_contact_only(self):
        motor=(ROOT/"Runtime/CHL003VehicleMotor.cs").read_text(encoding="utf8")
        wheel=(ROOT/"Runtime/CHL003WheelSupportWindow.cs").read_text(encoding="utf8")
        self.assertIn("CHL003WheelSupportWindow.WithinWheelReach(",motor)
        self.assertIn("NearContactAllowanceMeters = 0.03f",wheel)
        self.assertEqual(audit.inspect_chl_files([
            ("CHL003/Runtime/CHL003VehicleMotor.cs",motor),
            ("CHL003/Runtime/CHL003WheelSupportWindow.cs",wheel)]),2)

    def test_25_actual_demo_owns_runtime_created_root(self):
        self.assertIn('owned = new GameObject("CHL003_GENERATED_DEMO_ONLY");',self.demo)
        self.assertIn("go.transform.SetParent(owned.transform);",self.demo)
        self.assertIn("if (owned != null) Destroy(owned);",self.demo)

if __name__=="__main__":
    unittest.main(verbosity=2)
