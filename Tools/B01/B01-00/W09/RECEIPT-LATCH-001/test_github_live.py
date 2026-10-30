"""W10 original source tests; no actual HTTP performed in CI."""
import copy
from contextlib import redirect_stdout, redirect_stderr
import io
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

from github_live import (API, GateError, comment_id, fetch_github, main,
                         run_id, sha_mentioned, verify_live)
from test_receipt_latch import make_record, REPO, SHA, SHA_ALT


def original_objects():
    r = make_record()
    prno = r["producer"]["source_pr"]
    run = 41000000001
    return {
        "/pulls/" + str(prno): {
            "number": prno, "html_url": REPO + "pull/" + str(prno),
            "base": {"repo": {"full_name": "C8LyNn/RoboRougelike"}},
            "head": {"repo": {"full_name": "C8LyNn/RoboRougelike"}, "sha": SHA},
        },
        "/actions/runs/" + str(run): {
            "id": run, "html_url": REPO + "actions/runs/" + str(run),
            "head_sha": SHA, "status": "completed", "conclusion": "success",
            "repository": {"full_name": "C8LyNn/RoboRougelike"},
            "workflow_id": 401553591,
            "path": ".github/workflows/b01-00-dispatch-w06.yml",
            "event": "pull_request",
            "run_attempt": 1,
        },
        "/actions/runs/" + str(run) + "/jobs": {
            "total_count": 2, "jobs": [
                {"id": 101, "name": "dispatch (windows-2025)", "head_sha": SHA,
                 "labels": ["windows-2025"], "run_id": run, "run_attempt": 1,
                 "status": "completed", "conclusion": "success",
                 "steps": [
                     {"name": "Original source full dispatch and freeze regression suite",
                      "status": "completed", "conclusion": "success"},
                     {"name": "Produce deterministic example plan",
                      "status": "completed", "conclusion": "success"},
                 ]},
                {"id": 102, "name": "dispatch (ubuntu-24.04)", "head_sha": SHA,
                 "labels": ["ubuntu-24.04"], "run_id": run, "run_attempt": 1,
                 "status": "completed", "conclusion": "success",
                 "steps": [
                     {"name": "Original source full dispatch and freeze regression suite",
                      "status": "completed", "conclusion": "success"},
                     {"name": "Produce deterministic example plan",
                      "status": "completed", "conclusion": "success"},
                 ]},
            ],
        },
    }


def read_fixture(objects):
    def getter(path):
        if path not in objects:
            raise GateError("missing")
        return copy.deepcopy(objects[path])
    return getter


class RealObjectGate(unittest.TestCase):
    def test_live_pr_and_ci_match_only_external_review_candidate(self):
        x = verify_live(make_record(), read_fixture(original_objects()))
        self.assertTrue(x["source_git_objects_verified"])
        self.assertTrue(x["declared_source_gates_consistent"])
        self.assertEqual(x["decision"], "EXTERNAL_SOURCE_OBJECTS_MATCH_FOR_REVIEW")

    def test_never_claim_independent_g0(self):
        x = verify_live(make_record(), read_fixture(original_objects()))
        self.assertEqual(x["g0"], "HOLD_MANUAL_INDEPENDENT_QA")
        self.assertEqual(x["independent_qa_verdict"], "MANUAL_REVIEW_REQUIRED")

    def test_never_claim_codex_ack_or_integration(self):
        x = verify_live(make_record(), read_fixture(original_objects()))
        self.assertEqual(x["codex_ack_verdict"], "NOT_AUTHENTICATED")
        self.assertEqual(x["integration_verdict"], "NOT_AUTHENTICATED")

    def test_pr_sha_mismatch(self):
        data = original_objects(); data["/pulls/355"]["head"]["sha"] = SHA_ALT
        x = verify_live(make_record(), read_fixture(data))
        self.assertFalse(x["source_git_objects_verified"])
        self.assertIn("live_pr_head_or_repository_mismatch", x["blockers"])

    def test_pr_wrong_base_repo(self):
        data = original_objects(); data["/pulls/355"]["base"]["repo"]["full_name"] = "C8LyNn/Not-Robo"
        self.assertFalse(verify_live(make_record(), read_fixture(data))["source_git_objects_verified"])

    def test_pr_wrong_head_repo(self):
        data = original_objects(); data["/pulls/355"]["head"]["repo"]["full_name"] = "Fake/Repo"
        self.assertFalse(verify_live(make_record(), read_fixture(data))["source_git_objects_verified"])

    def test_pr_wrong_browser_url(self):
        data = original_objects(); data["/pulls/355"]["html_url"] = "https://evil.invalid/fake"
        self.assertFalse(verify_live(make_record(), read_fixture(data))["source_git_objects_verified"])

    def test_pr_malformed_null_head_repo_hold(self):
        data = original_objects(); data["/pulls/355"]["head"]["repo"] = None
        self.assertIn("live_pr_unavailable", verify_live(make_record(), read_fixture(data))["blockers"])

    def test_ci_run_malformed_null_repository_hold(self):
        data = original_objects(); data["/actions/runs/41000000001"]["repository"] = None
        self.assertIn("live_ci_unavailable", verify_live(make_record(), read_fixture(data))["blockers"])

    def test_pr_missing(self):
        data = original_objects(); del data["/pulls/355"]
        self.assertIn("live_pr_unavailable", verify_live(make_record(), read_fixture(data))["blockers"])

    def test_ci_wrong_sha(self):
        data = original_objects(); data["/actions/runs/41000000001"]["head_sha"] = SHA_ALT
        self.assertIn("live_ci_run_sha_result_or_repo_mismatch",
                      verify_live(make_record(), read_fixture(data))["blockers"])

    def test_ci_run_in_progress(self):
        data = original_objects(); data["/actions/runs/41000000001"]["status"] = "in_progress"
        self.assertFalse(verify_live(make_record(), read_fixture(data))["producer_ci_run_object_matches"])

    def test_ci_run_failed(self):
        data = original_objects(); data["/actions/runs/41000000001"]["conclusion"] = "failure"
        self.assertFalse(verify_live(make_record(), read_fixture(data))["producer_ci_run_object_matches"])

    def test_ci_run_wrong_repository(self):
        data = original_objects(); data["/actions/runs/41000000001"]["repository"]["full_name"] = "Attacker/Repo"
        self.assertFalse(verify_live(make_record(), read_fixture(data))["producer_ci_run_object_matches"])

    def test_ci_run_mismatched_id(self):
        data = original_objects(); data["/actions/runs/41000000001"]["id"] = 99
        self.assertFalse(verify_live(make_record(), read_fixture(data))["producer_ci_run_object_matches"])

    def test_ci_missing(self):
        data = original_objects(); del data["/actions/runs/41000000001"]
        self.assertIn("live_ci_unavailable", verify_live(make_record(), read_fixture(data))["blockers"])

    def test_declared_ci_fail_even_if_external_success(self):
        x = make_record(); x["producer"]["ci"]["status"] = "FAIL"
        res = verify_live(x, read_fixture(original_objects()))
        self.assertTrue(res["source_git_objects_verified"])
        self.assertFalse(res["declared_source_gates_consistent"])
        self.assertEqual(res["decision"], "HOLD")

    def test_qa_pending_does_not_fetch_comment(self):
        data = original_objects()
        r = verify_live(make_record(), read_fixture(data))
        # W09 source unit fixture declares PASS, but no real remote QA records exist.
        self.assertEqual(r["qa_comments"], {"intake": "FETCH_FAILED", "independent": "FETCH_FAILED"})
        self.assertIn("live_intake_unavailable", r["blockers"])

    def test_real_qa_pending_skips_network_and_remains_hold(self):
        x = make_record()
        x["qa"]["intake"]["status"] = "PENDING"
        x["qa"]["independent"]["status"] = "PENDING"
        r = verify_live(x, read_fixture(original_objects()))
        self.assertEqual(r["qa_comments"], {"intake": "PENDING", "independent": "PENDING"})
        self.assertEqual(r["decision"], "HOLD")

    def test_existing_comment_with_sha_is_not_authentic_qa(self):
        x = make_record()
        data = original_objects()
        for role in ("intake", "independent"):
            ident = comment_id(x["qa"][role]["url"])
            data["/issues/comments/" + str(ident)] = {
                "id": ident, "html_url": x["qa"][role]["url"],
                "body": "review candidate source " + SHA,
                "user": {"login": "C8LyNn"},
            }
        r = verify_live(x, read_fixture(data))
        self.assertEqual(r["qa_comments"]["intake"], "OBJECT_FOUND_SHA_MENTIONED_NOT_ROLE_AUTHENTICATED")
        self.assertEqual(r["qa_comments"]["independent"], "OBJECT_FOUND_SHA_MENTIONED_NOT_ROLE_AUTHENTICATED")
        self.assertEqual(r["g0"], "HOLD_MANUAL_INDEPENDENT_QA")

    def test_comment_sha_prefix_attack_rejected(self):
        self.assertFalse(sha_mentioned("source " + SHA + "a", SHA))

    def test_comment_missing_sha(self):
        x = make_record()
        data = original_objects()
        c = comment_id(x["qa"]["intake"]["url"])
        data["/issues/comments/" + str(c)] = {
            "id": c, "html_url": x["qa"]["intake"]["url"],
            "body": "looks okay", "user": {"login": "C8LyNn"}
        }
        r = verify_live(x, read_fixture(data))
        self.assertIn("live_intake_object_mismatch", r["blockers"])

    def test_comment_wrong_html_url(self):
        x = make_record(); data = original_objects()
        cid = comment_id(x["qa"]["intake"]["url"])
        data["/issues/comments/" + str(cid)] = {
            "id": cid, "html_url": REPO + "issues/1#issuecomment-9",
            "body": "source " + SHA, "user": {"login": "C8LyNn"}}
        self.assertIn("live_intake_object_mismatch", verify_live(x, read_fixture(data))["blockers"])

    def test_url_extractors_noncanonical(self):
        self.assertIsNone(comment_id(REPO + "issues/79#issuecomment-0"))
        self.assertIsNone(comment_id("https://evil.invalid/issues/79#issuecomment-123"))
        self.assertIsNone(run_id(REPO + "actions/runs/14?format=raw"))
        self.assertEqual(run_id(REPO + "actions/runs/41000000001"), 41000000001)


class NetworkSafety(unittest.TestCase):
    def test_disallow_api_paths_outside_fixed_repo(self):
        for p in ("https://evil.invalid", "/../user", "/issues/1",
                  "/pulls/1?token=SECRET", "//evil", "/pulls/0",
                  "/pulls/1/commits", "/pulls/-1"):
            with self.subTest(p=p), self.assertRaises(GateError):
                fetch_github(p, opener=None)

    def test_refuse_newline_in_token(self):
        with self.assertRaises(GateError):
            fetch_github("/pulls/1", token="secret\r\nAttack: 1")

    def test_no_redirect_to_external_host(self):
        class Response:
            status = 200
            def geturl(self): return "https://evil.invalid/data"
            def read(self, amount): return b"{}"
            def __enter__(self): return self
            def __exit__(self, *args): pass
        class Opener:
            def open(self, request, timeout): return Response()
        with self.assertRaises(GateError):
            fetch_github("/pulls/1", opener=Opener())

    def test_only_get_and_fixed_host_auth(self):
        class Response:
            status = 200
            def geturl(self): return API + "/pulls/1"
            def read(self, amount): return b'{"number":1}'
            def __enter__(self): return self
            def __exit__(self, *args): pass
        class Opener:
            def open(self, request, timeout):
                self.req = request
                return Response()
        opener = Opener()
        self.assertEqual(fetch_github("/pulls/1", token="testing", opener=opener)["number"], 1)
        self.assertEqual(opener.req.get_method(), "GET")
        self.assertEqual(opener.req.full_url, API + "/pulls/1")
        self.assertEqual(opener.req.get_header("Authorization"), "Bearer testing")
        self.assertEqual(opener.req.get_header("User-agent"), "B01-00-Receipt-Latch-W10")

    def test_oversized_response_refused(self):
        class Response:
            status = 200
            def geturl(self): return API + "/pulls/1"
            def read(self, amount): return b"Z" * (512 * 1024 + 1)
            def __enter__(self): return self
            def __exit__(self, *args): pass
        class Opener:
            def open(self, request, timeout): return Response()
        with self.assertRaises(GateError):
            fetch_github("/pulls/1", opener=Opener())

    def test_non_json_http_refused(self):
        class Response:
            status = 200
            def geturl(self): return API + "/pulls/1"
            def read(self, amount): return b"<html>oops</html>"
            def __enter__(self): return self
            def __exit__(self, *args): pass
        class Opener:
            def open(self, request, timeout): return Response()
        with self.assertRaises(GateError):
            fetch_github("/pulls/1", opener=Opener())

    def test_cli_hold_on_pending_with_required_source_objects(self):
        x = make_record()
        with tempfile.TemporaryDirectory() as td:
            inp = Path(td) / "in.json"; inp.write_text(json.dumps(x))
            with patch("github_live.fetch_github", side_effect=GateError("unavailable")):
                with redirect_stdout(io.StringIO()):
                    self.assertEqual(main([str(inp), "--require-source-objects"]), 2)

    def test_cli_does_not_overwrite_input_or_existing_output(self):
        with tempfile.TemporaryDirectory() as td:
            inp = Path(td) / "in.json"; inp.write_text(json.dumps(make_record()))
            out = Path(td) / "out.json"; out.write_text("DO NOT CHANGE")
            with patch("github_live.fetch_github", side_effect=GateError("unavailable")):
                with redirect_stderr(io.StringIO()):
                    self.assertEqual(main([str(inp), "--output", str(inp)]), 2)
                    self.assertEqual(main([str(inp), "--output", str(out)]), 2)
            self.assertEqual(out.read_text(), "DO NOT CHANGE")


if __name__ == "__main__":
    unittest.main()
