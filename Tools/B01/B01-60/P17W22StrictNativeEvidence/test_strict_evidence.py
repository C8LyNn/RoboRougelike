#!/usr/bin/env python3
"""Adversarial SYNTHETIC tests only: never claim native Unity was run."""
import json
from pathlib import Path
import sys
import unittest
import xml.etree.ElementTree as ET

W21 = Path(__file__).resolve().parents[1] / "P17W21WindowsUnityHandoff"
sys.path.insert(0, str(W21))
import test_verify_evidence as fixture21  # noqa: E402
import verify_evidence as old  # noqa: E402
import strict_evidence as guard  # noqa: E402


class StrictAuditTests(unittest.TestCase):
    def setUp(self):
        self.f = fixture21.GateTests("test_01_source_only_synthetic")
        self.f.setUp()
        self.addCleanup(self.f.tearDown)
        self.e = self.f.e

    def mutate_report(self, changes):
        report = json.loads(self.f.content["operator-report.json"])
        report.update(changes)
        self.f.content["operator-report.json"] = json.dumps(report).encode()
        self.f.write()

    def make_native(self):
        self.f.synthetic_native()
        verdict_path = "original-V2-native-only/native-verdict.json"
        verdict = json.loads(self.f.content[verdict_path])
        for mode, required in (("EditMode", old.EDIT), ("PlayMode", old.PLAY)):
            path = "original-V2-native-only/" + mode + ".xml"
            root = ET.fromstring(self.f.content[path])
            root.set("total", str(len(required)))
            root.set("skipped", "0")
            root.set("inconclusive", "0")
            self.f.content[path] = ET.tostring(root)
            verdict["results"][mode] = {
                "passed": len(required), "verified_independent_cases": len(required)}
        self.f.content[verdict_path] = json.dumps(verdict).encode()
        self.f.write()

    def edit_xml(self, mode, callback, sync_verdict=False):
        path = "original-V2-native-only/" + mode + ".xml"
        root = ET.fromstring(self.f.content[path])
        callback(root)
        self.f.content[path] = ET.tostring(root)
        if sync_verdict:
            p = "original-V2-native-only/native-verdict.json"
            v = json.loads(self.f.content[p])
            v["results"][mode]["passed"] = int(root.get("passed"))
            self.f.content[p] = json.dumps(v).encode()
        self.f.write()

    def reject(self):
        with self.assertRaises(old.Reject):
            guard.audit(self.e)

    def test_01_preflight_is_not_native(self):
        self.assertEqual(guard.audit(self.e), "SOURCE_ONLY_PASS_NATIVE_NOT_RUN")

    def test_02_preflight_rejects_hidden_native_exit(self):
        self.mutate_report({"native_process_exit": 0})
        self.assertEqual(old.audit(self.e), "SOURCE_ONLY_PASS_NATIVE_NOT_RUN")
        self.reject()

    def test_03_preflight_rejects_claimed_real_unity(self):
        self.mutate_report({"real_unity": "EXECUTED"})
        self.assertEqual(old.audit(self.e), "SOURCE_ONLY_PASS_NATIVE_NOT_RUN")
        self.reject()

    def test_04_synthetic_native_structurally_valid_but_not_attested(self):
        self.make_native()
        self.assertEqual(guard.audit(self.e),
                         "STRUCTURALLY_CONSISTENT_V2_ONLY_UNATTESTED_2_EDIT_14_PLAY")

    def test_05_missing_total_is_not_authoritative(self):
        self.make_native()
        self.edit_xml("EditMode", lambda x: x.attrib.pop("total"))
        self.assertEqual(old.audit(self.e),
                         "STRUCTURALLY_CONSISTENT_V2_ONLY_NOT_NATIVE_ATTESTATION")
        self.reject()

    def test_06_skipped_foreign_case_hidden_by_root_claim(self):
        self.make_native()
        def change(x):
            ET.SubElement(x, "test-case", {"fullname": "Foreign.Skipped", "result": "Skipped"})
            x.set("skipped", "1")
        self.edit_xml("PlayMode", change)
        self.assertEqual(old.audit(self.e),
                         "STRUCTURALLY_CONSISTENT_V2_ONLY_NOT_NATIVE_ATTESTATION")
        self.reject()

    def test_07_failed_foreign_case_hidden_by_root_claim(self):
        self.make_native()
        self.edit_xml("EditMode", lambda x: ET.SubElement(
            x, "test-case", {"fullname": "Foreign.Failed", "result": "Failed"}))
        self.reject()

    def test_08_inflated_root_counter(self):
        self.make_native()
        self.edit_xml("PlayMode", lambda x: (x.set("total", "15"),
                                               x.set("passed", "15")), sync_verdict=True)
        self.reject()

    def test_09_verdict_counter_disagrees_with_xml(self):
        self.make_native()
        p = "original-V2-native-only/native-verdict.json"
        d = json.loads(self.f.content[p])
        d["results"]["EditMode"]["passed"] = 3
        self.f.content[p] = json.dumps(d).encode()
        self.f.write()
        self.reject()

    def test_10_duplicate_foreign_fullname(self):
        self.make_native()
        def change(root):
            for _ in range(2):
                ET.SubElement(root, "test-case",
                              {"fullname": "Foreign.Duplicate", "result": "Passed"})
            root.set("total", "16")
            root.set("passed", "16")
        self.edit_xml("PlayMode", change, sync_verdict=True)
        self.reject()

    def test_11_extra_genuine_passing_case_is_allowed(self):
        self.make_native()
        def change(root):
            ET.SubElement(root, "test-case",
                          {"fullname": "Other.Unity.Test", "result": "Passed"})
            root.set("total", "3")
            root.set("passed", "3")
        self.edit_xml("EditMode", change, sync_verdict=True)
        self.assertEqual(guard.audit(self.e),
                         "STRUCTURALLY_CONSISTENT_V2_ONLY_UNATTESTED_3_EDIT_14_PLAY")

    def test_12_native_verdict_case_count_incorrect(self):
        self.make_native()
        p = "original-V2-native-only/native-verdict.json"
        d = json.loads(self.f.content[p])
        d["results"]["PlayMode"]["verified_independent_cases"] = 13
        self.f.content[p] = json.dumps(d).encode()
        self.f.write()
        self.reject()

    def test_13_raw_hash_tamper_rejected_by_original(self):
        self.make_native()
        (self.e / "original-V2-native-only/EditMode.xml").write_bytes(b"<broken />")
        self.reject()

    def test_14_source_signed_claim_never_promoted(self):
        self.mutate_report({"native_g1": "PASS_V2_ONLY_R4_STILL_NOT_RUN"})
        self.reject()


if __name__ == "__main__":
    unittest.main(verbosity=2)
