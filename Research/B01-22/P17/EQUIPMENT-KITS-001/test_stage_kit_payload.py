"""B01-22 wave02 real staging tests against physical source files. No Unity."""
import copy
import json
import shutil
import sys
import tempfile
import unittest
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
from stage_kit_payload import IntakeError, sha, json_bytes, verify_package, make_offers, stage, rollback


class OriginalPackageTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name) / "source"
        shutil.copytree(HERE, self.root)
        self.dest = Path(self.temp.name) / "consumer"

    def change_json(self, filename, mutation, rewrite_manifest=False):
        path = self.root / filename
        obj = json.loads(path.read_bytes())
        mutation(obj)
        data = json_bytes(obj)
        path.write_bytes(data)
        if rewrite_manifest:
            mp = self.root / "manifest.json"
            m = json.loads(mp.read_bytes())
            for entry in m["files"]:
                if entry["path"].endswith("/" + filename):
                    entry["sha256"], entry["bytes"] = sha(data), len(data)
            mp.write_bytes(json_bytes(m))

    def test_01_source_manifest_verifies_all_files(self):
        catalog, ref = verify_package(self.root)
        self.assertEqual(len(catalog["kits"]), 10)
        self.assertEqual(len(ref), 64)

    def test_02_stage_generates_ten_offers(self):
        stage(self.root, self.dest)
        doc = json.loads((self.dest / "offers.json").read_bytes())
        self.assertEqual(len(doc["offers"]), 10)
        self.assertEqual(len({k["id"] for k in doc["offers"]}), 10)

    def test_03_ek08_price_and_five_real_items(self):
        stage(self.root, self.dest)
        doc = json.loads((self.dest / "offers.json").read_bytes())
        kit = next(k for k in doc["offers"] if k["id"] == "EK08")
        self.assertEqual(kit["reference_total_scrap"], 235)
        self.assertEqual(kit["item_count"], 5)

    def test_04_successful_reversible_intake(self):
        stage(self.root, self.dest)
        self.assertFalse(json.loads((self.dest / "receipt.json").read_bytes())["modified_host"])
        self.assertEqual(rollback(self.dest)["status"], "ROLLED_BACK")
        self.assertFalse(self.dest.exists())

    def test_05_cannot_overwrite_existing_user_files(self):
        self.dest.mkdir()
        (self.dest / "owned-save.txt").write_text("KEEP", encoding="utf-8")
        with self.assertRaisesRegex(IntakeError, "DESTINATION_EXISTS"):
            stage(self.root, self.dest)
        self.assertEqual((self.dest / "owned-save.txt").read_text(), "KEEP")

    def test_06_bad_payload_hash_fails_closed(self):
        (self.root / "catalog.json").write_bytes(b"{}")
        with self.assertRaisesRegex(IntakeError, "PAYLOAD_HASH_MISMATCH"):
            verify_package(self.root)

    def test_07_missing_payload_fails_closed(self):
        (self.root / "kit_core.mjs").unlink()
        with self.assertRaisesRegex(IntakeError, "MISSING_PAYLOAD"):
            verify_package(self.root)

    def test_08_manifest_path_traversal_fails(self):
        self.change_json("manifest.json", lambda x: x["files"][0].update({"path": "Research/B01-22/P17/EQUIPMENT-KITS-001/../catalog.json"}))
        with self.assertRaisesRegex(IntakeError, "UNSAFE_OR_DUPLICATE_PATH"):
            verify_package(self.root)

    def test_09_duplicate_manifest_entry_fails(self):
        self.change_json("manifest.json", lambda x: x["files"].append(copy.deepcopy(x["files"][0])))
        with self.assertRaisesRegex(IntakeError, "UNSAFE_OR_DUPLICATE_PATH"):
            verify_package(self.root)

    def test_10_source_symlink_fails(self):
        path = self.root / "kit_cli.mjs"
        preserved = self.root / "saved.mjs"
        path.rename(preserved)
        try:
            path.symlink_to(preserved)
        except (OSError, NotImplementedError):
            self.skipTest("Symlinks not supported")
        with self.assertRaisesRegex(IntakeError, "SYMLINK_REJECTED"):
            verify_package(self.root)

    def test_11_unknown_part_rejected_even_after_manifest_rehash(self):
        self.change_json("catalog.json", lambda x: x["kits"][0]["parts"][0].update({"kind": "Unknown"}), True)
        with self.assertRaisesRegex(IntakeError, "UNKNOWN_PART"):
            verify_package(self.root)

    def test_12_third_weapon_family_rejected(self):
        self.change_json("catalog.json", lambda x: x["kits"][0].update({"parts": [
            {"kind": "Gun", "weapon": kind, "count": 1} for kind in ("Basic", "Heavy", "Railgun")]}), True)
        with self.assertRaisesRegex(IntakeError, "INVALID_KIT_SIZE_OR_GUN_TYPES"):
            verify_package(self.root)

    def test_13_modified_stage_prevents_rollback(self):
        stage(self.root, self.dest)
        (self.dest / "offers.json").write_text("CHANGED", encoding="utf-8")
        with self.assertRaisesRegex(IntakeError, "STAGED_DATA_CHANGED"):
            rollback(self.dest)
        self.assertTrue(self.dest.exists())

    def test_14_unexpected_user_file_prevents_rollback(self):
        stage(self.root, self.dest)
        (self.dest / "do-not-remove.txt").write_text("KEEP", encoding="utf-8")
        with self.assertRaisesRegex(IntakeError, "STAGE_HAS_UNEXPECTED_FILES"):
            rollback(self.dest)
        self.assertTrue((self.dest / "do-not-remove.txt").exists())

    def test_15_symlink_destination_prevents_rollback(self):
        stage(self.root, self.dest)
        alias = Path(self.temp.name) / "alias"
        try:
            alias.symlink_to(self.dest, target_is_directory=True)
        except (OSError, NotImplementedError):
            self.skipTest("Symlinks not supported")
        with self.assertRaisesRegex(IntakeError, "SYMLINK_REJECTED"):
            rollback(alias)
        self.assertTrue(self.dest.exists())

    def test_16_wrong_version_fails(self):
        self.change_json("manifest.json", lambda x: x.update({"revision": "WRONG"}))
        with self.assertRaisesRegex(IntakeError, "INVALID_PACKAGE_VERSION"):
            verify_package(self.root)

    def test_17_verify_only_does_not_write_game(self):
        before = sorted(str(x.relative_to(self.root)) for x in self.root.rglob("*"))
        verify_package(self.root)
        after = sorted(str(x.relative_to(self.root)) for x in self.root.rglob("*"))
        self.assertEqual(before, after)
        self.assertFalse(self.dest.exists())

    def test_18_generated_data_never_claims_purchase_or_mount(self):
        catalog, ref = verify_package(self.root)
        offers = make_offers(catalog, ref)
        self.assertTrue(offers["data_only"])
        self.assertFalse(offers["has_mount_positions"])
        self.assertTrue(offers["host_transaction_required"])
        self.assertEqual(len(offers["offers"]), 10)


if __name__ == "__main__":
    unittest.main()
