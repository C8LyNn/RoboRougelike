#!/usr/bin/env python3
"""Regression cases on real immutable CI-02/03 observation + labeled synthetic claim.

No test in this file launches Unity, certifies real NUnit execution or
changes the shared workflow. NATIVE_EXECUTED means CLAIM ONLY -> independent QA.
"""
import copy
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE.parent / "W74"))
sys.path.insert(0, str(HERE))
import ci_health
import required_aggregate as a
sys.path.insert(0, str(HERE.parent / 'W82'))

ROOT = HERE.parents[3]  # repo root
QA_BASE = "fb9e7b8b6d844bcac0351a3d7fb8f51cb2c45892"
QA_HEAD = "ba104561e833fb8914979a5d2f4c2d2baa4ea792"


def original():
    return json.loads((HERE / "observed_real_unlicensed.json").read_text("utf-8"))


def synthetic_native_candidate():
    """Strictly SYNTHETIC; hash strings are NOT original Unity artifacts."""
    x = copy.deepcopy(original())
    for mode, item in x["observed"].items():
        item["conclusion"] = "success"
        for step in item["steps"]:
            step["conclusion"] = "success"
        item["native_evidence"] = {
            "source_sha": x["source_sha"], "unity_version": "6000.3.14f1",
            "exit_code": 0, "test_cases": 5, "run_id": item["run_id"],
            "artifact_id": 123456,
            "original_xml_sha256": "a" * 64,
            "original_editor_log_sha256": "b" * 64,
            "fixture_label": "SYNTHETIC_NO_ORIGINAL_UNITY_ARTIFACT",
        }
    return x


class RequiredCheckAggregateTests(unittest.TestCase):
    def test_01_real_QA_only_original_diff_does_not_grant_G1(self):
        paths = ci_health.original_git_paths(ROOT, QA_BASE, QA_HEAD)
        out = a.evaluate(paths, original()["source_sha"])
        self.assertEqual(out["state"], "QA_ONLY_NOT_REQUIRED")
        self.assertEqual(out["required_check_policy"], "SCOPE_ONLY_NON_NATIVE")
        self.assertEqual(out["native_g1"], "NOT_RUN")
        self.assertFalse(out["shared_workflow_deployed"])
        print("QA_ONLY_NOT_REQUIRED=NON_NATIVE_SCOPE_ONLY_NOT_G1")

    def test_02_real_original_GH_CI02_CI03_failed_activation_never_green(self):
        x = original()
        self.assertEqual(x["observed"]["EditMode"]["run_id"], 40656682311)
        self.assertEqual(x["observed"]["PlayMode"]["run_id"], 40656682300)
        out = a.evaluate(x["paths"], x["source_sha"], x["observed"])
        self.assertEqual(out["state"], "UNITY_RELEVANT_BLOCKED_NO_LICENSE")
        self.assertEqual(out["required_check_policy"], "FAIL_BLOCKING")
        self.assertEqual(out["native_g1"], "NOT_RUN")
        print("REAL_GH_CI02_CI03_ACTIVATION_FAILURE=BLOCKED_NO_LICENSE")

    def test_03_original_missing_job_or_success_badge_with_skipped_unity_is_red(self):
        x = original()
        del x["observed"]["PlayMode"]
        self.assertEqual(a.evaluate(x["paths"], x["source_sha"], x["observed"])["required_check_policy"], "FAIL")
        x = original()
        for item in x["observed"].values():
            item["conclusion"] = "success"  # forged badge, actual step stays skipped
        self.assertEqual(a.evaluate(x["paths"], x["source_sha"], x["observed"])["required_check_policy"], "FAIL")
        print("MISSING_CONTEXT_OR_FALSE_GREEN_BADGE=FAIL_CLOSED")

    def test_04_synthetic_native_claim_only_waits_independent_original_receipt(self):
        x = synthetic_native_candidate()
        out = a.evaluate(x["paths"], x["source_sha"], x["observed"])
        self.assertEqual(out["state"], "NATIVE_EXECUTED")
        self.assertEqual(out["required_check_policy"], "PENDING_ORIGINAL_REVIEW")
        self.assertEqual(out["native_g1"], "PENDING_INDEPENDENT_B01_60")
        self.assertFalse(out["is_actual_native_proof"])
        self.assertEqual(out["evidence_status"], "STRUCTURAL_CLAIM_ONLY_ORIGINAL_BYTES_UNVERIFIED")
        print("NATIVE_EXECUTED_SYNTHETIC_INPUT=STRUCTURAL_CLAIM_ONLY_G1_NOT_CERTIFIED")

    def test_05_reject_synthetic_claim_without_any_original_receipt_or_partial_stage(self):
        cases = []
        for variant in ("missing_artifact", "missing_log_hash", "missing_xml_hash", "false_exit",
                        "wrong_sha", "wrong_run", "missing_play", "skipped_play", "no_cases"):
            x = synthetic_native_candidate()
            p = x["observed"]["PlayMode"]
            e = p["native_evidence"]
            if variant == "missing_artifact":
                e.pop("artifact_id")
            elif variant == "missing_log_hash":
                e.pop("original_editor_log_sha256")
            elif variant == "missing_xml_hash":
                e["original_xml_sha256"] = ""
            elif variant == "false_exit":
                e["exit_code"] = False
            elif variant == "wrong_sha":
                e["source_sha"] = "0" * 40
            elif variant == "wrong_run":
                e["run_id"] += 1
            elif variant == "missing_play":
                x["observed"].pop("PlayMode")
            elif variant == "skipped_play":
                for step in p["steps"]:
                    if step["name"].endswith("PlayMode"):
                        step["conclusion"] = "skipped"
            elif variant == "no_cases":
                e["test_cases"] = 0
            out = a.evaluate(x["paths"], x["source_sha"], x["observed"])
            self.assertEqual(out["state"], "UNITY_RELEVANT_INCOMPLETE_FAIL_CLOSED", variant)
            self.assertEqual(out["native_g1"], "NOT_RUN", variant)
            cases.append(variant)
        print("NATIVE_EVIDENCE_NEGATIVES_REJECTED=" + ",".join(cases))

    def test_06_critical_operator_diff_triggers_native_required_not_scope_bypass(self):
        x = original()
        for path in ("Tools/B01/CHL003/W66Receipt/chl003_w66_operator.py",
                     "Tools/B01/PARTSIZE001/W26Receipt/Run-UnityNative.ps1",
                     "Docs/B01/CHL003/manifest.json"):
            out = a.evaluate([path], x["source_sha"], x["observed"])
            self.assertEqual(out["state"], "UNITY_RELEVANT_BLOCKED_NO_LICENSE", path)
        print("NEW_W74_PROTECTED_OPERATORS_CANNOT_BYPASS_AGGREGATE")

    def test_07_new_cli_rejects_real_codex_diff_and_allows_verified_own_qa(self):
        import git_verified_required_gate as git_gate
        qa_base = "fb9e7b8b6d844bcac0351a3d7fb8f51cb2c45892"
        qa_head = "ba104561e833fb8914979a5d2f4c2d2baa4ea792"
        game_base = "231005c6d0351945a8ee6684f3856af118f0e94f"
        game_head = "a2ccd055070a963805cba0779b79f18489f4794f"
        with tempfile.TemporaryDirectory() as tmp:
            p = Path(tmp) / "input.json"
            qa = {"paths": git_gate.original_diff(ROOT, qa_base, qa_head),
                  "source_sha": qa_head}
            p.write_text(json.dumps(qa), encoding="utf-8")
            common = [sys.executable, str(HERE / "required_aggregate.py"),
                      "--input", str(p), "--repo", str(ROOT)]
            proc = subprocess.run(common + ["--base", qa_base, "--head", qa_head],
                                  capture_output=True, text=True)
            self.assertEqual(proc.returncode, 0, proc.stderr)
            self.assertEqual(json.loads(proc.stdout)["state"], "QA_ONLY_NOT_REQUIRED")
            self.assertEqual(json.loads(proc.stdout)["native_g1"], "NOT_RUN")
            # The exact same claimed QA-only changed paths are a forgery
            # when used with a real Codex commit that touched Assets/**.
            qa["source_sha"] = game_head
            p.write_text(json.dumps(qa), encoding="utf-8")
            proc = subprocess.run(common + ["--base", game_base, "--head", game_head],
                                  capture_output=True, text=True)
            self.assertEqual(proc.returncode, 2)
            self.assertEqual(json.loads(proc.stdout)["state"], "INVALID_INPUT_FAIL_CLOSED")
            # Legacy --input-only invocation must now fail (not a green skip).
            proc = subprocess.run(common, capture_output=True, text=True)
            self.assertNotEqual(proc.returncode, 0)
        print("W78_ACTUAL_CLI_IMMUTABLE_GIT_REQUIRED_QA_GREEN_GAME_FORGERY_RED")

    def test_08_synthetic_native_receipt_requires_source_identity_and_manual_QA(self):
        import git_verified_required_gate as git_gate
        game_base = "231005c6d0351945a8ee6684f3856af118f0e94f"
        game_head = "a2ccd055070a963805cba0779b79f18489f4794f"
        x = synthetic_native_candidate()
        x["paths"] = git_gate.original_diff(ROOT, game_base, game_head)
        x["source_sha"] = game_head
        for mode, job in x["observed"].items():
            job["source_sha"] = game_head
            job["native_evidence"]["source_sha"] = game_head
        with tempfile.TemporaryDirectory() as tmp:
            p = Path(tmp) / "synthetic.json"
            p.write_text(json.dumps(x), encoding="utf-8")
            proc = subprocess.run([sys.executable, str(HERE / "required_aggregate.py"),
                                   "--input", str(p), "--repo", str(ROOT),
                                   "--base", game_base, "--head", game_head],
                                  capture_output=True, text=True)
            self.assertEqual(proc.returncode, 3, proc.stderr)
            result = json.loads(proc.stdout)
            self.assertEqual(result["state"], "NATIVE_EXECUTED")
            self.assertFalse(result["is_actual_native_proof"])
            self.assertEqual(result["native_g1"], "PENDING_INDEPENDENT_B01_60")
            print("SYNTHETIC_NATIVE_CLAIM_CANNOT_RETURN_GREEN_EXIT_CODE")


if __name__ == "__main__":
    unittest.main(verbosity=2)
