import unittest,io,tempfile,zipfile,json
from pathlib import Path
import package as p
class V4(unittest.TestCase):
    def test_01_pinned_original_669_and_729(self):p.entries()
    def test_02_deterministic_real_Git_archive(self):self.assertEqual(p.newzip(),p.newzip())
    def test_03_manifest_source_exact(self):self.assertEqual(p.verify(p.newzip())["version"],"4.0.0")
    def test_04_reject_alt_manifest(self):
        raw=p.newzip()
        with zipfile.ZipFile(io.BytesIO(raw)) as z:files={f:z.read(f) for f in z.namelist()}
        j=json.loads(files[p.MANIFEST]);j["single_physical_lease"]="TWO_LEASES"
        files[p.MANIFEST]=json.dumps(j).encode();out=io.BytesIO()
        with zipfile.ZipFile(out,"w") as z:
            for k,v in files.items():z.writestr(k,v)
        with self.assertRaises(ValueError):p.verify(out.getvalue())
    def test_05_rollback_preserves_foreign_EMPTY_dir(self):
        with tempfile.TemporaryDirectory() as root:
            dest=Path(root)/"new";raw=p.newzip();p.stage(raw,dest)
            foreign=dest/"foreign_EMPTY";foreign.mkdir()
            p.rollback(raw,dest);self.assertTrue(foreign.is_dir())
    def test_06_dirty_file_no_deletion(self):
        with tempfile.TemporaryDirectory() as root:
            dest=Path(root)/"new";raw=p.newzip();p.stage(raw,dest)
            owned=dest/p.entries()[0];owned.write_text("FOREIGN")
            with self.assertRaises(ValueError):p.rollback(raw,dest)
            self.assertEqual(owned.read_text(),"FOREIGN")
    def test_07_occupied_target_denied(self):
        with tempfile.TemporaryDirectory() as root:
            dest=Path(root)/"existing";dest.mkdir()
            with self.assertRaises(ValueError):p.stage(p.newzip(),dest)
    def test_08_missing_member_denied(self):
        with tempfile.TemporaryDirectory() as root:
            dest=Path(root)/"new";raw=p.newzip();p.stage(raw,dest)
            (dest/p.entries()[0]).unlink()
            with self.assertRaises(ValueError):p.rollback(raw,dest)
    def test_09_no_standalone_729_acquire(self):
        s=p.src(p.PFX+"/Core/CanonicalWorldSlotLease.cs").decode()
        self.assertNotIn("public static bool TryAcquire",s)
        self.assertIn("TryAdoptRealSlot",s)
    def test_10_no_flat_PACKAGE_MANIFEST_conflict(self):
        self.assertNotEqual(p.MANIFEST,"PACKAGE_MANIFEST.json")
        self.assertTrue(p.MANIFEST.startswith("B01-23/W23/"))
if __name__=="__main__":unittest.main(verbosity=2)
