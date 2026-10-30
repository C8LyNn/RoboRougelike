#!/usr/bin/env python3
"""Independent W10 producer source-contract / fail-closed mutation tests."""
import copy
import json
import os
import unittest
from gate import ORIGINALS, GateError, assess, blob_sha, originals, method

class DockApiStateQA(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.src=originals(os.environ.get("B01_REPO","."))
    def mutate(self,key,old,new):
        out=dict(self.src)
        count=out[key].count(old)
        self.assertEqual(count,1, "fixture must mutate exactly once: "+key+":"+old)
        out[key]=out[key].replace(old,new)
        return out
    def fail(self,key,old,new):
        with self.assertRaises(GateError):
            assess(self.mutate(key,old,new))

    def test_01_six_exact_frozen_source_git_blobs(self):
        self.assertEqual(len(ORIGINALS),6)
        for k,(_path,pin) in ORIGINALS.items():
            self.assertEqual(blob_sha(self.src[k].encode("utf-8")),pin)
    def test_02_w09_guards_demo_but_not_public_method(self):
        v=assess(self.src)
        self.assertEqual(v["w09_demo_ui_source_guard"],"OBSERVED")
        self.assertEqual(v["public_reattach_host_guard"],"HOLD_UNSAFE_PUBLIC_API")
    def test_03_actual_legacy_gui_and_unprotected_reattach(self):
        c=assess(self.src)["checks"]
        self.assertTrue(c["w07_legacy_gui_calls_raw_reattach"])
        self.assertTrue(c["w07_exposes_public_raw_reattach"])
        self.assertTrue(c["w07_raw_reposition_and_id_rearm"])
    def test_04_real_w09_all_physics_layers(self):
        self.assertTrue(assess(self.src)["checks"]["w09_all_layer_overlap_and_penetration"])
    def test_05_w09_independent_original_w08_inspect(self):
        self.assertTrue(assess(self.src)["checks"]["w09_requires_both_physics_and_old_gate"])
    def test_06_original_w08_physx_api_present(self):
        self.assertTrue(assess(self.src)["checks"]["w08_actual_physics"])
    def test_07_arm_disables_original_gui_components(self):
        self.assertTrue(assess(self.src)["checks"]["w09_arm_disables_both_legacy_behaviours"])
    def test_08_runtime_update_protects_original_gui(self):
        self.assertTrue(assess(self.src)["checks"]["w09_update_redisables_legacy_behaviours"])
    def test_09_W09_never_claims_native_or_codex_ack(self):
        v=assess(self.src)
        self.assertEqual(v["unity_native"],"NOT_RUN")
        self.assertEqual(v["codex_ack"],"NOT_RECEIVED")
        self.assertEqual(v["release_gate"],"HOLD")
    def test_10_receipts_are_deterministic(self):
        self.assertEqual(json.dumps(assess(self.src),sort_keys=True),json.dumps(assess(self.src),sort_keys=True))
    def test_11_reversed_reattach_permission_is_rejected(self):
        self.fail("w09","if (Inspect(index) != HardenedDockVerdict.Allowed)","if (Inspect(index) == HardenedDockVerdict.Allowed)")
    def test_12_removed_w09_legacy_demo_disable_is_rejected(self):
        self.fail("w09","demo.enabled = false;\n            previousGate.enabled = false;","demo.enabled = true;\n            previousGate.enabled = false;")
    def test_13_removed_w09_legacy_w08_disable_is_rejected(self):
        self.fail("w09","demo.enabled = false;\n            previousGate.enabled = false;","demo.enabled = false;\n            previousGate.enabled = true;")
    def test_14_removed_w09_actual_all_layer_query_is_rejected(self):
        self.fail("w09","Quaternion.identity, HardenedDockPolicy.AllPhysicsLayers,","Quaternion.identity, 0,")
    def test_15_removed_real_penetration_test_rejected(self):
        self.fail("w09","Physics.ComputePenetration(","FakeComputePenetration(")
    def test_16_removed_original_w08_gate_rejected(self):
        self.fail("w09","previousGate.Inspect(demo, index)","DockVerdict.Allowed")
    def test_17_direct_replacement_of_safe_gui_handler_rejected(self):
        self.fail("w09","if (GUILayout.Button(\"SAFE reattach\")) TryDock(i);",
                        "if (GUILayout.Button(\"SAFE reattach\")) demo.Reattach(i);")
    def test_18_downgraded_all_layers_constant_rejected(self):
        self.fail("policy","public const int AllPhysicsLayers = -1;","public const int AllPhysicsLayers = 0;")
    def test_19_rearming_removed_from_original_path_rejected(self):
        self.fail("w07","rig.Emitter.RearmAfterReattach(","rig.Emitter.SkipRearm(")
    def test_20_w09_rearm_is_not_a_persistence_fix(self):
        v=assess(self.src)
        self.assertNotIn("G1_PASS",json.dumps(v))
        self.assertEqual(v["original_legacy_source"],"IMMUTABLE")
    def test_21_prior_gate_unarmed_denial_can_not_be_deleted(self):
        self.fail("policy","return HardenedDockVerdict.NotArmed;","return HardenedDockVerdict.Allowed;")
    def test_22_prior_gate_unsafe_gui_denial_can_not_be_deleted(self):
        self.fail("policy","return HardenedDockVerdict.UnsafeLegacyUi;","return HardenedDockVerdict.Allowed;")
    def test_23_negated_old_gui_sources_are_not_misclassified_as_fixed(self):
        s=self.mutate("w07",'if (GUILayout.Button("Reattach")) Reattach(i);',
                             'if (GUILayout.Button("Reattach")) { /* no call */ }')
        v=assess(s)
        self.assertTrue(v["checks"]["w07_exposes_public_raw_reattach"])
        self.assertFalse(v["checks"]["w07_legacy_gui_calls_raw_reattach"])
        self.assertEqual(v["public_reattach_host_guard"],"HOLD_UNSAFE_PUBLIC_API")
    def test_24_comment_fake_guard_does_not_secure_original_public_api(self):
        s=dict(self.src)
        s["w07"]=s["w07"].replace("public bool Reattach(int index)",
                           "/* Inspect(index) */ public bool Reattach(int index)")
        self.assertEqual(assess(s)["public_reattach_host_guard"],"HOLD_UNSAFE_PUBLIC_API")
    def test_25_does_not_mutate_original_source_on_assessment(self):
        before=copy.deepcopy(self.src)
        assess(self.src)
        self.assertEqual(self.src,before)
    def test_26_ambiguous_methods_fail_closed(self):
        s=dict(self.src)
        s["w09"] += "\npublic bool TryDock(int index) { return true; }\n"
        with self.assertRaises(GateError):
            assess(s)
if __name__=="__main__": unittest.main(verbosity=2)
