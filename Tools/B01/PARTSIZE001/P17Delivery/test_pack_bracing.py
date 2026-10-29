#!/usr/bin/env python3
"""Real original B01-23 source/ZIP/isolated-stage positive and negative QA."""
import importlib.util
import json
from pathlib import Path
import shutil
import tempfile
import unittest
import zipfile

SOURCE=Path(__file__).resolve().parent/"pack_bracing.py"
SPEC=importlib.util.spec_from_file_location("p17_delivery",SOURCE)
assert SPEC and SPEC.loader
p17=importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(p17)
REPO=Path(".").resolve()

class OriginalSourceBundleTests(unittest.TestCase):
    def setUp(self):
        self.temp=tempfile.TemporaryDirectory(prefix="p17-bracing-actual-")
        self.root=Path(self.temp.name)
        self.archive=self.root/"bracing-original.zip"
        self.stage=self.root/"isolated-stage"
        p17.make_archive(REPO,self.archive)
    def tearDown(self):
        self.temp.cleanup()

    def assert_fail(self,fn,category):
        with self.assertRaises((ValueError,OSError,zipfile.BadZipFile),msg=category):
            fn()

    def rewrite(self,source,output,alter,extra=None):
        with zipfile.ZipFile(source,"r") as old:
            with zipfile.ZipFile(output,"x") as newer:
                for member in old.infolist():
                    data=old.read(member.filename)
                    if member.filename==alter:data=b"{\"tampered\":true}"
                    newer.writestr(member,data)
                if extra is not None:
                    name=extra
                    info,data=p17.zip_entry(name,b"NO_CONTENT_ALLOWED")
                    newer.writestr(info,data)

    def test_01_all_actual_original_source_blobs_proven(self):
        files=p17.source_files(REPO)
        self.assertEqual(len(files),10)
        self.assertEqual({p17.gitblob(raw) for raw in files.values()},
                         set(p17.EXPECTED.values()))
    def test_02_zip_real_content_and_receipt(self):
        files=p17.verify_archive(REPO,self.archive)
        self.assertEqual(len(files),11)
        receipt=json.loads(files[p17.RECEIPT])
        self.assertEqual(len(receipt["entries"]),10)
        self.assertEqual(receipt["source_commit"],p17.SOURCE_COMMIT)
        self.assertEqual(receipt["unity_native_validation"],"NOT_RUN")
    def test_03_zip_reproducible_byte_equal(self):
        second=self.root/"second.zip"
        p17.make_archive(REPO,second)
        self.assertEqual(self.archive.read_bytes(),second.read_bytes())
    def test_04_correct_unity_textasset_scope_only(self):
        files=p17.verify_archive(REPO,self.archive)
        names=set(files)-{p17.RECEIPT}
        self.assertEqual(names,set(p17.EXPECTED))
        self.assertEqual(len([n for n in names if n.endswith(".json")]),4)
        self.assertEqual(len([n for n in names if n.endswith(".meta")]),6)
    def test_05_verified_init_stage_and_rollback(self):
        p17.stage_init(REPO,self.stage)
        p17.run_stage(REPO,self.archive,self.stage)
        self.assertEqual(set(p17.staged_paths(self.stage)),set(p17.EXPECTED)|{p17.RECEIPT,p17.MARKER})
        p17.rollback(REPO,self.archive,self.stage)
        self.assertFalse(self.stage.exists())
    def test_06_stage_does_not_write_codex_worktree(self):
        forbidden=REPO/"Assets/PARTSIZE001/Bracing/W03_NOT_ALLOWED"
        self.assert_fail(lambda:p17.stage_init(REPO,forbidden),"guard repository")
        self.assertFalse(forbidden.exists())
    def test_07_existing_stage_rejected(self):
        self.stage.mkdir()
        (self.stage/"owner.txt").write_text("protect",encoding="utf8")
        self.assert_fail(lambda:p17.stage_init(REPO,self.stage),"no overwrite")
        self.assertEqual((self.stage/"owner.txt").read_text(),"protect")
    def test_08_changed_source_blob_rejected(self):
        repo=self.root/"source-copy"
        for path in [*p17.EXPECTED,p17.ROOT_MANIFEST]:
            target=repo/path
            target.parent.mkdir(parents=True,exist_ok=True)
            shutil.copy2(REPO/path,target)
        candidate=next(k for k in p17.EXPECTED if k.endswith(".json"))
        (repo/candidate).write_text("{}\n",encoding="utf8")
        self.assert_fail(lambda:p17.source_files(repo),"source pin")
    def test_09_changed_original_manifest_rejected(self):
        repo=self.root/"manifest-copy"
        manifest=repo/p17.ROOT_MANIFEST
        manifest.parent.mkdir(parents=True,exist_ok=True)
        manifest.write_bytes((REPO/p17.ROOT_MANIFEST).read_bytes()+b" ")
        self.assert_fail(lambda:p17.source_files(repo),"source manifest pin")
    def test_10_changed_zip_member_rejected(self):
        altered=self.root/"mutated.zip"
        item=next(k for k in p17.EXPECTED if k.endswith(".json"))
        self.rewrite(self.archive,altered,item)
        self.assert_fail(lambda:p17.verify_archive(REPO,altered),"changed Unity asset")
    def test_11_changed_archive_receipt_rejected(self):
        altered=self.root/"receipt-mutant.zip"
        self.rewrite(self.archive,altered,p17.RECEIPT)
        self.assert_fail(lambda:p17.verify_archive(REPO,altered),"receipt")
    def test_12_extra_untrusted_archive_member_rejected(self):
        altered=self.root/"extra.zip"
        self.rewrite(self.archive,altered,"no-member-with-this-name",extra="Assets/R3/R3VehicleModel.cs")
        self.assert_fail(lambda:p17.verify_archive(REPO,altered),"Codex source injection")
    def test_13_zip_traversal_refused(self):
        altered=self.root/"traversal.zip"
        self.rewrite(self.archive,altered,"no-member-with-this-name",extra="../escape.txt")
        self.assert_fail(lambda:p17.verify_archive(REPO,altered),"zip slip")
    def test_14_duplicate_zip_member_refused(self):
        altered=self.root/"duplicate.zip"
        with zipfile.ZipFile(self.archive) as original,zipfile.ZipFile(altered,"x") as changed:
            for i in original.infolist():
                changed.writestr(i,original.read(i))
            i,data=p17.zip_entry(next(iter(p17.EXPECTED)),b"ALIAS")
            changed.writestr(i,data)
        self.assert_fail(lambda:p17.verify_archive(REPO,altered),"duplicated path")
    def test_15_reject_stage_without_init(self):
        self.assert_fail(lambda:p17.run_stage(REPO,self.archive,self.stage),"no marker")
    def test_16_reject_stage_with_extra_existing_file(self):
        p17.stage_init(REPO,self.stage)
        (self.stage/"dont-delete.txt").write_text("owner",encoding="utf8")
        self.assert_fail(lambda:p17.run_stage(REPO,self.archive,self.stage),"nonempty stage")
        self.assertEqual((self.stage/"dont-delete.txt").read_text(),"owner")
    def test_17_refuse_rollback_after_own_file_changed(self):
        p17.stage_init(REPO,self.stage);p17.run_stage(REPO,self.archive,self.stage)
        name=next(k for k in p17.EXPECTED if k.endswith(".json"))
        target=self.stage/name
        original=target.read_bytes()
        target.write_bytes(b"modified by receiver")
        self.assert_fail(lambda:p17.rollback(REPO,self.archive,self.stage),"user edits preserved")
        self.assertEqual(target.read_bytes(),b"modified by receiver")
        target.write_bytes(original)
        p17.rollback(REPO,self.archive,self.stage)
        self.assertFalse(self.stage.exists())
    def test_18_refuse_rollback_with_extra_unrelated_file(self):
        p17.stage_init(REPO,self.stage);p17.run_stage(REPO,self.archive,self.stage)
        own=self.stage/"other-owner-document.txt"
        own.write_text("preserve",encoding="utf8")
        self.assert_fail(lambda:p17.rollback(REPO,self.archive,self.stage),"unknown content")
        self.assertEqual(own.read_text(),"preserve")
        own.unlink();p17.rollback(REPO,self.archive,self.stage)
    def test_19_refuse_fake_guid_header_and_mutated_source(self):
        self.assertFalse(p17.safe_name("../Assets/test"))
        self.assertFalse(p17.safe_name("C:\\Windows\\System32"))
        self.assertTrue(p17.safe_name("Assets/PARTSIZE001/Bracing/P17/a.json"))
    def test_20_unexpected_archive_overwrite_rejected(self):
        self.assert_fail(lambda:p17.make_archive(REPO,self.archive),"preserve existing ZIP")
        self.assertEqual(len(p17.verify_archive(REPO,self.archive)),11)

if __name__=="__main__":
    unittest.main(verbosity=2)
