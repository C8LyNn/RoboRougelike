#!/usr/bin/env python3
"""CI-HEALTH-001: three adversarial original Git diffs + safety controls.

No Unity runs, no credential access. Original Git refs must be fetched by
the isolated QA-only workflow before these tests execute.
"""
import json
from pathlib import Path
import subprocess
import sys
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parent))
import ci_health as h

ROOT = Path(__file__).resolve().parents[4]  # Tools/B01/CI41/W74/test -> repo
QA_BASE = "fb9e7b8b6d844bcac0351a3d7fb8f51cb2c45892"
QA_HEAD = "ba104561e833fb8914979a5d2f4c2d2baa4ea792"
CHL_BASE = "2355bff6026e22443b164aa9de816c27d3a289e9"
CHL_HEAD = "c7b2a234a286089383503da41883d624fe650551"


class CIHealthSourceTests(unittest.TestCase):
    def test_01_adversarial_real_QA_only_git_diff_retains_stable_check(self):
        paths = h.original_git_paths(ROOT, QA_BASE, QA_HEAD)
        self.assertEqual(len(paths), 2, paths)
        self.assertEqual(h.classify(paths)["classification"], "QA_ONLY")
        d = h.classify(paths)
        self.assertFalse(d["must_execute_unity"])
        self.assertEqual(d["non_native_label"], "NOT_APPLICABLE_QA_ONLY_NOT_UNITY_G1")
        self.assertEqual(d["G1"], "NOT_RUN")
        print("ORIGINAL_QA_ONLY_DIFF=QA_ONLY_NON_NATIVE_NOT_G1")

    def test_02_adversarial_real_CHL_assets_test_change_requires_Unity(self):
        paths = h.original_git_paths(ROOT, CHL_BASE, CHL_HEAD)
        self.assertGreater(len(paths), 0)
        self.assertTrue(any(s.startswith("Assets/") for s in paths), paths)
        d = h.classify(paths)
        self.assertEqual(d["classification"], "NATIVE_REQUIRED")
        self.assertTrue(d["must_execute_unity"])
        self.assertEqual(d["credential_gate_when_unavailable"], "FAIL_CLOSED")
        print("ORIGINAL_CHL_ASSETS_SOURCE=NATIVE_REQUIRED_FAIL_CLOSED")

    def test_03_adversarial_mixed_original_diffs_never_skip_native(self):
        qa = h.original_git_paths(ROOT, QA_BASE, QA_HEAD)
        native = h.original_git_paths(ROOT, CHL_BASE, CHL_HEAD)
        self.assertEqual(h.classify(qa + native)["classification"], "NATIVE_REQUIRED")
        self.assertEqual(h.classify([])["classification"], "NATIVE_REQUIRED")
        for p in (".github/workflows/ci-02-editmode.yml", "ProjectSettings/ProjectVersion.txt",
                  "Packages/manifest.json", "../Tools/B01/ci.py", "Assets/Module.cs",
                  "Tools/B01/../../Assets/Fake.cs", ".github/workflows/unknown-native.yml"):
            self.assertEqual(h.classify(qa + [p])["classification"], "NATIVE_REQUIRED", p)
        print("MIXED_OR_UNRECOGNIZED_OR_EMPTY_DIFF=NATIVE_REQUIRED_FAIL_CLOSED")

    def test_04_actual_original_CI02_03_workflow_guards_pinned(self):
        source = h.audit_original_workflows(ROOT)
        self.assertEqual(set(source), set(h.ORIGINAL_CI_BLOBS))
        self.assertTrue(all(x["native_gate"] == "PRESENT" for x in source.values()))
        print("ORIGINAL_MAIN_CI02_CI03_GATES_PINNED_CHECKS_UNCONDITIONAL")

    def test_05_required_context_filter_risk_is_explicit(self):
        self.assertEqual(h.required_context_risk(workflow_skipped=True, required=True),
                         "REQUIRED_CONTEXT_MAY_BE_MISSING_OR_PENDING")
        self.assertEqual(h.required_context_risk(workflow_skipped=False, required=True),
                         "CHECK_EMITTED_NATIVE_OR_EXPLICIT_NOT_APPLICABLE")
        self.assertEqual(h.classify(["Tools/B01/CI41/W74/ci_health.py"])["G1"], "NOT_RUN")
        self.assertEqual(h.classify(["Assets/R3/R3VehicleModel.cs"])["G1"], "NOT_RUN")
        print("EVENT_PATH_FILTER_RISK_NOT_TREATED_AS_NATIVE_TEST_PASS")


    def test_07_original_fail_open_to_new_fail_closed_critical_operators(self):
        # Directly execute the PRE-FIX ORIGINAL immutable Git source, not a reimplementation.
        import types
        old_blob = h.git(ROOT, "rev-parse", "ef03cf7bbd53cc015749e1bbe28557989ca04b58:Tools/B01/CI41/W74/ci_health.py").decode().strip()
        self.assertEqual(old_blob, "5c412815413c7886d1933e3a803cf5af5b7df21b")
        pre = types.ModuleType("original_b01_41_ci_health_prior")
        exec(compile(h.git(ROOT, "show", "ef03cf7bbd53cc015749e1bbe28557989ca04b58:Tools/B01/CI41/W74/ci_health.py"),
                     "original_unchanged_before_bugfix.py", "exec"), pre.__dict__)
        critical_paths = [
            "Tools/B01/CHL003/W66Receipt/chl003_w66_operator.py",
            "Tools/B01/PARTSIZE001/W26Receipt/Run-UnityNative.ps1",
            "Tools/B01/PARTSIZE001/W26Receipt/check_receipt.py",
            "Docs/B01/CHL003/manifest.json",
            "Docs/B01Validation/provenance-receiver.md",
            ".github/workflows/b01-chl003-w66-source-pin.yml",
        ]
        for path in critical_paths:
            self.assertEqual(pre.classify([path])["classification"], "QA_ONLY", path)
            decision = h.classify([path])
            self.assertEqual(decision["classification"], "NATIVE_REQUIRED", path)
            self.assertEqual(decision["credential_gate_when_unavailable"], "FAIL_CLOSED")
            self.assertEqual(decision["G1"], "NOT_RUN")
        print("P0_ORIGINAL_QA_ALLOWLIST_FAIL_OPEN_REPRODUCED_THEN_FIXED=6")

    def test_08_original_b01_41_paths_remain_non_native(self):
        for path in (
            "Tools/B01/CI41/W74/ci_health.py",
            "Tools/B01/CI41/W78/required_aggregate.py",
            ".github/workflows/b01-41-w74-ci-health.yml",
        ):
            self.assertEqual(h.classify([path])["classification"], "QA_ONLY")
        self.assertEqual(h.classify([".github/workflows/b01-23-w26-partsize-receipt.yml"])["classification"],
                         "NATIVE_REQUIRED")
        print("B01_41_OWN_QA_STILL_ALLOWED_AND_OTHER_OWNERS_FAIL_CLOSED")

    def test_06_conservative_git_ref_validation(self):
        with self.assertRaises(ValueError):
            h.original_git_paths(ROOT, "main", QA_HEAD)
        with self.assertRaises(RuntimeError):
            h.original_git_paths(ROOT, "0" * 40, "1" * 40)


if __name__ == "__main__":
    unittest.main(verbosity=2)
