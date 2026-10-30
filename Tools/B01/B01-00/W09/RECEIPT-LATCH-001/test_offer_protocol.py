"""W13 original independent producer negative and positive source-only receipt cases.

Tested real pinned #510/#521/#517/#79 values; no network in unit tests.
The separate original workflow runs actual GitHub source CI in parallel.
"""
import copy
from contextlib import redirect_stderr, redirect_stdout
import io
import json
from pathlib import Path
import tempfile
import unittest

from offer_protocol import OfferError, check_offer, main

EXAMPLE = Path(__file__).with_name("offer_secure_v2_source_only.json")


def fixture():
    return json.loads(EXAMPLE.read_text(encoding="utf-8"))


class W13OfferStage(unittest.TestCase):
    def deny(self, change, expected):
        doc = fixture()
        change(doc)
        result = check_offer(doc)
        self.assertFalse(result["source_only_offer_record_consistent"], result)
        self.assertIn(expected, result["blockers"])
        self.assertEqual(result["verdict"], "HOLD")
        self.assertFalse(result["integrated"])
        self.assertFalse(result["merge_authorized"])

    def test_exact_real_source_offer_is_only_structural(self):
        x = check_offer(fixture())
        self.assertTrue(x["source_only_offer_record_consistent"])
        self.assertEqual(x["verdict"], "SOURCE_ONLY_RECORD_FOR_MANUAL_VERIFICATION")

    def test_success_never_means_g2(self):
        x = check_offer(fixture())
        self.assertEqual(x["g2"], "HOLD")
        self.assertFalse(x["integrated"])
        self.assertFalse(x["merge_authorized"])

    def test_success_never_means_codex_ack(self):
        x = check_offer(fixture())
        self.assertEqual(x["codex_ack"], "UNCONFIRMED")
        self.assertFalse(x["code_offer_authenticated_online"])

    def test_success_never_authenticates_independent_qa(self):
        self.assertFalse(check_offer(fixture())["independent_g0_authenticated_online"])

    def test_exact_sha_missing(self):
        x = fixture(); x["source"]["sha"] = "invalid"
        with self.assertRaises(OfferError): check_offer(x)

    def test_source_sha_changed_from_frozen(self):
        sha = "a" * 40
        self.deny(lambda x: x["source"].update(sha=sha), "g0_source_sha_mismatch")

    def test_source_pr_link_mismatch(self):
        self.deny(lambda x: x["source"].update(url="https://evil.invalid/pull/510"),
                  "source_url_mismatch")

    def test_source_not_frozen(self):
        self.deny(lambda x: x["source"].update(frozen=False), "producer_not_frozen")

    def test_source_zero_pr_rejected(self):
        x = fixture(); x["source"]["pr"] = 0
        with self.assertRaises(OfferError): check_offer(x)

    def test_zip_sha_corrupt(self):
        self.deny(lambda x: x["physical_archive"].update(sha256="f" * 64),
                  "g0_archive_mismatch")

    def test_zip_sha_invalid_format(self):
        self.deny(lambda x: x["physical_archive"].update(sha256="not-a-sha"),
                  "archive_hash_invalid")

    def test_zip_zero_members(self):
        self.deny(lambda x: x["physical_archive"].update(members=0),
                  "archive_size_or_members_invalid")

    def test_zip_wrong_guid_count(self):
        self.deny(lambda x: x["physical_archive"].update(guid_count=100),
                  "archive_structure_invalid")

    def test_g0_from_producer_not_independent(self):
        self.deny(lambda x: x["g0"].update(agent="B01-23"),
                  "independent_physical_g0_not_accepted")

    def test_g0_claim_without_acceptance(self):
        self.deny(lambda x: x["g0"].update(status="PENDING"),
                  "independent_physical_g0_not_accepted")

    def test_g0_on_wrong_selected_sha(self):
        self.deny(lambda x: x["g0"].update(source_sha="a" * 40),
                  "g0_source_sha_mismatch")

    def test_g0_wrong_zip(self):
        self.deny(lambda x: x["g0"].update(archive_sha256="a" * 64),
                  "g0_archive_mismatch")

    def test_g0_bad_pr(self):
        self.deny(lambda x: x["g0"].update(pr=510),
                  "g0_pr_invalid")

    def test_g0_qa_commit_missing(self):
        self.deny(lambda x: x["g0"].update(qa_sha=""),
                  "g0_exact_qa_commit_missing")

    def test_g0_bad_pr_url(self):
        self.deny(lambda x: x["g0"].update(url="https://github.com/C8LyNn/RoboRougelike/pull/99"),
                  "g0_pr_invalid")

    def test_source_qa_from_producer_not_independent(self):
        self.deny(lambda x: x["source_qa"].update(agent="B01-23"),
                  "independent_transaction_source_qa_missing")

    def test_source_qa_not_pass(self):
        self.deny(lambda x: x["source_qa"].update(status="NOT_RUN"),
                  "independent_transaction_source_qa_missing")

    def test_source_qa_reusing_g0_pr(self):
        self.deny(lambda x: x["source_qa"].update(pr=521),
                  "source_qa_pr_invalid")

    def test_source_qa_uses_alternative_sha(self):
        self.deny(lambda x: x["source_qa"].update(source_sha="b" * 40),
                  "source_qa_sha_mismatch")

    def test_source_qa_qa_commit_not_pinned(self):
        self.deny(lambda x: x["source_qa"].update(qa_sha=None),
                  "source_qa_exact_commit_missing")

    def test_offer_on_another_issue(self):
        self.deny(lambda x: x["offer"].update(url="https://github.com/C8LyNn/RoboRougelike/issues/535#issuecomment-6526629723"),
                  "offer_not_in_canonical_codex_inbox")

    def test_offer_mismatched_comment_id(self):
        self.deny(lambda x: x["offer"].update(comment_id=1),
                  "offer_not_in_canonical_codex_inbox")

    def test_offer_wrong_source(self):
        self.deny(lambda x: x["offer"].update(source_sha="a" * 40),
                  "offer_wrong_source_or_package")

    def test_offer_wrong_package(self):
        self.deny(lambda x: x["offer"].update(package_id="B01-23-OTHER"),
                  "offer_wrong_source_or_package")

    def test_offer_no_status(self):
        self.deny(lambda x: x["offer"].update(status="NOT_SENT"),
                  "offer_must_be_source_only")

    def test_source_511_cannot_be_selected(self):
        self.deny(lambda x: x["alternatives_hold"].append(510),
                  "alternatives_not_explicitly_excluded")

    def test_duplicate_exclusion_rejected(self):
        self.deny(lambda x: x["alternatives_hold"].append(511),
                  "alternatives_not_explicitly_excluded")

    def test_no_explicit_alternatives_hold(self):
        self.deny(lambda x: x.update(alternatives_hold=[]),
                  "alternatives_not_explicitly_excluded")

    def test_native_g1_premature_pass(self):
        self.deny(lambda x: x["release_constraints"].update(native_g1="PASS"),
                  "unsafe_or_missing_state_native_g1")

    def test_save_load_unverified_promotion(self):
        self.deny(lambda x: x["release_constraints"].update(save_load="ACCEPTED"),
                  "unsafe_or_missing_state_save_load")

    def test_rollback_unverified_promotion(self):
        self.deny(lambda x: x["release_constraints"].update(rollback_receiver="PASS"),
                  "unsafe_or_missing_state_rollback_receiver")

    def test_canonical_migration_unverified_promotion(self):
        self.deny(lambda x: x["release_constraints"].update(canonical_migration="DONE"),
                  "unsafe_or_missing_state_canonical_migration")

    def test_g2_premature_pass(self):
        self.deny(lambda x: x["release_constraints"].update(g2="PASS"),
                  "unsafe_or_missing_state_g2")

    def test_codex_ack_forged(self):
        self.deny(lambda x: x["release_constraints"].update(codex_ack="YES"),
                  "unsafe_or_missing_state_codex_ack")

    def test_integrated_forged(self):
        self.deny(lambda x: x["release_constraints"].update(integrated="YES"),
                  "unsafe_or_missing_state_integrated")

    def test_merge_forged(self):
        self.deny(lambda x: x["release_constraints"].update(merge="YES"),
                  "unsafe_or_missing_state_merge")

    def test_unrecognized_state_field_fail_closed(self):
        self.deny(lambda x: x["release_constraints"].update(exempt_native=True),
                  "unknown_release_state_fields")

    def test_missing_release_state_fail_closed(self):
        self.deny(lambda x: x["release_constraints"].pop("save_load"),
                  "unsafe_or_missing_state_save_load")

    def test_unsupported_schema_does_not_migrate_implicitly(self):
        x = fixture(); x["schema"] = "b01-source-only-offer-v2"
        with self.assertRaises(OfferError): check_offer(x)

    def test_cli_exact_existing_offer_exit_zero_with_manual_review(self):
        output = io.StringIO()
        with redirect_stdout(output):
            self.assertEqual(main([str(EXAMPLE), "--require-structural-offer"]), 0)
        data = json.loads(output.getvalue())
        self.assertTrue(data["source_only_offer_record_consistent"])
        self.assertEqual(data["g2"], "HOLD")
        self.assertEqual(len(data["input_sha256"]), 64)

    def test_cli_malicious_promotion_fails_exit_two(self):
        x = fixture(); x["release_constraints"]["g2"] = "PASS"
        with tempfile.TemporaryDirectory() as td:
            path = Path(td) / "bad.json"; path.write_text(json.dumps(x))
            with redirect_stdout(io.StringIO()):
                self.assertEqual(main([str(path), "--require-structural-offer"]), 2)

    def test_cli_must_never_overwrite_input(self):
        with redirect_stderr(io.StringIO()):
            self.assertEqual(main([str(EXAMPLE), "--output", str(EXAMPLE)]), 2)

    def test_cli_must_never_overwrite_existing_output(self):
        with tempfile.TemporaryDirectory() as td:
            out = Path(td) / "out.json"; out.write_text("KEEP")
            with redirect_stderr(io.StringIO()):
                self.assertEqual(main([str(EXAMPLE), "--output", str(out)]), 2)
            self.assertEqual(out.read_text(), "KEEP")

    def test_cli_write_once_then_reject_second_write(self):
        with tempfile.TemporaryDirectory() as td:
            out = Path(td) / "out.json"
            with redirect_stdout(io.StringIO()), redirect_stderr(io.StringIO()):
                self.assertEqual(main([str(EXAMPLE), "--output", str(out)]), 0)
                self.assertEqual(main([str(EXAMPLE), "--output", str(out)]), 2)
            self.assertEqual(json.loads(out.read_text())["g2"], "HOLD")


if __name__ == "__main__":
    unittest.main()
