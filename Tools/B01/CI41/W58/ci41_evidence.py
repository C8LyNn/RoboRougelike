#!/usr/bin/env python3
"""B01-41: CI activation triage + cross-workflow evidence integrity (not G1 approval).

The package-specific #298 receipt verifier remains authoritative for PARTSIZE test inventory.
All receipts/tests built with this module can be SYNTHETIC and therefore only prove
validator behavior.  This tool NEVER asserts licensed Unity ran or grants G1/G2.
"""
import argparse
import hashlib
import json
from pathlib import Path
import re
import sys
import xml.etree.ElementTree as ET

STAGE = {"EditMode": "CI-02 Unity EditMode", "PlayMode": "CI-03 Unity PlayMode"}
PREFLIGHT = "Require valid Unity activation credentials"
ACTIVATION_MARKER = "BLOCKED: Unity NOT RUN."
UNITY_VERSION = "6000.3.14f1"
HEX40 = re.compile(r"^[a-f0-9]{40}$")
HEX64 = re.compile(r"^[a-f0-9]{64}$")


class EvidenceError(ValueError):
    pass


def _step(steps, needle):
    matches = [s for s in steps if needle in s.get("name", "")]
    if len(matches) != 1:
        raise EvidenceError("MISSING_OR_DUPLICATED_STEP:" + needle)
    return matches[0].get("conclusion")


def classify_activation(job):
    """Do not use secrets, and do not claim a valid license from a passing preflight."""
    if job.get("workflow") not in STAGE.values():
        raise EvidenceError("WRONG_WORKFLOW")
    steps = job.get("steps")
    if not isinstance(steps, list):
        raise EvidenceError("NO_ACTIONS_STEPS")
    gate = _step(steps, PREFLIGHT)
    engine = _step(steps, UNITY_VERSION)
    marker = ACTIVATION_MARKER in job.get("public_log_excerpt", "")
    if gate == "failure" and engine in ("success", "failure", "cancelled"):
        raise EvidenceError("INCONSISTENT_ACTIVATION_VS_ENGINE_EXECUTION")
    if gate == "failure" and engine == "skipped" and marker:
        return {"code": "NO_LICENSE_INPUT", "native": "NOT_RUN", "g1": "HOLD",
                "detail": "Required activation inputs absent/empty IN THIS JOB; entitlement not evaluated"}
    if gate == "success" and engine == "success":
        return {"code": "ENGINE_STEP_SUCCEEDED", "native": "UNVERIFIED",
                "g1": "HOLD", "detail": "Require original XML, Editor.log, native exit and SHA"}
    if gate == "success" and engine in ("failure", "cancelled"):
        return {"code": "ENGINE_STEP_FAILED", "native": "ATTEMPTED_UNVERIFIED", "g1": "HOLD"}
    if gate == "failure" and engine == "skipped":
        return {"code": "ACTIVATION_PREFLIGHT_FAILED_UNKNOWN", "native": "NOT_RUN", "g1": "HOLD"}
    if engine == "skipped":
        return {"code": "ENGINE_SKIPPED_OTHER_CAUSE", "native": "NOT_RUN", "g1": "HOLD"}
    if engine in ("success", "failure", "cancelled"):
        return {"code": "ENGINE_EXECUTION_STATUS_UNCERTAIN", "native": "UNVERIFIED", "g1": "HOLD"}
    return {"code": "INCOMPLETE_JOB", "native": "NOT_RUN", "g1": "HOLD"}


def _sha256(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def _checked_file(root, relative, digest):
    if not isinstance(relative, str) or not relative or Path(relative).is_absolute():
        raise EvidenceError("UNSAFE_EVIDENCE_PATH")
    p = Path(relative)
    if any(z in ("..", ".") for z in p.parts) or len(p.parts) != 1:
        raise EvidenceError("EVIDENCE_OUTSIDE_RECEIPT_ROOT")
    if not HEX64.fullmatch(str(digest)):
        raise EvidenceError("INVALID_EVIDENCE_SHA256")
    path = root / p
    if path.is_symlink() or not path.is_file():
        raise EvidenceError("MISSING_OR_SYMLINKED_EVIDENCE:" + relative)
    if _sha256(path) != digest:
        raise EvidenceError("EVIDENCE_BYTES_TAMPER:" + relative)
    return path


def _verify_xml(path):
    try:
        xml = ET.parse(path).getroot()
    except (ET.ParseError, OSError) as e:
        raise EvidenceError("INVALID_NUNIT_XML") from e
    if xml.tag != "test-run" or xml.get("result") != "Passed":
        raise EvidenceError("NUNIT_NOT_PASSED")
    cases = list(xml.iter("test-case"))
    if not cases:
        raise EvidenceError("NO_EXECUTED_TEST_CASES")
    identities = [case.get("fullname") for case in cases]
    if any(not x for x in identities) or len(set(identities)) != len(identities):
        raise EvidenceError("DUPLICATE_OR_MISSING_TEST_ID")
    if any(c.get("result") != "Passed" for c in cases):
        raise EvidenceError("NONPASS_NUNIT_CASE")
    for field, expected in (("total", len(cases)), ("passed", len(cases)),
                            ("failed", 0), ("skipped", 0), ("inconclusive", 0)):
        if field not in xml.attrib:
            raise EvidenceError("MISSING_NUNIT_AGGREGATE:" + field)
        try:
            val = int(xml.get(field))
        except ValueError as e:
            raise EvidenceError("NONINTEGER_NUNIT_AGGREGATE:" + field) from e
        if val != expected:
            raise EvidenceError("INCONSISTENT_NUNIT_AGGREGATE:" + field)
    return len(cases)


def check_cross_workflow_receipt(receipt_dir, expected_sha, unity_version=UNITY_VERSION):
    """Check CI-02/03 identity and bytes; structurally sound != native G1."""
    if not HEX40.fullmatch(expected_sha):
        raise EvidenceError("INVALID_EXPECTED_SOURCE_SHA")
    folder = Path(receipt_dir)
    try:
        info = json.loads((folder / "receipt.json").read_text(encoding="utf-8"))
    except (OSError, ValueError, UnicodeError) as e:
        raise EvidenceError("MISSING_OR_INVALID_RECEIPT") from e
    if info.get("source_sha") != expected_sha or info.get("unity_version") != unity_version:
        raise EvidenceError("UNEXPECTED_SOURCE_OR_EDITOR")
    jobs = info.get("jobs")
    if not isinstance(jobs, dict) or set(jobs) != set(STAGE):
        raise EvidenceError("MISSING_OR_FOREIGN_STAGE")
    run_ids = set()
    stats = {}
    for stage, workflow in STAGE.items():
        item = jobs[stage]
        if not isinstance(item, dict) or item.get("workflow") != workflow:
            raise EvidenceError("STAGE_WORKFLOW_MISMATCH:" + stage)
        if item.get("head_sha") != expected_sha:
            raise EvidenceError("CROSS_STAGE_SHA_MISMATCH:" + stage)
        if item.get("activation") != "ENGINE_STEP_SUCCEEDED" or item.get("conclusion") != "success":
            raise EvidenceError("JOB_NOT_EXECUTED_SUCCESSFULLY:" + stage)
        run_id = item.get("run_id")
        if not isinstance(run_id, int) or isinstance(run_id, bool) or run_id <= 0 or run_id in run_ids:
            raise EvidenceError("INVALID_OR_REUSED_RUN_ID")
        run_ids.add(run_id)
        exit_code = item.get("native_exit_code")
        if exit_code != 0 or isinstance(exit_code, bool):
            raise EvidenceError("MISSING_OR_NONZERO_NATIVE_EXIT:" + stage)
        xml = _checked_file(folder, stage + ".xml", item.get("xml_sha256"))
        log = _checked_file(folder, stage + ".log", item.get("log_sha256"))
        raw = log.read_text(encoding="utf-8", errors="replace")
        # Text is a consistency check, not proof a real Unity editor generated it.
        if len(raw) < 50 or f"Initialize engine version: {unity_version}" not in raw:
            raise EvidenceError("LOG_WITHOUT_EDITOR_VERSION:" + stage)
        stats[stage] = _verify_xml(xml)
    return {"status": "STRUCTURAL_RECEIPT_ONLY", "g1": "PENDING_INDEPENDENT_NATIVE_REVIEW",
            "g2": "HOLD", "source_sha": expected_sha, "synthetic_possible": True,
            "stage_cases": stats, "run_ids_distinct": True}


def main(argv=None):
    p = argparse.ArgumentParser(description=__doc__)
    sub = p.add_subparsers(dest="command", required=True)
    a = sub.add_parser("activation"); a.add_argument("snapshot", type=Path)
    b = sub.add_parser("receipt"); b.add_argument("directory", type=Path)
    b.add_argument("--expected-sha", required=True)
    args = p.parse_args(argv)
    try:
        if args.command == "activation":
            data = json.loads(args.snapshot.read_text(encoding="utf-8"))
            value = classify_activation(data)
        else:
            value = check_cross_workflow_receipt(args.directory, args.expected_sha)
    except (ValueError, OSError, EvidenceError) as err:
        print(json.dumps({"code": "FAIL_CLOSED", "reason": str(err), "g1": "HOLD"}))
        return 2
    print(json.dumps(value, sort_keys=True))
    if args.command == "activation" and value["native"] == "NOT_RUN":
        return 3
    return 0


if __name__ == "__main__":
    sys.exit(main())
