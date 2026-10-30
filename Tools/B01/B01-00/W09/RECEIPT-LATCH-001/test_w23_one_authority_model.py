"""W23 reference authority model source tests; no native Unity/real FileStream."""
import copy
import io
import json
import tempfile
import unittest
from contextlib import redirect_stderr,redirect_stdout
from pathlib import Path
from w23_one_authority_model import (
    OneAuthorityModel,Rejected,validate_receipt,main)

FIXTURE=Path(__file__).with_name("w23_owner_snapshot.json")
GOOD_DIGEST="a"*64

def raw():return json.loads(FIXTURE.read_text(encoding="utf-8"))
def live():
    m=OneAuthorityModel()
    m.acquire("alpha")
    m.stage("alpha")
    m.publish("alpha")
    return m

class W23OwnerReceipt(unittest.TestCase):
    def deny(self,fn):
        data=raw()
        fn(data)
        with self.assertRaises(Rejected):
            validate_receipt(data)

    def test_source_only_hold_is_truthful(self):
        r=validate_receipt(raw())
        self.assertEqual(r["verdict"],
                         "HOLD_V3_729_TO_SESSION_712_DUAL_LOCK_NOT_INTEGRATED")
        self.assertEqual(r["selected_api"],729)
        self.assertEqual(r["alternative_v3"],719)
    def test_generation_actual_vs_fixture(self):
        r=validate_receipt(raw())
        self.assertEqual(r["generation_written_by_r5"],5)
        self.assertEqual(r["old_fixture_expected_generation"],4)
    def test_no_second_lock_accepted(self):
        r=validate_receipt(raw())
        self.assertTrue(r["mixed_lock_paths"])
        self.assertEqual(r["single_owner"],"NOT_IMPLEMENTED")
    def test_no_native_or_private_fabrication(self):
        r=validate_receipt(raw())
        self.assertEqual(r["native_g1"],"NOT_RUN")
        self.assertEqual(r["live_private_510_save"],"HOLD")
    def test_no_codex_outcome(self):
        r=validate_receipt(raw())
        self.assertEqual(r["codex_ack"],"NONE")
        self.assertIsNone(r["actual_integrator_latency"])
    def test_receivers_include_physical_and_manual(self):
        r=validate_receipt(raw())
        self.assertEqual(r["next_receivers"]["native_g1"],
                         "B01-60+LOCAL_OPERATOR")
        self.assertEqual(r["next_receivers"]["one_lock"],"B01-23/62")
    def test_selected_729_zip_not_719(self):
        self.assertEqual(raw()["original_sources"]["canonical_v3"]["sha"],
                         "b8ca29c7707cd6b4338d0b9772d74dee676d49d2")
    def test_all_owner_stages_nonreleased(self):
        v=validate_receipt(raw())["stage"]
        self.assertFalse(v["INTEGRATED"])
        self.assertFalse(v["RELEASED"])
        self.assertFalse(v["READY_FOR_INTEGRATOR"])
    def test_historical_712_707_scope_not_729(self):
        self.assertEqual(raw()["original_sources"]["session_r2"]["independent_707"],
                         "PASS_SCOPED_OLD_V2_ONLY")
    def test_native_coldrestart_not_corrected_by_model(self):
        self.assertEqual(validate_receipt(raw())["cold_restart_fixture"],
                         "CORRECTION_REQUIRED_NEW_OWNER_SHA")
    def test_reject_schema(self):
        self.deny(lambda x:x.update(schema="wrong"))
    def test_reject_host(self):
        self.deny(lambda x:x["original_sources"]["host"].update(sha="a"*40))
    def test_reject_v3(self):
        self.deny(lambda x:x["original_sources"]["canonical_v3"].update(sha="a"*40))
    def test_reject_v3_selected_faked(self):
        self.deny(lambda x:x["original_sources"]["canonical_v3"].update(pr=719))
    def test_reject_v3_source_tests_fake(self):
        self.deny(lambda x:x["original_sources"]["canonical_v3"].update(producer="FULL_UNITY_PASS"))
    def test_reject_v3_fake_g0(self):
        self.deny(lambda x:x["original_sources"]["canonical_v3"].update(physical_g0="PASS"))
    def test_reject_v3_fake_native(self):
        self.deny(lambda x:x["original_sources"]["canonical_v3"].update(native="PASS"))
    def test_reject_session_sha(self):
        self.deny(lambda x:x["original_sources"]["session_r2"].update(sha="a"*40))
    def test_reject_session_fake_compat(self):
        self.deny(lambda x:x["original_sources"]["session_r2"].update(independent_707="GREEN_V3_NATIVE"))
    def test_reject_store_sha(self):
        self.deny(lambda x:x["original_sources"]["store"].update(sha="a"*40))
    def test_reject_store_rollback_safe(self):
        self.deny(lambda x:x["original_sources"]["store"].update(external_rollback="SAFE"))
    def test_reject_game_sha(self):
        self.deny(lambda x:x["original_sources"]["game"].update(sha="a"*40))
    def test_reject_game_private_implemented(self):
        self.deny(lambda x:x["original_sources"]["game"].update(private_port="PASS"))
    def test_reject_codec_sha(self):
        self.deny(lambda x:x["original_sources"]["codec"].update(sha="a"*40))
    def test_reject_codec_mutable(self):
        self.deny(lambda x:x["original_sources"]["codec"].update(immutable=False))
    def test_reject_r5_fixture_actual_faked(self):
        self.deny(lambda x:x["original_sources"]["cold_restart"].update(actual_generation=4))
    def test_reject_r5_fixture_expected_faked(self):
        self.deny(lambda x:x["original_sources"]["cold_restart"].update(asserted_generation=5))
    def test_reject_r5_fixture_status_faked(self):
        self.deny(lambda x:x["original_sources"]["cold_restart"].update(fixture="PASS"))
    def test_reject_runner_native_faked(self):
        self.deny(lambda x:x["original_sources"]["runner"].update(native="PASS"))
    def test_reject_alts_empty(self):
        self.deny(lambda x:x["alternatives_hold"].clear())
    def test_reject_alts_719_accepted(self):
        self.deny(lambda x:x["alternatives_hold"].remove(719))
    def test_reject_alts_wrong_order(self):
        self.deny(lambda x:x["alternatives_hold"].reverse())
    def test_reject_actor_missing(self):
        self.deny(lambda x:x["receivers"].pop("one_lock"))
    def test_reject_actor_wrong(self):
        self.deny(lambda x:x["receivers"].update(one_lock="B01-00"))
    def test_reject_native_owner_wrong(self):
        self.deny(lambda x:x["receivers"].update(native_g1="AUTO_REMOTE"))
    def test_reject_selected_719(self):
        self.deny(lambda x:x["observed_risk"].update(sole_selected_v3=719))
    def test_reject_loser_719_selected(self):
        self.deny(lambda x:x["observed_risk"].update(loser_status="SELECTED"))
    def test_reject_host_alt_693(self):
        self.deny(lambda x:x["observed_risk"].update(frozen_host=693))
    def test_reject_bridge_faked(self):
        self.deny(lambda x:x["observed_risk"].update(canonical_bridge_pr=800))
    def test_reject_bridge_sha_faked(self):
        self.deny(lambda x:x["observed_risk"].update(bridge_git_sha="a"*40))
    def test_reject_single_lock_faked(self):
        self.deny(lambda x:x["observed_risk"].update(same_os_lock="PASS"))
    def test_reject_constructor_faked(self):
        self.deny(lambda x:x["observed_risk"].update(constructor_v3_r2_compiles="PASS"))
    def test_reject_lease_mixed_unnoticed(self):
        self.deny(lambda x:x["observed_risk"].update(lease_layout_legacy=[".owner"]))
    def test_reject_private_world_faked(self):
        self.deny(lambda x:x["observed_risk"].update(owned_host_world_binding="PASS"))
    def test_reject_rollback_trusted_faked(self):
        self.deny(lambda x:x["observed_risk"].update(trusted_external_rollback="PROVEN"))
    def test_reject_cold_restart_fix_pr_faked(self):
        self.deny(lambda x:x["observed_risk"].update(cold_restart_corrected_new_pr=999))
    def test_reject_independent_v3_faked(self):
        self.deny(lambda x:x["observed_risk"].update(independent_v3_qa="PASS"))
    def test_reject_combined_g0_faked(self):
        self.deny(lambda x:x["observed_risk"].update(combined_source_g0="PASS"))
    def test_reject_native_g1_faked(self):
        self.deny(lambda x:x["observed_risk"].update(unity_g1="PASS"))
    def test_reject_codex_ack_faked(self):
        self.deny(lambda x:x["observed_risk"].update(codex_ack="YES"))
    def test_reject_fake_integrator_time(self):
        self.deny(lambda x:x["observed_risk"].update(integrator_feedback="ACCEPTED"))
    def test_reject_merged_faked(self):
        self.deny(lambda x:x["observed_risk"].update(merged=True))
    def test_reject_source_only_false(self):
        self.deny(lambda x:x.update(source_only_handoff=False))
    def test_reject_model_proven_real_faked(self):
        self.deny(lambda x:x.update(tests_are_reference_model_only=False))
    def test_reject_pc_access_faked(self):
        self.deny(lambda x:x.update(user_pc_access=True))
    def test_reject_xml_faked(self):
        self.deny(lambda x:x.update(native_nunit_xml="Passed.xml"))
    def test_reject_editor_log_faked(self):
        self.deny(lambda x:x.update(native_editor_log="Editor.log"))
    def test_reject_human_playtests_faked(self):
        self.deny(lambda x:x.update(human_economy_trials=10))

class W23SingleOwnerReference(unittest.TestCase):
    def deny(self,fn):
        with self.assertRaises(Rejected):fn()
    def test_one_owner_acquires(self):
        self.assertEqual(OneAuthorityModel().acquire("a"),True)
    def test_valid_model_publication(self):
        m=live()
        self.assertTrue(m.live)
        self.assertFalse(m.partial)
    def test_model_commit_epoch_one(self):
        m=live()
        self.assertEqual(m.commit("alpha",1,GOOD_DIGEST),"COMMITTED")
    def test_model_second_owner_denied(self):
        m=live()
        self.deny(lambda:m.acquire("beta"))
    def test_model_same_owner_reentrant_denied(self):
        m=live()
        self.deny(lambda:m.acquire("alpha"))
    def test_model_legacy_two_locks_denied(self):
        m=OneAuthorityModel()
        self.deny(lambda:m.acquire("A",other_lock=".lease"))
    def test_model_noncanonical_owner_root_denied(self):
        self.deny(lambda:OneAuthorityModel(lock_path=".owner"))
    def test_model_store_separate_lease_denied(self):
        self.deny(lambda:OneAuthorityModel(lock_path=".lease"))
    def test_model_wrong_run_denied(self):
        m=OneAuthorityModel()
        self.deny(lambda:m.acquire("A",run=53))
    def test_model_wrong_slot_denied(self):
        m=OneAuthorityModel()
        self.deny(lambda:m.acquire("A",slot="slot-B"))
    def test_model_no_actor_denied(self):
        self.deny(lambda:OneAuthorityModel().acquire(""))
    def test_model_negative_run_denied(self):
        self.deny(lambda:OneAuthorityModel(run=-2))
    def test_model_empty_slot_denied(self):
        self.deny(lambda:OneAuthorityModel(slot=""))
    def test_model_unowned_stage_denied(self):
        self.deny(lambda:OneAuthorityModel().stage("alpha"))
    def test_model_unowned_commit_denied(self):
        self.deny(lambda:OneAuthorityModel().commit("alpha",1,GOOD_DIGEST))
    def test_model_not_published_commit_denied(self):
        m=OneAuthorityModel()
        m.acquire("alpha")
        m.stage("alpha")
        self.deny(lambda:m.commit("alpha",1,GOOD_DIGEST))
    def test_model_pending_partial_not_published(self):
        m=OneAuthorityModel()
        m.acquire("alpha")
        m.stage("alpha",partial=True)
        self.deny(lambda:m.publish("alpha"))
    def test_model_double_stage_denied(self):
        m=OneAuthorityModel()
        m.acquire("alpha")
        m.stage("alpha",partial=True)
        self.deny(lambda:m.stage("alpha",partial=True))
    def test_model_partial_false_cleanup_quarantine(self):
        m=OneAuthorityModel()
        m.acquire("alpha")
        m.stage("alpha",partial=True)
        self.assertFalse(m.cleanup("alpha",retired=False))
        self.assertTrue(m.quarantine)
        self.deny(lambda:m.acquire("beta"))
    def test_model_failed_cleanup_release_denied(self):
        m=OneAuthorityModel()
        m.acquire("alpha")
        m.stage("alpha",partial=True)
        m.cleanup("alpha",retired=False)
        self.deny(lambda:m.release("alpha",physical_retired=True))
    def test_model_failed_cleanup_second_stage_denied(self):
        m=OneAuthorityModel()
        m.acquire("alpha")
        m.stage("alpha",partial=True)
        m.cleanup("alpha",retired=False)
        self.deny(lambda:m.stage("alpha"))
    def test_model_partial_true_cleanup(self):
        m=OneAuthorityModel()
        m.acquire("alpha")
        m.stage("alpha",partial=True)
        self.assertTrue(m.cleanup("alpha",retired=True))
        self.assertFalse(m.partial)
    def test_model_healthy_publish_after_abort(self):
        m=OneAuthorityModel()
        m.acquire("alpha")
        m.stage("alpha",partial=True)
        m.cleanup("alpha",retired=True)
        m.stage("alpha")
        self.assertTrue(m.publish("alpha"))
    def test_model_epoch_skipped_denied(self):
        m=live()
        self.deny(lambda:m.commit("alpha",2,GOOD_DIGEST))
    def test_model_epoch_equal_denied(self):
        m=live()
        self.deny(lambda:m.commit("alpha",0,GOOD_DIGEST))
    def test_model_invalid_checksum_denied(self):
        m=live()
        self.deny(lambda:m.commit("alpha",1,"z"*64))
    def test_model_invalid_checksum_size_denied(self):
        m=live()
        self.deny(lambda:m.commit("alpha",1,"a"*63))
    def test_model_second_epoch_valid(self):
        m=live()
        m.commit("alpha",1,GOOD_DIGEST)
        self.assertEqual(m.commit("alpha",2,"b"*64),"COMMITTED")
    def test_model_stale_preview_denied(self):
        m=live()
        m.commit("alpha",1,GOOD_DIGEST)
        self.deny(lambda:m.preview("alpha",2,GOOD_DIGEST))
        self.assertTrue(m.quarantine)
    def test_model_digest_fork_denied(self):
        m=live()
        m.commit("alpha",1,GOOD_DIGEST)
        self.deny(lambda:m.preview("alpha",1,"b"*64))
    def test_model_good_preview(self):
        m=live()
        m.commit("alpha",1,GOOD_DIGEST)
        self.assertEqual(m.preview("alpha",1,GOOD_DIGEST),1)
    def test_model_foreign_preview_denied(self):
        m=live()
        m.commit("alpha",1,GOOD_DIGEST)
        self.deny(lambda:m.preview("other",1,GOOD_DIGEST))
    def test_model_lost_ack_quarantine(self):
        m=live()
        self.assertEqual(m.commit("alpha",1,GOOD_DIGEST,ack_lost=True),
                         "ACK_UNKNOWN")
        self.assertTrue(m.ack_uncertain)
        self.assertTrue(m.quarantine)
    def test_model_lost_ack_second_commit_denied(self):
        m=live()
        m.commit("alpha",1,GOOD_DIGEST,ack_lost=True)
        self.deny(lambda:m.commit("alpha",2,"b"*64))
    def test_model_lost_ack_preview_denied(self):
        m=live()
        m.commit("alpha",1,GOOD_DIGEST,ack_lost=True)
        self.deny(lambda:m.preview("alpha",1,GOOD_DIGEST))
    def test_model_lost_ack_release_denied(self):
        m=live()
        m.commit("alpha",1,GOOD_DIGEST,ack_lost=True)
        self.deny(lambda:m.release("alpha",physical_retired=True))
    def test_model_live_root_cannot_release(self):
        m=live()
        self.deny(lambda:m.release("alpha",physical_retired=True))
    def test_model_incomplete_retire_cannot_release(self):
        m=live()
        m.cleanup("alpha",retired=True)
        self.deny(lambda:m.release("alpha",physical_retired=False))
    def test_model_failed_disk_readback_cannot_release(self):
        m=live()
        m.cleanup("alpha",retired=True)
        self.deny(lambda:m.release("alpha",physical_retired=True,readback_ok=False))
    def test_model_healthy_close_after_root_retired(self):
        m=live()
        m.cleanup("alpha",retired=True)
        self.assertTrue(m.release("alpha",physical_retired=True))
        self.assertIsNone(m.owner)
    def test_model_reopen_other_owner_after_retirement(self):
        m=live()
        m.cleanup("alpha",retired=True)
        m.release("alpha",physical_retired=True)
        self.assertTrue(m.acquire("beta"))
    def test_model_cold_restart_requires_no_owner(self):
        m=live()
        m.commit("alpha",1,GOOD_DIGEST)
        self.deny(lambda:m.cold_restart("beta",1,GOOD_DIGEST))
    def test_model_cold_restart_gen5_good_reference(self):
        m=live()
        for gen in range(1,6):
            m.commit("alpha",gen,GOOD_DIGEST)
        self.assertEqual(m.epoch,5)
        m.cleanup("alpha",retired=True)
        m.release("alpha",physical_retired=True)
        self.assertTrue(m.cold_restart("beta",5,GOOD_DIGEST))
    def test_model_cold_restart_wrong_gen4(self):
        m=live()
        for gen in range(1,6):
            m.commit("alpha",gen,GOOD_DIGEST)
        m.cleanup("alpha",retired=True)
        m.release("alpha",physical_retired=True)
        self.deny(lambda:m.cold_restart("beta",4,GOOD_DIGEST))
    def test_model_cold_restart_wrong_digest(self):
        m=live()
        m.commit("alpha",1,GOOD_DIGEST)
        m.cleanup("alpha",retired=True)
        m.release("alpha",physical_retired=True)
        self.deny(lambda:m.cold_restart("beta",1,"b"*64))
    def test_model_no_release_by_foreign(self):
        m=live()
        m.cleanup("alpha",retired=True)
        self.deny(lambda:m.release("beta",physical_retired=True))
    def test_model_no_quarantine_owner_steal(self):
        m=live()
        m.commit("alpha",1,GOOD_DIGEST)
        self.deny(lambda:m.preview("alpha",1,"b"*64))
        self.deny(lambda:m.acquire("beta"))

class W23CLI(unittest.TestCase):
    def test_cli_source_holds_exit0(self):
        buf=io.StringIO()
        with redirect_stdout(buf):
            self.assertEqual(main([str(FIXTURE)]),0)
        x=json.loads(buf.getvalue())
        self.assertEqual(x["selected_api"],729)
        self.assertEqual(len(x["receipt_sha256"]),64)
    def test_cli_release_strict_exit3(self):
        with redirect_stdout(io.StringIO()):
            self.assertEqual(main([str(FIXTURE),"--require-ready"]),3)
    def test_cli_bad_json_rejected(self):
        with tempfile.TemporaryDirectory() as td:
            p=Path(td)/"bad"
            p.write_bytes(b"{\xff")
            with redirect_stderr(io.StringIO()):
                self.assertEqual(main([str(p)]),2)
    def test_cli_foreign_file_unchanged(self):
        with tempfile.TemporaryDirectory() as td:
            p=Path(td)/"USER"
            p.write_text("DO NOT REMOVE")
            with redirect_stderr(io.StringIO()):
                self.assertEqual(main([str(FIXTURE),"--output",str(p)]),2)
            self.assertEqual(p.read_text(),"DO NOT REMOVE")
    def test_cli_input_unchanged(self):
        with redirect_stderr(io.StringIO()):
            self.assertEqual(main([str(FIXTURE),"--output",str(FIXTURE)]),2)
    def test_cli_new_output_once(self):
        with tempfile.TemporaryDirectory() as td:
            p=Path(td)/"receipt"
            with redirect_stderr(io.StringIO()):
                self.assertEqual(main([str(FIXTURE),"--output",str(p)]),0)
                self.assertEqual(main([str(FIXTURE),"--output",str(p)]),2)
            self.assertEqual(json.loads(p.read_text())["native_g1"],"NOT_RUN")

if __name__=="__main__":
    unittest.main()
