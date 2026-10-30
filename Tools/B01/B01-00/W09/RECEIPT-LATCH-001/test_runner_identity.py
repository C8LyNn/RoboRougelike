"""W12 independent producer regression: job names are NOT runner OS proof.

These tests use GitHub's real Actions run/job response field names. The
actual separate live network verification is executed by the workflow CI.
"""
import copy
import unittest

from github_live import verify_live
from test_workflow_contract import secure_case, RUN_PATH, JOBS_PATH
from test_github_live import read_fixture


def legitimate():
    receipt, objects = secure_case()
    receipt["producer"]["ci"]["contract"]["required_runner_labels"] = {
        "dispatch (windows-2025)": "windows-2025",
        "dispatch (ubuntu-24.04)": "ubuntu-24.04",
    }
    objects[RUN_PATH]["run_attempt"] = 1
    for job, label in zip(objects[JOBS_PATH]["jobs"], ("windows-2025", "ubuntu-24.04")):
        job["labels"] = [label]
        job["run_id"] = 41000000001
        job["run_attempt"] = 1
    return receipt, objects


class RunnerIdentityW12(unittest.TestCase):
    def check_hold(self, mutate, expected):
        r, docs = legitimate()
        mutate(r, docs)
        result = verify_live(r, read_fixture(docs))
        self.assertFalse(result["source_workflow_jobs_verified"], result)
        self.assertFalse(result["source_git_objects_verified"], result)
        self.assertIn(expected, result["blockers"])
        self.assertEqual(result["decision"], "HOLD")

    def test_valid_two_actual_runner_labels_and_run_attempts(self):
        receipt, docs = legitimate()
        result = verify_live(receipt, read_fixture(docs))
        self.assertTrue(result["source_workflow_jobs_verified"])
        self.assertEqual(result["runner_identity_proof"], "RUNNER_LABELS_AND_ATTEMPT_MATCH")

    def test_named_windows_job_actually_ran_on_ubuntu(self):
        self.check_hold(lambda r, d: d[JOBS_PATH]["jobs"][0].update(labels=["ubuntu-24.04"]),
                        "live_runner_label_mismatch")

    def test_named_ubuntu_job_actually_ran_on_windows(self):
        self.check_hold(lambda r, d: d[JOBS_PATH]["jobs"][1].update(labels=["windows-2025"]),
                        "live_runner_label_mismatch")

    def test_both_jobs_actually_same_os(self):
        def switch(r, d):
            d[JOBS_PATH]["jobs"][1]["labels"] = ["windows-2025"]
        self.check_hold(switch, "live_runner_label_mismatch")

    def test_absent_runner_labels(self):
        self.check_hold(lambda r, d: d[JOBS_PATH]["jobs"][0].pop("labels"),
                        "live_runner_label_mismatch")

    def test_empty_runner_labels(self):
        self.check_hold(lambda r, d: d[JOBS_PATH]["jobs"][0].update(labels=[]),
                        "live_runner_label_mismatch")

    def test_malformed_string_runner_labels(self):
        self.check_hold(lambda r, d: d[JOBS_PATH]["jobs"][1].update(labels="ubuntu-24.04"),
                        "live_runner_label_mismatch")

    def test_unrecognized_runner_labels(self):
        self.check_hold(lambda r, d: d[JOBS_PATH]["jobs"][0].update(labels=["self-hosted"]),
                        "live_runner_label_mismatch")

    def test_missing_runner_contract_fail_closed(self):
        self.check_hold(lambda r, d: r["producer"]["ci"]["contract"].pop("required_runner_labels"),
                        "source_workflow_contract_invalid")

    def test_malformed_runner_contract_extra_os(self):
        def change(r, d):
            r["producer"]["ci"]["contract"]["required_runner_labels"]["extra"] = "ubuntu-24.04"
        self.check_hold(change, "source_workflow_contract_invalid")

    def test_malformed_runner_contract_duplicate_os(self):
        def change(r, d):
            r["producer"]["ci"]["contract"]["required_runner_labels"]["dispatch (ubuntu-24.04)"] = "windows-2025"
        self.check_hold(change, "source_workflow_contract_invalid")

    def test_unknown_runner_label_in_contract(self):
        def change(r, d):
            r["producer"]["ci"]["contract"]["required_runner_labels"]["dispatch (ubuntu-24.04)"] = "danger"
        self.check_hold(change, "source_workflow_contract_invalid")

    def test_job_from_different_run(self):
        self.check_hold(lambda r, d: d[JOBS_PATH]["jobs"][1].update(run_id=1234567),
                        "live_job_run_or_attempt_mismatch")

    def test_run_attempt_replayed_from_older_rerun(self):
        self.check_hold(lambda r, d: d[JOBS_PATH]["jobs"][0].update(run_attempt=0),
                        "live_job_run_or_attempt_mismatch")

    def test_run_attempt_replayed_from_future_rerun(self):
        self.check_hold(lambda r, d: d[JOBS_PATH]["jobs"][0].update(run_attempt=2),
                        "live_job_run_or_attempt_mismatch")

    def test_missing_run_attempt_metadata(self):
        self.check_hold(lambda r, d: d[RUN_PATH].pop("run_attempt"),
                        "live_run_attempt_invalid")

    def test_run_attempt_string_not_integer(self):
        self.check_hold(lambda r, d: d[RUN_PATH].update(run_attempt="1"),
                        "live_run_attempt_invalid")

    def test_attempt_bool_not_valid_int(self):
        self.check_hold(lambda r, d: d[RUN_PATH].update(run_attempt=True),
                        "live_run_attempt_invalid")

    def test_missing_job_run_id(self):
        self.check_hold(lambda r, d: d[JOBS_PATH]["jobs"][0].pop("run_id"),
                        "live_job_run_or_attempt_mismatch")

    def test_missing_job_run_attempt(self):
        self.check_hold(lambda r, d: d[JOBS_PATH]["jobs"][0].pop("run_attempt"),
                        "live_job_run_or_attempt_mismatch")

    def test_duplicate_job_ids_rejected(self):
        def change(r, d):
            d[JOBS_PATH]["jobs"][1]["id"] = d[JOBS_PATH]["jobs"][0]["id"]
        self.check_hold(change, "live_job_duplicate_id")

    def test_pr_run_id_and_source_correct_but_job_from_wrong_run(self):
        def change(r, d):
            d[JOBS_PATH]["jobs"][0]["run_id"] = 777
            d[JOBS_PATH]["jobs"][1]["run_id"] = 777
        self.check_hold(change, "live_job_run_or_attempt_mismatch")


if __name__ == "__main__":
    unittest.main()
