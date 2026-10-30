"""W18 original standard/polished batch code: negative fake-feedback gates."""
import copy
import io
import json
import tempfile
from pathlib import Path
from contextlib import redirect_stdout, redirect_stderr
import unittest
from integration_experiment import audit, main, BatchError

FIXTURE = Path(__file__).with_name("integrator_w18_batch.json")


def load():
    return json.loads(FIXTURE.read_text(encoding="utf-8"))


class W18Experiment(unittest.TestCase):
    def reject(self, mutate, why):
        data = load()
        mutate(data)
        with self.assertRaisesRegex(BatchError, why):
            audit(data)

    def test_two_real_source_groups(self):
        out = audit(load())
        self.assertEqual(out["matched_packages"], 2)
        self.assertEqual(out["presentation_arms"], 4)
        self.assertEqual(out["completed_integrator_reviews"], 0)

    def test_zero_verified_results_mean_nullable_yield(self):
        self.assertIsNone(audit(load())["acceptance_yield"])

    def test_zero_verified_results_mean_nullable_time(self):
        self.assertIsNone(audit(load())["median_integration_seconds"])

    def test_zero_verified_results_mean_nullable_regression(self):
        self.assertIsNone(audit(load())["regression_rate"])

    def test_every_package_has_exact_same_source_for_pair(self):
        out = audit(load())
        self.assertEqual({p["pr"] for p in out["packages"]}, {596,607})

    def test_all_stages_false_after_source_testing(self):
        out = audit(load())
        for pkg in out["packages"]:
            self.assertTrue(pkg["source_tested"])
            self.assertFalse(pkg["qa_accepted"])
            self.assertFalse(pkg["integrated"])
            self.assertFalse(pkg["released"])

    def test_no_inbox_offer_or_G2(self):
        out = audit(load())
        self.assertEqual(out["codex_offer"], "BLOCKED")
        self.assertEqual(out["G2"], "HOLD")

    def test_schema_migration(self):
        self.reject(lambda x: x.update(schema="v2"), "schema")

    def test_freeze_game_sha(self):
        self.reject(lambda x: x["freeze"].update(game_sha="a"*40), "frozen")

    def test_freeze_save_sha(self):
        self.reject(lambda x: x["freeze"].update(save_sha="a"*40), "frozen")

    def test_freeze_selected_host_sha(self):
        self.reject(lambda x: x["freeze"].update(host_sha="a"*40), "frozen")

    def test_alternative_604_cannot_become_selected(self):
        self.reject(lambda x: x["freeze"].update(host_pr=604), "frozen")

    def test_unfreeze(self):
        self.reject(lambda x: x["freeze"].update(immutable=False), "frozen")

    def test_fake_selected_g0(self):
        self.reject(lambda x: x["guards"].update(selected_host_g0="ACCEPTED"), "unjustified")

    def test_fake_unity_physx(self):
        self.reject(lambda x: x["guards"].update(unity_physx="PASS"), "unjustified")

    def test_fake_native_g1(self):
        self.reject(lambda x: x["guards"].update(selected_host_g1="PASS"), "unjustified")

    def test_fake_live_host(self):
        self.reject(lambda x: x["guards"].update(canonical_game_save_load="PASS"), "unjustified")

    def test_fake_security_629_reproduced(self):
        self.reject(lambda x: x["guards"].update(host_security_629="P0_CONFIRMED"), "unjustified")

    def test_fake_fixture_631_native_failure(self):
        self.reject(lambda x: x["guards"].update(fixture_631="UNITY_FAIL"), "unjustified")

    def test_fake_checkpoint_lock(self):
        self.reject(lambda x: x["guards"].update(checkpoint_lock="NATIVE_PASS"), "unjustified")

    def test_fake_codex_ack(self):
        self.reject(lambda x: x["guards"].update(codex_ack="YES"), "unjustified")

    def test_fake_g2(self):
        self.reject(lambda x: x["guards"].update(g2="PASS"), "unjustified")

    def test_fake_merge(self):
        self.reject(lambda x: x["guards"].update(merged=True), "unjustified")

    def test_fake_offer(self):
        self.reject(lambda x: x["guards"].update(codex_offer="OFFERED"), "unjustified")

    def test_nonpaired_assignment(self):
        self.reject(lambda x: x.update(assignment="BETWEEN_PACKAGE"), "nonpaired")

    def test_fake_delivery(self):
        self.reject(lambda x: x.update(delivery="SENT"), "self-assert")

    def test_fake_integrator_receipt(self):
        self.reject(lambda x: x.update(measured_feedback_source="https://example.com"), "separately")

    def test_missing_second_package(self):
        self.reject(lambda x: x["packages"].pop(), "two original")

    def test_duplicate_package(self):
        self.reject(lambda x: x["packages"].__setitem__(
            1,copy.deepcopy(x["packages"][0])), "duplicate")

    def test_original_sha_forged(self):
        self.reject(lambda x: x["packages"][0].update(sha="a"*40), "source")

    def test_original_pr_url_forged(self):
        self.reject(lambda x: x["packages"][0].update(pr_url="https://evil.invalid/pull/596"), "source")

    def test_untrusted_package_owner(self):
        self.reject(lambda x: x["packages"][1].update(owner="B01-00"), "source")

    def test_missing_actual_purpose(self):
        self.reject(lambda x: x["packages"][0].update(purpose="bad"), "purpose")

    def test_missing_original_actions(self):
        self.reject(lambda x: x["packages"][1].update(evidence=[]), "Actions")

    def test_wrong_original_actions_domain(self):
        self.reject(lambda x: x["packages"][1].update(
            evidence=["https://evil.invalid/attestation"]), "Actions")

    def test_duplicated_evidence_not_double_counted(self):
        self.reject(lambda x: x["packages"][0].update(
            evidence=x["packages"][0]["evidence"]+[x["packages"][0]["evidence"][0]]), "Actions")

    def test_missing_frozen_dependency(self):
        self.reject(lambda x: x["packages"][0].update(
            dependencies=["#510","#556"]), "dependency")

    def test_overwrite_shared_rollback(self):
        self.reject(lambda x: x["packages"][1].update(rollback="DELETE_GAME_ROOT"), "rollback")

    def test_blocker_suppression(self):
        self.reject(lambda x: x["packages"][0].update(known_blockers=[]), "blocker")

    def test_wrong_owner_receiver(self):
        self.reject(lambda x: x["packages"][0].update(receiver="Codex_ACK"), "receiver")

    def test_false_physical_QA_ACCEPTED(self):
        self.reject(lambda x: x["packages"][0]["stages"].update(QA_ACCEPTED=True), "six stages")

    def test_false_ready_for_integrator(self):
        self.reject(lambda x: x["packages"][0]["stages"].update(
            READY_FOR_INTEGRATOR=True), "six stages")

    def test_false_integrated(self):
        self.reject(lambda x: x["packages"][0]["stages"].update(INTEGRATED=True), "six stages")

    def test_false_released(self):
        self.reject(lambda x: x["packages"][0]["stages"].update(RELEASED=True), "six stages")

    def test_missing_stage(self):
        self.reject(lambda x: x["packages"][0]["stages"].pop("RELEASED"), "six stages")

    def test_source_ci_cannot_be_native(self):
        self.reject(lambda x: x["packages"][0].update(producer_source="UNITY_PASS"), "source test")

    def test_false_own_final_release(self):
        self.reject(lambda x: x["packages"][1].update(independent_qa="RELEASE_PASS"), "source test")

    def test_duplicate_arm_in_one_package(self):
        self.reject(lambda x: x["packages"][0]["arms"].__setitem__(
            1,copy.deepcopy(x["packages"][0]["arms"][0])), "duplicated arm")

    def test_drop_standard_arm(self):
        self.reject(lambda x: x["packages"][0]["arms"].pop(0), "two paired arms")

    def test_fake_polished_field(self):
        self.reject(lambda x: x["packages"][0]["arms"][1].update(
            presentation_fields=["pr","sha","native_green"]), "treatment")

    def test_false_integrator_accepted(self):
        self.reject(lambda x: x["packages"][0]["arms"][0]["integrator_feedback"].update(
            status="ACCEPTED"), "fabricated")

    def test_fake_elapsed_ms(self):
        self.reject(lambda x: x["packages"][0]["arms"][1]["integrator_feedback"].update(
            elapsed_seconds=10), "fabricated")

    def test_fake_integrator_retry(self):
        self.reject(lambda x: x["packages"][0]["arms"][1]["integrator_feedback"].update(
            retries=0), "fabricated")

    def test_fake_zero_regressions(self):
        self.reject(lambda x: x["packages"][1]["arms"][1]["integrator_feedback"].update(
            regressions=0), "fabricated")

    def test_fake_review_timestamp(self):
        self.reject(lambda x: x["packages"][0]["arms"][0]["integrator_feedback"].update(
            reviewed_at="2026-10-30T18:30:00Z"), "fabricated")

    def test_cli_real_report_no_feedback(self):
        buf=io.StringIO()
        with redirect_stdout(buf):
            self.assertEqual(main([str(FIXTURE)]),0)
        out=json.loads(buf.getvalue())
        self.assertEqual(out["state"], "NOT_MEASURED_NO_CLAIM_OF_INTEGRATION")
        self.assertEqual(len(out["input_sha256"]),64)

    def test_cli_require_actual_feedback_exit3(self):
        with redirect_stdout(io.StringIO()):
            self.assertEqual(main([str(FIXTURE),"--require-actual-feedback"]),3)

    def test_cli_refuse_overwrite_original(self):
        with redirect_stderr(io.StringIO()):
            self.assertEqual(main([str(FIXTURE),"--output",str(FIXTURE)]),2)

    def test_cli_refuse_overwrite_existing_foreign(self):
        with tempfile.TemporaryDirectory() as td:
            path=Path(td)/"foreign"
            path.write_text("DO NOT TOUCH")
            with redirect_stderr(io.StringIO()):
                self.assertEqual(main([str(FIXTURE),"--output",str(path)]),2)
            self.assertEqual(path.read_text(),"DO NOT TOUCH")

    def test_cli_write_once_and_refuse_reuse(self):
        with tempfile.TemporaryDirectory() as td:
            path=Path(td)/"own"
            with redirect_stderr(io.StringIO()):
                self.assertEqual(main([str(FIXTURE),"--output",str(path)]),0)
                self.assertEqual(main([str(FIXTURE),"--output",str(path)]),2)
            self.assertEqual(json.loads(path.read_text())["G1"],"NOT_RUN")

    def test_bad_utf8_json_fails_closed(self):
        with tempfile.TemporaryDirectory() as td:
            path=Path(td)/"invalid"
            path.write_bytes(b"{\xff")
            with redirect_stderr(io.StringIO()):
                self.assertEqual(main([str(path)]),2)


if __name__=="__main__":
    unittest.main()
