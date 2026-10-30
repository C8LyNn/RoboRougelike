#!/usr/bin/env python3
"""Physical original Git-object regression tests for CI41-ASMGRAPH-001.

Each test generates a real commit and reads the committed .asmdef and .meta
blobs with git ls-tree/cat-file, not a mocked filesystem scan.
"""
from __future__ import annotations

import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import asmdef_graph_gate as gate

A = "RoboRougelike.MOV010Original"
B = "RoboRougelike.MOV010Original.EditModeTests"
C = "RoboRougelike.MOV010Original.PlayModeTests"
GUID_A = "1" * 32
GUID_B = "2" * 32
GUID_C = "3" * 32


def command(repo, *args):
    run = subprocess.run(["git", "-C", str(repo), *args],
                         capture_output=True, text=True)
    if run.returncode:
        raise AssertionError(run.stderr)
    return run.stdout.strip()


def sample(name, refs=None, **fields):
    return dict({"name": name, "references": refs or [], "includePlatforms": [],
                 "excludePlatforms": [], "overrideReferences": False,
                 "precompiledReferences": [], "defineConstraints": []}, **fields)


def edit(name=B, refs=None):
    return sample(name, refs or [A], includePlatforms=["Editor"],
                  overrideReferences=True, precompiledReferences=["nunit.framework.dll"],
                  defineConstraints=["UNITY_INCLUDE_TESTS"])


def play(name=C, refs=None):
    return sample(name, refs or [A], overrideReferences=True,
                  precompiledReferences=["nunit.framework.dll"],
                  defineConstraints=["UNITY_INCLUDE_TESTS"])


class RealGitGraphTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix="ci41-asmgraph-")
        self.repo = Path(self.temp.name)
        command(self.repo, "init", "-q")
        command(self.repo, "config", "user.name", "CI41 Test")
        command(self.repo, "config", "user.email", "ci41@example.invalid")
        self.files: dict[str, str] = {}
        self.add(A, GUID_A, sample(A))
        self.add(B, GUID_B, edit())
        self.add(C, GUID_C, play())

    def tearDown(self):
        self.temp.cleanup()

    def path(self, name):
        return "Assets/MOV010/" + name + ".asmdef"

    def add(self, name, guid, obj):
        self.files[self.path(name)] = json.dumps(obj, sort_keys=True)
        self.files[self.path(name) + ".meta"] = (
            "fileFormatVersion: 2\n" + "guid: " + guid +
            "\nAssemblyDefinitionImporter:\n  userData: \n")

    def overwrite(self, name, obj):
        self.files[self.path(name)] = json.dumps(obj)

    def commit(self):
        for p, body in self.files.items():
            path = self.repo / p
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text(body, encoding="utf-8")
        command(self.repo, "add", "--", "Assets")
        command(self.repo, "commit", "-qm", "Original test-only asmdef input")
        return command(self.repo, "rev-parse", "HEAD")

    def results(self, *, count=None):
        sha = self.commit()
        return gate.validate(self.repo, sha, require_count=count), sha

    def assert_code(self, report, code):
        self.assertEqual(report["result"], "FAIL_SOURCE_GRAPH", report)
        self.assertIn(code, [e["code"] for e in report["errors"]], report)

    def test_01_original_real_git_three_assemblies_pass(self):
        report, sha = self.results(count=3)
        self.assertEqual(report["result"], "PASS_SOURCE_GRAPH", report)
        self.assertEqual(report["guid_count"], 3)
        self.assertEqual(report["tracked_asmdef_count"], 3)
        self.assertEqual(report["dependency_edge_count"], 2)
        self.assertEqual(report["source_sha"], sha)
        self.assertEqual(report["native_unity"], "NOT_RUN")
        print("PHYSICAL_GIT_TREE_3_ASMDEFS_2_EDGES_PASS_NOT_UNITY")

    def test_02_detect_missing_original_local_assembly_reference(self):
        self.overwrite(B, edit(refs=[A, "RoboRougelike.DoesNotExist"]))
        self.assert_code(self.results()[0], "MISSING_LOCAL_ASSEMBLY_REFERENCE")

    def test_03_detect_exact_name_duplicate_with_different_path(self):
        other = "Assets/Extra/Other.asmdef"
        self.files[other] = json.dumps(sample(A))
        self.files[other + ".meta"] = "guid: " + "a" * 32 + "\n"
        self.assert_code(self.results()[0], "DUPLICATE_ASSEMBLY_NAME")

    def test_04_detect_casefold_collision_from_real_git(self):
        other = "Assets/Extra/Other.asmdef"
        self.files[other] = json.dumps(sample(A.lower()))
        self.files[other + ".meta"] = "guid: " + "a" * 32 + "\n"
        self.assert_code(self.results()[0], "CASE_INSENSITIVE_ASSEMBLY_COLLISION")

    def test_05_detect_invalid_json_not_bypass(self):
        self.files[self.path(A)] = '{"name": '
        self.assert_code(self.results()[0], "INVALID_ASMDEF_JSON")

    def test_06_detect_cyclic_dependency(self):
        self.overwrite(A, sample(A, [B]))
        self.assert_code(self.results()[0], "ASSEMBLY_REFERENCE_CYCLE")

    def test_07_detect_direct_self_reference(self):
        self.overwrite(A, sample(A, [A]))
        self.assert_code(self.results()[0], "SELF_REFERENCE_ASSEMBLY")

    def test_08_detect_editor_test_assembly_not_editor_platform(self):
        self.overwrite(B, sample(B, [A], overrideReferences=True,
                                 precompiledReferences=["nunit.framework.dll"],
                                 defineConstraints=["UNITY_INCLUDE_TESTS"]))
        self.assert_code(self.results()[0], "EDITMODE_ASSEMBLY_MUST_INCLUDE_EDITOR_ONLY")

    def test_09_detect_editor_assembly_used_by_runtime(self):
        self.overwrite(A, sample(A, [B]))
        self.assert_code(self.results()[0], "RUNTIME_REFERENCES_EDITOR_ONLY_ASSEMBLY")

    def test_10_detect_missing_test_constraints_and_nunit(self):
        self.overwrite(B, sample(B, [A], includePlatforms=["Editor"]))
        report = self.results()[0]
        self.assert_code(report, "TEST_ASSEMBLY_MISSING_UNITY_INCLUDE_TESTS")
        self.assert_code(report, "TEST_ASSEMBLY_NUNIT_REFERENCE_MISSING")
        self.assert_code(report, "TEST_ASSEMBLY_OVERRIDE_REFERENCES_REQUIRED")

    def test_11_detect_asmdef_meta_missing(self):
        del self.files[self.path(A)+".meta"]
        self.assert_code(self.results()[0], "MISSING_ASMDEF_META")

    def test_12_detect_duplicate_meta_guid(self):
        self.files[self.path(C)+".meta"] = "guid: " + GUID_A + "\n"
        self.assert_code(self.results()[0], "DUPLICATE_ASMDEF_GUID")

    def test_13_guid_assembly_reference_resolves_real_meta(self):
        self.overwrite(B, edit(refs=["GUID:" + GUID_A]))
        report = self.results()[0]
        self.assertEqual(report["result"], "PASS_SOURCE_GRAPH", report)
        self.assertEqual(report["dependency_edge_count"], 2)

    def test_14_missing_guid_reference_rejects(self):
        self.overwrite(B, edit(refs=["GUID:" + "a"*32]))
        self.assert_code(self.results()[0], "MISSING_ASSEMBLY_REFERENCE_GUID")

    def test_15_invalid_guid_reference_rejects(self):
        self.overwrite(B, edit(refs=["GUID:this-is-malformed"]))
        self.assert_code(self.results()[0], "INVALID_ASSEMBLY_REFERENCE_GUID")

    def test_16_duplicate_reference_and_platform_conflict(self):
        self.overwrite(A, sample(A, [C,C], includePlatforms=["Editor"],
                                 excludePlatforms=["LinuxStandalone64"]))
        report=self.results()[0]
        self.assert_code(report, "DUPLICATE_ASSEMBLY_REFERENCE")
        self.assert_code(report, "INCLUDE_AND_EXCLUDE_PLATFORMS")

    def test_17_pin_exact_source_commit_not_other_sha(self):
        sha=self.commit()
        with self.assertRaisesRegex(gate.GraphGateError, "PINNED_40_HEX"):
            gate.validate(self.repo, "HEAD")
        with self.assertRaisesRegex(gate.GraphGateError, "GIT_FAILED"):
            gate.validate(self.repo, "f"*40)
        self.assertEqual(gate.validate(self.repo, sha)["result"],"PASS_SOURCE_GRAPH")

    def test_18_uncommitted_source_changes_do_not_change_original_git_object(self):
        sha=self.commit()
        (self.repo / self.path(A)).write_text('malformed json but UNCOMMITTED')
        original = gate.validate(self.repo, sha)
        self.assertEqual(original["result"], "PASS_SOURCE_GRAPH", original)
        print("EXACT_ORIGINAL_GIT_BLOBS_NOT_WORKTREE_DIRTY_DATA")

    def test_19_detect_count_drift_and_no_sources(self):
        report,_=self.results(count=4)
        self.assert_code(report, "ASMDEF_COUNT_DRIFT")
        with tempfile.TemporaryDirectory(prefix="ci41-empty-") as p:
            empty=Path(p)
            command(empty, "init", "-q")
            command(empty, "config", "user.email", "a@example.invalid")
            command(empty, "config", "user.name", "A")
            (empty / "README.md").write_text("no assemblies")
            command(empty, "add", ".")
            command(empty, "commit", "-qm", "no assemblies")
            result=gate.validate(empty, command(empty,"rev-parse","HEAD"))
            self.assert_code(result,"NO_ASMDEFS_FOUND")

    def test_20_runtime_external_packages_are_explicitly_unverified_not_claimed_fail(self):
        self.overwrite(A, sample(A, ["Unity.TextMeshPro"]))
        report=self.results()[0]
        self.assertEqual(report["result"], "PASS_SOURCE_GRAPH", report)
        self.assertEqual(report["warnings"][0]["code"],
                         "UNRESOLVED_EXTERNAL_ASSEMBLY_REFERENCE")
        print("EXTERNAL_PACKAGE_REF_NOT_CLAIMED_VALIDATED")

    def test_21_complete_real_cli_source_replay_and_json(self):
        sha=self.commit()
        out=self.repo/"evidence"/"asmgraph.json"
        argv=[sys.executable, str(HERE/"asmdef_graph_gate.py"),
              "--repo",str(self.repo),"--source-sha",sha,
              "--require-count","3","--out",str(out)]
        proc=subprocess.run(argv, capture_output=True,text=True)
        self.assertEqual(proc.returncode,0,proc.stdout+proc.stderr)
        report=json.loads(out.read_text())
        self.assertEqual(report["result"],"PASS_SOURCE_GRAPH")
        self.assertEqual(report["native_unity"],"NOT_RUN")
        self.assertEqual(report["tracked_asmdef_count"],3)
        print("ORIGINAL_PHYSICAL_GIT_OBJECTS_STANDALONE_CLI_JSON_PASS")

    def test_22_cli_missing_count_return_code_two(self):
        sha=self.commit()
        argv=[sys.executable, str(HERE/"asmdef_graph_gate.py"),
              "--repo",str(self.repo),"--source-sha",sha,
              "--require-count","27"]
        proc=subprocess.run(argv,capture_output=True,text=True)
        self.assertEqual(proc.returncode,2)
        self.assertEqual(json.loads(proc.stdout)["result"],"FAIL_SOURCE_GRAPH")
        print("ORIGINAL_PHYSICAL_GIT_OBJECTS_CLI_NEGATIVE_DRIFT_EXIT2")


if __name__ == "__main__":
    unittest.main(verbosity=2)
