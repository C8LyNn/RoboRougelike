import sys,io,json,tempfile,unittest,zipfile
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
import package as p
class RealSourcePackage(unittest.TestCase):
 @classmethod
 def setUpClass(cls):cls.real=p.make()
 def mutate(cls,change):
  with zipfile.ZipFile(io.BytesIO(cls.real)) as z:parts={n:z.read(n) for n in z.namelist()}
  change(parts);out=io.BytesIO()
  with zipfile.ZipFile(out,"w") as z:
   for name in sorted(parts):z.writestr(p.info(name),parts[name])
  return out.getvalue()
 def negative(self,fn):
  with self.assertRaises((ValueError,KeyError,zipfile.BadZipFile)):p.verify(self.mutate(fn))
 def test_01_original_git_identity(self):self.assertEqual(len(p.paths()),20)
 def test_02_deterministic(self):self.assertEqual(p.make(),self.real)
 def test_03_physical_source_verify(self):self.assertEqual(len(p.verify(self.real)),21)
 def test_04_foreign_file(self):self.negative(lambda x:x.update({"Assets/Canonical/Injected.cs":b"a"}))
 def test_05_traversal(self):self.negative(lambda x:x.update({"../bad.txt":b"bad"}))
 def test_06_malformed_byte(self):
  self.negative(lambda x:x.update({"Assets/B0123SecureV2HostBridge001/Runtime/AtomicColdUnityHostBridge.cs":b"tamper"}))
 def test_07_false_native_pass(self):
  self.negative(lambda x:x.update({p.MANIFEST:x[p.MANIFEST].replace(b'"native_unity":"NOT_RUN"',b'"native_unity":"PASS"')}))
 def test_08_false_game510_hydration(self):
  self.negative(lambda x:x.update({p.MANIFEST:x[p.MANIFEST].replace(b'"game510_private_ledger_hydrated":false',b'"game510_private_ledger_hydrated":true"')}))
 def test_09_false_codex_ack(self):
  self.negative(lambda x:x.update({p.MANIFEST:x[p.MANIFEST].replace(b'"codex_ack":false',b'"codex_ack":true"')}))
 def test_10_alternative_contract(self):
  self.negative(lambda x:x.update({p.MANIFEST:x[p.MANIFEST].replace(p.SAVE.encode(),b"6da2c523e03766cd5a6deaed0cb3cccf8765354b")}))
 def test_11_real_clean_external_stage_rollback(self):
  with tempfile.TemporaryDirectory() as d:
   root=Path(d)/"fresh";p.stage(self.real,root);self.assertTrue(root.is_dir())
   p.rollback(self.real,root);self.assertFalse(root.exists())
 def test_12_refuse_existing_target(self):
  with tempfile.TemporaryDirectory() as d:
   with self.assertRaises(ValueError):p.stage(self.real,Path(d))
 def test_13_refuse_repository_target(self):
  with self.assertRaises(ValueError):p.stage(self.real,p.ROOT/"Assets"/"unsafe")
 def test_14_preserve_foreign_empty_dir(self):
  with tempfile.TemporaryDirectory() as d:
   target=Path(d)/"fresh";p.stage(self.real,target)
   (target/"Assets"/"FOREIGN_EMPTY").mkdir()
   with self.assertRaisesRegex(ValueError,"FOREIGN_EMPTY"):p.rollback(self.real,target)
   self.assertTrue((target/"Assets"/"FOREIGN_EMPTY").is_dir())
 def test_15_preserve_foreign_file(self):
  with tempfile.TemporaryDirectory() as d:
   target=Path(d)/"fresh";p.stage(self.real,target)
   (target/"keep.bin").write_bytes(b"foreign")
   with self.assertRaises(ValueError):p.rollback(self.real,target)
   self.assertEqual((target/"keep.bin").read_bytes(),b"foreign")
 def test_16_preserve_modified_owned_bytes(self):
  with tempfile.TemporaryDirectory() as d:
   target=Path(d)/"fresh";p.stage(self.real,target)
   path=target/p.paths()[0];path.write_bytes(b"mutated")
   with self.assertRaises(ValueError):p.rollback(self.real,target)
   self.assertEqual(path.read_bytes(),b"mutated")
 def test_17_preserve_symlink(self):
  with tempfile.TemporaryDirectory() as d:
   target=Path(d)/"fresh";p.stage(self.real,target)
   link=target/"foreign";link.symlink_to(target/"not-a-file")
   with self.assertRaises(ValueError):p.rollback(self.real,target)
   self.assertTrue(link.is_symlink())
 def test_18_reject_missing_owned_file(self):
  with tempfile.TemporaryDirectory() as d:
   target=Path(d)/"fresh";p.stage(self.real,target)
   (target/p.paths()[0]).unlink()
   with self.assertRaises(ValueError):p.rollback(self.real,target)
   self.assertTrue(target.exists())
if __name__=="__main__":unittest.main(verbosity=2)
