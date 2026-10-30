import copy
import sys
from pathlib import Path
import unittest
sys.path.insert(0,str(Path(__file__).resolve().parents[1]))
from cilatency import analyze, EvidenceError, stats

def fixture():
    return {"schema":"b0150-cilatency-snapshot-v1","repository":"C8LyNn/RoboRougelike",
            "source":"GitHub REST explicit synthetic test fixture",
            "workflow_runs":[{"id":42,"name":"source test workflow","head_sha":"a"*40,
            "status":"completed","conclusion":"success","run_attempt":1,
            "created_at":"2026-10-30T12:00:00Z","updated_at":"2026-10-30T12:01:00Z",
            "jobs":[{"id":111,"run_id":42,"name":"source (ubuntu-24.04)",
                     "status":"completed","conclusion":"success",
                     "created_at":"2026-10-30T12:00:02Z",
                     "started_at":"2026-10-30T12:00:12Z",
                     "completed_at":"2026-10-30T12:00:25Z"}]}]}

class TestSource(unittest.TestCase):
    def invalid(self,mutate):
        data=fixture()
        mutate(data)
        with self.assertRaises(EvidenceError):
            analyze(data)

    def test_queue_execution_workflow_separate(self):
        r=analyze(fixture())
        self.assertEqual((r["jobs"][0]["wait_until_start_seconds"],r["jobs"][0]["execution_seconds"],
                          r["runs"][0]["created_to_last_update_seconds"]),(10,13,60))

    def test_nearest_rank(self):
        self.assertEqual(stats([1,7,3,5])["median_s"],3)
        self.assertEqual(stats([1,7,3,5])["p95_nearest_rank_s"],7)

    def test_does_not_claim_native_or_codex(self):
        r=analyze(fixture())
        self.assertEqual(r["unity_native_g1"],"NOT_ASSESSED_BY_ACTIONS_LATENCY")
        self.assertEqual(r["integrator_accepted"],"NOT_OBSERVED")

    def test_wrong_repo(self): self.invalid(lambda d:d.update(repository="Other/Repo"))
    def test_wrong_schema(self): self.invalid(lambda d:d.update(schema="broken"))
    def test_missing_provenance(self): self.invalid(lambda d:d.update(source="unknown"))
    def test_empty_runs(self): self.invalid(lambda d:d.update(workflow_runs=[]))
    def test_empty_jobs(self): self.invalid(lambda d:d["workflow_runs"][0].update(jobs=[]))
    def test_invalid_sha(self): self.invalid(lambda d:d["workflow_runs"][0].update(head_sha="abc"))
    def test_workflow_not_complete(self): self.invalid(lambda d:d["workflow_runs"][0].update(status="queued"))
    def test_job_not_complete(self): self.invalid(lambda d:d["workflow_runs"][0]["jobs"][0].update(status="in_progress"))
    def test_false_job_run_id(self): self.invalid(lambda d:d["workflow_runs"][0]["jobs"][0].update(run_id=41))
    def test_reversed_timestamps(self): self.invalid(lambda d:d["workflow_runs"][0]["jobs"][0].update(started_at="2026-10-30T12:00:01Z"))
    def test_job_after_workflow_updated(self): self.invalid(lambda d:d["workflow_runs"][0]["jobs"][0].update(completed_at="2026-10-30T12:02:00Z"))
    def test_impossible_date(self): self.invalid(lambda d:d["workflow_runs"][0].update(created_at="2026-02-30T12:00:00Z"))
    def test_duplicate_run(self): self.invalid(lambda d:d["workflow_runs"].append(copy.deepcopy(d["workflow_runs"][0])))
    def test_duplicate_job(self):
        def mutate(d):
            j=d["workflow_runs"][0]["jobs"]
            j.append(copy.deepcopy(j[0]))
        self.invalid(mutate)
    def test_boolean_run_id(self): self.invalid(lambda d:d["workflow_runs"][0].update(id=True))
    def test_invalid_attempt(self): self.invalid(lambda d:d["workflow_runs"][0].update(run_attempt=0))
    def test_workflow_failure_not_evidence_of_native(self):
        d=fixture()
        d["workflow_runs"][0]["conclusion"]="failure"
        d["workflow_runs"][0]["jobs"][0]["conclusion"]="failure"
        r=analyze(d)
        self.assertEqual(r["run_conclusions"],{"failure":1})
        self.assertEqual(r["game_fps_gpu"],"NOT_MEASURED")
