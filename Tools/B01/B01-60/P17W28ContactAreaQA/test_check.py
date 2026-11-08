"""Executed B01-60 W28 ORIGINAL Codex #824 source audit, not Unity tests.

Adversarial evidence cases deliberately all fail; they NEVER fabricate Unity PASS.
"""
import json
from pathlib import Path
import tempfile
import unittest
import check

class RealW27SourceAndNewNativeScenarioSourceTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.receipt=check.source_check()
        cls.body=(check.ROOT/check.NEW).read_text()
        cls.saw=check.git("show",check.W27+":Assets/WeaponIntegration/RRLSawBinding.cs")
        cls.emp=check.git("show",check.W27+":Assets/WeaponIntegration/RRLEmpBinding.cs")
        cls.area=check.git("show",check.W27+":Assets/RoboRougelikeMechanics/WPN006EMP/Runtime/EmpEmitter.cs")
        cls.assembly=check.git("show",check.W27+":Assets/R3/R3VehicleAssembly.cs")

    def test_01_only_own_W28_source_and_exact_W27_codex_sha(self):
        self.assertEqual(self.receipt["codex_w27_sha"],check.W27)
        self.assertEqual(self.receipt["source_files_added"],8)
        self.assertEqual(self.receipt["head_sha"],check.git("rev-parse","HEAD"))

    def test_02_exact_eight_original_gameplay_blobs_unchanged(self):
        for path,blob in check.W27_BODY.items():
            self.assertEqual(check.git("rev-parse","HEAD:"+path),blob,path)

    def test_03_two_original_Unity_real_PhysX_test_methods_not_mocks(self):
        self.assertEqual(self.body.count("[UnityTest]"),2)
        self.assertEqual(len(check.CASES),2)
        for name in check.CASES:
            self.assertIn("public IEnumerator "+name+"()",self.body)
        self.assertIn("R3VehicleAssembly.Build(",self.body)
        self.assertIn("Physics.SyncTransforms()",self.body)
        self.assertNotIn("FakeUnityEngine",self.body)

    def test_04_Saw_uses_actual_owner_router_with_live_dynamic_part(self):
        self.assertIn("RRLHitRouter.CanHit(collider,Device.Owner)",self.saw)
        self.assertIn("Module.Advance(Time.fixedDeltaTime)",self.saw)
        for expected in ("TryResolve(collider, out _)","SetMotorRequested(true)",
                         "block.transform.SetParent(null, true)",
                         "WaitForFixedUpdate"):
            self.assertIn(expected,self.body)

    def test_05_EMP_uses_spatial_scan_not_projectile_rule(self):
        for expected in ("Physics.OverlapSphereNonAlloc","Physics.RaycastNonAlloc",
                         "IsFilteredByOwnership","cooldownTimer.Commit(cooldown)"):
            self.assertIn(expected,self.area)
        self.assertIn("Module.TryPulse(out var report)",self.emp)
        self.assertIn("wall.SetActive(false)",self.body)
        self.assertIn("enemyReceiver.IsDisrupted",self.body)
        self.assertIn("ownReceiver.IsDisrupted",self.body)
        self.assertNotIn("ClearProjectilePath",self.emp)

    def test_06_EMP_direct_and_real_LPM_group_exercised(self):
        self.assertIn("emp.Fire()",self.body)
        self.assertIn("assembly.FireGroup(false)",self.body)
        self.assertIn("emp.State.DeviceReadyAt",self.assembly)
        self.assertIn("canonical.DeviceReadyAt",self.body)

    def test_07_meta_guid_is_unique_and_source_native_is_honest(self):
        meta=(check.ROOT/check.META).read_text()
        self.assertIn("guid: "+check.GUID,meta)
        self.assertEqual(self.receipt["unity_playmode"],"NOT_RUN")
        self.assertEqual(self.receipt["windows_player"],"NOT_RUN")
        self.assertFalse(self.receipt["game_integrated"])

    def test_08_missing_actual_Unity_xml_log_cannot_pass_native(self):
        with tempfile.TemporaryDirectory(prefix="b0160-w28-missing-") as root:
            with self.assertRaises(ValueError):
                check.native_audit(root,"a"*40)

    def test_09_invalid_commit_ref_cannot_promote_native_evidence(self):
        with tempfile.TemporaryDirectory(prefix="b0160-w28-badsha-") as root:
            with self.assertRaises(ValueError):
                check.native_audit(root,"wrong-sha")

    def test_10_forged_non_native_receipt_cannot_pass_even_if_xml_looks_green(self):
        with tempfile.TemporaryDirectory(prefix="b0160-w28-fake-") as root:
            p=Path(root)
            (p/"W28-contact-area-results.xml").write_text(
                '<test-run result="Passed" failed="0" passed="2">'
                '<test-case result="Passed" fullname="fake"/>'
                '<test-case result="Passed" fullname="fake2"/></test-run>')
            (p/"W28-Editor.log").write_text("6000.3.14f1")
            (p/"W28-process.log").write_text("exit_code=0\n")
            (p/"W28-run.json").write_text(json.dumps({
              "head_sha":"a"*40,"original_codex_w27_sha":check.W27,
              "unity_version":"6000.3.14f1","native_invoked":False,
              "exit_code":0,"test_filter":check.SUITE,"sha256":{}}))
            with self.assertRaisesRegex(ValueError,"native never invoked"):
                check.native_audit(p,"a"*40)

if __name__=="__main__":
    unittest.main(verbosity=2)
