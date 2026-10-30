#!/usr/bin/env python3
"""Original source integrity + actual route guard negative controls (not Unity)."""
import copy
import json
import os
import unittest
from intake import (
    FILES, SourceDrift, fetch_original, git_blob, inspect_sources, report, method,
)

class ReattachRouteIntake(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.sources=fetch_original(os.environ.get("B01_REPO","."))

    def changed(self,key,old,new):
        obj=dict(self.sources)
        if obj[key].count(old)!=1:
            raise AssertionError("bad mutated fixture "+key+" expected once "+old)
        obj[key]=obj[key].replace(old,new)
        return obj

    def test_01_all_five_immutable_original_git_blobs_exact(self):
        self.assertEqual(len(FILES),5)
        for key,(_,path,blob) in FILES.items():
            self.assertEqual(git_blob(self.sources[key].encode("utf-8")),blob)

    def test_02_original_unprotected_gui_still_reaches_reattach(self):
        actual=inspect_sources(self.sources)
        self.assertTrue(actual["old_gui_direct_recomposition_bypass"])
        self.assertTrue(actual["old_gui_button"])
        self.assertTrue(actual["old_gui_calls_reposition_and_rearm"])
        self.assertEqual(actual["release_integration_guard"],"HOLD_UNGUARDED_W07_UI")

    def test_03_original_reattach_rearms_the_same_stable_id(self):
        out=inspect_sources(self.sources)
        self.assertTrue(out["original_reposition_rearms_stable_id"])

    def test_04_new_gate_inspects_real_physics_API_and_guards_reattach(self):
        out=inspect_sources(self.sources)
        self.assertTrue(out["safe_gate_checks_real_physics_observations"])
        self.assertTrue(out["safe_gate_calls_original_reattach_only_after_allow"])

    def test_05_new_gui_calls_safe_reattach(self):
        self.assertTrue(inspect_sources(self.sources)["safe_ui_calls_gate"])

    def test_06_new_editor_installer_is_opt_in_not_w07_gui_replacement(self):
        self.assertTrue(inspect_sources(self.sources)[
            "installer_opt_in_only_no_original_ui_replace"])

    def test_07_unsafe_gui_disabled_in_memory_is_no_longer_source_red(self):
        src=self.changed("playground",
            'if (GUILayout.Button("Reattach")) Reattach(i);',
            'if (GUILayout.Button("Reattach")) { /* gated by host, disabled */ }')
        out=inspect_sources(src)
        self.assertFalse(out["old_gui_direct_recomposition_bypass"])
        self.assertEqual(out["release_integration_guard"],
                         "NO_STATIC_UI_BYPASS_DETECTED_NEEDS_NATIVE_REVIEW")
        self.assertFalse(out["fully_protected_native_playground"])

    def test_08_comment_fake_reattach_does_not_false_trigger(self):
        src=self.changed("playground",
            'if (GUILayout.Button("Reattach")) Reattach(i);',
            'if (GUILayout.Button("Reattach")) { /* Reattach(i); */ }')
        self.assertFalse(inspect_sources(src)["old_gui_direct_recomposition_bypass"])

    def test_09_string_fake_reattach_does_not_false_trigger(self):
        src=self.changed("playground",
            'if (GUILayout.Button("Reattach")) Reattach(i);',
            'if (GUILayout.Button("Reattach")) { GUILayout.Label("Reattach(i);"); }')
        self.assertFalse(inspect_sources(src)["old_gui_direct_recomposition_bypass"])

    def test_10_new_hidden_direct_call_even_different_index_is_red(self):
        src=self.changed("playground",
            'if (GUILayout.Button("Reattach")) Reattach(i);',
            'if (GUILayout.Button("Reattach")) { Reattach(1); }')
        self.assertTrue(inspect_sources(src)["old_gui_direct_recomposition_bypass"])

    def test_11_safe_gate_reversed_denial_is_detected(self):
        src=self.changed("safegate",
            'if (Inspect(demo, index) != DockVerdict.Allowed)',
            'if (Inspect(demo, index) == DockVerdict.Allowed)')
        with self.assertRaisesRegex(SourceDrift,"ORIGINAL_SOURCE_CONTRACT_OR_SAFE_BRANCH_DRIFT"):
            inspect_sources(src)

    def test_12_safe_gate_without_real_Penetration_is_detected(self):
        src=self.changed("safegate","Physics.ComputePenetration(",
                         "FakeComputePenetration(")
        with self.assertRaisesRegex(SourceDrift,"ORIGINAL_SOURCE_CONTRACT_OR_SAFE_BRANCH_DRIFT"):
            inspect_sources(src)

    def test_13_old_reattach_without_ledger_rearm_is_detected(self):
        src=self.changed("playground",
            "rig.Emitter.RearmAfterReattach(rig.Definition.PartId)",
            "rig.Emitter.NoRearm(rig.Definition.PartId)")
        with self.assertRaisesRegex(SourceDrift,"ORIGINAL_SOURCE_CONTRACT_OR_SAFE_BRANCH_DRIFT"):
            inspect_sources(src)

    def test_14_safe_gate_gui_directly_calls_old_method_is_detected(self):
        src=self.changed("safegate","TrySafeReattach(demo, i);","demo.Reattach(i);")
        with self.assertRaisesRegex(SourceDrift,"ORIGINAL_SOURCE_CONTRACT_OR_SAFE_BRANCH_DRIFT"):
            inspect_sources(src)

    def test_15_installer_without_opt_in_add_component_is_detected(self):
        src=self.changed("installer","Undo.AddComponent<DebrisSafeReattachGate>(",
                         "Undo.AddComponent<OtherGate>(")
        with self.assertRaisesRegex(SourceDrift,"ORIGINAL_SOURCE_CONTRACT_OR_SAFE_BRANCH_DRIFT"):
            inspect_sources(src)

    def test_16_physical_receiver_report_is_deterministic_and_explicit_HOLD(self):
        a=report(self.sources)
        b=report(self.sources)
        self.assertEqual(json.dumps(a,sort_keys=True),json.dumps(b,sort_keys=True))
        self.assertEqual(a["gate_result"],"SOURCE_INTEGRATION_HOLD")
        self.assertEqual(a["native_unity"],"NOT_RUN")
        self.assertFalse(a["original_source_modified"])

if __name__=="__main__":
    unittest.main(verbosity=2)
