"""W19 source-only release gate tests; never fake R3 owner, G0, G1 or Codex."""
import copy
from contextlib import redirect_stderr, redirect_stdout
import io
import json
from pathlib import Path
import tempfile
import unittest
from r3_decision_gate import audit, main, Deny

FIXTURE=Path(__file__).with_name("r3_w19_unselected.json")

def get():
    return json.loads(FIXTURE.read_text(encoding="utf-8"))

class R3ExclusiveSelection(unittest.TestCase):
    def rejects(self, operation):
        obj=get()
        operation(obj)
        with self.assertRaises(Deny):
            audit(obj)

    def test_initial_real_source_hold(self):
        x=audit(get())
        self.assertEqual(x["status"],
                         "HOLD_OWNER_R3_EXCLUSIVE_SELECTION_AND_INDEPENDENT_QA")
        self.assertIsNone(x["selected_sha"])

    def test_both_original_producer_candidates_present(self):
        self.assertEqual(audit(get())["competing_prs"],[642,644])

    def test_correct_original_source_does_not_allow_offer(self):
        self.assertFalse(audit(get())["ready_for_codex"])

    def test_no_invented_codex_time(self):
        self.assertIsNone(audit(get())["metrics"]["elapsed_seconds"])

    def test_not_even_zero_retries(self):
        self.assertIsNone(audit(get())["metrics"]["attempts"])

    def test_native_unity_not_run(self):
        self.assertEqual(audit(get())["native_unity_g1"],"NOT_RUN")

    def test_original_game_sha_stable(self):
        self.assertEqual(audit(get())["immutable_game_sha"],
                         "d94400d95d13db7f27e1533df05c9292d5380431")

    def test_alternative_604_cannot_stage(self):
        self.assertEqual(audit(get())["alt604"],"ALT_HOLD")

    def test_producer_130_tests_cannot_choose_644_automatically(self):
        self.assertTrue(audit(get())["never_auto_pick_more_tests"])
        self.assertIsNone(audit(get())["selected_pr"])

    def test_wrong_schema(self):
        self.rejects(lambda x:x.update(schema="b01-w18"))

    def test_frozen_game_drift(self):
        self.rejects(lambda x:x["frozen"]["game"].update(sha="a"*40))

    def test_frozen_save_drift(self):
        self.rejects(lambda x:x["frozen"]["save"].update(sha="a"*40))

    def test_frozen_r2_drift(self):
        self.rejects(lambda x:x["frozen"]["r2"].update(sha="a"*40))

    def test_unfreeze_alternative_604(self):
        self.rejects(lambda x:x["frozen"]["alt"].update(status="SELECTED"))

    def test_select_642_without_real_owner_evidence(self):
        self.rejects(lambda x:x["owner_choice"].update(status="SELECTED",
            winner_pr=642,loser_pr=644))

    def test_select_644_without_real_owner_evidence(self):
        self.rejects(lambda x:x["owner_choice"].update(status="SELECTED",
            winner_pr=644,loser_pr=642))

    def test_fake_owner_comment(self):
        self.rejects(lambda x:x["owner_choice"].update(
            decision_comment_url="https://github.com/C8LyNn/RoboRougelike/pull/644#issuecomment-123"))

    def test_fake_b01_00_owner(self):
        self.rejects(lambda x:x["owner_choice"].update(authority="B01-00"))

    def test_both_r3_installed(self):
        self.rejects(lambda x:x.update(competing_sources_installed=True))

    def test_fake_codex_offer(self):
        self.rejects(lambda x:x.update(code_offer_to_codex=True))

    def test_fake_external_ack(self):
        self.rejects(lambda x:x["gates"].update(codex_ack="ACK"))

    def test_fake_native_unity(self):
        self.rejects(lambda x:x["gates"].update(native_unity_physx="PASS"))

    def test_fake_r3_source_g0(self):
        self.rejects(lambda x:x["gates"].update(r3_physical_g0="ACCEPTED"))

    def test_fake_independent_source_qa(self):
        self.rejects(lambda x:x["gates"].update(
            r3_independent_security_qa="PASS"))

    def test_fake_live_game_save(self):
        self.rejects(lambda x:x["gates"].update(
            canonical_game_save_load="PASS"))

    def test_fake_g2(self):
        self.rejects(lambda x:x["gates"].update(g2="PASS"))

    def test_fake_merge(self):
        self.rejects(lambda x:x["gates"].update(merged=True))

    def test_fake_integration(self):
        self.rejects(lambda x:x["gates"].update(integrated=True))

    def test_fake_629_r3_retest(self):
        self.rejects(lambda x:x["security"].update(
            **{"629_stale_public_host":"RELEASE_GREEN"}))

    def test_fake_637_retest(self):
        self.rejects(lambda x:x["security"].update(
            **{"637_durable_commit_then_throw":"RELEASE_GREEN"}))

    def test_fake_631_native_test(self):
        self.rejects(lambda x:x["security"].update(
            **{"631_native_checkpoint_fixture":"NATIVE_PASS"}))

    def test_private_game_rig_reflection_not_permitted(self):
        self.rejects(lambda x:x["security"].update(
            whole_game_private_510_hydration="REFLECTION"))

    def test_powerloss_atomicity_not_proven(self):
        self.rejects(lambda x:x["security"].update(
            crash_atomic_payload_and_checkpoint="PASS"))

    def test_missing_candidate_642(self):
        self.rejects(lambda x:x["candidates"].pop(0))

    def test_duplicate_candidate_644(self):
        self.rejects(lambda x:x["candidates"].__setitem__(
            0,copy.deepcopy(x["candidates"][1])))

    def test_unknown_candidate_645(self):
        self.rejects(lambda x:x["candidates"][0].update(pr=645))

    def test_642_source_sha_drift(self):
        self.rejects(lambda x:x["candidates"][0]["source"].update(sha="b"*40))

    def test_644_source_sha_drift(self):
        self.rejects(lambda x:x["candidates"][1]["source"].update(sha="b"*40))

    def test_642_inner_zip_not_outer_zip(self):
        self.rejects(lambda x:x["candidates"][0]["source"].update(
            zip_sha256="c"*64))

    def test_644_inner_zip_not_outer_zip(self):
        self.rejects(lambda x:x["candidates"][1]["source"].update(
            zip_sha256="c"*64))

    def test_642_archive_size_drift(self):
        self.rejects(lambda x:x["candidates"][0]["source"].update(
            zip_bytes=81190))

    def test_644_guid_count_drift(self):
        self.rejects(lambda x:x["candidates"][1]["source"].update(
            unity_guid_count=10))

    def test_642_core_implementation_not_independent(self):
        self.rejects(lambda x:x["candidates"][0]["source"].update(
            core_stale_export_guard="RELEASE_GREEN"))

    def test_644_old_core_must_not_claim_direct_fix(self):
        self.rejects(lambda x:x["candidates"][1]["source"].update(
            core_stale_export_guard="R3_REPAIRED_CORE"))

    def test_642_wrong_producer_ci(self):
        self.rejects(lambda x:x["candidates"][0].update(
            producer_ci_url="https://github.com/C8LyNn/RoboRougelike/actions/runs/41105901634"))

    def test_644_wrong_producer_ci(self):
        self.rejects(lambda x:x["candidates"][1]["source"].update(
            producer_ci_run=41106902806))

    def test_642_must_be_unselected(self):
        self.rejects(lambda x:x["candidates"][0].update(
            branch_selection="WINNER"))

    def test_644_independent_qa_not_assumed(self):
        self.rejects(lambda x:x["candidates"][1].update(
            independent_qa="PASS"))

    def test_642_wrong_lineage_parent(self):
        self.rejects(lambda x:x["candidates"][0].update(
            lineage_parent_pr=604))

    def test_644_fake_nativetest(self):
        self.rejects(lambda x:x["candidates"][1].update(native_g1="PASS"))

    def test_source_ci_not_native(self):
        self.rejects(lambda x:x["candidates"][1].update(
            ci_status="UNITY_PASS"))

    def test_642_forged_qa_accepted(self):
        self.rejects(lambda x:x["candidates"][0]["lifecycle"].update(
            QA_ACCEPTED=True))

    def test_644_forged_integrator_ready(self):
        self.rejects(lambda x:x["candidates"][1]["lifecycle"].update(
            READY_FOR_INTEGRATOR=True))

    def test_644_forged_released(self):
        self.rejects(lambda x:x["candidates"][1]["lifecycle"].update(
            RELEASED=True))

    def test_omitted_sixth_stage(self):
        self.rejects(lambda x:x["candidates"][0]["lifecycle"].pop("RELEASED"))

    def test_fake_codex_accepted(self):
        self.rejects(lambda x:x["integrator_feedback"].update(
            status="ACCEPTED"))

    def test_fake_integrator_time(self):
        self.rejects(lambda x:x["integrator_feedback"].update(
            elapsed_seconds=10))

    def test_fake_zero_retries(self):
        self.rejects(lambda x:x["integrator_feedback"].update(
            attempts=0))

    def test_fake_zero_regressions(self):
        self.rejects(lambda x:x["integrator_feedback"].update(
            regressions=0))

    def test_fake_integrator_git_sha(self):
        self.rejects(lambda x:x["integrator_feedback"].update(
            integration_sha="f"*40))

    def test_cli_honest_hold_passes_as_source_consistency_only(self):
        out=io.StringIO()
        with redirect_stdout(out):
            self.assertEqual(main([str(FIXTURE)]),0)
        obj=json.loads(out.getvalue())
        self.assertEqual(obj["status"],
            "HOLD_OWNER_R3_EXCLUSIVE_SELECTION_AND_INDEPENDENT_QA")
        self.assertEqual(len(obj["receipt_sha256"]),64)

    def test_cli_release_strict_exit3(self):
        with redirect_stdout(io.StringIO()):
            self.assertEqual(main([str(FIXTURE),"--require-ready"]),3)

    def test_cli_refuses_overwrite_fixture(self):
        with redirect_stderr(io.StringIO()):
            self.assertEqual(main([str(FIXTURE),"--output",str(FIXTURE)]),2)

    def test_cli_refuses_overwrite_foreign(self):
        with tempfile.TemporaryDirectory() as td:
            path=Path(td)/"foreign"
            path.write_text("DO NOT TOUCH")
            with redirect_stderr(io.StringIO()):
                self.assertEqual(main([str(FIXTURE),"--output",str(path)]),2)
            self.assertEqual(path.read_text(),"DO NOT TOUCH")

    def test_cli_atomic_write_once(self):
        with tempfile.TemporaryDirectory() as td:
            path=Path(td)/"own"
            with redirect_stderr(io.StringIO()):
                self.assertEqual(main([str(FIXTURE),"--output",str(path)]),0)
                self.assertEqual(main([str(FIXTURE),"--output",str(path)]),2)
            self.assertIsNone(json.loads(path.read_text())["selected_pr"])

    def test_cli_invalid_json_rejected(self):
        with tempfile.TemporaryDirectory() as td:
            path=Path(td)/"bad"
            path.write_text("{not JSON")
            with redirect_stderr(io.StringIO()):
                self.assertEqual(main([str(path)]),2)

if __name__=="__main__":
    unittest.main()
