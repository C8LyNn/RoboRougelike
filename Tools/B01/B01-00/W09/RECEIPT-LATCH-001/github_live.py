#!/usr/bin/env python3
"""W10 read-only GitHub API object verification. This does NOT certify independent QA.

The local receipt parser from receipt_latch.py remains the required first gate.
No GitHub mutation, no ownership/ACK claims, no arbitrary URL fetch, no redirects.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import sys
from urllib.error import HTTPError, URLError
from urllib.request import Request, HTTPRedirectHandler, build_opener

from receipt_latch import REPO, GateError, audit

API = "https://api.github.com/repos/C8LyNn/RoboRougelike"
VALID_PATH = re.compile(
    r"^/(?:pulls/[1-9][0-9]*|issues/comments/[1-9][0-9]*"
    r"|actions/runs/[1-9][0-9]*(?:/jobs)?)$"
)
COMMENT = re.compile(
    r"^https://github\.com/C8LyNn/RoboRougelike/(?:issues|pull)/[1-9][0-9]*#issuecomment-([1-9][0-9]*)$"
)
RUN = re.compile(
    r"^https://github\.com/C8LyNn/RoboRougelike/actions/runs/([1-9][0-9]*)$"
)
HEX_CHARS = "0123456789abcdef"


class NoRedirect(HTTPRedirectHandler):
    def redirect_request(self, request, fp, code, msg, headers, newurl):
        raise GateError("GitHub API redirect refused")


def fetch_github(path, *, token=None, opener=None):
    """Read exactly one whitelisted GitHub API resource; never follow a redirect."""
    if not isinstance(path, str) or not VALID_PATH.fullmatch(path):
        raise GateError("non-whitelisted GitHub API resource")
    if token is not None and (not isinstance(token, str) or not token.strip()
                              or any(c in token for c in "\r\n")):
        raise GateError("invalid GitHub token")
    headers = {"Accept": "application/vnd.github+json",
               "User-Agent": "B01-00-Receipt-Latch-W10",
               "X-GitHub-Api-Version": "2022-11-28"}
    if token is not None:
        headers["Authorization"] = "Bearer " + token
    req = Request(API + path, headers=headers, method="GET")
    client = opener if opener is not None else build_opener(NoRedirect())
    try:
        with client.open(req, timeout=12) as response:
            if response.geturl() != API + path:
                raise GateError("unexpected API response URL")
            if response.status != 200:
                raise GateError("GitHub API returned non-200")
            payload = response.read(512 * 1024 + 1)
            if len(payload) > 512 * 1024:
                raise GateError("GitHub API response size exceeds limit")
    except (HTTPError, URLError, OSError) as exc:
        raise GateError("GitHub API read failed (status/network)") from exc
    try:
        doc = json.loads(payload.decode("utf-8"))
    except (UnicodeError, json.JSONDecodeError) as exc:
        raise GateError("GitHub API returned invalid JSON") from exc
    if type(doc) is not dict:
        raise GateError("GitHub API returned non-object")
    return doc


def comment_id(url):
    m = COMMENT.fullmatch(url) if isinstance(url, str) else None
    return int(m.group(1)) if m else None


def run_id(url):
    m = RUN.fullmatch(url) if isinstance(url, str) else None
    return int(m.group(1)) if m else None


def sha_mentioned(body, source):
    if not isinstance(body, str):
        return False
    # Disallow a source SHA that appears only as a prefix of a longer hash.
    return bool(re.search(r"(?<![0-9a-f])" + re.escape(source) + r"(?![0-9a-f])", body))


def workflow_attestation(ci, run, getter, rid, source, blockers):
    """Require the exact workflow and executed named test jobs on each OS.

    Source-run metadata alone MUST NOT authorize the verified-source receipt.
    This still does not replay logs or authenticate independent QA actors.
    """
    contract = ci.get("contract") if type(ci) is dict else None
    if contract is None:
        blockers.append("source_workflow_contract_missing")
        return False
    if type(contract) is not dict:
        blockers.append("source_workflow_contract_invalid")
        return False

    wid = contract.get("workflow_id")
    workflow_path = contract.get("workflow_path")
    event = contract.get("event")
    names = contract.get("required_job_names")
    steps = contract.get("required_steps")
    def valid_strings(values):
        return (type(values) is list and 0 < len(values) <= 8
                and all(isinstance(v, str) and 0 < len(v) < 128 and v.strip() == v
                        for v in values)
                and len(values) == len(set(values)))
    if (type(wid) is not int or wid < 1
            or not isinstance(workflow_path, str)
            or not re.fullmatch(r"\.github/workflows/[A-Za-z0-9_-]+\.ya?ml", workflow_path)
            or event not in ("pull_request", "push", "workflow_dispatch")
            or not valid_strings(names) or not valid_strings(steps)):
        blockers.append("source_workflow_contract_invalid")
        return False
    if (run.get("workflow_id") != wid or run.get("path") != workflow_path
            or run.get("event") != event):
        blockers.append("live_workflow_identity_mismatch")
        return False

    try:
        response = getter("/actions/runs/" + str(rid) + "/jobs")
    except (GateError, OSError, KeyError, TypeError, ValueError, AttributeError):
        blockers.append("live_workflow_jobs_unavailable")
        return False
    if (type(response) is not dict or type(response.get("total_count")) is not int
            or type(response.get("jobs")) is not list):
        blockers.append("live_workflow_jobs_invalid")
        return False
    jobs = response["jobs"]
    # GitHub returns first page (max 30): reject truncation conservatively.
    if response["total_count"] != len(jobs) or len(jobs) > 30:
        blockers.append("live_workflow_jobs_incomplete")
        return False
    job_names = [j.get("name") if type(j) is dict else None for j in jobs]
    if len(job_names) != len(set(job_names)):
        blockers.append("live_job_duplicate_name")
        return False
    by_name = dict(zip(job_names, jobs))
    if not set(names) <= set(by_name):
        blockers.append("live_required_job_missing")
        return False
    good = True
    for name in names:
        job = by_name[name]
        if (job.get("status") != "completed" or job.get("conclusion") != "success"
                or job.get("head_sha") != source
                or type(job.get("id")) is not int):
            blockers.append("live_job_sha_or_result_mismatch")
            good = False
            continue
        traces = job.get("steps")
        if type(traces) is not list:
            blockers.append("live_required_step_missing_or_fail")
            good = False
            continue
        for required in steps:
            matching = [t for t in traces if type(t) is dict and t.get("name") == required]
            if (len(matching) != 1 or matching[0].get("status") != "completed"
                    or matching[0].get("conclusion") != "success"):
                blockers.append("live_required_step_missing_or_fail")
                good = False
    return good


def verify_live(record, getter):
    """Verify source PR + Actions run using GitHub objects; QA comments existence only.

    The function always returns MANUAL_QA_REQUIRED even when every external object
    is present. No ChatGPT swarm role has a distinct GitHub login or signature here.
    """
    declared = audit(record)  # fail closed on malformed local receipt
    source = declared["source_sha"]
    producer = record["producer"]
    blockers = list(declared["blockers"])
    proof = {"source_pr_head": False, "producer_ci_run": False, "source_workflow_jobs": False}
    qa = {"intake": "PENDING", "independent": "PENDING"}
    try:
        pr = getter("/pulls/" + str(producer["source_pr"]))
        if (pr.get("number") == producer["source_pr"]
                and pr.get("head", {}).get("sha") == source
                and pr.get("base", {}).get("repo", {}).get("full_name") == "C8LyNn/RoboRougelike"
                and pr.get("head", {}).get("repo", {}).get("full_name") == "C8LyNn/RoboRougelike"
                and pr.get("html_url") == producer["url"]):
            proof["source_pr_head"] = True
        else:
            blockers.append("live_pr_head_or_repository_mismatch")
    except (GateError, KeyError, TypeError, ValueError, AttributeError):
        blockers.append("live_pr_unavailable")

    ci = producer.get("ci")
    rid = run_id(ci.get("url")) if type(ci) is dict else None
    if rid is None:
        blockers.append("live_ci_run_not_identified")
    else:
        try:
            live = getter("/actions/runs/" + str(rid))
            if (live.get("id") == rid and live.get("head_sha") == source
                    and live.get("html_url") == ci["url"]
                    and live.get("status") == "completed"
                    and live.get("conclusion") == "success"
                    and live.get("repository", {}).get("full_name") == "C8LyNn/RoboRougelike"):
                proof["producer_ci_run"] = True
                proof["source_workflow_jobs"] = workflow_attestation(
                    ci, live, getter, rid, source, blockers
                )
            else:
                blockers.append("live_ci_run_sha_result_or_repo_mismatch")
        except (GateError, KeyError, TypeError, ValueError, AttributeError):
            blockers.append("live_ci_unavailable")

    # Role-specific claims cannot be proved merely from a comment body or login.
    # We expose only whether an external comment exists and references exact SHA.
    for role in ("intake", "independent"):
        receipt = record.get("qa", {}).get(role)
        if type(receipt) is not dict or receipt.get("status") != "PASS":
            continue
        cid = comment_id(receipt.get("url"))
        if cid is None:
            qa[role] = "INVALID_URL"
            blockers.append("live_" + role + "_invalid_url")
            continue
        try:
            comment = getter("/issues/comments/" + str(cid))
            if (comment.get("id") == cid
                    and comment.get("html_url") == receipt["url"]
                    and sha_mentioned(comment.get("body"), source)
                    and isinstance(comment.get("user", {}).get("login"), str)
                    and comment["user"]["login"]):
                qa[role] = "OBJECT_FOUND_SHA_MENTIONED_NOT_ROLE_AUTHENTICATED"
            else:
                qa[role] = "OBJECT_MISMATCH"
                blockers.append("live_" + role + "_object_mismatch")
        except (GateError, KeyError, TypeError, ValueError, AttributeError):
            qa[role] = "FETCH_FAILED"
            blockers.append("live_" + role + "_unavailable")

    verified_source = all(proof.values())
    return {
        "schema": "b01-github-live-readonly-v1",
        "package_id": declared["package_id"],
        "source_sha": source,
        "source_pr_object_matches": proof["source_pr_head"],
        "producer_ci_run_object_matches": proof["producer_ci_run"],
        "source_git_objects_verified": verified_source,
        "source_workflow_jobs_verified": proof["source_workflow_jobs"],
        "source_gates_label": ("ORIGINAL_WORKFLOW_AND_JOBS_MATCH" if verified_source
                               else "SOURCE_WORKFLOW_PROOF_HOLD"),
        "declared_source_gates_consistent": declared["source_gates_consistent"],
        "qa_comments": qa,
        "independent_qa_verdict": "MANUAL_REVIEW_REQUIRED",
        "codex_ack_verdict": "NOT_AUTHENTICATED",
        "integration_verdict": "NOT_AUTHENTICATED",
        "g0": "HOLD_MANUAL_INDEPENDENT_QA",
        "g1": "NOT_ASSERTED",
        "g2": "HOLD",
        "blockers": sorted(set(blockers)),
        "decision": "EXTERNAL_SOURCE_OBJECTS_MATCH_FOR_REVIEW" if verified_source and declared["source_gates_consistent"] else "HOLD",
    }


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("receipt", type=Path)
    parser.add_argument("--require-source-objects", action="store_true")
    parser.add_argument("--output", type=Path, default=None)
    args = parser.parse_args(argv)
    try:
        raw = args.receipt.read_bytes()
        record = json.loads(raw.decode("utf-8"))
        token = os.environ.get("GITHUB_TOKEN")
        result = verify_live(record, lambda path: fetch_github(path, token=token))
        result["input_sha256"] = hashlib.sha256(raw).hexdigest()
        out = json.dumps(result, sort_keys=True, indent=2) + "\n"
        if args.output is None:
            print(out, end="")
        else:
            if args.output.resolve() == args.receipt.resolve() or args.output.exists():
                raise GateError("refusing overwrite of any input/evidence")
            with args.output.open("x", encoding="utf-8") as handle:
                handle.write(out)
        if args.require_source_objects and not result["source_git_objects_verified"]:
            return 2
        return 0
    except (GateError, OSError, ValueError, UnicodeError, TypeError, KeyError) as exc:
        print("GITHUB_LIVE_HOLD: " + str(exc), file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
