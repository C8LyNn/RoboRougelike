import csv
import sys
import unittest
from pathlib import Path
sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from analyze_native_csv import SHA, FIELDS, ABBA, SIZES, SCENARIOS, InvalidEvidence, check_rows


def fixture(missing_metrics=False):
    # SYNTHETIC TEST FIXTURE ONLY — NEVER a Unity physics measurement or G1 proof.
    rows = []
    for n in SIZES:
        for scene in SCENARIOS:
            for order, mode in enumerate(ABBA):
                core = dict(record_type="FRAME", unity_version="TEST_FIXTURE_NOT_UNITY",
                            platform="Synthetic", r3_dependency_sha=SHA,
                            scenario=scene, parts=str(n), order=str(order),
                            variant=mode, frame="0", main_thread_ns="1000",
                            physics_simulate_ns="100", gc_alloc_bytes="0",
                            sleeping_count="", sleep_request_count="",
                            active_control_sleep_requests="", static_stays="",
                            body_stays="", safety="")
                for i in range(2):
                    row = dict(core)
                    row["frame"] = str(i)
                    if missing_metrics:
                        row["physics_simulate_ns"] = "NOT_MEASURED"
                    rows.append(row)
                summary = dict(core)
                summary.update(record_type="SUMMARY", frame="NOT_APPLICABLE",
                               main_thread_ns="NOT_APPLICABLE", physics_simulate_ns="NOT_APPLICABLE",
                               gc_alloc_bytes="NOT_APPLICABLE", sleeping_count="1",
                               sleep_request_count=("1" if mode == "R3_CANDIDATE" else "0"),
                               active_control_sleep_requests="0", static_stays="1",
                               body_stays="1", safety="PASS")
                rows.append(summary)
    return rows


class NativeEvidenceGateTests(unittest.TestCase):
    def test_synthetic_well_formed_source_contract(self):
        r=check_rows(fixture(), min_frames=2)
        self.assertEqual(r["frame_sessions"], 24)
        self.assertEqual(len(r["pairs"]), 6)
        self.assertNotEqual(r["status"], "G1_PASS")

    def test_marker_unavailable_never_makes_up_numbers(self):
        r=check_rows(fixture(missing_metrics=True), min_frames=2)
        self.assertTrue(all(x["metrics"]["physics_simulate_ns"]["status"]=="NOT_MEASURED"
                            for x in r["pairs"]))

    def test_one_missing_run_fails_closed(self):
        rows=fixture()
        with self.assertRaises(InvalidEvidence):
            check_rows(rows[:-3], min_frames=2)

    def test_one_duplicate_frame_fails_closed(self):
        rows=fixture()
        rows[1]["frame"]="0"
        with self.assertRaises(InvalidEvidence):
            check_rows(rows, min_frames=2)

    def test_mismatched_r3_sha_fails_closed(self):
        rows=fixture()
        rows[0]["r3_dependency_sha"]="foreign"
        with self.assertRaises(InvalidEvidence):
            check_rows(rows, min_frames=2)

    def test_active_body_forced_sleep_fails_closed(self):
        rows=fixture()
        next(r for r in rows if r["record_type"]=="SUMMARY")["active_control_sleep_requests"]="1"
        with self.assertRaises(InvalidEvidence):
            check_rows(rows, min_frames=2)

    def test_unobserved_dynamic_contacts_inconclusive(self):
        rows=fixture()
        next(r for r in rows if r["record_type"]=="SUMMARY" and r["scenario"]=="DYNAMIC_CONTACT")["body_stays"]="0"
        with self.assertRaises(InvalidEvidence):
            check_rows(rows, min_frames=2)

    def test_zero_baseline_does_not_invent_infinite_savings(self):
        rows=fixture()
        for r in rows:
            if r["record_type"]=="FRAME" and r["variant"]=="UNITY_AUTO_BASELINE":
                r["main_thread_ns"]="0"
        result=check_rows(rows,min_frames=2)
        self.assertTrue(all(x["metrics"]["main_thread_ns"]["delta_percent"] is None
                            for x in result["pairs"]))

    def test_wrong_abba_baseline_order_fails_closed(self):
        rows=fixture()
        rows[0]["variant"]="R3_CANDIDATE"
        with self.assertRaises(InvalidEvidence):
            check_rows(rows,min_frames=2)

    def test_incomplete_safety_flag_fails_closed(self):
        rows=fixture()
        next(r for r in rows if r["record_type"]=="SUMMARY")["safety"]="INCONCLUSIVE"
        with self.assertRaises(InvalidEvidence):
            check_rows(rows,min_frames=2)

if __name__ == "__main__":
    unittest.main(verbosity=2)
