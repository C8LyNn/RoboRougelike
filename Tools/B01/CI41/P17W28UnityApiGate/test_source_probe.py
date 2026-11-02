#!/usr/bin/env python3
"""Fail-closed original-source identity and versioned API scanner unit regressions."""
import unittest
from unittest.mock import patch
from source_probe import (SOURCES, W27_FILES, EXPECTED_COVERAGE, CODEx_W27,
                          git_oid, unsupported_hits, coverage_contract, verified_original)


class SourceProbeTests(unittest.TestCase):
    def test_original_source_list_uses_immutable_sha(self):
        self.assertEqual(len(SOURCES), 9)
        self.assertEqual(len(W27_FILES), 5)
        for _, ref, path, oid in SOURCES:
            self.assertRegex(ref, r"^[0-9a-f]{40}$")
            self.assertRegex(oid, r"^[0-9a-f]{40}$")
            self.assertTrue(path.endswith(".cs"))
        self.assertRegex(CODEx_W27, r"^[0-9a-f]{40}$")

    def test_git_blob_exact_bytes(self):
        self.assertEqual(git_oid(b"hello\n"), "ce013625030ba8dba906f756967f9e9ca394464a")

    def test_changed_one_byte_fails_original_pin(self):
        with patch("source_probe.git", return_value=b"changed"):
            with self.assertRaisesRegex(AssertionError, "PINNED_GIT_BLOB_MISMATCH"):
                verified_original("a"*40, "Assets/Test.cs", "b"*40)

    def test_reject_relative_path_escape(self):
        for p in ("../x.cs", "Assets/../../x.cs", "/absolute.cs", "Assets\\file.cs", "file\0.cs"):
            with self.subTest(p=p), self.assertRaises(ValueError):
                verified_original("a"*40, p, "b"*40)

    def test_reject_branch_instead_of_sha(self):
        with self.assertRaises(ValueError):
            verified_original("main", "Assets/foo.cs", "a"*40)

    def test_compatibility_scanner_real_api_shapes(self):
        src = "SHA256.HashData(bytes); Convert.ToHexString(digest); File.Move(staging,authoritative,true);"
        self.assertEqual(unsupported_hits([src]), dict(hashdata=1, tohex=1, move_overwrite=1))

    def test_compatibility_scanner_denies_safe_replacement_as_legacy(self):
        src = 'using(var h=SHA256.Create()) h.ComputeHash(data); File.Replace(a,b,c);'
        self.assertEqual(unsupported_hits([src]), dict(hashdata=0, tohex=0, move_overwrite=0))

    def test_coverage_identifiers_and_qa_only_define(self):
        d={key: term for key,term in EXPECTED_COVERAGE.items()}
        d["player_harness"] += "#if B01_UNIFIED_RC2_ACCEPTANCE"
        r=coverage_contract(d)
        self.assertEqual(r["rotary_saw"], "FULL_SELF_COLLISION_AND_PLAYER_QA_NOT_CONFIRMED")
        self.assertEqual(r["human_playtest"], "NOT_RUN")

    def test_coverage_denies_missing_unity_fixture(self):
        d={key: term for key,term in EXPECTED_COVERAGE.items()}
        d["player_harness"] += "#if B01_UNIFIED_RC2_ACCEPTANCE"
        d["beam_unity_test"]="only empty source"
        with self.assertRaisesRegex(AssertionError,"W27_SOURCE_TEST_IDENTITY_DRIFT"):
            coverage_contract(d)

    def test_coverage_denies_player_without_compile_define(self):
        d={key: term for key,term in EXPECTED_COVERAGE.items()}
        with self.assertRaisesRegex(AssertionError,"W27_HARNESS_COMPILATION_GATE_CHANGED"):
            coverage_contract(d)


if __name__ == "__main__":
    unittest.main()
