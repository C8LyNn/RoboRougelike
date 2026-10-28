#!/usr/bin/env python3
"""Executed original Git/Unity-YAML regression cases (NO Unity/PhysX)."""
from pathlib import Path
import json
import os
import subprocess
import sys
import tempfile
import unittest

HERE = Path(__file__).resolve().parent
REPO = HERE.parents[3]
sys.path.insert(0, str(HERE))
import unity_ref_gate as gate

COMPONENT_GUID = "a" * 32
SCENE_GUID = "b" * 32
UNKNOWN_PACKAGE_GUID = "c" * 32
MAIN_SHA = "5f06af81e2dd9d877e4300259bb3dd008cb9c9d1"
CODEX_SHA = "682dc0c5c13615577334375ffb6c27be5cdebf4a"


def git(repo, *args):
    result = subprocess.run(["git", "-C", str(repo), *args], capture_output=True,
                            check=True, text=True)
    return result.stdout.strip()


class BaseUnityFixture(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory(prefix="b01-41-P17-")
        self.repo = Path(self.tmp.name)
        git(self.repo, "init", "-q")
        git(self.repo, "config", "user.name", "CI41-test")
        git(self.repo, "config", "user.email", "ci41@example.invalid")
        self.put("Assets/Main.unity", "%YAML 1.1\n--- !u!1 &1\nGameObject:\n"
                 "  m_Component: {fileID: 11500000, guid: " + COMPONENT_GUID + ", type: 3}\n"
                 "  m_Other: {fileID: 0, guid: " + UNKNOWN_PACKAGE_GUID + ", type: 3}\n")
        self.put("Assets/Main.unity.meta", "fileFormatVersion: 2\nguid: " + SCENE_GUID + "\n")
        self.put("Assets/Component.cs", "public class Component {}\n")
        self.put("Assets/Component.cs.meta", "fileFormatVersion: 2\nguid: " + COMPONENT_GUID + "\n")
        self.base = self.commit("original fixture")

    def tearDown(self):
        self.tmp.cleanup()

    def put(self, path, data):
        f = self.repo / path
        f.parent.mkdir(parents=True, exist_ok=True)
        f.write_text(data, encoding="utf-8")

    def commit(self, message):
        git(self.repo, "add", "-A")
        git(self.repo, "commit", "-qm", message)
        return git(self.repo, "rev-parse", "HEAD")

    def test_01_original_unmodified_yaml_still_references_deleted_script_RED(self):
        (self.repo / "Assets/Component.cs").unlink()
        (self.repo / "Assets/Component.cs.meta").unlink()
        head = self.commit("remove script and its .meta only")
        result = gate.check(self.repo, self.base, head)
        self.assertEqual(result["status"], "FAIL_DANGLING_RETIRED_LOCAL_GUID")
        self.assertEqual(result["retired_local_guids"], [COMPONENT_GUID])
        self.assertEqual(result["dangling_references"][0]["file"], "Assets/Main.unity")
        self.assertEqual(result["dangling_references"][0]["former_meta"], "Assets/Component.cs.meta")
        self.assertEqual(result["g1"], "NOT_RUN")
        print("ORIGINAL_GIT_DELETED_SCRIPT_STILL_REFERENCED_BY_UNCHANGED_SCENE=RED")

    def test_02_clear_reference_before_removing_script_GREEN(self):
        p = self.repo / "Assets/Main.unity"
        p.write_text(p.read_text().replace(COMPONENT_GUID, "0" * 32))
        (self.repo / "Assets/Component.cs").unlink()
        (self.repo / "Assets/Component.cs.meta").unlink()
        head = self.commit("clean scene and remove script")
        self.assertEqual(gate.check(self.repo, self.base, head)["status"],
                         "PASS_NO_DANGLING_RETIRED_LOCAL_GUID")
        print("REMOVED_SCRIPT_WITH_CLEANED_SCENE=PASS")

    def test_03_relocated_meta_preserves_GUID_GREEN(self):
        (self.repo / "Assets/Sub").mkdir()
        (self.repo / "Assets/Component.cs").rename(self.repo / "Assets/Sub/Component.cs")
        (self.repo / "Assets/Component.cs.meta").rename(self.repo / "Assets/Sub/Component.cs.meta")
        head = self.commit("relocate script same GUID")
        result = gate.check(self.repo, self.base, head)
        self.assertEqual(result["retired_local_guids"], [])
        self.assertEqual(result["status"], "PASS_NO_DANGLING_RETIRED_LOCAL_GUID")
        print("META_RELOCATION_PRESERVE_GUID=PASS")

    def test_04_changed_guid_without_scene_update_RED(self):
        self.put("Assets/Component.cs.meta", "fileFormatVersion: 2\nguid: " + "d" * 32 + "\n")
        head = self.commit("bad manual .meta GUID rotation")
        result = gate.check(self.repo, self.base, head)
        self.assertEqual(result["status"], "FAIL_DANGLING_RETIRED_LOCAL_GUID")
        self.assertEqual(len(result["dangling_references"]), 1)
        print("CHANGED_ORIGINAL_META_GUID_WITH_UNCHANGED_SCENE=RED")

    def test_05_unknown_package_and_untracked_references_NOT_FALSE_RED(self):
        self.put("Assets/Additional.asset", "%YAML 1.1\n--- !u!114 &11400000\nMonoBehaviour:\n"
                 "  m_Script: {fileID: 11500000, guid: " + UNKNOWN_PACKAGE_GUID + ", type: 3}\n")
        head = self.commit("external/package reference only")
        result = gate.check(self.repo, self.base, head)
        self.assertEqual(result["status"], "PASS_NO_DANGLING_RETIRED_LOCAL_GUID")
        self.assertEqual(result["native_unity"], "NOT_RUN")
        print("UNKNOWN_PACKAGE_GUID_CANNOT_BE_CALLED_DANGLING=PASS_LIMITED_SCOPE")

    def test_06_empty_or_fake_source_and_nonancestor_FAIL_CLOSED(self):
        with self.assertRaises(gate.GateError):
            gate.check(self.repo, "main", self.base)
        with self.assertRaises(gate.GateError):
            gate.check(self.repo, "0" * 40, self.base)
        head = self.commit("newer")
        with self.assertRaises(gate.GateError):
            gate.check(self.repo, head, self.base)
        print("NONEXISTENT_OR_REVERSED_GIT_HISTORY=FAIL_CLOSED")

    def test_07_real_original_repository_and_codex_asset_handoff(self):
        self.assertGreater(len(gate.tracked(REPO, MAIN_SHA)), 50)
        # Real Unity 6 source; Run.asset is original and references a script
        # and local game upgrade assets by GUID. Do not fake a synthetic scene.
        run = gate.read_blob(REPO, MAIN_SHA, "Assets/Resources/R1/Run.asset")
        self.assertTrue(run.startswith(b"%YAML"))
        self.assertIn(b"m_Script: {fileID: 11500000, guid:", run)
        self.assertIn(b"guid:", gate.read_blob(REPO, MAIN_SHA, "Assets/Resources/R1/Run.asset.meta"))
        outcome = gate.check(REPO, MAIN_SHA, CODEX_SHA)
        # An actual broken reference would be a real integration blocker.
        self.assertEqual(outcome["status"], "PASS_NO_DANGLING_RETIRED_LOCAL_GUID", outcome)
        self.assertEqual(outcome["g1"], "NOT_RUN")
        print("ORIGINAL_MAIN_TO_CODEX_ASSET_HANDOFF_NO_RETIRED_LOCAL_GUID_DANGLING=PASS")


if __name__ == "__main__":
    unittest.main(verbosity=2)
