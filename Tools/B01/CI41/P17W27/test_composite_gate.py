#!/usr/bin/env python3
"""B01-41 W27 actual unit negatives for safe reference transport, not Unity/PhysX."""
import io
import unittest
import zipfile
from composite_gate import PACKAGES, RUNTIME_PINS, check_zip, safe_path, zip_catalog


class CompositeGateSafetyTests(unittest.TestCase):
    def test_five_explicit_immutable_candidates_not_live_branches(self):
        self.assertEqual(len(PACKAGES), 5)
        for name, value in PACKAGES.items():
            self.assertRegex(value["sha"], "^[0-9a-f]{40}$")
            self.assertRegex(value["base"], "^[0-9a-f]{40}$")
            self.assertNotEqual(value["sha"], value["base"])
            self.assertGreater(value["count"], 0)

    def test_exact_blobs_required_for_every_package(self):
        self.assertEqual(set(PACKAGES), set(RUNTIME_PINS).intersection(PACKAGES))
        for name in PACKAGES:
            self.assertTrue(RUNTIME_PINS[name])

    def test_safe_posix_unity_paths(self):
        for path in ("Assets/A.cs", "Assets/Scene/Scene.unity", "Tools/B01/a.meta"):
            self.assertEqual(safe_path(path), path)

    def test_reject_escape_and_ntfs_aliases(self):
        for path in ("../Assets/file", "a/../b", "a//b", "./x", "/tmp/file",
                     "C:/temp/file", "a\\b", "", "a/\0b", "a/./b", "a/"):
            with self.subTest(path=repr(path)), self.assertRaises(ValueError):
                safe_path(path)

    def test_archive_bytes_reproducible(self):
        files = {"Assets/x.cs": b"good\n", "Assets/x.cs.meta": b"guid: " + b"a" * 32 + b"\n"}
        first = zip_catalog(files)
        self.assertEqual(first, zip_catalog(files))
        check_zip(first, files)

    def test_archive_detects_source_mutation(self):
        files = {"Assets/x.cs": b"good"}
        with self.assertRaisesRegex(ValueError, "ZIP_BYTES_MISMATCH"):
            check_zip(zip_catalog(files), {"Assets/x.cs": b"bad"})

    def test_archive_detects_missing_manifest_source(self):
        with self.assertRaisesRegex(ValueError, "ZIP_DUPLICATE_OR_UNEXPECTED"):
            check_zip(zip_catalog({"Assets/one.cs": b"1"}), {"Assets/two.cs": b"1"})

    def test_archive_denies_duplicate_names(self):
        output = io.BytesIO()
        with zipfile.ZipFile(output, "w", compression=zipfile.ZIP_STORED) as z:
            import warnings
            with warnings.catch_warnings():
                warnings.simplefilter("ignore", UserWarning)
                z.writestr("Assets/A.cs", b"x")
                z.writestr("Assets/A.cs", b"x")
        with self.assertRaisesRegex(ValueError, "ZIP_DUPLICATE_OR_UNEXPECTED"):
            check_zip(output.getvalue(), {"Assets/A.cs": b"x"})

    def test_archive_denies_traversal_even_when_bytes_match(self):
        unsafe = {"../foreign/host.cs": b"payload"}
        with self.assertRaises(ValueError):
            zip_catalog(unsafe)

    def test_archive_denies_absolute(self):
        with self.assertRaises(ValueError):
            zip_catalog({"/tmp/foreign.cs": b"payload"})


if __name__ == "__main__":
    unittest.main()
