#!/usr/bin/env python3
"""CI41-PATHPORT-001 real Python/Git-tree and source-pinned Codex tests.

Synthetic invalid Windows names are represented as REAL GIT TREE OBJECTS,
never checked out on Windows (which would reject those names).
"""
import json
import os
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[3]
sys.path.insert(0, str(HERE))
import pathport

CODEX_SHA = "be2c4b6ac63e04eff5b046e50641ec8d1da4837f"
MAIN_SHA = "5f06af81e2dd9d877e4300259bb3dd008cb9c9d1"


class PathPortTests(unittest.TestCase):
    def test_01_portable_names_pass(self):
        p = ["Assets/Arena/Industrial_Wall.prefab",
             "Assets/Arena/Industrial_Wall.prefab.meta",
             "Assets/Textures/Steel_01.mat",
             "Assets/Scripts/R4_Arm.cs"]
        x = pathport.audit_paths(p)
        self.assertEqual(x["status"], "PASS_PORTABLE_ASSET_PATHS")
        self.assertEqual(x["issues_count"], 0)
        print("VALID_PORTABLE_UNITY_ASSET_PATHS_GREEN")

    def test_02_case_only_file_conflict_detected(self):
        d = pathport.audit_paths(["Assets/Robots/Hull.prefab",
                                  "Assets/Robots/hull.prefab"])
        self.assertEqual(d["status"], "FAIL_PORTABILITY")
        self.assertIn("CROSS_PLATFORM_PATH_COLLISION",
                      {x["kind"] for x in d["issues"]})
        print("LINUX_DISTINCT_WINDOWS_COLLIDING_PREFAB_NAMES_RED")

    def test_03_implicit_directory_case_collision_detected(self):
        d = pathport.audit_paths(["Assets/Robots/Hull.prefab",
                                  "Assets/robots/Wheel.prefab"])
        self.assertEqual(d["status"], "FAIL_PORTABILITY")
        self.assertTrue(any(x["component"] == "robots" and
                            x["kind"] == "CROSS_PLATFORM_PATH_COLLISION"
                            for x in d["issues"]))
        print("UNTRACKED_IMPLICIT_FOLDER_CASE_ALIAS_RED")

    def test_04_unicode_nfc_nfd_alias_detected(self):
        d = pathport.audit_paths(["Assets/Props/Café.prefab",
                                  "Assets/Props/Cafe\u0301.prefab"])
        self.assertEqual(d["status"], "FAIL_PORTABILITY")
        self.assertTrue(any(x["kind"] == "CROSS_PLATFORM_PATH_COLLISION"
                            for x in d["issues"]))
        print("UNICODE_NFC_NFD_ASSET_NAME_COLLISION_RED")

    def test_05_win32_device_stem_even_with_extension_detected(self):
        for comp in ("CON.prefab", "prn.meta", "NUL", "com1.asset",
                     "Lpt9.prefab", "COM¹.prefab", "CONOUT$.asset"):
            with self.subTest(comp=comp):
                d = pathport.audit_paths(["Assets/Weapons/" + comp])
                self.assertIn("WINDOWS_DEVICE_NAME", {x["kind"] for x in d["issues"]})
        print("WINDOWS_RESERVED_DEVICE_NAMES_AND_EXTENSIONS_RED")

    def test_06_trailing_dot_or_space_rejected(self):
        for path in ("Assets/Foo./Bar.asset", "Assets/Back/Bar .prefab",
                     "Assets/Test/Bad ", "Assets/Foo../Wall.mat"):
            with self.subTest(path=path):
                d = pathport.audit_paths([path])
                self.assertTrue(any(x["kind"] == "WINDOWS_TRAILING_SPACE_OR_DOT"
                                    for x in d["issues"]) if
                                path.endswith((" ", ".")) or "Foo." in path else
                                d["status"] == "PASS_PORTABLE_ASSET_PATHS")
        print("WINDOWS_FINAL_DOT_SPACE_COMPONENT_REJECTION_PASS")

    def test_07_win32_invalid_chars_and_control_rejected(self):
        for comp in ("Weapon?.prefab", "UI:Panel.asset", 'Tank"Body.mat',
                     "bad\\name.asset", "bad\x01.asset", "a>b.asset"):
            with self.subTest(comp=comp):
                d = pathport.audit_paths(["Assets/Parts/" + comp])
                self.assertIn("WINDOWS_INVALID_CHARACTER",
                              {x["kind"] for x in d["issues"]})
        print("WIN32_FORBIDDEN_CHARS_CONTROL_AND_BACKSLASH_RED")

    def test_08_win32_utf16_component_limit(self):
        d = pathport.audit_paths(["Assets/Textures/" + "x" * 256 + ".mat"])
        self.assertIn("WINDOWS_COMPONENT_TOO_LONG", {x["kind"] for x in d["issues"]})
        d2 = pathport.audit_paths(["Assets/Textures/" + "x" * 240 + ".mat"])
        self.assertEqual(d2["status"], "PASS_PORTABLE_ASSET_PATHS")
        print("UTF16_COMPONENT_LENGTH_BUDGET_ENFORCED")

    def test_09_bad_sources_and_duplicate_paths_fail_closed(self):
        for paths in ([], ["Assets/Test.cs", "Assets/Test.cs"],
                      ["Other/Test.cs"], ["Assets/a", 7]):
            with self.subTest(paths=paths):
                with self.assertRaises(pathport.PathGateError):
                    pathport.audit_paths(paths)
        with self.assertRaises(pathport.PathGateError):
            pathport.original_asset_paths(ROOT, "main")
        with self.assertRaises(pathport.PathGateError):
            pathport.original_asset_paths(ROOT, "0" * 40)
        print("MISSING_OR_FORGED_GIT_SOURCE_AND_DUPLICATE_INPUT_FAIL_CLOSED")

    def test_10_git_tree_with_invalid_windows_names_without_checkout(self):
        # This is a real immutable Git tree/commit, not an in-memory stand-in.
        # Windows cannot materialize CON.asset as an ordinary checkout.
        with tempfile.TemporaryDirectory(prefix="B0141PathPort_") as d:
            repo = Path(d)
            self._git(repo, "init", "-q")
            obj = self._git(repo, "hash-object", "-w", "--stdin",
                            input=b"%YAML 1.1\n").decode().strip()
            asset_tree = self._git(repo, "mktree", "-z", input=(
                "100644 blob " + obj + "\tCON.asset\0" +
                "100644 blob " + obj + "\tHull.prefab\0" +
                "100644 blob " + obj + "\thull.prefab\0"
            ).encode()).decode().strip()
            root = self._git(repo, "mktree", "-z", input=(
                "040000 tree " + asset_tree + "\tAssets\0"
            ).encode()).decode().strip()
            env = dict(os.environ, GIT_AUTHOR_NAME="CI41", GIT_AUTHOR_EMAIL="ci41@example.invalid",
                       GIT_COMMITTER_NAME="CI41", GIT_COMMITTER_EMAIL="ci41@example.invalid")
            commit = self._git(repo, "commit-tree", root, input=b"source fixture\n",
                               env=env).decode().strip()
            self.assertEqual(len(commit), 40)
            self.assertEqual(len(pathport.original_asset_paths(repo, commit)), 3)
            d = pathport.run(repo, commit)
            self.assertEqual(d["status"], "FAIL_PORTABILITY")
            self.assertIn("WINDOWS_DEVICE_NAME", {x["kind"] for x in d["issues"]})
            self.assertIn("CROSS_PLATFORM_PATH_COLLISION",
                          {x["kind"] for x in d["issues"]})
            args = [sys.executable, str(HERE / "pathport.py"),
                    "--repo", str(repo), "--commit", commit]
            result = subprocess.run(args, capture_output=True, text=True)
            self.assertEqual(result.returncode, 2)
            self.assertEqual(json.loads(result.stdout)["original_commit_sha"], commit)
        print("ACTUAL_GIT_OBJECTS_CON_AND_CASEFOLD_COLLISION_RED_NO_WINDOWS_CHECKOUT")

    def test_11_original_codex_unity_asset_paths(self):
        for sha in (MAIN_SHA, CODEX_SHA):
            with self.subTest(sha=sha):
                d = pathport.run(ROOT, sha)
                self.assertEqual(d["original_commit_sha"], sha)
                self.assertGreater(d["files_inspected"], 100)
                self.assertIn(d["status"], ("PASS_PORTABLE_ASSET_PATHS", "FAIL_PORTABILITY"))
                self.assertEqual(d["native_unity"], "NOT_RUN")
        print("ORIGINAL_MAIN_AND_CODEX_GIT_SOURCE_TREES_ACTUALLY_SCANNED")

    def test_12_real_codex_cli_json_and_fail_closed_invalid_commit(self):
        args = [sys.executable, str(HERE / "pathport.py"),
                "--repo", str(ROOT), "--commit", CODEX_SHA]
        p = subprocess.run(args, capture_output=True, text=True)
        self.assertIn(p.returncode, (0, 2))
        d = json.loads(p.stdout)
        self.assertIn(d["status"], ("PASS_PORTABLE_ASSET_PATHS", "FAIL_PORTABILITY"))
        self.assertEqual(d["original_commit_sha"], CODEX_SHA)
        self.assertEqual(d["g1"], "NOT_RUN")
        p = subprocess.run(args[:-1] + ["0" * 40],
                           capture_output=True, text=True)
        self.assertEqual(p.returncode, 2)
        self.assertEqual(json.loads(p.stdout)["status"], "FAIL_CLOSED_SOURCE_UNAVAILABLE")
        print("ORIGINAL_CODEX_PATH_REPORT_CLI_MACHINE_READABLE_FAIL_CLOSED")

    @staticmethod
    def _git(repo, *args, input=None, env=None):
        p = subprocess.run(["git", "-C", str(repo), *args], input=input,
                           capture_output=True, env=env)
        if p.returncode:
            raise AssertionError("git failed: " + p.stderr.decode("utf-8", "replace"))
        return p.stdout


if __name__ == "__main__":
    unittest.main(verbosity=2)
