"""Adversarial tests of independent W07/W08/W09 source-route audit.
Synthetic fixtures validate the scanner. CI also executes on pinned real Git bytes.
"""
import copy
import json
import unittest
from unittest.mock import patch
from route_gate import inspect, render, REVISIONS, original_sources, SourceDrift

BASE = {
    "w07": '''public sealed class DebrisRigPlayground {
 public bool Reattach(int index) {
  rig.Body.isKinematic = true;
  rig.Part.transform.SetParent(rig.Root.transform, false);
  rig.Detached = false;
  rig.Emitter.RearmAfterReattach(rig.Definition.PartId);
  return true;
 }
 private void OnGUI() { if (GUILayout.Button("Reattach")) Reattach(i); }
}''',
    "w08": '''class DebrisSafeReattachGate {
 public bool TrySafeReattach(DebrisRigPlayground demo, int index) {
  if (Inspect(demo, index) != DockVerdict.Allowed) return false;
  return demo.Reattach(index);
 }
}''',
    "w09": '''class HardenedDockDemo {
 public bool Arm() { demo.enabled = false; previousGate.enabled = false; return true; }
 private void Update() { if (demo != null && demo.enabled) demo.enabled = false; if (previousGate.enabled) previousGate.enabled = false; }
 public bool TryDock(int index) { if (Inspect(index) != HardenedDockVerdict.Allowed) return false; return demo.Reattach(index); }
 public HardenedDockVerdict Inspect(int index) { var a=AuditEveryLayer(index); var b=previousGate.Inspect(demo, index); return HardenedDockVerdict.Allowed; }
 private DockVerdict AuditEveryLayer(int index) { Physics.OverlapBoxNonAlloc(x, y, z, q, HardenedDockPolicy.AllPhysicsLayers); Physics.ComputePenetration(a,b,c,d,e,f,out g,out h); return DockVerdict.Allowed; }
 private void OnGUI() { if (GUILayout.Button("SAFE reattach")) TryDock(i); }
}''',
    "policy": '''public static class HardenedDockPolicy { public static HardenedDockVerdict Evaluate(bool armed) { if (queryLayerMask != AllPhysicsLayers) return HardenedDockVerdict.InvalidFullLayerQuery; return HardenedDockVerdict.Allowed; }}''',
    "installer": '''public static class HardenedDockInstaller { public static void Install() { Undo.AddComponent<HardenedDockDemo>(host); }}'''
}

class RouteGateTests(unittest.TestCase):
    def test_01_positive_control_proves_hold_not_green(self):
        self.assertEqual(inspect(BASE)["verdict"], "API_BYPASS_HOLD")
    def test_02_all_call_routes_must_not_be_reported_safe(self):
        self.assertFalse(inspect(BASE)["all_call_routes_are_guarded"])
    def test_03_does_not_pretend_native_unity(self):
        self.assertEqual(inspect(BASE)["native_unity_physx"], "NOT_RUN")
    def test_04_receipt_determinism(self):
        self.assertEqual(render(BASE), render(copy.deepcopy(BASE)))
        self.assertEqual(json.loads(render(BASE))["codex_ack"], False)
    def test_05_public_api_absent_fails_closed(self):
        x=copy.deepcopy(BASE); x["w07"]=x["w07"].replace("public bool Reattach", "private bool Reattach")
        with self.assertRaises(SourceDrift): inspect(x)
    def test_06_original_ledger_rearm_absent_fails_closed(self):
        x=copy.deepcopy(BASE); x["w07"]=x["w07"].replace("RearmAfterReattach", "ClearLedgerWithoutRearm")
        with self.assertRaises(SourceDrift): inspect(x)
    def test_07_legacy_gui_call_rewired_requires_review(self):
        x=copy.deepcopy(BASE); x["w07"]=x["w07"].replace("Reattach(i)", "GuardedDock(i)")
        with self.assertRaises(SourceDrift): inspect(x)
    def test_08_original_public_method_guard_added_requires_review(self):
        x=copy.deepcopy(BASE); x["w07"]=x["w07"].replace("rig.Body.isKinematic = true;", "if (Inspect(index) == DockVerdict.Blocked) return false; rig.Body.isKinematic = true;")
        with self.assertRaises(SourceDrift): inspect(x)
    def test_09_w08_guard_removed_fails_closed(self):
        x=copy.deepcopy(BASE); x["w08"]=x["w08"].replace("if (Inspect(demo, index) != DockVerdict.Allowed) return false;", "")
        with self.assertRaises(SourceDrift): inspect(x)
    def test_10_w09_guard_removed_fails_closed(self):
        x=copy.deepcopy(BASE); x["w09"]=x["w09"].replace("if (Inspect(index) != HardenedDockVerdict.Allowed) return false;", "")
        with self.assertRaises(SourceDrift): inspect(x)
    def test_11_guard_after_mutation_fails_closed(self):
        x=copy.deepcopy(BASE); x["w09"]=x["w09"].replace("if (Inspect(index) != HardenedDockVerdict.Allowed) return false; return demo.Reattach(index);", "return demo.Reattach(index); if (Inspect(index) != HardenedDockVerdict.Allowed) return false;")
        with self.assertRaises(SourceDrift): inspect(x)
    def test_12_inspect_loses_old_gate_fails_closed(self):
        x=copy.deepcopy(BASE); x["w09"]=x["w09"].replace("previousGate.Inspect(demo, index)", "DockVerdict.Allowed")
        with self.assertRaises(SourceDrift): inspect(x)
    def test_13_all_layer_mask_regression_fails_closed(self):
        x=copy.deepcopy(BASE); x["w09"]=x["w09"].replace("HardenedDockPolicy.AllPhysicsLayers", "collisionLayers")
        with self.assertRaises(SourceDrift): inspect(x)
    def test_14_missing_penetration_fails_closed(self):
        x=copy.deepcopy(BASE); x["w09"]=x["w09"].replace("Physics.ComputePenetration(", "Physics.Raycast(")
        with self.assertRaises(SourceDrift): inspect(x)
    def test_15_installer_no_longer_installs_gate_fails_closed(self):
        x=copy.deepcopy(BASE); x["installer"]=x["installer"].replace("Undo.AddComponent<HardenedDockDemo>(host)", 'Debug.Log("skip")')
        with self.assertRaises(SourceDrift): inspect(x)
    def test_16_arm_fails_to_disable_original_fails_closed(self):
        x=copy.deepcopy(BASE); x["w09"]=x["w09"].replace("demo.enabled = false;", "demo.enabled = true;", 1)
        with self.assertRaises(SourceDrift): inspect(x)
    def test_17_policy_omits_all_layer_restriction_fails_closed(self):
        x=copy.deepcopy(BASE); x["policy"]=x["policy"].replace("queryLayerMask != AllPhysicsLayers", "queryLayerMask == AllPhysicsLayers")
        with self.assertRaises(SourceDrift): inspect(x)
    def test_18_comments_dont_count_as_guard(self):
        x=copy.deepcopy(BASE); x["w07"]=x["w07"].replace("rig.Body.isKinematic = true;", "// Inspect(index) is NOT called here\n  rig.Body.isKinematic = true;")
        self.assertEqual(inspect(x)["verdict"], "API_BYPASS_HOLD")
    def test_19_string_literal_doesnt_count_as_guard(self):
        x=copy.deepcopy(BASE); x["w07"]=x["w07"].replace("rig.Body.isKinematic = true;", 'var msg="TryDock(index)"; rig.Body.isKinematic = true;')
        self.assertEqual(inspect(x)["verdict"], "API_BYPASS_HOLD")
    def test_20_blob_pin_fails_closed(self):
        from route_gate import git_blob
        self.assertNotEqual(git_blob(b"altered"), REVISIONS["w07"][2])
    def test_21_git_source_mismatch_error_instead_of_forged_receipt(self):
        with patch("route_gate.subprocess.check_output", side_effect=[b"wrong bytes", b"wrongref"]):
            with self.assertRaises(SourceDrift): original_sources(".")

if __name__ == "__main__":
    unittest.main()
