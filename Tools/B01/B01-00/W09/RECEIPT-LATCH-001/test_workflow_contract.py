"""W11 RED-to-GREEN: a same-SHA green workflow must not impersonate source tests.

No network calls in this suite. Test W10 github_live.py using real GitHub
REST run/job/step response shapes derived from pinned B01-00 Actions #41046087105.
"""
import copy
import unittest
from github_live import GateError, verify_live, fetch_github
from test_receipt_latch import make_record, SHA
from test_github_live import original_objects, read_fixture


RUN_PATH = "/actions/runs/41000000001"
JOBS_PATH = RUN_PATH + "/jobs"
WORKFLOW_ID = 401553591
WORKFLOW_PATH = ".github/workflows/b01-00-dispatch-w06.yml"
STEP_TEST = "Original source full dispatch and freeze regression suite"
STEP_PLAN = "Produce deterministic example plan"


def secure_case():
    r = make_record()
    r["producer"]["ci"]["contract"] = {
        "workflow_id": WORKFLOW_ID,
        "workflow_path": WORKFLOW_PATH,
        "event": "pull_request",
        "required_job_names": ["dispatch (windows-2025)", "dispatch (ubuntu-24.04)"],
        "required_steps": [STEP_TEST, STEP_PLAN],
    }
    docs = original_objects()
    docs[RUN_PATH].update({
        "workflow_id": WORKFLOW_ID, "path": WORKFLOW_PATH,
        "event": "pull_request", "run_attempt": 1
    })
    docs[JOBS_PATH] = {
        "total_count": 2, "jobs": [
            {"id": 101, "name": "dispatch (windows-2025)", "head_sha": SHA,
             "status": "completed", "conclusion": "success",
             "steps": [{"name": STEP_TEST, "status": "completed", "conclusion": "success"},
                       {"name": STEP_PLAN, "status": "completed", "conclusion": "success"}]},
            {"id": 102, "name": "dispatch (ubuntu-24.04)", "head_sha": SHA,
             "status": "completed", "conclusion": "success",
             "steps": [{"name": STEP_TEST, "status": "completed", "conclusion": "success"},
                       {"name": STEP_PLAN, "status": "completed", "conclusion": "success"}]},
        ],
    }
    return r, docs


class W11WorkflowIdentity(unittest.TestCase):
    def check(self, change, expected_block):
        r, docs = secure_case()
        change(r, docs)
        out = verify_live(r, read_fixture(docs))
        self.assertFalse(out["source_git_objects_verified"], out)
        self.assertIn(expected_block, out["blockers"])
        self.assertEqual(out["decision"], "HOLD")

    def test_valid_immutable_source_and_both_platform_jobs(self):
        r, docs = secure_case()
        out = verify_live(r, read_fixture(docs))
        self.assertTrue(out["source_workflow_jobs_verified"])
        self.assertTrue(out["source_git_objects_verified"])
        self.assertEqual(out["source_gates_label"], "ORIGINAL_WORKFLOW_AND_JOBS_MATCH")

    def test_wrong_successful_workflow_id(self):
        self.check(lambda r, d: d[RUN_PATH].update(workflow_id=88), "live_workflow_identity_mismatch")

    def test_wrong_workflow_file(self):
        self.check(lambda r, d: d[RUN_PATH].update(path=".github/workflows/ci-01.yml"),
                   "live_workflow_identity_mismatch")

    def test_wrong_event_push(self):
        self.check(lambda r, d: d[RUN_PATH].update(event="push"), "live_workflow_identity_mismatch")

    def test_wrong_scheduled_event(self):
        self.check(lambda r, d: d[RUN_PATH].update(event="schedule"), "live_workflow_identity_mismatch")

    def test_missing_workflow_identity_contract(self):
        self.check(lambda r, d: r["producer"]["ci"].pop("contract"), "source_workflow_contract_missing")

    def test_contract_missing_required_steps(self):
        self.check(lambda r, d: r["producer"]["ci"]["contract"].pop("required_steps"),
                   "source_workflow_contract_invalid")

    def test_contract_empty_job_set(self):
        self.check(lambda r, d: r["producer"]["ci"]["contract"].update(required_job_names=[]),
                   "source_workflow_contract_invalid")

    def test_contract_duplicate_required_job_name(self):
        self.check(lambda r, d: r["producer"]["ci"]["contract"].update(
            required_job_names=["dispatch (windows-2025)"]*2),
                   "source_workflow_contract_invalid")

    def test_job_one_platform_only(self):
        def defect(r, d):
            d[JOBS_PATH]["jobs"].pop()
            d[JOBS_PATH]["total_count"] = 1
        self.check(defect, "live_required_job_missing")

    def test_job_missing_required_step(self):
        def defect(r, d):
            d[JOBS_PATH]["jobs"][0]["steps"].pop()
        self.check(defect, "live_required_step_missing_or_fail")

    def test_job_failed_required_step(self):
        def defect(r, d):
            d[JOBS_PATH]["jobs"][0]["steps"][0]["conclusion"] = "failure"
        self.check(defect, "live_required_step_missing_or_fail")

    def test_job_step_skipped(self):
        def defect(r, d):
            d[JOBS_PATH]["jobs"][0]["steps"][0]["conclusion"] = "skipped"
        self.check(defect, "live_required_step_missing_or_fail")

    def test_job_other_sha(self):
        self.check(lambda r, d: d[JOBS_PATH]["jobs"][1].update(head_sha="b"*40),
                   "live_job_sha_or_result_mismatch")

    def test_job_not_completed(self):
        self.check(lambda r, d: d[JOBS_PATH]["jobs"][1].update(status="in_progress"),
                   "live_job_sha_or_result_mismatch")

    def test_job_conclusion_failure(self):
        self.check(lambda r, d: d[JOBS_PATH]["jobs"][0].update(conclusion="failure"),
                   "live_job_sha_or_result_mismatch")

    def test_duplicate_job_names(self):
        self.check(lambda r, d: d[JOBS_PATH]["jobs"][1].update(name="dispatch (windows-2025)"),
                   "live_job_duplicate_name")

    def test_workflow_jobs_unavailable(self):
        r, docs = secure_case();del docs[JOBS_PATH]
        o=verify_live(r,read_fixture(docs))
        self.assertFalse(o["source_git_objects_verified"])
        self.assertIn("live_workflow_jobs_unavailable",o["blockers"])

    def test_api_partial_page_must_hold(self):
        self.check(lambda r,d:d[JOBS_PATH].update(total_count=31),"live_workflow_jobs_incomplete")

    def test_job_list_missing(self):
        self.check(lambda r,d:d[JOBS_PATH].pop("jobs"),"live_workflow_jobs_invalid")

    def test_job_list_not_array(self):
        self.check(lambda r,d:d[JOBS_PATH].update(jobs={"not":"a list"}),"live_workflow_jobs_invalid")

    def test_ci_workflow_id_string_rejected(self):
        self.check(lambda r,d:r["producer"]["ci"]["contract"].update(workflow_id="401553591"),
                   "source_workflow_contract_invalid")

    def test_jobs_endpoint_whitelist_accepts_only_fixed_repo(self):
        class Response:
            status=200
            def geturl(self):
                return "https://api.github.com/repos/C8LyNn/RoboRougelike"+JOBS_PATH
            def read(self,n):
                return b'{"total_count":0,"jobs":[]}'
            def __enter__(self):return self
            def __exit__(self,*a):pass
        class Opener:
            def open(self,request,timeout):
                self.url=request.full_url
                return Response()
        o=Opener()
        result=fetch_github(JOBS_PATH,opener=o)
        self.assertEqual(result["total_count"],0)
        self.assertTrue(o.url.endswith(JOBS_PATH))

    def test_jobs_endpoint_rejects_query_injection(self):
        with self.assertRaises(GateError):
            fetch_github(JOBS_PATH+"?per_page=999&token=secret")

    def test_workflow_success_without_test_jobs_stays_hold(self):
        def defect(r,d):
            d[JOBS_PATH]["jobs"]=[
                {"id":222,"name":"lint (ubuntu-24.04)","head_sha":SHA,"status":"completed",
                 "conclusion":"success","steps":[{"name":"Lint","status":"completed","conclusion":"success"}]}]
            d[JOBS_PATH]["total_count"]=1
        self.check(defect, "live_required_job_missing")


if __name__ == "__main__":
    unittest.main()
