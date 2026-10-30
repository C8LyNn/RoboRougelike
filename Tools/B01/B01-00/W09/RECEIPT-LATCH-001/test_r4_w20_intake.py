"""W20 B01-00 R4 source identity, explicit owner, operator G1 and API gates."""
import copy
import io
import json
import tempfile
import unittest
from contextlib import redirect_stdout, redirect_stderr
from pathlib import Path
from r4_w20_intake import check, main, IntakeError

FIXTURE=Path(__file__).with_name("r4_w20_pending.json")
def load():
    return json.loads(FIXTURE.read_text(encoding="utf-8"))

class W20Admission(unittest.TestCase):
    def denies(self,mutate):
        receipt=load()
        mutate(receipt)
        with self.assertRaises(IntakeError):
            check(receipt)

    def test_base_policy_not_promoted(self):
        result=check(load())
        self.assertEqual(result["verdict"],
            "HOLD_SELECTED_647_FORMAL_G0_AND_NATIVE_API")
        self.assertEqual(result["r4_final_sha"],
            "09c1809b73324a6d31c13e5d0143de62ac1e3ca6")
        self.assertEqual(result["r4_candidates"],[647,652])

    def test_real_647_independent_qa_scoped(self):
        result=check(load())
        self.assertEqual(result["r4_647_independent_source_qa"],
                         "GREEN_ONLY_FOR_ORIGINAL_647")

    def test_real_652_risk_not_native(self):
        self.assertEqual(check(load())["r4_652_lease_lifecycle"],
                         "SOURCE_RISK_NOT_EXACT_INDEPENDENT_QA")

    def test_canonical_authorization_not_completion(self):
        self.assertEqual(check(load())["canonical_api"],
                         "AUTHORIZED_TO_DEVELOP_NOT_IMPLEMENTED")

    def test_local_unity_no_made_up_run(self):
        self.assertEqual(check(load())["native_unity_g1"],"NOT_RUN")

    def test_no_invented_integration_metrics(self):
        self.assertIsNone(check(load())["acceptance_rate"])
        self.assertIsNone(check(load())["elapsed_seconds"])

    def test_no_invented_human_playtests(self):
        self.assertEqual(check(load())["economy"],
                         "PLAYTEST_HYPOTHESIS_NO_HUMAN_DATA")

    def test_wrong_schema(self):
        self.denies(lambda x: x.update(schema="fake"))

    def test_wrong_game_sha(self):
        self.denies(lambda x: x["frozen"].update(game510="a"*40))

    def test_wrong_save_sha(self):
        self.denies(lambda x: x["frozen"].update(save556="a"*40))

    def test_wrong_host_sha(self):
        self.denies(lambda x: x["frozen"].update(host596="a"*40))

    def test_wrong_r3_sha(self):
        self.denies(lambda x: x["frozen"].update(r3_642="a"*40))

    def test_old_r3_alt_admitted(self):
        self.denies(lambda x: x["frozen"].update(r3_644="SELECTED"))

    def test_old_r2_alt_admitted(self):
        self.denies(lambda x: x["frozen"].update(r2_604="SELECTED"))

    def test_fake_647_selected(self):
        self.denies(lambda x: x["owner_choice"].update(winner_pr=647,decision="FROZEN"))

    def test_fake_652_selected(self):
        self.denies(lambda x: x["owner_choice"].update(winner_pr=652,loser_pr=647))

    def test_fake_owner_url(self):
        self.denies(lambda x: x["owner_choice"].update(comment_url="https://example.com"))

    def test_other_role_owner(self):
        self.denies(lambda x: x["owner_choice"].update(authority="B01-00"))

    def test_fake_canonical_implemented(self):
        self.denies(lambda x: x["canonical_api"].update(implemented=True))

    def test_fake_canonical_pr(self):
        self.denies(lambda x: x["canonical_api"].update(source_pr=699))

    def test_fake_approved_rig_capture(self):
        self.denies(lambda x: x["canonical_api"].update(captured_private_rig_ledger=True))

    def test_fake_atomic_recovery(self):
        self.denies(lambda x: x["canonical_api"].update(atomic_single_slot_recovery="PASS"))

    def test_fake_canonical_integration(self):
        self.denies(lambda x: x["canonical_api"].update(canonical_game_integrated=True))

    def test_unsafe_local_automatic_execution(self):
        self.denies(lambda x: x["local_unity"].update(local_operator_opt_in=True))

    def test_fake_unity_runner(self):
        self.denies(lambda x: x["local_unity"].update(runner_pr=677))

    def test_fake_unity_source_review(self):
        self.denies(lambda x: x["local_unity"].update(source_review="PASS"))

    def test_unity_wrong_editor_version(self):
        self.denies(lambda x: x["local_unity"].update(exact_editor_version="6000.2.0f1"))

    def test_fake_private_checkout(self):
        self.denies(lambda x: x["local_unity"].update(private_disposable_checkout="VERIFIED"))

    def test_credentials_leak(self):
        self.denies(lambda x: x["local_unity"].update(credentials_disclosed=True))

    def test_ssh_remote_execution(self):
        self.denies(lambda x: x["local_unity"].update(remote_ssh_used=True))

    def test_fake_native_editor_execution(self):
        self.denies(lambda x: x["local_unity"].update(unity_editor_executed=True))

    def test_fake_editmode(self):
        self.denies(lambda x: x["local_unity"].update(editmode="PASS"))

    def test_fake_playmode(self):
        self.denies(lambda x: x["local_unity"].update(playmode="PASS"))

    def test_fake_physx(self):
        self.denies(lambda x: x["local_unity"].update(physx="PASS"))

    def test_forged_nunit_xml(self):
        self.denies(lambda x: x["local_unity"].update(nunit_xml="pass.xml"))

    def test_forged_editor_log(self):
        self.denies(lambda x: x["local_unity"].update(editor_log="unity.log"))

    def test_false_native_source_manifest(self):
        self.denies(lambda x: x["local_unity"].update(source_manifest="sha256"))

    def test_wrong_econ_cadence(self):
        self.denies(lambda x: x["economy"].update(policy="PER_RUN_FINAL"))

    def test_econ_more_than_one_free(self):
        self.denies(lambda x: x["economy"].update(global_free_reservations_per_victory=2))

    def test_econ_replay_permitted(self):
        self.denies(lambda x: x["economy"].update(victory_event_id_one_use=False))

    def test_fake_econ_source_implemented(self):
        self.denies(lambda x: x["economy"].update(stale_restore_replay_denied="PASS"))

    def test_econ_frozen_reference_changed(self):
        self.denies(lambda x: x["economy"].update(source252="PATCHED"))

    def test_fake_econ_price(self):
        self.denies(lambda x: x["economy"].update(unit_price=120))

    def test_fake_playtest_numbers(self):
        self.denies(lambda x: x["economy"].update(player_balancing_trials=50))

    def test_fake_human_feedback(self):
        self.denies(lambda x: x["economy"].update(human_preference_measured=True))

    def test_fake_r4_physical_g0(self):
        self.denies(lambda x: x["gates"].update(selected_r4_g0="ACCEPTED"))

    def test_fake_r4_native_g1(self):
        self.denies(lambda x: x["gates"].update(new_r4_native_g1="PASS"))

    def test_fake_live_save(self):
        self.denies(lambda x: x["gates"].update(canonical_game_save="PASS"))

    def test_fake_codex_available(self):
        self.denies(lambda x: x["gates"].update(codex_available=True))

    def test_fake_codex_offer(self):
        self.denies(lambda x: x["gates"].update(codex_offer="SENT"))

    def test_fake_codex_ack(self):
        self.denies(lambda x: x["gates"].update(codex_ack="YES"))

    def test_fake_g2(self):
        self.denies(lambda x: x["gates"].update(g2="PASS"))

    def test_fake_merge(self):
        self.denies(lambda x: x["gates"].update(merged=True))

    def test_fake_integrated(self):
        self.denies(lambda x: x["gates"].update(integrated=True))

    def test_fake_released(self):
        self.denies(lambda x: x["gates"].update(released=True))

    def test_fake_independent_qa(self):
        self.denies(lambda x: x["lifecycle"].update(QA_ACCEPTED=True))

    def test_fake_ready_for_integrator(self):
        self.denies(lambda x: x["lifecycle"].update(READY_FOR_INTEGRATOR=True))

    def test_fake_lifecycle_release(self):
        self.denies(lambda x: x["lifecycle"].update(RELEASED=True))

    def test_fake_codex_acceptance(self):
        self.denies(lambda x: x["integrator_feedback"].update(outcome="ACCEPTED"))

    def test_fake_review_url(self):
        self.denies(lambda x: x["integrator_feedback"].update(review_url="https://example.com"))

    def test_fake_acceptable_time(self):
        self.denies(lambda x: x["integrator_feedback"].update(elapsed_seconds=0))

    def test_fake_retries_zero(self):
        self.denies(lambda x: x["integrator_feedback"].update(attempts=0))

    def test_fake_regressions_zero(self):
        self.denies(lambda x: x["integrator_feedback"].update(regressions=0))

    def test_fake_conflicts_zero(self):
        self.denies(lambda x: x["integrator_feedback"].update(conflicts=0))

    def test_fake_metadata_selection_source(self):
        self.denies(lambda x: x.update(authoritative_selection_source="MODEL_DECISION"))

    def test_mix_both_r4(self):
        self.denies(lambda x: x.update(co_install_alternatives=True))

    def test_protected_merge_unapproved(self):
        self.denies(lambda x: x.update(authorized_protected_merge=True))

    def test_missing_second_candidate(self):
        self.denies(lambda x: x["r4_candidates"].pop())

    def test_duplicate_647_candidate(self):
        self.denies(lambda x: x["r4_candidates"].__setitem__(1,copy.deepcopy(x["r4_candidates"][0])))

    def test_wrong_647_sha(self):
        self.denies(lambda x: x["r4_candidates"][0]["source"].update(sha="a"*40))

    def test_wrong_652_sha(self):
        self.denies(lambda x: x["r4_candidates"][1]["source"].update(sha="a"*40))

    def test_wrong_647_zip(self):
        self.denies(lambda x: x["r4_candidates"][0]["source"].update(physical_zip_sha256="f"*64))

    def test_wrong_652_zip_members(self):
        self.denies(lambda x: x["r4_candidates"][1]["source"].update(physical_zip_members=23))

    def test_false_647_independent_scope(self):
        self.denies(lambda x: x["r4_candidates"][0]["source"]["independent_security"].update(scope="NATIVE_UNITY"))

    def test_false_652_independent_qa(self):
        self.denies(lambda x: x["r4_candidates"][1]["source"].update(independent_security={"pr":700}))

    def test_wrong_647_owner(self):
        self.denies(lambda x: x["r4_candidates"][0].update(owner="B01-00"))

    def test_wrong_652_lineage(self):
        self.denies(lambda x: x["r4_candidates"][1].update(parent="r3_644"))

    def test_premature_647_admission(self):
        self.denies(lambda x: x["r4_candidates"][0].update(admission="SOURCE_G0"))

    def test_premature_647_formal_g0(self):
        self.denies(lambda x: x["r4_candidates"][0].update(independent_formal_g0="PASS"))

    def test_fake_652_native(self):
        self.denies(lambda x: x["r4_candidates"][1].update(native_unity="PASS"))

    def test_wrong_647_url(self):
        self.denies(lambda x: x["r4_candidates"][0].update(original_pr_url="https://example.com"))

    def test_cli_valid_hold_not_release(self):
        buffer=io.StringIO()
        with redirect_stdout(buffer):
            self.assertEqual(main([str(FIXTURE)]),0)
        actual=json.loads(buffer.getvalue())
        self.assertEqual(actual["verdict"],
                         "HOLD_SELECTED_647_FORMAL_G0_AND_NATIVE_API")
        self.assertEqual(len(actual["input_sha256"]),64)

    def test_cli_require_release_exit3(self):
        with redirect_stdout(io.StringIO()):
            self.assertEqual(main([str(FIXTURE),"--require-release"]),3)

    def test_cli_existing_file_preserved(self):
        with tempfile.TemporaryDirectory() as tmp:
            output=Path(tmp)/"foreign"
            output.write_text("KEEP")
            with redirect_stderr(io.StringIO()):
                self.assertEqual(main([str(FIXTURE),"--output",str(output)]),2)
            self.assertEqual(output.read_text(),"KEEP")

    def test_cli_original_input_preserved(self):
        with redirect_stderr(io.StringIO()):
            self.assertEqual(main([str(FIXTURE),"--output",str(FIXTURE)]),2)

    def test_cli_write_only_new(self):
        with tempfile.TemporaryDirectory() as tmp:
            output=Path(tmp)/"own.json"
            with redirect_stderr(io.StringIO()):
                self.assertEqual(main([str(FIXTURE),"--output",str(output)]),0)
                self.assertEqual(main([str(FIXTURE),"--output",str(output)]),2)
            self.assertEqual(json.loads(output.read_text())["r4_final_pr"],647)

    def test_cli_invalid_json_fails(self):
        with tempfile.TemporaryDirectory() as tmp:
            bad=Path(tmp)/"bad.json"
            bad.write_bytes(b"{\xff")
            with redirect_stderr(io.StringIO()):
                self.assertEqual(main([str(bad)]),2)

if __name__ == "__main__":
    unittest.main()
