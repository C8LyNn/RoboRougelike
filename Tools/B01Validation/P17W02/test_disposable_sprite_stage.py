#!/usr/bin/env python3
"""W02 real original artifact: reversible staging + fail-closed negative filesystem tests."""
import json
import os
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

import disposable_sprite_stage as s

REPO=Path(__file__).resolve().parents[3]
ARTIFACT=Path(os.environ["B0140_ORIGINAL_ARTIFACT"])

class OriginalSpriteDisposableImport(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.files,cls.guids=s.check_file_archive(ARTIFACT)
        cls.source=s.original_codex_source_guard(REPO,cls.guids)

    def setUp(self):
        self.tmp=tempfile.TemporaryDirectory(prefix="b0140_w02_disposable_")
        self.addCleanup(self.tmp.cleanup)
        self.root=Path(self.tmp.name)/"disposable-project"
        self.root.mkdir()
        (self.root/"Assets").mkdir()
        (self.root/s.MARKER).write_bytes(s.MARKER_BYTES)
        (self.root/"Assets"/"KEEP-FOREIGN-EXISTING.txt").write_text("DON'T REMOVE",encoding="utf-8")
        self.root=s.guard_target(self.root)

    def unchanged(self):
        self.assertEqual((self.root/"Assets"/"KEEP-FOREIGN-EXISTING.txt").read_text(),
                         "DON'T REMOVE")
    def staged(self):
        return s.install(self.root,self.files,self.source)

    def test_01_original_binary_zip_hash_and_27_asset_files_are_real(self):
        self.assertEqual(len(self.files),27)
        self.assertEqual(len(self.guids),15)
        self.assertEqual(s.digest(ARTIFACT.read_bytes()),s.ZIP_SHA)
        self.assertEqual(self.source["new_guid_collisions"],0)

    def test_02_preflight_does_not_write_any_file(self):
        old=s.inventory(self.root)
        self.assertEqual(s.preflight(self.root,self.files)["mode"],"SOURCE_PREFLIGHT_PASS")
        self.assertEqual(old,s.inventory(self.root))

    def test_03_stage_actual_png_meta_files_and_verify_byte_for_byte(self):
        d=self.staged()
        self.assertEqual(d["file_count"],27)
        self.assertEqual(s.verify(self.root,self.files)["file_count"],27)
        for p,original in self.files.items():
            self.assertEqual((self.root/p).read_bytes(),original)
        self.unchanged()

    def test_04_stage_and_cleanup_real_original_bytes_preserves_foreign_files(self):
        old={p: (self.root/p).read_bytes() for p in s.inventory(self.root)}
        self.staged()
        self.assertEqual(s.rollback(self.root,self.files)["removed_own_files"],27)
        self.assertEqual({p:(self.root/p).read_bytes() for p in s.inventory(self.root)},old)
        self.unchanged()

    def test_05_rollback_rejects_modified_sprite_and_keeps_all_others(self):
        self.staged()
        key=next(x for x in self.files if x.endswith(".png"))
        (self.root/key).write_bytes(b"INTERFERENCE")
        with self.assertRaisesRegex(s.Reject,"STAGED_FILE_CHANGED_OR_MISSING"):
            s.rollback(self.root,self.files)
        self.assertTrue((self.root/s.RECEIPT).exists())
        self.assertEqual((self.root/key).read_bytes(),b"INTERFERENCE")
        self.unchanged()

    def test_06_rollback_rejects_new_foreign_file_in_staging(self):
        self.staged()
        foreign=self.root/s.FRAMES/"KEEP_FOREIGN.txt"
        foreign.write_text("other job")
        with self.assertRaisesRegex(s.Reject,"UNTRACKED_FOREIGN_FILES_IN_STAGED_FOLDER"):
            s.rollback(self.root,self.files)
        self.assertEqual(foreign.read_text(),"other job")
        self.unchanged()

    def test_07_second_install_rejects_without_overwriting_original(self):
        self.staged()
        old=(self.root/s.RECEIPT).read_bytes()
        with self.assertRaisesRegex(s.Reject,"TARGET_STAGING_ALREADY_PRESENT"):
            self.staged()
        self.assertEqual(old,(self.root/s.RECEIPT).read_bytes())

    def test_08_missing_explicit_disposable_marker_refuses(self):
        (self.root/s.MARKER).unlink()
        with self.assertRaisesRegex(s.Reject,"NOT_EXPLICIT_DISPOSABLE_TEST_PROJECT"):
            s.guard_target(self.root)
        self.assertFalse((self.root/s.PREFIX).exists())

    def test_09_wrong_disposable_marker_refuses(self):
        (self.root/s.MARKER).write_text("regular Unity project")
        with self.assertRaisesRegex(s.Reject,"NOT_EXPLICIT_DISPOSABLE_TEST_PROJECT"):
            s.guard_target(self.root)

    def test_10_refuse_directory_with_git_worktree_metadata(self):
        (self.root/".git").mkdir()
        with self.assertRaisesRegex(s.Reject,"TARGET_INSIDE_GIT_CHECKOUT"):
            s.guard_target(self.root)

    def test_11_case_insensitive_existing_alias_blocks_install(self):
        p=self.root/"Assets"/"b01importstaging.meta"
        p.write_bytes(b"Foreign")
        with self.assertRaisesRegex(
            s.Reject,"TARGET_(CASE_INSENSITIVE_PATH_COLLISION|STAGING_ALREADY_PRESENT)"
        ):
            s.preflight(self.root,self.files)
        self.assertEqual(p.read_bytes(),b"Foreign")

    def test_12_existing_unregistered_prefix_folder_blocks_staging(self):
        p=self.root/s.PREFIX
        p.mkdir()
        with self.assertRaisesRegex(s.Reject,"TARGET_STAGING_ALREADY_PRESENT"):
            s.preflight(self.root,self.files)
        self.assertTrue(p.is_dir())

    def test_13_receipt_corruption_blocks_rollback(self):
        self.staged()
        r=self.root/s.RECEIPT
        x=json.loads(r.read_bytes());x["source_original_zip_sha256"]="0"*64
        r.write_bytes(s.canonical(x))
        with self.assertRaisesRegex(s.Reject,"STAGE_RECEIPT_CHANGED"):
            s.rollback(self.root,self.files)
        self.assertTrue((self.root/s.PREFIX).is_dir())

    def test_14_deleted_meta_blocks_rollback(self):
        self.staged()
        meta=next(p for p in self.files if p.endswith(".png.meta"))
        (self.root/meta).unlink()
        with self.assertRaisesRegex(s.Reject,"STAGED_FILE_CHANGED_OR_MISSING"):
            s.rollback(self.root,self.files)
        self.unchanged()

    def test_15_source_zip_byte_mutation_fails_crypto_pin(self):
        with tempfile.TemporaryDirectory(prefix="b0140_w02_corrupt_") as t:
            archive=Path(t)/"original-zip.bin"
            original=bytearray(ARTIFACT.read_bytes())
            original[-2]^=1
            archive.write_bytes(original)
            with self.assertRaisesRegex(s.Reject,"ORIGINAL_30_MEMBER_ZIP_SHA_MISMATCH"):
                s.check_file_archive(archive)

    def test_16_symlink_target_is_rejected(self):
        with tempfile.TemporaryDirectory(prefix="b0140_w02_symlink_") as t:
            alias=Path(t)/"link-project"
            try:alias.symlink_to(self.root,target_is_directory=True)
            except (NotImplementedError,OSError):
                self.skipTest("symlink creation not permitted by runner policy")
            with self.assertRaisesRegex(s.Reject,"TARGET_SYMLINK_COMPONENT"):
                s.guard_target(alias)

    def test_17_source_guard_rejects_missing_original_codex_commit(self):
        with patch.object(s,"CODEX","0"*40):
            with self.assertRaisesRegex(s.Reject,"CURRENT_CODEX_GIT_UNAVAILABLE"):
                s.original_codex_source_guard(REPO,self.guids)

    def test_18_cleanup_second_time_fails_without_touching_existing_files(self):
        self.staged()
        s.rollback(self.root,self.files)
        with self.assertRaisesRegex(s.Reject,"STAGE_RECEIPT_MISSING"):
            s.rollback(self.root,self.files)
        self.unchanged()

    def test_19_live_guard_refuses_git_subdirectory_even_when_explicitly_marked(self):
        # Check a fake nested repo (not user's actual checkout).
        top=self.root/"source-controlled"
        top.mkdir()
        (top/".git").write_text("gitdir: external")
        inner=top/"temp-unity"
        inner.mkdir();(inner/"Assets").mkdir()
        (inner/s.MARKER).write_bytes(s.MARKER_BYTES)
        with self.assertRaisesRegex(s.Reject,"TARGET_INSIDE_GIT_CHECKOUT"):
            s.guard_target(inner)

    def test_21_foreign_empty_subdir_prevents_destructive_rollback(self):
        self.staged()
        extradir=self.root/s.FRAMES/"DO_NOT_TOUCH_EMPTY_DIR"
        extradir.mkdir()
        with self.assertRaisesRegex(s.Reject,"UNTRACKED_FOREIGN_DIRECTORY_IN_STAGED_FOLDER"):
            s.rollback(self.root,self.files,self.source)
        self.assertTrue(extradir.is_dir())
        self.assertTrue((self.root/s.RECEIPT).exists())

    def test_22_receipt_origin_mismatch_rejected_against_current_git_source(self):
        self.staged()
        spoof=dict(self.source)
        spoof["codex_asset_paths"]=0
        with self.assertRaisesRegex(s.Reject,"STAGE_RECEIPT_CHANGED"):
            s.verify(self.root,self.files,spoof)
        self.assertTrue((self.root/s.RECEIPT).exists())

    def test_20_source_only_boundary_is_explicit_and_never_uses_live_unity_save(self):
        self.staged()
        r=json.loads((self.root/s.RECEIPT).read_bytes())
        self.assertEqual(r["native_unity"],"NOT_RUN")
        self.assertEqual(r["G1"],"NOT_RUN")
        self.assertEqual(r["G2"],"HOLD")
        self.assertEqual(r["codex_ack"],"PENDING")
        self.assertTrue(all(x.startswith("Assets/B01ImportStaging") for x in r["file_sha256"]))
        self.assertNotIn("Application.persistentDataPath",str(r))

if __name__=="__main__":
    print("B01-40-P17W02-EXACT_ORIGINAL_ASSET_STAGE TEST_ONLY_DISPOSABLE UNITY_NATIVE_NOT_RUN",flush=True)
    unittest.main(verbosity=2)
