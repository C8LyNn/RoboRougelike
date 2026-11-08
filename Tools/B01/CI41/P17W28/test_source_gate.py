"""12 hostile original-source QA controls; Unity tests are authored NOT executed."""
import json,pathlib,sys,unittest
sys.path.insert(0,str(pathlib.Path(__file__).resolve().parent))
import source_gate as g
ROOT=pathlib.Path(__file__).resolve().parents[4]
C=(ROOT/g.OWN_NATIVE).read_text(encoding="utf-8")

class W28SourceG0(unittest.TestCase):
    def negative(self,needle,new):
        self.assertIn(needle,C)
        with self.assertRaises(AssertionError):g.validate_native(C.replace(needle,new))
    def test_01_real_unity_contract_positive(self):
        self.assertGreaterEqual(g.validate_native(C),20)
    def test_02_exact_four_genuine_tests(self):
        self.assertEqual(C.count("[UnityTest]"),4)
        self.assertEqual(len(g.TESTS),4)
    def test_03_exact_codex824_original_sha(self):
        self.assertEqual(g.BASE,"e03d36f2b86f8c73a067e574b67237a8921f622a")
    def test_04_original_source_inventory(self):
        self.assertEqual(len(g.ORIGINAL),11)
        self.assertIn("Assets/WeaponIntegration/RRLSawBinding.cs",g.ORIGINAL)
        self.assertIn("Assets/WeaponIntegration/RRLEmpBinding.cs",g.ORIGINAL)
    def test_05_owned_delta_is_seven_additions(self):
        self.assertEqual(len(g.OWN),7)
        self.assertEqual(g.OWN_NATIVE,"Assets/Tests/Editor/CI41W28SawEmpNativeRegressionTests.cs")
    def test_06_reject_mock_physics_type(self):
        with self.assertRaisesRegex(AssertionError,"FAKE_GAMEPLAY_OR_PHYSICS_TYPE"):
            g.validate_native(C+"\nclass Physics {}")
    def test_07_reject_missing_saw_heat(self):
        self.negative("saw.Module.Motor.Heat","saw.Module.Motor.Wish")
    def test_08_reject_missing_emp_real_physical_overlap(self):
        self.negative("binding.Module.ConfigurePhysics(","binding.ConfigureFake(")
    def test_09_reject_no_real_game_build(self):
        self.negative("R3VehicleAssembly.Build(","MockAssembly.Build(")
    def test_10_reject_no_real_collider_sync(self):
        self.negative("Physics.SyncTransforms()","NoPhysicsSync()")
    def test_11_reject_missing_own_emp_module(self):
        self.negative("own.Receiver.IsDisrupted","own.MockedReceiver.IsDisrupted")
    def test_12_reject_silent_no_native_framework(self):
        self.assertIn("new EnterPlayMode()",C)
        with self.assertRaisesRegex(AssertionError,"NOT_EXACTLY_4_GENUINE_NATIVE"):
            g.validate_native(C.replace("[UnityTest]","[Test]"))

if __name__=="__main__":unittest.main(verbosity=2)
