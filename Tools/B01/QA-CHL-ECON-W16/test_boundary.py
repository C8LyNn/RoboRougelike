"""Actual immutable Codex C# static audit + explicit mutation controls, NOT Unity."""
import pathlib
import sys
import unittest

HERE=pathlib.Path(__file__).resolve().parent
sys.path.insert(0,str(HERE))
import audit

R1=HERE/"_pinned/Assets/R1/R1RunController.cs"
R3=HERE/"_pinned/Assets/R3/R3VehicleModel.cs"

class OriginalCodexHostSourceTest(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        if not R1.is_file() or not R3.is_file():
            raise RuntimeError("ACTUAL_FROZEN_HOST_CSHARP_REQUIRED — prepare.py first")
        cls.run=R1.read_text(encoding="utf8")
        cls.model=R3.read_text(encoding="utf8")

    def test_01_unmodified_pinned_real_source_audit(self):
        r=audit.inspect_host(self.run,self.model)
        self.assertEqual(r["host_win_award_calls"],1)
        self.assertFalse(r["host_victory_event_id_journal_in_method"])

    def test_02_absent_combat_gate_is_rejected(self):
        altered=self.run.replace("Progress.State != R1RunState.Combat || Paused) return;",
          "Progress.State != R1RunState.Garage || Paused) return;",1)
        self.assertNotEqual(altered,self.run)
        with self.assertRaisesRegex(audit.QAError,"HOST_COMBAT_GATE_REMOVED"):
            audit.inspect_host(altered,self.model)

    def test_03_duplicate_credit_award_is_rejected(self):
        old="VehicleModel.Award(LastScrapReward);"
        altered=self.run.replace(old,old+"\n"+old,1)
        self.assertNotEqual(altered,self.run)
        with self.assertRaisesRegex(audit.QAError,"HOST_REWARD_MISSING_OR_DUPLICATED"):
            audit.inspect_host(altered,self.model)

    def test_04_reversed_loss_condition_is_rejected_as_semantic_failure(self):
        # RED fixture: old scanner only detects changed order, not changed
        # meaning. Add a precise fail-closed predicate check after RED CI.
        changed=self.run.replace("if (defeat != null)","if (defeat == null)",1)
        self.assertNotEqual(changed,self.run)
        with self.assertRaisesRegex(audit.QAError,"HOST_LOSS_PREDICATE_CHANGED"):
            audit.inspect_host(changed,self.model)

    def test_05_missing_post_win_salvage_transition_rejected(self):
        altered=self.run.replace("Progress.OpenSalvage();","/*removed*/",1)
        with self.assertRaisesRegex(audit.QAError,"HOST_SALVAGE_STAGE_MISSING"):
            audit.inspect_host(altered,self.model)

    def test_06_missing_reward_commit_guard_rejected(self):
        altered=self.run.replace("Progress?.State!=R1RunState.Salvage",
            "Progress?.State!=R1RunState.Combat",1)
        with self.assertRaisesRegex(audit.QAError,"HOST_SALVAGE_CONFIRM_UNGUARDED"):
            audit.inspect_host(altered,self.model)

    def test_07_repair_price_baseline_changed_requires_new_pin(self):
        altered=self.model.replace("part.Health <= 0 ? Price(part.Kind,part.Weapon)",
               "part.Health <= 0 ? 1",1)
        with self.assertRaisesRegex(audit.QAError,"PINNED_0HP_REPAIR_PRICE_SOURCE_CHANGED"):
            audit.inspect_host(self.run,altered)

class CHLSourceGateMutationTests(unittest.TestCase):
    def test_08_missing_real_CHL_never_reported_pass(self):
        v=audit.inspect_chl_directory(HERE/"CHL_DOES_NOT_EXIST")
        self.assertEqual(v["state"],"SOURCE_NOT_PRESENT")
        self.assertEqual(v["native"],"NOT_RUN")

    def test_09_neutral_checkpoint_class_allowed_as_static_fixture_only(self):
        n=audit.inspect_chl_files([
          ("CHL003/Runtime/OrderedCheckpoints.cs",
           "namespace CHL003 { class OrderedCheckpoints { event System.Action Completed; } }")])
        self.assertEqual(n,1)

    def test_10_direct_reward_payment_rejected(self):
        with self.assertRaisesRegex(audit.QAError,"DIRECT_REWARD_WRITE"):
            audit.inspect_chl_files([("CHL003/Runtime/Bad.cs",
              "class Bad { void Tick() { VehicleModel.Award(100); } }")])

    def test_11_salvage_grant_from_adapter_rejected(self):
        with self.assertRaisesRegex(audit.QAError,"DIRECT_REWARD_WRITE"):
            audit.inspect_chl_files([("CHL003/Adapters/Bad.cs",
              "class Bad { void OnComplete() { VehicleModel.ReceiveSalvage(items); } }")])

    def test_12_unverified_winencounter_from_adapter_rejected(self):
        with self.assertRaisesRegex(audit.QAError,"RUN_REWARD_MUTATION"):
            audit.inspect_chl_files([("CHL003/Integration/Bad.cs",
              "class Bad { void OnComplete() { Progress.WinEncounter(a,b); } }")])

    def test_13_host_R3_dependency_inside_course_core_rejected(self):
        with self.assertRaisesRegex(audit.QAError,"CHL_HOST_IMPORT_IN_CORE"):
            audit.inspect_chl_files([("CHL003/Runtime/Bad.cs",
              "using RoboRougelike.R3;\nclass A {}")])

    def test_14_host_R3_import_in_optional_adapter_alone_is_not_reward(self):
        self.assertEqual(audit.inspect_chl_files([("CHL003/Adapters/View.cs",
            "using RoboRougelike.R3;\nclass View {}")]),1)

    def test_15_direct_scrap_mutation_rejected(self):
        with self.assertRaisesRegex(audit.QAError,"DIRECT_CURRENCY_WRITE"):
            audit.inspect_chl_files([("CHL003/Runtime/Bad.cs",
              "class Bad { void Victory() { Scrap += 9999; } }")])

if __name__=="__main__":
    unittest.main(verbosity=2)
