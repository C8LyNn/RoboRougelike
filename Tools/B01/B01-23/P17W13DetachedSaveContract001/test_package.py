import io,json,os,tempfile,unittest,warnings,zipfile
from pathlib import Path
import package as p

def changed(raw,overrides=None,extra=None):
    with zipfile.ZipFile(io.BytesIO(raw)) as z:d={n:z.read(n) for n in z.namelist()}
    d.update(overrides or {});d.update(extra or {})
    output=io.BytesIO()
    with zipfile.ZipFile(output,"w") as z:
        for n in sorted(d):z.writestr(p.zinfo(n),d[n])
    return output.getvalue()
class OriginalW13PhysicalPackageTests(unittest.TestCase):
    def test_real_exact_source_paths(self):self.assertEqual(len(p.scope()),16)
    def test_original_frozen_game_head_pin(self):self.assertEqual(p.PIN,"d94400d95d13db7f27e1533df05c9292d5380431")
    def test_four_immutable_game_source_blobs(self):self.assertEqual(len(p.PINS),4)
    def test_original_guids_and_asmdef(self):self.assertIn(p.ASSET+".meta",p.scope())
    def test_real_source_contract(self):p.scope()
    def test_exact_deterministic_zip(self):self.assertEqual(p.build(),p.build())
    def test_original_manifest_all_bytes(self):self.assertEqual(len(p.verify(p.build())),17)
    def test_reject_corrupted_source(self):
        b=p.build();path=p.scope()[2]
        with self.assertRaises(ValueError):p.verify(changed(b,{path:b"tampered"}))
    def test_reject_foreign_asset(self):
        with self.assertRaises(ValueError):p.verify(changed(p.build(),extra={"Assets/Canon/file.cs":b"bad"}))
    def test_reject_zip_traversal(self):
        with self.assertRaises(ValueError):p.verify(changed(p.build(),extra={"../bad.txt":b"bad"}))
    def test_forged_selected_sha_rejected(self):
        b=p.build()
        with zipfile.ZipFile(io.BytesIO(b)) as z:m=json.loads(z.read(p.MANIFEST))
        m["selected_secure_v2_sha"]="baf65f2664e38f3b9c38815c3f07285f8107c1b6"
        with self.assertRaises(ValueError):p.verify(changed(b,{p.MANIFEST:json.dumps(m).encode()}))
    def test_forged_unity_pass_rejected(self):
        b=p.build()
        with zipfile.ZipFile(io.BytesIO(b)) as z:m=json.loads(z.read(p.MANIFEST))
        m["native_unity"]="PASS"
        with self.assertRaises(ValueError):p.verify(changed(b,{p.MANIFEST:json.dumps(m).encode()}))
    def test_forged_host_integration_rejected(self):
        b=p.build()
        with zipfile.ZipFile(io.BytesIO(b)) as z:m=json.loads(z.read(p.MANIFEST))
        m["savegame_host"]="INTEGRATED"
        with self.assertRaises(ValueError):p.verify(changed(b,{p.MANIFEST:json.dumps(m).encode()}))
    def test_real_external_scratch_stage_rollback(self):
        with tempfile.TemporaryDirectory() as t:
            path=Path(t)/"fresh";b=p.build();p.stage(b,path)
            self.assertTrue((path/p.MANIFEST).exists());p.rollback(b,path)
            self.assertFalse(path.exists())
    def test_refuse_existing_folder(self):
        with tempfile.TemporaryDirectory() as t:
            with self.assertRaises(ValueError):p.stage(p.build(),Path(t))
    def test_refuse_repository_write(self):
        with self.assertRaises(ValueError):p.stage(p.build(),p.ROOT/"dist"/"unsafe")
    def test_refuse_foreign_empty_dir_on_rollback(self):
        with tempfile.TemporaryDirectory() as t:
            path=Path(t)/"fresh";b=p.build();p.stage(b,path)
            (path/"foreign_empty_directory").mkdir()
            with self.assertRaisesRegex(ValueError,"foreign empty"):p.rollback(b,path)
            self.assertTrue((path/"foreign_empty_directory").is_dir())
    def test_refuse_foreign_nonempty_dir_on_rollback(self):
        with tempfile.TemporaryDirectory() as t:
            path=Path(t)/"fresh";b=p.build();p.stage(b,path)
            (path/"not_owned").mkdir();(path/"not_owned"/"keep.txt").write_text("safe")
            with self.assertRaises(ValueError):p.rollback(b,path)
            self.assertEqual((path/"not_owned"/"keep.txt").read_text(),"safe")
    def test_refuse_dirty_owned_bytes_on_rollback(self):
        with tempfile.TemporaryDirectory() as t:
            path=Path(t)/"fresh";b=p.build();p.stage(b,path)
            (path/p.scope()[0]).write_bytes(b"corrupt")
            with self.assertRaises(ValueError):p.rollback(b,path)
            self.assertTrue(path.exists())
    def test_refuse_missing_files_on_rollback(self):
        with tempfile.TemporaryDirectory() as t:
            path=Path(t)/"fresh";b=p.build();p.stage(b,path)
            (path/p.scope()[0]).unlink()
            with self.assertRaises(ValueError):p.rollback(b,path)
            self.assertTrue(path.exists())
    def test_refuse_foreign_file_on_rollback(self):
        with tempfile.TemporaryDirectory() as t:
            path=Path(t)/"fresh";b=p.build();p.stage(b,path)
            (path/"keep.txt").write_text("foreign")
            with self.assertRaises(ValueError):p.rollback(b,path)
            self.assertEqual((path/"keep.txt").read_text(),"foreign")
    def test_refuse_symlink_on_rollback(self):
        with tempfile.TemporaryDirectory() as t:
            path=Path(t)/"fresh";b=p.build();p.stage(b,path)
            try:(path/"link").symlink_to(path/"keep")
            except (OSError,NotImplementedError):self.skipTest("symlinks forbidden on host")
            with self.assertRaises(ValueError):p.rollback(b,path)
            self.assertTrue((path/"link").is_symlink())
    def test_reject_missing_original_frozen_id_version(self):
        b=p.build()
        with zipfile.ZipFile(io.BytesIO(b)) as z:m=json.loads(z.read(p.MANIFEST))
        m["version"]="0.0.0"
        with self.assertRaises(ValueError):p.verify(changed(b,{p.MANIFEST:json.dumps(m).encode()}))
if __name__=="__main__":unittest.main(verbosity=2)
