import copy
from contextlib import redirect_stderr, redirect_stdout
import io
import json
from pathlib import Path
import tempfile
import unittest

from receipt_latch import GateError, SCHEMA, audit, main, ref_url

SHA = "a" * 40
SHA_ALT = "b" * 40
REPO = "https://github.com/C8LyNn/RoboRougelike/"
COMMENT = REPO + "issues/79#issuecomment-6510326196"
QA_COMMENT = REPO + "pull/393#issuecomment-6510326197"
OFFER_COMMENT = REPO + "issues/79#issuecomment-6510326198"
ACK_COMMENT = REPO + "issues/79#issuecomment-6510326199"


def make_record(native=False):
    return {
        "schema": SCHEMA, "package_id": "B01-24-EXAMPLE-001",
        "version": "1.0.0", "needs_native": native,
        "producer": {
            "agent": "B01-24", "source_pr": 355, "sha": SHA, "url": REPO + "pull/355",
            "ci": {"sha": SHA, "status": "PASS", "passed": 30, "failed": 0,
                   "url": REPO + "actions/runs/41000000001",
                   "contract": {"workflow_id": 401553591,
                                "workflow_path": ".github/workflows/b01-00-dispatch-w06.yml",
                                "event": "pull_request",
                                "required_job_names": ["dispatch (windows-2025)", "dispatch (ubuntu-24.04)"],
                                "required_steps": ["Original source full dispatch and freeze regression suite",
                                                   "Produce deterministic example plan"]}},
        },
        "qa": {
            "intake": {"agent": "B01-40", "status": "PASS", "sha": SHA, "url": COMMENT},
            "independent": {"agent": "B01-60", "status": "PASS", "sha": SHA, "url": QA_COMMENT},
        },
        "native": {
            "edit": {"sha": SHA, "status": "NOT_RUN", "passed": 0, "failed": 0, "url": None},
            "play": {"sha": SHA, "status": "NOT_RUN", "passed": 0, "failed": 0, "url": None},
        },
        "offer": {"status": "NOT_SENT", "sha": None, "url": None},
        "ack": {"status": "NO", "sha": None, "url": None, "actor": None},
        "integration": {"status": "NO", "sha": None, "url": None, "commit_sha": None},
    }


class SourceGates(unittest.TestCase):
    def test_valid_offline_source_declared_ready(self):
        d = audit(make_record())
        self.assertTrue(d["source_gates_consistent"])
        self.assertEqual(d["recommendation"], "ELIGIBLE_FOR_EXTERNAL_SOURCE_REVIEW")

    def test_never_equate_source_with_sent(self):
        d = audit(make_record())
        self.assertFalse(d["offer_record_consistent"])
        self.assertFalse(d["ack_record_consistent"])
        self.assertFalse(d["integration_record_consistent"])

    def test_no_false_authenticated_claim(self):
        self.assertEqual(audit(make_record())["evidence_authentication"],
                         "NOT_PERFORMED_OFFLINE")

    def test_producer_ci_sha_drift(self):
        x = make_record(); x["producer"]["ci"]["sha"] = SHA_ALT
        self.assertIn("producer_ci_sha_mismatch", audit(x)["blockers"])

    def test_ci_failure(self):
        x = make_record(); x["producer"]["ci"]["status"] = "FAIL"
        self.assertFalse(audit(x)["source_gates_consistent"])

    def test_ci_not_run(self):
        x = make_record(); x["producer"]["ci"]["status"] = "NOT_RUN"
        self.assertFalse(audit(x)["source_gates_consistent"])

    def test_ci_zero_executed(self):
        x = make_record(); x["producer"]["ci"]["passed"] = 0
        self.assertIn("producer_ci_no_executed_tests", audit(x)["blockers"])

    def test_ci_failure_count(self):
        x = make_record(); x["producer"]["ci"]["failed"] = 2
        self.assertFalse(audit(x)["source_gates_consistent"])

    def test_ci_bool_not_integer(self):
        x = make_record(); x["producer"]["ci"]["passed"] = True
        self.assertFalse(audit(x)["source_gates_consistent"])

    def test_ci_missing(self):
        x = make_record(); del x["producer"]["ci"]
        self.assertIn("producer_ci_missing", audit(x)["blockers"])

    def test_ci_run_link_wrong_repository(self):
        x = make_record(); x["producer"]["ci"]["url"] = "https://github.com/Else/Repo/actions/runs/7"
        self.assertIn("producer_ci_no_run_url", audit(x)["blockers"])

    def test_bad_author_url(self):
        x = make_record(); x["producer"]["url"] = REPO + "pull/356"
        with self.assertRaises(GateError): audit(x)

    def test_invalid_source_hash(self):
        x = make_record(); x["producer"]["sha"] = "abc"
        with self.assertRaises(GateError): audit(x)

    def test_reject_producer_qa_role(self):
        x = make_record(); x["producer"]["agent"] = "B01-60"
        with self.assertRaises(GateError): audit(x)

    def test_independent_qa_missing(self):
        x = make_record(); x["qa"].pop("independent")
        self.assertFalse(audit(x)["source_gates_consistent"])

    def test_qa_role_spoof(self):
        x = make_record(); x["qa"]["intake"]["agent"] = "B01-24"
        self.assertIn("intake_not_independent", audit(x)["blockers"])

    def test_qa_sha_drift(self):
        x = make_record(); x["qa"]["independent"]["sha"] = SHA_ALT
        self.assertIn("independent_sha_mismatch", audit(x)["blockers"])

    def test_qa_not_run(self):
        x = make_record(); x["qa"]["independent"]["status"] = "NOT_RUN"
        self.assertFalse(audit(x)["source_gates_consistent"])

    def test_qa_comment_link_malformed(self):
        x = make_record(); x["qa"]["intake"]["url"] = "https://example.org/fake"
        self.assertIn("intake_no_comment_url", audit(x)["blockers"])

    def test_native_required_not_run(self):
        x = make_record(True)
        d = audit(x)
        self.assertFalse(d["source_gates_consistent"])
        self.assertIn("native_edit_not_pass", d["blockers"])
        self.assertIn("native_play_not_pass", d["blockers"])

    def test_native_required_one_side_missing(self):
        x = make_record(True)
        x["native"]["edit"] = copy.deepcopy(x["producer"]["ci"])
        self.assertIn("native_play_not_pass", audit(x)["blockers"])

    def test_native_both_original_source_pass(self):
        x = make_record(True)
        x["native"]["edit"] = copy.deepcopy(x["producer"]["ci"])
        x["native"]["play"] = copy.deepcopy(x["producer"]["ci"])
        self.assertTrue(audit(x)["source_gates_consistent"])

    def test_native_current_sha_required(self):
        x = make_record(True)
        for t in ("edit", "play"): x["native"][t] = copy.deepcopy(x["producer"]["ci"])
        x["native"]["play"]["sha"] = SHA_ALT
        self.assertFalse(audit(x)["source_gates_consistent"])

    def test_missing_explicit_native_flag(self):
        x = make_record(); del x["needs_native"]
        with self.assertRaises(GateError): audit(x)


class HandoffSequence(unittest.TestCase):
    def test_offer_source_ready_and_exact_sha(self):
        x = make_record()
        x["offer"] = {"status": "OFFERED", "sha": SHA, "url": OFFER_COMMENT}
        d = audit(x)
        self.assertTrue(d["offer_record_consistent"])
        self.assertFalse(d["ack_record_consistent"])

    def test_offer_no_qa_rejected(self):
        x = make_record(); x["qa"]["independent"]["status"] = "PENDING"
        x["offer"] = {"status": "OFFERED", "sha": SHA, "url": OFFER_COMMENT}
        self.assertIn("offer_claim_before_gates", audit(x)["blockers"])

    def test_offer_wrong_sha_rejected(self):
        x = make_record()
        x["offer"] = {"status": "OFFERED", "sha": SHA_ALT, "url": OFFER_COMMENT}
        self.assertIn("offer_receipt_invalid", audit(x)["blockers"])

    def test_ack_without_offer_rejected(self):
        x = make_record()
        x["ack"] = {"status": "YES", "actor": "CODEX", "sha": SHA, "url": ACK_COMMENT}
        self.assertIn("codex_ack_unverified_or_out_of_order", audit(x)["blockers"])

    def test_ack_from_non_codex_rejected(self):
        x = make_record()
        x["offer"] = {"status": "OFFERED", "sha": SHA, "url": OFFER_COMMENT}
        x["ack"] = {"status": "YES", "actor": "B01-00", "sha": SHA, "url": ACK_COMMENT}
        self.assertFalse(audit(x)["ack_record_consistent"])

    def test_codex_ack_consistent_after_offer(self):
        x = make_record()
        x["offer"] = {"status": "OFFERED", "sha": SHA, "url": OFFER_COMMENT}
        x["ack"] = {"status": "YES", "actor": "CODEX", "sha": SHA, "url": ACK_COMMENT}
        self.assertTrue(audit(x)["ack_record_consistent"])

    def test_integration_without_ack_rejected(self):
        x = make_record()
        x["integration"] = {"status": "YES", "sha": SHA, "commit_sha": SHA_ALT,
                            "url": REPO + "pull/450"}
        self.assertIn("integration_unverified_or_out_of_order", audit(x)["blockers"])

    def test_integration_with_invalid_commit(self):
        x = make_record()
        x["offer"] = {"status": "OFFERED", "sha": SHA, "url": OFFER_COMMENT}
        x["ack"] = {"status": "YES", "actor": "CODEX", "sha": SHA, "url": ACK_COMMENT}
        x["integration"] = {"status": "YES", "sha": SHA, "commit_sha": "abc",
                            "url": REPO + "pull/450"}
        self.assertFalse(audit(x)["integration_record_consistent"])

    def test_integration_claimed_after_ordered_receipts(self):
        x = make_record()
        x["offer"] = {"status": "OFFERED", "sha": SHA, "url": OFFER_COMMENT}
        x["ack"] = {"status": "YES", "actor": "CODEX", "sha": SHA, "url": ACK_COMMENT}
        x["integration"] = {"status": "YES", "sha": SHA, "commit_sha": SHA_ALT,
                            "url": REPO + "pull/450"}
        self.assertTrue(audit(x)["integration_record_consistent"])

    def test_actual_github_comment_anchor_supported(self):
        self.assertTrue(ref_url(COMMENT, "comment"))

    def test_reject_url_query_or_host_trick(self):
        self.assertFalse(ref_url(REPO + "issues/79#issuecomment-123?redirect=1", "comment"))
        self.assertFalse(ref_url("https://github.com.evil.invalid/C8LyNn/RoboRougelike/issues/79#issuecomment-123", "comment"))


class CliOperations(unittest.TestCase):
    def test_cli_holds_unverified_snapshot(self):
        x = make_record(); x["producer"]["ci"]["status"] = "NOT_RUN"
        with tempfile.TemporaryDirectory() as td:
            path = Path(td) / "input.json"; path.write_text(json.dumps(x))
            with redirect_stdout(io.StringIO()):
                self.assertEqual(main([str(path), "--require-source-ready"]), 2)

    def test_cli_stdout_json_contains_hash(self):
        with tempfile.TemporaryDirectory() as td:
            path = Path(td) / "input.json"; path.write_text(json.dumps(make_record()))
            output = io.StringIO()
            with redirect_stdout(output):
                self.assertEqual(main([str(path)]), 0)
            d = json.loads(output.getvalue())
            self.assertEqual(len(d["input_sha256"]), 64)

    def test_cli_immutable_output_no_overwrite(self):
        with tempfile.TemporaryDirectory() as td:
            path = Path(td) / "input.json"; path.write_text(json.dumps(make_record()))
            result = Path(td) / "report.json"
            with redirect_stdout(io.StringIO()), redirect_stderr(io.StringIO()):
                self.assertEqual(main([str(path), "--output", str(result)]), 0)
                self.assertEqual(main([str(path), "--output", str(result)]), 2)
            self.assertTrue(result.is_file())

    def test_cli_input_cannot_be_output(self):
        with tempfile.TemporaryDirectory() as td:
            path = Path(td) / "input.json"; path.write_text(json.dumps(make_record()))
            with redirect_stderr(io.StringIO()):
                self.assertEqual(main([str(path), "--output", str(path)]), 2)

    def test_cli_invalid_json_fails(self):
        with tempfile.TemporaryDirectory() as td:
            path = Path(td) / "input.json"; path.write_text("{bad")
            with redirect_stderr(io.StringIO()):
                self.assertEqual(main([str(path)]), 2)


if __name__ == "__main__":
    unittest.main()
