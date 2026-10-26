"""Synthetic negative and positive cases: no Unity execution implied."""
import json
import tempfile
import unittest
from pathlib import Path
from xml.sax.saxutils import escape
import rc2_edit_retest_gate as gate

A = "a" * 40
B = "b" * 40
F = (
    "R1PlayIntegrationTests.KeyboardEnterStartsRunWithoutPrematureDefeat",
    "RRLW34ReviewRegressionTests.FullInventoryHasExplicitRecoveryForMandatoryRewardsIncludingFinalVictory",
)


def xml(names, failures=()):
    body = "".join('<test-case name="{}" fullname="{}" result="{}"/>'.format(
        escape(n.split(".")[-1]), escape(n), "Failed" if n in failures else "Passed") for n in names)
    n = len(names); f = len(failures)
    return f'<test-run testcasecount="{n}" total="{n}" passed="{n-f}" failed="{f}" skipped="0" result="{("Passed" if not f else "Failed(Child)")}">{body}</test-run>'


class GateTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.root = Path(self.tmp.name)
        self.names = F + tuple(f"OtherTests.T{i}" for i in range(1066))
        self.files = [self.root / x for x in ("full.xml", "full.json", "retest.xml", "retest.json", "eq.json")]
        self.full = {"phase": "EditMode", "source": A, "exitCode": 2, "arguments": ["-runTests", "-testPlatform", "EditMode"]}
        self.retest = {"phase": "EditMode", "source": B, "exitCode": 0, "arguments": ["-runTests", "-testFilter", ";".join(F)]}
        self.eq = {"productionBuildSHA": A, "editorTestRevision": B, "productionChangesExcludingEditorTests": [],
                   "changedUnityFiles": sorted(gate.ALLOWED_EDITOR_FILES), "assemblySHA256": "f" * 64,
                   "buildAndExtractedAssemblyIdentical": True,
                   "status": "PRODUCTION_UNCHANGED; ONLY_TWO_EDITOR_FIXTURES_CORRECTED"}
        self.refresh()

    def tearDown(self):
        self.tmp.cleanup()

    def refresh(self):
        self.files[0].write_text(xml(self.names, F))
        self.files[1].write_text(json.dumps(self.full))
        self.files[2].write_text(xml(F))
        self.files[3].write_text(json.dumps(self.retest))
        self.files[4].write_text(json.dumps(self.eq))

    def run_gate(self):
        return gate.evaluate(*self.files)

    def assert_gate_error(self, expected):
        with self.assertRaisesRegex(ValueError, expected): self.run_gate()

    def test_positive_split_sha_receipt_only_not_full_pass(self):
        r = self.run_gate()
        self.assertEqual(r["evidence_verdict"], "PASS_SPLIT_SHA_RECEIPT_CONSISTENCY")
        self.assertEqual(r["full_editmode_final_sha"], "NOT_RUN")
        self.assertEqual(r["independent_unity_execution"], "NOT_RUN")

    def test_original_exit_nonzero_required(self):
        self.full["exitCode"] = 0; self.refresh(); self.assert_gate_error("NATIVE_EXIT_MISMATCH")

    def test_target_retest_exit_zero_required(self):
        self.retest["exitCode"] = 2; self.refresh(); self.assert_gate_error("NATIVE_EXIT_MISMATCH")

    def test_source_sha_change_required(self):
        self.retest["source"] = A; self.refresh(); self.assert_gate_error("EXPECTED_DISTINCT_TEST_REVISION")

    def test_source_sha_equivalence_required(self):
        self.eq["editorTestRevision"] = "c"*40; self.refresh(); self.assert_gate_error("EQUIVALENCE_SHA_MISMATCH")

    def test_illegal_runtime_diff_rejected(self):
        self.eq["productionChangesExcludingEditorTests"] = ["Assets/R3/Actual.cs"]; self.refresh(); self.assert_gate_error("PRODUCTION_CHANGED")

    def test_illegal_editor_test_diff_rejected(self):
        self.eq["changedUnityFiles"].append("Assets/Tests/Editor/Other.cs"); self.refresh(); self.assert_gate_error("EDITOR_DIFF")

    def test_retest_another_method_rejected(self):
        self.files[2].write_text(xml((F[0], "Wrong.Method")))
        self.assert_gate_error("RETEST_CASES")

    def test_retest_duplicated_cases_rejected(self):
        self.files[2].write_text(xml((F[0], F[0])))
        self.assert_gate_error("DUPLICATE")

    def test_retest_filter_incomplete_rejected(self):
        self.retest["arguments"] = ["-testFilter", F[0]]; self.refresh(); self.assert_gate_error("TARGETED_FILTER_CASE")

    def test_full_run_filter_rejected(self):
        self.full["arguments"].extend(["-testFilter", "X"]); self.refresh(); self.assert_gate_error("ORIGINAL_SUITE_FILTERED")

    def test_original_missing_case_rejected(self):
        self.files[0].write_text(xml(self.names[:-1], F))
        self.assert_gate_error("UNEXPECTED_ORIGINAL_RC2_COUNTS")

    def test_forged_xml_counts_rejected(self):
        self.files[2].write_text(xml(F).replace('total="2"', 'total="1068"'))
        self.assert_gate_error("NUNIT_COUNT_MISMATCH")

    def test_invalid_assembly_digest_rejected(self):
        self.eq["assemblySHA256"] = "0"*20; self.refresh(); self.assert_gate_error("ASSEMBLY_DIGEST_INVALID")

    def test_missing_binary_identity_rejected(self):
        self.eq["buildAndExtractedAssemblyIdentical"] = False; self.refresh(); self.assert_gate_error("ASSEMBLY_IDENTITY_UNPROVEN")

    def test_missing_xml_rejected(self):
        self.files[0].unlink()
        with self.assertRaises(OSError):self.run_gate()

if __name__ == "__main__": unittest.main()
