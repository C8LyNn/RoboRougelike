"""W17 exact immutable corrected bridge version intake: NO fake G0/Unity/ACK."""
import copy
from contextlib import redirect_stderr, redirect_stdout
import io
import json
from pathlib import Path
import tempfile
import unittest
from save_admission import AdmissionError, audit_final_w17, final_w17_main

SOURCE = Path(__file__).with_name("bridge_w17_final_pending.json")


def source():
    return json.loads(SOURCE.read_text(encoding="utf-8"))


class ExclusiveFinalSourceReceipt(unittest.TestCase):
    def rejects(self, fn, pattern):
        r = source()
        fn(r)
        with self.assertRaisesRegex(AdmissionError, pattern):
            audit_final_w17(r)

    def test_real_two_R2_drafts_hold(self):
        x = audit_final_w17(source())
        self.assertEqual(x["verdict"], "HOLD_ONE_OWNER_VERSION_AND_PHYSICAL_G0")
        self.assertIsNone(x["selected_version_pr"])
        self.assertFalse(x["authorized_offer"])

    def test_no_false_604_full_Unity_QA(self):
        x = audit_final_w17(source())
        self.assertEqual(x["one_qa_scope_604"], "UNCHANGED_CORE_FENCE_ONLY_NOT_WHOLE_HEAD")

    def test_596_exact_independent_source_QA(self):
        r = source()["versions"][0]
        self.assertEqual(r["qa_tested_commit"], r["sha"])
        self.assertEqual(r["qa_assertions_per_os"], 82)

    def test_604_previous_QA_core_scope(self):
        r = source()["versions"][1]
        self.assertNotEqual(r["qa_tested_commit"], r["sha"])
        self.assertEqual(r["qa_assertions_per_os"], 52)

    def test_green_source_does_not_approve_physical_g0(self):
        self.assertEqual(audit_final_w17(source())["physical_g0"], "HOLD")

    def test_green_source_does_not_approve_native_unity(self):
        self.assertEqual(audit_final_w17(source())["native_g1"], "NOT_RUN")

    def test_no_Codex_ack_or_merge(self):
        x = audit_final_w17(source())
        self.assertEqual(x["codex_ack"], "UNCONFIRMED")
        self.assertFalse(x["installed"])
        self.assertFalse(x["merged"])

    def test_original_587_stays_red(self):
        self.assertEqual(audit_final_w17(source())["historical_587_release"], "HOLD_P0_RED")

    def test_schema_downgrade(self):
        self.rejects(lambda r: r.update(schema="b01-w16-bridge-source-admission-v1"), "W17 schema")

    def test_gameplay_sha_drift(self):
        self.rejects(lambda r: r["frozen"].update(game_sha="a"*40), "frozen")

    def test_save_sha_drift(self):
        self.rejects(lambda r: r["frozen"].update(save_sha="a"*40), "frozen")

    def test_unfreeze_game(self):
        self.rejects(lambda r: r["frozen"].update(immutable=False), "frozen")

    def test_unselected_585_lineage(self):
        self.rejects(lambda r: r["lineage"].update(selected_original_pr=585), "lineage")

    def test_fake_prior_587_GREEN(self):
        self.rejects(lambda r: r["lineage"].update(original_587_source_release="PASS"), "lineage")

    def test_remove_596(self):
        self.rejects(lambda r: r["versions"].pop(0), "two original")

    def test_add_third_competing_597(self):
        self.rejects(lambda r: r["versions"].append(copy.deepcopy(r["versions"][0])), "two original")

    def test_duplicate_604(self):
        self.rejects(lambda r: r["versions"].__setitem__(0,copy.deepcopy(r["versions"][1])),
                     "duplicate")

    def test_596_sha_spoof(self):
        self.rejects(lambda r: r["versions"][0].update(sha="a"*40), "source")

    def test_604_sha_spoof(self):
        self.rejects(lambda r: r["versions"][1].update(sha="a"*40), "source")

    def test_596_wrong_independent_qa_sha(self):
        self.rejects(lambda r: r["versions"][0].update(qa_sha="a"*40), "source")

    def test_604_fake_whole_package_green(self):
        self.rejects(lambda r: r["versions"][1].update(qa_scope="FULL_NATIVE_UNITY_PASS"),
                     "source")

    def test_604_fake_latest_sha_tested(self):
        self.rejects(lambda r: r["versions"][1].update(
            qa_tested_commit=r["versions"][1]["sha"]), "source")

    def test_596_wrong_zip_sha(self):
        self.rejects(lambda r: r["versions"][0].update(archive_sha256="a"*64), "source")

    def test_604_wrong_zip_members(self):
        self.rejects(lambda r: r["versions"][1].update(archive_entries=23), "source")

    def test_596_fake_native_producer(self):
        self.rejects(lambda r: r["versions"][0].update(producer_ci="UNITY_PASS"), "source")

    def test_604_wrong_original_qa_pr(self):
        self.rejects(lambda r: r["versions"][1].update(qa_pr=607), "source")

    def test_coordinator_cannot_select_604(self):
        self.rejects(lambda r: r["owner_choice"].update(owner="B01-00"), "owner")

    def test_pending_cannot_mark_596(self):
        self.rejects(lambda r: r["owner_choice"].update(selected_pr=596), "pending")

    def test_pending_cannot_mark_604(self):
        self.rejects(lambda r: r["owner_choice"].update(selected_pr=604), "pending")

    def test_pending_cannot_fake_loser(self):
        self.rejects(lambda r: r["owner_choice"].update(loser_pr=604), "pending")

    def test_pending_cannot_fake_decision(self):
        self.rejects(lambda r: r["owner_choice"].update(
            decision_url="https://github.com/C8LyNn/RoboRougelike/issues/608#issuecomment-999"),
            "pending")

    def test_selected_596_can_still_be_hold(self):
        d = source()
        d["owner_choice"].update(state="ONE_FROZEN",selected_pr=596,loser_pr=604,
                                 decision_url="https://github.com/C8LyNn/RoboRougelike/issues/608#issuecomment-999")
        x = audit_final_w17(d)
        self.assertEqual(x["version_sha"],d["versions"][0]["sha"])
        self.assertEqual(x["physical_g0"],"HOLD")
        self.assertFalse(x["authorized_offer"])

    def test_selected_604_can_still_be_hold(self):
        d = source()
        d["owner_choice"].update(state="ONE_FROZEN",selected_pr=604,loser_pr=596,
                                 decision_url="https://github.com/C8LyNn/RoboRougelike/issues/608#issuecomment-999")
        x = audit_final_w17(d)
        self.assertEqual(x["version_sha"],d["versions"][1]["sha"])
        self.assertEqual(x["canonical_game_save_load"],"HOLD")

    def test_two_selected_same_version_denied(self):
        self.rejects(lambda r: r["owner_choice"].update(
            state="ONE_FROZEN",selected_pr=596,loser_pr=596,
            decision_url="https://github.com/C8LyNn/RoboRougelike/issues/608#issuecomment-999"),
            "one explicit")

    def test_fake_owner_url_denied(self):
        self.rejects(lambda r: r["owner_choice"].update(
            state="ONE_FROZEN",selected_pr=604,loser_pr=596,
            decision_url="https://attacker.example/pull/604#issuecomment-999"),
            "one explicit")

    def test_fake_physical_source_g0(self):
        self.rejects(lambda r: r["receiver"].update(bridge_physical_g0="ACCEPTED_SOURCE_ONLY"),
                     "fake SOURCE_G0")

    def test_fake_unity_playmode(self):
        self.rejects(lambda r: r["receiver"].update(unity_playmode_g1="PASS"),
                     "fake SOURCE_G0")

    def test_fake_unity_physx(self):
        self.rejects(lambda r: r["receiver"].update(unity_physx_g1="PASS"),
                     "fake SOURCE_G0")

    def test_private_ledger_reflection(self):
        self.rejects(lambda r: r["receiver"].update(canonical_private_ledger_hydration="REFLECTION"),
                     "fake SOURCE_G0")

    def test_fake_canonical_game_save(self):
        self.rejects(lambda r: r["receiver"].update(live_cold_game_save_load="PASS"),
                     "fake SOURCE_G0")

    def test_fake_canonical_migration(self):
        self.rejects(lambda r: r["receiver"].update(real_scene_migration="DONE"),
                     "fake SOURCE_G0")

    def test_unsafe_original_510_rollback(self):
        self.rejects(lambda r: r["receiver"].update(original_510_rollback="ALLOWED"),
                     "fake SOURCE_G0")

    def test_codex_fake_ack(self):
        self.rejects(lambda r: r["receiver"].update(codex_ack="YES"),
                     "fake SOURCE_G0")

    def test_fake_g2(self):
        self.rejects(lambda r: r["receiver"].update(g2="PASS"),
                     "fake SOURCE_G0")

    def test_fake_merge(self):
        self.rejects(lambda r: r["receiver"].update(merged=True), "fake SOURCE_G0")

    def test_g0_pending_means_no_inbox_offer(self):
        self.rejects(lambda r: r.update(offer_to_codex=True), "no offer")

    def test_missing_gate(self):
        self.rejects(lambda r: r["receiver"].pop("rollback"), "missing physical")

    def test_extra_gate_injection(self):
        self.rejects(lambda r: r["receiver"].update(skip_qa=True), "missing physical")

    def test_cli_real_hold_report(self):
        out=io.StringIO()
        with redirect_stdout(out):
            self.assertEqual(final_w17_main([str(SOURCE)]),0)
        self.assertEqual(json.loads(out.getvalue())["verdict"],
                         "HOLD_ONE_OWNER_VERSION_AND_PHYSICAL_G0")

    def test_cli_strict_release_exit_3(self):
        with redirect_stdout(io.StringIO()):
            self.assertEqual(final_w17_main([str(SOURCE),"--require-accepted-source"]),3)

    def test_cli_damaged_json_fails_closed(self):
        with tempfile.TemporaryDirectory() as td:
            q=Path(td)/"bad"
            q.write_text("{")
            with redirect_stderr(io.StringIO()):
                self.assertEqual(final_w17_main([str(q)]),2)

    def test_cli_existing_output_preserved(self):
        with tempfile.TemporaryDirectory() as td:
            q=Path(td)/"existing"
            q.write_text("USER FILE")
            with redirect_stderr(io.StringIO()):
                self.assertEqual(final_w17_main([str(SOURCE),"--output",str(q)]),2)
            self.assertEqual(q.read_text(),"USER FILE")

    def test_cli_write_once(self):
        with tempfile.TemporaryDirectory() as td:
            q=Path(td)/"own_output"
            with redirect_stdout(io.StringIO()),redirect_stderr(io.StringIO()):
                self.assertEqual(final_w17_main([str(SOURCE),"--output",str(q)]),0)
                self.assertEqual(final_w17_main([str(SOURCE),"--output",str(q)]),2)
            self.assertEqual(json.loads(q.read_text())["physical_g0"],"HOLD")


if __name__ == "__main__":
    unittest.main()
