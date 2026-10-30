import copy,io,json,tempfile,unittest,warnings,zipfile
from pathlib import Path
from pack_gallery import MEMBERS,PackageError,source_files,make_zip,receipt_for,verify_zip,stage,rollback

class OriginalPhysicalUnityPackageTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.files=source_files();cls.zip=make_zip(cls.files);cls.receipt=receipt_for(cls.zip,cls.files)
    def test_01_nine_actual_unity_members(self):
        self.assertEqual(verify_zip(self.zip,self.receipt,self.files),9)
    def test_02_deterministic_bytes(self):
        self.assertEqual(self.zip,make_zip(source_files()))
    def test_03_original_editor_sources_present(self):
        self.assertIn("Assets/B01KitGallery001/Editor/KitGalleryWindow.cs",self.files)
        self.assertIn("Assets/B01KitGallery001/Editor/KitComparisonRules.cs",self.files)
    def test_04_actual_411_source_identity(self):
        c=json.loads(self.files["Assets/B01KitGallery001/Data/KitGallery.json"])
        self.assertEqual(c["source_sha"],"b1d4e341d846c9397e236809a1d7e4c4fff9c935")
        self.assertEqual(len(c["offers"]),10)
    def test_05_six_unique_guid_metas(self):
        import re
        g=[re.search(rb"(?m)^guid: ([0-9a-f]{32})$",v).group(1) for k,v in self.files.items() if k.endswith(".meta")]
        self.assertEqual((len(g),len(set(g))),(6,6))
    def test_06_tampered_zip_refused(self):
        with self.assertRaises(PackageError):verify_zip(self.zip+b"bad",self.receipt,self.files)
    def test_07_forged_file_hash_refused(self):
        x=copy.deepcopy(self.receipt);x["files"][MEMBERS[0]]["sha256"]="0"*64
        with self.assertRaises(PackageError):verify_zip(self.zip,x,self.files)
    def test_08_changed_provenance_refused(self):
        x=copy.deepcopy(self.receipt);x["source_sha"]="old"
        with self.assertRaises(PackageError):verify_zip(self.zip,x,self.files)
    def test_09_foreign_asset_refused(self):
        x=dict(self.files);x["Assets/R3/NotOwned.cs"]=b"FAKE"
        with self.assertRaises(PackageError):make_zip(x)
    def test_10_duplicate_member_refused(self):
        buf=io.BytesIO()
        with warnings.catch_warnings():
            warnings.simplefilter("ignore",UserWarning)
            with zipfile.ZipFile(buf,"w") as z:
                z.writestr(MEMBERS[0],b"original")
                z.writestr(MEMBERS[0],b"duplicate")
        x=receipt_for(buf.getvalue(),self.files)
        with self.assertRaises(PackageError):verify_zip(buf.getvalue(),x,self.files)
    def test_11_real_stage_then_reversible_rollback(self):
        with tempfile.TemporaryDirectory() as t:
            dest=Path(t)/"out"
            rec=stage(dest,source_check=False)
            self.assertEqual(verify_zip((dest/"kitcompare-unity.zip").read_bytes(),rec,self.files),9)
            self.assertEqual(rollback(dest),"ROLLED_BACK_NO_GAME_CHANGES")
            self.assertFalse(dest.exists())
    def test_12_existing_output_is_protected(self):
        with tempfile.TemporaryDirectory() as t:
            dest=Path(t)/"out";dest.mkdir()
            with self.assertRaises(PackageError):stage(dest,source_check=False)
    def test_13_no_rollback_modified_zip(self):
        with tempfile.TemporaryDirectory() as t:
            dest=Path(t)/"out";stage(dest,source_check=False)
            (dest/"kitcompare-unity.zip").write_bytes(b"modified")
            with self.assertRaises(PackageError):rollback(dest)
            self.assertTrue(dest.exists())
    def test_14_no_rollback_foreign_file(self):
        with tempfile.TemporaryDirectory() as t:
            dest=Path(t)/"out";stage(dest,source_check=False)
            (dest/"another.file").write_bytes(b"owned by somebody else")
            with self.assertRaises(PackageError):rollback(dest)
            self.assertTrue((dest/"another.file").exists())
    def test_15_editor_only_source_scope(self):
        self.assertTrue(all(x.startswith("Assets/B01KitGallery001") for x in MEMBERS))
if __name__=="__main__":unittest.main(verbosity=2)
