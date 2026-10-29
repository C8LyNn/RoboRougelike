import copy
import json
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import compat_gate as g

class OriginalR3GateTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.repo = HERE.parents[3]
        cls.catalog, cls.manifest_sha = g.load_source(cls.repo)
        cls.old_r3 = g.content(cls.repo, g.OLD, g.R3).decode("utf-8")
        cls.new_r3 = g.content(cls.repo, g.CURRENT, g.R3).decode("utf-8")
        cls.r4_old = g.content(cls.repo, g.OLD, g.R4).decode("utf-8")
        cls.r4_new = g.content(cls.repo, g.CURRENT, g.R4).decode("utf-8")

    def reject(self, catalog=None, old=None, new=None, r4old=None, r4new=None):
        with self.assertRaises((g.GateError, KeyError)):
            g.check_versions(
                self.catalog if catalog is None else catalog,
                self.old_r3 if old is None else old,
                self.new_r3 if new is None else new,
                self.r4_old if r4old is None else r4old,
                self.r4_new if r4new is None else r4new)

    def test_01_real_13_immutable_file_manifest(self):
        self.assertEqual(len(self.manifest_sha), 64)

    def test_02_exact_partial_only_change(self):
        self.assertEqual(self.old_r3.replace(g.DECL_OLD, g.DECL_NEW, 1), self.new_r3)

    def test_03_exact_pinned_r3_blob(self):
        self.assertEqual(g.blob(self.repo, g.CURRENT, g.R3), g.NEW_R3)

    def test_04_original_r4_unchanged(self):
        self.assertEqual(g.blob(self.repo, g.CURRENT, g.R4), g.R4_BLOB)

    def test_05_all_10_kits_same_prices(self):
        result = g.check_versions(self.catalog, self.old_r3, self.new_r3, self.r4_old, self.r4_new)
        self.assertEqual(len(result), 10)
        self.assertEqual(result[7]["reference_scrap"], 235)
        self.assertEqual(result[7]["physical_items"], 5)

    def test_06_full_source_receipt_real_git(self):
        self.assertEqual(g.verify(self.repo)["status"], "EXACT_SOURCE_COMPATIBLE_NOT_INTEGRATED")

    def test_07_extra_source_change_rejected(self):
        self.reject(new=self.new_r3 + "\n// unexpected extra change")

    def test_08_changed_r3_part_price_rejected(self):
        changed = self.new_r3.replace("R3PartKind.Booster=>35", "R3PartKind.Booster=>39", 1)
        self.assertNotEqual(changed, self.new_r3)
        self.reject(new=changed)

    def test_09_missing_partial_rejected(self):
        self.reject(new=self.new_r3.replace(g.DECL_NEW, g.DECL_OLD, 1))

    def test_10_catalog_part_price_rejected(self):
        catalog = copy.deepcopy(self.catalog)
        catalog["part_prices"]["Wheel"] += 1
        self.reject(catalog=catalog)

    def test_11_catalog_weapon_price_rejected(self):
        catalog = copy.deepcopy(self.catalog)
        catalog["weapon_prices"]["Railgun"] += 1
        self.reject(catalog=catalog)

    def test_12_modified_r4_rejected(self):
        self.reject(r4new=self.r4_new + "\n// incompatible weapon code")

    def test_13_unknown_part_rejected(self):
        catalog = copy.deepcopy(self.catalog)
        catalog["kits"][0]["parts"][0]["kind"] = "FuturePart"
        self.reject(catalog=catalog)

    def test_14_third_weapon_type_rejected(self):
        catalog = copy.deepcopy(self.catalog)
        catalog["kits"][0]["parts"] = [
            {"kind":"Gun","weapon":weapon,"count":1}
            for weapon in ("Basic","Railgun","Heavy")]
        self.reject(catalog=catalog)

    def test_15_duplicate_part_rejected(self):
        catalog = copy.deepcopy(self.catalog)
        catalog["kits"][0]["parts"][1] = copy.deepcopy(catalog["kits"][0]["parts"][0])
        self.reject(catalog=catalog)

    def test_16_kit_order_change_rejected(self):
        catalog = copy.deepcopy(self.catalog)
        catalog["kits"][0], catalog["kits"][1] = catalog["kits"][1], catalog["kits"][0]
        self.reject(catalog=catalog)

    def test_17_negative_qty_rejected(self):
        catalog = copy.deepcopy(self.catalog)
        catalog["kits"][0]["parts"][0]["count"] = -1
        self.reject(catalog=catalog)

    def test_18_existing_receipt_never_overwritten(self):
        with tempfile.TemporaryDirectory() as tmp:
            target = Path(tmp)/"receipt.json"
            target.write_text("KEEP", encoding="utf-8")
            run = subprocess.run([sys.executable, str(HERE/"compat_gate.py"),
                "--repo", str(self.repo), "--receipt", str(target)],
                capture_output=True, text=True, encoding="utf-8")
            self.assertEqual(run.returncode, 2)
            self.assertEqual(target.read_text(encoding="utf-8"), "KEEP")

    def test_19_generate_new_source_pinned_receipt(self):
        with tempfile.TemporaryDirectory() as tmp:
            target = Path(tmp)/"new-receipt.json"
            run = subprocess.run([sys.executable, str(HERE/"compat_gate.py"),
                "--repo", str(self.repo), "--receipt", str(target)],
                capture_output=True, text=True, encoding="utf-8")
            self.assertEqual(run.returncode, 0, run.stderr)
            receipt = json.loads(target.read_text(encoding="utf-8"))
            self.assertEqual(receipt["frozen_files_verified"], 13)
            self.assertEqual(receipt["codex_integration"], "NOT_PERFORMED")

if __name__ == "__main__":
    unittest.main(verbosity=2)
