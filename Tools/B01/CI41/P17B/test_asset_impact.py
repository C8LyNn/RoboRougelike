#!/usr/bin/env python3
"""CI41-ASSETIMPACT-001 regression on authentic temporary Git+Unity YAML data.

Fixture scenes are deliberately minimal Unity-serialized YAML, not native Unity
tests. One separate test scans original, immutable Codex source commits.
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
import asset_impact as impact

OLD_CODEX = "a2ccd055070a963805cba0779b79f18489f4794f"
NEW_CODEX = "16c2668104952f1b8db304cc7817b897fb48a9b1"
A = "a" * 32
B = "b" * 32
C = "c" * 32
D = "d" * 32
E = "e" * 32
F = "f" * 32


def git(repo, *args):
    p = subprocess.run(["git", "-C", str(repo), *args], text=True,
                       capture_output=True, check=True)
    return p.stdout.strip()


def meta(guid):
    return "fileFormatVersion: 2\nguid: " + guid + "\n"


def prefab(guid):
    return ("%YAML 1.1\n--- !u!1 &1\nGameObject:\n"
            "  m_Component: {fileID: 11500000, guid: " + guid + ", type: 3}\n")


class OriginalGitImpactTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix="CI41-IMPACT-")
        self.repo = Path(self.temp.name)
        git(self.repo, "init", "-q")
        git(self.repo, "config", "user.name", "ci41-fixture")
        git(self.repo, "config", "user.email", "ci41-fixture@example.invalid")
        self.put("Assets/Scripts/Fighter.cs", "public class Fighter { int HP = 100; }\n")
        self.put("Assets/Scripts/Fighter.cs.meta", meta(A))
        self.put("Assets/Prefabs/Fighter.prefab", prefab(A))
        self.put("Assets/Prefabs/Fighter.prefab.meta", meta(B))
        self.put("Assets/Scenes/Arena.unity", prefab(B))
        self.put("Assets/Scenes/Arena.unity.meta", meta(C))
        self.put("Assets/Scenes/Unaffected.unity", prefab(E))
        self.put("Assets/Scenes/Unaffected.unity.meta", meta(D))
        self.put("Assets/Materials/Steel.mat", prefab(E))
        self.put("Assets/Materials/Steel.mat.meta", meta(F))
        self.base = self.commit("initial original tracked Unity files")

    def tearDown(self):
        self.temp.cleanup()

    def put(self, path, content):
        f = self.repo / path
        f.parent.mkdir(parents=True, exist_ok=True)
        f.write_text(content, encoding="utf-8")

    def commit(self, message):
        git(self.repo, "add", "-A")
        git(self.repo, "commit", "-qm", message)
        return git(self.repo, "rev-parse", "HEAD")

    def test_01_changed_script_impacts_prefab_then_scene_with_exact_chain(self):
        self.put("Assets/Scripts/Fighter.cs", "public class Fighter { int HP = 125; }\n")
        h = self.commit("change original gameplay script")
        d = impact.impact(self.repo, self.base, h)
        self.assertEqual(d["status"], "PASS_IMPACT_GRAPH_COMPUTED")
        self.assertEqual(d["changed_roots"], ["Assets/Scripts/Fighter.cs"])
        self.assertEqual(d["affected_prefabs"], [{
            "path": "Assets/Prefabs/Fighter.prefab",
            "via": ["Assets/Scripts/Fighter.cs", "Assets/Prefabs/Fighter.prefab"]}])
        self.assertEqual(d["affected_scenes"], [{
            "path": "Assets/Scenes/Arena.unity",
            "via": ["Assets/Scripts/Fighter.cs", "Assets/Prefabs/Fighter.prefab",
                    "Assets/Scenes/Arena.unity"]}])
        self.assertFalse(any("Unaffected" in x["path"] for x in d["affected_scenes"]))
        self.assertEqual(d["native_unity"], "NOT_RUN")
        print("SCRIPT_TO_PREFAB_TO_SCENE_ORIGINAL_GIT_TRANSITIVE_IMPACT_PASS")

    def test_02_direct_scene_edit_marks_scene_and_not_other_scene(self):
        self.put("Assets/Scenes/Arena.unity", prefab(B) + "  m_Name: New arena\n")
        h = self.commit("edit original scene")
        d = impact.impact(self.repo, self.base, h)
        self.assertEqual(d["affected_scenes"], [{
            "path": "Assets/Scenes/Arena.unity", "via": ["Assets/Scenes/Arena.unity"]}])
        self.assertEqual(d["affected_prefabs"], [])
        print("DIRECT_SCENE_CHANGE_TARGETS_ONLY_ITS_OWN_SMOKE_PASS")

    def test_03_material_change_direct_or_transitive_through_prefab(self):
        # Original (unmodified) scene references original prefab; changed
        # material is subsequently referenced by the prefab without GUI edits.
        self.put("Assets/Prefabs/Fighter.prefab", prefab(A) + "  m_Material: {guid: " + F + "}\n")
        material_base = self.commit("original prefab with steel dependency")
        self.put("Assets/Materials/Steel.mat", prefab(E) + "  m_Name: SteelUpdated\n")
        h = self.commit("change steel only")
        d = impact.impact(self.repo, material_base, h)
        self.assertEqual(d["affected_scenes"][0]["via"], [
            "Assets/Materials/Steel.mat", "Assets/Prefabs/Fighter.prefab", "Assets/Scenes/Arena.unity"])
        print("MATERIAL_TO_PREFAB_TO_SCENE_ORIGINAL_GIT_IMPACT_PASS")

    def test_04_cycle_does_not_repeat_or_loop(self):
        self.put("Assets/Prefabs/Fighter.prefab", prefab(A) + "  m_Other: {guid: " + C + "}\n")
        h = self.commit("create benign scene-prefab cycle")
        # New cycle is a changed prefab. Reach scene exactly once.
        d = impact.impact(self.repo, self.base, h)
        self.assertEqual(len(d["affected_scenes"]), 1)
        self.assertEqual(d["affected_scenes"][0]["path"], "Assets/Scenes/Arena.unity")
        print("CYCLIC_SERIALIZED_GRAPH_TERMINATES_WITH_SINGLE_SCENE_PASS")

    def test_05_removed_or_missing_meta_fails_closed(self):
        (self.repo / "Assets/Scripts/Fighter.cs").unlink()
        (self.repo / "Assets/Scripts/Fighter.cs.meta").unlink()
        h = self.commit("delete script still referenced")
        d = impact.impact(self.repo, self.base, h)
        self.assertEqual(d["status"], "INCOMPLETE_FAIL_CLOSED")
        self.assertIn("Assets/Scripts/Fighter.cs", d["removed_assets"])
        self.assertEqual(d["affected_scenes"], [])
        print("DELETED_ASSET_REQUIRES_SEPARATE_RETIREMENT_QA_FAIL_CLOSED")
        # A new asset without its tracked meta is never accepted as complete.
        self.put("Assets/NoMeta.cs", "class NoMeta {}\n")
        h2 = self.commit("add asset without meta")
        d2 = impact.impact(self.repo, h, h2)
        self.assertEqual(d2["status"], "INCOMPLETE_FAIL_CLOSED")
        self.assertIn("Assets/NoMeta.cs", d2["unmapped_assets"])

    def test_06_real_folder_meta_and_unknown_external_package_guid(self):
        self.put("Assets/Extra.meta", meta("1" * 32))
        self.put("Assets/Extra/Optional.cs", "class Optional {}\n")
        self.put("Assets/Extra/Optional.cs.meta", meta("2" * 32))
        h = self.commit("add folder with Unity meta")
        d = impact.impact(self.repo, self.base, h)
        self.assertEqual(d["status"], "PASS_IMPACT_GRAPH_COMPUTED", d)
        self.assertEqual(d["affected_scenes"], [])
        self.assertNotIn("Assets/Extra", d["changed_roots"])
        print("UNITY_FOLDER_META_HANDLED_WITHOUT_FALSE_NATIVE_TEST_TARGET")

    def test_07_bad_source_or_stale_ancestry_never_passes(self):
        with self.assertRaises(impact.ImpactError):
            impact.impact(self.repo, "main", self.base)
        with self.assertRaises(impact.ImpactError):
            impact.impact(self.repo, self.base, "0" * 40)
        self.put("Assets/Scripts/Fighter.cs", "public class Fighter { int HP = 99; }\n")
        h = self.commit("make real descendant")
        with self.assertRaises(impact.ImpactError):
            impact.impact(self.repo, h, self.base)
        print("INVALID_OR_REVERSED_GIT_SHA_FAIL_CLOSED")

    def test_08_original_codex_enemy_code_delta_scanned_no_unity_claim(self):
        before = impact.tree_blobs(ROOT, OLD_CODEX)
        after = impact.tree_blobs(ROOT, NEW_CODEX)
        self.assertIn("Assets/Pack/P12_PipelineSafeEnemyTurret.cs", before)
        self.assertIn("Assets/Pack/P12_PipelineSafeEnemyTurret.cs", after)
        self.assertNotEqual(before["Assets/Pack/P12_PipelineSafeEnemyTurret.cs"],
                            after["Assets/Pack/P12_PipelineSafeEnemyTurret.cs"])
        report = impact.impact(ROOT, OLD_CODEX, NEW_CODEX)
        self.assertEqual(report["status"], "PASS_IMPACT_GRAPH_COMPUTED", report)
        self.assertIn("Assets/Pack/P12_PipelineSafeEnemyTurret.cs", report["changed_roots"])
        self.assertEqual(report["native_unity"], "NOT_RUN")
        self.assertEqual(report["diff_authority"], "ORIGINAL_GIT_OBJECTS")
        print("REAL_CODEX_ENEMY_AI_SHA_ORIGINAL_ASSET_IMPACT_SCAN_PASS")

    def test_09_original_cli_exit_zero_for_impact_two_for_deletion(self):
        self.put("Assets/Scripts/Fighter.cs", "class Fighter { int HP = 151; }\n")
        changed = self.commit("change script")
        args = [sys.executable, str(HERE / "asset_impact.py"), "--repo", str(self.repo),
                "--base", self.base, "--head", changed]
        p = subprocess.run(args, capture_output=True, text=True)
        self.assertEqual(p.returncode, 0, p.stderr)
        self.assertEqual(len(json.loads(p.stdout)["affected_scenes"]), 1)
        (self.repo / "Assets/Scripts/Fighter.cs").unlink()
        removed = self.commit("delete tracked script")
        p = subprocess.run(args[:-1] + [removed], capture_output=True, text=True)
        self.assertEqual(p.returncode, 2)
        self.assertEqual(json.loads(p.stdout)["status"], "INCOMPLETE_FAIL_CLOSED")
        print("EXACT_GIT_CLI_SMOKE_TARGET_EXIT_AND_DELETION_FAIL_CLOSED_PASS")


if __name__ == "__main__":
    unittest.main(verbosity=2)
