"""B01-00 W14 source-only candidate admission: original #550 RED outranks producer PASS.

Original Python stdlib tests, no fake native Unity or online reviewer identity.
"""
import copy
from contextlib import redirect_stdout, redirect_stderr
import io
import json
from pathlib import Path
import tempfile
import unittest

from save_admission import AdmissionError, audit, main

REAL = Path(__file__).with_name("save_contract_w14_hold.json")


def input_doc():
    return json.loads(REAL.read_text(encoding="utf-8"))


def chosen_544():
    d = input_doc()
    c = d["proposals"][0]
    c["independent_qa"] = {
        "state": "PASS_SOURCE_ONLY", "review_pr": 600,
        "review_sha": "a" * 40, "target_sha": c["source_sha"],
        "failure_codes": [],
    }
    d["owner_choice"] = {
        "authority": "B01-23", "state": "SELECTED_ONE", "pr": 544,
        "source_sha": c["source_sha"],
        "decision_url": "https://github.com/C8LyNn/RoboRougelike/issues/552#issuecomment-7000000001"
    }
    return d


class RealState(unittest.TestCase):
    def test_real_w14_is_hold(self):
        r = audit(input_doc())
        self.assertEqual(r["verdict"], "HOLD_NO_ACCEPTED_SINGLE_SAVE_CONTRACT")
        self.assertFalse(r["release_allowed"])
        self.assertIsNone(r["only_source_candidate_for_manual_review"])

    def test_producer_positive_549_cannot_override_independent_security_red(self):
        self.assertEqual(audit(input_doc())["candidate_statuses"]["549"],
                         "REJECT_EXACT_SOURCE_SECURITY_RED")

    def test_544_producer_pass_cannot_override_missing_independent_qa(self):
        self.assertEqual(audit(input_doc())["candidate_statuses"]["544"],
                         "HOLD_INDEPENDENT_SOURCE_QA")

    def test_original_red_source_blob_provenance_pinned(self):
        q = input_doc()["proposals"][1]
        self.assertNotEqual(q["source_sha"], q["independent_qa"]["target_sha"])
        self.assertEqual(q["source_blob_sha"], q["independent_qa"]["tested_blob_sha"])

    def test_no_source_contract_is_promoted_to_unity(self):
        r = audit(input_doc())
        self.assertEqual(r["native_unity_g1"], "NOT_RUN")
        self.assertEqual(r["host_save_load"], "HOLD")
        self.assertEqual(r["safe_receiver_migration"], "HOLD")
        self.assertEqual(r["g2"], "HOLD")

    def test_no_codex_ack_or_integration(self):
        r = audit(input_doc())
        self.assertEqual(r["codex_ack"], "UNCONFIRMED")
        self.assertFalse(r["integrated"])
        self.assertFalse(r["release_allowed"])

    def test_declared_exclusive_544_plus_qa_pass_only_manual_review(self):
        r = audit(chosen_544())
        self.assertEqual(r["only_source_candidate_for_manual_review"], 544)
        self.assertEqual(r["verdict"], "MANUAL_G0_REVIEW_REQUIRED")
        self.assertFalse(r["source_only_candidate_proof_authenticated"])
        self.assertFalse(r["release_allowed"])

    def test_selected_549_remains_rejected_even_with_owner_choice(self):
        d = chosen_544()
        d["owner_choice"]["pr"] = 549
        d["owner_choice"]["source_sha"] = d["proposals"][1]["source_sha"]
        r = audit(d)
        self.assertEqual(r["candidate_statuses"]["549"], "REJECT_EXACT_SOURCE_SECURITY_RED")
        self.assertIsNone(r["only_source_candidate_for_manual_review"])

    def test_red_from_old_commit_same_blob_is_still_an_active_blocker(self):
        d = input_doc()
        self.assertTrue(audit(d)["independent_security_red_present"])

    def test_new_549_version_different_source_blob_must_reaudit(self):
        d = input_doc()
        d["proposals"][1]["source_blob_sha"] = "f" * 40
        with self.assertRaisesRegex(AdmissionError, "older RED cannot transfer"):
            audit(d)

    def test_old_qa_pass_not_transfer_to_changed_sha(self):
        d = chosen_544()
        d["proposals"][0]["source_sha"] = "f" * 40
        d["owner_choice"]["source_sha"] = "f" * 40
        with self.assertRaisesRegex(AdmissionError, "older QA PASS"):
            audit(d)

    def test_wrong_gameplay_sha_rejected(self):
        d = input_doc()
        d["frozen_gameplay"]["sha"] = "a" * 40
        for c in d["proposals"]:
            c["target_game_sha"] = "a" * 40
        with self.assertRaisesRegex(AdmissionError, "wrong immutable"):
            audit(d)

    def test_wrong_gameplay_pr_rejected(self):
        d = input_doc()
        d["frozen_gameplay"].update(pr=511, url="https://github.com/C8LyNn/RoboRougelike/pull/511")
        with self.assertRaisesRegex(AdmissionError, "wrong immutable"):
            audit(d)

    def test_gameplay_not_frozen_rejected(self):
        d = input_doc()
        d["frozen_gameplay"]["frozen"] = False
        with self.assertRaises(AdmissionError):
            audit(d)

    def test_missing_frozen_gameplay_object_rejected(self):
        d = input_doc(); del d["frozen_gameplay"]
        with self.assertRaises(AdmissionError):
            audit(d)

    def test_candidate_duplicate_pr_rejected(self):
        d = input_doc()
        d["proposals"][1]["pr"] = 544
        with self.assertRaises(AdmissionError):
            audit(d)

    def test_extra_third_contract_rejected(self):
        d = input_doc()
        d["proposals"].append(copy.deepcopy(d["proposals"][0]))
        with self.assertRaises(AdmissionError):
            audit(d)

    def test_only_one_candidate_rejected(self):
        d = input_doc()
        d["proposals"].pop()
        with self.assertRaises(AdmissionError):
            audit(d)

    def test_unknown_candidate_renumber_rejected(self):
        d = input_doc()
        d["proposals"][0].update(pr=551, url="https://github.com/C8LyNn/RoboRougelike/pull/551")
        with self.assertRaises(AdmissionError):
            audit(d)

    def test_foreign_candidate_target_game_rejected(self):
        d = input_doc()
        d["proposals"][0]["target_game_sha"] = "a" * 40
        with self.assertRaises(AdmissionError):
            audit(d)

    def test_bad_proposal_url_rejected(self):
        d = input_doc()
        d["proposals"][1]["url"] = "https://evil.invalid/pull/549"
        with self.assertRaises(AdmissionError):
            audit(d)

    def test_unreviewed_544_cannot_select_itself(self):
        d = input_doc()
        c = d["proposals"][0]
        d["owner_choice"].update(state="SELECTED_ONE", pr=544, source_sha=c["source_sha"],
                                 decision_url="https://github.com/C8LyNn/RoboRougelike/issues/552#issuecomment-7001")
        r = audit(d)
        self.assertEqual(r["candidate_statuses"]["544"], "HOLD_INDEPENDENT_SOURCE_QA")

    def test_wrong_owner_selection_refused(self):
        d = chosen_544()
        d["owner_choice"]["authority"] = "B01-00"
        with self.assertRaises(AdmissionError):
            audit(d)

    def test_owner_selected_wrong_sha_refused(self):
        d = chosen_544()
        d["owner_choice"]["source_sha"] = "b" * 40
        with self.assertRaises(AdmissionError):
            audit(d)

    def test_owner_selected_no_decision_receipt_refused(self):
        d = chosen_544()
        d["owner_choice"]["decision_url"] = None
        with self.assertRaises(AdmissionError):
            audit(d)

    def test_owner_selected_unsafe_decision_url_refused(self):
        d = chosen_544()
        d["owner_choice"]["decision_url"] = "http://evil.invalid/vote"
        with self.assertRaises(AdmissionError):
            audit(d)

    def test_owner_undecided_with_forged_sha_refused(self):
        d = input_doc()
        d["owner_choice"]["source_sha"] = d["proposals"][0]["source_sha"]
        with self.assertRaises(AdmissionError):
            audit(d)

    def test_qa_not_run_cannot_claim_review_pr(self):
        d = input_doc()
        d["proposals"][0]["independent_qa"]["review_pr"] = 999
        with self.assertRaises(AdmissionError):
            audit(d)

    def test_source_qa_pass_on_wrong_pr_not_independent(self):
        d = chosen_544()
        d["proposals"][0]["independent_qa"]["review_pr"] = 544
        with self.assertRaises(AdmissionError):
            audit(d)

    def test_qa_red_missing_failure_codes_refused(self):
        d = input_doc()
        d["proposals"][1]["independent_qa"]["failure_codes"] = []
        with self.assertRaises(AdmissionError):
            audit(d)

    def test_qa_red_unknown_failure_code_refused(self):
        d = input_doc()
        d["proposals"][1]["independent_qa"]["failure_codes"] = ["MADE_UP"]
        with self.assertRaises(AdmissionError):
            audit(d)

    def test_qa_red_duplicate_codes_refused(self):
        d = input_doc()
        d["proposals"][1]["independent_qa"]["failure_codes"] *= 2
        with self.assertRaises(AdmissionError):
            audit(d)

    def test_qa_red_wrong_tested_blob_refused(self):
        d = input_doc()
        d["proposals"][1]["independent_qa"]["tested_blob_sha"] = "f" * 40
        with self.assertRaises(AdmissionError):
            audit(d)

    def test_qa_red_missing_tested_commit_refused(self):
        d = input_doc()
        d["proposals"][1]["independent_qa"]["target_sha"] = None
        with self.assertRaises(AdmissionError):
            audit(d)

    def test_qa_pass_with_security_red_codes_refused(self):
        d = chosen_544()
        d["proposals"][0]["independent_qa"]["failure_codes"] = ["W06_LEDGER_PUBLIC_MUTATOR"]
        with self.assertRaises(AdmissionError):
            audit(d)

    def test_older_security_red_cannot_be_pretended_as_pass(self):
        d = input_doc()
        d["proposals"][1]["independent_qa"]["state"] = "PASS_SOURCE_ONLY"
        with self.assertRaisesRegex(AdmissionError, "older QA PASS"):
            audit(d)

    def test_malicious_native_g1_pass_refused(self):
        d = input_doc()
        d["release_gates"]["native_g1"] = "PASS"
        with self.assertRaises(AdmissionError):
            audit(d)

    def test_save_load_prematurely_live_refused(self):
        d = input_doc()
        d["release_gates"]["canonical_save_load"] = "IMPLEMENTED"
        with self.assertRaises(AdmissionError):
            audit(d)

    def test_original_unsafe_rollback_promotion_refused(self):
        d = input_doc()
        d["release_gates"]["original_510_rollback"] = "ALLOWED"
        with self.assertRaises(AdmissionError):
            audit(d)

    def test_false_canonical_migration_refused(self):
        d = input_doc()
        d["release_gates"]["canonical_migration"] = "DONE"
        with self.assertRaises(AdmissionError):
            audit(d)

    def test_g2_premature_pass_refused(self):
        d = input_doc()
        d["release_gates"]["g2"] = "PASS"
        with self.assertRaises(AdmissionError):
            audit(d)

    def test_ack_forged_refused(self):
        d = input_doc()
        d["release_gates"]["codex_ack"] = "ACK"
        with self.assertRaises(AdmissionError):
            audit(d)

    def test_false_integrated_refused(self):
        d = input_doc()
        d["release_gates"]["integrated"] = True
        with self.assertRaises(AdmissionError):
            audit(d)

    def test_false_merge_refused(self):
        d = input_doc()
        d["release_gates"]["merge"] = True
        with self.assertRaises(AdmissionError):
            audit(d)

    def test_additional_release_state_fields_refused(self):
        d = input_doc()
        d["release_gates"]["force"] = True
        with self.assertRaises(AdmissionError):
            audit(d)

    def test_schema_downgrade_refused(self):
        d = input_doc()
        d["schema"] = "b01-w13-source-only-offer-v1"
        with self.assertRaises(AdmissionError):
            audit(d)

    def test_cli_real_hold_display_exit_zero(self):
        output = io.StringIO()
        with redirect_stdout(output):
            self.assertEqual(main([str(REAL)]), 0)
        self.assertEqual(json.loads(output.getvalue())["verdict"],
                         "HOLD_NO_ACCEPTED_SINGLE_SAVE_CONTRACT")

    def test_cli_strict_hold_exit_two(self):
        with redirect_stdout(io.StringIO()):
            self.assertEqual(main([str(REAL), "--require-single-source-candidate"]), 2)

    def test_cli_accepted_structure_still_not_integratable(self):
        d = chosen_544()
        with tempfile.TemporaryDirectory() as td:
            p = Path(td) / "accepted.json"
            p.write_text(json.dumps(d))
            output = io.StringIO()
            with redirect_stdout(output):
                self.assertEqual(main([str(p), "--require-single-source-candidate"]), 0)
            v = json.loads(output.getvalue())
            self.assertEqual(v["verdict"], "MANUAL_G0_REVIEW_REQUIRED")
            self.assertFalse(v["release_allowed"])

    def test_cli_reject_missing_file(self):
        with redirect_stderr(io.StringIO()):
            self.assertEqual(main([str(REAL) + ".notfound"]), 2)

    def test_cli_never_overwrite_source(self):
        with redirect_stderr(io.StringIO()):
            self.assertEqual(main([str(REAL), "--output", str(REAL)]), 2)

    def test_cli_never_overwrite_external_evidence(self):
        with tempfile.TemporaryDirectory() as td:
            p = Path(td) / "receipt.json"
            p.write_text("PREEXISTING")
            with redirect_stderr(io.StringIO()):
                self.assertEqual(main([str(REAL), "--output", str(p)]), 2)
            self.assertEqual(p.read_text(), "PREEXISTING")

    def test_cli_write_once_output_then_refuse_overwrite(self):
        with tempfile.TemporaryDirectory() as td:
            p = Path(td) / "receipt.json"
            with redirect_stderr(io.StringIO()):
                self.assertEqual(main([str(REAL), "--output", str(p)]), 0)
                self.assertEqual(main([str(REAL), "--output", str(p)]), 2)
            self.assertEqual(json.loads(p.read_text())["g2"], "HOLD")


if __name__ == "__main__":
    unittest.main()
