import io
import json
from pathlib import Path
import tempfile
import unittest
import warnings
import zipfile
import package as p

def rewrite(payload,changes=None,extra=None):
    with zipfile.ZipFile(io.BytesIO(payload)) as original:
        items={n:original.read(n) for n in original.namelist()}
    items.update(changes or {})
    items.update(extra or {})
    buff=io.BytesIO()
    with zipfile.ZipFile(buff,"w",compression=zipfile.ZIP_STORED) as out:
        for path in sorted(items):
            out.writestr(p.info(path),items[path])
    return buff.getvalue()

class RealOriginalSourcePackageTest(unittest.TestCase):
    def test_frozen_parent_source_blobs(self):
        files,metas=p.original_scope()
        self.assertEqual(metas,12)
        self.assertGreaterEqual(len(files),20)
    def test_deterministic_actual_zip(self):
        a,_=p.produce()
        b,_=p.produce()
        self.assertEqual(a,b)
        self.assertEqual(p.digest(a),p.digest(b))
    def test_physical_sha_manifest(self):
        content,receipt=p.produce()
        self.assertEqual(receipt["source_sha"],p.source_sha())
        self.assertEqual(len(p.verify(content)),len(p.original_scope()[0])+1)
    def test_corrupted_source_rejected(self):
        data,_=p.produce()
        self.assertRaises(ValueError,p.verify,rewrite(data,{p.original_scope()[0][0]:b"mutated"}))
    def test_foreign_game_file_rejected(self):
        data,_=p.produce()
        self.assertRaises(ValueError,p.verify,rewrite(data,extra={"Assets/R3/Unowned.cs":b"bad"}))
    def test_zip_slip_rejected(self):
        data,_=p.produce()
        self.assertRaises(ValueError,p.verify,rewrite(data,extra={"../unsafe.cs":b"bad"}))
    def test_false_native_pass_rejected(self):
        data,_=p.produce()
        with zipfile.ZipFile(io.BytesIO(data)) as archive:
            receipt=json.loads(archive.read(p.MANIFEST))
        receipt["unity_engine_native"]="PASS"
        self.assertRaises(ValueError,p.verify,rewrite(data,{p.MANIFEST:json.dumps(receipt).encode()}))
    def test_false_codex_ack_rejected(self):
        data,_=p.produce()
        with zipfile.ZipFile(io.BytesIO(data)) as archive:
            receipt=json.loads(archive.read(p.MANIFEST))
        receipt["codex_ack"]=True
        self.assertRaises(ValueError,p.verify,rewrite(data,{p.MANIFEST:json.dumps(receipt).encode()}))
    def test_parent_pin_forgery_rejected(self):
        data,_=p.produce()
        with zipfile.ZipFile(io.BytesIO(data)) as archive:
            receipt=json.loads(archive.read(p.MANIFEST))
        receipt["requires_w08_sha"]="0"*40
        self.assertRaises(ValueError,p.verify,rewrite(data,{p.MANIFEST:json.dumps(receipt).encode()}))
    def test_safe_stage_and_rollback(self):
        data,_=p.produce()
        with tempfile.TemporaryDirectory() as scratch:
            target=Path(scratch)/"isolate"
            p.stage(data,target)
            self.assertTrue(target.is_dir())
            p.rollback(data,target)
            self.assertFalse(target.exists())
    def test_no_overwrite_existing_dir(self):
        data,_=p.produce()
        with tempfile.TemporaryDirectory() as scratch:
            self.assertRaises(ValueError,p.stage,data,Path(scratch))
    def test_no_staging_inside_repo(self):
        data,_=p.produce()
        self.assertRaises(ValueError,p.stage,data,p.ROOT/"Artifacts"/"not-written")
    def test_rollback_refuses_modified_files(self):
        data,_=p.produce()
        with tempfile.TemporaryDirectory() as scratch:
            target=Path(scratch)/"isolate"
            p.stage(data,target)
            (target/p.original_scope()[0][0]).write_bytes(b"modified")
            self.assertRaises(ValueError,p.rollback,data,target)
            self.assertTrue(target.exists())
    def test_rollback_refuses_foreign_files(self):
        data,_=p.produce()
        with tempfile.TemporaryDirectory() as scratch:
            target=Path(scratch)/"isolate"
            p.stage(data,target)
            (target/"important.txt").write_text("must not delete")
            self.assertRaises(ValueError,p.rollback,data,target)
            self.assertTrue((target/"important.txt").exists())
    def test_rollback_refuses_missing_file(self):
        data,_=p.produce()
        with tempfile.TemporaryDirectory() as scratch:
            target=Path(scratch)/"isolate"
            p.stage(data,target)
            (target/p.original_scope()[0][0]).unlink()
            self.assertRaises(ValueError,p.rollback,data,target)
            self.assertTrue(target.exists())
    def test_duplicate_zip_entry_rejected(self):
        data,_=p.produce()
        with zipfile.ZipFile(io.BytesIO(data)) as archive:
            objects={k:archive.read(k) for k in archive.namelist()}
        buff=io.BytesIO()
        with zipfile.ZipFile(buff,"w",compression=zipfile.ZIP_STORED) as archive:
            for name in sorted(objects):
                archive.writestr(p.info(name),objects[name])
            with warnings.catch_warnings():
                warnings.simplefilter("ignore",UserWarning)
                archive.writestr(p.info(p.MANIFEST),objects[p.MANIFEST])
        self.assertRaises(ValueError,p.verify,buff.getvalue())

if __name__=="__main__":
    unittest.main(verbosity=2)
