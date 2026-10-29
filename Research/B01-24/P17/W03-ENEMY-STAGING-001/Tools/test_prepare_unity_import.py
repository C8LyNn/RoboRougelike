"""Original executable B01-24 W03 deterministic Unity Editor delivery acceptance."""
from __future__ import annotations
import json
from pathlib import Path
import shutil
import tempfile
import unittest
import zipfile
from prepare_unity_import import ROOT, MANIFEST, EDITOR, META, DEST, package, source_inputs, git_blob, UnsafePackage


class DeliveryTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.real = Path.cwd()
        cls.manifest = json.loads((cls.real / MANIFEST).read_text(encoding="utf-8"))

    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name) / "checkout"
        self.root.mkdir()
        paths = [str(MANIFEST),
                 self.manifest["dependency"]["source_file"],
                 self.manifest["dependency"]["source_arena"]]
        paths.extend(x["path"] for x in self.manifest["files"])
        for rel in set(paths):
            dest = self.root / rel
            dest.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(self.real / rel, dest)
        self.out = Path(self.temporary.name) / "new-output"

    def corrupt(self, path, payload=b"\nTAMPERED\n"):
        f = self.root / path
        with f.open("ab") as stream:
            stream.write(payload)

    def test_original_source_pins_match_checkout(self):
        manifest, editor, meta = source_inputs(self.root)
        self.assertEqual(manifest["dependency"]["producer_SHA"],
                         "c39a400cb07ae0fd6f0d8d419a128490cfbcfd8a")
        self.assertEqual(git_blob(editor), "21dd5d598b83d9d704ad59859a213f464d4b0441")
        self.assertEqual(git_blob(meta), "f228154e0a1e10e3188ccf6e0da18562050c20de")

    def test_real_deterministic_zip_with_exact_two_actual_unity_assets(self):
        receipt = package(self.root, self.out)
        self.assertEqual(receipt["unity_native_import"], "NOT_RUN")
        self.assertEqual(receipt["integration"], "NOT_INTEGRATED")
        self.assertEqual(len(receipt["entries"]), 2)
        with zipfile.ZipFile(self.out / receipt["zip_name"]) as z:
            self.assertEqual(sorted(z.namelist()),
                             [DEST + "/B0124EnemyStageBuilder.cs",
                              DEST + "/B0124EnemyStageBuilder.cs.meta"])
            self.assertIsNone(z.testzip())
            for name, relative in [
                (DEST + "/B0124EnemyStageBuilder.cs", EDITOR),
                (DEST + "/B0124EnemyStageBuilder.cs.meta", META),
            ]:
                self.assertEqual(z.read(name), (self.root / relative).read_bytes())

    def test_zip_is_byte_identical_on_separate_fresh_destinations(self):
        x = package(self.root, self.out)
        y = package(self.root, Path(self.temporary.name) / "second-new-output")
        self.assertEqual(x["zip_sha256"], y["zip_sha256"])
        self.assertEqual(x["entries"], y["entries"])

    def test_receiver_receipt_matches_zip_bytes(self):
        x = package(self.root, self.out)
        other = json.loads((self.out / "receiver-receipt.json").read_text("utf-8"))
        self.assertEqual(x, other)
        self.assertEqual(len(other["zip_sha256"]), 64)

    def test_retry_never_overwrites_existing_zip_or_receipt(self):
        x = package(self.root, self.out)
        before = (self.out / x["zip_name"]).read_bytes()
        with self.assertRaisesRegex(UnsafePackage, "overwrite"):
            package(self.root, self.out)
        self.assertEqual(before, (self.out / x["zip_name"]).read_bytes())

    def test_existing_empty_destination_is_not_treated_as_scratch(self):
        self.out.mkdir()
        with self.assertRaisesRegex(UnsafePackage, "overwrite"):
            package(self.root, self.out)

    def test_changed_formation_producer_source_rejected_before_writing(self):
        self.corrupt(self.manifest["dependency"]["source_file"])
        with self.assertRaisesRegex(UnsafePackage, "SOURCE_DRIFT"):
            package(self.root, self.out)
        self.assertFalse(self.out.exists())

    def test_changed_original_r5_arena_rejected_before_writing(self):
        self.corrupt(self.manifest["dependency"]["source_arena"])
        with self.assertRaisesRegex(UnsafePackage, "SOURCE_DRIFT"):
            package(self.root, self.out)
        self.assertFalse(self.out.exists())

    def test_modified_editor_source_rejected_before_writing(self):
        self.corrupt(EDITOR)
        with self.assertRaisesRegex(UnsafePackage, "SOURCE_DRIFT"):
            package(self.root, self.out)
        self.assertFalse(self.out.exists())

    def test_modified_unity_guid_meta_rejected_before_writing(self):
        self.corrupt(META)
        with self.assertRaisesRegex(UnsafePackage, "SOURCE_DRIFT"):
            package(self.root, self.out)

    def test_changed_owner_test_harness_rejected(self):
        self.corrupt(ROOT / "CI/Program.cs")
        with self.assertRaisesRegex(UnsafePackage, "SOURCE_DRIFT"):
            package(self.root, self.out)

    def test_missing_source_rejected_before_writing(self):
        (self.root / EDITOR).unlink()
        with self.assertRaisesRegex(UnsafePackage, "Missing or symlink"):
            package(self.root, self.out)
        self.assertFalse(self.out.exists())

    def test_duplicate_manifest_entry_rejected(self):
        f = self.root / MANIFEST
        data = json.loads(f.read_text(encoding="utf-8"))
        data["files"].append(dict(data["files"][0]))
        f.write_text(json.dumps(data), encoding="utf-8")
        with self.assertRaisesRegex(UnsafePackage, "Duplicate"):
            package(self.root, self.out)

    def test_wrong_parent_sha_rejected(self):
        f = self.root / MANIFEST
        data = json.loads(f.read_text(encoding="utf-8"))
        data["dependency"]["producer_SHA"] = "0"*40
        f.write_text(json.dumps(data), encoding="utf-8")
        with self.assertRaisesRegex(UnsafePackage, "Different parent producer"):
            package(self.root, self.out)

    def test_forged_manifest_sha_rejected(self):
        f = self.root / MANIFEST
        data = json.loads(f.read_text(encoding="utf-8"))
        data["files"][0]["blob"] = "0"*40
        f.write_text(json.dumps(data), encoding="utf-8")
        with self.assertRaisesRegex(UnsafePackage, "SOURCE_DRIFT"):
            package(self.root, self.out)

    def test_symlink_source_refused_if_platform_allows(self):
        original = self.root / EDITOR
        backup = original.with_suffix(".original")
        original.rename(backup)
        try:
            original.symlink_to(backup)
        except (OSError, NotImplementedError):
            self.skipTest("OS does not allow symlinks")
        with self.assertRaisesRegex(UnsafePackage, "symlink"):
            package(self.root, self.out)


if __name__ == "__main__":
    unittest.main()
