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
                     "../Assets/PARTSIZE001/Bad.cs",
                     "Assets/PARTSIZE001/../../R3/file.cs",
                     "Assets\\B01Packages\\PARTSIZE001\\Bad.cs"]:
            self.assertFalse(p.valid_path(name),name)

    def test_03_no_missing_Unity_meta(self):
        bad=[x for x in self.files if not x.endswith(
             "Assets/PARTSIZE001/Runtime/PartSizeContract.cs.meta")]
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
            path=(p.ROOT/"Assets/PARTSIZE001/Tests"/category/
                  ("B01.PARTSIZE001."+category+"Tests.asmdef"))
            obj=json.loads(path.read_text())
            self.assertEqual(obj["references"],["B01.PARTSIZE001.Runtime"])
            self.assertIn("TestAssemblies",obj["optionalUnityReferences"])
            self.assertIn("nunit.framework.dll",obj["precompiledReferences"])

    def test_10_all_19_csv_kinds_match_real_catalog(self):
        source=(p.ROOT/"Assets/PARTSIZE001/Runtime/PartSizeBaselineCatalog.cs").read_text()
        section=source.split("CanonicalNames = {",1)[1].split("};",1)[0]
        names=re.findall(r'"([^"]+)"',section)
        with (p.ROOT/"Docs/B01/PARTSIZE001/observed_r3_size_matrix.csv").open(newline="") as f:
            rows=list(csv.DictReader(f))
        self.assertEqual(len(names),19)
        self.assertEqual(names,[row["part_kind"] for row in rows])
        self.assertEqual(sum(row["logical_x"]=="2" for row in rows),6)
        self.assertTrue(all(row["actual_unity_collider_measured"]=="NOT_RUN" for row in rows))

    def test_11_real_PhysX_source_not_replaced_by_stub(self):
        source=(p.ROOT/"Assets/PARTSIZE001/Runtime/PartSizeNativePhysics.cs").read_text()
        self.assertIn("Physics.ComputePenetration",source)
        self.assertIn("owner.GetComponentsInChildren<Collider>(true)",source)
        self.assertIn('return "PHYSICS_UNVERIFIED"',source)
        self.assertIn('return "UNREGISTERED_PHYSICAL_COLLIDER"',source)
        self.assertNotIn("using RoboRougelike.R3",source)

    def test_12_prefab_script_reference_uses_owned_guid(self):
        path=p.ROOT/"Assets/PARTSIZE001"
        prefab=(path/"Demo/PARTSIZE001_Demo.prefab").read_text()
        meta=(path/"Runtime/PartSizeDemoEntry.cs.meta").read_text()
        guid=re.search(r"(?m)^guid: ([0-9a-f]{32})$",meta).group(1)
        self.assertIn("guid: "+guid,prefab)
        self.assertFalse((p.ROOT/"Assets/B01Packages.meta").exists(),
                         "Producer branch cannot originate/own shared folder meta")

    def test_13_authored_native_case_counts_match_docs(self):
        source=p.ROOT/"Assets/PARTSIZE001/Tests"
        edit=(source/"EditMode/PartSize001EditModeTests.cs").read_text()
        play=(source/"PlayMode/PartSize001PlayModeTests.cs").read_text()
        self.assertEqual(edit.count("[Test]")+edit.count("[TestCase("),17)
        self.assertEqual(play.count("[UnityTest]"),8)
        self.assertIn("Physics.ComputePenetration",
            (p.ROOT/"Assets/PARTSIZE001/Runtime/PartSizeNativePhysics.cs").read_text())

    def test_14_package_bytes_match_frozen_git_blobs_even_on_windows(self):
        payload,manifest=p.build(self.files,"a"*40)
        by_path={f["path"]:f for f in manifest["files"]}
        for path in (
            "Assets/PARTSIZE001/Runtime/PartSizeContract.cs",
            "Assets/PARTSIZE001/Tests/PlayMode/PartSize001PlayModeTests.cs",
            "Docs/B01/PARTSIZE001/observed_r3_size_matrix.csv"
        ):
            blob=p.committed_bytes(path)
            self.assertEqual(p.sha(blob),by_path[path]["sha256"])
            self.assertEqual(len(blob),by_path[path]["bytes"])
        self.assertTrue(p.verify_zip(payload,manifest))

    def test_15_real_demo_scene_has_matching_owned_script_guid(self):
        root=p.ROOT/"Assets/PARTSIZE001"
        scene=(root/"Demo/PARTSIZE001_Demo.unity").read_text(encoding="utf8")
        scene_meta=(root/"Demo/PARTSIZE001_Demo.unity.meta").read_text(encoding="utf8")
        script_meta=(root/"Runtime/PartSizeDemoEntry.cs.meta").read_text(encoding="utf8")
        script_guid=re.search(r"(?m)^guid: ([0-9a-f]{32})$",script_meta).group(1)
        self.assertEqual(scene.count("guid: "+script_guid),1)
        self.assertIn("SceneRoots:",scene)
        self.assertIn("PARTSIZE001_DemoSceneRoot",scene)
        self.assertIn("m_Roots:",scene)
        self.assertEqual(scene.count("m_GameObject: {fileID: 510000}"),2)
        self.assertIn("guid: b012310000000000000000000000009e",scene_meta)
        self.assertNotEqual(script_guid,"b0123100000000000000000000000017")
        self.assertIn("Assets/PARTSIZE001/Demo/PARTSIZE001_Demo.unity",self.files)
        self.assertIn("Assets/PARTSIZE001/Demo/PARTSIZE001_Demo.unity.meta",self.files)

    def test_16_api_contract_in_complete_package_not_native_pass(self):
        target="Docs/B01/PARTSIZE001/API.md"
        self.assertIn(target,self.files)
        doc=(p.ROOT/target).read_text(encoding="utf8")
        self.assertIn("PartSizeNativePhysics.Validate(",doc)
        self.assertIn("PARTSIZE001_Demo.unity",doc)
        self.assertIn("NATIVE_NOT_RUN",doc)
        self.assertIn("G2 HOLD",doc)
        self.assertNotIn("CODEX_READY: PASS",doc)

if __name__=="__main__":unittest.main(verbosity=2)
