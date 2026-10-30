import io,json,tempfile,unittest,zipfile
from pathlib import Path
import package as p
def mutated(raw,extra=None):
    with zipfile.ZipFile(io.BytesIO(raw)) as z:items={x:z.read(x) for x in z.namelist()}
    items.update(extra or {})
    output=io.BytesIO()
    with zipfile.ZipFile(output,"w") as z:
        for x in sorted(items):z.writestr(p.entry(x),items[x])
    return output.getvalue()
class W24OriginalSourceTests(unittest.TestCase):
    def test_frozen_510_556_and_729_original_blob_pins(self):p.guard()
    def test_nine_unique_meta_and_two_asmdefs(self):
        self.assertEqual(len([x for x in p.files() if x.endswith(".meta")]),9)
        self.assertEqual(len([x for x in p.files() if x.endswith(".asmdef")]),2)
    def test_original_Unity_physical_and_native_source_not_faked(self):p.guard()
    def test_deterministic_real_git_zip(self):self.assertEqual(p.build(),p.build())
    def test_manifest_source_only(self):
        m=p.verify(p.build())
        self.assertEqual(m["native_g1"],"NOT_RUN")
        self.assertEqual(m["existing_private_510_rigs"],"HOLD")
    def test_foreign_member_rejected(self):
        with self.assertRaises(ValueError):p.verify(mutated(p.build(),{"../hack":b"a"}))
    def test_tampered_original_blob_rejected(self):
        with self.assertRaises(ValueError):p.verify(mutated(p.build(),{p.files()[0]:b"a"}))
    def test_fake_unity_success_rejected(self):
        with zipfile.ZipFile(io.BytesIO(p.build())) as z:m=json.loads(z.read(p.MANIFEST))
        m["native_g1"]="PASS"
        with self.assertRaises(ValueError):
            p.verify(mutated(p.build(),{p.MANIFEST:json.dumps(m).encode()}))
    def test_scratch_staging_and_safe_rollback(self):
        with tempfile.TemporaryDirectory() as d:
            root=Path(d)/"new";raw=p.build();p.stage(raw,root);p.rollback(raw,root)
            self.assertFalse(root.exists())
    def test_foreign_empty_directory_preserved(self):
        with tempfile.TemporaryDirectory() as d:
            root=Path(d)/"new";raw=p.build();p.stage(raw,root)
            (root/"FOREIGN_EMPTY").mkdir()
            with self.assertRaises(ValueError):p.rollback(raw,root)
            self.assertTrue((root/"FOREIGN_EMPTY").exists())
    def test_dirty_original_preserved(self):
        with tempfile.TemporaryDirectory() as d:
            root=Path(d)/"new";raw=p.build();p.stage(raw,root)
            f=root/p.files()[0];f.write_bytes(b"dirty")
            with self.assertRaises(ValueError):p.rollback(raw,root)
            self.assertEqual(f.read_bytes(),b"dirty")
    def test_existing_foreign_destination_refused(self):
        with tempfile.TemporaryDirectory() as d:
            root=Path(d)/"existing";root.mkdir()
            with self.assertRaises(ValueError):p.stage(p.build(),root)
if __name__=="__main__":unittest.main(verbosity=2)
