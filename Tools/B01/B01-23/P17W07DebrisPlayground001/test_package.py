import io
import json
import pathlib
import tempfile
import unittest
import zipfile
import package as p

def rewrite(data,changes=None,extra=None):
    changes=changes or {}
    extra=extra or {}
    with zipfile.ZipFile(io.BytesIO(data),"r") as archive:
        contents={x:archive.read(x) for x in archive.namelist()}
    contents.update(changes)
    contents.update(extra)
    dst=io.BytesIO()
    with zipfile.ZipFile(dst,"w",compression=zipfile.ZIP_STORED) as archive:
        for path in sorted(contents):
            archive.writestr(p.zipinfo(path),contents[path])
    return dst.getvalue()

class VerifiedPlaygroundSource(unittest.TestCase):
    def test_exact_parent_source_tree(self):
        p.check_parent()
    def test_all_guids_and_asmdefs(self):
        paths,n=p.source_gate()
        self.assertEqual(n,12)
        self.assertTrue(len(paths)>=20)
    def test_binary_bitwise_reproducible(self):
        a,_=p.produce()
        b,_=p.produce()
        self.assertEqual(a,b)
    def test_original_source_manifest(self):
        data,manifest=p.produce()
        self.assertEqual(manifest["source_sha"],p.head())
        self.assertEqual(len(p.verified(data)),len(p.names())+1)
    def test_corruption_refused(self):
        data,_=p.produce()
        self.assertRaises(ValueError,p.verified,rewrite(data,{p.names()[0]:b"changed"}))
    def test_false_native_unity_refused(self):
        data,_=p.produce()
        with zipfile.ZipFile(io.BytesIO(data)) as z:
            manifest=json.loads(z.read(p.MANIFEST))
        manifest["native_unity"]="PASS"
        self.assertRaises(ValueError,p.verified,rewrite(data,{p.MANIFEST:json.dumps(manifest).encode()}))
    def test_false_codex_claim_refused(self):
        data,_=p.produce()
        with zipfile.ZipFile(io.BytesIO(data)) as z:
            manifest=json.loads(z.read(p.MANIFEST))
        manifest["codeX_claimed"]=True
        self.assertRaises(ValueError,p.verified,rewrite(data,{p.MANIFEST:json.dumps(manifest).encode()}))
    def test_extra_member_refused(self):
        data,_=p.produce()
        self.assertRaises(ValueError,p.verified,rewrite(data,extra={"Assets/R3/unowned.cs":b"x"}))
    def test_traversal_refused(self):
        data,_=p.produce()
        self.assertRaises(ValueError,p.verified,rewrite(data,extra={"../escape":b"x"}))
    def test_stage_rollback_verified(self):
        data,_=p.produce()
        with tempfile.TemporaryDirectory() as temp:
            dest=pathlib.Path(temp)/"install"
            p.stage(data,dest)
            self.assertTrue(dest.is_dir())
            p.rollback(data,dest)
            self.assertFalse(dest.exists())
    def test_no_existing_destination_overwrite(self):
        data,_=p.produce()
        with tempfile.TemporaryDirectory() as temp:
            self.assertRaises(ValueError,p.stage,data,pathlib.Path(temp))
    def test_refuse_repo_staging(self):
        data,_=p.produce()
        self.assertRaises(ValueError,p.stage,data,p.ROOT/"Artifacts"/"not-written")
    def test_refuse_foreign_file_deletion(self):
        data,_=p.produce()
        with tempfile.TemporaryDirectory() as temp:
            dest=pathlib.Path(temp)/"install"
            p.stage(data,dest)
            (dest/"foreign.txt").write_text("important")
            self.assertRaises(ValueError,p.rollback,data,dest)
            self.assertTrue((dest/"foreign.txt").exists())
    def test_refuse_mutated_rollback(self):
        data,_=p.produce()
        with tempfile.TemporaryDirectory() as temp:
            dest=pathlib.Path(temp)/"install"
            p.stage(data,dest)
            (dest/p.names()[0]).write_bytes(b"modified")
            self.assertRaises(ValueError,p.rollback,data,dest)
            self.assertTrue(dest.exists())

if __name__=="__main__":unittest.main(verbosity=2)
