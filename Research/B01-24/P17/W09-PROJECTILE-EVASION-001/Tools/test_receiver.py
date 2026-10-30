"""Executable B01-24 W09 original Unity import ZIP receiver unit/negative tests."""
import json
import hashlib
from pathlib import Path
import shutil
import tempfile
import unittest
import zipfile

from build_receiver import BASE, DEST, NAME, Rejected, blob, build

class ReceiverTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.original=Path.cwd()
        cls.source_manifest=json.loads((cls.original/BASE/"manifest.json").read_text("utf-8"))
    def setUp(self):
        self.temp=tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.repo=Path(self.temp.name)/"repo"
        self.repo.mkdir()
        files=[f["path"] for f in self.source_manifest["files"]]
        files += [str(BASE/"manifest.json"),"Assets/R3/R3EnemyArchetype.cs"]
        for rel in set(files):
            target=self.repo/rel
            target.parent.mkdir(parents=True,exist_ok=True)
            shutil.copyfile(self.original/rel,target)
        self.out=Path(self.temp.name)/"receiver"
    def corrupt(self,rel):
        with (self.repo/rel).open("ab") as f:
            f.write(b"\nTAMPERED")
    def test_produces_two_exact_original_unity_assets(self):
        r=build(self.repo,self.out)
        self.assertEqual(r["status"],"SOURCE_ZIP_ONLY_UNITY_NATIVE_NOT_RUN")
        self.assertEqual(len(r["entries"]),2)
        self.assertEqual(r["integration"],"NOT_INTEGRATED")
        with zipfile.ZipFile(self.out/NAME) as z:
            self.assertEqual(sorted(z.namelist()),[
                DEST+"/ProjectileEvasion.cs",DEST+"/ProjectileEvasion.cs.meta"])
            self.assertIsNone(z.testzip())
            self.assertEqual(z.read(DEST+"/ProjectileEvasion.cs"),(self.repo/BASE/"ProjectileEvasion.cs").read_bytes())
            self.assertEqual(z.read(DEST+"/ProjectileEvasion.cs.meta"),(self.repo/BASE/"ProjectileEvasion.cs.meta").read_bytes())
    def test_two_fresh_rebuilds_are_byte_identical(self):
        a=build(self.repo,self.out)
        b=build(self.repo,Path(self.temp.name)/"receiver2")
        self.assertEqual(a["archive_sha256"],b["archive_sha256"])
        self.assertEqual((self.out/NAME).read_bytes(),(Path(self.temp.name)/"receiver2"/NAME).read_bytes())
    def test_receiver_sha_matches_actual_archive_bytes(self):
        r=build(self.repo,self.out)
        self.assertEqual(hashlib.sha256((self.out/NAME).read_bytes()).hexdigest(),r["archive_sha256"])
        self.assertEqual(json.loads((self.out/"receiver-receipt.json").read_text("utf-8")),r)
    def test_existing_empty_dir_never_overwritten(self):
        self.out.mkdir()
        with self.assertRaisesRegex(Rejected,"overwrite"):build(self.repo,self.out)
    def test_duplicate_archive_execution_does_not_mutate_receipt(self):
        r=build(self.repo,self.out)
        old=(self.out/NAME).read_bytes()
        with self.assertRaisesRegex(Rejected,"overwrite"):build(self.repo,self.out)
        self.assertEqual((self.out/NAME).read_bytes(),old)
        self.assertEqual(r["archive_sha256"],hashlib.sha256(old).hexdigest())
    def test_mutated_original_editor_code_rejected(self):
        self.corrupt(BASE/"ProjectileEvasion.cs")
        with self.assertRaisesRegex(Rejected,"SOURCE_DRIFT"):build(self.repo,self.out)
        self.assertFalse(self.out.exists())
    def test_mutated_stable_meta_guid_rejected(self):
        self.corrupt(BASE/"ProjectileEvasion.cs.meta")
        with self.assertRaisesRegex(Rejected,"SOURCE_DRIFT"):build(self.repo,self.out)
    def test_mutated_canonical_codex_R3_source_rejected(self):
        self.corrupt("Assets/R3/R3EnemyArchetype.cs")
        with self.assertRaisesRegex(Rejected,"ABI drift"):build(self.repo,self.out)
        self.assertFalse(self.out.exists())
    def test_mutated_original_scenario_data_rejected(self):
        self.corrupt(BASE/"evasion_scenarios.json")
        with self.assertRaisesRegex(Rejected,"SOURCE_DRIFT"):build(self.repo,self.out)
    def test_mutated_original_test_runner_rejected(self):
        self.corrupt(BASE/"Program.cs")
        with self.assertRaisesRegex(Rejected,"SOURCE_DRIFT"):build(self.repo,self.out)
    def test_missing_tracked_file_rejected(self):
        (self.repo/BASE/"ProjectileEvasion.cs").unlink()
        with self.assertRaisesRegex(Rejected,"Missing"):build(self.repo,self.out)
    def test_source_symlink_rejected(self):
        file=self.repo/BASE/"ProjectileEvasion.cs"
        original=file.with_suffix(".backup")
        file.rename(original)
        try:file.symlink_to(original)
        except (OSError,NotImplementedError):self.skipTest("No OS symlinks")
        with self.assertRaisesRegex(Rejected,"symlink"):build(self.repo,self.out)
    def test_duplicate_manifest_paths_fail_before_archive_write(self):
        mf=self.repo/BASE/"manifest.json"
        obj=json.loads(mf.read_text("utf-8"))
        obj["files"].append(dict(obj["files"][0]))
        mf.write_text(json.dumps(obj),encoding="utf-8")
        with self.assertRaisesRegex(Rejected,"duplicated"):build(self.repo,self.out)
        self.assertFalse(self.out.exists())
    def test_forged_member_blob_fails(self):
        mf=self.repo/BASE/"manifest.json"
        obj=json.loads(mf.read_text("utf-8"))
        obj["files"][0]["blob"]="0"*40
        mf.write_text(json.dumps(obj),encoding="utf-8")
        with self.assertRaisesRegex(Rejected,"SOURCE_DRIFT"):build(self.repo,self.out)
    def test_path_traversal_in_manifest_rejected(self):
        mf=self.repo/BASE/"manifest.json"
        obj=json.loads(mf.read_text("utf-8"))
        obj["files"][0]["path"]="../escape.cs"
        mf.write_text(json.dumps(obj),encoding="utf-8")
        with self.assertRaisesRegex(Rejected,"traversal"):build(self.repo,self.out)
    def test_bad_unity_asset_identity_rejected(self):
        mf=self.repo/BASE/"manifest.json"
        obj=json.loads(mf.read_text("utf-8"))
        obj["package_id"]="B01-OTHER"
        mf.write_text(json.dumps(obj),encoding="utf-8")
        with self.assertRaisesRegex(Rejected,"Wrong package"):build(self.repo,self.out)

if __name__=="__main__":
    unittest.main()
