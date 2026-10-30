#!/usr/bin/env python3
"""Original #596 physical source and disposable negative receiver QA, not Unity."""
import argparse
import json
import tempfile
import unittest
from pathlib import Path
from audit import (load_selected, selected_archive, scan_exclusive_receiver,
                   SELECTED_SHA, GAME_SHA, SAVE_SHA, ZIP_SHA256, ZIP_SIZE,
                   ALT_604_SHA, SOURCE_COUNT, ZIP_ENTRIES)

parser = argparse.ArgumentParser(add_help=False)
parser.add_argument("--selected", required=True)
options, extra = parser.parse_known_args()
unittest_argv = ["b0123-w17-test"] + extra

class OriginalSelected596(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.checkout = Path(options.selected).resolve()
        cls.package = load_selected(cls.checkout)
        cls.archive, cls.files, cls.manifest = selected_archive(cls.checkout)

    def new_staged(self, temporary):
        target = Path(temporary) / "exclusive-selected"
        self.package.stage(self.archive, target)
        return target

    def test_01_exact_frozen_producer_and_dependencies(self):
        self.assertEqual(self.package.git("rev-parse", "HEAD"), SELECTED_SHA)
        self.assertEqual(self.package.SELECTED_GAME, GAME_SHA)
        self.assertEqual(self.package.SELECTED_SAVE, SAVE_SHA)

    def test_02_production_blob_and_604_alternative_separated(self):
        self.assertNotEqual(SELECTED_SHA, ALT_604_SHA)
        self.assertFalse((self.checkout /
            "Assets/B0123UnitySaveBridge001/Core/CheckpointFence.cs").exists())

    def test_03_original_inner_source_zip_byte_identity(self):
        import hashlib
        self.assertEqual(len(self.archive), ZIP_SIZE)
        self.assertEqual(hashlib.sha256(self.archive).hexdigest(), ZIP_SHA256)

    def test_04_original_manifest_source_and_unity_metadata(self):
        self.assertEqual(len(self.files), ZIP_ENTRIES)
        self.assertEqual(len(self.package.scope()), SOURCE_COUNT)
        self.assertEqual(self.manifest["unity_guid_count"], 10)
        self.assertEqual(self.manifest["asmdef_count"], 3)
        self.assertEqual(self.manifest["own_head_sha"], SELECTED_SHA)

    def test_05_no_fake_native_codex_ack_or_canonical_live_hydration(self):
        self.assertEqual(self.manifest["native_unity"], "NOT_RUN")
        self.assertEqual(self.manifest["canonical_510_live_import"], "HOLD")
        self.assertIs(self.manifest["codex_ack"], False)

    def test_06_original_disposable_stage_readonly_preflight(self):
        with tempfile.TemporaryDirectory() as scratch:
            receiver = self.new_staged(scratch)
            self.assertEqual(scan_exclusive_receiver(receiver, self.files), ZIP_ENTRIES)
            self.assertEqual(self.package.verify(self.archive), self.files)

    def test_07_original_safe_disposable_rollback(self):
        with tempfile.TemporaryDirectory() as scratch:
            receiver = self.new_staged(scratch)
            self.package.rollback(self.archive, receiver)
            self.assertFalse(receiver.exists())

    def test_08_competing_604_checkpoint_source_rejected(self):
        with tempfile.TemporaryDirectory() as scratch:
            receiver = self.new_staged(scratch)
            competitor = receiver / "Assets/B0123UnitySaveBridge001/Core/CheckpointFence.cs"
            competitor.write_text("CONFLICTING_ALT_604", encoding="utf-8")
            with self.assertRaisesRegex(ValueError, "FOREIGN_OR_COMPETING"):
                scan_exclusive_receiver(receiver, self.files)
            self.assertTrue(competitor.exists())

    def test_09_competing_585_unity_host_rejected(self):
        with tempfile.TemporaryDirectory() as scratch:
            receiver = self.new_staged(scratch)
            extra = receiver / "Assets/B0123SecureV2HostBridge001"
            extra.mkdir()
            with self.assertRaisesRegex(ValueError, "FOREIGN_EMPTY"):
                scan_exclusive_receiver(receiver, self.files)
            self.assertTrue(extra.exists())

    def test_10_foreign_empty_directory_is_never_deleted(self):
        with tempfile.TemporaryDirectory() as scratch:
            receiver = self.new_staged(scratch)
            extra = receiver / "Assets/FOREIGN_EMPTY_OWNED_BY_RECEIVER"
            extra.mkdir()
            with self.assertRaisesRegex(ValueError, "FOREIGN_EMPTY"):
                scan_exclusive_receiver(receiver, self.files)
            with self.assertRaises(ValueError):
                self.package.rollback(self.archive, receiver)
            self.assertTrue(extra.is_dir())

    def test_11_dirty_original_byte_receipt_rejected(self):
        with tempfile.TemporaryDirectory() as scratch:
            receiver = self.new_staged(scratch)
            name = self.package.scope()[0]
            (receiver / name).write_bytes(b"HOST_CHANGED")
            with self.assertRaisesRegex(ValueError, "DIRTY_OR_ALTERED"):
                scan_exclusive_receiver(receiver, self.files)
            self.assertTrue((receiver / name).exists())

    def test_12_missing_original_file_rejected(self):
        with tempfile.TemporaryDirectory() as scratch:
            receiver = self.new_staged(scratch)
            name = self.package.scope()[0]
            (receiver / name).unlink()
            with self.assertRaisesRegex(ValueError, "MISSING_ORIGINAL"):
                scan_exclusive_receiver(receiver, self.files)

    def test_13_symlink_receiver_refusal_no_removal(self):
        with tempfile.TemporaryDirectory() as scratch:
            receiver = self.new_staged(scratch)
            link = receiver / "foreign-link"
            try:
                link.symlink_to(receiver / "not-real")
            except (OSError, NotImplementedError):
                self.skipTest("host forbids symlink creation")
            with self.assertRaisesRegex(ValueError, "SYMLINK"):
                scan_exclusive_receiver(receiver, self.files)
            self.assertTrue(link.is_symlink())

    def test_14_optional_mixed_guid_probe_original_scope(self):
        # All original package own meta files were already pinned by the
        # producer, and are checked against actual source bytes in the ZIP.
        names = [name for name in self.package.scope() if name.endswith(".meta")]
        self.assertEqual(len(names), 10)
        guids = []
        for name in names:
            data = self.files[name].decode("utf-8")
            markers = [line.split(":", 1)[1].strip()
                       for line in data.splitlines() if line.startswith("guid:")]
            self.assertEqual(len(markers), 1)
            guids.extend(markers)
        self.assertEqual(len(set(guids)), 10)

if __name__ == "__main__":
    unittest.main(argv=unittest_argv, verbosity=2)
