#!/usr/bin/env python3
"""CI41-REPLAY-001 regression: ACTUAL subprocesses simulating Unity CLI.

Runs a real Python fake-editor executable for process/exit/XML verification.
No test in this suite invokes the actual Unity Editor or qualifies native G1.
"""
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest
import xml.etree.ElementTree as ET

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import unity_repeat as u

CAMERA = "Camera275NativeTests.ScrollZoomIsSmoothAndRemainsAnchoredToVehicle"
HEAVY = "MapgenRC1IntegrationTests.MeasuredHeavySpawnsOnGeneratedFloorWithCanonicalEnemies"

EDITOR = r'''
import sys
from pathlib import Path
import time
import xml.etree.ElementTree as ET
args = sys.argv[1:]
def flag(s):
    return args[args.index(s)+1]
scenario = flag("--scenario")
if scenario == "timeout":
    time.sleep(4)
    sys.exit(0)
if scenario == "launch-error":
    sys.exit(77)
target = Path(flag("-testResults"))
log = Path(flag("-logFile"))
log.parent.mkdir(parents=True, exist_ok=True)
log.write_text("SIMULATED_EDITOR_ONLY; NO UNITY NATIVE QA\n")
if scenario == "no-xml":
    sys.exit(0)
filters = flag("-testFilter").split(";")
if scenario == "wrong-case":
    filters = ["UnknownTests.NotRequested"]
if scenario == "duplicate":
    filters.append(filters[0])
failure = scenario in ("red", "zero-red")
root = ET.Element("test-run", {"total": str(len(filters)),
    "passed": str(len(filters) - int(failure)),
    "failed": str(int(failure)), "skipped": "0", "inconclusive": "0",
    "result": "Failed(Child)" if failure else "Passed"})
for i, f in enumerate(filters):
    ET.SubElement(root, "test-case", {"fullname": f,
       "result": "Failed" if failure and i == 0 else "Passed"})
if scenario == "bad-count":
    root.set("passed", str(len(filters)+1))
if scenario == "xml-broken":
    target.write_text("<not xml")
else:
    target.write_bytes(b'<?xml version="1.0" encoding="utf-8"?>\n' + ET.tostring(root))
if scenario in ("red", "green-bad-exit"):
    sys.exit(2)
sys.exit(0)
'''


def git(repo, *args):
    p = subprocess.run(["git", "-C", str(repo), *args],
                       capture_output=True, text=True)
    if p.returncode:
        raise RuntimeError("git failed: " + p.stderr)
    return p.stdout.strip()


class PhysicalSubprocessReplayTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory(prefix="ci41-replay-")
        self.root = Path(self.tmp.name)
        self.project = self.root / "project"
        self.project.mkdir()
        git(self.project, "init", "-q")
        git(self.project, "config", "user.email", "ci41@example.invalid")
        git(self.project, "config", "user.name", "CI41 Fixture")
        (self.project / "Assets").mkdir()
        (self.project / "Assets" / "SafeGame.cs").write_text("class SafeGame {}")
        git(self.project, "add", "Assets/SafeGame.cs")
        git(self.project, "commit", "-qm", "original immutable fake game source")
        self.sha = git(self.project, "rev-parse", "HEAD")
        self.stub = self.root / "fake_editor.py"
        self.stub.write_text(EDITOR, encoding="utf-8")
        self.counter = 0

    def tearDown(self):
        self.tmp.cleanup()

    def run_fake(self, scenario="pass", trials=2, tests=None, timeout=3):
        self.counter += 1
        return u.replay(
            project=self.project, editor=Path(sys.executable),
            editor_args=[str(self.stub), "--scenario", scenario],
            output=self.root / ("run-%d" % self.counter),
            source=self.sha, test_names=tests or [CAMERA, HEAVY],
            repeats=trials, timeout=timeout)

    def test_01_original_failing_codex_test_names_repeated_green_fake_editor(self):
        d = self.run_fake(trials=3)
        self.assertEqual(d["status"], "REPEAT_PASS")
        self.assertEqual(d["trials_passed"], 3)
        self.assertEqual(d["full_suite_g1"], "NOT_ASSESSED_BY_FILTERED_REPLAY")
        for item in d["results"]:
            self.assertTrue(Path(item["xml_path"]).exists())
            self.assertEqual(set(item["cases"]), {CAMERA, HEAVY})
            self.assertEqual(len(item["xml_sha256"]), 64)
            self.assertIn("PASS", item["status"])
        print("ACTUAL_SUBPROCESS_THREE_TRIALS_TWO_EXACT_CODEX_FILTERS_GREEN_NOT_UNITY")

    def test_02_real_process_nonzero_and_failed_xml_hold(self):
        d = self.run_fake("red", trials=2)
        self.assertEqual(d["status"], "HOLD_REPRO_OR_INVALID_EVIDENCE")
        self.assertTrue(all(x["status"] == "NATIVE_TRIAL_FAIL" for x in d["results"]))
        self.assertTrue(all(x["exit_code"] == 2 for x in d["results"]))
        print("ACTUAL_SUBPROCESS_RED_XML_EXIT2_PRESERVED_HOLD")

    def test_03_fake_process_zero_but_red_xml_cannot_pass(self):
        d = self.run_fake("zero-red", trials=1)
        self.assertEqual(d["results"][0]["status"], "HOLD_XML_NONPASS_WITH_ZERO_EXIT")
        print("ZERO_EXIT_RED_NUNIT_IS_NEVER_GREEN")

    def test_04_real_process_nonzero_with_green_xml_cannot_pass(self):
        d = self.run_fake("green-bad-exit", trials=1)
        self.assertEqual(d["results"][0]["status"], "NATIVE_TRIAL_FAIL")
        print("GREEN_XML_NONZERO_PROCESS_IS_NEVER_GREEN")

    def test_05_missing_xml_even_if_exit_zero_is_hold(self):
        d = self.run_fake("no-xml", trials=1)
        self.assertEqual(d["results"][0]["status"], "HOLD_INVALID_OR_MISSING_XML")
        self.assertEqual(d["results"][0]["evidence_error"], "UNITY_XML_MISSING")
        print("SUCCESSFUL_PROCESS_WITH_MISSING_XML_FAILS_CLOSED")

    def test_06_invalid_xml_and_counter_mismatch_and_duplicate_case(self):
        for scenario in ("xml-broken", "bad-count", "duplicate"):
            with self.subTest(scenario=scenario):
                d = self.run_fake(scenario, trials=1)
                self.assertEqual(d["results"][0]["status"], "HOLD_INVALID_OR_MISSING_XML")
        print("CORRUPT_TRUNCATED_OR_DUPLICATE_NUNIT_FAILS_CLOSED")

    def test_07_filter_result_mismatch_does_not_qualify_pass(self):
        d = self.run_fake("wrong-case", trials=1)
        self.assertEqual(d["results"][0]["status"], "HOLD_INVALID_OR_MISSING_XML")
        self.assertEqual(d["results"][0]["evidence_error"], "UNITY_FILTER_COVERAGE_MISMATCH")
        print("GREEN_UNRELATED_TEST_CANNOT_SATISFY_REQUESTED_CODEX_FILTER")

    def test_08_editor_timeout_preserves_partial_attempt(self):
        d = self.run_fake("timeout", trials=2, timeout=1)
        self.assertEqual(d["results"][0]["status"], "HOLD_TIMEOUT")
        self.assertEqual(len(d["results"]), 1)
        self.assertTrue((self.root / ("run-%d" % self.counter) / "summary.json").is_file())
        print("REAL_SUBPROCESS_TIMEOUT_STOPS_REPLAY_WITH_EVIDENCE")

    def test_09_exact_gameplay_sha_mismatch_rejected_before_invoking_editor(self):
        with self.assertRaisesRegex(u.ReplayError, "SOURCE_SHA_MISMATCH"):
            u.replay(project=self.project, editor=Path(sys.executable),
                     output=self.root / "mismatched",
                     source="f"*40, test_names=[CAMERA], repeats=1, timeout=2)
        self.assertFalse((self.root / "mismatched").exists())
        print("STALE_OR_INCORRECT_GAMEPLAY_SHA_NO_EDITOR_EXECUTION")

    def test_10_dirty_tracked_assets_and_bad_filters_fail_closed(self):
        (self.project / "Assets" / "SafeGame.cs").write_text("class SafeGame { int x; }")
        with self.assertRaisesRegex(u.ReplayError, "TRACKED_UNITY_SOURCE_DIRTY"):
            self.run_fake(trials=1)
        with self.assertRaises(u.ReplayError):
            u.validate_names(["Demo.Test; rm -rf x"])
        with self.assertRaises(u.ReplayError):
            u.validate_names([CAMERA, CAMERA])
        with self.assertRaises(u.ReplayError):
            u.validate_names([""])
        print("DIRTY_CODEX_GAME_SOURCE_AND_UNSAFE_FILTERS_REJECTED_BEFORE_NATIVE_RUN")

    def test_11_output_collision_is_never_overwritten(self):
        d = self.run_fake("pass", trials=1)
        output = self.root / ("run-%d" % self.counter)
        before = (output / "summary.json").read_bytes()
        with self.assertRaisesRegex(u.ReplayError, "OUTPUT_DIRECTORY_MUST_NOT_EXIST"):
            u.replay(project=self.project, editor=Path(sys.executable),
                     editor_args=[str(self.stub), "--scenario", "pass"],
                     output=output, source=self.sha, test_names=[CAMERA],
                     repeats=1, timeout=3)
        self.assertEqual(before, (output / "summary.json").read_bytes())
        print("PRIOR_NATIVE_EVIDENCE_DIRECTORY_IMMUTABLE_NO_OVERWRITE")

    def test_12_cli_executes_real_stub_with_json_and_reports_non_native_scope(self):
        out = self.root / "cli"
        args = [sys.executable, str(HERE / "unity_repeat.py"),
                "--project", str(self.project), "--editor-exe", sys.executable,
                "--editor-launch-arg", str(self.stub),
                "--editor-launch-arg=--scenario",
                "--editor-launch-arg=pass",
                "--source-sha", self.sha, "--test", CAMERA,
                "--repeats", "1", "--timeout", "3", "--output", str(out)]
        p = subprocess.run(args, text=True, capture_output=True)
        self.assertEqual(p.returncode, 0, p.stdout + p.stderr)
        self.assertEqual(json.loads(p.stdout)["status"], "REPEAT_PASS")
        data = json.loads((out / "summary.json").read_text())
        self.assertEqual(data["full_suite_g1"], "NOT_ASSESSED_BY_FILTERED_REPLAY")
        self.assertEqual(data["results"][0]["cases"], {CAMERA: "Passed"})
        print("ACTUAL_COMMAND_LINE_SUBPROCESS_JSON_REPLAY_SCOPE_NOT_NATIVE_UNITY")

if __name__ == "__main__":
    unittest.main(verbosity=2)
