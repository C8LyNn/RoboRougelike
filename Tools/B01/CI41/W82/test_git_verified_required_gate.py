#!/usr/bin/env python3
"""PASS16 independent original-Git native-scope / forgery regression.

The negative intentionally executes the PREVIOUS original W78 CLI against a
forged QA-only path claim on a REAL newer Codex game SHA (observed RED).
All original game paths are read from Git objects, never modified.
"""
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[3]
sys.path.insert(0, str(HERE))
import git_verified_required_gate as gate

ORIGINAL_QA_BASE = "fb9e7b8b6d844bcac0351a3d7fb8f51cb2c45892"
ORIGINAL_QA_HEAD = "ba104561e833fb8914979a5d2f4c2d2baa4ea792"
CODEX_BEFORE = "231005c6d0351945a8ee6684f3856af118f0e94f"
CODEX_AFTER = "a2ccd055070a963805cba0779b79f18489f4794f"
OLD_W78 = "6fa53271bd8a2fec81cf0aab94556c8c53cfbf0f"


def call_cli(base, head, *, claim=None, observed=None):
    with tempfile.TemporaryDirectory(prefix="B0141_PASS16_") as tmp:
        args = [sys.executable, str(HERE / "git_verified_required_gate.py"),
                "--repo", str(ROOT), "--base", base, "--head", head]
        if claim is not None:
            p = Path(tmp) / "paths.json"
            p.write_text(json.dumps(claim), encoding="utf-8")
            args += ["--claim-paths-json", str(p)]
        if observed is not None:
            p = Path(tmp) / "observed.json"
            p.write_text(json.dumps(observed), encoding="utf-8")
            args += ["--observed-json", str(p)]
        out = subprocess.run(args, capture_output=True, text=True)
        return out.returncode, json.loads(out.stdout)


class ActualGitSourceGateTests(unittest.TestCase):
    def test_01_original_prior_W78_forged_paths_real_codex_sha_fail_open(self):
        # This real existing file Git blob is unchanged; don't substitute a
        # stand-in implementation of the older W78 parser.
        old = ROOT / "Tools/B01/CI41/W78/required_aggregate.py"
        blob = subprocess.run(["git", "-C", str(ROOT), "rev-parse",
                               OLD_W78 + ":Tools/B01/CI41/W78/required_aggregate.py"],
                              capture_output=True, text=True, check=True).stdout.strip()
        self.assertEqual(blob, "42ea9a2dbd82a96232a761c32a3c1097f14dc716")
        with tempfile.TemporaryDirectory() as tmp:
            receipt = Path(tmp) / "forged.json"
            receipt.write_text(json.dumps({
                "source_sha": CODEX_AFTER,
                "paths": ["Tools/B01/CI41/W74/ci_health.py"]
            }), encoding="utf-8")
            out = subprocess.run([sys.executable, str(old), "--input", str(receipt)],
                                 capture_output=True, text=True)
            self.assertEqual(out.returncode, 0, out.stderr)
            self.assertEqual(json.loads(out.stdout)["state"], "QA_ONLY_NOT_REQUIRED")
        print("ORIGINAL_PRIOR_W78_FORGED_QA_PATHS_ON_CODEX_GAME_ACCEPTED_RED")

    def test_02_new_actual_codex_original_git_diff_requires_unity(self):
        paths = gate.original_diff(ROOT, CODEX_BEFORE, CODEX_AFTER)
        self.assertIn("Assets/R1/R1RunController.cs", paths)
        self.assertIn("Assets/MapEditor/MapStorage.cs", paths)
        self.assertGreater(len(paths), 10)
        code, report = call_cli(CODEX_BEFORE, CODEX_AFTER)
        self.assertEqual(code, 2)
        self.assertEqual(report["source"]["head_sha"], CODEX_AFTER)
        self.assertEqual(report["source"]["provenance"], "ACTUAL_GIT_DIFF_EXACT_COMMITS")
        self.assertEqual(report["decision"]["state"], "UNITY_RELEVANT_INCOMPLETE_FAIL_CLOSED")
        self.assertEqual(report["decision"]["native_g1"], "NOT_RUN")
        print("ACTUAL_CODEX_ASSETS_OLD_TO_NEW_SHA=NATIVE_REQUIRED_BLOCKING_GREEN_FIXED")

    def test_03_new_rejects_forged_QA_only_changed_paths(self):
        code, report = call_cli(CODEX_BEFORE, CODEX_AFTER,
                                claim=["Tools/B01/CI41/W74/ci_health.py"])
        self.assertEqual(code, 2)
        self.assertEqual(report["status"], "SOURCE_SCOPE_FAIL_CLOSED")
        self.assertIn("UNTRUSTED_CHANGED_PATH_CLAIM_MISMATCH", report["error"])
        print("FORGED_QA_ONLY_CHANGED_PATHS=REJECTED_FAIL_CLOSED")

    def test_04_original_QA_only_diff_still_works_and_no_G1(self):
        paths = gate.original_diff(ROOT, ORIGINAL_QA_BASE, ORIGINAL_QA_HEAD)
        self.assertEqual(len(paths), 2, paths)
        code, report = call_cli(ORIGINAL_QA_BASE, ORIGINAL_QA_HEAD, claim=paths)
        self.assertEqual(code, 0)
        self.assertEqual(report["decision"]["state"], "QA_ONLY_NOT_REQUIRED")
        self.assertEqual(report["decision"]["native_g1"], "NOT_RUN")
        self.assertFalse(report["decision"]["shared_workflow_deployed"])
        print("REAL_B01_41_QA_ONLY_ORIGINAL_GIT=ALLOWED_NOT_UNITY_PASS")

    def test_05_malformed_stale_or_wrong_ancestry_cannot_skip_native(self):
        for base, head in (
            ("main", CODEX_AFTER),
            (CODEX_AFTER, CODEX_BEFORE),
            (CODEX_AFTER, CODEX_AFTER),
            (CODEX_BEFORE, "0" * 40),
        ):
            code, report = call_cli(base, head)
            self.assertEqual(code, 2, (base, head, report))
            self.assertEqual(report["status"], "SOURCE_SCOPE_FAIL_CLOSED")
        print("UNKNOWN_STALE_EMPTY_OR_NONANCESTOR_GIT_SOURCE=REJECTED")

    def test_06_untrusted_observed_data_cannot_force_G1_on_actual_game(self):
        fake = {
            "EditMode": {"mode": "EditMode", "source_sha": CODEX_AFTER,
                          "conclusion": "success", "run_id": 1,
                          "steps": [], "native_evidence": {}},
            "PlayMode": {"mode": "PlayMode", "source_sha": CODEX_AFTER,
                          "conclusion": "success", "run_id": 2,
                          "steps": [], "native_evidence": {}},
        }
        code, report = call_cli(CODEX_BEFORE, CODEX_AFTER, observed=fake)
        self.assertEqual(code, 2)
        self.assertEqual(report["decision"]["native_g1"], "NOT_RUN")
        self.assertNotEqual(report["decision"]["state"], "NATIVE_EXECUTED")
        print("FABRICATED_NATIVE_GREEN_JOB_METADATA=STILL_FAIL_CLOSED")


if __name__ == "__main__":
    unittest.main(verbosity=2)
