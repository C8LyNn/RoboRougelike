#!/usr/bin/env python3
"""Negative source/package checks; never label these Unity native QA."""
import io
import json
import unittest
import zipfile
import package_partsize as p

class PartSizeSourceTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.files=p.source_paths()

    def test_01_valid_full_package_and_meta(self):
        self.assertGreaterEqual(len(p.validate(self.files)),12)

    def test_02_reject_cross_owner_and_traversal(self):
        for name in ["Assets/R3/R3VehicleModel.cs","Assets/B01Packages.meta",
                     "../Assets/B01Packages/PARTSIZE001/Bad.cs",
                     "Assets/B01Packages/PARTSIZE001/../../R3/file.cs",
                     "Assets\\B01Packages\\PARTSIZE001\\Bad.cs"]:
            self.assertFalse(p.valid_path(name),name)

    def test_03_no_missing_Unity_meta(self):
        bad=[x for x in self.files if not x.endswith(
             "Assets/B01Packages/PARTSIZE001/Runtime/PartSizeContract.cs.meta")]
        with self.assertRaisesRegex(ValueError,"MISSING_ASSET_META"):
            p.validate(bad)

    def test_04_deterministic_byte_identical_zip(self):
        a,m=p.build(self.files,"a"*40)
        b,_=p.build(self.files,"a"*40)
        self.assertEqual(a,b)
        self.assertTrue(p.verify_zip(a,m))

    def test_05_sha256_manifest_tamper_rejected(self):
        data,m=p.build(self.files,"a"*40)
        altered=json.loads(json.dumps(m))
        altered["files"][0]["sha256"]="0"*64
        with self.assertRaisesRegex(ValueError,"ZIP_EMBEDDED_MANIFEST_MISMATCH"):
            p.verify_zip(data,altered)

    def test_06_zip_entry_swap_rejected(self):
        data,m=p.build(self.files,"a"*40)
        with zipfile.ZipFile(io.BytesIO(data)) as archive:
            names=archive.namelist()
        self.assertNotIn("Assets/R3/R3VehicleModel.cs",names)
        self.assertIn(p.MANIFEST,names)

    def test_07_explicit_native_hold(self):
        _,m=p.build(self.files,"a"*40)
        self.assertEqual(m["native_unity"],"NOT_RUN")
        self.assertEqual(m["state"],"IN_PROGRESS_NOT_OFFERED")
        self.assertEqual(m["G2"],"HOLD")
        self.assertIsNone(m["codex_target_sha"])
        self.assertNotIn("Assets/B01Packages.meta",[f["path"] for f in m["files"]])

    def test_08_real_Unity_Code_And_Demo_Exist(self):
        self.assertTrue(any(x.endswith("/PartSizeNativePhysics.cs") for x in self.files))
        self.assertTrue(any(x.endswith("/PARTSIZE001_Demo.prefab") for x in self.files))
        self.assertTrue(any(x.endswith("/PartSize001PlayModeTests.cs") for x in self.files))
        self.assertTrue(any(x.endswith("/PartSize001EditModeTests.cs") for x in self.files))

if __name__=="__main__":unittest.main(verbosity=2)
