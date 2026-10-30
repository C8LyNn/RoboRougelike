"""Original W16 source-only admission tests: exact RED vs stale QA, no fake G0.

No native Unity, networking, source writes or Codex ACK. Physical source SHAs
were independently read from GitHub before the fixture was authored.
"""
from contextlib import redirect_stdout, redirect_stderr
import copy
import io
import json
from pathlib import Path
import tempfile
import unittest

from save_admission import AdmissionError, audit_bridge_wave16, bridge_main

REAL = Path(__file__).with_name("bridge_w16_hold.json")


def fixture():
    return json.loads(REAL.read_text(encoding="utf-8"))


class BridgeW16Gate(unittest.TestCase):
    def blocked(self, change, reason):
        d = fixture()
        change(d)
        with self.assertRaisesRegex(AdmissionError, reason):
            audit_bridge_wave16(d)

    def test_real_unselected_producers_hold(self):
        out = audit_bridge_wave16(fixture())
        self.assertEqual(out["verdict"], "HOLD_NO_SELECTED_REPAIRED_BRIDGE")
        self.assertIsNone(out["selected_source"])
        self.assertFalse(out["merge_authorized"])

    def test_585_red_is_exact_original_head(self):
        out = audit_bridge_wave16(fixture())
        self.assertEqual(out["source_statuses"]["585"],
                         "HOLD_CURRENT_EXACT_SOURCE_SECURITY_RED")

    def test_587_qa_not_same_sha_or_blob(self):
        out = audit_bridge_wave16(fixture())
        self.assertEqual(out["source_statuses"]["587"],
                         "HOLD_CHANGED_CORE_NEW_EXACT_SHA_QA_REQUIRED")

    def test_producer_ci_green_does_not_cure_585_red(self):
        self.assertEqual(fixture()["candidates"][0]["producer_ci"], "PASS_SOURCE_ONLY")
        self.assertEqual(audit_bridge_wave16(fixture())["source_g0"], "HOLD")

    def test_source_only_cannot_authenticate_qa(self):
        self.assertFalse(audit_bridge_wave16(fixture())["external_review_identity_authenticated"])

    def test_source_only_never_runs_physx(self):
        self.assertEqual(audit_bridge_wave16(fixture())["native_unity_g1"], "NOT_RUN")

    def test_source_only_never_claims_host_save(self):
        self.assertEqual(audit_bridge_wave16(fixture())["live_game_save_load"], "HOLD")

    def test_no_fake_ack_g2(self):
        r = audit_bridge_wave16(fixture())
        self.assertEqual(r["codex_ack"], "UNCONFIRMED")
        self.assertEqual(r["g2"], "HOLD")
        self.assertFalse(r["integrated"])

    def test_select_587_owner_does_not_override_qa_gap(self):
        d = fixture()
        d["owner_selection"].update(state="SELECTED_ONE", pr=587,
                                    sha=d["candidates"][1]["sha"],
                                    decision_url="https://github.com/C8LyNn/RoboRougelike/issues/589#issuecomment-99999999")
        r = audit_bridge_wave16(d)
        self.assertEqual(r["selected_source"], 587)
        self.assertEqual(r["source_g0"], "HOLD")
        self.assertEqual(r["verdict"], "HOLD_NO_SELECTED_REPAIRED_BRIDGE")

    def test_select_585_owner_still_red(self):
        d = fixture()
        d["owner_selection"].update(state="SELECTED_ONE", pr=585,
                                    sha=d["candidates"][0]["sha"],
                                    decision_url="https://github.com/C8LyNn/RoboRougelike/issues/589#issuecomment-99999999")
        r = audit_bridge_wave16(d)
        self.assertEqual(r["source_statuses"]["585"], "HOLD_CURRENT_EXACT_SOURCE_SECURITY_RED")
        self.assertEqual(r["source_g0"], "HOLD")

    def test_schema_downgrade(self):
        self.blocked(lambda d: d.update(schema="b01-w15-selected-save-host-gate-v1"),
                     "W16 schema")

    def test_frozen_gameplay_sha_drift(self):
        self.blocked(lambda d: d["frozen"].update(game_sha="a"*40), "frozen")

    def test_frozen_save_sha_drift(self):
        self.blocked(lambda d: d["frozen"].update(save_sha="a"*40), "frozen")

    def test_false_mutable_frozen_source(self):
        self.blocked(lambda d: d["frozen"].update(frozen=False), "frozen")

    def test_alternative_game_511(self):
        self.blocked(lambda d: d["frozen"].update(game_pr=511), "frozen")

    def test_alternative_save_554(self):
        self.blocked(lambda d: d["frozen"].update(save_pr=554), "frozen")

    def test_remove_one_candidate(self):
        self.blocked(lambda d: d["candidates"].pop(), "exactly two")

    def test_duplicate_585_candidate(self):
        self.blocked(lambda d: d["candidates"].__setitem__(1,copy.deepcopy(d["candidates"][0])),
                     "duplicate")

    def test_third_source_577_illegal(self):
        self.blocked(lambda d: d["candidates"].append(copy.deepcopy(d["candidates"][0])),
                     "exactly two")

    def test_585_wrong_current_source_blob(self):
        self.blocked(lambda d: d["candidates"][0].update(core_blob_sha="f"*40), "source SHA")

    def test_587_wrong_current_source_commit(self):
        self.blocked(lambda d: d["candidates"][1].update(sha="f"*40), "source SHA")

    def test_fake_585_green_G0(self):
        self.blocked(lambda d: d["candidates"][0].update(source_g0="ACCEPTED_SOURCE_ONLY"),
                     "source SHA")

    def test_fake_587_green_G0(self):
        self.blocked(lambda d: d["candidates"][1].update(source_g0="ACCEPTED_SOURCE_ONLY"),
                     "source SHA")

    def test_bad_587_producer_ci(self):
        self.blocked(lambda d: d["candidates"][1].update(producer_ci="UNITY_PASS"),
                     "source SHA")

    def test_585_QA_572_wrong_commit(self):
        self.blocked(lambda d: d["candidates"][0]["independent_qa"].update(tested_commit="f"*40),
                     "independent QA")

    def test_587_QA_588_not_current_head(self):
        d = fixture()
        self.assertNotEqual(d["candidates"][1]["sha"], d["candidates"][1]["independent_qa"]["tested_commit"])
        self.assertNotEqual(d["candidates"][1]["core_blob_sha"],
                            d["candidates"][1]["independent_qa"]["tested_core_blob"])

    def test_no_relabel_588_as_current_sha(self):
        self.blocked(lambda d: d["candidates"][1]["independent_qa"].update(
            tested_commit=d["candidates"][1]["sha"]), "independent QA")

    def test_no_relabel_588_as_current_blob(self):
        self.blocked(lambda d: d["candidates"][1]["independent_qa"].update(
            tested_core_blob=d["candidates"][1]["core_blob_sha"]), "independent QA")

    def test_no_fake_QA_PASS_from_red_witness(self):
        self.blocked(lambda d: d["candidates"][0]["independent_qa"].update(
            verdict="RELEASE_PASS"), "independent QA")

    def test_port_577_red_not_relabelled_587(self):
        self.blocked(lambda d: d["noncanonical_qa"].update(tested_port_pr=587),
                     "port RED")

    def test_abort_throw_quarantine_not_dropped(self):
        self.blocked(lambda d: d["noncanonical_qa"].update(
            defects=["SAME_GENERATION_WORLD_FORK"]), "port RED")

    def test_port_577_cannot_be_selected_third_candidate(self):
        self.blocked(lambda d: d["noncanonical_qa"].update(port_state="SELECTED"),
                     "port RED")

    def test_alt_577_not_staged(self):
        self.blocked(lambda d: d["alternatives_hold"].remove(577), "alternatives")

    def test_alt_585_not_excluded_by_default(self):
        self.assertNotIn(585, fixture()["alternatives_hold"])

    def test_owner_B01_00_cannot_choose_587(self):
        self.blocked(lambda d: d["owner_selection"].update(actor="B01-00"),
                     "only B01-23")

    def test_undecided_with_spoofed_SHA(self):
        self.blocked(lambda d: d["owner_selection"].update(sha="f"*40),
                     "undecided")

    def test_undecided_with_spoofed_pr(self):
        self.blocked(lambda d: d["owner_selection"].update(pr=587),
                     "undecided")

    def test_selected_without_real_decision_link(self):
        self.blocked(lambda d: d["owner_selection"].update(
            state="SELECTED_ONE",pr=587,sha=d["candidates"][1]["sha"],
            decision_url=None), "decision")

    def test_selected_with_wrong_sha(self):
        self.blocked(lambda d: d["owner_selection"].update(
            state="SELECTED_ONE",pr=587,sha="f"*40,
            decision_url="https://github.com/C8LyNn/RoboRougelike/issues/589#issuecomment-99999"),
            "decision")

    def test_selected_with_wrong_domain(self):
        self.blocked(lambda d: d["owner_selection"].update(
            state="SELECTED_ONE",pr=587,sha=d["candidates"][1]["sha"],
            decision_url="https://evil.invalid/issues/589#issuecomment-99999"), "decision")

    def test_false_unity_g1(self):
        self.blocked(lambda d: d["release_gates"].update(unity_g1="PASS"),
                     "premature")

    def test_false_live_save(self):
        self.blocked(lambda d: d["release_gates"].update(canonical_save_reload="PASS"),
                     "premature")

    def test_false_phys_receiver(self):
        self.blocked(lambda d: d["release_gates"].update(physical_receiver="ACCEPTED"),
                     "premature")

    def test_false_canonical_integration(self):
        self.blocked(lambda d: d["release_gates"].update(canonical_game_host="INTEGRATED"),
                     "premature")

    def test_fake_rollback_success(self):
        self.blocked(lambda d: d["release_gates"].update(rollback="LIVE_APPROVED"),
                     "premature")

    def test_unsafe_original_510_rollback(self):
        self.blocked(lambda d: d["release_gates"].update(original_510_rollback="ALLOWED"),
                     "premature")

    def test_fake_ack(self):
        self.blocked(lambda d: d["release_gates"].update(codex_ack="YES"),
                     "premature")

    def test_fake_release_G2(self):
        self.blocked(lambda d: d["release_gates"].update(g2="PASS"),
                     "premature")

    def test_fake_merge(self):
        self.blocked(lambda d: d["release_gates"].update(merged=True),
                     "premature")

    def test_missing_release_gate(self):
        self.blocked(lambda d: d["release_gates"].pop("migration"),
                     "release gate")

    def test_extra_unsafe_skip_field(self):
        self.blocked(lambda d: d["release_gates"].update(skip_qa=True),
                     "release gate")

    def test_cli_real_w16_source_hold(self):
        output = io.StringIO()
        with redirect_stdout(output):
            self.assertEqual(bridge_main([str(REAL)]), 0)
        result = json.loads(output.getvalue())
        self.assertEqual(result["source_g0"], "HOLD")
        self.assertFalse(result["external_review_identity_authenticated"])

    def test_cli_strict_hard_gate_exit_two(self):
        with redirect_stdout(io.StringIO()):
            self.assertEqual(bridge_main([str(REAL), "--require-accepted-source"]), 2)

    def test_cli_never_overwrite_input(self):
        with redirect_stderr(io.StringIO()):
            self.assertEqual(bridge_main([str(REAL), "--output", str(REAL)]), 2)

    def test_cli_never_overwrite_external_output(self):
        with tempfile.TemporaryDirectory() as td:
            p = Path(td) / "existing"
            p.write_text("FOREIGN")
            with redirect_stderr(io.StringIO()):
                self.assertEqual(bridge_main([str(REAL), "--output", str(p)]), 2)
            self.assertEqual(p.read_text(), "FOREIGN")

    def test_cli_write_once_then_refuse_repeat(self):
        with tempfile.TemporaryDirectory() as td:
            p = Path(td) / "output"
            with redirect_stderr(io.StringIO()):
                self.assertEqual(bridge_main([str(REAL), "--output", str(p)]), 0)
                self.assertEqual(bridge_main([str(REAL), "--output", str(p)]), 2)
            self.assertEqual(json.loads(p.read_text())["g2"], "HOLD")

    def test_corrupt_receipt_json_fails_closed(self):
        with tempfile.TemporaryDirectory() as td:
            p = Path(td) / "bad.json"
            p.write_text("{invalid")
            with redirect_stderr(io.StringIO()):
                self.assertEqual(bridge_main([str(p)]), 2)


if __name__ == "__main__":
    unittest.main()
