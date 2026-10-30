import importlib.util
import io
from pathlib import Path
import sys
import tempfile
import unittest
from unittest.mock import patch
import zipfile
spec=importlib.util.spec_from_file_location("p17dep",Path(__file__).with_name("pack_deps.py"))
m=importlib.util.module_from_spec(spec);sys.modules[spec.name]=m;spec.loader.exec_module(m)
class ExactOriginalDependencyTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.files=m.source()
        cls.raw=m.make(cls.files)
    def changed(self,f):
        with zipfile.ZipFile(io.BytesIO(self.raw)) as z:
            items=[(p,z.read(p)) for p in z.namelist()]
        items=f(items)
        out=io.BytesIO()
        with zipfile.ZipFile(out,"w") as z:
            for p,data in items: z.writestr(m.zipinfo(p),data)
        return out.getvalue()
    def test_exact_original_16_blobs(self):
        self.assertEqual(len(self.files),16)
        self.assertTrue(all(m.blob(d)==m.PINS[p] for p,d in self.files.items()))
    def test_reproducible_archive(self): self.assertEqual(self.raw,m.make(self.files))
    def test_actual_zip_positive(self): self.assertEqual(len(m.verify(self.raw)),16)
    def test_real_original_codex_preflight(self):
        result=m.preflight(self.raw,m.TARGET)
        self.assertEqual(result["missing_four"],4)
        self.assertGreaterEqual(result["target_metas"],1000)
        self.assertEqual(result["path_guid_collision"],"NONE_AT_EXACT_SHA")
    def test_corrupt_original_byte(self):
        def modify(items):
            return [(p,d+b"MODIFIED" if p.endswith("PartSizeContract.cs") else d) for p,d in items]
        with self.assertRaises(m.Hold):m.verify(self.changed(modify))
    def test_forged_ack(self):
        with self.assertRaises(m.Hold):m.verify(self.changed(lambda it:[(p,d.replace(b'"codex_ack": false',b'"codex_ack": true') if p==m.RECEIPT else d) for p,d in it]))
    def test_missing_member(self):
        with self.assertRaises(m.Hold):m.verify(self.changed(lambda it:it[1:]))
    def test_duplicate_member(self):
        with self.assertRaises(m.Hold):m.verify(self.changed(lambda it:it+[it[0]]))
    def test_extra_codex_file(self):
        with self.assertRaises(m.Hold):m.verify(self.changed(lambda it:it+[("Assets/R3/Vehicle.cs",b"danger")]))
    def test_zip_slip(self):
        with self.assertRaises(m.Hold):m.verify(self.changed(lambda it:it+[("../danger.cs",b"danger")]))
    def test_duplicate_original_guid(self):
        data=self.files.copy(); keys=[p for p in data if p.endswith(".meta")]
        data[keys[1]]=data[keys[0]]
        with self.assertRaises(m.Hold):m.unique_guids(data)
    def test_tamper_before_export(self):
        data=self.files.copy();p="Assets/PARTSIZE001/Runtime/PartSizeContract.cs";data[p]+=b"\n"
        with self.assertRaises(m.Hold):m.make(data)
    def test_disposable_stage_and_rollback(self):
        with tempfile.TemporaryDirectory() as parent:
            root=Path(parent)/"new"
            m.stage(self.raw,root)
            self.assertEqual((root/"Assets/PARTSIZE001/Runtime/PartSizeContract.cs").read_bytes(),self.files["Assets/PARTSIZE001/Runtime/PartSizeContract.cs"])
            self.assertEqual(m.rollback(self.raw,root),"ROLLBACK_OWNED_FILES_PASS")
            self.assertFalse(root.exists())
    def test_refuse_existing_directory(self):
        with tempfile.TemporaryDirectory() as d:
            with self.assertRaises(m.Hold):m.stage(self.raw,Path(d))
    def test_rollback_refuses_modified_original(self):
        with tempfile.TemporaryDirectory() as d:
            root=Path(d)/"new";m.stage(self.raw,root)
            p=root/"Assets/PARTSIZE001/Runtime/PartSizeContract.cs";old=p.read_bytes();p.write_bytes(b"changed")
            with self.assertRaises(m.Hold):m.rollback(self.raw,root)
            self.assertTrue(p.exists());p.write_bytes(old);m.rollback(self.raw,root)
    def test_rollback_refuses_foreign_file(self):
        with tempfile.TemporaryDirectory() as d:
            root=Path(d)/"new";m.stage(self.raw,root)
            p=root/"Assets/R3/foreign.cs";p.parent.mkdir();p.write_text("foreign")
            with self.assertRaises(m.Hold):m.rollback(self.raw,root)
            self.assertTrue(p.exists());p.unlink();p.parent.rmdir();m.rollback(self.raw,root)
    def test_target_source_path_collision(self):
        with patch.object(m,"tree_at",return_value={"Assets/PARTSIZE001/Runtime/PartSizeContract.cs":"0"*40}):
            with patch.object(m,"batch_blobs",return_value=[]):
                with self.assertRaises(m.Hold):m.preflight(self.raw,m.TARGET)
    def test_target_casefold_collision(self):
        with patch.object(m,"tree_at",return_value={"assets/partsize001/runtime/partsizecontract.cs":"0"*40}):
            with patch.object(m,"batch_blobs",return_value=[]):
                with self.assertRaises(m.Hold):m.preflight(self.raw,m.TARGET)
if __name__=="__main__":unittest.main()
