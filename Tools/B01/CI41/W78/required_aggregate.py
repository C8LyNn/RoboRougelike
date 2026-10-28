#!/usr/bin/env python3
"""B01-41 required-check aggregate *policy*, not a native CI deployment.

Accepts exact diff paths and observed GitHub job metadata. A NATIVE_EXECUTED
state is only an execution CLAIM structurally suitable for independent intake:
it never grants Unity G1, G2, release, or a source receipt PASS.

Intended future integration requires HQ approval of shared CI-02/03 and stable
always-emitted status contexts; this tool DOES NOT modify shared workflows.
"""
from __future__ import annotations
import argparse
import json
from pathlib import Path
import re
import sys

from ci_health import classify

EXPECTED_STAGES = {
    "EditMode": "Unity 6000.3.14f1 EditMode",
    "PlayMode": "Unity 6000.3.14f1 PlayMode",
}
REQUIRED_STEPS = (
    "Validate project before launching Unity",
    "Require valid Unity activation credentials",
    "Require nonempty, fully passing executed test cases",
)
HEX40 = re.compile("[0-9a-f]{40}\\Z")
HEX64 = re.compile("[0-9a-f]{64}\\Z")


def _step(job: dict, name: str) -> str:
    steps = [s.get("conclusion") for s in job.get("steps", []) if s.get("name") == name]
    if len(steps) != 1 or steps[0] not in ("success", "failure", "skipped", "cancelled"):
        raise ValueError("MISSING_DUPLICATE_OR_INVALID_STEP:" + name)
    return steps[0]


def _wellformed_job(job: dict, mode: str, sha: str) -> bool:
    if not isinstance(job, dict) or job.get("mode") != mode:
        return False
    if job.get("source_sha") != sha or not HEX40.fullmatch(str(sha)):
        return False
    if type(job.get("run_id")) is not int or job["run_id"] <= 0:
        return False
    if job.get("conclusion") not in ("success", "failure", "cancelled", "skipped"):
        return False
    return isinstance(job.get("steps"), list)


def _native_claim_valid(job: dict, mode: str, sha: str) -> bool:
    evidence = job.get("native_evidence")
    if not isinstance(evidence, dict):
        return False
    if evidence.get("source_sha") != sha or evidence.get("unity_version") != "6000.3.14f1":
        return False
    if type(evidence.get("exit_code")) is not int or evidence["exit_code"] != 0:
        return False
    if type(evidence.get("test_cases")) is not int or evidence["test_cases"] <= 0:
        return False
    if type(evidence.get("artifact_id")) is not int or evidence["artifact_id"] <= 0:
        return False
    if type(evidence.get("run_id")) is not int or evidence["run_id"] != job.get("run_id"):
        return False
    return all(isinstance(evidence.get(k), str) and HEX64.fullmatch(evidence[k])
               for k in ("original_xml_sha256", "original_editor_log_sha256"))


def evaluate(paths: list[str], source_sha: str, observed: dict | None = None) -> dict:
    """Never labels native PASS based on a badge or user-provided metadata."""
    scope = classify(paths)
    base = {
        "scope": scope["classification"],
        "stable_required_contexts": "MUST_ALWAYS_EMIT",
        "native_g1": "NOT_RUN",
        "g2": "HOLD",
        "is_actual_native_proof": False,
        "shared_workflow_deployed": False,
    }
    if scope["classification"] == "QA_ONLY":
        base.update(state="QA_ONLY_NOT_REQUIRED", required_check_policy="SCOPE_ONLY_NON_NATIVE",
                    actual_global_check_status="NOT_INFERRED")
        return base
    base.update(state="UNITY_RELEVANT_INCOMPLETE_FAIL_CLOSED", required_check_policy="FAIL",
                actual_global_check_status="NOT_INFERRED")
    if not isinstance(observed, dict) or not HEX40.fullmatch(str(source_sha)):
        return base
    jobs = {}
    try:
        for mode in EXPECTED_STAGES:
            j = observed.get(mode)
            if not _wellformed_job(j, mode, source_sha):
                return base
            jobs[mode] = {
                "job": j,
                "activation": _step(j, "Require valid Unity activation credentials"),
                "native": _step(j, EXPECTED_STAGES[mode]),
                "results": _step(j, "Require nonempty, fully passing executed test cases"),
                "validation": _step(j, "Validate project before launching Unity"),
            }
    except (AttributeError, ValueError, TypeError):
        return base

    # Actual 2026-10-28 CI02/03 state: license is missing, Unity never launched.
    if all(z["job"]["conclusion"] == "failure" and z["validation"] == "success" and
           z["activation"] == "failure" and z["native"] == "skipped" and z["results"] == "skipped"
           for z in jobs.values()):
        base.update(state="UNITY_RELEVANT_BLOCKED_NO_LICENSE",
                    required_check_policy="FAIL_BLOCKING", native_g1="NOT_RUN")
        return base

    # Only a candidate for human/independent original evidence review. A fake
    # JSON cannot establish true executable provenance or a real Unity PASS.
    if all(z["job"]["conclusion"] == "success" and z["validation"] == "success" and
           z["activation"] == "success" and z["native"] == "success" and z["results"] == "success" and
           _native_claim_valid(z["job"], mode, source_sha)
           for mode, z in jobs.items()):
        base.update(state="NATIVE_EXECUTED", required_check_policy="PENDING_ORIGINAL_REVIEW",
                    native_g1="PENDING_INDEPENDENT_B01_60",
                    evidence_status="STRUCTURAL_CLAIM_ONLY_ORIGINAL_BYTES_UNVERIFIED")
    return base


def main() -> int:
    p = argparse.ArgumentParser(description="Fail-closed CI aggregate; NO native launch")
    p.add_argument("--input", required=True, help="JSON with paths/source_sha/observed")
    a = p.parse_args()
    try:
        obj = json.loads(Path(a.input).read_text("utf-8"))
        result = evaluate(obj["paths"], obj["source_sha"], obj.get("observed"))
        print(json.dumps(result, indent=2, sort_keys=True))
        return 0 if result["state"] == "QA_ONLY_NOT_REQUIRED" else (
            3 if result["state"] == "NATIVE_EXECUTED" else 2)
        # Nonzero for even NATIVE_EXECUTED: no auto successful required check.
    except (KeyError, ValueError, TypeError, OSError) as err:
        print(json.dumps({"state": "INVALID_INPUT_FAIL_CLOSED", "native_g1": "NOT_RUN",
                          "error": str(err)}, sort_keys=True))
        return 2


if __name__ == "__main__":
    sys.exit(main())
