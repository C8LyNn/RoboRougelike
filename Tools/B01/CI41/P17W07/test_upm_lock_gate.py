#!/usr/bin/env python3
"""Genuine Git-commit fixtures for CI41-UPMLOCK-001; NOT Unity runtime tests."""
from __future__ import annotations

import copy
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parent))
import upm_lock_gate as gate

FOO = "com.unity.foo"
BAR = "com.unity.bar"
MID = "com.unity.mid"
LEAF = "com.unity.leaf"


def command(repo, *args):
    p = subprocess.run(["git", "-C", str(repo), *args],
                       capture_output=True, text=True)
    if p.returncode:
        raise RuntimeError("git " + repr(args) + ":" + p.stderr)
    return p.stdout.strip()


def entry(version, depth, source="builtin", deps=None):
    x = {"version": version, "depth": depth, "source": source,
         "dependencies": deps if deps is not None else {}}
    if source == "registry":
        x["url"] = "https://packages.unity.com"
    return x


class OriginalGitUPMLockTest(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory(prefix="ci41-upm-")
        self.repo = Path(self.tmp.name)
        command(self.repo, "init", "-q")
        command(self.repo, "config", "user.name", "CI41")
        command(self.repo, "config", "user.email", "ci41@example.invalid")
        self.manifest = {"dependencies": {FOO: "1.2.3", BAR: "2.0.0"}}
        self.lock = {"dependencies": {
            FOO: entry("1.2.3", 0, "registry", {MID: "1.0.0"}),
            BAR: entry("2.0.0", 0),
            MID: entry("1.1.0", 1, "registry", {LEAF: "1.0.0"}),
            LEAF: entry("1.0.0", 2)}}
        self.editor = "m_EditorVersion: 6000.3.14f1\n"
        self.override = {}

    def tearDown(self):
        self.tmp.cleanup()

    def commit(self):
        data = {
            "Packages/manifest.json": json.dumps(self.manifest, sort_keys=True),
            "Packages/packages-lock.json": json.dumps(self.lock, sort_keys=True),
            "ProjectSettings/ProjectVersion.txt": self.editor,
        }
        data.update(self.override)
        for path, raw in data.items():
            target = self.repo / path
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_text(raw, encoding="utf-8")
        command(self.repo, "add", ".")
        command(self.repo, "commit", "-qm", "physical original UPM fixture")
        return command(self.repo, "rev-parse", "HEAD")

    def report(self, **options):
        sha = self.commit()
        src = gate.load_original(self.repo, sha)
        manifest = gate.decode(src[gate.PATHS[0]], gate.PATHS[0])
        lock = gate.decode(src[gate.PATHS[1]], gate.PATHS[1])
        return gate.validate(manifest, lock, src[gate.PATHS[2]], sha, **options)

    def assert_error(self, code):
        result = self.report()
        self.assertEqual(result["result"], "FAIL_SOURCE_UPM_LOCK", result)
        self.assertIn(code, [x["code"] for x in result["errors"]], result)

    def test_01_real_git_nested_four_packages_pass(self):
        x = self.report(expected_direct=2, expected_resolved=4)
        self.assertEqual(x["result"], "PASS_SOURCE_UPM_LOCK", x)
        self.assertEqual((x["manifest_count"],x["lock_count"],
                          x["transitive_count"],x["dependency_edge_count"]),(2,4,2,2))
        self.assertEqual(x["native_unity"],"NOT_RUN")
        print("PHYSICAL_ORIGINAL_GIT_UPM_DIRECT_TWO_LOCK_FOUR_PASS_NO_UNITY")

    def test_02_resolution_can_upgrade_transitive_minimum(self):
        x=self.report()
        self.assertEqual(x["result"],"PASS_SOURCE_UPM_LOCK", x)
        self.assertEqual([z["code"] for z in x["warnings"]],
                         ["UPM_TRANSITIVE_REQUEST_DIFFERS_FROM_RESOLVED"])
        print("REAL_GIT_TRANSITIVE_RESOLVED_VERSION_DIFFERS_IS_WARNING")

    def test_03_missing_direct_package_in_lock_reject(self):
        del self.lock["dependencies"][FOO]
        self.assert_error("MISSING_DIRECT_LOCK_ENTRY")

    def test_04_direct_version_drift_reject(self):
        self.lock["dependencies"][FOO]["version"]="1.2.4"
        self.assert_error("DIRECT_VERSION_LOCK_MISMATCH")

    def test_05_direct_nonzero_depth_reject(self):
        self.lock["dependencies"][BAR]["depth"]=1
        self.assert_error("DIRECT_DEPENDENCY_DEPTH_NOT_ZERO")

    def test_06_missing_transitive_package_reject(self):
        del self.lock["dependencies"][MID]
        self.assert_error("MISSING_TRANSITIVE_LOCK_ENTRY")

    def test_07_orphan_lock_package_reject(self):
        self.lock["dependencies"]["com.unity.orphan"]=entry("1.0.0",2)
        self.assert_error("ORPHAN_LOCK_ENTRY")

    def test_08_original_shortest_depth_mismatch_reject(self):
        self.lock["dependencies"][LEAF]["depth"]=1
        self.assert_error("LOCK_DEPTH_NOT_SHORTEST_REACHABLE")

    def test_09_secondary_shorter_path_recalculates_depth(self):
        self.lock["dependencies"][BAR]["dependencies"]={LEAF:"1.0.0"}
        self.lock["dependencies"][LEAF]["depth"]=1
        x=self.report()
        self.assertEqual(x["result"],"PASS_SOURCE_UPM_LOCK",x)

    def test_10_unsatisfied_registry_https_requirement_reject(self):
        self.lock["dependencies"][FOO]["url"]="http://packages.unity.com"
        self.assert_error("INSECURE_OR_MISSING_REGISTRY_URL")

    def test_11_credentials_in_registry_uri_reject(self):
        self.lock["dependencies"][FOO]["url"]="https://admin:secret@packages.unity.com"
        self.assert_error("INSECURE_OR_MISSING_REGISTRY_URL")

    def test_12_unsupported_remote_package_source_requires_review(self):
        self.lock["dependencies"][FOO]["source"]="git"
        self.assert_error("UNSUPPORTED_LOCK_ORIGIN_REQUIRES_REVIEW")

    def test_13_noninteger_bool_depth_reject(self):
        self.lock["dependencies"][MID]["depth"]=True
        self.assert_error("INVALID_LOCK_DEPTH")

    def test_14_missing_source_version_reject(self):
        del self.lock["dependencies"][MID]["version"]
        self.assert_error("INVALID_LOCK_VERSION")

    def test_15_missing_transitive_dict_reject(self):
        self.lock["dependencies"][FOO]["dependencies"]=[]
        self.assert_error("INVALID_TRANSITIVE_DEPENDENCY_MAP")

    def test_16_manifest_lock_counts_can_pin_original_source(self):
        x=self.report(expected_direct=3,expected_resolved=5)
        self.assertEqual({e["code"] for e in x["errors"]},
                         {"MANIFEST_PACKAGE_COUNT_DRIFT","LOCK_PACKAGE_COUNT_DRIFT"})

    def test_17_real_git_json_duplicate_keys_rejected(self):
        self.override["Packages/manifest.json"]='{"dependencies":{},"dependencies":{}}\n'
        sha=self.commit()
        data=gate.load_original(self.repo,sha)
        with self.assertRaisesRegex(gate.LockGateError,"DUPLICATE_JSON_KEY"):
            gate.decode(data["Packages/manifest.json"],"Packages/manifest.json")

    def test_18_invalid_original_json_rejected(self):
        self.override["Packages/packages-lock.json"]='{"dependencies":'
        sha=self.commit()
        data=gate.load_original(self.repo,sha)
        with self.assertRaisesRegex(gate.LockGateError,"INVALID_JSON"):
            gate.decode(data["Packages/packages-lock.json"],"Packages/packages-lock.json")

    def test_19_exact_git_sha_required_and_missing_commit_rejected(self):
        sha=self.commit()
        with self.assertRaisesRegex(gate.LockGateError,"EXACT_LOWERCASE_40_HEX_COMMIT_REQUIRED"):
            gate.load_original(self.repo,"HEAD")
        with self.assertRaisesRegex(gate.LockGateError,"GIT_COMMAND_FAILED"):
            gate.load_original(self.repo,"f"*40)
        self.assertEqual(gate.decode(gate.load_original(self.repo,sha)[gate.PATHS[0]],
                                     "manifest")["dependencies"],self.manifest["dependencies"])

    def test_20_uncommitted_worktree_changes_never_change_original_commit(self):
        sha=self.commit()
        (self.repo/"Packages/manifest.json").write_text('{"dependencies":{"com.unity.tampered":"0.0.0"}}')
        raw=gate.load_original(self.repo,sha)
        self.assertEqual(gate.decode(raw["Packages/manifest.json"],"manifest"),self.manifest)
        print("REAL_GIT_ORIGINAL_PACKAGE_BYTES_IMMUTABLE_NOT_DIRTY_WORKTREE")

    def test_21_missing_original_lock_file_is_hard_fail(self):
        sha=self.commit()
        command(self.repo,"rm","Packages/packages-lock.json")
        command(self.repo,"commit","-qm","remove original lock file")
        new=command(self.repo,"rev-parse","HEAD")
        with self.assertRaisesRegex(gate.LockGateError,"MISSING_COMMITTED_UPM_FILES"):
            gate.load_original(self.repo,new)
        self.assertIn("Packages/packages-lock.json",gate.load_original(self.repo,sha))

    def test_22_actual_cli_writes_successful_source_json(self):
        sha=self.commit()
        out=self.repo/"evidence"/"source-lock.json"
        p=subprocess.run([sys.executable,str(Path(gate.__file__)),
                          "--repo",str(self.repo),"--source-sha",sha,
                          "--expected-direct","2","--expected-resolved","4",
                          "--out",str(out)],capture_output=True,text=True)
        self.assertEqual(p.returncode,0,p.stdout+p.stderr)
        x=json.loads(out.read_text())
        self.assertEqual(x["result"],"PASS_SOURCE_UPM_LOCK")
        self.assertEqual(x["unity_editor"],"6000.3.14f1")
        self.assertEqual(x["package_resolver"],"NOT_RUN")
        print("PHYSICAL_GIT_OBJECTS_STANDALONE_UPM_CLI_JSON_PASS")

    def test_23_actual_cli_fail_closed_original_drift_exit2(self):
        self.lock["dependencies"][FOO]["version"]="99.0.0"
        sha=self.commit()
        p=subprocess.run([sys.executable,str(Path(gate.__file__)),
                          "--repo",str(self.repo),"--source-sha",sha],
                          capture_output=True,text=True)
        self.assertEqual(p.returncode,2,p.stdout+p.stderr)
        self.assertIn("DIRECT_VERSION_LOCK_MISMATCH",
                      [e["code"] for e in json.loads(p.stdout)["errors"]])
        print("PHYSICAL_GIT_OBJECTS_STANDALONE_UPM_CLI_DRIFT_EXIT2")

    def test_24_source_cannot_pass_without_editor_version(self):
        self.editor = "m_EditorVersionWithRevision: 6000.3.14f1\n"
        self.assert_error("MISSING_UNITY_EDITOR_VERSION")

    def test_25_nonobject_manifest_dependency_map_rejected(self):
        self.manifest["dependencies"] = []
        self.assert_error("INVALID_UPM_DEPENDENCY_MAP")

    def test_26_cycle_does_not_hang_and_preserves_shortest_depth(self):
        self.lock["dependencies"][LEAF]["dependencies"] = {FOO:"1.2.3"}
        x=self.report()
        self.assertEqual(x["result"],"PASS_SOURCE_UPM_LOCK",x)

if __name__=="__main__":
    unittest.main(verbosity=2)
