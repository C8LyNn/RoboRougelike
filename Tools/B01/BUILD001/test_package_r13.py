#!/usr/bin/env python3
"""Executable stdlib package integrity and negative tests; NOT Unity/PhysX QA."""
import hashlib
import io
import json
from pathlib import Path
import unittest
import zipfile

import package_r13 as pack


class PackageR13Tests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.files = pack.source_paths()
        cls.manifest = pack.manifest_for(cls.files,"a"*40)

    def test_01_complete_assets_have_meta_and_unique_guids(self):
        self.assertGreaterEqual(pack.validate_source(self.files), 16)

    def test_02_in_package_scope(self):
        self.assertTrue(all(pack.safe_path(p) for p in self.files))
        self.assertFalse(pack.safe_path("../Assets/B01Construction/BUILD001/hack.cs"))
        self.assertFalse(pack.safe_path("Assets/B01Construction/BUILD001/../../R3/R3.cs"))
        self.assertFalse(pack.safe_path("Assets\\B01Construction\\BUILD001\\foo.cs"))
        self.assertFalse(pack.safe_path("Assets/R3/Host.cs"))

    def test_03_duplicate_and_missing_paths_fail(self):
        with self.assertRaisesRegex(ValueError,"duplicate"):
            pack.validate_source(self.files+[self.files[0]])
        with self.assertRaisesRegex(ValueError,"missing source"):
            pack.validate_source(self.files+["Assets/B01Construction/BUILD001/not-in-git.cs"])

    def test_04_full_zip_matches_full_sha256_manifest(self):
        payload=pack.build_zip(self.files,self.manifest)
        pack.validate_zip(payload,self.manifest)
        self.assertEqual(payload,pack.build_zip(self.files,self.manifest))
        self.assertEqual(hashlib.sha256(payload).hexdigest(),
                         hashlib.sha256(pack.build_zip(self.files,self.manifest)).hexdigest())

    def test_05_tampered_checksum_fails(self):
        payload=pack.build_zip(self.files,self.manifest)
        poisoned=json.loads(json.dumps(self.manifest))
        poisoned["files"][0]["sha256"]="0"*64
        with self.assertRaisesRegex(ValueError,"mismatch"):
            pack.validate_zip(payload,poisoned)

    def test_06_tampered_manifest_in_zip_fails(self):
        payload=pack.build_zip(self.files,self.manifest)
        poisoned=json.loads(json.dumps(self.manifest))
        poisoned["G2"]="PASS"
        with self.assertRaisesRegex(ValueError,"manifest mismatch"):
            pack.validate_zip(payload,poisoned)

    def test_07_artifact_readability(self):
        payload=pack.build_zip(self.files,self.manifest)
        with zipfile.ZipFile(io.BytesIO(payload)) as z:
            self.assertIsNone(z.testzip())
            self.assertIn("PACKAGE_MANIFEST_R13.json",z.namelist())
            self.assertTrue(any(x.endswith("BUILD001_PreviewURP.shader") for x in z.namelist()))
            self.assertTrue(any(x.endswith("BuildVolumeValidator.cs") for x in z.namelist()))


if __name__ == "__main__":
    unittest.main(verbosity=2)
