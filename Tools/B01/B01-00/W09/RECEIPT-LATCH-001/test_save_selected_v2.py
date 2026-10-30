"""W15 real source-only selected #556 boundary and no fictitious host gate.

The fixture pins original GitHub PR refs verified separately by coordinator.
These unit tests test a *local declarative receipt*, not native Unity.
"""
from contextlib import redirect_stderr, redirect_stdout
import copy
import io
import json
from pathlib import Path
import tempfile
import unittest

from save_admission import AdmissionError, audit_selected_v2, selected_main

RECORD = Path(__file__).with_name("save_contract_w15_selected.json")


def original():
    return json.loads(RECORD.read_text(encoding="utf-8"))


class SelectedSaveSourceHostGate(unittest.TestCase):
    def deny(self, callback, reason):
        d = original()
        callback(d)
        with self.assertRaisesRegex(AdmissionError, reason):
            audit_selected_v2(d)

    def test_real_selected_source_only_and_host_hold(self):
        r = audit_selected_v2(original())
        self.assertEqual(r["verdict"], "SOURCE_G0_RECORDED_HOST_GATE_HOLD")
        self.assertEqual(r["selected_save_pr"], 556)
        self.assertFalse(r["source_claims_authenticated_online"])
        self.assertFalse(r["release_allowed"])

    def test_gameplay_510_unmodified(self):
        self.assertEqual(audit_selected_v2(original())["selected_gameplay_pr"], 510)

    def test_exact_corrected_556_sha(self):
        self.assertEqual(audit_selected_v2(original())["selected_save_sha"],
                         "33094f154c7b91119b36d58aab285e6e83c5de57")

    def test_host_unity_is_not_implemented(self):
        r = audit_selected_v2(original())
        self.assertEqual(r["unity_host_adapter"], "NOT_IMPLEMENTED")
        self.assertEqual(r["unity_cold_save_load"], "NOT_RUN")

    def test_real_physx_not_run(self):
        self.assertEqual(audit_selected_v2(original())["native_physx_g1"], "NOT_RUN")

    def test_source_only_is_never_codex_ack(self):
        self.assertEqual(audit_selected_v2(original())["codex_ack"], "UNCONFIRMED")

    def test_no_g2_no_game_merge(self):
        r = audit_selected_v2(original())
        self.assertEqual(r["g2"], "HOLD")
        self.assertFalse(r["integrated"])

    def test_historical_red_is_separate(self):
        self.assertEqual(audit_selected_v2(original())["historical_rejected_sources"],
                         [544, 549, 554])

    def test_source_only_qa_570_closed_unmerged(self):
        x = original()
        self.assertFalse(x["independent_source_qa"][1]["merged"])
        self.assertEqual(x["independent_source_qa"][1]["pr_state"], "CLOSED_UNMERGED")

    def test_wrong_record_schema(self):
        self.deny(lambda d: d.update(schema="b01-w14-single-save-admission-v1"),
                  "unsupported W15")

    def test_absent_frozen_game_510(self):
        self.deny(lambda d: d.pop("gameplay"), "gameplay")

    def test_game_swapped_to_511(self):
        self.deny(lambda d: d["gameplay"].update(pr=511), "gameplay")

    def test_game_wrong_sha(self):
        self.deny(lambda d: d["gameplay"].update(sha="a"*40), "gameplay")

    def test_game_unfrozen(self):
        self.deny(lambda d: d["gameplay"].update(frozen=False), "gameplay")

    def test_save_replaced_by_historical_549(self):
        self.deny(lambda d: d["selected_save"].update(pr=549), "only frozen")

    def test_save_replaced_by_unselected_554(self):
        self.deny(lambda d: d["selected_save"].update(pr=554), "only frozen")

    def test_save_changed_sha(self):
        self.deny(lambda d: d["selected_save"].update(sha="a"*40), "only frozen")

    def test_save_source_unfrozen(self):
        self.deny(lambda d: d["selected_save"].update(frozen=False), "only frozen")

    def test_save_version_downshift(self):
        self.deny(lambda d: d["selected_save"].update(version="1.0.0"), "only frozen")

    def test_save_source_url_redirect(self):
        self.deny(lambda d: d["selected_save"].update(url="https://evil.invalid"), "only frozen")

    def test_save_targets_other_game(self):
        self.deny(lambda d: d["selected_save"].update(target_game_sha="b"*40), "only frozen")

    def test_missing_owner_choice(self):
        self.deny(lambda d: d.pop("owner_choice"), "owner_choice")

    def test_fake_b01_00_owner_selection(self):
        self.deny(lambda d: d["owner_choice"].update(agent="B01-00"), "exclusive")

    def test_owner_forged_selection_of_554(self):
        self.deny(lambda d: d["owner_choice"].update(pr=554), "exclusive")

    def test_owner_swapped_sha(self):
        self.deny(lambda d: d["owner_choice"].update(sha="b"*40), "exclusive")

    def test_duplicate_alternative_hold(self):
        self.deny(lambda d: d["alternatives_hold"].append(544), "alternative")

    def test_absent_historical_549_hold(self):
        self.deny(lambda d: d["alternatives_hold"].remove(549), "alternative")

    def test_unheld_554_alternative(self):
        self.deny(lambda d: d["alternatives_hold"].remove(554), "alternative")

    def test_failed_receiver_g0(self):
        self.deny(lambda d: d["physical_source_g0"].update(state="NOT_RUN"), "physical G0")

    def test_qa560_wrong_original_zip_hash(self):
        self.deny(lambda d: d["physical_source_g0"].update(inner_zip_sha256="a"*64),
                  "physical G0")

    def test_qa560_wrong_target_sha(self):
        self.deny(lambda d: d["physical_source_g0"].update(target_sha="a"*40),
                  "physical G0")

    def test_qa560_wrong_sha(self):
        self.deny(lambda d: d["physical_source_g0"].update(qa_sha="a"*40),
                  "physical G0")

    def test_qa560_fake_increase_archive_members(self):
        self.deny(lambda d: d["physical_source_g0"].update(members=35),
                  "physical G0")

    def test_wrong_qa_actor(self):
        self.deny(lambda d: d["independent_source_qa"][0].update(agent="B01-23"),
                  "QA SHA")

    def test_qa_producer_green_does_not_replace_independent_green(self):
        self.deny(lambda d: d["independent_source_qa"][0].update(result="PASS_PRODUCER"),
                  "QA SHA")

    def test_qa_wrong_source_sha(self):
        self.deny(lambda d: d["independent_source_qa"][0].update(target_sha="a"*40),
                  "QA SHA")

    def test_missing_primary_qa563(self):
        self.deny(lambda d: d["independent_source_qa"].pop(0), "two source QA")

    def test_duplicated_qa563_cannot_masquerade_570(self):
        def change(d):
            d["independent_source_qa"][1] = copy.deepcopy(d["independent_source_qa"][0])
        self.deny(change, "duplicate")

    def test_closed_570_not_merged(self):
        self.deny(lambda d: d["independent_source_qa"][1].update(merged=True),
                  "QA SHA")

    def test_closed_570_cannot_claim_open_draft(self):
        self.deny(lambda d: d["independent_source_qa"][1].update(pr_state="OPEN_DRAFT"),
                  "QA SHA")

    def test_qa563_wrong_source_commit(self):
        self.deny(lambda d: d["independent_source_qa"][0].update(qa_sha="0"*40),
                  "QA SHA")

    def test_qa_noncanonical_review_url(self):
        self.deny(lambda d: d["independent_source_qa"][0].update(url="https://evil.invalid"),
                  "QA SHA")

    def test_remove_historical_544_red(self):
        self.deny(lambda d: d["historical_rejections"].pop(0), "historical")

    def test_historical_red_forged_to_pass(self):
        self.deny(lambda d: d["historical_rejections"][0].update(status="PASS"),
                  "historical")

    def test_historical_550_blob_swapped(self):
        self.deny(lambda d: d.update(legacy_red_source_blob="f"*40),
                  "historical original")

    def test_fake_host_bridge_implementation(self):
        self.deny(lambda d: d["host_runtime"].update(adapter="IMPLEMENTED"),
                  "host gate promotion")

    def test_fake_unity_cold_load(self):
        self.deny(lambda d: d["host_runtime"].update(actual_unity_save_reload="PASS"),
                  "host gate promotion")

    def test_fake_physx_pass(self):
        self.deny(lambda d: d["host_runtime"].update(native_physx_g1="PASS"),
                  "host gate promotion")

    def test_fake_migration_done(self):
        self.deny(lambda d: d["host_runtime"].update(migration="DONE"),
                  "host gate promotion")

    def test_foreign_empty_directory_rollback_not_cleared(self):
        self.deny(lambda d: d["host_runtime"].update(original_510_rollback="ALLOWED"),
                  "host gate promotion")

    def test_safe_receiver_only_scratch(self):
        self.deny(lambda d: d["host_runtime"].update(receiver_rollback="LIVE_SAFE"),
                  "host gate promotion")

    def test_codex_ack_forgery(self):
        self.deny(lambda d: d["host_runtime"].update(codex_ack="ACK"),
                  "host gate promotion")

    def test_integrated_prematurely(self):
        self.deny(lambda d: d["host_runtime"].update(integrated=True),
                  "host gate promotion")

    def test_merged_prematurely(self):
        self.deny(lambda d: d["host_runtime"].update(merged=True),
                  "host gate promotion")

    def test_g2_false_green(self):
        self.deny(lambda d: d["host_runtime"].update(g2="PASS"),
                  "host gate promotion")

    def test_unrecognized_host_override(self):
        self.deny(lambda d: d["host_runtime"].update(skip_physx=True),
                  "host gate field")

    def test_unauthorized_adapter_package_assignment(self):
        self.deny(lambda d: d.update(adapter_package_id="B01-00-FAKE-ADAPTER"),
                  "no approved host bridge")

    def test_cli_selected_v2_real_fixture(self):
        capture = io.StringIO()
        with redirect_stdout(capture):
            self.assertEqual(selected_main([str(RECORD)]), 0)
        result = json.loads(capture.getvalue())
        self.assertEqual(result["verdict"], "SOURCE_G0_RECORDED_HOST_GATE_HOLD")
        self.assertFalse(result["release_allowed"])

    def test_cli_input_does_not_get_overwritten(self):
        with redirect_stderr(io.StringIO()):
            self.assertEqual(selected_main([str(RECORD), "--output", str(RECORD)]), 2)

    def test_cli_existing_file_never_overwritten(self):
        with tempfile.TemporaryDirectory() as td:
            outfile = Path(td)/"out.json"
            outfile.write_text("ORIGINAL")
            with redirect_stderr(io.StringIO()):
                self.assertEqual(selected_main([str(RECORD), "--output", str(outfile)]), 2)
            self.assertEqual(outfile.read_text(), "ORIGINAL")

    def test_cli_write_once_and_reject_second(self):
        with tempfile.TemporaryDirectory() as td:
            outfile = Path(td)/"out.json"
            with redirect_stderr(io.StringIO()):
                self.assertEqual(selected_main([str(RECORD), "--output", str(outfile)]), 0)
                self.assertEqual(selected_main([str(RECORD), "--output", str(outfile)]), 2)
            self.assertEqual(json.loads(outfile.read_text())["g2"], "HOLD")

    def test_cli_corrupted_json_fails_without_exception(self):
        with tempfile.TemporaryDirectory() as td:
            outfile = Path(td)/"bad.json"
            outfile.write_text("{broken")
            with redirect_stderr(io.StringIO()):
                self.assertEqual(selected_main([str(outfile)]), 2)


if __name__ == "__main__":
    unittest.main()
