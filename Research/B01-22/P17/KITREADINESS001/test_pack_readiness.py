import copy
import hashlib
import re
import io
import json
import pathlib
import tempfile
import unittest
from unittest import mock
import zipfile

from pack_readiness import ASSETS, PREVIEWS, FILES, ROOT, SourceError, sha256, \
    physical_unity_files, make_zip, verify_zip, receipt_for, verify_receipt, \
    read_previews, stage, rollback, validate_sources


class PhysicalSourceTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.files=physical_unity_files()
        cls.zip=make_zip(cls.files)
        cls.fixture=pathlib.Path(__file__).parent/"test-previews"

    def preview(self):
        # Independent preview fixture canary, but must have original schema.
        values={}
        for name in PREVIEWS:
            values[name]=(json.dumps({
                "schema":"b01-kitreadiness-preview-v1",
                "source_sha":"b1d4e341d846c9397e236809a1d7e4c4fff9c935",
                "scenario":name,"reference_only":True,"modifies_game":False,
                "result":{"Requests":[{"Id":"EK01","Ready":False}]}
            },sort_keys=True)+"\n").encode()
        return values

    def data_dir(self,path):
        path.mkdir()
        for name,data in self.preview().items():(path/name).write_bytes(data)

    def test_01_exact_three_owned_unity_members(self):
        self.assertEqual(len(self.files),3)
        self.assertTrue(verify_zip(self.zip,self.files))
    def test_02_reproducible_zip(self):
        self.assertEqual(self.zip,make_zip(physical_unity_files()))
    def test_03_content_one_real_cppcompatible_source(self):
        self.assertIn(b"class ReadinessPlan",self.files["Assets/B01KitReadiness001/KitReadinessPlanner.cs"])
    def test_04_two_distinct_guid_metas(self):
        import re
        vals=[re.search(rb"(?m)^guid: ([0-9a-f]{32})$",blob).group(1)
              for name,blob in self.files.items() if name.endswith(".meta")]
        self.assertEqual((len(vals),len(set(vals))),(2,2))
    def test_05_extra_asset_refused(self):
        bogus=dict(self.files);bogus["Assets/R3/Extra.cs"]=b"bad"
        with self.assertRaises(SourceError):make_zip(bogus)
    def test_06_missing_asset_refused(self):
        bogus=dict(self.files);bogus.pop(ASSETS[0])
        with self.assertRaises(SourceError):make_zip(bogus)
    def test_07_tampered_sha_refused(self):
        receipt=receipt_for(self.zip,self.files,self.preview())
        receipt["files"]["kitreadiness-unity.zip"]["sha256"]="bad"
        with self.assertRaises(SourceError):
            verify_receipt(self.zip,self.files,self.preview(),receipt)
    def test_08_wrong_original_catalog_sha_refused(self):
        receipt=receipt_for(self.zip,self.files,self.preview())
        receipt["original_catalog_git_blob"]="bad"
        with self.assertRaises(SourceError):
            verify_receipt(self.zip,self.files,self.preview(),receipt)
    def test_09_preview_cannot_become_game_mutation(self):
        with tempfile.TemporaryDirectory() as t:
            p=pathlib.Path(t);self.data_dir(p/"in")
            obj=json.loads((p/"in"/PREVIEWS[0]).read_text())
            obj["modifies_game"]=True
            (p/"in"/PREVIEWS[0]).write_text(json.dumps(obj))
            with self.assertRaises(SourceError):read_previews(p/"in")
    def test_10_stage_then_rollback(self):
        with tempfile.TemporaryDirectory() as t:
            p=pathlib.Path(t);self.data_dir(p/"in")
            dest=p/"real-output"
            stage(dest,p/"in",source_check=False)
            self.assertEqual({f.name for f in dest.iterdir()},FILES)
            self.assertEqual(rollback(dest),"ROLLED_BACK_PREVIEW_ONLY")
            self.assertFalse(dest.exists())
    def test_11_refuse_overwrite(self):
        with tempfile.TemporaryDirectory() as t:
            p=pathlib.Path(t);self.data_dir(p/"in")
            dest=p/"existing";dest.mkdir()
            with self.assertRaises(SourceError):stage(dest,p/"in",source_check=False)
    def test_12_foreign_file_rollback_refused(self):
        with tempfile.TemporaryDirectory() as t:
            p=pathlib.Path(t);self.data_dir(p/"in")
            dest=p/"output";stage(dest,p/"in",source_check=False)
            (dest/"user.txt").write_text("do not remove")
            with self.assertRaises(SourceError):rollback(dest)
            self.assertTrue((dest/"user.txt").exists())
    def test_13_modified_zip_rollback_refused(self):
        with tempfile.TemporaryDirectory() as t:
            p=pathlib.Path(t);self.data_dir(p/"in")
            dest=p/"output";stage(dest,p/"in",source_check=False)
            (dest/"kitreadiness-unity.zip").write_bytes(b"modified")
            with self.assertRaises((SourceError,zipfile.BadZipFile)):rollback(dest)
            self.assertTrue(dest.exists())
    def test_14_modified_preview_rollback_refused(self):
        with tempfile.TemporaryDirectory() as t:
            p=pathlib.Path(t);self.data_dir(p/"in")
            dest=p/"output";stage(dest,p/"in",source_check=False)
            (dest/PREVIEWS[0]).write_text("{}")
            with self.assertRaises((SourceError,KeyError)):rollback(dest)
            self.assertTrue(dest.exists())
    def test_16_noncanonical_crlf_preview_rejected(self):
        with tempfile.TemporaryDirectory() as t:
            p=pathlib.Path(t);self.data_dir(p/"in")
            target=p/"in"/PREVIEWS[0]
            target.write_bytes(target.read_bytes().replace(b"\n",b"\r\n"))
            with self.assertRaises(SourceError):read_previews(p/"in")

    def test_20_reject_dirty_worktree_unity_source_despite_clean_git_head(self):
        # Reproduce HEAD SHA matching manifest while on-disk bytes differ.
        # Inject a fake read; actual source file remains untouched.
        target = ROOT/ASSETS[1]
        original = pathlib.Path.read_bytes
        def corrupted(candidate):
            data = original(candidate)
            if candidate == target:
                return data + b"\n// LOCAL_UNCOMMITTED_SOURCE_INJECTION\n"
            return data
        with mock.patch.object(pathlib.Path, "read_bytes", new=corrupted):
            with self.assertRaises(SourceError):
                validate_sources()

    def test_21_reject_dirty_frozen_catalog_despite_clean_git_head(self):
        target = ROOT/"Research/B01-22/P17/EQUIPMENT-KITS-001/catalog.json"
        original = pathlib.Path.read_bytes
        def corrupted(candidate):
            data = original(candidate)
            return data + b" " if candidate == target else data
        with mock.patch.object(pathlib.Path, "read_bytes", new=corrupted):
            with self.assertRaises(SourceError):
                validate_sources()

    def test_22_foreign_empty_nested_directory_never_removed_by_rollback(self):
        # Relevant also to W13 #535 empty-foreign-directory rollback hazards:
        # protect even a directory with no files, not merely user.txt.
        with tempfile.TemporaryDirectory() as t:
            p = pathlib.Path(t)
            self.data_dir(p/"in")
            dest = p/"staged"
            stage(dest,p/"in",source_check=False)
            protected = dest/"foreign"/"empty"
            protected.mkdir(parents=True)
            before = {name:(dest/name).read_bytes() for name in FILES}
            with self.assertRaises(SourceError):
                rollback(dest)
            self.assertTrue(protected.is_dir())
            self.assertEqual({name:(dest/name).read_bytes() for name in FILES},before)

    def test_23_foreign_empty_symlink_directory_never_removed(self):
        with tempfile.TemporaryDirectory() as t:
            p=pathlib.Path(t)
            self.data_dir(p/"in")
            dest=p/"staged"
            stage(dest,p/"in",source_check=False)
            elsewhere=p/"foreign-empty"
            elsewhere.mkdir()
            link=dest/"external-link"
            try:
                link.symlink_to(elsewhere,target_is_directory=True)
            except (OSError,NotImplementedError):
                self.skipTest("OS does not permit test symlink creation")
            with self.assertRaises(SourceError):
                rollback(dest)
            self.assertTrue(elsewhere.is_dir())
            self.assertTrue(link.is_symlink())
            self.assertEqual({x.name for x in dest.iterdir()}, FILES|{"external-link"})

    @staticmethod
    def original_digest(forced_price_delta=0, swap_first_two=False):
        # Independently reconstruct C# canonical UTF-16 length-prefix text from
        # immutable source #411, not from a replica data fixture.
        original = json.loads((ROOT/"Research/B01-22/P17/EQUIPMENT-KITS-001/catalog.json").read_text(encoding="utf-8"))
        fields = ["b1d4e341d846c9397e236809a1d7e4c4fff9c935",len(original["kits"])]
        for k_index, kit in enumerate(original["kits"]):
            rows=list(kit["parts"])
            if swap_first_two and k_index == 0: rows[0],rows[1]=rows[1],rows[0]
            fields += [kit["id"],len(rows)]
            for p_index, p in enumerate(rows):
                unit = original["weapon_prices"][p["weapon"]] if p["kind"] == "Gun" else original["part_prices"][p["kind"]]
                if k_index == 0 and p_index == 0: unit += forced_price_delta
                fields += [p["kind"],p.get("weapon",""),p["count"],unit]
        def token(field):
            value=str(field)
            return str(len(value.encode("utf-16-le"))//2)+":"+value
        return hashlib.sha256("".join(token(x) for x in fields).encode("utf-8")).hexdigest()

    def test_17_real_original_catalog_matches_compiled_digest(self):
        csharp=(ROOT/"Assets/B01KitReadiness001/KitReadinessPlanner.cs").read_text(encoding="utf-8")
        m=re.search(r'ApprovedRecipeDigest\s*=\s*"([0-9a-f]{64})"',csharp)
        self.assertIsNotNone(m)
        self.assertEqual(self.original_digest(),m.group(1))
        self.assertEqual(m.group(1),"6f121fe4207aaca2f30dfede9e79700d3af0dbec5349c87c1709c174dd4096eb")

    def test_18_coherently_forged_real_price_changes_digest(self):
        self.assertNotEqual(self.original_digest(forced_price_delta=7),self.original_digest())

    def test_19_same_physical_parts_reordered_changes_digest(self):
        self.assertNotEqual(self.original_digest(swap_first_two=True),self.original_digest())

    def test_15_receipt_rejects_unexpected_member_byte(self):
        files=dict(self.files);files[ASSETS[1]]=files[ASSETS[1]]+b"changed"
        rec=receipt_for(self.zip,self.files,self.preview())
        with self.assertRaises(SourceError):verify_receipt(self.zip,files,self.preview(),rec)


if __name__=="__main__":unittest.main(verbosity=2)
