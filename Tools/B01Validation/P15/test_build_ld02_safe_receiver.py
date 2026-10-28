#!/usr/bin/env python3
"""B01-40 PASS15 regression: original repaired B01-25 map bytes + original Codex C# blobs.
Real temporary filesystem and original Git blobs; no Unity, no user map folder.
"""
import copy
import hashlib
import json
from pathlib import Path
import subprocess
import tempfile
import unittest
import zipfile
import build_ld02_safe_receiver as m

REPO=Path(__file__).resolve().parents[3]

class OriginalRepairedLD02Receiver(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.raw,cls.summary=m.original_inputs(REPO)
        cls.meta=m.manifest(cls.raw,cls.summary)

    def setUp(self):
        self.temp=tempfile.TemporaryDirectory(prefix="b01_40_map_safe_")
        self.addCleanup(self.temp.cleanup)
        self.root=Path(self.temp.name)
        self.stage=self.root/"staging";self.stage.mkdir()
        self.test_maps=self.root/"disposable_test_maps";self.test_maps.mkdir()

    def marker(self,valid=True):
        (self.test_maps/m.MARKER).write_text(m.MARKER_TEXT if valid else "wrong marker\n",encoding="ascii")

    def test_01_original_pinned_map_and_codex_sources_are_real(self):
        self.assertEqual(len(self.raw),22772)
        self.assertEqual(self.summary["item_count"],168)
        self.assertEqual(self.summary["kinds"]["Platform"],4)
        self.assertEqual(self.summary["kinds"]["Wall"],36)
        self.assertTrue(self.summary["spawn_on_ground"])
        self.assertEqual(m.MAP_BLOB,
                         m.git(REPO,"rev-parse",m.OWNER_SHA+":"+m.MAP_PATH).decode().strip())
        for path,blob in m.CODEX_SOURCE.items():
            self.assertEqual(blob,m.git(REPO,"rev-parse",m.CODEX_SHA+":"+path).decode().strip())

    def test_02_dry_run_does_not_write_to_any_output_directory(self):
        before=list(self.stage.iterdir())
        self.assertEqual(m.run(["--repo",str(REPO)]),0)
        self.assertEqual(before,list(self.stage.iterdir()))

    def test_03_deterministic_package_contains_exact_original_git_blob(self):
        first=m.zip_bytes(self.raw,self.meta)
        second=m.zip_bytes(self.raw,self.meta)
        self.assertEqual(first,second)
        result=m.stage(REPO,self.stage,self.raw,self.meta)
        self.assertEqual(result["zip_sha256"],hashlib.sha256(first).hexdigest())
        with zipfile.ZipFile(self.stage/m.ARCHIVE) as z:
            self.assertEqual(z.namelist(),[m.MAP_MEMBER,"source-manifest.json"])
            self.assertEqual(z.read(m.MAP_MEMBER),self.raw)
            self.assertEqual(json.loads(z.read("source-manifest.json"))["owner_git_blob"],m.MAP_BLOB)

    def test_04_staging_refuses_overwrite(self):
        m.stage(REPO,self.stage,self.raw,self.meta)
        old=(self.stage/m.ARCHIVE).read_bytes()
        with self.assertRaisesRegex(m.GuardError,"STAGING_OUTPUT_ALREADY_EXISTS"):
            m.stage(REPO,self.stage,self.raw,self.meta)
        self.assertEqual((self.stage/m.ARCHIVE).read_bytes(),old)

    def test_05_wrong_blob_is_denied_before_staging(self):
        with self.assertRaisesRegex(m.GuardError,"PINNED_GIT_BLOB_DRIFT"):
            m.assert_original_blob(REPO,m.OWNER_SHA,m.MAP_PATH,"0"*40)

    def test_06_corrupt_map_schema_and_bool_as_integer_rejected(self):
        for key,val in (("version",3),("globalFloor",True),("items",[])):
            document=json.loads(self.raw)
            document[key]=val
            with self.subTest(key=key),self.assertRaises(m.GuardError):
                m.validate_map(json.dumps(document).encode())
        document=json.loads(self.raw)
        document["items"][0]["x"]=True
        with self.assertRaisesRegex(m.GuardError,"MAP_ITEM_TYPE"):
            m.validate_map(json.dumps(document).encode())

    def test_07_bad_footprint_and_duplicate_layer_denied(self):
        data=json.loads(self.raw)
        data["items"][0]["scaleX"]=5
        with self.assertRaisesRegex(m.GuardError,"MAP_ITEM_OUT_OF_RANGE"):
            m.validate_map(json.dumps(data).encode())
        data=json.loads(self.raw)
        data["items"][1]["x"]=data["items"][0]["x"]
        data["items"][1]["z"]=data["items"][0]["z"]
        with self.assertRaisesRegex(m.GuardError,"MAP_LAYER_FOOTPRINT_OVERLAP"):
            m.validate_map(json.dumps(data).encode())

    def test_08_dangerous_map_names_and_windows_devices_rejected(self):
        for name in ("../Maps/private","foo/bar","..","CON","con","AUX","COM1","LPT9",
                     "trailing ","name.json","name\\test","A"*65):
            with self.subTest(name=name),self.assertRaises(m.GuardError):
                m.validate_name(name)
        self.assertEqual(m.validate_name(m.MAP_NAME),m.MAP_NAME)

    def test_09_install_requires_a_real_optin_marker(self):
        with self.assertRaisesRegex(m.GuardError,"EXPLICIT_DISPOSABLE_MAPS_MARKER_REQUIRED"):
            m.install(REPO,self.test_maps,self.raw,self.meta)
        self.marker(valid=False)
        with self.assertRaisesRegex(m.GuardError,"EXPLICIT_DISPOSABLE_MAPS_MARKER_REQUIRED"):
            m.install(REPO,self.test_maps,self.raw,self.meta)
        self.assertFalse((self.test_maps/"Maps").exists())

    def test_10_install_then_cleanup_preserves_unrelated_bytes(self):
        self.marker()
        unrelated=self.test_maps/"do-not-touch.bin";unrelated.write_bytes(b"PRESERVE\0BINARY")
        result=m.install(REPO,self.test_maps,self.raw,self.meta)
        dest=Path(result["disposable_install"])
        self.assertEqual(dest.read_bytes(),self.raw)
        self.assertEqual(m.cleanup(REPO,self.test_maps,self.raw)["status"],
                         "NO_LIVE_UNITY_PATH_TOUCHED")
        self.assertFalse(dest.exists())
        self.assertFalse((self.test_maps/m.RECEIPT).exists())
        self.assertEqual(unrelated.read_bytes(),b"PRESERVE\0BINARY")
        self.assertEqual((self.test_maps/m.MARKER).read_text(),m.MARKER_TEXT)

    def test_11_file_overwrite_and_casefold_alias_both_denied(self):
        self.marker()
        maps=self.test_maps/"Maps";maps.mkdir()
        other=maps/m.MAP_FILE
        other.write_bytes(b"EXISTING USER MAP")
        with self.assertRaisesRegex(m.GuardError,"MAP_NAME_OR_CASEFOLD_COLLISION"):
            m.install(REPO,self.test_maps,self.raw,self.meta)
        self.assertEqual(other.read_bytes(),b"EXISTING USER MAP")
        other.unlink()
        alias=maps/(m.MAP_NAME.lower()+".JSON")
        alias.write_bytes(b"EXISTING CASE ALIAS")
        with self.assertRaisesRegex(m.GuardError,"MAP_NAME_OR_CASEFOLD_COLLISION"):
            m.install(REPO,self.test_maps,self.raw,self.meta)
        self.assertEqual(alias.read_bytes(),b"EXISTING CASE ALIAS")

    def test_12_receipt_tamper_prevents_cleanup_without_deletion(self):
        self.marker()
        m.install(REPO,self.test_maps,self.raw,self.meta)
        target=self.test_maps/"Maps"/m.MAP_FILE
        target.write_bytes(b"CHANGED AFTER TEST")
        with self.assertRaisesRegex(m.GuardError,"CLEANUP_REFUSES_MODIFIED_MAP"):
            m.cleanup(REPO,self.test_maps,self.raw)
        self.assertEqual(target.read_bytes(),b"CHANGED AFTER TEST")
        self.assertTrue((self.test_maps/m.RECEIPT).is_file())

    def test_13_install_denies_git_source_root_even_with_marker(self):
        with self.assertRaisesRegex(m.GuardError,"OUTPUT_OVERLAPS_GIT_SOURCE_WORKTREE"):
            m.guard_output_dir(REPO,REPO)

    def test_14_disposable_install_is_no_open_overwrite_on_second_attempt(self):
        self.marker()
        first=m.install(REPO,self.test_maps,self.raw,self.meta)
        with self.assertRaisesRegex(m.GuardError,"INSTALL_RECEIPT_COLLISION"):
            m.install(REPO,self.test_maps,self.raw,self.meta)
        self.assertEqual(Path(first["disposable_install"]).read_bytes(),self.raw)

    def test_15_cli_stage_install_cleanup_end_to_end(self):
        self.marker()
        self.assertEqual(m.run(["--repo",str(REPO),"--stage-to",str(self.stage)]),0)
        self.assertEqual(m.run(["--repo",str(REPO),"--install-to",str(self.test_maps)]),0)
        self.assertEqual(m.run(["--repo",str(REPO),"--cleanup-from",str(self.test_maps)]),0)
        self.assertFalse((self.test_maps/"Maps"/m.MAP_FILE).exists())
        self.assertTrue((self.stage/m.ARCHIVE).exists())

if __name__=="__main__":
    print("B01_40_ORIGINAL_REPAIRED_LD02_SOURCE_SHA="+m.OWNER_SHA+
          " UNITY_NOT_RUN NO_USER_MAPS_TOUCHED",flush=True)
    unittest.main(verbosity=2)
