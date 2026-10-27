#!/usr/bin/env python3
"""Negative source/package checks; never label these Unity native QA."""
import io
import csv
import re
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

    def test_09_nunit_assemblies_expose_UnityTest(self):
        for category in ("EditMode","PlayMode"):
            path=(p.ROOT/"Assets/B01Packages/PARTSIZE001/Tests"/category/
                  ("B01.PARTSIZE001."+category+"Tests.asmdef"))
            obj=json.loads(path.read_text())
            self.assertEqual(obj["references"],["B01.PARTSIZE001.Runtime"])
            self.assertIn("TestAssemblies",obj["optionalUnityReferences"])
            self.assertIn("nunit.framework.dll",obj["precompiledReferences"])

    def test_10_all_19_csv_kinds_match_real_catalog(self):
        source=(p.ROOT/"Assets/B01Packages/PARTSIZE001/Runtime/PartSizeBaselineCatalog.cs").read_text()
        section=source.split("CanonicalNames = {",1)[1].split("};",1)[0]
        names=re.findall(r'"([^"]+)"',section)
        with (p.ROOT/"Docs/B01/PARTSIZE001/observed_r3_size_matrix.csv").open(newline="") as f:
            rows=list(csv.DictReader(f))
        self.assertEqual(len(names),19)
        self.assertEqual(names,[row["part_kind"] for row in rows])
        self.assertEqual(sum(row["logical_x"]=="2" for row in rows),6)
        self.assertTrue(all(row["actual_unity_collider_measured"]=="NOT_RUN" for row in rows))

    def test_11_real_PhysX_source_not_replaced_by_stub(self):
        source=(p.ROOT/"Assets/B01Packages/PARTSIZE001/Runtime/PartSizeNativePhysics.cs").read_text()
        self.assertIn("Physics.ComputePenetration",source)
        self.assertIn("owner.GetComponentsInChildren<Collider>(true)",source)
        self.assertIn('return "PHYSICS_UNVERIFIED"',source)
        self.assertIn('return "UNREGISTERED_PHYSICAL_COLLIDER"',source)
        self.assertNotIn("using RoboRougelike.R3",source)

    def test_12_prefab_script_reference_uses_owned_guid(self):
        path=p.ROOT/"Assets/B01Packages/PARTSIZE001"
        prefab=(path/"Demo/PARTSIZE001_Demo.prefab").read_text()
        meta=(path/"Runtime/PartSizeDemoEntry.cs.meta").read_text()
        guid=re.search(r"(?m)^guid: ([0-9a-f]{32})$",meta).group(1)
        self.assertIn("guid: "+guid,prefab)
        self.assertFalse((p.ROOT/"Assets/B01Packages.meta").exists(),
                         "Producer branch cannot originate/own shared folder meta")

if __name__=="__main__":unittest.main(verbosity=2)
